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

        /// <summary>
        /// 分页处理
        /// </summary>
        /// <param name="data">源数据</param>
        /// <param name="pageIndex">页索引（0开始）</param>
        /// <param name="pageCount">页数</param>
        /// <param name="pageAction">页操作</param>
        void DealPageData(IList<IDictionary<string, object>> data, int pageIndex, int pageCount, Action<IList<IDictionary<string, object>>> pageAction);

        /// <summary>
        /// 分页处理
        /// </summary>
        /// <param name="data">源数据</param>
        /// <param name="pageIndex">页索引（0开始）</param>
        /// <param name="pageCount">页数</param>
        /// <param name="pageAction">页操作</param>
        void DealPageData(IDictionary<string, IDictionary<string, object>> data, int pageIndex, int pageCount, Action<IDictionary<string, IDictionary<string, object>>> pageAction);
    }
}
