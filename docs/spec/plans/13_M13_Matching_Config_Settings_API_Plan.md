# M13 — Matching Configuration & Platform Settings — API Plan

| | |
|---|---|
| **Module** | M13 — Matching Configuration & Platform Settings (spec `docs/spec/13_M13_Matching_Config_Settings.md`, functional specification Section 22) |
| **Document type** | API implementation plan: setting registry, change history, Test Match, recalculation entry points, legal content versioning and acceptance, City Master and city requests, validation, audit, sequencing, tests |
| **Build order** | #13 after Foundation (config reader, audit, numbering), M2 (cities used by routes), M3 (matching engine + Test Match service) (see `00_Execution_Plan.md`) |
| **Repo conventions** | ASP.NET Core, `ApiControllerBase`, `[AutoRegister(includeInterfaces: true)]`, `IUnitOfWork`/`BinseraDbContext`, EF migrations, global `ResponseFilterAttribute` envelope |
| **Companion** | UI plan: `13_M13_Matching_Config_Settings_UI_Plan.md` |
| **Status** | Plan for review — no code written yet |

---

## 1. Scope

M13 is the **Admin control panel for platform behaviour and content**. It owns the write side of every configuration value, the permanent record of changes, the sample-route Test Match tool, the versioned legal documents and their acceptance records, and the City Master (including member city suggestions).

1. **Configuration** — all CFG-1…CFG-32 values grouped, with current value, allowed range and default; a batch save with an optional remark; security settings require a second confirmation (SCR-22.1, BR-22.5, BR-22.9).
2. **Change History** — every change with setting, old value, new value, Admin, time and remark (SCR-22.2, BR-22.1, RPT-26).
3. **Test Match** — two sample legs (cities, points, days, time) evaluated under the current settings; no member data, no lead, no side effect (SCR-22.3, A-51).
4. **Recalculate all leads** — a confirmed full matching run; leaves pending requests and connections untouched; the run and its counts are recorded (BR-22.4, A-52, RPT-28). The engine work and run table belong to M3; M13 owns the Admin entry point and run view.
5. **Legal Content** — edit, preview, publish and version the Terms of Use, Privacy Policy and Safety Guidelines; the supplied sample is labelled **Sample 1**; one Published version per document; re-acceptance is optional and recorded per member (SCR-22.4, D-24, BR-22.12–BR-22.17, RPT-29).
6. **City Master** — add, edit, deactivate and reactivate cities; names unique ignoring capitals and spaces; never deleted; deactivation refused while a Draft or Active route uses the city (SCR-22.5, D-26, BR-22.18–BR-22.20, RPT-30).
7. **City requests** — member suggestions listed, added or declined with a reason, with an in-app notice to the member (SCR-22.6, A-56, BR-22.21).
8. **Read contracts for other modules** — `IAppConfigReader`, `ICityDirectory`, `ICityRequestService`, `ILegalAcceptanceService`, `ILegalAcceptanceGate`.

Out of scope (owned elsewhere):

| Concern | Owner |
|---|---|
| Configuration **read** contract used by every module | Foundation (`IAppConfigReader`) |
| Matching calculation, matching runs, lead writes | M3 / M4 |
| Route tables and route versions that reference a city | M2 |
| Member records, registration flow, session gate enforcement | Foundation / M1 |
| Notification delivery, templates and retry | M9 |
| Dashboard tiles and the sample-legal alert surface | M14 |
| Report catalogue, exports and scheduled reports (RPT-26/28/29/30) | M15 |
| Admin role and authentication | Foundation |
| Member-facing legal page layout and acceptance screen design | UI Plan W-1.5 / public legal pages; data supplied by M13 |

**Hard non-goals:** settings are **never deleted** (BR-22.10); legal versions are **never deleted** (BR-8.1, BR-22.17); a city is **never deleted** (BR-22.20); acceptance records are never edited or deleted (BR-22.15); there is **no** endpoint that creates a member or a lead; Test Match **never reads `member_Master`** (AC-22.5); a settings change **never rewrites** completed records or existing leads (BR-22.2, BR-22.3).

---

## 2. Module boundaries

### 2.1 Contracts consumed

| Contract | Source | Methods used by M13 |
|---|---|---|
| `IAppConfigReader` | Foundation | `GetAsync<T>(key)`, `GetAllAsync()`, `GetSnapshotAsync()`; used by matching, auth, routes, chat, etc. |
| `IAppSettingStore` | Foundation (or M13 fallback — UD-API-1) | `GetAllRowsAsync`, `UpsertManyAsync`, `GetRowAsync(key)` |
| `IAuditService` | Foundation | append-only audit for every change, publish, city action and decision |
| `IClock` | Foundation | UTC time, India-time effective dates, change timestamps |
| `IOutboxPublisher` | Foundation | enqueue `LegalReacceptanceRequiredEvent` and `SuggestedCityDecidedEvent` inside the business transaction |
| `INotificationDispatcher` | M9 | direct Admin-action notices when an in-app notice is required outside the outbox path |
| `IMemberEligibilityReader` | Foundation / M10 | count and list members eligible to log in, for the re-acceptance fan-out and report |
| `IRouteCityUsageReader` | M2 | routes using a city as Start/End; city stats (active routes, distinct members) |
| `IMatchingEngine` | M3 | `RunAllAsync(trigger: ManualRecalculation, adminId, remark)` |
| `IMatchingRunReader` | M3 | `GetRunAsync(runId)`, `ListRunsAsync(query)` |
| `ITestMatchService` | M3 | `RunAsync(TestMatchRequest, ct)` — pure calculator on sample text |
| `IRecordNumberGenerator` | Foundation | optional `CFG`, `LEG` and `CITY` numbers (see UD-API-15) |

### 2.2 Contracts provided by M13

| Contract | Purpose | Consumer |
|---|---|---|
| `IAppConfigAdminService` | grouped configuration screen, batch save, catalog metadata, defaults | Admin UI |
| `IConfigHistoryQuery` | change history list and filters (RPT-26) | Admin UI, M15 |
| `IAppConfigWriter` | typed write path used by the Admin service; clears the config cache after commit | M13 internals |
| `IMatchingRecalculationService` | confirmed full recalculation and run read | Admin UI, M14 |
| `ILegalContentAdminService` | draft save, publish, version history, acceptance summary | Admin UI |
| `ILegalContentPublicQuery` | current published documents for the public pages and My Account | Public pages, M1/M14 |
| `ILegalAcceptanceService` | pending status, record acceptance at registration and at re-acceptance | Foundation registration, member UI |
| `ILegalAcceptanceGate` | `HasPendingAsync(memberId)`/`GetPendingAsync(memberId)` for the login gate | Foundation auth middleware |
| `ILegalContentAlertReader` | `AnySampleCurrentAsync()` and unaccepted counts | M14 dashboard |
| `ICityDirectory` | active cities for route dropdowns (already declared by M2) | M2, M11 |
| `ICityAdminService` | City Master list, add, edit, deactivate, reactivate, usage | Admin UI |
| `ICityRequestService` | member submits a missing city (already declared by M2) | M2 |
| `ICityRequestAdminService` | city request list, add, decline | Admin UI, M15 |

### 2.3 Rules

- **Read vs write split.** Every module reads through the cached Foundation `IAppConfigReader`; only M13 writes. A successful save invalidates the cache before the response is returned.
- **One store, one registry.** Values live in `raah_AppSetting`; labels, groups, defaults, types and allowed ranges live in a code registry (`SettingDefinitionRegistry`) because ranges are safety rules, not editable content.
- **M13 does not run matching itself.** It calls M3's `IMatchingEngine.RunAllAsync`; the run counters live in M3's `raah_MatchingRun`.
- **M13 does not write route or member tables.** City usage and member counts are read through M2's reader; eligibility counts through Foundation.
- **Legal documents are fixed to three types.** M13 stores versions, not a mutable document list. A new document type is a product change, not Admin content.
- **City names are canonical by id.** Routes point to `CityId`; a name correction updates the city row and therefore every referencing route automatically.
- **All writes are transactional.** Config batch, legal publish + outbox event, and city decision + outbox event commit or roll back together.

---

## 3. Existing repository conventions to follow

| Topic | Convention |
|---|---|
| Controller | Inherit `ApiControllerBase`; route prefix below; services via primary constructor |
| DI | `[AutoRegister(includeInterfaces: true)]`; interfaces in `Binsera.Core`; no manual `AddScoped` |
| Response | `Ok(payload)` wrapped by `ResponseFilterAttribute` |
| Non-2xx | `BadRequest(new { code, message })`, `StatusCode(409, new { code, message })`, `NotFound(new { code, message })`; **never** `Forbid()` / `Challenge()` (swallowed to 200) |
| Persistence | `IUnitOfWork` / `BinseraDbContext`; new EF-managed tables; `member_Master` untouched |
| Migrations | `yyyyMMddHHmmss_DescriptiveName`, `.cs` + `.Designer.cs`, `long Id` PK, decimal precision, audit columns, `IsDeleted` |
| Validation | No FluentValidation; validate inside the application service; unit-test the rules |
| Audit | Append-only, old/new + reason + actor + UTC (Section 10) |
| Current user | `UserId`, `UserName`, `RoleId` from `ApiControllerBase`; Admin id is the actor |
| Admin gate | Enforce the Admin role in the controller; a plain reject uses `StatusCode(403)`; log denied attempts (BR-22.11) |
| Public routes | No auth; no member identity in request or response |
| Background job | None added by M13; recalculation is an on-demand M3 run |

**Route prefixes (no `/api`):**

- Admin: `/admin/config`, `/admin/legal`, `/admin/cities`, `/admin/city-requests`, `/admin/matching` (M3 routes mounted here).
- Member: `/me/legal`.
- Public: `/legal`.

---

## 4. Data model

### 4.1 `raah_AppSetting`

One row per CFG value. A row is **never deleted**; CFG-3 is seeded and read-only.

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `SettingKey` | nvarchar(10) | `CFG-1` … `CFG-32`, unique |
| `ValueText` | nvarchar(500) | Canonical serialized value: number as invariant digits, boolean as `true`/`false`, text as-is, time as `HH:mm`, multi-select as a sorted comma list |
| `ValueType` | tinyint | `AppSettingValueType` (§4.7) |
| `ModifiedOnUtc` | datetime2 | Last value change |
| `ModifiedByAdminId` | bigint | Last Admin |
| `RowVersion` | rowversion | Optimistic concurrency for two Admins |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Indexes: unique `SettingKey`; `(ModifiedOnUtc)`.

Seed all 32 keys with their Section 6 defaults in the migration (`HasData`). Metadata (label, group, range) is **not** stored here.

### 4.2 `raah_AppConfigChange`

The permanent change record behind SCR-22.2 and RPT-26. One row per **changed key**, sharing a batch id for one save.

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `ChangeBatchId` | uniqueidentifier | Groups keys saved together |
| `SettingKey` | nvarchar(10) | FK-like reference; no cascade |
| `GroupKey` | nvarchar(40) | Denormalized for reports |
| `OldValue` | nvarchar(500) null | Null when the key had no stored row before the first save |
| `NewValue` | nvarchar(500) | |
| `Remark` | nvarchar(500) null | The batch remark, repeated per row |
| `ChangedByAdminId` | bigint | Actor |
| `ChangedByAdminName` | nvarchar(120) | Snapshot for reports |
| `ChangedOnUtc` | datetime2 | |
| `SourceIp` | nvarchar(64) null | Optional security context |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Indexes: `(ChangedOnUtc DESC)`, `(SettingKey, ChangedOnUtc DESC)`, `(ChangeBatchId)`, `(ChangedByAdminId, ChangedOnUtc DESC)`.

### 4.3 `raah_LegalDocumentVersion`

The three fixed documents share this table; `DocumentType` distinguishes them. The supplied text is a seeded **sample** row.

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `DocumentType` | tinyint | `LegalDocumentType` (§4.7): Terms, Privacy, Safety |
| `VersionNo` | int | `0` for the supplied sample; `1..n` for real versions (UD-API-3) |
| `VersionLabel` | nvarchar(40) | `Sample 1` or `Version {n}` |
| `Title` | nvarchar(150) | |
| `BodyHtml` | nvarchar(max) | Sanitized subset: headings, paragraphs, lists, links |
| `EffectiveDate` | date | India-time date; not earlier than today when saved/published |
| `RequiresReAcceptance` | bit | Yes forces the gate for this version |
| `Status` | tinyint | `LegalDocumentStatus`: Draft or Published |
| `IsCurrent` | bit | Exactly one current Published row per document type |
| `IsSample` | bit | True only for the seeded sample |
| `CreatedByAdminId` | bigint | Draft author |
| `CreatedOnUtc` | datetime2 | |
| `PublishedByAdminId` | bigint null | |
| `PublishedOnUtc` | datetime2 null | |
| `PublishRemark` | nvarchar(500) null | Optional remark shown in history |
| `RetiredOnUtc` | datetime2 null | When a later version became current |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Indexes: `(DocumentType, IsCurrent)` filtered/unique equivalent enforced in service and by a filtered unique index; `(DocumentType, VersionNo)` unique; `(DocumentType, Status, VersionNo DESC)`.

Rules:

- Only one `IsCurrent = 1` row per `DocumentType`.
- Publishing the current draft retires the previous current version (`IsCurrent = 0`, `RetiredOnUtc = now`), but keeps its `Status = Published` and full body.
- At most **one Draft** per document type (UD-API-4). Saving an existing draft updates it; publishing promotes it.
- Sample rows are seeded and remain readable; replacing the sample means publishing a non-sample version.

### 4.4 `raah_LegalAcceptance`

Append-only acceptance record: member + document version + time.

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint | FK → `member_Master.Id` |
| `DocumentType` | tinyint | Denormalized for fast gate queries |
| `VersionId` | bigint | FK → `raah_LegalDocumentVersion.Id` |
| `VersionNo` | int | Snapshot |
| `AcceptedOnUtc` | datetime2 | |
| `AcceptedFrom` | tinyint | `Registration` or `ReAcceptance` |
| `SourceIp` | nvarchar(64) null | Optional |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Indexes: unique `(MemberId, VersionId)`; `(MemberId, DocumentType, AcceptedOnUtc DESC)`; `(VersionId)`.

Rules: no update, no delete (BR-22.15, BR-22.17). Accepting the same version twice is idempotent — the existing row is returned, not duplicated.

### 4.5 `raah_City`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `Name` | nvarchar(60) | 2–60 characters, display form |
| `NormalizedName` | nvarchar(60) | Trim, lowercase invariant, remove all whitespace; unique |
| `State` | nvarchar(60) null | Optional |
| `Status` | tinyint | `CityStatus`: Active / Inactive; default Active |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Indexes: unique `NormalizedName`; `(Status, Name)`.

Rules:

- Duplicate check is on `NormalizedName` so `"New Delhi"`, `"new delhi"` and `"NewDelhi"` collide (BR-22.18, AC-22.10).
- Name correction updates `Name` and `NormalizedName`; routes keep the id (BR-22.20).
- No delete endpoint and no hard delete (BR-22.20).
- Activation/deactivation changes `Status` only; it never touches routes.
- An Inactive city is excluded by `ICityDirectory.GetActiveCitiesAsync`; Inactive routes keep their historical city reference.

### 4.6 `raah_CityRequest`

| Column | Type | Notes |
|---|---|---|
| `Id` | bigint identity PK | |
| `MemberId` | bigint | FK → `member_Master.Id`; the requester |
| `RequestedName` | nvarchar(60) | As typed by the member |
| `NormalizedName` | nvarchar(60) | Duplicate/open-request key |
| `Status` | tinyint | `CityRequestStatus`: Open / Added / Declined |
| `RequestedOnUtc` | datetime2 | |
| `DecidedByAdminId` | bigint null | |
| `DecidedOnUtc` | datetime2 null | |
| `DeclineReason` | nvarchar(300) null | Mandatory on decline; shown to the member |
| `CreatedCityId` | bigint null | Set when Added |
| `CreatedDate`, `CreatedBy`, `ModifiedDate`, `ModifiedBy` | audit | |
| `IsDeleted` | bit | Always false |

Indexes: `(Status, RequestedOnUtc)`; `(MemberId, NormalizedName, Status)`; `(NormalizedName)`.

Rules:

- A request cannot be raised for a name already in `raah_City`, active or inactive (`CITY_ALREADY_EXISTS`).
- One **Open** request per member per normalized name (BR-22.21).
- Decline requires a reason; the member is notified in-app. Decisions are final for that request.
- Added requests link to the created city and notify the member in-app.

### 4.7 Enums

```
// New (M13)
AppSettingValueType  { Number = 1, Boolean = 2, Text = 3, Time = 4, MultiSelect = 5 }
AppSettingGroup      { Matching = 1, RequestsConnections = 2, ChatRating = 3,
                       Safety = 4, Notifications = 5, SecurityContent = 6 }
LegalDocumentType    { TermsOfUse = 1, PrivacyPolicy = 2, SafetyGuidelines = 3 }
LegalDocumentStatus  { Draft = 1, Published = 2 }
LegalAcceptanceSource{ Registration = 1, ReAcceptance = 2 }
CityStatus           { Active = 1, Inactive = 2 }
CityRequestStatus    { Open = 1, Added = 2, Declined = 3 }
ConfigHistorySort    { NewestFirst = 1, OldestFirst = 2, Setting = 3, Admin = 4 }
CityListSort         { NameAsc = 1, Status = 2, ActiveRoutesDesc = 3, MembersDesc = 4 }
CityRequestSort      { OldestFirst = 1, NewestFirst = 2, MemberNameAsc = 3 }

// Reuse (M3) — never redefine
MatchingRunStatus    { Running = 1, Completed = 2, Failed = 3 }
```

### 4.8 Settings registry (all Section 6 keys)

Every row is exposed on SCR-22.1 with the range and default shown. **CFG-21 appears in Section 6 but not in the Section 22.4 table**; this plan includes it (UD-API-2).

| Key | Group | Label | Type | Default | Allowed range / rule |
|---|---|---|---|---|---|
| CFG-1 | Matching | Departure time tolerance (minutes) | Number | 30 | 0–120 |
| CFG-2 | Matching | Maximum points per leg | Number | 5 | 3–10 |
| CFG-3 | Matching | Minimum points per leg | Number | 2 | Fixed at 2; read-only |
| CFG-4 | Matching | Minimum shared points for a Useful Segment | Number | 2 | 2–CFG-2 |
| CFG-5 | Requests & connections | Request expiry (days) | Number | 30 | 1–365 |
| CFG-6 | Requests & connections | Maximum pending requests per member | Number | 10 | 1–100 |
| CFG-7 | Matching | Leads shown per page | Number | 20 | 5–100 |
| CFG-8 | Security & content | Session idle timeout (minutes) | Number | 30 | 5–240; extra confirmation |
| CFG-9 | Security & content | Maximum failed logins | Number | 5 | 3–10; extra confirmation |
| CFG-10 | Security & content | Account lock duration (minutes) | Number | 15 | 1–1440; extra confirmation |
| CFG-11 | Security & content | Link validity (hours) | Number | 24 | 1–168; extra confirmation |
| CFG-12 | Matching | Gender filter enabled | Boolean | Yes | Yes / No |
| CFG-13 | Chat & rating | Rating enabled | Boolean | Yes | Yes / No |
| CFG-14 | Chat & rating | Rating comment moderation | Boolean | Yes | Yes / No |
| CFG-15 | Safety | Reports that trigger an alert | Number | 3 | 1–20 |
| CFG-16 | Notifications | Notification channels | MultiSelect | In-app, Email | In-app always on; Email optional (SMS out of Phase 1) |
| CFG-17 | Requests & connections | Cool-off after rejection (days) | Number | 7 | 0–90 |
| CFG-18 | Matching | Travel days allowed | MultiSelect | Mon–Sun | Any subset, at least one |
| CFG-19 | Matching | Return leg mandatory | Boolean | No | Yes / No |
| CFG-20 | Matching | Leg filter enabled | Boolean | Yes | Yes / No |
| CFG-21 | Matching | Vehicle types offered to a Raahi | MultiSelect | Bike, Car | At least one; Phase 1 values Bike/Car only |
| CFG-22 | Matching | Text match threshold (%) | Number | 70 | 50–100 |
| CFG-23 | Security & content | Email resend cooldown (seconds) | Number | 60 | 10–600; extra confirmation |
| CFG-24 | Security & content | Maximum photo size (MB) | Number | 5 | 1–10 |
| CFG-25 | Requests & connections | Request message text | Text | Hello, I want to connect with you | 1–150 chars; no links, URLs, emails or 10-digit runs |
| CFG-26 | Chat & rating | Maximum chat message length | Number | 1000 | 100–5000 |
| CFG-27 | Chat & rating | Maximum rating comment length | Number | 300 | 50–1000 |
| CFG-28 | Chat & rating | Minimum ratings before average shows | Number | 3 | 1–20 |
| CFG-29 | Notifications | Expiry reminder (days before) | Number | 5 | 1–(CFG-5 − 1) |
| CFG-30 | Notifications | Unread chat email delay (hours) | Number | 12 | 1–72 |
| CFG-31 | Notifications | Daily digest time | Time | 07:00 | Any valid time |
| CFG-32 | Safety | Report overdue after (days) | Number | 3 | 1–30 |

**Cross-field validation** is applied to the merged values of the batch, not key-by-key:

- `CFG-4 <= CFG-2`.
- `CFG-29 <= CFG-5 - 1`.
- `CFG-18` has at least one day; `CFG-21` has at least one vehicle; `CFG-16` always contains `In-app`.
- `CFG-3` cannot change (`CONFIG_FIXED`).
- An out-of-range or cross-field failure returns `CONFIG_OUT_OF_RANGE` / `CONFIG_DEPENDENCY_INVALID` with the offending key and the range.

---

## 5. DTOs (Binsera.Core)

Suggested namespace: `Binsera.Core.Configuration.Models.Admin`, `Binsera.Core.Legal.Models`, `Binsera.Core.Cities.Models`.

### 5.1 Configuration screen (SCR-22.1)

```
ConfigScreenDto
  groups: [
    {
      group            string        // Matching | RequestsConnections | ...
      groupLabel       string
      description      string?
      settings: [
        {
          key              string       // CFG-1
          label            string
          description      string?
          valueType        string       // Number | Boolean | Text | Time | MultiSelect
          value            object       // number | boolean | string | string[] | "HH:mm"
          displayValue     string
          defaultValue     object
          displayDefault   string
          allowedRange     { min: number?, max: number?, maxFromKey: string?,
                             minFromKey: string?, options: string[]?, unit: string? }
          isChanged        bool         // differs from the stored value in this request context
          isEditable       bool         // false only for CFG-3
          requiresExtraConfirmation bool // CFG-8/9/10/11/23
          note             string?
        }
      ]
    }
  ]
  lastSavedOnUtc        DateTime?
  lastSavedBy           string?
  canRecalculate        bool
  pendingDraftLegalCount int
  sampleLegalAlert      bool
```

```
SaveConfigRequest
  changes: [ { key: string, value: object } ]
  remark: string?                   // <= 500
  acknowledgedSecurity: bool        // required when any CFG-8/9/10/11/23 changes

SaveConfigResultDto
  changedKeys: string[]
  batchId: Guid
  changedOnUtc: DateTime
  screen: ConfigScreenDto           // fresh values
  message: string
```

### 5.2 Change history (SCR-22.2, RPT-26)

```
ConfigHistoryQuery
  settingKey     string?
  group          string?
  changedBy      long?
  from, to       DateTime?
  sort           string?    // default NewestFirst
  page           int        // default 1
  pageSize       int        // default 25, max 100

ConfigHistoryItemDto
  changeId       long
  batchId        Guid
  settingKey     string
  settingLabel   string
  group          string
  groupLabel     string
  oldValue       string?
  newValue       string
  remark         string?
  changedBy      { adminId, name }
  changedOnUtc   DateTime

ConfigHistoryPageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters
  filterOptions       ConfigHistoryOptionsDto
```

`ConfigHistoryOptionsDto` → `settings [{key,label,group}]`, `admins [{adminId,name}]`, `sorts`, `pageSizes`.

### 5.3 Test Match (SCR-22.3)

Reuse M3's `TestMatchRequest` / `TestMatchResultDto`; M13 adds only the options call.

```
TestMatchOptionsDto
  cities            [{ cityId, name, state }]   // active cities only
  maxPointsPerLeg   int                          // CFG-2
  allowedDays       [ "Mon".."Sun" ]             // CFG-18
  timeTolerance     int                          // CFG-1
  textThreshold     int                          // CFG-22
  minSharedPoints   int                          // CFG-4
  noMemberDataNotice string

// M3 shape, included here for the screen contract
TestMatchRequest
  saathi: SampleLegInput
  raahi:  SampleLegInput

SampleLegInput
  startCityId   int
  endCityId     int
  points        string[]     // 2..CFG-2, Start first, End last
  days          string[]     // at least one
  departureTime string       // HH:mm

TestMatchResultDto            // exactly as M3
  eligible, eligibilityChecks:[{code,label,passed,detail}],
  legs:[{legType,passed,checks:[...],pointPairs:[{a,b,similarity}],classification}],
  result                      // FullRoute | UsefulSegment | NoMatch
```

### 5.4 Recalculation (BR-22.4, A-52, RPT-28)

```
RecalculateRequest
  confirm: bool        // must be true
  remark: string?      // <= 500

RecalculateAcceptedDto
  runId     long
  status    string     // Running
  message   string

MatchingRunDto         // exactly as M3
  runId, status, routesEvaluated, leadsCreated, leadsUpdated, leadsWithdrawn,
  startedAt, completedAt, errorSummary

MatchingRunListQuery
  status, from, to, page, pageSize
```

### 5.5 Legal Content (SCR-22.4, D-24)

```
LegalOverviewDto
  documents: [ LegalDocumentSummaryDto ]
  sampleAlert: bool                 // true while any current version is a sample
  pendingDraftCount: int
  lastPublishedOnUtc: DateTime?

LegalDocumentSummaryDto
  documentType          string      // TermsOfUse | PrivacyPolicy | SafetyGuidelines
  documentLabel         string
  currentPublished      LegalVersionSummaryDto?
  draft                 LegalVersionSummaryDto?
  versionCount          int
  unacceptedMemberCount int?        // only when the current version requires re-acceptance
  isSampleCurrent       bool

LegalVersionSummaryDto
  versionId, versionNo, versionLabel, title, effectiveDate,
  requiresReAcceptance, status, isCurrent, isSample,
  publishedBy, publishedOnUtc, retiredOnUtc, publishRemark,
  lastSavedOnUtc

LegalDocumentDetailDto
  documentType, documentLabel
  currentPublished      LegalVersionDetailDto?
  draft                 LegalVersionDetailDto?
  versions              LegalVersionSummaryDto[]   // newest first, Admin only
  sampleAlert           bool

LegalVersionDetailDto
  versionId, versionNo, versionLabel, title, bodyHtml, effectiveDate,
  requiresReAcceptance, status, isCurrent, isSample,
  publishedBy, publishedOnUtc, publishRemark, lastSavedOnUtc
```

```
SaveLegalDraftRequest
  title               string     // 1..150
  bodyHtml            string     // allowed HTML subset
  effectiveDate       DateTime   // >= today (India)
  requiresReAcceptance bool

PublishLegalVersionRequest
  versionId           long       // the draft to publish
  remark              string?    // <= 500
  confirm             bool       // required when requiresReAcceptance = true

PublishLegalVersionResultDto
  documentType
  version             LegalVersionSummaryDto
  previousVersionId   long?
  requiresReAcceptance bool
  membersToNotify     int
  publishedOnUtc      DateTime
  sampleAlert         bool
  message             string
```

```
AcceptanceSummaryDto
  documentType, versionId, versionNo, versionLabel, requiresReAcceptance
  eligibleMembers, acceptedMembers, pendingMembers, acceptancePercent
```

### 5.6 Legal public and member surfaces (BR-22.14–BR-22.16)

```
PublicLegalDocumentDto
  documentType, documentLabel, title, bodyHtml, effectiveDate,
  versionLabel, isSample

MemberLegalStatusDto
  requiresReAcceptance   bool
  pending: [ PublicLegalDocumentDto ]       // required current versions not yet accepted
  acceptedCurrent: [ { documentType, versionId, versionLabel, acceptedOnUtc } ]
  allCurrent: [ PublicLegalDocumentDto ]    // readable at any time

AcceptLegalRequest
  versionIds: long[]                        // only current published versions

AcceptLegalResultDto
  accepted: [ { documentType, versionId, versionLabel, acceptedOnUtc } ]
  alreadyAccepted: [ { documentType, versionId } ]
  requiresReAcceptance: bool                // false when nothing remains pending
  message: string
```

### 5.7 City Master (SCR-22.5, RPT-30)

```
CityListQuery
  search      string?
  status      string?       // Active | Inactive
  sort        string?       // default NameAsc
  page, pageSize

CityListItemDto
  cityId, name, state, status
  activeRoutes       int
  draftRoutes        int
  members            int     // distinct members with an Active route using the city
  createdOnUtc, lastChangedOnUtc

CityListPageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters

CreateCityRequest
  name        string        // 2..60
  state       string?       // <= 60
  status      string?       // default Active

UpdateCityRequest
  name        string
  state       string?

CityUsageDto
  cityId, name, status
  canDeactivate        bool
  totalRoutes          int
  blockingRoutes: [ { routeId, routeNumber, memberId, memberName, role, status } ]
  hiddenRouteCount     int     // when the list is capped

ActivateCityResultDto / DeactivateCityResultDto
  cityId, name, status, message
```

### 5.8 City requests (SCR-22.6, A-56)

```
CityRequestListQuery
  status        string?      // Open | Added | Declined; default Open
  search        string?      // member name/MBR or requested name
  from, to      DateTime?
  sort          string?      // default OldestFirst
  page, pageSize

CityRequestItemDto
  requestId
  member        { memberId, memberNumber, fullName, photoUrl }
  requestedName string
  requestedOnUtc DateTime
  status, statusLabel
  decidedBy     string?
  decidedOnUtc  DateTime?
  declineReason string?
  createdCity   { cityId, name }?
  isDuplicateNow bool         // the name now exists in the City Master

CityRequestPageDto
  items, page, pageSize, totalCount, totalPages, appliedFilters

AddCityRequestRequest
  name  string?               // optional correction; default requestedName
  state string?

DeclineCityRequestRequest
  reason string               // 10..300 after trim

CityRequestDecisionDto
  requestId, status, statusLabel, cityId?, message
```

---

## 6. Endpoint catalogue

Every `/admin/*` endpoint requires an authenticated **Admin**; member tokens get `403` and the attempt is logged (BR-22.11). `/legal/*` is public. `/me/legal/*` requires a member token.

### 6.1 Configuration and history (SCR-22.1, SCR-22.2)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 1 | GET | `/admin/config` | Grouped settings with value, range, default, flags | 200 `ConfigScreenDto` |
| 2 | PUT | `/admin/config` | Batch save with optional remark; security confirmation required | 200 `SaveConfigResultDto` |
| 3 | GET | `/admin/config/history` | Change history with filters (RPT-26) | 200 `ConfigHistoryPageDto` |
| 4 | GET | `/admin/config/history/options` | Settings, Admins, sorts for filters | 200 `ConfigHistoryOptionsDto` |
| 5 | POST | `/admin/config/history/export` | CSV of the filtered history (optional; M15 owns catalogue) | 200 file |

### 6.2 Test Match and recalculation (SCR-22.3, BR-22.4)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 6 | GET | `/admin/config/test-match/options` | Active cities, limits and thresholds | 200 `TestMatchOptionsDto` |
| 7 | POST | `/admin/config/test-match` | Evaluate two sample legs; no member data | 200 `TestMatchResultDto` |
| 8 | POST | `/admin/matching/recalculate` | Confirmed full recalculation; returns a run | 202 `RecalculateAcceptedDto` |
| 9 | GET | `/admin/matching/runs/{runId}` | Recalculation run summary | 200 `MatchingRunDto` |
| 10 | GET | `/admin/matching/runs` | Recalculation log (RPT-28) | 200 paged `MatchingRunDto` |

Routes 8–10 are the M3-owned contracts; M13 mounts them and builds the Admin services behind them. There is **no** synchronous recalculation endpoint that blocks the request.

### 6.3 Legal Content (SCR-22.4)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 11 | GET | `/admin/legal` | Three documents, current/draft summary, sample alert | 200 `LegalOverviewDto` |
| 12 | GET | `/admin/legal/{documentType}` | Document detail with versions | 200 `LegalDocumentDetailDto` |
| 13 | PUT | `/admin/legal/{documentType}/draft` | Create or update the single draft | 200 `LegalVersionDetailDto` |
| 14 | POST | `/admin/legal/{documentType}/publish` | Publish the draft; retires the previous current version | 200 `PublishLegalVersionResultDto` |
| 15 | GET | `/admin/legal/{documentType}/versions` | Version history, newest first | 200 `LegalVersionSummaryDto[]` |
| 16 | GET | `/admin/legal/{documentType}/versions/{versionId}` | Read any version, including retired ones | 200 `LegalVersionDetailDto` |
| 17 | GET | `/admin/legal/{documentType}/acceptance-summary` | Accepted / pending counts for the current version (RPT-29) | 200 `AcceptanceSummaryDto` |

### 6.4 Public and member legal surfaces

| # | Method | Route | Auth | Purpose | Success |
|---|---|---|---|---|---|
| 18 | GET | `/legal` | Public | Current published titles and metadata | 200 `PublicLegalDocumentDto[]` |
| 19 | GET | `/legal/{documentType}` | Public | Current published body | 200 `PublicLegalDocumentDto` |
| 20 | GET | `/me/legal/status` | Member | Pending re-acceptance and all current documents | 200 `MemberLegalStatusDto` |
| 21 | POST | `/me/legal/accept` | Member | Record acceptance of the given current versions | 200 `AcceptLegalResultDto` |
| 22 | GET | `/me/legal/acceptances` | Member | The member's own acceptance history | 200 acceptance rows |

### 6.5 City Master and city requests (SCR-22.5, SCR-22.6)

| # | Method | Route | Purpose | Success |
|---|---|---|---|---|
| 23 | GET | `/admin/cities` | City list with status, route and member counts | 200 `CityListPageDto` |
| 24 | GET | `/admin/cities/options` | Statuses, sorts, page sizes | 200 options |
| 25 | GET | `/admin/cities/{cityId}` | City detail | 200 `CityListItemDto` |
| 26 | POST | `/admin/cities` | Add a city | 201 `CityListItemDto` |
| 27 | PUT | `/admin/cities/{cityId}` | Correct name/state | 200 `CityListItemDto` |
| 28 | GET | `/admin/cities/{cityId}/usage` | Routes blocking deactivation and counts | 200 `CityUsageDto` |
| 29 | POST | `/admin/cities/{cityId}/deactivate` | Deactivate when unused by Draft/Active routes | 200 `DeactivateCityResultDto` |
| 30 | POST | `/admin/cities/{cityId}/activate` | Reactivate | 200 `ActivateCityResultDto` |
| 31 | POST | `/admin/cities/export` | CSV of the City Register (optional; RPT-30) | 200 file |
| 32 | GET | `/admin/city-requests` | Member suggestions with filters | 200 `CityRequestPageDto` |
| 33 | GET | `/admin/city-requests/options` | Statuses, sorts | 200 options |
| 34 | POST | `/admin/city-requests/{requestId}/add` | Create the city and mark the request Added | 200 `CityRequestDecisionDto` |
| 35 | POST | `/admin/city-requests/{requestId}/decline` | Decline with a reason and notify the member | 200 `CityRequestDecisionDto` |

Notes:

- There is **no** `DELETE` route for a setting, legal version, city or city request.
- There is **no** route to edit a past legal version or an acceptance row.
- There is **no** Admin route that creates a member or a lead.
- Member city submission stays on M2 (`POST /me/route/{routeId}/city-requests`) and delegates to `ICityRequestService.SubmitAsync`; M13 adds no duplicate member route.

---

## 7. Endpoint details

### 7.1 `GET /admin/config`

- Builds the six groups in the fixed order Matching → Requests & connections → Chat & rating → Safety → Notifications → Security & content.
- Reads all values through the cached reader; fills missing rows from the registry default and records no change.
- `allowedRange` for CFG-4 and CFG-29 is filled from the **current** values of CFG-2 and CFG-5 (`maxFromKey`), so the UI shows the live upper bound.
- `isChanged` is always false on this call; the UI tracks unsaved edits locally.
- `requiresExtraConfirmation` is true only for CFG-8, CFG-9, CFG-10, CFG-11 and CFG-23 (BR-22.9). CFG-24 does **not** require it.
- `sampleLegalAlert` and `pendingDraftLegalCount` are convenience flags; the legal screen remains the source of truth.

### 7.2 `PUT /admin/config`

Atomic batch rules:

1. Admin authorisation.
2. `changes` is non-empty and contains no duplicate keys → else `CONFIG_SAVE_EMPTY` / `CONFIG_KEY_DUPLICATE`.
3. Every key exists in the registry → else `CONFIG_KEY_UNKNOWN`.
4. CFG-3 is rejected → `CONFIG_FIXED`.
5. Parse each value by `ValueType`; invalid shape → `CONFIG_VALUE_TYPE_INVALID`.
6. Range check each value → `CONFIG_OUT_OF_RANGE` with `{ key, allowedMin, allowedMax }`.
7. Merge the requested values over the stored values and run the cross-field rules (§4.8) → `CONFIG_DEPENDENCY_INVALID`.
8. If any changed key is in the security group, `acknowledgedSecurity` must be true → `SECURITY_CONFIRMATION_REQUIRED`.
9. CFG-25 is trimmed and checked for links, URLs, `@` addresses and 10-digit runs → `REQUEST_MESSAGE_INVALID`.
10. `remark` is trimmed; ≤ 500 → `REMARK_TOO_LONG`.
11. Drop keys whose value is already equal (no history row, no audit).
12. In one transaction:
    - update `raah_AppSetting` rows with `RowVersion` check (`409 CONFIG_CONFLICT` when stale);
    - insert one `raah_AppConfigChange` per changed key with a shared `ChangeBatchId`;
    - audit `CONFIG_CHANGED` once per key with old/new, remark, actor and batch id.
13. After commit, invalidate the config cache so every module reads the new values on its next call.
14. Return the fresh screen plus `changedKeys`.

No side effects on routes, leads, requests or connections. A lowering of CFG-2 does **not** invalidate existing routes (BR-22.6); switching CFG-19 to Yes does **not** edit existing routes (BR-22.6); existing leads are not withdrawn (BR-22.3).

### 7.3 `GET /admin/config/history`

- Default sort `NewestFirst`; `Setting` and `Admin` sorts are whitelisted.
- `changedBy` filters by Admin id; `group` by the registry group.
- Each row includes the batch id so two settings saved together can be recognised.
- History is read-only. There is no edit or delete endpoint (BR-22.1, BR-8.3).
- `pageSize` 25 default / 100 max.

### 7.4 `GET /admin/config/test-match/options`

- Returns only **active** cities (`ICityDirectory`), current CFG-2/CFG-18/CFG-1/CFG-22/CFG-4 and a fixed notice: "Uses current settings. No member data is used."
- No member fields are queried.

### 7.5 `POST /admin/config/test-match`

- Delegates to M3 `ITestMatchService.RunAsync`.
- Validate both sample legs: `startCityId`/`endCityId` exist and are active; points 2..CFG-2; days from CFG-18 with at least one; `departureTime` valid `HH:mm`.
- Cities may be the same; two routes that share no city must produce a clear `NoMatch`/failed city check.
- The tool uses only the submitted text; it must not load a member, create a lead, write a match row or notify anyone (AC-22.5).
- A result is not cached across config changes; each call re-reads the current settings.
- No audit is required because nothing changes; logging the Admin's use (without inputs) is optional security telemetry.

### 7.6 `POST /admin/matching/recalculate`

1. `confirm` must be true → `CONFIRMATION_REQUIRED`.
2. `remark` trimmed, ≤ 500.
3. Call `IMatchingEngine.RunAllAsync(trigger: ManualRecalculation, adminId, remark)`.
4. M3 captures a single config snapshot for the run, re-runs matching for every Active route, and writes `raah_MatchingRun` counters.
5. Pending requests and Accepted connections are **not** touched (AC-22.4).
6. Audit `MATCHING_RECALCULATION_REQUESTED` (M13) and `MATCHING_RECALCULATED` (M3, run level).
7. Return `202` with `runId`; the UI polls `GET /admin/matching/runs/{runId}`.
8. A second request while a run is already Running returns `409 RECALCULATION_IN_PROGRESS`.

### 7.7 `GET /admin/matching/runs/{runId}` and list

- Read-through to M3; no M13 table is added.
- `MatchingRunDto` includes `leadsCreated`, `leadsWithdrawn` and the started/completed times (BR-22.4, AC-22.4).
- The list is the RPT-28 source, filtered by status and date range.

### 7.8 `GET /admin/legal`

- Returns the three documents in fixed order: Terms of Use, Privacy Policy, Safety Guidelines.
- `currentPublished` is the `IsCurrent = 1` row; `draft` is the single Draft row when present.
- `sampleAlert` is true when any current published row has `IsSample = true` (BR-22.13, AC-22.7).
- `unacceptedMemberCount` is computed only for a current version with `RequiresReAcceptance = true`; otherwise null.

### 7.9 `PUT /admin/legal/{documentType}/draft`

- Validate `documentType` is one of the three → `LEGAL_DOCUMENT_TYPE_INVALID`.
- If no draft exists, create one with `VersionNo = max(VersionNo) + 1`, `Status = Draft`, `IsCurrent = false`, `IsSample = false`.
- If a draft exists, update it in place (no version increment until publish — UD-API-4).
- `title`: trim, 1–150 → `LEGAL_TITLE_REQUIRED` / `LEGAL_TITLE_TOO_LONG`.
- `bodyHtml`: required; sanitize to headings, paragraphs, lists and links only → `LEGAL_BODY_REQUIRED` / `LEGAL_BODY_FORMAT_INVALID`. Scripts, styles, images, iframes and event attributes are removed or rejected.
- `effectiveDate`: date-only, India time, not earlier than today → `LEGAL_EFFECTIVE_DATE_INVALID`.
- `requiresReAcceptance`: boolean.
- Audit `LEGAL_DRAFT_CREATED` or `LEGAL_DRAFT_UPDATED` with the document type, version number, title and actor. Never log the body.
- A draft save does not change what members see (BR-22.12).

### 7.10 `POST /admin/legal/{documentType}/publish`

1. Admin authorisation and document type check.
2. Load the draft by `versionId`; it must belong to the document and be `Status = Draft` → `LEGAL_DRAFT_NOT_FOUND`.
3. Re-validate the draft content, including effective date still not earlier than today.
4. When `requiresReAcceptance` is true, `confirm` must be true → `CONFIRMATION_REQUIRED`.
5. In one transaction:
   - set the previous current version `IsCurrent = false`, `RetiredOnUtc = now`;
   - set the draft `Status = Published`, `IsCurrent = true`, `PublishedByAdminId`, `PublishedOnUtc`, `PublishRemark`;
   - when re-acceptance is required, enqueue `LegalReacceptanceRequiredEvent { documentIds, effectiveDate }` in the outbox (M9 fans out to eligible members);
   - audit `LEGAL_DOCUMENT_PUBLISHED` with document, version, effective date, requiresReAcceptance, remark and actor.
6. Return `membersToNotify` from the eligibility reader (the same population M9 will notify).
7. Only one version is Published as current at a time; earlier versions stay readable by Admins (AC-22.8).
8. `sampleAlert` in the response reflects the state after publish; it disappears only when all three current versions are non-sample (BR-22.13, SC-22.9).
9. A publish with `RequiresReAcceptance = No` sends no legal notification (SC-22.10).

### 7.11 `GET /admin/legal/{documentType}/versions` and version detail

- Admin sees every version, newest first, including retired and sample versions.
- `GET .../versions/{versionId}` returns the full body; this is the only place a retired body is read.
- No version can be edited after publish and none can be deleted (BR-22.12, BR-22.17).

### 7.12 Public and member legal endpoints

- `GET /legal` and `GET /legal/{documentType}` return only the current Published version, no auth, no member data (BR-22.14).
- `GET /me/legal/status` computes `pending` as current published versions with `RequiresReAcceptance = true` that the member has not accepted. It also returns `allCurrent` so the member can read the texts while gated (BR-22.16).
- `POST /me/legal/accept`:
  - every `versionId` must be a current Published version → `LEGAL_VERSION_NOT_CURRENT`;
  - insert one `raah_LegalAcceptance` per version not already accepted with `AcceptedFrom = ReAcceptance`;
  - already accepted versions are returned in `alreadyAccepted`, not duplicated;
  - the member cannot accept a draft, a retired version or another member's record;
  - audit `LEGAL_ACCEPTANCE_RECORDED` per document/version.
- `ILegalAcceptanceService.RecordInitialAcceptanceAsync(memberId, currentVersionIds)` is the registration path; it records all three current versions with `AcceptedFrom = Registration`.
- `ILegalAcceptanceGate.HasPendingAsync(memberId)` returns true while any required current version is unaccepted. The auth gate lives in Foundation; M13 supplies the answer and the texts.
- An old acceptance is never rewritten when a new version is published (BR-22.17).

### 7.13 `GET /admin/legal/{documentType}/acceptance-summary`

- Shows eligible, accepted, pending and percentage for the current version.
- Eligible = members who can log in and are subject to the gate (default: Active and Restricted; UD-API-5). Blocked/Inactive members are counted separately, never as pending.
- This is the RPT-29 read model; M15 owns the report page and export.

### 7.14 City Master

`GET /admin/cities`

- Default `status` is all; `search` matches `Name`/`State` case-insensitively.
- Each row carries live `activeRoutes`, `draftRoutes` and distinct `members` from `IRouteCityUsageReader`.
- Sort whitelist: `NameAsc`, `Status`, `ActiveRoutesDesc`, `MembersDesc`.
- Numbered pager 25/100.

`POST /admin/cities` and `PUT /admin/cities/{cityId}`

- `name`: trim, collapse internal whitespace, 2–60 → `CITY_NAME_INVALID`.
- `state`: trim, ≤ 60 → `CITY_STATE_TOO_LONG`.
- Normalize name ignoring capitals and spaces; duplicate → `409 CITY_NAME_DUPLICATE` (BR-22.18, AC-22.10).
- Add defaults `Status = Active`; edit corrects the name/state and leaves routes untouched by id (BR-22.20).
- Audit `CITY_CREATED` / `CITY_UPDATED` with old/new name and state.

`GET /admin/cities/{cityId}/usage`

- Returns `canDeactivate`, total routes, and the blocking routes (Draft or Active) where the city is Start or End (BR-22.19, AC-22.11).
- The route list is capped (default 100) with `hiddenRouteCount`; the UI states the total.

`POST /admin/cities/{cityId}/deactivate`

- Re-read usage at action time. If any Draft or Active route uses the city → `409 CITY_IN_USE`; the UI calls the usage endpoint to show the routes.
- Otherwise set `Status = Inactive` and audit `CITY_DEACTIVATED`.
- Deactivation never edits routes and never deletes the city.

`POST /admin/cities/{cityId}/activate`

- Always allowed; set `Status = Active` and audit `CITY_REACTIVATED`.

### 7.15 `ICityDirectory` (consumed by M2)

- `GetActiveCitiesAsync(search?)` returns only `Status = Active`, ordered by name.
- `IsActiveCityAsync(cityId)` and `GetCityAsync(cityId)` are used at route save/activate.
- A city deactivated after a route was saved remains readable on that route for history, but cannot be used on a new route.

### 7.16 City requests

`GET /admin/city-requests`

- Default `status = Open`, oldest first (A-56).
- Row includes member, requested name, date, decision data and `isDuplicateNow` so Admin sees a request that became redundant because the city was added another way.
- Search matches member name/MBR or requested name; date range on requested date.

`POST /admin/city-requests/{requestId}/add`

1. Request must be `Open` → `CITY_REQUEST_ALREADY_DECIDED`.
2. `name` defaults to `RequestedName`; Admin may correct spelling before creating.
3. Re-check the City Master for the normalized name → `CITY_ALREADY_EXISTS`.
4. Create the city (`Status = Active`, state optional) and set the request `Status = Added`, `CreatedCityId`, `DecidedByAdminId/OnUtc`.
5. Enqueue `SuggestedCityDecidedEvent { memberId, cityName, accepted: true }` in the same transaction; M9 sends the in-app notice.
6. Audit `CITY_REQUEST_ADDED` with request id, new city id and actor.
7. Both writes commit together; a failure leaves the request Open.

`POST /admin/city-requests/{requestId}/decline`

1. Request must be `Open` → `CITY_REQUEST_ALREADY_DECIDED`.
2. `reason`: trim, 10–300 → `CITY_REQUEST_DECLINE_REASON_REQUIRED` / `..._TOO_LONG` (UD-API-6).
3. Set `Status = Declined`, reason and decision actor/time.
4. Enqueue `SuggestedCityDecidedEvent { memberId, cityName, accepted: false }`; the member sees the reason in-app.
5. Audit `CITY_REQUEST_DECLINED` with the reason.

Member submission through M2:

- `ICityRequestService.SubmitAsync(memberId, cityName)` normalizes, rejects an existing city name (`CITY_ALREADY_EXISTS`) and an existing Open request for the same member/name (`CITY_REQUEST_EXISTS`), then inserts an Open request.
- After a Declined request, the member may raise the same name again; only Open requests are deduplicated (UD-API-14).

---

## 8. Orchestration and failure handling

### 8.1 Configuration save

1. Validate the batch as a whole (§7.2).
2. Start a transaction.
3. Update each changed setting with optimistic concurrency.
4. Insert change rows and audit rows.
5. Commit.
6. Invalidate the config cache.
7. Return the fresh values.

If the transaction fails, **nothing** changes and no cache invalidation occurs. A partial save is never reported as success. Two Admins saving the same key: the second gets `409 CONFIG_CONFLICT` and must reload.

### 8.2 Legal publish

1. Validate the draft and confirmation.
2. Start a transaction.
3. Retire the previous current version.
4. Promote the draft.
5. Enqueue the re-acceptance event when required.
6. Audit.
7. Commit.

The outbox event is inside the transaction, so members are never notified about a version that failed to publish. M9 handles fan-out and retry. The public read reflects the new current version immediately after commit.

### 8.3 Legal acceptance

1. Validate the member and the requested version ids.
2. Load current published versions.
3. Insert missing acceptance rows in one transaction.
4. Commit.
5. Return the pending state.

Replaying the same request is idempotent. The gate reads the rows directly; there is no cached "accepted" flag on the member.

### 8.4 City request add/decline

As §7.16: city creation and request decision commit together; the `SuggestedCityDecidedEvent` is in the same transaction. A member is never told "added" unless the city row exists.

### 8.5 Recalculation

M13 only validates and calls M3. M3 owns batching, idempotency, run counters and failure recording. If M3 is unavailable, return `503 MATCHING_UNAVAILABLE`; no partial run is created by M13.

---

## 9. Authorization and gates

| Caller | Allowed |
|---|---|
| Anonymous | `GET /legal`, `GET /legal/{documentType}` only |
| Member | `/me/legal/status`, `/me/legal/accept`, `/me/legal/acceptances`; nothing under `/admin/*` |
| Unverified member | Public legal pages; the legal status endpoint; other member routes are gated by Foundation |
| Member pending re-acceptance | Only the legal texts, accept, and log out (BR-22.16); enforced by Foundation using `ILegalAcceptanceGate` |
| Admin | All `/admin/config`, `/admin/legal`, `/admin/cities`, `/admin/city-requests`, `/admin/matching` routes |
| Inactive/Blocked Admin | Rejected by Foundation session/status validation |

- Enforce `RoleId = Admin` in every Admin controller; use `StatusCode(403)` for a plain reject. Log denied attempts per BR-4.2 / BR-22.11.
- Never accept a member id from a body for a self-service legal route; derive it from the token.
- Public legal responses contain no member data and no Admin identity beyond the published title/date.
- Acceptance records are always scoped to the authenticated member.

### 9.1 Status transitions owned by M13

Legal version:

| From | To | Action | Condition |
|---|---|---|---|
| (none) | Draft | Save draft | title, body, effective date valid |
| Draft | Published (current) | Publish | confirm when re-acceptance is required; previous current retired |
| Published (current) | Published (retired) | Publish a newer version | automatic |

City:

| From | To | Action | Condition |
|---|---|---|---|
| (none) | Active | Add city | unique normalized name |
| Active | Inactive | Deactivate | no Draft/Active route uses it |
| Inactive | Active | Activate | always allowed |

City request:

| From | To | Action | Condition |
|---|---|---|---|
| (none) | Open | Member submit | name not in City Master; no open duplicate |
| Open | Added | Admin add | name still unique |
| Open | Declined | Admin decline | reason 10–300 |

Configuration values: edited within range only; no deletion and no other status.

---

## 10. Validation matrix

| ID | Rule | Where | Error |
|---|---|---|---|
| BR-22.5 / V-47 | Value inside its allowed range | Save config | `CONFIG_OUT_OF_RANGE` |
| BR-22.10 | Settings cannot be deleted | Whole module | no delete endpoint |
| — | Setting key exists; batch non-empty/deduplicated | Save config | `CONFIG_KEY_UNKNOWN` / `CONFIG_SAVE_EMPTY` / `CONFIG_KEY_DUPLICATE` |
| — | Value type matches the registry | Save config | `CONFIG_VALUE_TYPE_INVALID` |
| BR-22.6 | CFG-3 fixed at 2 | Save config | `CONFIG_FIXED` |
| §4.8 | CFG-4 ≤ CFG-2; CFG-29 ≤ CFG-5 − 1; day/vehicle/channel minimums | Save config | `CONFIG_DEPENDENCY_INVALID` |
| BR-22.9 | Security keys need extra confirmation | Save config | `SECURITY_CONFIRMATION_REQUIRED` |
| CFG-25 | Request text 1–150, no links/10-digit runs | Save config | `REQUEST_MESSAGE_INVALID` |
| — | Remark ≤ 500 | Save/config publish/decline | `REMARK_TOO_LONG` |
| — | Stale `RowVersion` | Save config | `CONFIG_CONFLICT` |
| BR-22.4 | Recalculation needs confirmation | Recalculate | `CONFIRMATION_REQUIRED` |
| — | One run at a time | Recalculate | `RECALCULATION_IN_PROGRESS` |
| AC-22.5 | Test Match uses no member data | Test Match | no member query; `SAMPLE_INPUT_INVALID` on bad input |
| BR-22.12 | Draft exists to publish; version belongs to document | Publish | `LEGAL_DRAFT_NOT_FOUND` |
| — | Title 1–150, body allowed subset | Draft save | `LEGAL_TITLE_REQUIRED` / `LEGAL_TITLE_TOO_LONG` / `LEGAL_BODY_REQUIRED` / `LEGAL_BODY_FORMAT_INVALID` |
| — | Effective date not before today | Draft/Publish | `LEGAL_EFFECTIVE_DATE_INVALID` |
| BR-22.16 | Re-acceptance publish needs confirmation | Publish | `CONFIRMATION_REQUIRED` |
| BR-22.15/17 | Acceptance rows immutable; no duplicates | Accept | idempotent; `LEGAL_VERSION_NOT_CURRENT` |
| BR-22.18/AC-22.10 | City name unique ignoring capitals/spaces | Add/Edit/Add request | `CITY_NAME_DUPLICATE` |
| — | City name 2–60; state ≤ 60 | Add/Edit | `CITY_NAME_INVALID` / `CITY_STATE_TOO_LONG` |
| BR-22.19/AC-22.11 | Cannot deactivate a city used by a Draft/Active route | Deactivate | `CITY_IN_USE` |
| BR-22.21 | No request for an existing city; one open request per member/name | Submit | `CITY_ALREADY_EXISTS` / `CITY_REQUEST_EXISTS` |
| BR-22.21 | Decline requires a reason | Decline | `CITY_REQUEST_DECLINE_REASON_REQUIRED` |
| — | Decision only on an Open request | Add/Decline | `CITY_REQUEST_ALREADY_DECIDED` |
| — | Document type / action / sort are known values | Various | `LEGAL_DOCUMENT_TYPE_INVALID` / `ACTION_INVALID` / `SORT_INVALID` |
| — | Page size 1–100 | Lists | `PAGE_SIZE_INVALID` |

No validation framework exists in the repo; implement and unit-test these in the application services.

---

## 11. Audit events

| Event | Trigger | Recorded |
|---|---|---|
| `CONFIG_CHANGED` | Each changed key in a batch | key, old, new, remark, batch id, actor, UTC |
| `MATCHING_RECALCULATION_REQUESTED` | Admin confirms | run id?, actor, remark |
| `MATCHING_RECALCULATED` / `MATCHING_RECALCULATION_FAILED` | M3 run completes/fails | run id, counts, error summary |
| `LEGAL_DRAFT_CREATED` / `LEGAL_DRAFT_UPDATED` | Draft save | document, version, title (never body), actor |
| `LEGAL_DOCUMENT_PUBLISHED` | Publish | document, version, effective date, re-acceptance, remark, actor |
| `LEGAL_ACCEPTANCE_RECORDED` | Member accepts | member id, document, version, source |
| `CITY_CREATED` / `CITY_UPDATED` | Add/edit | city id, old/new name and state, actor |
| `CITY_DEACTIVATED` / `CITY_REACTIVATED` | Status change | city id, name, actor |
| `CITY_REQUEST_ADDED` / `CITY_REQUEST_DECLINED` | Decision | request id, member id, name, reason, actor |
| `ADMIN_ACCESS_DENIED` | Member opens an Admin route | member id, path, UTC |

Every row includes actor, role, UTC timestamp and old/new where applicable. Audit is append-only. **Never** write legal body text, member contact details or chat content into logs.

---

## 12. Config usage

M13 **owns** the values that every module reads. The meaning of each key is defined by its owning module; M13 only edits and records it.

| CFG | Primary consumer |
|---|---|
| CFG-1, CFG-2, CFG-3, CFG-4, CFG-18, CFG-19, CFG-20, CFG-21, CFG-22 | M2 / M3 matching and routes |
| CFG-5, CFG-6, CFG-17, CFG-25 | M5 requests / connections |
| CFG-7 | M4 leads list |
| CFG-8, CFG-9, CFG-10, CFG-11, CFG-23 | Foundation auth |
| CFG-12 | M4 lead filter |
| CFG-13, CFG-14, CFG-26, CFG-27, CFG-28 | M6 chat / M7 rating |
| CFG-15, CFG-32 | M12 safety |
| CFG-16, CFG-29, CFG-30, CFG-31 | M9 notifications |
| CFG-24 | M1 profile photo upload |

M13 reads values through the same `IAppConfigReader` for the screen and reference checks; it never hard-codes a default outside the registry.

---

## 13. Error responses

Use `BadRequest(new { code, message })`, `StatusCode(409, new { code, message })`, or `NotFound(new { code, message })`. Never `Forbid()`/`Challenge()`.

| Code | HTTP | Message (UI-safe) |
|---|---|---|
| `CONFIG_KEY_UNKNOWN` | 400 | "This setting is not recognised." |
| `CONFIG_SAVE_EMPTY` | 400 | "Change at least one setting." |
| `CONFIG_KEY_DUPLICATE` | 400 | "The same setting was sent more than once." |
| `CONFIG_FIXED` | 400 | "This value is fixed and cannot be changed." |
| `CONFIG_VALUE_TYPE_INVALID` | 400 | "Enter a valid value." |
| `CONFIG_OUT_OF_RANGE` | 400 | "Allowed range is {min} to {max}." |
| `CONFIG_DEPENDENCY_INVALID` | 400 | "This value must stay within its related setting." |
| `CONFIG_CONFLICT` | 409 | "Another Admin changed this setting. Reload and try again." |
| `SECURITY_CONFIRMATION_REQUIRED` | 400 | "Confirm that you understand this affects every member." |
| `REQUEST_MESSAGE_INVALID` | 400 | "Use plain text only, with no links, email addresses or phone numbers." |
| `REMARK_TOO_LONG` | 400 | "Keep the remark under 500 characters." |
| `CONFIRMATION_REQUIRED` | 400 | "Confirm that you understand what will happen." |
| `RECALCULATION_IN_PROGRESS` | 409 | "A recalculation is already running." |
| `MATCHING_UNAVAILABLE` | 503 | "Matching is not available right now." |
| `SAMPLE_INPUT_INVALID` | 400 | "Check the sample cities, points, days and time." |
| `LEGAL_DOCUMENT_TYPE_INVALID` | 400 | "Choose a valid document." |
| `LEGAL_DOCUMENT_NOT_FOUND` | 404 | "Document not found." |
| `LEGAL_DRAFT_NOT_FOUND` | 404 | "Save a draft before publishing." |
| `LEGAL_TITLE_REQUIRED` | 400 | "Enter a title." |
| `LEGAL_TITLE_TOO_LONG` | 400 | "Keep the title under 150 characters." |
| `LEGAL_BODY_REQUIRED` | 400 | "Enter the document text." |
| `LEGAL_BODY_FORMAT_INVALID` | 400 | "Use headings, paragraphs, lists and links only." |
| `LEGAL_EFFECTIVE_DATE_INVALID` | 400 | "The effective date cannot be earlier than today." |
| `LEGAL_VERSION_NOT_CURRENT` | 409 | "This version is not the current published version." |
| `LEGAL_ACCEPTANCE_REQUIRED` | 403 | "Accept the updated documents to continue." |
| `CITY_NOT_FOUND` | 404 | "City not found." |
| `CITY_NAME_INVALID` | 400 | "Enter a city name of 2 to 60 characters." |
| `CITY_NAME_DUPLICATE` | 409 | "A city with this name already exists." |
| `CITY_STATE_TOO_LONG` | 400 | "Keep the state under 60 characters." |
| `CITY_IN_USE` | 409 | "This city is used by routes. Deactivate those routes first." |
| `CITY_ALREADY_EXISTS` | 409 | "This city is already in the list." |
| `CITY_REQUEST_EXISTS` | 409 | "You have already requested this city." |
| `CITY_REQUEST_NOT_FOUND` | 404 | "City request not found." |
| `CITY_REQUEST_ALREADY_DECIDED` | 409 | "This request has already been decided." |
| `CITY_REQUEST_DECLINE_REASON_REQUIRED` | 400 | "Enter a reason of at least 10 characters." |
| `CITY_REQUEST_DECLINE_REASON_TOO_LONG` | 400 | "Keep the reason under 300 characters." |
| `SORT_INVALID` | 400 | "Choose a valid sort order." |
| `PAGE_SIZE_INVALID` | 400 | "Page size must be between 1 and 100." |
| `FORBIDDEN` | 403 | "You do not have access to this action." |

Keep wording aligned with the UI plan's microcopy.

---

## 14. Privacy and security

- **Admin-only control surface.** Every `/admin/*` route checks the Admin role server-side and logs denials (BR-22.11, BR-4.2).
- **Test Match is isolated.** It evaluates only submitted text; it must never query `member_Master`, create a lead or affect a member (AC-22.5).
- **Legal bodies are sanitized.** Store only the allowed HTML subset; reject scripts, styles, images, iframes and event handlers. Never log body text.
- **Acceptance is append-only.** No update/delete path exists; a new version never rewrites an old acceptance (BR-22.15, BR-22.17).
- **Public legal pages are anonymous.** No token, no member data, no Admin details.
- **Config history is immutable.** No edit/delete route; the audit trail and RPT-26 read the same rows.
- **Settings never rewrite history.** Existing routes, leads, requests, connections, chats and completed records are not touched by a setting change (BR-22.2, BR-22.3).
- **City deletion does not exist.** Inactive cities remain readable for history (BR-22.20).
- **No member contact data** is exposed by any M13 endpoint; the city-request list shows the same public-safe member summary used by Admin screens (name/number/photo), never email or mobile.
- **CSV exports** (if built here) use UTF-8, a CSV injection guard, a row cap and audit `M13_EXPORTED`; they never include legal bodies or acceptance tokens.

---

## 15. Build sequence (M13)

| Step | Work | Output |
|---|---|---|
| 1 | Enums, DTOs, registry metadata for all CFG keys, query/request models | Compiles |
| 2 | `raah_AppSetting` + `raah_AppConfigChange`, configuration + migration, seed 32 defaults | Tables apply |
| 3 | `IAppConfigReader` implementation (or wiring to Foundation) + cache invalidation hook | All modules read values |
| 4 | `IAppConfigAdminService`: grouped screen, batch validation, optimistic save | SCR-22.1 data |
| 5 | `IConfigHistoryQuery` + history endpoints | SCR-22.2, RPT-26 |
| 6 | Test Match options + endpoint through M3 `ITestMatchService` | SCR-22.3 |
| 7 | Recalculation endpoint + run read/list through M3 | BR-22.4, RPT-28 |
| 8 | `raah_City` + configuration/migration; `ICityDirectory` read path | M2 can choose cities |
| 9 | City Admin: list, add, edit, usage, activate/deactivate | SCR-22.5, RPT-30 |
| 10 | `raah_CityRequest`, `ICityRequestService` for M2, Admin list/add/decline + notification event | SCR-22.6, A-56 |
| 11 | `raah_LegalDocumentVersion`, seed three sample versions, public read endpoints | Public legal pages work |
| 12 | Legal Admin: overview, draft save, publish, version history | SCR-22.4, D-24 |
| 13 | `raah_LegalAcceptance`, status/accept endpoints, registration hook, gate contract | BR-22.14–BR-22.17 |
| 14 | Acceptance summary + sample alert reader for M14 | RPT-29, BR-22.13 |
| 15 | Controllers + Admin gate + error codes | Postman-ready |
| 16 | Audit coverage for every event | §11 |
| 17 | Optional CSV extracts for history/cities | M15 extracts |

Suggested folders:

```
Binsera.Core/Configuration/Enums/AppSettingValueType.cs, AppSettingGroup.cs, ConfigHistorySort.cs
Binsera.Core/Configuration/Models/Admin/ConfigScreenDto.cs, SaveConfigRequest.cs,
                                         ConfigHistoryQuery.cs, ConfigHistoryItemDto.cs
Binsera.Core/Configuration/Contracts/IAppConfigAdminService.cs,
                                         IConfigHistoryQuery.cs,
                                         IAppConfigWriter.cs
Binsera.Core/Configuration/Registry/SettingDefinition.cs, SettingDefinitionRegistry.cs
Binsera.Core/Configuration/Domain/AppSetting.cs, AppConfigChange.cs

Binsera.Core/Cities/Enums/CityStatus.cs, CityRequestStatus.cs, CityListSort.cs, CityRequestSort.cs
Binsera.Core/Cities/Models/*Dto.cs, *Request.cs, *Query.cs
Binsera.Core/Cities/Contracts/ICityDirectory.cs, ICityAdminService.cs,
                                ICityRequestService.cs, ICityRequestAdminService.cs,
                                IRouteCityUsageReader.cs
Binsera.Core/Cities/Domain/City.cs, CityRequest.cs
Binsera.Core/Cities/Events/SuggestedCityDecidedEvent.cs

Binsera.Core/Legal/Enums/LegalDocumentType.cs, LegalDocumentStatus.cs, LegalAcceptanceSource.cs
Binsera.Core/Legal/Models/*Dto.cs, *Request.cs
Binsera.Core/Legal/Contracts/ILegalContentAdminService.cs, ILegalContentPublicQuery.cs,
                              ILegalAcceptanceService.cs, ILegalAcceptanceGate.cs,
                              ILegalContentAlertReader.cs
Binsera.Core/Legal/Domain/LegalDocumentVersion.cs, LegalAcceptance.cs
Binsera.Core/Legal/Events/LegalReacceptanceRequiredEvent.cs

Binsera.Application/Configuration/Services/Admin/AppConfigAdminService.cs,
                                            ConfigHistoryQuery.cs
Binsera.Application/Configuration/Services/AppConfigReader.cs
Binsera.Application/Cities/Services/CityDirectory.cs, CityAdminService.cs,
                                            CityRequestService.cs, CityRequestAdminService.cs
Binsera.Application/Legal/Services/LegalContentAdminService.cs,
                                  LegalContentPublicQuery.cs,
                                  LegalAcceptanceService.cs,
                                  LegalContentAlertReader.cs
Binsera.Application/Legal/Services/LegalHtmlSanitizer.cs

Binsera.API/Controllers/Admin/ConfigController.cs,
                             MatchingAdminController.cs,      // recalculate/runs, mounted with M3
                             LegalContentController.cs,
                             CitiesController.cs,
                             CityRequestsController.cs
Binsera.API/Controllers/Public/LegalController.cs
Binsera.API/Controllers/Member/MemberLegalController.cs

Binsera.Persistence/Configurations/Raah/AppSettingConfiguration.cs,
                                          AppConfigChangeConfiguration.cs,
                                          CityConfiguration.cs,
                                          CityRequestConfiguration.cs,
                                          LegalDocumentVersionConfiguration.cs,
                                          LegalAcceptanceConfiguration.cs
Binsera.Persistence/Migrations/yyyyMMddHHmmss_AddRaahAppSettingAndConfigChange.cs,
                              yyyyMMddHHmmss_AddRaahCityAndCityRequest.cs,
                              yyyyMMddHHmmss_AddRaahLegalContentAndAcceptance.cs
```

---

## 16. Test plan

### 16.1 Unit

- Registry: every CFG-1…32 has a group, type, default and range; CFG-3 is non-editable; CFG-8/9/10/11/23 are flagged for extra confirmation; CFG-4/CFG-29 upper bounds follow live values.
- Config save: valid numeric/boolean/text/time/multi-select values; out-of-range boundaries (min−1, min, max, max+1); cross-field CFG-4 > CFG-2; CFG-29 ≥ CFG-5; empty day set; empty vehicle set; missing In-app; CFG-25 link/10-digit rejection; duplicate key; unknown key; fixed CFG-3; no-op value produces no history; security flag required only for the five keys; remark length.
- Change history: newest-first default, each filter, batch grouping, paging boundaries, Admin snapshot.
- Test Match: two shared-point routes; same-city requirement fails when cities differ; day mismatch; time gap at CFG-1 and CFG-1+1; text similarity at CFG-22−1/CFG-22/CFG-22+1; point pairing; Full Route vs Useful Segment vs No Match; point limit; no member-table access (assert the service uses only the request).
- Recalculate: `confirm=false` refused; successful request returns a run id; one run at a time; pending requests/connections not queried or written by M13.
- Legal: draft create/update, version numbering, title/body/date limits, HTML sanitizer, publish retires the previous current, one current per type, retired version still readable, sample alert only while any sample current, re-acceptance confirmation, event emitted only when required.
- Acceptance: pending computed only for current required versions; idempotent accept; retired/draft version rejected; old acceptance untouched; registration records all three; gate true/false boundaries.
- City: normalization (`New Delhi` = `new delhi` = `NewDelhi`), duplicate add/edit, name/state limits, deactivate with Draft route, with Active route, with only Inactive routes, reactivation, stats counts.
- City requests: existing city refused, open duplicate refused, resubmit after decline allowed, add creates city + link + event, decline requires reason, decided request refused again.

### 16.2 Integration / API

- Missing token → 401; member token on `/admin/*` → 403 and an audit/concession log row; Admin token → 200.
- Public legal works without a token; member acceptance routes require a member token; Admin token cannot accept for a member.
- Config save updates the reader immediately (cache invalidation) and inserts one history row per changed key.
- A security setting without `acknowledgedSecurity` is refused; with it, saved.
- Config save does not change lead/route/request/connection rows.
- Recalculation integration: M3 run creates/withdraws leads, pending requests and connections unchanged, counts visible in the run, audit written.
- Legal publish integration: public endpoint returns the new body immediately; previous version remains in Admin history; members with the gate on cannot use product routes until accept; after accept the gate clears.
- City deactivate integration: a city used by a Draft/Active route returns `CITY_IN_USE`; after those routes are changed/deactivated it succeeds.
- City add from a request creates exactly one city and one notification event; decline stores the reason and notifies.
- Migration does not touch `member_Master`.
- No delete endpoints exist for settings, legal versions, cities, requests or acceptance rows.

### 16.3 Acceptance mapping

| AC | Covered by |
|---|---|
| AC-22.1 Change logged | §4.2, §7.2, §7.3, §11, §16.2 |
| AC-22.2 Range enforced | §4.8, §7.2, §10 |
| AC-22.3 Existing leads not withdrawn on a normal change | §7.2, §7.6, §16.2 |
| AC-22.4 Recalculate leaves requests alone | §7.6, §8.5, §16.2 |
| AC-22.5 Test Match uses no member data | §7.5, §14, §16.1 |
| AC-22.6 Access control | §9, §16.2 |
| AC-22.7 Sample alert | §7.8, §7.10, §16.1 |
| AC-22.8 Publish replaces one version only | §7.10, §4.3, §16.1 |
| AC-22.9 Re-acceptance gate | §7.12, §9, §16.2 |
| AC-22.10 City duplicate | §7.14, §10, §16.1 |
| AC-22.11 City in use | §7.14, §10, §16.2 |

SC-22.1…SC-22.16 map to the endpoints and orchestrations in §7–§8; SC-22.13/14/15/16 are the City Master and city-request cases in §7.14–§7.16.

---

## 17. Open decisions

| # | Question | Default in this plan |
|---|---|---|
| UD-API-1 | Does M13 own the setting store? | Yes, unless Foundation already provides an equivalent typed store; M13 adds `raah_AppSetting` and implements `IAppConfigReader` from it |
| UD-API-2 | CFG-21 appears in Section 6 but not Section 22.4 | Include it in the Matching group with at least one of Bike/Car (Phase 1) |
| UD-API-3 | Sample version numbering | Sample is `VersionNo = 0`, label `Sample 1`; real versions start at 1 (matches SC-22.9 "publishes it as version 1") |
| UD-API-4 | One draft per document or a new draft per edit? | One draft per document; save updates it until publish |
| UD-API-5 | Who counts as pending re-acceptance? | Members who can log in (Active, Restricted, Unverified); Blocked/Inactive are not counted as pending |
| UD-API-6 | City decline reason length | 10–300 after trim; consistent with other Admin reasons |
| UD-API-7 | Does publish re-check the effective date? | Yes; a draft whose date has passed must be corrected before publishing |
| UD-API-8 | Registration acceptance | Record all three current published versions with `AcceptedFrom = Registration` |
| UD-API-9 | Who owns the recalculate route? | M3 defines the engine and run table; M13 mounts and exposes the Admin route |
| UD-API-10 | Do config saves require a remark? | Optional (BR-22.1 says optional remark) |
| UD-API-11 | Can a setting be reverted? | Only by saving the older value as a new change; no special revert endpoint |
| UD-API-12 | Legal body format | Sanitized HTML subset (headings, paragraphs, lists, links only) |
| UD-API-13 | Member count per city | Distinct members with an Active route whose Start or End city is this city |
| UD-API-14 | Resubmission after a declined city request | Allowed; only Open requests are deduplicated |
| UD-API-15 | Record numbers for settings/legal/city | None required by spec; use ids. A `CITY-{nnnnnn}` display number is optional |
| UD-API-16 | Are CSV exports in M13 or M15? | M15 owns the catalogue; M13 exposes optional filtered extracts |

---

## 18. Traceability

| Spec item | Where in this plan |
|---|---|
| Purpose / Admin only | §1, §9 |
| SCR-22.1 Configuration | §5.1, §6.1, §7.1–7.2 |
| SCR-22.2 Change History | §5.2, §6.1, §7.3 |
| SCR-22.3 Test Match | §5.3, §6.2, §7.4–7.5 |
| SCR-22.4 Legal Content | §5.5, §6.3, §7.8–7.11 |
| SCR-22.5 City Master | §5.7, §6.5, §7.14 |
| SCR-22.6 City Requests | §5.8, §6.5, §7.16 |
| Settings by group / allowed range | §4.8 |
| Test Match fields | §5.3, §7.5 |
| Legal fields / version rules | §4.3, §5.5, §7.8–7.11 |
| City fields / city requests | §4.5–4.6, §7.14, §7.16 |
| BR-22.1 change logged | §4.2, §7.2, §11 |
| BR-22.2 change applies to new activity | §2.3, §7.2 |
| BR-22.3 matching settings and leads | §7.2, §7.6 |
| BR-22.4 recalculate | §7.6, §8.5 |
| BR-22.5 range refused | §7.2, §10 |
| BR-22.6 CFG-2/CFG-19 no invalidation | §7.2 |
| BR-22.7 CFG-25 applies forward | §4.8, §7.2 |
| BR-22.8 CFG-5 shortening | §4.8 (value only; M5 expiry job reads it) |
| BR-22.9 security confirmation | §7.1–7.2 |
| BR-22.10 no deletion | §1, §6 notes |
| BR-22.11 Admin only | §9 |
| BR-22.12 legal draft/publish | §7.8–7.11 |
| BR-22.13 sample alert | §7.8, §7.10 |
| BR-22.14 public texts | §7.12 |
| BR-22.15 acceptance recorded | §4.4, §7.12, §11 |
| BR-22.16 re-acceptance gate | §5.6, §7.12, §9 |
| BR-22.17 old acceptance untouched | §4.4, §7.11–7.12 |
| BR-22.18 city duplicate | §7.14, §10 |
| BR-22.19 city in use | §7.14, §10 |
| BR-22.20 city never deleted / name correction | §4.5, §7.14 |
| BR-22.21 city requests | §7.16, §10 |
| BR-22.22 per-city counts | §5.7, §7.14 |
| Status lifecycle | §4.3–4.6, §9.1 |
| Data impact | §8, §11 |
| RPT-26/28/29/30 | §5.2, §5.4, §5.5, §5.7, §7.3, §7.7, §7.13, §7.14 |
| CFG-1…CFG-32 | §4.8, §12 |
| V-47, V-51–V-54 | §10 |
| D-24, D-26, A-51, A-52, A-56 | §1, §7.5–7.16 |
| SC-22.1…SC-22.16 | §7, §8, §16.3 |
| AC-22.1…AC-22.11 | §16.3 |
| Dependencies M2/M3/Foundation | §2, §9 |
