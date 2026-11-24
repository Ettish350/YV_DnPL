# D-004: Claude proposes the mapping; the architect reviews it

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
Lior's Claude proposes a target object per entity and a target field per field from the DD entity map. The architect approves or changes rows; he does not map from scratch. Rows marked review or question need his decision; the rest can be approved as a batch.

## DD source / evidence
Lior's decision, 16.09.2026. Doc 2 chapter 'ישויות', entity map.

## Alternatives considered
Architect maps from scratch: rejected, slower and repeats work the DD already settles.

## Affects
mapping/. Brief 5.

## History
- 16.09.2026: accepted.
- 12.10.2026: for mapping sections written by implementers, D-030 changes the reviewer: rows marked `review` or `question` are approved by Lior, not the architect, and `high` rows are self-merged without review. Lior may still ask Niv on a specific PR (Lior).
