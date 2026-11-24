# D-030: Data-model work is research, and build with page layouts, per entity

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor, with Anna Pertzovski |
| Date | 12.10.2026 |
| Challenges | none |

## Decision

### Track for the object migration (14.10.2026, Lior)
From 14.10.2026 the two tasks per entity follow Anna's object migration track in `tickets/Data Migration/` (`00-CONVENTIONS.md`, Stage A and Stage B), not the `mapping/` sections described further down. The `mapping/` files stay as reference.

| Task | Output | Done when | Percent done (weekly check) |
|---|---|---|---|
| **Research** (Stage A) | The object file `tickets/Data Migration/NN_Object.md`, through a Stage A pull request whose reviewers follow D-029 | The file's Status is `Approved`. Partial approval counts: fields marked `Not built: open question` do not hold research back (D-029) | By the file's Status: `Draft` 60 x the share of the legacy entity's fields the file has gone through (mapped in a row, or listed under Dropped legacy fields), or 40 when the object has no legacy source entity; `In review` 70; `Approved` 100. Every pushed branch counts, with or without a pull request (19.10.2026) |
| **Build and page layouts** (Stage B) | The fields, the record page `Order-<Object Name>- Record Page`, the Hebrew translation, FLS (D-033) and the object's tab in the migration workbook; metadata under `tickets/Data Migration/metadata/<Object>/`, through a Stage B pull request | Every row of the approved file with a custom API name, except rows marked `Not built`, exists in `yv-partial`, and the record page exists | Fields found in the sandbox, averaged with the page |

The two-PR rule for mapping rows below (`high` self-merged, `review` and `question` to Lior) does not apply to new work. Hours stay as below.

### Original track (12.10.2026, reference)
Data-model work in the plan workbook (V9 as edited on 13.10.2026, and later) is two tasks per entity or entity group:

| Task | What it is | Output |
|---|---|---|
| **Research** | The fields Yad Vashem sent, mapped to the new Salesforce architecture | The entity's section in a field-mapping markdown in `mapping/`, in the table format of `mapping/A1-field-mapping.md`: one `## <source entity> -> <target> (N fields)` section per source entity, one row per source field, Decision and Confidence filled |
| **Build and page layouts** | The object if needed, the fields, and the page layouts or Lightning pages, including the Hebrew label translation | Metadata in `metadata/<epic>/`, through a branch and pull request (D-015) |

The Jira summary of the second task names the entity and says what it builds, for example `מוצר: בניית השדות ופריסות עמוד`. Field counts and field types are not separate tasks, and build and page layouts are one task, not two (before 13.10.2026 they were two).

**Hours**: 3h per object plus 0.25h per field, plus the page layouts as planned in the sprint tab. Of the first part, 5 minutes per field (minimum 1h per entity) is research, and the rest is build.

**Build and page layouts is done** when every `carry over` row with a custom API name exists in `yv-partial` and the layout or Lightning page of the target object is merged and deployed, after an approved PR.

**Research is done** when all rows of the entity's section are on `main` and none is still `question`. The section header always carries the full N (all source fields of the entity), from the first PR on. Each `question` row opens a GitHub Issue with the `question` label.

**Who writes and who approves the mapping.** The implementer's Claude generates or edits the mapping sections of the implementer's own entities, in two pull requests:
1. **Rows with Confidence `high`**: the implementer's own PR, merged to `main` by the implementer without approval (self-merge). The implementer builds those fields in the sandbox right away, without waiting for the other rows.
2. **Rows with Confidence `review` or `question`**: a separate PR, with Lior (`liorpridorrrrrrr`) as requested reviewer. Lior approves; then it is merged. These fields are not built until Lior approves. A `question` row stays `question` on `main` until the Issue is answered and a later PR changes it.

Niv is not a default reviewer of mapping PRs. Lior may ask him on a specific PR.

The markdown is the research record: the weekly check measures it and PRs review it row by row. An Excel copy may also be committed if it holds no personal information (D-031), but it does not replace the markdown. `main` has no branch protection, so the rule is not enforced by GitHub; the weekly check flags a mapping PR merged without Lior's approval that contains `review` or `question` rows, and fields of such rows found in the sandbox before Lior approved them.

**Relation to D-004.** D-004 holds in part: Claude proposes the mapping, and the rows that need a decision (`review`, `question`) are approved before they are built. D-030 changes three things for the sections the implementers write: the implementer's Claude, not only Lior's or Anna's (through `scripts/build_mapping_md.py`), proposes them; the rows that need a decision are approved by Lior, not by the architect (Niv is not a default reviewer); and rows with Confidence `high` are mechanical (D-004: "the rest can be approved as a batch"), so the implementer merges and builds them without review.

## DD source / evidence
Lior's decision, 12.10.2026, with the plan workbook V9 (SharePoint, `06 תכנון וספרינטים`, `תוכנית יישום - מודול הזמנות - מסמכים 1+2 - V9 - 2026-10-12.xlsx`).

## Alternatives considered
Separate tasks for field counts and field types (plan V8 and earlier): too fine to track in Jira, and the mapping already records both.
Research kept in Excel: the weekly check cannot measure it, and rows cannot be reviewed in a PR.

## Affects
`mapping/`, `scripts/build_mapping_md.py` (header text), `plan/SCHEDULE.md`, `plan/EPICS.md`, `.claude/commands/weekly-progress.md` (research, build and layout measures), CLAUDE.md, WORKFLOW.md.

## History
- 12.10.2026: accepted.
- 13.10.2026: the note on Excel updated for D-031 (Excel may be committed without personal information; the markdown stays the research record) (Lior).
- 13.10.2026: build and page layouts merged into one task per entity (plan V9 edited in place), so two tasks per entity instead of three. The weekly check measures that task on fields and layouts together (Anna).
- 12.10.2026: `high` rows are self-merged and built right away by the implementer; `review` and `question` rows go in a separate PR with Lior as requested reviewer and are not built until he approves; Niv is not a default reviewer; research is done when all rows are on `main` and none is `question` (Lior).
- 14.10.2026: the research and build tasks follow `tickets/Data Migration/` (Stage A and B), not `mapping/`; research percent by the object file's Status (Lior, Issue #13; item 7 of `meetings/2026-10-13-sprint-1-handoff.md`).
- 19.10.2026: a `Draft` is scored by how many legacy fields it has gone through, up to 60 (mapping the fields is about 60 to 70% of the research; a flat 40 hid how far a draft was). Every pushed branch counts, not only `main` and open pull requests, and implementers push their research branch at the end of every work session, even as Draft (Lior).
- 22.10.2026: no approval gate on the data-model track: the implementer approves the research, builds in `yv-partial` and self-merges (D-035). `review` and `question` rows no longer wait for Lior.
