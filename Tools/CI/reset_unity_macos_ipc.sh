#!/usr/bin/env bash
set -euo pipefail

DRY_RUN=0
KILL_UNITY=0
QUIT_HUB=0
CONFIRM_NO_EDITORS=0

usage() {
    cat <<'EOF'
Usage: Tools/CI/reset_unity_macos_ipc.sh --confirm-no-editors [--dry-run] [--kill-unity] [--quit-hub]

Resets Unity helper IPC on macOS after a stuck Codex/Unity batchmode run.

Default cleanup is intentionally narrow:
  - stops Unity.Licensing.Client helper processes
  - stops Unity Package Manager helper processes when found
  - removes stale Unity licensing and UPM sockets under /private/tmp

It does not kill the Unity Editor by default. Pass --kill-unity only for a
known stuck batchmode/editor process. Pass --quit-hub when Unity Hub keeps
respawning the generic Licensing Client and Unity reports unsupported protocol
or Package Manager stays blocked until reboot.

The reset always requires --confirm-no-editors because process discovery can
be restricted for agent sessions. Never use it while any Unity Editor is open.
EOF
}

for arg in "$@"; do
    case "$arg" in
        --dry-run)
            DRY_RUN=1
            ;;
        --kill-unity)
            KILL_UNITY=1
            ;;
        --quit-hub)
            QUIT_HUB=1
            ;;
        --confirm-no-editors)
            CONFIRM_NO_EDITORS=1
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "[UnityReset] Unknown argument: $arg" >&2
            usage >&2
            exit 2
            ;;
    esac
done

if [[ "$CONFIRM_NO_EDITORS" -eq 0 ]]; then
    echo "[UnityReset] ERROR: reset is destructive. Re-run with --confirm-no-editors only after every Unity Editor is closed." >&2
    exit 64
fi

active_editor_pids="$(pgrep -f '/Unity\.app/Contents/MacOS/Unity' 2>/dev/null || true)"
if [[ -n "$active_editor_pids" && "$KILL_UNITY" -eq 0 ]]; then
    echo "[UnityReset] ERROR: Unity Editor is active. Refusing to reset shared licensing or UPM IPC because it would disconnect the active Editor." >&2
    echo "[UnityReset] Close all Editors first, or use --kill-unity only for a known stuck batchmode run." >&2
    exit 4
fi

run() {
    echo "[UnityReset] $*"
    if [[ "$DRY_RUN" -eq 0 ]]; then
        "$@"
    fi
}

list_pids() {
    local pattern="$1"
    pgrep -fl "$pattern" 2>/dev/null |
        awk -v self="$$" '$1 != self {print $1}' || true
}

terminate_pattern() {
    local label="$1"
    local pattern="$2"
    local pids
    pids="$(list_pids "$pattern")"
    if [[ -z "$pids" ]]; then
        echo "[UnityReset] No $label processes found."
        return
    fi

    echo "[UnityReset] Stopping $label PID(s): ${pids//$'\n'/ }"
    if [[ "$DRY_RUN" -eq 0 ]]; then
        kill $pids 2>/dev/null || true
        sleep 2
        local remaining
        remaining="$(list_pids "$pattern")"
        if [[ -n "$remaining" ]]; then
            echo "[UnityReset] Force-stopping $label PID(s): ${remaining//$'\n'/ }"
            kill -9 $remaining 2>/dev/null || true
        fi
    fi
}

terminate_pattern "Unity Licensing Client" "Unity\\.Licensing\\.Client"
terminate_pattern "Unity Package Manager" "UnityPackageManager|Unity Package Manager|Package Manager.*Unity|/upm"

if [[ "$QUIT_HUB" -eq 1 ]]; then
    if [[ "$DRY_RUN" -eq 0 ]]; then
        osascript -e 'tell application "Unity Hub" to quit' >/dev/null 2>&1 || true
        sleep 2
    else
        echo "[UnityReset] osascript -e 'tell application \"Unity Hub\" to quit'"
    fi
    terminate_pattern "Unity Hub" "Unity Hub\\.app/Contents/MacOS/Unity Hub|Unity Hub Helper"
    terminate_pattern "Unity Licensing Client" "Unity\\.Licensing\\.Client"
fi

if [[ "$KILL_UNITY" -eq 1 ]]; then
    terminate_pattern "Unity Editor" "/Unity\\.app/Contents/MacOS/Unity"
else
    if [[ -n "$(list_pids "/Unity\\.app/Contents/MacOS/Unity")" ]]; then
        echo "[UnityReset] Unity Editor is running; leaving it alive. Use --kill-unity only for a known stuck editor/batchmode process."
    fi
fi

if [[ "$DRY_RUN" -eq 1 ]]; then
    find /private/tmp -maxdepth 1 \
        \( -name 'Unity-LicenseClient-*.sock' \
        -o -name 'Unity-Upm-*.sock' \
        -o -name 'Upm-*.sock' \) \
        -print 2>/dev/null || true
else
    find /private/tmp -maxdepth 1 \
        \( -name 'Unity-LicenseClient-*.sock' \
        -o -name 'Unity-Upm-*.sock' \
        -o -name 'Upm-*.sock' \) \
        -print -delete 2>/dev/null || true
fi

if [[ -z "$(list_pids "/Unity\\.app/Contents/MacOS/Unity")" ]]; then
    if [[ -e /private/tmp/unitypreflock ]]; then
        run rm -f /private/tmp/unitypreflock
    fi
fi

echo "[UnityReset] Done."
