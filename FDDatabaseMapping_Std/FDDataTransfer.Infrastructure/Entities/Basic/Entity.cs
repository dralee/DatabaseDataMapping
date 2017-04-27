using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Entities.Basic
{
    public class Entity : IEntity
    {
        public long Id { get; set; }
    }
}
