using FDDataTransfer.Infrastructure.Entities.Basic;
using FDDataTransfer.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public abstract class RepositoryContextBase<T> : IRepositoryContext<T> where T : IEntity, new()
    {
        public abstract string ConnString { get; }

        public abstract Func<Type, string> TableName { get; }
        public abstract DbConnection Connection { get; }
        public abstract Func<DbCommand> Command { get; }

        public abstract DbParameter CreateParameter(string name, object value);

        public string GetSql(T item, string sqlFirst, string sqlLast, Func<PropertyInfo, string> sqlItem, Func<PropertyInfo, object, string> sqlValue)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            StringBuilder sb = new StringBuilder();
            sb.Append(sqlFirst);
            foreach (var p in properties)
            {
                sb.AppendFormat("{0},", sqlItem(p));
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(sqlLast);
            return sb.ToString();
        }

        protected string GetSqlInsert(T item)
        {
            var sqlFirst = $"INSERT INTO {TableName(typeof(T))}(";
            var sqlMiddle = " VALUES (";
            var sqlLast = ")";
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            sb.Append(sqlFirst);
            sb2.Append(sqlMiddle);
            foreach (var p in properties)
            {
                if (p.Name.Equals("Id")) // 去除Id项
                    continue;
                sb.AppendFormat("{0},", p.Name);
                sb2.AppendFormat("{0}", p.GetValue(item));
            }
            sb.Remove(sb.Length - 1, 1);
            sb2.Remove(sb.Length - 1, 1);
            sb2.Append(sqlLast);
            sb.Append(sqlLast).Append(sb2.ToString());
            return sb.ToString();
        }

        protected string GetSqlUpdate(T item, Func<T, string> condition)
        {
            var sqlFirst = $"UPDATE {TableName(typeof(T))} SET ";
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            StringBuilder sb = new StringBuilder();
            sb.Append(sqlFirst);
            foreach (var p in properties)
            {
                sb.AppendFormat("{0}={1},", p.Name, p.GetValue(item));
            }
            sb.Remove(sb.Length - 1, 1);
            sb.AppendFormat(" WHERE {0}", condition(item));
            return sb.ToString();
        }

        protected string GetSqlDelete(T item, Func<T, string> condition)
        {
            var sql = $"DELETE FROM {TableName(typeof(T))} WHERE {condition(item)}";
            return sql;
        }
        protected string GetSqlDelete(string condition)
        {
            var sql = $"DELETE FROM {TableName(typeof(T))} WHERE {condition}";
            return sql;
        }

        protected string GetSqlQuery(T item, Func<T, string> condition)
        {
            var sqlFirst = $"SELECT ";
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            StringBuilder sb = new StringBuilder();
            sb.Append(sqlFirst);
            foreach (var p in properties)
            {
                sb.AppendFormat("{0},", p.Name);
            }
            sb.Remove(sb.Length - 1, 1);
            sb.AppendFormat(" FROM {0}", TableName(typeof(T)));
            sb.AppendFormat(" WHERE {0}", condition(item));
            return sb.ToString();
        }

        private void Open()
        {
            if (Connection.State != ConnectionState.Open)
                Connection.Open();
        }

        private DbCommand GetCommand(string sql)
        {
            var cmd = Command();
            cmd.Connection = Connection;
            cmd.CommandText = sql;
            return cmd;
        }

        public void ExecuteNonQuery(string sql, DbParameter[] parameters = null)
        {
            Open();
            var cmd = GetCommand(sql);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
            Connection.Close();
        }

        /// <summary>
        /// 由于.net core中ado.net无效
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public object ExecuteInsertQuery(string sql, string tableName)
        {
            Open();
            var cmd = GetCommand(sql);
            string keySql = "SELECT LAST_INSERT_ID();";
            if (cmd.GetType().Name.Equals("MySqlCommand"))
                keySql = "SELECT LAST_INSERT_ID();";
            else if (cmd.GetType().Name.Equals("SqlCommand"))
            {
                keySql = "SELECT SCOPE_IDENTITY();";
            }
            else { }
            sql = $"{sql};{keySql}";

            var obj = cmd.ExecuteScalar(); // 居然不返回
            Connection.Close();
            return obj;
        }

        /// <summary>
        /// 由于.net core中ado.net无效，只针对sql server返回标识列
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public object ExecuteSqlInsert(string sql, string tableName, string key = "Id", DbParameter[] parameters = null)
        {
            Open();
            int index = sql.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
            sql = $"{sql.Substring(0, index)} OUTPUT INSERTED.{key} {sql.Substring(index)}";
            var cmd = GetCommand(sql);
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            var obj = cmd.ExecuteScalar();
            Connection.Close();
            return obj;
        }

        public DbDataReader ExecuteReader(string sql)
        {
            Open();
            var cmd = GetCommand(sql);
            return cmd.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public object ExecuteScalar(string sql)
        {
            Open();
            var cmd = GetCommand(sql);
            var obj = cmd.ExecuteScalar();
            Connection.Close();
            return obj;
        }

        public void Add(T item)
        {
            var sql = GetSqlInsert(item);
            ExecuteNonQuery(sql);
        }

        public void Delete(long id)
        {
            var sql = GetSqlDelete($"Id={id}");
            ExecuteNonQuery(sql);
        }

        public void Delete(T item)
        {
            var sql = GetSqlDelete(item, (t) => $"Id={item.Id}");
            ExecuteNonQuery(sql);
        }

        public T Get(long id)
        {
            var sql = GetSqlQuery(new T { Id = id }, (item) => $"Id={id}");
            using (DbDataReader reader = ExecuteReader(sql))
            {
                if (reader.Read())
                {
                    var item = new T();
                    SetValue(item, reader);
                    return item;
                }
            }
            return default(T);
        }

        private void SetValue(T item, DbDataReader reader)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var p in properties)
            {
                if (reader[p.Name] != null)
                {
                    p.SetValue(item, reader[p.Name]);
                }
            }
        }

        public IEnumerable<T> Get(Func<T, string> query)
        {
            var sql = GetSqlQuery(default(T), query);
            List<T> res = new List<T>();
            using (DbDataReader reader = ExecuteReader(sql))
            {
                while (reader.Read())
                {
                    var item = new T();
                    SetValue(item, reader);
                    res.Add(item);
                }
            }
            return res;
        }

        public void Update(T item)
        {
            string sql = GetSqlUpdate(item, p => $"Id={p.Id}");
            ExecuteNonQuery(sql);
        }

        public void Execute(string sql)
        {
            ExecuteNonQuery(sql);
        }

        public IEnumerable<IDictionary<string, object>> Get(string tableName, IEnumerable<string> columns, string condition)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            foreach (var c in columns)
            {
                sb.AppendFormat("{0},", c);
            }
            sb.Remove(sb.Length - 1, 1);
            sb.AppendFormat(" FROM {0}", tableName);
            if (!string.IsNullOrEmpty(condition))
            {
                sb.AppendFormat(" WHERE {0}", condition);
            }
            return Get(sb.ToString(), columns);
        }

        public IEnumerable<IDictionary<string, object>> Get(string sql, IEnumerable<string> columns = null)
        {
            using (DbDataReader reader = ExecuteReader(sql))
            {
                List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
                while (reader.Read())
                {
                    Dictionary<string, object> item = new Dictionary<string, object>();
                    if (columns != null)
                    {
                        foreach (var c in columns)
                        {
                            if (typeof(DateTime) == reader.GetFieldType(reader.GetOrdinal(c)))
                            {
                                item.Add(c, reader[c].ToDateTime(DateTime.Parse("1/1/1753 12:00:00 AM")));
                            }
                            else
                            {
                                item.Add(c, reader[c]);
                            }
                        }
                    }
                    else
                    {
                        for (var i = 0; i < reader.FieldCount; ++i)
                        {
                            if (typeof(DateTime) == reader.GetFieldType(i))
                            {
                                item.Add(reader.GetName(i), reader.GetValue(i).ToDateTime(DateTime.MinValue));
                            }
                            else
                            {
                                item.Add(reader.GetName(i), reader.GetValue(i));
                            }
                        }
                    }
                    res.Add(item);
                }
                return res;
            }
        }

        private string GetInsertSql(string tableName, IDictionary<string, object> items)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}(", tableName);
            sb2.Append(" VALUES (");
            foreach (var key in items.Keys)
            {
                sb.AppendFormat("{0},", key);
                sb2.AppendFormat("{0},", items[key]);
            }
            sb.Remove(sb.Length - 1, 1);
            sb2.Remove(sb2.Length - 1, 1);
            sb2.Append(")");
            sb.Append(")").Append(sb2.ToString());
            return sb.ToString();
        }

        private string GetInsertSql(string tableName, IDictionary<string, object> items, out DbParameter[] parameters)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            sb.AppendFormat("INSERT INTO {0}(", tableName);
            sb2.Append(" VALUES (");
            List<DbParameter> paras = new List<DbParameter>();
            foreach (var key in items.Keys)
            {
                sb.AppendFormat("{0},", key);
                sb2.AppendFormat("@{0},", key);// items[key]);
                paras.Add(CreateParameter(key, items[key].CheckValue()));
            }
            sb.Remove(sb.Length - 1, 1);
            sb2.Remove(sb2.Length - 1, 1);
            sb2.Append(")");
            sb.Append(")").Append(sb2.ToString());
            parameters = paras.ToArray();
            return sb.ToString();
        }

        public object ExecuteInsert(string tableName, IDictionary<string, object> items, string key)
        {
            DbParameter[] parameters;
            var sql = GetInsertSql(tableName, items, out parameters);
            //Execute(sql);
            return ExecuteSqlInsert(sql, tableName, key, parameters);
        }

        public void Execute(string tableName, IDictionary<string, object> items)
        {
            DbParameter[] parameters;
            var sql = GetInsertSql(tableName, items, out parameters);
            //Execute(sql);
            ExecuteNonQuery(sql, parameters);
        }
    }
}
