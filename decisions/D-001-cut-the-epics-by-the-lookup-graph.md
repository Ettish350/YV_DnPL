# D-001: Cut the epics by the lookup graph

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor (proposed by Lior's Claude) |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
The module is split into a shared gate (L0), three tracks (A catalog and pricing, B service providers and scheduling, C orders and routing) and cross-cutting epics (X1-X5). Tracks follow clusters of entities with few lookups between them, not the order of the DD chapters.

## DD source / evidence
YV fields workbook: 195 lookups, 46 many-to-many. Doc 2 chapter 'תהליכים עסקיים' (one business epic per chapter).

## Alternatives considered
Cut by DD chapter only: rejected, because chapters that look separate share entities, which would make owners block each other.

## Affects
Brief 3.1-3.3. All epic assignments.

## History
- 16.09.2026: accepted.
