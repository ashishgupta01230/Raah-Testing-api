# M1 — Member Profile — UI Plan

| | |
|---|---|
| **Module** | M1 — Member Profile (spec `docs/spec/01_M1_Member_Profile.md`) |
| **UI reference** | `docs/requirements/RAAH_UI_Plan.md` — principles §1, design system §3, navigation §4, flows §5, module design §6.2, patterns §7, delivery §8 |
| **Platform** | One responsive web app; **phone first** (design at 360 px), then tablet and desktop (D-20) |
| **Device/network** | Mid-range Android + Chrome, Safari on iPhone; slow-network tolerant (skeletons, small images) |
| **Companion** | API plan: `01_M1_Member_Profile_API_Plan.md` |
| **Status** | Plan for design review |

---

## 1. Scope and goals

M1 gives the member a calm, private "Me" area to:

1. See their own profile and verification/rating status (SCR-10.1).
2. Keep name, photo, mobile, email and vehicle current (SCR-10.2).
3. Preview exactly what other members see (SCR-10.3).
4. Switch journey role with clear consequences (SCR-10.4).
5. End their own access safely (SCR-10.5).

UI jobs for this module:

- **Privacy is visible.** The member can see that email and mobile are never shown to others, and the preview proves it (UP-4).
- **Nothing is a surprise.** Role change and deactivation say exactly what happens to routes, leads, requests and chats before the member acts (UP-5).
- **Locked means explained.** Gender is read-only with a reason, not a mysterious disabled control (BR-10.3).
- **Every state is designed** — loading, empty photo, pending email, error, saving, offline (UP-10).

Out of scope for M1 screens (linked, owned elsewhere): ratings list (M7), blocked members and reports (M8), notification settings (M9), legal texts (M13), change password / My Account (Foundation), route creation (M2).

---

## 2. Navigation and routes

M1 lives under the **Me** tab (bottom nav on phone, left sidebar on desktop). The top bar still shows the RAAH mark, the journey-role chip (Saathi/Raahi) and the notification bell (UI Plan §4.1).

| Screen | Route | Spec | Entry points |
|---|---|---|---|
| Me hub | `/me` | Hub for SCR-10.1…10.5 | Me tab |
| My Profile | `/profile` | SCR-10.1 | Me hub name/avatar, "View profile" |
| Edit Profile | `/profile/edit` | SCR-10.2 | Me hub "Edit profile", My Profile "Edit" |
| How Others See Me | `/profile/preview` | SCR-10.3 | Me hub row |
| Change Journey Role | `/profile/role` | SCR-10.4 | Me hub row |
| Deactivate My Account | `/profile/deactivate` | SCR-10.5 | Me hub row |
| Email change sheet | modal on `/profile/edit` | SCR-10.2 | "Change" beside Email |
| Photo picker sheet | modal on `/profile/edit` | SCR-10.2 | "Change photo" |
| Email-change result | `/profile/email/verify?token=…` | Email link | Verification email |

**Me hub vs My Profile.** The hub is a summary + menu (per UI Plan W-2.1). SCR-10.1 "My Profile" is a dedicated full-fact view reachable from the hub, so every field in the spec's field table has a home. On phone the hub's first card is the summary; tapping the name/avatar opens `/profile`.

Global gates that affect M1 (owned by Foundation):

- **Verify-email gate:** an Unverified member sees only Verify email; Me is not reachable (BR-5.1).
- **Legal re-acceptance gate:** shown before any M1 screen when a required version is pending (BR-22.16).
- **Restricted banner:** a Restricted member may still use M1; the banner appears on Leads/Requests only (BR-21.6).
- **Inactive/Blocked:** cannot log in (BR-5.4).

---

## 3. Screen inventory and components

| Screen | Components used (UI Plan §3.6) |
|---|---|
| Me hub | C-13 Avatar, C-12 Badges/chips, C-14 Rating stars, C-1 Button, C-33 Key-value/list rows, C-21 Banner (setup prompt), C-22 Toast |
| My Profile | C-13, C-12, C-14, C-33 Key-value list, C-21, C-26 Skeleton |
| Edit Profile | C-2 Text field, C-31 Photo upload, C-4 Select, C-1 Button, C-12, C-22, C-23 Dialog, C-24 Bottom sheet |
| How Others See Me | C-13, C-12, C-14, C-15 Route line (when M2 exists), C-21 info banner, C-25 Empty state (no route) |
| Change Journey Role | C-1 (primary + destructive), C-21 warning banner, C-12 role chips, C-5 Role card (optional), C-23 Dialog |
| Deactivate | C-1 destructive, C-6 Checkbox, C-23 Dialog, C-21 warning banner |
| Email change | C-2, C-24 sheet/dialog, C-22 toast, C-21 info banner |
| Photo | C-31, C-24, C-22 |

No new reusable component is required for M1; reuse the design system. If `C-33` and `C-31` do not exist yet, build them here and reuse later.

---

## 4. Screen designs

### 4.1 Me hub — `/me` (W-2.1)

```text
┌────────────────────────────────────────┐
│  RAAH                         [Saathi] 🔔│
├────────────────────────────────────────┤
│  (Avatar 72)  Ashish Kumar              │
│  [Saathi]  Email verified ✓             │
│  ★ 4.6 (12 ratings)   Member since Oct  │
│  [ Edit profile ]                       │
├────────────────────────────────────────┤
│  How others see me                   > │
│  Change journey role                 > │
│  Ratings I received and gave         > │  ← M7
│  Blocked members                     > │  ← M8
│  My reports                          > │  ← M8
│  Notification settings               > │  ← M9
│  Terms, Privacy, Safety Guidelines   > │  ← M13
│  Account and password                > │  ← Foundation
│  Deactivate my account               > │  ← M1
│  Log out                               │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Profile card | Avatar (photo or initials on a tinted background), full name, role chip, verified badge, rating line, "Member since Mon YYYY" |
| Rating | `★ 4.6 (12 ratings)`; under CFG-28 ratings show `★ New member` with no number (BR-16.6 / CFG-28). M7 later supplies the real value; until then always "New member" |
| Setup prompt | If photo or mobile is missing (both optional), show a dismissible info banner: "Add a photo so members recognise you. It is optional." No hard nagging |
| Edit | Primary **Edit profile** button in the card |
| Rows | Full-width 44 px rows with chevrons; destructive "Deactivate my account" in red text; "Log out" is a plain row/button |
| Badges | Tab/notification counts come from a shared shell, not M1 |
| States | Skeleton card while loading; error banner with Retry; offline banner disables Edit/Change/Deactivate that write |
| Desktop | Two columns: profile summary card left, settings list right (UI Plan §6.2 responsive) |

**Note on scroll:** the menu can be long on small phones; keep 44 px targets and 8 px separation, and keep "Log out" last.

---

### 4.2 My Profile — `/profile` (SCR-10.1)

Purpose: the full own-profile fact view, including fields not on the hub (email, mobile, vehicle).

```text
┌────────────────────────────────────────┐
│  ← My profile                  [Edit]  │
│        (Avatar 96)                     │
│        Ashish Kumar                    │
│        [Saathi]  [Email verified ✓]    │
│  ────────────────────────────────────  │
│  Full name            Ashish Kumar     │
│  Gender               Male 🔒          │
│  Email                ashish@mail.com  │
│  Mobile               •••••• 4321 *    │
│  Journey role         Saathi           │
│  Vehicle              —                │
│  Verification         Email verified   │
│  Rating               ★ New member     │
│  Member since         Oct 2026         │
│  ────────────────────────────────────  │
│  (i) Email and mobile are never shown  │
│      to other members.                 │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Layout | C-33 key-value list, labels left, values right or below on narrow widths |
| Mobile | The member's own value is shown in full on this screen (it is their data); the **public** preview and other members never see it. Do not mask on the member's own view unless privacy mode is introduced later |
| Gender | Value plus a small lock icon; helper "Only support can correct this." (AC-10.2, BR-10.3) |
| Vehicle | Shown only when role is Raahi; hidden for Saathi (not "—") |
| Rating | Read-only stars with count, or "New member" under CFG-28 |
| Verification | Badge: "Email verified" green or "Unverified" amber with a "Resend link" action routed to the verify screen |
| Photo | 96 px circular avatar; initials fallback |
| Empty/partial | A missing photo shows initials; missing mobile/vehicle row is hidden (not blank) |
| States | Loading skeleton; 404/"Profile unavailable" with Retry; offline read-only |
| Primary action | **Edit** opens `/profile/edit` |

---

### 4.3 Edit Profile — `/profile/edit` (W-2.2 / SCR-10.2)

```text
┌────────────────────────────────────────┐
│  ← Edit profile                        │
│        (Photo 96)  [ Change photo ]    │
│        JPG or PNG, up to 5 MB          │
│                                        │
│  Full name                             │
│  [Ashish Kumar__________________]      │
│                                        │
│  Email                                 │
│  [ashish@mail.com_______]  [ Change ]  │
│  We sent a link? Show pending note.    │
│                                        │
│  Mobile number (optional)              │
│  [__________________________]          │
│  (i) Not shown to other members        │
│                                        │
│  Vehicle                      (Raahi)  │
│  (•) Bike   ( ) Car                    │
│                                        │
│  Gender                                │
│  Male 🔒   Only support can correct it │
│                                        │
│  [          Save changes          ]    │
└────────────────────────────────────────┘
```

| Field | Behaviour |
|---|---|
| Photo | C-31: pick, crop to a circle, preview; guidance "JPG or PNG, up to 5 MB" using the API's CFG-24 value. **Change photo** and **Remove photo** (secondary/tertiary). Upload immediately; success toast; failure keeps the old photo |
| Full name | C-2; required, 3–100 characters; inline error "Enter a name between 3 and 100 characters." Trim on blur |
| Email | Read-only text with **Change**. Opens the email-change sheet (4.8). Never editable inline |
| Mobile | C-2 numeric keyboard, max 10 digits; optional; helper "Not shown to other members"; inline error "Enter a 10-digit mobile number." |
| Vehicle | Segmented/radio Bike/Car; visible **only** for Raahi; mandatory for Raahi (BR-5.12); hidden for Saathi |
| Gender | Read-only value + lock + helper reason. No disabled-looking editable control that invites tapping; use a static row with a lock (BR-10.3, AC-10.2) |
| Save | Sticky bottom primary, full width on phone. Disabled only while saving, not while invalid; on submit focus jumps to the first error (UP-7) |
| Dirty state | Leaving with unsaved changes prompts "Discard changes?" (C-23) |
| Success | Toast "Profile updated." and return to `/profile`; me hub refreshes |
| States | Loading skeleton; saving (button spinner); field errors; server error banner; offline → Save disabled with notice |

**Why email is separate:** email changes need verification and the old email stays the login until the new one is verified (BR-10.4). Putting it inline would imply an instant change.

---

### 4.4 Email change sheet — part of SCR-10.2

```text
┌  Change email ─────────────────────── ┐
│  New email                            │
│  [______________________________]     │
│  (i) We will send a link to the new   │
│      address. Your login email stays  │
│      the same until you open it.      │
│  [        Send verification link  ]   │
└───────────────────────────────────────┘
```

| State | UI |
|---|---|
| Default | Empty field, primary "Send verification link" |
| Invalid | "Enter a valid email address." |
| Same email | "This is already your email address." |
| Already registered | "This email is already registered." (SC-10.5) |
| Sending | Button spinner, field disabled |
| Sent | Sheet closes; banner on Edit: "We sent a link to the new address. Your login email changes when you open it." (BR-10.4) |
| Cooldown | "You can request another email change in 0:45." (CFG-23) |
| Pending exists | Show the pending note with the masked address and the cooldown; do not allow a second request until the cooldown ends |
| Offline | Primary disabled |

**Verification result page** `/profile/email/verify?token=…` (opened from email, may be logged out):

| Outcome | Screen |
|---|---|
| Success | "Email updated. Your login email is now a•••@mail.com." Primary "Continue" (to Me or Log in if logged out) |
| Expired | "This link has expired." Primary "Request a new change" (to Edit → Email, if logged in) |
| Used | "This link has already been used." Primary "Continue" |
| Invalid | "This link is not valid." Primary "Go to My Profile" |

Keep the page calm, no blame, one clear action (UP-6).

---

### 4.5 How Others See Me — `/profile/preview` (W-2.3 / SCR-10.3)

```text
┌────────────────────────────────────────┐
│  ← How others see me                   │
│  This is what members see on a lead.   │
│  ┌──────────────────────────────────┐  │
│  │ (Avatar) Ashish K.   ★ New member│  │
│  │ Saathi · Male · Email verified ✓ │  │
│  │ Member since Oct 2026            │  │
│  │ Current route: Delhi → Noida     │  │
│  │  Going  Vinay Nagar ● … ● Nehru  │  │
│  │  Return Nehru Place ● … ● Vinay  │  │
│  └──────────────────────────────────┘  │
│  (i) Never shown: email, mobile        │
│      number, login history, reports.   │
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Content | Exactly the public allow-list: name, photo, gender, role, vehicle (Raahi), verified badge, rating, member since, current Active route (both legs: points, days, approximate departure time). No email or mobile anywhere (AC-10.1, SC-10.6) |
| Identity | Use the same public card visual as a lead, so the preview is honest |
| Route | When M2 exists: route line (C-15) with going/return. Until then: a neutral placeholder "No active route yet" with a link to Route (do **not** invent data) |
| Rating | "New member" until CFG-28 ratings, then stars + count |
| Privacy note | Fixed info banner under the card: "Email and mobile number are never shown to other members." |
| States | Loading skeleton; no route → placeholder; offline → static cached view if available |
| Actions | None except Back. No edit controls here — it is a preview |

---

### 4.6 Change Journey Role — `/profile/role` (W-2.4 / SCR-10.4)

Two-step in one screen, driven by the preflight API.

**Step A — consequences**

```text
┌────────────────────────────────────────┐
│  ← Change journey role                 │
│  You are a Saathi.                     │
│                                        │
│  ⚠ To become a Raahi, your current     │
│    route must be switched off first.   │
│    This withdraws your leads and       │
│    pending requests.                   │
│    Your existing connections and       │
│    chats stay.                         │
│                                        │
│  [ Deactivate route and continue ]     │
│  [ Cancel ]                            │
└────────────────────────────────────────┘
```

**Step B — vehicle (only when switching to Raahi and no vehicle is stored)**

```text
│  Choose your vehicle                   │
│  (•) Bike   ( ) Car                    │
│  [ Continue ]                          │
```

**Step C — done**

```text
│  You are now a Raahi.                  │
│  Create your Raahi route to start      │
│  finding Saathis.                      │
│  [ Create Raahi route ]   (M2)         │
│  [ Not now ]                           │
```

| Situation | UI behaviour |
|---|---|
| No Active route | Skip Step A warning; go straight to target role confirmation/vehicle. Show the same "connections and chats stay" note |
| Active route exists | Warning card with the exact consequences; primary destructive "Deactivate route and continue"; Cancel safe default |
| Confirmation | The destructive action may use C-23 confirm ("Turn off your route? Your leads and pending requests will be withdrawn."). No typed word |
| Raahi target without vehicle | Step B blocks Continue with inline error "Choose Bike or Car to continue as a Raahi." (AC-10.4) |
| Raahi → Saathi | Vehicle becomes hidden; no vehicle step; explain it is no longer shown to others |
| Route deactivation fails | Error banner "We could not switch off your route. Your role was not changed. Try again." + Retry |
| Role save fails after deactivation | "Your route is off, but your role could not be changed. Try again." + Retry (idempotent) |
| Offline | Continue disabled with notice |
| Success | Step C; audit happens server-side; the shell role chip updates immediately |

**Copy is fixed by BR-10.7:** leads and pending requests withdrawn, connections and chats kept (A-6). Do not use vague "some things may change" wording.

**Role timing.** The UI treats the role as switched as soon as the API call succeeds (Step C) and updates the shell role chip immediately. The member has no Active route between deactivation and creating the new M2 route, so nothing is matched in between. If product decides the role must instead change only when the new M2 route is activated, Step C copy becomes "Your route is off. Finish creating your Raahi route to complete the switch." — see API plan §8.11 and §17.

---

### 4.7 Deactivate My Account — `/profile/deactivate` (W-2.5 / SCR-10.5)

```text
┌────────────────────────────────────────┐
│  ← Deactivate my account               │
│  Deactivating your account:            │
│   • turns off your active route        │
│   • withdraws pending requests         │
│   • closes your connections and chats  │
│   • logs you out everywhere            │
│                                        │
│  Only Admin can reactivate your        │
│  account.                              │
│                                        │
│  [ ] I understand                      │
│  [      Deactivate my account      ]   │  ← red, disabled until checked
└────────────────────────────────────────┘
```

| Aspect | Design |
|---|---|
| Explanation | Plain bullet list of effects (BR-10.9) and the reactivation condition |
| Confirmation | Checkbox "I understand" enables the red button. A typed word is **not** used (UI Plan W-2.5). Tapping the red button opens a final C-23 dialog with the same consequences and a red confirm |
| Dialog | Title "Deactivate your account?"; body repeats the effects; Cancel default-focused; red "Yes, deactivate" |
| Success | Full-screen success: "Your account is deactivated. Only Admin can reactivate it." Then clear session and go to Log in. Do not leave the member on a half-authenticated page |
| Error | "We could not deactivate your account. Nothing was changed. Try again." (only if the orchestration is transactional; otherwise reflect what did happen) |
| Safety | Never auto-deactivate on a mis-tap; 44 px targets, checkbox required, destructive colour not used for any other action on the screen |
| Offline | Both controls disabled |

---

### 4.8 Photo upload sheet (C-31)

| State | UI |
|---|---|
| Empty | Avatar with initials, "Choose photo", guidance "JPG or PNG, up to 5 MB" (CFG-24 value) |
| Picking | Native file picker; accept `image/jpeg,image/png` |
| Preview | Circular crop, drag/zoom if the component supports it, "Use this photo" |
| Uploading | Progress or spinner on the primary |
| Done | Sheet closes, new avatar shown, toast "Photo updated." |
| Error (type/size) | Inline "Please upload a JPG or PNG up to 5 MB." Keep the sheet open, old photo intact (SC-10.2) |
| Remove | Secondary "Remove photo" with a small confirm; falls back to initials |
| Accessible | File input labelled; error announced; crop controls keyboard-accessible; 44 px targets |

---

## 5. State matrix (all screens)

| State | Required design |
|---|---|
| Loading | Skeleton (C-26), never a bare spinner; preserve layout height |
| Empty | Photo → initials; vehicle → hidden for Saathi; preview route → "No active route yet"; rating → "New member" |
| Error | Banner with plain message and one Retry; keep form input |
| Success | Toast (C-22, 4 s, polite live region) or success screen for destructive flows |
| Saving | Disable only the submitting control; show progress; prevent double submit |
| Disabled | Explain why inline (e.g. vehicle for Saathi, Save while saving, Deactivate until checked) |
| No permission | Not applicable to own-profile screens; generic 403 page for wrong role |
| Offline | Slim top banner "No connection. Changes will not be saved."; disable writes (UP/§4.3) |
| Session expired | Foundation dialog; unsaved Edit input kept in memory (BR-5.6) |
| Verify gate | Unverified member never reaches M1; sees Verify email only |
| Restricted | M1 fully usable; the restriction banner belongs to Leads |

---

## 6. Responsive rules

| Breakpoint | Behaviour |
|---|---|
| Phone 360–479 | Single column; sticky Save on Edit; bottom sheets for email/photo; Me hub full-width rows |
| Large phone 480–767 | Same single column, wider gutters |
| Tablet 768–1023 | Profile/preview card max ~640 px centred; Me hub two columns from 768 |
| Desktop 1024+ | Me hub: summary card left, settings list right; My Profile and Edit in a 480–560 px form column or two-column (photo/facts left, form right); sheets become centred dialogs (C-24) |
| Wide 1280+ | Member content max 1120 px; do not stretch forms |

- The shell navigation changes from bottom tabs to a left sidebar at tablet/desktop (UI Plan §4.1); M1 does not implement the shell.
- Keep the safe-area padding for the sticky Save and bottom nav.
- At 200% text zoom the layout must not scroll horizontally.

---

## 7. Accessibility

| Area | Requirement |
|---|---|
| Contrast | Text ≥ 4.5:1, control edges ≥ 3:1 using the palette in UI Plan §3.2 |
| Focus | Visible 2 px Indigo focus ring, never removed; return focus after sheets/dialogs close |
| Labels | Visible label above every field; placeholders are examples, not labels |
| Targets | ≥ 44 × 44 px, 8 px apart |
| Colour | Status/role/vehicle always have text, not colour alone |
| Screen readers | Announce toasts politely; announce "Photo uploaded", "Email change sent", "Role changed"; the gender lock reads "locked, only support can correct" |
| Keyboard | All fields, photo picker, email sheet, role change, deactivate checkbox/dialog usable by keyboard; dialogs trap focus |
| Motion | Honour "reduce motion"; no decorative animation |
| Language | `lang="en"`; dates as `09-Oct-2026`, times `8:05 AM`, India time |

---

## 8. Microcopy (M1)

| Situation | Copy |
|---|---|
| Page title | "My profile" |
| Email helper | "Your email is never shown to other members." |
| Mobile helper | "Not shown to other members." |
| Gender lock | "Only support can correct this." |
| Photo guidance | "JPG or PNG, up to 5 MB." |
| Photo error | "Please upload a JPG or PNG up to 5 MB." |
| Email change sent | "We sent a link to the new address. Your login email changes when you open it." |
| Email already used | "This email is already registered." |
| Email same | "This is already your email address." |
| Email link expired | "This link has expired. Request a new email change." |
| Role warning | "To become a Raahi, your current route must be switched off first. This withdraws your leads and pending requests. Your existing connections and chats stay." |
| Vehicle required | "Choose Bike or Car to continue as a Raahi." |
| Role done | "You are now a Raahi. Create your Raahi route to start finding Saathis." |
| Deactivate list | "Turning off your route · Withdrawing pending requests · Closing your connections and chats · Logging you out everywhere" |
| Reactivation | "Only Admin can reactivate your account." |
| Deactivated | "Your account is deactivated. Only Admin can reactivate it." |
| Preview privacy | "Email and mobile number are never shown to other members." |
| Rating placeholder | "New member" |
| Offline | "No connection. Changes will not be saved." |

Style: sentence case, active voice, one idea per sentence, no exclamation marks, never blame (UI Plan §3.8).

---

## 9. API mapping

| UI action | API call (API plan) |
|---|---|
| Load Me hub / My Profile | `GET /me/profile` |
| Load Edit limits (photo MB, vehicles) | `GET /me/profile/options` |
| Save name/mobile | `PUT /me/profile` |
| Change photo | `POST /me/profile/photo` (multipart) |
| Remove photo | `DELETE /me/profile/photo` |
| Change vehicle | `PUT /me/profile/vehicle` |
| Open email change | (same page) |
| Send email change | `POST /me/profile/email-change` |
| Verify email link | `POST /auth/email-change/verify` |
| Load How Others See Me | `GET /me/profile/preview` |
| Open Change Role | `GET /me/profile/role-change/preflight` |
| Confirm role switch | `POST /me/profile/role-change` |
| Deactivate account | `POST /me/profile/deactivate` |

Rules for the client:

- Always send the auth cookie/token; never pass a member id for self routes.
- On `401` → Foundation session-expiry handling; on `403` → generic no-access.
- On `429` show the cooldown time from the response.
- On `409 ROUTE_DEACTIVATION_REQUIRED` show Step A (do not silently retry).
- After any successful mutation, refresh `GET /me/profile` and the shell role chip.

---

## 10. Dependencies and placeholders

| Dependency | Until it exists, the UI does |
|---|---|
| M2 route summary | Preview shows "No active route yet"; role screen preflight reports no active route |
| M2 route creation | Step C "Create Raahi route" links into M2; the role is already switched (§4.6) |
| M9 notifications | Admin-driven photo removal does not get its own M1 screen; the notice appears in M9 when it exists |
| M7 ratings | Show "New member"; hide stars/number |
| M5 connections | Role/deactivate copy still states the rule; server no-ops the side effects |
| Foundation verification | Email change pending note still shown; verify link handled by the Foundation route if shared |
| Design tokens | Use UI Plan §3 values; do not hard-code colours in screens |
| Support contact for gender correction | UD-7: copy says "Only support can correct this" with the support link to be supplied |

---

## 11. Delivery plan (M1 UI)

| Step | Work | Depends on |
|---|---|---|
| 1 | Confirm design system tokens and build/reuse C-13, C-31, C-33, C-21, C-22, C-23, C-24, C-26 | UI Plan §3 |
| 2 | App shell **Me** tab + routes `/me`, `/profile`, `/profile/edit`, `/profile/preview`, `/profile/role`, `/profile/deactivate` | Shell/navigation |
| 3 | Me hub + My Profile against `GET /me/profile` with skeleton/empty/error | API endpoints 1, 3 |
| 4 | Edit Profile: name/mobile/vehicle save, gender lock, photo upload/remove | API 4, 5, 6, 7 |
| 5 | Email change sheet + pending banner + verify result page | API 8, 9 |
| 6 | How Others See Me with privacy note and route placeholder | API 2 |
| 7 | Change Journey Role: preflight, warning, vehicle step, success CTA to M2 | API 10, 11 |
| 8 | Deactivate: explanation, checkbox, dialog, logout | API 12 |
| 9 | Responsive desktop two-column, dialogs | — |
| 10 | Accessibility pass (labels, focus, targets, SR announcements) | — |
| 11 | QA: states, network, 200% zoom, privacy regression | API running |
| 12 | Replace route/rating placeholders when M2/M7 land | M2/M7 |

Suggested route files (framework-agnostic naming): `me/index`, `profile/index`, `profile/edit`, `profile/preview`, `profile/role`, `profile/deactivate`, `profile/email/verify`.

---

## 12. Acceptance checklist (UI)

| ID | Check |
|---|---|
| AC-10.1 | No M1 screen other than the member's own My Profile shows email or mobile; the preview explicitly states they are never shown |
| AC-10.2 | Edit shows Gender read-only with a lock and reason; there is no gender input |
| AC-10.3 | Switching role with an Active route forces the deactivation explanation and confirmation before continuing |
| AC-10.4 | Switching to Raahi without a vehicle blocks Continue until Bike/Car is chosen |
| BR-10.4 | Email change shows the pending note; the old email remains the login until verified |
| BR-10.5 | Photo errors use the exact JPG/PNG + size message; old photo remains |
| BR-10.9 | Deactivate lists all effects and requires the checkbox + final dialog; after success the member is logged out |
| UP-4 | Preview uses the public allow-list card only |
| UP-5 | Destructive flows state effects before acting |
| UP-10 | Loading, empty, error, success, disabled, offline states are implemented on every screen |
| UP-11 | Contrast, focus, labels, targets, keyboard and screen-reader announcements pass |
| — | Dates use `Mon YYYY` on the hub and `09-Oct-2026` where a full date is shown |
| — | Role chip in the shell updates immediately after a role change |

---

## 13. Open UI decisions

| # | Question | Proposed default |
|---|---|---|
| UD-7 | Support route for gender correction | Static "Only support can correct this"; support email/link to be supplied |
| — | Should the member's own mobile be masked on My Profile? | Show in full on the member's own screen; never in preview/others |
| — | Allow removing the profile photo? | Yes, via the photo sheet; confirm product preference |
| — | Show a setup-completion meter in M1? | Optional info banner only; no progress percentage in Phase 1 |
| — | Does the hub need "View profile" if the summary card opens `/profile`? | Make the card tappable; keep "Edit profile" explicit |
| — | Rating stars when no ratings | "New member" text only, no empty stars |
| — | Where to land after email verification when logged out | Login screen with a success toast |

---

## 14. Traceability

| Spec / reference | Where |
|---|---|
| SCR-10.1…10.5 | §2 routes, §4 screens |
| BR-10.1 completeness | §4.1 setup prompt, §4.2 |
| BR-10.3 gender lock | §4.2, §4.3 |
| BR-10.4 email change | §4.4 |
| BR-10.5 photo rules | §4.8 |
| BR-10.6 mobile privacy | §4.2, §4.5 |
| BR-10.7 role change | §4.6 |
| BR-10.8 vehicle change | §4.3 |
| BR-10.9 deactivation | §4.7 |
| AC-10.1…10.4 | §12 |
| UI Plan §3 design system | §3, §7 |
| UI Plan §4 navigation | §2 |
| UI Plan §6.2 W-2.1…W-2.5 | §4 |
| UI Plan §7 state patterns | §5, §6 |
| UI Plan §8 delivery | §11 |
