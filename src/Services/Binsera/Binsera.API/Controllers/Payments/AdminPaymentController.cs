using Binsera.Core.Payments.Contracts;
using Binsera.Core.Payments.Models;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Configuration.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Payments
{
    [Route("admin/payments")]
    [AdminOnly]
    public class AdminPaymentController(IPaymentService paymentService) : ApiControllerBase
    {
        [HttpGet("")]
        public async Task<IActionResult> GetPayments(
            [FromQuery] AdminPaymentListRequest request, CancellationToken cancellationToken)
        {
            var response = await paymentService.GetAdminPaymentsAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetPaymentDetails(long id)
        {
            var response = await paymentService.GetAdminPaymentDetailsAsync(id);
            return Ok(response);
        }

        [HttpPost("{id:long}/verify")]
        public async Task<IActionResult> VerifyPayment(long id, VerifyPaymentRequest request)
        {
            await paymentService.VerifyPaymentAsync(id, UserId, request);
            return Ok(new { message = "Payment verified successfully." });
        }

        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> RejectPayment(long id, RejectPaymentRequest request)
        {
            await paymentService.RejectPaymentAsync(id, UserId, request);
            return Ok(new { message = "Payment rejected successfully." });
        }

        [HttpPost("{id:long}/gateway-status")]
        public async Task<IActionResult> UpdateGatewayStatus(long id, UpdateGatewayStatusRequest request)
        {
            await paymentService.UpdateGatewayStatusAsync(id, request);
            return Ok(new { message = "Gateway status updated successfully." });
        }
    }
}
