import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI.android_development_performance_gate import (
    DEFAULT_PROFILE,
    GateValidationError,
    build_orchestration_contract,
    load_profile,
    validate_evidence,
)


REVISION = "1" * 40


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


class AndroidDevelopmentPerformanceGateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.profile = load_profile(DEFAULT_PROFILE)
        self.profile["capture"]["minimumFrameSamples"] = 5
        self.profile["limits"]["p99FrameMs"] = {
            "comparison": "lessThanOrEqual",
            "value": 33.0,
            "status": "tracked-budget",
        }
        self.profile["limits"]["startupP95Ms"] = {
            "comparison": "lessThanOrEqual",
            "value": 1500.0,
            "status": "tracked-budget",
        }
        self.artifact_data = {
            "apk": b"development apk",
            "structuredRecorder": b"{}",
            "rawDeviceLog": b"clean device log",
            "screenshot": self.png_header(
                self.profile["device"]["resolutionWidth"],
                self.profile["device"]["resolutionHeight"],
            ),
        }
        self.paths = {
            "apk": self.profile["build"]["apkPath"],
            "structuredRecorder": "evidence/recorder.json",
            "rawDeviceLog": "evidence/device.log",
            "screenshot": "evidence/match.png",
        }
        for key, relative in self.paths.items():
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(self.artifact_data[key])
        self.apk_sha = digest(self.artifact_data["apk"])
        self.evidence = self.make_evidence()
        recorder_bytes = json.dumps(
            self.evidence["sustainedRun"], sort_keys=True
        ).encode("utf-8")
        self.artifact_data["structuredRecorder"] = recorder_bytes
        (self.root / self.paths["structuredRecorder"]).write_bytes(recorder_bytes)
        self.evidence["artifacts"]["structuredRecorder"]["sha256"] = digest(recorder_bytes)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def artifact(self, key: str) -> dict:
        return {
            "path": self.paths[key],
            "sha256": digest(self.artifact_data[key]),
        }

    @staticmethod
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

    def make_evidence(self) -> dict:
        frames = [10.0, 20.0, 25.0, 30.0, 32.0]
        screenshot = self.artifact("screenshot")
        screenshot.update({
            "capturedPackage": self.profile["build"]["packageName"],
            "width": self.profile["device"]["resolutionWidth"],
            "height": self.profile["device"]["resolutionHeight"],
        })
        snapshots = [
            {
                "phase": phase,
                "status": 0,
                "coolingDevices": [{"name": "cpu", "value": 0}],
                "temperatures": [{"name": "cpu", "valueC": 40.0 + index}],
            }
            for index, phase in enumerate(("before", "during", "after"))
        ]
        return {
            "schemaVersion": 1,
            "taskId": "APH-803",
            "provenance": {
                "exactCommit": REVISION,
                "dirty": False,
                "apkSha256": self.apk_sha,
            },
            "device": copy.deepcopy(self.profile["device"]),
            "build": {
                "packageName": self.profile["build"]["packageName"],
                "activity": self.profile["build"]["activity"],
                "apkPath": self.profile["build"]["apkPath"],
                "buildType": self.profile["build"]["buildType"],
                "scriptingBackend": self.profile["build"]["scriptingBackend"],
                "architecture": self.profile["build"]["architecture"],
                "qualityTier": self.profile["build"]["qualityTier"],
                "frameRateMode": self.profile["build"]["frameRateMode"],
                "launchArguments": copy.deepcopy(self.profile["build"]["requiredLaunchArguments"]),
            },
            "startup": {
                "launchDefinition": "process start to structured Match-ready transition",
                "coldStartSamplesMs": [1000.0, 1100.0, 1200.0, 1300.0, 1400.0],
                "warmStartSamplesMs": [500.0, 600.0, 700.0, 800.0, 900.0],
                "coldP50Ms": 1200.0,
                "coldP95Ms": 1400.0,
                "coldMaximumMs": 1400.0,
                "warmP50Ms": 700.0,
                "warmP95Ms": 900.0,
                "warmMaximumMs": 900.0,
            },
            "sustainedRun": {
                "source": "structured-per-frame-recorder",
                "startupFramesExcluded": True,
                "warmupSeconds": self.profile["capture"]["warmupSeconds"],
                "sampleDurationSeconds": self.profile["capture"]["sustainedSampleSeconds"],
                "frameTimesMs": frames,
                "averageFrameMs": sum(frames) / len(frames),
                "p95FrameMs": 32.0,
                "p99FrameMs": 32.0,
                "maximumFrameMs": 32.0,
                "p95CpuFrameMs": 20.0,
                "p95GpuFrameMs": 24.0,
                "peakAllocatedMemoryMB": 900.0,
                "peakMonoMemoryMB": 32.0,
            },
            "thermal": {"parser": "dumpsys-thermalservice-v1", "snapshots": snapshots},
            "artifacts": {
                "apk": self.artifact("apk"),
                "structuredRecorder": self.artifact("structuredRecorder"),
                "rawDeviceLog": self.artifact("rawDeviceLog"),
                "screenshot": screenshot,
            },
            "crashScan": {"processSurvived": True, "fatalMarkers": []},
        }

    def validate(self, evidence=None, profile=None):
        return validate_evidence(
            self.evidence if evidence is None else evidence,
            self.profile if profile is None else profile,
            expected_revision=REVISION,
            expected_apk_sha256=self.apk_sha,
            artifact_root=self.root,
        )

    def sync_recorder_artifact(self, evidence: dict) -> None:
        recorder_bytes = json.dumps(
            evidence["sustainedRun"], sort_keys=True
        ).encode("utf-8")
        (self.root / self.paths["structuredRecorder"]).write_bytes(recorder_bytes)
        evidence["artifacts"]["structuredRecorder"]["sha256"] = digest(recorder_bytes)

    def test_reference_profile_pins_device_and_keeps_unapproved_limits_unset(self) -> None:
        profile = load_profile(DEFAULT_PROFILE)
        self.assertEqual("R4M7PZEQZ58T59ZH", profile["device"]["serial"])
        self.assertEqual(5, profile["capture"]["coldStartSampleCount"])
        self.assertEqual(5, profile["capture"]["warmStartSampleCount"])
        self.assertIsNone(profile["limits"]["p99FrameMs"]["value"])
        self.assertIsNone(profile["limits"]["startupP95Ms"]["value"])

    def test_schema_is_valid_json_and_fail_closed(self) -> None:
        schema_path = DEFAULT_PROFILE.parent / "android_development_performance_evidence.schema.json"
        schema = json.loads(schema_path.read_text(encoding="utf-8"))
        self.assertEqual(1, schema["properties"]["schemaVersion"]["const"])
        self.assertEqual("APH-803", schema["properties"]["taskId"]["const"])
        self.assertFalse(schema["additionalProperties"])

    def test_contract_is_deterministic_and_requires_five_cold_plus_five_warm_runs(self) -> None:
        first = build_orchestration_contract(self.profile, REVISION, self.apk_sha)
        second = build_orchestration_contract(self.profile, REVISION, self.apk_sha)
        self.assertEqual(first, second)
        self.assertEqual(10, len(first["startupRuns"]))
        self.assertEqual(5, sum(run["kind"] == "cold" for run in first["startupRuns"]))
        self.assertEqual(5, sum(run["kind"] == "warm" for run in first["startupRuns"]))
        self.assertEqual("contract-only-no-adb-execution", first["mode"])
        self.assertTrue(first["acceptanceReady"])

        unset_profile = load_profile(DEFAULT_PROFILE)
        unset_contract = build_orchestration_contract(unset_profile, REVISION, self.apk_sha)
        self.assertFalse(unset_contract["acceptanceReady"])
        self.assertEqual(["p99FrameMs", "startupP95Ms"], unset_contract["unsetLimits"])

    def test_complete_evidence_passes_after_limits_are_approved(self) -> None:
        result = self.validate()
        self.assertEqual("Passed", result["result"])
        self.assertEqual(5, result["coldStartSampleCount"])
        self.assertEqual(5, result["warmStartSampleCount"])

    def test_unset_p99_limit_fails_closed(self) -> None:
        profile = copy.deepcopy(self.profile)
        profile["limits"]["p99FrameMs"] = {
            "comparison": "lessThanOrEqual", "value": None, "status": "measurement-required"
        }
        with self.assertRaisesRegex(GateValidationError, "p99 frame limit is unset"):
            self.validate(profile=profile)

    def test_unset_startup_limit_fails_closed(self) -> None:
        profile = copy.deepcopy(self.profile)
        profile["limits"]["startupP95Ms"] = {
            "comparison": "lessThanOrEqual", "value": None, "status": "measurement-required"
        }
        with self.assertRaisesRegex(GateValidationError, "startup p95 limit is unset"):
            self.validate(profile=profile)

    def test_rejects_revision_apk_and_dirty_provenance_mismatch(self) -> None:
        mutations = (
            ("exactCommit", "2" * 40, "revision"),
            ("apkSha256", "2" * 64, "APK SHA-256"),
            ("dirty", True, "clean revision"),
        )
        for key, value, message in mutations:
            with self.subTest(key=key):
                evidence = copy.deepcopy(self.evidence)
                evidence["provenance"][key] = value
                with self.assertRaisesRegex(GateValidationError, message):
                    self.validate(evidence=evidence)

    def test_rejects_device_build_and_launch_argument_drift(self) -> None:
        cases = (
            ("device", "model", "different", "device.model mismatch"),
            ("build", "architecture", "ARMv7", "build.architecture mismatch"),
            ("build", "qualityTier", "High", "build.qualityTier mismatch"),
        )
        for section, key, value, message in cases:
            with self.subTest(section=section, key=key):
                evidence = copy.deepcopy(self.evidence)
                evidence[section][key] = value
                with self.assertRaisesRegex(GateValidationError, message):
                    self.validate(evidence=evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["build"]["launchArguments"] = evidence["build"]["launchArguments"][:-1]
        with self.assertRaisesRegex(GateValidationError, "launchArguments mismatch"):
            self.validate(evidence=evidence)

    def test_requires_exactly_five_cold_and_five_warm_samples(self) -> None:
        for key, message in (
            ("coldStartSamplesMs", "5 cold-start"),
            ("warmStartSamplesMs", "5 warm-start"),
        ):
            with self.subTest(key=key):
                evidence = copy.deepcopy(self.evidence)
                evidence["startup"][key].pop()
                with self.assertRaisesRegex(GateValidationError, message):
                    self.validate(evidence=evidence)

    def test_recomputes_startup_and_frame_metrics(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        evidence["startup"]["coldP95Ms"] = 1.0
        with self.assertRaisesRegex(GateValidationError, "does not match recomputed"):
            self.validate(evidence=evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["sustainedRun"]["p99FrameMs"] = 1.0
        with self.assertRaisesRegex(GateValidationError, "does not match recomputed"):
            self.validate(evidence=evidence)

    def test_rejects_aggregate_source_short_capture_and_startup_contamination(self) -> None:
        cases = (
            ("source", "frame-rate-diagnostic-averages", "aggregate diagnostic"),
            ("startupFramesExcluded", False, "startup frames"),
            ("warmupSeconds", 59, "warmup is shorter"),
            ("sampleDurationSeconds", 599, "sample duration is shorter"),
        )
        for key, value, message in cases:
            with self.subTest(key=key):
                evidence = copy.deepcopy(self.evidence)
                evidence["sustainedRun"][key] = value
                with self.assertRaisesRegex(GateValidationError, message):
                    self.validate(evidence=evidence)

    def test_applies_frame_startup_and_memory_limits(self) -> None:
        cases = (
            ("sustainedRun", "frameTimesMs", [10, 20, 30, 33, 33], "p95 frame failed"),
            ("startup", "coldStartSamplesMs", [1000, 1100, 1200, 1300, 1600], "recomputed"),
            ("sustainedRun", "peakAllocatedMemoryMB", 968.0, "peak allocated memory failed"),
        )
        for section, key, value, message in cases:
            with self.subTest(section=section, key=key):
                evidence = copy.deepcopy(self.evidence)
                evidence[section][key] = value
                if key == "frameTimesMs":
                    evidence[section].update({"averageFrameMs": 25.2, "p95FrameMs": 33, "p99FrameMs": 33, "maximumFrameMs": 33})
                if section == "sustainedRun":
                    self.sync_recorder_artifact(evidence)
                with self.assertRaisesRegex(GateValidationError, message):
                    self.validate(evidence=evidence)

    def test_rejects_nonzero_thermal_or_cooling_and_missing_phase(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        evidence["thermal"]["snapshots"][1]["status"] = 1
        with self.assertRaisesRegex(GateValidationError, "thermal limit"):
            self.validate(evidence=evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["thermal"]["snapshots"][1]["coolingDevices"][0]["value"] = 1
        with self.assertRaisesRegex(GateValidationError, "cooling-device limit"):
            self.validate(evidence=evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["thermal"]["snapshots"] = evidence["thermal"]["snapshots"][:2]
        with self.assertRaisesRegex(GateValidationError, "before, during, and after"):
            self.validate(evidence=evidence)

    def test_rejects_missing_or_tampered_artifact_and_wrong_screenshot(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["rawDeviceLog"]["sha256"] = "2" * 64
        with self.assertRaisesRegex(GateValidationError, "does not match file"):
            self.validate(evidence=evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["screenshot"]["width"] = 1
        with self.assertRaisesRegex(GateValidationError, "dimensions"):
            self.validate(evidence=evidence)

    def test_rejects_recorder_mismatch_and_invalid_png(self) -> None:
        recorder_path = self.root / self.paths["structuredRecorder"]
        recorder_path.write_text("{}", encoding="utf-8")
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["structuredRecorder"]["sha256"] = digest(b"{}")
        with self.assertRaisesRegex(GateValidationError, "does not exactly match"):
            self.validate(evidence=evidence)

        recorder_path.write_bytes(self.artifact_data["structuredRecorder"])
        screenshot_path = self.root / self.paths["screenshot"]
        screenshot_path.write_bytes(b"not a png")
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["screenshot"]["sha256"] = digest(b"not a png")
        with self.assertRaisesRegex(GateValidationError, "not a valid PNG"):
            self.validate(evidence=evidence)

    def test_scans_hashed_raw_log_instead_of_trusting_empty_marker_list(self) -> None:
        raw_log_path = self.root / self.paths["rawDeviceLog"]
        raw_log = b"Unity: FATAL EXCEPTION in com.warlinecapture.game\n"
        raw_log_path.write_bytes(raw_log)
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["rawDeviceLog"]["sha256"] = digest(raw_log)
        with self.assertRaisesRegex(GateValidationError, "raw device log contains"):
            self.validate(evidence=evidence)

    def test_rejects_process_death_or_crash_markers(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        evidence["crashScan"]["processSurvived"] = False
        with self.assertRaisesRegex(GateValidationError, "did not survive"):
            self.validate(evidence=evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["crashScan"]["fatalMarkers"] = ["SIGABRT"]
        with self.assertRaisesRegex(GateValidationError, "crash/fatal"):
            self.validate(evidence=evidence)


if __name__ == "__main__":
    unittest.main()
