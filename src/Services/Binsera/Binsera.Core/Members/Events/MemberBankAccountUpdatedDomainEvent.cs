using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Events
{
    /// <summary>
    /// Raised when a member records or corrects the bank account payouts would go to. The account
    /// number and IFSC travel with it so downstream consumers (payout runs, audit trail) never have
    /// to read the member row back to find out where the money was headed.
    /// </summary>
    public record MemberBankAccountUpdatedDomainEvent(
        long MemberId,
        Guid MemberGraphId,
        string NameOnAccount,
        string BankName,
        string IFSCCode,
        string AccountNo) : DomainEventBase;
}
