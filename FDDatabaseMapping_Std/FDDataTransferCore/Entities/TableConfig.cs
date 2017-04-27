using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Core.Entities
{
    /// <summary>
    /// 配置表
    /// </summary>
    public class TableConfig
    {
        public string FileName { get; set; }
        /// <summary>
        /// 来源数据连接
        /// </summary>
        public string ConnStringFrom { get; set; }
        /// <summary>
        /// 目标数据连接
        /// </summary>
        public string ConnStringTo { get; set; }
        /// <summary>
        /// 来源数据连接上下文件类型
        /// </summary>
        public string DBContextTypeFrom { get; set; }
        /// <summary>
        /// 目标数据连接上下文件类型
        /// </summary>
        public string DBContextTypeTo { get; set; }

        /// <summary>
        /// 最大队列处理数量
        /// </summary>
        public int QueueMaxCount { get; set; }
        /// <summary>
        /// 启用
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// 配置项
        /// </summary>
        public List<Table> Tables { get; set; }
    }

    public class Table
    {
        /// <summary>
        /// 来源数据表
        /// </summary>
        public string TableFrom { get; set; }
        /// <summary>
        /// 目标数据表
        /// </summary>
        public string TableTo { get; set; }
        /// <summary>
        /// 来源数据表主键
        /// </summary>
        public string KeyFrom { get; set; }
        /// <summary>
        /// 目标数据表主键
        /// </summary>
        public string KeyTo { get; set; }
        /// <summary>
        /// 每次执行数据
        /// </summary>
        public int PerExecuteCount { get; set; }
        /// <summary>
        /// 列对应关系
        /// </summary>
        public List<Column> Columns { get; set; }
        /// <summary>
        /// 目标无对应关系默认值列
        /// </summary>
        public List<ColumnDefaultValue> ColumnDefaultValues { get; set; }
    }

    /// <summary>
    /// 对应列
    /// </summary>
    public class Column
    {
        /// <summary>
        /// 源列名
        /// </summary>
        public string ColumnFrom { get; set; }
        /// <summary>
        /// 目标列名
        /// </summary>
        public string ColumnTo { get; set; }
    }

    /// <summary>
    /// 默认值列
    /// </summary>
    public class ColumnDefaultValue
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
