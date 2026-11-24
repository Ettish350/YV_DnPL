# D-043: The group in an order is the GoMeddo group

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
A group in an order (a class, with its participant quota and minimum) is stored in GoMeddo's group object, `B25__Group__c`, not in Campaign.

## Why
The portal already stores groups in `B25__Group__c` and links them to the reservations. Doc 2 chapter 10.2 (entities) says Campaign; this is a known deviation, ruled by Lior.

## Affects
`knowledge/model-register.md` row 22 (closes the order-world conflict). Events are unchanged: the group there is the registrations to the same level (correction C3). Unblocks plan item D1 and the group rules in processes 02, 03, 05 and 11.

## History
- 16.11.2026: accepted.
