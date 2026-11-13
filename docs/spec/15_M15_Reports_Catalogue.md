# Module M15 — Reports Catalogue

> Source: `RAAH_Functional_Specification.md` Section 24. Build order #15 (final).

## Purpose

Give Admin a single catalogue of read-only reports over every part of the platform — members, routes, matching, leads, connections, chat activity, ratings, notifications, safety and system audit — with filters, drill-down and safe exports.

## Who Uses It

Admin only. Members have no reports module in Phase 1; they use their own screens (leads, requests, connections, ratings, reports).

## Dependencies

- All modules above (reports read their records).
- Foundation: audit (Section 8), masking rule BR-19.12, role permissions (Section 4).
- Fuzzy grouping in RPT-4 uses the same rule as matching (D-22, A-21).

## Screens

| Screen | Purpose |
|---|---|
| SCR-24.1 Reports Home | Catalogue grouped by Member / Route / Matching & Lead / Connection / Chat, Rating & Notification / Safety / System & Audit |
| SCR-24.2 Report View | Filters, columns, export, print and drill-down for the selected report |

## Common Report Behaviour

| Capability | Detail |
|---|---|
| Who can run reports | **Admin only.** Members do not have a reports module in Phase 1; they use their own screens (leads, requests, connections, ratings, reports) |
| Filters | Date range, status, journey role, member, route type, leg, reason, whichever apply |
| Export | Excel and PDF |
| Print | Print-friendly layout |
| Drill-down | Clicking any figure opens the records behind it |
| Saved filters | A filter combination can be saved and re-run |
| Columns | Show/hide, with the choice remembered |
| Contact details | Email and mobile columns are **masked** in exports unless Admin confirms "include contact details"; the confirmation is logged (BR-19.12) |
| Chat content | **Never** included in any report. Only counts are reported (BR-4.7) |
| Scheduling / emailing | Out of Phase 1 scope |

## Report Catalogue

### Member Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-1 | User & Access Report | Who is on the platform and in what state | Member no., name, role, vehicle, status, verification, registered on, last login, locked flag |
| RPT-2 | Registration & Verification Funnel | How many registered members become useful | Registered, verified, with a route, with a lead, with a request, with a connection; per period |

### Route Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-3 | Route Register | Every route and its state | Route no., member, Start City, End City, type, status, Going Start → End, Return Start → End, days, times, versions, activated on |
| RPT-4 | Route Coverage | Where members travel and how people spell it, per city | City, stop name groups (same fuzzy rule as matching), variants, number of routes, number matched, **unmatched stop names** |
| RPT-5 | Supply & Demand | Balance of Saathi and Raahi routes | Active Saathi routes, Active Raahi routes, with Return leg, by city, by time band and by common Start/End stop |

### Matching & Lead Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-6 | Matching Summary | How well matching performs | Leads created, Full Route vs Useful Segment per leg, average leads per Active route, both-legs leads |
| RPT-7 | Routes Without Leads | Routes that find no one | Route no., member, type, active since, days with no leads, Start/End |
| RPT-8 | Lead Register | Every lead and its fate | Lead no., for member, matched member, legs matched, match type per leg, status, created / last calculated |
| RPT-9 | Lead Conversion | From lead to request to connection | Leads, viewed, dismissed, requested, accepted; rates; by role and by match type |

### Connection Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-10 | Connection Register | Every request and connection | Connection no., sender, receiver, sent on, status, responded on, ended on and by |
| RPT-11 | Connection Funnel | Outcome of requests | Sent, Accepted, Rejected, Expired, Withdrawn (by sender / by system), Disconnected, Blocked; acceptance rate |
| RPT-12 | Request Response Time | How fast requests are answered | Connection no., sent, answered, hours to answer; average and median by role |

### Chat, Rating & Notification Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-13 | Chat Activity Summary | Whether connections turn into conversation (counts only) | Conversations opened, with at least one reply, messages per conversation, closed chats |
| RPT-14 | Rating Summary | Ratings per member | Member, ratings count, average, distribution 1–5 |
| RPT-15 | Low-Rated Members | Members who may need attention | Member, ratings count, average below a chosen level, reports against |
| RPT-16 | Rating Moderation Log | Hidden comments | Rating no., date, rater, rated member, reason, Admin |
| RPT-23 | Notification Delivery Summary | Whether notices and emails go out | Event type, created, sent, failed, opened (in-app) |

### Safety Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-17 | Safety Report Register | Every report | SAF no., date, reporter, reported member, reason, status, action, closed on |
| RPT-18 | Overdue Reports | Reports not decided in time | SAF no., age in days, status, reason |
| RPT-19 | Action Summary | What actions are being taken | Warning, Restriction, Temporary Block, Permanent Block, Dismissed; counts by period and reason |
| RPT-20 | Block & Restriction Register | Everyone currently restricted or blocked | Member, status, since, end date, reason, by Admin |
| RPT-21 | Flagged Members | Members at or above the report threshold | Member, number of different reporters, reasons, current status |
| RPT-22 | Member Block Summary | How often members block each other | Blocks by period; members blocked most often (counts only) |

### System & Audit Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-24 | Audit Trail Report | Who changed what | Date, user, role, record, action, old value, new value, reason |
| RPT-25 | Login Audit | Access history | Member, login time, device, IP, success/failure |
| RPT-26 | Configuration Change Report | Changes to settings | Setting, old value, new value, changed by, date, remark |
| RPT-27 | Chat Access Log | Every time Admin opened a reported chat | Date, Admin, report no., connection no. |
| RPT-28 | Recalculation Log | Runs of "Recalculate all leads" | Date, Admin, leads created, withdrawn, routes processed |
| RPT-29 | Legal Acceptance Report | Who accepted which version of the Terms, Privacy Policy and Safety Guidelines | Member, document, version, accepted on; members yet to accept the current version |
| RPT-30 | City Register | Cities available and how much they are used | City, state, status, Active routes, members, created on |

## Report Access by Role

| Report group | Admin | Member |
|---|---|---|
| Member, Route, Matching & Lead, Connection | Full | — |
| Chat, Rating, Notification | Full (no chat content) | — |
| Safety | Full | — (members see only their own reports on SCR-17.4) |
| System & Audit | Full | — |

## Business Rules

**BR-24.1** — Reports are **read-only** and always computed from current records. Nothing in a report can be edited.

**BR-24.2** — No report ever shows chat text (BR-4.7). Reports of chat activity show counts only.

**BR-24.3** — Exports mask email and mobile unless Admin ticks "include contact details" (BR-19.12); the confirmation is written to the audit trail.

**BR-24.4** — The fuzzy grouping used in RPT-4 is the **same** text-similarity rule as matching (D-22, A-21), so Admin sees what the engine sees.

**BR-24.5** — Any report that counts members counts each member once, even if they have several routes in the period.

**BR-24.6** — Report dates follow DD-MMM-YYYY and the platform time zone (Section 27).

**BR-24.7** — A member cannot view or export another member's data through any report; Members have no report access (Section 24.9).

## Status Lifecycle

No records of its own. Each report reflects the current status of the records it reads (Member 9.1, Route 9.2, Lead 9.5, Connection 9.6, Safety report 9.7, Rating 9.8).

## Scenarios

**SC-24.1 — Weekly health check**
1. Admin runs RPT-9 Lead Conversion for the last 7 days: 400 leads, 90 requests, 48 accepted.

**SC-24.2 — Find spelling problems**
1. Admin opens RPT-4 and sees "Kalkaji" with variants "Kalka Ji" and "kalkajee", and "Sai Mandir Gate" listed as unmatched on four routes.

**SC-24.3 — Export with contact details**
1. Admin exports RPT-1. The email and mobile columns are masked. Admin ticks "include contact details" and re-exports; the audit trail records it.

**SC-24.4 — Which chats did Admin open**
1. Admin runs RPT-27 and sees every access to a reported chat, by date and report number.

**SC-24.5 — Members who need attention**
1. Admin runs RPT-15 with "average below 3.0" and cross-checks RPT-21 Flagged Members.

## Acceptance Criteria

**AC-24.1 — Admin only**
- Given a Member is logged in
- When they try to open a report address
- Then access is denied and logged.

**AC-24.2 — No chat content**
- Given a report about chat activity
- When it is generated
- Then it contains counts only and no message text.

**AC-24.3 — Masked export**
- Given Admin exports RPT-1 without ticking "include contact details"
- When the file is opened
- Then email and mobile columns are masked.

**AC-24.4 — Drill-down**
- Given a figure in RPT-11
- When Admin clicks it
- Then the underlying connections are listed.

## Data Impact

None, except that an export with contact details writes an audit entry.

## Reports Produced

This module **is** the reports catalogue (RPT-1 … RPT-30 above).

## Cross-References

- **Config:** CFG-15, CFG-32 (thresholds that shape report filters/flags).
- **Statuses:** Member (9.1), Route (9.2), Lead (9.5), Connection (9.6), Safety report (9.7), Rating (9.8).
- **Validations:** V-48, V-49.
- **Decisions/assumptions:** A-21, D-22.
- **Related modules:** M1–M14 (data sources), M14 (drill-downs).
