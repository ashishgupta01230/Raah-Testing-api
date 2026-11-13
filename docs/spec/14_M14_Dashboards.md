# Module M14 — Dashboards

> Source: `RAAH_Functional_Specification.md` Section 23. Build order #14.

## Purpose

Give each role one landing screen built around the decisions that role has to make today. Both dashboards are **live**: every number comes from the records, none is typed.

## Who Uses It

Every Member (Member Dashboard) and Admin (Admin Dashboard). The dashboard is the first screen after login (BR-5.11).

## Dependencies

- All modules above (the dashboard reads their records).
- Foundation: role-based landing (BR-5.11), permission matrix (Section 4).
- Drill-downs open the lists/screens of M4, M5, M10, M11, M12 and M15.
- Admin alerts reference CFG-15, CFG-32, and the sample legal text flag (M13).

## Screens

| Screen | Purpose |
|---|---|
| SCR-23.1 Member Dashboard | The member's own work queue and status |
| SCR-23.2 Admin Dashboard | Platform health, matching health, safety queue and alerts |

## Member Dashboard (SCR-23.1)

| Block | Content | Why it is there |
|---|---|---|
| **Setup prompt** (shown only when needed) | "Verify your email" if Unverified · "Create your route" if no route · "Add your Return leg" if Return is missing and a Return leg would add leads · "Add a photo" if none | Gets a new member to a working route quickly |
| **My Route** | Journey role (Saathi / Raahi), status, Going summary (Start → End, days, time), Return summary, vehicle (Raahi), buttons Edit / Deactivate / Reactivate | The member's single active commitment on the platform |
| **New Leads** | Count of new leads (split Going / Return), top three leads with a Connect button, link "See all" | The reason the member came |
| **Requests Received** | Pending requests with sender, time left before expiry, Accept / Reject | Answers owed to others |
| **Requests Sent** | Pending requests with how long each has waited, Withdraw | What the member is waiting for |
| **My Connections** | Number of connections, conversations with unread messages | Where the journey is actually decided |
| **Notifications** | Latest five | Quick catch-up |
| **Safety** | Links to Blocked Members and My Reports | Safety is always one tap away |

Wording is role-aware: a Saathi sees "Raahis for you", a Raahi sees "Saathis for you".

## Admin Dashboard (SCR-23.2)

A period selector (Today / 7 days / 30 days) applies to activity counts.

| Block | Content | Drill-down |
|---|---|---|
| **Members** | Total · Active · Unverified · new in period · Inactive · Restricted · Temporarily / Permanently Blocked | Member List filtered |
| **Routes** | Active Saathi routes · Active Raahi routes · with Return leg · Invalid · Under Review · Active routes by city (top five) | Route Monitor filtered |
| **Matching** | Leads in period · Full Route vs Useful Segment · Active routes with no leads · lead → request rate | Matching Health |
| **Connections** | Pending · Accepted · Rejected · Expired · Withdrawn · Disconnected · acceptance rate | Connection Monitor |
| **Safety** | Open reports · **overdue** (past CFG-32 days) · flagged members (CFG-15) · Restricted · Blocked | Report Queue, Flagged Members |
| **Moderation** | Rating comments awaiting review | Rating Moderation |
| **Alerts** | Locked accounts · Invalid routes · members at the report threshold · failed-login spikes · "Legal text is still the sample" | Relevant list |
| **Quick links** | Matching Configuration · Notifications settings · Platform Settings and Legal Content (M13) | — |

## Business Rules

**BR-23.1** — A dashboard shows only what the logged-in role may see. A Member Dashboard never shows another member's data or platform totals (BR-4.1).

**BR-23.2** — Every figure is a **live count** from the records. Nobody can type or adjust a number.

**BR-23.3** — Every Admin block **drills down** to the list behind the number.

**BR-23.4** — Dashboards never show chat content, mobile numbers or emails of other members. Admin sees contact details only on the Member Detail screen (BR-19.12).

**BR-23.5** — Setup prompts on the Member Dashboard disappear as soon as the condition is met, and never block use of the rest of the dashboard.

**BR-23.6** — The dashboard remains usable when a block has no data: each block shows a clear empty state, not an error.

## Status Lifecycle

No records of its own. The dashboard reflects the statuses of Member (9.1), Route (9.2), Lead (9.5), Connection (9.6), Safety report (9.7) and Rating (9.8) as they are at the moment of viewing.

## Scenarios

**SC-23.1 — New member's first dashboard**
1. A member verifies email and logs in. The Setup prompt says "Create your route". My Route, New Leads and Requests blocks show empty states.

**SC-23.2 — Saathi with leads and a request to answer**
1. Ashish sees "5 new leads (3 Going, 2 Return)", the top three leads with Connect buttons, and one request from a Raahi expiring in 4 days.

**SC-23.3 — Raahi after deactivating**
1. Amit deactivates his route. My Route shows Inactive with a Reactivate button; New Leads shows an empty state; his connections and unread messages still show.

**SC-23.4 — Admin morning check**
1. Admin opens the dashboard. Safety shows 2 overdue reports, 1 flagged member and 3 rating comments awaiting review. Admin drills into the overdue reports first.

**SC-23.5 — Matching health at a glance**
1. Admin sees 30 Active routes with no leads and drills into Matching Health to see whether the cause is thin coverage or spelling (Module M11).

**SC-23.6 — Unverified member**
1. A member who has not verified sees only the "Verify your email" prompt with a Resend button, and no route functions (BR-5.1).

## Acceptance Criteria

**AC-23.1 — Role separation**
- Given a Member is logged in
- When the dashboard opens
- Then only that member's own data is shown, with no platform totals.

**AC-23.2 — Live counts and drill-down**
- Given a new report is submitted
- When Admin refreshes the dashboard
- Then the Open reports count has increased and clicking it opens the report queue.

**AC-23.3 — Overdue reports**
- Given CFG-32 is 3 and a report is 4 days old and not decided
- When Admin opens the dashboard
- Then the report is counted as overdue.

**AC-23.4 — Setup prompt**
- Given a member has no route
- When they open the dashboard
- Then the prompt "Create your route" is shown and disappears once a route exists.

## Data Impact

None. Dashboards only read.

## Reports Produced

None of its own; drill-downs open the screens and reports of M11, M12 and M15.

## Cross-References

- **Config:** CFG-15, CFG-32.
- **Statuses:** Member (9.1), Route (9.2), Lead (9.5), Connection (9.6), Safety report (9.7), Rating (9.8).
- **Decisions/assumptions:** D-19, A-53.
- **Related modules:** M1–M13 (data sources), M15 (drill-down reports).
