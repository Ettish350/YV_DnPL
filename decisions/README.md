# Decision register

Every decision that shapes the build lives here, one file per decision. The brief and the mapping refer to these IDs.
**Any decision can be challenged, including Lior's.** A challenge is a GitHub Issue, not an edit to these files.

## Status values

| Status | Meaning |
|---|---|
| proposed | Written down, not yet ruled on by the person named. Build on it only where the ticket says so |
| accepted | Ruled on. Build on it |
| challenged | An open challenge Issue exists. Keep building unless the Issue says the work is blocked |
| reversed | Overturned. The file says what replaced it |

## How to challenge
1. Open an Issue with the **Challenge a decision** template (`gh issue create --template challenge.yml`).
2. Evidence is required: a DD quote with chapter and code, or a concrete case where the decision fails.
3. Lior's or Anna's Claude sets the decision to `challenged` and links the Issue in the file's "Challenges" row.
4. The reviewer checks the evidence and comments. Lior or Anna rules in the Issue.
5. If upheld: a pull request changes the decision file (status `reversed` or updated), the mapping or brief if affected, and CHANGELOG. The Issue closes with the PR.
6. If rejected: the decision file gets a History line and the Issue closes.

New decisions are added by Lior's or Anna's Claude only, with the next free ID.

## Register

| ID | Decision | Status | Open challenges |
|---|---|---|---|
| [D-001](D-001-cut-the-epics-by-the-lookup-graph.md) | Cut the epics by the lookup graph | accepted | none |
| [D-002](D-002-a1-and-b1-are-the-pilot-epics.md) | A1 and B1 are the pilot epics | accepted | none |
| [D-003](D-003-each-epic-owner-builds-the-fields-of-their-own-epic.md) | Each epic owner builds the fields of their own epic | accepted | none |
| [D-004](D-004-claude-proposes-the-mapping-the-architect-reviews-it.md) | Claude proposes the mapping; the architect reviews it | accepted | none |
| [D-005](D-005-fold-the-11-role-checkboxes-into-one-dated-status.md) | Fold the 11 role checkboxes into one dated status | proposed | none |
| [D-006](D-006-weekday-checkboxes-become-one-multi-select-picklist.md) | Weekday checkboxes become one multi-select picklist | proposed | none |
| [D-007](D-007-move-23-activity-type-display-fields-from-a1-to-c4.md) | Move 23 activity-type display fields from A1 to C4 | proposed | none |
| [D-008](D-008-defer-the-relationship-to-new_event_reservations.md) | Defer the relationship to new_event_reservations | proposed | none |
| [D-009](D-009-stubs-for-lookup-targets-outside-the-pilot.md) | Stubs for lookup targets outside the pilot | proposed | none |
| [D-010](D-010-a-component-belongs-to-the-epic-where-it-is-enforced.md) | A component belongs to the epic where it is enforced | proposed | none |
| [D-011](D-011-fixed-build-order-inside-every-epic.md) | Fixed build order inside every epic | proposed | none |
| [D-012](D-012-service-provider-identity-fields-use-standard-contact-fields.md) | Service provider identity fields use standard Contact fields | proposed | none |
| [D-013](D-013-default-type-translation-from-dynamics.md) | Default type translation from Dynamics | proposed | none |
| [D-014](D-014-client-documents-stay-on-sharepoint-never-in-git.md) | Client documents stay on SharePoint, never in git | reversed by D-031 | none |
| [D-015](D-015-merging-a-pull-request-is-the-approval-to-deploy.md) | Merging a pull request is the approval to deploy | proposed | none |
| [D-016](D-016-the-partial-copy-sandbox-is-the-master-sandbox.md) | The partial copy sandbox is the master sandbox | accepted | none |
| [D-017](D-017-the-repo-is-the-master-for-tickets.md) | The repo is the master for tickets | reversed by D-028 | none |
| [D-018](D-018-activity-type-is-the-bundle-and-day-subject-is-a-separate-object.md) | Activity type is the Bundle; the activity day subject is a separate object | accepted | none |
| [D-028](D-028-jira-is-the-master-for-tasks-the-sprint-excel-is-for-planning.md) | Jira is the master for tasks; the sprint Excel is for planning | accepted | none |
| [D-029](D-029-two-week-sprints-and-the-communication-flow.md) | Two-week sprints and the communication flow | accepted | none |
| [D-030](D-030-data-model-work-is-research-build-and-page-layouts-per-entity.md) | Data-model work is research, and build with page layouts, per entity | accepted | none |
| [D-031](D-031-documents-may-be-committed-unless-they-hold-personal-information.md) | Documents may be committed unless they hold personal information; the git copy of the DD is binding | accepted | none |
| [D-032](D-032-the-approved-plan-workbook-and-its-markdown-copy-are-in-git.md) | The approved plan workbook and its Markdown copy are in git | accepted | none |
| [D-033](D-033-fls-through-the-yv-order-user-profile-and-system-administrator.md) | Field-level security through the YV Order user profile and System Administrator | accepted | none |
| [D-035](D-035-implementers-approve-their-own-research-and-build-without-lior.md) | Implementers approve their own data-model research and build it in yv-partial, without Lior's approval | accepted | none |
| [D-036](D-036-build-per-task-fields-and-automations-with-two-fixed-commands.md) | Build per task: the fields and automations one process step needs, with two fixed commands | accepted | none |
| [D-037](D-037-doc3-approved-by-yv-joins-the-binding-dd.md) | Doc 3, approved by Yad Vashem, joins the binding DD | accepted | none |
| [D-040](D-040-the-day-subject-is-chosen-on-the-activity-day.md) | The activity day subject is chosen on the activity day | accepted | none |
| [D-041](D-041-the-schedule-template-is-chosen-by-bundle-and-subject.md) | The schedule template is chosen by bundle and subject | accepted | none |
| [D-042](D-042-budget-frameworks-on-the-order-are-filtered-by-department.md) | Budget frameworks on the order are filtered by department | accepted | none |
| [D-043](D-043-the-group-in-an-order-is-the-gomeddo-group.md) | The group in an order is the GoMeddo group | accepted | none |
| [D-044](D-044-mandatory-bundle-items-always-become-order-lines-optional-ones-only-when-chosen.md) | Mandatory bundle items always become order lines; optional ones only when chosen | accepted | none |
| [D-045](D-045-discounts-are-stored-on-both-the-order-line-and-the-order.md) | Discounts are stored on both the order line and the order | accepted | none |
| [D-046](D-046-no-quote-object-the-order-lines-are-the-quote.md) | No Quote object: the order lines are the quote | accepted | none |
| [D-047](D-047-online-ordering-is-built-for-schools-first.md) | Online ordering is built for schools first | accepted | none |
