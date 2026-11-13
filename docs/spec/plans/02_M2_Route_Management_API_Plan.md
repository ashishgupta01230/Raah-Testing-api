# M2 — Route Management — API Plan

| | |
|---|---|
| **Module** | M2 — Route Management (spec `docs/spec/02_M2_Route_Management.md`) |
| **Document type** | API implementation plan: contracts, data model, endpoints, rules, validation, audit, sequencing, tests |
| **Build order** | #2 after Foundation + M1 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M2 lets an authenticated, Active Member:

1. Read their current route: status, both legs, vehicle, active-since date (SCR-11.1).
2. Create, save and edit a route as a Draft, with Going and Return legs (SCR-11.2).
3. Review a route and its activation checklist (SCR-11.3).
4. Activate, deactivate and reactivate their route.
5. See past routes and versions, read-only (SCR-11.4).
6. Ask for a missing city ("My city is not listed") — delegated to M13.

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Profile, journey role, vehicle, member status | M1 / Foundation |
| Matching, lead generation and withdrawal | M3 / M4 |
| Pending request withdrawal | M5 |
| Connections and chats | M5 / M6 |
| Active-city master and city requests | M13 |
| Admin deactivation with reason, Under Review status | M10 / M12 |
| Audit storage | Foundation (Section 8) |
| Config editing and defaults | M13 |

---

## 2. Dependency and build-order handling

M2 is built **before** M3, M4, M5. Activation and edits must trigger matching and withdraw leads/requests, but those modules do not exist yet. Do not import them; define narrow **module boundary contracts owned by M2** and register safe no-op implementations, exactly as M1 does for its gateways.

| Contract (M2) | Purpose | M2 implementation now | Later implementation |
|---|---|---|---|
| `IMatchingEngineGateway` | Enqueue/re-run matching for a member's route after activation/edit | No-op returning `{ queued = true }` | M3 background matching job |
| `ILeadWithdrawalGateway` | Withdraw all leads for a route, or leads matching only on the removed Return leg | No-op returning `0` | M4 |
| `IPendingRequestGateway` | Withdraw all pending requests (sent and received) on deactivation | No-op returning `0` | M5 |
| `IMemberProfileGateway` | Read journey role and vehicle; validate Raahi has a vehicle | Reads `raah_MemberProfile` (M1 table) directly | Stays M2-owned; M1 already stores it |

Outbound contract **M2 provides** (consumed by M1 and later modules):

| Contract | Implemented by M2 | Consumers |
|---|---|---|
| `IActiveRouteGateway` (declared in M1) | `ActiveRouteGateway` reads the Active route summary and deactivates the Active route | M1 preview, M1 role change, M1 deactivation |
| `IRoutePublicReader` (declared in M2) | Public route summary (points, days, departure time) | M4 lead detail, M5 request comparison |

Rules:

- M2 depends on Foundation contracts + M1's `raah_MemberProfile` (read-only) + its own boundary gateways.
- When M3/M4/M5 land, they supply real implementations via DI; M2's endpoint contract does not change.
- The `IActiveRouteGateway` no-op from M1 is **replaced** by M2's real implementation. Keep a single registration on both sides so only one resolves.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; `[Route("me/route")]`; services via the primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interface in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` → global `ResponseFilterAttribute` envelope `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | `BadRequest(new { code, message })` / `StatusCode(409, ...)` object results; **never** `Forbid()` / `Challenge()` (they are swallowed to 200) |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new RAAH tables normal EF-managed tables; `member_Master` stays excluded and untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, PK `long Id`, explicit precision, audit columns, `IsDeleted`, per-column indexes |
| Validation | No FluentValidation in the repo. Validate in the application service (manual checks + result objects); keep DTO annotations minimal |
| Audit | Foundation audit service; append-only; record old/new values |
| Current user | `UserId`, `UserName`, `RoleId`, `MemberGraphId` from `ApiControllerBase` — never accept a member id in the body |

**Naming note.** Route prefix `me/route` (no `/api`) to match existing no-prefix routes. Public route endpoints later live under their owning module, not M2.

---

## 4. Data model

### 4.1 Modelling decision

A **route** is one lineage with a fixed route type (Saathi/Raahi). Each save of an Active route produces a **version**; a Draft has a single working version. A role change creates a **new route** (BR-11.17). Legs, points and days belong to a version so history is immutable.

Vertical structure:

```
raah_Route (lineage)
  └── raah_RouteVersion (1..n)
        ├── raah_RouteLeg (Going, optional Return)
        │     ├── raah_RoutePoint (2..5, ordered)
        │     └── raah_RouteLegDay (1..7)
        └── StartCityId / EndCityId (snapshot per version)
```

### 4.2 New tables

#### `raah_Route` (route lineage, one per created route)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `RouteNumber` | nvarchar(16) | `RTE-nnnnnn`, unique, generated on first save |
| `MemberId` | bigint | FK → `member_Master.Id`; indexed |
| `RouteType` | tinyint | 1 Saathi, 2 Raahi; **fixed** after creation (BR-11.17) |
| `Status` | tinyint | 1 Draft, 2 Active, 3 Inactive, 4 Invalid, 5 UnderReview |
| `CurrentVersionNo` | int | Default 1 |
| `ActiveSinceUtc` | datetime2 null | Set on activation/last reactivation |
| `DeactivatedAtUtc` | datetime2 null | |
| `DeactivationReasonCode` | tinyint null | 1 Member, 2 RoleChange, 3 SelfDeactivation, 4 Admin, 5 System |
| `RowVersion` | rowversion | Optimistic concurrency |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false (BR-11.16 no hard delete) |

Indexes:
- Unique `RouteNumber`.
- **Filtered unique** `UX_RaahRoute_Member_Active` on `MemberId` where `Status = 2 AND IsDeleted = 0` (BR-11.1).
- **Filtered unique** `UX_RaahRoute_Member_Draft` on `MemberId` where `Status = 1 AND IsDeleted = 0` (BR-11.2).
- Non-unique `MemberId, Status`.

`UnderReview` (Status 5) is treated as Active for "one route" purposes in the service (the admin may set it while an Active route exists). Do not add a second filtered index for it.

#### `raah_RouteVersion`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `RouteId` | bigint | FK → `raah_Route.Id` |
| `VersionNo` | int | 1..n, unique per route |
| `StartCityId` | int null | FK → city master (M13); null allowed while Draft |
| `EndCityId` | int null | FK → city master; may equal Start (D-26) |
| `StartCityName` | nvarchar(120) null | Snapshot for history |
| `EndCityName` | nvarchar(120) null | Snapshot |
| `HasReturnLeg` | bit | |
| `IsCurrent` | bit | Exactly one current version per route |
| `ActivatedAtUtc` | datetime2 null | |
| `DeactivatedAtUtc` | datetime2 null | |
| `ChangeReason` | tinyint | 1 Created, 2 DraftSave, 3 ActiveEdit, 4 Reactivation, 5 AdminCorrection |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | |

Indexes: unique `RouteId, VersionNo`; filtered unique `RouteId` where `IsCurrent = 1 AND IsDeleted = 0`.

#### `raah_RouteLeg`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `RouteVersionId` | bigint | FK → `raah_RouteVersion.Id` |
| `LegType` | tinyint | 1 Going, 2 Return |
| `DepartureTimeMinutes` | int null | Minutes from midnight (0–1439); null allowed while Draft |
| audit columns + `IsDeleted` | | |

Indexes: unique `RouteVersionId, LegType`.

#### `raah_RoutePoint`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `RouteLegId` | bigint | FK → `raah_RouteLeg.Id` |
| `Order` | int | 1-based; 1 = Start, last = End |
| `Name` | nvarchar(100) | Trimmed, 2–100 chars (BR-11.5) |
| `NormalizedName` | nvarchar(100) | Upper-invariant, whitespace-collapsed, for duplicate/place checks |
| audit columns + `IsDeleted` | | |

Indexes: unique `RouteLegId, Order`.

#### `raah_RouteLegDay`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `RouteLegId` | bigint | FK → `raah_RouteLeg.Id` |
| `DayOfWeek` | tinyint | 1 Mon … 7 Sun (CFG-18) |

Indexes: unique `RouteLegId, DayOfWeek`.

Migration names (indicative):
- `20260102000000_AddRaahRoute`
- `20260102000100_AddRaahRouteVersion`
- `20260102000200_AddRaahRouteLegPointDay`

### 4.3 Enums

```
RouteStatus  { Draft = 1, Active = 2, Inactive = 3, Invalid = 4, UnderReview = 5 }
RouteType    { Saathi = 1, Raahi = 2 }          // mirrors JourneyRole
LegType      { Going = 1, Return = 2 }
DayOfWeek    { Mon = 1, Tue = 2, Wed = 3, Thu = 4, Fri = 5, Sat = 6, Sun = 7 }
RouteChangeReason { Created = 1, DraftSave = 2, ActiveEdit = 3, Reactivation = 4, AdminCorrection = 5 }
RouteDeactivationReason { Member = 1, RoleChange = 2, SelfDeactivation = 3, Admin = 4, System = 5 }
```

Store enums as `tinyint` via EF conversion, matching the existing style.

---

## 5. Foundation / M1 contracts consumed by M2

| Contract | Method (shape) |
|---|---|
| `IMemberProfileGateway` (M2-owned) | `GetRoleAndVehicleAsync(memberId)` → `{ journeyRole, vehicleType? }` |
| `IMemberStatusService` | `GetStatusAsync(memberId)` (Active gate, BR-11.12) |
| `ICityDirectory` (M13-owned, read now) | `GetActiveCitiesAsync(search?)`; `IsActiveCityAsync(cityId)`; `GetCityAsync(cityId)` |
| `ICityRequestService` (M13-owned) | `SubmitAsync(memberId, cityName)` (A-56) |
| `IAppConfigReader` | CFG-2, CFG-3, CFG-18, CFG-19, CFG-21, CFG-22 |
| `IAuditService` | `RecordAsync(entity, entityId, action, oldValue, newValue, actor)` |
| `IRouteNumberGenerator` | `NextAsync()` → `RTE-nnnnnn` |
| `IPointMatchService` | `AreSamePlace(a, b, threshold)` using the shared matching rule (D-22) |
| `ICurrentUserService` | Existing current user accessor |

If a contract does not exist yet, add it as M2 groundwork; do not edit `member_Master`.

---

## 6. DTOs (Binsera.Core)

Namespace: `Binsera.Core.Routes.Models.Raah`.

### 6.1 Read models

```
RouteLegDto
  points          [{ order:int, name:string }]     // 2..5
  days            [string]                          // "Mon","Tue",...
  departureTime   string?                           // "HH:mm" 24h; null while Draft

RouteDto                                      // SCR-11.1 / SCR-11.2 / SCR-11.3
  routeId             long
  routeNumber         string
  status              string                    // Draft|Active|Inactive|Invalid|UnderReview
  routeType           string                    // Saathi|Raahi
  vehicle             string?                   // Raahi only
  versionNo           int
  startCity           { id:int, name:string }?
  endCity             { id:int, name:string }?
  going               RouteLegDto
  return               RouteLegDto?              // null when absent
  activeSinceUtc      DateTime?
  deactivatedAtUtc    DateTime?
  canEdit             bool
  canActivate         bool
  canDeactivate       bool
  canReactivate       bool
  editBlockedReason   string?                   // e.g. "UnderReview"

RouteSummaryDto                                // My Route + M1 preview
  routeId, routeNumber, status, routeType, vehicle,
  startCity, endCity, going: RouteLegDto, return: RouteLegDto?,
  activeSinceUtc

PublicRouteSummaryDto                          // M1 preview / M4 / M5
  routeNumber, startCity, endCity, going: PublicLegDto, return: PublicLegDto?

PublicLegDto
  points [{ order, name }]
  days   [string]
  departureTime "HH:mm"
```

### 6.2 Request DTOs

| DTO | Fields |
|---|---|
| `SaveRouteRequest` | `StartCityId?`, `EndCityId?`, `Going: RouteLegInput`, `Return: RouteLegInput?`, `SaveAsDraft: bool` |
| `RouteLegInput` | `Points: string[]` (ordered, Start first, End last), `Days: string[]`, `DepartureTime: string?` |
| `ReviewRouteRequest` | `StartCityId?`, `EndCityId?`, `Going`, `Return?` (same shape; review without persisting) |
| `ValidatePointsRequest` | `LegType: string`, `Points: string[]` |
| `DeactivateRouteRequest` | `Confirm: bool` |
| `CityRequestDto` | `CityName: string` |

`SaveRouteRequest` has **no** route type, member or vehicle field; those are server-derived. Extras are ignored by the binder; the UI never sends them.

### 6.3 Response DTOs

| DTO | Fields |
|---|---|
| `RouteOptionsDto` | `minPoints` (CFG-3), `maxPoints` (CFG-2), `maxIntermediate`, `days: [{value,label}]` (CFG-18), `returnLegRequired` (CFG-19), `duplicateThreshold` (CFG-22), `vehicle` (Raahi only), `pointMin=2`, `pointMaxLength=100` |
| `ReviewResultDto` | `canActivate: bool`, `checks: [{ code, label, passed, message?, tab }]`, `normalizedRoute: RouteDto` |
| `ValidatePointsResultDto` | `points: [{ order, name, errorCode?, message?, conflictsWithOrder? }]`, `hasErrors: bool` |
| `ActivationResultDto` | `status`, `versionNo`, `routeNumber`, `matchingQueued: bool`, `leadsWithdrawn: int` |
| `DeactivationResultDto` | `status`, `leadsWithdrawn`, `pendingRequestsWithdrawn`, `connectionsKept: true` |
| `RouteHistoryDto` | `routes: [{ routeId, routeNumber, routeType, status, versions: [{ versionNo, startCity, endCity, activatedAtUtc, deactivatedAtUtc, changeReason }] }]` |
| `CityDto` | `id`, `name` |

---

## 7. Endpoint catalogue

Base route: `/me/route`. All endpoints require an authenticated **Member** unless noted.

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/me/route` | Current route (Active, else Draft, else latest Inactive) | SCR-11.1 | 200 `RouteDto` or `null` |
| 2 | GET | `/me/route/options` | Limits: points, days, return rule, duplicate threshold, vehicle | SCR-11.2/11.3 | 200 `RouteOptionsDto` |
| 3 | GET | `/me/route/cities?search=` | Active cities for the dropdowns | SCR-11.2 | 200 `CityDto[]` |
| 4 | POST | `/me/route` | Create a Draft or return the existing Draft (BR-11.2) | SCR-11.2 | 201/200 `RouteDto` |
| 5 | GET | `/me/route/{routeId}` | Route detail for view/edit (owner only) | SCR-11.1/11.2 | 200 `RouteDto` |
| 6 | PUT | `/me/route/{routeId}` | Save Draft, or save an Active edit as a new version | SCR-11.2 | 200 `RouteDto` |
| 7 | POST | `/me/route/{routeId}/points/validate` | Live per-point format/duplicate check (BR-11.6) | SCR-11.2 | 200 `ValidatePointsResultDto` |
| 8 | POST | `/me/route/{routeId}/review` | Activation checklist without persisting | SCR-11.3 | 200 `ReviewResultDto` |
| 9 | POST | `/me/route/{routeId}/activate` | Activate a Draft/Inactive route (BR-11.12/11.15) | SCR-11.3 | 200 `ActivationResultDto` |
| 10 | POST | `/me/route/{routeId}/deactivate` | Deactivate the Active route (BR-11.14) | SCR-11.1 | 200 `DeactivationResultDto` |
| 11 | POST | `/me/route/{routeId}/city-requests` | "My city is not listed" → M13 (A-56) | SCR-11.2 | 202 `{ message }` |
| 12 | GET | `/me/route/history` | Past routes and versions | SCR-11.4 | 200 `RouteHistoryDto` |

Forward-looking, **not** M2 public API:
- M1 preview and M4/M5 read route data through `IActiveRouteGateway` / `IRoutePublicReader`, never through `/me/route/{id}` (which is owner-only).
- Admin deactivation/status changes go through M10/M12 endpoints, not M2.

---

## 8. Endpoint details

### 8.1 `GET /me/route`

- Resolve `memberId` from the token.
- Precedence: Active route → Draft → most recent non-deleted route (Inactive/Invalid/UnderReview). If none, `data = null` (UI shows the empty state).
- Populate `canEdit/canActivate/canDeactivate/canReactivate`:
  - `canEdit` = status ∈ {Draft, Active}; not Invalid/UnderReview (Under Review blocks edits, G-1).
  - `canActivate` = status ∈ {Draft, Inactive} and server-side `ReviewRoute` would pass.
  - `canDeactivate` = status ∈ {Active, UnderReview? no} → Active only.
  - `canReactivate` = status = Inactive and no other Active route.
- `editBlockedReason` = `UnderReview` → "This route is under review. You cannot change it for now."
- Never returns another member's route.

### 8.2 `GET /me/route/options`

- Read CFG-2, CFG-3, CFG-18, CFG-19, CFG-22; vehicle from `IMemberProfileGateway`.
- `maxIntermediate = maxPoints - 2`.
- No member-specific route data; still authenticated.

### 8.3 `GET /me/route/cities`

- Delegate to `ICityDirectory.GetActiveCitiesAsync(search)`.
- Only **active** cities (AC-11.9); never return inactive ones.
- `search` is optional and used by the desktop city selector.

### 8.4 `POST /me/route`

- If the member already has a Draft → return it with `200` (BR-11.2 "starting a new route while a Draft exists opens that Draft").
- Else create `raah_Route` (RouteNumber from generator, RouteType from M1 role) + version 1 with empty legs → `201`.
- Do **not** create a second Draft; the filtered unique index backs this.
- If the member is not Active → `403 MEMBER_NOT_ACTIVE` (BR-11.12 / BR-10.2).

### 8.5 `GET /me/route/{routeId}`

- Owner check: `route.MemberId == token memberId`, else `404 ROUTE_NOT_FOUND` (do not leak existence).
- Return the **current version** for Draft/Active, the latest version for Inactive/Invalid.

### 8.6 `PUT /me/route/{routeId}` — save

Request: `SaveRouteRequest`.

Rules:

1. Owner only; route must be Draft or Active → else `409 ROUTE_NOT_EDITABLE`.
2. If Admin has set `UnderReview` → `409 ROUTE_UNDER_REVIEW` (G-1).
3. Normalize the payload:
   - Trim and collapse whitespace in each point; trim city ids.
   - `Points`: 2..CFG-2 per leg (always represent Start/End explicitly; the first is Start, the last is End).
   - Each point: 2–100 chars (BR-11.5); allowed chars `A–Z a–z 0–9 space . , - ' / & ( )`; reject a URL or a run of ≥10 digits (A-26) → `ROUTE_POINT_PHONE_OR_LINK`.
   - Duplicate place inside a leg: use `IPointMatchService` with CFG-22; numbers exact; Start vs End included (BR-11.6). Return the two clashing **orders** → `ROUTE_DUPLICATE_POINT`.
   - `Days`: 1..7 distinct values from CFG-18 → `ROUTE_DAYS_REQUIRED`.
   - `DepartureTime`: required for a **complete** leg; while Draft it may be null. Format `HH:mm` or `h:mm AM/PM` normalized to `HH:mm` → `ROUTE_TIME_REQUIRED`.
   - Cities: optional while Draft; if supplied must be active (`ROUTE_CITY_INACTIVE`). Same city allowed (D-26).
4. Persistence:
   - **Draft (`SaveAsDraft` or status Draft):** update version 1 in place (replace legs/points/days). Status stays Draft. `ROUTE_DRAFT_SAVED` audit.
   - **Active edit:** create a new `raah_RouteVersion` (CurrentVersionNo+1, `IsCurrent` flip), copy the validated payload; keep status Active; set `ActiveSinceUtc` unchanged; call `IMatchingEngineGateway.RecalculateAsync`; call `ILeadWithdrawalGateway.WithdrawNoLongerMatchingAsync` and return `leadsWithdrawn`. Pending requests and connections are untouched (BR-11.13). Audit `ROUTE_VERSION_CREATED`.
   - Removing the Return leg on an Active route (`Return = null`) with CFG-19 = No: save the new version and withdraw leads matching **only** on Return (`WithdrawReturnOnlyLeadsAsync`).
5. If CFG-19 = Yes and status will be Active, a Return leg is required → `ROUTE_RETURN_REQUIRED` (only enforced at activation, not at Draft save).
6. Return the refreshed `RouteDto`.
7. Concurrency: compare `RowVersion`; on mismatch → `409 ROUTE_CONFLICT`, ask the client to reload.

### 8.7 `POST /me/route/{routeId}/points/validate`

- Stateless validation of a leg's points for **as-you-type** feedback (UI plan §4.3). Persists nothing.
- Returns per-point `errorCode/message` and `conflictsWithOrder` when two points match (BR-11.6), so the UI names both ("Govindpuri and Govindpure look like the same place").
- Same format rules as §8.6; counts optional here (the add button is limited client-side by `maxPoints`, server still enforces on save).

### 8.8 `POST /me/route/{routeId}/review`

- Accepts the same payload as save (so unsaved review works) or, when the body is empty, reviews the stored current version.
- Never persists.
- Builds `ReviewResultDto.checks` (the checklist on SCR-11.3):
  | code | label | tab |
  |---|---|---|
  | `EMAIL_VERIFIED` | Email verified | — |
  | `CITIES_CHOSEN` | Cities chosen | Cities |
  | `GOING_COMPLETE` | Going leg complete | Going |
  | `RETURN_COMPLETE` | Return leg complete (if present/required) | Return |
  | `VEHICLE_PRESENT` | Vehicle (Raahi) | — |
- `canActivate = all required checks pass`; `normalizedRoute` is the cleaned route for the review screen.
- Endpoint is safe to call repeatedly; it changes no state.

### 8.9 `POST /me/route/{routeId}/activate`

Rules (BR-11.12, BR-11.15, AC-11.3, AC-11.4, AC-11.8, AC-11.9, BR-11.19):

1. Owner only; status must be Draft or Inactive → `409 ROUTE_NOT_ACTIVATABLE`.
2. Re-run the full review **server-side** (never trust a prior review call). If any check fails → `400 ROUTE_INCOMPLETE` with the failing checks.
3. If the member already has another Active route → `409 ROUTE_ALREADY_ACTIVE` ("You already have an Active route. Deactivate it first.").
4. Member must be Active (status) → `403 MEMBER_NOT_ACTIVE`.
5. Raahi must have a vehicle on the profile → `400 VEHICLE_REQUIRED` (BR-11.19; may link to M1).
6. In one transaction: set route Status = Active, `ActiveSinceUtc = now` (Inactive → reactivation), version `ActivatedAtUtc = now`, clear `DeactivatedAtUtc`; audit `ROUTE_ACTIVATED`/`ROUTE_REACTIVATED`.
7. After commit: `IMatchingEngineGateway.RecalculateAsync(memberId, routeId)` (queued). Matching never runs on Draft (BR-11.11).
8. Return `{ status: "Active", versionNo, routeNumber, matchingQueued, leadsWithdrawn }`.

### 8.10 `POST /me/route/{routeId}/deactivate`

Request: `{ confirm: true }`.

Rules (BR-11.14, AC-11.6):

1. `confirm` must be true → `400 CONFIRMATION_REQUIRED`.
2. Owner only; status must be Active → `409 ROUTE_NOT_ACTIVE`. Admin/Under Review deactivation is not here.
3. Transaction: Status = Inactive, `DeactivatedAtUtc = now`, `DeactivationReasonCode = 1` (Member); audit `ROUTE_DEACTIVATED`.
4. After commit:
   - `ILeadWithdrawalGateway.WithdrawAllForRouteAsync(routeId)`
   - `IPendingRequestGateway.WithdrawAllForMemberAsync(memberId)`
   - **Do not** touch connections/chats (A-6, AC-11.6).
5. Return counts; connections kept = true.
6. Idempotent-safe: already Inactive → `409 ROUTE_NOT_ACTIVE` (no repeated side effects).

### 8.11 `POST /me/route/{routeId}/city-requests`

- Body `{ cityName }` (trim, 2–120 chars).
- Delegate to `ICityRequestService.SubmitAsync(memberId, cityName)` (M13, A-56).
- Rate-limit per member (e.g. one open request per name) → `409 CITY_REQUEST_EXISTS`.
- Return `202 { message: "We have told Admin. You can save a draft but cannot activate until the city is added." }`.
- The member can keep the Draft; activation stays blocked until both cities are chosen from the active list (BR-11.20).

### 8.12 `GET /me/route/history`

- Return all routes for the member (including Inactive/past types) with every version: route number, type, status, version no, cities, activation/deactivation dates, change reason.
- Read-only; no hard-deleted rows (BR-11.16).
- Paged with keyset "Show more" (UI plan C-35) — `?cursor=&take=` (default 20).

---

## 9. Authorization and status rules

| Caller / state | Allowed |
|---|---|
| Anonymous | None (all M2 endpoints authenticated) |
| Member, Active | All M2 endpoints |
| Member, Restricted | Read, draft and edit; **activation** allowed (restriction affects requests, not routes — BR-4.8). If product decides otherwise, gate at activate only |
| Member, Unverified | No route endpoints reachable (verify gate, BR-5.1/BR-10.2); return `403 MEMBER_NOT_ACTIVE` |
| Member, Inactive/Blocked | Rejected by session/status validation (BR-5.4/5.9) |
| Other member | `404 ROUTE_NOT_FOUND` — never reveal that the route exists |
| Admin | No self-service route endpoints; Admin deactivation goes through M10/M12 with a reason (BR-4.5, BR-11.18) |

Enforce ownership by always deriving the member from the token. Return `StatusCode(403)` for a plain role reject, never `Forbid()`.

---

## 10. Validation matrix

| ID | Rule | Where | Error |
|---|---|---|---|
| V-14 | Point 2–100 chars | Save/validate | `ROUTE_POINT_LENGTH` |
| V-15 | 2–5 points per leg incl. Start/End (CFG-2/3) | Save | `ROUTE_POINT_COUNT` |
| V-16 | Allowed point characters only | Save/validate | `ROUTE_POINT_CHARS` |
| V-17 | No link / no run of ≥10 digits | Save/validate | `ROUTE_POINT_PHONE_OR_LINK` |
| V-18 | No duplicate places in a leg, incl. Start/End (CFG-22, numbers exact) | Save/validate | `ROUTE_DUPLICATE_POINT` |
| V-19 | ≥1 travel day per leg (CFG-18) | Save | `ROUTE_DAYS_REQUIRED` |
| V-20 | Departure time required for a complete leg | Save/activate | `ROUTE_TIME_REQUIRED` |
| V-21 | Cities mandatory to activate, from active list | Activate | `ROUTE_CITY_REQUIRED` / `ROUTE_CITY_INACTIVE` |
| V-22 | Return leg required when CFG-19 = Yes | Activate | `ROUTE_RETURN_REQUIRED` |
| V-23 | One Active route (D-14) | Activate | `ROUTE_ALREADY_ACTIVE` |
| V-24 | One Draft at a time (A-27) | Create | return existing Draft |
| V-25 | Route type fixed (BR-11.17) | Save | never accept a type field; `ROUTE_TYPE_LOCKED` if forced |
| V-26 | Raahi needs a vehicle | Activate | `VEHICLE_REQUIRED` |

Implement these in `RouteService` and unit-test them. **Do not add FluentValidation.**

`IPointMatchService` is the **same** rule used by matching (D-22); do not fork a second fuzzy matcher. Similarity ≥ CFG-22 (default 70%) with exact number comparison.

---

## 11. Audit events (Section 8, BR-10.10 reused for routes)

| Event | Trigger | Old → New |
|---|---|---|
| `ROUTE_CREATED` | POST create Draft | — → route number/type |
| `ROUTE_DRAFT_SAVED` | PUT Draft | summary of changed legs/cities |
| `ROUTE_VERSION_CREATED` | PUT Active edit | version n → n+1, changed fields |
| `ROUTE_RETURN_LEG_REMOVED` | PUT Active edit without Return | had Return → none |
| `ROUTE_ACTIVATED` | Activate Draft | Draft → Active |
| `ROUTE_REACTIVATED` | Activate Inactive | Inactive → Active |
| `ROUTE_DEACTIVATED` | Member deactivate | Active → Inactive + counts |
| `ROUTE_CITY_REQUESTED` | City request | — → requested city name |

Include actor, actor role, UTC timestamp; append-only. `ROUTE_DEACTIVATED` also records lead/request counts for traceability.

---

## 12. Config usage

| CFG | Use in M2 |
|---|---|
| CFG-2 | Max points per leg (default 5) |
| CFG-3 | Min points per leg (default 2) |
| CFG-18 | Allowed travel days |
| CFG-19 | Return leg mandatory or optional |
| CFG-21 | Vehicle options (Raahi display) |
| CFG-22 | Duplicate/place-match threshold |

Read through the Foundation reader; defaults live in Foundation/M13, not M2.

---

## 13. Error responses

Use `BadRequest(new { code, message })`, `StatusCode(409, ...)` object results, or `NotFound`; never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `MEMBER_NOT_ACTIVE` | 403 | "Verify your email to manage your route." |
| `ROUTE_NOT_FOUND` | 404 | "Route not found." |
| `ROUTE_NOT_EDITABLE` | 409 | "This route cannot be edited now." |
| `ROUTE_UNDER_REVIEW` | 409 | "This route is under review. You cannot change it for now." |
| `ROUTE_NOT_ACTIVATABLE` | 409 | "This route cannot be activated." |
| `ROUTE_NOT_ACTIVE` | 409 | "This route is not active." |
| `ROUTE_INCOMPLETE` | 400 | "Complete the highlighted fields before activating." |
| `ROUTE_POINT_LENGTH` | 400 | "Each point must be 2 to 100 characters." |
| `ROUTE_POINT_CHARS` | 400 | "Use letters, numbers and simple punctuation only." |
| `ROUTE_POINT_PHONE_OR_LINK` | 400 | "Please do not enter phone numbers or links. Use chat after connecting." |
| `ROUTE_POINT_COUNT` | 400 | "A leg can have at most 5 points, including Start and End." |
| `ROUTE_DUPLICATE_POINT` | 400 | "<A> and <B> look like the same place." |
| `ROUTE_SAME_PLACE` | 400 | "Start and End look like the same place." |
| `ROUTE_DAYS_REQUIRED` | 400 | "Choose at least one day." |
| `ROUTE_TIME_REQUIRED` | 400 | "Enter the departure time." |
| `ROUTE_CITY_REQUIRED` | 400 | "Choose the start and end city." |
| `ROUTE_CITY_INACTIVE` | 400 | "This city is not available. Choose another." |
| `ROUTE_RETURN_REQUIRED` | 400 | "This route needs a return leg." |
| `ROUTE_ALREADY_ACTIVE` | 409 | "You already have an Active route. Deactivate it first." |
| `ROUTE_TYPE_LOCKED` | 400 | "Route type cannot be changed. Create a new route instead." |
| `ROUTE_CONFLICT` | 409 | "This route was changed on another device. Reload and try again." |
| `VEHICLE_REQUIRED` | 400 | "Choose Bike or Car before activating a Raahi route." |
| `CITY_REQUEST_EXISTS` | 409 | "You have already requested this city." |
| `CONFIRMATION_REQUIRED` | 400 | "Confirm that you understand what will happen." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

Keep messages aligned with the UI plan's microcopy.

---

## 14. Privacy and security

- Route data is private to its owner except the **public summary** (points, days, departure time) shown on a lead/request through `IRoutePublicReader`; never expose member contact data from a route.
- `GET /me/route/{routeId}` is owner-only; use `404` for others to avoid enumeration.
- Points may contain place names only; reject links and long digit runs (also guards accidental phone sharing, A-26).
- Store no raw PII beyond the city/point text; points are member-typed place names.
- Optimistic concurrency via `RowVersion` to prevent lost updates from two devices.
- Activation/edit side effects (matching, withdrawal) run **after** the DB commit and are idempotent gateways, so a retry does not double-withdraw.
- Rate-limit city requests per member; do not allow anonymous access.
- No hard delete of routes or versions (BR-11.16); `IsDeleted` exists only for parity and stays false.

---

## 15. Build sequence (M2)

| Step | Work | Output |
|---|---|---|
| 1 | Add enums, entities, configurations, migrations for route/version/leg/point/day | Compiles, migrations apply, `member_Master` untouched |
| 2 | Add M2 gateways (`IMatchingEngineGateway`, `ILeadWithdrawalGateway`, `IPendingRequestGateway`) with no-op implementations; implement `IActiveRouteGateway` for M1 | DI resolvable; M1 preview works |
| 3 | `IPointMatchService` shared place-matching (or wire to the matching module's implementation) | V-18 works |
| 4 | `IRouteNumberGenerator` | `RTE-nnnnnn` |
| 5 | `RouteService`: read, options, create draft, save, validate points | Draft flow complete |
| 6 | Review service (checklist) | SCR-11.3 API |
| 7 | Activate/deactivate/reactivate orchestration with gateways | AC-11.3/11.4/11.6 |
| 8 | History + city request delegation | SCR-11.4, A-56 |
| 9 | `MemberRouteController` endpoints + error codes | Postman-ready |
| 10 | Audit calls on every mutation | Audit rows verified |
| 11 | Replace no-op gateways when M3/M4/M5 arrive; add matching job | Integration complete |

Suggested folders:

```
Binsera.Core/Routes/
  Domain/Raah/Route.cs, RouteVersion.cs, RouteLeg.cs, RoutePoint.cs, RouteLegDay.cs
  Enums/RouteStatus.cs, RouteType.cs, LegType.cs, RouteChangeReason.cs, RouteDeactivationReason.cs
  Models/Raah/*Dto.cs, *Request.cs
  Contracts/IRouteService.cs, IRoutePublicReader.cs, IPointMatchService.cs, IRouteNumberGenerator.cs
  Contracts/Gateways/IMatchingEngineGateway.cs, ILeadWithdrawalGateway.cs, IPendingRequestGateway.cs,
                     IMemberProfileGateway.cs
Binsera.Application/Routes/Services/
  RouteService.cs, RouteReviewService.cs, RouteNumberGenerator.cs
  Gateways/NoOpMatchingEngineGateway.cs, NoOpLeadWithdrawalGateway.cs, NoOpPendingRequestGateway.cs,
           MemberProfileGateway.cs, ActiveRouteGateway.cs
Binsera.API/Controllers/Routes/MemberRouteController.cs
Binsera.Persistence/Configurations/Raah/RouteConfiguration.cs, RouteVersionConfiguration.cs,
                                         RouteLegConfiguration.cs, RoutePointConfiguration.cs,
                                         RouteLegDayConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahRoute*.cs
```

---

## 16. Test plan

### 16.1 Unit

- Point count: 1 and 6 rejected; 2 and 5 accepted; first=Start, last=End.
- Point text: 1/2/100/101 chars; illegal chars; URL and 10+ digit run rejected.
- Duplicate: "Nehru Place" vs "nehru place" rejected; "Govindpuri" vs "Govindpure" (~90%) rejected; "Govindpuri" vs "Govindpuri Metro" (~62%) accepted; numeric points exact.
- Days: none rejected; all seven accepted; duplicates de-duped.
- Time: required for complete leg; Draft may omit; AM/PM normalized.
- Cities: null blocks activation; inactive rejected; same city allowed.
- Return: CFG-19=No → Going-only activates; CFG-19=Yes → blocks; removing Return on Active withdraws Return-only leads.
- One Active / one Draft (including concurrent create → unique index).
- Active edit creates version n+1 and calls matching + withdrawal gateways; pending requests untouched.
- Deactivate withdraws leads/requests, keeps connections; already-inactive conflict.
- Reactivation re-runs review and matching.
- Raahi without vehicle blocks activation; route type cannot be changed.
- Concurrency: stale `RowVersion` → `ROUTE_CONFLICT`.

### 16.2 Integration / API

- Every endpoint under auth: missing token → 401; wrong role → 403; non-owner route → 404.
- Response envelope present; `data` shape matches the DTOs.
- `GET /me/route/cities` returns only active cities.
- Draft save → activate → edit → deactivate → reactivate lifecycle works end to end with gateways.
- Audit row per mutation with old/new; no raw tokens.
- Migrations apply cleanly; `member_Master` untouched.
- Filtered unique indexes prevent a second Active and a second Draft.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-11.1 Point limit per leg | §8.6 V-15 / `ROUTE_POINT_COUNT` |
| AC-11.2 Start and End mandatory | §8.8 checklist / `ROUTE_INCOMPLETE` |
| AC-11.3 One Active route | §8.9 `ROUTE_ALREADY_ACTIVE` + filtered unique index |
| AC-11.4 Return leg optional | §8.9 with CFG-19 = No |
| AC-11.5 Copy reversed | client-side prefill + `PUT` save (same days, empty time) |
| AC-11.6 Deactivation effects | §8.10 gateways; connections kept |
| AC-11.7 Same-place check | §8.6/8.7 `ROUTE_SAME_PLACE` |
| AC-11.8 City mandatory | §8.8/8.9 `ROUTE_CITY_REQUIRED` |
| AC-11.9 Only active cities | §8.3 + `ROUTE_CITY_INACTIVE` |

Scenarios SC-11.1 … SC-11.18 map to the endpoints above; SC-11.15 (Admin deactivation) is M10/M12, not M2.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| — | Should Restricted members be able to activate a route? | Yes (BR-4.8); can tighten at the activate step |
| — | Is matching run synchronously on activation or queued? | Queued through `IMatchingEngineGateway`; response reports `matchingQueued` |
| — | Does editing an Active route require review again? | No — Active edits save a new version and re-match immediately (BR-11.13); only Draft/Inactive go through the review screen |
| — | Where does "My city is not listed" store requests? | M13 (`ICityRequestService`); M2 only delegates |
| — | Should abandoned Drafts auto-expire? | No auto-expiry in Phase 1; member can move it to Inactive |
| — | Global API prefix `api`? | Keep existing no-prefix convention |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| BR-11.1 one Active route | §4.2 filtered unique index, §8.9 |
| BR-11.2 one Draft | §4.2 filtered index, §8.4 |
| BR-11.3 Start/End mandatory, 2–5 points | §8.6 V-15, §8.8 |
| BR-11.4 free-text points | §8.6 (no suggestions) |
| BR-11.5 point text rules | §8.6 V-14/V-16 |
| BR-11.6 duplicate places | §8.6/8.7 V-18, `IPointMatchService` |
| BR-11.7 order matters | `Order` column, §8.6 |
| BR-11.8 days/time | §8.6 V-19/V-20 |
| BR-11.9 Return optional/required | §8.6/8.9 V-22, CFG-19 |
| BR-11.10 Return-time warning | UI-only; API stores any valid time |
| BR-11.11 Draft never matched | §8.9 (matching only on activation) |
| BR-11.12 activation checks | §8.8/8.9 |
| BR-11.13 edit Active → new version | §8.6, §4.2 `raah_RouteVersion` |
| BR-11.14 deactivation effects | §8.10 |
| BR-11.15 reactivation | §8.9 |
| BR-11.16 no hard delete | §4.2/§14 |
| BR-11.17 fixed route type | §4.2 (no type on save) |
| BR-11.18 ownership | §9 |
| BR-11.19 Raahi vehicle | §8.9 |
| BR-11.20 cities | §8.3/8.8/8.11 |
| SCR-11.1…11.4 | §7 endpoint catalogue |
| Data Impact | §4, §8.6, §8.9, §8.10, §11 |
| CFG-2/3/18/19/21/22 | §12 |
| V-14…V-26 | §10 |
