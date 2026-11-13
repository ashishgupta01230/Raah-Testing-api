using Binsera.Core.Payments.Contracts;
using Binsera.Core.Payments.Enums;
using Binsera.Core.Payments.Models;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Payments
{
    [Route("payments")]
    public class PaymentController(IPaymentService paymentService) : ApiControllerBase
    {
        [HttpGet("my")]
        public async Task<IActionResult> GetMyPayments(
            [FromQuery] PaymentListRequest request, CancellationToken cancellationToken)
        {
            var response = await paymentService.GetMemberPaymentsAsync(
                UserId, request, cancellationToken);
            return Ok(response);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetPaymentDetails(long id)
        {
            var response = await paymentService.GetPaymentAsync(id, UserId);
            return Ok(response);
        }

        [HttpGet("by-reference")]
        public async Task<IActionResult> GetPaymentByReference(
            [FromQuery] ReferenceType referenceType, [FromQuery] long referenceId)
        {
            var response = await paymentService.GetPaymentByReferenceAsync(
                referenceType, referenceId, UserId);
            return Ok(response);
        }
    }
}
