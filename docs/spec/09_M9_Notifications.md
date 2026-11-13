# Module M9 — Notifications

> Source: `RAAH_Functional_Specification.md` Section 18. Build order #9.

## Purpose

Tell members and Admin, promptly and briefly, about the events that need their attention — without exposing private details and without being the only way to see anything.

## Who Uses It

Every Member and Admin.

## Dependencies

- All core modules M4–M8 produce the events listed in the matrix.
- Foundation config: CFG-16 (channels), CFG-29 (expiry reminder days), CFG-30 (unread chat email delay), CFG-31 (daily digest time).
- Foundation: BR-4.9 (no private details), audit (Section 8).

## Screens

| Screen | Purpose |
|---|---|
| SCR-18.1 Notification Centre | In-app list of notifications with read/unread, filter by type |
| SCR-18.2 Notification Preferences | Switch optional email notifications on or off |

## Notification Matrix (default, A-48)

| Event | Who is notified | Channel |
|---|---|---|
| Email verification | New member | Email |
| Email verified, welcome | Member | In-app |
| New lead found | Member | In-app; **daily digest email** of new leads at CFG-31 |
| Connection request received | Receiver | In-app + email |
| Request about to expire (CFG-29 days before) | Receiver | In-app + email |
| Request accepted | Sender | In-app + email |
| Request rejected | Sender | In-app (neutral wording) |
| Request withdrawn by sender | Receiver | In-app |
| Request expired | Sender | In-app |
| Pending request withdrawn by system (route deactivated, member blocked or deactivated) | The other member | In-app |
| New chat message | Recipient | In-app; one email if still unread after CFG-30 hours |
| Disconnected | The other member | In-app |
| Rating received | Rated member | In-app |
| Rating comment hidden | Rater | In-app |
| Report submitted | Reporter | In-app |
| Report decided | Reporter | In-app |
| Warning, Restriction, Temporary Block, Permanent Block | The member concerned | In-app + email |
| Route deactivated by Admin | Member | In-app + email |
| Profile photo removed by Admin | Member | In-app |
| Account reactivated by Admin | Member | Email |
| Terms of Use, Privacy Policy or Safety Guidelines updated and re-acceptance required | All Members | In-app + email |
| Suggested city added or declined | The member who suggested it | In-app |
| Email change verification, password reset | Member | Email |
| Member reaches CFG-15 reports | Admin | In-app + email |
| New report submitted | Admin | In-app (digest by email) |
| Reported rating comment | Admin | In-app |

## Business Rules

**BR-18.1** — Notifications are **informational**. No business process depends on a notification being read. Lists such as leads and requests are the authoritative view.

**BR-18.2** — Notifications and emails **never contain** chat text, a mobile number, an email address of another member, or a reason for a rejection or a block. They name the other member and give a link only.

**BR-18.3** — A notification about a rejection uses neutral wording ("Your request was not accepted"). A block is never notified (BR-17.2).

**BR-18.4** — **Email preferences**: a member can switch off the email for *new leads digest* and *unread chat reminder*. Emails for safety, verification, password and account actions cannot be switched off.

**BR-18.5** — New leads are sent as **one daily digest**, not one email per lead, to avoid alert fatigue.

**BR-18.6** — **Expiry reminder**: a receiver is reminded CFG-29 days (default 5) before a Pending request expires.

**BR-18.7** — A **new-message email** is sent at most once per conversation until the recipient next opens it, and only if the message is unread after CFG-30 hours (default 12).

**BR-18.8** — Members can mark notifications as read and filter by type. Notifications older than 90 days are archived, not deleted.

**BR-18.9** — Channels are **In-app and Email**. SMS is not part of Phase 1; the framework is built so SMS can be added later (D-8, CFG-16).

**BR-18.10** — A notification for a deactivated, blocked or Inactive member is not sent, except the email about their own account status.

**BR-18.11** — **Wording of the request notice** follows the business flow: to a Raahi, "*Name* is looking for someone on a route that overlaps with your regular route"; to a Saathi, "*Name* regularly travels a route that overlaps with yours". The notice gives the name and a link only (BR-18.2).

## Status Lifecycle

A notification is created **Unread**, becomes **Read** when the member opens it, and is **Archived** (not deleted) after 90 days.

| From | To | Who |
|---|---|---|
| (new) | Unread | System |
| Unread | Read | Member |
| Read / Unread | Archived | System (90 days) |

## Scenarios

**SC-18.1 — Request received**
1. Ashish sends Neha a request. Neha gets an in-app notification and an email: "Ashish wants to connect with you" with a link.
2. The email has no route details beyond the link.

**SC-18.2 — New leads digest**
1. Five new Raahis match Ashish overnight.
2. At the digest time he gets one email summarising the count, with a link to the lead list.

**SC-18.3 — Expiry reminder**
1. A request is 25 days old (CFG-5 = 30, CFG-29 = 5).
2. Neha gets a reminder that it will expire in 5 days.

**SC-18.4 — Rejection wording**
1. Rohit rejects Ashish's request.
2. Ashish sees "Your request was not accepted." No reason is shown.

**SC-18.5 — Unread chat email**
1. Neha messages Ashish; he does not open it for 12 hours.
2. He receives one email "You have a new message from Neha". Later messages do not send more emails until he opens the chat.

**SC-18.6 — Member turns off digest**
1. Ashish switches off the leads digest email.
2. He still gets in-app notices and all safety and security emails.

**SC-18.7 — Admin alert**
1. A third member reports the same person.
2. Admin gets an in-app and email alert.

**SC-18.8 — Blocked member**
1. Ashish blocks Rohit.
2. Rohit receives no notification of the block.

## Acceptance Criteria

**AC-18.1 — No private content in notifications**
- Given a chat message is sent
- When the email or notification is created
- Then it contains the sender's name and a link, never the message text.

**AC-18.2 — Digest, not one per lead**
- Given three new leads are created before the digest time
- When the digest is sent
- Then one email summarises all three.

**AC-18.3 — Mandatory emails**
- Given a member has switched off all optional emails
- When Admin issues a warning
- Then the member still receives the email.

**AC-18.4 — Expiry reminder timing**
- Given CFG-29 is 5 and a request expires on day 30
- When day 25 arrives
- Then the receiver is reminded.

## Data Impact

| Event | Records created or changed |
|---|---|
| Any listed event | Notification record per recipient; email queued where the matrix says so |
| Member marks read | Notification status updated |

## Reports Produced

RPT-23 Notification Delivery Summary — in M15.

## Cross-References

- **Config:** CFG-16, CFG-29, CFG-30, CFG-31.
- **Validations:** V-49 (no private details).
- **Decisions/assumptions:** D-8, A-48.
- **Related modules:** M5–M8 (event sources), M12/M13 (Admin actions), M14 (latest five on dashboard).
