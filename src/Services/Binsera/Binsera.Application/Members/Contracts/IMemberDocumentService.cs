using Binsera.Core.Members.Models;

namespace Binsera.Application.Members.Contracts
{
    public interface IMemberDocumentService
    {
        Task<List<DocumentDto>> GetActiveDocumentsAsync();
        Task<long> SaveDocument(UploadMemberDocumentRequest request, long memberId);
        Task<List<MemberDocumentDto>> GetMemberDocumentsAsync(long memberId);
        Task<List<AdminMemberDocumentDto>> GetMemberDocumentsByGraphIdAsync(Guid memberGraphId);
        Task<List<AdminMemberDocumentDto>> GetPendingDocumentsAsync(long? memberId = null);
        Task ApproveDocumentAsync(long id, long adminId);
        Task RejectDocumentAsync(long id, long adminId, RejectDocumentRequest request);
    }
}
