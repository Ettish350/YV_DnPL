# D-013: Default type translation from Dynamics

| | |
|---|---|
| Status | **proposed** |
| Decided by | Lior's Claude, awaiting architect |
| Date | 16.09.2026 |
| Challenges | none |

## Decision
nvarchar to Text(255) (builder narrows), ntext to Long Text Area(32000), bit to Checkbox, int to Number(18,0), decimal to Number(18,2), money to Currency(16,2), datetime to Date or Date/Time, picklist to Picklist with values from the PICKLISTS sheet. API name: drop new_, PascalCase, add __c.

## DD source / evidence
YV fields workbook column FieldType.

## Alternatives considered
Decide type per field: 1,262 decisions with no added value for mechanical cases.

## Affects
Brief 5.3. All mapping rows with confidence high.

## History
- 16.09.2026: proposed.
