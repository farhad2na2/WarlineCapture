from __future__ import annotations

import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI.android_release_device_collection import (
    BUILD_REPORT_RELATIVE_PATH,
    RECORDER_DEVICE_PATH,
    CollectionError,
    CommandResult,
    InstalledPackage,
    PreinstallInputs,
    SustainedCollection,
    _pull_verbatim,
    assemble_evidence,
    collect_device_identity,
    collect_startup_samples,
    collect_sustained,
    ensure_package_uninstalled,
    launch_argv,
    monitor_sustained_run,
    parse_battery,
    parse_du_bytes,
    parse_resolved_launcher,
    parse_thermal_snapshot,
    require_unplugged_battery,
    require_install_completion,
    run_collection,
    sha256_file,
)
from Tools.CI.android_release_performance_gate import (
    DEFAULT_PROFILE,
    GateValidationError,
    load_profile,
    validate_evidence,
)


REVISION = "a" * 40


def result(args, stdout=b"", stderr=b"", returncode=0):
    if isinstance(stdout, str):
        stdout = stdout.encode("utf-8")
    if isinstance(stderr, str):
        stderr = stderr.encode("utf-8")
    return CommandResult(tuple(args), returncode, stdout, stderr)


def valid_thermal() -> str:
    return """Thermal Status: 0
Cached temperatures:
    Temperature{mValue=-99.0, mType=0, mName=ignored-cache, mStatus=0}
Current temperatures from HAL:
    Temperature{mValue=39.5, mType=0, mName=cpu0, mStatus=0}
    Temperature{mValue=31.25, mType=2, mName=battery, mStatus=0}
Current cooling devices from HAL:
    CoolingDevice{mValue=0, mType=2, mName=thermal-cpufreq-0}
Temperature static thresholds from HAL:
    ignored
"""


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

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        self.calls.append(args)
        if args[:4] == ("shell", "pm", "clear", "com.warlinecapture.game"):
            return result(args, "Success\n")
        if args[:3] == ("shell", "am", "start"):
            return result(args, "Starting: Intent\nStatus: ok\nTotalTime: 500\n")
        if args[:2] == ("logcat", "-b") and "-d" in args:
            return result(args, "07-12 12:00:00.000 I Unity : [APH-804 MatchReady] realtimeMs=1234.500\n")
        return result(args)

    def start_logcat(self, output_path):
        raise AssertionError("startup collection must not start continuous logcat")


class StaticSession:
    def __init__(self, text: str) -> None:
        self.text = text
        self.stopped = False

    def read_text(self) -> str:
        return self.text

    def stop(self) -> None:
        self.stopped = True


class MonitorAdb:
    def __init__(self, pids: list[int]) -> None:
        self.pids = pids
        self.pid_index = 0
        self.calls: list[tuple[str, ...]] = []

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        self.calls.append(args)
        if args[:3] == ("shell", "pidof", "com.warlinecapture.game"):
            index = min(self.pid_index, len(self.pids) - 1)
            self.pid_index += 1
            return result(args, f"{self.pids[index]}\n")
        if args[:4] == ("shell", "dumpsys", "activity", "activities"):
            return result(
                args,
                "mResumedActivity: ActivityRecord{abc u0 "
                "com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity t5}\n",
            )
        return result(args)

    def start_logcat(self, output_path):
        raise AssertionError


class StaleRecorderAdb:
    def __init__(self) -> None:
        self.calls: list[tuple[str, ...]] = []

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        self.calls.append(args)
        if args == ("shell", "dumpsys", "battery"):
            return result(
                args,
                "AC powered: false\nUSB powered: false\nWireless powered: false\n"
                "Dock powered: false\nlevel: 80\n",
            )
        if args == ("shell", "dumpsys", "thermalservice"):
            return result(args, valid_thermal())
        if args[:3] == ("shell", "pm", "clear"):
            return result(args, "Success\n")
        if args[:3] == ("shell", "test", "!"):
            return result(args, returncode=1)
        return result(args)

    def start_logcat(self, output_path):
        raise AssertionError("stale recorder must fail before logcat starts")


class PullAdb:
    def __init__(self, payload: bytes) -> None:
        self.payload = payload
        self.calls: list[tuple[str, ...]] = []

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        self.calls.append(args)
        if args[0] == "pull":
            Path(args[2]).write_bytes(self.payload)
        return result(args)

    def start_logcat(self, output_path):
        raise AssertionError


class NoCallAdb:
    def __init__(self) -> None:
        self.calls: list[tuple[str, ...]] = []

    def run(self, args, *, timeout=60.0, use_serial=True):
        self.calls.append(tuple(args))
        raise AssertionError("pre-install mismatch must fail before any ADB command")

    def start_logcat(self, output_path):
        raise AssertionError


class PackageStateAdb:
    def __init__(self, listing: str, uninstall: CommandResult | None = None) -> None:
        self.listing = listing
        self.uninstall = uninstall
        self.calls: list[tuple[str, ...]] = []

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        self.calls.append(args)
        if args[:4] == ("shell", "pm", "list", "packages"):
            return result(args, self.listing)
        if args[:1] == ("uninstall",):
            if self.uninstall is None:
                raise AssertionError("unexpected uninstall")
            return self.uninstall
        raise AssertionError(f"unexpected ADB call: {args!r}")

    def start_logcat(self, output_path):
        raise AssertionError


class IdentityAdb:
    def __init__(self, profile: dict, soc_manufacturer: str) -> None:
        self.profile = profile
        self.properties = {
            "ro.product.manufacturer": profile["device"]["manufacturer"],
            "ro.product.model": profile["device"]["model"],
            "ro.product.device": profile["device"]["deviceCodeName"],
            "ro.build.version.release": profile["device"]["androidRelease"],
            "ro.soc.manufacturer": soc_manufacturer,
            "ro.soc.model": "MT6878",
            "ro.build.version.sdk": str(profile["device"]["sdkLevel"]),
        }

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        if args[:2] == ("shell", "getprop"):
            return result(args, f"{self.properties[args[2]]}\n")
        if args == ("shell", "wm", "size"):
            device = self.profile["device"]
            return result(
                args,
                f"Physical size: {device['resolutionWidth']}x{device['resolutionHeight']}\n",
            )
        raise AssertionError(f"unexpected identity command: {args!r}")

    def start_logcat(self, output_path):
        raise AssertionError


class AndroidReleaseDeviceCollectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.profile = load_profile(DEFAULT_PROFILE)

    def test_device_identity_accepts_soc_casing_and_emits_canonical_profile_value(self) -> None:
        actual = collect_device_identity(IdentityAdb(self.profile, "Mediatek"), self.profile)

        self.assertEqual(self.profile["device"], actual)

    def test_install_completion_accepts_empty_adb_acknowledgment_for_later_hash_proof(self) -> None:
        require_install_completion(result(("install",), stdout=""), "exact APK install")
        require_install_completion(result(("install",), stdout="Success\n"), "exact APK install")
        require_install_completion(
            result(("install",), stdout="Performing Push Install\nSuccess\n"),
            "exact APK install",
        )

        with self.assertRaisesRegex(CollectionError, "unexpected response"):
            require_install_completion(
                result(("install",), stdout="Performing Push Install"),
                "exact APK install",
            )

    def test_package_uninstall_is_idempotent_only_after_exact_package_query(self) -> None:
        package = self.profile["build"]["packageName"]
        absent = PackageStateAdb("")
        ensure_package_uninstalled(absent, package)
        self.assertEqual(1, len(absent.calls))

        present = PackageStateAdb(
            f"package:{package}\n",
            result(("uninstall", package), "Success\n"),
        )
        ensure_package_uninstalled(present, package)
        self.assertEqual(("uninstall", package), present.calls[-1])

        with self.assertRaisesRegex(CollectionError, "ambiguous"):
            ensure_package_uninstalled(
                PackageStateAdb(f"package:{package}\npackage:unexpected\n"),
                package,
            )
        with self.assertRaisesRegex(CollectionError, "package uninstall failed"):
            ensure_package_uninstalled(
                PackageStateAdb(
                    f"package:{package}\n",
                    result(("uninstall", package), "Failure\n", returncode=1),
                ),
                package,
            )

    def test_resolved_launcher_accepts_only_canonical_adb_formats(self) -> None:
        component = "com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity"
        summary = "priority=0 preferredOrder=0 match=0x108000 specificIndex=-1 isDefault=false"

        self.assertEqual(component, parse_resolved_launcher(f"{component}\n"))
        self.assertEqual(component, parse_resolved_launcher(f"{summary}\n{component}\n"))

        for malformed in (
            "",
            f"unexpected summary\n{component}\n",
            f"{summary}\n{component}\nextra\n",
        ):
            with self.subTest(malformed=malformed):
                with self.assertRaisesRegex(CollectionError, "not canonical"):
                    parse_resolved_launcher(malformed)

    def test_exact_launch_argv_has_one_unity_extra_with_all_tokens(self) -> None:
        argv = launch_argv(self.profile)
        self.assertEqual(
            (
                "shell", "am", "start", "-W", "-S",
                "-a", "android.intent.action.MAIN",
                "-c", "android.intent.category.LAUNCHER",
                "-n", "com.warlinecapture.game/com.unity3d.player.UnityPlayerGameActivity",
                "--es", "unity",
                "'-warlineAutoStartMatch -warlineAndroidPerformanceGate APH-804 "
                "-warlinePerformanceFrameRate 60'",
            ),
            argv,
        )
        self.assertEqual(1, argv.count("--es"))

    def test_thermal_parser_reads_only_current_hal_sections(self) -> None:
        snapshot = parse_thermal_snapshot(valid_thermal(), "during")
        self.assertEqual(0, snapshot["status"])
        self.assertEqual([39.5, 31.25], [item["valueC"] for item in snapshot["temperatures"]])
        self.assertEqual([{"name": "thermal-cpufreq-0", "value": 0}], snapshot["coolingDevices"])

    def test_thermal_parser_rejects_duplicate_nonfinite_negative_and_empty(self) -> None:
        duplicate = valid_thermal().replace(
            "Current cooling devices from HAL:",
            "    Temperature{mValue=40, mType=0, mName=cpu0, mStatus=0}\n"
            "Current cooling devices from HAL:",
        )
        nonfinite = valid_thermal().replace("mValue=39.5", "mValue=NaN")
        negative = valid_thermal().replace("mValue=39.5", "mValue=-1")
        empty = valid_thermal().replace(
            "    Temperature{mValue=39.5, mType=0, mName=cpu0, mStatus=0}\n"
            "    Temperature{mValue=31.25, mType=2, mName=battery, mStatus=0}\n",
            "",
        )
        for text in (duplicate, nonfinite, negative, empty):
            with self.subTest(text=text[:50]):
                with self.assertRaises(CollectionError):
                    parse_thermal_snapshot(text, "before")

    def test_battery_parser_rejects_any_charging_source(self) -> None:
        output = (
            "AC powered: false\nUSB powered: true\nWireless powered: false\n"
            "Dock powered: false\nlevel: 81\n"
        )
        self.assertEqual(81, parse_battery(output)["level"])
        with self.assertRaisesRegex(CollectionError, "unplugged"):
            require_unplugged_battery(output)

    def test_cold_runs_clear_and_warm_runs_do_not(self) -> None:
        adb = StartupAdb()
        cold, warm = collect_startup_samples(adb, FakeClock(), self.profile)
        clears = [call for call in adb.calls if call[:3] == ("shell", "pm", "clear")]
        launches = [call for call in adb.calls if call[:3] == ("shell", "am", "start")]
        force_stops = [call for call in adb.calls if call[:3] == ("shell", "am", "force-stop")]
        self.assertEqual([1234.5] * 5, cold)
        self.assertEqual([1234.5] * 5, warm)
        self.assertEqual(5, len(clears))
        self.assertEqual(10, len(launches))
        self.assertEqual(20, len(force_stops))

    def test_stale_external_recorder_is_rejected_after_clear(self) -> None:
        adb = StaleRecorderAdb()
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(CollectionError, "stale external"):
                collect_sustained(adb, FakeClock(), self.profile, Path(directory))
        stale_calls = [call for call in adb.calls if call[:3] == ("shell", "test", "!")]
        self.assertEqual(("shell", "test", "!", "-e", RECORDER_DEVICE_PATH), stale_calls[0])

    def test_pid_change_fails_continuity_monitor(self) -> None:
        session = StaticSession("[APH-804 MatchReady] realtimeMs=1000.0\n")
        with self.assertRaisesRegex(CollectionError, "PID changed"):
            monitor_sustained_run(
                MonitorAdb([124]),
                FakeClock(),
                session,
                self.profile,
                123,
                0.0,
                lambda: parse_thermal_snapshot(valid_thermal(), "during"),
                timeout_seconds=20,
                during_offset_seconds=10,
            )

    def test_sustained_monitor_times_out_without_device_sleep(self) -> None:
        clock = FakeClock()
        session = StaticSession("[APH-804 MatchReady] realtimeMs=1000.0\n")
        with self.assertRaisesRegex(CollectionError, "timed out"):
            monitor_sustained_run(
                MonitorAdb([123]),
                clock,
                session,
                self.profile,
                123,
                0.0,
                lambda: parse_thermal_snapshot(valid_thermal(), "during"),
                timeout_seconds=10,
                during_offset_seconds=5,
            )
        self.assertEqual(10.0, clock.now)

    def test_external_pull_uses_exact_path_and_preserves_bytes(self) -> None:
        payload = b"{\r\n  \"opaque\": true\r\n}\x00"
        adb = PullAdb(payload)
        with tempfile.TemporaryDirectory() as directory:
            destination = Path(directory) / "recorder.json"
            _pull_verbatim(adb, RECORDER_DEVICE_PATH, destination)
            self.assertEqual(payload, destination.read_bytes())
        self.assertEqual("pull", adb.calls[0][0])
        self.assertEqual(RECORDER_DEVICE_PATH, adb.calls[0][1])

    def test_installed_size_parser_requires_exact_code_path(self) -> None:
        path = "/data/app/~~token/com.warlinecapture.game-token"
        self.assertEqual(987654321, parse_du_bytes(f"987654321  {path}\n", path))
        with self.assertRaises(CollectionError):
            parse_du_bytes("987654321  /wrong/path\n", path)

    def test_preinstall_dirty_report_fails_before_adb(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            apk = root / self.profile["build"]["apkPath"]
            apk.parent.mkdir(parents=True)
            apk.write_bytes(b"release-apk")
            report_path = root / BUILD_REPORT_RELATIVE_PATH
            report_path.parent.mkdir(parents=True)
            report_path.write_text(
                json.dumps(self.build_report(apk, dirty=True)),
                encoding="utf-8",
            )
            adb = NoCallAdb()
            with self.assertRaisesRegex(CollectionError, "dirty mismatch"):
                run_collection(
                    project_root=root,
                    adb_path=Path("adb"),
                    serial=self.profile["device"]["serial"],
                    apk_path=apk,
                    build_report_path=report_path,
                    profile_path=DEFAULT_PROFILE,
                    output_dir=root / "TestResults/device",
                    expected_revision=REVISION,
                    adb=adb,
                    clock=FakeClock(),
                )
            self.assertEqual([], adb.calls)

    def build_report(self, apk: Path, *, dirty: bool = False) -> dict:
        return {
            "schemaVersion": 1,
            "taskId": "APH-500",
            "exactCommit": REVISION,
            "dirty": dirty,
            "status": "complete",
            "releaseBuildType": "release",
            "packageType": "APK",
            "buildTarget": "Android",
            "scriptingBackend": "IL2CPP",
            "targetArchitecture": "ARM64",
            "detailedBuildReport": True,
            "artifactPath": self.profile["build"]["apkPath"],
            "artifactSha256": sha256_file(apk),
            "artifactBytes": apk.stat().st_size,
        }

    def test_assembled_evidence_hashes_and_shape_pass_existing_gate(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            output = root / "TestResults/device"
            output.mkdir(parents=True)
            profile = copy.deepcopy(self.profile)
            profile["capture"]["minimumFrameSamples"] = 5
            apk = root / profile["build"]["apkPath"]
            apk.parent.mkdir(parents=True)
            apk_bytes = b"exact-release-apk"
            apk.write_bytes(apk_bytes)
            raw_log = output / "raw.log"
            raw_log.write_bytes(
                b"[APH-804 MatchReady] realtimeMs=1000.0\n"
                b"[APH-804 Recorder] complete=1 samples=5 duration=600.0s\n"
            )
            screenshot = output / "match.png"
            screenshot.write_bytes(
                png_header(
                    profile["device"]["resolutionWidth"],
                    profile["device"]["resolutionHeight"],
                )
            )
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
                "gc": {
                    "totalAllocatedBytes": 500,
                    "averageAllocatedBytesPerFrame": 100.0,
                    "collectionCount": 2,
                },
                "memory": {
                    "peakAllocatedMemoryMB": 800.0,
                    "peakMonoMemoryMB": 32.0,
                    "peakResidentSetMB": 1100.0,
                },
                "battery": {"startPercent": 80.0, "endPercent": 75.0, "drainPercent": 5.0},
                "counters": {
                    "cpuTimingSampleCount": 5,
                    "gpuTimingSampleCount": 5,
                    "averageCpuFrameMs": 12.0,
                    "p95CpuFrameMs": 20.0,
                    "averageGpuFrameMs": 14.0,
                    "p95GpuFrameMs": 22.0,
                    "averageBatches": 500.0,
                    "averageSetPassCalls": 40.0,
                    "averageTriangles": 100000.0,
                    "averageVertices": 150000.0,
                },
            }
            recorder = {
                "schemaVersion": 1,
                "taskId": "APH-804",
                "complete": True,
                "failure": "",
                "launchRealtimeSeconds": 0.25,
                "matchReadyRealtimeSeconds": 1.25,
                "processToMatchReadyMs": 1000.0,
                "cpuTimingSampleCount": 5,
                "gpuTimingSampleCount": 5,
                "sustainedRun": sustained_run,
                "recorderMode": "release-performance-evidence",
                "buildType": "release",
                "developmentBuild": False,
                "scriptDebugging": False,
                "profilerAttached": False,
                "profilerMarkersEnabled": False,
            }
            recorder_bytes = (json.dumps(recorder, indent=1) + "\n\n").encode("utf-8")
            recorder_path = output / "recorder.json"
            recorder_path.write_bytes(recorder_bytes)
            snapshots = [
                {
                    "phase": phase,
                    "status": 0,
                    "coolingDevices": [{"name": "cpu", "value": 0}],
                    "temperatures": [{"name": "cpu", "valueC": 40.0 + index}],
                }
                for index, phase in enumerate(("before", "during", "after"))
            ]
            inputs = PreinstallInputs(
                root,
                apk,
                hashlib.sha256(apk_bytes).hexdigest(),
                len(apk_bytes),
                profile,
                REVISION,
            )
            evidence = assemble_evidence(
                inputs,
                copy.deepcopy(profile["device"]),
                InstalledPackage("/data/app/base.apk", "/data/app/package", 7654321),
                [1000.0, 1100.0, 1200.0, 1300.0, 1400.0],
                [500.0, 600.0, 700.0, 800.0, 900.0],
                SustainedCollection(snapshots, recorder_path, raw_log, screenshot, True),
            )
            result_value = validate_evidence(
                evidence,
                profile,
                expected_revision=REVISION,
                expected_apk_sha256=inputs.apk_sha256,
                artifact_root=root,
            )
            self.assertEqual("Passed", result_value["result"])
            self.assertEqual(7654321, evidence["installedSizeBytes"])
            self.assertEqual(recorder_bytes, recorder_path.read_bytes())
            self.assertEqual(hashlib.sha256(recorder_bytes).hexdigest(), evidence["artifacts"]["structuredRecorder"]["sha256"])
            self.assertEqual(sha256_file(raw_log), evidence["artifacts"]["rawDeviceLog"]["sha256"])
            self.assertEqual(sha256_file(screenshot), evidence["artifacts"]["screenshot"]["sha256"])
            self.assertEqual(
                (profile["device"]["resolutionWidth"], profile["device"]["resolutionHeight"]),
                (evidence["artifacts"]["screenshot"]["width"], evidence["artifacts"]["screenshot"]["height"]),
            )

            screenshot.write_bytes(png_header(1220, 2712))
            evidence["artifacts"]["screenshot"]["sha256"] = sha256_file(screenshot)
            with self.assertRaisesRegex(GateValidationError, "PNG data"):
                validate_evidence(
                    evidence,
                    profile,
                    expected_revision=REVISION,
                    expected_apk_sha256=inputs.apk_sha256,
                    artifact_root=root,
                )


if __name__ == "__main__":
    unittest.main()
