#nullable disable
namespace Binsera.Core.Members.Models
{
    /// <summary>
    /// The bank account a member gives us to pay them into. These names are the JSON contract and are
    /// deliberately cleaner than the legacy member_Master column names (branchName, accountType) --
    /// the entity maps across once, in Member.UpdateBankDetails.
    /// </summary>
    public sealed class MemberBankAccountModel
    {
        public string AccountNo { get; set; }
        public string NameOnAccount      { get; set; }
        public string BankName { get; set; }
        public string IFSCCode { get; set; }
        public string BranchName { get; set; }

        /// <summary>Savings or Current. Left blank it is simply not recorded.</summary>
        public string AccountType { get; set; }
    }

    /// <summary>
    /// What the member has on file. Carries BranchCode as read-only: the column exists on
    /// member_Master but no screen captures it, so it is never accepted back on a save -- that
    /// would let a round-trip through this model wipe a value nothing else maintains.
    /// </summary>
    public sealed class MemberBankAccountResponseModel
    {
        public string AccountNo { get; set; }
        public string NameOnAccount  { get; set; }      
        public string BankName { get; set; }
        public string IFSCCode { get; set; }
        public string BranchName { get; set; }
        public string AccountType { get; set; }
        public string BranchCode { get; set; }
    }
}
