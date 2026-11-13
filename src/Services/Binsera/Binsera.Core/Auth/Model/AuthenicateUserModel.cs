namespace Binsera.Core.Auth.Model
{
    public class AuthenicateUserModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool KeepMeSignedIn { get; set; }
    }
    public class ChangePasswordModel
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
    }
    public class UserTokens
    {
        public string AccessToken { get; set; }
        public string UserName { get; set; }
        public string RefreshToken { get; set; }
        public long Id { get; set; }
        public string EmailId { get; set; }
        public Guid SessionId { get; set; }
        public DateTime ExpiredTime { get; set; }
        public int RoleId { get; set; }
        public string RedirectUrl { get; set; }
        public long? RegNo { get; set; }
        public Guid? MemberGraphId { get; set; }
    }
}
