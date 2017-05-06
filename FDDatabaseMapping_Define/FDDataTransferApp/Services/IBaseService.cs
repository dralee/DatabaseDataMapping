using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.App.Services
{
    public interface IBaseService
    {
        /// <summary>
        /// 对象转string类型
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        IDictionary<string, string> ObjectToString(IDictionary<string, object> items);
        /// <summary>
        /// 超时重试
        /// </summary>
        /// <param name="action">执行的操作</param>
        /// <param name="maxTryTimes">最大重试次数</param>
        /// <param name="tryElapseSeconds">重试间隔（秒）</param>
        void TimeOutTryAgain(Action action, int maxTryTimes = 100, int tryElapseSeconds = 30);
    }
}
