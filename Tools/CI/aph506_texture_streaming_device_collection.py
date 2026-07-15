#!/usr/bin/env python3
"""Collect fail-closed APH-506 Android texture-streaming pilot evidence."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import re
import tempfile
import uuid
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Sequence

try:
    from Tools.CI.android_development_device_collection import (
        RepositoryBoundary,
        SubprocessRepository,
        install_and_verify,
        launch_argv,
        parse_match_ready_markers,
        parse_recorder_complete_markers,
    )
    from Tools.CI.android_development_performance_gate import (
        DEFAULT_PROFILE,
        load_profile,
    )
    from Tools.CI.android_performance_evidence_gate import (
        detect_fatal_markers,
        read_png_dimensions,
    )
    from Tools.CI.android_release_device_collection import (
        AdbBoundary,
        Clock,
        CollectionError,
        CommandResult,
        LogcatSession,
        SubprocessAdb,
        SystemClock,
        collect_device_identity,
        parse_foreground_component,
        parse_pid,
        parse_thermal_snapshot,
        require_exact_target,
        require_idle_thermal,
        require_unplugged_battery,
        sha256_file,
    )
except ModuleNotFoundError:  # Direct execution adds Tools/CI, not the repository root.
    from android_development_device_collection import (
        RepositoryBoundary,
        SubprocessRepository,
        install_and_verify,
        launch_argv,
        parse_match_ready_markers,
        parse_recorder_complete_markers,
    )
    from android_development_performance_gate import DEFAULT_PROFILE, load_profile
    from android_performance_evidence_gate import detect_fatal_markers, read_png_dimensions
    from android_release_device_collection import (
        AdbBoundary,
        Clock,
        CollectionError,
        CommandResult,
        LogcatSession,
        SubprocessAdb,
        SystemClock,
        collect_device_identity,
        parse_foreground_component,
        parse_pid,
        parse_thermal_snapshot,
        require_exact_target,
        require_idle_thermal,
        require_unplugged_battery,
        sha256_file,
    )


TASK_ID = "APH-506"
SCHEMA_VERSION = 1
DEFAULT_WARMUP_SECONDS = 60.0
DEFAULT_SESSION_SECONDS = 600.0
DEFAULT_SAMPLE_INTERVAL_SECONDS = 5.0
DEFAULT_GESTURE_INTERVAL_SECONDS = 5.0
MIN_WARMUP_SECONDS = 60.0
MAX_WARMUP_SECONDS = 300.0
MIN_SESSION_SECONDS = 600.0
MAX_SESSION_SECONDS = 1800.0
MIN_INTERVAL_SECONDS = 2.0
MAX_INTERVAL_SECONDS = 30.0
MAX_SAMPLE_COUNT = 512
MAX_GESTURE_COUNT = 1024
MAX_THERMAL_RECORDS = 64
MAX_ARTIFACT_BYTES = 64 * 1024 * 1024
MAX_APK_BYTES = 2 * 1024 * 1024 * 1024
MAX_EVIDENCE_BYTES = 8 * 1024 * 1024
MAX_COUNTER_VALUE = (1 << 63) - 1
REVISION_PATTERN = re.compile(r"^[0-9a-f]{40}$")
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
CAPTURE_ID_PATTERN = re.compile(r"^[0-9a-f]{32}$")
UTC_PATTERN = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$")

PILOT_TEXTURE_PATHS = (
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png",
)

RAW_LOG_FILE_NAME = "aph506_texture_streaming_device.log"
BEFORE_SCREENSHOT_FILE_NAME = "aph506_texture_streaming_before.png"
AFTER_SCREENSHOT_FILE_NAME = "aph506_texture_streaming_after.png"
EVIDENCE_FILE_NAME = "aph506_texture_streaming_evidence.json"
OWNED_ARTIFACT_NAMES = (
    RAW_LOG_FILE_NAME,
    BEFORE_SCREENSHOT_FILE_NAME,
    AFTER_SCREENSHOT_FILE_NAME,
    EVIDENCE_FILE_NAME,
)

IO_KEYS = (
    "rchar",
    "wchar",
    "syscr",
    "syscw",
    "read_bytes",
    "write_bytes",
    "cancelled_write_bytes",
)


@dataclass(frozen=True)
class CollectionConfig:
    warmup_seconds: float = DEFAULT_WARMUP_SECONDS
    session_seconds: float = DEFAULT_SESSION_SECONDS
    sample_interval_seconds: float = DEFAULT_SAMPLE_INTERVAL_SECONDS
    gesture_interval_seconds: float = DEFAULT_GESTURE_INTERVAL_SECONDS


@dataclass(frozen=True)
class PreinstallInputs:
    project_root: Path
    apk_path: Path
    apk_sha256: str
    apk_size_bytes: int
    profile: dict[str, Any]
    expected_revision: str
    candidates: list[dict[str, str]]


@dataclass(frozen=True)
class PilotCollection:
    raw_log_path: Path
    before_screenshot_path: Path
    after_screenshot_path: Path
    initial_pid: int
    warmup_measured_seconds: float
    session_measured_seconds: float
    samples: list[dict[str, Any]]
    gestures: list[dict[str, Any]]


def _text(value: bytes | str) -> str:
    if isinstance(value, str):
        return value
    try:
        return value.decode("utf-8")
    except UnicodeDecodeError as exc:
        raise CollectionError("ADB output is not valid UTF-8 text") from exc


def _checked(result: CommandResult, label: str) -> CommandResult:
    if result.returncode != 0:
        detail = (_text(result.stderr) or _text(result.stdout)).strip()
        raise CollectionError(f"{label} failed with exit code {result.returncode}: {detail}")
    return result


def _run(
    adb: AdbBoundary,
    arguments: Sequence[str],
    label: str,
    *,
    timeout: float = 60.0,
) -> CommandResult:
    return _checked(adb.run(arguments, timeout=timeout), label)


def _require_success_word(result: CommandResult, label: str) -> None:
    _checked(result, label)
    if _text(result.stdout).strip() != "Success":
        raise CollectionError(f"{label} did not return exact Success")


def _finite_number(value: Any, label: str) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise CollectionError(f"{label} must be numeric")
    result = float(value)
    if not math.isfinite(result):
        raise CollectionError(f"{label} must be finite")
    return result


def scheduled_offsets(duration_seconds: float, interval_seconds: float) -> list[float]:
    """Return stable offsets including both zero and the exact duration."""
    duration = _finite_number(duration_seconds, "duration")
    interval = _finite_number(interval_seconds, "interval")
    if duration <= 0 or interval <= 0:
        raise CollectionError("duration and interval must be positive")
    offsets = [round(index * interval, 6) for index in range(int(duration // interval) + 1)]
    offsets = [value for value in offsets if value < duration]
    offsets.append(round(duration, 6))
    return offsets


def gesture_offsets(duration_seconds: float, interval_seconds: float) -> list[float]:
    duration = _finite_number(duration_seconds, "gesture duration")
    interval = _finite_number(interval_seconds, "gesture interval")
    if duration <= 0 or interval <= 0:
        raise CollectionError("gesture duration and interval must be positive")
    return [
        round(index * interval, 6)
        for index in range(int(math.ceil(duration / interval)))
        if index * interval < duration
    ]


def validate_config(config: CollectionConfig) -> CollectionConfig:
    warmup = _finite_number(config.warmup_seconds, "warmup seconds")
    session = _finite_number(config.session_seconds, "session seconds")
    sample_interval = _finite_number(config.sample_interval_seconds, "sample interval")
    gesture_interval = _finite_number(config.gesture_interval_seconds, "gesture interval")
    if not MIN_WARMUP_SECONDS <= warmup <= MAX_WARMUP_SECONDS:
        raise CollectionError(
            f"warmup must be within {MIN_WARMUP_SECONDS:g}..{MAX_WARMUP_SECONDS:g} seconds"
        )
    if not MIN_SESSION_SECONDS <= session <= MAX_SESSION_SECONDS:
        raise CollectionError(
            f"session must be within {MIN_SESSION_SECONDS:g}..{MAX_SESSION_SECONDS:g} seconds"
        )
    for label, value in (("sample interval", sample_interval), ("gesture interval", gesture_interval)):
        if not MIN_INTERVAL_SECONDS <= value <= MAX_INTERVAL_SECONDS:
            raise CollectionError(
                f"{label} must be within {MIN_INTERVAL_SECONDS:g}..{MAX_INTERVAL_SECONDS:g} seconds"
            )
    sample_count = len(scheduled_offsets(warmup, sample_interval)) + len(
        scheduled_offsets(session, sample_interval)
    )
    gestures = len(gesture_offsets(session, gesture_interval))
    if sample_count > MAX_SAMPLE_COUNT:
        raise CollectionError(f"configuration would exceed {MAX_SAMPLE_COUNT} samples")
    if gestures > MAX_GESTURE_COUNT:
        raise CollectionError(f"configuration would exceed {MAX_GESTURE_COUNT} gestures")
    return CollectionConfig(warmup, session, sample_interval, gesture_interval)


def _require_repository_state(
    repository: RepositoryBoundary,
    project_root: Path,
    expected_revision: str,
) -> None:
    revision = repository.head_revision(project_root)
    if revision != expected_revision:
        raise CollectionError(
            f"Git HEAD mismatch: expected {expected_revision!r}, found {revision!r}"
        )
    if repository.status_porcelain(project_root).strip():
        raise CollectionError("Git worktree must be clean for APH-506 collection")


def _candidate_identity(project_root: Path) -> list[dict[str, str]]:
    candidates: list[dict[str, str]] = []
    for relative in PILOT_TEXTURE_PATHS:
        source = (project_root / relative).resolve()
        importer = Path(str(source) + ".meta")
        try:
            source.relative_to(project_root)
            importer.relative_to(project_root)
        except ValueError as exc:
            raise CollectionError(f"pilot candidate escapes project root: {relative}") from exc
        if not source.is_file() or source.stat().st_size <= 0:
            raise CollectionError(f"pilot candidate is missing or empty: {relative}")
        if not importer.is_file() or importer.stat().st_size <= 0:
            raise CollectionError(f"pilot importer is missing or empty: {relative}.meta")
        candidates.append(
            {
                "path": relative,
                "sha256": sha256_file(source),
                "importerPath": relative + ".meta",
                "importerSha256": sha256_file(importer),
            }
        )
    return candidates


def validate_preinstall_inputs(
    project_root: Path,
    apk_path: Path,
    profile_path: Path,
    serial: str,
    expected_revision: str,
    expected_apk_sha256: str,
    repository: RepositoryBoundary | None = None,
) -> PreinstallInputs:
    root = project_root.resolve()
    apk = apk_path.resolve()
    if not root.is_dir():
        raise CollectionError(f"project root does not exist: {root}")
    if REVISION_PATTERN.fullmatch(expected_revision) is None:
        raise CollectionError("expected revision must be exactly 40 lowercase hexadecimal characters")
    if SHA256_PATTERN.fullmatch(expected_apk_sha256) is None:
        raise CollectionError("expected APK SHA-256 must be exactly 64 lowercase hexadecimal characters")

    profile = load_profile(profile_path)
    if serial != profile["device"]["serial"]:
        raise CollectionError("requested serial does not match the pinned Android profile")
    expected_apk = (root / profile["build"]["apkPath"]).resolve()
    if apk != expected_apk or not apk.is_file() or apk.stat().st_size <= 0:
        raise CollectionError("APK must be the exact non-empty artifact pinned by the Android profile")
    actual_apk_sha256 = sha256_file(apk)
    if actual_apk_sha256 != expected_apk_sha256:
        raise CollectionError(
            f"host APK SHA-256 mismatch: expected {expected_apk_sha256}, found {actual_apk_sha256}"
        )

    source = repository if repository is not None else SubprocessRepository()
    _require_repository_state(source, root, expected_revision)
    candidates = _candidate_identity(root)
    return PreinstallInputs(
        root,
        apk,
        actual_apk_sha256,
        apk.stat().st_size,
        profile,
        expected_revision,
        candidates,
    )


def parse_meminfo(output: str, expected_pid: int, expected_package: str) -> dict[str, int]:
    headers = re.findall(
        r"^\s*\*\* MEMINFO in pid (\d+) \[([^]]+)\] \*\*\s*$",
        output,
        re.MULTILINE,
    )
    if headers != [(str(expected_pid), expected_package)]:
        raise CollectionError(
            f"meminfo must identify exact PID/package {expected_pid}/{expected_package}, found {headers!r}"
        )
    totals = re.findall(
        r"^\s*TOTAL PSS:\s*(\d+)\s+TOTAL RSS:\s*(\d+)\s+TOTAL SWAP PSS:\s*(\d+)\s*$",
        output,
        re.MULTILINE,
    )
    graphics = re.findall(r"^\s*Graphics:\s*(\d+)\s+(\d+)\s*$", output, re.MULTILINE)
    if len(totals) != 1 or len(graphics) != 1:
        raise CollectionError("meminfo requires exactly one total and Graphics summary row")
    values = tuple(int(value) for value in (*totals[0], *graphics[0]))
    if any(value < 0 or value > MAX_COUNTER_VALUE for value in values):
        raise CollectionError("meminfo values are outside the supported non-negative range")
    return {
        "totalPssKb": values[0],
        "totalRssKb": values[1],
        "totalSwapPssKb": values[2],
        "graphicsPssKb": values[3],
        "graphicsRssKb": values[4],
    }


def parse_process_io(output: str) -> dict[str, int]:
    values: dict[str, int] = {}
    for raw_line in output.splitlines():
        line = raw_line.strip()
        if not line:
            continue
        match = re.fullmatch(r"([a-z_]+):\s*(-?\d+)", line)
        if match is None or match.group(1) not in IO_KEYS or match.group(1) in values:
            raise CollectionError(f"malformed, unknown, or duplicate process I/O row: {line!r}")
        value = int(match.group(2))
        if abs(value) > MAX_COUNTER_VALUE or (value < 0 and match.group(1) != "cancelled_write_bytes"):
            raise CollectionError(f"process I/O counter is outside its valid range: {match.group(1)}")
        values[match.group(1)] = value
    if tuple(values) != IO_KEYS:
        raise CollectionError(f"process I/O keys must be exact and ordered, found {tuple(values)!r}")
    return values


def _nonnegative_int(value: Any, label: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or not 0 <= value <= MAX_COUNTER_VALUE:
        raise CollectionError(f"{label} must be a bounded non-negative integer")
    return value


def _process_io_int(value: Any, key: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int) or abs(value) > MAX_COUNTER_VALUE:
        raise CollectionError(f"process I/O field {key} is not a bounded integer")
    if value < 0 and key != "cancelled_write_bytes":
        raise CollectionError(f"process I/O field {key} cannot be negative")
    return value


def _current_pid(adb: AdbBoundary, package: str) -> int:
    output = _text(_run(adb, ("shell", "pidof", package), "PID continuity").stdout)
    return parse_pid(output)


def _foreground_component(adb: AdbBoundary) -> str:
    output = _text(
        _run(
            adb,
            ("shell", "dumpsys", "activity", "activities"),
            "foreground continuity",
        ).stdout
    )
    return parse_foreground_component(output)


def _temperature_statuses(output: str) -> list[tuple[str, int]]:
    lines = output.splitlines()
    headings = [
        index
        for index, line in enumerate(lines)
        if line.strip() == "Current temperatures from HAL:"
    ]
    if len(headings) != 1:
        raise CollectionError("thermal output requires one current HAL temperature section")
    statuses: list[tuple[str, int]] = []
    for raw_line in lines[headings[0] + 1 :]:
        line = raw_line.strip()
        if line.endswith(":") and "{" not in line:
            break
        if not line:
            continue
        match = re.fullmatch(r"Temperature\{(.*)\}", line)
        if match is None:
            raise CollectionError(f"malformed current HAL temperature record: {line!r}")
        fields: dict[str, str] = {}
        for item in match.group(1).split(","):
            key, separator, value = item.strip().partition("=")
            if not separator or not key or key in fields:
                raise CollectionError(f"malformed current HAL temperature field: {item!r}")
            fields[key] = value.strip()
        name = " ".join(fields.get("mName", "").split())
        status_text = fields.get("mStatus", "")
        if not name or not status_text.isdigit():
            raise CollectionError("current HAL temperature name/status is malformed")
        statuses.append((name, int(status_text)))
    if not statuses or len({name for name, _ in statuses}) != len(statuses):
        raise CollectionError("current HAL temperature statuses must be non-empty and unique")
    return statuses


def _bounded_thermal(output: str, phase: str, profile: dict[str, Any]) -> dict[str, Any]:
    snapshot = parse_thermal_snapshot(output, phase)
    temperatures = snapshot["temperatures"]
    cooling = snapshot["coolingDevices"]
    if len(temperatures) > MAX_THERMAL_RECORDS or len(cooling) > MAX_THERMAL_RECORDS:
        raise CollectionError("thermal snapshot exceeds the bounded record count")
    for item in [*temperatures, *cooling]:
        if len(item["name"]) > 128:
            raise CollectionError("thermal record name exceeds 128 characters")
    maximum_status = profile["limits"]["maximumThermalStatus"]
    maximum_cooling = profile["limits"]["maximumCoolingDeviceValue"]
    if snapshot["status"] > maximum_status:
        raise CollectionError(
            f"thermal status {snapshot['status']} exceeds profile maximum {maximum_status}"
        )
    temperature_statuses = _temperature_statuses(output)
    if [name for name, _ in temperature_statuses] != [item["name"] for item in temperatures]:
        raise CollectionError("temperature status identities do not match parsed thermal identities")
    for item, (_, status) in zip(temperatures, temperature_statuses):
        item["status"] = status
    warned = [item["name"] for item in temperatures if item["status"] > maximum_status]
    if warned:
        raise CollectionError("temperature sensor statuses exceed the profile maximum: " + ", ".join(warned))
    active = [item["name"] for item in cooling if item["value"] > maximum_cooling]
    if active:
        raise CollectionError("cooling devices exceed the profile maximum: " + ", ".join(active))
    return {
        "status": snapshot["status"],
        "temperatures": temperatures,
        "coolingDevices": cooling,
    }


def collect_sample(
    adb: AdbBoundary,
    profile: dict[str, Any],
    initial_pid: int,
    phase: str,
    scheduled_offset_seconds: float,
    observed_offset_seconds: float,
) -> dict[str, Any]:
    package = profile["build"]["packageName"]
    component = f"{package}/{profile['build']['activity']}"
    if _current_pid(adb, package) != initial_pid:
        raise CollectionError("application PID changed or died before an APH-506 sample")
    foreground = _foreground_component(adb)
    if foreground != component:
        raise CollectionError(
            f"foreground activity changed: expected {component!r}, found {foreground!r}"
        )
    thermal_text = _text(
        _run(adb, ("shell", "dumpsys", "thermalservice"), "periodic thermal sample").stdout
    )
    meminfo_text = _text(
        _run(
            adb,
            ("shell", "dumpsys", "meminfo", "-d", str(initial_pid)),
            "periodic meminfo sample",
        ).stdout
    )
    process_io_text = _text(
        _run(
            adb,
            (
                "shell",
                "run-as",
                package,
                "cat",
                f"/proc/{initial_pid}/io",
            ),
            "periodic process I/O sample",
        ).stdout
    )
    if _current_pid(adb, package) != initial_pid:
        raise CollectionError("application PID changed or died during an APH-506 sample")
    return {
        "phase": phase,
        "scheduledOffsetSeconds": round(scheduled_offset_seconds, 6),
        "observedOffsetSeconds": round(observed_offset_seconds, 6),
        "pid": initial_pid,
        "foregroundComponent": foreground,
        "processSurvived": True,
        "thermal": _bounded_thermal(thermal_text, "during", profile),
        "meminfoKb": parse_meminfo(meminfo_text, initial_pid, package),
        "processIoCounters": parse_process_io(process_io_text),
    }


def gesture_argv(
    width: int,
    height: int,
    index: int,
) -> tuple[str, tuple[str, ...]]:
    if width <= height or height <= 0 or index < 0:
        raise CollectionError("gesture geometry requires positive landscape dimensions and index")
    center_y = int(round(height * 0.5))
    left = int(round(width * 0.30))
    inner_left = int(round(width * 0.45))
    inner_right = int(round(width * 0.55))
    right = int(round(width * 0.70))
    duration_ms = 450
    variant = index % 4
    if variant == 0:
        return "pan-left", (
            "shell", "input", "touchscreen", "swipe",
            str(right), str(center_y), str(left), str(center_y), str(duration_ms),
        )
    if variant == 2:
        return "pan-right", (
            "shell", "input", "touchscreen", "swipe",
            str(left), str(center_y), str(right), str(center_y), str(duration_ms),
        )
    if variant == 1:
        first = (inner_left, left)
        second = (inner_right, right)
        name = "zoom-in"
    else:
        first = (left, inner_left)
        second = (right, inner_right)
        name = "zoom-out"
    script = (
        f"input touchscreen swipe {first[0]} {center_y} {first[1]} {center_y} {duration_ms} & "
        f"input touchscreen swipe {second[0]} {center_y} {second[1]} {center_y} {duration_ms} & wait"
    )
    return name, ("shell", "sh", "-c", script)


def execute_gesture(
    adb: AdbBoundary,
    profile: dict[str, Any],
    index: int,
    scheduled_offset_seconds: float,
    observed_offset_seconds: float,
) -> dict[str, Any]:
    device = profile["device"]
    name, arguments = gesture_argv(
        device["resolutionWidth"],
        device["resolutionHeight"],
        index,
    )
    _run(adb, arguments, f"deterministic {name} gesture", timeout=10.0)
    return {
        "index": index,
        "scheduledOffsetSeconds": round(scheduled_offset_seconds, 6),
        "observedOffsetSeconds": round(observed_offset_seconds, 6),
        "kind": "pan" if name.startswith("pan-") else "zoom",
        "direction": name,
        "commandSha256": hashlib.sha256("\0".join(arguments).encode("utf-8")).hexdigest(),
    }


def _sleep_until(clock: Clock, deadline: float) -> None:
    remaining = deadline - clock.monotonic()
    if remaining > 0:
        clock.sleep(remaining)


def _run_sample_schedule(
    adb: AdbBoundary,
    clock: Clock,
    profile: dict[str, Any],
    initial_pid: int,
    phase: str,
    phase_started_at: float,
    offsets: list[float],
) -> list[dict[str, Any]]:
    samples: list[dict[str, Any]] = []
    for offset in offsets:
        _sleep_until(clock, phase_started_at + offset)
        observed = max(0.0, clock.monotonic() - phase_started_at)
        samples.append(
            collect_sample(adb, profile, initial_pid, phase, offset, observed)
        )
    return samples


def _run_session_schedule(
    adb: AdbBoundary,
    clock: Clock,
    profile: dict[str, Any],
    initial_pid: int,
    session_started_at: float,
    config: CollectionConfig,
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    sample_schedule = scheduled_offsets(config.session_seconds, config.sample_interval_seconds)
    gesture_schedule = gesture_offsets(config.session_seconds, config.gesture_interval_seconds)
    sample_by_offset = {value for value in sample_schedule}
    gesture_by_offset = {value for value in gesture_schedule}
    samples: list[dict[str, Any]] = []
    gestures: list[dict[str, Any]] = []
    gesture_index = 0
    for offset in sorted(sample_by_offset | gesture_by_offset):
        _sleep_until(clock, session_started_at + offset)
        if offset in sample_by_offset:
            observed = max(0.0, clock.monotonic() - session_started_at)
            samples.append(
                collect_sample(adb, profile, initial_pid, "session", offset, observed)
            )
        if offset in gesture_by_offset:
            observed = max(0.0, clock.monotonic() - session_started_at)
            gestures.append(
                execute_gesture(adb, profile, gesture_index, offset, observed)
            )
            gesture_index += 1
    return samples, gestures


def _atomic_write_bytes(path: Path, payload: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary: Path | None = None
    try:
        with tempfile.NamedTemporaryFile(dir=path.parent, delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(payload)
            stream.flush()
        temporary.replace(path)
    finally:
        if temporary is not None and temporary.exists():
            temporary.unlink()


def atomic_write_json(path: Path, value: dict[str, Any]) -> None:
    payload = (json.dumps(value, indent=2, sort_keys=True) + "\n").encode("utf-8")
    if len(payload) > MAX_EVIDENCE_BYTES:
        raise CollectionError(
            f"APH-506 evidence JSON exceeds the {MAX_EVIDENCE_BYTES}-byte bound"
        )
    _atomic_write_bytes(path, payload)


def _load_json_object(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise CollectionError(f"emitted APH-506 evidence is not valid JSON: {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise CollectionError("emitted APH-506 evidence must be a JSON object")
    return value


def _capture_screenshot(
    adb: AdbBoundary,
    profile: dict[str, Any],
    destination: Path,
    label: str,
) -> None:
    payload = _run(adb, ("exec-out", "screencap", "-p"), label, timeout=120.0).stdout
    data = payload if isinstance(payload, bytes) else payload.encode("latin-1")
    if not data or len(data) > MAX_ARTIFACT_BYTES:
        raise CollectionError(f"{label} is empty or exceeds the artifact size bound")
    _atomic_write_bytes(destination, data)
    dimensions = read_png_dimensions(destination)
    expected = (
        profile["device"]["resolutionWidth"],
        profile["device"]["resolutionHeight"],
    )
    if dimensions != expected or dimensions[0] <= dimensions[1]:
        raise CollectionError(f"{label} must be exact landscape {expected}, found {dimensions}")


def _clear_package(adb: AdbBoundary, package: str) -> None:
    _require_success_word(adb.run(("shell", "pm", "clear", package)), "pm clear")


def _force_stop(adb: AdbBoundary, package: str) -> None:
    _run(adb, ("shell", "am", "force-stop", package), "force-stop")


def _clear_logcat(adb: AdbBoundary) -> None:
    _run(adb, ("logcat", "-b", "all", "-c"), "logcat clear")


def _launch(adb: AdbBoundary, profile: dict[str, Any]) -> None:
    result = _run(adb, launch_argv(profile), "exact APH-506 pilot launch", timeout=120.0)
    statuses = re.findall(r"^Status:\s*(\S+)\s*$", _text(result.stdout), re.MULTILINE)
    if statuses != ["ok"]:
        raise CollectionError("am start -W must return exactly one successful Status row")


def _wait_for_match_ready(
    session: LogcatSession,
    clock: Clock,
    *,
    timeout_seconds: float = 180.0,
    poll_seconds: float = 0.25,
) -> float:
    deadline = clock.monotonic() + timeout_seconds
    while True:
        markers = parse_match_ready_markers(session.read_text())
        if len(markers) > 1:
            raise CollectionError("continuous log contains duplicate APH-803 MatchReady markers")
        if markers:
            return clock.monotonic()
        now = clock.monotonic()
        if now >= deadline:
            raise CollectionError("timed out waiting for the exact MatchReady marker")
        clock.sleep(min(poll_seconds, deadline - now))


def _require_live_process(adb: AdbBoundary, profile: dict[str, Any], expected_pid: int) -> None:
    package = profile["build"]["packageName"]
    component = f"{package}/{profile['build']['activity']}"
    if _current_pid(adb, package) != expected_pid:
        raise CollectionError("application process did not survive APH-506 collection")
    foreground = _foreground_component(adb)
    if foreground != component:
        raise CollectionError(
            f"application is not foreground after APH-506 collection: {foreground!r}"
        )


def collect_pilot_session(
    adb: AdbBoundary,
    clock: Clock,
    profile: dict[str, Any],
    config: CollectionConfig,
    output_dir: Path,
) -> tuple[dict[str, Any], PilotCollection]:
    package = profile["build"]["packageName"]
    battery_text = _text(
        _run(adb, ("shell", "dumpsys", "battery"), "battery preflight").stdout
    )
    battery = require_unplugged_battery(battery_text)
    preflight_thermal_text = _text(
        _run(adb, ("shell", "dumpsys", "thermalservice"), "thermal preflight").stdout
    )
    parsed_preflight_thermal = parse_thermal_snapshot(preflight_thermal_text, "before")
    require_idle_thermal(parsed_preflight_thermal)
    preflight_thermal = _bounded_thermal(preflight_thermal_text, "before", profile)

    _clear_package(adb, package)
    _force_stop(adb, package)
    _clear_logcat(adb)
    raw_log_path = output_dir / RAW_LOG_FILE_NAME
    before_path = output_dir / BEFORE_SCREENSHOT_FILE_NAME
    after_path = output_dir / AFTER_SCREENSHOT_FILE_NAME
    session = adb.start_logcat(raw_log_path)
    try:
        _launch(adb, profile)
        match_ready_at = _wait_for_match_ready(session, clock)
        initial_pid = _current_pid(adb, package)
        _require_live_process(adb, profile, initial_pid)

        warmup_samples = _run_sample_schedule(
            adb,
            clock,
            profile,
            initial_pid,
            "warmup",
            match_ready_at,
            scheduled_offsets(config.warmup_seconds, config.sample_interval_seconds),
        )
        warmup_measured = clock.monotonic() - match_ready_at
        if warmup_measured < config.warmup_seconds:
            raise CollectionError("warmup evidence is shorter than the configured duration")

        _capture_screenshot(adb, profile, before_path, "before APH-506 screenshot")
        session_started_at = clock.monotonic()
        session_samples, gestures = _run_session_schedule(
            adb,
            clock,
            profile,
            initial_pid,
            session_started_at,
            config,
        )
        session_measured = clock.monotonic() - session_started_at
        if session_measured < config.session_seconds:
            raise CollectionError("camera pan/zoom evidence is shorter than the configured duration")

        _capture_screenshot(adb, profile, after_path, "after APH-506 screenshot")
        _require_live_process(adb, profile, initial_pid)
        final_log = session.read_text()
        if len(parse_match_ready_markers(final_log)) != 1:
            raise CollectionError("raw device log must contain exactly one MatchReady marker")
        if parse_recorder_complete_markers(final_log) != [True]:
            raise CollectionError("raw device log must contain one successful APH-803 Recorder marker")
        fatals = detect_fatal_markers(final_log)
        if fatals:
            raise CollectionError("raw device log contains fatal markers: " + ", ".join(fatals))
        return (
            {"battery": battery, "thermal": preflight_thermal},
            PilotCollection(
                raw_log_path,
                before_path,
                after_path,
                initial_pid,
                warmup_measured,
                session_measured,
                warmup_samples + session_samples,
                gestures,
            ),
        )
    finally:
        session.stop()
        _force_stop(adb, package)


def _artifact_path(path: Path, project_root: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(project_root.resolve()).as_posix()
    except ValueError:
        return str(resolved)


def _artifact_record(path: Path, project_root: Path) -> dict[str, Any]:
    if not path.is_file() or path.stat().st_size <= 0:
        raise CollectionError(f"required APH-506 artifact is missing or empty: {path}")
    size = path.stat().st_size
    if size > MAX_ARTIFACT_BYTES:
        raise CollectionError(f"APH-506 artifact exceeds the size bound: {path}")
    return {
        "path": _artifact_path(path, project_root),
        "sha256": sha256_file(path),
        "sizeBytes": size,
    }


def _summaries(samples: list[dict[str, Any]]) -> dict[str, Any]:
    session_samples = [sample for sample in samples if sample["phase"] == "session"]
    if len(session_samples) < 2:
        raise CollectionError("at least two session samples are required")
    first_io = session_samples[0]["processIoCounters"]
    last_io = session_samples[-1]["processIoCounters"]
    io_delta: dict[str, int] = {}
    for key in IO_KEYS:
        previous = first_io[key]
        for sample in session_samples[1:]:
            current = sample["processIoCounters"][key]
            if key != "cancelled_write_bytes" and current < previous:
                raise CollectionError(f"process I/O counter decreased during session: {key}")
            previous = current
        io_delta[key] = last_io[key] - first_io[key]

    memory_fields = (
        "totalPssKb",
        "totalRssKb",
        "totalSwapPssKb",
        "graphicsPssKb",
        "graphicsRssKb",
    )
    memory: dict[str, dict[str, int]] = {}
    for field in memory_fields:
        values = [sample["meminfoKb"][field] for sample in session_samples]
        memory[field] = {
            "start": values[0],
            "end": values[-1],
            "minimum": min(values),
            "maximum": max(values),
        }
    statuses = [sample["thermal"]["status"] for sample in session_samples]
    sensor_statuses = [
        item["status"]
        for sample in session_samples
        for item in sample["thermal"]["temperatures"]
    ]
    temperature_values = [
        item["valueC"]
        for sample in session_samples
        for item in sample["thermal"]["temperatures"]
    ]
    cooling_values = [
        item["value"]
        for sample in session_samples
        for item in sample["thermal"]["coolingDevices"]
    ]
    return {
        "memoryKb": memory,
        "processIoDelta": io_delta,
        "thermal": {
            "maximumStatus": max(statuses),
            "maximumSensorStatus": max(sensor_statuses),
            "maximumTemperatureC": max(temperature_values),
            "maximumCoolingDeviceValue": max(cooling_values),
        },
    }


def assemble_evidence(
    inputs: PreinstallInputs,
    device: dict[str, Any],
    config: CollectionConfig,
    environment: dict[str, Any],
    collection: PilotCollection,
    *,
    capture_id: str,
    captured_at_utc: str,
) -> dict[str, Any]:
    if CAPTURE_ID_PATTERN.fullmatch(capture_id) is None:
        raise CollectionError("capture ID must be exactly 32 lowercase hexadecimal characters")
    if UTC_PATTERN.fullmatch(captured_at_utc) is None:
        raise CollectionError("capturedAtUtc must be an exact second-resolution UTC timestamp")
    raw_log = _artifact_record(collection.raw_log_path, inputs.project_root)
    before = _artifact_record(collection.before_screenshot_path, inputs.project_root)
    after = _artifact_record(collection.after_screenshot_path, inputs.project_root)
    before_width, before_height = read_png_dimensions(collection.before_screenshot_path)
    after_width, after_height = read_png_dimensions(collection.after_screenshot_path)
    before.update({"width": before_width, "height": before_height})
    after.update({"width": after_width, "height": after_height})

    build = copy.deepcopy(inputs.profile["build"])
    build["launchArguments"] = build.pop("requiredLaunchArguments")
    return {
        "schemaVersion": SCHEMA_VERSION,
        "taskId": TASK_ID,
        "collectionStatus": "complete",
        "captureId": capture_id,
        "capturedAtUtc": captured_at_utc,
        "provenance": {
            "exactCommit": inputs.expected_revision,
            "dirty": False,
            "hostApkSha256": inputs.apk_sha256,
            "deviceApkSha256": inputs.apk_sha256,
            "deviceApkHashVerified": True,
        },
        "pilotCandidates": copy.deepcopy(inputs.candidates),
        "device": copy.deepcopy(device),
        "build": build,
        "environmentPreflight": copy.deepcopy(environment),
        "capture": {
            "warmupSeconds": config.warmup_seconds,
            "sessionSeconds": config.session_seconds,
            "sampleIntervalSeconds": config.sample_interval_seconds,
            "gestureIntervalSeconds": config.gesture_interval_seconds,
            "warmupMeasuredSeconds": collection.warmup_measured_seconds,
            "sessionMeasuredSeconds": collection.session_measured_seconds,
            "initialPid": collection.initial_pid,
            "gesturePattern": ["pan-left", "zoom-in", "pan-right", "zoom-out"],
            "samples": copy.deepcopy(collection.samples),
            "gestures": copy.deepcopy(collection.gestures),
            "summary": _summaries(collection.samples),
        },
        "artifacts": {
            "apk": {
                "path": inputs.profile["build"]["apkPath"],
                "sha256": inputs.apk_sha256,
                "sizeBytes": inputs.apk_size_bytes,
            },
            "rawDeviceLog": raw_log,
            "beforeScreenshot": before,
            "afterScreenshot": after,
        },
        "survival": {
            "processSurvived": True,
            "pidStable": True,
            "foregroundStable": True,
            "fatalMarkers": [],
        },
        "acceptanceBoundary": {
            "collectorEvidenceValid": True,
            "streamingExpansionAuthorized": False,
            "decision": "measurement-only; APH-506 acceptance remains a later policy decision",
        },
    }


def _require_exact(value: Any, expected: Any, label: str) -> None:
    if type(value) is not type(expected) or value != expected:
        raise CollectionError(f"{label} mismatch: expected {expected!r}, found {value!r}")


def _resolve_artifact(path_value: Any, artifact_root: Path) -> Path:
    if not isinstance(path_value, str) or not path_value:
        raise CollectionError("artifact path must be a non-empty string")
    path = Path(path_value)
    return path.resolve() if path.is_absolute() else (artifact_root / path).resolve()


def _validate_artifact(
    record: Any,
    artifact_root: Path,
    label: str,
    *,
    maximum_size_bytes: int = MAX_ARTIFACT_BYTES,
) -> Path:
    if not isinstance(record, dict):
        raise CollectionError(f"{label} artifact record must be an object")
    path = _resolve_artifact(record.get("path"), artifact_root)
    if not path.is_file() or path.stat().st_size <= 0:
        raise CollectionError(f"{label} artifact is missing or empty: {path}")
    if path.stat().st_size > maximum_size_bytes:
        raise CollectionError(f"{label} artifact exceeds the size bound")
    _require_exact(record.get("sizeBytes"), path.stat().st_size, f"{label} size")
    digest = record.get("sha256")
    if not isinstance(digest, str) or SHA256_PATTERN.fullmatch(digest) is None:
        raise CollectionError(f"{label} SHA-256 is malformed")
    if sha256_file(path) != digest:
        raise CollectionError(f"{label} SHA-256 does not match the current artifact")
    return path


def validate_evidence(
    evidence: Any,
    profile: dict[str, Any],
    *,
    expected_revision: str,
    expected_apk_sha256: str,
    artifact_root: Path,
) -> dict[str, Any]:
    if not isinstance(evidence, dict):
        raise CollectionError("APH-506 evidence must be a JSON object")
    encoded = (json.dumps(evidence, sort_keys=True) + "\n").encode("utf-8")
    if len(encoded) > MAX_EVIDENCE_BYTES:
        raise CollectionError("APH-506 evidence JSON exceeds its size bound")
    _require_exact(evidence.get("schemaVersion"), SCHEMA_VERSION, "schemaVersion")
    _require_exact(evidence.get("taskId"), TASK_ID, "taskId")
    _require_exact(evidence.get("collectionStatus"), "complete", "collectionStatus")
    if CAPTURE_ID_PATTERN.fullmatch(str(evidence.get("captureId", ""))) is None:
        raise CollectionError("captureId is malformed")
    if UTC_PATTERN.fullmatch(str(evidence.get("capturedAtUtc", ""))) is None:
        raise CollectionError("capturedAtUtc is malformed")
    try:
        datetime.strptime(evidence["capturedAtUtc"], "%Y-%m-%dT%H:%M:%SZ")
    except ValueError as exc:
        raise CollectionError("capturedAtUtc is not a valid UTC calendar timestamp") from exc

    provenance = evidence.get("provenance")
    if not isinstance(provenance, dict):
        raise CollectionError("provenance must be an object")
    _require_exact(provenance.get("exactCommit"), expected_revision, "exact revision")
    _require_exact(provenance.get("dirty"), False, "dirty state")
    _require_exact(provenance.get("hostApkSha256"), expected_apk_sha256, "host APK SHA-256")
    _require_exact(provenance.get("deviceApkSha256"), expected_apk_sha256, "device APK SHA-256")
    _require_exact(provenance.get("deviceApkHashVerified"), True, "device APK verification")
    _require_exact(evidence.get("device"), profile["device"], "device identity")
    expected_build = copy.deepcopy(profile["build"])
    expected_build["launchArguments"] = expected_build.pop("requiredLaunchArguments")
    _require_exact(evidence.get("build"), expected_build, "build identity")

    environment = evidence.get("environmentPreflight")
    if not isinstance(environment, dict):
        raise CollectionError("environmentPreflight must be an object")
    battery = environment.get("battery")
    preflight_thermal = environment.get("thermal")
    if not isinstance(battery, dict) or not isinstance(preflight_thermal, dict):
        raise CollectionError("environment preflight battery and thermal values must be objects")
    powered = battery.get("powered")
    if not isinstance(powered, dict) or set(powered) != {
        "AC powered",
        "USB powered",
        "Wireless powered",
        "Dock powered",
    }:
        raise CollectionError("environment preflight power identities are invalid")
    if any(value is not False for value in powered.values()):
        raise CollectionError("environment preflight must prove the device was unplugged")
    level = _nonnegative_int(battery.get("level"), "environment preflight battery level")
    if level > 100:
        raise CollectionError("environment preflight battery level exceeds 100")
    _require_exact(preflight_thermal.get("status"), 0, "preflight thermal status")
    preflight_temperatures = preflight_thermal.get("temperatures")
    preflight_cooling = preflight_thermal.get("coolingDevices")
    if not isinstance(preflight_temperatures, list) or not preflight_temperatures:
        raise CollectionError("preflight temperatures must be a non-empty array")
    if not isinstance(preflight_cooling, list) or not preflight_cooling:
        raise CollectionError("preflight cooling devices must be a non-empty array")
    if any(
        not isinstance(item, dict)
        or _nonnegative_int(item.get("status"), "preflight sensor status") != 0
        for item in preflight_temperatures
    ):
        raise CollectionError("preflight temperature sensors must all be idle")
    if any(
        not isinstance(item, dict)
        or _nonnegative_int(item.get("value"), "preflight cooling value") != 0
        for item in preflight_cooling
    ):
        raise CollectionError("preflight cooling devices must all be idle")

    candidates = evidence.get("pilotCandidates")
    if not isinstance(candidates, list) or [item.get("path") for item in candidates if isinstance(item, dict)] != list(PILOT_TEXTURE_PATHS):
        raise CollectionError("pilot candidate paths are missing, reordered, or changed")
    current_candidates = _candidate_identity(artifact_root.resolve())
    _require_exact(candidates, current_candidates, "pilot candidate identity")

    capture = evidence.get("capture")
    if not isinstance(capture, dict):
        raise CollectionError("capture must be an object")
    config = validate_config(
        CollectionConfig(
            capture.get("warmupSeconds"),
            capture.get("sessionSeconds"),
            capture.get("sampleIntervalSeconds"),
            capture.get("gestureIntervalSeconds"),
        )
    )
    warmup_measured = _finite_number(capture.get("warmupMeasuredSeconds"), "measured warmup")
    session_measured = _finite_number(capture.get("sessionMeasuredSeconds"), "measured session")
    if warmup_measured < config.warmup_seconds:
        raise CollectionError("warmup evidence is short")
    if session_measured < config.session_seconds or session_measured < MIN_SESSION_SECONDS:
        raise CollectionError("camera pan/zoom evidence is short")
    initial_pid = capture.get("initialPid")
    if isinstance(initial_pid, bool) or not isinstance(initial_pid, int) or initial_pid <= 0:
        raise CollectionError("initial PID must be a positive integer")
    _require_exact(
        capture.get("gesturePattern"),
        ["pan-left", "zoom-in", "pan-right", "zoom-out"],
        "gesture pattern",
    )

    samples = capture.get("samples")
    if not isinstance(samples, list) or not samples or len(samples) > MAX_SAMPLE_COUNT:
        raise CollectionError("sample list is missing, empty, or unbounded")
    expected_sample_rows = [
        ("warmup", value)
        for value in scheduled_offsets(config.warmup_seconds, config.sample_interval_seconds)
    ] + [
        ("session", value)
        for value in scheduled_offsets(config.session_seconds, config.sample_interval_seconds)
    ]
    actual_sample_rows: list[tuple[Any, Any]] = []
    expected_component = f"{profile['build']['packageName']}/{profile['build']['activity']}"
    for sample in samples:
        if not isinstance(sample, dict):
            raise CollectionError("each sample must be an object")
        actual_sample_rows.append((sample.get("phase"), sample.get("scheduledOffsetSeconds")))
        _require_exact(sample.get("pid"), initial_pid, "sample PID")
        _require_exact(sample.get("foregroundComponent"), expected_component, "sample foreground")
        _require_exact(sample.get("processSurvived"), True, "sample survival")
        observed = _finite_number(sample.get("observedOffsetSeconds"), "observed sample offset")
        scheduled = _finite_number(sample.get("scheduledOffsetSeconds"), "scheduled sample offset")
        if observed + 1e-6 < scheduled:
            raise CollectionError("sample was recorded before its scheduled offset")
        thermal = sample.get("thermal")
        meminfo = sample.get("meminfoKb")
        process_io = sample.get("processIoCounters")
        if not isinstance(thermal, dict) or not isinstance(meminfo, dict) or not isinstance(process_io, dict):
            raise CollectionError("sample thermal, meminfo, and process I/O values must be objects")
        thermal_status = _nonnegative_int(thermal.get("status"), "sample thermal status")
        if thermal_status > profile["limits"]["maximumThermalStatus"]:
            raise CollectionError("sample thermal status exceeds the profile maximum")
        temperatures = thermal.get("temperatures")
        cooling_devices = thermal.get("coolingDevices")
        if not isinstance(temperatures, list) or not temperatures:
            raise CollectionError("sample temperatures must be a non-empty array")
        if not isinstance(cooling_devices, list) or not cooling_devices:
            raise CollectionError("sample cooling devices must be a non-empty array")
        if len(temperatures) > MAX_THERMAL_RECORDS or len(cooling_devices) > MAX_THERMAL_RECORDS:
            raise CollectionError("sample thermal records exceed the bound")
        for item in temperatures:
            if not isinstance(item, dict) or not isinstance(item.get("name"), str) or not item["name"]:
                raise CollectionError("sample temperature identity is invalid")
            value_c = _finite_number(item.get("valueC"), "sample temperature")
            sensor_status = _nonnegative_int(item.get("status"), "sample sensor status")
            if value_c < 0 or len(item["name"]) > 128:
                raise CollectionError("sample temperature value or name is invalid")
            if sensor_status > profile["limits"]["maximumThermalStatus"]:
                raise CollectionError("sample sensor status exceeds the profile maximum")
        for item in cooling_devices:
            if not isinstance(item, dict) or not isinstance(item.get("name"), str) or not item["name"]:
                raise CollectionError("sample cooling-device identity is invalid")
            cooling_value = _nonnegative_int(item.get("value"), "sample cooling-device value")
            if len(item["name"]) > 128:
                raise CollectionError("sample cooling-device name is too long")
            if cooling_value > profile["limits"]["maximumCoolingDeviceValue"]:
                raise CollectionError("sample cooling value exceeds the profile maximum")
        for key in ("totalPssKb", "totalRssKb", "totalSwapPssKb", "graphicsPssKb", "graphicsRssKb"):
            _nonnegative_int(meminfo.get(key), f"sample meminfo field {key}")
        if set(process_io) != set(IO_KEYS) or len(process_io) != len(IO_KEYS):
            raise CollectionError("sample process I/O keys are missing or changed")
        for key in IO_KEYS:
            _process_io_int(process_io.get(key), key)
    _require_exact(actual_sample_rows, expected_sample_rows, "sample schedule")

    gestures = capture.get("gestures")
    if not isinstance(gestures, list) or len(gestures) > MAX_GESTURE_COUNT:
        raise CollectionError("gesture trace is missing or unbounded")
    expected_gesture_offsets = gesture_offsets(config.session_seconds, config.gesture_interval_seconds)
    if len(gestures) != len(expected_gesture_offsets):
        raise CollectionError("gesture trace count does not match the configured schedule")
    pattern = ["pan-left", "zoom-in", "pan-right", "zoom-out"]
    for index, (gesture, offset) in enumerate(zip(gestures, expected_gesture_offsets)):
        if not isinstance(gesture, dict):
            raise CollectionError("each gesture trace row must be an object")
        _require_exact(gesture.get("index"), index, "gesture index")
        _require_exact(gesture.get("scheduledOffsetSeconds"), offset, "gesture offset")
        observed = _finite_number(gesture.get("observedOffsetSeconds"), "observed gesture offset")
        if observed + 1e-6 < offset:
            raise CollectionError("gesture was executed before its scheduled offset")
        _require_exact(gesture.get("direction"), pattern[index % len(pattern)], "gesture direction")
        _require_exact(gesture.get("kind"), "pan" if index % 2 == 0 else "zoom", "gesture kind")
        name, arguments = gesture_argv(
            profile["device"]["resolutionWidth"],
            profile["device"]["resolutionHeight"],
            index,
        )
        _require_exact(name, gesture["direction"], "gesture command direction")
        expected_digest = hashlib.sha256("\0".join(arguments).encode("utf-8")).hexdigest()
        _require_exact(gesture.get("commandSha256"), expected_digest, "gesture command SHA-256")

    expected_summary = _summaries(samples)
    _require_exact(capture.get("summary"), expected_summary, "capture summary")
    survival = evidence.get("survival")
    _require_exact(
        survival,
        {
            "processSurvived": True,
            "pidStable": True,
            "foregroundStable": True,
            "fatalMarkers": [],
        },
        "survival evidence",
    )

    artifacts = evidence.get("artifacts")
    if not isinstance(artifacts, dict):
        raise CollectionError("artifacts must be an object")
    apk = _validate_artifact(
        artifacts.get("apk"),
        artifact_root,
        "APK",
        maximum_size_bytes=MAX_APK_BYTES,
    )
    expected_apk_path = (artifact_root / profile["build"]["apkPath"]).resolve()
    if apk != expected_apk_path or sha256_file(apk) != expected_apk_sha256:
        raise CollectionError("APK artifact path or SHA-256 does not match the pinned APK")
    raw_log = _validate_artifact(artifacts.get("rawDeviceLog"), artifact_root, "raw log")
    before = _validate_artifact(artifacts.get("beforeScreenshot"), artifact_root, "before screenshot")
    after = _validate_artifact(artifacts.get("afterScreenshot"), artifact_root, "after screenshot")
    expected_dimensions = (
        profile["device"]["resolutionWidth"],
        profile["device"]["resolutionHeight"],
    )
    for label, path, record in (
        ("before", before, artifacts["beforeScreenshot"]),
        ("after", after, artifacts["afterScreenshot"]),
    ):
        dimensions = read_png_dimensions(path)
        if dimensions != expected_dimensions:
            raise CollectionError(f"{label} screenshot dimensions are invalid: {dimensions}")
        _require_exact(record.get("width"), dimensions[0], f"{label} screenshot width")
        _require_exact(record.get("height"), dimensions[1], f"{label} screenshot height")
    try:
        raw_text = raw_log.read_text(encoding="utf-8")
    except UnicodeDecodeError as exc:
        raise CollectionError("raw device log is not valid UTF-8") from exc
    if len(parse_match_ready_markers(raw_text)) != 1:
        raise CollectionError("raw device log has missing, duplicate, or stale MatchReady evidence")
    if parse_recorder_complete_markers(raw_text) != [True]:
        raise CollectionError("raw device log has missing, failed, duplicate, or stale Recorder evidence")
    fatals = detect_fatal_markers(raw_text)
    if fatals:
        raise CollectionError("raw device log contains fatal markers: " + ", ".join(fatals))

    boundary = evidence.get("acceptanceBoundary")
    if not isinstance(boundary, dict):
        raise CollectionError("acceptanceBoundary must be an object")
    _require_exact(boundary.get("collectorEvidenceValid"), True, "collector validity")
    _require_exact(boundary.get("streamingExpansionAuthorized"), False, "expansion authorization")
    return {
        "result": "Passed",
        "taskId": TASK_ID,
        "exactCommit": expected_revision,
        "deviceSerial": profile["device"]["serial"],
        "sampleCount": len(samples),
        "gestureCount": len(gestures),
    }


def _prepare_output(output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)
    stale = [name for name in OWNED_ARTIFACT_NAMES if (output_dir / name).exists()]
    if stale:
        raise CollectionError(
            "stale APH-506 output artifacts are present; use a clean output directory: "
            + ", ".join(stale)
        )


def run_collection(
    *,
    project_root: Path,
    adb_path: Path,
    serial: str,
    apk_path: Path,
    profile_path: Path,
    output_dir: Path,
    expected_revision: str,
    expected_apk_sha256: str,
    config: CollectionConfig = CollectionConfig(),
    adb: AdbBoundary | None = None,
    clock: Clock | None = None,
    repository: RepositoryBoundary | None = None,
    capture_id: str | None = None,
    captured_at_utc: str | None = None,
) -> tuple[dict[str, Any], dict[str, Any]]:
    checked_config = validate_config(config)
    source = repository if repository is not None else SubprocessRepository()
    inputs = validate_preinstall_inputs(
        project_root,
        apk_path,
        profile_path,
        serial,
        expected_revision,
        expected_apk_sha256,
        source,
    )
    output = output_dir.resolve()
    _prepare_output(output)
    boundary = adb if adb is not None else SubprocessAdb(adb_path, serial)
    timer = clock if clock is not None else SystemClock()

    require_exact_target(boundary, serial)
    device = collect_device_identity(boundary, inputs.profile)
    install_and_verify(boundary, inputs.apk_path, inputs.apk_sha256, inputs.profile)
    environment, collection = collect_pilot_session(
        boundary,
        timer,
        inputs.profile,
        checked_config,
        output,
    )
    _require_repository_state(source, inputs.project_root, expected_revision)
    if sha256_file(inputs.apk_path) != inputs.apk_sha256:
        raise CollectionError("host APK changed during APH-506 collection")
    _require_exact(_candidate_identity(inputs.project_root), inputs.candidates, "pilot candidates after collection")

    evidence = assemble_evidence(
        inputs,
        device,
        checked_config,
        environment,
        collection,
        capture_id=capture_id or uuid.uuid4().hex,
        captured_at_utc=captured_at_utc
        or datetime.now(timezone.utc).replace(microsecond=0).strftime("%Y-%m-%dT%H:%M:%SZ"),
    )
    validate_evidence(
        evidence,
        inputs.profile,
        expected_revision=expected_revision,
        expected_apk_sha256=expected_apk_sha256,
        artifact_root=inputs.project_root,
    )
    atomic_write_json(output / EVIDENCE_FILE_NAME, evidence)
    emitted = _load_json_object(output / EVIDENCE_FILE_NAME)
    result = validate_evidence(
        emitted,
        inputs.profile,
        expected_revision=expected_revision,
        expected_apk_sha256=expected_apk_sha256,
        artifact_root=inputs.project_root,
    )
    return emitted, result


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--adb-path", type=Path, required=True)
    parser.add_argument("--serial", required=True)
    parser.add_argument("--apk", type=Path, required=True)
    parser.add_argument("--profile", type=Path, default=DEFAULT_PROFILE)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--expected-revision", required=True)
    parser.add_argument("--expected-apk-sha256", required=True)
    parser.add_argument("--warmup-seconds", type=float, default=DEFAULT_WARMUP_SECONDS)
    parser.add_argument("--session-seconds", type=float, default=DEFAULT_SESSION_SECONDS)
    parser.add_argument(
        "--sample-interval-seconds",
        type=float,
        default=DEFAULT_SAMPLE_INTERVAL_SECONDS,
    )
    parser.add_argument(
        "--gesture-interval-seconds",
        type=float,
        default=DEFAULT_GESTURE_INTERVAL_SECONDS,
    )
    args = parser.parse_args(argv)
    try:
        _, result = run_collection(
            project_root=args.project_root,
            adb_path=args.adb_path,
            serial=args.serial,
            apk_path=args.apk,
            profile_path=args.profile,
            output_dir=args.output_dir,
            expected_revision=args.expected_revision,
            expected_apk_sha256=args.expected_apk_sha256,
            config=CollectionConfig(
                args.warmup_seconds,
                args.session_seconds,
                args.sample_interval_seconds,
                args.gesture_interval_seconds,
            ),
        )
    except (CollectionError, OSError) as exc:
        print(f"[APH-506 TextureStreamingDeviceCollection] result=Failed reason={exc}")
        return 1
    print(
        "[APH-506 TextureStreamingDeviceCollection] "
        f"result={result['result']} revision={result['exactCommit']} "
        f"device={result['deviceSerial']} samples={result['sampleCount']} "
        f"gestures={result['gestureCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
