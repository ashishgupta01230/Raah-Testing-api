# Module M13 — Matching Configuration & Platform Settings

> Source: `RAAH_Functional_Specification.md` Section 22. Build order #13.

## Purpose

Give Admin one place to tune how the platform behaves — matching tolerance, limits, expiry, notification timing, security — without any code change, and to keep a complete record of every change (Principle 15).

## Who Uses It

Admin only.

## Dependencies

- M3 (matching config CFG-1, CFG-4, CFG-22), M2 (City Master used by routes), Foundation (all CFG settings, legal texts, audit).
- Foundation: Section 6 settings, BR-5.13 (legal acceptance), D-24, D-26, A-51, A-52.

## Screens

| Screen | Purpose |
|---|---|
| SCR-22.1 Configuration | All settings, grouped, with current value, allowed range and default |
| SCR-22.2 Change History | Every change: setting, old value, new value, who, when, remark |
| SCR-22.3 Test Match | Enter two sample legs and see the result under the current settings (A-51) |
| SCR-22.4 Legal Content | Edit, preview and publish the Terms of Use, Privacy Policy and Safety Guidelines; version history (D-24) |
| SCR-22.5 City Master | Add, edit and deactivate cities (D-26) |
| SCR-22.6 City Requests | Cities suggested by members, to add or decline (A-56) |

## Settings by Group, with Allowed Range

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

## Test Match Tool (SCR-22.3)

| Field | Content |
|---|---|
| Sample Saathi leg | Points (Start, up to 3 in between, End), days, time |
| Sample Raahi leg | The same |
| Cities | Start City and End City of each sample route, to confirm that routes sharing no city never match |
| Result | Pass/fail of each step (days, time, points paired with their similarity %, direction, shared points), then **Full Route**, **Useful Segment** or **No match** |

The tool uses only the sample text Admin types. It reads no member data, creates no lead and has no effect on members.

## Legal Content (SCR-22.4)

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

## City Master (SCR-22.5, SCR-22.6)

Cities are set up by Admin and chosen by members on their route (D-26). There is no fixed city list in the product; Admin creates the cities needed for launch (Section 31).

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| City Name | Text | Yes | 2 to 60 characters. Unique, **ignoring capital letters and spaces** ("New Delhi", "new delhi" and "NewDelhi" are the same) |
| State | Text | No | For clarity in the list |
| Status | Toggle | Yes | Active / Inactive. Default Active |

**City requests (A-56):** a member whose city is not listed sends the city name from "My city is not listed". Admin sees it on SCR-22.6 with member, name and date, and either **Adds** it (creating a city and telling the member in-app) or **Declines** it with a reason (the member is told in-app).

## Business Rules

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

## Status Lifecycle

Legal document versions: **Draft → Published** (one Published version at a time; earlier versions kept in history).

City: **Active ⇄ Inactive** (never deleted; cannot be deactivated while used by a Draft or Active route).

Configuration: values are edited within range; no deletion.

## Scenarios

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

## Acceptance Criteria

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

## Data Impact

| Action | Records created or changed |
|---|---|
| Change a setting | Setting value updated; change-history entry |
| Recalculate all leads | Leads created, updated or withdrawn; a log entry with counts |
| Test Match | None |
| Add / edit / deactivate a city | City record changed; audit entry; member told if it came from a request |
| Publish a legal text | New version Published; previous version kept; notifications if re-acceptance is required; audit entry |
| Member accepts | Acceptance record (member, document, version, time) |

## Reports Produced

RPT-26 Configuration Change Report · RPT-28 Recalculation Log · RPT-29 Legal Acceptance Report · RPT-30 City Register — in M15.

## Cross-References

- **Config:** CFG-1 … CFG-32 (all).
- **Statuses:** Member (9.1), Route (9.2).
- **Validations:** V-47, V-51, V-52, V-53, V-54.
- **Decisions/assumptions:** D-24, D-26, A-51, A-52, A-56.
- **Related modules:** M2 (cities), M3 (matching settings), M14 (dashboard), M15 (reports).
