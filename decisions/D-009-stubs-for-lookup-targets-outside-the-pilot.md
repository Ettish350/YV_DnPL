# D-009: Stubs for lookup targets outside the pilot

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting Lior |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
Lookups from pilot entities to objects owned by later epics point to stubs (object with a name only) created in L0: Almog_Interface__c, Budget_Framework__c, Department__c. Existing and standard targets are used as they are.

## DD source / evidence
mapping/entities.md, 'Lookup targets outside the pilot'.

## Alternatives considered
Wait for the owning epic: would block A1 and B1 on A3 and C2.

## Affects
L0. mapping rows with decision 'redirect'.

## History
- 16.09.2026: proposed.
