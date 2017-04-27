using System;
using System.Collections.Generic;
using System.Text;

namespace FDDatabaseMappingCore
{
    public static class DbConfig
    {
        public static readonly string SqlServerConnString = @"Server=(localdb)\mssqllocaldb;Database=MyDatabase;Trusted_Connection=True;";
        public static readonly string MySqlConnString = @"Server=(localdb)\mssqllocaldb;Database=MyDatabase;Trusted_Connection=True;";
    }
}
