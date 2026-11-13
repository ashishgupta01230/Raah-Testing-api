using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Core.Auth.Model
{
    public class UserLoginHistoryResponse
    {
        public string UserName { get; set; }
        public DateTime LoginDateTime { get; set; }
        public string Browser { get; set; }
        public string ClientIp { get; set; }
    }

    public class UserLoginHistoryRequest : PaginationModel
    {
        public string UserName { get; set; }
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
