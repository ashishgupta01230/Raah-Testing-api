# Module M3 — Matching Engine

> Source: `RAAH_Functional_Specification.md` Section 12. Build order #3.

## Purpose

Automatically find, for every Active route, the other members who travel a relevant route at a relevant time — **full route first, then the maximum useful segment** — and record the reason for each match. The engine never contacts anyone, never books anything and never decides a journey.

## Who Uses It

The engine is a system process. Members see its result as **leads** (M4). Admin sets its parameters (M13) and monitors its results (M11). No member or Admin can create or edit a match by hand (BR-4.6).

## Dependencies

- M2 (Active routes with both legs, cities, days, times, free-text points).
- Foundation config: CFG-1 (time tolerance), CFG-4 (min shared points), CFG-22 (text threshold).
- Foundation statuses: Route (9.2), Member (9.1), Lead match type (9.4).

## Screens

None of its own. Output shown on SCR-13.1 … SCR-13.3 (M4). Parameters on the Admin configuration screen (M13).

## When Matching Runs

| Trigger | What is re-checked |
|---|---|
| A route is activated | That route against all Active routes of the opposite type |
| An Active route is edited (new version) | That route against all opposite routes; leads that stop matching are withdrawn |
| A route is reactivated | As for activation |
| A route is deactivated, or its member is blocked or deactivated | All its leads are withdrawn |
| A block between two members is removed by Admin | That pair is re-checked |
| Route Under Review is released back to Active | That route is re-checked |

A change to CFG-1, CFG-4 or CFG-22 applies only to matching done after the change (BR-6.3).

## The Matching Procedure

For a pair of members, one **Saathi** and one **Raahi**, in this order. A pair becomes a lead only if it passes every test for at least one leg.

**Step 1 — Eligibility (route level)**

| Test | Rule |
|---|---|
| Opposite roles | One Saathi, one Raahi (A-13) |
| Route status | Both Active (A-32) |
| Member status | Both Active or Restricted (A-32) |
| City overlap | Share at least one city: Start City or End City (D-26) |
| Not blocked | Neither has blocked the other |

**Step 2 — Leg pairing.** Going vs Going, Return vs Return (A-17). A leg not entered by one member is skipped.

**Step 3 — Days.** The two legs must have **at least one travel day in common**.

**Step 4 — Time.** Difference between approximate departure times must be **no more than CFG-1 minutes** (default 30), inclusive (A-31).

**Step 5 — Aligning points.** Each point of one leg compared with the other leg's points by **text similarity** (D-22):
- not case-sensitive; extra spaces ignored;
- same place when similarity ≥ CFG-22 (70%);
- **numbers inside a point must match exactly**; a short name inside a longer name is **not** a match (A-21);
- a point pairs with only one point on the other side; highest similarity wins.

**Step 6 — Direction and shared stretch.** Among paired points, find the **longest set that appears in the same order in both legs** (A-30). Opposite direction → shared stretch too short → leg fails.

**Step 7 — Minimum overlap.** Shared stretch must have **at least CFG-4 points** (default 2, A-5).

**Step 8 — Classify the leg.**

| Result | Condition |
|---|---|
| **Full Route** | The Raahi's leg contains the Saathi's **Start and End** in that order (A-29) |
| **Useful Segment** | Passed Steps 3–7 but not Full Route; the shared stretch is the longest ordered set of shared points |
| No match | Failed any of Steps 3–7 |

**Step 9 — Lead.** If at least one leg matches, a lead is created **for each of the two members** (one in the Saathi's list, one in the Raahi's). Each lead records the result for both legs.

## Ranking

Order by these keys in sequence (A-17):

| # | Key | Better is |
|---|---|---|
| 1 | Number of legs matched | Two before one |
| 2 | Number of legs that are Full Route | More first |
| 3 | Total shared points across matched legs | More first |
| 4 | Average time difference across matched legs | Smaller first |
| 5 | Number of common travel days | More first |
| 6 | Lead creation time | Earlier first |

Ranking never uses gender, rating or any other personal detail. Filters (Gender, Leg) change what is shown, not the order (M4).

## What the Engine Records on Each Lead

| Item | Content |
|---|---|
| Lead number | LED-nnnnnnn, one per member side |
| For member / Matched member | The two members |
| Route versions used | Exact version of each route when the lead was last calculated (Principle 14, BR-8.6) |
| Per leg | Matched or not; Full Route or Useful Segment; shared points (as typed); common days; time difference in minutes |
| Cities | Start City and End City of both routes |
| Calculated on | Date and time of last calculation |

## Business Rules

**BR-12.1** — Matching is **deterministic and explainable**: same routes + same settings → same result, and every lead can show *which points, which days, what time difference* produced it (Principle 14).

**BR-12.2** — The engine matches **only Saathi with Raahi** (A-13).

**BR-12.3** — Draft, Inactive, Invalid and Under Review routes are never matched (A-32).

**BR-12.4** — **Full Route** is preferred over Useful Segment on the same leg. Useful Segment searched only when not Full Route.

**BR-12.5** — **Time comparison** uses the approximate departure times typed by the members (A-31). It does not consider where on the route each member starts.

**BR-12.6** — **Minimum shared points** (CFG-4) applies to the **shared stretch**; one common place alone never produces a lead.

**BR-12.7** — A pair has **one lead per side** at a time. Recalculation updates it; it does not create a second. A withdrawn lead that matches again is revived (see BR-13.9).

**BR-12.8** — The engine **never changes** a member's route, never moves a point, never fills in a missing stop.

**BR-12.9** — Blocking removes the pair from matching in both directions immediately.

**BR-12.10** — Leads are refreshed almost immediately (target within one minute, A-38). No "search" press required.

**BR-12.11** — Gender is **not** a matching rule. It is a viewing filter on the lead list (D-10, A-4).

**BR-12.12** — Matching events are shown on the lead's History tab; not written as individual platform audit entries (volume). Settings changes are audited (BR-6.1).

**BR-12.13** — **City overlap** (D-26): pair two routes only when they share at least one city (a Start/End City of one equals a Start/End City of the other). A route without cities is a Draft and never matched.

## Worked Example

**Ashish (Saathi), Going:** Vinay Nagar → Badarpur Metro → Govindpuri → Kalkaji → Nehru Place, Mon–Fri, **08:00**.
**Ashish, Return:** Nehru Place → Kalkaji → Govindpuri → Badarpur Metro → Vinay Nagar, Mon–Fri, **18:30**.
All candidate routes have Start/End City **Delhi**.

| Raahi | Their Going route and time | Common days | Time gap | Shared points | Result for Ashish |
|---|---|---|---|---|---|
| **Neha** | Vinay Nagar → Govindpuri → Kalkaji → Nehru Place, Tue–Thu, 07:50 | Tue–Thu (3) | 10 min | 4 | **Full Route** |
| **Rohit** | Kalkaji → Nehru Place, Mon–Fri, 08:00 | Mon–Fri (5) | 0 min | 2 | **Useful Segment** |
| **Amit** | Vinay Nagar → Badarpur Metro, Mon–Fri, 08:05 | Mon–Fri (5) | 5 min | 2 | **Useful Segment** |
| **Sunil** | Nehru Place → Kalkaji → Vinay Nagar, Mon–Fri, 08:00 | Mon–Fri | 0 min | 1 in order | **No lead** (opposite direction) |
| **Pooja** | Vinay Nagar → Badarpur Metro, Mon–Fri, 09:00 | Mon–Fri | 60 min | 2 | **No lead** (time gap > 30) |
| **Karan** | Kalkaji → Saket, Mon–Fri, 08:00 | Mon–Fri | 0 min | 1 | **No lead** (one shared point) |

Neha's Return leg: Nehru Place → Kalkaji → Govindpuri → Vinay Nagar, Tue–Thu, 18:20 → Full Route for Ashish's Return (10 min gap). Her lead matches **both legs**.

**Order in Ashish's lead list:** 1. Neha (two legs, both Full Route) · 2. Rohit (one leg, Useful Segment, 0 min) · 3. Amit (one leg, Useful Segment, 5 min). Rohit ranks above Amit (tie on keys 1–3; smaller time gap).

**Spelling example.** *Bdarpur Metro* / *vinay nagar* still pair (~93% and 100%). *Sector 12* vs *Sector 15* do **not** pair (numbers must match exactly).

## Scenarios

**SC-12.1 — New route finds a Full Route match**
1. Ashish activates his route.
2. Within ~1 minute his list contains Neha (Full Route on both legs). Neha's list gets a lead showing Ashish.

**SC-12.2 — Only a useful segment exists**
1. Ashish travels Vinay Nagar → Nehru Place; no Raahi covers all of it.
2. Amit covers only Vinay Nagar → Badarpur Metro. Lead marked Useful Segment with that stretch.

**SC-12.3 — Opposite direction**
1. A Raahi's route is Nehru Place → Vinay Nagar at 08:00.
2. Shared points appear in opposite order → shared stretch is one point → no lead.

**SC-12.4 — Time gap just outside**
1. Saathi 08:00; Raahi 08:31 (CFG-1 = 30).
2. 31-min gap fails Step 4 → no lead. At 08:30 the lead would exist.

**SC-12.5 — Time across midnight**
1. Night-shift Saathi 23:50; Raahi 00:10.
2. Gap is 20 minutes → Step 4 passes.

**SC-12.6 — Misspelled stop still matches**
1. Raahi typed *badarpoor metro*; Saathi typed *Badarpur Metro*.
2. ~87% alike → pair.

**SC-12.7 — Numbers differ**
1. Saathi *Sector 12 Metro*; Raahi *Sector 15 Metro*.
2. Numbers differ → do not pair (~93% text alike is irrelevant).

**SC-12.8 — Route edit removes a match**
1. Ashish edits Going time 08:00 → 09:30.
2. A Raahi leaving 08:05 falls outside tolerance; Going lead withdrawn. If Return still matches, the lead stays showing only Return.

**SC-12.9 — Other member deactivates**
1. Neha deactivates.
2. Her lead disappears from Ashish's list and his from hers. Accepted connection stays (A-6).

**SC-12.10 — Blocked pair**
1. Ashish blocks Rohit.
2. Both leads withdrawn. Never appear again even if routes match.

**SC-12.11 — No match found**
1. A rare route matches nothing.
2. Member sees "No relevant person found yet. We will tell you when someone matches." (M4, M9). No error.

**SC-12.12 — Settings changed**
1. Admin raises CFG-1 30 → 45.
2. New calculations use 45. Already-shown leads not withdrawn (BR-6.3).

**SC-12.13 — No common city**
1. Ashish (Delhi→Delhi) and Raahi (Noida→Noida) have identical points/days/times.
2. Step 1 fails (no shared city) → no lead.

**SC-12.14 — Cross-city match**
1. Meera (Delhi→Noida) meets a Raahi (Delhi→Delhi). Share Delhi → Step 1 passes; stops compared as usual.

## Acceptance Criteria

**AC-12.1 — Time tolerance boundary**
- Given CFG-1 is 30 and legs otherwise match
- When departure times differ by exactly 30 minutes
- Then a lead is created; at 31 minutes no lead is created.

**AC-12.2 — Direction**
- Given two legs share points A and B, but A first in one and B first in the other
- When matching runs
- Then no lead is created for that leg.

**AC-12.3 — Minimum shared points**
- Given CFG-4 is 2
- When two legs share only one point
- Then no lead is created.

**AC-12.4 — Full Route**
- Given a Raahi leg contains the Saathi's Start and End in that order
- When matching runs
- Then the leg is Full Route, even if some Saathi intermediate points are missing from the Raahi leg.

**AC-12.5 — Fuzzy text and numbers**
- Given CFG-22 is 70
- When one types *Badarpur Metro* and the other *bdarpur metro*
- Then the points pair; and *Sector 12* and *Sector 15* do not pair.

**AC-12.6 — Both legs ranking**
- Given one lead matches both legs and another matches one leg
- When the list is shown
- Then the both-legs lead is placed first.

**AC-12.7 — Exclusions**
- Given a route is Draft, Inactive, Invalid or Under Review, or its member Blocked
- When matching runs
- Then that route produces no leads.

**AC-12.8 — Same-role pairs**
- Given two Saathi routes with identical points
- When matching runs
- Then no lead is created.

**AC-12.9 — City overlap**
- Given two routes are otherwise a perfect match
- When they share no city
- Then no lead is created; and when they share at least one city, a lead is created.

## Known Limitations

- **Free text.** Accuracy depends on consistent spelling (D-17, D-22). CFG-22 tunable later.
- **Departure time** compared as typed, regardless of where each member starts (BR-12.5). Follows D-4.
- **Cities.** Matching checks shared city, not that each stop belongs to that city. RPT-4 helps Admin spot cross-city false pairs.

## Data Impact

| Event | Records created or changed |
|---|---|
| Match found | Lead created for each member (or revived) |
| Match changes | Lead updated; route versions and leg results re-recorded |
| Match lost | Lead set to Withdrawn by System |

## Reports Produced

Matching Summary (RPT-6), Lead Conversion (RPT-9) — in M15.

## Cross-References

- **Config:** CFG-1, CFG-4, CFG-22.
- **Statuses:** Lead match type (9.4), Route (9.2), Member (9.1).
- **Validations:** V-27, V-28, V-29, V-53.
- **Decisions/assumptions:** A-13, A-17, A-29, A-30, A-31, A-32, A-38, D-22, D-26, A-21.
