using System.Text.Json.Serialization;

namespace HautSphere.BuildingBlocks.Domain
{
    public class UserModel
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }

    public class UserActionModel
    {
        public string CreatedByName { get; set; }
        [JsonIgnore]
        public long CreatedById { get; set; }
    }
}
