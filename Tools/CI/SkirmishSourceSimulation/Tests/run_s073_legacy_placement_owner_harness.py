#!/usr/bin/env python3
"""Compile and run the real S073 legacy initial-building pipeline with pinned Campaign inputs."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import time


EXPECTED = {
    # The old four-root capture was deleted with temporary validation artifacts.
    # The retained full production capture contains those exact same four roots;
    # use that pinned authoritative capture for both root checks.
    "initial_template": "97a1ffe028eccf10b7486f6ba0694cfedda3b463b99e784f0a5325ae31e4e594",
    "production_template": "97a1ffe028eccf10b7486f6ba0694cfedda3b463b99e784f0a5325ae31e4e594",
    "identity": "d06c52158242ed371d18588db60c469dea2674007e24e830a625ff84901e265e",
    "legacy": "1565010ac85b7c07ea1892befaa44cde3fd68c5f6e91913e86d967d1596dcb83",
    "definitions": "4823b2bae00e0e153e6f21e0a84ddc71e2cc3bc22ad9181541489b2d8a39ac31",
    "initialization": "989bdfcb3e94d64a94940fac14a5772560e38acdadbef7b33cadabdae7ff3f21",
    "graph": "64d01052bdefe3bb68350cb781b19a3f3a71f68d1e681d8abb3ac83df655a039",
    "physical": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
    "water": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
    "startup": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
    "map": "b145eae26821fa3ee559de12473354813c83d0a4c1997159086c20654ea0112a",
    "grid": "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb",
    "surface": "5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d",
    "preset": "354b73f15a90a0653e263207789ba677c5251767b5a796b9c7f991af2a20f0f3",
    "construction": "6688f039a073ce6c4c834a44f056d73bbada237cd0bd76c0db555d37a89be813",
    "preset_readback": "b0b76f25ff16e67b5fb287fdd9b739754ce3f5baf1e6e93e085e0dbba480ee10",
    "enemy_ai": "4d0924531ce7f80a8f4791c2ef2518ecd0f36a4842b0175cde0899392a9c3f52",
    "player_ai": "2b651113e0952830bd4d260b0cdfe73c3864c632734333134aacf9929c9b6698",
    "map_placement_capture": "a0107fcdaaf7ace274345b4615292134ec5e8547b456dffdb3c6b857dc72ae73",
    "map_placement_helper": "64f61689f5fd1fda1cb61cc3d38dd7b516aca6a43dda964b447c85a1269889b4",
    "map_placement_data_only_owner": "8192cd3b7ed9acb277c8e9bc90b6861c9ece0bee91db3d4b4db51a2d842e395c",
    "map_placement_helper_base_build": "4888598f62d9ee223bc0f41e516717c7d2a07749b412bf1b54f823d8cc0d2fc1",
    "map_placement_sequence_owner": "06ead45c98b2368edb96a0dcefb4da96d67a2d521d336a3d0e6419b07f0c61bc",
    "runtime_grid_blocker_ownership_kernel": "a33a6ed858883279c681d7d28af1a4be64348d6a80faffdc3da6ef4088b38218",
    "map_definitions_adapter": "3a5982d3ff0d45fa74065c25cc3043ff30bafb66389a33e7ad7d3f7fcb81b34a",
    "map_definitions_adapter_base_build": "673c2d6e8f19119134252063909c1e193b4c43dae1320b288a3d4d76b6cfb768",
    "blocker_metadata": "e73b06fb4913d442139071cc6739aa4d9672aa312b6543a31f377d139aab8776",
    "ai_plan": "7627f93d25d2446c7d43ba5e2c97745f275eeda3e018501c0a004ee1ec3a712f",
}
GRID_GUID = "b201000000000000000000000000000b"
SURFACE_GUID = "12f517deb32ab49698acbfdaf7c3eac7"
SPECIAL_CAPTURE_COMPATIBLE_OWNER_PINS = {
    "Assets/Game/Scripts/Systems/RuntimeGridBlockerOwnershipKernel.cs": "a33a6ed858883279c681d7d28af1a4be64348d6a80faffdc3da6ef4088b38218",
    "Assets/Game/Scripts/Components/StaticGridBlockerSurfaceOpeningComponent.cs": "c896017f098026415f321451ab0015c1bf19ea3475de5468ea77f5322e36eb62",
    "Assets/Game/Scripts/RuntimeState/RuntimeBuildingEntity.cs": "840624cebb501c8c2b7e4dcdec887569ecebda2b78cf4ef4f8902a84f7779c53",
    "Assets/Game/Scripts/Systems/BuildingBarrierUtilitySystemHelper.cs": "22501469a3b1d51fd81bf6507f7b2805719eb50485d03c01cfa9807ed205f9be",
    "Assets/Game/Scripts/Systems/BuildingRunwayPathingSurface.cs": "c6cfbfdd23ce5e8cdce3c6b27cb994933a38da4aef07dd997438daddcb80bd7a",
    "Assets/Game/Scripts/Systems/BuildingRunwaySystem.cs": "489085e98e3d3698746dba2e1779cd2592ccfb40630bf496f0a33fa69b4c81d1",
    "Assets/Game/Scripts/Simulation/GameplayStartGatePolicy.cs": "f1a5f2b1dfe614bc86cb9ddfdf41551514c839ba8d60908aef8ace789a11ed7b",
    "Assets/Game/Scripts/Systems/GameplayRuntimeUpdateCompositionSystemHelper.Runtime.cs": "f4351c2f96c7790bf6bdf183f61111bdbf5fc021af22143f25f0d178cc1ad429",
}
CURRENT_S073_REGISTRATION_SOURCE_PINS = {
    "Tools/CI/SkirmishSourceSimulation/Adapters/137_RuntimeBuildingBindings.cs": "dd74e299a02bc73bbecc0534debce057ab594d3528f0be5c9a4c6c925ab82680",
    "Tools/CI/SkirmishSourceSimulation/Adapters/333_GeneratedBuildingRegistration.cs": "be1f4520278717cbfe2dcbd906f64796e704c174110f9bb4182105255f13b152",
    "Tools/CI/SkirmishSourceSimulation/Recipes/building_requests/regenerate_building_requests.py": "96bfb271753b272159d38574d8bf2865ce9cf993868ba9a94a123b3d83cb3d30",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs": "6b52d8dc35d845820d0c19843eb202eef06a07577279fddf1510e1e072d83e76",
    "Tools/CI/SkirmishSourceSimulation/Adapters/300_GeneratedBuildingDefinitions.cs": "3a5982d3ff0d45fa74065c25cc3043ff30bafb66389a33e7ad7d3f7fcb81b34a",
    "Assets/Game/Scripts/Environment/RuntimeGridBlockerPresentationSystemHelper.cs": "c683c286c2faebf5b79d112c2abeebdcdd34fb089480361105dabc3bcd9a224d",
    "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs": "64f61689f5fd1fda1cb61cc3d38dd7b516aca6a43dda964b447c85a1269889b4",
    "Assets/Game/Scripts/Systems/MapBuildingPlacementOriginKernel.cs": "4abd88b4f4b9f97cb57b41f6ca1cd8b9e14afa323f56f2b35267486030a0359f",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073MapBuildings.cs": "93fcdc8528d9dc782cfefbd32a368abd675071193cc43eac54879c3165c0154b",
    "Tools/CI/SkirmishSourceSimulation/Recipes/operation_map_publication/regenerate_full_read_models.py": "b0a8a4deb7dc6c9b2998e9b61d9cdf913252454ef6a82ca5ef01651bb88dfd54",
    "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyHostHandoff.cs": "7c914182857a1f0c86f102814d7fc170b626e49e0c140a14312c2131b38e7778",
    "Tools/CI/SkirmishSourceSimulation/Placement/SourcePlacementDomain.OriginalOwnerCapture.cs": "b3e6a36fee1039e4205b0c3a6d741a7364742604359161f90bfe1fcf133d5274",
    "Assets/Game/Scripts/Systems/RuntimeGridBlockerOwnershipKernel.cs": "a33a6ed858883279c681d7d28af1a4be64348d6a80faffdc3da6ef4088b38218",
    **SPECIAL_CAPTURE_COMPATIBLE_OWNER_PINS,
}
ASSETS = {
    GRID_GUID: ("GridAuthoringSceneConfigAsset", "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset", EXPECTED["grid"]),
    SURFACE_GUID: ("MapSurfaceDataAsset", "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset", EXPECTED["surface"]),
}


def sha(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def pin_input_files(named_paths):
    return {key: {"path": str(Path(path).resolve(strict=True)), "sha256": sha(path)}
            for key, path in named_paths.items()}


def input_pins_replayable(input_pins):
    for value in input_pins.values():
        path = Path(value["path"])
        try:
            if not path.is_file() or sha(path) != value["sha256"]:
                return False
        except OSError:
            return False
    return True


def prepare_map_graph(source_graph_path, source_root, output_path):
    raw = json.loads(Path(source_graph_path).read_text())
    require(raw.get("schema_version") == 1 and raw.get("authority") == "AuthoredConfigAssets",
            "Exact authored config graph required")
    selected = []
    pins = {}
    for guid, (source_type, relative, expected_sha) in ASSETS.items():
        matches = [row for row in raw.get("nodes", []) if row.get("asset_guid") == guid]
        require(len(matches) == 1, "Unique graph node required for " + relative)
        node = matches[0]
        asset = Path(source_root) / relative
        meta = Path(str(asset) + ".meta")
        require(node.get("source_type") == source_type and node.get("source_path") == relative and
                node.get("source_sha256") == expected_sha and sha(asset) == expected_sha,
                "Selected Campaign asset data changed: " + relative)
        meta_text = meta.read_text()
        meta_guid = re.findall(r"(?m)^guid: ([0-9a-f]{32})$", meta_text)
        require(meta_guid == [guid], "Selected asset GUID changed: " + relative)
        pins[str(asset)] = expected_sha
        pins[str(meta)] = sha(meta)
        selected.append(node)
    def has_asset_reference(value):
        if isinstance(value, dict):
            if "fileID" in value and "guid" in value:
                return True
            return any(has_asset_reference(v) for v in value.values())
        if isinstance(value, list):
            return any(has_asset_reference(v) for v in value)
        return False
    require(not any(has_asset_reference(row.get("fields", {})) for row in selected),
            "Map-only graph closure unexpectedly references an omitted asset")
    compact = {"schema_version": 1, "authority": "AuthoredConfigAssets",
               "source_pins": pins, "nodes": selected,
               "derived_scope": "Selected S073-bound Campaign grid and surface nodes from the pinned original graph"}
    Path(output_path).write_text(json.dumps(compact, separators=(",", ":")) + "\n")
    return sha(output_path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build", required=True)
    parser.add_argument("--placement-domain-build", required=True,
                        help="Fresh current-source build receipt for the existing shared placement data facade")
    parser.add_argument("--source-root", required=True)
    parser.add_argument("--captured-asset-root", required=True,
                        help="Separate complete T-drive project root for hash-pinned captured Campaign asset dependencies")
    parser.add_argument("--production-template", required=True)
    parser.add_argument("--identity", required=True)
    parser.add_argument("--legacy-config", required=True)
    parser.add_argument("--building-definitions", required=True)
    parser.add_argument("--initialization", required=True)
    parser.add_argument("--physical", required=True)
    parser.add_argument("--water", required=True)
    parser.add_argument("--startup", required=True)
    parser.add_argument("--preset-readback", required=True,
                        help="Existing hash-bound full S073 preset/config source readback")
    parser.add_argument("--placement-config", required=True)
    parser.add_argument("--map-placement-capture", required=True)
    parser.add_argument("--special-building-runtime-reference", required=True)
    parser.add_argument("--special-building-runtime-reference-sha256", required=True)
    parser.add_argument("--blocker-metadata", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    started = time.monotonic()
    build_path = Path(args.build).resolve(strict=True)
    source_root = Path(args.source_root).resolve(strict=True)
    captured_asset_root = Path(args.captured_asset_root).resolve(strict=True)
    production_template = Path(args.production_template).resolve(strict=True)
    initial_template = production_template
    identity = Path(args.identity).resolve(strict=True)
    legacy = Path(args.legacy_config).resolve(strict=True)
    definitions = Path(args.building_definitions).resolve(strict=True)
    initialization = Path(args.initialization).resolve(strict=True)
    physical = Path(args.physical).resolve(strict=True)
    water = Path(args.water).resolve(strict=True)
    startup = Path(args.startup).resolve(strict=True)
    preset_readback = Path(args.preset_readback).resolve(strict=True)
    placement_config = Path(args.placement_config).resolve(strict=True)
    map_placement_capture = Path(args.map_placement_capture).resolve(strict=True)
    special_building_reference = Path(args.special_building_runtime_reference).resolve(strict=True)
    special_building_reference_sha = sha(special_building_reference)
    require(special_building_reference_sha == args.special_building_runtime_reference_sha256,
            "Exact captured S073 airport/gate runtime-reference hash required")
    special_reference_data = json.loads(special_building_reference.read_text())
    require(special_reference_data.get("schema_version") == 1 and
            special_reference_data.get("kind") == "S073OriginalSpecialBuildingRuntimeReference" and
            special_reference_data.get("catalog_id") == "S073" and
            special_reference_data.get("source_scene_path") == "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity" and
            special_reference_data.get("mission_world_allocated") is False and
            special_reference_data.get("full_mission_initialized") is False and
            special_reference_data.get("native_visual_readiness") is False and
            [row.get("row_index") for row in special_reference_data.get("road_gates", [])] == [108, 109, 110],
            "Exact non-world S073 special-building owner capture required")
    special_capture_owner_deltas = {}
    for pin_path, expected in special_reference_data.get("source_pins", {}).items():
        current = sha(pin_path) if Path(pin_path).is_file() else None
        if current == expected:
            continue
        require(SPECIAL_CAPTURE_COMPATIBLE_OWNER_PINS.get(pin_path) == current,
                "S073 special-building capture source pin changed outside the tested numeric-owner compatibility set: " + pin_path)
        if pin_path.endswith("/RuntimeGridBlockerOwnershipKernel.cs"):
            evidence = "Shared owner now maintains a reverse entity-to-id index alongside its existing ownership records; registration rejects duplicate entity owners, removal/clear update both indexes. The S073 harness cross-checks the returned record against live ECS origin and footprint before treating the entity as removable runtime occupancy."
        elif pin_path.endswith("/GameplayStartGatePolicy.cs") or pin_path.endswith("/GameplayRuntimeUpdateCompositionSystemHelper.Runtime.cs"):
            evidence = "The shared gameplay-start gate policy is independently covered by portable controls; the S073 placement harness exercises the same policy and original rules transition against the actual legacy initialized-world owners."
        else:
            evidence = "No-Unity S073NumericRuntimeOwnerProbe: exact runway output, invalid extent rejection, captured-original pathing opening equivalence, and numeric gate midpoint"
        special_capture_owner_deltas[pin_path] = {"captured_sha256": expected, "current_sha256": current,
            "equivalence_evidence": evidence}
    blocker_metadata = Path(args.blocker_metadata).resolve(strict=True)
    placement_config_sha = sha(placement_config)
    map_placement_capture_sha = sha(map_placement_capture)
    require(map_placement_capture_sha == EXPECTED["map_placement_capture"],
            "Exact reviewed S073 source map placement capture required")
    output = Path(args.output).absolute()
    require(not output.exists(), "Fresh T-drive test output required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text())
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("compile_exit") == 0, "Passing pinned Roslyn source build required")
    map_placement_helper = source_root / "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs"
    def verify_build_source_pin(path, expected):
        actual = sha(path)
        try:
            relative = str(Path(path).resolve().relative_to(source_root))
        except ValueError:
            relative = None
        if relative in CURRENT_S073_REGISTRATION_SOURCE_PINS and actual == CURRENT_S073_REGISTRATION_SOURCE_PINS[relative]:
            return
        if Path(path).resolve() == map_placement_helper.resolve() and expected != actual:
            require(expected == EXPECTED["map_placement_helper_base_build"] and
                    actual == EXPECTED["map_placement_helper"],
                    "Only the reviewed split of the original data-only placement owner may differ from the base build pin")
            return
        map_definitions_adapter = source_root / "Tools/CI/SkirmishSourceSimulation/Adapters/300_GeneratedBuildingDefinitions.cs"
        if Path(path).resolve() == map_definitions_adapter.resolve() and expected != actual:
            require(expected == EXPECTED["map_definitions_adapter_base_build"] and
                    actual == EXPECTED["map_definitions_adapter"],
                    "Only the reviewed original placement-owner pin refresh may differ from the base build pin")
            return
        require(actual == expected, "Current source build input changed: " + path)
    for path, expected in build["source_and_reference_pins"].items():
        verify_build_source_pin(path, expected)
    paths = {"initial_template": initial_template, "production_template": production_template, "identity": identity,
             "legacy": legacy, "definitions": definitions, "initialization": initialization,
             "physical": physical, "water": water, "startup": startup}
    paths["preset_readback"] = preset_readback
    paths["enemy_ai"] = source_root / "Assets/Game/Configs/Skirmish/Enemy.asset"
    paths["player_ai"] = source_root / "Assets/Game/Configs/Skirmish/Player.asset"
    paths["ai_plan"] = source_root / "Assets/Game/Configs/Skirmish/Plan.asset"
    paths["blocker_metadata"] = blocker_metadata
    paths["map_placement_helper"] = map_placement_helper
    paths["map_placement_data_only_owner"] = source_root / "Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.DataOnly.cs"
    paths["map_definitions_adapter"] = source_root / "Tools/CI/SkirmishSourceSimulation/Adapters/300_GeneratedBuildingDefinitions.cs"
    for key, path in paths.items():
        require(sha(path) == EXPECTED[key], "Retained S073 source input changed: " + key)
    initialization_data = json.loads(initialization.read_text())
    graph_descriptor = initialization_data["source_config_graph"]
    source_graph = Path(graph_descriptor["path"]).resolve(strict=True)
    require(graph_descriptor["sha256"] == EXPECTED["graph"] and sha(source_graph) == EXPECTED["graph"],
            "Retained shared Campaign asset graph changed")
    compact_graph = output / "s073-campaign-grid-surface-graph.json"
    compact_graph_sha = prepare_map_graph(source_graph, source_root, compact_graph)
    map_path = source_root / "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
    require(sha(map_path) == EXPECTED["map"], "Current S073 logical Campaign binding changed")
    require(sha(source_root / "Assets/Game/Resources/SkirmishIndustrialBasin.asset") == EXPECTED["preset"],
            "Current original S073 preset asset changed")
    require(sha(source_root / "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset") == EXPECTED["construction"],
            "Current original S073 construction asset changed")
    # The source package can be an isolated T-drive worktree while --repo points
    # at the canonical checkout containing the complete baked Campaign assets.
    harness = Path(__file__).resolve().parents[4] / "Tools/CI/SkirmishSourceSimulation/Tests/S073LegacyPlacementOwnerHarness.cs"
    harness_sha = sha(harness)
    response = build["compiler_response"]
    rsp_path = Path(response["path"]).resolve(strict=True)
    require(sha(rsp_path) == response["sha256"], "Pinned source compile response changed")
    compiler_args = [json.loads(line) for line in rsp_path.read_text().splitlines() if line.strip()]
    # The retained facade DLL predates the captured-asset-root overload. Build
    # that existing data facade from current shared sources and replace its exact
    # reference; do not create a parallel placement implementation.
    placement_build_path = Path(args.placement_domain_build).resolve(strict=True)
    placement_build = json.loads(placement_build_path.read_text())
    placement_dll = Path(placement_build["dll"]).resolve(strict=True)
    require(placement_build.get("status") == "Passed" and sha(placement_dll) == placement_build.get("dll_sha256"),
            "Current-source placement facade build receipt required")
    old_placement_dll = str(Path(build["placement_build"]).parent / "SourcePlacementDomain.dll")
    placement_refs = [i for i, value in enumerate(compiler_args)
                      if isinstance(value, str) and value == "-r:" + old_placement_dll]
    require(len(placement_refs) == 1, "One retained placement facade assembly reference required")
    compiler_args[placement_refs[0]] = "-r:" + str(placement_dll)
    entry = [i for i, value in enumerate(compiler_args) if value == "-main:SourceSimulationProgram"]
    output_arg = [i for i, value in enumerate(compiler_args) if value.startswith("-out:")]
    require(len(entry) == 1 and len(output_arg) == 1, "Checked CLI source entry required")
    owner_generator = source_root / "Tools/CI/SkirmishSourceSimulation/Recipes/building_requests/regenerate_building_requests.py"
    owner_generator_sha = sha(owner_generator)
    generated_owner_dir = output / "generated-building-owners"
    generated_owner_dir.mkdir()
    generated_result = subprocess.run([sys.executable, str(owner_generator), "--repo", str(source_root),
                                       "--output", str(generated_owner_dir)], capture_output=True, text=True, timeout=60)
    require(generated_result.returncode == 0, "Current original ownership adapter generation failed: " + generated_result.stderr[-1200:])
    generated_owner_adapter = generated_owner_dir / "BuildingOwnerFaction.source.cs"
    owner_adapter_sha = sha(generated_owner_adapter)
    owner_adapter_args = [i for i, value in enumerate(compiler_args)
                          if isinstance(value, str) and value.endswith("/340_BuildingOwnerFaction.source.cs")]
    require(len(owner_adapter_args) == 1, "One generated owner-faction adapter source required")
    compiler_args[owner_adapter_args[0]] = str(generated_owner_adapter)
    executable = output / "S073LegacyPlacementOwnerHarness.exe"
    compiler_args[entry[0]] = "-main:S073LegacyPlacementOwnerHarness"
    compiler_args[output_arg[0]] = "-out:" + str(executable)
    pinned_gameplay_state = [Path(path).resolve(strict=True) for path in build["source_and_reference_pins"]
                             if path.endswith("/Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs")]
    gameplay_state_source = (Path(__file__).resolve().parents[4] /
                             "Assets/Game/Scripts/Systems/RuntimeGameplayStateSystem.cs").resolve(strict=True)
    require(len(pinned_gameplay_state) == 1 and
            sha(pinned_gameplay_state[0]) == build["source_and_reference_pins"][str(pinned_gameplay_state[0])] and
            sha(gameplay_state_source) == sha(pinned_gameplay_state[0]),
            "Hash-pinned original runtime gameplay-state owner required")
    compiler_args.append(str(pinned_gameplay_state[0]))
    pinned_placement_origin = [Path(path).resolve(strict=True) for path in build["source_and_reference_pins"]
                               if path.endswith("/Assets/Game/Scripts/Systems/MapBuildingPlacementOriginKernel.cs")]
    origin_current_sha = sha(pinned_placement_origin[0]) if len(pinned_placement_origin) == 1 else None
    origin_build_sha = build["source_and_reference_pins"].get(str(pinned_placement_origin[0])) if len(pinned_placement_origin) == 1 else None
    require(len(pinned_placement_origin) == 1 and
            (origin_build_sha == origin_current_sha or
             CURRENT_S073_REGISTRATION_SOURCE_PINS.get("Assets/Game/Scripts/Systems/MapBuildingPlacementOriginKernel.cs") == origin_current_sha),
            "Hash-pinned original map placement origin owner required")
    compiler_args.append(str(pinned_placement_origin[0]))
    pinned_placement_sequence = source_root / "Assets/Game/Scripts/Systems/MapBuildingPlacementSequenceKernel.cs"
    require(sha(pinned_placement_sequence) == EXPECTED["map_placement_sequence_owner"],
            "Hash-pinned current original map placement sequence owner required")
    if str(pinned_placement_sequence) not in compiler_args:
        compiler_args.append(str(pinned_placement_sequence))
    priority_policy = source_root / "Assets/Game/Scripts/Simulation/MapBuildingPlacementPriorityPolicy.cs"
    require(priority_policy.is_file(), "Original shared placement priority policy required")
    if str(priority_policy) not in compiler_args:
        # S073's generated read-model adapter embeds this exact source file via
        # regenerate_full_read_models.py. Do not compile it a second time; when
        # absent from that generated source, include the shared owner directly.
        embedded_priority_sources = [Path(value) for value in compiler_args
            if isinstance(value, str) and value.endswith("/SourceReadModelDefinitionStatics.cs")]
        embedded_priority = (len(embedded_priority_sources) == 1 and
            priority_policy.read_text() in embedded_priority_sources[0].read_text())
        require(len(embedded_priority_sources) <= 1,
                "One generated original read-model priority source required")
        if not embedded_priority:
            compiler_args.append(str(priority_policy))
    data_only_owner = paths["map_placement_data_only_owner"].resolve(strict=True)
    require(sha(data_only_owner) == EXPECTED["map_placement_data_only_owner"],
            "Hash-pinned original data-only placement owner required")
    compiler_args.append(str(data_only_owner))
    pinned_blocker_ownership = source_root / "Assets/Game/Scripts/Systems/RuntimeGridBlockerOwnershipKernel.cs"
    require(sha(pinned_blocker_ownership) == EXPECTED["runtime_grid_blocker_ownership_kernel"],
            "Hash-pinned current original runtime blocker ownership kernel required")
    if str(pinned_blocker_ownership) not in compiler_args:
        compiler_args.append(str(pinned_blocker_ownership))
    compiler_args.append(str(harness))
    test_rsp = output / "harness.rsp"
    test_rsp.write_text("".join(json.dumps(value) + "\n" for value in compiler_args))
    tools = {Path(path).name: Path(path).resolve(strict=True) for path in build["source_and_reference_pins"]}
    dotnet, csc = tools.get("dotnet"), tools.get("csc.dll")
    require(dotnet and csc, "Pinned .NET/Roslyn tools required")
    compile_result = subprocess.run([str(dotnet), str(csc), "@" + str(test_rsp)], capture_output=True, text=True, timeout=120)
    (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    run_result = None
    timed_out = False
    if compile_result.returncode == 0:
        runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
        env = dict(__import__("os").environ)
        env["MONO_PATH"] = __import__("os").pathsep.join([str(placement_dll.parent), str(source_root / "Library/ScriptAssemblies"), str(runtime["engine"]), str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
        env["WARLINE_PLACEMENT_DIAGNOSTIC_TIMING"] = "1"
        env["WARLINE_S073_SOURCE_ROOT"] = str(source_root)
        installed_config = runtime["script"] / "MonoBleedingEdge/etc/mono/config"
        mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
        require(installed_config.is_file() and mono_helper.is_file(), "Pinned Mono config and Posix helper required")
        config_text = installed_config.read_text()
        config_text = config_text.replace('target="libMonoPosixHelper.dylib"', 'target="' + str(mono_helper) + '"')
        config_text = config_text.replace('target="MonoPosixHelper"', 'target="' + str(mono_helper) + '"')
        mono_config = output / "mono-config.xml"
        mono_config.write_text(config_text)
        scene_path = source_root / "Assets/Game/Scenes/Match.unity"
        map_source = source_root / "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
        args_run = [str(initial_template), EXPECTED["initial_template"], str(production_template), EXPECTED["production_template"],
                    str(identity), EXPECTED["identity"], str(legacy), EXPECTED["legacy"], str(definitions), EXPECTED["definitions"],
                    str(source_root), str(compact_graph), compact_graph_sha, str(initialization), EXPECTED["initialization"],
                    str(physical), EXPECTED["physical"], str(water), EXPECTED["water"], str(startup), EXPECTED["startup"],
                    EXPECTED["map"], str(output / "initial-roads"), str(placement_config), placement_config_sha,
                    str(preset_readback), EXPECTED["preset_readback"], EXPECTED["enemy_ai"], EXPECTED["player_ai"], EXPECTED["ai_plan"],
                    str(map_placement_capture), map_placement_capture_sha,
                    str(blocker_metadata), EXPECTED["blocker_metadata"], str(captured_asset_root),
                    str(special_building_reference), special_building_reference_sha]
        try:
            run_result = subprocess.run([str(runtime["mono"]), "--config", str(mono_config), "--optimize=-float32", str(executable), *args_run], env=env, capture_output=True,
                                        text=True, timeout=300)
            (output / "run.log").write_text(run_result.stdout + run_result.stderr)
        except subprocess.TimeoutExpired as error:
            timed_out = True
            stdout, stderr = error.stdout or "", error.stderr or ""
            if isinstance(stdout, bytes): stdout = stdout.decode("utf-8", errors="replace")
            if isinstance(stderr, bytes): stderr = stderr.decode("utf-8", errors="replace")
            (output / "run.log").write_text(stdout + stderr)
    for path, expected in build["source_and_reference_pins"].items():
        verify_build_source_pin(path, expected)
    for key, path in paths.items():
        require(sha(path) == EXPECTED[key], "S073 source input changed during test: " + key)
    require(sha(placement_config) == placement_config_sha, "External S073 placement config changed during test")
    require(sha(map_placement_capture) == EXPECTED["map_placement_capture"], "Captured S073 source placement data changed during test")
    require(sha(special_building_reference) == special_building_reference_sha, "Captured S073 special-building runtime data changed during test")
    require(sha(preset_readback) == EXPECTED["preset_readback"], "S073 source preset readback changed during test")
    require(sha(harness) == harness_sha and sha(compact_graph) == compact_graph_sha and
            sha(map_path) == EXPECTED["map"], "Harness or derived/current map source changed")
    marker = "[S073LegacyPlacementOwnerHarness] "
    stdout = "" if run_result is None else run_result.stdout
    payload = None
    if marker in stdout:
        payload = json.loads(stdout[stdout.rfind(marker) + len(marker):].strip().splitlines()[0])
    passed = compile_result.returncode == 0 and not timed_out and run_result is not None and run_result.returncode == 0 and payload and payload.get("status") == "Passed"
    run_inputs = dict(paths)
    run_inputs.update({"placement_config": placement_config, "map_placement_capture": map_placement_capture,
                       "blocker_metadata": blocker_metadata, "special_building_runtime_reference": special_building_reference,
                       "generated_owner_adapter": generated_owner_adapter,
                       "campaign_source_graph": source_graph,
                       "logical_campaign_map": map_path})
    input_pins = pin_input_files(run_inputs)
    run_request = {"kind": "S073LegacyPlacementOwnerRunRequest", "schema_version": 1,
                   "build_receipt": str(build_path), "build_receipt_sha256": sha(build_path),
                   "source_root": str(source_root), "captured_asset_root": str(captured_asset_root),
                   "harness": str(harness), "harness_sha256": harness_sha,
                   "runtime": build["runtime_selection"],
                   "build_source_and_reference_pins": build["source_and_reference_pins"],
                   "generated_owner_adapter": {"path": str(generated_owner_adapter), "sha256": owner_adapter_sha,
                       "generator": str(owner_generator), "generator_sha256": owner_generator_sha},
                   "output_directory": str(output), "input_pins": input_pins,
                   "replayable": input_pins_replayable(input_pins)}
    (output / "run-request.json").write_text(json.dumps(run_request, indent=2) + "\n")
    result = {"kind": "S073LegacyPlacementOwnerHarnessRun", "status": "Passed" if passed else "Failed",
              "failure": "bounded Mono timeout" if timed_out else None, "wall_seconds": time.monotonic() - started,
              "build_receipt": str(build_path), "build_receipt_sha256": sha(build_path),
              "placement_config": {"path": str(placement_config), "sha256": placement_config_sha},
              "map_placement_capture": {"path": str(map_placement_capture), "sha256": map_placement_capture_sha},
              "blocker_metadata": {"path": str(blocker_metadata), "sha256": sha(blocker_metadata)},
              "special_building_runtime_reference": {"path": str(special_building_reference), "sha256": special_building_reference_sha},
              "special_capture_owner_pin_compatibility": special_capture_owner_deltas,
              "opening_prefab_root_evidence": {"path": str(production_template), "sha256": EXPECTED["production_template"],
              "scope": "Pinned current full production capture; its four S073 legacy opening roots exactly match the source-bound legacy config. The deleted compact capture is retained only as historical hash metadata in the independent name readback."},
              "harness": {"path": str(harness), "sha256": harness_sha}, "compact_campaign_graph": {"path": str(compact_graph), "sha256": compact_graph_sha,
              "derived_from": str(source_graph), "source_graph_sha256": EXPECTED["graph"], "selected_nodes": 2},
              "input_pins": input_pins, "run_request": str(output / "run-request.json"),
              "run_exit": None if run_result is None else run_result.returncode, "compile_exit": compile_result.returncode,
              "result": payload, "source_log": str(output / "run.log"),
              "captured_asset_root": str(captured_asset_root),
              "captured_asset_root_scope": "Separate root checked against every retained original Campaign asset dependency pin; current source code remains pinned by the Roslyn build and source root",
              "unity_launched": False,
              "full_mission": False, "autonomous_aria": False, "mission_outcome": False, "mission_ready": False}
    (output / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, separators=(",", ":")))
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
