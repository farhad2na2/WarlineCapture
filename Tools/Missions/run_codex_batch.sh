#!/bin/bash
# Runs Codex one-shot tasks in parallel and reports start and finish to
# Design/Orchestration/STATUS.md so the owner can see what is running.
#
# Each task gets a plain copy of the script folders (no git worktree: checking one out of this
# repository takes many minutes). The result is a patch file that the Unity-lane owner applies
# to missions/integration with `git apply --3way` while no Unity run is active.
#
# Usage: run_codex_batch.sh <name>:<effort>[:<minutes>] ...
#   name    brief at $BRIEFS/<name>.md
#   effort  low | medium | high | xhigh
#   minutes hard time limit, default 60
set -u
REPO=/Volumes/T7/Projects/WarlineCapture
WORK=/Volumes/T7/Projects/WarlineCapture-wt/codex
BRIEFS=/Volumes/T7/Projects/WarlineCapture-program-logs/briefs
STATUS=$REPO/Design/Orchestration/STATUS.md
CODEX=/Applications/ChatGPT.app/Contents/Resources/codex-cli/CodexCLI.app/Contents/MacOS/codex

note() { echo "- $(date '+%Y-%m-%d %H:%M') codex: $*" >> "$STATUS"; }

run_one() {
  local spec=$1 name effort minutes dir brief report transcript patch
  name=${spec%%:*}; effort=$(echo "$spec" | cut -d: -f2); minutes=$(echo "$spec" | cut -d: -f3)
  minutes=${minutes:-60}
  dir=$WORK/$name
  brief=$BRIEFS/$name.md; report=$BRIEFS/$name-report.txt
  transcript=$BRIEFS/$name-transcript.log; patch=$BRIEFS/$name.patch
  [ -f "$brief" ] || { note "$name not started: brief missing"; return 1; }
  rm -rf "$dir"; mkdir -p "$dir"
  ( cd "$REPO" && rsync -a --relative \
      Assets/Game/Configs/Skirmish Assets/Game/Configs/SkirmishExpansion \
      Assets/Game/Resources/Skirmish Assets/Game/Resources/SkirmishExpansion \
      Assets/Game/Scripts Assets/Tests Tools/Missions AGENTS.md "$dir/" &&
    rsync -a --relative --prune-empty-dirs --include='*/' --include='*.md' --include='*.csv' \
      --exclude='*' Design/MapVariants Design/Roadmap/Skirmish_Expansion "$dir/" ) >>"$transcript" 2>&1 ||
    { note "$name not started: copy failed (see $transcript)"; return 1; }
  ( cd "$dir" && git init -q && git add -A && git -c user.name=codex -c user.email=codex@local \
      commit -q -m baseline ) >>"$transcript" 2>&1
  note "$name started (model ${CODEX_MODEL:-gpt-6.2-sol}, effort $effort, limit ${minutes} min)"
  "$CODEX" exec -C "$dir" -s workspace-write --skip-git-repo-check --color never \
    -m "${CODEX_MODEL:-gpt-6.2-sol}" -c model_reasoning_effort="$effort" -o "$report" - < "$brief" >>"$transcript" 2>&1 &
  local pid=$!
  ( sleep $((minutes * 60)); kill "$pid" 2>/dev/null ) &
  local dog=$!
  wait "$pid"; local code=$?
  kill "$dog" 2>/dev/null
  ( cd "$dir" && git add -A && git diff --cached --binary > "$patch" )
  local files; files=$(cd "$dir" && git diff --cached --name-only | wc -l | tr -d ' ')
  note "$name finished: exit $code, $files files changed, patch $patch, report $report (not yet verified or merged)"
}

# Disk guard (owner rule, 2026-11-17): clean temp files first; start nothing when space is critical.
"$REPO/Tools/Missions/disk_guard.sh" --quiet
if [ $? -eq 2 ]; then note "batch not started: free disk below the stop limit (see disk guard line)"; exit 2; fi

for spec in "$@"; do run_one "$spec" & done
wait
note "batch finished: $*"
"$REPO/Tools/Missions/disk_guard.sh" --quiet
exit 0
