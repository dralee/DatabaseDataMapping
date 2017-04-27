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
        public IDictionary<string, object> Data { get; }
        public Message(IDictionary<string, object> data)
        {
            Data = data;
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
}
