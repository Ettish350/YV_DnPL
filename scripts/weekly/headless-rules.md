# Rules for the headless weekly check

This session runs with no one to approve tool calls: a call outside the `--allowedTools` list of the run script is refused, not asked. These rules keep every call inside that list (written after the 19.10.2026 test runs, where such calls were refused).

- The working directory is already the repo root. Never `cd`, and never `git -C`: Claude Code refuses any git command that follows a change of directory, even into the same folder.
- One simple command per Bash call. No `for` or `while` loops, no shell variables (`$f`), no `$(...)`, no `;`. To do the same thing for several files, branches or PRs, make several calls in one turn.
- A pipe may go only into a read-only filter: `grep`, `head`, `tail`, `wc`, `sort`, `uniq`, `cut`, `tr`, `jq`. Not `sed`, `awk`, `cat`, `tee`, `xargs`, `python3` (the scoring script is a call of its own, never in a pipe).
- No redirects (`>`, `>>`, `2>&1`, `2>/dev/null`). Read output from the tool result; write files with the Write tool.
- Do not write or run scripts, with one exception: `python3 scripts/weekly/score-tasks.py --dir <RUNDIR> --report <REPORT>`, once, in step 3 (it is on the allowlist). Use Read, Grep and Glob for files in the repo, `jq` for the JSON inventory, and the counts already in `_research.md`.
- An object file on a branch: `git show "origin/<branch>:tickets/Data Migration/<file>"`, alone or piped into a filter. The files on a branch: `git ls-tree -r --name-only origin/<branch> -- "tickets/Data Migration/"`.
- A weekday: `date -j -f %F 2026-10-22 +%A`, one date per call.
- Every file you write stays inside the repo: the report, `package.xml` files and any notes under `sources/local/`. Give paths relative to the repo root (`sources/local/...`), never absolute. Edit and Grep work only inside the repo (the run uses acceptEdits there). Write is allowed anywhere, so a wrong absolute path writes outside the repo without any error. Correct the report with Edit; do not rewrite the whole file.
- If a call is refused anyway, do not retry variants of it: use another allowed way, and name the refused command in the report under what the check could not see.
