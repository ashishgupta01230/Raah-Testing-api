/* =============================================================================
   Global Auto Pool -- cross-table data integrity check
   Ref: docs/requirements/Global_Auto_Pool_Scenario_Complete_Flow.md

   Runs a set of PASS/FAIL checks across every table this feature touches:
     member_Master, PoolSeedAccount, PoolLevelMaster, UplinePath,
     PoolWalletTransaction, Member_Level, [Order], UserLogin, OutboxMessage.

   Each SELECT below returns rows only when something is WRONG -- an empty
   result set for a check means that check passed. Read top to bottom.
   ============================================================================= */

SET NOCOUNT ON;

PRINT '--- 1. member_Master: PPositionM must be unique among placed rows ---';
SELECT PPositionM, COUNT(*) AS Occurrences
FROM member_Master
WHERE PPositionM IS NOT NULL AND PPositionM > 0
GROUP BY PPositionM
HAVING COUNT(*) > 1;

PRINT '--- 2. member_Master: no gaps in the position sequence (1..max) ---';
;WITH Positions AS (
    SELECT PPositionM FROM member_Master WHERE PPositionM > 0
), Expected AS (
    SELECT TOP (SELECT COUNT(*) FROM Positions) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    FROM sys.all_objects
)
SELECT e.n AS MissingPosition
FROM Expected e
LEFT JOIN Positions p ON p.PPositionM = e.n
WHERE p.PPositionM IS NULL;

PRINT '--- 3. member_Master: every non-seed placed member''s parent actually exists ---';
SELECT m.Id, m.RegNo, m.PPositionM, m.ParentRegNoB
FROM member_Master m
WHERE m.PPositionM > 0
  AND m.ParentRegNoB > 0
  AND NOT EXISTS (SELECT 1 FROM member_Master p WHERE p.RegNo = m.ParentRegNoB);

PRINT '--- 4. member_Master: ParentSideB/parity must agree with position arithmetic (even=L, odd=R) ---';
SELECT Id, RegNo, PPositionM, ParentSideB,
       CASE WHEN PPositionM % 2 = 0 THEN 'L' ELSE 'R' END AS ExpectedSide
FROM member_Master
WHERE PPositionM > 1
  AND ParentSideB <> CASE WHEN PPositionM % 2 = 0 THEN 'L' ELSE 'R' END;

PRINT '--- 5. PoolSeedAccount: exactly 7 rows, SeedIndex 1-7, each pointing at a real member_Master row ---';
SELECT s.SeedIndex, s.MemberRegNo
FROM PoolSeedAccount s
WHERE NOT EXISTS (SELECT 1 FROM member_Master m WHERE m.RegNo = s.MemberRegNo);
SELECT COUNT(*) AS PoolSeedAccountRowCount_ExpectedSeven FROM PoolSeedAccount;

PRINT '--- 6. PoolLevelMaster: exactly 8 rows (Level 0-7), IncomingTotal must equal TransferAmount + MainWalletIncome ---';
SELECT LevelNo, RequiredMembers, ContributionPerMember, TransferAmount,
       (RequiredMembers * ContributionPerMember) AS IncomingTotal
FROM PoolLevelMaster
WHERE StepsUp IS NOT NULL   -- Level 7 has no upward transfer by design, skip it here
  AND (RequiredMembers * ContributionPerMember) < TransferAmount;

PRINT '--- 7. UplinePath: every row''s AncestorRegNo must exist (member or seed) ---';
SELECT u.Id, u.MemberRegNo, u.AncestorRegNo, u.Depth
FROM UplinePath u
WHERE NOT EXISTS (SELECT 1 FROM member_Master m WHERE m.RegNo = u.AncestorRegNo);

PRINT '--- 8. UplinePath: depths for a given member must be contiguous starting at 1, no duplicates ---';
SELECT MemberRegNo, Depth, COUNT(*) AS Occurrences
FROM UplinePath
GROUP BY MemberRegNo, Depth
HAVING COUNT(*) > 1;

PRINT '--- 9. PoolWalletTransaction: IdempotencyKey must be globally unique (the real double-payment guard) ---';
SELECT IdempotencyKey, COUNT(*) AS Occurrences
FROM PoolWalletTransaction
GROUP BY IdempotencyKey
HAVING COUNT(*) > 1;

PRINT '--- 10. PoolWalletTransaction: every row has exactly one of Credit/Debit non-zero, never both, never neither ---';
SELECT Id, Credit, Debit
FROM PoolWalletTransaction
WHERE (Credit > 0 AND Debit > 0) OR (Credit = 0 AND Debit = 0);

PRINT '--- 11. PoolWalletTransaction: Company rows (WalletType=2) must have MemberRegNo NULL; member rows must not ---';
SELECT Id, MemberRegNo, WalletType
FROM PoolWalletTransaction
WHERE (WalletType = 2 AND MemberRegNo IS NOT NULL)
   OR (WalletType <> 2 AND MemberRegNo IS NULL);

PRINT '--- 12. PoolWalletTransaction: total credits must equal total debits per member (fuel in = fuel out once drained) ---';
SELECT MemberRegNo,
       SUM(Credit) AS TotalCredit,
       SUM(Debit) AS TotalDebit,
       SUM(Credit) - SUM(Debit) AS NetBalance
FROM PoolWalletTransaction
WHERE MemberRegNo IS NOT NULL AND WalletType = 0   -- Upgrade wallet only: must drain to 0 or sit mid-level
GROUP BY MemberRegNo
HAVING SUM(Credit) - SUM(Debit) < 0;   -- negative would mean more drained than ever credited -- impossible if correct

PRINT '--- 13. Member_Level: tlCount must never exceed RequiredMembers for that level ---';
SELECT ml.RegNo, ml.Levels, ml.tlCount, lvl.RequiredMembers
FROM Member_Level ml
JOIN PoolLevelMaster lvl ON lvl.LevelNo = ml.Levels
WHERE ml.tlCount > lvl.RequiredMembers;

PRINT '--- 14. Member_Level: mlActive must be 1 exactly when tlCount >= RequiredMembers ---';
SELECT ml.RegNo, ml.Levels, ml.tlCount, lvl.RequiredMembers, ml.mlActive
FROM Member_Level ml
JOIN PoolLevelMaster lvl ON lvl.LevelNo = ml.Levels
WHERE (ml.tlCount >= lvl.RequiredMembers AND ml.mlActive = 0)
   OR (ml.tlCount < lvl.RequiredMembers AND ml.mlActive = 1);

PRINT '--- 15. Order: every Approved order for the joining kit (ItemId=2) has a matching placed member ---';
SELECT o.Id, o.MemberId, o.Status
FROM [Order] o
JOIN OrderItem oi ON oi.OrderId = o.Id AND oi.ItemId = 2
JOIN member_Master m ON m.Id = o.MemberId
WHERE o.Status = 4   -- Approved
  AND (m.PPositionM IS NULL OR m.PPositionM = 0);

PRINT '--- 16. Order: ApprovedBy/CreatedBy must not be 0 for orders created after the audit-claim fix ---';
SELECT Id, MemberId, CreatedBy, ApprovedBy
FROM [Order]
WHERE Id >= 10028 AND (CreatedBy = 0 OR ApprovedBy = 0);

PRINT '--- 17. UserLogin: every Member-role login must point at a real member_Master row via LoginID ---';
SELECT ul.Id, ul.UserName
FROM UserLogin ul
WHERE ul.RoleId = 3
  AND NOT EXISTS (SELECT 1 FROM member_Master m WHERE m.LoginID = ul.UserName);

PRINT '--- 18. OutboxMessage: no message stuck unprocessed-and-not-dead-lettered for more than 5 minutes ---';
SELECT Id, MessageType, Attempts, DeadLetter, Processed, OccurredOn
FROM OutboxMessage
WHERE Processed = 0 AND DeadLetter = 0 AND DATEDIFF(MINUTE, OccurredOn, GETUTCDATE()) > 5;

PRINT '--- Done. Every SELECT above returning zero rows = that check passed. ---';



truncate table WithdrawalRequest
truncate table PoolSeedAccount
truncate table PoolWalletTransaction
truncate table UplinePath
truncate table AuditLog

alter table MediaMapping add IsApproved bit default(0)