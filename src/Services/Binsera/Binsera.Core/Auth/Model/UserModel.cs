using Binsera.Core.Auth.Domain;

namespace Binsera.Core.Auth.Model
{
    public class UserModel
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public RoleType RoleType { get; set; }
        public string MemberId { get; set; }
        public string EmailId { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string UserName { get; set; }
        public string ProfileImageId { get; set; }
    }
}
