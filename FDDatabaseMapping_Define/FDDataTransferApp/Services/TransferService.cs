using System;
using System.Collections.Generic;
using FDDataTransfer.App.Core.Queues;
using System.Linq;
using FDDataTransfer.App.Entities;
using FDDataTransfer.Infrastructure.Repositories;
using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Extensions;
using System.Threading.Tasks;
using FDDataTransfer.App.Extensions;
using System.Threading;
using FDDataTransfer.Infrastructure.Logger;

namespace FDDataTransfer.App.Services
{
    public class TransferService : QueueBaseService, ITransferService //ServiceBase<Transfer>, ITransferService
    {
        protected override string Name => "基础数据初始化";

        public TransferService(string configFileName)
        {
            _context = new OperConext<Transfer>(configFileName);
        }
        public TransferService(TableConfig config)
        {
            _context = new OperConext<Transfer>(config);
        }

        protected override void ExecuteForUserDefineBusiness(IRepositoryContext<Transfer> context, Message message)
        {
            ExecuteForUserAndAccount(context, message);
        }

        /// <summary>
        /// 执行用户及账号逻辑
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        private void ExecuteForUserAndAccount(IRepositoryContext<Transfer> context, Message message)
        {
            long userId = 0;
            TimeOutTryAgain(() =>
            {
                // execute for user
                userId = context.ExecuteInsert(message.TargetTable, message.Data).ToLong();
            });

            // execute for account
            var ItemDatas = message.Data;
            foreach (var type in types)
            {
                IDictionary<string, object> row = new Dictionary<string, object>();
                if (type.Name.IsNullOrEmpty())
                {
                    row["Amount"] = 0;
                }
                else
                {
                    if (type.Name.Equals("UE_cp"))
                        row["Amount"] = GetMessageData(type.Name, message).ToDecimal() * 850;
                    else
                        row["Amount"] = GetMessageData(type.Name, message);
                }
                row["Currency"] = type.Currency;
                row["CreateTime"] = DateTime.Now;
                row["ExtraDate"] = "";
                row["FreezeAmount"] = 0;
                row["HistoryAmount"] = 0;
                row["ModifiedTime"] = "0001-01-01 00:00:00.0000000";
                row["MoneyTypeId"] = type.MoneyType;
                row["Remark"] = "from qefgj database.";
                row["SortOrder"] = 1000;
                row["Status"] = GetMessageData("Status", message);
                row["UserId"] = userId;
                row["SrcId"] = GetMessageData("SrcId", message);
                context.Execute("Finance_Account", row);
            }

            TimeOutTryAgain(() =>
            {
                ExecuteForUserTypeIndex(context, message, userId);
            });
        }

        /// <summary>
        /// 执行用户类型
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        private void ExecuteForUserTypeIndex(IRepositoryContext<Transfer> context, Message message, long userId)
        {
            IDictionary<string, object> row = new Dictionary<string, object>();
            var level = GetMessageData("UE_level", message).ToInt();
            var userGrade = _userGrades[level - 1];
            row["UserGradeId"] = userGrade.Id;
            row["UserTypeId"] = userGrade.UserTypeId;
            row["UserId"] = userId;
            row["Name"] = "会员";
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

            TimeOutTryAgain(() =>
            {
                context.Execute("User_UserTypeIndex", row);
                this.Log($"Execute For UserTypeIndex :{row.CollToString()} SUCCESS.");
            });
        }

        #region many to one
        /// <summary>
        /// 多个字段对应目标一个字段
        /// </summary>
        /// <param name="context"></param>
        /// <param name="targetTable"></param>
        /// <param name="items"></param>
        /// <param name="manyToOneKeys"></param>
        private void ExecuteSpecialManyToOne(IRepositoryContext<Transfer> context, string targetTable, IDictionary<string, object> items, List<string> manyToOneKeys, IDictionary<string, List<object>> manyToOneDefaults)
        {
            if (manyToOneKeys != null && manyToOneKeys.Count > 0)
            {
                Dictionary<string, List<object>> values = new Dictionary<string, List<object>>();
                foreach (var key in manyToOneKeys)
                {
                    if (!items.ContainsKey(key))
                        throw new ArgumentException($"指定的多值字段{key}，在数据项中不存在");
                    values[key] = items[key] as List<object>;
                }
                List<IDictionary<string, object>> resItems = new List<IDictionary<string, object>>();
                int count = 0;
                // 产生目标数
                foreach (var value in values)
                {
                    count += value.Value.Count;
                }
                // 产生毛数据
                for (int i = 0; i < count; ++i)
                {
                    var item = new Dictionary<string, object>();
                    foreach (var iKey in items.Keys)
                    {
                        item[iKey] = items[iKey];
                    }
                    resItems.Add(item);
                }

                if (manyToOneDefaults != null && manyToOneDefaults.Count > 0)
                {
                    foreach (var item in manyToOneDefaults)
                    {
                        if (item.Value.Count != resItems.Count)
                        {
                            throw new ArgumentOutOfRangeException($"给定多对一列{item.Key}的默认值行数不匹配");
                        }
                        int index = 0;
                        foreach (var resItem in resItems)
                        {
                            resItem[item.Key] = item.Value[index++];
                        }
                    }
                }

                // 精确修复
                foreach (var item in resItems)
                {
                    int i = 0;
                    foreach (var key in item.Keys)
                    {
                        if (i == values[key].Count)
                        {
                            i = 0;
                        }
                        item[key] = values[key][i++];
                    }
                }

                foreach (var item in resItems)
                {
                    context.Execute(targetTable, item);
                }
            }
        }

        private IDictionary<string, List<object>> MapManyToOne(List<ManyToOneDefaultValue> values)
        {
            if (values == null || values.Count == 0)
                return null;
            IDictionary<string, List<object>> res = new Dictionary<string, List<object>>();
            foreach (var value in values)
            {
                res[value.Name] = value.Values;
            }
            return res;
        }
        #endregion

        /// <summary>
        /// 人民币现金账户（充值账户）
        /// </summary>
        List<UserAccountParam> types = new List<UserAccountParam> {
            // 充值账户
            new UserAccountParam{ Name = "", Currency = 0, MoneyType = "e97ccd1e-1478-49bd-bfc7-e73a5d699000"},
            // 提现账户
            new UserAccountParam{ Name = "", Currency = 1001, MoneyType = "e97ccd1e-1400-4900-bfc7-e73a5d691001"},
            // 交易积分
            new UserAccountParam{ Name = "UE_money", Currency = 1003, MoneyType = "e97ccd1e-1400-4900-bfc7-e73a5d691003"},
            // 激活积分
            new UserAccountParam{ Name = "UE_ji_money", Currency = 1004, MoneyType = "e97ccd1e-1400-4900-bfc7-e73a5d691004"},
            // 消费积分
            new UserAccountParam{ Name = "UE_register", Currency = 1005, MoneyType = "e97ccd1e-1400-4900-bfc7-e73a5d691005"},
            // 商城积分
            new UserAccountParam{ Name = "UE_cp", Currency = 1006, MoneyType = "e97ccd1e-1400-4900-bfc7-e73a5d691006"},
            // 电子积分
            new UserAccountParam{ Name = "UE_sum", Currency = 2, MoneyType = "e97ccd1e-1478-49bd-bfc7-e73a5d699002"},
            // 积分股
            new UserAccountParam{ Name = "UE_dai_money", Currency = 1002, MoneyType = "e97ccd1e-1400-4900-bfc7-e73a5d691002"},
            // 数字资产
            new UserAccountParam{ Name = "UE_integral", Currency = 1007, MoneyType = "e97ccd1e-1400-4900-bfc7-e73a5d691007"},
        };

        /// <summary>
        /// 用户等级
        /// </summary>
        private List<UserGrade> _userGrades = new List<UserGrade>
        {
            new UserGrade{ Id = Guid.Parse("72be65e6-3000-414d-972e-1a3d4a366001"),UserTypeId = Guid.Parse("71be65e6-3a64-414d-972e-1a3d4a365000"), Name = "VIP1"},
            new UserGrade{ Id = Guid.Parse("b0982590-f085-4510-bf5c-813e32c7b806"),UserTypeId = Guid.Parse("71be65e6-3a64-414d-972e-1a3d4a365000"), Name = "VIP2"},
            new UserGrade{ Id = Guid.Parse("46ea91a2-a295-4631-b8a9-9e7747dba895"),UserTypeId = Guid.Parse("71be65e6-3a64-414d-972e-1a3d4a365000"), Name = "VIP3"},
            new UserGrade{ Id = Guid.Parse("52899c74-7296-49bf-96ea-5e89960d91b8"),UserTypeId = Guid.Parse("71be65e6-3a64-414d-972e-1a3d4a365000"), Name = "VIP4"},
            new UserGrade{ Id = Guid.Parse("150a9931-c399-44eb-819c-f1a7ba8a21dd"),UserTypeId = Guid.Parse("71be65e6-3a64-414d-972e-1a3d4a365000"), Name = "VIP5"},
            new UserGrade{ Id = Guid.Parse("75eca104-5e9f-4d73-be56-64aba39316b7"),UserTypeId = Guid.Parse("71be65e6-3a64-414d-972e-1a3d4a365000"), Name = "VIP6"}
        };
    }

    class UserAccountParam
    {
        public string Name { get; set; }
        public int Currency { get; set; }
        public string MoneyType { get; set; }
    }
    class UserGrade
    {
        public Guid Id { get; set; }
        public Guid UserTypeId { get; set; }
        public string Name { get; set; }
    }
}
