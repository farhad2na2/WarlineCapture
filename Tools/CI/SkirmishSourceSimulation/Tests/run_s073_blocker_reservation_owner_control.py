#!/usr/bin/env python3
"""Compile and run a bounded control against the original blocker reservation owner."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(build_path, output_path):
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    require(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    build_raw = build_path.read_bytes()
    build = json.loads(build_raw)
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
            "Passing checked source build required")
    pins = build.get("source_and_reference_pins", {})
    repo = Path(build["repository_root"]).resolve(strict=True)
    response_path = Path(build["compiler_response"]["path"]).resolve(strict=True)
    require(sha(response_path) == build["compiler_response"]["sha256"], "Checked compiler response changed")
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    owner = str((repo / "Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs").resolve(strict=True))
    require(owner in arguments,
            "Reservation control requires the original InitialUnitsSpawnSystem in the compiled source closure")
    def verify_pins():
        for path, expected in pins.items():
            require(sha(path) == expected, "Checked source/build pin changed: " + path)

    verify_pins()

    source = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073BlockerReservationOwnerControl.cs"
    source_hash = sha(source)
    mains = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    require(len(mains) == 1 and len(outputs) == 1, "Expected one source host entry and output")
    executable = output / "S073BlockerReservationOwnerControl.exe"
    arguments[mains[0]] = "-main:S073BlockerReservationOwnerControl"
    arguments[outputs[0]] = "-out:" + str(executable)
    arguments.append(str(source))
    test_response = output / "control.rsp"
    test_response.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")

    dotnet = next((path for path in pins if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pins if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    require(dotnet and compiler, "Pinned Roslyn toolchain required")
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    require(mono_helper.is_file() and mono_native.is_file(), "Pinned Mono helper libraries required")
    mono_config = output / "mono-config.xml"
    dllmaps = "".join("<dllmap dll=\"" + name + "\" target=\"" + str(mono_helper) + "\" os=\"!windows\"/>"
                       for name in ["MonoPosixHelper"]) + "".join(
        "<dllmap dll=\"" + name + "\" target=\"" + str(mono_native) + "\" os=\"!windows\"/>"
        for name in ["System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"])
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")

    started = time.monotonic()
    compile_result = subprocess.run([dotnet, compiler, "@" + str(test_response)], capture_output=True,
                                    text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    if compile_result.returncode == 0:
        verify_pins()
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
        ])
        run_result = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), str(executable)],
                                    env=env, capture_output=True, text=True, timeout=60)
        (output / "run.log").write_text(run_result.stdout + run_result.stderr)
        verify_pins()

    log = "" if run_result is None else run_result.stdout + run_result.stderr
    marker = "[S073BlockerReservationOwnerControl] result=Passed"
    result = {
        "schema_version": 1,
        "kind": "S073BlockerReservationOwnerControl",
        "status": "Passed" if compile_result.returncode == 0 and run_result is not None and
                  run_result.returncode == 0 and marker in log else "Failed",
        "scope": "Actual InitialUnitsSpawnSystem.ReserveStaticBlockerFootprints through checked source host; bounded synthetic cell fixture only, not mission state/readiness.",
        "build_receipt": str(build_path),
        "build_receipt_sha256": hashlib.sha256(build_raw).hexdigest(),
        "source_sha256": source_hash,
        "response_sha256": sha(response_path),
        "compile_exit": compile_result.returncode,
        "run_exit": None if run_result is None else run_result.returncode,
        "pass_marker_present": marker in log,
        "elapsed_seconds": time.monotonic() - started,
        "output": str(output),
        "native_or_player_launched": False,
        "mission_outcome_observed": False,
        "mission_ready": False
    }
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, indent=2))
    return 0 if result["status"] == "Passed" else 1


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: run_s073_blocker_reservation_owner_control.py <build.json> <fresh-output-dir>")
    raise SystemExit(run(sys.argv[1], sys.argv[2]))
