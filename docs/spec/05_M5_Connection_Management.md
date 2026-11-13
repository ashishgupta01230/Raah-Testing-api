# Module M5 — Connection Management

> Source: `RAAH_Functional_Specification.md` Section 14. Build order #5.

## Purpose

Let one member ask another to connect, let the other accept or decline, and open a chat only when both agree. A connection does **not** book or fix any journey (Principle 7, Rule 8).

## Who Uses It

Any Member, as sender or receiver (D-6). Admin views connections for monitoring (M11); Admin cannot create, accept or reject a connection (BR-4.5).

## Dependencies

- M4 (leads to connect from), M3 (route snapshots), M1 (limited profile).
- Foundation config: CFG-5 (expiry), CFG-6 (pending limit), CFG-17 (cool-off), CFG-25 (request message).
- Foundation statuses: Connection (9.6). Record numbering CON-.

## Screens

| Screen | Purpose |
|---|---|
| SCR-14.1 Confirm Request | Shows the fixed request message (read-only) before a request is sent from a lead |
| SCR-14.2 Requests Sent | Pending and past requests the member sent, with Withdraw |
| SCR-14.3 Requests Received | Pending and past requests the member received, with Accept / Reject |
| SCR-14.4 My Connections | Accepted connections, with Open Chat |
| SCR-14.5 Connection Detail | Other member's limited profile, both routes as at request time, chat link, Disconnect, Block, Report, Rate |

## Field Specification — Connection

| Field | Source | Notes |
|---|---|---|
| Connection Number | Auto | CON-nnnnnn. Shared by the request and the connection it becomes (BR-7.3) |
| Sender | Auto | The member who pressed Connect |
| Receiver | Auto | The other member |
| Created from lead | Auto | Link to the lead |
| Route snapshot | Auto | Exact versions of both routes and per-leg match result at the moment the request was sent (Principle 14) |
| Status | Auto | Section 9.6 |
| Sent on / Responded on / Expires on | Auto | Expires on = Sent on + CFG-5 days |
| Request message | Text | Fixed text "Hello, I want to connect with you" (CFG-25). Read-only. Sender cannot change it (D-23). |
| Ended on / Ended by | Auto | For Disconnected, Blocked, Withdrawn |

## Business Rules

**BR-14.1** — A request can be sent **from a lead only**. Neither member can request someone who is not among their current leads.

**BR-14.2** — **Either side can send** (D-6): Saathi can request Raahi and Raahi can request Saathi. Nobody is connected until the receiver accepts (Principle 6).

**BR-14.3** — A request is allowed only if: sender and receiver are Active (a Restricted sender must not be restricted from sending); neither has blocked the other (BR-4.4); the sender has fewer than CFG-6 Pending requests; no open connection (Pending or Accepted) exists between the two; and the cool-off (CFG-17) is not running (A-36).

**BR-14.4** — **One open connection per pair** (A-35). If Ashish has a Pending request to Rohit and Rohit presses Connect on Ashish's lead, Rohit is shown Ashish's request with an **Accept** option; no second request is created.

**BR-14.5** — **Accept**: only the receiver can accept. Status → Accepted, chat opens (M6), both notified, each sees the other in My Connections.

**BR-14.6** — **Reject**: only the receiver. Status → Rejected (final). Sender sees "Your request was not accepted" with no reason. Sender cannot re-request for CFG-17 days (default 7).

**BR-14.7** — **Withdraw**: only the sender, only while Pending. Status → Withdrawn (final).

**BR-14.8** — **Expiry**: a Pending request unanswered within CFG-5 days (default 30) becomes Expired (final). Sender told; receiver not penalised; no cool-off after expiry.

**BR-14.9** — **Pending requests always belong to Active routes.** If a member's route is deactivated, or the member is blocked or deactivated, all their Pending requests (sent and received) are Withdrawn automatically (A-6, BR-11.14).

**BR-14.10** — **Editing an Active route** does not change Pending requests (A-28). The receiver sees the sender's current route and a note "Route changed since this request was sent". The request keeps its snapshot.

**BR-14.11** — **Disconnect**: either member can end an Accepted connection. Status → Disconnected (final), chat closes, other member told. Either can later receive a new request through a fresh lead.

**BR-14.12** — **Block** (M8) ends any open connection immediately, status Blocked.

**BR-14.13** — A member may hold **any number of Accepted connections** (A-37).

**BR-14.14** — Connection data are **never deleted** (BR-8.1). Closed connections stay in history, read-only.

**BR-14.15** — Accepting does not book, schedule or promise anything. Screen states: "You can now chat. Decide time, meeting point and shared portion together."

**BR-14.16** — A member sees only connections they belong to (BR-4.1). Opening another connection's number is denied and logged (BR-4.2).

**BR-14.17** — **Request message** (D-23): every request carries the fixed message "Hello, I want to connect with you" (CFG-25). SCR-14.1 shows it read-only. The receiver sees it on SCR-14.3 and SCR-14.5. On Accept it becomes the **first message in the chat** (M6). On Reject/Withdraw/Expire the message stays only on the closed request record.

## Status Lifecycle

Uses Section 9.6.

| From | To | Who | Condition |
|---|---|---|---|
| (new) | Pending | System | Sender presses Connect and confirms |
| Pending | Accepted | Receiver | Accept |
| Pending | Rejected | Receiver | Reject |
| Pending | Withdrawn | Sender | Withdraw; or System when route deactivated or member blocked/deactivated |
| Pending | Expired | System | CFG-5 days passed |
| Accepted | Disconnected | Either member | Disconnect |
| Accepted | Blocked | Either member | Block (M8) |

## Scenarios

**SC-14.1 — Saathi connects with a Raahi**
1. Ashish opens Neha's lead, taps Connect. SCR-14.1 shows the fixed message read-only; he confirms.
2. Request CON-000087 is Pending. Neha notified (M9).
3. Neha opens SCR-14.3, sees Ashish's limited profile and route comparison, taps Accept.
4. Connection Accepted. Both can open the chat.

**SC-14.2 — Raahi connects with a Saathi (D-6)**
1. Amit (Raahi) finds Ashish in his leads, taps Connect.
2. Ashish accepts. Flow is the same in the opposite direction.

**SC-14.3 — Both press Connect**
1. Ashish has a Pending request to Rohit. Rohit, not having seen it, opens Ashish's lead and presses Connect.
2. System shows Rohit Ashish's request and asks "Accept this request?". No second request (A-35).

**SC-14.4 — Reject and cool-off**
1. Rohit rejects Ashish's request.
2. Ashish sees "Your request was not accepted". Connect disabled 7 days on Rohit's lead (CFG-17). Rohit may still send Ashish a request.

**SC-14.5 — Sender withdraws**
1. Ashish withdraws while Pending.
2. Status Withdrawn; lead returns to Viewed. He can send again immediately; no cool-off.

**SC-14.6 — Request expires**
1. Neha never answers.
2. Day 30: request Expired; Ashish told. He can send a new one from the lead.

**SC-14.7 — Too many pending requests**
1. A member has 10 Pending (CFG-6).
2. Connect disabled until one is answered, withdrawn or expires.

**SC-14.8 — Sender's route deactivated while pending**
1. Ashish deactivates his route for a week.
2. All his Pending requests (sent and received) Withdrawn. Accepted connections/chats stay.

**SC-14.9 — Sender edits his route while pending**
1. Ashish sent a request to Neha, then changed Going time 08:00 → 08:10.
2. Request stays Pending. Neha sees Ashish's current route with "Route changed since this request was sent", and the route as it was at request time.

**SC-14.10 — Disconnect**
1. Neha taps Disconnect.
2. Status Disconnected; chat closes; Ashish told. History kept.

**SC-14.11 — Request to a member just blocked by Admin**
1. Ashish presses Connect on a lead whose member was blocked a minute ago.
2. Lead already withdrawn; system says "This person is no longer available."

**SC-14.12 — Accept after the sender became inactive**
1. Request Pending, then sender deactivates.
2. Request Withdrawn automatically; receiver can no longer accept.

**SC-14.13 — The request message is fixed**
1. Ashish presses Connect. SCR-14.1 shows the message read-only; no edit field.
2. He confirms. Neha sees the same message; after accept it appears as first chat message.

## Acceptance Criteria

**AC-14.1 — Either side can send**
- Given a Saathi lead in a Raahi's list
- When the Raahi presses Connect and confirms
- Then a Pending request is created for the Saathi.

**AC-14.2 — Only the receiver can accept**
- Given a Pending request
- When the sender tries to accept it by editing the address
- Then the system denies access and logs the attempt.

**AC-14.3 — Chat opens only on acceptance**
- Given a Pending request
- When no one has accepted
- Then no chat exists between the two members.

**AC-14.4 — Expiry**
- Given CFG-5 is 30
- When 30 days pass without a response
- Then the status is Expired and the sender is notified.

**AC-14.5 — Pending request limit**
- Given CFG-6 is 10 and a member has 10 Pending requests
- When they press Connect on another lead
- Then the request is not created.

**AC-14.6 — One open connection per pair**
- Given a Pending request from A to B
- When B presses Connect on A's lead
- Then B is shown A's request to accept and no second request is created.

**AC-14.7 — Deactivation withdraws pending only**
- Given a member has one Pending request and one Accepted connection
- When the member deactivates their route
- Then the Pending request becomes Withdrawn and the Accepted connection stays.

**AC-14.8 — Cool-off**
- Given CFG-17 is 7 and B rejected A's request today
- When A tries to send B a request tomorrow
- Then the system refuses and shows the remaining days.

**AC-14.9 — Fixed request message**
- Given a member presses Connect on a lead
- When the confirmation screen opens
- Then the message "Hello, I want to connect with you" is shown read-only and every request is sent with it.

**AC-14.10 — Message reaches chat only on acceptance**
- Given a Pending request with a message
- When the receiver accepts
- Then the message appears as the first chat message; and if the request is rejected, no chat is created.

## Data Impact

| Event | Records created or changed |
|---|---|
| Send request | Connection (Pending) with route snapshot and request message; lead → Requested; receiver notified |
| Accept | Connection Accepted; chat created |
| Reject / Withdraw / Expire | Connection final; lead returns to Viewed |
| Disconnect / Block | Connection final; chat closed |

## Reports Produced

Connection Register (RPT-10), Connection Funnel (RPT-11), Request Response Time (RPT-12) — in M15.

## Cross-References

- **Config:** CFG-5, CFG-6, CFG-17, CFG-25.
- **Statuses:** Connection (9.6).
- **Validations:** V-31 … V-37.
- **Decisions/assumptions:** D-6, D-23, A-28, A-35, A-36, A-37.
