# D-032: The approved plan workbook and its Markdown copy are in git

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 13.10.2026 |
| Challenges | none |

## Decision
- The plan workbook (`תוכנית יישום - מודול הזמנות - מסמכים 1+2 - V<n> - <date>.xlsx`) is still edited on SharePoint, `06 תכנון וספרינטים`, by Lior and Anna (D-028).
- **Each approved version is committed** to `plan/workbooks/`, replacing the previous version (git history keeps the old ones). It holds team names only, no personal information (D-031).
- In the same PR, `scripts/plan_to_md.py` generates a read-only Markdown copy:
  - `plan/implementation-plan.md`: the first tab, the whole plan.
  - `plan/sprints/<YYYY-MM-DD>.md`: one file per sprint tab, named by the sprint start date.
- **The latest committed version is the approved plan.** The team, Claude Cowork and the weekly check read the planned tasks from `plan/sprints/`. Nobody edits the Markdown by hand; a change goes into the workbook, then the PR.
- When the plan is approved on Thursday, Lior's or Anna's Claude commits the new version and the Markdown copy, before Anna opens the tasks in Jira.

## DD source / evidence
Lior's decision, 13.10.2026: the sprint plan was not in git, so the weekly check and Cowork could not see the planned tasks before they were opened in Jira.

## Alternatives considered
Workbook only on SharePoint (D-028 as written on 12.10.2026): planned tasks invisible to the repo and to the weekly check while YV20 has no open sprint.
Markdown only, without the workbook: loses the formulas and the epic summaries the workbook computes.

## Affects
`plan/workbooks/`, `plan/implementation-plan.md`, `plan/sprints/`, `scripts/plan_to_md.py`, D-028, WORKFLOW.md, `plan/OPERATING-GUIDE.md`, `plan/SCHEDULE.md`, `.claude/commands/weekly-progress.md`.

## History
- 13.10.2026: accepted. First version committed: V9 of 12.10.2026.
