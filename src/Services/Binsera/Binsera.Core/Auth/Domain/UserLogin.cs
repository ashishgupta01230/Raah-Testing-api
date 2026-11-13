using Binsera.Core.Auth.Events;
using HautSphere.BuildingBlocks.Domain;

namespace Binsera.Core.Auth.Domain
{
    public class UserLogin : AuditableDomainBase
    {
        private UserLogin()
        {
        }
        public UserLogin(string userName, string email, RoleType roleType)
        {
            UserName = userName;
            Email = email;
            Password = GenerateSixDigitPassword();
            RoleId = roleType;
            IsActive = true;
        }
        public string UserName { get; private set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public RoleType RoleId { get; set; }
        public bool IsActive { get; set; }

        public static UserLogin CreateDomain(string userName, string email, RoleType roleType)
        {
            return new UserLogin(userName, email, roleType);
        }
        public void UpdateStatus(bool active)
        {
            IsActive = active;
            AddDomainEvent(new UserLoginStatusChangedMemberStatusDomainEvent(UserName, IsActive));
        }
        private string GenerateSixDigitPassword()
        {
            Random random = new Random();
            int password = random.Next(100000, 1000000); // 100000 to 999999
            return password.ToString();
        }
    }
}
