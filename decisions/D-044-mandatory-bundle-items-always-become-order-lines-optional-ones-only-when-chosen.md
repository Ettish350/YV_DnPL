# D-044: Mandatory bundle items always become order lines; optional ones only when chosen

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
When an order is created from a bundle, every mandatory bundle item becomes an order line (Opportunity Product). An optional bundle item (paid or free) becomes an order line only when the customer or the coordinator chooses it.

## Source
Doc 2 process 01, rule 3 and the entity table: a bundle item carries product, quantity, mandatory or optional, per channel. The 9.11.2026 fields "shown to the customer" and "removable" still apply on top of this.

## Affects
Plan item D12. Bundle item to order line automation (process 01). `knowledge/model-register.md` row 17.

## History
- 16.11.2026: accepted.
