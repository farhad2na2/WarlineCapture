#!/usr/bin/env python3
"""Compile and run the isolated S073 source-host birth-profile controls."""
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


BUILD_RELATIVE = Path("Logs/SkirmishProduction/s073-source-birth-profile-build-20261103-r1/build.json")
BUILD_SHA256 = "4a8f4f0234ada1af2bbf45761c335dc03755e2a0a51f308b523a7c3c3d204098"
BUNDLE_RELATIVE = Path("Logs/SkirmishProduction/s073-shared-grid-frame-binding-20261102-r4/diagnostic-bundle/s073-current-source-diagnostic-bundle.json")
BUNDLE_SHA256 = "82929eb8fe6312a5ca7177a0d41f920d6255236aa371938806f8e8a58e41f9cc"
CAPTURE_SHA256 = "f489eceeea22f0eb51dbb7d44ba99b759a8a2b0e8a85561fb532b2a317773523"
CAPTURE_PATH = Path("/private/tmp/s073-current-offset-prefab-20261101-r2/prefab-reference-data.json")
OUTPUT_RELATIVE = Path("T Logs/SkirmishProduction/s073-source-birth-profile-control-20261103-r1")
CONTROL_NAME = "S073SourceHostBirthProfileControl.cs"
PASS_MARKER = "[S073SourceHostBirthProfileControl] result=Passed scope=ExactSourceHostEntityManagerOnly nativeWorldCompleteness=0 missionReadiness=0"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def load_json(path):
    return json.loads(Path(path).read_text())


def verify_build(repo, build_path):
    need(build_path.resolve(strict=True) == (repo / BUILD_RELATIVE).resolve(strict=True),
         "Only the pinned S073 source-birth-profile build receipt is admitted")
    need(sha(build_path) == BUILD_SHA256, "S073 source-host build receipt hash changed")
    build = load_json(build_path)
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073" and build.get("runtime_backend") == "LegacyS073OwnerV1" and
         build.get("compile_exit") == 0 and build.get("native_or_player_launched") is False,
         "Passing source-only S073 build receipt required")
    need(Path(build["repository_root"]).resolve(strict=True) == repo.resolve(strict=True),
         "Build receipt repository root mismatch")

    pins = build.get("source_and_reference_pins")
    need(isinstance(pins, dict) and len(pins) >= 100, "Complete pinned build source set required")
    for path, expected in pins.items():
        need(sha(path) == expected, "Built source/reference pin changed: " + path)

    for key in ("source_manifest", "canonical_source_manifest", "source_resolution_pin"):
        descriptor = build.get(key)
        need(isinstance(descriptor, dict) and sha(descriptor["path"]) == descriptor["sha256"],
             "Build descriptor pin changed: " + key)

    response = build.get("compiler_response")
    need(isinstance(response, dict) and sha(response["path"]) == response["sha256"],
         "Compiler response file changed")
    executable = build.get("executable")
    need(isinstance(executable, dict) and sha(executable["path"]) == executable["sha256"],
         "Source-host executable changed")
    placement_path = Path(build["placement_build"]).resolve(strict=True)
    placement = load_json(placement_path)
    need(placement.get("status") == "Passed" and placement.get("compile_exit") == 0,
         "Passing pinned placement support build required")
    return build, pins, response, executable, placement_path


def verify_capture(repo):
    bundle_path = repo / BUNDLE_RELATIVE
    need(sha(bundle_path) == BUNDLE_SHA256, "Current S073 diagnostic bundle hash changed")
    bundle = load_json(bundle_path)
    need(bundle.get("kind") == "S073LegacyStartupBundle" and bundle.get("schema_version") == 1 and
         bundle.get("mission") == "S073" and bundle.get("seed") == 104729 and
         Path(bundle["source_repository_root"]).resolve(strict=True) == repo.resolve(strict=True),
         "Exact current S073 source bundle required")
    descriptor = bundle.get("prefab_capture")
    need(isinstance(descriptor, dict) and Path(descriptor.get("path", "")).resolve(strict=True) == CAPTURE_PATH.resolve(strict=True) and
         descriptor.get("sha256") == CAPTURE_SHA256 and sha(descriptor["path"]) == CAPTURE_SHA256,
         "Exact four-root S073 prefab-capture descriptor/pin required")
    capture = load_json(descriptor["path"])
    roots = capture.get("opening_prefabs")
    expected_roots = {
        "Unit_Veh_Light_Armored_Car", "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker",
        "Unit_Chr_Soldier_Male_02_Alt_04"
    }
    need(capture.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         capture.get("schema_version") == 1 and
         capture.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
         isinstance(capture.get("entities"), list) and len(capture["entities"]) == 140 and
         isinstance(roots, dict) and set(roots) == expected_roots,
         "S073 initial prefab capture root/entity closure changed")
    return bundle_path, bundle, Path(descriptor["path"]), capture, sorted(roots)


def compile_control(repo, build, pins, response, output):
    response_path = Path(response["path"]).resolve(strict=True)
    args = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    output_indices = [i for i, arg in enumerate(args)
                      if isinstance(arg, str) and arg.startswith("-out:")]
    need(len(output_indices) == 1, "Unique build output argument required")

    control_path = Path(__file__).resolve().with_name(CONTROL_NAME)
    need(control_path.is_file(), "S073 source-host control source missing")
    exe_path = output / "S073SourceHostBirthProfileControl.exe"
    args.extend([str(control_path), "-main:S073SourceHostBirthProfileControl"])
    args[output_indices[0]] = "-out:" + str(exe_path)
    response_out = output / "control.compile.rsp"
    response_out.write_text("\n".join(json.dumps(arg) for arg in args) + "\n")

    dots = [Path(path) for path in pins if Path(path).name == "dotnet"]
    cscs = sorted((Path(path) for path in pins if Path(path).name == "csc.dll"),
                  key=lambda path: tuple(int(part) for part in path.parents[2].name.split(".")))
    need(len(dots) == 1 and cscs, "Build-pinned Roslyn and dotnet required")
    proc = subprocess.run([str(dots[0]), str(cscs[-1]), "@" + str(response_out)],
                          capture_output=True, text=True, timeout=120)
    compile_log = output / "compile.log"
    compile_log.write_text(proc.stdout + proc.stderr)
    need(proc.returncode == 0 and exe_path.is_file(),
         "Focused control compilation failed; inspect compile.log")
    return control_path, exe_path, response_out, compile_log


def run_control(build, pins, capture_path, roots, exe_path, output):
    mono = Path(build["runtime_selection"]["mono"]).resolve(strict=True)
    need(str(mono) in pins and sha(mono) == pins[str(mono)], "Build-pinned Mono required")
    mono_config = mono.parent.parent / "etc/mono/config"
    need(mono_config.is_file(), "Build-matched Mono config required")
    runtime = build["runtime_selection"]
    mono_path = [str(Path(build["repository_root"]) / "Library/ScriptAssemblies"),
                 str(Path(runtime["engine"])), str(Path(runtime["newtonsoft"]).parent),
                 str(Path(build["placement_build"]).parent)]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join(mono_path)
    proc = subprocess.run([str(mono), "--config", str(mono_config), "--optimize=-float32",
                           str(exe_path), str(capture_path), str(Path(capture_path).parent)],
                          capture_output=True, text=True, timeout=120, env=env)
    run_log = output / "run.log"
    run_log.write_text(proc.stdout + proc.stderr)
    need(proc.returncode == 0, "Source-host controls failed; inspect run.log")
    need(PASS_MARKER in proc.stdout, "Required S073 source-host control pass marker missing")
    report_lines = [line for line in proc.stdout.splitlines() if line.startswith("[S073SourceHostBirthProfileControl]")]
    need(len(report_lines) == 1, "Exactly one source-host control report required")
    return report_lines[0], run_log


def run(output_path):
    repo = Path(__file__).resolve().parents[4]
    build_path = repo / BUILD_RELATIVE
    output = Path(output_path).absolute()
    need(not output.exists(), "Fresh T Logs receipt directory required")
    output.mkdir(parents=True)

    build, pins, response, executable, placement_path = verify_build(repo, build_path)
    bundle_path, bundle, capture_path, capture, roots = verify_capture(repo)
    control_path, control_exe, control_rsp, compile_log = compile_control(
        repo, build, pins, response, output)
    control_report, run_log = run_control(
        build, pins, capture_path, roots, control_exe, output)

    receipt = {
        "schema_version": 1,
        "kind": "S073SourceHostBirthProfileControlReceipt",
        "status": "Passed",
        "build_receipt": {"path": str(build_path), "sha256": sha(build_path),
                           "executable": executable, "compiler_response": response},
        "source_pin_count_verified": len(pins),
        "placement_build": {"path": str(placement_path), "sha256": sha(placement_path)},
        "bundle": {"path": str(bundle_path), "sha256": sha(bundle_path),
                   "kind": bundle["kind"]},
        "prefab_capture": {"path": str(capture_path), "sha256": sha(capture_path),
                           "entity_count": len(capture["entities"]), "root_count": len(roots),
                           "roots": roots},
        "control_source": {"path": str(control_path), "sha256": sha(control_path)},
        "control_executable": {"path": str(control_exe), "sha256": sha(control_exe)},
        "compile_response": {"path": str(control_rsp), "sha256": sha(control_rsp)},
        "compile_log": {"path": str(compile_log), "sha256": sha(compile_log)},
        "run_log": {"path": str(run_log), "sha256": sha(run_log)},
        "control_report": control_report,
        "source_host_only": True,
        "native_world_completeness": False,
        "mission_executed": False,
        "mission_ready": False
    }
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, separators=(",", ":")))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: run_s073_source_host_birth_profile_control.py <fresh-T-Logs-output-dir>")
    run(sys.argv[1])
