# M7 — Rating & Feedback — UI Plan

| | |
|---|---|
| **Module** | M7 — Rating & Feedback (spec `docs/spec/07_M7_Rating_Feedback.md`) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5, module design **§6.7 (M7)** + §6.5 (connection entry) + §6.2 (Me hub), patterns §7, delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20) |
| **Device/network** | Mid-range Android + Chrome, Safari on iPhone; slow-network tolerant (skeletons, small avatars, no heavy media) |
| **Companion** | API plan: `07_M7_Rating_Feedback_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M7 lets connected members rate each other and lets a member review the ratings they received and gave. Rating builds trust but never affects matching (BR-16.10); it informs a member's own choice only.

Screens and jobs:

1. **Rate member** (SCR-16.1) — 1–5 stars and an optional comment, opened from a connection or the chat header; a bottom sheet on phone, a centred dialog on desktop.
2. **Ratings hub** (SCR-16.2 / SCR-16.3) — two segments, **Received** and **Given**, with the member's own summary header.
3. **Summary everywhere** — the same average/count (or "New member") on Me, profile preview, lead cards and connection detail, supplied by M1's projection.
4. **Hidden comment** display — "Comment hidden" while the stars still count (BR-16.8).
5. **Report a comment** — hand a received comment to the M8 report flow (BR-16.9, BR-17.12).
6. **Honest states** — loading, empty, already rated, not eligible, rating disabled, over-limit, contact detail in comment, error and offline.

UI jobs for this module:

- **Explain, don't pressure** — the rating is optional and final; the sheet says so before the member taps Submit (UP-5).
- **Show the minimum about people** — only public cards; never email or mobile (UP-4).
- **Neutral, never blaming** — a hidden comment says "Comment hidden", never why (UP-6).
- **Every state is designed** — disabled, already rated, hidden, empty, error (UP-10).
- **Accessible stars** — 44 px targets, keyboard arrows, spoken value, never colour-only (UP-11).

Out of scope for M7 screens (owned elsewhere but cross-linked): the connection detail and chat header entry (M5/M6), profile and the public preview (M1), the report form and report list (M8), the Admin rating-moderation screen (M12), and notification wording (M9).

---

## 2. Navigation and routes

M7 has no tab of its own. It is reached from the **Me** hub, a **connection detail**, or the **chat** `⋯` menu (UI Plan §4.1, §6.7).

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Rate member | `/ratings/[connectionId]` | SCR-16.1 | Connection detail **Rate** button (M5); chat `⋯` → **Rate** (M6); after disconnect still available if eligible |
| Ratings hub — Received | `/ratings` (default) | SCR-16.2 | Me hub → "Ratings I received and gave"; notification "You received a rating" (M9) |
| Ratings hub — Given | `/ratings?view=given` | SCR-16.3 | Received/Given segment |

Gates that affect M7 (owned by Foundation / M13):

- **Verify-email gate:** Unverified members cannot reach M7 (BR-5.1).
- **Legal re-acceptance gate:** shown before any M7 screen when a required version is pending (BR-22.16).
- **Rating disabled (CFG-13 = No):** the **Rate** action is hidden and a small info line explains "Rating is not available right now."; **Received/Given** stay readable.
- **Inactive/Blocked:** cannot log in (BR-5.4).

The **Rate** action is shown only when the connection is eligible (`canRate` / `wasEverAccepted`) and the viewer has not already rated (`alreadyRated`); otherwise the connection detail shows "You rated this connection" (V-42).

---

## 3. Screen inventory and components

| Screen / part | Components used (UI Plan §3.6) |
|---|---|
| Rate sheet | C-24 Bottom sheet (phone) / C-23 Dialog (desktop), C-13 Avatar, C-14 Rating stars (input), C-2 Text field with counter, C-21 Info banner, C-1 Button, C-22 Toast |
| Ratings hub | C-11 Segmented control (Received/Given), C-14 Rating stars (read-only), C-12 Badge (Submitted / Comment hidden), C-13 Avatar, C-25 Empty state, C-26 Skeleton, C-35 Show more |
| Summary header | C-14 Rating stars (read-only, 1 decimal + count), C-12 "New member" chip |
| Hidden comment | C-12 "Comment hidden" badge, C-1 Button ("Report comment") |
| Connection entry | C-1 Button ("Rate" / "You rated this connection") |
| Report hand-off | M8 report flow (C-24/C-23, radio list, C-2 details) |

M7 reuses **C-14 Rating stars** (both read-only and input), **C-11 Segmented control**, **C-24 Bottom sheet**, **C-13 Avatar**, **C-12 Badge**, **C-26 Skeleton** and **C-22 Toast**. No new reusable component is needed; the rate sheet is a composition of existing components.

---

## 4. Screen designs

### 4.1 Rate member (W-7.1, SCR-16.1)

```text
┌────────────────────────────────────────┐
│  Rate Neha                             │
│  (Av 40) Neha S. · Raahi · Car ✓       │
│  How was travelling with Neha?         │
│     ☆  ☆  ☆  ☆  ☆      (tap 1 to 5)   │
│                                         │
│  Comment (optional)                    │
│  [______________________________]      │
│  0 / 300                               │
│  (i) Neha sees your stars and comment. │
│  Others only see her average. A rating │
│  cannot be changed once submitted.     │
│                                         │
│  [      Submit rating      ]  Cancel   │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Header | The other member's public card: avatar, name, role, vehicle, verified badge. Never email/mobile (UP-4) |
| Stars (C-14 input) | Five 44 px targets; tap to set 1–5; keyboard arrows move the value; screen reader announces "4 of 5"; selected stars filled amber with a text value beside them |
| Comment | One field, optional; **counter to CFG-27** (default 300). Counter turns amber near the limit; Send/Submit disabled over the limit |
| Contact-detail rule | A link or a run of 10+ digits is refused inline: "Please do not enter phone numbers or links." (V-41, SC-16.8). The client may warn before submit but the server is authoritative |
| Fixed notice | Always visible: the rated member sees the comment; others see only the average; a rating is final (BR-16.3/16.5, UP-5) |
| Submit | Disabled until a star is chosen. On success: close the sheet and show the toast "Thanks. Your rating was saved."; the entry point changes to "You rated this connection" |
| Already rated | The sheet is not opened; the entry shows "You rated this connection" (V-42). A deep link shows a read-only card with the member's own stars/comment/date |
| Rating disabled | The action is hidden; a deep link shows "Rating is not available right now." |
| Not eligible | A deep link shows "You can rate someone only after you have been connected." with Back |
| Desktop | A centred dialog (max 480 px); same content; Escape/Cancel closes |

### 4.2 Ratings hub — Received (W-7.2, SCR-16.2)

```text
┌────────────────────────────────────────┐
│  Ratings      [ Received ] [ Given ]   │
│  ★ 4.6   12 ratings                    │
│  ┌──────────────────────────────────┐  │
│  │ ★★★★★  Neha S.        09-Oct     │  │
│  │ Punctual and friendly.           │  │
│  ├──────────────────────────────────┤  │
│  │ ★★★☆☆  Rohit M.       02-Oct     │  │
│  │ Comment hidden                   │  │
│  │                  Report comment  │  │
│  └──────────────────────────────────┘  │
│              [ Show more ]             │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Segments | **Received** (default) / **Given** (C-11); each shows a count when > 0. The URL drives the segment (`?view=given`) |
| Summary header | Average to one decimal, star row, and "N ratings". Under CFG-28 it shows a **"New member"** chip instead of an average (BR-16.6) |
| Row | Star row (always shown), the other member's public card (rater on Received, rated member on Given — BR-16.7), date (`09-Oct-2026` style), comment |
| Hidden comment | The comment text is replaced by the words **"Comment hidden"** (C-12 slate badge); the stars remain visible and still count (BR-16.8/16.11) |
| Report action | **Report comment** appears only on Received, when a visible comment exists; it opens the M8 report flow with the comment attached (BR-16.9, BR-17.12) |
| Order | Exactly the API order (newest first); the UI never re-sorts |
| Paging | First page from the API, then **Show more** (C-35, keyset) |
| Clamping | Long comments wrap; no "read more" is required in Phase 1 unless a comment hits the limit |

### 4.3 Ratings hub — Given (SCR-16.3)

Same layout. Each row shows the **rated** member, the rater's own stars, their own comment, and the date. There is **no edit or delete** control anywhere (BR-16.3). A comment hidden by Admin shows "Comment hidden" with the stars still visible.

### 4.4 Summary on other screens (owned by M1/M4/M5, fed by M7)

| Location | Design |
|---|---|
| Me hub (W-2.1) | "★ 4.6 (12 ratings)" beside role and verified badge; "New member" under CFG-28 |
| How others see me / lead card / lead detail / connection detail | The same average and count, one decimal, with a count; never a list of comments (BR-16.5) |
| No ratings yet | "New member" (not "0.0" and not an empty star row) |

M7 supplies the number; those screens own the placement.

### 4.5 Report a comment (hand-off to M8)

| Aspect | Design |
|---|---|
| Entry | **Report comment** on a received, visible comment |
| Flow | Opens the M8 report form (SCR-17.3) with the reason list; the comment is attached and the reporter is the rated member |
| Copies | "Report this comment?" and the standard safety text from M8. The reporter is told Admin can read the rating comment (BR-16.9, BR-17.12) |
| After submit | "Report sent. We will review it." with a link to My reports (M8) |
| Hidden already | No report action on a hidden comment |
| Placeholder | If M8 is not built, the action is hidden |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton rating rows and a skeleton summary header (C-26); never a bare spinner |
| Empty (Received) | "No ratings yet. Ratings come from members you have been connected with." |
| Empty (Given) | "You have not rated anyone yet. After a connection, you can rate from the connection or the chat." |
| Error (load) | Inline message with one **Try again**; the segment header stays usable |
| Error (submit) | Keep the sheet open, show the error beside the field or above the button; never clear the chosen stars/comment |
| Success | Toast "Thanks. Your rating was saved."; entry point switches to the rated state |
| Already rated | Entry shows "You rated this connection"; a deep link shows a read-only card |
| Not eligible | "You can rate someone only after you have been connected." |
| Rating disabled | Action hidden; info line "Rating is not available right now." |
| Over limit | Counter amber, Submit disabled, "Comments can be up to *n* characters." |
| Contact detail | Inline error "Please do not enter phone numbers or links." |
| Hidden comment | "Comment hidden" text instead of the comment; stars still shown |
| No permission / foreign id | "This rating is not available." with Back; never reveal another member's data |
| Offline | Slim banner "No connection. Changes will not be saved."; Submit disabled |
| Session expired | Foundation dialog; typed comment preserved where possible (BR-5.6) |
| Verify / legal gate | Foundation; M7 not reachable until cleared |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Rate sheet is a bottom sheet over the connection/chat; Ratings hub is single column with sticky segments; stars 44 px |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | Rate becomes a centred dialog; hub keeps a single column with a wider card |
| Desktop 1024+ | Centred dialog for rate; hub content max 720 px inside the member content area; segments remain |
| Wide 1280+ | Member content max 1120 px; do not stretch rating rows edge to edge |

- The shell switches bottom tabs to a left sidebar at tablet/desktop (UI Plan §4.1); M7 does not build the shell.
- Respect safe areas and the on-screen keyboard for the bottom sheet and the comment field.
- At 200% text zoom there is no horizontal scroll; long comments and names wrap.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2; amber stars have a text value beside them |
| Focus | Visible 2 px Indigo focus ring; the sheet/dialog traps focus and returns it to the **Rate** trigger |
| Labels | "Rate Neha" heading; star group labelled "Your rating, 1 to 5 stars"; comment field has a visible label and counter |
| Targets | ≥ 44 × 44 px, 8 px apart (stars, Submit, Cancel, Report comment, Show more) |
| Colour | Stars carry a number ("4 of 5") and the summary carries the number; hidden comments have words, not just grey |
| Screen readers | Star group announces the current value and arrow-key changes; submit success announced politely (live region); "Comment hidden" is read as text |
| Keyboard | Stars move with arrow keys; Enter submits from the comment when a star is chosen; Escape closes the sheet/dialog |
| Motion | Honour "reduce motion"; the sheet uses the standard 150–200 ms ease and nothing more |
| Language | `lang="en"`; dates `09-Oct-2026`, India time |

---

## 8. Microcopy (M7)

| Situation | Copy |
|---|---|
| Rate title | "Rate *Name*" · question "How was travelling with *Name*?" |
| Stars hint | "(tap 1 to 5)" · screen-reader value "*n* of 5" |
| Comment label | "Comment (optional)" · counter "0 / *CFG-27*" |
| Fixed notice | "*Name* sees your stars and comment. Others only see their average. A rating cannot be changed once submitted." |
| Submit / Cancel | "Submit rating" · "Cancel" |
| Success | "Thanks. Your rating was saved." |
| Already rated | "You rated this connection." |
| Not eligible | "You can rate someone only after you have been connected." |
| Rating disabled | "Rating is not available right now." |
| Over limit | "Comments can be up to *n* characters." |
| Contact detail | "Please do not enter phone numbers or links." |
| Duplicate (server) | "You have already rated this connection." |
| Received heading | "Ratings" · segments "Received" / "Given" |
| Summary | "★ 4.6   12 ratings" · under threshold "New member" |
| Hidden comment | "Comment hidden" |
| Report comment | "Report comment" |
| Empty received | "No ratings yet. Ratings come from members you have been connected with." |
| Empty given | "You have not rated anyone yet. After a connection, you can rate from the connection or the chat." |
| Not available | "This rating is not available." |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8, UP-6).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Open rate sheet | `GET /ratings/context/{connectionId}` |
| Submit rating | `POST /ratings` `{ connectionId, stars, comment }` |
| Received list | `GET /ratings/received?limit=&cursor=` |
| Given list | `GET /ratings/given?limit=&cursor=` |
| Show more | same endpoint with `cursor=<nextCursor>` |
| Report a comment | `POST /ratings/{ratingId}/report-comment` → M8 report record |
| Rate entry state | M5 `GET /connections/{connectionId}` (`rating.canRate`, `rating.alreadyRated`) |
| Summary elsewhere | M1/M4/M5 DTOs (`rating.average/count/isNewMember`) |

Rules for the client:

- Always send the auth cookie/token; derive the member on the server; never send a member id.
- Never re-order rows; trust the API order (server date/id).
- Trust the server for eligibility: if `canRate` is false, do not open the sheet.
- On `409 ALREADY_RATED`, switch the entry to "You rated this connection" and close the sheet.
- On `400 COMMENT_CONTAINS_CONTACT` / `COMMENT_TOO_LONG`, show the inline error and keep the input.
- On `403 RATING_DISABLED`, hide the action and show the info line.
- On a hidden comment, render "Comment hidden"; never show the text because the API omits it.
- Reuse the same star/comment values on retry after a transient error.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M5 accepted connections | No connection detail; no rate entry |
| M1 profile projection | Summary shows "New member" until the first rating |
| M6 chat | The chat `⋯` → Rate entry is hidden |
| M8 report flow | **Report comment** is hidden |
| M9 notifications | Rating-received badge/notification does not appear; the hub still loads |
| M12 moderation | Hidden comments arrive with the text already removed; no admin UI in the member app |
| M13 config | Use Foundation values; do not hard-code limits or thresholds |
| Design tokens | Use UI Plan §3 values; no hard-coded colours |

---

## 11. Delivery plan (M7 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Reuse C-14 Rating stars (input + read-only) and C-11 segments | UI Plan §3 |
| 2 | Rate sheet/dialog: stars, comment, counter, fixed notice, submit states | API 1, 2 |
| 3 | Entry state on connection detail and chat menu (Rate / You rated this connection) | M5/M6, API 1 |
| 4 | Ratings hub: Received/Given segments, summary header, rows, hidden comment | API 3, 4 |
| 5 | Paging (Show more) and skeleton/empty/error states | API 3, 4 |
| 6 | Inline validation for limit and contact details; server-error mapping | API 2 |
| 7 | Report-comment hand-off to M8 | M8, API 5 |
| 8 | Desktop dialog + responsive layout | — |
| 9 | Accessibility pass (star group, focus return, live region, keyboard, 200% zoom) | — |
| 10 | QA: already rated, not eligible, disabled, hidden, offline, slow network, privacy | API running |
| 11 | Enable the Report action when M8 lands | M8 |

Suggested route files: `ratings/index` (hub with `?view=`), `ratings/[connectionId]` (sheet/dialog). The connection/chat entry points are owned by M5/M6.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-16.1 | No Rate action exists for someone who was never connected; a deep link shows the not-eligible copy |
| AC-16.2 | After submitting, the entry reads "You rated this connection"; a second submit is refused |
| AC-16.3 | Under CFG-28 the profile/hub shows "New member" and no average |
| AC-16.4 | A hidden comment shows "Comment hidden"; the stars and the average are unchanged |
| AC-16.5 | No other member's comment is ever shown; only the average and count appear outside the hub |
| BR-16.3 | There is no edit or delete control on any rating |
| BR-16.4 | Stars are required; the comment is optional; Submit is disabled without a star |
| BR-16.6 | "New member" appears below the CFG-28 threshold |
| BR-16.7 | On Received, the rater's name is visible to the rated member |
| BR-16.8 | Moderation removes only the comment; an explanation is never shown |
| BR-16.9 | **Report comment** reaches the M8 flow (when M8 exists) |
| V-41 | Over-limit and contact-detail comments are blocked with the exact copy |
| V-42 | An already-rated connection cannot be rated again |
| UP-3 | Safety is reachable from the same entry points (via M6/M8) |
| UP-4 | No email or mobile is rendered anywhere |
| UP-10 | Loading, empty, error, already-rated, not-eligible, disabled, hidden, over-limit, offline states are implemented |
| UP-11 | Contrast, focus, labels, star keyboard control, targets, SR announcements, 200% zoom pass |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| — | Bottom sheet or full screen for Rate? | Bottom sheet on phone, centred dialog on desktop (UI Plan §6.7) |
| — | Where does the Ratings hub live? | Only from the **Me** hub; no bottom tab |
| — | Received/Given as segments or tabs? | Segments (C-11), matching Connections/Chats |
| — | Does the rate sheet show the average of the person being rated? | No — it would pressure the rater; only the public card and the fixed notice |
| — | Can the rater see their own hidden comment? | Yes, on **Given**, with "Comment hidden" (matches the API default) |
| — | Is the comment counter always visible or only near the limit? | Always visible at 300 chars; turns amber near the limit |
| — | Does the hub show the count on segments? | Yes when > 0, to help orientation |
| — | Is there a "Rate later" reminder? | No in Phase 1; the entry stays on the connection/chat |
| — | What if CFG-13 is off? | Hide the Rate action; keep Received/Given readable |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-16.1 Rate Member | §4.1 |
| SCR-16.2 Ratings Received | §4.2 |
| SCR-16.3 Ratings Given | §4.3 |
| Field spec (Rating) | §4.1, §4.2 |
| BR-16.1 only connected members | §2 gates, §4.1 |
| BR-16.2 one per connection | §4.1 already-rated |
| BR-16.3 final, no edit/delete | §4.1, §4.3, §12 |
| BR-16.4 stars required, comment optional | §4.1, §5 |
| BR-16.5 visibility (rated + Admin only) | §4.2, §4.4, §12 |
| BR-16.6 CFG-28 "New member" | §4.2/§4.4 |
| BR-16.7 rater identity shown | §4.2 |
| BR-16.8 moderation hides comment only | §4.2, §5 |
| BR-16.9 report a comment | §4.5 |
| BR-16.10 no effect on matching | §1 |
| BR-16.11 never deleted; stars still count | §4.2, §4.3 |
| Status lifecycle §9.8 | §4.2 |
| CFG-13 / 14 / 27 / 28 | §2, §4.1, §10 |
| V-41 / V-42 | §4.1, §5, §12 |
| SC-16.1…SC-16.8 | §4, §5 |
| AC-16.1…AC-16.5 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §6.7 (W-7.1, W-7.2) | §4 |
| UI Plan §6.5 (connection entry) | §4.1 |
| UI Plan §6.2 (Me hub entry) | §2, §4.4 |
| UI Plan §7 patterns | §4.5, §5 |
| UI Plan §8 delivery | §11 |
