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
                _usersFrom = contextFrom.Get("qefgj_user", "UE_account", key => key.ToTLower(), new string[] { "UE_ID", "UE_account" }, "");
                //if (_usersTo == null)
                _usersTo = contextTo.Get("User_User", "UserName", key => key.ToTLower(), new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0 AND ParentId=0" : "");
            });

            TimeOutTryAgain(() =>
            {
                string sql;
                if (_isContinueToDeal)
                {
                    foreach (var item in _usersTo)
                    {
                        if (!_usersFrom.ContainsKey(item.Value["UserName"].ToTLower()))
                            continue;


                        var userFrom = _usersFrom[item.Value["UserName"].ToTLower()];//_usersFrom.FirstOrDefault(d => d["UE_account"].Equals(item["UserName"]));
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

                        if (!_usersTo.ContainsKey(item.Value["UE_account"].ToTLower()))
                            continue;
                        var user = _usersTo[item.Value["UE_account"].ToTLower()];//_usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
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
                    if (!_usersTo.TryGetValue(reader["username"].ToTLower(), out IDictionary<string, object> toUser))
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
                    _usersFrom = contextFrom.Get("qefgj_user", "UE_account", key => key.ToTLower(), new string[] { "UE_ID", "UE_account", "tree_position" }, "");

                if (_isContinueToDeal)
                {
                    var userRelations = contextTo.Get("SELECT u.UserName FROM User_PlacementRelation AS pr INNER JOIN User_User AS u ON u.Id=pr.UserId WHERE pr.SrcId>0").ToList();//("User_PlacementRelation", new string[] { "UserId" }, "SrcId>0")?.ToList();
                    _usersTo = contextTo.Get("User_User", "UserName", key => key.ToTLower(), new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0" : "");
                    if (userRelations != null && userRelations.Count > 0)
                    {
                        //_usersTo = _usersTo.Where(u => !userRelations.Exists(r => r["UserId"].Equals(u["Id"])))?.ToList();
                        foreach (var relation in userRelations)
                        {
                            //var user = _usersTo.FirstOrDefault(u => relation["UserId"].Equals(u["Id"]));
                            string key = relation["UserName"].ToTLower();
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
                    _usersTo = contextTo.Get("User_User", "UserName", key => key.ToTLower(), new string[] { "Id", "UserName" }, "");
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
                    if (!_usersFrom.TryGetValue(item.Value["UserName"].ToTLower(), out IDictionary<string, object> userFrom))
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
                    DealRelation(contextFrom, contextTo, sql, item.Value["Id"], userFrom["tree_position"], userFrom["UE_ID"]);
                    try
                    {
                        count++;
                        if (count == 1000)
                        {
                            SubRelationAllProcSubmitTransaction(contextTo);
                            count = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log($"DealRelation({sql}, {item.Value["Id"]}); exception:", ex);
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
                    if (!_usersTo.TryGetValue(item.Value["UE_account"].ToTLower(), out IDictionary<string, object> user))
                        continue;
                    //var user = _usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                    //if (user == null)
                    //    continue;
                    if (count == 0)
                    {
                        contextTo.BeginTransaction();
                    }
                    DealRelation(contextFrom, contextTo, sql, user["Id"], item.Value["tree_position"], item.Value["UE_ID"]);
                    try
                    {
                        count++;
                        if (count == 1000)
                        {
                            SubRelationAllProcSubmitTransaction(contextTo);
                            count = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log($"DealRelation({sql}, {user["Id"]}); exception:", ex);
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
        private void DealRelation(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo, string sql, object targetUserId, object position, object userFromId)
        {
            using (var reader = contextFrom.ExecuteReader(sql))
            {
                var parents = new List<UserRelation>();
                int level = 1;
                long parentId = 0;
                UserRelation ur = null;
                while (reader.Read())
                {
                    if (!_usersTo.TryGetValue(reader["username"].ToTLower(), out IDictionary<string, object> toUser))
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
                row["Location"] = position; //ur.Location;
                row["ParentId"] = ur.UserId;
                row["UserId"] = targetUserId;
                row["ParentMap"] = parentMap;
                row["SrcId"] = userFromId;//reader["userId"];

                contextTo.Execute("User_PlacementRelation", row);
                this.Log($"Execute Relation :{row.CollToString()} SUCCESS.");
            }
        }

        /// <summary>
        /// 用户数据中心数据
        /// </summary>
        /// <param name="toContexts"></param>
        /// <param name="finish"></param>
        private void ExecuteUserCenter(IRepositoryContext<Transfer> fromContext, IRepositoryContext<Transfer> toContext)
        {
            TimeOutTryAgain(() =>
            {
                _usersFrom = fromContext.Get("qefgj_user", "UE_account", key => key.ToTLower(), new string[] { "UE_account", "UE_drpd" }, "");
                _usersTo = toContext.Get("User_User", "UserName", key => key.ToTLower(), new string[] { "UserName", "Id" }, "");
            });

            SubUserCenterProc(toContext, _usersFrom);

            // 多批更新的表的，不应该多线程
            //var tasks = new List<Task>();
            //for (int i = 0; i < _context.ToCount; ++i)
            //{
            //    int index = i;
            //    tasks.Add(
            //        Task.Run(() =>
            //        {
            //            DealPageData(_usersFrom, index, _context.ToCount, data =>
            //                            SubUserCenterProc(toContexts[index], data, _usersTo));
            //        })
            //    );
            //}
        }

        private void SubUserCenterProc(IRepositoryContext<Transfer> contextTo, IDictionary<string, IDictionary<string, object>> usersFrom)
        {
            TimeOutTryAgain(() =>
            {
                string sql;
                int count = 0;
                foreach (var item in usersFrom)
                {
                    var account = item.Value["UE_account"].ToTLower();
                    var drpd = item.Value["UE_drpd"].ToTLower();
                    if (!_usersTo.TryGetValue(account, out IDictionary<string, object> user))
                    {
                        this.Log($"SubUserCenterProc UE_account:{account}不存在于目标库中");
                        continue;
                    }
                    if (!_usersTo.TryGetValue(drpd, out IDictionary<string, object> userCenter))
                    {
                        this.Log($"SubUserCenterProc UE_drpd:{drpd}不存在于目标库中");
                        continue;
                    }
                    if (count == 0)
                    {
                        contextTo.BeginTransaction();
                    }

                    sql = $"UPDATE User_User SET ServiceCenterUserId={userCenter["Id"]} WHERE Id={user["Id"]}";
                    contextTo.Execute(sql);
                    try
                    {
                        count++;
                        if (count == 1000)
                        {
                            SubRelationAllProcSubmitTransaction(contextTo);
                            count = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log($"SubUserCenterProc({sql}, {user["Id"]}); exception:", ex);
                        contextTo.RollbackTransaction();
                        contextTo.DisposeTransaction();
                        contextTo.Close();
                    }
                    this.Log($"SubUserCenterProc:{sql}");
                }
                if (count > 0)
                    SubRelationAllProcSubmitTransaction(contextTo);

                this.Log($"SubUserCenterProc execute ({Thread.CurrentThread.ManagedThreadId}) finish {usersFrom.Count()} records.");
            });
        }

        private void ExecuteServiceCenter(IRepositoryContext<Transfer> fromContext, IRepositoryContext<Transfer> toContext)
        {
            var sqlFrom = @"SELECT UE_account,UE_level,UE_status,UE_ID,yj_total FROM qefgj_user AS u INNER JOIN (
SELECT DISTINCT UE_drpd FROM qefgj_user) T ON T.UE_drpd=u.UE_account";
            var usersFrom = fromContext.Get(sqlFrom, "UE_account", key => key.ToTLower());

            var sqlTo = @"SELECT Id,UserName FROM User_User AS u WHERE u.Id NOT IN (
	SELECT UserId FROM dbo.User_UserTypeIndex WHERE UserTypeId='71BE65E6-3A64-414D-972E-1A3D4A365666' AND UserGradeId='72be65e6-3a64-414d-972e-1a3d4a36f400'
)";
            var usersTo = toContext.Get(sqlTo, "UserName", key => key.ToTLower());

            foreach (var userFrom in usersFrom)
            {
                var userName = userFrom.Key;
                if (!usersTo.TryGetValue(userName, out IDictionary<string, object> userTo))
                    continue;
                ExecuteServiceItem(toContext, userName, userFrom.Value, userTo["Id"]);
            }
        }

        private void ExecuteServiceItem(IRepositoryContext<Transfer> context, string userName, IDictionary<string, object> data, object targetUserId)
        {
            IDictionary<string, object> row = new Dictionary<string, object>();
            var level = data["UE_level"].ToInt();//GetMessageData("UE_level", message).ToInt();
            row["UserGradeId"] = "72be65e6-3a64-414d-972e-1a3d4a36f400";
            row["UserTypeId"] = "71BE65E6-3A64-414D-972E-1A3D4A365666";
            row["UserId"] = targetUserId; //user["Id"];
            row["Name"] = "服务中心会员";
            row["RegionId"] = 0;
            row["ExtraDate"] = "";
            row["CreateTime"] = DateTime.Now;
            row["ModifiedTime"] = "0001-01-01 00:00:00.0000000";
            row["Remark"] = "from qefgj database.";
            row["SortOrder"] = 1000;
            row["Status"] = data["UE_status"]; //GetMessageData("Status", message);
            row["SrcId"] = data["UE_ID"]; //GetMessageData("SrcId", message);

            row["ParentId"] = 0;
            row["UserRegionId"] = 0;
            row["CheckUserId"] = 0;
            row["CircleId"] = 0;
            row["CheckTime"] = DateTime.Now;

            var qty = data["yj_total"].ToDecimal(); //GetMessageData("yj_total", message).ToDecimal();
            row["AchieveQty"] = qty;
            TimeOutTryAgain(() =>
            {
                context.Execute("User_UserTypeIndex", row);
                this.Log($"Execute For UserTypeIndex Center Achievement :{row.CollToString()} SUCCESS.");
            });
        }

        public void FixServiceCenter(Action<ExecuteResult> execResult)
        {
            try
            {
                ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 正在修复创业中心问题..." };
                execResult?.Invoke(result);
                this.Log(result);

                ExecuteServiceCenter(_context.FromContext, _context.ToContext);

                result = new ExecuteResult { ServiceFinished = true, State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 创业中心问题处理完毕！" };
                execResult?.Invoke(result);
                this.Log(result);
            }
            catch (Exception ex)
            {
                this.Log("Execute Service Center Error", ex);
                execResult?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute Service Center Error:", State = ExecuteState.Fail });
            }
        }

        /// <summary>
        /// 更新用户数据中心
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        public void RunUserCenter(Action<ExecuteResult> execResult)
        {
            try
            {
                ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 正在处理用户中心关系..." };
                execResult?.Invoke(result);
                this.Log(result);

                ExecuteUserCenter(_context.FromContext, _context.ToContext);

                result = new ExecuteResult { ServiceFinished = true, State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 用户中心关系处理完毕！" };
                execResult?.Invoke(result);
                this.Log(result);
            }
            catch (Exception ex)
            {
                this.Log("Execute UserCenter Error", ex);
                execResult?.Invoke(new ExecuteResult { Exception = ex, Message = "Execute UserCenter Error:", State = ExecuteState.Fail });
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
