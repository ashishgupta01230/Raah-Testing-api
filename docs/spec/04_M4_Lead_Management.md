# Module M4 — Lead Management

> Source: `RAAH_Functional_Specification.md` Section 13. Build order #4.

## Purpose

Show each member the relevant people found for them, explain why each person was matched, let them filter and dismiss, and start a connection request from a lead. A lead is **a suggestion, not a booking and not a connection** (Principle 7).

## Who Uses It

Saathi (sees Raahi leads) and Raahi (sees Saathi leads). Admin views all leads for monitoring (M11).

## Dependencies

- M3 (leads with match data), M1 (limited profile shown per member).
- Foundation config: CFG-7 (page size), CFG-12 (gender filter), CFG-20 (leg filter).
- Foundation statuses: Lead (9.5).

## Screens

| Screen | Purpose |
|---|---|
| SCR-13.1 My Leads | Ranked list of leads with filters |
| SCR-13.2 Lead Detail | Limited profile, route comparison, "why this match", Connect button |
| SCR-13.3 Dismissed Leads | Leads the member dismissed, with Undo |

## What a Lead Row Shows (SCR-13.1)

| Item | Content |
|---|---|
| Person | Name, photo, gender, Bike/Car for a Raahi, verified badge, rating average and count (Section 10.5) |
| Cities | Start City and End City, e.g. *Delhi → Noida* |
| Going leg | Match type (Full Route / Useful Segment / No match), shared stretch (e.g. *Vinay Nagar → Badarpur Metro*) |
| Return leg | Same as above, or "Not entered" / "No match" |
| Common days | e.g. *Mon–Fri* |
| Departure times | Both times and the gap, e.g. *8:00 AM · 8:05 AM · 5 min apart* |
| Action | **Connect**, or current state: *Request sent*, *Request received*, *Connected* |
| Dismiss | "Not interested" |

Match labels are worded from the viewer's side:

| Viewer | Full Route label | Useful Segment label |
|---|---|---|
| Saathi | "This Raahi covers your whole route" | "This Raahi shares part of your route" |
| Raahi | "Your route covers this Saathi's whole route" | "You share part of this Saathi's route" |

## Lead Detail (SCR-13.2)

| Block | Content |
|---|---|
| Profile | Limited profile of Section 10.5. Email and mobile never appear. |
| Route comparison | Other member's Going and Return legs, point by point as typed, shared points highlighted; days and times beside each leg. |
| Why this match | Plain-language reasons: *Shared points: Vinay Nagar, Badarpur Metro. Common days: Mon–Fri. Time gap: 5 minutes.* (Principle 14) |
| Actions | Connect · Dismiss · Report · Block |

## Filters (D-10, D-16)

| Filter | Options | Effect |
|---|---|---|
| Leg | Any / Going / Return | Going = only leads matching on Going; Return = only on Return; Any = all. Available when CFG-20 is Yes. |
| Gender | Any / Male / Female | Shows only that gender. Available when CFG-12 is Yes. |

Both roles can use both filters. Filters never change lead status, order, or delete a lead.

## Business Rules

**BR-13.1** — Leads are produced only by the Matching Engine (M3). No one can add a lead manually (BR-4.6).

**BR-13.2** — A member sees only **their own leads** (BR-4.1). The other member's info shown is limited to Section 10.5.

**BR-13.3** — List ordered by Section 12.6 ranking. Only the first CFG-7 leads (default 20) shown at once, with "Show more".

**BR-13.4** — **Lead statuses** (Section 9.5): New, Viewed, Dismissed, Requested, Withdrawn by System. A lead becomes **Viewed** when its detail is opened.

**BR-13.5** — **Dismiss**: mark "Not interested". Moves to SCR-13.3, stops appearing in main list. **Undo** returns it. A dismissed lead stays dismissed even when matching recalculates (A-33). No notice to the other member.

**BR-13.6** — **Connect** allowed only if (rules in M5): lead not Withdrawn; member not restricted from sending; fewer than CFG-6 Pending requests; no open connection already exists with this person; cool-off (CFG-17) not running.

**BR-13.7** — After a request is sent, the lead becomes **Requested**. The row shows the connection state (Pending / Connected). If the request ends Rejected/Withdrawn/Expired, the lead returns to **Viewed**. (A lead whose request was accepted stays Requested, labelled **Connected**.)

**BR-13.8** — A lead that no longer applies (route changed, member deactivated, blocked) becomes **Withdrawn by System** and is hidden. An existing connection or pending request with that person is untouched (A-6, A-28).

**BR-13.9** — If a withdrawn lead matches again, the **same lead is revived**: returns as New, unless it was Dismissed (stays Dismissed) (A-33).

**BR-13.10** — Every lead keeps a **History tab** (BR-8.5) showing created, updated, viewed, dismissed, requested and withdrawn events.

**BR-13.11** — The lead list is a **live view**. If the other member edits their route, the lead shows the current route. A request already sent keeps its snapshot (M5).

**BR-13.12** — An empty list shows a helpful message (SC-13.8), not an error.

## Status Lifecycle

| From | To | Who | Trigger |
|---|---|---|---|
| (none) | New | System | Match found |
| New | Viewed | System | Member opens the lead |
| New / Viewed | Dismissed | Member | "Not interested" |
| Dismissed | Viewed | Member | Undo |
| New / Viewed | Requested | System | Member sends a request |
| Requested | Viewed | System | Request Rejected, Withdrawn or Expired |
| Any | Withdrawn by System | System | Match no longer valid |
| Withdrawn by System | New (or Dismissed if it was) | System | Match valid again |

## Scenarios

**SC-13.1 — Saathi opens the lead list**
1. Ashish opens SCR-13.1. Sees Neha, Rohit, Amit in that order (worked example, Section 12.9).
2. Each row shows match type per leg, shared stretch, common days and times.

**SC-13.2 — Filter by Return leg**
1. Ashish selects Leg = Return.
2. Only Neha remains. Rohit and Amit hidden, not deleted. Back to Any shows all three.

**SC-13.3 — Filter by gender**
1. A female Saathi selects Gender = Female.
2. Only female Raahis shown. Order of remaining leads unchanged.

**SC-13.4 — Why this match**
1. Ashish opens Amit's lead.
2. Sees Amit's route with *Vinay Nagar* and *Badarpur Metro* highlighted, and "Shared points: 2 · Common days: Mon–Fri · Time gap: 5 minutes."

**SC-13.5 — Dismiss and undo**
1. Ashish dismisses Amit. Row disappears from main list, appears on SCR-13.3.
2. Amit edits route and still matches; the lead stays dismissed.
3. Ashish taps Undo; lead returns to main list.

**SC-13.6 — Raahi sends a request from a Saathi lead**
1. Amit (Raahi) opens his list, sees Ashish (Saathi).
2. Taps Connect. A request goes to Ashish (D-6). Lead shows "Request sent".

**SC-13.7 — Lead disappears**
1. Rohit deactivates his route.
2. Rohit's lead vanishes from Ashish's list. If already connected, connection and chat remain.

**SC-13.8 — No leads**
1. A member's route has no matches.
2. Screen: "No relevant person found yet. We will tell you when someone matches. You can add a Return leg or check your points and times to find more."

**SC-13.9 — Connect blocked by limit**
1. A member has CFG-6 (10) Pending requests.
2. Connect disabled: "You have too many pending requests. Wait for answers or withdraw one."

**SC-13.10 — Request cool-off**
1. Rohit rejected Ashish's request 2 days ago (CFG-17 = 7).
2. Rohit's lead shows "You can request again in 5 days"; Connect disabled.

## Acceptance Criteria

**AC-13.1 — Mobile number and email hidden**
- Given a lead is displayed
- When the member opens the lead detail
- Then the other member's email and mobile number are not shown anywhere.

**AC-13.2 — Leg filter**
- Given a list with leads matching Going only, Return only and both
- When the member sets Leg = Return
- Then only leads matching on the Return leg are shown, and order among them is unchanged.

**AC-13.3 — Gender filter**
- Given CFG-12 is Yes and the list has male and female members
- When the member sets Gender = Female
- Then only female members are shown.

**AC-13.4 — Dismissed stays dismissed**
- Given a member dismissed a lead
- When matching is recalculated and the pair still matches
- Then the lead stays in the dismissed list.

**AC-13.5 — Why this match**
- Given any lead
- When the member opens its detail
- Then the shared points, common days and time gap are listed.

**AC-13.6 — Lead page size**
- Given CFG-7 is 20 and a member has 35 leads
- When the list opens
- Then 20 leads are shown with a "Show more" option.

## Data Impact

| Action | Records created or changed |
|---|---|
| Open lead | Lead status New to Viewed |
| Dismiss / Undo | Lead status changes; history entry |
| Connect | Lead status Requested; connection request created (M5) |

## Reports Produced

Lead Register (RPT-8), Lead Conversion (RPT-9) — in M15.

## Cross-References

- **Config:** CFG-7, CFG-12, CFG-20, CFG-6, CFG-17.
- **Statuses:** Lead (9.5), Lead match type (9.4).
- **Validations:** V-30.
- **Decisions/assumptions:** D-10, D-16, A-33.
