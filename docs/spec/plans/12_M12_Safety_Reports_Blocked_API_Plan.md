# M12 — Safety, Reports & Blocked Users — API Plan

| | |
|---|---|
| **Module** | M12 — Safety, Reports & Blocked Users (spec `docs/spec/12_M12_Safety_Reports_Blocked.md`, functional specification Section 21) |
| **Document type** | API implementation plan: admin safety read models, report lifecycle, sanction model, action orchestration, expiry job, validation, audit, sequencing, tests |
| **Build order** | #12 after M8 (report data) and M10 (member status actions) (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Companion** | UI plan: `12_M12_Safety_Reports_Blocked_UI_Plan.md` |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M12 is the **Admin-only safety control room**. It reviews member reports, applies proportionate, recorded and reversible-where-it-should-be action, manages restricted and blocked members, and moderates rating comments.

1. **Report queue** — every report, oldest first, with age, reason, reporter, reported member, status and flags; reports past CFG-32 days are marked overdue (SCR-21.1, BR-21.1).
2. **Report detail** — the report, reporter history, reported member history, the attached conversation (opened only on demand and logged), and related reports/connection (SCR-21.2).
3. **Take action** — Warning / Restriction / Temporary Block / Permanent Block / Dismiss, each with a mandatory reason, optional member message, and a preview of the exact effects (SCR-21.3, BR-21.5).
4. **Restricted & Blocked members** — everyone currently restricted or blocked, with end dates, reason and **Lift** for the reversible ones (SCR-21.4, BR-21.6, BR-21.7).
5. **Flagged members** — members reported by CFG-15 distinct members (SCR-21.5, BR-21.11).
6. **Rating moderation** — reported comments with **Hide** and a reason; stars keep counting (SCR-21.6, BR-21.13).
7. **Route hold during investigation** — hold the reported member's route Under Review from the report and release it afterwards (SC-21.7).
8. **Auto-expiry** — Temporary Blocks and dated Restrictions lift themselves when their end date passes (BR-21.6, BR-21.7, AC-21.2).
9. **Reporting hooks** — RPT-17…RPT-22 queries read from the same data (report *catalogue* stays in M15).

Out of scope (owned elsewhere):

| Concern | Owner |
|---|---|
| Report creation and member-side Block/Report flows | M8 |
| Storing block rows, report rows, escalation flag | M8 |
| Member status field itself and session revocation | Foundation / M10 |
| Route deactivation/hold/release mechanics | M2 / M11 |
| Lead withdrawal, request withdrawal, connection disconnect, chat close | M4 / M5 / M6 |
| Chat content and its access log | M6 |
| Rating records and the hide write | M7 |
| Notification/email delivery and templates | M9 |
| Configuration editing for CFG-14/15/32 | M13 |
| Report catalogue UI, exports and scheduled reports | M15 |
| Member profile fields | M1 |

**Hard non-goals:** a report is **never deleted** (BR-21.1, BR-17.10); the reporter is **never named** to the reported member (BR-21.10, BR-17.7); a **Permanent Block cannot be reversed** in the application (BR-21.8, A-50); Admin cannot create a report on a member's behalf (BR-21.16); chat is readable **only** from an open report and every opening is logged (BR-21.3, BR-4.7).

---

## 2. Module boundaries

M12 owns only its **sanction records** (warnings, restrictions and blocks with end dates). Everything else is composed through gateways.

### 2.1 Contracts consumed

| Contract | Source | Methods used by M12 |
|---|---|---|
| `IReportAdminGateway` | M8 | `GetQueueAsync`, `GetDetailAsync`, `MarkUnderReviewAsync`, `RecordActionAsync`, `DismissAsync`, `CloseAsync`, `GetFlaggedMembersAsync` |
| `IReportAccessPolicyGateway` | M8 | `IsReportOpenForAsync` (guard before any conversation read) |
| `IChatAdminAccessGateway` | M6 | `OpenForReportAsync(reportId, chatId, adminId)` — returns read-only messages and appends `raah_ChatAccessLog` |
| `IRatingModerationGateway` | M7 | `GetModerationQueueAsync(filter)`, `HideCommentAsync(ratingId, reason, adminId)` |
| `IMemberAdminIdentityService` | Foundation | `GetIdentityAsync(memberId)`, `SetStatusAsync(memberId, status, reason)` for Restricted/Temporarily Blocked/Permanently Blocked/Active |
| `IMemberSessionAdminService` | Foundation | `RevokeAllSessionsAsync(memberId)` on both block types |
| `IRouteAdminGateway` | M2/M11 | `GetActiveRouteSummaryAsync`, `DeactivateActiveRouteAsync`, `HoldUnderReviewAsync`, `ReleaseUnderReviewAsync` (release added for M12 — UD-API-3) |
| `ILeadAdminGateway` | M4 | `WithdrawAllLeadsAsync(memberId, reason)` |
| `IPendingRequestAdminGateway` | M5 | `WithdrawAllPendingAsync(memberId, reason)` |
| `IConnectionAdminGateway` | M5 | `DisconnectAllAcceptedAsync(memberId, reason)` |
| `IChatAdminGateway` | M6 | `CloseAllForMemberAsync(memberId, reason)` (no message content) |
| `IMemberProfileAdminReader` | M1 | member number, name, photo, role, vehicle, status, rating summary |
| `INotificationDispatcher` | M9 | warning/restriction/block notices to member, "reviewed" notice to reporter |
| `IAuditService` | Foundation | every lifecycle change and every conversation open |
| `IAppConfigReader` | Foundation | CFG-14, CFG-15, CFG-32 read live |
| `IRecordNumberGenerator` | Foundation | optional `SAN-{nnnnnn}` sanction number |
| `IClock` | Foundation | `ageDays`, overdue, end-date comparison, UTC |

### 2.2 Contracts provided by M12

| Contract | Purpose | Consumer |
|---|---|---|
| `IReportQueryService` | report queue, detail enrichment, filter options, safety summary tiles | Admin UI, M14 |
| `IReportActionService` | preview and execute Under Review / Take Action / Dismiss / Close / route hold-release | Admin UI |
| `ISanctionQueryService` | restricted & blocked list, per-member sanction history | Admin UI, M10 Safety tab, M14 |
| `ISanctionActionService` | lift a restriction or temporary block early | Admin UI |
| `IFlaggedMemberQueryService` | flagged members and their grouped reports | Admin UI, M14 |
| `IRatingModerationService` | moderation queue and hide | Admin UI |
| `IMemberSanctionReader` | current warning count, open sanction, active restriction/block for a member | M8 escalation display, M10 Safety tab, M14 |
| `ISafetyExportService` | CSV extracts for RPT-17/18/20/21/16 | Admin UI (M15 owns catalogue) |
| `IAdminAlertGateway` (implementation) | receives new-report/threshold alerts from M8 and surfaces them in the queue/dashboard | M8 |

### 2.3 Rules

- M12 never writes M4/M5/M6/M7/M8 tables directly. It calls their gateways in a defined order (§8).
- M12 **does** own `raah_MemberSanction` because end dates, lift history and reasons have no home in the report row (M8 stores only the decision reference) or in the member table.
- The report lifecycle itself stays in `raah_SafetyReport` (M8). `RecordActionAsync` stores `DecisionAction`/`DecisionReason`; M12 creates the matching sanction row in the same unit of work.
- Every read is live. Overdue and flagged counts are computed from the records, never stored as editable numbers.
- No contract in this module exposes a reporter identity except the Admin-only report detail.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; route prefix below; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interfaces in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` wrapped by `ResponseFilterAttribute` |
| Non-2xx | `BadRequest(new { code, message })`, `StatusCode(409, new { code, message })`, `NotFound(new { code, message })`; **never** `Forbid()` / `Challenge()` (swallowed to 200) |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new EF-managed table; `member_Master` untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id` PK, decimal precision, audit columns, `IsDeleted` |
| Validation | No FluentValidation; validate inside the application service; unit-test the rules |
| Audit | Append-only, old/new + reason + actor + UTC + source IP (Section 8) |
| Current user | `UserId`, `UserName`, `RoleId`, `MemberGraphId` from `ApiControllerBase`; Admin id is the actor |
| Export | Return a `FileResult`; `ResponseFilterAttribute` leaves file results untouched |
| Admin gate | Enforce the Admin role in the controller; a plain reject uses `StatusCode(403)` |
| Background job | `BackgroundService` + `Cronos` at `Binsera.API/BackgroundServices/MemberSanctionExpiryJob.cs` |

**Route prefix (no `/api`):** `/admin/safety`.

---

## 4. Data model

### 4.1 New table `raah_MemberSanction`

One row per Admin action that has a lasting record on the member: Warning, Restriction, Temporary Block, Permanent Block. Reports and their decisions stay in `raah_SafetyReport` (M8); this table holds the **end date, current state and lift history** that the report row cannot.

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `SanctionNumber` | nvarchar(20) | `SAN-000042`, generated once, never reused |
| `MemberId` | bigint | FK → `member_Master.Id`; the member acted on |
| `SanctionType` | tinyint | `MemberSanctionType` (§4.4) |
| `ReportId` | bigint null | FK → `raah_SafetyReport.Id`; null for a route-hold-only or M10-origin action |
| `Reason` | nvarchar(500) | Admin reason (≥ 10 chars, A-54); **Admin only** |
| `MessageToMember` | nvarchar(500) null | Optional note included in the notice; **never** names the reporter |
| `StartOnUtc` | datetime2 | When the action was applied |
| `EndDateUtc` | datetime2 null | Restriction: optional; Temporary Block: mandatory future date; Warning/Permanent: null |
| `Status` | tinyint | `SanctionStatus` (§4.4): `Active`, `Expired`, `Lifted`, `Recorded` |
| `LiftedOnUtc` | datetime2 null | Early lift |
| `LiftedByAdminId` | bigint null | |
| `LiftReason` | nvarchar(500) null | Required on early lift (≥ 10) |
| `AppliedByAdminId` | bigint | Actor |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false (sanctions are never deleted) |

Indexes:

- Unique `SanctionNumber`.
- `(MemberId, SanctionType, Status)` — "current active restriction/block for this member".
- `(Status, EndDateUtc)` — expiry job scan.
- `(SanctionType, StartOnUtc DESC)` — restricted & blocked list and RPT-20.
- `(ReportId)` — trace a report to the action taken.

Rules:

- A member has at most **one active Restriction** and at most **one active Block** at a time; a new action supersedes the previous active row (it is marked `Expired`/`Lifted` or simply superseded in the same transaction — UD-API-6).
- Warnings are `Recorded` immediately; they never change member status.
- Permanent Block rows are `Active` with no end date and **cannot** be lifted (BR-21.8).
- No hard delete. History stays for RPT-20 and the member's Safety tab.

### 4.2 No other new table

- Reports, decisions, reasons and escalation stay in M8.
- Chat access log stays in M6 (`raah_ChatAccessLog`).
- Rating hidden state stays in M7.
- Member status stays in Foundation/M10.
- Audit stays in the Foundation audit trail.

### 4.3 Enums (reuse where they exist)

```
// Existing (M8) — never redefine
SafetyReportStatus   { Submitted = 1, UnderReview = 2, ActionTaken = 3, Dismissed = 4, Closed = 5 }
ReportReason         { Harassment = 1, InappropriateMessages = 2, FakeProfile = 3,
                       SafetyConcern = 4, Spam = 5, Other = 6 }
ReportDecisionAction { Warning = 1, Restriction = 2, TemporaryBlock = 3, PermanentBlock = 4 }

// Existing Foundation — never redefine
MemberStatus         { Unverified = 1, Active = 2, Inactive = 3, Restricted = 4,
                       TemporarilyBlocked = 5, PermanentlyBlocked = 6 }

// New (M12)
MemberSanctionType   { Warning = 1, Restriction = 2, TemporaryBlock = 3, PermanentBlock = 4 }
SanctionStatus       { Active = 1, Expired = 2, Lifted = 3, Recorded = 4 }
SafetyReportAction   { MarkUnderReview = 1, TakeAction = 2, Dismiss = 3, Close = 4,
                       HoldRouteUnderReview = 5, ReleaseRoute = 6 }
ReportQueueSort      { OldestFirst = 1, NewestFirst = 2, OverdueFirst = 3, AgeDesc = 4 }
SanctionListSort     { EndDateAsc = 1, StartDesc = 2, MemberNameAsc = 3, Type = 4 }
FlaggedListSort      { ReportersDesc = 1, LatestReportDesc = 2, MemberNameAsc = 3 }
```

### 4.4 Sanction → member status mapping

| Sanction | Member status after | End date | Reversible |
|---|---|---|---|
| Warning | unchanged (Active/Restricted stays) | — | n/a; it is a record |
| Restriction | `Restricted` | optional, must be future | Yes — end date or early lift |
| Temporary Block | `TemporarilyBlocked` | mandatory, must be future | Yes — end date or early lift |
| Permanent Block | `PermanentlyBlocked` | none | **No** (BR-21.8) |

---

## 5. DTOs (Binsera.Core)

Suggested namespace: `Binsera.Core.Safety.Models.Admin`.

### 5.1 Report queue (SCR-21.1)

```
ReportQueueQuery
  status          string[]?   // Submitted|UnderReview|ActionTaken|Dismissed|Closed
  reason          string[]?   // the six ReportReason labels
  overdueOnly     bool?       // ageDays >= CFG-32 and status in Submitted/UnderReview
  flaggedOnly     bool?       // reported member at/above CFG-15 distinct reporters
  memberStatus    string?     // reported member currently Restricted/Blocked
  search          string?     // SAF number, reporter/reported name or MBR
  submittedFrom   DateTime?
  submittedTo     DateTime?
  sort            string?     // default OldestFirst
  page            int          // default 1
  pageSize        int          // default 25, max 100

ReportQueueItemDto
  reportId            long
  reportNumber        string        // SAF-nnnnnn
  ageDays             int
  isOverdue           bool          // CFG-32
  overdueSeverity     string        // None | Amber | Red (past 2x CFG-32)
  reason              string
  reasonLabel         string
  reporter            { memberId, memberNumber, fullName, photoUrl }
  reportedMember      { memberId, memberNumber, fullName, photoUrl, status }
  status              string
  statusLabel         string
  reportCountAgainstMember   int
  distinctReporterCount      int
  isEscalated         bool
  reportedMemberRestricted   bool
  submittedOn         DateTime

ReportQueuePageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters
  summary             SafetySummaryDto        // convenience; same as /summary
```

### 5.2 Report detail (SCR-21.2)

```
ReportDetailDto
  reportId, reportNumber, reason, reasonLabel, description
  status, statusLabel, ageDays, isOverdue, submittedOn
  conversationAttached bool
  chatId               long?
  connectionId         long?
  ratingId             long?
  targetType           string
  reportedMember       { memberId, memberNumber, fullName, photoUrl, status, role, vehicle }
  reporter             { memberId, memberNumber, fullName, photoUrl }
  reporterHistory      ReporterHistoryDto
  reportedMemberHistory ReportedMemberHistoryDto
  related              { connection { connectionId, connectionNumber, status }?,
                         otherOpenReports [ { reportId, reportNumber, reasonLabel, status, submittedOn } ] }
  timeline             [ { statusLabel, on, note? } ]   // note never names the reporter
  decisionAction       string?      // Admin only, never sent to member DTOs
  decisionReason       string?
  availableActions     AvailableSafetyActionDto[]
  conversationAccess   { canOpen, openLabel, loggedNotice }

ReporterHistoryDto
  reportsRaised        int
  dismissedCount       int
  actionTakenCount     int
  openCount            int

ReportedMemberHistoryDto
  warnings             int
  restrictions         int
  temporaryBlocks      int
  permanentBlocked     bool
  openReports          int
  currentStatus        string
  rating               { average, count, isNewMember }
  activeRoute          { routeId, routeNumber, status, startCity, endCity }?
  activeSanction       { sanctionId, type, status, startOn, endDate }?
  reportsFromOthers    [ { reportNumber, reasonLabel, status, submittedOn } ]

AvailableSafetyActionDto
  action               string   // MarkUnderReview | TakeAction | Dismiss | Close |
                                // HoldRouteUnderReview | ReleaseRoute
  allowed              bool
  disabledReason       string?
  requiresReason       bool
  isDestructive        bool
```

### 5.3 Take action (SCR-21.3)

```
TakeActionContextQuery
  action               string   // Warning|Restriction|TemporaryBlock|PermanentBlock|Dismiss

TakeActionContextDto
  member               { memberId, memberNumber, fullName, photoUrl, status }
  reportId, reportNumber
  action               string
  actionLabel          string
  effects              string[]     // ordered plain sentences, built from live counts
  counts               { leads, pendingRequests, acceptedConnections, chats, membersAffected }
  endDateRequired      bool         // TemporaryBlock
  endDateOptional      bool         // Restriction
  minEndDate           DateTime     // tomorrow (must be future)
  reasonMinLength      int          // 10
  messageMaxLength     int          // 500
  currentStatus        string
  currentSanction      { sanctionId, type, startOn, endDate }?
  warning              string?
  requiresDoubleConfirm bool        // true only for PermanentBlock

TakeActionRequest
  action               string       // Warning|Restriction|TemporaryBlock|PermanentBlock
  endDate              DateTime?    // mandatory for TemporaryBlock; optional for Restriction
  reason               string       // >= 10 after trim, <= 500
  messageToMember      string?      // <= 500

DismissReportRequest
  reason               string       // >= 10 after trim, <= 500

CloseReportRequest
  reason               string?      // required only if product wants a close note (UD-API-8)

SafetyActionResultDto
  reportId, reportNumber
  reportStatus, reportStatusLabel
  sanction             { sanctionId, sanctionNumber, type, status, startOn, endDate }?
  memberStatus         string?
  effectsApplied       { routeDeactivated, leadsWithdrawn, requestsWithdrawn,
                         connectionsDisconnected, chatsClosed, sessionsRevoked }
  performedAtUtc       DateTime
  message              string
```

### 5.4 Restricted & Blocked (SCR-21.4)

```
SanctionListQuery
  type            string[]?   // Restriction|TemporaryBlock|PermanentBlock (Warning excluded by default)
  status          string?     // Active|Expired|Lifted|Recorded (default Active)
  endingFrom      DateTime?
  endingTo        DateTime?
  search          string?     // member name/MBR, sanction number, report number
  sort            string?     // default EndDateAsc
  page, pageSize

SanctionListItemDto
  sanctionId, sanctionNumber
  member              { memberId, memberNumber, fullName, photoUrl, status }
  sanctionType        string
  sanctionTypeLabel   string
  reason              string
  messageToMember     string?
  startOn             DateTime
  endDate             DateTime?
  status              string
  statusLabel         string
  reportId, reportNumber
  appliedBy           string
  canLift             bool
  liftDisabledReason  string?    // "A permanent block cannot be reversed."

SanctionListPageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters

MemberSanctionHistoryDto
  memberId, memberNumber, fullName
  items               SanctionListItemDto[]
  warningCount, restrictionCount, temporaryBlockCount, permanentBlocked
```

### 5.5 Lift (SCR-21.4, BR-21.6/21.7)

```
LiftSanctionRequest
  reason              string   // >= 10 after trim, <= 500

LiftSanctionResultDto
  sanctionId, sanctionNumber
  sanctionStatus      string   // Lifted
  memberStatus        string   // Active
  liftedOnUtc         DateTime
  closuresRestored    false    // never true; closures are not restored
  message             string
```

### 5.6 Flagged members (SCR-21.5)

```
FlaggedListQuery
  search, page, pageSize, sort   // default ReportersDesc

FlaggedMemberItemDto
  member              { memberId, memberNumber, fullName, photoUrl, status, role }
  distinctReporterCount   int
  reportCount             int
  latestReportOn          DateTime
  isEscalated             bool
  reasons                 [ { reasonLabel, count } ]
  openReportCount         int
  reports                 [ { reportId, reportNumber, reasonLabel, status, submittedOn } ]

FlaggedListPageDto
  items, page, pageSize, totalCount, totalPages
  threshold           int      // CFG-15
```

### 5.7 Rating moderation (SCR-21.6, BR-21.13)

```
RatingModerationQuery
  status          string?    // Reported|Hidden (default Reported)
  search          string?    // rating number, rater/rated name or MBR
  submittedFrom, submittedTo  DateTime?
  page, pageSize

RatingModerationItemDto
  ratingId, ratingNumber, stars, comment
  commentHidden       bool
  submittedOn         DateTime
  rater               { memberId, memberNumber, fullName, photoUrl }
  ratedMember         { memberId, memberNumber, fullName, photoUrl }
  status              string
  hiddenOn            DateTime?
  hiddenReason        string?
  reportCount         int
  reportId            long?
  canHide             bool
  hideDisabledReason  string?   // "Comment moderation is turned off." (CFG-14)

HideRatingCommentRequest
  reason              string   // >= 10, <= 500

HideRatingCommentResultDto
  ratingId, ratingNumber, status, hiddenOn, message
```

### 5.8 Summary, options, requests

| DTO | Fields |
|---|---|
| `SafetySummaryDto` | `openReports`, `overdueReports`, `underReviewReports`, `flaggedMembers`, `restrictedMembers`, `temporarilyBlocked`, `permanentlyBlocked`, `ratingCommentsAwaitingReview`, `cfgs { overdueDays: CFG-32, flagThreshold: CFG-15, moderationEnabled: CFG-14 }` |
| `SafetyFilterOptionsDto` | report statuses, reasons, member statuses, sanction types, sorts, `overdueDays`, `flagThreshold`, `reasonMinLength=10`, `messageMaxLength=500` |
| `RouteHoldRequest` | `reason` (≥ 10) |
| `RouteHoldResultDto` | `routeId`, `routeNumber`, `status`, `message` |

---

## 6. Endpoint catalogue

Every endpoint requires an authenticated **Admin**; member tokens get `403`.

### 6.1 Report queue & detail (SCR-21.1, SCR-21.2)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 1 | GET | `/admin/safety/reports` | Report queue with filters and overdue flags | 200 `ReportQueuePageDto` |
| 2 | GET | `/admin/safety/reports/options` | Statuses, reasons, sorts, config thresholds | 200 `SafetyFilterOptionsDto` |
| 3 | GET | `/admin/safety/summary` | Dashboard safety tiles | 200 `SafetySummaryDto` |
| 4 | GET | `/admin/safety/reports/{reportId}` | Report detail (reporter hidden from members) | 200 `ReportDetailDto` |
| 5 | GET | `/admin/safety/reports/{reportId}/conversation` | **Read-only** chat for an open report; logs the access | 200 `ReportConversationDto` |
| 6 | GET | `/admin/safety/members/{memberId}/sanctions` | Sanction/warning history for a member | 200 `MemberSanctionHistoryDto` |
| 7 | GET | `/admin/safety/members/{memberId}/reports` | Reports raised by and against a member | 200 `ReportQueueItemDto[]` (paged) |

`ReportConversationDto` → `{ reportId, reportNumber, chatId, connectionId, canRead, openedOnUtc, messages: [{ messageId, senderMemberId, senderIsReporter, body, sentOn }] }`. `canRead = false` with no messages when the report is not open (not Submitted/UnderReview) or has no chat.

### 6.2 Report actions (SCR-21.3, SC-21.7)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 8 | GET | `/admin/safety/reports/{reportId}/actions/context?action=` | Effects, counts, end-date rules for the dialog | 200 `TakeActionContextDto` |
| 9 | POST | `/admin/safety/reports/{reportId}/mark-under-review` | Submitted → Under Review | 200 `SafetyActionResultDto` |
| 10 | POST | `/admin/safety/reports/{reportId}/action` | Warning / Restriction / Temporary Block / Permanent Block | 200 `SafetyActionResultDto` |
| 11 | POST | `/admin/safety/reports/{reportId}/dismiss` | Under Review → Dismissed (reason) | 200 `SafetyActionResultDto` |
| 12 | POST | `/admin/safety/reports/{reportId}/close` | Action Taken/Dismissed → Closed | 200 `SafetyActionResultDto` |
| 13 | POST | `/admin/safety/reports/{reportId}/hold-route` | Hold the reported member's route Under Review | 200 `RouteHoldResultDto` |
| 14 | POST | `/admin/safety/reports/{reportId}/release-route` | Release the route Under Review | 200 `RouteHoldResultDto` |

### 6.3 Restricted & Blocked / Flagged / Rating moderation (SCR-21.4–21.6)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 15 | GET | `/admin/safety/restricted-blocked` | Active restrictions and blocks with end dates | 200 `SanctionListPageDto` |
| 16 | GET | `/admin/safety/restricted-blocked/options` | Types, statuses, sorts, config | 200 `SafetyFilterOptionsDto` |
| 17 | POST | `/admin/safety/sanctions/{sanctionId}/lift` | Lift a restriction/temporary block early | 200 `LiftSanctionResultDto` |
| 18 | GET | `/admin/safety/flagged` | Members at/above CFG-15 distinct reporters | 200 `FlaggedListPageDto` |
| 19 | GET | `/admin/safety/rating-moderation` | Reported hidden-comment queue | 200 paged `RatingModerationItemDto` |
| 20 | GET | `/admin/safety/rating-moderation/options` | Status filter, CFG-14 gate | 200 `SafetyFilterOptionsDto` |
| 21 | POST | `/admin/safety/rating-moderation/{ratingId}/hide` | Hide a comment with a reason | 200 `HideRatingCommentResultDto` |

### 6.4 Exports (optional, RPT extracts)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 22 | POST | `/admin/safety/reports/export` | CSV of the filtered report queue | 200 file (CSV) |
| 23 | POST | `/admin/safety/restricted-blocked/export` | CSV of RPT-20 | 200 file (CSV) |
| 24 | POST | `/admin/safety/flagged/export` | CSV of RPT-21 | 200 file (CSV) |

Notes:

- There is **no** endpoint to create, edit or delete a report (BR-21.16, BR-21.1).
- There is **no** "undo permanent block" endpoint (BR-21.8, AC-21.6).
- There is **no** member-facing route in M12.
- The conversation endpoint is the **only** way M12 reads chat text, and only for an open report.

---

## 7. Endpoint details

### 7.1 `GET /admin/safety/reports`

- Filters combine with AND; arrays within a filter use OR.
- `ageDays = floor((now - SubmittedOnUtc) / 1 day)`.
- `isOverdue` = `status ∈ {Submitted, UnderReview}` **and** `ageDays >= CFG-32` (BR-21.1, AC-21.9). `overdueSeverity` = `Amber` at CFG-32, `Red` at `2 × CFG-32` or more (UI highlight rule; UD-API-9).
- Default sort `OldestFirst`; `OverdueFirst` sorts overdue rows to the top, then oldest.
- `flaggedOnly` uses the M8 escalation/distinct-reporter data read live.
- `reportedMemberRestricted` is true when the member is Restricted or Blocked; the UI shows a flag so Admin knows further functional action may not be needed (BR-21.15).
- `summary` is included so the queue header does not need a second call.
- `pageSize` 25 default / 100 max.

### 7.2 `GET /admin/safety/reports/{reportId}`

- 404 `REPORT_NOT_FOUND` when missing.
- Composes M8 detail with M1 profile summary, M7 rating, M2 active route, and M12 active sanction.
- **`reporterHistory`** carries `reportsRaised`, `dismissedCount`, `actionTakenCount`, `openCount` (needed by BR-21.12, SC-21.11). If M8's existing `ReportDetailDto` only exposes `reportsRaised`/`unfoundedCount`, add the two counts there (no endpoint change — UD-API-10).
- **`reportedMemberHistory`** carries warnings, restrictions, temporary/permanent blocks, open reports, current status, rating, active route and the list of reports from other members (BR-21.4, BR-21.11, SC-21.6).
- `related` shows the connection between the two members and any other open reports against the member (SCR-21.2).
- `availableActions` is computed per current status:
  - `MarkUnderReview`: only `Submitted`.
  - `TakeAction`, `Dismiss`: `Submitted` **or** `UnderReview` (Under Review is recommended but optional — BR-21.2).
  - `Close`: `ActionTaken` or `Dismissed`.
  - `HoldRouteUnderReview`: member has an Active route and no active Under-Review hold.
  - `ReleaseRoute`: member's route is Under Review.
- The reporter identity appears here and **only** here; it is never placed in any notice, sanction or member DTO (BR-21.10).

### 7.3 `GET /admin/safety/reports/{reportId}/conversation`

- Guard: `IReportAccessPolicyGateway.IsReportOpenForAsync(reportId, chatId, connectionId)` must be true (BR-21.3, BR-4.7).
- If the report has no attached chat → `200 { canRead: false, messages: [] }` with `conversationAttached = false` in the detail.
- If the report is not open (Action Taken/Dismissed/Closed) → `409 REPORT_NOT_OPEN` (no content).
- On success delegates to `IChatAdminAccessGateway.OpenForReportAsync`, which returns messages read-only and appends `raah_ChatAccessLog` **in the same call**.
- M12 additionally writes audit `REPORT_CONVERSATION_VIEWED` with report number, chat id, Admin and UTC (AC-21.1, BR-21.3).
- The response never includes the other member's contact details and never a write/send capability.

### 7.4 `GET /admin/safety/reports/{reportId}/actions/context?action=`

- Validate `action` ∈ `{Warning, Restriction, TemporaryBlock, PermanentBlock, Dismiss}` → else `400 ACTION_INVALID`.
- Build ordered `effects` from live counts. Examples:
  - **Warning** (BR-21.5, SC-21.1): "Record a warning on the member's history." → "The member's status and connections do not change." → "Send the member a notice (in-app and email)." → "Tell the reporter the report was reviewed." → "Record the reason in the audit trail."
  - **Restriction** (BR-21.6, A-50): "Set the member to Restricted." → "They cannot send connection requests{ until <date>}." → "They can still log in, chat and accept requests." → "Send the member a notice (in-app and email)." → "Tell the reporter the report was reviewed."
  - **Temporary Block** (BR-21.7, BR-21.9): "Set the member to Temporarily Blocked until <date>." → "End their sessions." → "Switch off their route." → "Withdraw {n} pending requests." → "Disconnect {m} connections and close {c} chats." → "Withdraw their leads." → "Send the member a notice (in-app and email)." → "Tell the reporter the report was reviewed." Warning: "The route and connections are not restored when the block lifts."
  - **Permanent Block** (BR-21.8): same closures as Temporary Block → "The block cannot be reversed in the application." → "The email cannot register again." extra `requiresDoubleConfirm = true`.
  - **Dismiss** (BR-21.12, SC-21.5): "Dismiss the report with this reason." → "The report record stays." → "Tell the reporter the report was reviewed." → "The reported member is not told anything."
- `endDateRequired` true only for Temporary Block; `endDateOptional` true only for Restriction; `minEndDate` = tomorrow in India time (must be strictly future).
- `currentSanction` tells Admin if the member is already restricted/blocked so the step is proportionate (BR-21.4).
- `reasonMinLength = 10`, `messageMaxLength = 500`.
- `counts` are live per member; a member with no route/requests/connections shows zeros and the effects read accordingly.

### 7.5 `POST /admin/safety/reports/{reportId}/mark-under-review`

- Only `Submitted` → `UnderReview`; else `409 REPORT_STATUS_INVALID`.
- Calls `IReportAdminGateway.MarkUnderReviewAsync(reportId, adminId)`.
- Audit `REPORT_MARKED_UNDER_REVIEW` with actor/time (BR-21.2).

### 7.6 `POST /admin/safety/reports/{reportId}/action`

Common rules (AC-21.7, BR-21.5):

1. Admin authorisation and report existence.
2. Report status must be `Submitted` or `UnderReview`; else `409 REPORT_STATUS_INVALID` (BR-21.15 allows reviewing a report about an already Inactive/Blocked member, but not a closed report).
3. `reason`: trim; after trim ≥ 10 (A-54) and ≤ 500 → else `400 REASON_REQUIRED` / `400 REASON_TOO_LONG`.
4. `messageToMember`: trim; ≤ 500; optional.
5. `action` must be one of the four; `Dismiss` uses its own endpoint.
6. `endDate`:
   - Temporary Block: required, strictly in the future → else `400 END_DATE_REQUIRED` / `400 END_DATE_INVALID`.
   - Restriction: optional; when present must be strictly future.
   - Warning / Permanent Block: must be empty → `400 END_DATE_NOT_ALLOWED`.
7. A member already Permanently Blocked can still be warned? **No further functional action is needed** (BR-21.15): return `409 MEMBER_ALREADY_BLOCKED` for Restriction/Block actions; allow Dismiss/Close and Warning-as-record only if product confirms (UD-API-7).
8. Never delete anything (V-48).
9. Execute orchestration §8.2 inside one unit of work where possible; record each applied effect.
10. Audit before/after with statuses, sanction, counts, reason, actor, UTC.
11. Return `SafetyActionResultDto` with the fresh report and member status.

#### Action-specific rules

| Action | Report transition | Member status | Sanction row |
|---|---|---|---|
| Warning | → ActionTaken | unchanged | `Warning`, `Recorded` |
| Restriction | → ActionTaken | → Restricted | `Restriction`, `Active`, `EndDateUtc?` |
| Temporary Block | → ActionTaken | → TemporarilyBlocked | `TemporaryBlock`, `Active`, `EndDateUtc` required |
| Permanent Block | → ActionTaken | → PermanentlyBlocked | `PermanentBlock`, `Active`, no end date |

- Notifications per §8.4; **never** name the reporter (BR-21.10, AC-21.5).
- Reporter always receives only "Your report has been reviewed" (BR-17.8).

### 7.7 `POST /admin/safety/reports/{reportId}/dismiss`

- Only `Submitted` or `UnderReview` → `Dismissed`; reason required (BR-21.12).
- Calls `IReportAdminGateway.DismissAsync(reportId, adminId, reason)`.
- No member notice; reporter gets "Your report has been reviewed".
- Audit `REPORT_DISMISSED` with reason.
- Dismissing never changes reported-member status.

### 7.8 `POST /admin/safety/reports/{reportId}/close`

- Only `ActionTaken` or `Dismissed` → `Closed`; final state (BR-21.1 lifecycle).
- Calls `IReportAdminGateway.CloseAsync`.
- Audit `REPORT_CLOSED`.

### 7.9 Route hold / release (SC-21.7)

- `hold-route` requires reason ≥ 10 and the member to have an Active route → else `409 NO_ACTIVE_ROUTE`.
  - Calls `IRouteAdminGateway.HoldUnderReviewAsync(routeId, reason, adminId)` (M11's action semantics, A-32).
  - Audit `REPORT_ROUTE_HELD`.
- `release-route` requires the route to be Under Review → else `409 ROUTE_NOT_HELD`.
  - Calls `IRouteAdminGateway.ReleaseUnderReviewAsync(routeId, reason, adminId)` — the release M11 deliberately does not offer (UD-API-3, UD-API-4 in M11).
  - Audit `REPORT_ROUTE_RELEASED`.
- Holding/releasing a route does not change report status and does not change member status.
- These are optional investigation aids; the route hold is visible on the member detail Route tab (M10/M11).

### 7.10 `GET /admin/safety/restricted-blocked`

- Default: `status = Active`, `type ∈ {Restriction, TemporaryBlock, PermanentBlock}` (Warnings are history, not this screen).
- `Warnings` can be requested explicitly but the UI does not show them here.
- `canLift` = `type ∈ {Restriction, TemporaryBlock}` **and** `status = Active`; Permanent Block gets `canLift = false` and reason "A permanent block cannot be reversed."
- Rows whose `EndDateUtc` has passed but the expiry job has not yet run are shown as `Expired` and `canLift = false` (the job will complete the status change) — UD-API-5.
- `endDate` null on a Restriction means "until lifted"; the UI shows "No end date".
- Numbered pager 25/100.

### 7.11 `POST /admin/safety/sanctions/{sanctionId}/lift`

- Allowed only for `Restriction` and `TemporaryBlock` with `Status = Active`; else:
  - Permanent Block → `409 PERMANENT_BLOCK_FINAL` ("A permanent block cannot be reversed." — AC-21.6).
  - Warning → `409 SANCTION_NOT_LIFTABLE`.
  - Already Lifted/Expired → `409 SANCTION_NOT_ACTIVE`.
- Reason required ≥ 10 (BR-21.6, BR-21.7).
- Effect: sanction `Status = Lifted`, `LiftedOnUtc/By/Reason`; member `SetStatusAsync(Active, reason)`; audit `SANCTION_LIFTED`.
- **Closures are not restored**: route stays Inactive, connections stay Disconnected, chats stay Closed (BR-21.7, BR-19.3). Response `closuresRestored = false`.
- Notify the member that the restriction/block was lifted (in-app; email for temporary block — UD-API-11).

### 7.12 `GET /admin/safety/flagged`

- Returns members with `distinctReporterCount >= CFG-15`, counting reports whose status ≠ `Dismissed` in the M8 escalation window (default rolling 30 days; inherited from M8 §8.8).
- The escalation flag is read live and matches M8's `EscalatedOnUtc` and alert.
- `reasons` is a breakdown by reason label so Admin sees the pattern (BR-21.11, SC-21.6).
- `reports` returns the report references so Admin can open the reports together; the UI never shows chat text here.
- Threshold shown from CFG-15 (not hard-coded).

### 7.13 Rating moderation (BR-21.13, AC-21.10)

- `GET /rating-moderation` delegates to `IRatingModerationGateway.GetModerationQueueAsync` with the filters; default `status = Reported`, newest first.
- `GET /options` returns `moderationEnabled = CFG-14`; when off, the queue is read-only and `canHide = false` with the reason "Comment moderation is turned off."
- `POST /{ratingId}/hide` requires reason ≥ 10; calls `HideCommentAsync`, which:
  - sets `Status = Hidden` and `HiddenOnUtc/By/Reason` (M7),
  - leaves `RatingAverage`/`RatingCount` untouched (stars still count, BR-16.11),
  - notifies the rater (M9),
  - audit `RATING_COMMENT_HIDDEN` (no comment text in audit).
- Second hide → `409 COMMENT_ALREADY_HIDDEN`.
- Hiding does **not** change the associated safety report automatically; Admin dismisses/closes that report separately if one exists (UD-API-12).

### 7.14 Exports

- Same filters as the list; CSV UTF-8; columns mirror the table.
- Report queue export includes reporter identity (Admin-only) but **never** chat text or description? The description is part of the report and may be included for RPT-17 only if product confirms — **open decision UD-API-13** (default: include report description, never chat text).
- Restricted/blocked export includes reason (Admin-only).
- CSV injection guard (`=`, `+`, `-`, `@` prefixed with `'`); row cap from config; `EXPORT_TOO_LARGE` when exceeded.
- Audit `SAFETY_EXPORTED` with type, filter summary and row count.

---

## 8. Orchestration and failure handling

### 8.1 Warning (BR-21.5, SC-21.1)

1. Validate report open and reason.
2. Insert `raah_MemberSanction` (`Warning`, `Recorded`).
3. `IReportAdminGateway.RecordActionAsync(reportId, adminId, Warning, reason)` → report `ActionTaken`.
4. Notify member: in-app + email (never names reporter; optional `messageToMember`). Notify reporter: reviewed.
5. Audit `WARNING_ISSUED`.
6. No status/session/route/connection/chat change.

### 8.2 Restriction (BR-21.6, AC-21.3, SC-21.2, SC-21.8)

1. Validate report open, reason, optional end date future.
2. If the member is currently Permanently Blocked → `409 MEMBER_ALREADY_BLOCKED` (BR-21.15).
3. Insert `raah_MemberSanction` (`Restriction`, `Active`, `EndDateUtc?`).
4. `IMemberAdminIdentityService.SetStatusAsync(memberId, Restricted, reason)`.
5. `IReportAdminGateway.RecordActionAsync(... Restriction ...)` → `ActionTaken`.
6. Notify member (in-app + email) and reporter (reviewed).
7. Audit `RESTRICTION_APPLIED` with end date.
8. **No** session revocation, route change, request withdrawal, connection or chat change. Restriction only blocks sending new requests (A-50, AC-21.3).

### 8.3 Temporary Block (BR-21.7, BR-21.9, AC-21.2, AC-21.4, SC-21.3)

Execute in this order; record each applied effect:

1. Validate report open, reason, mandatory future end date.
2. Re-read member status inside the transaction; if already Permanently Blocked → `409 MEMBER_ALREADY_BLOCKED`.
3. Insert `raah_MemberSanction` (`TemporaryBlock`, `Active`, `EndDateUtc`).
4. `IMemberSessionAdminService.RevokeAllSessionsAsync(memberId)`.
5. `IRouteAdminGateway.DeactivateActiveRouteAsync(memberId, reason, adminId)` — route Inactive; leads and pending requests withdrawn per M2/M5.
6. `IPendingRequestAdminGateway.WithdrawAllPendingAsync(memberId, reason)` — any remaining pending.
7. `IConnectionAdminGateway.DisconnectAllAcceptedAsync(memberId, reason)` — Accepted → Disconnected.
8. `IChatAdminGateway.CloseAllForMemberAsync(memberId, reason)` — chats Closed.
9. `ILeadAdminGateway.WithdrawAllLeadsAsync(memberId, reason)` — both ways.
10. `IMemberAdminIdentityService.SetStatusAsync(memberId, TemporarilyBlocked, reason)`.
11. `IReportAdminGateway.RecordActionAsync(... TemporaryBlock ...)`.
12. Notify member (in-app + email), reporter (reviewed).
13. Audit `TEMPORARY_BLOCK_APPLIED` with counts and end date.

Failure isolation:

- Steps are idempotent; if a step fails, stop and keep the member **not** Blocked unless step 10 already succeeded.
- Return `500 SAFETY_ACTION_INCOMPLETE` with the applied steps; never report success unless the member status is `TemporarilyBlocked`.
- The route, connections and chats are **not restored** when the block lifts (BR-19.3, BR-21.7).

### 8.4 Permanent Block (BR-21.8, AC-21.6, SC-21.4)

Same steps as §8.3 with:

- No end date; member status `PermanentlyBlocked`.
- `requiresDoubleConfirm = true` in the context and the API expects the client to have confirmed; the server cannot enforce UI intent, so it enforces the reason and finality.
- Email notice only (member cannot log in to see in-app).
- Audit `PERMANENT_BLOCK_APPLIED`.
- There is **no** lift path (AC-21.6). The email stays registered and blocked from re-registration (BR-5.3, M8 registration).
- `IReportAdminGateway.RecordActionAsync(... PermanentBlock ...)`.

### 8.5 Dismiss (BR-21.12, SC-21.5)

1. Validate report open and reason.
2. `IReportAdminGateway.DismissAsync(reportId, adminId, reason)`.
3. Reporter notified "Your report has been reviewed"; reported member not told.
4. Audit `REPORT_DISMISSED`.

### 8.6 Expiry job (BR-21.6, BR-21.7, AC-21.2)

`MemberSanctionExpiryJob` (`BackgroundService`, config-driven `Enabled`/`Cron`/`TimeZone`):

1. Find `raah_MemberSanction` where `Status = Active` and `EndDateUtc <= nowUtc` and `SanctionType ∈ {Restriction, TemporaryBlock}`.
2. For each, in a transaction:
   - Re-read the sanction; skip if already `Lifted`/`Expired`.
   - `SetStatusAsync(memberId, Active, "End date reached")` **only if** the member's current status matches the sanction type (do not overwrite a newer Permanent Block or a newer sanction).
   - Mark sanction `Expired`.
   - If `TemporaryBlock`, notify by email that the block has ended and they can log in; the route is not reactivated and connections/chats remain closed (BR-21.7).
   - If `Restriction`, notify in-app that they can send requests again.
   - Audit `SANCTION_EXPIRED` (system actor).
3. Idempotent; safe to re-run.

Also expose a **lazy read guard**: when a login/status check reads a member who is Restricted/TemporaryBlocked and the latest active sanction is past its end date, the status is treated as Active even if the job has not yet run (defence in depth). The job remains the primary mechanism.

### 8.7 Concurrency

- Re-read report status and member status before acting; two Admins cannot both act on the same report without the second failing cleanly (`409`).
- A sanction row is created at most once per action; retries must not duplicate.
- Reason is never optional or defaulted, including on retries.
- A new action supersedes an older active sanction of the same member in the same transaction (UD-API-6).

---

## 9. Authorization and gates

| Caller | Allowed |
|---|---|
| Anonymous | Nothing |
| Member / Restricted member | Nothing (403 on every `/admin/safety/*` route) |
| Admin | All M12 endpoints |
| Inactive/Blocked Admin | Rejected by Foundation session/status validation |
| Temporarily/Permanently Blocked member | Cannot log in, so cannot reach any route |

- Enforce `RoleId = Admin` in the controller; use `StatusCode(403)` for a plain reject.
- Never accept a member id except where the route names it (`/members/{memberId}/...`), and always verify the report/member exists.
- Log denied attempts per BR-4.2.
- Conversation access is authorised per report and logged on every open (AC-21.1).

### 9.1 Status transitions owned by M12

Report (delegated to M8):

| From | To | Action | Condition |
|---|---|---|---|
| Submitted | UnderReview | MarkUnderReview | always allowed |
| Submitted / UnderReview | ActionTaken | TakeAction | reason ≥ 10 |
| Submitted / UnderReview | Dismissed | Dismiss | reason ≥ 10 |
| ActionTaken / Dismissed | Closed | Close | final |

Member (via M10/Foundation):

| From | To | Action | Condition |
|---|---|---|---|
| Active | Restricted | Restriction | reason ≥ 10 (BR-21.6) |
| Restricted | Active | Lift / expiry | reason or end date |
| Active / Restricted | TemporarilyBlocked | Temporary Block | future end date |
| TemporarilyBlocked | Active | Lift / expiry | reason or end date |
| Active / Restricted / TemporarilyBlocked | PermanentlyBlocked | Permanent Block | final (BR-21.8) |

No other member status transition is available in M12; Inactive/Active is M10's.

---

## 10. Validation matrix

| ID | Rule | Where | Error |
|---|---|---|---|
| A-54 / V-45 | Every action/dismiss/lift reason ≥ 10 characters | Action, Dismiss, Lift | `REASON_REQUIRED` |
| — | Reason ≤ 500 characters | Action, Dismiss, Lift | `REASON_TOO_LONG` |
| BR-21.7 | Temporary Block needs a strictly future end date | Action | `END_DATE_REQUIRED` / `END_DATE_INVALID` |
| BR-21.6 | Restriction end date, when present, must be future | Action | `END_DATE_INVALID` |
| — | Warning/Permanent Block must not carry an end date | Action | `END_DATE_NOT_ALLOWED` |
| BR-21.8 / AC-21.6 | Permanent Block cannot be lifted | Lift | `PERMANENT_BLOCK_FINAL` |
| BR-21.6/21.7 | Lift only an Active Restriction/Temporary Block | Lift | `SANCTION_NOT_ACTIVE` / `SANCTION_NOT_LIFTABLE` |
| §9.7 | Only legal report transitions | All actions | `REPORT_STATUS_INVALID` |
| BR-21.1/17.10 | Reports never deleted | Whole module | no delete endpoint |
| BR-21.16 | No Admin-created report | Whole module | no create endpoint |
| BR-21.3/4.7 | Chat only from an open report | Conversation | `REPORT_NOT_OPEN` |
| BR-21.15 | No functional repeat on a Blocked member | Action | `MEMBER_ALREADY_BLOCKED` |
| CFG-14 | Hide only when moderation is on | Hide | `MODERATION_DISABLED` |
| M7 | A hidden comment cannot be hidden again | Hide | `COMMENT_ALREADY_HIDDEN` |
| — | Route hold needs an Active route | Hold | `NO_ACTIVE_ROUTE` |
| — | Release needs an Under-Review route | Release | `ROUTE_NOT_HELD` |
| — | Action is a known value | Context/Action | `ACTION_INVALID` |
| — | Sort whitelisted | Lists | `SORT_INVALID` |
| — | Page size 1–100 | Lists | `PAGE_SIZE_INVALID` |
| — | `messageToMember` ≤ 500 | Action | `MESSAGE_TOO_LONG` |

No validation framework exists in the repo; implement and unit-test these in the application services.

---

## 11. Audit events

| Event | Trigger | Recorded |
|---|---|---|
| `REPORT_MARKED_UNDER_REVIEW` | Mark Under Review | report no., actor, UTC |
| `REPORT_CONVERSATION_VIEWED` | Every conversation open | report no., chat id., actor, UTC (AC-21.1) |
| `WARNING_ISSUED` | Warning | report no., sanction no., reason, actor |
| `RESTRICTION_APPLIED` | Restriction | report no., sanction no., end date, reason, actor |
| `TEMPORARY_BLOCK_APPLIED` | Temporary Block | report no., sanction no., end date, counts, reason, actor |
| `PERMANENT_BLOCK_APPLIED` | Permanent Block | report no., sanction no., counts, reason, actor |
| `REPORT_DISMISSED` | Dismiss | report no., reason, actor |
| `REPORT_CLOSED` | Close | report no., actor |
| `REPORT_ROUTE_HELD` / `REPORT_ROUTE_RELEASED` | Route hold/release | route no., reason, actor |
| `SANCTION_LIFTED` | Early lift | sanction no., reason, actor |
| `SANCTION_EXPIRED` | Expiry job | sanction no., system actor, UTC |
| `RATING_COMMENT_HIDDEN` | Hide comment | rating no., reason, actor (never comment text) |
| `SAFETY_EXPORTED` | Any export | type, filter summary, row count |

Every row includes actor, role, UTC timestamp, old/new and reason where applicable. Audit is append-only. **Never** write chat text, report descriptions or contact details into logs.

---

## 12. Config usage

| CFG | Use in M12 |
|---|---|
| CFG-15 | Flagged-member threshold (default 3 distinct reporters) shown on SCR-21.5; read live |
| CFG-32 | Overdue threshold in days (default 3) for the report queue highlight (BR-21.1) |
| CFG-14 | Rating comment moderation enabled; gates the Hide action |
| CFG-7 | Optional first-page size; lists otherwise fixed 25/100 |

Read config through the Foundation reader; no hard-coded defaults.

---

## 13. Error responses

Use `BadRequest(new { code, message })`, `StatusCode(409, new { code, message })`, or `NotFound(new { code, message })`. Never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `REPORT_NOT_FOUND` | 404 | "Report not found." |
| `REPORT_NOT_OPEN` | 409 | "The conversation can be opened only while the report is open." |
| `REPORT_STATUS_INVALID` | 409 | "This action is not available for the report's current status." |
| `REASON_REQUIRED` | 400 | "Enter a reason of at least 10 characters." |
| `REASON_TOO_LONG` | 400 | "Keep the reason under 500 characters." |
| `END_DATE_REQUIRED` | 400 | "Choose an end date for a temporary block." |
| `END_DATE_INVALID` | 400 | "The end date must be in the future." |
| `END_DATE_NOT_ALLOWED` | 400 | "This action does not take an end date." |
| `MESSAGE_TOO_LONG` | 400 | "Keep the message under 500 characters." |
| `ACTION_INVALID` | 400 | "Choose a valid action." |
| `MEMBER_ALREADY_BLOCKED` | 409 | "This member is already blocked. No further action is needed." |
| `PERMANENT_BLOCK_FINAL` | 409 | "A permanent block cannot be reversed in the application." |
| `SANCTION_NOT_ACTIVE` | 409 | "This restriction or block is not active." |
| `SANCTION_NOT_LIFTABLE` | 409 | "A warning cannot be lifted." |
| `NO_ACTIVE_ROUTE` | 409 | "This member has no active route to hold." |
| `ROUTE_NOT_HELD` | 409 | "This route is not under review." |
| `MODERATION_DISABLED` | 403 | "Comment moderation is turned off." |
| `COMMENT_ALREADY_HIDDEN` | 409 | "This comment is already hidden." |
| `SORT_INVALID` | 400 | "Choose a valid sort order." |
| `PAGE_SIZE_INVALID` | 400 | "Page size must be between 1 and 100." |
| `EXPORT_TOO_LARGE` | 400 | "The export is too large. Narrow the filters." |
| `SAFETY_ACTION_INCOMPLETE` | 500 | "The action did not finish. Review the applied steps and try again." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

Keep wording aligned with the UI plan's microcopy.

---

## 14. Privacy and security

- **Admin-only surface.** Every route checks the Admin role server-side; denial is logged (BR-4.2).
- **Reporter identity** appears only in the Admin report detail; it never appears in a notice, sanction, member DTO or export sent outside Admin (BR-21.10).
- **Chat content** is readable only from an open report, read-only, and every open is logged in both `raah_ChatAccessLog` (M6) and the audit trail (BR-21.3, AC-21.1).
- **Permanent Block is final.** No endpoint exists to reverse it (AC-21.6).
- **Reports are never deleted.** No delete path exists (BR-21.1, BR-17.10).
- **No Admin-created reports.** No create path exists (BR-21.16).
- **Reason everywhere.** Every action, dismiss and lift writes the reason to the append-only audit.
- **No silent restoration.** Lifting a block does not bring back routes, connections or chats (BR-21.7, BR-19.3).
- **Export safety.** CSV injection guard; audit each export; never include chat text.
- **Logging.** Never log chat bodies, report descriptions, reasons or contact details in application logs.

---

## 15. Build sequence (M12)

| Step | Work | Output |
|---|---|---|
| 1 | Enums, DTOs, query contracts | Compiles |
| 2 | `raah_MemberSanction` entity, configuration, migration (`member_Master` untouched) | Table applies |
| 3 | `IReportAdminGateway` real wiring from M8; extend detail with reporter action/dismissed counts (UD-API-10) | Queue/detail data |
| 4 | `IReportQueryService`: queue filters, overdue, summary, options | SCR-21.1 data |
| 5 | Report detail composition + `availableActions` | SCR-21.2 data |
| 6 | Conversation endpoint through M6 with access log + audit | AC-21.1 |
| 7 | `IReportActionService`: mark under review, dismiss, close | Report lifecycle |
| 8 | Take-action context builder with live effects and end-date rules | Dialog can render |
| 9 | Warning + Restriction orchestration | AC-21.3, SC-21.1/21.2 |
| 10 | Temporary + Permanent Block orchestration (closures, status, notifications) | AC-21.4, SC-21.3/21.4 |
| 11 | `ISanctionQueryService` + lift with guards | SCR-21.4, SC-21.8 |
| 12 | `MemberSanctionExpiryJob` + lazy status guard | AC-21.2 |
| 13 | `IFlaggedMemberQueryService` | SCR-21.5 |
| 14 | `IRatingModerationService` (M7 gateway) | SCR-21.6, AC-21.10 |
| 15 | Route hold/release from the report | SC-21.7 |
| 16 | Exports with audit and injection guard | RPT extracts |
| 17 | Controllers + Admin gate + error codes | Postman-ready |
| 18 | Audit coverage for every event | §11 |

Suggested folders:

```
Binsera.Core/Safety/Enums/MemberSanctionType.cs, SanctionStatus.cs, SafetyReportAction.cs,
                              ReportQueueSort.cs, SanctionListSort.cs, FlaggedListSort.cs
Binsera.Core/Safety/Models/Admin/*Dto.cs, *Query.cs, *Request.cs
Binsera.Core/Safety/Contracts/IReportQueryService.cs,
                              IReportActionService.cs,
                              ISanctionQueryService.cs,
                              ISanctionActionService.cs,
                              IFlaggedMemberQueryService.cs,
                              IRatingModerationService.cs,
                              IMemberSanctionReader.cs,
                              ISafetyExportService.cs
Binsera.Core/Safety/Contracts/Gateways/IRouteAdminGatewayRelease.cs   // release addition, if separate
Binsera.Core/Safety/Domain/MemberSanction.cs
Binsera.Application/Safety/Services/Admin/ReportQueryService.cs,
                                       ReportActionService.cs,
                                       SanctionQueryService.cs,
                                       SanctionActionService.cs,
                                       FlaggedMemberQueryService.cs,
                                       RatingModerationService.cs,
                                       SafetyExportService.cs
Binsera.Application/Safety/Services/Admin/Orchestration/SafetyActionOrchestrator.cs
Binsera.Application/Safety/Services/Admin/MemberSanctionExpiryService.cs
Binsera.API/Controllers/Admin/SafetyReportsController.cs,
                         SafetySanctionsController.cs,
                         SafetyFlaggedController.cs,
                         SafetyRatingModerationController.cs
Binsera.API/BackgroundServices/MemberSanctionExpiryJob.cs
Binsera.Persistence/Configurations/Raah/MemberSanctionConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahMemberSanction.cs
```

---

## 16. Test plan

### 16.1 Unit

- Queue: each status/reason/overdue/flagged/member-status filter; oldest-first default; `OverdueFirst`; age boundaries at CFG-32−1 / CFG-32 / 2×CFG-32; paging.
- Detail: reporter history counts (raised/dismissed/action-taken/open); reported history (warnings, blocks, open reports, status, route, rating); related connection and other open reports; `availableActions` per report status.
- Conversation: open report returns messages; non-open report returns `REPORT_NOT_OPEN` and no content; no chat returns `canRead:false`; access log written for each open.
- Context: effect text per action from live counts; end-date required/optional/allowed; double-confirm only for Permanent Block; reason/message limits.
- Warning: sanction `Recorded`; status unchanged; reporter/member notices; audit.
- Restriction: status `Restricted`; optional/future end date; no other effect; request sending refused downstream (AC-21.3).
- Temporary Block: mandatory future end date; all closures in order; status `TemporarilyBlocked`; end-date-in-past rejected; lifting does not restore.
- Permanent Block: no end date; double-confirm flag; lift returns `PERMANENT_BLOCK_FINAL`; no reversal path exists (AC-21.6).
- Dismiss: reason required; status `Dismissed`; no member notice; reporter generic notice.
- Close: only from ActionTaken/Dismissed.
- Lift: restriction/temporary block → Active; reason required; permanent/warning rejected; closures not restored.
- Expiry job: due rows expire, status returns Active, notification sent, audit written, idempotent on re-run; a newer permanent block is not overwritten.
- Flagged: threshold boundary CFG-15−1 / CFG-15; distinct reporters count once; dismissed reports excluded; reasons breakdown.
- Rating moderation: CFG-14 off → `MODERATION_DISABLED`; hide sets Hidden and reason; stars/count unchanged; double hide → `COMMENT_ALREADY_HIDDEN`.
- Route hold/release: no active route → `NO_ACTIVE_ROUTE`; not held → `ROUTE_NOT_HELD`; reason required.
- Exports: columns, no chat text, audit, injection guard, row cap.

### 16.2 Integration / API

- Missing token → 401; member token → 403; Admin token → 200.
- Envelope shape matches DTOs; export is a `FileResult` unaffected by the response envelope.
- Temporary Block integration: route Inactive, pending requests Withdrawn, connections Disconnected, chats Closed, leads Withdrawn, member TemporarilyBlocked, sessions revoked.
- Expiry integration: after the end date passes, status Active and the member can log in; route still Inactive.
- Restriction integration: a blocked request send is refused with the correct error; login/chat/accept still work.
- Permanent Block: login refused; re-registration with the same email refused (BR-5.3); no lift endpoint works.
- Conversation: access works only while the report is open; `raah_ChatAccessLog` grows on each open; audit row per open.
- Reason < 10 characters rejected for action/dismiss/lift.
- Report and sanction rows are never deleted.
- Audit row per lifecycle change with reason/actor/old-new.
- Migration does not touch `member_Master`.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-21.1 Conversation access logged | §7.3, §11, §16.2 |
| AC-21.2 Temporary Block lifts itself | §8.6, §16.1, §16.2 |
| AC-21.3 Restriction scope | §8.2, §16.1 |
| AC-21.4 Block effects | §8.3, §16.2 |
| AC-21.5 Reporter hidden | §7.6, §8.1–8.4, §14 |
| AC-21.6 Permanent Block final | §7.11, §8.4, §14, §16.1 |
| AC-21.7 Reason required | §7.6, §10, §16.1 |

SC-21.1…SC-21.11 map to §7 endpoints and §8 orchestration; SC-21.6 (three reporters) is §7.12 and the M8 escalation read.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| UD-API-1 | Does M12 own a sanction table? | Yes — `raah_MemberSanction` for end dates, lift history and warning records; reports stay in M8 |
| UD-API-2 | Can Admin take action directly from `Submitted` without Under Review? | Yes; Under Review remains optional (BR-21.2). Record Under Review internally if needed |
| UD-API-3 | Who releases a route held Under Review? | M12 exposes `release-route`; add `ReleaseUnderReviewAsync` to M2/M11 |
| UD-API-4 | Is the route hold part of "Take action"? | No; separate `hold-route` / `release-route` endpoints |
| UD-API-5 | Show sanctions past their end date before the job runs? | Show `Expired`/due and disable lift; job completes the status change |
| UD-API-6 | How is a superseded active sanction handled? | Mark it `Lifted`/`Expired` in the same transaction as the new action |
| UD-API-7 | Can a Warning be issued to an already Permanent-Blocked member? | Allow Dismiss/Close only; block further functional action (`MEMBER_ALREADY_BLOCKED`) unless product says otherwise |
| UD-API-8 | Does Close need a reason? | Optional; audit records the actor and time |
| UD-API-9 | Overdue severity levels | Amber at CFG-32, Red at 2×CFG-32 |
| UD-API-10 | Extend M8 detail DTO with `dismissedCount`/`actionTakenCount`? | Yes; small read addition, no endpoint change |
| UD-API-11 | Notification channel on lift | In-app for restriction; email for temporary block; follow the M9 matrix |
| UD-API-12 | Does hiding a rating comment close its report? | No; Admin dismisses/closes the report separately |
| UD-API-13 | Include report description in RPT-17 export? | Yes for Admin export; never chat text |
| UD-API-14 | Expiry job cadence | Hourly, config-driven; plus lazy status guard on read |
| UD-API-15 | Sanction numbering | `SAN-{nnnnnn}` via Foundation generator; if product prefers report number only, drop it |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Purpose / Admin only | §1, §9 |
| SCR-21.1 Report Queue | §5.1, §6.1, §7.1 |
| SCR-21.2 Report Detail | §5.2, §6.1, §7.2–7.3 |
| SCR-21.3 Take Action | §5.3, §6.2, §7.4–7.8 |
| SCR-21.4 Restricted & Blocked | §5.4, §6.3, §7.10–7.11 |
| SCR-21.5 Flagged Members | §5.6, §6.3, §7.12 |
| SCR-21.6 Rating Moderation | §5.7, §6.3, §7.13 |
| Report Detail blocks | §5.2, §7.2 |
| Take Action fields | §5.3, §7.4–7.6 |
| What each action does | §8.1–8.5, §5.3 effects |
| BR-21.1 never delete / oldest first / overdue | §1, §7.1, §7.7 |
| BR-21.2 Under Review optional | §7.2, §7.5, UD-API-2 |
| BR-21.3 conversation logged | §7.3, §11, AC-21.1 |
| BR-21.4 proportionate | §7.2, §7.4 |
| BR-21.5 reason + audit + notices | §7.6, §8, §11 |
| BR-21.6 restriction scope/lift | §8.2, §7.11, AC-21.3 |
| BR-21.7 temporary block lift/closure | §8.3, §8.6, §7.11 |
| BR-21.8 permanent final | §8.4, §7.11, AC-21.6 |
| BR-21.9 block effects | §8.3, §8.4 |
| BR-21.10 reporter hidden | §7.2, §7.6, §14 |
| BR-21.11 flagged members | §7.12, CFG-15 |
| BR-21.12 dismiss reason + history | §7.2, §7.7 |
| BR-21.13 rating moderation | §7.13 |
| BR-21.14 photo/gender from report | §1, §7.2 (links to M10; no duplicate endpoint) |
| BR-21.15 already Inactive/Blocked | §7.2, §7.6, UD-API-7 |
| BR-21.16 no Admin report | §1, §6 notes |
| Status lifecycle | §4.4, §9.1 |
| Data Impact | §8, §11 |
| Reports RPT-16…22 | §1, §7.14 |
| CFG-14/15/32 | §12 |
| V-45/V-48 | §10 |
| A-46/A-50/A-53/A-54 | §1, §7.6, §7.11, §8 |
| SC-21.1…SC-21.11 | §7, §8, §16.3 |
| AC-21.1…AC-21.7 | §16.3 |
| Dependencies M8/M10/M11 | §2, §9.1, §17 |
