using Npgsql;

using StandardRepository.Factories;
using StandardRepository.Helpers;
using StandardRepository.Helpers.SqlExecutor;
using StandardRepository.PostgreSQL.Factories;

namespace StandardRepository.PostgreSQL.Helpers.SqlExecutor
{
    public class PostgreSQLExecutor : SQLExecutor<NpgsqlConnection, NpgsqlCommand, NpgsqlParameter>
    {
        public PostgreSQLExecutor(IConnectionFactory<NpgsqlConnection> connectionFactory, EntityUtils entityUtils) : base(connectionFactory, entityUtils)
        {

        }

        public PostgreSQLExecutor(PostgreSQLConnectionFactory connectionFactory, EntityUtils entityUtils) : base(connectionFactory, entityUtils)
        {

        }
    }
}