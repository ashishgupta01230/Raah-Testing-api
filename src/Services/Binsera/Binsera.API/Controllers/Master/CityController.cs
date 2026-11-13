using Binsera.Core.Locations.Models;
using Binsera.Core.Master.Contract;
using Binsera.Core.Master.Model;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Master
{
    [Route("master/city")]
    [ApiController]
    public class CityController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetCities(
            [FromQuery] CityFilterModel filterModel,
            [FromServices] ICityService cityService)
        {
            var cities = await cityService.GetAllAsync(filterModel);
            return Ok(cities);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCityById(
            [FromRoute] int id,
            [FromServices] ICityService cityService)
        {
            var city = await cityService.GetByIdAsync(id);
            if (city == null)
                return NotFound(new { message = "City not found" });
            return Ok(city);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCity(
            [FromRoute] int id,
            [FromBody] Core.Locations.Models.UpdateCityRequest request,
            [FromServices] ICityService cityService)
        {
            await cityService.UpdateAsync(id, request.cityName, request.pincode, request.active);
            return Ok(new { message = "City updated successfully" });
        }
    }
}
