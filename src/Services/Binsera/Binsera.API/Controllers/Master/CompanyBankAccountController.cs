using Binsera.Core.Master.Contract;
using HautSphere.BuildingBlocks.Configuration.Controller;
using Microsoft.AspNetCore.Mvc;

namespace Binsera.API.Controllers.Master
{
    [Route("master/company-bank-account")]
    public class CompanyBankAccountController(ICompanyBankAccountService companyBankAccountService) : ApiControllerBase
    {
        private readonly ICompanyBankAccountService _companyBankAccountService = companyBankAccountService;

        [HttpGet]
        public async Task<IActionResult> GetCompanyBankAccounts()
        {
            var result = await _companyBankAccountService.GetCompanyBankAccounts();
            return Ok(result);
        }
    }
}