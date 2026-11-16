"""Launcher/protocol controls only; no Unity or mission simulation is executed."""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

spec = importlib.util.spec_from_file_location(
    "validation_player_runner", Path(__file__).parents[1] / "run_skirmish_validation_player.py")
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class PlayerRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.player = self.root / "build/Test.app/Contents/MacOS/TestPlayer"
        self.player.parent.mkdir(parents=True)
        self.player.write_bytes(b"unit-test-only executable fixture, never launched")
        self.player.chmod(0o700)
        self.receipt = self.root / "build-receipt.json"
        self.build = dict(schema_version=1, kind="SkirmishValidationPlayerBuild",
                          status="Passed", validation_define="WARLINE_VALIDATION_PLAYER",
                          build_guid="test-build-guid", build_root=str(self.root / "build/Test.app"), player_executable=str(self.player),
                          player_sha256=hashlib.sha256(self.player.read_bytes()).hexdigest())
        self.build["package_files"] = [dict(path=str(self.player), size=self.player.stat().st_size,
                                          sha256=self.build["player_sha256"])]

    def tearDown(self):
        self.temp.cleanup()

    def write_build(self):
        self.receipt.write_text(json.dumps(self.build))

    @staticmethod
    def terminal(mode="Mission"):
        return dict(schema_version=1, kind="SkirmishValidationPlayerResult", mode=mode,
                    catalog="S002", seed=104731, status="Passed",
                    full_mission=mode == "Mission", terminal_outcome="Victory", build_guid="test-build-guid")

    def check_terminal(self, result, mode="Mission", exit_code=0, log=None):
        if log is None:
            log = "[SkirmishValidationPlayer] result=Passed mode=" + mode
        runner.validate_terminal(result, log, exit_code, mode, "S002", 104731, "test-build-guid")

    def test_successful_build_pins_exact_executable(self):
        self.write_build()
        player, _ = runner.load_build_receipt(self.receipt)
        self.assertEqual(self.player.resolve(), player)

    def test_changed_executable_rejected(self):
        self.write_build()
        self.player.write_bytes(b"changed after build")
        with self.assertRaisesRegex(ValueError, "changed"):
            runner.load_build_receipt(self.receipt)

    def test_shipping_or_failed_build_rejected(self):
        for field, value in (("validation_define", ""), ("status", "Failed")):
            with self.subTest(field=field):
                original = self.build[field]
                self.build[field] = value
                self.write_build()
                with self.assertRaises(ValueError):
                    runner.load_build_receipt(self.receipt)
                self.build[field] = original

    def test_editor_target_rejected(self):
        self.build["player_executable"] = str(self.player.rename(self.player.with_name("Unity")))
        self.write_build()
        with self.assertRaisesRegex(ValueError, "Editor"):
            runner.load_build_receipt(self.receipt)

    def test_symlink_cannot_escape_recorded_build(self):
        outside = self.root / "outside"
        outside.write_bytes(self.player.read_bytes())
        outside.chmod(0o700)
        self.player.unlink()
        self.player.symlink_to(outside)
        self.write_build()
        with self.assertRaisesRegex(ValueError, "escapes"):
            runner.load_build_receipt(self.receipt)

    def test_genuine_terminal_vocabulary_and_smoke_scope(self):
        for outcome in ("Victory", "Defeat", "Draw"):
            result = self.terminal()
            result["terminal_outcome"] = outcome
            self.check_terminal(result)
        self.check_terminal(self.terminal("StartupSmoke"), mode="StartupSmoke")

    def test_startup_cannot_masquerade_as_complete_mission(self):
        result = self.terminal()
        result["full_mission"] = False
        with self.assertRaisesRegex(ValueError, "Startup evidence"):
            self.check_terminal(result)
        result = self.terminal("StartupSmoke")
        result["full_mission"] = True
        with self.assertRaisesRegex(ValueError, "disclaim"):
            self.check_terminal(result, mode="StartupSmoke")

    def test_failed_exit_missing_marker_and_wrong_identity_rejected(self):
        with self.assertRaisesRegex(ValueError, "code 2"):
            self.check_terminal(self.terminal(), exit_code=2)
        with self.assertRaisesRegex(ValueError, "marker"):
            self.check_terminal(self.terminal(), log="")
        for field, value in (("seed", 1), ("catalog", "S003"), ("mode", "StartupSmoke"),
                             ("status", "Failed"), ("kind", "OtherReceipt")):
            result = self.terminal()
            result[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.check_terminal(result)

    def test_abort_unknown_and_nonterminal_cannot_pass(self):
        for outcome in (None, "None", "Running", "Abort", "Invented", "TechnicalFailure"):
            result = self.terminal()
            result["terminal_outcome"] = outcome
            with self.subTest(outcome=outcome), self.assertRaises(ValueError):
                self.check_terminal(result)

    def test_stop_is_confined_to_owned_process_group(self):
        child = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(30)"],
                                 start_new_session=True)
        independent = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(30)"],
                                       start_new_session=True)
        try:
            runner.stop_owned_player(child)
            self.assertIsNotNone(child.poll())
            self.assertIsNone(independent.poll())
        finally:
            runner.stop_owned_player(child)
            runner.stop_owned_player(independent)

    @staticmethod
    def throughput():
        return dict(simulation_seconds=100, simulation_wall_seconds=5,
                    simulation_speed_ratio=20, step_seconds=1 / 60, steps=6000)

    def test_measured_speed_passes_and_realtime_fails(self):
        self.assertEqual(20, runner.validate_speed(self.throughput(), 10))
        result = self.throughput()
        result.update(simulation_wall_seconds=100, simulation_speed_ratio=1)
        with self.assertRaisesRegex(ValueError, "below required"):
            runner.validate_speed(result, 10)

    def test_unmeasured_or_fabricated_speed_rejected(self):
        for field, value in (("simulation_wall_seconds", 0), ("simulation_seconds", float("nan")),
                             ("simulation_speed_ratio", 200), ("steps", True)):
            result = self.throughput()
            result[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                runner.validate_speed(result, 10)
        with self.assertRaises(ValueError):
            runner.validate_speed({}, 10)

    def test_enormous_step_is_not_fast_simulation(self):
        result = self.throughput()
        result["step_seconds"] = 2
        with self.assertRaisesRegex(ValueError, "Large simulation steps"):
            runner.validate_speed(result, 10)

    def test_adjacent_gameplay_data_must_match_successful_build(self):
        data = self.player.parent.parent / "Resources/game-data"
        data.parent.mkdir()
        data.write_bytes(b"actual build fixture data")
        self.build["package_files"].append(dict(path=str(data), size=data.stat().st_size,
                                               sha256=hashlib.sha256(data.read_bytes()).hexdigest()))
        self.write_build()
        runner.load_build_receipt(self.receipt)
        data.write_bytes(b"stale or changed data")
        with self.assertRaisesRegex(ValueError, "package changed"):
            runner.load_build_receipt(self.receipt)

    def test_result_must_match_exact_player_build(self):
        result = self.terminal()
        result["build_guid"] = "some-other-build"
        with self.assertRaisesRegex(ValueError, "different player build"):
            self.check_terminal(result)

    def test_elapsed_cannot_exceed_recorded_normal_step_budget(self):
        result = self.throughput()
        result["steps"] = 1
        with self.assertRaisesRegex(ValueError, "step budget"):
            runner.validate_speed(result, 10)


class ThroughputWatchdogTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.trace = self.root / "trace.jsonl"

    def tearDown(self):
        self.temp.cleanup()

    def append(self, elapsed, wall, phase="Playing"):
        with self.trace.open("a") as stream:
            stream.write(json.dumps(dict(phase=phase, elapsed=elapsed, wallSeconds=wall)) + "\n")

    def test_loading_time_never_starts_speed_gate(self):
        watch = runner.ThroughputWatchdog(10)
        watch.poll(self.trace, 0)
        self.append(0, 100, "Preparing")
        watch.poll(self.trace, 100)
        watch.poll(self.trace, 1000)
        self.assertFalse(watch.snapshot()["observed_playing"])

    def test_real_slow_progress_rejected_only_after_warmup_window(self):
        watch = runner.ThroughputWatchdog(10)
        self.append(0, 100)
        watch.poll(self.trace, 0)
        self.append(4, 104)
        watch.poll(self.trace, 4.9)
        self.append(5, 105)
        watch.poll(self.trace, 5)
        self.append(7, 107)
        with self.assertRaisesRegex(ValueError, "Early measured.*1.000x"):
            watch.poll(self.trace, 7)
        self.assertEqual(1, watch.snapshot()["last_measurement"]["simulation_speed_ratio"])

    def test_missing_progress_after_playing_is_rejected(self):
        watch = runner.ThroughputWatchdog(10)
        self.append(3, 10)
        watch.poll(self.trace, 0)
        watch.poll(self.trace, 5)
        with self.assertRaisesRegex(ValueError, "progress 0.000s"):
            watch.poll(self.trace, 7)

    def test_high_throughput_and_completion_leave_terminal_gate_authority(self):
        watch = runner.ThroughputWatchdog(10)
        for now, elapsed in ((0, 0), (5, 100), (7, 140), (9, 180)):
            self.append(elapsed, now)
            watch.poll(self.trace, now)
        self.assertEqual(20, watch.snapshot()["last_measurement"]["simulation_speed_ratio"])
        self.append(180, 9, "Finished")
        watch.poll(self.trace, 100)
        self.assertTrue(watch.snapshot()["finished_observed"])

    def test_partial_jsonl_is_buffered_without_fabricating_progress(self):
        watch = runner.ThroughputWatchdog(10)
        self.trace.write_bytes(b'{"phase":"Play')
        watch.poll(self.trace, 0)
        self.assertFalse(watch.snapshot()["observed_playing"])
        with self.trace.open("ab") as stream:
            stream.write(b'ing","elapsed":0,"wallSeconds":10}\n')
        watch.poll(self.trace, 1)
        self.assertTrue(watch.snapshot()["observed_playing"])
        self.append(100, 15)
        watch.poll(self.trace, 6)
        with self.trace.open("ab") as stream:
            stream.write(b'{"phase":"Playing","elapsed":140,"wallSeconds":17}')
        watch.poll(self.trace, 7)
        with self.trace.open("ab") as stream:
            stream.write(b'\n')
        watch.poll(self.trace, 8)
        self.assertEqual(20, watch.snapshot()["last_measurement"]["simulation_speed_ratio"])

    def test_poll_loop_waits_quarter_seconds_and_honors_exit_and_timeout(self):
        class Process:
            args = ["control-only-never-launched"]
            returncode = None
            def __init__(self, exit_after=None):
                self.now, self.waits, self.exit_after = 0., [], exit_after
            def poll(self):
                return self.returncode
            def wait(self, timeout):
                self.waits.append(timeout)
                self.now += timeout
                if self.exit_after is not None and len(self.waits) >= self.exit_after:
                    self.returncode = 0
                    return 0
                raise subprocess.TimeoutExpired(self.args, timeout)
        process = Process(exit_after=2)
        runner.wait_for_player(process, 1, runner.ThroughputWatchdog(10), self.trace, lambda: process.now)
        self.assertEqual([.25, .25], process.waits)
        process = Process()
        with self.assertRaises(subprocess.TimeoutExpired):
            runner.wait_for_player(process, 1, runner.ThroughputWatchdog(10), self.trace, lambda: process.now)
        self.assertEqual([.25] * 4, process.waits)

    def test_early_watchdog_failure_reaches_existing_owned_cleanup(self):
        receipt = self.root / "build.json"
        receipt.write_text("{}")
        output = self.root / "evidence"
        player = self.root / "control-only-never-launched"
        clock = [0.]
        class Process:
            args = [str(player)]
            pid = 123
            returncode = None
            def poll(self):
                return self.returncode
            def wait(self, timeout):
                clock[0] += timeout
                with (output / "trace.jsonl").open("a") as stream:
                    stream.write(json.dumps(dict(phase="Playing", elapsed=clock[0], wallSeconds=clock[0])) + "\n")
                raise subprocess.TimeoutExpired(self.args, timeout)
        process = Process()
        def launch(*args, **kwargs):
            self.assertTrue(kwargs["start_new_session"])
            (output / "trace.jsonl").write_text(json.dumps(dict(phase="Playing", elapsed=0, wallSeconds=0)) + "\n")
            return process
        def stop(owned):
            self.assertIs(owned, process)
            process.returncode = -15
        with mock.patch.object(runner, "load_build_receipt", return_value=(player, dict(player_sha256="test", build_guid="test"))), \
             mock.patch.object(runner.subprocess, "Popen", side_effect=launch), \
             mock.patch.object(runner.time, "monotonic", side_effect=lambda: clock[0]), \
             mock.patch.object(runner, "stop_owned_player", side_effect=stop) as cleanup:
            code = runner.main(["--build-receipt", str(receipt), "--output", str(output), "--mode", "StartupSmoke"])
        result = json.loads((output / "runner-result.json").read_text())
        self.assertEqual(2, code)
        self.assertEqual("Failed", result["status"])
        self.assertIn("Early measured", result["reason"])
        self.assertLessEqual(result["wall_seconds"], 7.25)
        cleanup.assert_called_once_with(process)
        self.assertEqual(-15, result["player_exit"])

    def test_timeout_still_reaches_owned_cleanup_and_failure_receipt(self):
        receipt = self.root / "build.json"
        receipt.write_text("{}")
        output = self.root / "timeout-evidence"
        process = mock.Mock(returncode=-15, pid=123)
        with mock.patch.object(runner, "load_build_receipt", return_value=(self.root / "not-launched", dict(player_sha256="test", build_guid="test"))), \
             mock.patch.object(runner.subprocess, "Popen", return_value=process), \
             mock.patch.object(runner, "wait_for_player", side_effect=subprocess.TimeoutExpired("owned", 1)), \
             mock.patch.object(runner, "stop_owned_player") as cleanup:
            code = runner.main(["--build-receipt", str(receipt), "--output", str(output), "--timeout", "1"])
        self.assertEqual(124, code)
        self.assertEqual("Failed", json.loads((output / "runner-result.json").read_text())["status"])
        cleanup.assert_called_once_with(process)


if __name__ == "__main__":
    unittest.main()
