using HautSphere.BuildingBlocks.Domain;

namespace HautSphere.BuildingBlocks.Persistence.Contracts
{
    public interface IUnitOfWork
    {
        Task StartTransactionAsync(CancellationToken ct = default);
        Task CommitAsync(CancellationToken ct = default);
        Task RollbackAsync(CancellationToken ct = default);
        IRepository<T> Repository<T>() where T : DomainBase;
      
    }
}
