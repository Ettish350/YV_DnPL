# D-028: Jira is the master for tasks; the sprint Excel is for planning

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor, with Anna Pertzovski |
| Date | 12.10.2026 |
| Reverses | D-017 |
| Challenges | none |

## Decision
- Each sprint is planned in the implementation-plan workbook (SharePoint, `06 תכנון וספרינטים`), on a sprint tab named after its dates. One workbook version per sprint. Each approved version is committed to `plan/workbooks/`, with a Markdown copy in `plan/sprints/` (D-032).
- A task approved in planning is created in Jira (project YV20) by Anna, assigned, created under the sprint's Process item in YV20 (Parent = the Process), given the sprint-tab hours as original estimate, and its Jira key is written back in the sprint tab. YV20 is a Jira business space and has no Jira sprints, so a sprint is a work item of type Process named `Sprint N (DD/MM-DD/MM)`, for example YV20-108 `Sprint 1 (13/10-22/10)`. The weekly check reads and updates only the tasks whose Parent is the current sprint Process, never the older YV20 tasks; with no current sprint Process it writes nothing to Jira. A task not finished by the end of the sprint is moved to the next sprint Process by changing its Parent (Bulk change for several); key, comments and history stay. The Jira field `Sprint` in the space is not used and not read.
- Implementers work from Jira: status, comments and hours live there. The hours estimate from the sprint tab goes into the Jira original estimate.
- Progress is measured every Thursday at 07:30 by Lior's Claude Code, before the planning meeting: Jira tasks against a fresh read-only metadata pull from `yv-partial`, the mapping research and the git work (`.claude/commands/weekly-progress.md`, D-029).
- The weekly check comments on each task of the current sprint Process with its evidence, and moves a status forward (to In Progress, or to Ready for testing sandbox or Done at 100%) when the evidence is clear. It never moves a status backwards and never touches other tasks. Implementers and Anna still own the status.
- `tickets/` in this repo is frozen as history. No new tickets there. Exception: `tickets/Data Migration/` is the active data-model track (D-030, from 14.10.2026).
- Unchanged: decisions in `decisions/`, challenges and questions as GitHub Issues, metadata through branch and pull request (D-015), the DD in `dd/`.

## DD source / evidence
Lior's decision, 12.10.2026, after the planning meeting with Anna the same day.

## Alternatives considered
Repo as ticket master (D-017): implementers did not use it, and Anna manages the team's tasks in Jira.

## Affects
CLAUDE.md, WORKFLOW.md, brief 8.2, tickets/, sync files.

## History
- 12.10.2026: accepted. Reverses D-017.
- 12.10.2026: the weekly check comments on current sprint tasks and moves their status forward from the evidence (Lior).
- 12.10.2026: sprint tasks carry the label `sprint-<start date>`; the weekly check selects them by it (Lior).
- 12.10.2026: the label rule is replaced by the open Jira sprint. The check reads and updates only open-sprint tasks created from 12.10.2026; no open sprint means no Jira writes (Lior).
- 13.10.2026: approved workbook versions and their Markdown copy are committed to git (D-032) (Lior).
- 13.10.2026: the open Jira sprint is replaced by a sprint Process item, because YV20 is a business space with no sprints (Space settings has no Features or Sprints option; converting the space was not tried). Sprint tasks are the children of the current `Sprint N (DD/MM-DD/MM)` Process; the check reads and updates only those, created from 12.10.2026. The older Process items (YV20-56, 69, 70, 88) have no dates in their names and are ignored. Carry-over: change the Parent (Anna).
- 14.10.2026: `tickets/Data Migration/` excepted from the freeze; it is the data-model track (Lior, D-030).
