using FDDataTransfer.Core.Entities;
using FDDataTransfer.Infrastructure.Repositories;

namespace FDDataTransfer.Core.Repositories
{
    public interface ITransferRepository : IReadWriteRepository<Transfer>
    {
    }
}
