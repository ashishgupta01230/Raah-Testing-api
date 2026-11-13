# Module M12 — Safety, Reports & Blocked Users

> Source: `RAAH_Functional_Specification.md` Section 21. Build order #12.

## Purpose

Handle safety reports from members, apply proportionate action, manage restricted and blocked members, and moderate rating comments — in a way that is fair, recorded and reversible where it should be.

## Who Uses It

Admin only.

## Dependencies

- M8 (reports and blocks from members), M10 (member status and account actions), M11 (route holds).
- Foundation config: CFG-15 (alert threshold), CFG-32 (overdue days).
- Foundation statuses: Safety report (9.7), Member (9.1), Rating (9.8).

## Screens

| Screen | Purpose |
|---|---|
| SCR-21.1 Report Queue | All reports with age, reason, status, flags |
| SCR-21.2 Report Detail | The report, the attached conversation, the reported member's history |
| SCR-21.3 Take Action | Warning, Restriction, Temporary Block, Permanent Block, Dismiss |
| SCR-21.4 Restricted & Blocked Members | Everyone currently restricted or blocked, with end dates |
| SCR-21.5 Flagged Members | Members with CFG-15 or more reports from different members |
| SCR-21.6 Rating Moderation | Rating comments reported or to be reviewed |

## Report Detail (SCR-21.2)

| Block | Content |
|---|---|
| Report | Number (SAF-), date, reason, description, status, age in days |
| Reporter | Name, member number, their report history (counts of raised, dismissed, action taken) |
| Reported member | Name, member number, status, route, ratings, previous warnings, restrictions and blocks, reports from other members |
| Conversation | The chat attached to the report, read-only. **Opening it is logged** (BR-4.7, BR-15.11) |
| Related | The connection between the two members and any other open reports |
| Actions | Under Review · Hold route Under Review · Take Action · Dismiss |

## Field Specification — Take Action (SCR-21.3)

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Action | Select | Yes | Warning / Restriction / Temporary Block / Permanent Block / Dismiss report |
| End date | Date | For Restriction (optional) and Temporary Block (mandatory) | Must be in the future |
| Reason | Text | Yes | At least 10 characters (A-54). Visible to Admin only |
| Message to member | Text | No | Optional note included in the notice to the member. It never names the reporter |

## What Each Action Does

| Action | Effect on the member | Notice to member | Notice to reporter |
|---|---|---|---|
| **Warning** | None on function. Recorded on the member's history | In-app + email | "Your report has been reviewed" |
| **Restriction** | Status **Restricted**: cannot send connection requests until the end date (or until lifted). Can log in, chat with existing connections and accept requests (A-50) | In-app + email | As above |
| **Temporary Block** | Status **Temporarily Blocked** until the end date. Cannot log in. Route deactivated, requests withdrawn, connections Disconnected and chats Closed. Lifts itself on the end date; the member returns Active with no Active route | In-app + email | As above |
| **Permanent Block** | Status **Permanently Blocked**. Cannot log in. Same closures as a Temporary Block. Cannot be reversed in the application (A-50) | Email | As above |
| **Dismiss** | None | None | "Your report has been reviewed" |

The reporter is told only that the report was reviewed, never which action was taken (BR-17.8).

## Business Rules

**BR-21.1** — Admin reviews every report. A report is **never deleted** (BR-17.10). The queue shows oldest first, with those past CFG-32 days (default 3) highlighted (A-53).

**BR-21.2** — **Taking a report to Under Review** is optional but recommended when starting an investigation. It records who started and when.

**BR-21.3** — **Reading the conversation** is allowed only from an open report, is read-only, and **every opening is logged** with Admin and time (BR-4.7).

**BR-21.4** — **Actions are proportionate.** The screen shows the member's previous warnings, restrictions and blocks so that Admin can choose a fitting step. The system does not force a sequence.

**BR-21.5** — Every action needs a **reason** (A-54) and is audited. Warning, Restriction and blocks notify the member (Section 18.4).

**BR-21.6** — **Restriction** removes only the ability to send connection requests (A-50). All other functions continue. With an end date it lifts itself; without one it stays until Admin lifts it. Admin can lift a Restriction early, with reason.

**BR-21.7** — **Temporary Block** needs a future end date. On that date the status returns to Active automatically and the member is told by email. Admin can lift it early, with reason. Closures made during the block (route, connections) are **not** restored (BR-19.3).

**BR-21.8** — **Permanent Block** closes the account in the same way, with no end date. It **cannot be reversed in the application** (A-50). The email address stays registered and cannot be used again (BR-5.3).

**BR-21.9** — **Blocking effects** (Temporary and Permanent) are the same as deactivation (BR-19.2), with the member's status being Blocked, not Inactive.

**BR-21.10** — The **reporter is never named** to the reported member (BR-17.7). Admin screens show the reporter to Admin only.

**BR-21.11** — **Flagged members**: when CFG-15 different members have reported the same member (default 3), the member appears on SCR-21.5 and Admin is alerted (BR-17.9).

**BR-21.12** — **Dismiss** needs a reason. A dismissed report keeps its record. The number of dismissed reports per reporter is shown on SCR-21.2, so that repeated unfounded reports can be seen; any step against such a reporter is an Admin decision using the same actions (BR-17.11).

**BR-21.13** — **Rating moderation**: Admin can **Hide** a rating comment with a reason. The stars remain counted (BR-16.8, BR-16.11). The rater is told.

**BR-21.14** — **Photo removal** and **gender correction** are done in M10 and may be triggered from a report.

**BR-21.15** — A report about a member who is **already Inactive or Blocked** can still be reviewed and closed. No further functional action is needed.

**BR-21.16** — Admin cannot create a report on a member's behalf. Reports come only from members (A-46). Admin acts on their own observation through Account Actions (M10) or Route holds (M11).

## Status Lifecycle

Report statuses are those of Section 9.7.

| From | To | Who | Condition |
|---|---|---|---|
| Submitted | Under Review | Admin | Investigation started |
| Under Review | Action Taken | Admin | Warning, Restriction, Temporary Block or Permanent Block applied |
| Under Review | Dismissed | Admin | With reason |
| Action Taken / Dismissed | Closed | Admin | Final |

Member status changes made here:

| From | To | Who | Condition |
|---|---|---|---|
| Active | Restricted | Admin | Restriction |
| Restricted | Active | Admin / System | Lifted, or end date reached |
| Active / Restricted | Temporarily Blocked | Admin | Temporary Block with end date |
| Temporarily Blocked | Active | System / Admin | End date reached, or lifted early |
| Active / Restricted / Temporarily Blocked | Permanently Blocked | Admin | Permanent Block |

## Scenarios

**SC-21.1 — Warning**
1. Ashish reports Rohit for "Inappropriate messages". Admin reads the attached chat and sees a mildly offensive remark.
2. Admin issues a Warning with a note. Rohit is told he received a warning; Ashish is told the report was reviewed. Rohit's status and connections do not change.

**SC-21.2 — Restriction**
1. A member repeatedly sends requests and is reported as spam by several members.
2. Admin applies a Restriction for 30 days. The member cannot send requests but can still chat and respond to requests. It lifts on its end date.

**SC-21.3 — Temporary Block**
1. Admin finds clear harassment. A Temporary Block for 14 days is applied.
2. Rohit cannot log in; his route is Inactive; connections are Disconnected. After 14 days he can log in, with no Active route.

**SC-21.4 — Permanent Block**
1. Admin confirms a fake profile used to harass others and applies a Permanent Block.
2. The account is closed for good; the email cannot register again.

**SC-21.5 — Dismiss**
1. Admin reads the chat and finds nothing wrong. Dismisses with reason "No violation found".
2. The reporter is told the report was reviewed. The reported member is not told anything.

**SC-21.6 — Three reporters**
1. Three different members report Rohit.
2. Admin sees an alert and Rohit on SCR-21.5; opens the three reports together.

**SC-21.7 — Hold the route during an investigation**
1. While investigating, Admin holds the reported member's route Under Review so that no new leads form.
2. After the decision, Admin releases or deactivates it.

**SC-21.8 — Lift a restriction early**
1. A member apologises; Admin lifts the Restriction with reason. The member can send requests again.

**SC-21.9 — Overdue report**
1. A report is 4 days old (CFG-32 = 3). It is highlighted on the Admin dashboard.

**SC-21.10 — Abusive rating comment**
1. A member reports a comment on SCR-16.2. Admin hides it with reason in Rating Moderation. Stars still count.

**SC-21.11 — Reporter with many dismissed reports**
1. SCR-21.2 shows that the reporter has five dismissed reports. Admin takes this into account when deciding.

## Acceptance Criteria

**AC-21.1 — Conversation access is logged**
- Given Admin opens a report with an attached chat
- When Admin reads the conversation
- Then the access is written to the audit log.

**AC-21.2 — Temporary Block lifts itself**
- Given a Temporary Block with an end date
- When the end date arrives
- Then the member's status becomes Active and they are notified.

**AC-21.3 — Restriction scope**
- Given a member is Restricted
- When they try to send a connection request
- Then it is refused, and all other functions still work.

**AC-21.4 — Block effects**
- Given a member has an Active route, a Pending request and an Accepted connection
- When Admin applies a Temporary Block
- Then the route is Inactive, the request Withdrawn, the connection Disconnected and the chat Closed.

**AC-21.5 — Reporter hidden**
- Given Admin has issued a Warning
- When the reported member reads the notice
- Then it does not name the reporter.

**AC-21.6 — Permanent Block is final**
- Given a member is Permanently Blocked
- When Admin looks for a way to reverse it
- Then no such option exists in the application.

**AC-21.7 — Reason required**
- Given Admin chooses any action
- When the reason is shorter than 10 characters
- Then the action is not saved.

## Data Impact

| Action | Records created or changed |
|---|---|
| Under Review | Report status; audit entry |
| Warning | Warning recorded on member; report Action Taken; notices |
| Restriction | Member Restricted; end date; notices |
| Temporary / Permanent Block | Member Blocked; route Inactive; requests Withdrawn; connections Disconnected; chats Closed; leads Withdrawn; notices |
| Dismiss | Report Dismissed; reason recorded |
| Hide comment | Rating status Hidden; rater notified |

## Reports Produced

RPT-17 Safety Report Register · RPT-18 Overdue Reports · RPT-19 Action Summary · RPT-20 Block & Restriction Register · RPT-21 Flagged Members · RPT-22 Member Block Summary · RPT-16 Rating Moderation Log — in M15.

## Cross-References

- **Config:** CFG-15, CFG-32.
- **Statuses:** Safety report (9.7), Member (9.1), Rating (9.8).
- **Validations:** V-45, V-46, V-48.
- **Decisions/assumptions:** A-46, A-50, A-53, A-54.
- **Related modules:** M8, M10, M11.
