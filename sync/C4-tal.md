# Sync: C4 (online self-service ordering) - Tal

Status only. Disagreements, questions and needs from other epics go to GitHub Issues (see WORKFLOW.md). Items marked "to raise" below are not yet Issues.

Last updated: 08.11.2026.
Brief version I worked from: not re-checked this session (work started from a mockup, `yvNewReservation`).
Org used: `YV2026` (same org as `yv-partial`: instance URL `yadvashemtheworldholocaustremembr2--partial.sandbox.my.salesforce.com`), approved by Tal for read-only retrieves. Nothing was deployed by Claude: Tal deploys from the generated metadata.

## Where the code is
For now the code is only on Tal's machine, in `metadata/LWCs/main/default/` (not in the repository yet). Tal deploys it to the org from those files; Claude never deploys. It will be added to the repository through a branch and pull request when Tal decides (metadata does not go to `main` directly).
- LWCs: `yvEduNewReservation` (parent), `yvEduNewReservationActivity`, `yvEduNewReservationWho`, `yvEduNewReservationWhen`, `yvEduSharedCss`, and the changed `yvEduNavBar` and `yvEduLoginForm`
- Apex: `yvEduNewReservationController` and `yvEduNewReservationControllerTest`
- Custom Labels `yvEdu_Res_*` (34) and the Hebrew translation file `translations/iw.translation-meta.xml`
- Two new fields: `Bundle_Item_Entry__c.Price__c`, `OpportunityLineItem.Bundle_Item__c`
- `yvEduNewReservationItems` was removed locally (its step was merged into Activity). An old copy is still in the org, unused, and can be deleted.
- Not planned for the repository: the mockup `yvNewReservation`, and `org-snapshot/` (a local read-only copy of the org metadata)

## What exists now: the "new reservation" flow (Experience Cloud, LWR site "Education")

One parent LWC with the stepper and all state, one child LWC per step, one Apex controller. Nothing is saved while the customer moves through the steps; one call at the end creates the whole order. End to end it works in the sandbox (Tal confirmed an order is created).

Steps: Activity, Who, When, Done. The stepper is a timeline: finished steps are green with a tick, the current one is highlighted, and a step opens only when all steps before it are complete.

| Component | Role |
|---|---|
| `yvEduNewReservation` (parent) | Stepper, keeps the choices, sends the order once, shows the Done step with the Opportunity id. Step order is the `STEPS` array only. |
| `yvEduNewReservationActivity` | Audience Category first (only the values set on the user's Account; if there is only one it is chosen automatically and the question is skipped). Then the bundles of that audience (filters: all, up to 3h, up to 6h). Several different bundles can be selected, each once, no quantity. Inside each selected bundle card the items are ticked, at least one per selected bundle. |
| `yvEduNewReservationWho` | Groups: class name and number of participants (1 to 35). Duplicate and remove are icon buttons. |
| `yvEduNewReservationWhen` | Month calendar (only days where all resources are free, from GoMeddo, 12 months ahead), then the free start times of the day. Spinner while loading. Button "Create order". |
| `yvEduNewReservationController` (+ Test) | `getAudienceCategories`, `getActivities`, `getAvailableDates`, `getAvailableTimes`, `submitOrder(String requestJson)`. |
| `yvEduSharedCss` | Shared design tokens and all styles (primary color #2c2f6a, the official Yad Vashem color). Each component also carries a copy of it, because importing it (`@import 'c/yvEduSharedCss'`) gave a white screen on the site. After any change the file must be copied into the four CSS files again. |
| `yvEduNavBar` | White Yad Vashem logo (static resource `yvEduLogo`, links Home), links aligned to the start side (left in English, right in Hebrew) with margin from the logo and the user, "My reservations" goes to page `MyReservations__c`, the signed-in user's name next to the avatar. |

### What `submitOrder` creates (one transaction, all or nothing)
- Opportunity: name = Account name + visit date, stage `New`, record type `Order`, price book from the user's account (`Account.Price_Book__c`), `Audience_Category__c`, `NumberOfGroups__c`, `NumberOfParticipants__c`, `CloseDate` = visit date, `Visit_Start_Date__c` and `Visit_End_Date__c`.
- One Opportunity Product per chosen Bundle Item (`OpportunityLineItem.Bundle_Item__c` points back to the item). Unit price comes from `Bundle_Item_Entry__c.Price__c` for the order's price book, else from the price book entry. Price is never taken from the page.
- One `B25__Group__c` per class: `Name` and `Number_Participants__c` (new Number field, 3,0).
- One GoMeddo `B25__Reservation__c` per resource, each resource booked for the hours of its own items. Type "Reservation", status "מחכה לאישור", title "הזמנה של <opportunity name> - <resource name>", Quantity = participants.
- Then the Opportunity moves to stage `Waiting for Approval`.
- One `Connected_Bundle__c` per chosen bundle, named "<opportunity name> - <bundle name>" (shortened to 80 characters).
- Everything the page sends is re-validated on the server: audience against the Account, items against the audience, group sizes, and the date and time against GoMeddo. On any error everything is rolled back.
- The flow `Connected_Bundle_Create_Opportunity_Products` was turned off by Tal, because Apex now creates the Opportunity Products and the Reservations (the flow asked for an end date that did not exist yet).

### How availability works
- `B25.AvailableTimeRanges.getTimeRanges` (GoMeddo Apex API) per resource (`Product2.Resource__c` of the chosen items). A day is free when every resource has a free range. A start time is offered when every resource is free for the hours its items take (`DurationOfHours__c` x quantity), starting then, every 30 minutes. The page only receives free or busy, never reservation data.
- Items without a resource use a default window of 08:00 to 17:00.
- Buffers are configured inside GoMeddo by Tal. Not yet verified that this API applies them.
- The GoMeddo managed classes respect the caller's sharing, so the external user must see the configuration records (see "Org changes").

### Texts and languages
- All texts are Custom Labels `yvEdu_Res_*` (categories `yvEdu,Reservation`), English as master language, Hebrew in the translation file. The page is bilingual through the site language (`/iw/` and `/en-US/`).
- After each deploy of components or labels the site must be Published, otherwise the old labels show.

## Decisions taken (by Tal)
- Nothing is saved until the end of the flow. No draft Opportunity (changed from "open an Opportunity in New when the activity is chosen").
- Audience Category is asked first on the activity page; bundles and items are filtered by the same picklist value. The offered values come from the new `Account.Audience_Category__c`.
- A customer can choose several different bundles in one order, each once, with no quantity. The separate "Choose items" step was removed.
- Opportunity Product is the way to store the chosen items (not `Connected_Bundle_Item__c`, which was deleted). `Bundle_Item__c` lookup added on Opportunity Product.
- `Connected_Bundle__c` is created, one per bundle (an order can have more than one bundle).
- `B25__Group__c` is the group object (not `Registration__c`, not Campaign).
- Group profile removed from the UI until a backend field exists.
- Opportunity name: Account name + visit date, without the bundle name.
- Field and label names in English, Hebrew through translation (differs from CONVENTIONS.md, which says Hebrew labels).
- No temporary hold of the date for now.

## Differences from the DD or the model register (to raise, Lior or Anna decide)
- DD 02, step 7: the order is created after payment. The flow now creates the Opportunity at the end of the steps, with no payment step yet.
- `knowledge/model-register.md` row 22 says a group in an order is a Campaign, and row 36 mentions Registration. Tal's ruling: `B25__Group__c`. The register needs the change.
- DD 02 describes museum entry rules (long products: pick the start time and the system finds a museum entry; short products: pick the museum entry time and see the arrival time, windows every 10 minutes, capacity 100, regulation window 09:30 to 13:30). Not built: the museum is treated like any resource with a duration. Tal decided to leave it so for this stage.
- DD: 50 participants per group applies only in the Tagalit and Overseas frameworks. Not built, the limit is 35 for everyone.
- Conventions: labels in English (see above).

## Needs from others (to raise)
- Anna: closed list for the group profile ("אפיון לקבוצה"), the dynamic questionnaire per product (which object, which questions), how the budget framework is computed (35 vs 50 participants).
- Niv: permissions for the external user (profile `Education`, license Customer Community Login). See "Org changes".
- Anna and Niv: license. GoMeddo recommends Community Plus for Experience Cloud. Several problems here come from the basic license (junction objects, sharing).

## Org changes made by hand (Tal), from the generated metadata
- New fields: `Bundle_Item_Entry__c.Price__c`, `OpportunityLineItem.Bundle_Item__c`.
- New fields created by Tal in the org: `Bundle_Item_Entry__c.Price_Book__c` and `Account.Price_Book__c` (lookups to Pricebook2), `Account.Audience_Category__c` (multipicklist on the global value set `Audience_Category`), `B25__Group__c.Number_Participants__c`.
- External sharing set to Public Read Only on the GoMeddo configuration objects needed for availability (Resource, Resource Type, Staff, Availability, Reservation Status, Reservation Type, Timeframe, Schedule, Booking Restriction Rule, Dimension). `B25__Reservation__c` stays Private on purpose.
- Profile `Education`: object and field permissions for Opportunity, Opportunity Product, Bundle objects, price book objects, `B25__Group__c`, and the new Account fields.
- `Bundle__c` has Edit for the external profile. `Connected_Bundle__c` is a junction (master-detail to Opportunity and `Bundle__c`), and creating it needs access to both parents even when Apex runs in system context. Decision for Niv: keep Edit on `Bundle__c` for external users, or find another way. It lets the external user edit bundles, so it is a risk.
- Site branding set `Portal`: background color changed to the official color in Experience Builder (Theme, Colors).
- The flow `Connected_Bundle_Create_Opportunity_Products` is deactivated.

## Known issues and not built yet
- Payment, order confirmation, communication, editing an order after creation: not built.
- Group profile and the dynamic questionnaire: not built, waiting for the backend design.
- Product rules not applied: `MinimumDays__c`, `Minimum_Notice_Time__c`, `Advance_Booking_Limit__c`, `Relevant_Days__c`, closure days and holidays, `MaxProductsPerDay__c`. Units and precedence are not defined.
- Debug output is still in the code and must be removed before go-live: errors are returned in the response instead of thrown (`TODO-DEBUG`: `SaveResult.error`, `DatesResult.error`, `TimesResult.error`, `describeRunningUser`, `describeResources`, `dmlDetails`).
- The Apex test class has not been confirmed passing in the org. The code references GoMeddo classes (`B25.AvailableTimeRanges`) that cannot be compiled outside the org.
- Unused items left in the org: component `yvEduNewReservationItems`, labels `yvEdu_Res_DateHint` and `yvEdu_Res_OrderNow`.
- The code is not in git yet (see "Where the code is").
- Lessons: a SOQL for-loop with a bind variable inside an inner class fails at runtime in this org (`Variable does not exist: tmpVar1`); a custom Apex class parameter arrived empty from the LWR site, so the order is sent as a JSON string; a label description (`shortDescription`) is limited to 80 characters; GoMeddo returned a NullPointerException when the external user could not see the resource (fixed by the sharing change above).

## Ticket status
| Key | Status | Category | Est. h | Actual h | Review min | Rework h | Fixed by hand | Components actually built |
|---|---|---|---|---|---|---|---|---|
| (no Jira key recorded) | in progress | layout, automation, permissions | not recorded | not recorded | | | | see above |

Hours were not logged during the session as PILOT-MEASUREMENT.md requires. They cannot be reconstructed reliably, so none are entered here.

## Time log (write as you go, see PILOT-MEASUREMENT.md)
| Date | Ticket | Start | End |
|---|---|---|---|
| 2026-10-26 to 2026-11-08 | not recorded | | |

## Blocked
- Group profile and questionnaire: waiting for Anna.
- `Bundle__c` Edit for external users: decision pending (Niv).

## Open Issues I raised
- None yet. The items under "Differences from the DD" and "Needs from others" are drafts for question or challenge Issues, to be shown to Tal before posting.
