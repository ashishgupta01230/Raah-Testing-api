using Binsera.Core.Payments.Enums;

namespace Binsera.Core.Payments.Models
{
    public sealed class UpdateGatewayStatusRequest
    {
        public string GatewayTransactionId { get; set; }
        public PaymentStatus Status { get; set; }
        public string Remarks { get; set; }
    }
}
