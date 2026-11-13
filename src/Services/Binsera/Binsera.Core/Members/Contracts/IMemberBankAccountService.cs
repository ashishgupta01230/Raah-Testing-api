using Binsera.Core.Members.Models;

namespace Binsera.Core.Members.Contracts
{
    public interface IMemberBankAccountService
    {
        /// <summary>
        /// The bank account this member has on file, or null if they have never saved one.
        /// </summary>
        Task<MemberBankAccountResponseModel?> GetAsync(long memberRegNo);

        /// <summary>
        /// Records the member's bank account, replacing whatever was there. Rejects a malformed
        /// account number or IFSC with an InvalidOperationException (a 400) rather than storing it.
        /// </summary>
        Task<MemberBankAccountResponseModel> SaveAsync(MemberBankAccountModel model, long memberRegNo);
    }
}
