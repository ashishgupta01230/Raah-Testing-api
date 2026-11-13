# Module M8 — Safety: Block & Report (Member Side)

> Source: `RAAH_Functional_Specification.md` Section 17. Build order #8.

## Purpose

Give every member two simple tools they can use at any time — **Block** to stop contact immediately and **Report** to ask Admin to review a person — so that a member never has to stay in contact with someone who makes them uncomfortable. Admin handling is in Module M12.

## Who Uses It

Any Member. Admin reviews reports (M12).

## Dependencies

- M4 (leads), M5 (requests and connections), M6 (chat), M7 (rating comments can be reported).
- Foundation config: CFG-15 (reports that trigger an alert).
- Foundation statuses: Safety report (9.7), Member (9.1). Record numbering SAF-.
- Admin side built later in M12.

## Screens

| Screen | Purpose |
|---|---|
| SCR-17.1 Block Member | Confirmation, reachable from a lead, a connection or a chat |
| SCR-17.2 Blocked Members | List of members the member has blocked, with **Unblock** |
| SCR-17.3 Report Member | Reason, description and optional "also block" |
| SCR-17.4 My Reports | Reports the member raised and their current status |

An emergency note is shown on SCR-17.3: "RAAH does not provide emergency services. If you are in danger, contact local emergency services."

## Block — What Happens

| Effect | Detail |
|---|---|
| Leads | Both members' leads for each other are withdrawn, and they are removed from matching (BR-12.9) |
| Pending requests | Withdrawn in both directions |
| Accepted connection | Becomes Blocked; the chat is Closed |
| Visibility | Neither member sees the other in leads or connection lists. The blocked member is **not told** they were blocked; the closed connection simply shows as ended |
| Ratings | Already given ratings stay |
| Reversal | The blocker can Unblock from SCR-17.2 (A-47) |

## Field Specification — Report

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Report Number | Auto | — | SAF-nnnnnn |
| Reported Member | Auto | — | From the screen the member came from |
| Reason | Dropdown | Yes | Harassment / Inappropriate messages / Fake profile or wrong details / Safety concern / Spam / Other (A-46) |
| Description | Text | Yes for Other; otherwise optional | Up to 500 characters |
| Conversation attached | Auto | — | If a chat exists with that member, it is attached with the reporter's knowledge (BR-15.11) |
| Also block this member | Checkbox | No | If ticked, a block is applied (BR-17.1) |
| Status | Auto | — | Section 9.7 |

## Business Rules

**BR-17.1** — **Block** is available from a lead, a connection and a chat, and it takes effect **immediately** and in **both directions** (Section 17.4).

**BR-17.2** — The blocked member is **never notified** of the block and cannot see who blocked them.

**BR-17.3** — A member can **Unblock** at any time. After unblocking, matching includes the pair again. Unblocking does not restore a closed connection or chat; a new request is needed.

**BR-17.4** — A member can **report** any member with whom they have a lead, a current or past connection, or an Accepted chat (A-46). A member cannot report someone with whom they have had no contact on the platform.

**BR-17.5** — Before submission the screen tells the reporter that Admin will be able to read the attached conversation (BR-15.11).

**BR-17.6** — A report **does not block or restrict anyone automatically**. It is a request for review. Reporters may tick "also block" to protect themselves immediately.

**BR-17.7** — The **reported member is not told who reported them.** They are told only of the final admin action, if any.

**BR-17.8** — The reporter sees status updates on SCR-17.4 (Submitted / Under Review / Action Taken / Dismissed / Closed) and a notification when the report is decided. They are not told which action was applied to the other member.

**BR-17.9** — **Escalation**: when **CFG-15** (default 3) different members have reported the same member, Admin is alerted and the member is flagged on the Admin dashboard (M12).

**BR-17.10** — A report is **never deleted**. Dismissed and closed reports stay in history, visible to Admin.

**BR-17.11** — A member who raises many reports found to be unfounded may be restricted by Admin (M12); this is an Admin decision, not an automatic rule.

**BR-17.12** — A **rating comment** can also be reported (BR-16.9). It goes to the Admin moderation queue.

## Status Lifecycle (Report)

Uses Safety Report Status of Section 9.7.

| From | To | Who |
|---|---|---|
| (new) | Submitted | Member |
| Submitted | Under Review | Admin |
| Under Review | Action Taken | Admin (Warning, Restriction, Temporary Block, Permanent Block) |
| Under Review | Dismissed | Admin, with reason |
| Action Taken / Dismissed | Closed | Admin |

## Scenarios

**SC-17.1 — Block from a lead**
1. Ashish dislikes a profile and taps Block on the lead.
2. The lead disappears from both lists. The other member is not notified.

**SC-17.2 — Block a connected member**
1. Ashish blocks Rohit from the chat header.
2. The connection becomes Blocked, the chat closes, and any pending request between them is withdrawn.

**SC-17.3 — Unblock**
1. Ashish later unblocks Rohit from SCR-17.2.
2. Their routes match again and leads reappear, but there is no connection. A new request is needed.

**SC-17.4 — Report with chat attached**
1. Ashish reports Rohit for "Inappropriate messages", ticks "Also block".
2. The report SAF-000009 is Submitted with the conversation attached. The connection is Blocked immediately.

**SC-17.5 — Report as Other**
1. A member chooses "Other" and leaves the description empty.
2. System requires a description before submitting.

**SC-17.6 — Report on a stranger**
1. A member tries to report someone never seen as a lead or connection.
2. No such option exists. A direct attempt is denied.

**SC-17.7 — Three reporters**
1. Three different members report Rohit within a short time.
2. Admin receives an alert (CFG-15 = 3) and Rohit is flagged on the Admin dashboard.

**SC-17.8 — Reported member told of outcome**
1. Admin issues a Warning to Rohit.
2. Rohit is told he received a warning. He is not told who reported him.

**SC-17.9 — Reporter follows up**
1. Ashish opens SCR-17.4 and sees SAF-000009 as Under Review, later Closed.

## Acceptance Criteria

**AC-17.1 — Immediate mutual block**
- Given two members have an Accepted connection
- When one blocks the other
- Then both leads are withdrawn, pending requests are withdrawn, the connection is Blocked and the chat is Closed immediately.

**AC-17.2 — Blocked member not notified**
- Given a member has been blocked
- When they use the app
- Then no message tells them they were blocked.

**AC-17.3 — Report does not block automatically**
- Given a member submits a report without ticking "also block"
- When the report is saved
- Then no block or restriction is applied.

**AC-17.4 — Reporter privacy**
- Given a report against a member
- When the reported member views their notifications
- Then the reporter's identity is not shown.

**AC-17.5 — Escalation threshold**
- Given CFG-15 is 3
- When a third different member reports the same member
- Then Admin is alerted.

## Data Impact

| Event | Records created or changed |
|---|---|
| Block | Block record; leads withdrawn; connection Blocked; chat Closed; audit entry |
| Unblock | Block record ended; pair re-matched |
| Report | Safety Report created (SAF); conversation attached if present; Admin notified |

## Reports Produced

RPT-17 Safety Report Register, RPT-22 Member Block Summary — in M15.

## Cross-References

- **Config:** CFG-15.
- **Statuses:** Safety report (9.7), Member (9.1).
- **Validations:** V-43, V-44.
- **Decisions/assumptions:** A-46, A-47.
- **Related modules:** M6 (chat), M12 (Admin handling).
