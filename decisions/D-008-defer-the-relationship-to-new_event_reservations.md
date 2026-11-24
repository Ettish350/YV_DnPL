# D-008: Defer the relationship to new_event_reservations

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting Lior |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
`new_event_reservations` (unified events) is a Doc 3 entity with 17 incoming lookups. In the pilot the object is not built and no lookup to it is created.

## DD source / evidence
Doc 2 structure table: events belong to Doc 3.

## Alternatives considered
Build a stub now: creates lookups whose meaning Doc 3 has not defined yet.

## Affects
A1-03, mapping rows with target new_event_reservations.

## History
- 16.09.2026: proposed.
