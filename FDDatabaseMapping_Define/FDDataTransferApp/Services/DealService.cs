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
        private IList<IDictionary<string, object>> _usersFrom;
        private IList<IDictionary<string, object>> _usersTo;
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
                _usersFrom = contextFrom.Get("qefgj_user", new string[] { "UE_ID", "UE_account" }, "").ToList();
                //if (_usersTo == null)
                _usersTo = contextTo.Get("User_User", new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0 AND ParentId=0" : "").ToList();
            });

            TimeOutTryAgain(() =>
            {
                string sql;
                if (_isContinueToDeal)
                {
                    foreach (var item in _usersTo)
                    {
                        var userFrom = _usersFrom.FirstOrDefault(d => d["UE_account"].Equals(item["UserName"]));
                        if (userFrom == null)
                            continue;
                        sql = $"CALL getParentAccount({userFrom["UE_ID"]})";
                        this.Log($"ExecuteUserRecommend:{sql}");
                        DealRecommendInfo(contextFrom, contextTo, sql, item["Id"]);
                    }
                }
                else
                {
                    foreach (var item in _usersFrom)
                    {
                        sql = $"CALL getParentAccount({item["UE_ID"]})";
                        this.Log($"ExecuteUserRecommend:{sql}");
                        var user = _usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                        if (user == null)
                            continue;
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
                    var toUser = _usersTo.FirstOrDefault(d => d["UserName"].Equals(reader["username"]));
                    if (toUser != null)
                    {
                        var ur = new UserRecommend { UserId = toUser["Id"].ToLong(), ParentLevel = level++ };
                        parents.Add(ur);
                        parentId = ur.UserId;
                    }
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
                    _usersFrom = contextFrom.Get("qefgj_user", new string[] { "UE_ID", "UE_account" }, "").ToList();

                if (_isContinueToDeal)
                {
                    var userRelations = contextTo.Get("User_PlacementRelation", new string[] { "UserId" }, "SrcId>0")?.ToList();
                    _usersTo = contextTo.Get("User_User", new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0" : "").ToList();
                    if (userRelations != null && userRelations.Count > 0)
                    {
                        //_usersTo = _usersTo.Where(u => !userRelations.Exists(r => r["UserId"].Equals(u["Id"])))?.ToList();
                        foreach (var relation in userRelations)
                        {
                            var user = _usersTo.FirstOrDefault(u => relation["UserId"].Equals(u["Id"]));
                            if (user != null)
                            {
                                _usersTo.Remove(user);
                            }
                        }
                    }
                    else
                    {
                        _usersTo = contextTo.Get("User_User", new string[] { "Id", "UserName" }, "").ToList();
                    }
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

        private void SubRelationContinueProc(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo, IList<IDictionary<string, object>> usersTo)
        {
            TimeOutTryAgain(() =>
            {
                string sql;
                foreach (var item in usersTo)
                {
                    var userFrom = _usersFrom.FirstOrDefault(d => d["UE_account"].Equals(item["UserName"]));
                    if (userFrom == null)
                        continue;
                    sql = $"CALL getParentPlace({userFrom["UE_ID"]})";
                    this.Log($"ExecuteRelation:{sql}");
                    DealRelation(contextFrom, contextTo, sql, item["Id"]);
                }
                this.Log($"SubRelationContinueProc execute ({Thread.CurrentThread.ManagedThreadId}) finish {usersTo.Count} records.");
            });
        }

        private void SubRelationAllProc(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo, IEnumerable<IDictionary<string, object>> usersFrom)
        {
            TimeOutTryAgain(() =>
            {
                string sql;
                foreach (var item in usersFrom)
                {
                    sql = $"CALL getParentPlace({item["UE_ID"]})";
                    var user = _usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                    if (user == null)
                        continue;
                    this.Log($"ExecuteRelation:{sql}");
                    DealRelation(contextFrom, contextTo, sql, user["Id"]);
                }
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
                    var toUser = _usersTo.FirstOrDefault(d => d["UserName"].Equals(reader["username"]));
                    if (toUser != null)
                    {
                        ur = new UserRelation { UserId = toUser["Id"].ToLong(), ParentLevel = level++, Location = reader["location"].ToInt() };
                        parents.Add(ur);
                        parentId = ur.UserId;
                    }
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
                row["SrcId"] = 1000;

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
