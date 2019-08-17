using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;
using StandardRepository.Helpers;
using StandardRepository.Models.Entities;
using StandardRepository.PostgreSQL.IntegrationTests.Base;
using StandardRepository.PostgreSQL.IntegrationTests.Base.Entities;

namespace StandardRepository.PostgreSQL.IntegrationTests
{
    public class PostgreSQLRepositoryIntegrationTests : PostgresqlBaseRepositoryIntegrationTests
    {
        [Test]
        public async Task Repository_Insert_and_SelectById()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();

            // act
            var id =  await repository.Insert(CURRENT_USER_ID, entity);

            // assert
            var result = await repository.SelectById(id);

            entity.Id.ShouldBe(id);
            result.Name.ShouldBe(entity.Name);
            result.Email.ShouldBe(entity.Email);
            result.IsActive.ShouldBe(entity.IsActive);
            result.ProjectCount.ShouldBe(entity.ProjectCount);

            AssertCreated(result);
            AssertUpdateFieldsNull(result);
            AssertDeleteFieldsNull(result);
        }

        [Test]
        public async Task Repository_Update()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();
            var id = await repository.Insert(CURRENT_USER_ID, entity);
            entity = await repository.SelectById(id);
            entity.IsSuperOrganization = true;
            entity.Description = "test description " + Guid.NewGuid();
            entity.Name = "updated name";

            // act
            var result = await repository.Update(CURRENT_USER_ID, entity);

            // assert
            result.ShouldBe(true);

            var updatedEntity = await repository.SelectById(id);

            updatedEntity.Name.ShouldBe(entity.Name);
            updatedEntity.Email.ShouldBe(entity.Email);
            updatedEntity.IsActive.ShouldBe(entity.IsActive);
            updatedEntity.ProjectCount.ShouldBe(entity.ProjectCount);
            updatedEntity.Description.ShouldBe(entity.Description);
            updatedEntity.IsSuperOrganization.ShouldBe(entity.IsSuperOrganization);

            AssertCreated(updatedEntity);
            AssertUpdated(updatedEntity);
            AssertDeleteFieldsNull(updatedEntity);
        }

        [Test]
        public async Task Repository_Delete()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();
            var id = await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Delete(CURRENT_USER_ID, id);

            // assert
            result.ShouldBe(true);

            var deletedEntity = await repository.Select(x => x.Id == id);
            deletedEntity.IsNotExist().ShouldBeTrue();

            var deletedEntity2 = await repository.Select(x => x.Id == id, true);
            AssertDeleted(deletedEntity2);
        }

        [Test]
        public async Task Repository_UndoDelete()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();
            var id = await repository.Insert(CURRENT_USER_ID, entity);
            await repository.Delete(CURRENT_USER_ID, id);
            var deletedEntity = await repository.Select(x => x.Id == id, true);
            AssertDeleted(deletedEntity);

            // act
            var result = await repository.UndoDelete(CURRENT_USER_ID, id);

            // assert
            result.ShouldBe(true);

            deletedEntity = await repository.Select(x => x.Id == id);
            AssertDeleteFieldsNull(deletedEntity);
        }

        [Test]
        public async Task Repository_Count()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var theCount = 10;

            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            var result = await repository.Count();

            // assert
            result.ShouldBe(theCount);

            // arrange again
            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act again
            result = await repository.Count();

            // assert
            result.ShouldBe(theCount + theCount);
        }

        [Test]
        public async Task Repository_Any_True()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Any();

            // assert
            result.ShouldBe(true);
        }

        [Test]
        public async Task Repository_Any_False()
        {
            // arrange
            var repository =await GetOrganizationRepository();
            var entity = GetOrganization();
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Any(x => x.IsSuperOrganization);

            // assert
            result.ShouldBe(false);
        }

        [Test]
        public async Task Repository_Max_Long()
        {
            // arrange
            var repository =await GetOrganizationRepository();
            var entity = GetOrganization();
            entity.LongField = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.LongField = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.LongField = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Max(x => x.Id > 0, x => x.LongField);

            // assert
            result.ShouldBe(456);
        }

        [Test]
        public async Task Repository_Max_Int()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();
            entity.ProjectCount = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.ProjectCount = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.ProjectCount = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Max(x => x.Id > 0, x => x.ProjectCount);

            // assert
            result.ShouldBe(456);
        }

        [Test]
        public async Task Repository_Max_Decimal()
        {
            // arrange
            var repository = await GetProjectRepository();
            var entity = GetProject(GetOrganization());
            entity.Cost = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.Cost = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.Cost = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Max(x => x.Id > 0, x => x.Cost);

            // assert
            result.ShouldBe(456);
        }

        [Test]
        public async Task Repository_Min_Long()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();
            entity.LongField = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.LongField = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.LongField = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Min(x => x.Id > 0, x => x.LongField);

            // assert
            result.ShouldBe(100);
        }

        [Test]
        public async Task Repository_Min_Int()
        {
            // arrange
            var repository =  await GetOrganizationRepository();
            var entity = GetOrganization();
            entity.ProjectCount = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.ProjectCount = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.ProjectCount = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Min(x => x.Id > 0, x => x.ProjectCount);

            // assert
            result.ShouldBe(100);
        }

        [Test]
        public async Task Repository_Min_Decimal()
        {
            // arrange
            var repository =await GetProjectRepository();
            var entity = GetProject(GetOrganization());
            entity.Cost = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.Cost = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.Cost = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Min(x => x.Id > 0, x => x.Cost);

            // assert
            result.ShouldBe(100);
        }

        [Test]
        public async Task Repository_Sum_Long()
        {
            // arrange
            var repository =await GetOrganizationRepository();
            var entity = GetOrganization();
            entity.LongField = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.LongField = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.LongField = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Sum(x => x.LongField > 105, x => x.LongField);

            // assert
            result.ShouldBe(123 + 456);
        }

        [Test]
        public async Task Repository_Sum_Int()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var entity = GetOrganization();
            entity.ProjectCount = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.ProjectCount = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.ProjectCount = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Sum(x => x.ProjectCount > 105, x => x.ProjectCount);

            // assert
            result.ShouldBe(123 + 456);
        }

        [Test]
        public async Task Repository_Sum_Decimal()
        {
            // arrange
            var repository = await GetProjectRepository();
            var entity = GetProject(GetOrganization());
            entity.Cost = 100;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.Cost = 123;
            await repository.Insert(CURRENT_USER_ID, entity);

            entity.Cost = 456;
            await repository.Insert(CURRENT_USER_ID, entity);

            // act
            var result = await repository.Sum(x => x.Cost > 105, x => x.Cost);

            // assert
            result.ShouldBe(123 + 456);
        }

        [Test]
        public async Task Repository_Select()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var theCount = 10;

            Organization entity = null;
            for (var i = 0; i < theCount; i++)
            {
                entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            entity.ShouldNotBeNull();
            var result = await repository.Select(x => x.Name == entity.Name);

            // assert
            result.Name.ShouldBe(entity.Name);
            result.Email.ShouldBe(entity.Email);
            result.IsActive.ShouldBe(entity.IsActive);
            result.ProjectCount.ShouldBe(entity.ProjectCount);

            AssertCreated(result);
            AssertUpdateFieldsNull(result);
            AssertDeleteFieldsNull(result);
        }

        [Test]
        public async Task Repository_SelectIds()
        {
            // arrange
            var repository =await GetOrganizationRepository();
            var theCount = 10;
            var filter = 3;

            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            var result = await repository.SelectIds(x => x.Id > filter);

            // assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(theCount - filter);

            for (var i = 0; i < result.Count; i++)
            {
                var id = result[i];
                id.ShouldBeLessThan(theCount + 1);
                id.ShouldBeGreaterThan(filter);
            }
        }

        [Test]
        public async Task Repository_SelectAfter()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var theCount = 10;
            var filter = 3;
            var lastId = 5;

            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            var items = await repository.SelectAfter(x => x.Id > filter, lastId);

            // assert
            items.ShouldNotBeNull();
            items.Count.ShouldBe(5);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Id.ShouldBeLessThan(theCount + 1);
                item.Id.ShouldBeGreaterThan(lastId);
            }
        }

        [Test]
        public async Task Repository_SelectAfter_2()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var theCount = 10;
            var filter = 3;
            var lastId = 5;

            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            var items = await repository.SelectAfter(x => x.Id > filter && x.IsActive, lastId, 2);

            // assert
            items.ShouldNotBeNull();
            items.Count.ShouldBe(2);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Id.ShouldBeLessThan(theCount + 1);
                item.Id.ShouldBeGreaterThan(lastId);
            }
        }

        [Test]
        public async Task Repository_SelectMany()
        {
            // arrange
            var repository =  await GetOrganizationRepository();
            var theCount = 10;
            var filter = 3;

            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            var items = await repository.SelectMany(x => x.Id > filter && x.IsActive);

            // assert
            items.ShouldNotBeNull();
            items.Count.ShouldBe(7);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Id.ShouldBeLessThan(theCount + 1);
                item.Id.ShouldBeGreaterThan(filter);
            }
        }

        [Test]
        public async Task Repository_SelectMany_2()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var theCount = 10;
            var filter = 3;

            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            var items = await repository.SelectMany(x => x.Id > filter && x.IsActive, 2, 2, x => x.Id);

            // assert
            items.ShouldNotBeNull();
            items.Count.ShouldBe(2);

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Id.ShouldBeLessThan(theCount + 1);
                item.Id.ShouldBeGreaterThan(5);
            }
        }

        [Test]
        public async Task Repository_SelectMany_2_descending()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var theCount = 10;
            var filter = 3;

            for (var i = 0; i < theCount; i++)
            {
                var entity = GetOrganization();
                await repository.Insert(CURRENT_USER_ID, entity);
            }

            // act
            var items = await repository.SelectMany(x => x.Id > filter && x.IsActive, 2, 2, x => x.Id, false);

            // assert
            items.ShouldNotBeNull();
            items.Count.ShouldBe(2);

            items.First().Id.ShouldBe(8);
            items.Last().Id.ShouldBe(7);
        }

        [Test]
        public async Task Repository_SelectRevisions()
        {
            // arrange
            var repository = await GetOrganizationRepository();

            var entity = GetOrganization();
            var id = await repository.Insert(CURRENT_USER_ID, entity);
            entity.Id = id;

            var entities = new Dictionary<int, Organization>();
            entities.Add(1, entity);

            var theCount = 5;
            for (var i = 0; i < theCount; i++)
            {
                var updatingEntity = GetOrganization(entity);
                updatingEntity.Description = "test description " + Guid.NewGuid();
                await repository.Update(CURRENT_USER_ID, updatingEntity);
                entities.Add(i + 2, updatingEntity);
            }

            // act
            var revisions = await repository.SelectRevisions(id);

            // assert
            revisions.ShouldNotBeNull();
            revisions.Count.ShouldBe(theCount);

            var revisionNumber = 1;
            for (var i = 0; i < revisions.Count; i++)
            {
                var entityRevision = revisions[i];
                entityRevision.Revision.ShouldBe(revisionNumber);
                revisionNumber++;

                entityRevision.RevisionedAt.ShouldNotBeNull();
                entityRevision.RevisionedBy.ShouldNotBeNull();

                var oldEntity = entities[entityRevision.Revision];
                var updatedEntity = entityRevision.Entity;

                updatedEntity.Name.ShouldBe(oldEntity.Name);
                updatedEntity.Email.ShouldBe(oldEntity.Email);
                updatedEntity.IsActive.ShouldBe(oldEntity.IsActive);
                updatedEntity.ProjectCount.ShouldBe(oldEntity.ProjectCount);
                updatedEntity.Description.ShouldBe(oldEntity.Description ?? string.Empty);
                updatedEntity.IsSuperOrganization.ShouldBe(oldEntity.IsSuperOrganization);

                AssertCreated(updatedEntity);
                AssertDeleteFieldsNull(updatedEntity);

                if (entityRevision.Revision > 1)
                {
                    AssertUpdated(updatedEntity);
                }
                else
                {
                    AssertUpdateFieldsNull(updatedEntity);
                }
            }
        }

        [Test]
        public async Task Repository_Revision()
        {
            // arrange
            var repository = await GetOrganizationRepository();

            var entity = GetOrganization();
            var id = await repository.Insert(CURRENT_USER_ID, entity);
            entity.Id = id;

            var entities = new Dictionary<int, Organization>();
            entities.Add(1, entity);

            var theCount = 5;
            var theRevision = 3;
            for (var i = 0; i < theCount; i++)
            {
                var updatingEntity = GetOrganization(entity);
                updatingEntity.Description = "test description " + Guid.NewGuid();
                await repository.Update(CURRENT_USER_ID, updatingEntity);
                entities.Add(i + 2, updatingEntity);
            }

            var oldRevisions = await repository.SelectRevisions(id);

            // act
            var result = await repository.RestoreRevision(CURRENT_USER_ID, id, theRevision);

            // assert
            result.ShouldBeTrue();

            var revisions = await repository.SelectRevisions(id);
            revisions.Count.ShouldBe(oldRevisions.Count + 1);

            var revisionEntity = revisions.FirstOrDefault(x => x.Revision == theRevision);
            revisionEntity.ShouldNotBeNull();

            var restoredEntity = await repository.SelectById(id);

            revisionEntity.Entity.Name.ShouldBe(restoredEntity.Name);
            revisionEntity.Entity.Email.ShouldBe(restoredEntity.Email);
            revisionEntity.Entity.IsActive.ShouldBe(restoredEntity.IsActive);
            revisionEntity.Entity.ProjectCount.ShouldBe(restoredEntity.ProjectCount);
            revisionEntity.Entity.Description.ShouldBe(restoredEntity.Description);
            revisionEntity.Entity.IsSuperOrganization.ShouldBe(restoredEntity.IsSuperOrganization);

            AssertCreated(restoredEntity);
            AssertUpdated(restoredEntity);
            AssertDeleteFieldsNull(restoredEntity);
        }

        [Test]
        public async Task Repository_Parallel_For()
        {
            // arrange
            var repository = await GetOrganizationRepository();
            var theCount = 100;

            // act
            Parallel.For(0, theCount, async x =>
            {
                var entity = GetOrganization();
                entity.ProjectCount += 5;
                await repository.Insert(CURRENT_USER_ID, entity);
            });

            // assert
            var result = await repository.Count();
            result.ShouldBe(theCount);
        }

        #region Assertions

        protected static void AssertDeleteFieldsNull(BaseEntity actualEntity)
        {
            actualEntity.IsDeleted.ShouldBe(false);
            actualEntity.DeletedAt.ShouldBeNull();
            actualEntity.DeletedBy.ShouldBeNull();
        }

        protected static void AssertDeleted(BaseEntity actualEntity)
        {
            actualEntity.IsDeleted.ShouldBe(true);
            actualEntity.DeletedAt.ShouldNotBeNull();
            actualEntity.DeletedBy.ShouldNotBeNull();
        }

        protected static void AssertUpdateFieldsNull(BaseEntity actualEntity)
        {
            actualEntity.UpdatedAt.ShouldBeNull();
            actualEntity.UpdatedBy.ShouldBeNull();
        }

        protected static void AssertUpdated(BaseEntity actualEntity)
        {
            actualEntity.UpdatedAt.ShouldNotBeNull();
            actualEntity.UpdatedBy.ShouldNotBeNull();
        }

        protected static void AssertCreated(BaseEntity actualEntity)
        {
            actualEntity.CreatedAt.ShouldNotBeNull();
            actualEntity.CreatedBy.ShouldNotBeNull();
        }

        #endregion
    }
    
}