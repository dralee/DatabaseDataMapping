using FDDataTransfer.Infrastructure.Entities.Basic;
using FDDataTransfer.Infrastructure.Repositories;
using MySql.Data.MySqlClient;
using System;
using System.Data.Common;

namespace FDDataTransfer.SqlServer.Repositories
{
    public class MySqlRepositoryContext<T> : RepositoryContextBase<T> where T : IEntity, new()
    {
        public override string ConnString { get; }

        public override Func<Type, string> TableName { get; } = type => { return type.Name; };
        public override DbConnection Connection { get; }
        public override Func<DbCommand> Command { get; } = () => new MySqlCommand();

        public MySqlRepositoryContext(string connString)
        {
            ConnString = connString;
            Connection = new MySqlConnection(ConnString);
        }
    }
}
