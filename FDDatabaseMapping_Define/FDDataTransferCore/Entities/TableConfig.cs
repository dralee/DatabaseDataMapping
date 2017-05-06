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
        /// 没有可处理消息多少分钟后退出。0：永久不退出
        /// </summary>
        public int NoMessageToQuit { get; set; }
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
        /// 多个字段对一个
        /// </summary>
        public bool ManyToOne { get; set; }
        /// <summary>
        /// 消息执行类型(与执行业务一一对应)
        /// 0:一般消息，1用户账号消息执行逻辑 
        /// </summary>
        public int MessageType { get; set; }
        /// <summary>
        /// 列对应关系
        /// </summary>
        public List<Column> Columns { get; set; }
        /// <summary>
        /// 目标无对应关系默认值列
        /// </summary>
        public List<ColumnDefaultValue> ColumnDefaultValues { get; set; }
        /// <summary>
        /// 查询中所需要的扩展列
        /// </summary>
        public List<string> ExtendQueryColumns { get; set; }
        /// <summary>
        /// 目标无对应关系默认值列(与<see cref="ManyToOne"/>一一对应，设置的ManyToOne数量及该指定列运算后的总行数必须一一对应)
        /// </summary>
        public List<ManyToOneDefaultValue> ManyToOneDefaultValues { get; set; }
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

    /// <summary>
    /// 多列值默认值列
    /// </summary>
    public class ManyToOneDefaultValue
    {
        public string Name { get; set; }
        public List<object> Values { get; set; }
    }
}
