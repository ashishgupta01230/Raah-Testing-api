namespace Binsera.Core.Members.Models
{
    public class PaymentTransactionModel
    {
        public string ReferenceType { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string ReferenceNo { get; set; }
        public string Status { get; set; }
        public long MediaId { get; set; }
    }
}
