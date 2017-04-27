using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Repositories;

namespace FDDataTransfer.Core.Repositories
{
    public class TransferRepository : RepositoryBase<Transfer>, ITransferRepository
    {
        public TransferRepository(IRepositoryContext<Transfer> context) : base(context)
        {
        }
    }
}
