using System.Collections.Generic;
using StandardRepository.Helpers;
using StandardRepository.PostgreSQL.Helpers;
using StandardRepository.PostgreSQL.Helpers.SqlExecutor;
using StandardRepository.PostgreSQL.UnitTests.Base.Entities;

namespace StandardRepository.PostgreSQL.UnitTests.Base.Repositories
{
    public class ProjectRepository : PostgreSQLRepository<Project>
    {
        public ProjectRepository(PostgreSQLTypeLookup typeLookup, PostgreSQLConstants<Project> sqlConstants, EntityUtils entityUtils, ExpressionUtils expressionUtils, PostgreSQLExecutor sqlExecutor, List<string> updateableFields) : base(typeLookup, sqlConstants, entityUtils, expressionUtils, sqlExecutor, updateableFields)
        {
        }
    }
}