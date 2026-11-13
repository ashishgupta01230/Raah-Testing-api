# Module M6 — In-App Chat

> Source: `RAAH_Functional_Specification.md` Section 15. Build order #6.

## Purpose

Give two **connected** members one safe place to talk and to decide the journey between themselves — time, meeting point, shared portion, days (D-5). Chat is the **only** communication channel in RAAH (D-1, Principle 9).

## Who Uses It

The two members of an Accepted connection. Admin can read a conversation only inside an open Safety Report that involves it (BR-4.7, A-9).

## Dependencies

- M5 (Accepted connections; request message becomes first message).
- Foundation config: CFG-26 (message length), CFG-30 (unread email delay, used by M9).
- Foundation: BR-4.7, BR-4.9, audit (Section 8), safety (M8/M12 for reports).

## Screens

| Screen | Purpose |
|---|---|
| SCR-15.1 Chat List | All the member's conversations, newest first, with unread counts |
| SCR-15.2 Conversation | Messages, message box, header with the other member's limited profile and actions Block · Report · Disconnect · Rate |

## Field Specification — Message

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Message text | Text | Yes | 1 to CFG-26 characters (default 1000). Text only (A-40). |
| Sender | Auto | — | Logged-in member |
| Sent time | Auto | — | System timestamp |

A conversation has no title and no members other than the two connected members. There are no group chats.

## Business Rules

**BR-15.1** — A chat exists **only for an Accepted connection** (A-9). No chat for Pending, Rejected, Withdrawn or Expired requests (AC-14.3).

**BR-15.2** — When a request is Accepted, the chat is created and its **first message is the request message** (D-23), shown as sent by the original sender at the time of the request.

**BR-15.3** — Only the two members can open the chat. Another member who edits the address gets "access denied", logged (BR-4.2).

**BR-15.4** — Messages are **text only** (A-40): no images, files, voice or live location. Maximum CFG-26 characters. An empty message cannot be sent.

**BR-15.5** — Messages delivered almost immediately. Unread counts per conversation shown. No read receipts or typing indicators (A-41).

**BR-15.6** — A message **cannot be edited or deleted** by either member once sent (A-42, Principle 13).

**BR-15.7** — **Closing**: a chat becomes **Closed** when the connection ends — Disconnected, Blocked, or either member made Inactive or Blocked by Admin. A Closed chat accepts no new messages; both can still read history, read-only (A-42).

**BR-15.8** — **A new connection after a closed one starts a new chat.** The old chat stays as history, not merged.

**BR-15.9** — **Phone numbers and links are not blocked in chat** (A-43). Standing safety tip shown: "Do not share personal details or money until you trust the person. You can block or report at any time."

**BR-15.10** — The member's mobile number is never added to the chat by the system (BR-4.9, D-21).

**BR-15.11** — **Admin access**: chat content hidden from Admin normally. When a Safety Report involves a chat, the conversation is attached to the report and Admin can read it for the review. Each access is logged (BR-8.4). The reporting member is told, before submitting, that Admin will be able to read the conversation.

**BR-15.12** — Chat messages are never purged in Phase 1.

## Status Lifecycle

| Status | Meaning | Moves to Closed when |
|---|---|---|
| Open | Both connected; messages can be sent | Connection becomes Disconnected or Blocked, or a member is made Inactive or Blocked |
| Closed | Read-only history | — (final) |

## Scenarios

**SC-15.1 — First message from the request**
1. Neha accepts Ashish's request that carried "Hello, I want to connect with you".
2. The chat opens with that message as the first line. Neha replies.

**SC-15.2 — Deciding the journey**
1. Ashish and Neha agree in chat: meet at Badarpur Metro at 8:10, travel Tue–Thu, Ashish pays for his share of petrol.
2. RAAH stores only the messages. It records no agreement and does not remind or enforce anything (D-5, Principle 8).

**SC-15.3 — Unread messages**
1. Neha sends two messages while Ashish is away.
2. Ashish's chat list shows "2 unread" on that conversation. Opening it clears the count.

**SC-15.4 — Disconnect**
1. Neha disconnects.
2. The chat closes. Both can read the history; neither can send.

**SC-15.5 — Block**
1. Ashish blocks Neha from the chat header.
2. The connection is Blocked and the chat Closed. History stays read-only.

**SC-15.6 — Member shares a phone number in chat**
1. Neha types her number.
2. The message is sent normally; the chat keeps showing the safety tip. RAAH does not prevent it.

**SC-15.7 — Message too long**
1. Member pastes 1,500 characters.
2. System asks them to shorten it to 1,000.

**SC-15.8 — Admin reviews a reported chat**
1. Ashish reports Rohit for abusive messages.
2. Admin opens the report and reads the attached conversation. The access is logged.
3. Without a report, Admin sees no chat content.

**SC-15.9 — Chat with a pending request**
1. Ashish has a Pending request to Rohit.
2. There is no chat yet. Screen: "You can chat once Rohit accepts."

**SC-15.10 — New connection after a disconnect**
1. Ashish and Neha disconnect, later match again, and connect afresh.
2. A new chat opens. The earlier closed chat remains in their list as history.

## Acceptance Criteria

**AC-15.1 — Text only and length**
- Given CFG-26 is 1000
- When a member sends a 1,001-character message
- Then the system rejects it and nothing is sent.

**AC-15.2 — Chat only after acceptance**
- Given a request is Pending
- When either member opens the connection
- Then no chat messages can be sent.

**AC-15.3 — Closed chat is read-only**
- Given a connection becomes Disconnected
- When a member opens the chat
- Then they can read all messages and cannot send a new one.

**AC-15.4 — No edit or delete**
- Given a message was sent
- When the sender looks for edit or delete
- Then no such option exists.

**AC-15.5 — Admin access only through a report**
- Given Admin has no open report involving a chat
- When Admin looks for that chat's content
- Then the content is not available.

## Data Impact

| Event | Records created or changed |
|---|---|
| Request Accepted | Chat created; request message becomes first message |
| Message sent | Message stored; unread count updated; notification (M9) |
| Connection ended | Chat set to Closed |
| Report raised | Conversation attached to the report; access logged |

## Reports Produced

Chat Activity Summary (RPT-13, counts only, no content) — in M15. Chat Access Log (RPT-27).

## Cross-References

- **Config:** CFG-26, CFG-30.
- **Validations:** V-38, V-39, V-40.
- **Decisions/assumptions:** D-1, D-5, D-23, A-9, A-40, A-41, A-42, A-43.
