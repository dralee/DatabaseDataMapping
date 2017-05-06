using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public interface IReadWriteRepository<T> : IReadRepository<T>, IWriteRepository<T> where T : Entity
    {
    }
}
