using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FDDataTransfer.Infrastructure.Extensions
{
    public static class ObjectExtensions
    {
        public static IDictionary<string, string> OToString(this IDictionary<string, object> items)
        {
            Func<Type, bool> isNumeric = type =>
            {
                return type == typeof(int) || type == typeof(double) || type == typeof(float) ||
                type == typeof(long) || type == typeof(decimal);
            };
            Dictionary<string, string> res = new Dictionary<string, string>();
            foreach (var item in items)
            {
                if (item.Value is bool)
                {
                    res[item.Key] = Convert.ToBoolean(item.Value) ? "1" : "0";
                }
                else if (item.Value is DateTime)
                {
                    res[item.Key] = (item.Value == DBNull.Value) ? "NULL" : $"'{item.Value.ToDateTime(DateTime.MinValue)}'";
                }
                else if (isNumeric(item.Value.GetType()))
                {
                    res[item.Key] = item.Value.ToString();
                }
                else
                {
                    res[item.Key] = $"'{item.Value}'";
                }
            }
            return res;
        }

        public static object CheckValue(this object obj)
        {
            if (obj == null) return obj;

            Func<Type, bool> isNumeric = type =>
            {
                return type == typeof(int) || type == typeof(double) || type == typeof(float) ||
                type == typeof(long) || type == typeof(decimal);
            };
            if (obj is bool)
            {
                return Convert.ToBoolean(obj) ? 1 : 0;
            }
            else if (obj is DateTime)
            {
                return (obj == DBNull.Value) ? obj.ToDateTime(DateTime.MinValue) : obj;
            }
            else if (isNumeric(obj.GetType()))
            {
                return (obj == DBNull.Value) ? 0 : obj;
            }
            else
            {
                return obj.ToString();
            }
        }

        public static bool IsNullOrEmpty(this string str)
        {
            if (str == null) return true;
            return string.IsNullOrEmpty(str.Trim());
        }

        public static T ToObject<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static long ToLong(this object obj)
        {
            return Convert.ToInt64(obj);
        }

        public static DateTime ToDateTime(this object str, DateTime dtDefault)
        {
            if (str == null || str.ToString().Trim().IsNullOrEmpty()) return dtDefault;
            return Convert.ToDateTime(str);
        }

        public static string ToFormatString(this DateTime dt)
        {
            if (dt == null) return string.Empty;
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static int ToInt(this object obj)
        {
            return Convert.ToInt32(obj);
        }
        public static decimal ToDecimal(this object obj)
        {
            return Convert.ToDecimal(obj);
        }
    }
}
