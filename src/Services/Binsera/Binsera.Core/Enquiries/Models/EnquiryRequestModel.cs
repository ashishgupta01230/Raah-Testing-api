using Binsera.Core.Enquiries.Domain;

namespace Binsera.Core.Enquiries.Models
{
    public record EnquiryRequestModel
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public string Message { get; set; }
        //public required string? City { get; set; }
        //public required string? State { get; set; }
        //public required string? ZipCode { get; set; }
        public EnquiryType EnquiryType { get; set; }
    }
}
