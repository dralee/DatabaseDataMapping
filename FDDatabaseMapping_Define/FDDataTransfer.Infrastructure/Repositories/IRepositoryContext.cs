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
        /// <summary>
        /// 执行插入并返回自增列
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        object ExecuteInsertQuery(string sql, string tableName);
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
        /// 根据指定列获取表中字符集
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="keyResult">返回集合中key</param>
        /// <param name="columns">指定列</param>
        /// <param name="condition">查询条件</param>
        /// <returns></returns>
        IDictionary<string, IDictionary<string, object>> Get(string tableName, string keyResult, IEnumerable<string> columns, string condition);

        /// <summary>
        /// 根据指定列获取表中字符集
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="keyResult">返回集合中key</param>
        /// <param name="checkKeyValue">key的值处理后作为key的值</param>
        /// <param name="columns">指定列</param>
        /// <param name="condition">查询条件</param>
        /// <returns></returns>
        IDictionary<string, IDictionary<string, object>> Get(string tableName, string keyResult, Func<string, string> checkKeyValue, IEnumerable<string> columns, string condition);

        /// <summary>
        /// 据sql获取数据
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="columns">如指定，则只返回指定列数据（不指定则为null，不要初始化）</param>
        /// <returns></returns>
        IEnumerable<IDictionary<string, object>> Get(string sql, IEnumerable<string> columns = null);

        /// <summary>
        /// 据sql获取数据
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="keyResult">返回集合中key</param>
        /// <param name="columns"></param>
        /// <returns></returns>
        IDictionary<string, IDictionary<string, object>> Get(string sql, string keyResult, IEnumerable<string> columns = null);

        /// <summary>
        /// 据sql获取数据
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="keyResult">返回集合中key</param>
        /// <param name="checkKeyValue">key的值处理后作为key的值</param>
        /// <param name="columns"></param>        
        /// <returns></returns>
        IDictionary<string, IDictionary<string, object>> Get(string sql, string keyResult, Func<string, string> checkKeyValue, IEnumerable<string> columns = null);

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="items">数据项</param>
        /// <param name="manyToOneKeys">数据项中包含多个值的字段名</param>
        void Execute(string tableName, IDictionary<string, object> items);

        /// <summary>
        /// 执行插入
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="items">数据项</param>
        /// <param name="manyToOneKeys">数据项中包含多个值的字段名</param>
        object ExecuteInsert(string tableName, IDictionary<string, object> items, string key = "Id");
        
        /// <summary>
        /// 开启事务
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// 提交事务
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// 事务回滚
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// 释放事务
        /// </summary>
        void DisposeTransaction();

        /// <summary>
        /// 关闭连接
        /// </summary>
        void Close();
    }
}
