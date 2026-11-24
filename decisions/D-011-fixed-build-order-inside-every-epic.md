# D-011: Fixed build order inside every epic

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting Lior |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
Fields, then page layout, then validation rules, then automations (record-triggered, screen, scheduled), then data load, then permission set, then end-to-end test, then updating 'Components' in every ticket.

## DD source / evidence
Practice: a validation added after an automation breaks it; a scheduled flow built first can only be tested the next day.

## Alternatives considered
Free order per owner: makes the pilots incomparable.

## Affects
Brief 8.1. Order of all tickets.

## History
- 16.09.2026: proposed.
