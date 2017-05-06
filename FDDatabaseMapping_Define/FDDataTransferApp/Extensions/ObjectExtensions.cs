using FDDataTransfer.App.Services;
using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Logger;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.App.Extensions
{
    public static class ObjectExtensions
    {
        public static Dictionary<string, string> ToDictionary(this Column column)
        {
            if (column == null)
                return null;
            var res = new Dictionary<string, string>();
            res.Add(column.ColumnFrom, column.ColumnTo);
            return res;
        }

        public static Dictionary<string, string> ToDictionary(this List<Column> columns)
        {
            if (columns == null)
                return null;
            var res = new Dictionary<string, string>();
            foreach (var column in columns)
            {
                res.Add(column.ColumnFrom, column.ColumnTo);
            }
            return res;
        }

        public static string CollToString(this IDictionary<string, object> dic)
        {
            if (dic == null || dic.Count == 0)
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Items:({0})->", dic.Count);
            foreach (var item in dic)
            {
                sb.AppendFormat("{0}:{1},", item.Key, item.Value);
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public static string CollToString<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.AppendFormat("{0},", item);
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public static void Log(this object service, object mesage, Exception ex = null)
        {
            LoggerManager.Log(mesage, ex);
        }
    }
}
