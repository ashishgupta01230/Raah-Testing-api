using Binsera.Core.Locations.Contracts;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Locations
{
    [AllowAnonymous]
    [Route("locations/addresses")]
    public class AddressController(IAddressViewService addressViewService) : ApiControllerBase
    {
        [HttpGet("")]
        public async Task<IActionResult> GetByPinCode([FromQuery] string pinCode)
        {
            var addresses = await addressViewService.GetAddressesByPinCode(pinCode);
            return Ok(addresses);
        }
    }
}
