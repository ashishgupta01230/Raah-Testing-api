# M1 — Member Profile — API Plan

| | |
|---|---|
| **Module** | M1 — Member Profile (spec `docs/spec/01_M1_Member_Profile.md`) |
| **Document type** | API implementation plan: contracts, data model, endpoints, rules, validation, audit, sequencing, tests |
| **Build order** | #1 after Foundation (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M1 lets an authenticated, verified Member:

1. Read their own full profile (SCR-10.1).
2. Edit name, mobile, photo, and email (SCR-10.2).
3. Preview the limited public view other members see (SCR-10.3).
4. Switch journey role Saathi ⇄ Raahi (SCR-10.4).
5. Deactivate their own account (SCR-10.5).

Out of scope (owned elsewhere but referenced):

| Concern | Owner |
|---|---|
| Registration, login, email verification, sessions, account status | Foundation (spec Section 5) |
| Active route, route deactivation, route summary | M2 |
| Leads / pending requests withdrawal | M4 / M5 |
| Connections closing | M5 |
| Rating average and count | M7 (M1 renders a placeholder until M7) |
| Gender correction by Admin with reason | M10 |
| Config editing, audit storage | Foundation / M13 |
| Blocked members, my reports | M8 / M12 |
| Member notifications (e.g. Admin removed a photo) | M9 (M1 only records the audit event) |

**Reports.** M1 is the read-only data source for **RPT-1 User & Access Report** (M15): member number, name, role, vehicle, status, verification, registered on, last login. M1 exposes no report endpoint; M15 reads the same Foundation + `raah_MemberProfile` fields directly. Verification/role/status changes made in M1 must keep those fields accurate so RPT-1 stays correct.

---

## 2. Dependency and build-order handling

M1 is built **before** M2, M4, M5, M7, but BR-10.7 and BR-10.9 require route deactivation, request withdrawal, and connection closing. Do not import M2/M5 services in M1 (they do not exist yet, and reverse dependencies are forbidden).

Define narrow **module boundary contracts in M1** and register safe implementations now:

| Contract (M1) | Purpose | M1 implementation | Later implementation |
|---|---|---|---|
| `IActiveRouteGateway` | Read active-route summary; deactivate the Active route | No-op: reports no active route, deactivation returns "none" | M2 replaces registration |
| `IPendingRequestGateway` | Withdraw the member's pending requests | No-op | M5 |
| `IConnectionGateway` | Close the member's connections | No-op | M5 |
| `IRatingSummaryGateway` | Read rating average/count for a member | Returns `count = 0`, `average = null` | M7 |

Rules:

- M1 depends only on Foundation contracts plus these four M1-owned gateways.
- When M2/M5/M7 land, they supply the real implementation via DI; the M1 endpoint contract does not change.
- Rating fields are stored on the profile projection table so M7 can write them without an M1 code change. Until then the API returns `isNewMember = true`.
- The route summary in SCR-10.3 is `null` until M2 is available; the UI must handle the placeholder (see UI plan).

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; class decorated by route only (e.g. `[Route("me/profile")]`); services injected through the primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]` on the service; interface in `Binsera.Core`; no manual `AddScoped` |
| Response | Return `Ok(payload)`; the global `ResponseFilterAttribute` wraps it as `{ data, errorCode, errorDescription, message, modelValidation }` |
| Non-2xx | Return `BadRequest(new { code, message })` / `StatusCode(409)` style object results; **never** `Forbid()` / `Challenge()` (they get swallowed to 200) |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new RAAH tables are normal EF-managed tables in `Binsera.Persistence`; `member_Master` stays excluded and untouched by migrations |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, PK `long Id`, decimal precision explicit, audit columns, `IsDeleted`, per-column indexes |
| Validation | No FluentValidation in the repo. Validate inside the application service (manual checks + `InvalidOperationException`/result object), and keep DTO data annotations only where the existing model-binding envelope expects them |
| Audit | Use the Foundation audit service; never hard-delete; record old/new values |
| Current user | `UserId`, `UserName`, `RoleId`, `MemberGraphId` from `ApiControllerBase` |

**Naming note.** This plan uses the route prefix `me/profile` (no `/api`) to match existing routes such as `users`, `media`, `auth/validate`. If the team later adopts a global `api` prefix, add it centrally, not per controller.

---

## 4. Data model

### 4.1 Field ownership

Do **not** bolt RAAH fields onto `member_Master`. Identity fields stay with the Foundation member record; RAAH-specific profile data lives in new tables linked by FK.

| Field | Owner | Access from M1 |
|---|---|---|
| Full Name | Foundation member | Read + write through `IMemberIdentityService` |
| Email | Foundation member / login | Read; change through M1 pending-email flow |
| Mobile | Foundation member | Read + write through `IMemberIdentityService` |
| Gender | Foundation member | Read only (A-22) |
| Member status (Unverified/Active/Inactive/…) | Foundation (Section 9.1) | Read; deactivate/reactivate through `IMemberStatusService` |
| Member number `MBR-nnnnnn`, Member since | Foundation | Read |
| Journey Role | **M1** (`raah_MemberProfile`) | Read + write via role change |
| Vehicle | **M1** (`raah_MemberProfile`) | Read + write |
| Profile photo | **M1** (`raah_MemberProfile.PhotoMediaId`) | Read + write (media stored through `IMediaService`) |
| Pending email change | **M1** (`raah_MemberEmailChange`) | Read + write |
| Rating summary | M7 writes into **M1** projection columns | Read |

### 4.2 New tables

#### `raah_MemberProfile` (1:1 with Member)

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint | FK → `member_Master.Id`, **unique** |
| `JourneyRole` | tinyint | 1 = Saathi, 2 = Raahi (enum `JourneyRole`) |
| `VehicleType` | tinyint null | 1 = Bike, 2 = Car; mandatory when role = Raahi |
| `PhotoMediaId` | bigint null | References media record; not `Member.ProfileImage` |
| `RatingAverage` | decimal(2,1) null | Written by M7; null until first rating |
| `RatingCount` | int not null default 0 | Written by M7 |
| `ProfileCompletedAtUtc` | datetime2 null | Set when email verified and mandatory fields present |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | |

Indexes: unique `MemberId`; `JourneyRole`.

#### `raah_MemberEmailChange`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint | FK → `member_Master.Id` |
| `NewEmail` | nvarchar(256) | As entered, trimmed |
| `NormalizedEmail` | nvarchar(256) | Upper-invariant, for uniqueness checks |
| `TokenHash` | nvarchar(64) | Hex SHA-256 of the emailed token; raw token never stored |
| `ExpiresAtUtc` | datetime2 | Now + CFG-11 hours |
| `ConsumedAtUtc` | datetime2 null | Set when verified |
| `Status` | tinyint | 1 Pending, 2 Verified, 3 Cancelled, 4 Expired |
| audit columns + `IsDeleted` | | |

Indexes: `MemberId, Status`; unique `TokenHash`. Uniqueness of `NormalizedEmail` is enforced in the service against live accounts **and** other Pending requests; do not add a DB unique index that a cancelled request would block.

Migration names (indicative):
- `20260101000000_AddRaahMemberProfile`
- `20260101000100_AddRaahMemberEmailChange`

### 4.3 Enums

```
JourneyRole    { Saathi = 1, Raahi = 2 }
VehicleType    { Bike = 1, Car = 2 }
Gender         { Male = 1, Female = 2, Other = 3 }   // Foundation value, display only in M1
EmailChangeStatus { Pending = 1, Verified = 2, Cancelled = 3, Expired = 4 }
```

Store enums as `tinyint` via EF conversion, matching the existing small-int style (`Gender` on `Member` is `int?`).

---

## 5. Foundation contracts consumed by M1

| Contract | Method (shape) |
|---|---|
| `IMemberIdentityService` | `GetAsync(memberId)` → name, email, mobile, gender, member number, member since, email verified, status |
| | `UpdateNameAsync(memberId, fullName)` |
| | `UpdateMobileAsync(memberId, mobile?)` |
| | `UpdateEmailAsync(memberId, newEmail)` (called only after token verification) |
| | `IsEmailInUseAsync(normalizedEmail, excludeMemberId)` |
| `IMemberStatusService` | `SetInactiveAsync(memberId, reasonSource)` ; `GetStatusAsync(memberId)` |
| `ISessionRevoker` | `RevokeAllSessionsAsync(memberId)` (self-deactivation) |
| `IEmailSender` | `SendEmailChangeVerificationAsync(toEmail, token, expiresAtUtc)` |
| `IAppConfigReader` | `GetIntAsync(ConfigKey)` for CFG-11, CFG-21, CFG-23, CFG-24, CFG-28 |
| `IAuditService` | `RecordAsync(entity, entityId, action, oldValue, newValue, actor)` |
| `IMediaService` | Existing `UploadTempAsync` / `AttachMediaAsync` / `DeleteMediaAsync` / `GetMediaFile` |
| `ICurrentUserService` | Existing current user accessor |

If a contract does not exist yet, add it to the Foundation module as part of the M1 groundwork — do not edit `member_Master` schema from M1.

---

## 6. DTOs (Binsera.Core)

Suggested namespace: `Binsera.Core.Members.Models.Raah`.

### 6.1 `MyProfileDto` (own view — SCR-10.1)

```
memberId              long
memberNumber          string          // MBR-nnnnnn
fullName              string
photoUrl              string?         // resolved media URL or null
photoMediaId          long?
gender                string          // "Male" | "Female" | "Other"
email                 string?
mobile                string?         // own value; never in public DTO
journeyRole           string          // "Saathi" | "Raahi"
vehicle               string?         // "Bike" | "Car" | null
emailVerified         bool
memberStatus          string          // "Active" | "Unverified" | ...
memberSince           DateTime        // ISO date; client renders "Oct 2026"
rating                RatingSummaryDto
profileComplete       bool
flags                 ProfileFlagsDto // { canEditGender=false, canChangeRole, hasActiveRoute }
```

`RatingSummaryDto` → `{ average: decimal?, count: int, isNewMember: bool, threshold: int }` where `isNewMember = count < CFG-28`.

### 6.2 `PublicMemberProfileDto` (what others see — SCR-10.3 / M4 / M5)

This DTO is deliberately **separate** from `MyProfileDto`; it must not inherit from it and must contain no private fields.

```
memberId              long
fullName              string
photoUrl              string?
gender                string
journeyRole           string
vehicle               string?
emailVerifiedBadge    bool
rating                RatingSummaryDto
memberSince           DateTime
currentRoute          PublicRouteSummaryDto?   // null until M2
```

`PublicRouteSummaryDto` → `{ routeNumber, startCity, endCity, going: PublicLegDto, return: PublicLegDto? }`
`PublicLegDto` → `{ points: [{ order, name }], days: [string], departureTime: "HH:mm" }`

**Never present:** email, mobile, login history, IP/device, past/deactivated routes, reports, admin notes, blocks. Add a unit test asserting none of those property names exist on the public DTO or its JSON.

### 6.3 Request DTOs

| DTO | Fields |
|---|---|
| `UpdateMyProfileRequest` | `FullName` (string, required), `Mobile` (string?, optional; empty clears) |
| `ChangeEmailRequest` | `NewEmail` (string, required) |
| `VerifyEmailChangeRequest` | `Token` (string, required) |
| `ChangeVehicleRequest` | `VehicleType` (string, required) |
| `ChangeJourneyRoleRequest` | `NewRole` (string), `VehicleType` (string?), `ConfirmDeactivateActiveRoute` (bool) |
| `DeactivateAccountRequest` | `Confirm` (bool, required true) |
| Photo | `IFormFile file` (multipart) |

### 6.4 Response DTOs

| DTO | Fields |
|---|---|
| `ProfileOptionsDto` | `vehicleTypes: [{value,label}]` (CFG-21), `photoMaxMb` (CFG-24), `nameMin=3`, `nameMax=100`, `ratingThreshold` (CFG-28), `emailChangeLinkHours` (CFG-11), `emailResendCooldownSeconds` (CFG-23) |
| `EmailChangeRequestedDto` | `maskedNewEmail`, `expiresAtUtc`, `message` |
| `RoleChangePreflightDto` | `currentRole`, `targetRoles`, `hasActiveRoute`, `activeRouteSummary`, `willWithdrawLeads`, `willWithdrawPendingRequests`, `keepsConnections`, `requiresVehicle` |
| `RoleChangeResultDto` | `journeyRole`, `vehicle`, `routeDeactivated`, `nextStep` ("CreateRoute") |
| `PhotoUploadResultDto` | `mediaId`, `photoUrl` |

---

## 7. Endpoint catalogue

Base route: `/me/profile`. All endpoints require an authenticated **Member** JWT unless marked anonymous.

| # | Method | Route | Purpose | Screen | Success |
|---|---|---|---|---|---|
| 1 | GET | `/me/profile` | Own full profile | SCR-10.1 / Me hub | 200 `MyProfileDto` |
| 2 | GET | `/me/profile/preview` | Exactly what others see | SCR-10.3 | 200 `PublicMemberProfileDto` |
| 3 | GET | `/me/profile/options` | Vehicle types and limits | SCR-10.2 / 10.4 | 200 `ProfileOptionsDto` |
| 4 | PUT | `/me/profile` | Update full name and mobile | SCR-10.2 | 200 `MyProfileDto` |
| 5 | POST | `/me/profile/photo` | Upload/replace photo (multipart) | SCR-10.2 | 200 `PhotoUploadResultDto` |
| 6 | DELETE | `/me/profile/photo` | Remove photo | SCR-10.2 | 200 `{ photoMediaId: null }` |
| 7 | PUT | `/me/profile/vehicle` | Change vehicle (Raahi only) | SCR-10.2 | 200 `MyProfileDto` |
| 8 | POST | `/me/profile/email-change` | Request email change (sends link) | SCR-10.2 | 202 `EmailChangeRequestedDto` |
| 9 | POST | `/auth/email-change/verify` | Verify new email token (anonymous link target) | Email link | 200 `{ email, message }` |
| 10 | GET | `/me/profile/role-change/preflight` | Consequences before role switch | SCR-10.4 | 200 `RoleChangePreflightDto` |
| 11 | POST | `/me/profile/role-change` | Switch journey role | SCR-10.4 | 200 `RoleChangeResultDto` |
| 12 | POST | `/me/profile/deactivate` | Self-deactivate account | SCR-10.5 | 200 `{ message }` |

Forward-looking, **not part of the M1 public API**:
- M4/M5 read public profiles through `IPublicMemberProfileService.GetAsync(memberId, viewerMemberId)`, which performs the access check (viewer must have a lead/request/connection with the member). Do not expose an unguarded `/members/{id}` endpoint from M1.

---

## 8. Endpoint details

### 8.1 `GET /me/profile`

- Resolve `memberId` from the token (`MemberGraphId` → member, or `UserId` per Foundation).
- Load Foundation identity + `raah_MemberProfile`.
- If no profile row exists, create it lazily from Foundation registration data (role/vehicle already chosen at registration) in the same request, or return a defensively built DTO and repair on first write. Prefer lazy repair.
- `profileComplete` = email verified AND name valid AND (role = Saathi OR vehicle present). Photo and mobile are optional (BR-10.1).
- `flags.canEditGender = false` (AC-10.2).
- `flags.hasActiveRoute` from `IActiveRouteGateway` (false until M2).
- `profileComplete` is not a gate on this endpoint.

**Auth:** Member role. Unverified members may read their own profile (the UI still shows only the verify gate, but the endpoint is safe). Inactive/Blocked tokens are rejected by Foundation session validation (BR-5.4/BR-5.9).

### 8.2 `GET /me/profile/preview`

- Same source data, projected through `PublicMemberProfileDto`.
- `currentRoute` from `IActiveRouteGateway.GetSummaryAsync(memberId)`; `null` until M2.
- Never include email/mobile. This endpoint is also the contract M4/M5 reuse.

### 8.3 `GET /me/profile/options`

- Read CFG-21, CFG-24, CFG-28, CFG-11, CFG-23 from Foundation config.
- No member-specific data; still authenticated to prevent anonymous scraping of platform settings.

### 8.4 `PUT /me/profile`

Request: `{ fullName, mobile }`.

Rules:

1. `fullName`: trim, collapse internal whitespace; after trim length 3–100, else `400 PROFILE_NAME_INVALID`.
2. `mobile`: optional. If provided and non-empty: digits only after removing spaces/hyphens; exactly 10 digits, else `400 PROFILE_MOBILE_INVALID`. Empty/null clears it. Visible only to the member and Admin (BR-10.6/D-21).
3. **Ignore or reject any attempt to change gender/email/role/vehicle through this endpoint.** If the client sends a `gender` property, reject with `400 GENDER_LOCKED` (AC-10.2). Use a DTO with no such properties; the model binder will ignore extras by default, so an explicit check is optional — but the UI must not offer the control.
4. Persist through `IMemberIdentityService.UpdateName/Mobile` in a transaction; audit each changed field with old/new value (BR-10.10).
5. Names keep history via audit; do not overwrite audit rows.
6. Return the refreshed `MyProfileDto`.

### 8.5 `POST /me/profile/photo` (multipart)

Pipeline:

1. Must be exactly one file.
2. Extension and content type must be JPG/JPEG or PNG; verify the **magic bytes** (JPEG `FF D8 FF`, PNG `89 50 4E 47`) — do not trust only `Content-Type`.
3. Size ≤ CFG-24 MB (default 5). Return the exact copy "Please upload a JPG or PNG up to 5 MB." via the error message (SC-10.2).
4. Store via `IMediaService.UploadTempAsync` then `AttachMediaAsync`, or the existing single-step media flow; on success set `raah_MemberProfile.PhotoMediaId` (a new upload replaces the old one, BR-10.5).
5. On any failure, the existing photo must remain unchanged.
6. Audit `PROFILE_PHOTO_CHANGED` with old/new media id.
7. Return `{ mediaId, photoUrl }`.

Errors: `400 PHOTO_REQUIRED`, `400 PHOTO_TYPE_INVALID`, `400 PHOTO_TOO_LARGE`.

### 8.6 `DELETE /me/profile/photo`

- Remove the profile photo (set `PhotoMediaId = null`, soft-delete/mark media), audit. The avatar falls back to initials in the UI. No spec rule forbids this; the UI exposes it as "Remove photo". If the client wants photo removal to be Admin-only, confirm as an open decision (UI plan UD-7).
- Return `{ photoMediaId: null }`.

### 8.7 `PUT /me/profile/vehicle`

- Allowed only when the current role is **Raahi**. If Saathi: `400 VEHICLE_NOT_APPLICABLE` (the UI hides the field).
- Value must be one of CFG-21. If CFG-21 is later edited, accept only current configured values; existing stored value remains readable.
- Mandatory for Raahi: never allow clearing it to empty (BR-5.12).
- Audit `VEHICLE_CHANGED` (old/new). If an Active route exists, the change applies to the member's active route from that moment (BR-10.8) — M2 reads the profile value at render time, so no route write is needed now; when M2 exists it may cache the vehicle, in which case call its gateway to refresh.
- Return refreshed `MyProfileDto`.

### 8.8 `POST /me/profile/email-change`

Request: `{ newEmail }`.

Rules (BR-10.4, V-13, AC-10.1 not applicable):

1. Normalize (trim, lower-invariant) and validate email shape → `400 EMAIL_INVALID`.
2. If it equals the current email (case-insensitive) → `409 EMAIL_UNCHANGED`.
3. If `IMemberIdentityService.IsEmailInUseAsync` returns true → `409 EMAIL_IN_USE` with "This email is already registered." (SC-10.5). Also reject if another **Pending** request for any member already holds it.
4. Enforce resend cooldown CFG-23 (seconds) between requests for the same member → `429 EMAIL_CHANGE_COOLDOWN` with seconds remaining.
5. Cancel any older Pending request for this member (Status = Cancelled), then create a new Pending row.
6. Generate a 32-byte cryptographically random token; email only the raw token; store only its SHA-256 hex hash.
7. Expiry = now + CFG-11 hours.
8. Send the verification link through `IEmailSender`.
9. Audit `EMAIL_CHANGE_REQUESTED` (old email, masked new email — do not write the raw token).
10. The **old email stays the login email** until verification succeeds.
11. Return `202 { maskedNewEmail, expiresAtUtc, message }`.

Do not reveal whether the new email belongs to another account beyond the specified "already registered" message.

### 8.9 `POST /auth/email-change/verify`

- `[AllowAnonymous]` because the link is opened from email (the user may not be logged in). The one-time token is the credential.
- Hash the supplied token, find a Pending row with a matching hash.
- Cases:
  - Not found → `400 EMAIL_TOKEN_INVALID`.
  - `ConsumedAt` set → `409 EMAIL_TOKEN_USED`.
  - `ExpiresAtUtc < now` → `410 EMAIL_TOKEN_EXPIRED`; mark Status = Expired.
- On success, inside one transaction:
  1. Re-check `IsEmailInUseAsync` (race protection); if now taken → `409 EMAIL_IN_USE`, cancel request.
  2. `IMemberIdentityService.UpdateEmailAsync(memberId, newEmail)`.
  3. Mark row Verified, `ConsumedAtUtc = now`.
  4. Audit `EMAIL_CHANGED` with old/new.
  5. Cancel other Pending rows for the member.
- Old email becomes free for a new registration (SC-10.4).
- Return `{ email, message }`.

Resend of the email-change link is intentionally **not** a separate endpoint in M1 unless Foundation wants it; the member can re-submit after the cooldown. If added, reuse the same cooldown and validity rules.

### 8.10 `GET /me/profile/role-change/preflight`

Returns, for the current member:

```
currentRole                  "Saathi" | "Raahi"
targetRoles                  [ "Raahi" ]            // the other role
hasActiveRoute               bool                   // gateway; false until M2
activeRouteSummary           { routeNumber, startCity, endCity } | null
willWithdrawLeads            true                   // when an active route exists
willWithdrawPendingRequests  true
keepsConnections             true                   // A-6
requiresVehicle              bool                   // target Raahi && no vehicle
```

This exists so the UI can show exact consequences before a destructive step (UP-5, BR-10.7).

### 8.11 `POST /me/profile/role-change`

Request: `{ newRole, vehicleType?, confirmDeactivateActiveRoute }`.

Rules (BR-10.7, D-14, AC-10.3, AC-10.4):

1. `newRole` must be Saathi or Raahi and must differ from the current role → `400 ROLE_INVALID` / `409 ROLE_UNCHANGED`.
2. If `IActiveRouteGateway.GetActiveRouteSummaryAsync` returns a route:
   - If `confirmDeactivateActiveRoute` is false → `409 ROUTE_DEACTIVATION_REQUIRED` with a payload naming the route and consequences. **Do not** deactivate anything in this call.
   - If true → deactivate the Active route through the gateway (this withdraws leads and pending requests, keeps connections/chats; the gateway returns counts).
3. If `newRole = Raahi` and the profile has no vehicle: `vehicleType` is mandatory from CFG-21 → `400 VEHICLE_REQUIRED`. Do not proceed without it (AC-10.4).
4. If `newRole = Saathi`, keep the stored vehicle value for later but the UI/API stop showing it; vehicle is not required.
5. Persist `JourneyRole` and (when provided) `VehicleType` atomically **after** successful route deactivation.
6. Audit `ROLE_CHANGE_REQUESTED` and `ROLE_CHANGED` (old/new role, vehicle, route id deactivated). Vehicle change, if any, is audited as `VEHICLE_CHANGED`.
7. Respond `200 { journeyRole, vehicle, routeDeactivated, nextStep: "CreateRoute" }`.
8. The member creates the new route in M2; M1 does not create routes.

**Role-effective timing.** The spec's Data Impact line reads "journey role updated when the new route is created", but BR-10.7 step 4 only says the member *then* creates the new route. This plan **persists `JourneyRole` at role-change time** (rule 5) so that M2 can create a route of the new type and the shell role chip updates immediately. Interpret the Data Impact wording as "the role takes effect operationally once the new route exists"; the member has no Active route between the two steps, so no matching/lead rule is violated. If product intends the role to remain the old value until the M2 route is activated, switch rule 5 to store a `PendingJourneyRole` and let M2 commit it — recorded as an open decision (§17).

Failure isolation: if route deactivation fails, do not change the role. If role persistence fails after deactivation, the route is already off but the role remains unchanged; return `500` and log a compensating warning (the member can retry; the route is inactive, which is recoverable).

### 8.12 `POST /me/profile/deactivate`

Request: `{ confirm: true }`.

Rules (BR-10.9, A-25):

1. `confirm` must be true → `400 CONFIRMATION_REQUIRED`.
2. In one orchestrations sequence (order matters for safety):
   1. `IActiveRouteGateway.DeactivateActiveRouteAsync` (route Inactive; leads and pending requests withdrawn).
   2. `IPendingRequestGateway.WithdrawAllPendingAsync`.
   3. `IConnectionGateway.CloseAllAsync` (connections Disconnected, chats closed).
   4. `IMemberStatusService.SetInactiveAsync(memberId, reason = "Member self-deactivation")`.
   5. `ISessionRevoker.RevokeAllSessionsAsync(memberId)`.
3. Audit `ACCOUNT_SELF_DEACTIVATED` with the effects and counts.
4. Return `200 { message: "Your account has been deactivated." }`.
5. Only Admin can reactivate (M10); M1 provides no reactivation endpoint.
6. After the response, the client clears its auth cookies and returns to login.

Idempotency: if the account is already Inactive, return `409 ACCOUNT_NOT_ACTIVE` rather than repeating the side effects.

---

## 9. Authorization and status rules

| Caller / state | Allowed |
|---|---|
| Anonymous | Only `POST /auth/email-change/verify` (token is the credential) |
| Member, Active | All M1 endpoints |
| Member, Restricted | All M1 endpoints (Restricted keeps access; BR-4.8). Restrictions affect requests, not profile |
| Member, Unverified | `GET /me/profile`, `GET /me/profile/options`, photo/name/mobile edits (safe), email re-verification. The UI gate still hides the app. M1 does not create routes or leads (BR-10.2) |
| Member, Inactive | Rejected by session/status validation (BR-5.4/5.9) |
| Blocked (temp/permanent) | Rejected at login/session |
| Admin | No member self-service endpoints. Admin profile changes go through M10 with a reason (BR-10.3, BR-4.5) |
| Other member | Only the public projection through M4/M5 access checks; no M1 endpoint returns another member's private data |

Enforce the role check in the controller/action (`RoleId` Member), and enforce record ownership by always deriving the member from the token — never accept a `memberId` in the body for self-service routes.

Return `StatusCode(403)` (a `StatusCodeResult`) for a plain role reject, not `Forbid()`.

### 9.1 Member status lifecycle (spec §9.1)

M1 triggers only the Member transitions below; Restricted and Blocked are M12.

| From | To | Who | M1 mechanism |
|---|---|---|---|
| Unverified | Active | System | Foundation email-link verification (not an M1 endpoint); M1 only reads the status |
| Active | Inactive | Member | `POST /me/profile/deactivate` → `IMemberStatusService.SetInactiveAsync` (BR-10.9) |
| Active | Inactive | Admin | M10 admin deactivation with reason (not M1) |
| Inactive | Active | Admin only | M10 reactivation with reason (not M1); M1 has no reactivation endpoint |

Do not add any M1 endpoint that moves a member to Active — that is Foundation verification (Unverified→Active) or M10 (Inactive→Active).

---

## 10. Validation matrix

| ID | Rule | Where | Error |
|---|---|---|---|
| V-11 | Photo JPG/PNG, ≤ CFG-24 MB | Photo endpoint | `PHOTO_TYPE_INVALID` / `PHOTO_TOO_LARGE` |
| V-12 | Gender locked after registration | Edit profile (no field) + Admin only path | `GENDER_LOCKED` if forced |
| V-13 | Email change requires verification of the new address | Email-change + verify endpoints | `EMAIL_TOKEN_*` |
| — | Name 3–100 characters after trim | PUT profile | `PROFILE_NAME_INVALID` |
| — | Mobile optional, exactly 10 digits | PUT profile | `PROFILE_MOBILE_INVALID` |
| — | Raahi must have a vehicle | Vehicle endpoint, role change, profile completeness | `VEHICLE_REQUIRED` / `VEHICLE_NOT_APPLICABLE` |
| — | Vehicle must be from CFG-21 | Vehicle endpoint, role change | `VEHICLE_INVALID` |
| — | New email unique, valid, different | Email-change + verify | `EMAIL_INVALID` / `EMAIL_IN_USE` / `EMAIL_UNCHANGED` |
| — | One Active/one pending verification at a time | Email-change | cancel older Pending |
| — | Self-deactivation requires confirmation | Deactivate | `CONFIRMATION_REQUIRED` |

Existing repo has no validation framework; implement these checks in `MemberProfileService` and unit-test them. Do not add FluentValidation as a drive-by change.

---

## 11. Audit events (Section 8, BR-10.10)

| Event | Trigger | Old → New recorded |
|---|---|---|
| `PROFILE_NAME_CHANGED` | PUT profile | old name → new name |
| `PROFILE_MOBILE_CHANGED` | PUT profile | old mobile (masked in logs) → new mobile |
| `PROFILE_PHOTO_CHANGED` | Photo upload/replace | old media id → new media id |
| `PROFILE_PHOTO_REMOVED` | Photo delete / Admin removal | old media id → null |
| `EMAIL_CHANGE_REQUESTED` | Email-change request | old email → masked new email (no token) |
| `EMAIL_CHANGED` | Verify success | old email → new email |
| `VEHICLE_CHANGED` | Vehicle PUT / role change | old vehicle → new vehicle |
| `ROLE_CHANGE_REQUESTED` | Role change accepted for processing | old role → new role (route context) |
| `ROLE_CHANGED` | Role persisted | old role → new role |
| `ACCOUNT_SELF_DEACTIVATED` | Deactivate | Active → Inactive + effect counts |

Every event includes actor (member), actor role, UTC timestamp, and source IP where the Foundation audit convention includes it. Audit is append-only (BR-8.3). Never write the raw verification token to audit or logs.

---

## 12. Config usage

| CFG | Use in M1 |
|---|---|
| CFG-11 | Email-change verification link validity (hours) |
| CFG-21 | Vehicle options offered (Bike/Car); validate against it |
| CFG-23 | Email-change/resend cooldown (seconds) |
| CFG-24 | Max profile photo size (MB) |
| CFG-28 | Minimum ratings before the average is shown (`isNewMember`) |

Read config through the Foundation reader; do not hard-code defaults in M1 (defaults live in Foundation/M13).

---

## 13. Error responses

Use `BadRequest(new { code, message })`, `StatusCode(409, ...)` via object results, or `NotFound` — all are preserved by `ResponseFilterAttribute`. Never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `PROFILE_NAME_INVALID` | 400 | "Enter a name between 3 and 100 characters." |
| `PROFILE_MOBILE_INVALID` | 400 | "Enter a 10-digit mobile number." |
| `GENDER_LOCKED` | 400 | "Gender cannot be changed. Contact support to correct it." |
| `PHOTO_REQUIRED` | 400 | "Choose a photo to upload." |
| `PHOTO_TYPE_INVALID` | 400 | "Please upload a JPG or PNG up to 5 MB." |
| `PHOTO_TOO_LARGE` | 400 | "Please upload a JPG or PNG up to 5 MB." |
| `VEHICLE_REQUIRED` | 400 | "Choose Bike or Car to continue as a Raahi." |
| `VEHICLE_INVALID` | 400 | "Choose a valid vehicle." |
| `VEHICLE_NOT_APPLICABLE` | 400 | "Vehicle applies only to a Raahi." |
| `EMAIL_INVALID` | 400 | "Enter a valid email address." |
| `EMAIL_IN_USE` | 409 | "This email is already registered." |
| `EMAIL_UNCHANGED` | 409 | "This is already your email address." |
| `EMAIL_CHANGE_COOLDOWN` | 429 | "Please wait before requesting another email change." |
| `EMAIL_TOKEN_INVALID` | 400 | "This link is invalid. Request a new email change." |
| `EMAIL_TOKEN_USED` | 409 | "This link has already been used." |
| `EMAIL_TOKEN_EXPIRED` | 410 | "This link has expired. Request a new email change." |
| `ROLE_INVALID` | 400 | "Choose Saathi or Raahi." |
| `ROLE_UNCHANGED` | 409 | "You already have this journey role." |
| `ROUTE_DEACTIVATION_REQUIRED` | 409 | "Turn off your current route before changing role." |
| `CONFIRMATION_REQUIRED` | 400 | "Confirm that you understand what will happen." |
| `ACCOUNT_NOT_ACTIVE` | 409 | "This account is not active." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |
| `NOT_FOUND` | 404 | "Profile not found." |

Keep messages aligned with the UI plan's microcopy.

---

## 14. Privacy and security

- Public DTO is a hard allow-list (Section 6.2). Add an automated serialization test that fails if `email`, `mobile`, `phone`, `ip`, `device`, `loginHistory`, `reports`, `adminNotes`, or `blocked` appear.
- Never log or audit raw tokens, passwords, or full mobile numbers in plain error logs; mask mobile as `••••••1234` if logging is unavoidable.
- Photo validation uses magic bytes plus extension; reject SVG, PDF, and mismatched content.
- Email-change tokens: 32 random bytes, SHA-256 hashed at rest, single-use, expiry from CFG-11, invalidated when a newer request is made.
- Self-service routes derive the member from the token only.
- Rate-limit email-change requests by member (CFG-23) and consider an IP-level throttle at the gateway.
- CSRF: the app uses JWT/cookies; follow the existing auth cookie/CSRF posture from Foundation for state-changing calls.
- Optimistic concurrency (row version or `ModifiedDate` check) on `raah_MemberProfile` updates to avoid lost updates from two devices.

---

## 15. Build sequence (M1)

| Step | Work | Output |
|---|---|---|
| 1 | Add enums, DTOs, `raah_MemberProfile` + `raah_MemberEmailChange` entities/configurations | Compiles, migrations created |
| 2 | Add Foundation contract interfaces needed by M1; add no-op gateways for route/request/connection/rating | DI resolvable |
| 3 | `IMemberProfileService` + `MemberProfileService` (`[AutoRegister]`): read, update, options, preview | Unit-testable, no controller |
| 4 | Email-change service (request, verify, cooldown, token hashing) + email sender call | Email flow works |
| 5 | Photo upload/remove via `IMediaService` with validation | SC-10.1, SC-10.2 pass |
| 6 | Role-change preflight + change orchestration (gateway no-ops) | AC-10.3/10.4 logic in place |
| 7 | Self-deactivation orchestration + session revoke + logout | SC-10.10 |
| 8 | `MemberProfileController` endpoints + route wiring + error codes | Postman-ready |
| 9 | Audit calls on every mutation | Audit rows verified |
| 10 | Public projection service + privacy test | M4/M5 contract ready |
| 11 | Replace no-op gateways when M2/M5/M7 arrive | Integration complete |

Suggested folders:

```
Binsera.Core/Members/
  Domain/Raah/MemberProfile.cs
  Domain/Raah/MemberEmailChange.cs
  Enums/JourneyRole.cs, VehicleType.cs, EmailChangeStatus.cs
  Models/Raah/*Dto.cs, *Request.cs
  Contracts/IMemberProfileService.cs, IPublicMemberProfileService.cs
  Contracts/Gateways/IActiveRouteGateway.cs, IPendingRequestGateway.cs,
                     IConnectionGateway.cs, IRatingSummaryGateway.cs
Binsera.Application/Members/Services/
  MemberProfileService.cs, MemberEmailChangeService.cs, PublicMemberProfileService.cs
  Gateways/NoOpActiveRouteGateway.cs, ...
Binsera.API/Controllers/Profile/MemberProfileController.cs
Binsera.Persistence/Configurations/Raah/MemberProfileConfiguration.cs,
                              MemberEmailChangeConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahMemberProfile.cs
```

---

## 16. Test plan

### 16.1 Unit

- Name trim/length boundaries (2, 3, 100, 101).
- Mobile: empty clears, valid 10 digits, letters/symbols/9/11 digits rejected.
- Photo: valid JPEG/PNG; PDF, SVG, renamed file, > CFG-24 MB, 0-byte rejected; failure leaves old photo.
- Email: invalid format, same email, email owned by another member, email held by another Pending request, cooldown, expiry, reuse, verify success changes email and frees old.
- Role: same role rejected; active route requires confirmation; Raahi without vehicle requires vehicle; CFG-21 invalid value; Saathi→Raahi and Raahi→Saathi happy paths; gateway failure does not change role.
- Vehicle: Saathi rejected; Raahi valid/invalid; cannot clear.
- Deactivate: confirm required; status Inactive; side-effect counts; already-inactive conflict; sessions revoked.
- Completeness: Saathi complete with name+verified; Raahi requires vehicle; photo/mobile optional.
- Rating: below CFG-28 → `isNewMember`; at/above → average shown.

### 16.2 Integration / API

- Every endpoint under auth: missing token → 401; wrong role → 403; inactive → rejected.
- Response envelope present and `data` shape matches DTO.
- `GET /me/profile/preview` JSON contains no email/mobile (privacy regression).
- Audit row per mutation with old/new.
- Idempotency/conflict codes from Section 13.
- Migration applies cleanly and does not touch `member_Master`.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-10.1 Mobile never exposed | Public DTO privacy test; preview endpoint; M4/M5 reuse |
| AC-10.2 Gender locked | Edit DTO has no gender; `GENDER_LOCKED` on forced change |
| AC-10.3 Role switch needs route deactivation | Role-change 409 without confirm; deactivation via gateway |
| AC-10.4 Vehicle required for Raahi | Role-change `VEHICLE_REQUIRED`; vehicle endpoint |

Scenarios SC-10.1 … SC-10.10 map to the endpoints above; SC-10.5 duplicate email → `EMAIL_IN_USE`; SC-10.8 vehicle change → `VEHICLE_CHANGED` audit; SC-10.9 admin gender correction is M10, not M1.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| UD-7 | How does a member ask support to correct gender? | UI shows "Contact support"; support route is a copy decision, not an API |
| — | Is mobile editable after verification? | Yes (spec says editable); no re-verification required |
| — | Can a member remove their photo? | Yes, `DELETE /me/profile/photo`; confirm if product wants Admin-only |
| — | Should vehicle be cleared when switching to Saathi? | Keep stored, hide it; required again only if returning to Raahi |
| — | Reuse Foundation email-verification token table or the M1 table? | Dedicated M1 table (clearer audit and reason) |
| — | Global API prefix `api`? | Keep existing no-prefix convention |
| — | Error-code mechanism (`ResponseErrorCodeType` int) | Add M1 codes to the shared enum; keep HTTP status authoritative |
| — | Is the journey role effective at role-change time or only when the M2 route is created? | Persist immediately at role-change (§8.11); switch to `PendingJourneyRole` if product prefers the literal Data Impact wording |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| BR-10.1 complete on verification | §8.1 `profileComplete`, §16.1 |
| BR-10.2 Active gate | §9 |
| BR-10.3 gender locked | §8.4, §10 V-12 |
| BR-10.4 email change | §8.8, §8.9 |
| BR-10.5 photo rules | §8.5, §10 V-11 |
| BR-10.6 mobile hidden | §6.2, §14 |
| BR-10.7 role change | §8.10, §8.11 |
| BR-10.8 vehicle change | §8.7 |
| BR-10.9 self-deactivation | §8.12 |
| BR-10.10 audit | §11 |
| SCR-10.1…10.5 | §7 endpoint catalogue |
| Data Impact | §4, §8.11, §8.12, §11 |
| RPT-1 User & Access Report | §1 Reports note |
| Status lifecycle (§9.1) | §9.1 |
| CFG-21/24/28 (+11/23) | §12 |
| V-11/V-12/V-13 | §10 |
