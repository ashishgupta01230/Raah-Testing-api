using Binsera.Core.Auth.Domain;
using Binsera.Core.Members.Contracts;
using Binsera.Core.Members.Domain;
using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Application.Attributes;
using HautSphere.BuildingBlocks.Domain.Models;
using HautSphere.BuildingBlocks.Persistence.Contracts;
using HautSphere.BuildingBlocks.Persistence.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Binsera.Application.Members.Services
{
    [AutoRegister(includeInterfaces: true)]
    public class MemberDataService(IUnitOfWork unitOfWork) : IMemberDataService
    {
        private readonly IRepository<Member> _memberRepository = unitOfWork.Repository<Member>();
        private readonly IRepository<UserLogin> _userLoginRepository = unitOfWork.Repository<UserLogin>();
        public async Task<IPagedList<MemberItemModel>> GetMembers(MemberFilterModel filterModel)
        {
            var query = (from m in _memberRepository.TableNoTracking
                         where m.RegNo != 1 && m.RegNo != 2 && (string.IsNullOrEmpty(filterModel.Keyword) || m.FName.Contains(filterModel.Keyword)
                         || m.LoginID.Contains(filterModel.Keyword))
                               && (!filterModel.Status.HasValue || m.ConfPayNo == filterModel.Status)
                               && (!filterModel.SponsorId.HasValue || m.IntroRegNo == filterModel.SponsorId)
                               orderby m.RegDate descending
                         select new MemberItemModel
                         {
                             Id = m.Id,
                             Name = m.FName + ' ' + m.LName,
                             RegNo = m.LoginID,
                             JoiningDate = m.RegDate,
                             MobileNo = m.Mobile,
                             Email = m.EmailID,
                             Status = m.ConfPayNo ?? 0,
                             ReferralMemberId = m.IntroRegNo,
                             IsAcountActive = m.ConfPayNo == 1,
                             MemberId = m.MemberGraphId,
                             ProfileImage = m.ProfileImage,
                         });
            var data = await query.ToPagedListAsync(filterModel.PageIndex, filterModel.PageSize);

            var referalIds = data.Items.Select(x => x.ReferralMemberId).ToList();
            var referalMembers = await _memberRepository.TableNoTracking.Where(x => x.RegNo != null && referalIds.Contains(x.RegNo.Value))
                .Select(x => new { x.RegNo, x.FName, x.LName, x.LoginID, x.MemberGraphId, x.ProfileImage }).ToListAsync();
            foreach (var referalMember in data.Items)
            {
                var referal = referalMembers.FirstOrDefault(x => x.RegNo == referalMember.ReferralMemberId);
                if (referal != null)
                {
                    referalMember.ReferralMember = referal.FName + ' ' + referal.LName;
                    referalMember.ReferralRegNo = referal.LoginID;
                    referalMember.ReferralId = referal.MemberGraphId;
                    referalMember.ReferralProfileImage = referal.ProfileImage;
                }
            }
            return data;
        }
        public async Task<MemberLoginDetailsModel> GetMemberLoginDetails(long memberId)
        {
            var member = await _memberRepository.TableNoTracking.Where(x => x.Id == memberId)
                .Select(x => new MemberLoginDetailsModel
                {
                    FirstName = x.FName,
                    LastName = x.LName,
                    Password = string.Empty,
                    LoginId = x.LoginID,
                    Id = x.Id,

                }).FirstAsync();

            var loginDetails = await _userLoginRepository.TableNoTracking.Where(x => x.UserName == member.LoginId).Select(x => new { x.Password, x.UserName, x.IsActive }).FirstOrDefaultAsync();
            member.Password = loginDetails?.Password ?? string.Empty;
            member.IsActive = loginDetails?.IsActive ?? false;
            return member;
        }

    }
}
