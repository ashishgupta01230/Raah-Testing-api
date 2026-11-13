# M10 — Member Management & Verification — UI Plan

| | |
|---|---|
| **Module** | M10 — Member Management & Verification (spec `docs/spec/10_M10_Member_Management.md`, functional specification Section 19) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, Admin navigation §4.2, flows §5, module design §6.10 (W-10.1, W-10.2, W-10.3), patterns §7 (tables §7.4, confirmations §7.3, states §7.1), delivery §8 |
| **Platform** | Admin screens **desktop first**, then tablet and phone (D-20). No member UI in this module |
| **Companion** | API plan: `10_M10_Member_Management_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M10 gives Admin a fast, calm member control room:

1. **SCR-19.1 Member list** — search, filters, sortable columns, saved views, export.
2. **SCR-19.2 Member detail** — profile, account, read-only routes, activity, safety summary and history in tabs.
3. **SCR-19.3 Change member status** — deactivate or reactivate, with a mandatory reason.
4. **SCR-19.4 Account actions** — unlock, mark email verified, send password reset, correct gender, remove photo.

UI jobs for this module:

- **Deliberate, never bulk.** Every action is one member at a time and carries a reason (UP-12, BR-19.1).
- **Show the consequences before acting.** The dialog names exactly what will happen to sessions, route, requests, connections, chats and leads (UP-5, BR-19.2).
- **Read-only where the member owns the data.** No control exists to edit name, email, mobile, route, leads, messages or ratings (BR-19.9, AC-19.4).
- **Privacy visible in the pattern.** Mobile/email are Admin-visible but exports mask them unless confirmed (BR-19.12).
- **Every state designed** — loading, empty, filtered-empty, error, no access, not found, action loading, partial failure, success (UP-10).
- **Admin density, calm use.** Dense tables, keyboard-friendly, clear audit feel (UP-12).

Out of scope for M10 screens (linked, owned elsewhere): route deactivation and route detail (M11), restrict/block (M12), report detail (M12), config (M13), reports catalogue (M15), notifications centre (M9).

---

## 2. Navigation and routes

M10 lives in the Admin sidebar group **Members** (UI Plan §4.2). The Admin top bar keeps global search, the notification bell and the Admin menu.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Member list | `/admin/members` | SCR-19.1 | Admin sidebar "Members"; global search result |
| Member detail | `/admin/members/[id]` | SCR-19.2 | Row in the list; notification deep link |
| Change status dialog | modal on detail | SCR-19.3 | Actions menu → Deactivate / Reactivate |
| Account action dialog | modal on detail | SCR-19.4 | Actions menu → Unlock / Mark verified / Send reset / Correct gender / Remove photo |
| Export dialog | modal on list | SCR-19.1 Export | "Export" button |

**Hard route rule:** there is no `/admin/members/[id]/edit` and no delete route. If someone types one, show the generic no-access page rather than a form.

Admin gates (Foundation): Admin login only (D-19, BR-5.11); Inactive/Blocked Admin cannot log in (BR-5.4); the Admin UI is a separate bundle from the member app (plan §9.5).

---

## 3. Screen inventory and components

| Screen / element | Components used (UI Plan §3.6) |
|---|---|
| Member list | C-28 Data table (sort, column chooser, saved filters, export), C-27 Filter bar, C-2 Search field, C-4 Select, C-12 Status/role/verification/flag chips, C-25 Empty state, C-26 Skeleton, C-22 Toast, C-35 numbered pager |
| Member detail header | C-13 Avatar, C-12 badges, C-1 buttons, C-11 tabs, C-23 dialog trigger |
| Detail tabs | C-33 Key-value list, C-29 Stat tiles (activity), C-34 History timeline, C-28 table (login history), C-15 Route line (read-only), C-21 Banner |
| Actions menu | C-1 tertiary/dropdown, C-23 dialogs |
| Account action dialog | C-23 Confirm dialog, C-2 Text field (reason), C-4 Select (gender), C-31 photo preview (remove), C-21 warning banner, C-22 toast |
| Export dialog | C-23, C-6 checkbox, C-21 info/warning banner, C-1 |
| Flags | C-12 chips with words, never colour alone |

Build C-28/C-34/C-29 here if they do not exist yet; they are shared Admin components and will be reused by M11–M15.

---

## 4. Screen designs

### 4.1 Member list — `/admin/members` (W-10.1)

```text
┌────────────┬─────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Members                                      [Export ▼]    │
│ Dashboard  │  Search [name, email, mobile, MBR no.__________]            │
│ Members ◄  │  Status[All▼] Role[All▼] Verified[All▼] Flags[Any▼] Date[..]│
│ Routes ▸   │  [ Filters (2) ]  [ Clear all ]            [ Columns ▾ ]    │
│ Safety ▸   │ ┌───────┬─────────┬──────────┬──────┬────────┬─────┬──────┐ │
│ Settings ▸ │ │MBR no │ Name    │ Email    │ Role │ Status │ Last│Route │ │
│ Reports    │ ├───────┼─────────┼──────────┼──────┼────────┼─────┼──────┤ │
│            │ │000231 │ Ashish K│ a@mail   │Saathi│ Active │ 09-O│ Yes  │ │
│            │ │000232 │ Neha S. │ n@mail   │Raahi │ Active │ 09-O│ Yes  │ │
│            │ │000233 │ Rohit M.│ r@mail   │Raahi │ Inactive│ —  │ No   │ │
│            │ └───────┴─────────┴──────────┴──────┴────────┴─────┴──────┘ │
│            │  Showing 1–25 of 1,204            [ < 1 2 3 … 49 > ]        │
└────────────┴─────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Search | One box over name, email, mobile and `MBR-` number; debounce 300 ms; clear icon |
| Filters | Status, Journey role, Verification, Flags, Registered date range; active count + Clear all (§7.4) |
| Columns | Default: MBR no., Name, Email, Role, Vehicle, Status, Verification, Registered, Last login, Active route. Column chooser persists per Admin (C-28) |
| Sort | MBR, name, registered, last login; show the sort arrow in the header; default Registered newest |
| Status | Badge text + colour from UI Plan §3.7 (Active/Unverified/Inactive/Restricted/Temporary/Permanent) |
| Flags | Chips with words: "Locked", "Open report", "Flagged", "Inactive with open items". A row may carry several; never colour alone |
| Row | Whole row clickable/keyboard-activatable → detail; visible focus |
| Export | Opens the export dialog (§4.3); never exports silently |
| Bulk actions | **None.** No checkboxes for actions, no bulk bar (BR-19.1) |
| Pager | Numbered pager with total; page size fixed at 25 by default (Admin pattern §7.4) |
| Empty (no members) | "No members yet." + "Members appear here after registration." |
| Filtered empty | "No members match these filters." + "Clear filters" |
| Loading | Skeleton table rows, header stable |
| Error | Inline banner "Could not load members." + Try again; filters stay |
| Offline | Slim banner; Export disabled with a reason; list may show cached page |
| No access | "You do not have access to this record." (generic; no hint) |
| Mobile | Table becomes stacked cards with MBR, name, status, role, last login and a flag row; filters open in a bottom sheet; pager stays |

**Row flags are important for triage:** Locked account, Has open report, Flagged by CFG-15, Inactive but holding open items. Show them as a second line under the name on narrow screens.

---

### 4.2 Member detail — `/admin/members/[id]` (W-10.2)

```text
┌──────────────────────────────────────────────────────────────────────┐
│ ← Members     (Av) Ashish Kumar   MBR-000231  [Active] [Saathi]      │
│ [Profile][Account][Route][Activity][Safety][History]      [Actions ▾]│
├──────────────────────────────────────────────────────────────────────┤
│ Photos / facts                          Account                       │
│ Photo     (circle 72)                   Status        Active          │
│ Full name Ashish Kumar                  Verified      Yes ✓           │
│ Gender    Male                          Locked        No              │
│ Email     ashish@mail.com               Failed logins 0               │
│ Mobile    98•••••210  [ Show ]           Last login    09-Oct 08:30    │
│ Journey   Saathi                        Member since  12-Oct-2026     │
│ Vehicle   —                                                           │
├──────────────────────────────────────────────────────────────────────┤
│ (!) Chat content is not available here.                              │
└──────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | Back to Members, avatar, name, `MBR-`, status badge, role chip; actions menu right |
| Tabs | **Profile · Account · Route · Activity · Safety · History** (hash routes keep the tab on refresh) |
| Profile tab | Read-only facts; photo; email; mobile masked with **Show** (reveals locally; no edit) |
| Account tab | Status, verification, lock flag, failed-login count, last login, login-history table (paged) |
| Route tab | Current Active route (both legs, route line C-15) and past routes, read-only; if none: "No active route." with a link to the Route monitor (M11) |
| Activity tab | Stat tiles: leads, requests sent, requests received, connections by status, chats (count), ratings (average + count) |
| Safety tab | Counts/statuses only: reports against, reports raised, warnings, restrictions, blocks; link to the Safety module for detail. **No report text here** |
| History tab | Timeline (C-34): date/time, Admin, action, old → new, reason. Newest first |
| Chat notice | Always-visible info banner: "Chat content is not available here. It can be opened only inside an open safety report." (BR-4.7, SC-19.9) |
| No edit controls | There is no edit name/email/mobile/route/message/rating control anywhere (BR-19.9, AC-19.4) |
| No delete | No delete menu item anywhere (BR-19.10) |
| Restrict/Block | Not in the Actions menu; a link reads "Restrictions and blocks are handled in the Safety module." (BR-19.11) |
| Mobile | Header stacks; tabs scroll horizontally; facts become a key-value list; tables become cards |
| States | Skeleton per tab; tab error with retry; 404 "Member not found."; no-access page for a member token; offline read-only |

**Mobile number.** Show `98•••••210` with a **Show** button. The value is Admin-visible (D-21, AC-19.5) but should not be shoulder-surfable by default. Toggling Show is local in Phase 1; if product wants an audit, use the optional contact endpoint (API plan UD-API-4).

---

### 4.3 Export dialog

```text
┌  Export members ─────────────────────── ┐
│  Current filters:                       │
│   Status: Active · Role: All · Flags: 1 │
│  Rows: 1,204                            │
│  [ ] Include contact details            │
│  (i) Email and mobile are masked by     │
│      default. Including them is         │
│      recorded in the audit trail.       │
│  [ Cancel ]            [ Export CSV ]   │
└─────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Filters summary | Show the active filters and the row estimate so the Admin knows what is being exported |
| Contact details | Unchecked by default; when checked, show an inline warning and require the confirmation; the API also requires it |
| Masked preview | A small sample line shows `a•••@mail.com` / `98•••••210` when unchecked |
| Large export | If the API refuses with `EXPORT_TOO_LARGE`, show "Too many rows. Narrow the filters." and keep the dialog open |
| Audit | Copy says the export (and contact inclusion) is recorded |
| Offline | Export disabled |
| Success | Browser downloads the file; toast "Export started." |

---

### 4.4 Actions menu and action availability

```text
                         [ Actions ▾ ]
                         ┌───────────────────────────┐
                         │ Deactivate member          │  ← red text
                         │ Reactivate member          │  (disabled here)
                         │ Unlock account             │
                         │ Mark email verified        │
                         │ Send password reset        │
                         │ Correct gender…            │
                         │ Remove photo               │
                         ├───────────────────────────┤
                         │ Restrictions and blocks →  │  ← Safety module
                         └───────────────────────────┘
```

| Aspect | Design |
|---|---|
| State-aware | Only valid actions are enabled; invalid ones are hidden or shown disabled with a nearby reason ("Member is not locked") |
| Order | Deactivate/Reactivate first, then Unlock, Mark verified, Send reset, Correct gender, Remove photo; safety link last |
| Destructive | Deactivate uses red text/icon; other actions are neutral |
| Keyboard | Menu opens with Enter/Space, arrow keys move, Esc closes, focus returns |
| No bulk | The menu acts on the open member only |

---

### 4.5 Account action dialog — shared pattern (W-10.3)

```text
┌  Deactivate Ashish Kumar? ──────────────┐
│  This will:                             │
│  • end his open sessions                │
│  • switch off his route (RTE-000418)    │
│  • withdraw his pending requests (2)    │
│  • disconnect his connections (1)       │
│  • close his chats (1)                  │
│  • withdraw leads both ways             │
│  • set the account to Inactive          │
│                                         │
│  Reason (at least 10 characters)        │
│  [________________________________]     │
│  0/500                                  │
│  Recorded in the audit trail.           │
│  [ Cancel ]         [ Deactivate ]      │
└─────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| One pattern | Same dialog for every action; title, effects list and confirm label change with the action (UI Plan §7.3 "Dialog with reason") |
| Effects | Built from the API context with real counts; never vague ("some things may change") |
| Reason | C-2 textarea, visible label, live counter, helper "Recorded in the audit trail."; confirm disabled until trimmed length ≥ 10; inline error on blur "Enter a reason of at least 10 characters." (V-45) |
| Confirm | Destructive actions use red; Cancel is the safe default and receives initial focus |
| Loading | Confirm shows a spinner, dialog fields disabled, double-submit blocked |
| Error | Inline banner inside the dialog, keep the reason; specific copy for `ACTION_NOT_ALLOWED` and incomplete deactivation |
| Success | Dialog closes, toast, detail and History tab refresh |
| Offline | Confirm disabled with the offline notice |
| Accessibility | Focus trap, labelled dialog, reason error announced, effects list read in order |

**Per-action copy:**

| Action | Title | Effects / caution | Confirm |
|---|---|---|---|
| Deactivate | "Deactivate {Name}?" | End sessions; switch off route; withdraw pending requests; disconnect connections; close chats; withdraw leads; set Inactive | "Deactivate" (red) |
| Reactivate | "Reactivate {Name}?" | "The member can log in again. Their route stays Inactive until they switch it on. Closed connections stay closed. A reactivation email will be sent." | "Reactivate" |
| Unlock | "Unlock {Name}?" | "The member can try logging in again immediately." | "Unlock" |
| Mark email verified | "Mark {Name}'s email as verified?" | "The account becomes Active as if the member opened the verification link. Use this only when the email could not reach them." (amber warning) | "Mark verified" |
| Send password reset | "Send a password reset to {Name}?" | "We will send the standard reset email. You will not see or set the password." | "Send reset email" |
| Correct gender | "Correct {Name}'s gender" | "Current: Male. Choose the correct value. This is recorded with your reason and applies to filters from the next search." | "Save correction" |
| Remove photo | "Remove {Name}'s photo?" | Show the current photo; "The photo is removed immediately and the member is notified. The avatar falls back to initials." | "Remove photo" (red) |

The **Correct gender** dialog adds a required select (Male / Female / Other) above the reason.

**Mark email verified** and **Send password reset** must feel like exception paths, not routine: use an amber caution banner and place them lower in the menu.

---

### 4.6 History tab (C-34)

```text
│ History                                                     [ Show more ] │
│ 09-Oct-2026 08:42   Admin: Priya S.                                        │
│ Unlocked account                                                           │
│ Locked → Unlocked · Reason: Member called support                           │
│ ────────────────────────────────────────────────────────────────────────── │
│ 08-Oct-2026 17:10   Admin: Priya S.                                        │
│ Marked email verified                                                       │
│ Unverified → Active · Reason: Email delivery failed; identity confirmed     │
```

- Newest first; date `09-Oct-2026`, time `8:05 AM` (India).
- Each entry: actor, action, old → new, reason.
- Read-only; no edit or delete of history (BR-8.3).
- Empty: "No account actions yet."
- Paged with Show more.

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton table/rows/tiles; header stable; never a bare spinner |
| Empty | Per-tab copy with one next action; no fake data |
| Filtered empty | "No members match these filters." + Clear filters |
| Error (load) | Inline banner + Try again; keep filters/tab |
| Error (action) | Inline in the dialog; keep the reason; specific code copy |
| Success | Toast + refresh detail/history; deactivation success states the new status |
| Partial failure | "Deactivation did not finish." with the applied steps; no false success |
| Disabled | Action or confirm disabled with a visible reason nearby |
| No access | "You do not have access to this record." (no existence hint) |
| Not found | "Member not found." with Back to Members |
| Offline | Slim banner; Export and actions that write disabled with a reason; reads may show cached data |
| Session expired | Foundation dialog; return to login (BR-5.6) |
| Restricted/Inactive Admin | Cannot log in (BR-5.4) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Desktop 1024+ | Full data table, sidebar, 12-column layout; detail tabs; dialogs centred; Admin content fluid with a 1440 px text cap |
| Tablet 768–1023 | Table collapses to fewer columns or cards; filters in a sheet; detail two columns; dialogs centred |
| Phone 360–767 | Cards with MBR, name, status, role, last login and flags; filters in a bottom sheet; detail header stacks; tabs horizontally scroll; actions in a full-width menu/card list; dialogs become full-height sheets |

- Admin is desktop-first, but every action must remain usable on a phone (UI Plan §7.5).
- Tables scroll inside their container on small screens; they never force the page to scroll horizontally.
- At 200% text zoom the page must remain usable; Admin tables may scroll inside their box.
- Sticky table header on long member lists.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring; dialogs trap focus and return it to the Actions trigger |
| Labels | Every filter and the reason field have visible labels; placeholders are examples |
| Targets | ≥ 44 × 44 px, 8 px apart |
| Colour | Status/role/flag badges always carry words; no colour-only meaning |
| Keyboard | Search, filters, table sort, row open, tabs, Actions menu, reason field, export checkbox and pager work by keyboard |
| Screen readers | Announce dialog title, effect list, reason validation, action success; table headers use `th` scope; row action is a real button/link |
| Motion | Honour "reduce motion"; no decorative animation |
| Zoom | 200% text without horizontal page scroll; Admin tables scroll internally |
| Language | `lang="en"`; dates `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M10)

| Situation | Copy |
|---|---|
| Page title | "Members" |
| Search placeholder | "Search by name, email, mobile or MBR number" |
| Active filters | "{n} filters applied" |
| Clear all | "Clear all" |
| Export | "Export" |
| Export title | "Export members" |
| Export masked note | "Email and mobile are masked by default. Including them is recorded in the audit trail." |
| Include contact details | "Include contact details" |
| Export too large | "Too many rows. Narrow the filters." |
| Empty list | "No members yet." |
| Filtered empty | "No members match these filters." |
| Load error | "Could not load members." |
| Detail not found | "Member not found." |
| Chat notice | "Chat content is not available here. It can be opened only inside an open safety report." |
| Mobile reveal | "Show" / "Hide" |
| Actions | "Actions" |
| Reason label | "Reason (at least 10 characters)" |
| Reason helper | "Recorded in the audit trail." |
| Reason error | "Enter a reason of at least 10 characters." |
| Deactivate notice | "This will: end open sessions, switch off the route, withdraw pending requests, disconnect connections, close chats, withdraw leads, set the account to Inactive." |
| Reactivate notice | "The member can log in again. Their route stays Inactive until they switch it on. Closed connections stay closed. A reactivation email will be sent." |
| Unlock notice | "The member can try logging in again immediately." |
| Mark verified caution | "Use this only when the verification email could not reach the member." |
| Password reset notice | "We will send the standard reset email. You will not see or set the password." |
| Correct gender | "Current: {gender}. Choose the correct value." |
| Remove photo | "The photo is removed immediately and the member is notified." |
| Action success | "{Action} completed." |
| Action not allowed | "This action is not available for the member's current status." |
| Partial failure | "Deactivation did not finish. Review the applied steps and try again." |
| No access | "You do not have access to this record." |
| Safety link | "Restrictions and blocks are handled in the Safety module." |
| History empty | "No account actions yet." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load member list | `GET /admin/members?search=&status=&role=&verification=&flags=&registeredFrom=&registeredTo=&sort=&page=&pageSize=` |
| Load filter options | `GET /admin/members/options` |
| Open member detail | `GET /admin/members/{id}` |
| Route tab | `GET /admin/members/{id}/routes` |
| Activity tab | `GET /admin/members/{id}/activity` |
| Safety tab | `GET /admin/members/{id}/safety` |
| Account tab login history | `GET /admin/members/{id}/login-history` |
| History tab | `GET /admin/members/{id}/history` |
| Open action dialog | `GET /admin/members/{id}/actions/context?action={action}` |
| Confirm action | `POST /admin/members/{id}/actions` `{ action, reason, newGender? }` |
| Export | `POST /admin/members/export` `{ filters, includeContactDetails, confirmContactDetails }` |
| Reveal contact (optional) | `GET /admin/members/{id}/contact` |

Client rules:

- Always send the Admin token; never accept a member id from a query for an action other than the route.
- On `403` → generic no-access; on `404` → "Member not found."; never guess which.
- On `409 ACTION_NOT_ALLOWED` → refresh the detail and show the current status in the dialog.
- On `400 REASON_REQUIRED` → focus the reason field and keep the dialog open.
- After any successful action, refresh the detail header, available actions and History tab.
- Refresh the list after leaving a detail so status changes are reflected.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M1 profile fields | Detail shows what the profile endpoint returns; missing photo → initials |
| M2/M11 route tab | Show "No active route." and a link to the Route monitor |
| M4/M5 activity counts | Stat tiles show `0` or "—" with a neutral note; never fabricate |
| M5/M6 connection and chat counts | Show counts only; no chat content ever |
| M7 ratings | "New member" or "—" until ratings exist |
| M8/M12 safety summary | Counts/statuses; link to the Safety module |
| M9 notifications | Reactivate email, photo-removed notice, welcome; the action still succeeds if delivery is queued |
| M12 restrict/block | Actions menu shows the safety link, not the actions (BR-19.11) |
| CFG-15/9/10/11 | Filter flag and dialog copy read config from the API |
| Data-table component (C-28) | Build it here for reuse in M11–M15 |

---

## 11. Delivery plan (M10 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Build/reuse C-28 data table (sort, column chooser, saved filters, pager), C-34 timeline, C-29 tiles | UI Plan §3 |
| 2 | Member list: search, filters, flags, columns, empty/error/loading | API 1, 2 |
| 3 | Member detail: header, tabs, read-only facts, chat notice | API 3 |
| 4 | Account tab login history; History timeline | API 7, 8 |
| 5 | Route/Activity/Safety tabs | API 4, 5, 6 |
| 6 | Actions menu with availability and disabled reasons | API 3 |
| 7 | Shared account-action dialog + context effects + reason validation | API 9, 10 |
| 8 | Deactivate partial-failure and success states | API 10 |
| 9 | Export dialog with masking, confirmation and audit note | API 11 |
| 10 | Responsive phone/tablet pass; Admin density | — |
| 11 | Accessibility pass: table, menu, dialog focus, reason counter, announcements | — |
| 12 | QA: permissions, no-edit/no-delete, contact masking, 200% zoom, slow network | API running |

Suggested route files (framework-agnostic naming): `admin/members/index`, `admin/members/[id]`, plus dialogs `DeactivateMemberDialog`, `ReactivateMemberDialog`, `AccountActionDialog`, `ExportMembersDialog`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-19.1 | Confirm is disabled until the reason has 10 non-space characters; the API also refuses short reasons |
| AC-19.2 | The deactivate dialog lists sessions, route, pending requests, connections, chats and leads, and the success state confirms Inactive |
| AC-19.3 | Reactivate copy states the route stays Inactive and closed connections stay closed |
| AC-19.4 | No control exists to edit name, email, mobile, route, leads, messages or ratings; no delete control exists |
| AC-19.5 | Mobile is visible on the detail (with Show) and never appears in the public/member surfaces |
| BR-19.1 | Every action dialog requires a reason and says it is recorded in the audit trail |
| BR-19.2 | Deactivation effects are shown from real counts before confirming |
| BR-19.3 | Reactivation does not offer to revive the route |
| BR-19.4 | Unlock is available only when locked |
| BR-19.5 | Mark verified shows the exception caution |
| BR-19.6 | Password reset never asks for or displays a password |
| BR-19.7 | Correct gender shows the old value and requires a new value plus reason |
| BR-19.8 | Remove photo shows the current photo and warns that the member is notified |
| BR-19.9 | Read-only member data everywhere (AC-19.4) |
| BR-19.10 | No delete option anywhere |
| BR-19.11 | Restrict/Block are not in the Actions menu; a link points to Safety |
| BR-19.12 | Export masks email/mobile by default and warns when contact details are included |
| BR-19.13 | Admin accounts are not listed and cannot be opened here |
| UP-5 | Destructive actions state exact effects before acting |
| UP-10 | Loading, empty, filtered-empty, error, no-access, not-found, disabled, offline, partial-failure and success states are designed |
| UP-11 | Contrast, focus, labels, targets, keyboard and screen-reader announcements pass |
| UP-12 | Admin screens are dense and deliberate; no bulk actions |
| — | Dates use `09-Oct-2026`, times `8:05 AM`, India time |
| — | No chat text appears anywhere in M10 |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| UD-UI-1 | Show full mobile on the list or only on detail? | Masked on the list, full with Show on the detail |
| UD-UI-2 | Should Show/Hide mobile be audited? | Not in Phase 1; add the optional contact endpoint if product wants it |
| UD-UI-3 | Disabled vs hidden for unavailable actions | Hidden for impossible actions, disabled with a reason for temporary ones (e.g. locked) |
| UD-UI-4 | Where do Restrict/Block live in navigation? | A "Restrictions and blocks" link to the Safety module only |
| UD-UI-5 | Export format | CSV first; Excel/PDF later if requested |
| UD-UI-6 | Default member-list sort | Registered newest first; Last login available |
| UD-UI-7 | Show the login-history table in full or behind "Show more"? | First 10 rows with Show more |
| UD-UI-8 | Should Activity/Safety tabs be summary-only or deep-link? | Summary with links to M11/M12 for detail |
| UD-UI-9 | Full-page action dialog on phone or bottom sheet? | Full-height sheet on phone; centred dialog on desktop |
| UD-UI-10 | Show a member photo on the list? | Optional column, off by default to keep the table dense |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-19.1 Member list | §2, §4.1 |
| SCR-19.2 Member detail + tabs | §2, §4.2, §4.6 |
| SCR-19.3 Change status | §4.4, §4.5 |
| SCR-19.4 Account actions | §4.4, §4.5 |
| Member list filters/columns | §4.1 |
| Member detail blocks | §4.2 |
| Account action field spec | §4.5 |
| BR-19.1 reason + audit | §4.5, §8, §12 |
| BR-19.2 deactivate | §4.5 |
| BR-19.3 reactivate | §4.5 |
| BR-19.4 unlock | §4.5 |
| BR-19.5 mark verified | §4.5 |
| BR-19.6 password reset | §4.5 |
| BR-19.7 correct gender | §4.5 |
| BR-19.8 remove photo | §4.5 |
| BR-19.9 no member edit | §2, §4.2, §12 |
| BR-19.10 no delete | §2, §12 |
| BR-19.11 restrict/block in M12 | §4.2, §4.4 |
| BR-19.12 contact masking/export | §4.3 |
| BR-19.13 Admin accounts excluded | §2, §4.1, §4.2 |
| SC-19.1…19.9 | §4, §9 |
| AC-19.1…19.5 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4.2 Admin navigation | §2 |
| UI Plan §6.10 W-10.1…W-10.3 | §4.1, §4.2, §4.5 |
| UI Plan §7.3 confirmation levels | §4.5 |
| UI Plan §7.4 lists/filters/paging | §4.1 |
| UI Plan §7.1 states | §5 |
| UI Plan §8 delivery | §11 |
