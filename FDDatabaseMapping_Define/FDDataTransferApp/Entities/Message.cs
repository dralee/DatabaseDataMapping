using System.Collections.Generic;
using FDDataTransfer.App.Extensions;

namespace FDDataTransfer.App.Entities
{
    public class Message
    {
        public int MaxTryTimes { get; set; }
        public int TryTimes { get; set; }
        /// <summary>
        /// 目标表名
        /// </summary>
        public string TargetTable { get; set; }
        public List<string> Exceptions { get; set; }
        /// <summary>
        /// 目标表所需要字段数据
        /// </summary>
        public IDictionary<string, object> Data { get; }
        /// <summary>
        /// 扩展查询用的
        /// </summary>
        public IDictionary<string, object> ExtendData { get; }
        /// <summary>
        /// 多个源字段对一个目标字段的目标字段名
        /// </summary>
        public List<string> ManyToKeys { get; set; }
        /// <summary>
        /// 消息执行类型
        /// </summary>
        public MessageType MessageType { get; set; }
        /// <summary>
        /// 与ManayToKeys所计算成的行数一一对应
        /// </summary>
        public IDictionary<string, List<object>> ManyToOneData { get; set; }
        public Message(IDictionary<string, object> data, IDictionary<string, object> extendData)
        {
            Data = data;
            ExtendData = extendData;
            Exceptions = new List<string>();
        }

        public bool NeedTry(string exMsg = null)
        {
            if (exMsg == null)
                return true;
            if (MaxTryTimes > TryTimes)
            {
                Exceptions.Add(exMsg);
                TryTimes++;
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"Message: {Data.CollToString()};{nameof(TargetTable)}:{TargetTable};{nameof(Exceptions)}:{Exceptions.CollToString()};{nameof(MaxTryTimes)}:{MaxTryTimes};{nameof(TryTimes)}:{TryTimes}";
        }
    }

    /// <summary>
    /// 消息执行类型
    /// </summary>
    public enum MessageType
    {
        Normal, UserDefine
    }
}
