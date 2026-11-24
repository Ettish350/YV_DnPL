# YV20-228 (O3): order number with the department prefix

DD: Doc 2 process 04, A04-06, process step 4, comment c353. Object file: `tickets/Data Migration/06_Opportunity.md`. Org: `yv-partial`, read-only retrieve on 18.11.2026 (Flows, Apex classes, Apex triggers, Opportunity, Department__c).

## 1. Impact check result

| Question | Result |
|---|---|
| Does any Flow, Apex class or trigger build an order number, or read `OrderSequence__c`, `OrderNumber__c` or `LegacyOrderNumber__c`? | No. |
| Does the portal use the order Name as the number? | No. `yvEduNewReservationController` only puts `order.Name` in the title of a Reservation ("הזמנה של ..."). Unchanged. |
| Existing fields | `OrderSequence__c` (Auto Number `{0000000}`), `LegacyOrderNumber__c` (Text), `Department__c.Order_Prefix__c` (Text, length 3). `OrderNumber__c` did not exist. |
| Prefix values | `Order_Prefix__c` is now required on the department (Anna, 18.11.2026). A check of the existing values was run by Anna (output cut off in the session; two prefixes seen: SCH, SCM). Not completed here. |

Conflicts: none. Side findings:
- `Order_Prefix__c` is also defined in the open branch `dm/budget-framework-sections` (D-039 draft) with `required: false`. If that branch is merged as it is, it sets the field back to optional. Needs the same change there.
- The org translation file `Opportunity-iw` is empty (two lines), although the repo has a translation file of about 290 lines. Hebrew labels of Opportunity fields are therefore not in the org.
- The repo files `profiles/Admin.profile-meta.xml` and `flexipages/Order_Opportunity_Record_Page.flexipage-meta.xml` in `tickets/Data Migration/metadata/Opportunity/` use the field name `BudgetFramework__c`; the org field is `Budget_Framework__c`. The org page is `Opportunity_Order_Lightning_Page` (a copy of it is now in the repo). Deployed through reduced files.

## 2. What is built (deployed to `yv-partial` on 18.11.2026 after Anna's approval)

| Component | What |
|---|---|
| `Opportunity.OrderNumber__c`, label `Order number`, Hebrew translation `מספר הזמנה` | Formula (Text): `IF(NOT(ISBLANK(Department__r.Order_Prefix__c)), Department__r.Order_Prefix__c & "-" & OrderSequence__c, OrderSequence__c)`. With no department or no prefix: the sequence only (Anna chose this, 18.11.2026). Moves with the department by itself. |
| `Department__c.Order_Prefix__c` | Required (Anna, 18.11.2026). |
| Page `Opportunity_Order_Lightning_Page` | `OrderNumber__c` added before the sequence. |
| Profile `Admin` | Read only on `OrderNumber__c`. |

A formula field is not found by the global search.

## 3. Tests

Anna tested the field in `yv-partial` on 18.11.2026 and reported that it works. The cases were not itemized in the report, so each is listed as planned, not as confirmed one by one:

1. Order in a department with a prefix: prefix, hyphen, sequence.
2. Move the order to another department: the prefix changes by itself.
3. Order with no department: the sequence only, no error.
4. Department with no prefix: not tested, not needed, the prefix is required.
