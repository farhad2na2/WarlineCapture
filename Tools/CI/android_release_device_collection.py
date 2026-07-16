#!/usr/bin/env python3
"""Collect bounded APH-804 release evidence from the pinned Android device."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import os
import re
import shlex
import subprocess
import tempfile
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable, Protocol, Sequence

try:
    from Tools.CI.android_performance_evidence_gate import detect_fatal_markers, read_png_dimensions
    from Tools.CI.android_release_performance_gate import (
        GateValidationError,
        load_profile,
        percentile,
        validate_evidence,
    )
except ModuleNotFoundError:  # Direct execution adds Tools/CI, not the repository root.
    from android_performance_evidence_gate import detect_fatal_markers, read_png_dimensions
    from android_release_performance_gate import (
        GateValidationError,
        load_profile,
        percentile,
        validate_evidence,
    )


PACKAGE_NAME = "com.warlinecapture.game"
RECORDER_DEVICE_PATH = (
    "/sdcard/Android/data/com.warlinecapture.game/files/WarlineCapture/Diagnostics/"
    "aph804_android_release_recorder.json"
)
RECORDER_FILE_NAME = "aph804_android_release_recorder.json"
RAW_LOG_FILE_NAME = "aph804_android_release_device.log"
SCREENSHOT_FILE_NAME = "aph804_android_release.png"
EVIDENCE_FILE_NAME = "aph804_android_release_evidence.json"
RESULT_FILE_NAME = "aph804_android_release_result.json"
BUILD_REPORT_RELATIVE_PATH = Path(
    "Design/AgentReports/architecture_performance_android_apk_build_report.json"
)
MATCH_READY_PATTERN = re.compile(
    r"\[APH-804 MatchReady\]\s+realtimeMs=(?P<ms>(?:\d+(?:\.\d*)?|\.\d+))\b"
)
RECORDER_MARKER_PATTERN = re.compile(r"\[APH-804 Recorder\]\s+complete=(?P<complete>[01])\b")
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
REVISION_PATTERN = re.compile(r"^[0-9a-f]{40}$")
WIRELESS_ADB_SERIAL_PATTERN = re.compile(
    r"^(?P<a>\d{1,3})\.(?P<b>\d{1,3})\.(?P<c>\d{1,3})\.(?P<d>\d{1,3}):(?P<port>\d{1,5})$"
)
COMPONENT_PATTERN = re.compile(
    r"(?:mResumedActivity:\s+|topResumedActivity=)ActivityRecord\{[^}]*\s"
    r"(?P<component>[A-Za-z0-9_.]+/[A-Za-z0-9_.$]+)(?:\s|\})"
)
RESOLVED_ACTIVITY_SUMMARY_PATTERN = re.compile(
    r"priority=-?\d+ preferredOrder=-?\d+ match=0x[0-9a-fA-F]+ "
    r"specificIndex=-?\d+ isDefault=(?:true|false)"
)
MAX_COMMAND_ERROR_DETAIL_CHARS = 2048


class CollectionError(RuntimeError):
    """Raised when device evidence cannot be proven exactly."""


@dataclass(frozen=True)
class CommandResult:
    argv: tuple[str, ...]
    returncode: int
    stdout: bytes = b""
    stderr: bytes = b""


class LogcatSession(Protocol):
    def read_text(self) -> str: ...

    def stop(self) -> None: ...


class AdbBoundary(Protocol):
    def run(
        self,
        args: Sequence[str],
        *,
        timeout: float = 60.0,
        use_serial: bool = True,
    ) -> CommandResult: ...

    def start_logcat(self, output_path: Path) -> LogcatSession: ...


class Clock(Protocol):
    def monotonic(self) -> float: ...

    def sleep(self, seconds: float) -> None: ...


class SystemClock:
    def monotonic(self) -> float:
        return time.monotonic()

    def sleep(self, seconds: float) -> None:
        time.sleep(seconds)


class _SubprocessLogcatSession:
    def __init__(self, argv: Sequence[str], output_path: Path) -> None:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        self._path = output_path
        self._stream = output_path.open("wb")
        self._process = subprocess.Popen(
            list(argv),
            stdout=self._stream,
            stderr=subprocess.DEVNULL,
        )

    def read_text(self) -> str:
        self._stream.flush()
        try:
            return self._path.read_text(encoding="utf-8")
        except UnicodeDecodeError as exc:
            raise CollectionError("continuous logcat is not valid UTF-8 text") from exc

    def stop(self) -> None:
        if self._process.poll() is None:
            self._process.terminate()
            try:
                self._process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self._process.kill()
                self._process.wait(timeout=5)
        self._stream.close()


class SubprocessAdb:
    """The only production boundary that executes ADB commands."""

    def __init__(self, adb_path: Path, serial: str) -> None:
        self.adb_path = str(adb_path)
        self.serial = serial

    def _argv(self, args: Sequence[str], use_serial: bool) -> list[str]:
        argv = [self.adb_path]
        if use_serial:
            argv.extend(("-s", self.serial))
        argv.extend(str(value) for value in args)
        return argv

    def run(
        self,
        args: Sequence[str],
        *,
        timeout: float = 60.0,
        use_serial: bool = True,
    ) -> CommandResult:
        argv = self._argv(args, use_serial)
        try:
            completed = subprocess.run(argv, capture_output=True, timeout=timeout, check=False)
        except (OSError, subprocess.TimeoutExpired) as exc:
            raise CollectionError(f"ADB command could not complete: {' '.join(argv)}: {exc}") from exc
        return CommandResult(tuple(argv), completed.returncode, completed.stdout, completed.stderr)

    def start_logcat(self, output_path: Path) -> LogcatSession:
        return _SubprocessLogcatSession(
            self._argv(("logcat", "-b", "all", "-v", "threadtime"), True),
            output_path,
        )


@dataclass(frozen=True)
class PreinstallInputs:
    project_root: Path
    apk_path: Path
    apk_sha256: str
    apk_size_bytes: int
    profile: dict[str, Any]
    expected_revision: str


@dataclass(frozen=True)
class InstalledPackage:
    base_apk_path: str
    code_path: str
    installed_size_bytes: int


@dataclass(frozen=True)
class SustainedCollection:
    thermal_snapshots: list[dict[str, Any]]
    recorder_path: Path
    raw_log_path: Path
    screenshot_path: Path
    process_survived: bool


def _text(data: bytes | str) -> str:
    if isinstance(data, str):
        return data
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError as exc:
        raise CollectionError("ADB output is not valid UTF-8 text") from exc


def _checked(result: CommandResult, label: str) -> CommandResult:
    if result.returncode != 0:
        streams = []
        stdout = _text(result.stdout).strip()
        stderr = _text(result.stderr).strip()
        if stdout:
            streams.append(f"stdout={stdout}")
        if stderr:
            streams.append(f"stderr={stderr}")
        detail = "; ".join(streams) or "no command output"
        if len(detail) > MAX_COMMAND_ERROR_DETAIL_CHARS:
            detail = detail[:MAX_COMMAND_ERROR_DETAIL_CHARS] + "...[truncated]"
        raise CollectionError(f"{label} failed with exit code {result.returncode}: {detail}")
    return result


def _run(
    adb: AdbBoundary,
    args: Sequence[str],
    label: str,
    *,
    timeout: float = 60.0,
    use_serial: bool = True,
) -> CommandResult:
    return _checked(adb.run(args, timeout=timeout, use_serial=use_serial), label)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def normalize_property(value: str) -> str:
    return " ".join(value.strip().split())


def parse_resolved_launcher(output: str) -> str:
    rows = [line.strip() for line in output.splitlines() if line.strip()]
    if len(rows) == 1:
        return rows[0]
    if len(rows) == 2 and RESOLVED_ACTIVITY_SUMMARY_PATTERN.fullmatch(rows[0]):
        return rows[1]
    raise CollectionError(f"resolved launcher output was not canonical: {rows!r}")


def _device_identity_values_match(key: str, expected: Any, actual: Any) -> bool:
    if key == "soc" and isinstance(expected, str) and isinstance(actual, str):
        return normalize_property(expected).casefold() == normalize_property(actual).casefold()
    return actual == expected


def parse_adb_devices(output: str, expected_serial: str) -> str:
    rows: list[tuple[str, str]] = []
    for raw_line in output.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("List of devices attached") or line.startswith("*"):
            continue
        columns = line.split()
        if len(columns) < 2:
            raise CollectionError(f"malformed adb devices row: {line!r}")
        rows.append((columns[0], columns[1]))
    if rows != [(expected_serial, "device")]:
        raise CollectionError(
            f"exactly one online target device is required; expected {expected_serial!r}, found {rows!r}"
        )
    return expected_serial


def parse_wm_physical_size(output: str) -> tuple[int, int]:
    matches = re.findall(r"^Physical size:\s*(\d+)x(\d+)\s*$", output, re.MULTILINE)
    if len(matches) != 1:
        raise CollectionError("wm size must contain exactly one physical size")
    width, height = (int(value) for value in matches[0])
    if width <= 0 or height <= 0:
        raise CollectionError("wm physical dimensions must be positive")
    return width, height


def validate_transport_serial(requested_serial: str, pinned_hardware_serial: str) -> str:
    if requested_serial == pinned_hardware_serial:
        return requested_serial
    match = WIRELESS_ADB_SERIAL_PATTERN.fullmatch(requested_serial)
    if match is None:
        raise CollectionError("requested serial must match the pinned hardware serial or an IPv4 ADB endpoint")
    octets = [int(match.group(name)) for name in ("a", "b", "c", "d")]
    port = int(match.group("port"))
    if any(value > 255 for value in octets) or not 1 <= port <= 65535:
        raise CollectionError("wireless ADB endpoint is outside the valid IPv4 or port range")
    return requested_serial


def parse_battery(output: str) -> dict[str, Any]:
    values: dict[str, str] = {}
    wanted = ("AC powered", "USB powered", "Wireless powered", "Dock powered", "level")
    for raw_line in output.splitlines():
        match = re.match(r"^\s*([^:]+):\s*(.*?)\s*$", raw_line)
        if match is None or match.group(1) not in wanted:
            continue
        key = match.group(1)
        if key in values:
            raise CollectionError(f"battery field is duplicated: {key}")
        values[key] = match.group(2)
    if set(values) != set(wanted):
        raise CollectionError("battery output is missing a required power or level field")
    powered: dict[str, bool] = {}
    for key in wanted[:-1]:
        if values[key] not in {"true", "false"}:
            raise CollectionError(f"battery field {key} is not an exact boolean")
        powered[key] = values[key] == "true"
    try:
        level = int(values["level"])
    except ValueError as exc:
        raise CollectionError("battery level is not an integer") from exc
    if not 0 <= level <= 100:
        raise CollectionError("battery level is outside 0..100")
    return {"powered": powered, "level": level}


def require_unplugged_battery(output: str) -> dict[str, Any]:
    battery = parse_battery(output)
    charging = [name for name, powered in battery["powered"].items() if powered]
    if charging:
        raise CollectionError("device must be unplugged; powered inputs: " + ", ".join(charging))
    return battery


def _section_records(output: str, heading: str) -> list[str]:
    lines = output.splitlines()
    indexes = [index for index, line in enumerate(lines) if line.strip() == heading]
    if len(indexes) != 1:
        raise CollectionError(f"thermal output requires exactly one {heading!r} section")
    records: list[str] = []
    for line in lines[indexes[0] + 1 :]:
        stripped = line.strip()
        if stripped.endswith(":") and "{" not in stripped:
            break
        if stripped:
            records.append(stripped)
    if not records:
        raise CollectionError(f"thermal section {heading!r} is empty")
    return records


def _thermal_fields(record: str, kind: str) -> dict[str, str]:
    match = re.fullmatch(rf"{kind}\{{(.*)\}}", record)
    if match is None:
        raise CollectionError(f"malformed current HAL {kind} record: {record!r}")
    fields: dict[str, str] = {}
    for item in match.group(1).split(","):
        key, separator, value = item.strip().partition("=")
        if not separator or not key or key in fields:
            raise CollectionError(f"malformed or duplicate {kind} field: {item!r}")
        fields[key] = value.strip()
    return fields


def _finite_nonnegative(value: str, path: str) -> float:
    try:
        parsed = float(value)
    except ValueError as exc:
        raise CollectionError(f"{path} is not numeric") from exc
    if not math.isfinite(parsed) or parsed < 0:
        raise CollectionError(f"{path} must be finite and non-negative")
    return parsed


def parse_thermal_snapshot(output: str, phase: str) -> dict[str, Any]:
    if phase not in {"before", "during", "after"}:
        raise CollectionError(f"unsupported thermal phase: {phase}")
    statuses = re.findall(r"^\s*Thermal Status:\s*(\d+)\s*$", output, re.MULTILINE)
    if len(statuses) != 1:
        raise CollectionError("thermal output requires exactly one non-negative Thermal Status")
    status = int(statuses[0])

    temperatures: list[dict[str, Any]] = []
    temperature_names: set[str] = set()
    for record in _section_records(output, "Current temperatures from HAL:"):
        fields = _thermal_fields(record, "Temperature")
        name = normalize_property(fields.get("mName", ""))
        if not name or name in temperature_names:
            raise CollectionError("current HAL temperature names must be non-empty and unique")
        temperature_names.add(name)
        temperatures.append({
            "name": name,
            "valueC": _finite_nonnegative(fields.get("mValue", ""), f"temperature {name}"),
        })

    cooling_devices: list[dict[str, Any]] = []
    cooling_names: set[str] = set()
    for record in _section_records(output, "Current cooling devices from HAL:"):
        fields = _thermal_fields(record, "CoolingDevice")
        name = normalize_property(fields.get("mName", ""))
        numeric = _finite_nonnegative(fields.get("mValue", ""), f"cooling device {name or '<unnamed>'}")
        if not name or name in cooling_names or not numeric.is_integer():
            raise CollectionError("current HAL cooling names must be unique and values must be integers")
        cooling_names.add(name)
        cooling_devices.append({"name": name, "value": int(numeric)})
    return {
        "phase": phase,
        "status": status,
        "coolingDevices": cooling_devices,
        "temperatures": temperatures,
    }


def require_idle_thermal(snapshot: dict[str, Any]) -> None:
    if snapshot["status"] != 0:
        raise CollectionError(f"thermal status must be 0, found {snapshot['status']}")
    active = [item["name"] for item in snapshot["coolingDevices"] if item["value"] != 0]
    if active:
        raise CollectionError("cooling device values must all be 0: " + ", ".join(active))


def parse_match_ready_markers(log_text: str) -> list[float]:
    values = [float(match.group("ms")) for match in MATCH_READY_PATTERN.finditer(log_text)]
    if any(not math.isfinite(value) or value <= 0 for value in values):
        raise CollectionError("MatchReady realtime values must be finite and positive")
    return values


def parse_recorder_complete_markers(log_text: str) -> list[bool]:
    return [match.group("complete") == "1" for match in RECORDER_MARKER_PATTERN.finditer(log_text)]


def parse_base_apk_path(output: str) -> str:
    paths = [line[len("package:") :].strip() for line in output.splitlines() if line.startswith("package:")]
    base_paths = [path for path in paths if path.endswith("/base.apk")]
    if len(base_paths) != 1 or not base_paths[0].startswith("/"):
        raise CollectionError("pm path must resolve exactly one absolute base.apk")
    return base_paths[0]


def parse_package_dump(output: str) -> tuple[str, str, set[str]]:
    code_paths = re.findall(r"^\s*codePath=(\S+)\s*$", output, re.MULTILINE)
    primary_abis = re.findall(r"^\s*primaryCpuAbi=(\S+)\s*$", output, re.MULTILINE)
    flag_rows = re.findall(r"^\s*flags=\[([^]]*)\]\s*$", output, re.MULTILINE)
    if len(code_paths) != 1 or len(primary_abis) != 1 or len(flag_rows) != 1:
        raise CollectionError("package dump requires exactly one codePath, primaryCpuAbi, and flags row")
    if not code_paths[0].startswith("/"):
        raise CollectionError("installed package codePath must be absolute")
    return code_paths[0], primary_abis[0], set(flag_rows[0].split())


def parse_sha256sum(output: str, expected_path: str) -> str:
    rows = [line.strip() for line in output.splitlines() if line.strip()]
    if len(rows) != 1:
        raise CollectionError("device sha256sum must return exactly one row")
    match = re.fullmatch(r"([0-9A-Fa-f]{64})\s+\*?(.+)", rows[0])
    if match is None or match.group(2) != expected_path:
        raise CollectionError("device sha256sum path or digest is malformed")
    return match.group(1).lower()


def parse_du_bytes(output: str, expected_path: str) -> int:
    rows = [line.strip() for line in output.splitlines() if line.strip()]
    if len(rows) != 1:
        raise CollectionError("installed-size du must return exactly one row")
    match = re.fullmatch(r"(\d+)\s+(.+)", rows[0])
    if match is None or match.group(2) != expected_path:
        raise CollectionError("installed-size du path or byte count is malformed")
    size = int(match.group(1))
    if size <= 0:
        raise CollectionError("installed package size must be positive")
    return size


def measure_installed_artifact_bytes(
    adb: AdbBoundary,
    base_apk_path: str,
    code_path: str,
) -> int:
    normalized_code_path = code_path.rstrip("/")
    if base_apk_path != f"{normalized_code_path}/base.apk":
        raise CollectionError("installed base APK must be the direct child of codePath")

    native_library_path = f"{normalized_code_path}/lib"
    base_apk_bytes = parse_du_bytes(
        _text(
            _run(
                adb,
                ("shell", "du", "-sb", base_apk_path),
                "installed base APK size",
            ).stdout
        ),
        base_apk_path,
    )
    native_library_bytes = parse_du_bytes(
        _text(
            _run(
                adb,
                ("shell", "du", "-sb", native_library_path),
                "installed native-library size",
            ).stdout
        ),
        native_library_path,
    )
    return base_apk_bytes + native_library_bytes


def parse_pid(output: str) -> int:
    tokens = output.split()
    if len(tokens) != 1 or not tokens[0].isdigit() or int(tokens[0]) <= 0:
        raise CollectionError("pidof must resolve exactly one positive PID")
    return int(tokens[0])


def parse_foreground_component(output: str) -> str:
    components = {match.group("component") for match in COMPONENT_PATTERN.finditer(output)}
    if len(components) != 1:
        raise CollectionError(f"foreground activity must resolve exactly one component, found {sorted(components)!r}")
    return next(iter(components))


def _load_json_object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise CollectionError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise CollectionError(f"{label} must be a JSON object")
    return value


def validate_preinstall_inputs(
    project_root: Path,
    apk_path: Path,
    build_report_path: Path,
    profile_path: Path,
    serial: str,
    expected_revision: str,
) -> PreinstallInputs:
    root = project_root.resolve()
    apk = apk_path.resolve()
    report_path = build_report_path.resolve()
    if not root.is_dir():
        raise CollectionError(f"project root does not exist: {root}")
    if REVISION_PATTERN.fullmatch(expected_revision) is None:
        raise CollectionError("expected revision must be exactly 40 lowercase hexadecimal characters")
    profile = load_profile(profile_path)
    validate_transport_serial(serial, profile["device"]["serial"])
    expected_apk = (root / profile["build"]["apkPath"]).resolve()
    if apk != expected_apk or not apk.is_file() or apk.stat().st_size <= 0:
        raise CollectionError("APK must be the exact non-empty artifact pinned by the release profile")
    expected_report = (root / BUILD_REPORT_RELATIVE_PATH).resolve()
    if report_path != expected_report or not report_path.is_file():
        raise CollectionError("build report must be the canonical Android APK build report")

    apk_size = apk.stat().st_size
    apk_sha = sha256_file(apk)
    maximum_size = profile["limits"]["maximumApkSizeBytes"]
    if maximum_size != {"comparison": "lessThanOrEqual", "value": maximum_size.get("value")}:
        raise CollectionError("profile APK limit must be an exact inclusive byte limit")
    maximum_value = maximum_size["value"]
    if isinstance(maximum_value, bool) or not isinstance(maximum_value, int) or maximum_value <= 0:
        raise CollectionError("profile APK byte limit is invalid")
    if apk_size > maximum_value:
        raise CollectionError(f"APK size {apk_size} exceeds profile maximum {maximum_value}")

    report = _load_json_object(report_path, "build report")
    expected_report_values: dict[str, Any] = {
        "schemaVersion": 1,
        "taskId": "APH-500",
        "exactCommit": expected_revision,
        "dirty": False,
        "status": "complete",
        "releaseBuildType": "release",
        "packageType": "APK",
        "buildTarget": "Android",
        "scriptingBackend": "IL2CPP",
        "targetArchitecture": "ARM64",
        "detailedBuildReport": True,
        "artifactPath": profile["build"]["apkPath"],
        "artifactSha256": apk_sha,
        "artifactBytes": apk_size,
    }
    for key, expected in expected_report_values.items():
        actual = report.get(key)
        if type(actual) is not type(expected) or actual != expected:
            raise CollectionError(
                f"build report {key} mismatch: expected {expected!r}, found {actual!r}"
            )
    return PreinstallInputs(root, apk, apk_sha, apk_size, profile, expected_revision)


def require_exact_target(adb: AdbBoundary, serial: str) -> None:
    output = _text(_run(adb, ("devices", "-l"), "adb devices", use_serial=False).stdout)
    parse_adb_devices(output, serial)


def collect_device_identity(adb: AdbBoundary, profile: dict[str, Any]) -> dict[str, Any]:
    device = profile["device"]

    def prop(name: str) -> str:
        value = normalize_property(
            _text(_run(adb, ("shell", "getprop", name), f"getprop {name}").stdout)
        )
        if not value:
            raise CollectionError(f"device property {name} is empty")
        return value

    actual: dict[str, Any] = {
        "serial": prop("ro.serialno"),
        "manufacturer": prop("ro.product.manufacturer"),
        "model": prop("ro.product.model"),
        "deviceCodeName": prop("ro.product.device"),
        "androidRelease": prop("ro.build.version.release"),
    }
    soc_manufacturer = prop("ro.soc.manufacturer")
    soc_model = prop("ro.soc.model")
    actual["soc"] = normalize_property(f"{soc_manufacturer} {soc_model}")
    sdk_text = prop("ro.build.version.sdk")
    if not sdk_text.isdigit():
        raise CollectionError("device SDK property is not a positive integer")
    actual["sdkLevel"] = int(sdk_text)

    physical = parse_wm_physical_size(
        _text(_run(adb, ("shell", "wm", "size"), "wm size").stdout)
    )
    expected_dimensions = (device["resolutionWidth"], device["resolutionHeight"])
    if sorted(physical) != sorted(expected_dimensions):
        raise CollectionError(
            f"physical display mismatch: expected unordered {expected_dimensions}, found {physical}"
        )
    actual["resolutionWidth"], actual["resolutionHeight"] = expected_dimensions
    for key, expected in device.items():
        if not _device_identity_values_match(key, expected, actual.get(key)):
            raise CollectionError(
                f"live device {key} mismatch: expected {expected!r}, found {actual.get(key)!r}"
            )
        if key == "soc":
            actual[key] = expected
    return actual


def _require_success_word(result: CommandResult, label: str) -> None:
    _checked(result, label)
    if _text(result.stdout).strip() != "Success":
        raise CollectionError(f"{label} did not return exact Success")


def require_install_completion(result: CommandResult, label: str) -> None:
    _checked(result, label)
    response = _text(result.stdout).strip()
    response_lines = tuple(line.strip() for line in response.splitlines() if line.strip())
    if response_lines not in ((), ("Success",), ("Performing Push Install", "Success")):
        raise CollectionError(f"{label} returned an unexpected response: {response!r}")


def ensure_release_package_available(
    adb: AdbBoundary,
    package: str,
    apk_path: Path,
) -> bool:
    listing = _text(
        _run(
            adb,
            ("shell", "pm", "list", "packages", "--user", "0", package),
            "installed package query",
        ).stdout
    )
    rows = [line.strip() for line in listing.splitlines() if line.strip()]
    expected_row = f"package:{package}"
    if rows not in ([], [expected_row]):
        raise CollectionError(f"installed package query was ambiguous: {rows!r}")
    if rows:
        return False

    install = adb.run(("install", "--no-streaming", str(apk_path)), timeout=600.0)
    require_install_completion(install, "exact APK install")
    return True


def install_and_verify(
    adb: AdbBoundary,
    apk_path: Path,
    apk_sha256: str,
    profile: dict[str, Any],
) -> InstalledPackage:
    package = profile["build"]["packageName"]
    activity = profile["build"]["activity"]
    ensure_release_package_available(adb, package, apk_path)

    expected_component = f"{package}/{activity}"
    resolved_component = parse_resolved_launcher(
        _text(
            _run(
                adb,
                (
                    "shell", "cmd", "package", "resolve-activity", "--brief",
                    "-a", "android.intent.action.MAIN",
                    "-c", "android.intent.category.LAUNCHER",
                    package,
                ),
                "resolve launcher activity",
            ).stdout
        )
    )
    if resolved_component != expected_component:
        raise CollectionError(
            f"resolved launcher must be exact GameActivity {expected_component!r}, "
            f"found {resolved_component!r}"
        )

    package_dump = _text(
        _run(adb, ("shell", "dumpsys", "package", package), "package dump").stdout
    )
    code_path, primary_abi, flags = parse_package_dump(package_dump)
    if primary_abi != "arm64-v8a":
        raise CollectionError(f"installed package ABI must be arm64-v8a, found {primary_abi!r}")
    if "DEBUGGABLE" in flags:
        raise CollectionError("installed package is debuggable")

    run_as = adb.run(("shell", "run-as", package, "id"))
    run_as_text = (_text(run_as.stdout) + "\n" + _text(run_as.stderr)).lower()
    if run_as.returncode == 0 or "not debuggable" not in run_as_text:
        raise CollectionError("run-as must fail specifically because the package is not debuggable")

    base_apk_path = parse_base_apk_path(
        _text(_run(adb, ("shell", "pm", "path", package), "package path").stdout)
    )
    device_sha = parse_sha256sum(
        _text(
            _run(adb, ("shell", "sha256sum", base_apk_path), "device base APK hash").stdout
        ),
        base_apk_path,
    )
    if device_sha != apk_sha256:
        raise CollectionError("device-side base APK hash does not match the host APK")
    installed_size = measure_installed_artifact_bytes(adb, base_apk_path, code_path)
    return InstalledPackage(base_apk_path, code_path, installed_size)


def launch_argv(profile: dict[str, Any]) -> tuple[str, ...]:
    build = profile["build"]
    unity_extra = shlex.quote(" ".join(build["requiredLaunchArguments"]))
    component = f"{build['packageName']}/{build['activity']}"
    return (
        "shell", "am", "start", "-W", "-S",
        "-a", "android.intent.action.MAIN",
        "-c", "android.intent.category.LAUNCHER",
        "-n", component,
        "--es", "unity", unity_extra,
    )


def _clear_package(adb: AdbBoundary, package: str) -> None:
    _require_success_word(adb.run(("shell", "pm", "clear", package)), "pm clear")


def _force_stop(adb: AdbBoundary, package: str) -> None:
    _run(adb, ("shell", "am", "force-stop", package), "force-stop")


def _clear_logcat(adb: AdbBoundary) -> None:
    _run(adb, ("logcat", "-b", "all", "-c"), "logcat clear")


def _launch(adb: AdbBoundary, profile: dict[str, Any]) -> None:
    result = _run(adb, launch_argv(profile), "exact release launch", timeout=120.0)
    statuses = re.findall(r"^Status:\s*(\S+)\s*$", _text(result.stdout), re.MULTILINE)
    if statuses != ["ok"]:
        raise CollectionError("am start -W must return exactly one successful Status row")


def wait_for_match_ready_dump(
    adb: AdbBoundary,
    clock: Clock,
    *,
    timeout_seconds: float = 180.0,
    poll_seconds: float = 0.25,
) -> float:
    deadline = clock.monotonic() + timeout_seconds
    while True:
        log_text = _text(
            _run(
                adb,
                ("logcat", "-b", "all", "-d", "-v", "threadtime"),
                "startup logcat dump",
            ).stdout
        )
        markers = parse_match_ready_markers(log_text)
        if len(markers) > 1:
            raise CollectionError("startup logcat must contain exactly one APH-804 MatchReady marker")
        if len(markers) == 1:
            return markers[0]
        now = clock.monotonic()
        if now >= deadline:
            raise CollectionError("timed out waiting for APH-804 MatchReady marker")
        clock.sleep(min(poll_seconds, deadline - now))


def collect_startup_samples(
    adb: AdbBoundary,
    clock: Clock,
    profile: dict[str, Any],
) -> tuple[list[float], list[float]]:
    package = profile["build"]["packageName"]
    capture = profile["capture"]
    cold: list[float] = []
    warm: list[float] = []
    runs = [
        (cold, True) for _ in range(capture["coldStartSampleCount"])
    ] + [
        (warm, False) for _ in range(capture["warmStartSampleCount"])
    ]
    for destination, clear_data in runs:
        _force_stop(adb, package)
        try:
            if clear_data:
                _clear_package(adb, package)
            _clear_logcat(adb)
            _launch(adb, profile)
            destination.append(wait_for_match_ready_dump(adb, clock))
        finally:
            _force_stop(adb, package)
    return cold, warm


def _current_pid(adb: AdbBoundary, package: str) -> int:
    return parse_pid(_text(_run(adb, ("shell", "pidof", package), "pid continuity").stdout))


def _require_foreground(adb: AdbBoundary, expected_component: str) -> None:
    actual = parse_foreground_component(
        _text(
            _run(
                adb,
                ("shell", "dumpsys", "activity", "activities"),
                "foreground continuity",
            ).stdout
        )
    )
    if actual != expected_component:
        raise CollectionError(
            f"foreground activity changed: expected {expected_component!r}, found {actual!r}"
        )


def monitor_sustained_run(
    adb: AdbBoundary,
    clock: Clock,
    session: LogcatSession,
    profile: dict[str, Any],
    initial_pid: int,
    match_ready_at: float,
    capture_during_thermal: Callable[[], dict[str, Any]],
    *,
    timeout_seconds: float = 780.0,
    check_interval_seconds: float = 5.0,
    during_offset_seconds: float = 360.0,
) -> tuple[str, dict[str, Any]]:
    package = profile["build"]["packageName"]
    expected_component = f"{package}/{profile['build']['activity']}"
    deadline = match_ready_at + timeout_seconds
    during: dict[str, Any] | None = None
    while True:
        now = clock.monotonic()
        if now >= deadline:
            raise CollectionError("timed out waiting for the exact APH-804 recorder completion marker")
        clock.sleep(min(check_interval_seconds, deadline - now))
        log_text = session.read_text()
        match_markers = parse_match_ready_markers(log_text)
        if len(match_markers) != 1:
            raise CollectionError(
                f"sustained logcat must contain exactly one MatchReady marker, found {len(match_markers)}"
            )
        fatals = detect_fatal_markers(log_text)
        if fatals:
            raise CollectionError("fatal marker in sustained logcat: " + ", ".join(fatals))
        recorder_markers = parse_recorder_complete_markers(log_text)
        if any(not complete for complete in recorder_markers):
            raise CollectionError("APH-804 recorder emitted a failed completion marker")
        if len(recorder_markers) > 1:
            raise CollectionError("APH-804 recorder completion marker is duplicated")
        if _current_pid(adb, package) != initial_pid:
            raise CollectionError("application PID changed or died during the sustained run")
        _require_foreground(adb, expected_component)
        if during is None and clock.monotonic() >= match_ready_at + during_offset_seconds:
            during = capture_during_thermal()
        if recorder_markers == [True]:
            if during is None:
                raise CollectionError("recorder completed before the required during-thermal capture")
            return log_text, during


def _atomic_write_bytes(path: Path, data: bytes) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", dir=path.parent)
    temporary = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        if temporary.exists():
            temporary.unlink()


def atomic_write_json(path: Path, value: dict[str, Any]) -> None:
    _atomic_write_bytes(
        path,
        (json.dumps(value, indent=2, sort_keys=True) + "\n").encode("utf-8"),
    )


def _capture_thermal(adb: AdbBoundary, output_dir: Path, phase: str) -> dict[str, Any]:
    raw = _run(
        adb,
        ("shell", "dumpsys", "thermalservice"),
        f"{phase} thermal snapshot",
    ).stdout
    raw_path = output_dir / f"aph804_thermal_{phase}.txt"
    _atomic_write_bytes(raw_path, raw if isinstance(raw, bytes) else raw.encode("utf-8"))
    snapshot = parse_thermal_snapshot(_text(raw), phase)
    require_idle_thermal(snapshot)
    return snapshot


def _pull_verbatim(adb: AdbBoundary, remote_path: str, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(prefix=f".{destination.name}.", dir=destination.parent)
    os.close(descriptor)
    temporary = Path(temporary_name)
    temporary.unlink()
    try:
        _run(adb, ("pull", remote_path, str(temporary)), "release recorder pull", timeout=120.0)
        if not temporary.is_file() or temporary.stat().st_size <= 0:
            raise CollectionError("pulled release recorder is missing or empty")
        os.replace(temporary, destination)
    finally:
        if temporary.exists():
            temporary.unlink()


def collect_sustained(
    adb: AdbBoundary,
    clock: Clock,
    profile: dict[str, Any],
    output_dir: Path,
) -> SustainedCollection:
    package = profile["build"]["packageName"]
    expected_component = f"{package}/{profile['build']['activity']}"
    battery_output = _text(
        _run(adb, ("shell", "dumpsys", "battery"), "battery preflight").stdout
    )
    require_unplugged_battery(battery_output)
    before = _capture_thermal(adb, output_dir, "before")
    _clear_package(adb, package)
    stale = adb.run(("shell", "test", "!", "-e", RECORDER_DEVICE_PATH))
    if stale.returncode != 0:
        raise CollectionError("stale external APH-804 recorder is present after pm clear")

    _force_stop(adb, package)
    _clear_logcat(adb)
    raw_log_path = output_dir / RAW_LOG_FILE_NAME
    session = adb.start_logcat(raw_log_path)
    try:
        _launch(adb, profile)
        match_ready_at: float | None = None
        deadline = clock.monotonic() + 180.0
        while match_ready_at is None:
            log_text = session.read_text()
            markers = parse_match_ready_markers(log_text)
            if len(markers) > 1:
                raise CollectionError("sustained launch emitted duplicate MatchReady markers")
            if markers:
                match_ready_at = clock.monotonic()
                break
            now = clock.monotonic()
            if now >= deadline:
                raise CollectionError("timed out waiting for sustained MatchReady marker")
            clock.sleep(min(0.25, deadline - now))

        initial_pid = _current_pid(adb, package)
        _require_foreground(adb, expected_component)
        _, during = monitor_sustained_run(
            adb,
            clock,
            session,
            profile,
            initial_pid,
            match_ready_at,
            lambda: _capture_thermal(adb, output_dir, "during"),
        )
        after = _capture_thermal(adb, output_dir, "after")

        screenshot_result = _run(
            adb,
            ("exec-out", "screencap", "-p"),
            "final screenshot",
            timeout=120.0,
        )
        screenshot_path = output_dir / SCREENSHOT_FILE_NAME
        screenshot_bytes = screenshot_result.stdout
        if isinstance(screenshot_bytes, str):
            screenshot_bytes = screenshot_bytes.encode("latin-1")
        _atomic_write_bytes(screenshot_path, screenshot_bytes)
        dimensions = read_png_dimensions(screenshot_path)
        expected_dimensions = (
            profile["device"]["resolutionWidth"],
            profile["device"]["resolutionHeight"],
        )
        if dimensions != expected_dimensions or dimensions[0] <= dimensions[1]:
            raise CollectionError(
                f"final screenshot must be exact landscape {expected_dimensions}, found {dimensions}"
            )
        if _current_pid(adb, package) != initial_pid:
            raise CollectionError("application process did not survive final evidence capture")
        _require_foreground(adb, expected_component)

        recorder_path = output_dir / RECORDER_FILE_NAME
        _pull_verbatim(adb, RECORDER_DEVICE_PATH, recorder_path)
        return SustainedCollection(
            [before, during, after],
            recorder_path,
            raw_log_path,
            screenshot_path,
            True,
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


def _startup_evidence(cold: list[float], warm: list[float]) -> dict[str, Any]:
    return {
        "launchDefinition": "process start to structured Match-ready transition",
        "coldStartSamplesMs": cold,
        "warmStartSamplesMs": warm,
        "coldP50Ms": percentile(cold, 50),
        "coldP95Ms": percentile(cold, 95),
        "coldMaximumMs": max(cold),
        "warmP50Ms": percentile(warm, 50),
        "warmP95Ms": percentile(warm, 95),
        "warmMaximumMs": max(warm),
    }


def assemble_evidence(
    inputs: PreinstallInputs,
    device: dict[str, Any],
    installed: InstalledPackage,
    cold_samples: list[float],
    warm_samples: list[float],
    sustained: SustainedCollection,
) -> dict[str, Any]:
    recorder_bytes = sustained.recorder_path.read_bytes()
    try:
        recorder = json.loads(recorder_bytes.decode("utf-8-sig"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise CollectionError("pulled release recorder is not valid JSON") from exc
    if not isinstance(recorder, dict) or not isinstance(recorder.get("sustainedRun"), dict):
        raise CollectionError("pulled release recorder does not contain a sustainedRun object")
    sustained_run = copy.deepcopy(recorder["sustainedRun"])
    profile = inputs.profile
    build = {
        key: copy.deepcopy(value)
        for key, value in profile["build"].items()
        if key != "requiredLaunchArguments"
    }
    build["launchArguments"] = copy.deepcopy(profile["build"]["requiredLaunchArguments"])
    screenshot_width, screenshot_height = read_png_dimensions(sustained.screenshot_path)
    return {
        "schemaVersion": 1,
        "taskId": "APH-804",
        "provenance": {
            "exactCommit": inputs.expected_revision,
            "dirty": False,
            "apkSha256": inputs.apk_sha256,
        },
        "device": copy.deepcopy(device),
        "build": build,
        "startup": _startup_evidence(cold_samples, warm_samples),
        "sustainedRun": sustained_run,
        "installedSizeBytes": installed.installed_size_bytes,
        "thermal": {
            "parser": "dumpsys-thermalservice-v1",
            "snapshots": copy.deepcopy(sustained.thermal_snapshots),
        },
        "artifacts": {
            "apk": {
                "path": profile["build"]["apkPath"],
                "sha256": inputs.apk_sha256,
                "sizeBytes": inputs.apk_size_bytes,
            },
            "structuredRecorder": {
                "path": _artifact_path(sustained.recorder_path, inputs.project_root),
                "sha256": hashlib.sha256(recorder_bytes).hexdigest(),
            },
            "rawDeviceLog": {
                "path": _artifact_path(sustained.raw_log_path, inputs.project_root),
                "sha256": sha256_file(sustained.raw_log_path),
            },
            "screenshot": {
                "path": _artifact_path(sustained.screenshot_path, inputs.project_root),
                "sha256": sha256_file(sustained.screenshot_path),
                "capturedPackage": profile["build"]["packageName"],
                "width": screenshot_width,
                "height": screenshot_height,
            },
        },
        "crashScan": {"processSurvived": sustained.process_survived, "fatalMarkers": []},
    }


def run_collection(
    *,
    project_root: Path,
    adb_path: Path,
    serial: str,
    apk_path: Path,
    build_report_path: Path,
    profile_path: Path,
    output_dir: Path,
    expected_revision: str,
    adb: AdbBoundary | None = None,
    clock: Clock | None = None,
) -> tuple[dict[str, Any], dict[str, Any]]:
    inputs = validate_preinstall_inputs(
        project_root,
        apk_path,
        build_report_path,
        profile_path,
        serial,
        expected_revision,
    )
    boundary = adb if adb is not None else SubprocessAdb(adb_path, serial)
    timer = clock if clock is not None else SystemClock()
    output = output_dir.resolve()
    output.mkdir(parents=True, exist_ok=True)

    require_exact_target(boundary, serial)
    device = collect_device_identity(boundary, inputs.profile)
    installed = install_and_verify(boundary, inputs.apk_path, inputs.apk_sha256, inputs.profile)
    cold, warm = collect_startup_samples(boundary, timer, inputs.profile)
    sustained = collect_sustained(boundary, timer, inputs.profile, output)
    evidence = assemble_evidence(inputs, device, installed, cold, warm, sustained)
    result = validate_evidence(
        evidence,
        inputs.profile,
        expected_revision=inputs.expected_revision,
        expected_apk_sha256=inputs.apk_sha256,
        artifact_root=inputs.project_root,
    )
    atomic_write_json(output / EVIDENCE_FILE_NAME, evidence)
    atomic_write_json(output / RESULT_FILE_NAME, result)
    return evidence, result


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--adb-path", type=Path, required=True)
    parser.add_argument("--serial", required=True)
    parser.add_argument("--apk", type=Path, required=True)
    parser.add_argument("--build-report", type=Path, required=True)
    parser.add_argument("--profile", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--expected-revision", required=True)
    args = parser.parse_args(argv)
    try:
        _, result = run_collection(
            project_root=args.project_root,
            adb_path=args.adb_path,
            serial=args.serial,
            apk_path=args.apk,
            build_report_path=args.build_report,
            profile_path=args.profile,
            output_dir=args.output_dir,
            expected_revision=args.expected_revision,
        )
    except (CollectionError, GateValidationError, OSError) as exc:
        print(f"[APH-804 AndroidReleaseDeviceCollection] result=Failed reason={exc}")
        return 1
    print(
        "[APH-804 AndroidReleaseDeviceCollection] "
        f"result={result['result']} revision={result['exactCommit']} device={result['deviceSerial']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
