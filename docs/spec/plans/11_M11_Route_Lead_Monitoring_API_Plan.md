# M11 — Route & Lead Monitoring — API Plan

| | |
|---|---|
| **Module** | M11 — Route & Lead Monitoring (spec `docs/spec/11_M11_Route_Lead_Monitoring.md`, functional specification Section 20) |
| **Document type** | API implementation plan: admin read models, monitoring aggregates, endpoints, two route actions, validation, audit, sequencing, tests |
| **Build order** | #11 after M1–M10 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Companion** | UI plan: `11_M11_Route_Lead_Monitoring_UI_Plan.md` |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M11 is the **Admin-only route and matching observatory**:

1. List every route with filters and the columns Admin needs to see whether matching is working (SCR-20.1).
2. Open one route in full: both legs, versions, leads by match type, connections by status (SCR-20.2).
3. List leads by member or route with match type and conversion (SCR-20.3).
4. List requests and connections by status with a **Reported** flag and a funnel; no chat content (SCR-20.4).
5. Show matching health: no-lead routes, leads per route, match-type mix, most-used and unmatched stop names, funnel rates, response time, supply/demand, by city (SCR-20.5).
6. Perform **only two** route actions, each with a mandatory reason: **Deactivate** and **Hold Under Review** (BR-20.3).
7. Feed RPT-3…RPT-12 queries from the same live data (report *catalogue* stays in M15).

Out of scope (owned elsewhere):

| Concern | Owner |
|---|---|
| Route creation, editing, activation and versioning | M2 |
| Lead generation, ranking and withdrawal mechanics | M3 / M4 |
| Request/connection lifecycle and cool-off | M5 |
| Chat content and message APIs | M6 |
| Rating values and moderation | M7 / M12 |
| Reports, restrict/block, holding rules for safety releases | M8 / M12 |
| Matching configuration values and Recalculate | M13 |
| Report catalogue UI and exports | M15 |
| Member profile fields and member actions | M1 / M10 |
| Notification/email delivery | M9 |

**Hard non-goals (BR-20.1):** Admin cannot create, edit or delete a route, lead, request or connection; there is **no manual matching or assignment** anywhere (BR-4.6, SC-20.6); the monitors **never return chat text** (BR-20.4); no figure is typed or adjusted by Admin (BR-20.6).

---

## 2. Module boundaries

M11 owns no business data. It reads M2–M8 through gateways and writes only through M2's route-status gateway and the Foundation audit trail.

### 2.1 Contracts consumed

| Contract | Source | Methods used by M11 |
|---|---|---|
| `IRouteAdminReader` | M2 | route list with filters; route detail; legs; versions; active route summary; route status counts |
| `IRouteAdminGateway` | M2 (provided by M11 boundary, see §2.2) | `DeactivateRouteAsync(routeId, reason, actor)`, `HoldUnderReviewAsync(routeId, reason, actor)` |
| `ILeadAdminReader` | M4 | leads by route/member; counts by match type and leg; lead status counts; lead → request counts; list with pager |
| `IPendingRequestAdminReader` | M5 | pending request counts and withdrawal (via gateway on deactivation) |
| `IConnectionAdminReader` | M5 | connection counts by status; list with filters; response timestamps; acceptance/wait metrics |
| `ISafetyAdminReader` | M8 / M12 | open-report flag for route's member; open-report flag for a connection pair; report status counts |
| `IMemberProfileAdminReader` | M1 | member number, name, photo, journey role, vehicle, member status |
| `IPointMatchService` | M2 / M3 | **same text-similarity rule as matching** for stop grouping and unmatched-stop detection (D-22, A-21, BR-20.5) |
| `IAppConfigReader` | Foundation | CFG-1, CFG-2, CFG-4, CFG-19, CFG-22 (matching context and rule names) |
| `IAuditService` | Foundation | append-only audit for the two route actions |
| `INotificationDispatcher` | M9 | events on route deactivation/hold, if the notification matrix requires them |
| `IClock` | Foundation | UTC-consistent "active for N days", response-time windows |

### 2.2 Contracts provided by M11

| Contract | Purpose | Consumer |
|---|---|---|
| `IRouteMonitorQueryService` | Route list, route detail, legs, versions, route lead/connection summaries | Admin UI, M10 Route tab |
| `IRouteAdminActionService` | Validate, preview and execute Deactivate / Hold Under Review | Admin UI, M12 |
| `ILeadMonitorQueryService` | Lead monitor list, match-type mix, conversion | Admin UI, M14/M15 |
| `IConnectionMonitorQueryService` | Connection monitor list, funnel, response-time metrics | Admin UI, M14/M15 |
| `IMatchingHealthQueryService` | No-lead routes, per-route lead stats, stop-name grouping, city table, supply/demand | Admin UI, M14/M15 |
| `IMonitoringExportService` | CSV export of route/lead/connection monitors | Admin UI, M15 |
| `IRouteAdminReader` | See §2.1 — shared read contract | M10 |

### 2.3 Rules

- M11 never writes M4/M5/M6 tables directly. Deactivation effects are applied by M2/M4/M5 gateways in a defined order (§8).
- A route action is authorised per record (V-45, V-48): the route must exist, not be deleted, and the action must be legal for its current status.
- Every monitor query is **live** from the source modules. No cached or Admin-edited number (BR-20.6).
- No contract in this module exposes chat text, message counts per conversation body, emails or mobiles beyond the Admin-visible member summary.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; routes below; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interfaces in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` wrapped by `ResponseFilterAttribute` |
| Non-2xx | `BadRequest(new { code, message })`, `StatusCode(409, new { code, message })`, `NotFound(new { code, message })`; **never** `Forbid()` / `Challenge()` (swallowed to 200) |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; no new table unless a measured need appears |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id` PK, audit columns, `IsDeleted` |
| Validation | No FluentValidation; validate inside the application service; unit-test the rules |
| Audit | Append-only, old/new + reason + actor + UTC + source IP (Section 8, BR-20.3) |
| Current user | `UserId`, `UserName`, `RoleId`, `MemberGraphId` from `ApiControllerBase` |
| Export | Return a `FileResult`; `ResponseFilterAttribute` leaves file results untouched |
| Admin gate | Enforce the Admin role in the controller; a plain reject uses `StatusCode(403)` |

**Route prefixes (no `/api`):** `/admin/routes`, `/admin/leads`, `/admin/connections`, `/admin/matching-health`.

---

## 4. Data model

### 4.1 No new business table (default)

M11 reads Foundation + M2–M8 and writes through the M2 route-status gateway. Do **not** duplicate route, lead, request or connection rows into an M11 table; a copied table would break BR-20.6 (figures must be live).

**Optional read model (open decision UD-API-1).** Only if the monitors are slow at scale, add a `raah_MonitoringDailyMetric` projection (per day × city × route type) written by background aggregation. Fields: `Id`, `MetricDate`, `CityId`, `RouteType`, `ActiveRoutes`, `NoLeadRoutes`, `Leads`, `FullRouteLeads`, `UsefulSegmentLeads`, `Requests`, `Accepted`, `Rejected`, `Expired`, `Withdrawn`, `AvgResponseMinutes`, audit columns, `IsDeleted`. Index unique `(MetricDate, CityId, RouteType)`. Do not build pre-emptively; live queries first, measure, then decide. Aggregates must be labelled as such and never replace the live drill-down lists.

### 4.2 Audit

No new audit table. Use the Foundation audit trail for the two route actions, filtered by `entity = Route`, `entityId = routeId`.

### 4.3 Enums (reuse, never redefine)

```
RouteStatus      { Draft = 1, Active = 2, Inactive = 3, Invalid = 4, UnderReview = 5 }   // M2
LeadMatchType    { None = 0, FullRoute = 1, UsefulSegment = 2 }                          // M3
LeadStatus       { New, Viewed, Dismissed, Requested, WithdrawnBySystem }                // M4
ConnectionStatus { Pending = 1, Accepted = 2, Rejected = 3, Withdrawn = 4,
                   Expired = 5, Disconnected = 6, Blocked = 7 }                         // M5
LegType          { Going, Return }                                                       // M2

RouteMonitorAction { Deactivate = 1, HoldUnderReview = 2 }
RouteListSort      { InvalidFirst = 1, LastEditedDesc = 2, ActiveSinceDesc = 3,
                     LeadsDesc = 4, RouteNumberAsc = 5, MemberNameAsc = 6 }
LeadListSort       { CreatedDesc = 1, CreatedAsc = 2, MatchType = 3, Status = 4 }
ConnectionListSort { SentDesc = 1, SentAsc = 2, Status = 3, AnsweredDesc = 4 }
```

---

## 5. DTOs (Binsera.Core)

Suggested namespace: `Binsera.Core.Monitoring.Models.Admin`.

### 5.1 Route Monitor (SCR-20.1)

```
RouteMonitorQuery
  search            string?    // route number or member name/number
  status            string[]?  // Draft|Active|Inactive|Invalid|UnderReview
  routeType         string?    // Saathi|Raahi
  cityId            int?       // matches Start City OR End City
  hasReturnLeg      bool?
  hasOpenReport     bool?      // route's member is subject of an open safety report
  leadsBucket       string?    // None | 1to5 | MoreThan5
  pointText         string?    // contains a stop name, same fuzzy rule as matching
  changedFrom       DateTime?
  changedTo         DateTime?
  sort              string?    // default InvalidFirst
  page              int        // default 1
  pageSize          int        // default 25, max 100

RouteMonitorItemDto
  routeId            long
  routeNumber        string        // RTE-nnnnnn
  member             { memberId, memberNumber, fullName, photoUrl, status }
  routeType          string
  status             string
  invalidRuleName    string?       // populated only when status = Invalid (BR-20.7)
  goingSummary       { startCity, endCity, points: string[], days: string[], departureTime }
  returnSummary      { startCity, endCity, points: string[], days: string[], departureTime }?
  leadsCount         int
  requestsCount      int
  connectionsCount   int
  hasOpenReport      bool
  activeSinceUtc     DateTime?
  lastEditedUtc      DateTime

RouteMonitorPageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters
  invalidCount       int           // banner count for BR-20.7
```

### 5.2 Route Detail (SCR-20.2)

```
RouteDetailDto
  routeId, routeNumber, routeType, vehicle, status, invalidRuleName
  versionNo
  member             { memberId, memberNumber, fullName, photoUrl, status }
  startCity, endCity
  activeSinceUtc, deactivatedAtUtc, lastEditedUtc
  hasOpenReport      bool
  availableActions   AvailableRouteActionDto[]
  capabilities       { canEdit: false, canDelete: false, canMatch: false }

RouteLegAdminDto
  legType            string        // Going | Return
  points             [{ order, name, matchedStopLabel? }]   // as typed, point by point
  days               [string]
  departureTime      string
  leadsByMatchType   { fullRoute, usefulSegment, none }
  leadsByLeg         int

RouteVersionItemDto
  versionNo          int
  startCity, endCity
  goingPoints, returnPoints   [string]
  activatedAtUtc, deactivatedAtUtc
  changedSummary     string[]      // e.g. "Start changed", "Point 2 renamed", "Days changed"
  changedBy          string        // actor or "Member"
  changedAtUtc       DateTime

RouteLeadSummaryDto
  total, fullRoute, usefulSegment, noMatch
  byLeg              { going: {...}, return: {...} }
  byStatus           { new, viewed, dismissed, requested, withdrawnBySystem }

RouteConnectionSummaryDto
  total
  byStatus           { pending, accepted, rejected, withdrawn, expired, disconnected, blocked }
  reportedCount      int           // connections with an open report on the pair

AvailableRouteActionDto
  action             string        // Deactivate | HoldUnderReview
  allowed            bool
  disabledReason     string?
  isDestructive      bool
  requiresReason     true
```

### 5.3 Lead Monitor (SCR-20.3)

```
LeadMonitorQuery
  search            string?    // lead number, member name/number, route number
  memberId          long?
  routeId           long?
  matchType         string?    // FullRoute | UsefulSegment | NoMatch
  legType           string?    // Going | Return
  status            string?    // New | Viewed | Dismissed | Requested | WithdrawnBySystem
  converted         string?    // Requested | Connected | NotConverted
  createdFrom, createdTo  DateTime?
  sort, page, pageSize

LeadMonitorItemDto
  leadId, leadNumber
  route             { routeId, routeNumber, memberId, memberNumber, memberName }
  matchedMember     { memberId, memberNumber, fullName, photoUrl, role, vehicle }
  matchTypeByLeg    { going, return }        // FullRoute|UsefulSegment|NoMatch
  status            string
  createdUtc        DateTime
  requestedUtc      DateTime?
  connectionStatus  string?
  converted         bool
  hasOpenReport     bool

LeadMonitorPageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters
  conversionSummary { total, requested, connected, requestRate, connectionRate }
```

### 5.4 Connection Monitor (SCR-20.4)

```
ConnectionMonitorQuery
  search            string?    // connection number, member name/number
  status            string[]?  // Pending|Accepted|Rejected|Withdrawn|Expired|Disconnected|Blocked
  reported          bool?      // open safety report involves the pair
  sentFrom, sentTo  DateTime?
  sort, page, pageSize

ConnectionMonitorItemDto
  connectionId, connectionNumber
  sender            { memberId, memberNumber, fullName, photoUrl }
  receiver          { memberId, memberNumber, fullName, photoUrl }
  status            string
  statusLabel       string         // neutral Admin wording
  sentOnUtc         DateTime
  respondedOnUtc    DateTime?
  responseMinutes   int?
  reported          bool           // open report on the pair
  // never: message body, message count per thread, chat id with content link

ConnectionMonitorPageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters
  funnel            ConnectionFunnelDto

ConnectionFunnelDto
  sent, pending, accepted, rejected, expired, withdrawn, disconnected, blocked
  answered          int        // accepted + rejected
  acceptanceRate    decimal?   // accepted / answered (SC-20.4)
  avgResponseMinutes decimal?
```

### 5.5 Matching Health (SCR-20.5)

```
MatchingHealthQuery
  cityId            int?
  days              int        // period for no-lead and rate windows; default 7
  routeType         string?    // Saathi|Raahi (tile drill-down)
  from, to          DateTime?  // optional explicit period override

MatchingHealthSummaryDto
  period             { days, fromUtc, toUtc }
  noLeadRoutes       { count, byRouteType: { saathi, raahi }, thresholdDays }
  leadsPerRoute      { average, distribution: [{ bucket, count }] }
  matchMix           { going: { fullRoutePct, usefulSegmentPct },
                       return: { fullRoutePct, usefulSegmentPct } }
  supplyDemand       { activeSaathiRoutes, activeRaahiRoutes }
  funnel             { leadToRequestRate, requestAcceptRate, answeredRequests }
  responseTime       { averageMinutes }
  byCity             [{ cityId, cityName, activeRoutes, leads,
                         leadToRequestRate, acceptanceRate }]

NoLeadRouteItemDto
  routeId, routeNumber, member { id, number, fullName }, routeType, city,
  activeSinceUtc, daysActive, points, days, lastLeadUtc?

LeadsPerRouteBucketDto   { bucket, routeCount }

MatchMixDto               { legType, fullRoute, usefulSegment, fullRoutePct, usefulSegmentPct }

StopNameVariantDto        { text, count }

StopNameGroupDto
  canonical           string       // representative spelling
  totalCount          int
  variants            StopNameVariantDto[]   // variant spellings incl. canonical
  exampleRouteIds     [long]       // for drill-down (max 10)

UnmatchedStopNameDto
  text                string
  occurrenceCount     int
  routeIds            [long]
  nearestMatchedText  string?      // nearest stop that did match, if any
  nearestSimilarity   decimal?     // so Admin sees why it did not group (BR-20.5)

ResponseTimeDto
  averageMinutes      decimal?
  answeredCount       int
  buckets             [{ range, count }]
```

### 5.6 Requests, options and results

| DTO | Fields |
|---|---|
| `RouteActionContextDto` | `routeId`, `routeNumber`, `memberName`, `currentStatus`, `effects` (ordered plain sentences), `counts` `{ leads, pendingRequests, acceptedConnections, membersAffected }`, `requiresReason: true`, `reasonMinLength: 10`, `warning` (for Under Review: "Removes the route from matching until released.") |
| `PerformRouteActionRequest` | `Action` (string, required), `Reason` (string, required, min 10 after trim, max 500) |
| `RouteActionResultDto` | `routeId`, `action`, `status`, `effects` (applied counts), `performedAtUtc`, `message` |
| `RouteFilterOptionsDto` | statuses, route types, cities (from City Master), lead buckets, sorts, `reasonMinLength`, `defaultNoLeadDays`, `cfgs` `{ timeToleranceMin: CFG-1, minSharedPoints: CFG-4, textThresholdPct: CFG-22 }` |
| `LeadFilterOptionsDto` | match types, legs, lead statuses, conversion options, sorts |
| `ConnectionFilterOptionsDto` | statuses, reported options, sorts |
| `MatchingHealthOptionsDto` | cities, periods (7/14/30 days), route types, `defaultNoLeadDays`, `textThresholdPct` (CFG-22), `minSharedPoints` (CFG-4) |
| `MonitoringExportRequest` | monitor type + same filters as that monitor + `IncludeMemberNames` (bool, default true; contact details are never exported here) |

---

## 6. Endpoint catalogue

Every endpoint requires an authenticated **Admin**; member tokens get `403`.

### 6.1 Route Monitor / Detail (SCR-20.1, SCR-20.2)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 1 | GET | `/admin/routes` | Route list with filters, invalid-first sort | 200 `RouteMonitorPageDto` |
| 2 | GET | `/admin/routes/options` | Statuses, types, cities, buckets, config context | 200 `RouteFilterOptionsDto` |
| 3 | GET | `/admin/routes/{id}` | Route detail header | 200 `RouteDetailDto` |
| 4 | GET | `/admin/routes/{id}/legs` | Going/Return legs point by point + per-leg lead counts | 200 `RouteLegAdminDto[]` |
| 5 | GET | `/admin/routes/{id}/versions` | Saved versions with dates and change summary | 200 `RouteVersionItemDto[]` |
| 6 | GET | `/admin/routes/{id}/leads` | Lead counts by match type and leg | 200 `RouteLeadSummaryDto` |
| 7 | GET | `/admin/routes/{id}/connections` | Connection counts by status + reported count | 200 `RouteConnectionSummaryDto` |
| 8 | GET | `/admin/routes/{id}/actions/context?action=` | Effects and counts for the confirm dialog | 200 `RouteActionContextDto` |
| 9 | POST | `/admin/routes/{id}/actions` | Deactivate or Hold Under Review with reason | 200 `RouteActionResultDto` |
| 10 | POST | `/admin/routes/export` | CSV export of the current list | 200 file (CSV) |

### 6.2 Lead Monitor (SCR-20.3)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 11 | GET | `/admin/leads` | Lead list with filters + conversion summary | 200 `LeadMonitorPageDto` |
| 12 | GET | `/admin/leads/options` | Match types, legs, statuses, conversion, sorts | 200 `LeadFilterOptionsDto` |
| 13 | GET | `/admin/leads/{id}` | Read-only lead detail (route + matched member + match reasons; no chat) | 200 `LeadMonitorItemDto` |
| 14 | POST | `/admin/leads/export` | CSV export of the current lead list | 200 file (CSV) |

### 6.3 Connection Monitor (SCR-20.4)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 15 | GET | `/admin/connections` | Request/connection list with status, reported flag, funnel | 200 `ConnectionMonitorPageDto` |
| 16 | GET | `/admin/connections/options` | Statuses, reported options, sorts | 200 `ConnectionFilterOptionsDto` |
| 17 | GET | `/admin/connections/funnel` | Funnel and acceptance/response metrics for the period | 200 `ConnectionFunnelDto` |
| 18 | GET | `/admin/connections/{id}` | Read-only counts/status/dates only (never messages) | 200 `ConnectionMonitorItemDto` |
| 19 | POST | `/admin/connections/export` | CSV export of the current list | 200 file (CSV) |

### 6.4 Matching Health (SCR-20.5)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 20 | GET | `/admin/matching-health` | Summary tiles + match mix + supply/demand + by-city | 200 `MatchingHealthSummaryDto` |
| 21 | GET | `/admin/matching-health/options` | Cities, periods, route types, config context | 200 `MatchingHealthOptionsDto` |
| 22 | GET | `/admin/matching-health/no-lead-routes` | Active routes longer than N days with zero leads | 200 page of `NoLeadRouteItemDto` |
| 23 | GET | `/admin/matching-health/leads-per-route` | Average + distribution | 200 `LeadsPerRouteBucketDto[]` |
| 24 | GET | `/admin/matching-health/match-mix` | Full Route vs Useful Segment per leg | 200 `MatchMixDto[]` |
| 25 | GET | `/admin/matching-health/stop-names/most-used` | Most-used stop names grouped by the matching rule, with variants | 200 `StopNameGroupDto[]` |
| 26 | GET | `/admin/matching-health/stop-names/unmatched` | Active-route stop names that never match | 200 `UnmatchedStopNameDto[]` |
| 27 | GET | `/admin/matching-health/funnel` | Lead → request and request → accept rates | 200 `ConnectionFunnelDto` + lead rate |
| 28 | GET | `/admin/matching-health/response-time` | Average time from sent to accepted/rejected | 200 `ResponseTimeDto` |
| 29 | GET | `/admin/matching-health/supply-demand` | Active Saathi routes vs Active Raahi routes | 200 `{ activeSaathiRoutes, activeRaahiRoutes }` |
| 30 | GET | `/admin/matching-health/by-city` | Per-city active routes, leads, rates | 200 rows |

Notes:
- Sub-resources 22–30 exist so each tile/table drills down independently and stays fast (UI Plan §5, BR-23.3 style).
- There is **no** `PUT`/`PATCH`/`DELETE` route, lead, request or connection endpoint. There is no manual-match, assign, or "connect two members" endpoint (BR-4.6, SC-20.6).
- Under-Review **release** is owned by M12 (A-32); see UD-API-4.

---

## 7. Endpoint details

### 7.1 `GET /admin/routes`

- Filters combine with AND; arrays within a filter use OR.
- `status` includes Draft/Active/Inactive/Invalid/UnderReview; default is all except Draft unless the filter asks for it — **open decision UD-API-2** (proposed: include Draft but the default sort puts Invalid first and Active next).
- `cityId` matches **Start City or End City**; blank ignored.
- `hasReturnLeg` filters on a non-null Return leg.
- `hasOpenReport` = the route's member is the subject of an open safety report (not Closed/Dismissed) from `ISafetyAdminReader`.
- `leadsBucket`: `None` = 0; `1to5` = 1–5; `MoreThan5` = >5. Counts are per route lineage, live.
- `pointText`: normalize and compare with `IPointMatchService` at the current CFG-22 threshold, so "contains a stop" uses the **same fuzzy rule** as matching (BR-20.5, D-22). A literal contains also passes.
- Sort whitelist only (`RouteListSort`); `InvalidFirst` sorts Invalid routes to the top, then Active, then others; unknown sort → `400 SORT_INVALID`.
- `invalidRuleName` is filled from the rule that marked the route Invalid (M2 stores it), never invented by the UI (BR-20.7).
- Numbered pager, `pageSize` 25 default / 100 max.

### 7.2 `GET /admin/routes/options`

- Finite lists only (statuses, types, cities from City Master) plus CFG-1/CFG-4/CFG-22 for display. Authenticated Admin only.

### 7.3 `GET /admin/routes/{id}`

- 404 `ROUTE_NOT_FOUND` when missing or soft-deleted.
- Compose M2 header, M1 member summary, M8 open-report flag, and `availableActions`:
  - **Deactivate**: allowed for Active only (Under Review is released by M12 first — UD-API-4).
  - **HoldUnderReview**: allowed for Active or Draft.
- `capabilities` is always `{ canEdit: false, canDelete: false, canMatch: false }`.
- Return the full route (all points as typed) — Admin-only, never exported without the BR-19.12 masking rule (BR-20.2).

### 7.4 Legs, versions, leads, connections

- `legs`: both legs point by point as typed, days, times, plus `leadsByMatchType` and `leadsByLeg` from M4. Return leg `[]`/null when absent.
- `versions`: newest first; each entry has date and `changedSummary` computed from stored version deltas ("Start changed", "Point 3 renamed", "Days changed", "Time changed") — a plain-language list, not a raw diff. Read-only; no edit or restore (BR-11.13 versioning is M2's, Admin cannot roll back).
- `leads`: counts only by match type (Full Route / Useful Segment / No match) and by leg, plus status counts. Never lists message content.
- `connections`: counts by status and the reported count. Never returns chat text (BR-20.4).

### 7.5 `GET /admin/routes/{id}/actions/context?action=`

- Validate action → `400 ACTION_INVALID`; 404 for missing route.
- Return ordered `effects` built from live counts:
  - **Deactivate** (BR-11.14, BR-20.3): "Switch the route to Inactive." → "Withdraw {n} leads." → "Withdraw {m} pending requests." → "Existing connections and chats are kept." → "Record the reason in the audit trail." Include route number and member name.
  - **HoldUnderReview** (BR-20.3, A-32): "Set the route to Under Review." → "Remove it from matching, so it produces no leads." → "Withdraw {n} leads." → "Record the reason in the audit trail." Warning: "The route stays out of matching until it is released."
- `requiresReason: true`, `reasonMinLength: 10`.

### 7.6 `POST /admin/routes/{id}/actions`

Request: `{ action, reason }`.

Common rules (AC-20.1, AC-20.2):

1. Admin authorisation and route existence (not deleted).
2. `reason`: trim; after trim ≥ 10 characters (V-45) and ≤ 500; else `400 REASON_REQUIRED` / `400 REASON_TOO_LONG`.
3. Action must be allowed for the current status; else `409 ACTION_NOT_ALLOWED` with the current status.
4. Never hard-delete anything (V-48). Never edit points, cities, days or times (BR-20.1).
5. Audit before/after with old/new status, counts, actor, timestamp and reason (BR-20.3).
6. Return the applied effect counts and fresh status.

#### Action rules

| Action | Precondition | Effect | Notification |
|---|---|---|---|
| **Deactivate** | status Active | Orchestration §8.1; status → Inactive; leads and pending requests Withdrawn; connections/chats kept | Per notification matrix; do not invent a template (UD-API-5) |
| **HoldUnderReview** | status Active or Draft | Orchestration §8.2; status → Under Review; route removed from matching; leads withdrawn | Per matrix (safety reason); no chat/member content |

- `Deactivate` has the effects of BR-11.14.
- `HoldUnderReview` removes the route from matching until released (A-32). Release is an M12 action (UD-API-4); M11 does not offer it here.

### 7.7 `GET /admin/leads`, `GET /admin/leads/{id}`

- List filters: `memberId`, `routeId`, `matchType`, `legType`, `status`, `converted`, date range, search, sort.
- `converted` = Requested or Connected. `conversionSummary` is computed over the **filtered set**, not the page.
- Lead detail returns the same item fields plus per-leg match reasons (the same plain-language explanation used to members) and the related connection status. **No chat text.**
- Read-only: there is no edit, dismiss, withdraw or request endpoint on this controller.

### 7.8 `GET /admin/connections`, `/funnel`, `/{id}`

- List filters: `status[]`, `reported`, date range, search, sort.
- `reported = true` when an open safety report involves the sender/receiver pair (`ISafetyAdminReader`); flag is a boolean, never a link to report text.
- `funnel` returns status counts for the period and `acceptanceRate = accepted / answered`, where `answered = accepted + rejected` (SC-20.4). `avgResponseMinutes` uses `sentOnUtc` → `respondedOnUtc` for answered rows only.
- `/{id}` returns counts, statuses and dates only. There is **no** message-read method anywhere in M11 (AC-20.4).

### 7.9 Matching Health endpoints

- `GET /admin/matching-health` returns summary tiles plus match mix, supply/demand and by-city in one call.
- `no-lead-routes`: Active routes with `now - activeSinceUtc > N days` and zero leads, N from query (default 7). Group/filter by Saathi/Raahi.
- `leads-per-route`: average and a fixed distribution (0, 1–2, 3–5, 6–10, 10+). Live counts.
- `match-mix`: share of Full Route versus Useful Segment per leg, from M4 lead-leg results.
- `stop-names/most-used` (BR-20.5): collect stop texts from Active routes, normalize (trim, collapse spaces, case-insensitive), group with `IPointMatchService` at CFG-22 using the **same** similarity and exact-number rule (D-22, A-21). Output canonical label + all variant spellings + counts. Never one-to-one pair grouping here; use transitive grouping within the threshold — **open decision UD-API-3** (proposed: greedy canonical grouping for stable output).
- `stop-names/unmatched`: stop texts on Active routes that never appear in any match pair. Include `nearestMatchedText` and `nearestSimilarity` so Admin sees why it fell below CFG-22 (SC-20.3).
- `funnel`: lead → request rate and request → accept rate for the period.
- `response-time`: average and buckets from sent to accepted/rejected.
- `supply-demand`: counts of Active Saathi routes versus Active Raahi routes.
- `by-city`: per active city, active routes, leads, lead → request rate, acceptance rate.

### 7.10 Exports (RPT extracts)

- `POST /admin/routes/export`, `/admin/leads/export`, `/admin/connections/export` accept the same filters as their list.
- CSV, UTF-8; columns mirror the on-screen table; no chat text; no contact details (BR-20.2, BR-20.4). Member names are included by default and can be dropped with `IncludeMemberNames = false` — **open decision UD-API-6**.
- If a future variant needs contact details, enforce the BR-19.12 masking rule and explicit confirmation; do not add it to M11 without product sign-off.
- Audit `MONITOR_EXPORTED` with monitor type, filter summary and row count. CSV injection guard (`=`, `+`, `-`, `@` prefixed with `'`). Row cap from config; `EXPORT_TOO_LARGE` when exceeded.

---

## 8. Orchestration and failure handling

### 8.1 Deactivate route (BR-20.3, BR-11.14)

Execute in this order and record each applied effect:

1. Validate reason and status (Active).
2. Set route status → Inactive via `IRouteAdminGateway.DeactivateRouteAsync` (M2 keeps version and history).
3. M4 withdraws the route's leads (`WithdrawnBySystem`).
4. M5 withdraws pending requests tied to those leads.
5. Connections and chats are **kept** (BR-11.14); do not touch them.
6. Audit `ROUTE_DEACTIVATED` with counts and reason.
7. Dispatch any notification the matrix requires (M9).

Failure isolation: steps are idempotent; if a step fails after the status change, return `500 ROUTE_DEACTIVATION_INCOMPLETE` with the applied steps; never report success unless the route status is Inactive.

### 8.2 Hold Under Review (BR-20.3, AC-20.3)

1. Validate reason and status (Active or Draft).
2. Re-read status inside the transaction; another Admin may have changed it → `409 ACTION_NOT_ALLOWED`.
3. Set route status → Under Review via `IRouteAdminGateway.HoldUnderReviewAsync`.
4. M4 withdraws the route's leads; the route must produce **no leads** while Under Review (AC-20.3). Matching (M3) already excludes Under Review.
5. Audit `ROUTE_HELD_UNDER_REVIEW` with reason.
6. Dispatch notification if the matrix requires it.

### 8.3 Read-only guarantees

- No monitor endpoint writes any M2/M4/M5/M6 row.
- The only writes in M11 are the two actions in §8.1–8.2 plus audit.
- No path exists to create, edit or delete a route, lead, request or connection (AC-20.1, SC-20.6).

### 8.4 Concurrency

- Re-read route status before acting; two Admins cannot both deactivate or hold the same route without the second failing cleanly.
- Reason is never optional or defaulted, including on retries.
- Health figures are read inside a consistent snapshot per request; long-running aggregates must label the period they cover.

---

## 9. Authorization and gates

| Caller | Allowed |
|---|---|
| Anonymous | Nothing |
| Member / Restricted member | Nothing (403 on every `/admin/*` monitoring route) |
| Admin | All M11 endpoints |
| Inactive/Blocked Admin | Rejected by Foundation session/status validation |

- Enforce `RoleId = Admin` in the controller; use `StatusCode(403)` for a plain reject.
- Never accept a target route id except from the route `{id}`.
- Log denied attempts per BR-4.2 (Foundation convention).
- BR-20.2 (full route visibility) is granted to Admin only; the same data is never exposed on member-facing DTOs.

### 9.1 Status transitions owned by M11

| From | To | Action | Condition |
|---|---|---|---|
| Active | Inactive | Deactivate | reason ≥ 10 (BR-20.3) |
| Active / Draft | UnderReview | HoldUnderReview | reason ≥ 10 (BR-20.3, A-32) |
| UnderReview | Active/Inactive | *(release)* | **M12 only** (UD-API-4) |

No other route status transition is available in M11.

---

## 10. Validation matrix

| ID | Rule | Where | Error |
|---|---|---|---|
| V-45 | Every route action needs a reason ≥ 10 characters | Action POST | `REASON_REQUIRED` |
| V-48 | No hard delete of any record | Whole module | no delete endpoint |
| BR-20.1 | Monitoring is read-only | No PUT/PATCH/DELETE | 405 / no route |
| BR-20.3 | Deactivate/Hold only with reason | Actions | `REASON_REQUIRED` |
| BR-20.4 | Never show chat text | All DTOs | no chat contract |
| BR-20.5 | Same similarity rule as matching | Stop-name endpoints, pointText filter | uses `IPointMatchService` + CFG-22 |
| BR-20.7 | Invalid routes first + rule name | Route list | sort + `invalidRuleName` |
| — | Action allowed for current status | Actions | `ACTION_NOT_ALLOWED` |
| — | Sort whitelisted | Lists | `SORT_INVALID` |
| — | Page size 1–100 | Lists | `PAGE_SIZE_INVALID` |
| — | Reason ≤ 500 characters | Actions | `REASON_TOO_LONG` |
| — | `days` for health between 1 and 90 | Health | `PERIOD_INVALID` |
| — | City must exist in City Master | Filters | `CITY_INVALID` |

No validation framework exists in the repo; implement and unit-test these in the application services.

---

## 11. Audit events (Section 8, BR-20.3)

| Event | Trigger | Recorded |
|---|---|---|
| `ROUTE_DEACTIVATED` | Deactivate | Active → Inactive; leads withdrawn; pending requests withdrawn; reason |
| `ROUTE_HELD_UNDER_REVIEW` | HoldUnderReview | Active/Draft → Under Review; leads withdrawn; reason |
| `MONITOR_EXPORTED` | Any monitor export | monitor type, filter summary, row count |
| `MONITOR_VIEWED` | *(optional)* full-route or connection detail view | route/connection id, actor — only if product wants view auditing (UD-API-7) |

Every row includes actor, actor role, UTC timestamp, reason and old/new where applicable. Audit is append-only. Never write chat text, full contact lists or message content into logs.

---

## 12. Config usage

| CFG | Use in M11 |
|---|---|
| CFG-1 | Departure time tolerance shown in matching context; used to explain why routes find no matches |
| CFG-2 | Maximum points per leg shown on Route Detail and used to detect Invalid routes (BR-6.2 keeps old routes valid) |
| CFG-4 | Minimum shared points shown in matching health context |
| CFG-19 | Whether a Return leg is expected; used in "Has Return leg" context and no-lead hints |
| CFG-22 | Text match threshold used by stop-name grouping and the point-text filter |

Read config through the Foundation reader; do not hard-code defaults.

---

## 13. Error responses

Use `BadRequest(new { code, message })`, `StatusCode(409, new { code, message })`, or `NotFound(new { code, message })`. Never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `ROUTE_NOT_FOUND` | 404 | "Route not found." |
| `LEAD_NOT_FOUND` | 404 | "Lead not found." |
| `CONNECTION_NOT_FOUND` | 404 | "Connection not found." |
| `REASON_REQUIRED` | 400 | "Enter a reason of at least 10 characters." |
| `REASON_TOO_LONG` | 400 | "Keep the reason under 500 characters." |
| `ACTION_INVALID` | 400 | "Choose a valid action." |
| `ACTION_NOT_ALLOWED` | 409 | "This action is not available for the route's current status." |
| `SORT_INVALID` | 400 | "Choose a valid sort order." |
| `PAGE_SIZE_INVALID` | 400 | "Page size must be between 1 and 100." |
| `PERIOD_INVALID` | 400 | "Choose a period between 1 and 90 days." |
| `CITY_INVALID` | 400 | "Choose a city from the list." |
| `EXPORT_TOO_LARGE` | 400 | "The export is too large. Narrow the filters." |
| `ROUTE_DEACTIVATION_INCOMPLETE` | 500 | "Deactivation did not finish. Review the applied steps and try again." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

Keep wording aligned with the UI plan's microcopy.

---

## 14. Privacy and security

- **Admin-only surface.** Every route checks the Admin role server-side; denial is logged (BR-4.2).
- **Full route visibility.** Admin sees all points as typed, including all members' routes; member-facing DTOs remain public-view only (BR-20.2, BR-4.9).
- **No chat content.** M11 has no method to read messages; Connection Monitor returns counts, statuses and dates only (BR-4.7, BR-20.4, AC-20.4).
- **No manual matching.** There is no endpoint to create a lead, assign a partner or connect two members (BR-4.6, SC-20.6).
- **No member/route edits.** Only the two route-status actions exist; all other fields are read-only (BR-20.1, BR-20.6).
- **Reason everywhere.** Both actions write the reason to the append-only audit (BR-20.3).
- **Export safety.** No contact details; CSV injection guard; audit each export (BR-20.2).
- **Logging.** Never log chat text, message bodies or full contact lists.

---

## 15. Build sequence (M11)

| Step | Work | Output |
|---|---|---|
| 1 | Add enums, DTOs, query contracts | Compiles |
| 2 | `IRouteAdminReader` + gateway (`Deactivate`, `HoldUnderReview`) with M2 | Shared with M10 |
| 3 | `RouteMonitorQueryService`: filters, invalid-first sort, point-text fuzzy filter | SCR-20.1 data |
| 4 | Route detail tabs: legs, versions, lead summary, connection summary | SCR-20.2 data |
| 5 | `IRouteAdminActionService` + action context builder | Dialog can render |
| 6 | Deactivation orchestration and Under Review orchestration with gates | AC-20.2, AC-20.3 |
| 7 | `LeadMonitorQueryService` with match types and conversion | SCR-20.3 data |
| 8 | `ConnectionMonitorQueryService` with reported flag, funnel, response time | SCR-20.4 data |
| 9 | `MatchingHealthQueryService`: no-lead, per-route, match mix, supply/demand, by city | SCR-20.5 data |
| 10 | Stop-name grouping and unmatched detection using `IPointMatchService` | BR-20.5 |
| 11 | Exports with filtering, audit and injection guard | RPT extracts |
| 12 | Controllers + Admin role gate + error codes | Postman-ready |
| 13 | Optional `raah_MonitoringDailyMetric` only if measured need | UD-API-1 |

Suggested folders:

```
Binsera.Core/Monitoring/Enums/RouteMonitorAction.cs, RouteListSort.cs, LeadListSort.cs, ConnectionListSort.cs
Binsera.Core/Monitoring/Models/Admin/*Dto.cs, *Query.cs, *Request.cs
Binsera.Core/Monitoring/Contracts/IRouteMonitorQueryService.cs,
                                      IRouteAdminActionService.cs,
                                      ILeadMonitorQueryService.cs,
                                      IConnectionMonitorQueryService.cs,
                                      IMatchingHealthQueryService.cs,
                                      IMonitoringExportService.cs
Binsera.Core/Monitoring/Contracts/Gateways/IRouteAdminReader.cs,
                                      IRouteAdminGateway.cs,
                                      ILeadAdminReader.cs,
                                      IPendingRequestAdminReader.cs,
                                      IConnectionAdminReader.cs
Binsera.Application/Monitoring/Services/Admin/RouteMonitorQueryService.cs,
                                           RouteAdminActionService.cs,
                                           LeadMonitorQueryService.cs,
                                           ConnectionMonitorQueryService.cs,
                                           MatchingHealthQueryService.cs,
                                           MonitoringExportService.cs
Binsera.API/Controllers/Admin/RoutesAdminController.cs,
                         LeadsAdminController.cs,
                         ConnectionsAdminController.cs,
                         MatchingHealthAdminController.cs
Binsera.Persistence/Configurations/Raah/MonitoringDailyMetricConfiguration.cs   // only if UD-API-1 approved
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahMonitoringDailyMetric.cs  // only if approved
```

---

## 16. Test plan

### 16.1 Unit

- Route filters: each status, type, city (start or end), has-return, has-open-report, lead buckets (0 / 1–5 / >5 boundaries), point text at 69/70/71% similarity and exact-number rule, date range.
- Invalid-first sort; sort whitelist; paging boundaries (page 0, > last, size 1/100/101).
- Route detail: legs as typed; versions change summary; lead counts by match type and leg; connection counts by status; reported count.
- Available actions: Deactivate only from Active; Hold from Active/Draft; none from Under Review/Inactive/Invalid.
- Reason: 9 vs 10 vs 500 vs 501 characters; whitespace only; trim.
- Deactivate orchestration order, effect counts, retry idempotency, partial-failure reporting; connections/chats untouched.
- Hold Under Review withdraws leads and produces no new leads (AC-20.3).
- Lead monitor: match type/leg/status/conversion filters; conversion summary over the filtered set.
- Connection monitor: status filters, reported flag, funnel math (`accepted / (accepted + rejected)`), response-time averaging (answered only).
- Matching health: N-day no-lead threshold, distribution buckets, match-mix per leg, supply/demand, by-city rates.
- Stop grouping: variants within CFG-22 group together; numbers that differ stay apart (A-21); unmatched list includes nearest similarity.
- Exports: columns, no chat/contact fields, audit, CSV injection guard, row cap.

### 16.2 Integration / API

- Missing token → 401; member token → 403; Admin token → 200.
- Envelope shape matches DTOs; export is a `FileResult` unaffected by the response envelope.
- Deactivate integration: route Inactive, leads Withdrawn, pending requests Withdrawn, connections/chats kept.
- Hold Under Review integration: route Under Review; matching run produces no leads.
- No endpoint accepts route/lead/request/connection edits and no delete or manual-match route exists.
- No response contains message text or a chat-content field.
- Health figures change after new leads/requests (live, not cached).
- Audit row per action and per export with reason/actor/old-new.
- Migration (if any) does not touch `member_Master`.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-20.1 Read-only | §6 notes; §8.3; §9; §16.2 |
| AC-20.2 Reason for deactivation | §7.6 common rules; §10 V-45; §16.1 |
| AC-20.3 Under Review excludes matching | §8.2; §16.1; §16.2 |
| AC-20.4 No chat content | §2.1, §5.4, §7.8, §14 |
| AC-20.5 Same rule as matching | §7.9 stop-name endpoints; `IPointMatchService` + CFG-22 |

SC-20.1…SC-20.7 map to §7 endpoints and §8 orchestration; SC-20.6 (no manual matching) is enforced by the absence of any create/assign contract in §2.2 and §6.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| UD-API-1 | Add a monitoring daily-metric projection? | No; live queries first, measure, then decide |
| UD-API-2 | Include Draft routes in the Route Monitor by default? | Yes in the list, but default sort puts Invalid first and Active next |
| UD-API-3 | Stop-name grouping: transitive vs canonical greedy | Canonical greedy for stable, explainable output at CFG-22 |
| UD-API-4 | Who releases an Under Review route? | M12 only; M11 offers only Hold. Add release here only if product disagrees |
| UD-API-5 | Notification on route deactivation/hold | Follow the notification matrix; do not invent a template |
| UD-API-6 | Should exports include member names? | Yes by default; `IncludeMemberNames=false` drops them; no contacts ever |
| UD-API-7 | Audit viewing of full route/connection detail? | No in Phase 1; add `MONITOR_VIEWED` only if product asks |
| UD-API-8 | Default health period | 7 days (screen-configurable 7/14/30) |
| UD-API-9 | Reason maximum length | 500 characters (spec only sets a minimum) |
| UD-API-10 | Route list default page size | 25, max 100 |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Purpose / Admin only | §1, §9 |
| SCR-20.1 Route Monitor | §5.1, §6.1, §7.1–7.2 |
| SCR-20.2 Route Detail | §5.2, §6.1, §7.3–7.6 |
| SCR-20.3 Lead Monitor | §5.3, §6.2, §7.7 |
| SCR-20.4 Connection Monitor | §5.4, §6.3, §7.8 |
| SCR-20.5 Matching Health | §5.5, §6.4, §7.9 |
| Route Monitor filters/columns | §5.1, §7.1 |
| Route Detail blocks | §5.2, §7.3–7.5 |
| Matching Health blocks | §5.5, §7.9 |
| BR-20.1 read-only | §1, §6, §8.3, §9 |
| BR-20.2 full route visibility | §7.3, §14 |
| BR-20.3 deactivate/hold with reason | §7.6, §8.1–8.2, §11 |
| BR-20.4 no chat text | §2.1, §5.4, §7.8, §14 |
| BR-20.5 same rule as matching | §7.1, §7.9 |
| BR-20.6 live figures | §2.3, §4.1, §7.9 |
| BR-20.7 Invalid routes first | §5.1, §7.1 |
| Data Impact | §8, §11 |
| Reports RPT-3…RPT-12 | §1, §7.10 |
| CFG-1/2/4/19/22 | §12 |
| V-45/V-48 | §10 |
| Statuses Route/Lead/Connection | §4.3, §9.1 |
| A-21/A-32/D-22 | §7.9, §8.2 |
| SC-20.1…SC-20.7 | §7, §8, §16.3 |
| AC-20.1…AC-20.5 | §16.3 |
| M2/M3/M4/M5/M12 dependencies | §2, §9.1, §17 |
