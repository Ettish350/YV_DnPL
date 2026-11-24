#!/bin/bash
# Install on Lior's Mac: daily status sheet and email Sunday to Thursday at 17:00, end of the working day (moved from 06:45 on 21.10.2026, so the Mac is on). Safe to run again.
# Uses the same clone as the weekly check (~/yv-weekly/YV2026), because macOS blocks background jobs from ~/Documents.
# If the Mac is asleep at 17:00, macOS runs it when it wakes. If it was shut down, that day is skipped.
set -e
for c in claude sf git python3; do command -v $c >/dev/null || { echo "Missing: $c (run this from a Terminal where '$c' works)"; exit 1; }; done
python3 -c "import openpyxl" 2>/dev/null || { echo "Missing: openpyxl. Fix: pip3 install openpyxl"; exit 1; }
W="$HOME/yv-weekly"; mkdir -p "$W/daily"
[ -d "$W/YV2026/.git" ] || git clone https://github.com/liorpridorrrrrrr/YV2026.git "$W/YV2026"
cat > "$W/daily-run.sh" <<'RUN'
git -C ~/yv-weekly/YV2026 pull --ff-only >> ~/yv-weekly/daily.log 2>&1
exec bash ~/yv-weekly/YV2026/scripts/daily/run-daily-status.sh >> ~/yv-weekly/daily.log 2>&1
RUN
chmod +x "$W/daily-run.sh"
PL="$HOME/Library/LaunchAgents/com.lpconsulting.yv-daily-status.plist"
DAYS=""; for d in 0 1 2 3 4; do DAYS="$DAYS<dict><key>Weekday</key><integer>$d</integer><key>Hour</key><integer>17</integer><key>Minute</key><integer>0</integer></dict>"; done
cat > "$PL" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>Label</key><string>com.lpconsulting.yv-daily-status</string>
  <key>ProgramArguments</key><array><string>/bin/bash</string><string>$W/daily-run.sh</string></array>
  <key>EnvironmentVariables</key><dict><key>PATH</key><string>$PATH</string><key>HOME</key><string>$HOME</string></dict>
  <key>StartCalendarInterval</key><array>$DAYS</array>
  <key>StandardOutPath</key><string>$W/launchd-daily.log</string>
  <key>StandardErrorPath</key><string>$W/launchd-daily.log</string>
</dict></plist>
PLIST
plutil -lint "$PL" >/dev/null
launchctl bootout "gui/$(id -u)" "$PL" 2>/dev/null || true
launchctl bootstrap "gui/$(id -u)" "$PL"
echo "Installed: Sunday to Thursday 17:00. Sheets: $W/daily/ (open 'Daily status - latest.xlsx'). Log: $W/daily.log"
echo "Run it now with: bash $W/daily-run.sh"
