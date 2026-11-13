using Binsera.Application.Incomes.Contracts;
using Binsera.Application.Incomes.Model;
using Binsera.Core.Members.Domain;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Incomes.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class TransactionService(IUnitOfWork unitOfWork) : ITransactionService
    {
        private readonly IRepository<MemberAccount> _memberAccountRepository = unitOfWork.Repository<MemberAccount>();
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();

        public async Task<IPagedList<TransactionItemModel>> GetTransactionList(Guid memberId, TransactionFilterModel filter, CancellationToken cancellationToken)
        {
            var regNo = await GetRegNo(memberId, cancellationToken);
            var query = await _memberAccountRepository.TableNoTracking
                .Where(x => x.RegNo == regNo
                && x.Debit + x.Credit > 0
                && (filter.TransDate == null || (x.TransDate >= filter.TransDate && x.TransDate <= filter.TransDate.Value.AddDays(1).AddTicks(-1)))
                && (string.IsNullOrEmpty(filter.Particular) || x.Remark.Contains(filter.Particular))
                ).OrderByDescending(x => x.TransDate)
                .Select(x => new TransactionItemModel
                {
                    Id = x.Id,
                    TransType = x.TransType,
                    Remark = x.Remark,
                    TransDate = x.TransDate,
                    Amount = x.Credit == 0 ? -x.Debit : x.Credit,
                })
                .ToPagedListAsync
                (filter.PageIndex, filter.PageSize, cancellationToken);
            return query;
        }


        private async Task<long> GetRegNo(Guid memberId, CancellationToken cancellationToken)
        {
            var regNo = await _memberRepository.TableNoTracking
              .Where(x => x.MemberGraphId == memberId).Select(s => s.RegNo)
              .FirstOrDefaultAsync(cancellationToken);
            return regNo ?? 0;
        }
    }
}
