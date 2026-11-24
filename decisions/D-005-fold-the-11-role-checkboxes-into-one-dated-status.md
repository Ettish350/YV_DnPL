# D-005: Fold the 11 role checkboxes into one dated status

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting architect |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
The 11 `new_rsv_status_is_*` checkboxes on the service provider become one picklist `Status_Type__c` on `Service_Provider_Status__c`, with start and end dates. Each active checkbox in the source becomes one status record at load.

## DD source / evidence
Doc 2, chapter 'זמינות מדריכים וניהול נותני שירות': dated statuses (סטטוסים מתוארכים) and their effect on scheduling; rule V13-03.

## Alternatives considered
Keep 11 checkboxes on Contact: cannot answer when a role started or ended, which B3 scheduling needs.

## Affects
B1-03, B1-05, B1-10. mapping/B1-field-mapping.md rows marked 'One of the 11 role checkboxes'.

## Note, 22.09.2026
Doc 2 V9 defines the dated status record itself (entity table row "סטטוס נותן שירות (רשומה מתוארכת)"): this is a DD requirement. Open point: the status values must also include "מדריך 25" and "מדריך רגיל" (knowledge/B1-binding-rules.md, F2).

## History
- 16.09.2026: proposed.
