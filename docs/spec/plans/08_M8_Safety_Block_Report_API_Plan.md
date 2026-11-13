# M8 — Safety: Block & Report (Member Side) — API Plan

| | |
|---|---|
| **Module** | M8 — Safety: Block & Report (spec `docs/spec/08_M8_Safety_Block_Report.md`, functional spec Section 17) |
| **Document type** | API implementation plan: contracts, data model, endpoints, block/report lifecycle, eligibility, validation, audit, escalation, reporting hooks, sequencing, tests |
| **Build order** | #8 after Foundation + M1…M7 (see `00_Execution_Plan.md`); Admin handling is M12 (build order later) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M8 gives every member two always-available safety tools — **Block** (stop contact immediately, both directions) and **Report** (ask Admin to review a person) — plus a **Blocked members** list with Unblock and a **My reports** list with status. Admin handling of reports is M12 and is only **made possible** by the data and gateways this module owns.

1. **Block a member** — from a lead, a connection or a chat; immediate and mutual, with no reason asked (SCR-17.1, BR-17.1, BR-17.2, Section 17.4).
2. **Apply block effects atomically** — withdraw both members' leads, withdraw pending requests in both directions, set an Accepted connection to Blocked and close the chat; keep ratings (Section 17.4, AC-17.1).
3. **Blocked members list + Unblock** — the blocker sees only the members they blocked; Unblock re-includes the pair in matching but never restores a closed connection or chat (SCR-17.2, BR-17.3, SC-17.3).
4. **Report a member** — reason (fixed list), optional description (500 max, required for "Other"), optional "also block", a chat attached with the reporter's knowledge (SCR-17.3, V-43, V-44, BR-17.5).
5. **Report does not act on its own** — no automatic block or restriction; only "also block" changes anything immediately (BR-17.6, AC-17.3).
6. **My reports** — the reporter sees `Submitted / Under Review / Action Taken / Dismissed / Closed`, never which action was applied to the other member (SCR-17.4, BR-17.8).
7. **Escalation** — when CFG-15 distinct members report the same member, flag it for Admin and raise an alert once; the Admin dashboard and queue read this in M12 (BR-17.9, AC-17.5).
8. **Data and gateways for other modules** — real implementations of the block/report seams that M3, M4, M5, M6 and M7 consume, an Admin report gateway for M12, and report data for M15.
9. **Rating-comment report intake** — accept the hand-off from M7 (BR-16.9, BR-17.12) and keep it in the same report store.
10. **Reporting hooks** — RPT-17 Safety Report Register and RPT-22 Member Block Summary for M15 (spec Section 24).

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Admin report queue, detail, action dialog, restriction/block execution, flagged-members screen, rating moderation, overdue styling | M12 |
| Member status changes (Warning/Restriction/Temporary/Permanent Block) and their effect on login/matching | M10 |
| Lead creation/withdrawal by matching, `WithdrawPairAsync`, pair re-match on unblock | M3 |
| Connection lifecycle (Pending → Withdrawn, Accepted → Blocked) and connection `EndReason` | M5 |
| Chat lifecycle (close a chat) and Admin chat content access logging | M6 |
| Rating records, comment moderation and the "Report comment" entry | M7 |
| Notification centre and email wording | M9 |
| Configuration editing for CFG-15/CFG-32 | M13 |
| RPT-17/RPT-22 rendering | M15 |

M8 **never** removes a lead row, edits a connection, or deletes a chat. It asks M3/M5/M6 to perform those effects and only stores its own Block and Safety Report records.

---

## 2. Dependency and module-boundary handling

M8 is built after M1…M7. It consumes the seams those modules already expose and provides the real implementations of the safety seams they currently stub.

### 2.1 Contracts consumed by M8

| Contract | Owner | Purpose |
|---|---|---|
| `IPublicMemberProfileService.GetAsync(memberId, viewerMemberId)` | M1 | Limited public card for the other member (no email/mobile — BR-4.9, D-21) |
| `IMemberStatusReader.GetAsync(memberId)` | Foundation | Active/Restricted/Inactive/Blocked guard for the acting member |
| `IAppConfigReader` | Foundation | CFG-15, CFG-32 read live; CFG-7 for list size (optional) |
| `IRecordNumberGenerator.NextAsync("SAF")` | Foundation | `SAF-{nnnnnn}` numbering (BR-7.1/7.2, no yearly reset) |
| `IAuditService` | Foundation | All M8 audit events (§11) |
| `ILeadWriter.WithdrawPairAsync(memberA, memberB)` | M3 | Withdraw both directions of the pair's leads on block (BR-12.9, Section 17.4) |
| `IMatchingEngine.RunForPairAsync(memberA, memberB)` | M3 | Re-evaluate the pair after unblock (may revive leads — BR-17.3, SC-17.3) |
| `IConnectionLifecycleGateway.EndForBlockAsync(memberA, memberB)` | M5 | Withdraw Pending in both directions, set Accepted to Blocked and close the chat (BR-14.12, AC-17.1) |
| `IChatReadGateway.GetLatestChatIdForPairAsync(memberA, memberB)` | M6 | Find the conversation to attach to a report, if one exists (Section 17.5, BR-17.5) |
| `IRatingReadGateway.GetForReportAsync(ratingId, reporterMemberId)` | M7 | Resolve a reported rating comment for the M7 hand-off (BR-16.9, BR-17.12) |
| `INotificationGateway` | M9-owned, M8 consumes (no-op until M9) | Report-decided notice to the reporter; never a block notice (§8.6) |
| `IAdminAlertGateway` | M12-owned, M8 consumes (no-op until M12) | New report / escalation alert to Admin (BR-17.9) |

Small **read additions** required on existing modules (no endpoint changes):

| Addition | Owner | Purpose |
|---|---|---|
| `ILeadRelationReader.HasAnyLeadBetweenAsync(a, b)` | M4 | BR-17.4/V-44 eligibility (lead exists, current or past) |
| `IConnectionRelationReader.HasAnyConnectionBetweenAsync(a, b)` | M5 | BR-17.4/V-44 eligibility (current or past connection) |
| `IChatRelationReader.HasAnyChatBetweenAsync(a, b)` / `GetLatestChatIdForPairAsync` | M6 | BR-17.4 eligibility and report conversation attachment |

`IBlockPolicyGateway`, `IBlockGateway` and `IInteractionPolicyGateway` are **M8-owned interfaces**; until now the other modules registered no-op implementations. M8 replaces every no-op with one real implementation (§8.1–8.3) — exactly one registration each.

### 2.2 Contracts provided by M8 (consumed later)

| Contract | Purpose | Consumer |
|---|---|---|
| `IBlockPolicyGateway` (real) | `IsBlockedPairAsync(a,b)`, `CanReceiveRequestAsync(target, from)` | M3, M5, M6, M7 |
| `IBlockGateway` (real, M3 interface) | `IsBlockedPairAsync(a,b)`, `GetBlockedMemberIdsAsync(memberId)` | M3 matching exclusion |
| `IInteractionPolicyGateway` (real, M4 interface) | `CanBlockAsync(viewer, target)`, `CanReportAsync(viewer, target)`, `IsBlockedPairAsync(a,b)` | M4 lead actions, M5/M6 action flags |
| `IReportIntakeGateway` (real, M7 interface) | `CreateRatingCommentReportAsync(ratingId, reporterMemberId, reasonCode, details)` | M7 "Report comment" |
| `IReportAccessPolicyGateway` | `IsReportOpenForAsync(reportId, chatId, connectionId)` | M6 `IChatAdminAccessGateway` (open-report check) |
| `IReportAdminGateway` | Queue, detail, mark-under-review, record action, dismiss, close, flagged members | M12 |
| `ISafetyReportDataSource` | RPT-17 aggregates/rows | M15 |
| `IBlockReportDataSource` | RPT-22 aggregates/rows | M15 |

Rules:

- Each no-op M3/M4/M5/M6/M7 ships today is safe by default (`isBlocked = false`, `canBlock/canReport = false`, report intake unavailable). When M8 lands it registers the real implementation; **no consumer endpoint contract changes**.
- M8 stores **no** lead, connection or chat state. It owns only `raah_Block` and `raah_SafetyReport`.
- All block effects run in **one** `IUnitOfWork` transaction together with the block row. A failure in any effect rolls the block back, so the members are never half-blocked.
- A report stores the conversation **by reference** (`ChatId`/`ConnectionId`); M6 keeps messages and authorization/logging. M8 never copies chat text, so there is nothing to leak from M8 tables.
- Admin actions (restriction/block) are executed by M12 through M10. M8 only records the report lifecycle and the decision reference; it does not change member status.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; route `safety`; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interfaces in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` → `ResponseFilterAttribute` envelope `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | `BadRequest(new { code, message })` / object-result `StatusCode(403/404/409, new { code, message })`; **never** `Forbid()` / `Challenge()` |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new EF-managed tables; `member_Master` untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id`, decimal precision, audit columns, `IsDeleted`, per-column indexes |
| Validation | No FluentValidation; validate in `BlockService` / `ReportService` / `ReportQueryService` |
| Current user | Derive the member id from the token; never accept an actor member id in the body |
| Background job | None required. Escalation is computed at submit time; no scheduler (optional repair job only if product wants it) |

---

## 4. Data model

### 4.1 New table `raah_Block`

One row per block action, kept forever as history. A member has at most one **active** block per blocked member; unblocking ends the active row and the pair can be blocked again later as a new row (BR-17.3, A-47).

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `BlockerMemberId` | bigint | FK → `member_Master.Id`; the member who blocked (from token) |
| `BlockedMemberId` | bigint | FK → `member_Master.Id`; the member blocked |
| `Source` | tinyint | `BlockSource` (§4.4): `Lead = 1`, `Connection = 2`, `Chat = 3`, `Report = 4` (from "also block") |
| `SourceRefId` | bigint null | Optional `leadId` / `connectionId` / `chatId` / `reportId` context, for audit only |
| `BlockedOnUtc` | datetime2 | Immediate |
| `IsActive` | bit | True while the block stands; false after Unblock |
| `UnblockedOnUtc` | datetime2 null | Set by Unblock |
| `UnblockedByMemberId` | bigint null | Always the original blocker |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false (block history is retained) |

Indexes:

- **Filtered unique `(BlockerMemberId, BlockedMemberId)` where `IsActive = 1`** — one active block per ordered pair (the final guard against a double-tap race).
- `(BlockedMemberId, IsActive)` — "who blocked me" checks for `IsBlockedPairAsync` and matching exclusion.
- `(BlockerMemberId, BlockedOnUtc DESC)` — blocked-members list.
- `(IsActive, UnblockedOnUtc DESC)` — RPT-22.

Blocking is **directional** in storage but **mutual** in effect: the pair is blocked if either direction has an active row. The blocker's list shows only rows where they are `BlockerMemberId`. The blocked member never sees the row (BR-17.2).

### 4.2 New table `raah_SafetyReport`

One row per report. Never deleted; Dismissed and Closed rows stay for Admin (BR-17.10).

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `ReportNumber` | nvarchar(20) | `SAF-000009`, generated on submit, never reused (BR-7.1/7.2) |
| `ReporterMemberId` | bigint | FK → `member_Master.Id` (from token) |
| `ReportedMemberId` | bigint | FK → `member_Master.Id` |
| `Reason` | tinyint | `ReportReason` (§4.4) |
| `Description` | nvarchar(500) null | Trimmed; required when Reason = `Other` (V-43). Stored verbatim; never logged |
| `TargetType` | tinyint | `ReportTargetType`: `Member = 1`, `Lead = 2`, `Connection = 3`, `Chat = 4`, `RatingComment = 5` |
| `ConnectionId` | bigint null | FK → M5 connection, when the report concerns a connection/chat |
| `ChatId` | bigint null | FK → M6 chat, when a conversation is attached (BR-17.5) |
| `RatingId` | bigint null | FK → M7 rating, for a rating-comment report (BR-16.9) |
| `ConversationAttached` | bit | True when `ChatId` is set; shown to the reporter with the access notice |
| `AlsoBlockRequested` | bit | The reporter ticked "Also block" (BR-17.6) |
| `AlsoBlockApplied` | bit | True when the block effect succeeded in the same transaction |
| `Status` | tinyint | `SafetyReportStatus` (§4.4): `Submitted = 1`, `UnderReview = 2`, `ActionTaken = 3`, `Dismissed = 4`, `Closed = 5` |
| `SubmittedOnUtc` | datetime2 | |
| `UnderReviewOnUtc` | datetime2 null | Admin transition |
| `UnderReviewByAdminId` | bigint null | |
| `DecidedOnUtc` | datetime2 null | ActionTaken or Dismissed |
| `DecidedByAdminId` | bigint null | |
| `DecisionAction` | tinyint null | `ReportDecisionAction`: `Warning = 1`, `Restriction = 2`, `TemporaryBlock = 3`, `PermanentBlock = 4` — **Admin-only**, never shown to the reporter/member |
| `DecisionReason` | nvarchar(500) null | Admin reason (V-45 pattern; ≥ 10 chars) |
| `ClosedOnUtc` | datetime2 null | |
| `ClosedByAdminId` | bigint null | |
| `EscalatedOnUtc` | datetime2 null | Set on the report that crossed CFG-15; drives the flag and one alert (BR-17.9) |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false (BR-17.10) |

Indexes:

- Unique `ReportNumber`.
- `(ReportedMemberId, SubmittedOnUtc DESC)` — escalation count and action history.
- `(ReportedMemberId, Status)` — distinct-reporter count and flags.
- `(ReporterMemberId, SubmittedOnUtc DESC)` — My Reports.
- `(Status, SubmittedOnUtc ASC)` — Admin queue oldest-first and CFG-32 overdue.
- `(Reason)` — queue filter.
- Filtered `(EscalatedOnUtc)` where `EscalatedOnUtc IS NOT NULL` — flagged-members read.

### 4.3 Where conversation evidence lives

M8 stores **only** `ChatId`/`ConnectionId`. Messages, retention and Admin access logging stay in M6 (`raah_ChatAccessLog`). A closed chat is still readable by its two members, so the report keeps a valid reference after the connection ends. There is no M8 chat copy — this keeps BR-4.7 / AC-15.5 intact (Admin reads chat only through the open-report gateway and every access is logged by M6).

### 4.4 Enums

```
BlockSource         { Lead = 1, Connection = 2, Chat = 3, Report = 4 }
ReportReason        { Harassment = 1, InappropriateMessages = 2, FakeProfile = 3,
                      SafetyConcern = 4, Spam = 5, Other = 6 }
ReportTargetType    { Member = 1, Lead = 2, Connection = 3, Chat = 4, RatingComment = 5 }
SafetyReportStatus  { Submitted = 1, UnderReview = 2, ActionTaken = 3, Dismissed = 4, Closed = 5 }
ReportDecisionAction{ Warning = 1, Restriction = 2, TemporaryBlock = 3, PermanentBlock = 4 }
```

Migration (indicative): `20260108000000_AddRaahBlockAndSafetyReport`.

---

## 5. DTOs (Binsera.Core)

Namespace: `Binsera.Core.Safety.Models.Raah`.

### 5.1 Block confirmation context (SCR-17.1)

`BlockContextDto`:
```
targetMember        PublicMemberCardDto   // M1 limited card; no email/mobile
alreadyBlocked      bool
canBlock            bool
disabledReasonCode  string?               // "ALREADY_BLOCKED" | "NO_CONTACT" | "SELF" | "TARGET_UNAVAILABLE"
disabledReasonMessage string?
source              string                // "lead" | "connection" | "chat"
effects             string[]              // ordered consequence lines for the sheet (Section 17.4)
```

### 5.2 Block result

`BlockResultDto`:
```
blockId              long
member               PublicMemberCardDto
blockedOn            DateTime
effectsApplied       BlockEffectsDto       // { leadsWithdrawn, pendingRequestsWithdrawn, connectionBlocked, chatClosed }
message              string                // "Blocked. You will no longer see each other. They are not told."
```

`BlockEffectsDto` → `{ leadsWithdrawn bool, pendingRequestsWithdrawn bool, connectionBlocked bool, chatClosed bool }`.

### 5.3 Blocked members list (SCR-17.2)

`BlockedMemberListItemDto`:
```
blockId        long
member         PublicMemberCardDto
blockedOn      DateTime
canUnblock     bool
```

`BlockedMemberListResponseDto` → `{ items, hasMore, nextCursor }`.

`UnblockResultDto` → `{ blockId, unblockedOn, message }`.

### 5.4 Report context (SCR-17.3)

`ReportContextDto`:
```
targetMember            PublicMemberCardDto
canReport               bool
disabledReasonCode      string?            // "NO_CONTACT" | "SELF" | "TARGET_UNAVAILABLE"
disabledReasonMessage   string?
reasons                 ReportReasonOptionDto[]
descriptionMaxLength    int                // 500
hasAttachedConversation bool               // a chat exists with the target
conversationAttachedLabel string           // "Your chat with {name} will be attached."
chatAccessNotice        string             // "Admin can read your chat with {name} to review this report."
emergencyNote           string             // fixed emergency copy
alreadyBlocked          bool
canAlsoBlock            bool
source                  string             // "lead" | "connection" | "chat" | "rating_comment"
```

`ReportReasonOptionDto` → `{ code string, label string, requiresDescription bool }` (labels from A-46; only `Other` has `requiresDescription = true`).

### 5.5 Submit report

`SubmitReportRequest`:
```
targetMemberId   long
reasonCode       string          // one of the six codes
description      string?         // trimmed; required when reasonCode = "Other"; max 500
alsoBlock        bool            // default false
source           string          // "lead" | "connection" | "chat" | "rating_comment"
sourceRefId      long?           // leadId / connectionId / chatId / ratingId, optional context
```

`ReportSubmittedDto`:
```
reportId        long
reportNumber    string          // SAF-nnnnnn
status          string          // "Submitted"
statusLabel     string
alsoBlocked     bool
submittedOn     DateTime
message         string          // "Report sent. We will review it."
```

### 5.6 My reports (SCR-17.4)

`MyReportListItemDto`:
```
reportId        long
reportNumber    string
reportedMember  PublicMemberCardDto
reasonLabel     string
submittedOn     DateTime
status          string            // Submitted | UnderReview | ActionTaken | Dismissed | Closed
statusLabel     string            // Submitted | Under Review | Action Taken | Dismissed | Closed
isDecided       bool
decisionSummary string?           // "Your report has been reviewed." — never the action
```

`MyReportListResponseDto` → `{ items, hasMore, nextCursor }`.

`MyReportDetailDto` → list fields plus:
```
description            string?
conversationAttached   bool
timeline               ReportTimelineItemDto[]   // { statusLabel, on, note? } — no decision action
```

`ReportTimelineItemDto` → `{ statusLabel string, on DateTime, note string? }`. The decided step note is always "Your report has been reviewed." (BR-17.8).

### 5.7 Admin DTOs (consumed by M12)

| DTO | Fields |
|---|---|
| `ReportQueueItemDto` | `reportId`, `reportNumber`, `ageDays`, `isOverdue` (CFG-32), `reasonLabel`, `reporter PublicMemberCardDto`, `reportedMember PublicMemberCardDto`, `status`, `statusLabel`, `reportCountAgainstMember`, `distinctReporterCount`, `isEscalated`, `reportedMemberRestricted`, `submittedOn` |
| `ReportDetailDto` | queue fields plus `description`, `conversationAttached`, `chatId?`, `connectionId?`, `ratingId?`, `targetType`, `timeline`, `reporterHistory` (`reportsRaised`, `unfoundedCount`), `reportedMemberHistory` (`warnings`, `openReports`, `currentStatus`, `activeBlockers`), `decisionAction?`, `decisionReason?` |
| `MarkUnderReviewRequest` | (empty body) |
| `RecordReportActionRequest` | `action` (`Warning\|Restriction\|TemporaryBlock\|PermanentBlock`), `reason` (≥ 10 chars) |
| `DismissReportRequest` | `reason` (≥ 10 chars) |
| `ReportStatusChangeResultDto` | `reportId`, `reportNumber`, `status`, `statusLabel`, `changedOn` |
| `FlaggedMemberDto` | `member PublicMemberCardDto`, `distinctReporterCount`, `reportCount`, `latestReportOn`, `isEscalated` |

Admin DTOs are the **only** place the reporter's identity is exposed; they are returned only by M12's Admin endpoints (BR-21.10). Member DTOs never carry `DecisionAction`, `DecisionReason`, the reporter identity of another person's report, or any block information about who blocked whom.

---

## 6. Endpoint catalogue

Base route: `safety`. Every endpoint requires an authenticated **Member** token and acts only on the member's own records.

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/safety/block-context/{memberId}?source=&sourceRefId=` | Block confirmation context and effects | SCR-17.1 | 200 `BlockContextDto` |
| 2 | POST | `/safety/blocks` | Block a member immediately | SCR-17.1 | 200 `BlockResultDto` |
| 3 | GET | `/safety/blocked?limit=&cursor=` | Members the member blocked | SCR-17.2 | 200 `BlockedMemberListResponseDto` |
| 4 | POST | `/safety/blocks/{blockId}/unblock` | Unblock | SCR-17.2 | 200 `UnblockResultDto` |
| 5 | GET | `/safety/report-context/{memberId}?source=&sourceRefId=` | Report form context: reasons, notices, attachment | SCR-17.3 | 200 `ReportContextDto` |
| 6 | POST | `/safety/reports` | Submit a report (optional also-block) | SCR-17.3 | 200 `ReportSubmittedDto` |
| 7 | GET | `/safety/reports?limit=&cursor=` | My Reports | SCR-17.4 | 200 `MyReportListResponseDto` |
| 8 | GET | `/safety/reports/{reportId}` | My Report detail and timeline | SCR-17.4 | 200 `MyReportDetailDto` |

Admin surface (owned by M12, backed by `IReportAdminGateway` — not part of the member API):

| # | Method | Route | Purpose |
|---|---|---|---|
| A1 | GET | `/admin/safety/reports?status=&reason=&age=&flagged=&limit=&cursor=` | Report queue (SCR-21.1) |
| A2 | GET | `/admin/safety/reports/{reportId}` | Report detail (SCR-21.2) |
| A3 | POST | `/admin/safety/reports/{reportId}/mark-under-review` | Submitted → Under Review |
| A4 | POST | `/admin/safety/reports/{reportId}/action` | Under Review → Action Taken (records the decision) |
| A5 | POST | `/admin/safety/reports/{reportId}/dismiss` | Under Review → Dismissed (reason) |
| A6 | POST | `/admin/safety/reports/{reportId}/close` | Action Taken/Dismissed → Closed |
| A7 | GET | `/admin/safety/flagged?limit=&cursor=` | Flagged members at CFG-15 (SCR-21.5) |

**Identifiers.** Routes use numeric `memberId` / `blockId` / `reportId`; DTOs expose the user-facing `reportNumber` (SAF-). Internal ids never appear in the member UI.

Forward-looking, not part of the M8 member API:

- Admin chat content access stays `IChatAdminAccessGateway.OpenForReportAsync` in M6 (SCR-21.2); M8 only answers `IsReportOpenForAsync`.
- Member restriction/block execution is M10/M12; M8 only records the report decision.
- "Report comment" is M7's route (`POST /ratings/{ratingId}/report-comment`), which calls M8's `IReportIntakeGateway`.

---

## 7. Endpoint details

### 7.1 `GET /safety/block-context/{memberId}`

1. Resolve `meMemberId` from the token.
2. Load the target through M1's public card; missing/inactive → `404 MEMBER_NOT_AVAILABLE`.
3. `alreadyBlocked` = an active `raah_Block` `(me → target)` exists.
4. `canBlock` = `NOT alreadyBlocked AND me ≠ target AND me is Active/Restricted AND HasContact(me,target)`.
5. `HasContact` = a lead, current-or-past connection, or chat exists between the pair (M4/M5/M6 readers). Being on a lead/connection/chat screen implies this; a direct call without contact returns `canBlock = false`, `disabledReasonCode = "NO_CONTACT"` (open decision — §18).
6. `effects` is the fixed Section 17.4 list, ordered: "You will no longer see each other.", "Any pending request is withdrawn.", "Your chat closes. You can still read it.", "They are not told."
7. Reading the context is side-effect-free.

### 7.2 `POST /safety/blocks`

Request: `{ targetMemberId, source, sourceRefId? }` — never `blockerMemberId`.

1. Resolve `meMemberId` from the token.
2. **Not self** — `targetMemberId ≠ me`, else `400 CANNOT_BLOCK_SELF`.
3. **Target usable** — exists and is not the viewer; else `404 MEMBER_NOT_AVAILABLE`.
4. **Contact exists** — `HasContact(me, target)`; else `403 BLOCK_NOT_ALLOWED` (BR-17.1 entry points; V-44-style relation).
5. **Not already blocked** — no active `(me → target)` row; else `409 ALREADY_BLOCKED`. The filtered unique index is the race guard; catch the duplicate and return the same code.
6. In one `IUnitOfWork` transaction:
   1. Insert `raah_Block` (`IsActive = true`, `BlockedOnUtc = now`, `Source` from the request).
   2. `ILeadWriter.WithdrawPairAsync(me, target)` — withdraw both members' leads for the pair (BR-12.9).
   3. `IConnectionLifecycleGateway.EndForBlockAsync(me, target)` — withdraw Pending both directions, set Accepted to Blocked and **close the chat** through M6 (BR-14.12, AC-17.1).
   4. Do **not** call `INotificationGateway` for the blocked member (BR-17.2, AC-17.2). No block notification is sent to either party.
   5. Audit `BLOCK_APPLIED` (pair reference, source; **no reason/description**).
7. Return `BlockResultDto` with `effectsApplied` derived from what M3/M5 reported.
8. Ratings are untouched (Section 17.4).

If M3/M5 are absent (should not happen at build order #8), the effects no-op safely and the block still stands; the response marks the affected effects false. Never leave a block row committed without attempting the effects in the same unit of work.

### 7.3 `GET /safety/blocked`

1. Resolve `BlockerMemberId = me`.
2. Return only active rows `IsActive = true`, ordered `BlockedOnUtc DESC, Id DESC`, keyset paging (`limit` default 20, clamp 50).
3. `member` = blocked member's public card (no email/mobile). `canUnblock = true`.
4. Never include a row where the viewer is the **blocked** party (BR-17.2).

### 7.4 `POST /safety/blocks/{blockId}/unblock`

1. Load the block; it must exist, be `IsActive`, and `BlockerMemberId = me`; else `404 BLOCK_NOT_FOUND` (do not reveal another member's block).
2. In one transaction: `IsActive = false`, `UnblockedOnUtc = now`, `UnblockedByMemberId = me`; audit `BLOCK_REMOVED`.
3. After commit, call `IMatchingEngine.RunForPairAsync(me, target)` (may revive leads when the routes still match — BR-17.3, SC-17.3).
4. Do **not** restore a closed connection or chat; a new request is required (BR-17.3). Return `UnblockResultDto`.
5. If the reverse direction still has an active block (target blocked me independently), the pair remains blocked; the response message notes "You have unblocked {name}." and matching stays excluded — no detail about the other direction is revealed (open decision §18).

### 7.5 `GET /safety/report-context/{memberId}`

1. Resolve `meMemberId` from the token; target via M1 public card (`404 MEMBER_NOT_AVAILABLE` if missing).
2. `canReport` = `me ≠ target AND HasContact(me,target)` (BR-17.4, V-44). `HasContact` = lead (current/past) OR connection (current/past) OR chat.
3. `reasons` = the fixed six options (A-46); only `Other` has `requiresDescription = true`.
4. `descriptionMaxLength = 500` (V-43).
5. `hasAttachedConversation` = `IChatReadGateway.GetLatestChatIdForPairAsync(me,target)` returns a chat; fill `conversationAttachedLabel` and the fixed `chatAccessNotice` (BR-17.5).
6. `alreadyBlocked` / `canAlsoBlock` from the active-block check: `alsoBlock` is offered only when not already blocked.
7. `emergencyNote` = the fixed text: "RAAH does not provide emergency services. If you are in danger, contact local emergency services."
8. Side-effect-free.

### 7.6 `POST /safety/reports`

Request: `{ targetMemberId, reasonCode, description?, alsoBlock, source, sourceRefId? }` — never `reporterMemberId`.

Rules (BR-17.4–17.6, V-43, V-44, AC-17.3):

1. **Target usable** — exists, not self; else `400 CANNOT_REPORT_SELF` / `404 MEMBER_NOT_AVAILABLE`.
2. **Contact exists** — `HasContact(me, target)`, else `403 REPORT_TARGET_NOT_ALLOWED` (SC-17.6).
3. **Reason** — must be one of the six codes; else `400 REPORT_REASON_INVALID`.
4. **Description** — trim; empty → stored as `null`.
   - If `reasonCode = Other` and empty → `400 REPORT_DESCRIPTION_REQUIRED` (SC-17.5).
   - Length > 500 → `400 REPORT_DESCRIPTION_TOO_LONG`.
   - No contact-detail filter: the description is free text read only by Admin (unlike chat).
5. **Also block** — when true and not already blocked, apply the §7.2 block effects **inside the same transaction**; set `AlsoBlockRequested = true`, `AlsoBlockApplied = true`. When already blocked, set `AlsoBlockApplied = false` but still submit the report.
6. Determine `ChatId`/`ConnectionId` from `IChatReadGateway` when a chat exists; set `ConversationAttached`.
7. In one `IUnitOfWork` transaction:
   1. Generate `ReportNumber` (`SAF-`).
   2. Insert `raah_SafetyReport` with `Status = Submitted`, `SubmittedOnUtc = now`.
   3. If `alsoBlock` → run the block effects (steps in §7.2.6) and set the flags.
   4. Count **distinct reporters** against `ReportedMemberId` in the escalation window (default rolling 30 days) among reports whose status ≠ `Dismissed`. If `count >= CFG-15` → set `EscalatedOnUtc = now` on this report and call `IAdminAlertGateway.RaiseReportThresholdAlertAsync(target, count)` once (no-op until M12) (BR-17.9, AC-17.5).
   5. Call `IAdminAlertGateway.NewReportSubmittedAsync(reportId)` (no-op until M12) so the queue/dashboard can refresh.
   6. Audit `REPORT_SUBMITTED` (report number, reason code, target; **never the description**) and `REPORT_ESCALATED` when it crossed.
8. Return `ReportSubmittedDto`. A report **never** blocks or restricts anyone unless `alsoBlock` was ticked (BR-17.6, AC-17.3).

### 7.7 `GET /safety/reports`

1. `ReporterMemberId = me`; order `SubmittedOnUtc DESC, Id DESC`; keyset paging (default 20, clamp 50).
2. Map status → label (`Submitted`, `Under Review`, `Action Taken`, `Dismissed`, `Closed`).
3. `isDecided` = status ∈ {ActionTaken, Dismissed, Closed}; `decisionSummary` = "Your report has been reviewed." — **never** the action or reason (BR-17.8).
4. `reportedMember` = public card. No other member's data, no Admin notes.

### 7.8 `GET /safety/reports/{reportId}`

1. Load the report; it must be `ReporterMemberId = me`; else `404 REPORT_NOT_AVAILABLE` (and log the denied attempt).
2. Return the own description, conversation flag, and a timeline built from the status dates: Submitted → Under Review → Action Taken/Dismissed → Closed.
3. The decided step shows only "Your report has been reviewed." (BR-17.8). Never expose `DecisionAction`/`DecisionReason`, the reported member's status, or who else reported.
4. A report is never deleted; a Closed report still opens read-only (BR-17.10).

---

## 8. Lifecycle and gateways

### 8.1 `IBlockPolicyGateway` (real implementation)

```
IsBlockedPairAsync(a, b)      -> either direction has an active raah_Block row
CanReceiveRequestAsync(target, from) -> NOT IsBlockedPairAsync(target, from) AND target not a member-blocked pair
GetBlockedMemberIdsAsync(memberId)   -> ids blocked by OR blocking the member (both directions, active)
```

Backed by two indexed lookups. Used by M3 (matching exclusion), M5 (request eligibility), M6 (chat send/close) and M7 (rating refusal). Replaces every no-op registration.

### 8.2 `IBlockGateway` (M3 interface, real implementation)

`IsBlockedPairAsync` and `GetBlockedMemberIdsAsync` as above; registered in place of M3's no-op so the matching engine excludes blocked pairs and after unblock can revive leads.

### 8.3 `IInteractionPolicyGateway` (M4 interface, real implementation)

```
CanBlockAsync(viewer, target)  -> NOT self AND HasContact(viewer,target) AND NOT already blocked
CanReportAsync(viewer, target) -> NOT self AND HasContact(viewer,target)
IsBlockedPairAsync(a, b)       -> delegate to IBlockPolicyGateway
```

Feeds the `canBlock`/`canReport` flags on lead and connection/chat DTOs so the entry points appear exactly where the spec allows them (BR-17.4, UP-3).

### 8.4 `IReportIntakeGateway` (real implementation, M7 hand-off)

`CreateRatingCommentReportAsync(ratingId, reporterMemberId, reasonCode, details)`:

1. Resolve the rating through `IRatingReadGateway.GetForReportAsync`; it must belong to `reporterMemberId` (the rated member) and have a visible comment; else return a "not available" result → M7 maps to `404`/`409`.
2. Create `raah_SafetyReport` with `TargetType = RatingComment`, `RatingId`, `ReportedMemberId` = the rating's rater, `Description` = `details` (≤ 500) or derived, status `Submitted`.
3. Apply the same escalation count/alert logic as §7.6 step 7.4.
4. Audit `REPORT_SUBMITTED` (target type rating comment; no comment text).
5. Return `{ reportId, reportNumber }` so M7 can answer `POST /ratings/{ratingId}/report-comment`.

The rating moderation UI and hide action remain M7/M12; the report row is M8's.

### 8.5 Report status lifecycle (spec §9.7)

| From | To | Who | M8 mechanism |
|---|---|---|---|
| (none) | Submitted | Member | `POST /safety/reports` / `IReportIntakeGateway` |
| Submitted | Under Review | Admin | `IReportAdminGateway.MarkUnderReviewAsync` |
| Under Review | Action Taken | Admin | `IReportAdminGateway.RecordActionAsync` (M12 executes the member action via M10) |
| Under Review | Dismissed | Admin | `IReportAdminGateway.DismissAsync` (reason required) |
| Action Taken / Dismissed | Closed | Admin | `IReportAdminGateway.CloseAsync` |

Illegal transitions → `409 REPORT_STATUS_INVALID`. Transitions are idempotent when replayed to the same target state.

### 8.6 `IReportAdminGateway` (consumed by M12)

| Method | Behaviour |
|---|---|
| `GetQueueAsync(filter)` | Oldest-first queue with status/reason/age/flagged filters; computes `ageDays` and `isOverdue` (CFG-32); includes reporter/reported public cards, report counts, distinct reporters, escalation and restricted flags |
| `GetDetailAsync(reportId)` | Full report + timeline + reporter history + reported-member history (warnings, open reports, current status, active blockers). Admin-only reporter identity |
| `MarkUnderReviewAsync(reportId, adminId)` | Submitted → Under Review only |
| `RecordActionAsync(reportId, adminId, action, reason)` | Under Review → Action Taken; requires a reason ≥ 10 chars; stores `DecisionAction`/`DecisionReason`; calls `INotificationGateway.ReportDecidedAsync(reporterId, reportNumber)` (no-op until M9). Does **not** itself restrict or block — M12 calls M10 first |
| `DismissAsync(reportId, adminId, reason)` | Under Review → Dismissed; reason ≥ 10; notifies the reporter generically |
| `CloseAsync(reportId, adminId)` | Action Taken/Dismissed → Closed |
| `GetFlaggedMembersAsync(filter)` | Members with `distinctReporters >= CFG-15` (non-dismissed) and/or an escalated report; for SCR-21.5 |

Every Admin transition is audited (§11). Admin actions always carry a reason (V-45 pattern).

### 8.7 `IReportAccessPolicyGateway` (for M6)

`IsReportOpenForAsync(reportId, chatId, connectionId)` → true only when the report exists, `Status ∈ {Submitted, UnderReview}`, and its `ChatId`/`ConnectionId` matches. M6's `IChatAdminAccessGateway.OpenForReportAsync` calls this before returning chat content; M6 writes `raah_ChatAccessLog`. Without an open report there is no access (BR-4.7, AC-15.5).

### 8.8 Escalation (BR-17.9, AC-17.5)

- Trigger: on every report submit (member or rating-comment intake).
- Count: **distinct** `ReporterMemberId` for the target, reports with `Status ≠ Dismissed`, inside the escalation window (default rolling 30 days — open decision).
- When `count >= CFG-15` and this report is not already escalated: set `EscalatedOnUtc`, audit `REPORT_ESCALATED`, call `IAdminAlertGateway.RaiseReportThresholdAlertAsync(target, count)`.
- The same reporter filing several reports counts once.
- Flag is read live by `GetFlaggedMembersAsync` and `GetQueueAsync`; no background job.

### 8.9 Block effects ordering and failure

Within the block transaction the order is: insert block → withdraw leads (M3) → end connections/close chat (M5/M6). If any step throws, the whole unit rolls back and the API returns `500 SAFETY_ACTION_FAILED` (UI shows a retry). The members are never left "blocked in one place only". Leads/connection/chat writes are idempotent enough that a retry is safe; the filtered unique block index prevents a duplicate active row.

---

## 9. Validation matrix

| ID / rule | Rule | Where | Error |
|---|---|---|---|
| BR-17.1 | Block only from a lead/connection/chat; a contact relation must exist | Block context/submit | `403 BLOCK_NOT_ALLOWED` |
| BR-17.1 | Cannot block self | Block | `400 CANNOT_BLOCK_SELF` |
| BR-17.2 | Blocked member is never notified | Block | no notification call (test) |
| BR-17.3 | Unblock only the blocker's own active block; no connection restored | Unblock | `404 BLOCK_NOT_FOUND` |
| BR-17.4 / V-44 / SC-17.6 | Report only with a lead, current/past connection or chat | Report context/submit | `403 REPORT_TARGET_NOT_ALLOWED` |
| BR-17.4 | Cannot report self | Report | `400 CANNOT_REPORT_SELF` |
| V-43 | Reason required, one of the six | Report | `400 REPORT_REASON_INVALID` |
| V-43 / SC-17.5 | "Other" requires a description | Report | `400 REPORT_DESCRIPTION_REQUIRED` |
| V-43 | Description ≤ 500 characters | Report | `400 REPORT_DESCRIPTION_TOO_LONG` |
| BR-17.6 / AC-17.3 | Report alone never blocks or restricts | Report | only `alsoBlock` applies a block |
| BR-17.8 | Reporter sees status, never the action | My Reports | no action field in member DTO |
| BR-17.10 | Reports are never deleted | All | no delete path |
| — | Idempotent active block per ordered pair | Block | `409 ALREADY_BLOCKED`; filtered unique index |
| — | A member's own reports only | List/detail | `404 REPORT_NOT_AVAILABLE` + log |
| V-45 pattern | Admin decision/dismiss reason ≥ 10 chars | Admin | `400 REASON_REQUIRED` |
| §9.7 | Only legal status transitions | Admin | `409 REPORT_STATUS_INVALID` |
| — | `limit` bounds (default 20, clamp 50) | Lists | clamp / `400 SAFETY_LIMIT_INVALID` |
| — | Cursor matches the list | Lists | `400 CURSOR_INVALID` |

No validation framework exists; implement in the services. Do not add FluentValidation as a drive-by change.

---

## 10. Config usage

| CFG | Use in M8 |
|---|---|
| CFG-15 | Escalation threshold (default 3): distinct reporters before flagging/alerting (BR-17.9) |
| CFG-32 | Admin queue overdue threshold in days (default 3): `ageDays >= CFG-32` for Submitted/Under Review (read by M12 through the gateway) |
| CFG-7 | Optional: first-page size for blocked/report lists; otherwise fixed default 20, clamp 50 |

Read config through the Foundation reader; no hard-coded defaults in M8. A changed CFG-15 applies to new submissions; existing `EscalatedOnUtc` rows stay flagged.

---

## 11. Audit events (Section 8)

| Event | Trigger | Old → New recorded |
|---|---|---|
| `BLOCK_APPLIED` | Block submit / also-block | pair ids, source, effects applied. **No reason** (none exists) |
| `BLOCK_REMOVED` | Unblock | pair ids, unblocked on |
| `REPORT_SUBMITTED` | Member report / rating-comment intake | report number, reason code, target type. **Never the description or chat text** |
| `REPORT_ESCALATED` | CFG-15 crossed | target member, distinct reporter count |
| `REPORT_UNDER_REVIEW` | Admin mark | Submitted → Under Review |
| `REPORT_ACTION_RECORDED` | Admin action | Under Review → Action Taken; action type + reason (Admin-only) |
| `REPORT_DISMISSED` | Admin dismiss | Under Review → Dismissed; reason |
| `REPORT_CLOSED` | Admin close | Action Taken/Dismissed → Closed |

Every event includes actor, actor role, UTC timestamp and source IP where the Foundation audit convention includes it. Audit is append-only (BR-8.3). Chat content, report descriptions and the reporter identity are **never** written to platform audit entries.

---

## 12. Error responses

Use `BadRequest(new { code, message })` or object-result `StatusCode(403/404/409, new { code, message })`. Never `Forbid()`/`Challenge()`. Messages align with the UI plan microcopy.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `BLOCK_NOT_ALLOWED` | 403 | "You can block someone you have a lead, connection or chat with." |
| `CANNOT_BLOCK_SELF` | 400 | "You cannot block yourself." |
| `ALREADY_BLOCKED` | 409 | "You have already blocked this person." |
| `BLOCK_NOT_FOUND` | 404 | "This block is no longer active." |
| `CANNOT_REPORT_SELF` | 400 | "You cannot report yourself." |
| `REPORT_TARGET_NOT_ALLOWED` | 403 | "You can report someone you have a lead, connection or chat with." |
| `REPORT_REASON_INVALID` | 400 | "Choose a reason for the report." |
| `REPORT_DESCRIPTION_REQUIRED` | 400 | "Please describe what happened when you choose Other." |
| `REPORT_DESCRIPTION_TOO_LONG` | 400 | "Details can be up to 500 characters." |
| `REPORT_NOT_AVAILABLE` | 404 | "This report is not available." |
| `REPORT_STATUS_INVALID` | 409 | "This report cannot move to that status." |
| `MEMBER_NOT_AVAILABLE` | 404 | "This person is no longer available." |
| `REASON_REQUIRED` | 400 | "Enter a reason of at least 10 characters." |
| `SAFETY_LIMIT_INVALID` | 400 | "Choose a valid page size." |
| `CURSOR_INVALID` | 400 | "Refresh the list and try again." |
| `SAFETY_ACTION_FAILED` | 500 | "We could not complete that. Try again." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

---

## 13. Privacy and security

- **Blocked member is never told** (BR-17.2, AC-17.2): no notification, no status change they can see, no API field that reveals a block. The connection simply shows "Ended" to them through M5's neutral label.
- **Reporter privacy** (BR-17.7, AC-17.4): the reported member is never shown who reported them in any member DTO or notification. Only M12's Admin DTOs carry the reporter identity (BR-21.10).
- **My Reports** only ever returns the caller's own reports; a foreign `reportId`/`blockId` returns `404` and logs the attempt (BR-4.2 pattern).
- **Blocked list** only shows rows where the caller is the blocker; the blocked party cannot enumerate who blocked them.
- **No contact data** (email/mobile/IP/device/login history) appears in any M8 member DTO; other members are projected through M1's public card (BR-4.9, D-21). Add a serialization test that fails if those keys appear.
- **Chat evidence** is a reference only; content is read solely through M6's Admin open-report gateway and every access is logged (BR-4.7/15.11, AC-15.5).
- **Report descriptions** are Admin-only, stored once, never logged in audit/errors/notifications.
- **Reports are immutable history** (BR-17.10): no delete; only status transitions.
- **Blocking is the only immediate member-to-member action**; reporting changes nothing unless "Also block" is ticked (BR-17.6).
- **Escalation** uses distinct reporters, so one member cannot trigger the CFG-15 alert alone; the alert is raised once per crossing (open decision on re-arming).
- **Concurrency:** the filtered unique active-block index prevents duplicate blocks from a double submit; duplicate submissions return `409 ALREADY_BLOCKED`. A report has no uniqueness constraint (multiple incidents allowed); the escalation count uses distinct reporters.

---

## 14. Reporting hooks (RPT-17, RPT-22)

M8 owns the data; M15 renders it through `ISafetyReportDataSource` and `IBlockReportDataSource`. Reports never expose chat text.

- **RPT-17 Safety Report Register** — every report: number, submitted date, reporter (Admin-only view), reported member, reason, status, action (Admin-only), days open. Filter by period/status/reason. Source: `raah_SafetyReport`.
- **RPT-22 Member Block Summary** — blocks per member: active blocks given, blocked-by count, total blocks over a period, unblocked count. Source: `raah_Block` (no chat/report text).

No member endpoint returns another member's reports or block counterparties.

---

## 15. Build sequence (M8)

| Step | Work | Output |
|---|---|---|
| 1 | Enums, `raah_Block` + `raah_SafetyReport` entities/configurations + migration | Compiles, migration applies, `member_Master` untouched |
| 2 | `BlockService.ApplyBlockAsync` (insert + M3 withdraw + M5 end/close) and `UnblockAsync` | AC-17.1 effects; M3/M5 fakes |
| 3 | Register real `IBlockPolicyGateway`, `IBlockGateway`, `IInteractionPolicyGateway` (replace no-ops) | Entry points and matching exclusion live |
| 4 | Block context, block, blocked list, unblock endpoints | SCR-17.1/17.2 read/write |
| 5 | `IReportEligibilityReader` using M4/M5/M6 small reads; report context endpoint | SCR-17.3 context |
| 6 | `ReportService.SubmitAsync` (validation, SAF number, attachment, also-block, escalation) | BR-17.4–17.9, AC-17.3/17.5 |
| 7 | My Reports list + detail endpoints | SCR-17.4 |
| 8 | `IReportIntakeGateway` for M7 rating-comment reports | BR-16.9/17.12 |
| 9 | `IReportAdminGateway` + `IReportAccessPolicyGateway` + audit | M12/M6 ready |
| 10 | `ISafetyReportDataSource` / `IBlockReportDataSource` for M15 | RPT-17/22 |
| 11 | Wire M9 notifications and M12/M10 actions as they land | End-to-end |

Suggested folders:

```
Binsera.Core/Safety/
  Domain/Raah/Block.cs, SafetyReport.cs
  Enums/BlockSource.cs, ReportReason.cs, ReportTargetType.cs,
        SafetyReportStatus.cs, ReportDecisionAction.cs
  Models/Raah/BlockContextDto.cs, BlockResultDto.cs, BlockedMemberListItemDto.cs,
               ReportContextDto.cs, SubmitReportRequest.cs, ReportSubmittedDto.cs,
               MyReportListItemDto.cs, MyReportDetailDto.cs, ReportQueueItemDto.cs,
               ReportDetailDto.cs, FlaggedMemberDto.cs
  Contracts/IBlockService.cs, IReportService.cs, IReportQueryService.cs,
            IBlockPolicyGateway.cs, IBlockGateway.cs, IInteractionPolicyGateway.cs,
            IReportIntakeGateway.cs, IReportAccessPolicyGateway.cs,
            IReportAdminGateway.cs, ISafetyReportDataSource.cs, IBlockReportDataSource.cs
Binsera.Application/Safety/Services/
  BlockService.cs, ReportService.cs, ReportQueryService.cs,
  BlockPolicyService.cs, ReportAdminService.cs,
  ReportEligibilityReader.cs, SafetyReportDataSource.cs, BlockReportDataSource.cs
  Gateways/NoOpNotificationGateway.cs, NoOpAdminAlertGateway.cs (as needed)
Binsera.API/Controllers/Safety/SafetyController.cs
Binsera.Persistence/Configurations/Raah/BlockConfiguration.cs, SafetyReportConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahBlockAndSafetyReport.cs
```

---

## 16. Test plan

### 16.1 Unit

- **Block effects:** Accepted connection → leads withdrawn both ways, Pending withdrawn both ways, connection `Blocked`, chat closed (with M3/M5 fakes) (AC-17.1).
- **No notification:** block never calls `INotificationGateway` for either party (AC-17.2).
- **Block guards:** self → `CANNOT_BLOCK_SELF`; no relation → `BLOCK_NOT_ALLOWED`; second block → `ALREADY_BLOCKED`; concurrent double submit yields one active row (filtered unique index).
- **Unblock:** only the blocker's own active row; sets ended; calls `RunForPairAsync`; never reopens connection/chat; reverse-direction active block keeps the pair excluded.
- **Report eligibility:** lead-only, past connection, and existing chat each allow; stranger → `REPORT_TARGET_NOT_ALLOWED` (SC-17.6); self refused.
- **Reason/description:** each of the six reasons accepted; unknown rejected; `Other` + empty → required; 500 accepted / 501 rejected; whitespace trimmed; empty description stored as null.
- **Also block:** false → no block row and no M3/M5 effect (AC-17.3); true → block applied in the same transaction (SC-17.4); already blocked → report saved, `AlsoBlockApplied = false`.
- **Conversation attach:** chat exists → `ConversationAttached` and `ChatId` set (closed chat included); no chat → false.
- **Escalation:** CFG-15 = 3; distinct reporters 1,2,3 → flag + one alert on the third; the same reporter repeatedly counts once; a Dismissed report is excluded; CFG-15 changed applies to new submits.
- **My reports:** only own; status labels; `decisionSummary` never contains the action; foreign id → 404; Closed still readable; no delete path.
- **Admin transitions:** Submitted→UnderReview→ActionTaken→Closed and UnderReview→Dismissed→Closed allowed; illegal transitions → `REPORT_STATUS_INVALID`; reason < 10 rejected.
- **Flagged members:** `distinctReporters >= CFG-15`; comes from the same count as the alert.

### 16.2 Integration / API

- Every endpoint requires auth; foreign ids → 404 + logged; missing token → 401; envelope present and shaped like the DTO.
- Migration applies cleanly and does not touch `member_Master`; no FK to a legacy non-EF table beyond `member_Master.Id`.
- Blocking from a lead removes it from both members' lead lists via M3 stubs; blocking from a chat closes it via M5/M6 stubs.
- Unblocking re-runs matching and a matching pair's leads can reappear (SC-17.3).
- A report with a chat lets M6's `IsReportOpenForAsync` return true until the report is Closed; after Closing it returns false.
- Notifications dispatched only to the reporter on decision (with M9 fake), never to the blocked member.
- RPT-17/22 aggregates correct; RPT-17 includes the action only for Admin audiences.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-17.1 Immediate mutual block | §7.2 step 6, §8.9, §16.1 |
| AC-17.2 Blocked member not notified | §7.2 step 6.4, §13, §16.1 |
| AC-17.3 Report does not block automatically | §7.6 step 5/7, §9, §16.1 |
| AC-17.4 Reporter privacy | §5.6, §7.8, §13, §16.1 |
| AC-17.5 Escalation threshold | §7.6 step 7.4, §8.8, §16.1 |

Scenarios SC-17.1…SC-17.9 map to the endpoints and rules above: SC-17.1 block from lead → §7.2; SC-17.2 block from chat → §7.2 (M5 closes chat); SC-17.3 unblock → §7.4; SC-17.4 report + also block → §7.6; SC-17.5 Other empty → §7.6 step 4; SC-17.6 stranger → §7.6 step 2; SC-17.7 three reporters → §8.8; SC-17.8 reported member told of outcome → M12/M10 (M8 supplies the record only); SC-17.9 reporter follows up → §7.7/§7.8.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| — | Must a member have contact (lead/connection/chat) to **block**, or is any member blockable? | **Contact required** — block entry points are lead/connection/chat (BR-17.1); enforce server-side |
| — | Escalation window ("within a short time")? | **Rolling 30 days**, distinct reporters, status ≠ Dismissed |
| — | Do Closed reports still count toward escalation? | **Only non-dismissed** within the window (Action Taken/Closed count) |
| — | Can a member report the same member more than once? | **Yes** (each incident), no dedupe; repeated unfounded reports handled by M12 |
| — | Is the escalation alert raised again after the count grows? | **Once per crossing**; `EscalatedOnUtc` marks the report. Re-arm policy is an open product question |
| — | Should the description reject phone numbers/links? | **No** — free text up to 500, Admin-only; no V-41 filter (unlike chat) |
| — | Blocking twice: idempotent success or error? | `409 ALREADY_BLOCKED`; UI hides the action when already blocked |
| — | Can a member block someone who blocked them first? | **Yes** — each direction is stored independently; the pair stays blocked |
| — | Is a chat attached only while Accepted? | Attach **any existing chat** (open or closed) with the pair; closed chats remain readable |
| — | Who sends the "report decided" notification? | M8 raises `INotificationGateway.ReportDecidedAsync` when Admin records a decision; M9 renders it. Reported-member notices are M12/M10 |
| — | Does the Admin take-action step also restrict/block the member? | **No** — M12 executes via M10, then calls M8 `RecordActionAsync` |
| — | Store a chat snapshot or a reference? | **Reference** (`ChatId`); M6 retains messages and logs Admin access |
| — | Page size (no CFG)? | Fixed default 20, clamp 50, keyset "Show more" |
| — | Report number format | `SAF-{nnnnnn}` via `IRecordNumberGenerator` |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| SCR-17.1 Block Member | §5.1/5.2, §7.1, §7.2 |
| SCR-17.2 Blocked Members | §5.3, §7.3, §7.4 |
| SCR-17.3 Report Member | §5.4/5.5, §7.5, §7.6 |
| SCR-17.4 My Reports | §5.6, §7.7, §7.8 |
| Field spec (Report) | §4.2, §5.4/5.5 |
| Section 17.4 Block effects | §4.1, §7.2, §8.9 |
| BR-17.1 block immediate/mutual | §7.2, §8.1, §9 |
| BR-17.2 blocked member not notified | §7.2, §8.6, §13 |
| BR-17.3 unblock; no restore | §7.4, §8.2 |
| BR-17.4 report eligibility | §7.5, §7.6, §8.3 |
| BR-17.5 chat access notice | §5.4, §7.5 |
| BR-17.6 report does not act | §7.6, §9 |
| BR-17.7 reporter privacy | §5.6, §7.8, §13 |
| BR-17.8 status, never action | §5.6, §7.7, §7.8 |
| BR-17.9 escalation CFG-15 | §7.6, §8.8, §10 |
| BR-17.10 never deleted | §4.2, §7.8, §13 |
| BR-17.11 reporter restriction is Admin | out of scope (M12/M10) |
| BR-17.12 rating comment report | §8.4 |
| Status lifecycle §9.7 | §4.4, §8.5 |
| CFG-15 / CFG-32 | §10 |
| V-43 / V-44 | §7.6, §9 |
| SC-17.1…SC-17.9 | §16.3 |
| AC-17.1…AC-17.5 | §16.3 |
| Data Impact | §7.2, §7.6, §11 |
| RPT-17 / RPT-22 | §14 |
| Module boundaries (M3/M4/M5/M6/M7/M9/M12) | §2 |
