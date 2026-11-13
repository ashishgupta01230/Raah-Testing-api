using Binsera.Core.Members.Models;

namespace Binsera.Core.Members.Contracts
{
    public interface IPaymentTransactionService
    {
        Task<List<PaymentTransactionModel>> GetPayment();

        Task<long> CreatePayment(PaymentTransactionModel request);
    }
}
