using Binsera.Core.Master.Contract;
using Binsera.Core.Master.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Master
{
    [Route("master/state")]
    [ApiController]
    public class StateController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetStates(
            [FromQuery] StateFilterModel filterModel,
            [FromServices] IStateService stateService)
        {
            var states = await stateService.GetStates(filterModel);
            return Ok(states);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStateStatus(
            [FromRoute] int id,
            [FromBody] UpdateStateStatus request,
            [FromServices] IStateService stateService)
        {
            await stateService.UpdateStateStatus(id, request.sActive);
            return Ok(new { message = "Status updated successfully" });
        }
    }
}
