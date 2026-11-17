#!/bin/bash
# Keeps the T7 volume from filling up during mission work (owner rule, 2026-11-17: agents manage
# disk usage themselves; the owner must never have to free space).
#
# Every run:
#   1. keeps git background maintenance off for this repository (it once left 183 GB of temp packs);
#   2. deletes leftover git temp packs (older than 30 minutes, and only while no git pack job runs);
#   3. deletes finished Codex scratch copies whose patch is already saved;
#   4. reports free space and writes to STATUS.md when it deleted something or space is low.
# It never touches Library, Assets, real pack files, evidence or program logs.
#
# Usage: disk_guard.sh [--quiet] [--watch <minutes>]
#   --watch N   repeat every 5 minutes for N minutes; start it detached at the start of every shift
#               and daily cycle: nohup Tools/Missions/disk_guard.sh --quiet --watch 1440 >/dev/null 2>&1 &
# Exit code (single run): 0 = fine, 1 = low (below WARN_GB), 2 = stop (below STOP_GB: start no
# Unity run and no Codex batch until space is back).
set -u
REPO=/Volumes/T7/Projects/WarlineCapture
VOL=/Volumes/T7
WT=/Volumes/T7/Projects/WarlineCapture-wt
BRIEFS=/Volumes/T7/Projects/WarlineCapture-program-logs/briefs
STATUS=$REPO/Design/Orchestration/STATUS.md
WARN_GB=${DISK_GUARD_WARN_GB:-80}
STOP_GB=${DISK_GUARD_STOP_GB:-30}
QUIET=0; WATCH=0
while [ $# -gt 0 ]; do
  case $1 in
    --quiet) QUIET=1 ;;
    --watch) WATCH=${2:-0}; shift ;;
    *) echo "unknown option: $1" >&2; exit 64 ;;
  esac
  shift
done

free_gb() { df -g "$VOL" | awk 'NR==2 {print $4}'; }
note() { echo "- $(date '+%Y-%m-%d %H:%M') disk guard: $*" >> "$STATUS"; }
say() { [ "$QUIET" = 1 ] || echo "$*"; }
git_pack_job_running() {
  pgrep -f 'git[^ ]* (pack-objects|repack|maintenance|gc|index-pack|fetch|push|clone)' >/dev/null
}

keep_maintenance_off() {
  [ "$(git -C "$REPO" config --local --get maintenance.auto 2>/dev/null)" = false ] ||
    { git -C "$REPO" config --local maintenance.auto false && note "git maintenance.auto was on; turned off again"; }
  [ "$(git -C "$REPO" config --local --get gc.auto 2>/dev/null)" = 0 ] ||
    { git -C "$REPO" config --local gc.auto 0 && note "git gc.auto was on; turned off again"; }
}

clean_temp_packs() {
  local dir=$REPO/.git/objects/pack count=0 kb=0 size f
  git_pack_job_running && return 0
  while IFS= read -r f; do
    size=$(du -k "$f" 2>/dev/null | cut -f1)
    rm -f "$f" && { count=$((count + 1)); kb=$((kb + ${size:-0})); }
  done < <(find "$dir" -maxdepth 1 -type f \( -name 'tmp_pack_*' -o -name '.tmp-*' \) -mmin +30 2>/dev/null)
  [ "$count" -gt 0 ] && note "deleted $count leftover git temp packs ($((kb / 1048576)) GB)"
  return 0
}

clean_codex_copies() {
  local d name count=0
  for d in "$WT"/codex/*/; do
    [ -d "$d" ] || continue
    d=${d%/}; name=$(basename "$d")
    # Finished only: the saved patch is newer than the copy's baseline, and no Codex runs in it.
    [ -f "$d/.git/HEAD" ] && [ "$BRIEFS/$name.patch" -nt "$d/.git/HEAD" ] || continue
    pgrep -f "codex exec -C $d " >/dev/null && continue
    rm -rf "$d" && count=$((count + 1))
  done
  [ "$count" -gt 0 ] && say "removed $count finished Codex scratch copies"
  return 0
}

biggest() {
  local p
  for p in "$REPO/.git/objects" "$REPO/Logs" "$REPO/T Logs" "$REPO/Temp" "$WT" \
           /Volumes/T7/Projects/WarlineCapture-program-logs; do
    [ -e "$p" ] && du -sg "$p" 2>/dev/null
  done | sort -rn | head -4 | awk '{size=$1; $1=""; printf "%s GB%s; ", size, $0}'
}

LAST=ok
check() {
  local free state=ok
  keep_maintenance_off
  clean_temp_packs
  clean_codex_copies
  free=$(free_gb)
  if [ "$free" -lt "$STOP_GB" ]; then state=stop
  elif [ "$free" -lt "$WARN_GB" ]; then state=low
  fi
  say "free=${free}GB state=$state (warn below $WARN_GB, stop below $STOP_GB)"
  if [ "$state" != ok ] && [ "$state" != "$LAST" ]; then
    if [ "$state" = stop ]; then
      note "STOP: only $free GB free. Start no Unity run and no Codex batch. Largest: $(biggest)"
    else
      note "LOW: $free GB free (below $WARN_GB). Find what is growing now. Largest: $(biggest)"
    fi
  fi
  [ "$state" = ok ] && [ "$LAST" != ok ] && note "free space back to $free GB"
  LAST=$state
  case $state in ok) return 0 ;; low) return 1 ;; *) return 2 ;; esac
}

if [ "$WATCH" -gt 0 ]; then
  # One watcher at a time: a second start exits quietly while the first is alive.
  PIDFILE=/Volumes/T7/Projects/WarlineCapture-program-logs/disk_guard.pid
  old=$(cat "$PIDFILE" 2>/dev/null)
  if [ -n "$old" ] && kill -0 "$old" 2>/dev/null && ps -p "$old" -o command= | grep -q disk_guard; then
    say "watcher already running (pid $old)"; exit 0
  fi
  echo $$ > "$PIDFILE"
  trap 'rm -f "$PIDFILE"' EXIT
  end=$(( $(date +%s) + WATCH * 60 ))
  while [ "$(date +%s)" -lt "$end" ]; do check; sleep 300; done
  exit 0
fi
check
