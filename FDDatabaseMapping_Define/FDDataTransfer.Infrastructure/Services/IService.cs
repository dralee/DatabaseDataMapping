using FDDataTransfer.Infrastructure.Entities.Basic;
using FDDataTransfer.Infrastructure.Repositories;
using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Services
{
    public interface IService<T> where T : Entity
    {
        IReadWriteRepository<T> Repository { get; }

        void Add(T item);
        void Update(T item);
        T Get(long id);
        IEnumerable<T> Get(Func<T, string> query);
        void Delete(long id);
        void Delete(T item);

        void Execute(string sql);
    }
}
