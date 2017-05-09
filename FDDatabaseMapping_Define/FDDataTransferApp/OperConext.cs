using FDDataTransfer.App.Configs;
using FDDataTransfer.Infrastructure.Entities.Basic;
using FDDataTransfer.Infrastructure.Repositories;
using FDDataTransfer.Infrastructure.Runtime;
using FDDataTransfer.Core.Entities;
using System.Collections.Generic;
using System;
using System.Linq;

namespace FDDataTransfer.App
{
    /// <summary>
    /// 操作上下文
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class OperConext<T> where T : IEntity, new()
    {
        /// <summary>
        /// 当前配置
        /// </summary>
        public TableConfig CurrentTableConfig { get; set; }

        public IRepositoryContext<T> FromContext { get; set; }
        public IList<IRepositoryContext<T>> FromContexts { get; set; }
        public IList<IRepositoryContext<T>> ToContexts { get; set; }
        public IRepositoryContext<T> ToContext { get; set; }

        /// <summary>
        /// 目标数
        /// </summary>
        public int ToCount { get { return ToContexts.Count; } }

        public OperConext(string configFileName)
        {
            var configManager = new ConfigManager();
            CurrentTableConfig = configManager.LoadConfig(configFileName);
            Init();
        }

        public OperConext(TableConfig config)
        {
            CurrentTableConfig = config;
            Init();
        }

        private void Init()
        {
            var n = Environment.ProcessorCount;
            ToContexts = new List<IRepositoryContext<T>>();
            FromContexts = new List<IRepositoryContext<T>>();
            for (var i = 0; i < n; ++i)
            {
                FromContexts.Add(RuntimeContext.Current.GetInstance<IRepositoryContext<T>, T>(CurrentTableConfig.DBContextTypeFrom, CurrentTableConfig.ConnStringFrom));
                ToContexts.Add(RuntimeContext.Current.GetInstance<IRepositoryContext<T>, T>(CurrentTableConfig.DBContextTypeTo, CurrentTableConfig.ConnStringTo));
            }
            FromContext = FromContexts.FirstOrDefault();//RuntimeContext.Current.GetInstance<IRepositoryContext<T>, T>(CurrentTableConfig.DBContextTypeFrom, CurrentTableConfig.ConnStringFrom); //new MySqlRepositoryContext<T>(ConfigManager.TableConfig.ConnStringFrom);
            ToContext = ToContexts.FirstOrDefault();//RuntimeContext.Current.GetInstance<IRepositoryContext<T>, T>(CurrentTableConfig.DBContextTypeTo, CurrentTableConfig.ConnStringTo); //new SqlServerRepositoryContext<T>(ConfigManager.TableConfig.ConnStringTo);
        }
    }
}
