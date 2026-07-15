from __future__ import annotations

import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI.android_development_device_collection import (
    EVIDENCE_FILE_NAME,
    RECORDER_DEVICE_PATH,
    CollectionError,
    CommandResult,
    PreinstallInputs,
    SustainedCollection,
    assemble_evidence,
    collect_sustained,
    collect_startup_samples,
    install_and_verify,
    launch_argv,
    monitor_sustained_run,
    parse_match_ready_markers,
    parse_recorder_complete_markers,
    sha256_file,
    validate_preinstall_inputs,
)
from Tools.CI.android_development_performance_gate import (
    DEFAULT_PROFILE,
    GateValidationError,
    load_profile,
    validate_evidence,
)


REVISION = "a" * 40


def result(arguments, stdout=b"", stderr=b"", returncode=0):
    if isinstance(stdout, str):
        stdout = stdout.encode("utf-8")
    if isinstance(stderr, str):
        stderr = stderr.encode("utf-8")
    return CommandResult(tuple(arguments), returncode, stdout, stderr)


def png_header(width: int, height: int) -> bytes:
    return (
        b"\x89PNG\r\n\x1a\n"
        + (13).to_bytes(4, "big")
        + b"IHDR"
        + width.to_bytes(4, "big")
        + height.to_bytes(4, "big")
        + b"\x08\x06\x00\x00\x00"
        + b"\x00\x00\x00\x00"
    )


def thermal_snapshot(phase: str) -> dict:
    return {
        "phase": phase,
        "status": 0,
        "coolingDevices": [{"name": "cpu", "value": 0}],
        "temperatures": [{"name": "cpu", "valueC": 40.0}],
    }


def thermal_output() -> str:
    return """Thermal Status: 0
Current temperatures from HAL:
    Temperature{mValue=39.5, mType=0, mName=cpu0, mStatus=0}
Current cooling devices from HAL:
    CoolingDevice{mValue=0, mType=2, mName=thermal-cpufreq-0}
Temperature static thresholds from HAL:
    ignored
"""


class FakeRepository:
    def __init__(self, revision: str = REVISION, status: str = "") -> None:
        self.revision = revision
        self.status = status

    def head_revision(self, project_root: Path) -> str:
        return self.revision

    def status_porcelain(self, project_root: Path) -> str:
        return self.status


class FakeClock:
    def __init__(self) -> None:
        self.now = 0.0
        self.sleeps: list[float] = []

    def monotonic(self) -> float:
        return self.now

    def sleep(self, seconds: float) -> None:
        self.sleeps.append(seconds)
        self.now += seconds


class StartupAdb:
    def __init__(self) -> None:
        self.calls: list[tuple[str, ...]] = []

    def run(self, arguments, *, timeout=60.0, use_serial=True):
        arguments = tuple(arguments)
        self.calls.append(arguments)
        if arguments[:4] == ("shell", "pm", "clear", "com.warlinecapture.game"):
            return result(arguments, "Success\n")
        if arguments[:3] == ("shell", "am", "start"):
            return result(arguments, "Starting: Intent\nStatus: ok\nTotalTime: 500\n")
        if arguments[:2] == ("logcat", "-b") and "-d" in arguments:
            return result(arguments, "I Unity : [APH-803 MatchReady] realtimeMs=1234.500\n")
        return result(arguments)

    def start_logcat(self, output_path):
        raise AssertionError("startup sampling must not start continuous logcat")


class StaticSession:
    def __init__(self, text: str) -> None:
        self.text = text

    def read_text(self) -> str:
        return self.text

    def stop(self) -> None:
        pass


class MonitorAdb:
    def __init__(self, pids: list[int]) -> None:
        self.pids = pids
        self.index = 0

    def run(self, arguments, *, timeout=60.0, use_serial=True):
        arguments = tuple(arguments)
        if arguments[:3] == ("shell", "pidof", "com.warlinecapture.game"):
            index = min(self.index, len(self.pids) - 1)
            self.index += 1
            return result(arguments, f"{self.pids[index]}\n")
        if arguments[:4] == ("shell", "dumpsys", "activity", "activities"):
            return result(
                arguments,
                "mResumedActivity: ActivityRecord{abc u0 "
                "com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity t5}\n",
            )
        return result(arguments)

    def start_logcat(self, output_path):
        raise AssertionError


class InstallAdb:
    def __init__(self, apk_sha256: str, *, debuggable: bool = True) -> None:
        self.apk_sha256 = apk_sha256
        self.debuggable = debuggable
        self.calls: list[tuple[str, ...]] = []

    def run(self, arguments, *, timeout=60.0, use_serial=True):
        arguments = tuple(arguments)
        self.calls.append(arguments)
        if arguments[:5] == ("shell", "pm", "list", "packages", "--user"):
            return result(arguments)
        if arguments[:3] == ("install", "--no-streaming", "-t"):
            return result(arguments, "Success\n")
        if arguments[:4] == ("shell", "cmd", "package", "resolve-activity"):
            return result(
                arguments,
                "com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity\n",
            )
        if arguments[:3] == ("shell", "dumpsys", "package"):
            flag = "DEBUGGABLE HAS_CODE" if self.debuggable else "HAS_CODE"
            return result(
                arguments,
                "  codePath=/data/app/warline\n"
                "  primaryCpuAbi=arm64-v8a\n"
                f"  flags=[{flag}]\n",
            )
        if arguments[:3] == ("shell", "run-as", "com.warlinecapture.game"):
            return result(arguments, "uid=10234(u0_a234) gid=10234(u0_a234)\n")
        if arguments[:4] == ("shell", "pm", "path", "com.warlinecapture.game"):
            return result(arguments, "package:/data/app/warline/base.apk\n")
        if arguments[:3] == ("shell", "sha256sum", "/data/app/warline/base.apk"):
            return result(arguments, f"{self.apk_sha256}  /data/app/warline/base.apk\n")
        return result(arguments)

    def start_logcat(self, output_path):
        raise AssertionError


class StaleRecorderAdb:
    def __init__(self) -> None:
        self.calls: list[tuple[str, ...]] = []

    def run(self, arguments, *, timeout=60.0, use_serial=True):
        arguments = tuple(arguments)
        self.calls.append(arguments)
        if arguments == ("shell", "dumpsys", "thermalservice"):
            return result(arguments, thermal_output())
        if arguments[:4] == ("shell", "pm", "clear", "com.warlinecapture.game"):
            return result(arguments, "Success\n")
        if arguments == ("shell", "test", "!", "-e", RECORDER_DEVICE_PATH):
            return result(arguments, returncode=1)
        return result(arguments)

    def start_logcat(self, output_path):
        raise AssertionError("stale recorder must fail before logcat starts")


class AndroidDevelopmentDeviceCollectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.profile = load_profile(DEFAULT_PROFILE)

    def test_names_paths_markers_and_launch_are_aph803_only(self) -> None:
        argv = launch_argv(self.profile)
        self.assertEqual(
            (
                "shell", "am", "start", "-W", "-S",
                "-a", "android.intent.action.MAIN",
                "-c", "android.intent.category.LAUNCHER",
                "-n", "com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity",
                "--es", "unity",
                "'-warlineAutoStartMatch -warlineProfilerMarkers "
                "-warlineAndroidPerformanceGate'",
            ),
            argv,
        )
        self.assertNotIn("APH-804", argv[-1])
        self.assertIn("aph803_android_development_recorder.json", RECORDER_DEVICE_PATH)
        self.assertEqual([1234.5], parse_match_ready_markers("[APH-803 MatchReady] realtimeMs=1234.5"))
        self.assertEqual([], parse_match_ready_markers("[APH-804 MatchReady] realtimeMs=1234.5"))
        self.assertEqual([True], parse_recorder_complete_markers("[APH-803 Recorder] complete=1"))
        self.assertEqual([], parse_recorder_complete_markers("[APH-804 Recorder] complete=1"))

    def test_preinstall_requires_exact_clean_head_before_adb(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            apk = root / self.profile["build"]["apkPath"]
            apk.parent.mkdir(parents=True)
            apk.write_bytes(b"development-apk")
            clean = validate_preinstall_inputs(
                root,
                apk,
                DEFAULT_PROFILE,
                self.profile["device"]["serial"],
                REVISION,
                FakeRepository(),
            )
            self.assertEqual(hashlib.sha256(b"development-apk").hexdigest(), clean.apk_sha256)
            with self.assertRaisesRegex(CollectionError, "Git HEAD mismatch"):
                validate_preinstall_inputs(
                    root, apk, DEFAULT_PROFILE, self.profile["device"]["serial"], REVISION,
                    FakeRepository("b" * 40),
                )
            with self.assertRaisesRegex(CollectionError, "must be clean"):
                validate_preinstall_inputs(
                    root, apk, DEFAULT_PROFILE, self.profile["device"]["serial"], REVISION,
                    FakeRepository(status=" M tracked.txt\n"),
                )

    def test_five_cold_and_five_warm_starts_are_deterministic(self) -> None:
        adb = StartupAdb()
        cold, warm = collect_startup_samples(adb, FakeClock(), self.profile)
        clears = [call for call in adb.calls if call[:3] == ("shell", "pm", "clear")]
        launches = [call for call in adb.calls if call[:3] == ("shell", "am", "start")]
        stops = [call for call in adb.calls if call[:3] == ("shell", "am", "force-stop")]
        self.assertEqual([1234.5] * 5, cold)
        self.assertEqual([1234.5] * 5, warm)
        self.assertEqual(5, len(clears))
        self.assertEqual(10, len(launches))
        self.assertEqual(20, len(stops))

    def test_install_requires_debuggable_arm64_and_device_hash_match(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            apk = Path(directory) / "WarlineCapture-Profiler.apk"
            apk.write_bytes(b"development-apk")
            digest = sha256_file(apk)
            install_and_verify(InstallAdb(digest), apk, digest, self.profile)
            with self.assertRaisesRegex(CollectionError, "must be debuggable"):
                install_and_verify(
                    InstallAdb(digest, debuggable=False),
                    apk,
                    digest,
                    self.profile,
                )
            with self.assertRaisesRegex(CollectionError, "does not match"):
                install_and_verify(
                    InstallAdb("b" * 64),
                    apk,
                    digest,
                    self.profile,
                )

    def test_stale_aph803_recorder_fails_before_logcat(self) -> None:
        adb = StaleRecorderAdb()
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(CollectionError, "stale external APH-803"):
                collect_sustained(adb, FakeClock(), self.profile, Path(directory))
        self.assertIn(("shell", "test", "!", "-e", RECORDER_DEVICE_PATH), adb.calls)

    def test_monitor_fails_on_pid_change(self) -> None:
        profile = copy.deepcopy(self.profile)
        profile["capture"]["warmupSeconds"] = 1
        profile["capture"]["sustainedSampleSeconds"] = 2
        with self.assertRaisesRegex(CollectionError, "PID changed"):
            monitor_sustained_run(
                MonitorAdb([124]),
                FakeClock(),
                StaticSession("[APH-803 MatchReady] realtimeMs=1000.0\n"),
                profile,
                123,
                0.0,
                lambda: thermal_snapshot("during"),
            )

    def test_monitor_rejects_early_or_failed_recorder_marker(self) -> None:
        profile = copy.deepcopy(self.profile)
        profile["capture"]["warmupSeconds"] = 1
        profile["capture"]["sustainedSampleSeconds"] = 2
        for marker, reason in (
            ("[APH-803 Recorder] complete=0", "incomplete"),
            ("[APH-803 Recorder] complete=1", "before the required duration"),
        ):
            with self.subTest(marker=marker):
                with self.assertRaisesRegex(CollectionError, reason):
                    monitor_sustained_run(
                        MonitorAdb([123]),
                        FakeClock(),
                        StaticSession(marker),
                        profile,
                        123,
                        0.0,
                        lambda: thermal_snapshot("during"),
                    )

    def test_assembled_evidence_passes_existing_gate_after_limits_are_approved(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            profile = copy.deepcopy(self.profile)
            profile["capture"]["minimumFrameSamples"] = 5
            profile["limits"]["p99FrameMs"] = {
                "comparison": "lessThanOrEqual", "value": 33.0, "status": "tracked-budget"
            }
            profile["limits"]["startupP95Ms"] = {
                "comparison": "lessThanOrEqual", "value": 1500.0, "status": "tracked-budget"
            }
            apk = root / profile["build"]["apkPath"]
            apk.parent.mkdir(parents=True)
            apk.write_bytes(b"development-apk")
            output = root / "TestResults/aph803-device"
            output.mkdir(parents=True)
            frames = [20.0, 21.0, 22.0, 23.0, 32.0]
            sustained_run = {
                "source": "structured-per-frame-recorder",
                "startupFramesExcluded": True,
                "warmupSeconds": 60.0,
                "sampleDurationSeconds": 600.0,
                "frameTimesMs": frames,
                "averageFrameMs": sum(frames) / len(frames),
                "p95FrameMs": 32.0,
                "p99FrameMs": 32.0,
                "maximumFrameMs": 32.0,
                "p95CpuFrameMs": 20.0,
                "p95GpuFrameMs": 24.0,
                "peakAllocatedMemoryMB": 900.0,
                "peakMonoMemoryMB": 32.0,
            }
            recorder = {
                "schemaVersion": 1,
                "taskId": "APH-803",
                "complete": True,
                "failure": "",
                "launchRealtimeSeconds": 0.25,
                "matchReadyRealtimeSeconds": 1.25,
                "processToMatchReadyMs": 1000.0,
                "cpuTimingSampleCount": 5,
                "gpuTimingSampleCount": 5,
                "sustainedRun": sustained_run,
            }
            recorder_path = output / "recorder.json"
            recorder_path.write_text(json.dumps(recorder), encoding="utf-8")
            raw_log = output / "device.log"
            raw_log.write_text(
                "[APH-803 MatchReady] realtimeMs=1000.0\n"
                "[APH-803 Recorder] complete=1 samples=5 duration=600.0s\n",
                encoding="utf-8",
            )
            screenshot = output / "match.png"
            screenshot.write_bytes(
                png_header(profile["device"]["resolutionWidth"], profile["device"]["resolutionHeight"])
            )
            inputs = PreinstallInputs(root, apk, sha256_file(apk), profile, REVISION)
            evidence = assemble_evidence(
                inputs,
                copy.deepcopy(profile["device"]),
                [1000.0, 1100.0, 1200.0, 1300.0, 1400.0],
                [500.0, 600.0, 700.0, 800.0, 900.0],
                SustainedCollection(
                    [thermal_snapshot(phase) for phase in ("before", "during", "after")],
                    recorder_path,
                    raw_log,
                    screenshot,
                    True,
                ),
            )
            value = validate_evidence(
                evidence,
                profile,
                expected_revision=REVISION,
                expected_apk_sha256=inputs.apk_sha256,
                artifact_root=root,
            )
            self.assertEqual("Passed", value["result"])
            self.assertEqual(5, value["coldStartSampleCount"])
            self.assertEqual(5, value["warmStartSampleCount"])
            self.assertEqual(sha256_file(recorder_path), evidence["artifacts"]["structuredRecorder"]["sha256"])
            self.assertEqual(sha256_file(raw_log), evidence["artifacts"]["rawDeviceLog"]["sha256"])

    def test_unset_limits_keep_collected_evidence_fail_closed(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            profile = copy.deepcopy(self.profile)
            profile["capture"]["minimumFrameSamples"] = 5
            apk = root / profile["build"]["apkPath"]
            apk.parent.mkdir(parents=True)
            apk.write_bytes(b"development-apk")
            output = root / "TestResults/aph803-device"
            output.mkdir(parents=True)
            frames = [20.0, 21.0, 22.0, 23.0, 32.0]
            sustained_run = {
                "source": "structured-per-frame-recorder",
                "startupFramesExcluded": True,
                "warmupSeconds": 60.0,
                "sampleDurationSeconds": 600.0,
                "frameTimesMs": frames,
                "averageFrameMs": sum(frames) / len(frames),
                "p95FrameMs": 32.0,
                "p99FrameMs": 32.0,
                "maximumFrameMs": 32.0,
                "p95CpuFrameMs": 20.0,
                "p95GpuFrameMs": 24.0,
                "peakAllocatedMemoryMB": 900.0,
                "peakMonoMemoryMB": 32.0,
            }
            recorder_path = output / "recorder.json"
            recorder_path.write_text(
                json.dumps({
                    "schemaVersion": 1, "taskId": "APH-803", "complete": True, "failure": "",
                    "launchRealtimeSeconds": 0.25, "matchReadyRealtimeSeconds": 1.25,
                    "processToMatchReadyMs": 1000.0, "cpuTimingSampleCount": 5,
                    "gpuTimingSampleCount": 5, "sustainedRun": sustained_run,
                }),
                encoding="utf-8",
            )
            raw_log = output / "device.log"
            raw_log.write_text(
                "[APH-803 MatchReady] realtimeMs=1000.0\n[APH-803 Recorder] complete=1\n",
                encoding="utf-8",
            )
            screenshot = output / "match.png"
            screenshot.write_bytes(
                png_header(profile["device"]["resolutionWidth"], profile["device"]["resolutionHeight"])
            )
            inputs = PreinstallInputs(root, apk, sha256_file(apk), profile, REVISION)
            evidence = assemble_evidence(
                inputs,
                copy.deepcopy(profile["device"]),
                [1000.0] * 5,
                [500.0] * 5,
                SustainedCollection(
                    [thermal_snapshot(phase) for phase in ("before", "during", "after")],
                    recorder_path,
                    raw_log,
                    screenshot,
                    True,
                ),
            )
            evidence_path = output / EVIDENCE_FILE_NAME
            evidence_path.write_text(json.dumps(evidence), encoding="utf-8")
            with self.assertRaisesRegex(GateValidationError, "p99 frame limit is unset"):
                validate_evidence(
                    evidence,
                    profile,
                    expected_revision=REVISION,
                    expected_apk_sha256=inputs.apk_sha256,
                    artifact_root=root,
                )
            self.assertTrue(evidence_path.is_file())

    def test_preinstall_rejects_wrong_pinned_apk_path(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            apk = root / "wrong.apk"
            apk.write_bytes(b"development-apk")
            with self.assertRaisesRegex(CollectionError, "exact non-empty artifact"):
                validate_preinstall_inputs(
                    root,
                    apk,
                    DEFAULT_PROFILE,
                    self.profile["device"]["serial"],
                    REVISION,
                    FakeRepository(),
                )


if __name__ == "__main__":
    unittest.main()
