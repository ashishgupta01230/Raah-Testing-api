using Binsera.Application.Incomes.Model;
using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Application.Incomes.Contracts
{
    public interface ITransactionService
    {
        Task<IPagedList<TransactionItemModel>> GetTransactionList(Guid memberId, TransactionFilterModel filter, CancellationToken cancellationToken);
    }
}
