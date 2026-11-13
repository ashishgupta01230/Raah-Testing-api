using Binsera.Core.Members.Domain;
using Binsera.Core.Orders.Contracts;
using Binsera.Core.Orders.Models;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Binsera.API.Controllers.Orders
{
    [Route("orders")]
    public class OrderController(IOrderService orderService, IUnitOfWork unitOfWork) : ApiControllerBase
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();

        [HttpPost("")]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
        {
            var orderId = await orderService.CreateOrderAsync(request, RegNo ?? 0);
            return Ok(orderId);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] OrderListRequest request, CancellationToken cancellationToken)
        {
            var response = await orderService.GetMemberOrdersAsync(
                RegNo ?? 0, request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetOrderDetails(long id)
        {
            var response = await orderService.GetOrderDetailsAsync(id, RegNo ?? 0);
            return Ok(response);
        }

        [HttpGet("has-no-purchased-order")]
        public async Task<IActionResult> HasNoPurchasedOrder(Guid? memberId, CancellationToken cancellationToken)
        {
            if (memberId.HasValue)
            {
                var regNo = await GetRegNo(memberId.Value, cancellationToken);
                if (regNo <= 0)
                {
                    return NotFound(new { message = "Member not found." });
                }

                var result = await orderService.HasNoPurchasedOrderAsync(regNo, cancellationToken);
                return Ok(result);
            }

            var result2 = await orderService.HasNoPurchasedOrderAsync(RegNo ?? 0, cancellationToken);
            return Ok(result2);
        }

        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> CancelOrder(long id)
        {
            await orderService.CancelOrderAsync(id, RegNo ?? 0);
            return Ok(new { message = "Order cancelled successfully." });
        }

        private async Task<long> GetRegNo(Guid memberId, CancellationToken cancellationToken)
        {
            var regNo = await _memberRepository.TableNoTracking
                .Where(x => x.MemberGraphId == memberId)
                .Select(s => s.RegNo)
                .FirstOrDefaultAsync(cancellationToken);

            return regNo ?? 0;
        }
    }
}
