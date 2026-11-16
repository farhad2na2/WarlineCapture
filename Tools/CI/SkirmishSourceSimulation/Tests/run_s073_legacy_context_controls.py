#!/usr/bin/env python3
"""Compile and run focused S073 legacy-context controls against a checked source build."""
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
    require(not output.exists(), "Fresh isolated test output is required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
            "A passing checked source build is required")
    repo = Path(build["repository_root"]).resolve(strict=True)
    selected_map = repo / "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
    selected_map_sha = sha(selected_map)
    placement_inputs = {
        "physical": (Path("/private/tmp/s005-primary-source-project-r1/Logs/SkirmishProduction/s002-current-physical-valid-20261029.json"), "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"),
        "water": (Path("/private/tmp/s002-water-build-observed-20261029/capture-00.json"), "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5"),
        "startup": (Path("/private/tmp/s002-water-build-startup-20261029/startup-observation.json"), "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03"),
        "selected_surface_graph": (Path("/private/tmp/s002-current-prepare-20261029/source-config-graph.json"), "64d01052bdefe3bb68350cb781b19a3f3a71f68d1e681d8abb3ac83df655a039"),
        "s073_native_authored_cache": (repo / "Logs/SkirmishProduction/s073-authored-building-mask-20261031-r3.bin", "b4c979394eb734ccd978b30896303dbb1c1ac058e7fc3eaef5da3fa0cb98c8b8"),
    }
    for label, (path, expected) in placement_inputs.items():
        require(sha(path) == expected, "Retained placement capture changed: " + label)
    pins = build["source_and_reference_pins"]
    for path, expected in pins.items():
        require(sha(path) == expected, "Source/build input changed: " + path)

    control_source = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073LegacyContextControls.cs"
    control_runner = Path(__file__).resolve(strict=True)
    control_source_sha = sha(control_source)
    runner_sha = sha(control_runner)
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    require(sha(response_path) == response["sha256"], "Checked build response file changed")
    arguments = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    main_index = [i for i, value in enumerate(arguments) if value == "-main:SourceSimulationProgram"]
    out_index = [i for i, value in enumerate(arguments) if value.startswith("-out:")]
    require(len(main_index) == 1 and len(out_index) == 1, "Expected one source main and output argument")
    test_exe = output / "S073LegacyContextControls.exe"
    arguments[main_index[0]] = "-main:S073LegacyContextControls"
    arguments[out_index[0]] = "-out:" + str(test_exe)
    arguments.append(str(control_source))
    test_response = output / "controls.rsp"
    test_response.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")

    executable_pins = list(pins)
    dotnet = next((path for path in executable_pins if Path(path).name == "dotnet"), None)
    compiler = next((path for path in executable_pins if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    require(dotnet is not None and compiler is not None, "Pinned dotnet/Roslyn compiler missing")
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    require(mono_helper.is_file() and mono_native.is_file(), "Pinned Unity Mono native helpers missing")
    mono_config = output / "mono-config.xml"
    mappings = ["MonoPosixHelper"]
    native_mappings = ["System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"]
    dllmaps = "".join("<dllmap dll=\"" + name + "\" target=\"" + str(mono_helper) + "\" os=\"!windows\"/>"
                     for name in mappings) + "".join(
        "<dllmap dll=\"" + name + "\" target=\"" + str(mono_native) + "\" os=\"!windows\"/>"
        for name in native_mappings)
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")
    began = time.monotonic()
    compile_result = subprocess.run([dotnet, compiler, "@" + str(test_response)], capture_output=True,
                                    text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    if compile_result.returncode == 0:
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join([
            str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
        ])
        env["WARLINE_S073_OPERATION_MAP_PATH"] = str(selected_map)
        env["WARLINE_S073_OPERATION_MAP_SHA256"] = selected_map_sha
        env["WARLINE_S073_PHYSICAL_PATH"], env["WARLINE_S073_PHYSICAL_SHA256"] = map(str, placement_inputs["physical"])
        env["WARLINE_S073_WATER_PATH"], env["WARLINE_S073_WATER_SHA256"] = map(str, placement_inputs["water"])
        env["WARLINE_S073_STARTUP_PATH"], env["WARLINE_S073_STARTUP_SHA256"] = map(str, placement_inputs["startup"])
        env["WARLINE_S073_SURFACE_GRAPH_PATH"], env["WARLINE_S073_SURFACE_GRAPH_SHA256"] = map(str, placement_inputs["selected_surface_graph"])
        env["WARLINE_S073_NATIVE_AUTHORED_MASK_PATH"], env["WARLINE_S073_NATIVE_AUTHORED_MASK_SHA256"] = map(str, placement_inputs["s073_native_authored_cache"])
        env["WARLINE_S073_SOURCE_ROOT"] = str(repo)
        env["WARLINE_PLACEMENT_DIAGNOSTIC_TIMING"] = "1"
        try:
            run_result = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), str(test_exe)], env=env,
                                        capture_output=True, text=True, timeout=60)
            (output / "run.log").write_text(run_result.stdout + run_result.stderr)
        except subprocess.TimeoutExpired as error:
            stdout = error.stdout or ""
            stderr = error.stderr or ""
            if isinstance(stdout, bytes):
                stdout = stdout.decode("utf-8", errors="replace")
            if isinstance(stderr, bytes):
                stderr = stderr.decode("utf-8", errors="replace")
            (output / "run.log").write_text(stdout + stderr)
            result = {
                "schema_version": 1,
                "kind": "S073LegacyContextControls",
                "status": "Failed",
                "failure": "bounded shared placement-data load timed out",
                "timeout_seconds": 60,
                "compile_exit": compile_result.returncode,
                "run_exit": None,
                "build_receipt": str(build_path),
                "build_receipt_sha256": sha(build_path),
                "test_source": str(control_source),
                "test_source_sha256": control_source_sha,
                "test_runner_sha256": runner_sha,
                "selected_s073_operation_map": {"path": str(selected_map), "sha256": selected_map_sha},
                "s073_shared_placement_inputs": {label: {"path": str(path), "sha256": expected}
                                                 for label, (path, expected) in placement_inputs.items()},
                "native_or_player_launched": False,
                "world_allocated": False,
                "mission_outcome_observed": False,
                "mission_ready": False
            }
            (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
            print(json.dumps(result, separators=(",", ":")))
            return 1
    if sha(control_source) != control_source_sha or sha(control_runner) != runner_sha:
        raise RuntimeError("Focused test source changed during execution")
    for path, expected in pins.items():
        require(sha(path) == expected, "Source/build input changed during test: " + path)
    require(sha(selected_map) == selected_map_sha, "S073 selected OperationMapDefinition changed during test")
    for label, (path, expected) in placement_inputs.items():
        require(sha(path) == expected, "Retained placement capture changed during test: " + label)
    passed = compile_result.returncode == 0 and run_result is not None and run_result.returncode == 0 and \
        "[S073LegacyContextControls] result=Passed" in run_result.stdout
    result = {
        "schema_version": 1,
        "kind": "S073LegacyContextControls",
        "status": "Passed" if passed else "Failed",
        "compile_exit": compile_result.returncode,
        "run_exit": None if run_result is None else run_result.returncode,
        "wall_seconds": time.monotonic() - began,
        "build_receipt": str(build_path),
        "build_receipt_sha256": sha(build_path),
        "test_source": str(control_source),
        "test_source_sha256": control_source_sha,
        "test_runner_sha256": runner_sha,
        "selected_s073_operation_map": {"path": str(selected_map), "sha256": selected_map_sha},
        "s073_shared_placement_inputs": {label: {"path": str(path), "sha256": expected}
                                         for label, (path, expected) in placement_inputs.items()},
        "native_or_player_launched": False,
        "world_allocated": False,
        "mission_outcome_observed": False,
        "mission_ready": False
    }
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if passed else 1


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: run_s073_legacy_context_controls.py <build.json> <fresh-output-dir>")
    raise SystemExit(run(sys.argv[1], sys.argv[2]))
