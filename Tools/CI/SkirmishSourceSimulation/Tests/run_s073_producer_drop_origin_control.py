#!/usr/bin/env python3
"""Compile and run the isolated S073 producer-drop origin control from a fresh build receipt."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time


CONTROL_NAME = "SourceS073ProducerDropOriginControl"
PASS_MARKER = "[SourceS073ProducerDropOriginControl] result=Passed scope=OptInLiveProducerSlotFallbackMissingFrame_DefaultBranchPreserved"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(build_arg, output_arg, repository_arg):
    build_path = Path(build_arg).resolve(strict=True)
    output = Path(output_arg).absolute()
    repo = Path(repository_arg).resolve(strict=True)
    need(not output.exists(), "Fresh output directory required")
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073", "Passing fresh S073 source build receipt required")
    need(Path(build["repository_root"]).resolve(strict=True) == repo,
         "Build receipt repository mismatch")
    pins = build.get("source_and_reference_pins")
    need(isinstance(pins, dict) and len(pins) >= 100,
         "Complete source/reference pin set required")
    for path, expected in pins.items():
        need(sha(path) == expected, "Built source/reference pin changed: " + path)

    response_info = build.get("compiler_response")
    need(isinstance(response_info, dict), "Pinned compiler response required")
    response_path = Path(response_info["path"]).resolve(strict=True)
    need(sha(response_path) == response_info["sha256"], "Compiler response changed")
    control = repo / "Tools/CI/SkirmishSourceSimulation/Tests/SourceS073ProducerDropOriginControl.cs"
    runner = Path(__file__).resolve(strict=True)
    control_sha, runner_sha = sha(control), sha(runner)

    argv = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    main_indices = [i for i, value in enumerate(argv) if value == "-main:SourceSimulationProgram"]
    output_indices = [i for i, value in enumerate(argv) if value.startswith("-out:")]
    need(len(main_indices) == 1 and len(output_indices) == 1,
         "Expected unique checked source entry point/output")
    output.mkdir(parents=True)
    executable = output / (CONTROL_NAME + ".exe")
    argv[main_indices[0]] = "-main:" + CONTROL_NAME
    argv[output_indices[0]] = "-out:" + str(executable)
    argv.append(str(control))
    rsp = output / "control.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in argv) + "\n")

    dotnet = next((path for path in pins if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pins if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    need(dotnet and compiler, "Build-pinned dotnet/Roslyn toolchain required")
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    native_names = ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple")
    maps = "".join('<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
                    for name in native_names)
    maps += '<dllmap dll="MonoPosixHelper" target="' + str(mono_helper) + '" os="!windows"/>'
    mono_config = output / "mono-config.xml"
    mono_config.write_text("<configuration>" + maps + "</configuration>\n")

    started = time.monotonic()
    compiled = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True,
                              text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Producer-drop control compilation failed")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([
        str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
        str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
    ])
    ran = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), str(executable)],
                         env=env, capture_output=True, text=True, timeout=60)
    (output / "run.log").write_text(ran.stdout + ran.stderr)
    need(ran.returncode == 0 and PASS_MARKER in ran.stdout + ran.stderr,
         "Producer-drop origin control failed or pass marker missing")

    need(sha(control) == control_sha and sha(runner) == runner_sha,
         "Control or runner changed during execution")
    for path, expected in pins.items():
        need(sha(path) == expected, "Built source/reference pin changed during control: " + path)
    result = {
        "schema_version": 1,
        "kind": "S073ProducerDropOriginControlReceipt",
        "status": "Passed",
        "build_receipt": {"path": str(build_path), "sha256": sha(build_path)},
        "control_source": {"path": str(control), "sha256": control_sha},
        "runner_source": {"path": str(runner), "sha256": runner_sha},
        "compiler_response": {"path": str(response_path), "sha256": sha(response_path)},
        "source_pin_count_revalidated_before_and_after": len(pins),
        "compile_exit": compiled.returncode,
        "run_exit": ran.returncode,
        "elapsed_seconds": round(time.monotonic() - started, 3),
        "pass_marker": PASS_MARKER,
        "scope": "OptInLiveProducerSlotFallbackMissingFrameAndIdentityChecks_DefaultBranchPreserved",
        "native_parity_established": False,
        "mission_readiness_claimed": False
    }
    (output / "receipt.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("usage: run_s073_producer_drop_origin_control.py BUILD_JSON FRESH_OUTPUT_DIR REPOSITORY_ROOT")
    run(*sys.argv[1:])
