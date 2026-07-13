from __future__ import annotations

import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI.android_release_performance_gate import (
    DEFAULT_PROFILE,
    GateValidationError,
    build_orchestration_contract,
    load_profile,
    percentile,
    validate_evidence,
)


REVISION = "a" * 40


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


class AndroidReleasePerformanceGateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.profile = load_profile(DEFAULT_PROFILE)
        self.profile["capture"]["minimumFrameSamples"] = 5
        self.apk_bytes = b"release apk boundary"
        self.paths = {
            "apk": self.profile["build"]["apkPath"],
            "structuredRecorder": "evidence/release-recorder.json",
            "rawDeviceLog": "evidence/device.log",
            "screenshot": "evidence/match.png",
        }
        self.data = {
            "apk": self.apk_bytes,
            "rawDeviceLog": b"clean release device log\n",
            "screenshot": self.png_header(
                self.profile["device"]["resolutionWidth"],
                self.profile["device"]["resolutionHeight"],
            ),
        }
        for key in ("apk", "rawDeviceLog", "screenshot"):
            path = self.root / self.paths[key]
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(self.data[key])
        self.apk_sha = digest(self.apk_bytes)
        self.evidence = self.make_evidence()
        self.sync_recorder(self.evidence)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    @staticmethod
    def png_header(width: int, height: int) -> bytes:
        return (
            b"\x89PNG\r\n\x1a\n" + (13).to_bytes(4, "big") + b"IHDR"
            + width.to_bytes(4, "big") + height.to_bytes(4, "big")
            + b"\x08\x06\x00\x00\x00" + b"\x00\x00\x00\x00"
        )

    def artifact(self, key: str, *, size: bool = False) -> dict:
        result = {"path": self.paths[key], "sha256": digest(self.data[key])}
        if size:
            result["sizeBytes"] = len(self.data[key])
        return result

    def make_evidence(self) -> dict:
        frames = [20.0, 21.0, 22.0, 23.0, 32.0]
        sustained = {
            "source": "structured-per-frame-recorder",
            "startupFramesExcluded": True,
            "warmupSeconds": 60,
            "sampleDurationSeconds": 600,
            "frameTimesMs": frames,
            "averageFrameMs": sum(frames) / len(frames),
            "p95FrameMs": 32.0,
            "p99FrameMs": 32.0,
            "maximumFrameMs": 32.0,
            "gc": {"totalAllocatedBytes": 500, "averageAllocatedBytesPerFrame": 100.0, "collectionCount": 2},
            "memory": {"peakAllocatedMemoryMB": 900.0, "peakMonoMemoryMB": 30.0, "peakResidentSetMB": 1200.0},
            "battery": {"startPercent": 80.0, "endPercent": 75.0, "drainPercent": 5.0},
            "counters": {
                "cpuTimingSampleCount": 5, "gpuTimingSampleCount": 5,
                "averageCpuFrameMs": 12.0, "p95CpuFrameMs": 20.0,
                "averageGpuFrameMs": 15.0, "p95GpuFrameMs": 24.0,
                "averageBatches": 500.0, "averageSetPassCalls": 45.0,
                "averageTriangles": 100000.0, "averageVertices": 150000.0,
            },
        }
        snapshots = [{
            "phase": phase, "status": 0,
            "coolingDevices": [{"name": "cpu", "value": 0}],
            "temperatures": [{"name": "cpu", "valueC": 40.0 + index}],
        } for index, phase in enumerate(("before", "during", "after"))]
        screenshot = self.artifact("screenshot")
        screenshot.update({
            "capturedPackage": self.profile["build"]["packageName"],
            "width": self.profile["device"]["resolutionWidth"],
            "height": self.profile["device"]["resolutionHeight"],
        })
        return {
            "schemaVersion": 1,
            "taskId": "APH-804",
            "provenance": {"exactCommit": REVISION, "dirty": False, "apkSha256": self.apk_sha},
            "device": copy.deepcopy(self.profile["device"]),
            "build": {
                **{key: value for key, value in self.profile["build"].items() if key != "requiredLaunchArguments"},
                "launchArguments": copy.deepcopy(self.profile["build"]["requiredLaunchArguments"]),
            },
            "startup": {
                "launchDefinition": "process start to structured Match-ready transition",
                "coldStartSamplesMs": [1000.0, 1100.0, 1200.0, 1300.0, 1400.0],
                "warmStartSamplesMs": [500.0, 600.0, 700.0, 800.0, 900.0],
                "coldP50Ms": 1200.0, "coldP95Ms": 1400.0, "coldMaximumMs": 1400.0,
                "warmP50Ms": 700.0, "warmP95Ms": 900.0, "warmMaximumMs": 900.0,
            },
            "sustainedRun": sustained,
            "installedSizeBytes": 800000000,
            "thermal": {"parser": "dumpsys-thermalservice-v1", "snapshots": snapshots},
            "artifacts": {
                "apk": self.artifact("apk", size=True),
                "structuredRecorder": {"path": self.paths["structuredRecorder"], "sha256": "0" * 64},
                "rawDeviceLog": self.artifact("rawDeviceLog"),
                "screenshot": screenshot,
            },
            "crashScan": {"processSurvived": True, "fatalMarkers": []},
        }

    @staticmethod
    def recorder_payload(evidence: dict) -> dict:
        return {
            "schemaVersion": 1, "taskId": "APH-804", "complete": True, "failure": "",
            "launchRealtimeSeconds": 0.25, "matchReadyRealtimeSeconds": 1.25,
            "processToMatchReadyMs": 1000.0,
            "cpuTimingSampleCount": len(evidence["sustainedRun"]["frameTimesMs"]),
            "gpuTimingSampleCount": len(evidence["sustainedRun"]["frameTimesMs"]),
            "recorderMode": "release-performance-evidence", "buildType": "release",
            "developmentBuild": False, "scriptDebugging": False,
            "profilerAttached": False, "profilerMarkersEnabled": False,
            "sustainedRun": evidence["sustainedRun"],
        }

    def sync_recorder(self, evidence: dict, payload: dict | None = None) -> None:
        recorder = self.recorder_payload(evidence) if payload is None else payload
        data = json.dumps(recorder, sort_keys=True).encode("utf-8")
        path = self.root / self.paths["structuredRecorder"]
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        evidence["artifacts"]["structuredRecorder"]["sha256"] = digest(data)

    def validate(self, evidence: dict | None = None, profile: dict | None = None) -> dict:
        return validate_evidence(
            self.evidence if evidence is None else evidence,
            self.profile if profile is None else profile,
            expected_revision=REVISION,
            expected_apk_sha256=self.apk_sha,
            artifact_root=self.root,
        )

    def set_frames(self, evidence: dict, frames: list[float]) -> None:
        run = evidence["sustainedRun"]
        run["frameTimesMs"] = frames
        run.update({"averageFrameMs": sum(frames) / len(frames), "p95FrameMs": percentile(frames, 95),
                    "p99FrameMs": percentile(frames, 99), "maximumFrameMs": max(frames)})
        run["gc"]["averageAllocatedBytesPerFrame"] = run["gc"]["totalAllocatedBytes"] / len(frames)
        run["counters"]["cpuTimingSampleCount"] = len(frames)
        run["counters"]["gpuTimingSampleCount"] = len(frames)
        self.sync_recorder(evidence)

    def test_profile_pins_release_build_device_launch_and_blockers(self) -> None:
        profile = load_profile(DEFAULT_PROFILE)
        self.assertEqual(("Xiaomi", "24090RA29G"), (profile["device"]["manufacturer"], profile["device"]["model"]))
        self.assertEqual("Build/AndroidAPK/WarlineCapture.apk", profile["build"]["apkPath"])
        self.assertEqual(("release", "IL2CPP", "ARM64", "Mobile"), tuple(profile["build"][key] for key in ("buildType", "scriptingBackend", "architecture", "qualityTier")))
        self.assertEqual((60, 60), (profile["build"]["requestedFrameRate"], profile["build"]["actualFrameRate"]))
        self.assertEqual([
            "-warlineAutoStartMatch", "-warlineAndroidPerformanceGate", "APH-804",
            "-warlinePerformanceFrameRate", "60",
        ], profile["build"]["requiredLaunchArguments"])
        self.assertEqual(463359198, profile["limits"]["maximumApkSizeBytes"]["value"])
        self.assertEqual("lessThan", profile["limits"]["p95FrameMs"]["comparison"])
        self.assertEqual((60, 600, 9000), tuple(profile["capture"][key] for key in ("warmupSeconds", "sustainedSampleSeconds", "minimumFrameSamples")))

    def test_schema_is_strict_release_json(self) -> None:
        schema = json.loads((DEFAULT_PROFILE.parent / "android_release_performance_evidence.schema.json").read_text())
        self.assertEqual("APH-804", schema["properties"]["taskId"]["const"])
        self.assertEqual("release", schema["$defs"]["build"]["properties"]["buildType"]["const"])
        launch_schema = schema["$defs"]["build"]["properties"]["launchArguments"]
        self.assertEqual(len(self.profile["build"]["requiredLaunchArguments"]), launch_schema["minItems"])
        self.assertEqual(launch_schema["minItems"], launch_schema["maxItems"])
        self.assertFalse(schema["additionalProperties"])

    def test_contract_is_deterministic_and_not_acceptance_ready(self) -> None:
        first = build_orchestration_contract(self.profile, REVISION, self.apk_sha)
        self.assertEqual(first, build_orchestration_contract(self.profile, REVISION, self.apk_sha))
        self.assertFalse(first["acceptanceReady"])
        self.assertIn("release-mode-structured-recorder", first["unmetAcceptanceRequirements"])
        self.assertIn("validated-release-device-evidence", first["unmetAcceptanceRequirements"])
        self.assertEqual(["p99FrameMs", "startupP95Ms", "installedSizeBytes", "absoluteMemoryMB"], first["measurementRequiredLimits"])

    def test_complete_release_evidence_passes_and_becomes_ready(self) -> None:
        result = self.validate()
        self.assertTrue(result["acceptanceReady"])
        self.assertEqual("Passed", result["result"])
        self.assertFalse(result["highEndObservation"]["blocking"])

    def test_release_validation_requires_artifact_root(self) -> None:
        with self.assertRaisesRegex(GateValidationError, "artifact files"):
            validate_evidence(self.evidence, self.profile, expected_revision=REVISION, expected_apk_sha256=self.apk_sha)

    def test_rejects_development_build_and_development_recorder(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        evidence["build"]["buildType"] = "development"
        with self.assertRaisesRegex(GateValidationError, "buildType mismatch"):
            self.validate(evidence)
        evidence = copy.deepcopy(self.evidence)
        payload = self.recorder_payload(evidence)
        payload["developmentBuild"] = True
        self.sync_recorder(evidence, payload)
        with self.assertRaisesRegex(GateValidationError, "release-mode evidence"):
            self.validate(evidence)

    def test_profile_rejects_profiler_debug_and_development_arguments(self) -> None:
        for argument in ("-warlineProfilerMarkers", "-development", "-debug"):
            with self.subTest(argument=argument):
                profile = load_profile(DEFAULT_PROFILE)
                profile["build"]["requiredLaunchArguments"].append(argument)
                path = self.root / "profile.json"
                path.write_text(json.dumps(profile), encoding="utf-8")
                with self.assertRaisesRegex(GateValidationError, "forbidden profiler/development/debug"):
                    load_profile(path)

    def test_p95_boundary_is_exclusive(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        self.set_frames(evidence, [20.0, 21.0, 22.0, 23.0, 32.999])
        self.validate(evidence)
        evidence = copy.deepcopy(self.evidence)
        self.set_frames(evidence, [20.0, 21.0, 22.0, 23.0, 33.0])
        with self.assertRaisesRegex(GateValidationError, "p95 frame failed"):
            self.validate(evidence)

    def test_apk_size_boundary_is_inclusive(self) -> None:
        profile = copy.deepcopy(self.profile)
        profile["limits"]["maximumApkSizeBytes"]["value"] = len(self.apk_bytes)
        self.validate(profile=profile)
        profile["limits"]["maximumApkSizeBytes"]["value"] = len(self.apk_bytes) - 1
        with self.assertRaisesRegex(GateValidationError, "APK size failed"):
            self.validate(profile=profile)

    def test_high_end_observation_never_blocks_aph804(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        self.set_frames(evidence, [20.0, 21.0, 22.0, 23.0, 24.999])
        self.assertTrue(self.validate(evidence)["highEndObservation"]["observed"])
        evidence = copy.deepcopy(self.evidence)
        self.set_frames(evidence, [20.0, 21.0, 22.0, 23.0, 25.0])
        result = self.validate(evidence)
        self.assertFalse(result["highEndObservation"]["observed"])
        self.assertEqual("Passed", result["result"])

    def test_measurement_required_metrics_are_recorded_but_non_blocking(self) -> None:
        for name in ("p99FrameMs", "startupP95Ms", "installedSizeBytes", "absoluteMemoryMB"):
            limit = self.profile["limits"][name]
            self.assertIsNone(limit["value"])
            self.assertEqual("measurement-required", limit["status"])
            self.assertFalse(limit["blocking"])
        evidence = copy.deepcopy(self.evidence)
        self.set_frames(evidence, [20.0] * 95 + [100.0] * 5)
        evidence["startup"].update({"coldStartSamplesMs": [10000.0] * 5, "warmStartSamplesMs": [9000.0] * 5,
                                    "coldP50Ms": 10000.0, "coldP95Ms": 10000.0, "coldMaximumMs": 10000.0,
                                    "warmP50Ms": 9000.0, "warmP95Ms": 9000.0, "warmMaximumMs": 9000.0})
        evidence["installedSizeBytes"] = 2_000_000_000
        evidence["sustainedRun"]["memory"]["peakResidentSetMB"] = 5000.0
        self.sync_recorder(evidence)
        result = self.validate(evidence)
        self.assertEqual(100.0, result["metrics"]["p99FrameMs"])
        self.assertEqual("Passed", result["result"])

    def test_rejects_tampering_and_dirty_or_mismatched_provenance(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["rawDeviceLog"]["sha256"] = "f" * 64
        with self.assertRaisesRegex(GateValidationError, "does not match file"):
            self.validate(evidence)
        for key, value, message in (("dirty", True, "clean revision"), ("exactCommit", "b" * 40, "revision"), ("apkSha256", "f" * 64, "APK SHA-256")):
            with self.subTest(key=key):
                evidence = copy.deepcopy(self.evidence)
                evidence["provenance"][key] = value
                with self.assertRaisesRegex(GateValidationError, message):
                    self.validate(evidence)

    def test_rejects_thermal_or_cooling_activity(self) -> None:
        for key, value, message in (("status", 1, "thermal limit"), ("cooling", 1, "cooling-device limit")):
            evidence = copy.deepcopy(self.evidence)
            if key == "status":
                evidence["thermal"]["snapshots"][1]["status"] = value
            else:
                evidence["thermal"]["snapshots"][1]["coolingDevices"][0]["value"] = value
            with self.assertRaisesRegex(GateValidationError, message):
                self.validate(evidence)

    def test_rejects_short_duration_warmup_and_sample_count(self) -> None:
        for key, value, message in (("warmupSeconds", 59, "warmup is shorter"), ("sampleDurationSeconds", 599, "duration is shorter")):
            evidence = copy.deepcopy(self.evidence)
            evidence["sustainedRun"][key] = value
            self.sync_recorder(evidence)
            with self.assertRaisesRegex(GateValidationError, message):
                self.validate(evidence)
        evidence = copy.deepcopy(self.evidence)
        self.set_frames(evidence, [20.0, 21.0, 22.0, 23.0])
        with self.assertRaisesRegex(GateValidationError, "too few structured frame samples"):
            self.validate(evidence)

    def test_rejects_process_death_fatal_log_and_incoherent_screenshot(self) -> None:
        evidence = copy.deepcopy(self.evidence)
        evidence["crashScan"]["processSurvived"] = False
        with self.assertRaisesRegex(GateValidationError, "did not survive"):
            self.validate(evidence)
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["screenshot"]["width"] = 1
        with self.assertRaisesRegex(GateValidationError, "dimensions"):
            self.validate(evidence)
        raw = b"FATAL EXCEPTION in com.warlinecapture.game\n"
        (self.root / self.paths["rawDeviceLog"]).write_bytes(raw)
        evidence = copy.deepcopy(self.evidence)
        evidence["artifacts"]["rawDeviceLog"]["sha256"] = digest(raw)
        with self.assertRaisesRegex(GateValidationError, "raw device log contains"):
            self.validate(evidence)


if __name__ == "__main__":
    unittest.main()
