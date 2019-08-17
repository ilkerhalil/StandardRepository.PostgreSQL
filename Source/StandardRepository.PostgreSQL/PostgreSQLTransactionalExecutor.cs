using Npgsql;

using StandardRepository.PostgreSQL.Factories;

namespace StandardRepository.PostgreSQL
{
    public class PostgreSQLTransactionalExecutor : TransactionalExecutor<NpgsqlConnection, NpgsqlTransaction>
    {
        public PostgreSQLTransactionalExecutor(NpgsqlConnection connection) : base(connection)
        {
        }
    }
}