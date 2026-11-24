# Changelog

## 16.11.2026 (Lior's rulings, D-040 to D-047)
- New decisions, all Lior 16.11.2026: D-040 the day subject is chosen on the activity day (open for Neev: when the activity day is created, and how the portal's reservations relate to it); D-041 the schedule template is a separate template-line object chosen by bundle plus subject (known deviation from Doc 2 process 01 rule 4); D-042 budget frameworks on the order are filtered by department, subject filtering open; D-043 the group in an order is `B25__Group__c`, not Campaign; D-044 mandatory bundle items always become order lines, optional ones only when chosen; D-045 discounts on the line and the order; D-046 no Quote object, the order lines are the quote; D-047 online ordering is built for schools first, private customers and agents deferred.
- `knowledge/model-register.md`: rows 12, 17, 18 and 22 updated; new row 50 (activity day subject).
- IDs D-038 and D-039 are left for the open branch `dm/budget-framework-sections`.

## 08.11.2026 (Doc 3 approved, D-037)
- Yad Vashem approved Doc 3 as final (file of 04.11.2026). New decision D-037 (Lior, 08.11.2026): Doc 3 joins the binding DD. The approved docx is in `sources/dd/`, with the three appendix 13 customer screenshots replaced (D-031; body identical to the original); `sources/SOURCES.md` lists it in the binding table, V35 and earlier drafts as superseded, and the drafts table is empty.
- `sources/drafts/doc3/` removed: its README is now `sources/DD-CHANGES-DOC3-V35.md`, the draft text export is dropped. New `sources/DD-CHANGES-DOC3-2026-11-04.md`: the 38 body changes from V35 (tracked changes accepted) to the approved file.
- `scripts/dd_to_md.py` takes Doc 3 as an optional third document (per-document process folders, codes with a letter such as A17B-01). Run on Doc 1 and Doc 2 alone it reproduces the 06.10.2026 set exactly, apart from the generation date.
- New `dd/doc3-events-finance/` (27 files) and `dd/assets/d3/`. `dd/INDEX.md` covers three documents: precedence, layout, codes 14 to 24 and 17B, entities, comments, known gaps (interfaces and permissions now point to Doc 3 chapters 9 and 8).
- Doc 1 and Doc 2 Markdown: body text unchanged; only the generation date, plus links from Doc 2's forward references to the Doc 3 chapter they name. Three references to "תשלום מדריכים ונותני שירות וקובץ מלם" used to link, wrongly, to a Doc 1 current-state section; they now link to the Doc 3 process.
- `/fields-for-task`, `/automation-spec` and the automation file template point to `dd/doc3-events-finance/` instead of the draft folder.

## 03.11.2026 (per-task build, D-036)
- New decision D-036 (Lior, 03.11.2026): each implementer owns one business process per sprint and builds it task by task. Fields: only what the task needs, recorded in the object file with two new columns, Task and Status. Automations: one file per DD code in `tickets/Data Migration/automations/`. The implementer approves, builds in `yv-partial` and tests their own work; no approval from Lior.
- New commands `.claude/commands/fields-for-task.md` and `.claude/commands/automation-spec.md`, so every implementer gets the same sources and format.
- `plan/IMPLEMENTER-PROCESS.md` rewritten as the how-to for Tal, Neev and Reut, with the first prompts for Tal's and Neev's sprint.
- `tickets/Data Migration/00-CONVENTIONS.md`: new section "From 03.11.2026: build per task"; Stage A no longer goes through every legacy field, and a draft is no longer scored by legacy fields gone through. New `tickets/Data Migration/automations/README.md` with the automation file template.
- `WORKFLOW.md` (deploy gate exception), `CLAUDE.md` and `.claude/agents/data-migration-workbook.md` point to D-036.
- The daily email is paused (Lior, 03.11.2026). The daily email and the weekly check score by the old rules until they are rebuilt for D-036.

## 28.10.2026 (Sprint 2 opened)
- `plan/sprints/2026-10-25.md` and plan workbook V18 merged into `main` as the approved Sprint 2 plan (Lior, 28.10.2026). Jira: Process YV20-145 `Sprint 2 (25/10-05/11)` with the new tasks; Sprint 1 tasks carried over moved under it with the V18 dates and estimates.
- Budget framework (`Budget_Framework__c`, including supporters and partners): research and build move from Or to Tal (Lior, 28.10.2026). The sprint tab still names Or until the workbook is next regenerated; Jira is right.
- `plan/IMPLEMENTER-PROCESS.md`: the implementer process and definition of done (done = exists in `yv-partial`).
- `tickets/Data Migration/00-CONVENTIONS.md`: Stage B no longer adds a tab to `Migration - Objects and fields.xlsx`.
- `scripts/weekly/score-tasks.py` in git; an object file still marked `In review` scores as a Draft (D-035).

## 28.10.2026 (Doc 3 V35 sent to Yad Vashem)
- `sources/drafts/doc3/`: V35, the version Lior sent to Yad Vashem on 28.10, replaces draft V34 (one change: a full session cannot be registered to, 3.2.5). Text export regenerated; `SOURCES.md` and the README updated. Due from Yad Vashem by 04.11: the full forms list with a short explanation of each form, the online self-service rules, and the updated report list (appendix 23); other material during the first month of implementation.

## 28.10.2026 (Doc 3 draft V34)
- `sources/drafts/doc3/`: draft V34 replaces V29, after two independent full reviews and Lior's decisions of 27.10 and 28.10 (see the README). Internal explanatory comments removed; text export regenerated; `SOURCES.md` updated.
- `knowledge/dd-corrections.md` C8 to C14: one actual-participants permission set, no fast days in the invoice job, orders coordinator issues invoices, unlimited date changes and fee refund by parameter (Doc 2 appendix 25 not updated), teacher-credit transfer is manual, Almog only (no Priority), invoice as PDF. Open question: purchase order as object (Doc 1) or fields (Doc 2).
- `knowledge/model-register.md` rows 21, 24, 26, 27, 43 updated (participant per session A19-10, coupon, billing amounts and payment status, financial account parent, credit approval and refunds).

## 27.10.2026 (Doc 3 draft V29)
- `sources/drafts/doc3/`: draft V29 replaces V28, with Lior's decisions of 27.10: kiosk pay at the desk (A20-12), twice-daily status update from Almog (A21-21), conference defaults by type (A16-18) and financial handling of changes after payment (A16-17), automatic card refunds to Pelecard after approval, receipt link, Almog API extensions. Text export regenerated; `SOURCES.md` and the README updated.
- `knowledge/dd-corrections.md` C7: the registration and handling fee applies only to orders with museum entry (overrides Doc 2 A02-05).

## 26.10.2026 (Doc 3 draft)
- New `sources/drafts/doc3/`: Doc 3 (Events and Finance) draft V28 (V27 replaced the same day: all replies and tracked changes now under Lior's name), the answer to Yad Vashem's 120 comments, with a chapter text export and a README. Not binding: Doc 1 and Doc 2 in `sources/dd/` stay the binding DD until Doc 3 is approved. For implementers to learn the events and finance world and set up fields with Doc 3 in view, only as Jira tasks ask. Three screenshots with customer records replaced (D-031). `sources/SOURCES.md` has a new "Drafts (not binding)" table.
## 19.10.2026 (weekly check, layouts)
- Rule 3c amended (Lior, 19.10.2026, PR #23 decision 1): a record page scores 100 only when it is assigned in the `Orders` app; a page built by an orders implementer that is not assigned yet scores 50 until it is assigned, whatever its name. Without the page retrieve, the assignment cannot be seen, so at most 50.
- Rule 3c (layouts) of `.claude/commands/weekly-progress.md` had the date defect step 3d had: a FlexiPage or Layout scored 50 only if it changed since the previous snapshot and 100 only if Jira said Done, so record pages built in July to October scored 0 (YV20-138, 142 and 144 held at 25%). Now, as D-030 says, the layout part is done when the object's record page exists, whatever its date: a Lightning record page with the D-030 name `Order-<Object Name>- Record Page`, assigned in the Orders app, or built by an orders implementer = 100; only a page layout a person edited = 50; nothing, or only the default layout Salesforce creates with the object = 0. Several objects in one task: the average. A page without the D-030 name or the Orders app assignment is listed under Mismatches (no page in `yv-partial` has the D-030 name today), not scored down. With an approved object file, approved fields not placed on the page are named. New `scripts/weekly/record-pages.py` retrieves every FlexiPage and Layout no managed package owns, and the `Orders` app, read-only before the check starts, and writes `_pages.md` per object; the run and test scripts call it. `scripts/weekly/README.md` and `plan/OPERATING-GUIDE.md` section 5 updated.
## 19.10.2026 (weekly check, portal work)
- The weekly check undercounted Tal's portal tasks (YV20-120 to 124 scored 0 to 50% when done): the inventory read 18 types and only dates, and most portal work was built 07-08.10, before the sprint baseline. `scripts/weekly/pull-inventory.sh` now lists 29 types (adds Profile, CustomLabel, Translations, StaticResource, NavigationMenu, DigitalExperienceBundle, DigitalExperienceConfig, AuraDefinitionBundle, ApexPage, SharingSet, PermissionSetGroup), at most 6 calls at once with one retry, and names failed types in `_FAILED.txt`. `.claude/commands/weekly-progress.md` step 3d: portal and other tasks count components whatever their date, retrieve LWCs, site bundles, navigation, labels and translations read-only and judge the code against the task; mock data (`TODO-APEX`) is 50%, "UI built, data not connected". The run and test scripts allow `sf project retrieve start`. `scripts/weekly/README.md` and `plan/OPERATING-GUIDE.md` section 5 updated. Issue #15 stays open: its definition of done replaces these rules when it is closed.
- D-030 amended (Lior): a `Draft` object file is scored by the legacy fields it has gone through (mapped by Old API Name or listed under Dropped legacy fields) out of the legacy entity's fields, up to 60 (was a flat 40); `In review` 70 and `Approved` 100 unchanged. Every pushed branch counts, with or without a PR. New `scripts/weekly/research-progress.py` does the count before the check starts (`_research.md` in the day's snapshot folder). Implementers push their research branch at the end of every work session, even as Draft (`CLAUDE.md`, `tickets/Data Migration/00-CONVENTIONS.md`). Weekly command step 3a, `scripts/weekly/README.md` and `plan/OPERATING-GUIDE.md` sections 4 and 5 updated.
## 15.10.2026 (DD corrections and model register, after Lior's review of Doc 3 V22)
- `knowledge/doc1-corrections.md` renamed `knowledge/dd-corrections.md`. New C3 (Lior, 06.10 and 08.10): Doc 2 10.2 maps the participant group to Campaign and its members to Campaign Member "in the events world only"; events use Registration__c instead, Campaign stays in the model. References in `knowledge/README.md` and the model register updated.
- `knowledge/model-register.md`: rows 22 and 23 (events do not use Campaign; row 23 now Lior 08.10), 24 (coupon: percentage or full exemption, no cap), 26 (billing amounts derived, master-detail to the Opportunity; kept as in Doc 1, Lior 14.10), 38 and 45 (seminar transport on Event Shuttle and Pickup Point, tender price lists out of scope), 42 (headphones order open, open item 81).

## 15.10.2026 (model register)
- `knowledge/model-register.md` rows 31 and 11 (Lior, 15.10): an event is linked to a Product, from which the registration price is derived, or to a Bundle when sold as a package. Replaces the 12.10 course-only exception.
- New `knowledge/doc1-corrections.md` (Lior, 15.10): rulings that override Doc 1 4.3 for implementers. C1 conference levels as GoMeddo Reservations, not parent registrations (11.10); C2 event linked to a Product, or to a Bundle when sold as a package (15.10). `knowledge/README.md` lists it; register rows 31 and 35 point to it.

## 14.10.2026 (repo, research track and profiles)
- D-030 amended (Lior, #13): research and build follow `tickets/Data Migration/` (Stage A and B), not `mapping/`, which stays as reference. Research percent by the object file's Status (`Draft` 40, `In review` 70, `Approved` 100). D-028: `tickets/Data Migration/` excepted from the freeze. Weekly check (`.claude/commands/weekly-progress.md`, steps 3a and 3b), `scripts/weekly/README.md`, `plan/OPERATING-GUIDE.md` section 4, `CLAUDE.md`, `WORKFLOW.md` updated.
- New D-033 (Anna's decision in #16, Lior's answer): FLS on a new `YV Order user` profile, created from scratch with the Salesforce user licence, and on System Administrator; profiles retrieved before every deploy. D-029: profile and permission set questions go to Niv. `CONVENTIONS.md` and `tickets/Data Migration/00-CONVENTIONS.md` updated.
- `tickets/Data Migration/00-CONVENTIONS.md`: Anna's answers in #13: `Legacy_ID__c` External ID on every object; list views not in the migration; shared metadata retrieved before every deploy; Copilot for pull and push only.
- `meetings/2026-10-13-sprint-1-handoff.md`: items 6 to 9 and section 11 updated.

## 13.10.2026 (repo, Sprint 1 handoff, Lior's review)
- `meetings/2026-10-13-sprint-1-handoff.md` version 2: Lior's edits and his response to each proposal; items for Anna, Niv and Tal tagged and opened as GitHub Issues.
- `tickets/Data Migration/00-CONVENTIONS.md`: start an object from its goal and role in the business process; dropped legacy fields logged in the object file; another implementer's object file changed through a PR tagged to them; answers that affect other objects also written to `knowledge/model-register.md` (Lior).

## 13.10.2026 (repo, Sprint 1 handoff)
- New `meetings/2026-10-13-sprint-1-handoff.md`: summary of the team's Sprint 1 handoff meeting, Lior's decisions after it, and open issues and proposals not yet decided.
- D-029 amended (Lior): technical questions go to Lior, general questions to Anna, GoMeddo questions to Niv; DD, scope and Yad Vashem stay with Lior. Every question is tagged `[Lior]`, `[Anna]` or `[Niv]`; points the implementer decides alone are recorded as `[Implementer]`. Partial approval: an object's fields without an open question are approved and built, fields with an open question are marked `Not built: open question` and built after the answer. `plan/OPERATING-GUIDE.md`, `WORKFLOW.md` and `tickets/Data Migration/00-CONVENTIONS.md` (Questions column, Stage A gate, Stage B start, handoff) updated.
- Not changed: `plan/TEAM.md` and `plan/EPICS.md` still call Niv the architect; the weekly check still expects Lior as requested reviewer on `review` and `question` PRs, which D-029 keeps.

## 13.10.2026 (repo, two data-model tasks)
- D-030 amended: data-model work is two tasks per entity, research and build with page layouts (the plan V9 workbook was edited in place and committed). Hours: 3h per object plus 0.25h per field plus the layouts as planned. The weekly check classifies and measures the build-and-layouts task on fields and layouts together (average, 100 only when both are). `plan/OPERATING-GUIDE.md`, `plan/SCHEDULE.md`, `plan/EPICS.md`, `WORKFLOW.md`, `scripts/weekly/README.md` and `decisions/README.md` updated. The Markdown copies under `plan/` are not regenerated yet.

## 13.10.2026 (repo, sprint Process)
- D-028 and D-029 amended: YV20 is a Jira business space with no sprints, so a sprint is a Process item `Sprint N (DD/MM-DD/MM)` (YV20-108 for 13.10 to 22.10) and its tasks are its children (Parent). The weekly check (`.claude/commands/weekly-progress.md`) finds the current Process by the dates in its name and reads and updates only its children created from 12.10.2026; carry-over is a change of Parent; the Jira `Sprint` field is not read. `plan/OPERATING-GUIDE.md` (V2) and `scripts/weekly/README.md` updated. Not yet tested against Jira.

## 13.10.2026 (repo)
- New `sources/legacy-code/`: the old system's code (Dynamics plugins, Tahel web API, PDF and Word generator, kiosk), 188 source files, reference only (the DD wins). Settings and publish files left out; a domain password, a Google API key and private contacts replaced with placeholders (D-031). `sources/SOURCES.md` and `CLAUDE.md` point to it.
- D-032: the approved plan workbook is committed to `plan/workbooks/` (V9 of 12.10.2026), with a read-only Markdown copy generated by the new `scripts/plan_to_md.py`: `plan/implementation-plan.md` and `plan/sprints/2026-10-13.md`. The weekly check reads the planned tasks from `plan/sprints/` and measures them while YV20 has no open sprint. D-028, `WORKFLOW.md`, `CLAUDE.md`, `plan/OPERATING-GUIDE.md` (Thursday step, prompt 6), `plan/SCHEDULE.md`, `plan/EPICS.md`, `scripts/README.md` updated.
- D-031 reverses D-014: documents (DDs, Yad Vashem workbooks, our own Word and Excel files) may be committed unless they contain personal information; work names of project staff are fine. The binding DD is the git copy in `sources/dd/`, byte-identical to the SharePoint originals (hashes in `sources/SOURCES.md`). Approved Doc 1 and Doc 2 of 06.10.2026 committed.
- The YV fields workbook (`sources/מיפוי שדות ורשימות 2 1.xlsx`) holds exported records with personal data (service providers, institutions): removed from the tree and blocked in `.gitignore`. Still in the git history.
- Updated `.gitignore`, `CLAUDE.md` (start of session, hard rules, data migration sources), `WORKFLOW.md`, `REVIEWER.md`, `README.md`, `sources/SOURCES.md`, `scripts/README.md`, D-014, D-030, `plan/OPERATING-GUIDE.md`.
- knowledge/model-register.md (draft 5): row 43, the credit instruction, per Lior's answers of 13.10: orders-center approval also for institutional credits, no link to the paid invoice, A23-13 on a paid order opens a credit instruction; the bank-transfer case stays open.

## 12.10.2026 (repo, plan V9)
- knowledge/model-register.md (draft 4): hotels and shuttles per Lior's choice A, new rows 44 to 46 (Event Hotel Allocation, Event Shuttle, Pickup Point) and row 38 updated; row 8 gets the includes-travel field on contract days; row 43 now covers the institutional credit instruction (5.11), with two points open for Lior.
- knowledge/model-register.md: Lior's 12.10 decisions: a course event is not linked to a Bundle (linked to a Product; doc 1 4.3 needs a correction note); a Bundle carries its own cancellation rules that override its products; new row 43, the private-customer credit instruction is a Receipt of type credit.
- knowledge/model-register.md: rows 40 and 41 updated per Lior's 12.10 decisions: the kiosk donation is a Gift Transaction (Nonprofit Cloud); the monthly coordinator approval is a status on the month's guide payment records, not a separate entity.
- D-030 amended: mapping rows with Confidence `high` are self-merged by the implementer and built right away; `review` and `question` rows go in a separate PR with Lior as requested reviewer and are not built until he approves. Niv is not a default reviewer. Research is done when all rows are on `main` and none is `question`. Weekly check measures rows on `main` and flags mapping PRs merged without approval.
- The weekly check reads and updates only tasks of the open Jira sprint in YV20 (created from 12.10.2026), never the older YV20 tasks; with no open sprint it writes nothing to Jira (D-028, D-029). New `plan/OPERATING-GUIDE.md`: everything about the sprint cycle and the weekly check in one place, with prompts for Claude Cowork.
- D-028: Jira is the master for tasks; sprints are planned in the plan workbook and approved tasks are created in Jira by Anna. Reverses D-017. `tickets/` frozen.
- D-029: two-week sprint cycle (Thursday planning, Jira and a 30-minute team review on Thursday afternoon, kickoff on Sunday, weekly report every Thursday 07:30) and who to go to for what. Current sprint 13.10 to 22.10.2026 with the plan V9 assignments; next sprint 25.10 to 05.11.
- D-030: data-model work is three tasks per entity (research, build, page layouts). Research is the entity's section in `mapping/`, written by the implementer's Claude through a PR and approved by the task reviewer (Niv by default). `scripts/build_mapping_md.py` header text updated; A1 and B1 not regenerated.
- New `plan/TEAM.md`: name, role, GitHub handle, git author, Jira name per person. `plan/SCHEDULE.md` V4 and `plan/EPICS.md` V2: two-week sprints, three-task model, pointer to the plan workbook V9.
- `CLAUDE.md` and `WORKFLOW.md` updated for D-028 and D-030.
- New `.claude/commands/weekly-progress.md` and `scripts/weekly/`: Lior's weekly read-only check, every Thursday 07:30: Jira tasks against a fresh `yv-partial` pull, mapping research and git work, one section per person, posted as a `weekly-report` Issue. `~/yv-weekly/run.sh` is now a wrapper that pulls first; run the install once more after merge.
- Weekly check writes to Jira for the current sprint only: a comment with the evidence and forward-only status moves (D-028 updated). `scripts/weekly/pull-inventory.sh` pulls the sandbox before Claude starts; `test-weekly-progress.sh` runs the check without posting or writing to Jira.
- `.gitignore`: `.claude/settings.local.json`.

## 11.10.2026 (repo)
- New `knowledge/model-register.md`, draft 1 pending Lior's review: one row per data-model concept with its Salesforce object, main relations, source and status (approved DD, Lior decision with date, Doc 3 draft, source conflict, open). Built from approved Doc 1 and Doc 2, the Doc 3 draft and Lior's decisions of 06.10, 08.10 and 11.10.2026, then checked by an independent review pass.
- `CLAUDE.md`: check every statement about an object against the register (start of session, step 6); new entry under mistakes already made (picking a side when sources disagree on the model).
- `REVIEWER.md`: run 1 checks objects against the register.
- `knowledge/README.md`: register listed.

## 07.10.2026 (repo)
- Anna has the same rights as Lior in this repo: edits master files (`brief/`, `mapping/`, `sources/`, `scripts/`, `decisions/`, `CLAUDE.md`, `WORKFLOW.md`, `REVIEWER.md`, `CHANGELOG.md`), approves and merges pull requests (deploy gate, D-015), rules on challenges, adds decisions. Updated `CLAUDE.md`, `WORKFLOW.md`, `decisions/`, `scripts/`, `sources/ORGS.md`.

## Brief V6, 06.10.2026: DD approved
- Yad Vashem approved Doc 1 and Doc 2 on 06.10.2026. They replace Doc 1 of 31.08.2026 and Doc 2 V9 of 15.09.2026. The DD is frozen; later changes come as change requests. File names, sizes and hashes: `sources/SOURCES.md`.
- `dd/` regenerated from the approved files. New converter `scripts/dd_to_md.py` (the 24.09 converter was never committed). 211 V/A codes: new A09-14 and A09-15, A13-04 voided, none removed. Review threads: Doc 1 244 of 244 resolved, Doc 2 746 of 758; 1 thread awaits a Malam reply (`dd/_meta/open-comments.md`).
- Word comment numbers were renumbered in the approved files. An old `cN` does not point to the same thread.
- `sources/DD-CHANGES-2026-10-06.md`: what changed in the body text, impact by epic, and 14 repo statements to re-check. Biggest changes: permissions keep full visibility (no Role hierarchy or sharing rules, X4); migration scope is open again (X4); TAHEL blocks groups above 35; discounts at three levels with fixed types (A4, A2); regulation rewritten per department quota (A5); changes made in TAHEL send no automatic messages (X1, B3); personal-file entity and vacation form removed (B1, B2).
- Brief V6, `plan/SCHEDULE.md` risk 3, `knowledge/README.md` (A1 and B1 rules flagged for re-check against the approved Doc 2), `scripts/README.md`.

## 05.10.2026 (repo)
- `CLAUDE.md`: the project check uses the git remote (`YV2026.git`), not the folder name. A clone into `YV2026` is valid.

## Brief V5, 23.09.2026
- `plan/EPICS.md`: every epic explained twice, once for people and once for Claude, plus what the letters mean and how sprints work around an epic.
- Epic codes renamed so each letter is one owner's area: old C1 to **A3** (manual intake), old A3 to **A4** (quote), old C5 to **A5** (regulation), old C2/C3/C4 to **C1/C2/C3**. A1, A2, B1 to B5 and X1 to X5 unchanged.
- `plan/SCHEDULE.md` V3 uses the new codes.

## 23.09.2026 (repo)
- `plan/SCHEDULE.md` V2: 12 sprints of 1.5 weeks from 04.10, three owners (Anna, Niv, Or Rozner), what each sprint contains, meeting cadence, risks. A proposal for the owners to challenge.
- `backlog/doc3-dependent.md`: work taken out of the epics because it depends on Doc 3. No epic waits for Doc 3.

## 22.09.2026 (repo)
- `scripts/dd_to_text.py`: reads a binding DD into one text file per chapter, comments included. Output goes to `sources/local/` (not committed). Every Claude reads the DD this way.
- `knowledge/A1-binding-rules.md`, `knowledge/B1-binding-rules.md`: rules from Doc 2 V9 that are easy to miss, and where they conflict with the current mapping (A1: F1 to F6, B1: F1 to F5). These conflicts need Lior and Niv before A1-01 / B1-01 start.
- `CLAUDE.md`: DD reading step, and a list of mistakes already made on this project.

## Brief V4, 22.09.2026
- D-016: `yv-partial` (partial copy sandbox) is the master sandbox. `sources/ORGS.md`.
- D-017: tickets live in the repo; Jira is not updated.
- `CONVENTIONS.md`: project layout, names, required descriptions, what not to touch, org check before creating anything.
- `sfdx-project.json` and `metadata/L0`, `metadata/A1`, `metadata/B1`.
- `PILOT-MEASUREMENT.md`: what to record per ticket; sync file ticket table and time log extended; PR template records review time.

## Brief V3, 22.09.2026
- Decision register `decisions/` (D-001 to D-015). Brief sections now cite decision IDs.
- Challenges and questions move to GitHub Issues (templates in `.github/ISSUE_TEMPLATE/`). Sync files are status only.
- `REVIEWER.md`: spec for the shared reviewer (owner: Tal). `reviews/` for its daily reports.
- Pull request template with a decisions checklist.

## 22.09.2026 (repo)
- Added `WORKFLOW.md`: SharePoint for client docs, git for team output, PR as the deploy gate, how Claude sessions communicate through sync files.
- Added `metadata/` for generated metadata (branch and PR only).
- Sync files: new section "Needs from other epics".
- `sources/SOURCES.md`: binding folder is now on SharePoint; Doc 2 V9 placed in it locally, V4 moved to `גרסאות קודמות`.

## Brief V2, 22.09.2026
- Brief translated to English; team communication is English from now on.
- Doc 2 binding version set to V9 (15.09.2026, last edited 16.09). V4 (06.09) superseded.
- Field mapping moved out of the brief into `mapping/`, generated by `scripts/build_mapping_md.py`.
- Sync between owners moved to this repo (`sync/`).
- Corrected B1 counts: 13 standard Contact mappings, 31 custom Contact fields (the 16.09 ticket book wrongly said 45 merges).
- 23 display fields proposed to move from A1 to C4 are listed explicitly (pending Lior).

## Brief V1, 22.09.2026
- First version, Hebrew. Superseded by V2.
