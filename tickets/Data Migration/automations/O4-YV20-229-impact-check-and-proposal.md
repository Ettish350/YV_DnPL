# YV20-229 (O4): a school order cannot be saved without an institution

DD: Doc 2 process 04 (V04-02, A04-03) and process 03 (V03-03). Org: `yv-partial`, read-only retrieve on 18.11.2026 (Flows, Apex classes, Apex triggers, Opportunity, Department__c).

## 1. Impact check result

| Question | Result |
|---|---|
| Triggers on Opportunity | None (the 6 triggers are on Case, Account, Gift objects). |
| Portal `yvEduNewReservationController` | Creates the Order with `AccountId` from the portal user's account and does not set a department. `Opportunity_Before_Create_Update` fills the department only from the budget framework or an internal user's department, so a portal order has no department and the rule does not apply. If the portal user has no account, the order is created without one; with no department it still saves. |
| Tal's flows (`Add_Bundles_to_Opportunity`, `Create_Bundle_Items`, `Connected_Bundle_Create_Opportunity_Products`) | Create bundle items and lines only, not orders. No effect. |
| `Opportunity_After_Create_Update`, `P11_Order_Cancel_Cascade`, `P11_*` Apex | Update existing orders; none clears `AccountId`. No effect. |
| `Opps_that_create_a_gift_commitment` | Gift records, not Order. |
| `IsWithoutInstitution__c` | Not used by any flow, Apex or validation rule; a plain checkbox. |
| Other validation rules | `Budget_Framework_Required`, `P11_V11_07_Order_Cancel_Reason`. No conflict. |

Conflicts: none. Opportunity also has the Gift and Grant record types, so the rule is limited to Order.

## 2. What is built (`metadata/O4/`, deployed to `yv-partial` on 18.11.2026)

| Component | What |
|---|---|
| `Department__c.Institution_Required__c` (label `Institution required`, Hebrew translation מחייבת מוסד) | Checkbox, default false. Added to the Department layout. Marks the department whose orders need an institution. No department name or Id in the rule. Admin profile: read and edit. Approved by Anna 18.11.2026 (no wait for Lior). |
| Validation rule `Opportunity.Institution_Required_For_School_Order` | Order record type, `Department__r.Institution_Required__c = true` and `AccountId` empty: blocks the save on the institution field. Message: "הזמנת בית ספר מחייבת מוסד". One rule covers V04-02 and V03-03. Uses the order's `Department__c` (user-department question still open in O2). |

## 3. Open points

- Which departments are "school": decided in the data migration (Anna, 18.11.2026). Until then the checkbox is false everywhere, so the rule blocks nothing. Question for Lior (Issue #56).
- `IsWithoutInstitution__c`: the DD does not say it is an exception. Built with no exception. Question for Lior: may a user bypass the rule per order when it is defined per department?
- The rule blocks loading legacy orders in a school department that have no institution. Plan with the data migration (as for `Budget_Framework_Required`).
- **Account is already required on the order page** (`Opportunity_Order_Lightning_Page`, `uiBehavior = required`) and on the Opportunity and Grant layouts, in every department. So in the UI the rule is redundant, and orders without an institution (private customers, Doc 3) cannot be created. Not changed; question Issue #56 to Lior (18.11.2026).

## 4. Test plan

1. Department with the box ticked, order with no institution: blocked, message shown on the institution field.
2. Same order with an institution: saves.
3. Order in a department with the box not ticked, no institution: saves.
4. Order with bundles (Tal's flow) and a portal-created order: still work.
5. Gift or Grant opportunity with no account: saves.
