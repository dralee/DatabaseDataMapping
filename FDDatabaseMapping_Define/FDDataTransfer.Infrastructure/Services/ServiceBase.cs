using FDDataTransfer.Infrastructure.Entities.Basic;
using FDDataTransfer.Infrastructure.Repositories;
using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Services
{
    public class ServiceBase<T> : IService<T> where T : Entity, new()
    {
        public IReadWriteRepository<T> Repository { get; }

        public ServiceBase(IReadWriteRepository<T> repository)
        {
            Repository = repository;
        }

        public void Add(T item)
        {
            Repository.Add(item);
        }

        public void Delete(long id)
        {
            Repository.Delete(id);
        }

        public void Delete(T item)
        {
            Repository.Delete(item);
        }

        public T Get(long id)
        {
            return Repository.Get(id);
        }

        public IEnumerable<T> Get(Func<T, string> query)
        {
            return Repository.Get(query);
        }

        public void Update(T item)
        {
            Repository.Update(item);
        }

        public void Execute(string sql)
        {
            Repository.RepositoryContext.Execute(sql);
        }
    }
}
