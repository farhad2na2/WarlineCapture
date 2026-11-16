#!/usr/bin/env python3
"""Run a provenance-checked standalone validation player without Editor or graphics.

This launcher contains no mission simulation. StartupSmoke and Mission evidence
are deliberately distinct; neither substitutes for native visual acceptance.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import re
import signal
import subprocess
import sys
import time


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_build_receipt(path):
    receipt = json.loads(path.read_text())
    if (receipt.get("schema_version") != 1 or
            receipt.get("kind") != "SkirmishValidationPlayerBuild" or
            receipt.get("status") != "Passed" or
            receipt.get("validation_define") != "WARLINE_VALIDATION_PLAYER"):
        raise ValueError("A successful explicit validation-player build receipt is required")
    player = Path(receipt["player_executable"]).resolve(strict=True)
    root = Path(receipt["build_root"]).resolve(strict=True)
    if not player.is_relative_to(root):
        raise ValueError("Player executable escapes its recorded build root")
    if not player.is_file() or not os.access(player, os.X_OK):
        raise ValueError("Recorded standalone player is not executable")
    if player.parent.name != "MacOS" or player.parent.parent.name != "Contents":
        raise ValueError("Expected a built macOS standalone application")
    if player.parent.parent.parent.suffix != ".app":
        raise ValueError("Expected a standalone application bundle")
    if player.name == "Unity" or "Unity.app" in player.parts or "Editor" in player.parts:
        raise ValueError("Unity Editor execution is forbidden by this player launcher")
    if sha256(player) != receipt.get("player_sha256"):
        raise ValueError("Player executable changed after its successful build")
    if not isinstance(receipt.get("build_guid"), str) or not receipt["build_guid"].strip():
        raise ValueError("Actual player build GUID is required")
    package = receipt.get("package_files")
    if not isinstance(package, list) or not package:
        raise ValueError("Actual built gameplay code/data package pins are required")
    pinned = set()
    for item in package:
        file = Path(item["path"]).resolve(strict=True)
        if not file.is_relative_to(root) or file in pinned:
            raise ValueError("Invalid or duplicate built package path")
        if not file.is_file() or file.stat().st_size != item["size"] or sha256(file) != item["sha256"]:
            raise ValueError("Built player package changed: " + str(file))
        pinned.add(file)
    actual = {file.resolve() for file in root.rglob("*") if file.is_file()}
    if actual != pinned or player not in pinned:
        raise ValueError("Built package inventory differs from successful build receipt")
    return player, receipt


def validate_terminal(result, log, exit_code, mode, catalog, seed, build_guid):
    if exit_code != 0:
        raise ValueError("Standalone player exited with code " + str(exit_code))
    expected = dict(schema_version=1, kind="SkirmishValidationPlayerResult",
                    mode=mode, catalog=catalog, seed=seed, status="Passed")
    if any(result.get(key) != value for key, value in expected.items()):
        raise ValueError("Missing, failed or mismatched player terminal receipt")
    if not build_guid or result.get("build_guid") != build_guid:
        raise ValueError("Runtime result belongs to a different player build")
    marker = "[SkirmishValidationPlayer] result=Passed mode=" + mode
    if marker not in log:
        raise ValueError("Required player pass marker is missing")
    if mode == "Mission":
        if result.get("full_mission") is not True:
            raise ValueError("Startup evidence cannot establish full mission completion")
        outcome = result.get("terminal_outcome")
        if outcome not in ("Victory", "Defeat", "Draw"):
            raise ValueError("An actual terminal mission outcome is required")
    elif result.get("full_mission") is not False:
        raise ValueError("StartupSmoke must explicitly disclaim full mission completion")


def stop_owned_player(process):
    # start_new_session gives this invocation its own group. Never enumerate or
    # terminate Editors, Hub, licensing clients, Package Manager or other runs.
    if process.poll() is not None:
        return
    try:
        os.killpg(process.pid, signal.SIGTERM)
    except ProcessLookupError:
        return
    try:
        process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        process.wait(timeout=5)


def validate_speed(result, minimum):
    values = {}
    for key in ("simulation_seconds", "simulation_wall_seconds", "simulation_speed_ratio", "step_seconds"):
        value = result.get(key)
        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value) or value <= 0:
            raise ValueError("Missing or invalid measured throughput field: " + key)
        values[key] = value
    if values["step_seconds"] > .1:
        raise ValueError("Large simulation steps cannot substitute for accelerated normal-sized steps")
    steps = result.get("steps")
    if type(steps) is not int or steps < 1:
        raise ValueError("Actual normal-sized simulation step count is required")
    # Match elapsed is sampled around the native initialization/gameplay groups.
    # Allow two boundary ticks; pauses may reduce elapsed, never create more.
    if values["simulation_seconds"] > (steps + 2) * values["step_seconds"] + 1e-6:
        raise ValueError("Simulation elapsed exceeds the recorded normal-sized step budget")
    ratio = values["simulation_seconds"] / values["simulation_wall_seconds"]
    if not math.isclose(ratio, values["simulation_speed_ratio"], rel_tol=1e-5):
        raise ValueError("Reported throughput does not match measured simulation/wall time")
    if ratio < minimum:
        raise ValueError("Measured simulation speed %.3fx is below required %.3fx" % (ratio, minimum))
    return ratio



class ThroughputWatchdog:
    """Observe source trace progress; no simulation or source-clock substitution."""
    warmup_seconds = 5.0
    window_seconds = 2.0
    reject_fraction = .8  # Early rejection is conservative; terminal gate stays exact.

    def __init__(self, minimum):
        self.minimum = minimum
        self.offset = 0
        self.partial = b""
        self.playing_at = None
        self.latest_elapsed = None
        self.latest_source_wall = None
        self.window_at = None
        self.window_elapsed = None
        self.finished = False
        self.last_measurement = None

    def poll(self, path, now):
        if path.exists():
            with path.open("rb") as stream:
                stream.seek(self.offset)
                self.partial += stream.read()
                self.offset = stream.tell()
            lines = self.partial.split(b"\n")
            self.partial = lines.pop()  # An unflushed/partial row is not evidence.
            for line in lines:
                if not line.strip():
                    continue
                row = json.loads(line)
                if not isinstance(row, dict):
                    raise ValueError("Invalid source trace row")
                if row.get("phase") == "Finished":
                    self.finished = True
                if row.get("phase") != "Playing":
                    continue
                elapsed, source_wall = row.get("elapsed"), row.get("wallSeconds")
                if any(isinstance(value, bool) or not isinstance(value, (int, float)) or
                       not math.isfinite(value) or value < 0 for value in (elapsed, source_wall)):
                    raise ValueError("Invalid actual Playing trace progress")
                if self.latest_elapsed is not None and elapsed < self.latest_elapsed:
                    raise ValueError("Actual Playing trace elapsed regressed")
                self.latest_elapsed, self.latest_source_wall = elapsed, source_wall
                if self.playing_at is None:
                    self.playing_at = now
        if self.playing_at is None or self.finished:
            return
        if now - self.playing_at < self.warmup_seconds:
            return
        if self.window_at is None:
            self.window_at, self.window_elapsed = now, self.latest_elapsed
            return
        wall = now - self.window_at
        if wall < self.window_seconds:
            return
        progress = self.latest_elapsed - self.window_elapsed
        ratio = progress / wall
        self.last_measurement = dict(simulation_seconds=progress, observer_wall_seconds=wall,
                                     simulation_speed_ratio=ratio)
        if ratio < self.minimum * self.reject_fraction:
            raise ValueError("Early measured simulation throughput %.3fx is clearly below required %.3fx "
                             "after Playing warmup (progress %.3fs / observer %.3fs)" %
                             (ratio, self.minimum, progress, wall))
        self.window_at, self.window_elapsed = now, self.latest_elapsed

    def snapshot(self):
        return dict(observed_playing=self.playing_at is not None,
                    warmup_seconds=self.warmup_seconds, measurement_window_seconds=self.window_seconds,
                    early_reject_fraction=self.reject_fraction, minimum_speed=self.minimum,
                    latest_source_elapsed=self.latest_elapsed, latest_source_wall_seconds=self.latest_source_wall,
                    finished_observed=self.finished, last_measurement=self.last_measurement)


def wait_for_player(process, timeout, watchdog, trace_path, clock=None):
    clock = clock or time.monotonic
    deadline = clock() + timeout
    while process.poll() is None:
        now = clock()
        if now >= deadline:
            raise subprocess.TimeoutExpired(process.args, timeout)
        watchdog.poll(trace_path, now)
        try:
            process.wait(timeout=min(.25, deadline - now))
        except subprocess.TimeoutExpired:
            pass


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-receipt", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True, help="Fresh, non-existing evidence directory")
    parser.add_argument("--mode", choices=("StartupSmoke", "Mission"), default="Mission")
    parser.add_argument("--catalog", default="S002")
    parser.add_argument("--seed", type=int, default=104731)
    parser.add_argument("--timeout", type=float, default=300)
    parser.add_argument("--min-speed", type=float, default=10,
                        help="Required measured simulation/wall ratio after loading (default: 10)")
    args = parser.parse_args(argv)
    if not re.fullmatch(r"S\d{3}", args.catalog) or not 0 <= args.seed <= 2147483647:
        parser.error("Expected a coded Skirmish catalog ID and nonnegative signed-int seed")
    if not 0 < args.timeout <= 86400:
        parser.error("Timeout must be positive and no more than one day")
    if not math.isfinite(args.min_speed) or args.min_speed <= 0:
        parser.error("Minimum simulation speed must be finite and positive")
    player, build = load_build_receipt(args.build_receipt)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    command = [str(player), "-batchmode", "-nographics", "-screen-width", "1920", "-screen-height", "1080",
               "-logFile", str(output / "player.log"),
               "-warlineValidationMode", args.mode, "-warlineValidationCatalog", args.catalog,
               "-warlineValidationSeed", str(args.seed), "-warlineValidationOutput", str(output)]
    environment = {key: value for key, value in os.environ.items() if not key.startswith("WARLINE_")}
    environment["WARLINE_VALIDATION_SAVE_ROOT"] = str(output / "save-profile")
    environment["WARLINE_ARIA_BACKGROUND_VALIDATION"] = "1"
    record = dict(schema_version=1, kind="SkirmishValidationPlayerInvocation", mode=args.mode,
                  catalog=args.catalog, seed=args.seed, command=command,
                  build_receipt=str(args.build_receipt.resolve()),
                  build_receipt_sha256=sha256(args.build_receipt), player_sha256=build["player_sha256"],
                  timeout_seconds=args.timeout, minimum_simulation_speed=args.min_speed,
                  status="Running", native_visual_acceptance=False)
    (output / "invocation.json").write_text(json.dumps(record, indent=2) + "\n")
    process = None
    watchdog = ThroughputWatchdog(args.min_speed)
    started = time.monotonic()
    exit_code = 2
    try:
        with (output / "stdout.log").open("wb") as stdout:
            process = subprocess.Popen(command, stdout=stdout, stderr=subprocess.STDOUT,
                                       env=environment, start_new_session=True)
            record["pid"] = process.pid
            wait_for_player(process, args.timeout, watchdog, output / "trace.jsonl")
        result = json.loads((output / "result.json").read_text())
        record["player_result"] = result
        log = (output / "player.log").read_text(errors="replace")
        validate_terminal(result, log, process.returncode, args.mode, args.catalog, args.seed, build["build_guid"])
        record["functional_protocol_passed"] = True
        record["measured_simulation_speed"] = validate_speed(result, args.min_speed)
        record["status"] = "Passed"
        exit_code = 0
    except subprocess.TimeoutExpired:
        record.update(status="Failed", reason="Owned standalone player timed out")
        exit_code = 124
    except KeyboardInterrupt:
        record.update(status="Incomplete", reason="Invocation interrupted")
        exit_code = 130
    except (OSError, ValueError, KeyError, TypeError) as error:
        record.update(status="Failed", reason=str(error))
    finally:
        if process is not None:
            stop_owned_player(process)
            record["player_exit"] = process.returncode
        record["throughput_watchdog"] = watchdog.snapshot()
        record["wall_seconds"] = time.monotonic() - started
        record["runner_exit"] = exit_code
        (output / "runner-result.json").write_text(json.dumps(record, indent=2) + "\n")
    print("[SkirmishValidationPlayerRunner] result=" + record["status"] +
          " mode=" + args.mode + " evidence=" + str(output))
    return exit_code


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, KeyError, TypeError) as error:
        print("[SkirmishValidationPlayerRunner] result=Failed reason=" + str(error), file=sys.stderr)
        raise SystemExit(2)
