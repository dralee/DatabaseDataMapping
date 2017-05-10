using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Extensions;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FDDataTransfer.App.Entities;
using FDDataTransfer.App.Extensions;
using System.Threading.Tasks;
using System.Threading;

namespace FDDataTransfer.App.Services
{
    public class DealService : BaseService, IDealService
    {
        private OperConext<Transfer> _context;
        private IDictionary<string, IDictionary<string, object>> _usersFrom;
        private IDictionary<string, IDictionary<string, object>> _usersTo;
        private bool _isContinueToDeal; // 增量处理

        protected override string Name => "推荐、安置数据处理";

        public DealService(string configFileName, bool isContinueToDeal)
        {
            _context = new OperConext<Transfer>(configFileName);
            _isContinueToDeal = isContinueToDeal;
        }

        public DealService(TableConfig config, bool isContinueToDeal)
        {
            _context = new OperConext<Transfer>(config);
            _isContinueToDeal = isContinueToDeal;
        }

        /// <summary>
        /// 更新推荐关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        private void ExecuteUserRecommend(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo)
        {
            //InitUserData(contextFrom, contextTo);
            TimeOutTryAgain(() =>
            {
                // if (_usersFrom == null)
                _usersFrom = contextFrom.Get("qefgj_user", "UE_account", new string[] { "UE_ID", "UE_account" }, "");
                //if (_usersTo == null)
                _usersTo = contextTo.Get("User_User", "UserName", new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0 AND ParentId=0" : "");
            });

            TimeOutTryAgain(() =>
            {
                string sql;
                if (_isContinueToDeal)
                {
                    foreach (var item in _usersTo)
                    {
                        if (!_usersFrom.ContainsKey(item.Value["UserName"].ToString()))
                            continue;


                        var userFrom = _usersFrom[item.Value["UserName"].ToString()];//_usersFrom.FirstOrDefault(d => d["UE_account"].Equals(item["UserName"]));
                        //if (userFrom == null)
                        //    continue;
                        sql = $"CALL getParentAccount({userFrom["UE_ID"]})";
                        this.Log($"ExecuteUserRecommend:{sql}");
                        DealRecommendInfo(contextFrom, contextTo, sql, item.Value["Id"]);
                    }
                }
                else
                {
                    foreach (var item in _usersFrom)
                    {
                        sql = $"CALL getParentAccount({item.Value["UE_ID"]})";
                        this.Log($"ExecuteUserRecommend:{sql}");

                        if (!_usersTo.ContainsKey(item.Value["UE_account"].ToString()))
                            continue;
                        var user = _usersTo[item.Value["UE_account"].ToString()];//_usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                        //if (user == null)
                        //    continue;
                        DealRecommendInfo(contextFrom, contextTo, sql, user["Id"]);
                    }
                }
            });
        }

        /// <summary>
        /// 处理推荐关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        /// <param name="sql">获取源数据sql</param>
        /// <param name="targetUserId">目标用户id</param>
        private void DealRecommendInfo(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo, string sql, object targetUserId)
        {
            using (var reader = contextFrom.ExecuteReader(sql))
            {
                var parents = new List<UserRecommend>();
                int level = 1;
                long parentId = 0;
                while (reader.Read())
                {
                    //var toUser = _usersTo[(reader["username"].ToString()];//_usersTo.FirstOrDefault(d => d["UserName"].Equals(reader["username"]));
                    if (!_usersTo.TryGetValue(reader["username"].ToString(), out IDictionary<string, object> toUser))
                        continue;
                    //if (toUser != null)
                    //{
                    var ur = new UserRecommend { UserId = toUser["Id"].ToLong(), ParentLevel = level++ };
                    parents.Add(ur);
                    parentId = ur.UserId;
                    //    }
                }
                var parentMap = parents.ToJson();
                sql = $"UPDATE User_User SET ParentMap='{parentMap}',ParentId={parentId} WHERE Id={targetUserId}";
                this.Log($"Executing UserRecommend:{sql}");
                contextTo.Execute(sql);
                this.Log($"Execute User Recommend :[{sql}] SUCCESS.");
            }
        }

        /// <summary>
        /// 安置前期准备
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        private void RelationDataPrepare(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo)
        {
            TimeOutTryAgain(() =>
            {
                if (_usersFrom == null)
                    _usersFrom = contextFrom.Get("qefgj_user", "UE_account", new string[] { "UE_ID", "UE_account" }, "");

                if (_isContinueToDeal)
                {
                    var userRelations = contextTo.Get("SELECT u.UserName FROM User_PlacementRelation AS pr INNER JOIN User_User AS u ON u.Id=pr.UserId WHERE pr.SrcId>0").ToList();//("User_PlacementRelation", new string[] { "UserId" }, "SrcId>0")?.ToList();
                    _usersTo = contextTo.Get("User_User", "UserName", new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0" : "");
                    if (userRelations != null && userRelations.Count > 0)
                    {
                        //_usersTo = _usersTo.Where(u => !userRelations.Exists(r => r["UserId"].Equals(u["Id"])))?.ToList();
                        foreach (var relation in userRelations)
                        {
                            //var user = _usersTo.FirstOrDefault(u => relation["UserId"].Equals(u["Id"]));
                            string key = relation["UserName"].ToString();
                            if (!_usersTo.TryGetValue(key, out IDictionary<string, object> user))
                                continue;
                            //if (user != null)
                            //{
                            _usersTo.Remove(key);//(user);
                            //}
                        }
                    }
                }
                else
                {
                    _usersTo = contextTo.Get("User_User", "UserName", new string[] { "Id", "UserName" }, "");
                }
            });
        }

        /// <summary>
        /// 更新安置关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        private void ExecuteRelation(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo)
        {
            //InitUserData(contextFrom, contextTo);
            RelationDataPrepare(contextFrom, contextTo);

            if (_isContinueToDeal)
            {
                SubRelationContinueProc(contextFrom, contextTo, _usersTo);
            }
            else
            {
                SubRelationAllProc(contextFrom, contextTo, _usersFrom);
            }
        }

        /// <summary>
        /// 多程更新安置关系
        /// </summary>
        /// <param name="fromContexts"></param>
        /// <param name="toContexts"></param>
        /// <param name="finish"></param>
        private void ExecuteRelation(IList<IRepositoryContext<Transfer>> fromContexts, IList<IRepositoryContext<Transfer>> toContexts, Action finish)
        {
            RelationDataPrepare(_context.FromContext, _context.ToContext);

            var tasks = new List<Task>();
            if (_isContinueToDeal)
            {
                for (int i = 0; i < _context.ToCount; ++i)
                {
                    int index = i;
                    tasks.Add(
                        Task.Run(() =>
                        {
                            DealPageData(_usersTo, index, _context.ToCount, data =>
                                             SubRelationContinueProc(fromContexts[index], toContexts[index], data));
                        })
                    );
                };
            }
            else
            {
                for (int i = 0; i < _context.ToCount; ++i)
                {
                    int index = i;
                    tasks.Add(
                        Task.Run(() =>
                        {
                            DealPageData(_usersFrom, index, _context.ToCount, data =>
                                            SubRelationAllProc(fromContexts[index], toContexts[index], data));
                        })
                    );
                };
            }
            Task.WaitAll(tasks.ToArray());
            finish?.Invoke();
        }

        private void SubRelationContinueProc(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo, IDictionary<string, IDictionary<string, object>> usersTo)
        {
            TimeOutTryAgain(() =>
            {
                string sql;
                int count = 0;
                foreach (var item in usersTo)
                {
                    if (!_usersFrom.TryGetValue(item.Value["UserName"].ToString(), out IDictionary<string, object> userFrom))
                        continue;
                    //var userFrom = _usersFrom.FirstOrDefault(d => d["UE_account"].Equals(item["UserName"]));
                    //if (userFrom == null)
                    //    continue;
                    sql = $"CALL getParentPlace({userFrom["UE_ID"]})";
                    this.Log($"ExecuteRelation:{sql}");

                    if (count == 0)
                    {
                        contextTo.BeginTransaction();
                    }
                    try
                    {
                        DealRelation(contextFrom, contextTo, sql, item.Value["Id"]);
                        count++;
                        if (count == 1000)
                        {
                            SubRelationAllProcSubmitTransaction(contextTo);
                            count = 0;
                        }
                    }
                    catch
                    {
                        contextTo.RollbackTransaction();
                        contextTo.DisposeTransaction();
                        contextTo.Close();
                    }
                }
                if (count > 0)
                    SubRelationAllProcSubmitTransaction(contextTo);
                this.Log($"SubRelationContinueProc execute ({Thread.CurrentThread.ManagedThreadId}) finish {usersTo.Count} records.");
            });
        }

        private void SubRelationAllProcSubmitTransaction(IRepositoryContext<Transfer> contextTo)
        {
            contextTo.CommitTransaction();
            contextTo.DisposeTransaction();
            contextTo.Close();
        }

        private void SubRelationAllProc(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo, IDictionary<string, IDictionary<string, object>> usersFrom)
        {
            TimeOutTryAgain(() =>
            {
                string sql;
                int count = 0;
                foreach (var item in usersFrom)
                {
                    sql = $"CALL getParentPlace({item.Value["UE_ID"]})";
                    if (!_usersTo.TryGetValue(item.Value["UE_account"].ToString(), out IDictionary<string, object> user))
                        continue;
                    //var user = _usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                    //if (user == null)
                    //    continue;
                    if (count == 0)
                    {
                        contextTo.BeginTransaction();
                    }
                    try
                    {
                        DealRelation(contextFrom, contextTo, sql, user["Id"]);
                        count++;
                        if (count == 1000)
                        {
                            SubRelationAllProcSubmitTransaction(contextTo);
                            count = 0;
                        }
                    }
                    catch
                    {
                        contextTo.RollbackTransaction();
                        contextTo.DisposeTransaction();
                        contextTo.Close();
                    }
                    this.Log($"ExecuteRelation:{sql}");
                }
                if (count > 0)
                    SubRelationAllProcSubmitTransaction(contextTo);

                this.Log($"SubRelationAllProc execute ({Thread.CurrentThread.ManagedThreadId}) finish {usersFrom.Count()} records.");
            });
        }

        /// <summary>
        /// 处理安置关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        /// <param name="sql">源数据sql</param>
        /// <param name="targetUserId">目标用户</param>
        private void DealRelation(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo, string sql, object targetUserId)
        {
            using (var reader = contextFrom.ExecuteReader(sql))
            {
                var parents = new List<UserRelation>();
                int level = 1;
                long parentId = 0;
                UserRelation ur = null;
                while (reader.Read())
                {
                    if (!_usersTo.TryGetValue(reader["username"].ToString(), out IDictionary<string, object> toUser))
                        continue;

                    //var toUser = _usersTo.FirstOrDefault(d => d["UserName"].Equals(reader["username"]));
                    //if (toUser != null)
                    //{
                    ur = new UserRelation { UserId = toUser["Id"].ToLong(), ParentLevel = level++, Location = reader["location"].ToInt() };
                    parents.Add(ur);
                    parentId = ur.UserId;
                    //}
                }
                if (ur == null)
                    return;
                var parentMap = parents.ToJson();

                IDictionary<string, object> row = new Dictionary<string, object>();
                row["Level"] = level;
                row["Location"] = ur.Location;
                row["ParentId"] = ur.UserId;
                row["UserId"] = targetUserId;
                row["ParentMap"] = parentMap;
                row["SrcId"] = reader["userId"];

                contextTo.Execute("User_PlacementRelation", row);
                this.Log($"Execute Relation :{row.CollToString()} SUCCESS.");
            }
        }

        public void Run(Action<ExecuteResult> execResult)
        {
            RunRecommend(execResult);
            RunRelation(execResult);
        }

        public void RunRecommend(Action<ExecuteResult> execResult)
        {
            try
            {
                ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 正在处理推荐关系..." }; //{Environment.NewLine}具体功能正开发中，稍后开放."
                execResult?.Invoke(result);
                this.Log(result);

                ExecuteUserRecommend(_context.FromContext, _context.ToContext);

                result = new ExecuteResult { ServiceFinished = true, State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 推荐关系处理完毕！" };
                execResult?.Invoke(result);
                this.Log(result);
            }
            catch (Exception ex)
            {
                this.Log("Execute Deal Error", ex);
                execResult?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Deal Error:", State = ExecuteState.Fail });
            }
        }

        public void RunRelation(Action<ExecuteResult> execResult)
        {
            try
            {
                ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}]{Environment.NewLine}正在处理安置关系..." };
                execResult?.Invoke(result);
                this.Log(result);

                bool bSingle = false;
                if (bSingle)
                {
                    ExecuteRelation(_context.FromContext, _context.ToContext);
                    result = new ExecuteResult { ServiceFinished = true, State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}]安置关系处理完毕！" };
                    execResult?.Invoke(result);
                    this.Log(result);
                }
                else
                {
                    ExecuteRelation(_context.FromContexts, _context.ToContexts, () =>
                     {
                         result = new ExecuteResult { ServiceFinished = true, State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}]安置关系处理完毕！" };
                         execResult?.Invoke(result);
                         this.Log(result);
                     });
                }
            }
            catch (Exception ex)
            {
                this.Log("Execute Deal Error", ex);
                execResult?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Deal Error:", State = ExecuteState.Fail });
            }
        }
    }

    class UserRecommend
    {
        public long ParentLevel { get; set; }
        public long UserId { get; set; }
    }

    class UserRelation
    {
        public long ParentLevel { get; set; }
        public long UserId { get; set; }
        public int Location { get; set; }
    }
}
