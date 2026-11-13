/* =============================================================================
   Global Auto Pool -- the Rs.200 joining package
   Ref: docs/requirements/Global_Auto_Pool_Scenario_Complete_Flow.md (PART A rule 1)
        docs/plan/Global_Auto_Pool_Placement_Plan.md (section 7a)

   Pool entry happens when an admin approves the member's joining order, so the
   joining package has to exist as a real orderable kit. Before this script,
   Kit_Master held only "Join Free" (kid 1, price 0) and the Rs.5,000 Shiv Kripa
   Rudraksha Kit (kid 2) left over from the previous business.

   Kid 2 is rewritten in place rather than a new row being added, because two
   places already point at it by id and would otherwise need changing:

     - OrderService.SubscriptionKitId = 2, which also enforces "one open
       subscription order per member"
     - KitMasterService lists kits WHERE KitActive = 1 AND Id > 1

   KitActive stays 1 so it keeps appearing in that list. PV and BV are zeroed:
   the pool has no points/volume concept, and these values flow onto the member
   row via Member.SetKitMaster.

   Safe to re-run.
   ============================================================================= */

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM Kit_Master WHERE kid = 2)
BEGIN
    RAISERROR('Kit_Master row kid = 2 not found; OrderService.SubscriptionKitId expects it.', 16, 1);
    RETURN;
END

UPDATE Kit_Master
SET kitcode     = 'ABZ-200',
    kitdesc     = 'Global Auto Pool Joining Package',
    kitProducts = 'Global Auto Pool Joining',
    kitprice    = 200.0000,
    kitpv       = 0.0000,
    KitBV       = 0.0000,
    boardBV     = 0.0000,
    KitActive   = 1
WHERE kid = 2;

SELECT kid, kitcode, kitdesc, kitProducts, kitpv, KitBV, kitprice, kitActive, joinactive
FROM Kit_Master
ORDER BY kid;
