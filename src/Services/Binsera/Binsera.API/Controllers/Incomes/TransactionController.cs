using Binsera.Application.Incomes.Contracts;
using Binsera.Application.Incomes.Model;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Incomes
{
    [Route("incomes/transactions")]
    public class TransactionController(ITransactionService transactionService) : ApiControllerBase
    {
        [HttpGet("{memberId}/list")]
        public async Task<IActionResult> GetTransactionList(Guid memberId, [FromQuery] TransactionFilterModel filter, CancellationToken cancellationToken)
        {
            var result = await transactionService.GetTransactionList(memberId, filter, cancellationToken);
            return Ok(result);
        }
    }
}
