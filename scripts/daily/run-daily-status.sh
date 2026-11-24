#!/bin/bash
# Daily status sheet, every working day at 17:00 (Sunday to Thursday). Called by launchd through ~/yv-weekly/daily-run.sh,
# which pulls the clone first (see install-daily-status.sh), so this script always runs at its latest version.
# Same evidence and the same scoring (scripts/weekly/score-tasks.py) as the weekly check, in TEST mode:
# nothing is written to Jira and no Issue is posted. Output: ~/yv-weekly/daily/Daily status <date>.xlsx (and a "latest" copy),
# plus one summary email to Lior (LiorP@malamteam.com only) built by build-daily-email.py and sent through Outlook.
# Run it by hand from the clone or from this repo: bash scripts/daily/run-daily-status.sh
cd "$(dirname "$0")/../.." || exit 1
D=$(date +%F); DAY=$(date +%d.%m.%Y)
OUT="$HOME/yv-weekly/daily"; mkdir -p "$OUT" sources/local/daily
REPORT="sources/local/daily/report-$D.md"; TASKS="sources/local/daily/tasks-$D.json"
echo "=== $(date) daily status"
bash scripts/weekly/pull-inventory.sh
python3 scripts/weekly/research-progress.py
python3 scripts/weekly/record-pages.py
# Retry (Lior, 22.10.2026): a dropped API connection used to mean no sheet and no email that day. Up to 3 tries, 10 minutes apart.
# Safe to repeat: this run writes nothing outside the repo and the daily folder.
for TRY in 1 2 3; do
  claude -p "Run .claude/commands/weekly-progress.md, steps 0 to 5 only, with RUNDIR = sources/local/daily and REPORT = $REPORT. This is a daily TEST run: do NOT do step 5b, 6 or 7. Do not write anything to Jira (no comments, no transitions). Do not create any GitHub Issue or label. Do not send any email. score-tasks.py (step 3) writes $TASKS, the list the sheet and the email are built from; do not write it yourself. At the end, say whether you could read Jira." \
    --permission-mode acceptEdits --allowedTools \
    "Bash(sf org display:*)" "Bash(sf org list metadata:*)" "Bash(sf project retrieve start:*)" "Bash(gh issue list:*)" \
    "Bash(git fetch:*)" "Bash(git log:*)" "Bash(git show:*)" \
    "Bash(gh pr list:*)" "Bash(gh pr view:*)" "Bash(gh pr diff:*)" \
    "Bash(mkdir:*)" "Bash(ls:*)" "Bash(date:*)" "Bash(python3 scripts/weekly/score-tasks.py:*)" \
    "Read" "Write" "Glob" "Grep" "mcp__atlassian" "mcp__claude_ai_Atlassian" "mcp__claude_ai_Atlassian_Rovo"
  [ -s "$TASKS" ] && break
  [ $TRY -lt 3 ] && { echo "=== try $TRY wrote no task list, retrying in 10 minutes"; sleep 600; }
done
[ -s "$TASKS" ] || { echo "=== no task list written ($TASKS), no sheet today"; exit 1; }
python3 scripts/daily/build-daily-status.py "$TASKS" "$OUT/Daily status $D.xlsx" "$DAY" || exit 1
cp "$OUT/Daily status $D.xlsx" "$OUT/Daily status - latest.xlsx"
cp "$REPORT" "$OUT/Report $D.md" 2>/dev/null
echo "=== done: $OUT/Daily status $D.xlsx"
PREV=$(ls sources/local/daily/tasks-*.json 2>/dev/null | grep -v "tasks-$D.json" | sort | tail -1)
MAIL="sources/local/daily/email-$D.html"
python3 scripts/daily/build-daily-email.py "$TASKS" "$MAIL" "$PREV" || exit 1
for TRY in 1 2; do
  claude -p "Send one email with the Outlook connector (outlook_send_mail): to LiorP@malamteam.com only, no cc or bcc, no attachments. Subject: the exact text of $MAIL.subject.txt. Body: the exact content of $MAIL, unchanged, bodyType html. Do nothing else. At the end, say whether it was sent." \
    --permission-mode acceptEdits --allowedTools "Read" "mcp__claude_ai_Microsoft_365__outlook_send_mail"
  RC=$?; [ $RC -eq 0 ] && break
  echo "=== email try $TRY failed (exit $RC)"; [ $TRY -lt 2 ] && sleep 120
done
echo "=== email step finished"
