using System;
using System.Threading.Tasks;
using System.Transactions;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using StandardRepository.PostgreSQL.Factories;
using StandardRepository.PostgreSQL.IntegrationTests.Base;

namespace StandardRepository.PostgreSQL.IntegrationTests
{
    [TestFixture]
    public class PostgreSQLTransactionalExecutorIntegrationTests : PostgresqlBaseRepositoryIntegrationTests
    {
        [Test]
        public async Task Commits()
        {
            // arrange
            var transactionalExecutor = await GetPostgreSQLTransactionalExecutor();
            var organizationRepository = await GetOrganizationRepository();
            var projectRepository = await GetProjectRepository();

            var organization = GetOrganization();
            var project = GetProject(organization);

            // act
            await transactionalExecutor.ExecuteAsync<bool>(async cnn =>
            {
                organizationRepository.SetSqlExecutorForTransaction(cnn);
                projectRepository.SetSqlExecutorForTransaction(cnn);
                var orgIdOther =   organizationRepository.Insert(1, organization).Result;
                var projectIdOther = projectRepository.Insert(1, project).Result;

                return await Task.FromResult(true);
            });

            // assert
            organizationRepository.Count().Result.ShouldBe(1);
            projectRepository.Count().Result.ShouldBe(1);
        }

        [Test]
        public async Task Rollbacks()
        {
            // arrange
            var transactionalExecutor = await GetPostgreSQLTransactionalExecutor();
            var organizationRepository = await GetOrganizationRepository();
            var projectRepository = await GetProjectRepository();

            var organization = GetOrganization();
            var project = GetProject(organization);

            try
            {
                // act
                var result = await transactionalExecutor.ExecuteAsync<bool>(cnn =>
                {
                    organizationRepository.SetSqlExecutorForTransaction(cnn);
                    projectRepository.SetSqlExecutorForTransaction(cnn);

                    var unused = organizationRepository.Insert(1, organization).Result;

                    throw new TransactionAbortedException();
                });
            }
            catch (Exception e)
            {
                e.ShouldBeOfType<AggregateException>();
                e.InnerException.ShouldBeOfType<TransactionAbortedException>();
            }

            // assert
            organizationRepository.Count().Result.ShouldBe(0);
            projectRepository.Count().Result.ShouldBe(0);
        }

        private async Task<PostgreSQLTransactionalExecutor> GetPostgreSQLTransactionalExecutor()
        {
            var connectionSettings = await GetConnectionSettings(GetTestDBName());
            var npgsqlConnection =
                new NpgsqlConnection(PostgreSQLConnectionFactory.GetConnectionString(connectionSettings));
            var transactionalExecutor = new PostgreSQLTransactionalExecutor(npgsqlConnection);
            return await Task.FromResult(transactionalExecutor);
        }
    }
}