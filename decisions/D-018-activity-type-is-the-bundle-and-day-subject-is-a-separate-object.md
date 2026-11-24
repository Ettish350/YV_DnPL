# D-018: Activity type is the Bundle; the activity day subject is a separate object

| | |
|---|---|
| Status | **accepted** |
| Decided by | Anna |
| Date | 08.10.2026 |
| Challenges | none |

## Decision
1. `new_tour_type` (activity type) maps to the Bundle itself. The activity type is not a separate object and not only a field on the Bundle.
2. `new_activity_day_subject` (activity day subjects) maps to its own object, separate from the Bundle and from `Product_Subject__c`. It stays an entity, not a picklist (R4). API name to be set in Stage A.
3. The activity day template links to the day subject, not to the Bundle (R3).

## DD source / evidence
Doc 2 V9 (chapter "בניית מוצרים, חבילות ותבניות יום פעילות") says the activity type is a field on the Bundle and that the Bundle is not the activity type (R2, comment c143). This decision takes the opposite reading of R2. Per CLAUDE.md the DD wins over this repo, so the contradiction is logged in `sync/A1-anna.md` and needs confirmation from Niv and Yad Vashem.

## Alternatives considered
- Activity type as a field on the Bundle (the DD wording): rejected by the owner.
- Day subject as a picklist or merged into `Product_Subject__c`: closed by R4; the two Dynamics entities are different (53 day subjects vs 344 product topics).

## Affects
A1-02, A1-04, A1-05, A1-10, D-007 (its F1 note is now settled), F1, F2, F3, F4.

## History
- 08.10.2026: accepted.
