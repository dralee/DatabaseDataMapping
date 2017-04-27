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
    public class TransferService : ITransferService//ServiceBase<Transfer>, ITransferService
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

        public void Read(IRepositoryContext<Transfer> context, Table tableConfig, Action<ExecuteResult> readFinish)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (tableConfig == null)
                throw new ArgumentNullException(nameof(tableConfig));
            //if (srcTableName.IsNullOrEmpty())
            //    throw new ArgumentNullException(nameof(srcTableName));
            //if (targetTableName.IsNullOrEmpty())
            //    throw new ArgumentNullException(nameof(targetTableName));
            //if (key.IsNullOrEmpty())
            //    throw new ArgumentNullException(nameof(key));
            //if (columnMapper == null || columnMapper.Count == 0)
            //    throw new ArgumentNullException(nameof(columnMapper));

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
                while (index < max)
                {
                    IEnumerable<IDictionary<string, object>> items = context.Get(srcTableName, columnMapper.Keys, $"{key} BETWEEN {index} AND {last()}");
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
                        foreach (var cValue in item)
                        {
                            var ckey = columnMapper[cValue.Key];
                            targetItem[ckey] = cValue.Value;
                        }
                        checkDefaultValues(targetItem);
                        var message = new Message(targetItem) { TargetTable = targetTableName };
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
                    writeFinish?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Read Error:", State = ExecuteState.Fail });
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
                Action checkWrite = () =>
                {
                    if ((checkTimes == 0) || (DateTime.Now - dtLast).Minutes >= 1)
                    {
                        checkTimes++;
                        dtLast = DateTime.Now;
                        this.Log($"Executing Write: nothing to execute. try for {checkTimes} minutes.Queue current({_queue.Count})");
                    }
                };
                while (true)
                {
                    if (_queue.Count == 0)
                    {
                        checkWrite();
                        continue;
                    }
                    checkTimes = 0;
                    var message = _queue.Dequeue();
                    if (message != null)
                    {
                        try
                        {
                            context.Execute(message.TargetTable, ObjectToString(message.Data));
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
            }
            catch (Exception ex)
            {
                this.Log("Execute Write Error", ex);
            }
        }

        private IDictionary<string, string> ObjectToString(IDictionary<string, object> items)
        {
            Func<Type, bool> isNumeric = type =>
             {
                 return type == typeof(int) || type == typeof(double) || type == typeof(float) ||
                 type == typeof(long) || type == typeof(decimal);
             };
            Dictionary<string, string> res = new Dictionary<string, string>();
            foreach (var item in items)
            {
                if (item.Value is bool)
                {
                    res[item.Key] = Convert.ToBoolean(item.Value) ? "1" : "0";
                }
                else if (isNumeric(item.Value.GetType()))
                {
                    res[item.Key] = item.Value.ToString();
                }
                else
                {
                    res[item.Key] = $"'{item.Value}'";
                }
            }
            return res;
        }

    }
}
