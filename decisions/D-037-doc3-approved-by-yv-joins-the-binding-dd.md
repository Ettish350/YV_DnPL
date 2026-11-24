# D-037: Doc 3, approved by Yad Vashem, joins the binding DD

| | |
|---|---|
| Status | **accepted** |
| Decided by | Lior Pridor |
| Date | 08.11.2026 |
| Challenges | none |

## Decision
Doc 3 (Events and Finance), in the version Yad Vashem approved as final (file of 04.11.2026), is binding alongside Doc 1 and Doc 2:
- The docx is in `sources/dd/`, with its size and SHA-256 in `sources/SOURCES.md`. Three screenshots in appendix 13 show real customer records and are replaced with a placeholder in the git copy (D-031), so its hash differs from the original by design. The body is byte-identical to the original.
- The Markdown reading copy is `dd/doc3-events-finance/`, generated with `scripts/dd_to_md.py`, which now takes Doc 3 as an optional third document.
- `sources/drafts/doc3/` is removed. Its README is kept as `sources/DD-CHANGES-DOC3-V35.md`; the changes from V35 to the approved file are in `sources/DD-CHANGES-DOC3-2026-11-04.md`.

## Why
Lior, 08.11.2026: Yad Vashem approved Doc 3 as final.

## What this does not change
- Later changes, if any, come in as a new docx version through the D-031 update path (new file in `sources/dd/`, `SOURCES.md` row, `dd/` regenerated in one pull request).
- Where Doc 3 and Doc 1 or Doc 2 disagree, `knowledge/dd-corrections.md` rules; a point it does not cover is logged or raised as a question Issue, not decided.
- Tasks still come from Jira (D-028); Doc 3 being binding does not open Doc 3 work that no task asks for.

## Affects
`sources/dd/`, `sources/SOURCES.md`, `sources/DD-CHANGES-DOC3-V35.md`, `sources/DD-CHANGES-DOC3-2026-11-04.md`, `dd/` (new `doc3-events-finance/`, `assets/d3/`, `_meta/`, `INDEX.md`), `scripts/dd_to_md.py`, `CLAUDE.md`, `.claude/commands/fields-for-task.md`, `.claude/commands/automation-spec.md`, `tickets/Data Migration/automations/README.md`. Not yet aligned (for Lior): `plan/SCHEDULE.md` ("nothing waits for Doc 3"), `backlog/doc3-dependent.md`, the "Doc 3 draft" status values in `knowledge/model-register.md`, and D-008 (the deferred `new_event_reservations` relationship).

## History
- 08.11.2026: accepted. First drafted the same day as "V35 binds before Yad Vashem's formal approval"; replaced within hours by the approved file.
