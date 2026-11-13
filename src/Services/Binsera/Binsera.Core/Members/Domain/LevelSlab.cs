namespace Binsera.Core.Members.Domain
{
    /// <summary>
    /// Read-only mapping over the legacy dbo.Level_Slab table -- no known primary key
    /// column, so this is a keyless EF entity (HasNoKey), queried via BinseraDbContext
    /// directly rather than the DomainBase/IRepository convention.
    /// </summary>
    public class LevelSlab
    {
        public int Levels { get; set; }
        public decimal LevelPerIdRate { get; set; }
        public bool IsActive { get; set; }
        public string CommissionType { get; set; }
    }
}
