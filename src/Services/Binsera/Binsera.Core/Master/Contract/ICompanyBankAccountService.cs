using Binsera.Core.Master.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Binsera.Core.Master.Contract
{
    public interface ICompanyBankAccountService
    {
        Task<List<CompanyBankAccountModel>> GetCompanyBankAccounts();
    }
}
