# RAAH — Phase 1 Functional Specification

**Jaha Chaah Waha Raah** — Saathi • Raahi • Admin

| | |
|---|---|
| **Document type** | Functional / System Specification (build-ready) |
| **Product** | RAAH, a pre-journey route-matching and connection platform |
| **Version** | 1.0 — Draft for client review |
| **Source** | *RAAH — Complete Product & Business Flow* (53 sections) and the client's answers to the clarification questions |
| **Platform** | One responsive web application for desktop and mobile browsers (D-20). A separate mobile app is built later |
| **Country / language** | India only, English (D-27) |
| **Scope** | Member registration and profile, routes, matching, leads, connections, chat, rating, safety, notifications, Admin management, configuration, dashboards and reports |
| **Excluded (later phases)** | Booking, driver assignment, payments, live tracking and SOS, ID verification, native mobile app, place picker, and the items listed in Section 29.2 |
| **Size** | 33 sections in 5 parts (Sections 0–32) · 15 modules · 30 reports · 54 validation rules · 15 end-to-end scenarios · 32 configurable settings |

> This document describes **what the system does**, screen by screen and scenario by scenario. It contains no program code and no database design.

---

## How to Read This Document

Every functional module (Parts B–D) is written in the same structure, so any module can be read on its own:

| Sub-heading | What it tells you |
|---|---|
| Purpose | Why the module exists, in business terms |
| Who uses it | Which role operates the screen |
| Screens | The screens to be built |
| Field specification | Every field, whether mandatory, and how it behaves |
| Business rules | Non-negotiable logic the system enforces (BR-x) |
| Status lifecycle | The states a record passes through, and who moves it |
| Scenarios | Normal flow, alternate flows and every exception (SC-x) |
| Data impact | Which records are created, changed or closed |
| Acceptance criteria | Key flows in Given / When / Then form (AC-x) |
| Reports produced | Which reports draw on this module |

**Numbering used throughout**

| Prefix | Meaning |
|---|---|
| D-x | Confirmed client Decision |
| A-x | Assumption made while writing, with its status (Confirmed / Replaced by a decision / Open), listed in Section 0 |
| BR-x | Business Rule |
| SC-x | Scenario |
| AC-x | Acceptance Criterion (Given/When/Then) |
| SCR-x | Screen |
| RPT-x | Report |
| CFG-x | Configurable setting |
| V-x | Validation rule |
| S-x, DEP-x | System assumption, dependency (Section 28) |
| E2E-x | End-to-end scenario (Section 26) |

---

## Contents

**Part A — Foundation**

- 0. Decisions & Assumptions Register
- 1. Objective & Business Problem
- 2. Solution Overview & Member Journey
- 3. Glossary of Terms
- 4. User Roles & Permission Matrix
- 5. Registration, Login, Session & Access Control
- 6. System Configuration Settings
- 7. Record Numbering
- 8. Audit Trail & History
- 9. Master Status Reference

**Part B — Member Setup Modules**

- 10. Module M1 — Member Profile
- 11. Module M2 — Route Management

**Part C — Core Modules**

- 12. Module M3 — Matching Engine
- 13. Module M4 — Lead Management
- 14. Module M5 — Connection Management
- 15. Module M6 — In-App Chat
- 16. Module M7 — Rating & Feedback
- 17. Module M8 — Safety: Block & Report (member side)
- 18. Module M9 — Notifications

**Part D — Admin Modules**

- 19. Module M10 — Member Management & Verification
- 20. Module M11 — Route & Lead Monitoring
- 21. Module M12 — Safety, Reports & Blocked Users
- 22. Module M13 — Matching Configuration & Platform Settings

**Part E — Output & Cross-Cutting**

- 23. Module M14 — Dashboards
- 24. Module M15 — Reports Catalogue
- 25. Consolidated Validation Rules
- 26. End-to-End Scenario Catalogue
- 27. Non-Functional Requirements
- 28. Assumptions & Dependencies
- 29. Phase 1 Boundary
- 30. Points for Client Confirmation
- 31. Go-Live Setup Sequence
- 32. Traceability Matrix

**Appendices**

- Appendix A — Document Conventions
- Appendix B — Sample Legal Content (Terms of Use, Privacy Policy, Safety Guidelines)

---

# PART A — FOUNDATION

---

# 0. Decisions & Assumptions Register

This section is the single place where choices made for this specification are recorded. Everything in later sections is consistent with it.

## 0.1 Confirmed Decisions (from client)

| # | Topic | Decision | Effect on the specification |
|---|---|---|---|
| D-1 | How members contact each other | **In-app chat** | Module M6. No phone number or email is shown to the other member by the system. |
| D-2 | What counts as a useful segment | Entirely depends on the route points the Saathi and the Raahi each enter. The Start and End points are mandatory; intermediate points are optional. **Maximum 5 points** (see A-1 for how this is counted). No minimum number of intermediate points. | Route rules in M2; matching rules in M3. |
| D-3 | How route points are entered | **Free text** (no fixed stop master). See D-17. | No Location/Stop Master module. |
| D-4 | Time tolerance | Departure times of Saathi and Raahi must be within **0–30 minutes** of each other | CFG-1, default 30. |
| D-5 | Journey details: exact time, meeting point, shared portion, frequency | Saathi and Raahi **decide mutually** | The system stores no agreed time, place or frequency. (The return route itself is **not** part of this — see D-15.) |
| D-6 | Who can initiate a connection | **Both sides** — a Saathi can request a Raahi, and a Raahi can request a Saathi | M4 and M5 support both directions. |
| D-7 | Pending request expiry | **1 month (30 days)** | CFG-5. |
| D-8 | Verification | **Email** verification | Mobile number is optional and is not verified. |
| D-9 | Vehicle type | **Bike / Car**, asked for a Raahi at registration (see D-18) | Field shown only when the member selects Raahi at registration. |
| D-10 | Gender-based preference | Added as a **filter** | Gender is captured on the profile; filter available on lead lists (see A-4). |
| D-11 | Platforms | **Web and Mobile**: one web application that works on both desktop and phone browsers (see D-20) | Responsive layout for every screen. A separate native mobile app is a later piece of work. |
| D-12 | Behaviour after a route change when a connection exists | **As per the source document** (the source document is silent) | See A-6 for the interim behaviour used. |
| D-13 | Rating and Feedback | **In Phase 1 scope** | Module M7. |
| D-14 | Active journey selection | **One active selection at a time** per member | One active route per member; a member is either Saathi or Raahi at any moment. That one route contains a Going leg and a Return leg (D-15). |
| D-15 | Return route | Saathi and Raahi **create the return route at the same time as the going route**, so that leads are visible to both Saathi and Raahi for both legs | A route has two legs: **Going** and **Return**. Both are entered when the route is created, both are matched, and a lead shows the result for each leg. Return is not a separate route and does not need a separate activation. |
| D-16 | Filtering leads by leg | Leads can be **filtered by Going route or Return route** | Lead list filter "Leg: Any / Going / Return" (see Section 2.3 and CFG-20). Detailed behaviour in M4. |
| D-17 | Route point entry | Saathi and Raahi **type every point themselves as free text**, including Start and End. No suggestions, no auto-complete, no place picker, no master list. | Members may spell the same place differently, so match accuracy depends on how consistently people write. How text is compared is decided in D-22. |
| D-18 | Journey role at registration | At the **very top of the registration form** the member selects **Saathi or Raahi**. If **Raahi** is selected, a **Vehicle** field appears with options **Bike** and **Car**. | Registration form in Section 5.2. The role chosen is the member's starting journey role (see A-19). Replaces A-3. |
| D-19 | Admin account creation | Admin accounts are **added directly in the database** by the owner. Admin logs in on the normal login screen and the **Admin UI opens**. | No admin registration screen and no "create admin" screen in Phase 1. Replaces A-11. |
| D-20 | Platform scope | Phase 1 is a **web application that works on both desktop and mobile browsers** (responsive). A **separate mobile app is built later**, as separate work. | Every screen must be comfortable on a phone and on a desktop. Every rule in this specification is written so a native mobile client can reuse it later. Replaces A-12. |
| D-21 | Mobile number | Collected **at registration only**, **hidden from all other members**, **visible to Admin**. | Never shown on leads, requests or chat; not used for login. Replaces A-14. |
| D-22 | How route point text is compared in matching | Comparison is **not case-sensitive**, and it **tolerates spelling mistakes**: two points count as the same place when **70% or more of the letters match** (CFG-22). Example: *Badarpur* is matched by *bdarpure* (75%) and *badarpoor* (78%). | Applies to every point of both legs. Replaces A-20. Numbers in a point must match exactly, and a short name inside a longer name is not a match (A-21). |
| D-23 | Message with a connection request | A request is sent **with a fixed message**: **"Hello, I want to connect with you"**. The sender **cannot edit** it. | The receiver sees it with the request; on acceptance it becomes the first chat message (M5, M6). Replaces A-34 and A-39. |
| D-24 | Terms of Use, Privacy Policy and Safety Guidelines | The texts are **managed by Admin** in the application. A **sample text is supplied** with the product; the client edits and publishes the final version later. | Section 22.5.1 (Legal Content), BR-5.13, Appendix B. |
| D-25 | Reach of the platform | RAAH is open to members **everywhere**; there is no area restriction. | See D-26 (City on each route). Expected member numbers are still needed for sizing (Section 30). |
| D-26 | Cities on each route | Each route carries a mandatory **Start City** and **End City** (they may be the same), chosen from an **Admin-managed list of cities**. Two routes are eligible for matching only when they **share at least one city**. | New City Master in M13 (Section 22.5.2); City fields in M2; city-overlap test in M3. Replaces A-55 and A-57. |
| D-27 | Country | **India only** for now. One time zone (India Standard Time), 10-digit mobile number, English. | Replaces the open question on countries (Section 30). |
| D-28 | Data retention | **No retention period is specified for now.** All records are kept (BR-8.1). To be decided later with legal advice. | Section 30, Point 9 is deferred by the client, not forgotten. |

## 0.2 Assumptions Register (status after client review)

Items marked **Confirmed** are now binding. Items marked **Replaced** were changed by the client and are superseded by the decision named. Items marked **OPEN** still need a client answer. IDs are kept so that references elsewhere in the document stay valid. **In this version no assumption is OPEN.** Business questions that are still undecided are in Section 30.

| # | Item | Status |
|---|---|---|
| A-1 | The 5-point limit counts all points including Start and End, so **each leg has 2 to 5 points**. | **Confirmed** |
| A-2 | System normalises and suggests route point text. | **Replaced** by D-17 (pure free text, no suggestions) |
| A-3 | Vehicle type asked when a Raahi route is first activated. | **Replaced** by D-18 (asked at registration) |
| A-4 | Gender is a mandatory profile field (Male / Female / Other); lead list can be filtered Any / Male / Female. It is a viewing filter, not a block on matching. | **Confirmed** |
| A-5 | A Useful Segment needs **at least 2 shared points** in the same order and direction (CFG-4). One shared point is not a segment. | **Confirmed** |
| A-6 | If a member edits or deactivates a route, existing connections and chats are unaffected. Pending requests tied to a deactivated route are withdrawn automatically. | **Confirmed** |
| A-7 | At most **10 pending requests** per member (CFG-6); no re-request to the same member for **7 days** after a rejection (CFG-17). | **Confirmed** |
| A-8 | Rating is 1–5 stars with an optional comment, one per member per connection, only after the connection is Accepted. Admin can hide abusive comments. | **Confirmed** |
| A-9 | Chat exists only while a connection is Accepted. It closes when either member disconnects or blocks. Admin can read a chat only inside an open safety report that involves it. | **Confirmed** |
| A-10 | There is one Admin role in Phase 1 (no sub-admin roles). | **Confirmed** |
| A-11 | Admin account created by the system owner; no self-registration. | **Replaced** by D-19 (added in the database; Admin UI opens on login) |
| A-12 | "Web and Mobile" delivered as one responsive web application. | **Replaced** by D-20 (web now, mobile later) |
| A-13 | Saathi routes match only Raahi routes, and Raahi routes match only Saathi routes. | **Confirmed** |
| A-14 | Mobile number optional and never displayed to members. | **Replaced** by D-21 (hidden from members, visible to Admin) |
| A-15 | The Return leg is **optional** by default (CFG-19 = No). If entered, it is matched like the Going leg. | **Confirmed** |
| A-16 | The Return leg has its own points and its own approximate departure time. A "Copy reversed from Going leg" button pre-fills the points in reverse order, and they can be edited. Travel days default to the Going days and can be changed. | **Confirmed** |
| A-17 | Matching is leg by leg (Going with Going, Return with Return). A lead exists if at least one leg matches. Leads matching both legs rank first; within a leg, Full Route ranks above Useful Segment. | **Confirmed** |
| A-18 | A connection request goes to the member as a whole, not per leg. Which leg(s) they travel together is decided in chat. | **Confirmed** |
| A-19 | After registration a member may **switch journey role** (D-14: deactivate the current route, then create a route of the other type). If they switch to Raahi and have no vehicle on their profile, the Vehicle field is asked at that point. | **Confirmed** |
| A-20 | Text comparison ignores only upper/lower case and extra spaces. | **Replaced** by D-22 (fuzzy match, 70% similarity) |
| A-21 | Two refinements to the fuzzy match of D-22: **(a)** numbers inside a point must match exactly ("Sector 12" does not match "Sector 15"); **(b)** a short name inside a longer one ("Badarpur Metro" inside "Badarpur Metro Station") is **not** treated as a match. | **Confirmed** |
| A-55 | Each route carries a mandatory City from an Admin-managed list; matching only within the same city. | **Replaced** by D-26 (confirmed by the client) |
| A-56 | A member whose city is not listed can use **"My city is not listed"** to send the city name to Admin. Admin adds it (the member is told) or declines it with a reason. | **Confirmed** |
| A-57 | Cross-city commutes (for example Delhi to Noida) need a Start City and an End City, with eligibility when the cities overlap. | **Replaced** by D-26 (confirmed by the client) |

## 0.3 Assumptions — Member Setup Modules (M1–M2)

All seven items below were confirmed by the client.

| # | Assumption | Status |
|---|---|---|
| A-22 | **Gender is locked after registration.** A member cannot edit it; Admin can correct it with a recorded reason. Gender is shown on the profile that other members see, because the gender filter exists. | **Confirmed** |
| A-23 | **Profile photo is optional.** | **Confirmed** |
| A-24 | A member **can change their email**, but the new email must be verified first. Login keeps using the old email until the new one is verified. | **Confirmed** |
| A-25 | A member **can deactivate their own account**. Only Admin can reactivate it. | **Confirmed** |
| A-26 | A route point **cannot contain a web link or a run of 10 or more digits** (this stops phone numbers being passed outside chat, in line with D-1). | **Confirmed** |
| A-27 | A member can hold **one Draft route at a time**, in addition to the one Active route. | **Confirmed** |
| A-28 | **Editing an Active route** keeps pending requests and existing connections as they are. Leads that no longer match are withdrawn by the system. | **Confirmed** |

## 0.4 Assumptions — Core Modules (M3–M9)

All items below were answered by the client. A-34 and A-39 were changed into decision D-23 (fixed request message).

| # | Assumption | Status |
|---|---|---|
| A-29 | **"Full Route" is judged from the Saathi's need.** A Raahi leg is a Full Route match when it covers the Saathi's leg from the Saathi's Start to the Saathi's End, in that order. The Saathi's unlisted intermediate points are not required to appear. The same label is shown to both members. | **Confirmed** |
| A-30 | **Shared points need not be next to each other.** The shared stretch is the longest set of points that appear in both legs in the same order. If a member did not type a stop on their route, it does not break the match. | **Confirmed** |
| A-31 | **Departure time is compared as typed** for the two legs, inclusive of CFG-1 (so a 30-minute gap matches, 31 does not). Times either side of midnight are compared correctly (23:50 and 00:10 are 20 minutes apart). | **Confirmed** |
| A-32 | **Excluded from matching:** members who are Unverified, Inactive, Temporarily Blocked or Permanently Blocked; routes that are Draft, Inactive, Invalid or Under Review. **Restricted** members are still matched. | **Confirmed** |
| A-33 | A lead the member **Dismissed stays dismissed** even if matching is recalculated, until the member undoes it. | **Confirmed** |
| A-34 | A connection request carries no message. | **Replaced** by D-23 (request carries the message "Hello, I want to connect with you") |
| A-35 | Two members can have only **one open connection** (Pending or Accepted) at a time. If both send a request to each other, the second person is shown the first request with an Accept option instead of creating a second one. | **Confirmed** |
| A-36 | The **cool-off (CFG-17) applies after a Rejection only**: the rejected sender cannot send again to the same member for that many days. | **Confirmed** |
| A-37 | There is **no cap** on the number of Accepted connections a member can hold. | **Confirmed** |
| A-38 | Leads are refreshed **almost immediately** (target: within one minute) after a route is activated, edited, deactivated or reactivated. | **Confirmed** |
| A-39 | The request message can be edited by the sender. | **Replaced** by D-23 (the message is fixed; no edit option) |
| A-40 | Chat is **text only** (no images, files, voice or live location), up to CFG-26 characters (default 1000) per message. | **Confirmed** |
| A-41 | **No read receipts or typing indicators.** Only unread counts per conversation. | **Confirmed** |
| A-42 | Messages **cannot be edited or deleted** by members. A closed chat stays readable (read-only) by both members. | **Confirmed** |
| A-43 | Chat **does not block phone numbers or links** (members' own choice); a standing safety tip is shown instead. | **Confirmed** |
| A-44 | A rating is **final once submitted**. Other members see only the average and count; the comment is visible to the rated member and Admin. Stars 1–5; comment up to CFG-27 characters (default 300). | **Confirmed** |
| A-45 | The average is shown only after **CFG-28 ratings** (default 3); before that the profile says "New member". | **Confirmed** |
| A-46 | A member can **report only someone they have a lead or connection with**. Reasons: Harassment, Inappropriate messages, Fake profile or wrong details, Safety concern, Spam, Other (description mandatory for Other). Reported member is not told who reported. | **Confirmed** |
| A-47 | **Block** is immediate, works both ways, is silent to the blocked member, and the blocker can **unblock** later. | **Confirmed** |
| A-48 | Notification **defaults** as in the matrix of Section 18.4: daily leads digest at CFG-31 (default 07:00), expiry reminder CFG-29 days before expiry (default 5), unread-chat email after CFG-30 hours (default 12), and optional emails can be switched off but safety, verification and account emails cannot. | **Confirmed** |

## 0.5 Assumptions — Admin Modules (M10–M13)

All six items below were confirmed by the client.

| # | Assumption | Status |
|---|---|---|
| A-49 | Admin can **manually mark a member's email as verified**, with a recorded reason (for example when verification emails do not reach the member). | **Confirmed** |
| A-50 | **Restriction** means one thing in Phase 1: the member **cannot send connection requests**. It may have an end date. A **Temporary Block** lifts itself on its end date. A **Permanent Block** cannot be reversed inside the application. | **Confirmed** |
| A-51 | The Admin configuration screen has a **Test Match tool**: Admin types two sample legs and sees the result under the current settings. It uses sample text only, never member data. | **Confirmed** |
| A-52 | Admin has an explicit **"Recalculate all leads"** action (with confirmation and logging) to apply changed matching settings to every Active route. Without it, new settings apply only as routes are activated or edited. | **Confirmed** |
| A-53 | A safety report still **Submitted or Under Review after CFG-32 days** (default 3) is escalated on the Admin dashboard. | **Confirmed** |
| A-54 | There is **one Admin role** with all Admin functions (A-10 already confirmed). Every Admin action needs a **reason of at least 10 characters** where this part says "with reason". | **Confirmed** |

---

# 1. Objective & Business Problem

## 1.1 Questions the System Must Answer

At any moment, without manual effort, the system must be able to answer:

| # | Question | Answered by |
|---|---|---|
| 1 | Who are our members, and are they verified? | Member register (M1, M10) |
| 2 | What is each member's complete route, in order? | Route (M2) |
| 3 | When does each member regularly travel? | Travel pattern on Route |
| 4 | Who else travels a relevant route at a relevant time? | Matching Engine (M3) |
| 5 | Is the overlap a full route or a useful segment, and why was it matched? | Lead (M4) with match explanation |
| 6 | Who requested whom, and who accepted, rejected or ignored? | Connection (M5) |
| 7 | Can connected members talk to each other safely? | In-App Chat (M6) |
| 8 | Is every member safe — who is blocked, reported, restricted? | Safety (M8, M12) |
| 9 | What did members think of each other? | Rating & Feedback (M7) |
| 10 | Is matching actually producing connections? | Admin lead monitoring (M11) |
| 11 | Can every lead, request, decision and admin action be traced to a user and a time? | Audit trail (Section 8) |

## 1.2 The Core Business Problem

A daily office or college commuter faces crowded transport, uncertain shared autos, waiting and repeated transport changes. Someone travelling the same way at the same time very likely exists — but there is no way to find them. The failure patterns RAAH is built to prevent:

| Failure pattern | What happens today | What the system does instead |
|---|---|---|
| **Invisible co-traveller** | Two people travel the same road every morning and never meet. | Members add their complete route; RAAH finds relevant people before the journey. |
| **Exact-match blindness** | A tool matches only identical Start → End. Someone who covers only the first half of the route is never found. | RAAH searches the full route first, then the longest useful segment. |
| **Platform decides the journey** | Ride apps book, assign and fix pickup and timing. This does not fit shared daily commuting. | RAAH only connects. Members decide time, meeting point, shared portion and days themselves. |
| **Unsafe early contact** | Phone numbers are exchanged with strangers before any trust exists. | Contact is possible only through in-app chat, and only after both sides accept. Block and report are always available. |

## 1.3 Phase 1 Goal

> **Right Person + Right Route Overlap + Right Time + Safe Connection + Traceable History**

## 1.4 Product Statement

> **Find the person before the journey. Decide the journey together.**

RAAH is a **pre-journey route-matching and connection platform**. Its output is a **lead** to a relevant person — never a booking.

## 1.5 What RAAH Does and Does Not Do

| RAAH DOES | RAAH DOES NOT |
|---|---|
| Collect each member's complete route | Book a ride |
| Collect regular travel pattern (days, approximate departure time) | Assign a driver or passenger |
| Search for full-route matches first | Fix a pickup or drop point |
| Search for the longest useful segment if no full match | Force or fix a journey time |
| Present relevant people as leads with a reason for the match | Force a shared route |
| Let members connect by request and acceptance | Decide anything on a user's behalf |
| Provide in-app chat between connected members | Share phone numbers or emails automatically |
| Provide block, report, rating and admin safety tools | Track live journeys, take payments or share fares (not in Phase 1) |

---

# 2. Solution Overview & Member Journey

## 2.1 The Journey in Stages

Each stage has a named owner, a record, and a system effect.

| Stage | Record created | Owner | System effect |
|---|---|---|---|
| Member registers and selects Saathi or Raahi | Member | Member | Account created as Unverified; starting journey role recorded |
| Member verifies email | — | Member | Account becomes Active |
| Member completes profile | Profile | Member | Profile complete |
| Member creates a route (as Saathi or Raahi) with a Going leg and a Return leg | Route | Member | Route saved as Draft |
| Member activates the route | — | Member | Route becomes Active and eligible for matching |
| RAAH searches | Lead (system-generated) | System | Full-route and useful-segment leads appear for both sides |
| Member sends a connection request | Connection Request | Either member | Status Pending; receiver notified |
| Receiver accepts | Connection | Receiving member | Status Accepted; chat opens |
| Receiver rejects, ignores, or sender withdraws | — | Either | Rejected / Expired (after 30 days) / Withdrawn |
| Members chat and decide the journey | Chat messages | Both members | **No system record of the decision** — it belongs to the members |
| Members travel | — | Both members | Outside the system |
| Members rate each other | Rating | Either member | Rating stored |
| Anyone reports a problem | Safety Report | Member → Admin | Admin review and action |

## 2.2 Journey Flow

```text
Register
   ↓
Verify Email
   ↓
Complete Profile
   ↓
Create ONE Route: Going leg + Return leg (as Saathi or Raahi)
   ↓
Activate Route
   ↓
RAAH Matching ──── Full Route match? ──── YES ──→ Full Route Lead
   │                       │
   │                       NO
   │                       ↓
   │              Longest Useful Segment ──→ Useful Segment Lead
   ↓
Member views Lead and profile
   ↓
Send Connection Request  (either side can send)
   ↓
Receiver: Accept / Reject / No response (30 days → Expired)
   ↓ (Accept)
Connection  →  In-App Chat
   ↓
Members decide timing, meeting point, shared portion, frequency (outside RAAH)
   ↓
Actual Travel  →  Rating & Feedback
```

## 2.3 How a Match Is Decided (summary)

The full rules are in Module M3. In one view, a Raahi route becomes a lead for a Saathi route (and vice versa) only when **all** of these hold:

| Test | Rule |
|---|---|
| Both routes are **Active** | Draft, Inactive and Invalid routes are never matched |
| **Opposite roles** | Saathi ↔ Raahi only (A-13) |
| **City overlap** | The two routes share at least one city (Start City or End City) (D-26) |
| **Travel days overlap** | At least one common day of the week |
| **Departure time within tolerance** | Within CFG-1 minutes (default 30) |
| **Same direction** | Shared points appear in the same order in both routes |
| **Enough shared points** | At least CFG-4 shared points (default 2) |
| **Points match by text** | Typed points are compared ignoring case; two points are the same place when their text similarity is at least CFG-22 (default 70%), so small spelling mistakes still match. Numbers must match exactly (D-22, A-21) |
| **Not blocked** | Neither member has blocked the other |
| **Leg by leg** | Going is matched with Going, Return with Return. The tests above (days, time, direction, shared points) apply to each leg separately. A lead exists if at least one leg matches (A-17) |

The overlap is then classified:

```text
Full Route Match      →  Raahi route covers the Saathi's entire route, Start to End
Useful Segment Match  →  Raahi route covers part of it; the longest such part ranks first
```

This classification is made **separately for the Going leg and the Return leg**. A lead can therefore show, for example, *Going: Full Route* and *Return: Useful Segment*, or a match on one leg only. Ranking order (A-17): leads matching **both legs** first, then single-leg leads; inside each leg, Full Route before Useful Segment, and longer overlap before shorter.

**Lead filters (D-10, D-16).** Both a **Saathi** (viewing Raahi leads) and a **Raahi** (viewing Saathi leads) can narrow their lead list by:

| Filter | Options | Effect |
|---|---|---|
| Leg | Any / Going / Return | **Going** (for example, home to office) shows only leads that match on the Going leg; **Return** (for example, office to home) shows only leads that match on the Return leg; **Any** shows all |
| Gender | Any / Male / Female | Shows only members of that gender (CFG-12) |

Filters only change what is shown. They do not change how matching works, and they do not hide a lead permanently.

## 2.4 Design Principles (Binding on Development)

These fifteen principles govern every design decision in this document. Where a specific requirement and a principle appear to conflict, the **principle wins** and the requirement must be re-discussed.

| # | Principle |
|---|---|
| 1 | RAAH connects people before the journey. It never books, assigns or decides a journey. |
| 2 | A route is always a complete, ordered list of points — never just Start and End. |
| 3 | Matching looks for a full-route match first, then the maximum useful segment. |
| 4 | Route points are entered by members; the system never invents or edits a member's route. |
| 5 | Saathi and Raahi are journey-context roles, not permanent account types. |
| 6 | Either side may start a connection. Nobody is connected without the other's acceptance. |
| 7 | A connection is not a booking. It only allows two members to talk. |
| 8 | Journey details (time, meeting point, shared portion, days) are decided by the members in chat and are not stored as system commitments. |
| 9 | The only communication channel is in-app chat, and only between connected members. |
| 10 | Safety is first-class: verification, block, report, restriction and admin review always exist. |
| 11 | Private information is exposed to others only as far as necessary. |
| 12 | Admin monitors and keeps the platform safe; Admin never matches or assigns people manually. |
| 13 | Every lead, request, response and admin action is traceable; nothing is hard-deleted. |
| 14 | Every lead shows why it was matched (which points, which days, how much overlap). |
| 15 | Thresholds such as time tolerance, expiry and limits are configurable, never hard-coded. |

---

# 3. Glossary of Terms

This glossary is binding — these words carry exactly these meanings throughout the document and in the application screens.

| Term | Definition |
|---|---|
| **RAAH** | The platform. A pre-journey route-matching and connection platform. |
| **Member** | A registered person with an account. The permanent identity. |
| **Saathi** | The role a member plays while looking for a relevant person to travel with along their route. A journey-context role. |
| **Raahi** | The role a member plays while travelling a regular route and open to someone joining them on it. A journey-context role. |
| **Journey Role** | Saathi or Raahi. Selected at registration (D-18) and thereafter given by the type of the member's single active route. |
| **Vehicle Type** | Bike or Car. Asked for a member who selects Raahi. |
| **Route** | A member's regular commute, made of a **Going leg** and a **Return leg**, each with its own ordered points and travel pattern. A member has one Active route at a time. |
| **Going Leg** | The outgoing journey (for example, home to office). Mandatory. |
| **Return Leg** | The coming-back journey (for example, office to home). Entered together with the Going leg so that leads show for both. Optional by default (CFG-19). |
| **Route Point** | One named place on a leg, entered as free text. Start and End are mandatory; intermediate points are optional. A leg has 2 to 5 points in total. |
| **Start / End** | The first and last point of a route. Always mandatory. |
| **Travel Pattern** | The days of the week and approximate departure time of the member's regular travel, set separately for each leg. |
| **Departure Time** | The approximate time the member leaves the Start point. Used for matching within the time tolerance. |
| **Active Route** | A route that is saved, reviewed and switched on. Only Active routes are matched. |
| **Matching** | The system process that compares an Active route with Active routes of the opposite role and produces leads. |
| **Full Route Match** | A lead where the other member's route covers the searching member's entire route, Start to End. |
| **Useful Segment** | The part of a route that two members share, in the same order and direction. The longest shared part is the "maximum useful segment". |
| **Text Similarity** | A percentage showing how alike two typed route points are, ignoring upper/lower case and extra spaces. It counts how many letters must be added, removed or changed to turn one text into the other, compared with the length of the longer text. Two points match when similarity is at least CFG-22 (default 70%). Numbers in the text must match exactly (A-21). |
| **Time Tolerance** | The largest allowed gap between two members' departure times (CFG-1, default 30 minutes). |
| **Lead** | A system-produced suggestion of one relevant person, with the reason for the match. A lead is not a booking and not a connection. |
| **Connection Request** | A request from one member to another, created from a lead. Starts as Pending. |
| **Connection** | An accepted request. It allows the two members to chat. |
| **Chat** | The in-app messaging space between two connected members. |
| **Journey Decision** | The time, meeting point, shared portion and days that two members agree between themselves. RAAH does not record it. |
| **Rating** | A 1–5 star score with optional comment that one connected member gives another. |
| **Block** | A member's action that removes another member from their leads, requests and chat, in both directions. |
| **Safety Report** | A member's complaint about another member, reviewed by Admin. |
| **Restriction** | An admin action that limits what a member can do (for example, cannot send requests). |
| **Admin** | The platform manager. Monitors, verifies, handles safety. Does not assign journeys. |

---

# 4. User Roles & Permission Matrix

## 4.1 The Roles

| Role | Scope | One-line description |
|---|---|---|
| **Admin** | Whole platform | Manages members, verification, safety, reports, matching configuration and platform settings. Never assigns people to each other. |
| **Member** | Own data | The normal user. Creates a route, receives leads, connects, chats, rates and reports. |
| **— as Saathi** | Own data | Member context while their active route is of type Saathi. |
| **— as Raahi** | Own data | Member context while their active route is of type Raahi. |

Saathi and Raahi are **not separate accounts** and not permanent. They are the two journey contexts of one Member (D-14: one active selection at a time).

```text
                    ┌───────────────┐
                    │     ADMIN     │  whole platform
                    └───────┬───────┘
                            │ monitors / configures / safety
                    ┌───────┴───────┐
                    │    MEMBER     │  own data only
                    └───────┬───────┘
              ┌─────────────┴─────────────┐
              ↓                           ↓
     Active route = Saathi        Active route = Raahi
     (looks for a Raahi)          (looks for a Saathi)
```

## 4.2 Full Permission Matrix

`C` = Create · `V` = View · `E` = Edit · `A` = Approve / Decide · `—` = No access

| Function | Admin | Member (Saathi) | Member (Raahi) |
|---|---|---|---|
| Register / log in | Log in only (D-19) | C V E (own) | C V E (own) |
| Own profile | V | C V E | C V E |
| Create / edit own route | — | C V E | C V E |
| Activate / deactivate own route | Deactivate for safety only | C V E | C V E |
| View any member's route | V | — | — |
| View a member's mobile number | V | — | — |
| View leads for own route | — | V | V |
| Send connection request | — | C | C |
| Accept / reject request received | — | A | A |
| Withdraw own pending request | — | E | E |
| Chat with connected member | — (only within an open safety report) | C V | C V |
| Disconnect from a member | — | E | E |
| Rate a connected member | — | C | C |
| Block a member | V | C | C |
| Report a member | V | C | C |
| Member management (verify, activate, deactivate) | C V E A | — | — |
| Restrict / temporarily block / permanently block | A | — | — |
| Review safety reports and take action | A | — | — |
| Moderate ratings / comments | A | — | — |
| View all leads and connections (monitoring) | V | — | — |
| Matching configuration | C V E | — | — |
| Platform settings / notifications | C V E | — | — |
| All reports | V | — | — |
| Audit trail / login audit | V | — | — |

## 4.3 Permission Rules

**BR-4.1** — A member can see and act only on their own data: own profile, own route, leads generated for their route, their own requests and connections, and chats they are part of. The only information about another member they can see is the limited profile shown on a lead (Module M4).

**BR-4.2** — Access control is enforced on the server for every request, not only by hiding menu items. A member who edits a record number in the browser address bar to open another member's route, connection or chat must receive "access denied", and the attempt is written to the audit trail.

**BR-4.3** — A member has **at most one Active route at a time** (D-14). The type of that route (Saathi or Raahi) is the member's current journey role. That one route holds both the Going leg and the Return leg (D-15); the two legs are activated and deactivated together, never separately. To switch role, the member deactivates the current route and creates or activates a route of the other type (see A-19).

**BR-4.4** — A member cannot send a connection request to themselves, to a member they have blocked, to a member who has blocked them, or to a member who is Restricted from receiving requests, Temporarily Blocked or Permanently Blocked.

**BR-4.5** — Admin has full read visibility for monitoring but cannot create, edit or delete a member's route, lead, request or connection. Admin may only deactivate a route or restrict/block a member for safety reasons, with a recorded reason.

**BR-4.6** — Admin never matches or assigns a Saathi to a Raahi (or the reverse), and never forces a journey decision.

**BR-4.7** — Admin cannot read the content of a chat in normal operation. Chat content becomes visible to Admin only inside an **open Safety Report** that involves that chat, and that access is itself logged (A-9).

**BR-4.8** — A Restricted member keeps access to their account but loses the restricted capability (for example sending requests). A Temporarily or Permanently Blocked member cannot log in.


**BR-4.9** — A member's mobile number is hidden from all other members everywhere in the product (leads, requests, chat, ratings). Admin can view it (D-21).
---

# 5. Registration, Login, Session & Access Control

## 5.1 Screens

| Screen | Purpose |
|---|---|
| SCR-5.1 Register | New member selects Saathi or Raahi and creates an account |
| SCR-5.2 Verify Email | Confirms the emailed verification link or code |
| SCR-5.3 Login | Email and password entry |
| SCR-5.4 Forgot Password | Request a reset link |
| SCR-5.5 Reset Password | Set a new password from the emailed link |
| SCR-5.6 Change Password | Logged-in member changes own password |
| SCR-5.7 My Account | View own name, email, verification status and current journey role |
| SCR-5.8 Admin Login | Same login screen. Admin accounts are added directly in the database; the Admin UI opens after login (D-19) |

## 5.2 Registration Field Specification

Profile details (photo and others) are collected in Module M1 after the first login. Only the fields below are needed to create the account.

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| I am a (Journey Role) | Radio / Dropdown | Yes | **First field at the top of the form.** Options: Saathi / Raahi (D-18) |
| Vehicle | Dropdown | Conditional | Shown **only when Raahi is selected**; then mandatory. Options: Bike / Car (D-9, D-18, CFG-21) |
| Full Name | Text | Yes | 3–100 characters |
| Email | Email | Yes | Unique across the system (including inactive and blocked accounts). Used for login, verification and password reset. |
| Password | Password | Yes | Minimum 8 characters, must include a letter and a number. Masked. |
| Confirm Password | Password | Yes | Must match Password |
| Gender | Dropdown | Yes | Male / Female / Other (A-4). Used only for the gender filter. |
| Mobile Number | Number | No | 10 digits. Collected here only. Not verified. Hidden from all other members; visible to Admin (D-21). |
| Accept Terms of Use, Privacy Policy and Safety Guidelines | Checkbox | Yes | Must be ticked. Links open the current published texts. The acceptance is recorded with the version and time (D-24, BR-5.13) |

## 5.3 Login Field Specification

| Field | Type | Mandatory | Behaviour |
|---|---|---|---|
| Email | Email | Yes | Case-insensitive match |
| Password | Password | Yes | Masked; never displayed or emailed |
| Remember this device | Checkbox | No | Extends session validity on this browser |

## 5.4 Business Rules

**BR-5.1** — Registration creates the account in status **Unverified**. The member cannot create a route, see leads or send requests until the email is verified (D-8).

**BR-5.2** — A verification link is sent to the registered email and is valid for CFG-11 hours (default 24). The member can request a fresh link; each new link invalidates the previous one.

**BR-5.3** — An email address cannot be registered twice. The system gives a clear message for a duplicate email, but does not confirm whether that account is active, blocked or inactive.

**BR-5.4** — A member who is Inactive, Temporarily Blocked or Permanently Blocked cannot log in. The message is generic ("Invalid credentials or inactive account") and does not reveal which condition applies.

**BR-5.5** — After CFG-9 consecutive failed login attempts (default 5), the account is locked for CFG-10 minutes (default 15). Admin can unlock it manually.

**BR-5.6** — A session expires after CFG-8 idle minutes (default 30). The member returns to the login screen, with unsaved route entry preserved where technically possible.

**BR-5.7** — On every login the system records member, timestamp, device/browser, IP and success/failure. Admin sees this in the Login Audit report (RPT-25).

**BR-5.8** — A password reset link is valid for CFG-11 hours and can be used once. Passwords are stored hashed and are never recoverable, displayed or emailed.

**BR-5.9** — If Admin changes a member's status (restriction, block, deactivation), the change applies from the member's next request. An open session is ended when the status is Inactive or Blocked.

**BR-5.10** — The same member can log in from several devices. Changes made on one device are visible on the others on the next screen load.

**BR-5.11** — Admin accounts are not created through the application. They are added directly in the database (D-19). On login the system reads the account's role: an Admin lands on the Admin UI, a Member on the Member UI.

**BR-5.12** — The Journey Role chosen at registration is the member's starting role. A Raahi cannot complete registration without choosing a Vehicle (Bike or Car).

**BR-5.13** — **Legal texts.** The Terms of Use, Privacy Policy and Safety Guidelines are public (readable before registering) and managed by Admin (Section 22.5.1). Registration requires accepting all three; the system records member, document, version and time. When Admin publishes a new version marked *requires re-acceptance*, every member must accept it at their next login before using the product (D-24).

## 5.5 Scenarios

**SC-5.1 — Normal registration and first login**
1. Person opens RAAH, selects Saathi or Raahi at the top of SCR-5.1 (a Raahi also selects Bike or Car), then fills name, email, password, gender and accepts the terms.
2. System creates the account as Unverified and emails a verification link.
3. Person opens the link; the account becomes Active.
4. Person logs in and is taken to profile completion (M1).

**SC-5.2 — Login before verifying email**
1. Member registers but does not verify, then tries to log in.
2. System lets them reach only the "Verify your email" screen with an option to resend the link. No route or lead functions are available.

**SC-5.3 — Verification link expired**
1. Member opens a link older than CFG-11 hours.
2. System says the link has expired and offers "Send a new link". The old link remains invalid.

**SC-5.4 — Duplicate email**
1. Person registers with an email already in use.
2. System rejects it with "This email is already registered. Try logging in or reset your password." Nothing is created.

**SC-5.5 — Account locked after repeated failures**
1. Member fails login CFG-9 times.
2. System locks the account for CFG-10 minutes and says it is temporarily locked.
3. Admin sees the locked flag in Member Management and may unlock it immediately.

**SC-5.6 — Forgot password**
1. Member requests a reset from SCR-5.4 with their email.
2. System always shows "If this email is registered, a reset link has been sent", whether or not the email exists.
3. Member opens the link, sets a new password, and all other active sessions are ended.

**SC-5.7 — Member blocked mid-session**
1. Admin permanently blocks a member who is logged in.
2. On the member's next action the session ends and they are returned to login.
3. Their routes are deactivated, their pending requests are withdrawn and their connections are closed (rules in M12).

**SC-5.8 — Member opens another member's record**
1. A member edits the URL to a connection or route that belongs to someone else.
2. System responds "You do not have access to this record." The attempt is written to the audit trail with user, time and record attempted.

**SC-5.9 — Same member on two devices**
1. Member is logged in on laptop and phone.
2. Sending a request on the phone shows as Pending on the laptop on the next refresh. There is one account and one active route.

**SC-5.10 — Raahi registration**
1. Person selects **Raahi** at the top of the form; the Vehicle field appears.
2. They choose **Car** and complete the other fields.
3. The account is created Unverified with starting role Raahi and vehicle Car.

**SC-5.11 — Raahi tries to register without a vehicle**
1. Person selects Raahi and leaves Vehicle empty.
2. System blocks the form with "Please choose Bike or Car." Nothing is created.

**SC-5.12 — Admin logs in**
1. Owner has added an Admin account directly in the database.
2. Admin enters email and password on the normal login screen.
3. System recognises the role and opens the Admin UI. There is no Admin registration screen.

**SC-5.13 — New terms published**
1. Admin publishes a new Terms of Use marked "requires re-acceptance".
2. At next login each member sees the new text and a button to accept. Until they accept, only the texts and Log out are available. Their acceptance is recorded with the new version.

## 5.6 Acceptance Criteria

**AC-5.1 — Email verification gate**
- Given a member registered but did not verify their email
- When they attempt to create a route
- Then the system blocks the action and shows the verification screen.

**AC-5.2 — Server-side access check**
- Given Member A is logged in and Member B owns connection C-100
- When Member A requests connection C-100 by changing the address
- Then the system returns "access denied" and logs the attempt.

**AC-5.3 — Account lock**
- Given CFG-9 is 5
- When a member enters a wrong password 5 times in a row
- Then the account is locked for CFG-10 minutes, even if the correct password is entered during that time.

**AC-5.4 — Raahi needs a vehicle**
- Given a person selects Raahi on the registration form
- When they submit without choosing Bike or Car
- Then the system rejects the form and no account is created.

**AC-5.5 — Role-based landing**
- Given an Admin account exists in the database
- When that user logs in with correct credentials
- Then the Admin UI opens, and a Member account on the same screen would open the Member UI.

**AC-5.6 — Legal acceptance recorded**
- Given a person ticks the acceptance box and registers
- When the account is created
- Then the version of each of the three texts and the time of acceptance are stored against the member.

---

# 6. System Configuration Settings

All settings below are maintained by Admin on a single Configuration screen (M13). None is hard-coded. A change applies to new activity from that moment and never rewrites completed records.

| ID | Setting | Type | Default | Effect |
|---|---|---|---|---|
| CFG-1 | Departure time tolerance (minutes) | Number | 30 | Largest allowed gap between two members' departure times for a match (D-4) |
| CFG-2 | Maximum points per leg (including Start and End) | Number | 5 | Upper limit on points in the Going leg and in the Return leg, each (D-2, A-1) |
| CFG-3 | Minimum points per leg | Number | 2 | Start and End are always mandatory |
| CFG-4 | Minimum shared points for a Useful Segment | Number | 2 | A segment needs at least this many shared points in the same order (A-5) |
| CFG-5 | Pending request expiry (days) | Number | 30 | A request not answered in this time becomes Expired (D-7) |
| CFG-6 | Maximum pending outgoing requests per member | Number | 10 | Prevents request spamming (A-7) |
| CFG-7 | Maximum leads shown per list | Number | 20 | Keeps lead lists short and relevant |
| CFG-8 | Session idle timeout (minutes) | Number | 30 | Inactivity before automatic logout |
| CFG-9 | Maximum failed login attempts | Number | 5 | Attempts before an account lock |
| CFG-10 | Account lock duration (minutes) | Number | 15 | How long a locked account stays locked |
| CFG-11 | Verification / reset link validity (hours) | Number | 24 | Life of an email verification or password reset link |
| CFG-12 | Gender filter enabled | Yes/No | Yes | Whether members can filter leads by gender (D-10) |
| CFG-13 | Rating enabled | Yes/No | Yes | Whether rating and feedback are available (D-13) |
| CFG-14 | Rating comment moderation | Yes/No | Yes | Whether Admin can hide a rating comment |
| CFG-15 | Reports against one member that trigger an admin alert | Number | 3 | Auto-flags a member for review |
| CFG-16 | Notification channels | Multi-select | In-app + Email | Channels for each event type (SMS is out of Phase 1) |
| CFG-17 | Cool-off after a rejected request (days) | Number | 7 | The same sender cannot re-request the same member within this time (A-7) |
| CFG-18 | Days of week counted as travel days | Multi-select | Mon–Sun | Allowed values for the travel pattern |
| CFG-19 | Return leg mandatory | Yes/No | No | Whether a member must enter a Return leg when creating a route (A-15, D-15) |
| CFG-20 | Leg filter enabled | Yes/No | Yes | Whether members can filter leads by Going / Return leg (D-16) |
| CFG-21 | Vehicle types offered to a Raahi | Multi-select | Bike, Car | Options in the Vehicle field (D-9, D-18) |
| CFG-22 | Text match threshold (%) | Number | 70 | Minimum letter similarity for two typed route points to count as the same place (D-22) |
| CFG-23 | Verification email resend cooldown (seconds) | Number | 60 | Minimum wait before another verification or reset email can be requested |
| CFG-24 | Maximum profile photo size (MB) | Number | 5 | Upload limit for a profile photo |
| CFG-25 | Request message text | Text | Hello, I want to connect with you | The fixed message sent with every connection request. Members cannot edit it (D-23). Admin may change the wording platform-wide. |
| CFG-26 | Maximum chat message length (characters) | Number | 1000 | Limit on one chat message (A-40) |
| CFG-27 | Maximum rating comment length (characters) | Number | 300 | Limit on a rating comment (A-44) |
| CFG-28 | Minimum ratings before the average is shown | Number | 3 | Below this the profile shows "New member" (A-45) |
| CFG-29 | Request expiry reminder (days before expiry) | Number | 5 | When the receiver is reminded of a Pending request (A-48) |
| CFG-30 | Unread chat email delay (hours) | Number | 12 | An unread message triggers one email after this time (A-48) |
| CFG-31 | Daily digest send time | Time | 07:00 | When the new-leads digest email goes out (A-48) |
| CFG-32 | Safety report overdue after (days) | Number | 3 | A report still Submitted or Under Review after this many days is escalated on the Admin dashboard (A-53) |

**BR-6.1** — Every configuration change is logged with old value, new value, user and timestamp, and appears in the Configuration Change report (RPT-26).

**BR-6.2** — Lowering CFG-2 does not invalidate existing routes that already have more points. It applies to routes created or edited afterwards.

**BR-6.3** — Changing CFG-1 or CFG-4 changes leads generated from that moment. Leads already shown to members are not withdrawn.

---

# 7. Record Numbering

Every record that members or Admin may refer to carries a unique, human-readable, non-reusable number generated by the system.

| Record | Format | Example |
|---|---|---|
| Member | `MBR-{nnnnnn}` | MBR-000231 |
| Route | `RTE-{nnnnnn}` | RTE-000418 |
| Lead | `LED-{nnnnnnn}` | LED-0004521 |
| Connection Request / Connection | `CON-{nnnnnn}` | CON-000087 |
| Rating | `RAT-{nnnnnn}` | RAT-000042 |
| Safety Report | `SAF-{nnnnnn}` | SAF-000009 |

**BR-7.1** — A number is generated on first save, never on screen open, so abandoned entries do not use up numbers.

**BR-7.2** — A number is never reused, even if the record is cancelled, withdrawn or deactivated.

**BR-7.3** — A connection request and the connection it becomes share one number (CON-). Its status changes; its number does not.

**BR-7.4** — Numbers are continuous across the whole platform. There is no yearly reset because Phase 1 has no financial year.

---

# 8. Audit Trail & History

**BR-8.1** — No record is ever physically deleted. A route is Deactivated, a request is Withdrawn, a member is made Inactive or Blocked. A reason is recorded for every such action by Admin.

**BR-8.2** — Every create, edit, activate, deactivate, send, accept, reject, withdraw, block, report, rate and admin action records: record, action, user, role, timestamp, old value and new value.

**BR-8.3** — Audit records are read-only to every role, including Admin.

**BR-8.4** — A member's chat messages are not part of the admin audit view. Only the facts that a chat existed and who accessed it under a safety report are logged (see BR-4.7).

**BR-8.5** — The history of any route, connection or safety report is visible from the record itself through a **History** tab, in chronological order. A member sees the history of their own records only.

**BR-8.6** — Edits to an active route keep every earlier version, so that "what the route looked like when this lead was generated" can always be shown.

## 8.1 What the History Tab Shows

| Column | Example |
|---|---|
| Date & time | 09-Oct-2026 08:32 |
| User | Ashish (Member) |
| Action | Connection request sent |
| Detail | Lead LED-0004521, match type: Useful Segment |
| Remarks | — |

---

# 9. Master Status Reference

This section is the single authority for every status used in the system. No module introduces a status outside this list.

## 9.1 Member Status

| Status | Meaning | Who sets it |
|---|---|---|
| Unverified | Registered, email not yet verified | System at registration |
| Active | Verified and in normal use | System on verification |
| Inactive | Deactivated; cannot log in | Admin (with reason) |
| Restricted | Can log in, but with a capability removed (for example, cannot send requests) | Admin (with reason) |
| Temporarily Blocked | Cannot log in until a stated date | Admin (with reason and end date) |
| Permanently Blocked | Cannot log in, final | Admin (with reason) |

## 9.2 Route Status

| Status | Meaning | Next possible status |
|---|---|---|
| Draft | Being prepared; not matched | Active, Inactive |
| Active | Reviewed and switched on; eligible for matching. Only one per member | Inactive, Under Review |
| Inactive | Switched off by the member (or by system on member block); not matched | Active |
| Invalid | Failed a validation (for example, a missing Start/End) or flagged as unusable | Draft (after correction) |
| Under Review | Held by Admin because it is the subject of an open safety report | Active, Inactive |

## 9.3 Route Type

| Type | Meaning |
|---|---|
| Saathi | The member is looking for a relevant person on their route |
| Raahi | The member regularly travels this route and is open to a Saathi |

## 9.3a Route Leg

A route status (9.2) applies to the route as a whole, so both legs are Active or Inactive together.

| Leg | Meaning | Mandatory |
|---|---|---|
| Going | The outgoing journey (for example, home to office) | Yes |
| Return | The coming-back journey (for example, office to home) | No by default; Yes if CFG-19 is on |

## 9.4 Lead Match Type

| Match type | Meaning |
|---|---|
| Full Route | The other member's route covers the searching member's route, Start to End |
| Useful Segment | The other member's route shares only part of the route; ranked by length of overlap |

The match type is recorded **separately for the Going leg and the Return leg** of a lead (A-17).

| Legs matched | Meaning | Rank |
|---|---|---|
| Both legs | Going and Return both match | Highest |
| Going only | Only the Going leg matches | Lower |
| Return only | Only the Return leg matches | Lower |

Within each leg, Full Route ranks above Useful Segment.

## 9.5 Lead Status

| Status | Meaning | Set by |
|---|---|---|
| New | Generated, not yet seen | System |
| Viewed | Seen by the member | System |
| Dismissed | Member chose "Not interested" | Member |
| Requested | A connection request has been sent from this lead | System |
| Withdrawn by System | The lead no longer applies (route changed, deactivated, other member blocked) | System |

## 9.6 Connection Status

| Status | Meaning | Next possible status |
|---|---|---|
| Pending | Request sent, awaiting a response | Accepted, Rejected, Withdrawn, Expired |
| Accepted | Both members are connected; chat is open | Disconnected, Blocked |
| Rejected | Receiver declined. Final | — |
| Withdrawn | Sender cancelled before a response. Final | — |
| Expired | No response within CFG-5 days. Final | — |
| Disconnected | One member ended the connection. Chat closed. Final | — |
| Blocked | One member blocked the other. Chat closed. Final | — |

*"Reported" is a flag on a connection that has an open safety report, not a separate status.*

## 9.7 Safety Report Status

| Status | Meaning | Who moves it |
|---|---|---|
| Submitted | Raised by a member | Member |
| Under Review | Admin is investigating | Admin |
| Action Taken | A warning, restriction or block was applied | Admin |
| Dismissed | No action needed | Admin (with reason) |
| Closed | Finished | Admin |

**Admin actions on a report:** Warning · Restriction · Temporary Block · Permanent Block.

## 9.8 Rating Status

| Status | Meaning |
|---|---|
| Submitted | Visible as given |
| Hidden | Comment hidden by Admin moderation (the star score still counts) |

---

# PART B — MEMBER SETUP MODULES

---

# 10. Module M1 — Member Profile

## 10.1 Purpose

Let a verified member present themselves to other members in a limited, safe way, keep their own details current, and switch between Saathi and Raahi when their need changes.

## 10.2 Who Uses It

Member edits their own profile. Admin views (Module M10).

## 10.3 Screens

| Screen | Purpose |
|---|---|
| SCR-10.1 My Profile | View own name, photo, gender, email, mobile, journey role, vehicle, verification status and rating summary |
| SCR-10.2 Edit Profile | Change name, photo, mobile, email, vehicle |
| SCR-10.3 How Others See Me | Preview of the limited profile that other members see on a lead |
| SCR-10.4 Change Journey Role | Switch from Saathi to Raahi or the reverse (A-19) |
| SCR-10.5 Deactivate My Account | Member ends their own access (A-25) |

## 10.4 Field Specification — Profile

| Field | Type | Mandatory | Editable by member | Rules |
|---|---|---|---|---|
| Full Name | Text | Yes | Yes | 3–100 characters |
| Profile Photo | Image upload | No (A-23) | Yes | JPG or PNG, up to CFG-24 MB (default 5). One photo; a new upload replaces it. |
| Gender | Display | Yes | **No** (A-22) | Set at registration. Admin may correct it with a reason. |
| Email | Email | Yes | Yes (A-24) | Unique. A change needs verification of the new address. |
| Mobile Number | Number | No | Yes | 10 digits. Hidden from other members; visible to Admin (D-21). |
| Journey Role | Display | Yes | Via SCR-10.4 only | Saathi or Raahi (D-18, A-19) |
| Vehicle | Dropdown | Conditional | Yes | Shown and mandatory only when the journey role is Raahi. Options: Bike / Car (CFG-21). |
| Verification Status | Display | — | No | Email Verified / Unverified (Section 9.1) |
| Rating Summary | Display | — | No | Average stars and number of ratings (Module M7) |
| Member Since | Display | — | No | Month and year of registration |

## 10.5 What Other Members See

When a lead or a connection request shows a member, only this limited view is used.

| Shown to other members | Never shown to other members |
|---|---|
| Name | Email |
| Photo (if any) | Mobile number (D-21) |
| Gender | Login history, IP, device |
| Journey role (Saathi / Raahi) | Past routes and deactivated routes |
| Vehicle: Bike / Car (Raahi only) | Reports raised by or against the member |
| Email verified badge | Admin notes, restrictions |
| Rating average and number of ratings | Other members' blocks |
| Member since (month, year) | |
| The **current Active route** (both legs: points, days, approximate departure time) | |

## 10.6 Business Rules

**BR-10.1** — The profile exists from registration. Name, gender and (for a Raahi) vehicle are already filled by then, so a profile is **complete** as soon as the email is verified. Photo and mobile are optional extras.

**BR-10.2** — A member cannot create a route, see leads or send requests until their status is Active (email verified), as per BR-5.1.

**BR-10.3** — Gender cannot be edited by the member after registration (A-22). A correction by Admin needs a reason and is written to the audit trail.

**BR-10.4** — **Email change** (A-24): the member enters the new email; the system sends a verification link to it; until it is verified the old email stays in use for login. The new email must not already belong to any account. When verified, the change is audited.

**BR-10.5** — **Profile photo**: only JPG or PNG up to CFG-24 MB. Admin may remove a photo that breaks the platform's safety guidelines, with a reason; the member is notified.

**BR-10.6** — The mobile number is never shown to any other member, anywhere in the product — leads, requests, chat headers, ratings (BR-4.9, D-21).

**BR-10.7** — **Changing journey role** (A-19, D-14):
1. The member opens SCR-10.4 and chooses the other role.
2. If the member has an Active route, it must first be deactivated. The system explains that deactivation withdraws the member's leads and pending requests, but keeps existing connections and chats (A-6).
3. If the new role is Raahi and the profile has no vehicle, the Vehicle field is asked now.
4. The member then creates a new route of the new type in M2.

**BR-10.8** — A change of **Vehicle** is allowed at any time. It applies to the member's active route from that moment and is audited.

**BR-10.9** — **Self-deactivation** (A-25): a member may deactivate their own account. This sets the member to Inactive, deactivates the Active route, withdraws pending requests, and closes connections, with the same effect as an admin deactivation (Module M10). Only Admin can reactivate the account.

**BR-10.10** — Every profile change is audited with old and new value (Section 8). Names changed by a member keep their history so that a past connection or report can still be traced.

## 10.7 Status Lifecycle

M1 uses the Member status of Section 9.1. Who moves each status in this module:

| From | To | Who | Condition |
|---|---|---|---|
| Unverified | Active | System | Email link opened in time |
| Active | Inactive | Member (BR-10.9) or Admin | Member self-deactivates, or Admin deactivates with a reason |
| Inactive | Active | Admin only | Reactivation with a reason |

Restricted and Blocked statuses are set by Admin (Module M12).

## 10.8 Scenarios

**SC-10.1 — Add a photo after first login**
1. A newly verified member opens SCR-10.2 and uploads a JPG of 2 MB.
2. The photo appears on SCR-10.1 and in SCR-10.3 (How Others See Me).

**SC-10.2 — Photo too large or wrong type**
1. Member uploads a 9 MB file, or a PDF.
2. System rejects it with "Please upload a JPG or PNG up to 5 MB." Nothing changes.

**SC-10.3 — Change name**
1. Member changes "Ashish K" to "Ashish Kumar".
2. The new name shows on all future screens. The audit trail keeps the old name.

**SC-10.4 — Change email**
1. Member enters a new email on SCR-10.2.
2. System emails a verification link to the new address; login still uses the old one.
3. Member opens the link; the email changes. The old email can now be used by a new registration.

**SC-10.5 — New email already in use**
1. Member enters an email that belongs to another account.
2. System says "This email is already registered." Nothing is sent.

**SC-10.6 — Preview how others see me**
1. Member opens SCR-10.3.
2. The screen shows exactly the "shown to other members" items of Section 10.5. Email and mobile are visibly absent.

**SC-10.7 — Saathi becomes Raahi**
1. Member (currently Saathi, with an Active route) opens SCR-10.4 and chooses Raahi.
2. System asks them to deactivate the current route first; they agree.
3. The Vehicle field appears; member chooses Car.
4. Member creates a new Raahi route in M2. Old connections and chats continue.

**SC-10.8 — Raahi changes vehicle**
1. A Raahi with an Active route changes Vehicle from Bike to Car.
2. Other members now see Car on this member's lead. The change is audited.

**SC-10.9 — Gender entered wrongly at registration**
1. Member tells Admin the gender is wrong.
2. Admin corrects it with a reason. The gender filter on others' lead lists reflects the change from the next search.

**SC-10.10 — Member deactivates own account**
1. Member opens SCR-10.5 and confirms.
2. The account becomes Inactive; the route is deactivated; pending requests are withdrawn; connections are closed. The member is logged out.
3. To come back, the member must ask Admin to reactivate.

## 10.9 Acceptance Criteria

**AC-10.1 — Mobile number never exposed**
- Given a member has entered a mobile number
- When any other member opens that member's lead, request or chat
- Then the mobile number is not displayed anywhere.

**AC-10.2 — Gender locked**
- Given a member has registered
- When they open Edit Profile
- Then Gender is shown but cannot be changed.

**AC-10.3 — Role switch needs route deactivation**
- Given a member has an Active Saathi route
- When they choose Raahi on Change Journey Role
- Then the system requires the Saathi route to be deactivated before continuing.

**AC-10.4 — Vehicle required for Raahi**
- Given a member without a vehicle on their profile switches to Raahi
- When they continue
- Then the system asks for Bike or Car and does not proceed without it.

## 10.10 Data Impact

| Action | Records created or changed |
|---|---|
| Profile edit | Member record updated; audit entry |
| Email change | Pending email held until verified; audit entry |
| Role change | Active route deactivated (leads and pending requests withdrawn); journey role updated when the new route is created |
| Self-deactivation | Member Inactive; route Inactive; pending requests Withdrawn; connections Disconnected |

## 10.11 Reports Produced

RPT-1 User & Access Report (full list in Section 24).

---

# 11. Module M2 — Route Management

## 11.1 Purpose

Capture each member's **complete route** — a Going leg and, if they travel back, a Return leg — with the regular days and approximate departure time, so the Matching Engine (M3) can find relevant people. This is the module that every other module depends on.

## 11.2 Who Uses It

Member creates and manages their own route. Admin views all routes and may deactivate one for safety reasons (BR-4.5).

## 11.3 Screens

| Screen | Purpose |
|---|---|
| SCR-11.1 My Route | Summary of the member's current route: status, Going leg, Return leg, vehicle, active-since date |
| SCR-11.2 Create / Edit Route | Form with two tabs: **Going** and **Return** |
| SCR-11.3 Review & Activate | Final read-only view of both legs with the Activate button |
| SCR-11.4 Route History | Past versions and past routes, read-only |

## 11.4 Route Structure

```text
ROUTE   (one per member Active at a time; type = Saathi or Raahi)
 ├── START CITY / END CITY   (mandatory to activate; from the Admin list, may be the same, D-26)
 ├── GOING LEG   (mandatory)
 │     ├── Start Point            (mandatory, free text)
 │     ├── Intermediate Points    (0 to 3, ordered, free text)
 │     ├── End Point              (mandatory, free text)
 │     ├── Travel Days            (at least one)
 │     └── Approximate Departure Time
 │
 └── RETURN LEG  (optional by default, CFG-19)
       ├── Start Point, Intermediate Points, End Point
       ├── Travel Days
       └── Approximate Departure Time
```

A leg has **2 to 5 points in total including Start and End** (CFG-2, CFG-3, A-1), so up to 3 intermediate points.

## 11.5 Field Specification — Route

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Route Number | Auto | — | Generated on first save (RTE-nnnnnn) |
| Member | Auto | — | Logged-in member |
| Route Type | Auto | — | Saathi or Raahi, taken from the member's journey role |
| Start City | Dropdown | Yes, to activate | The city where the Going leg starts, chosen from the Admin-managed list of active cities (D-26). Can be empty while the route is a Draft. |
| End City | Dropdown | Yes, to activate | The city where the Going leg ends. May be the same as the Start City. The Return leg runs the other way, so it uses the same two cities reversed. |
| Vehicle | Display | — | Raahi only; read from the profile (Bike / Car) |
| Status | Auto | — | Per Section 9.2 |
| Version | Auto | — | Increases each time an Active route is edited (BR-8.6) |

## 11.6 Field Specification — Each Leg (Going and Return)

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Start Point | Text | Yes (Going always; Return if Return is entered) | 2–100 characters. Typed by the member as free text (D-17). |
| Intermediate Point 1–3 | Text | No | Same text rules. Kept in the order entered; the member can reorder them. |
| End Point | Text | Yes | Same text rules. |
| Travel Days | Multi-select | Yes | At least one of Mon–Sun (CFG-18). Return defaults to the Going days and can be changed (A-16). |
| Approximate Departure Time | Time | Yes | Time the member usually leaves the Start point, in 24-hour or AM/PM entry. |

**Helper on the Return tab:** a **"Copy reversed from Going leg"** button pre-fills the Return points in reverse order and the same travel days. The member can edit any of it. The departure time is **not** copied and must be entered (A-16).

**Hint shown beside every point field:** "Enter an area or landmark, for example *Badarpur Metro*. You do not need your full address." This protects the member's home address and keeps text comparable.

## 11.7 Business Rules

**BR-11.1** — **One Active route per member** (D-14). A member cannot activate a second route while one is Active. They must deactivate the first.

**BR-11.2** — **One Draft at a time** (A-27). A member can hold one Draft route in addition to the Active one. Starting a new route while a Draft exists opens that Draft.

**BR-11.3** — **Start and End are mandatory** for every leg that exists. Intermediate points are optional. The total number of points per leg must be between CFG-3 (2) and CFG-2 (5).

**BR-11.4** — Points are **free text typed by the member** (D-17). The system gives no suggestions, auto-complete or place picker.

**BR-11.5** — **Point text rules**: 2 to 100 characters; letters, digits, spaces and the characters `. , - ' / & ( )` only. A point cannot contain a web link or a run of 10 or more digits (A-26).

**BR-11.6** — **No duplicate places inside a leg.** No two points in the same leg may be the same place, judged with the same text-match rule as matching (at least CFG-22, default 70%, with numbers exact, D-22, A-21). In particular the **Start and End must not be the same place.** The system points to the two clashing points so the member can correct one.

**BR-11.7** — **Order matters.** The sequence of points is the direction of travel and is what the Matching Engine compares (Section 2.3). Reordering changes the route.

**BR-11.8** — **Travel Days**: at least one day per leg. **Departure Time**: mandatory for every leg that exists.

**BR-11.9** — **Return leg**: optional by default (A-15, CFG-19). When CFG-19 is Yes, a route cannot be activated without it. If the member starts entering a Return leg, all its mandatory fields must be complete before activation. A member can remove the Return leg at any time while editing.

**BR-11.10** — **Return departure time** has no required relation to the Going departure time (for example, night shifts). The system only shows a gentle warning if the Return time is earlier than the Going time on the same day.

**BR-11.11** — **Draft**: a route can be saved at any stage as Draft with incomplete fields. Matching never uses a Draft (Section 2.3).

**BR-11.12** — **Activation** needs all of: member Active (email verified); Start City and End City chosen; Going leg complete and valid; Return leg complete and valid if present, or present if CFG-19 is Yes; the member has reviewed SCR-11.3. On activation the route becomes Active and is immediately submitted to matching.

**BR-11.13** — **Editing an Active route** (A-28): the route stays Active; a new **version** is saved, keeping the old one (BR-8.6). Matching re-runs. Leads that no longer match are withdrawn by the system (Lead status "Withdrawn by System"). **Pending requests and existing connections are not affected.** A receiver of a pending request sees the sender's current route, with a note that it changed since the request was sent.

**BR-11.14** — **Deactivating a route** (by the member, by role change, by self-deactivation, or by Admin): all its leads are withdrawn, pending requests the member sent or received through it are withdrawn, and the route cannot be matched. **Existing connections and chats are not affected** (A-6).

**BR-11.15** — **Reactivation**: an Inactive route can be reactivated by the member if they have no other Active route. It goes through review and activation again and is re-matched.

**BR-11.16** — **No hard delete.** A route is only deactivated. Past routes and past versions stay visible to the member on SCR-11.4 and to Admin.

**BR-11.17** — **Route type is fixed** once the route is created. Changing from Saathi to Raahi means creating a new route (BR-10.7).

**BR-11.18** — A route belongs to its member alone. No other member can open, edit or deactivate it (BR-4.1, BR-4.2). Admin may deactivate it only with a recorded reason (BR-4.5).

**BR-11.19** — **Raahi vehicle**: a Raahi route can be activated only if the member's profile has a vehicle.

**BR-11.20** — **Cities** (D-26): a route has a **Start City** and an **End City**, both mandatory before activation, chosen only from the Admin-managed list of **active** cities. They may be the same city. They apply to both legs (the Return leg uses them reversed). Matching considers two routes only when they **share at least one city** (BR-12.13). A member can change the cities on an Active route; this saves a new version and re-runs matching (BR-11.13). If the member's city is not in the list, they use **"My city is not listed"** to ask Admin to add it (A-56, Section 22.5.2); they cannot activate a route until both cities are chosen.

## 11.8 Status Lifecycle

Uses Route Status of Section 9.2. Both legs share one status.

| From | To | Who | Condition |
|---|---|---|---|
| (new) | Draft | Member | First save |
| Draft | Active | Member | Activation checks pass (BR-11.12) |
| Draft | Inactive | Member | Abandoned route kept for history |
| Active | Active (new version) | Member | Edit, BR-11.13 |
| Active | Inactive | Member / Admin / System | Deactivation, role change, account block or deactivation |
| Inactive | Active | Member | Reactivation, BR-11.15 |
| Active | Under Review | Admin (via M12) | Route is the subject of an open safety report |
| Under Review | Active / Inactive | Admin | After the report is decided |
| Draft / Active | Invalid | System | A rule above is found broken (for example CFG-2 data fix) |
| Invalid | Draft | Member | After correction |

## 11.9 What a Route Change Does to Leads and Requests

| Event | Leads | Pending requests | Connections / chats |
|---|---|---|---|
| Route activated | Generated (M3) | — | — |
| Active route edited | Re-calculated; those no longer matching become Withdrawn by System | Kept | Kept |
| Return leg removed | Leads that matched **only** on the Return leg are withdrawn | Kept | Kept |
| Route deactivated | All withdrawn | Withdrawn | Kept |
| Member blocked or deactivated by Admin | All withdrawn | Withdrawn | Closed (M12) |

## 11.10 Scenarios

**SC-11.1 — Saathi creates a full route with both legs**
1. Ashish (Saathi) opens SCR-11.2. On the **Going** tab he types: Start *Vinay Nagar*, then *Badarpur Metro*, *Govindpuri*, *Kalkaji*, End *Nehru Place*. Days Mon–Fri, time 8:00 AM.
2. On the **Return** tab he taps "Copy reversed from Going leg". Points now run Nehru Place → … → Vinay Nagar with Mon–Fri. He enters 6:30 PM.
3. On SCR-11.3 he reviews both legs and activates. Status Active; matching starts.

**SC-11.2 — Raahi creates only a Going leg**
1. Amit (Raahi, Bike) enters Going: Start *Vinay Nagar*, End *Badarpur Metro*, Mon–Fri, 8:05 AM. He leaves Return empty.
2. CFG-19 is No, so he activates. Only the Going leg takes part in matching.

**SC-11.3 — Draft finished next day**
1. Member enters Start and End, saves as Draft.
2. Next day he opens the Draft, adds days and time, reviews, activates.

**SC-11.4 — Too many points**
1. Member tries to add a 6th point to a leg.
2. System blocks: "A leg can have at most 5 points, including Start and End."

**SC-11.5 — Start and End are the same place**
1. Member types Start *Nehru Place* and End *nehru place*.
2. System blocks: "Start and End look like the same place." (They are 100% alike.)

**SC-11.6 — Two points that look alike**
1. Member adds *Govindpuri* and then *Govindpure* in the same leg.
2. The two are about 90% alike, so the system flags them as the same place and asks the member to remove or change one.
3. If he had added *Govindpuri* and *Govindpuri Metro* (about 62% alike), both would be accepted as different points.

**SC-11.7 — Phone number typed in a point**
1. Member types "Vinay Nagar 9876543210".
2. System blocks: "Please do not enter phone numbers or links. Use chat after connecting."

**SC-11.8 — No travel day chosen**
1. Member leaves Travel Days empty and tries to activate.
2. System blocks and highlights the missing field on the Going tab.

**SC-11.9 — Second route while one is Active**
1. Member with an Active route taps "Activate" on a Draft.
2. System says "You already have an Active route. Deactivate it first."

**SC-11.10 — Edit an Active route**
1. Ashish changes Going departure time from 8:00 to 9:30 AM.
2. A new version is saved; matching re-runs. A lead with a Raahi leaving at 8:05 now falls outside the time tolerance and is withdrawn.
3. A request Ashish had already sent to another Raahi stays Pending.

**SC-11.11 — Remove the Return leg**
1. Member removes the Return leg of an Active route.
2. Leads that matched only on Return are withdrawn; leads that matched on Going remain.

**SC-11.12 — Deactivate and reactivate**
1. Member deactivates his route while on leave. Leads are withdrawn; his pending requests are withdrawn; his connections and chats stay.
2. After leave he reactivates it. It goes through review and activation, and new leads are generated.

**SC-11.13 — Return leg compulsory**
1. Admin sets CFG-19 to Yes.
2. A member now tries to activate a route with only a Going leg. System asks them to complete the Return leg first.
3. Routes that were already Active are not forced to add a Return leg until they are next edited.

**SC-11.14 — Raahi without a vehicle**
1. A member who switched to Raahi has no vehicle on the profile.
2. System asks for Bike or Car before the Raahi route can be activated.

**SC-11.15 — Admin deactivates a route for safety**
1. Admin deactivates a route linked to a verified safety complaint, entering a reason.
2. The route becomes Inactive; leads and pending requests are withdrawn; the member is notified. Connections are handled by the safety action (M12).

**SC-11.16 — City missing from the list**
1. A member in a new city finds it is not in the list.
2. They tap "My city is not listed" and type the city name. Admin is notified; the member can save a Draft but cannot activate.
3. Admin adds the city and the member is told in-app. The member chooses it and activates the route.

**SC-11.17 — Same stops, different cities**
1. A Saathi (Start City Delhi, End City Delhi) and a Raahi (Noida, Noida) both type *Sector 12 Metro* and identical points.
2. Their routes share no city (Delhi → Delhi and Noida → Noida), so no lead is created between them.

**SC-11.18 — A cross-city commute**
1. Meera lives in Delhi and works in Noida. She chooses Start City Delhi and End City Noida.
2. A Raahi who travels Delhi → Noida, and a Raahi who travels only within Delhi, are both eligible for her, because each shares a city with her route. The stops must still match by text.

## 11.11 Acceptance Criteria

**AC-11.1 — Point limit per leg**
- Given CFG-2 is 5
- When a member adds a sixth point to the Going leg
- Then the system rejects it and the leg keeps its five points.

**AC-11.2 — Start and End mandatory**
- Given a member is activating a route
- When the Going leg has no End point
- Then activation is blocked and the End field is highlighted.

**AC-11.3 — One Active route**
- Given a member has an Active route
- When they try to activate another route
- Then the system refuses until the first is deactivated.

**AC-11.4 — Return leg optional**
- Given CFG-19 is No
- When a member activates a route with a complete Going leg and no Return leg
- Then the route becomes Active.

**AC-11.5 — Copy reversed**
- Given a Going leg of A → B → C
- When the member taps "Copy reversed from Going leg"
- Then the Return leg shows C → B → A with the same travel days and an empty departure time.

**AC-11.6 — Deactivation effects**
- Given a member has leads, a pending request and an accepted connection
- When they deactivate their route
- Then the leads and the pending request are withdrawn and the accepted connection and its chat remain.

**AC-11.7 — Same-place check**
- Given a leg with Start "Nehru Place"
- When the member enters End "nehru place"
- Then the system rejects it as the same place.

**AC-11.8 — City mandatory**
- Given a member is activating a route
- When the Start City or the End City has not been chosen
- Then activation is blocked and the missing city field is highlighted.

**AC-11.9 — Only active cities**
- Given Admin has deactivated a city
- When a member creates a new route
- Then that city is not offered in the list.

## 11.12 Data Impact

| Action | Records created or changed |
|---|---|
| Save Draft | Route created (RTE number), status Draft |
| Activate | Route Active; matching job run (M3) |
| Edit Active route | New route version; leads recalculated; audit entry |
| Deactivate | Route Inactive; leads Withdrawn by System; pending requests Withdrawn |
| Reactivate | Route Active; matching job run |

## 11.13 Reports Produced

Route Register and Route Coverage reports are defined in Section 24 (Reports Catalogue).

---

# PART C — CORE MODULES

---

# 12. Module M3 — Matching Engine

## 12.1 Purpose

Automatically find, for every Active route, the other members who travel a relevant route at a relevant time — **full route first, then the maximum useful segment** — and record the reason for each match. The engine never contacts anyone, never books anything and never decides a journey.

## 12.2 Who Uses It

The engine is a system process. Members see its result as **leads** (M4). Admin sets its parameters (M13) and monitors its results (M11). No member or Admin can create or edit a match by hand (BR-4.6).

## 12.3 Screens

The engine has no screen of its own. Its output is shown on SCR-13.1 to SCR-13.3 (M4). Its parameters are on the Admin configuration screen (M13).

## 12.4 When Matching Runs

| Trigger | What is re-checked |
|---|---|
| A route is activated | That route against all Active routes of the opposite type |
| An Active route is edited (new version) | That route against all opposite routes; leads that stop matching are withdrawn |
| A route is reactivated | As for activation |
| A route is deactivated, or its member is blocked or deactivated | All its leads are withdrawn |
| A block between two members is removed by Admin | That pair is re-checked |
| Route Under Review is released back to Active | That route is re-checked |

A change to CFG-1, CFG-4 or CFG-22 applies only to matching done after the change (BR-6.3).

## 12.5 The Matching Procedure

For a pair of members, one **Saathi** and one **Raahi**, the engine works in the order below. A pair becomes a lead only if it passes every test for at least one leg.

**Step 1 — Eligibility (route level)**

| Test | Rule |
|---|---|
| Opposite roles | One route is Saathi and the other Raahi (A-13) |
| Route status | Both routes Active (A-32) |
| Member status | Both members Active or Restricted (A-32) |
| City overlap | The two routes share at least one city: Start City or End City (D-26) |
| Not blocked | Neither member has blocked the other |

**Step 2 — Leg pairing.** Going is compared with Going, and Return with Return (A-17). A leg that one member did not enter is skipped.

**Step 3 — Days.** The two legs must have **at least one travel day in common**.

**Step 4 — Time.** The difference between the two approximate departure times must be **no more than CFG-1 minutes** (default 30), inclusive (A-31).

**Step 5 — Aligning points.** Each point of one leg is compared with the points of the other leg by **text similarity** (D-22):
- not case-sensitive; extra spaces ignored;
- two points are the same place when similarity is **at least CFG-22 (70%)**;
- **numbers inside a point must match exactly**; a short name inside a longer name is **not** a match (A-21);
- a point can be paired with only one point on the other side; if several candidates qualify, the one with the highest similarity is used.

**Step 6 — Direction and shared stretch.** Among the paired points, the engine finds the **longest set that appears in the same order in both legs** (A-30). If one leg runs the opposite way, the shared stretch is too short and the leg fails.

**Step 7 — Minimum overlap.** The shared stretch must have **at least CFG-4 points** (default 2, A-5).

**Step 8 — Classify the leg.**

| Result | Condition |
|---|---|
| **Full Route** | The Raahi's leg contains the Saathi's **Start and End** in that order, so the Raahi covers the Saathi's whole leg (A-29) |
| **Useful Segment** | Anything else that passed Steps 3 to 7. The shared stretch is the longest ordered set of shared points |
| No match | The leg failed any of Steps 3 to 7 |

**Step 9 — Lead.** If at least one leg is a match, a lead is created **for each of the two members** (one lead in the Saathi's list showing the Raahi, one in the Raahi's list showing the Saathi). Each lead records the result for both legs.

## 12.6 Ranking

Leads in a member's list are ordered by these keys, in this sequence (A-17):

| # | Key | Better is |
|---|---|---|
| 1 | Number of legs matched | Two before one |
| 2 | Number of legs that are Full Route | More first |
| 3 | Total shared points across matched legs | More first |
| 4 | Average time difference across matched legs | Smaller first |
| 5 | Number of common travel days | More first |
| 6 | Lead creation time | Earlier first |

Ranking never uses gender, rating or any other personal detail. Filters (Gender, Leg) change what is shown, not the order (M4).

## 12.7 What the Engine Records on Each Lead

| Item | Content |
|---|---|
| Lead number | LED-nnnnnnn, one per member side |
| For member / Matched member | The two members |
| Route versions used | The exact version of each route when the lead was last calculated (Principle 14, BR-8.6) |
| Per leg | Matched or not; Full Route or Useful Segment; list of shared points (as typed by each member); common days; time difference in minutes |
| Cities | Start City and End City of both routes |
| Calculated on | Date and time of last calculation |

## 12.8 Business Rules

**BR-12.1** — Matching is **deterministic and explainable**: the same two routes with the same settings always produce the same result, and every lead can show *which points, which days, what time difference* produced it (Principle 14).

**BR-12.2** — The engine matches **only Saathi with Raahi** (A-13).

**BR-12.3** — Draft, Inactive, Invalid and Under Review routes are never matched (A-32).

**BR-12.4** — A **Full Route** result is always preferred over a Useful Segment on the same leg. A Useful Segment is searched only when the leg is not a Full Route.

**BR-12.5** — The **time comparison** uses the approximate departure times typed by the members (A-31). It does not consider where on the route each member starts.

**BR-12.6** — The **minimum shared points** (CFG-4) is a rule on the **shared stretch**, so one common place alone never produces a lead.

**BR-12.7** — A pair of members has **one lead per side** at a time. Recalculation updates that lead; it does not create a second one. If a lead was withdrawn and the pair matches again, the same lead is revived (see BR-13.9).

**BR-12.8** — The engine **never changes** a member's route, never moves a point, and never fills in a missing stop.

**BR-12.9** — Blocking removes the pair from matching in both directions immediately.

**BR-12.10** — Leads are refreshed almost immediately (target within one minute, A-38). A member is never required to press "search".

**BR-12.11** — Gender is **not** a matching rule. It is a viewing filter on the lead list (D-10, A-4).

**BR-12.12** — Matching events are shown on the lead's own History tab (Section 8.1). They are not written as individual entries in the platform audit trail, because of volume. Changes to matching settings are audited (BR-6.1).

**BR-12.13** — **City overlap** (D-26): the engine pairs two routes only when they share at least one city (a Start City or an End City of one equals a Start City or an End City of the other). A route without cities is a Draft and is never matched.

## 12.9 Worked Example

**Ashish (Saathi), Going:** Vinay Nagar → Badarpur Metro → Govindpuri → Kalkaji → Nehru Place, Mon–Fri, **08:00**.
**Ashish, Return:** Nehru Place → Kalkaji → Govindpuri → Badarpur Metro → Vinay Nagar, Mon–Fri, **18:30**.

All routes below have Start City and End City **Delhi**. A Raahi whose cities do not include Delhi, even with identical stops, would not appear (D-26).

Candidates (all Raahi, Going leg unless stated):

| Raahi | Their Going route and time | Common days | Time gap | Shared points | Result for Ashish |
|---|---|---|---|---|---|
| **Neha** | Vinay Nagar → Govindpuri → Kalkaji → Nehru Place, Tue–Thu, 07:50 | Tue–Thu (3) | 10 min | 4 | **Full Route** (Ashish's Start and End both inside her route) |
| **Rohit** | Kalkaji → Nehru Place, Mon–Fri, 08:00 | Mon–Fri (5) | 0 min | 2 | **Useful Segment** (Kalkaji → Nehru Place) |
| **Amit** | Vinay Nagar → Badarpur Metro, Mon–Fri, 08:05 | Mon–Fri (5) | 5 min | 2 | **Useful Segment** (Vinay Nagar → Badarpur Metro) |
| **Sunil** | Nehru Place → Kalkaji → Vinay Nagar, Mon–Fri, 08:00 | Mon–Fri | 0 min | 1 in order | **No lead** (runs the opposite direction) |
| **Pooja** | Vinay Nagar → Badarpur Metro, Mon–Fri, 09:00 | Mon–Fri | 60 min | 2 | **No lead** (time gap above 30) |
| **Karan** | Kalkaji → Saket, Mon–Fri, 08:00 | Mon–Fri | 0 min | 1 | **No lead** (only one shared point) |

Neha also entered a Return leg: Nehru Place → Kalkaji → Govindpuri → Vinay Nagar, Tue–Thu, 18:20. It is a Full Route for Ashish's Return leg (10 min gap). Her lead therefore matches **both legs**.

**Order in Ashish's lead list:**
1. **Neha** (two legs matched, both Full Route)
2. **Rohit** (one leg, Useful Segment, 0-minute gap)
3. **Amit** (one leg, Useful Segment, 5-minute gap)

Rohit ranks above Amit because they tie on keys 1 to 3 and Rohit's time gap is smaller (key 4).

**Spelling example.** If Amit typed *Bdarpur Metro* and *vinay nagar*, the points still pair (about 93% and 100%), and the result is unchanged. If he typed *Sector 12* where Ashish typed *Sector 15*, they would **not** pair, because numbers must match exactly (A-21).

## 12.10 Scenarios

**SC-12.1 — New route finds a Full Route match**
1. Ashish activates his route.
2. Within about a minute his list contains Neha as a Full Route lead on both legs. Neha's list receives a lead showing Ashish.

**SC-12.2 — Only a useful segment exists**
1. Ashish travels Vinay Nagar to Nehru Place; no Raahi covers all of it.
2. Amit's route covers only Vinay Nagar → Badarpur Metro. The lead is marked Useful Segment with that stretch shown.

**SC-12.3 — Opposite direction**
1. A Raahi's route is Nehru Place → Vinay Nagar at 08:00.
2. The shared points appear in the opposite order, so the shared stretch is one point. No lead.

**SC-12.4 — Time gap just outside**
1. Saathi leaves 08:00; Raahi leaves 08:31 (CFG-1 = 30).
2. The 31-minute gap fails Step 4. No lead. At 08:30 the lead would exist.

**SC-12.5 — Time across midnight**
1. A night-shift Saathi leaves 23:50; a Raahi leaves 00:10.
2. The gap is 20 minutes, so Step 4 passes.

**SC-12.6 — Misspelled stop still matches**
1. Raahi typed *badarpoor metro*; Saathi typed *Badarpur Metro*.
2. The points are about 87% alike, above 70%, so they pair.

**SC-12.7 — Numbers differ**
1. Saathi's route has *Sector 12 Metro*; Raahi's has *Sector 15 Metro*.
2. Numbers differ, so the points do not pair, even though the text is about 93% alike.

**SC-12.8 — Route edit removes a match**
1. Ashish edits his Going time from 08:00 to 09:30.
2. A Raahi leaving at 08:05 is now outside the tolerance; the lead on the Going leg is withdrawn. If the Return leg still matches, the lead stays, showing only Return.

**SC-12.9 — Other member deactivates**
1. Neha deactivates her route.
2. Her lead disappears from Ashish's list and his from hers. Nothing else changes; any accepted connection stays (A-6).

**SC-12.10 — Blocked pair**
1. Ashish blocks Rohit.
2. Both leads are withdrawn. They never appear in each other's lists again, even if routes match.

**SC-12.11 — No match found**
1. A member activates a rare route and nothing matches.
2. The member sees "No relevant person found yet. We will tell you when someone matches." (M4, M9). No error.

**SC-12.12 — Settings changed**
1. Admin raises CFG-1 from 30 to 45.
2. New calculations use 45. Leads already shown are not withdrawn (BR-6.3).

**SC-12.13 — No common city**
1. Ashish (Delhi → Delhi) and a Raahi (Noida → Noida) have identical points, days and times.
2. Step 1 fails because the routes share no city. No lead is created.

**SC-12.14 — Cross-city match**
1. Meera (Delhi → Noida) meets a Raahi who travels Delhi → Delhi. The routes share Delhi, so Step 1 passes and the stops are compared as usual.

## 12.11 Acceptance Criteria

**AC-12.1 — Time tolerance boundary**
- Given CFG-1 is 30 and a Saathi and a Raahi have otherwise matching legs
- When the departure times differ by exactly 30 minutes
- Then a lead is created; at 31 minutes no lead is created.

**AC-12.2 — Direction**
- Given two legs share points A and B, but A comes first in one and B first in the other
- When matching runs
- Then no lead is created for that leg.

**AC-12.3 — Minimum shared points**
- Given CFG-4 is 2
- When two legs share only one point
- Then no lead is created.

**AC-12.4 — Full Route**
- Given a Raahi leg contains the Saathi's Start and End in that order
- When matching runs
- Then the leg is classified Full Route, even if some of the Saathi's intermediate points are missing from the Raahi's leg.

**AC-12.5 — Fuzzy text and numbers**
- Given CFG-22 is 70
- When one member types *Badarpur Metro* and the other *bdarpur metro*
- Then the points pair; and *Sector 12* and *Sector 15* do not pair.

**AC-12.6 — Both legs ranking**
- Given one lead matches both legs and another matches one leg
- When the list is shown
- Then the both-legs lead is placed first.

**AC-12.7 — Exclusions**
- Given a route is Draft, Inactive, Invalid or Under Review, or its member is Blocked
- When matching runs
- Then that route produces no leads.

**AC-12.8 — Same-role pairs**
- Given two Saathi routes with identical points
- When matching runs
- Then no lead is created.

**AC-12.9 — City overlap**
- Given two routes are otherwise a perfect match
- When they share no city
- Then no lead is created; and when they share at least one city, a lead is created.

## 12.12 Known Limitations

- **Free text.** Matching accuracy depends on how consistently people write place names (D-17, D-22). If pilot results show missed matches, the threshold (CFG-22) can be lowered or a place picker considered later.
- **Departure time** is compared as typed, regardless of where each member starts on the route (BR-12.5). A Saathi who starts mid-route may therefore leave later than the Raahi's own start time. This follows the client's 0–30 minute rule (D-4).

- **Cities.** Matching checks that two routes share a city, not that each stop belongs to that city. A stop name that exists in two cities (for example *Sector 12 Metro*) could still pair across the shared city of a cross-city route. This is rare, and RPT-4 helps Admin spot it.

## 12.13 Data Impact

| Event | Records created or changed |
|---|---|
| Match found | Lead created for each member (or revived) |
| Match changes | Lead updated; route versions and leg results re-recorded |
| Match lost | Lead set to Withdrawn by System |

## 12.14 Reports Produced

Matching Summary and Lead Conversion reports are defined in Section 24 (Reports Catalogue).

---

# 13. Module M4 — Lead Management

## 13.1 Purpose

Show each member the relevant people found for them, explain why each person was matched, let them filter and dismiss, and start a connection request from a lead. A lead is **a suggestion, not a booking and not a connection** (Principle 7).

## 13.2 Who Uses It

Saathi (sees Raahi leads) and Raahi (sees Saathi leads). Admin views all leads for monitoring (M11).

## 13.3 Screens

| Screen | Purpose |
|---|---|
| SCR-13.1 My Leads | Ranked list of leads with filters |
| SCR-13.2 Lead Detail | Limited profile of the other member, route comparison, "why this match", Connect button |
| SCR-13.3 Dismissed Leads | Leads the member dismissed, with Undo |

## 13.4 What a Lead Row Shows (SCR-13.1)

| Item | Content |
|---|---|
| Person | Name, photo, gender, Bike/Car for a Raahi, verified badge, rating average and count (Section 10.5) |
| Cities | Start City and End City, for example *Delhi → Noida* |
| Going leg | Match type (Full Route / Useful Segment / No match), shared stretch (for example *Vinay Nagar → Badarpur Metro*) |
| Return leg | Same as above, or "Not entered" / "No match" |
| Common days | For example *Mon–Fri* |
| Departure times | Both members' times and the gap, for example *8:00 AM · 8:05 AM · 5 min apart* |
| Action | **Connect**, or the current state: *Request sent*, *Request received*, *Connected* |
| Dismiss | "Not interested" |

Match labels are worded from the viewer's side:

| Viewer | Full Route label | Useful Segment label |
|---|---|---|
| Saathi | "This Raahi covers your whole route" | "This Raahi shares part of your route" |
| Raahi | "Your route covers this Saathi's whole route" | "You share part of this Saathi's route" |

## 13.5 Lead Detail (SCR-13.2)

| Block | Content |
|---|---|
| Profile | The limited profile of Section 10.5. Email and mobile never appear. |
| Route comparison | The other member's Going and Return legs, listed point by point as typed by them, with the shared points highlighted. Days and approximate times shown beside each leg. |
| Why this match | Plain-language reasons: *Shared points: Vinay Nagar, Badarpur Metro. Common days: Mon–Fri. Time gap: 5 minutes.* (Principle 14) |
| Actions | Connect · Dismiss · Report · Block |

## 13.6 Filters (D-10, D-16)

| Filter | Options | Effect |
|---|---|---|
| Leg | Any / Going / Return | **Going** shows only leads matching on the Going leg; **Return** only leads matching on the Return leg; **Any** shows all. Available when CFG-20 is Yes. |
| Gender | Any / Male / Female | Shows only members of that gender. Available when CFG-12 is Yes. |

Both Saathi and Raahi can use both filters. Filters never change lead status, never change order, and never delete a lead.

## 13.7 Business Rules

**BR-13.1** — Leads are produced only by the Matching Engine (M3). A member cannot add a lead and Admin cannot add one for them (BR-4.6).

**BR-13.2** — A member sees only **their own leads** (BR-4.1). The other member's information shown is limited to Section 10.5.

**BR-13.3** — The list is ordered by the ranking of Section 12.6. Only the first CFG-7 leads (default 20) are shown at once, with "Show more".

**BR-13.4** — **Lead statuses** are those of Section 9.5: New, Viewed, Dismissed, Requested, Withdrawn by System. A lead becomes **Viewed** when its detail is opened.

**BR-13.5** — **Dismiss**: the member can mark a lead "Not interested". It moves to SCR-13.3 and stops appearing in the main list. **Undo** returns it. A dismissed lead stays dismissed even when matching is recalculated (A-33). Dismissing sends no notice to the other member.

**BR-13.6** — **Connect** from a lead is allowed only if all the following hold (rules in M5): the lead is not Withdrawn, the member is not restricted from sending requests, fewer than CFG-6 requests of theirs are Pending, no open connection already exists with this person, and the cool-off (CFG-17) is not running.

**BR-13.7** — After a request is sent, the lead status becomes **Requested**. The lead row shows the current connection state (Pending / Connected). If the request ends as Rejected, Withdrawn or Expired, the lead returns to **Viewed**. (A lead whose request was accepted keeps status Requested and is labelled **Connected**.)

**BR-13.8** — A lead that no longer applies (route changed, member deactivated, blocked) becomes **Withdrawn by System** and is hidden from the list. If an existing connection or pending request existed with that person, it is not touched (A-6, A-28).

**BR-13.9** — If a withdrawn lead matches again, the **same lead is revived**: it returns as New, unless it was Dismissed, in which case it stays Dismissed (A-33).

**BR-13.10** — Every lead keeps a **History tab** (BR-8.5) showing created, updated, viewed, dismissed, requested and withdrawn events.

**BR-13.11** — The lead list is a **live view**. If the other member edits their route, the lead shows their current route. A request already sent keeps the snapshot of how the route looked then (M5).

**BR-13.12** — An empty list shows a helpful message (SC-13.8), not an error.

## 13.8 Status Lifecycle

| From | To | Who | Trigger |
|---|---|---|---|
| (none) | New | System | Match found |
| New | Viewed | System | Member opens the lead |
| New / Viewed | Dismissed | Member | "Not interested" |
| Dismissed | Viewed | Member | Undo |
| New / Viewed | Requested | System | Member sends a request |
| Requested | Viewed | System | Request Rejected, Withdrawn or Expired |
| Any | Withdrawn by System | System | Match no longer valid |
| Withdrawn by System | New (or Dismissed if it was) | System | Match valid again |

## 13.9 Scenarios

**SC-13.1 — Saathi opens the lead list**
1. Ashish opens SCR-13.1. He sees Neha, Rohit and Amit in that order (worked example, Section 12.9).
2. Each row shows match type per leg, shared stretch, common days and times.

**SC-13.2 — Filter by Return leg**
1. Ashish selects Leg = Return.
2. Only Neha remains, because only she matches on the Return leg. Rohit and Amit are hidden by the filter, not deleted. Setting the filter back to Any shows all three.

**SC-13.3 — Filter by gender**
1. A female Saathi selects Gender = Female.
2. Only Raahis whose profile gender is Female are shown. The order of the remaining leads is unchanged.

**SC-13.4 — Why this match**
1. Ashish opens Amit's lead.
2. He sees Amit's route with *Vinay Nagar* and *Badarpur Metro* highlighted and the line "Shared points: 2 · Common days: Mon–Fri · Time gap: 5 minutes."

**SC-13.5 — Dismiss and undo**
1. Ashish dismisses Amit. The row disappears from the main list and appears on SCR-13.3.
2. Amit edits his route and still matches; the lead stays dismissed.
3. Ashish taps Undo and the lead returns to the main list.

**SC-13.6 — Raahi sends a request from a Saathi lead**
1. Amit (Raahi) opens his lead list and sees Ashish (Saathi).
2. He taps Connect. A request goes to Ashish (D-6). The lead shows "Request sent".

**SC-13.7 — Lead disappears**
1. Rohit deactivates his route.
2. Rohit's lead vanishes from Ashish's list. If Rohit and Ashish were already connected, the connection and chat remain.

**SC-13.8 — No leads**
1. A member's route has no matches.
2. The screen says: "No relevant person found yet. We will tell you when someone matches. You can add a Return leg or check your points and times to find more."

**SC-13.9 — Connect blocked by limit**
1. A member already has CFG-6 (10) Pending requests.
2. Connect is disabled with the message "You have too many pending requests. Wait for answers or withdraw one."

**SC-13.10 — Request cool-off**
1. Rohit rejected Ashish's request two days ago (CFG-17 = 7).
2. Rohit's lead in Ashish's list shows "You can request again in 5 days" and Connect is disabled.

## 13.10 Acceptance Criteria

**AC-13.1 — Mobile number and email hidden**
- Given a lead is displayed
- When the member opens the lead detail
- Then the other member's email and mobile number are not shown anywhere.

**AC-13.2 — Leg filter**
- Given a list with leads matching Going only, Return only and both
- When the member sets Leg = Return
- Then only leads matching on the Return leg are shown, and the order among them is unchanged.

**AC-13.3 — Gender filter**
- Given CFG-12 is Yes and the list has male and female members
- When the member sets Gender = Female
- Then only female members are shown.

**AC-13.4 — Dismissed stays dismissed**
- Given a member dismissed a lead
- When the matching is recalculated and the pair still matches
- Then the lead stays in the dismissed list.

**AC-13.5 — Why this match**
- Given any lead
- When the member opens its detail
- Then the shared points, common days and time gap are listed.

**AC-13.6 — Lead page size**
- Given CFG-7 is 20 and a member has 35 leads
- When the list opens
- Then 20 leads are shown with a "Show more" option.

## 13.11 Data Impact

| Action | Records created or changed |
|---|---|
| Open lead | Lead status New to Viewed |
| Dismiss / Undo | Lead status changes; history entry |
| Connect | Lead status Requested; connection request created (M5) |

## 13.12 Reports Produced

Lead Register and Lead Conversion reports (Section 24).

---

# 14. Module M5 — Connection Management

## 14.1 Purpose

Let one member ask another to connect, let the other accept or decline, and open a chat only when both agree. A connection does **not** book or fix any journey (Principle 7, Rule 8).

## 14.2 Who Uses It

Any Member, as sender or receiver (D-6). Admin views connections for monitoring (M11); Admin cannot create, accept or reject a connection (BR-4.5).

## 14.3 Screens

| Screen | Purpose |
|---|---|
| SCR-14.1 Confirm Request | Confirmation screen that shows the fixed request message (read-only), shown before a request is sent from a lead |
| SCR-14.2 Requests Sent | Pending and past requests the member sent, with Withdraw |
| SCR-14.3 Requests Received | Pending and past requests the member received, with Accept / Reject |
| SCR-14.4 My Connections | Accepted connections, with Open Chat |
| SCR-14.5 Connection Detail | The other member's limited profile, both routes as they were at request time, chat link, Disconnect, Block, Report, Rate |

## 14.4 Field Specification — Connection

| Field | Source | Notes |
|---|---|---|
| Connection Number | Auto | CON-nnnnnn. Shared by the request and the connection it becomes (BR-7.3) |
| Sender | Auto | The member who pressed Connect |
| Receiver | Auto | The other member |
| Created from lead | Auto | Link to the lead |
| Route snapshot | Auto | The exact versions of both members' routes and the per-leg match result at the moment the request was sent (Principle 14) |
| Status | Auto | Section 9.6 |
| Sent on / Responded on / Expires on | Auto | Expires on = Sent on + CFG-5 days |
| Request message | Text | Fixed text "Hello, I want to connect with you" (CFG-25). Shown read-only. The sender cannot change it (D-23). |
| Ended on / Ended by | Auto | For Disconnected, Blocked, Withdrawn |

Every request carries the fixed message (D-23); it cannot be removed or changed by the member.

## 14.5 Business Rules

**BR-14.1** — A request can be sent **from a lead only**. Neither member can request someone who is not among their current leads.

**BR-14.2** — **Either side can send** (D-6): a Saathi can request a Raahi and a Raahi can request a Saathi. Nobody is connected until the receiver accepts (Principle 6).

**BR-14.3** — A request is allowed only if: sender and receiver are Active (a Restricted sender must not be restricted from sending); neither has blocked the other (BR-4.4); the sender has fewer than CFG-6 Pending requests; no open connection (Pending or Accepted) exists between the two; and the cool-off (CFG-17) is not running (A-36).

**BR-14.4** — **One open connection per pair** (A-35). If Ashish has a Pending request to Rohit and Rohit presses Connect on Ashish's lead, Rohit is shown Ashish's request with an **Accept** option; no second request is created.

**BR-14.5** — **Accept**: only the receiver can accept. The status becomes Accepted, the chat opens (M6), both members are notified, and each sees the other in My Connections.

**BR-14.6** — **Reject**: only the receiver can reject. The status becomes Rejected and is final. The sender sees a neutral message ("Your request was not accepted") with no reason. The sender cannot request the same member again for CFG-17 days (default 7).

**BR-14.7** — **Withdraw**: only the sender can withdraw, and only while Pending. The status becomes Withdrawn and is final.

**BR-14.8** — **Expiry**: a Pending request not answered within CFG-5 days (default 30) becomes Expired and is final. The sender is told; the receiver is not penalised. No cool-off applies after expiry.

**BR-14.9** — **Pending requests always belong to Active routes.** If a member's route is deactivated, or the member is blocked or deactivated, all their Pending requests, sent and received, are Withdrawn automatically (A-6, BR-11.14).

**BR-14.10** — **Editing an Active route** does not change Pending requests (A-28). The receiver sees the sender's current route and a note: "Route changed since this request was sent". The request still keeps its snapshot.

**BR-14.11** — **Disconnect**: either member can end an Accepted connection. The status becomes Disconnected and is final, the chat closes, and the other member is told. Either member can later receive a new request from the other through a fresh lead.

**BR-14.12** — **Block** (M8) ends any open connection immediately, with status Blocked.

**BR-14.13** — A member may hold **any number of Accepted connections** (A-37).

**BR-14.14** — Connection data (members, snapshots, dates, statuses) are **never deleted** (BR-8.1). Closed connections stay in the member's history, read-only.

**BR-14.15** — Accepting does not book, schedule or promise anything. The screen states: "You can now chat. Decide time, meeting point and shared portion together."

**BR-14.16** — A member sees only connections they belong to (BR-4.1). Opening another connection's number is denied and logged (BR-4.2).

**BR-14.17** — **Request message** (D-23): every request carries the fixed message "Hello, I want to connect with you" (CFG-25). SCR-14.1 shows it as read-only text; the sender cannot edit, remove or replace it. The receiver sees the message on SCR-14.3 and SCR-14.5. If the request is Accepted, the message becomes the **first message in the chat** (M6). If it is Rejected, Withdrawn or Expired, the message stays only on the closed request record and never reaches a chat.

## 14.6 Status Lifecycle

Uses Section 9.6.

| From | To | Who | Condition |
|---|---|---|---|
| (new) | Pending | System | Sender presses Connect and confirms |
| Pending | Accepted | Receiver | Accept |
| Pending | Rejected | Receiver | Reject |
| Pending | Withdrawn | Sender | Withdraw; or System when a route is deactivated or a member is blocked or deactivated |
| Pending | Expired | System | CFG-5 days passed |
| Accepted | Disconnected | Either member | Disconnect |
| Accepted | Blocked | Either member | Block (M8) |

## 14.7 Scenarios

**SC-14.1 — Saathi connects with a Raahi**
1. Ashish opens Neha's lead and taps Connect. SCR-14.1 shows the fixed message "Hello, I want to connect with you" as read-only text, and he confirms.
2. A request CON-000087 is Pending. Neha is notified (M9).
3. Neha opens SCR-14.3, sees Ashish's limited profile and the route comparison, and taps Accept.
4. The connection is Accepted. Both can open the chat.

**SC-14.2 — Raahi connects with a Saathi (D-6)**
1. Amit (Raahi) finds Ashish in his leads and taps Connect.
2. Ashish accepts. The flow is the same in the opposite direction.

**SC-14.3 — Both press Connect**
1. Ashish has a Pending request to Rohit. Rohit, not having seen it yet, opens Ashish's lead and presses Connect.
2. The system shows Rohit Ashish's request and asks "Accept this request?". No second request is created (A-35).

**SC-14.4 — Reject and cool-off**
1. Rohit rejects Ashish's request.
2. Ashish sees "Your request was not accepted". Connect is disabled for 7 days on Rohit's lead (CFG-17). Rohit may still send Ashish a request if he later changes his mind.

**SC-14.5 — Sender withdraws**
1. Ashish realises he sent the request to the wrong person and withdraws it while Pending.
2. Status Withdrawn; the lead returns to Viewed. He can send again immediately; no cool-off applies.

**SC-14.6 — Request expires**
1. Neha never answers.
2. On day 30 the request becomes Expired and Ashish is told. He can send a new one from the lead.

**SC-14.7 — Too many pending requests**
1. A member has 10 Pending requests (CFG-6).
2. Connect is disabled until one is answered, withdrawn or expires.

**SC-14.8 — Sender's route deactivated while a request is pending**
1. Ashish deactivates his route for a week.
2. All his Pending requests, sent and received, are Withdrawn. Accepted connections and chats stay.

**SC-14.9 — Sender edits his route while the request is pending**
1. Ashish sent a request to Neha, then changed his Going time from 08:00 to 08:10.
2. The request stays Pending. When Neha opens it she sees Ashish's current route with the note "Route changed since this request was sent", and can also see the route as it was when the request was sent.

**SC-14.10 — Disconnect**
1. After a few weeks, Neha taps Disconnect.
2. Status Disconnected; chat closes; Ashish is told. History is kept.

**SC-14.11 — Request to a member who has just been blocked by Admin**
1. Ashish presses Connect on a lead whose member was blocked a minute ago.
2. The lead is already withdrawn; the system says "This person is no longer available."

**SC-14.12 — Accept after the sender became inactive**
1. A request is Pending, then the sender deactivates their account.
2. The request is Withdrawn automatically; the receiver can no longer accept it.

**SC-14.13 — The request message is fixed**
1. Ashish presses Connect. SCR-14.1 shows "Hello, I want to connect with you" as read-only text; there is no field to edit it.
2. He confirms. Neha sees the same message with the request, and after she accepts it appears as the first message in the chat.

## 14.8 Acceptance Criteria

**AC-14.1 — Either side can send**
- Given a Saathi lead in a Raahi's list
- When the Raahi presses Connect and confirms
- Then a Pending request is created for the Saathi.

**AC-14.2 — Only the receiver can accept**
- Given a Pending request
- When the sender tries to accept it by editing the address
- Then the system denies access and logs the attempt.

**AC-14.3 — Chat opens only on acceptance**
- Given a Pending request
- When no one has accepted
- Then no chat exists between the two members.

**AC-14.4 — Expiry**
- Given CFG-5 is 30
- When 30 days pass without a response
- Then the status is Expired and the sender is notified.

**AC-14.5 — Pending request limit**
- Given CFG-6 is 10 and a member has 10 Pending requests
- When they press Connect on another lead
- Then the request is not created.

**AC-14.6 — One open connection per pair**
- Given a Pending request from A to B
- When B presses Connect on A's lead
- Then B is shown A's request to accept and no second request is created.

**AC-14.7 — Deactivation withdraws pending only**
- Given a member has one Pending request and one Accepted connection
- When the member deactivates their route
- Then the Pending request becomes Withdrawn and the Accepted connection stays.

**AC-14.8 — Cool-off**
- Given CFG-17 is 7 and B rejected A's request today
- When A tries to send B a request tomorrow
- Then the system refuses and shows the remaining days.

**AC-14.9 — Fixed request message**
- Given a member presses Connect on a lead
- When the confirmation screen opens
- Then the message "Hello, I want to connect with you" is shown read-only and every request is sent with it.

**AC-14.10 — Message reaches chat only on acceptance**
- Given a Pending request with a message
- When the receiver accepts
- Then the message appears as the first chat message; and if the request is rejected, no chat is created.

## 14.9 Data Impact

| Event | Records created or changed |
|---|---|
| Send request | Connection (Pending) with route snapshot and request message; lead becomes Requested; receiver notified |
| Accept | Connection Accepted; chat created |
| Reject / Withdraw / Expire | Connection final; lead returns to Viewed |
| Disconnect / Block | Connection final; chat closed |

## 14.10 Reports Produced

Connection Register and Connection Funnel reports (Section 24).

---

# 15. Module M6 — In-App Chat

## 15.1 Purpose

Give two **connected** members one safe place to talk and to decide the journey between themselves — time, meeting point, shared portion, days (D-5). Chat is the **only** communication channel in RAAH (D-1, Principle 9).

## 15.2 Who Uses It

The two members of an Accepted connection. Admin can read a conversation only inside an open Safety Report that involves it (BR-4.7, A-9).

## 15.3 Screens

| Screen | Purpose |
|---|---|
| SCR-15.1 Chat List | All the member's conversations, newest first, with unread counts |
| SCR-15.2 Conversation | Messages with the other member, message box, header with the other member's limited profile and the actions Block · Report · Disconnect · Rate |

## 15.4 Field Specification — Message

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Message text | Text | Yes | 1 to CFG-26 characters (default 1000). Text only (A-40). |
| Sender | Auto | — | Logged-in member |
| Sent time | Auto | — | System timestamp |

A conversation has no title and no members other than the two connected members. There are no group chats.

## 15.5 Business Rules

**BR-15.1** — A chat exists **only for an Accepted connection** (A-9). There is no chat for Pending, Rejected, Withdrawn or Expired requests (AC-14.3).

**BR-15.2** — When a request is Accepted, the chat is created and its **first message is the request message** (D-23), shown as sent by the original sender at the time of the request.

**BR-15.3** — Only the two members of the connection can open the chat. Another member who edits the address gets "access denied", logged (BR-4.2).

**BR-15.4** — Messages are **text only** (A-40): no images, files, voice or live location in Phase 1. Maximum CFG-26 characters. An empty message cannot be sent.

**BR-15.5** — Messages are delivered almost immediately. The system shows unread counts per conversation. It does **not** show read receipts or typing indicators (A-41).

**BR-15.6** — A message **cannot be edited or deleted** by either member once sent (A-42, Principle 13). This keeps the record reliable for safety review.

**BR-15.7** — **Closing**: a chat becomes **Closed** when the connection ends — Disconnected, Blocked, or either member made Inactive or Blocked by Admin. A Closed chat accepts no new messages. Both members can still read its history, read-only (A-42).

**BR-15.8** — **A new connection after a closed one starts a new chat.** The old chat stays as history and is not merged.

**BR-15.9** — **Phone numbers and links are not blocked in chat** (A-43). Sharing personal contact details is each member's own choice. To protect members, the conversation shows a standing safety tip: "Do not share personal details or money until you trust the person. You can block or report at any time."

**BR-15.10** — The member's mobile number is never added to the chat by the system (BR-4.9, D-21).

**BR-15.11** — **Admin access**: chat content is hidden from Admin in normal operation. When a Safety Report involves a chat, the conversation is attached to the report and Admin can read it for the review. Each such access is logged (BR-8.4). The reporting member is told, before submitting, that Admin will be able to read the conversation (Section 17).

**BR-15.12** — Chat messages are never purged in Phase 1 (Section 27, data retention).

## 15.6 Status Lifecycle

| Status | Meaning | Moves to Closed when |
|---|---|---|
| Open | Both members connected; messages can be sent | Connection becomes Disconnected or Blocked, or a member is made Inactive or Blocked |
| Closed | Read-only history | — (final) |

## 15.7 Scenarios

**SC-15.1 — First message from the request**
1. Neha accepts Ashish's request that carried "Hello, I want to connect with you".
2. The chat opens with that message as the first line. Neha replies.

**SC-15.2 — Deciding the journey**
1. Ashish and Neha agree in chat: meet at Badarpur Metro at 8:10, travel Tue–Thu, Ashish pays for his share of petrol.
2. RAAH stores only the messages. It records no agreement and does not remind or enforce anything (D-5, Principle 8).

**SC-15.3 — Unread messages**
1. Neha sends two messages while Ashish is away.
2. Ashish's chat list shows "2 unread" on that conversation. Opening it clears the count.

**SC-15.4 — Disconnect**
1. Neha disconnects.
2. The chat closes. Both can read the history; neither can send.

**SC-15.5 — Block**
1. Ashish blocks Neha from the chat header.
2. The connection is Blocked and the chat is Closed. History stays read-only.

**SC-15.6 — Member shares a phone number in chat**
1. During chat, Neha types her number.
2. The message is sent normally; the chat keeps showing the safety tip. RAAH does not prevent it.

**SC-15.7 — Message too long**
1. Member pastes 1,500 characters.
2. System asks them to shorten it to 1,000.

**SC-15.8 — Admin reviews a reported chat**
1. Ashish reports Rohit for abusive messages (Section 17).
2. Admin opens the report and reads the attached conversation. The access is logged.
3. Without a report, Admin sees no chat content.

**SC-15.9 — Chat with a pending request**
1. Ashish has a Pending request to Rohit.
2. There is no chat yet. The screen says "You can chat once Rohit accepts."

**SC-15.10 — New connection after a disconnect**
1. Ashish and Neha disconnect, later match again, and connect afresh.
2. A new chat opens. The earlier closed chat remains in their list as history.

## 15.8 Acceptance Criteria

**AC-15.1 — Text only and length**
- Given CFG-26 is 1000
- When a member sends a 1,001-character message
- Then the system rejects it and nothing is sent.

**AC-15.2 — Chat only after acceptance**
- Given a request is Pending
- When either member opens the connection
- Then no chat messages can be sent.

**AC-15.3 — Closed chat is read-only**
- Given a connection becomes Disconnected
- When a member opens the chat
- Then they can read all messages and cannot send a new one.

**AC-15.4 — No edit or delete**
- Given a message was sent
- When the sender looks for edit or delete
- Then no such option exists.

**AC-15.5 — Admin access only through a report**
- Given Admin has no open report involving a chat
- When Admin looks for that chat's content
- Then the content is not available.

## 15.9 Data Impact

| Event | Records created or changed |
|---|---|
| Request Accepted | Chat created; request message becomes first message |
| Message sent | Message stored; unread count updated; notification (M9) |
| Connection ended | Chat set to Closed |
| Report raised | Conversation attached to the report; access logged |

## 15.10 Reports Produced

Chat Activity Summary (counts only, no content) in Section 24.

---

# 16. Module M7 — Rating & Feedback

## 16.1 Purpose

Let connected members rate each other so that trust builds up on the platform and members can choose with more confidence. Rating is in Phase 1 scope (D-13).

## 16.2 Who Uses It

A Member rates another member they have been connected with. Admin moderates comments (BR-16.8).

## 16.3 Screens

| Screen | Purpose |
|---|---|
| SCR-16.1 Rate Member | Choose 1–5 stars and an optional comment |
| SCR-16.2 Ratings Received | Ratings and comments the member received |
| SCR-16.3 Ratings Given | Ratings the member gave |

## 16.4 Field Specification

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Rating Number | Auto | — | RAT-nnnnnn |
| Connection | Auto | — | The connection the rating belongs to |
| From / To | Auto | — | The rater and the member rated |
| Stars | Select 1–5 | Yes | Whole numbers only |
| Comment | Text | No | Up to CFG-27 characters (default 300). No web links or runs of 10 or more digits. |
| Date | Auto | — | System timestamp |
| Status | Auto | — | Submitted / Hidden (Section 9.8) |

## 16.5 Business Rules

**BR-16.1** — A member can rate another only if they have an **Accepted connection** with them, now or in the past (A-8). A member cannot rate themselves or someone they were never connected with.

**BR-16.2** — **One rating per member per connection.** A new connection with the same person after a disconnect allows a new rating for that new connection.

**BR-16.3** — A rating is **final once submitted** (A-44). It cannot be edited or deleted by the rater. A mistake or an abusive rating is handled by Admin (BR-16.8).

**BR-16.4** — **Stars** are required; the **comment** is optional. Stars are whole numbers 1 to 5.

**BR-16.5** — **What others see**: the average stars (one decimal) and the number of ratings, on the profile (Section 10.5). Individual ratings and comments are visible **only to the member rated and to Admin** (A-44).

**BR-16.6** — The average is shown only after the member has at least CFG-28 ratings (default 3, A-45). Before that the profile shows "New member".

**BR-16.7** — The rater's identity is **not hidden** from the rated member on SCR-16.2, because ratings come only from connected members.

**BR-16.8** — **Moderation**: Admin may Hide a comment that is abusive, contains personal details or breaks the guidelines, with a reason (CFG-14). The comment disappears from the rated member's view; the **stars still count** (Section 9.8). The rater is told.

**BR-16.9** — A rated member who thinks a comment is abusive can **report it** to Admin from SCR-16.2.

**BR-16.10** — A rating does not change ranking in matching (BR-12.11 and Section 12.6). It informs the member's own choice only.

**BR-16.11** — Ratings are never deleted. Averages are recalculated from Submitted ratings (stars of Hidden-comment ratings still included).

## 16.6 Status Lifecycle

| From | To | Who |
|---|---|---|
| (new) | Submitted | Member |
| Submitted | Hidden (comment only) | Admin, with reason |

## 16.7 Scenarios

**SC-16.1 — Rate after travelling together**
1. After a few weeks of travelling with Neha, Ashish opens the connection and taps Rate.
2. He gives 5 stars and the comment "Punctual and friendly." Status Submitted; Neha is notified.

**SC-16.2 — Stars only**
1. Member gives 4 stars and leaves the comment empty. The rating is saved.

**SC-16.3 — Rate twice**
1. Ashish tries to rate Neha again for the same connection.
2. System says "You have already rated this connection."

**SC-16.4 — Rating someone never connected**
1. A member tries to rate someone who only appeared as a lead.
2. No Rate option exists. A direct attempt is denied.

**SC-16.5 — New member profile**
1. Neha has 2 ratings.
2. Her profile shows "New member" instead of an average. At the third rating, the average appears.

**SC-16.6 — Abusive comment hidden**
1. Rohit receives a comment with abusive language and reports it.
2. Admin hides the comment with a reason. The stars still count in his average; Rohit no longer sees the text.

**SC-16.7 — Rating after disconnect**
1. Ashish disconnects from Rohit and then rates him.
2. The rating is accepted, because the connection was Accepted at some point.

**SC-16.8 — Phone number in a comment**
1. Member writes "Call 9876543210" in the comment.
2. System blocks the comment: "Please do not enter phone numbers or links."

## 16.8 Acceptance Criteria

**AC-16.1 — Only connected members**
- Given two members were never in an Accepted connection
- When one tries to rate the other
- Then the system denies the action.

**AC-16.2 — One rating per connection**
- Given a member has rated a connection
- When they try to rate it again
- Then the system refuses.

**AC-16.3 — Average display threshold**
- Given CFG-28 is 3 and a member has 2 ratings
- When another member views their profile
- Then no average is shown and the label reads "New member".

**AC-16.4 — Hidden comment keeps stars**
- Given Admin hides a comment
- When the average is recalculated
- Then the stars of that rating still count.

**AC-16.5 — Rating privacy**
- Given a member has received rated comments
- When another member views their profile
- Then only the average and the count are visible, not the comments.

## 16.9 Data Impact

| Event | Records created or changed |
|---|---|
| Rating submitted | Rating created; member's average and count updated; rated member notified |
| Comment hidden | Rating status Hidden; audit entry; rater notified |

## 16.10 Reports Produced

Rating Summary and Low-Rated Members reports (Section 24).

---

# 17. Module M8 — Safety: Block & Report (member side)

## 17.1 Purpose

Give every member two simple tools they can use at any time — **Block** to stop contact immediately and **Report** to ask Admin to review a person — so that a member never has to stay in contact with someone who makes them uncomfortable. Admin handling is in Module M12.

## 17.2 Who Uses It

Any Member. Admin reviews reports (M12).

## 17.3 Screens

| Screen | Purpose |
|---|---|
| SCR-17.1 Block Member | Confirmation, reachable from a lead, a connection or a chat |
| SCR-17.2 Blocked Members | List of members the member has blocked, with **Unblock** |
| SCR-17.3 Report Member | Reason, description and optional "also block" |
| SCR-17.4 My Reports | Reports the member raised and their current status |

An emergency note is shown on SCR-17.3: "RAAH does not provide emergency services. If you are in danger, contact local emergency services."

## 17.4 Block — What Happens

| Effect | Detail |
|---|---|
| Leads | Both members' leads for each other are withdrawn, and they are removed from matching (BR-12.9) |
| Pending requests | Withdrawn in both directions |
| Accepted connection | Becomes Blocked; the chat is Closed |
| Visibility | Neither member sees the other in leads or connection lists. The blocked member is **not told** they were blocked; the closed connection simply shows as ended |
| Ratings | Already given ratings stay |
| Reversal | The blocker can Unblock from SCR-17.2 (A-47) |

## 17.5 Field Specification — Report

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Report Number | Auto | — | SAF-nnnnnn |
| Reported Member | Auto | — | From the screen the member came from |
| Reason | Dropdown | Yes | Harassment / Inappropriate messages / Fake profile or wrong details / Safety concern / Spam / Other (A-46) |
| Description | Text | Yes for Other; otherwise optional | Up to 500 characters |
| Conversation attached | Auto | — | If a chat exists with that member, it is attached with the reporter's knowledge (BR-15.11) |
| Also block this member | Checkbox | No | If ticked, a block is applied (BR-17.1) |
| Status | Auto | — | Section 9.7 |

## 17.6 Business Rules

**BR-17.1** — **Block** is available from a lead, a connection and a chat, and it takes effect **immediately** and in **both directions** (Section 17.4).

**BR-17.2** — The blocked member is **never notified** of the block and cannot see who blocked them.

**BR-17.3** — A member can **Unblock** at any time. After unblocking, matching includes the pair again. Unblocking does not restore a closed connection or chat; a new request is needed.

**BR-17.4** — A member can **report** any member with whom they have a lead, a current or past connection, or an Accepted chat (A-46). A member cannot report someone with whom they have had no contact on the platform.

**BR-17.5** — Before submission the screen tells the reporter that Admin will be able to read the attached conversation (BR-15.11).

**BR-17.6** — A report **does not block or restrict anyone automatically**. It is a request for review. Reporters may tick "also block" to protect themselves immediately.

**BR-17.7** — The **reported member is not told who reported them.** They are told only of the final admin action, if any.

**BR-17.8** — The reporter sees status updates on SCR-17.4 (Submitted / Under Review / Action Taken / Dismissed / Closed) and a notification when the report is decided. They are not told which action was applied to the other member.

**BR-17.9** — **Escalation**: when **CFG-15** (default 3) different members have reported the same member, Admin is alerted and the member is flagged on the Admin dashboard (M12).

**BR-17.10** — A report is **never deleted**. Dismissed and closed reports stay in history, visible to Admin.

**BR-17.11** — A member who raises many reports found to be unfounded may be restricted by Admin (M12); this is an Admin decision, not an automatic rule.

**BR-17.12** — A **rating comment** can also be reported (BR-16.9). It goes to the Admin moderation queue.

## 17.7 Status Lifecycle (Report)

Uses Section 9.7.

| From | To | Who |
|---|---|---|
| (new) | Submitted | Member |
| Submitted | Under Review | Admin |
| Under Review | Action Taken | Admin (Warning, Restriction, Temporary Block, Permanent Block) |
| Under Review | Dismissed | Admin, with reason |
| Action Taken / Dismissed | Closed | Admin |

## 17.8 Scenarios

**SC-17.1 — Block from a lead**
1. Ashish dislikes a profile and taps Block on the lead.
2. The lead disappears from both lists. The other member is not notified.

**SC-17.2 — Block a connected member**
1. Ashish blocks Rohit from the chat header.
2. The connection becomes Blocked, the chat closes, and any pending request between them is withdrawn.

**SC-17.3 — Unblock**
1. Ashish later unblocks Rohit from SCR-17.2.
2. Their routes match again and leads reappear, but there is no connection. A new request is needed.

**SC-17.4 — Report with chat attached**
1. Ashish reports Rohit for "Inappropriate messages", ticks "Also block".
2. The report SAF-000009 is Submitted with the conversation attached. The connection is Blocked immediately.

**SC-17.5 — Report as Other**
1. A member chooses "Other" and leaves the description empty.
2. System requires a description before submitting.

**SC-17.6 — Report on a stranger**
1. A member tries to report someone never seen as a lead or connection.
2. No such option exists. A direct attempt is denied.

**SC-17.7 — Three reporters**
1. Three different members report Rohit within a short time.
2. Admin receives an alert (CFG-15 = 3) and Rohit is flagged on the Admin dashboard.

**SC-17.8 — Reported member told of outcome**
1. Admin issues a Warning to Rohit.
2. Rohit is told he received a warning. He is not told who reported him.

**SC-17.9 — Reporter follows up**
1. Ashish opens SCR-17.4 and sees SAF-000009 as Under Review, later Closed.

## 17.9 Acceptance Criteria

**AC-17.1 — Immediate mutual block**
- Given two members have an Accepted connection
- When one blocks the other
- Then both leads are withdrawn, pending requests are withdrawn, the connection is Blocked and the chat is Closed immediately.

**AC-17.2 — Blocked member not notified**
- Given a member has been blocked
- When they use the app
- Then no message tells them they were blocked.

**AC-17.3 — Report does not block automatically**
- Given a member submits a report without ticking "also block"
- When the report is saved
- Then no block or restriction is applied.

**AC-17.4 — Reporter privacy**
- Given a report against a member
- When the reported member views their notifications
- Then the reporter's identity is not shown.

**AC-17.5 — Escalation threshold**
- Given CFG-15 is 3
- When a third different member reports the same member
- Then Admin is alerted.

## 17.10 Data Impact

| Event | Records created or changed |
|---|---|
| Block | Block record; leads withdrawn; connection Blocked; chat Closed; audit entry |
| Unblock | Block record ended; pair re-matched |
| Report | Safety Report created (SAF); conversation attached if present; Admin notified |

## 17.11 Reports Produced

Safety Report Register and Block Summary (Section 24).

---

# 18. Module M9 — Notifications

## 18.1 Purpose

Tell members and Admin, promptly and briefly, about the events that need their attention — without exposing private details and without being the only way to see anything.

## 18.2 Who Uses It

Every Member and Admin.

## 18.3 Screens

| Screen | Purpose |
|---|---|
| SCR-18.1 Notification Centre | In-app list of notifications with read/unread, filter by type |
| SCR-18.2 Notification Preferences | Switch optional email notifications on or off |

## 18.4 Notification Matrix (default, A-48)

| Event | Who is notified | Channel |
|---|---|---|
| Email verification | New member | Email |
| Email verified, welcome | Member | In-app |
| New lead found | Member | In-app; **daily digest email** of new leads at CFG-31 |
| Connection request received | Receiver | In-app + email |
| Request about to expire (CFG-29 days before) | Receiver | In-app + email |
| Request accepted | Sender | In-app + email |
| Request rejected | Sender | In-app (neutral wording) |
| Request withdrawn by sender | Receiver | In-app |
| Request expired | Sender | In-app |
| Pending request withdrawn by system (route deactivated, member blocked or deactivated) | The other member | In-app |
| New chat message | Recipient | In-app; one email if still unread after CFG-30 hours |
| Disconnected | The other member | In-app |
| Rating received | Rated member | In-app |
| Rating comment hidden | Rater | In-app |
| Report submitted | Reporter | In-app |
| Report decided | Reporter | In-app |
| Warning, Restriction, Temporary Block, Permanent Block | The member concerned | In-app + email |
| Route deactivated by Admin | Member | In-app + email |
| Profile photo removed by Admin | Member | In-app |
| Account reactivated by Admin | Member | Email |
| Terms of Use, Privacy Policy or Safety Guidelines updated and re-acceptance required | All Members | In-app + email |
| Suggested city added or declined | The member who suggested it | In-app |
| Email change verification, password reset | Member | Email |
| Member reaches CFG-15 reports | Admin | In-app + email |
| New report submitted | Admin | In-app (digest by email) |
| Reported rating comment | Admin | In-app |

## 18.5 Business Rules

**BR-18.1** — Notifications are **informational**. No business process depends on a notification being read. Lists such as leads and requests are the authoritative view.

**BR-18.2** — Notifications and emails **never contain** chat text, a mobile number, an email address of another member, or a reason for a rejection or a block. They name the other member and give a link only.

**BR-18.3** — A notification about a rejection uses neutral wording ("Your request was not accepted"). A block is never notified (BR-17.2).

**BR-18.4** — **Email preferences**: a member can switch off the email for *new leads digest* and *unread chat reminder*. Emails for safety, verification, password and account actions cannot be switched off.

**BR-18.5** — New leads are sent as **one daily digest**, not one email per lead, to avoid alert fatigue.

**BR-18.6** — **Expiry reminder**: a receiver is reminded CFG-29 days (default 5) before a Pending request expires.

**BR-18.7** — A **new-message email** is sent at most once per conversation until the recipient next opens it, and only if the message is unread after CFG-30 hours (default 12).

**BR-18.8** — Members can mark notifications as read and filter by type. Notifications older than 90 days are archived, not deleted.

**BR-18.9** — Channels are **In-app and Email**. SMS is not part of Phase 1; the framework is built so SMS can be added later (D-8, CFG-16).

**BR-18.10** — A notification for a deactivated, blocked or Inactive member is not sent, except the email about their own account status.

**BR-18.11** — **Wording of the request notice** follows the business flow: to a Raahi, "*Name* is looking for someone on a route that overlaps with your regular route"; to a Saathi, "*Name* regularly travels a route that overlaps with yours". The notice gives the name and a link only (BR-18.2).

## 18.6 Scenarios

**SC-18.1 — Request received**
1. Ashish sends Neha a request. Neha gets an in-app notification and an email: "Ashish wants to connect with you" with a link.
2. The email has no route details beyond the link.

**SC-18.2 — New leads digest**
1. Five new Raahis match Ashish overnight.
2. At the digest time he gets one email summarising the count, with a link to the lead list.

**SC-18.3 — Expiry reminder**
1. A request is 25 days old (CFG-5 = 30, CFG-29 = 5).
2. Neha gets a reminder that it will expire in 5 days.

**SC-18.4 — Rejection wording**
1. Rohit rejects Ashish's request.
2. Ashish sees "Your request was not accepted." No reason is shown.

**SC-18.5 — Unread chat email**
1. Neha messages Ashish; he does not open it for 12 hours.
2. He receives one email "You have a new message from Neha". Later messages do not send more emails until he opens the chat.

**SC-18.6 — Member turns off digest**
1. Ashish switches off the leads digest email.
2. He still gets in-app notices and all safety and security emails.

**SC-18.7 — Admin alert**
1. A third member reports the same person.
2. Admin gets an in-app and email alert.

**SC-18.8 — Blocked member**
1. Ashish blocks Rohit.
2. Rohit receives no notification of the block.

## 18.7 Acceptance Criteria

**AC-18.1 — No private content in notifications**
- Given a chat message is sent
- When the email or notification is created
- Then it contains the sender's name and a link, never the message text.

**AC-18.2 — Digest, not one per lead**
- Given three new leads are created before the digest time
- When the digest is sent
- Then one email summarises all three.

**AC-18.3 — Mandatory emails**
- Given a member has switched off all optional emails
- When Admin issues a warning
- Then the member still receives the email.

**AC-18.4 — Expiry reminder timing**
- Given CFG-29 is 5 and a request expires on day 30
- When day 25 arrives
- Then the receiver is reminded.

## 18.8 Data Impact

| Event | Records created or changed |
|---|---|
| Any listed event | Notification record per recipient; email queued where the matrix says so |
| Member marks read | Notification status updated |

## 18.9 Reports Produced

Notification Delivery Summary (Section 24).

---

# PART D — ADMIN MODULES

All Admin screens open after login with an Admin account (D-19). Every Admin action is audited with user, time, old value, new value and reason (Section 8).

---

# 19. Module M10 — Member Management & Verification

## 19.1 Purpose

Let Admin find any member, see their status and verification, and take the account-level actions the platform needs: deactivate, reactivate, unlock, verify, correct gender, remove a photo, send a password reset.

## 19.2 Who Uses It

Admin only.

## 19.3 Screens

| Screen | Purpose |
|---|---|
| SCR-19.1 Member List | Search and filter all members |
| SCR-19.2 Member Detail | Everything about one member, with History tab |
| SCR-19.3 Change Member Status | Deactivate or reactivate, with reason |
| SCR-19.4 Account Actions | Unlock, mark email verified, send password reset, correct gender, remove photo |

## 19.4 Member List (SCR-19.1)

| Filter / search | Options |
|---|---|
| Search | Name, email, mobile number, member number (MBR-) |
| Status | Unverified / Active / Inactive / Restricted / Temporarily Blocked / Permanently Blocked |
| Journey role | Saathi / Raahi |
| Verification | Email Verified / Unverified |
| Flags | Locked account · Has open report · Flagged by CFG-15 · Inactive but holding open items |
| Registered | Date range |

Columns: Member number, name, email, role, vehicle, status, verification, registered on, last login, active route (yes/no).

## 19.5 Member Detail (SCR-19.2)

| Block | Content |
|---|---|
| Profile | Name, photo, gender, email, **mobile number (visible to Admin, D-21)**, journey role, vehicle, member since |
| Account | Status, verification, locked flag, failed-login count, last login, login history (RPT-25 extract) |
| Route | Current Active route (both legs) and past routes, read-only (Module M11) |
| Activity counts | Leads, requests sent/received, connections (by status), chats (count only), ratings received (average and count) |
| Safety | Reports against the member (count, status), reports raised by the member, warnings, restrictions, blocks |
| History | Every account action, by whom, when, with reason |

**Admin cannot see** chat content here (BR-4.7) and cannot see a member's password.

## 19.6 Field Specification — Account Action

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Action | Select | Yes | Deactivate / Reactivate / Unlock / Mark email verified / Send password reset / Correct gender / Remove photo |
| Reason | Text | Yes | At least 10 characters (A-54) |
| New gender | Select | For Correct gender | Male / Female / Other |

## 19.7 Business Rules

**BR-19.1** — Every Admin action on a member needs a **reason** and is audited. The member is notified where Section 18.4 says so.

**BR-19.2** — **Deactivate.** The member becomes Inactive and cannot log in. The system then: ends any open session; deactivates the Active route; withdraws all Pending requests (sent and received); sets Accepted connections to Disconnected and closes their chats; withdraws all leads both ways. Nothing is deleted (BR-8.1). The same effects apply as for self-deactivation (BR-10.9).

**BR-19.3** — **Reactivate.** An Inactive member becomes Active again. Their old route stays Inactive and the member must reactivate it (BR-11.15). Closed connections stay closed. The member is told by email.

**BR-19.4** — **Unlock** clears a lock caused by failed logins (BR-5.5) immediately.

**BR-19.5** — **Mark email verified** (A-49): sets the member Active as if they had opened the link, with the reason recorded. It is used only when the verification email could not reach the member.

**BR-19.6** — **Send password reset**: Admin triggers the normal reset email. Admin never sets, sees or receives a password (BR-5.8).

**BR-19.7** — **Correct gender** (A-22, BR-10.3): changes the value, records old and new value and the reason, and applies to filters from the next search.

**BR-19.8** — **Remove photo** (BR-10.5): removes the photo, notifies the member, records the reason.

**BR-19.9** — Admin **cannot edit** a member's name, email, mobile number, route, leads, requests, connections, messages or ratings. These belong to the member (BR-4.5).

**BR-19.10** — Admin **cannot delete** a member. There is no delete option anywhere (BR-8.1).

**BR-19.11** — **Restricting, temporarily blocking and permanently blocking** are done from the safety module (M12), not from this screen, so that each has a safety reason on record.

**BR-19.12** — Admin screens show a member's mobile number and email, but exports and reports mask them unless the Admin confirms "include contact details" (Section 24, report access rules).

**BR-19.13** — Admin accounts are not listed here and cannot be changed in the application. They are managed in the database (D-19).

## 19.8 Status Lifecycle

Member statuses are those of Section 9.1. Who moves them in this module:

| From | To | Who | Condition |
|---|---|---|---|
| Unverified | Active | Admin | Mark email verified, with reason |
| Active / Restricted | Inactive | Admin | Deactivate, with reason |
| Inactive | Active | Admin | Reactivate, with reason |

Restricted and Blocked statuses are moved in M12.

## 19.9 Scenarios

**SC-19.1 — Find a member**
1. Admin types an email fragment in SCR-19.1.
2. The list narrows to that member; Admin opens SCR-19.2 and reviews status, route summary and safety history.

**SC-19.2 — Deactivate a member who left the company**
1. Admin deactivates the member with reason "Requested account closure by email".
2. Route Inactive; pending requests withdrawn; connections Disconnected; chats Closed. The member's history is intact.

**SC-19.3 — Reactivate**
1. The member returns and asks for reactivation.
2. Admin reactivates with a reason. The member can log in; the route is Inactive until the member reactivates it.

**SC-19.4 — Verification email does not arrive**
1. A member writes to Admin that the link never came.
2. Admin first sends a fresh link; if it still fails, Admin marks the email verified with reason "Email delivery failed; identity confirmed by phone call".

**SC-19.5 — Locked account**
1. A member is locked after five wrong passwords and asks for help.
2. Admin unlocks it immediately; the member can try again.

**SC-19.6 — Wrong gender chosen**
1. Member asks for a correction. Admin changes it with reason; audit shows old and new value.

**SC-19.7 — Remove an inappropriate photo**
1. A photo breaks guidelines. Admin removes it with reason; the member is notified.

**SC-19.8 — Contact details in an export**
1. Admin exports the member list. The email and mobile columns are masked.
2. Admin ticks "include contact details" and exports again; the confirmation is logged.

**SC-19.9 — Admin tries to read a chat**
1. Admin opens a member's detail looking for conversations.
2. Only counts appear. Content is available only through an open safety report (M12).

## 19.10 Acceptance Criteria

**AC-19.1 — Reason mandatory**
- Given Admin chooses Deactivate
- When the reason is empty or shorter than 10 characters
- Then the action is not saved.

**AC-19.2 — Deactivation effects**
- Given a member has an Active route, a Pending request and an Accepted connection
- When Admin deactivates the member
- Then the route is Inactive, the request is Withdrawn, the connection is Disconnected and the chat is Closed.

**AC-19.3 — Reactivation does not revive the route**
- Given an Inactive member is reactivated
- When they log in
- Then they have no Active route until they reactivate one.

**AC-19.4 — No member edit**
- Given Admin opens a member's detail
- When Admin looks for a way to change the member's name, route or messages
- Then no such option exists.

**AC-19.5 — Mobile visible to Admin only**
- Given a member has entered a mobile number
- When Admin opens the member detail
- Then the number is visible; for any other member it is not (BR-4.9).

## 19.11 Data Impact

| Action | Records created or changed |
|---|---|
| Deactivate | Member Inactive; route Inactive; requests Withdrawn; connections Disconnected; chats Closed; leads Withdrawn; audit entry; notification |
| Reactivate | Member Active; audit entry; email |
| Mark verified / Unlock / Correct gender / Remove photo | The field concerned; audit entry |

## 19.12 Reports Produced

RPT-1 User & Access Report · RPT-25 Login Audit (Section 24).

---

# 20. Module M11 — Route & Lead Monitoring

## 20.1 Purpose

Let Admin see how well matching is working — which routes find people, which do not, how many leads turn into requests and connections — and spot unusual or unsafe routes, **without ever matching or assigning anyone** (BR-4.6).

## 20.2 Who Uses It

Admin only. Read-only, except that Admin may deactivate a route or hold it Under Review for a safety reason (BR-4.5, M12).

## 20.3 Screens

| Screen | Purpose |
|---|---|
| SCR-20.1 Route Monitor | List of all routes with filters |
| SCR-20.2 Route Detail | One route in full: both legs, versions, leads count, connections |
| SCR-20.3 Lead Monitor | Leads by member or route, match types, conversion |
| SCR-20.4 Connection Monitor | Requests and connections by status, with a **Reported** flag where an open safety report involves the pair; funnel; no chat content |
| SCR-20.5 Matching Health | Where matching works and where it does not |

## 20.4 Route Monitor (SCR-20.1)

| Filter | Options |
|---|---|
| Status | Draft / Active / Inactive / Invalid / Under Review |
| Type | Saathi / Raahi |
| City | Any city of the City Master (matches Start City or End City) |
| Has Return leg | Yes / No |
| Has open report | Yes / No (the route's member is the subject of an open safety report) |
| Leads | None / 1–5 / More than 5 |
| Point text | Contains a stop name (matched with the same fuzzy rule as matching) |
| Last changed | Date range |

Columns: Route number, member, type, status, Going summary (Start → End), Return summary, days, times, leads count, requests, connections, active since, last edited.

## 20.5 Route Detail (SCR-20.2)

| Block | Content |
|---|---|
| Header | Route number, member, Start City → End City, type, vehicle (Raahi), status, version |
| Legs | Going and Return, point by point as typed, days, times |
| Versions | Each saved version with date and what changed |
| Leads | Count by match type (Full Route, Useful Segment) and by leg |
| Connections | Count by status |
| Actions | **Deactivate** (with reason) · **Hold Under Review** (with reason; M12) |

## 20.6 Matching Health (SCR-20.5)

| Block | Meaning |
|---|---|
| Active routes with **no leads** | Routes active for more than N days with zero leads (N set on the screen, default 7), by Saathi/Raahi |
| Leads per Active route | Average and distribution |
| Match type mix | Share of Full Route versus Useful Segment, per leg |
| **Most used stop names** | The stop texts members type most often, grouped by the same fuzzy rule, with variant spellings listed |
| **Unmatched stop names** | Stop names that appear on Active routes but never produce a match, to spot spelling problems or thin coverage |
| Lead → request rate | Share of leads that led to a request |
| Request → accept rate | Share of requests Accepted, Rejected, Expired, Withdrawn |
| Average time to answer a request | From sent to Accepted or Rejected |
| Supply and demand | Count of Active Saathi routes versus Active Raahi routes |
| **By city** | Active routes, leads, lead → request rate and acceptance rate for each city |

## 20.7 Business Rules

**BR-20.1** — Monitoring is **read-only**. Admin cannot create, edit or delete a route, lead, request or connection (BR-4.5, BR-4.6).

**BR-20.2** — Admin can see the **full route** of any member, including all points as typed. It is an Admin-only view and is never exported without the masking rule of BR-19.12.

**BR-20.3** — Admin can **deactivate a route** or **hold it Under Review** only with a reason, and only for a safety or data-integrity cause (BR-4.5). Deactivation has the effects of BR-11.14; Under Review removes the route from matching until released (A-32).

**BR-20.4** — The monitors **never show chat text**. Connection Monitor shows counts and statuses only (BR-4.7).

**BR-20.5** — "Most used stop names" and "Unmatched stop names" are produced with the **same text-similarity rule as matching** (D-22, A-21), so Admin sees what the engine sees.

**BR-20.6** — Figures are live from the records. No number is typed or adjusted by Admin.

**BR-20.7** — A route flagged **Invalid** by the system (a rule broken, for example after a CFG change) appears at the top of the Route Monitor with the broken rule named, so Admin can contact the member if needed. Admin does not fix it.

## 20.8 Scenarios

**SC-20.1 — Routes with no leads**
1. Admin opens Matching Health and sees 14 Active routes with no leads for over 7 days.
2. Admin opens a few. Most end at a stop no other member uses. No action is needed; the list shows where supply is thin.

**SC-20.2 — Spelling variants of one stop**
1. "Most used stop names" shows *Badarpur Metro* with variants *Bdarpur Metro* and *badarpoor metro*.
2. Admin sees that the 70% rule is grouping them and keeps CFG-22 as it is.

**SC-20.3 — Unmatched stop**
1. "Unmatched stop names" shows *Kalka Ji Temple Gate* on three routes with no matches.
2. Admin considers whether CFG-22 should change, tests it in the Test Match tool (M13), and decides.

**SC-20.4 — Funnel check**
1. Connection Monitor shows 120 requests sent this month: 55 Accepted, 20 Rejected, 15 Expired, 5 Withdrawn, 25 Pending.
2. The acceptance rate is shown as 55 of 95 answered requests.

**SC-20.5 — Unsafe route**
1. A report says a route lists a school gate as the Start with an odd pattern.
2. Admin opens the route, holds it Under Review with reason, and handles the report in M12.

**SC-20.6 — Admin looks for manual matching**
1. Admin searches for a way to connect two members directly.
2. No such function exists (BR-4.6).

**SC-20.7 — Route made Invalid by a setting change**
1. CFG-2 is lowered from 5 to 4. A route with 5 points stays valid, as BR-6.2 protects existing routes. A route edited afterwards with 5 points is rejected at save.

## 20.9 Acceptance Criteria

**AC-20.1 — Read-only**
- Given Admin opens any route or lead
- When Admin looks for edit or delete
- Then none exists.

**AC-20.2 — Reason for deactivation**
- Given Admin deactivates a route
- When no reason is entered
- Then the action is rejected.

**AC-20.3 — Under Review excludes matching**
- Given Admin holds a route Under Review
- When matching runs
- Then the route produces no leads.

**AC-20.4 — No chat content**
- Given Admin opens Connection Monitor
- When Admin looks at a connection
- Then only status, dates and counts are shown, never messages.

**AC-20.5 — Same rule as matching**
- Given two stop names are 75% alike
- When "Most used stop names" groups names
- Then they appear as one stop with two variants.

## 20.10 Data Impact

| Action | Records created or changed |
|---|---|
| Deactivate route | Route Inactive; leads and pending requests Withdrawn; audit entry; notification |
| Hold Under Review | Route Under Review; leads withdrawn; audit entry |
| Viewing monitors | None |

## 20.11 Reports Produced

Route Register, Route Coverage, Matching Summary, Lead Conversion, Connection Funnel (Section 24).

---

# 21. Module M12 — Safety, Reports & Blocked Users

## 21.1 Purpose

Handle safety reports from members, apply proportionate action, manage restricted and blocked members, and moderate rating comments — in a way that is fair, recorded and reversible where it should be.

## 21.2 Who Uses It

Admin only.

## 21.3 Screens

| Screen | Purpose |
|---|---|
| SCR-21.1 Report Queue | All reports with age, reason, status, flags |
| SCR-21.2 Report Detail | The report, the attached conversation, the reported member's history |
| SCR-21.3 Take Action | Warning, Restriction, Temporary Block, Permanent Block, Dismiss |
| SCR-21.4 Restricted & Blocked Members | Everyone currently restricted or blocked, with end dates |
| SCR-21.5 Flagged Members | Members with CFG-15 or more reports from different members |
| SCR-21.6 Rating Moderation | Rating comments reported or to be reviewed |

## 21.4 Report Detail (SCR-21.2)

| Block | Content |
|---|---|
| Report | Number (SAF-), date, reason, description, status, age in days |
| Reporter | Name, member number, their report history (counts of raised, dismissed, action taken) |
| Reported member | Name, member number, status, route, ratings, previous warnings, restrictions and blocks, reports from other members |
| Conversation | The chat attached to the report, read-only. **Opening it is logged** (BR-4.7, BR-15.11) |
| Related | The connection between the two members and any other open reports |
| Actions | Under Review · Hold route Under Review · Take Action · Dismiss |

## 21.5 Field Specification — Take Action (SCR-21.3)

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Action | Select | Yes | Warning / Restriction / Temporary Block / Permanent Block / Dismiss report |
| End date | Date | For Restriction (optional) and Temporary Block (mandatory) | Must be in the future |
| Reason | Text | Yes | At least 10 characters (A-54). Visible to Admin only |
| Message to member | Text | No | Optional note included in the notice to the member. It never names the reporter |

## 21.6 What Each Action Does

| Action | Effect on the member | Notice to member | Notice to reporter |
|---|---|---|---|
| **Warning** | None on function. Recorded on the member's history | In-app + email | "Your report has been reviewed" |
| **Restriction** | Status **Restricted**: cannot send connection requests until the end date (or until lifted). Can log in, chat with existing connections and accept requests (A-50) | In-app + email | As above |
| **Temporary Block** | Status **Temporarily Blocked** until the end date. Cannot log in. Route deactivated, requests withdrawn, connections Disconnected and chats Closed. Lifts itself on the end date; the member returns Active with no Active route | In-app + email | As above |
| **Permanent Block** | Status **Permanently Blocked**. Cannot log in. Same closures as a Temporary Block. Cannot be reversed in the application (A-50) | Email | As above |
| **Dismiss** | None | None | "Your report has been reviewed" |

The reporter is told only that the report was reviewed, never which action was taken (BR-17.8).

## 21.7 Business Rules

**BR-21.1** — Admin reviews every report. A report is **never deleted** (BR-17.10). The queue shows oldest first, with those past CFG-32 days (default 3) highlighted (A-53).

**BR-21.2** — **Taking a report to Under Review** is optional but recommended when starting an investigation. It records who started and when.

**BR-21.3** — **Reading the conversation** is allowed only from an open report, is read-only, and **every opening is logged** with Admin and time (BR-4.7).

**BR-21.4** — **Actions are proportionate.** The screen shows the member's previous warnings, restrictions and blocks so that Admin can choose a fitting step. The system does not force a sequence.

**BR-21.5** — Every action needs a **reason** (A-54) and is audited. Warning, Restriction and blocks notify the member (Section 18.4).

**BR-21.6** — **Restriction** removes only the ability to send connection requests (A-50). All other functions continue. With an end date it lifts itself; without one it stays until Admin lifts it. Admin can lift a Restriction early, with reason.

**BR-21.7** — **Temporary Block** needs a future end date. On that date the status returns to Active automatically and the member is told by email. Admin can lift it early, with reason. Closures made during the block (route, connections) are **not** restored (BR-19.3).

**BR-21.8** — **Permanent Block** closes the account in the same way, with no end date. It **cannot be reversed in the application** (A-50). The email address stays registered and cannot be used again (BR-5.3).

**BR-21.9** — **Blocking effects** (Temporary and Permanent) are the same as deactivation (BR-19.2), with the member's status being Blocked, not Inactive.

**BR-21.10** — The **reporter is never named** to the reported member (BR-17.7). Admin screens show the reporter to Admin only.

**BR-21.11** — **Flagged members**: when CFG-15 different members have reported the same member (default 3), the member appears on SCR-21.5 and Admin is alerted (BR-17.9).

**BR-21.12** — **Dismiss** needs a reason. A dismissed report keeps its record. The number of dismissed reports per reporter is shown on SCR-21.2, so that repeated unfounded reports can be seen; any step against such a reporter is an Admin decision using the same actions (BR-17.11).

**BR-21.13** — **Rating moderation**: Admin can **Hide** a rating comment with a reason. The stars remain counted (BR-16.8, BR-16.11). The rater is told.

**BR-21.14** — **Photo removal** and **gender correction** are done in M10 and may be triggered from a report.

**BR-21.15** — A report about a member who is **already Inactive or Blocked** can still be reviewed and closed. No further functional action is needed.

**BR-21.16** — Admin cannot create a report on a member's behalf. Reports come only from members (A-46). Admin acts on their own observation through Account Actions (M10) or Route holds (M11).

## 21.8 Status Lifecycle

Report statuses are those of Section 9.7.

| From | To | Who | Condition |
|---|---|---|---|
| Submitted | Under Review | Admin | Investigation started |
| Under Review | Action Taken | Admin | Warning, Restriction, Temporary Block or Permanent Block applied |
| Under Review | Dismissed | Admin | With reason |
| Action Taken / Dismissed | Closed | Admin | Final |

Member status changes made here:

| From | To | Who | Condition |
|---|---|---|---|
| Active | Restricted | Admin | Restriction |
| Restricted | Active | Admin / System | Lifted, or end date reached |
| Active / Restricted | Temporarily Blocked | Admin | Temporary Block with end date |
| Temporarily Blocked | Active | System / Admin | End date reached, or lifted early |
| Active / Restricted / Temporarily Blocked | Permanently Blocked | Admin | Permanent Block |

## 21.9 Scenarios

**SC-21.1 — Warning**
1. Ashish reports Rohit for "Inappropriate messages". Admin reads the attached chat and sees a mildly offensive remark.
2. Admin issues a Warning with a note. Rohit is told he received a warning; Ashish is told the report was reviewed. Rohit's status and connections do not change.

**SC-21.2 — Restriction**
1. A member repeatedly sends requests and is reported as spam by several members.
2. Admin applies a Restriction for 30 days. The member cannot send requests but can still chat and respond to requests. It lifts on its end date.

**SC-21.3 — Temporary Block**
1. Admin finds clear harassment. A Temporary Block for 14 days is applied.
2. Rohit cannot log in; his route is Inactive; connections are Disconnected. After 14 days he can log in, with no Active route.

**SC-21.4 — Permanent Block**
1. Admin confirms a fake profile used to harass others and applies a Permanent Block.
2. The account is closed for good; the email cannot register again.

**SC-21.5 — Dismiss**
1. Admin reads the chat and finds nothing wrong. Dismisses with reason "No violation found".
2. The reporter is told the report was reviewed. The reported member is not told anything.

**SC-21.6 — Three reporters**
1. Three different members report Rohit.
2. Admin sees an alert and Rohit on SCR-21.5; opens the three reports together.

**SC-21.7 — Hold the route during an investigation**
1. While investigating, Admin holds the reported member's route Under Review so that no new leads form.
2. After the decision, Admin releases or deactivates it.

**SC-21.8 — Lift a restriction early**
1. A member apologises; Admin lifts the Restriction with reason. The member can send requests again.

**SC-21.9 — Overdue report**
1. A report is 4 days old (CFG-32 = 3). It is highlighted on the Admin dashboard.

**SC-21.10 — Abusive rating comment**
1. A member reports a comment on SCR-16.2. Admin hides it with reason in Rating Moderation. Stars still count.

**SC-21.11 — Reporter with many dismissed reports**
1. SCR-21.2 shows that the reporter has five dismissed reports. Admin takes this into account when deciding.

## 21.10 Acceptance Criteria

**AC-21.1 — Conversation access is logged**
- Given Admin opens a report with an attached chat
- When Admin reads the conversation
- Then the access is written to the audit log.

**AC-21.2 — Temporary Block lifts itself**
- Given a Temporary Block with an end date
- When the end date arrives
- Then the member's status becomes Active and they are notified.

**AC-21.3 — Restriction scope**
- Given a member is Restricted
- When they try to send a connection request
- Then it is refused, and all other functions still work.

**AC-21.4 — Block effects**
- Given a member has an Active route, a Pending request and an Accepted connection
- When Admin applies a Temporary Block
- Then the route is Inactive, the request Withdrawn, the connection Disconnected and the chat Closed.

**AC-21.5 — Reporter hidden**
- Given Admin has issued a Warning
- When the reported member reads the notice
- Then it does not name the reporter.

**AC-21.6 — Permanent Block is final**
- Given a member is Permanently Blocked
- When Admin looks for a way to reverse it
- Then no such option exists in the application.

**AC-21.7 — Reason required**
- Given Admin chooses any action
- When the reason is shorter than 10 characters
- Then the action is not saved.

## 21.11 Data Impact

| Action | Records created or changed |
|---|---|
| Under Review | Report status; audit entry |
| Warning | Warning recorded on member; report Action Taken; notices |
| Restriction | Member Restricted; end date; notices |
| Temporary / Permanent Block | Member Blocked; route Inactive; requests Withdrawn; connections Disconnected; chats Closed; leads Withdrawn; notices |
| Dismiss | Report Dismissed; reason recorded |
| Hide comment | Rating status Hidden; rater notified |

## 21.12 Reports Produced

Safety Report Register, Action Summary, Block & Restriction Register, Flagged Members, Rating Moderation Log (Section 24).

---

# 22. Module M13 — Matching Configuration & Platform Settings

## 22.1 Purpose

Give Admin one place to tune how the platform behaves — matching tolerance, limits, expiry, notification timing, security — without any code change, and to keep a complete record of every change (Principle 15).

## 22.2 Who Uses It

Admin only.

## 22.3 Screens

| Screen | Purpose |
|---|---|
| SCR-22.1 Configuration | All settings, grouped, with current value, allowed range and default |
| SCR-22.2 Change History | Every change: setting, old value, new value, who, when, remark |
| SCR-22.3 Test Match | Enter two sample legs and see the result under the current settings (A-51) |
| SCR-22.4 Legal Content | Edit, preview and publish the Terms of Use, Privacy Policy and Safety Guidelines; version history (D-24) |
| SCR-22.5 City Master | Add, edit and deactivate cities (D-26) |
| SCR-22.6 City Requests | Cities suggested by members, to add or decline (A-56) |

## 22.4 Settings by Group, with Allowed Range

All settings are those of Section 6. The allowed range prevents unsafe values.

| Group | Setting | Default | Allowed range |
|---|---|---|---|
| **Matching** | CFG-1 Departure time tolerance (minutes) | 30 | 0 to 120 |
| | CFG-2 Maximum points per leg | 5 | 3 to 10 |
| | CFG-3 Minimum points per leg | 2 | Fixed at 2 |
| | CFG-4 Minimum shared points for a Useful Segment | 2 | 2 to CFG-2 |
| | CFG-22 Text match threshold (%) | 70 | 50 to 100 |
| | CFG-7 Leads shown per page | 20 | 5 to 100 |
| | CFG-18 Travel days allowed | Mon–Sun | Any subset, at least one |
| | CFG-19 Return leg mandatory | No | Yes / No |
| | CFG-12 Gender filter enabled | Yes | Yes / No |
| | CFG-20 Leg filter enabled | Yes | Yes / No |
| **Requests & connections** | CFG-5 Request expiry (days) | 30 | 1 to 365 |
| | CFG-6 Maximum pending requests per member | 10 | 1 to 100 |
| | CFG-17 Cool-off after rejection (days) | 7 | 0 to 90 |
| | CFG-25 Request message text | Hello, I want to connect with you | 1 to 150 characters, no links or 10-digit runs |
| **Chat & rating** | CFG-26 Maximum chat message length | 1000 | 100 to 5000 |
| | CFG-13 Rating enabled | Yes | Yes / No |
| | CFG-14 Rating comment moderation | Yes | Yes / No |
| | CFG-27 Maximum rating comment length | 300 | 50 to 1000 |
| | CFG-28 Minimum ratings before average shows | 3 | 1 to 20 |
| **Safety** | CFG-15 Reports that trigger an alert | 3 | 1 to 20 |
| | CFG-32 Report overdue after (days) | 3 | 1 to 30 |
| **Notifications** | CFG-16 Channels | In-app + Email | In-app is always on |
| | CFG-29 Expiry reminder (days before) | 5 | 1 to CFG-5 minus 1 |
| | CFG-30 Unread chat email delay (hours) | 12 | 1 to 72 |
| | CFG-31 Daily digest time | 07:00 | Any time |
| **Security & content** | CFG-8 Session idle timeout (minutes) | 30 | 5 to 240 |
| | CFG-9 Maximum failed logins | 5 | 3 to 10 |
| | CFG-10 Account lock duration (minutes) | 15 | 1 to 1440 |
| | CFG-11 Link validity (hours) | 24 | 1 to 168 |
| | CFG-23 Email resend cooldown (seconds) | 60 | 10 to 600 |
| | CFG-24 Maximum photo size (MB) | 5 | 1 to 10 |

## 22.5 Test Match Tool (SCR-22.3)

| Field | Content |
|---|---|
| Sample Saathi leg | Points (Start, up to 3 in between, End), days, time |
| Sample Raahi leg | The same |
| Cities | Start City and End City of each sample route, to confirm that routes sharing no city never match |
| Result | Pass/fail of each step (days, time, points paired with their similarity %, direction, shared points), then **Full Route**, **Useful Segment** or **No match** |

The tool uses only the sample text Admin types. It reads no member data, creates no lead and has no effect on members.

## 22.5.1 Legal Content (SCR-22.4)

Three fixed documents are managed here: **Terms of Use**, **Privacy Policy** and **Safety Guidelines**. A sample text for each is supplied with the product (Appendix B) so that the client can edit and publish the final wording later (D-24).

| Field | Type | Rules |
|---|---|---|
| Document | Fixed | Terms of Use / Privacy Policy / Safety Guidelines |
| Title | Text | Up to 150 characters |
| Body | Formatted text | Headings, paragraphs, lists and links only |
| Version | Auto | 1, 2, 3 … The supplied sample is labelled **Sample 1** |
| Effective date | Date | Not earlier than today |
| Requires re-acceptance | Yes / No | Yes forces every member to accept at next login |
| Status | Auto | Draft / Published |
| Published by / on | Auto | Admin and time |

## 22.5.2 City Master (SCR-22.5, SCR-22.6)

Cities are set up by Admin and chosen by members on their route (D-26). There is no fixed city list in the product; Admin creates the cities needed for launch (Section 31).

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| City Name | Text | Yes | 2 to 60 characters. Unique, **ignoring capital letters and spaces** ("New Delhi", "new delhi" and "NewDelhi" are the same) |
| State | Text | No | For clarity in the list |
| Status | Toggle | Yes | Active / Inactive. Default Active |

**City requests (A-56):** a member whose city is not listed sends the city name from "My city is not listed". Admin sees it on SCR-22.6 with member, name and date, and either **Adds** it (creating a city and telling the member in-app) or **Declines** it with a reason (the member is told in-app).

## 22.6 Business Rules

**BR-22.1** — Every change is logged with old value, new value, user, time and an optional remark (BR-6.1). It appears in the Configuration Change report (RPT-26).

**BR-22.2** — A change takes effect for **new activity from that moment**. Completed records are never rewritten (Section 6).

**BR-22.3** — **Matching settings** (CFG-1, CFG-4, CFG-22) change leads only as routes are activated, edited or reactivated afterwards. Existing leads are not withdrawn (BR-6.3), unless Admin runs **Recalculate all leads** (BR-22.4).

**BR-22.4** — **Recalculate all leads** (A-52): Admin confirms a warning that some leads may be withdrawn and some created. The system re-runs matching for every Active route. Pending requests and connections are **not** affected (A-28). The run is logged with the number of leads created and withdrawn.

**BR-22.5** — A value outside its **allowed range** (Section 22.4) is refused with the range shown.

**BR-22.6** — **CFG-2 lowered** does not invalidate existing routes (BR-6.2). **CFG-19 switched to Yes** does not force existing Active routes to add a Return leg until they are next edited (SC-11.13).

**BR-22.7** — **CFG-25** (request message text) applies to requests sent after the change. Requests already sent keep their message.

**BR-22.8** — **CFG-5 shortened** applies from the next daily expiry check to requests whose sent date plus the new value has already passed. Those requests expire at that run, and their senders are notified.

**BR-22.9** — Security settings (CFG-8 to CFG-11, CFG-23) are changed with extra confirmation ("Are you sure?"), because they affect every member at once.

**BR-22.10** — Settings cannot be **deleted**. Admin can only change their value within range (BR-8.1).

**BR-22.11** — Only Admin can open this module. A Member requesting its address gets "access denied", logged (BR-4.2).

**BR-22.12** — **Legal Content** (D-24): Admin edits the three documents on SCR-22.4. Editing creates a new **Draft**; nothing changes for members until Admin **Publishes** it, with a confirmation and an optional remark. Only one version of each document is Published at a time; earlier versions stay readable by Admin and are never deleted (BR-8.1).

**BR-22.13** — **Sample text.** The product is supplied with sample text for all three documents, published as "Sample 1", so that registration can be tested. While any of the three is still a sample version, the Admin Dashboard shows the alert "Legal text is still the sample", and the go-live check (Section 31.5) fails.

**BR-22.14** — The published texts are **public**: visible without login on the registration screen and the login screen, and from My Account for members.

**BR-22.15** — **Acceptance is recorded** per member, per document, with version and time (BR-5.13). Records cannot be edited or deleted, even by Admin.

**BR-22.16** — **Re-acceptance.** When a version is published with *Requires re-acceptance = Yes*, every member must accept it at their next login. Until they do, they can read the texts and log out, but cannot use the product. Members are notified in-app and by email (Section 18.4). Admin can see how many members have not yet accepted (RPT-29). A change published with *No* (for example a typo fix) needs no re-acceptance.

**BR-22.17** — Admin cannot change what a member accepted in the past. A new version never rewrites an old acceptance record.

**BR-22.18** — **City Master** (D-26): only Admin creates and edits cities. A duplicate name (ignoring capital letters and spaces) is refused.

**BR-22.19** — A city **cannot be deactivated** while any Draft or Active route uses it as a Start City or End City. The system lists those routes. An inactive city is not offered on new routes; Inactive routes keep their city for history.

**BR-22.20** — A city is **never deleted** (BR-8.1) and its name can be corrected. A correction changes the name everywhere, because routes point to the city, not to its text.

**BR-22.21** — **City requests** (A-56) cannot be raised for a name already in the list. A member can have only one open request per city name. A declined request carries a reason shown to the member.

**BR-22.22** — Admin can see, per city, the number of Active routes and members (RPT-30).

## 22.7 Scenarios

**SC-22.1 — Widen the time tolerance**
1. Admin raises CFG-1 from 30 to 45 minutes after pilot feedback.
2. The change is logged. New route activations and edits use 45; existing leads stay.

**SC-22.2 — Tune the text threshold with the test tool**
1. Admin sees some missed matches caused by spelling. He opens Test Match and enters *Nehru Place* and *Nehru Plaza Mkt*.
2. The tool shows the two points are 60% alike, so they do not pair at CFG-22 = 70. Lowering the threshold to 60 would pair them, but they may be two different places, so he sees that a lower value also risks false matches. He decides to keep 70.

**SC-22.3 — Recalculate everything**
1. After lowering CFG-22 to 65, Admin clicks Recalculate all leads and confirms.
2. 340 leads are created and 12 are withdrawn. Pending requests and connections are untouched. The run is logged.

**SC-22.4 — Value out of range**
1. Admin types 150 for CFG-1.
2. The system refuses: "Allowed range is 0 to 120."

**SC-22.5 — Make Return mandatory**
1. Admin sets CFG-19 to Yes.
2. New routes need a Return leg to activate. Existing Active routes continue until edited.

**SC-22.6 — Change the request message**
1. Admin changes CFG-25 to "Hi, I would like to connect."
2. New requests carry it; older requests still show the original.

**SC-22.7 — Shorten request expiry**
1. Admin changes CFG-5 from 30 to 20.
2. At the next daily run, requests already older than 20 days expire and their senders are notified.

**SC-22.8 — Review the history**
1. Admin opens Change History and sees that CFG-4 was changed from 2 to 3 yesterday, by whom, with a remark.

**SC-22.9 — Replace the sample text**
1. Admin opens SCR-22.4, edits the sample Terms of Use with the client's wording and publishes it as version 1 with a remark. The same is done for the other two documents.
2. The alert "Legal text is still the sample" disappears from the dashboard.

**SC-22.10 — Typo fix**
1. Admin corrects a spelling mistake in the Privacy Policy and publishes it with *Requires re-acceptance = No*. Members are not interrupted.

**SC-22.11 — Material change**
1. Admin changes a clause of the Terms of Use and publishes it with *Requires re-acceptance = Yes*.
2. Members are notified; each must accept at next login. RPT-29 shows how many have accepted.

**SC-22.12 — Public reading**
1. A visitor opens the registration screen and reads all three texts without logging in.

**SC-22.13 — Add launch cities**
1. Before go-live Admin adds Delhi, Noida and Gurugram on SCR-22.5. Members can now choose them on a route.

**SC-22.14 — City suggested by a member**
1. A member in Jaipur sends "Jaipur" from "My city is not listed". Admin adds it on SCR-22.6 and the member is told.

**SC-22.15 — Deactivate a city in use**
1. Admin tries to deactivate Noida while 40 routes use it. The system refuses and lists them.

**SC-22.16 — Duplicate city name**
1. Admin types "new delhi" when "New Delhi" exists. The system refuses it as a duplicate.

## 22.8 Acceptance Criteria

**AC-22.1 — Change logged**
- Given Admin changes CFG-1
- When the change is saved
- Then the history shows old value, new value, user, time and remark.

**AC-22.2 — Range enforced**
- Given the allowed range of CFG-1 is 0 to 120
- When Admin enters 150
- Then the change is refused.

**AC-22.3 — Existing leads not withdrawn on a normal change**
- Given CFG-22 is lowered
- When no recalculation is run
- Then existing leads remain.

**AC-22.4 — Recalculate leaves requests alone**
- Given Admin runs Recalculate all leads
- When it completes
- Then Pending requests and Accepted connections are unchanged and the counts of leads created and withdrawn are logged.

**AC-22.5 — Test Match uses no member data**
- Given Admin uses the Test Match tool
- When a result is shown
- Then no lead is created and no member is affected.

**AC-22.6 — Access control**
- Given a Member is logged in
- When they try to open the configuration address
- Then access is denied and logged.

**AC-22.7 — Sample alert**
- Given a legal document is still the supplied sample
- When Admin opens the dashboard
- Then the alert "Legal text is still the sample" is shown.

**AC-22.8 — Publish replaces one version only**
- Given a version of a document is Published
- When Admin publishes a new version
- Then the new version is the only Published one and the old one remains in history.

**AC-22.9 — Re-acceptance gate**
- Given a version was published with Requires re-acceptance = Yes
- When a member who has not accepted it logs in
- Then they cannot use the product until they accept.

**AC-22.10 — City duplicate**
- Given the city "New Delhi" exists
- When Admin adds "newdelhi"
- Then the system refuses it as a duplicate.

**AC-22.11 — City in use**
- Given an Active route uses a city
- When Admin tries to deactivate that city
- Then the action is refused and the routes are listed.

## 22.9 Data Impact

| Action | Records created or changed |
|---|---|
| Change a setting | Setting value updated; change-history entry |
| Recalculate all leads | Leads created, updated or withdrawn; a log entry with counts |
| Test Match | None |
| Add / edit / deactivate a city | City record changed; audit entry; member told if it came from a request |
| Publish a legal text | New version Published; previous version kept; notifications if re-acceptance is required; audit entry |
| Member accepts | Acceptance record (member, document, version, time) |

## 22.10 Reports Produced

RPT-26 Configuration Change Report · RPT-29 Legal Acceptance Report · RPT-30 City Register (Section 24).

---

# PART E — OUTPUT & CROSS-CUTTING

---

# 23. Module M14 — Dashboards

## 23.1 Purpose

Give each role one landing screen built around the decisions that role has to make today. Both dashboards are **live**: every number comes from the records, none is typed.

## 23.2 Who Uses It

Every Member (Member Dashboard) and Admin (Admin Dashboard). The dashboard is the first screen after login (BR-5.11).

## 23.3 Screens

| Screen | Purpose |
|---|---|
| SCR-23.1 Member Dashboard | The member's own work queue and status |
| SCR-23.2 Admin Dashboard | Platform health, matching health, safety queue and alerts |

## 23.4 Member Dashboard (SCR-23.1)

| Block | Content | Why it is there |
|---|---|---|
| **Setup prompt** (shown only when needed) | "Verify your email" if Unverified · "Create your route" if no route · "Add your Return leg" if Return is missing and a Return leg would add leads · "Add a photo" if none | Gets a new member to a working route quickly |
| **My Route** | Journey role (Saathi / Raahi), status, Going summary (Start → End, days, time), Return summary, vehicle (Raahi), buttons Edit / Deactivate / Reactivate | The member's single active commitment on the platform |
| **New Leads** | Count of new leads (split Going / Return), top three leads with a Connect button, link "See all" | The reason the member came |
| **Requests Received** | Pending requests with sender, time left before expiry, Accept / Reject | Answers owed to others |
| **Requests Sent** | Pending requests with how long each has waited, Withdraw | What the member is waiting for |
| **My Connections** | Number of connections, conversations with unread messages | Where the journey is actually decided |
| **Notifications** | Latest five | Quick catch-up |
| **Safety** | Links to Blocked Members and My Reports | Safety is always one tap away |

Wording is role-aware: a Saathi sees "Raahis for you", a Raahi sees "Saathis for you".

## 23.5 Admin Dashboard (SCR-23.2)

A period selector (Today / 7 days / 30 days) applies to activity counts.

| Block | Content | Drill-down |
|---|---|---|
| **Members** | Total · Active · Unverified · new in period · Inactive · Restricted · Temporarily / Permanently Blocked | Member List filtered |
| **Routes** | Active Saathi routes · Active Raahi routes · with Return leg · Invalid · Under Review · Active routes by city (top five) | Route Monitor filtered |
| **Matching** | Leads in period · Full Route vs Useful Segment · Active routes with no leads · lead → request rate | Matching Health |
| **Connections** | Pending · Accepted · Rejected · Expired · Withdrawn · Disconnected · acceptance rate | Connection Monitor |
| **Safety** | Open reports · **overdue** (past CFG-32 days) · flagged members (CFG-15) · Restricted · Blocked | Report Queue, Flagged Members |
| **Moderation** | Rating comments awaiting review | Rating Moderation |
| **Alerts** | Locked accounts · Invalid routes · members at the report threshold · failed-login spikes · "Legal text is still the sample" | Relevant list |
| **Quick links** | Matching Configuration · Notifications settings · Platform Settings and Legal Content (M13) | — |

## 23.6 Business Rules

**BR-23.1** — A dashboard shows only what the logged-in role may see. A Member Dashboard never shows another member's data or platform totals (BR-4.1).

**BR-23.2** — Every figure is a **live count** from the records. Nobody can type or adjust a number.

**BR-23.3** — Every Admin block **drills down** to the list behind the number.

**BR-23.4** — Dashboards never show chat content, mobile numbers or emails of other members. Admin sees contact details only on the Member Detail screen (BR-19.12).

**BR-23.5** — Setup prompts on the Member Dashboard disappear as soon as the condition is met, and never block use of the rest of the dashboard.

**BR-23.6** — The dashboard remains usable when a block has no data: each block shows a clear empty state, not an error.

## 23.7 Scenarios

**SC-23.1 — New member's first dashboard**
1. A member verifies email and logs in. The Setup prompt says "Create your route". My Route, New Leads and Requests blocks show empty states.

**SC-23.2 — Saathi with leads and a request to answer**
1. Ashish sees "5 new leads (3 Going, 2 Return)", the top three leads with Connect buttons, and one request from a Raahi expiring in 4 days.

**SC-23.3 — Raahi after deactivating**
1. Amit deactivates his route. My Route shows Inactive with a Reactivate button; New Leads shows an empty state; his connections and unread messages still show.

**SC-23.4 — Admin morning check**
1. Admin opens the dashboard. Safety shows 2 overdue reports, 1 flagged member and 3 rating comments awaiting review. Admin drills into the overdue reports first.

**SC-23.5 — Matching health at a glance**
1. Admin sees 30 Active routes with no leads and drills into Matching Health to see whether the cause is thin coverage or spelling (Module M11).

**SC-23.6 — Unverified member**
1. A member who has not verified sees only the "Verify your email" prompt with a Resend button, and no route functions (BR-5.1).

## 23.8 Acceptance Criteria

**AC-23.1 — Role separation**
- Given a Member is logged in
- When the dashboard opens
- Then only that member's own data is shown, with no platform totals.

**AC-23.2 — Live counts and drill-down**
- Given a new report is submitted
- When Admin refreshes the dashboard
- Then the Open reports count has increased and clicking it opens the report queue.

**AC-23.3 — Overdue reports**
- Given CFG-32 is 3 and a report is 4 days old and not decided
- When Admin opens the dashboard
- Then the report is counted as overdue.

**AC-23.4 — Setup prompt**
- Given a member has no route
- When they open the dashboard
- Then the prompt "Create your route" is shown and disappears once a route exists.

## 23.9 Data Impact

None. Dashboards only read.

## 23.10 Reports Produced

None of its own; drill-downs open the reports of Section 24.

---

# 24. Module M15 — Reports Catalogue

## 24.1 Common Report Behaviour

| Capability | Detail |
|---|---|
| Who can run reports | **Admin only.** Members do not have a reports module in Phase 1; they use their own screens (leads, requests, connections, ratings, reports) |
| Filters | Date range, status, journey role, member, route type, leg, reason, whichever apply |
| Export | Excel and PDF |
| Print | Print-friendly layout |
| Drill-down | Clicking any figure opens the records behind it |
| Saved filters | A filter combination can be saved and re-run |
| Columns | Show/hide, with the choice remembered |
| Contact details | Email and mobile columns are **masked** in exports unless Admin confirms "include contact details"; the confirmation is logged (BR-19.12) |
| Chat content | **Never** included in any report. Only counts are reported (BR-4.7) |
| Scheduling / emailing | Out of Phase 1 scope |

## 24.2 Member Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-1 | User & Access Report | Who is on the platform and in what state | Member no., name, role, vehicle, status, verification, registered on, last login, locked flag |
| RPT-2 | Registration & Verification Funnel | How many registered members become useful | Registered, verified, with a route, with a lead, with a request, with a connection; per period |

## 24.3 Route Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-3 | Route Register | Every route and its state | Route no., member, Start City, End City, type, status, Going Start → End, Return Start → End, days, times, versions, activated on |
| RPT-4 | Route Coverage | Where members travel and how people spell it, per city | City, stop name groups (same fuzzy rule as matching), variants, number of routes, number matched, **unmatched stop names** |
| RPT-5 | Supply & Demand | Balance of Saathi and Raahi routes | Active Saathi routes, Active Raahi routes, with Return leg, by city, by time band and by common Start/End stop |

## 24.4 Matching & Lead Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-6 | Matching Summary | How well matching performs | Leads created, Full Route vs Useful Segment per leg, average leads per Active route, both-legs leads |
| RPT-7 | Routes Without Leads | Routes that find no one | Route no., member, type, active since, days with no leads, Start/End |
| RPT-8 | Lead Register | Every lead and its fate | Lead no., for member, matched member, legs matched, match type per leg, status, created / last calculated |
| RPT-9 | Lead Conversion | From lead to request to connection | Leads, viewed, dismissed, requested, accepted; rates; by role and by match type |

## 24.5 Connection Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-10 | Connection Register | Every request and connection | Connection no., sender, receiver, sent on, status, responded on, ended on and by |
| RPT-11 | Connection Funnel | Outcome of requests | Sent, Accepted, Rejected, Expired, Withdrawn (by sender / by system), Disconnected, Blocked; acceptance rate |
| RPT-12 | Request Response Time | How fast requests are answered | Connection no., sent, answered, hours to answer; average and median by role |

## 24.6 Chat, Rating & Notification Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-13 | Chat Activity Summary | Whether connections turn into conversation (counts only) | Conversations opened, with at least one reply, messages per conversation, closed chats |
| RPT-14 | Rating Summary | Ratings per member | Member, ratings count, average, distribution 1–5 |
| RPT-15 | Low-Rated Members | Members who may need attention | Member, ratings count, average below a chosen level, reports against |
| RPT-16 | Rating Moderation Log | Hidden comments | Rating no., date, rater, rated member, reason, Admin |
| RPT-23 | Notification Delivery Summary | Whether notices and emails go out | Event type, created, sent, failed, opened (in-app) |

## 24.7 Safety Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-17 | Safety Report Register | Every report | SAF no., date, reporter, reported member, reason, status, action, closed on |
| RPT-18 | Overdue Reports | Reports not decided in time | SAF no., age in days, status, reason |
| RPT-19 | Action Summary | What actions are being taken | Warning, Restriction, Temporary Block, Permanent Block, Dismissed; counts by period and reason |
| RPT-20 | Block & Restriction Register | Everyone currently restricted or blocked | Member, status, since, end date, reason, by Admin |
| RPT-21 | Flagged Members | Members at or above the report threshold | Member, number of different reporters, reasons, current status |
| RPT-22 | Member Block Summary | How often members block each other | Blocks by period; members blocked most often (counts only) |

## 24.8 System & Audit Reports

| ID | Report | Purpose | Key columns |
|---|---|---|---|
| RPT-24 | Audit Trail Report | Who changed what | Date, user, role, record, action, old value, new value, reason |
| RPT-25 | Login Audit | Access history | Member, login time, device, IP, success/failure |
| RPT-26 | Configuration Change Report | Changes to settings | Setting, old value, new value, changed by, date, remark |
| RPT-27 | Chat Access Log | Every time Admin opened a reported chat | Date, Admin, report no., connection no. |
| RPT-28 | Recalculation Log | Runs of "Recalculate all leads" | Date, Admin, leads created, withdrawn, routes processed |
| RPT-29 | Legal Acceptance Report | Who accepted which version of the Terms, Privacy Policy and Safety Guidelines | Member, document, version, accepted on; members yet to accept the current version |
| RPT-30 | City Register | Cities available and how much they are used | City, state, status, Active routes, members, created on |

## 24.9 Report Access by Role

| Report group | Admin | Member |
|---|---|---|
| Member, Route, Matching & Lead, Connection | Full | — |
| Chat, Rating, Notification | Full (no chat content) | — |
| Safety | Full | — (members see only their own reports on SCR-17.4) |
| System & Audit | Full | — |

## 24.10 Business Rules

**BR-24.1** — Reports are **read-only** and always computed from current records. Nothing in a report can be edited.

**BR-24.2** — No report ever shows chat text (BR-4.7). Reports of chat activity show counts only.

**BR-24.3** — Exports mask email and mobile unless Admin ticks "include contact details" (BR-19.12); the confirmation is written to the audit trail.

**BR-24.4** — The fuzzy grouping used in RPT-4 is the **same** text-similarity rule as matching (D-22, A-21), so Admin sees what the engine sees.

**BR-24.5** — Any report that counts members counts each member once, even if they have several routes in the period.

**BR-24.6** — Report dates follow DD-MMM-YYYY and the platform time zone (Section 27).

**BR-24.7** — A member cannot view or export another member's data through any report; Members have no report access (Section 24.9).

## 24.11 Scenarios

**SC-24.1 — Weekly health check**
1. Admin runs RPT-9 Lead Conversion for the last 7 days: 400 leads, 90 requests, 48 accepted.

**SC-24.2 — Find spelling problems**
1. Admin opens RPT-4 and sees "Kalkaji" with variants "Kalka Ji" and "kalkajee", and "Sai Mandir Gate" listed as unmatched on four routes.

**SC-24.3 — Export with contact details**
1. Admin exports RPT-1. The email and mobile columns are masked. Admin ticks "include contact details" and re-exports; the audit trail records it.

**SC-24.4 — Which chats did Admin open**
1. Admin runs RPT-27 and sees every access to a reported chat, by date and report number.

**SC-24.5 — Members who need attention**
1. Admin runs RPT-15 with "average below 3.0" and cross-checks RPT-21 Flagged Members.

## 24.12 Acceptance Criteria

**AC-24.1 — Admin only**
- Given a Member is logged in
- When they try to open a report address
- Then access is denied and logged.

**AC-24.2 — No chat content**
- Given a report about chat activity
- When it is generated
- Then it contains counts only and no message text.

**AC-24.3 — Masked export**
- Given Admin exports RPT-1 without ticking "include contact details"
- When the file is opened
- Then email and mobile columns are masked.

**AC-24.4 — Drill-down**
- Given a figure in RPT-11
- When Admin clicks it
- Then the underlying connections are listed.

## 24.13 Data Impact

None, except that an export with contact details writes an audit entry.

---

# 25. Consolidated Validation Rules

Every validation enforced by the system, in one list, for test-case preparation. Each refers back to the rule that defines it.

## 25.1 Registration, Login & Profile

| # | Rule | Source |
|---|---|---|
| V-1 | Email is unique across all members, including inactive and blocked | BR-5.3 |
| V-2 | Password is at least 8 characters with a letter and a number; Confirm Password must match | Section 5.2 |
| V-3 | Full name is 3 to 100 characters | Section 5.2 |
| V-4 | Journey role (Saathi / Raahi) is selected at registration | D-18 |
| V-5 | A Raahi must choose a Vehicle (Bike / Car) | BR-5.12 |
| V-6 | Gender is mandatory (Male / Female / Other) | A-4 |
| V-7 | Mobile number, if given, is 10 digits | Section 5.2 |
| V-8 | Terms of Use, Privacy Policy and Safety Guidelines must be accepted; the version and time are recorded | BR-5.13 |
| V-9 | Verification and reset links expire after CFG-11 hours and are single-use | BR-5.2, BR-5.8 |
| V-10 | Account locks after CFG-9 failed logins for CFG-10 minutes | BR-5.5 |
| V-11 | Profile photo is JPG or PNG, up to CFG-24 MB | BR-10.5 |
| V-12 | A member cannot edit gender after registration | BR-10.3 |
| V-13 | A changed email must be verified before it is used | BR-10.4 |

## 25.2 Route

| # | Rule | Source |
|---|---|---|
| V-14 | Start and End are mandatory for every leg | BR-11.3 |
| V-15 | Each leg has between CFG-3 (2) and CFG-2 (5) points including Start and End | BR-11.3 |
| V-16 | A point is 2 to 100 characters of letters, digits, spaces and `. , - ' / & ( )` | BR-11.5 |
| V-17 | A point cannot contain a web link or a run of 10 or more digits | A-26, BR-11.5 |
| V-18 | No two points of one leg may be the same place (similarity at least CFG-22, numbers exact) | BR-11.6 |
| V-19 | Start and End must not be the same place | BR-11.6 |
| V-20 | At least one travel day per leg | BR-11.8 |
| V-21 | Departure time is mandatory for each leg that exists | BR-11.8 |
| V-22 | A member has at most one Active route | BR-11.1 |
| V-23 | A member has at most one Draft route | BR-11.2 |
| V-24 | Return leg is required to activate when CFG-19 is Yes; if started, it must be complete | BR-11.9 |
| V-25 | A Raahi route can be activated only if the profile has a vehicle | BR-11.19 |
| V-26 | Only an email-verified (Active) member can create or activate a route | BR-5.1 |

## 25.3 Matching, Leads & Connections

| # | Rule | Source |
|---|---|---|
| V-27 | A lead is created only for Saathi–Raahi pairs, both routes Active, neither member blocked | Section 12.5 |
| V-28 | A leg matches only if days overlap, time gap is within CFG-1, shared points are in order, and shared points number at least CFG-4 | Section 12.5 |
| V-29 | Numbers inside a route point must match exactly for two points to pair | A-21 |
| V-30 | A request can be sent only from a lead that is not Withdrawn | BR-14.1 |
| V-31 | A member cannot have more than CFG-6 pending requests | BR-14.3 |
| V-32 | A member cannot request themselves, a member they blocked, a member who blocked them, or a Blocked / Inactive member | BR-4.4, BR-14.3 |
| V-33 | Only one open connection (Pending or Accepted) per pair of members | BR-14.4 |
| V-34 | After a rejection the sender cannot re-request for CFG-17 days | BR-14.6 |
| V-35 | Only the receiver can accept or reject; only the sender can withdraw, and only while Pending | BR-14.5 to BR-14.7 |
| V-36 | A Pending request not answered within CFG-5 days becomes Expired | BR-14.8 |
| V-37 | Every request carries the fixed request message | D-23, BR-14.17 |

## 25.4 Chat, Rating & Safety

| # | Rule | Source |
|---|---|---|
| V-38 | A chat message is 1 to CFG-26 characters, text only | BR-15.4 |
| V-39 | A chat exists and accepts messages only while the connection is Accepted | BR-15.1 |
| V-40 | Messages cannot be edited or deleted | BR-15.6 |
| V-41 | A rating needs 1 to 5 whole stars; the comment is up to CFG-27 characters with no link or 10-digit run | BR-16.4 |
| V-42 | Only members with an Accepted connection (now or past) can rate each other, once per connection | BR-16.1, BR-16.2 |
| V-43 | A report needs a reason; "Other" needs a description of up to 500 characters | Section 17.5 |
| V-44 | A report can be raised only against a member with a lead or connection | BR-17.4 |

## 25.5 Admin & System

| # | Rule | Source |
|---|---|---|
| V-45 | Every Admin action on a member, route or report needs a reason of at least 10 characters | A-54 |
| V-46 | A Temporary Block needs a future end date | Section 21.5 |
| V-47 | Configuration values must stay inside their allowed range | BR-22.5 |
| V-48 | No record (member, route, lead, connection, message, rating, report) is ever hard-deleted | BR-8.1 |
| V-49 | Another member's mobile number and email are never shown anywhere in the member product | BR-4.9, BR-18.2 |
| V-50 | Every request is authorised on the server for the record it touches | BR-4.2 |
| V-51 | A member must have accepted the current required version of the legal texts before using the product | BR-22.16 |
| V-52 | A route needs a Start City and an End City before it can be activated, chosen from the active cities | BR-11.20 |
| V-53 | Matching pairs two routes only when they share at least one city | BR-12.13 |
| V-54 | A city name is unique ignoring capital letters and spaces; a city in use by a Draft or Active route cannot be deactivated | BR-22.18, BR-22.19 |

---

# 26. End-to-End Scenario Catalogue

These scenarios run through several modules and are intended as acceptance-test material. Names and routes follow the examples used in the specification.

**E2E-1 — The Clean Path (full route on both legs)**
1. Ashish registers as Saathi; Neha registers as Raahi and chooses Car. Both verify email.
2. Ashish activates a route: Going Vinay Nagar → Badarpur Metro → Govindpuri → Kalkaji → Nehru Place (Mon–Fri, 08:00), Return reversed (18:30). Neha activates Going Vinay Nagar → Govindpuri → Kalkaji → Nehru Place (Tue–Thu, 07:50) and Return (18:20).
3. Matching runs. Ashish's list shows Neha first, Full Route on both legs; Neha's list shows Ashish.
4. Ashish opens the lead, reads "Why this match", taps Connect. SCR-14.1 shows the fixed message. A Pending request is created; Neha is notified.
5. Neha accepts. A chat opens with the fixed message as its first message.
6. They agree time and meeting point in chat. RAAH records no agreement.
7. Weeks later each rates the other. Averages stay hidden ("New member") until three ratings exist.

**E2E-2 — Useful Segment only**
1. Amit (Raahi) covers only Vinay Nagar → Badarpur Metro, Mon–Fri, 08:05.
2. Ashish sees Amit as Useful Segment with the shared stretch shown. No Full Route exists for the first leg.
3. Ashish connects; Amit accepts; they chat.

**E2E-3 — The Raahi starts the connection (D-6)**
1. Amit finds Ashish in his lead list and taps Connect.
2. Ashish sees the request, accepts, and the chat opens.

**E2E-4 — Rejection and cool-off**
1. Rohit rejects Ashish's request. Ashish sees "Your request was not accepted".
2. Rohit's lead shows "You can request again in 7 days". Ashish connects with Amit meanwhile.

**E2E-5 — No response, reminder and expiry**
1. Ashish's request to Neha is not answered. On day 25 Neha gets a reminder (CFG-29 = 5).
2. On day 30 the request becomes Expired; Ashish is told and the lead returns to Viewed.

**E2E-6 — Route edit moves a match out**
1. Ashish changes his Going time from 08:00 to 09:30.
2. A lead with a Raahi leaving at 08:05 is withdrawn on the Going leg; a lead matching on Return stays. A request Ashish already sent stays Pending.

**E2E-7 — Changing journey role**
1. Ashish (Saathi, Active route) opens Change Journey Role and chooses Raahi.
2. He must deactivate his route first: leads and pending requests are withdrawn; his connection with Neha and its chat stay.
3. The Vehicle field appears; he chooses Bike; he creates and activates a Raahi route.

**E2E-8 — Spelling variants and numbers**
1. Amit typed *Bdarpur Metro*; Ashish typed *Badarpur Metro*. The points pair at 93% and the lead appears.
2. A Saathi with *Sector 12 Metro* does not match a Raahi with *Sector 15 Metro*, because numbers must match exactly.

**E2E-9 — Block and unblock**
1. Ashish blocks Rohit from a lead. Both leads disappear; Rohit is not told.
2. Ashish later unblocks Rohit; matching includes them again, but there is no connection until a new request is accepted.

**E2E-10 — Report to Temporary Block**
1. Ashish reports Rohit for Inappropriate messages, ticks "Also block".
2. Admin opens the report, reads the attached chat (logged), applies a 14-day Temporary Block with a reason.
3. Rohit is notified and cannot log in; his route is Inactive; his connections are Disconnected. Ashish is told only that the report was reviewed.
4. After 14 days Rohit is Active again with no Active route.

**E2E-11 — Admin tunes matching**
1. Admin uses Test Match on *Nehru Place* vs *Nehru Plaza Mkt* (60%) and keeps CFG-22 at 70.
2. Admin raises CFG-1 from 30 to 45, logged. New activations use 45.
3. Admin runs "Recalculate all leads"; counts of leads created and withdrawn are logged; requests and connections are untouched.

**E2E-12 — Deactivation cascade**
1. A member with an Active route, one Pending request and one Accepted connection is deactivated by Admin.
2. Route Inactive; request Withdrawn; connection Disconnected; chat Closed; leads withdrawn both ways. History is intact.

**E2E-13 — Return leg made mandatory**
1. Admin sets CFG-19 to Yes.
2. A member activating a route with only a Going leg is asked to complete the Return leg. Existing Active routes continue until next edited.

**E2E-14 — Filters**
1. Ashish opens his list (Neha, Rohit, Amit). He selects Leg = Return: only Neha remains. He selects Gender = Female: only female members are shown.
2. Order among the remaining leads is unchanged; clearing the filters shows all three again.

**E2E-15 — City overlap decides eligibility**
1. Ashish (Delhi → Delhi) and a Raahi (Noida → Noida) type the same stop names, days and time. No lead appears for either.
2. Meera (Delhi → Noida) is eligible for both Ashish's Delhi Raahis and a Noida Raahi, because each shares a city with her route.
3. When the Noida Raahi changes his End City to Delhi, a lead for Ashish is created at once.

---

# 27. Non-Functional Requirements

| Area | Requirement |
|---|---|
| **Platform** | Phase 1 is one **responsive web application** that works comfortably on both desktop and phone browsers (D-20). A separate native mobile app is built later using the same rules. |
| **Browsers** | Current versions of Chrome, Edge, Firefox and Safari on desktop; Chrome on Android and Safari on iPhone for mobile |
| **Responsive layout** | Every screen, including registration, route entry, leads, requests and chat, must be fully usable on a phone screen and on a desktop screen. Admin screens must work on desktop and remain usable on a tablet. |
| **Performance** | Pages and lists open within a few seconds under expected load. Leads refresh within one minute of a route being activated, edited, deactivated or reactivated (A-38). Chat messages appear to the recipient within a few seconds. |
| **Concurrency** | If two members press Connect on each other at the same moment, only one open connection results (BR-14.4). Two Admin actions on the same record are applied in order and never both succeed in conflict. |
| **Security** | Role-based access enforced on the server for every request (BR-4.2). Passwords stored hashed and never recoverable. HTTPS for all traffic. Login attempts rate-limited (CFG-9, CFG-10). Session timeout per CFG-8. |
| **Privacy** | Data minimisation: mobile number and email of a member are never shown to other members (BR-4.9). Chat content hidden from Admin outside a report (BR-4.7). Route data shown to others is limited to Section 10.5. Lead lists cannot be bulk-exported by members. |
| **Audit** | Every create, edit, send, accept, reject, withdraw, block, report, rate and Admin action is logged with user, time and before/after values (Section 8) |
| **Data retention** | No member, route, lead, connection, message, rating, report or audit record is purged in Phase 1. How long to keep data and how to handle a member's request for erasure are Point 9 in Section 30. |
| **Backup** | Daily backup with a documented restore procedure, to be agreed with the client's IT |
| **Localisation** | English interface. Dates DD-MMM-YYYY. Times shown in 12-hour form with AM/PM. One platform time zone (India Standard Time); India only (D-27). |
| **Email** | Verification, reset and notification emails must be deliverable (sender domain and delivery service are a dependency, Section 28) |
| **Accessibility** | Readable contrast, form labels, keyboard operation of main flows |
| **Monitoring** | Failed jobs (matching, email, expiry, digest) are logged and visible to the technical team |
| **Offline** | Not supported. Connectivity is required |

---

# 28. Assumptions & Dependencies

These assumptions underpin the design. If any is wrong, the affected area must be revisited before development.

| # | Assumption | What changes if it is wrong |
|---|---|---|
| S-1 | The product is used by regular commuters for repeated office or college travel (Rule: primary use case) | Occasional one-off rides would need dates and a different matching model |
| S-2 | Members know and can write the names of the places they travel through; free text is acceptable (D-17) | A place picker or map becomes necessary |
| S-3 | India only (D-27): one time zone (India Standard Time), 10-digit mobile numbers, English | Other countries need time-zone, phone-format and language handling |
| S-4 | A member has one Active route at a time (D-14) | Several simultaneous routes change matching, dashboards and limits |
| S-5 | Seats or the number of co-travellers a Raahi can take are **not** modelled | Capacity limits would add fields, rules and matching changes |
| S-6 | Verification is by **email only** (D-8). No ID or phone verification | Stronger verification adds a verification workflow and an Admin queue |
| S-7 | Members agree money, fuel sharing and similar arrangements between themselves; RAAH takes no payment | Payments or fare sharing are a separate phase |
| S-8 | RAAH does not track live journeys or provide emergency (SOS) features | Live tracking and SOS are separate scope |
| S-9 | One Admin role with all Admin functions (A-10) | Role separation would need a permission matrix for Admin staff |
| S-10 | Members use their own devices and reliable internet | Offline support is a significant addition |
| S-11 | Chat is text-only (A-40) | Images, files or voice add storage, safety and moderation work |
| S-12 | The platform is open to members **everywhere in India** with no area restriction (D-25). A route's City comes from an Admin-managed list (D-26) | A restricted launch area would simplify sizing; cross-city commutes need the refinement in A-57 |

**Dependencies**

| # | Dependency | Owner |
|---|---|---|
| DEP-1 | Email delivery service and a sender domain that does not land in spam | Client IT / technical team |
| DEP-2 | Hosting, HTTPS certificate and database backups | Technical team |
| DEP-3 | Real-time channel for chat delivery | Technical team |
| DEP-4 | Terms of Use, Privacy Policy and Safety Guidelines: sample text supplied (Appendix B); the client edits and publishes the final text through Admin (D-24) and gets it reviewed by their legal advisor | Client |
| DEP-5 | Admin account(s) added to the database before launch (D-19) | Technical team / owner |

---

# 29. Phase 1 Boundary

## 29.1 Included

Registration with Saathi/Raahi selection and email verification · member profile with limited public view · one Active route with a Going leg and an optional Return leg, 2–5 free-text points per leg, days and approximate departure time · leg-by-leg matching with Full Route and Useful Segment, fuzzy text match, time tolerance, ranking · leads with explanation, filters by leg and gender, dismiss · connection requests from either side with a fixed message, accept / reject / withdraw / expire / disconnect · in-app text chat · 1–5 star rating with comments and moderation · block and report · notifications by in-app and email · Admin member management, route and lead monitoring, safety handling, configuration with test tool · Admin-managed Terms of Use, Privacy Policy and Safety Guidelines with versioning and re-acceptance · Admin-managed City list with member city requests · dashboards · 30 reports · full audit trail.

## 29.2 Excluded — Later Phases

Ride booking · driver or passenger assignment · fixed pickup or drop · journey timing enforcement · payments, fare or fuel sharing · live trip tracking and SOS · a separate native mobile app (the responsive web app already works on phones, D-20) · map or place picker · automatic route suggestion · group chat · images, files, voice or location in chat · SMS and phone OTP verification · ID or KYC verification · seat capacity per Raahi · multiple simultaneous active routes · countries other than India · multi-language · scheduled report emailing · Admin broadcast announcements (see Point 14, Section 30).

## 29.3 Why the Boundary Sits Here

Phase 1 answers **who travels my way, at my time, and can we talk safely**. Everything beyond — money, tracking, capacity, ID checks — is only worth building once matching is proved to find the right people and members actually connect. This is the product statement of Section 1.4: *Find the person before the journey. Decide the journey together.*

---

# 30. Points for Client Confirmation

These need a business decision. Each is a genuine choice, not a technical detail. Status shows what has been decided.

| # | Point | Decision / Options | Status | Impact if deferred |
|---|---|---|---|---|
| 1 | **Mobile browser use** | **Decided:** one responsive web app that works on desktop and phone browsers (D-20); a separate mobile app is built later | Resolved | — |
| 2 | **Reach and size** | **Decided:** open to members everywhere (D-25). Still needed: the expected number of members at launch and in the first year, for sizing | Partly resolved | **High** for sizing |
| 3 | **Free-text accuracy.** Review CFG-22 (70%) and RPT-4 after launch; decide whether a place picker is needed | Keep free text / add place picker later | Open (default: keep) | **High** |
| 4 | **Time comparison** at each member's own Start (D-4) | Keep as is / compare at the shared point later | Open (default: keep) | Medium |
| 5 | **Rating timing**: allowed as soon as the connection is Accepted (A-8) | Immediately / after N days | Open (default: immediately) | Medium |
| 6 | **Verification strength** | **Decided:** email only (D-8) | Resolved | — |
| 7 | **Emergency features**: no SOS or live location in Phase 1 | Accept / plan for later | Open (default: accept) | Medium |
| 8 | **Permanent Block** cannot be undone in the application | **Decided** (A-50) | Resolved | — |
| 9 | **Data protection and retention.** Records are never deleted (BR-8.1) | **Deferred by the client** (D-28): no retention period is set for now; all records are kept. Revisit with legal advice before a wide public launch | Deferred | **High** if a deletion or retention rule is later required |
| 10 | **Terms of Use, Privacy Policy, Safety Guidelines** | **Decided:** sample text supplied (Appendix B); Admin edits and publishes the final text (D-24). The client should still get the final text reviewed by a legal advisor | Resolved | — |
| 11 | **Admin capacity.** Reports are escalated after 3 days (CFG-32). Who reviews reports and how fast? | One Admin / team with defined hours | Open | Medium |
| 12 | **Home-area privacy.** Start points may reveal where a member lives. Hide the Start point from others until a connection is accepted? | Show full route (current) / hide Start until accepted | Open (default: current) | Medium |
| 13 | **Seats and capacity** per Raahi are not modelled | Not needed / add later | Open (default: not needed) | Medium |
| 14 | **Admin announcements.** The business flow says Admin manages notifications; Phase 1 has settings but no broadcast message | Not needed / add a simple announcement | Open (default: not needed) | Low |
| 15 | **Email sender and provider** | Client to confirm domain and provider | Open | Medium |
| 16 | **Language.** English only in Phase 1 | Accept / add Hindi later | Open (default: English) | Medium |
| 17 | **Cities on each route** | **Decided:** mandatory Start City and End City from an Admin-managed list; routes match only when they share a city (D-26) | Resolved | — |
| 18 | **Countries and time zone** | **Decided:** India only (D-27) | Resolved | — |
| 19 | **Cross-city commutes** | **Decided:** covered by Start City and End City (D-26) | Resolved | — |
| 20 | **"My city is not listed"** | **Decided:** included (A-56) | Resolved | — |

---

# 31. Go-Live Setup Sequence

## 31.1 Purpose

RAAH has **no masters to load**: no stop list, no category list, no opening stock. What must exist before the first real member is a working environment, an Admin account, reviewed settings and approved legal text. This section gives the order.

## 31.2 UI Prototype Reference

No clickable prototype has been created yet. A link will be added here once screens are designed from this specification.

## 31.3 Stage 1 — Platform Setup (Technical team and owner)

| Step | Action | Needs first | Done by |
|---|---|---|---|
| 1 | Prepare the hosted environment, HTTPS and database backups | Nothing | Technical team |
| 2 | Set up the email delivery service and sender domain; send a test verification email | Step 1 | Technical team |
| 3 | Add the Admin account(s) directly in the database (D-19) | Step 1 | Technical team / owner |
| 4 | Log in as Admin and confirm the Admin UI opens | Step 3 | Admin |

## 31.4 Stage 2 — Configuration and Content (Admin)

| Step | Action | Notes |
|---|---|---|
| 5 | Review all settings CFG-1 to CFG-32 and confirm defaults with the client | Section 22.4 lists ranges |
| 6 | Add the cities for launch on the City Master (SCR-22.5) | D-26, BR-31.4 |
| 7 | Open Legal Content (SCR-22.4), review the supplied sample Terms of Use, Privacy Policy and Safety Guidelines, edit them with the client's legal advisor and publish the final versions | DEP-4, D-24, BR-22.13 |
| 8 | Confirm the request message text (CFG-25) | Default "Hello, I want to connect with you" |
| 9 | Use the Test Match tool with sample routes of the pilot area to confirm CFG-1, CFG-4 and CFG-22 | Section 22.5 |

## 31.5 Stage 3 — Ready Check Before First Member

| # | Check | Pass condition |
|---|---|---|
| 1 | Admin can log in and sees the Admin Dashboard | Yes |
| 2 | A test member can register, verify email and log in | Verification email arrives |
| 3 | Test Saathi and Raahi routes produce a lead and the explanation is correct | Yes |
| 4 | A test request can be sent, accepted, chatted on and rated | Yes |
| 5 | A test block and a test report work and Admin can see the report | Yes |
| 6 | Notifications and the daily digest are delivered | Yes |
| 7 | Backups run and a restore has been tested | Yes |
| 8 | The three legal texts are published (not "Sample") and visible at registration; the "Legal text is still the sample" alert is gone | Yes |
| 9 | At least one active city exists and members can choose it on a route | Yes |

## 31.6 Stage 4 — First Live Cycle

| Step | Action | Who |
|---|---|---|
| 10 | Pilot members register, choose Saathi or Raahi, verify email | Members |
| 11 | Members create and activate routes with Going and (if they wish) Return legs | Members |
| 12 | Matching produces leads; members connect, chat and decide their journey | Members |
| 13 | Admin watches the dashboard, Matching Health and the Report Queue daily in the first weeks | Admin |
| 14 | After the pilot, Admin reviews RPT-4, RPT-9 and RPT-11 and tunes CFG-22, CFG-1 and CFG-4 | Admin |

## 31.7 Rules That Follow From This Order

**BR-31.1** — The system may be tested with the sample legal text, but must not be opened to real members until all three documents have been replaced by final published versions (BR-22.13).

**BR-31.2** — The system must never be opened to members with no Admin account in place (D-19).

**BR-31.3** — Settings changed after go-live apply to new activity only (BR-22.2). Use "Recalculate all leads" only deliberately (BR-22.4).

**BR-31.4** — The system must not be opened to members until at least one city is active, because a route cannot be activated without a City (D-26).

---

# 32. Traceability Matrix

## 32.1 Business Flow Document → Specification

The source document is *RAAH — Complete Product & Business Flow* (53 sections).

| Source sections | Topic | Where it is specified |
|---|---|---|
| 1, 3, 6, 34, 46–53 | Core concept, product loop, does / does-not, vision | Sections 1.4, 1.5, 2, 29 |
| 2 | Real-life problem | Section 1.2 |
| 4, 36–42 | Roles; Admin functions | Sections 4, 19–22 |
| 5 | Saathi/Raahi as journey-context roles | BR-4.3, Section 9.3, BR-10.7 |
| 7 | Complete route, ordered points | M2 (Section 11) |
| 8–10, 19, 30, 33, 47 | Full route first, then maximum useful segment; matching order; ranking | M3 (Section 12), M4 |
| 11, 20, 21 | Examples; lead; what a Saathi sees | Section 12.9; M4; Section 10.5 |
| 12, 13, 25–28 | Connection is not booking; mutual decision | D-5, Principle 8, BR-14.15, M6 |
| 14 | Registration, verification, profile | Section 5; M1; D-8 (email only) |
| 15–18, 29, 35, 44 | Saathi and Raahi flows, route creation, activation | Sections 2.2, 11 |
| 17 | Travel pattern | Section 11.6 |
| 22–24 | Connection request, notification, accept | M5, M9 |
| 31, 32 | Rejected, no response | BR-14.6, BR-14.8 |
| 36 | Admin dashboard | Section 23.5 |
| 43 | Notifications | M9 (Section 18) |
| 45 | The 12 core business rules | Section 32.2 |

## 32.2 The 12 Core Business Rules

| Source rule | Rule in brief | Specified in | Tested by |
|---|---|---|---|
| Rule 1 | Member adds a complete route | BR-11.3, BR-11.12, V-14, V-15 | AC-11.1, AC-11.2, E2E-1 |
| Rule 2 | Route points kept in sequence | BR-11.7, Section 12.5 Step 6 | AC-12.2 |
| Rule 3 | Full-route relevance checked first | BR-12.4, Section 12.5 Step 8 | AC-12.4, E2E-1 |
| Rule 4 | Else maximum useful segment | BR-12.4, BR-12.6 | AC-12.3, E2E-2 |
| Rule 5 | RAAH provides a relevant-person lead | M3, M4 | E2E-1, E2E-2 |
| Rule 6 | Member contacts the other member | M5, M6, BR-14.5 | AC-14.1, E2E-1 |
| Rule 7 | Journey decided by users after connection | Principle 8, BR-15.9 | E2E-1 step 6 |
| Rule 8 | RAAH never books or assigns | Principle 1, BR-4.6, BR-14.15, Section 1.5 | SC-20.6 |
| Rule 9 | Saathi/Raahi are journey-context roles | BR-4.3, BR-10.7, Section 9.3 | E2E-7 |
| Rule 10 | Matching happens before the journey | Section 2.2, M3 | E2E-1 |
| Rule 11 | Time, meeting point, shared portion decided mutually | D-5, BR-15.9 | E2E-1 step 6 |
| Rule 12 | Maximum useful coverage, not only Start → End | Section 12.5, BR-12.4 | AC-12.4, E2E-2 |

## 32.3 Business Questions → Module → Report

| # | Question (Section 1.1) | Module | Key rule | Report | Test |
|---|---|---|---|---|---|
| 1 | Who are our members, and verified? | M1, M10 | BR-5.1, BR-19.5 | RPT-1, RPT-2 | AC-5.1 |
| 2 | What is each member's complete route? | M2 | BR-11.3, BR-11.7 | RPT-3 | AC-11.1 |
| 3 | When does each member travel? | M2 | BR-11.8 | RPT-3 | SC-11.8 |
| 4 | Who else travels a relevant route at a relevant time? | M3 | Section 12.5 | RPT-6, RPT-7 | E2E-1, AC-12.1 |
| 5 | Full route or segment, and why? | M3, M4 | BR-12.1, Section 13.5 | RPT-6, RPT-8 | AC-13.5 |
| 6 | Who requested whom; accepted, rejected, ignored? | M5 | BR-14.5 to BR-14.8 | RPT-10, RPT-11, RPT-12 | E2E-4, E2E-5 |
| 7 | Can connected members talk safely? | M6 | BR-15.1, BR-15.9 | RPT-13 | AC-15.2 |
| 8 | Is every member safe? | M8, M12 | BR-17.1, BR-21.1 | RPT-17 to RPT-22 | E2E-9, E2E-10 |
| 9 | What did members think of each other? | M7 | BR-16.1, BR-16.5 | RPT-14, RPT-15, RPT-16 | AC-16.1 |
| 10 | Is matching producing connections? | M11, M14 | BR-20.5 | RPT-4, RPT-9, RPT-11 | SC-20.1 |
| 11 | Can everything be traced? | Section 8 | BR-8.1, BR-8.2 | RPT-24 to RPT-29 | V-48 |

## 32.4 Principles → Where They Are Enforced

| Principle | Enforced in |
|---|---|
| 1 RAAH never books or assigns | BR-4.6, BR-14.15, Section 29.2 |
| 2 Complete ordered route | BR-11.3, BR-11.7 |
| 3 Full route first | BR-12.4 |
| 4 Members enter their own route | BR-11.4, BR-12.8, D-17 |
| 5 Roles are journey-context | BR-4.3, BR-10.7 |
| 6 Either side may connect; nobody auto-connected | BR-14.2, BR-14.5 |
| 7 Connection is not a booking | BR-14.15 |
| 8 Journey decided by members | BR-15.9, D-5 |
| 9 Chat only, only when connected | BR-15.1 |
| 10 Safety first-class | M8, M12 |
| 11 Minimal private information | BR-4.9, BR-18.2, Section 10.5 |
| 12 Admin monitors, never matches | BR-4.6, BR-20.1 |
| 13 Nothing hard-deleted | BR-8.1, V-48 |
| 14 Every lead explains itself | BR-12.1, Section 13.5 |
| 15 Settings are configurable | Section 6, M13 |

---

# Appendix A — Document Conventions

| Item | Convention |
|---|---|
| Business rules | `BR-{section}.{n}`, numbered inside each module section (for example BR-14.5 is rule 5 of Section 14) |
| Scenarios | `SC-{section}.{n}`; end-to-end scenarios are `E2E-{n}` (Section 26) |
| Acceptance criteria | `AC-{section}.{n}` in Given / When / Then form |
| Screens | `SCR-{section}.{n}` |
| Reports | `RPT-1` to `RPT-30` (Section 24) |
| Settings | `CFG-1` to `CFG-32` (Section 6; ranges in Section 22.4) |
| Validations | `V-1` to `V-54` (Section 25) |
| Decisions and assumptions | `D-n` confirmed client decisions, `A-n` assumptions and their status (Section 0) |
| System assumptions and dependencies | `S-n` and `DEP-n` (Section 28) |
| Statuses | Only those listed in Section 9. No module introduces a status of its own |
| Records | `MBR-`, `RTE-`, `LED-`, `CON-`, `RAT-`, `SAF-` (Section 7) |

---

# Appendix B — Sample Legal Content

> **SAMPLE TEXT — NOT LEGAL ADVICE.** This text is supplied so the product can be tested and so that Admin has something to edit. It must be reviewed and rewritten by the client's legal advisor before real members use RAAH. Words in [square brackets] are placeholders to fill in.
>
> In the application these three documents are loaded as **"Sample 1"** and are edited and published by Admin on the Legal Content screen (SCR-22.4, D-24). Until they are replaced, the Admin Dashboard shows "Legal text is still the sample".

---

## Document 1 — Terms of Use

**RAAH — Terms of Use** · Version: Sample 1 · Effective from: [date]

### 1. About RAAH

RAAH, provided by [Company Name] ("we", "us"), helps people who travel the same regular route find each other **before** the journey. RAAH is a **connection platform**. It is not a transport service.

RAAH does **not** book rides, assign drivers or passengers, fix pickup or drop points, decide journey times, collect fares or provide vehicles. Members decide every detail of any journey themselves, in chat, after both have agreed to connect.

### 2. Who can use RAAH

- You must be at least [18] years old, or the age of majority where you live, to register.
- You must give a valid email address and keep it under your control.
- You may have **one** account. You are responsible for everything done through it, so keep your password private.

### 3. Your information

- Give accurate information about yourself, your gender, your vehicle (if you are a Raahi) and your route.
- Keep your route up to date. Deactivate it when you no longer travel it.
- You choose which places to write on your route. We suggest naming an area or landmark, not your full home address.

### 4. How RAAH works

- You create one active route with a Going leg and, if you like, a Return leg.
- RAAH shows you **leads**: members whose route and time are relevant to yours. A lead is a suggestion only.
- A connection is created only when one member sends a request and the other accepts. Either side may send a request.
- Accepting a connection lets two members chat. It is **not** a booking and does not oblige anyone to travel together.

### 5. Your responsibilities

You agree **not** to:
- give false, misleading or someone else's information;
- harass, threaten, insult, pressure or discriminate against anyone;
- send spam, advertising or unwanted messages;
- ask for money, personal documents or financial details through RAAH;
- use RAAH for any unlawful purpose or for anything other than finding co-travellers on a regular route;
- try to copy or collect other members' information, or to interfere with the service.

### 6. Journeys are between members

Any journey, meeting point, cost-sharing or other arrangement is made **between members**, at their own choice and risk. RAAH does not supervise, track or guarantee any journey, any member or any vehicle, and does not check driving licences or vehicle documents. Please read and follow the **Safety Guidelines**.

### 7. Safety tools and moderation

- You can **block** any member and **report** any member you have been matched or connected with.
- Administrators may review reports. If a report involves a chat, an administrator may read that conversation for the review. Chats are otherwise not read by administrators.
- We may warn, restrict, suspend or permanently block an account that breaks these Terms or puts others at risk.

### 8. Ratings

Connected members can rate each other from 1 to 5 stars and may add a comment. Comments must be fair and respectful. We may hide a comment that is abusive or contains personal details.

### 9. Ending your use

You may deactivate your account at any time. We may end or suspend an account as described in section 7. Records needed for safety and legal reasons may be kept as explained in the Privacy Policy.

### 10. Limits of our responsibility

RAAH is provided "as is". To the extent the law allows, [Company Name] is not responsible for what members do, for any journey, or for any loss, injury or dispute that happens between members. Nothing in these Terms limits any right you have by law that cannot be limited.

### 11. Changes

We may update these Terms. If a change is important, we will ask you to accept the new version before you continue to use RAAH.

### 12. Governing law and contact

These Terms are governed by the laws of [country/state]. Questions: [support email address]. Address: [company address].

---

## Document 2 — Privacy Policy

**RAAH — Privacy Policy** · Version: Sample 1 · Effective from: [date]

### 1. Who we are

[Company Name] ("we") runs RAAH. Contact for privacy questions: [privacy contact email].

### 2. What we collect

| Information | Why we need it |
|---|---|
| Name, email address, password (stored protected, never readable) | To create and secure your account |
| Gender | To let members filter leads by gender |
| Mobile number (optional) | Collected at registration. **Not shown to other members.** Visible to administrators |
| Profile photo (optional) | To show on your profile |
| Vehicle type: Bike or Car (Raahi only) | To show on your profile |
| Your route: places you type, travel days, approximate departure times | To find relevant co-travellers |
| Connection requests, chat messages, ratings and reports | To run the service and keep members safe |
| Login details: time, device, browser, IP address | For security |
| Terms you accepted, with version and time | To keep a record of your agreement |

### 3. What other members can see

Other members see only a **limited profile**: your name, photo, gender, journey role, vehicle (if Raahi), verified-email badge, rating average and count, the month and year you joined, and your **current route** (the places, days and approximate times you entered).

Other members **never** see your email address or mobile number from RAAH. Anything you type in a chat is visible to the member you are chatting with, so share personal details only if you choose to.

### 4. Who can read your chat

Administrators **do not** read chats during normal operation. If a member reports a conversation, an administrator can read that conversation to review the report, and each such access is logged.

### 5. How we use information

To run RAAH: find matches, show leads, send connection requests and messages, send you notifications and emails about your account and activity, moderate ratings, handle safety reports and improve the service. We do **not** sell your personal information.

### 6. Sharing

We share information only with service providers who help us run RAAH (for example hosting and email delivery), and only as needed; or where required by law or to protect someone's safety.

### 7. How long we keep it

[Retention period to be decided with the client's legal advisor.] Records of activity, chats, ratings and reports may be kept for safety, audit and legal reasons. See section 9 for your choices.

### 8. Security

We protect information with access controls, protected password storage, encrypted connections and activity logs. No system is completely secure, so please keep your password private.

### 9. Your choices

You can view and correct most of your profile in the app. You can deactivate your account. To ask about access, correction or deletion of your information, write to [privacy contact email]; we will respond as required by applicable law. Gender is corrected by an administrator on request.

### 10. Emails and notifications

We send emails needed for your account (verification, password reset, safety and account actions). You can switch off optional emails, such as the daily leads digest and unread-message reminders, in Notification Preferences.

### 11. Age

RAAH is not meant for people under [18]. If we learn that a younger person has registered, we may close the account.

### 12. Changes and contact

We may update this policy. If the change is important, we will ask you to accept it. Questions: [privacy contact email]. Address: [company address].

---

## Document 3 — Safety Guidelines

**RAAH — Safety Guidelines** · Version: Sample 1 · Effective from: [date]

Travelling with someone new is a personal decision. These guidelines help you stay safe. RAAH connects people; **you decide whether and how to travel together.**

### Before you connect

- Read the person's profile, route and rating. A verified-email badge only shows that the email address was confirmed; it does **not** prove who the person is.
- Take your time. You are never obliged to accept a request or to send one.
- Use the **gender filter** if it makes you more comfortable.

### In chat

- Keep conversations on RAAH chat until you trust the person.
- Do **not** share your home address, bank details, identity documents, passwords or OTPs.
- Do **not** send or lend money.
- If anyone pressures you, makes you uncomfortable, or asks for something that feels wrong, **stop and block them**, then report them.

### Meeting for the first time

- Meet in a **public, well-lit, busy place** such as a metro station or a main road, not at a private location.
- Tell a family member or friend who you are meeting, where, and when. Share the person's name and, if you know it, the vehicle details.
- Check that the person matches their profile. If anything is different, do not go ahead.
- Trust your instincts. You can change your mind at any time.

### While travelling

- If you are a Raahi: drive safely and legally. You are responsible for your vehicle, licence, insurance and helmet or seat-belt use. RAAH does not check these.
- If you are a Saathi: wear a helmet on a bike, use a seat belt in a car, and do not travel if the driver seems unwell, careless or unsafe.
- Agree beforehand how any costs are shared. RAAH does not handle payments.

### Block and report

- **Block** a member at any time from a lead, a connection or a chat. They will not be told.
- **Report** a member if they harass you, behave inappropriately, use a fake profile or make you feel unsafe. Administrators will review your report. If your report involves a chat, an administrator can read that conversation.
- You will be told when your report has been reviewed, but not which action was taken.

### Emergencies

**RAAH does not provide emergency services.** If you are in danger, contact the local emergency services immediately (in India, call **112**). Then report the member on RAAH when it is safe.
