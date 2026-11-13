# M14 — Dashboards — API Plan

| | |
|---|---|
| **Module** | M14 — Dashboards (spec `docs/spec/14_M14_Dashboards.md`, functional specification Section 23) |
| **Document type** | API implementation plan: member dashboard composition, admin dashboard composition, period selector, alert strip, drill-down link contract, partial-failure handling, validation, sequencing, tests |
| **Build order** | #14 after M1–M13 (the dashboard reads their records) (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, global `ResponseFilterAttribute` envelope |
| **Companion** | UI plan: `14_M14_Dashboards_UI_Plan.md` |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M14 is the **read-only landing layer for both roles**. It owns no business data and no writes. It composes live counts and short queues from the modules that own the records, and returns the exact drill-down targets behind every Admin figure.

1. **Member Dashboard** (`GET /me/dashboard`, SCR-23.1) — setup prompts, My Route, new leads (Going/Return split, top three), requests received, requests sent, connections/unread, latest five notifications, safety links. Every item is the member's own data (BR-23.1).
2. **Admin Dashboard** (`GET /admin/dashboard`, SCR-23.2) — Members, Routes, Matching, Connections, Safety, Moderation, Alerts and Quick links. A **Today / 7 days / 30 days** selector applies to activity counts (BR-23.2).
3. **Alert strip** (`GET /admin/dashboard/alerts`) — locked accounts, invalid routes, members at the report threshold, failed-login spikes, and "legal text is still the sample" (CFG-15, CFG-32, M13 sample flag).
4. **Drill-down contract** — every Admin block returns a `drillDown` object (UI route + API route + query) so the UI never guesses the list behind a number (BR-23.3).
5. **Dashboard options** (`GET /admin/dashboard/options`) — period values, block keys, quick links and the live definitions used in tooltips (CFG-15/CFG-32).
6. **Partial-failure handling** — a slow or unavailable source must not blank the whole dashboard; the affected block returns `null` plus a warning and clear empty/error state (BR-23.6).

Out of scope (owned elsewhere):

| Concern | Owner |
|---|---|
| Member, profile, photo, verification status | Foundation / M1 |
| Route records, legs, status changes | M2 |
| Lead generation, ranking, dismissal | M3 / M4 |
| Connect / accept / reject / withdraw / disconnect | M4 / M5 |
| Chat content and unread counts | M6 |
| Rating values and moderation writes | M7 / M12 |
| Notifications list, unread counts and delivery | M9 |
| Member list, filters, status actions | M10 |
| Route monitor, matching health aggregates | M11 |
| Safety report queue, sanctions, flagged members | M12 |
| Configuration values and legal content | M13 |
| Report catalogue and exports | M15 |

**Hard non-goals:** there is **no** write endpoint in M14 — no number is typed or adjusted (BR-23.2); a Member Dashboard never returns platform totals or another member's data (BR-23.1); no dashboard endpoint returns chat text, email addresses or mobile numbers of other members (BR-23.4); no dashboard endpoint creates, edits or deletes a record; there is **no** dashboard-specific persistence table by default.

---

## 2. Module boundaries

### 2.1 Contracts consumed

Each contract is a **small read contract** on the owning module. If an existing query service already exposes the same data, M14 reuses it and adds no duplicate route. The exact method placement is open decision UD-API-1.

| Contract | Source | Methods used by M14 |
|---|---|---|
| `IMemberDashboardProfileReader` | M1 / Foundation | own display name, photo, journey role, email-verified flag, rating summary |
| `ICurrentRouteDashboardReader` | M2 | current route in Active → Draft → latest Inactive order, with Going/Return presence, days, times, vehicle, status and allowed actions |
| `ILeadDashboardReader` | M4 | new-lead counts split Going/Return and the top N public-safe lead cards |
| `IConnectionDashboardReader` | M5 | pending received and sent lists, accepted connection count |
| `IChatReadGateway` | M6 | `GetUnreadSummaryAsync(memberId)` → unread messages and conversations |
| `INotificationDashboardReader` | M9 | latest five notifications and unread count |
| `IMemberAdminStatsReader` | M10 / Foundation | member status counts, new-in-period, locked accounts, failed-login spikes |
| `IRouteAdminReader` | M2 / M11 | route status counts, with-Return count, top five Active routes by city |
| `ILeadMonitorQueryService` | M11 | leads in period, match-type mix, lead → request rate |
| `IMatchingHealthQueryService` | M11 | Active routes with no leads |
| `IConnectionMonitorQueryService` | M11 | connection status counts and acceptance rate |
| `IReportQueryService` | M12 | open and overdue report counts |
| `IFlaggedMemberQueryService` | M12 | members at the CFG-15 report threshold |
| `ISanctionQueryService` | M12 | current Restricted / Temporarily Blocked / Permanently Blocked counts |
| `IRatingModerationService` | M12 | rating comments awaiting review |
| `ILegalContentAlertReader` | M13 | `AnySampleCurrentAsync()` |
| `IAppConfigReader` | Foundation | CFG-15, CFG-19, CFG-32 read live |
| `IClock` | Foundation | India-time period boundaries, generated-at timestamp |

### 2.2 Contracts provided by M14

| Contract | Purpose | Consumer |
|---|---|---|
| `IMemberDashboardQueryService` | Compose and return the member dashboard and prompts | Member UI |
| `IAdminDashboardQueryService` | Compose one or more Admin blocks for a period | Admin UI |
| `IAdminDashboardAlertQueryService` | Compose the alert strip independently of the period selector | Admin UI, Admin shell |
| `IDashboardDrillDownLinkBuilder` | Build the UI route, API route and query for each count | M14 internals, reused by any future dashboard widget |

### 2.3 Rules

- **Read-only composition.** M14 never queries another module's tables directly and never writes; it goes through the contracts above.
- **Live counts.** No stored dashboard row and no Admin-editable number. Aggregates are computed at request time from the source records (BR-23.2).
- **Role separation.** The member endpoint derives `memberId` from the token; it never accepts a member id in the body or query. The Admin endpoint requires the Admin role (BR-23.1, BR-4.1).
- **Public-safe by default.** Member items reuse the same public view used by leads/requests: name, photo, gender, vehicle, verified badge, rating and current route. Never email, mobile, chat content or platform totals (BR-23.4, BR-4.9).
- **Drill-down is server-described.** Each Admin figure returns a `drillDown` with the target list route and query, so BR-23.3 is a contract, not a UI guess.
- **Period is explicit.** Stock counts (total/active/blocked) are as-of-now; activity counts (new members, leads, connections) carry the period window. The response labels both (see §4.3).
- **Partial failure is a first-class result.** A failed source yields `null` for its block and a `dataWarnings[]` entry; it never yields a fake zero (BR-23.6).
- **No deletion or mutation.** There is no dashboard `POST`, `PUT`, `PATCH` or `DELETE`.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; route prefix below; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interfaces in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` wrapped by `ResponseFilterAttribute` |
| Non-2xx | `BadRequest(new { code, message })`, `StatusCode(409, ...)`, `NotFound(...)`, `StatusCode(503, ...)`; **never** `Forbid()` / `Challenge()` (swallowed to 200) |
| Persistence | No new business table; live reads through module contracts |
| Migrations | None expected. If an optional read projection is approved later, use `yyyyMMddHHmmss_DescriptiveName` conventions |
| Validation | No FluentValidation; validate inside the application service; unit-test the rules |
| Audit | No audit rows for reads. Source failures and denied access may be logged as operational telemetry, never with member contact data |
| Current user | `UserId`, `UserName`, `RoleId` from `ApiControllerBase`; member id from the token |
| Admin gate | Enforce the Admin role in the controller; a plain reject uses `StatusCode(403)` |
| Background job | None added by M14 |

**Route prefixes (no `/api`):**

- Member: `/me/dashboard`.
- Admin: `/admin/dashboard`.
- No public dashboard routes.

---

## 4. Data model and definitions

### 4.1 No new table (default)

M14 does not own records. Counts are computed live from the source modules. Do **not** copy members, routes, leads, requests, connections, reports or ratings into an M14 table; a copied table would break BR-23.2 (figures must be live).

**Optional projection (open decision UD-API-5).** Only if the Admin dashboard is measured as too slow, add a short-lived read projection (for example `raah_DashboardDailyMetric`, keyed by metric date and block) written by a background aggregation. It must be labelled as an aggregate, must refresh within the period's granularity, and must never replace the live drill-down lists. Do not build it pre-emptively.

### 4.2 Enums (reuse, never redefine)

```
DashboardPeriod     { Today = 1, Last7Days = 2, Last30Days = 3 }   // new (M14)
DashboardBlock      { Members = 1, Routes = 2, Matching = 3,
                      Connections = 4, Safety = 5, Moderation = 6 } // new (M14)
DashboardSetupPromptType { VerifyEmail = 1, CreateRoute = 2,
                           AddReturnLeg = 3, AddPhoto = 4 }          // new (M14)
DashboardAlertCode  { LegalSample = 1, ReportsOverdue = 2, RoutesInvalid = 3,
                      ReportThreshold = 4, FailedLoginSpike = 5,
                      AccountsLocked = 6 }                           // new (M14)
DashboardAlertSeverity { Info = 1, Warning = 2, Danger = 3 }         // new (M14)

// Reuse (never redefine)
MemberStatus        // Foundation/9.1
RouteStatus         // M2/9.2
JourneyRole         // M1
VehicleType         // M1
LeadStatus          // M4/9.5
ConnectionStatus    // M5/9.6
SafetyReportStatus  // M8/M12/9.7
```

### 4.3 Period semantics

| Period | Window (India time, `Asia/Kolkata`) | Applies to |
|---|---|---|
| `Today` | Local midnight today → now | Activity: new members, leads created, connections sent/answered |
| `Last7Days` | Local midnight 6 days ago → now (7 calendar days including today) | Same |
| `Last30Days` | Local midnight 29 days ago → now (30 calendar days including today) | Same |

Rules:

- The response includes `fromUtc`, `toUtc`, `timeZone = "Asia/Kolkata"` and `dayCount`.
- **Stock counts are not period-scoped:** Total members, Active routes, Open reports, Pending requests, Flagged members, Restricted/Blocked are always "as of now".
- **Activity counts are period-scoped and labelled:** "new in period", "leads in period", "connections in period".
- A count of zero is returned as `0`, never `null`. `null` means "block not loaded / source failed" and always has a matching warning.

### 4.4 Definitions used by tooltips

| Term | Definition |
|---|---|
| Overdue report | A report still Submitted or Under Review after CFG-32 days (default 3) (A-53, AC-23.3) |
| Flagged member | A member reported by at least CFG-15 distinct members (default 3) (BR-17.9) |
| Locked account | An account currently locked by the CFG-9/CFG-10 rules |
| Failed-login spike | An account with at least CFG-9 failed logins in the last 24 hours, or locked in the last 24 hours (UD-API-3) |
| Acceptance rate | Accepted ÷ (Accepted + Rejected + Expired + Withdrawn) for connections created in the period; `null` when the denominator is 0 (UD-API-13) |
| Lead → request rate | Distinct leads created in the period that produced a request ÷ leads created in the period; `null` when no leads |
| Full Route vs Useful Segment | Lead-level match classification as shown on the lead card (M4). A lead with a Full Route leg is counted under Full Route; otherwise Useful Segment (UD-API-14) |
| Active routes by city | Active routes whose Start or End city is the city, top five by count, ties alphabetical (UD-API-12) |

---

## 5. DTOs (Binsera.Core)

Suggested namespace: `Binsera.Core.Dashboards.Models`, `Binsera.Core.Dashboards.Models.Admin`, `Binsera.Core.Dashboards.Contracts`.

### 5.1 Shared drill-down and warning

```
DrillDownDto
  uiRoute        string        // e.g. "/admin/safety"
  apiRoute       string        // e.g. "/admin/safety"
  query          object        // already URL-safe key/values
  label          string        // e.g. "View report queue"

DataWarningDto
  block          string        // Members | Routes | Matching | Connections | Safety | Moderation | Prompts | Alerts
  code           string        // SOURCE_UNAVAILABLE | SOURCE_TIMEOUT | PARTIAL_DATA
  message        string        // UI-safe, no internals
  retryable      bool
```

### 5.2 Member Dashboard (SCR-23.1)

```
MemberDashboardDto
  member            MemberHeaderDto
  setupPrompts      SetupPromptDto[]        // ordered; UI shows the first applicable
  myRoute           MyRouteSummaryDto?      // null when no route
  newLeads          NewLeadsBlockDto
  requestsReceived  RequestsReceivedBlockDto
  requestsSent      RequestsSentBlockDto
  connections       ConnectionsBlockDto
  notifications     NotificationsBlockDto
  safety            SafetyLinksDto
  generatedAtUtc    DateTime

MemberHeaderDto
  memberId          long
  displayName       string
  photoUrl          string?
  journeyRole       string          // Saathi | Raahi
  emailVerified     bool
  rating            { average: decimal?, count: int, isNewMember: bool }
  memberStatus      string          // Active | Restricted | ...
  restrictedUntil   DateTime?

SetupPromptDto
  type              string          // VerifyEmail | CreateRoute | AddReturnLeg | AddPhoto
  message           string
  actionLabel       string
  actionUiRoute     string?         // in-app navigation
  actionApi         string?         // e.g. the Foundation resend-verification route
  dismissible       bool            // always false in Phase 1

MyRouteSummaryDto
  routeId           long
  routeNumber       string?
  status            string          // Draft | Active | Inactive | Invalid | UnderReview
  journeyRole       string
  vehicle           string?         // Raahi only
  startCity         string?
  endCity           string?
  going             RouteLegSummaryDto?
  return            RouteLegSummaryDto?
  canEdit           bool
  canDeactivate     bool
  canReactivate     bool
  deactivateBlockedReason string?   // e.g. Under Review

RouteLegSummaryDto
  legType           string          // Going | Return
  days              string[]        // Mon..Sun
  departureTime     string          // HH:mm (rendered 12-hour by UI)
  startPoint        string
  endPoint          string
  pointCount        int

NewLeadsBlockDto
  total             int
  going             int
  return            int
  heading           string          // "Raahis for you" / "Saathis for you"
  top               LeadCardSummaryDto[]   // up to 3; reuse M4 public-safe shape
  seeAllDrillDown   DrillDownDto

LeadCardSummaryDto                 // reuse M4's existing list-card shape
  leadId, matchedMember { memberId, displayName, photoUrl, gender, vehicle,
                          verified, rating, routeSummary },
  matchByLeg        { going: string, return: string },   // FullRoute | UsefulSegment | NoMatch
  connectEligible   bool
  connectBlockedReason string?

RequestsReceivedBlockDto
  total             int
  items             RequestReceivedSummaryDto[]   // up to 5
  drillDown         DrillDownDto

RequestReceivedSummaryDto
  connectionId      long
  sender            PublicMemberSummaryDto
  sentAtUtc         DateTime
  expiresAtUtc      DateTime
  daysLeft          int
  canAccept         bool
  canReject         bool

RequestsSentBlockDto
  total             int
  items             RequestSentSummaryDto[]       // up to 5
  drillDown         DrillDownDto

RequestSentSummaryDto
  connectionId      long
  receiver          PublicMemberSummaryDto
  sentAtUtc         DateTime
  waitingDays       int
  canWithdraw       bool

ConnectionsBlockDto
  acceptedCount     int
  unreadConversations int
  unreadMessages    int
  drillDown         DrillDownDto

NotificationsBlockDto
  unreadCount       int
  items             NotificationSummaryDto[]       // latest 5; no chat content
  drillDown         DrillDownDto

SafetyLinksDto
  blockedMembers    DrillDownDto
  myReports         DrillDownDto
```

`PublicMemberSummaryDto` reuses the M1/M4 public view. It contains no email, mobile, chat text or platform totals.

Rules:

- An **Unverified** member gets only the `VerifyEmail` prompt; route/lead/request/connection blocks are returned as empty blocks with `locked = true` (add `locked` to each block) or omitted per UD-API-8. M14 never exposes route features before verification (BR-5.1, SC-23.6).
- `setupPrompts` is ordered **VerifyEmail → CreateRoute → AddReturnLeg → AddPhoto**; it includes every applicable prompt so the UI can show the first and refetch after acting.
- The `AddReturnLeg` prompt is shown when the current route has a Going leg and no Return leg and CFG-19 is off. The "would add leads" part is advisory (UD-API-4).
- `AddPhoto` is shown when the member has no profile photo.

### 5.3 Admin Dashboard (SCR-23.2)

```
AdminDashboardDto
  period            PeriodDto
  generatedAtUtc    DateTime
  requestedBlocks   string[]
  members           MemberBlockDto?
  routes            RoutesBlockDto?
  matching          MatchingBlockDto?
  connections       ConnectionsBlockDto?
  safety            SafetyBlockDto?
  moderation        ModerationBlockDto?
  alerts            AdminAlertDto[]?          // included unless blocks excludes Alerts
  quickLinks        QuickLinkDto[]
  dataWarnings      DataWarningDto[]
  definitions       DashboardDefinitionsDto

PeriodDto
  key               string          // Today | Last7Days | Last30Days
  label             string
  fromUtc           DateTime
  toUtc             DateTime
  timeZone          string          // Asia/Kolkata
  dayCount          int

MemberBlockDto
  total             int
  active            int
  unverified        int
  newInPeriod       int
  inactive          int
  restricted        int
  temporarilyBlocked int
  permanentlyBlocked int
  drillDowns        { total, active, unverified, newInPeriod, inactive,
                      restricted, temporarilyBlocked, permanentlyBlocked: DrillDownDto }

RoutesBlockDto
  activeSaathiRoutes int
  activeRaahiRoutes  int
  withReturnLeg      int
  invalid            int
  underReview        int
  activeByCity       { cityId, cityName, activeRoutes, drillDown }[]   // top 5
  drillDowns         { activeSaathiRoutes, activeRaahiRoutes, withReturnLeg,
                       invalid, underReview: DrillDownDto }

MatchingBlockDto
  leadsInPeriod      int
  fullRouteLeads     int
  usefulSegmentLeads int
  fullRoutePercent   decimal?      // over classified leads
  usefulSegmentPercent decimal?
  activeRoutesWithNoLeads int
  leadToRequestRate  decimal?      // 0..1 or null
  drillDowns         { leadsInPeriod, fullRouteLeads, usefulSegmentLeads,
                       activeRoutesWithNoLeads, leadToRequestRate: DrillDownDto }

ConnectionsBlockDto
  pending            int
  accepted           int
  rejected           int
  expired            int
  withdrawn          int
  disconnected       int
  acceptanceRate     decimal?      // 0..1 or null
  drillDowns         { pending, accepted, rejected, expired, withdrawn,
                       disconnected, acceptanceRate: DrillDownDto }

SafetyBlockDto
  openReports        int
  overdueReports     int
  flaggedMembers     int
  restrictedMembers  int
  blockedMembers     int
  drillDowns         { openReports, overdueReports, flaggedMembers,
                       restrictedMembers, blockedMembers: DrillDownDto }

ModerationBlockDto
  commentsAwaitingReview int
  drillDown          DrillDownDto

AdminAlertDto
  code               string        // LegalSample | ReportsOverdue | RoutesInvalid |
                                   // ReportThreshold | FailedLoginSpike | AccountsLocked
  severity           string        // Info | Warning | Danger
  count              int?          // null for the legal-sample flag
  title              string
  message            string
  actionLabel        string
  drillDown          DrillDownDto

QuickLinkDto
  key                string
  label              string
  description        string?
  uiRoute            string

DashboardDefinitionsDto
  reportOverdueDays  int           // CFG-32
  reportThreshold    int           // CFG-15
  acceptanceRateFormula string
  leadToRequestFormula string
  failedLoginWindowHours int
```

### 5.4 Admin Dashboard options

```
AdminDashboardOptionsDto
  periods            { key, label }[]
  blocks             { key, label }[]
  quickLinks         QuickLinkDto[]
  definitions        DashboardDefinitionsDto
  canView            bool
```

---

## 6. Endpoint catalogue

### 6.1 Member (SCR-23.1)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 1 | GET | `/me/dashboard` | Full member dashboard in one call | 200 `MemberDashboardDto` |
| 2 | GET | `/me/dashboard/prompts` | Setup prompts only, for a light refresh after verifying/creating a route/adding a photo | 200 `SetupPromptDto[]` |

There are **no** member dashboard write endpoints. Connect, dismiss, accept, reject and withdraw stay on M4/M5. The Resend-verification prompt calls the existing Foundation verification resend route, not a new M14 route.

### 6.2 Admin (SCR-23.2)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 3 | GET | `/admin/dashboard?period=Today&blocks=` | Full Admin dashboard, or a subset of blocks for a targeted refresh | 200 `AdminDashboardDto` |
| 4 | GET | `/admin/dashboard/alerts` | Alert strip only; not period-scoped | 200 `AdminAlertDto[]` |
| 5 | GET | `/admin/dashboard/options` | Periods, block keys, quick links, live definitions | 200 `AdminDashboardOptionsDto` |

Notes:

- `period` defaults to `Today`; unknown values return `PERIOD_INVALID`.
- `blocks` is an optional comma-separated subset of `members,routes,matching,connections,safety,moderation,alerts`. An unknown key returns `BLOCK_INVALID`. When omitted, all blocks plus alerts are returned.
- There is **no** dashboard write route, no export route (M15 owns exports), and no route that accepts a member id from the Admin caller for member data.
- `GET /admin/dashboard/alerts` is deliberately separate so the Admin shell can poll it cheaply without recomputing period counts.

---

## 7. Endpoint details

### 7.1 `GET /me/dashboard`

1. Authorise the member from the token. If the account is Inactive/Blocked, Foundation rejects the session before this endpoint.
2. Read the profile header (`IMemberDashboardProfileReader`).
3. Compute setup prompts in the fixed order (§5.2). If unverified, return the prompt and empty/locked blocks, then stop (SC-23.6).
4. Otherwise fan out **in parallel**:
   - current route (M2),
   - new leads (M4): counts by leg and top three,
   - pending received/sent requests (M5),
   - accepted connection count (M5) and unread summary (M6),
   - latest five notifications and unread count (M9).
5. Apply privacy shaping: only the public member summary on lead/request items; never email, mobile, chat text or platform totals.
6. Return `generatedAtUtc`.

Rules:

- A member with an Inactive route still sees connections, unread messages, notifications and safety links (SC-23.3); the new-leads block shows an empty state.
- A Restricted member sees the restriction banner data (`restrictedUntil`) but otherwise a normal dashboard (BR-21.6).
- Top leads are exactly the ranking returned by M4; M14 never re-sorts.
- If one source fails, its block is `null` with a warning; the rest render (BR-23.6).
- An unverified member's `locked` blocks carry no route/lead/request data.

### 7.2 `GET /me/dashboard/prompts`

- Returns the same ordered prompt list as §5.2 without loading the heavy blocks.
- Used after verifying email, creating a route, adding a Return leg or uploading a photo so the prompt disappears immediately (BR-23.5).
- If the member is unverified, returns only `VerifyEmail`.
- Prompt conditions are re-evaluated live; there is no stored "prompt dismissed" flag.

### 7.3 `GET /admin/dashboard`

1. Require the Admin role. A member token returns `403` via `StatusCode(403)`; log the denied attempt (BR-4.2).
2. Resolve the period window from `IClock` in India time (§4.3).
3. Resolve the requested block set (all by default).
4. Fan out counts in parallel per block:
   - **Members** — M10/Foundation status counts; new-in-period from the period window.
   - **Routes** — M2/M11 status counts; top five Active routes by city.
   - **Matching** — M11 leads in period, match-type mix, no-lead routes, lead → request rate.
   - **Connections** — M11 status counts and acceptance rate for connections created in the period.
   - **Safety** — M12 open/overdue reports, flagged, restricted, blocked (stock).
   - **Moderation** — M12 rating comments awaiting review (stock).
   - **Alerts** — §7.4.
5. Build a `DrillDownDto` for every figure (§8).
6. Return the `definitions` (CFG-32, CFG-15, formulas) so the UI can label tooltips without another call.

Rules:

- **Every Admin count drills down** (BR-23.3). A count without a drill-down is a bug.
- Counts are live and never cached beyond an optional short request-scoped cache (UD-API-5).
- When a block's source fails, set the block to `null`, add a `dataWarnings` row and keep the other blocks. Do not return `0` for a failed block.
- If **all** blocks fail, return `503 DASHBOARD_UNAVAILABLE`; otherwise `200` with partial data.
- Stock vs activity is explicit: `newInPeriod`, `leadsInPeriod` carry the period; the rest are as-of-now.
- Percentages are returned as `0..1` decimals (the UI formats `58%`); `null` when undefined.
- The response never contains member names, emails, mobiles or chat content. It is aggregate-only.

### 7.4 Alert strip rules

Alerts are computed independently of the period selector and ordered by severity then code:

| Order | Code | Condition | Count | Severity | Drill-down |
|---|---|---|---|---|---|
| 1 | `LegalSample` | Any current published legal version is a sample (M13 `IsSample`) | null | Warning | `/admin/legal` |
| 2 | `ReportsOverdue` | Reports still Submitted/Under Review after CFG-32 days | overdue count | Danger | `/admin/safety?overdue=true` |
| 3 | `RoutesInvalid` | Routes with status Invalid | invalid count | Warning | `/admin/routes?status=Invalid` |
| 4 | `ReportThreshold` | Members at the CFG-15 report threshold | flagged count | Warning | `/admin/flagged` |
| 5 | `FailedLoginSpike` | Accounts at the failed-login spike definition | count | Warning | `/admin/members?failedLoginSpike=true` |
| 6 | `AccountsLocked` | Accounts currently locked | locked count | Info | `/admin/members?locked=true` |

Rules:

- An alert with a zero count is omitted (except `LegalSample`, which is present while the sample is current).
- The legal-sample copy comes from BR-22.13 and matches the M13 screen exactly.
- `count` is `null` only for the legal-sample flag; every other alert has a positive count.
- Alerts are not dismissible and not Admin-editable.

### 7.5 `GET /admin/dashboard/options`

- Admin only.
- Returns the three periods, the six block keys, quick links and live definitions.
- Quick links: Matching Configuration (`/admin/config`), Notifications settings (`/admin/config?group=Notifications`), Legal Content (`/admin/legal`), Cities (`/admin/cities`).
- Definitions are read live from `IAppConfigReader`, so CFG-15/CFG-32 changes appear without a deploy.

---

## 8. Drill-down mapping (BR-23.3)

The server returns these targets in the `drillDown` objects. Query names match the owning module's API plan.

### 8.1 Members (M10)

| Figure | API route | Query |
|---|---|---|
| Total | `/admin/members` | — |
| Active / Inactive / Restricted / Temporarily Blocked / Permanently Blocked | `/admin/members` | `status={status}` |
| Unverified | `/admin/members` | `verification=Unverified` |
| New in period | `/admin/members` | `registeredFrom={fromUtc}&registeredTo={toUtc}` |
| Locked accounts | `/admin/members` | `locked=true` |
| Failed-login spike | `/admin/members` | `failedLoginSpike=true` |

### 8.2 Routes (M11 / M2)

| Figure | API route | Query |
|---|---|---|
| Active Saathi / Raahi routes | `/admin/routes` | `status=Active&journeyRole=Saathi` / `Raahi` |
| With Return leg | `/admin/routes` | `status=Active&hasReturn=true` |
| Invalid | `/admin/routes` | `status=Invalid` |
| Under Review | `/admin/routes` | `status=UnderReview` |
| Active routes by city | `/admin/routes` | `status=Active&cityId={cityId}` |

### 8.3 Matching (M11)

| Figure | API route | Query |
|---|---|---|
| Leads in period | `/admin/leads` | `createdFrom={fromUtc}&createdTo={toUtc}` |
| Full Route leads | `/admin/leads` | `matchType=FullRoute&createdFrom=...` |
| Useful Segment leads | `/admin/leads` | `matchType=UsefulSegment&createdFrom=...` |
| Active routes with no leads | `/admin/matching-health/no-lead-routes` | — |
| Lead → request rate | `/admin/matching-health` | `section=conversion` |

### 8.4 Connections (M11)

| Figure | API route | Query |
|---|---|---|
| Pending / Accepted / Rejected / Expired / Withdrawn / Disconnected | `/admin/connections` | `status={status}` |
| Acceptance rate | `/admin/matching-health` | `section=connections` |

### 8.5 Safety and moderation (M12)

| Figure | API route | Query |
|---|---|---|
| Open reports | `/admin/safety` | `status=Open` |
| Overdue reports | `/admin/safety` | `overdue=true` |
| Flagged members | `/admin/flagged` | — |
| Restricted members | `/admin/restricted` | `type=Restricted` |
| Blocked members | `/admin/restricted` | `type=Blocked` |
| Comments awaiting review | `/admin/rating-moderation` | `status=AwaitingReview` |

### 8.6 Alerts

| Alert | UI route |
|---|---|
| Legal sample | `/admin/legal` |
| Overdue reports | `/admin/safety?overdue=true` |
| Invalid routes | `/admin/routes?status=Invalid` |
| Report threshold | `/admin/flagged` |
| Failed-login spike | `/admin/members?failedLoginSpike=true` |
| Locked accounts | `/admin/members?locked=true` |

**Contract note.** If an owning module does not yet support a query parameter above, M14 still returns the target; the owning plan is the source of truth and should add the filter. M14 must not invent a duplicate list endpoint.

---

## 9. Orchestration and failure handling

### 9.1 Member composition

1. Authorise and read the header.
2. Evaluate prompts; short-circuit to the unverified state when needed.
3. Run the independent block reads in parallel with a per-source timeout (default 3 s).
4. Collect successes; convert failures into `null` blocks + warnings.
5. Shape privacy, cap list sizes (top 3 leads, 5 requests, 5 notifications).
6. Return one response.

Partial data is normal. A failed notifications source must not hide the leads block.

### 9.2 Admin composition

1. Authorise and resolve the period and requested blocks.
2. Start the block reads in parallel.
3. Each block is independent; a single slow block does not wait for the others beyond the timeout.
4. Build drill-downs and definitions.
5. Return `200` with partial data when at least one block succeeded; `503` only when every requested block failed.

No transaction is opened. There are no writes to roll back.

### 9.3 Live-count guarantee

- Every count is computed in the same request that returns it.
- A refresh after a new report, route change or request must reflect the change (AC-23.2).
- The optional projection (UD-API-5) is only allowed when its staleness is documented and still satisfies the "live" expectation at the product level.

---

## 10. Authorization and privacy

| Caller | Allowed |
|---|---|
| Anonymous | Nothing under `/me/dashboard` or `/admin/dashboard` |
| Unverified member | `/me/dashboard` returns only the verify prompt and locked blocks; M14 reveals no route or lead data (BR-5.1) |
| Member | `/me/dashboard`, `/me/dashboard/prompts` for their own data only |
| Member token on `/admin/dashboard*` | `403`, attempt logged (BR-4.2) |
| Admin | All `/admin/dashboard*` routes |
| Inactive/Blocked Admin | Rejected by Foundation session/status validation |
| Restricted member | Member dashboard works; restriction banner data included; no platform totals |

Privacy rules enforced by M14:

- Member blocks show only the member's own records plus the public view of matched people.
- No email, mobile, chat text, report description or Admin identity is returned.
- Admin blocks are aggregate-only. Names and contact details stay on the owning Admin screens (BR-23.4, BR-19.12).
- The member id is always derived from the token, never from a request body or query.

---

## 11. Validation matrix

| Rule | Where | Error |
|---|---|---|
| `period` is one of Today / Last7Days / Last30Days | Admin dashboard | `PERIOD_INVALID` |
| `blocks` keys are known and not duplicated | Admin dashboard | `BLOCK_INVALID` |
| Dashboards are read-only | Whole module | no write route exists |
| Member id from token, never from input | Member dashboard | no member-id parameter accepted |
| Admin role required | Admin dashboard | `FORBIDDEN` (403) |
| A failed source must not become a zero | All blocks | `dataWarnings` + `null` block |
| List caps: top 3 leads, 5 requests, 5 notifications | Member dashboard | fixed caps |
| Percentages 0..1 or null | Admin dashboard | computed only |
| No chat/contact data | All endpoints | structural (DTO has no such fields) |

No validation framework exists in the repo; implement and unit-test these in the application services.

---

## 12. Error responses

Use `BadRequest(new { code, message })`, `StatusCode(403)`, or `StatusCode(503, new { code, message })`. Never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `PERIOD_INVALID` | 400 | "Choose Today, last 7 days or last 30 days." |
| `BLOCK_INVALID` | 400 | "One of the dashboard sections is not recognised." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |
| `DASHBOARD_UNAVAILABLE` | 503 | "The dashboard is not available right now." |
| `MEMBER_NOT_FOUND` | 404 | "Your account could not be found." |
| `SOURCE_TIMEOUT` | — | Not returned as HTTP; recorded in `dataWarnings` with a UI-safe message |
| `SOURCE_UNAVAILABLE` | — | Not returned as HTTP; recorded in `dataWarnings` with a UI-safe message |

Keep wording aligned with the UI plan's microcopy.

---

## 13. Build sequence (M14)

| Step | Work | Output |
|---|---|---|
| 1 | Enums, DTOs, period calculator, drill-down builder | Compiles |
| 2 | Add the thin read methods to the owning module contracts (§2.1) or adapters | Sources readable |
| 3 | `IMemberDashboardQueryService`: header, prompts, route, unverified short-circuit | Member dashboard data |
| 4 | Member leads/requests/connections/notifications blocks with caps and privacy shaping | SCR-23.1 complete |
| 5 | Member partial-failure and prompt refresh endpoint | BR-23.5/23.6 |
| 6 | `IAdminDashboardQueryService` skeleton + period + `blocks` selection | SCR-23.2 shell |
| 7 | Members and Routes blocks + drill-downs | Tiles 1–2 |
| 8 | Matching and Connections blocks via M11 | Tiles 3–4 |
| 9 | Safety and Moderation blocks via M12 | Tiles 5–6 |
| 10 | Alert strip via M12/M13/Foundation + legal sample flag | Alerts |
| 11 | Options endpoint + live definitions | Quick links, tooltips |
| 12 | Controllers + Admin gate + error codes | Postman-ready |
| 13 | Partial-failure, timeout and all-fail handling | Resilience |
| 14 | Tests, privacy checks, acceptance mapping | §15 |

Suggested folders:

```
Binsera.Core/Dashboards/Enums/DashboardPeriod.cs, DashboardBlock.cs,
                                     DashboardSetupPromptType.cs,
                                     DashboardAlertCode.cs, DashboardAlertSeverity.cs
Binsera.Core/Dashboards/Models/MemberDashboardDto.cs, SetupPromptDto.cs,
                                MyRouteSummaryDto.cs, NewLeadsBlockDto.cs,
                                RequestsReceivedBlockDto.cs, RequestsSentBlockDto.cs,
                                ConnectionsBlockDto.cs, NotificationsBlockDto.cs,
                                SafetyLinksDto.cs, DrillDownDto.cs, DataWarningDto.cs
Binsera.Core/Dashboards/Models/Admin/AdminDashboardDto.cs, MemberBlockDto.cs,
                                     RoutesBlockDto.cs, MatchingBlockDto.cs,
                                     ConnectionsBlockDto.cs, SafetyBlockDto.cs,
                                     ModerationBlockDto.cs, AdminAlertDto.cs,
                                     QuickLinkDto.cs, DashboardDefinitionsDto.cs,
                                     AdminDashboardOptionsDto.cs
Binsera.Core/Dashboards/Contracts/IMemberDashboardQueryService.cs,
                                   IAdminDashboardQueryService.cs,
                                   IAdminDashboardAlertQueryService.cs,
                                   IDashboardDrillDownLinkBuilder.cs

Binsera.Application/Dashboards/Services/MemberDashboardQueryService.cs,
                                        AdminDashboardQueryService.cs,
                                        AdminDashboardAlertQueryService.cs,
                                        DashboardDrillDownLinkBuilder.cs,
                                        DashboardPeriodCalculator.cs

Binsera.API/Controllers/Member/MemberDashboardController.cs
Binsera.API/Controllers/Admin/AdminDashboardController.cs
```

No migration is expected for M14. Do not touch `member_Master`.

---

## 14. Test plan

### 14.1 Unit

- **Period calculator:** Today/7/30 boundaries in India time across midnight, month end and DST-free India zone; `dayCount`; UTC conversion; a late-evening request still uses the local day.
- **Prompts:** unverified → only VerifyEmail; verified with no route → CreateRoute; route without Return and CFG-19 off → AddReturnLeg; route with Return → no prompt; no photo → AddPhoto; order is always VerifyEmail → CreateRoute → AddReturnLeg → AddPhoto; a satisfied condition removes the prompt (AC-23.4, SC-23.1).
- **Member privacy:** lead cards and request senders contain only the public view; no email/mobile/chat fields exist on the DTOs.
- **Member blocks:** Going/Return lead split; top-three cap; pending received/sent caps; `daysLeft` at 0 and at the CFG-5 boundary; unread counts; latest five notifications; Inactive route keeps connections (SC-23.3).
- **Role wording:** Saathi heading "Raahis for you"; Raahi heading "Saathis for you".
- **Admin members block:** total/active/unverified/new-in-period/inactive/restricted/temporarily/permanently blocked counts; new-in-period uses the period window.
- **Admin routes block:** Active Saathi/Raahi split, with-Return, invalid, under review, top five cities and tie-break alphabetical.
- **Admin matching block:** leads in period, Full Route vs Useful Segment, no-lead routes, lead → request rate, null when no leads.
- **Admin connections block:** each status for the period, acceptance rate denominator and null case.
- **Admin safety block:** open/overdue counts; CFG-32 = 3 with a 4-day-old report counts overdue (AC-23.3); flagged at CFG-15 boundary (2 vs 3); restricted/blocked stock.
- **Alerts:** legal sample present while any current version is a sample; zero-count alerts omitted; order by severity/code; count null only for legal sample.
- **Drill-down builder:** every figure produces a non-empty UI route and API route; no count without a drill-down.
- **Partial failure:** a failed source yields `null` + warning, not zero; other blocks still render.

### 14.2 Integration / API

- Missing token → 401; member token on `/admin/dashboard*` → 403 and a denial log; Admin token → 200.
- `GET /me/dashboard` returns only the caller's data; two members see different dashboards; no platform totals (AC-23.1).
- A new report raises the Admin open/overdue count on the next refresh and its drill-down opens the queue (AC-23.2).
- CFG-32 change alters the overdue classification without a deploy (AC-23.3).
- Creating a route removes the CreateRoute prompt on the next `/me/dashboard/prompts` call (AC-23.4).
- Adding a photo removes the AddPhoto prompt; adding a Return leg removes the AddReturnLeg prompt.
- Unverified member receives only the verify prompt and locked blocks (SC-23.6).
- Inactive route: member still sees connections/unread (SC-23.3).
- Admin `blocks=members` returns only the Members block; unknown block returns `BLOCK_INVALID`.
- All sources failing returns `503`; one source failing returns `200` with the rest.
- No dashboard endpoint returns an email, mobile or message text.
- No write route exists for dashboards.

### 14.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-23.1 Role separation | §7.1, §10, §14.1, §14.2 |
| AC-23.2 Live counts and drill-down | §7.3, §8, §9.3, §14.2 |
| AC-23.3 Overdue reports | §4.4, §7.4, §14.1, §14.2 |
| AC-23.4 Setup prompt | §5.2, §7.2, §14.1, §14.2 |

SC-23.1…SC-23.6 map to the member prompt/block rules in §7.1–§7.2 and the Admin blocks/alerts in §7.3–§7.4.

---

## 15. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| UD-API-1 | Where do the dashboard read methods live? | Thin methods added to the owning module query contracts; M14 adapters if a module prefers not to change its public interface |
| UD-API-2 | One composite endpoint or per-block endpoints? | One composite with an optional `blocks` subset; alerts are separate |
| UD-API-3 | Failed-login spike definition | ≥ CFG-9 failed logins in the last 24 hours, or locked in the last 24 hours |
| UD-API-4 | "Return leg would add leads" heuristic | Show AddReturnLeg when a Going leg exists, Return is missing and CFG-19 is off; advisory only |
| UD-API-5 | Caching / projection | No cache by default; optional short-lived projection only after measurement |
| UD-API-6 | Period definition | India-time calendar days including today |
| UD-API-7 | Admin dashboard path | `/admin/dashboard` (UI may alias `/admin`) |
| UD-API-8 | Unverified member blocks | Return empty blocks with `locked = true`; do not reveal route/lead data |
| UD-API-9 | Member top lists | Top 3 leads; 5 requests received/sent; latest 5 notifications |
| UD-API-10 | Alert ordering | Legal sample first, then Danger → Warning → Info, then code order |
| UD-API-11 | "Routes by city" counts | Active routes whose Start or End city is the city |
| UD-API-12 | City tie-break | Alphabetical by city name |
| UD-API-13 | Acceptance-rate denominator | Accepted ÷ (Accepted + Rejected + Expired + Withdrawn) in the period |
| UD-API-14 | Full Route vs Useful Segment | Lead-level classification as M4 displays it |
| UD-API-15 | Dashboard read audit | None; optional operational telemetry for source failures and denied access |
| UD-API-16 | Quick links ownership | Server-provided list in the options endpoint, so routes change without a deploy |

---

## 16. Traceability

| Spec item | Where in this plan |
|---|---|
| Purpose / live numbers | §1, §2.3, §9.3 |
| Who uses it / first screen after login | §1, §10 |
| SCR-23.1 Member Dashboard | §5.2, §6.1, §7.1–§7.2 |
| SCR-23.2 Admin Dashboard | §5.3, §6.2, §7.3–§7.5 |
| Member blocks and setup prompts | §5.2, §7.1–§7.2 |
| Admin block contents and period selector | §4.3, §5.3, §7.3 |
| BR-23.1 role separation | §2.3, §10, §14.2 |
| BR-23.2 live counts | §2.3, §7.3, §9.3 |
| BR-23.3 every block drills down | §2.3, §8 |
| BR-23.4 no chat/contact data | §2.3, §10, §14.1 |
| BR-23.5 prompts disappear | §5.2, §7.2 |
| BR-23.6 usable when a block is empty | §7.1, §9.1–§9.2, §12 |
| Status lifecycle reflected | §4.2, §7.3 |
| SC-23.1…SC-23.6 | §7.1–§7.4, §14.3 |
| AC-23.1…AC-23.4 | §14.3 |
| CFG-15, CFG-32 | §4.4, §7.4, §7.5 |
| D-19, A-53 | §4.4, §10 |
| Data impact (none) | §4.1 |
| Reports produced (none; drill-downs to M11/M12/M15) | §8 |
| Dependencies M1–M13 | §2.1 |
