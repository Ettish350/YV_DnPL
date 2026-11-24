# D-045: Discounts are stored on both the order line and the order

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
A discount can sit on an order line and on the whole order, both. It is implemented as fields and logic, not as a dedicated object. The budget source of a discount is the budget framework (PR #27).

## Source
Doc 2 process 08, comment c432 (Malam's position, 23.09.2026): discount at field and logic level, not a dedicated entity. Comment c430 had left the topic for a dedicated discussion.

## Affects
Plan item D4. `knowledge/model-register.md` row 17. Existing fields: `OpportunityLineItem.Discount_Reason__c`, `Opportunity.DiscountApprovedBy__c`, `Opportunity.DiscountBudgetSource__c`.

## History
- 16.11.2026: accepted.
