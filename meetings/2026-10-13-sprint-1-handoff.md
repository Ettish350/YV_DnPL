# Sprint 1 handoff meeting, 13.10.2026

Team meeting, about 51 minutes (Teams, recorded). Participants: Lior Pridor, Anna Pertzovski, Niv Doron, Or Rozner, Tal Arviv. Summary written by Lior's Claude from the transcript; version 2, with Lior's review of 13.10.2026. Decisions taken after the meeting are in section 9 and in D-029. Proposals and who they are for are in section 10.

## 1. Purpose
First working session with the implementation team on how Sprint 1 runs. Anna presented the object mapping workflow she built with Claude Code, the rules for working in the shared repo, the Jira setup and each person's Sprint 1 scope. The workflow covers objects and fields only, for the coming sprints until all objects are mapped. Automations, validations, Apex and reports will get their own working method later.

## 2. Object mapping workflow
Each object goes through five steps. The agent is the migration workflow agent, called with @ in Claude Code. The full rules are in `tickets/Data Migration/00-CONVENTIONS.md`.

| Step | What happens | Who |
|---|---|---|
| 1. Generate the object MD | The agent reads the old system fields workbook in `sources/` (no client data; the entity list tab and the fields tab are the ones that matter) and writes one MD per object. Fields it cannot resolve get a question in the Questions column | Implementer, via the agent |
| 2. Field by field review | The main work of the sprint. Start by reviewing the object's general goal and its role in the business process; each field is then understood in relation to that process. Each field is checked against the old system and the DD: field type, which object it belongs on (for example bundle or product), whether it is needed at all, whether an existing field covers it, and whether the DD needs fields the old system did not have. Standard Salesforce calls are made by the implementer. Legacy fields with no future use (for example old website section display flags) are dropped, not migrated, and kept in a separate log so they can be added later if needed. The old system code is in `sources/legacy-code/`; Claude can explain what an old field did | Implementer |
| 3. Questions out | The MD stays in Draft. Claude opens a pull request with the object's questions. Lior and Anna receive it by email | Implementer |
| 4. Approval and migration template | Once answered, the MD moves to Approved. The agent adds the object to the data migration workbook (one tab per object, the MD columns without section and questions) that Yad Vashem will fill for the data load | Implementer, via the agent |
| 5. Deploy to the sandbox | The agent creates the object and fields, the Lightning page by naming convention, the sections and the Hebrew translation. The implementer checks the result in the Orders app | Implementer, via the agent |

Why questions go through git: answers given in one to one calls are not visible to the others, while an answer in git reaches everyone's Claude sessions (one bundle decision already reached Anna only by chance), and it saves Lior's time compared to a call per question. Lior framed this as the part of the pilot being tested. Even if pull requests prove wrong, answers must still be recorded in git.

## 3. Rules for the shared repo
- Sync daily: pull before starting, push after every significant change.
- Objects are not owned. Whoever adds a lookup to an existing object, built by anyone, also updates that object's MD.
- Anna will add the workflow explanation to the conventions document. Claude Code then stops an implementer from moving on without the required updates. When unsure, ask Claude Code "I finished X, what next?".
- Labels are in English with a Hebrew translation (already in the shared Claude instructions). New lessons from anyone go into the shared instructions the same way.
- In VS Code, Claude Code and Copilot chat both work inside the YV2026 folder. Anna uses Claude Code for object work (a separate session per object) and Copilot for git operations such as the morning pull. GitHub accounts should use the Malam email, which also gives Copilot. Install the Salesforce skills in Claude Code as part of setup.

## 4. Jira and progress tracking
Sprint 1 tasks sit under the Process item YV20-108 `Sprint 1 (13/10-22/10)` (D-028). Each object has two tasks: research, and build with page layouts (D-030). Each task carries the effort estimate, percent done (updated weekly by Claude), start and due dates from the sprint tab, actual work days and Solution / Components. Implementers must fill actual work days, so plan can be compared to actual and Claude's contribution measured. Solution / Components is light for fields and important once flows start. The weekly check runs Thursday morning and updates percent done and hours before the next sprint is planned (D-029). Anna creates the assigned tasks by tomorrow morning.

## 5. Sprint 1 scope (about a week and a half)
| Owner | Objects | Notes |
|---|---|---|
| Anna | Product, Bundle, Bundle Item, Activity Day Subject | Research and build for each |
| Niv | Order and Activity Day first, then resource scheduling and hours regulation | Order is where the old system is most disordered: Yad Vashem put on it every field they associated with it, and many belong on other objects. Process work already started (partly GoMeddo) is paused; all objects first, then processes |
| Or | Institution, institution characteristics, department (Account) | Starts Sunday. Expected to take longer than estimated: first understand what institution and department mean for orders in the DD. Mostly fields or record types on objects already in production; where Or can decide, she decides |
| Tal | Portal (Experience Cloud) | Section 6 |

## 6. Portal track
Tal's tasks were drafted at headline level and most move to the next sprint. Tal has built the site base (Hebrew and English, login). The weekly check scored "site setup, templates and navigation" at 60%; Tal would put it near 80%. Agreed: Tal works first, then sits with Lior and Anna to reshape her tasks. She sends new tasks to Anna rather than opening them in Jira. Her next step is to connect to git and ask Claude which objects the portal needs. Lior wants a joint Lior, Tal and Claude way of planning portal tasks, and values Tal's input on how portal tasks should be built over finishing specific items. Tal should investigate, with Claude's help, why certain tasks are not considered done in the weekly check and what a done task looks like for her work. This will guide the portal tasks created for her from now on.

## 7. What comes next
After objects comes automation. The team is asked to propose how to work on automations, validations, Apex, LWC and reports, including how review becomes part of Jira tracking; Lior and Anna will turn this into tasks. Niv suggested Tal review Apex for poor patterns (example: fetching bundle items one by one for about 300 products would hit governor limits). Lior proposed one to one retrospectives at the end of the sprint; ideas should come in early, by email or pull request, and Anna will fold them into the shared Claude instructions. Role split: Lior on the technical side, Anna on task management.

## 8. Action items
| Owner | Item |
|---|---|
| Anna | Create the assigned Sprint 1 tasks in Jira |
| Anna | Add the workflow explanation to the conventions document |
| Anna | Setup session on 14.10 at the office with Niv and Tal (Or asked to join): check each setup and run a first object together |
| Lior | Fix the GitHub notification setting so review requests reach him by email |
| Lior | Resend Tal's repo invitation to her Malam email (she gets 404). Done |
| Tal | With Claude: why some portal tasks are not counted as done by the weekly check, and what done looks like for a portal task |
| Tal, Lior, Anna | Reshape the portal tasks after Tal's first round |
| All | GitHub with the Malam email; Salesforce skills installed in Claude Code |

## 9. Decisions after the meeting (Lior, 13.10.2026)
Recorded in D-029 and `tickets/Data Migration/00-CONVENTIONS.md`.
- **Channel**: questions about one object's fields go in that object's pull request; other questions in a GitHub Issue (D-029).
- **Who decides questions**: technical questions, Lior; general questions, Anna; GoMeddo questions, Niv. DD, scope and Yad Vashem stay with Lior.
- **Every question is tagged** with who decides: `[Lior]`, `[Anna]` or `[Niv]`. A point the implementer can decide alone is not asked; it is recorded as `[Implementer]` so Lior can scan these in batch.
- **Partial approval**: an object does not wait for all its answers. Fields and objects that are ready are created, so Lior is not the bottleneck; a field with an open question is marked "Not built: open question" and built once answered.
- **Start from the process**: research on an object starts with its goal and its role in the business process.
- **Dropped legacy fields are logged** in the object file, so they can be added later and Yad Vashem can be told where a field went.
- **Answers land in files**: at merge, each answer is written into the object file; an answer that affects other objects also goes into `knowledge/model-register.md`.
- **Another person's object file** is changed through a pull request tagged to its owner when it belongs to their open task. The lookup target must exist before the lookup is deployed.

## 10. Proposals from Lior's Claude, with Lior's response
GitHub does not notify for names written in a file; each tagged item is also opened as a GitHub Issue assigned to the person.

| # | Proposal | Lior's response | For |
|---|---|---|---|
| 1 | Questions through a pull request per object; who decides each kind of question | Decided (D-029) | All |
| 2 | Tag who decides; partial approval | Decided (D-029) | All |
| 3 | Decisions taken alone leave a trace (`[Implementer]`, dropped fields log) | Agreed (section 9) | All |
| 4 | Answers land in files, cross-object answers in the model register | Agreed (section 9) | All |
| 5 | Another person's object file changed through a pull request to its owner; lookup target first | Agreed (section 9) | All |
| 6 | The deploy step should also cover: field level security through a permission set (not only the System Administrator profile), list views where needed, and a legacy ID external ID field on every migrated object for upserts and parent and child links in the data load. Shared metadata in `yv-partial` (the Orders app, profiles, permission sets) is overwritten by whoever deploys last unless it is retrieved before each deploy or owned by one person | Anna, 14.10 (#13, #16): FLS on a new `YV Order user` profile and System Administrator (D-033); list views later, not in the migration; `Legacy_ID__c` External ID on every object; retrieve shared metadata before every deploy | @AnnaPertzovski |
| 7 | Percent done by stages for object tasks (file generated 20, review done 50, approved 70, migration tab 80, deployed and checked 100); acceptance criteria per portal task | Agreed. Applied 14.10: research percent by the object file's Status (D-030) | Lior |
| 8 | The repo rules are read by Claude Code, not by Copilot: Copilot for pull and push only, content changes (object files, metadata) only through Claude Code | Anna, 14.10 (#13): Copilot for pull and push only; content only through Claude Code | @AnnaPertzovski |
| 9 | One page daily routine (pull, one session per object, call the agent, review, pull request, push, log days in Jira); run steps 1 to 5 end to end on one small object in the 14.10 session, recorded | Agreed. The recorded run on 14.10 is on Activity Day Subject (#13) | @AnnaPertzovski |
| 10 | Niv's Sprint 1 limited to Order and Activity Day; Or's objects planned as carry-over | No change | |
| 11 | Apex review checklist (no queries or DML inside loops, bulk safe triggers, test coverage, no hardcoded IDs), applied by Tal and Claude before any Apex merge; written now, used in the automation phase. Bundle selection: query all bundle items in one call | Agreed | @Neev1993, @TalArvivMalam |
| 12 | Keep files with client data out of git, including the data migration workbook once Yad Vashem fills it | Fine. The YV fields workbook is already blocked in `.gitignore`; Yad Vashem's filled workbook must be saved under a different name than `Migration - Objects and fields.xlsx`, which is tracked | All |

## 11. Open issues
- **Two research tracks: settled 14.10.2026.** Anna's track (`tickets/Data Migration/`, Stage A and B) is binding (Lior, #13). D-028, D-030 and the weekly check updated.
- **Profiles: settled 14.10.2026.** `YV Order user` is created from scratch with the Salesforce user licence; profile and permission set questions go to Niv (Lior, #16; D-033).
- **Working method for automations, validations, Apex and code review**: next phase.
