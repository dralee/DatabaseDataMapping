using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public interface IReadRepository<T> : IRepository<T> where T : IEntity
    {
        T Get(long id);
        IEnumerable<T> Get(Func<T, string> query);
    }
}
