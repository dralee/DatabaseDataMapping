using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public class ReadWriteRepository<T> : IReadWriteRepository<T> where T : Entity
    {
        public IRepositoryContext<T> RepositoryContext { get; }
        public ReadWriteRepository(IRepositoryContext<T> context)
        {
            RepositoryContext = context;
        }

        public void Add(T item)
        {
            RepositoryContext.Add(item);
        }

        public void Delete(long id)
        {
            RepositoryContext.Delete(id);
        }

        public void Delete(T item)
        {
            RepositoryContext.Delete(item);
        }

        public T Get(long id)
        {
            return RepositoryContext.Get(id);
        }

        public IEnumerable<T> Get(Func<T, string> query)
        {
            return RepositoryContext.Get(query);
        }

        public void Update(T item)
        {
            RepositoryContext.Update(item);
        }
    }
}
