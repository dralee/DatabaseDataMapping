using Newtonsoft.Json;
using System;

namespace FDDataTransfer.Infrastructure.Extensions
{
    public static class ObjectExtensions
    {
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
