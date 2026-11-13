using Binsera.Application.Master.Service;
using Binsera.Core.Master.Contract;
using Binsera.Core.Master.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Master
{
    [Route("master/country")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetCountries(
            [FromQuery] CountryFilterModel filterModel,
            [FromServices] ICountryService countryService)
        {
            var countries = await countryService.GetCountries(filterModel);
            return Ok(countries);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCountryStatus(
            [FromRoute] int id,
            [FromBody] UpdateCountryStatus request,
            [FromServices] ICountryService countryService)
        {
            await countryService.UpdateCountryStatus(id, request.cActive);
            return Ok(new { message = "Status updated successfully" });
        }
    }
}