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

        public DealService(string configFileName)
        {
            _context = new OperConext<Transfer>(configFileName);
        }

        public DealService(TableConfig config)
        {
            _context = new OperConext<Transfer>(config);
        }

        private void InitUserData(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo)
        {
            TimeOutTryAgain(() =>
            {
                if (_usersFrom == null)
                    _usersFrom = contextFrom.Get("qefgj_user", new string[] { "UE_ID", "UE_account" }, "");
                if (_usersTo == null)
                    _usersTo = contextTo.Get("User_User", new string[] { "Id", "UserName" }, "");
            });
        }

        /// <summary>
        /// 更新推荐关系
        /// </summary>
        /// <param name="contextFrom"></param>
        /// <param name="contextTo"></param>
        private void ExecuteUserRecommend(IRepositoryContext<Transfer> contextFrom, IRepositoryContext<Transfer> contextTo)
        {
            InitUserData(contextFrom, contextTo);

            TimeOutTryAgain(() =>
            {
                string sql;
                foreach (var item in _usersFrom)
                {
                    sql = $"CALL getParentAccount({item["UE_ID"]})";
                    this.Log($"ExecuteUserRecommend:{sql}");
                    var user = _usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                    if (user == null)
                        continue;
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
                        sql = $"UPDATE User_User SET ParentMap='{parentMap}',ParentId={parentId} WHERE Id={user["Id"]}";
                        this.Log($"Executing UserRecommend:{sql}");
                        contextTo.Execute(sql);
                        this.Log($"Execute User Recommend :[{sql}] SUCCESS.");
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
            InitUserData(contextFrom, contextTo);

            TimeOutTryAgain(() =>
            {
                string sql;
                foreach (var item in _usersFrom)
                {
                    sql = $"CALL getParentPlace({item["UE_ID"]})";
                    var user = _usersTo.FirstOrDefault(d => d["UserName"].Equals(item["UE_account"]));
                    if (user == null)
                        continue;
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
                            continue;
                        var parentMap = parents.ToJson();

                        IDictionary<string, object> row = new Dictionary<string, object>();
                        row["Level"] = level;
                        row["Location"] = ur.Location;
                        row["ParentId"] = ur.UserId;
                        row["UserId"] = user["Id"];
                        row["ParentMap"] = parentMap;
                        row["SrcId"] = 1000;

                        contextTo.Execute("User_PlacementRelation", ObjectToString(row));
                        this.Log($"Execute Relation :{row.CollToString()} SUCCESS.");
                    }
                }
            });
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
