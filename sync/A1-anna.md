# Sync: A1 - Anna

Status, requests and continuation handoff. Disagreements, questions and needs from other epics go to GitHub Issues (see WORKFLOW.md).

Brief version I worked from: V4

## Continuation handoff (2026-10-05)

### Project context and guardrails
- Work is in `c:\Salesforce\Yad Vashem Orders\YV2026`, epic A1 (Anna). The latest `git pull` completed with `Already up to date`.
- Read `dd/INDEX.md` first in every session. Do not edit anything under `dd/`; the Markdown is generated from the SharePoint Word masters.
- Doc 2 governs processes and V/A codes; Doc 1 governs the general model and architecture. DD body text is binding; review comments are non-binding.
- Never deploy to an org. For Salesforce commands, name `--target-org yv-partial`. Do not commit or push unless the owner asks.
- Repo instructions require English for team artifacts. Ticket labels and deliverables for Yad Vashem follow the project Hebrew-label/RTL rules.

### Work completed in the A1 ticket book
- Every one of the 11 A1 tickets has a Hebrew `מקור באפיון` field with DD headings, rows, or V/A codes. These changes are in `tickets/A1/tickets.md`.
- A1-03 now plans only `YV_Location__c` and `Coupon__c`, the two entities explicitly named in the Doc 2 entity map. It explicitly excludes `Product_Subject__c`, `Tag__c`, and `Activity_Window__c`; a product-to-topic-list requirement exists, but the DD does not specify a separate object for it.
- A1-04 no longer plans a weekday field on `Activity_Window__c`; its remaining proposed Product2/Bundle consolidation depends on proposed D-006 approval.
- A1-05 now includes a three-column dependency matrix: controlling field, selected value, dependent fields shown. It records explicit DD conditions and marks unspecified API names/field lists as unresolved rather than inventing them.
- A1-08 still covers DD automation A01-12 (clone activity type), but no longer assumes the source/target is Bundle. Implementation depends on a decision resolving F1/F2 and approval of the clone field/relationship list.

### Open questions and dependencies
- A1-03: confirm whether the product-topic requirement should retain `Product_Subject__c` as a later design proposal. `Tag__c` and `Activity_Window__c` have no explicit DD entity requirement and are out of the current build scope.
- A1-05: Lior to provide/approve the exact matrix `controlling field | selected value | fields shown`, with API names: product-type values and field groups; online/kiosk fields; multi-channel behavior; price-book controller and pricing fields; and conditions for provider status, resource, location, duration, languages, itinerary, group classification, and topics. Clarify how a product without a price book satisfies the universal price-value requirement.
- A1-08: resolve F1/F2: identify the A01-12 source/target object and its relationship to Bundle, Bundle Items, activity-day template, and subject; approve the complete clone list before implementation.
- Follow-up required: A1-09 still lists product-subject data among catalog load files. Reconcile it with the A1-03 exclusion after the subject data model is decided.
- Other A1 blockers remain as listed at the end of `tickets/A1/tickets.md`: F1-F6, D-006, D-007, D-008/D-009 scope and the unresolved Doc 2 catalog questions. Do not silently resolve them.

### Validation and local state
- Focused checks passed for A1-03/A1-04 scope, A1-05's dependency table and matching Lior question, and A1-08's deferred target model and matching Lior question.
- No metadata was generated or deployed. Ticket status remains `To Do`; estimates were not recalculated and no pilot time was logged during this review work.
- At handoff, `tickets/A1/tickets.md` is untracked (`??`) and contains the A1 ticket book. `sync/A1-anna.md` is modified. `.sf/` and `.sfdx/` are untracked workspace state; do not remove or overwrite them. No commit or push has been made.
- The `gh` command was unavailable when checked, so open GitHub challenge/question issues were not verified. Check them when GitHub CLI is available.
- `tickets/B1/` currently contains only `.gitkeep`; no B1 tickets were updated.

### Suggested next steps
1. Start from this sync file and `tickets/A1/tickets.md`; pull and check for new answers from Lior before editing.
2. Resolve the A1-03 topic representation and reconcile A1-09's product-subject load scope.
3. Apply Lior's A1-05 visibility matrix and F1/F2 answer to A1-08; keep implementation blocked where the model remains undecided.
4. Preserve the existing local `.sf/`, `.sfdx/`, and untracked ticket file. Do not deploy.

## Ticket status
| Key | Status | Category | Est. h | Actual h | Review min | Rework h | Fixed by hand | Components actually built |
|---|---|---|---|---|---|---|---|---|

## Time log (write as you go, see PILOT-MEASUREMENT.md)
| Date | Ticket | Start | End |
|---|---|---|---|

## Blocked
- ticket, blocked by (Issue number or decision ID)

## Request to Lior
- Please approve and add a project-wide field-label convention: every Salesforce field must have an English metadata label and a Hebrew translation through Translation Workbench. Every ticket that creates fields must list both the English label and the Hebrew translation for each field. Please add this rule to `CONVENTIONS.md`, add the corresponding ticket field/instruction to `brief/epic-owners-brief.md`, and include the check in `REVIEWER.md`.
- Please also approve and add a project-wide convention: every field used by a Flow, Validation Rule, Formula, automation or integration must have a Salesforce `Description` explaining its purpose and how the automation uses it. The description should begin with the ticket key and DD code, for example: `A1-06 | V01-02 | Stores the minimum notice time used to validate online product ordering.` Please add this rule to `CONVENTIONS.md`, add the corresponding ticket field/instruction to `brief/epic-owners-brief.md`, and include the check in `REVIEWER.md`.
- Question for A1-03: Doc 2 requires linking a product to a topic list or free text but does not define a separate subject object. Please confirm whether the `Product_Subject__c` mapping should remain for a later design decision; the `Tag__c` and `Activity_Window__c` mapping rows also have no explicit DD entity requirement and are excluded from A1 build scope.
- Question for A1-05: Please provide/approve a dependency matrix in the form `controlling field | selected value | fields shown`, with actual API names where decided. Doc 2 does not enumerate product-type values and their fields, kiosk-only fields, online-publication fields, or behavior when multiple display channels are selected. Please also identify the price-book controller and pricing fields, and clarify how a product without a price book satisfies the DD requirement that every product has a price value. Specify dependencies for provider status, resource, location, duration, languages, itinerary, group classification and topics. Until approved, implement only conditions explicitly defined in the DD.
- Question for A1-08: Doc 2 requires A01-12 to clone an activity type but does not consistently define that record: activity type is described as a Bundle field, while another statement says a Bundle is not the activity type, and the mapping of `new_tour_type` to Bundle is unresolved (F1/F2). Please confirm the source/target object and its relationships to Bundle, Bundle Items, the activity-day template and its subject, and approve the complete fields and relationships to copy before implementation.

## Open Issues I raised
- #number, one line
