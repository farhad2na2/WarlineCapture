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


def run(build_path, output_path, snapshot_path, snapshot_sha, identity_path, config_path, config_sha,
        source_repo, asset_repo, template_path, template_sha, definitions_path, definitions_sha,
        physical_path, water_path, startup_path, surface_graph_path, relocation_path, relocation_sha):
    build_path = Path(build_path).resolve(strict=True)
    output = Path(output_path).absolute()
    snapshot = Path(snapshot_path).resolve(strict=True)
    identity = Path(identity_path).resolve(strict=True)
    identity_sha = sha(identity)
    config = Path(config_path).resolve(strict=True)
    repo = Path(source_repo).resolve(strict=True)
    asset_root = Path(asset_repo).resolve(strict=True)
    template = Path(template_path).resolve(strict=True)
    definitions = Path(definitions_path).resolve(strict=True)
    relocation = Path(relocation_path).resolve(strict=True)
    require(sha(relocation) == relocation_sha, "Pinned S073 captured-source relocation receipt changed")
    require(not output.exists(), "Fresh T: output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
            "Passing Roslyn source build required")
    require(sha(snapshot) == snapshot_sha and sha(config) == config_sha and
            sha(template) == template_sha and sha(definitions) == definitions_sha,
            "Pinned S073 prefab/config/template/definition inputs changed")
    selected_map_relative = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
    selected_map = repo / selected_map_relative
    snapshot_document = json.loads(snapshot.read_text())
    snapshot_source_pins = snapshot_document.get("source_pins", {})
    map_pin = snapshot_source_pins.get(selected_map_relative)
    require(isinstance(map_pin, str) and len(map_pin) == 64 and
            all(c in "0123456789abcdef" for c in map_pin) and sha(selected_map) == map_pin,
            "Selected S073 map must match its pinned prefab-capture source identity")
    placement_inputs = {
        "operation_map": (selected_map, map_pin),
        "physical": (Path(physical_path).resolve(strict=True), "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"),
        "water": (Path(water_path).resolve(strict=True), "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5"),
        "startup": (Path(startup_path).resolve(strict=True), "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03"),
        "selected_surface_graph": (Path(surface_graph_path).resolve(strict=True), "64d01052bdefe3bb68350cb781b19a3f3a71f68d1e681d8abb3ac83df655a039"),
        "s073_native_authored_cache": (repo / "Logs/SkirmishProduction/s073-authored-building-mask-20261031-r3.bin", "b4c979394eb734ccd978b30896303dbb1c1ac058e7fc3eaef5da3fa0cb98c8b8"),
        "s073_native_authored_cache_manifest": (repo / "Logs/SkirmishProduction/s073-authored-building-mask-20261031-r3.bin.json", "24770b37d86e55e86cbe52f97090e6cfecac93cdd231be1f8c8b89d34120dd9c"),
    }
    for label, (path, expected) in placement_inputs.items():
        require(sha(path) == expected, "Retained S073 placement input changed: " + label)
    relocation_receipt = json.loads(relocation.read_text())
    physical = placement_inputs["physical"][0]
    require(relocation_receipt.get("schema_version") == 1 and
            relocation_receipt.get("kind") == "S073CapturedSourcePinPreservation" and
            relocation_receipt.get("status") == "Passed" and
            relocation_receipt.get("capture_path") == str(physical) and
            relocation_receipt.get("capture_sha256") == placement_inputs["physical"][1] and
            relocation_receipt.get("mission_ready") is False and
            isinstance(relocation_receipt.get("preserved_root"), str) and
            Path(relocation_receipt["preserved_root"]).is_dir() and
            relocation_receipt.get("project_files") == len(relocation_receipt.get("files", [])),
            "Strict relocation receipt for the exact retained S073 physical capture required")
    graph_generator = repo / "Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_operation_map_graph.py"
    graph_generator_sha = sha(graph_generator)
    graph_output = output / "selected-operation-map-graph"
    graph_generation = subprocess.run([
        sys.executable, str(graph_generator), "--repo", str(repo),
        "--base-graph", str(placement_inputs["selected_surface_graph"][0]),
        "--base-graph-sha256", placement_inputs["selected_surface_graph"][1],
        "--selected-map-sha256", map_pin, "--output", str(graph_output)
    ], capture_output=True, text=True, timeout=60)
    (output / "selected-operation-map-graph-generation.log").write_text(
        graph_generation.stdout + graph_generation.stderr)
    require(graph_generation.returncode == 0,
            "Source-authored selected S073 map graph projection failed")
    graph_result = json.loads((graph_output / "result.json").read_text())
    require(graph_result.get("kind") == "S073SelectedOperationMapSourceGraphExtension" and
            graph_result.get("status") == "Passed" and
            graph_result.get("selected_map", {}).get("sha256") == map_pin,
            "Exact selected S073 OperationMap graph extension required")
    selected_map_graph = Path(graph_result["output_graph"]["path"]).resolve(strict=True)
    selected_map_graph_sha = graph_result["output_graph"]["sha256"]
    require(sha(selected_map_graph) == selected_map_graph_sha,
            "Generated selected S073 OperationMap graph changed")
    placement_inputs["selected_operation_map_graph"] = (selected_map_graph, selected_map_graph_sha)
    pins = build["source_and_reference_pins"]
    for path, expected in pins.items():
        require(sha(path) == expected, "Checked source build pin changed: " + path)

    harness = repo / "Tools/CI/SkirmishSourceSimulation/Tests/S073LegacyStartupOwnerHarness.cs"
    harness_sha = sha(harness)
    occupancy_adapter = repo / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073OccupancyContributorCensus.cs"
    occupancy_adapter_sha = sha(occupancy_adapter)
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
    arguments.append(str(harness))
    rsp = output / "harness.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")

    pinned_tools = list(pins)
    dotnet = next((path for path in pinned_tools if Path(path).name == "dotnet"), None)
    compiler = next((path for path in pinned_tools if Path(path).name == "csc.dll"), None)
    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    require(dotnet and compiler, "Pinned .NET/Roslyn toolchain required")
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    mono_installed_config = runtime["mono"].parent.parent / "etc/mono/config"
    require(mono_helper.is_file() and mono_native.is_file() and mono_installed_config.is_file(),
            "Pinned Unity Mono native helpers and config required")
    mono_support = {str(path): sha(path) for path in (mono_helper, mono_native, mono_installed_config)}
    require(pins.get(str(mono_helper)) == mono_support[str(mono_helper)] and
            pins.get(str(mono_native)) == mono_support[str(mono_native)] and
            pins.get(str(mono_installed_config)) == mono_support[str(mono_installed_config)],
            "Selected Unity Mono libraries, helper and installed config must match source-build pins")
    mono_config = output / "mono-config.xml"
    mappings = ["MonoPosixHelper"]
    native_mappings = ["System.Native", "System.Net.Security.Native", "System.Security.Cryptography.Native.Apple"]
    dllmaps = "".join("<dllmap dll=\"" + name + "\" target=\"" + str(mono_helper) + "\" os=\"!windows\"/>"
                      for name in mappings) + "".join(
        "<dllmap dll=\"" + name + "\" target=\"" + str(mono_native) + "\" os=\"!windows\"/>"
        for name in native_mappings)
    mono_config.write_text("<configuration>" + dllmaps + "</configuration>\n")
    mono_config_sha = sha(mono_config)
    began = time.monotonic()
    compile_result = subprocess.run([dotnet, compiler, "@" + str(rsp)], capture_output=True,
                                    text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    timed_out = False
    if compile_result.returncode == 0:
        env = os.environ.copy()
        env["WARLINE_S073_HOST_OUTPUT"] = str(output)
        env["MONO_PATH"] = os.pathsep.join([
            str(repo / "Library/ScriptAssemblies"), str(runtime["engine"]),
            str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)
        ])
        env["WARLINE_S073_OPERATION_MAP_SHA256"] = placement_inputs["operation_map"][1]
        env["WARLINE_S073_PHYSICAL_PATH"], env["WARLINE_S073_PHYSICAL_SHA256"] = map(str, placement_inputs["physical"])
        env["WARLINE_S073_WATER_PATH"], env["WARLINE_S073_WATER_SHA256"] = map(str, placement_inputs["water"])
        env["WARLINE_S073_STARTUP_PATH"], env["WARLINE_S073_STARTUP_SHA256"] = map(str, placement_inputs["startup"])
        env["WARLINE_S073_CAPTURE_RELOCATION_PATH"] = str(relocation)
        env["WARLINE_S073_CAPTURE_RELOCATION_SHA256"] = relocation_sha
        env["WARLINE_S073_SURFACE_GRAPH_PATH"], env["WARLINE_S073_SURFACE_GRAPH_SHA256"] = map(str, placement_inputs["selected_operation_map_graph"])
        env["WARLINE_S073_NATIVE_AUTHORED_MASK_PATH"], env["WARLINE_S073_NATIVE_AUTHORED_MASK_SHA256"] = map(str, placement_inputs["s073_native_authored_cache"])
        env["WARLINE_S073_NATIVE_AUTHORED_MASK_MANIFEST_SHA256"] = placement_inputs["s073_native_authored_cache_manifest"][1]
        try:
            harness_args = [snapshot, identity, config, repo, asset_root, template, template_sha,
                            definitions, definitions_sha]
            run_result = subprocess.run([str(runtime["mono"]), "--config", str(mono_config),
                                         str(exe), *map(str, harness_args)],
                                        env=env, capture_output=True, text=True, timeout=90)
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
    for path, expected in mono_support.items():
        require(sha(path) == expected, "Bundled Mono runtime support file changed during harness run: " + path)
    require(sha(mono_config) == mono_config_sha, "Harness Mono DLL map changed during run")
    for label, (path, expected) in placement_inputs.items():
        require(sha(path) == expected, "S073 placement input changed during harness run: " + label)
    require(sha(graph_generator) == graph_generator_sha,
            "S073 selected OperationMap graph generator changed during harness run")
    require(sha(harness) == harness_sha and sha(occupancy_adapter) == occupancy_adapter_sha and sha(snapshot) == snapshot_sha and
            sha(identity) == identity_sha and sha(config) == config_sha and
            sha(template) == template_sha and sha(definitions) == definitions_sha,
            "Harness or S073 input changed during run")
    placement_result = None
    if run_result is not None:
        prefix = "[S073LegacyInitialBuildingPlacement] "
        for line in run_result.stdout.splitlines():
            if line.startswith(prefix):
                placement_result = json.loads(line[len(prefix):])
    harness_ran = compile_result.returncode == 0 and not timed_out and run_result is not None and \
        run_result.returncode == 0 and "[S073LegacyStartupOwnerHarness] result=Passed" in run_result.stdout and placement_result is not None
    placement_status = placement_result.get("status") if placement_result else None
    status = {"Passed": "Passed", "Rejected": "PlacementRejected", "Incomplete": "PlacementIncomplete"}.get(placement_status, "Failed") if harness_ran else "Failed"
    result = {
        "schema_version": 1,
        "kind": "S073LegacyStartupOwnerHarness",
        "status": status,
        "failure": "90-second harness timeout" if timed_out else None,
        "compile_exit": compile_result.returncode,
        "run_exit": None if run_result is None else run_result.returncode,
        "wall_seconds": time.monotonic() - began,
        "build_receipt": str(build_path),
        "build_receipt_sha256": sha(build_path),
        "mono_config": {"path": str(mono_config), "sha256": mono_config_sha},
        "mono_runtime_support": {path: {"sha256": digest, "build_pinned": pins.get(path) == digest}
                                 for path, digest in mono_support.items()},
        "harness_source": str(harness),
        "harness_source_sha256": harness_sha,
        "occupancy_adapter_source": str(occupancy_adapter),
        "occupancy_adapter_source_sha256": occupancy_adapter_sha,
        "prefab_capture": {"path": str(snapshot), "sha256": snapshot_sha},
        "prefab_identity_readback": {"path": str(identity), "sha256": identity_sha},
        "legacy_config": {"path": str(config), "sha256": config_sha},
        "template_capture": {"path": str(template), "sha256": template_sha},
        "legacy_building_definitions": {"path": str(definitions), "sha256": definitions_sha},
        "placement_inputs": {label: {"path": str(path), "sha256": expected}
                             for label, (path, expected) in placement_inputs.items()},
        "capture_relocation": {"path": str(relocation), "sha256": relocation_sha,
                               "preserved_root": relocation_receipt["preserved_root"],
                               "project_files": relocation_receipt["project_files"]},
        "placement_result": placement_result,
        "source_repo": str(repo),
        "asset_repo": str(asset_root),
        "unity_launched": False,
        "mission_world_allocated": False,
        "initial_facilities_placed": False,
        "mission_outcome_observed": False,
        "mission_ready": False
    }
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if status == "Passed" else 1


if __name__ == "__main__":
    if len(sys.argv) != 20:
        raise SystemExit("usage: run_s073_legacy_startup_owner_harness.py <build.json> <fresh-output> <prefabs.json> <prefab-sha256> <identity-readback.json> <legacy-config.json> <config-sha256> <source-repository> <asset-repository> <template-capture.json> <template-sha256> <legacy-building-definitions.json> <definitions-sha256> <physical-capture.json> <water-capture.json> <startup-capture.json> <surface-config-graph.json> <capture-relocation-receipt.json> <relocation-sha256>")
    raise SystemExit(run(*sys.argv[1:]))
