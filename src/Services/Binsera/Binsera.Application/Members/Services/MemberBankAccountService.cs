using Binsera.Core.Members.Contracts;
using Binsera.Core.Members.Domain;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Members.Services
{
    /// <summary>
    /// Reads and writes the bank account a member is paid into. The account lives on the member row
    /// itself rather than a table of its own, so there is nothing extra to reconcile -- a member has
    /// exactly one payout destination and changing it overwrites the previous one. The member is
    /// found by RegNo, which comes off the JWT and is what every member-owned table keys off.
    /// </summary>
    [AutoRegister(includeInterfaces: true)]
    public class MemberBankAccountService(IUnitOfWork unitOfWork) : IMemberBankAccountService
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();

        public async Task<MemberBankAccountResponseModel?> GetAsync(long memberRegNo)
        {
            var member = await _memberRepository.TableNoTracking
                .FirstOrDefaultAsync(x => x.RegNo == memberRegNo);

            return member == null ? null : ToResponse(member);
        }

        public async Task<MemberBankAccountResponseModel> SaveAsync(MemberBankAccountModel model, long memberRegNo)
        {
            // Tracked, not no-tracking: the entity has to come back to be mutated by the domain.
            var member = await _memberRepository.Table
                .FirstOrDefaultAsync(x => x.RegNo == memberRegNo)
                ?? throw new KeyNotFoundException($"Member {memberRegNo} not found.");

            // UpdateBankDetails does the validating and normalising, and raises the domain event the
            // unit of work drains on commit.
            member.UpdateBankDetails(model);
            await _memberRepository.Save(member);

            return ToResponse(member);
        }

        private static MemberBankAccountResponseModel ToResponse(Member member) => new()
        {
            AccountNo = member.AccountNo,
            NameOnAccount = member.NameOnAccount,
            BankName = member.BankName,
            IFSCCode = member.IFSCCode,
            BranchName = member.branchName,
            AccountType = member.accountType,
            BranchCode = member.BranchCode,
        };
    }
}
