# D-029: Two-week sprints and the communication flow

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor, with Anna Pertzovski |
| Date | 12.10.2026 |
| Challenges | none |

## Decision

### Sprint cycle
| When | Who | What |
|---|---|---|
| Before Thursday | Lior (with Claude) | Drafts the next sprint's tasks in a new sprint tab of the plan workbook |
| Thursday morning | Lior and Anna | Review the draft: build order, load, assignments. The sprint tab is final when this meeting ends |
| Thursday afternoon | Anna | Creates the sprint Process item for the new dates (`Sprint N (DD/MM-DD/MM)`), creates the approved tasks under it (Parent) with the sprint-tab hours as original estimate, assigns them, moves the unfinished tasks of the last sprint to it, writes the Jira keys back in the sprint tab (D-028) |
| Thursday afternoon, 30 min | Anna | Team review of the sprint: each implementer hears their tasks |
| Sunday | Implementers | Kickoff: the sprint starts |
| Every Thursday 07:30 | Lior's Claude Code | Weekly progress report: Jira against the sandbox, mapping research and git work (D-028, D-030). Runs before the planning meeting, so Lior and Anna plan from fresh numbers |

### Dates
| Sprint | Runs | Jira sprint Process | Planning and team review |
|---|---|---|---|
| Current | Tuesday 13.10 to Thursday 22.10.2026 (an exception: it starts on a Tuesday) | YV20-108 `Sprint 1 (13/10-22/10)`, created 13.10 | Held 12.10.2026 |
| Next | Sunday 25.10 to Thursday 05.11.2026 | created by Anna on 22.10 or 25.10 | Thursday 22.10.2026 |

Pricing and budget frameworks moved to the next sprint.

### Assignments, sprint 13.10 to 22.10.2026
From the plan workbook V9. Reviewers not yet assigned.

| Implementer | Work | Hours in sprint |
|---|---|---|
| Anna | Catalog: product, bundle and bundle item, activity day subject (research, build, layouts) | 68 |
| Niv | Bundle in the order (A01-02 flows, filters); scheduling: assignment, resource, hours regulation and closures; order and activity day (research, build, layouts) | 155.5 |
| Or | Institutions: institution and institution characteristics, department (research, build, layouts) | 33.5 |
| Tal | Portal skeleton (site, design, RTL, home page, portal permissions, parent and child institution) and first booking components | 160 |

Who is who, with GitHub and Jira names: `plan/TEAM.md`.

### Who to go to
| Topic | Goes to | Decides |
|---|---|---|
| Task status, blockers, reassignment, a wrong or missing task | Anna | Anna. She may fix tasks in Jira and in the sprint tab |
| General question: the method, the process, how to work in the repo, Jira or the agent | Anna | Anna |
| Technical question: build, data model, field placement, Salesforce configuration | Lior | Lior |
| GoMeddo: scheduling, resources, reservations and anything built on GoMeddo | Niv | Niv |
| Profiles and permission sets | Niv | Niv (D-033) |
| Anything that changes the DD or scope | Lior | Lior |
| Anything to ask Yad Vashem | Lior | Lior. Nobody else contacts Yad Vashem about the build |
| Open questions during implementation | Below | The person the question is tagged for |

### Where questions go and how they are tagged (13.10.2026)
- **Questions about one object's fields** go in that object's research pull request: the Stage A review gate in `tickets/Data Migration/00-CONVENTIONS.md`, or the mapping PR of D-030. Lior is always a requested reviewer; Anna and Niv are added when questions are tagged for them. **Other questions**: a GitHub Issue with the `question` label, addressed to the person who decides.
- **Every question is tagged** with who decides, from the table above: `[Lior]`, `[Anna]` or `[Niv]`. A point the implementer can decide alone (standard Salesforce practice, a legacy field with no future use) is not asked. It is decided and recorded with the tag `[Implementer]`, so Lior can scan these in batch.
- **Partial approval.** An object does not wait for all its answers. Fields with no open question, or whose questions are answered, are approved and built. A field whose question is still open is marked `Not built: open question` in the object file and left out of the build; when the answer comes, a later PR updates the file and the field is built.

### Escalation
Anna raises a flag to Lior when a task is blocked for more than a day, when a question touches scope, or when the cycle itself is not working.

## DD source / evidence
Lior and Anna, planning meeting of 12.10.2026.

## Affects
WORKFLOW.md, plan/SCHEDULE.md (sprint lengths), plan/TEAM.md, the plan workbook, scripts/weekly/.

## History
- 12.10.2026: accepted.
- 12.10.2026: kickoff fixed to Sunday, team review on Thursday, weekly report moved to Thursday 07:30 (Lior).
- 13.10.2026: "Jira sprint" read as the sprint Process item, per D-028 of the same date (Anna).
- 13.10.2026: who to go to changed after the Sprint 1 handoff meeting: technical questions to Lior, general questions to Anna, GoMeddo questions to Niv. Questions are tagged with who decides, and an object's fields are approved and built without waiting for its open questions (Lior). Meeting summary: `meetings/2026-10-13-sprint-1-handoff.md`.
- 14.10.2026: profiles and permission sets go to Niv (Lior, Issue #16).
- 22.10.2026: for the data-model track, Lior is no longer always a reviewer of research pull requests; implementers approve and merge their own (D-035).
