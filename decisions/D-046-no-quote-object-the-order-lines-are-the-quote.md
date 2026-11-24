# D-046: No Quote object: the order lines are the quote

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
Salesforce Quotes stay off. Where the DD says "quote line" (שורת הצעת מחיר), read order line (Opportunity Product). The quote sent to the customer is a document generated from the order and its lines.

## Why
Quotes are not enabled in `yv-partial`, and a second set of lines would have to be kept in sync with the order lines. Branded documents need the third-party document product (standing decision), so generating the quote document needs it too.

## Affects
Plan item D5. Doc 2 processes 01 (V01-04: "only components in the quote" = only products on the order lines), 03 and 08. `knowledge/model-register.md` row 17.

## History
- 16.11.2026: accepted.
