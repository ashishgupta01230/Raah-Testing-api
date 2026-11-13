using Binsera.Core.Enquiries.Contracts;
using Binsera.Core.Enquiries.Domain;
using Binsera.Core.Enquiries.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;

namespace Binsera.Application.Enquiries.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class EnquiryService(IUnitOfWork unitOfWork) : IEnquiryService
    {
        private readonly IRepository<Enquiry> _enquiryRepository = unitOfWork.Repository<Enquiry>();
        public async Task<long> CreateEnquiry(EnquiryRequestModel editModel)
        {
            var domain = Enquiry.CreateDomain(editModel);
            await _enquiryRepository.Save(domain);
            return domain.Id;
        }
    }
}
