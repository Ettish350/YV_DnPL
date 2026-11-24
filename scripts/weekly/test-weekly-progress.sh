#!/bin/bash
# Test run of the weekly progress check: same steps, headless, but posts nothing.
# The report goes to sources/local/weekly-test-<date>.md (ignored by git; inside the repo, so Claude can Edit and Grep it).
# Run from anywhere: bash scripts/weekly/test-weekly-progress.sh
cd "$(dirname "$0")/../.." || exit 1
OUT="sources/local/weekly-test-$(date +%F).md"
bash scripts/weekly/pull-inventory.sh
python3 scripts/weekly/research-progress.py
python3 scripts/weekly/record-pages.py
claude -p "Run .claude/commands/weekly-progress.md, steps 0 to 5 only, with RUNDIR = sources/local/weekly-test and REPORT = $OUT. This is a test: do NOT do step 5b, 6 or 7. Do not write anything to Jira (no comments, no transitions); in section 8 list the Jira changes step 5b WOULD make. Do not create any GitHub Issue or label. At the end, say whether you could read Jira and list the exact names of the Atlassian tools you used. Then list every tool call that was refused or needed approval, with the command and the reason, or say there were none." \
  --append-system-prompt-file scripts/weekly/headless-rules.md \
  --permission-mode acceptEdits --allowedTools \
  "Bash(sf org display:*)" "Bash(sf org list metadata:*)" "Bash(sf project retrieve start:*)" "Bash(gh issue list:*)" \
  "Bash(git fetch:*)" "Bash(git log:*)" "Bash(git show:*)" "Bash(git ls-tree:*)" \
  "Bash(gh pr list:*)" "Bash(gh pr view:*)" "Bash(gh pr diff:*)" \
  "Bash(grep:*)" "Bash(head:*)" "Bash(tail:*)" "Bash(wc:*)" "Bash(sort:*)" "Bash(uniq:*)" "Bash(cut:*)" "Bash(tr:*)" "Bash(jq:*)" \
  "Bash(mkdir:*)" "Bash(ls:*)" "Bash(date:*)" "Bash(python3 scripts/weekly/score-tasks.py:*)" \
  "Read" "Write" "Glob" "Grep" "mcp__atlassian" "mcp__claude_ai_Atlassian" "mcp__claude_ai_Atlassian_Rovo"
echo "Report: $OUT"
