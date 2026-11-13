using Binsera.Application.Media.Contracts;
using Binsera.Application.Members.Contracts;
using Binsera.Core.Members.Domain;
using Binsera.Core.Members.Enums;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Members.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class MemberDocumentService(
        IUnitOfWork unitOfWork,
        IMediaService mediaService) : IMemberDocumentService
    {
        private readonly IRepository<Document> _documentRepository = unitOfWork.Repository<Document>();
        private readonly IRepository<MemberDocument> _memberDocumentRepository = unitOfWork.Repository<MemberDocument>();
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();

        public async Task<List<DocumentDto>> GetActiveDocumentsAsync()
        {
            return await _documentRepository.TableNoTracking
                .Where(x => x.IsActive)
                .Select(x => new DocumentDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description
                })
                .ToListAsync();
        }

        public async Task<long> SaveDocument(UploadMemberDocumentRequest request, long memberId)
        {
            var document = await _documentRepository.TableNoTracking
                .FirstOrDefaultAsync(x => x.Id == request.DocumentId && x.IsActive);

            if (document == null)
                throw new KeyNotFoundException("Document type not found or inactive.");

            var memberDocument = MemberDocument.CreateDomain(memberId, request.DocumentId, request.MediaId);
            await _memberDocumentRepository.Save(memberDocument);

            await mediaService.AttachMediaAsync(
                new[] { request.MediaId },
                "MemberDocument",
                memberDocument.Id,
                CancellationToken.None);

            return memberDocument.Id;
        }

        public async Task<List<MemberDocumentDto>> GetMemberDocumentsAsync(long memberId)
        {
            return await _memberDocumentRepository.TableNoTracking
                .Include(x => x.Document)
                .Where(x => x.MemberId == memberId)
                .Select(x => new MemberDocumentDto
                {
                    Id = x.Id,
                    DocumentName = x.Document.Name,
                    MediaId = x.MediaId,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason,
                    UploadedAt = x.CreatedDate,
                    ReviewedAt = x.ModifiedDate
                })
                .ToListAsync();
        }

        public async Task<List<AdminMemberDocumentDto>> GetMemberDocumentsByGraphIdAsync(Guid memberGraphId)
        {
            var regNo = await _memberRepository.TableNoTracking
                .Where(x => x.MemberGraphId == memberGraphId)
                .Select(s => s.RegNo)
                .FirstOrDefaultAsync();

            if (regNo == 0)
                return new List<AdminMemberDocumentDto>();

            return await _memberDocumentRepository.TableNoTracking
                .Include(x => x.Document)
                .Where(x => x.MemberId == regNo)
                .Select(x => new AdminMemberDocumentDto
                {
                    Id = x.Id,
                    MemberId = x.MemberId,
                    DocumentName = x.Document.Name,
                    MediaId = x.MediaId,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason,
                    UploadedAt = x.CreatedDate,
                    ReviewedAt = x.ModifiedDate,
                    ReviewedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        public async Task<List<AdminMemberDocumentDto>> GetPendingDocumentsAsync(long? memberId = null)
        {
            var query = _memberDocumentRepository.TableNoTracking
                .Include(x => x.Document)
                .Where(x => x.Status == MemberDocumentStatus.Pending);

            if (memberId.HasValue)
                query = query.Where(x => x.MemberId == memberId.Value);

            return await query
                .Select(x => new AdminMemberDocumentDto
                {
                    Id = x.Id,
                    MemberId = x.MemberId,
                    DocumentName = x.Document.Name,
                    MediaId = x.MediaId,
                    Status = x.Status,
                    RejectionReason = x.RejectionReason,
                    UploadedAt = x.CreatedDate,
                    ReviewedAt = x.ModifiedDate,
                    ReviewedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        public async Task ApproveDocumentAsync(long id, long adminId)
        {
            var memberDocument = await _memberDocumentRepository.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (memberDocument == null)
                throw new KeyNotFoundException($"Member document with id {id} not found.");

            if (memberDocument.Status != MemberDocumentStatus.Pending)
                throw new InvalidOperationException("Only pending documents can be approved.");

            memberDocument.Approve(adminId);
            await _memberDocumentRepository.Save(memberDocument);
        }

        public async Task RejectDocumentAsync(long id, long adminId, RejectDocumentRequest request)
        {
            var memberDocument = await _memberDocumentRepository.Table
                .FirstOrDefaultAsync(x => x.Id == id);

            if (memberDocument == null)
                throw new KeyNotFoundException($"Member document with id {id} not found.");

            if (memberDocument.Status != MemberDocumentStatus.Pending)
                throw new InvalidOperationException("Only pending documents can be rejected.");

            memberDocument.Reject(adminId, request.Reason);
            await _memberDocumentRepository.Save(memberDocument);
        }
    }
}
