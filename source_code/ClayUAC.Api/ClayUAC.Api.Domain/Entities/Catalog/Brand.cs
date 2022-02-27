﻿using AspNetCoreHero.Abstractions.Domain;

namespace ClayUAC.Api.Domain.Entities.Catalog
{
    public class Brand : AuditableEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Tax { get; set; }
    }
}