# Module M1 — Member Profile

> Source: `RAAH_Functional_Specification.md` Section 10. Build order #1.

## Purpose

Let a verified member present themselves to other members in a limited, safe way, keep their own details current, and switch between Saathi and Raahi when their need changes.

## Who Uses It

Member edits their own profile. Admin views (Module M10).

## Dependencies

- Foundation: registration (Section 5), member statuses (9.1), config (CFG-21, CFG-24, CFG-28), audit trail (Section 8).
- Rating summary shown here comes from M7 (built later; render as placeholder/"New member" until then).

## Screens

| Screen | Purpose |
|---|---|
| SCR-10.1 My Profile | View own name, photo, gender, email, mobile, journey role, vehicle, verification status and rating summary |
| SCR-10.2 Edit Profile | Change name, photo, mobile, email, vehicle |
| SCR-10.3 How Others See Me | Preview of the limited profile that other members see on a lead |
| SCR-10.4 Change Journey Role | Switch from Saathi to Raahi or the reverse (A-19) |
| SCR-10.5 Deactivate My Account | Member ends their own access (A-25) |

## Field Specification — Profile

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

## What Other Members See

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

## Business Rules

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

## Status Lifecycle

M1 uses the Member status of Section 9.1.

| From | To | Who | Condition |
|---|---|---|---|
| Unverified | Active | System | Email link opened in time |
| Active | Inactive | Member (BR-10.9) or Admin | Member self-deactivates, or Admin deactivates with a reason |
| Inactive | Active | Admin only | Reactivation with a reason |

Restricted and Blocked statuses are set by Admin (Module M12).

## Scenarios

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
2. The screen shows exactly the "shown to other members" items of the section above. Email and mobile are visibly absent.

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

## Acceptance Criteria

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

## Data Impact

| Action | Records created or changed |
|---|---|
| Profile edit | Member record updated; audit entry |
| Email change | Pending email held until verified; audit entry |
| Role change | Active route deactivated (leads and pending requests withdrawn); journey role updated when the new route is created |
| Self-deactivation | Member Inactive; route Inactive; pending requests Withdrawn; connections Disconnected |

## Reports Produced

RPT-1 User & Access Report (full list in M15).

## Cross-References

- **Config:** CFG-21 (vehicle types), CFG-24 (photo size), CFG-28 (rating threshold).
- **Statuses:** Member (9.1).
- **Validations:** V-11 (photo), V-12 (gender locked), V-13 (email change verify).
- **Decisions/assumptions:** D-18, D-21, A-19, A-22, A-23, A-24, A-25.
