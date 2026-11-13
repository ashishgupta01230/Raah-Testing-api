# M14 — Dashboards — UI Plan

| | |
|---|---|
| **Module** | M14 — Dashboards (spec `docs/spec/14_M14_Dashboards.md`, functional specification Section 23) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4.1–§4.2, gates §4.3, flow F-1/F-8, module design §6.14 (W-14.1, W-14.2), cross-module patterns §7, delivery §8 |
| **Platform** | **Member dashboard phone first** (360 px design); **Admin dashboard desktop first** (1024 px+), still usable down to phone (D-20) |
| **Companion** | API plan: `14_M14_Dashboards_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M14 is the **first screen after login** for both roles (BR-5.11). The member version answers "what needs me today?"; the Admin version answers "is the platform healthy, and what needs me first?" (F-8).

1. **SCR-23.1 Member Dashboard** (`/`) — setup prompts, My Route, new leads, requests received, requests sent, connections, latest notifications, safety links (W-14.1).
2. **SCR-23.2 Admin Dashboard** (`/admin/dashboard`) — period selector, alert strip, Members/Routes/Matching/Connections/Safety tiles, Moderation row and quick links (W-14.2).

UI jobs for this module:

- **One glance, one decision.** Every block leads to an action: create a route, connect, answer a request, withdraw, open a chat, open a report (UP-1, UP-12).
- **Live numbers only.** No editable figure, no placeholder count, no "approximately" (BR-23.2). Each figure states whether it is "now" or "in the period".
- **Every Admin number drills down.** A tile is a link, not decoration; sub-numbers open the filtered list behind them (BR-23.3).
- **Nothing surprising.** Destructive or irreversible actions are not performed from the dashboard; the dashboard navigates to the screen that owns the confirmation (UP-5).
- **Privacy on the member dashboard.** Other members appear only as the public view (name, photo, gender, vehicle, verified, rating, route). Never email, mobile or chat content (UP-4, BR-23.4).
- **Prompts, not blockers.** Setup prompts disappear the moment the condition is met and never prevent the rest of the dashboard from working (BR-23.5).
- **Empty is designed.** Every block has an empty state, not an error (BR-23.6, UP-10).
- **Role-aware wording.** A Saathi sees "Raahis for you"; a Raahi sees "Saathis for you".

Out of scope for M14 screens (linked, owned elsewhere): route editing (M2), lead detail/connect sheet (M4), request actions (M5), chat (M6), rating (M7), block/report forms (M8), notifications screen (M9), member list/detail (M10), route monitor/matching health (M11), safety queues/moderation (M12), configuration/legal/cities (M13), report catalogue (M15). M14 only **links** to these screens; it does not reimplement their dialogs or confirmations.

---

## 2. Navigation and routes

### 2.1 Member

The dashboard is the **Home** tab in the member shell (UI Plan §4.1). The top bar shows the RAAH mark, the journey-role chip and the notification bell with an unread count.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Member Dashboard | `/` (alias `/home`) | SCR-23.1 / W-14.1 | Home tab; post-login landing (BR-5.11) |
| My Route | `/route` | SCR-11.1 | My Route card; setup prompt "Create your route" |
| Route edit / Return leg | `/route/edit` | SCR-11.2/11.3 | "Edit", "Add Return leg" |
| Leads | `/leads` | SCR-13.1 | New leads "See all"; bottom Leads tab |
| Requests | `/leads?tab=requests` | SCR-14.2/14.3 | Requests received/sent blocks |
| Chats | `/chats` | SCR-15.1 | My Connections block; unread line |
| Notifications | `/notifications` | SCR-18.1 | Notification block; top-bar bell |
| Blocked members | `/blocked` | SCR-17.2 | Safety strip |
| My reports | `/reports` | SCR-17.4 | Safety strip |
| Verify email | `/verify-email` | SCR-5.2 | Verify prompt; unverified gate |
| Profile edit | `/profile/edit` | SCR-10.2 | Add-photo prompt |

**Gates that override the dashboard:**

- **Unverified:** only the Verify-email prompt with a Resend button; route and lead blocks are locked/empty (SC-23.6, BR-5.1).
- **Legal re-acceptance pending:** the legal gate replaces every page until accepted (BR-22.16).
- **Restricted:** an amber banner on Leads/Requests says requests are blocked until the date; the dashboard otherwise works (BR-21.6).
- **Session expired / offline:** Foundation dialog/banner; the dashboard shows its last state if available and disables actions that need the network.

### 2.2 Admin

The Admin dashboard is the first item in the Admin sidebar (UI Plan §4.2). The top bar keeps global search, the notification bell and the Admin menu.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Admin Dashboard | `/admin/dashboard` (alias `/admin`) | SCR-23.2 / W-14.2 | Sidebar "Dashboard"; post-login landing |
| Member list | `/admin/members` | SCR-19.1 | Members tile and alert drill-downs |
| Route monitor | `/admin/routes` | SCR-20.1 | Routes tile and Invalid-route alert |
| Matching Health | `/admin/matching-health` | SCR-20.5 | Matching tile |
| Lead monitor | `/admin/leads` | SCR-20.3 | Matching lead counts |
| Connection monitor | `/admin/connections` | SCR-20.4 | Connections tile |
| Report queue | `/admin/safety` | SCR-21.1 | Safety tile; overdue alert |
| Restricted and Blocked | `/admin/restricted` | SCR-21.4 | Safety tile |
| Flagged members | `/admin/flagged` | SCR-21.5 | Flagged tile; threshold alert |
| Rating moderation | `/admin/rating-moderation` | SCR-21.6 | Moderation row |
| Configuration | `/admin/config` | SCR-22.1 | Quick link |
| Legal Content | `/admin/legal` | SCR-22.4 | Quick link; sample alert |
| Cities | `/admin/cities` | SCR-22.5 | Quick link |

**Hard route rules:** there is no dashboard edit route, no "add number" control, no dashboard delete/export, and no Admin action that mutates a record from this screen. Every action opens the owning screen with its confirmation.

---

## 3. Screen inventory and components

| Screen / element | Components used (UI Plan §3.6) |
|---|---|
| Member header | App shell, role chip (C-12), avatar (C-13), notification bell with unread count |
| Setup prompt | C-21 setup-prompt banner (info/warning variant), C-1 button |
| My Route card | C-12 status badge, role/vehicle chips, C-15 compact route line, C-1 Edit/Deactivate/Reactivate |
| New leads | C-16 compact lead card, C-12 match-type badge, C-13 avatar, C-14 rating stars, C-35 "See all" link |
| Requests received | C-17 request card, C-1 Accept/Reject, C-12 status/expiry chip |
| Requests sent | C-17 request card (sent variant), C-1 Withdraw |
| My Connections | C-18 conversation row summary, C-12 unread badge |
| Notifications | C-20 notification item, unread dot, C-35 "See all" |
| Safety strip | Text links with icons (Blocked members, My reports) |
| Bottom tabs | Shell tab bar with counts on Leads and Chats |
| Admin period selector | C-11 segmented control (desktop); C-4 select (phone) |
| Admin alert strip | C-21 warning/danger/info banners, one-line count, Open action |
| Admin tiles | C-29 stat tile: primary number, label, sub-metrics, drill-down cursor; accessible as a link |
| Admin Moderation | C-29 flat row/tile |
| Admin quick links | C-1 tertiary links or link cards |
| All loading states | C-26 skeleton |
| All empty states | C-25 empty state |
| All errors | Inline block banner + Try again; C-22 toast for a refresh result |

Feature components to build for M14 only:

- `SetupPromptBanner` — one prompt at a time with a single action.
- `MemberRouteSummaryCard` — compact My Route with status-aware actions.
- `DashboardBlock` — shared wrapper providing title, count, empty state, skeleton, error and a drill-down header.
- `AdminStatTile` — primary number + clickable sub-metrics + drill-down.
- `AdminAlertStrip` — ordered alerts with severity, count and Open action.
- `PeriodSelector` — segmented on desktop, select on phone.

Reuse from M4/M5/M9/M10–M13: lead card, request card, notification item, status badges, route line, empty state and skeleton. Do not build a generic dashboard framework.

---

## 4. Screen designs

### 4.1 Member Dashboard — `/` (W-14.1)

```text
┌────────────────────────────────────────┐
│  RAAH   [Saathi]                  (bell) 2 │
│  ┌──────────────────────────────────┐  │
│  │ (!) Create your route to find    │  │  ← one setup prompt at a time
│  │ people who travel your way.      │  │
│  │ [ Create route ]                 │  │
│  └──────────────────────────────────┘  │
│  My route                   [Active]   │
│  Delhi → Noida · Mon–Fri               │
│  Going 8:00 AM   Return 6:30 PM        │
│  [ Edit route ]        [ Switch off ]  │
│  Raahis for you             See all >  │
│  5 new (3 Going, 2 Return)             │
│  (Av) Neha S.  [Full Route] [Full Route]│
│       Tue–Thu · 7:50 AM · 10 min apart │
│  (Av) Rohit M. [Useful Segment]        │
│  [ Connect ]            Not interested │
│  Requests received (1)                 │
│  Amit R. · expires in 4 days           │
│  [ Accept ]  [ Reject ]                │
│  Requests sent (1) · waiting 6 days    │
│  [ Withdraw ]                          │
│  My connections                        │
│  2 connected · 1 chat unread    Open > │
│  Notifications                 See all >│
│  (dot) Amit accepted your request · 2h  │
│  Safety    Blocked members · My reports │
├────────────────────────────────────────┤
│ Home   Route   Leads(6)  Chats(1)   Me │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Block order | Fixed: setup prompt(s) → My Route → New leads → Requests received → Requests sent → My Connections → Notifications → Safety. Order never changes between loads |
| Setup prompts | One prompt at a time, highest priority first: Verify email → Create route → Add Return leg → Add photo. It disappears as soon as the condition is met (BR-23.5) |
| Unverified | Only the Verify-email prompt is shown, with `Resend link` and the resend cooldown; route/lead/request blocks are not rendered (SC-23.6) |
| My Route | Status badge, journey role, Start → End, Going days/time, Return days/time, vehicle for a Raahi. Actions depend on status (§4.2) |
| New leads | Role-aware heading; count split Going/Return; top three leads with match badges per leg, days, both times and gap; primary Connect; "Not interested" (dismiss) with Undo; "See all" opens `/leads` |
| Requests received | Up to 5 pending requests: sender public view, `expires in N days`, Accept / Reject. Accept opens the chat; Reject asks for confirmation only in the sheet (owned by M5) |
| Requests sent | Up to 5 pending requests: receiver, `waiting N days`, Withdraw. Withdraw asks for confirmation (owned by M5) |
| My Connections | Accepted count and unread conversation/message counts; opens `/chats` |
| Notifications | Latest five one-line items with unread dots; "See all" opens `/notifications`. No chat content |
| Safety | Two plain links, always present: Blocked members and My reports |
| Refresh | Pull-to-refresh on phone; a refresh keeps the page usable and replaces block data. No auto-refresh that moves content while the member is reading |
| Loading | Skeletons matching the block shapes (route card, two lead cards, request rows, notification rows); the header stays stable |
| Error | A block-level inline message, not a full-page error: "Could not load this section." + Try again. Other blocks remain usable (BR-23.6) |

### 4.2 Member block states

**My Route** (status-aware):

| Route state | Card shows | Actions |
|---|---|---|
| No route | Empty card: "No route yet." + "Create your route to find people who travel your way." | `Create route` |
| Draft | Draft badge, setup progress hint | `Continue setting up` |
| Active | Full summary | `Edit route` · `Switch off` (M2 confirmation) |
| Inactive | Inactive badge, last summary | `Switch on` |
| Under Review | Amber note "This route is under review. You cannot change it for now." | `View route` only |
| Invalid | Red note naming the broken rule | `Fix route` |

**New leads**:

| Situation | Block |
|---|---|
| No route | Empty state "Activate your route to see leads." with `Create route`/`Activate route` |
| Route Inactive | Empty state "Your route is off. Switch it on to see new leads." with `Switch on` |
| No matches yet | "No relevant person found yet. We will tell you when someone matches. You can add a Return leg or check your points and times." with `Edit route` and `Add Return leg` (when applicable) |
| Leads exist | Count split + top three + See all |
| Restricted | Amber banner above the leads: "You cannot send new requests until {date}." Connect buttons disabled with the reason nearby |
| Pending limit reached | Connect disabled with "You have too many pending requests. Wait for answers or withdraw one." |

**Requests received / sent / connections / notifications**: each has a one-sentence empty state and, where useful, one next action.

| Block | Empty copy | Next action |
|---|---|---|
| Requests received | "No requests waiting for an answer." | `Browse leads` |
| Requests sent | "No requests sent yet." | `Find people` |
| Connections | "No connections yet. Accept a request or send one from a lead." | `Browse leads` |
| Notifications | "No notifications yet." | — |
| Safety | Always shown; never empty | — |

**Lead and request card privacy.** Only the public view is rendered. No email, mobile, chat text or platform totals appear anywhere on this screen (BR-23.4, UP-4).

**Role-aware wording.**

| Member role | New-leads heading | Lead explanation |
|---|---|---|
| Saathi | "Raahis for you" | "This Raahi covers your whole route" / "…shares part of your route" |
| Raahi | "Saathis for you" | "Your route covers this Saathi's whole route" / "You share part of this Saathi's route" |

### 4.3 Scenario checks (member)

| Scenario | What the screen must show |
|---|---|
| **SC-23.1** New member verifies and logs in | "Create your route" prompt; My Route empty; New Leads empty; Requests and Connections empty; Notifications empty. No error styling |
| **SC-23.2** Saathi with leads and a request | "5 new leads (3 Going, 2 Return)"; top three with Connect; one request "expires in 4 days" with Accept/Reject |
| **SC-23.3** Raahi deactivates | My Route shows Inactive + `Switch on`; New Leads empty state; connections and unread messages still visible |
| **SC-23.6** Unverified | Only "Verify your email" + Resend; no route or lead blocks; the rest of the app is gated by Foundation |

---

### 4.4 Admin Dashboard — `/admin/dashboard` (W-14.2)

```text
┌────────────┬─────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Dashboard                     Period [Today][7d][30d]  ⟳  │
│ Dashboard◄ │  Updated 9:10 AM · counts marked "in period" use the filter  │
│ Members    │  (!) Legal text is still the sample            [ Open ]      │
│ Routes     │  (!) 2 reports are overdue                     [ Open ]      │
│ Safety     │  (!) 3 routes are invalid                      [ Open ]      │
│ Settings   │ ┌────────┐┌────────┐┌────────┐┌────────┐┌────────┐          │
│ Reports    │ │Members ││Routes  ││Matching││Connect.││Safety  │          │
│            │ │ 1,204  ││ 850    ││ 2,310  ││  25    ││Open 7  │          │
│            │ │Active  ││Saathi  ││Leads   ││Pending ││Overdue │          │
│            │ │ 1,118  ││ 540    ││ in per.││ 25     ││ 2      │          │
│            │ │Unverif.││Raahi   ││Full 58%││Accept. ││Flagged │          │
│            │ │ 42     ││ 310    ││No-lead ││ 58%    ││ 1      │          │
│            │ │New 36  ││Return  ││ 30     ││        ││Restr.  │          │
│            │ │        ││ 420    ││L→R 12% ││        ││ 4      │          │
│            │ └────────┘└────────┘└────────┘└────────┘└────────┘          │
│            │  Moderation: 3 rating comments awaiting review   [ Open ]  │
│            │  Quick links: Configuration · Notifications · Legal · Cities│
└────────────┴─────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Period selector | Segmented `Today · Last 7 days · Last 30 days` on desktop; a select on phone. Changing it refetches **activity** counts only; stock counts keep their values and stay labelled "now" |
| Scope labels | Every activity metric carries "in period" (or a small `in period` caption). Stock metrics carry "now". The header states the active period and the update time |
| Tiles | Five tiles: Members, Routes, Matching, Connections, Safety. Each tile has one primary number and 2–4 sub-metrics. Every number is a link to its filtered list (BR-23.3) |
| Tile order | Fixed: Members → Routes → Matching → Connections → Safety. Matching tile is emphasised (the product's core health) but not larger than the others |
| Alert strip | Sits above the tiles; ordered Legal sample → Overdue → Invalid routes → Report threshold → Failed-login spike → Locked accounts. See §4.5 |
| Moderation | A single dense row: "3 rating comments awaiting review" + Open. Count is a link to Rating Moderation |
| Quick links | Configuration · Notifications settings · Legal Content · Cities. Text links, keyboard reachable |
| Refresh | A visible refresh control and "Updated {time}". Manual refresh is the default; an optional 60 s poll may refresh the **alert strip only** (UD-UI-4) |
| Empty data | A zero is shown as `0`, not a blank or dash. A block with no records shows the tile with zeros and a neutral "No activity in this period" caption where useful |
| Partial failure | The affected tile shows "Could not load this section." + Try again; the other tiles and alerts remain. Never substitute a fake zero for a failed tile |
| Data definitions | An `(i)` on Overdue, Flagged and Acceptance rate opens a tooltip with the live CFG-32/CFG-15 value and the formula (from the options endpoint) |
| Privacy | Aggregate-only. No member name, email, mobile, chat text or report description on this screen (BR-23.4) |
| Loading | Tile skeletons in the same grid; alert skeletons as slim bars; header and period selector stay stable |
| Error (all sources) | Full-page inline error "The dashboard is not available right now." + Try again. No blank white screen |
| No access | Member token or non-Admin user gets the generic no-access page, never a hint about the records |

### 4.5 Admin alert strip

```text
(!) Legal text is still the sample. Replace and publish final text
    before real members join.                          [ Open ]
(!) 2 reports are overdue. Reports past 3 days need a decision. [ Open ]
(!) 3 routes are invalid. Fix them to keep matching healthy.    [ Open ]
(!) 1 member is at the report threshold (3 reports).            [ Open ]
(!) 4 accounts had a failed-login spike in 24 hours.            [ Open ]
(i) 2 accounts are locked.                                      [ Open ]
```

| Aspect | Design |
|---|---|
| Order | Legal sample (amber) first, then Danger (overdue), then Warning (invalid routes, threshold, failed-login), then Info (locked). Never reorder randomly between refreshes |
| Count | Shown inline, in numerals: "2 reports are overdue." No count for the legal-sample flag |
| Action | One `Open` per alert; opens the relevant filtered list or Legal Content |
| Zero alerts | The strip disappears entirely; no empty container |
| Not dismissible | Alerts are not dismissed; they clear when the condition clears (BR-23.2, BR-22.13) |
| Overflow | Show the first three; a "Show all {n} alerts" disclosure reveals the rest (UD-UI-8) |
| Accessibility | Each alert is a list item with an icon, a sentence and a link; severity is in words, not colour alone |

### 4.6 Admin tile details and drill-downs

| Tile | Primary number | Sub-metrics (all clickable) | Drill-down |
|---|---|---|---|
| **Members** | Total members | Active · Unverified · New in period · Inactive · Restricted · Temporarily Blocked · Permanently Blocked (show the first 4; the rest in a "More" disclosure) | `/admin/members` with the matching status/verification/date filter |
| **Routes** | Active routes (Saathi + Raahi) | Active Saathi · Active Raahi · With Return leg · Invalid · Under Review · Active by city (top 5) | `/admin/routes` with status/role/hasReturn/city filters |
| **Matching** | Leads in period | Full Route % · Useful Segment % · Active routes with no leads · Lead → request rate | `/admin/leads` (period), `/admin/matching-health` (no-lead, conversion) |
| **Connections** | Pending requests | Accepted · Rejected · Expired · Withdrawn · Disconnected · Acceptance rate | `/admin/connections` with status filter; `/admin/matching-health` for the rate |
| **Safety** | Open reports | Overdue · Flagged members · Restricted · Blocked | `/admin/safety`, `/admin/flagged`, `/admin/restricted` |
| **Moderation** | Rating comments awaiting review | — | `/admin/rating-moderation` |

Tile interaction:

- The whole tile title and primary number are a link; each sub-number is its own link. On hover/focus the block shows a subtle tint and pointer; focus ring is visible (UP-11).
- On phone, tiles stack one per row; the sub-metrics wrap as a two-column mini-list.
- The drill-down preserves the period where the metric is period-scoped (for example a "leads in period" link carries the date range).
- The dashboard never opens a record in a modal; it navigates to the owning list so filters and context stay intact.

### 4.7 Admin dashboard states

| State | Design |
|---|---|
| Loading | Tile and alert skeletons; period selector and header stable; no bare spinner |
| Empty platform | All tiles show `0`; a neutral line "No activity yet." where appropriate; quick links remain |
| Period with no activity | Activity sub-metrics show `0` and "in period"; stock metrics keep their real values |
| Partial failure | The block shows an inline message + Try again; other blocks render (BR-23.6) |
| All sources fail | Full-page inline error + Try again; no fake data |
| Legal sample current | Amber alert and a link to Legal Content; mirrors M13 exactly (BR-22.13) |
| Overdue reports | Danger alert; the Safety tile's Overdue sub-number is highlighted; both open `/admin/safety?overdue=true` |
| Failed-login spike | Warning alert with the 24-hour window in the copy |
| Offline | Slim banner; refresh disabled with a reason; the last loaded values may remain but are labelled stale |
| Session expired | Foundation dialog; return to login, keep no stale counts visible after logout |
| No access | Generic no-access page; no record existence hint |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeletons matching the final layout (C-26); header, tabs and period selector stable |
| Empty | Per-block C-25 with one sentence and one next action; never an error style |
| Error (block) | Inline banner + Try again; other blocks stay usable |
| Error (page) | Inline page banner + Try again; no blank page |
| Success (refresh) | Values update; "Updated {time}" changes; no toast unless the member triggered a manual refresh and it failed |
| Disabled | Connect/Accept/etc. disabled with a visible reason nearby; never a silent disabled control |
| No access | "You do not have access to this record." (Admin); no existence hint |
| Unverified | Only the verify prompt; route/lead blocks absent, not broken |
| Restricted | Amber banner with the date; other functions work |
| Inactive route | My Route shows Inactive + Switch on; leads empty; connections/unread still visible (SC-23.3) |
| Offline | Slim banner; actions that need the network disabled with a reason |
| Session expired | Foundation dialog; return to login (BR-5.6) |
| Legal gate pending | Legal gate replaces the dashboard until accepted (BR-22.16) |
| Partial failure (Admin) | Per-tile message; no fake zero; refresh available |
| Stale data | "Updated {time}" visible; no silent stale counts after logout |

---

## 6. Responsive rules

| Breakpoint | Member | Admin |
|---|---|---|
| Phone 360–479 | Single column; 16 px margins; cards 12 px apart; bottom tabs; setup prompt full width; lead cards compact; Accept/Reject side by side full-width targets | One tile per row; period becomes a select; alert strip stacks; sub-metrics two per row; quick links wrap; moderation row full width |
| Large phone 480–767 | Same as phone with slightly wider cards; lead card may show match badges on one line | Tiles two per row; alert strip two-line; Moderation row full width |
| Tablet 768–1023 | Member content capped; two-column block grid where it helps (Requests received/sent side by side) | Tiles three per row; sidebar becomes a rail; alert strip full width; quick links inline |
| Desktop 1024–1279 | Member shell becomes left sidebar; content max 1120 px; dashboard blocks can use two columns | Full sidebar; five tiles in one row if space allows, otherwise 3+2; alert strip above; Moderation row and quick links below |
| Wide 1280+ | Member content stays capped; no stretched cards | Content fluid with a 1440 px cap; tiles keep equal widths; no oversized empty gaps |

- The member dashboard is **phone first**: the first viewport must show the setup prompt (if any), My Route, and the first lead.
- The Admin dashboard is **desktop first** but every tile and alert must remain reachable on a phone without horizontal page scroll; tables only exist behind drill-downs.
- Sticky header on the Admin page keeps the period selector visible while scrolling tiles.
- At 200% text zoom the member page remains one column; Admin tiles reflow to fewer per row and never force page-level horizontal scroll.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2; alert colours are paired with icons and words |
| Focus | Visible 2 px Indigo focus ring; tile links and sub-metric links each have a clear focus state |
| Labels | Every tile has a text label; a number is never read alone. Accessible name example: "Open reports: 7. View report queue" |
| Targets | ≥ 44 × 44 px; sub-metric links have at least 44 px height on phone; 8 px apart |
| Colour | Status, match type, alert severity and unread states always carry text/icons; never colour alone |
| Keyboard | Period selector, refresh, every tile and sub-metric, every alert Open, quick links and safety links work by keyboard in visual order |
| Screen readers | The member dashboard announces block headings in order; setup prompt is announced once on appearance; unread counts are in the accessible name; the Admin page announces the active period, update time and alert count |
| Live regions | A manual refresh announces "Dashboard updated at {time}" politely; a failed refresh announces the error politely |
| Motion | Honour "reduce motion"; no animated counters, no auto-scrolling |
| Zoom | 200% text without horizontal page scroll; Admin tiles reflow |
| Language | `lang="en"`; dates `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M14)

| Situation | Copy |
|---|---|
| Page title (member) | "Home" (the RAAH mark remains in the header) |
| Page title (Admin) | "Dashboard" |
| Setup — verify email | "Verify your email to continue. Check your inbox for the link." · button "Resend link" |
| Setup — create route | "Create your route to find people who travel your way." · button "Create route" |
| Setup — add Return | "Add your Return leg to find more people." · button "Add Return leg" |
| Setup — add photo | "Add a photo so members recognise you." · button "Add photo" |
| My Route — no route | "No route yet." · "Create your route to find people who travel your way." |
| My Route — under review | "This route is under review. You cannot change it for now." |
| My Route — invalid | "This route needs a fix before it can match." |
| New leads — Saathi | "Raahis for you" |
| New leads — Raahi | "Saathis for you" |
| New leads — count | "{n} new ({g} Going, {r} Return)" |
| New leads — no results | "No relevant person found yet. We will tell you when someone matches. You can add a Return leg or check your points and times." |
| New leads — no route | "Activate your route to see leads." |
| New leads — inactive | "Your route is off. Switch it on to see new leads." |
| New leads — restricted | "You cannot send new requests until {date}." |
| New leads — pending limit | "You have too many pending requests. Wait for answers or withdraw one." |
| Requests received — empty | "No requests waiting for an answer." |
| Requests sent — empty | "No requests sent yet." |
| Requests — expiry | "Expires in {n} days" · "Expires today" |
| Requests — waiting | "Waiting {n} days" |
| Connections — empty | "No connections yet. Accept a request or send one from a lead." |
| Connections — summary | "{n} connected · {u} chat unread" |
| Notifications — empty | "No notifications yet." |
| Safety strip | "Blocked members · My reports" |
| Block error | "Could not load this section." · "Try again" |
| Page error | "The dashboard is not available right now." · "Try again" |
| Refresh success | "Dashboard updated at {time}." |
| Period labels | "Today" · "Last 7 days" · "Last 30 days" |
| Period header | "Showing activity for {period}. Counts marked “in period” use this filter." |
| Updated line | "Updated {time}" |
| Members tile | "Members" · "Total" · "Active" · "Unverified" · "New in period" · "Inactive" · "Restricted" · "Temporarily blocked" · "Permanently blocked" |
| Routes tile | "Routes" · "Active Saathi" · "Active Raahi" · "With Return leg" · "Invalid" · "Under review" · "Active by city" |
| Matching tile | "Matching" · "Leads in period" · "Full Route" · "Useful Segment" · "Active routes with no leads" · "Lead → request" |
| Connections tile | "Connections" · "Pending" · "Accepted" · "Rejected" · "Expired" · "Withdrawn" · "Disconnected" · "Acceptance rate" |
| Safety tile | "Safety" · "Open reports" · "Overdue" · "Flagged" · "Restricted" · "Blocked" |
| Moderation row | "{n} rating comments awaiting review" |
| Alert — legal | "Legal text is still the sample. Replace and publish final text before real members join." |
| Alert — overdue | "{n} reports are overdue. Reports past {CFG-32} days need a decision." |
| Alert — invalid routes | "{n} routes are invalid. Fix them to keep matching healthy." |
| Alert — threshold | "{n} member(s) at the report threshold ({CFG-15} reports)." |
| Alert — failed logins | "{n} accounts had a failed-login spike in 24 hours." |
| Alert — locked | "{n} accounts are locked." |
| Alert action | "Open" |
| More alerts | "Show all {n} alerts" |
| Quick links | "Configuration" · "Notifications settings" · "Legal Content" · "Cities" |
| Definition — overdue | "A report still open after {CFG-32} days." |
| Definition — flagged | "A member reported by {CFG-15} different members." |
| Definition — acceptance | "Accepted ÷ (accepted + rejected + expired + withdrawn) for connections in the period." |
| No access | "You do not have access to this record." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, numbers as digits, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load member dashboard | `GET /me/dashboard` |
| Refresh setup prompts after an action | `GET /me/dashboard/prompts` |
| Resend verification | Existing Foundation verification-resend endpoint (not M14) |
| Open My Route | Navigate `/route` (M2 read) |
| Open all leads | Navigate `/leads` (M4) |
| Connect / dismiss a lead | M4 `POST /leads/{leadId}/connect` / `/dismiss` |
| Accept / reject a request | M5 `POST /connections/{connectionId}/accept` / `/reject` |
| Withdraw a sent request | M5 `POST /connections/{connectionId}/withdraw` |
| Open chats / unread | Navigate `/chats` (M6) |
| Open notifications | Navigate `/notifications` (M9) |
| Load Admin dashboard | `GET /admin/dashboard?period={Today|Last7Days|Last30Days}&blocks=` |
| Refresh one tile | `GET /admin/dashboard?period=&blocks={block}` |
| Load / poll alerts | `GET /admin/dashboard/alerts` |
| Load periods, quick links, definitions | `GET /admin/dashboard/options` |
| Open a tile drill-down | Navigate to the route/query in the tile's `drillDown` (M10–M12) |
| Open Moderation | Navigate `/admin/rating-moderation` (M12) |

Client rules:

- Always send the token; the member id is never sent from the client.
- Use the `drillDown` object returned by the API; do not build filter URLs in the UI.
- On `dataWarnings[]`, show the block-level message and keep the rest of the page.
- On `403` → generic no-access; on `503` → page-level error + Try again; never guess.
- Never display a number as a link unless the API returned a drill-down for it.
- Never show a count from a previous period after the selector changes; show the skeleton until the new data arrives.
- Dates use `09-Oct-2026`, times `8:05 AM`, India time.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M1 profile/photo/verification | Header uses initials and hides the photo prompt; verification prompt still works through Foundation |
| M2 current route | My Route shows "Route is not available yet." with Edit disabled; no fake Active badge |
| M4 new leads | New Leads shows a neutral "Leads are not available yet." with See all disabled |
| M5 requests/connections | Requests and Connections blocks show "Not available yet." and no action buttons |
| M6 unread summary | Connections shows the accepted count only; no unread line |
| M9 notifications | Notification block shows "Notifications are not available yet."; the bell still works if M9 is present |
| M10–M12 Admin stats | The affected tile shows "Could not load this section." + Try again; other tiles render |
| M13 legal sample flag | The sample alert is absent until M13 answers; no fake "sample" banner |
| M15 reports | Not linked from M14; drill-downs go to M10–M12 lists |
| Shared C-16/C-17/C-20/C-29 | Reuse from M4/M5/M9/M10–M12; build only the missing feature components |

The dashboard is never blocked by a missing module. A missing block is visibly unavailable, not zero and not an error page (BR-23.6).

---

## 11. Delivery plan (M14 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Member shell: header, role chip, bell, bottom tabs, Home route | API 1 |
| 2 | Setup prompt banner and prompt refresh after verify/create/photo | API 2 |
| 3 | My Route card with status-aware actions | M2 route data |
| 4 | New leads block: role-aware heading, counts, top three, empty/restricted states | M4 |
| 5 | Requests received/sent blocks and connection summary | M5/M6 |
| 6 | Notifications preview and safety strip | M9 |
| 7 | Member loading/error/empty states; pull-to-refresh | API running |
| 8 | Admin shell Dashboard entry + period selector | API 3, 5 |
| 9 | Members and Routes tiles with sub-metric links | API 3 |
| 10 | Matching and Connections tiles | API 3 |
| 11 | Safety and Moderation blocks | API 3 |
| 12 | Alert strip and its ordering; legal-sample alert | API 4 |
| 13 | Quick links and definitions tooltips | API 5 |
| 14 | Admin partial-failure, refresh and stale states | API running |
| 15 | Responsive pass: phone member, desktop Admin, tablet | — |
| 16 | Accessibility pass: tile link names, period selector, alert list, focus order | — |
| 17 | QA: role separation, live count refresh, overdue boundary, prompt disappearance, partial failure, 200% zoom, slow network | API running |

Suggested route files (framework-agnostic naming): `home/index` (member dashboard), `admin/dashboard/index`, plus components `SetupPromptBanner`, `MemberRouteSummaryCard`, `DashboardBlock`, `AdminStatTile`, `AdminAlertStrip`, `PeriodSelector`, `QuickLinks`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-23.1 | A logged-in member sees only their own data; no platform totals appear anywhere on `/` |
| AC-23.2 | After a new report is submitted, the Admin Open reports count increases on refresh and clicking it opens the report queue |
| AC-23.3 | With CFG-32 = 3, a 4-day-old undecided report is counted and shown as Overdue; the alert opens the filtered queue |
| AC-23.4 | A member with no route sees "Create your route"; the prompt disappears once a route exists |
| BR-23.1 | Member and Admin dashboards are separate surfaces; a member cannot see Admin blocks |
| BR-23.2 | No number can be typed or adjusted; every value refreshes from the records |
| BR-23.3 | Every Admin tile and sub-metric opens the list behind the number |
| BR-23.4 | No email, mobile, chat content or report text appears on either dashboard |
| BR-23.5 | Setup prompts disappear as soon as their condition is met and never block the rest of the page |
| BR-23.6 | Each block has a designed empty state, and a failed block shows a retry message without breaking the page |
| SC-23.1 | New member sees Create-route prompt and empty blocks |
| SC-23.2 | Saathi sees "5 new leads (3 Going, 2 Return)", top three Connect buttons and an expiring request |
| SC-23.3 | Deactivated Raahi sees Inactive + Switch on, empty leads and still sees connections/unread |
| SC-23.4 | Admin sees overdue reports, flagged member and moderation count, and drills into overdue first |
| SC-23.5 | Admin sees "30 Active routes with no leads" and drills into Matching Health |
| SC-23.6 | Unverified member sees only the verify prompt and Resend |
| UP-1 | Primary action on each block is reachable with one thumb on a phone |
| UP-4 | Other members show only the public view |
| UP-5 | Dashboard actions never perform an irreversible change without the owning screen's confirmation |
| UP-9 | Same status words/colours as the rest of the product (UI Plan §3.7) |
| UP-10 | Loading, empty, error, disabled, no-access, offline and partial-failure states are designed |
| UP-11 | Contrast, focus, labels, targets, keyboard and screen-reader announcements pass |
| UP-12 | Member page is calm and spacious; Admin page is dense with drill-downs |
| — | Dates use `09-Oct-2026`, times `8:05 AM`, India time |
| — | Period change updates activity counts only; stock counts stay labelled "now" |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| UD-UI-1 | Member dashboard path | `/` with `/home` alias; Home is the first bottom tab |
| UD-UI-2 | Setup prompts: one at a time or stacked? | One at a time, highest priority first; refetch after the action |
| UD-UI-3 | Can the Add-photo prompt be dismissed? | No in Phase 1; it disappears when a photo exists |
| UD-UI-4 | Admin auto-refresh? | Manual refresh by default; optional 60 s poll of the alert strip only |
| UD-UI-5 | Period control: segmented or select? | Segmented on desktop/tablet; select on phone |
| UD-UI-6 | Tile layout | 5 across on wide desktop, 3+2 or 2 per row on smaller screens; one per row on phone |
| UD-UI-7 | Sub-metrics: all visible or “More”? | Show the 4 most important; the rest behind a "More" disclosure to keep tiles readable |
| UD-UI-8 | Alert overflow | Show the first three; "Show all {n} alerts" reveals the rest |
| UD-UI-9 | Drill-down: navigate or sheet? | Navigate to the owning list; no dashboard modal |
| UD-UI-10 | Member notification preview | Latest five inline; "See all" navigates to `/notifications` |
| UD-UI-11 | Show rating on dashboard lead cards? | Yes, read-only public rating (or "New member") |
| UD-UI-12 | Admin dashboard alias | `/admin` may redirect to `/admin/dashboard` |
| UD-UI-13 | Quick links count | Configuration, Notifications settings, Legal Content, Cities |
| UD-UI-14 | "Lead → request" label | Keep the short label with an `(i)` tooltip explaining the formula |
| UD-UI-15 | Stale-data treatment | Keep the last values visible with the "Updated" line; after logout, clear all counts |
