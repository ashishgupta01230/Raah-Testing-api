using Binsera.Core.Members.Enums;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Members.Domain
{
    public class MemberDocument : AuditableDomainBase
    {
        

        

        public long MemberId { get; private set; }
        public long DocumentId { get; private set; }
        public long MediaId { get; private set; }
        public MemberDocumentStatus Status { get; private set; }
        public string? RejectionReason { get; private set; }

        private MemberDocument() { }

        private MemberDocument(long memberId, long documentId, long mediaId)
        {
            MemberId = memberId;
            DocumentId = documentId;
            MediaId = mediaId;
            Status = MemberDocumentStatus.Pending;
        }

        public Document Document { get; private set; } = null!;

        public static MemberDocument CreateDomain(long memberId, long documentId, long mediaId)
        {
            return new MemberDocument(memberId, documentId, mediaId);
        }

        public void Approve(long adminId)
        {
            Status = MemberDocumentStatus.Approved;
            ModifiedBy = adminId;
            ModifiedDate = DateTime.UtcNow;
        }

        public void Reject(long adminId, string reason)
        {
            Status = MemberDocumentStatus.Rejected;
            RejectionReason = reason;
            ModifiedBy = adminId;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}
