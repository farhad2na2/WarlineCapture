#!/usr/bin/env python3
"""Compile and run the S073/S002 clearance differential in a fresh scratch world."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(build_path, output_path, repository_root):
    repo = Path(repository_root).resolve(strict=True)
    build_file = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    need(not output.exists(), "Fresh output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_file.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073", "Passing current S073 source build required")
    pins = build.get("source_and_reference_pins", {})
    adapter = repo / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073TacticalAIDisposableCopyFixture.Production.cs"
    s002 = repo / "Tools/CI/SkirmishSourceSimulation/Adapters/527_GeneratedS002DropClearance.cs"
    fixture = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073ClearanceDifferentialScratchControl.cs"
    runner = Path(__file__).resolve(strict=True)
    need(str(adapter) in pins and pins[str(adapter)] == sha(adapter),
         "Current compiled build must pin the exact S073 source-clearance adapter")
    need(str(s002) in pins and pins[str(s002)] == sha(s002),
         "Current compiled build must pin the strict S002 baseline adapter")
    need(build.get("source_count", 0) >= 700, "Expected full experimental source build")
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    need(sha(response_path) == response["sha256"], "Checked compiler response changed")
    for path, expected in pins.items():
        need(sha(path) == expected, "Checked source/build input pin changed: " + path)
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    main_indices = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    output_indices = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    need(len(main_indices) == 1 and len(output_indices) == 1, "Expected unique checked entry point/output")
    exe = output / "S073ClearanceDifferentialScratchControl.exe"
    arguments[main_indices[0]] = "-main:S073ClearanceDifferentialScratchControl"
    arguments[output_indices[0]] = "-out:" + str(exe)
    arguments.append(str(fixture))
    rsp = output / "fixture.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")
    pinned_tools = list(pins)
    dotnet = next((path for path in pinned_tools if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pinned_tools if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    need(dotnet and compiler, "Pinned .NET/Roslyn toolchain required")
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    mono_config_source = runtime["mono"].parent.parent / "etc/mono/config"
    for path in (mono_helper, mono_native, mono_config_source):
        need(path.is_file() and str(path) in pins and sha(path) == pins[str(path)],
             "Pinned Unity Mono runtime support required: " + str(path))
    dllmaps = "".join('<dllmap dll="' + name + '" target="' + str(mono_helper) + '" os="!windows"/>'
                     for name in ("MonoPosixHelper",)) + "".join(
        '<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
        for name in ("System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"))
    mono_config = output / "mono-config.xml"
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")

    started = time.monotonic()
    compiled = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0, "Scratch differential fixture compilation failed")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([
        str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
        str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
    ])
    ran = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), str(exe), str(repo)],
                         env=env, capture_output=True, text=True, timeout=120)
    (output / "run.log").write_text(ran.stdout + ran.stderr)
    need(ran.returncode == 0, "Scratch differential fixture failed")
    marker = next((line for line in ran.stdout.splitlines()
                   if line.startswith("[S073ClearanceDifferentialScratchControl] ")), None)
    need(marker is not None, "Required compiled differential pass marker missing")
    result = json.loads(marker.split(" ", 1)[1])
    need(result.get("status") == "Passed" and result.get("compiled_differential_executed") is True and
         result.get("delivery_invoked") is False and result.get("startup_bundle_consumed") is False and
         result.get("scheduler_admitted") is False and result.get("mission_ready") is False,
         "Diagnostic-only compiled differential receipt required")
    for path, expected in pins.items():
        need(sha(path) == expected, "Pinned build input changed during fixture: " + path)
    receipt = {
        "schema_version": 1,
        "kind": "S073ClearanceDifferentialScratchControlRun",
        "status": "Passed",
        "build_report": str(build_file),
        "build_report_sha256": sha(build_file),
        "compiled_source_build_executable": build["executable"],
        "s073_adapter_sha256": pins[str(adapter)],
        "s002_adapter_sha256": pins[str(s002)],
        "fixture_sha256": sha(fixture),
        "runner_sha256": sha(runner),
        "fixture_executable_sha256": sha(exe),
        "compiler_response_sha256": response["sha256"],
        "source_pin_count_revalidated_before_and_after": len(pins),
        "source_provider_parity_qualified": False,
        "historical_capture_dependency_pins_revalidated": False,
        "source_capture_current_producer_equivalence": False,
        "startup_bundle_consumed": False,
        "native_runtime_state_consumed": False,
        "queue_or_delivery_owner_invoked": False,
        "scheduler_admitted": False,
        "mission_ready": False,
        "elapsed_seconds": round(time.monotonic() - started, 3),
        "differential": result,
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print("[run_s073_clearance_differential_scratch_control] result=Passed output=" + str(output))


if __name__ == "__main__":
    if len(sys.argv) != 4:
        raise SystemExit("usage: run_s073_clearance_differential_scratch_control.py BUILD_JSON FRESH_OUTPUT_DIR REPOSITORY_ROOT")
    run(*sys.argv[1:])
