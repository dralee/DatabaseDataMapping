using FDDataTransfer.Infrastructure.Entities.Basic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FDDataTransfer.Infrastructure.Repositories
{
    public class ReadRepository<T> : IReadRepository<T> where T : Entity
    {
        public IRepositoryContext<T> RepositoryContext { get; }

        public ReadRepository(IRepositoryContext<T> context)
        {
            RepositoryContext = context;
        }

        public T Get(long id)
        {
            return RepositoryContext.Get(id);
        }

        public IEnumerable<T> Get(Func<T, string> query)
        {
            return RepositoryContext.Get(query);
        }
    }
}
