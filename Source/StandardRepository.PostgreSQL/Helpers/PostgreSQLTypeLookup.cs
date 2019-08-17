using System;
using System.Collections.Generic;

using NpgsqlTypes;

using StandardRepository.Helpers;

namespace StandardRepository.PostgreSQL.Helpers
{
    public class PostgreSQLTypeLookup : TypeLookup
    {
        private static readonly IReadOnlyDictionary<Type, string> _types = new Dictionary<Type, string>()
        {
            [typeof(int)] = NpgsqlDbType.Integer.ToString(),
            [typeof(int?)] = NpgsqlDbType.Integer.ToString(),
            [typeof(long)] = NpgsqlDbType.Bigint.ToString(),
            [typeof(long?)] = NpgsqlDbType.Bigint.ToString(),
            [typeof(decimal)] = NpgsqlDbType.Numeric.ToString(),
            [typeof(decimal?)] = NpgsqlDbType.Numeric.ToString(),
            [typeof(bool)] = NpgsqlDbType.Boolean.ToString(),
            [typeof(bool?)] = NpgsqlDbType.Boolean.ToString(),
            [typeof(char)] = NpgsqlDbType.Text.ToString(),
            [typeof(char?)] = NpgsqlDbType.Text.ToString(),
            [typeof(string)] = NpgsqlDbType.Text.ToString(),
            [typeof(Guid)] = NpgsqlDbType.Uuid.ToString(),
            [typeof(Guid?)] = NpgsqlDbType.Uuid.ToString(),
            [typeof(DateTime)] = NpgsqlDbType.Timestamp.ToString(),
            [typeof(DateTime?)] = NpgsqlDbType.Timestamp.ToString()
        };

        public override string GetSqlDbTypeName(Type type) => _types[type];
    }
}