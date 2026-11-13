# M8 — Safety: Block & Report (Member Side) — UI Plan

| | |
|---|---|
| **Module** | M8 — Safety: Block & Report (spec `docs/spec/08_M8_Safety_Block_Report.md`, functional spec Section 17) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5 (F-6), module design **§6.8 (M8)** + §6.2 (Me hub) + §6.4 (lead detail) + §6.5 (connection detail) + §6.6 (chat header) + §6.7 (rating comment), patterns §7, delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20) |
| **Device/network** | Mid-range Android + Chrome, Safari on iPhone; slow-network tolerant (skeletons, small avatars, no heavy media) |
| **Companion** | API plan: `08_M8_Safety_Block_Report_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M8 gives every member two simple, always-reachable safety tools: **Block** to stop contact immediately, and **Report** to ask Admin to review a person. Safety must never feel buried, slow, or blaming. Block is instant and silent; Report is calm and honest about what happens next.

Screens and jobs:

1. **Block confirmation** (SCR-17.1) — a bottom sheet (centred dialog on desktop) opened from a lead, a connection or a chat. States exactly what will happen, with one red **Block** button and no reason field (BR-17.1, BR-17.2).
2. **Blocked members** (SCR-17.2) — the caller's own list with **Unblock**, and the consequence stated before unblocking (BR-17.3).
3. **Report member** (SCR-17.3) — reason list, optional description (required for "Other"), optional "Also block", the always-visible chat-access notice, and the emergency note (BR-17.5, V-43).
4. **My reports** (SCR-17.4) — the caller's reports with a reference (SAF-), date, reason and a status badge; a decided report says "Your report has been reviewed." and never which action was taken (BR-17.8).
5. **Entry points** — Block and Report in the `⋯` menu of a lead detail, a connection detail and the chat header, plus the rating-comment report hand-off from M7 (UP-3).
6. **Honest states** — loading, empty, already blocked, no contact, submit error, offline, and a report never deleted (BR-17.10).

UI jobs for this module:

- **Safety is one tap away** — Block and Report in every lead/connection/chat `⋯` menu (UP-3).
- **Nothing happens by surprise** — the Block sheet states the effects; the Report form states Admin can read the chat and that the number is 112 (UP-5, BR-17.5).
- **Neutral, never blaming** — Report is a request for review; the reported member is not told; a decided report never names the action (UP-6, BR-17.7/17.8).
- **Show the minimum about people** — only the public card; never email or mobile (UP-4).
- **Every state is designed** — loading, empty, error, already blocked, disabled, offline (UP-10).
- **Accessible by default** — 44 px targets, visible focus, labels, no colour-only status, focus trapped in sheets/dialogs (UP-11).

Out of scope for M8 screens (owned elsewhere, cross-linked): the lead and connection `⋯` menus that host the entry points (M4/M5), the chat header menu (M6), the rating hub and "Report comment" surface (M7), the Admin report queue and actions (M12), notification wording (M9), and the My hub/dashboard shell (M1/M14).

---

## 2. Navigation and routes

M8 has no bottom tab of its own. It is reached from the **Me** hub, the **member dashboard** safety strip, and the **`⋯` menus** of a lead, a connection and a chat (UI Plan §4.1, §6.8).

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Block confirmation | Overlay (bottom sheet / dialog), no standalone route | SCR-17.1 | Lead detail `⋯` → Block (M4); connection detail `⋯` → Block (M5); chat `⋯` → Block (M6); "Also block" on the report form |
| Blocked members | `/blocked` | SCR-17.2 | Me hub → "Blocked members"; member dashboard safety strip |
| Report member | `/reports/new?memberId=&source=&sourceRefId=` | SCR-17.3 | Lead detail `⋯` → Report (M4); connection detail `⋯` → Report (M5); chat `⋯` → Report (M6); rating hub → **Report comment** (M7) |
| My reports | `/reports` | SCR-17.4 | Me hub → "My reports"; member dashboard safety strip; "View my reports" after submitting |
| My report detail | `/reports/[reportId]` | SCR-17.4 | A row in My reports |

Gates that affect M8 (owned by Foundation / M13):

- **Verify-email gate:** Unverified members cannot reach M8 (BR-5.1).
- **Legal re-acceptance gate:** shown before any M8 screen when a required version is pending (BR-22.16).
- **Inactive / Temporarily or Permanently Blocked:** cannot log in (BR-5.4). A **Restricted** member can still load and see M8; safety tools stay available (BR-4.8).
- **Offline:** the Block and Submit report actions are disabled with the standard offline banner.

The entry points are gated by the API's `canBlock` / `canReport` (relation with the other member). The UI never offers Block or Report where there is no lead, connection or chat (BR-17.4, SC-17.6). Until M8 is wired, M4/M5/M6 hide these menu items.

---

## 3. Screen inventory and components

| Screen / part | Components used (UI Plan §3.6) |
|---|---|
| Block sheet | C-24 Bottom sheet (phone) / C-23 Dialog (desktop), C-13 Avatar, C-21 Warning banner (consequences), C-1 Button (destructive "Block", secondary "Cancel") |
| Blocked members | C-13 Avatar, C-33 Key-value list (blocked-on date), C-1 Button (Unblock), C-24 Bottom sheet (unblock confirmation), C-25 Empty state, C-26 Skeleton, C-35 Show more |
| Report form | C-13 Avatar, radio list (C-5 pattern / native radios), C-2 Text field with counter (500), C-6 Checkbox ("Also block"), C-21 Info banner (chat access), C-21 Warning banner (emergency), C-1 Button |
| My reports | C-12 Badge (statuses), C-13 Avatar, C-25 Empty state, C-26 Skeleton, C-35 Show more |
| My report detail | C-33 Key-value list, C-34 History timeline, C-12 Badge, C-21 Info banner |
| Entry points | C-1 Button / menu item in M4/M5/M6 `⋯` menu |

M8 reuses existing components only. The **block sheet** and **unblock sheet** are compositions of C-24/C-23; the **report form** is a normal page on phone and a centred dialog on desktop when opened from a chat. No new reusable component is required.

Status badges use the UI Plan §3.7 mapping: **Report** — Submitted (sky), Under Review (amber), Action Taken (indigo), Dismissed (slate), Closed (slate). Always a word plus colour.

---

## 4. Screen designs

### 4.1 Block confirmation (W-8.1, SCR-17.1)

```text
┌────────────────────────────────────────┐
│  Block Rohit?                          │
│  • You will no longer see each other.  │
│  • Any pending request is withdrawn.   │
│  • Your chat closes. You can still     │
│    read it.                            │
│  • Rohit is not told.                  │
│  [        Block        ]  [ Cancel ]   │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | "Block *Name*?" using the other member's public card (avatar + name). Never email/mobile (UP-4) |
| Consequences | The four fixed lines in Section 17.4 order, as a bulleted list; short, plain, sentence case (UP-5) |
| Buttons | **Block** is destructive (Red 700), **Cancel** secondary; one destructive action per sheet. No reason field — blocking must be fast (BR-17.1) |
| On confirm | Call `POST /safety/blocks`; button shows an inline loading state; on success close the sheet and show the toast "Blocked. You will no longer see each other. They are not told." The lead/connection/chat view refreshes or returns to its list (the pair is gone or the chat is read-only) |
| Already blocked | Entry hidden; a deep open shows the blocked state with an **Unblock** link to `/blocked` |
| No contact | Entry hidden; a deep open shows "You can block someone you have a lead, connection or chat with." |
| Failure | Keep the sheet open; inline error above the buttons with **Try again** (never a bare toast for a blocking failure) |
| Desktop | Centred dialog (max 420 px); same content; Escape/Cancel closes; focus trapped and returned to the trigger |

### 4.2 Blocked members (W-8.2, SCR-17.2)

```text
┌────────────────────────────────────────┐
│  ← Blocked members                     │
│  (Av) Rohit M.                         │
│  Blocked on 09-Oct-2026     [ Unblock ]│
│  (Av) Neha S.                          │
│  Blocked on 02-Oct-2026     [ Unblock ]│
│              [ Show more ]             │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Rows | Public card (avatar + name), "Blocked on *date*", one **Unblock** button. No reason, no email/mobile (none is stored/shown) |
| Order | API order (newest first); the UI never re-sorts |
| Unblock | Opens a sheet: "Unblock *Name*? You may see each other in leads again. This does not bring back the old chat; a new request is needed." with **Unblock** (secondary/confirm) and **Cancel** (BR-17.3) |
| Empty | "You have not blocked anyone." with no action button (nothing useful to offer) |
| Paging | First page, then **Show more** (C-35, keyset) |
| Loading | Skeleton rows (C-26), never a bare spinner |
| Error | Inline "We could not load your blocked members." with **Try again** |
| Privacy | Only the caller's blocks are listed; a blocked party never sees this row (BR-17.2) |

### 4.3 Report member (W-8.3, SCR-17.3)

```text
┌────────────────────────────────────────┐
│  ← Report Rohit                        │
│  (Av) Rohit M. · Raahi · Bike ✓        │
│  What happened?                        │
│  ( ) Harassment                        │
│  ( ) Inappropriate messages            │
│  ( ) Fake profile or wrong details     │
│  ( ) Safety concern                    │
│  ( ) Spam                              │
│  (•) Other                             │
│  Details (required for Other)          │
│  [______________________________]      │
│  0 / 500                               │
│  [ ] Also block Rohit                  │
│  ┌──────────────────────────────────┐  │
│  │ (i) Admin can read your chat with │  │
│  │ Rohit to review this report.      │  │
│  └──────────────────────────────────┘  │
│  RAAH does not provide emergency       │
│  services. If you are in danger, call  │
│  112.                                  │
│  [       Submit report        ]        │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | Public card + name; "What happened?" prompt. Never email/mobile |
| Reasons | Six radios (A-46) in the fixed order: Harassment · Inappropriate messages · Fake profile or wrong details · Safety concern · Spam · Other. Single choice; **Submit** disabled until one is chosen |
| Description | One field, counter "0 / 500"; label becomes "Details (required for Other)" when Other is chosen. Over the limit blocks submit with "Details can be up to 500 characters." |
| Also block | Checkbox (C-6); when ticked, the same block effects apply immediately (BR-17.6). Hidden or disabled when already blocked, with the line "You have already blocked *Name*." |
| Chat access notice | C-21 Info banner, **always visible before submit** (BR-17.5). Copy: "Admin can read your chat with *Name* to review this report." Shown only when a chat exists; when none, the notice is absent |
| Emergency note | C-21 Warning banner, always visible: "RAAH does not provide emergency services. If you are in danger, contact 112." |
| After submit | Success screen/toast: "Report sent. We will review it." plus a link **View my reports**. The report form is not re-openable for editing |
| Not allowed | If opened without contact, show "You can report someone you have a lead, connection or chat with." with Back |
| Failure | Keep the form and all input; inline error above Submit; never clear the reason or description |
| Desktop | Centred dialog (max 520 px) when opened from chat; otherwise a normal page. Escape/Cancel confirms discarding typed details |

### 4.4 My reports (W-8.4, SCR-17.4)

```text
┌────────────────────────────────────────┐
│  ← My reports                          │
│  SAF-000009 · Rohit M.       [Sky]     │
│  Inappropriate messages · 09-Oct-2026  │
│  Submitted                             │
│  ────────────────────────────────────  │
│  SAF-000004 · Neha S.        [Sky]     │
│  Spam · 02-Oct-2026                    │
│  Under Review                          │
│  ────────────────────────────────────  │
│  SAF-000001 · Amit R.        [Slate]   │
│  Other · 28-Sep-2026                   │
│  Closed · Your report has been reviewed│
│              [ Show more ]             │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Row | Report number (SAF-), reported member's public card, reason label, submitted date (`09-Oct-2026`), status badge with the exact words (Submitted / Under Review / Action Taken / Dismissed / Closed) |
| Decided rows | Add "Your report has been reviewed." and **never** the action or reason (BR-17.8). No "we blocked them" wording |
| Detail | Tapping a row opens `/reports/[reportId]`: reason, own description, conversation-attached flag, and a **History timeline** (C-34) built from the status dates. The decided step reads only "Your report has been reviewed." |
| Order | API order (newest first); never re-sorted |
| Paging | **Show more** (C-35) |
| Empty | "You have not reported anyone. You can report a person from a lead, a connection or a chat." with no fake action |
| Loading / error | Skeleton rows / inline "We could not load your reports." + **Try again** |
| Never deleted | Closed and Dismissed reports remain listed read-only (BR-17.10) |

### 4.5 Entry points in other screens (owned by M4/M5/M6/M7, M8 owns the flows)

| Location | Design |
|---|---|
| Lead detail `⋯` (M4) | **Block** and **Report** items; each opens the M8 sheet/page with the lead's other member. Hidden when `canBlock`/`canReport` is false and until M8 lands |
| Connection detail `⋯` (M5) | **Block** and **Report** as above; Block states the chat closes |
| Chat header `⋯` (M6) | **Block** and **Report**; the chat becomes read-only after a block (M5/M6 state), the safety tip stays visible |
| Rating hub — received comment (M7) | **Report comment** opens the M8 report form with the comment attached; the reporter is the rated member (BR-16.9, BR-17.12) |
| Me hub (M1) | "Blocked members" → `/blocked` and "My reports" → `/reports` (UI Plan §6.2) |
| Member dashboard safety strip (M14) | "Blocked members · My reports" links (UI Plan §6.14) |

The copy and consequences are M8's; the menus themselves stay with the owning module (UP-3).

### 4.6 Ratings-comment report (hand-off from M7)

| Aspect | Design |
|---|---|
| Trigger | **Report comment** on a received, visible rating comment (M7) |
| Form | The same report form (SCR-17.3), with the reported member being the rater and the comment attached |
| Notice | "Admin can read this rating comment to review this report." in place of the chat notice |
| After submit | "Report sent. We will review it." with **View my reports** |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton rows and skeleton detail (C-26); never a bare spinner |
| Empty (blocked) | "You have not blocked anyone." |
| Empty (reports) | "You have not reported anyone. You can report a person from a lead, a connection or a chat." |
| Error (load) | Inline message with **Try again**; the screen header stays usable |
| Error (action) | Keep the sheet/form open; inline error above the action button; never clear input |
| Success (block) | Sheet closes; toast "Blocked. You will no longer see each other. They are not told."; origin view refreshes |
| Success (report) | "Report sent. We will review it." + **View my reports** |
| Already blocked | Block entry hidden; an opened context shows the blocked state with an **Unblock** link |
| No contact / not allowed | "You can report/block someone you have a lead, connection or chat with." with Back |
| Report Other empty | Inline "Please describe what happened when you choose Other."; Submit blocked |
| Over limit | Counter amber; Submit disabled with "Details can be up to 500 characters." |
| No permission / foreign id | "This report is not available." / "This block is no longer active." with Back; never reveal another member's data |
| Offline | Slim banner "No connection. Changes will not be saved."; Block and Submit disabled |
| Session expired | Foundation dialog; typed reason/description preserved where possible (BR-5.6) |
| Verify / legal gate | Foundation; M8 not reachable until cleared |
| Restricted member | Safety tools remain usable; no restriction banner on M8 (BR-4.8) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Block and unblock are **bottom sheets** over the origin screen; the report form is a full page from a lead/connection or a sheet from a chat; single column; 44 px targets |
| Large phone 480–767 | Same layouts, wider gutters |
| Tablet 768–1023 | Sheets become centred dialogs; lists in a single wider card |
| Desktop 1024+ | Dialogs for block/unblock/report-from-chat; `/blocked`, `/reports`, `/reports/[id]` content max 720 px inside the member content area |
| Wide 1280+ | Member content max 1120 px; do not stretch rows edge to edge |

- The shell switches bottom tabs to a left sidebar at tablet/desktop (UI Plan §4.1); M8 does not build the shell.
- Respect safe areas and the on-screen keyboard for the report description and the action buttons.
- At 200% text zoom there is no horizontal scroll; long names and descriptions wrap.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2; the destructive Block button uses Red 700 with white text |
| Focus | Visible 2 px Indigo focus ring; sheets/dialogs trap focus and return it to the triggering `⋯` item / Unblock button |
| Labels | "Block *Name*?" heading; reason group labelled "What happened?"; description has a visible label + counter; "Also block *Name*" has a real label |
| Targets | ≥ 44 × 44 px, 8 px apart (reasons, checkbox, Block/Unblock, Submit, Show more) |
| Colour | Every status has a word; consequences are a text list, not colour; the emergency note is text, not an icon alone |
| Screen readers | Sheets announce their title and consequences; radio group announces the selected reason; success and errors are announced politely (live region); the "They are not told." line is read as text |
| Keyboard | Radios move with arrow keys; Enter submits when valid; Escape closes the sheet/dialog; focus never escapes a modal |
| Motion | Honour "reduce motion"; standard 150–200 ms sheet transition only |
| Language | `lang="en"`; dates `09-Oct-2026`, India time |

---

## 8. Microcopy (M8)

| Situation | Copy |
|---|---|
| Block title | "Block *Name*?" |
| Block consequences | "You will no longer see each other." · "Any pending request is withdrawn." · "Your chat closes. You can still read it." · "*Name* is not told." |
| Block / Cancel | "Block" · "Cancel" |
| Block success | "Blocked. You will no longer see each other. They are not told." |
| Block not allowed | "You can block someone you have a lead, connection or chat with." |
| Blocked empty | "You have not blocked anyone." |
| Blocked row | "Blocked on *date*" |
| Unblock sheet | "Unblock *Name*? You may see each other in leads again. This does not bring back the old chat; a new request is needed." |
| Unblock success | "You have unblocked *Name*." |
| Report title | "Report *Name*" |
| Report prompt | "What happened?" |
| Reason labels | Harassment · Inappropriate messages · Fake profile or wrong details · Safety concern · Spam · Other |
| Description | "Details (required for Other)" · "0 / 500" |
| Other empty | "Please describe what happened when you choose Other." |
| Over limit | "Details can be up to 500 characters." |
| Also block | "Also block *Name*" |
| Chat access notice | "Admin can read your chat with *Name* to review this report." |
| Rating-comment notice | "Admin can read this rating comment to review this report." |
| Emergency note | "RAAH does not provide emergency services. If you are in danger, contact 112." |
| Submit | "Submit report" |
| Report success | "Report sent. We will review it." |
| Report not allowed | "You can report someone you have a lead, connection or chat with." |
| My reports title | "My reports" |
| Decided row | "Your report has been reviewed." |
| Reports empty | "You have not reported anyone. You can report a person from a lead, a connection or a chat." |
| Statuses | "Submitted" · "Under Review" · "Action Taken" · "Dismissed" · "Closed" |
| Not available | "This report is not available." · "This block is no longer active." |
| Load error | "We could not load this." with "Try again" |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8, UP-6). The emergency number is 112 (India, spec Appendix B).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Open block sheet | `GET /safety/block-context/{memberId}?source=&sourceRefId=` |
| Confirm block | `POST /safety/blocks` `{ targetMemberId, source, sourceRefId }` |
| Blocked list | `GET /safety/blocked?limit=&cursor=` |
| Confirm unblock | `POST /safety/blocks/{blockId}/unblock` |
| Open report form | `GET /safety/report-context/{memberId}?source=&sourceRefId=` |
| Submit report | `POST /safety/reports` `{ targetMemberId, reasonCode, description, alsoBlock, source, sourceRefId }` |
| My reports | `GET /safety/reports?limit=&cursor=` |
| My report detail | `GET /safety/reports/{reportId}` |
| Show more | same list endpoint with `cursor=<nextCursor>` |
| Report a rating comment | `POST /ratings/{ratingId}/report-comment` (M7 → M8 intake) |
| Entry availability | M4/M5/M6 DTO flags `canBlock` / `canReport` |

Rules for the client:

- Always send the auth cookie/token; derive the member on the server; never send an actor member id.
- Trust the server for eligibility: if `canBlock`/`canReport` is false, do not render or open the action.
- Never re-order lists; trust the API order.
- On `409 ALREADY_BLOCKED`, switch to the blocked state and show the toast "You have already blocked this person."
- On `409 REPORT_STATUS_INVALID` (should not occur from the member UI), refresh the report and keep the view read-only.
- On `400 REPORT_DESCRIPTION_REQUIRED` / `REPORT_DESCRIPTION_TOO_LONG`, show the inline error beside the field and keep input.
- On `403 REPORT_TARGET_NOT_ALLOWED` / `BLOCK_NOT_ALLOWED`, show the not-allowed copy and a Back action.
- On `500 SAFETY_ACTION_FAILED`, keep the sheet/form open with **Try again**; do not assume the block happened.
- Never expect or display `DecisionAction` or the reporter identity in any member view.
- Reuse the same chosen reason/description/also-block values on retry after a transient error.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M4 lead detail menu | The Block/Report entry is hidden on the lead surface |
| M5 connection detail menu | The Block/Report entry is hidden on the connection surface |
| M6 chat | The chat header `⋯` Block/Report entry is hidden; no chat attachment notice |
| M7 rating hub | **Report comment** is hidden |
| M9 notifications | No "report decided" badge/notification; My reports still shows status on reload |
| M12 Admin handling | Reports stay `Submitted`; no member-facing effect |
| M13 config | Use Foundation values (CFG-15, CFG-32); do not hard-code limits |
| Design tokens | Use UI Plan §3 values; no hard-coded colours |

---

## 11. Delivery plan (M8 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Block sheet + unblock sheet as compositions of C-24/C-23 | UI Plan §3 |
| 2 | Block flow: context load, consequences, destructive confirm, success/toast, refresh origin | API 1, 2 |
| 3 | Blocked members page: rows, empty/loading/error, Show more | API 3 |
| 4 | Unblock confirmation and result | API 4 |
| 5 | Report form: reasons, description counter, Other rule, also-block, chat notice, emergency note | API 5, 6 |
| 6 | Report success screen + View my reports link | API 6 |
| 7 | My reports list: status badges, decided copy, empty/loading/error, Show more | API 7 |
| 8 | My report detail + history timeline | API 8 |
| 9 | Wire entry points in M4/M5/M6 `⋯` menus (flags) and the Me hub/dashboard links | M4/M5/M6, M1/M14 |
| 10 | Rating-comment report hand-off from M7 | M7, API 6 |
| 11 | Responsive dialogs, accessibility pass, QA states | — |

Suggested route files: `blocked/index`, `reports/index`, `reports/new`, `reports/[reportId]`. The block and unblock confirmations are overlays owned by M8 but rendered over M4/M5/M6 screens.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-17.1 | Confirming Block states the mutual effects and, after it succeeds, the lead/pair disappears and the chat is read-only |
| AC-17.2 | No screen, toast or notification tells the blocked member they were blocked; the consequence line says "They are not told." |
| AC-17.3 | Submitting a report without "Also block" applies no block; only the ticked case blocks immediately |
| AC-17.4 | No member screen reveals the reporter's identity to the reported member |
| AC-17.5 | (Admin) the flag/alert surfaces in M12; M8 shows nothing member-facing |
| BR-17.1 | Block is reachable from a lead, a connection and a chat only; no "block anyone" screen |
| BR-17.2 | Block copy explicitly says the other person is not told |
| BR-17.3 | Unblock sheet says the old chat is not restored and a new request is needed |
| BR-17.4 | Report is reachable only with a lead, connection or chat; a direct attempt shows the not-allowed copy |
| BR-17.5 | The chat-access notice is visible before Submit whenever a chat exists |
| BR-17.6 | Report copy frames it as a request for review; only "Also block" changes anything |
| BR-17.7 | No reported-member surface names the reporter |
| BR-17.8 | My reports shows the status words and "Your report has been reviewed." only — never the action |
| BR-17.10 | Dismissed and Closed reports stay in My reports; there is no delete control |
| V-43 | A reason is required; "Other" requires details; 500 max; exact copy |
| V-44 | A stranger cannot be reported; the entry is absent |
| UP-3 | Block and Report are in every lead/connection/chat `⋯` menu (when M8 exists) |
| UP-4 | No email or mobile is rendered anywhere |
| UP-5 | The Block sheet and Report form state the consequences before the destructive action |
| UP-6 | Neutral wording everywhere; no blame, no action disclosure |
| UP-10 | Loading, empty, error, already-blocked, not-allowed, over-limit, offline states are implemented |
| UP-11 | Contrast, focus trap/return, labels, targets, keyboard, SR announcements, 200% zoom pass |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| — | Is Block a sheet or a full page? | Bottom sheet on phone, centred dialog on desktop; no standalone route |
| — | Does the Block sheet show the member's public card header? | Yes — one line (avatar + name) so the member knows whom they are blocking |
| — | Is a reason ever asked for a block? | **No** — blocking must be fast (BR-17.1) |
| — | Where do Block/Report live? | In the `⋯` menu of lead, connection and chat; not as primary buttons (avoids accidental taps) |
| — | Does the Report form open as a sheet or page? | Full page from lead/connection; centred dialog from chat on desktop |
| — | Is the emergency note dismissible? | **No** — always visible before Submit |
| — | Is the chat-access notice shown without a chat? | No; it appears only when a conversation will be attached |
| — | Does the "Also block" option appear when already blocked? | No; show "You have already blocked *Name*." instead |
| — | Should My reports have a detail page? | Yes — timeline helps the reporter follow up (SC-17.9) |
| — | Is there an "Undo block" snackbar? | No — Unblock lives in `/blocked`; an immediate undo would undercut the "immediate and mutual" promise |
| — | Should the blocked list show the route/role? | No — keep the minimum (public card + date + Unblock) |
| — | Can a member report from My reports again? | No "report anyone" screen; a new report starts from a lead/connection/chat |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-17.1 Block Member | §4.1 |
| SCR-17.2 Blocked Members | §4.2 |
| SCR-17.3 Report Member | §4.3 |
| SCR-17.4 My Reports | §4.4, §4.5 |
| Field spec (Report) | §4.3 |
| Section 17.4 Block effects | §4.1 |
| BR-17.1 block immediate/mutual | §4.1, §4.5 |
| BR-17.2 blocked member not told | §4.1, §8, §12 |
| BR-17.3 unblock; no restore | §4.2 |
| BR-17.4 report eligibility | §2, §4.3 |
| BR-17.5 chat access notice | §4.3, §8 |
| BR-17.6 report does not act | §4.3 |
| BR-17.7 reporter privacy | §4.4, §12 |
| BR-17.8 status, never action | §4.4, §8 |
| BR-17.9 escalation (Admin) | §12 (out of member UI) |
| BR-17.10 never deleted | §4.4, §5 |
| BR-17.12 rating comment report | §4.5, §4.6 |
| Status lifecycle §9.7 | §3, §4.4 |
| CFG-15 / CFG-32 | §10 (Admin only / config) |
| V-43 / V-44 | §4.3, §5, §12 |
| SC-17.1…SC-17.9 | §4, §5 |
| AC-17.1…AC-17.5 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §5 F-6 | §4.1, §4.3 |
| UI Plan §6.2 Me hub | §2, §4.5 |
| UI Plan §6.4 lead detail | §4.5 |
| UI Plan §6.5 connection detail | §4.5 |
| UI Plan §6.6 chat header | §4.5 |
| UI Plan §6.7 rating comment | §4.6 |
| UI Plan §6.8 (W-8.1…W-8.4) | §4 |
| UI Plan §7 patterns | §4, §5 |
| UI Plan §8 delivery | §11 |
