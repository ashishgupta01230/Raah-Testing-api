using Binsera.Core.Orders.Models;
using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Orders.Contracts
{
    public interface IOrderService
    {
        Task<long> CreateOrderAsync(CreateOrderRequest request, long memberId);
        Task<IPagedList<OrderListResponse>> GetMemberOrdersAsync(long memberId, OrderListRequest request, CancellationToken cancellationToken);
        Task<OrderDetailResponse> GetOrderDetailsAsync(long orderId, long memberId);
        Task<bool> HasNoPurchasedOrderAsync(long memberId, CancellationToken cancellationToken);
        Task CancelOrderAsync(long orderId, long memberId);
        Task<AdminOrderSummaryDto> GetAdminOrderSummaryAsync(AdminOrderListRequest request, CancellationToken cancellationToken);
        Task<AdminOrderListResponseDto> GetAdminOrdersAsync(AdminOrderListRequest request, CancellationToken cancellationToken);
        Task<AdminOrderDetailResponse> GetAdminOrderDetailsAsync(long orderId);
        Task ApproveOrderAsync(long orderId, long adminId);
        Task VerifyPaymentAsync(long orderId, long adminId);
        Task RejectOrderAsync(long orderId, long adminId, RejectOrderRequest request);
    }
}
