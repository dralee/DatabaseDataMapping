using FDDataTransfer.Infrastructure.Entities.Basic;
using FDDataTransfer.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public class RepositoryBase<T> : IReadRepository<T>, IWriteRepository<T> where T : IEntity
    {
        public IRepositoryContext<T> RepositoryContext { get; }

        public RepositoryBase(IRepositoryContext<T> repositoryContext)
        {
            RepositoryContext = repositoryContext;
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
