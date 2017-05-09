using System;
using System.Data.SqlClient;
using System.Data.Common;
using FDDataTransfer.Infrastructure.Repositories;
using FDDataTransfer.Infrastructure.Entities.Basic;

namespace FDDataTransfer.SqlServer.Repositories
{
    public class SqlServerRepositoryContext<T> : RepositoryContextBase<T> where T : IEntity, new()
    {
        public override string ConnString { get; }

        public override Func<Type, string> TableName { get; } = type => { return type.Name; };
        public override DbConnection Connection { get; }
        public override Func<DbCommand> Command { get; } = () => new SqlCommand();

        public SqlServerRepositoryContext(string connString)
        {
            ConnString = connString;
            Connection = new SqlConnection(ConnString);
        }

        public override DbParameter CreateParameter(string name, object value)
        {
            return new SqlParameter(name, value);
        }
    }
}
