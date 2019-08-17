using Npgsql;

using StandardRepository.Factories;
using StandardRepository.Models;

namespace StandardRepository.PostgreSQL.Factories
{
    public class PostgreSQLConnectionFactory : ConnectionFactory<NpgsqlConnection>
    {
        public PostgreSQLConnectionFactory(ConnectionSettings connectionSettings) : base(connectionSettings)
        {
        }

        public PostgreSQLConnectionFactory(string connectionString) : base(connectionString)
        {
        }
    }
}