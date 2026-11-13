# M12 — Safety, Reports & Blocked Users — UI Plan

| | |
|---|---|
| **Module** | M12 — Safety, Reports & Blocked Users (spec `docs/spec/12_M12_Safety_Reports_Blocked.md`, functional specification Section 21) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, Admin navigation §4.2, flows §5 (F-8), module design §6.12 (W-12.1…W-12.4), cross-module patterns §7 (states §7.1, confirmations §7.3, lists §7.4), delivery §8 |
| **Platform** | Admin screens **desktop first**, then tablet and phone (D-20). No member UI in this module |
| **Companion** | API plan: `12_M12_Safety_Reports_Blocked_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M12 is the Admin **safety control room**. It answers: *is every reported member handled fairly, fast and on the record?*

1. **SCR-21.1 Report queue** — every report, oldest first, with age, reason, reporter, reported member, status and flags; overdue reports highlighted (W-12.1).
2. **SCR-21.2 Report detail** — the report, reporter history, reported member history, the read-only conversation (opened on demand and logged), and related reports (W-12.2).
3. **SCR-21.3 Take action** — Warning / Restriction / Temporary Block / Permanent Block / Dismiss, each with a reason and a preview of exact effects (W-12.3).
4. **SCR-21.4 Restricted & Blocked** — everyone currently restricted or blocked, with end dates and **Lift** for reversible ones (W-12.4).
5. **SCR-21.5 Flagged members** — members at/above CFG-15 distinct reporters, with their reports side by side.
6. **SCR-21.6 Rating moderation** — reported comments with **Hide** and a reason; stars keep counting.

UI jobs for this module:

- **Calm, deliberate action.** Every action states its effects before it happens and requires a reason (UP-5, UP-12, §7.3 "Dialog with reason").
- **Proportionality at a glance.** The member's warnings, restrictions, blocks, route, rating and other reports are visible before Admin chooses a step (BR-21.4).
- **The reporter is invisible to the member.** Admin sees the reporter; nothing sent to the member names them (BR-21.10, AC-21.5).
- **Chat is locked until Admin asks.** The conversation is a deliberate "Open conversation" action with a visible logging notice (BR-21.3, AC-21.1).
- **Reversibility is explicit.** Restriction and Temporary Block can be lifted; Permanent Block says plainly that it cannot (BR-21.6/21.7/21.8).
- **Every state designed** — loading, empty, filtered-empty, error, no access, action loading, partial failure, success (UP-10).
- **Admin density, calm use.** Dense tables, keyboard-friendly, drill-down from every flag and tile (UP-12).

Out of scope for M12 screens (linked, owned elsewhere): member list/detail (M10), route monitor and route actions (M11), configuration (M13), dashboard (M14), reports catalogue (M15), member-side report/block forms (M8), rating creation (M7).

---

## 2. Navigation and routes

M12 lives in the Admin sidebar group **Safety** (UI Plan §4.2): **Report queue · Restricted and Blocked · Flagged members · Rating moderation**. The Admin top bar keeps global search, the notification bell and the Admin menu.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Report queue | `/admin/safety` | SCR-21.1 | Sidebar "Report queue"; dashboard overdue/open-report tiles; Admin alert |
| Report detail | `/admin/safety/[id]` | SCR-21.2 | Row in the queue; flagged-member report list; M10 Safety tab; M11 "Report" flag |
| Take action | dialog on detail | SCR-21.3 | Detail → Take action |
| Restricted and Blocked | `/admin/restricted` | SCR-21.4 | Sidebar "Restricted and Blocked"; dashboard Restricted/Blocked tiles; M10 Safety tab |
| Flagged members | `/admin/flagged` | SCR-21.5 | Sidebar "Flagged members"; dashboard flagged tile; Admin alert |
| Rating moderation | `/admin/rating-moderation` | SCR-21.6 | Sidebar "Rating moderation"; dashboard moderation tile |

Admin gates (Foundation): Admin login only (D-19, BR-5.11); Inactive/Blocked Admin cannot log in (BR-5.4); the Admin UI is a separate bundle from the member app (plan §9.5).

**Hard route rules:** there is no `/admin/safety/new` (Admin cannot create a report — BR-21.16), no delete route (BR-21.1), no "match" route, and no "undo permanent block" control (BR-21.8, AC-21.6). If someone types one, show the generic no-access page rather than a form.

---

## 3. Screen inventory and components

| Screen / element | Components used (UI Plan §3.6) |
|---|---|
| Report queue | C-28 Data table (sort, column chooser, saved filters, export), C-27 Filter bar, C-2 Search, C-4 Select multi, C-12 Status/reason chips, C-21 Overdue banner, C-25 Empty state, C-26 Skeleton, C-22 Toast, C-35 numbered pager, C-29 summary tiles |
| Report detail | C-13 Avatar, C-12 Status/flag chips, C-1 Buttons, C-33 Key-value list, C-34 History timeline, C-29 Stat tiles, C-15 Route line (compact), C-21 Banners, C-23 Dialog trigger |
| Conversation viewer | C-19 Message bubble (read-only variant), C-21 Info banner ("This access is logged"), C-1 Close button, C-26 Skeleton |
| Take action dialog | C-23 Dialog (double confirm for permanent block), C-2 Textarea (reason), C-6 Radio list (action), C-8 Date field (end date), C-2 Textarea (message to member), C-21 Warning banner, C-22 Toast |
| Restricted and Blocked | C-28 table, C-27 filters, C-12 status chips, C-33 key-value rows, C-23 Lift dialog, C-29 count tiles, C-35 pager |
| Flagged members | C-28 table, C-29 stat tiles, C-12 reason chips, expandable report rows, C-35 pager |
| Rating moderation | C-28 table, C-14 Rating stars (read-only), C-12 status chip, C-23 Hide dialog, C-25 Empty state, C-26 Skeleton |
| Lift dialog | C-23 Confirm dialog, C-2 Textarea (reason), C-21 Warning banner (closures not restored), C-22 Toast |

Build shared Admin components here if not yet created by M10/M11: C-28, C-29, C-34. M12 adds a small feature-scoped **conversation viewer** (read-only message list with a logging notice) — not a global chat component.

---

## 4. Screen designs

### 4.1 Report queue — `/admin/safety` (W-12.1)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Report queue                                          [Export ]  │
│ Dashboard  │ ┌ 12 open │ 2 overdue │ 1 flagged │ 4 restricted │ 3 blocked │    │
│ Members ▸  │ │ 2 comments awaiting review                              │      │
│ Routes ▸   │ └─────────────────────────────────────────────────────────┘      │
│ Safety ◄   │  Search [SAF no., reporter or member____]                          │
│  Queue     │  Status[All▼] Reason[All▼] Age[Any▼] Flags[Any▼] Member[Any▼]     │
│  Restricted│  [ Filters (1) ]  [ Clear all ]                  [ Columns ▾ ]   │
│  Flagged   │ ┌ (!) 2 reports are overdue (older than 3 days). ──────────────┐ │
│  Moderation│ │ SAF-000007 · Submitted · Inappropriate messages · 4 days      │ │
│ Settings ▸ │ └───────────────────────────────────────────────────────────────┘ │
│ Reports    │ ┌──────────┬─────┬────────────┬─────────┬─────────┬─────────┬───┐ │
│            │ │SAF no.   │Age  │Reason      │Reporter │Reported │Status   │Flg│ │
│            │ ├──────────┼─────┼────────────┼─────────┼─────────┼─────────┼───┤ │
│            │ │SAF-000009│2 d  │Inapprop.   │Ashish K.│Rohit M. │[Under…] │⚑  │ │
│            │ │SAF-000007│4 d ⚠│Harassment  │Neha S.  │Rohit M. │[Submitted│⚑⚑│ │
│            │ └──────────┴─────┴────────────┴─────────┴─────────┴─────────┴───┘ │
│            │  Showing 1–25 of 48            [ < 1 2 · · · > ]                   │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Summary strip | Open reports, overdue, flagged members, restricted, blocked, comments awaiting review. Each is a **drill-down** tile (C-29) filtering the queue or navigating to the matching screen (BR-23.3 style) |
| Overdue banner | Amber at CFG-32 days, red at 2× CFG-32, with the word "Overdue" and the age in days. The banner summarises the count; rows carry the same styling (BR-21.1, AC-21.9) |
| Sort | Default **Oldest first**; also Newest first, Overdue first, Age. Arrow in the header |
| Columns | SAF no., Age, Reason, Reporter, Reported member, Status, Flags. Column chooser persists per Admin (C-28) |
| Flags | Small labelled chips: "3 reporters" (escalated), "Member restricted", "Member blocked". Always text + icon, never colour alone. A flag opens the detail or filtered view — it never shows chat text |
| Reporter / reported | Name + MBR; avatar optional in the row. Reporter is Admin-only and never leaves this surface |
| Filters | Status (multi), Reason (multi), Age (Any / Overdue / 0–1 / 1–3 / 3+ days), Flags (flagged / member restricted / member blocked), Reported-member status, date range. Active count + Clear all (§7.4) |
| Row | Whole row clickable/keyboard-activatable → Report detail; visible focus |
| Status | Exact UI Plan §3.7 words: Submitted (sky), Under Review (amber), Action Taken (indigo), Dismissed (slate), Closed (slate) |
| Pager | Numbered pager with total; 25/page |
| Empty (no reports) | "No reports yet." + "Reports appear here when members raise one." |
| Filtered empty | "No reports match these filters." + "Clear filters" |
| Loading | Skeleton rows; summary strip skeletons; header and filters stable |
| Error | Inline banner "Could not load reports." + Try again; filters stay |
| Mobile | Cards: SAF no., age/overdue, reason, reporter → reported, status, flags; filters in a bottom sheet; pager stays |

**Why the reporter is a column.** Admin must be able to spot a member who raises many unfounded reports (BR-21.12, SC-21.11). The reporter identity is Admin-only and never appears in anything sent to the reported member (BR-21.10).

---

### 4.2 Report detail — `/admin/safety/[id]` (W-12.2)

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ ← Report queue   SAF-000009   [Under Review]   4 days · Overdue? no          │
│ Inappropriate messages · Reported 09-Oct-2026                                 │
│ [ Mark Under Review ] [ Hold route under review ] [ Take action ▾ ] [Dismiss]│
├───────────────────────────────┬──────────────────────────────────────────────┤
│ Report                        │ Reported member: Rohit M.  MBR-000232 [Active]│
│ Description                   │ Raahi · Bike · ★ 3.1 (8)                      │
│ "Sent repeated unwanted…"     │ Route: Active, Delhi → Noida                 │
│                               │ Warnings 1 · Restrictions 0 · Blocks 0       │
│ Reporter: Ashish K. MBR-000231│ Open reports 2 · Reports from others 2       │
│ Raised 5 · Dismissed 2 ·      │ [ View full member ]  [ View route ]         │
│ Action taken 2 · Open 1       │                                              │
├───────────────────────────────┼──────────────────────────────────────────────┤
│ Related                       │ Conversation (read-only)                      │
│ Connection: CON-000441        │ No conversation attached /                    │
│ [Accepted]                    │ [ Open conversation ]                         │
│ Other open reports:           │ (i) Opening it is logged with your name and   │
│ SAF-000010 · Spam · Submitted │ time. You can read only while the report is   │
│                               │ open.                                         │
├───────────────────────────────┴──────────────────────────────────────────────┤
│ Timeline: Submitted 07-Oct → Under Review 08-Oct → …                          │
└──────────────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | Back to queue, SAF number, status badge, age and overdue state, reason, submitted date; actions on the right |
| Report block | Description (Admin-only), reporter name + MBR and the reporter's history counts: **Raised · Dismissed · Action taken · Open** (BR-21.12, SC-21.11) |
| Reported member block | Name, MBR, status badge, role, vehicle, rating (or "New member"), current route (compact C-15), warnings, restrictions, temporary/permanent blocks, open reports, and reports from other members. Links open the member detail (M10) and route detail (M11) |
| Related block | The connection between the two members with its status; other open reports against the member, each a link (SC-21.6) |
| Conversation block | **Closed by default.** "Open conversation" reveals the read-only messages. Notice is always visible: "Opening it is logged with your name and time." Only available while the report is open (BR-21.3, AC-21.1). If no chat is attached: "No conversation is attached to this report." |
| Actions | Mark Under Review (only Submitted), Hold route under review / Release route (if a route applies), Take action (Warning/Restriction/Temporary/Permanent), Dismiss. Each is availability-aware; disabled items show the reason nearby |
| Hold route | Opens the reason dialog. Copy states the route leaves matching and produces no leads until released (SC-21.7, A-32) |
| Timeline | C-34: Submitted → Under Review → Action Taken/Dismissed → Closed, with dates. Never contains the reporter's name or chat text |
| Decision | After an action, the detail shows the decision and sanction reference to Admin only; it is never shown to the member (BR-21.10) |
| Mobile | Blocks stack in one column; actions become a sticky full-width menu/button group; conversation opens full-screen |
| States | Skeleton per block; block-level error with retry; 404 "Report not found."; no-access page for a member token; report closed → conversation button disabled with "The conversation can be opened only while the report is open." |

---

### 4.3 Conversation viewer (BR-21.3, AC-21.1)

```text
┌  Conversation · SAF-000009 ───────────────────────────────[ Close ]┐
│ (i) This view is read-only and this opening is recorded with your   │
│     name and the time. It is allowed only while the report is open. │
├─────────────────────────────────────────────────────────────────────┤
│  Ashish K.                                          09-Oct 8:05 AM │
│  Hello, I want to connect with you.                                 │
│                                                                     │
│  Rohit M.                                           09-Oct 8:12 AM │
│  …                                                                  │
└─────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Trigger | Only the "Open conversation" button on an open report. There is no chat reader anywhere else in the Admin app |
| Logging notice | Always visible before and inside the viewer; the open is logged with Admin and time (BR-21.3) |
| Content | Read-only message bubbles (C-19 read-only variant), sender label and time. **No composer, no edit, no delete, no download** |
| Closed report | Button disabled with the reason "The conversation can be opened only while the report is open." |
| No chat | "No conversation is attached to this report." — no empty viewer |
| Loading / Error | Skeleton messages / inline "Could not open the conversation." + Try again (does not re-log a failed open) |
| Privacy | Only the attached chat is shown; no contact details; no other conversations |

---

### 4.4 Take action dialog — `/admin/safety/[id]` (W-12.3)

```text
┌  Take action on Rohit M. ──────────────────────────────────────────┐
│  (•) Warning        — record only, no change to status             │
│  ( ) Restriction    — cannot send connection requests              │
│  ( ) Temporary block                                              │
│  ( ) Permanent block                                              │
│                                                                    │
│  End date  [ dd-mmm-yyyy ]   (Restriction optional · Temp required)│
│  What this does                                                    │
│  • Set the member to Restricted                                    │
│  • They cannot send connection requests until 12-Dec-2026          │
│  • They can still log in, chat and accept requests                 │
│  • Send the member a notice (in-app and email)                     │
│  • Tell the reporter the report was reviewed                       │
│                                                                    │
│  Reason (at least 10 characters)                                   │
│  [__________________________________________________________]      │
│  0/500 · Recorded in the audit trail.                              │
│  Message to the member (optional)                                  │
│  [__________________________________________________________]      │
│  (i) The message never names the reporter.                         │
│  [ Cancel ]                              [ Apply action ]          │
└────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| One dialog | Same pattern for all four actions; title, effect list, end-date rule, warning and confirm label change with the choice (UI Plan §7.3) |
| Action selector | Radio list (native radios, C-6 pattern) with a one-line plain explanation per action. Dismiss uses its own reason dialog, not this one |
| Effects | Built from the API context with **real counts** ("withdraw 2 pending requests", "disconnect 1 connection", "close 1 chat"). Never vague (UP-5) |
| End date | C-8 date field. Hidden/disabled for Warning and Permanent Block; optional for Restriction; mandatory for Temporary Block; must be in the future. Shows the India-time date format `dd-mmm-yyyy` |
| Reason | C-2 textarea, visible label, live counter, helper "Recorded in the audit trail."; confirm disabled until trimmed length ≥ 10; inline error on blur "Enter a reason of at least 10 characters." (A-54) |
| Message to member | Optional C-2 textarea, ≤ 500, helper "The message never names the reporter." |
| Warning action | Neutral styling; copy stresses no status/connection change (SC-21.1) |
| Restriction action | Amber confirm; copy says only sending requests is removed and all else continues (A-50, AC-21.3) |
| Temporary block | Red destructive confirm; effect list includes sessions, route, requests, connections, chats, leads and the note that **closures are not restored when it lifts** (BR-21.7, AC-21.4) |
| Permanent block | Red; **double confirmation**: a second step reads "This cannot be reversed in the application." and requires an explicit tick/button (BR-21.8, A-50, AC-21.6, §7.3 "Double confirm") |
| Already blocked | If the member is Permanently Blocked, the action options are disabled with "This member is already blocked. No further action is needed." (BR-21.15) |
| Proportionality | The dialog shows the member's current warnings/restrictions/blocks and current status, so Admin can choose a fitting step (BR-21.4) |
| Loading | Confirm shows a spinner, fields disabled, double-submit blocked |
| Error | Inline banner inside the dialog, keep the reason; specific copy for `END_DATE_REQUIRED`, `REASON_REQUIRED`, `MEMBER_ALREADY_BLOCKED`, `PERMANENT_BLOCK_FINAL`, `SAFETY_ACTION_INCOMPLETE` |
| Success | Dialog closes, toast, header status and history refresh; queue reflects the new status on return |
| Offline | Confirm disabled with the offline notice |
| Accessibility | Focus trap, labelled dialog, effects read in order, reason error announced, date field labelled, double-confirm announced |

---

### 4.5 Restricted and Blocked — `/admin/restricted` (W-12.4)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Restricted and Blocked                              [Export ]    │
│ Safety ◄   │ ┌ 4 restricted │ 2 temporarily blocked │ 1 permanently blocked │ │
│  Queue     │  Search [member, SAN or report no.__________]                     │
│  Restricted│  Type[All▼] Status[Active▼] Ending[dd-mmm..dd-mmm]  [ Clear ]    │
│  Flagged   │ ┌──────────┬─────────┬───────────────┬──────────┬─────────┬────┐ │
│  Moderation│ │Member    │Type     │Since / End    │Reason    │Report   │    │ │
│            │ ├──────────┼─────────┼───────────────┼──────────┼─────────┼────┤ │
│            │ │Rohit M.  │[Restr.] │09-Oct→08-Nov  │Spam…     │SAF-00009│Lift│ │
│            │ │Neha S.   │[Temp Bl]│10-Oct→24-Oct  │Harass…   │SAF-00012│Lift│ │
│            │ │Vikram T. │[Perm Bl]│12-Oct→—       │Fake…     │SAF-00015│ —  │ │
│            │ └──────────┴─────────┴───────────────┴──────────┴─────────┴────┘ │
│            │  Showing 1–25 of 7                                                 │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Count tiles | Restricted, temporarily blocked, permanently blocked; each drills into the list filtered by type |
| Columns | Member (name, MBR, status badge), Type, Since → End date (or "No end date"), Reason (Admin-only), Report number link, Lift action |
| End dates | Shown as `dd-mmm-yyyy`; a restriction with no end date reads "Until lifted"; a row past its end but not yet processed reads "Ended" and has no Lift |
| Lift | Only for Active Restriction and Temporary Block. **Permanent Block has no Lift control**; the cell shows "Cannot be reversed" (BR-21.8, AC-21.6) |
| Filters | Type (multi), Status (Active / Expired / Lifted), end-date range, search. Clear all |
| Empty | "No members are restricted or blocked." |
| Filtered empty | "No members match these filters." + Clear filters |
| Loading / Error | Skeleton rows / inline banner + Try again |
| Mobile | Cards: member, status, type, since/end, Lift; filters in a sheet |

### 4.6 Lift dialog (BR-21.6, BR-21.7, SC-21.8)

```text
┌  Lift restriction for Rohit M.? ───────────────────────────┐
│  This will:                                                │
│  • return the member to Active                             │
│  • let them send connection requests again                 │
│  • record the reason in the audit trail                    │
│  (i) Connections closed during a block are not restored.   │
│  Reason (at least 10 characters)                           │
│  [__________________________________________________]      │
│  0/500 · Recorded in the audit trail.                      │
│  [ Cancel ]                          [ Lift restriction ]  │
└────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Reversible only | The dialog title and confirm label change between "Lift restriction" and "Lift temporary block"; no Lift option exists for Permanent Block or Warning |
| Effects | Exact: status → Active, capability restored (requests for restriction; login for temporary block), audit. A fixed note says closures from a block are **not restored** (BR-21.7, BR-19.3) |
| Reason | Same C-2 reason rule (≥ 10, ≤ 500, counter, audit helper) |
| Notification | Copy says the member is told the restriction/block was lifted |
| Error | Inline for `PERMANENT_BLOCK_FINAL`, `SANCTION_NOT_ACTIVE`, `REASON_REQUIRED` |
| Success | Row updates to Lifted/Active; toast; tiles refresh |

---

### 4.7 Flagged members — `/admin/flagged` (SCR-21.5)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Flagged members                  Threshold: 3 different reporters│
│ Safety ◄   │  Search [member name or MBR__________]                            │
│  Queue     │  Sort [Most reporters▼]                                           │
│  Restricted│ ┌────────────────────────────────────────────────────────────────┐│
│  Flagged ◄ │ │ Rohit M.  MBR-000232  [Active]                                   ││
│  Moderation│ │ 3 different reporters · 4 reports · latest 10-Oct-2026           ││
│            │ │ Reasons: Inappropriate messages ×2 · Spam ×2                     ││
│            │ │ ▸ SAF-000009 · Inapprop. · Under Review · 09-Oct                  ││
│            │ │ ▸ SAF-000010 · Spam · Submitted · 10-Oct                          ││
│            │ │ [ Open reports ]  [ View member ]                                 ││
│            │ └────────────────────────────────────────────────────────────────┘│
│            │ ┌────────────────────────────────────────────────────────────────┐│
│            │ │ Neha S.   MBR-000231  [Restricted]  3 reporters · 3 reports …   ││
│            │ └────────────────────────────────────────────────────────────────┘│
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Definition | A member appears here when **CFG-15 different members** have reported them; the threshold is shown from config, never hard-coded (BR-21.11) |
| Card content | Member, MBR, status, distinct reporter count, total reports, latest report date, reason breakdown, and the report rows (link each to detail) |
| Open together | "Open reports" filters the report queue to this member so Admin can compare the reports (SC-21.6) |
| No chat here | Report rows link to the detail; no chat text appears on this screen |
| Empty | "No members are flagged. A member appears here when {CFG-15} different members report them." |
| Loading / Error | Skeleton cards / inline banner + Try again |
| Mobile | Cards stack; reason chips wrap; report rows collapse under a "Show reports" disclosure |

---

### 4.8 Rating moderation — `/admin/rating-moderation` (SCR-21.6)

```text
┌────────────┬──────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Rating moderation                                               │
│ Safety ◄   │  Status[Reported▼]  Search [rating or member______]  [ Clear ]   │
│  Queue     │ ┌ (!) Comment moderation is turned off. Hiding is unavailable. ┐ │
│  Restricted│ └───────────────────────────────────────────────────────────────┘ │
│  Flagged   │ ┌──────────┬────┬─────────────────────┬────────┬──────────┬────┐ │
│  Moderation│ │Rating no.│★   │Comment              │Rater   │Rated     │    │ │
│            │ ├──────────┼────┼─────────────────────┼────────┼──────────┼────┤ │
│            │ │RAT-000042│★3  │"…abusive text…"     │Amit K. │Rohit M.  │Hide│ │
│            │ │RAT-000041│★5  │[Comment hidden]     │Neha S. │Ashish K. │ —  │ │
│            │ └──────────┴────┴─────────────────────┴────────┴──────────┴────┘ │
│            │  Showing 1–25 of 6                                                 │
└────────────┴──────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Columns | Rating number, stars (C-14 read-only), comment (or "Comment hidden"), rater, rated member, report count, status, Hide action |
| Stars stay | Copy makes clear "Hiding the comment does not change the stars or the member's rating." (BR-21.13, BR-16.11) |
| Hide | Opens a reason dialog (≥ 10); on success the row shows "Comment hidden" and the stars remain visible |
| Moderation off | If CFG-14 is off, the queue is read-only and Hide is disabled with "Comment moderation is turned off." (M7 gate) |
| Empty | "No comments are awaiting review." |
| Loading / Error | Skeleton rows / inline banner + Try again |
| Mobile | Cards: rating no., stars, comment (truncated with expand), rater/rated, Hide |

### 4.9 Hide comment dialog

```text
┌  Hide this comment? ────────────────────────────────────────┐
│  Rater: Amit K.   Rated member: Rohit M.   ★3               │
│  Comment: "…abusive text…"                                  │
│  (i) The stars and the member's rating do not change.       │
│  Reason (at least 10 characters)                            │
│  [__________________________________________________]      │
│  0/500 · Recorded in the audit trail.                       │
│  [ Cancel ]                              [ Hide comment ]   │
└────────────────────────────────────────────────────────────┘
```

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton table/rows/tiles; header and filters stable; never a bare spinner |
| Empty | Per-screen copy with one next action; no fake data |
| Filtered empty | "No {records} match these filters." + Clear filters |
| Error (load) | Inline banner + Try again; keep filters/tab; block-level errors on detail |
| Error (action) | Inline in the dialog; keep the reason; specific code copy |
| Success | Toast + refresh header/status/history; queue/list reflects the change on return |
| Partial failure | "The action did not finish." with the applied steps; no false success (`SAFETY_ACTION_INCOMPLETE`) |
| Disabled | Action or confirm disabled with a visible reason nearby |
| No access | "You do not have access to this record." (no existence hint) |
| Not found | "Report not found." / "Member not found." with a back link |
| Overdue | Amber at CFG-32, red at 2× CFG-32; banner plus row styling; the word "Overdue" (BR-21.1) |
| Conversation closed | Button disabled with "The conversation can be opened only while the report is open." |
| Permanent block | No Lift control; "Cannot be reversed." (AC-21.6) |
| Moderation off | Info notice; Hide disabled with a reason (CFG-14) |
| Offline | Slim banner; write actions and export disabled with a reason; reads may show cached data |
| Session expired | Foundation dialog; return to login (BR-5.6) |
| Restricted/Inactive Admin | Cannot log in (BR-5.4) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Desktop 1024+ | Full data tables, sidebar, 12-column layout; detail in two columns (report/history left, actions/conversation right); dialogs centred; content fluid with a 1440 px text cap |
| Tablet 768–1023 | Tables collapse to fewer columns or cards; filters in a sheet; detail two columns; dialogs centred |
| Phone 360–767 | Cards with primary fields and flags; filters in a bottom sheet; detail stacks; conversation full-screen; action dialogs become full-height sheets; sticky action bar |

- Admin is desktop-first, but every screen must remain readable on a phone (UI Plan §7.5).
- Tables scroll inside their container on small screens; they never force page-level horizontal scroll.
- At 200% text zoom the page must remain usable; Admin tables may scroll inside their box.
- Sticky table header on long lists; state is preserved when opening a detail and coming back.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring; dialogs trap focus and return it to the triggering action |
| Labels | Every filter, date field, reason field and message field has a visible label; placeholders are examples |
| Targets | ≥ 44 × 44 px, 8 px apart |
| Colour | Status, reason and flag chips always carry words/icons; overdue has the word "Overdue", not colour alone |
| Keyboard | Search, filters, table sort, row open, tabs, action menu, end-date picker, reason/message fields, lift and hide dialogs, pager all work by keyboard |
| Screen readers | Announce dialog title, action choice, effect list, end-date error, reason validation, success; conversation messages announced in order with sender and time; table headers use `th` scope |
| Motion | Honour "reduce motion"; no decorative animation |
| Zoom | 200% text without horizontal page scroll; Admin tables scroll internally |
| Language | `lang="en"`; dates `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M12)

| Situation | Copy |
|---|---|
| Page titles | "Report queue" · "Report detail" · "Restricted and Blocked" · "Flagged members" · "Rating moderation" |
| Search (queue) | "Search by SAF number, reporter or member" |
| Active filters | "{n} filters applied" |
| Clear all | "Clear all" |
| Overdue banner | "{n} reports are overdue (older than {CFG-32} days)." |
| Overdue row | "Overdue" |
| Report actions | "Mark Under Review" · "Hold route under review" · "Release route" · "Take action" · "Dismiss" |
| Reporter history | "Raised {a} · Dismissed {b} · Action taken {c} · Open {d}" |
| Reported member history | "Warnings {a} · Restrictions {b} · Blocks {c}" |
| Conversation button | "Open conversation" |
| Conversation notice | "Opening it is logged with your name and time." |
| Conversation unavailable | "The conversation can be opened only while the report is open." |
| No conversation | "No conversation is attached to this report." |
| Reader notice | "This view is read-only. It is recorded with your name and the time." |
| Action dialog title | "Take action on {name}" |
| Action options | "Warning — record only, no change to status" · "Restriction — cannot send connection requests" · "Temporary block" · "Permanent block" |
| End date label | "End date" |
| End date hint | "Restriction: optional. Temporary block: required." |
| End date error | "The end date must be in the future." |
| What this does | "What this does" |
| Warning effect | "Record a warning; status and connections do not change." |
| Restriction effect | "Set the member to Restricted; they cannot send connection requests; all else continues." |
| Temporary block effect | "Set the member to Temporarily Blocked; end sessions; switch off the route; withdraw requests; disconnect connections; close chats; withdraw leads." |
| Temporary block note | "Route and connections are not restored when the block lifts." |
| Permanent block warning | "This cannot be reversed in the application." |
| Reason label | "Reason (at least 10 characters)" |
| Reason helper | "Recorded in the audit trail." |
| Reason error | "Enter a reason of at least 10 characters." |
| Message label | "Message to the member (optional)" |
| Message helper | "The message never names the reporter." |
| Apply | "Apply action" |
| Dismiss helper | "The report record stays. The reporter is told it was reviewed." |
| Already blocked | "This member is already blocked. No further action is needed." |
| Lift title | "Lift restriction for {name}?" / "Lift temporary block for {name}?" |
| Lift effect | "Return the member to Active and record the reason." |
| Lift note | "Connections closed during a block are not restored." |
| Lift button | "Lift restriction" / "Lift temporary block" |
| Permanent block cell | "Cannot be reversed." |
| Flagged threshold | "A member appears here when {CFG-15} different members report them." |
| Flagged empty | "No members are flagged." |
| Moderation notice | "Hiding the comment does not change the stars or the member's rating." |
| Moderation off | "Comment moderation is turned off." |
| Hide button | "Hide comment" |
| Hide dialog title | "Hide this comment?" |
| Hide reason label | "Reason (at least 10 characters)" |
| Partial failure | "The action did not finish. Review the applied steps and try again." |
| No access | "You do not have access to this record." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load report queue | `GET /admin/safety/reports?status=&reason=&overdueOnly=&flaggedOnly=&memberStatus=&search=&submittedFrom=&submittedTo=&sort=&page=&pageSize=` |
| Queue filter options | `GET /admin/safety/reports/options` |
| Summary tiles | `GET /admin/safety/summary` (or the `summary` block of the list) |
| Open report detail | `GET /admin/safety/reports/{reportId}` |
| Open conversation | `GET /admin/safety/reports/{reportId}/conversation` |
| Member sanction history | `GET /admin/safety/members/{memberId}/sanctions` |
| Reports by/against member | `GET /admin/safety/members/{memberId}/reports` |
| Action dialog context | `GET /admin/safety/reports/{reportId}/actions/context?action={Warning|Restriction|TemporaryBlock|PermanentBlock|Dismiss}` |
| Mark Under Review | `POST /admin/safety/reports/{reportId}/mark-under-review` |
| Apply action | `POST /admin/safety/reports/{reportId}/action` `{ action, endDate, reason, messageToMember }` |
| Dismiss report | `POST /admin/safety/reports/{reportId}/dismiss` `{ reason }` |
| Close report | `POST /admin/safety/reports/{reportId}/close` |
| Hold route | `POST /admin/safety/reports/{reportId}/hold-route` `{ reason }` |
| Release route | `POST /admin/safety/reports/{reportId}/release-route` `{ reason }` |
| Load restricted & blocked | `GET /admin/safety/restricted-blocked?type=&status=&endingFrom=&endingTo=&search=&sort=&page=&pageSize=` |
| Restricted & blocked options | `GET /admin/safety/restricted-blocked/options` |
| Lift sanction | `POST /admin/safety/sanctions/{sanctionId}/lift` `{ reason }` |
| Load flagged members | `GET /admin/safety/flagged?search=&sort=&page=&pageSize=` |
| Load rating moderation | `GET /admin/safety/rating-moderation?status=&search=&submittedFrom=&submittedTo=&page=&pageSize=` |
| Rating moderation options | `GET /admin/safety/rating-moderation/options` |
| Hide comment | `POST /admin/safety/rating-moderation/{ratingId}/hide` `{ reason }` |
| Export | `POST /admin/safety/reports/export` · `/restricted-blocked/export` · `/flagged/export` |

Client rules:

- Always send the Admin token; never accept a report or member id except from the route path.
- On `403` → generic no-access; on `404` → "{Record} not found."; never guess which.
- On `409 REPORT_STATUS_INVALID` → refresh the detail and show the current status in the dialog.
- On `409 MEMBER_ALREADY_BLOCKED` → close the functional action options with the reason.
- On `409 PERMANENT_BLOCK_FINAL` → never offer Lift for a permanent block; show "Cannot be reversed."
- On `400 REASON_REQUIRED` / `END_DATE_*` → focus the field and keep the dialog open.
- After a successful action, refresh the header, actions, history and conversation rules; the queue reflects the new status on return.
- Never render chat text outside the conversation viewer; there is no such field in any other M12 response.
- Status and sanction badges use the shared status map (UI Plan §3.7); never invent colours.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M8 report data and lifecycle | Queue/detail show only what the gateway returns; missing sections show a neutral empty state |
| M6 conversation access | The button is disabled with "The conversation is not available." and no chat is shown |
| M7 rating moderation | The queue shows "Rating moderation is not available yet."; Hide disabled |
| M10 member status actions | Functional actions remain disabled with a neutral reason; no fake success |
| M2/M11 route hold/release | Hide the hold/release buttons until the gateway answers; the rest of the report still works |
| M9 notifications | The action still succeeds if delivery is queued; do not block the dialog on email |
| M14 dashboard tiles | The summary endpoint still works; the dashboard can link into the filtered queue |
| CFG-14/15/32 | Copy and thresholds read from the API; never hard-code |
| Shared C-28/C-29/C-34 | Reuse from M10/M11 if already present; otherwise build here |

---

## 11. Delivery plan (M12 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Reuse/build C-28 data table, C-29 stat tiles, C-34 timeline, C-14 read-only stars | UI Plan §3 |
| 2 | Report queue: summary tiles, filters, overdue styling, columns, flags, empty/error/loading | API 1, 2, 3 |
| 3 | Report detail: header, report/reporter/reported/related blocks, timeline | API 4 |
| 4 | Conversation viewer with logging notice and read-only messages | API 5 |
| 5 | Take action dialog: action choice, effects, end date, reason, message | API 8 |
| 6 | Warning/Restriction submit and refresh | API 10 |
| 7 | Temporary block and Permanent block double-confirm | API 10 |
| 8 | Mark Under Review, Dismiss, Close | API 9, 11, 12 |
| 9 | Route hold/release from the report | API 13, 14 |
| 10 | Restricted and Blocked list + Lift dialog | API 15, 16, 17 |
| 11 | Flagged members with reason breakdown and report links | API 18 |
| 12 | Rating moderation list + Hide dialog | API 19, 20, 21 |
| 13 | Export dialog for queue/lists | API 22–24 |
| 14 | Responsive phone/tablet pass; Admin density | — |
| 15 | Accessibility pass: dialogs, date field, effects list, conversation order, announcements | — |
| 16 | QA: partial failure, permanent-block finality, reporter hidden, overdue styling, 200% zoom, slow network | API running |

Suggested route files (framework-agnostic naming): `admin/safety/index`, `admin/safety/[id]`, `admin/restricted/index`, `admin/flagged/index`, `admin/rating-moderation/index`, plus dialogs `TakeActionDialog`, `LiftSanctionDialog`, `HideCommentDialog`, `ConversationViewer`, `RouteHoldDialog`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-21.1 | The conversation opens only from an open report, is read-only, shows the logging notice, and has no access when the report is closed |
| AC-21.2 | A temporary block shows its end date and can be lifted; the UI states it lifts itself |
| AC-21.3 | The restriction copy says only sending requests is removed; all else continues |
| AC-21.4 | The temporary/permanent block effects list names route, requests, connections and chats |
| AC-21.5 | No notice, message or screen sent to the member names the reporter |
| AC-21.6 | No Lift or reversal control exists for a Permanent Block; "Cannot be reversed." is shown |
| AC-21.7 | Take action, Dismiss and Lift confirm is disabled until the reason has 10 non-space characters |
| BR-21.1 | Oldest first; overdue reports are highlighted in amber/red with the word "Overdue"; nothing is deletable |
| BR-21.2 | Under Review is available and optional; the detail explains it when starting an investigation |
| BR-21.3 | Every conversation open is logged and the UI says so before opening |
| BR-21.4 | The detail shows prior warnings/restrictions/blocks before Admin chooses a step |
| BR-21.5 | Every action has a reason, audit helper and notices |
| BR-21.6 | Restriction end date optional; early lift available with reason |
| BR-21.7 | Temporary block end date mandatory; lifting does not restore closures |
| BR-21.8 | Permanent block double confirm; no reversal control |
| BR-21.9 | Block effects shown are the same closures as deactivation |
| BR-21.10 | Reporter visible to Admin only; never in member-facing copy |
| BR-21.11 | Flagged list uses CFG-15 different reporters and shows the threshold |
| BR-21.12 | Reporter's dismissed count is shown on the detail |
| BR-21.13 | Hide comment requires a reason and states stars are unchanged |
| BR-21.15 | A report about an Inactive/Blocked member can still be reviewed and closed; functional actions are disabled with a reason |
| BR-21.16 | No control exists to create a report |
| UP-5 | Every action states exact effects from real counts |
| UP-10 | Loading, empty, filtered-empty, error, no-access, not-found, disabled, offline, partial-failure and success states are designed |
| UP-11 | Contrast, focus, labels, targets, keyboard and screen-reader announcements pass |
| UP-12 | Admin screens are dense and deliberate; no bulk actions |
| — | Dates use `09-Oct-2026`, times `8:05 AM`, India time |
| — | Status and sanction badges use the shared map, always text plus colour |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| UD-UI-1 | Queue default sort | Oldest first; Overdue-first is a filter/sort option |
| UD-UI-2 | Detail layout: tabs or blocks? | One page of blocks (Report · Reporter · Reported · Related · Conversation · Timeline), matching W-12.2 |
| UD-UI-3 | Conversation: inline block or modal? | Modal/full-height sheet with the logging notice; closed by default |
| UD-UI-4 | Take action dialog or separate page? | One dialog with action radios, effects, end date and reason |
| UD-UI-5 | Restriction end-date optional? | Yes; leave empty for "until lifted" |
| UD-UI-6 | Permanent block confirm style | Double confirmation with an explicit "I understand it cannot be reversed" step |
| UD-UI-7 | Where is route hold/release? | Buttons on the report detail, separate from Take action |
| UD-UI-8 | Show warnings on Restricted & Blocked? | No; that screen is restrictions/blocks only — warnings stay in the member detail history |
| UD-UI-9 | Flagged members: card or table? | Cards on desktop with an expandable report list; stacked cards on phone |
| UD-UI-10 | Rating moderation off | Show an info notice; the queue is read-only and Hide is disabled |
| UD-UI-11 | Show the reporter's identity in the queue? | Yes, Admin-only; never in member-facing copy (BR-21.10) |
| UD-UI-12 | Overdue colour thresholds | Amber at CFG-32, red at 2× CFG-32; always with the word "Overdue" |
| UD-UI-13 | Hidden comment display | Show "Comment hidden" in place of the text; keep stars visible |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-21.1 Report Queue | §2, §4.1 |
| SCR-21.2 Report Detail | §2, §4.2 |
| SCR-21.3 Take Action | §2, §4.4 |
| SCR-21.4 Restricted & Blocked | §2, §4.5–4.6 |
| SCR-21.5 Flagged Members | §2, §4.7 |
| SCR-21.6 Rating Moderation | §2, §4.8–4.9 |
| Report Detail blocks | §4.2 |
| Conversation block | §4.2–4.3 |
| Take Action fields | §4.4 |
| What each action does | §4.4, §4.6 (effects) |
| BR-21.1 never delete / oldest first / overdue | §4.1, §5 |
| BR-21.2 Under Review optional | §4.2 |
| BR-21.3 conversation logged | §4.2–4.3 |
| BR-21.4 proportionate | §4.2, §4.4 |
| BR-21.5 reason + audit + notices | §4.4 |
| BR-21.6 restriction scope/lift | §4.4–4.6 |
| BR-21.7 temporary block lift/closure | §4.4–4.6 |
| BR-21.8 permanent final | §4.4–4.5 |
| BR-21.9 block effects | §4.4 |
| BR-21.10 reporter hidden | §4.2, §4.4, §8 |
| BR-21.11 flagged members | §4.7 |
| BR-21.12 dismiss reason + history | §4.2, §4.4 |
| BR-21.13 rating moderation | §4.8–4.9 |
| BR-21.15 already Inactive/Blocked | §4.2, §4.4, §5 |
| BR-21.16 no Admin report | §1, §2 |
| SC-21.1…SC-21.11 | §4, §9 |
| AC-21.1…AC-21.7 | §12 |
| Data Impact | §4.4, §4.6 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4.2 Admin navigation | §2 |
| UI Plan §6.12 W-12.1…W-12.4 | §4.1–4.9 |
| UI Plan §7.1 states | §5 |
| UI Plan §7.3 confirmations | §4.4, §4.6 |
| UI Plan §7.4 lists/filters/paging | §4.1, §4.5, §4.7, §4.8 |
| UI Plan §8 delivery | §11 |
| Flow F-8 Admin handles a safety report | §4.1–4.4 |
