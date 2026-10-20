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
    if len(found) != 1 or any(not found[0].get(k) for k in ("Name", "ExecutablePath", "CommandLine", "CreationDate")):
        raise ValueError("Owned PID identity unavailable; not evidence of absence")
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
                                      "chatgpt.exe", "codex.exe", "explorer.exe", "unitypackagemanager.exe"}:
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


def low(ram, commit, ram_floor=RAM_FLOOR, commit_floor=COMMIT_FLOOR):
    return ram < ram_floor or commit < commit_floor

def authorization():
    state = json.loads((ROOT / "Design/AgentReports/SkirmishExpansion/PRODUCTION_STATE.json").read_bytes())
    return state["execution_policy"]["owned_validation_threshold_cancellation"].get("authorized") is True

def select_reserves(owner, override_id=None, policy=None, now=None):
    """A lower reserve requires an unexpired grant bound to this exact owned process."""
    if override_id is None:
        return RAM_FLOOR, COMMIT_FLOOR
    if policy is None:
        state = json.loads((ROOT / "Design/AgentReports/SkirmishExpansion/PRODUCTION_STATE.json").read_bytes())
        policy = state["execution_policy"].get("one_run_reserve_override", {})
    now = now or datetime.datetime.now(datetime.timezone.utc)
    reserves = (policy.get("freeRamGiB"), policy.get("freeCommitGiB"))
    if (policy.get("id") != override_id or policy.get("authorized") is not True or
            policy.get("status") != "InUse" or
            policy.get("ownerIdentity") != list(identity(owner["process"])) or
            norm(policy.get("log", "")) != norm(owner["log"]) or
            policy.get("executeMethod") != owner.get("executeMethod") or
            any(type(value) is not int for value in reserves) or
            reserves not in ((4, 4), (8, 8), (8, 12))):
        raise ValueError("No matching single-run reserve authorization")
    expires = datetime.datetime.fromisoformat(policy["expiresUtc"])
    if expires.tzinfo is None or now >= expires:
        raise ValueError("Single-run reserve authorization expired")
    return reserves[0] * 1024**3, reserves[1] * 1024**3


def write(path, value):
    with path.open("a", encoding="utf-8") as stream:
        stream.write(json.dumps(dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(), **value)) + "\n")

PROTECTED = {'unitypackagemanager.exe', 'unity hub.exe', 'unity.licensing.client.exe'}

def cancellation_event(path, kind, **facts):
    with Path(path).open('a', encoding='utf-8') as f:
        f.write(json.dumps(dict(utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                                kind=kind, **facts)) + '\n')

def current(api, rows, owner):
    matches = [r for r in rows if r['ProcessId'] == owner['process']['ProcessId']]
    if not matches:
        return None
    if len(matches) != 1 or any(not matches[0].get(k) for k in
            ('Name', 'ExecutablePath', 'CommandLine', 'CreationDate')):
        raise ValueError('Owned PID identity unavailable; not evidence of absence')
    verified = api['verify'](rows, owner)
    if verified is None:
        raise ValueError('Present owned PID was not verified')
    return verified

def plan(api, rows, row):
    try:
        tree = api['descendants'](rows, row)
        protected = any(str(r.get('Name', '')).casefold() in PROTECTED for r in tree)
        return protected, 'Protected descendant' if protected else None
    except (ValueError, TypeError, KeyError, OSError, AttributeError) as error:
        # A forbidden child can never authorize /T. Only the separately verified root.
        return True, str(error)

def stop_owned(api, owner, output, reason, *, run=None, sleep=None):
    run = run or subprocess.run
    sleep = sleep or time.sleep
    # Same frozen owner verification supplied by the existing controller, twice fresh.
    rows = api['census']()
    row = current(api, rows, owner)
    if row is None:
        cancellation_event(output, 'AlreadyExitedBeforeRecovery', cancelled=False)
        return 0
    root_only, protected_reason = plan(api, rows, row)
    identity = api['identity'](row)
    rows = api['census']()
    row = current(api, rows, owner)
    if row is None:
        cancellation_event(output, 'ExitedBeforeCancellation', cancelled=False)
        return 0
    if api['identity'](row) != identity:
        raise ValueError('Owned identity changed before cancellation')
    fresh_root_only, fresh_reason = plan(api, rows, row)
    root_only = root_only or fresh_root_only
    protected_reason = fresh_reason or protected_reason
    if not api['authorization']():
        raise ValueError('Cancellation authorization revoked; no process stopped')
    command = ['rtk', 'proxy', 'taskkill.exe', '/PID', str(row['ProcessId'])]
    command += ([] if root_only else ['/T']) + ['/F']
    # Observation only: retain the full parent-linked tree even when the strict
    # descendant validator refuses it. These rows never authorize killing a child.
    observed = {row['ProcessId']}
    while True:
        expanded = observed | {r['ProcessId'] for r in rows if type(r.get('ProcessId')) is int
                               and r.get('ParentProcessId') in observed}
        if expanded == observed:
            break
        observed = expanded
    public_tree = [{k: r.get(k) for k in ('ProcessId', 'ParentProcessId', 'Name',
                    'ExecutablePath', 'CreationDate')} for r in rows if r.get('ProcessId') in observed]
    cancellation_event(output, 'CancellationIntent', reason=reason, pid=row['ProcessId'],
          identity=identity, rootOnly=root_only, protectedReason=protected_reason,
          command=command, observedTree=public_tree, missionReady=False)
    result = run(command, capture_output=True, text=True, timeout=30)
    cancellation_event(output, 'CancellationCommandResult', exit=result.returncode,
          stdout=result.stdout, stderr=result.stderr, rootOnly=root_only)
    # A successful command is not a shutdown observation. NULL CIM fields are unknown,
    # not absence. Require two fresh absent samples; never retry a kill using a PID alone.
    absent = 0
    for _ in range(20):
        rows = api['census']()
        matches = [r for r in rows if r['ProcessId'] == owner['process']['ProcessId']]
        absent = absent + 1 if not matches else 0
        if absent == 2:
            cancellation_event(output, 'OwnedRootShutdownVerified', commandExit=result.returncode,
                  cancelled=True, rootOnly=root_only, protectedProcessesNotTargeted=True,
                  descendantShutdownClaim=False, missionReady=False)
            return 3  # Resource-cancelled validation remains failed.
        sleep(.25)
    cancellation_event(output, 'OwnedRootShutdownPending', commandExit=result.returncode,
          rootOnly=root_only, missionReady=False)
    raise RuntimeError('Owned shutdown unverified after bounded fresh observations')


def watch(owner_path, output, reserve_override_id=None):
    owner = json.loads(owner_path.read_bytes())
    if norm(owner["project"]) != norm(ROOT) or not Path(owner["log"]).resolve().is_relative_to(ROOT / "Logs/SkirmishProduction"):
        raise ValueError("Owner scope outside this workspace")
    if not authorization():
        raise ValueError("Explicit threshold cancellation authorization required")
    initial = verify(census(), owner)
    if initial is None:
        write(output, {"kind": "AlreadyExited", "cancelled": False})
        return 0
    try:
        ram_floor, commit_floor = select_reserves(owner, reserve_override_id)
    except (ValueError, OSError, KeyError, TypeError) as error:
        # A malformed/revoked grant must never leave an already-started Editor unguarded.
        write(output, {"kind": "ReserveOverrideRefused", "requestedOverrideId": reserve_override_id,
                       "error": str(error), "fallbackRamGiB": 12, "fallbackCommitGiB": 16})
        reserve_override_id = None
        ram_floor, commit_floor = RAM_FLOOR, COMMIT_FLOOR
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.OpenProcess.argtypes = [ctypes.c_ulong, ctypes.c_int, ctypes.c_ulong]
    kernel.OpenProcess.restype = ctypes.c_void_p
    kernel.GetExitCodeProcess.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
    kernel.CloseHandle.argtypes = [ctypes.c_void_p]
    handle = kernel.OpenProcess(0x1000, False, initial["ProcessId"])
    if not handle:
        raise OSError(ctypes.get_last_error(), "Cannot observe owned process")
    write(output, {"kind": "Armed", "pid": initial["ProcessId"], "ramFloorGiB": ram_floor / 1024**3,
                   "commitFloorGiB": commit_floor / 1024**3, "reserveOverrideId": reserve_override_id})
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
            if low(ram, commit, ram_floor, commit_floor):
                return stop_owned(globals(), owner, output, "Actual resource floor crossed")
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
