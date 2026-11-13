namespace Binsera.Core.Members.Models
{
    public class KitItemModel
    {
        public long Id { get; set; }
        public string KitCode { get;  set; }
        public decimal KitPrice { get; set; }
        public string KitDesc { get; set; }
        public string KitProducts { get; set; }
        public decimal KitBV { get; set; }
        public decimal KitPV { get; set; }
    }
}
