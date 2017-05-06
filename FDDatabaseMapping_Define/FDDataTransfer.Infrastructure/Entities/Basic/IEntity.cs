using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Entities.Basic
{
    public interface IEntity<T>
    {
        T Id { get; set; }
    }
    public interface IEntity : IEntity<long>
    {
    }
}
