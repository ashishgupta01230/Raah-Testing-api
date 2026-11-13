# M6 — In-App Chat — UI Plan

| | |
|---|---|
| **Module** | M6 — In-App Chat (spec `docs/spec/06_M6_InApp_Chat.md`) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5, module design **§6.6 (M6)** + §6.5 (connection entry), patterns §7, delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20) |
| **Device/network** | Mid-range Android + Chrome, Safari on iPhone; slow-network tolerant (skeletons, small avatars, no heavy media) |
| **Companion** | API plan: `06_M6_InApp_Chat_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M6 gives the two members of an accepted connection one safe place to talk and to decide the journey (D-5). Chat is the **only** communication channel in RAAH (D-1, Principle 9).

Screens and jobs:

1. **Chat list** (SCR-15.1) — open and closed conversations, newest first, with unread counts.
2. **Conversation** (SCR-15.2) — messages, composer, header with the other member's limited profile and actions (View connection · Block · Report · Disconnect · Rate), a standing safety tip, and the request message as the first line.
3. **Send and read** text-only messages within CFG-26, with a clear "New messages" divider and no editing or deleting.
4. **Closed chat** read-only state and neutral wording.
5. **Honest states** for sending, failed, reconnecting, throttled, offline and access-denied.

UI jobs for this module:

- **Trust and safety are always one tap away** (UP-3): the safety tip is always visible; Block and Report live in the header menu.
- **Nothing happens by surprise** (UP-5): the composer shows the length limit near the cap; a closed chat explains that sending is off and history stays.
- **Neutral, never blaming** (UP-6): a closed chat says "Connection ended", never who blocked whom.
- **Show the minimum about people** (UP-4): header shows the public view only — never email or mobile.
- **Every state is designed** (UP-10): loading, empty, closed, sending, failed, reconnecting, throttled, offline, no-access.
- **The route is the product, chat is the room** (Principle 8): copy never implies a booking or an enforced agreement.

Out of scope for M6 screens (owned elsewhere, but cross-linked): the **Requests** content and **Connection detail** (M5), Block and Report forms (M8), rating form (M7), profile and role (M1), leads (M4). The **Chats** tab shell and the conversation-row badge are M6's; the connection detail opens from the chat header (M5).

---

## 2. Navigation and routes

M6 is the **Chats** tab, on both the phone bottom nav and the desktop sidebar (UI Plan §4.1). The top bar keeps the RAAH mark, role chip (Saathi/Raahi) and notification bell.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Chat list | `/chats` | SCR-15.1 | Chats tab; notification "new message" (M9) |
| Chat list — Closed filter | `/chats?view=closed` | SCR-15.1 | Open/Closed segment |
| Conversation | `/chats/[id]` | SCR-15.2 | Chat list row; "Open chat" on connection detail (M5); lead "Connected · Open chat" (M4/M5) |

Gates that affect M6 (owned by Foundation / M2):

- **Verify-email gate:** Unverified members cannot reach Chats (BR-5.1).
- **No Active route:** existing connections and chats still work; no new requests, but chat is unaffected.
- **Legal re-acceptance gate:** shown before any M6 screen when a required version is pending (BR-22.16).
- **Restricted member:** can still chat with accepted connections — no restriction on messaging (BR-21.6 only limits new requests).
- **Inactive/Blocked:** cannot log in (BR-5.4).

The Chats tab badge counts conversations with unread messages (API `GET /chats/unread-count`), not total messages, to stay calm.

---

## 3. Screen inventory and components

| Screen / part | Components used (UI Plan §3.6) |
|---|---|
| Chat list | C-18 Conversation row, C-11 Segmented control (Open/Closed), C-13 Avatar, C-12 Badge/chip (Closed marker), C-25 Empty state, C-26 Skeleton, C-35 Show more |
| Conversation header | C-13 Avatar, C-12 Status badge, C-1 Button (back), overflow menu (C-24), C-21 Banner (safety tip) |
| Message list | C-19 Message bubble, C-15 (not used), C-26 Skeleton, C-25 Empty state, C-22 Toast |
| Composer | C-19 Composer, C-2/C-2 counter, C-1 Button (Send) |
| Header menu actions | C-24 Bottom sheet (Phone) / C-23 Dialog (Desktop), C-1 |
| Closed state | C-21 Banner, C-19 read-only bubbles |
| Request message | C-19 system-style bubble with a "Request message" label |

M6 reuses **C-18 Conversation row**, **C-19 Message bubble and composer**, **C-13 Avatar**, **C-11 Segmented control**, **C-21 Banner**, **C-24 Bottom sheet** and **C-22 Toast**. No new reusable component is required; if the request-message styling is built, keep it a variant of C-19, not a new component.

---

## 4. Screen designs

### 4.1 Chat list (W-6.1)

```text
┌────────────────────────────────────────┐
│  Chats                [Open] [Closed]  │
├────────────────────────────────────────┤
│  (Av) Neha S.              8:32 AM     │
│  Meet at Badarpur Metro?          (2)  │
├────────────────────────────────────────┤
│  (Av) Amit R.              Yesterday   │
│  Hello, I want to connect with you     │
│  [Closed]                              │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Segments | **Open** (default) / **Closed** (C-11); a count on each when > 0. Closed chats are read-only history (BR-15.7) |
| Row (C-18) | Avatar, other member's name, one-line last-message preview, relative time, unread count badge, closed marker |
| Preview | Last message text only; the request message preview reads as the connection opener; never contact data |
| Unread | Count bubble when > 0; cleared when the conversation is opened (BR-15.5) |
| Order | Exactly the API order (newest first); the UI never re-sorts |
| Closed row | A "Closed" chip; no unread possible; tapping still opens read-only history |
| Load more | First page from API, then **Show more** (C-35, keyset) |

**Empty and filtered-empty states**

| Situation | Message |
|---|---|
| No conversations at all | "No conversations yet. When someone accepts your request, or you accept theirs, the chat appears here." |
| No closed conversations | "No closed conversations." with the Open segment selected |

### 4.2 Conversation (W-6.2)

```text
┌────────────────────────────────────────┐
│  ←  (Av) Neha S.                   ⋯   │
│  Do not share personal details or      │
│  money until you trust the person.     │
│  You can block or report any time.     │
├────────────────────────────────────────┤
│  Request message                       │
│  ┌─────────────────────────┐           │
│  │ Hello, I want to connect│           │
│  │ with you        09-Oct  │           │
│  └─────────────────────────┘           │
│          ┌────────────────────────┐    │
│          │ Hi Ashish! Meet at     │    │
│          │ Badarpur Metro at 8:10?│    │
│          │                 9:12 AM│    │
│          └────────────────────────┘    │
│  ───── New messages ─────              │
├────────────────────────────────────────┤
│  [ Type a message________ ] [ Send ]   │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | Back arrow, avatar, other member's name, `⋯` overflow. Tapping the name/avatar opens the **Connection detail** (M5) |
| `⋯` menu | **View connection**, **Rate** (M7), **Disconnect** (confirm), **Block** (M8), **Report** (M8). Items hidden until the owning module exists |
| Safety tip | A slim, **always visible, not dismissible** banner under the header (BR-15.9) |
| First message | Labelled **"Request message"** and styled as a system line, sent by the original requester (BR-15.2) |
| Bubbles | Own messages right, other's left, with time; system/request line spans the width and is visually distinct; no edit/delete controls (BR-15.6) |
| Divider | "New messages" drawn once at the first unread message (BR-15.5); disappears after reading |
| Composer | Fixed at the bottom above the keyboard; **Send** disabled when empty; a character counter appears near CFG-26; **text only** — no attach, image, voice or location controls (A-40) |
| Time | 12-hour India time (TD-17); dates shown per UI Plan §3.3 |
| Indicator | No read receipts, no typing indicator (A-41) |

### 4.3 Message states and delivery

| State | UI |
|---|---|
| Sending | Bubble shown immediately at low emphasis with a small "Sending" mark |
| Sent | Server time replaces "Sending"; stable order by server id |
| Failed | Bubble shows **"Not sent"** with a **Retry** action; retry reuses the same `clientMessageId` (no duplicate) |
| Reconnecting | A slim banner "Reconnecting…"; already-typed text stays in the composer |
| Throttled (G-5) | Inline/toast "You are sending messages too fast. Wait a moment." with Send re-enabled after a moment |
| Too long | Counter turns amber at the limit; at/over the limit the Send button is disabled and the message reads "Messages can be up to *n* characters." |
| Empty | Send disabled; no request is made |

### 4.4 Closed chat (read-only)

```text
┌────────────────────────────────────────┐
│  ←  (Av) Neha S.                 ⋯     │
│  Safety tip …                          │
├────────────────────────────────────────┤
│   … full history, read-only …          │
├────────────────────────────────────────┤
│  This chat is closed.                  │
│  You can read the history.             │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Composer | Replaced by a calm notice: "This chat is closed. You can read the history." (BR-15.7) |
| Reason | Neutral only — "Connection ended"; **never** says who blocked (BR-17.2) |
| Reading | All history scrolls and pages normally; no unread count |
| Safety actions | Block / Report remain available if M8 permits (safety is always reachable — UP-3) |
| Connection | "View connection" still opens the connection detail with its ended status (M5) |

### 4.5 Header menu actions

| Action | Level (UI Plan §7.3) | Behaviour |
|---|---|---|
| View connection | None | Opens M5 connection detail (`/connections/[id]`) |
| Rate | None | Opens M7 rating; hidden until M7; shown only when `canRate` |
| Disconnect | Sheet with consequences | M5 copy: "End this connection? The chat will close. You can both still read it…"; after success the chat becomes read-only |
| Block | Sheet with consequences (M8) | M8 owns copy; states "They are not told."; chat becomes read-only after |
| Report | Report form (M8) | M8 owns the form; the reporter is told Admin can read the attached conversation (BR-15.11) |

### 4.6 Connection access and empty conversation

| Situation | UI |
|---|---|
| Chat has only the request message | Show that one line under "Request message"; no empty-state card over a valid conversation |
| No messages at all (should not occur) | "No messages yet. Say hello." |
| Request not accepted yet | M6 is not reachable; M5 shows "You can chat once *Name* accepts." (SC-15.9) |
| Foreign or removed chat id | "This conversation is not available." with Back; never reveal another pair's data (BR-15.3) |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton conversation rows / message bubbles (C-26); never a bare spinner |
| Empty | Distinct copy for no conversations and no closed conversations (4.1) |
| Error (load) | Inline message with one **Try again**; the rest of the shell stays usable |
| Error (send) | Bubble-level "Not sent" + Retry; a toast for a transient failure |
| Success | Sent bubble turns stable; no celebratory toast for every message |
| Sending | Disable only the Send button while a request is in flight; show the optimistic bubble |
| Disabled | Send disabled with a reason: empty, over limit, closed chat, offline |
| No permission | Generic 404-style "This conversation is not available."; foreign ids never render data |
| Offline | Slim banner "No connection. Changes will not be saved."; Send disabled; queued text stays in the composer |
| Reconnecting | "Reconnecting…" banner; no message loss within the open screen |
| Session expired | Foundation dialog; typed text is preserved where possible (BR-5.6) |
| Verify / legal gate | Foundation; M6 not reachable until cleared |
| Real-time | Poll on the open conversation and on focus/visibility; no polling storm elsewhere (API plan §9) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Single column; `/chats` list full width, conversation full screen with back arrow; composer lifts above the keyboard and safe area |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | Master-detail: list left, conversation right; composer at the bottom of the right pane |
| Desktop 1024+ | **Master-detail** with the list on the left and the open conversation on the right (UI Plan §6.6); sheets become centred dialogs |
| Wide 1280+ | Member content max 1120 px; do not stretch message bubbles edge to edge |

- The shell switches bottom tabs to a left sidebar at tablet/desktop (UI Plan §4.1); M6 does not build the shell.
- Keep safe-area padding for the composer and the bottom nav; reserve space for the keyboard so the last message stays visible.
- At 200% text zoom the conversation must not scroll horizontally; long words wrap inside bubbles.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring; opening `⋯`/dialogs returns focus to the trigger |
| Labels | Composer labelled "Message to *Name*"; Send, Back and menu have accessible names |
| Targets | ≥ 44 × 44 px, 8 px apart (Send, Back, `⋯`, Retry) |
| Colour | Status and "New messages" always have text, not colour alone; unread count is a number |
| Screen readers | New incoming messages announced politely (live region); sender ("You" / *Name*) and time announced; the "New messages" divider is announced once |
| Keyboard | Every action works by keyboard; composer supports Enter to send where appropriate; sheets/dialogs trap focus |
| Motion | Honour "reduce motion"; no decorative animation when messages arrive |
| Language | `lang="en"`; dates `09-Oct-2026`, times `9:12 AM`, India time |

---

## 8. Microcopy (M6)

| Situation | Copy |
|---|---|
| List heading | "Chats" · segments "Open" / "Closed" |
| Safety tip (always) | "Do not share personal details or money until you trust the person. You can block or report at any time." |
| Request message label | "Request message" |
| Composer placeholder | "Type a message" |
| Empty list | "No conversations yet. When someone accepts your request, or you accept theirs, the chat appears here." |
| No closed | "No closed conversations." |
| New divider | "New messages" |
| Not sent | "Not sent" · "Retry" |
| Reconnecting | "Reconnecting…" |
| Throttled | "You are sending messages too fast. Wait a moment." |
| Too long | "Messages can be up to *n* characters." |
| Closed chat | "This chat is closed. You can read the history." |
| Closed reason | "Connection ended" (never who blocked) |
| Not available | "This conversation is not available." |
| Pending request (from M5) | "You can chat once *Name* accepts." (SC-15.9) |
| Offline | "No connection. Changes will not be saved." |
| Report notice (M8) | "Admin can read this conversation if you report it." (BR-15.11) |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8, UP-6).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load chat list (Open) | `GET /chats?status=open&limit=&cursor=` |
| Closed filter | `GET /chats?status=closed&limit=&cursor=` |
| Show more | same endpoint with `cursor=<nextCursor>` |
| Chats tab badge | `GET /chats/unread-count` |
| Open conversation header | `GET /chats/{chatId}` |
| Load newest messages | `GET /chats/{chatId}/messages?limit=` |
| Scroll up | `GET /chats/{chatId}/messages?before=<oldestId>&limit=` |
| Poll new messages | `GET /chats/{chatId}/messages?after=<lastId>` |
| Send message | `POST /chats/{chatId}/messages` `{ text, clientMessageId }` |
| Mark read (on open) | `POST /chats/{chatId}/read` `{ upToMessageId }` |
| View connection | M5 `GET /connections/{connectionId}` |
| Disconnect | M5 `POST /connections/{connectionId}/disconnect` |
| Rate | M7 (`/ratings/[connectionId]`) |
| Block / Report | M8 |

Rules for the client:

- Always send the auth cookie/token; derive the member on the server; never send a member or sender id.
- Never re-order rows or messages; trust the API order (server ids).
- Generate `clientMessageId` once per composed message and reuse it on Retry.
- On `404 CHAT_NOT_FOUND` show "This conversation is not available." and return to the list.
- On `409 CHAT_CLOSED` / `CONNECTION_NOT_ACTIVE` switch to the read-only state and refresh the header.
- On `400 MESSAGE_TOO_LONG` show the limit near the composer; on `429 MESSAGE_RATE_LIMITED` show the throttle copy and re-enable Send shortly.
- Poll only while the conversation is visible; stop on blur/hidden tab.
- Show the neutral closed reason regardless of any internal detail.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M5 accepted connections | No chats can exist; show the empty list |
| M5 connection detail | "View connection" is hidden |
| M7 rating | "Rate" is hidden in the `⋯` menu |
| M8 Block/Report | Block and Report are hidden; the safety tip still shows |
| M9 notifications | The Chats badge uses `GET /chats/unread-count`; no push required |
| M12 report chat access | No member-facing effect; the report notice is shown by M8 |
| Design tokens | Use UI Plan §3 values; no hard-coded colours |
| Realtime | Polling only; no SignalR dependency in Phase 1 |

---

## 11. Delivery plan (M6 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Reuse/build C-18 Conversation row and C-19 Message bubble/composer variants | UI Plan §3 |
| 2 | **Chats** list (`/chats`) with Open/Closed segments, unread badge, skeleton/empty/error | API 1, 6 |
| 3 | Conversation shell: header, safety tip, request message, message paging (newest + scroll up) | API 2, 3 |
| 4 | Composer: text-only, empty/limit states, optimistic send, Retry with `clientMessageId` | API 4 |
| 5 | Mark read on open + "New messages" divider + badge refresh | API 5, 6 |
| 6 | Polling (`after`) on the visible conversation; reconnecting banner | API 3 |
| 7 | Closed chat read-only state and neutral reason | API 2, 4 |
| 8 | `⋯` menu: View connection, Disconnect (via M5); gate Rate/Block/Report until ready | M5/M7/M8 |
| 9 | Desktop master-detail; sheets become dialogs | — |
| 10 | Accessibility pass (labels, focus, live regions, keyboard, 200% zoom) | — |
| 11 | QA: closed/send-fail/retry/throttle/offline, privacy, slow network | API running |
| 12 | Enable Rate, Block/Report as M7/M8 land | M7/M8 |

Suggested route files: `chats/index`, `chats/[id]`, with the Chats tab shell owned by the app shell.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-15.1 | A message over CFG-26 is blocked with "Messages can be up to *n* characters."; nothing is sent |
| AC-15.2 | A chat cannot be opened or sent to before acceptance (M5 shows the pending note) |
| AC-15.3 | A closed chat shows all history and has no composer; no send control exists |
| AC-15.4 | No edit or delete control exists on any message |
| AC-15.5 | No member screen exposes another pair's chat; admin access is not a member route |
| BR-15.2 | The request message is visible as the first line, labelled "Request message" |
| BR-15.4 | Composer offers text only — no attach, image, voice or location |
| BR-15.5 | Unread count appears on the list and clears on open; there is no read receipt or typing indicator |
| BR-15.7 | Closed chat is read-only; history readable |
| BR-15.9 | The safety tip is always visible and not dismissible |
| BR-17.2 | A closed chat never says who blocked whom |
| UP-3 | Block and Report are reachable from the conversation header (when M8 exists) |
| UP-4 | No email or mobile is ever rendered |
| UP-10 | Loading, empty, closed, sending, failed, reconnecting, throttled, offline and no-access states are implemented |
| UP-11 | Contrast, focus, labels, targets, keyboard and SR announcements pass |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| — | Open/Closed as segments or a filter link? | Segments (C-11), matching Requests in M5 |
| — | Does the list show the request message as the preview? | Yes — it is the conversation's first real message |
| — | Should an empty conversation exist in the list? | Yes, one request message always exists (BR-15.2) |
| — | Optimistic send vs wait for the server? | Optimistic bubble with "Sending" → "Sent"/"Not sent" + Retry |
| — | Polling interval? | ~3–5 s while the conversation is visible; stop on blur |
| — | Where do Rate/Disconnect/Block/Report live? | `⋯` header menu, not a footer |
| — | Does the "New messages" divider persist after reading? | No; it clears once read |
| — | Is the safety tip dismissible? | No (BR-15.9) |
| — | Do closed chats appear by default? | No; under the **Closed** segment only |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-15.1 Chat List | §4.1 |
| SCR-15.2 Conversation | §4.2 |
| Field spec (Message) | §4.2, §4.3 |
| BR-15.1 chat only for Accepted | §2 gates, §4.6 |
| BR-15.2 first message = request message | §4.2 |
| BR-15.3 only the two members | §4.6, §5 no-permission |
| BR-15.4 text only + CFG-26 | §4.2, §4.3, §8 |
| BR-15.5 delivery + unread, no receipts/typing | §4.1, §4.2, §5 |
| BR-15.6 no edit/delete | §4.2, §12 |
| BR-15.7 closing + read-only | §4.4 |
| BR-15.8 new connection = new chat | §4.1 (separate rows) |
| BR-15.9 safety tip; phone/links allowed | §4.2, §8 |
| BR-15.10 mobile never shown | §4.2, §12 |
| BR-15.11 Admin access via report | §4.5, §8 |
| BR-15.12 never purged | §4.1, §4.4 |
| Status lifecycle §15.5 | §4.1, §4.4 |
| CFG-26 / CFG-30 | §4.3, §10 |
| V-38 / V-39 / V-40 | §4.3, §4.4, §12 |
| SC-15.1…SC-15.10 | §4, §5 |
| AC-15.1…AC-15.5 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §6.6 (W-6.1, W-6.2) | §4 |
| UI Plan §7 state/confirmation patterns | §4.5, §5, §6 |
| UI Plan §8 delivery | §11 |
