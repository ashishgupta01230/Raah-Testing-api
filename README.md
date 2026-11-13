# Raah API

Backend for **Raah's Global Auto Pool** — a binary-tree matrix pool system where members join for a flat ₹200, get auto-placed into a shared binary tree, and earn as their placement tree fills up through 7 levels.

> Full business-rules source of truth: [`docs/requirements/Global_Auto_Pool_Scenario_Complete_Flow.md`](docs/requirements/Global_Auto_Pool_Scenario_Complete_Flow.md). This README is a summary — that doc governs in case of any difference.

## Solution layout

```
src/
  BuildingBlocks/          Shared cross-cutting libraries (Domain, Application, Persistence,
                            Logger, Configuration, QueryBuilder, Neo4j config)
  Services/Binsera/         The main service (project name "Binsera")
    Binsera.API/             ASP.NET Core Web API — controllers, background services, DI wiring
    Binsera.Application/     Application/business logic (Members, Orders, Incomes, Payments,
                              Genealogy, Enquiries, Media, Locations, Master, Website, Auth)
    Binsera.Core/             Domain entities and domain events (Members, Orders, Payments,
                              Branches, Organizations, Outbox, ...)
    Binsera.Persistence/      EF Core DbContext, entity configurations, migrations, interceptors
  DatabaseQuery/DbQuery/     Raw SQL objects (legacy stored procedures, DB scripts)
docs/
  requirements/              Business rules & requirement docs (source of truth)
  postman/                  Postman collections for manual/API testing of each phase
```

## What the system does

1. A **Member** joins by paying a flat **₹200 joining package**, of which **₹15 goes into the pool**.
2. The member is **auto-placed** into a shared binary tree — always at the topmost-available empty slot, filled left to right, breadth-first. Placement is independent of who referred them; `SponsorId` is stored for record-keeping only.
3. Seven **company seed accounts** (`X1`–`X7`, `IsSystemAccount = true`) sit above the first real member in a straight chain, so every one of a member's 7 possible level-completions always has a recipient upline, even before enough real members exist.
4. As members join beneath a given member, that member's **level counters** fill up (2, 4, 8, 16, 32, 64, 128 members for levels 1–7). Each completed level pays a fixed amount **N+1 steps up the tree** (see the level table below) and moves the member to the next level.
5. Each member has **two wallets**: an **Upgrade Wallet** (fuel — incoming downstream contributions that pay for the member's own next-level upgrade; not withdrawable) and a **Main Wallet** (the member's real earnings — the remainder left over once a level's transfer amount is deducted; withdrawable, subject to a Withdrawal → Admin Approval → Bank Transfer flow, not yet built).
6. Money paid to a system account (`X1`–`X7`) goes straight to the **company wallet**, and system accounts never run level-completion logic.

## Level & payout table

| Level | Members needed | Transfer (on completion) | Steps up | Main Wallet gain |
|---|---|---|---|---|
| L1 | 2 | ₹30 | 2 | ₹0 |
| L2 | 4 | ₹110 | 3 | ₹10 |
| L3 | 8 | ₹860 | 4 | ₹20 |
| L4 | 16 | ₹12,760 | 5 | ₹1,000 |
| L5 | 32 | ₹1,08,320 | 6 | ₹3,00,000 |
| L6 | 64 | ₹1,00,000 | 7 | ₹68,32,480 |
| L7 | 128 | ₹0 (final level) | — | ₹1,28,00,000 |

**Master rule:** completing Level N sends the transfer amount **N+1 steps up** the tree — this is what guarantees each level's member-count target is hit exactly (see Part E of the requirements doc for the proof). A single member joining can trigger a **cascade** of level-completions up the tree, so the whole chain must commit as one atomic database transaction.

## Technical rules that shape the implementation

- **Idempotency** — every financial event has a unique key (`JOIN_{MemberId}`, `LVLUP_{MemberId}_{LevelNo}`) so retries never double-pay.
- **Server-side amounts only** — level thresholds and transfer amounts always come from a `LevelMaster` table read on the server; the client never supplies them.
- **Upline lookup** — a `UplinePath (MemberId, AncestorId, Depth)` table, indexed on `(MemberId, Depth)`, resolves "N steps up" without walking the tree at request time.
- **Concurrency-critical operations** (placement, level-upgrade cascade, upgrade-wallet credit) need explicit transactions and row locking (`UPDLOCK`/`ROWLOCK`) to prevent race-condition double-payment when members join concurrently — see the requirements doc's guidance on when to reach for a stored procedure instead of plain EF Core/LINQ.
- **Wallet never goes negative**, and a downstream event is never consumed twice (`SourceEventIds` tracked).

## Not yet built — Payout / Withdrawal

Main Wallet balance → member submits a Withdrawal Request → Admin Approves → amount transferred to bank account. Minimum withdrawal amount, TDS handling, KYC requirement, and processing time are still pending business decisions (see the requirements doc, Part L).

## Legal note

Before the auto-pool structure goes live in production, compliance with India's Direct Selling Rules, 2021 and the Prize Chits and Money Circulation Schemes (Banning) Act, 1978 must be confirmed with a legal advisor. This is a business/legal task, not an engineering one — HAUT IT Solutions develops software against the given business rules; the business plan and its legal compliance remain the client's responsibility.
