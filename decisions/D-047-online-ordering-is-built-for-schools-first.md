# D-047: Online ordering is built for schools first

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 16.11.2026 |
| Challenges | none |

## Decision
For now the online ordering build (Doc 2 process 02) covers institutions, specifically schools. Rules for private customers and travel agents are deferred: card payment before completion, registration and handling fees, hour changes in a private order, bar mitzvah birth year, and "add another order" for agents.

## What stays in scope now
Identifying a school by institution code or company number (with a pick list when several match); a blocked institution cannot complete an order; an unknown school is sent to the new-customer form, which creates a temporary card for the orders center to approve. Opening the customer in Almog from Salesforce stays out of scope (Lior, 12.10.2026).

## Affects
Sprint planning for Tal. The deferred rules stay in the DD and the plan; they are not cancelled.

## History
- 16.11.2026: accepted.
