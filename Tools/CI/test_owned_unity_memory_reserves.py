"""Exact-owned single-run reserve authorization and guard wiring; no real process stopped."""
import datetime
import json
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch
import ctypes
import watch_owned_unity_memory as g
import test_owned_unity_memory_telemetry as support


class ReserveTests(unittest.TestCase):
    def setUp(self):
        self.now = datetime.datetime.now(datetime.timezone.utc)
        self.row = dict(ProcessId=100, ParentProcessId=90, Name="Unity.exe",
            ExecutablePath=r"D:\Program Files\Unity\6000.5.8f1\Editor\Unity.exe",
            CreationDate="/Date(1000)/", CommandLine="owned normal M5")
        self.owner = dict(process=self.row, project=str(g.ROOT), executeMethod="M5.Run",
            log=str(g.ROOT / "Logs/SkirmishProduction/reserve-test/unity.log"))
        self.policy = dict(id="one-test", authorized=True, status="InUse",
            ownerIdentity=list(g.identity(self.row)), log=self.owner["log"],
            executeMethod="M5.Run", freeRamGiB=8, freeCommitGiB=12,
            expiresUtc=(self.now+datetime.timedelta(hours=1)).isoformat())

    def test_default_unchanged_without_grant(self):
        self.assertEqual(g.select_reserves(self.owner), (12*1024**3,16*1024**3))

    def test_exact_grant(self):
        self.assertEqual(g.select_reserves(self.owner,"one-test",self.policy,self.now),
                         (8*1024**3,12*1024**3))

    def test_other_or_reused_grants_refuse(self):
        for change in [dict(id="other"),dict(authorized=False),dict(status="Completed"),
            dict(status="Authorized"),dict(ownerIdentity=[]),dict(log=str(g.ROOT/"foreign.log")),
            dict(executeMethod="Other.Run"),dict(freeRamGiB=7),dict(freeCommitGiB=11)]:
            with self.subTest(change=change), self.assertRaises(ValueError):
                g.select_reserves(self.owner,"one-test",dict(self.policy,**change),self.now)

    def test_pid_reuse_refuses(self):
        owner=dict(self.owner,process=dict(self.row,CreationDate="/Date(1001)/"))
        with self.assertRaises(ValueError):
            g.select_reserves(owner,"one-test",self.policy,self.now)

    def test_expired_grant_refuses(self):
        for expires in [self.now.isoformat(),self.now.replace(tzinfo=None).isoformat()]:
            with self.subTest(expires=expires), self.assertRaises(ValueError):
                g.select_reserves(self.owner,"one-test",dict(self.policy,expiresUtc=expires),self.now)

    def test_scoped_floor_boundaries(self):
        ram,commit=8*1024**3,12*1024**3
        self.assertFalse(g.low(ram,commit,ram,commit))
        self.assertTrue(g.low(ram-1,commit,ram,commit))
        self.assertTrue(g.low(ram,commit-1,ram,commit))
        self.assertTrue(g.low(ram,commit)) # default12/16 still applies elsewhere

    def exercise_watch(self, ram, commit, cancel, invalid=False):
        with tempfile.TemporaryDirectory(dir=g.ROOT/"Temp") as folder:
            owner=Path(folder)/"owner.json";output=Path(folder)/"watch.jsonl"
            owner.write_text(json.dumps(self.owner))
            api=support.kernel(memory_ok=False)
            api.OpenProcess=support.Call(lambda *args:123)
            statuses=iter([259,0])
            api.GetExitCodeProcess=support.Call(lambda h,out:setattr(out._obj,"value",next(statuses)) or 1)
            closed=[];api.CloseHandle=support.Call(lambda h:closed.append(h))
            with patch.object(g,"census",return_value=[self.row]), \
                 patch.object(g,"verify",return_value=self.row), \
                 patch.object(g,"authorization",return_value=True), \
                 patch.object(g,"select_reserves",return_value=(8*1024**3,12*1024**3), side_effect=ValueError("invalid grant") if invalid else None) as selector, \
                 patch.object(ctypes,"WinDLL",return_value=api), \
                 patch.object(ctypes,"get_last_error",return_value=5), \
                 patch.object(g,"memory",return_value=(ram,commit)), \
                 patch.object(g.time,"sleep"), \
                 patch.object(g.subprocess,"run",return_value=SimpleNamespace(returncode=0,stdout="",stderr="")) as stop:
                self.assertEqual(g.watch(owner,output,"one-test"),3 if cancel else 0)
            selector.assert_called_once_with(self.owner,"one-test")
            self.assertEqual(stop.call_count,1 if cancel else 0)
            self.assertEqual(closed,[123])
            armed=next(row for row in map(json.loads,output.read_text().splitlines()) if row["kind"]=="Armed")
            self.assertEqual((armed["ramFloorGiB"],armed["commitFloorGiB"]),(12,16) if invalid else (8,12))
            self.assertEqual(armed["reserveOverrideId"],None if invalid else "one-test")

    def test_authorized_run_can_use_extra_headroom(self):
        self.exercise_watch(10*1024**3,14*1024**3,False)

    def test_invalid_override_falls_back_to_default_guard(self):
        self.exercise_watch(10*1024**3,14*1024**3,True,invalid=True)

    def test_authorized_run_still_cancels_at_new_floor(self):
        self.exercise_watch(8*1024**3-1,12*1024**3,True)


if __name__=="__main__":
    unittest.main()
