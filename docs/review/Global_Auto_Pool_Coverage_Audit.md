# Global Auto Pool — coverage against the requirements doc

Every rule in [`Global_Auto_Pool_Scenario_Complete_Flow.md`](../requirements/Global_Auto_Pool_Scenario_Complete_Flow.md), part by part, and where it is covered. Gaps are called out rather than glossed.

**Summary: the pool engine itself is fully covered. Three real gaps sit outside it** — what triggers pool entry, withdrawals, and the business rules the doc itself lists as unconfirmed.

Legend: **✅** covered and tested · **⚙️** implemented, guaranteed structurally rather than by a test · **⚠️** partial · **❌** not built

---

## PART A — the nine confirmed rules

| # | Rule | | Where |
|---|---|---|---|
| 1 | ₹200 joining, ₹15 into the pool | ✅ | Kit 2 is ₹200 (`JoiningPackage.KitId`); only approving *that* kit grants pool entry — `Approving_anything_else_does_not_carry_the_joining_kit_id` |
| 2 | Joining ₹15 → 1 step up | ✅ | `Joining_contribution_goes_one_step_up_to_the_parent` |
| 3 | Level N complete → N+1 steps up | ✅ | `The_level_table_satisfies_the_invariants_PART_F_is_built_on` |
| 4 | Binary tree, max 2 children | ⚙️ | Positions `2N`/`2N+1` make a third child unrepresentable |
| 5 | Placement: topmost empty row, left to right | ✅ | `Parent_matches_the_doc_tree_diagram` (all of A–O) |
| 6 | Seven seed accounts X1–X7 | ✅ | `SeedGlobalAutoPoolSeedAccounts.sql`; `Seven_seeds_are_exactly_enough_for_the_root_members_deepest_transfer` |
| 7 | Seeds are collectors — no level logic, money to company | ✅ | `Money_reaching_a_seed_account_ends_the_chain` |
| 8 | Two wallets — Upgrade (fuel), Main (earnings) | ✅ | `Upgrade_wallet_movements_never_turn_into_main_wallet_movements` |
| 9 | Seven levels: 2, 4, 8, 16, 32, 64, 128 | ✅ | Seeded in `PoolLevelMaster`; `Each_level_is_fed_by_exactly_the_number_of_members_it_requires` |

## PART B — setup

| Rule | | Where |
|---|---|---|
| X7→X1 chain above the first member | ✅ | Seed script; verified in `member_Master` |
| Seeds skip level completion logic | ✅ | The cascade returns at a seed recipient |
| Every rupee they receive → company wallet | ✅ | `CompanyWalletCredit` effect |
| Only the first member sits under X1 | ⚙️ | Only position 1 has no in-tree parent |
| Why exactly seven | ✅ | `Seven_seeds_are_exactly_enough_...` — the first member's L6 goes 7 steps up |

## PART C — placement rule

| Rule | | Where |
|---|---|---|
| Fill order B→C→D→…→O | ✅ | `Parent_matches_the_doc_tree_diagram` |
| Breadth-first, a row completes before the next starts | ✅ | Level-order numbering; `Depth_counts_rows_below_the_root_member` |
| **Sponsor has nothing to do with placement** | ⚙️ | Sponsor is not a parameter to `PoolTreePosition` or `PoolCascade` at all. `IntroRegNo` is stored and never read by the pool. |

## PART D — the two money paths

| Rule | | Where |
|---|---|---|
| Joining ₹15 → 1 step up, counts in receiver's level 1 | ✅ | `A_members_joining_fee_never_enters_their_own_wallet` |
| Level upgrade → N+1 steps up | ✅ | `Transfers_from_the_root_member_walk_the_seed_chain` |
| Transfer table (L1 ₹30/2 … L7 ₹0) | ✅ | Seeded and asserted row by row |

## PART E — the "N+1 steps" proof

| Rule | | Where |
|---|---|---|
| Level N is filled by exactly 2^N members | ✅ | `Each_level_is_fed_by_exactly_the_number_of_members_it_requires` reproduces the doc's whole proof table from arithmetic |
| Row-one members' ₹30 goes to X1, **not** the first member | ✅ | `Row_one_transfers_go_to_the_company_not_the_root_member` — the case the doc explicitly warns about |

## PART F — the level master table

| Rule | | Where |
|---|---|---|
| All eight rows' amounts | ✅ | Seeded; verified in SQL and in `The_level_table_satisfies_the_invariants...` |
| `IncomingTotal = Members × Amount` | ✅ | Computed, not stored — cannot drift |
| `MainWalletIncome = Incoming − Transfer` | ✅ | Same |
| Chain: level N's transfer = level N+1's contribution | ✅ | Asserted across all levels |

## PART G — the two wallets

| Rule | | Where |
|---|---|---|
| Upgrade credited by downstream contributions | ✅ | `UpgradeWalletCredit` |
| Upgrade debited when own level upgrades | ✅ | `Completing_a_level_drains_exactly_what_that_level_took_in` |
| **Upgrade is not withdrawable** | ✅ | Kept in `PoolWalletTransaction`, never `Member_Account` — `WithdrawalService` sums that table, so fuel would otherwise inflate withdrawable balance |
| Main credited with the remainder on completion | ✅ | Mirrored into `Member_Account` |
| **Main debited for a withdrawal** | ✅ | Debited when the request is made, returned on rejection — see Gap 2 below |

## PART H — the fifteen-member scenario

| | Where |
|---|---|
| ✅ | `PoolCascadeScenarioTests` replays it whole and asserts the doc's exact stated outcome: first member on level 3 with ₹10, B–G on level 2, H–O on level 1, ₹215 to the company, nothing left as fuel. Also reproduced against the real database. |

## PART I — the cascade

| Rule | | Where |
|---|---|---|
| One join can chain through several members | ✅ | `The_root_members_level_two_is_filled_by_the_four_members_two_rows_below` |
| Whole cascade in one transaction | ⚙️ | `PoolPlacementService` — one serializable transaction, rolled back on any failure |
| Guard against an endless chain | ✅ | Hard step ceiling in `PoolCascade` |

## PART J — technical rules

| # | Rule | | Where |
|---|---|---|---|
| 1 | Idempotency key per financial event | ✅ | `Replaying_the_same_join_produces_identical_keys`; unique index on `(IdempotencyKey, WalletType)`; replaying a real placement three times against the database changed nothing |
| 2 | System account → company wallet, skip levels | ✅ | `Money_reaching_a_seed_account_ends_the_chain` |
| 3 | No recipient above X7 → company wallet | ✅ | `Seven_seeds_are_exactly_enough_...` |
| 4 | `UplinePath (Member, Ancestor, Depth)`, indexed | ✅ | Written at placement, depths 1–8; unique index on `(MemberRegNo, Depth)` |
| 5 | Amounts always server-side from the level table | ✅ | Nothing in the pool hardcodes a rupee value |
| 6 | Upgrade Wallet never negative | ✅ | `Upgrade_wallets_never_go_negative`, and per member in `Completing_a_level_drains_exactly...` |
| 7 | Never consume a downstream event twice | ✅ | Placement guard plus the unique index; verified by replay |

## PART J2 — data access

| Guidance | | Notes |
|---|---|---|
| Phase 1 entirely in EF Core / LINQ | ✅ | No stored procedures written |
| Explicit transaction with isolation level | ✅ | Serializable |
| Row locks during the cascade | ⚙️ | A single global `sp_getapplock` instead — coarser, simpler, certainly correct at this volume |
| Idempotency check before each credit | ⚠️ | The guard is at the top (already placed → skip) plus the unique index, rather than a check per credit. Equivalent in effect; noted because it differs from the doc's wording |
| Never hardcode an amount | ✅ | All from `PoolLevelMaster` |
| Phase 2: load test for double payment and deadlock | ❌ | Not run — see Gap 4 |

## PART K — reality of reaching each level

| Level | Verified how |
|---|---|
| L1–L5 | Simulated in the suite, up to 65,535 members, checked against PART K's thresholds and PART F's cumulative income |
| L6 | Simulated out of band — 41,94,303 members, 2.3 s, income reaching ₹71,33,510 exactly as PART F predicts |
| L7 | **Cannot be simulated** (53 crore members). Its distinguishing behaviour — a final level transfers nothing and ends the chain — is tested with a level table whose first level is final, exercising the same branch |

## PART L — points the doc itself leaves open

| # | Question | | Notes |
|---|---|---|---|
| 1 | Upgrade automatic or a button? | ✅ | Automatic, as the doc suggests — the member has no choice to make |
| 2 | Withdrawal rules: minimum, TDS, KYC, timing | ❌ | Gap 3 |
| 3 | Inactive member's balance | ❌ | Gap 3 |
| 4 | Refund / reversal / failed payment | ❌ | Gap 3 |
| 5 | Excess Upgrade Wallet carried forward | ✅ | There is never any excess — a level completes at exactly the amount it needs, and the tests assert the wallet drains to zero |

---

## The gaps, in priority order

### ~~Gap 1 — pool entry is not tied to the joining order~~ — **fixed**

`OrderApprovedDomainEventHandler` used to set `ConfPayNo = 1` for the member of **any** approved order, which is what triggers placement. It never checked which kit was ordered — `SubscriptionKitId` was consulted when an order was *created*, never when it was approved. So approving a repurchase, or anything else, granted pool entry and took the pool's ₹15 regardless of what was actually paid.

It now returns unless the approved order is for the joining package. The kit id moved out of `OrderService`'s private constant into `Binsera.Core/Orders/JoiningPackage.cs`, since two places need it.

**Protection against a duplicate approval is five layers deep:**

1. `OrderService.CreateOrderAsync` — a member cannot open a second joining order while one is live (only after the first is rejected or cancelled).
2. `OrderService.ApproveOrderAsync` — refuses anything not in `PaymentReceived`, so an already-approved order throws.
3. `OrderApprovedDomainEventHandler` — ignores orders that are not the joining package.
4. `PoolPlacementService` — skips a member who is already placed, re-checked *after* taking the placement lock.
5. Unique index on `(IdempotencyKey, WalletType)` — the backstop, if all of the above were somehow bypassed.

Layer 4 was verified against the real database: replaying a placement three times changed nothing (ledger 32 rows, ₹10 to Main, ₹215 to Company, 119 UplinePath rows, all unmoved).

One thing to be aware of: **`Order.Approve()` itself has no guard** — it will run again and raise a second event. The status check in `OrderService` is the only thing preventing that, so nothing else should ever call `Approve()` directly. Pinned by `The_aggregate_itself_does_not_guard_re_approval`.

### ~~Gap 2 — withdrawal is not wired to the pool~~ — **fixed**

It turned out to be larger than "wire it up". `WithdrawalService` had a single method, `CreateRequestAsync`; there was no approve or reject, and **no withdrawal endpoint existed anywhere in the API**. `WithdrawalRequest` had properly guarded `Approve`/`Reject`/`MarkProcessing`/`MarkCompleted` methods that nothing ever called.

And no code path wrote a **debit** at all. Balance is read as credits minus debits on `Member_Account`, so it never moved — a member with ₹10 could submit a ₹10 request, then another, then another, each passing the balance check. Harmless while that table was empty; not harmless now that pool income is credited there.

Built per [`../plan/Withdrawal_Plan.md`](../plan/Withdrawal_Plan.md): the money is held when the request is made rather than when it is approved, and returned if the request is rejected. Holding this way keeps balance a plain credits-minus-debits sum, so every existing reader stays correct without knowing withdrawals exist. Every movement is written to both `Member_Account` and the pool ledger, so the two views cannot drift. Approval moves no money — by then the balance is already committed.

Verified against the real database: balance ₹10 → request ₹10 → balance ₹0 → **a second ₹10 request is refused** → reject → balance back to ₹10, with matching `WithdrawalHold`/`WithdrawalReversal` rows in `Member_Account` and `WDHOLD_n`/`WDREV_n` in the pool ledger.

Still out of scope, and in the backlog because they need business answers: minimum amount, TDS, KYC gating, bank-file export, UTR capture.

### Gap 3 — the doc's own open questions — **moved to backlog**

PART L items 2, 3 and 4 — withdrawal minimums, TDS thresholds, KYC gating, an inactive member's balance, and refund/reversal rules — need business answers before any code. Parked in [`../plan/Backlog.md`](../plan/Backlog.md) with the context each one will need.

Worth reading the refund entry there before promising anything to the client: placement is irreversible by design, so a true reversal is not available — only a compensating adjustment.

### Gap 4 — no load test

PART J2's phase 2: simulate concurrent registrations and watch for double payment or deadlock. Not done. Correctness under concurrency currently rests on the global lock being correct by construction rather than on measurement, and this is what should decide whether anything needs moving into stored procedures.

### Also outstanding (from the implementation, not the doc)

- The real trigger path — admin approves → Outbox → placement — has never run, because there is no admin account. The placement service was verified by direct invocation.
- No API endpoint exposes any of this yet.
- No admin view of members who are active but unplaced, which is how a dead-lettered placement would surface.
