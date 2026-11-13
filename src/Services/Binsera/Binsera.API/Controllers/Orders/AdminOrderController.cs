using Binsera.Core.Members.Domain;
using Binsera.Core.Orders.Contracts;
using Binsera.Core.Orders.Models;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Configuration.Filters;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Binsera.API.Controllers.Orders
{
    [Route("admin/orders")]
    [AdminOnly]
    public class AdminOrderController(IOrderService orderService, IUnitOfWork unitOfWork) : ApiControllerBase
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();


        [HttpGet("summary")]
        public async Task<IActionResult> GetOrderSummary(
            [FromQuery] AdminOrderListRequest request, CancellationToken cancellationToken)
        {
            var response = await orderService.GetAdminOrderSummaryAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("")]
        public async Task<IActionResult> GetOrders(
            [FromQuery] AdminOrderListRequest request, CancellationToken cancellationToken)
        {
            var response = await orderService.GetAdminOrdersAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetOrderDetails(long id, Guid? memberId, CancellationToken cancellationToken)
        {
            if (memberId.HasValue)
            {
                var regNo = await GetRegNo(memberId.Value, cancellationToken);
                if (regNo <= 0)
                    return NotFound(new { message = "Member not found." });

                var response = await orderService.GetAdminOrderDetailsAsync(id);
                if (response?.Member?.RegNo != regNo)
                    return NotFound(new { message = "Order not found for this member." });

                return Ok(response);
            }

            var detail = await orderService.GetAdminOrderDetailsAsync(id);
            return Ok(detail);
        }

        [HttpPost("{id:long}/approve")]
        public async Task<IActionResult> ApproveOrder(long id)
        {
            await orderService.ApproveOrderAsync(id, UserId);
            return Ok(new { message = "Order approved successfully." });
        }

        [HttpPost("{id:long}/verify-payment")]
        public async Task<IActionResult> VerifyPayment(long id)
        {
            await orderService.VerifyPaymentAsync(id, UserId);
            return Ok(new { message = "Payment verified successfully." });
        }

        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> RejectOrder(long id, RejectOrderRequest request)
        {
            await orderService.RejectOrderAsync(id, UserId, request);
            return Ok(new { message = "Order rejected successfully." });
        }

        private async Task<long> GetRegNo(Guid memberId, CancellationToken cancellationToken)
        {
            return await _memberRepository.TableNoTracking
                .Where(x => x.MemberGraphId == memberId)
                .Select(x => x.RegNo ?? 0)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
