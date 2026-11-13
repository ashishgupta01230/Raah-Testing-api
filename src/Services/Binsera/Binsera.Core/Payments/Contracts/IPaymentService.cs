using Binsera.Core.Payments.Enums;
using Binsera.Core.Payments.Models;
using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Payments.Contracts
{
    public interface IPaymentService
    {
        Task<long> CreatePaymentAsync(CreatePaymentRequest request, long memberId);
        Task VerifyPaymentAsync(long paymentId, long adminId, VerifyPaymentRequest request);
        Task RejectPaymentAsync(long paymentId, long adminId, RejectPaymentRequest request);
        Task CancelPaymentAsync(long paymentId, long memberId);
        Task CancelPaymentByReferenceAsync(ReferenceType referenceType, long referenceId, string reason);
        Task VerifyPaymentByReferenceAsync(ReferenceType referenceType, long referenceId, long adminId, string remarks);
        Task<PaymentDetailResponse> GetPaymentAsync(long paymentId, long memberId);
        Task<PaymentDetailResponse> GetPaymentByReferenceAsync(ReferenceType referenceType, long referenceId, long memberId);
        Task UpdateGatewayStatusAsync(long paymentId, UpdateGatewayStatusRequest request);
        Task<IPagedList<PaymentListResponse>> GetMemberPaymentsAsync(long memberId, PaymentListRequest request, CancellationToken cancellationToken);
        Task<IPagedList<AdminPaymentListResponse>> GetAdminPaymentsAsync(AdminPaymentListRequest request, CancellationToken cancellationToken);
        Task<AdminPaymentDetailResponse> GetAdminPaymentDetailsAsync(long paymentId);
    }
}
