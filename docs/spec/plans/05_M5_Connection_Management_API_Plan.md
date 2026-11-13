# M5 — Connection Management — API Plan

| | |
|---|---|
| **Module** | M5 — Connection Management (spec `docs/spec/05_M5_Connection_Management.md`) |
| **Document type** | API implementation plan: contracts, data model, endpoints, lifecycle, eligibility rules, validation, audit/history, sequencing, tests |
| **Build order** | #5 after Foundation + M1 + M2 + M3 + M4 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M5 turns a **lead** into a **connection** through a fixed, one-sided-first handshake:

1. **Send** a request from a lead, using the fixed message and a route/match snapshot (SCR-14.1, backed by M4's `/leads/{leadId}/connect`).
2. **List** requests the member **received** (Accept / Reject) and **sent** (Withdraw) (SCR-14.2, SCR-14.3).
3. **Accept / Reject / Withdraw** with the exact ownership rules (BR-14.5 to BR-14.7).
4. **Expire** unanswered requests after CFG-5 days (BR-14.8), by background job.
5. **Withdraw automatically** all pending requests when a route is deactivated or a member is blocked/deactivated (BR-14.9).
6. **List accepted connections** and open the **connection detail** with both routes at request time, current routes, history, disconnect, and links to chat/block/report/rate (SCR-14.4, SCR-14.5).
7. **Disconnect** an accepted connection (BR-14.11) and close its chat.
8. **Implement `IConnectionRequestGateway`** (M4 consumes a no-op today) and expose the lifecycle gateways M2/M6/M7/M8/M9/M11/M12 will consume.

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Producing / revoking leads, lead ranking/explanation, lead Viewed/Requested status | M4 / M3 |
| Public member profile, rating summary | M1 / M7 |
| Route snapshots (points, days, times, cities) and route deactivation events | M2 |
| Chat creation and messages, chat close, unread state | M6 |
| Block / Report actions and block effects | M8 |
| Notifications (request received, accepted, neutral rejection, expiry, disconnect) | M9 |
| Member deactivation / block by Admin | M10 / M12 |
| Admin connection monitoring and read-only view | M11 |
| Connection Register (RPT-10), Connection Funnel (RPT-11), Request Response Time (RPT-12) | M15 |
| Rating creation | M7 |

M5 **does not book, schedule or promise a journey** (Principle 7, Rule 8). Acceptance only opens a chat (BR-14.15).

---

## 2. Dependency and module-boundary handling

M5 is built **after M4** (leads + gateway seam exist) and **before M6/M7/M8** (which consume M5 gateways). Respect direction: M5 reads M2 route snapshots, consumes M4's lead snapshot/lifecycle, and provides seams for downstream modules.

### 2.1 Contracts consumed by M5

| Contract | Owner | Purpose |
|---|---|---|
| `ILeadSnapshotReader.GetForRequestAsync(leadId, senderMemberId)` | M4 | Exact lead + route/match snapshot handed to a new request |
| `ILeadLifecycleGateway.MarkRequestedAsync(leadId, connectionId)` / `ReopenAfterRequestClosedAsync(leadId)` | M4 | Move lead status on send and on Rejected/Withdrawn/Expired (BR-13.7) |
| `IPublicMemberProfileService.GetAsync(memberId, viewerMemberId)` | M1 | Limited public profile for the other member (no email/mobile) |
| `IRouteSnapshotReader` | M2 | Snapshot of both routes at send time; current routes for the detail |
| `IAppConfigReader` | Foundation | CFG-5, CFG-6, CFG-17, CFG-25, CFG-29 |
| `IRecordNumberGenerator` | Foundation | `CON-{nnnnnn}` shared by request and connection (BR-7.3) |
| `IMemberStatusReader` | Foundation | Active / Restricted / Inactive / Blocked check (BR-14.3) |
| `IBlockPolicyGateway` (no-op until M8) | M8-owned, M5 consumes | `IsBlockedPairAsync`, `CanReceiveRequestAsync` |
| `INotificationGateway` (no-op until M9) | M9-owned, M5 consumes | Request received/accepted/rejected-neutral/expired/disconnected |
| `IChatLifecycleGateway` (no-op until M6) | M6-owned, M5 consumes | `OpenChatAsync(connectionId, firstMessage)`, `CloseChatAsync(connectionId, reason)` |
| `IConnectionRatingReader` (no-op until M7) | M7-owned, M5 consumes | `canRate`, `alreadyRated` for the detail |

### 2.2 Contracts provided by M5 (consumed later)

| Contract | Purpose | Consumer |
|---|---|---|
| `IConnectionRequestGateway` | `GetEligibilityAsync`, `SendRequestAsync`, `GetStatesAsync` — replaces M4's no-op (BR-14.3, BR-14.4) | M4 |
| `IConnectionLifecycleGateway` | `WithdrawPendingForMemberAsync(memberId, reason)`, `EndAcceptedAsync(connectionId, reason)`, `EndForBlockAsync(a, b)` | M2 (route off), M8 (block), M10 (deactivate) |
| `IConnectionReadGateway` | `GetOpenConnectionAsync(a, b)`, `GetForChatAsync(connectionId)`, `GetSnapshotAsync(connectionId)`, `IsChatOpenAsync` | M6, M7, M11, M12 |
| `IConnectionQueryService` | Sent/received/connection lists, detail, history | M5 controller |
| `IConnectionExpiryService` | Expire due Pending requests (used by the background job) | `ConnectionExpiryJob` |

Rules:

- M5 depends on M6/M7/M8/M9 **only through the tiny gateways above**; each no-op returns a safe default (`chatId = null`, `canRate = false`, `isBlocked = false`, notification dropped), so M5 ships and is testable before those modules.
- When M6/M7/M8/M9 land they register real implementations; M5 endpoint contracts do not change.
- The **send** endpoint remains M4's `POST /leads/{leadId}/connect`; M5 owns the rules and record creation behind `IConnectionRequestGateway`. M5 does not duplicate that route.
- M5 **never creates, edits or deletes a lead**; it only calls `ILeadLifecycleGateway` (BR-13.7, BR-12.7).
- Connection data are **never deleted** (BR-8.1, BR-14.14); closed records stay read-only in history.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; `[Route("connections")]`; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interface in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` → `ResponseFilterAttribute` envelope `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | `BadRequest(new { code, message })` / object-result `StatusCode(404/409/403, new { code, message })`; **never** `Forbid()` / `Challenge()` |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new EF-managed tables; `member_Master` untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id`, decimal precision, audit columns, `IsDeleted`, per-column indexes |
| Validation | No FluentValidation; validate in `ConnectionRequestService` / `ConnectionLifecycleService` |
| Current user | Derive the member id from the token; never accept a `memberId` in the body |
| Background job | Plain `BackgroundService` + `Cronos` parser, config-driven (`ConnectionExpiry` section), `IServiceScopeFactory` per tick |

---

## 4. Data model

### 4.1 New table `raah_Connection`

One row per **request** and per **connection** — the same record through its lifecycle; request and connection share one `CON` number (BR-7.3).

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `ConnectionNumber` | nvarchar(20) | `CON-000087`, generated on send, never reused (BR-7.1/7.2) |
| `SenderMemberId` | bigint | FK → `member_Master.Id` (never deleted) |
| `ReceiverMemberId` | bigint | FK → `member_Master.Id` |
| `LowerMemberId` | bigint | `min(sender, receiver)` — normalized pair key |
| `HigherMemberId` | bigint | `max(sender, receiver)` — normalized pair key |
| `LeadId` | bigint | FK → `raah_Lead.Id`; the lead the request came from |
| `Status` | tinyint | `ConnectionStatus` (§4.4) |
| `RequestMessage` | nvarchar(150) | Copy of CFG-25 at send time (BR-22.7); never edited |
| `SentOnUtc` | datetime2 | |
| `ExpiresOnUtc` | datetime2 | `SentOnUtc + CFG-5 days` (BR-14.8) |
| `RespondedOnUtc` | datetime2 null | Accept/Reject |
| `EndedOnUtc` | datetime2 null | Withdrawn / Disconnected / Blocked |
| `EndedByMemberId` | bigint null | Who ended (Withdraw/Disconnect/Block); null for System |
| `EndReason` | tinyint null | `ConnectionEndReason` (§4.4) |
| `SenderRouteNumber` | nvarchar(20) null | Snapshot reference for display |
| `ReceiverRouteNumber` | nvarchar(20) null | Snapshot reference for display |
| `SenderRouteVersion` | int null | Route version at send (detects "route changed", BR-14.10) |
| `ReceiverRouteVersion` | int null | Route version at send |
| `SenderRouteSnapshotJson` | nvarchar(max) null | Immutable route snapshot (points, days, times, cities) |
| `ReceiverRouteSnapshotJson` | nvarchar(max) null | Immutable route snapshot |
| `MatchSnapshotJson` | nvarchar(max) null | Per-leg match result at send (Principle 14) |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false (never deleted) |

Indexes:

- Unique `ConnectionNumber`.
- `(LowerMemberId, HigherMemberId, Status)` — one-open-connection check and cool-off lookup.
- `(SenderMemberId, Status, SentOnUtc DESC)` — Sent list / pending-limit count.
- `(ReceiverMemberId, Status, SentOnUtc DESC)` — Received list.
- Filtered `(ExpiresOnUtc)` where `Status = Pending` — expiry job scan.

Snapshots are **write-once**; they are never regenerated when a route changes (BR-14.10). Current routes come live from M2 in the detail DTO.

### 4.2 New table `raah_ConnectionHistory` (append-only)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `ConnectionId` | bigint | FK → `raah_Connection.Id` |
| `EventType` | tinyint | `ConnectionHistoryEventType` (§4.4) |
| `ActorType` | tinyint | 1 Member, 2 System |
| `ActorMemberId` | bigint null | Null when System |
| `Detail` | nvarchar(500) null | UI-safe, no contact data |
| `OccurredOnUtc` | datetime2 | |

Index: `ConnectionId, OccurredOnUtc ASC`. Members see history only for their own connections (BR-8.5, BR-14.16).

### 4.3 Where cool-off and pending-limit live

No new counters. Both are derived from `raah_Connection`:

- **Pending limit (CFG-6):** `COUNT(*) WHERE SenderMemberId = me AND Status = Pending`.
- **Cool-off (CFG-17):** latest `Status = Rejected` row for the ordered pair; cool-off active while `RespondedOnUtc + CFG-17 days > now`. Applies **only after a Rejection** (A-36), never after Withdraw/Expire.
- **One open connection per pair (A-35):** any row with `Status IN (Pending, Accepted)` for the pair.

Keeping derivations query-based avoids denormalized drift and satisfies BR-8.1 (nothing deleted).

### 4.4 Enums

```
ConnectionStatus            { Pending = 1, Accepted = 2, Rejected = 3, Withdrawn = 4,
                              Expired = 5, Disconnected = 6, Blocked = 7 }
ConnectionEndReason         { SenderWithdrew = 1, ReceiverRejected = 2, SystemExpiry = 3,
                              RouteDeactivated = 4, MemberDeactivated = 5, MemberBlocked = 6,
                              MemberDisconnected = 7 }
ConnectionHistoryEventType  { RequestSent = 1, Accepted = 2, Rejected = 3, Withdrawn = 4,
                              Expired = 5, Disconnected = 6, Blocked = 7, RouteChanged = 8 }
ConnectionActorType         { Member = 1, System = 2 }
ConnectionDirection         { Received = 1, Sent = 2 }
LeadConnectionState         { None = 1, RequestSentPending = 2, RequestReceivedPending = 3,
                              Connected = 4, CoolOff = 5, Blocked = 6, Withdrawn = 7 }   // shared with M4
```

Migration (indicative): `20260105000000_AddRaahConnectionAndHistory`.

---

## 5. DTOs (Binsera.Core)

Namespace: `Binsera.Core.Connections.Models.Raah`.

### 5.1 `ConnectionEligibilityDto` (returned to M4 preflight / `GetEligibilityAsync`)

```
state                  string            // LeadConnectionState name
canConnect             bool
daysLeft               int?              // cool-off or pending-limit countdown
disabledReasonCode     string?           // "PENDING_LIMIT" | "COOL_OFF" | "RESTRICTED" | "BLOCKED" | "ALREADY_OPEN" | "NOT_AVAILABLE"
disabledReasonMessage  string?
requestMessage         string            // CFG-25 fixed text
incomingConnectionId   long?             // RequestReceivedPending → offer Accept (BR-14.4)
outgoingConnectionId   long?
```

### 5.2 `PublicConnectionStateDto` (bulk for M4 lead list)

```
otherMemberId, state, canConnect, daysLeft, outgoingConnectionId, incomingConnectionId
```

`GetStatesAsync(memberId, otherMemberIds)` returns one per matched member; `None` when no row.

### 5.3 `ConnectionRequestListItemDto` (SCR-14.2 / SCR-14.3)

```
connectionId           long
connectionNumber       string            // CON-nnnnnn
direction              string            // "Received" | "Sent"
status                 string            // "Pending" | "Accepted" | "Rejected" | ...
statusLabel            string            // UI wording, incl. neutral "Not accepted" for a rejected sender
otherMember            PublicMemberCardDto      // M1 limited card; no email/mobile
requestMessage         string            // stored copy
sentOn                 DateTime
expiresOn              DateTime
daysLeft               int?              // Pending only
respondedOn            DateTime?
matchSummary           ConnectionMatchSummaryDto
routeChangedSinceRequest bool            // Received only (BR-14.10)
actions                { canAccept, canReject, canWithdraw }
```

`ConnectionMatchSummaryDto`:

```
going                  LegMatchSummaryDto
return                 LegMatchSummaryDto    // null/absent when no return leg
```

`LegMatchSummaryDto`:

```
legType, matchType ("FullRoute" | "UsefulSegment" | "NoMatch"),
sharedPoints [ { order, name, matchedName } ],
commonDays string[],
senderTime, receiverTime, timeGapMinutes int
```

### 5.4 `ConnectionListItemDto` (SCR-14.4 My Connections)

```
connectionId, connectionNumber, status, statusLabel ("Connected" | "Disconnected" | "Ended"),
otherMember PublicMemberCardDto,
acceptedOn DateTime, endedOn DateTime?,
chat { chatId long?, isOpen bool, canOpen bool }
```

### 5.5 `ConnectionDetailDto` (SCR-14.5)

```
connectionId, connectionNumber,
status, statusLabel,
viewerRole             string            // "Sender" | "Receiver"
otherMember            PublicMemberProfileDto      // M1 limited profile (never private)
requestMessage         string
sentOn, respondedOn?, expiresOn, endedOn?, endedBy ("You" | "Other" | "System" | null)
routeAtRequest         { sender PublicRouteSummaryDto, receiver PublicRouteSummaryDto }
currentRoutes          { sender PublicRouteSummaryDto, receiver PublicRouteSummaryDto }
routeChangedSinceRequest bool
matchAtRequest         ConnectionMatchSummaryDto
chat                   { chatId, isOpen, canOpen }
rating                 { canRate, alreadyRated }
actions                { canAccept, canReject, canWithdraw, canDisconnect,
                         canBlock, canReport, canOpenChat, canRate }
```

### 5.6 Request / result DTOs

| DTO | Fields |
|---|---|
| `ConnectionActionResultDto` | `connectionId`, `connectionNumber`, `status`, `statusLabel`, `otherMemberName`, `chatId?` |
| `SendRequestResultDto` | `connectionId`, `connectionNumber`, `connectionStatus` ("Pending"), `leadStatus` ("Requested") |
| `ConnectionHistoryItemDto` | `occurredOnUtc`, `eventType`, `actor` ("You" \| "Other" \| "System"), `detail` |

### 5.7 Status labels (neutral, never blaming — UP-6)

| Stored status | Shown to sender | Shown to receiver |
|---|---|---|
| Pending | "Pending · waiting {n} days" | "Pending · {n} days left" |
| Accepted | "Connected" | "Connected" |
| Rejected | **"Your request was not accepted"** | "Rejected" (history only) |
| Withdrawn | "Withdrawn" | "Withdrawn" |
| Expired | "Expired" | "Expired" |
| Disconnected | "Ended" | "Ended" |
| Blocked | "Ended" (never says who blocked — BR-17.2) | "Ended" |

---

## 6. Endpoint catalogue

Base route: `/connections`. All endpoints require an authenticated **Member** token and operate on connections the member belongs to (BR-14.16). Send stays at M4's `POST /leads/{leadId}/connect`.

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/connections/requests/received?status=&limit=&cursor=` | Requests received (Accept/Reject) | SCR-14.3 | 200 `ConnectionRequestListResponseDto` |
| 2 | GET | `/connections/requests/sent?status=&limit=&cursor=` | Requests sent (Withdraw) | SCR-14.2 | 200 `ConnectionRequestListResponseDto` |
| 3 | GET | `/connections?status=&limit=&cursor=` | My accepted / closed connections | SCR-14.4 | 200 `ConnectionListResponseDto` |
| 4 | GET | `/connections/{connectionId}` | Connection detail (routes, snapshot, actions) | SCR-14.5 | 200 `ConnectionDetailDto` |
| 5 | POST | `/connections/{connectionId}/accept` | Receiver accepts; chat opens | SCR-14.3 | 200 `ConnectionActionResultDto` |
| 6 | POST | `/connections/{connectionId}/reject` | Receiver rejects; cool-off starts | SCR-14.3 | 200 `ConnectionActionResultDto` |
| 7 | POST | `/connections/{connectionId}/withdraw` | Sender withdraws while Pending | SCR-14.2 | 200 `ConnectionActionResultDto` |
| 8 | POST | `/connections/{connectionId}/disconnect` | Either ends an Accepted connection | SCR-14.5 | 200 `ConnectionActionResultDto` |
| 9 | GET | `/connections/{connectionId}/history?limit=&cursor=` | Connection history timeline | SCR-14.5 | 200 `ConnectionHistoryItemDto[]` |

**Connection id.** Routes use numeric `connectionId`. DTOs expose the user-facing `connectionNumber`. Keep one canonical key.

Forward-looking (not part of the M5 public API):

- Block/Report endpoints are M8; M5 only reports availability / transitions to `Blocked` when M8 calls its gateway.
- Chat endpoints are M6; M5 exposes `chatId` and `isOpen` through the M6 gateway.
- Admin connection viewing is M11.
- Rating is M7; M5 only reports `canRate`/`alreadyRated`.

---

## 7. Endpoint details

### 7.1 `GET /connections/requests/received`

1. Resolve `ReceiverMemberId` from the token.
2. Base set: `ReceiverMemberId = me`.
3. `status` filter: `pending` (default) | `history` (Rejected, Withdrawn, Expired, Blocked) | `all`.
4. Order `SentOnUtc DESC, ConnectionNumber ASC`; keyset paging.
5. Project `otherMember` through M1 (public card only — AC-14.1/BR-4.9).
6. `daysLeft` for Pending = `max(0, ceil((ExpiresOnUtc - now)/day))`, computed from CFG-5.
7. `routeChangedSinceRequest` = sender's **current** route version ≠ `SenderRouteVersion` (BR-14.10).
8. `actions.canAccept/canReject` = status Pending; both belong to the receiver (V-35).

### 7.2 `GET /connections/requests/sent`

1. Resolve `SenderMemberId`.
2. Base set: `SenderMemberId = me`.
3. Same status filter and ordering.
4. `actions.canWithdraw` = Pending; `statusLabel` uses the neutral rejection wording for a Rejected row (UP-6).
5. `routeChangedSinceRequest` is not shown to the sender (only the receiver sees it, BR-14.10).

### 7.3 `GET /connections`

1. Base set: `SenderMemberId = me OR ReceiverMemberId = me`.
2. `status` filter: `open` (default, Accepted) | `closed` (Disconnected, Blocked) | `all`.
3. Order: `RespondedOnUtc DESC` (accepted/ended recency), then `ConnectionNumber`.
4. `otherMember` = the side that is not the viewer.
5. `chat` from `IChatLifecycleGateway` (no-op → `chatId = null, canOpen = false`). The UI's real chat list is M6; this endpoint feeds the My Connections view and chat-header link (SCR-14.4).

### 7.4 `GET /connections/{connectionId}`

1. Load the connection; if it does not exist **or** the viewer is neither sender nor receiver → `404 CONNECTION_NOT_FOUND` **and log the denied attempt** (BR-4.2, BR-14.16, AC-14.2).
2. Build:
   - `otherMember` via `IPublicMemberProfileService.GetAsync(otherMemberId, viewerMemberId)` (public fields only — BR-4.9).
   - `routeAtRequest` from the stored snapshot JSON (never regenerated).
   - `currentRoutes` via `IRouteSnapshotReader` (live; BR-14.10).
   - `routeChangedSinceRequest` from version comparison.
   - `matchAtRequest` from `MatchSnapshotJson`.
   - `chat` from M6 gateway; `rating` from M7 reader.
   - `actions`: Accept/Reject only for the receiver and only Pending; Withdraw only for the sender and only Pending; Disconnect only Accepted; Block/Report via M8 (lead/connection established — always available per BR-17.4).
3. Return `ConnectionDetailDto`.

Opening the detail is side-effect-free (no status change). Accepting happens only through `POST /accept`.

### 7.5 `POST /connections/{connectionId}/accept`

1. Load; viewer must be the **receiver** and the record Pending (V-35, BR-14.5); else `403 CONNECTION_NOT_RECEIVER` / `409 CONNECTION_NOT_PENDING`.
2. Re-check reachability (block no-op until M8): pair not blocked, sender still Active; if the sender became Inactive the row is already Withdrawn by the lifecycle hook (`409 CONNECTION_ALREADY_CLOSED`, AC-14.3/SC-14.12).
3. In one transaction (`IUnitOfWork`):
   - `Status = Accepted`, `RespondedOnUtc = now`.
   - Write history `Accepted`.
   - Call `IChatLifecycleGateway.OpenChatAsync(connectionId, RequestMessage)` — the fixed message becomes the **first chat message** (BR-14.17, AC-14.10). If M6 is absent, store nothing and return `chatId = null` (chat opens when M6 lands).
   - Notify both via `INotificationGateway` (M9 no-op).
4. Return `ConnectionActionResultDto` with `status = "Accepted"`, `chatId`, `otherMemberName`.

Accepting does **not** book/schedule anything; the UI shows BR-14.15 copy.

### 7.6 `POST /connections/{connectionId}/reject`

1. Viewer must be the **receiver** and Pending; else `403`/`409`.
2. In one transaction: `Status = Rejected`, `RespondedOnUtc = now`, `EndedOnUtc = now`, `EndReason = ReceiverRejected`; history `Rejected`; call `ILeadLifecycleGateway.ReopenAfterRequestClosedAsync(LeadId)` → lead back to Viewed (BR-13.7/BR-14.6); notify sender with the **neutral** message only (no reason, UP-6).
3. Cool-off is derived automatically (§4.3) and applies to the sender for CFG-17 days (A-36).
4. Return `ConnectionActionResultDto` (`status = "Rejected"`).

No chat is created. The sender sees "Your request was not accepted" (BR-14.6, SC-14.4).

### 7.7 `POST /connections/{connectionId}/withdraw`

1. Viewer must be the **sender** and Pending (V-35, BR-14.7); else `403 CONNECTION_NOT_SENDER` / `409 CONNECTION_NOT_PENDING`.
2. In one transaction: `Status = Withdrawn`, `EndedOnUtc = now`, `EndedByMemberId = me`, `EndReason = SenderWithdrew`; history `Withdrawn`; `ILeadLifecycleGateway.ReopenAfterRequestClosedAsync(LeadId)` → Viewed; **no cool-off**; no notification to the receiver (SC-14.5).
3. Return `ConnectionActionResultDto` (`status = "Withdrawn"`).

### 7.8 `POST /connections/{connectionId}/disconnect`

1. Viewer must be sender **or** receiver; status must be Accepted (BR-14.11); else `403`/`409 CONNECTION_NOT_ACCEPTED`.
2. In one transaction: `Status = Disconnected`, `EndedOnUtc = now`, `EndedByMemberId = me`, `EndReason = MemberDisconnected`; history `Disconnected`; `IChatLifecycleGateway.CloseChatAsync(connectionId, "Connection ended")`; notify the other member (neutral).
3. Both may later get a **new** request through a fresh lead (BR-14.11) — no cool-off after disconnect.
4. History is kept and read-only (BR-14.14).

### 7.9 `GET /connections/{connectionId}/history`

- Viewer must belong to the connection; else `404` + log.
- Return `raah_ConnectionHistory` by `OccurredOnUtc ASC`.
- Map `ActorMemberId == viewer` → "You"; another member → "Other"; null → "System".
- `Detail` is UI-safe, no contact data/tokens/IPs.

---

## 8. Eligibility rules (gateway, enforced on send)

`GetEligibilityAsync(senderMemberId, receiverMemberId, leadId)` is the single source of truth (called by M4's preflight and send). Order of checks:

| # | Rule | Result state / reason |
|---|---|---|
| 1 | Lead belongs to sender and is connectable (not Dismissed/Withdrawn) | `None` is not returned; M4 handles lead state → `LEAD_NOT_AVAILABLE` |
| 2 | Sender ≠ receiver | `SELF_REQUEST` (should not occur) |
| 3 | Sender is Active and not Restricted (BR-14.3, BR-21.6) | `RESTRICTED` → `MEMBER_RESTRICTED` |
| 4 | Receiver is Active (not Blocked/Inactive) (V-32) | `BLOCKED` → not available |
| 5 | Neither blocked the other (M8 gateway; no-op until M8) (V-32) | `BLOCKED` |
| 6 | No open connection (Pending/Accepted) between the pair (A-35) | If a **pending request from the receiver to the sender** exists → `RequestReceivedPending` + `incomingConnectionId` (BR-14.4/AC-14.6); if outgoing pending → `RequestSentPending`; if accepted → `Connected` |
| 7 | Cool-off after rejection not running (CFG-17, A-36) | `CoolOff` + `daysLeft` |
| 8 | Sender's Pending count < CFG-6 (V-31) | `disabledReasonCode = PENDING_LIMIT` |
| 9 | Otherwise | `None`, `canConnect = true`, `requestMessage = CFG-25` |

`SendRequestAsync` re-runs **all** checks atomically, then creates the row with a fresh `CON` number, snapshot and message (V-37), marks the lead Requested via `ILeadLifecycleGateway`, and notifies the receiver. If M5 is not deployed, M4's no-op gateway returns `state = None, canConnect = false, NOT_AVAILABLE`.

**Concurrency.** Two parallel sends for the same pair must not both create a row: use a transaction plus the unique/`(LowerMemberId, HigherMemberId, Status)` open-pair guard (apply a serializable or `UPDLOCK` check, or a filtered unique index on open states — state changes across rows make a partial unique index on `Status IN (Pending, Accepted)` tricky; prefer an app-level transaction with row locks on both member ids).

---

## 9. Connection status lifecycle (spec §9.6)

| From | To | Who | M5 mechanism |
|---|---|---|---|
| (none) | Pending | System | `SendRequestAsync` via M4 `/connect` (BR-14.2) |
| Pending | Accepted | Receiver | `POST /accept`; chat opens (BR-14.5) |
| Pending | Rejected | Receiver | `POST /reject`; cool-off; lead → Viewed (BR-14.6) |
| Pending | Withdrawn | Sender / System | `POST /withdraw`; or `WithdrawPendingForMemberAsync` on route off / member blocked/deactivated (BR-14.9) |
| Pending | Expired | System | `ConnectionExpiryJob` after CFG-5 (BR-14.8) |
| Accepted | Disconnected | Either | `POST /disconnect`; chat closes (BR-14.11) |
| Accepted | Blocked | Either (M8) | M8 calls `EndForBlockAsync`; M5 sets Blocked + closes chat (BR-14.12) |

All transitions are final except the pair may start again via a **new fresh lead** after Rejected (after cool-off), Withdrawn, Expired, or Disconnected — always a **new** `CON` row (BR-14.14).

---

## 10. Validation matrix

| ID / rule | Rule | Where | Error |
|---|---|---|---|
| BR-14.1 | Request only from a lead | Send (gateway) | `LEAD_NOT_AVAILABLE` (409) |
| BR-14.2 | Either side can send | Send | (both directions permitted) |
| BR-14.3 | Active, not restricted, not blocked, < limit, no open pair, no cool-off | Send | codes in §7/§13 |
| V-31 | ≤ CFG-6 pending | Send | `REQUEST_LIMIT_REACHED` (409) |
| V-32 | No self/blocked/inactive | Send | `MEMBER_RESTRICTED` / not-available |
| V-33 | One open connection per pair | Send | `OPEN_CONNECTION_EXISTS` (409) |
| V-34 | ≤ CFG-17 after rejection | Send | `COOLOFF_ACTIVE` (409 + daysLeft) |
| V-35 | Only receiver accepts/rejects; only sender withdraws while Pending | Accept/Reject/Withdraw | `CONNECTION_NOT_RECEIVER`/`CONNECTION_NOT_SENDER` (403), `CONNECTION_NOT_PENDING` (409) |
| V-36 | Pending → Expired after CFG-5 | Expiry job | — |
| V-37 | Fixed request message on every request | Send | `REQUEST_MESSAGE_REQUIRED` (500 if CFG empty) |
| BR-14.16 | Only members of the connection can read it | Detail/History | `404` + log |
| — | `status` filter ∈ {pending, history, all} | Lists | `400 CONNECTION_FILTER_INVALID` |
| — | `limit` within bounds (default 20) | Lists | clamp / `400 CONNECTION_LIMIT_INVALID` |
| — | Cursor matches filters | Lists | `400 CURSOR_INVALID` |

No validation framework exists; implement in the services. Do not add FluentValidation as a drive-by change.

---

## 11. Background jobs and automatic transitions

### 11.1 `ConnectionExpiryJob`

- `BackgroundService` + `Cronos`, config section `ConnectionExpiry { Enabled, Cron (default daily 01:00), TimeZone }`.
- Each tick: find `Status = Pending AND ExpiresOnUtc <= now` (honours the **current** CFG-5, including a shortened value — BR-22.8).
- For each, in a transaction: `Status = Expired`, `EndReason = SystemExpiry`, history `Expired`, `ILeadLifecycleGateway.ReopenAfterRequestClosedAsync`, notify sender (M9). Receiver is **not penalised** and **no cool-off** applies (BR-14.8).
- Idempotent and chunked (batch 200) to avoid long locks.

### 11.2 Lifecycle hooks (no job)

| Trigger | Caller | M5 action |
|---|---|---|
| Route deactivated (BR-11.14) | M2 | `WithdrawPendingForMemberAsync(memberId, RouteDeactivated)` — withdraws **sent and received** Pending (BR-14.9, AC-14.7) |
| Member deactivated | M10 | `WithdrawPendingForMemberAsync(memberId, MemberDeactivated)` + `EndAcceptedAsync(..., MemberDeactivated)` if required by the deactivation rule |
| Member blocked by Admin | M8/M12 | `EndForBlockAsync(a, b)` → Blocked; withdraw any Pending |
| Member blocks another | M8 | `EndForBlockAsync(a, b)` → Blocked (BR-14.12) |

`WithdrawPendingForMemberAsync` moves Pending → Withdrawn, writes history, reopens each lead via M4, and notifies each counterparty where the spec requires it (neutral). Accepted connections/chats stay unless the triggering rule says otherwise (BR-14.9).

---

## 12. Config usage

| CFG | Use in M5 |
|---|---|
| CFG-5 | `ExpiresOnUtc` on send; expiry job window |
| CFG-6 | Pending outgoing limit (V-31) |
| CFG-17 | Cool-off after rejection (V-34, A-36) |
| CFG-25 | Fixed request message copied onto the row and shown in preflight (V-37) |
| CFG-29 | Expiry reminder — **consumed by M9**, not M5; M5 supplies `ExpiresOnUtc` |

Read config through the Foundation reader; no hard-coded defaults in M5.

---

## 13. Error responses

Use `BadRequest(new { code, message })` or object-result `StatusCode(404/409/403, new { code, message })`. Never `Forbid()`/`Challenge()`. Keep messages aligned with UI Plan §7.2 and the M5 UI plan microcopy.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `CONNECTION_NOT_FOUND` | 404 | "This request is not available." |
| `CONNECTION_NOT_PENDING` | 409 | "This request has already been answered." |
| `CONNECTION_NOT_ACCEPTED` | 409 | "This connection is not open." |
| `CONNECTION_ALREADY_CLOSED` | 409 | "This request is no longer open." |
| `CONNECTION_NOT_RECEIVER` | 403 | "Only the receiver can answer this request." |
| `CONNECTION_NOT_SENDER` | 403 | "Only the sender can withdraw this request." |
| `REQUEST_MESSAGE_REQUIRED` | 500 | "The request message is not configured." |
| `REQUEST_LIMIT_REACHED` | 409 | "You have too many pending requests. Wait for answers or withdraw one." (CFG-6) |
| `COOLOFF_ACTIVE` | 409 | "You can request again in *n* days." (CFG-17) |
| `OPEN_CONNECTION_EXISTS` | 409 | "*Name* has already sent you a request. Accept it?" (BR-14.4) |
| `MEMBER_RESTRICTED` | 409 | "You cannot send new requests until *date*." |
| `MEMBER_BLOCKED` | 409 | "You cannot send a request to this person." |
| `LEAD_NOT_AVAILABLE` | 409 | "This person is no longer available." |
| `CONNECTION_FILTER_INVALID` | 400 | "Choose a valid filter." |
| `CONNECTION_LIMIT_INVALID` | 400 | "Choose a valid page size." |
| `CURSOR_INVALID` | 400 | "Refresh the list and try again." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

---

## 14. Privacy and security

- The other member is always projected through `IPublicMemberProfileService`; DTOs contain **no** email, mobile, IP, device, login history, reports, admin notes or blocks. Add a serialization test that fails if those keys appear (BR-4.9).
- Membership is derived from the token; a foreign `connectionId` returns `404` and logs the attempt (BR-4.2, BR-14.16, AC-14.2).
- The fixed message is **stored, read-only** and never client-supplied (D-23, V-37).
- Route snapshots are immutable; the detail separately shows the current route with a "route changed" flag (BR-14.10, Principle 14).
- Accept/Reject/Withdraw/Disconnect are state-checked and transaction-scoped; double-submits are idempotent (a second accept on an Accepted row returns `409 CONNECTION_NOT_PENDING`, never duplicate chat) or rejected.
- Chat creation happens **only** on accept (AC-14.3, AC-14.10); a rejected/withdrawn/expired request never opens a chat.
- No connection data are deleted (BR-8.1); closed records stay read-only in history (BR-14.14).
- Notifications never reveal a block or a rejection reason (BR-17.2, UP-6).

---

## 15. Build sequence (M5)

| Step | Work | Output |
|---|---|---|
| 1 | Enums, DTOs, `raah_Connection` + `raah_ConnectionHistory` migration | Compiles, migration applies |
| 2 | `ConnectionRequestService` eligibility + send (replacing M4 no-op); numbering + snapshot + message | Send works; AC-14.1/14.5/14.6/14.8/14.9 |
| 3 | `ConnectionQueryService` received/sent/connection lists + detail with snapshot vs current routes | SCR-14.2/14.3/14.4/14.5 read |
| 4 | Accept / Reject / Withdraw lifecycle + lead reopen + neutral notifications | AC-14.2/14.3/14.10 |
| 5 | Disconnect + chat-close hook (no-op until M6) | BR-14.11 |
| 6 | `IConnectionLifecycleGateway.WithdrawPendingForMemberAsync` + wire M2 route-off hook | AC-14.7 / BR-14.9 |
| 7 | `ConnectionExpiryJob` + config + notifications | AC-14.4 / BR-14.8 |
| 8 | History read/write + audit events | BR-8.2/8.5 |
| 9 | Controller endpoints + error codes + privacy tests | Postman-ready |
| 10 | Wire M6 chat open/close, M7 rating, M8 block, M9 notifications as they land | End-to-end loop |
| 11 | Performance: filtered expiry index, pair index, keyset lists | Meets paging/expiry latency |

Suggested folders:

```
Binsera.Core/Connections/
  Domain/Raah/Connection.cs, ConnectionHistory.cs
  Enums/ConnectionStatus.cs, ConnectionEndReason.cs, ConnectionHistoryEventType.cs,
        ConnectionDirection.cs
  Models/Raah/ConnectionEligibilityDto.cs, ConnectionRequestListItemDto.cs,
               ConnectionDetailDto.cs, ConnectionActionResultDto.cs, *ResponseDto.cs
  Contracts/IConnectionQueryService.cs, IConnectionExpiryService.cs,
            IConnectionLifecycleGateway.cs, IConnectionReadGateway.cs
  Contracts/Gateways/IConnectionRequestGateway.cs   (moved/shared from Leads)
Binsera.Application/Connections/Services/
  ConnectionRequestService.cs, ConnectionLifecycleService.cs, ConnectionQueryService.cs,
  ConnectionExpiryService.cs, ConnectionHistoryWriter.cs
  Gateways/NoOpNotificationGateway.cs, NoOpChatLifecycleGateway.cs,
           NoOpBlockPolicyGateway.cs, NoOpConnectionRatingReader.cs
Binsera.API/Controllers/Connections/ConnectionController.cs
Binsera.API/BackgroundServices/ConnectionExpiryJob.cs
Binsera.Persistence/Configurations/Raah/ConnectionConfiguration.cs,
                     ConnectionHistoryConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahConnectionAndHistory.cs
```

---

## 16. Test plan

### 16.1 Unit

- Eligibility: each check in §8 in isolation and in precedence order; no-op M8/M6 behaves safely.
- Send: creates Pending with CON number, snapshot, CFG-25 message, ExpiresOn = Sent + CFG-5; marks lead Requested atomically; duplicate pair blocked (A-35).
- Pending limit: 10 pending (CFG-6) rejects the 11th with `REQUEST_LIMIT_REACHED`.
- Cool-off: rejection starts CFG-17; withdraw/expire does **not**; countdown days correct (A-36).
- Accept: receiver only; sets Accepted + RespondedOn; opens chat with the fixed message as first message; no-op M6 keeps status Accepted.
- Reject: receiver only; neutral outcome; lead → Viewed; no chat.
- Withdraw: sender only, Pending only; lead → Viewed; no cool-off.
- Disconnect: either member, Accepted only; chat close called; history kept.
- Expiry: due rows expire; sender notified; no receiver penalty; shortened CFG-5 expires already-due rows.
- Auto-withdraw: route deactivation / block / member deactivation withdraws sent **and** received Pending, keeps Accepted (AC-14.7).
- History: correct events, order, actor mapping ("You"/"Other"/"System").
- Labels: rejected sender sees "Your request was not accepted"; blocked participant sees "Ended".
- Privacy: DTOs never contain email/mobile keys.

### 16.2 Integration / API

- Every endpoint requires auth; foreign `connectionId` → 404 + logged; missing token → 401.
- Envelope present; `data` matches DTO; paging keyset no overlap/gap.
- Migration applies cleanly and does not touch `member_Master`.
- M4 `/leads/{leadId}/connect` now creates a real Pending row via M5 gateway; preflight maps all states.
- Lead status: Requested on send; Viewed after reject/withdraw/expire.
- Chat open on accept / close on disconnect and block (with M6 fake).
- Route-changed note appears for the receiver when the sender's route version differs (BR-14.10).
- Notifications dispatched to the right party with neutral wording (with M9 fake).

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-14.1 Either side can send | §7, §8 (direction-agnostic send) |
| AC-14.2 Only receiver accepts | §7.5, §8, §14 foreign-id log |
| AC-14.3 Chat only on acceptance | §7.5, §7.6, §14 |
| AC-14.4 Expiry | §11.1 |
| AC-14.5 Pending limit | §8 step 8, §13 |
| AC-14.6 One open per pair | §8 step 6, §7 preflight |
| AC-14.7 Deactivation withdraws pending only | §11.2, §16.1 |
| AC-14.8 Cool-off | §4.3, §8 step 7 |
| AC-14.9 Fixed message | §8, §7.5, V-37 |
| AC-14.10 Message reaches chat only on accept | §7.5, §7.6 |

Scenarios SC-14.1…SC-14.13 map to the endpoints and lifecycle transitions above; SC-14.3 (both press Connect) is handled by §8 step 6 + M4 preflight.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| — | Connection list page size (no CFG defined)? | Fixed default 20 with keyset "Show more" |
| — | Store route/match snapshot as JSON vs child tables? | JSON columns (immutable, simpler); revisit if reporting needs querying |
| — | Does the send endpoint live in M4 or M5? | Stays at M4 `/leads/{leadId}/connect`; M5 owns rules via gateway |
| — | Should the receiver be notified on Withdraw? | No (noise); sender's own record only |
| — | Should disconnect end any pending new request too? | No; only the accepted connection ends |
| — | Do Admin deactivation/block use the same `WithdrawPendingForMemberAsync`? | Yes, with distinct `EndReason` values |
| — | Should expired requests show a cool-off? | No (BR-14.8) |
| — | Bulk `GetStatesAsync` or per-lead preflight on the lead list? | Bulk gateway preferred for list performance |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Field spec (Connection) | §4.1, §5 |
| BR-14.1 request from lead only | §8 step 1 |
| BR-14.2 either side can send | §8, §16.3 AC-14.1 |
| BR-14.3 send eligibility | §8 |
| BR-14.4 one open connection per pair / accept incoming | §8 step 6, §7.5 |
| BR-14.5 Accept | §7.5, §9 |
| BR-14.6 Reject + neutral + cool-off | §7.6, §4.3 |
| BR-14.7 Withdraw | §7.7 |
| BR-14.8 Expiry | §11.1 |
| BR-14.9 Pending belongs to Active routes / auto-withdraw | §11.2 |
| BR-14.10 Route snapshot + changed note | §4.1, §7.4 |
| BR-14.11 Disconnect | §7.8 |
| BR-14.12 Block ends connection | §9, §11.2 |
| BR-14.13 Any number of Accepted | §8 (no cap on Accepted) |
| BR-14.14 Never deleted / history | §4.2, §9 |
| BR-14.15 Accept = chat only | §7.5 |
| BR-14.16 Own connections only + logged | §7.4, §14 |
| BR-14.17 Fixed message | §5.1, §7.5, §8 |
| Status lifecycle §9.6 | §9 |
| CFG-5/6/17/25 (+29) | §12 |
| V-31…V-37 | §10 |
| SC-14.1…SC-14.13 | §16.2/16.3 |
| AC-14.1…AC-14.10 | §16.3 |
| Data Impact | §7.5–7.8, §9, §11 |
| RPT-10/11/12 | M15 reads M5 data; no M5 report endpoint |
