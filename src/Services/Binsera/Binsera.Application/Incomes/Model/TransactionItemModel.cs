#nullable disable
using HautSphere.BuildingBlocks.Domain.Models;

namespace Binsera.Application.Incomes.Model
{
    public class TransactionItemModel
    {
        public long Id { get; set; }
        public string TransType { get; set; }
        public string Remark { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransDate { get; set; }
    }
    public class TransactionFilterModel : PaginationModel
    {
        public DateTime? TransDate { get; set; }
        public string Particular { get; set; }

    }
}
