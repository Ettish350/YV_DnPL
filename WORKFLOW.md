# How we work together

Three places, three kinds of content. Nothing lives in two places.

| Place | What lives there | Who writes | How Claude reaches it |
|---|---|---|---|
| **SharePoint, binding folder** | Originals of Doc 1, Doc 2 and the YV fields workbook. The DDs are also committed in `sources/dd/`, and that git copy is binding (D-031). The YV fields workbook holds personal data and exists only here | Lior only | Read-only. Sync the folder only if you need the YV fields workbook (see `sources/SOURCES.md`) |
| **This git repo (YV2026)** | Everything the team produces (brief, mapping, sync files, generated metadata), the binding DDs in `sources/dd/`, and any document without personal information (D-031) | See ownership table below | `git pull` / `git push` |
| **Salesforce: `yv-partial`** (partial copy sandbox, the master sandbox) | Deployed metadata | Nobody, until Lior or Anna approves; data-model objects (`tickets/Data Migration/`): the implementer, no approval (D-035) | Deploy only after an approved pull request, except the data-model track (D-035). See `sources/ORGS.md` |
| **Jira (YV20)** | Tasks: one per approved line of the sprint tab, with status, assignee, comments and hours (D-028) | Anna creates and assigns; implementers update | Through the Atlassian connector, where the session has it |
| **Plan workbook** (edited on SharePoint, `06 תכנון וספרינטים`; each approved version committed to `plan/workbooks/` with a Markdown copy in `plan/sprints/`, D-032) | Sprint planning: one sprint tab per sprint, approved on Thursday (D-029) | Lior and Anna | Read the planned tasks in `plan/sprints/<start date>.md` |

## Ownership

| Path | Owner | Others |
|---|---|---|
| `brief/`, `mapping/`, `sources/`, `scripts/`, `decisions/`, `CLAUDE.md`, `WORKFLOW.md`, `REVIEWER.md`, `CHANGELOG.md` | Lior and Anna | read only, except the mapping sections of your own entities (next row) |
| `mapping/*.md`, the section of an entity you research | the implementer of that entity, through a pull request (D-030) | `high` rows: self-merged, built right away. `review` and `question` rows: separate PR, Lior approves before they are built |
| `reviews/` | shared reviewer | read only |
| `sync/<epic>-<name>.md`, `tickets/<epic>/` | the epic owner | read only |
| `metadata/<epic>/` | the epic owner, through a pull request | review |

Because nobody writes to anyone else's files, there are no merge conflicts.

## How the Claude sessions talk to each other

They never talk directly, and none of them runs between sessions except the shared reviewer. Each Claude acts only while its owner has a session open, and posts under its owner's GitHub account after the owner approves.

| What | Where | Why there |
|---|---|---|
| A decision and its reasoning | `decisions/D-xxx-*.md` | One file per decision, with DD source and status. Anyone can point to it |
| Disagreement with a decision | GitHub Issue, `challenge` label | Threaded, people get email, the reviewer and Lior or Anna answer in the same place |
| Question, or a need from another epic | In the object's research PR when it is about that object's fields, otherwise a GitHub Issue with the `question` label. Tagged `[Lior]` technical, `[Anna]` general, `[Niv]` GoMeddo (D-029) | Same |
| Status: tickets, hours, blocked | `sync/<epic>-<name>.md` | Quick to read at session start |
| Changes to the master | Pull request by Lior's or Anna's Claude, line in `CHANGELOG.md` | Traceable |

### A challenge, end to end
1. **Owner's session.** Their Claude finds that the DD contradicts a decision, stops that ticket and drafts a challenge Issue with evidence. The owner approves and it is posted.
2. **GitHub** emails Lior and Anna (and anyone watching).
3. **Reviewer's daily run.** Checks the evidence against the DD and comments: confirmed, not confirmed, or partly.
4. **Lior's or Anna's session.** Their Claude lists open challenges at start. Lior or Anna rules in the Issue. If upheld, their Claude updates the decision file, the mapping or brief if needed, and CHANGELOG, in a PR that closes the Issue.
5. **Everyone's next session.** Their Claude pulls, reads CHANGELOG and the decision register, and adjusts.

Challenges can target any decision, including Lior's and those drafted by Lior's Claude.

## Every session

**Start**
1. `git pull`
2. Claude reads `CLAUDE.md` (automatic in Claude Code), then the brief, `CHANGELOG.md` since your last session, all sync files, and checks the DD versions against `sources/SOURCES.md`.

**Work**
3. Tasks in Jira (YV20). `tickets/` is frozen history (D-028). Data-model work is two tasks per entity: research (the object file in `tickets/Data Migration/`, Stage A, through a PR), and build with page layouts (Stage B) (D-030, `tickets/Data Migration/00-CONVENTIONS.md`). Sprint cycle and the Thursday 07:30 weekly report: D-029. The full operating guide: `plan/OPERATING-GUIDE.md`.
4. Metadata that Claude Code generates goes to `metadata/<epic>/` (sfdx source format), on a branch, never deployed.

**End**
5. Update your sync file: decisions, requests, ticket status, actual hours.
6. Push:
   - sync and tickets go straight to `main`:
     `git add sync/ tickets/ && git commit -m "A1: tickets A1-01..A1-04" && git push`
   - metadata goes through a branch and a pull request:
     `git checkout -b A1/product-fields && git add metadata/A1 && git commit -m "A1-01: Product2 fields" && git push -u origin A1/product-fields`, then open a PR on GitHub.

## Shared reviewer

Runs on a schedule on one machine, independent of all owners. Comments on every pull request and posts a daily report to `reviews/`. It can open challenge Issues but never merges or deploys. Spec: `REVIEWER.md`.

## Deploy gate

A pull request into `main` for `metadata/` is the approval request. **Lior or Anna approving and merging the PR is the approval to deploy.** Nobody deploys from a branch, and nobody deploys something that is not merged.

**Exception, the data-model track (D-035, 22.10.2026):** objects, fields, record pages, translations and FLS under `tickets/Data Migration/` need no approval. The implementer approves their own research, deploys to `yv-partial` (never production) and self-merges the pull request so `main` matches the sandbox.

**Exception, per-task build (D-036, 03.11.2026):** fields built with `/fields-for-task` and automations built with `/automation-spec` (records in `tickets/Data Migration/` and `tickets/Data Migration/automations/`) need no approval either. The implementer approves the definition, deploys to `yv-partial` only, tests it, and self-merges the pull request. See `plan/IMPLEMENTER-PROCESS.md`.

## When the DD changes

Lior or Anna puts the new version in the SharePoint binding folder, moves the old one to `גרסאות קודמות`, and in one PR commits the new file to `sources/dd/` (removing the old one), updates `sources/SOURCES.md` (file name, size, hash) and regenerates `dd/` (D-031). Your Claude's version check at session start will fail until you refresh your local copy. That is intentional: nobody keeps working on an old DD without noticing.

## When something is wrong in the master

Do not fix it. Open a challenge Issue (if it is a decision) or a question Issue (anything else).
