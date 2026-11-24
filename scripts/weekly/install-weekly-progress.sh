#!/bin/bash
# Install on Lior's Mac: weekly progress check every Thursday 07:30. Safe to run again.
# ~/yv-weekly/run.sh is a wrapper that pulls the clone and runs the repo script, so later repo changes reach the job.
# If the Mac is asleep at 07:30, macOS runs it when it wakes. If it was shut down, that week is skipped.
set -e
for c in claude sf git; do command -v $c >/dev/null || { echo "Missing: $c (run this from a Terminal where '$c' works)"; exit 1; }; done
command -v gh >/dev/null || echo "WARNING: gh not found. The report will be written but not posted as an Issue. Fix: brew install gh && gh auth login"
W="$HOME/yv-weekly"; mkdir -p "$W"
[ -d "$W/YV2026/.git" ] || git clone https://github.com/liorpridorrrrrrr/YV2026.git "$W/YV2026"
cat > "$W/run.sh" <<'RUN'
git -C ~/yv-weekly/YV2026 pull --ff-only >> ~/yv-weekly/run.log 2>&1
exec bash ~/yv-weekly/YV2026/scripts/weekly/run-weekly-progress.sh
RUN
chmod +x "$W/run.sh"
PL="$HOME/Library/LaunchAgents/com.lpconsulting.yv-weekly-progress.plist"
cat > "$PL" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>Label</key><string>com.lpconsulting.yv-weekly-progress</string>
  <key>ProgramArguments</key><array><string>/bin/bash</string><string>$W/run.sh</string></array>
  <key>EnvironmentVariables</key><dict><key>PATH</key><string>$PATH</string><key>HOME</key><string>$HOME</string></dict>
  <key>StartCalendarInterval</key><dict><key>Weekday</key><integer>4</integer><key>Hour</key><integer>7</integer><key>Minute</key><integer>30</integer></dict>
  <key>StandardOutPath</key><string>$W/launchd.log</string>
  <key>StandardErrorPath</key><string>$W/launchd.log</string>
</dict></plist>
PLIST
launchctl bootout "gui/$(id -u)" "$PL" 2>/dev/null || true
launchctl bootstrap "gui/$(id -u)" "$PL"
echo "Installed: every Thursday 07:30. Log: $W/run.log"
echo "Test it now with: bash $W/run.sh"
