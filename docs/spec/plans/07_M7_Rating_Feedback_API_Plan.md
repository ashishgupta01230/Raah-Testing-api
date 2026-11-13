# M7 — Rating & Feedback — API Plan

| | |
|---|---|
| **Module** | M7 — Rating & Feedback (spec `docs/spec/07_M7_Rating_Feedback.md`) |
| **Document type** | API implementation plan: contracts, data model, endpoints, lifecycle, eligibility, validation, audit, reporting hooks, sequencing, tests |
| **Build order** | #7 after Foundation + M1…M5 (see `00_Execution_Plan.md`); M6 and M8/M9 are built in the same phase but M7 only needs M5 |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M7 lets a member rate another member they were connected with, and lets the rated member and Admin see the individual ratings. It turns the stored ratings into the average/count shown everywhere a member is compared.

1. **Rate a connection** — 1–5 stars plus an optional comment on an Accepted connection, past or present (SCR-16.1, BR-16.1, BR-16.4, V-41).
2. **One rating per member per connection** — a new connection after a disconnect allows a new rating (BR-16.2, V-42).
3. **Final once submitted** — no edit, no delete path (BR-16.3).
4. **Ratings received** — the member sees stars, comment, rater identity, date; a moderator-hidden comment shows "Comment hidden" while its stars still count (SCR-16.2, BR-16.5, BR-16.7, BR-16.8, BR-16.11).
5. **Ratings given** — the rater sees their own ratings (SCR-16.3).
6. **Summary projection** — write `RatingAverage` / `RatingCount` into M1's `raah_MemberProfile` so profiles, leads and connections show one consistent summary; below CFG-28 the summary reads "New member" (BR-16.6).
7. **Report a comment** — hand the comment to M8's report intake; M12 moderates it (BR-16.9, BR-17.12, SCR-21.6).
8. **Admin moderation gateway** — M12 consumes M7 to list reported comments and hide a comment with a reason (BR-16.8, CFG-14).
9. **Reporting hooks** — RPT-14/15/16 data readers for M15 (counts/averages only, never exposed publicly).

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Connection creation, status, history, "was accepted" | M5 |
| Public member card/profile and where the average is displayed | M1 (writes through M1 projection) |
| Notification centre and email for "rating received" / "comment hidden" | M9 |
| Report creation (rating comment report), report lifecycle, emergency copy | M8 |
| Admin rating-moderation screen and hide action UI | M12 |
| Configuration editing for CFG-13/14/27/28 | M13 |
| RPT-14/15/16 rendering | M15 |
| Matching/ranking | M3 (rating must never affect ranking — BR-16.10) |

M7 **does not** change matching, connections, or chats. A rating is informational only (BR-16.10).

---

## 2. Dependency and module-boundary handling

M7 is built after M5 (Accepted connections exist). It consumes M1/M5/Foundation and provides small gateways to M5, M8, M9, M12 and M15.

### 2.1 Contracts consumed by M7

| Contract | Owner | Purpose |
|---|---|---|
| `IConnectionReadGateway.GetForRatingAsync(connectionId)` | M5 | Participants, connection status, `wasEverAccepted`, ended/responded times. **Add this one read method** to M5's existing gateway; no M5 endpoint change |
| `IPublicMemberProfileService.GetAsync(memberId, viewerMemberId)` | M1 | Limited public card for the other member (no email/mobile) |
| `IMemberRatingProjectionWriter.UpsertAsync(memberId, average, count)` | M1 | Write the summary into `raah_MemberProfile.RatingAverage/RatingCount`; M1 owns the table, M7 owns the calculation |
| `IMemberStatusReader.GetAsync(memberId)` | Foundation | Active / Restricted / Inactive / Blocked guard for the rater |
| `IAppConfigReader` | Foundation | CFG-13, CFG-14, CFG-27, CFG-28 read live |
| `IRecordNumberGenerator` | Foundation | `RAT-{nnnnnn}` numbering (BR-7.1/7.2, no yearly reset) |
| `IBlockPolicyGateway.IsBlockedPairAsync(a, b)` | M8-owned, M7 consumes | Refuse rating for a blocked pair (safety) |
| `IReportIntakeGateway.CreateRatingCommentReportAsync(ratingId, reporterMemberId, reason, details)` | M8-owned, M7 consumes (no-op until M8) | "Report comment" (BR-16.9, BR-17.12) |
| `INotificationGateway` | M9-owned, M7 consumes (no-op until M9) | Rating received, comment hidden |
| `IAuditService` | Foundation | Submit, hide, report-intake audit |

Rules:

- M7 depends on M8/M9 only through the small gateways above; each no-op is safe (report dropped with a "not available" result, notification dropped).
- When M8/M9 land they register the real implementations; M7 endpoint contracts do not change.
- M1 keeps ownership of `raah_MemberProfile`; M7 never edits that table directly. It calls `IMemberRatingProjectionWriter` (M1-provided) inside its own transaction boundary.
- M5 remains the single source of truth for "was this connection ever accepted"; M7 does not duplicate connection state.

### 2.2 Contracts provided by M7 (consumed later)

| Contract | Purpose | Consumer |
|---|---|---|
| `IRatingSummaryGateway` (real implementation) | Replaces M1's no-op reader: `GetSummaryAsync(memberId)` → `{ average, count, isNewMember, threshold }` | M1 public/own profile, M4 lead cards, M5 connection detail |
| `IConnectionRatingReader` (real implementation) | Replaces M5's no-op: `GetRatingStateAsync(connectionId, raterMemberId)` → `{ canRate, alreadyRated, ratingDisabled }` | M5 connection detail / chat header |
| `IRatingReadGateway.GetForReportAsync(ratingId, reporterMemberId)` | Verify a rating belongs to the reporter and return stars/comment/target for M8's report intake | M8 |
| `IRatingModerationGateway` | `GetModerationQueueAsync(filter)`, `HideCommentAsync(ratingId, reason, adminMemberId)` | M12 |
| `IRatingReportDataSource` | Aggregate/distribution queries for RPT-14/15/16, no comment text in exports | M15 |

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; `[Route("ratings")]`; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interface in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` → `ResponseFilterAttribute` envelope `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | `BadRequest(new { code, message })` / object-result `StatusCode(404/409/403/429, new { code, message })`; **never** `Forbid()` / `Challenge()` |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new EF-managed table; `member_Master` untouched; `raah_MemberProfile` written only through M1's projection writer |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id`, decimal precision, audit columns, `IsDeleted`, per-column indexes |
| Validation | No FluentValidation; validate in `RatingService` / `RatingQueryService` |
| Current user | Derive the member id from the token; never accept a `fromMemberId` in the body |
| Background job | None required. Optional repair job for the projection (see §8.4) uses the standard `BackgroundService` + `Cronos` pattern |

---

## 4. Data model

### 4.1 New table `raah_Rating`

One row per **member per connection**. A connection has at most two ratings (one in each direction); a later connection for the same pair gets new rows (BR-16.2, BR-16.8).

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `RatingNumber` | nvarchar(20) | `RAT-000042`, generated on submit, never reused (BR-7.1/7.2) |
| `ConnectionId` | bigint | FK → `raah_Connection.Id`; the rating belongs to this connection |
| `FromMemberId` | bigint | FK → `member_Master.Id`; the rater (derived from the token) |
| `ToMemberId` | bigint | FK → `member_Master.Id`; the member rated |
| `Stars` | tinyint | 1–5, whole numbers only (BR-16.4) |
| `Comment` | nvarchar(1000) null | Stored text; validated against **current** CFG-27 at submit (max range 1000). Never edited |
| `Status` | tinyint | `RatingStatus` (§4.3): `Submitted = 1`, `Hidden = 2`. `Hidden` hides the **comment only**; stars always count |
| `HiddenOnUtc` | datetime2 null | Set on moderation |
| `HiddenByMemberId` | bigint null | Admin who hid the comment |
| `HiddenReason` | nvarchar(500) null | Audit-style reason (V-45 pattern) |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false (ratings are never deleted — BR-16.11) |

Indexes:

- Unique `RatingNumber`.
- **Unique `(ConnectionId, FromMemberId)`** — one rating per member per connection (BR-16.2).
- `(ToMemberId, CreatedDate DESC)` — received list.
- `(FromMemberId, CreatedDate DESC)` — given list.
- Filtered `(Status, HiddenOnUtc DESC)` where `Status = Hidden` — moderation log (RPT-16).
- `(ToMemberId, Stars)` — distribution/aggregate helper.

There is **no** update, edit or delete path. `Status` changes only through Admin moderation and only from `Submitted` → `Hidden`.

### 4.2 Where the average lives

The rating rows are the source of truth. The display summary lives on M1's projection columns:

- `raah_MemberProfile.RatingAverage` (decimal(2,1), null until first rating)
- `raah_MemberProfile.RatingCount` (int)

M7 recomputes both from `raah_Rating` **including Hidden rows** (stars still count — BR-16.11, AC-16.4) and writes them through `IMemberRatingProjectionWriter` in the same transaction as the submit. `isNewMember` / the "New member" label is decided by readers from CFG-28, not stored, so changing CFG-28 takes effect immediately (BR-16.6).

### 4.3 Enums

```
RatingStatus       { Submitted = 1, Hidden = 2 }
RatingListScope    { Received = 1, Given = 2 }
```

Migration (indicative): `20260107000000_AddRaahRating`.

---

## 5. DTOs (Binsera.Core)

Namespace: `Binsera.Core.Ratings.Models.Raah`.

### 5.1 `RateContextDto` (SCR-16.1 entry)

```
connectionId            long
connectionNumber        string            // CON-nnnnnn
connectionStatus        string            // "Accepted" | "Disconnected" | "Blocked" | ...
wasEverAccepted         bool
otherMember             PublicMemberCardDto   // M1 limited card; no email/mobile
ratingEnabled           bool              // CFG-13
canRate                 bool
alreadyRated            bool
myRating                MyRatingDto?      // when alreadyRated: my stars/comment/date
limits                  RatingLimitsDto   // { starsMin:1, starsMax:5, commentMaxLength: CFG-27 }
ratingThreshold         int               // CFG-28 (not shown to the rater as a target)
disabledReasonCode      string?           // "ALREADY_RATED" | "NOT_ACCEPTED" | "RATING_DISABLED" | "BLOCKED" | "SELF"
disabledReasonMessage   string?
```

`MyRatingDto` → `{ ratingId, ratingNumber, stars, comment, commentHidden, submittedOn }`.

### 5.2 `RatingSubmittedDto` (after submit)

```
ratingId, ratingNumber, stars, commentPresent bool, submittedOn
memberSummary           RatingSummaryDto    // the rated member's new average/count/isNewMember
```

### 5.3 `RatingSummaryDto` (also the `IRatingSummaryGateway` payload)

```
average                 decimal?            // one decimal, null until the first rating
count                   int
isNewMember             bool                // count < CFG-28
threshold               int                 // CFG-28
```

### 5.4 `RatingListItemDto` (received / given)

```
ratingId                long
ratingNumber            string
stars                   int                 // 1–5, always shown
comment                 string?             // received: null when hidden; given: own comment
commentHidden           bool
submittedOn             DateTime
otherMember             PublicMemberCardDto // received → rater; given → rated member (BR-16.7)
canReportComment        bool                // received only, when a comment exists and is not hidden
statusLabel             string              // "Submitted" | "Comment hidden"
```

### 5.5 `RatingListResponseDto`

```
scope                   string            // "Received" | "Given"
summary                 RatingSummaryDto  // the viewer's own summary on both tabs
items                   RatingListItemDto[]
hasMore                 bool
nextCursor              string?
```

### 5.6 Admin moderation DTOs (consumed by M12)

| DTO | Fields |
|---|---|
| `RatingModerationItemDto` | `ratingId`, `ratingNumber`, `stars`, `comment`, `commentHidden`, `submittedOn`, `rater PublicMemberCardDto`, `ratedMember PublicMemberCardDto`, `status`, `hiddenOn`, `hiddenReason`, `reportCount`, `reportId?` |
| `HideRatingCommentRequest` | `reason` (string, min 10 chars — V-45 pattern) |
| `HideRatingCommentResultDto` | `ratingId`, `ratingNumber`, `status`, `hiddenOn`, `memberSummary` (unchanged stars/count) |

### 5.7 Status labels

| Stored | Shown to rated member | Shown to rater |
|---|---|---|
| Submitted | "Submitted" | "Submitted" |
| Hidden | "Comment hidden" (stars still shown) | "Comment hidden" |

Never include email, mobile, IP, device, login history or admin notes in any M7 DTO.

---

## 6. Endpoint catalogue

Base route: `/ratings`. Every endpoint requires an authenticated **Member** token and acts only on ratings/connections the member belongs to (BR-16.1, BR-16.5).

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/ratings/context/{connectionId}` | Rate context: other member, eligibility, limits, my rating | SCR-16.1 | 200 `RateContextDto` |
| 2 | POST | `/ratings` | Submit a rating for a connection | SCR-16.1 | 200 `RatingSubmittedDto` |
| 3 | GET | `/ratings/received?limit=&cursor=` | Ratings and comments received | SCR-16.2 | 200 `RatingListResponseDto` |
| 4 | GET | `/ratings/given?limit=&cursor=` | Ratings the member gave | SCR-16.3 | 200 `RatingListResponseDto` |
| 5 | POST | `/ratings/{ratingId}/report-comment` | Report a received comment to Admin (via M8 intake) | SCR-16.2 | 200 `{ reportNumber?, message }` |

Admin surface (owned by M12, backed by `IRatingModerationGateway` — not part of the member API):

| # | Method | Route | Purpose |
|---|---|---|---|
| A1 | GET | `/admin/safety/rating-moderation?status=&limit=&cursor=` | Reported / hidden comment queue (SCR-21.6) |
| A2 | POST | `/admin/safety/rating-moderation/{ratingId}/hide` | Hide a comment with a reason (BR-16.8, CFG-14) |

**Identifiers.** Routes use numeric `connectionId` / `ratingId`. DTOs expose the user-facing `connectionNumber` / `ratingNumber`.

Forward-looking (not part of the M7 member API):

- The public average is delivered through M1's `PublicMemberProfileDto.rating`; there is no public `/ratings/member/{id}` endpoint.
- Reporting a comment is a Safety feature; M7 provides the intake call and the read gateway, M8 owns the report record and lifecycle.
- Hiding a comment is an Admin action; M7 provides the gateway and the audit, M12 owns the screen.

---

## 7. Endpoint details

### 7.1 `GET /ratings/context/{connectionId}`

1. Resolve `meMemberId` from the token.
2. Load the connection through `IConnectionReadGateway.GetForRatingAsync`; if missing or the viewer is neither participant → `404 RATING_NOT_AVAILABLE` **and log the denied attempt** (BR-4.2).
3. `otherMember` = the participant that is not the viewer, projected through M1 (public card only — AC-10.1/BR-4.9).
4. `wasEverAccepted` from M5 (Accepted today **or** an Accepted event in `raah_ConnectionHistory` — SC-16.7).
5. `ratingEnabled` = CFG-13.
6. `alreadyRated` = a `raah_Rating` row exists for `(ConnectionId, me)`; if so, populate `myRating`.
7. `canRate` = `ratingEnabled AND wasEverAccepted AND NOT alreadyRated AND connectionStatus != "Blocked" AND viewer is Active/Restricted` (never a self-rating; impossible here).
8. `disabledReasonCode/Message` in this order: `ALREADY_RATED`, `NOT_ACCEPTED`, `RATING_DISABLED`, `BLOCKED`.
9. Read limits from CFG-27; `starsMin = 1`, `starsMax = 5`.

Reading the context is side-effect-free.

### 7.2 `POST /ratings`

Request: `{ connectionId, stars, comment? }` — never `fromMemberId`.

Rules (BR-16.1–16.4, V-41, V-42, AC-16.1/16.2):

1. **Rating enabled** — CFG-13 must be on, else `403 RATING_DISABLED`.
2. **Connection valid** — exists, viewer is a participant, and `wasEverAccepted == true`, else `409 RATING_NOT_ALLOWED` (covers never-connected and only-lead cases, AC-16.1).
3. **Not self** — `FromMemberId != ToMemberId` (impossible via a real connection; defensive `400 SELF_RATING`).
4. **Not blocked** — `IBlockPolicyGateway.IsBlockedPairAsync` must be false; else `409 RATING_NOT_ALLOWED` (safety default; open decision for "accepted in the past").
5. **Not already rated** — check `(ConnectionId, me)`; else `409 ALREADY_RATED` ("You have already rated this connection." — SC-16.3, AC-16.2). The unique index is the final guard against a race; catch the duplicate and return the same code.
6. **Stars** — integer 1–5, else `400 STARS_INVALID`.
7. **Comment** — trim; empty → stored as `null`. If present:
   - length ≤ current CFG-27, else `400 COMMENT_TOO_LONG` with the limit;
   - no web links and no run of 10+ digits, else `400 COMMENT_CONTAINS_CONTACT` "Please do not enter phone numbers or links." (V-41, SC-16.8). Reject; do not silently strip.
8. In one transaction (`IUnitOfWork`):
   1. Generate `RatingNumber` (`IRecordNumberGenerator`, `RAT-`).
   2. Insert `raah_Rating` (`Status = Submitted`, `FromMemberId = me`, `ToMemberId = other`).
   3. Recompute the rated member's summary over **all** their ratings (Submitted + Hidden):
      `count = COUNT(*)`, `average = ROUND(AVG(Stars), 1)`.
   4. `IMemberRatingProjectionWriter.UpsertAsync(ToMemberId, average, count)`.
   5. Emit `INotificationGateway.RatingReceivedAsync(ToMemberId, fromName, stars)` (M9 no-op).
   6. Audit `RATING_SUBMITTED` (old → new summary, stars; never the raw comment in logs).
9. Return `RatingSubmittedDto` with the new summary.

A rating is **final**: there is no PUT/PATCH/DELETE for a rating (BR-16.3, AC-16.1/16.2).

### 7.3 `GET /ratings/received`

1. Resolve `ToMemberId = me`.
2. `summary` = `IRatingSummaryGateway.GetSummaryAsync(me)` (average/count/isNewMember from CFG-28).
3. Order `CreatedDate DESC, Id DESC`; keyset paging (`limit` default 20, clamp 50).
4. For each row:
   - `otherMember` = rater public card (BR-16.7 — identity not hidden from the rated member).
   - `comment` = null when `Status = Hidden`, else the stored comment; `commentHidden` reflects the status.
   - `canReportComment` = a visible non-empty comment exists.
   - `statusLabel` = "Submitted" / "Comment hidden".
5. Stars are always returned, including for Hidden rows (BR-16.8/16.11).

### 7.4 `GET /ratings/given`

1. Resolve `FromMemberId = me`.
2. Same summary/paging.
3. `otherMember` = rated member public card.
4. `comment` = own comment (the rater may still see their own text, even if Admin hid it? — default: show own comment with the "Comment hidden" note; recorded as an open decision).
5. `canReportComment = false` (a rater cannot report their own comment).
6. No edit/delete links, because none exist (AC-16.1/16.2).

### 7.5 `POST /ratings/{ratingId}/report-comment`

1. Resolve `meMemberId` from the token.
2. Load the rating; it must be `ToMemberId = me` and have a visible non-empty comment, else `404 RATING_NOT_AVAILABLE` (AC-16.5 privacy).
3. A rating already hidden cannot be reported again → `409 COMMENT_ALREADY_HIDDEN`.
4. Call `IReportIntakeGateway.CreateRatingCommentReportAsync(ratingId, me, reason, details)`:
   - M8 creates a Safety Report (`SAF-`) with target type "Rating comment" and the comment attached;
   - when M8 is absent (no-op), return `503 REPORT_UNAVAILABLE` and the UI hides the action until M8 lands.
5. Audit `RATING_COMMENT_REPORTED` (no comment text).
6. Return `{ reportNumber?, message: "Report sent. We will review it." }`.

The report lifecycle, Admin visibility and outcome notices belong to M8/M12 (BR-16.9, BR-17.12).

---

## 8. Lifecycle, projection and moderation

### 8.1 Rating lifecycle (spec §9.8)

| From | To | Who | M7 mechanism |
|---|---|---|---|
| (none) | Submitted | Member | `POST /ratings` (BR-16.1/16.4) |
| Submitted | Hidden (comment only) | Admin | `IRatingModerationGateway.HideCommentAsync` via M12 (BR-16.8) |

There is no other transition. Ratings are never deleted; a Hidden rating keeps its stars in the average (BR-16.11, AC-16.4).

### 8.2 `IRatingSummaryGateway` (real implementation)

`GetSummaryAsync(memberId)` reads `raah_MemberProfile.RatingAverage/RatingCount` (falling back to an on-the-fly aggregate if the projection row is missing), applies CFG-28 and returns `RatingSummaryDto`. This replaces M1's no-op and is used by M1 (own/preview), M4 (lead card) and M5 (connection detail).

### 8.3 `IConnectionRatingReader` (real implementation)

`GetRatingStateAsync(connectionId, raterMemberId)` → `{ canRate, alreadyRated, ratingDisabled, wasEverAccepted }`. This replaces M5's no-op `canRate`/`alreadyRated` and feeds the connection detail and chat header "Rate" action.

### 8.4 Projection consistency

- Submit path recomputes and writes the summary atomically (§7.2 step 8).
- Hide changes only `Status`; stars do not change, so the summary is unchanged (AC-16.4). A defensive recompute is still written for the audit trail.
- An optional `RatingProjectionRepairJob` (`BackgroundService`, config section `RatingProjectionRepair { Enabled, Cron, TimeZone }`) can recompute members whose stored count differs from `COUNT(raah_Rating)`. It is **not** required for Phase 1 correctness and is off by default.

### 8.5 `IRatingModerationGateway` (consumed by M12)

| Method | Behaviour |
|---|---|
| `GetModerationQueueAsync(filter)` | Reported comments (from M8 report data) and already-hidden comments, newest first, paged; includes rater/rated public cards and report count |
| `HideCommentAsync(ratingId, reason, adminMemberId)` | Requires CFG-14 on; reason ≥ 10 chars; `Status = Hidden`, `HiddenOnUtc/By/Reason`; audit `RATING_COMMENT_HIDDEN`; notify the rater (M9) that the comment was hidden; stars and summary unchanged. Idempotent: hiding twice returns `409 COMMENT_ALREADY_HIDDEN` |

Admin moderation is available only when CFG-14 is on (otherwise the queue is read-only and hide is refused with `MODERATION_DISABLED`).

---

## 9. Validation matrix

| ID / rule | Rule | Where | Error |
|---|---|---|---|
| BR-16.1 / AC-16.1 / V-42 | Only a member with an Accepted connection (now or past) can rate | Submit | `409 RATING_NOT_ALLOWED` |
| BR-16.2 / AC-16.2 / V-42 | One rating per member per connection | Submit | `409 ALREADY_RATED`; unique index |
| BR-16.3 | Final once submitted; no edit/delete | — | No such endpoint exists |
| BR-16.4 / V-41 | Stars required, whole 1–5 | Submit | `400 STARS_INVALID` |
| V-41 | Comment ≤ current CFG-27 | Submit | `400 COMMENT_TOO_LONG` |
| V-41 / SC-16.8 | No links, no run of 10+ digits in a comment | Submit | `400 COMMENT_CONTAINS_CONTACT` |
| CFG-13 | Rating must be enabled | Submit | `403 RATING_DISABLED` |
| — | Viewer must belong to the connection/rating | Context / lists / report | `404 RATING_NOT_AVAILABLE` + log |
| — | Reported comment must be visible and belong to the reporter | Report | `404 RATING_NOT_AVAILABLE` / `409 COMMENT_ALREADY_HIDDEN` |
| CFG-14 | Hide only when moderation is enabled | Hide | `403 MODERATION_DISABLED` |
| V-45 pattern | Hide reason ≥ 10 characters | Hide | `400 REASON_REQUIRED` |
| — | `limit` within bounds (default 20, clamp 50) | Lists | clamp / `400 RATING_LIMIT_INVALID` |
| — | Cursor matches the list | Lists | `400 CURSOR_INVALID` |

No validation framework exists; implement in the services. Do not add FluentValidation as a drive-by change.

---

## 10. Config usage

| CFG | Use in M7 |
|---|---|
| CFG-13 | Rating enabled: gate submit and the `canRate` flag; when off, existing ratings stay readable |
| CFG-14 | Comment moderation enabled: gate the Admin hide action |
| CFG-27 | Maximum comment length validated on submit and shown as the counter limit; read live (a shortened value applies to new comments, never invalidates stored ones) |
| CFG-28 | Minimum ratings before the average shows; applied by readers (`isNewMember` / "New member") |

Read config through the Foundation reader; no hard-coded defaults in M7 (defaults live in Foundation/M13).

---

## 11. Audit events (Section 8)

| Event | Trigger | Old → New recorded |
|---|---|---|
| `RATING_SUBMITTED` | Submit | summary count/average before → after; stars; rating number. **Never log the comment text** |
| `RATING_COMMENT_HIDDEN` | Admin hide | Submitted → Hidden; reason; rater notified |
| `RATING_COMMENT_REPORTED` | Report comment | rating number → report reference (no comment text) |

Every event includes actor, actor role, UTC timestamp, and source IP where the Foundation audit convention includes it. Audit is append-only (BR-8.3).

---

## 12. Error responses

Use `BadRequest(new { code, message })` or object-result `StatusCode(404/409/403/503, new { code, message })`. Never `Forbid()`/`Challenge()`. Messages align with UI Plan §7.2 and the M7 UI plan microcopy.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `RATING_NOT_AVAILABLE` | 404 | "This rating is not available." |
| `RATING_NOT_ALLOWED` | 409 | "You can rate someone only after you have been connected." |
| `ALREADY_RATED` | 409 | "You have already rated this connection." |
| `RATING_DISABLED` | 403 | "Rating is not available right now." |
| `STARS_INVALID` | 400 | "Choose 1 to 5 stars." |
| `COMMENT_TOO_LONG` | 400 | "Comments can be up to *n* characters." |
| `COMMENT_CONTAINS_CONTACT` | 400 | "Please do not enter phone numbers or links." |
| `COMMENT_ALREADY_HIDDEN` | 409 | "This comment is no longer visible." |
| `REPORT_UNAVAILABLE` | 503 | "Reporting is not available right now." |
| `MODERATION_DISABLED` | 403 | "Comment moderation is turned off." |
| `REASON_REQUIRED` | 400 | "Enter a reason of at least 10 characters." |
| `RATING_LIMIT_INVALID` | 400 | "Choose a valid page size." |
| `CURSOR_INVALID` | 400 | "Refresh the list and try again." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

---

## 13. Privacy and security

- Individual ratings and comments are visible **only to the rated member and Admin** (BR-16.5, AC-16.5). Other members get only the average/count through M1's public profile; add a test asserting the public DTO exposes no comment text and no rater identity.
- The rater's identity is shown to the rated member (BR-16.7) through the public card only — never email/mobile (BR-4.9, D-21).
- Membership and ownership are derived from the token; a foreign `connectionId`/`ratingId` returns `404` and logs the attempt (BR-4.2).
- Ratings are immutable and never deleted (BR-16.3/16.11); there is no update/delete endpoint.
- Comment validation rejects links and 10+ digit runs; the comment is stored verbatim only after validation (V-41). Do not log comment text in errors or audit.
- Hidden comments are returned as `null` to the rated member; stars still count (BR-16.8).
- Rating never feeds matching/ranking (BR-16.10); M7 exposes nothing to M3.
- Admin hide requires CFG-14 and a reason, and is audited (V-45 pattern).
- Concurrency: the unique `(ConnectionId, FromMemberId)` index prevents duplicate ratings from double-submits; handle the duplicate as `409 ALREADY_RATED`.

---

## 14. Reporting hooks (RPT-14, RPT-15, RPT-16)

M7 owns the data; M15 renders it through `IRatingReportDataSource`. Reports never expose comment text to non-Admin audiences.

- **RPT-14 Rating Summary** — per member: ratings count, average, distribution 1–5. Aggregate query over `raah_Rating` (all statuses).
- **RPT-15 Low-Rated Members** — members with `count >= CFG-28` and `average < threshold`, plus reports-against count joined from M12's data by M15. M7 supplies count/average; M15 joins the rest.
- **RPT-16 Rating Moderation Log** — hidden comments: rating number, date, rater, rated member, reason, Admin. M7's moderation gateway supplies these rows; comment text is included for Admin-only reports.

No member endpoint returns another member's comment list.

---

## 15. Build sequence (M7)

| Step | Work | Output |
|---|---|---|
| 1 | Enums, DTOs, `raah_Rating` entity/configuration + migration | Compiles, migration applies |
| 2 | `RatingService.SubmitAsync` (eligibility, validation, numbering, unique guard) | AC-16.1/16.2, V-41/42 |
| 3 | Summary computation + `IMemberRatingProjectionWriter` call; register `IRatingSummaryGateway` | Profiles/leads show averages; "New member" below CFG-28 |
| 4 | `RatingQueryService` received/given lists + context endpoint | SCR-16.1/16.2/16.3 read |
| 5 | `IConnectionRatingReader` implementation; replace M5 no-op | Connection detail / chat "Rate" state |
| 6 | Report-comment intake via `IReportIntakeGateway` (M8 no-op) | BR-16.9 ready |
| 7 | `IRatingModerationGateway` (queue + hide + audit + notification) for M12 | BR-16.8/AC-16.4 |
| 8 | Controller endpoints + error codes + privacy tests | Postman-ready |
| 9 | `IRatingReportDataSource` for M15 (RPT-14/15/16) | Reporting |
| 10 | Wire M8 report intake, M9 notifications as they land | End-to-end |
| 11 | Optional projection repair job | Consistency tooling |

Suggested folders:

```
Binsera.Core/Ratings/
  Domain/Raah/Rating.cs
  Enums/RatingStatus.cs, RatingListScope.cs
  Models/Raah/RateContextDto.cs, RatingSubmittedDto.cs, RatingSummaryDto.cs,
               RatingListItemDto.cs, RatingListResponseDto.cs,
               RatingModerationItemDto.cs, HideRatingCommentRequest.cs
  Contracts/IRatingService.cs, IRatingQueryService.cs,
            IRatingSummaryGateway.cs, IConnectionRatingReader.cs,
            IRatingReadGateway.cs, IRatingModerationGateway.cs,
            IRatingReportDataSource.cs
Binsera.Application/Ratings/Services/
  RatingService.cs, RatingQueryService.cs, RatingSummaryService.cs,
  RatingModerationService.cs, RatingReportDataSource.cs
  Gateways/NoOpReportIntakeGateway.cs, NoOpNotificationGateway.cs (as needed)
Binsera.API/Controllers/Ratings/RatingController.cs
Binsera.Persistence/Configurations/Raah/RatingConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahRating.cs
```

---

## 16. Test plan

### 16.1 Unit

- Eligibility: Accepted now → can rate; Disconnected but was Accepted → can rate (SC-16.7); never accepted / lead only → `RATING_NOT_ALLOWED` (AC-16.1); blocked pair → refused; self → refused.
- One per connection: second submit → `ALREADY_RATED`; a new connection for the same pair allows a new rating (BR-16.2); concurrent double-submit yields one row (unique index).
- Stars: 0, 6, 3.5, null rejected; 1 and 5 accepted.
- Comment: empty allowed; at CFG-27 accepted; CFG-27+1 rejected; link (`http://`, `www.`, bare domain) rejected; 10-digit run rejected; a 9-digit run accepted.
- Config: CFG-13 off → `RATING_DISABLED`; CFG-27 shortened applies to new comments only; CFG-28 threshold flips `isNewMember`.
- Summary: average rounds to one decimal; Hidden rows still count in average and count (AC-16.4); count 2 with CFG-28=3 → `isNewMember` (AC-16.3).
- Moderation: hide requires reason ≥ 10 and CFG-14; hides comment only; stars/summary unchanged; second hide → `COMMENT_ALREADY_HIDDEN`; rater notified.
- Lists: received shows rater identity (BR-16.7) and hides hidden comment text; given shows own comment; paging has no gaps/overlap.
- Privacy: DTOs never contain email/mobile; public projection exposes no comment/rater.
- Report: reporter must be `ToMemberId` with a visible comment; hidden comment cannot be reported; no-op M8 → `REPORT_UNAVAILABLE`.

### 16.2 Integration / API

- Every endpoint requires auth; foreign `connectionId`/`ratingId` → 404 + logged; missing token → 401.
- Envelope present; `data` shape matches DTO.
- Migration applies cleanly and does not touch `member_Master`; M1 projection columns updated through the writer only.
- Submitting a rating updates the average in `GET /me/profile/preview` and in M4/M5 DTOs (with M1/M4/M5 fakes).
- Hiding a comment removes the text from SCR-16.2 but the average is unchanged.
- Notifications dispatched to the right party with no contact data (with M9 fake).
- RPT-14/15/16 aggregates correct; RPT-16 includes hidden comments for Admin only.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-16.1 Only connected members can rate | §7.2 step 2, §9, §16.1 |
| AC-16.2 One rating per connection | §4.1 unique index, §7.2 step 5, §16.1 |
| AC-16.3 Average display threshold | §7.3/§8.2 `isNewMember`, §16.1 |
| AC-16.4 Hidden comment keeps stars | §4.1, §7.2 step 8, §8.4/§8.5, §16.1 |
| AC-16.5 Rating privacy (comments not public) | §5.7, §13, §16.1 |

Scenarios SC-16.1…SC-16.8 map to the endpoints and rules above; SC-16.3 (rate twice) → `ALREADY_RATED`; SC-16.4 (never connected) → `RATING_NOT_ALLOWED`; SC-16.5 (New member) → CFG-28; SC-16.6 (abusive comment hidden) → moderation; SC-16.7 (after disconnect) → `wasEverAccepted`; SC-16.8 (phone in comment) → `COMMENT_CONTAINS_CONTACT`.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| — | Should a rating be allowed for a pair that is now Blocked but was Accepted before? | **No** — refuse when the pair is blocked (safety); revisit if product wants the literal "past accepted" wording |
| — | Can the rater still see their own comment after Admin hides it? | Yes, in **Given** with a "Comment hidden" note; the rated member sees "Comment hidden" only |
| — | Are ratings shown to other members after CFG-13 is turned off? | Yes, read-only; only new submissions are blocked |
| — | Page size (no CFG defined)? | Fixed default 20, clamp 50, keyset "Show more" |
| — | Is the projection recompute on hide needed? | Written defensively; stars never change, so the value is identical |
| — | Does report-comment create the report in M7 or M8? | M8 owns the record; M7 calls `IReportIntakeGateway` and returns the report number when available |
| — | Should the moderation queue live in M7 or M12? | M7 provides `IRatingModerationGateway`; M12 exposes the screen/endpoints |
| — | Rating number prefix/format | `RAT-{nnnnnn}` via `IRecordNumberGenerator` |
| — | Should the "New member" threshold ever be exposed to the member? | No; only the label and the count are shown |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Field spec (Rating) | §4.1, §5 |
| SCR-16.1 Rate Member | §5.1, §7.1, §7.2 |
| SCR-16.2 Ratings Received | §5.4, §7.3 |
| SCR-16.3 Ratings Given | §5.4, §7.4 |
| BR-16.1 only connected members | §7.2, §8.3, §9 |
| BR-16.2 one per connection | §4.1, §7.2 |
| BR-16.3 final, no edit/delete | §4.1, §7.2, §13 |
| BR-16.4 stars required, comment optional | §7.2, §9 |
| BR-16.5 visibility (rated + Admin only) | §5.7, §13, §14 |
| BR-16.6 CFG-28 threshold | §4.2, §8.2, §10 |
| BR-16.7 rater identity shown to rated member | §7.3 |
| BR-16.8 moderation, stars count | §8.5, §7.3, §13 |
| BR-16.9 report a comment | §7.5, §8 |
| BR-16.10 no effect on matching | §1, §13 |
| BR-16.11 never deleted; recalculated | §4.1, §4.2, §8.4 |
| Status lifecycle §9.8 | §4.3, §8.1 |
| CFG-13/14/27/28 | §10 |
| V-41 / V-42 | §9 |
| SC-16.1…SC-16.8 | §16 |
| AC-16.1…AC-16.5 | §16.3 |
| Data Impact | §7.2, §8.4, §8.5, §11 |
| RPT-14/15/16 | §14 |
| BR-17.12 rating comment report | §7.5, §8 |
