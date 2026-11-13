using Binsera.Core.Members.Enums;

namespace Binsera.Core.Members.Models
{
    public class MemberDocumentDto
    {
        public long Id { get; set; }
        public string DocumentName { get; set; } = null!;
        public long MediaId { get; set; }
        public MemberDocumentStatus Status { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
