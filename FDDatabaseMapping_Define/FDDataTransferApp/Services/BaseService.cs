using FDDataTransfer.App.Extensions;
using FDDataTransfer.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace FDDataTransfer.App.Services
{
    public abstract class BaseService : IBaseService
    {
        private static readonly string[] _timeoutMsg = { "timeout", "time out", "period", "SocketException", "超时", "过时", "等待" };

        /// <summary>
        /// 服务名称
        /// </summary>
        protected abstract string Name { get; }

        public IDictionary<string, string> ObjectToString(IDictionary<string, object> items)
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

        Func<string, bool> IsTimeoutMsg = msg =>
           {
               if (msg.IsNullOrEmpty()) return false;
               foreach (var tmsg in _timeoutMsg)
               {
                   if (msg.IndexOf(tmsg, StringComparison.OrdinalIgnoreCase) != -1)
                   {
                       return true;
                   }
               }
               return false;
           };

        private bool IsTimeOutException(Exception ex)
        {
            while (ex != null)
            {
                if ((ex is SocketException) || IsTimeoutMsg(ex.Message))
                {
                    return true;
                }
                ex = ex.InnerException;
            }
            return false;
        }

        public void TimeOutTryAgain(Action action, int maxTryTimes = 100, int tryElapseSeconds = 30)
        {
            int tryTimes = 0;
            DateTime dtLast = DateTime.Now;
            TryAgain:
            if (tryTimes > maxTryTimes)
                return;
            try
            {
                if (tryTimes > 0 && (DateTime.Now - dtLast).Seconds < tryElapseSeconds) // 30秒后重试
                {
                    goto TryAgain;
                }
                dtLast = DateTime.Now;
                action();
            }
            catch (Exception ex)
            {
                this.Log($"执行可能超时的操作异常{ex.Message}", ex);

                if (IsTimeOutException(ex))
                {
                    tryTimes++;
                    this.Log($"超时重试{tryTimes}次", ex);
                    goto TryAgain;
                }
                else
                    throw ex;
            }
        }
    }
}
