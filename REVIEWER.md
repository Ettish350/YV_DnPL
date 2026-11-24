# Shared reviewer: specification

Owner: Tal. Status: to build.

## What it is
A Claude Code session that runs on a schedule on one machine, independent of every owner's session and of Lior's. It reviews, comments and opens Issues. **It never merges, never edits master files, never deploys.**

## Where it runs
One machine with all three:
1. A clone of this repo.
2. Nothing extra for the DDs: the binding copy is in git, `sources/dd/` (D-031).
3. Read-only access to the build org (sf CLI, read-only user).

Its own GitHub identity (bot account or a fine-grained token limited to Issues, PR comments and `reviews/`), so its comments are clearly the reviewer's.

## Run 1: on every pull request (or every 2 hours, listing open PRs)
For each open PR not yet reviewed at its latest commit:
- Every field and object in the diff exists in `mapping/`. Flag anything else.
- Every object named in the diff, the ticket or the sync file matches `knowledge/model-register.md`. Flag work that builds on a row marked source conflict or open.
- Nothing contradicts a decision with status accepted or proposed. If it does and no challenge Issue is linked, flag it.
- Every validation rule and automation has a DD code, and that code belongs to this epic (D-010).
- The PR checklist is honest (e.g. "Components" really updated).
Output: one PR comment starting with "Reviewer:", ending with PASS or CHANGES NEEDED.

## Run 2: daily
1. For every open challenge Issue without a reviewer comment: check the evidence against the binding DD. Comment "confirmed", "not confirmed" or "partly", with the quote.
2. Three-way check: DD requirement vs repo (tickets, "Components") vs org (actual metadata). List gaps in each direction.
3. Decision drift: tickets or PRs that work around a decision without challenging it. Open a challenge Issue itself when evidence is strong, marked "Raised by: reviewer".
4. DD version: binding files match `sources/SOURCES.md` (size, hash).
Output: `reviews/YYYY-MM-DD.md`, committed directly to main (the only path it writes).

## Rules
- Evidence or silence: every finding cites a DD quote, a file and line, or org metadata.
- Advisory only. Lior decides.
- English. No em-dashes.
