using Binsera.Application.Genealogy.Models;

namespace Binsera.Application.Genealogy
{
    public interface IGenealogyService
    {
        Task<Guid?> GetCompanyRootIdAsync();
        Task<GenealogyNodeResponse> GetNodeAsync(Guid memberId, Guid currentMemberId, bool isAdmin);
        Task<List<GenealogyNodeResponse>> ExpandNodeAsync(Guid memberId, Guid currentMemberId, bool isAdmin);
        Task<GenealogySearchResponse> SearchMembersAsync(Guid currentMemberId, bool isAdmin, string keyword, int page = 1, int pageSize = 20);
        Task<GenealogyPathResponse> GetMemberPathAsync(Guid memberId, Guid currentMemberId, bool isAdmin);
        Task<GenealogyChildrenResponse> GetChildrenPagedAsync(Guid memberId, Guid currentMemberId, bool isAdmin, int page = 1, int pageSize = 50);
    }
}
