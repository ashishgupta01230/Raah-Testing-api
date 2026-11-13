# M10 — Member Management & Verification — API Plan

| | |
|---|---|
| **Module** | M10 — Member Management & Verification (spec `docs/spec/10_M10_Member_Management.md`, functional spec Section 19) |
| **Document type** | API implementation plan: admin contracts, read models, endpoints, action orchestration, validation, audit, export, sequencing, tests |
| **Build order** | #10 after M1–M9 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Companion** | UI plan: `10_M10_Member_Management_UI_Plan.md` |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M10 is the **Admin-only member control room**:

1. Find any member by name, email, mobile or `MBR-` number; filter by status, role, verification, flags and registration date (SCR-19.1).
2. Open one member and see profile, account, read-only routes, activity counts, safety summary and history (SCR-19.2).
3. Perform the seven account-level actions with a mandatory reason (SCR-19.3, SCR-19.4).
4. Export the member list with contact masking by default (BR-19.12).
5. Feed RPT-1, RPT-2 and RPT-25 from the same data.

Out of scope (owned elsewhere):

| Concern | Owner |
|---|---|
| Member profile fields and self-service edits | M1 |
| Route data and route deactivation mechanics | M2 / M11 |
| Leads withdrawal | M4 |
| Pending requests, connections, chats close | M5 / M6 |
| Rating values | M7 |
| Report history and safety counts (read only here) | M8 / M12 |
| Restrict / Temporary Block / Permanent Block | **M12 only** (BR-19.11) |
| Notification/email delivery | M9 |
| Admin account creation and management | Database only (D-19, BR-19.13) |
| Report catalogue UI | M15 |

**Hard non-goals:** Admin cannot edit name, email, mobile, role, route, leads, requests, connections, messages or ratings (BR-19.9); cannot delete a member (BR-19.10, V-48); cannot read chat content here (BR-4.7); never sees or sets a password (BR-5.8).

---

## 2. Module boundaries

M10 is a read/orchestration module. It owns no business data except audit entries; it composes other modules' gateways.

### 2.1 Contracts consumed

| Contract | Source | Methods used by M10 |
|---|---|---|
| `IMemberAdminIdentityService` | Foundation | get identity/status/verification/lock/failed-login/last-login/member-since; update name? **no**; set status; mark verified; unlock; correct gender |
| `IMemberSessionAdminService` | Foundation | `RevokeAllSessionsAsync(memberId)` |
| `IPasswordResetAdminService` | Foundation | `SendResetEmailAsync(memberId)` (never returns a token or password) |
| `IMemberProfileAdminReader` | M1 | photo, gender, journey role, vehicle, member number, member since |
| `IRouteAdminGateway` | M2 / M11 | active route summary; past routes; `DeactivateActiveRouteAsync(memberId, reason, actor)` |
| `ILeadAdminGateway` | M4 | counts; `WithdrawAllLeadsAsync(memberId, reason)` |
| `IPendingRequestAdminGateway` | M5 | counts; `WithdrawAllPendingAsync(memberId, reason)` |
| `IConnectionAdminGateway` | M5 | counts by status; `DisconnectAllAcceptedAsync(memberId, reason)` |
| `IChatAdminGateway` | M6 | count only; `CloseAllForMemberAsync(memberId, reason)`; **no message content method** |
| `IRatingAdminReader` | M7 | average and count |
| `ISafetyAdminReader` | M8 / M12 | reports against/raised counts and statuses; warnings, restrictions, blocks |
| `INotificationDispatcher` | M9 | optional direct notification intents (prefer events; see §8.3) |
| `IMediaService` | Foundation | remove profile photo |
| `IAuditService` | Foundation | append-only audit with reason, old/new |
| `IAppConfigReader` | Foundation | CFG-9, CFG-10, CFG-11, CFG-15, CFG-21 |

### 2.2 Contracts provided by M10

| Contract | Purpose | Consumer |
|---|---|---|
| `IMemberAdminQueryService` | member list, detail, login history, history, activity, safety, route views | Admin UI |
| `IMemberAccountActionService` | validate, preview and execute the seven actions | Admin UI |
| `IMemberExportService` | masked/confirmed contact export | Admin UI |

### 2.3 Rules

- M10 never writes another module's tables directly; it calls the gateway.
- Every action is authorised on the server per record (V-50): the target must exist, not be an Admin account, and the action must be allowed for the current status.
- Action execution is idempotent where possible; a retry after partial failure must not duplicate effects.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; `[Route("admin/members")]`; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interfaces in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` wrapped by `ResponseFilterAttribute` |
| Non-2xx | `BadRequest(new { code, message })`, `StatusCode(409, ...)`, `NotFound(...)`; **never** `Forbid()` / `Challenge()` (swallowed to 200) |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; migrations only when a new table is genuinely needed |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id` PK, audit columns, `IsDeleted` |
| Validation | No FluentValidation; validate inside the application service; unit-test the rules |
| Audit | Append-only, old/new + reason + actor + UTC + source IP (Section 8, BR-19.1) |
| Current user | `UserId`, `UserName`, `RoleId`, `MemberGraphId` from `ApiControllerBase` |
| Export | Return a `FileResult`; `ResponseFilterAttribute` leaves file results untouched |
| Admin gate | Enforce the Admin role in the controller; a plain reject uses `StatusCode(403)` |

**Route prefix.** Follow the existing no-`/api` convention: `/admin/members`.

---

## 4. Data model

### 4.1 No new business table (default)

M10 reads Foundation + M1–M8 and writes through their gateways. Do **not** duplicate member, route, lead, request, connection, chat or report data into an M10 table.

**Optional read model (open decision UD-API-1).** If the member list becomes slow at scale, add a `raah_MemberAdminSummary` projection (1:1 with Member) maintained by outbox handlers from M2/M4/M5/M6/M7/M8 events. Do not build it pre-emptively; measure first.

If the optional projection is approved, its fields would be:

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint unique | FK → `member_Master.Id` |
| `Status`, `JourneyRole`, `Vehicle` | tinyint | Mirrors current values |
| `EmailVerified`, `IsLocked`, `FailedLoginCount`, `LastLoginAt`, `RegisteredAt` | | Reconciliation fields |
| `HasActiveRoute` | bit | |
| `OpenReportCount`, `FlaggedByThreshold` | int / bit | CFG-15 |
| `OpenItemCount` | int | "Inactive but holding open items" flag |
| `LeadsCount`, `RequestsSent`, `RequestsReceived`, `ConnectionsAccepted`, `ConnectionsPending`, `ChatsCount` | int | |
| `RatingAverage`, `RatingCount` | decimal(2,1) / int | |
| audit columns + `IsDeleted` | | |

Indexes: unique `MemberId`, `(Status, RegisteredAt)`, `(LastLoginAt)`, `(FlaggedByThreshold, OpenReportCount)`.

### 4.2 Audit

No new audit table. Use the Foundation audit trail (Section 8) for every M10 action. History tab reads the same records filtered by `entity = Member`, `entityId = memberId`, action in the M10 set.

### 4.3 Enums

```
MemberAdminAction {
    Deactivate = 1,
    Reactivate = 2,
    Unlock = 3,
    MarkEmailVerified = 4,
    SendPasswordReset = 5,
    CorrectGender = 6,
    RemovePhoto = 7
}

MemberFlag {
    LockedAccount = 1,
    HasOpenReport = 2,
    FlaggedByThreshold = 3,
    InactiveWithOpenItems = 4
}

MemberListSort { RegisteredDesc = 1, RegisteredAsc = 2, LastLoginDesc = 3, NameAsc = 4, MemberNumberAsc = 5 }
```

Member status, journey role, vehicle and gender reuse the Foundation/M1 enum values; never redefine them in M10.

---

## 5. DTOs (Binsera.Core)

Suggested namespace: `Binsera.Core.Members.Models.Admin`.

### 5.1 Member list

```
MemberListQuery
  search              string?      // name, email, mobile, MBR-
  status              string[]?    // Unverified/Active/Inactive/Restricted/TemporarilyBlocked/PermanentlyBlocked
  role                string?      // Saathi | Raahi
  verification        string?      // Verified | Unverified
  flags               string[]?    // LockedAccount | HasOpenReport | FlaggedByThreshold | InactiveWithOpenItems
  registeredFrom      DateTime?
  registeredTo        DateTime?
  sort                string?      // default RegisteredDesc
  page                int          // default 1
  pageSize            int          // default 25, max 100

MemberListItemDto
  memberId            long
  memberNumber        string      // MBR-nnnnnn
  fullName            string
  photoUrl            string?
  email               string      // Admin may see it (BR-19.12); export masks by default
  mobileMasked        string?     // e.g. 98•••••210; full value only on detail
  journeyRole         string
  vehicle             string?
  status              string
  emailVerified       bool
  registeredOn        DateTime
  lastLoginAt         DateTime?
  hasActiveRoute      bool
  flags               string[]    // human-readable flag labels

MemberListPageDto
  items               MemberListItemDto[]
  page, pageSize, totalCount, totalPages
  appliedFilters      object
```

### 5.2 Member detail

```
MemberDetailDto
  memberId, memberNumber, fullName, photoUrl
  status, journeyRole, vehicle, gender, email, mobile, memberSince
  emailVerified, isLocked, failedLoginCount, lastLoginAt
  activeRoute         RouteSummaryDto?
  activity            ActivityCountsDto
  safety              SafetySummaryDto
  flags               string[]
  availableActions    AvailableActionDto[]
  capabilities        { canEditProfile: false, canDelete: false, canRestrictOrBlockHere: false }

ActivityCountsDto
  leads, requestsSent, requestsReceived,
  connectionsPending, connectionsAccepted, connectionsRejected,
  connectionsDisconnected, chats, ratingsCount, ratingAverage

SafetySummaryDto
  reportsAgainst, reportsAgainstOpen,
  reportsRaised, warnings, restrictions, temporaryBlocks, permanentBlocks
  // counts and statuses only; never report text or chat content

RouteSummaryDto
  routeNumber, startCity, endCity, status, journeyRole, vehicle,
  going: { points[], days[], departureTime }, return: ...
  // read-only, supplied by M11/M2

AvailableActionDto
  action              string      // enum name
  allowed             bool
  disabledReason      string?     // e.g. "Member is not locked."
  requiresNewGender   bool
  isDestructive       bool
```

### 5.3 History, login history and action context

```
LoginHistoryItemDto   { occurredAtUtc, ipMasked, device, result }   // RPT-25 extract; pageable
MemberHistoryItemDto  { occurredAtUtc, actorName, action, oldValue, newValue, reason }

AccountActionContextDto
  action                    string
  memberName                string
  currentStatus             string
  effects                   string[]   // ordered, plain sentences for the dialog
  counts                    {
                              sessions, activeRoutes, pendingRequests,
                              acceptedConnections, chats, leads
                            }
  requiresReason            true
  reasonMinLength           10
  requiresNewGender         bool
  oldGender                 string?
  photoUrl                  string?   // RemovePhoto only
  warning                  string?    // e.g. MarkEmailVerified caution
```

### 5.4 Requests and results

| DTO | Fields |
|---|---|
| `PerformMemberActionRequest` | `Action` (string, required), `Reason` (string, required, min 10 after trim, max 500), `NewGender` (string?, required for `CorrectGender`) |
| `MemberActionResultDto` | `memberId`, `action`, `status`, `effects` (counts actually applied), `performedAtUtc`, `message` |
| `MemberExportRequest` | same filters as `MemberListQuery` + `IncludeContactDetails` (bool, default false) + `ConfirmContactDetails` (bool, must be true when `IncludeContactDetails` is true) |
| `MemberFilterOptionsDto` | statuses, roles, verifications, flags, vehicle types (CFG-21), genders, `reasonMinLength=10`, `exportPageLimit`, `lockConfig` (CFG-9/10) |

---

## 6. Endpoint catalogue

Base route: `/admin/members`. Every endpoint requires an authenticated **Admin**; member tokens get `403`.

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/admin/members` | List with search, filters, sort, pager | SCR-19.1 | 200 `MemberListPageDto` |
| 2 | GET | `/admin/members/options` | Filter values and limits | SCR-19.1 | 200 `MemberFilterOptionsDto` |
| 3 | GET | `/admin/members/{id}` | Full member detail | SCR-19.2 | 200 `MemberDetailDto` |
| 4 | GET | `/admin/members/{id}/routes` | Read-only current and past routes | SCR-19.2 Route tab | 200 `RouteSummaryDto[]` |
| 5 | GET | `/admin/members/{id}/activity` | Activity counts | SCR-19.2 Activity tab | 200 `ActivityCountsDto` |
| 6 | GET | `/admin/members/{id}/safety` | Safety counts/statuses | SCR-19.2 Safety tab | 200 `SafetySummaryDto` |
| 7 | GET | `/admin/members/{id}/login-history` | Login audit extract (paged) | SCR-19.2 Account tab | 200 page of `LoginHistoryItemDto` |
| 8 | GET | `/admin/members/{id}/history` | Account-action history (paged) | SCR-19.2 History tab | 200 page of `MemberHistoryItemDto` |
| 9 | GET | `/admin/members/{id}/actions/context?action=` | Effects and counts for the confirmation dialog | SCR-19.3 / 19.4 | 200 `AccountActionContextDto` |
| 10 | POST | `/admin/members/{id}/actions` | Execute one account action with reason | SCR-19.3 / 19.4 | 200 `MemberActionResultDto` |
| 11 | POST | `/admin/members/export` | Export list (masked by default) | SCR-19.1 Export | 200 file (CSV) |
| 12 | GET | `/admin/members/{id}/contact` | Explicit reveal of mobile/email (optional; audit) | SCR-19.2 | 200 `{ email, mobile }` |

Notes:
- Endpoints 4–8 exist so the detail tabs load independently and stay fast; the detail endpoint may return cached summaries for the header.
- There is **no** `PUT`/`PATCH`/`DELETE` member endpoint. There is no bulk-action endpoint. There is no endpoint for name, email, mobile, role, route, lead, message or rating edits.
- Restrict/Block endpoints live in M12 and are not reachable from this controller.

---

## 7. Endpoint details

### 7.1 `GET /admin/members`

- Always exclude Admin accounts (BR-19.13): `member role != Admin`.
- `search` is a single case-insensitive contains over name, email, mobile and `MBR-` number. Trim; blank is ignored.
- Filters combine with AND; arrays within a filter use OR.
- Flags:
  - `LockedAccount`: lock flag or `LockedUntil > now`.
  - `HasOpenReport`: at least one report not Closed/Dismissed (M8/M12).
  - `FlaggedByThreshold`: distinct reporters against the member ≥ CFG-15.
  - `InactiveWithOpenItems`: status Inactive with an Active route, Pending request or Accepted connection (should normally be zero after a correct deactivation; used to find anomalies).
- Sort whitelist only (`MemberListSort`); unknown sort → `400 SORT_INVALID`.
- Numbered pager: `page` 1-based, `pageSize` 25 default / 100 max.
- Return `email` as stored; `mobileMasked` only. The full mobile is available on the detail endpoint.
- No mutation, no side effects.

### 7.2 `GET /admin/members/options`

Returns finite lists for the filter bar and the reason minimum. Values come from enums and config, not free text. Authenticated Admin only (prevents anonymous scraping of platform settings).

### 7.3 `GET /admin/members/{id}`

- `404 MEMBER_NOT_FOUND` when the id does not exist **or is an Admin account** (do not reveal Admin existence).
- Compose profile (M1), account (Foundation), active route summary (M2/M11), activity counts (M4–M7), safety counts (M8/M12), flags and `availableActions`.
- `availableActions` is computed from status/verification/lock/photo:
  - Deactivate: allowed when status is Active or Restricted.
  - Reactivate: allowed only when status is Inactive.
  - Unlock: allowed only when locked.
  - MarkEmailVerified: allowed only when Unverified.
  - SendPasswordReset: allowed for any non-Blocked member account.
  - CorrectGender: always allowed for a member.
  - RemovePhoto: allowed only when a photo exists.
- Never return chat content, password, tokens, admin notes or another member's data.
- Full `mobile` and `email` are returned here for the Admin (AC-19.5); the UI masks mobile until "Show".

### 7.4 `GET /admin/members/{id}/routes`

- Read-only: current Active route (both legs) plus past routes, newest first.
- Delegates to `IRouteAdminGateway`; never edits. Route deactivation is an M11 action, not M10.
- Return `[]` when none.

### 7.5 Activity, safety, login history, history

- `activity`: counts only; no chat text (SC-19.9).
- `safety`: report counts/statuses, warnings, restrictions and blocks; never report descriptions here (M12 detail owns those).
- `login-history`: newest first, paged; mask IP where the Foundation convention masks it; include device and result. This is an RPT-25 extract, not the full export.
- `history`: newest first, paged; source is the audit trail filtered to M10 actions; each row has actor, action, old/new, reason, timestamp.

### 7.6 `GET /admin/members/{id}/actions/context?action=`

- Validate the action name → `400 ACTION_INVALID`.
- 404 for missing/Admin target.
- Return the ordered `effects` strings and the live `counts` used by the dialog. Build the text from real counts so the Admin sees exactly what will happen (UI Plan §7.3).
- For `Deactivate`, effects in this order:
  1. "End the member's open sessions."
  2. "Switch off the Active route." (+ route number when one exists)
  3. "Withdraw pending requests sent and received."
  4. "Disconnect Accepted connections."
  5. "Close their chats."
  6. "Withdraw leads both ways."
  7. "Set the account to Inactive."
- For `Reactivate`: "The member can log in again. Their route stays Inactive until they switch it on. Closed connections stay closed. A reactivation email is sent." (BR-19.3)
- For `MarkEmailVerified`, include the caution: "Use this only when the verification email could not reach the member." (BR-19.5)
- For `SendPasswordReset`, include: "We will send the standard password reset email. You will not see or set the password." (BR-19.6)
- For `RemovePhoto`, include `photoUrl` so the dialog can show the current photo (BR-19.8).

### 7.7 `POST /admin/members/{id}/actions`

Request: `{ action, reason, newGender? }`.

Common rules (AC-19.1):

1. Admin authorisation and target validation (not Admin, exists).
2. `reason`: trim; after trim ≥ 10 characters (V-45) and ≤ 500; else `400 REASON_REQUIRED`. The same reason is written to audit.
3. Never hard-delete anything (V-48).
4. Execute only if the action is allowed for the current state; otherwise `409 ACTION_NOT_ALLOWED` with the current status.
5. Audit **before/after** the action with old/new values, actor, timestamp and reason.
6. Return the action result and the fresh status.
7. Wrap the multi-step deactivation in a compensating sequence (§8) and return the applied effect counts.

#### Action rules

| Action | Precondition | Effect | Notification |
|---|---|---|---|
| **Deactivate** | status Active or Restricted | Full orchestration in §8.1; status → Inactive | No dedicated "deactivated" notice in the matrix; see UD-API-5. Route-deactivated/status notices use M9 templates where applicable |
| **Reactivate** | status Inactive | status → Active; route stays Inactive; connections stay closed | Email `AccountReactivatedByAdmin` (M9) |
| **Unlock** | locked (flag or `LockedUntil > now`) | clear lock and failed-login counter | None |
| **MarkEmailVerified** | status Unverified | status → Active; verification = verified; recorded as if the link was opened (BR-19.5) | In-app welcome may fire via M9 (`MemberWelcome`) |
| **SendPasswordReset** | member not Blocked | trigger Foundation reset email | Email `PasswordReset` (Foundation/M9 layout) |
| **CorrectGender** | member exists | `newGender` required and one of Male/Female/Other; set gender; audit old→new | None |
| **RemovePhoto** | a photo exists | remove via `IMediaService`; profile falls back to initials | In-app `ProfilePhotoRemovedByAdmin` (M9) |

- `SendPasswordReset` never returns a token or password and never sets one (BR-5.8).
- `CorrectGender` applies to filters from the next search (BR-19.7); no cache invalidation beyond normal read.
- `RemovePhoto` keeps the media record soft-deleted; the member is notified (BR-19.8).
- Actions that belong to M12 (`Restrict`, `TemporaryBlock`, `PermanentBlock`) are not in the enum; sending them returns `400 ACTION_NOT_SUPPORTED` and the UI never offers them (BR-19.11).

### 7.8 `POST /admin/members/export`

- Accepts the same filters as the list plus `IncludeContactDetails` and `ConfirmContactDetails`.
- `IncludeContactDetails = false` (default): email and mobile columns are masked in the file (`a•••@mail.com`, `98•••••210`), matching BR-19.12/SC-19.8.
- `true` requires `ConfirmContactDetails = true` → else `400 EXPORT_CONFIRMATION_REQUIRED`.
- Audit `MEMBER_LIST_EXPORTED` with filter summary, row count and the include-contact flag. When contact details are included, the confirmation is part of the audit.
- Return a `FileResult` (CSV, UTF-8, no BOM issues in Excel). Cap rows at the configured export limit; if exceeded, return `400 EXPORT_TOO_LARGE` with the limit, or stream — UD-API-3.
- Columns: MBR no., name, email, mobile, role, vehicle, status, verification, registered on, last login, active route, flags.
- No bulk actions are implied by export.

### 7.9 `GET /admin/members/{id}/contact` (optional)

- Only if product wants a separate audit event for revealing contact details. Returns `{ email, mobile }` and writes `MEMBER_CONTACT_VIEWED`.
- Default plan: the detail endpoint returns the values and the UI masks them; no separate call. Keep this endpoint as an open decision (UD-API-4).

---

## 8. Orchestration and failure handling

### 8.1 Deactivation sequence (BR-19.2, AC-19.2)

Execute in this order and record each applied effect:

1. Validate reason and status.
2. `IMemberSessionAdminService.RevokeAllSessionsAsync` — end open sessions.
3. `IRouteAdminGateway.DeactivateActiveRouteAsync` — route Inactive (this withdraws its leads and pending requests per M2/M5).
4. `IPendingRequestAdminGateway.WithdrawAllPendingAsync` — any remaining sent and received Pending requests.
5. `IConnectionAdminGateway.DisconnectAllAcceptedAsync` — Accepted → Disconnected.
6. `IChatAdminGateway.CloseAllForMemberAsync` — close chats.
7. `ILeadAdminGateway.WithdrawAllLeadsAsync` — withdraw leads both ways.
8. `IMemberAdminIdentityService.SetStatusAsync(memberId, Inactive, reason)`.
9. Audit `MEMBER_DEACTIVATED` with counts.
10. Dispatch any notifications the matrix requires (M9).

Failure isolation:
- Steps are idempotent so a retry is safe.
- If a step fails, stop, keep the member Active **unless** step 8 succeeded, and return `500 MEMBER_DEACTIVATION_INCOMPLETE` with the steps already applied.
- If step 8 fails after side effects, return `500` with the applied counts; the Admin can retry; the audit records the attempt and the partial outcome.
- Never report success unless the member status is Inactive.

### 8.2 Other actions

- `Reactivate`: set Active, then dispatch email. If the email queue fails, the status change still stands; the email is retried by M9.
- `Unlock`: single write; no cross-module effect.
- `MarkEmailVerified`: set Active + verified in one transaction, then dispatch welcome (best-effort).
- `SendPasswordReset`: Foundation call is fire-and-forget from the Admin's view; the audit records the request, not a password.
- `CorrectGender` and `RemovePhoto`: single-module writes plus audit; `RemovePhoto` also dispatches the member notice.

### 8.3 Notifications

Prefer M9's event/dispatcher path. M10 does not build its own email copy:

| Action | M9 event/template |
|---|---|
| Reactivate | `AccountReactivatedByAdmin` (email) |
| RemovePhoto | `ProfilePhotoRemovedByAdmin` (in-app) |
| Deactivate | No matrix template for "admin deactivated"; do not invent one (UD-API-5). Route/status effects may already notify where the matrix says so |
| MarkEmailVerified | `MemberWelcome` in-app (optional) |
| SendPasswordReset | Foundation reset email |

### 8.4 Concurrency

- Re-read the member status inside the transaction before acting; return `409 ACTION_NOT_ALLOWED` if another Admin changed it.
- Two Admins acting at once: the second action must see the new status and fail cleanly, not repeat side effects.
- The reason is never optional or defaulted, including on retries.

---

## 9. Authorization and gates

| Caller | Allowed |
|---|---|
| Anonymous | Nothing |
| Member | Nothing (403 on every `/admin/members` route) |
| Restricted member | Nothing (Admin routes only) |
| Admin | All M10 endpoints |
| Admin account as a target | 404 (not listed, not changeable — BR-19.13) |
| Inactive/Blocked Admin | Rejected by Foundation session/status validation |

- Enforce `RoleId = Admin` in the controller/action; use `StatusCode(403)` for a plain reject.
- Never accept a target member id from a body for an action other than the route `{id}`.
- Log denied attempts per BR-4.2 (Foundation convention).

### 9.1 Status transitions owned by M10

| From | To | Action | Condition |
|---|---|---|---|
| Unverified | Active | MarkEmailVerified | reason present (BR-19.5) |
| Active / Restricted | Inactive | Deactivate | reason present (BR-19.2) |
| Inactive | Active | Reactivate | reason present (BR-19.3) |

Restricted and Blocked transitions belong to M12 and are never offered here (BR-19.11).

---

## 10. Validation matrix

| ID | Rule | Where | Error |
|---|---|---|---|
| V-45 | Every action needs a reason ≥ 10 characters | Action POST, export confirm | `REASON_REQUIRED` |
| V-48 | No hard delete of any record | Whole module | no delete endpoint |
| V-50 | Server-side authorisation per record | Every endpoint | `FORBIDDEN` / 404 |
| BR-19.9 | No member field edit | No PUT/PATCH | 405 / no route |
| BR-19.10 | No delete | No DELETE | 405 / no route |
| BR-19.11 | Restrict/Block not here | Action enum guard | `ACTION_NOT_SUPPORTED` |
| BR-19.12 | Export masks contact by default | Export | masked file; `EXPORT_CONFIRMATION_REQUIRED` when unmasked without confirmation |
| BR-19.13 | Admin accounts excluded | List/detail | excluded / `MEMBER_NOT_FOUND` |
| — | Action allowed for current status | Actions | `ACTION_NOT_ALLOWED` |
| — | `NewGender` valid for CorrectGender | Actions | `GENDER_INVALID` |
| — | Sort value whitelisted | List | `SORT_INVALID` |
| — | Page size 1–100 | List | `PAGE_SIZE_INVALID` |
| — | Reason ≤ 500 characters | Actions | `REASON_TOO_LONG` |

No validation framework exists in the repo; implement and unit-test these in the application services.

---

## 11. Audit events (Section 8, BR-19.1)

| Event | Trigger | Recorded |
|---|---|---|
| `MEMBER_DEACTIVATED` | Deactivate | old status → Inactive; effects counts; reason |
| `MEMBER_REACTIVATED` | Reactivate | Inactive → Active; reason |
| `MEMBER_UNLOCKED` | Unlock | lock/failed-count old → new; reason |
| `MEMBER_EMAIL_MARKED_VERIFIED` | MarkEmailVerified | Unverified → Active; reason |
| `MEMBER_PASSWORD_RESET_SENT` | SendPasswordReset | request only; no token/password; reason |
| `MEMBER_GENDER_CORRECTED` | CorrectGender | old gender → new; reason |
| `MEMBER_PHOTO_REMOVED` | RemovePhoto | media id → null; reason |
| `MEMBER_LIST_EXPORTED` | Export | filter summary, row count, include-contact flag |
| `MEMBER_CONTACT_VIEWED` | Contact reveal (optional) | member id, actor |

Every row includes actor, actor role, UTC timestamp, reason and old/new where applicable. Audit is append-only. Never write passwords, reset tokens, chat text or full contact lists into logs.

---

## 12. Config usage

| CFG | Use in M10 |
|---|---|
| CFG-9 | Failed-login threshold shown in Account tab and used by the lock flag |
| CFG-10 | Lock duration shown in Account tab |
| CFG-11 | Reset link validity shown in the SendPasswordReset dialog context |
| CFG-15 | "Flagged by threshold" filter and flag |
| CFG-21 | Vehicle options shown on the detail |
| CFG-24 | Photo guidance if the Admin removes a photo (display only) |

Read config through the Foundation reader; do not hard-code defaults.

---

## 13. Error responses

Use `BadRequest(new { code, message })`, `StatusCode(409, ...)` via object results, or `NotFound`. Never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `MEMBER_NOT_FOUND` | 404 | "Member not found." |
| `REASON_REQUIRED` | 400 | "Enter a reason of at least 10 characters." |
| `REASON_TOO_LONG` | 400 | "Keep the reason under 500 characters." |
| `ACTION_INVALID` | 400 | "Choose a valid action." |
| `ACTION_NOT_SUPPORTED` | 400 | "This action is handled in the Safety module." |
| `ACTION_NOT_ALLOWED` | 409 | "This action is not available for the member's current status." |
| `GENDER_INVALID` | 400 | "Choose Male, Female or Other." |
| `SORT_INVALID` | 400 | "Choose a valid sort order." |
| `PAGE_SIZE_INVALID` | 400 | "Page size must be between 1 and 100." |
| `EXPORT_CONFIRMATION_REQUIRED` | 400 | "Confirm that you want to include contact details." |
| `EXPORT_TOO_LARGE` | 400 | "The export is too large. Narrow the filters." |
| `MEMBER_DEACTIVATION_INCOMPLETE` | 500 | "Deactivation did not finish. Review the applied steps and try again." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

Keep wording aligned with the UI plan's microcopy.

---

## 14. Privacy and security

- **Admin-only surface.** Every route checks the Admin role server-side; denial is logged (BR-4.2).
- **Contact details.** Full mobile/email are visible only to Admin (D-21, AC-19.5). Export masks by default; unmasked export requires explicit confirmation and is audited (BR-19.12).
- **No chat content.** M10 has no method to read messages; only counts (BR-4.7, SC-19.9).
- **No passwords.** Send-reset never returns or stores a token/password (BR-5.8).
- **No member edits.** There is no endpoint for name/email/mobile/role/route/lead/message/rating changes (BR-19.9).
- **No Admin self-management through the app.** Admin accounts are excluded from lists and 404 on detail (BR-19.13).
- **Reason everywhere.** Every action writes the reason to the append-only audit (BR-19.1).
- **Export safety.** CSV injection guard: prefix cells beginning with `=`, `+`, `-`, `@` with an apostrophe in the file.
- **Logging.** Never log passwords, reset tokens, chat text or full contact lists.
- **Rate-limit** password-reset triggers per Admin/member to prevent mail abuse; use Foundation's existing cooldown where it exists.

---

## 15. Build sequence (M10)

| Step | Work | Output |
|---|---|---|
| 1 | Add enums, DTOs, query/action contracts | Compiles |
| 2 | `MemberAdminQueryService` list query with filters/flags/sort/pager | SCR-19.1 data |
| 3 | Detail composition (profile, account, route, activity, safety, actions) | SCR-19.2 data |
| 4 | Login-history and history readers (RPT-25 extract + audit) | Tabs ready |
| 5 | `MemberAccountActionService` + action context builder | Dialogs can render |
| 6 | Deactivation orchestration with gateways and failure isolation | AC-19.2 |
| 7 | Reactivate, unlock, mark-verified, send-reset, correct-gender, remove-photo | SCR-19.3/19.4 |
| 8 | Audit calls with reason/old/new for every action | BR-19.1 verified |
| 9 | Export with masking, confirmation and audit | SC-19.8 |
| 10 | Controllers + Admin role gate + error codes | Postman-ready |
| 11 | Wire M9 notifications (reactivate email, photo removed, welcome) | Matrix respected |
| 12 | Optional read-model projection only if list performance requires it | UD-API-1 |

Suggested folders:

```
Binsera.Core/Members/Models/Admin/*Dto.cs, *Request.cs
Binsera.Core/Members/Enums/MemberAdminAction.cs, MemberFlag.cs, MemberListSort.cs
Binsera.Core/Members/Contracts/Admin/IMemberAdminQueryService.cs,
                                  IMemberAccountActionService.cs,
                                  IMemberExportService.cs
Binsera.Core/Members/Contracts/Gateways/IMemberAdminIdentityService.cs,
                                  IMemberSessionAdminService.cs,
                                  IPasswordResetAdminService.cs,
                                  IRouteAdminGateway.cs, ILeadAdminGateway.cs,
                                  IPendingRequestAdminGateway.cs, IConnectionAdminGateway.cs,
                                  IChatAdminGateway.cs, IRatingAdminReader.cs,
                                  ISafetyAdminReader.cs, IMemberProfileAdminReader.cs
Binsera.Application/Members/Services/Admin/MemberAdminQueryService.cs,
                                           MemberAccountActionService.cs,
                                           MemberExportService.cs
Binsera.API/Controllers/Admin/MembersAdminController.cs
Binsera.Persistence/Configurations/Raah/MemberAdminSummaryConfiguration.cs   // only if UD-API-1 approved
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahMemberAdminSummary.cs    // only if approved
```

---

## 16. Test plan

### 16.1 Unit

- Filters: each status, role, verification, flag and date range; search over name/email/mobile/MBR; combined AND/OR; filtered-empty.
- Flags: locked, open report, threshold (CFG-15 boundary), inactive-with-open-items.
- Sort whitelist and paging boundaries (page 0, > last, size 1/100/101).
- Detail: Admin target → not found; member target → full data; no chat/password/token fields.
- Available actions computed correctly for each status/lock/verification/photo combination.
- Reason: 9 vs 10 vs 500 vs 501 characters; whitespace only; trim.
- Action preconditions for every action, including blocked/restricted states.
- Deactivate orchestration order, effect counts, retry idempotency, partial-failure reporting.
- Reactivate does not reactivate the route or reopen connections.
- MarkEmailVerified only from Unverified; sends welcome best-effort.
- SendPasswordReset never returns a secret.
- CorrectGender old→new audit; invalid gender rejected.
- RemovePhoto only when a photo exists; falls back to initials.
- Export masking, confirmation rule, audit flag, CSV injection guard, row cap.
- Admin accounts excluded from list and detail.

### 16.2 Integration / API

- Missing token → 401; member token → 403; Admin token → 200.
- Envelope shape matches DTOs; file export is a `FileResult` unaffected by the response envelope.
- Audit row per action with reason, actor and old/new.
- No endpoint accepts name/email/mobile/role/route/message/rating edits, and no delete route exists.
- Deactivation integration: route Inactive, request Withdrawn, connection Disconnected, chat Closed, leads Withdrawn, user cannot log in.
- Reactivation integration: user can log in; route still Inactive; email queued.
- Export with include-contact details writes the confirmation to audit.
- Migration (if any) does not touch `member_Master`.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-19.1 Reason mandatory | §7.7 common rules; §10 V-45; §16.1 reason tests |
| AC-19.2 Deactivation effects | §8.1; §16.2 integration test |
| AC-19.3 Reactivation does not revive route | §7.7 Reactivate; §8.2; §16.1 |
| AC-19.4 No member edit | §6 notes; §9; §14; §16.2 |
| AC-19.5 Mobile visible to Admin only | §5.2, §7.3, §14; M1 public DTO remains private |

Scenarios SC-19.1…SC-19.9 map to §7 endpoints and §8 orchestration; SC-19.9 (no chat content) is enforced by the absence of any message-read contract in §2.1.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| UD-API-1 | Add a member admin summary projection for list performance? | No; query source modules directly, measure first |
| UD-API-2 | Single `POST .../actions` with a discriminator vs one endpoint per action | Single endpoint, matching the spec's Action field and the shared dialog |
| UD-API-3 | Export format and row cap | CSV; cap from config; `EXPORT_TOO_LARGE` when exceeded |
| UD-API-4 | Should revealing mobile/email be a separate audited call? | No; detail returns values, UI masks mobile; add `/contact` only if product wants the extra audit |
| UD-API-5 | Should Admin deactivation send a member email? | No dedicated template in Section 18.4; do not invent one; revisit with product |
| UD-API-6 | Can an Admin deactivate another Admin? | No; Admin accounts are excluded (BR-19.13) |
| UD-API-7 | Show full mobile in the list? | No; list has masked mobile, detail has full |
| UD-API-8 | Login-history IP masking | Mask by default, reusing the Foundation audit display convention |
| UD-API-9 | Reason maximum length | 500 characters (spec only sets a minimum) |
| UD-API-10 | Unmasked export confirmation UI vs API flag | Both: `ConfirmContactDetails` is required by the API and the UI shows a confirmation |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Purpose / Admin only | §1, §9 |
| SCR-19.1 Member list | §6, §7.1, §7.2 |
| SCR-19.2 Member detail + tabs | §6, §7.3–7.5 |
| SCR-19.3 Change status | §7.6, §7.7 |
| SCR-19.4 Account actions | §7.6, §7.7 |
| Member list filters/columns | §5.1, §7.1 |
| Member detail blocks | §5.2, §7.3–7.5 |
| Account action field spec | §5.4, §7.7 |
| BR-19.1 reason + audit | §7.7, §11 |
| BR-19.2 deactivate | §8.1 |
| BR-19.3 reactivate | §7.7, §8.2 |
| BR-19.4 unlock | §7.7 |
| BR-19.5 mark verified | §7.7, §8.2 |
| BR-19.6 password reset | §7.7, §14 |
| BR-19.7 correct gender | §7.7 |
| BR-19.8 remove photo | §7.7 |
| BR-19.9 no member edit | §1, §6, §9 |
| BR-19.10 no delete | §1, §6, §10 |
| BR-19.11 restrict/block in M12 | §1, §7.7, §9.1 |
| BR-19.12 contact masking/export | §7.8, §14 |
| BR-19.13 Admin accounts excluded | §7.1, §7.3, §9 |
| Status lifecycle | §9.1 |
| SC-19.1…19.9 | §7, §8, §16.3 |
| AC-19.1…19.5 | §16.3 |
| Data Impact | §8, §11 |
| Reports RPT-1, RPT-2, RPT-25 | §1, §7.5, §11 |
| CFG-9/10/11/15/21/24 | §12 |
| V-45/V-48/V-50 | §10 |
| D-19/D-21/A-22/A-49/A-54 | §1, §7, §9, §14 |
| M11 route viewing, M12 safety | §1, §2.1, §7.4 |
