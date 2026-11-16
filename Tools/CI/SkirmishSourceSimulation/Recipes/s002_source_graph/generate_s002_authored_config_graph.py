#!/usr/bin/env python3
"""Build a current S002 authored-config graph from checked-in Unity YAML.

This is a data-only graph builder. It reuses the strict Ruby YAML reader and
GUID/anchor gates used by the S004 source graph author; it does not compile a
setup, construct a World, export geometry, or qualify runtime owners.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import tempfile

HERE = Path(__file__).resolve().parent
GENERIC = HERE.parent / "s004_source_graph" / "generate_s004_typed_config_graph.py"
ROOTS = (
    "Assets/Game/Resources/SkirmishExpansionCatalog.asset",
    "Assets/Game/Resources/SkirmishBattleCatalog.asset",
    "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset",
    "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset",
)
SCENARIO = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset"
S002_MAP = "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset"
MATRIX = "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv"
S002_PRESET = "Assets/Game/Resources/SkirmishBaseAssault.asset"
MATCH_SCENE = "Assets/Game/Scenes/Match.unity"
PRESET_SCRIPT = "Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs"
MATCH_VIEW_SCRIPT = "Assets/Game/Scripts/Composition/MatchSceneView.cs"
PRESET_SELECTION_POLICY = "Assets/Game/Scripts/Configs/SkirmishPresetConfig.cs"
MATCH_SKIRMISH_POLICY = "Assets/Game/Scripts/Composition/MatchSceneView.Skirmish.cs"
BASELINE_BUILDING_FIELD = "buildingPlacement"
UNIT_REGISTRY_FIELD = "unitPrefabRegistryConfig"
SCENE_BUILDING_FIELD = "buildingPlacementConfig"
MAX_NODES = 64
MAX_REFS = 1024


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def need(ok: bool, why: str) -> None:
    if not ok:
        raise ValueError(why)


def unique_guid_target(h, assets: Path, guid: str, context: str) -> Path:
    hits = h.guid_hits(assets, guid)
    need(len(hits) == 1, f"GUID must resolve to exactly one current asset: {context}={guid}")
    return hits[0]


def cached_guid_target(h, assets: Path, guid: str, context: str, cache: dict | None = None) -> Path:
    if cache is None:
        return unique_guid_target(h, assets, guid, context)
    hits = cache.get(guid, [])
    need(len(hits) == 1, f"GUID must resolve to exactly one current asset: {context}={guid}")
    target = hits[0]
    need(target.is_file() and target.name.endswith(".meta") and
         re.findall(rb"^guid: ([0-9a-f]{32})\r?$", target.read_bytes(), re.M) == [guid.encode()],
         "Cached GUID target changed: " + context)
    return target


def scan_current_meta_guids(assets: Path, maximum: int) -> dict[str, list[Path]]:
    """Index current metadata GUIDs once, including CRLF files and duplicate targets."""
    index: dict[str, list[Path]] = {}
    for root, directories, files in os.walk(assets, followlinks=False):
        for name in directories:
            need(not (Path(root) / name).is_symlink(), "Symlink directory in current asset GUID domain")
    for meta in assets.rglob("*.meta"):
        need(not meta.is_symlink() and meta.is_file(), "Regular current asset metadata file required: " + str(meta))
        info = meta.stat()
        need(0 < info.st_size <= maximum, "Bounded current asset metadata file required: " + str(meta))
        raw = meta.read_bytes()
        for match in re.finditer(rb"(?m)^guid: ([0-9a-f]{32})\r?$", raw):
            index.setdefault(match.group(1).decode(), []).append(meta)
    return index


def require_unique_anchor(h, target: Path, file_id, context: str) -> None:
    raw = target.read_bytes()
    declared = re.findall(rb"^--- !u!\d+ &(-?\d+)\s*$", raw, re.M)
    key = str(file_id).encode()
    need(declared.count(key) == 1 and h.unity_anchor(target, file_id),
         "Unity fileID must identify exactly one YAML document: " + context)


def select_scene_component(components: list[tuple[int, dict]], script_guid: str) -> tuple[int, dict]:
    selected = []
    for file_id, component in components:
        script = component.get("m_Script") if isinstance(component, dict) else None
        if isinstance(script, dict) and script.get("fileID") == 11500000 and \
                script.get("guid") == script_guid and script.get("type") == 3:
            selected.append((file_id, component))
    need(len(selected) == 1, "Match scene must contain exactly one GUID-bound MatchSceneView component")
    return selected[0]


def require_scene_game_object_link(component_id: int, component: dict, game_objects: dict[int, dict]) -> int:
    game_object_ref = component.get("m_GameObject") if isinstance(component, dict) else None
    need(isinstance(game_object_ref, dict) and set(game_object_ref) == {"fileID"} and
         isinstance(game_object_ref["fileID"], int) and game_object_ref["fileID"] != 0,
         "MatchSceneView must have an exact in-scene GameObject reference")
    game_object_id = game_object_ref["fileID"]
    game_object = game_objects.get(game_object_id)
    need(isinstance(game_object, dict), "MatchSceneView GameObject fileID must resolve uniquely in Match.unity")
    component_rows = game_object.get("m_Component")
    need(isinstance(component_rows, list), "MatchSceneView GameObject component list required")
    linked = [row for row in component_rows if isinstance(row, dict) and row.get("component") == {"fileID": component_id}]
    need(len(linked) == 1, "MatchSceneView component must link exactly once from its referenced GameObject")
    return game_object_id


def strict_scene_component_fields(repo: Path, h, scene: Path, script_guid: str) -> tuple[int, dict, int]:
    """Select the unique serialized MatchSceneView component from the pinned scene."""
    raw = scene.read_bytes()
    docs = list(re.finditer(rb"(?m)^--- !u!(\d+) &(-?\d+)(?:[ \t]+stripped)?\s*$", raw))
    components = []
    game_objects = {}
    for index, match in enumerate(docs):
        if match.group(1) not in (b"1", b"114"):
            continue
        end = docs[index + 1].start() if index + 1 < len(docs) else len(raw)
        document = raw[match.start():end]
        normalized = re.sub(rb"^--- !u!\d+ &-?\d+(?:[ \t]+stripped)?\s*$", b"---", document,
                            count=1, flags=re.M)
        with tempfile.TemporaryDirectory(prefix="s002-match-scene-") as temp:
            path = Path(temp) / "component.unity"
            path.write_bytes(b"%YAML 1.1\n" + normalized)
            parsed = h.strict_read(repo / h.READER_REL, [path])[str(path)]
        if match.group(1) == b"1":
            go = parsed.get("GameObject")
            if isinstance(go, dict):
                game_object_id = int(match.group(2))
                need(game_object_id not in game_objects, "Duplicate Match scene GameObject fileID")
                game_objects[game_object_id] = go
        else:
            component = parsed.get("MonoBehaviour")
            if isinstance(component, dict):
                components.append((int(match.group(2)), component))
    component_id, component = select_scene_component(components, script_guid)
    game_object_id = require_scene_game_object_link(component_id, component, game_objects)
    return component_id, component, game_object_id


def exact_asset_reference(value, context: str) -> tuple[str, int, int]:
    need(isinstance(value, dict) and set(value) == {"fileID", "guid", "type"} and
         isinstance(value["guid"], str) and re.fullmatch(r"[0-9a-f]{32}", value["guid"]) and
         isinstance(value["fileID"], int) and value["fileID"] != 0 and value["type"] == 2,
         "Exact non-null ScriptableObject reference required: " + context)
    return value["guid"], value["fileID"], value["type"]


def resolve_asset_reference(repo: Path, h, ref: dict, context: str, guid_cache: dict | None = None) -> tuple[Path, dict]:
    guid, file_id, _ = exact_asset_reference(ref, context)
    target_meta = cached_guid_target(h, repo / "Assets", guid, context, guid_cache)
    _, target = h.checked_meta_target(repo / "Assets", target_meta)
    need(target.suffix == ".asset", "Provider reference must resolve to a current config asset: " + context)
    require_unique_anchor(h, target, file_id, context)
    meta, checked = h.checked_meta_target(repo / "Assets", Path(str(target) + ".meta"))
    need(checked == target, "Provider GUID target changed during resolution: " + context)
    return target, {"guid": guid, "file_id": file_id, "type": 2,
                    "path": str(target.relative_to(repo)), "sha256": sha(target.read_bytes()),
                    "meta_sha256": sha(meta.read_bytes())}


def read_serialized_config(repo: Path, h, path: Path, expected_script: Path, expected_class: str) -> tuple[dict, str]:
    checked = h.checked_asset_path(repo / "Assets", path)
    need(checked == path and path.suffix == ".asset", "Exact current serialized provider config asset required: " + str(path))
    meta, _ = h.checked_meta_target(repo / "Assets", Path(str(path) + ".meta"))
    document = h.strict_read(repo / h.READER_REL, [path])[str(path)]
    mono = document.get("MonoBehaviour")
    need(isinstance(mono, dict), "Provider config MonoBehaviour required: " + str(path))
    script = mono.get("m_Script")
    expected_script_guid = re.findall(rb"^guid: ([0-9a-f]{32})$", Path(str(expected_script) + ".meta").read_bytes(), re.M)
    need(len(expected_script_guid) == 1 and isinstance(script, dict) and
         script == {"fileID": 11500000, "guid": expected_script_guid[0].decode(), "type": 3},
         "Provider config must bind exact pinned source script: " + str(path))
    script_meta_hit = unique_guid_target(h, repo / "Assets", expected_script_guid[0].decode(), str(path) + " script")
    _, script_target = h.checked_meta_target(repo / "Assets", script_meta_hit)
    need(script_target == expected_script, "Provider config script GUID resolves to unexpected source class")
    identifier = mono.get("m_EditorClassIdentifier", "")
    if expected_class == "SkirmishPresetConfig":
        need(identifier == "Game.Configs::Game.Configs.SkirmishPresetConfig",
             "S002 preset exact serialized class identifier required")
    else:
        need(identifier in (None, ""), "Scene provider config expected inherited script serialization shape")
    fields = {key: value for key, value in mono.items() if not key.startswith("m_")}
    return fields, sha(path.read_bytes())


def resolve_prefab_provider_rows(repo: Path, h, refs: list, context: str, pin, guid_cache: dict | None = None) -> list[dict]:
    need(isinstance(refs, list), "Ordered provider reference list required: " + context)
    result = []
    for index, ref in enumerate(refs):
        need(isinstance(ref, dict) and set(ref) == {"fileID", "guid", "type"} and
             isinstance(ref["guid"], str) and re.fullmatch(r"[0-9a-f]{32}", ref["guid"]) and
             isinstance(ref["fileID"], int) and ref["fileID"] != 0 and ref["type"] == 3,
             "Exact ordered non-null prefab ref required: " + context + "[" + str(index) + "]")
        meta_hit = cached_guid_target(h, repo / "Assets", ref["guid"], context + "[" + str(index) + "]", guid_cache)
        meta, target = h.checked_meta_target(repo / "Assets", meta_hit)
        need(target.suffix == ".prefab", "Provider reference must resolve to prefab: " + context + "[" + str(index) + "]")
        anchor_status = "anchor_present_unqualified" if h.unity_anchor(target, ref["fileID"]) else "unresolved"
        prefab_hash, meta_hash = pin(target), pin(meta)
        result.append({"index": index, "key": target.stem, "path": str(target.relative_to(repo)),
                       "guid": ref["guid"], "file_id": ref["fileID"], "type": ref["type"],
                       "local_id_validation": anchor_status,
                       "prefab_sha256": prefab_hash, "meta_sha256": meta_hash})
    return result


def _generic():
    spec = importlib.util.spec_from_file_location("s004_graph_helpers", GENERIC)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


def generate(repo: Path, output: Path) -> dict:
    repo = repo.resolve(strict=True)
    out = output.absolute()
    need(not out.exists(), "fresh output directory required")
    generator_path = Path(__file__).resolve()
    generator_sha = sha(generator_path.read_bytes())
    h = _generic()
    assets = repo / "Assets"
    guid_cache: dict[str, list[Path]] = scan_current_meta_guids(assets, h.MAX_FILE)
    reader = repo / h.READER_REL
    scenario = repo / SCENARIO
    matrix = repo / MATRIX
    preset = repo / S002_PRESET
    match_scene = repo / MATCH_SCENE
    for path in (reader, scenario, matrix, preset, match_scene):
        need(path.is_file() and not path.is_symlink(), "required current source missing: " + str(path))

    pins: dict[str, str] = {}
    nodes: dict[str, dict] = {}
    script_guids: dict[str, str] = {}
    parsed: dict[str, dict] = {}
    queue = [(repo / rel, "root") for rel in (*ROOTS, SCENARIO)]
    reference_count = 0

    def pin(path: Path) -> str:
        lexical = Path(os.path.abspath(path))
        if lexical.is_relative_to(Path(os.path.abspath(assets))):
            path = h.checked_asset_path(assets, lexical)
        raw = path.read_bytes()
        need(0 < len(raw) <= h.MAX_FILE, "empty/oversized source input: " + str(path))
        digest = sha(raw)
        prior = pins.setdefault(str(path), digest)
        need(prior == digest, "source changed during graph generation: " + str(path))
        return digest

    pin(reader)
    pin(GENERIC)
    pin(matrix)
    pin(Path(str(matrix) + ".meta"))
    pin(scenario)
    pin(Path(str(scenario) + ".meta"))
    preset_script = repo / PRESET_SCRIPT
    match_view_script = repo / MATCH_VIEW_SCRIPT
    match_view_policy = repo / MATCH_SKIRMISH_POLICY
    preset_selection_policy = repo / PRESET_SELECTION_POLICY
    for path in (preset, Path(str(preset) + ".meta"), preset_script, Path(str(preset_script) + ".meta"),
                 match_scene, Path(str(match_scene) + ".meta"), match_view_script, Path(str(match_view_script) + ".meta"),
                 match_view_policy, Path(str(match_view_policy) + ".meta"),
                 preset_selection_policy, Path(str(preset_selection_policy) + ".meta")):
        pin(path)

    preset_script_guid = re.findall(rb"^guid: ([0-9a-f]{32})$", Path(str(preset_script) + ".meta").read_bytes(), re.M)
    match_script_guid = re.findall(rb"^guid: ([0-9a-f]{32})$", Path(str(match_view_script) + ".meta").read_bytes(), re.M)
    need(len(preset_script_guid) == 1 and len(match_script_guid) == 1,
         "exact SkirmishPresetConfig and MatchSceneView script GUIDs required")
    preset_script_guid = preset_script_guid[0].decode()
    match_script_guid = match_script_guid[0].decode()
    preset_meta = Path(str(preset) + ".meta").read_bytes()
    preset_guids = re.findall(rb"^guid: ([0-9a-f]{32})$", preset_meta, re.M)
    need(len(preset_guids) == 1, "exact current S002 preset GUID required")
    preset_guid = preset_guids[0].decode()
    preset_meta_hit = cached_guid_target(h, assets, preset_guid, "S002 baseline preset", guid_cache)
    _, preset_hit = h.checked_meta_target(assets, preset_meta_hit)
    need(preset_hit == preset, "S002 preset GUID must resolve to the exact packaged baseline resource")

    scene_meta = Path(str(match_scene) + ".meta").read_bytes()
    scene_guids = re.findall(rb"^guid: ([0-9a-f]{32})$", scene_meta, re.M)
    scene_meta_hit = cached_guid_target(h, assets, scene_guids[0].decode(), "Match scene", guid_cache) if len(scene_guids) == 1 else None
    _, scene_target = h.checked_meta_target(assets, scene_meta_hit) if scene_meta_hit is not None else (None, None)
    need(len(scene_guids) == 1 and scene_target == match_scene,
         "Match scene GUID must resolve uniquely to the exact current scene")
    match_component_file_id, match_component, match_game_object_file_id = strict_scene_component_fields(
        repo, h, match_scene, match_script_guid)
    scene_building_ref = match_component.get(SCENE_BUILDING_FIELD)
    scene_building_path, scene_building_identity = resolve_asset_reference(
        repo, h, scene_building_ref, "MatchSceneView." + SCENE_BUILDING_FIELD, guid_cache)
    scene_building_identity["sha256"] = pin(scene_building_path)
    scene_building_identity["meta_sha256"] = pin(Path(str(scene_building_path) + ".meta"))

    preset_fields, preset_sha = read_serialized_config(repo, h, preset, preset_script, "SkirmishPresetConfig")
    baseline_path, baseline_identity = resolve_asset_reference(
        repo, h, preset_fields.get("buildingPlacement"), "SkirmishPresetConfig." + BASELINE_BUILDING_FIELD, guid_cache)
    baseline_identity["sha256"] = pin(baseline_path)
    baseline_identity["meta_sha256"] = pin(Path(str(baseline_path) + ".meta"))
    baseline_script = repo / "Assets/Game/Scripts/Configs/Scene/BuildingPlacementSystemSceneConfigAsset.cs"
    baseline_fields, _ = read_serialized_config(repo, h, baseline_path, baseline_script,
                                                "BuildingPlacementSystemSceneConfigAsset")
    unit_registry_path, unit_registry_identity = resolve_asset_reference(
        repo, h, baseline_fields.get(UNIT_REGISTRY_FIELD), "baseline." + UNIT_REGISTRY_FIELD, guid_cache)
    unit_registry_identity["sha256"] = pin(unit_registry_path)
    unit_registry_identity["meta_sha256"] = pin(Path(str(unit_registry_path) + ".meta"))
    unit_script = repo / "Assets/Game/Scripts/Configs/UnitPrefabRegistryAuthoringConfig.cs"
    unit_fields, _ = read_serialized_config(repo, h, unit_registry_path, unit_script,
                                            "UnitPrefabRegistryAuthoringConfig")
    scene_fields, _ = read_serialized_config(repo, h, scene_building_path, baseline_script,
                                             "BuildingPlacementSystemSceneConfigAsset")
    scene_unit_registry_path, scene_unit_registry_identity = resolve_asset_reference(
        repo, h, scene_fields.get(UNIT_REGISTRY_FIELD), "Match scene." + UNIT_REGISTRY_FIELD, guid_cache)
    scene_unit_registry_identity["sha256"] = pin(scene_unit_registry_path)
    scene_unit_registry_identity["meta_sha256"] = pin(Path(str(scene_unit_registry_path) + ".meta"))
    scene_unit_fields, _ = read_serialized_config(repo, h, scene_unit_registry_path, unit_script,
                                                  "UnitPrefabRegistryAuthoringConfig")
    script_paths = (baseline_script, unit_script)
    for script in script_paths:
        pin(script)
        pin(Path(str(script) + ".meta"))

    ref_count = 0
    while queue:
        path, selected_from = queue.pop(0)
        path = h.checked_asset_path(assets, path)
        need(path.is_relative_to(assets.resolve(strict=True)) and path.suffix == ".asset",
             "graph dependency escaped authored Assets or is not a ScriptableObject asset: " + str(path))
        rel = str(path.relative_to(repo))
        meta, _ = h.checked_meta_target(assets, Path(str(path) + ".meta"))
        meta_raw = meta.read_bytes()
        guids = re.findall(rb"^guid: ([0-9a-f]{32})$", meta_raw, re.M)
        need(len(guids) == 1, "asset needs one GUID: " + str(meta))
        guid = guids[0].decode()
        if guid in nodes:
            need(nodes[guid]["source_path"] == rel, "duplicate GUID resolves to different assets: " + guid)
            continue
        need(len(nodes) < MAX_NODES, "authored S002 config node bound exceeded")
        digest = pin(path)
        pin(meta)
        if str(path) not in parsed:
            parsed[str(path)] = h.strict_read(reader, [path])[str(path)]
        doc = parsed[str(path)]
        mono = doc.get("MonoBehaviour")
        need(isinstance(mono, dict), "expected MonoBehaviour source asset: " + rel)
        identifier = mono.get("m_EditorClassIdentifier", "")
        script = mono.get("m_Script")
        need(isinstance(script, dict) and script.get("fileID") == 11500000 and script.get("type") == 3,
             "exact MonoScript reference required: " + rel)
        script_guid = script.get("guid")
        need(isinstance(script_guid, str), "MonoScript GUID required: " + rel)
        script_hit = unique_guid_target(h, assets, script_guid, rel + ":m_Script")
        script_meta, script_path = h.checked_meta_target(assets, script_hit)
        need(isinstance(identifier, str) and "::" in identifier,
             "typed editor class identifier missing: " + rel + " (guid=" + guid + ", selected from " + selected_from + ")")
        source_type = identifier.rsplit("::", 1)[-1].rsplit(".", 1)[-1]
        need(source_type in h.ALLOWED, "source type outside existing materializer profile: " + source_type)
        need(script_path.suffix == ".cs" and script_path.stem == source_type,
             "MonoScript must resolve to the exact checked-in config type: " + rel)
        pin(script_meta)
        pin(script_path)
        script_guids[script_guid] = str(script_path.relative_to(repo))
        fields = {key: value for key, value in mono.items() if not key.startswith("m_")}
        spellings = {key[len("MonoBehaviour."):]: value
                     for key, value in doc.get("__source_scalar_spellings", {}).items()
                     if key.startswith("MonoBehaviour.")}
        need(spellings, "strict scalar spelling transcript missing: " + rel)
        row_refs = h.refs(fields, "fields")
        ref_count += len(row_refs)
        need(ref_count <= MAX_REFS, "authored S002 config reference bound exceeded")
        nodes[guid] = {"asset_guid": guid, "source_type": source_type, "source_path": rel,
                       "source_sha256": digest, "fields": fields, "scalar_spellings": spellings}

        for ref_path, ref in row_refs:
            ref_guid = ref.get("guid")
            ref_hit = unique_guid_target(h, assets, ref_guid, rel + ":" + ref_path)
            ref_meta, target = h.checked_meta_target(assets, ref_hit)
            pin(ref_meta)
            pin(target)
            if target.suffix == ".cs":
                need(ref.get("fileID") == 11500000, "MonoScript fileID mismatch: " + rel + ":" + ref_path)
                continue
            need(target.suffix == ".asset",
                 "config reference must target a checked-in asset: " + rel + ":" + ref_path)
            require_unique_anchor(h, target, ref.get("fileID"), rel + ":" + ref_path)
            queue.append((target, rel + ":" + ref_path))

    baseline_building_rows = resolve_prefab_provider_rows(
        repo, h, baseline_fields.get("spawnables"), "baseline building providers", pin, guid_cache)
    scene_building_rows = resolve_prefab_provider_rows(
        repo, h, scene_fields.get("spawnables"), "Match scene building providers", pin, guid_cache)
    unit_rows = resolve_prefab_provider_rows(
        repo, h, unit_fields.get("unitSpawnPrefabs"), "baseline unit providers", pin, guid_cache)
    scene_unit_rows = resolve_prefab_provider_rows(
        repo, h, scene_unit_fields.get("unitSpawnPrefabs"), "Match scene unit providers", pin, guid_cache)
    for policy in (match_view_policy, preset_selection_policy, repo / "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs"):
        pin(policy)
        pin(Path(str(policy) + ".meta"))
    preset_source = preset_selection_policy.read_text(encoding="utf-8")
    launch_source = (repo / "Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs").read_text(encoding="utf-8")
    match_source = match_view_policy.read_text(encoding="utf-8")
    need('ResourceName = "SkirmishBaseAssault"' in preset_source and
         "scenarioIndex == StressScaleProbeScenarioIndex ? StressResourceName" in preset_source and
         ": ResourceName;" in preset_source,
         "Current S002 source preset resource selection policy changed")
    need("SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex)" in launch_source,
         "Current launch projection no longer selects the S002 Desert Base baseline preset")
    need("var baseline = SkirmishPreset.buildingPlacement;" in match_source,
         "Match skirmish catalog no longer consumes the selected preset building placement")
    match_view_source = match_view_script.read_text(encoding="utf-8")
    need("BuildingPlacementSystemConfig BuildingPlacementConfig =>" in match_view_source and
         "IsSkirmishSession ? ResolvedSkirmishBuildingPlacement : buildingPlacementConfig" in match_view_source and
         "Create(setup, baseline, buildingPlacementConfig," in match_source,
         "MatchSceneView must route its serialized buildingPlacementConfig into the shared skirmish catalog owner")
    provider_registry_inputs = {
        "kind": "S002SerializedProviderRegistryInputs",
        "scope": "Pinned source YAML and serialized provider reference order only; no importer execution, metadata factory, runtime registry admission, World, source-fit, or mission readiness.",
        "preset": {"path": S002_PRESET, "guid": preset_guid, "sha256": preset_sha,
                   "scenario_index": 0, "resource_name": "SkirmishBaseAssault"},
        "match_scene": {"path": MATCH_SCENE, "guid": scene_guids[0].decode(),
                         "sha256": pins[str(match_scene)], "match_scene_view_script_guid": match_script_guid,
                         "component_file_id": match_component_file_id,
                         "game_object_file_id": match_game_object_file_id,
                         "game_object_component_link_verified": True,
                         "building_placement_field": SCENE_BUILDING_FIELD,
                         "building_placement_owner_usage_verified": True},
        "baseline_building_placement_class": "Game.Configs.BuildingPlacementSystemSceneConfigAsset",
        "scene_building_placement_class": "Game.Configs.BuildingPlacementSystemSceneConfigAsset",
        "unit_registry_class": "Game.Configs.UnitPrefabRegistryAuthoringConfig",
        "baseline_building_placement": baseline_identity,
        "scene_building_placement": scene_building_identity,
        "baseline_unit_registry": unit_registry_identity,
        "scene_unit_registry": scene_unit_registry_identity,
        "ordered_rows": {
            "baseline_buildings": baseline_building_rows,
            "scene_buildings": scene_building_rows,
            "baseline_units": unit_rows,
            "scene_units": scene_unit_rows
        }
    }

    # S002 map geometry GUIDs are serialized scalar identities, not Unity object refs.
    # Resolve and pin their exact current files without modifying or inventing geometry.
    map_node = next((n for n in nodes.values() if n["source_path"] == S002_MAP), None)
    need(map_node is not None and map_node["source_type"] == "OperationMapDefinition",
         "current Desert Base source map must resolve through the existing graph")
    geometry_targets = {}
    for field, name in (("gridMetadata", "assetGuid"), ("surfaceMetadata", "assetGuid"),
                        ("mapSurfaceDataReference", "m_AssetGUID"), ("sourceSceneReference", "m_AssetGUID")):
        row = map_node["fields"].get(field)
        guid = row.get(name) if isinstance(row, dict) else None
        if not guid:
            continue
        hit = unique_guid_target(h, assets, guid, "map." + field)
        target_meta, target = h.checked_meta_target(assets, hit)
        pin(target_meta)
        raw = target.read_bytes()
        need(0 < len(raw) <= 64 * 1024 * 1024, "bounded physical map source required: " + str(target))
        geometry_targets[field] = {"guid": guid, "path": str(target.relative_to(repo)), "sha256": sha(raw),
                                   "bytes": len(raw)}

    out.mkdir(parents=True)
    need(sha(generator_path.read_bytes()) == generator_sha,
         "S002 graph generator changed during generation")
    graph = {"schema_version": 1, "authority": "AuthoredConfigAssets",
             "source_pins": dict(sorted(pins.items())),
             "nodes": sorted(nodes.values(), key=lambda n: n["source_path"]),
             "compiled_setup": False, "world_initialized": False, "native_world_state_used": False,
             "definition_path": SCENARIO,
             "catalog_path": "Assets/Game/Resources/SkirmishExpansionCatalog.asset",
             "battle_catalog_path": "Assets/Game/Resources/SkirmishBattleCatalog.asset",
             "setup_matrix": {"path": str(matrix), "sha256": pins[str(matrix)]},
             "geometry_source_targets": geometry_targets,
             "provider_registry_inputs": provider_registry_inputs,
             "scope": "Current S002 authored config graph from checked-in Unity YAML, including pinned baseline preset and GUID-bound Match scene provider references. Exact serialized config fields and list order only; no setup compilation, geometry generation, runtime metadata, registry admission, World, or readiness."}
    target = out / "source-config-graph.json"
    target.write_text(json.dumps(graph, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    for path, digest in pins.items():
        need(sha(Path(path).read_bytes()) == digest, "source pin changed before graph receipt: " + path)
    need(sha(generator_path.read_bytes()) == generator_sha,
         "S002 graph generator changed before proof receipt")
    proof = {"kind": "S002CurrentAuthoredConfigGraph", "status": "Passed",
             "graph": {"path": str(target), "sha256": sha(target.read_bytes())},
             "generator": {"path": str(generator_path.relative_to(repo)), "sha256": generator_sha,
                           "unchanged_before_and_after_generation": True},
             "node_count": len(nodes), "reference_count": ref_count,
             "script_count": len(script_guids), "source_pin_count": len(pins),
             "geometry_source_targets": geometry_targets,
             "compiled_setup": False, "world_initialized": False, "mission_ready": False,
             "scope": graph["scope"]}
    proof_path = out / "graph-proof.json"
    proof_path.write_text(json.dumps(proof, indent=2) + "\n", encoding="utf-8")
    if sha(generator_path.read_bytes()) != generator_sha:
        proof["status"] = "Failed"
        proof["generator"]["unchanged_before_and_after_generation"] = False
        proof["failure"] = "Generator source hash changed during output finalization"
        proof_path.write_text(json.dumps(proof, indent=2) + "\n", encoding="utf-8")
        raise ValueError(proof["failure"])
    return proof


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[5])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(generate(args.repo, args.output), indent=2))
    except (OSError, ValueError, KeyError, TypeError) as exc:
        raise SystemExit("S002 authored config graph failed closed: " + str(exc))
