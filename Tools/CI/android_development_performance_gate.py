#!/usr/bin/env python3
"""Build and validate the non-Unity APH-803 Android evidence contract."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
from pathlib import Path
from typing import Any, Iterable


SCHEMA_VERSION = 1
TASK_ID = "APH-803"
DEFAULT_PROFILE = Path("Tools/CI/android_reference_device_profile.json")
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
COMMIT_PATTERN = re.compile(r"^[0-9a-f]{40}$")
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
FATAL_PATTERNS = (
    ("FATAL EXCEPTION", re.compile(r"FATAL EXCEPTION", re.IGNORECASE)),
    ("fatal signal", re.compile(r"fatal signal", re.IGNORECASE)),
    ("SIGSEGV", re.compile(r"\bSIGSEGV\b", re.IGNORECASE)),
    ("SIGABRT", re.compile(r"\bSIGABRT\b", re.IGNORECASE)),
    ("ANR", re.compile(r"\bANR in com\.warlinecapture\.game\b", re.IGNORECASE)),
    ("task removal", re.compile(r"ProcessSceneCleaner\.handleSwipeKill|removeTask", re.IGNORECASE)),
)


class GateValidationError(RuntimeError):
    pass


def _object(value: Any, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise GateValidationError(f"{path} must be an object")
    return value


def _array(value: Any, path: str) -> list[Any]:
    if not isinstance(value, list):
        raise GateValidationError(f"{path} must be an array")
    return value


def _string(value: Any, path: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise GateValidationError(f"{path} must be a non-empty string")
    return value.strip()


def _number(value: Any, path: str, *, positive: bool = False) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
        raise GateValidationError(f"{path} must be a finite number")
    result = float(value)
    if result < 0 or (positive and result <= 0):
        qualifier = "positive" if positive else "non-negative"
        raise GateValidationError(f"{path} must be {qualifier}")
    return result


def _integer(value: Any, path: str, *, minimum: int = 0) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or value < minimum:
        raise GateValidationError(f"{path} must be an integer >= {minimum}")
    return value


def _exact_keys(value: dict[str, Any], expected: set[str], path: str) -> None:
    missing = sorted(expected - set(value))
    unknown = sorted(set(value) - expected)
    if missing:
        raise GateValidationError(f"{path} is missing: {', '.join(missing)}")
    if unknown:
        raise GateValidationError(f"{path} has unknown fields: {', '.join(unknown)}")


def load_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise GateValidationError(f"missing JSON file: {path}")
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise GateValidationError(f"invalid JSON '{path}': {exc}") from exc
    return _object(value, str(path))


def load_profile(path: Path = DEFAULT_PROFILE) -> dict[str, Any]:
    profile = load_json(path)
    _exact_keys(profile, {"schemaVersion", "taskId", "device", "build", "capture", "limits"}, "profile")
    if profile["schemaVersion"] != SCHEMA_VERSION or profile["taskId"] != TASK_ID:
        raise GateValidationError("profile schemaVersion/taskId mismatch")

    device = _object(profile["device"], "profile.device")
    device_keys = {
        "serial", "manufacturer", "model", "deviceCodeName", "soc", "androidRelease",
        "sdkLevel", "resolutionWidth", "resolutionHeight"
    }
    _exact_keys(device, device_keys, "profile.device")
    for key in device_keys - {"sdkLevel", "resolutionWidth", "resolutionHeight"}:
        _string(device[key], f"profile.device.{key}")
    for key in ("sdkLevel", "resolutionWidth", "resolutionHeight"):
        _integer(device[key], f"profile.device.{key}", minimum=1)

    build = _object(profile["build"], "profile.build")
    build_keys = {
        "packageName", "activity", "apkPath", "buildType", "scriptingBackend", "architecture",
        "qualityTier", "frameRateMode", "requiredLaunchArguments"
    }
    _exact_keys(build, build_keys, "profile.build")
    for key in build_keys - {"frameRateMode", "requiredLaunchArguments"}:
        _string(build[key], f"profile.build.{key}")
    _integer(build["frameRateMode"], "profile.build.frameRateMode", minimum=1)
    arguments = _array(build["requiredLaunchArguments"], "profile.build.requiredLaunchArguments")
    if not arguments or len(arguments) != len(set(arguments)):
        raise GateValidationError("profile.build.requiredLaunchArguments must be non-empty and unique")
    for index, argument in enumerate(arguments):
        _string(argument, f"profile.build.requiredLaunchArguments[{index}]")

    capture = _object(profile["capture"], "profile.capture")
    capture_keys = {
        "coldStartSampleCount", "warmStartSampleCount", "warmupSeconds", "sustainedSampleSeconds",
        "minimumFrameSamples", "requiredThermalPhases"
    }
    _exact_keys(capture, capture_keys, "profile.capture")
    for key in capture_keys - {"requiredThermalPhases"}:
        _integer(capture[key], f"profile.capture.{key}", minimum=1)
    phases = _array(capture["requiredThermalPhases"], "profile.capture.requiredThermalPhases")
    if phases != ["before", "during", "after"]:
        raise GateValidationError("profile capture thermal phases must be before/during/after")

    limits = _object(profile["limits"], "profile.limits")
    _exact_keys(limits, {
        "p95FrameMs", "p99FrameMs", "startupP95Ms", "peakAllocatedMemoryMB",
        "maximumThermalStatus", "maximumCoolingDeviceValue"
    }, "profile.limits")
    _validate_limit(limits["p95FrameMs"], "profile.limits.p95FrameMs", allow_unset=False)
    _validate_limit(limits["p99FrameMs"], "profile.limits.p99FrameMs", allow_unset=True)
    _validate_limit(limits["startupP95Ms"], "profile.limits.startupP95Ms", allow_unset=True)
    memory = _object(limits["peakAllocatedMemoryMB"], "profile.limits.peakAllocatedMemoryMB")
    _exact_keys(memory, {
        "comparison", "value", "sourceBaselineMaximumMB", "requiredReductionPercent"
    }, "profile.limits.peakAllocatedMemoryMB")
    _validate_comparison(memory["comparison"], "profile.limits.peakAllocatedMemoryMB.comparison")
    _number(memory["value"], "profile.limits.peakAllocatedMemoryMB.value", positive=True)
    baseline = _number(memory["sourceBaselineMaximumMB"], "profile.limits.peakAllocatedMemoryMB.sourceBaselineMaximumMB", positive=True)
    reduction = _number(memory["requiredReductionPercent"], "profile.limits.peakAllocatedMemoryMB.requiredReductionPercent", positive=True)
    expected_value = baseline * (1.0 - reduction / 100.0)
    if not math.isclose(float(memory["value"]), expected_value, rel_tol=0, abs_tol=1e-9):
        raise GateValidationError("peak memory limit does not match its baseline reduction")
    _integer(limits["maximumThermalStatus"], "profile.limits.maximumThermalStatus")
    _integer(limits["maximumCoolingDeviceValue"], "profile.limits.maximumCoolingDeviceValue")
    return profile


def _validate_limit(value: Any, path: str, *, allow_unset: bool) -> None:
    item = _object(value, path)
    expected = {"comparison", "value"} | ({"status"} if allow_unset else set())
    _exact_keys(item, expected, path)
    _validate_comparison(item["comparison"], f"{path}.comparison")
    if item["value"] is None:
        if not allow_unset or item.get("status") != "measurement-required":
            raise GateValidationError(f"{path}.value cannot be unset")
    else:
        _number(item["value"], f"{path}.value", positive=True)
        if allow_unset and item.get("status") != "tracked-budget":
            raise GateValidationError(f"{path}.status must be tracked-budget after approval")


def _validate_comparison(value: Any, path: str) -> str:
    result = _string(value, path)
    if result not in {"lessThan", "lessThanOrEqual"}:
        raise GateValidationError(f"{path} has unsupported comparison: {result}")
    return result


def percentile(values: Iterable[float], percentile_value: float) -> float:
    ordered = sorted(float(value) for value in values)
    if not ordered:
        raise GateValidationError("cannot calculate a percentile from zero samples")
    position = (len(ordered) - 1) * percentile_value / 100.0
    index = int(math.floor(position + 0.5))
    return ordered[index]


def _assert_close(actual: Any, expected: float, path: str) -> None:
    value = _number(actual, path)
    if not math.isclose(value, expected, rel_tol=0, abs_tol=0.001):
        raise GateValidationError(f"{path}={value} does not match recomputed value {expected}")


def _apply_limit(metric: float, limit: dict[str, Any], path: str) -> None:
    value = limit.get("value")
    if value is None:
        raise GateValidationError(f"{path} limit is unset; APH-803 must fail closed")
    threshold = _number(value, f"{path}.value", positive=True)
    comparison = _validate_comparison(limit.get("comparison"), f"{path}.comparison")
    passed = metric < threshold if comparison == "lessThan" else metric <= threshold
    if not passed:
        operator = "<" if comparison == "lessThan" else "<="
        raise GateValidationError(f"{path} failed: {metric} must be {operator} {threshold}")


def _validate_exact_mapping(actual: dict[str, Any], expected: dict[str, Any], keys: set[str], path: str) -> None:
    _exact_keys(actual, keys, path)
    for key in sorted(keys):
        if actual[key] != expected[key]:
            raise GateValidationError(
                f"{path}.{key} mismatch: expected {expected[key]!r}, found {actual[key]!r}"
            )


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _resolve_artifact_path(path_text: str, artifact_root: Path) -> Path:
    path = Path(path_text)
    return path if path.is_absolute() else artifact_root / path


def detect_fatal_markers(log_text: str) -> list[str]:
    return [name for name, pattern in FATAL_PATTERNS if pattern.search(log_text)]


def read_png_dimensions(path: Path) -> tuple[int, int]:
    header = path.read_bytes()[:24]
    if len(header) < 24 or header[:8] != PNG_SIGNATURE or header[12:16] != b"IHDR":
        raise GateValidationError(f"screenshot is not a valid PNG header: {path}")
    return int.from_bytes(header[16:20], "big"), int.from_bytes(header[20:24], "big")


def _validate_artifact(value: Any, path: str, artifact_root: Path | None, extra_keys: set[str] | None = None) -> dict[str, Any]:
    item = _object(value, path)
    keys = {"path", "sha256"} | (extra_keys or set())
    _exact_keys(item, keys, path)
    artifact_path_text = _string(item["path"], f"{path}.path")
    digest = _string(item["sha256"], f"{path}.sha256").lower()
    if SHA256_PATTERN.fullmatch(digest) is None:
        raise GateValidationError(f"{path}.sha256 must be 64 lowercase hexadecimal characters")
    if artifact_root is not None:
        artifact_path = Path(artifact_path_text)
        resolved = artifact_path if artifact_path.is_absolute() else artifact_root / artifact_path
        if not resolved.is_file() or resolved.stat().st_size == 0:
            raise GateValidationError(f"{path}.path is missing or empty: {resolved}")
        actual_digest = _sha256(resolved)
        if actual_digest != digest:
            raise GateValidationError(f"{path}.sha256 does not match file: {resolved}")
    return item


def validate_evidence(
    evidence: Any,
    profile: dict[str, Any],
    *,
    expected_revision: str,
    expected_apk_sha256: str,
    artifact_root: Path | None = None,
) -> dict[str, Any]:
    report = _object(evidence, "evidence")
    root_keys = {
        "schemaVersion", "taskId", "provenance", "device", "build", "startup",
        "sustainedRun", "thermal", "artifacts", "crashScan"
    }
    _exact_keys(report, root_keys, "evidence")
    if report["schemaVersion"] != SCHEMA_VERSION or report["taskId"] != TASK_ID:
        raise GateValidationError("evidence schemaVersion/taskId mismatch")
    if COMMIT_PATTERN.fullmatch(expected_revision) is None:
        raise GateValidationError("expected revision must be 40 lowercase hexadecimal characters")
    expected_apk_sha256 = expected_apk_sha256.lower()
    if SHA256_PATTERN.fullmatch(expected_apk_sha256) is None:
        raise GateValidationError("expected APK SHA-256 must be 64 lowercase hexadecimal characters")

    provenance = _object(report["provenance"], "evidence.provenance")
    _exact_keys(provenance, {"exactCommit", "dirty", "apkSha256"}, "evidence.provenance")
    if provenance["exactCommit"] != expected_revision:
        raise GateValidationError("evidence revision does not match the requested revision")
    if provenance["dirty"] is not False:
        raise GateValidationError("development gate evidence must come from a clean revision")
    if provenance["apkSha256"] != expected_apk_sha256:
        raise GateValidationError("evidence APK SHA-256 does not match the requested artifact")

    device_keys = set(profile["device"])
    _validate_exact_mapping(_object(report["device"], "evidence.device"), profile["device"], device_keys, "evidence.device")
    build = _object(report["build"], "evidence.build")
    build_keys = {
        "packageName", "activity", "apkPath", "buildType", "scriptingBackend", "architecture",
        "qualityTier", "frameRateMode", "launchArguments"
    }
    _exact_keys(build, build_keys, "evidence.build")
    for key in build_keys - {"launchArguments"}:
        expected = profile["build"]["requiredLaunchArguments"] if key == "launchArguments" else profile["build"].get(key)
        if expected is not None and build[key] != expected:
            raise GateValidationError(f"evidence.build.{key} mismatch")
    arguments = _array(build["launchArguments"], "evidence.build.launchArguments")
    if arguments != profile["build"]["requiredLaunchArguments"]:
        raise GateValidationError("evidence.build.launchArguments mismatch")

    startup = _object(report["startup"], "evidence.startup")
    startup_keys = {
        "launchDefinition", "coldStartSamplesMs", "warmStartSamplesMs", "coldP50Ms", "coldP95Ms",
        "coldMaximumMs", "warmP50Ms", "warmP95Ms", "warmMaximumMs"
    }
    _exact_keys(startup, startup_keys, "evidence.startup")
    _string(startup["launchDefinition"], "evidence.startup.launchDefinition")
    cold = [_number(value, f"evidence.startup.coldStartSamplesMs[{index}]", positive=True)
            for index, value in enumerate(_array(startup["coldStartSamplesMs"], "evidence.startup.coldStartSamplesMs"))]
    warm = [_number(value, f"evidence.startup.warmStartSamplesMs[{index}]", positive=True)
            for index, value in enumerate(_array(startup["warmStartSamplesMs"], "evidence.startup.warmStartSamplesMs"))]
    if len(cold) != profile["capture"]["coldStartSampleCount"]:
        raise GateValidationError("exactly 5 cold-start samples are required")
    if len(warm) != profile["capture"]["warmStartSampleCount"]:
        raise GateValidationError("exactly 5 warm-start samples are required")
    startup_recomputed = {
        "coldP50Ms": percentile(cold, 50), "coldP95Ms": percentile(cold, 95), "coldMaximumMs": max(cold),
        "warmP50Ms": percentile(warm, 50), "warmP95Ms": percentile(warm, 95), "warmMaximumMs": max(warm),
    }
    for key, value in startup_recomputed.items():
        _assert_close(startup[key], value, f"evidence.startup.{key}")

    sustained = _object(report["sustainedRun"], "evidence.sustainedRun")
    sustained_keys = {
        "source", "startupFramesExcluded", "warmupSeconds", "sampleDurationSeconds", "frameTimesMs",
        "averageFrameMs", "p95FrameMs", "p99FrameMs", "maximumFrameMs", "p95CpuFrameMs",
        "p95GpuFrameMs", "peakAllocatedMemoryMB", "peakMonoMemoryMB"
    }
    _exact_keys(sustained, sustained_keys, "evidence.sustainedRun")
    if sustained["source"] != "structured-per-frame-recorder":
        raise GateValidationError("aggregate diagnostic log lines cannot satisfy APH-803")
    if sustained["startupFramesExcluded"] is not True:
        raise GateValidationError("startup frames must be excluded from sustained percentiles")
    if _number(sustained["warmupSeconds"], "evidence.sustainedRun.warmupSeconds") < profile["capture"]["warmupSeconds"]:
        raise GateValidationError("sustained warmup is shorter than the reference profile")
    if _number(sustained["sampleDurationSeconds"], "evidence.sustainedRun.sampleDurationSeconds") < profile["capture"]["sustainedSampleSeconds"]:
        raise GateValidationError("sustained sample duration is shorter than the reference profile")
    frames = [_number(value, f"evidence.sustainedRun.frameTimesMs[{index}]", positive=True)
              for index, value in enumerate(_array(sustained["frameTimesMs"], "evidence.sustainedRun.frameTimesMs"))]
    if len(frames) < profile["capture"]["minimumFrameSamples"]:
        raise GateValidationError("sustained run has too few structured frame samples")
    frame_metrics = {
        "averageFrameMs": sum(frames) / len(frames),
        "p95FrameMs": percentile(frames, 95),
        "p99FrameMs": percentile(frames, 99),
        "maximumFrameMs": max(frames),
    }
    for key, value in frame_metrics.items():
        _assert_close(sustained[key], value, f"evidence.sustainedRun.{key}")
    for key in ("p95CpuFrameMs", "p95GpuFrameMs", "peakAllocatedMemoryMB", "peakMonoMemoryMB"):
        _number(sustained[key], f"evidence.sustainedRun.{key}")

    thermal = _object(report["thermal"], "evidence.thermal")
    _exact_keys(thermal, {"parser", "snapshots"}, "evidence.thermal")
    if thermal["parser"] != "dumpsys-thermalservice-v1":
        raise GateValidationError("thermal parser contract mismatch")
    snapshots = _array(thermal["snapshots"], "evidence.thermal.snapshots")
    phase_counts = {phase: 0 for phase in profile["capture"]["requiredThermalPhases"]}
    max_status = profile["limits"]["maximumThermalStatus"]
    max_cooling = profile["limits"]["maximumCoolingDeviceValue"]
    for index, snapshot_value in enumerate(snapshots):
        path = f"evidence.thermal.snapshots[{index}]"
        snapshot = _object(snapshot_value, path)
        _exact_keys(snapshot, {"phase", "status", "coolingDevices", "temperatures"}, path)
        phase = _string(snapshot["phase"], f"{path}.phase")
        if phase not in phase_counts:
            raise GateValidationError(f"{path}.phase is not allowed")
        phase_counts[phase] += 1
        status = _integer(snapshot["status"], f"{path}.status")
        if status > max_status:
            raise GateValidationError(f"{path}.status exceeds thermal limit")
        cooling_devices = _array(snapshot["coolingDevices"], f"{path}.coolingDevices")
        temperatures = _array(snapshot["temperatures"], f"{path}.temperatures")
        if not cooling_devices or not temperatures:
            raise GateValidationError(f"{path} requires parsed cooling devices and temperatures")
        for device_index, cooling_value in enumerate(cooling_devices):
            cooling_path = f"{path}.coolingDevices[{device_index}]"
            cooling = _object(cooling_value, cooling_path)
            _exact_keys(cooling, {"name", "value"}, cooling_path)
            _string(cooling["name"], f"{cooling_path}.name")
            if _integer(cooling["value"], f"{cooling_path}.value") > max_cooling:
                raise GateValidationError(f"{cooling_path}.value exceeds cooling-device limit")
        for temperature_index, temperature_value in enumerate(temperatures):
            temperature_path = f"{path}.temperatures[{temperature_index}]"
            temperature = _object(temperature_value, temperature_path)
            _exact_keys(temperature, {"name", "valueC"}, temperature_path)
            _string(temperature["name"], f"{temperature_path}.name")
            if isinstance(temperature["valueC"], bool) or not isinstance(temperature["valueC"], (int, float)) or not math.isfinite(temperature["valueC"]):
                raise GateValidationError(f"{temperature_path}.valueC must be finite")
    if any(count < 1 for count in phase_counts.values()):
        raise GateValidationError("thermal evidence requires before, during, and after snapshots")

    artifacts = _object(report["artifacts"], "evidence.artifacts")
    _exact_keys(artifacts, {"apk", "structuredRecorder", "rawDeviceLog", "screenshot"}, "evidence.artifacts")
    apk = _validate_artifact(artifacts["apk"], "evidence.artifacts.apk", artifact_root)
    if apk["path"] != profile["build"]["apkPath"] or apk["sha256"] != expected_apk_sha256:
        raise GateValidationError("APK artifact path/hash does not match profile and provenance")
    _validate_artifact(artifacts["structuredRecorder"], "evidence.artifacts.structuredRecorder", artifact_root)
    _validate_artifact(artifacts["rawDeviceLog"], "evidence.artifacts.rawDeviceLog", artifact_root)
    screenshot = _validate_artifact(
        artifacts["screenshot"], "evidence.artifacts.screenshot", artifact_root,
        {"capturedPackage", "width", "height"}
    )
    if screenshot["capturedPackage"] != profile["build"]["packageName"]:
        raise GateValidationError("screenshot package does not match the reference profile")
    if screenshot["width"] != profile["device"]["resolutionWidth"] or screenshot["height"] != profile["device"]["resolutionHeight"]:
        raise GateValidationError("screenshot dimensions do not match the reference device")

    detected_fatal_markers: list[str] = []
    if artifact_root is not None:
        recorder_path = _resolve_artifact_path(
            artifacts["structuredRecorder"]["path"], artifact_root
        )
        recorder_payload = load_json(recorder_path)
        if recorder_payload != sustained:
            raise GateValidationError(
                "structured recorder artifact does not exactly match sustainedRun evidence"
            )

        raw_log_path = _resolve_artifact_path(artifacts["rawDeviceLog"]["path"], artifact_root)
        try:
            raw_log_text = raw_log_path.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeDecodeError) as exc:
            raise GateValidationError(f"raw device log is not readable text: {raw_log_path}") from exc
        detected_fatal_markers = detect_fatal_markers(raw_log_text)

        screenshot_path = _resolve_artifact_path(screenshot["path"], artifact_root)
        png_width, png_height = read_png_dimensions(screenshot_path)
        if png_width != screenshot["width"] or png_height != screenshot["height"]:
            raise GateValidationError("reported screenshot dimensions do not match PNG data")

    crash = _object(report["crashScan"], "evidence.crashScan")
    _exact_keys(crash, {"processSurvived", "fatalMarkers"}, "evidence.crashScan")
    if crash["processSurvived"] is not True:
        raise GateValidationError("application process did not survive the sustained run")
    fatal_markers = _array(crash["fatalMarkers"], "evidence.crashScan.fatalMarkers")
    if fatal_markers:
        raise GateValidationError("crash/fatal markers were found in the raw device log")
    if detected_fatal_markers:
        raise GateValidationError(
            "raw device log contains crash/fatal markers: " + ", ".join(detected_fatal_markers)
        )

    _apply_limit(frame_metrics["p95FrameMs"], profile["limits"]["p95FrameMs"], "p95 frame")
    _apply_limit(frame_metrics["p99FrameMs"], profile["limits"]["p99FrameMs"], "p99 frame")
    _apply_limit(max(startup_recomputed["coldP95Ms"], startup_recomputed["warmP95Ms"]), profile["limits"]["startupP95Ms"], "startup p95")
    _apply_limit(float(sustained["peakAllocatedMemoryMB"]), profile["limits"]["peakAllocatedMemoryMB"], "peak allocated memory")

    return {
        "result": "Passed",
        "schemaVersion": SCHEMA_VERSION,
        "taskId": TASK_ID,
        "exactCommit": expected_revision,
        "apkSha256": expected_apk_sha256,
        "deviceSerial": profile["device"]["serial"],
        "coldStartSampleCount": len(cold),
        "warmStartSampleCount": len(warm),
        "frameSampleCount": len(frames),
        "metrics": {
            **frame_metrics,
            "coldStartupP95Ms": startup_recomputed["coldP95Ms"],
            "warmStartupP95Ms": startup_recomputed["warmP95Ms"],
            "peakAllocatedMemoryMB": float(sustained["peakAllocatedMemoryMB"]),
        },
        "thermalSnapshotCount": len(snapshots),
    }


def build_orchestration_contract(profile: dict[str, Any], expected_revision: str, expected_apk_sha256: str) -> dict[str, Any]:
    if COMMIT_PATTERN.fullmatch(expected_revision) is None or SHA256_PATTERN.fullmatch(expected_apk_sha256) is None:
        raise GateValidationError("orchestration contract requires exact lowercase revision and APK SHA-256")
    capture = profile["capture"]
    startup_runs = [
        {"id": f"cold-{index:02d}", "kind": "cold", "clearApplicationData": True}
        for index in range(1, capture["coldStartSampleCount"] + 1)
    ] + [
        {"id": f"warm-{index:02d}", "kind": "warm", "clearApplicationData": False}
        for index in range(1, capture["warmStartSampleCount"] + 1)
    ]
    unset_limits = [
        name
        for name in ("p99FrameMs", "startupP95Ms")
        if profile["limits"][name]["value"] is None
    ]
    return {
        "schemaVersion": SCHEMA_VERSION,
        "taskId": TASK_ID,
        "mode": "contract-only-no-adb-execution",
        "exactCommit": expected_revision,
        "apkSha256": expected_apk_sha256,
        "deviceSerial": profile["device"]["serial"],
        "acceptanceReady": not unset_limits,
        "unsetLimits": unset_limits,
        "startupRuns": startup_runs,
        "sustainedRun": {
            "warmupSeconds": capture["warmupSeconds"],
            "sampleDurationSeconds": capture["sustainedSampleSeconds"],
            "minimumFrameSamples": capture["minimumFrameSamples"],
        },
        "requiredCollections": [
            "device-properties", "physical-resolution", "structured-recorder", "thermal-before",
            "thermal-during", "thermal-after", "raw-device-log", "screenshot", "process-survival"
        ],
    }


def _write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--profile", type=Path, default=DEFAULT_PROFILE)
    subparsers = parser.add_subparsers(dest="command", required=True)

    contract = subparsers.add_parser("contract", help="write the deterministic collection contract without running ADB")
    contract.add_argument("--expected-revision", required=True)
    contract.add_argument("--expected-apk-sha256", required=True)
    contract.add_argument("--output-json", type=Path, required=True)

    validate = subparsers.add_parser("validate", help="validate already-collected evidence")
    validate.add_argument("--evidence", type=Path, required=True)
    validate.add_argument("--expected-revision", required=True)
    validate.add_argument("--expected-apk-sha256", required=True)
    validate.add_argument("--artifact-root", type=Path, required=True)
    validate.add_argument("--output-json", type=Path, required=True)
    args = parser.parse_args()

    try:
        profile = load_profile(args.profile)
        if args.command == "contract":
            result = build_orchestration_contract(
                profile, args.expected_revision, args.expected_apk_sha256.lower()
            )
        else:
            result = validate_evidence(
                load_json(args.evidence),
                profile,
                expected_revision=args.expected_revision,
                expected_apk_sha256=args.expected_apk_sha256,
                artifact_root=args.artifact_root,
            )
        _write_json(args.output_json, result)
    except (OSError, GateValidationError) as exc:
        print(f"[APH-803 AndroidDevelopmentGate] result=Failed reason={exc}")
        return 1

    print(
        f"[APH-803 AndroidDevelopmentGate] result={result.get('result', 'ContractGenerated')} "
        f"revision={result['exactCommit']} device={result['deviceSerial']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
