using FDDataTransfer.App.Entities;
using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;

namespace FDDataTransfer.App.Services
{
    public interface ITransferService// : IService<Transfer>
    {
        /// <summary>
        /// 读取源数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="srcTableName"></param>
        /// <param name="targetTableName"></param>
        /// <param name="key"></param>
        /// <param name="columnMapper"></param>
        /// <param name="perExecuteCount"></param>
        /// <param name="readFinish">操作完成通知</param>
        void Read(IRepositoryContext<Transfer> context, Table tableConfig, Action<ExecuteResult> readFinish);

        /// <summary>
        /// 写入目标数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="writeFinish">操作完成通知</param>
        void Write(IRepositoryContext<Transfer> context, Action<ExecuteResult> writeFinish = null);

        /// <summary>
        /// 运行任务
        /// </summary>
        /// <param name="readFinish">读取操作完成通知</param>
        /// <param name="writeFinish">写入操作完成通知</param>
        void Run(Action<ExecuteResult> readFinish, Action<ExecuteResult> writeFinish);
        
    }
}
