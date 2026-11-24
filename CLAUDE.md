# CLAUDE.md: read this at the start of every session

This repo is the master for the Yad Vashem orders module build (Salesforce, Malam). It is shared by the epic owners and their Claude sessions.

## Start of session

1. `git pull`.
2. Read `WORKFLOW.md` and `brief/epic-owners-brief.md` in full. Note the brief version and read `CHANGELOG.md` entries since your last session.
3. Read `sources/SOURCES.md`. The binding DD is the copy committed in `sources/dd/` (D-031); check that its size and SHA-256 match the table there. If not, stop and tell the user.
4. Read all sync files in `sync/`: your own (look for new "Answers from Lior") and the others (look for "Needs from other epics" addressed to your epic).
5. Read `decisions/README.md`. List open Issues: `gh issue list --label challenge` and `gh issue list --label question`. Tell the user about any that concern their epic, and any new reviewer report in `reviews/`.
6. For field work, use `mapping/<epic>-field-mapping.md` and `mapping/entities.md`. Before writing any statement about an object (in a ticket, sync file, PR or comment), check it against `knowledge/model-register.md`. A concept that is missing there, or a source that says otherwise, is a question Issue, not a decision.
7. Read the DD through `scripts/dd_to_text.py`, never directly. Run it once per DD version:
   `python3 scripts/dd_to_text.py "sources/dd/<Doc 2 file name from sources/SOURCES.md>" sources/local/doc2`
   It writes one file per chapter with all Word comments inline (many binding decisions from the 14.9 meeting are in resolved comments). Read your epic's chapter in full, comments included. Then read `knowledge/<epic>-binding-rules.md`.
8. Before building anything in Salesforce, read `CONVENTIONS.md` (names, descriptions, what not to touch, the org check) and `sources/ORGS.md`.
9. Read `PILOT-MEASUREMENT.md`. Note the start time when work on a ticket begins and the end time when it stops, in the sync file, as you go.

**Tasks live in Jira (YV20)** (D-028). Anna creates them from the approved sprint tab; implementers work and update status there. `tickets/` is frozen history: do not add tickets to it, except `tickets/Data Migration/`, the data-model track (D-030). Who to ask and who decides: D-029. The approved sprint plan: `plan/sprints/<start date>.md` (D-032). How the sprint cycle, the Jira rules and the weekly check work, with prompts for Claude Cowork: `plan/OPERATING-GUIDE.md`.
**From 03.11.2026 work is per task (D-036).** Each implementer owns one business process per sprint. Fields: `/fields-for-task <task> <Object> <step>`, only what that step needs, recorded in the object file in `tickets/Data Migration/`. Automations: `/automation-spec <DD code> <task>`, one file per code in `tickets/Data Migration/automations/`. The full cycle, with examples: `plan/IMPLEMENTER-PROCESS.md`. Nobody goes through every legacy field of an object any more. Questions are tagged and routed by D-029. `mapping/` is reference only.

## While working: challenge, do not work around

Before building, check the work against `decisions/`. If the DD or a concrete case contradicts a decision (anyone's, including Lior's):
1. Stop that ticket. Do not silently build around the decision.
2. Draft a challenge Issue with the `challenge.yml` template: decision ID, what is wrong, evidence (DD quote with chapter and code), proposed change, blocked tickets.
3. Show the draft to the user. Post it only when they approve: `gh issue create --template challenge.yml` or `gh issue create --label challenge --title ... --body ...`.
4. Continue with unblocked tickets.

Same for questions and needs from another epic: an Issue with the `question.yml` template.

## Stay inside this project

This repo is for Yad Vashem only. Lior also works on other clients (for example Centenario) in other folders and other Salesforce orgs.
- Work only inside this folder. Never `cd` out of it, and never read, edit or run git in another folder.
- The folder name does not matter (`YV2026`, `yv-orders-master` or anything else). Identify the project by its git remote: `git remote get-url origin` must end in `YV2026.git`. If it does not, or the folder is not a git repo, stop and tell the user they started Claude Code in the wrong place.
- Salesforce commands must always name the Yad Vashem org explicitly: `--target-org yv-partial`. Never use another org alias, and never change the global default org.

## Mistakes already made on this project (do not repeat)

- **Counting from memory.** "2 stubs needed" was really 11; "45 fields merge into standard Contact" was really 13. Count with a script against the source file, then state the number.
- **Raising a question the DD already answers.** GoMeddo was flagged as open when the DD had settled it. Search the whole DD, including comments, before calling anything open.
- **Wrong file version.** Work was done twice on the wrong copy of a DD. Use only the copy in `sources/dd/`, and check its size and hash against `sources/SOURCES.md` every session.
- **Taking the old system as the spec.** `sources/legacy-code/` shows how Tahel behaves today. Use it to understand a rule, never as the requirement: the DD wins, and a difference that matters is a question Issue.
- **Mapping from the source instead of the DD.** The field mapping was built from the Dynamics workbook, so it misses fields the DD requires and can carry fields the DD removed (see `knowledge/`). Check both directions.
- **Reading the DD body without its comments.** Resolved comments hold decisions. Use `scripts/dd_to_text.py`.
- **Assigning a rule to the chapter it is written in.** A rule belongs to the epic where it is enforced (D-010).
- **Picking a side when sources disagree on the data model.** Conference schedule levels were modelled as parent Registration records because Doc 1 4.3 says so, against the seminar model and appendix A, and had to be reversed (08.10 to 11.10.2026). When sources disagree, quote both and ask. The ruling goes into `knowledge/model-register.md`.

## Hard rules

- Never deploy to a Salesforce org. Every Claude Code prompt ends with: "Do not deploy to the org. Generate the metadata and show it for approval." **Exception (D-035):** in the data-model track (`tickets/Data Migration/`), the implementer builds their approved object in `yv-partial` without approval: fields, record page, translation and FLS. Never production, never `sf data`.
- Never invent fields, objects or rules. If the DD is silent, write "question" in the sync file.
- The DD wins over this repo. Log any contradiction in your sync file; do not resolve it yourself.
- English for everything in this repo and for team communication. Deliverables for Yad Vashem are Hebrew RTL. Salesforce labels are Hebrew.
- No em-dashes.
- Verify before asserting that something is missing from the DD: search the whole document.
- Documents (DDs, Yad Vashem workbooks, our own Word and Excel files) may be committed, **unless they contain personal information**: data about real people as private individuals (IDs, private phones or emails, addresses, records of visitors, guides, customers or contacts) or any exported records. Work names and roles of project staff are fine. Check every file, every sheet, before committing; if in doubt, do not commit and ask Lior (D-031). The YV fields workbook holds exported records and stays on SharePoint.

## If the user is Lior or Anna

Lior is the master owner of this repo. Anna has the same rights as Lior: anything this repo lets Lior do, Anna may do too (edit master files, approve and merge pull requests, rule on challenges, add decisions). Do not block Anna on anything Lior could do. When the session's user is Lior or Anna, the "Lior and Anna" rows in the table below are theirs to edit directly: `brief/`, `mapping/`, `sources/`, `scripts/`, `decisions/`, `CLAUDE.md`, `WORKFLOW.md`, `REVIEWER.md`, `CHANGELOG.md`. Everything else in this file still applies to them, including the hard rules and the metadata gate (branch and pull request, never a direct push of metadata to `main`).

## Data Migration workbook

If your task is the Data Migration workbook (one object file per Salesforce object, then its Excel tab and proposed metadata), this section replaces the epic start-of-session steps that do not apply to you (epic sync file, epic binding rules).
- Everything lives in `tickets/Data Migration/`: `00-CONVENTIONS.md`, one `NN_Object.md` per object, the workbook `Migration - Objects and fields.xlsx` and proposed metadata under `metadata/<Object>/`. This is not the repo-level `metadata/` folder.
- Use the `data-migration-workbook` agent (`.claude/agents/data-migration-workbook.md`) for every object. It reads `Claude_Salesforce_Migration_Guidelines.md`, `00-CONVENTIONS.md`, this file and `CONVENTIONS.md` first. Use an existing object file (for example `01_Product2.md`) as the model.
- Sources are only the legacy-fields file `sources/שדות מערכת ישנה.xlsx` (the YV fields workbook with all records removed, committed in git; the original holds personal data and stays on SharePoint, D-031) and the DD in `dd/`. Do not use `mapping/<epic>-field-mapping.md`.
- Research reads the org only (`sf sobject describe --sobject <Object> --target-org yv-partial`). Build deploys the approved object to `yv-partial` only, without approval (D-035). Never production, never `sf data`.
- Open questions go in the Questions column of the object file. Anything that contradicts a decision in `decisions/` is a challenge Issue, as above. Check `decisions/README.md` first; D-018 and D-030 apply.
- Work on a branch and open a pull request; do not push to `main`. You set your own object file to `Approved` and merge your own pull requests; Lior is not a reviewer (D-035). Push the branch at the end of every work session, even as `Draft`: the checks cannot see work on your laptop. From 03.11.2026 the scope is one task, not a whole object (D-036): use `/fields-for-task` and `/automation-spec`. Status goes in your own `sync/DM-<name>.md` (copy `sync/TEMPLATE.md`).

## What you may change

| Path | Who changes it |
|---|---|
| `brief/`, `mapping/`, `sources/`, `scripts/`, `decisions/`, `CLAUDE.md`, `WORKFLOW.md`, `REVIEWER.md`, `CHANGELOG.md` | Lior and Anna |
| `tickets/Data Migration/` and `sync/DM-<name>.md` | the Data Migration implementer, through a branch and pull request |
| the `mapping/` sections of your own entities | that entity's implementer, only through a branch and pull request (D-030) |
| `reviews/` | the shared reviewer only |
| `metadata/<epic>/` | that epic's owner, only through a branch and pull request |
| `sync/<epic>-<name>.md` | that epic's owner |
| `tickets/<epic>/` | that epic's owner |

To change something you do not own, write the request in your sync file.

## End of session

Update your sync file (decisions, requests, ticket status), then commit and push:
`git add sync/ tickets/ && git commit -m "<epic>: <what changed>" && git push`

Generated metadata never goes to `main` directly and is never deployed: branch, push, pull request (see `WORKFLOW.md`). Exception: the data-model track (D-035).## DD sources
The binding DD (Doc 1 + Doc 2, and Doc 3, approved 04.11.2026, by D-037) is in `dd/` as Markdown. Read `dd/INDEX.md` before answering any DD question or writing a ticket. Do not edit files under `dd/`; they are regenerated from the binding docx in `sources/dd/` (D-031).
