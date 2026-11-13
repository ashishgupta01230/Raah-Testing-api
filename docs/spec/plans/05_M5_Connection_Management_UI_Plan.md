# M5 — Connection Management — UI Plan

| | |
|---|---|
| **Module** | M5 — Connection Management (spec `docs/spec/05_M5_Connection_Management.md`) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5, module design **§6.5 (M5)** + §6.6 (chat entry), patterns §7, delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20) |
| **Device/network** | Mid-range Android + Chrome, Safari on iPhone; slow-network tolerant (skeletons, small images) |
| **Companion** | API plan: `05_M5_Connection_Management_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M5 lets one member ask another to connect, lets the other answer, and opens a chat only when both agree:

1. **Send** a request from a lead through the fixed-message confirmation (SCR-14.1 — the sheet is opened by M4; M5 owns its meaning and states).
2. **Requests received** with Accept / Reject (SCR-14.3).
3. **Requests sent** with Withdraw (SCR-14.2).
4. **My Connections** (SCR-14.4) — accepted connections, surfaced through the Chats list (M6) and the connection detail.
5. **Connection detail** (SCR-14.5): the other member's limited profile, both routes at request time vs now, history, Disconnect, and links to Block / Report / Rate.
6. **Expiry, cool-off and limit** shown honestly, with the exact neutral wording.

UI jobs for this module:

- **Nothing happens by surprise** (UP-5): the fixed request message is shown read-only before sending; Reject and Disconnect confirm what will happen.
- **Neutral, never blaming** (UP-6): a rejected sender sees "Your request was not accepted" with no reason; a block is never announced.
- **Show the minimum about people** (UP-4): only the public profile view — never email or mobile.
- **Explain the match** (UP-2): each request shows the stored shared stretch, days, times and gap in plain words.
- **Every state is designed** (UP-10): pending, expiring, expired, rejected, withdrawn, accepted, disconnected, blocked, restricted, limited, offline, error.
- **A connection is not a booking** (Principle 7): copy never implies a scheduled journey (BR-14.15).

Out of scope for M5 screens (owned elsewhere, but cross-linked): the **Leads** and **Dismissed** segments (M4), chat messages/composer and the chat list contents (M6), rating creation (M7), Block/Report flows (M8), profile (M1), route (M2). The **Leads · Requests · Dismissed** segment bar is a shared shell; M5 owns the **Requests** content and the **Connection detail**.

---

## 2. Navigation and routes

M5 lives inside the **Leads** tab (Requests segment) and the **Chats** tab (accepted connections), on both the phone bottom nav and the desktop sidebar (UI Plan §4.1). The top bar keeps the RAAH mark, role chip (Saathi/Raahi) and notification bell. The Leads tab badge (shell-owned) counts new leads plus pending received requests.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Requests — Received / Sent | `/connections` (segments) | SCR-14.2, SCR-14.3 | Leads tab → **Requests** segment; notification |
| Requests — History | `/connections?view=history` | SCR-14.2/14.3 | Received/Sent → History filter |
| Connection detail | `/connections/[id]` | SCR-14.5 | Request card after accept; chat header menu; My Connections |
| My Connections | `/chats` (conversation list) | SCR-14.4 | Chats tab |
| Connect confirmation | bottom sheet on `/leads/[id]` and lead card | SCR-14.1 | M4 Connect |
| Chat | `/chats/[id]` | M6 | Connection detail "Open chat" |

Gates that affect M5 (owned by Foundation / M2):

- **Verify-email gate:** Unverified members cannot reach Requests or connection detail (BR-5.1).
- **No Active route:** no new requests can be sent or received; accepted connections and their chats still work. Send is disabled with a note linking to Route (M2).
- **Legal re-acceptance gate:** shown before any M5 screen when a required version is pending (BR-22.16).
- **Restricted banner:** amber banner on Requests: "You cannot send new requests until *date*." Accepting/rejecting received requests still works (BR-21.6).
- **Inactive/Blocked:** cannot log in (BR-5.4).

---

## 3. Screen inventory and components

| Screen / part | Components used (UI Plan §3.6) |
|---|---|
| Requests | C-11 Segmented control (Received/Sent + History), C-17 Request card, C-12 Badges/chips, C-13 Avatar, C-14 Rating stars, C-15 Route line, C-21 Banner, C-25 Empty state, C-26 Skeleton, C-35 Show more, C-1 Button |
| Connection detail | C-13, C-12, C-14, C-15 Route line, C-33 Key-value list, C-34 History timeline, C-23 Confirm dialog, C-24 Bottom sheet, C-1 Button, C-21 Banner, C-25, C-26 |
| Reject confirm | C-23 Dialog / C-24 Bottom sheet, C-1 Button |
| Disconnect confirm | C-23 Dialog, C-1 Button |
| Connect sheet (SCR-14.1) | C-24 Bottom sheet, C-1 Button, C-21 Banner, C-22 Toast |
| My Connections | C-18 Conversation row (M6 surface), C-25, C-26 |

M5 reuses `C-17 Request card`, `C-15 Route line`, `C-33 Key-value list`, `C-34 History timeline`, `C-23/C-24` confirm patterns. No new reusable component is required; if any is built in M4/M6, reuse it.

---

## 4. Screen designs

### 4.1 Requests — Received (W-5.1)

```text
┌────────────────────────────────────────┐
│  Leads    Requests (2)    Dismissed    │
│  [ Received (2) ]  [ Sent (1) ]        │
├────────────────────────────────────────┤
│  (Av) Amit R.            ★ New member  │
│  Raahi · Bike · Email verified ✓       │
│  “Hello, I want to connect with you”   │
│  Shares: Vinay Nagar → Badarpur Metro  │
│  Mon–Fri · 8:05 AM                     │
│  Expires in 4 days                     │
│  (i) Route changed since this request  │
│  was sent                              │
│  [   Accept   ]   [ Reject ]           │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Segment bar | **Received** / **Sent** (C-11); a count on each tab when > 0. A **History** link/filter shows answered requests (Rejected/Withdrawn/Expired/Blocked) |
| Card content | Other member's public card (avatar, name, role, vehicle, verified, rating or "New member"); the stored request message in a quoted, read-only block; the match summary for each leg (shared points, days, times, gap); expiry countdown |
| Wording | Use the API's `matchSummary`; never invent order or match text |
| Expiry | "Expires in *n* days" in amber when ≤ CFG-29 days; "Expired" once past. Expired cards move to History (BR-14.8) |
| Route-changed note | A `(i)` note when the sender changed their route after sending (BR-14.10); tapping shows both routes (as sent and now) |
| Accept | One tap (no confirmation) → toast "Connected. You can now chat." and a link to the chat; the screen adds "Decide time, meeting point and shared portion together." (BR-14.15) |
| Reject | Confirmation sheet: "Reject this request? They will only see *Your request was not accepted*." (UP-6, BR-14.6) |
| Privacy | Only the public card; no email/mobile (BR-4.9) |

**Request card states (C-17)**

| State | Card shows |
|---|---|
| Pending | Countdown; **Accept** + **Reject** (received) or **Withdraw** (sent) |
| Expiring soon | Amber "Expires in *n* days" |
| Expired | Moved to History; muted "Expired" |
| Accepted | Card is removed from Requests; it appears under Connections/Chats |
| Rejected (received) | Muted "Rejected" in History |
| Rejected (sent) | Muted **"Your request was not accepted"** in History (no reason) |
| Withdrawn | Muted "Withdrawn" |
| Blocked | Muted "Ended" (never says who blocked — BR-17.2) |
| Route changed | `(i)` note + compare link |
| Restricted member | Amber banner at top of the list; receiving/answering still works |

### 4.2 Requests — Sent

Same card, direction "Sent":

- Pending shows **"Pending · waiting *n* days"** and a **Withdraw** button (BR-14.7).
- Answered requests move to the **History** filter: Accepted, Rejected (neutral wording), Withdrawn, Expired, Disconnected, Blocked.
- A Withdraw is immediate on tap (low risk, reversible by asking again; no confirmation per UI Plan §7.3), with toast "Request withdrawn."

### 4.3 Connection detail (W-5.2)

```text
┌────────────────────────────────────────┐
│  ← Neha S.                         ⋯   │
│  [Accepted]  since 09-Oct-2026         │
│  Raahi · Car · ★ 4.6 (12)              │
│  [  Open chat  ]  [ Rate ]             │
│  Route when the request was sent       │
│   ...                                  │
│  Neha's current route                  │
│   ...                                  │
│  History                               │
│   09-Oct 08:32  Request sent           │
│   09-Oct 09:10  Accepted               │
│  Disconnect                            │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | Back arrow, other member's name, status badge (Accepted / Disconnected / Ended). `⋯` menu holds **Block** and **Report** (M8) |
| Profile | Public view only: avatar, name, role, vehicle (Raahi), verified badge, rating or "New member"; no email/mobile (BR-4.9) |
| Actions | **Open chat** (M6) and **Rate** (M7) for Accepted; disabled with a note when the connection is closed |
| Route at request | Both routes exactly as they were when the request was sent (snapshot); highlights the shared stretch (C-15) with a text list, never colour alone |
| Current route | The other member's **current** route, with a note "Route changed since this request was sent" when it differs (BR-14.10) |
| History | C-34 timeline: Request sent, Accepted, Rejected/Withdrawn/Expired, Disconnected, Blocked, Route changed — date, time, actor ("You"/"Other"/"System"), read-only |
| Disconnect | Dialog: "End this connection? The chat will close. You can both still read it. This cannot be undone; you would need a new request." (BR-14.11) |
| Post-disconnect | Status "Ended"; chat read-only; Block/Report still available for safety |
| States | Skeleton; not-found → "This request is not available." with Back; error banner + Retry |

### 4.4 Connect confirmation — bottom sheet (SCR-14.1, opened by M4)

```text
┌────────────────────────────────────────┐
│  Send request to Neha?                 │
│  This message will be sent with it:    │
│  ┌──────────────────────────────────┐  │
│  │ [lock] Hello, I want to connect with  │  │
│  │    you                            │  │
│  └──────────────────────────────────┘  │
│  She decides whether to accept. You can│
│  chat once she does.                   │
│  [      Send request      ]  [ Cancel ]│
└────────────────────────────────────────┘
```

| State | UI |
|---|---|
| Default | Read-only message box from CFG-25 (via preflight); no editable field (D-23, V-37) |
| Sending | Primary spinner; both buttons disabled |
| Sent | Sheet closes; toast "Request sent."; the lead card becomes "Request sent · Pending" |
| Pending limit | Inline warning "You have too many pending requests. Wait for answers or withdraw one."; primary disabled |
| Cool-off | Inline warning with days left; primary disabled |
| Already received a request | "Neha has already sent you a request. Accept it?" with an **Accept** button (BR-14.4) → `POST /connections/{id}/accept` |
| Not available any more | "This person is no longer available."; close + refresh |
| Restricted | Inline "You cannot send new requests until *date*."; primary disabled |
| Offline | Primary disabled with the offline notice |

The sheet is M5's SCR-14.1 but is triggered from M4; M5 owns the request record and accept/reject.

### 4.5 My Connections (SCR-14.4)

- Accepted connections appear as the **Chats** list (M6 conversation rows, C-18) — avatar, name, last message, time, unread, closed marker.
- Closed conversations (Disconnected / Ended) appear under the Chats **Closed** filter (M6) and stay read-only.
- Tapping the chat header opens the **Connection detail** (4.3) for route history, Rate, Disconnect, Block, Report.
- If M6 is not yet available, a "My Connections" list can render the API's connection list (C-18 rows, no message preview) until chat ships.

### 4.6 Confirmations

| Action | Level (UI Plan §7.3) | Copy |
|---|---|---|
| Accept | None (one tap + toast) | "Connected. You can now chat." |
| Withdraw | None (toast) | "Request withdrawn." |
| Reject | Sheet with consequences | "Reject this request? They will only see *Your request was not accepted*." |
| Disconnect | Sheet with consequences | "End this connection? The chat will close. You can both still read it. This cannot be undone; you would need a new request." |
| Block | Sheet with consequences (M8) | Owned by M8; states "They are not told." |
| Report | Report form (M8) | Owned by M8 |

### 4.7 Empty and special states

| Situation | Message and action |
|---|---|
| No received requests | "No requests waiting." (calm, no action needed) |
| No sent requests | "You have no pending requests. Find people in Leads." with a link to Leads |
| No accepted connections | "Your connections will appear here once someone accepts a request." with a link to Leads |
| History empty | "No past requests yet." |
| Expired | Moved to History; muted "Expired" (sender told; no cool-off) |
| Restricted | Amber banner: "You cannot send new requests until *date*." |
| Nothing available | "This person is no longer available." (SC-14.11) |
| Offline | Slim banner "No connection. Changes will not be saved."; Accept/Reject/Withdraw/Disconnect disabled |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton request cards / detail rows (C-26); never a bare spinner |
| Empty | Distinct copy for received-empty, sent-empty, history-empty, connections-empty (4.7) |
| Error | Banner with a plain message and one **Retry** |
| Success | Toast (C-22, 4 s, polite live region) for accept, withdraw, disconnect, request sent |
| Saving | Disable only the submitting control; prevent double submit; in-flight confirm sheet stays open |
| Disabled | Explain inline: pending limit, cool-off days, restricted date, closed connection, chat unavailable until M6 |
| No permission | Generic 403; a foreign connection shows the not-available state, never another member's data |
| Offline | Top banner; disable writes (Accept, Reject, Withdraw, Disconnect) |
| Session expired | Foundation dialog; the Requests view state is restored (BR-5.6) |
| Verify / legal gate | Foundation; M5 not reachable until cleared |
| Real-time | Refresh on focus/visibility and after returning from chat; a new request updates via the notification badge (M9). No polling storm |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Single column; Requests as segments; connection detail full screen with back arrow; confirmations as bottom sheets (C-24) |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | Requests max ~720 px centred; consider master-detail preview; detail full screen |
| Desktop 1024+ | **Received and Sent as two columns** on wide screens (UI Plan §6.5); connection detail with routes side by side; sheets become centred dialogs |
| Wide 1280+ | Member content max 1120 px; do not stretch route columns |

- The shell switches bottom tabs to a left sidebar at tablet/desktop (UI Plan §4.1); M5 does not build the shell.
- Keep safe-area padding for sticky actions and the bottom nav.
- At 200% text zoom the layout must not scroll horizontally; wide route comparisons scroll inside their container.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring; return focus after sheets/dialogs close |
| Labels | Visible labels on segments and filters; the read-only request message has a label ("Request message") |
| Targets | ≥ 44 × 44 px, 8 px apart (Accept, Reject, Withdraw, Disconnect, ⋯) |
| Colour | Status badges and shared points always have words/text; counts are not colour-only |
| Screen readers | Announce "Request accepted", "Request withdrawn", "Connection ended"; the card announces status and days left; shared points are a text list |
| Keyboard | Segment switching, card actions, ⋯ menu, confirm dialogs and history all keyboard-operable; dialogs trap focus |
| Motion | Honour "reduce motion"; no decorative animation |
| Language | `lang="en"`; dates `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M5)

| Situation | Copy |
|---|---|
| Received heading | "Requests" · segment "Received (2)" · "Sent (1)" |
| Request message label | "Request message" (read-only) |
| Match line | "Shares: Vinay Nagar → Badarpur Metro" · "Mon–Fri · 8:05 AM" |
| Pending (received) | "Expires in 4 days" |
| Pending (sent) | "Pending · waiting 6 days" |
| Accept | "Accept" |
| Accepted toast | "Connected. You can now chat." |
| Accepted body | "Decide time, meeting point and shared portion together." |
| Reject action | "Reject" |
| Reject confirm | "Reject this request? They will only see *Your request was not accepted*." |
| Rejected (sender) | "Your request was not accepted." |
| Withdraw | "Withdraw" |
| Withdrawn toast | "Request withdrawn." |
| Expired | "Expired" · "No reply in 30 days. You can send a new request." |
| Cool-off | "You can request again in 5 days." |
| Pending limit | "You have too many pending requests. Wait for answers or withdraw one." |
| Route changed | "Route changed since this request was sent." |
| Disconnect | "Disconnect" |
| Disconnect confirm | "End this connection? The chat will close. You can both still read it. This cannot be undone; you would need a new request." |
| Ended (blocked) | "Ended" (never says who blocked) |
| Connect sheet | "Send request to Neha? This message will be sent with it:" · "She decides whether to accept. You can chat once she does." |
| Already requested by them | "Neha has already sent you a request. Accept it?" |
| No received | "No requests waiting." |
| No sent | "You have no pending requests. Find people in Leads." |
| No connections | "Your connections will appear here once someone accepts a request." |
| Not available | "This person is no longer available." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8, UP-6).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load Requests — Received | `GET /connections/requests/received?status=&limit=&cursor=` |
| Load Requests — Sent | `GET /connections/requests/sent?status=&limit=&cursor=` |
| History filter | `GET /connections/requests/{received\|sent}?status=history&cursor=` |
| Show more | same endpoint with `cursor=<nextCursor>` |
| Load My Connections | `GET /connections?status=open&limit=&cursor=` (or M6 chat list) |
| Open connection detail | `GET /connections/{connectionId}` |
| Load connection history | `GET /connections/{connectionId}/history` |
| Accept | `POST /connections/{connectionId}/accept` |
| Reject | `POST /connections/{connectionId}/reject` |
| Withdraw | `POST /connections/{connectionId}/withdraw` |
| Disconnect | `POST /connections/{connectionId}/disconnect` |
| Connect preflight (sheet) | `GET /leads/{leadId}/connect-preflight` (M4, backed by M5) |
| Send request | `POST /leads/{leadId}/connect` (M4, M5 rules) |
| Accept an incoming request from a lead | `POST /connections/{connectionId}/accept` |
| Open chat | M6 (`/chats/[id]`) |
| Rate | M7 (`/ratings/[connectionId]`) |
| Block / Report | M8 |

Rules for the client:

- Always send the auth cookie/token; derive the member on the server; never send a member id.
- Never re-order the list; trust the API order.
- On `404 CONNECTION_NOT_FOUND` show "This request is not available." and refresh.
- On `409 CONNECTION_NOT_PENDING` / `CONNECTION_ALREADY_CLOSED` refresh the card to its new state.
- On `403 CONNECTION_NOT_RECEIVER` / `CONNECTION_NOT_SENDER` hide the action and refresh.
- On `409 REQUEST_LIMIT_REACHED`, `COOLOFF_ACTIVE`, `MEMBER_RESTRICTED`, `OPEN_CONNECTION_EXISTS` map to the exact copy in §8; do not silently retry.
- After a successful mutation, update the card in place; on accept, surface the chat link.
- Show the neutral copy for a rejected sender regardless of any internal detail returned.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M4 leads + connect sheet | Requests cannot originate; show the empty sent state |
| M6 chat | "Open chat" is disabled with "Chat is not available yet."; My Connections can use `GET /connections` without previews |
| M7 rating | "Rate" is hidden; profile shows "New member" |
| M8 Block/Report | The ⋯ menu hides Block/Report until M8 |
| M9 notifications | The Leads badge counts only what the shell can fetch; no push required |
| M2 route | Route at request falls back to the stored snapshot; current route hidden if unreadable with a note |
| Design tokens | Use UI Plan §3 values; no hard-coded colours |

---

## 11. Delivery plan (M5 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Reuse/build C-17 Request card, C-15 Route line, C-33 Key-value list, C-34 History timeline | UI Plan §3 |
| 2 | **Requests** segment shell under Leads (`/connections`): Received / Sent / History | Shell, M4 |
| 3 | Received list against `GET /connections/requests/received` with skeleton/empty/error | API 1 |
| 4 | Sent list + Withdraw action + toast | API 2, 7 |
| 5 | Accept (one tap + toast + chat link) and Reject (confirm sheet) | API 5, 6 |
| 6 | Connection detail: public card, route at request vs current, route-changed note, history | API 4, 9 |
| 7 | Disconnect confirm and post-disconnect read-only state | API 8 |
| 8 | Connect sheet states (limit, cool-off, restricted, already-received) — from M4 | API send/preflight |
| 9 | My Connections via Chats list / connection list; link to detail from chat header | M6 |
| 10 | Desktop two-column Received/Sent; dialogs instead of sheets | — |
| 11 | Accessibility pass (labels, focus traps, SR announcements, text status) | — |
| 12 | QA: expiry/cool-off/limit, neutral rejection, privacy, slow network, 200% zoom | API running |
| 13 | Enable chat link, Rate, Block/Report as M6/M7/M8 land | M6/M7/M8 |

Suggested route files: `connections/index`, `connections/[id]`, with the Requests segment shared inside the `leads` tab shell.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-14.1 | Connecting from either a Saathi or Raahi lead shows the same confirmation and fixed message |
| AC-14.2 | Only the receiver sees Accept/Reject; a foreign connection id shows the not-available state |
| AC-14.3 | No chat link appears until a request is accepted |
| AC-14.4 | A pending request shows the correct days left and becomes "Expired" in History at CFG-5 |
| AC-14.5 | At the pending limit, Connect is disabled with the exact limit message |
| AC-14.6 | When the other side already sent a request, the sheet offers **Accept it?** instead of a second send |
| AC-14.7 | After a route deactivation, pending requests disappear from Requests; accepted connections stay |
| AC-14.8 | After a rejection, the sender sees the cool-off with days left |
| AC-14.9 | The fixed message is shown read-only; there is no edit field |
| AC-14.10 | On accept, the fixed message is visible as the first chat message; on reject, no chat exists |
| BR-14.15 | Accept copy never implies a booked/scheduled journey |
| BR-14.16 | No screen shows another pair's connection |
| UP-4 | No email or mobile is ever rendered |
| UP-5 | Reject and Disconnect state their consequences before confirming |
| UP-6 | A rejected sender sees only "Your request was not accepted" |
| UP-10 | Loading, empty (received/sent/history/connections), expired, restricted, offline and error states are implemented |
| UP-11 | Contrast, focus, labels, targets, keyboard and SR announcements pass |
| — | Status badges and shared points never rely on colour alone |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| — | Are Received and Sent segments, or two separate lists? | Segments under Requests; two columns on desktop |
| — | Does Withdraw need a confirmation? | No (toast only); it is low risk and reversible by a new request |
| — | Where does the History of answered requests live? | A History filter within Received/Sent |
| — | How does "Route changed since this request was sent" work? | An `(i)` note opening a comparison of snapshot vs current |
| — | Is My Connections a separate list or the Chats tab? | The Chats tab (M6); connection detail from the chat header |
| — | Show the request message on the card always? | Yes, quoted and read-only — it is the sender's only words |
| — | Should expired requests show a cool-off? | No (BR-14.8) |
| — | Disconnect vs Delete wording? | "Disconnect"; history remains and never says "delete" |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-14.1 Confirm Request | §4.4 |
| SCR-14.2 Requests Sent | §4.2 |
| SCR-14.3 Requests Received | §4.1 |
| SCR-14.4 My Connections | §4.5 |
| SCR-14.5 Connection Detail | §4.3 |
| Field spec (Connection) | §4.1, §4.3 |
| BR-14.1 request from a lead | §4.4 |
| BR-14.2 either side can send | §4.1, §4.2 |
| BR-14.3 eligibility | §4.4 states |
| BR-14.4 one open / accept incoming | §4.4 "already sent you a request" |
| BR-14.5 Accept | §4.1, §4.6 |
| BR-14.6 Reject + neutral + cool-off | §4.1, §4.6, §8 |
| BR-14.7 Withdraw | §4.2 |
| BR-14.8 Expiry | §4.1, §4.7 |
| BR-14.9 auto-withdraw | §4.7, §12 AC-14.7 |
| BR-14.10 route snapshot + changed note | §4.3 |
| BR-14.11 Disconnect | §4.3, §4.6 |
| BR-14.12 Block ends connection | §4.3, §4.6 (M8) |
| BR-14.13 many Accepted | §4.5 |
| BR-14.14 never deleted / history | §4.3 |
| BR-14.15 accept is chat only | §4.1, §8 |
| BR-14.16 own connections only | §5 no-permission row, §12 |
| BR-14.17 fixed message | §4.4, §8 |
| Status lifecycle §9.6 | §4.1 card states, §4.3 |
| CFG-5/6/17/25 (+29) | §4.1, §4.4, §4.7, §8 |
| V-31…V-37 | §4.4, §8 |
| SC-14.1…SC-14.13 | §4 flows, §12 |
| AC-14.1…AC-14.10 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §6.5 (W-5.1, W-5.2) + §6.6 chat entry | §4 |
| UI Plan §7 state/confirmation patterns | §4.6, §5, §6 |
| UI Plan §8 delivery | §11 |
