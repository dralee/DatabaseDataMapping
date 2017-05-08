using FDDataTransfer.App.Entities;
using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.App.Services
{
    public interface IDealService : IBaseService
    {
        /// <summary>
        /// 更新推荐关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        void RunRecommend(Action<ExecuteResult> result);

        /// <summary>
        /// 更新安置关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        void RunRelation(Action<ExecuteResult> result);

        /// <summary>
        /// 更新推荐/安置关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        void Run(Action<ExecuteResult> result);
    }
}
