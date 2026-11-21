#!/bin/bash
# Runs one counted Mac lane on the main checkout with no AI worker (owner, 2026-11-21: save Claude usage).
# Waits for any Editor that owns the main project, moves leftover scene backups aside (never deletes them), checks disk,
# runs the ARIA queue with the standard counted options, then writes gate3.txt and tables.md next to the queue logs.
# The program lead verifies and applies the results; this script never edits missions.csv, STATUS.md or state.json.
#
# Usage: run_lane.sh <lane-name> <jobs> [timeout-seconds]
#   lane-name  e.g. lane52 -> logs in $LOGROOT/<yyyy-mm-dd>/<lane-name>/a
#   jobs       comma list, first job is the uncounted warm-up, e.g. "S031:130366:en,S013:130365:en,..."
set -u
REPO=/Volumes/T7/Projects/WarlineCapture
LOGROOT=/Volumes/T7/Projects/WarlineCapture-program-logs
lane=${1:?lane name}; jobs=${2:?jobs}; timeout=${3:-30000}
L=$LOGROOT/$(date +%F)/$lane; mkdir -p "$L"
cd "$REPO" || exit 1
editor_owns() { ps -axo pid=,comm= | awk '$2 ~ /Unity\.app\/Contents\/MacOS\/Unity$/ {print $1}' | while read -r p; do
  ps -o args= -p "$p" | grep -qE -- "-projectPath $REPO( |$)" && echo "$p"; done | grep -q .; }
for _ in $(seq 1 60); do editor_owns || break; echo "$(date '+%H:%M') waiting for the main-project Editor" >> "$L/run.txt"; sleep 60; done
if editor_owns; then echo "main-project Editor still running after 60 min" >> "$L/run.txt"; exit 99; fi
for b in Temp/__Backupscenes/*.backup; do [ -f "$b" ] || continue; mkdir -p "$L/temp-backup-moved"; mv "$b" "$L/temp-backup-moved/"; done
Tools/Missions/disk_guard.sh --quiet; [ $? -eq 2 ] && { echo "disk_guard stop limit" >> "$L/run.txt"; exit 97; }
echo "$jobs" > "$L/jobs-a.txt"
echo "$(date '+%F %H:%M') start on $(git rev-parse --short HEAD) branch $(git branch --show-current)" >> "$L/run.txt"
python3 -B Tools/Missions/run_aria_queue.py --jobs "$jobs" --menu-journey --validation-library --no-table \
  --timeout "$timeout" --log-dir "$L/a" > "$L/a.out.txt" 2>&1
rc=$?
echo "$(date '+%F %H:%M') queue rc=$rc: $(grep -h 'SkirmishAriaQueue\] result=' "$L/a/unity.log" 2>/dev/null | tail -1)" >> "$L/run.txt"
python3 -B Tools/Missions/gate3_objective.py "$L/a/unity.log" > "$L/gate3.txt" 2>"$L/gate3.err"
python3 -B - "$L" > "$L/tables.md" <<'EOF'
import re, sys
L = sys.argv[1]; log = L + '/a/unity.log'
rows, cur = [], None
for i, line in enumerate(open(log, errors='replace'), 1):
    if line.startswith('[SkirmishLaunch] queued expanded catalog='):
        cur = dict(cid=re.search(r'catalog=(\S+)', line).group(1), res='-', pct='-', menu='-', ret='-', blk=0, first=''); rows.append(cur)
    elif cur is None: continue
    elif line.startswith('[SkirmishTerminalObjective] catalog='):
        m = re.search(r'outcome=(\S+) reason=(\S+).*deadlinePct=(\S+)', line)
        if m: cur.update(res=f'{m.group(1)} {m.group(2)}', pct=m.group(3), tl=i)
    elif line.startswith('[SkirmishMenuJourney] catalog=') and ' result=' in line and cur['menu'] == '-':
        cur['menu'] = ('Passed' if 'result=Passed' in line else 'FAIL') + f' :{i}'
    elif line.startswith('[SkirmishTerminalProbe] result='):
        cur['ret'] = ('Passed' if 'result=Passed' in line and 'MAIN MENU' in line else 'FAIL') + f' :{i}'
    elif line.startswith('[AriaBlocked]'):
        cur['blk'] += 1
        if not cur['first']: cur['first'] = f':{i} ' + line.strip()[:90]
    elif 'expandedStartupFailure=' in line and not cur['first']:
        cur['first'] = f':{i} ' + re.search(r'expandedStartupFailure=\S+', line).group(0)
jobs = open(L + '/jobs-a.txt').read().strip().split(',')
print('| # | job | result | % | menu | return | AriaBlocked | first failure |'); print('|---|---|---|---|---|---|---|---|')
k = 0
for n, j in enumerate(jobs, 1):
    cid = j.split(':')[0]
    if k < len(rows) and rows[k]['cid'] == cid:
        r = rows[k]; k += 1
        print(f"| {n} | {j} | {r['res']} | {r['pct']} | {r['menu']} | {r['ret']} | {r['blk']} | {r['first']} |")
    else:
        print(f'| {n} | {j} | not dispatched (see a.out.txt) | | | | | |')
EOF
echo "$(date '+%F %H:%M') done: $L/tables.md, $L/gate3.txt" >> "$L/run.txt"
exit $rc
