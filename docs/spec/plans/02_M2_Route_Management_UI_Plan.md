# M2 — Route Management — UI Plan

| | |
|---|---|
| **Module** | M2 — Route Management (spec `docs/spec/02_M2_Route_Management.md`) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5, module design §6.3, patterns §7, delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20) |
| **Device/network** | Mid-range Android + Chrome, Safari on iPhone; slow-network tolerant (skeletons, small images, drafts saved on the server) |
| **Companion** | API plan: `02_M2_Route_Management_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M2 gives the member a guided, forgiving way to describe the route they actually travel:

1. See the current route at a glance — status, both legs, vehicle, active-since (SCR-11.1).
2. Create or edit a route with **Going** and **Return** legs in a four-step flow (SCR-11.2).
3. Review both legs and pass an activation checklist (SCR-11.3).
4. See past routes and versions, read-only (SCR-11.4).

UI jobs for this module:

- **The route is the product** (UP-8). The form is a guided stepper with a review step; "Copy reversed" removes retyping.
- **Nothing is a surprise** (UP-5). Deactivation and Active edits say what happens to leads, requests and chats before acting.
- **Validate as they type** (UP-7). Same-place and phone/link errors appear beside the fields, using the same rule the server uses, so activation never surprises.
- **Every state is designed** (UP-10): no route, Draft, Active, Inactive, Under Review, Invalid, offline, saving, error.

Out of scope for M2 screens (linked, owned elsewhere): profile/vehicle edit (M1), leads and requests (M4/M5), matching results (M3), city master (M13), Admin route monitor (M11).

---

## 2. Navigation and routes

M2 lives under the **Route** tab (bottom nav on phone, left sidebar on desktop) and is also reachable from the Me hub's route summary. The top bar keeps the RAAH mark, role chip and notification bell (UI Plan §4.1).

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| My Route | `/route` | SCR-11.1 | Route tab; dashboard "My Route" card |
| Create / Edit Route | `/route/edit` | SCR-11.2 | "Create your route", "Edit route", "Continue setting up" (Draft) |
| Review & Activate | `/route/review` | SCR-11.3 | "Next: Review" from the form |
| Route History | `/route/history` | SCR-11.4 | "Route history" link on My Route |
| City request sheet | modal on `/route/edit` | SCR-11.2 / A-56 | "My city is not listed" |
| Deactivate dialog | dialog on `/route` and `/route/edit` | BR-11.14 | "Switch off" actions |

**One flow for create and edit.** `/route/edit` opens in create mode when there is no route, resume mode for a Draft, and edit mode for an Active route. Mode only changes copy and the review/activation step, not the layout.

Global gates that affect M2 (owned by Foundation):

- **Verify-email gate:** an Unverified member sees only Verify email; Route is unreachable (BR-5.1/BR-10.2).
- **Legal re-acceptance gate:** shown before any M2 screen when a version is pending (BR-22.16).
- **Restricted banner:** a Restricted member may still manage and activate routes; the restriction banner belongs to Leads (BR-21.6).
- **Inactive/Blocked:** cannot log in (BR-5.4).

---

## 3. Screen inventory and components

| Screen | Components used (UI Plan §3.6) |
|---|---|
| My Route | C-12 Badge/chip, C-15 Route line, C-33 Key-value list, C-21 Banner, C-1 Button, C-23 Dialog, C-25 Empty state, C-26 Skeleton |
| Create / Edit Route | C-30 Stepper, C-10 Tabs (Going/Return), C-9 Route point input, C-4 Select, C-7 Day selector, C-8 Time field, C-1 Button, C-2 helper text, C-21 Banner, C-22 Toast, C-23 Dialog, C-24 Bottom sheet |
| Review & Activate | C-15 Route line, C-33 Key-value list, C-30 Stepper, C-1 Button, C-21 Banner, C-12 Badge |
| Route History | C-33, C-15 (compact), C-12, C-34 History timeline, C-35 Show more, C-26 Skeleton |
| City request | C-2, C-24, C-22 |
| Deactivate dialog | C-23 Dialog (destructive), C-21 Warning banner |

No new reusable component is required; M2 exercises `C-9`, `C-10`, `C-30`, `C-7` and `C-8` from the design system for the first time. Build them here and reuse in Admin.

---

## 4. Screen designs

### 4.1 My Route — `/route` (W-3.1 / SCR-11.1)

```text
┌────────────────────────────────────────┐
│  My route                    [Active]  │
│  Delhi → Noida              [Saathi]   │
│  ┌──────────────────────────────────┐  │
│  │ Going  Mon–Fri · 8:00 AM         │  │
│  │ Vinay Nagar ● Badarpur Metro ●   │  │
│  │ Govindpuri ● Kalkaji ● Nehru Pl. │  │
│  ├──────────────────────────────────┤  │
│  │ Return Mon–Fri · 6:30 PM         │  │
│  │ Nehru Place ● … ● Vinay Nagar    │  │
│  └──────────────────────────────────┘  │
│  Active since 12 Oct 2026              │
│  [ Edit route ]      [ Switch off ]    │
│  Route history                      >  │
└────────────────────────────────────────┘
```

| Route state | What the screen shows |
|---|---|
| **No route** | C-25 empty state: "Create your route to find people who travel your way." + primary **Create route** |
| **Draft** | Badge **Draft**, "Continue setting up" primary, stepper progress; no route line treated as live |
| **Active** | Route badge, both legs, vehicle chip (Raahi), active-since, **Edit route** + **Switch off** |
| **Inactive** | Badge **Inactive**, "This route is off. You will not get leads." + **Switch on** (reactivate) + Edit |
| **Under Review** | Amber banner "This route is under review. You cannot change it for now." Edit disabled (G-1) |
| **Invalid** | Red banner naming the broken rule + **Fix route** to the failing step |

| Aspect | Design |
|---|---|
| Both legs | One card: Going block, divider, Return block. Each leg: days summary + departure time + C-15 route line (start/end marked). No Return → a single "Return leg not added" row with **Add Return leg** |
| Role and vehicle | Role chip (Saathi indigo / Raahi teal) and vehicle chip for Raahi |
| Actions | **Edit route** primary; **Switch off** secondary until tapped (destructive confirm). Reactivate for Inactive |
| History | A full-width row to `/route/history` |
| States | Skeleton card while loading; error banner with Retry; offline → write actions disabled with notice |
| Desktop | Two columns: route card left, summary facts + actions right; route line wider |

### 4.2 Create / Edit Route — `/route/edit` (W-3.2 … W-3.5 / SCR-11.2)

Four steps in a stepper **① Cities · ② Going · ③ Return · ④ Review**. A tab marks a check when complete and a dot when it has an error. **Save draft** is always available in the header.

#### Step 1 — Cities

```text
┌────────────────────────────────────────┐
│  ← Create route             Save draft │
│  ①Cities ─ ②Going ─ ③Return ─ ④Review │
│  Where does your journey start and end?│
│  Start city                            │
│  [ Select ▼ ]                          │
│  End city                              │
│  [ Select ▼ ]                          │
│  (i) A route can start and end in the  │
│  same city.                            │
│  My city is not listed                 │
│  [           Next: Going leg       ]   │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| City select | C-4 native select on phone; searchable list on desktop; **only active cities** (AC-11.9) |
| Same city | Allowed; no warning (D-26) |
| Missing city | If the member skips, Draft still saves; activation later shows the error on this step (AC-11.8) |
| City not listed | "My city is not listed" opens the city request sheet (§4.7); member may keep working on the Draft |
| Next | Always enabled; server validates on save/review, errors appear inline |

#### Step 2 — Going leg

```text
┌────────────────────────────────────────┐
│  ← Create route             Save draft │
│  ①Cities ✓ ─ ②Going ● ─ ③Return ─ ④Review│
│  [ Going ● ]  [ Return ]               │
│  Start point                           │
│  ① [Vinay Nagar_______________]        │
│  Stops on the way (up to 3)            │
│  ② [Badarpur Metro____________] ≡ ✕    │
│  ③ [Govindpuri________________] ≡ ✕    │
│  [ + Add a stop ]                      │
│  End point                             │
│  ④ [Nehru Place_______________]        │
│  (i) Enter an area or landmark, for    │
│  example Badarpur Metro. You do not    │
│  need your full address.               │
│                                        │
│  Travel days                           │
│  (M)(T)(W)(T)(F)( S )( S )  Weekdays   │
│  Leaves around                         │
│  [ 8:00 AM ]                           │
│  [        Next: Return leg         ]   │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Point list | Start fixed first, End fixed last; intermediate stops reorder by drag handle **and** up/down buttons (keyboard/SR friendly); positions renumber as the order changes |
| Add stop | Hidden at the limit (CFG-2), replaced with "A leg can have at most 5 points, including Start and End." (SC-11.4) |
| Free text | No suggestions or place picker (D-17); helper text under every point (BR-11.4) |
| Same-place check | Live on blur/typing via API `points/validate`; names both points: "Govindpuri and Govindpure look like the same place." (SC-11.5/11.6) |
| Phone/link | "Please do not enter phone numbers or links. Use chat after connecting." (SC-11.7) |
| Length | "Each point must be 2 to 100 characters." |
| Days | Seven round toggles + **Weekdays** / **All** shortcuts; error "Choose at least one day." (SC-11.8) |
| Time | C-8 12-hour AM/PM; required |
| Tab markers | Check on complete, dot on error; failing field gets focus on Next |
| Autosave | "Save draft" persists the whole form; leaving with unsaved changes prompts "Discard changes?" |

#### Step 3 — Return leg

```text
│  [ Going ✓ ]  [ Return ● ]             │
│  [ Copy reversed from Going leg ]      │
│  Travel days default to the Going days │
│  Leaves around  [ __:__ ]  ← empty     │
│  Skip Return leg                       │  ← only when CFG-19 is No
```

| Aspect | Design |
|---|---|
| Copy reversed | Fills points **in reverse** and the same days; leaves departure time **empty** (A-16, AC-11.5) |
| Skip | Visible only when CFG-19 = No (A-15); with CFG-19 = Yes the Return tab is required and cannot be skipped (SC-11.13) |
| Remove Return | A secondary "Remove return leg" on an existing route, with a note that leads matching only on Return will be withdrawn (BR-11.14) if the route is Active |
| Time order | Gentle note only if Return is earlier than Going on the same day; never blocks (BR-11.10) |
| Same rules | Points, days, duplicate checks identical to Going |

#### Step 4 — Review & Activate — `/route/review` (W-3.5 / SCR-11.3)

```text
┌────────────────────────────────────────┐
│  ← Review your route                   │
│  Delhi → Noida              [Saathi]   │
│  Going  Mon–Fri · 8:00 AM              │
│   ① Vinay Nagar                        │
│   ② Badarpur Metro                     │
│   ③ Govindpuri                         │
│   ④ Nehru Place                        │
│  Return Mon–Fri · 6:30 PM              │
│   ① Nehru Place … ④ Vinay Nagar        │
│  Checklist                             │
│   ✓ Email verified                     │
│   ✓ Cities chosen                      │
│   ✓ Going leg complete                 │
│   ✓ Return leg complete                │
│   ✓ Vehicle (Raahi)                    │
│  [        Activate route          ]    │
│  [ Go back and edit ]                  │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Checklist | From the API `review` result; a failed check shows a red cross, the reason and a **Fix** link that jumps to the owning step/tab |
| Edit | "Go back and edit" returns to the form with input preserved |
| Activate | Enabled only when `canActivate`; on success, a success screen: "Your route is active. We are looking for people who travel your way. This usually takes under a minute." + **See leads** (M4) |
| Active edit | Copy differs: "Save changes" instead of "Activate"; an info banner explains a new version, leads recalculated, pending requests and connections unaffected (BR-11.13) |
| Error | `ROUTE_INCOMPLETE` maps to the checklist; never lose input |
| States | Reviewing (skeleton on the checklist), activating (button spinner), network error banner with Retry |

### 4.3 Route History — `/route/history` (SCR-11.4)

```text
┌────────────────────────────────────────┐
│  ← Route history                       │
│  RTE-000123  Saathi  [Inactive]        │
│  Delhi → Noida · v2                    │
│   Active 01 Aug 2026 – 12 Oct 2026     │
│  ┌ compact route line ───────────────┐ │
│  └───────────────────────────────────┘ │
│  RTE-000121  Saathi  [Inactive] · v1   │
│   ...                                  │
│  [ Show more ]                         │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Content | Group by route (route number, type, status) then versions (version no, dates, change reason) |
| Route line | Compact, read-only C-15 |
| Timeline | Optional C-34 inside an expanded item (activation, edits, deactivation) |
| Paging | Keyset **Show more** (C-35) |
| Read-only | No edit/deactivate actions here; back to `/route` |
| Empty | "No past routes yet." |
| States | Skeleton list; error with Retry; offline read-only |

### 4.4 Deactivate route dialog (BR-11.14 / AC-11.6)

Opened by **Switch off** on My Route. A C-23 destructive dialog:

| Content | Copy |
|---|---|
| Title | "Switch off your route?" |
| Effects | "Your leads and pending requests will be withdrawn. Your existing connections and chats stay." |
| Actions | Cancel (default focus) · red **Yes, switch off** |
| Loading | Confirm disabled with spinner; no double submit |
| Error | "We could not switch off your route. Nothing was changed. Try again." |
| Success | Toast "Your route is switched off." and My Route shows Inactive |

### 4.5 Edit Active route warning (BR-11.13)

Before the form opens on an Active route, show a dismissible info banner at the top of `/route/edit`:

> "Editing creates a new version. Your leads will be recalculated. Pending requests and connections are not affected."

No extra confirmation is needed for a save; the banner explains the consequence.

### 4.6 Role and CFG-19 states

| Situation | UI |
|---|---|
| Raahi without vehicle | Banner on Review/Activate: "Choose Bike or Car before activating a Raahi route." with a link to M1 Edit Profile (BR-11.19) |
| CFG-19 = Yes | Return tab required; "Skip Return leg" hidden; activation check `Return leg complete` required |
| CFG-19 = No | Return optional; "Skip Return leg" shown |
| Role chip | Read-only in the form, from the profile; changing role is M1 only |

### 4.7 City request sheet (A-56 / SC-11.16)

```text
┌  My city is not listed ───────────────┐
│  City name                            │
│  [______________________________]     │
│  (i) Admin will add it. You can save  │
│      a draft but cannot activate      │
│      until both cities are available. │
│  [        Send request            ]   │
└───────────────────────────────────────┘
```

| State | UI |
|---|---|
| Sent | Sheet closes, toast "Request sent to Admin.", Draft still savable |
| Duplicate | "You have already requested this city." |
| Offline | Primary disabled |

### 4.8 Point input micro-behaviour (C-9)

| Aspect | Design |
|---|---|
| Layout | Numbered pin, text field, drag handle / up-down buttons, remove for intermediates only |
| Errors | Inline under the field, plain language; never a modal |
| Limits | Live count "Point 2 of 5"; add stops disabled at `maxPoints` |
| Reorder | Renumber positions; announce "Point 3 of 5, moved up" to screen readers |
| Keyboard | Up/down buttons are the keyboard path; drag is an enhancement only |
| Paste | Sanitize/validate on paste; phone numbers and links rejected with the same copy |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton (C-26), not a bare spinner; preserve layout height |
| Empty | No route → Create; Draft → Continue; no history → message; no Return → Add Return row |
| Error | Banner with plain message and one Retry; keep form input |
| Success | Toast (C-22, 4 s, polite live region) or a success screen for activation |
| Saving | Disable only the submitting control; show progress; prevent double submit |
| Disabled | Explain inline: Edit disabled Under Review; Activate disabled until checklist passes; Switch off disabled offline |
| No permission | 403 page for wrong role; non-owner route → 404-style "Route not found" |
| Offline | Slim banner "No connection. Changes will not be saved."; disable Save/Activate/Switch off/Reactivate |
| Session expired | Foundation dialog; unsaved form kept in memory (BR-5.6) |
| Verify gate | Unverified member never reaches M2; sees Verify email only |
| Restricted | M2 fully usable; restriction banner belongs to Leads |
| Under Review | Edit disabled with amber banner (G-1) |
| Invalid | Red banner naming the rule + Fix action |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Single column; stepper as a horizontal progress row; sticky **Save draft** / **Next**; point rows full width; bottom sheet for city request and confirmations |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | Form max ~640 px centred; stepper as a left rail begins here |
| Desktop 1024+ | Left rail stepper; Review shows Going and Return **side by side**; point rows wider with visible reorder buttons; dialogs centred (C-24) |
| Wide 1280+ | Member content max 1120 px; do not stretch the form |

- The shell switches bottom tabs → left sidebar at tablet/desktop (UI Plan §4.1); M2 does not implement the shell.
- Respect safe-area padding for sticky actions and the bottom nav; the keyboard must not cover the active point field or Save.
- At 200% text zoom the layout must not scroll horizontally.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using the palette in UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring, never removed; focus returns after sheets/dialogs close; focus moves to the first error on Next/Save |
| Labels | Visible label above every field; point hint and examples are helper text, not labels |
| Targets | ≥ 44 × 44 px, 8 px apart; drag handles paired with up/down buttons |
| Colour | Status, role and match meaning always carry text; errors have icon + text |
| Screen readers | Announce the point list position and moves; announce tab error dots ("Going leg has 2 errors"); announce saves, activation and deactivation |
| Keyboard | Stepper, tabs, points reorder, day toggles, time, dialogs and sheets fully keyboard-operable; dialogs trap focus |
| Motion | Honour "reduce motion"; no decorative animation; tab/step changes are instant or ≤ 200 ms |
| Language | `lang="en"`; dates as `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M2)

| Situation | Copy |
|---|---|
| Page title | "My route" / "Create route" / "Edit route" |
| Route point hint | "Enter an area or landmark, for example Badarpur Metro. You do not need your full address." |
| Point length | "Each point must be 2 to 100 characters." |
| Point chars | "Use letters, numbers and simple punctuation only." |
| Phone/link | "Please do not enter phone numbers or links. Use chat after connecting." |
| Duplicate places | "<A> and <B> look like the same place. Change one of them." |
| Start = End | "Start and End look like the same place." |
| Point limit | "A leg can have at most 5 points, including Start and End." |
| Days missing | "Choose at least one day." |
| Time missing | "Enter the departure time." |
| City missing | "Choose the start and end city." |
| Return required | "This route needs a return leg. Add it to continue." |
| One Active route | "You already have an Active route. Switch it off first." |
| Raahi vehicle | "Choose Bike or Car before activating a Raahi route." |
| Active edit banner | "Editing creates a new version. Your leads will be recalculated. Pending requests and connections are not affected." |
| Deactivate dialog | "Your leads and pending requests will be withdrawn. Your existing connections and chats stay." |
| Deactivated | "Your route is switched off." |
| Activated | "Your route is active. We are looking for people who travel your way. This usually takes under a minute." |
| Under Review | "This route is under review. You cannot change it for now." |
| Empty route | "Create your route to find people who travel your way." |
| City request sent | "Request sent to Admin. You can save a draft but cannot activate until the city is added." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load My Route / Draft resume | `GET /me/route` |
| Load limits (points, days, CFG-19, vehicle) | `GET /me/route/options` |
| City dropdown / city search | `GET /me/route/cities?search=` |
| "Create route" | `POST /me/route` |
| Open an existing route | `GET /me/route/{routeId}` |
| Save draft / save Active edit | `PUT /me/route/{routeId}` |
| Live point check on blur | `POST /me/route/{routeId}/points/validate` |
| Review & Activate checklist | `POST /me/route/{routeId}/review` |
| Activate / reactivate | `POST /me/route/{routeId}/activate` |
| Switch off | `POST /me/route/{routeId}/deactivate` |
| "My city is not listed" | `POST /me/route/{routeId}/city-requests` |
| Route history | `GET /me/route/history` |

Rules for the client:

- Always send the auth cookie/token; never pass a member id.
- On `401` → Foundation session-expiry handling; on `403` → generic no-access / verify gate; on non-owner `404` → "Route not found".
- On `409 ROUTE_CONFLICT` reload the route and warn about the other-device change.
- On `409 ROUTE_ALREADY_ACTIVE` show the "switch it off first" copy with a link to My Route.
- After a successful save/activate/deactivate, refresh `GET /me/route`; after activation, refresh the shell role/route summary.
- "Copy reversed" is purely client-side; the server still validates on save.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M3 matching | Activation success copy is generic ("looking for people"); no live lead count |
| M4 leads | "See leads" opens the Leads tab; until built, hide the button or route to an empty placeholder |
| M1 profile/vehicle | Vehicle chip and Raahi check read from the profile; a missing vehicle links to `/profile/edit` |
| M13 city master | City list comes from `GET /me/route/cities`; "My city is not listed" is enabled and delegates |
| Design tokens | Use UI Plan §3 values; do not hard-code colours in screens |
| Admin Under Review | The Under Review banner and disabled Edit are ready before M12 exists (status only appears when set) |

---

## 11. Delivery plan (M2 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Confirm tokens; build/reuse C-9 point input, C-10 tabs, C-30 stepper, C-7 days, C-8 time, C-23 dialog, C-24 sheet | UI Plan §3 |
| 2 | App shell **Route** tab + routes `/route`, `/route/edit`, `/route/review`, `/route/history` | Shell/navigation |
| 3 | My Route all states (no route, Draft, Active, Inactive, Under Review, Invalid) against `GET /me/route` | API 1 |
| 4 | Stepper shell + Cities step + city list/search + city request sheet | API 2, 3, 11 |
| 5 | Going leg: points add/reorder/remove, live validation, days, time, add-stop limit | API 6, 7 |
| 6 | Return leg: Copy reversed, skip (CFG-19), remove Return, time warning | API 6 |
| 7 | Review & Activate: checklist from server, fix links, activate, Active-edit variant | API 8, 9 |
| 8 | Deactivate dialog + Reactivate + Active-edit banner | API 1, 10 |
| 9 | Route history list with Show more + compact route line | API 12 |
| 10 | Responsive desktop (left-rail stepper, side-by-side Review) | — |
| 11 | Accessibility pass (labels, focus, targets, point reorder announcements) | — |
| 12 | QA: states, network drops, 200% zoom, unsaved-changes prompt, copy-reversed edge cases | API running |
| 13 | Replace placeholders when M3/M4/M13 land | M3/M4/M13 |

Suggested route files (framework-agnostic naming): `route/index`, `route/edit`, `route/review`, `route/history`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-11.1 | Add-stop disappears at the limit; a sixth point cannot be added; the note is shown |
| AC-11.2 | Activation with a missing End point is blocked; the End field is focused/highlighted on the Going tab |
| AC-11.3 | Activating a second route shows the "switch off first" message with a link; no silent retry |
| AC-11.4 | With CFG-19 = No, a complete Going-only route can be activated from Review |
| AC-11.5 | "Copy reversed" produces reversed points, same days, empty departure time |
| AC-11.6 | Deactivate dialog states leads/requests withdrawn and connections/chats kept; after success the member stays logged in |
| AC-11.7 | Start "Nehru Place" + End "nehru place" produces the same-place error naming both |
| AC-11.8 | Missing city blocks activation and links back to the Cities step |
| AC-11.9 | An inactive city never appears in the dropdown or search |
| BR-11.5 | Phone numbers, links and illegal characters are rejected inline with the exact copy |
| BR-11.10 | Return time earlier than Going shows a note, not a block |
| BR-11.13 | Editing an Active route shows the version banner; Review says pending requests/connections are unaffected |
| BR-11.14 | Deactivation only affects leads/requests; chats remain |
| BR-11.16 | History shows past versions read-only; no delete anywhere |
| UP-5 | Destructive actions state effects before acting |
| UP-8 | Guided stepper + Copy reversed + review step are present |
| UP-10 | Loading, empty, error, success, disabled, offline, Under Review and Invalid states are implemented |
| UP-11 | Contrast, focus, labels, 44 px targets, keyboard reorder and SR announcements pass |
| — | Review shows the checklist and a Fix link per failed item |
| — | Role chip updates and route summary refresh after activation/deactivation |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| — | Show the route line on My Route when the route is Draft? | Show a muted preview with a "Draft" badge, not the live match view |
| — | Where does "Switch on" (reactivate) lead? | Review & Activate (BR-11.15 requires review again) |
| — | Does removing a Return need its own confirm? | Only when the route is Active; a small confirm noting Return-only leads are withdrawn |
| — | Stepper on phone: horizontal dots vs labelled steps? | Labelled horizontal stepper with short labels; scroll only if needed |
| — | City selector on desktop: searchable custom list vs native select? | Searchable list when > 20 cities, else native select |
| — | Show "active since" for Draft/Inactive? | Only Active; otherwise show last activated/deactivated date |
| — | Keep "See leads" CTA before M4 exists? | Hide until M4; replace with "Back to home" |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-11.1…11.4 | §2 routes, §4 screens |
| BR-11.1 one Active | §4.1 Inactive/Active states, §8 copy |
| BR-11.2 one Draft | §4.1 Draft state, §4.2 resume mode |
| BR-11.3 Start/End, 2–5 points | §4.2 points, §4.8 |
| BR-11.4 free text | §4.2 (no suggestions) |
| BR-11.5 point rules | §4.2/4.8, §8 microcopy |
| BR-11.6 duplicate places | §4.2 live check, §4.8 |
| BR-11.7 order | §4.8 reorder |
| BR-11.8 days/time | §4.2 Going tab |
| BR-11.9 Return optional/required | §4.2 Step 3, §4.6 |
| BR-11.10 Return-time warning | §4.2 Step 3 |
| BR-11.11 Draft | §4.1, §4.2 |
| BR-11.12 activation | §4.2 Step 4 |
| BR-11.13 Active edit | §4.5 banner, §4.2 Step 4 variant |
| BR-11.14 deactivation | §4.4 dialog |
| BR-11.15 reactivation | §4.1 Inactive state, §13 |
| BR-11.16 no hard delete | §4.3 history |
| BR-11.17 fixed type | §4.6 role chip read-only |
| BR-11.19 Raahi vehicle | §4.6 |
| BR-11.20 cities | §4.2 Step 1, §4.7 |
| AC-11.1…11.9 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §6.3 W-3.1…W-3.6 | §4 |
| UI Plan §7 state patterns | §5, §6 |
| UI Plan §8 delivery | §11 |
