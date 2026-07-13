#!/usr/bin/env python3
"""Shared strict validator for Android performance evidence contracts."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable


SCHEMA_VERSION = 1
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


@dataclass(frozen=True)
class GatePolicy:
    task_id: str
    marker: str
    build_kind: str

    @property
    def is_release(self) -> bool:
        return self.build_kind == "release"


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


def _validate_comparison(value: Any, path: str) -> str:
    result = _string(value, path)
    if result not in {"lessThan", "lessThanOrEqual"}:
        raise GateValidationError(f"{path} has unsupported comparison: {result}")
    return result


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


def _validate_measurement_required_limit(value: Any, path: str) -> None:
    item = _object(value, path)
    _exact_keys(item, {"comparison", "value", "status", "blocking"}, path)
    _validate_comparison(item["comparison"], f"{path}.comparison")
    if item["value"] is not None or item["status"] != "measurement-required" or item["blocking"] is not False:
        raise GateValidationError(f"{path} must remain measurement-required and non-blocking")


def load_profile(path: Path, policy: GatePolicy) -> dict[str, Any]:
    profile = load_json(path)
    root_keys = {"schemaVersion", "taskId", "device", "build", "capture", "limits"}
    if policy.is_release:
        root_keys.add("observations")
    _exact_keys(profile, root_keys, "profile")
    if profile["schemaVersion"] != SCHEMA_VERSION or profile["taskId"] != policy.task_id:
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
        "qualityTier", "requiredLaunchArguments"
    }
    build_keys.add("requestedFrameRate" if policy.is_release else "frameRateMode")
    if policy.is_release:
        build_keys.add("actualFrameRate")
    _exact_keys(build, build_keys, "profile.build")
    numeric_build_keys = {"requestedFrameRate", "actualFrameRate"} if policy.is_release else {"frameRateMode"}
    for key in build_keys - numeric_build_keys - {"requiredLaunchArguments"}:
        _string(build[key], f"profile.build.{key}")
    for key in numeric_build_keys:
        _integer(build[key], f"profile.build.{key}", minimum=1)
    if build["buildType"] != policy.build_kind:
        raise GateValidationError(f"profile.build.buildType must be {policy.build_kind}")
    arguments = _array(build["requiredLaunchArguments"], "profile.build.requiredLaunchArguments")
    if not arguments or len(arguments) != len(set(arguments)):
        raise GateValidationError("profile.build.requiredLaunchArguments must be non-empty and unique")
    for index, argument in enumerate(arguments):
        _string(argument, f"profile.build.requiredLaunchArguments[{index}]")
    if policy.is_release:
        _reject_release_arguments(arguments, "profile.build.requiredLaunchArguments")

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
    if policy.is_release:
        _validate_release_limits(limits)
        observations = _object(profile["observations"], "profile.observations")
        _exact_keys(observations, {"highEndP95FrameMs"}, "profile.observations")
        observation = _object(observations["highEndP95FrameMs"], "profile.observations.highEndP95FrameMs")
        _exact_keys(observation, {"comparison", "value", "status", "blocking"}, "profile.observations.highEndP95FrameMs")
        if (_validate_comparison(observation["comparison"], "profile.observations.highEndP95FrameMs.comparison") != "lessThan"
                or _number(observation["value"], "profile.observations.highEndP95FrameMs.value", positive=True) != 25.0
                or observation["status"] != "observation-only" or observation["blocking"] is not False):
            raise GateValidationError("high-end p95 observation must be a non-blocking <25 ms observation")
    else:
        _validate_development_limits(limits)
    return profile


def _validate_development_limits(limits: dict[str, Any]) -> None:
    _exact_keys(limits, {
        "p95FrameMs", "p99FrameMs", "startupP95Ms", "peakAllocatedMemoryMB",
        "maximumThermalStatus", "maximumCoolingDeviceValue"
    }, "profile.limits")
    _validate_limit(limits["p95FrameMs"], "profile.limits.p95FrameMs", allow_unset=False)
    _validate_limit(limits["p99FrameMs"], "profile.limits.p99FrameMs", allow_unset=True)
    _validate_limit(limits["startupP95Ms"], "profile.limits.startupP95Ms", allow_unset=True)
    memory = _object(limits["peakAllocatedMemoryMB"], "profile.limits.peakAllocatedMemoryMB")
    _exact_keys(memory, {"comparison", "value", "sourceBaselineMaximumMB", "requiredReductionPercent"}, "profile.limits.peakAllocatedMemoryMB")
    _validate_comparison(memory["comparison"], "profile.limits.peakAllocatedMemoryMB.comparison")
    _number(memory["value"], "profile.limits.peakAllocatedMemoryMB.value", positive=True)
    baseline = _number(memory["sourceBaselineMaximumMB"], "profile.limits.peakAllocatedMemoryMB.sourceBaselineMaximumMB", positive=True)
    reduction = _number(memory["requiredReductionPercent"], "profile.limits.peakAllocatedMemoryMB.requiredReductionPercent", positive=True)
    if not math.isclose(float(memory["value"]), baseline * (1.0 - reduction / 100.0), rel_tol=0, abs_tol=1e-9):
        raise GateValidationError("peak memory limit does not match its baseline reduction")
    _integer(limits["maximumThermalStatus"], "profile.limits.maximumThermalStatus")
    _integer(limits["maximumCoolingDeviceValue"], "profile.limits.maximumCoolingDeviceValue")


def _validate_release_limits(limits: dict[str, Any]) -> None:
    _exact_keys(limits, {
        "p95FrameMs", "maximumApkSizeBytes", "p99FrameMs", "startupP95Ms",
        "installedSizeBytes", "absoluteMemoryMB", "maximumThermalStatus",
        "maximumCoolingDeviceValue"
    }, "profile.limits")
    _validate_limit(limits["p95FrameMs"], "profile.limits.p95FrameMs", allow_unset=False)
    _validate_limit(limits["maximumApkSizeBytes"], "profile.limits.maximumApkSizeBytes", allow_unset=False)
    for name in ("p99FrameMs", "startupP95Ms", "installedSizeBytes", "absoluteMemoryMB"):
        _validate_measurement_required_limit(limits[name], f"profile.limits.{name}")
    _integer(limits["maximumThermalStatus"], "profile.limits.maximumThermalStatus")
    _integer(limits["maximumCoolingDeviceValue"], "profile.limits.maximumCoolingDeviceValue")


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


def _apply_limit(metric: float, limit: dict[str, Any], path: str, task_id: str) -> None:
    value = limit.get("value")
    if value is None:
        raise GateValidationError(f"{path} limit is unset; {task_id} must fail closed")
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
            raise GateValidationError(f"{path}.{key} mismatch: expected {expected[key]!r}, found {actual[key]!r}")


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
    if "sizeBytes" in item:
        _integer(item["sizeBytes"], f"{path}.sizeBytes", minimum=1)
    if artifact_root is not None:
        resolved = _resolve_artifact_path(artifact_path_text, artifact_root)
        if not resolved.is_file() or resolved.stat().st_size == 0:
            raise GateValidationError(f"{path}.path is missing or empty: {resolved}")
        if _sha256(resolved) != digest:
            raise GateValidationError(f"{path}.sha256 does not match file: {resolved}")
        if "sizeBytes" in item and resolved.stat().st_size != item["sizeBytes"]:
            raise GateValidationError(f"{path}.sizeBytes does not match file: {resolved}")
    return item


def _reject_release_arguments(arguments: list[Any], path: str) -> None:
    forbidden = ("profiler", "development", "debug")
    for index, value in enumerate(arguments):
        argument = _string(value, f"{path}[{index}]")
        if any(fragment in argument.lower() for fragment in forbidden):
            raise GateValidationError(f"{path}[{index}] contains a forbidden profiler/development/debug flag")


def _validate_startup(report: dict[str, Any], profile: dict[str, Any]) -> tuple[list[float], list[float], dict[str, float]]:
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
    recomputed = {
        "coldP50Ms": percentile(cold, 50), "coldP95Ms": percentile(cold, 95), "coldMaximumMs": max(cold),
        "warmP50Ms": percentile(warm, 50), "warmP95Ms": percentile(warm, 95), "warmMaximumMs": max(warm),
    }
    for key, value in recomputed.items():
        _assert_close(startup[key], value, f"evidence.startup.{key}")
    return cold, warm, recomputed


def _validate_sustained(report: dict[str, Any], profile: dict[str, Any], policy: GatePolicy) -> tuple[dict[str, Any], list[float], dict[str, float]]:
    sustained = _object(report["sustainedRun"], "evidence.sustainedRun")
    common_keys = {
        "source", "startupFramesExcluded", "warmupSeconds", "sampleDurationSeconds", "frameTimesMs",
        "averageFrameMs", "p95FrameMs", "p99FrameMs", "maximumFrameMs"
    }
    release_keys = {"gc", "memory", "battery", "counters"}
    development_keys = {"p95CpuFrameMs", "p95GpuFrameMs", "peakAllocatedMemoryMB", "peakMonoMemoryMB"}
    _exact_keys(sustained, common_keys | (release_keys if policy.is_release else development_keys), "evidence.sustainedRun")
    if sustained["source"] != "structured-per-frame-recorder":
        raise GateValidationError(f"aggregate diagnostic log lines cannot satisfy {policy.task_id}")
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
    metrics = {
        "averageFrameMs": sum(frames) / len(frames), "p95FrameMs": percentile(frames, 95),
        "p99FrameMs": percentile(frames, 99), "maximumFrameMs": max(frames),
    }
    for key, value in metrics.items():
        _assert_close(sustained[key], value, f"evidence.sustainedRun.{key}")
    if policy.is_release:
        _validate_release_measurements(sustained, len(frames))
    else:
        for key in development_keys:
            _number(sustained[key], f"evidence.sustainedRun.{key}")
    return sustained, frames, metrics


def _validate_release_measurements(sustained: dict[str, Any], frame_count: int) -> None:
    gc = _object(sustained["gc"], "evidence.sustainedRun.gc")
    _exact_keys(gc, {"totalAllocatedBytes", "averageAllocatedBytesPerFrame", "collectionCount"}, "evidence.sustainedRun.gc")
    _integer(gc["totalAllocatedBytes"], "evidence.sustainedRun.gc.totalAllocatedBytes")
    _number(gc["averageAllocatedBytesPerFrame"], "evidence.sustainedRun.gc.averageAllocatedBytesPerFrame")
    _integer(gc["collectionCount"], "evidence.sustainedRun.gc.collectionCount")
    _assert_close(gc["averageAllocatedBytesPerFrame"], gc["totalAllocatedBytes"] / frame_count, "evidence.sustainedRun.gc.averageAllocatedBytesPerFrame")

    memory = _object(sustained["memory"], "evidence.sustainedRun.memory")
    _exact_keys(memory, {"peakAllocatedMemoryMB", "peakMonoMemoryMB", "peakResidentSetMB"}, "evidence.sustainedRun.memory")
    for key in memory:
        _number(memory[key], f"evidence.sustainedRun.memory.{key}")

    battery = _object(sustained["battery"], "evidence.sustainedRun.battery")
    _exact_keys(battery, {"startPercent", "endPercent", "drainPercent"}, "evidence.sustainedRun.battery")
    start = _number(battery["startPercent"], "evidence.sustainedRun.battery.startPercent")
    end = _number(battery["endPercent"], "evidence.sustainedRun.battery.endPercent")
    if start > 100 or end > 100 or end > start:
        raise GateValidationError("battery percentages must be within 0..100 and cannot increase")
    _assert_close(battery["drainPercent"], start - end, "evidence.sustainedRun.battery.drainPercent")

    counters = _object(sustained["counters"], "evidence.sustainedRun.counters")
    counter_keys = {
        "cpuTimingSampleCount", "gpuTimingSampleCount", "averageCpuFrameMs", "p95CpuFrameMs",
        "averageGpuFrameMs", "p95GpuFrameMs", "averageBatches", "averageSetPassCalls",
        "averageTriangles", "averageVertices"
    }
    _exact_keys(counters, counter_keys, "evidence.sustainedRun.counters")
    for key in ("cpuTimingSampleCount", "gpuTimingSampleCount"):
        count = _integer(counters[key], f"evidence.sustainedRun.counters.{key}", minimum=1)
        if count > frame_count:
            raise GateValidationError(f"evidence.sustainedRun.counters.{key} exceeds frame count")
    for key in counter_keys - {"cpuTimingSampleCount", "gpuTimingSampleCount"}:
        _number(counters[key], f"evidence.sustainedRun.counters.{key}")


def _validate_thermal(report: dict[str, Any], profile: dict[str, Any]) -> int:
    thermal = _object(report["thermal"], "evidence.thermal")
    _exact_keys(thermal, {"parser", "snapshots"}, "evidence.thermal")
    if thermal["parser"] != "dumpsys-thermalservice-v1":
        raise GateValidationError("thermal parser contract mismatch")
    snapshots = _array(thermal["snapshots"], "evidence.thermal.snapshots")
    phase_counts = {phase: 0 for phase in profile["capture"]["requiredThermalPhases"]}
    for index, snapshot_value in enumerate(snapshots):
        path = f"evidence.thermal.snapshots[{index}]"
        snapshot = _object(snapshot_value, path)
        _exact_keys(snapshot, {"phase", "status", "coolingDevices", "temperatures"}, path)
        phase = _string(snapshot["phase"], f"{path}.phase")
        if phase not in phase_counts:
            raise GateValidationError(f"{path}.phase is not allowed")
        phase_counts[phase] += 1
        if _integer(snapshot["status"], f"{path}.status") > profile["limits"]["maximumThermalStatus"]:
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
            if _integer(cooling["value"], f"{cooling_path}.value") > profile["limits"]["maximumCoolingDeviceValue"]:
                raise GateValidationError(f"{cooling_path}.value exceeds cooling-device limit")
        for temperature_index, temperature_value in enumerate(temperatures):
            temperature_path = f"{path}.temperatures[{temperature_index}]"
            temperature = _object(temperature_value, temperature_path)
            _exact_keys(temperature, {"name", "valueC"}, temperature_path)
            _string(temperature["name"], f"{temperature_path}.name")
            value = temperature["valueC"]
            if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
                raise GateValidationError(f"{temperature_path}.valueC must be finite")
    if any(count < 1 for count in phase_counts.values()):
        raise GateValidationError("thermal evidence requires before, during, and after snapshots")
    return len(snapshots)


def _validate_recorder(recorder: dict[str, Any], sustained: dict[str, Any], frames: list[float], policy: GatePolicy) -> None:
    common_keys = {
        "schemaVersion", "taskId", "complete", "failure", "launchRealtimeSeconds",
        "matchReadyRealtimeSeconds", "processToMatchReadyMs", "cpuTimingSampleCount",
        "gpuTimingSampleCount", "sustainedRun"
    }
    release_keys = {
        "recorderMode", "buildType", "developmentBuild", "scriptDebugging",
        "profilerAttached", "profilerMarkersEnabled"
    }
    _exact_keys(recorder, common_keys | (release_keys if policy.is_release else set()), "structuredRecorder")
    if recorder["schemaVersion"] != SCHEMA_VERSION or recorder["taskId"] != policy.task_id:
        raise GateValidationError("structured recorder schemaVersion/taskId mismatch")
    if recorder["complete"] is not True or recorder["failure"] != "":
        raise GateValidationError("structured recorder did not complete successfully")
    if policy.is_release:
        expected = {
            "recorderMode": "release-performance-evidence", "buildType": "release",
            "developmentBuild": False, "scriptDebugging": False,
            "profilerAttached": False, "profilerMarkersEnabled": False,
        }
        for key, value in expected.items():
            if recorder[key] != value:
                raise GateValidationError(f"structured recorder {key} does not attest release-mode evidence")
    launch_seconds = _number(recorder["launchRealtimeSeconds"], "structuredRecorder.launchRealtimeSeconds")
    match_ready_seconds = _number(recorder["matchReadyRealtimeSeconds"], "structuredRecorder.matchReadyRealtimeSeconds", positive=True)
    process_to_match_ms = _number(recorder["processToMatchReadyMs"], "structuredRecorder.processToMatchReadyMs", positive=True)
    _assert_close(process_to_match_ms, (match_ready_seconds - launch_seconds) * 1000.0, "structuredRecorder.processToMatchReadyMs")
    for prefix, key in (("CPU", "cpuTimingSampleCount"), ("GPU", "gpuTimingSampleCount")):
        if _integer(recorder[key], f"structuredRecorder.{key}", minimum=1) > len(frames):
            raise GateValidationError(f"structured recorder {prefix} timing count exceeds frame count")
    if recorder["sustainedRun"] != sustained:
        raise GateValidationError("structured recorder sustainedRun does not exactly match sustainedRun evidence")


def validate_evidence(
    evidence: Any,
    profile: dict[str, Any],
    *,
    expected_revision: str,
    expected_apk_sha256: str,
    artifact_root: Path | None,
    policy: GatePolicy,
) -> dict[str, Any]:
    if policy.is_release and artifact_root is None:
        raise GateValidationError("release evidence requires artifact files and release-recorder verification")
    report = _object(evidence, "evidence")
    root_keys = {
        "schemaVersion", "taskId", "provenance", "device", "build", "startup",
        "sustainedRun", "thermal", "artifacts", "crashScan"
    }
    if policy.is_release:
        root_keys.add("installedSizeBytes")
    _exact_keys(report, root_keys, "evidence")
    if report["schemaVersion"] != SCHEMA_VERSION or report["taskId"] != policy.task_id:
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
        source = "release" if policy.is_release else "development gate"
        raise GateValidationError(f"{source} evidence must come from a clean revision")
    if provenance["apkSha256"] != expected_apk_sha256:
        raise GateValidationError("evidence APK SHA-256 does not match the requested artifact")

    _validate_exact_mapping(_object(report["device"], "evidence.device"), profile["device"], set(profile["device"]), "evidence.device")
    build = _object(report["build"], "evidence.build")
    build_keys = set(profile["build"]) - {"requiredLaunchArguments"} | {"launchArguments"}
    _exact_keys(build, build_keys, "evidence.build")
    for key in build_keys - {"launchArguments"}:
        if build[key] != profile["build"][key]:
            raise GateValidationError(f"evidence.build.{key} mismatch")
    arguments = _array(build["launchArguments"], "evidence.build.launchArguments")
    if arguments != profile["build"]["requiredLaunchArguments"]:
        raise GateValidationError("evidence.build.launchArguments mismatch")
    if policy.is_release:
        _reject_release_arguments(arguments, "evidence.build.launchArguments")

    cold, warm, startup_metrics = _validate_startup(report, profile)
    sustained, frames, frame_metrics = _validate_sustained(report, profile, policy)
    thermal_count = _validate_thermal(report, profile)

    artifacts = _object(report["artifacts"], "evidence.artifacts")
    _exact_keys(artifacts, {"apk", "structuredRecorder", "rawDeviceLog", "screenshot"}, "evidence.artifacts")
    apk_extra = {"sizeBytes"} if policy.is_release else set()
    apk = _validate_artifact(artifacts["apk"], "evidence.artifacts.apk", artifact_root, apk_extra)
    if apk["path"] != profile["build"]["apkPath"] or apk["sha256"] != expected_apk_sha256:
        raise GateValidationError("APK artifact path/hash does not match profile and provenance")
    _validate_artifact(artifacts["structuredRecorder"], "evidence.artifacts.structuredRecorder", artifact_root)
    _validate_artifact(artifacts["rawDeviceLog"], "evidence.artifacts.rawDeviceLog", artifact_root)
    screenshot = _validate_artifact(artifacts["screenshot"], "evidence.artifacts.screenshot", artifact_root, {"capturedPackage", "width", "height"})
    if screenshot["capturedPackage"] != profile["build"]["packageName"]:
        raise GateValidationError("screenshot package does not match the reference profile")
    if screenshot["width"] != profile["device"]["resolutionWidth"] or screenshot["height"] != profile["device"]["resolutionHeight"]:
        raise GateValidationError("screenshot dimensions do not match the reference device")

    detected_fatal_markers: list[str] = []
    if artifact_root is not None:
        recorder = load_json(_resolve_artifact_path(artifacts["structuredRecorder"]["path"], artifact_root))
        _validate_recorder(recorder, sustained, frames, policy)
        raw_log_path = _resolve_artifact_path(artifacts["rawDeviceLog"]["path"], artifact_root)
        try:
            detected_fatal_markers = detect_fatal_markers(raw_log_path.read_text(encoding="utf-8-sig"))
        except (OSError, UnicodeDecodeError) as exc:
            raise GateValidationError(f"raw device log is not readable text: {raw_log_path}") from exc
        png_width, png_height = read_png_dimensions(_resolve_artifact_path(screenshot["path"], artifact_root))
        if png_width != screenshot["width"] or png_height != screenshot["height"]:
            raise GateValidationError("reported screenshot dimensions do not match PNG data")

    crash = _object(report["crashScan"], "evidence.crashScan")
    _exact_keys(crash, {"processSurvived", "fatalMarkers"}, "evidence.crashScan")
    if crash["processSurvived"] is not True:
        raise GateValidationError("application process did not survive the sustained run")
    if _array(crash["fatalMarkers"], "evidence.crashScan.fatalMarkers"):
        raise GateValidationError("crash/fatal markers were found in the raw device log")
    if detected_fatal_markers:
        raise GateValidationError("raw device log contains crash/fatal markers: " + ", ".join(detected_fatal_markers))

    _apply_limit(frame_metrics["p95FrameMs"], profile["limits"]["p95FrameMs"], "p95 frame", policy.task_id)
    if policy.is_release:
        installed_size = _integer(report["installedSizeBytes"], "evidence.installedSizeBytes", minimum=1)
        _apply_limit(float(apk["sizeBytes"]), profile["limits"]["maximumApkSizeBytes"], "APK size", policy.task_id)
        observation = profile["observations"]["highEndP95FrameMs"]
        high_end_observed = frame_metrics["p95FrameMs"] < float(observation["value"])
        return {
            "result": "Passed", "acceptanceReady": True, "schemaVersion": SCHEMA_VERSION,
            "taskId": policy.task_id, "exactCommit": expected_revision, "apkSha256": expected_apk_sha256,
            "deviceSerial": profile["device"]["serial"], "coldStartSampleCount": len(cold),
            "warmStartSampleCount": len(warm), "frameSampleCount": len(frames),
            "metrics": {**frame_metrics, "coldStartupP95Ms": startup_metrics["coldP95Ms"],
                        "warmStartupP95Ms": startup_metrics["warmP95Ms"], "apkSizeBytes": apk["sizeBytes"],
                        "installedSizeBytes": installed_size, "gc": sustained["gc"], "memory": sustained["memory"],
                        "battery": sustained["battery"], "counters": sustained["counters"]},
            "highEndObservation": {"name": "p95FrameMs", "comparison": "lessThan", "thresholdMs": 25.0,
                                   "observed": high_end_observed, "blocking": False},
            "measurementRequiredLimits": ["p99FrameMs", "startupP95Ms", "installedSizeBytes", "absoluteMemoryMB"],
            "thermalSnapshotCount": thermal_count,
        }

    _apply_limit(frame_metrics["p99FrameMs"], profile["limits"]["p99FrameMs"], "p99 frame", policy.task_id)
    _apply_limit(max(startup_metrics["coldP95Ms"], startup_metrics["warmP95Ms"]), profile["limits"]["startupP95Ms"], "startup p95", policy.task_id)
    _apply_limit(float(sustained["peakAllocatedMemoryMB"]), profile["limits"]["peakAllocatedMemoryMB"], "peak allocated memory", policy.task_id)
    return {
        "result": "Passed", "schemaVersion": SCHEMA_VERSION, "taskId": policy.task_id,
        "exactCommit": expected_revision, "apkSha256": expected_apk_sha256,
        "deviceSerial": profile["device"]["serial"], "coldStartSampleCount": len(cold),
        "warmStartSampleCount": len(warm), "frameSampleCount": len(frames),
        "metrics": {**frame_metrics, "coldStartupP95Ms": startup_metrics["coldP95Ms"],
                    "warmStartupP95Ms": startup_metrics["warmP95Ms"],
                    "peakAllocatedMemoryMB": float(sustained["peakAllocatedMemoryMB"])},
        "thermalSnapshotCount": thermal_count,
    }


def build_orchestration_contract(profile: dict[str, Any], expected_revision: str, expected_apk_sha256: str, policy: GatePolicy) -> dict[str, Any]:
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
    if policy.is_release:
        return {
            "schemaVersion": SCHEMA_VERSION, "taskId": policy.task_id,
            "mode": "contract-only-no-adb-execution", "exactCommit": expected_revision,
            "apkSha256": expected_apk_sha256, "deviceSerial": profile["device"]["serial"],
            "acceptanceReady": False,
            "unmetAcceptanceRequirements": ["release-mode-structured-recorder", "validated-release-device-evidence"],
            "measurementRequiredLimits": ["p99FrameMs", "startupP95Ms", "installedSizeBytes", "absoluteMemoryMB"],
            "startupRuns": startup_runs,
            "sustainedRun": {"warmupSeconds": capture["warmupSeconds"],
                             "sampleDurationSeconds": capture["sustainedSampleSeconds"],
                             "minimumFrameSamples": capture["minimumFrameSamples"]},
            "releaseRecorderRequirements": {
                "recorderMode": "release-performance-evidence", "buildType": "release",
                "developmentBuild": False, "scriptDebugging": False,
                "profilerAttached": False, "profilerMarkersEnabled": False,
            },
            "requiredCollections": [
                "device-properties", "physical-resolution", "release-structured-recorder", "startup",
                "per-frame-times", "gc", "absolute-memory", "battery", "cpu-gpu-render-counters",
                "installed-size", "apk-size", "thermal-before", "thermal-during", "thermal-after",
                "raw-device-log", "screenshot", "process-survival"
            ],
        }
    unset_limits = [name for name in ("p99FrameMs", "startupP95Ms") if profile["limits"][name]["value"] is None]
    return {
        "schemaVersion": SCHEMA_VERSION, "taskId": policy.task_id,
        "mode": "contract-only-no-adb-execution", "exactCommit": expected_revision,
        "apkSha256": expected_apk_sha256, "deviceSerial": profile["device"]["serial"],
        "acceptanceReady": not unset_limits, "unsetLimits": unset_limits, "startupRuns": startup_runs,
        "sustainedRun": {"warmupSeconds": capture["warmupSeconds"],
                         "sampleDurationSeconds": capture["sustainedSampleSeconds"],
                         "minimumFrameSamples": capture["minimumFrameSamples"]},
        "requiredCollections": [
            "device-properties", "physical-resolution", "structured-recorder", "thermal-before",
            "thermal-during", "thermal-after", "raw-device-log", "screenshot", "process-survival"
        ],
    }


def _write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def run_cli(policy: GatePolicy, default_profile: Path, description: str) -> int:
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument("--profile", type=Path, default=default_profile)
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
        profile = load_profile(args.profile, policy)
        if args.command == "contract":
            result = build_orchestration_contract(profile, args.expected_revision, args.expected_apk_sha256.lower(), policy)
        else:
            result = validate_evidence(load_json(args.evidence), profile, expected_revision=args.expected_revision,
                                       expected_apk_sha256=args.expected_apk_sha256,
                                       artifact_root=args.artifact_root, policy=policy)
        _write_json(args.output_json, result)
    except (OSError, GateValidationError) as exc:
        print(f"[{policy.marker}] result=Failed reason={exc}")
        return 1
    print(f"[{policy.marker}] result={result.get('result', 'ContractGenerated')} "
          f"revision={result['exactCommit']} device={result['deviceSerial']}")
    return 0
