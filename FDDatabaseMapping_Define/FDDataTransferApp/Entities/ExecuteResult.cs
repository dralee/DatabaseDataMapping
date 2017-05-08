using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.App.Entities
{
    public class ExecuteResult
    {
        public ExecuteState State { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        /// <summary>
        /// 指定服务是否已完成
        /// </summary>
        public bool ServiceFinished { get; set; }

        public override string ToString()
        {
            return $"ExecuteResult:{nameof(State)}:{State};{nameof(Message)}:{Message};{nameof(Exception)}{Exception?.Message}";
        }
    }

    public enum ExecuteState
    {
        Success = 1, Fail
    }
}
