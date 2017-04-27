using FDDataTransfer.App.Extensions;
using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Extensions;
using FDDataTransfer.Infrastructure.FileOpers;
using FDDataTransfer.Infrastructure.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FDDataTransfer.App.Configs
{
    public class ConfigManager
    {
        private static ConfigManager _configManager = new ConfigManager();
        public List<TableConfig> TableConfigs { get; } = new List<TableConfig>();

        public static ConfigManager Default { get { return _configManager; } }

        public TableConfig LoadConfig(string fileName)
        {
            if (!FileHelper.IsFileExists(fileName))
                throw new FileNotFoundException(fileName);
            string content = FileHelper.ReadFile(fileName);
            if (string.IsNullOrEmpty(content))
                throw new Exception("配置文件内容为空");
            try
            {
                var config = content.ToObject<TableConfig>();
                config.FileName = Path.GetFileName(fileName);
                return config;
            }
            catch (Exception ex)
            {
                throw new Exception($"配置文件{fileName}内容不合法：{ex.Message}");
            }
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        public void Init()
        {
            var files = Directory.GetFiles(RuntimeContext.Current.TableConfigPath)?.Where(p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                try
                {
                    var config = LoadConfig(file);
                    if (config.Enabled)
                        TableConfigs.Add(config);
                }
                catch (Exception ex)
                {
                    this.Log("配置文件加载失败", ex);
                }
            }
        }
    }
}
