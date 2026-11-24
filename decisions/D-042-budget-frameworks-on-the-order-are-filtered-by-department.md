# D-042: Budget frameworks on the order are filtered by department

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
The budget frameworks offered on an order are filtered by the department, derived from the user who creates the order; a system administrator can pick manually (Doc 2 A04-04). The hierarchy stays department, then section (for reporting), then budget framework (Lior with Tal, 12.11.2026).

Whether the department (or the framework) also limits which day subjects can be picked is **open**. Doc 2's entity table puts "רשימת נושאי יום" on the framework; A04-04 reloads the lists "לפי המחלקה"; no legacy field links the two. Nothing is built for it until Lior or Yad Vashem decides.

## Needed
The system must know each user's department. Not yet checked whether a field for it exists in `yv-partial`.

## Affects
`knowledge/model-register.md` rows 13 and 14.

## History
- 16.11.2026: accepted.
