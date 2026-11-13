# Module M2 — Route Management

> Source: `RAAH_Functional_Specification.md` Section 11. Build order #2.

## Purpose

Capture each member's **complete route** — a Going leg and, if they travel back, a Return leg — with the regular days and approximate departure time, so the Matching Engine (M3) can find relevant people. This is the module that every other module depends on.

## Who Uses It

Member creates and manages their own route. Admin views all routes and may deactivate one for safety reasons (BR-4.5).

## Dependencies

- M1 (profile, journey role, vehicle), Foundation (statuses 9.2, config CFG-2/3/18/19/21/22, audit, city master from M13's City Master data).

## Screens

| Screen | Purpose |
|---|---|
| SCR-11.1 My Route | Summary of the member's current route: status, Going leg, Return leg, vehicle, active-since date |
| SCR-11.2 Create / Edit Route | Form with two tabs: **Going** and **Return** |
| SCR-11.3 Review & Activate | Final read-only view of both legs with the Activate button |
| SCR-11.4 Route History | Past versions and past routes, read-only |

## Route Structure

```text
ROUTE   (one per member Active at a time; type = Saathi or Raahi)
 ├── START CITY / END CITY   (mandatory to activate; from the Admin list, may be the same, D-26)
 ├── GOING LEG   (mandatory)
 │     ├── Start Point            (mandatory, free text)
 │     ├── Intermediate Points    (0 to 3, ordered, free text)
 │     ├── End Point              (mandatory, free text)
 │     ├── Travel Days            (at least one)
 │     └── Approximate Departure Time
 │
 └── RETURN LEG  (optional by default, CFG-19)
       ├── Start Point, Intermediate Points, End Point
       ├── Travel Days
       └── Approximate Departure Time
```

A leg has **2 to 5 points in total including Start and End** (CFG-2, CFG-3, A-1), so up to 3 intermediate points.

## Field Specification — Route

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Route Number | Auto | — | Generated on first save (RTE-nnnnnn) |
| Member | Auto | — | Logged-in member |
| Route Type | Auto | — | Saathi or Raahi, taken from the member's journey role |
| Start City | Dropdown | Yes, to activate | The city where the Going leg starts, from the Admin-managed list of active cities (D-26). Can be empty while Draft. |
| End City | Dropdown | Yes, to activate | The city where the Going leg ends. May be the same as Start City. The Return leg uses the same two cities reversed. |
| Vehicle | Display | — | Raahi only; read from the profile (Bike / Car) |
| Status | Auto | — | Per Section 9.2 |
| Version | Auto | — | Increases each time an Active route is edited (BR-8.6) |

## Field Specification — Each Leg (Going and Return)

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Start Point | Text | Yes (Going always; Return if Return is entered) | 2–100 characters. Typed free text (D-17). |
| Intermediate Point 1–3 | Text | No | Same text rules. Kept in order entered; member can reorder. |
| End Point | Text | Yes | Same text rules. |
| Travel Days | Multi-select | Yes | At least one of Mon–Sun (CFG-18). Return defaults to Going days, can be changed (A-16). |
| Approximate Departure Time | Time | Yes | Time the member usually leaves the Start point, 24-hour or AM/PM. |

**Helper on the Return tab:** a **"Copy reversed from Going leg"** button pre-fills the Return points in reverse order and the same travel days. The member can edit any of it. The departure time is **not** copied and must be entered (A-16).

**Hint beside every point field:** "Enter an area or landmark, for example *Badarpur Metro*. You do not need your full address."

## Business Rules

**BR-11.1** — **One Active route per member** (D-14). A member cannot activate a second route while one is Active. They must deactivate the first.

**BR-11.2** — **One Draft at a time** (A-27). A member can hold one Draft route in addition to the Active one. Starting a new route while a Draft exists opens that Draft.

**BR-11.3** — **Start and End are mandatory** for every leg that exists. Intermediate points are optional. Total points per leg between CFG-3 (2) and CFG-2 (5).

**BR-11.4** — Points are **free text typed by the member** (D-17). The system gives no suggestions, auto-complete or place picker.

**BR-11.5** — **Point text rules**: 2 to 100 characters; letters, digits, spaces and `. , - ' / & ( )` only. A point cannot contain a web link or a run of 10 or more digits (A-26).

**BR-11.6** — **No duplicate places inside a leg.** No two points in the same leg may be the same place, judged with the same text-match rule as matching (≥ CFG-22, default 70%, numbers exact; D-22, A-21). In particular **Start and End must not be the same place.** The system points to the two clashing points.

**BR-11.7** — **Order matters.** The sequence of points is the direction of travel and is what the Matching Engine compares. Reordering changes the route.

**BR-11.8** — **Travel Days**: at least one per leg. **Departure Time**: mandatory for every leg that exists.

**BR-11.9** — **Return leg**: optional by default (A-15, CFG-19). When CFG-19 is Yes, a route cannot be activated without it. If the member starts a Return leg, all its mandatory fields must be complete before activation. A member can remove the Return leg at any time while editing.

**BR-11.10** — **Return departure time** has no required relation to Going (e.g. night shifts). Only a gentle warning if Return time is earlier than Going time on the same day.

**BR-11.11** — **Draft**: a route can be saved at any stage as Draft with incomplete fields. Matching never uses a Draft.

**BR-11.12** — **Activation** needs all of: member Active; Start City and End City chosen; Going leg complete and valid; Return leg complete and valid if present, or present if CFG-19 is Yes; the member has reviewed SCR-11.3. On activation the route becomes Active and is immediately submitted to matching.

**BR-11.13** — **Editing an Active route** (A-28): the route stays Active; a new **version** is saved, keeping the old one (BR-8.6). Matching re-runs. Leads that no longer match are withdrawn ("Withdrawn by System"). **Pending requests and existing connections are not affected.** A receiver of a pending request sees the sender's current route, with a note that it changed since the request was sent.

**BR-11.14** — **Deactivating a route** (by member, role change, self-deactivation, or Admin): all leads withdrawn, pending requests (sent or received) withdrawn, route cannot be matched. **Existing connections and chats are not affected** (A-6).

**BR-11.15** — **Reactivation**: an Inactive route can be reactivated by the member if they have no other Active route. It goes through review and activation again and is re-matched.

**BR-11.16** — **No hard delete.** A route is only deactivated. Past routes and versions stay visible (SCR-11.4 and Admin).

**BR-11.17** — **Route type is fixed** once created. Changing Saathi ↔ Raahi means creating a new route (BR-10.7).

**BR-11.18** — A route belongs to its member alone. No other member can open, edit or deactivate it (BR-4.1, BR-4.2). Admin may deactivate only with a recorded reason (BR-4.5).

**BR-11.19** — **Raahi vehicle**: a Raahi route can be activated only if the profile has a vehicle.

**BR-11.20** — **Cities** (D-26): a route has Start City and End City, both mandatory before activation, chosen from the Admin-managed list of **active** cities. They may be the same. They apply to both legs (Return reversed). Matching considers two routes only when they **share at least one city** (BR-12.13). Changing cities on an Active route saves a new version and re-runs matching (BR-11.13). If the city is not listed, the member uses **"My city is not listed"** (A-56, M13) and cannot activate until both cities are chosen.

## Status Lifecycle

Uses Route Status of Section 9.2. Both legs share one status.

| From | To | Who | Condition |
|---|---|---|---|
| (new) | Draft | Member | First save |
| Draft | Active | Member | Activation checks pass (BR-11.12) |
| Draft | Inactive | Member | Abandoned route kept for history |
| Active | Active (new version) | Member | Edit, BR-11.13 |
| Active | Inactive | Member / Admin / System | Deactivation, role change, account block or deactivation |
| Inactive | Active | Member | Reactivation, BR-11.15 |
| Active | Under Review | Admin (via M12) | Route is subject of an open safety report |
| Under Review | Active / Inactive | Admin | After the report is decided |
| Draft / Active | Invalid | System | A rule found broken (e.g. CFG-2 data fix) |
| Invalid | Draft | Member | After correction |

## What a Route Change Does to Leads and Requests

| Event | Leads | Pending requests | Connections / chats |
|---|---|---|---|
| Route activated | Generated (M3) | — | — |
| Active route edited | Re-calculated; no-longer-matching become Withdrawn by System | Kept | Kept |
| Return leg removed | Leads matching **only** on Return withdrawn | Kept | Kept |
| Route deactivated | All withdrawn | Withdrawn | Kept |
| Member blocked or deactivated by Admin | All withdrawn | Withdrawn | Closed (M12) |

## Scenarios

**SC-11.1 — Saathi creates a full route with both legs**
1. Ashish (Saathi) opens SCR-11.2. Going: *Vinay Nagar* → *Badarpur Metro* → *Govindpuri* → *Kalkaji* → *Nehru Place*, Mon–Fri, 8:00 AM.
2. Return tab: "Copy reversed from Going leg". Points run Nehru Place → … → Vinay Nagar with Mon–Fri. Enters 6:30 PM.
3. On SCR-11.3 reviews and activates. Status Active; matching starts.

**SC-11.2 — Raahi creates only a Going leg**
1. Amit (Raahi, Bike) enters Going: *Vinay Nagar* → *Badarpur Metro*, Mon–Fri, 8:05 AM. Return empty.
2. CFG-19 is No, so he activates. Only the Going leg takes part in matching.

**SC-11.3 — Draft finished next day**
1. Member enters Start and End, saves as Draft.
2. Next day opens the Draft, adds days and time, reviews, activates.

**SC-11.4 — Too many points**
1. Member tries to add a 6th point to a leg.
2. System blocks: "A leg can have at most 5 points, including Start and End."

**SC-11.5 — Start and End are the same place**
1. Member types Start *Nehru Place* and End *nehru place*.
2. System blocks: "Start and End look like the same place."

**SC-11.6 — Two points that look alike**
1. Member adds *Govindpuri* then *Govindpure* in the same leg.
2. ~90% alike → flagged as the same place; member must remove/change one.
3. *Govindpuri* + *Govindpuri Metro* (~62%) would be accepted as different.

**SC-11.7 — Phone number typed in a point**
1. Member types "Vinay Nagar 9876543210".
2. System blocks: "Please do not enter phone numbers or links. Use chat after connecting."

**SC-11.8 — No travel day chosen**
1. Member leaves Travel Days empty and tries to activate.
2. System blocks and highlights the missing field on the Going tab.

**SC-11.9 — Second route while one is Active**
1. Member with an Active route taps "Activate" on a Draft.
2. System says "You already have an Active route. Deactivate it first."

**SC-11.10 — Edit an Active route**
1. Ashish changes Going time from 8:00 to 9:30 AM.
2. New version saved; matching re-runs. A lead with a Raahi leaving 8:05 now falls outside tolerance and is withdrawn.
3. A request Ashish already sent stays Pending.

**SC-11.11 — Remove the Return leg**
1. Member removes the Return leg of an Active route.
2. Leads matching only on Return are withdrawn; Going leads remain.

**SC-11.12 — Deactivate and reactivate**
1. Member deactivates route while on leave. Leads withdrawn; pending requests withdrawn; connections/chats stay.
2. After leave reactivates. Goes through review+activation; new leads generated.

**SC-11.13 — Return leg compulsory**
1. Admin sets CFG-19 to Yes.
2. A member activating a Going-only route is asked to complete the Return leg first.
3. Already-Active routes are not forced until next edited.

**SC-11.14 — Raahi without a vehicle**
1. A member who switched to Raahi has no vehicle on the profile.
2. System asks for Bike or Car before the Raahi route can be activated.

**SC-11.15 — Admin deactivates a route for safety**
1. Admin deactivates a route linked to a verified complaint, with reason.
2. Route Inactive; leads and pending requests withdrawn; member notified. Connections handled by M12.

**SC-11.16 — City missing from the list**
1. Member finds the city not listed.
2. Taps "My city is not listed", types the name. Admin notified; member can save Draft but not activate.
3. Admin adds the city; member told in-app; chooses it and activates.

**SC-11.17 — Same stops, different cities**
1. Saathi (Delhi→Delhi) and Raahi (Noida→Noida) both type identical points.
2. Routes share no city → no lead.

**SC-11.18 — A cross-city commute**
1. Meera (Delhi→Noida). A Raahi (Delhi→Delhi) and a Raahi (Delhi→Noida) are both eligible because each shares a city. Stops must still match.

## Acceptance Criteria

**AC-11.1 — Point limit per leg**
- Given CFG-2 is 5
- When a member adds a sixth point to the Going leg
- Then the system rejects it and the leg keeps its five points.

**AC-11.2 — Start and End mandatory**
- Given a member is activating a route
- When the Going leg has no End point
- Then activation is blocked and the End field is highlighted.

**AC-11.3 — One Active route**
- Given a member has an Active route
- When they try to activate another route
- Then the system refuses until the first is deactivated.

**AC-11.4 — Return leg optional**
- Given CFG-19 is No
- When a member activates a route with a complete Going leg and no Return leg
- Then the route becomes Active.

**AC-11.5 — Copy reversed**
- Given a Going leg of A → B → C
- When the member taps "Copy reversed from Going leg"
- Then the Return leg shows C → B → A with the same travel days and an empty departure time.

**AC-11.6 — Deactivation effects**
- Given a member has leads, a pending request and an accepted connection
- When they deactivate their route
- Then the leads and the pending request are withdrawn and the accepted connection and its chat remain.

**AC-11.7 — Same-place check**
- Given a leg with Start "Nehru Place"
- When the member enters End "nehru place"
- Then the system rejects it as the same place.

**AC-11.8 — City mandatory**
- Given a member is activating a route
- When the Start City or the End City has not been chosen
- Then activation is blocked and the missing city field is highlighted.

**AC-11.9 — Only active cities**
- Given Admin has deactivated a city
- When a member creates a new route
- Then that city is not offered in the list.

## Data Impact

| Action | Records created or changed |
|---|---|
| Save Draft | Route created (RTE number), status Draft |
| Activate | Route Active; matching job run (M3) |
| Edit Active route | New route version; leads recalculated; audit entry |
| Deactivate | Route Inactive; leads Withdrawn by System; pending requests Withdrawn |
| Reactivate | Route Active; matching job run |

## Reports Produced

Route Register (RPT-3), Route Coverage (RPT-4) — defined in M15.

## Cross-References

- **Config:** CFG-2, CFG-3, CFG-18, CFG-19, CFG-21, CFG-22.
- **Statuses:** Route (9.2), Route type (9.3), Route leg (9.3a).
- **Validations:** V-14 … V-26.
- **Decisions/assumptions:** D-14, D-15, D-17, D-26, A-1, A-6, A-15, A-16, A-26, A-27, A-28, A-56.
