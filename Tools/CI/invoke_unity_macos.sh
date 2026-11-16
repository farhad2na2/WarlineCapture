#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="$(pwd)"
UNITY_EXE=""
LOG_FILE="/private/tmp/warline-unity-$(date +%Y%m%d-%H%M%S).log"
TIMEOUT_SECONDS=0
REUSE_EDITOR=0

usage() {
    cat <<'EOF'
Usage: Tools/CI/invoke_unity_macos.sh [options] -- [Unity arguments]

Options:
  --unity PATH       Unity executable path. Defaults to ProjectVersion.txt.
  --project PATH     Unity project path. Defaults to current directory.
  --log PATH         Unity log file path. Defaults under /private/tmp.
  --timeout SECONDS  Kill Unity if the command exceeds timeout.
  --reuse            Run executeMethod through this project's existing Pipeline Editor;
                     never launch or quit an Editor. Timeout fails without terminating it.

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
        --reuse)
            REUSE_EDITOR=1
            shift
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
    version="$(awk -F': ' '/m_EditorVersion:/ {gsub(/\r/, "", $2); print $2; exit}' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt")"
    UNITY_EXE="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity"
fi

if [[ ! -x "$UNITY_EXE" ]]; then
    echo "[UnityInvokeMac] Unity executable not found or not executable: $UNITY_EXE" >&2
    exit 127
fi

if ! pgrep -fl '/Applications/Unity Hub\.app/Contents/MacOS/Unity Hub' >/dev/null 2>&1; then
    echo "[UnityInvokeMac] ERROR: Unity Hub is not running. Open Unity Hub, sign in, wait until idle, then rerun this command." >&2
    exit 65
fi

mkdir -p "$(dirname "$LOG_FILE")"

if [[ "$REUSE_EDITOR" -eq 1 ]]; then
    if [[ "$TIMEOUT_SECONDS" -le 0 || "$#" -ne 2 || "$1" != "-executeMethod" ]]; then
        echo "[UnityInvokeMac] ERROR: --reuse requires a positive timeout and exactly -executeMethod <static-method>." >&2
        exit 64
    fi
    execute_method="$2"
    if [[ -e "$LOG_FILE" || -e "$LOG_FILE.exit" || -e "$LOG_FILE.dispatch.json" || -e "$LOG_FILE.dispatch.cs" ]]; then
        echo "[UnityInvokeMac] ERROR: --reuse requires a new log path to preserve prior evidence." >&2
        exit 64
    fi
    dispatch_source="$LOG_FILE.dispatch.cs"
    # Preserve the exact owned callback as exclusive evidence. run_script has a
    # distinct millisecond parameter; eval's timeout collides with CLI HTTP timeout.
    python3 - "$execute_method" "$LOG_FILE" "$dispatch_source" <<'PYCODE'
import json,sys
code = 'public static class WarlineExistingValidationDispatch { public static string Run() { double runAfter=UnityEditor.EditorApplication.timeSinceStartup+10; UnityEditor.EditorApplication.CallbackFunction callback=null; callback=() => { if(UnityEditor.EditorApplication.timeSinceStartup<runAfter)return; UnityEditor.EditorApplication.update -= callback; Game.Editor.ExistingEditorValidation.Run('+json.dumps(sys.argv[1])+','+json.dumps(sys.argv[2])+'); }; UnityEditor.EditorApplication.update += callback; return "scheduled"; } }\n'
with open(sys.argv[3], 'x', encoding='utf-8') as stream:
    stream.write(code)
PYCODE
    dispatch_timeout_ms=30000
    if [[ "$TIMEOUT_SECONDS" -lt 30 ]]; then
        dispatch_timeout_ms=$((TIMEOUT_SECONDS * 1000))
    fi
    echo "[UnityInvokeMac] LicensingMode: existing GUI Editor; project=$PROJECT_PATH"
    echo "[UnityInvokeMac] Existing executeMethod: $execute_method; LogFile: $LOG_FILE; TimeoutSeconds: $TIMEOUT_SECONDS"
    # Pipeline 0.9 groups script commands and uses seconds for their timeout.
    # Keep the exact legacy dispatch contract for the pinned 0.6 package.
    dispatch_protocol="$(python3 - "$PROJECT_PATH" <<'PYCODE'
import json,re,sys
from pathlib import Path
root=Path(sys.argv[1])
lock=root/'Packages/packages-lock.json'
version=None
if lock.exists():
    version=json.loads(lock.read_text()).get('dependencies',{}).get('com.unity.pipeline',{}).get('version')
if version is None:
    version=json.loads((root/'Packages/manifest.json').read_text()).get('dependencies',{}).get('com.unity.pipeline')
match=re.fullmatch(r'(\d+)\.(\d+)\.(\d+)(?:[-+][A-Za-z0-9.+-]+)?',version or '')
if match is None:
    raise SystemExit('Cannot resolve the installed Pipeline dispatch version: '+str(version))
print('grouped' if tuple(map(int,match.groups())) >= (0,9,0) else 'legacy')
PYCODE
    )"
    dispatch_command=(run_script)
    dispatch_timeout_args=(--timeout_ms "$dispatch_timeout_ms" --timeout "$TIMEOUT_SECONDS")
    if [[ "$dispatch_protocol" == "grouped" ]]; then
        dispatch_command=(script run)
        dispatch_timeout_args=(--timeout "$((dispatch_timeout_ms / 1000))")
    fi
    # CLI targets the exact existing project. Failure never falls back to opening another Editor.
    live_started_seconds=$SECONDS
    dispatch_status=0
    dispatch_output="$(unity command "${dispatch_command[@]}" --project-path "$PROJECT_PATH" --file "$dispatch_source" --entry WarlineExistingValidationDispatch.Run "${dispatch_timeout_args[@]}" --format json)" || dispatch_status=$?
    printf '%s\n' "$dispatch_output" > "$LOG_FILE.dispatch.json"
    printf '%s\n' "$dispatch_output"
    # One monitor process retains the exact ownership/receipt checks and timeout.
    # Long native runs must not lose their monitor because a new `sleep` process
    # cannot be forked during temporary memory or process pressure.
    monitor_path="$(dirname "${BASH_SOURCE[0]}")/wait_existing_unity_validation.py"
    python3 "$monitor_path" "$LOG_FILE" "$PROJECT_PATH" "$execute_method" \
        "$((TIMEOUT_SECONDS - (SECONDS - live_started_seconds)))" "$dispatch_status"
    exit "$?"
fi

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
