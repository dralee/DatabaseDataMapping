using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Extensions;

namespace FDDatabaseMappingCore
{
    public class FDContext : DbContext
    {

        public FDContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer(Ser;
            //optionsBuilder.UseMySql
            base.OnConfiguring(optionsBuilder);
        }
    }
}
