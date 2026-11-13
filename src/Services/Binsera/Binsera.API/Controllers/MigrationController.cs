using Binsera.Application.Members;
using Binsera.Core.Members.Domain;
using HautSphere.BuildingBlocks.Configuration.Controller;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Binsera.API.Controllers
{
    [Route("migrations")]
    public class MigrationController(IUnitOfWork unitOfWork) : ApiControllerBase
    {
        [AllowAnonymous]
        [HttpGet("")]
        public async Task<IActionResult> Migrate([FromServices] IGraphDatabaseMemberRegistrationService memberService)
        {
            var members = await unitOfWork.Repository<Member>().TableNoTracking.Where(s => s.Id > 0).OrderBy(m => m.Id).ToListAsync();
            foreach (var member in members)
            {
                await memberService.RegisterMember(member.Id);
            }
            return Ok("Migration completed successfully.");
        }
    }
}
