using FDDataTransfer.App.Core.Queues;
using FDDataTransfer.App.Entities;
using FDDataTransfer.App.Extensions;
using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Extensions;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FDDataTransfer.App.Services
{
    public abstract class QueueBaseService : BaseService, IQueueBaseService
    {
        protected OperConext<Transfer> _context;
        protected IMessageQueue<Message> _queue = new MessageQueue<Message>();

        /// <summary>
        /// 检测系统消息类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 从message中检索数据
        /// </summary>
        protected Func<string, Message, object> GetMessageData = (name, message) =>
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

        public void Read(IRepositoryContext<Transfer> context, Table tableConfig, Action<ExecuteResult> readFinish)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (tableConfig == null)
                throw new ArgumentNullException(nameof(tableConfig));
            if (!CheckMessageType(tableConfig.MessageType))
                throw new ArgumentException("MessageType设置异常");

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
                                case MessageType.UserDefine:
                                    ExecuteForUserDefineBusiness(context, message);
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
        /// 写操作执行系统消息类型为用户自定义的业务逻辑
        /// </summary>
        /// <param name="context">数据操作上下文</param>
        /// <param name="message">当前消息</param>
        protected abstract void ExecuteForUserDefineBusiness(IRepositoryContext<Transfer> context, Message message);

        /// <summary>
        /// 系统间状态转换
        /// </summary>
        /// <param name="message"></param>
        protected virtual void CheckMessageStatus(Message message)
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

        public void Run(Action<ExecuteResult> readFinish, Action<ExecuteResult> writeFinish)
        {
            foreach (var tableConfig in _context.CurrentTableConfig.Tables)
            {
                Task.Run(() =>
                {
                    try
                    {
                        ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 正在处理{Name}[读取]..." };
                        readFinish?.Invoke(result);
                        this.Log(result);

                        Read(_context.FromContext, tableConfig, readFinish);

                        result = new ExecuteResult { ServiceFinished = true, State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] {Name}[读取]处理完毕！" };
                        readFinish?.Invoke(result);
                        this.Log(result);
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
                    ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 正在处理{Name}[写入]..." };
                    writeFinish?.Invoke(result);
                    this.Log(result);

                    Write(_context.ToContext, writeFinish);

                    result = new ExecuteResult { ServiceFinished = true, State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 正在处理{Name}[写入]..." };
                    writeFinish?.Invoke(result);
                    this.Log(result);
                }
                catch (Exception ex)
                {
                    writeFinish?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Write Error:", State = ExecuteState.Fail });
                    this.Log("Execute Write Error", ex);
                }
            });
        }
    }
}
