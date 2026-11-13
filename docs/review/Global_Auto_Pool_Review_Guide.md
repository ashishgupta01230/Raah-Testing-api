# Global Auto Pool — how the system actually runs

Start here. This walks the flow from the first HTTP call to money landing in wallets, then points at the code behind each step.

Business rules: [`../requirements/Global_Auto_Pool_Scenario_Complete_Flow.md`](../requirements/Global_Auto_Pool_Scenario_Complete_Flow.md).
Decisions and why: [`../plan/Global_Auto_Pool_Placement_Plan.md`](../plan/Global_Auto_Pool_Placement_Plan.md).

---

## The short version

**Nothing about the pool happens at registration.** A member joins the pool only when an admin approves their ₹200 joining order. That single click sets off everything else.

```
POST /api/admin/orders/{id}/approve      <-- the trigger, everything follows from here
```

---

## The full journey

### Step 1 — Member registers

```
POST /api/members/registration          (no auth)
```

Creates the `member_Master` row, generates the login id (`ABZ000008`), sets `RegNo = Id`, creates the `UserLogin`, and returns a JWT so the member is logged in straight away.

At this point the member exists and can log in, but **is not in the pool**: `ConfPayNo` is null and `PPositionM` is 0. They have not paid.

### Step 2 — Member places the joining order

```
POST /api/orders                        (member JWT)
```

The ₹200 joining package is `Kit_Master` row 2 (`ABZ-200`), which `OrderService` knows as `SubscriptionKitId`. It also enforces one open joining order per member.

```json
{
  "orderType": 1,
  "paymentMode": 2,
  "items": [{ "itemId": 2, "itemName": "Global Auto Pool Joining Package", "quantity": 1, "unitPrice": 200 }],
  "offlinePayment": { "paymentMethod": 1, "referenceNumber": "TXN123", "paymentDate": "2026-01-01" }
}
```

Order status: `PendingPayment`.

### Step 3 — Admin confirms the money arrived

```
POST /api/admin/orders/{id}/verify-payment      (admin JWT)
```

Status: `PendingPayment` → `PaymentReceived`. Still nothing pool-related.

### Step 4 — Admin approves. This is the trigger.

```
POST /api/admin/orders/{id}/approve             (admin JWT)
```

What that one call sets off, in order:

```
AdminOrderController.ApproveOrder
  └─ OrderService.ApproveOrderAsync          guards status == PaymentReceived
       └─ order.Approve(adminId)             raises OrderApprovedDomainEvent
            │
            └─ UnitOfWork.CommitAsync()      dispatches events in repeated passes
                 │
                 ├─ OrderApprovedDomainEventHandler          (runs in the transaction)
                 │     └─ member.UpdatePayNo(1)              member is now paid + active
                 │          └─ raises MemberPayNoUpdatedDomainEvent
                 │             (a nested event -- the commit loop runs again to catch it)
                 │
                 └─ MemberPayNoUpdatedDomainEvent has an external handler,
                    so an OutboxMessage row is written in the same transaction
                        │
                        COMMIT  <-- order approved and member activated, atomically
```

The HTTP call returns here. The member is active; placement has not happened yet.

### Step 5 — The background worker picks it up

`OutboxPublisher` polls every 5 seconds, finds the message, and calls:

```
MemberActivatedPoolPlacementHandler
  └─ PoolPlacementService.PlaceMemberAsync(memberId)
```

So placement happens *shortly after* approval, in its own transaction, and retries up to 5 times if it fails. That is deliberate — a failing cascade should not roll back a legitimate order approval. The cost is a few seconds' delay, and the risk that a message dead-letters and leaves a member active but unplaced. (Nothing surfaces that yet — see "What is not built".)

### Step 6 — Placement and the cascade

Inside `PlaceMemberAsync`, all in one serializable transaction behind a global lock:

```
  take sp_getapplock                so two joins cannot claim the same slot
  re-check member is still unplaced
  position = MAX(PPositionM) + 1    join order is placement order
  write placement                   parent, side, depth onto member_Master
  write UplinePath rows             depths 1..8
  run the cascade                   pure arithmetic, returns a list of effects
  apply the effects                 ledger, level counters, wallets
  COMMIT
```

**Where the member lands.** The doc's rule is "topmost empty slot, left to right", which is level-order fill of a complete binary tree. So position `N`'s parent is `N/2`, left if `N` is even, right if odd — no tree search, ever. The first member hangs off X1.

```
position   1    2    3    4    5    6    7   ...
member     A    B    C    D    E    F    G
parent     X1   A    A    B    B    C    C
side       L    L    R    L    R    L    R
```

**Where the money goes.** ₹15 travels one step up from the joiner. If that lands on a real member it fills their current level; when a level fills, that member's transfer travels further up (level N goes N+1 steps), which can fill *another* member's level, and so on. The chain stops when it reaches an X1–X7 seed account, where the money becomes company income.

Worked example, the moment member O joins (the doc's PART H):

```
O joins  ->  Rs.15 to G          G's level 1 is now 2/2, complete
         ->  G transfers Rs.30 two steps up  ->  A
                                  A's level 2 is now 4/4, complete
         ->  A transfers Rs.110 three steps up  ->  X3  ->  company
         ->  A keeps Rs.10 in Main Wallet      <- A's first earnings
```

One join, three members' state changed, two payments made — all in one transaction.

---

## Where each step lives

| Step | File |
|---|---|
| Approve endpoint | `Binsera.API/Controllers/Orders/AdminOrderController.cs` |
| Raises the event | `Binsera.Application/Orders/Services/OrderService.cs` → `ApproveOrderAsync` |
| Activates the member | `Binsera.Application/Orders/EventHandlers/OrderApprovedDomainEventHandler.cs` |
| Event dispatch + Outbox | `Binsera.Application/UnitOfWork.cs` → `CommitAsync` |
| Background delivery | `Binsera.API/BackgroundServices/OutboxPublisher.cs` |
| **Pool entry point** | `Binsera.Application/Pool/EventHandlers/MemberActivatedPoolPlacementHandler.cs` |
| **The whole flow** | `Binsera.Application/Pool/Services/PoolPlacementService.cs` |
| Loading tree state | `Binsera.Application/Pool/Services/PlacementContext.cs` |
| Position arithmetic | `Binsera.Core/Pool/PoolTreePosition.cs` |
| The cascade | `Binsera.Core/Pool/PoolCascade.cs` |
| Amounts | `Binsera.Core/Pool/Domain/PoolLevelMaster.cs` (seeded, 8 rows) |

---

## Reviewing the code

Once the flow above makes sense, read in this order. The first two files are where a mistake costs real money.

**1. `PoolTreePosition.cs`** — the arithmetic. `ResolveStepsUp` is the one to read closely: inside the tree it halves per step; past the first member it walks the X1–X7 chain; above X7 it returns nothing.

**2. `PoolCascade.cs`** — the chain reaction. It reads nothing and writes nothing, just returns a list of effects. That is what makes the doc's fifteen-member scenario testable with no database.

**3. `PoolPlacementService.cs`** — read `PlaceMemberAsync` top to bottom; it is the whole flow in one method.

**4. `PlacementContext.cs`** — why every member the cascade could touch is loaded up front (a cascade only ever climbs, so the reachable set is the joiner's ancestors).

**Then the tests.** They encode the doc's own worked examples:

```bash
dotnet test tests/Binsera.Pool.Tests/Binsera.Pool.Tests.csproj
```

63 tests, about a second, no database. `PoolCascadeScenarioTests.cs` replays PART H and asserts its exact stated outcome — A on level 3 with ₹10, B–G on level 2, H–O on level 1, ₹215 to the company. If you read one test file, read that one.

Two worth a sceptical look:
- `Each_level_is_fed_by_exactly_the_number_of_members_it_requires` — reproduces the doc's PART E proof table (L1=2 … L7=128) from arithmetic alone.
- `Row_one_transfers_go_to_the_company_not_the_root_member` — the case the doc explicitly warns about: B and C's ₹30 goes to X1, **not** to A.

### How deep the cascade has actually been run

Worth knowing precisely, because the PART H scenario alone only reaches level 2 — fifteen members is all it needs, and a mistake in level 4's amounts would not show up there.

| Level | Members needed (PART K + 1) | How it was verified |
|---|---|---|
| L1 | 3 | simulated, in the suite |
| L2 | 15 | simulated, in the suite (this is PART H) |
| L3 | 127 | simulated, in the suite |
| L4 | 2,047 | simulated, in the suite |
| L5 | 65,535 | simulated, in the suite (~150 ms) |
| L6 | 41,94,303 | simulated **out of band** — 2.3 s, 347 MB. Too expensive to keep in the suite. |
| L7 | 53,68,70,910 | **cannot be simulated.** See below. |

`PoolDeepLevelTests.The_first_member_climbs_the_levels_exactly_when_PART_K_says_they_should` grows a real tree to 65,535 members and checks, at each of PART K's thresholds, that the first member is on the level PART K predicts with the cumulative Main Wallet income PART F specifies — ₹10, ₹30, ₹1,030, ₹3,01,030. The out-of-band level 6 run continued the same numbers to ₹71,33,510.

Level 7 would need 53 crore members, so its *completion* has never executed and cannot be made to. What is tested instead is the thing that makes level 7 different — that a final level transfers nothing and ends the chain — using a level table whose first level is final. That exercises the same branch in the cascade. Separately, `Level_seven_is_configured_as_the_final_level` pins the level 7 row itself.

---

## Changes to existing code

- **`Member.PlaceInPool`** — writes the legacy `*B` placement columns plus `PPositionM`.
- **`MemberLevel`** — gained `MlActive` and mutators. Its config had a real latent bug: it never ignored `IsDeleted`, a column that table does not have, so *any* query against it failed. That is why nothing used it.
- **`MemberAccount.Credited`** — Main Wallet income is written directly, not through `sp_IA2`, which deducts TDS, a handling charge, and another 15% from members without a PAN. That would quietly shave 10–25% off amounts the plan defines exactly, and the seed accounts have no PAN at all.
- **`UserLoginStatusChangedMemberStatusDomainEventHandler` deleted** — activating a login no longer sets `ConfPayNo`. Pool entry follows payment only.

---

## See it in the database

X1–X7 are `member_Master` rows 1–7. There are also **15 test members `POOL001`–`POOL015`** I placed to verify the persistence layer.

The tree, against the doc's PART C diagram:

```sql
SELECT PPositionM AS pos, fName, regNo, ParentRegNoB AS parent, ParentSideB AS side, DepthsB AS depth
FROM member_Master WHERE PPositionM > 0 ORDER BY PPositionM;
```

The money, against PART H — expect ₹10 to Main, ₹215 to Company, Upgrade netting to zero:

```sql
SELECT WalletType, SUM(Credit) AS credited, SUM(Debit) AS debited
FROM dbo.PoolWalletTransaction GROUP BY WalletType;   -- 0=Upgrade, 1=Main, 2=Company
```

Clearing the test data when you are done:

```sql
DELETE FROM UplinePath;
DELETE FROM PoolWalletTransaction;
DELETE FROM Member_Level;
DELETE FROM Member_Account WHERE transType = 'PoolLevelIncome';
DELETE FROM Company_account WHERE transType = 'PoolTransfer';
DELETE FROM member_Master WHERE LoginID LIKE 'POOL%';
```

---

## Performance

**Addressed:**

- Position allocation reads `MAX(PPositionM)`, not `COUNT`. They are equal — positions are contiguous and one transaction means no gaps — but `MAX` over an index is a seek to its end while `COUNT` walks every placed member. At a million members that is a constant-time lookup versus a full scan, on every join.
- `PlacementContext` loads all ancestors in one query rather than walking the tree row by row.
- `AddGlobalAutoPoolIndexes.sql` — `member_Master` had no index on `PPositionM`, and `Member_Level` had **no indexes at all**. Both are read on every placement and both grow with the membership. They live in a script rather than a migration because both tables are excluded from EF migrations.

**Accepted deliberately:**

- `sp_getapplock` serialises all placements globally. Simple and certainly correct; at ₹200 joinings the throughput ceiling is nowhere near binding. If joins ever arrive fast enough to hurt, revisit this first.
- `UplinePath` writes up to 8 rows per member — pure redundancy against the position arithmetic. It exists because the requirements doc asks for it.

**Not measured:** no load test yet. Simulating concurrent registrations and watching for double payment or deadlock is still outstanding, and is what should decide whether any of this needs stored procedures.

---

## What is not built

- **Any pool API endpoint.** Placement writes correctly but nothing exposes it — no member summary, tree, team, wallet or upgrade-history endpoint.
- **Admin visibility.** A dead-lettered Outbox message leaves a member active but unplaced, silently. An "active but unplaced" view is required, not optional.
- **The real trigger path, tested end to end.** `UserLogin` is empty — there is no admin account — so step 4 onwards has never actually run. I verified the placement service by invoking it directly instead. **This is the biggest untested gap.**
- **UI.** Both panels are placeholder shells since the Business module was removed.
