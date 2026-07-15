#!/usr/bin/env python3
"""Collect fail-closed APH-803 development evidence from the pinned Android device."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
import re
import subprocess
import tempfile
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable, Protocol, Sequence

try:
    from Tools.CI.android_development_performance_gate import (
        DEFAULT_PROFILE,
        GateValidationError,
        load_profile,
        percentile,
        validate_evidence,
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
        SubprocessAdb,
        SystemClock,
        collect_device_identity,
        parse_base_apk_path,
        parse_foreground_component,
        parse_package_dump,
        parse_pid,
        parse_resolved_launcher,
        parse_sha256sum,
        parse_thermal_snapshot,
        require_exact_target,
        require_idle_thermal,
        require_install_completion,
        sha256_file,
    )
except ModuleNotFoundError:  # Direct execution adds Tools/CI, not the repository root.
    from android_development_performance_gate import (
        DEFAULT_PROFILE,
        GateValidationError,
        load_profile,
        percentile,
        validate_evidence,
    )
    from android_performance_evidence_gate import detect_fatal_markers, read_png_dimensions
    from android_release_device_collection import (
        AdbBoundary,
        Clock,
        CollectionError,
        CommandResult,
        SubprocessAdb,
        SystemClock,
        collect_device_identity,
        parse_base_apk_path,
        parse_foreground_component,
        parse_package_dump,
        parse_pid,
        parse_resolved_launcher,
        parse_sha256sum,
        parse_thermal_snapshot,
        require_exact_target,
        require_idle_thermal,
        require_install_completion,
        sha256_file,
    )


TASK_ID = "APH-803"
PACKAGE_NAME = "com.warlinecapture.game"
RECORDER_DEVICE_PATH = (
    "/sdcard/Android/data/com.warlinecapture.game/files/WarlineCapture/Diagnostics/"
    "aph803_android_development_recorder.json"
)
RECORDER_FILE_NAME = "aph803_android_development_recorder.json"
RAW_LOG_FILE_NAME = "aph803_android_development_device.log"
SCREENSHOT_FILE_NAME = "aph803_android_development.png"
EVIDENCE_FILE_NAME = "aph803_android_development_evidence.json"
RESULT_FILE_NAME = "aph803_android_development_result.json"
REVISION_PATTERN = re.compile(r"^[0-9a-f]{40}$")
MATCH_READY_PATTERN = re.compile(
    r"\[APH-803 MatchReady\]\s+realtimeMs=(?P<ms>(?:\d+(?:\.\d*)?|\.\d+))\b"
)
RECORDER_MARKER_PATTERN = re.compile(
    r"\[APH-803 Recorder\]\s+complete=(?P<complete>[01])\b"
)


class RepositoryBoundary(Protocol):
    def head_revision(self, project_root: Path) -> str: ...

    def status_porcelain(self, project_root: Path) -> str: ...


class SubprocessRepository:
    def _run(self, project_root: Path, arguments: Sequence[str]) -> str:
        try:
            completed = subprocess.run(
                ("git", "-C", str(project_root), *arguments),
                capture_output=True,
                text=True,
                timeout=60.0,
                check=False,
            )
        except (OSError, subprocess.TimeoutExpired) as exc:
            raise CollectionError(f"Git provenance command failed: {exc}") from exc
        if completed.returncode != 0:
            detail = (completed.stderr or completed.stdout).strip()
            raise CollectionError(
                f"Git provenance command exited {completed.returncode}: {detail}"
            )
        return completed.stdout

    def head_revision(self, project_root: Path) -> str:
        return self._run(project_root, ("rev-parse", "--verify", "HEAD")).strip()

    def status_porcelain(self, project_root: Path) -> str:
        return self._run(
            project_root,
            ("status", "--porcelain=v1", "--untracked-files=all"),
        )


@dataclass(frozen=True)
class PreinstallInputs:
    project_root: Path
    apk_path: Path
    apk_sha256: str
    profile: dict[str, Any]
    expected_revision: str


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
        detail = (_text(result.stderr) or _text(result.stdout)).strip()
        raise CollectionError(f"{label} failed with exit code {result.returncode}: {detail}")
    return result


def _run(
    adb: AdbBoundary,
    arguments: Sequence[str],
    label: str,
    *,
    timeout: float = 60.0,
    use_serial: bool = True,
) -> CommandResult:
    return _checked(
        adb.run(arguments, timeout=timeout, use_serial=use_serial),
        label,
    )


def _require_success_word(result: CommandResult, label: str) -> None:
    _checked(result, label)
    if _text(result.stdout).strip() != "Success":
        raise CollectionError(f"{label} did not return exact Success")


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
    _atomic_write_bytes(path, payload)


def _load_json_object(path: Path, label: str) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise CollectionError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(value, dict):
        raise CollectionError(f"{label} must be a JSON object")
    return value


def _artifact_path(path: Path, project_root: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(project_root.resolve()).as_posix()
    except ValueError:
        return str(resolved)


def validate_preinstall_inputs(
    project_root: Path,
    apk_path: Path,
    profile_path: Path,
    serial: str,
    expected_revision: str,
    repository: RepositoryBoundary | None = None,
) -> PreinstallInputs:
    root = project_root.resolve()
    apk = apk_path.resolve()
    if not root.is_dir():
        raise CollectionError(f"project root does not exist: {root}")
    if REVISION_PATTERN.fullmatch(expected_revision) is None:
        raise CollectionError("expected revision must be exactly 40 lowercase hexadecimal characters")

    profile = load_profile(profile_path)
    if serial != profile["device"]["serial"]:
        raise CollectionError("requested serial does not match the pinned APH-803 profile")
    expected_apk = (root / profile["build"]["apkPath"]).resolve()
    if apk != expected_apk or not apk.is_file() or apk.stat().st_size <= 0:
        raise CollectionError("APK must be the exact non-empty artifact pinned by the APH-803 profile")

    source = repository if repository is not None else SubprocessRepository()
    head = source.head_revision(root)
    if head != expected_revision:
        raise CollectionError(
            f"Git HEAD mismatch: expected {expected_revision!r}, found {head!r}"
        )
    dirty = source.status_porcelain(root)
    if dirty.strip():
        raise CollectionError("Git worktree must be clean before APH-803 collection")

    return PreinstallInputs(root, apk, sha256_file(apk), profile, expected_revision)


def install_and_verify(
    adb: AdbBoundary,
    apk_path: Path,
    apk_sha256: str,
    profile: dict[str, Any],
) -> None:
    package = profile["build"]["packageName"]
    activity = profile["build"]["activity"]
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
        _require_success_word(adb.run(("uninstall", package), timeout=120.0), "package uninstall")

    require_install_completion(
        adb.run(("install", "--no-streaming", "-t", str(apk_path)), timeout=600.0),
        "exact development APK install",
    )

    component = f"{package}/{activity}"
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
    if resolved_component != component:
        raise CollectionError(
            f"resolved launcher must be exact GameActivity {component!r}, "
            f"found {resolved_component!r}"
        )

    package_dump = _text(
        _run(adb, ("shell", "dumpsys", "package", package), "package dump").stdout
    )
    _, primary_abi, flags = parse_package_dump(package_dump)
    if primary_abi != "arm64-v8a":
        raise CollectionError(f"installed package ABI must be arm64-v8a, found {primary_abi!r}")
    if "DEBUGGABLE" not in flags:
        raise CollectionError("installed APH-803 package must be debuggable")

    run_as = adb.run(("shell", "run-as", package, "id"))
    if run_as.returncode != 0 or not _text(run_as.stdout).strip().startswith("uid="):
        raise CollectionError("run-as must succeed for the development package")

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


def launch_argv(profile: dict[str, Any]) -> tuple[str, ...]:
    build = profile["build"]
    component = f"{build['packageName']}/{build['activity']}"
    return (
        "shell", "am", "start", "-W", "-S",
        "-a", "android.intent.action.MAIN",
        "-c", "android.intent.category.LAUNCHER",
        "-n", component,
        "--es", "unity", " ".join(build["requiredLaunchArguments"]),
    )


def _force_stop(adb: AdbBoundary, package: str) -> None:
    _run(adb, ("shell", "am", "force-stop", package), "force-stop")


def _clear_package(adb: AdbBoundary, package: str) -> None:
    _require_success_word(adb.run(("shell", "pm", "clear", package)), "pm clear")


def _clear_logcat(adb: AdbBoundary) -> None:
    _run(adb, ("logcat", "-b", "all", "-c"), "logcat clear")


def _launch(adb: AdbBoundary, profile: dict[str, Any]) -> None:
    launch = _run(adb, launch_argv(profile), "exact APH-803 launch", timeout=120.0)
    statuses = re.findall(r"^Status:\s*(\S+)\s*$", _text(launch.stdout), re.MULTILINE)
    if statuses != ["ok"]:
        raise CollectionError("am start -W must return exactly one successful Status row")


def parse_match_ready_markers(log_text: str) -> list[float]:
    values = [float(match.group("ms")) for match in MATCH_READY_PATTERN.finditer(log_text)]
    if any(not math.isfinite(value) or value <= 0 for value in values):
        raise CollectionError("APH-803 MatchReady values must be finite and positive")
    return values


def parse_recorder_complete_markers(log_text: str) -> list[bool]:
    return [match.group("complete") == "1" for match in RECORDER_MARKER_PATTERN.finditer(log_text)]


def _current_pid(adb: AdbBoundary, package: str) -> int:
    return parse_pid(_text(_run(adb, ("shell", "pidof", package), "pidof").stdout))


def _require_foreground(adb: AdbBoundary, expected_component: str) -> None:
    output = _text(
        _run(
            adb,
            ("shell", "dumpsys", "activity", "activities"),
            "foreground activity",
        ).stdout
    )
    actual = parse_foreground_component(output)
    if actual != expected_component:
        raise CollectionError(
            f"foreground component mismatch: expected {expected_component!r}, found {actual!r}"
        )


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
            raise CollectionError("startup logcat must contain exactly one APH-803 MatchReady marker")
        if markers:
            return markers[0]
        now = clock.monotonic()
        if now >= deadline:
            raise CollectionError("timed out waiting for APH-803 MatchReady marker")
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
        if clear_data:
            _clear_package(adb, package)
        _clear_logcat(adb)
        _launch(adb, profile)
        destination.append(wait_for_match_ready_dump(adb, clock))
        _force_stop(adb, package)
    return cold, warm


def _capture_thermal(
    adb: AdbBoundary,
    output_dir: Path,
    phase: str,
) -> dict[str, Any]:
    payload = _run(
        adb,
        ("shell", "dumpsys", "thermalservice"),
        f"{phase} thermal snapshot",
    ).stdout
    raw_path = output_dir / f"aph803_thermal_{phase}.txt"
    _atomic_write_bytes(raw_path, payload if isinstance(payload, bytes) else payload.encode("utf-8"))
    snapshot = parse_thermal_snapshot(_text(payload), phase)
    require_idle_thermal(snapshot)
    return snapshot


def monitor_sustained_run(
    adb: AdbBoundary,
    clock: Clock,
    session: Any,
    profile: dict[str, Any],
    initial_pid: int,
    match_ready_at: float,
    capture_during_thermal: Callable[[], dict[str, Any]],
    *,
    poll_seconds: float = 1.0,
    timeout_padding_seconds: float = 120.0,
) -> dict[str, Any]:
    package = profile["build"]["packageName"]
    expected_component = f"{package}/{profile['build']['activity']}"
    required_seconds = (
        float(profile["capture"]["warmupSeconds"])
        + float(profile["capture"]["sustainedSampleSeconds"])
    )
    deadline = match_ready_at + required_seconds + timeout_padding_seconds
    during_at = match_ready_at + float(profile["capture"]["warmupSeconds"]) + (
        float(profile["capture"]["sustainedSampleSeconds"]) / 2.0
    )
    during: dict[str, Any] | None = None
    while True:
        now = clock.monotonic()
        if now >= during_at and during is None:
            during = capture_during_thermal()

        log_text = session.read_text()
        markers = parse_recorder_complete_markers(log_text)
        if len(markers) > 1:
            raise CollectionError("sustained log contains duplicate APH-803 Recorder markers")
        if markers:
            if not markers[0]:
                raise CollectionError("APH-803 recorder reported incomplete evidence")
            if now - match_ready_at < required_seconds:
                raise CollectionError("APH-803 recorder completed before the required duration")
            if during is None:
                raise CollectionError("APH-803 recorder completed before during-thermal evidence")
            return during

        if _current_pid(adb, package) != initial_pid:
            raise CollectionError("application PID changed during APH-803 capture")
        _require_foreground(adb, expected_component)
        if now >= deadline:
            raise CollectionError("timed out waiting for APH-803 Recorder completion marker")
        clock.sleep(min(poll_seconds, deadline - now))


def _pull_verbatim(adb: AdbBoundary, device_path: str, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_name(destination.name + ".partial")
    if temporary.exists():
        temporary.unlink()
    try:
        _run(
            adb,
            ("pull", device_path, str(temporary)),
            "structured recorder pull",
            timeout=120.0,
        )
        if not temporary.is_file() or temporary.stat().st_size <= 0:
            raise CollectionError("pulled APH-803 recorder is missing or empty")
        temporary.replace(destination)
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
    before = _capture_thermal(adb, output_dir, "before")
    _clear_package(adb, package)
    stale = adb.run(("shell", "test", "!", "-e", RECORDER_DEVICE_PATH))
    if stale.returncode != 0:
        raise CollectionError("stale external APH-803 recorder is present after pm clear")

    _force_stop(adb, package)
    _clear_logcat(adb)
    raw_log_path = output_dir / RAW_LOG_FILE_NAME
    session = adb.start_logcat(raw_log_path)
    try:
        _launch(adb, profile)
        match_ready_at: float | None = None
        deadline = clock.monotonic() + 180.0
        while match_ready_at is None:
            markers = parse_match_ready_markers(session.read_text())
            if len(markers) > 1:
                raise CollectionError("sustained log contains duplicate APH-803 MatchReady markers")
            if markers:
                match_ready_at = clock.monotonic()
                break
            now = clock.monotonic()
            if now >= deadline:
                raise CollectionError("timed out waiting for sustained APH-803 MatchReady marker")
            clock.sleep(min(0.25, deadline - now))

        initial_pid = _current_pid(adb, package)
        _require_foreground(adb, expected_component)
        during = monitor_sustained_run(
            adb,
            clock,
            session,
            profile,
            initial_pid,
            match_ready_at,
            lambda: _capture_thermal(adb, output_dir, "during"),
        )
        after = _capture_thermal(adb, output_dir, "after")

        screenshot_payload = _run(
            adb,
            ("exec-out", "screencap", "-p"),
            "final APH-803 screenshot",
            timeout=120.0,
        ).stdout
        screenshot_path = output_dir / SCREENSHOT_FILE_NAME
        screenshot_bytes = (
            screenshot_payload
            if isinstance(screenshot_payload, bytes)
            else screenshot_payload.encode("latin-1")
        )
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
            raise CollectionError("application process did not survive final APH-803 evidence capture")
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
    cold_samples: list[float],
    warm_samples: list[float],
    sustained: SustainedCollection,
) -> dict[str, Any]:
    profile = inputs.profile
    recorder = _load_json_object(sustained.recorder_path, "APH-803 structured recorder")
    if recorder.get("taskId") != TASK_ID:
        raise CollectionError("structured recorder taskId is not APH-803")
    if recorder.get("complete") is not True or recorder.get("failure") != "":
        raise CollectionError("structured recorder did not complete successfully")
    sustained_run = recorder.get("sustainedRun")
    if not isinstance(sustained_run, dict):
        raise CollectionError("structured recorder sustainedRun must be an object")

    raw_log_text = sustained.raw_log_path.read_text(encoding="utf-8-sig")
    fatal_markers = detect_fatal_markers(raw_log_text)
    if fatal_markers:
        raise CollectionError("raw APH-803 log contains fatal markers: " + ", ".join(fatal_markers))
    if parse_match_ready_markers(raw_log_text) == []:
        raise CollectionError("raw APH-803 log is missing MatchReady evidence")
    if parse_recorder_complete_markers(raw_log_text) != [True]:
        raise CollectionError("raw APH-803 log must contain one successful Recorder marker")

    screenshot_width, screenshot_height = read_png_dimensions(sustained.screenshot_path)
    build = {
        key: copy.deepcopy(value)
        for key, value in profile["build"].items()
        if key != "requiredLaunchArguments"
    }
    build["launchArguments"] = copy.deepcopy(profile["build"]["requiredLaunchArguments"])
    return {
        "schemaVersion": 1,
        "taskId": TASK_ID,
        "provenance": {
            "exactCommit": inputs.expected_revision,
            "dirty": False,
            "apkSha256": inputs.apk_sha256,
        },
        "device": copy.deepcopy(device),
        "build": build,
        "startup": _startup_evidence(cold_samples, warm_samples),
        "sustainedRun": copy.deepcopy(sustained_run),
        "thermal": {
            "parser": "dumpsys-thermalservice-v1",
            "snapshots": copy.deepcopy(sustained.thermal_snapshots),
        },
        "artifacts": {
            "apk": {
                "path": profile["build"]["apkPath"],
                "sha256": inputs.apk_sha256,
            },
            "structuredRecorder": {
                "path": _artifact_path(sustained.recorder_path, inputs.project_root),
                "sha256": sha256_file(sustained.recorder_path),
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
    profile_path: Path,
    output_dir: Path,
    expected_revision: str,
    adb: AdbBoundary | None = None,
    clock: Clock | None = None,
    repository: RepositoryBoundary | None = None,
) -> tuple[dict[str, Any], dict[str, Any]]:
    inputs = validate_preinstall_inputs(
        project_root,
        apk_path,
        profile_path,
        serial,
        expected_revision,
        repository,
    )
    boundary = adb if adb is not None else SubprocessAdb(adb_path, serial)
    timer = clock if clock is not None else SystemClock()
    output = output_dir.resolve()
    output.mkdir(parents=True, exist_ok=True)

    require_exact_target(boundary, serial)
    device = collect_device_identity(boundary, inputs.profile)
    install_and_verify(boundary, inputs.apk_path, inputs.apk_sha256, inputs.profile)
    cold, warm = collect_startup_samples(boundary, timer, inputs.profile)
    sustained = collect_sustained(boundary, timer, inputs.profile, output)
    evidence = assemble_evidence(inputs, device, cold, warm, sustained)
    atomic_write_json(output / EVIDENCE_FILE_NAME, evidence)
    result = validate_evidence(
        evidence,
        inputs.profile,
        expected_revision=inputs.expected_revision,
        expected_apk_sha256=inputs.apk_sha256,
        artifact_root=inputs.project_root,
    )
    atomic_write_json(output / RESULT_FILE_NAME, result)
    return evidence, result


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--adb-path", type=Path, required=True)
    parser.add_argument("--serial", required=True)
    parser.add_argument("--apk", type=Path, required=True)
    parser.add_argument("--profile", type=Path, default=DEFAULT_PROFILE)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--expected-revision", required=True)
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
        )
    except (CollectionError, GateValidationError, OSError) as exc:
        print(f"[APH-803 AndroidDevelopmentDeviceCollection] result=Failed reason={exc}")
        return 1
    print(
        "[APH-803 AndroidDevelopmentDeviceCollection] "
        f"result={result['result']} revision={result['exactCommit']} device={result['deviceSerial']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
