/* =============================================================================
   Global Auto Pool -- indexes on the legacy tables the pool reads
   Ref: docs/plan/Global_Auto_Pool_Placement_Plan.md

   member_Master and Member_Level are both excluded from EF migrations, so EF
   cannot create these. Without them every single placement scans a table, and
   both tables grow with the membership -- so this gets steadily worse rather
   than staying merely slow.

   1. member_Master (PPositionM)
      Read twice per placement: once for MAX(PPositionM) to allocate the next
      position, and once to resolve the joining member's ancestors. Filtered to
      exclude the seed accounts and any member not yet placed, which is most of
      the table early on.

   2. Member_Level (RegNo, Levels)
      This table had no indexes at all. The pool loads every level row for the
      joining member's ancestors on each placement, and writes roughly eight
      rows per member over their lifetime.

   3. Company_account (transDate)
      Only ever inserted to by the pool, but the admin money-flow screens read
      it by date.

   Safe to re-run.

   Note the SET options below. The member_Master index is filtered, and SQL
   Server refuses to create a filtered index unless QUOTED_IDENTIFIER and
   ANSI_NULLS are ON. Application connections have them on already; sqlcmd does
   not, so running this file without them fails with error 1934.
   ============================================================================= */

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_member_Master_PPositionM' AND object_id = OBJECT_ID('member_Master'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_member_Master_PPositionM
        ON member_Master (PPositionM)
        INCLUDE (regNo)
        WHERE PPositionM > 0;

    PRINT 'Created IX_member_Master_PPositionM';
END
ELSE PRINT 'IX_member_Master_PPositionM already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Member_Level_RegNo_Levels' AND object_id = OBJECT_ID('Member_Level'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Member_Level_RegNo_Levels
        ON Member_Level (RegNo, Levels)
        INCLUDE (tlCount, tlIncome, mlActive);

    PRINT 'Created IX_Member_Level_RegNo_Levels';
END
ELSE PRINT 'IX_Member_Level_RegNo_Levels already exists';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Company_account_transDate' AND object_id = OBJECT_ID('Company_account'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Company_account_transDate
        ON Company_account (transDate);

    PRINT 'Created IX_Company_account_transDate';
END
ELSE PRINT 'IX_Company_account_transDate already exists';
