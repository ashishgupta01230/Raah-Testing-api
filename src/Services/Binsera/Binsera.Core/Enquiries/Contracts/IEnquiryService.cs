using Binsera.Core.Enquiries.Models;

namespace Binsera.Core.Enquiries.Contracts
{
    public interface IEnquiryService
    {
        Task<long> CreateEnquiry(EnquiryRequestModel editModel);
    }
}
