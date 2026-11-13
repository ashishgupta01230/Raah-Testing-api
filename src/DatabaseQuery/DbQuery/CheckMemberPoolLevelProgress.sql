/* =============================================================================
   Global Auto Pool -- check how many levels a member has completed
   Ref: docs/requirements/Global_Auto_Pool_Scenario_Complete_Flow.md (PART C, F)

   Change @RegNo below and run. Three result sets:
     1. Placement -- where this member sits in the tree.
     2. Level-by-level progress -- Member_Level joined to PoolLevelMaster, so you
        see "how many downline reached me at this level" next to "how many are
        required", and whether that level is complete (mlActive = 1).
     3. Wallet ledger -- every PoolWalletTransaction row touching this member's
        own Upgrade/Main wallets (MemberRegNo = @RegNo), in order.

   "How many levels completed" = COUNT(*) in result set 2 WHERE mlActive = 1.
   ============================================================================= */

SET NOCOUNT ON;

DECLARE @RegNo BIGINT = 8;

-- 1. Placement
SELECT Id, RegNo, ConfPayNo, PPositionM AS [Position], ParentRegNoB AS ParentRegNo,
       ParentSideB AS Side, DepthsB AS Depth
FROM member_Master
WHERE RegNo = @RegNo;

-- 2. Level-by-level progress
SELECT
    lvl.LevelNo,
    lvl.RequiredMembers,
    ISNULL(ml.tlCount, 0)                                   AS ReceivedSoFar,
    lvl.ContributionPerMember,
    lvl.RequiredMembers * lvl.ContributionPerMember          AS IncomingTotalWhenComplete,
    lvl.TransferAmount                                       AS SentUpwardWhenComplete,
    (lvl.RequiredMembers * lvl.ContributionPerMember) - lvl.TransferAmount AS MainWalletIncomeWhenComplete,
    lvl.StepsUp,
    CASE WHEN ISNULL(ml.mlActive, 0) = 1 THEN 'Complete' ELSE 'In progress' END AS Status
FROM PoolLevelMaster lvl
LEFT JOIN Member_Level ml ON ml.RegNo = @RegNo AND ml.Levels = lvl.LevelNo
ORDER BY lvl.LevelNo;

-- 2b. Levels completed -- the single number
SELECT COUNT(*) AS LevelsCompleted
FROM Member_Level
WHERE RegNo = @RegNo AND mlActive = 1;

-- 3. Wallet ledger for this member (Upgrade + Main only -- Company rows have
--    MemberRegNo = NULL by design, see PoolWalletTransaction.MemberRegNo).
SELECT Id, WalletType, Credit, Debit, LevelNo, SourceRegNo, IdempotencyKey, OccurredOn
FROM PoolWalletTransaction
WHERE MemberRegNo = @RegNo
ORDER BY Id;
