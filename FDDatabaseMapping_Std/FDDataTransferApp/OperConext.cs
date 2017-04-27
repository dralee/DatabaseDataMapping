using FDDataTransfer.App.Configs;
using FDDataTransfer.Infrastructure.Entities.Basic;
using FDDataTransfer.Infrastructure.Repositories;
using FDDataTransfer.Infrastructure.Runtime;
using FDDataTransfer.SqlServer.Repositories;
using System.Reflection;
using System;
using FDDataTransfer.Core.Entities;

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
        public IRepositoryContext<T> ToContext { get; set; }

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
            FromContext = RuntimeContext.Current.GetInstance<IRepositoryContext<T>, T>(CurrentTableConfig.DBContextTypeFrom, CurrentTableConfig.ConnStringFrom); //new MySqlRepositoryContext<T>(ConfigManager.TableConfig.ConnStringFrom);

            ToContext = RuntimeContext.Current.GetInstance<IRepositoryContext<T>, T>(CurrentTableConfig.DBContextTypeTo, CurrentTableConfig.ConnStringTo); //new SqlServerRepositoryContext<T>(ConfigManager.TableConfig.ConnStringTo);
        }
    }
}
