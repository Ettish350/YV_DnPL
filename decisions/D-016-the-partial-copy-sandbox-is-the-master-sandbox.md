# D-016: The partial copy sandbox is the master sandbox

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 22.09.2026 |
| Challenges | none |

## Decision
All pilot build and testing happens in the partial copy sandbox, alias `yv-partial`. Every builder connects with their own user. Production is never connected from this repo.

## DD source / evidence
Lior's decision, 22.09.2026.

## Alternatives considered
A separate build sandbox: not needed for the pilot.

## Affects
sources/ORGS.md, CONVENTIONS.md, every Salesforce command.

## History
- 22.09.2026: accepted.
