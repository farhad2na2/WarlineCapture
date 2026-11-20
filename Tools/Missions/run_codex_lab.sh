#!/bin/bash
# Runs ONE Codex task in the Codex lab: a separate full checkout with its own Unity Library at
# /Volumes/T7/Projects/WarlineCapture-wt/codexlab, so Codex can compile and run Unity tests without
# using the main checkout's Unity lane (owner direction, 2026-11-18).
#
# The task works on its own branch missions/<name>, cut from missions/integration (or from
# --base <branch>), and commits there. The program lead merges that branch into missions/integration.
# One lab task at a time (one Editor per project): a lock directory serialises runs.
#
# Usage: run_codex_lab.sh [--base <branch>] <name>:<effort>[:<minutes>]
#   name    brief at $BRIEFS/<name>.md (the lab header is prepended automatically)
set -u
REPO=/Volumes/T7/Projects/WarlineCapture
LAB=/Volumes/T7/Projects/WarlineCapture-wt/codexlab
LOCK=/Volumes/T7/Projects/WarlineCapture-wt/codexlab.lock
BRIEFS=/Volumes/T7/Projects/WarlineCapture-program-logs/briefs
LOGS=/Volumes/T7/Projects/WarlineCapture-program-logs/codexlab
STATUS=$REPO/Design/Orchestration/STATUS.md
CODEX=/Applications/ChatGPT.app/Contents/Resources/codex-cli/CodexCLI.app/Contents/MacOS/codex
BASE=missions/integration
if [ "${1:-}" = "--base" ]; then BASE=$2; shift 2; fi
spec=${1:?usage: run_codex_lab.sh [--base branch] name:effort[:minutes]}
name=${spec%%:*}; effort=$(echo "$spec" | cut -d: -f2); minutes=$(echo "$spec" | cut -d: -f3); minutes=${minutes:-90}
brief=$BRIEFS/$name.md; report=$BRIEFS/$name-report.txt; transcript=$BRIEFS/$name-transcript.log
note() { echo "- $(date '+%Y-%m-%d %H:%M') codex lab: $*" >> "$STATUS"; }

[ -f "$brief" ] || { note "$name not started: brief missing"; exit 1; }
"$REPO/Tools/Missions/disk_guard.sh" --quiet
if [ $? -eq 2 ]; then note "$name not started: disk below the stop limit"; exit 2; fi
mkdir "$LOCK" 2>/dev/null || { note "$name not started: lab busy ($(cat "$LOCK/owner" 2>/dev/null))"; exit 3; }
echo "$name $(date '+%H:%M') pid $$" > "$LOCK/owner"
trap 'rm -rf "$LOCK"' EXIT
mkdir -p "$LOGS/$name"

# A force-stopped Editor leaves Temp/__Backupscenes/*.backup behind; the next launch then waits on a hidden
# scene-recovery dialog. With no lab Editor running, move any leftover backup into the task's log folder (never delete it).
if ! ps -axo command= | grep "Unity.app/Contents/MacOS/Unity" | grep -v grep | grep -qi "projectPath $LAB"; then
  for b in "$LAB"/Temp/__Backupscenes/*.backup; do
    [ -f "$b" ] || continue
    mkdir -p "$LOGS/$name/temp-backup-moved" && mv "$b" "$LOGS/$name/temp-backup-moved/$(basename "$b").$(date +%H%M%S)" &&
      note "$name: moved leftover lab scene backup $(basename "$b") to $LOGS/$name/temp-backup-moved"
  done
fi

( cd "$LAB" && git status --porcelain --untracked-files=no | grep -q . && git stash push -q -m "lab-leftover-before-$name" ;
  git checkout -q -B "missions/$name" "$BASE" ) >>"$transcript" 2>&1 ||
  { note "$name not started: could not cut missions/$name from $BASE in the lab (see $transcript)"; exit 1; }

note "$name started on missions/$name from $BASE (model ${CODEX_MODEL:-gpt-6.2-sol}, effort $effort, limit $minutes min, logs $LOGS/$name)"
{ cat "$BRIEFS/_codexlab-header.md"; echo; echo "Your Unity log folder: $LOGS/$name"; echo "Your branch: missions/$name (already checked out)"; echo; cat "$brief"; } |
  "$CODEX" exec -C "$LAB" -s danger-full-access --skip-git-repo-check --color never \
    -m "${CODEX_MODEL:-gpt-6.2-sol}" -c model_reasoning_effort="$effort" -o "$report" - >>"$transcript" 2>&1 &
pid=$!
( sleep $((minutes * 60)); kill "$pid" 2>/dev/null ) & dog=$!
wait "$pid"; code=$?
kill "$dog" 2>/dev/null
# Never leave a lab Editor behind: stop only Editors whose project is the lab checkout.
for p in $(ps -axo pid=,command= | grep "Unity.app/Contents/MacOS/Unity" | grep -i "projectPath $LAB" | grep -v grep | awk '{print $1}'); do
  kill "$p" 2>/dev/null; note "$name: stopped leftover lab Editor pid $p"
done
commits=$(cd "$LAB" && git log --oneline "$BASE..missions/$name" | wc -l | tr -d ' ')
dirty=$(cd "$LAB" && git status --porcelain --untracked-files=no | wc -l | tr -d ' ')
note "$name finished: exit $code, $commits commits on missions/$name, $dirty uncommitted files, report $report (not yet verified or merged)"
"$REPO/Tools/Missions/disk_guard.sh" --quiet
exit 0
