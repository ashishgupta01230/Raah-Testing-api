# M3 — Matching Engine — API Plan

| | |
|---|---|
| **Module** | M3 — Matching Engine (spec `docs/spec/03_M3_Matching_Engine.md`) |
| **Document type** | API implementation plan: algorithm, contracts, data model, jobs, triggers, validation, ranking, tests |
| **Build order** | #3 after Foundation + M1 + M2 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M3 is a **system process**, not a member feature. It:

1. Evaluates a pair (one Saathi + one Raahi) across eligible Active routes.
2. Applies the nine matching steps from the spec deterministically.
3. Classifies each leg as Full Route, Useful Segment or No match.
4. Produces one **lead per side**, with the stored explanation (shared points, common days, time gap, route versions).
5. Ranks leads with the six fixed keys.
6. Reacts to triggers: activation, Active edit, reactivation, deactivation, block removal, Under Review release, manual recalculation.

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Route data and route status | M2 |
| Member and route statuses | Foundation / M2 |
| Lead screens, filters, dismiss, connect | M4 |
| Pending requests, connections | M5 |
| Blocks and reports | M8 / M12 |
| Admin Matching Health screen | M11 |
| Config screens, Test Match screen, Recalculate button | M13 |
| Notifications on new leads | M9 |
| Reports (RPT-6, RPT-9) | M15 |

**No member-facing endpoints.** M3 exposes internal contracts plus a small set of Admin-facing endpoints that are mounted by M11/M13 controllers (marked below).

---

## 2. Dependency and module-boundary handling

M3 is built **before** M4, M5, M8, M11, M13 but must feed them. Respect dependency direction: M3 depends on M2 and Foundation, and owns the contracts others consume.

### 2.1 Contracts consumed by M3

| Contract | Owner | Purpose |
|---|---|---|
| `IRouteSnapshotReader` | M2 | Load route snapshots (cities, legs, points, days, times, role, status) for a route or the candidate set |
| `IMemberStatusReader` | Foundation | Bulk member statuses (Active / Restricted gate) |
| `IBlockGateway` | M3-owned (no-op until M8) | `IsBlockedPairAsync(a, b)`; `GetBlockedMemberIdsAsync(memberId)` |
| `IAppConfigReader` | Foundation | CFG-1, CFG-4, CFG-22 |
| `IPointMatchService` | Shared (M2) | Text similarity with exact-number rule (D-22) |
| `IAuditService` | Foundation | Audit engine-level setting-triggered runs; lead events are **not** platform audit (BR-12.12) |

### 2.2 Contracts provided by M3 (consumed later)

| Contract | Purpose | Consumer |
|---|---|---|
| `IMatchingEngineGateway` (declared in M2) | Enqueue matching after a route change | M2 |
| `ILeadWithdrawalGateway` (declared in M2) | Withdraw leads for a route | M2 |
| `IMatchingEngine` | `RunForRouteAsync`, `RunForPairAsync`, `RunAllAsync` | M8 (unblock), M12 (review release), M13 (recalculate) |
| `ILeadWriter` | Upsert/revive/withdraw lead rows | M4 (replace/extend) |
| `IMatchingHealthQuery` | Aggregations for Matching Health | M11 |
| `ITestMatchService` | Run the calculator on sample data | M13 |

Rules:

- M2's no-op `IMatchingEngineGateway` / `ILeadWithdrawalGateway` are **replaced** by M3 implementations; keep exactly one registration.
- M4 will own lead **lifecycle** (Viewed, Dismissed, Requested, Connect); M3 owns lead **creation/update/withdrawal from matching**. `ILeadWriter` is the seam.
- M3 never edits routes (BR-12.8), never contacts members, never books.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controllers (Admin) | Inherit `ApiControllerBase`; `[AutoRegister]` services; route under `admin/matching` |
| DI | `[AutoRegister(includeInterfaces: true)]`; no manual `AddScoped` |
| Response | `Ok(payload)` → `ResponseFilterAttribute` envelope |
| Non-2xx | `BadRequest(new { code, message })` / `StatusCode(409, ...)`; never `Forbid()` / `Challenge()` |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new EF-managed tables; `member_Master` untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id`, audit columns, `IsDeleted`, per-column indexes |
| Background jobs | Plain `BackgroundService` with config-driven `Enabled`/poll interval, scoped services via `IServiceScopeFactory` (see `MonthlyCommissionCalculationJob`) |
| Validation | No FluentValidation; validate in services |
| Current user | Admin id from `ApiControllerBase` for manual recalc; matching itself is system |

---

## 4. Matching algorithm (normative)

Implement as a **pure, side-effect-free** `MatchCalculator` over immutable snapshots, then a thin orchestrator persists results. This guarantees determinism (BR-12.1) and makes every rule unit-testable.

### 4.1 Inputs

```
RouteSnapshot
  memberId, routeId, routeVersionId
  role                 Saathi | Raahi
  memberStatus         Active | Restricted | ...
  routeStatus          Active | ...
  startCityId, endCityId
  vehicle?             (not used in matching)
  going: LegSnapshot?
  return: LegSnapshot?

LegSnapshot
  points        [ { order:int, name:string } ]      // order = direction
  days          set<DayOfWeek>                       // 1..7
  departureMin  int                                  // minutes from midnight

MatchConfig
  timeToleranceMinutes   CFG-1
  minSharedPoints        CFG-4
  textThresholdPercent   CFG-22
```

### 4.2 Step 1 — Eligibility

All must hold:

| Test | Rule |
|---|---|
| Opposite roles | one Saathi, one Raahi (A-13, BR-12.2) |
| Route status | both Active (BR-12.3) |
| Member status | both Active **or** Restricted (A-32) |
| City overlap | `{start,end}` of one ∩ `{start,end}` of the other ≠ ∅ (BR-12.13) |
| Not blocked | neither direction blocked (BR-12.9) |

Failing any → return `NoMatch` with reason code; **do not** create/update leads.

### 4.3 Step 2 — Leg pairing

`Going ↔ Going`, `Return ↔ Return` (A-17). A leg absent on either side is skipped. Evaluate each existing pair independently; a pair qualifies if **either** leg matches.

### 4.4 Step 3 — Common days

`commonDays = daysA ∩ daysB`; require `count ≥ 1`, else the leg fails.

### 4.5 Step 4 — Time

```
raw = |departureA - departureB|
diff = min(raw, 1440 - raw)        // handles midnight (SC-12.5)
pass = diff <= CFG-1               // inclusive (AC-12.1)
```

Uses each leg's typed departure time only (BR-12.5); where the member joins the route is irrelevant.

### 4.6 Step 5 — Aligning points

For every point in leg A versus every point in leg B:

- Normalize: trim, collapse spaces, case-insensitive.
- Similarity from `IPointMatchService` (same rule as M2, D-22).
- Numbers inside a point must be **exactly** equal; if not, similarity = 0 (A-21).
- A short name inside a longer name is not a match (the shared rule already handles this; keep the check).
- Consider a pair a candidate when similarity ≥ CFG-22.
- Assign **one-to-one**, highest similarity first (greedy), so a point pairs with only one point on the other side.

Result: `pairedPoints = [{ aOrder, aName, bOrder, bName, similarity }]` sorted by `aOrder`.

### 4.7 Step 6 — Direction and shared stretch

- Take `pairedPoints` sorted by `aOrder`.
- Find the **longest subsequence whose `bOrder` is strictly increasing** (longest increasing subsequence on `bOrder`) — the same-order shared stretch (A-30).
- Tie-break: higher total similarity, then earlier `aOrder` (stable).
- If the increasing subsequence length `< 2` because direction reverses, the leg fails (AC-12.2).

### 4.8 Step 7 — Minimum overlap

`sharedStretch.length ≥ CFG-4` (default 2), else fail (AC-12.3, BR-12.6).

### 4.9 Step 8 — Classify

Determine the **Saathi's** leg Start (point order 1) and End (last order). The leg is **Full Route** when:

- the Saathi Start and End both belong to the increasing shared stretch, and
- the matched Raahi positions for Start and End are in increasing order (the Raahi covers the Saathi's whole leg in the same direction), and
- intermediate Saathi points may be missing (AC-12.4).

Otherwise, if Steps 3–7 pass, it is **Useful Segment**. Full Route is preferred; the engine does not also search a segment for the same leg (BR-12.4). The shared stretch stored is the longest ordered set for a Useful Segment (D-22).

> **Role note:** classification is always “does the Raahi cover the Saathi's leg”. For the Raahi's own lead, the same leg result is stored (spec: each lead records the result for both legs). Do not invert.

### 4.10 Step 9 — Lead decision and ranking

- A pair produces a lead if **at least one** leg matches.
- Compute ranking components (identical for both sides; gender/rating never used — BR-12.11):
  1. `legsMatched`
  2. `fullRouteLegs`
  3. `totalSharedPoints` (sum over matched legs)
  4. `avgTimeGapMinutes` (mean over matched legs, rounded 2dp)
  5. `commonDaysTotal` (sum of common-day counts across matched legs)
  6. `leadCreatedUtc` (earliest first)
- Create/update one lead for **each side** (BR-12.7); link both with one `MatchGroupId`.

### 4.11 Determinism and idempotency

- `InputsHash = SHA-256(routeVersionA + routeVersionB + config fingerprint)`.
- If an active lead for the pair/side already has the same `InputsHash` and the same leg results, skip writes.
- Recalculation **updates** the existing lead; never creates a second for the same side (BR-12.7).
- A lead that was withdrawn and matches again is **revived** (same row, status back to New only if the member had not already Viewed/Dismissed it in the prior cycle — see §8.5).

---

## 5. Data model

M3 owns the machine-written lead record and its explanation. M4 will add lifecycle transitions and read APIs on the same tables.

### 5.1 `raah_Lead` (one row per member side)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `LeadNumber` | nvarchar(16) | `LED-nnnnnnn`, unique |
| `MatchGroupId` | uniqueidentifier | Links the two sides of a pair |
| `ForMemberId` | bigint | FK → `member_Master.Id`; the list owner |
| `MatchedMemberId` | bigint | FK → `member_Master.Id` |
| `ForRouteId` | bigint | |
| `MatchedRouteId` | bigint | |
| `ForRouteVersionId` | bigint | Exact version used (BR-12.1) |
| `MatchedRouteVersionId` | bigint | |
| `ForStartCity` / `ForEndCity` | nvarchar(120) | Snapshots |
| `MatchedStartCity` / `MatchedEndCity` | nvarchar(120) | Snapshots |
| `LegsMatched` | int | Ranking key 1 |
| `FullRouteLegs` | int | Ranking key 2 |
| `TotalSharedPoints` | int | Ranking key 3 |
| `AvgTimeGapMinutes` | decimal(6,2) | Ranking key 4 |
| `CommonDaysTotal` | int | Ranking key 5 |
| `Status` | tinyint | 1 New, 2 Viewed, 3 Dismissed, 4 Requested, 5 WithdrawnBySystem |
| `InputsHash` | char(64) | Determinism/idempotency |
| `ConfigFingerprint` | nvarchar(64) | CFG-1/4/22 at calculation |
| `CalculatedOnUtc` | datetime2 | Shown to Admin/History |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Indexes:
- Unique `LeadNumber`; unique `MatchGroupId, ForMemberId`; unique `ForMemberId, MatchedMemberId`.
- `ForMemberId, Status, LegsMatched DESC, FullRouteLegs DESC, TotalSharedPoints DESC, AvgTimeGapMinutes ASC, CommonDaysTotal DESC, CreatedDate ASC` (list ranking).
- `MatchedMemberId`; `ForRouteId`; `MatchedRouteId`.

### 5.2 `raah_LeadLegResult`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `LeadId` | bigint | FK → `raah_Lead.Id` |
| `LegType` | tinyint | 1 Going, 2 Return |
| `Matched` | bit | |
| `MatchType` | tinyint | 0 None, 1 FullRoute, 2 UsefulSegment |
| `SharedPointCount` | int | |
| `TimeDifferenceMinutes` | int | |
| `CommonDaysCount` | int | |
| `ForDepartureMinutes` | int | |
| `MatchedDepartureMinutes` | int | |
| audit columns + `IsDeleted` | | |

Index: unique `LeadId, LegType`.

### 5.3 `raah_LeadPointPair` (the explanation)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `LeadLegResultId` | bigint | FK |
| `Sequence` | int | Order in the shared stretch |
| `ForPointOrder` / `ForPointName` | int / nvarchar(100) | As typed, version snapshot |
| `MatchedPointOrder` / `MatchedPointName` | int / nvarchar(100) | As typed |
| `Similarity` | decimal(5,2) | For Admin diagnostics |

Index: unique `LeadLegResultId, Sequence`.

### 5.4 `raah_LeadCommonDay`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `LeadLegResultId` | bigint | FK |
| `DayOfWeek` | tinyint | 1 Mon … 7 Sun |

Index: unique `LeadLegResultId, DayOfWeek`.

### 5.5 `raah_MatchingJob` (durable trigger queue)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `Trigger` | tinyint | RouteActivated, RouteEdited, RouteReactivated, RouteDeactivated, MemberStatusChanged, BlockRemoved, UnderReviewReleased, ManualRecalculation |
| `RouteId` | bigint null | For route-scoped jobs |
| `MemberAId` / `MemberBId` | bigint null | For pair jobs (block removal) |
| `RunId` | bigint null | Set for manual recalculation batches |
| `Status` | tinyint | 1 Pending, 2 Running, 3 Done, 4 Failed |
| `Attempts` | int | Retry cap |
| `EnqueuedAt`, `StartedAt`, `CompletedAt` | datetime2 | |
| `LastError` | nvarchar(1000) null | |
| `DedupeKey` | nvarchar(120) | e.g. `Route:{id}:{version}` to coalesce |

Indexes: `Status, EnqueuedAt`; unique filtered `DedupeKey` where `Status = 1`.

### 5.6 `raah_MatchingRun` (manual recalculation summary)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `Trigger` | tinyint | ManualRecalculation, ConfigChange |
| `Status` | tinyint | Running, Completed, Failed |
| `RoutesEvaluated`, `LeadsCreated`, `LeadsUpdated`, `LeadsWithdrawn` | int | |
| `StartedAt`, `CompletedAt` | datetime2 | |
| `RequestedBy` | bigint | Admin id |
| `ErrorSummary` | nvarchar(2000) null | |

### 5.7 Enums

```
LeadStatus        { New = 1, Viewed = 2, Dismissed = 3, Requested = 4, WithdrawnBySystem = 5 }
MatchType         { None = 0, FullRoute = 1, UsefulSegment = 2 }
LegType           { Going = 1, Return = 2 }
MatchTrigger      { RouteActivated = 1, RouteEdited = 2, RouteReactivated = 3, RouteDeactivated = 4,
                    MemberStatusChanged = 5, BlockRemoved = 6, UnderReviewReleased = 7,
                    ManualRecalculation = 8 }
MatchingJobStatus { Pending = 1, Running = 2, Done = 3, Failed = 4 }
MatchingRunStatus { Running = 1, Completed = 2, Failed = 3 }
```

Migration names (indicative):
- `20260103000000_AddRaahLead`
- `20260103000100_AddRaahLeadLegResultPointDay`
- `20260103000200_AddRaahMatchingJobRun`

---

## 6. DTOs (Binsera.Core)

Namespace: `Binsera.Core.Matching.Models.Raah`.

### 6.1 Internal matching models

```
RouteSnapshot, LegSnapshot, MatchConfig            // §4.1
PointPair          { aOrder, aName, bOrder, bName, similarity }
LegMatchResult     { legType, matched, matchType, sharedPoints[], commonDays[], timeDiffMinutes,
                     forDepartureMinutes, matchedDepartureMinutes }
MatchOutcome       { isMatch, eligibilityReason, legs: LegMatchResult[],
                     ranking { legsMatched, fullRouteLegs, totalSharedPoints),
                               avgTimeGapMinutes, commonDaysTotal },
                     inputsHash, configFingerprint }
```

`MatchOutcome` is the pure calculator's result. The orchestrator turns it into two lead writes.

### 6.2 Lead DTOs (consumed by M4)

```
MatchLeadDto                // what M4 stores/reads; M4 owns the public API shape
  leadNumber, matchGroupId
  matchedMember { memberId, fullName, photoUrl, gender, journeyRole, vehicle, emailVerified,
                  rating { average, count, isNewMember }, memberSince }
  perLeg: [ { legType, matched, matchType, sharedPoints:[{order,name,matchedName}],
              commonDays:[string], forDepartureTime, matchedDepartureTime, timeDiffMinutes } ]
  cities { forStart, forEnd, matchedStart, matchedEnd }
  calculatedOnUtc
```

M4's list/detail DTOs are defined in the M4 plan; M3 guarantees the stored rows contain every field needed for "Why this match" (UP-2).

### 6.3 Admin DTOs (M11/M13 mounted)

| DTO | Fields |
|---|---|
| `RecalculateResultDto` | `runId`, `status`, `message` |
| `MatchingRunDto` | `runId`, `status`, `routesEvaluated`, `leadsCreated`, `leadsUpdated`, `leadsWithdrawn`, `startedAt`, `completedAt`, `errorSummary` |
| `TestMatchRequest` | `saathi: SampleLegInput`, `raahi: SampleLegInput` (cities + points + days + time) |
| `TestMatchResultDto` | `eligible`, `eligibilityChecks:[{code,label,passed,detail}]`, `legs:[{legType, passed, checks:[...], pointPairs:[...], classification}]`, `result` |
| `MatchingHealthDto` | `noLeadRoutes`, `leadsPerRoute`, `leadToRequestRate`, `requestToAcceptRate`, `avgHoursToAnswer`, `matchMix:{going:{full,segment},return:{full,segment}}`, `supplyDemand:{saathiRoutes,raahiRoutes}`, `byCity[]`, `mostUsedStops[]`, `unmatchedStops[]` |

---

## 7. Endpoint catalogue

M3 has **no member screens**. The following Admin endpoints are mounted by M11/M13 controllers; M3 supplies the services. Base route shown for clarity.

| # | Method | Route (owner) | Purpose | Success |
|---|---|---|---|---|
| 1 | POST | `admin/matching/recalculate` (M13) | Enqueue a full recalculation and return `runId` | 202 `RecalculateResultDto` |
| 2 | GET | `admin/matching/runs/{runId}` (M13) | Poll a recalculation run summary | 200 `MatchingRunDto` |
| 3 | POST | `admin/config/test-match` (M13) | Run the calculator on sample inputs, current settings | 200 `TestMatchResultDto` |
| 4 | GET | `admin/matching-health` (M11) | Aggregated engine health | 200 `MatchingHealthDto` |
| 5 | GET | `admin/matching-health/unmatched-stops` (M11) | Stop names that never paired, with variants | 200 `UnmatchedStopDto[]` |

All require an **Admin** role. There is no member endpoint and no way for anyone to hand-create or edit a match (BR-4.6).

**Internal triggers (no HTTP):** M2 calls `IMatchingEngineGateway`; M8 calls `IMatchingEngine.RunForPairAsync` after unblock; M12/M11 call `RunForRouteAsync` after releasing Under Review; M13 calls `RunAllAsync`.

---

## 8. How triggers and writes work

### 8.1 Enqueue path (from M2)

`IMatchingEngineGateway.EnqueueForRouteAsync(routeId, trigger)`:

1. Compute `DedupeKey = "Route:{routeId}:{currentVersion}"`; insert `raah_MatchingJob` (Pending) if no pending job with the same key.
2. Return immediately (M2 has already committed the route change).
3. The background processor picks it up within the poll interval (default 5 s; target ≤ 1 min, BR-12.10).

### 8.2 Background processor — `MatchingQueueProcessor : BackgroundService`

Config section `Matching:Queue` → `{ Enabled, PollSeconds, BatchSize, MaxAttempts }`.

Per tick (own `while` loop + `Cronos`-free polling, matching existing job style):

1. Resolve a scope via `IServiceScopeFactory`.
2. Claim a batch atomically: `UPDATE ... SET Status = Running, StartedAt = now, Attempts = Attempts + 1 OUTPUT inserted.*` (or a transaction with row locks) to avoid double processing.
3. For each job dispatch:
   - `RouteActivated` / `RouteReactivated` / `RouteEdited` → `RunForRouteAsync`.
   - `RouteDeactivated` / `MemberStatusChanged` → `WithdrawForRouteAsync` / `WithdrawForMemberAsync`.
   - `BlockRemoved` → `RunForPairAsync`.
   - `UnderReviewReleased` → `RunForRouteAsync`.
   - `ManualRecalculation` → process the run in batches (see §8.6).
4. Mark Done or Failed with `LastError`; retry up to `MaxAttempts`, then leave Failed for Admin visibility.

### 8.3 `RunForRouteAsync(routeId, trigger)`

1. Load the route snapshot (`IRouteSnapshotReader`). If not Active → withdraw its leads if the trigger is deactivation; else no-op.
2. Load candidates: opposite-role, Active routes, sharing at least one city, not blocked.
   - Query via city ids and role; exclude same member; apply the pair-level block filter.
3. For each candidate, `MatchCalculator.Calculate(snapshot, candidateSnapshot, config)`.
4. Persist via `ILeadWriter.UpsertSideAsync(outcome, side)` for both sides (one lead each).
5. Candidates that previously had a lead with this route but are no longer in the matching set → `WithdrawPairAsync(routeId, candidateRouteId)`.
6. Do **not** touch pending requests or connections (BR-11.13/11.14; A-6).

### 8.4 `RunForPairAsync(memberA, memberB)`

- Load both members' Active routes; if both eligible and match → upsert both sides; else withdraw the pair's leads.
- Used after Admin removes a block (BR-12.9) and by the Test/recalc paths where a pair needs re-checking.

### 8.5 `ILeadWriter` rules

| Method | Behaviour |
|---|---|
| `UpsertSideAsync(outcome, side)` | Find the lead by `(ForMemberId, MatchedMemberId)`. If none → insert New. If exists and active → update leg results, ranking and `CalculatedOnUtc`; **preserve** `Status` (Viewed/Dismissed/Requested). If exists and `WithdrawnBySystem` → revive to `New` (BR-12.7, SC-12.8) and refresh |
| `WithdrawForRouteAsync(routeId)` | Set `Status = WithdrawnBySystem` for leads where the route is either side; keep rows for history |
| `WithdrawForMemberAsync(memberId)` | Same for a member (block/deactivation) |
| `WithdrawPairAsync(a, b)` | Both directions of one pair |
| `WithdrawReturnOnlyLeadsAsync` | Withdraw leads whose only matched leg is Return (callers: M2 return-leg removal) |

Lead numbers: `LED-nnnnnnn` from a generator on insert only.

### 8.6 `RunAllAsync` (manual recalculation, M13)

1. Create `raah_MatchingRun` (Running) and capture a single `MatchConfig` snapshot for the whole run.
2. Iterate all Active routes in batches (e.g. 200). For each, evaluate against candidates as in §8.3, but **do not** re-evaluate pairs twice: use an unordered pair key set to skip.
3. Update run counters; on completion set Completed and `CompletedAt`.
4. Configuration changes do **not** auto-run; existing leads are not withdrawn by a settings change alone (BR-6.3 / SC-12.12). Only this explicit Admin action or a later trigger applies new config.
5. Audit `MATCHING_RECALCULATED` (settings action, BR-6.1) at the run level; individual lead changes stay in the lead history (BR-12.12).

### 8.7 Deactivation / block / member status

| Event | Action |
|---|---|
| Route deactivated (M2) | `WithdrawForRouteAsync`; connections/chats untouched |
| Member blocked / deactivated (Admin) | `WithdrawForMemberAsync`; M12 closes connections |
| Block between two members removed | `RunForPairAsync` (may revive) |
| Under Review released | `RunForRouteAsync` |

---

## 9. Ranking (BR-12.1, spec §Ranking)

Implement the comparator exactly, in order:

```
1. legsMatched          DESC
2. fullRouteLegs        DESC
3. totalSharedPoints    DESC
4. avgTimeGapMinutes    ASC
5. commonDaysTotal      DESC
6. createdDate          ASC
```

- Gender, rating and any personal detail are **never** inputs (BR-12.11 / AC-12.6).
- Filters (Leg, Gender) in M4 change visibility only; they never re-order.
- Store the components on each lead so M4 can sort without recomputation.
- Final tie-break (equal on all six keys) by `LeadNumber` ASC for stable paging.

---

## 10. Authorization and security

| Caller | Allowed |
|---|---|
| Anonymous | Nothing |
| Member | Nothing directly; sees results through M4 |
| Admin | The Admin endpoints above (recalculate, runs, test match, health) only |
| System | Queue processor and triggers |

- `admin/config/test-match` uses **no member data** (M13 copy); it only evaluates supplied sample legs and must not query `member_Master`.
- Matching Health aggregates are counts only; no chat text or contact details (BR-20.4).
- Determinism must not depend on iteration order of unordered DB results: sort candidate routes by `routeId` before evaluating, and sort point candidates deterministically before the greedy assignment.
- Never log point names at volume with member ids unless needed for errors; no contact data in matching logs.

---

## 11. Validation matrix

| ID | Rule | Where | Handling |
|---|---|---|---|
| V-27 | Opposite roles only | Calculator step 1 | no lead |
| V-28 | Only Active routes; Active/Restricted members | Calculator step 1 / reader | no lead |
| V-29 | City overlap required | Calculator step 1 | no lead |
| V-53 | Not blocked (either direction) | Calculator step 1 | no lead |
| — | Days overlap ≥ 1 | Step 3 | leg fails |
| — | Time gap ≤ CFG-1 inclusive, midnight-safe | Step 4 | leg fails |
| — | Text similarity ≥ CFG-22, numbers exact | Step 5 | point not paired |
| — | Same order / direction | Step 6 | leg fails |
| — | Shared points ≥ CFG-4 | Step 7 | leg fails |
| — | One lead per side at a time | Writer | upsert, never duplicate |
| — | Draft/Invalid/Under Review never matched | Reader/eligibility | no lead |

---

## 12. Config usage

| CFG | Use |
|---|---|
| CFG-1 | Time tolerance minutes (inclusive) |
| CFG-4 | Minimum shared points in the stretch |
| CFG-22 | Text similarity threshold (%) |

Read once per job/run into a `MatchConfig` snapshot. Store `ConfigFingerprint` on each lead so a result can be reproduced and explained (BR-12.1). A config change applies only to runs started after it (BR-6.3).

---

## 13. Error responses (Admin endpoints)

| Code | HTTP | Message |
|---|---|---|
| `FORBIDDEN` | 403 | "You do not have access to this action." |
| `RECALC_ALREADY_RUNNING` | 409 | "A recalculation is already running." |
| `RUN_NOT_FOUND` | 404 | "Run not found." |
| `TEST_MATCH_INVALID` | 400 | "Enter valid sample legs to run the test." |
| `MATCHING_FAILED` | 500 | "Matching could not be completed. See the run summary." |

Use object results; never `Forbid()`/`Challenge()`.

---

## 14. Performance and scale

- Candidate query uses `raah_Route` (Status, RouteType) + city ids; avoid full scans.
- One route change evaluates only opposite-type Active routes sharing a city.
- Full recalculation batches routes and de-duplicates unordered pairs.
- Target: a single route change processed within ≤ 1 minute (BR-12.10); Admin recalc reports progress.
- Point matching is O(points²) per leg with ≤ 5 points — negligible.
- Coalesce duplicate pending jobs via `DedupeKey`.
- Write in a single transaction per lead pair to keep both sides consistent.

---

## 15. Build sequence (M3)

| Step | Work | Output |
|---|---|---|
| 1 | Add enums, entities/configurations/migrations for lead, leg result, point pairs, days, jobs, runs | Compiles, migrations apply |
| 2 | `IRouteSnapshotReader` implementation over M2 tables; `IBlockGateway` no-op; `IMemberStatusReader` wiring | Snapshots load |
| 3 | `MatchCalculator` pure implementation (§4) | Unit tests pass |
| 4 | `ILeadWriter` + lead number generator | Leads persist/revive |
| 5 | `MatchingQueueProcessor` + `raah_MatchingJob` | Triggers processed |
| 6 | Implement M2's `IMatchingEngineGateway` and `ILeadWithdrawalGateway` | M2 integration live |
| 7 | `RunForRouteAsync`, `RunForPairAsync`, `RunAllAsync` | Triggers + recalc |
| 8 | Admin services: recalculate/runs, Test Match, Matching Health queries | M11/M13 ready |
| 9 | Wiring in M2/M8/M12/M13 call sites | End-to-end |
| 10 | Replace `IBlockGateway` when M8 lands; tune indexes | Production-ready |

Suggested folders:

```
Binsera.Core/Matching/
  Domain/Raah/Lead.cs, LeadLegResult.cs, LeadPointPair.cs, LeadCommonDay.cs,
               MatchingJob.cs, MatchingRun.cs
  Enums/LeadStatus.cs, MatchType.cs, MatchTrigger.cs, MatchingJobStatus.cs
  Models/Raah/MatchModels.cs, AdminDtos.cs
  Contracts/IMatchingEngine.cs, ILeadWriter.cs, IMatchingHealthQuery.cs,
            ITestMatchService.cs, MatchCalculator.cs
  Contracts/Gateways/IBlockGateway.cs, IRouteSnapshotReader.cs, IMemberStatusReader.cs
Binsera.Application/Matching/Services/
  MatchingEngine.cs, MatchCalculator.cs, LeadWriter.cs, LeadNumberGenerator.cs,
  MatchingHealthQuery.cs, TestMatchService.cs
  Gateways/NoOpBlockGateway.cs, MatchingEngineGateway.cs, LeadWithdrawalGateway.cs
Binsera.API/BackgroundServices/MatchingQueueProcessor.cs
Binsera.API/Controllers/Admin/MatchingAdminController.cs   // mounted by M11/M13
Binsera.Persistence/Configurations/Raah/LeadConfiguration.cs, ... 
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahLead*.cs
```

---

## 16. Test plan

### 16.1 Unit — Calculator (the core)

Use the worked example from the spec as a golden test:

- Neha: 4 shared points, 3 common days, 10 min → Full Route.
- Rohit: 2 points, 0 min → Useful Segment.
- Amit: 2 points, 5 min → Useful Segment; Rohit ranks above Amit.
- Sunil: opposite direction → no lead.
- Pooja: 60 min > CFG-1 → no lead.
- Karan: one shared point → no lead.
- Ranking: two-legs lead first (AC-12.6).

Additional:

- Boundary AC-12.1: gap 30 → lead; 31 → none.
- Midnight: 23:50 vs 00:10 → 20 min (SC-12.5).
- Fuzzy: Badarpur Metro / bdarpur metro → pair; Sector 12 / Sector 15 → no pair (AC-12.5).
- Full Route with missing intermediates → Full Route (AC-12.4).
- Same role → none (AC-12.8); Draft/Inactive/Invalid/Under Review → none (AC-12.7).
- City overlap: none → none; Delhi→Noida vs Delhi→Delhi → eligible (AC-12.9 / SC-12.14).
- Determinism: shuffle input node order → identical `MatchOutcome` and `InputsHash`.
- One-to-one pairing: repeated names do not pair the same point twice.

### 16.2 Integration

- Route activation enqueues a job and creates two leads within the SLA.
- Active edit creates a version, recalculates, withdraws no-longer-matching leads; pending requests and connections untouched.
- Deactivation withdraws all its leads but leaves connections/chats.
- Block removal re-checks the pair and may revive leads.
- Idempotency: re-running the same job does not duplicate leads or change `LeadNumber`.
- Withdrawn lead revived when the route matches again; status handling follows §8.5.
- Manual recalc creates one run, reports counters, and does not double-process pairs.
- Config change alone does not alter existing leads (SC-12.12).
- Admin endpoints require Admin; Test Match never reads member data.
- Migrations apply cleanly; `member_Master` untouched.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-12.1 Time tolerance boundary | §4.5, unit |
| AC-12.2 Direction | §4.7 LIS |
| AC-12.3 Minimum shared points | §4.8 |
| AC-12.4 Full Route with missing intermediates | §4.9 |
| AC-12.5 Fuzzy text and numbers | §4.6 + shared point matcher |
| AC-12.6 Both-legs ranking | §9 |
| AC-12.7 Exclusions | §4.2 |
| AC-12.8 Same-role pairs | §4.2 |
| AC-12.9 City overlap | §4.2 |

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| — | Does M3 or M4 own the lead tables? | M3 creates/writes them; M4 extends lifecycle and read APIs on the same tables |
| — | Queue durability | DB table `raah_MatchingJob` (survives restart), not in-memory |
| — | Revive a Withdrawn lead that the member had Dismissed before? | Revive to New; if Dismissed state must persist, keep Dismissed and only refresh details (decide with M4) |
| — | Should config changes trigger an automatic recalculation? | No — only future matching; explicit Admin Recalculate (BR-6.3) |
| — | Matching on activation synchronous or async? | Async queue; response reports `matchingQueued` |
| — | Performance cap for full recalculation | Batched; report progress via the run record |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Step 1 eligibility | §4.2 |
| Step 2 leg pairing | §4.3 |
| Step 3 days | §4.4 |
| Step 4 time | §4.5 |
| Step 5 point alignment | §4.6 |
| Step 6 direction/shared stretch | §4.7 |
| Step 7 minimum overlap | §4.8 |
| Step 8 classification | §4.9 |
| Step 9 leads | §4.10 |
| Ranking | §9 |
| What the engine records | §5 (lead tables), §6 |
| BR-12.1 determinism | §4.11, `InputsHash` |
| BR-12.2/12.3/12.9/12.13 | §4.2 |
| BR-12.4 Full Route preferred | §4.9 |
| BR-12.5 typed time | §4.5 |
| BR-12.6 min shared points | §4.8 |
| BR-12.7 one lead per side | §5.1, §8.5 |
| BR-12.8 never edits routes | §2 |
| BR-12.10 refresh ≤ 1 min | §8.1/8.2 |
| BR-12.11 gender not a rule | §9 |
| BR-12.12 lead events not audit | §8.6, §11 |
| Triggers | §8 |
| AC-12.1…12.9 | §16.3 |
| CFG-1/4/22 | §12 |
| V-27/28/29/53 | §11 |
