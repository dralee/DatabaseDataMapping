using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public interface IRepository<T> where T : IEntity
    {
        IRepositoryContext<T> RepositoryContext { get; }
    }
}
