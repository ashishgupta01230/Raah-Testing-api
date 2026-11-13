using Binsera.Core.Master.Contract;
using Binsera.Core.Master.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Binsera.API.Controllers.Master
{
    [Route("master/district")]
    [ApiController]
    public class DistrictController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetDistricts(
            [FromQuery] DistrictFilterModel filterModel,
            [FromServices] IDistrictService districtService)
        {
            var districts = await districtService.GetAllAsync(filterModel);
            return Ok(districts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDistrictById(
            [FromRoute] int id,
            [FromServices] IDistrictService districtService)
        {
            var district = await districtService.GetByIdAsync(id);
            if (district == null)
                return NotFound(new { message = "District not found" });
            return Ok(district);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDistrictStatus(
            [FromRoute] int id,
            [FromBody] UpdateDistrictStatus request,
            [FromServices] IDistrictService districtService)
        {
            await districtService.UpdateStatusAsync(id, request.active);
            return Ok(new { message = "Status updated successfully" });
        }
    }
}
