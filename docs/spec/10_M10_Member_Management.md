# Module M10 — Member Management & Verification

> Source: `RAAH_Functional_Specification.md` Section 19. Build order #10.

## Purpose

Let Admin find any member, see their status and verification, and take the account-level actions the platform needs: deactivate, reactivate, unlock, verify, correct gender, remove a photo, send a password reset.

## Who Uses It

Admin only.

## Dependencies

- M1 (profile), M2 (route), M5 (connections), M6 (chat), M7 (ratings), M8 (safety counts).
- Foundation: member statuses (9.1), registration/login (Section 5), audit (Section 8).
- Admin entry: Admin accounts exist directly in the database (D-19).

## Screens

| Screen | Purpose |
|---|---|
| SCR-19.1 Member List | Search and filter all members |
| SCR-19.2 Member Detail | Everything about one member, with History tab |
| SCR-19.3 Change Member Status | Deactivate or reactivate, with reason |
| SCR-19.4 Account Actions | Unlock, mark email verified, send password reset, correct gender, remove photo |

## Member List (SCR-19.1)

| Filter / search | Options |
|---|---|
| Search | Name, email, mobile number, member number (MBR-) |
| Status | Unverified / Active / Inactive / Restricted / Temporarily Blocked / Permanently Blocked |
| Journey role | Saathi / Raahi |
| Verification | Email Verified / Unverified |
| Flags | Locked account · Has open report · Flagged by CFG-15 · Inactive but holding open items |
| Registered | Date range |

Columns: Member number, name, email, role, vehicle, status, verification, registered on, last login, active route (yes/no).

## Member Detail (SCR-19.2)

| Block | Content |
|---|---|
| Profile | Name, photo, gender, email, **mobile number (visible to Admin, D-21)**, journey role, vehicle, member since |
| Account | Status, verification, locked flag, failed-login count, last login, login history (RPT-25 extract) |
| Route | Current Active route (both legs) and past routes, read-only (Module M11) |
| Activity counts | Leads, requests sent/received, connections (by status), chats (count only), ratings received (average and count) |
| Safety | Reports against the member (count, status), reports raised by the member, warnings, restrictions, blocks |
| History | Every account action, by whom, when, with reason |

**Admin cannot see** chat content here (BR-4.7) and cannot see a member's password.

## Field Specification — Account Action

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Action | Select | Yes | Deactivate / Reactivate / Unlock / Mark email verified / Send password reset / Correct gender / Remove photo |
| Reason | Text | Yes | At least 10 characters (A-54) |
| New gender | Select | For Correct gender | Male / Female / Other |

## Business Rules

**BR-19.1** — Every Admin action on a member needs a **reason** and is audited. The member is notified where Section 18.4 says so.

**BR-19.2** — **Deactivate.** The member becomes Inactive and cannot log in. The system then: ends any open session; deactivates the Active route; withdraws all Pending requests (sent and received); sets Accepted connections to Disconnected and closes their chats; withdraws all leads both ways. Nothing is deleted (BR-8.1). The same effects apply as for self-deactivation (BR-10.9).

**BR-19.3** — **Reactivate.** An Inactive member becomes Active again. Their old route stays Inactive and the member must reactivate it (BR-11.15). Closed connections stay closed. The member is told by email.

**BR-19.4** — **Unlock** clears a lock caused by failed logins (BR-5.5) immediately.

**BR-19.5** — **Mark email verified** (A-49): sets the member Active as if they had opened the link, with the reason recorded. It is used only when the verification email could not reach the member.

**BR-19.6** — **Send password reset**: Admin triggers the normal reset email. Admin never sets, sees or receives a password (BR-5.8).

**BR-19.7** — **Correct gender** (A-22, BR-10.3): changes the value, records old and new value and the reason, and applies to filters from the next search.

**BR-19.8** — **Remove photo** (BR-10.5): removes the photo, notifies the member, records the reason.

**BR-19.9** — Admin **cannot edit** a member's name, email, mobile number, route, leads, requests, connections, messages or ratings. These belong to the member (BR-4.5).

**BR-19.10** — Admin **cannot delete** a member. There is no delete option anywhere (BR-8.1).

**BR-19.11** — **Restricting, temporarily blocking and permanently blocking** are done from the safety module (M12), not from this screen, so that each has a safety reason on record.

**BR-19.12** — Admin screens show a member's mobile number and email, but exports and reports mask them unless the Admin confirms "include contact details" (Section 24, report access rules).

**BR-19.13** — Admin accounts are not listed here and cannot be changed in the application. They are managed in the database (D-19).

## Status Lifecycle

Member statuses are those of Section 9.1. Who moves them in this module:

| From | To | Who | Condition |
|---|---|---|---|
| Unverified | Active | Admin | Mark email verified, with reason |
| Active / Restricted | Inactive | Admin | Deactivate, with reason |
| Inactive | Active | Admin | Reactivate, with reason |

Restricted and Blocked statuses are moved in M12.

## Scenarios

**SC-19.1 — Find a member**
1. Admin types an email fragment in SCR-19.1.
2. The list narrows to that member; Admin opens SCR-19.2 and reviews status, route summary and safety history.

**SC-19.2 — Deactivate a member who left the company**
1. Admin deactivates the member with reason "Requested account closure by email".
2. Route Inactive; pending requests withdrawn; connections Disconnected; chats Closed. The member's history is intact.

**SC-19.3 — Reactivate**
1. The member returns and asks for reactivation.
2. Admin reactivates with a reason. The member can log in; the route is Inactive until the member reactivates it.

**SC-19.4 — Verification email does not arrive**
1. A member writes to Admin that the link never came.
2. Admin first sends a fresh link; if it still fails, Admin marks the email verified with reason "Email delivery failed; identity confirmed by phone call".

**SC-19.5 — Locked account**
1. A member is locked after five wrong passwords and asks for help.
2. Admin unlocks it immediately; the member can try again.

**SC-19.6 — Wrong gender chosen**
1. Member asks for a correction. Admin changes it with reason; audit shows old and new value.

**SC-19.7 — Remove an inappropriate photo**
1. A photo breaks guidelines. Admin removes it with reason; the member is notified.

**SC-19.8 — Contact details in an export**
1. Admin exports the member list. The email and mobile columns are masked.
2. Admin ticks "include contact details" and exports again; the confirmation is logged.

**SC-19.9 — Admin tries to read a chat**
1. Admin opens a member's detail looking for conversations.
2. Only counts appear. Content is available only through an open safety report (M12).

## Acceptance Criteria

**AC-19.1 — Reason mandatory**
- Given Admin chooses Deactivate
- When the reason is empty or shorter than 10 characters
- Then the action is not saved.

**AC-19.2 — Deactivation effects**
- Given a member has an Active route, a Pending request and an Accepted connection
- When Admin deactivates the member
- Then the route is Inactive, the request is Withdrawn, the connection is Disconnected and the chat is Closed.

**AC-19.3 — Reactivation does not revive the route**
- Given an Inactive member is reactivated
- When they log in
- Then they have no Active route until they reactivate one.

**AC-19.4 — No member edit**
- Given Admin opens a member's detail
- When Admin looks for a way to change the member's name, route or messages
- Then no such option exists.

**AC-19.5 — Mobile visible to Admin only**
- Given a member has entered a mobile number
- When Admin opens the member detail
- Then the number is visible; for any other member it is not (BR-4.9).

## Data Impact

| Action | Records created or changed |
|---|---|
| Deactivate | Member Inactive; route Inactive; requests Withdrawn; connections Disconnected; chats Closed; leads Withdrawn; audit entry; notification |
| Reactivate | Member Active; audit entry; email |
| Mark verified / Unlock / Correct gender / Remove photo | The field concerned; audit entry |

## Reports Produced

RPT-1 User & Access Report · RPT-2 Registration & Verification Funnel · RPT-25 Login Audit — in M15.

## Cross-References

- **Config:** CFG-9, CFG-10, CFG-11, CFG-24.
- **Statuses:** Member (9.1).
- **Validations:** V-45, V-48, V-50.
- **Decisions/assumptions:** D-19, D-21, A-22, A-49, A-54.
- **Related modules:** M12 (restriction and blocking), M11 (route viewing).
