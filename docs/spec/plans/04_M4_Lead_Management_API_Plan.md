# M4 — Lead Management — API Plan

| | |
|---|---|
| **Module** | M4 — Lead Management (spec `docs/spec/04_M4_Lead_Management.md`) |
| **Document type** | API implementation plan: contracts, data model, endpoints, filters, ranking/paging, lifecycle, validation, audit/history, sequencing, tests |
| **Build order** | #4 after Foundation + M1 + M2 + M3 (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M4 turns the machine-produced lead rows (M3) into a member feature:

1. **List** the member's own leads, ranked by the fixed keys, with Leg and Gender filters and keyset "Show more" (SCR-13.1).
2. **Detail** a lead: limited public profile, route comparison, "Why this match", and actions (SCR-13.2).
3. **Dismiss / Undo** a lead and list dismissed leads (SCR-13.3).
4. **Start a connection request** from a lead through the M5 boundary (SCR-14.1).
5. Keep a **History tab** for each lead (BR-13.10).

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Producing / revoking leads, ranking components, matching explanation data | M3 |
| Public member profile, rating summary | M1 / M7 |
| Route snapshots (points, days, times, cities) | M2 |
| Creating the connection request, accept/reject/withdraw/expiry, cool-off, pending limit | M5 |
| Block and Report actions | M8 |
| Admin lead monitoring | M11 |
| Notifications (new lead, request states) | M9 |
| Lead Register (RPT-8), Lead Conversion (RPT-9) | M15 |
| Requests / Dismissed segment shell and Request cards | M5 (Requests), M4 (Dismissed) |

M4 **never creates, edits or deletes a match**. No one can add a lead by hand (BR-13.1, BR-4.6).

---

## 2. Dependency and module-boundary handling

M4 is built **after M3** (leads exist) and **before M5** (connect) and M8 (block/report). Respect direction: M4 reads M3's tables, consumes M1/M2, and provides seams for M5.

### 2.1 Contracts consumed by M4

| Contract | Owner | Purpose |
|---|---|---|
| `raah_Lead`, `raah_LeadLegResult`, `raah_LeadPointPair`, `raah_LeadCommonDay` | M3 | Read ranked leads and the stored explanation; M4 owns lifecycle transitions on `raah_Lead.Status` |
| `IPublicMemberProfileService.GetAsync(memberId, viewerMemberId)` | M1 | Limited public profile with the access check (a lead establishes the relationship) |
| `IRouteSnapshotReader` | M2 | Viewer and matched-member current routes for route comparison |
| `IConnectionRequestGateway` | M5-owned, M4 consumes | `GetEligibilityAsync(...)`, `SendRequestAsync(...)` — **no-op until M5** |
| `IInteractionPolicyGateway` (block/report availability) | M8-owned, M4 consumes | `CanBlockAsync`, `CanReportAsync`, `IsBlockedPairAsync` — **no-op until M8** |
| `IAppConfigReader` | Foundation | CFG-7, CFG-12, CFG-20, CFG-25, CFG-28 |
| `IMemberStatusReader` | Foundation | Restricted flag for the banner / connect disable |

### 2.2 Contracts provided by M4 (consumed later)

| Contract | Purpose | Consumer |
|---|---|---|
| `ILeadQueryService` | Ranked list, detail, dismissed list | M4 controller |
| `ILeadSnapshotReader.GetForRequestAsync(leadId, senderMemberId)` | Exact lead + route/match snapshot handed to a new request | M5 |
| `ILeadLifecycleGateway.MarkRequestedAsync(leadId, connectionId)` / `ReopenAfterRequestClosedAsync(leadId)` | Move lead status when a request is sent or ends Rejected/Withdrawn/Expired (BR-13.7) | M5 |
| `ILeadHistoryWriter` | Append history events | M3 (created/updated/withdrawn), M4 (viewed/dismissed/undone), M5 (requested/reopened) |
| `ILeadLifecycleGateway` (same service) | Status transitions | M5 |

Rules:

- M4 reads M3 rows; it must never duplicate or re-create a lead (BR-13.1, BR-12.7).
- M4 depends on M5/M8 **only through the tiny gateways above**. A no-op gateways returns `state = None`, `canConnect = false`, `disabledReason = "NOT_AVAILABLE"`, so M4 ships and is testable before M5/M8.
- When M5/M8 land they register real implementations; M4 endpoint contracts do not change.
- The route comparison uses M2's **current** snapshots; the "Why this match" explanation uses the **stored** leg results (M3 keeps them current on recalculation). This satisfies BR-13.11 (live view) and Principle 14 (explainable) without inventing data.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; `[Route("leads")]`; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interface in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` → `ResponseFilterAttribute` envelope `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | `BadRequest(new { code, message })` / object-result `StatusCode(409, ...)`; **never** `Forbid()` / `Challenge()` |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; EF-managed tables; `member_Master` untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id`, decimal precision, audit columns, `IsDeleted`, per-column indexes |
| Validation | No FluentValidation; validate in `LeadQueryService` / `LeadLifecycleService` |
| Current user | Derive the member id from the token (`MemberGraphId` / `UserId` per Foundation); never accept a `memberId` in the body |

---

## 4. Data model

### 4.1 Existing M3 tables M4 reads

| Table | M4 use |
|---|---|
| `raah_Lead` | Ownership (`ForMemberId`), status, ranking keys, route/city snapshots, `LeadNumber` |
| `raah_LeadLegResult` | Per-leg match type, shared-point count, time gap, departure minutes, common-day count |
| `raah_LeadPointPair` | Ordered shared stretch (names as typed) for "Why this match" and route highlight |
| `raah_LeadCommonDay` | Common travel days per leg |

M4 does **not** change how M3 writes these rows. It only updates `raah_Lead.Status` for lifecycle transitions and writes history (see 4.2).

### 4.2 New / additive structures (M4 migration)

#### `raah_LeadHistory` (append-only)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `LeadId` | bigint | FK → `raah_Lead.Id` |
| `EventType` | tinyint | `LeadHistoryEventType` |
| `ActorType` | tinyint | 1 Member, 2 System |
| `ActorMemberId` | bigint null | Null when System |
| `Detail` | nvarchar(500) null | UI-safe, no contact data |
| `OccurredOnUtc` | datetime2 | |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Index: `LeadId, OccurredOnUtc DESC`. Members see history only for their own leads (BR-8.5).

#### Additive columns on `raah_Lead` (coordinate with M3)

| Column | Type | Notes |
|---|---|---|
| `ViewedOnUtc` | datetime2 null | Set on first New→Viewed |
| `DismissedOnUtc` | datetime2 null | Set on dismiss; cleared to null on undo |

These keep the dismissed list orderable without a history join. If the team prefers not to extend M3's table, derive the order from `raah_LeadHistory` instead — either is acceptable; the API shape is unchanged.

### 4.3 Enums

```
LeadStatus          { New = 1, Viewed = 2, Dismissed = 3, Requested = 4, WithdrawnBySystem = 5 }   // M3
MatchType           { None = 0, FullRoute = 1, UsefulSegment = 2 }                                 // M3
LegType             { Going = 1, Return = 2 }                                                      // M3
LeadHistoryEventType{ Created = 1, Updated = 2, Viewed = 3, Dismissed = 4, Undone = 5,
                      Requested = 6, RequestClosed = 7, WithdrawnBySystem = 8, Revived = 9 }
LeadConnectionState { None = 1, RequestSentPending = 2, RequestReceivedPending = 3, Connected = 4,
                      CoolOff = 5, Blocked = 6, Withdrawn = 7 }
LegFilter           { Any = 0, Going = 1, Return = 2 }
GenderFilter        { Any = 0, Male = 1, Female = 2 }
```

Migration (indicative): `20260104000000_AddRaahLeadHistoryAndLeadTimestamps`.

---

## 5. DTOs (Binsera.Core)

Namespace: `Binsera.Core.Leads.Models.Raah`.

### 5.1 `LeadListItemDto`

```
leadId                long
leadNumber            string            // LED-nnnnnnn
status                string            // "New" | "Viewed" | "Requested"
matchedMember         PublicMemberCardDto
cities                { startCity, endCity }        // matched member's route cities, "Delhi → Noida"
viewerRole            string            // "Saathi" | "Raahi"
legs                  LeadLegSummaryDto[]           // Going then Return
connection            LeadConnectionStateDto
```

`PublicMemberCardDto` (never inherits the private profile; no email/mobile):

```
memberId, fullName, photoUrl, gender, journeyRole, vehicle, emailVerifiedBadge,
rating { average, count, isNewMember }, memberSince
```

`LeadLegSummaryDto`:

```
legType               string            // "Going" | "Return"
legState              string            // "Matched" | "NoMatch" | "NotEntered"
matchType             string            // "FullRoute" | "UsefulSegment" | "NoMatch"
matchLabel            string            // viewer-worded (see 5.5)
sharedPoints          [ { order, name, matchedName } ]   // ordered shared stretch, as typed
commonDays            string[]          // "Mon", "Tue", ...
viewerDepartureTime   "HH:mm"
matchedDepartureTime  "HH:mm"
timeDifferenceMinutes int
```

`LeadConnectionStateDto`:

```
state                 string            // LeadConnectionState name
canConnect            bool
daysLeft              int?              // cool-off or expiry countdown
disabledReasonCode    string?           // e.g. "PENDING_LIMIT", "COOL_OFF"
disabledReasonMessage string?
incomingConnectionId  long?             // when RequestReceivedPending → offer Accept
outgoingConnectionId  long?
```

### 5.2 `LeadListResponseDto`

```
items                 LeadListItemDto[]
meta                  LeadListMetaDto
```

`LeadListMetaDto`:

```
roleHeading           string            // "Raahis for you" | "Saathis for you"
totalAvailable        int               // non-dismissed, non-withdrawn leads (before filters)
totalFiltered         int               // after filters
shown                 int
pageSize              int               // CFG-7
hasMore               bool
nextCursor            string?           // opaque; null when no more
filters               { legEnabled: bool (CFG-20), genderEnabled: bool (CFG-12) }
applied               { leg, gender }
memberRestricted      bool
restrictedUntil       DateTime?
```

### 5.3 `LeadDetailDto`

All list fields for one lead, plus:

```
matchedMember         PublicMemberProfileDto      // M1, includes currentRoute
viewerRoute           PublicRouteSummaryDto       // M2
matchedRoute          PublicRouteSummaryDto       // M2
whyThisMatch          { perLeg: LeadWhyMatchDto[], summaryLines: string[] }
actions               { canDismiss, canUndoDismiss, canConnect, canReport, canBlock }
```

`LeadWhyMatchDto`:

```
legType, matchType, matchLabel
sharedPoints          [ { order, name, matchedName } ]
commonDays            string[]
viewerDepartureTime, matchedDepartureTime, timeDifferenceMinutes
summaryLine           string            // "Shared points: Vinay Nagar, Badarpur Metro. Common days: Mon–Fri. Time gap: 5 minutes."
```

### 5.4 Request / response DTOs

| DTO | Fields |
|---|---|
| `DismissResultDto` | `leadId`, `status` ("Dismissed") |
| `UndoDismissResultDto` | `leadId`, `status` ("Viewed") |
| `ConnectPreflightDto` | `canConnect`, `state`, `disabledReasonCode`, `disabledReasonMessage`, `requestMessage` (CFG-25), `matchedMemberName`, `incomingConnectionId?` |
| `ConnectResultDto` | `connectionId`, `connectionNumber`, `connectionStatus` ("Pending"), `leadStatus` ("Requested") |
| `LeadHistoryItemDto` | `occurredOnUtc`, `eventType`, `actor` ("You" \| "Other" \| "System"), `detail` |

### 5.5 Match labels (computed server-side, viewer-worded)

| Viewer | Full Route | Useful Segment |
|---|---|---|
| Saathi | "This Raahi covers your whole route" | "This Raahi shares part of your route" |
| Raahi | "Your route covers this Saathi's whole route" | "You share part of this Saathi's route" |

Never invert the stored result: M3 stores the same leg result on both sides; M4 only changes the wording by `viewerRole`.

---

## 6. Endpoint catalogue

Base route: `/leads`. All endpoints require an authenticated **Member** token and operate on the member's **own** leads only.

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/leads?leg=&gender=&limit=&cursor=` | Ranked list with filters + keyset paging | SCR-13.1 | 200 `LeadListResponseDto` |
| 2 | GET | `/leads/dismissed?limit=&cursor=` | Dismissed leads | SCR-13.3 | 200 `LeadListResponseDto` |
| 3 | GET | `/leads/{leadId}` | Detail + why + route comparison; marks New→Viewed | SCR-13.2 | 200 `LeadDetailDto` |
| 4 | POST | `/leads/{leadId}/dismiss` | "Not interested" | SCR-13.1/13.2 | 200 `DismissResultDto` |
| 5 | POST | `/leads/{leadId}/undo-dismiss` | Undo a dismissal | SCR-13.3 | 200 `UndoDismissResultDto` |
| 6 | GET | `/leads/{leadId}/connect-preflight` | Eligibility + fixed message before sending | SCR-14.1 | 200 `ConnectPreflightDto` |
| 7 | POST | `/leads/{leadId}/connect` | Send request (delegates to M5) | SCR-14.1 | 200 `ConnectResultDto` |
| 8 | GET | `/leads/{leadId}/history` | History tab | BR-13.10 | 200 `LeadHistoryItemDto[]` |

**Lead id.** Routes use the numeric `leadId`. DTOs expose the user-facing `leadNumber`. If the frontend prefers the number in the URL, add a lookup by `LED-…`; keep one canonical key to avoid ambiguity.

Forward-looking (not part of the M4 public API):
- Admin lead viewing, filters and export are M11.
- Block/Report endpoints are M8; M4 only reports availability via `actions`.

---

## 7. Endpoint details

### 7.1 `GET /leads`

Query:

| Param | Values | Rules |
|---|---|---|
| `leg` | `any` (default), `going`, `return` | Only applied when CFG-20 = Yes; otherwise forced to `any` |
| `gender` | `any` (default), `male`, `female` | Only applied when CFG-12 = Yes; otherwise forced to `any` |
| `limit` | 1..CFG-7 | Default CFG-7 |
| `cursor` | opaque | Must match the same `leg`+`gender` used to mint it |

Pipeline:

1. Resolve `ForMemberId` from the token.
2. Base set: leads with `Status IN (New, Viewed, Requested)` (Dismissed goes to the dismissed list; `WithdrawnBySystem` is hidden — BR-13.5/13.8).
3. Apply **Leg** filter: keep leads that have at least one `raah_LeadLegResult` with `MatchType <> None` for that leg. `Any` keeps all.
4. Apply **Gender** filter: join the matched member's gender. `Other` members appear only under `Any`.
5. Order **exactly** by the M3 ranking keys, then `LeadNumber ASC` as a stable tie-break:
   `legsMatched DESC, fullRouteLegs DESC, totalSharedPoints DESC, avgTimeGapMinutes ASC, commonDaysTotal DESC, createdDate ASC`.
6. Keyset paging from `cursor` (see 7.2); return `limit` items.
7. Compute `totalAvailable` (step 2) and `totalFiltered` (after 3–4) so the UI can show "5 found" and "2 of 5 shown".
8. `memberRestricted` / `restrictedUntil` from `IMemberStatusReader` so the banner renders without another call.
9. `connection` per item from `IConnectionRequestGateway` (bulk form preferred: `GetStatesAsync(memberId, matchedIds)`), returning `None` when M5 is absent.

Filters change **visibility only** — never status, order or data (BR-13.5/13.6, AC-13.2/13.3).

### 7.2 Keyset cursor

The cursor is an opaque base64url token over:

```
{ legsMatched, fullRouteLegs, totalSharedPoints, avgTimeGapMinutes, commonDaysTotal,
  createdDateUtc, leadNumber, filterHash }
```

- The next page applies `WHERE (ranking tuple) > (cursor tuple)` using the same comparator.
- If `filterHash` does not match the current `leg`+`gender`, return `400 CURSOR_INVALID`.
- Never use offset paging; leads can change between requests.

### 7.3 `GET /leads/dismissed`

- Base set: `Status = Dismissed` for the member.
- Order: `DismissedOnUtc DESC` (most recently dismissed first), then `LeadNumber`.
- Same keyset pattern; no Leg/Gender filters (the dismissed list is a recovery view).
- Returns the same `LeadListResponseDto` shape with `meta` used for paging only.

### 7.4 `GET /leads/{leadId}`

1. Load the lead; if it does not exist **or** `ForMemberId != current member`, return `404 LEAD_NOT_FOUND` and log the denied attempt (BR-4.2). Do not reveal existence.
2. If `Status = WithdrawnBySystem` → `409 LEAD_WITHDRAWN` (the UI removes it).
3. If `Status = Dismissed` → allow read via the dismissed view only; opening from the dismissed list is allowed and does not change status.
4. **Mark viewed:** if `Status = New`, set `Viewed` and `ViewedOnUtc = now`, write a `Viewed` history event. In one transaction and idempotent (conditional update on `Status = New` so two devices do not double-write). Opening a `Requested` lead does **not** reopen it (only M5 can).
5. Build the DTO:
   - `matchedMember` via `IPublicMemberProfileService.GetAsync(matchedMemberId, viewerMemberId)` — this enforces the lead/connection access check and returns only public fields (AC-13.1).
   - `viewerRoute` via `IRouteSnapshotReader` for `ForRouteId`; `matchedRoute` for `MatchedRouteId` (current versions).
   - `whyThisMatch` from `raah_LeadLegResult` + `raah_LeadPointPair` + `raah_LeadCommonDay`, plus the `summaryLine` built server-side.
   - `actions` from status + gateways: `canDismiss` when status ∈ {New, Viewed}; `canUndoDismiss` when Dismissed; `canConnect` from the M5 eligibility; `canReport`/`canBlock` from the M8 gateway.
6. Return `LeadDetailDto`.

**Why GET mutates.** BR-13.4 ties Viewed to opening the detail. The write is a single idempotent status flip. If the team prefers side-effect-free GETs, add `POST /leads/{leadId}/view` and call it on open; the DTO and privacy rules are unchanged.

### 7.5 `POST /leads/{leadId}/dismiss`

- Owner only; else `404`.
- Allowed only when `Status ∈ {New, Viewed}`; otherwise `409 LEAD_DISMISS_NOT_ALLOWED`.
- Set `Status = Dismissed`, `DismissedOnUtc = now`; write `Dismissed` history. No audit event for the other member and **no notification** to them (BR-13.5).
- Idempotent: dismissing an already-dismissed lead returns `200` with the same state.
- A dismissed lead stays dismissed when matching recalculates (A-33) — guaranteed by M3's `UpsertSideAsync` preserving `Dismissed`.
- Return `DismissResultDto`.

### 7.6 `POST /leads/{leadId}/undo-dismiss`

- Owner only; only when `Status = Dismissed`; else `409 LEAD_UNDO_NOT_ALLOWED`.
- Set `Status = Viewed` (spec lifecycle: Dismissed → Viewed) and `DismissedOnUtc = null`; write `Undone` history.
- Return `UndoDismissResultDto`.

### 7.7 `GET /leads/{leadId}/connect-preflight`

Owner only; lead must not be `WithdrawnBySystem`/`Dismissed` (`409`).

Calls `IConnectionRequestGateway.GetEligibilityAsync(viewerMemberId, matchedMemberId, leadId)`, which owns the M5 rules (BR-13.6 / BR-14.3):

| State | `canConnect` | Extra |
|---|---|---|
| None | true | `requestMessage` (CFG-25) |
| RequestSentPending | false | `outgoingConnectionId`, "Request sent · Pending" |
| RequestReceivedPending | false | `incomingConnectionId` + "Rohit has already sent you a request. Accept it?" (BR-14.4) |
| Connected | false | `state = Connected`, "Open chat" (M5/M6) |
| CoolOff | false | `daysLeft` from CFG-17, "You can request again in N days" |
| Pending limit | false | `disabledReasonCode = PENDING_LIMIT` (CFG-6) |
| Restricted | false | `disabledReasonCode = RESTRICTED` |
| Blocked | false | `disabledReasonCode = BLOCKED` |

No-op M5 gateway returns `state = None`, `canConnect = false`, `disabledReasonCode = "NOT_AVAILABLE"` so the button is safely inert before M5 ships.

### 7.8 `POST /leads/{leadId}/connect`

1. Owner only; re-check the lead is connectable (not Dismissed/Withdrawn).
2. Call `IConnectionRequestGateway.SendRequestAsync(viewerMemberId, matchedMemberId, leadId)`; M5 enforces all BR-13.6/BR-14.3 rules and creates the Pending connection with the fixed message (CFG-25) and route snapshot.
3. On success, in the **same transaction** (`IUnitOfWork`) mark the lead `Requested` and write `Requested` history, then commit. On any failure, no lead change.
4. Return `ConnectResultDto` `{ connectionId, connectionNumber, connectionStatus: "Pending", leadStatus: "Requested" }`.

Error mapping from the gateway: `CONNECT_PENDING_LIMIT` (409), `CONNECT_COOL_OFF` (409 + `daysLeft`), `CONNECT_ALREADY_OPEN` (409 + incoming connection to accept), `CONNECT_BLOCKED` (409), `CONNECT_RESTRICTED` (409), `CONNECT_NOT_AVAILABLE` (503 — M5 not deployed), `LEAD_NOT_CONNECTABLE` (409).

M4 does not create the chat; acceptance is M5/M6.

### 7.9 `GET /leads/{leadId}/history`

- Owner only; else `404`.
- Return events from `raah_LeadHistory` by `OccurredOnUtc ASC` (a timeline).
- Map `ActorMemberId == viewer` → "You"; otherwise "System" (never expose the other member's identity beyond what the lead already shows). `Detail` must be UI-safe and contain no contact data.
- Matching events (Created/Updated/Withdrawn/Revived) are written by M3 through `ILeadHistoryWriter`; lifecycle events by M4; request events by M5 (via `ILeadLifecycleGateway`). If M3 shipped before the history table existed, backfill the `Created` event once from `raah_Lead.CreatedDate`.

---

## 8. Authorization and status rules

| Caller / lead | Allowed |
|---|---|
| Anonymous | Nothing |
| Member, own lead | All M4 endpoints |
| Member, another member's lead | Denied; return `404` + log (BR-4.2, BR-13.2) |
| Member, `WithdrawnBySystem` lead | Hidden from list; detail/history return `409`/`404` as above |
| Member, `Dismissed` lead | Visible only in the dismissed list; undo allowed; not connectable |
| Member, `Requested` lead | Visible in list with connection state; not dismissable; not connectable |
| Restricted member | Sees leads and history; Connect disabled with the restriction reason (BR-4.8) |
| Unverified member | Cannot reach leads (BR-10.2/BR-5.1); Foundation gate blocks first |
| Inactive / Blocked | Rejected by session/status validation (BR-5.4/5.9) |
| Admin | No M4 endpoint; monitoring is M11 |

Ownership is always derived from the token — never accept a `memberId` in the body/query.

For a plain role reject use `StatusCode(403)` (a `StatusCodeResult`), never `Forbid()`.

---

## 9. Lead status lifecycle (spec §9.5)

| From | To | Who | M4 mechanism |
|---|---|---|---|
| (none) | New | System (M3) | Match found; M4 reads |
| New | Viewed | System | `GET /leads/{leadId}` conditional flip (BR-13.4) |
| New / Viewed | Dismissed | Member | `POST /dismiss` (BR-13.5) |
| Dismissed | Viewed | Member | `POST /undo-dismiss` (BR-13.5) |
| New / Viewed | Requested | System | `POST /connect` → M5 send, marked in transaction (BR-13.7) |
| Requested | Viewed | System | M5 calls `ILeadLifecycleGateway.ReopenAfterRequestClosedAsync` on Rejected/Withdrawn/Expired (BR-13.7) |
| Any | WithdrawnBySystem | System (M3) | Matching invalidation; M4 hides (BR-13.8) |
| WithdrawnBySystem | New (or Dismissed if it was) | System (M3) | Match valid again; revived (BR-13.9, A-33) |

M4 must **not** move `Requested → Viewed` itself; that transition is owned by M5's request outcome. Likewise it must not revive withdrawn leads — that is M3.

---

## 10. Validation matrix

| ID / rule | Rule | Where | Error |
|---|---|---|---|
| V-30 | Lead only from a match; no manual creation | All M4 endpoints | No create endpoint exists |
| — | Only the owner can read/act on a lead | All | `404 LEAD_NOT_FOUND` + log |
| — | `leg` ∈ {any, going, return} | List | `400 LEAD_FILTER_INVALID` |
| — | `gender` ∈ {any, male, female} | List | `400 LEAD_FILTER_INVALID` |
| — | `limit` ≤ CFG-7 | List | clamp or `400 LEAD_LIMIT_INVALID` |
| — | Cursor matches filters | List | `400 CURSOR_INVALID` |
| — | Dismiss only from New/Viewed | Dismiss | `409 LEAD_DISMISS_NOT_ALLOWED` |
| — | Undo only from Dismissed | Undo | `409 LEAD_UNDO_NOT_ALLOWED` |
| — | Connect only from a valid lead + M5 eligibility | Connect | `409` codes in §7.8 |
| — | Withdrawn lead hidden | List/Detail | hidden / `409 LEAD_WITHDRAWN` |
| — | Public profile never exposes email/mobile | Detail | Privacy test (§14) |

No validation framework exists in the repo; implement in the service. Do not add FluentValidation as a drive-by change.

---

## 11. History vs audit (BR-12.12, BR-8.5)

| Event | Written by | Table | Notes |
|---|---|---|---|
| Created, Updated, WithdrawnBySystem, Revived | M3 | `raah_LeadHistory` | Matching events are **not** platform audit entries (BR-12.12) |
| Viewed, Dismissed, Undone | M4 | `raah_LeadHistory` | Member-visible timeline |
| Requested, RequestClosed | M5 | `raah_LeadHistory` | Via `ILeadLifecycleGateway` |

- History is member-visible only for the member's own leads.
- No contact data, tokens or IP addresses in `Detail`.
- The platform audit trail (Section 8) is not used for per-lead matching volume; setting changes are audited by M13.

---

## 12. Config usage

| CFG | Use in M4 |
|---|---|
| CFG-7 | List page size (default 20) and "Show more" page size |
| CFG-12 | Gender filter enabled (show/hide; force Any when No) |
| CFG-20 | Leg filter enabled (show/hide; force Any when No) |
| CFG-25 | Fixed request message shown in the Connect preflight/sheet (M5 sends it) |
| CFG-6 / CFG-17 | Read **through the M5 gateway** for pending-limit and cool-off; M4 does not re-implement them |
| CFG-28 | Via M1 rating summary (`isNewMember`) |

Read config through the Foundation reader; no hard-coded defaults in M4.

---

## 13. Error responses

Use `BadRequest(new { code, message })` or object-result `StatusCode(409/404, new { code, message })`. Never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `LEAD_NOT_FOUND` | 404 | "This lead is not available." |
| `LEAD_WITHDRAWN` | 409 | "This person is no longer available." |
| `LEAD_FILTER_INVALID` | 400 | "Choose a valid filter." |
| `CURSOR_INVALID` | 400 | "Refresh the list and try again." |
| `LEAD_LIMIT_INVALID` | 400 | "Choose a valid page size." |
| `LEAD_DISMISS_NOT_ALLOWED` | 409 | "This lead cannot be dismissed now." |
| `LEAD_UNDO_NOT_ALLOWED` | 409 | "This lead is not dismissed." |
| `LEAD_NOT_CONNECTABLE` | 409 | "This lead is not available for a request." |
| `CONNECT_PENDING_LIMIT` | 409 | "You have too many pending requests. Wait for answers or withdraw one." |
| `CONNECT_COOL_OFF` | 409 | "You can request again in {days} days." |
| `CONNECT_ALREADY_OPEN` | 409 | "This person already sent you a request. Accept it instead." |
| `CONNECT_BLOCKED` | 409 | "You cannot send a request to this person." |
| `CONNECT_RESTRICTED` | 409 | "You cannot send new requests until {date}." |
| `CONNECT_NOT_AVAILABLE` | 503 | "Connecting is not available yet." (M5 not deployed) |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

Keep messages aligned with the M4 UI plan's microcopy.

---

## 14. Privacy and security

- The matched member is always projected through `IPublicMemberProfileService`; M4 DTOs contain **no** email, mobile, IP, device, login history, reports, admin notes or blocks. Add a serialization test that fails if those keys appear (AC-13.1).
- Ownership is derived from the token; a mismatched `leadId` returns `404` and logs the attempt (BR-4.2).
- Dismiss/undo/connect are state-checked and transaction-scoped; concurrent double-submits are idempotent or rejected, never duplicated.
- Connect is atomic with the lead status change (`IUnitOfWork`); a failed send leaves the lead touched only by its prior state.
- Cursors reveal no personal data; sign or HMAC the cursor so clients cannot forge ranking tuples.
- History `Detail` is UI-safe and free of contact data, tokens and IPs.
- Rate-limit list/detail reads per member at the gateway if abuse is observed (no spec requirement in Phase 1).

---

## 15. Build sequence (M4)

| Step | Work | Output |
|---|---|---|
| 1 | DTOs, enums, `raah_LeadHistory` + led timestamps migration | Compiles, migration applies |
| 2 | `ILeadQueryService` list/detail/dismissed over M3 tables, ranking + keyset | Unit-testable |
| 3 | `ILeadHistoryWriter` + history read; M3 integration to write created/updated/withdrawn | History rows verified |
| 4 | Dismiss / Undo lifecycle + guards | AC-13.4 |
| 5 | No-op `IConnectionRequestGateway` / `IInteractionPolicyGateway` + connect preflight | Safe before M5/M8 |
| 6 | `POST /connect` orchestration with transaction + `ILeadLifecycleGateway` | Works with fake gateway |
| 7 | `LeadController` endpoints + error codes + privacy tests | Postman-ready |
| 8 | Wire M5 gateway when it lands; wire M8 block/report availability | Connect/block live |
| 9 | Backfill Created history if M3 predates the table | Complete timelines |
| 10 | Performance: composite index on ranking + status | Meets paging latency |

Suggested folders:

```
Binsera.Core/Leads/
  Domain/Raah/LeadHistory.cs
  Enums/LeadHistoryEventType.cs, LeadConnectionState.cs, LegFilter.cs, GenderFilter.cs
  Models/Raah/LeadListItemDto.cs, LeadDetailDto.cs, LeadListResponseDto.cs, *Request/Result.cs
  Contracts/ILeadQueryService.cs, ILeadHistoryWriter.cs, ILeadSnapshotReader.cs,
            ILeadLifecycleGateway.cs
  Contracts/Gateways/IConnectionRequestGateway.cs, IInteractionPolicyGateway.cs
Binsera.Application/Leads/Services/
  LeadQueryService.cs, LeadLifecycleService.cs, LeadHistoryWriter.cs, LeadSnapshotReader.cs
  Gateways/NoOpConnectionRequestGateway.cs, NoOpInteractionPolicyGateway.cs
Binsera.API/Controllers/Leads/LeadController.cs
Binsera.Persistence/Configurations/Raah/LeadHistoryConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahLeadHistoryAndLeadTimestamps.cs
```

---

## 16. Test plan

### 16.1 Unit

- Ranking: both-legs before one-leg; Full Route before Useful Segment; more shared points; smaller gap; more common days; earlier created; stable tie-break.
- Leg filter: only leads with a matched leg for that type; order unchanged; `Any` restores all.
- Gender filter: male/female only; `Other` only under `Any`; enabled/disabled by CFG-12.
- Paging: first CFG-7 then keyset `Show more`; no overlap, no gap; cursor/filter mismatch rejected.
- Viewed: New→Viewed once; second open no change; Requested not reopened; history event written once.
- Dismiss: New/Viewed allowed; Requested/Withdrawn rejected; idempotent; stays dismissed after recalculation (A-33).
- Undo: only Dismissed→Viewed; clears dismissed timestamp.
- Connect preflight mapping for all seven states; fixed message = CFG-25.
- Connect: success marks Requested atomically; gateway failure leaves status unchanged; each error code maps correctly.
- Privacy: `LeadDetailDto` never contains email/mobile keys.
- Withdrawn hidden from both lists; detail returns `LEAD_WITHDRAWN`.

### 16.2 Integration / API

- Every endpoint requires auth; foreign `leadId` → 404 + logged; missing token → 401.
- Envelope present; `data` matches DTO; `meta` counts correct ("5 found", "2 of 5 shown").
- Migration applies cleanly and does not touch `member_Master`.
- M5 fake gateway: send creates a fake connection, marks Requested; rejection reopens Viewed.
- M3 recalculation preserves Dismissed and revives Withdrawn appropriately.
- History timeline order and actor mapping.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-13.1 Mobile/email hidden | §7.4 public projection, §14 privacy test |
| AC-13.2 Leg filter | §7.1 step 3, §16.1 |
| AC-13.3 Gender filter | §7.1 step 4, §16.1 |
| AC-13.4 Dismissed stays dismissed | §7.5, M3 preserve-status, §16.1 |
| AC-13.5 Why this match | §7.4 `whyThisMatch` from stored results |
| AC-13.6 Page size | §7.1/7.2 CFG-7 + Show more |

Scenarios SC-13.1…SC-13.10 map to the endpoints above; SC-13.9 pending limit and SC-13.10 cool-off are surfaced through `connection`/preflight (rules enforced by M5).

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| — | Does GET detail mark Viewed, or a separate `POST /view`? | GET marks New→Viewed idempotently (spec ties Viewed to opening) |
| — | Route comparison shows current or snapshot route? | Current routes (BR-13.11); explanation from stored results |
| — | Add `ViewedOnUtc`/`DismissedOnUtc` to `raah_Lead` or derive from history? | Add nullable columns (simpler dismissed ordering); coordinate with M3 |
| — | Who owns the lead tables overall? | M3 writes rows; M4 owns lifecycle + read; M5 owns requests |
| — | Can a Requested lead be dismissed? | No (status must stay Requested until M5 outcome) |
| — | Undo target: Viewed or the pre-dismiss status? | Viewed (spec §9.5 lifecycle) |
| — | Global API prefix? | Keep existing no-prefix convention (`/leads`) |
| — | Dismissed list filters? | None; it is a recovery view only |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Lead row contents | §5.1, §5.5 |
| Lead detail blocks | §5.3, §7.4 |
| Filters (D-10, D-16) | §7.1 |
| BR-13.1 no manual leads | §1, §2.2 |
| BR-13.2 own leads + limited profile | §7.4, §8, §14 |
| BR-13.3 ranking + CFG-7 + Show more | §7.1, §7.2 |
| BR-13.4 Viewed on open | §7.4, §9 |
| BR-13.5 Dismiss/Undo | §7.5, §7.6 |
| BR-13.6 connect rules | §7.7, §7.8 (M5 gateway) |
| BR-13.7 Requested and reopen | §7.8, §9 |
| BR-13.8 withdrawn hidden | §7.1, §7.4, §9 |
| BR-13.9 revive same lead | §9 (M3) |
| BR-13.10 History tab | §7.9, §11 |
| BR-13.11 live view | §2.2, §7.4 |
| BR-13.12 empty state | UI plan §4 |
| Status lifecycle | §9 |
| Data Impact | §7.5, §7.8, §11 |
| RPT-8, RPT-9 | M15 reads M3/M4 data; no M4 report endpoint |
| CFG-7/12/20/25 (+6/17/28) | §12 |
| V-30 | §10 |
| AC-13.1…13.6 | §16.3 |
