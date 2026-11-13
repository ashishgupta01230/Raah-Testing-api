using Binsera.Core.Enquiries.Models;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Enquiries.Domain
{
    public class Enquiry : AuditableDomainBase
    {
        private Enquiry()
        {
        }

        private Enquiry(EnquiryRequestModel editModel)
        {
            Name = editModel.Name;
            Email = editModel.Email;
            Phone = editModel.Phone;
            //City = editModel.City;
            //State = editModel.State;
            //ZipCode = editModel.ZipCode;
            EnquiryType = editModel.EnquiryType;
            Remarks = editModel.Message;
        }
        public string Name { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string Phone { get; private set; } = null!;
        //public string? City { get; private set; } = null!;
        //public string? State { get; private set; } = null!;
        //public string? ZipCode { get; private set; } = null!;
        public EnquiryType EnquiryType { get; private set; } = EnquiryType.Student;
        public EnquiryStatus Status { get; private set; } = EnquiryStatus.Pending;
        public string Remarks { get; private set; }
        public static Enquiry CreateDomain(EnquiryRequestModel editModel)
        {
            return new Enquiry(editModel);
        }

    }

}
