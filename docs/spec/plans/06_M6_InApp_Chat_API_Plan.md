# M6 — In-App Chat — API Plan

| | |
|---|---|
| **Module** | M6 — In-App Chat (spec `docs/spec/06_M6_InApp_Chat.md`) |
| **Document type** | API implementation plan: contracts, data model, endpoints, lifecycle, delivery/unread, validation, audit, sequencing, tests |
| **Build order** | #6 after Foundation + M1…M5 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M6 gives the two members of an **Accepted** connection one private, text-only conversation (D-1, D-5):

1. **Create a chat on acceptance** — `IChatLifecycleGateway.OpenChatAsync(connectionId, requestMessage)` is called by M5 inside the accept transaction; the request message becomes the **first message** (BR-15.2, D-23).
2. **List conversations** — newest first, with the other member's limited public card, last-message preview and unread count (SCR-15.1).
3. **Open a conversation** — header (other member, connection link, safety tip, actions), messages paged by keyset, "New messages" boundary (SCR-15.2).
4. **Send a message** — text only, 1…CFG-26 characters, no edit/delete, idempotent on retry (BR-15.4, BR-15.6, V-38, V-40).
5. **Track unread** — per-member read state, unread count in the list and on the Chats tab badge (BR-15.5).
6. **Close a chat** — when the connection is Disconnected/Blocked or a member is Inactive/Blocked; history stays readable, sending is refused (BR-15.7).
7. **Admin access only through a report** — a gateway lets M12 attach and read a conversation inside an open Safety Report and **logs every access** (BR-15.11, RPT-27).

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Creating/closing the connection, accept/reject/disconnect, connection detail | M5 |
| The fixed request message value (CFG-25) and the accept action | Foundation / M5 |
| Block / Report actions and their effects on the connection | M8 |
| Member deactivation / Admin block | M10 / M12 |
| Unread-chat **email** after CFG-30 hours | M9 (M6 supplies unread state) |
| Rating after a connection | M7 |
| Notification centre, badges, digests | M9 |
| Admin report UI, report lifecycle, chat access UI | M12 |
| Chat Activity Summary (RPT-13) and Chat Access Log (RPT-27) reporting | M15 |

M6 **does not book, schedule or record an agreement**. It stores messages only; the system never reminds or enforces anything (D-5, Principle 8).

---

## 2. Dependency and module-boundary handling

M6 is built **after M5** (Accepted connections exist) and **before M7/M8/M9** in the loop. It consumes M5/M1/Foundation and provides gateways used by M5, M8, M9, M12.

### 2.1 Contracts consumed by M6

| Contract | Owner | Purpose |
|---|---|---|
| `IConnectionReadGateway.GetForChatAsync(connectionId)` | M5 | Participants, connection status, connection number, request message |
| `IConnectionReadGateway.IsChatOpenAsync(connectionId)` | M5 | Whether the connection is still Accepted |
| `IPublicMemberProfileService.GetAsync(memberId, viewerMemberId)` | M1 | Limited public card/profile for the other member (no email/mobile) |
| `IMemberStatusReader.GetAsync(memberId)` | Foundation | Active / Restricted / Inactive / Blocked |
| `IAppConfigReader` | Foundation | CFG-26 (message length), CFG-30 (unread email delay — surfaced to M9) |
| `IBlockPolicyGateway.IsBlockedPairAsync(a, b)` (no-op until M8) | M8-owned, M6 consumes | Refuse/close a chat for a blocked pair |
| `INotificationGateway` (no-op until M9) | M9-owned, M6 consumes | New-message in-app notification + unread-email trigger (CFG-30) |
| `IAuditWriter` | Foundation | Admin chat access, system close events |

### 2.2 Contracts provided by M6 (consumed later)

| Contract | Purpose | Consumer |
|---|---|---|
| `IChatLifecycleGateway` | `OpenChatAsync(connectionId, firstMessage)` → `chatId`; `CloseChatAsync(connectionId, reason)`; `CloseForMemberAsync(memberId, reason)` | M5 (accept/disconnect), M8 (block), M10 (deactivate) |
| `IChatReadGateway` | `GetChatIdForConnectionAsync`, `IsChatOpenAsync`, `GetUnreadSummaryAsync(memberId)`, `GetParticipantsAsync(chatId)` | M5, M9, M11 |
| `IChatAdminAccessGateway` | `OpenForReportAsync(reportId, chatId/connectionId, adminMemberId)` → messages; appends `raah_ChatAccessLog` | M12 |

Rules:

- These interfaces replace the **no-op** `IChatLifecycleGateway` M5 ships today. When M6 lands, register the real implementation via `[AutoRegister]`; M5 endpoint contracts do not change.
- M6 depends on M7/M8/M9 only through the tiny gateways above; each no-op returns a safe default (notification dropped, pair-not-blocked).
- M6 **never creates or edits a connection**; it reads connection state through M5 and closes chats when M5/M8/M10 say the connection ended.
- Messages and chats are **never deleted** (BR-15.12); closed chats stay read-only.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; `[Route("chats")]`; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interface in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` → `ResponseFilterAttribute` envelope `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | `BadRequest(new { code, message })` / object-result `StatusCode(404/409/403/429, new { code, message })`; **never** `Forbid()` / `Challenge()` |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new EF-managed tables; `member_Master` untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id`, decimal precision, audit columns, `IsDeleted`, per-column indexes |
| Validation | No FluentValidation; validate in `ChatMessageService` / `ChatQueryService` |
| Current user | Derive the member id from the token; never accept a `memberId`/`senderId` in the body |
| Background job | Plain `BackgroundService` + `Cronos` parser, config-driven, `IServiceScopeFactory` per tick (only if a job is needed; M6 has none) |

---

## 4. Data model

### 4.1 New table `raah_Chat`

One row per **connection** — a new connection after a closed one always starts a **new** chat (BR-15.8). A chat has exactly two participants, derived from the connection.

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `ChatNumber` | nvarchar(20) | `CHT-000123`, generated on open, never reused (BR-7.1/7.2) |
| `ConnectionId` | bigint | FK → `raah_Connection.Id`; **unique** (one chat per connection) |
| `LowerMemberId` | bigint | `min(sender, receiver)` — normalized pair key |
| `HigherMemberId` | bigint | `max(sender, receiver)` |
| `Status` | tinyint | `ChatStatus` (§4.5) |
| `FirstMessageId` | bigint null | The request message promoted to first message (BR-15.2) |
| `LastMessageId` | bigint null | Denormalized for ordering/list preview |
| `LastMessageOnUtc` | datetime2 null | List order (`DESC`) |
| `OpenedOnUtc` | datetime2 | |
| `ClosedOnUtc` | datetime2 null | |
| `ClosedReason` | tinyint null | `ChatClosedReason` (§4.5); never shown with a blocker identity |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false (never deleted) |

Indexes:

- Unique `ChatNumber`.
- Unique `ConnectionId`.
- `(LowerMemberId, HigherMemberId)` — pair lookup.
- `(LowerMemberId, Status, LastMessageOnUtc DESC)` and `(HigherMemberId, Status, LastMessageOnUtc DESC)` — chat list for each participant.

### 4.2 New table `raah_ChatMessage`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | Monotonic; ordering and keyset cursor |
| `ChatId` | bigint | FK → `raah_Chat.Id` |
| `SenderMemberId` | bigint | FK → `member_Master.Id`; the request message's sender is the original requester (BR-15.2) |
| `Kind` | tinyint | `ChatMessageKind` (§4.5): `RequestMessage = 1`, `Text = 2` |
| `MessageText` | nvarchar(4000) | Stored text; length validated against **current** CFG-26 on send (V-38) |
| `ClientMessageId` | uniqueidentifier null | Client-supplied idempotency key for retries |
| `SentOnUtc` | datetime2 | |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false; **no edit/delete path** (BR-15.6, V-40) |

Indexes:

- `(ChatId, Id)` — message page / `after` cursor.
- Unique filtered `(ChatId, ClientMessageId)` where `ClientMessageId IS NOT NULL` — idempotent send.

Messages are append-only. There is **no** update or delete endpoint and no soft-delete path exposed.

### 4.3 New table `raah_ChatReadState`

One row per `(ChatId, MemberId)`; created lazily on first read/mark-read.

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `ChatId` | bigint | FK → `raah_Chat.Id` |
| `MemberId` | bigint | FK → `member_Master.Id` |
| `LastReadMessageId` | bigint null | Highest message id the member has seen |
| `LastReadOnUtc` | datetime2 null | |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Index: unique `(ChatId, MemberId)`; `(MemberId, ChatId)` for badge aggregation.

**Unread derivation (no stored counters — avoids drift):**

```
unread(chat, me) = COUNT(*) FROM raah_ChatMessage m
                   WHERE m.ChatId = chat AND m.Id > COALESCE(lastReadMessageId, 0)
                     AND m.SenderMemberId <> me
```

Opening a conversation marks it read up to the newest message (§7.5), clearing the count (SC-15.3).

### 4.4 New table `raah_ChatAccessLog` (append-only, for RPT-27)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `ChatId` | bigint | FK → `raah_Chat.Id` |
| `ReportId` | bigint | The open Safety Report that authorized access (BR-15.11) |
| `AdminMemberId` | bigint | Who viewed |
| `Action` | tinyint | `ChatAccessAction` (§4.5) — `ViewConversation = 1` |
| `AccessedOnUtc` | datetime2 | |

Index: `(ChatId, AccessedOnUtc DESC)`, `(ReportId)`. Normal members never read this table. Written only by `IChatAdminAccessGateway`.

### 4.5 Enums

```
ChatStatus             { Open = 1, Closed = 2 }
ChatClosedReason       { ConnectionDisconnected = 1, ConnectionBlocked = 2,
                         ConnectionEnded = 3, MemberDeactivated = 4,
                         MemberBlocked = 5, System = 6 }
ChatMessageKind        { RequestMessage = 1, Text = 2 }
ChatAccessAction       { ViewConversation = 1 }
ChatStatusFilter       { Open = 1, Closed = 2, All = 3 }
```

Migration (indicative): `20260106000000_AddRaahChatAndMessages`.

---

## 5. DTOs (Binsera.Core)

Namespace: `Binsera.Core.Chat.Models.Raah`.

### 5.1 `ChatListItemDto` (SCR-15.1)

```
chatId                 long
chatNumber             string            // CHT-nnnnnn
connectionId           long
connectionNumber       string            // CON-nnnnnn
status                 string            // "Open" | "Closed"
statusLabel            string            // "Open" | "Closed"
otherMember            PublicMemberCardDto   // M1 limited card; no email/mobile
lastMessagePreview     string?           // trimmed, single line
lastMessageOn          DateTime?
lastMessageSenderIsMe  bool
unreadCount            int
closedReasonLabel      string?           // neutral "Connection ended"; never names a blocker
canOpen                bool
```

### 5.2 `ChatDetailDto` (SCR-15.2 header)

```
chatId, chatNumber, connectionId, connectionNumber
status, statusLabel
closedReasonLabel      string?           // neutral
otherMember            PublicMemberProfileDto  // M1 limited profile (never private)
connectionStatus       string            // "Accepted" | "Ended"
safetyTip              string            // always present (BR-15.9)
canSend                bool
canRead                bool              // always true while member belongs
actions                { canViewConnection, canRate, canDisconnect, canBlock, canReport }
firstUnreadMessageId   long?             // for the "New messages" divider
lastMessageId          long?
participants           { meMemberId, otherMemberId }   // ids only, no contact data
```

### 5.3 `ChatMessageDto`

```
messageId              long
chatId                 long
kind                   string            // "RequestMessage" | "Text"
sender                 string            // "Me" | "Other" | "System"
text                   string
sentOn                 DateTime
deliveryStatus         string            // "Sent" (client shows "Sending"/"Failed" locally)
```

No `senderMemberId` is returned to the client; only the relative `Me`/`Other`/`System` label (§14).

### 5.4 `ChatMessagePageDto`

```
chatId
messages               ChatMessageDto[]
firstUnreadMessageId   long?
hasMore                bool
nextCursor             string?           // keyset: oldest message id for backward paging
```

### 5.5 Request / result DTOs

| DTO | Fields |
|---|---|
| `SendMessageRequest` | `text` (string), `clientMessageId` (guid?) |
| `SendMessageResultDto` | `messageId`, `chatId`, `sentOn`, `chatStatus`, `unreadCountForOther` |
| `MarkReadResultDto` | `chatId`, `lastReadMessageId`, `unreadCount` |
| `ChatUnreadSummaryDto` | `totalUnreadMessages`, `conversationsWithUnread` |
| `ChatActionResultDto` | `chatId`, `connectionId`, `status`, `statusLabel` |

### 5.6 Status / reason labels (neutral, never blaming — UP-6)

| Stored status / reason | Shown to members |
|---|---|
| Open | "Open" |
| Closed — ConnectionDisconnected | "Closed · Connection ended" |
| Closed — ConnectionBlocked | "Closed · Connection ended" (never says who blocked — BR-17.2) |
| Closed — MemberDeactivated | "Closed · Connection ended" |
| Closed — MemberBlocked | "Closed · Connection ended" |
| Closed — System | "Closed" |

---

## 6. Endpoint catalogue

Base route: `/chats`. Every endpoint requires an authenticated **Member** token and acts only on chats where the member is a participant (BR-15.3).

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/chats?status=open&limit=&cursor=` | Chat list (newest first, unread counts) | SCR-15.1 | 200 `ChatListResponseDto` |
| 2 | GET | `/chats/{chatId}` | Conversation header/detail (other member, actions, unread divider) | SCR-15.2 | 200 `ChatDetailDto` |
| 3 | GET | `/chats/{chatId}/messages?limit=&before=&after=` | Message page (keyset) | SCR-15.2 | 200 `ChatMessagePageDto` |
| 4 | POST | `/chats/{chatId}/messages` | Send a text message | SCR-15.2 | 200 `SendMessageResultDto` |
| 5 | POST | `/chats/{chatId}/read` | Mark conversation read up to a message | SCR-15.2 | 200 `MarkReadResultDto` |
| 6 | GET | `/chats/unread-count` | Total unread for the Chats tab badge | Shell | 200 `ChatUnreadSummaryDto` |

**Chat id.** Routes use numeric `chatId`. DTOs expose the user-facing `chatNumber`. `connectionId` is included for the "View connection" link.

Forward-looking (not part of the M6 member API):

- **Admin conversation access** is `IChatAdminAccessGateway.OpenForReportAsync(...)`, exposed by M12's report endpoint. It is **not** a public `/chats/...` route and always writes `raah_ChatAccessLog` (BR-15.11, AC-15.5).
- Block/Report actions are M8; M6 only reports availability through `actions`.
- Rating is M7.
- Unread email is M9 (M6 exposes unread state + a notification seam).

---

## 7. Endpoint details

### 7.1 `GET /chats`

1. Resolve `meMemberId` from the token.
2. Base set: `LowerMemberId = me OR HigherMemberId = me` (or the indexed split form).
3. `status` filter: `open` (default) | `closed` | `all`.
4. Order `LastMessageOnUtc DESC, Id DESC`; keyset paging (`limit` default 20, clamp to 50).
5. Project `otherMember` through M1 (public card only — AC-10.1/BR-4.9).
6. `lastMessagePreview` = first ~120 chars of the last message, single line; empty for a chat with only the request message if filtered, otherwise the request message preview.
7. `unreadCount` from §4.3 for `me`.
8. `closedReasonLabel` mapped from `ClosedReason` to a neutral phrase (§5.6).
9. `canOpen` = participant (always true for the base set).

### 7.2 `GET /chats/{chatId}`

1. Load the chat; if missing **or** the viewer is neither participant → `404 CHAT_NOT_FOUND` **and log the denied attempt** (BR-4.2, BR-15.3).
2. If the chat is `Open` but the connection is no longer Accepted (M5 says ended) → **reconcile**: close it with the matching reason in the same transaction, then continue (defensive; the gateway normally closes it eagerly).
3. Build:
   - `otherMember` via `IPublicMemberProfileService.GetAsync(otherMemberId, meMemberId)` (public fields only — BR-4.9).
   - `connectionStatus` via `IConnectionReadGateway`.
   - `safetyTip` from the fixed copy (BR-15.9).
   - `canSend` = `Status = Open` **and** connection Accepted **and** pair not blocked (M8 gateway).
   - `actions` from M5/M7/M8 availability: `canViewConnection = true`; `canRate`, `canDisconnect`, `canBlock`, `canReport` mirror M5's detail flags.
   - `firstUnreadMessageId` from the read state (§4.3).
4. Return `ChatDetailDto`. Opening the header is side-effect-free (no status change).

### 7.3 `GET /chats/{chatId}/messages`

1. Participant check; else `404` + log.
2. Paging (keyset, all by `Id`):
   - Default: newest page — return the latest `limit` messages in ascending order, `hasMore` if older exist, `nextCursor` = oldest returned id.
   - `before=<id>`: messages older than `id` (scrolling up).
   - `after=<id>`: messages newer than `id` (polling / reconnect).
3. `limit` default 30, clamp to 100.
4. Map each message to `ChatMessageDto` with relative `sender` ("Me"/"Other"/"System").
5. Include `firstUnreadMessageId` so the client can draw the "New messages" divider once.

### 7.4 `POST /chats/{chatId}/messages`

1. Participant check; else `404` + log.
2. Load chat + connection state. If `Status = Closed` → `409 CHAT_CLOSED`; if connection not Accepted → `409 CONNECTION_NOT_ACTIVE` (V-39, AC-15.2/AC-15.3).
3. Pair-block check (M8 no-op → not blocked); if blocked → `409 CHAT_CLOSED`.
4. Validate `text` (V-38, BR-15.4): required after trim; `1 … CFG-26` characters; text only (no attachment fields exist). Empty → `400 MESSAGE_REQUIRED`; too long → `400 MESSAGE_TOO_LONG` with the limit.
5. Rate limit (G-5): simple per-member sliding window (e.g. 20 messages / 10 s); exceed → `429 MESSAGE_RATE_LIMITED`.
6. Idempotency: if `clientMessageId` matches an existing row for this chat, return that message (200) without inserting a duplicate.
7. In one transaction (`IUnitOfWork`):
   - Insert `raah_ChatMessage` (`Kind = Text`, `SenderMemberId = me`).
   - Update `raah_Chat.LastMessageId/LastMessageOnUtc`.
   - Upsert my `raah_ChatReadState.LastReadMessageId = new id` (sending implies the chat was open).
   - Emit `INotificationGateway.NewChatMessageAsync(otherMemberId, chatId, preview)` (M9 no-op), which drives the in-app notification and the CFG-30 email timer.
8. Return `SendMessageResultDto` with the new id, sent time and the other member's unread count.

### 7.5 `POST /chats/{chatId}/read`

1. Participant check; else `404` + log.
2. Body: `upToMessageId` (optional; default = current `LastMessageId`). Clamp to an existing message in this chat; a foreign/unknown id → `400 MESSAGE_ID_INVALID`.
3. Upsert `raah_ChatReadState` for `me` with `LastReadMessageId = max(current, upToMessageId)`, `LastReadOnUtc = now`. Never moves backwards.
4. Return `MarkReadResultDto` with the remaining `unreadCount` (0 when read fully).

On opening a conversation the client calls this once; sending also advances it (§7.4).

### 7.6 `GET /chats/unread-count`

- Sum unread per §4.3 across the member's `Open` and `Closed` chats (closed chats have no new messages, so effectively Open).
- Return `{ totalUnreadMessages, conversationsWithUnread }` for the tab badge.

---

## 8. Chat lifecycle and gateways

### 8.1 `IChatLifecycleGateway` (implementation, replaces M5 no-op)

| Method | Behaviour |
|---|---|
| `OpenChatAsync(connectionId, firstMessage)` | Idempotent by `ConnectionId`: if a chat exists, return its id. Else create `raah_Chat` (`Open`), insert the **first message** (`Kind = RequestMessage`, sender = connection sender, `SentOnUtc` = connection `SentOnUtc` for accuracy), set `FirstMessageId`/`LastMessageId`. Returns `chatId`. Caller (M5 accept) owns the transaction and passes its `IUnitOfWork`. |
| `CloseChatAsync(connectionId, reason)` | If an `Open` chat exists: `Status = Closed`, `ClosedOnUtc = now`, `ClosedReason = reason`. No messages touched (BR-15.7). Idempotent. |
| `CloseForMemberAsync(memberId, reason)` | Close every `Open` chat where the member is a participant (used by M10 deactivation / Admin block). Idempotent and chunked. |

### 8.2 `IChatReadGateway`

| Method | Behaviour |
|---|---|
| `GetChatIdForConnectionAsync(connectionId)` | `long?`; used by M5 detail/accept and M11. |
| `IsChatOpenAsync(connectionId)` | `Status = Open AND connection Accepted`. |
| `GetUnreadSummaryAsync(memberId)` | `ChatUnreadSummaryDto` for M9 digests/badges. |
| `GetParticipantsAsync(chatId)` | `(meMemberId, otherMemberId)` if caller is a participant; else `null`. |

### 8.3 `IChatAdminAccessGateway`

`OpenForReportAsync(reportId, chatId, adminMemberId)`:

1. Verify the report is **open** and actually involves this chat/connection (M12/M8 owns the check; M6 re-verifies via the connection participants).
2. Return the conversation messages (read-only) to M12.
3. Append `raah_ChatAccessLog` (§4.4) **in the same call**, and write an audit entry.

Without an open report, no member route and no admin route returns chat content (AC-15.5). Every access is logged (RPT-27).

### 8.4 Lifecycle table

| Trigger | Caller | M6 action |
|---|---|---|
| Request Accepted | M5 | `OpenChatAsync` — chat created, request message is first message (BR-15.2) |
| Connection Disconnected | M5 | `CloseChatAsync(ConnectionDisconnected)` |
| Connection Blocked (member block / Admin block) | M8 / M12 | `CloseChatAsync(ConnectionBlocked)` or `CloseForMemberAsync(MemberBlocked)` |
| Member deactivated (self or Admin) | M10 | `CloseForMemberAsync(MemberDeactivated)` |
| New connection after a closed one | M5 | New `OpenChatAsync` → **new** chat row (BR-15.8) |

A closed chat never reopens. History stays readable (BR-15.7, AC-15.3).

---

## 9. Delivery, realtime and unread

- **Default (Phase 1): HTTP polling on the open conversation.** While a conversation is on screen, the client polls `GET /chats/{chatId}/messages?after=<lastId>` every ~3–5 s (and immediately on focus/visibility regain). This needs no new infrastructure and satisfies "delivered almost immediately" (BR-15.5). No read receipts, no typing indicators (A-41).
- **No push requirement.** Off-screen changes surface through the Chats tab badge via `GET /chats/unread-count` and M9 notifications.
- **Optional SignalR (open decision).** A `/hubs/chat` hub may replace polling later; the REST contracts above stay the source of truth. If adopted, the hub only pushes "new message / chat closed" signals; clients still fetch via REST.
- **Ordering.** Messages sort by monotonic `Id` (identity), not client clocks; `sentOn` is display-only.
- **Idempotent send.** `clientMessageId` (§4.2) makes a retry after a timeout safe.
- **Unread** is derived from `raah_ChatReadState` (§4.3), never a stored counter; opening/sending advances it, so counts cannot drift.
- **CFG-30 unread email** is M9's job: M6 emits a new-message notification; M9 decides whether the message is still unread after CFG-30 hours and sends one email per conversation.

---

## 10. Validation matrix

| ID / rule | Rule | Where | Error |
|---|---|---|---|
| BR-15.1 / AC-15.2 | Chat exists and accepts messages only for an Accepted connection | Send, Detail | `409 CHAT_CLOSED` / `409 CONNECTION_NOT_ACTIVE` |
| BR-15.3 | Only the two participants can open the chat | All member routes | `404 CHAT_NOT_FOUND` + log (BR-4.2) |
| V-38 / BR-15.4 | Message 1…CFG-26 chars, text only, not empty | Send | `400 MESSAGE_REQUIRED` / `400 MESSAGE_TOO_LONG` |
| V-39 | Messages only while connection Accepted and chat Open | Send | `409 CHAT_CLOSED` / `409 CONNECTION_NOT_ACTIVE` |
| V-40 / BR-15.6 | No edit or delete | — | No such endpoint exists |
| BR-15.7 / AC-15.3 | Closed chat is read-only; reads still work | Send vs List | Send `409 CHAT_CLOSED`; reads allowed |
| G-5 | Message flood throttle | Send | `429 MESSAGE_RATE_LIMITED` |
| — | `upToMessageId` must belong to this chat | Read | `400 MESSAGE_ID_INVALID` |
| — | Cursor must match the chat and paging direction | Messages | `400 CURSOR_INVALID` |
| — | `limit` within bounds (default/clamp) | List / Messages | clamp or `400 CHAT_LIMIT_INVALID` |
| — | `status` filter ∈ {open, closed, all} | List | `400 CHAT_FILTER_INVALID` |
| BR-15.11 / AC-15.5 | Admin sees content only through an open report; access logged | Admin gateway | not exposed elsewhere |

No validation framework exists; implement in the services. Do not add FluentValidation as a drive-by change.

---

## 11. Config usage

| CFG | Use in M6 |
|---|---|
| CFG-26 | Maximum message length validated on send and shown as the counter limit; read live (a shortened value applies to new messages, never invalidates stored ones) |
| CFG-30 | **Consumed by M9**, not M6; M6 supplies unread state and emits the new-message notification that starts M9's email timer |
| CFG-25 | The fixed request message is supplied by M5 on accept; M6 stores it as the first message verbatim and never edits it |

Read config through the Foundation reader; no hard-coded defaults in M6. Throttle bounds (G-5) have no CFG entry — keep them as a named constant and note as an open decision.

---

## 12. Error responses

Use `BadRequest(new { code, message })` or object-result `StatusCode(404/409/403/429, new { code, message })`. Never `Forbid()`/`Challenge()`. Messages align with UI Plan §7.2 and the M6 UI plan microcopy.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `CHAT_NOT_FOUND` | 404 | "This conversation is not available." |
| `CHAT_CLOSED` | 409 | "This chat is closed. You can read the history." |
| `CONNECTION_NOT_ACTIVE` | 409 | "You can chat once the connection is active." |
| `MESSAGE_REQUIRED` | 400 | "Write a message first." |
| `MESSAGE_TOO_LONG` | 400 | "Messages can be up to *n* characters." |
| `MESSAGE_RATE_LIMITED` | 429 | "You are sending messages too fast. Wait a moment." |
| `MESSAGE_ID_INVALID` | 400 | "Refresh the conversation and try again." |
| `CHAT_FILTER_INVALID` | 400 | "Choose a valid filter." |
| `CHAT_LIMIT_INVALID` | 400 | "Choose a valid page size." |
| `CURSOR_INVALID` | 400 | "Refresh the list and try again." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

---

## 13. Reporting hooks (RPT-13, RPT-27)

M6 owns the data; M15 renders it.

- **RPT-13 Chat Activity Summary (counts only, no content):** conversations opened (count `raah_Chat`), conversations with at least one reply (a `Text` message exists after the `RequestMessage`), messages per conversation, closed chats. Expose aggregate queries through a small read service consumed by M15; **never** return message text.
- **RPT-27 Chat Access Log:** every row of `raah_ChatAccessLog` (date, Admin, report no., connection/chat no.) via M15.

---

## 14. Privacy and security

- The other member is always projected through `IPublicMemberProfileService`; DTOs contain **no** email, mobile, IP, device, login history, reports, admin notes or blocks (BR-4.9, BR-15.10, D-21). Add a serialization test that fails if those keys appear.
- Membership is derived from the token; a foreign `chatId` returns `404` and logs the attempt (BR-4.2, BR-15.3, AC-15.3).
- Messages are immutable and append-only; there is **no** edit/delete path and no soft-delete exposed (BR-15.6, V-40, AC-15.4).
- Admin content access requires an **open report** in the same call and always writes `raah_ChatAccessLog`; no member or unauthenticated route exposes content (BR-15.11, AC-15.5).
- Closed-chat reasons are neutral and never name a blocker (BR-17.2).
- Phone numbers and links are **not** blocked or filtered in chat (A-43, BR-15.9); the system neither strips nor flags them — the UI shows the standing safety tip.
- The mobile number is never injected into a chat by the system (BR-15.10).
- Chats and messages are never purged in Phase 1 (BR-15.12).

---

## 15. Build sequence (M6)

| Step | Work | Output |
|---|---|---|
| 1 | Enums, DTOs, `raah_Chat` / `raah_ChatMessage` / `raah_ChatReadState` / `raah_ChatAccessLog` migration | Compiles, migration applies |
| 2 | `ChatLifecycleService` implementing `IChatLifecycleGateway` (`OpenChatAsync` with first message, `CloseChatAsync`, `CloseForMemberAsync`); wire M5 accept/disconnect | AC-15.2, BR-15.2/15.7/15.8 |
| 3 | `ChatQueryService`: chat list + detail + unread derivation | SCR-15.1/15.2 read |
| 4 | `ChatMessageService`: send (validation, throttle, idempotency), message paging | V-38, G-5, AC-15.1 |
| 5 | `POST /chats/{chatId}/read` + `GET /chats/unread-count` | BR-15.5, SC-15.3 |
| 6 | Notification seam (M9 no-op) + `IChatReadGateway` for badges/digests | BR-15.5, CFG-30 hand-off |
| 7 | `IChatAdminAccessGateway` + `raah_ChatAccessLog` + audit | BR-15.11, AC-15.5, RPT-27 |
| 8 | Controller endpoints + error codes + privacy tests | Postman-ready |
| 9 | Wire M8 block close, M10 deactivation close as they land | End-to-end loop |
| 10 | RPT-13/RPT-27 aggregate readers for M15 | Reporting |
| 11 | Performance: keyset indexes, per-participant list indexes, throttle constant review | Meets latency/paging |

Suggested folders:

```
Binsera.Core/Chat/
  Domain/Raah/Chat.cs, ChatMessage.cs, ChatReadState.cs, ChatAccessLog.cs
  Enums/ChatStatus.cs, ChatClosedReason.cs, ChatMessageKind.cs, ChatAccessAction.cs
  Models/Raah/ChatListItemDto.cs, ChatDetailDto.cs, ChatMessageDto.cs,
               ChatMessagePageDto.cs, SendMessageRequest.cs, *ResultDto.cs
  Contracts/IChatQueryService.cs, IChatMessageService.cs,
            IChatLifecycleGateway.cs, IChatReadGateway.cs, IChatAdminAccessGateway.cs
Binsera.Application/Chat/Services/
  ChatLifecycleService.cs, ChatQueryService.cs, ChatMessageService.cs,
  ChatReadService.cs, ChatAccessLogWriter.cs
Binsera.API/Controllers/Chat/ChatController.cs
Binsera.Persistence/Configurations/Raah/ChatConfiguration.cs,
                     ChatMessageConfiguration.cs, ChatReadStateConfiguration.cs,
                     ChatAccessLogConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahChatAndMessages.cs
```

---

## 16. Test plan

### 16.1 Unit

- Open: idempotent by `ConnectionId`; first message stored as `RequestMessage` with the requester as sender and the request time (BR-15.2); a second connection for the same pair creates a **new** chat (BR-15.8).
- Close: `CloseChatAsync` on disconnect/block sets Closed + neutral reason; `CloseForMemberAsync` closes all participant chats; idempotent.
- Send: rejects empty and `> CFG-26`; accepts exactly CFG-26; updates `LastMessage` and my read state; no edit/delete path exists (V-38/40).
- Idempotency: same `clientMessageId` inserted twice returns one row.
- Throttle: burst over the window returns `MESSAGE_RATE_LIMITED`.
- Permission: non-participant on list/detail/messages/send/read → `404` + log.
- Close guard: send on a Closed chat or ended connection is refused (AC-15.2/AC-15.3).
- Unread: derivation excludes own messages; opening/`read` clears; send advances; badge sum correct (SC-15.3).
- Admin access: `OpenForReportAsync` requires an open report, returns content, appends `raah_ChatAccessLog`; no access without a report (AC-15.5).
- Privacy: DTOs never contain email/mobile keys; `sender` is relative only.

### 16.2 Integration / API

- Every endpoint requires auth; foreign `chatId` → 404 + logged; missing token → 401.
- Envelope present; `data` matches DTO; keyset paging (`before`/`after`/default) has no overlap/gap.
- Migration applies cleanly and does not touch `member_Master`.
- M5 accept now yields `chatId` and the fixed request message as the first chat message (replacing the no-op gateway).
- Disconnect/block/deactivation close the chat; history still readable; send refused.
- Polling with `after=<id>` returns only newer messages; reconnect has no duplicates.
- Notifications dispatched to the recipient only, with a preview and no contact data (with M9 fake).
- RPT-13 aggregates are counts only; RPT-27 returns every access row.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-15.1 Text only and length (CFG-26) | §7.4, §10, §16.1 |
| AC-15.2 Chat only after acceptance | §7.4, §8.1, §10 |
| AC-15.3 Closed chat is read-only | §7.4, §8.4, §16.1 |
| AC-15.4 No edit or delete | §4.2, §10, §14 |
| AC-15.5 Admin access only through a report | §8.3, §14, §16.1 |

Scenarios SC-15.1…SC-15.10 map to the endpoints and lifecycle transitions above; SC-15.6 (phone number in chat) is explicitly allowed (A-43).

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| — | Realtime transport: polling vs SignalR? | HTTP polling with `after` cursor in Phase 1; SignalR optional later, REST stays canonical |
| — | Message page size / initial load? | 30 default, clamp 100; newest page first, scroll up with `before` |
| — | How is a request message represented? | A stored `ChatMessage` with `Kind = RequestMessage`, styled as "Request message" in the UI, never editable |
| — | Throttle limits for G-5 (no CFG defined)? | Named constant: 20 messages / 10 s per member; revisit with M13 |
| — | Do closed chats contribute to the badge? | No new messages can arrive, so effectively Open only |
| — | Should `upToMessageId` accept the newest by default? | Yes; sending also advances the sender's read state |
| — | Chat numbering prefix? | `CHT-{nnnnnn}` via `IRecordNumberGenerator` |
| — | Is admin access logged even if the report is closed mid-request? | Yes — re-verify open at call time; log only successful access |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Field spec (Message) | §4.2, §5.3 |
| SCR-15.1 Chat List | §5.1, §7.1 |
| SCR-15.2 Conversation | §5.2, §5.3, §7.2–7.5 |
| BR-15.1 chat only for Accepted | §8, §10, §16.3 |
| BR-15.2 first message = request message | §4.1/4.2, §8.1 |
| BR-15.3 only the two members | §7.2, §14 |
| BR-15.4 text only + CFG-26 | §7.4, §10, §11 |
| BR-15.5 immediate delivery + unread, no receipts/typing | §9, §4.3 |
| BR-15.6 no edit/delete | §4.2, §10, §14 |
| BR-15.7 closing + read-only | §8.4, §7.4 |
| BR-15.8 new connection = new chat | §4.1, §8.1 |
| BR-15.9 safety tip; phone/links not blocked | §5.2, §14 |
| BR-15.10 mobile never injected | §14 |
| BR-15.11 Admin access via report + logged | §8.3, §13, §14 |
| BR-15.12 never purged | §4.2, §14 |
| Status lifecycle §15.5 | §4.5, §8.4 |
| CFG-26 / CFG-30 / CFG-25 | §11 |
| V-38 / V-39 / V-40 | §10 |
| SC-15.1…SC-15.10 | §16 |
| AC-15.1…AC-15.5 | §16.3 |
| Data Impact | §8.4, §9 |
| RPT-13 / RPT-27 | §13 |
