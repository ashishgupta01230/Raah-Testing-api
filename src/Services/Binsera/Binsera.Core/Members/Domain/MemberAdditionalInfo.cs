using Binsera.Core.Members.Models;
using HautSphere.BuildingBlocks.Domain;
using System.Text.Json;

namespace Binsera.Core.Members.Domain
{
    public class MemberAdditionalInfo : DomainBase
    {
        private MemberAdditionalInfo()
        {
        }
        public MemberAdditionalInfo(long organizationId, AdditionalInfoModel additionalInfo)
        {
            OrganizationId = organizationId;
            MemberInfo = JsonSerializer.Serialize(additionalInfo);
        }
        public long OrganizationId { get; set; }
        public string MemberInfo { get; set; }
        public static MemberAdditionalInfo Create(long organizationId, AdditionalInfoModel additionalInfo)
        {
            return new MemberAdditionalInfo(organizationId, additionalInfo);
        }
    }
}
