# YV20-227 (O2): department on the order, budget frameworks filtered by department

DD: Doc 2 process 04, A04-04. Decision: D-042 (Lior 16.11.2026). Rulings: Lior 12.11.2026 (department follows from the budget framework) and 16.11.2026 (department, not section, limits the frameworks). Org: `yv-partial`, read-only retrieve on 18.11.2026 (Flows, Apex classes, Apex triggers, Opportunity, Budget_Framework__c, Department__c, Section__c).

## 1. Impact check result

| Question | Result |
|---|---|
| Which Flow, Apex class or trigger reads or writes `Opportunity.Department__c` or `Opportunity.Budget_Framework__c`? | None. `Create_Master_Case` and `Create_New_Case` mention `Department__c`, but as a picklist on Account. `yvEduNewReservationController` only has a comment (line 72: the budget framework is not used yet). |
| Does the portal set a budget framework that would fail the filter? | No, it does not set one. |
| Does anything already set the department on the order? | No. `Opportunity_Before_Create_Update` only assigns the Desk. |
| Validation rules touching them | `Opportunity.Budget_Framework_Required` (YV20-226) and `Budget_Framework__c.Section_Department_Match`. No conflict. |
| Tal's bundle flows (`Add_Bundles_to_Opportunity`, `Connected_Bundle_Create_Opportunity_Products`, `Create_Bundle_Items`) | Do not use either field. |

Conflicts: none. Side finding: the repo file `tickets/Data Migration/metadata/Opportunity/.../BudgetFramework__c.field-meta.xml` and the record page `Order_Opportunity_Record_Page` use the name `BudgetFramework__c`. The org field is `Budget_Framework__c`. The org name is used here.

## 2. What is built (in `metadata/O2/`, deployed to `yv-partial` on 18.11.2026 after Anna's approval)

A Lookup from User to a custom object is not possible in Salesforce ("Invalid data type"; the Setup UI offers only User as a target). Anna chose a picklist on a global value set (18.11.2026); no ruling from Lior needed.

| Component | What |
|---|---|
| Global Value Set `Order_Department` | The 13 departments (list from Anna, 18.11.2026). English API values, Hebrew labels. |
| Field `User.Order_Department__c` (תווית: מחלקה (הזמנות)) | Picklist on the value set, optional. A system administrator fills it per user. Admin profile: read and edit. |
| Field `Department__c.Order_Department__c` (תווית: מחלקה (הזמנות)) | Same picklist, required. Marks each department record with its value. |
| Lookup filter on `Opportunity.Budget_Framework__c` | Required. Passes when 1) the framework's department equals the order's department, or 2) the order has no department, or 3) the user's profile is `System Administrator`. |
| New version (Draft, not active; version 2 stays active) of Flow `Opportunity_Before_Create_Update` | For an Order with no department: from the budget framework; else, on create only, the `Department__c` record whose `Order_Department__c` equals the user's `Order_Department__c`. Never overwrites a department already set. The Desk logic is unchanged. |

Why condition 2 in the filter: without it, a new order with no department shows an empty list and the order of events cannot start.

## 3. Open points

- Existing `Department__c` records have no `Order_Department__c` value yet (the field is required). Fill it for each record (Anna), or the "from the user" step finds nothing.
- The value "יעץ ומידע" is spelled as in Anna's list; confirm "ייעוץ".
- A picklist cannot be unique: two departments with the same value would give the first one. A new department needs a new value in the global value set and on its record.
- `$Profile.Name` for the administrator: the filter compares to the profile display name `System Administrator` (the API name `Admin` did not match; fixed 18.11.2026 after the test).
- Required lookup filters are enforced on API loads. Legacy orders whose budget framework belongs to another department than the order's will fail to load. Plan with the data migration, as for `Budget_Framework_Required`.
- `User.Order_Department__c` is not on a User page layout. Add it, or fill it with mass edit.
- Prefilling the department on the New order form (LWC `o1NewOrder`, YV20-226) is not done: the department from the user is set on save. Optional next step.
- Repo file `tickets/Data Migration/metadata/Opportunity/.../BudgetFramework__c.field-meta.xml` and the record page use `BudgetFramework__c`; the org field is `Budget_Framework__c`.

## 4. Test plan

1. Order with a department: the budget framework lookup shows only that department's frameworks.
2. Same order as `Admin`: a framework of another department can be picked.
3. Order with no department: all frameworks show. Pick one and save: the order gets the framework's department.
4. Order with a department set and a framework: the department is not overwritten.
5. A user with `Order_Department__c` (and a matching `Department__c` record) creates an order with no framework: the order gets the user's department. A user without it: stays empty.
6. Order with bundles (Tal's flows) and an order created by the portal still work.
7. The Draft flow version is activated for the test, and the previous version is activated back if the test fails.
