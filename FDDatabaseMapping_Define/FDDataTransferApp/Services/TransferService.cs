﻿using System;
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
    public class TransferService : BaseService, ITransferService//ServiceBase<Transfer>, ITransferService
    {
        private OperConext<Transfer> _context;

        IMessageQueue<Message> _queue = new MessageQueue<Message>();

        //public TransferService(IRepositoryContext<Transfer> context) : base(new ReadWriteRepository<Transfer>(context))
        //{

        //}

        /// <summary>
        /// 是否已达最大队列数
        /// </summary>
        public bool IsMaxQueue
        {
            get
            {
                var max = _context.CurrentTableConfig.QueueMaxCount;
                return max > 0 && max < _queue.Count;
            }
        }

        public TransferService(string configFileName)
        {
            _context = new OperConext<Transfer>(configFileName);
        }
        public TransferService(TableConfig config)
        {
            _context = new OperConext<Transfer>(config);
        }

        private bool CheckMessageType(int type)
        {
            var values = Enum.GetValues(typeof(MessageType));
            for (int i = 0; i < values.Length; ++i)
            {
                if (values.GetValue(i).ToInt() == type)
                    return true;
            }
            return false;
        }

        public void Read(IRepositoryContext<Transfer> context, Table tableConfig, Action<ExecuteResult> readFinish)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (tableConfig == null)
                throw new ArgumentNullException(nameof(tableConfig));
            if (!CheckMessageType(tableConfig.MessageType))
                throw new ArgumentException("MessageType设置异常");
            //if (srcTableName.IsNullOrEmpty())
            //    throw new ArgumentNullException(nameof(srcTableName));
            //if (targetTableName.IsNullOrEmpty())
            //    throw new ArgumentNullException(nameof(targetTableName));
            //if (key.IsNullOrEmpty())
            //    throw new ArgumentNullException(nameof(key));
            //if (columnMapper == null || columnMapper.Count == 0)
            //    throw new ArgumentNullException(nameof(columnMapper));

            /*if (tableConfig.ManyToOne && tableConfig.ManyToOneDefaultValues != null)
            {
                int count = 0;
                for (int i = 0; i < tableConfig.ManyToOneDefaultValues.Count; ++i)
                {
                    var item = tableConfig.ManyToOneDefaultValues[i];
                    if (i == 0)
                    {
                        count = item.Values.Count;
                    }
                    else if (count != item.Values.Count)
                    {
                        throw new ArgumentException($"列{item.Name}多值对一列的配置默认值有误");
                    }
                }
            }*/

            string srcTableName = tableConfig.TableFrom;
            string targetTableName = tableConfig.TableTo;
            string key = tableConfig.KeyFrom;
            Dictionary<string, string> columnMapper = tableConfig.Columns.ToDictionary();
            int perExecuteCount = tableConfig.PerExecuteCount;
            Action<IDictionary<string, object>> checkDefaultValues = dic =>
             {
                 if (tableConfig.ColumnDefaultValues != null && tableConfig.ColumnDefaultValues.Count > 0)
                 {
                     foreach (var defValue in tableConfig.ColumnDefaultValues)
                     {
                         if (!dic.ContainsKey(defValue.Name))
                             dic[defValue.Name] = defValue.Value;
                     }
                 }
             };

            try
            {
                var index = context.ExecuteScalar($"SELECT MIN({key}) FROM {srcTableName}").ToLong();
                var max = context.ExecuteScalar($"SELECT MAX({key}) FROM {srcTableName}").ToLong();
                var count = 0;
                var perCount = perExecuteCount;
                Func<long> last = () => index + perCount - 1;
                var columns = columnMapper.Keys.ToList();
                var cKey = columns.FirstOrDefault(c => c.Equals(key));
                if (cKey == null)
                {
                    columns.Add(key);
                }
                var extendQueryColumns = tableConfig.ExtendQueryColumns;
                if (extendQueryColumns != null && extendQueryColumns.Count > 0)
                {
                    foreach (var column in extendQueryColumns)
                    {
                        if (!column.IsNullOrEmpty() && !columns.Contains(column))
                        {
                            columns.Add(column);
                        }
                    }
                }
                while (index < max)
                {
                    IEnumerable<IDictionary<string, object>> items = context.Get(srcTableName, columns, $"{key} BETWEEN {index} AND {last()}");
                    count += items.Count();

                    foreach (var item in items)
                    {
                        if (IsMaxQueue)
                        {
                            SLEEP:
                            Thread.Sleep(100);
                            if (IsMaxQueue)
                                goto SLEEP;
                        }
                        Dictionary<string, object> targetItem = new Dictionary<string, object>();
                        IDictionary<string, object> extendItem = new Dictionary<string, object>();
                        List<string> manyToOneKeys = new List<string>();
                        foreach (var cValue in item)
                        {
                            if (columnMapper.ContainsKey(cValue.Key))
                            {
                                var ckey = columnMapper[cValue.Key];
                                if (tableConfig.ManyToOne)
                                {
                                    if (!targetItem.ContainsKey(ckey))
                                    {
                                        manyToOneKeys.Add(ckey);
                                        List<object> values = new List<object>();
                                        values.Add(cValue.Value);
                                        targetItem[ckey] = values;
                                    }
                                }
                                else
                                {
                                    targetItem[ckey] = cValue.Value;
                                }
                            }
                            else // 扩展数据
                            {
                                extendItem[cValue.Key] = cValue.Value;
                            }
                        }
                        checkDefaultValues(targetItem);
                        var message = new Message(targetItem, extendItem) { TargetTable = targetTableName, MessageType = (MessageType)tableConfig.MessageType };//, ManyToOneData = MapManyToOne(tableConfig.ManyToOneDefaultValues) };
                        CheckMessageStatus(message);
                        _queue.Enqueue(message);
                        this.Log($"Execute Read: Current Queue({_queue.Count}){message}");
                    }
                    index += perCount;
                }
                if (readFinish != null)
                {
                    ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}]已读取完源[{context.ConnString}]表{srcTableName}中的{count}条记录到队列。当前队列数量{_queue.Count}" };
                    readFinish(result);
                    this.Log(result);
                }
            }
            catch (Exception ex)
            {
                this.Log("Execute Read Error", ex);
            }
        }

        public void Run(Action<ExecuteResult> readFinish, Action<ExecuteResult> writeFinish)
        {
            foreach (var tableConfig in _context.CurrentTableConfig.Tables)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Read(_context.FromContext, tableConfig, readFinish);
                    }
                    catch (Exception ex)
                    {
                        this.Log("Execute Read Error", ex);
                        readFinish?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Read Error:", State = ExecuteState.Fail });
                    }
                });
            }

            Task.Run(() =>
            {
                try
                {
                    Write(_context.ToContext, writeFinish);
                }
                catch (Exception ex)
                {
                    writeFinish?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Write Error:", State = ExecuteState.Fail });
                    this.Log("Execute Write Error", ex);
                }
            });
        }

        public void Write(IRepositoryContext<Transfer> context, Action<ExecuteResult> writeFinish)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            try
            {
                DateTime dtLast = DateTime.Now;
                int checkTimes = 0;
                long elapseMinutes = 0;
                Func<bool> checkWrite = () =>
                {
                    elapseMinutes = (DateTime.Now - dtLast).Minutes;
                    if ((checkTimes == 0) || elapseMinutes >= 1)
                    {
                        checkTimes++;
                        dtLast = DateTime.Now;
                        this.Log($"Executing Write: nothing to execute. try for {checkTimes} minutes.Queue current({_queue.Count})");
                    }
                    return _context.CurrentTableConfig.NoMessageToQuit != 0 && _context.CurrentTableConfig.NoMessageToQuit < checkTimes; // 是否满足中止条件
                };
                while (true)
                {
                    if (_queue.Count == 0)
                    {
                        if (checkWrite())
                            break;
                        continue;
                    }
                    checkTimes = 0;
                    var message = _queue.Dequeue();
                    if (message != null)
                    {
                        try
                        {
                            switch (message.MessageType)
                            {
                                case MessageType.Normal:
                                    context.Execute(message.TargetTable, ObjectToString(message.Data));
                                    break;
                                case MessageType.UserAccount:
                                    ExecuteForUserAndAccount(context, message);
                                    break;
                            }
                            //message.ManyToKeys != null && message.ManyToKeys.Count > 0)                                
                            //ExecuteSpecialManyToOne(context, message.TargetTable, message.Data, message.ManyToKeys);
                            this.Log($"Execute Write:{message} SUCCESS.");
                        }
                        catch (Exception ex)
                        {
                            if (message.NeedTry(ex.Message))
                            {
                                _queue.Enqueue(message);
                                this.Log($"Execute Write:{message} Failed.Retrying...", ex);
                            }
                            else
                            {
                                _queue.FailureMessages.Add(message);
                                this.Log($"Execute Write:{message} Failed.Enter for Failure Message.", ex);
                            }
                        }
                    }
                }
                writeFinish?.Invoke(new ExecuteResult { Message = $"Execute Write Finished by timeout {elapseMinutes} minutes.", State = ExecuteState.Success });
            }
            catch (Exception ex)
            {
                this.Log("Execute Write Error", ex);
                writeFinish?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Write Error:", State = ExecuteState.Fail });
            }
        }

        /// <summary>
        /// 从message中检索数据
        /// </summary>
        Func<string, Message, object> getMessageData = (name, message) =>
         {
             object res;
             if (message.Data.ContainsKey(name))
             {
                 res = message.Data[name];
             }
             else if (message.ExtendData.ContainsKey(name))
             {
                 res = message.ExtendData[name];
             }
             else
             {
                 res = null;
             }
             return res;
         };

        /// <summary>
        /// 系统间状态转换
        /// </summary>
        /// <param name="message"></param>
        private void CheckMessageStatus(Message message)
        {
            Func<int, int> statusMapping = status =>
             {
                 int result = 0;
                 switch (status)
                 {
                     case 3:
                         result = 0; break;
                     case 2:
                         result = 1; break;
                     case 0:
                         result = 2; break;
                 }
                 return result;
             };
            if (message.Data.ContainsKey("Status"))
            {
                message.Data["Status"] = statusMapping(message.Data["Status"].ToInt());
            }
            if (message.ExtendData.ContainsKey("Status"))
            {
                message.ExtendData["Status"] = statusMapping(message.ExtendData["Status"].ToInt());
            }
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
                userId = context.ExecuteInsert(message.TargetTable, ObjectToString(message.Data)).ToLong();
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
                        row["Amount"] = getMessageData(type.Name, message).ToDecimal() * 850;
                    else
                        row["Amount"] = getMessageData(type.Name, message);
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
                row["Status"] = getMessageData("Status", message);
                row["UserId"] = userId;
                row["SrcId"] = getMessageData("SrcId", message);
                context.Execute("Finance_Account", ObjectToString(row));
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
            var level = getMessageData("UE_level", message).ToInt();
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
            row["Status"] = getMessageData("Status", message);
            row["SrcId"] = getMessageData("SrcId", message);

            row["ParentId"] = 0;
            row["UserRegionId"] = 0;
            row["CheckUserId"] = 0;
            row["CircleId"] = 0;
            row["CheckTime"] = DateTime.Now;

            TimeOutTryAgain(() =>
            {
                context.Execute("User_UserTypeIndex", ObjectToString(row));
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
                    context.Execute(targetTable, ObjectToString(item));
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
