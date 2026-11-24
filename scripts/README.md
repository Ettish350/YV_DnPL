# Scripts

| Script | What it does | Output |
|---|---|---|
| `dd_to_text.py` | Reads a binding DD into one plain-text file per chapter, comments inline | `sources/local/` (not committed) |
| `dd_to_md.py` | Regenerates the Markdown edition `dd/` (chapter files, images, toc, open comments, INDEX tables) from Doc 1 and Doc 2. Lior or Anna only | `dd/` |
| `mapping.py` | Mapping rules (entity targets, merges, type conversion, `propose()`). Also builds the Hebrew architect-review workbook of 16.09 | `mapping.xlsx` (not committed) |
| `plan_to_md.py` | Read-only Markdown copy of an approved plan workbook in `plan/workbooks/` (D-032). Needs `openpyxl` | `plan/implementation-plan.md`, `plan/sprints/<start>.md` |
| `build_mapping_md.py` | Builds the English mapping files in `mapping/` from the rules in `mapping.py` | `mapping/*.md` |
| `weekly/` | The weekly progress check: sandbox inventory (`pull-inventory.sh`), field count of the research object files on every pushed branch (`research-progress.py`, reads the committed `sources/שדות מערכת ישנה.xlsx`), run, test and install. See `weekly/README.md` | `sources/local/org-snapshot-<date>/` (not committed) |
| `daily/` | Daily status sheet and email, Sunday to Thursday 17:00: the weekly check in test mode (no Jira writes, no Issue, no email) with the same scoring script as the weekly (`weekly/score-tasks.py`), then `build-daily-status.py` turns its task list into one workbook with each person's tasks, % done, hours done and hours left, and `build-daily-email.py` emails Lior a visual summary (progress bars, per person, what moved since yesterday). Install with `bash scripts/daily/install-daily-status.sh`. Needs `openpyxl` | `~/yv-weekly/daily/Daily status <date>.xlsx` and `Daily status - latest.xlsx` |
| `epics.py` | Builds the Hebrew epic map workbook of 16.09 (epics, dependencies, build order) | `epics.xlsx` (not committed) |
| `pilot_tickets.py` | Builds the Hebrew A1/B1 ticket workbook of 16.09. Superseded by `brief/` section 8.4 and `tickets/` | `pilot.xlsx` (not committed) |

All scripts need the YV fields workbook locally. It holds personal data, so it is not in git (D-031). Get it from SharePoint (`sources/SOURCES.md`), then:

```
pip install -r requirements.txt
YV_FIELDS_XLSX="/path/to/מיפוי שדות ורשימות 2 1.xlsx" python3 scripts/build_mapping_md.py
```

Only Lior's or Anna's Claude regenerates `mapping/`. The xlsx builders still write to a hard-coded scratch path; change `out =` at the bottom before running them.

Note: `epics.py` and `pilot_tickets.py` are the Hebrew generators of 16.09 and still use the **old epic codes** (C1 for manual intake, C4 for online, C5 for regulation). The current codes are in `plan/EPICS.md`.
