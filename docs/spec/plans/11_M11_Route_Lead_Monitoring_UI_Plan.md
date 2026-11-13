# M11 — Route & Lead Monitoring — UI Plan

| | |
|---|---|
| **Module** | M11 — Route & Lead Monitoring (spec `docs/spec/11_M11_Route_Lead_Monitoring.md`, functional specification Section 20) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, Admin navigation §4.2, flows §5 (F-9), module design §6.11 (W-11.1…W-11.4), patterns §7 (tables §7.4, confirmations §7.3, states §7.1), delivery §8 |
| **Platform** | Admin screens **desktop first**, then tablet and phone (D-20). No member UI in this module |
| **Companion** | API plan: `11_M11_Route_Lead_Monitoring_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M11 is the Admin **route and matching observatory**. It answers one question: *is matching producing safe, useful connections?*

1. **SCR-20.1 Route Monitor** — every route, filterable, with the Invalid routes on top (W-11.1).
2. **SCR-20.2 Route Detail** — one route in full: both legs, versions, leads, connections, and the two safety actions (W-11.2).
3. **SCR-20.3 Lead Monitor** — leads by member or route, match types, conversion.
4. **SCR-20.4 Connection Monitor** — requests and connections by status, Reported flag, funnel, **no chat content** (W-11.4).
5. **SCR-20.5 Matching Health** — where matching works and where it does not (W-11.3).

UI jobs for this module:

- **Read-only by design.** No control anywhere to create, edit or delete a route, lead, request or connection, and no way to match two people by hand (BR-20.1, BR-4.6, AC-20.1). If there is no such control, SC-20.6 has no screen to find.
- **Show stability, not just numbers.** Figures are live from records; the UI never offers to type or adjust one (BR-20.6).
- **Explain why a route finds no one.** Invalid routes name the broken rule; unmatched stops show why the engine did not group them (BR-20.7, BR-20.5).
- **Two deliberate safety actions only.** Deactivate and Hold Under Review, both requiring a reason and both stating their effects first (BR-20.3, UP-5).
- **Never show chat.** Connection Monitor shows counts, statuses and dates; no message text anywhere (BR-20.4, AC-20.4).
- **Every state designed** — loading, empty, filtered-empty, error, no access, action loading, partial failure, success (UP-10).
- **Admin density, calm use.** Dense tables, keyboard-friendly, drill-down from every tile (UP-12, UI Plan §6.11).

Out of scope for M11 screens (linked, owned elsewhere): route create/edit (M2), config and Test Match (M13), report detail and releases (M12), member actions (M10), reports catalogue (M15).

---

## 2. Navigation and routes

M11 lives in the Admin sidebar group **Routes and matching** (UI Plan §4.2): **Route monitor · Leads · Connections · Matching Health**. The Admin top bar keeps global search, the notification bell and the Admin menu.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Route monitor | `/admin/routes` | SCR-20.1 | Sidebar "Route monitor"; dashboard tiles; global route search |
| Route detail | `/admin/routes/[id]` | SCR-20.2 | Row in the monitor; member detail Route tab (M10); Invalid banner |
| Lead monitor | `/admin/leads` | SCR-20.3 | Sidebar "Leads"; Matching Health tiles |
| Connection monitor | `/admin/connections` | SCR-20.4 | Sidebar "Connections"; dashboard connection tiles |
| Matching Health | `/admin/matching-health` | SCR-20.5 | Sidebar "Matching Health"; dashboard "active routes with no leads" |
| Route action dialog | modal on detail | BR-20.3 | Actions → Deactivate / Hold Under Review |

**Hard route rule:** there is no `/admin/routes/new`, no `/admin/routes/[id]/edit`, no delete route, and no "match" or "assign" route. If someone types one, show the generic no-access page rather than a form (AC-20.1, SC-20.6).

Admin gates (Foundation): Admin login only (D-19, BR-5.11); Inactive/Blocked Admin cannot log in (BR-5.4); the Admin UI is a separate bundle from the member app (plan §9.5).

---

## 3. Screen inventory and components

| Screen / element | Components used (UI Plan §3.6) |
|---|---|
| Route monitor | C-28 Data table (sort, column chooser, saved filters, export), C-27 Filter bar, C-2 Search field, C-4 Select, C-12 Status/type chips, C-25 Empty state, C-26 Skeleton, C-22 Toast, C-35 numbered pager, C-21 Invalid banner |
| Route detail | C-13 Avatar, C-12 badges, C-1 buttons, C-11 tabs, C-33 Key-value list, C-15 Route line (read-only), C-34 History/version timeline, C-29 Stat tiles, C-23 dialog trigger |
| Version timeline | C-34, C-33 |
| Lead monitor | C-28 table, C-27 filters, C-12 match-type chips (Full Route green, Useful Segment sky, No match slate), C-29 conversion tiles, C-26 skeleton |
| Connection monitor | C-28 table, C-27 filters, C-12 connection status chips + **Reported** flag, C-29 funnel tiles, C-21 info banner ("No chat content") |
| Matching Health | C-29 Stat tiles, simple bar rows (Match mix), C-28 tables (By city), C-25 empty state, C-26 skeleton, C-4 period/city selects |
| Route action dialog | C-23 Confirm dialog, C-2 Textarea (reason), C-21 warning banner, C-22 toast |

Build shared Admin components here if not yet created by M10: C-28, C-29, C-34. M11 adds **bar row** (a labelled horizontal bar that carries its number as text) as a small, feature-scoped component — not a global abstraction.

---

## 4. Screen designs

### 4.1 Route monitor — `/admin/routes` (W-11.1)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Route monitor                                         [Export ]  │
│ Dashboard  │  Search [route no. or member__________]                            │
│ Members ▸  │  Status[All▼] Type[All▼] City[All▼] Return[Any▼] Report[Any▼]      │
│ Routes ◄   │  Leads[Any▼] Point[contains_________] Changed[dd-mmm..dd-mmm]     │
│ Safety ▸   │  [ Filters (2) ]  [ Clear all ]                  [ Columns ▾ ]   │
│ Settings ▸ │ ┌ (!) 3 routes are Invalid. Fix is required by the member. ───────┐│
│ Reports    │ │ RTE-000418 · Active · Delhi → Noida · rule: "5 points max"      ││
│            │ └────────────────────────────────────────────────────────────────┘│
│            │ ┌──────────┬────────┬──────┬───────┬──────────┬────┬───┬───┬─────┐│
│            │ │Route no. │Member  │Type  │Status │Going     │Days│Lds│Req│Con  ││
│            │ ├──────────┼────────┼──────┼───────┼──────────┼────┼───┼───┼─────┤│
│            │ │RTE-000418│Ashish K│Saathi│[Active]│Delhi→Noida│M–F│ 4 │ 2 │ 1   ││
│            │ │RTE-000419│Neha S. │Raahi │[Under…]│Delhi→Gur…│M–F│ 0 │ 0 │ 0   ││
│            │ └──────────┴────────┴──────┴───────┴──────────┴────┴───┴───┴─────┘│
│            │  Showing 1–25 of 850           [ < 1 2 3 … 34 > ]                 │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Invalid banner | Routes marked **Invalid** sort to the **top** and the banner names the broken rule (BR-20.7). Copy: "{n} routes are Invalid. A member fix is required." with a "Show Invalid" filter action. Admin never fixes it |
| Search | One box over route number and member name/number; debounce 300 ms; clear icon |
| Filters | Status, Type, City (matches Start **or** End), Has Return leg, Has open report, Leads (None / 1–5 / More than 5), Point text contains, Last changed date range; active count + Clear all (§7.4) |
| Point filter | Uses the same fuzzy rule as matching; helper text "Enter a stop name. Spelling variants are grouped like matching." (BR-20.5) |
| Columns | Default: Route no., Member, Type, Status, Going summary, Return summary, Days, Times, Leads, Requests, Connections, Active since, Last edited. Column chooser persists per Admin (C-28) |
| Sort | Invalid-first default; also Last edited, Active since, Leads, Route number, Member name. Arrow in header |
| Status | Badge text + colour, exact UI Plan §3.7 words (Draft/Active/Inactive/Invalid/Under Review) |
| Row | Whole row clickable/keyboard-activatable → Route detail; visible focus. Invalid row keeps the red status badge and shows the rule name as a small second line under the status on narrow screens |
| Report flag | A small "Report" chip where the route's member has an open safety report; never a link to report text (M12 owns it) |
| Export | Opens the export dialog (§4.7); never exports silently; no contact details in the file (BR-20.2) |
| Bulk actions | **None.** No row checkboxes for actions (BR-20.1) |
| Pager | Numbered pager with total; 25/page |
| Empty (no routes) | "No routes yet." + "Routes appear here after members create them." |
| Filtered empty | "No routes match these filters." + "Clear filters" |
| Loading | Skeleton table rows; header and filters stable |
| Error | Inline banner "Could not load routes." + Try again; filters stay |
| Mobile | Table becomes stacked cards: route no., member, status, Going/Return summary, leads/requests/connections; filters in a bottom sheet; pager stays |

**Why the columns matter.** Leads, Requests and Connections are shown together so Admin can see the funnel per route at a glance without opening it; all three are live counts from the records (BR-20.6).

---

### 4.2 Route detail — `/admin/routes/[id]` (W-11.2)

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ ← Route monitor   (Av) Ashish Kumar   RTE-000418   [Active] [Saathi]  v3      │
│ Delhi → Noida · Vehicle: —                                    [ Actions ▾ ]  │
│ [Legs] [Versions] [Leads] [Connections]                                      │
├──────────────────────────────────────────────────────────────────────────────┤
│ Going   Mon–Fri · 8:00 AM                                                    │
│  ① Vinay Nagar   ② Badarpur Metro   ③ Govindpuri   ④ Nehru Place             │
│ Return  Mon–Fri · 6:30 PM                                                    │
│  ① Nehru Place   ② Govindpuri   ③ Badarpur Metro   ④ Vinay Nagar             │
├──────────────────────────────────────────────────────────────────────────────┤
│ Leads  ┌───────────┬───────────────┬──────────┐   Connections                 │
│        │Full Route │Useful Segment │No match  │   Pending 2 · Accepted 3       │
│        │ 4         │ 1             │ 9        │   Rejected 1 · Withdrawn 0     │
│        └───────────┴───────────────┴──────────┘                              │
│ (i) This route is read-only. Admin cannot edit points, days or times.        │
└──────────────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | Back to Route monitor, member avatar, name, `RTE-`, status badge, type chip, version `v{n}`, cities, vehicle (Raahi only); Actions right |
| Tabs | **Legs · Versions · Leads · Connections** (hash routes keep the tab on refresh) |
| Legs tab | Going and Return point by point **as typed**, days and times, using C-15 route line; per-leg lead count. Return empty state "No Return leg." |
| Versions tab | C-34 timeline: version number, date/time, what changed in plain words ("Point 2 renamed", "Days changed", "Time changed"), who. **Read-only** — no restore (M2 owns versioning) |
| Leads tab | Counts by match type (Full Route green, Useful Segment sky, No match slate) and by leg, plus lead status counts; a link opens the Lead monitor filtered to this route |
| Connections tab | Counts by status plus the Reported count; a link opens the Connection monitor filtered to this route's member. **No chat text** |
| Read-only notice | Always-visible info banner: "This route is read-only. Admin cannot edit points, days or times." (BR-20.1, AC-20.1) |
| No edit / delete | There is no edit-point, edit-days, edit-time, delete-route or manual-match control anywhere (BR-20.1, BR-4.6) |
| Actions menu | Only **Hold Under Review** and **Deactivate** (availability-aware); no other action |
| Invalid route | Red banner naming the broken rule; both actions hidden/disabled with the reason "The member must fix this route first." (BR-20.7) |
| Under Review | Amber banner "Under review. This route is out of matching until it is released." Actions hidden; release happens in M12 |
| Mobile | Header stacks; tabs scroll horizontally; legs stack; counts become stat tiles; actions in a full-width menu list |
| States | Skeleton per tab; tab error with retry; 404 "Route not found."; no-access page for a member token; offline read-only |

---

### 4.3 Lead monitor — `/admin/leads` (SCR-20.3)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Leads                                              [Export ]     │
│            │  Search [lead no., member or route______]                         │
│            │  Member[..] Route[..] Match[All▼] Leg[Any▼] Status[All▼] Conv[Any▼]│
│            │  [ Filters (1) ]  [ Clear all ]                                   │
│            │ ┌──────────┬────────────┬────────────┬───────────┬───────┬───────┐ │
│            │ │Lead no.  │Route       │Matched     │Going      │Return │Status │ │
│            │ ├──────────┼────────────┼────────────┼───────────┼───────┼───────┤ │
│            │ │LED-000812│RTE-000418  │Neha S.     │[Full]     │[Useful]│[Req] │ │
│            │ │LED-000813│RTE-000419  │Rohit M.    │[Useful]   │No match│[New] │ │
│            │ └──────────┴────────────┴────────────┴───────────┴───────┴───────┘ │
│            │  Converted: 42 of 120 · Requested 30 · Connected 12               │
│            │  Showing 1–25 of 120            [ < 1 2 3 … 5 > ]                 │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Filters | Member, Route, Match type (Full Route / Useful Segment / No match), Leg, Lead status, Conversion (Requested / Connected / Not converted), date range |
| Columns | Lead no., Route no. + owner, Matched member (avatar, MBR, role, vehicle), Match type per leg (C-12 with icon), Status, Created, Requested date, Connection status, Converted |
| Match type | Per leg badge: Full Route green with "route" icon, Useful Segment sky with "part" icon, No match slate — always text plus icon, never colour alone (BR-20.5 display rule) |
| Conversion summary | A strip above the table computed over the **filtered set**: total, requested, connected, request rate. These are drill-downs, not editable numbers (BR-20.6) |
| Lead detail | Read-only side panel/drawer: route + member summary, per-leg match explanation in plain words, related connection status. **No chat text** (BR-20.4) |
| Read-only | No dismiss, withdraw, request or edit control (BR-20.1) |
| Empty | "No leads yet." / filtered: "No leads match these filters." + Clear filters |
| Loading / Error | Skeleton rows / inline banner + Try again |
| Mobile | Cards: lead no., matched member, match badges, status, conversion; filters in a sheet |

---

### 4.4 Connection monitor — `/admin/connections` (W-11.4)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Connections                                        [Export ]     │
│            │  Search [connection or member__________]                          │
│            │  Status[All▼] Reported[Any▼] Sent[dd-mmm..dd-mmm]  [ Clear ]     │
│            │ ┌ 120 sent │ 55 accepted │ 20 rejected │ 15 expired │ 5 withdraw │ │
│            │ │ 25 pending                                              │     │ │
│            │ │ Acceptance rate 55 of 95 answered · Avg reply 9.5 h     │     │ │
│            │ └──────────────────────────────────────────────────────────┘     │ │
│            │ (!) Chat content is not shown here. Counts and status only.       │ │
│            │ ┌──────────┬────────┬────────┬──────────┬──────┬────────┬───────┐ │
│            │ │Conn no.  │Sender  │Receiver│Status    │Sent  │Respond │Report │ │
│            │ ├──────────┼────────┼────────┼──────────┼──────┼────────┼───────┤ │
│            │ │CON-000441│Ashish K│Neha S. │[Accepted]│09-Oct│09-Oct  │  —    │ │
│            │ │CON-000442│Rohit M.│Ashish K│[Pending] │10-Oct│ —      │[Report]│ │
│            │ └──────────┴────────┴────────┴──────────┴──────┴────────┴───────┘ │
│            │  Showing 1–25 of 120            [ < 1 2 3 … 5 > ]                 │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Funnel tiles | Sent, Pending, Accepted, Rejected, Expired, Withdrawn (+ Disconnected/Blocked in the drill-down). Each tile drills to the list filtered by that status (BR-23.3 style) |
| Acceptance rate | Shown as "55 of 95 answered" — the denominator is accepted + rejected, never pending (SC-20.4). Bars/tiles carry their number as text so they read without colour |
| Avg reply time | Sent → accepted/rejected only; display "Avg reply 9.5 h" |
| Filters | Status (multi), Reported (Yes/No), date range, search |
| Columns | Connection no., Sender, Receiver, Status, Sent on, Responded on, **Reported** flag, Response time. No route/chat column is required |
| Reported flag | Chip "Report" where an open safety report involves the pair; clicking it goes to the Safety module report (M12). Never shows report text here |
| Status chips | Pending amber, Accepted green, Rejected slate, Withdrawn/Expired/Disconnected slate, Blocked red (Admin only) — exact UI Plan §3.7 wording |
| No chat | Always-visible info banner "Chat content is not shown here. Counts and status only." (BR-20.4, AC-20.4). There is no "open chat" control |
| Connection detail | Read-only drawer: pair, status, dates, response time, Reported flag. **No messages** |
| Read-only | No accept/reject/withdraw/disconnect control (BR-20.1) |
| Empty / filtered-empty | "No connections yet." / "No connections match these filters." + Clear filters |
| Loading / Error | Skeleton tiles + rows / inline banner + Try again |
| Mobile | Tiles wrap in two columns; table becomes cards: pair, status, sent date, Reported flag |

---

### 4.5 Matching Health — `/admin/matching-health` (W-11.3)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Matching Health          City [All ▼]   Period [7 days ▼]        │
│            │ ┌─────────┐┌─────────┐┌─────────┐┌─────────┐┌─────────┐          │
│            │ │ 30      ││ 4.2     ││ 22%     ││ 58%     ││ 9.5 h   │          │
│            │ │No-lead  ││Leads per││Lead →   ││Request →││Avg time │          │
│            │ │routes   ││route    ││request  ││accept   ││to answer│          │
│            │ └─────────┘└─────────┘└─────────┘└─────────┘└─────────┘          │
│            │ Match mix per leg   Going  ▇▇▇▇▇▇░░ Full 62% · Segment 38%      │
│            │                     Return ▇▇▇▇░░░░ Full 48% · Segment 52%      │
│            │ Supply and demand   Saathi routes 540 · Raahi routes 310         │
│            │ By city  City | Active routes | Leads | Lead→req | Accept        │
│            │ Most used stop names            │ Unmatched stop names           │
│            │  Badarpur Metro (312)           │  Kalka Ji Temple Gate (3)      │
│            │   variants: Bdarpur Metro,      │   nearest: Kalkaji 52%         │
│            │   badarpoor metro               │  Sai Mandir Gate (4)          │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Controls | City (All + City Master), Period (7/14/30 days; default 7). The period applies to rates/response; the no-lead threshold N is editable below |
| Stat tiles | No-lead routes, Leads per route, Lead → request, Request → accept, Avg time to answer. Each tile **drills down** to the list behind it (UI Plan §6.11, BR-23.3) |
| No-lead routes | Tile + a list with an editable N ("Active for more than [7] days") split by Saathi/Raahi. Opens each route; no action required (SC-20.1) |
| Leads per route | Average plus a small distribution (0 · 1–2 · 3–5 · 6–10 · 10+); bars carry numbers as text |
| Match mix | Full Route vs Useful Segment per leg, as labelled bars with percentages as text |
| Supply and demand | Active Saathi routes vs Active Raahi routes, shown as two numbers and a simple ratio; each drills into the Route monitor filtered by type |
| By city | Table: city, active routes, leads, lead → request rate, acceptance rate; sortable; row drills to the Route monitor filtered by city |
| Most used stop names | Grouped by the **same similarity rule as matching** (CFG-22), canonical label + count + variant spellings listed (SC-20.2, BR-20.5). Clicking a group filters the Route Monitor by point text |
| Unmatched stop names | Stop names on Active routes that never match, with occurrence count and nearest matched text + similarity, so Admin sees why grouping failed (SC-20.3). A link hints at Test Match (M13) — it does not change config here |
| Live figures | No typable or adjustable number anywhere; the screen is read-only (BR-20.6) |
| Empty | "Not enough activity for this period." with a hint to widen the period; no fabricated data |
| Loading / Error | Skeletons per block; block-level error with retry so one failing tile does not blank the page |
| Mobile | Tiles in two columns; bars stack; By city and stop lists become cards; controls in a sheet |

**Important:** the stop lists must visibly state that grouping uses the matching rule, e.g. helper text "Grouped with the same text rule as matching (threshold {CFG-22}%)." This is the whole point of the screen (BR-20.5).

---

### 4.6 Route action dialog — Deactivate / Hold Under Review (BR-20.3)

```text
┌  Deactivate RTE-000418? ──────────────────────┐
│  Member: Ashish Kumar                         │
│  This will:                                   │
│  • switch the route to Inactive               │
│  • withdraw 4 leads                           │
│  • withdraw 2 pending requests                │
│  • keep existing connections and chats        │
│  • record the reason in the audit trail       │
│                                               │
│  Reason (at least 10 characters)              │
│  [______________________________________]     │
│  0/500                                        │
│  Recorded in the audit trail.                 │
│  [ Cancel ]            [ Deactivate ]  (red)  │
└───────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| One pattern | Same dialog for both actions; title, effect list, warning and confirm label change with the action (UI Plan §7.3 "Dialog with reason") |
| Effects | Built from the API context with real counts; never vague (UP-5) |
| Reason | C-2 textarea, visible label, live counter, helper "Recorded in the audit trail."; confirm disabled until trimmed length ≥ 10; inline error on blur "Enter a reason of at least 10 characters." (V-45) |
| Deactivate | Red confirm; effects as above; note connections/chats are kept (BR-11.14) |
| Hold Under Review | Neutral/amber confirm; warning banner "This removes the route from matching and withdraws its leads. It stays out until released in the Safety module." (BR-20.3, A-32) |
| Availability | Only shown when allowed for the route status; otherwise the menu item is disabled with a nearby reason or hidden |
| Loading | Confirm shows a spinner, fields disabled, double-submit blocked |
| Error | Inline banner inside the dialog, keep the reason; specific copy for `ACTION_NOT_ALLOWED` and incomplete deactivation |
| Success | Dialog closes, toast, header status refreshes, tabs reload; Route monitor reflects the new status on return |
| Offline | Confirm disabled with the offline notice |
| Accessibility | Focus trap, labelled dialog, reason error announced, effects list read in order |

---

### 4.7 Export dialog

```text
┌  Export routes ─────────────────────────┐
│  Current filters:                       │
│   Status: All · Type: Saathi · City: Delhi│
│  Rows: 850                              │
│  [x] Include member names               │
│  (i) Chat content and contact details   │
│      are never included.                │
│  [ Cancel ]            [ Export CSV ]   │
└─────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Filters summary | Show active filters and row estimate |
| Member names | Checked by default; can be dropped for a de-identified file |
| Never included | Chat text, message counts, email and mobile are never in the file (BR-20.2, BR-20.4) |
| Large export | If API refuses with `EXPORT_TOO_LARGE`, show "Too many rows. Narrow the filters." and keep the dialog open |
| Audit | Copy says the export is recorded |
| Offline | Export disabled |
| Success | Browser downloads; toast "Export started." |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton table/rows/tiles; header and filters stable; never a bare spinner |
| Empty | Per-block copy with one next action; no fake data |
| Filtered empty | "No {records} match these filters." + Clear filters |
| Error (load) | Inline banner + Try again; keep filters/tab; block-level for Health |
| Error (action) | Inline in the dialog; keep the reason; specific code copy |
| Success | Toast + refresh header/status/tabs; monitor reflects the new status on return |
| Partial failure | "Deactivation did not finish." with the applied steps; no false success |
| Disabled | Action or confirm disabled with a visible reason nearby |
| No access | "You do not have access to this record." (no existence hint) |
| Not found | "Route / Lead / Connection not found." with a back link |
| Invalid route | Invalid routes appear first with the broken rule named (BR-20.7) |
| Under Review | Amber banner; actions hidden; still visible in the monitor |
| Offline | Slim banner; Export and write actions disabled with a reason; reads may show cached data |
| Session expired | Foundation dialog; return to login (BR-5.6) |
| Restricted/Inactive Admin | Cannot log in (BR-5.4) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Desktop 1024+ | Full data tables, sidebar, 12-column layout; detail tabs; dialogs centred; Health in a 2–3 column grid; Admin content fluid with a 1440 px text cap |
| Tablet 768–1023 | Tables collapse to fewer columns or cards; filters in a sheet; detail two columns; Health tiles 2-up; dialogs centred |
| Phone 360–767 | Cards with the primary fields and flags; filters in a bottom sheet; detail header stacks; tabs scroll horizontally; actions in a full-width menu/card list; dialogs become full-height sheets |

- Admin is desktop-first, but every screen must remain readable on a phone (UI Plan §7.5).
- Tables scroll inside their container on small screens; they never force page-level horizontal scroll.
- At 200% text zoom the page must remain usable; Admin tables may scroll inside their box.
- Sticky table header on long monitors; state is preserved when opening a detail and coming back.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring; dialogs trap focus and return it to the Actions trigger |
| Labels | Every filter, the period/N field and the reason field have visible labels; placeholders are examples |
| Targets | ≥ 44 × 44 px, 8 px apart |
| Colour | Status, type, match-type and flag badges always carry words/icons; **bars carry their number as text** so meaning does not depend on colour |
| Keyboard | Search, filters, table sort, row open, tabs, tile drill-down, Actions menu, reason field, export checkbox and pager work by keyboard |
| Screen readers | Announce dialog title, effect list, reason validation, action success; table headers use `th` scope; tile values and bar values are read as text; row action is a real button/link |
| Motion | Honour "reduce motion"; no decorative animation |
| Zoom | 200% text without horizontal page scroll; Admin tables scroll internally |
| Language | `lang="en"`; dates `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M11)

| Situation | Copy |
|---|---|
| Page titles | "Route monitor" · "Route detail" · "Leads" · "Connections" · "Matching Health" |
| Search (routes) | "Search by route number or member" |
| Search (leads) | "Search by lead number, member or route" |
| Search (connections) | "Search by connection number or member" |
| Active filters | "{n} filters applied" |
| Clear all | "Clear all" |
| Invalid banner | "{n} routes are Invalid. A member fix is required." |
| Invalid rule line | "Rule broken: {rule name}." |
| Read-only notice (route) | "This route is read-only. Admin cannot edit points, days or times." |
| Read-only notice (monitor) | "Monitoring is read-only. There is no way to match or assign members here." |
| No leads empty | "No leads yet." |
| No-lead routes empty | "No Active route has gone without leads for {n} days." |
| Filtered empty | "No {records} match these filters." |
| Load error | "Could not load {records}." |
| Not found | "{Record} not found." |
| Stop grouping helper | "Grouped with the same text rule as matching (threshold {CFG-22}%)." |
| Unmatched stop hint | "Nearest match: {text} ({n}%)." |
| By-city empty | "Not enough activity for this period." |
| No chat notice | "Chat content is not shown here. Counts and status only." |
| Reported flag | "Report" |
| Funnel acceptance | "Acceptance rate {accepted} of {answered} answered" |
| Avg reply time | "Avg reply {x} h" |
| Actions | "Actions" |
| Hold action | "Hold Under Review" |
| Deactivate action | "Deactivate route" |
| Hold warning | "This removes the route from matching and withdraws its leads. It stays out until released in the Safety module." |
| Reason label | "Reason (at least 10 characters)" |
| Reason helper | "Recorded in the audit trail." |
| Reason error | "Enter a reason of at least 10 characters." |
| Deactivate effects | "Switch the route to Inactive; withdraw leads; withdraw pending requests; keep connections and chats." |
| Action success | "{Action} completed." |
| Action not allowed | "This action is not available for the route's current status." |
| Partial failure | "Deactivation did not finish. Review the applied steps and try again." |
| Export title | "Export {records}" |
| Export note | "Chat content and contact details are never included." |
| Export too large | "Too many rows. Narrow the filters." |
| No access | "You do not have access to this record." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load route monitor | `GET /admin/routes?search=&status=&routeType=&cityId=&hasReturnLeg=&hasOpenReport=&leadsBucket=&pointText=&changedFrom=&changedTo=&sort=&page=&pageSize=` |
| Route filter options | `GET /admin/routes/options` |
| Open route detail | `GET /admin/routes/{id}` |
| Legs tab | `GET /admin/routes/{id}/legs` |
| Versions tab | `GET /admin/routes/{id}/versions` |
| Leads tab | `GET /admin/routes/{id}/leads` |
| Connections tab | `GET /admin/routes/{id}/connections` |
| Open action dialog | `GET /admin/routes/{id}/actions/context?action={Deactivate|HoldUnderReview}` |
| Confirm action | `POST /admin/routes/{id}/actions` `{ action, reason }` |
| Export routes | `POST /admin/routes/export` `{ filters }` |
| Load lead monitor | `GET /admin/leads?search=&memberId=&routeId=&matchType=&legType=&status=&converted=&createdFrom=&createdTo=&sort=&page=&pageSize=` |
| Lead options | `GET /admin/leads/options` |
| Open lead detail | `GET /admin/leads/{id}` |
| Export leads | `POST /admin/leads/export` |
| Load connection monitor | `GET /admin/connections?search=&status=&reported=&sentFrom=&sentTo=&sort=&page=&pageSize=` |
| Connection options | `GET /admin/connections/options` |
| Funnel tiles | `GET /admin/connections/funnel` (or the `funnel` block of the list) |
| Open connection detail | `GET /admin/connections/{id}` |
| Export connections | `POST /admin/connections/export` |
| Load Matching Health | `GET /admin/matching-health?cityId=&days=` |
| Health options | `GET /admin/matching-health/options` |
| No-lead routes | `GET /admin/matching-health/no-lead-routes?days=&routeType=` |
| Leads per route | `GET /admin/matching-health/leads-per-route?days=` |
| Match mix | `GET /admin/matching-health/match-mix?days=&cityId=` |
| Most used stops | `GET /admin/matching-health/stop-names/most-used?days=&cityId=` |
| Unmatched stops | `GET /admin/matching-health/stop-names/unmatched?days=&cityId=` |
| Health funnel | `GET /admin/matching-health/funnel?days=&cityId=` |
| Response time | `GET /admin/matching-health/response-time?days=&cityId=` |
| Supply and demand | `GET /admin/matching-health/supply-demand?cityId=` |
| By city | `GET /admin/matching-health/by-city?days=` |

Client rules:

- Always send the Admin token; never accept a route id except from the route path.
- On `403` → generic no-access; on `404` → "{Record} not found."; never guess which.
- On `409 ACTION_NOT_ALLOWED` → refresh the route detail and show the current status in the dialog.
- On `400 REASON_REQUIRED` → focus the reason field and keep the dialog open.
- After a successful action, refresh the header, actions and all tabs; the Route monitor reflects the new status on return.
- Never render a chat or message body; there is no such field in any M11 response.
- Match-type and status badges use the shared status map (UI Plan §3.7); never invent colours.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M2 route list/detail/versions | Detail shows only what the route reader returns; missing sections show a neutral empty state |
| M4 lead counts and list | Show `0` or "—" with a neutral note; never fabricate |
| M5 connections and funnel | Show `0`/`—`; funnel tiles disabled with "No data yet" |
| M8/M12 open-report flags | No flag shown until the reader returns it; the Reported column reads "—" |
| M13 Test Match | Unmatched-stop hint links to "Try in Test Match"; if absent, show plain text only |
| M9 notifications | The action still succeeds if delivery is queued; do not block the dialog on email |
| M12 release of Under Review | Detail shows "Release is handled in the Safety module." No release control here |
| CFG-1/2/4/19/22 | Copy and thresholds read from the API; never hard-code |
| Shared C-28/C-29/C-34 | Build/reuse from M10 if already present; otherwise build here |

---

## 11. Delivery plan (M11 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Reuse/build C-28 data table, C-29 stat tiles, C-34 timeline, C-15 route line | UI Plan §3 |
| 2 | Route monitor: search, all filters, invalid-first banner, columns, empty/error/loading | API 1, 2 |
| 3 | Route detail: header, tabs, read-only notice | API 3 |
| 4 | Legs and Versions tabs | API 4, 5 |
| 5 | Leads and Connections summary tabs with drill-down links | API 6, 7 |
| 6 | Actions menu + shared reason dialog with context effects | API 8, 9 |
| 7 | Deactivate/Hold success, partial-failure and not-allowed states | API 9 |
| 8 | Lead monitor with match badges, conversion strip, read-only detail drawer | API 11–13 |
| 9 | Connection monitor with funnel tiles, Reported flag, no-chat notice | API 15–18 |
| 10 | Matching Health summary, bars, by-city table, supply/demand | API 20, 21 |
| 11 | No-lead list with editable N; leads-per-route distribution | API 22, 23 |
| 12 | Most used / unmatched stop lists with grouping helper text | API 25, 26 |
| 13 | Export dialog for the three monitors | API 10, 14, 19 |
| 14 | Responsive phone/tablet pass; Admin density | — |
| 15 | Accessibility pass: tables, bars-as-text, menu, dialog focus, reason counter, announcements | — |
| 16 | QA: read-only guarantees, no chat anywhere, same-rule grouping, invalid-first, 200% zoom, slow network | API running |

Suggested route files (framework-agnostic naming): `admin/routes/index`, `admin/routes/[id]`, `admin/leads/index`, `admin/connections/index`, `admin/matching-health/index`, plus dialogs `RouteActionDialog`, `ExportMonitorDialog`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-20.1 | No control exists anywhere to create, edit or delete a route, lead, request or connection; no manual-match control exists (SC-20.6) |
| AC-20.2 | Deactivate confirm is disabled until the reason has 10 non-space characters; the API also refuses short reasons |
| AC-20.3 | Hold Under Review copy states the route leaves matching and produces no leads until released |
| AC-20.4 | Connection Monitor and detail show status, dates and counts only; no message text or "open chat" control exists |
| AC-20.5 | The stop-name lists group variants with the same rule as matching and say so |
| BR-20.1 | Monitoring is read-only everywhere (AC-20.1) |
| BR-20.2 | Full route is visible to Admin; exports never include contact details |
| BR-20.3 | Both actions require a reason and record it; effects are shown from real counts |
| BR-20.4 | No chat text appears anywhere in M11 |
| BR-20.5 | Most-used and unmatched stops use the matching similarity rule with variants listed |
| BR-20.6 | No figure is typable or adjustable; all counts are live |
| BR-20.7 | Invalid routes sort first, show the broken rule, and are not fixed by Admin |
| UP-5 | Destructive actions state exact effects before acting |
| UP-10 | Loading, empty, filtered-empty, error, no-access, not-found, disabled, offline, partial-failure and success states are designed |
| UP-11 | Contrast, focus, labels, targets, keyboard and screen-reader announcements pass; bars carry numbers as text |
| UP-12 | Admin screens are dense and deliberate; no bulk actions |
| — | Dates use `09-Oct-2026`, times `8:05 AM`, India time |
| — | Status/match-type badges use the shared map, always text plus colour |
| — | Every tile and row drills down to the list behind it |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| UD-UI-1 | Show Draft routes in the monitor by default? | Yes in the list; default sort puts Invalid first, then Active |
| UD-UI-2 | Route detail as tabs or one long page? | Tabs (Legs · Versions · Leads · Connections), matching W-11.2 |
| UD-UI-3 | Lead/connection detail: drawer or separate page? | Side drawer on desktop, full-height sheet on phone; no separate page |
| UD-UI-4 | Where is the Under Review release? | Not here; a note points to the Safety module (M12) |
| UD-UI-5 | Export member names by default? | Yes; checkbox can drop names; contacts/chat never included |
| UD-UI-6 | Show the no-lead threshold N inline or in a dialog? | Inline number field on the no-lead block with helper "Active for more than N days" |
| UD-UI-7 | Matching Health: one page with blocks or sub-pages? | One page of drill-down blocks (W-11.3); each block links to a filtered list |
| UD-UI-8 | Show a route's Invalid rule as a banner or inline column? | Both: banner at top of the monitor and rule text under the status in the row/detail |
| UD-UI-9 | Phone layout for wide monitors? | Stacked cards; filters in a bottom sheet; pager retained |
| UD-UI-10 | Should Reported flag link to the report? | Yes, directly to the M12 report detail; it never shows report text in M11 |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-20.1 Route Monitor | §2, §4.1 |
| SCR-20.2 Route Detail | §2, §4.2 |
| SCR-20.3 Lead Monitor | §2, §4.3 |
| SCR-20.4 Connection Monitor | §2, §4.4 |
| SCR-20.5 Matching Health | §2, §4.5 |
| Route Monitor filters/columns | §4.1 |
| Route Detail blocks/actions | §4.2, §4.6 |
| Matching Health blocks | §4.5 |
| BR-20.1 read-only | §1, §2, §4.1–4.5, §12 |
| BR-20.2 full route + masking | §4.1, §4.7 |
| BR-20.3 deactivate/hold with reason | §4.2, §4.6 |
| BR-20.4 no chat text | §4.4, §8, §12 |
| BR-20.5 same rule as matching | §4.1, §4.5 |
| BR-20.6 live figures | §4.1, §4.3, §4.5 |
| BR-20.7 Invalid routes first | §4.1, §4.2 |
| SC-20.1…SC-20.7 | §4, §9 |
| AC-20.1…AC-20.5 | §12 |
| Data Impact (deactivate/hold) | §4.6 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4.2 Admin navigation | §2 |
| UI Plan §6.11 W-11.1…W-11.4 | §4.1–4.5 |
| UI Plan §7.3 confirmations | §4.6 |
| UI Plan §7.4 lists/filters/paging | §4.1–4.4 |
| UI Plan §7.1 states | §5 |
| UI Plan §8 delivery | §11 |
| Flow F-9 Admin tunes matching | §4.5 (stop lists → Test Match hint, M13) |
