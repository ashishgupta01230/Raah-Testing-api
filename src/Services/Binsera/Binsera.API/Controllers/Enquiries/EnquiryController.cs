using Binsera.Core.Enquiries.Contracts;
using Binsera.Core.Enquiries.Models;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Enquiries
{
    [Route("enquiries")]
    [AllowAnonymous]
    public class EnquiryController(IEnquiryService enquiryService) : ApiControllerBase
    {
        [HttpPost("")]
        public async Task<IActionResult> CreateEnquiry(EnquiryRequestModel request)
        {
            var response = await enquiryService.CreateEnquiry(request);
            return Ok(response);
        }
    }
}
