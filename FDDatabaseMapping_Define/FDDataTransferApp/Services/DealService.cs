using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Extensions;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FDDataTransfer.App.Entities;
using FDDataTransfer.App.Extensions;

namespace FDDataTransfer.App.Services
{
    public class DealService : BaseService, IDealService
    {
        private OperConext<Transfer> _context;
        private IEnumerable<IDictionary<string, object>> _usersFrom, _usersTo;
        private bool _isContinueToDeal; // 增量处理

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

        //private void InitUserData(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo)
        //{

        //}

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
                _usersFrom = contextFrom.Get("qefgj_user", new string[] { "UE_ID", "UE_account" }, "");
                //if (_usersTo == null)
                _usersTo = contextTo.Get("User_User", new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0 AND ParentId=0" : "");
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
        /// 更新安置关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        private void ExecuteRelation(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo)
        {
            //InitUserData(contextFrom, contextTo);
            TimeOutTryAgain(() =>
            {
                if (_usersFrom == null)
                    _usersFrom = contextFrom.Get("qefgj_user", new string[] { "UE_ID", "UE_account" }, "");
                //if (_usersTo == null)
                //{
                if (_isContinueToDeal)
                {
                    var userRelations = contextTo.Get("User_PlacementRelation", new string[] { "UserId" }, "SrcId>0")?.ToList();
                    _usersTo = contextTo.Get("User_User", new string[] { "Id", "UserName" }, _isContinueToDeal ? "SrcId>0" : "");
                    if (userRelations != null && userRelations.Count > 0)
                    {
                        _usersTo = _usersTo.Where(u => !userRelations.Exists(r => r["UserId"].Equals(u["Id"])))?.ToList();
                    }
                }
                else
                {
                    _usersTo = contextTo.Get("User_User", new string[] { "Id", "UserName" }, "");
                }
                //}
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
                        sql = $"CALL getParentPlace({userFrom["UE_ID"]})";
                        this.Log($"ExecuteRelation:{sql}");
                        DealRelation(contextFrom, contextTo, sql, item["Id"]);
                    }
                }
                else
                {
                    foreach (var item in _usersFrom)
                    {
                        sql = $"CALL getParentPlace({item["UE_ID"]})";
                        var user = _usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                        if (user == null)
                            continue;
                        this.Log($"ExecuteRelation:{sql}");
                        DealRelation(contextFrom, contextTo, sql, user["Id"]);
                    }
                }
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

                contextTo.Execute("User_PlacementRelation", ObjectToString(row));
                this.Log($"Execute Relation :{row.CollToString()} SUCCESS.");
            }
        }

        public void Run(Action<ExecuteResult> execResult)
        {
            try
            {
                ExecuteResult result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}] 正在处理推荐关系..." }; //{Environment.NewLine}具体功能正开发中，稍后开放."
                execResult?.Invoke(result);
                this.Log(result);

                ExecuteUserRecommend(_context.FromContext, _context.ToContext);

                result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}]推荐关系处理完毕！{Environment.NewLine}正在处理安置关系..." };
                execResult?.Invoke(result);
                this.Log(result);

                ExecuteRelation(_context.FromContext, _context.ToContext);
                result = new ExecuteResult { State = ExecuteState.Success, Message = $"[{DateTime.Now.ToFormatString()}]安置关系处理完毕！" };
                execResult?.Invoke(result);
                this.Log(result);
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
