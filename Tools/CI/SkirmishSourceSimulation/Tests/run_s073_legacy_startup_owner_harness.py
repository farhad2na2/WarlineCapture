#!/usr/bin/env python3
"""Run exact S073 legacy rows through the shared building-request owner."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, message):
    if not ok:
        raise RuntimeError(message)


def run(build_path, output_path, snapshot_path, snapshot_sha, config_path, config_sha, source_repo):
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    snapshot = Path(snapshot_path).resolve(strict=True)
    config = Path(config_path).resolve(strict=True)
    repo = Path(source_repo).resolve(strict=True)
    require(not output.exists(), "Fresh T: output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
            "Passing Roslyn source build required")
    require(sha(snapshot) == snapshot_sha and sha(config) == config_sha,
            "Pinned S073 prefab/config inputs changed")
    pins = build["source_and_reference_pins"]
    for path, expected in pins.items():
        require(sha(path) == expected, "Checked source build pin changed: " + path)

    harness = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073LegacyStartupOwnerHarness.cs"
    harness_sha = sha(harness)
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    require(sha(response_path) == response["sha256"], "Checked C# response file changed")
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    mains = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    require(len(mains) == 1 and len(outputs) == 1, "Expected the checked source host entry and output")
    exe = output / "S073LegacyStartupOwnerHarness.exe"
    arguments[mains[0]] = "-main:S073LegacyStartupOwnerHarness"
    arguments[outputs[0]] = "-out:" + str(exe)
    arguments.extend([str(harness)])
    rsp = output / "harness.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")

    pinned_tools = list(pins)
    dotnet = next((path for path in pinned_tools if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pinned_tools if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    require(dotnet and compiler, "Pinned .NET/Roslyn toolchain required")
    began = time.monotonic()
    compile_result = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True,
                                    text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    timed_out = False
    if compile_result.returncode == 0:
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
        ])
        try:
            run_result = subprocess.run([str(runtime["mono"]), str(exe), str(snapshot), str(config), str(repo)],
                                        env=env, capture_output=True, text=True, timeout=60)
            (output / "run.log").write_text(run_result.stdout + run_result.stderr)
        except subprocess.TimeoutExpired as error:
            timed_out = True
            stdout, stderr = error.stdout or "", error.stderr or ""
            if isinstance(stdout, bytes):
                stdout = stdout.decode("utf-8", errors="replace")
            if isinstance(stderr, bytes):
                stderr = stderr.decode("utf-8", errors="replace")
            (output / "run.log").write_text(stdout + stderr)

    for path, expected in pins.items():
        require(sha(path) == expected, "Source/build input changed during harness run: " + path)
    require(sha(harness) == harness_sha and sha(snapshot) == snapshot_sha and sha(config) == config_sha,
            "Harness or S073 input changed during run")
    passed = compile_result.returncode == 0 and not timed_out and run_result is not None and \
        run_result.returncode == 0 and "[S073LegacyStartupOwnerHarness] result=Passed" in run_result.stdout
    result = {
        "schema_version": 1,
        "kind": "S073LegacyStartupOwnerHarness",
        "status": "Passed" if passed else "Failed",
        "failure": "60-second harness timeout" if timed_out else None,
        "compile_exit": compile_result.returncode,
        "run_exit": None if run_result is None else run_result.returncode,
        "wall_seconds": time.monotonic() - began,
        "build_receipt": str(build_path),
        "build_receipt_sha256": sha(build_path),
        "harness_source": str(harness),
        "harness_source_sha256": harness_sha,
        "prefab_capture": {"path": str(snapshot), "sha256": snapshot_sha},
        "legacy_config": {"path": str(config), "sha256": config_sha},
        "source_repo": str(repo),
        "unity_launched": False,
        "mission_world_allocated": False,
        "initial_facilities_placed": False,
        "mission_outcome_observed": False,
        "mission_ready": False
    }
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if passed else 1


if __name__ == "__main__":
    if len(sys.argv) != 8:
        raise SystemExit("usage: run_s073_legacy_initial_owner_harness.py <build.json> <fresh-output> <prefabs.json> <prefab-sha256> <legacy-config.json> <config-sha256> <source-repository>")
    raise SystemExit(run(*sys.argv[1:]))
