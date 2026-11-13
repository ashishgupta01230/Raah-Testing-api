# M13 — Matching Configuration & Platform Settings — UI Plan

| | |
|---|---|
| **Module** | M13 — Matching Configuration & Platform Settings (spec `docs/spec/13_M13_Matching_Config_Settings.md`, functional specification Section 22) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, Admin navigation §4.2, flow F-9, module design §6.13 (W-13.1…W-13.4), legal/public surfaces §6.1 (W-1.5), cross-module patterns §7, delivery §8 |
| **Platform** | Admin screens **desktop first**, then tablet and phone (D-20). Member-facing surfaces fed by M13 are phone first |
| **Companion** | API plan: `13_M13_Matching_Config_Settings_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M13 is the Admin **control room for how RAAH behaves and what it says**. It answers: *can Admin tune the platform safely, explain every change, and publish trusted content without a code release?*

1. **SCR-22.1 Configuration** — all settings grouped, each with current value, allowed range and default; batch save with a remark; security settings require a second confirmation (W-13.1).
2. **SCR-22.2 Change History** — every change with setting, old value, new value, who, when and remark.
3. **SCR-22.3 Test Match** — two sample legs evaluated under the current settings, with a step-by-step result (W-13.2).
4. **SCR-22.4 Legal Content** — edit, preview, publish and version the three legal documents, with the sample alert and re-acceptance choice (W-13.3).
5. **SCR-22.5 City Master** — add, edit, deactivate and reactivate cities, with route usage and counts (W-13.4).
6. **SCR-22.6 City Requests** — add or decline member-suggested cities with a reason (W-13.4).

UI jobs for this module:

- **Safe tuning.** Every field states its allowed range and default; an out-of-range value is refused inline before save, with the range shown (BR-22.5, UP-7).
- **Nothing changes by surprise.** A save is a deliberate batch with a remark; security settings ask "Are you sure?"; recalculation warns that leads may be withdrawn (BR-22.9, BR-22.4, UP-5).
- **Every change is visible and permanent.** Changed rows are marked until saved; Change History is read-only and shows old → new (BR-22.1, UP-9).
- **Explain the match.** Test Match shows each step (cities, days, time, points with similarity, direction, shared points) before the final Full Route / Useful Segment / No match result (A-51, UP-2).
- **Content is governed, not edited live.** Drafts are separate from the Published text; publish is confirmed and versioned; the sample alert is impossible to miss (D-24, BR-22.12, BR-22.13).
- **The city list is the product's map.** Cities are created by Admin, never deleted; duplicate names are caught; deactivation explains exactly which routes block it (D-26, BR-22.18–BR-22.20).
- **Every state designed** — loading, empty, filtered-empty, error, no access, disabled, read-only, saving, publishing, partial failure, success (UP-10).
- **Admin density, calm use.** Dense tables and groups, keyboard-friendly, drill-down from every count; destructive actions always explain their effect (UP-12).

Out of scope for M13 screens (linked, owned elsewhere): the Admin dashboard (M14), the report catalogue and exports (M15), matching health monitors (M11), member management (M10), safety queues (M12), route editing (M2), and the member route form (the "My city is not listed" sheet is W-3.2 in M2). The member legal gate and public legal pages are **fed** by M13; their layouts already exist in UI Plan W-1.5 and §6.1.

---

## 2. Navigation and routes

M13 lives in the Admin sidebar group **Settings** (UI Plan §4.2): **Configuration · Test Match · Legal Content · Cities · City requests**. Change History is reached from Configuration.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Configuration | `/admin/config` | SCR-22.1 | Sidebar "Configuration"; dashboard quick links; M11 Matching Health "tune settings" |
| Change History | `/admin/config/history` | SCR-22.2 | Configuration header "Change history" |
| Test Match | `/admin/config/test-match` | SCR-22.3 | Sidebar "Test Match"; Configuration "Test a match"; M11 Matching Health |
| Legal Content | `/admin/legal` | SCR-22.4 | Sidebar "Legal Content"; dashboard sample-legal alert; quick links |
| City Master | `/admin/cities` | SCR-22.5 | Sidebar "Cities"; city-request "Add city"; M2 member suggestion notice |
| City Requests | `/admin/city-requests` | SCR-22.6 | Sidebar "City requests"; Cities header count badge |

Shared member-facing surfaces fed by M13:

| Surface | Route | Spec | M13 provides |
|---|---|---|---|
| Public legal pages | `/legal/terms`, `/legal/privacy`, `/legal/safety` | BR-22.14 | Current published title/body/effective date |
| Legal re-acceptance gate | `/accept-terms` | BR-22.16 | Pending required versions + acceptance write |
| "My city is not listed" sheet | inside `/route/edit` | A-56 | Submission service (M2 renders the sheet) |

Admin gates (Foundation): Admin login only (D-19, BR-5.11); Inactive/Blocked Admin cannot log in (BR-5.4); the Admin UI is a separate bundle from the member app.

**Hard route rules:** there is no `/admin/config/new`, no delete route for a setting, legal version, city or request (BR-22.10, BR-22.12, BR-22.20), no "edit past version" route, and no member-data entry in Test Match. If someone types one, show the generic no-access page rather than a form.

---

## 3. Screen inventory and components

| Screen / element | Components used (UI Plan §3.6) |
|---|---|
| Configuration | C-28 Admin shell/table patterns, grouped disclosure panels, C-2 Number/Text/Time fields, C-4 Select, C-6 Switch, C-7 Day selector, C-21 Info/warning banners, C-1 Buttons, C-23 Confirm dialog, C-22 Toast, C-33 Key-value hints |
| Change History | C-28 Data table, C-27 Filter bar, C-2 Search, C-4 Select multi, C-12 Change chips, C-34 History timeline (optional expanded row), C-25 Empty state, C-26 Skeleton, C-35 pager |
| Test Match | Two sample-leg cards, C-4 City selects, C-9 Route point inputs, C-7 Day selector, C-8 Time fields, C-1 Run button, pass/fail checklist, C-12 Match-type badge, C-15 Route line (compact), C-21 No-member-data banner, C-25 Empty state, C-26 Skeleton |
| Legal Content | C-32 Rich text editor, C-10 Tabs (three documents), C-2 Text field (title), C-8 Date field, C-6 Switch (re-acceptance), C-21 Sample alert banner, C-1 Save draft / Publish, C-23 Publish dialog, C-35 version list, C-25 Empty state, C-26 Skeleton |
| City Master | C-28 Data table, C-27 Filter bar, C-2 Search, C-4 Select, C-12 Status chips, C-23 Add/Edit dialog, C-23 Deactivate dialog, C-21 In-use error panel, C-25 Empty state, C-26 Skeleton, C-35 pager |
| City Requests | C-28 table, C-27 filters, C-12 Status chips, C-23 Add city dialog (prefilled), C-23 Decline dialog with reason, C-22 Toast, C-25 Empty state, C-26 Skeleton, C-35 pager |
| Shared member surfaces | Existing W-1.5 gate and public legal page layouts; no new member components required |

Build shared Admin components here if not already created by M10–M12: C-28, C-34, C-35. M13 adds feature-scoped components only: a **settings group panel**, a **sample-leg card**, a **legal editor + preview split**, and a **city usage panel**. Do not build a generic approval workflow.

---

## 4. Screen designs

### 4.1 Configuration — `/admin/config` (W-13.1)

```text
┌────────────┬──────────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Configuration                         [ Change history ]           │
│ Dashboard  │                                        [ Recalculate leads ]       │
│ Members ▸  │  Last saved 12-Nov-2026 9:10 AM by Priya N.                         │
│ Routes ▸   │  (i) Changes apply to new activity from the moment you save.        │
│ Safety ▸   │      Completed records are never rewritten.                         │
│ Settings ◄ │ ┌ Matching ──────────────────────────────────────────────── ▼ ┐     │
│  Config ◄  │ │ Departure time tolerance (min)  [ 30 ]  0–120 · default 30  │     │
│  Test Match│ │ Text match threshold (%)        [ 70 ]  50–100 · default 70 │     │
│  Legal     │ │ Minimum shared points           [ 2  ]  2–5 · default 2     │     │
│  Cities    │ │ Maximum points per leg          [ 5  ]  3–10 · default 5    │     │
│  Requests  │ │ Minimum points per leg          [ 2  ]  Fixed at 2          │     │
│ Reports    │ │ Travel days      [Mon][Tue][Wed][Thu][Fri][Sat][Sun]        │     │
│            │ │ Return leg mandatory            [ off ]                     │     │
│            │ │ Gender filter enabled           [ on  ]                     │     │
│            │ │ Leg filter enabled              [ on  ]                     │     │
│            │ └──────────────────────────────────────────────────────────────┘     │
│            │ ▶ Requests and connections    ▶ Chat and rating                     │
│            │ ▶ Safety                      ▶ Notifications                      │
│            │ ▶ Security and content  (changes ask for extra confirmation)        │
│            │                                                                      │
│            │ Remark (optional) [__________________________________] 0/500        │
│            │ [ Reset changes ]                              [ Save changes ]      │
└────────────┴──────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Grouping | Six groups in the spec order: Matching · Requests and connections · Chat and rating · Safety · Notifications · Security and content. One group expanded at a time is fine; matching opens by default |
| Row | Label, control, unit, allowed range and default are always visible. Helper text explains the effect in one sentence. The input is sized to the value; numbers show tabular figures |
| Changed row | A row edited but not saved gets a small amber "Changed" dot and a subtle tint. "Reset changes" restores all unsaved edits |
| CFG-3 | Read-only field showing "Fixed at 2" with a lock; no control (BR-22.10, BR-22.5) |
| CFG-4 / CFG-29 | The range text shows the live dependency: "2–5 (follows maximum points per leg)" and "1–29 (follows request expiry − 1)". If the related value changes, the range updates before save |
| CFG-18 | Day selector with "Weekdays" and "All" shortcuts; clearing every day is an error "Choose at least one day" (C-7) |
| CFG-21 | Vehicle multi-select (Bike, Car) with at least one required; shown only in Matching |
| CFG-16 | In-app is locked on; Email is a switch. Copy: "In-app is always on. Email can be turned off." |
| CFG-25 | Text field with a 150-character counter and helper "No links, email addresses or phone numbers." Inline error when the text contains one |
| CFG-31 | 12-hour time field with AM/PM (C-8), e.g. `7:00 AM` |
| Validation | On blur and on save, inline beside the field: "Allowed range is 0 to 120." Cross-field errors name both settings: "Minimum shared points cannot exceed maximum points per leg." (BR-22.5, V-47) |
| Security group | A warning banner inside the group: "These settings affect every member immediately. You will be asked to confirm." Saving any of CFG-8/9/10/11/23 opens "Are you sure?" with the old → new list (BR-22.9) |
| Remark | One optional remark for the batch, ≤ 500, counter visible. Helper: "Saved with every change in this batch." |
| Save | One primary button. Disabled when nothing changed. While saving, fields and buttons disable and a spinner replaces the label; double-submit blocked |
| Save result | Toast "Changes saved." plus a summary line "3 settings changed." The header `Last saved` updates. Changed dots clear |
| Save error | Banner above the actions; keep all edits; specific copy for range, dependency, security confirmation and conflict. On `CONFIG_CONFLICT`, offer "Reload values" and keep a copy of the attempted values visible |
| Recalculate leads | Opens the confirmation dialog (§4.6), not an immediate action |
| Change history | Header link to `/admin/config/history`; keyboard reachable |
| No deletion | There is no delete or clear control for any setting; resetting to default is a normal change through "Reset to default" per row (proposed) |
| Mobile | Groups stack; each row becomes label above control; range/default wraps under the control; save bar sticks to the bottom above the safe area |

**Layout note.** The page is a form, not a table. At 1440 px the content is capped for readability; the right side is empty rather than stretching each field across the screen. Groups use a border and a disclosure header, not heavy shadow.

---

### 4.2 Change History — `/admin/config/history` (SCR-22.2)

```text
┌────────────┬──────────────────────────────────────────────────────────────────────┐
│ RAAH Admin │ ← Configuration                                                     │
│ Settings ◄ │  Change history                                                     │
│            │  Search [setting or Admin__________]  Group[All▼] Admin[All▼]        │
│            │  Date [ dd-mmm-yyyy ] – [ dd-mmm-yyyy ]   [ Clear all ]              │
│            │ ┌────────────┬──────────────────────┬────────┬────────┬───────┬──────┐│
│            │ │ When       │ Setting              │ Old    │ New    │ By    │Remark││
│            │ ├────────────┼──────────────────────┼────────┼────────┼───────┼──────┤│
│            │ │12-Nov 9:10 │ CFG-4 Min shared pts │ 2      │ 3      │Priya  │Pilot ││
│            │ │12-Nov 9:10 │ CFG-22 Text threshold│ 70     │ 65     │Priya  │Pilot ││
│            │ │11-Nov 4:32 │ CFG-1 Time tolerance │ 30     │ 45     │Arun   │—     ││
│            │ └────────────┴──────────────────────┴────────┴────────┴───────┴──────┘│
│            │  Showing 1–25 of 48                        [ < 1 2 3 … > ]           │
└────────────┴──────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Read-only | There is no edit or delete control anywhere on this screen (BR-8.3) |
| Columns | When (India time), Setting (key + short label), Old value, New value, Changed by, Remark. A batch saved together shows the same time and a subtle "Batch" marker |
| Sort | Default newest first; also oldest first, by setting, by Admin |
| Filters | Search by setting key/label or Admin; Group; Admin; date range. Active count + Clear all |
| Remark | Full remark shown on row expand or a tooltip; never truncated in an inaccessible way |
| Empty | "No changes yet. Settings you save will appear here." |
| Filtered empty | "No changes match these filters." + Clear filters |
| Loading / Error | Skeleton rows / inline banner + Try again; filters stay |
| Mobile | Cards: setting, old → new, when, by, remark; filters in a sheet |

---

### 4.3 Test Match — `/admin/config/test-match` (W-13.2)

```text
┌────────────┬──────────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Test Match                                                          │
│ Settings ◄ │  (i) Uses current settings. No member data is used.                  │
│            │  ┌ Sample Saathi leg ─────────────┐ ┌ Sample Raahi leg ──────────────┐ │
│            │  │ Start city [ Delhi      ▼ ]    │ │ Start city [ Delhi      ▼ ]    │ │
│            │  │ End city   [ Noida      ▼ ]    │ │ End city   [ Noida      ▼ ]    │ │
│            │  │ Points                         │ │ Points                         │ │
│            │  │ ① [Vinay Nagar________]        │ │ ① [Vinay Nagar________]        │ │
│            │  │ ② [Badarpur Metro______]       │ │ ② [Bdarpur Metro_______]       │ │
│            │  │ ③ [Nehru Place_________]       │ │ ③ [Nehru Place_________]       │ │
│            │  │ [+ Add stop]                   │ │ [+ Add stop]                   │ │
│            │  │ Days [M][T][W][T][F][S][S]     │ │ Days [M][T][W][T][F][S][S]     │ │
│            │  │ Leaves [ 8:00 AM ]             │ │ Leaves [ 8:05 AM ]             │ │
│            │  └────────────────────────────────┘ └────────────────────────────────┘ │
│            │  [ Run test ]                                                        │
│            │  ── Result ─────────────────────────────────────────────────────────  │
│            │  ✓ Cities overlap   ✓ Common days   ✓ Time gap 5 min (limit 30)     │
│            │  Point pairs: Vinay Nagar = Vinay Nagar 100% ·                       │
│            │               Badarpur Metro = Bdarpur Metro 93%                     │
│            │  ✓ Direction   ✓ Shared points 2 (minimum 2)                         │
│            │  Result: [ Useful Segment ]   Both legs?  Going only                  │
│            │  [ Open matching settings ]                                           │
└────────────┴──────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Two cards | Saathi leg left, Raahi leg right on desktop; stacked on tablet/phone. Each card is independent and labelled plainly |
| Cities | Start and End city selects from the active City Master (C-4, searchable on desktop). Same city is allowed (D-26) |
| Points | Start, up to 3 stops, End; 2–CFG-2 points per leg (C-9). "Add stop" disappears at the limit with "A leg can have at most {CFG-2} points, including Start and End." (V-15, CFG-2) |
| Days | C-7 with at least one day; configured travel days from CFG-18 |
| Time | C-8 with AM/PM; required for both sample legs |
| Run | One primary button. While running, the button shows a spinner and the inputs disable. No auto-run on every keystroke |
| Result checklist | Each matching step is a row with pass/fail icon, label and detail: Cities overlap · Common days · Time gap vs CFG-1 · Point pairs with similarity % vs CFG-22 · Direction · Shared points vs CFG-4. Failures name the reason, e.g. "No shared city — these routes cannot match." |
| Point pairs | Canonical text left = right, similarity percentage; a pair below CFG-22 is shown as not paired with its percentage, so Admin sees why |
| Result badge | Full Route (green, route icon) · Useful Segment (sky, part icon) · No match (slate). Always text plus icon (UI Plan §3.7) |
| No member data | A fixed info banner and a line under the title: "Uses current settings. No member data is used." Never show a member name, photo, route id or count (AC-22.5) |
| Settings link | "Open matching settings" returns to `/admin/config` with the Matching group expanded and CFG-1/CFG-4/CFG-22 in view, so Admin can tune after seeing the result (F-9) |
| Empty | Before the first run: a short explanation and "Run test" disabled until both legs are complete |
| Loading / Error | Skeleton legs on first load / inline banner "Could not load the city list." + Try again; result error "Could not run the test." keeps the inputs |
| Validation | Inline per field: choose both cities, at least two points, at least one day, valid time, point limit, same-place warning as the member route form uses (BR-11.6) |
| Mobile | Cards stack; result checklist and point pairs stack; the result badge stays visible above the fold after run |

**Worked example for review (SC-22.2).** "Nehru Place" vs "Nehru Plaza Mkt" shows 60% similarity. At CFG-22 = 70 the pair is not made, the shared-point step fails and the result is No match; the copy explains that lowering the threshold would pair them and may also create false matches. This makes the threshold understandable rather than magic.

---

### 4.4 Legal Content — `/admin/legal` (W-13.3)

```text
┌────────────┬──────────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Legal Content                                                       │
│ Settings ◄ │  (!) Legal text is still the sample. Replace and publish final text  │
│            │      before real members join.                    [ View public page ] │
│            │  [ Terms of Use ] [ Privacy Policy ] [ Safety Guidelines ]           │
│            │  Published: Sample 1 · effective 01-Jan-2026   Versions (3) ▾        │
│            │  ┌ Draft / current ────────────────────────────────────────────────┐  │
│            │  │ Title [Terms of Use________________________________] 0/150     │  │
│            │  │ Effective date [ 12-Nov-2026 ]  Requires re-acceptance [ off ] │  │
│            │  │ ┌ Editor (headings, lists, links) ───────┐ ┌ Preview ────────┐ │  │
│            │  │ │ H  B  I  • 1.  🔗                      │ │ Terms of Use    │ │  │
│            │  │ │                                        │ │                 │ │  │
│            │  │ │ 1. About these terms                   │ │ 1. About these  │ │  │
│            │  │ │ RAAH connects people...                │ │ terms ...       │ │  │
│            │  │ │                                        │ │                 │ │  │
│            │  │ └────────────────────────────────────────┘ └─────────────────┘ │  │
│            │  └────────────────────────────────────────────────────────────────┘  │
│            │  Saved 12-Nov 9:10 AM · [ Save draft ]                 [ Publish… ]  │
└────────────┴──────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Three documents | C-10 tabs: Terms of Use · Privacy Policy · Safety Guidelines. Each tab carries a dot when it has an unpublished draft |
| Sample alert | C-21 amber banner at the top while any current published version is a sample. Copy from UI Plan §3.8: "Legal text is still the sample. Replace and publish final text before real members join." It stays until **all three** have a published non-sample version (BR-22.13, AC-22.7) |
| Published line | "Published: {version label} · effective {date}" with a Versions disclosure. Current and prior versions are read-only links |
| Title | C-2, 1–150, counter visible; helper "Shown at the top of the public page." |
| Effective date | C-8 date field, `dd-mmm-yyyy`, not earlier than today. Inline error "The effective date cannot be earlier than today." |
| Requires re-acceptance | C-6 switch with plain copy: "On: every member must accept this version at their next login. Off: no interruption (for example a typo fix)." (BR-22.16, SC-22.10) |
| Editor | C-32 rich text with headings, paragraphs, numbered/bulleted lists and links only. No images, tables, scripts or styles. A format error on save explains the allowed set |
| Preview | Live side-by-side preview on desktop, a "Preview" tab on phone/tablet. The preview uses the same typography as the public page |
| Draft vs live | A draft never changes the public page. The screen says so: "Saved as a draft. Members still see {published version} until you publish." (BR-22.12) |
| Save draft | Secondary button; toast "Draft saved."; the tab dot clears only after publish, not after save (draft still exists) |
| Publish | Opens the publish dialog (§4.5). Disabled when the draft is invalid; when no draft exists, show "Save a draft before publishing." |
| Versions | A read-only list/sheet: version label, title, effective date, status (Published current / Published retired / Draft / Sample), published by and on, re-acceptance, remark. Opening a retired version shows the body read-only; there is no edit or delete |
| Sample replacement | Publishing a non-sample version removes the sample alert only when all three documents are non-sample (SC-22.9) |
| Public link | "View public page" opens the public route in a new tab; it always shows the current published version, never the draft |
| Loading / Error | Skeleton document / inline banner + Try again; save/publish errors keep the editor content |
| Mobile | Tabs scroll horizontally; editor and preview become two tabs; actions stick to the bottom above the safe area |

**Copy for the draft state.** When a draft exists: "Draft saved {time}. Members still see {published label} until you publish." When no draft: "Editing creates a draft. Nothing changes for members until you publish."

---

### 4.5 Publish dialog (BR-22.12, BR-22.16)

```text
┌  Publish “Terms of Use” version 2? ─────────────────────────────────┐
│  Effective date: 12-Nov-2026                                        │
│  Requires re-acceptance: Yes                                        │
│                                                                     │
│  What this does                                                     │
│  • Replaces the current published version (Sample 1)                │
│  • Members see the new text immediately after publishing            │
│  • 1,240 members must accept it at their next login                 │
│  • An in-app notice and email are sent to those members             │
│  • Earlier versions stay readable by Admin and are never deleted    │
│                                                                     │
│  (i) The previous version is kept in history. To fix a typo without │
│      interrupting members, publish with re-acceptance off.          │
│                                                                     │
│  Remark (optional) [____________________________________] 0/500     │
│  [ Cancel ]                                      [ Publish ]        │
└─────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Effects | Built from the API response: previous version label, affected member count, and whether a gate will start. Never vague (UP-5) |
| Re-acceptance off | Effects read "No member has to accept it again." and no notification line appears (SC-22.10) |
| Confirm | Only after the dialog. The button label is "Publish", not "OK" |
| Loading | Buttons disable, spinner on Publish; dialog stays open on error |
| Error | Inline in the dialog: `LEGAL_EFFECTIVE_DATE_INVALID`, `LEGAL_DRAFT_NOT_FOUND`, `CONFIRMATION_REQUIRED`, `LEGAL_BODY_FORMAT_INVALID` |
| Success | Dialog closes, toast "Published", the Published line and Versions refresh, the sample alert recalculates |
| Accessibility | Focus trap, dialog title announces the document and version, effects read in order |

---

### 4.6 Recalculate leads dialog (BR-22.4, A-52)

```text
┌  Recalculate all leads? ────────────────────────────────────────────┐
│  This re-runs matching for every Active route.                      │
│  • Some leads may be withdrawn                                     │
│  • Some new leads may be created                                   │
│  • Pending requests and accepted connections are not changed       │
│  • The run is logged with the number of leads created and withdrawn│
│                                                                     │
│  Current matching settings: time tolerance 30 · shared points 2 ·   │
│  text threshold 70%                                                │
│                                                                     │
│  Remark (optional) [____________________________________] 0/500     │
│  [ Cancel ]                              [ Recalculate leads ]      │
└─────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Trigger | Configuration header. Always a dialog; never an inline immediate action |
| Effects | Fixed ordered list from the spec, with live settings shown so Admin knows which rules will run |
| Progress | After confirm, the dialog becomes a progress panel: "Recalculation started" with a spinner and a link to the run. The UI polls the run endpoint; it does not block the screen |
| Completion | Run summary shows routes evaluated, leads created, updated and withdrawn, started/completed, and any error summary. "View run log" opens the run list |
| Partial / failure | "The recalculation did not finish." with the last known counters and error summary; no false success (BR-22.4) |
| Duplicate | If a run is already in progress, the button is disabled with "A recalculation is already running." |
| Mobile | Dialog becomes a full-height sheet; run summary becomes a stacked card |

---

### 4.7 City Master — `/admin/cities` (W-13.4, SCR-22.5)

```text
┌────────────┬──────────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Cities                                            [ Add city ]     │
│ Settings ◄ │  Search [city or state______________] Status[All▼] [ Clear all ]     │
│  Cities ◄  │ ┌─────────────┬──────────┬─────────┬──────────────┬─────────┬──────┐ │
│  Requests  │ │ City        │ State    │ Status  │ Active routes│ Members │      │ │
│            │ ├─────────────┼──────────┼─────────┼──────────────┼─────────┼──────┤ │
│            │ │ Delhi       │ Delhi    │[Active] │ 182          │ 240     │ ⋯    │ │
│            │ │ Noida       │ UP       │[Active] │ 40           │ 55      │ ⋯    │ │
│            │ │ Gurugram    │ Haryana  │[Inactive│ 0            │ 0       │ ⋯    │ │
│            │ └─────────────┴──────────┴─────────┴──────────────┴─────────┴──────┘ │
│            │  Showing 1–25 of 12                                                 │
└────────────┴──────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Columns | City, State, Status, Active routes, Members, actions menu. Members are distinct members with an Active route using the city (BR-22.22) |
| Add city | C-23 dialog: City name, State (optional), Status default Active. Name is trimmed; duplicate error appears inline under the field with the existing spelling, e.g. `"New Delhi" already exists.` (BR-22.18) |
| Edit city | Same dialog, prefilled. Correction copy: "The name changes everywhere routes use this city." (BR-22.20) |
| Deactivate | Opens the deactivation dialog (§4.8). If Draft/Active routes use the city, the dialog is replaced by a refusal panel listing the routes; no status change occurs (BR-22.19, AC-22.11) |
| Reactivate | Always allowed; confirm is a single button, no reason required. Toast "City activated." |
| No deletion | No delete item in the menu anywhere; the city remains for route history (BR-22.20) |
| Search / status | Search name or state; status Active/Inactive/All. Active filter count + Clear all |
| Counts | Active routes and members are live; a zero shows "0", not a blank |
| Loading / Error | Skeleton rows / inline banner + Try again; actions disabled while loading |
| Empty | "No cities yet. Add the cities members can start and end a route in." (SC-22.13) |
| Filtered empty | "No cities match these filters." + Clear filters |
| Mobile | Cards: city, state, status, active routes/members, action menu; filters in a sheet |

---

### 4.8 City deactivation dialog and in-use refusal (BR-22.19)

```text
┌  Deactivate “Noida”? ───────────────────────────────────────────────┐
│  Members will not be able to choose Noida on a new route.           │
│  Existing Inactive routes keep it for history.                      │
│  (i) A city cannot be deactivated while a Draft or Active route     │
│      uses it as a Start or End city.                                │
│  [ Cancel ]                                [ Deactivate city ]      │
└─────────────────────────────────────────────────────────────────────┘

┌  “Noida” cannot be deactivated ─────────────────────────────────────┐
│  40 routes use this city as a Start or End city.                    │
│  Change or deactivate them first:                                   │
│  ┌────────────┬────────────┬────────────┬──────────┐                │
│  │ RTE-000418 │ Ashish K.  │ Saathi     │ [Active] │                │
│  │ RTE-000421 │ Neha S.    │ Raahi      │ [Draft]  │                │
│  └────────────┴────────────┴────────────┴──────────┘                │
│  38 more routes not shown.                                          │
│  [ Close ]                          [ View routes in Route monitor ]│
└─────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Pre-check | The dialog loads usage first. When free, show the simple confirm; when blocked, show the refusal panel instead of a confirm so Admin never clicks a doomed action (BR-22.19) |
| Route list | Route number, member name, role, status (Draft/Active). Each row links to the Route monitor detail (M11). The list caps at 100 with a total count |
| Re-check | The action re-checks on the server; a race returns `CITY_IN_USE` and the UI refreshes the refusal panel |
| No override | There is no "deactivate anyway" button |
| Success | Dialog closes, status becomes Inactive, toast "City deactivated." |

---

### 4.9 City Requests — `/admin/city-requests` (SCR-22.6, A-56)

```text
┌────────────┬──────────────────────────────────────────────────────────────────────┐
│ RAAH Admin │  City requests                                                       │
│ Settings ◄ │  Status[Open▼] Search[member or city________] Date[from–to] [ Clear ] │
│  Cities    │ ┌──────────────┬────────────────┬─────────────┬─────────┬────────────┐ │
│  Requests ◄│ │ Member       │ Requested city │ Requested   │ Status  │ Actions    │ │
│            │ ├──────────────┼────────────────┼─────────────┼─────────┼────────────┤ │
│            │ │ Jaipur member│ Jaipur         │ 12-Nov-2026 │ [Open]  │ Add·Decline│ │
│            │ │ MBR-000245   │                │             │         │            │ │
│            │ │ Neha S.      │ NewDelhi       │ 11-Nov-2026 │ [Open]  │ Add·Decline│ │
│            │ └──────────────┴────────────────┴─────────────┴─────────┴────────────┘ │
│            │  Showing 1–25 of 6                                                   │
└────────────┴──────────────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Default filter | Open requests, oldest first; "Requests" keeps the count visible in the sidebar/header |
| Row | Member name + MBR + small avatar, requested name, requested date, status chip, decision info when decided |
| Duplicate flag | If the name now exists in the City Master, a slate chip "Already in the list" appears; Add is disabled with the reason |
| Add city | Opens the City Master add dialog prefilled with the requested name. Admin may correct spelling and add a State. On success the request becomes Added and the member is told in-app (A-56) |
| Decline | Opens a reason dialog (10–300) with helper "The member sees this reason in the app." On success the member is told in-app and the row shows Declined |
| Decided rows | Show decision by/on and the reason for a decline; no further action |
| Empty | "No city requests right now." + "Members can suggest a city from the route form." |
| Filtered empty | "No city requests match these filters." + Clear filters |
| Loading / Error | Skeleton rows / inline banner + Try again |
| Mobile | Cards: member, requested city, date, status, Add/Decline; filters in a sheet |

---

### 4.10 Shared member-facing surfaces (data supplied by M13)

These layouts already exist in the UI Plan; M13 supplies content and state. They are listed so the module is complete end to end.

| Surface | Existing design | M13 data/state |
|---|---|---|
| Public legal pages | UI Plan §6.1 `/legal/terms`, `/legal/privacy`, `/legal/safety` | Current published title, body, effective date, version label. No login, no member data |
| Legal re-acceptance gate | W-1.5 `/accept-terms` | Pending required documents in three tabs; "Accept and continue" enabled only after the tick; only the texts and Log out work until accepted (BR-22.16) |
| My Account legal links | W-2.1 | Three public links; always show the current published version |
| "My city is not listed" | W-3.2 sheet in the route form | One field, a note that Admin will review it, and a success/decline notice later. M13 owns the request record and notification, not the sheet |

**Legal gate rules to preserve.** While a required version is pending, the member can read the texts and log out but cannot use the product. The gate must not reveal Admin or version internals; it shows the human title, effective date and body.

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton groups/rows/tiles; header, filters and actions stable; never a bare spinner |
| Empty | Per-screen copy with one next action; no fake data |
| Filtered empty | "No {records} match these filters." + Clear filters |
| Error (load) | Inline banner + Try again; keep filters/expanded group/tab |
| Error (save/action) | Inline in the form/dialog; keep all edits except the invalid field; specific code copy |
| Success | Toast + refresh affected panel/header; changed markers clear; lists reflect the change on return |
| Disabled | Control or confirm disabled with a visible reason nearby (CFG-3, Publish without draft, Add for a duplicate request) |
| No access | "You do not have access to this record." (no existence hint) |
| Not found | "City not found." / "Document not found." / "City request not found." with a back link |
| Sample legal | Amber banner on Legal Content and the Admin dashboard; the go-live check stays failed while any sample is current (BR-22.13) |
| Re-acceptance pending | Publish dialog states the member count and that the gate starts; the member side shows the gate |
| Security confirmation | A second "Are you sure?" listing old → new for CFG-8/9/10/11/23 |
| Conflict | "Another Admin changed this setting. Reload and try again." with a reload action and the attempted values visible |
| Recalculation running | Confirm disabled; run panel links to the live run |
| City in use | Refusal panel listing routes; no override |
| Offline | Slim banner; save/publish/run/deactivate disabled with a reason; reads may show cached data |
| Session expired | Foundation dialog; return to login (BR-5.6) |
| Restricted/Inactive Admin | Cannot log in (BR-5.4) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Desktop 1024+ | Full Admin sidebar; configuration groups as full-width panels capped for readability; Test Match two columns; Legal editor and preview side by side; City tables full width; dialogs centred |
| Tablet 768–1023 | Configuration rows keep label above control; Test Match cards stack or stay two columns if wide enough; Legal editor/preview become tabs; City tables drop secondary columns or become cards; dialogs centred |
| Phone 360–767 | One column; configuration group accordions; save bar sticky above the safe area; Test Match cards stacked; Legal tabs scroll horizontally and editor/preview are tabs; City and request tables become cards; action dialogs become full-height sheets |

- Admin is desktop-first, but every screen must remain readable on a phone (UI Plan §7.5).
- Tables scroll inside their container on small screens; they never force page-level horizontal scroll.
- At 200% text zoom the page must remain usable; Admin tables may scroll inside their box.
- Sticky table header on long lists; filter/group/tab state is preserved when opening a detail and coming back.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring; dialogs trap focus and return it to the triggering control |
| Labels | Every setting, filter, date field, title, reason and remark has a visible label; placeholders are examples only |
| Targets | ≥ 44 × 44 px, 8 px apart; switches and day toggles meet the target |
| Colour | Status, match-type, changed and sample states always carry words/icons; never colour alone |
| Keyboard | Group disclosure, every setting control, day selector, save, history table, Test Match fields and run, legal editor toolbar and tabs, publish dialog, city dialogs and pager all work by keyboard |
| Screen readers | Announce the group name, setting label, current value, allowed range, default, inline error, save result and dialog effects in order; the Test Match result is announced step by step; the rich text editor has a labelled toolbar and text area |
| Motion | Honour "reduce motion"; no decorative animation |
| Zoom | 200% text without horizontal page scroll; Admin tables scroll internally |
| Language | `lang="en"`; dates `12-Nov-2026`, times `9:10 AM`, India time |

---

## 8. Microcopy (M13)

| Situation | Copy |
|---|---|
| Page titles | "Configuration" · "Change history" · "Test Match" · "Legal Content" · "Cities" · "City requests" |
| Configuration intro | "Changes apply to new activity from the moment you save. Completed records are never rewritten." |
| Range helper | "Allowed range {min}–{max}. Default {default}." |
| Fixed setting | "Fixed at 2." |
| Changed row | "Changed" |
| Reset | "Reset changes" |
| Save | "Save changes" |
| Save success | "Changes saved." |
| Save summary | "{n} settings changed." |
| Save conflict | "Another Admin changed this setting. Reload and try again." |
| Range error | "Allowed range is {min} to {max}." |
| Dependency error | "This value must stay within its related setting." |
| Security group notice | "These settings affect every member immediately. You will be asked to confirm." |
| Security confirm | "Are you sure? This change affects every member." |
| Remark label | "Remark (optional)" |
| Remark helper | "Saved with every change in this batch." |
| Change history empty | "No changes yet. Settings you save will appear here." |
| Test Match notice | "Uses current settings. No member data is used." |
| Test Match empty | "Enter two sample legs and run the test." |
| Test Match result | "Result: {Full Route / Useful Segment / No match}" |
| Test Match no city | "No shared city. These routes cannot match." |
| Test Match point limit | "A leg can have at most {CFG-2} points, including Start and End." |
| Test Match settings link | "Open matching settings" |
| Recalculate button | "Recalculate leads" |
| Recalculate dialog title | "Recalculate all leads?" |
| Recalculate warning | "Some leads may be withdrawn and some new leads may be created. Pending requests and connections are not changed." |
| Recalculate confirm | "Recalculate leads" |
| Recalculate running | "A recalculation is already running." |
| Recalculate summary | "{created} leads created · {withdrawn} withdrawn · {routes} routes evaluated" |
| Sample legal alert | "Legal text is still the sample. Replace and publish final text before real members join." |
| Legal no draft | "Editing creates a draft. Nothing changes for members until you publish." |
| Legal draft saved | "Draft saved {time}. Members still see {published label} until you publish." |
| Legal effective date error | "The effective date cannot be earlier than today." |
| Legal re-acceptance on | "On: every member must accept this version at their next login." |
| Legal re-acceptance off | "Off: no interruption (for example a typo fix)." |
| Publish button | "Publish" |
| Publish dialog title | "Publish “{document}” {version}?" |
| Publish effects | "Members see the new text immediately after publishing." |
| Publish with gate | "{n} members must accept it at their next login." |
| Publish without gate | "No member has to accept it again." |
| Publish note | "Earlier versions stay readable by Admin and are never deleted." |
| Publish success | "Published" |
| City add | "Add city" |
| City name helper | "2 to 60 characters. Names are matched ignoring capitals and spaces." |
| City duplicate | "A city with this name already exists." |
| City edit note | "The name changes everywhere routes use this city." |
| City deactivate | "Deactivate city" |
| City deactivate note | "Members will not be able to choose this city on a new route. Existing Inactive routes keep it for history." |
| City in use | "{n} routes use this city as a Start or End city. Change or deactivate them first." |
| City reactivate | "City activated." |
| City empty | "No cities yet. Add the cities members can start and end a route in." |
| City request add | "Add city" |
| City request decline | "Decline" |
| City request reason | "Reason (at least 10 characters). The member sees this reason in the app." |
| City request added | "City added. The member has been told." |
| City request declined | "Request declined. The member has been told the reason." |
| No access | "You do not have access to this record." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load Configuration | `GET /admin/config` |
| Save settings | `PUT /admin/config` `{ changes, remark, acknowledgedSecurity }` |
| Load Change History | `GET /admin/config/history?settingKey=&group=&changedBy=&from=&to=&sort=&page=&pageSize=` |
| History filter options | `GET /admin/config/history/options` |
| Export history (optional) | `POST /admin/config/history/export` |
| Load Test Match options | `GET /admin/config/test-match/options` |
| Run Test Match | `POST /admin/config/test-match` `{ saathi, raahi }` |
| Recalculate leads | `POST /admin/matching/recalculate` `{ confirm: true, remark }` |
| Poll run | `GET /admin/matching/runs/{runId}` |
| Run log | `GET /admin/matching/runs?status=&from=&to=&page=&pageSize=` |
| Load Legal Content | `GET /admin/legal` |
| Open a document | `GET /admin/legal/{documentType}` |
| Save draft | `PUT /admin/legal/{documentType}/draft` `{ title, bodyHtml, effectiveDate, requiresReAcceptance }` |
| Publish | `POST /admin/legal/{documentType}/publish` `{ versionId, remark, confirm }` |
| Version list | `GET /admin/legal/{documentType}/versions` |
| Read a version | `GET /admin/legal/{documentType}/versions/{versionId}` |
| Acceptance summary | `GET /admin/legal/{documentType}/acceptance-summary` |
| Public legal list | `GET /legal` |
| Public legal document | `GET /legal/{documentType}` |
| Member legal status / gate | `GET /me/legal/status` |
| Member accepts | `POST /me/legal/accept` `{ versionIds }` |
| Member acceptance history | `GET /me/legal/acceptances` |
| Load Cities | `GET /admin/cities?search=&status=&sort=&page=&pageSize=` |
| City options | `GET /admin/cities/options` |
| Add city | `POST /admin/cities` `{ name, state, status }` |
| Edit city | `PUT /admin/cities/{cityId}` `{ name, state }` |
| City usage before deactivate | `GET /admin/cities/{cityId}/usage` |
| Deactivate city | `POST /admin/cities/{cityId}/deactivate` |
| Reactivate city | `POST /admin/cities/{cityId}/activate` |
| Load City Requests | `GET /admin/city-requests?status=&search=&from=&to=&sort=&page=&pageSize=` |
| Request options | `GET /admin/city-requests/options` |
| Add requested city | `POST /admin/city-requests/{requestId}/add` `{ name, state }` |
| Decline request | `POST /admin/city-requests/{requestId}/decline` `{ reason }` |
| Export cities (optional) | `POST /admin/cities/export` |

Client rules:

- Always send the Admin token on `/admin/*`; never accept a member id except from the route path.
- On `403` → generic no-access; on `404` → "{Record} not found."; never guess which.
- On `CONFIG_CONFLICT` → show "Reload values"; do not silently discard edits.
- On `CONFIG_OUT_OF_RANGE` / `CONFIG_DEPENDENCY_INVALID` / `SECURITY_CONFIRMATION_REQUIRED` → keep the form, focus the field, show the range/dependency.
- On `CITY_IN_USE` → open the usage panel and list the routes; never retry blindly.
- On `CITY_ALREADY_EXISTS` / `CITY_NAME_DUPLICATE` → inline under the name field; keep the dialog open.
- On `LEGAL_DRAFT_NOT_FOUND` → switch the action to "Save draft"; keep the editor content.
- On `LEGAL_EFFECTIVE_DATE_INVALID` → focus the date field and keep the publish dialog open.
- On `RECALCULATION_IN_PROGRESS` → disable the confirm and link to the running run.
- Never render a legal body outside the editor/preview/public page; never put a legal body in a toast.
- Status, match-type and changed badges use the shared status map (UI Plan §3.7); never invent colours.
- Dates use `12-Nov-2026`, times `9:10 AM`, India time.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| Foundation `IAppConfigReader` | The Configuration screen shows defaults from the registry with a neutral banner "Settings are not available yet."; save stays disabled |
| M3 `ITestMatchService` | Test Match inputs render; Run disabled with "Test Match is not available yet." |
| M3 `IMatchingEngine` / run table | Recalculate button disabled with a neutral reason; no fake run |
| M2 route/city data | City usage shows "Route usage is not available yet."; deactivate stays disabled, never assumes zero |
| M9 notifications | Legal publish and city decisions still succeed; the UI says the notice is queued, never blocks on email |
| Foundation auth / legal gate | The member gate is hidden until the gate contract answers; never fake an acceptance |
| M14 dashboard | The sample-legal alert link still works; the dashboard tile may be absent |
| M15 reports | Change History and the city list are the working views; export buttons appear only when the extract exists |
| Shared C-28/C-34/C-35 | Reuse from M10–M12 if present; otherwise build here |

---

## 11. Delivery plan (M13 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Configuration screen: groups, range/default hints, changed markers, day/vehicle/channel controls | API 1 |
| 2 | Save batch, validation, security "Are you sure?", conflict state | API 2 |
| 3 | Change History table, filters, paging, empty/error states | API 3, 4 |
| 4 | Test Match two-leg cards, options, run, step checklist, point pairs, result badge | API 6, 7 |
| 5 | Recalculate dialog, progress and run summary | API 8–10 |
| 6 | Legal overview with tabs, sample alert, editor + preview, draft save | API 11–13 |
| 7 | Publish dialog with member count, remark, confirmation; version history | API 14–16 |
| 8 | Acceptance summary read view | API 17 |
| 9 | City Master table, add/edit dialogs, duplicate handling | API 23–27 |
| 10 | City usage panel, deactivate refusal, reactivate | API 28–30 |
| 11 | City Requests list, Add (prefilled), Decline reason dialog | API 32–35 |
| 12 | Shared surfaces check: public legal page, member gate, My Account links | API 18–22 |
| 13 | Responsive phone/tablet pass; Admin density | — |
| 14 | Accessibility pass: group labels, range/error announcements, dialogs, editor toolbar, history table, Test Match result | — |
| 15 | QA: conflict, out-of-range, sample alert, publish with/without gate, city in use, duplicate name, 200% zoom, slow network | API running |

Suggested route files (framework-agnostic naming): `admin/config/index`, `admin/config/history`, `admin/config/test-match`, `admin/legal/index`, `admin/cities/index`, `admin/city-requests/index`, plus dialogs `RecalculateDialog`, `PublishLegalDialog`, `CityFormDialog`, `CityDeactivateDialog`, `DeclineCityRequestDialog`, and components `SettingsGroupPanel`, `SampleLegCard`, `LegalEditorPreview`, `CityUsagePanel`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-22.1 | After a save, Change History shows old value, new value, Admin, time and remark |
| AC-22.2 | Entering 150 for CFG-1 shows "Allowed range is 0 to 120." and the save is refused inline |
| AC-22.3 | A normal settings change shows no lead-withdrawal warning and no leads are implied to change |
| AC-22.4 | The recalculation dialog states that pending requests and connections are not changed; the run summary shows leads created/withdrawn |
| AC-22.5 | Test Match visibly states "No member data is used." and never shows a member identity |
| AC-22.6 | Admin routes are unreachable from the member app; a member token gets the no-access page |
| AC-22.7 | The sample banner appears while any document is still a sample and links to Legal Content |
| AC-22.8 | Publishing shows the previous version still in the version list and only one current version |
| AC-22.9 | Publishing with re-acceptance on states how many members must accept; the member gate blocks the product until accept |
| AC-22.10 | Adding "newdelhi" when "New Delhi" exists is refused inline |
| AC-22.11 | Deactivating a city in use opens the refusal panel with the route list; there is no override |
| BR-22.1 | Every change row is read-only and has old/new, who, when and remark |
| BR-22.2 | Configuration intro says changes apply to new activity and never rewrite completed records |
| BR-22.3 | No UI implies a settings save withdraws existing leads; that only happens after Recalculate |
| BR-22.4 | Recalculate is always confirmed and shows its run summary |
| BR-22.5 | Every setting shows its allowed range and default; out-of-range is refused |
| BR-22.6 | Copy states lowering maximum points does not invalidate existing routes; making Return mandatory does not edit existing routes |
| BR-22.7 | Changing the request message says it applies to requests sent after the change |
| BR-22.8 | Shortening expiry is presented as applying to requests that already pass the new limit at the next daily check |
| BR-22.9 | Saving a security setting asks "Are you sure?" with old → new |
| BR-22.10 | No delete control exists for any setting |
| BR-22.11 | Only Admins can open M13; denial is shown as no access |
| BR-22.12 | Editing creates a draft; members see nothing until Publish; publish is confirmed and remarkable |
| BR-22.13 | The sample alert is shown until all three documents are non-sample |
| BR-22.14 | The public legal pages are reachable without login and show the current published text |
| BR-22.15 | Acceptance is recorded per member/version; the UI never offers to edit it |
| BR-22.16 | The gate shows the required text and allows only accept/log out; re-acceptance count is visible to Admin |
| BR-22.17 | A new version does not alter an old acceptance row in the UI |
| BR-22.18 | City duplicate is refused inline with a plain message |
| BR-22.19 | A city in use cannot be deactivated and the routes are listed |
| BR-22.20 | There is no delete control for a city; editing the name warns it changes everywhere |
| BR-22.21 | City request decisions notify the member in-app; decline requires a reason |
| BR-22.22 | City rows show Active routes and Members counts |
| UP-5 | Every save, publish, recalculate and deactivate states what will happen before it happens |
| UP-10 | Loading, empty, filtered-empty, error, no-access, not-found, disabled, offline, conflict, running, partial-failure and success states are designed |
| UP-11 | Contrast, focus, labels, targets, keyboard and screen-reader announcements pass |
| UP-12 | Admin screens are dense and deliberate; counts drill into their lists |
| — | Dates use `12-Nov-2026`, times `9:10 AM`, India time |
| — | Status, match-type and changed badges use the shared map, always text plus colour |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| UD-UI-1 | Configuration layout: one long page or groups? | Accordion groups on one page, Matching open first; sticky save bar |
| UD-UI-2 | Show a "Reset to default" per row? | Yes, as a small secondary action; it is a normal change and still logged |
| UD-UI-3 | Change History on a separate page or a tab? | Separate page linked from Configuration |
| UD-UI-4 | Test Match result: inline below or a modal? | Inline below the two cards so Admin can compare inputs and result |
| UD-UI-5 | Recalculate progress: dialog or page? | Dialog turns into a progress panel with a link to the run log |
| UD-UI-6 | Legal editor and preview: side by side or tabs? | Side by side on desktop; two tabs on phone/tablet |
| UD-UI-7 | Version history: list on the page or a sheet? | A disclosure/sheet from the "Published" line; newest first, read-only |
| UD-UI-8 | Publish with no changes | Disabled with "Save a draft before publishing." |
| UD-UI-9 | City actions: menu or inline buttons? | Overflow menu with Edit, Deactivate/Activate; no delete item |
| UD-UI-10 | City deactivation: pre-check before dialog? | Yes; show the refusal panel instead of a confirm when routes block it |
| UD-UI-11 | City request Add: new screen or prefilled City dialog? | Prefilled City dialog, so Admin can correct spelling/state (W-13.4) |
| UD-UI-12 | Show city request decision history? | Yes on the row (decided by/on, reason for decline); the default filter is Open |
| UD-UI-13 | Sample legal alert tone | Amber warning, not red; the dashboard alert mirrors it |
| UD-UI-14 | Member-facing legal gate layout | Reuse W-1.5 exactly; M13 only supplies the pending documents |
| UD-UI-15 | Should the Configuration screen show an "effective for new activity" note per matching row? | Show it once at the top and repeat it in the Matching group helper |

---
