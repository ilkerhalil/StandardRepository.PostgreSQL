using System;
using StandardRepository.Models.Entities;
using StandardRepository.Models.Entities.Schemas;

namespace StandardRepository.PostgreSQL.UnitTests.Base.Entities
{
    public class Organization : BaseEntity, ISchemaMain
    {
        public string Email { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSuperOrganization { get; set; }

        public int ProjectCount { get; set; }
        public DateTime StartDate { get; set; }

        public long LongField { get; set; }
    }
}