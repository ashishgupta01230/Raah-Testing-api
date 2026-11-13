# Module M7 — Rating & Feedback

> Source: `RAAH_Functional_Specification.md` Section 16. Build order #7.

## Purpose

Let connected members rate each other so that trust builds up on the platform and members can choose with more confidence. Rating is in Phase 1 scope (D-13).

## Who Uses It

A Member rates another member they have been connected with. Admin moderates comments (BR-16.8).

## Dependencies

- M5 (Accepted connections, past or present).
- Foundation config: CFG-13 (rating enabled), CFG-14 (comment moderation), CFG-27 (comment length), CFG-28 (minimum ratings before average shows).
- Foundation statuses: Rating (9.8). Record numbering RAT-.

## Screens

| Screen | Purpose |
|---|---|
| SCR-16.1 Rate Member | Choose 1–5 stars and an optional comment |
| SCR-16.2 Ratings Received | Ratings and comments the member received |
| SCR-16.3 Ratings Given | Ratings the member gave |

## Field Specification — Rating

| Field | Type | Mandatory | Rules |
|---|---|---|---|
| Rating Number | Auto | — | RAT-nnnnnn |
| Connection | Auto | — | The connection the rating belongs to |
| From / To | Auto | — | The rater and the member rated |
| Stars | Select 1–5 | Yes | Whole numbers only |
| Comment | Text | No | Up to CFG-27 characters (default 300). No web links or runs of 10 or more digits. |
| Date | Auto | — | System timestamp |
| Status | Auto | — | Submitted / Hidden (Section 9.8) |

## Business Rules

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

## Status Lifecycle

Uses Rating Status of Section 9.8.

| From | To | Who |
|---|---|---|
| (new) | Submitted | Member |
| Submitted | Hidden (comment only) | Admin, with reason |

## Scenarios

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

## Acceptance Criteria

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

## Data Impact

| Event | Records created or changed |
|---|---|
| Rating submitted | Rating created; member's average and count updated; rated member notified |
| Comment hidden | Rating status Hidden; audit entry; rater notified |

## Reports Produced

RPT-14 Rating Summary, RPT-15 Low-Rated Members, RPT-16 Rating Moderation Log — in M15.

## Cross-References

- **Config:** CFG-13, CFG-14, CFG-27, CFG-28.
- **Statuses:** Rating (9.8).
- **Validations:** V-41, V-42.
- **Decisions/assumptions:** D-13, A-8, A-44, A-45.
