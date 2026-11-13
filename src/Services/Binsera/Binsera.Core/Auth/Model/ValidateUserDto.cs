using Binsera.Core.Auth.Domain;

namespace Binsera.Core.Auth.Model
{
    public class ValidateUserDto
    {
        public bool IsAccountActive { get; set; }
        public string Messages { get; set; }
        public long UserId { get; set; }
        public RoleType RoleId { get; set; }
        public string EmailId { get; set; }
        public long? RegNo { get; set; }
        public Guid? MemberGraphId { get; set; }
    }

    public class AuthenicatedUserRequest
    {
        public string Token { get; set; }
        public string SessionId { get; set; }
    }
}
