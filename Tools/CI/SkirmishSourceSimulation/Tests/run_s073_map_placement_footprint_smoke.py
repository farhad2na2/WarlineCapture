#!/usr/bin/env python3
"""Compile and run the focused S073 map/blocker policy smoke against a pinned CLI build."""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import re
import subprocess
import time


EXPECTED_BLOCKER_CAPTURE = "e73b06fb4913d442139071cc6739aa4d9672aa312b6543a31f377d139aab8776"
EXPECTED_MAP_CAPTURE = "a0107fcdaaf7ace274345b4615292134ec5e8547b456dffdb3c6b857dc72ae73"
EXPECTED_GRID_ASSET = "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb"


def sha(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", required=True)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--map-capture", required=True)
    parser.add_argument("--blocker-metadata", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    started = time.monotonic()
    repo = Path(args.repo).resolve(strict=True)
    build_path = Path(args.build).resolve(strict=True)
    map_capture = Path(args.map_capture).resolve(strict=True)
    blocker_metadata = Path(args.blocker_metadata).resolve(strict=True)
    grid_asset = repo / "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"
    output = Path(args.output).absolute()
    require(not output.exists(), "Fresh isolated S073 smoke output required")
    output.mkdir(parents=True)

    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("compile_exit") == 0, "Passing pinned Roslyn build required")
    for path, expected in build["source_and_reference_pins"].items():
        require(sha(path) == expected, "Source build input changed: " + path)
    require(sha(map_capture) == EXPECTED_MAP_CAPTURE, "Exact reviewed S073 map-placement capture required")
    require(sha(blocker_metadata) == EXPECTED_BLOCKER_CAPTURE, "Reviewed exact S073 blocker metadata capture required")
    require(sha(grid_asset) == EXPECTED_GRID_ASSET, "Exact selected S073 Campaign grid asset required")
    grid_text = grid_asset.read_text()
    cell_sizes = re.findall(r"(?m)^  cellSize: ([^\r\n]+)$", grid_text)
    require(len(cell_sizes) == 1, "Unique source grid cell size required")
    cell_size = float(cell_sizes[0])
    require(cell_size > 0 and math.isfinite(cell_size), "Finite positive source grid cell size required")

    response = build["compiler_response"]
    rsp_path = Path(response["path"]).resolve(strict=True)
    require(sha(rsp_path) == response["sha256"], "Pinned source compiler response changed")
    compiler_args = [json.loads(line) for line in rsp_path.read_text().splitlines() if line.strip()]
    entry = [i for i, value in enumerate(compiler_args) if value == "-main:SourceSimulationProgram"]
    output_arg = [i for i, value in enumerate(compiler_args) if value.startswith("-out:")]
    require(len(entry) == 1 and len(output_arg) == 1, "Checked CLI source entry required")
    compiler_args[entry[0]] = "-main:S073MapPlacementFootprintSmoke"
    executable = output / "S073MapPlacementFootprintSmoke.exe"
    compiler_args[output_arg[0]] = "-out:" + str(executable)
    smoke_source = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073MapPlacementFootprintSmoke.cs"
    smoke_source_sha = sha(smoke_source)
    compiler_args.append(str(smoke_source))
    smoke_response = output / "smoke.rsp"
    smoke_response.write_text("".join(json.dumps(value) + "\n" for value in compiler_args))

    tools = {Path(path).name: Path(path).resolve(strict=True) for path in build["source_and_reference_pins"]}
    dotnet, csc = tools.get("dotnet"), tools.get("csc.dll")
    require(dotnet and csc, "Pinned .NET/Roslyn tools required")
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    policy_output = output / "Runtime.BlockerPlacementPolicy.dll"
    policy_response = output / "runtime-policy.rsp"
    runtime_refs = [
        runtime["script"] / "NetStandard/ref/2.1.0/netstandard.dll",
        runtime["script"] / "NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll",
        runtime["engine"] / "UnityEngine.CoreModule.dll",
        runtime["engine"] / "UnityEngine.MathematicsModule.dll",
        repo / "Library/ScriptAssemblies/Unity.Collections.dll",
        repo / "Library/ScriptAssemblies/Unity.Mathematics.dll"]
    for reference in runtime_refs:
        require(reference.is_file(), "Required runtime blocker policy reference missing: " + str(reference))
    policy_args = ["-nologo", "-unsafe+", "-nostdlib+", "-langversion:latest", "-target:library",
                       "-out:" + str(policy_output)] + ["-r:" + str(path) for path in runtime_refs] + [
                           str(repo / "Assets/Game/Scripts/Simulation/RuntimeGridBlockerPlacementKernel.cs"),
                           str(repo / "Assets/Game/Scripts/Environment/RuntimeGridBlockerPlacementPolicy.cs")]
    policy_response.write_text("".join(json.dumps(value) + "\n" for value in policy_args))
    policy_compile = subprocess.run([str(dotnet), str(csc), "@" + str(policy_response)],
                                        capture_output=True, text=True, timeout=60)
    (output / "runtime-policy-compile.log").write_text(policy_compile.stdout + policy_compile.stderr)
    compile_result = subprocess.run([str(dotnet), str(csc), "@" + str(smoke_response)],
                                    capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    if policy_compile.returncode == 0 and compile_result.returncode == 0:
        env = dict(os.environ)
        env["MONO_PATH"] = os.pathsep.join([
            str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
        installed_config = runtime["script"] / "MonoBleedingEdge/etc/mono/config"
        mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
        config_text = installed_config.read_text()
        config_text = config_text.replace('target="libMonoPosixHelper.dylib"', 'target="' + str(mono_helper) + '"')
        config_text = config_text.replace('target="MonoPosixHelper"', 'target="' + str(mono_helper) + '"')
        mono_config = output / "mono-config.xml"
        mono_config.write_text(config_text)
        run_result = subprocess.run([
            str(runtime["mono"]), "--config", str(mono_config), "--optimize=-float32", str(executable),
            str(map_capture), EXPECTED_MAP_CAPTURE, str(repo), str(blocker_metadata),
            EXPECTED_BLOCKER_CAPTURE, format(cell_size, ".17g")],
            env=env, capture_output=True, text=True, timeout=60)
        (output / "run.log").write_text(run_result.stdout + run_result.stderr)

    source_unchanged = sha(smoke_source) == smoke_source_sha
    build_pins_unchanged = all(sha(path) == expected for path, expected in build["source_and_reference_pins"].items())
    marker = "[S073MapPlacementFootprintSmoke] "
    run_payload = None
    if run_result is not None:
        marker_rows = [line[len(marker):] for line in run_result.stdout.splitlines() if line.startswith(marker)]
        if len(marker_rows) == 1:
            try:
                run_payload = json.loads(marker_rows[0])
            except json.JSONDecodeError:
                run_payload = None
    passed = policy_compile.returncode == 0 and compile_result.returncode == 0 and \
        run_result is not None and run_result.returncode == 0 and \
        source_unchanged and build_pins_unchanged and run_payload is not None and run_payload.get("status") == "Passed" and \
        run_payload.get("world_allocated") is False and run_payload.get("actors_registered") is False and \
        run_payload.get("mission_initialized") is False and run_payload.get("blocker_random_placement_executed") is False and \
        type(run_payload.get("seeded_plan_fixture")) is dict and \
        run_payload["seeded_plan_fixture"].get("status") == "Passed" and \
        run_payload["seeded_plan_fixture"].get("synthetic_grid") is True and \
        run_payload["seeded_plan_fixture"].get("native_grid_predicates_used") is False and \
        run_payload["seeded_plan_fixture"].get("native_blocker_instance_observed") is False and \
        run_payload["seeded_plan_fixture"].get("repeatable_same_seed") is True
    if passed:
        metadata = json.loads(blocker_metadata.read_text())
        expected_footprints = []
        for prefab in metadata["prefabs"]:
            center_only = prefab["blocks_only_center_cell"]
            size = prefab["local_bounds_size"]
            expected_footprints.append({
                "name": prefab["name"], "kind": prefab["kind"],
                "width": 1 if center_only else max(1, math.ceil(size[0] / cell_size)),
                "depth": 1 if center_only else max(1, math.ceil(size[2] / cell_size)),
                "center_only": center_only})
        passed = run_payload.get("grid_cell_size") == cell_size and \
            run_payload.get("blocker_footprints") == expected_footprints
    result = {"kind": "S073MapPlacementFootprintSmokeRun", "status": "Passed" if passed else "Failed",
              "wall_seconds": time.monotonic() - started, "build_receipt": str(build_path),
              "build_receipt_sha256": sha(build_path), "map_capture": str(map_capture),
              "map_capture_sha256": sha(map_capture), "blocker_metadata": str(blocker_metadata),
              "blocker_metadata_sha256": sha(blocker_metadata), "source": str(smoke_source),
              "grid_asset": str(grid_asset), "grid_asset_sha256": sha(grid_asset),
              "grid_cell_size": cell_size,
              "source_sha256": smoke_source_sha, "source_unchanged": source_unchanged,
              "build_pins_unchanged": build_pins_unchanged,
              "source_result": run_payload, "runtime_policy_assembly_compile_exit": policy_compile.returncode,
              "runtime_policy_references": [str(path) for path in runtime_refs],
              "compile_exit": compile_result.returncode,
              "run_exit": None if run_result is None else run_result.returncode,
              "result_log": str(output / "run.log"), "unity_launched": False}
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
