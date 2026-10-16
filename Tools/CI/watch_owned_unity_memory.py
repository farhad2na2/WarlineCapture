"""Watch one explicitly owned Unity validation; never launch or restart Unity."""
import argparse, base64, ctypes, datetime, hashlib, json, os, subprocess, sys, time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PS = r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
RAM_FLOOR, COMMIT_FLOOR = 12 * 1024**3, 16 * 1024**3

def norm(value):
    return str(Path(value).resolve()).replace("/", "\\").casefold()

def argv(command):
    shell = ctypes.WinDLL("shell32", use_last_error=True)
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    shell.CommandLineToArgvW.argtypes = [ctypes.c_wchar_p, ctypes.POINTER(ctypes.c_int)]
    shell.CommandLineToArgvW.restype = ctypes.POINTER(ctypes.c_wchar_p)
    kernel.LocalFree.argtypes = [ctypes.c_void_p]
    count = ctypes.c_int()
    pointer = shell.CommandLineToArgvW(command, ctypes.byref(count))
    if not pointer:
        raise OSError(ctypes.get_last_error(), "Cannot parse owned command")
    try:
        return [pointer[i] for i in range(count.value)]
    finally:
        kernel.LocalFree(pointer)

def option(tokens, name):
    places = [i for i, token in enumerate(tokens) if token.casefold() == name.casefold()]
    if len(places) != 1 or places[0] + 1 >= len(tokens):
        raise ValueError("Exact single command option required: " + name)
    return tokens[places[0] + 1]

def created(row):
    return int(row["CreationDate"].removeprefix("/Date(").removesuffix(")/"))

def identity(row):
    return (row["ProcessId"], row["ParentProcessId"], norm(row["ExecutablePath"]),
            created(row), hashlib.sha256(row["CommandLine"].encode()).hexdigest())

def verify(rows, owner):
    found = [r for r in rows if r["ProcessId"] == owner["process"]["ProcessId"]]
    if not found:
        return None
    row = found[0]
    if identity(row) != identity(owner["process"]):
        raise ValueError("Owned PID identity changed; no process stopped")
    tokens = argv(row["CommandLine"])
    if row["Name"].casefold() != "unity.exe" or norm(row["ExecutablePath"]) != norm(owner["editor"]):
        raise ValueError("Unexpected Editor executable")
    if norm(option(tokens, "-projectPath")) != norm(owner["project"]):
        raise ValueError("Foreign project")
    if norm(option(tokens, "-logFile")) != norm(owner["log"]):
        raise ValueError("Foreign log")
    if option(tokens, "-executeMethod") != owner["executeMethod"]:
        raise ValueError("Foreign executeMethod")
    return row

def descendants(rows, parent):
    selected = {parent["ProcessId"]: parent}
    changed = True
    while changed:
        changed = False
        for row in rows:
            ancestor = selected.get(row["ParentProcessId"])
            if row["ProcessId"] not in selected and ancestor and created(row) >= created(ancestor):
                selected[row["ProcessId"]] = row
                changed = True
    for row in selected.values():
        if row["Name"].casefold() in {"unity hub.exe", "unityhub.exe", "unity.licensing.client.exe",
                                      "chatgpt.exe", "codex.exe", "explorer.exe"}:
            raise ValueError("Protected process in proposed tree; no process stopped")
        if row is not parent and row["Name"].casefold() == "unity.exe":
            if norm(option(argv(row["CommandLine"]), "-projectPath")) != norm(ROOT):
                raise ValueError("Foreign Editor descendant; no process stopped")
    return list(selected.values())

def census():
    query = ("@(Get-CimInstance Win32_Process | Select-Object ProcessId,ParentProcessId,"
             "Name,ExecutablePath,CommandLine,CreationDate) | ConvertTo-Json -Compress")
    result = subprocess.run(["rtk", "proxy", PS, "-NoProfile", "-EncodedCommand",
                            base64.b64encode(query.encode("utf-16-le")).decode()],
                           capture_output=True, text=True, check=True, timeout=30)
    data = json.loads(result.stdout or "[]")
    return [data] if isinstance(data, dict) else data

class Memory(ctypes.Structure):
    _fields_ = [("length", ctypes.c_ulong), ("load", ctypes.c_ulong)] + [
        (n, ctypes.c_ulonglong) for n in ("total_ram", "free_ram", "total_commit",
        "free_commit", "total_virtual", "free_virtual", "extended")]

def memory(kernel):
    value = Memory()
    value.length = ctypes.sizeof(value)
    if not kernel.GlobalMemoryStatusEx(ctypes.byref(value)):
        raise OSError(ctypes.get_last_error(), "Memory observation failed")
    return value.free_ram, value.free_commit

class ProcessMemory(ctypes.Structure):
    _fields_ = [("cb", ctypes.c_ulong), ("PageFaultCount", ctypes.c_ulong)] + [
        (name, ctypes.c_size_t) for name in ("PeakWorkingSetSize", "WorkingSetSize",
        "QuotaPeakPagedPoolUsage", "QuotaPagedPoolUsage", "QuotaPeakNonPagedPoolUsage",
        "QuotaNonPagedPoolUsage", "PagefileUsage", "PeakPagefileUsage", "PrivateUsage")]


def process_memory(kernel, handle, expected_created):
    """Optional telemetry on the existing limited-query handle; never affect cancellation."""
    try:
        kernel.GetProcessTimes.argtypes = [ctypes.c_void_p] + [ctypes.POINTER(ctypes.c_ulonglong)] * 4
        kernel.GetProcessTimes.restype = ctypes.c_int
        times = [ctypes.c_ulonglong() for _ in range(4)]
        if not kernel.GetProcessTimes(handle, *(ctypes.byref(value) for value in times)):
            return {"telemetryUnavailable": "GetProcessTimes", "winError": ctypes.get_last_error()}
        observed_created = times[0].value // 10000 - 11644473600000
        if observed_created != expected_created:
            return {"telemetryUnavailable": "CreationIdentityMismatch"}
        kernel.K32GetProcessMemoryInfo.argtypes = [ctypes.c_void_p, ctypes.POINTER(ProcessMemory), ctypes.c_ulong]
        kernel.K32GetProcessMemoryInfo.restype = ctypes.c_int
        counters = ProcessMemory()
        counters.cb = ctypes.sizeof(counters)
        if not kernel.K32GetProcessMemoryInfo(handle, ctypes.byref(counters), counters.cb):
            return {"telemetryUnavailable": "K32GetProcessMemoryInfo", "winError": ctypes.get_last_error()}
        return {"workingSetBytes": counters.WorkingSetSize,
                "peakWorkingSetBytes": counters.PeakWorkingSetSize,
                "privateCommitBytes": counters.PrivateUsage}
    except (AttributeError, OSError) as error:
        return {"telemetryUnavailable": type(error).__name__, "error": str(error)}


def low(ram, commit):
    return ram < RAM_FLOOR or commit < COMMIT_FLOOR

def authorization():
    state = json.loads((ROOT / "Design/AgentReports/SkirmishExpansion/PRODUCTION_STATE.json").read_bytes())
    return state["execution_policy"]["owned_validation_threshold_cancellation"].get("authorized") is True

def write(path, value):
    with path.open("a", encoding="utf-8") as stream:
        stream.write(json.dumps(dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), **value)) + "\n")

def watch(owner_path, output):
    owner = json.loads(owner_path.read_bytes())
    if norm(owner["project"]) != norm(ROOT) or not Path(owner["log"]).resolve().is_relative_to(ROOT / "Logs/SkirmishProduction"):
        raise ValueError("Owner scope outside this workspace")
    if not authorization():
        raise ValueError("Explicit threshold cancellation authorization required")
    initial = verify(census(), owner)
    if initial is None:
        write(output, {"kind": "AlreadyExited", "cancelled": False})
        return 0
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_int, ctypes.c_ulong]
    kernel.OpenProcess.restype = ctypes.c_void_p
    kernel.GetExitCodeProcess.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    handle = kernel.OpenProcess(0x1000, False, initial["ProcessId"])
    if not handle:
        raise OSError(ctypes.get_last_error(), "Cannot observe owned process")
    write(output, {"kind": "Armed", "pid": initial["ProcessId"], "ramFloorGiB": 12, "commitFloorGiB": 16})
    try:
        while True:
            status = ctypes.c_ulong()
            if not kernel.GetExitCodeProcess(handle, ctypes.byref(status)):
                raise OSError(ctypes.get_last_error(), "Owned process status unavailable")
            if status.value != 259:
                write(output, {"kind": "Exited", "processExit": status.value, "cancelled": False})
                return 0
            ram, commit = memory(kernel)
            write(output, {"kind": "Resource", "freeRamBytes": ram, "freeCommitBytes": commit,
                           "ownedEditorMemory": process_memory(kernel, handle, created(initial))})
            if low(ram, commit):
                rows = census()
                current = verify(rows, owner)
                if current is None:
                    write(output, {"kind": "ExitedDuringObservation", "cancelled": False})
                    return 0
                tree = descendants(rows, current)
                if not authorization():
                    raise ValueError("Cancellation authorization revoked; no process stopped")
                # Recheck the exact root after the tree census, immediately before taskkill.
                latest = census()
                immediate = verify(latest, owner)
                if immediate is None:
                    write(output, {"kind": "ExitedBeforeCancellation", "cancelled": False})
                    return 0
                tree = descendants(latest, immediate)
                public = [{k: r.get(k) for k in ("ProcessId", "ParentProcessId", "Name", "ExecutablePath", "CreationDate")}
                          for r in tree]
                write(output, {"kind": "CancellationIntent", "tree": public, "missionReady": False})
                result = subprocess.run(["rtk", "proxy", "taskkill.exe", "/PID", str(current["ProcessId"]), "/T", "/F"],
                                        capture_output=True, text=True, timeout=30)
                write(output, {"kind": "CancelledAtResourceFloor", "exit": result.returncode,
                               "stdout": result.stdout, "stderr": result.stderr, "missionReady": False})
                return 3 if result.returncode == 0 else 2
            time.sleep(2)
    finally:
        kernel.CloseHandle(handle)

def self_test():
    project = str(ROOT)
    log = str(ROOT / "Logs/SkirmishProduction/memory-watch-selftest/unity.log")
    editor = r"D:\Program Files\Unity\6000.5.8f1\Editor\Unity.exe"
    command = f'"{editor}" -projectPath "{project}" -logFile "{log}" -executeMethod Test.Run'
    main = dict(ProcessId=100, ParentProcessId=90, Name="Unity.exe", ExecutablePath=editor,
                CreationDate="/Date(1000)/", CommandLine=command)
    owner = dict(process=main, project=project, log=log, editor=editor, executeMethod="Test.Run")
    assert verify([main], owner) is main
    assert verify([], owner) is None
    for change in ({"CreationDate": "/Date(1001)/"}, {"ParentProcessId": 91},
                   {"ExecutablePath": editor + ".foreign"}, {"CommandLine": command + " -different"}):
        try:
            verify([dict(main, **change)], owner)
        except ValueError:
            pass
        else:
            raise AssertionError("Foreign identity accepted")
    child = dict(ProcessId=101, ParentProcessId=100, Name="UnityShaderCompiler.exe",
                 ExecutablePath="compiler.exe", CreationDate="/Date(1001)/", CommandLine="")
    old = dict(child, ProcessId=102, CreationDate="/Date(999)/")
    assert len(descendants([main, child, old], main)) == 2
    try:
        descendants([main, dict(child, Name="Unity Hub.exe")], main)
    except ValueError:
        pass
    else:
        raise AssertionError("Protected descendant accepted")
    for rows, expected in (
        ([dict(main, Name="python.exe")], owner),
        ([main, dict(child, Name="Unity.exe", ExecutablePath=editor,
                     CommandLine=f'"{editor}" -projectPath "{ROOT.parent / "foreign"}"')], owner),
    ):
        try:
            root = verify(rows, expected)
            descendants(rows, root)
        except ValueError:
            pass
        else:
            raise AssertionError("Foreign process accepted")
    duplicate = dict(main, CommandLine=command + f' -projectPath "{project}"')
    try:
        verify([duplicate], dict(owner, process=duplicate))
    except ValueError:
        pass
    else:
        raise AssertionError("Duplicate ownership option accepted")
    assert not low(RAM_FLOOR, COMMIT_FLOOR)
    assert low(RAM_FLOOR - 1, COMMIT_FLOOR)
    assert low(RAM_FLOOR, COMMIT_FLOOR - 1)
    print("[OwnedUnityMemoryWatcherSelfTest] result=Passed controls=14 noProcessesStopped=1")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--owner", type=Path)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if os.name != "nt":
        parser.error("Windows only")
    if args.self_test:
        self_test()
    else:
        if not args.owner or not args.output or args.output.exists():
            parser.error("Fresh exclusive owner and output paths required")
        evidence = ROOT / "Logs/SkirmishProduction"
        if not args.owner.resolve().is_relative_to(evidence) or not args.output.resolve().is_relative_to(evidence):
            parser.error("Owner and output must remain inside persistent workspace evidence")
        try:
            sys.exit(watch(args.owner, args.output))
        except Exception as error:
            write(args.output, {"kind": "Refused", "error": str(error), "cancelled": False, "missionReady": False})
            raise
