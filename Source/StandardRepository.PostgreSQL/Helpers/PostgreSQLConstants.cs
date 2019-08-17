using System;

using StandardRepository.Helpers;

namespace StandardRepository.PostgreSQL.Helpers
{
    public class PostgreSQLConstants : SQLConstants
    {
        public const string CREATE_TABLE_IF_NOT_EXISTS = "CREATE TABLE IF NOT EXISTS";
        public const string CREATE_OR_REPLACE_PROCEDURE = "CREATE OR REPLACE PROCEDURE";
        public const string CREATE_OR_REPLACE_FUNCTION = "CREATE OR REPLACE FUNCTION";
        public const string RETURNING = "RETURNING";
        public const string RETURN = "RETURN";
        public const string RETURNS = "RETURNS";
        public const string DECLARE = "DECLARE";
        public const string QUERY = "QUERY";
        public const string RETURNS_SETOF = "RETURNS SETOF";
        public const string RETURNS_TABLE = "RETURNS TABLE";
        public const string RETURN_ID = "return_id";
        public const string LANGUAGE_SQL = "LANGUAGE SQL";
        public const string LANGUAGE_PLPGSQL = "LANGUAGE 'plpgsql'";
        public const string AS = "AS";
        public const string CALL = "CALL";
        public const string OUT = "OUT";
        public const string LIMIT = "LIMIT";
        public const string OFFSET = "OFFSET";
        public const string IF_NOT_FOUND_THEN = "IF NOT FOUND THEN";
        public const string END_IF = "END IF";

        public const string WHERE_IS_NOT_DELETED = "WHERE is_deleted = false";
        public const string WHERE_IS_NOT_DELETED_AND = "WHERE is_deleted = false AND";

        public new const string PARAMETER_PRESIGN = ":";

        public PostgreSQLConstants(string schemaName, string tableName) : base(schemaName, tableName)
        {
        }

        public PostgreSQLConstants(Type entityType, EntityUtils entityUtils) : base(entityType, entityUtils)
        {
        }
    }

    public class PostgreSQLConstants<TEntity> : PostgreSQLConstants
    {
        public PostgreSQLConstants(EntityUtils entityUtils) : base(typeof(TEntity), entityUtils)
        {
        }
    }
}