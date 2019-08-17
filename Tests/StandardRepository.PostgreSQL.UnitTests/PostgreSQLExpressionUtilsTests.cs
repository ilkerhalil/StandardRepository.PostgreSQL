using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using NUnit.Framework;
using Shouldly;
using StandardRepository.Models;
using StandardRepository.PostgreSQL.Helpers;
using StandardRepository.PostgreSQL.UnitTests.Base.Entities;
using StandardRepository.PostgreSQL.UnitTests.Base.Requests;

namespace StandardRepository.PostgreSQL.UnitTests
{
    [TestFixture]
    public class PostgreSQLExpressionUtilsTests
    {
        public PostgreSQLExpressionUtils SystemUnderTest { get; set; }

        [SetUp]
        public void run_before_every_test()
        {
            SystemUnderTest = new PostgreSQLExpressionUtils();
        }

        [TestCase(ExpressionType.And, true),
         TestCase(ExpressionType.AndAlso, true),
         TestCase(ExpressionType.Or, true),
         TestCase(ExpressionType.OrElse, true),
         TestCase(ExpressionType.Not, true),
         TestCase(ExpressionType.Add, false),
         TestCase(ExpressionType.Constant, false)]
        public void ExpressionUtils_IsNodeNeedingParentheses(ExpressionType nodeType, bool expected)
        {
            Assert.AreEqual(expected, SystemUnderTest.IsNodeNeedingParentheses(nodeType));
        }

        [TestCase(ExpressionType.And, "&"),
         TestCase(ExpressionType.AndAlso, "AND"),
         TestCase(ExpressionType.Equal, "="),
         TestCase(ExpressionType.GreaterThan, ">"),
         TestCase(ExpressionType.GreaterThanOrEqual, ">="),
         TestCase(ExpressionType.LessThan, "<"),
         TestCase(ExpressionType.LessThanOrEqual, "<="),
         TestCase(ExpressionType.Not, "NOT"),
         TestCase(ExpressionType.NotEqual, "<>"),
         TestCase(ExpressionType.Or, "|"),
         TestCase(ExpressionType.OrElse, "OR"),
         TestCase(ExpressionType.Convert, ""),
         TestCase(ExpressionType.ConvertChecked, "")]
        public void ExpressionUtils_NodeTypeToString(ExpressionType nodeType, string expected)
        {
            Assert.AreEqual(expected, SystemUnderTest.NodeTypeToString(nodeType));
        }

        [Test]
        public void ExpressionUtils_NodeTypeToString_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => { SystemUnderTest.NodeTypeToString(ExpressionType.Add); });
        }

        [Test]
        public void ExpressionUtils_GetField_int()
        {
            Expression<Func<Organization, int>> expression = x => x.ProjectCount;
            Assert.AreEqual("project_count", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_DateTime()
        {
            Expression<Func<Organization, DateTime>> expression = x => x.StartDate;
            Assert.AreEqual("start_date", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_String()
        {
            Expression<Func<Organization, string>> expression = x => x.Email;
            Assert.AreEqual("email", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_Id()
        {
            Expression<Func<Organization, long>> expression = x => x.Id;
            Assert.AreEqual("organization_id", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_Child_Id()
        {
            Expression<Func<Project, long>> expression = x => x.OrganizationId;
            Assert.AreEqual("organization_id", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_Uid()
        {
            Expression<Func<Organization, Guid>> expression = x => x.Uid;
            Assert.AreEqual("organization_uid", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_Child_Uid()
        {
            Expression<Func<Project, Guid>> expression = x => x.OrganizationUid;
            Assert.AreEqual("organization_uid", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_Name()
        {
            Expression<Func<Organization, string>> expression = x => x.Name;
            Assert.AreEqual("organization_name", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_Child_Name()
        {
            Expression<Func<Project, string>> expression = x => x.OrganizationName;
            Assert.AreEqual("organization_name", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_decimal()
        {
            Expression<Func<Project, decimal>> expression = x => x.Cost;
            Assert.AreEqual("cost", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_long_nullable()
        {
            Expression<Func<Project, long?>> expression = x => x.DeletedBy;
            Assert.AreEqual("deleted_by", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetField_DateTime_nullable()
        {
            Expression<Func<Project, DateTime?>> expression = x => x.DeletedAt;
            Assert.AreEqual("deleted_at", SystemUnderTest.GetField(expression.Body));
        }

        [Test]
        public void ExpressionUtils_GetConditions_DateTime()
        {
            Expression<Func<Project, bool>> expression = x => x.CreatedAt == DateTime.UtcNow;
            Thread.Sleep(123);

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("created_at = :varprm_date", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<DateTime>();
            ((DateTime)prmValue.Value).ShouldBeLessThan(DateTime.UtcNow);
        }

        [Test]
        public void ExpressionUtils_GetConditions_DateTime_Add_Days_Throws_NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                Expression<Func<Project, bool>> expression = x => x.CreatedAt == DateTime.UtcNow.AddDays(2);
                var parameters = new Dictionary<string, DbParameterInfo>();
                var filter = SystemUnderTest.GetConditions(expression.Body, parameters);
            });
        }

        [Test]
        public void ExpressionUtils_GetConditions_DateTime_variable()
        {
            var now = DateTime.UtcNow;
            Expression<Func<Project, bool>> expression = x => x.CreatedAt == now;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("created_at = :varprm_now", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<DateTime>();
            prmValue.Value.ShouldBe(now);
        }

        [Test]
        public void ExpressionUtils_GetConditions_decimal_variable()
        {
            var cost = new decimal(100);
            Expression<Func<Project, bool>> expression = x => x.Cost > cost;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("cost > :varprm_cost", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<decimal>();
            prmValue.Value.ShouldBe(cost);
        }

        [Test]
        public void ExpressionUtils_GetConditions_long_variable()
        {
            long orgId = 123;
            Expression<Func<Project, bool>> expression = x => x.OrganizationId < orgId;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("organization_id < :varprm_orgid", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<long>();
            prmValue.Value.ShouldBe(orgId);
        }

        [Test]
        public void ExpressionUtils_GetConditions_string_variable()
        {
            var orgName = "test";
            Expression<Func<Project, bool>> expression = x => x.OrganizationName == orgName;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("organization_name = :varprm_orgname", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<string>();
            prmValue.Value.ShouldBe(orgName);
        }

        [Test]
        public void ExpressionUtils_GetConditions_guid_variable()
        {
            var orgUid = Guid.NewGuid();
            Expression<Func<Project, bool>> expression = x => x.Uid == orgUid;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("project_uid = :varprm_orguid", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<Guid>();
            prmValue.Value.ShouldBe(orgUid);
        }

        [Test]
        public void ExpressionUtils_GetConditions_guid_nullable_variable()
        {
            var orgUid = Guid.NewGuid();
            Expression<Func<Project, bool>> expression = x => x.OwnerUid == orgUid;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("owner_uid = :varprm_orguid", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<Guid>();
            prmValue.Value.ShouldBe(orgUid);
        }

        [Test]
        public void ExpressionUtils_GetConditions_null_variable()
        {
            Expression<Func<Project, bool>> expression = x => x.OwnerUid == null;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            Assert.AreEqual("owner_uid = :varprm_owner_uid", filter);
            parameters.Count.ShouldBe(1);
            var prmValue = parameters.Values.First();
            prmValue.Value.ShouldBeOfType<DBNull>();
            prmValue.Value.ShouldBe(DBNull.Value);
        }

        [Test]
        public void ExpressionUtils_GetConditions_OR()
        {
            Expression<Func<Project, bool>> expression = x => x.IsDeleted && x.OrganizationId > 5 || x.Description == "asd";

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("((is_deleted) AND (organization_id > :varprm_organization_id)) OR (description = :varprm_description)");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_organization_id");
            parameters.Values.Select(x => x.Value).ShouldContain("asd");
            parameters.Values.Select(x => x.Value).ShouldContain(5L);
        }

        [Test]
        public void ExpressionUtils_GetConditions_AND()
        {
            Expression<Func<Project, bool>> expression = x => x.IsDeleted && x.OrganizationId > 5 && x.Description == "asd";

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("((is_deleted) AND (organization_id > :varprm_organization_id)) AND (description = :varprm_description)");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_organization_id");
            parameters.Values.Select(x => x.Value).ShouldContain("asd");
            parameters.Values.Select(x => x.Value).ShouldContain(5L);
        }

        [Test]
        public void ExpressionUtils_GetConditions_AND_OR()
        {
            Expression<Func<Project, bool>> expression = x => (x.IsActive && x.IsDeleted) || x.OrganizationId > 15 && x.Description == "dsa";

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("((is_active) AND (is_deleted)) OR ((organization_id > :varprm_organization_id) AND (description = :varprm_description))");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_organization_id");
            parameters.Values.Select(x => x.Value).ShouldContain("dsa");
            parameters.Values.Select(x => x.Value).ShouldContain(15L);
        }

        [Test]
        public void ExpressionUtils_GetConditions_OR_AND()
        {
            Expression<Func<Project, bool>> expression = x => (x.IsActive || x.IsDeleted) && 15 < x.OrganizationId && "dsa" == x.Description;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("(((is_active) OR (is_deleted)) AND (:varprm_organization_id < organization_id)) AND (:varprm_description = description)");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_organization_id");
            parameters.Values.Select(x => x.Value).ShouldContain("dsa");
            parameters.Values.Select(x => x.Value).ShouldContain(15L);
        }

        [Test]
        public void ExpressionUtils_GetConditions_OR_AND_other()
        {
            Expression<Func<Project, bool>> expression = x => (x.IsActive || x.IsDeleted) && x.Id < x.OrganizationId && "dsa" == x.Description;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("(((is_active) OR (is_deleted)) AND (project_id < organization_id)) AND (:varprm_description = description)");
            parameters.Count.ShouldBe(1);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Values.Select(x => x.Value).ShouldContain("dsa");
        }

        [Test]
        public void ExpressionUtils_GetConditions_entity_property()
        {
            var project = new Project();
            project.Id = 5;

            Expression<Func<Project, bool>> expression = x => (x.IsActive || x.IsDeleted) && x.OrganizationId == project.Id && "dsa" == x.Description;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("(((is_active) OR (is_deleted)) AND (organization_id = :varprm_project_id)) AND (:varprm_description = description)");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_project_id");
            parameters.Values.Select(x => x.Value).ShouldContain("dsa");
            parameters.Values.Select(x => x.Value).ShouldContain(project.Id);
        }

        [Test]
        public void ExpressionUtils_GetConditions_entity_property_other()
        {
            var organization = new Organization();
            organization.Id = 5;

            Expression<Func<Project, bool>> expression = x => (x.IsActive || x.IsDeleted) && x.OrganizationId == organization.Id && "dsa" == x.Description;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("(((is_active) OR (is_deleted)) AND (organization_id = :varprm_organization_id)) AND (:varprm_description = description)");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_organization_id");
            parameters.Values.Select(x => x.Value).ShouldContain("dsa");
            parameters.Values.Select(x => x.Value).ShouldContain(organization.Id);
        }

        [Test]
        public void ExpressionUtils_GetConditions_Field_and_Variable_Are_Same_Name()
        {
            var request = new ProjectCreateRequest();
            request.ProjectName = "test";
            var value = request.ProjectName;

            Expression<Func<Project, bool>> expression = x => "Url" == x.Url && x.Name == request.ProjectName;

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("(:varprm_url = url) AND (project_name = :varprm_project_name)");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_url");
            parameters.Keys.ShouldContain(":varprm_project_name");
            parameters.Values.Select(x => x.Value).ShouldContain("Url");
            parameters.Values.Select(x => x.Value).ShouldContain(value);
        }

        [Test]
        public void ExpressionUtils_GetConditions_Contains()
        {
            Expression<Func<Project, bool>> expression = x => x.IsDeleted && x.OrganizationId > 5 && x.Description.Contains("asd");

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("((is_deleted) AND (organization_id > :varprm_organization_id)) AND (LOWER(description) LIKE '%' || :varprm_description || '%')");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_organization_id");
            parameters.Values.Select(x => x.Value).ShouldContain("asd");
            parameters.Values.Select(x => x.Value).ShouldContain(5L);
        }

        [Test]
        public void ExpressionUtils_GetConditions_Name_Contains()
        {
            Expression<Func<Organization, bool>> expression = x => x.Name.Contains("test") || x.Description.Contains("other test");

            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe("(LOWER(organization_name) LIKE '%' || :varprm_organization_name || '%') OR (LOWER(description) LIKE '%' || :varprm_description || '%')");
            parameters.Count.ShouldBe(2);
            parameters.Keys.ShouldContain(":varprm_description");
            parameters.Keys.ShouldContain(":varprm_organization_name");
            parameters.Values.Select(x => x.Value).ShouldContain("test");
            parameters.Values.Select(x => x.Value).ShouldContain("other test");
        }

        [Test]
        public void ExpressionUtils_GetConditions_Constant()
        {
            Expression<Func<Project, bool>> expression = x => 5 > 6;
            var parameters = new Dictionary<string, DbParameterInfo>();
            var filter = SystemUnderTest.GetConditions(expression.Body, parameters);

            filter.ShouldBe(":varprm_constant");
            parameters.Count.ShouldBe(1);
            parameters.Keys.ShouldContain(":varprm_constant");
            parameters.Values.Select(x => x.Value).ShouldContain(false);
        }
    }
}
