# Module M11 — Route & Lead Monitoring

> Source: `RAAH_Functional_Specification.md` Section 20. Build order #11.

## Purpose

Let Admin see how well matching is working — which routes find people, which do not, how many leads turn into requests and connections — and spot unusual or unsafe routes, **without ever matching or assigning anyone** (BR-4.6).

## Who Uses It

Admin only. Read-only, except that Admin may deactivate a route or hold it Under Review for a safety reason (BR-4.5, M12).

## Dependencies

- M2 (routes and versions), M3 (leads and match types), M4 (leads), M5 (requests and connections).
- Foundation config: CFG-1, CFG-4, CFG-22 (the same matching rules used for grouping).
- Foundation statuses: Route (9.2), Lead (9.5), Connection (9.6).

## Screens

| Screen | Purpose |
|---|---|
| SCR-20.1 Route Monitor | List of all routes with filters |
| SCR-20.2 Route Detail | One route in full: both legs, versions, leads count, connections |
| SCR-20.3 Lead Monitor | Leads by member or route, match types, conversion |
| SCR-20.4 Connection Monitor | Requests and connections by status, with a **Reported** flag where an open safety report involves the pair; funnel; no chat content |
| SCR-20.5 Matching Health | Where matching works and where it does not |

## Route Monitor (SCR-20.1)

| Filter | Options |
|---|---|
| Status | Draft / Active / Inactive / Invalid / Under Review |
| Type | Saathi / Raahi |
| City | Any city of the City Master (matches Start City or End City) |
| Has Return leg | Yes / No |
| Has open report | Yes / No (the route's member is the subject of an open safety report) |
| Leads | None / 1–5 / More than 5 |
| Point text | Contains a stop name (matched with the same fuzzy rule as matching) |
| Last changed | Date range |

Columns: Route number, member, type, status, Going summary (Start → End), Return summary, days, times, leads count, requests, connections, active since, last edited.

## Route Detail (SCR-20.2)

| Block | Content |
|---|---|
| Header | Route number, member, Start City → End City, type, vehicle (Raahi), status, version |
| Legs | Going and Return, point by point as typed, days, times |
| Versions | Each saved version with date and what changed |
| Leads | Count by match type (Full Route, Useful Segment) and by leg |
| Connections | Count by status |
| Actions | **Deactivate** (with reason) · **Hold Under Review** (with reason; M12) |

## Matching Health (SCR-20.5)

| Block | Meaning |
|---|---|
| Active routes with **no leads** | Routes active for more than N days with zero leads (N set on the screen, default 7), by Saathi/Raahi |
| Leads per Active route | Average and distribution |
| Match type mix | Share of Full Route versus Useful Segment, per leg |
| **Most used stop names** | The stop texts members type most often, grouped by the same fuzzy rule, with variant spellings listed |
| **Unmatched stop names** | Stop names that appear on Active routes but never produce a match, to spot spelling problems or thin coverage |
| Lead → request rate | Share of leads that led to a request |
| Request → accept rate | Share of requests Accepted, Rejected, Expired, Withdrawn |
| Average time to answer a request | From sent to Accepted or Rejected |
| Supply and demand | Count of Active Saathi routes versus Active Raahi routes |
| **By city** | Active routes, leads, lead → request rate and acceptance rate for each city |

## Business Rules

**BR-20.1** — Monitoring is **read-only**. Admin cannot create, edit or delete a route, lead, request or connection (BR-4.5, BR-4.6).

**BR-20.2** — Admin can see the **full route** of any member, including all points as typed. It is an Admin-only view and is never exported without the masking rule of BR-19.12.

**BR-20.3** — Admin can **deactivate a route** or **hold it Under Review** only with a reason, and only for a safety or data-integrity cause (BR-4.5). Deactivation has the effects of BR-11.14; Under Review removes the route from matching until released (A-32).

**BR-20.4** — The monitors **never show chat text**. Connection Monitor shows counts and statuses only (BR-4.7).

**BR-20.5** — "Most used stop names" and "Unmatched stop names" are produced with the **same text-similarity rule as matching** (D-22, A-21), so Admin sees what the engine sees.

**BR-20.6** — Figures are live from the records. No number is typed or adjusted by Admin.

**BR-20.7** — A route flagged **Invalid** by the system (a rule broken, for example after a CFG change) appears at the top of the Route Monitor with the broken rule named, so Admin can contact the member if needed. Admin does not fix it.

## Scenarios

**SC-20.1 — Routes with no leads**
1. Admin opens Matching Health and sees 14 Active routes with no leads for over 7 days.
2. Admin opens a few. Most end at a stop no other member uses. No action is needed; the list shows where supply is thin.

**SC-20.2 — Spelling variants of one stop**
1. "Most used stop names" shows *Badarpur Metro* with variants *Bdarpur Metro* and *badarpoor metro*.
2. Admin sees that the 70% rule is grouping them and keeps CFG-22 as it is.

**SC-20.3 — Unmatched stop**
1. "Unmatched stop names" shows *Kalka Ji Temple Gate* on three routes with no matches.
2. Admin considers whether CFG-22 should change, tests it in the Test Match tool (M13), and decides.

**SC-20.4 — Funnel check**
1. Connection Monitor shows 120 requests sent this month: 55 Accepted, 20 Rejected, 15 Expired, 5 Withdrawn, 25 Pending.
2. The acceptance rate is shown as 55 of 95 answered requests.

**SC-20.5 — Unsafe route**
1. A report says a route lists a school gate as the Start with an odd pattern.
2. Admin opens the route, holds it Under Review with reason, and handles the report in M12.

**SC-20.6 — Admin looks for manual matching**
1. Admin searches for a way to connect two members directly.
2. No such function exists (BR-4.6).

**SC-20.7 — Route made Invalid by a setting change**
1. CFG-2 is lowered from 5 to 4. A route with 5 points stays valid, as BR-6.2 protects existing routes. A route edited afterwards with 5 points is rejected at save.

## Acceptance Criteria

**AC-20.1 — Read-only**
- Given Admin opens any route or lead
- When Admin looks for edit or delete
- Then none exists.

**AC-20.2 — Reason for deactivation**
- Given Admin deactivates a route
- When no reason is entered
- Then the action is rejected.

**AC-20.3 — Under Review excludes matching**
- Given Admin holds a route Under Review
- When matching runs
- Then the route produces no leads.

**AC-20.4 — No chat content**
- Given Admin opens Connection Monitor
- When Admin looks at a connection
- Then only status, dates and counts are shown, never messages.

**AC-20.5 — Same rule as matching**
- Given two stop names are 75% alike
- When "Most used stop names" groups names
- Then they appear as one stop with two variants.

## Data Impact

| Action | Records created or changed |
|---|---|
| Deactivate route | Route Inactive; leads and pending requests Withdrawn; audit entry; notification |
| Hold Under Review | Route Under Review; leads withdrawn; audit entry |
| Viewing monitors | None |

## Reports Produced

RPT-3 Route Register · RPT-4 Route Coverage · RPT-5 Supply & Demand · RPT-6 Matching Summary · RPT-7 Routes Without Leads · RPT-8 Lead Register · RPT-9 Lead Conversion · RPT-10 Connection Register · RPT-11 Connection Funnel · RPT-12 Request Response Time — in M15.

## Cross-References

- **Config:** CFG-1, CFG-2, CFG-4, CFG-22.
- **Statuses:** Route (9.2), Lead (9.5), Connection (9.6).
- **Validations:** V-45, V-48.
- **Decisions/assumptions:** A-21, A-32, D-22.
- **Related modules:** M2, M3, M4, M5, M12.
