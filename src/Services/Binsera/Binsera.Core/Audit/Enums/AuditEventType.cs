namespace Binsera.Core.Audit.Enums
{
    public enum AuditEventType
    {
        // Values for removed modules are left as gaps rather than renumbered/reused, since this
        // enum is persisted as an int in historical audit log rows.
        CommissionRunCompleted = 7,
        CommissionRunApproved = 8,
        WalletCredited = 11,
        WalletDebited = 12
        // 13 = retired BulkBusinessApproved (same reason as above).
    }
}
