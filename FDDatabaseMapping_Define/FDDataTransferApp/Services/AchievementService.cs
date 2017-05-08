using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using FDDataTransfer.App.Entities;
using FDDataTransfer.App.Extensions;
using FDDataTransfer.App.Core.Queues;
using FDDataTransfer.Infrastructure.Extensions;
using System.Linq;

namespace FDDataTransfer.App.Services
{
    public class AchievementService : QueueBaseService, IAchievementService
    {
        private bool _isContinueToDeal; // 增量处理
        private IEnumerable<IDictionary<string, object>> _userLevelInfo; // 用户层级关系

        protected override string Name => "业绩数据处理";

        public AchievementService(string configFileName, bool isContinueToDeal)
        {
            _context = new OperConext<Transfer>(configFileName);
            _isContinueToDeal = isContinueToDeal;
        }

        public AchievementService(TableConfig config, bool isContinueToDeal)
        {
            _context = new OperConext<Transfer>(config);
            _isContinueToDeal = isContinueToDeal;
        }

        private void InitData(IRepositoryContext<Transfer> context)
        {
            if (_userLevelInfo == null)
                TimeOutTryAgain(() => _userLevelInfo = context.Get("SELECT u.Id,u.UserName,pr.Level FROM dbo.User_PlacementRelation AS pr INNER JOIN User_User AS u ON u.Id=pr.UserId"));
        }

        /// <summary>
        /// 业绩数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        private void ExecuteAchievement(IRepositoryContext<Transfer> context, Message message)
        {
            InitData(context);

            var username = GetMessageData("UserName", message);
            var userLevel = _userLevelInfo.FirstOrDefault(u => u["UserName"].Equals(username));
            if (userLevel == null)
            {
                this.Log($"未找到{username}的安置关系信息，业绩数据无法导入。");
                return;
            }

            IDictionary<string, object> row = new Dictionary<string, object>();
            row["ModuleId"] = "5ABF19F3-9E71-4D27-AD0C-D1340D9CB81B";
            row["UserId"] = userLevel["Id"];
            row["Level"] = userLevel["Level"];
            row["LValue"] = GetMessageData("left_duipen_total", message);
            row["RValue"] = GetMessageData("right_duipen_total", message);
            row["TouchedValue"] = 0;
            row["TouchedTime"] = "0001-01-01 00:00:00.0000000";
            row["TouchedReturnTime"] = "0001-01-01 00:00:00.0000000";
            row["LTeamNum"] = GetMessageData("left_num", message);
            row["RTeamNum"] = GetMessageData("right_num", message);
            row["LLeftValue"] = GetMessageData("left_duipen", message);
            row["RLeftValue"] = GetMessageData("right_duipen", message);
            row["SrcId"] = GetMessageData("SrcId", message);

            TimeOutTryAgain(() =>
            {
                context.Execute("FenRun_LevelTouchValue", ObjectToString(row));
                this.Log($"Execute For Achievement :{row.CollToString()} SUCCESS.");
            });
        }

        protected override void ExecuteForUserDefineBusiness(IRepositoryContext<Transfer> context, Message message)
        {
            ExecuteAchievement(context, message);
        }
    }
}
