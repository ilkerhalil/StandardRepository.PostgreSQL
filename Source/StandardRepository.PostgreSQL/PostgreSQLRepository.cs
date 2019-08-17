using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Npgsql;
using NpgsqlTypes;

using StandardRepository.Helpers;
using StandardRepository.Models;
using StandardRepository.Models.Entities;
using StandardRepository.PostgreSQL.Helpers;
using StandardRepository.PostgreSQL.Helpers.SqlExecutor;

namespace StandardRepository.PostgreSQL
{
    public class PostgreSQLRepository<T> : StandardRepository<T, NpgsqlConnection, NpgsqlCommand, NpgsqlParameter> where T : BaseEntity, new()
    {
        public PostgreSQLRepository(PostgreSQLTypeLookup typeLookup, PostgreSQLConstants<T> sqlConstants, EntityUtils entityUtils,
                                    ExpressionUtils expressionUtils, PostgreSQLExecutor sqlExecutor, List<string> updateableFields) : base(typeLookup, sqlConstants, entityUtils,
                                                                                                                                           expressionUtils, sqlExecutor, updateableFields)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Fields.Length; i++)
            {
                var field = Fields[i];
                sb.Append($"{PostgreSQLConstants.PARAMETER_PRESIGN}{SQLConstants.PARAMETER_PREFIX}{field.Name.GetDelimitedName()},");
            }

            var lastComma = sb.ToString().LastIndexOf(',');
            sb.Remove(lastComma, 1);

            QueryInsert = $"{SQLConstants.SELECT} * {SQLConstants.FROM} {_sqlConstants.ProcedureNameInsert} (:{SQLConstants.UPDATED_BY_PARAMETER_NAME},:{SQLConstants.UID_PARAMETER_NAME},:{SQLConstants.NAME_PARAMETER_NAME},{sb});";
            QueryUpdate = $"{PostgreSQLConstants.CALL} {_sqlConstants.ProcedureNameUpdate} (:{SQLConstants.UPDATED_BY_PARAMETER_NAME},:{_sqlConstants.IdParameterName},:{SQLConstants.NAME_PARAMETER_NAME},{sb});";

            QueryDelete = $"{PostgreSQLConstants.CALL} {_sqlConstants.ProcedureNameDelete} (:{SQLConstants.UPDATED_BY_PARAMETER_NAME},:{_sqlConstants.IdParameterName});";
            QueryUndoDelete = $"{PostgreSQLConstants.CALL} {_sqlConstants.ProcedureNameUndoDelete} (:{SQLConstants.UPDATED_BY_PARAMETER_NAME},:{_sqlConstants.IdParameterName});";
            QueryHardDelete = $"{PostgreSQLConstants.CALL} {_sqlConstants.ProcedureNameHardDelete} (:{SQLConstants.UPDATED_BY_PARAMETER_NAME},:{_sqlConstants.IdParameterName});";

            QuerySelectById = $"{SQLConstants.SELECT} * {SQLConstants.FROM} {_sqlConstants.ProcedureNameSelectById} (:{_sqlConstants.IdParameterName});";

            QuerySelectRevisions = $"{SQLConstants.SELECT} * {SQLConstants.FROM} {_sqlConstants.ProcedureNameSelectRevisions} (:{_sqlConstants.IdParameterName});";
            QueryRestoreRevision = $"{PostgreSQLConstants.CALL} {_sqlConstants.ProcedureNameRestoreRevision} (:{SQLConstants.UPDATED_BY_PARAMETER_NAME},:{_sqlConstants.IdParameterName},:{SQLConstants.REVISION_PARAMETER_NAME}, null);";
        }

        public override void SetSqlExecutorForTransaction(NpgsqlConnection connection)
        {
            SQLExecutor.SetConnection(connection);
        }

        protected override void AppendWhere(Expression<Func<T, bool>> @where, List<NpgsqlParameter> parameters, StringBuilder sb, bool isIncludeDeleted)
        {
            if (where != null)
            {
                var prmDictionary = new Dictionary<string, DbParameterInfo>();
                var conditions = _expressionUtils.GetConditions(where.Body, prmDictionary);
                AddToParameters(parameters, prmDictionary);

                if (!isIncludeDeleted)
                {
                    sb.AppendLine($"{PostgreSQLConstants.WHERE_IS_NOT_DELETED_AND} {conditions}");
                }
                else
                {
                    sb.AppendLine($"{SQLConstants.WHERE} {conditions}");
                }
            }
            else
            {
                if (!isIncludeDeleted)
                {
                    sb.AppendLine($"{PostgreSQLConstants.WHERE_IS_NOT_DELETED}");
                }
            }
        }

        private static void AddToParameters(List<NpgsqlParameter> parameters, Dictionary<string, DbParameterInfo> prmDictionary)
        {
            foreach (var prm in prmDictionary)
            {
                var parameter = new NpgsqlParameter(prm.Value.Name, prm.Value.DbType) { Value = prm.Value.Value };
                parameters.Add(parameter);
            }
        }

        public override async Task<List<T>> SelectMany(Expression<Func<T, bool>> @where, int skip = 0, int take = 100,
                                                       Expression<Func<T, object>> orderByColumn = null, bool isAscending = true, bool isIncludeDeleted = false)
        {
            var sb = new StringBuilder();
            sb.Append($"{SQLConstants.SELECT} *{Environment.NewLine}");
            sb.Append($"{SQLConstants.FROM} {_sqlConstants.TableFullName}{Environment.NewLine}");

            var orderColumn = _sqlConstants.IdFieldName;
            if (orderByColumn != null)
            {
                orderColumn = _expressionUtils.GetField(orderByColumn.Body);
            }

            var ascOrDesc = SQLConstants.DESC;
            if (isAscending)
            {
                ascOrDesc = SQLConstants.ASC;
            }

            var parameters = new List<NpgsqlParameter>();
            var prmSkip = new NpgsqlParameter<int>(SQLConstants.SKIP_PARAMETER_NAME, NpgsqlDbType.Integer) { TypedValue = skip };
            parameters.Add(prmSkip);
            var prmTake = new NpgsqlParameter<int>(SQLConstants.TAKE_PARAMETER_NAME, NpgsqlDbType.Integer) { TypedValue = take };
            parameters.Add(prmTake);

            AppendWhere(where, parameters, sb, isIncludeDeleted);
            sb.Append($"{SQLConstants.ORDER_BY} {orderColumn} {ascOrDesc}{Environment.NewLine}");
            sb.Append($"{PostgreSQLConstants.LIMIT} {PostgreSQLConstants.PARAMETER_PRESIGN}{SQLConstants.TAKE_PARAMETER_NAME} {PostgreSQLConstants.OFFSET} {PostgreSQLConstants.PARAMETER_PRESIGN}{SQLConstants.SKIP_PARAMETER_NAME} {Environment.NewLine}");

            var items = await SQLExecutor.ExecuteSqlReturningEntityList<T>(sb.ToString(), parameters);
            return items;
        }

        public override async Task<List<T>> SelectAfter(Expression<Func<T, bool>> @where, long lastId, int take = 100,
                                                        Expression<Func<T, object>> orderByColumn = null, bool isAscending = true, bool isIncludeDeleted = false)
        {
            var sb = new StringBuilder();
            sb.Append($"{SQLConstants.SELECT} *{Environment.NewLine}");
            sb.Append($"{SQLConstants.FROM} {_sqlConstants.TableFullName}{Environment.NewLine}");

            var orderColumn = _sqlConstants.IdFieldName;
            if (orderByColumn != null)
            {
                orderColumn = _expressionUtils.GetField(orderByColumn.Body);
            }

            var ascOrDesc = SQLConstants.DESC;
            if (isAscending)
            {
                ascOrDesc = SQLConstants.ASC;
            }

            var parameters = new List<NpgsqlParameter>();
            var prmTake = new NpgsqlParameter<int>(SQLConstants.TAKE_PARAMETER_NAME, NpgsqlDbType.Integer) { TypedValue = take };
            parameters.Add(prmTake);

            AppendWhere(where, parameters, sb, isIncludeDeleted);

            sb.Append($" {SQLConstants.AND} {_sqlConstants.IdFieldName} > {lastId}{Environment.NewLine}");
            sb.Append($"{SQLConstants.ORDER_BY} {orderColumn} {ascOrDesc}{Environment.NewLine}");
            sb.Append($"{PostgreSQLConstants.LIMIT} {PostgreSQLConstants.PARAMETER_PRESIGN}{SQLConstants.TAKE_PARAMETER_NAME} {PostgreSQLConstants.OFFSET} 0{Environment.NewLine}");

            var items = await SQLExecutor.ExecuteSqlReturningEntityList<T>(sb.ToString(), parameters);
            return items;
        }

        public override async Task<List<T>> SelectAfter(Expression<Func<T, bool>> @where, Guid lastUid, int take = 100,
                                                        Expression<Func<T, object>> orderByColumn = null, bool isAscending = true, bool isIncludeDeleted = false)
        {
            var sb = new StringBuilder();
            sb.Append($"{SQLConstants.SELECT} *{Environment.NewLine}");
            sb.Append($"{SQLConstants.FROM} {_sqlConstants.TableFullName}{Environment.NewLine}");

            var orderColumn = _sqlConstants.IdFieldName;
            if (orderByColumn != null)
            {
                orderColumn = _expressionUtils.GetField(orderByColumn.Body);
            }

            var ascOrDesc = SQLConstants.DESC;
            if (isAscending)
            {
                ascOrDesc = SQLConstants.ASC;
            }

            var parameters = new List<NpgsqlParameter>();
            var prmTake = new NpgsqlParameter<int>(SQLConstants.TAKE_PARAMETER_NAME, NpgsqlDbType.Integer) { TypedValue = take };
            parameters.Add(prmTake);

            AppendWhere(where, parameters, sb, isIncludeDeleted);

            if (lastUid != Guid.Empty)
            {
                var prmLastUid = new NpgsqlParameter<Guid>(SQLConstants.LAST_UID_PARAMETER_NAME, NpgsqlDbType.Uuid) { TypedValue = lastUid };
                parameters.Add(prmLastUid);

                var schemaName = _entityUtils.GetSchemaName(typeof(T));
                var tableName = _entityUtils.GetTableName(typeof(T));

                if (where == null && isIncludeDeleted)
                {
                    sb.Append($"{SQLConstants.WHERE}");
                }
                else
                {
                    sb.Append($" {SQLConstants.AND}");
                }

                sb.Append($" {_sqlConstants.IdFieldName} > ({SQLConstants.SELECT} {tableName}_id {SQLConstants.FROM} {schemaName}.{tableName} {SQLConstants.WHERE} {tableName}_uid = {PostgreSQLConstants.PARAMETER_PRESIGN}{SQLConstants.LAST_UID_PARAMETER_NAME}){Environment.NewLine}");
            }

            sb.Append($"{SQLConstants.ORDER_BY} {orderColumn} {ascOrDesc}{Environment.NewLine}");
            sb.Append($"{PostgreSQLConstants.LIMIT} {PostgreSQLConstants.PARAMETER_PRESIGN}{SQLConstants.TAKE_PARAMETER_NAME} {PostgreSQLConstants.OFFSET} 0{Environment.NewLine}");

            var items = await SQLExecutor.ExecuteSqlReturningEntityList<T>(sb.ToString(), parameters);
            return items;
        }

        public override async Task<List<long>> SelectIds(Expression<Func<T, bool>> @where, bool isIncludeDeleted = false)
        {
            var sb = new StringBuilder();
            sb.Append($"{SQLConstants.SELECT} {_sqlConstants.IdFieldName}{Environment.NewLine}");
            sb.Append($"{SQLConstants.FROM} {_sqlConstants.TableFullName}{Environment.NewLine}");

            if (isIncludeDeleted)
            {
                sb.Append($"{SQLConstants.WHERE} {_entityUtils.GetFieldNameFromPropertyName(nameof(BaseEntity.IsDeleted))} = true");
            }
            else
            {
                sb.Append($"{SQLConstants.WHERE} {_entityUtils.GetFieldNameFromPropertyName(nameof(BaseEntity.IsDeleted))} = false");
            }

            var parameters = new List<NpgsqlParameter>();

            if (where != null)
            {
                var prmDictionary = new Dictionary<string, DbParameterInfo>();
                var conditions = _expressionUtils.GetConditions(where.Body, prmDictionary);
                AddToParameters(parameters, prmDictionary);

                sb.Append($" AND {conditions}");
            }

            var result = await SQLExecutor.ExecuteSqlReturningList<long>(sb.ToString(), parameters);
            return result;
        }

        public override async Task<List<T>> SelectAll(Expression<Func<T, bool>> @where, Expression<Func<T, object>> orderByColumn = null, bool isAscending = true, bool isIncludeDeleted = false)
        {
            var sb = new StringBuilder();
            sb.Append($"{SQLConstants.SELECT} *{Environment.NewLine}");
            sb.Append($"{SQLConstants.FROM} {_sqlConstants.TableFullName}{Environment.NewLine}");

            var orderColumn = _sqlConstants.IdFieldName;
            if (orderByColumn != null)
            {
                orderColumn = _expressionUtils.GetField(orderByColumn.Body);
            }

            var ascOrDesc = SQLConstants.DESC;
            if (isAscending)
            {
                ascOrDesc = SQLConstants.ASC;
            }

            var parameters = new List<NpgsqlParameter>();

            AppendWhere(where, parameters, sb, isIncludeDeleted);
            sb.Append($"{SQLConstants.ORDER_BY} {orderColumn} {ascOrDesc}{Environment.NewLine}");

            var items = await SQLExecutor.ExecuteSqlReturningEntityList<T>(sb.ToString(), parameters);
            return items;
        }
    }
}