# M9 — Notifications — API Plan

| | |
|---|---|
| **Module** | M9 — Notifications (spec `docs/spec/09_M9_Notifications.md`, functional spec Section 18) |
| **Document type** | API implementation plan: event contracts, data model, endpoints, template rules, jobs, validation, privacy, sequencing, tests |
| **Build order** | #9 after M4–M8 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope, existing `OutboxPublisher` + `IExternalEventHandler<T>` |
| **Companion** | UI plan: `09_M9_Notifications_UI_Plan.md` |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M9 is the **notification engine** for every Member and Admin:

1. Turn business events from M4–M8 and Admin actions from M10–M13 into notification records for the right recipient.
2. Render and store **one-line, private-safe** in-app notifications with a link only.
3. Queue, retry and send **emails** per the matrix (immediate, daily digest, reminders, conditional chat email).
4. Serve the Notification Centre and Notification Preferences screens (SCR-18.1, SCR-18.2).
5. Archive notifications after 90 days (never delete) and expose delivery data for **RPT-23**.

Out of scope (owned elsewhere, consumed here):

| Concern | Owner |
|---|---|
| Registration / login / email verification send / password reset send | Foundation (Section 5) — M9 only reuses the email layout |
| Lead, request, connection, chat, rating, report state machines | M3–M8 (M9 never changes business state) |
| Admin actions (warning, restriction, block, deactivate, reactivate) | M10, M11, M12 |
| Legal text versions and publish/re-acceptance trigger | M13 |
| Config values CFG-16, CFG-29, CFG-30, CFG-31 and CFG-5 | Foundation / M13 |
| Report catalogue UI | M15 (reads M9 tables) |
| SMS channel | Not Phase 1; framework reserved (D-8) |

**Non-goal (BR-18.1):** no business process may depend on a notification. Leads, requests, chats and reports are always the authoritative views.

---

## 2. Architecture and module boundaries

### 2.1 Existing infrastructure to reuse

The repo already has a transactional outbox:

- `OutboxMessage` (`Binsera.Core/OutBox/Domain/OutboxMessage.cs`) with `MessageType`, `Payload`, `Attempts`, `Processed`, `DeadLetter`, `Error`.
- `OutboxPublisher` (`Binsera.API/BackgroundServices/OutboxPublisher.cs`) polls every 5 s, deserialises the payload to the type named by `MessageType`, resolves all `IExternalEventHandler<T>` from DI and calls `HandleAsync`.

M9 uses this instead of a new event bus:

1. Source modules enqueue an `OutboxMessage` **inside the same transaction** as the business write (`MessageType = typeof(TEvent).AssemblyQualifiedName`, JSON payload matching the publisher's camelCase settings).
2. M9 handlers implement `IExternalEventHandler<TEvent>` and are discovered by DI via `[AutoRegister(includeInterfaces: true)]`.
3. A handler maps the event to a **`NotificationIntent`** and calls the M9 dispatcher. Handlers must be idempotent because the outbox retries.

### 2.2 Contracts provided by M9

| Contract | Shape | Used by |
|---|---|---|
| `INotificationDispatcher` | `DispatchAsync(NotificationIntent intent, CancellationToken)` | M9 handlers, M10–M13 for direct Admin/system actions |
| `NotificationIntent` | recipient, category, event type, source entity, safe template data, link, dedup key, channels | producers |
| `IEmailOutboxQueue` | `EnqueueAsync(EmailMessage message, CancellationToken)` | M9 internals, Foundation if it adopts the shared email layout |
| `IUnreadCountReader` | `GetUnreadCountAsync(memberId, recipientKind, CancellationToken)` | Shell / Admin shell bell |
| `IPublicDisplayNameResolver` | `GetDisplayNameAsync(memberId, CancellationToken)` | M9 handlers needing a name for wording |

### 2.3 Contracts consumed by M9

Because M9 is build #9, it may reference M4–M8 directly. Keep the surface narrow:

| Contract | Purpose |
|---|---|
| `IPendingRequestReminderSource` | Pending requests due for a CFG-29 reminder (request id, receiver id, sender display name, expires at) |
| `IUnreadChatSource` | Conversations with an unread message older than CFG-30 hours (connection id, recipient id, sender display name, latest message id) |
| `IChatReadStateWriter` | Reset the "one email per conversation" state when the recipient opens the chat |
| `INewLeadDigestSource` | New-lead notifications since the member's last digest, for the CFG-31 daily digest |
| `IRecipientEligibilityReader` | Member status/role and Admin recipients for BR-18.10 |
| `IAppConfigReader` | CFG-16, CFG-29, CFG-30, CFG-31, CFG-5 |
| `IAuditService` | Preference changes, delivery failures, template suppression |
| `IEmailTransport` | Actual SMTP/API send (provider decided in open decision UD-API-1) |

**Rule:** M9 only *reads* other modules' data. It never mutates leads, requests, connections, chats or reports.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; route-only class attributes (`[Route("me/notifications")]`, `[Route("admin/notifications")]`); services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interfaces in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)`; global `ResponseFilterAttribute` wraps into `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | `BadRequest(new { code, message })`, `StatusCode(409, ...)`; **never** `Forbid()` / `Challenge()` |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new normal EF tables; never edit `member_Master` |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id` PK, audit columns, `IsDeleted`, explicit decimal precision, per-column indexes |
| Validation | No FluentValidation in the repo; validate inside the application service and unit-test it |
| Background jobs | `BackgroundService`; cron-driven jobs use `Cronos` with an `IConfiguration` section (`Enabled`/`Cron`/`TimeZone`); register in `Program.cs` with `AddHostedService<>()` |
| Current user | `UserId`, `UserName`, `RoleId`, `MemberGraphId` from `ApiControllerBase` |

**Naming note.** Routes follow the existing no-`/api` prefix convention used by `users`, `media`, `auth`, and the M1 plan's `me/profile`.

---

## 4. Data model

### 4.1 New tables

#### `raah_Notification` (in-app inbox, Member and Admin)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `RecipientMemberId` | bigint | FK → `member_Master.Id`; Admin recipients are member accounts with the Admin role |
| `RecipientKind` | tinyint | 1 = Member, 2 = Admin |
| `Category` | tinyint | See `NotificationCategory` |
| `EventType` | int | See `NotificationEventType` |
| `SourceEntityType` | nvarchar(40) null | e.g. `ConnectionRequest`, `ChatMessage`, `SafetyReport` |
| `SourceEntityId` | bigint null | For drill-back and support; never rendered to the recipient |
| `Title` | nvarchar(160) | **Safe rendered** one-line sentence (no chat text, no contact details) |
| `Body` | nvarchar(500) null | Optional second line, same privacy rules |
| `LinkUrl` | nvarchar(300) null | Internal relative route only (allow-list, see §11) |
| `IsRead` | bit not null default 0 | |
| `ReadAtUtc` | datetime2 null | |
| `ArchivedAtUtc` | datetime2 null | Set after 90 days; never hard-deleted (BR-18.8) |
| `DedupKey` | nvarchar(200) | Uniqueness guard for outbox retries |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always 0 in practice; archive is a flag, not a delete |

Indexes:
- Unique `DedupKey`.
- `(RecipientMemberId, RecipientKind, IsRead, CreatedDate)` for the bell count and unread list.
- `(RecipientMemberId, RecipientKind, Category, CreatedDate)` for filters.
- `(ArchivedAtUtc)` for the archiver.

#### `raah_NotificationPreference` (1:1 with Member)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint | FK → `member_Master.Id`, **unique** |
| `LeadsDigestEmail` | bit not null default 1 | Optional (BR-18.4) |
| `UnreadChatEmail` | bit not null default 1 | Optional (BR-18.4) |
| audit columns + `IsDeleted` | | |

Only the two optional switches are stored. Mandatory emails are not configurable and therefore not represented.

#### `raah_EmailOutbox` (delivery queue and RPT-23 source)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint null | Recipient member when known |
| `RecipientEmail` | nvarchar(256) | Resolved at enqueue time from Foundation |
| `TemplateKey` | nvarchar(60) | See §8 |
| `Category` | tinyint | Reuses `NotificationCategory` |
| `IsMandatory` | bit | Cannot be suppressed by preferences (BR-18.4) |
| `DataJson` | nvarchar(max) | **Allow-listed safe fields only** (name, count, link); never chat text or contact details |
| `Status` | tinyint | 1 Pending, 2 Sending, 3 Sent, 4 Failed, 5 Cancelled |
| `Attempts` | int not null default 0 | |
| `NextAttemptAtUtc` | datetime2 | Backoff |
| `SentAtUtc` | datetime2 null | |
| `LastError` | nvarchar(1000) null | Truncated; never contains message bodies or tokens |
| `NotificationId` | bigint null | FK → `raah_Notification` when the email accompanies an in-app row |
| `DedupKey` | nvarchar(200) | Unique; prevents duplicate sends on retry |
| audit columns + `IsDeleted` | | |

Indexes: unique `DedupKey`; `(Status, NextAttemptAtUtc)`; `(MemberId, Category, CreatedDate)`.

#### `raah_ChatEmailState` (BR-18.7 "one email per conversation")

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint | Recipient of the emails |
| `ConnectionId` | bigint | Conversation |
| `LastNotifiedMessageId` | bigint null | Highest message already emailed |
| `LastEmailSentAtUtc` | datetime2 null | |
| `LastOpenedAtUtc` | datetime2 null | Written when the recipient opens the chat |
| audit columns + `IsDeleted` | | |

Unique `(MemberId, ConnectionId)`.

#### `raah_LeadDigestState` (BR-18.5 daily digest)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint | |
| `DigestDate` | date | India business date |
| `LeadCount` | int | Number of new leads summarised |
| `LastNotificationId` | bigint | High-water mark |
| `SentAtUtc` | datetime2 | |
| audit columns + `IsDeleted` | | |

Unique `(MemberId, DigestDate)`.

**Migration names (indicative):**
- `20260101000000_AddRaahNotification`
- `20260101000100_AddRaahNotificationPreference`
- `20260101000200_AddRaahEmailOutbox`
- `20260101000300_AddRaahChatEmailState`
- `20260101000400_AddRaahLeadDigestState`

### 4.2 Enums

```
NotificationRecipientKind { Member = 1, Admin = 2 }

NotificationCategory {
    Leads = 1, Requests = 2, Chat = 3, Safety = 4,
    Account = 5, Legal = 6, System = 7, AdminAlerts = 8
}

NotificationEventType {
    MemberWelcome = 1,
    NewLeadFound = 2,
    ConnectionRequestReceived = 3,
    RequestExpiring = 4,
    RequestAccepted = 5,
    RequestRejected = 6,
    RequestWithdrawn = 7,
    RequestExpired = 8,
    PendingRequestWithdrawnBySystem = 9,
    NewChatMessage = 10,
    Disconnected = 11,
    RatingReceived = 12,
    RatingCommentHidden = 13,
    ReportSubmitted = 14,
    ReportDecided = 15,
    SafetyActionTaken = 16,
    RouteDeactivatedByAdmin = 17,
    ProfilePhotoRemovedByAdmin = 18,
    AccountReactivatedByAdmin = 19,   // email-only per matrix
    LegalReacceptanceRequired = 20,
    SuggestedCityDecided = 21,
    AdminReportsThresholdReached = 22,
    AdminNewReport = 23,
    AdminReportedRating = 24
}

EmailOutboxStatus { Pending = 1, Sending = 2, Sent = 3, Failed = 4, Cancelled = 5 }
```

Email-only events (`EmailVerification`, `EmailChangeVerification`, `PasswordReset`, `AccountReactivatedByAdmin`) do **not** create a `raah_Notification` row; they only enqueue an email. Use the template keys in §8.

Store enums as `tinyint` / `int` through EF conversion, matching existing small-int style.

---

## 5. Event contracts (source → M9)

These are plain Core event classes, published through `OutboxMessage`. Payloads carry **IDs only**; M9 loads display names through `IPublicDisplayNameResolver` so wording stays current and private data never travels in the event.

| Event | Producer | Key payload | Notification(s) produced |
|---|---|---|---|
| `MemberEmailVerifiedEvent` | Foundation | memberId | In-app welcome |
| `LeadCreatedEvent` | M3/M4 | memberId, leadId, otherMemberId | In-app "new lead"; counted for the daily digest |
| `ConnectionRequestReceivedEvent` | M5 | requestId, senderId, receiverId | In-app + email to receiver |
| `ConnectionRequestAcceptedEvent` | M5 | requestId, senderId, receiverId | In-app + email to sender |
| `ConnectionRequestRejectedEvent` | M5 | requestId, senderId, receiverId | In-app to sender, neutral wording |
| `ConnectionRequestWithdrawnEvent` | M5 | requestId, senderId, receiverId | In-app to receiver |
| `ConnectionRequestExpiredEvent` | M5 | requestId, senderId, receiverId | In-app to sender |
| `PendingRequestWithdrawnBySystemEvent` | M5/M8 | requestId, recipientId, cause | In-app to "the other member" |
| `ConnectionDisconnectedEvent` | M5 | connectionId, recipientId | In-app to the other member |
| `ChatMessageSentEvent` | M6 | connectionId, senderId, recipientId, messageId | In-app only; email handled by the §7.4 job |
| `ChatConversationOpenedEvent` | M6 | connectionId, memberId | Resets `raah_ChatEmailState` |
| `RatingReceivedEvent` | M7 | ratingId, ratedMemberId, raterMemberId | In-app |
| `RatingCommentHiddenEvent` | M7/M12 | ratingId, raterMemberId | In-app |
| `SafetyReportSubmittedEvent` | M8 | reportId, reporterId | In-app to reporter; Admin in-app |
| `SafetyReportDecidedEvent` | M8/M12 | reportId, reporterId | In-app to reporter, no action detail |
| `SafetyActionTakenEvent` | M12 | memberId, actionType | In-app + email (mandatory) |
| `RouteDeactivatedByAdminEvent` | M11/M12 | memberId, routeId | In-app + email (mandatory) |
| `ProfilePhotoRemovedByAdminEvent` | M10 | memberId | In-app |
| `AccountReactivatedByAdminEvent` | M10 | memberId | Email only |
| `LegalReacceptanceRequiredEvent` | M13 | documentIds, effectiveDate | In-app + email to all eligible members (fan-out in handler) |
| `SuggestedCityDecidedEvent` | M2/M13 | memberId, cityName, accepted | In-app |
| `ReportsThresholdReachedEvent` | M8 | memberId, reportCount | Admin in-app + email |
| `ReportedRatingCommentEvent` | M7/M12 | ratingId | Admin in-app |

Integration rule: when M9 lands, add the missing `OutboxMessage` enqueue calls to the source modules **inside their existing transactions**. Do not add business logic to the handlers.

---

## 6. DTOs (Binsera.Core)

Suggested namespace: `Binsera.Core.Notifications.Models.Raah`.

### 6.1 Read models

```
NotificationItemDto
  id                long
  category          string      // "Leads" | "Requests" | "Chat" | "Safety" | "Account" | "Legal" | "System" | "AdminAlerts"
  eventType         string      // stable enum name for analytics/telemetry
  title             string
  body              string?
  linkUrl           string?     // internal relative path
  isRead            bool
  createdAtUtc      DateTime

NotificationListDto
  items             NotificationItemDto[]
  unreadCount       int
  nextCursor        string?     // keyset: "ticks:id"
  activeFilter      string

NotificationPreferenceDto
  leadsDigestEmail        bool
  unreadChatEmail         bool
  digestTimeLabel         string   // e.g. "7:00 AM"
  digestTimeZone          string   // "Asia/Kolkata"
  mandatoryEmailCategories string[] // ["Safety","Account","Legal","System"]
  canEdit                 bool     // false for Admin
```

### 6.2 Request DTOs

| DTO | Fields |
|---|---|
| `NotificationListQuery` | `Filter` (string? — All/Leads/Requests/Chat/Safety/Account/Legal/System), `Cursor` (string?), `Limit` (int?, default 20, max 50) |
| `UpdateNotificationPreferenceRequest` | `LeadsDigestEmail` (bool), `UnreadChatEmail` (bool) |
| `MarkReadRequest` | none (id in route); optional `MarkAllRequest { Filter?: string }` |

### 6.3 Internal models

| Model | Fields |
|---|---|
| `NotificationIntent` | `RecipientMemberId`, `RecipientKind`, `Category`, `EventType`, `SourceEntityType`, `SourceEntityId`, `TemplateKey`, `SafeData` (dictionary of allow-listed keys), `LinkUrl`, `DedupKey`, `SendEmail` (bool), `IsMandatoryEmail` (bool) |
| `EmailMessage` | `MemberId`, `RecipientEmail`, `TemplateKey`, `Category`, `IsMandatory`, `Data` (allow-listed), `DedupKey`, `NotificationId` |
| `RenderedNotification` | `Title`, `Body`, `LinkUrl` — produced by the template renderer and validated before persistence |

---

## 7. Endpoint catalogue

### 7.1 Member endpoints

Base route: `/me/notifications` unless noted. Member session required; the member is always derived from the token.

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/me/notifications?filter=&cursor=&limit=` | List notifications with unread count | SCR-18.1 | 200 `NotificationListDto` |
| 2 | GET | `/me/notifications/unread-count` | Bell badge (polled by the shell) | Shell | 200 `{ unreadCount }` |
| 3 | POST | `/me/notifications/{id}/read` | Mark one as read | SCR-18.1 | 200 `{ unreadCount }` |
| 4 | POST | `/me/notifications/read-all` | Mark all (optionally filtered) as read | SCR-18.1 | 200 `{ unreadCount }` |
| 5 | GET | `/me/notification-preferences` | Read the two switches and limits | SCR-18.2 | 200 `NotificationPreferenceDto` |
| 6 | PUT | `/me/notification-preferences` | Update the two optional switches | SCR-18.2 | 200 `NotificationPreferenceDto` |

### 7.2 Admin endpoints

Base route: `/admin/notifications`. Admin session required; `RecipientKind = Admin`.

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 7 | GET | `/admin/notifications?filter=&cursor=&limit=` | Admin notification list | 200 `NotificationListDto` |
| 8 | GET | `/admin/notifications/unread-count` | Admin shell badge | 200 `{ unreadCount }` |
| 9 | POST | `/admin/notifications/{id}/read` | Mark one read | 200 `{ unreadCount }` |
| 10 | POST | `/admin/notifications/read-all` | Mark all read | 200 `{ unreadCount }` |

Admin has **no preferences endpoint**: Admin alerts are mandatory. Admin must never see another member's notification list.

### 7.3 No public creation endpoint

Notifications are created only by handlers and dispatcher calls. There is no `POST /notifications`. The Admin UI cannot compose or send arbitrary notifications in Phase 1.

---

## 8. Templates and wording

### 8.1 In-app templates

Templates live in code (a small registry keyed by `NotificationEventType`), not the database, in Phase 1. Every placeholder is resolved from `SafeData`; unknown keys fail the render and dead-letter the outbox message.

| Event | Title / body (fixed) | Link |
|---|---|---|
| MemberWelcome | "Welcome to RAAH. Your email is verified." | `/route` |
| NewLeadFound | "A new person matches your route." (no name needed; count shown in the list) | `/leads` |
| ConnectionRequestReceived | "**{Name}** wants to connect with you." | `/leads/requests` |
| RequestExpiring | "The request from **{Name}** expires in {Days} days." | `/leads/requests` |
| RequestAccepted | "**{Name}** accepted your request. You can now chat." | `/chats` |
| RequestRejected | "Your request was not accepted." | `/leads/requests` |
| RequestWithdrawn | "**{Name}** withdrew their request." | `/leads/requests` |
| RequestExpired | "A request expired because there was no reply." | `/leads/requests` |
| PendingRequestWithdrawnBySystem | "A pending request was withdrawn." | `/leads/requests` |
| NewChatMessage | "**{Name}** sent you a message." | `/chats/{connectionId}` |
| Disconnected | "**{Name}** disconnected from you." | `/chats` |
| RatingReceived | "**{Name}** rated you." | `/me/ratings` |
| RatingCommentHidden | "A comment on your rating was hidden." | `/me/ratings` |
| ReportSubmitted | "Your report was sent. We will review it." | `/me/reports` |
| ReportDecided | "Your report has been reviewed." (never states the action) | `/me/reports` |
| SafetyActionTaken | "Your account status was updated. Check your account." | `/me` |
| RouteDeactivatedByAdmin | "Your route was turned off by Admin." | `/route` |
| ProfilePhotoRemovedByAdmin | "Your profile photo was removed by Admin." | `/profile` |
| LegalReacceptanceRequired | "Our Terms of Use have been updated." | `/accept-terms` |
| SuggestedCityDecided | "Your city suggestion was {added/declined}." | `/route` |
| AdminReportsThresholdReached | "**{MemberNumber}** has reached {Count} reports." | `/admin/reports` |
| AdminNewReport | "A new safety report was submitted." | `/admin/reports` |
| AdminReportedRating | "A rating comment was reported." | `/admin/ratings` |

**BR-18.11 role-aware request wording** is used for the email body and may be used in the in-app body:
- To a **Raahi**: "*{Name}* is looking for someone on a route that overlaps with your regular route."
- To a **Saathi**: "*{Name}* regularly travels a route that overlaps with yours."

**Rejection and block:** rejection is always "Your request was not accepted." with no reason (BR-18.3). A block is **never** notified (BR-17.2); there is no `Blocked` template.

### 8.2 Email template keys

| Template key | Use | Optional? |
|---|---|---|
| `LeadsDailyDigest` | One email per day with the count of new leads (BR-18.5) | Yes |
| `ConnectionRequestReceived` | Request received (SC-18.1) | No |
| `RequestExpiring` | Reminder CFG-29 days before expiry (AC-18.4) | No |
| `RequestAccepted` | Accept (safety of the loop) | No |
| `NewChatMessageReminder` | One unread-message email per conversation (BR-18.7) | Yes |
| `SafetyActionTaken` | Warning / restriction / block | No |
| `RouteDeactivatedByAdmin` | Admin route deactivation | No |
| `AccountReactivatedByAdmin` | Reactivation | No |
| `LegalReacceptanceRequired` | Terms/privacy/safety update | No |
| `AdminReportsThresholdReached` | Admin alert at CFG-15 | No |
| `AdminNewReportDigest` | Admin digest of new reports | No |
| `EmailVerification` | Foundation-owned send; shared layout | No |
| `EmailChangeVerification` | M1-owned send; shared layout | No |
| `PasswordReset` | Foundation-owned send; shared layout | No |

Email layout rules (UI plan §6.9 "Email layout"): one column, 560 px, RAAH mark, **one sentence naming the person**, one button, footer with "Notification settings" and Terms/Privacy links. **Never** chat text, contact details, route details, rejection/block reasons (BR-18.2). The digest shows only the count and a button to Leads.

### 8.3 Render-time privacy guard (V-49)

Before persisting `Title`/`Body` or enqueuing an email:

1. Reject if the text contains a 10-digit digit run, an `@` address, a URL, or a token.
2. `LinkUrl` must match an allow-listed internal path prefix (`/leads`, `/chats`, `/route`, `/profile`, `/me`, `/accept-terms`, `/admin`, `/settings`).
3. `DataJson` may only contain allow-listed keys per template (name, count, days, memberNumber, link).
4. On failure: do not persist/send, log a warning, and dead-letter the outbox message. A privacy failure must never be silently swallowed.

---

## 9. Endpoint details

### 9.1 `GET /me/notifications`

- Derive `memberId` from the token.
- Default `filter = All`, `limit = 20` (max 50).
- Return non-archived rows only, ordered `CreatedDate DESC, Id DESC` (keyset paging via `(CreatedDate, Id)`).
- Include `unreadCount` for the **whole inbox**, not just the filtered set.
- `filter` maps to categories: All → all; Leads → Leads; Requests → Requests; Chat → Chat; Safety → Safety; Account → Account; Legal → Legal; System → System.
- Admin list uses `RecipientKind = Admin`; a Member token can never read Admin rows and vice versa.

### 9.2 `GET /me/notifications/unread-count`

- Count `IsRead = 0`, `ArchivedAtUtc IS NULL`, `RecipientKind` matching the caller.
- Lightweight; safe for 30–60 s polling. No websocket/SSE in Phase 1.

### 9.3 `POST /me/notifications/{id}/read`

- Ownership check: `RecipientMemberId == token member` and matching kind; otherwise `404 NOTIFICATION_NOT_FOUND` (do not reveal existence).
- If already read, return success (idempotent).
- Set `IsRead = 1`, `ReadAtUtc = now`.
- Do not write a business audit row for reads (they are informational, BR-18.1); the `ReadAtUtc` column is the record.
- Return the fresh `unreadCount`.

### 9.4 `POST /me/notifications/read-all`

- Optional `filter`; mark matching unread rows read.
- One `UPDATE`-style loop inside a transaction; idempotent.
- Return fresh `unreadCount`.

### 9.5 `GET /me/notification-preferences`

- Create the row lazily with defaults (`true`, `true`) if it does not exist.
- Return `digestTimeLabel` / `digestTimeZone` from CFG-31 (Asia/Kolkata).
- `mandatoryEmailCategories` is informational so the UI can show the locked "Always on" rows.

### 9.6 `PUT /me/notification-preferences`

- Accept only the two booleans. Ignore/reject unknown properties.
- Persist and audit `NOTIFICATION_PREFERENCES_CHANGED` with old/new.
- Never allow a preference to suppress a mandatory email (BR-18.4, AC-18.3). Attempting to include a "mandatory" flag must be rejected with `400 PREFERENCE_NOT_ALLOWED`.
- Return the refreshed DTO.

### 9.7 Admin endpoints

Same behaviour as member endpoints with `RecipientKind = Admin`. Admin unread counts drive the Admin shell bell and the Dashboard "latest five". No preferences. Admin list filter maps `AdminAlerts` and `Safety` categories.

---

## 10. Dispatch pipeline

`INotificationDispatcher.DispatchAsync(intent)`:

1. Validate the intent (recipient, category, event type, dedup key, template key).
2. Load recipient eligibility:
   - If the recipient is Inactive, deactivated or Blocked → **skip** (BR-18.10), **except** when the event is the recipient's own account status (`SafetyActionTaken`, `RouteDeactivatedByAdmin`, `AccountReactivatedByAdmin`).
   - Admin recipients must have the Admin role.
3. Render the in-app template with safe data; run the V-49 guard.
4. Upsert the `raah_Notification` row by `DedupKey` (skip if it already exists; outbox retries stay idempotent).
5. If `SendEmail`:
   - Evaluate preferences for optional templates only.
   - Resolve the recipient email from Foundation; never store it in the notification row.
   - Apply channel config CFG-16 (email enabled?).
   - Enqueue `raah_EmailOutbox` by `DedupKey`.
6. Return a small result (`created`, `duplicate`, `skippedReason`) for logging.

**Ordering and failure:** the in-app row and the email queue row are written in one transaction. If the email row fails, the notification is not duplicated on retry because of `DedupKey`.

**Digest exception:** `NewLeadFound` always creates the in-app row immediately, but its email is **not** enqueued per lead. The digest job (§11.3) builds one email.

**Chat exception:** `NewChatMessage` creates the in-app row immediately; the email is handled by the reminder job (§11.4), not by the dispatcher.

---

## 11. Scheduled jobs

Register each as `AddHostedService<>()` in `Program.cs`, config-driven (`Enabled`/`Cron`/`TimeZone`) per the repo's job convention. Resolve scoped services through `IServiceScopeFactory` inside each tick.

### 11.1 Expiry reminder job (BR-18.6, AC-18.4)

- Runs daily.
- Ask `IPendingRequestReminderSource` for Pending requests where `ExpiresAtUtc - CFG-29 days <= now < ExpiresAtUtc` and no reminder sent.
- For each: dispatch `RequestExpiring` (in-app + mandatory email) to the **receiver**.
- `DedupKey = $"request-expiring:{requestId}"` guarantees one reminder per request.
- Re-read CFG-5 and CFG-29 on each run.

### 11.2 Archive job (BR-18.8)

- Runs daily.
- Set `ArchivedAtUtc` on notifications older than 90 days (read or unread). Never delete, never set `IsDeleted`.
- Archived rows are excluded from the list and from unread counts.

### 11.3 Daily leads digest job (BR-18.5, SC-18.2, AC-18.2)

- Runs at CFG-31 (Asia/Kolkata).
- For each member with `LeadsDigestEmail = true` and at least one new-lead notification since `raah_LeadDigestState.SentAtUtc` (or last 24 h):
  1. Count the new leads.
  2. Enqueue **one** `LeadsDailyDigest` email with `{ count }` only.
  3. Upsert `raah_LeadDigestState` for `DigestDate` with the high-water mark.
- `DedupKey = $"leads-digest:{memberId}:{yyyy-MM-dd}"` prevents a second send on restart.
- Members with the switch off get no digest but keep in-app lead notifications (SC-18.6).

### 11.4 Unread chat email job (BR-18.7, SC-18.5)

- Runs every 5–15 minutes (configurable).
- Ask `IUnreadChatSource` for conversations where the latest unread message is older than CFG-30 hours.
- For each, if `UnreadChatEmail = true` and no email has been sent for this conversation since `LastOpenedAtUtc`:
  1. Enqueue one `NewChatMessageReminder` email: "You have a new message from {Name}." Never the message text.
  2. Set `LastNotifiedMessageId` and `LastEmailSentAtUtc`.
- When the recipient opens the chat, `ChatConversationOpenedEvent` sets `LastOpenedAtUtc = now`, re-arming at most one future email per conversation.

### 11.5 Email sender job

- Polls `raah_EmailOutbox` for `Status = Pending` and `NextAttemptAtUtc <= now`.
- Marks `Sending`, calls `IEmailTransport`, then `Sent` or `Failed` with exponential backoff (e.g. 1, 5, 15, 60, 240 minutes; max attempts 5).
- `LastError` stores a truncated provider error, never the payload.
- Success/failure counts feed RPT-23.

### 11.6 Job concurrency

Only one instance of each job may run at a time. Use the existing single-host assumption; if multi-instance later, add a DB lease row. State this explicitly in code comments/config.

---

## 12. Config usage

| CFG | Use in M9 |
|---|---|
| CFG-16 | In-app always on; Email must be enabled to queue optional/mandatory emails; SMS reserved (D-8) |
| CFG-29 | Days before request expiry for the reminder (default 5) |
| CFG-30 | Hours an unread chat message waits before one reminder email (default 12) |
| CFG-31 | Daily digest send time (default 07:00, Asia/Kolkata) |
| CFG-5 | Request expiry length, read by the reminder source (owned by M5) |
| CFG-15 | Reports threshold, read by the Admin alert producer (M8) |

Read config through the Foundation reader; do not hard-code defaults in M9 (defaults live in Foundation/M13).

---

## 13. Validation matrix

| ID | Rule | Where | Error |
|---|---|---|---|
| V-49 | No private details in notification or email | Render guard (§8.3) | dead-letter + log, never persist |
| BR-18.2 | No chat text, other member's email/mobile, or rejection/block reason | Templates + guard | as above |
| BR-18.4 | Only two optional email switches exist | Preferences PUT | `PREFERENCE_NOT_ALLOWED` |
| BR-18.8 | Old notifications archived, not deleted | Archive job | — |
| BR-18.10 | No send to Inactive/Blocked/deactivated, except own account status | Dispatcher | skip with reason |
| — | Filter must be a known value | List query | `NOTIFICATION_FILTER_INVALID` |
| — | Limit 1–50 | List query | `NOTIFICATION_LIMIT_INVALID` |
| — | Read target must belong to the caller | Read endpoints | `NOTIFICATION_NOT_FOUND` |
| — | `LinkUrl` must be an allow-listed internal path | Render guard | dead-letter + log |
| — | Dedup key unique per recipient/event/source | Dispatcher | duplicate → skip |

No validation framework exists in the repo; implement these checks in the M9 services and unit-test them.

---

## 14. Audit and reporting

### 14.1 Audit events

| Event | Trigger | Old → New |
|---|---|---|
| `NOTIFICATION_PREFERENCES_CHANGED` | Preferences PUT | old booleans → new booleans |
| `NOTIFICATION_TEMPLATE_REJECTED` | V-49 guard failure | event type, recipient ref, reason (no body) |
| `NOTIFICATION_EMAIL_FAILED` | Sender job final failure | template key, attempts, provider code (no payload) |

Reads and in-app creation are **not** business audit events (BR-18.1); the notification row and `ReadAtUtc` are the record. Email sends are tracked in `raah_EmailOutbox` for RPT-23.

### 14.2 RPT-23 Notification Delivery Summary

M15 reads M9 tables directly (no report endpoint in M9):

- Notifications created / read per category per period.
- Emails queued / sent / failed per template key.
- Digest runs and counts per day.
- Optional: preference adoption (how many members switched each optional email off).

Keep `raah_EmailOutbox` rows (do not purge) so historical delivery reporting stays accurate.

---

## 15. Privacy and security

- **Allow-list everywhere.** The event payload, template data, persisted title/body and email data are separate allow-lists; no DTO inherits from a member profile DTO.
- **Names only.** The only person data in a notification is the display name (and Admin member number for Admin alerts). Never email, mobile, route details, chat text, rejection/block reason.
- **Links only.** Store internal relative paths; never absolute URLs with tokens.
- **No notification for a block** (BR-18.3/BR-17.2). Do not add a `Blocked` event handler.
- **Email addresses** live only in `raah_EmailOutbox`; they are not copied into `raah_Notification`.
- **Idempotency** through unique `DedupKey` on both tables; outbox retries and job restarts must not duplicate sends.
- **Race safety:** dispatcher upserts by dedup key; sender job claims rows with an atomic status transition (`Pending` → `Sending`).
- **Rate/abuse:** a member cannot read another member's notifications; ownership is always derived from the token. Admin cannot read member notifications through the member routes.
- **Logging:** never log rendered chat text, tokens, or full email addresses; mask as `••••@mail.com` if a log line is unavoidable.
- **Admin access:** Admin rows are separate (`RecipientKind = Admin`); a member with an Admin role sees only the Admin inbox in the Admin UI.

---

## 16. Build sequence (M9)

| Step | Work | Output |
|---|---|---|
| 1 | Add enums, entities, configurations, migrations (`raah_Notification`, `raah_NotificationPreference`, `raah_EmailOutbox`, `raah_ChatEmailState`, `raah_LeadDigestState`) | Compiles; migrations apply cleanly |
| 2 | Add Core event contracts (§5) and `NotificationIntent`/`EmailMessage` models | Contracts ready |
| 3 | `INotificationTemplateRegistry` + in-app and email renderers + V-49 guard | Unit-testable renderer |
| 4 | `NotificationDispatcher` (`[AutoRegister]`) with eligibility, dedup, in-app insert, email enqueue | Core pipeline |
| 5 | M9 handlers (`IExternalEventHandler<T>`) for every §5 event | Outbox wiring |
| 6 | Add missing `OutboxMessage` enqueue calls to M4–M8 inside their transactions | Events flow |
| 7 | `NotificationQueryService` (list, unread count, read, read-all) + preference service | Read APIs |
| 8 | Member and Admin controllers (`me/notifications`, `admin/notifications`, `me/notification-preferences`) | Postman-ready |
| 9 | Email sender `BackgroundService` with retry/backoff | Emails send |
| 10 | Jobs: expiry reminder, archive, leads digest, unread chat email | SC-18.2/18.3/18.5 pass |
| 11 | Narrow source gateways (reminder, unread chat, digest, eligibility, display name) | Jobs resolve real data |
| 12 | Privacy tests, dedup tests, RPT-23 queries, audit calls | AC-18.1…18.4 pass |
| 13 | Admin inbox wiring and Dashboard latest-five hand-off (M14) | Admin bell works |

Suggested folders:

```
Binsera.Core/Notifications/
  Domain/Raah/Notification.cs, NotificationPreference.cs, EmailOutbox.cs,
                ChatEmailState.cs, LeadDigestState.cs
  Enums/NotificationRecipientKind.cs, NotificationCategory.cs, NotificationEventType.cs,
        EmailOutboxStatus.cs
  Models/Raah/NotificationItemDto.cs, NotificationListDto.cs, NotificationPreferenceDto.cs,
               NotificationIntent.cs, EmailMessage.cs
  Contracts/INotificationDispatcher.cs, INotificationQueryService.cs,
            INotificationPreferenceService.cs, IEmailOutboxQueue.cs,
            IUnreadCountReader.cs, IPublicDisplayNameResolver.cs
  Contracts/Gateways/IPendingRequestReminderSource.cs, IUnreadChatSource.cs,
                      INewLeadDigestSource.cs, IRecipientEligibilityReader.cs
  Events/*.cs
Binsera.Application/Notifications/Services/
  NotificationDispatcher.cs, NotificationTemplateRegistry.cs, NotificationQueryService.cs,
  NotificationPreferenceService.cs, EmailOutboxQueue.cs, NotificationPrivacyGuard.cs
Binsera.Application/Notifications/Handlers/*Handler.cs
Binsera.Application/Notifications/Gateways/*.cs
Binsera.API/Controllers/Notifications/NotificationController.cs,
             NotificationPreferenceController.cs, AdminNotificationController.cs
Binsera.API/BackgroundServices/EmailSenderJob.cs, ExpiryReminderJob.cs,
             NotificationArchiveJob.cs, LeadsDigestJob.cs, UnreadChatEmailJob.cs
Binsera.Persistence/Configurations/Raah/NotificationConfiguration.cs, ...
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahNotification*.cs
```

---

## 17. Test plan

### 17.1 Unit

- Renderer: every event type produces the fixed wording; role-aware request wording for Saathi/Raahi.
- Privacy guard: rejects 10-digit runs, emails, URLs, tokens; rejects non-allow-listed links; accepts normal names with spaces/initials.
- Dispatcher: creates one row per dedup key; duplicate intent is a no-op; skips Inactive/Blocked/deactivated except own-account-status events.
- Preferences: optional toggles honoured; mandatory emails always queued even when both switches are off (AC-18.3).
- Rejection wording has no reason; no block notification exists.
- Digest: three leads → one email; switch off → no email, in-app rows remain (AC-18.2, SC-18.6).
- Expiry reminder: CFG-5=30, CFG-29=5 → reminder at day 25 only (AC-18.4).
- Unread chat: one email after CFG-30 hours; no second email until the chat is opened; opening re-arms.
- Read endpoints: ownership, idempotent read, read-all with filter.
- Archive: 90-day boundary; archived rows excluded from list and unread count.
- Filters and keyset paging boundaries.

### 17.2 Integration / API

- Missing token → 401; member token on Admin route → 403; Admin token on member route → 403.
- Envelope shape matches DTOs.
- No notification/email JSON or row contains email, mobile, chat text, or a rejection reason.
- Outbox retry does not duplicate in-app rows or emails.
- Email sender retries and dead-letter/failed status.
- Migration does not touch `member_Master`.
- Jobs are idempotent across a restart.

### 17.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-18.1 No private content | Render guard §8.3; template tests; integration JSON test |
| AC-18.2 Digest, not one per lead | §11.3; §17.1 digest test |
| AC-18.3 Mandatory emails | §9.6, §10.5; §17.1 preferences test |
| AC-18.4 Expiry reminder timing | §11.1; §17.1 reminder test |

Scenarios SC-18.1…SC-18.8 map to §5 events, §8 templates and §11 jobs; SC-18.8 (block) is verified by the absence of any block handler/notification.

---

## 18. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| UD-API-1 | Email provider/transport (SMTP vs transactional API) | Define `IEmailTransport`; provider chosen at implementation |
| UD-API-2 | Template storage: code vs database | Code registry for Phase 1; DB templates later |
| UD-API-3 | Editor for Admin notification templates | Not in Phase 1 |
| UD-API-4 | Admin "new report" email | Digest (one per cycle) rather than per report; configurable |
| UD-API-5 | Digest time zone | Asia/Kolkata fixed in Phase 1 |
| UD-API-6 | In-app rows for email-only events (verification, password reset, reactivation) | No in-app rows; email only, matching the matrix |
| UD-API-7 | Store rendered title/body vs re-render on read | Store rendered (history must not change if wording changes); link stored too |
| UD-API-8 | Localisation of notifications | English only in Phase 1; template keys are i18n-ready |
| UD-API-9 | Multi-instance job leasing | Single instance assumed; add a DB lease if scaled out |
| UD-API-10 | Should marking read be in the audit trail? | No; `ReadAtUtc` is the record (informational, BR-18.1) |
| UD-API-11 | Reuse `OutboxMessage` or a dedicated email queue? | Dedicated `raah_EmailOutbox` for retries/reporting; `OutboxMessage` carries the domain events |

---

## 19. Traceability

| Spec item | Where in this plan |
|---|---|
| Purpose (informational only) | §1, §14.1 |
| SCR-18.1 Notification centre | §7.1, §9.1–9.4 |
| SCR-18.2 Notification preferences | §7.1, §9.5–9.6 |
| Notification matrix | §5 events, §8 templates, §11 jobs |
| BR-18.1 informational | §1, §14.1 |
| BR-18.2 no private content | §8.3, §15, §17.2 |
| BR-18.3 neutral rejection / no block notice | §8.1, §15 |
| BR-18.4 email preferences | §4.1 (`raah_NotificationPreference`), §9.6, §10.5 |
| BR-18.5 daily digest | §11.3 |
| BR-18.6 expiry reminder | §11.1 |
| BR-18.7 unread chat email | §4.1 (`raah_ChatEmailState`), §11.4 |
| BR-18.8 read/filter/archive 90 days | §4.1, §7.1, §11.2 |
| BR-18.9 channels / SMS later | §12, §18 UD-API-1 |
| BR-18.10 skip inactive/blocked | §10.2 |
| BR-18.11 role-aware wording | §8.1 |
| Status lifecycle Unread/Read/Archived | §4.1 |
| SC-18.1…18.8 | §5, §8, §11, §17.3 |
| AC-18.1…18.4 | §17.3 |
| Data Impact | §4, §10, §11 |
| RPT-23 Notification Delivery Summary | §14.2 |
| CFG-16/29/30/31 (+5/15) | §12 |
| V-49 | §8.3, §13 |
| D-8, A-48 | §1, §12, §18 |
| Related M5–M8, M12–M14 | §2, §5, §16 |
