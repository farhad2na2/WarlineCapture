#!/usr/bin/env python3
"""Compile and run isolated ECS gate controls against the pinned r3 source build."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


BUILD_RELATIVE = Path("Logs/SkirmishProduction/s073-original-aria-probe-build-20261103-r3/build.json")
BUILD_SHA256 = "97f21831793357edc4b7ca20ba8a412578ef59d5de93d1130a041976d86787d1"
CONTROL_NAME = "S073OriginalAriaStepSourceHostControls.cs"
PASS_MARKER = "[S073OriginalAriaStepSourceHostControls] result=Passed cases=4 stepInvoked=0 mutation=0 readiness=0 gameplay=0 unity=0 syntheticFixtures=1"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def run(output_path):
    repo = Path(__file__).resolve().parents[4]
    build_path = repo / BUILD_RELATIVE
    need(build_path.resolve(strict=True) == (repo / BUILD_RELATIVE).resolve(strict=True),
         "Pinned r3 source-host build path required")
    need(sha(build_path) == BUILD_SHA256, "Pinned r3 source-host build receipt changed")
    build = json.loads(build_path.read_text(encoding="utf-8"))
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073" and build.get("runtime_backend") == "LegacyS073OwnerV1" and
         build.get("native_or_player_launched") is False,
         "Passing source-only S073 build required")
    need(Path(build["repository_root"]).resolve(strict=True) == repo.resolve(strict=True),
         "Pinned build repository root mismatch")

    pins = build.get("source_and_reference_pins")
    need(isinstance(pins, dict) and len(pins) >= 100, "Complete pinned source/reference closure required")
    for path, expected in pins.items():
        need(sha(path) == expected, "Pinned source/reference changed: " + path)
    response = build["compiler_response"]
    need(sha(response["path"]) == response["sha256"], "Pinned r3 compiler response changed")
    output = Path(output_path).absolute()
    need(not output.exists(), "Fresh T Logs output directory required")
    output.mkdir(parents=True)

    args = [json.loads(row) for row in Path(response["path"]).read_text().splitlines() if row.strip()]
    mains = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-main:")]
    outs = [i for i, value in enumerate(args) if isinstance(value, str) and value.startswith("-out:")]
    need(len(mains) == 1 and len(outs) == 1, "Unique source entry and output required")
    control = Path(__file__).resolve().with_name(CONTROL_NAME)
    need(control.is_file(), "Source-host ECS control fixture missing")
    executable = output / "S073OriginalAriaStepSourceHostControls.exe"
    args[mains[0]] = "-main:S073OriginalAriaStepSourceHostControls"
    args[outs[0]] = "-out:" + str(executable)
    args.append(str(control))
    compile_response = output / "controls.compile.rsp"
    compile_response.write_text("\n".join(json.dumps(value) for value in args) + "\n", encoding="utf-8")

    dotnets = [Path(path) for path in pins if Path(path).name == "dotnet"]
    cscs = [Path(path) for path in pins if Path(path).name == "csc.dll"]
    need(len(dotnets) == 1 and cscs, "Pinned Roslyn and dotnet toolchain required")
    cscs.sort(key=lambda path: tuple(int(part) for part in path.parents[2].name.split(".")))
    compile_result = subprocess.run([str(dotnets[0]), str(cscs[-1]), "@" + str(compile_response)],
                                    capture_output=True, text=True, timeout=180)
    compile_log = output / "compile.log"
    compile_log.write_text(compile_result.stdout + compile_result.stderr, encoding="utf-8")
    need(compile_result.returncode == 0 and executable.is_file(),
         "Focused ECS controls failed to compile; inspect compile.log")

    runtime = build["runtime_selection"]
    mono = Path(runtime["mono"]).resolve(strict=True)
    mono_config = mono.parent.parent / "etc/mono/config"
    need(str(mono) in pins and sha(mono) == pins[str(mono)] and mono_config.is_file(),
         "Pinned build-matched Mono runtime required")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([
        str(repo / "Library/ScriptAssemblies"), str(Path(runtime["engine"])),
        str(Path(runtime["newtonsoft"]).parent), str(Path(build["placement_build"]).parent)
    ])
    run_result = subprocess.run([str(mono), "--config", str(mono_config), "--optimize=-float32", str(executable)],
                                 capture_output=True, text=True, timeout=120, env=env)
    run_log = output / "run.log"
    run_log.write_text(run_result.stdout + run_result.stderr, encoding="utf-8")
    need(run_result.returncode == 0 and PASS_MARKER in run_result.stdout,
         "Focused ECS controls failed; inspect run.log")

    receipt = {
        "schema_version": 1,
        "kind": "S073OriginalAriaStepSourceHostControlsReceipt",
        "status": "Passed",
        "base_build": {"path": str(build_path), "sha256": sha(build_path)},
        "base_compiler_response": response,
        "verified_source_reference_pin_count": len(pins),
        "control_source": {"path": str(control), "sha256": sha(control)},
        "control_executable": {"path": str(executable), "sha256": sha(executable)},
        "control_compile_response": {"path": str(compile_response), "sha256": sha(compile_response)},
        "compile_log": {"path": str(compile_log), "sha256": sha(compile_log)},
        "run_log": {"path": str(run_log), "sha256": sha(run_log)},
        "pass_marker": PASS_MARKER,
        "fixture_cases": ["absent root", "partial lifecycle tuple", "ambiguous owners", "stale owner sequence"],
        "synthetic_fixtures_only": True,
        "planner_step_invoked": False,
        "entity_manager_mutations_observed": False,
        "actions_or_input_synthesized": False,
        "gameplay_executed": False,
        "mission_ready": False,
        "unity_launched": False,
    }
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(receipt, separators=(",", ":")))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: run_s073_original_aria_step_source_host_controls.py <fresh-T-Logs-output-dir>")
    run(sys.argv[1])
