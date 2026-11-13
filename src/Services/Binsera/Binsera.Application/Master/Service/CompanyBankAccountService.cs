using Binsera.Core.Master.Contract;
using Binsera.Core.Master.Domain;
using Binsera.Core.Master.Model;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Master.Service
{
    [AutoRegister(includeInterfaces: true)]
    public class CompanyBankAccountService(IUnitOfWork unitOfWork) : ICompanyBankAccountService
    {
        private readonly IRepository<CompanyBankAccount> _repository =
            unitOfWork.Repository<CompanyBankAccount>();

        public async Task<List<CompanyBankAccountModel>> GetCompanyBankAccounts()
        {
            return await _repository.TableNoTracking
                .Select(x => new CompanyBankAccountModel
                {
                    AccountNo = x.AccountNo,
                    BankName = x.BankName,
                    BranchName = x.BranchName,
                    IFSCCode = x.IFSCCode,
                    Address = x.Address,
                    paymentTypeId = x.PaymentTypeId


                })
                .ToListAsync();
        }
    }
}