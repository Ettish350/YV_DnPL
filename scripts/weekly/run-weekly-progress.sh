#!/bin/bash
# Runs the weekly progress check headless. Called by launchd every Thursday 07:30 through ~/yv-weekly/run.sh,
# which pulls the clone first (see install-weekly-progress.sh), so this script always runs at its latest version.
# Uses a separate clone in ~/yv-weekly so macOS does not block a background job from ~/Documents.
# headless-rules.md keeps Claude's shell calls inside --allowedTools (no cd, loops, variables or redirects); keep the two lists in step with test-weekly-progress.sh.
W="$HOME/yv-weekly"
cd "$W/YV2026" || { echo "$(date) clone missing" >> "$W/run.log"; exit 1; }
{
  echo "=== $(date)"
  bash scripts/weekly/pull-inventory.sh
  python3 scripts/weekly/research-progress.py
  python3 scripts/weekly/record-pages.py
  # Retry (Lior, 22.10.2026): one dropped API connection ("Connection closed mid-response") used to end the run with no Issue and no email.
  # Up to 3 tries, 10 minutes apart. If a failed try already posted today's Issue, send only the email instead of running again.
  D=$(date +%F)
  SEND=0
  PROMPT="/weekly-progress"
  for TRY in 1 2 3; do
    claude -p "$PROMPT" --append-system-prompt-file scripts/weekly/headless-rules.md \
      --permission-mode acceptEdits --allowedTools \
      "Bash(sf org display:*)" "Bash(sf org list metadata:*)" "Bash(sf project retrieve start:*)" \
      "Bash(gh issue list:*)" "Bash(gh issue create:*)" "Bash(gh label create:*)" \
      "Bash(git fetch:*)" "Bash(git log:*)" "Bash(git show:*)" "Bash(git ls-tree:*)" \
      "Bash(gh pr list:*)" "Bash(gh pr view:*)" "Bash(gh pr diff:*)" \
      "Bash(grep:*)" "Bash(head:*)" "Bash(tail:*)" "Bash(wc:*)" "Bash(sort:*)" "Bash(uniq:*)" "Bash(cut:*)" "Bash(tr:*)" "Bash(jq:*)" \
      "Bash(mkdir:*)" "Bash(ls:*)" "Bash(date:*)" "Bash(python3 scripts/weekly/score-tasks.py:*)" \
      "Read" "Write" "Glob" "Grep" "mcp__atlassian" "mcp__claude_ai_Atlassian" "mcp__claude_ai_Atlassian_Rovo"
    RC=$?
    echo "=== exit $RC (try $TRY)"
    [ $RC -eq 0 ] && { SEND=1; break; }
    N=$(gh issue list --label weekly-report --state all --search "Weekly progress $D in:title" --json number --jq '.[0].number // empty' 2>/dev/null)
    if [ -n "$N" ]; then
      echo "=== Issue #$N was already posted today, sending the email only"
      SEND=1
      break
    fi
    [ $TRY -eq 3 ] && { echo "=== gave up after 3 tries"; break; }
    echo "=== retrying in 10 minutes"; sleep 600
    PROMPT="Run .claude/commands/weekly-progress.md in full. This is try $((TRY + 1)): an earlier try today stopped midway on a connection error. Before each Jira comment or transition in step 5b, read the task's comments and skip what an earlier try already did today. Before step 6, check gh issue list --label weekly-report --state all and do not post a second Issue for today."
  done
  # The email (Lior, 22.10.2026): built from score-tasks.py's task list with the daily email's template, sent with the
  # daily's exact-content call, one retry. Also after a failed try that already posted the Issue (extras may be missing then).
  if [ "$SEND" = 1 ]; then
    N=$(gh issue list --label weekly-report --state all --search "Weekly progress $D in:title" --json number --jq '.[0].number // empty' 2>/dev/null)
    MAIL="sources/local/weekly/email-$D.html"
    if python3 scripts/daily/build-daily-email.py "sources/local/weekly/tasks-$D.json" "$MAIL" --weekly ${N:+--issue "$N"}; then
      for ETRY in 1 2; do
        claude -p "Send one email with the Outlook connector (outlook_send_mail): to LiorP@malamteam.com only, no cc or bcc, no attachments. Subject: the exact text of $MAIL.subject.txt. Body: the exact content of $MAIL, unchanged, bodyType html. Do nothing else. At the end, say whether it was sent." \
          --permission-mode acceptEdits --allowedTools "Read" "mcp__claude_ai_Microsoft_365__outlook_send_mail"
        ERC=$?; [ $ERC -eq 0 ] && break
        echo "=== email try $ETRY failed (exit $ERC)"; [ $ETRY -lt 2 ] && sleep 120
      done
      echo "=== email step finished"
    else
      echo "=== no email: build-daily-email.py failed (no task list for $D?)"
    fi
  fi
} >> "$W/run.log" 2>&1
