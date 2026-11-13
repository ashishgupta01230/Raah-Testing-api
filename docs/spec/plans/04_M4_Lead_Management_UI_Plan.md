# M4 — Lead Management — UI Plan

| | |
|---|---|
| **Module** | M4 — Lead Management (spec `docs/spec/04_M4_Lead_Management.md`) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5, module design §6.4 (leads) and §6.5 (connect hand-off), patterns §7, delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20) |
| **Device/network** | Mid-range Android + Chrome, Safari on iPhone; slow-network tolerant (skeletons, small images) |
| **Companion** | API plan: `04_M4_Lead_Management_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M4 gives the member a clear, honest list of people found for them and a safe way to act:

1. See **ranked leads** with a plain-language match explanation per leg (SCR-13.1).
2. Open a **lead detail** with the other member's limited profile, route comparison and "Why this match" (SCR-13.2).
3. **Filter** by Leg and Gender without changing the order.
4. **Dismiss / Undo** and view dismissed leads (SCR-13.3).
5. **Connect** from a lead through the fixed-message confirmation (SCR-14.1).

UI jobs for this module:

- **Explain every match** (UP-2): each card and detail shows match type per leg, shared stretch, days and gap in plain words; highlighted shared points always have a text list, never colour alone.
- **A lead is a suggestion, not a booking** (Principle 7): wording never implies a confirmed journey.
- **Show the minimum about people** (UP-4): only the public view — never email or mobile.
- **Nothing happens by surprise** (UP-5): Connect shows the exact fixed message; states (sent, received, cool-off, limit) are explicit.
- **Calm by default** (UP-6): empty, filtered-empty and withdrawn states are helpful and blame-free.
- **Every state is designed** (UP-10): loading, empty, filtered, restricted, cool-off, limit, offline, error.

Out of scope for M4 screens (owned elsewhere, but cross-linked): Requests cards and accept/reject (M5), chat (M6), rating (M7), Block/Report flows (M8), profile (M1), route (M2). The **Leads · Requests · Dismissed** segment bar is a shared shell; M4 owns the **Leads** and **Dismissed** segments and the connect hand-off, not the Requests content.

---

## 2. Navigation and routes

M4 lives in the **Leads** tab (bottom nav on phone, left sidebar on desktop). The top bar keeps the RAAH mark, role chip (Saathi/Raahi) and notification bell (UI Plan §4.1). The tab badge shows new leads plus pending received requests (shell-owned).

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| My Leads | `/leads` | SCR-13.1 | Leads tab; Home "new leads" |
| Lead detail | `/leads/[id]` | SCR-13.2 | Lead card tap |
| Dismissed leads | `/leads/dismissed` | SCR-13.3 | Leads tab → Dismissed segment |
| Connect confirmation | bottom sheet on `/leads/[id]` and card | SCR-14.1 | Connect / card Connect |
| Lead history | tab/section inside `/leads/[id]` | BR-13.10 | Detail → History |
| Requests | `/connections` (segments) | M5 | Leads tab → Requests segment |

Gates that affect M4 (owned by Foundation/M2):

- **Verify-email gate:** Unverified members cannot reach leads (BR-5.1/BR-10.2); they see Verify email only.
- **No Active route:** leads cannot exist; show "Activate your route to see leads." with a link to Route (M2).
- **Legal re-acceptance gate:** shown before any M4 screen when a required version is pending (BR-22.16).
- **Restricted banner:** amber banner on Leads/Requests: "You cannot send new requests until *date*." Browsing and history still work (BR-21.6).
- **Inactive/Blocked:** cannot log in (BR-5.4).

---

## 3. Screen inventory and components

| Screen / part | Components used (UI Plan §3.6) |
|---|---|
| Leads list | C-11 Segmented control, C-27 Filter bar, C-16 Lead card, C-12 Badges/chips, C-13 Avatar, C-14 Rating stars, C-15 Route line, C-21 Banner, C-25 Empty state, C-26 Skeleton, C-35 Show more |
| Lead detail | C-13, C-12, C-14, C-15 Route line, C-33 Key-value list, C-11 tabs (Overview/History), C-34 History timeline, C-1 Button, C-21 Banner, C-24 Bottom sheet, C-25 Empty state, C-26 Skeleton |
| Dismissed | C-16 muted variant, C-1 Button (Undo), C-25, C-26, C-35 |
| Connect sheet | C-24 Bottom sheet, C-1 Button, C-21 Banner, C-22 Toast |
| Filters | C-27 Filter bar, C-4 Select/segmented, C-11 Segmented control |

No new reusable component is required; M4 reuses `C-16 Lead card`, `C-15 Route line`, `C-27 Filter bar`, `C-34 History timeline`. If `C-16`/`C-15` are built in M2/M3, reuse them.

---

## 4. Screen designs

### 4.1 Leads list — `/leads` (W-4.1)

```text
┌────────────────────────────────────────┐
│  Leads      Requests (2)   Dismissed   │
│  Raahis for you            5 found     │
│  Leg: [Any ▼]  Gender: [Any ▼]  Clear  │
├────────────────────────────────────────┤
│  (Av) Neha S.             ★ 4.6 (12)   │
│  Raahi · Car · Email verified ✓        │
│  Going   [Full Route]                  │
│   Vinay Nagar ◉ Govindpuri ◉ Kalkaji ◉ │
│   Nehru Place ◉                        │
│   Tue–Thu · 7:50 AM · 10 min apart     │
│  Return  [Full Route]                  │
│   Nehru Place ◉ ... Vinay Nagar ◉      │
│   Tue–Thu · 6:20 PM · 10 min apart     │
│  [ Connect ]            Not interested │
├────────────────────────────────────────┤
│  (Av) Rohit M.            ★ New member │
│  Raahi · Bike                          │
│  Going   [Useful Segment]              │
│   Kalkaji ◉ ─ Nehru Place ◉            │
│   Mon–Fri · 8:00 AM · same time        │
│  Return  No match                      │
│  [ Connect ]            Not interested │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Heading | Role-aware: a Saathi sees "Raahis for you", a Raahi sees "Saathis for you" (API `meta.roleHeading`) |
| Order | Exactly the API ranking. **The UI never re-sorts** (BR-13.3) |
| Filter bar | Leg `Any/Going/Return` when CFG-20; Gender `Any/Male/Female` when CFG-12. "Clear" appears only when a filter is active; count updates ("2 of 5 shown") |
| Match lines | One block per leg: badge (Full Route green, Useful Segment sky, or "No match"/"Not entered"), shared stretch on a compact route line (C-15) **plus** a text list of shared points, days, both times and the gap |
| Wording | Use the API's viewer-worded `matchLabel`; never invent your own |
| Cities | Show the matched member's route cities ("Delhi → Noida") near the name or first leg |
| Card actions | Primary **Connect** (or the state chip), secondary **Not interested** (dismiss) |
| Privacy | Only the public card: no email, no mobile (AC-13.1) |
| Paging | First CFG-7 items, then **Show more** (keyset). Show more appends; it does not reset scroll |
| States | Skeleton cards while loading; per-card states below; offline banner disables writes |

**Lead card states (C-16)**

| State | Card shows |
|---|---|
| New | Small indigo "New" dot |
| Viewed | No dot; normal card |
| Request sent · Pending | Primary button replaced by "Request sent · Pending" (disabled) |
| Request received | "Request received · Review" → opens M5 Requests / Accept (BR-14.4) |
| Connected | "Connected · Open chat" → M6 |
| Cool-off | Disabled button "You can request again in 5 days" (CFG-17) |
| Restricted | Connect disabled; amber banner at top of list (not each card) |
| Pending limit | Disabled button; tip "You have too many pending requests. Wait for answers or withdraw one." (CFG-6) |
| Withdrawn | Card removed from the list on next refresh |

### 4.2 Leads — desktop master-detail (W-4.2)

```text
┌────────────────────┬────────────────────────────────────────────┐
│ Leads Requests Dis.│  Neha S.   ★ 4.6 (12)   Raahi · Car ✓       │
│ Leg[Any▼] Gen[Any▼]│  ┌──────── Why this match ───────────────┐  │
│ ┌────────────────┐ │  │ Going: Full Route. Shared: 4 points   │  │
│ │▶ Neha S.       │ │  │ Common days Tue–Thu. 10 minutes apart │  │
│ │  Full · Full   │ │  └───────────────────────────────────────┘  │
│ ├────────────────┤ │  Your route          Neha's route           │
│ │  Rohit M.      │ │  ① Vinay Nagar ◉     ① Vinay Nagar ◉        │
│ │  Segment · —   │ │  ...                 ...                    │
│ └────────────────┘ │  [ Connect ]  Dismiss   ⋯                    │
└────────────────────┴────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Breakpoint | Two columns from 1024 px (UI Plan §4.1) |
| Left | Filtered, ranked list; selected row uses the Indigo 50 tint; keyboard ↑/↓ moves selection and loads the detail |
| Right | Detail of the selected lead; opening selects it and marks Viewed |
| Phone | Detail is a full screen with a back arrow (4.3) |

### 4.3 Lead detail — `/leads/[id]` (W-4.3)

```text
┌────────────────────────────────────────┐
│  ← Neha S.                         ⋯   │
│  (Avatar 96)  Neha S.                  │
│  Raahi · Car · Email verified ✓        │
│  ★ 4.6 (12)    Member since Aug 2026   │
│  ┌────────── Why this match ─────────┐ │
│  │ Going   Full Route                │ │
│  │ Shared: Vinay Nagar, Govindpuri,  │ │
│  │ Kalkaji, Nehru Place              │ │
│  │ Common days: Tue, Wed, Thu        │ │
│  │ Time: 7:50 AM and 8:00 AM,        │ │
│  │ 10 minutes apart                  │ │
│  │ Return  Full Route ...            │ │
│  └───────────────────────────────────┘ │
│  Compare routes                        │
│   Going   You            Neha          │
│   ① Vinay Nagar ◉        ① Vinay N. ◉  │
│   ② Badarpur Metro       ② Govindp. ◉  │
│   ③ Govindpuri ◉         ③ Kalkaji ◉   │
│   ⑤ Nehru Place ◉        ④ Nehru P. ◉  │
│  ───────────────────────────────────── │
│  [          Connect          ]         │
│  Not interested                        │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Profile block | Public card only: avatar, name, gender, role, vehicle (Raahi), verified badge, rating (or "New member"), member since. No email/mobile (AC-13.1) |
| Why this match | From the stored explanation: per leg the match type, shared points as typed, common days, both times and the gap; a readable `summaryLine`. Shared points marked ◉ **and** listed in words (no colour-only meaning) |
| Route comparison | Two columns (You / them) for Going and Return, points as typed, shared points highlighted; when a leg is absent show "Return not entered". On phone the two columns stack or scroll horizontally within their container |
| Actions | Primary **Connect** (or its state); **Not interested** (dismiss); **⋯** menu holds **Block** and **Report** (M8) and **History** |
| Viewed | Opening details marks the lead Viewed (BR-13.4) |
| Route changed | If the matched member changed their route, the live route is shown; the explanation reflects the latest calculation (BR-13.11) |
| States | Skeleton; withdrawn → "This person is no longer available." with Back; error banner + Retry |
| Privacy | Never render email/mobile even if a payload accidentally contains them; the client maps an allow-list |

### 4.4 Connect confirmation — bottom sheet (W-4.4 / SCR-14.1)

```text
┌────────────────────────────────────────┐
│  Send request to Neha?                 │
│  This message will be sent with it:    │
│  ┌──────────────────────────────────┐  │
│  │ [lock] Hello, I want to connect with  │  │
│  │    you                            │  │
│  └──────────────────────────────────┘  │
│  She decides whether to accept. You can│
│  chat once she does.                   │
│  [      Send request      ]  [ Cancel ]│
└────────────────────────────────────────┘
```

| State | UI |
|---|---|
| Default | Read-only message box from CFG-25 (via preflight); no editable field (D-23) |
| Sending | Primary spinner; both buttons disabled |
| Sent | Sheet closes; toast "Request sent."; card becomes "Request sent · Pending" |
| Pending limit | Inline warning; primary disabled; "You have too many pending requests…" |
| Cool-off | Inline warning with days left; primary disabled |
| Already received a request | "Neha has already sent you a request. Accept it?" with an **Accept** button (BR-14.4) → M5 |
| Not available any more | "This person is no longer available."; close + refresh list |
| Restricted | Inline "You cannot send new requests until *date*."; primary disabled |
| Offline | Primary disabled with the offline notice |

The sheet is M5's SCR-14.1 but is opened from M4; M5 owns accept/reject and the request record.

### 4.5 Dismissed leads — `/leads/dismissed` (W-4.5)

- Same cards in a muted style, with **Undo** instead of Connect/dismiss.
- Empty state: "Leads you dismiss appear here." with "Go to Leads".
- A dismissed lead stays here even when matching recalculates (A-33 / AC-13.4); Undo returns it to the main list as Viewed.

### 4.6 Filters

| Filter | Options | Rules |
|---|---|---|
| Leg | Any / Going / Return | Shown only when CFG-20 = Yes; filters to leads with that leg matched |
| Gender | Any / Male / Female | Shown only when CFG-12 = Yes; filters by the matched member's gender |
| Clear | — | Resets both to Any; appears only when a filter is active |

- Filters never change status, order or data (AC-13.2/13.3). The scroll position resets to the top on filter change and the list reloads.
- The count text reflects the filtered set: "2 of 5 shown".
- Filtered-empty: "No leads match these filters." with **Clear filters**.

### 4.7 History tab

- Inside the detail, an **Overview / History** tab pair (or a section at the bottom).
- Timeline (C-34): created, updated, viewed, dismissed, requested, request closed, withdrawn/revived, with date and actor ("You" / "System").
- Read-only; no controls. Members see only their own lead's history (BR-8.5).

### 4.8 Empty and special states

| Situation | Message and action |
|---|---|
| No leads at all | "No relevant person found yet. We will tell you when someone matches. You can add a Return leg or check your points and times." with **Edit route** and **Add Return leg** (M2) |
| Filters hide everything | "No leads match these filters." with **Clear filters** |
| No Active route | "Activate your route to see leads." with a button to Route (M2) |
| Restricted member | Amber banner: "You cannot send new requests until *date*." List and detail still viewable |
| Withdrawn lead opened | "This person is no longer available." with **Back to leads** |
| Offline | Slim banner "No connection. Changes will not be saved."; Connect/Dismiss/Undo disabled |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton cards (C-26), never a bare spinner; preserve card height |
| Empty | Different copy for no leads, filtered-empty, no route, dismissed-empty (4.8) |
| Error | Banner with plain message and one **Retry** |
| Success | Toast (C-22, 4 s, polite live region) for dismiss/undo/request sent |
| Saving | Disable only the submitting control; prevent double submit; in-flight sheet stays open |
| Disabled | Explain inline: cool-off days, pending limit, restricted date, already connected |
| No permission | Generic 403; foreign lead shows the not-available state, never another member's data |
| Offline | Top banner; disable writes (Connect, Dismiss, Undo) |
| Session expired | Foundation dialog; unsaved filter/view state restored (BR-5.6) |
| Verify gate / legal gate | Foundation; M4 not reachable until cleared |
| Real-time | The list is a live view; refresh on focus/visibility and after returns from M5/M6. No polling storm |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Single column; list → full-screen detail; Connect sheet as bottom sheet (C-24); sticky primary on detail |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | List max ~720 px centred; detail full screen; consider master-detail preview |
| Desktop 1024+ | Master-detail (W-4.2); list left, detail right; sheets become centred dialogs (C-24); table-like density for the list |
| Wide 1280+ | Member content max 1120 px; do not stretch route columns |

- The shell switches bottom tabs to a left sidebar at tablet/desktop (UI Plan §4.1); M4 does not build the shell.
- Keep safe-area padding for the sticky action and bottom nav.
- At 200% text zoom the layout must not scroll horizontally; route comparisons scroll inside their container.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring; return focus after the connect sheet closes |
| Labels | Visible labels on filter selects; the read-only request message has a label |
| Targets | ≥ 44 × 44 px, 8 px apart (Connect cards, filter chips, ⋯ menu) |
| Colour | Match type and shared points always have text; ◉ is accompanied by a written list |
| Screen readers | Announce "Lead opened", "Dismissed", "Request sent"; the route line announces point order ("Point 2 of 5"); filters announce the new count |
| Keyboard | List selection, filters, detail tabs, ⋯ menu, connect sheet and history all keyboard-operable; dialogs trap focus |
| Motion | Honour "reduce motion"; no decorative animation |
| Language | `lang="en"`; dates `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M4)

| Situation | Copy |
|---|---|
| Heading (Saathi) | "Raahis for you" |
| Heading (Raahi) | "Saathis for you" |
| Count | "5 found" · "2 of 5 shown" |
| Match labels | From the API (viewer-worded, §5.5 of the API plan) |
| Shared stretch label | "Shared points: …" |
| Time label | "8:00 AM and 8:05 AM, 5 minutes apart" |
| No leads | "No relevant person found yet. We will tell you when someone matches. You can add a Return leg or check your points and times." |
| Filtered empty | "No leads match these filters." |
| No route | "Activate your route to see leads." |
| Dismissed empty | "Leads you dismiss appear here." |
| Dismiss action | "Not interested" |
| Undo | "Undo" |
| Dismissed toast | "Lead dismissed." |
| Undone toast | "Lead restored." |
| Request sent | "Request sent." |
| Pending | "Request sent · Pending" |
| Received | "Request received · Review" |
| Connected | "Connected · Open chat" |
| Cool-off | "You can request again in 5 days." |
| Pending limit | "You have too many pending requests. Wait for answers or withdraw one." |
| Restricted | "You cannot send new requests until *date*." |
| Withdrawn | "This person is no longer available." |
| Connect sheet | "Send request to Neha? This message will be sent with it:" · "She decides whether to accept. You can chat once she does." |
| Already requested by them | "Neha has already sent you a request. Accept it?" |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load Leads list (filters, page) | `GET /leads?leg=&gender=&limit=&cursor=` |
| Show more | `GET /leads?…&cursor=<nextCursor>` |
| Open lead detail (marks Viewed) | `GET /leads/{leadId}` |
| Load history tab | `GET /leads/{leadId}/history` |
| Dismiss | `POST /leads/{leadId}/dismiss` |
| Undo dismissal | `POST /leads/{leadId}/undo-dismiss` |
| Load dismissed | `GET /leads/dismissed?limit=&cursor=` |
| Open connect sheet | `GET /leads/{leadId}/connect-preflight` |
| Send request | `POST /leads/{leadId}/connect` |
| Accept an incoming request | M5 API (link) |
| Open chat | M6 API (link) |
| Block / Report | M8 API (link) |

Rules for the client:

- Always send the auth cookie/token; derive the member on the server; never send a member id.
- Never re-sort the list; trust the API order.
- On `404 LEAD_NOT_FOUND` show "This lead is not available." and refresh the list.
- On `409 LEAD_WITHDRAWN` remove the lead from the list and show "This person is no longer available."
- On `409 CONNECT_*` map to the exact copy in §8; do not silently retry.
- After a successful mutation, update the card in place and, for connect, refresh `connection` state.
- On `429`/cooldown show the returned days.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M3 leads | Empty list with the helpful no-leads message; no fake data |
| M5 connect | Connect sheet shows "Connecting is not available yet." and is disabled (API `NOT_AVAILABLE`) |
| M5 Requests segment | Tab present but shows a neutral "Requests are not available yet." (or hide until M5) |
| M6 chat | "Connected · Open chat" is disabled with a note until chat exists |
| M7 ratings | Show "New member"; no stars/number until M7 |
| M8 Block/Report | The ⋯ menu hides Block/Report until M8 is available |
| M2 route | Route comparison falls back to the stored snapshot; empty state links to M2 |
| Design tokens | Use UI Plan §3 values; no hard-coded colours |

---

## 11. Delivery plan (M4 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Reuse/build C-16 Lead card, C-15 Route line, C-27 Filter bar, C-34 History timeline | UI Plan §3 |
| 2 | Leads tab route `/leads` with segments shell (Leads/Dismissed; Requests placeholder) | Shell |
| 3 | Leads list against `GET /leads` with skeleton/empty/filtered/error | API 1 |
| 4 | Filter bar (CFG-12/20) + count text + Show more keyset | API 1 |
| 5 | Lead detail with public card, Why this match, route comparison | API 3 |
| 6 | Dismiss/Undo + dismissed list | API 2, 4, 5 |
| 7 | Connect preflight sheet + all disabled states + request sent | API 6, 7 |
| 8 | History tab timeline | API 8 |
| 9 | Desktop master-detail | — |
| 10 | Accessibility pass (labels, focus, SR announcements, ◉ text list) | — |
| 11 | QA: states, slow network, 200% zoom, privacy regression | API running |
| 12 | Enable real connect when M5 lands; hide placeholders | M5/M6/M8 |

Suggested route files: `leads/index`, `leads/[id]`, `leads/dismissed`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-13.1 | No lead card or detail shows email or mobile; only the public card fields render |
| AC-13.2 | Leg = Return shows only Return-matching leads, with unchanged order; Any restores all |
| AC-13.3 | Gender filter shows only that gender (when CFG-12 = Yes) |
| AC-13.4 | A dismissed lead stays in Dismissed after a matching recalculation; Undo returns it |
| AC-13.5 | Every detail shows shared points, common days and the time gap in words |
| AC-13.6 | With CFG-7 = 20 and 35 leads, 20 show first with **Show more** |
| BR-13.3 | The UI never re-sorts; order matches the API |
| BR-13.4 | Opening the detail marks the lead Viewed without a flash of stale status |
| BR-13.6 | Connect is disabled with the correct reason for cool-off, limit, restricted, connected and received states |
| UP-2 | Match type, shared stretch, days and gap are visible on card and detail |
| UP-4 | Only public member data is shown |
| UP-5 | The connect sheet shows the exact fixed message read-only |
| UP-10 | Loading, empty, filtered, withdrawn, restricted, offline and error states are implemented |
| UP-11 | Contrast, focus, labels, targets, keyboard and SR announcements pass |
| — | Match type and shared points never rely on colour alone |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| — | Does the Dismissed segment live in the same tab bar as Leads/Requests? | Yes, shared shell segment |
| — | Should the list auto-refresh while open? | Refresh on focus/visibility only; no polling storm |
| — | Show the matched member's cities on the card? | Yes, near the name ("Delhi → Noida") |
| — | Route comparison on phone: stacked or horizontal scroll? | Stacked per leg; horizontal scroll inside wide tables only |
| — | Where does History live? | Tab/section inside the detail (Overview / History) |
| — | Is the Requests segment visible before M5? | Show a neutral placeholder, then enable with M5 |
| — | Should the connect sheet be pre-fetched on card tap? | Fetch preflight when the sheet opens; cache briefly |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-13.1 My Leads | §2, §4.1 |
| SCR-13.2 Lead Detail | §4.3 |
| SCR-13.3 Dismissed | §4.5 |
| SCR-14.1 Connect confirm | §4.4 |
| Lead row contents | §4.1 |
| Lead detail blocks | §4.3 |
| Filters (D-10, D-16) | §4.6 |
| BR-13.1 no manual leads | §4.8 (no create UI) |
| BR-13.2 own leads + limited profile | §4.1, §4.3, §12 |
| BR-13.3 ranking + CFG-7 + Show more | §4.1 |
| BR-13.4 Viewed on open | §4.3 |
| BR-13.5 Dismiss/Undo | §4.5 |
| BR-13.6 connect states | §4.4, §4.1 card states |
| BR-13.7 Requested/reopen | §4.1 card states |
| BR-13.8 withdrawn hidden | §4.8 |
| BR-13.9 revive same lead | §4.5 (stays dismissed / revived) |
| BR-13.10 History tab | §4.7 |
| BR-13.11 live view | §4.3 |
| BR-13.12 empty state | §4.8 |
| AC-13.1…13.6 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §6.4/6.5 (W-4.1…W-4.5) | §4 |
| UI Plan §7 state patterns | §5, §6 |
| UI Plan §8 delivery | §11 |
