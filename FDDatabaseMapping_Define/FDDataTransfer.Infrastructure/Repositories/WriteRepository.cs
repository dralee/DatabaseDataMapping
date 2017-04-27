using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public class WriteRepository<T> : IWriteRepository<T> where T : Entity
    {
        public IRepositoryContext<T> RepositoryContext { get; }

        public WriteRepository(IRepositoryContext<T> context)
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

        public void Update(T item)
        {
            RepositoryContext.Update(item);
        }
    }
}
