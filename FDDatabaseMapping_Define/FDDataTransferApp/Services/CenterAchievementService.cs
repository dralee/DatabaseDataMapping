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
    public class CenterAchievementService : QueueBaseService, IAchievementService
    {
        private bool _isContinueToDeal; // 增量处理
        private IDictionary<string, IDictionary<string, object>> _userInfo; // 用户

        protected override string Name => "服务中心业绩数据处理";

        public CenterAchievementService(string configFileName, bool isContinueToDeal)
        {
            _context = new OperConext<Transfer>(configFileName);
            _isContinueToDeal = isContinueToDeal;
            ReadCondition = "yj_total>0";
        }

        private void InitData(IRepositoryContext<Transfer> context)
        {
            if (_userInfo == null)
                TimeOutTryAgain(() => _userInfo = context.Get("SELECT Id,UserName FROM User_User ", "UserName", key => key.ToTLower()));
        }

        public CenterAchievementService(TableConfig config, bool isContinueToDeal)
        {
            _context = new OperConext<Transfer>(config);
            _isContinueToDeal = isContinueToDeal;
            ReadCondition = "yj_total>0";
        }

        /// <summary>
        /// 业绩数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        private void ExecuteAchievement(IRepositoryContext<Transfer> context, Message message)
        {
            InitData(context);

            var username = GetMessageData("UserName", message).ToTLower();
            if (!_userInfo.TryGetValue(username, out IDictionary<string, object> user))
            {
                this.Log($"未找到{username}的用户信息，服务中心业绩数据无法导入。");
                return;
            }

            IDictionary<string, object> row = new Dictionary<string, object>();
            var level = GetMessageData("UE_level", message).ToInt();
            row["UserGradeId"] = "72be65e6-3a64-414d-972e-1a3d4a36f400";
            row["UserTypeId"] = "71BE65E6-3A64-414D-972E-1A3D4A365666";
            row["UserId"] = user["Id"];
            row["Name"] = "服务中心会员";
            row["RegionId"] = 0;
            row["ExtraDate"] = "";
            row["CreateTime"] = DateTime.Now;
            row["ModifiedTime"] = "0001-01-01 00:00:00.0000000";
            row["Remark"] = "from qefgj database.";
            row["SortOrder"] = 1000;
            row["Status"] = GetMessageData("Status", message);
            row["SrcId"] = GetMessageData("SrcId", message);

            row["ParentId"] = 0;
            row["UserRegionId"] = 0;
            row["CheckUserId"] = 0;
            row["CircleId"] = 0;
            row["CheckTime"] = DateTime.Now;

            var qty = GetMessageData("yj_total", message).ToDecimal();
            row["AchieveQty"] = qty;
            TimeOutTryAgain(() =>
            {
                context.Execute("User_UserTypeIndex", row);
                this.Log($"Execute For UserTypeIndex Center Achievement :{row.CollToString()} SUCCESS.");
            });
        }

        protected override void ExecuteForUserDefineBusiness(IRepositoryContext<Transfer> context, Message message)
        {
            ExecuteAchievement(context, message);
        }
    }
}
