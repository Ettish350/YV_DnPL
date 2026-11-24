# YV20-226 (O1): defaults on a new order, and no closing without a budget framework

DD: Doc 2 process 03 A03-01 and process 04 V04-04. Org: `yv-partial`, read-only retrieve on 17.11.2026 (96 Flows, 154 Apex classes, 12 triggers, Opportunity object). 

## 1. Impact check result

| Question | Result |
|---|---|
| Does anything close an Order to Closed Won on its own? | No. The portal (`yvEduNewReservationController`) creates the order at New (line 1417) and moves it to Waiting for Approval (line 227). Tal's `Connected_Bundle_Create_Opportunity_Products` and `Create_Bundle_Items` set Waiting for Approval. No change needed from Tal. |
| Does anything already set the defaults on create? | No. The portal sets groups, participants and Stage from the request, and the language only when it is sent. `Opportunity_Before_Create_Update` only assigns the Desk. |
| Validation rules on Opportunity today | None. |
| Apex tests that create an Opportunity | `GS_StaffFilterLogicTest` uses the first Stage value (not closed). `yvEduNewReservationControllerTest` creates through the controller (New). No test closes an order. |
| Anything else that reacts to Closed Won | `Opps_that_create_a_gift_commitment` (record-triggered, update, Stage = Closed Won, no Record Type filter). See finding B. |

## 2. Findings

**A. DD contradiction (decided by Anna, 17.11.2026).** Doc 2 A03-01 says the new order opens with one group and one participant. The implementation spec in the same chapter (`03-manual-order-intake.md`, "פתיחת הזמנה ידנית ובניית הקבוצות", step 1) says groups and participants open empty as required fields. Decision for this ticket: default 1 and 1 (the A03-01 row and the ticket). Not raised as an Issue.

**B. Gift commitment flow will fire on closed orders.** `Opps_that_create_a_gift_commitment` creates a GiftCommitment on every Opportunity that reaches Closed Won, including the Order record type. Once orders can close, each closed order gets one. Message to Tal and Niv below.

**C. Defaults on shared fields.** `NumberOfGroups__c` and `NumberOfParticipants__c` are fields of the whole Opportunity object. A field default cannot depend on the record type, so Grant and Individual Gift opportunities will also open with 1 and 1. The Stage default (business process) and the language default (record type picklist) are Order only.

**D. Migration.** The validation rule also blocks inserting an Order that is already Closed Won without a budget framework. Legacy closed orders loaded by data migration need a budget framework, or the rule must be bypassed for the load. **Note for Lior (also in `sync/DM-anna.md`): when the legacy orders are loaded, this validation rule must be turned off before the load and turned on again after it.**

## 3. Proposal (in `metadata/O1/`, deployed to `yv-partial` on 17.11.2026 after Anna's approval)

| Rule | Component | Value |
|---|---|---|
| A03-01 Stage | **Not solved by a field default.** Tested 17.11.2026: Salesforce keeps no default for Opportunity Stage (the `OpportunityStage` default flag is accepted by the deploy but not stored, and the business process refuses a default). The new-order form opens with Stage empty. Options in the conversation: Quick Action with predefined values, or a Screen Flow overriding New. The business process list was reordered (New first); it changes nothing in the form. | solved with an override of New, see below |
| A03-01 Language | Record type `Order`, picklist `InstructionLanguage__c`, default | `Hebrew` |
| A03-01 Form language | Record type `Order`, picklist `FormLanguage__c`, default (added 17.11.2026 at Anna's request) | `Hebrew` |
| A03-01 Groups | Field `NumberOfGroups__c`, default value | `1` |
| A03-01 Participants | Field `NumberOfParticipants__c`, default value | `1` |
| V04-04 | Validation rule `Budget_Framework_Required` on Opportunity | Order record type, Stage = Closed Won, `Budget_Framework__c` empty. Error on the budget framework field: "יש לשייך מסגרת תקציבית להזמנה" |

The user sees the values when the new order form opens; no Flow is used. Only an empty field gets a default: a value set by the portal or the user is never overwritten.

## 4. Test plan

1. New order, Order record type, from the UI: Stage New, language Hebrew, groups 1, participants 1. Change all four and save: saves.
2. Move an order without a budget framework to Closed Won: blocked with the Hebrew message. Select a budget framework and repeat: saves.
3. Add a bundle to a test order (`Add_Bundles_to_Opportunity`, then the bundle flows): works, order ends at Waiting for Approval.
4. Order created by the portal (test via `yvEduNewReservationController`): stays at Waiting for Approval, never Closed Won; its groups and participants are the ones from the request, not 1.
5. A Grant opportunity opens with groups 1 and participants 1 and nothing else changes (finding C).

## 5. Message to Tal and Niv (finding B)

To: Tal, Niv. Subject: YV20-226, gift commitment flow will run on closed orders.

The flow `Opps_that_create_a_gift_commitment` runs after an update when Stage becomes Closed Won, for every Opportunity, with no Record Type filter. We are adding a rule that lets an Order close only with a budget framework (YV20-226). From then on, every Order that closes creates a GiftCommitment. Orders are not donations, so please add a Record Type condition to the flow (exclude `Order`, or run it only for `Individual_Gift_Opportunity` and `Grant_Opportunity`, whichever fits the donations process). Please confirm when it is done in `yv-partial`. We do not change the flow ourselves. Nothing else you built needs a change: your flows and the portal never set an order to Closed Won.

## 6. Open for the next step

- Add the `metadata/O1` folder as a package directory in `sfdx-project.json` before the deploy (the metadata gate goes through a branch and pull request, D-015).
- Tal's change to the flow (finding B) is not a condition for this rule: no component closes orders today.

## 7. New button with defaults (added 17.11.2026)

Stage has no default in Salesforce (checked in Setup by Anna: no Default column). The New action of Opportunity is overridden (Lightning, Large) by the Aura component `O1_NewOrderOverride`, which wraps the LWC `o1NewOrder`. A user who has the custom permission `O1_Order_Defaults` gets the standard form with Record Type Order and Stage New, Hebrew, 1 group, 1 participant. Anyone else gets the standard New. The custom permission `O1_Order_Defaults` was added by Anna on 17.11.2026 to the existing `YadVashem_Order_Permission_Set` (Niv's); no new permission set. A temporary `O1_Order_Coordinator` was created for testing and deleted the same day. (An override per app is not allowed for New; Lightning accepts only View and Tab.)
Deployed to `yv-partial` on 17.11.2026.
