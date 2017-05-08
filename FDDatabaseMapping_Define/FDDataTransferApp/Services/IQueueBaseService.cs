using FDDataTransfer.App.Entities;
using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.App.Services
{
    public interface IQueueBaseService : IBaseService
    {
        /// <summary>
        /// 是否已达最大队列数
        /// </summary>
        bool IsMaxQueue { get; }

        /// <summary>
        /// 读取源数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tableConfig"></param>
        /// <param name="readFinish"></param>
        void Read(IRepositoryContext<Transfer> context, Table tableConfig, Action<ExecuteResult> readFinish);

        /// <summary>
        /// 写入目标数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="writeFinish"></param>
        void Write(IRepositoryContext<Transfer> context, Action<ExecuteResult> writeFinish);

        /// <summary>
        /// 运行任务
        /// </summary>
        /// <param name="readFinish">读取操作完成通知</param>
        /// <param name="writeFinish">写入操作完成通知</param>
        void Run(Action<ExecuteResult> readFinish, Action<ExecuteResult> writeFinish);
    }
}
