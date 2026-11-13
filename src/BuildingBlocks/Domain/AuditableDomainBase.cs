namespace HautSphere.BuildingBlocks.Domain
{
    public class AuditableDomainBase: TransactionDomainBase
    {
        public DateTime? ModifiedDate { get; set; }
        public long? ModifiedBy { get; set; }
    }

    public abstract class TransactionDomainBase : DomainBase
    {
        public DateTime CreatedDate { get; set; }
        public long CreatedBy { get; set; }

    }
}
