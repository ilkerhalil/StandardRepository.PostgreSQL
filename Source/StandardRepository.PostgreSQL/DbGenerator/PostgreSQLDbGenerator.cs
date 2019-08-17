using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Npgsql;

using StandardRepository.DbGenerator;
using StandardRepository.Helpers;
using StandardRepository.Models;
using StandardRepository.PostgreSQL.Helpers;
using StandardRepository.PostgreSQL.Helpers.SqlExecutor;

namespace StandardRepository.PostgreSQL.DbGenerator
{
    public class PostgreSQLDbGenerator : DbGenerator<NpgsqlConnection, NpgsqlCommand, NpgsqlParameter>
    {
        private readonly TypeLookup _typeLookup;
        private new readonly PostgreSQLExecutor _sqlExecutor;
        private readonly List<Type> _entityTypes;

        public PostgreSQLDbGenerator(TypeLookup typeLookup, EntityUtils entityUtils, PostgreSQLExecutor sqlExecutorMaster,
                                     PostgreSQLExecutor sqlExecutor) : base(entityUtils, sqlExecutorMaster, sqlExecutor)
        {
            _typeLookup = typeLookup;
            _sqlExecutor = sqlExecutor;
            _entityTypes = _entityUtils.GetEntityTypes();
        }

        public override List<string> GenerateSchemas()
        {
            var schemas = _entityTypes.Select(x => _entityUtils.GetSchemaName(x)).Distinct().ToList();
            for (var i = 0; i < schemas.Count; i++)
            {
                var schema = schemas[i];
                _sqlExecutor.ExecuteSql($"CREATE SCHEMA IF NOT EXISTS {schema};").Wait();
            }

            return schemas;
        }

        public override List<string> GenerateTables()
        {
            var result = new List<string>();

            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                var tableFullName = _entityUtils.GetTableFullName(entityType);
                result.Add(tableFullName);

                var createTableSql = GetCreateTableSql(entityType);
                _sqlExecutor.ExecuteSql(createTableSql).Wait();

                var createRevisionTableSql = GetCreateRevisionTableSql(entityType);
                _sqlExecutor.ExecuteSql(createRevisionTableSql).Wait();
            }

            return result;
        }

        private string GetCreateTableSql(Type entityType)
        {
            var sb = new StringBuilder();
            sb.Append($"{PostgreSQLConstants.CREATE_TABLE_IF_NOT_EXISTS} {_entityUtils.GetTableFullName(entityType)}{Environment.NewLine}");
            sb.Append($"({Environment.NewLine}");
            sb.Append(GetFieldsWithTypesForTableCreation(entityType));
            sb.Append($"{Environment.NewLine});");

            return sb.ToString();
        }

        public string GetCreateRevisionTableSql(Type entityType)
        {
            var sb = new StringBuilder();
            sb.Append($"{PostgreSQLConstants.CREATE_TABLE_IF_NOT_EXISTS} {_entityUtils.GetTableFullName(entityType)}_revision{Environment.NewLine}");
            sb.Append($"({Environment.NewLine}");
            sb.Append(GetFieldsWithTypesForTableCreation(entityType, true));
            sb.Append($"{Environment.NewLine});");

            return sb.ToString();
        }

        #region Stored Procedure and Function Generators
        protected override void PrepareInsertProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_INSERT_POSTFIX, GetInsertStoredProcedureSql);
            }
        }

        protected override void PrepareUpdateProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_UPDATE_POSTFIX, GetUpdateStoredProcedureSql);
            }
        }

        protected override void PrepareDeleteProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_DELETE_POSTFIX, GetDeleteStoredProcedureSql);
            }
        }

        protected override void PrepareUndoDeleteProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_UNDO_DELETE_POSTFIX, GetUndoDeleteStoredProcedureSql);
            }
        }

        protected override void PrepareHardDeleteProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_HARD_DELETE_POSTFIX, GetHardDeleteStoredProcedureSql);
            }
        }

        protected override void PrepareSelectByIdProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_SELECT_BY_ID_POSTFIX, GetSelectByIdStoredProcedureSql);
            }
        }

        protected override void PrepareRevisionsProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_SELECT_REVISIONS_POSTFIX, GetSelectRevisionsStoredProcedureSql);
            }
        }

        protected override void PrepareSaveRevisionProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_SAVE_REVISION_POSTFIX, GetSaveRevisionStoredProcedureSql);
            }
        }

        protected override void PrepareRestoreRevisionProcedures()
        {
            for (var i = 0; i < _entityTypes.Count; i++)
            {
                var entityType = _entityTypes[i];
                PrepareStoredProcedure(entityType, SQLConstants.PROCEDURE_RESTORE_REVISION_POSTFIX, GetRestoreRevisionStoredProcedureSql);
            }
        }

        private ProcedureGenerationModel GetProcedureGenerationModel(Type entityType)
        {
            var model = new ProcedureGenerationModel(entityType, _entityUtils.GetSchemaName, NamingUtils.GetFieldNameFromPropertyName);
            return model;
        }

        private const string FULL_TABLE_NAME = "##FULL_TABLE_NAME##";
        private const string TABLE_NAME = "##TABLE_NAME##";
        private const string SELF_FIELDS_WITH_PREFIX_AND_TYPE = "##SELF_FIELDS_WITH_PREFIX_AND_TYPE##";
        private const string ALL_FIELDS_EXCEPT_ID = "##ALL_FIELDS_EXCEPT_ID##";
        private const string SELF_FIELDS_WITH_PREFIX = "##SELF_FIELDS_WITH_PREFIX##";
        private const string SELF_FIELDS_FOR_UPDATE = "##SELF_FIELDS_FOR_UPDATE##";
        private const string ALL_FIELDS_WITH_TYPE = "##ALL_FIELDS_WITH_TYPE##";
        private const string ALL_FIELDS_WITH_PREFIX = "##ALL_FIELDS_WITH_PREFIX##";
        private const string ALL_FIELDS_INCLUDING_REVISION_FIELDS = "##ALL_FIELDS_INCLUDING_REVISION_FIELDS##";
        private const string ALL_FIELDS_INCLUDING_REVISION_FIELDS_WITH_TYPE = "##ALL_FIELDS_INCLUDING_REVISION_FIELDS_WITH_TYPE##";
        private const string ALL_FIELDS_INCLUDING_REVISION_FIELDS_WITH_PREFIX = "##ALL_FIELDS_INCLUDING_REVISION_FIELDS_WITH_PREFIX##";
        private const string RELATED_NAME_UPDATES = "##RELATED_NAME_UPDATES##";
        private const string RELATED_NAME_UPDATES_FOR_RESTORE = "##RELATED_NAME_UPDATES_FOR_RESTORE##";

        private static string GetProcedureTemplate(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Templates." + name))
            using (var reader = new StreamReader(stream))
            {
                var readToEnd = reader.ReadToEnd();
                if (Environment.NewLine != "\r\n")
                {
                    readToEnd = readToEnd.Replace("\r\n", "\n");
                }

                return readToEnd;
            }
        }

        private string GetRelatedNameUpdateQueries(Type entityType, ProcedureGenerationModel model)
        {
            var sb = new StringBuilder();
            var relatedTypes = _entityUtils.GetRelatedEntityTypes(entityType);
            var tableName = _entityUtils.GetTableName(entityType);
            for (var i = 0; i < relatedTypes.Count; i++)
            {
                var relatedType = relatedTypes[i];
                var schemaName = _entityUtils.GetSchemaName(relatedType);
                var delimitedName = relatedType.Name.GetDelimitedName();

                sb.Append($"    {SQLConstants.UPDATE} {schemaName}.{delimitedName}{Environment.NewLine}");
                sb.Append($"    {SQLConstants.SET} {tableName}_name = {SQLConstants.NAME_PARAMETER_NAME}{Environment.NewLine}");
                sb.Append($"    {SQLConstants.WHERE} {model.IdFieldName} = {model.IdParameterName};{Environment.NewLine}{Environment.NewLine}");
            }

            return sb.ToString();
        }

        private string GetRelatedNameUpdateQueriesForRestore(Type entityType, ProcedureGenerationModel model)
        {
            var sb = new StringBuilder();
            var baseTableSchema = _entityUtils.GetSchemaName(entityType);
            var baseTableName = _entityUtils.GetTableName(entityType);

            var relatedTypes = _entityUtils.GetRelatedEntityTypes(entityType);
            for (var i = 0; i < relatedTypes.Count; i++)
            {
                var relatedType = relatedTypes[i];
                var schemaName = _entityUtils.GetSchemaName(relatedType);
                var delimitedName = relatedType.Name.GetDelimitedName();

                sb.Append($"    {SQLConstants.UPDATE} {schemaName}.{delimitedName}{Environment.NewLine}");
                sb.Append($"    {SQLConstants.SET} {baseTableName}_name = (SELECT bt.{baseTableName}_name FROM {baseTableSchema}.{baseTableName} bt WHERE bt.{baseTableName}_id = {SQLConstants.PARAMETER_PREFIX}{baseTableName}_id){Environment.NewLine}");
                sb.Append($"    {SQLConstants.WHERE} {model.IdFieldName} = {model.IdParameterName};{Environment.NewLine}{Environment.NewLine}");
            }

            return sb.ToString();
        }

        public string GetInsertStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_insert.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName)
                                 .Replace(SELF_FIELDS_WITH_PREFIX_AND_TYPE, GetFieldPartOfQuery(entityType, true, false, true, "prm_", false))
                                 .Replace(ALL_FIELDS_EXCEPT_ID, GetFieldPartOfQuery(entityType, false, false, false, "", false))
                                 .Replace(SELF_FIELDS_WITH_PREFIX, GetFieldPartOfQuery(entityType, true, false, false, "prm_", false));
            return script;
        }

        public string GetUpdateStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_update.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName)
                                 .Replace(SELF_FIELDS_WITH_PREFIX_AND_TYPE, GetFieldPartOfQuery(entityType, true, false, true, "prm_", false))
                                 .Replace(SELF_FIELDS_FOR_UPDATE, GetFieldPartOfQueryForUpdate(entityType, "prm_"))
                                 .Replace(RELATED_NAME_UPDATES, GetRelatedNameUpdateQueries(entityType, model));
            return script;
        }

        public string GetSelectByIdStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_select_by_id.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName)
                                 .Replace(ALL_FIELDS_WITH_TYPE, GetFieldPartOfQuery(entityType, false, false, true))
                                 .Replace(ALL_FIELDS_WITH_PREFIX, GetFieldPartOfQuery(entityType, false, false, false, "t."));
            return script;
        }

        public string GetDeleteStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_delete.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName);
            return script;
        }

        private string GetUndoDeleteStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_undo_delete.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName);
            return script;
        }

        private string GetHardDeleteStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_hard_delete.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName);
            return script;
        }

        private string GetSelectRevisionsStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_select_revisions.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName)
                                 .Replace(ALL_FIELDS_INCLUDING_REVISION_FIELDS_WITH_TYPE, GetFieldPartOfQuery(entityType, false, true, true))
                                 .Replace(ALL_FIELDS_INCLUDING_REVISION_FIELDS_WITH_PREFIX, GetFieldPartOfQuery(entityType, false, true, false, "t."));
            return script;
        }

        private string GetSaveRevisionStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_save_revision.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName)
                                 .Replace(ALL_FIELDS_INCLUDING_REVISION_FIELDS, GetFieldPartOfQuery(entityType))
                                 .Replace(ALL_FIELDS_WITH_PREFIX, GetFieldPartOfQuery(entityType, false, false, false, "t."));
            return script;
        }

        private string GetRestoreRevisionStoredProcedureSql(Type entityType)
        {
            var template = GetProcedureTemplate("sp_restore_revision.txt");

            var model = GetProcedureGenerationModel(entityType);

            var script = template.Replace(FULL_TABLE_NAME, model.TableFullName)
                                 .Replace(TABLE_NAME, model.TableName)
                                 .Replace(SELF_FIELDS_FOR_UPDATE, GetFieldPartOfQueryForUpdate(entityType, "rev."))
                                 .Replace(SELF_FIELDS_WITH_PREFIX, GetFieldPartOfQuery(entityType, true, false, false, "r.", false))
                                 .Replace(RELATED_NAME_UPDATES_FOR_RESTORE, GetRelatedNameUpdateQueriesForRestore(entityType, model));
            return script;
        }
        #endregion

        private string GetFieldTypeString(PropertyInfo propertyInfo)
        {
            return _typeLookup.GetSqlDbTypeName(propertyInfo.PropertyType).ToLowerInvariant();
        }

        private static string GetNotNullStringIfFieldNullable(PropertyInfo field)
        {
            return IsNullableField(field) ? string.Empty : " not null";
        }

        private static bool IsNullableField(PropertyInfo field)
        {
            return field.PropertyType.IsGenericType && field.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public string GetFieldPartOfQuery(Type entityType, bool isSelfFieldsOnly = false, bool isWithRevisionFields = true,
                                          bool isWithTypes = false, string prefix = "", bool isIncludingId = true)
        {
            var sb = new StringBuilder();
            var baseName = entityType.Name.GetDelimitedName();

            var space = isWithTypes ? "    " : string.Empty;
            var newLine = isWithTypes ? Environment.NewLine : string.Empty;

            if (!isSelfFieldsOnly)
            {
                if (isIncludingId)
                {
                    sb.Append($"{space}{prefix}{baseName}_id");
                    if (isWithTypes)
                    {
                        sb.Append($" bigint,{newLine}");
                    }
                    else
                    {
                        sb.Append($",{newLine}");
                    }
                }

                sb.Append($"{space}{prefix}{baseName}_uid");
                if (isWithTypes)
                {
                    sb.Append($" uuid,{newLine}");
                }
                else
                {
                    sb.Append($",{newLine}");
                }

                sb.Append($"{space}{prefix}{baseName}_name");
                if (isWithTypes)
                {
                    sb.Append($" text,{newLine}");
                }
                else
                {
                    sb.Append($",{newLine}");
                }
            }

            var baseProperties = _entityUtils.GetBaseProperties();

            var fields = _entityUtils.GetAllProperties(entityType);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldName = field.Name.GetDelimitedName();

                if (field.Name == "Id"
                    || field.Name == "Uid"
                    || field.Name == "Name")
                {
                    continue;
                }

                if (isSelfFieldsOnly
                    && baseProperties.ToList().Any(x => x.Name == field.Name))
                {
                    continue;
                }

                sb.Append($"{space}{prefix}{fieldName}");
                if (isWithTypes)
                {
                    if (field.Name == "IP")
                    {
                        sb.Append($" inet,{newLine}");
                    }
                    else
                    {
                        sb.Append($" {GetFieldTypeString(field)},{newLine}");
                    }
                }
                else
                {
                    sb.Append($",{newLine}");
                }
            }

            if (isWithRevisionFields)
            {
                sb.Append($"{space}{prefix}revision");
                if (isWithTypes)
                {
                    sb.Append($" integer,{newLine}");
                }
                else
                {
                    sb.Append($",{newLine}");
                }

                sb.Append($"{space}{prefix}revisioned_by");
                if (isWithTypes)
                {
                    sb.Append($" bigint,{newLine}");
                }
                else
                {
                    sb.Append($",{newLine}");
                }

                sb.Append($"{space}{prefix}revisioned_at");
                if (isWithTypes)
                {
                    sb.Append(" timestamp,");
                }
                else
                {
                    sb.Append(",");
                }
            }

            var lastComma = sb.ToString().LastIndexOf(',');
            sb.Remove(lastComma, 1);

            if (sb.ToString().EndsWith(Environment.NewLine))
            {
                sb.Remove(sb.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine == "\r\n" ? 2 : 1);
            }

            return sb.ToString();
        }

        public string GetFieldPartOfQueryForUpdate(Type entityType, string prefix)
        {
            var sb = new StringBuilder();

            var baseProperties = _entityUtils.GetBaseProperties();

            var fields = _entityUtils.GetAllProperties(entityType);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var fieldName = field.Name.GetDelimitedName();

                if (field.Name == "Id"
                    || field.Name == "Uid"
                    || field.Name == "Name"
                    || baseProperties.ToList().Any(x => x.Name == field.Name))
                {
                    continue;
                }

                sb.Append($"    {fieldName} = {prefix}{fieldName},{Environment.NewLine}");
            }

            var lastComma = sb.ToString().LastIndexOf(',');
            sb.Remove(lastComma, 1);

            if (sb.ToString().EndsWith(Environment.NewLine))
            {
                sb.Remove(sb.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine == "\r\n" ? 2 : 1);
            }

            return sb.ToString();
        }

        private StringBuilder GetFieldsWithTypesForTableCreation(Type entityType, bool isIncludingRevisionFields = false)
        {
            var sb = new StringBuilder();

            var tableName = entityType.Name.GetFieldNameFromPropertyName();
            if (isIncludingRevisionFields)
            {
                sb.Append($"    {tableName}_revision_id bigserial primary key,{Environment.NewLine}");
                sb.Append($"    {tableName}_id bigint not null,{Environment.NewLine}");
            }
            else
            {
                sb.Append($"    {tableName}_id bigserial primary key,{Environment.NewLine}");
            }

            sb.Append($"    {tableName}_uid uuid not null,{Environment.NewLine}");
            sb.Append($"    {tableName}_name text not null,{Environment.NewLine}");

            var fields = _entityUtils.GetAllProperties(entityType);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.PropertyType.IsArray)
                {
                    continue;
                }

                if (field.Name == "Id"
                    || field.Name == "Uid"
                    || field.Name == "Name")
                {
                    continue;
                }

                if (field.Name == "row"
                    || field.Name == "column")
                {
                    throw new ArgumentException(entityType.Name + " has not supported fieldname! (" + field.Name + ")");
                }

                var fieldName = _entityUtils.GetFieldNameFromPropertyName(field.Name);
                var notNullText = GetNotNullStringIfFieldNullable(field);

                if (field.Name == "IP")
                {
                    sb.Append($"    {fieldName} inet{notNullText},{Environment.NewLine}");
                }
                else
                {
                    sb.Append($"    {fieldName} {GetFieldTypeString(field)}{notNullText},{Environment.NewLine}");
                }
            }

            if (isIncludingRevisionFields)
            {
                sb.Append($"    revision        integer not null,{Environment.NewLine}");
                sb.Append($"    revisioned_by   bigint not null,{Environment.NewLine}");
                sb.Append($"    revisioned_at   timestamp not null,{Environment.NewLine}");
            }

            var lastComma = sb.ToString().LastIndexOf(',');
            sb.Remove(lastComma, 1);

            if (sb.ToString().EndsWith(Environment.NewLine))
            {
                sb.Remove(sb.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal), Environment.NewLine == "\r\n" ? 2 : 1);
            }

            return sb;
        }
    }
}