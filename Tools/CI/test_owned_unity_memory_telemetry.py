"""Read-only footprint telemetry and unchanged owned-guard controls (no Unity launch)."""
import ctypes
import json
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch
import watch_owned_unity_memory as guard


class Call:
    def __init__(self, fn):
        self.fn = fn
    def __call__(self, *args):
        return self.fn(*args)


def kernel(created=1000, times_ok=True, memory_ok=True):
    def times(handle, birth, *rest):
        birth._obj.value = (created + 11644473600000) * 10000
        return times_ok
    def footprint(handle, result, size):
        assert result._obj.cb == size == ctypes.sizeof(guard.ProcessMemory)
        result._obj.WorkingSetSize = 5 * 1024**3
        result._obj.PeakWorkingSetSize = 7 * 1024**3
        result._obj.PrivateUsage = 9 * 1024**3
        return memory_ok
    return SimpleNamespace(GetProcessTimes=Call(times), K32GetProcessMemoryInfo=Call(footprint))


class TelemetryTests(unittest.TestCase):
    def test_large_counters_and_semantics(self):
        result = guard.process_memory(kernel(), 123, 1000)
        self.assertEqual(result, dict(workingSetBytes=5*1024**3,
            peakWorkingSetBytes=7*1024**3, privateCommitBytes=9*1024**3))

    def test_creation_mismatch_does_not_read_memory(self):
        api = kernel(created=1001)
        api.K32GetProcessMemoryInfo = Call(lambda *args: self.fail("foreign handle read"))
        self.assertEqual(guard.process_memory(api, 123, 1000),
                         {"telemetryUnavailable": "CreationIdentityMismatch"})

    def test_query_denied(self):
        with patch.object(ctypes, "get_last_error", return_value=5):
            self.assertEqual(guard.process_memory(kernel(times_ok=False), 123, 1000),
                             dict(telemetryUnavailable="GetProcessTimes", winError=5))

    def test_memory_read_denied(self):
        with patch.object(ctypes, "get_last_error", return_value=5):
            self.assertEqual(guard.process_memory(kernel(memory_ok=False), 123, 1000),
                             dict(telemetryUnavailable="K32GetProcessMemoryInfo", winError=5))

    def test_missing_api_is_observational(self):
        self.assertEqual(guard.process_memory(SimpleNamespace(), 123, 1000)["telemetryUnavailable"],
                         "AttributeError")

    def test_guard_controls(self):
        guard.self_test()

    def test_unavailable_telemetry_preserves_floor_cancellation(self):
        with tempfile.TemporaryDirectory(dir=guard.ROOT / "Temp") as folder:
            owner = Path(folder) / "owner.json"
            output = Path(folder) / "watch.jsonl"
            row = dict(ProcessId=100, ParentProcessId=90, Name="Unity.exe",
                ExecutablePath=r"D:\Program Files\Unity\6000.5.8f1\Editor\Unity.exe",
                CreationDate="/Date(1000)/", CommandLine="")
            data = dict(process=row, project=str(guard.ROOT),
                log=str(guard.ROOT / "Logs/SkirmishProduction/telemetry-test/unity.log"))
            owner.write_text(json.dumps(data))
            api = kernel(memory_ok=False)
            api.OpenProcess = Call(lambda *args: 123)
            api.GetExitCodeProcess = Call(lambda h, out: setattr(out._obj, "value", 259) or 1)
            closed = []
            api.CloseHandle = Call(lambda h: closed.append(h))
            with patch.object(guard, "census", return_value=[row]), \
                 patch.object(guard, "verify", return_value=row), \
                 patch.object(guard, "authorization", return_value=True), \
                 patch.object(ctypes, "WinDLL", return_value=api), \
                 patch.object(ctypes, "get_last_error", return_value=5), \
                 patch.object(guard, "memory", return_value=(guard.RAM_FLOOR-1, guard.COMMIT_FLOOR)), \
                 patch.object(guard.subprocess, "run", return_value=SimpleNamespace(returncode=0, stdout="", stderr="")) as stop:
                self.assertEqual(guard.watch(owner, output), 3)
            self.assertEqual(closed, [123])
            self.assertEqual(stop.call_count, 1)
            rows = [json.loads(line) for line in output.read_text().splitlines()]
            self.assertEqual(rows[1]["ownedEditorMemory"]["telemetryUnavailable"], "K32GetProcessMemoryInfo")
            self.assertEqual(rows[-1]["kind"], "CancelledAtResourceFloor")


if __name__ == "__main__":
    unittest.main()
