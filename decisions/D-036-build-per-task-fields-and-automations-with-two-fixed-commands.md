# D-036: Build per task: the fields and automations one process step needs, with two fixed commands

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 03.11.2026 |
| Challenges | none |

## Decision
From 03.11.2026 each implementer owns one business process per sprint and builds it end to end in `yv-partial`, task by task. Objects are no longer researched or built whole up front.

- **Fields, per task.** The implementer runs `/fields-for-task <task> <Object> <step>`. Claude Code proposes only the fields that step needs, each marked `Exists` or `To build`. The implementer approves, adds or removes, builds the missing fields in `yv-partial` with a test record, and the rows go into the object file in `tickets/Data Migration/` with two new columns: **Task** and **Status** (`To build`, `Built`, `Not built: open question`). Legacy fields no task has needed are not listed; nobody goes through every legacy field of an object any more.
- **Automations, per DD code.** The implementer runs `/automation-spec <code> <task>`. Claude Code proposes a definition from the DD (trigger, steps, reads and writes, scope, tool, 3 to 6 test cases). The implementer approves or changes it, builds it in `yv-partial`, and tests it. The record is one file per code in `tickets/Data Migration/automations/<code>.md`, linked from every object file it touches.
- **The implementer approves their own fields and automations** and deploys them to `yv-partial` without Lior's approval (extends D-035 from fields and pages to automations built this way). Only `yv-partial`; never production, never `sf data`.
- **Testing:** the implementer tests their own work. A test counts when it is written in the automation file (record, steps, expected, actual result and date).
- **Progress:** a field row counts when it is `Built` and exists in `yv-partial`; an automation counts when it is `Tested`, its components are active in `yv-partial`, and the test records show the expected results. A morning check confirms this read-only and lists anything new in `yv-partial` that no file names. A process is done when every task passes and Lior accepts the demo at the end of the sprint.

## Why
Lior, 03.11.2026: the team is down to two implementers (Tal, Neev), with Reut joining. Building whole objects first produced fields nobody had placed in a process, and gaps showed up only when a process was built. Tal and Neev asked to work process first. Fixed commands keep the sources and the output format the same for every implementer.

## Affects
`tickets/Data Migration/00-CONVENTIONS.md`, `tickets/Data Migration/automations/README.md`, `.claude/commands/fields-for-task.md`, `.claude/commands/automation-spec.md`, `plan/IMPLEMENTER-PROCESS.md`, `WORKFLOW.md`, `CLAUDE.md`, `.claude/agents/data-migration-workbook.md`. Replaces the Stage A rule of D-030 and D-035 that research goes through every legacy field, and the D-030 scoring of a draft by legacy fields gone through. The daily email and the weekly check score by the old rules and are paused until they are rebuilt for this decision.

## History
- 03.11.2026: accepted.
