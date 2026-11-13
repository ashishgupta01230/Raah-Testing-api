/* =============================================================================
   Global Auto Pool -- company seed accounts X1..X7
   Ref: docs/requirements/Global_Auto_Pool_Scenario_Complete_Flow.md (PART B)
        docs/plan/Global_Auto_Pool_Placement_Plan.md (section 3)

   The pool needs a seven-deep chain of company accounts sitting above the first
   real member, so that every one of that member's seven level-upgrade transfers
   has a recipient: their L6 transfer travels 7 steps up, and X7 has to be there
   to receive it.

       X7   <- top of the chain, no parent
        |
       X6
        |
       ...
        |
       X1   <- the first real member is placed directly under this one

   These are collectors, not members: no login, no wallet, no level logic. Every
   rupee that reaches them belongs to the company wallet. They are stored in
   member_Master purely so the tree has real rows to hang placement pointers off
   and so the admin tree view can render the chain -- the same way ids 1-3 were
   already used for system accounts before this.

   Because they sit above the binary tree rather than inside it, they carry no
   position in the level-order numbering: PPositionM stays 0, and that numbering
   starts at 1 with the first real member.

   Safe to re-run -- each block below skips itself if already done.

   PREREQUISITE for the member_Master block: that table must be empty, so that
   IDENTITY_INSERT can place these at ids 1-7 and the first real member lands at
   id 8. This script does NOT clear the table itself; deleting members is a
   deliberate, manual act and does not belong in a script that could be pointed
   at the wrong database.

   Also populates PoolSeedAccount (SeedIndex 1-7 -> MemberRegNo 1-7). This is the
   table PlacementContext.SeedRegNo() actually reads at runtime to resolve "which
   RegNo is X1" -- member_Master alone is not enough; placement throws
   InvalidOperationException("Seed account X{n} is missing") without this table
   populated, even though the X1..X7 rows exist in member_Master. The two blocks
   are independent on purpose, so re-running this script can top up
   PoolSeedAccount alone on a database where member_Master already has data.
   ============================================================================= */

SET NOCOUNT ON;
-- member_Master has a filtered index (see AddGlobalAutoPoolIndexes.sql), so SQL Server
-- checks these SET options at compile time for the whole batch -- even inside a branch
-- that ends up skipped at runtime -- or the INSERT below fails with error 1934.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Block 1: member_Master rows 1-7 (X1..X7).
IF NOT EXISTS (SELECT 1 FROM member_Master WHERE LoginID IN ('X1','X2','X3','X4','X5','X6','X7'))
BEGIN
    IF EXISTS (SELECT 1 FROM member_Master)
    BEGIN
        RAISERROR('member_Master is not empty. The seed chain must occupy ids 1-7; clear the table first.', 16, 1);
        RETURN;
    END

    BEGIN TRANSACTION;

    SET IDENTITY_INSERT member_Master ON;

    INSERT INTO member_Master
        (id, LoginID, regNo, fName, lName, IntroRegNo,
         ParentRegNoB, ParentSideB, ParentPosB, DepthsB, PPositionM,
         ConfPayNo, Active, RegDate, PayMode, PaymentMode, UpdateBy, UpdateDate)
    VALUES
        (1, 'X1', 1, 'X1', 'Pool', 2, 2, 'L', 1, 6, 0, 1, 1, GETDATE(), 'System', 'System', 'System', GETDATE()),
        (2, 'X2', 2, 'X2', 'Pool', 3, 3, 'L', 1, 5, 0, 1, 1, GETDATE(), 'System', 'System', 'System', GETDATE()),
        (3, 'X3', 3, 'X3', 'Pool', 4, 4, 'L', 1, 4, 0, 1, 1, GETDATE(), 'System', 'System', 'System', GETDATE()),
        (4, 'X4', 4, 'X4', 'Pool', 5, 5, 'L', 1, 3, 0, 1, 1, GETDATE(), 'System', 'System', 'System', GETDATE()),
        (5, 'X5', 5, 'X5', 'Pool', 6, 6, 'L', 1, 2, 0, 1, 1, GETDATE(), 'System', 'System', 'System', GETDATE()),
        (6, 'X6', 6, 'X6', 'Pool', 7, 7, 'L', 1, 1, 0, 1, 1, GETDATE(), 'System', 'System', 'System', GETDATE()),
        (7, 'X7', 7, 'X7', 'Pool', 0, 0, NULL, 0, 0, 0, 1, 1, GETDATE(), 'System', 'System', 'System', GETDATE());

    SET IDENTITY_INSERT member_Master OFF;

    COMMIT;

    PRINT 'member_Master: X1..X7 inserted at ids 1-7.';
END
ELSE
BEGIN
    PRINT 'member_Master: X1..X7 already present -- nothing to do.';
END

-- Block 2: PoolSeedAccount (SeedIndex -> MemberRegNo), independent of block 1.
IF NOT EXISTS (SELECT 1 FROM PoolSeedAccount)
BEGIN
    INSERT INTO PoolSeedAccount (SeedIndex, MemberRegNo, IsDeleted, CreatedDate, CreatedBy)
    VALUES
        (1, 1, 0, GETUTCDATE(), 0),
        (2, 2, 0, GETUTCDATE(), 0),
        (3, 3, 0, GETUTCDATE(), 0),
        (4, 4, 0, GETUTCDATE(), 0),
        (5, 5, 0, GETUTCDATE(), 0),
        (6, 6, 0, GETUTCDATE(), 0),
        (7, 7, 0, GETUTCDATE(), 0);

    PRINT 'PoolSeedAccount: SeedIndex 1-7 mapped to RegNo 1-7.';
END
ELSE
BEGIN
    PRINT 'PoolSeedAccount: already populated -- nothing to do.';
END

SELECT id, LoginID, regNo, IntroRegNo, ParentRegNoB, DepthsB, PPositionM, ConfPayNo
FROM member_Master
WHERE id BETWEEN 1 AND 7
ORDER BY id;

SELECT SeedIndex, MemberRegNo FROM PoolSeedAccount ORDER BY SeedIndex;
