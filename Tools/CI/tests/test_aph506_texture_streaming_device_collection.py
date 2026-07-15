from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from Tools.CI.aph506_texture_streaming_device_collection import (
    AFTER_SCREENSHOT_FILE_NAME,
    BEFORE_SCREENSHOT_FILE_NAME,
    DEFAULT_SESSION_SECONDS,
    DEFAULT_WARMUP_SECONDS,
    EVIDENCE_FILE_NAME,
    IO_KEYS,
    PILOT_TEXTURE_PATHS,
    RAW_LOG_FILE_NAME,
    CollectionConfig,
    CollectionError,
    collect_sample,
    gesture_argv,
    gesture_offsets,
    parse_meminfo,
    parse_process_io,
    run_collection,
    scheduled_offsets,
    sha256_file,
    validate_config,
    validate_evidence,
    validate_preinstall_inputs,
)
from Tools.CI.android_development_performance_gate import DEFAULT_PROFILE, load_profile
from Tools.CI.android_release_device_collection import CommandResult


REVISION = "a" * 40
CAPTURE_ID = "b" * 32
CAPTURED_AT = "2026-07-15T12:30:00Z"
PID = 4312


def result(args, stdout=b"", stderr=b"", returncode=0):
    if isinstance(stdout, str):
        stdout = stdout.encode("utf-8")
    if isinstance(stderr, str):
        stderr = stderr.encode("utf-8")
    return CommandResult(tuple(args), returncode, stdout, stderr)


def png_header(width: int, height: int, suffix: bytes = b"") -> bytes:
    return (
        b"\x89PNG\r\n\x1a\n"
        + (13).to_bytes(4, "big")
        + b"IHDR"
        + width.to_bytes(4, "big")
        + height.to_bytes(4, "big")
        + b"\x08\x06\x00\x00\x00"
        + b"\x00\x00\x00\x00"
        + suffix
    )


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


def warned_thermal() -> str:
    return valid_thermal().replace(
        "mName=cpu0, mStatus=0",
        "mName=cpu0, mStatus=3",
    )


def valid_meminfo(pid: int = PID, package: str = "com.warlinecapture.game", value: int = 1000) -> str:
    return f"""Applications Memory Usage (in Kilobytes):
** MEMINFO in pid {pid} [{package}] **
 App Summary
            Graphics:      {value + 3}      {value + 4}
           TOTAL PSS:      {value}      TOTAL RSS:      {value + 1}      TOTAL SWAP PSS:      {value + 2}
"""


def valid_process_io(value: int = 1000) -> str:
    return "\n".join(f"{key}: {value + index}" for index, key in enumerate(IO_KEYS)) + "\n"


class FakeClock:
    def __init__(self) -> None:
        self.now = 0.0
        self.sleeps: list[float] = []

    def monotonic(self) -> float:
        return self.now

    def sleep(self, seconds: float) -> None:
        self.sleeps.append(seconds)
        self.now += seconds


class FakeRepository:
    def __init__(self, revision: str = REVISION, status: str = "") -> None:
        self.revision = revision
        self.status = status
        self.head_calls = 0
        self.status_calls = 0

    def head_revision(self, project_root: Path) -> str:
        self.head_calls += 1
        return self.revision

    def status_porcelain(self, project_root: Path) -> str:
        self.status_calls += 1
        return self.status


class StaticLogcatSession:
    def __init__(self, output_path: Path, text: str) -> None:
        self.output_path = output_path
        self.text = text
        self.stopped = False
        output_path.write_text(text, encoding="utf-8")

    def read_text(self) -> str:
        return self.text

    def stop(self) -> None:
        self.stopped = True


class NoCallAdb:
    def __init__(self) -> None:
        self.calls: list[tuple[str, ...]] = []

    def run(self, args, *, timeout=60.0, use_serial=True):
        self.calls.append(tuple(args))
        raise AssertionError("ADB must not run after a local fail-closed preflight")

    def start_logcat(self, output_path):
        raise AssertionError("ADB must not start logcat after a local fail-closed preflight")


class FullFakeAdb:
    def __init__(self, profile: dict, apk_sha256: str) -> None:
        self.profile = profile
        self.apk_sha256 = apk_sha256
        self.calls: list[tuple[str, ...]] = []
        self.io_samples = 0
        self.session: StaticLogcatSession | None = None
        self.pid = PID

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        self.calls.append(args)
        package = self.profile["build"]["packageName"]
        activity = self.profile["build"]["activity"]
        if args == ("devices", "-l"):
            return result(args, f"List of devices attached\n{self.profile['device']['serial']} device product:x model:y\n")
        properties = {
            "ro.product.manufacturer": self.profile["device"]["manufacturer"],
            "ro.product.model": self.profile["device"]["model"],
            "ro.product.device": self.profile["device"]["deviceCodeName"],
            "ro.build.version.release": self.profile["device"]["androidRelease"],
            "ro.soc.manufacturer": "MediaTek",
            "ro.soc.model": "MT6878",
            "ro.build.version.sdk": str(self.profile["device"]["sdkLevel"]),
        }
        if args[:3] == ("shell", "getprop", args[2] if len(args) > 2 else ""):
            return result(args, properties[args[2]] + "\n")
        if args == ("shell", "wm", "size"):
            return result(
                args,
                f"Physical size: {self.profile['device']['resolutionHeight']}x"
                f"{self.profile['device']['resolutionWidth']}\n",
            )
        if args == ("shell", "pm", "list", "packages", "--user", "0", package):
            return result(args, "")
        if args[:3] == ("install", "--no-streaming", "-t"):
            return result(args, "Success\n")
        if args[:5] == ("shell", "cmd", "package", "resolve-activity", "--brief"):
            return result(args, f"{package}/{activity}\n")
        if args == ("shell", "dumpsys", "package", package):
            return result(
                args,
                "codePath=/data/app/~~token/com.warlinecapture.game-token\n"
                "primaryCpuAbi=arm64-v8a\nflags=[ DEBUGGABLE HAS_CODE ]\n",
            )
        if args == ("shell", "run-as", package, "id"):
            return result(args, "uid=10123(u0_a123) gid=10123(u0_a123)\n")
        if args == ("shell", "pm", "path", package):
            return result(args, "package:/data/app/~~token/com.warlinecapture.game-token/base.apk\n")
        if args[:3] == ("shell", "sha256sum", "/data/app/~~token/com.warlinecapture.game-token/base.apk"):
            return result(
                args,
                f"{self.apk_sha256}  /data/app/~~token/com.warlinecapture.game-token/base.apk\n",
            )
        if args == ("shell", "dumpsys", "battery"):
            return result(
                args,
                "AC powered: false\nUSB powered: false\nWireless powered: false\n"
                "Dock powered: false\nlevel: 83\n",
            )
        if args == ("shell", "dumpsys", "thermalservice"):
            return result(args, valid_thermal())
        if args == ("shell", "pm", "clear", package):
            return result(args, "Success\n")
        if args == ("shell", "am", "force-stop", package):
            return result(args)
        if args == ("logcat", "-b", "all", "-c"):
            return result(args)
        if args[:3] == ("shell", "am", "start"):
            return result(args, "Starting: Intent\nStatus: ok\nTotalTime: 500\n")
        if args == ("shell", "pidof", package):
            return result(args, f"{self.pid}\n")
        if args == ("shell", "dumpsys", "activity", "activities"):
            return result(
                args,
                "mResumedActivity: ActivityRecord{abc u0 "
                f"{package}/{activity} t5}}\n",
            )
        if args == ("shell", "dumpsys", "meminfo", "-d", str(PID)):
            return result(args, valid_meminfo(value=2000 + self.io_samples))
        if args == ("shell", "run-as", package, "cat", f"/proc/{PID}/io"):
            self.io_samples += 1
            return result(args, valid_process_io(10000 + self.io_samples * 100))
        if args == ("exec-out", "screencap", "-p"):
            suffix = b"before" if sum(1 for call in self.calls if call == args) == 1 else b"after"
            return result(
                args,
                png_header(
                    self.profile["device"]["resolutionWidth"],
                    self.profile["device"]["resolutionHeight"],
                    suffix,
                ),
            )
        if args[:4] == ("shell", "input", "touchscreen", "swipe"):
            return result(args)
        if args[:3] == ("shell", "sh", "-c"):
            return result(args)
        raise AssertionError(f"unexpected fake ADB command: {args!r}")

    def start_logcat(self, output_path):
        text = (
            "07-15 12:00:00.000 I Unity : [APH-803 MatchReady] realtimeMs=1234.5\n"
            "07-15 12:11:01.000 I Unity : [APH-803 Recorder] complete=1 samples=9000 duration=600.0s\n"
        )
        self.session = StaticLogcatSession(Path(output_path), text)
        return self.session


class SampleAdb:
    def __init__(self, profile: dict, pids: list[int], thermal: str | None = None) -> None:
        self.profile = profile
        self.pids = pids
        self.index = 0
        self.thermal = thermal or valid_thermal()

    def run(self, args, *, timeout=60.0, use_serial=True):
        args = tuple(args)
        package = self.profile["build"]["packageName"]
        activity = self.profile["build"]["activity"]
        if args == ("shell", "pidof", package):
            value = self.pids[min(self.index, len(self.pids) - 1)]
            self.index += 1
            return result(args, f"{value}\n")
        if args == ("shell", "dumpsys", "activity", "activities"):
            return result(
                args,
                f"mResumedActivity: ActivityRecord{{abc u0 {package}/{activity} t5}}\n",
            )
        if args == ("shell", "dumpsys", "thermalservice"):
            return result(args, self.thermal)
        if args == ("shell", "dumpsys", "meminfo", "-d", str(PID)):
            return result(args, valid_meminfo())
        if args == ("shell", "run-as", package, "cat", f"/proc/{PID}/io"):
            return result(args, valid_process_io())
        raise AssertionError(args)

    def start_logcat(self, output_path):
        raise AssertionError


class Aph506TextureStreamingDeviceCollectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory(dir="/private/tmp")
        self.root = Path(self.temporary.name)
        self.profile = load_profile(DEFAULT_PROFILE)
        self.apk = self.root / self.profile["build"]["apkPath"]
        self.apk.parent.mkdir(parents=True)
        self.apk.write_bytes(b"exact-development-apk")
        self.apk_sha256 = sha256_file(self.apk)
        for index, relative in enumerate(PILOT_TEXTURE_PATHS):
            source = self.root / relative
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(f"texture-{index}".encode("ascii"))
            Path(str(source) + ".meta").write_text(
                f"fileFormatVersion: 2\nguid: {index:032x}\n",
                encoding="utf-8",
            )
        self.output = self.root / "TestResults/aph506"

    def tearDown(self) -> None:
        self.temporary.cleanup()

    def run_valid(self):
        adb = FullFakeAdb(self.profile, self.apk_sha256)
        repository = FakeRepository()
        evidence, result_value = run_collection(
            project_root=self.root,
            adb_path=Path("adb-must-not-run"),
            serial=self.profile["device"]["serial"],
            apk_path=self.apk,
            profile_path=DEFAULT_PROFILE,
            output_dir=self.output,
            expected_revision=REVISION,
            expected_apk_sha256=self.apk_sha256,
            config=CollectionConfig(60.0, 600.0, 30.0, 30.0),
            adb=adb,
            clock=FakeClock(),
            repository=repository,
            capture_id=CAPTURE_ID,
            captured_at_utc=CAPTURED_AT,
        )
        return adb, repository, evidence, result_value

    def validate(self, evidence):
        return validate_evidence(
            evidence,
            self.profile,
            expected_revision=REVISION,
            expected_apk_sha256=self.apk_sha256,
            artifact_root=self.root,
        )

    def test_default_schedule_is_exact_60_second_warmup_and_600_second_session(self) -> None:
        config = validate_config(CollectionConfig())
        self.assertEqual(DEFAULT_WARMUP_SECONDS, config.warmup_seconds)
        self.assertEqual(DEFAULT_SESSION_SECONDS, config.session_seconds)
        self.assertEqual(13, len(scheduled_offsets(config.warmup_seconds, config.sample_interval_seconds)))
        self.assertEqual(121, len(scheduled_offsets(config.session_seconds, config.sample_interval_seconds)))
        self.assertEqual(120, len(gesture_offsets(config.session_seconds, config.gesture_interval_seconds)))
        self.assertEqual(600.0, scheduled_offsets(600.0, 7.0)[-1])

    def test_short_or_unbounded_config_fails_closed(self) -> None:
        invalid = (
            CollectionConfig(59.0, 600.0, 5.0, 5.0),
            CollectionConfig(60.0, 599.0, 5.0, 5.0),
            CollectionConfig(60.0, 600.0, 1.0, 5.0),
            CollectionConfig(60.0, 600.0, 5.0, 31.0),
        )
        for config in invalid:
            with self.subTest(config=config):
                with self.assertRaises(CollectionError):
                    validate_config(config)

    def test_gesture_commands_alternate_pan_and_zoom_deterministically(self) -> None:
        actual = [gesture_argv(2712, 1220, index) for index in range(8)]
        self.assertEqual(
            ["pan-left", "zoom-in", "pan-right", "zoom-out"] * 2,
            [name for name, _ in actual],
        )
        self.assertEqual(actual[:4], actual[4:])
        self.assertEqual(("shell", "input", "touchscreen", "swipe"), actual[0][1][:4])
        self.assertEqual(("shell", "sh", "-c"), actual[1][1][:3])
        self.assertIn(" & wait", actual[1][1][3])

    def test_meminfo_parser_requires_exact_pid_package_and_summaries(self) -> None:
        parsed = parse_meminfo(valid_meminfo(), PID, "com.warlinecapture.game")
        self.assertEqual(1000, parsed["totalPssKb"])
        self.assertEqual(1004, parsed["graphicsRssKb"])
        for malformed in (
            valid_meminfo(pid=PID + 1),
            valid_meminfo(package="wrong.package"),
            valid_meminfo().replace("Graphics:", "Other:"),
            valid_meminfo() + valid_meminfo(),
        ):
            with self.subTest(malformed=malformed[:80]):
                with self.assertRaises(CollectionError):
                    parse_meminfo(malformed, PID, "com.warlinecapture.game")

    def test_process_io_parser_requires_complete_ordered_nonnegative_counters(self) -> None:
        parsed = parse_process_io(valid_process_io())
        self.assertEqual(tuple(parsed), IO_KEYS)
        self.assertEqual(1004, parsed["read_bytes"])
        signed = valid_process_io().replace("cancelled_write_bytes: 1006", "cancelled_write_bytes: -10")
        self.assertEqual(-10, parse_process_io(signed)["cancelled_write_bytes"])
        malformed = (
            valid_process_io().replace("read_bytes: 1004\n", ""),
            valid_process_io().replace("rchar: 1000", "rchar: -1"),
            valid_process_io() + "unknown: 2\n",
            valid_process_io().replace("wchar: 1001", "rchar: 1001"),
        )
        for payload in malformed:
            with self.subTest(payload=payload):
                with self.assertRaises(CollectionError):
                    parse_process_io(payload)

    def test_preinstall_pins_clean_revision_apk_hash_and_candidate_hashes(self) -> None:
        repository = FakeRepository()
        inputs = validate_preinstall_inputs(
            self.root,
            self.apk,
            DEFAULT_PROFILE,
            self.profile["device"]["serial"],
            REVISION,
            self.apk_sha256,
            repository,
        )
        self.assertEqual(self.apk_sha256, inputs.apk_sha256)
        self.assertEqual(list(PILOT_TEXTURE_PATHS), [item["path"] for item in inputs.candidates])
        self.assertTrue(all(len(item["importerSha256"]) == 64 for item in inputs.candidates))
        self.assertEqual(1, repository.head_calls)

    def test_preinstall_rejects_dirty_revision_hash_and_serial_mismatches(self) -> None:
        cases = (
            (FakeRepository(status=" M tracked"), REVISION, self.apk_sha256, self.profile["device"]["serial"]),
            (FakeRepository(revision="c" * 40), REVISION, self.apk_sha256, self.profile["device"]["serial"]),
            (FakeRepository(), REVISION, "d" * 64, self.profile["device"]["serial"]),
            (FakeRepository(), REVISION, self.apk_sha256, "wrong-device"),
        )
        for repository, revision, digest, serial in cases:
            with self.subTest(repository=repository, digest=digest, serial=serial):
                with self.assertRaises(CollectionError):
                    validate_preinstall_inputs(
                        self.root,
                        self.apk,
                        DEFAULT_PROFILE,
                        serial,
                        revision,
                        digest,
                        repository,
                    )

    def test_sample_fails_when_pid_changes_during_collection(self) -> None:
        adb = SampleAdb(self.profile, [PID, PID + 1])
        with self.assertRaisesRegex(CollectionError, "PID changed"):
            collect_sample(adb, self.profile, PID, "session", 0.0, 0.0)

    def test_sample_fails_on_individual_thermal_sensor_warning(self) -> None:
        adb = SampleAdb(self.profile, [PID, PID], warned_thermal())
        with self.assertRaisesRegex(CollectionError, "sensor statuses"):
            collect_sample(adb, self.profile, PID, "session", 0.0, 0.0)

    def test_full_mock_collection_emits_bounded_hashed_evidence_without_adb(self) -> None:
        adb, repository, evidence, result_value = self.run_valid()
        self.assertEqual("Passed", result_value["result"])
        self.assertEqual(24, result_value["sampleCount"])
        self.assertEqual(20, result_value["gestureCount"])
        self.assertEqual(2, repository.head_calls)
        self.assertEqual(2, repository.status_calls)
        self.assertTrue(adb.session and adb.session.stopped)
        self.assertTrue((self.output / EVIDENCE_FILE_NAME).is_file())
        self.assertEqual(
            sha256_file(self.output / RAW_LOG_FILE_NAME),
            evidence["artifacts"]["rawDeviceLog"]["sha256"],
        )
        self.assertEqual(
            sha256_file(self.output / BEFORE_SCREENSHOT_FILE_NAME),
            evidence["artifacts"]["beforeScreenshot"]["sha256"],
        )
        self.assertEqual(
            sha256_file(self.output / AFTER_SCREENSHOT_FILE_NAME),
            evidence["artifacts"]["afterScreenshot"]["sha256"],
        )
        calls = adb.calls
        first_screenshot = calls.index(("exec-out", "screencap", "-p"))
        first_gesture = min(
            index
            for index, call in enumerate(calls)
            if call[:3] in (("shell", "sh", "-c"), ("shell", "input", "touchscreen"))
        )
        self.assertLess(first_screenshot, first_gesture)
        self.assertTrue(
            all(
                call[:4] == ("shell", "run-as", self.profile["build"]["packageName"], "cat")
                for call in calls
                if len(call) >= 6 and call[-1] == f"/proc/{PID}/io"
            )
        )
        self.assertFalse(evidence["acceptanceBoundary"]["streamingExpansionAuthorized"])

    def test_stale_output_is_rejected_before_any_adb_call(self) -> None:
        self.output.mkdir(parents=True)
        (self.output / EVIDENCE_FILE_NAME).write_text("{}\n", encoding="utf-8")
        adb = NoCallAdb()
        with self.assertRaisesRegex(CollectionError, "stale APH-506"):
            run_collection(
                project_root=self.root,
                adb_path=Path("adb-must-not-run"),
                serial=self.profile["device"]["serial"],
                apk_path=self.apk,
                profile_path=DEFAULT_PROFILE,
                output_dir=self.output,
                expected_revision=REVISION,
                expected_apk_sha256=self.apk_sha256,
                adb=adb,
                clock=FakeClock(),
                repository=FakeRepository(),
            )
        self.assertEqual([], adb.calls)

    def test_tampered_or_missing_artifacts_fail_validation(self) -> None:
        _, _, evidence, _ = self.run_valid()
        after = self.output / AFTER_SCREENSHOT_FILE_NAME
        after.write_bytes(after.read_bytes() + b"tampered")
        with self.assertRaisesRegex(CollectionError, "size|SHA-256"):
            self.validate(evidence)

        after.write_bytes(png_header(2712, 1220, b"after"))
        evidence["artifacts"]["afterScreenshot"]["sha256"] = sha256_file(after)
        evidence["artifacts"]["afterScreenshot"]["sizeBytes"] = after.stat().st_size
        (self.output / RAW_LOG_FILE_NAME).unlink()
        with self.assertRaisesRegex(CollectionError, "missing or empty"):
            self.validate(evidence)

    def test_short_session_evidence_fails_validation(self) -> None:
        _, _, evidence, _ = self.run_valid()
        evidence["capture"]["sessionMeasuredSeconds"] = 599.999
        with self.assertRaisesRegex(CollectionError, "short"):
            self.validate(evidence)

    def test_semantically_stale_or_fatal_raw_log_fails_even_with_updated_hash(self) -> None:
        _, _, evidence, _ = self.run_valid()
        raw = self.output / RAW_LOG_FILE_NAME
        for replacement in (
            "[APH-803 MatchReady] realtimeMs=1234.5\n",
            "[APH-803 MatchReady] realtimeMs=1234.5\n"
            "[APH-803 Recorder] complete=1\nFATAL EXCEPTION: main\n",
        ):
            with self.subTest(replacement=replacement):
                raw.write_text(replacement, encoding="utf-8")
                evidence["artifacts"]["rawDeviceLog"]["sha256"] = sha256_file(raw)
                evidence["artifacts"]["rawDeviceLog"]["sizeBytes"] = raw.stat().st_size
                with self.assertRaises(CollectionError):
                    self.validate(evidence)

    def test_nonmonotonic_process_io_fails_validation(self) -> None:
        _, _, evidence, _ = self.run_valid()
        session_samples = [
            sample for sample in evidence["capture"]["samples"] if sample["phase"] == "session"
        ]
        session_samples[1]["processIoCounters"]["read_bytes"] = (
            session_samples[0]["processIoCounters"]["read_bytes"] - 1
        )
        with self.assertRaisesRegex(CollectionError, "decreased"):
            self.validate(evidence)


if __name__ == "__main__":
    unittest.main()
