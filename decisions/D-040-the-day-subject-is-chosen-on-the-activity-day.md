# D-040: The activity day subject is chosen on the activity day

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
The activity day subject (נושא יום העיון, legacy `new_activity_day_subject`, D-018) is stored on the activity day: a lookup on the GoMeddo parent reservation (`B25__Reservation__c`, parent). It is not stored on the order (Opportunity).

## Why
An order can hold several bundles and activity days, so one subject per order is too coarse. The legacy system also keeps the subject on the activity day (`new_activity_day.new_activity_day_subject`). Doc 2 process 01, rule 8: the coordinator picks the subject from a closed list and the schedule lines are created from it.

## Open (for Neev, GoMeddo)
- D-024 says the activity day is created in GoMeddo only once type and subject are both known. With the subject on the activity day, the activity day must exist before the subject is picked. Recommended (not yet decided): the activity day is created with the order; the coordinator adds the subject later. The portal already creates GoMeddo reservations linked to the order at booking time (`yvEduNewReservationController`), so Neev also needs to say how those reservations relate to the parent reservation.
- The lookup is not built until Neev confirms. YV20-223 (the subject object) adds no lookup to any existing object.

## Affects
`knowledge/model-register.md` rows 18 and 50. D-024 (order of steps), when Neev rules.

## History
- 16.11.2026: accepted.
