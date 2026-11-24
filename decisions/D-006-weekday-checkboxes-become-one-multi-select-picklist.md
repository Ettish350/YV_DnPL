# D-006: Weekday checkboxes become one multi-select picklist

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting architect |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
Six weekday checkboxes on Product2, Bundle, Activity_Window__c and Contract_Days__c become one Multi-Select Picklist per object (Relevant_Days__c, Days_Of_Week__c, Contract_Days_Of_Week__c). The load translates six flags into values.

## DD source / evidence
No DD rule either way. Reason: simpler reports and availability formulas.

## Alternatives considered
Keep six checkboxes: works, but every availability formula checks six fields.

## Affects
A1-04, B1-05. mapping rows marked 'One of six weekday checkboxes'.

## Note, 22.09.2026
Doc 2 V9 lists contract days with a single weekday per record. If so, the multi-select does not apply to Contract_Days__c (knowledge/B1-binding-rules.md, F3).

## History
- 16.09.2026: proposed.
