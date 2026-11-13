# RAAH — Phase 1 Module Execution Plan

> Derived from `docs/requirements/RAAH_Functional_Specification.md`. This folder splits the specification into **one file per module**, ordered by build dependency so each module can be built and tested in sequence.

## Serial Execution Order

Build strictly in this order. A module may reference only modules **above** it in the list.

| # | File | Module | Part | Depends on |
|---|---|---|---|---|
| 00 | `00_Execution_Plan.md` | Foundation (shared) | A | — |
| 01 | `01_M1_Member_Profile.md` | M1 — Member Profile | B | Foundation (registration, auth, statuses, config, audit) |
| 02 | `02_M2_Route_Management.md` | M2 — Route Management | B | M1, Foundation |
| 03 | `03_M3_Matching_Engine.md` | M3 — Matching Engine | C | M2 |
| 04 | `04_M4_Lead_Management.md` | M4 — Lead Management | C | M3 |
| 05 | `05_M5_Connection_Management.md` | M5 — Connection Management | C | M4 |
| 06 | `06_M6_InApp_Chat.md` | M6 — In-App Chat | C | M5 |
| 07 | `07_M7_Rating_Feedback.md` | M7 — Rating & Feedback | C | M5 |
| 08 | `08_M8_Safety_Block_Report.md` | M8 — Safety: Block & Report | C | M4, M5 |
| 09 | `09_M9_Notifications.md` | M9 — Notifications | C | M4, M5, M6, M7, M8 |
| 10 | `10_M10_Member_Management.md` | M10 — Member Management & Verification | D | M1, M2 |
| 11 | `11_M11_Route_Lead_Monitoring.md` | M11 — Route & Lead Monitoring | D | M2, M3, M4, M5 |
| 12 | `12_M12_Safety_Reports_Blocked.md` | M12 — Safety, Reports & Blocked Users | D | M8, M10 |
| 13 | `13_M13_Matching_Config_Settings.md` | M13 — Matching Config & Platform Settings | D | Foundation config, M3 |
| 14 | `14_M14_Dashboards.md` | M14 — Dashboards | E | All above |
| 15 | `15_M15_Reports_Catalogue.md` | M15 — Reports Catalogue | E | All above |

## Build Phases

- **Phase 0 — Foundation.** Configuration settings, record numbering, audit trail, master statuses, registration/login/session, role & permission matrix. Everything below depends on these.
- **Phase 1 — Member setup.** M1 (profile), M2 (route). No leads yet.
- **Phase 2 — Core loop.** M3 (match) → M4 (lead) → M5 (connect) → M6 (chat), M7 (rate), M8 (block/report), M9 (notify). This is the product loop.
- **Phase 3 — Admin.** M10 (members), M11 (monitoring), M12 (safety), M13 (configuration).
- **Phase 4 — Output.** M14 (dashboards), M15 (reports).

## Foundation Reference (shared, referenced by every module)

These are the cross-cutting items every module file assumes already exist. Full text is in the source spec; the essentials are reproduced here so a module file can be read on its own.

### Configuration Settings (CFG-1 … CFG-32)

| ID | Setting | Default | Allowed range (M13) |
|---|---|---|---|
| CFG-1 | Departure time tolerance (minutes) | 30 | 0–120 |
| CFG-2 | Maximum points per leg (incl. Start/End) | 5 | 3–10 |
| CFG-3 | Minimum points per leg | 2 | fixed at 2 |
| CFG-4 | Minimum shared points for a Useful Segment | 2 | 2–CFG-2 |
| CFG-5 | Pending request expiry (days) | 30 | 1–365 |
| CFG-6 | Maximum pending outgoing requests per member | 10 | 1–100 |
| CFG-7 | Maximum leads shown per list | 20 | 5–100 |
| CFG-8 | Session idle timeout (minutes) | 30 | 5–240 |
| CFG-9 | Maximum failed login attempts | 5 | 3–10 |
| CFG-10 | Account lock duration (minutes) | 15 | 1–1440 |
| CFG-11 | Verification / reset link validity (hours) | 24 | 1–168 |
| CFG-12 | Gender filter enabled | Yes | Yes/No |
| CFG-13 | Rating enabled | Yes | Yes/No |
| CFG-14 | Rating comment moderation | Yes | Yes/No |
| CFG-15 | Reports against one member that trigger an alert | 3 | 1–20 |
| CFG-16 | Notification channels | In-app + Email | In-app always on |
| CFG-17 | Cool-off after a rejected request (days) | 7 | 0–90 |
| CFG-18 | Days of week counted as travel days | Mon–Sun | any subset ≥1 |
| CFG-19 | Return leg mandatory | No | Yes/No |
| CFG-20 | Leg filter enabled | Yes | Yes/No |
| CFG-21 | Vehicle types offered to a Raahi | Bike, Car | — |
| CFG-22 | Text match threshold (%) | 70 | 50–100 |
| CFG-23 | Verification email resend cooldown (seconds) | 60 | 10–600 |
| CFG-24 | Maximum profile photo size (MB) | 5 | 1–10 |
| CFG-25 | Request message text | Hello, I want to connect with you | 1–150 chars, no links/10-digit runs |
| CFG-26 | Maximum chat message length (characters) | 1000 | 100–5000 |
| CFG-27 | Maximum rating comment length (characters) | 300 | 50–1000 |
| CFG-28 | Minimum ratings before average shows | 3 | 1–20 |
| CFG-29 | Request expiry reminder (days before) | 5 | 1–CFG-5−1 |
| CFG-30 | Unread chat email delay (hours) | 12 | 1–72 |
| CFG-31 | Daily digest send time | 07:00 | any time |
| CFG-32 | Safety report overdue after (days) | 3 | 1–30 |

Rules: BR-6.1 (every change logged), BR-6.2 (lowering CFG-2 does not invalidate existing routes), BR-6.3 (CFG-1/CFG-4 changes apply to new leads only).

### Record Numbering (Section 7)

| Record | Format | Example |
|---|---|---|
| Member | `MBR-{nnnnnn}` | MBR-000231 |
| Route | `RTE-{nnnnnn}` | RTE-000418 |
| Lead | `LED-{nnnnnnn}` | LED-0004521 |
| Connection Request / Connection | `CON-{nnnnnn}` | CON-000087 |
| Rating | `RAT-{nnnnnn}` | RAT-000042 |
| Safety Report | `SAF-{nnnnnn}` | SAF-000009 |

BR-7.1 generated on first save; BR-7.2 never reused; BR-7.3 request and connection share one CON number; BR-7.4 no yearly reset.

### Audit Trail & History (Section 8)

- BR-8.1 nothing is hard-deleted.
- BR-8.2 every create/edit/activate/deactivate/send/accept/reject/withdraw/block/report/rate/admin action logs record, action, user, role, timestamp, old & new value.
- BR-8.3 audit is read-only to everyone.
- BR-8.4 chat messages are not in the admin audit view (only chat-existed + access logs).
- BR-8.5 History tab on each record, chronological; members see only their own.
- BR-8.6 editing an Active route keeps earlier versions (lead provenance).

### Master Status Reference (Section 9)

**Member:** Unverified · Active · Inactive · Restricted · Temporarily Blocked · Permanently Blocked.

**Route:** Draft · Active · Inactive · Invalid · Under Review.

**Route type:** Saathi · Raahi. **Route leg:** Going (mandatory) · Return (optional by CFG-19).

**Lead match type:** Full Route · Useful Segment (per leg). **Legs matched:** Both / Going only / Return only.

**Lead status:** New · Viewed · Dismissed · Requested · Withdrawn by System.

**Connection status:** Pending · Accepted · Rejected · Withdrawn · Expired · Disconnected · Blocked.

**Safety report status:** Submitted · Under Review · Action Taken · Dismissed · Closed.

**Rating status:** Submitted · Hidden.

### Roles & Permission Matrix (Section 4)

- **Admin** — whole platform; monitors, verifies, handles safety, configures. Never assigns/matches people.
- **Member** — own data; as **Saathi** or **Raahi** (journey-context roles, not accounts).

Key rules: BR-4.1 (member sees only own data), BR-4.2 (server-side access control, denied attempts logged), BR-4.3 (one Active route, one role), BR-4.4 (cannot request self / blocked / restricted-from-receiving / blocked members), BR-4.5 (Admin read-only for member records; may deactivate/restrict/block for safety with reason), BR-4.6 (Admin never matches/assigns), BR-4.7 (Admin cannot read chat except inside an open safety report), BR-4.8 (Restricted keeps access minus capability; Blocked cannot log in), BR-4.9 (mobile number hidden from members, visible to Admin).

### Registration, Login, Session & Access Control (Section 5)

Screens: SCR-5.1 Register · SCR-5.2 Verify Email · SCR-5.3 Login · SCR-5.4 Forgot Password · SCR-5.5 Reset Password · SCR-5.6 Change Password · SCR-5.7 My Account · SCR-5.8 Admin Login (same screen).

Registration fields: Journey Role (Saathi/Raahi, first field), Vehicle (Raahi only: Bike/Car), Full Name (3–100), Email (unique), Password (min 8, letter+number), Confirm Password, Gender (Male/Female/Other), Mobile (optional, 10 digits, hidden from members), Accept Terms/Privacy/Safety (recorded with version+time).

Key rules: BR-5.1 (Unverified until email verified), BR-5.2 (link valid CFG-11 h, single-use), BR-5.3 (email unique; generic duplicate message), BR-5.4 (Inactive/Blocked cannot log in; generic message), BR-5.5 (lock after CFG-9 fails for CFG-10 min), BR-5.6 (session timeout CFG-8 min), BR-5.7 (login audit), BR-5.8 (hashed passwords, single-use reset), BR-5.9 (Admin status change applies next request; session ended if Inactive/Blocked), BR-5.10 (multi-device), BR-5.11 (role-based landing: Admin → Admin UI, Member → Member UI), BR-5.12 (Raahi needs Vehicle), BR-5.13 (legal texts public + managed by Admin + re-acceptance).

### Design Principles (Section 2.4) — binding

The 15 principles govern all modules. Most important for implementation: (1) connect, never book/assign; (2) route is an ordered point list; (3) full route first, then max segment; (4) members enter own route; (6) either side connects, no auto-connect; (9) chat only between connected members; (10) safety first-class; (13) nothing hard-deleted; (14) every lead explains itself; (15) thresholds configurable.

## Convention

Each module file uses the same sub-headings: Purpose · Who uses it · Dependencies · Screens · Field specification · Business rules (BR-x) · Status lifecycle · Scenarios (SC-x) · Acceptance criteria (AC-x) · Data impact · Reports produced · Cross-references (CFG, V, statuses). IDs (BR-x, SC-x, AC-x, SCR-x, CFG-x, V-x, RPT-x) are kept identical to the source spec for traceability.
