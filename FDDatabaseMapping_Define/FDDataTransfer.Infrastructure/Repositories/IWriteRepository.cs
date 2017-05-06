using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public interface IWriteRepository<T> : IRepository<T> where T : IEntity
    {
        void Add(T item);
        void Update(T item);
        void Delete(long id);
        void Delete(T item);
    }
}
