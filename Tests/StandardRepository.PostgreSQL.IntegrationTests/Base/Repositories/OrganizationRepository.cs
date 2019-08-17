using System.Collections.Generic;
using StandardRepository.Helpers;
using StandardRepository.PostgreSQL.Helpers;
using StandardRepository.PostgreSQL.Helpers.SqlExecutor;
using StandardRepository.PostgreSQL.IntegrationTests.Base.Entities;

namespace StandardRepository.PostgreSQL.IntegrationTests.Base.Repositories
{
    public class OrganizationRepository : PostgreSQLRepository<Organization>
    {
        public OrganizationRepository(PostgreSQLTypeLookup typeLookup, PostgreSQLConstants<Organization> sqlConstants, EntityUtils entityUtils, ExpressionUtils expressionUtils, PostgreSQLExecutor sqlExecutor, List<string> updateableFields) : base(typeLookup, sqlConstants, entityUtils, expressionUtils, sqlExecutor, updateableFields)
        {
        }
    }
}