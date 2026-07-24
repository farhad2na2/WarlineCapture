#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="$(pwd)"
UNITY_EXE=""
LOG_FILE="/private/tmp/warline-unity-$(date +%Y%m%d-%H%M%S).log"
TIMEOUT_SECONDS=0

usage() {
    cat <<'EOF'
Usage: Tools/CI/invoke_unity_macos.sh [options] -- [Unity arguments]

Options:
  --unity PATH       Unity executable path. Defaults to ProjectVersion.txt.
  --project PATH     Unity project path. Defaults to current directory.
  --log PATH         Unity log file path. Defaults under /private/tmp.
  --timeout SECONDS  Kill Unity if the command exceeds timeout.

The wrapper always supplies:
  -projectPath <project> -logFile <log>

It intentionally does NOT pass -batchmode on macOS. Unity 6000.x batchmode
handshakes with Hub's generic LicenseClient-farhad and fails with protocol
1.18.1 mismatch. GUI licensing avoids that broken path.

Pass remaining Unity arguments after --, for example:
  Tools/CI/invoke_unity_macos.sh --timeout 240 -- -quit -executeMethod MyTests.Run

Forbidden:
  -batchmode / --batchmode in Unity arguments (rejected)
  --reset-ipc / --batchmode wrapper flags (rejected)
  Direct Unity binary invocation (use this wrapper; see AGENTS.md)
EOF
}

while [[ "$#" -gt 0 ]]; do
    case "$1" in
        --unity)
            UNITY_EXE="${2:-}"
            shift 2
            ;;
        --project)
            PROJECT_PATH="${2:-}"
            shift 2
            ;;
        --log)
            LOG_FILE="${2:-}"
            shift 2
            ;;
        --timeout)
            TIMEOUT_SECONDS="${2:-0}"
            shift 2
            ;;
        --reset-ipc)
            echo "[UnityInvokeMac] ERROR: automatic IPC reset is disabled. Close every Unity Editor, then run reset_unity_macos_ipc.sh --confirm-no-editors only for a known stuck environment." >&2
            exit 64
            ;;
        --batchmode)
            echo "[UnityInvokeMac] ERROR: --batchmode is forbidden on macOS for this project. Use invoke_unity_macos.sh without batchmode; it already runs the GUI licensing path." >&2
            exit 64
            ;;
        --skip-reset)
            echo "[UnityInvokeMac] --skip-reset is obsolete; IPC reset is disabled by default." >&2
            shift
            ;;
        --)
            shift
            break
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "[UnityInvokeMac] Unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

for arg in "$@"; do
    case "$arg" in
        -batchmode|--batchmode)
            echo "[UnityInvokeMac] ERROR: -batchmode is forbidden on macOS. This wrapper uses GUI licensing to avoid the Unity 6000.x / Hub protocol 1.18.1 mismatch. Remove -batchmode and rerun the same command." >&2
            exit 64
            ;;
    esac
done

if [[ -z "$UNITY_EXE" ]]; then
    version="$(awk -F': ' '/m_EditorVersion:/ {print $2; exit}' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt")"
    UNITY_EXE="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity"
fi

if [[ ! -x "$UNITY_EXE" ]]; then
    echo "[UnityInvokeMac] Unity executable not found or not executable: $UNITY_EXE" >&2
    exit 127
fi

if ! pgrep -f '/Unity Hub\.app/Contents/MacOS/Unity Hub' >/dev/null 2>&1; then
    echo "[UnityInvokeMac] ERROR: Unity Hub is not running. Open Unity Hub, sign in, wait until idle, then rerun this command." >&2
    exit 65
fi

mkdir -p "$(dirname "$LOG_FILE")"

cleanup() {
    local exit_code=$?
    exit "$exit_code"
}
trap cleanup EXIT INT TERM

kill_tree() {
    local pid="$1"
    local children
    children="$(pgrep -P "$pid" 2>/dev/null || true)"
    for child in $children; do
        kill_tree "$child"
    done
    kill "$pid" 2>/dev/null || true
}

echo "[UnityInvokeMac] LicensingMode: gui (batchmode disabled on macOS)"
echo "[UnityInvokeMac] UnityExe: $UNITY_EXE"
echo "[UnityInvokeMac] ProjectPath: $PROJECT_PATH"
echo "[UnityInvokeMac] LogFile: $LOG_FILE"
echo "[UnityInvokeMac] TimeoutSeconds: $TIMEOUT_SECONDS"
echo "[UnityInvokeMac] Arguments: -projectPath $PROJECT_PATH -logFile $LOG_FILE $*"

"$UNITY_EXE" -projectPath "$PROJECT_PATH" -logFile "$LOG_FILE" "$@" &
unity_pid=$!
elapsed_seconds=0

while kill -0 "$unity_pid" 2>/dev/null; do
    if [[ "$TIMEOUT_SECONDS" -gt 0 && "$elapsed_seconds" -ge "$TIMEOUT_SECONDS" ]]; then
        echo "[UnityInvokeMac] ERROR: Unity timed out after ${TIMEOUT_SECONDS}s. Killing PID $unity_pid."
        kill_tree "$unity_pid"
        wait "$unity_pid" 2>/dev/null || true
        exit 124
    fi
    sleep 1
    elapsed_seconds=$((elapsed_seconds + 1))
done

wait "$unity_pid"
