using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Core.Entities
{
    public class Transfer : Entity
    {
        public long SourceId { get; set; }
        public long TargetId { get; set; }
        public DateTime? ExecuteTime { get; set; }
    }
}
