#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="$(pwd)"
UNITY_EXE=""
LOG_FILE="/private/tmp/warline-unity-$(date +%Y%m%d-%H%M%S).log"
TIMEOUT_SECONDS=0
SKIP_RESET=0

usage() {
    cat <<'EOF'
Usage: Tools/CI/invoke_unity_macos.sh [options] -- [Unity arguments]

Options:
  --unity PATH       Unity executable path. Defaults to ProjectVersion.txt.
  --project PATH     Unity project path. Defaults to current directory.
  --log PATH         Unity log file path. Defaults under /private/tmp.
  --timeout SECONDS  Kill Unity and helper IPC if the command exceeds timeout.
  --skip-reset       Do not reset Unity helper IPC before/after the run.

The wrapper always supplies:
  -batchmode -projectPath <project> -logFile <log>

Pass remaining Unity arguments after --, for example:
  Tools/CI/invoke_unity_macos.sh --timeout 240 -- -quit -executeMethod MyTests.Run
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
        --skip-reset)
            SKIP_RESET=1
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

if [[ -z "$UNITY_EXE" ]]; then
    version="$(awk -F': ' '/m_EditorVersion:/ {print $2; exit}' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt")"
    UNITY_EXE="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity"
fi

if [[ ! -x "$UNITY_EXE" ]]; then
    echo "[UnityInvokeMac] Unity executable not found or not executable: $UNITY_EXE" >&2
    exit 127
fi

mkdir -p "$(dirname "$LOG_FILE")"

cleanup() {
    local exit_code=$?
if [[ "$SKIP_RESET" -eq 0 ]]; then
    "$PROJECT_PATH/Tools/CI/reset_unity_macos_ipc.sh" --quit-hub || true
fi
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

if [[ "$SKIP_RESET" -eq 0 ]]; then
    "$PROJECT_PATH/Tools/CI/reset_unity_macos_ipc.sh" --quit-hub
fi

echo "[UnityInvokeMac] UnityExe: $UNITY_EXE"
echo "[UnityInvokeMac] ProjectPath: $PROJECT_PATH"
echo "[UnityInvokeMac] LogFile: $LOG_FILE"
echo "[UnityInvokeMac] TimeoutSeconds: $TIMEOUT_SECONDS"
echo "[UnityInvokeMac] Arguments: -batchmode -projectPath $PROJECT_PATH -logFile $LOG_FILE $*"

"$UNITY_EXE" -batchmode -projectPath "$PROJECT_PATH" -logFile "$LOG_FILE" "$@" &
unity_pid=$!
started_at="$(date +%s)"

while kill -0 "$unity_pid" 2>/dev/null; do
    if [[ "$TIMEOUT_SECONDS" -gt 0 ]]; then
        now="$(date +%s)"
        elapsed=$((now - started_at))
        if [[ "$elapsed" -ge "$TIMEOUT_SECONDS" ]]; then
            echo "[UnityInvokeMac] ERROR: Unity timed out after ${TIMEOUT_SECONDS}s. Killing PID $unity_pid."
            kill_tree "$unity_pid"
            wait "$unity_pid" 2>/dev/null || true
            exit 124
        fi
    fi
    sleep 1
done

wait "$unity_pid"
