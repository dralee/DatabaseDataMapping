using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public interface IRepositoryContext<T> where T : IEntity
    {
        string ConnString { get; }
        void Add(T item);
        void Update(T item);
        T Get(long id);
        IEnumerable<T> Get(Func<T, string> query);
        void Delete(long id);
        void Delete(T item);

        void Execute(string sql);
        DbDataReader ExecuteReader(string sql);
        object ExecuteScalar(string sql);
        /// <summary>
        /// 根据指定列获取表中字符集
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">指定列</param>
        /// <param name="condition">查询条件</param>
        /// <returns></returns>
        IEnumerable<IDictionary<string, object>> Get(string tableName, IEnumerable<string> columns, string condition);
        /// <summary>
        /// 执行插入
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="items"></param>
        void Execute(string tableName, IDictionary<string, string> items);
    }
}
