using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using StandardRepository.Helpers;
using StandardRepository.PostgreSQL.DbGenerator;
using StandardRepository.PostgreSQL.Factories;
using StandardRepository.PostgreSQL.Helpers;
using StandardRepository.PostgreSQL.Helpers.SqlExecutor;
using StandardRepository.PostgreSQL.IntegrationTests.Base.Entities;
using StandardRepository.PostgreSQL.IntegrationTests.Base.Repositories;

namespace StandardRepository.PostgreSQL.IntegrationTests.Base
{
    public class PostgresqlBaseRepositoryIntegrationTests : BaseRepositoryIntegrationTests
    {
        [SetUp]
        public virtual async  Task Setup()
        {
            var dbName = GetTestDBName();
            await EnsureDbGenerated(dbName);
        }

        [TearDown]
        public async Task TearDown()
        {
            var dbName = GetTestDBName();
            await DropDb(dbName);
        }

        public string POSTGRES_DB_NAME => "postgres";

        public Organization GetOrganization()
        {
            var organization = new Organization
            {
                Name = "Org " + Guid.NewGuid(),
                Email = "email." + Guid.NewGuid() + "@email.com",
                IsActive = true,
                ProjectCount = 5
            };
            return organization;
            
        }

        public Organization GetOrganization(Organization entity)
        {
            var organization = new Organization
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                IsActive = entity.IsActive,
                ProjectCount = entity.ProjectCount
            };
            return organization;
        }

        public Project GetProject(Organization organization)
        {
            var project = new Project
            {
                Name = "Project " + Guid.NewGuid(),
                IsActive = true,
                OrganizationUid = organization.Uid,
                OrganizationId = organization.Id,
                OrganizationName = organization.Name
            };
            return project;
        }

        protected async Task<OrganizationRepository> GetOrganizationRepository()
        {
            var postgreSqlTypeLookup = GetTypeLookup();
            var entityUtils = GetEntityUtils(postgreSqlTypeLookup);
            var sqlExecutor = await GetSQLExecutor(GetTestDBName());
            var repository = new OrganizationRepository(postgreSqlTypeLookup, new PostgreSQLConstants<Organization>(entityUtils), entityUtils,
                                                        new PostgreSQLExpressionUtils(), sqlExecutor, new List<string>());

            return await Task.FromResult(repository);
        }

        public async  Task<ProjectRepository> GetProjectRepository()
        {
            var postgreSqlTypeLookup = GetTypeLookup();
            var entityUtils = GetEntityUtils(postgreSqlTypeLookup);
            var sqlExecutor = await GetSQLExecutor(GetTestDBName());
            var repository = new ProjectRepository(postgreSqlTypeLookup, new PostgreSQLConstants<Project>(entityUtils), entityUtils,
                                                   new PostgreSQLExpressionUtils(), sqlExecutor, new List<string>());

            return await Task.FromResult(repository);
        }

        public PostgreSQLTypeLookup GetTypeLookup()
        {
            return new PostgreSQLTypeLookup();
        }

        private async Task EnsureDbGenerated(string dbName)
        {
            var masterExecutor =await GetSQLExecutor(POSTGRES_DB_NAME);
            var isDbExist =  masterExecutor.ExecuteSqlReturningValue<bool>($"SELECT true FROM pg_database WHERE datname = '{dbName}';").Result;
            if (!isDbExist)
            {
                masterExecutor.ExecuteSql($"CREATE DATABASE {dbName};").Wait();
            }

            Sleep();

            if (!isDbExist)
            {
                var typeLookup = new PostgreSQLTypeLookup();
                var entityUtils = new EntityUtils(typeLookup, Assembly.GetExecutingAssembly());
                var executor = await GetSQLExecutor(dbName);
                var dbGenerator = new PostgreSQLDbGenerator(typeLookup, entityUtils, (PostgreSQLExecutor)masterExecutor, (PostgreSQLExecutor)executor);
                await dbGenerator.Generate();
            }
        }

        public async Task DropDb(string dbName)
        {
            Sleep();

            var utils = await GetSQLExecutor(POSTGRES_DB_NAME);

            utils.ExecuteSql($@"SELECT Pg_terminate_backend(pg_stat_activity.pid)
                                FROM   pg_stat_activity
                                WHERE  pg_stat_activity.datname = '{dbName}'
                                       AND pid <> Pg_backend_pid();

                                DROP DATABASE {dbName};").Wait();
        }

        private async Task<PostgreSQLExecutor> GetSQLExecutor(string dbName)
        {
            var typeLookup = new PostgreSQLTypeLookup();
            var entityUtils = new EntityUtils(typeLookup, Assembly.GetExecutingAssembly());
            var connectionSettings = await GetConnectionSettings(dbName);
            var sqlExecutor = new PostgreSQLExecutor(new PostgreSQLConnectionFactory(connectionSettings), entityUtils);
            return sqlExecutor;
        }
    }
}