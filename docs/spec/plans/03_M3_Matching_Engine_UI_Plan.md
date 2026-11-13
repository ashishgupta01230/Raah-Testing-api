# M3 — Matching Engine — UI Surface Plan

| | |
|---|---|
| **Module** | M3 — Matching Engine (spec `docs/spec/03_M3_Matching_Engine.md`) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1 (UP-2 explain every match), design system §3, navigation §4, flows §5, module designs §6.4 / §6.9 / §6.11 / §6.13 / §6.14, patterns §7, delivery §8 |
| **Platform** | Responsive web app; **phone first** (design at 360 px); Admin surfaces desktop first |
| **Companion** | API plan: `03_M3_Matching_Engine_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and nature of this plan

M3 has **no screens of its own** (spec §Screens). It is a system process whose output is shown by other modules. This document therefore defines the **UI surfaces and display contracts** the engine feeds, so the same match result is presented consistently everywhere.

| Surface | Owner module | What M3 supplies |
|---|---|---|
| Leads list — match badges per leg | M4 (SCR-13.1) | `MatchType` per leg, shared points, days, time gap |
| Lead detail — "Why this match" and route comparison | M4 (SCR-13.2) | shared stretch, common days, both times, gap, route versions |
| Dashboard — "New leads" summary | M14 (SCR-23.1) | counts by leg |
| Notifications | M9 (SCR-18.1) | a match exists for a member |
| Admin Matching Health | M11 (SCR-20.5) | no-lead routes, match mix, unmatched stops |
| Admin Configuration — matching rows + Recalculate | M13 (SCR-22.1) | config values; run summary |
| Admin Test Match | M13 (SCR-22.3) | step-by-step dry-run result |

UI jobs for M3:

- **Explain every match** (UP-2). Every lead shows which points, which days and what time gap produced it, in plain words.
- **No black box.** The same result is rendered identically in the lead list, the lead detail and Admin monitoring.
- **No blame, no scores.** Members see the reason, not internal weights or a numeric score; gender and rating never appear as a reason.
- **Every state is designed** (UP-10): recalculating, no leads, filtered-empty, engine error, withdrawn leads.

Out of scope: the lead lifecycle controls (Connect, Dismiss, Block, Report) belong to M4/M8; this plan only covers how the match **result** is displayed.

---

## 2. Where matches appear (navigation)

Matching results live under the **Leads** tab (bottom nav on phone, left sidebar on desktop) and are read-only on Admin surfaces.

| Surface | Route | Screen | M3 content |
|---|---|---|---|
| Leads list | `/leads` | SCR-13.1 / W-4.1 | Match badge per leg, shared stretch preview, days, both times, gap |
| Lead detail | `/leads/[id]` | SCR-13.2 / W-4.3 | Full "Why this match", side-by-side route comparison |
| Dismissed leads | `/leads/dismissed` | SCR-13.3 / W-4.5 | Same cards, muted |
| Member dashboard | `/` | SCR-23.1 / W-14.1 | New-lead counts split by Going/Return |
| Admin Matching Health | `/admin/matching-health` | SCR-20.5 / W-11.3 | Aggregated match quality |
| Admin Configuration | `/admin/config` | SCR-22.1 / W-13.1 | Matching settings + Recalculate leads |
| Admin Test Match | `/admin/config/test-match` | SCR-22.3 / W-13.2 | Dry-run of the algorithm |

No new route is added by M3.

---

## 3. Components used

| Surface | Components (UI Plan §3.6) |
|---|---|
| Lead card | C-16 Lead card, C-15 Route line (compact), C-12 Badge/chip (match type), C-13 Avatar, C-14 Rating stars, C-1 Button |
| Lead detail | C-15 Route line (compare), C-33 Key-value list, C-12, C-13, C-14, C-23 Dialog (safety actions, owned by M4/M8) |
| Dashboard block | C-29 Stat tile/summary row, C-12 |
| Matching Health | C-29 Stats, C-28 Data table, C-12, C-35 pager |
| Test Match | C-9 Point input (display-only sample), C-7 Day selector, C-8 Time field, C-4 Select (cities), C-1 Button, C-21 Banner, C-12 result badge |
| Configuration | C-2 number fields, C-6 switch, C-21/ C-23 for Recalculate dialog, C-22 toast |

Components are **shared with M4/M11/M13**, not forked for matching. Build once in the owning module and reuse.

---

## 4. Surface designs

### 4.1 Lead card — match badges (part of SCR-13.1, owned by M4)

```text
┌────────────────────────────────────────┐
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
└────────────────────────────────────────┘
```

| Aspect | Design | Rule |
|---|---|---|
| Heading | Role-aware: a Saathi sees "Raahis for you", a Raahi sees "Saathis for you" | UI Plan W-4.1 |
| Order | Exactly the API ranking; the UI **never re-sorts** | spec §Ranking |
| Per-leg badge | `Full Route` green with route icon, `Useful Segment` sky with part icon, `No match` muted | C-12; never colour-only |
| Shared stretch | Compact C-15 with shared points highlighted (◉) and a text list | UP-2, no colour-only |
| Days | The **common** days only (e.g. "Tue–Thu") | spec Step 3 |
| Time | Both departure times and the gap ("7:50 AM · 10 min apart") | spec Step 4 |
| Leg absent on one side | Show "No match" only if the leg exists but failed; if a leg is absent, omit that leg block | spec Step 2 |
| Privacy | Public profile view only — no email/mobile | BR-4.9 |

Card states are owned by M4 (New/Requested/Connected/Cool-off); M3 only populates the match block. It must render correctly in each state (e.g., a Connected card still shows why it matched).

### 4.2 Lead detail — "Why this match" (part of SCR-13.2, owned by M4)

```text
┌────────── Why this match ─────────────┐
│ Going   [Full Route]                  │
│ Shared: Vinay Nagar, Govindpuri,      │
│         Kalkaji, Nehru Place           │
│ Common days: Tue, Wed, Thu             │
│ Time: 7:50 AM and 8:00 AM,             │
│       10 minutes apart                 │
│ Return  [Full Route]                  │
│ Shared: Nehru Place, Kalkaji, ...      │
│ Common days: Tue, Wed, Thu             │
│ Time: 6:20 PM and 6:30 PM, 10 min apart│
└───────────────────────────────────────┘
```

Then the side-by-side "Compare routes" block:

```text
 Going   You            Neha
 ① Vinay Nagar ◉        ① Vinay Nagar ◉
 ② Badarpur Metro       ② Govindpuri ◉
 ③ Govindpuri ◉         ③ Kalkaji ◉
 ⑤ Nehru Place ◉        ④ Nehru Place ◉
```

| Aspect | Design | Rule |
|---|---|---|
| Content source | Built from stored result only (route versions at calculation), never recomputed in the browser | BR-12.1, Principle 14 |
| Wording | Saathi: "This Raahi covers your whole route" / "…shares part of your route". Raahi: "Your route covers this Saathi's whole route" / "You share part of this Saathi's route" | spec §13.4 |
| Shared points | Highlighted on the route line **and** listed as text; positions/renumbering match the stored order | no colour-only |
| Days | List common days; do not show all days both travel | Step 3 |
| Times | Show both times and the gap in minutes ("10 minutes apart"); for a Useful Segment say "same time" when gap is 0 | Step 4 |
| Missing leg | If only one leg matches, show that leg and a muted "No match" line for the other; never imply a second match | spec Step 9 |
| Route changed since | If a route version changed after calculation, show a small note "Route changed since this match was calculated." and let M4 render the current route comparison | BR-11.13, M4 |
| Gender/rating | Never shown as a reason; rating may appear only as profile info, never inside "Why this match" | BR-12.11 |
| Scroll | The block is collapsible on phone; expanded by default on desktop | — |

### 4.3 Member dashboard — new leads block (part of SCR-23.1)

```text
│  New leads                  See all >  │
│  5 new (3 Going, 2 Return)             │
│  (Av) Neha S. Full Route · Full Route  │
│  (Av) Rohit M. Useful Segment          │
```

| Aspect | Design |
|---|---|
| Counts | Split by leg matched (Going / Return), from the engine's data |
| Preview | Top two leads by rank, match type per leg, role-aware heading |
| Empty | "No relevant person found yet. We will tell you when someone matches." (SC-12.11) — never an error |
| Freshness | Refresh quietly; a "Updated just now" caption is enough, no spinners over content |

### 4.4 Admin Matching Health — SCR-20.5 (owned by M11)

```text
│ Matching Health        City [All ▼]       Period [7 days ▼]          │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐     │
│ │ 30       │ │ 4.2      │ │ 22%      │ │ 58%      │ │ 9.5 h    │     │
│ │No-lead   │ │Leads per │ │Lead →    │ │Request → │ │Avg time  │     │
│ │routes    │ │route     │ │request   │ │accept    │ │to answer │     │
│ └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘     │
│ Match mix per leg   Going  ▇▇▇▇▇▇░░ Full 62% · Segment 38%           │
│                     Return ▇▇▇▇░░░░ Full 48% · Segment 52%           │
│ Supply and demand   Saathi routes 540 · Raahi routes 310             │
│ Most used stop names                 │ Unmatched stop names          │
│  Badarpur Metro  (312)               │  Kalka Ji Temple Gate (3)     │
│   variants: Bdarpur Metro, badarpoor │  Sai Mandir Gate (4)          │
```

| Aspect | Design | Rule |
|---|---|---|
| Tiles | Number + label + definition on tap; every tile drills to the list behind it | BR-23.3 |
| Match mix bars | Bars carry numbers as text; Full vs Segment labelled | no colour-only |
| Stop names | Most-used and unmatched lists use the **same similarity rule** as matching, with variants shown | BR-20.5 |
| Privacy | Counts and stop names only; **no chat text**, no contact details | BR-20.4 |
| Empty/loading | Skeleton tiles; "No data for this period" | UP-10 |
| Period/city filters | Change the aggregate, not stored leads | — |

### 4.5 Admin Configuration — matching rows + Recalculate (SCR-22.1, owned by M13)

M3 defines which rows and help text appear; M13 builds the screen.

| Row | UI |
|---|---|
| Departure time tolerance (min) | Number, range and default shown (CFG-1) |
| Text match threshold (%) | Number, range and default (CFG-22) |
| Minimum shared points | Number, range and default (CFG-4) |
| Return leg mandatory | Switch (CFG-19, owned by M2 but read by matching) |

**Recalculate leads** action:

| State | UI |
|---|---|
| Idle | Secondary button top-right of the Configuration screen |
| Confirm | C-23 dialog: "This re-runs matching for every active route. Some leads may be withdrawn and some created. Pending requests and connections are not changed." |
| Running | Button shows progress; a run row appears with status Running; disable a second run (`RECALC_ALREADY_RUNNING`) |
| Done | Toast + run summary (routes evaluated, leads created/updated/withdrawn) with a link to the run detail |
| Failed | Error banner with the run summary and Retry |

### 4.6 Admin Test Match — SCR-22.3 (owned by M13)

```text
│ Test Match            Uses current settings. No member data is used. │
│ ┌──── Sample Saathi leg ─────┐ ┌──── Sample Raahi leg ─────────────┐  │
│ │ Cities: Delhi → Delhi      │ │ Cities: Delhi → Delhi             │  │
│ │ Points: Vinay Nagar,       │ │ Points: Vinay Nagar,              │  │
│ │ Badarpur Metro, Nehru Place│ │ Bdarpur Metro                     │  │
│ │ Days: Mon–Fri  Time 8:00 AM│ │ Days: Mon–Fri  Time 8:05 AM       │  │
│ └────────────────────────────┘ └───────────────────────────────────┘  │
│ [ Run test ]                                                         │
│ ✓ Cities overlap   ✓ Common days   ✓ Time gap 5 min (limit 30)      │
│ Point pairs: Vinay Nagar = Vinay Nagar 100% · Badarpur Metro =       │
│ Bdarpur Metro 93%                                                    │
│ ✓ Shared points 2 (minimum 2)   Result: [ Useful Segment ]           │
```

| Aspect | Design | Rule |
|---|---|---|
| Inputs | Two sample legs with cities, points (≤ 5), days, time; no member search | no member data |
| Result | A step-by-step checklist mirroring the API's `eligibilityChecks` and per-leg checks | explainable |
| Failures | The failing step is marked with a cross and the reason ("Time gap 40 min, limit 30") | plain language |
| Point pairs | Show each paired point and its similarity %; show unpaired points muted | Admin diagnostics |
| Result badge | Full Route / Useful Segment / No match | C-12 |
| States | Idle, running, success, invalid input, error | UP-10 |
| Reset | "Clear" empties the sample legs; no data is saved | — |

---

## 5. State matrix (all surfaces)

| State | Required design |
|---|---|
| Loading | Skeleton cards/tiles (C-26); do not collapse layout |
| No leads (member) | "No relevant person found yet. We will tell you when someone matches. You can add a Return leg or check your points and times." + Edit route / Add Return leg (SC-12.11, SC-13.8) |
| Filters hide all | "No leads match these filters." + Clear filters |
| No active route | "Activate your route to see leads." + button |
| Recalculating | Keep existing leads visible; a quiet caption "Updating matches…" — never blank the list |
| Engine/job failure | Admin sees the failed run with reason; members see existing leads (stale) and no scary error |
| Withdrawn lead | Disappears from the active list; remains visible on the lead/connection history where applicable (M4/M5) |
| Route Under Review | No new matching for that route (spec Step 1); member sees the route banner (M2), not a lead error |
| Blocked pair | Leads removed in both directions immediately; no announcement to the other person (BR-12.9) |
| Offline | Read-only cached leads; no writes (M4 controls) |
| Config changed | Existing leads unchanged; note only on Admin screens that changes apply to future matching (BR-6.3) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Lead cards full width; "Why this match" collapsible; compare-routes stacks You/Neha vertically with aligned order numbers |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | Leads master-detail begins; detail pane shows card + why + compare |
| Desktop 1024+ | Leads list left, detail right; Admin tables/health tiles full width (Admin desktop first) |
| Wide 1280+ | Member content max 1120 px; Admin tables scroll inside their container |

- Route comparison must align point order numbers across both columns and remain readable when names wrap.
- Admin bars/tiles reflow to two columns on tablet, one on phone.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Explainability | Shared points are shown as a **text list** and highlighted; match type is a word + icon, never colour alone |
| Contrast | Match badges use the checked palette (Full Route green, Useful Segment sky); text ≥ 4.5:1 |
| Screen readers | Announce per leg: "Going, Full Route, shared points 4, common days Tuesday to Thursday, 10 minutes apart"; the compare block announces point position and whether it is shared |
| Targets | ≥ 44 × 44 px on cards and Admin controls; 8 px apart |
| Keyboard | Lead cards, detail, Test Match inputs and Recalculate dialog fully keyboard-operable; dialogs trap focus |
| Motion | Honour "reduce motion"; no animated match reveal |
| Zoom | Readable at 200%; Admin tables scroll inside their container |

---

## 8. Microcopy (M3 surfaces)

| Situation | Copy |
|---|---|
| Section heading | "Why this match" |
| Full Route (Saathi view) | "This Raahi covers your whole route." |
| Useful Segment (Saathi view) | "This Raahi shares part of your route." |
| Full Route (Raahi view) | "Your route covers this Saathi's whole route." |
| Useful Segment (Raahi view) | "You share part of this Saathi's route." |
| No match leg | "No match" |
| Gap | "<n> minutes apart" / "Same time" |
| One-leg lead | Show only the matched leg; other leg "No match" |
| Route changed note | "Route changed since this match was calculated." |
| No leads | "No relevant person found yet. We will tell you when someone matches." |
| Filtered empty | "No leads match these filters." |
| Recalculating (quiet) | "Updating matches…" |
| Dashboard heading | Saathi: "Raahis for you"; Raahi: "Saathis for you" |
| Test Match title note | "Uses current settings. No member data is used." |
| Recalculate dialog | "This re-runs matching for every active route. Some leads may be withdrawn and some created. Pending requests and connections are not changed." |
| Settings note | "A change applies to matching done after you save it." (BR-6.3) |

Style: sentence case, active voice, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI need | API (from M3/M4/M13 plans) |
|---|---|
| Lead list match block | M4 read API over `raah_Lead`/leg results (M3 data) |
| Lead detail "Why this match" | M4 read API including point pairs, days, gap |
| Dashboard lead counts | M14 dashboard API (reads lead counts) |
| Matching Health tiles/lists | `GET /admin/matching-health`, `GET /admin/matching-health/unmatched-stops` |
| Configuration rows | M13 config read/write (CFG-1, CFG-4, CFG-22, CFG-19) |
| Recalculate leads | `POST /admin/matching/recalculate`, `GET /admin/matching/runs/{runId}` |
| Test Match | `POST /admin/config/test-match` |

Rules for the client:

- Never recompute match results client-side; always render the stored result.
- Show the route-version changed note from a server flag; do not infer.
- On a reconnecting list, keep current cards and refresh in place.
- Admin endpoints are role-gated; on `403` show the generic no-access state.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M4 leads | M3 itself has no screen; lead cards are delivered with M4 |
| M9 notifications | Copy is specified here; delivery is M9 |
| M11/M13 Admin screens | M3 supplies the data contracts; screens delivered there |
| M8 blocks | Blocked-pair removal is reflected through lead disappearance; no UI work in M3 |
| M2 route versions | "Route changed" note relies on the version ids stored by M3; until M4 renders it, omit |
| Rating | Rating is never a match reason; wherever shown it is profile info only |

---

## 11. Delivery plan (M3 UI surfaces)

| Step | Work | Depends on |
|---|---|---|
| 1 | Agree the lead match-block contract with M4 (fields, wording, collapsed/expanded) | M3 API |
| 2 | Build/reuse C-12 match badges and C-15 highlighted route line | UI Plan §3.6 |
| 3 | Lead card match block (per leg: badge, shared stretch, days, times, gap) | M4 list API |
| 4 | Lead detail "Why this match" + compare routes | M4 detail API |
| 5 | Role-aware wording and one-leg/absent-leg variants | M4 |
| 6 | Dashboard new-lead counts block | M14 |
| 7 | Admin Matching Health tiles, bars, stop lists, drill-downs | M11 + M3 query |
| 8 | Admin Configuration matching rows + Recalculate dialog/summary | M13 + M3 jobs |
| 9 | Admin Test Match form + step-by-step result | M13 + M3 service |
| 10 | Accessibility and responsive pass (compare alignment, bars, tiles) | — |
| 11 | QA against the spec worked example (Neha/Rohit/Amit/Sunil/Pooja/Karan) | engine running |

Because M3 owns no screens, steps 3–4 ship **with M4**, steps 7–9 **with M11/M13**; this plan fixes their content and behaviour.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-12.1 | A 30-minute gap shows a lead; 31 minutes shows none |
| AC-12.2 | Opposite-direction legs show no lead; the reason is not shown as an error |
| AC-12.3 | A single shared point produces no lead |
| AC-12.4 | Full Route is shown even when some Saathi intermediates are missing |
| AC-12.5 | Fuzzy points pair and are shown in the shared list; differing numbers do not pair |
| AC-12.6 | A two-leg lead appears above a one-leg lead; the UI never re-sorts |
| AC-12.7 | Draft/Inactive/Invalid/Under Review routes produce no leads |
| AC-12.8 | Same-role routes produce no leads |
| AC-12.9 | A lead appears when cities overlap; no lead when they do not |
| UP-2 | Every lead shows points, days, times and gap in plain words; "Why this match" matches the stored result |
| UP-4 | No email/mobile in any match surface |
| UP-10 | Loading, no-lead, filtered-empty, recalculating, withdrawn and Admin error states are designed |
| — | Match badges carry text + icon; shared points are listed as text |
| — | Test Match never shows member data and mirrors the algorithm's steps |
| — | Recalculate dialog states withdrawn/created leads and that requests/connections are unchanged |
| — | Matching Health uses counts only, with drill-downs and text-labelled bars |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| — | Expand "Why this match" by default on phone? | Collapsed in the list, expanded in detail |
| — | Show similarity % to members? | No — similarity is Admin-only (Test Match/health); members see shared points only |
| — | Show the "route changed" note in the list or only detail? | Detail only; the list shows current data |
| — | Show withdrawn leads to members? | No; history is Admin/M4 filtering only |
| — | Matching Health default period | 7 days (W-11.3) |
| — | Recalculate progress style | Run row with status + counters; no blocking modal |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| No screens (system process) | §1 |
| Steps 3–8 visible explanation | §4.1, §4.2 |
| Ranking (no UI re-sort) | §4.1, §12 |
| Trigger effects (withdraw/revive) | §5 states |
| BR-12.1 explainable/deterministic | §4.2, §4.6 |
| BR-12.9 blocked pair | §5 |
| BR-12.10 ≤ 1 min freshness | §4.3, §5 recalculating |
| BR-12.11 gender not a rule | §4.2, §8 |
| BR-12.13 city overlap | §4.1 ("No match" rendering) |
| BR-6.3 settings future-only | §4.5, §8 |
| BR-20.4/20.5 Admin health | §4.4 |
| AC-12.1…12.9 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan W-4.1/W-4.3/W-9.1/W-11.3/W-13.1/W-13.2/W-14.1 | §4 |
| UI Plan §7 state patterns | §5, §6 |
| UI Plan §8 delivery | §11 |
