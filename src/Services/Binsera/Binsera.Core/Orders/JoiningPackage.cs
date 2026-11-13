namespace Binsera.Core.Orders
{
    /// <summary>
    /// The Global Auto Pool joining package -- `Kit_Master` row 2, code `ABZ-200`, ₹200.
    ///
    /// Approving an order for this kit is what puts a member into the pool, so the id matters in
    /// two places: when an order is created (one open joining order per member) and when one is
    /// approved (only this kit grants pool entry). See
    /// src/DatabaseQuery/DbQuery/SetGlobalAutoPoolJoiningKit.sql, which defines the row.
    /// </summary>
    public static class JoiningPackage
    {
        public const long KitId = 2;
    }
}
