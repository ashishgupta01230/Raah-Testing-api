# M9 — Notifications — UI Plan

| | |
|---|---|
| **Module** | M9 — Notifications (spec `docs/spec/09_M9_Notifications.md`, functional spec Section 18) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5, module design §6.9 (W-9.1, W-9.2), patterns §7 (especially §7.6), delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20). Admin screens desktop first |
| **Companion** | API plan: `09_M9_Notifications_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M9 gives every Member and Admin a quiet, trustworthy notification surface:

1. A **bell** in the shell with an unread badge.
2. **SCR-18.1 Notification centre** — a list with read/unread, category filters and "Mark all as read".
3. **SCR-18.2 Notification preferences** — the two optional email switches only.
4. **Emails** that use one calm layout and never carry private content.

UI jobs for this module:

- **Prompt, not noisy.** One line per event, one clear link, no repeated banners (BR-18.1, BR-18.5).
- **Private by construction.** No chat text, no contact details, no rejection or block reason appears in any notification or email (BR-18.2, BR-18.3, UP-4).
- **Neutral language.** A rejection is "Your request was not accepted." A block is never announced (UP-6, BR-17.2).
- **Optional means optional.** Only the leads digest and unread-chat email can be switched off; safety, verification, password, account and legal emails are visibly locked on (BR-18.4).
- **Every state designed** — loading, empty, filtered-empty, error, offline, already-read, archived (UP-10).

Out of scope for M9 screens (linked, owned elsewhere): the notification *content* of every event (M4–M8, M10–M13), the leads/requests/chat screens themselves, Admin dashboard composition (M14), and the legal re-acceptance screen (Foundation/M13).

---

## 2. Navigation and routes

The bell lives in the **shell**, not inside a module. M9 provides the bell, its unread badge and the list/preferences screens; the shell decides placement.

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Notification centre | `/notifications` | SCR-18.1 | Bell on phone; "View all" in the desktop bell panel; Home "notifications" link |
| Notification preferences | `/settings/notifications` | SCR-18.2 | Me hub row "Notification settings"; footer link in every email |
| Desktop bell panel | popover on any member screen | SCR-18.1 (compact) | Bell in the top bar |
| Admin notification centre | `/admin/notifications` | SCR-18.1 (admin) | Admin bell; Admin sidebar |
| Admin bell panel | popover on any Admin screen | SCR-18.1 (compact) | Admin top bar |

Navigation rules (UI Plan §4.1):

- **Phone:** the bell opens the full-screen `/notifications`. The 5-tab bottom bar is unchanged; M9 adds only the badge.
- **Tablet/desktop:** the bell opens a dropdown panel with the latest 5 notifications and **View all**; `/notifications` is the full page.
- **Admin:** a left sidebar item and the top-bar bell; `/admin/notifications`.

Global gates (owned by Foundation) still apply: Verify-email gate and Legal re-acceptance gate come before notification screens; Inactive/Blocked members cannot log in (BR-5.1, BR-22.16, BR-5.4).

---

## 3. Screen inventory and components

| Screen / element | Components used (UI Plan §3.6) |
|---|---|
| Shell bell + badge | C-1 icon button, C-12 badge, live region for count |
| Bell panel (desktop) | C-20 Notification item, C-25 Empty state, C-35 "View all" |
| Notification centre | C-20, C-11 Segmented/filter tabs, C-27 Filter bar, C-1, C-25, C-26 Skeleton, C-35 Show more, C-21 Banner, C-22 Toast |
| Notification item | C-20 (icon, one line, time, unread dot, link) |
| Notification preferences | C-6 Switch (on/off/disabled), C-33 Key-value/locked rows, C-12 chips, C-22 Toast, C-21 banner |
| Admin notification centre | C-20, C-28 pattern for dense list on desktop, C-27, C-11, C-25, C-26 |
| Email templates | Not app screens; designed as HTML email layouts (C-brand tokens only) |

Reuse the design system. If C-20 does not exist yet, build it here as a reusable **Notification item** and reuse it in the bell, centre and Admin list.

---

## 4. Screen designs

### 4.1 Shell bell and unread badge

```text
Phone top bar
┌────────────────────────────────────────┐
│  RAAH            [Saathi]        🔔 3   │
└────────────────────────────────────────┘

Desktop top bar
┌───────────────────────────────────────────────────────┐
│ RAAH        Search…              [Saathi]    🔔 (3)  ▾ │
└───────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Badge | Count of unread; show `9+` above 9. Text on the badge is required, not colour alone |
| No unread | No badge (no "0") |
| Update | Poll `GET /me/notifications/unread-count` every 60 s and on window focus; no websocket in Phase 1 |
| Click/tap | Phone → `/notifications`; desktop → dropdown panel (§4.2) |
| Accessibility | Icon button has `aria-label="Notifications, 3 unread"`; count changes announced politely, not on every poll if unchanged |
| Admin | Same bell, `/admin/notifications`, Admin-only data |

---

### 4.2 Desktop bell panel (compact, latest 5)

```text
                     ┌───────────────────────────────────┐
                     │ Notifications      Mark all read  │
                     ├───────────────────────────────────┤
                     │ ● Amit wants to connect with you. │
                     │   2 hours ago                     │
                     │ ○ Neha accepted your request.     │
                     │   Yesterday                       │
                     │ ○ Your request was not accepted.  │
                     │   09-Oct-2026                     │
                     ├───────────────────────────────────┤
                     │            View all               │
                     └───────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Content | Latest 5 non-archived items; unread dot + bold title |
| Actions | Item click marks read and opens the link; "Mark all read"; "View all" → `/notifications` |
| Empty | "You have no notifications." with a short line "New activity will appear here." |
| Error | "Could not load notifications." + Retry |
| Keyboard | Panel traps focus while open, closes on `Esc`, returns focus to the bell |
| Mobile | Not used; the bell navigates to the full screen |

---

### 4.3 Notification centre — `/notifications` (W-9.1)

```text
┌────────────────────────────────────────┐
│  ← Notifications    Mark all as read   │
│  [All][Leads][Requests][Chat][Safety]  │
│  [Account][Legal][System]   (scroll →) │
├────────────────────────────────────────┤
│  ● Amit wants to connect with you.     │
│    Requests · 2 hours ago              │
│                                        │
│  ○ Your request was not accepted.      │
│    Requests · Yesterday                │
│                                        │
│  ○ Neha accepted your request. You can │
│    now chat.                           │
│    Requests · 09-Oct-2026              │
│                                        │
│  ○ A new person matches your route.    │
│    Leads · 09-Oct-2026                 │
│                                        │
│  [            Show more            ]   │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Title | "Notifications"; the count of unread may appear as "Notifications (3)" for clarity |
| Filters | Category tabs/chips. Baseline from W-9.1 is All · Requests · Chat · Safety · Account; this plan adds **Leads**, **Legal** and **System** because the matrix produces those categories (see UD-UI-1). Horizontal scroll on narrow phones |
| Item | Icon by category, **one-line** title, optional second line, category label, relative time, unread dot; whole row is the tap target (≥ 44 px) |
| Unread | Dot + semibold title; read items normal weight. Never colour alone |
| Tap | Mark read (optimistic) then navigate to the link. If the target is gone or forbidden, show a calm inline note "This item is no longer available." and keep the user on the list |
| Mark all | "Mark all as read" for the current filter; toast "All notifications marked as read." |
| Paging | First page 20, then **Show more** (keyset). Do not use numbered pages |
| Sorting | Newest first; the UI never re-sorts |
| Archive | Items older than 90 days are not shown (archived, not deleted). No archive filter in Phase 1 |
| Empty | "You have no notifications." + "We will tell you here when something needs your attention." |
| Filtered empty | "No notifications in this category." + "Show all" |
| Loading | Skeleton rows (C-26), preserve row height |
| Error | Banner "Could not load notifications." + Retry; keep the filter selection |
| Offline | Slim banner "No connection. Changes will not be saved."; read actions disabled; the list may show cached items |
| Admin | Same layout at `/admin/notifications`, desktop-first dense list; no preferences |

**Category → icon and label (one set, reused everywhere):**

| Category | Label | Icon idea | Deep link |
|---|---|---|---|
| Leads | Leads | route/person | `/leads` |
| Requests | Requests | envelope/arrows | `/leads/requests` |
| Chat | Chat | chat bubble | `/chats/{connectionId}` |
| Safety | Safety | shield | `/me/reports` or `/me` |
| Account | Account | user | `/me` or `/profile` |
| Legal | Legal | document | `/accept-terms` |
| System | System | info | `/me` |
| AdminAlerts | Admin | alert | `/admin/reports` |

---

### 4.4 Notification item (C-20) states

| State | Design |
|---|---|
| Unread | Indigo dot, semibold title, full contrast |
| Read | No dot, normal weight, same contrast for text (do not grey out to unreadable) |
| Linked | Chevron or subtle affordance; the whole row is clickable |
| No link | Row not clickable; no chevron |
| Link unavailable | Tap shows "This item is no longer available."; item still marks read |
| Loading (marking read) | Keep the row; remove the dot optimistically; roll back on failure with a small toast |
| Archived | Not rendered in the centre |

No item ever shows an avatar of the other member, a preview of chat text, an email/mobile, or a reason for a rejection/block (BR-18.2, BR-18.3, UP-4).

---

### 4.5 Notification preferences — `/settings/notifications` (W-9.2)

```text
┌────────────────────────────────────────┐
│  ← Notification settings               │
│  Email                                 │
│  ┌──────────────────────────────────┐  │
│  │ New leads, one email a day at    │  │
│  │ 7:00 AM                     [on] │  │
│  ├──────────────────────────────────┤  │
│  │ Reminder for unread messages     │  │
│  │                             [on] │  │
│  ├──────────────────────────────────┤  │
│  │ Safety, verification, password   │  │
│  │ and account emails      [locked] │  │
│  └──────────────────────────────────┘  │
│  (i) In-app notifications are always on│
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Digest row | Label "New leads, one email a day at {time}" from CFG-31; switch on/off |
| Unread chat row | "Reminder for unread messages"; switch on/off |
| Always-on block | A locked row listing safety, verification, password, account and legal emails with a lock icon and "Always on" (BR-18.4, AC-18.3). Not a disabled switch that invites tapping; make it a key-value row |
| In-app note | "In-app notifications are always on." |
| Save | Auto-save on toggle with a toast "Notification settings updated."; if not possible, a sticky Save. Do not require a Save button for two switches if the API is fast (prefer auto-save) |
| Dirty/failed | On failure, revert the switch and show a banner "We could not save your setting. Try again." |
| Loading | Skeleton rows; switches disabled until loaded |
| No permission | Preferences are member-only; an Admin sees no preferences entry |
| Offline | Switches disabled with the offline banner |
| Admin | `/settings/notifications` is not shown in the Admin UI |

**Wording rationale.** The member must understand what can and cannot be switched off. Do not hide mandatory email categories behind a vague "some emails are always sent".

---

### 4.6 Admin notification centre — `/admin/notifications`

```text
┌────────────┬─────────────────────────────────────────────────────────────┐
│ RAAH Admin │  Notifications                              Mark all read    │
│ Dashboard  │  [All][Safety][Reports][System]                              │
│ Members ▸  │ ┌─────────────────────────────────────────────────────────┐ │
│ Safety ▸   │ │ ● MBR-000231 has reached 3 reports.     2 hours ago     │ │
│ Settings ▸ │ │ ○ A new safety report was submitted.    Yesterday       │ │
│ Reports    │ │ ○ A rating comment was reported.        09-Oct-2026     │ │
│            │ └─────────────────────────────────────────────────────────┘ │
│            │                                        [ Show more ]        │
└────────────┴─────────────────────────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Layout | Desktop-first dense list; on phone it collapses to the same stacked cards |
| Categories | All · Safety · Reports · System (Admin alerts map to these) |
| Items | One line, time, unread dot, link to the Admin screen (`/admin/reports`, `/admin/members/{id}`, `/admin/ratings`) |
| No preferences | Admin alerts are mandatory; do not show notification settings |
| Privacy | Admin notifications carry a member number, never member contact details or chat text |
| States | Same loading/empty/error/offline rules as §4.3 |

---

### 4.7 Email templates

All emails share the layout from UI Plan §6.9:

- One column, max 560 px, white surface, RAAH mark.
- One sentence naming the person (or the count for the digest).
- **One** primary button.
- Footer: "Notification settings" and Terms / Privacy links.
- No chat text, no mobile/email of another member, no route details beyond the link, no rejection/block reason (BR-18.2).

| Email | Subject | Body | Button |
|---|---|---|---|
| Request received | "{Name} wants to connect with you" | One sentence; no route detail beyond the link | "View request" |
| Request expiring | "Your request from {Name} expires in {Days} days" | One sentence | "View request" |
| Request accepted | "{Name} accepted your request" | "You can now chat. Decide time and place together." | "Open chat" |
| Unread message | "You have a new message from {Name}" | One sentence; never the message text | "Open chat" |
| Leads digest | "You have {Count} new leads" | Count only | "View leads" |
| Safety action | "Your account status was updated" | Neutral; check your account | "Open my account" |
| Route deactivated by Admin | "Your route was turned off by Admin" | One sentence | "View route" |
| Account reactivated | "Your account was reactivated" | One sentence | "Log in" |
| Legal update | "We updated our Terms of Use" | Effective date | "Review and accept" |
| Admin report alert | "{MemberNumber} has reached {Count} reports" | One sentence | "Open report queue" |
| Admin new report digest | "{Count} new safety reports" | Count only | "Open report queue" |

Verification, email-change, password-reset emails reuse this layout but are owned by Foundation/M1.

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton rows (C-26), preserved height; no bare spinner |
| Empty | "You have no notifications." with one calm sentence |
| Filtered empty | "No notifications in this category." + "Show all" |
| Error | Banner with plain message, keep filters, one Retry |
| Success | Toast for mark-all and preference save; no celebratory motion |
| Marking read | Optimistic dot removal; rollback + toast on failure |
| Disabled | Preferences while loading/offline; read actions offline |
| No permission | A member cannot open the Admin list; show the generic no-access page |
| Offline | Slim top banner "No connection. Changes will not be saved."; reads still viewable if cached |
| Session expired | Foundation dialog; return to login (BR-5.6) |
| Archived | Hidden from list and bell; not an error state |
| Link target gone | Inline "This item is no longer available." and stay on the list |
| Verify gate | Unverified member never reaches the app incl. notifications (BR-5.1) |
| Legal gate | Re-acceptance screen comes first (BR-22.16) |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Bell → full-screen `/notifications`; category chips horizontally scroll; rows full width; sticky "Mark all as read" is not needed (top action) |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | list max ~720 px centred; bell panel becomes a dropdown; Admin list uses a table-like grid |
| Desktop 1024+ | Bell dropdown with latest 5; `/notifications` in a centred 720 px column; Admin list dense with the Admin sidebar |
| Wide 1280+ | Member content max 1120 px; do not stretch the list beyond reading width |

- The shell owns the bottom bar / sidebar switch; M9 does not implement the shell (UI Plan §4.1).
- Respect safe areas for the sticky top bar and bell.
- At 200% text zoom the list must not scroll horizontally.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1; unread dot is not the only indicator (semibold + dot) |
| Focus | Visible 2 px Indigo focus ring; bell panel/dialog traps focus and returns it |
| Labels | Bell has an accessible label with the unread count; switches have visible labels in the row |
| Targets | ≥ 44 × 44 px; rows are full-width targets |
| Screen readers | Unread count changes use a polite live region; new list items are announced once; "Mark all as read" result is announced; category chips expose selected state |
| Colour | Category icons always have text labels |
| Keyboard | Bell, panel, filters, rows, Show more and switches all operate by keyboard |
| Motion | Honour "reduce motion"; no decorative animation |
| Email | Semantic headings, meaningful alt text for the mark, ≥ 4.5:1 contrast, single-column at 200% zoom |
| Language | `lang="en"`; dates `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M9)

| Situation | Copy |
|---|---|
| Page title | "Notifications" |
| Admin page title | "Notifications" (Admin context) |
| Bell label | "Notifications, {n} unread" |
| Mark all | "Mark all as read" |
| Mark all done | "All notifications marked as read." |
| Empty | "You have no notifications." |
| Empty helper | "We will tell you here when something needs your attention." |
| Filtered empty | "No notifications in this category." |
| Error | "Could not load notifications." |
| Link gone | "This item is no longer available." |
| New request | "{Name} wants to connect with you." |
| Request expiring | "The request from {Name} expires in {Days} days." |
| Accepted | "{Name} accepted your request. You can now chat." |
| Rejected | "Your request was not accepted." |
| Expired | "A request expired because there was no reply." |
| Chat | "{Name} sent you a message." |
| Disconnected | "{Name} disconnected from you." |
| Rated | "{Name} rated you." |
| Report sent | "Your report was sent. We will review it." |
| Report decided | "Your report has been reviewed." |
| Safety action | "Your account status was updated. Check your account." |
| Route off by Admin | "Your route was turned off by Admin." |
| Photo removed | "Your profile photo was removed by Admin." |
| Legal update | "Our Terms of Use have been updated." |
| City decision | "Your city suggestion was added." / "Your city suggestion was declined." |
| Admin threshold | "{MemberNumber} has reached {Count} reports." |
| Admin new report | "A new safety report was submitted." |
| Admin rating | "A rating comment was reported." |
| Preferences title | "Notification settings" |
| Digest row | "New leads, one email a day at {time}" |
| Unread row | "Reminder for unread messages" |
| Always on | "Safety, verification, password and account emails — Always on" |
| In-app note | "In-app notifications are always on." |
| Preference saved | "Notification settings updated." |
| Preference error | "We could not save your setting. Try again." |
| Offline | "No connection. Changes will not be saved." |
| Email footer | "Notification settings · Terms of Use · Privacy Policy" |

**Fixed wording from the spec:** rejection is neutral and reasonless (BR-18.3); a block is never notified (BR-17.2); the role-aware request notice uses BR-18.11 exactly.

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Shell bell count | `GET /me/notifications/unread-count` (or `/admin/notifications/unread-count`) |
| Open notification centre | `GET /me/notifications?filter=&cursor=&limit=` |
| Open Admin centre | `GET /admin/notifications?filter=&cursor=&limit=` |
| Tap an item | `POST /me/notifications/{id}/read` then navigate to `linkUrl` |
| Mark all | `POST /me/notifications/read-all` |
| Load preferences | `GET /me/notification-preferences` |
| Toggle a switch | `PUT /me/notification-preferences` |
| Show more | `GET /me/notifications?...&cursor={nextCursor}` |

Client rules:

- Always send the auth token; never pass a member id for self routes.
- On `401` → Foundation session-expiry handling; on `403` → generic no-access.
- Mark read is optimistic: remove the dot immediately, roll back on error.
- After login and on window focus, refresh the unread count; poll every 60 s.
- Never render `linkUrl` unless it is an internal relative path; the server already allow-lists it, the client must not turn it into an external URL.
- After marking read, the shell badge updates from the returned `unreadCount`.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M4–M8 event producers | Notifications are empty; the empty state is correct and calm |
| M5 requests | Deep link `/leads/requests` may show empty; no fake items |
| M6 chat | Deep link `/chats/{id}` may be unavailable; show "This item is no longer available." |
| M7 ratings | Rating notifications can point to `/me/ratings`; if unavailable, land on Me |
| M8 reports | `/me/reports` from M8 should exist at build #9 |
| M10–M13 Admin actions | Safety/route/photo/legal/city notifications appear once those modules publish |
| CFG-31 digest time | Show the default "7:00 AM" from the API options; do not hard-code |
| Email transport | Templates can be reviewed in the UI/storybook before sending is wired |
| Shell bell | M9 provides the component and count API; the shell places it |

---

## 11. Delivery plan (M9 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Build/reuse C-20 Notification item, C-11 category chips, C-6 switch, C-26 skeleton, C-25 empty state | UI Plan §3 |
| 2 | Shell bell + unread badge + 60 s polling + polite live region | API 2 / 8 |
| 3 | Desktop bell dropdown panel (latest 5, Mark all, View all) | API 1, 3, 4 |
| 4 | Notification centre: filters, keyset Show more, optimistic read, link handling | API 1, 3, 4 |
| 5 | Empty / filtered-empty / error / offline / link-gone states | API 1 |
| 6 | Preferences screen with auto-save, locked always-on rows, revert on failure | API 5, 6 |
| 7 | Admin notification centre and Admin bell | API 7–10 |
| 8 | Email templates (all keys in §4.7) with the shared layout | API templates |
| 9 | Responsive pass: phone full screen, desktop panel, Admin density | — |
| 10 | Accessibility pass: labels, focus, live regions, switches, 200% zoom | — |
| 11 | QA: privacy regression (no chat/contact/reason anywhere), dedup, archive boundary, slow network | API running |
| 12 | Wire deep links as M5–M8 screens land; final copy review | M5–M8 |

Suggested route files (framework-agnostic naming): `notifications/index`, `settings/notifications`, `admin/notifications`, plus shell components `NotificationBell`, `NotificationItem`, `NotificationList`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-18.1 | No notification or email UI shows chat text, a mobile number, another member's email, or a rejection/block reason |
| AC-18.2 | The leads digest email shows a count only, never one email per lead |
| AC-18.3 | Preferences show safety/verification/password/account/legal as locked "Always on" |
| AC-18.4 | The expiry reminder copy uses the CFG-29 day count |
| BR-18.1 | No screen implies a notification is required to see leads, requests, chats or reports |
| BR-18.3 | Rejection copy is exactly "Your request was not accepted."; no block notification exists |
| BR-18.4 | Only the two optional switches are interactive |
| BR-18.8 | Read and unread items are distinguishable without colour alone; archived items are absent |
| BR-18.11 | Request notices use the Saathi/Raahi wording correctly |
| UP-4 | Items show a name and link only, never private data |
| UP-6 | Rejection, expiry and safety copy is neutral and blameless |
| UP-10 | Loading, empty, filtered-empty, error, success, disabled, offline and link-gone states are implemented |
| UP-11 | Contrast, focus, labels, targets, keyboard and screen-reader announcements pass |
| — | Bell count updates after read/mark-all and never shows "0" |
| — | Dates use `09-Oct-2026` and relative time ("2 hours ago") only for recent items |
| — | Email layout is single column, one button, footer with settings and legal links |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| UD-UI-1 | W-9.1 shows 5 filters (All/Requests/Chat/Safety/Account) but the matrix also produces Leads, Legal and System | Add Leads, Legal and System as extra scrollable chips; keep the original five first |
| UD-UI-2 | Relative time vs absolute date | Relative for items under 7 days ("2 hours ago", "Yesterday"), `09-Oct-2026` after that |
| UD-UI-3 | Should a bell panel item open a detail view? | No detail view; tap marks read and opens the linked screen |
| UD-UI-4 | Keep archived items reachable? | No archive filter in Phase 1; archived items are hidden |
| UD-UI-5 | Auto-save vs Save button for two switches | Auto-save with revert-on-error |
| UD-UI-6 | Where does the member see the digest time? | Inline in the digest row using CFG-31, no separate settings page |
| UD-UI-7 | Should Admin have notification preferences? | No; Admin alerts are mandatory |
| UD-UI-8 | Notification grouping (e.g. several new leads into one row)? | No grouping in Phase 1; each in-app lead notification is one row, email is the digest |
| UD-UI-9 | Show a category icon or the other member's avatar? | Category icon only (privacy + consistency); no avatars in notifications |
| UD-UI-10 | Landing when a deep link is no longer accessible | Stay on the list with "This item is no longer available." |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-18.1 Notification centre | §2, §4.2, §4.3, §4.4, §4.6 |
| SCR-18.2 Notification preferences | §2, §4.5 |
| Notification matrix | §4.3 category table, §4.7 email table, §9 |
| BR-18.1 informational | §1, §12 |
| BR-18.2 no private content | §4.3, §4.4, §4.7, §12 |
| BR-18.3 neutral rejection / no block notice | §8, §12 |
| BR-18.4 optional emails | §4.5, §12 |
| BR-18.5 digest | §4.7, §9 |
| BR-18.6 expiry reminder | §4.7, §8 |
| BR-18.7 unread chat email | §4.7 |
| BR-18.8 read/filter/archive | §4.3, §4.4, §5 |
| BR-18.9 channels / SMS later | §1, §10 |
| BR-18.10 skip inactive/blocked | §5 (no UI for skipped notifications) |
| BR-18.11 role-aware wording | §8 |
| AC-18.1…18.4 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §6.9 W-9.1/W-9.2 | §4.3, §4.5, §4.7 |
| UI Plan §7.6 messages | §4.1, §4.3, §5 |
| UI Plan §8 delivery | §11 |
