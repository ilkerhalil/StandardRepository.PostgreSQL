﻿using System;

namespace StandardRepository.PostgreSQL.UnitTests.Base.Requests
{
    public class ProjectCreateRequest
    {
        public Guid OrganizationUid { get; set; }
        public string ProjectName { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
    }
}