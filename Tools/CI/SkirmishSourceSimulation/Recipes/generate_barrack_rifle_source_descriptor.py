#!/usr/bin/env python3
"""Emit current source-only authored Barrack/rifle facts with content pins.

This describes checked-in text assets and relevant source parsers only. It does
not qualify native factories, mission overlays, or runtime admission.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import re
import sys

ROOT = Path(__file__).resolve().parents[4]
BARRACK_CONFIG = Path("Assets/Game/Configs/Prefabs/Prefab_BuildingDefinition_Building_Barrack_Config.asset")
BARRACK_PREFAB = Path("Assets/Game/Prefabs/Buildings/Building_Barrack.prefab")
RIFLE_CONFIG = Path("Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset")
RIFLE_PREFAB = Path("Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab")
UNIT_SOURCE_PREFAB = Path("Assets/Game/Prefabs/Characters/Unit.prefab")
RIFLE_MODEL_PREFAB = Path("Assets/Game/Prefabs/Generated/CharactersBaked/Prefab_SM_Chr_Soldier_Male_02_Alt_04_CombinedSkinned_31.prefab")
OPERATION_MAP_IDENTITY_AUTHORING = Path("Assets/Game/Scripts/Authorings/OperationMapEntityPresentationIdentityAuthoring.cs")
TRANSPORT_CONFIG = Path("Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Helicopter_Transport_Config.asset")
TRANSPORT_PREFAB = Path("Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab")
TRANSPORT_BASE_PREFAB = Path("Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab")
RIFLE_KEY = "Unit_Chr_Soldier_Male_02_Alt_04"
GENERIC_CONSTRUCTION = Path("Assets/Game/Configs/Skirmish/Construction.asset")
GENERIC_INITIAL_FORCES = Path("Assets/Game/Configs/Skirmish/InitialForces.asset")
GENERIC_PRESET = Path("Assets/Game/Resources/SkirmishBaseAssault.asset")
EXPANSION_SCENARIOS = (
    Path("Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset"),
    Path("Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset"),
)
PARSER = Path("Tools/CI/SkirmishSourceSimulation/Recipes/unit_move_projection/run_unit_move_authoring_source_host.py")
GENERATOR = Path(__file__).resolve().relative_to(ROOT)
PARSER_SHA256 = "70d4aa8af4358c33a8e4d926e8f307b50b58cdea6811d1b4db049c23f5e1853b"
_UNIT_MOVE_PARSER_PATH = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/unit_move_projection/run_unit_move_authoring_source_host.py"
_UNIT_MOVE_SPEC = importlib.util.spec_from_file_location("warline_unit_move_source_parser", _UNIT_MOVE_PARSER_PATH)
need_parser = _UNIT_MOVE_SPEC is not None and _UNIT_MOVE_SPEC.loader is not None
if not need_parser:
    raise RuntimeError("repository UnitMove source parser unavailable")
_UNIT_MOVE_PARSER = importlib.util.module_from_spec(_UNIT_MOVE_SPEC)
_UNIT_MOVE_SPEC.loader.exec_module(_UNIT_MOVE_PARSER)

SOURCE_FILES = (
    Path("Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs"),
    Path("Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs"),
    Path("Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs"),
    Path("Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs"),
    Path("Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs"),
    Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"),
    Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.BakerVisuals.cs"),
    Path("Assets/Game/Scripts/Components/BuildingProducerCoreProjection.cs"),
    Path("Assets/Game/Scripts/Components/UnitMoveAuthoringProjection.cs"),
    Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.GroundContact.cs"),
    Path("Assets/Game/Scripts/Components/UnitSurfaceAuthoringProjection.cs"),
    Path("Assets/Game/Scripts/Components/MapSurfaceComponents.cs"),
    Path("Assets/Game/Scripts/Components/UnitVisualComponents.cs"),
    Path("Assets/Game/Scripts/Components/GridComponents.cs"),
    Path("Assets/Game/Scripts/Components/UnitCombatComponents.cs"),
    Path("Assets/Game/Scripts/Components/FactionAIComponents.cs"),
    Path("Assets/Game/Scripts/Systems/SharedProductionActorInitializer.cs"),
    OPERATION_MAP_IDENTITY_AUTHORING,
    OPERATION_MAP_IDENTITY_AUTHORING.with_suffix(OPERATION_MAP_IDENTITY_AUTHORING.suffix + ".meta"),
    PARSER,
    GENERATOR,
)


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def sha_bytes(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def safe_file(root: Path, rel: Path) -> Path:
    root = root.resolve(strict=True)
    candidate = root / rel
    try:
        relative = candidate.relative_to(root)
    except ValueError as error:
        raise ValueError("source path escaped repository: " + str(rel)) from error
    cursor = root
    for part in relative.parts:
        cursor = cursor / part
        need(not cursor.is_symlink(), "symlink/reparse source path component: " + str(cursor))
    need(candidate.is_file(), "required regular source file missing: " + str(rel))
    return candidate


def read_text(root: Path, rel: Path) -> tuple[str, bytes]:
    raw = safe_file(root, rel).read_bytes()
    return raw.decode("utf-8", errors="strict"), raw


def one(pattern: str, text: str, label: str) -> re.Match[str]:
    matches = list(re.finditer(pattern, text, re.MULTILINE))
    need(len(matches) == 1, f"expected exactly one {label}; found {len(matches)}")
    return matches[0]


def scalar(text: str, name: str, kind: str = "int"):
    if kind == "float":
        return _UNIT_MOVE_PARSER.one_float(text, name)
    if kind == "bool":
        return _UNIT_MOVE_PARSER.one_bool(text, name)
    patterns = {
        "int": rf"^\s*{re.escape(name)}:\s*(-?\d+)\s*$",
        "float": rf"^\s*{re.escape(name)}:\s*(-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)\s*$",
        "bool": rf"^\s*{re.escape(name)}:\s*([01])\s*$",
    }
    m = one(patterns[kind], text, "authored " + name)
    value = m.group(1)
    return float(value) if kind == "float" else (value == "1" if kind == "bool" else int(value))


def inline_pair(text: str, name: str) -> tuple[int, int]:
    m = one(rf"^\s*{re.escape(name)}:\s*\{{([^}}]+)\}}\s*$", text, "authored " + name)
    body = m.group(1)
    return (_UNIT_MOVE_PARSER.one_int(text, name, "x"), _UNIT_MOVE_PARSER.one_int(text, name, "y"))


def meta_guid(text: str, label: str) -> str:
    return one(r"^guid: ([0-9a-f]{32})$", text, label + " meta GUID").group(1)


def _guid_domain(root: Path) -> tuple[list[tuple[str, str, str]], str]:
    root = root.resolve(strict=True)
    assets = root / "Assets"
    need(assets.is_dir() and not assets.is_symlink(), "regular Assets lookup root required")
    rows = []
    for current, dirs, files in os.walk(assets, topdown=True, followlinks=False):
        current_path = Path(current)
        for dirname in list(dirs):
            child = current_path / dirname
            need(not child.is_symlink(), "symlinked ancestor in GUID lookup domain: " + str(child))
        for filename in files:
            if not filename.endswith(".meta"):
                continue
            meta = current_path / filename
            need(not meta.is_symlink(), "symlinked .meta in GUID lookup domain: " + str(meta))
            raw = meta.read_bytes()
            text = raw.decode("utf-8", errors="strict")
            matches = re.findall(r"^guid: ([0-9a-f]{32})$", text, re.MULTILINE)
            if len(matches) > 1:
                raise ValueError("ambiguous multiple GUID declarations in meta: " + str(meta.relative_to(root)))
            if matches:
                rows.append((str(meta.relative_to(root)), matches[0], sha_bytes(raw)))
    rows.sort()
    digest = sha_bytes(json.dumps(rows, separators=(",", ":")).encode("utf-8"))
    return rows, digest


def resolve_guid(root: Path, guid: str) -> tuple[Path, dict]:
    rows, domain_sha = _guid_domain(root)
    matches = [row for row in rows if row[1] == guid]
    need(len(matches) == 1, f"GUID must resolve to exactly one asset meta; found {len(matches)} for {guid}")
    rel_meta, _, meta_sha = matches[0]
    meta_path = Path(rel_meta)
    asset_path = meta_path.with_suffix("")
    need(safe_file(root, asset_path).is_file(), "GUID meta has no matching asset: " + str(asset_path))
    return asset_path, {"guid": guid, "asset_path": str(asset_path), "meta_path": rel_meta,
                        "meta_sha256": meta_sha, "lookup_domain_count": len(rows),
                        "lookup_domain_sha256": domain_sha}


def parse_recipe(text: str, root: Path, lookups: list[dict]) -> dict:
    block = one(r"^  productions:\s*\n((?:^  - .*\n|^    .*\n)+)", text, "Barrack productions block").group(1)
    rows = list(re.finditer(r"^  - spawnUnitPrefab: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: 3\}\s*\n    quantity: (\d+)\s*$", block, re.MULTILINE))
    need(len(rows) == 1 and len(re.findall(r"^  - ", block, re.MULTILINE)) == 1,
         "Barrack must contain exactly one unambiguous authored production row")
    ref_id, guid, quantity = rows[0].groups()
    asset_path, lookup = resolve_guid(root, guid)
    lookups.append(lookup)
    expected = RIFLE_PREFAB
    need(asset_path == expected, "Barrack recipe GUID resolves to an unexpected prefab: " + str(asset_path))
    return {"index": 0, "unit_key": RIFLE_KEY, "prefab": {"path": str(asset_path), "guid": guid,
            "local_id": int(ref_id), "local_id_basis": "authored Barrack recipe reference"}, "quantity": int(quantity)}


def parse_generic_barrack_identity(root: Path, contents: dict[str, str], pins: dict[str, str],
                                   lookups: list[dict]) -> dict:
    """Prove the exact source-authored Barrack reference pair in generic configs only."""
    barrack_meta_rel = BARRACK_PREFAB.with_suffix(BARRACK_PREFAB.suffix + ".meta")
    barrack_guid = meta_guid(contents[barrack_meta_rel], "Barrack prefab")
    local_id = 6603657636351658259
    pair = re.compile(r"\{fileID:\s*(\d+),\s*guid:\s*([0-9a-f]{32}),\s*type:\s*3\}")

    config_facts = []
    for rel, expected_count in ((GENERIC_CONSTRUCTION, 1), (GENERIC_INITIAL_FORCES, 2)):
        text = contents[rel]
        refs = [m for m in pair.finditer(text)
                if int(m.group(1)) == local_id and m.group(2) == barrack_guid]
        need(len(refs) == expected_count,
             f"generic {rel.name} must contain exactly {expected_count} exact Barrack GUID/localID refs; found {len(refs)}")
        meta_rel = rel.with_suffix(rel.suffix + ".meta")
        config_guid = meta_guid(contents[meta_rel], rel.name)
        resolved, evidence = resolve_guid(root, config_guid)
        need(resolved == rel, "generic config GUID resolves to unexpected asset: " + str(resolved))
        lookups.append(evidence)
        config_facts.append({"path": str(rel), "guid": config_guid,
                             "reference_pair": {"guid": barrack_guid, "local_id": local_id},
                             "exact_reference_count": expected_count,
                             "meta_sha256": pins[str(meta_rel)]})

    initial_guid = config_facts[1]["guid"]
    initial_ref = one(r"^\s*initialUnitsConfig: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}$",
                      contents[GENERIC_CONSTRUCTION], "generic Construction initialUnitsConfig")
    need(initial_ref.group(1) == initial_guid, "generic Construction does not point to pinned InitialForces")
    construction_guid = config_facts[0]["guid"]
    preset_ref = one(r"^\s*buildingPlacement: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}$",
                     contents[GENERIC_PRESET], "generic SkirmishBaseAssault buildingPlacement")
    need(preset_ref.group(1) == construction_guid,
         "SkirmishBaseAssault does not point to pinned generic Construction")

    expansion_checks = []
    for rel in EXPANSION_SCENARIOS:
        text = contents[rel]
        need(construction_guid not in text and initial_guid not in text,
             "S002/S003 expansion scenario entry directly references generic Skirmish config")
        refs = [m for m in pair.finditer(text)
                if int(m.group(1)) == local_id and m.group(2) == barrack_guid]
        need(not refs, "S002/S003 expansion scenario entry directly contains generic Barrack reference")
        expansion_checks.append({"path": str(rel), "sha256": pins[str(rel)],
                                 "direct_generic_config_refs": 0, "direct_barrack_pair_refs": 0})

    return {
        "kind": "GenericSkirmishAuthoredBarrackReferenceWitness",
        "scope": "generic SkirmishBaseAssault template only",
        "barrack_pair": {"guid": barrack_guid, "local_id": local_id,
                         "basis": "exact authored YAML reference pair; no Unity importer resolution claimed"},
        "configs": config_facts,
        "generic_chain": {"construction_initial_forces_reference": "verified",
                          "base_assault_construction_reference": "verified"},
        "expansion_membership": {"S002": "not established; direct entry references absent",
                                  "S003": "not established; direct entry references absent",
                                  "checked_assets": expansion_checks},
        "unity_resolution": False,
    }


def build_descriptor(root: Path) -> dict:
    root = root.resolve(strict=True)
    generic_paths = [GENERIC_CONSTRUCTION, GENERIC_CONSTRUCTION.with_suffix(GENERIC_CONSTRUCTION.suffix + ".meta"),
                     GENERIC_INITIAL_FORCES, GENERIC_INITIAL_FORCES.with_suffix(GENERIC_INITIAL_FORCES.suffix + ".meta"),
                     GENERIC_PRESET, *EXPANSION_SCENARIOS]
    paths = [BARRACK_CONFIG, BARRACK_CONFIG.with_suffix(BARRACK_CONFIG.suffix + ".meta"),
             BARRACK_PREFAB, BARRACK_PREFAB.with_suffix(BARRACK_PREFAB.suffix + ".meta"),
             RIFLE_CONFIG, RIFLE_CONFIG.with_suffix(RIFLE_CONFIG.suffix + ".meta"),
             RIFLE_PREFAB, RIFLE_PREFAB.with_suffix(RIFLE_PREFAB.suffix + ".meta"),
             UNIT_SOURCE_PREFAB, UNIT_SOURCE_PREFAB.with_suffix(UNIT_SOURCE_PREFAB.suffix + ".meta"),
             TRANSPORT_BASE_PREFAB, TRANSPORT_BASE_PREFAB.with_suffix(TRANSPORT_BASE_PREFAB.suffix + ".meta"),
             RIFLE_MODEL_PREFAB, RIFLE_MODEL_PREFAB.with_suffix(RIFLE_MODEL_PREFAB.suffix + ".meta"),
             *generic_paths, *SOURCE_FILES]
    contents = {}
    pins = {}
    for rel in paths:
        raw = safe_file(root, rel).read_bytes()
        raw.decode("utf-8", errors="strict")
        contents[rel] = raw.decode("utf-8")
        digest = sha_bytes(raw)
        if rel == PARSER:
            need(digest == PARSER_SHA256, "repository UnitMove parser source pin drift")
        pins[str(rel)] = digest
    all_root_sources = re.findall(
        r"(?m)^  m_SourcePrefab: \{fileID: 100100000, guid: ([0-9a-f]{32}), type: 3\}$",
        contents[RIFLE_PREFAB])
    need(len(all_root_sources) == 2, "Rifle prefab must have exactly the authoring root and model source instances")
    root_source_path, root_source_lookup = resolve_guid(root, all_root_sources[0])
    need(root_source_path == UNIT_SOURCE_PREFAB,
         "Rifle root source prefab GUID resolves to an unexpected asset: " + str(root_source_path))
    model_source_path, model_source_lookup = resolve_guid(root, all_root_sources[1])
    need(model_source_path == RIFLE_MODEL_PREFAB,
         "Rifle model GUID resolves to an unexpected asset: " + str(model_source_path))
    barrack_cfg, barrack_meta = contents[BARRACK_CONFIG], contents[BARRACK_CONFIG.with_suffix(BARRACK_CONFIG.suffix + ".meta")]
    barrack_prefab, barrack_prefab_meta = contents[BARRACK_PREFAB], contents[BARRACK_PREFAB.with_suffix(BARRACK_PREFAB.suffix + ".meta")]
    rifle_cfg, rifle_meta = contents[RIFLE_CONFIG], contents[RIFLE_CONFIG.with_suffix(RIFLE_CONFIG.suffix + ".meta")]
    _, rifle_prefab_meta = contents[RIFLE_PREFAB], contents[RIFLE_PREFAB.with_suffix(RIFLE_PREFAB.suffix + ".meta")]
    need(one(r"^  m_Name: Prefab_BuildingDefinition_Building_Barrack_Config$", barrack_cfg, "Barrack config identity"), "wrong Barrack config")
    need(one(r"^  m_Name: Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config$", rifle_cfg, "rifle config identity"), "wrong rifle config")
    barrack_guid = meta_guid(barrack_prefab_meta, "Barrack prefab")
    barrack_config_guid = meta_guid(barrack_meta, "Barrack config")
    rifle_prefab_guid = meta_guid(rifle_prefab_meta, "rifle prefab")
    rifle_config_guid = meta_guid(rifle_meta, "rifle config")
    need(barrack_prefab.count("guid: " + barrack_config_guid) == 1, "Barrack prefab must reference its pinned config GUID exactly once")
    need(contents[RIFLE_PREFAB].count("guid: " + rifle_config_guid) == 1, "rifle prefab must reference its pinned config GUID exactly once")
    guid_lookups = []
    guid_lookups.append(root_source_lookup)
    guid_lookups.append(model_source_lookup)
    for guid_value, expected_path, label in (
            (barrack_guid, BARRACK_PREFAB, "Barrack prefab"),
            (barrack_config_guid, BARRACK_CONFIG, "Barrack config"),
            (rifle_prefab_guid, RIFLE_PREFAB, "rifle prefab"),
            (rifle_config_guid, RIFLE_CONFIG, "rifle config")):
        resolved, evidence = resolve_guid(root, guid_value)
        need(resolved == expected_path, label + " GUID resolves to unexpected asset: " + str(resolved))
        guid_lookups.append(evidence)
    recipe = parse_recipe(barrack_cfg, root, guid_lookups)
    need(meta_guid(contents[RIFLE_PREFAB.with_suffix(RIFLE_PREFAB.suffix + ".meta")], "rifle prefab") ==
         recipe["prefab"]["guid"], "Barrack recipe GUID does not match rifle prefab meta")
    generic_identity = parse_generic_barrack_identity(root, contents, pins, guid_lookups)

    def override(axis: str) -> int:
        m = one(r"- target: \{fileID: [^\n]+\}\s*\n\s+propertyPath: footprintCells\." + axis +
                r"\s*\n\s+value: (-?\d+)\s*$", barrack_prefab, "Barrack authored footprint " + axis)
        return int(m.group(1))

    footprint = [override("x"), override("y")]
    need(all(v > 0 for v in footprint), "Barrack authored footprint must be positive")
    unit_fp = inline_pair(rifle_cfg, "footprintCells")
    transport = one(r"^\s*productionTransportPrefab: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: 3\}\s*$", rifle_cfg,
                    "rifle production transport reference")
    transport_path, transport_lookup = resolve_guid(root, transport.group(2))
    need(transport_path == TRANSPORT_PREFAB, "Rifle transport GUID resolves to unexpected prefab: " + str(transport_path))
    guid_lookups.append(transport_lookup)
    transport_meta_path = Path(str(transport_path) + ".meta")
    transport_config_text_path = TRANSPORT_CONFIG
    transport_config_meta_path = TRANSPORT_CONFIG.with_suffix(TRANSPORT_CONFIG.suffix + ".meta")
    for rel in (transport_path, transport_meta_path):
        transport_bytes = safe_file(root, rel).read_bytes()
        transport_bytes.decode("utf-8", errors="strict")
        pins[str(rel)] = sha_bytes(transport_bytes)
    transport_prefab_text = contents.get(transport_path)
    if transport_prefab_text is None:
        transport_prefab_raw = safe_file(root, transport_path).read_bytes()
        transport_prefab_text = transport_prefab_raw.decode("utf-8", errors="strict")
        contents[transport_path] = transport_prefab_text
    # The authored transport is a prefab variant. Pin its immediate base prefab
    # and verify the root-name override through the Unit_Veh -> Unit variant chain.
    transport_base_meta_path = TRANSPORT_BASE_PREFAB.with_suffix(TRANSPORT_BASE_PREFAB.suffix + ".meta")
    transport_base_guid = meta_guid(contents[transport_base_meta_path], "transport base prefab")
    transport_base_text = contents[TRANSPORT_BASE_PREFAB]
    variant_source = one(r"^  m_SourcePrefab: \{fileID: 100100000, guid: ([0-9a-f]{32}), type: 3\}$",
                         transport_prefab_text, "transport variant source prefab")
    need(variant_source.group(1) == transport_base_guid, "transport variant does not reference pinned Unit_Veh base")
    variant_name = one(r"(?m)^    - target: \{fileID: 1972179586304415830, guid: " + re.escape(transport_base_guid) +
                       r", type: 3\}\s*\n      propertyPath: m_Name\s*\n      value: Unit_Veh_Helicopter_Transport\s*\n      objectReference: \{fileID: 0\}$",
                       transport_prefab_text, "transport variant root-name override")
    base_source = one(r"^  m_SourcePrefab: \{fileID: 100100000, guid: ([0-9a-f]{32}), type: 3\}$",
                      transport_base_text, "transport base source prefab")
    unit_source_guid = meta_guid(contents[UNIT_SOURCE_PREFAB.with_suffix(UNIT_SOURCE_PREFAB.suffix + ".meta")], "Unit source prefab")
    need(base_source.group(1) == unit_source_guid, "Unit_Veh base does not reference pinned Unit source")
    base_name = one(r"(?m)^    - target: \{fileID: 2254344950327853479, guid: " + re.escape(unit_source_guid) +
                    r", type: 3\}\s*\n      propertyPath: m_Name\s*\n      value: Unit_Veh\s*\n      objectReference: \{fileID: 0\}$",
                    transport_base_text, "Unit_Veh root-name override")
    unit_root = one(r"(?ms)^--- !u!1 &2254344950327853479\nGameObject:\n(?:(?!^--- !u!).)*?^  m_Name: Unit$",
                    contents[UNIT_SOURCE_PREFAB], "pinned Unit root GameObject")
    need(unit_root is not None and re.search(r"(?ms)^--- !u!4 &23759086217692639\nTransform:\n(?:(?!^--- !u!).)*?^  m_GameObject: \{fileID: 2254344950327853479\}\n(?:(?!^--- !u!).)*?^  m_Father: \{fileID: 0\}$", contents[UNIT_SOURCE_PREFAB]) is not None,
         "Unit_Veh name override target is not the pinned Unit root GameObject")
    variant_lineage = {"base_path": str(TRANSPORT_BASE_PREFAB), "base_guid": transport_base_guid,
                       "base_root_name": "Unit_Veh", "base_root_gameobject_source_file_id": 2254344950327853479,
                       "source_path": str(UNIT_SOURCE_PREFAB), "source_guid": unit_source_guid,
                       "source_root_name": "Unit", "source_root_gameobject_file_id": 2254344950327853479,
                       "variant_root_name": "Unit_Veh_Helicopter_Transport",
                       "variant_root_gameobject_source_file_id": 1972179586304415830}
    transport_config_meta_text = safe_file(root, transport_config_meta_path).read_bytes().decode("utf-8", errors="strict")
    transport_config_guid = meta_guid(transport_config_meta_text, "transport config")
    transport_config_path, transport_config_lookup = resolve_guid(root, transport_config_guid)
    need(transport_config_path == transport_config_text_path,
         "Transport config GUID resolves to unexpected asset: " + str(transport_config_path))
    guid_lookups.append(transport_config_lookup)
    for rel in (transport_config_text_path, transport_config_meta_path):
        transport_bytes = safe_file(root, rel).read_bytes()
        transport_bytes.decode("utf-8", errors="strict")
        pins[str(rel)] = sha_bytes(transport_bytes)
    transport_config_text = contents.get(transport_config_text_path)
    if transport_config_text is None:
        transport_config_text = safe_file(root, transport_config_text_path).read_bytes().decode("utf-8", errors="strict")
        contents[transport_config_text_path] = transport_config_text
    need(one(r"^  m_Name: Prefab_UnitGrid_Veh_Helicopter_Transport_Config$", transport_config_text,
             "transport config identity"), "wrong transport config")
    transport_config_ref = one(
        r"^\s*propertyPath: config\s*\n\s+value:\s*\n\s+objectReference: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}$",
        transport_prefab_text, "transport prefab config override")
    need(transport_config_ref.group(1) == transport_config_guid,
         "Transport prefab config override does not reference its pinned transport config")
    need(len(re.findall(r"^\s*- target: \{fileID: [^\n]+\}\s*$\n\s+propertyPath: config\s*$", transport_prefab_text, re.MULTILINE)) == 1,
         "Transport prefab must contain exactly one config override")
    transport_facts = {
        "key": "Unit_Veh_Helicopter_Transport",
        "prefab": {"path": str(transport_path), "guid": transport.group(2),
                   "local_id": int(transport.group(1)), "local_id_basis": "authored rifle unit config reference"},
        "config": {"path": str(transport_config_text_path), "guid": transport_config_guid,
                   "config_reference_basis": "transport prefab config override"},
        "production_duration_seconds": scalar(transport_config_text, "productionDurationSeconds", "float"),
        "is_air_unit": scalar(transport_config_text, "isAirUnit", "bool"),
        "production_transport_prefab": None,
        "production_transport_arrival_seconds": scalar(transport_config_text, "productionTransportArrivalSeconds", "float"),
        "production_transport_hold_for_next_ready_seconds": scalar(transport_config_text, "productionTransportHoldForNextReadySeconds", "float"),
        "production_transport_max_concurrent": scalar(transport_config_text, "productionTransportMaxConcurrent"),
        "production_transport_requires_airport_runway": scalar(transport_config_text, "productionTransportRequiresAirportRunway", "bool"),
        "production_transport_uses_runway_landing": scalar(transport_config_text, "productionTransportUsesRunwayLanding", "bool"),
        "footprint_cells": list(inline_pair(transport_config_text, "footprintCells")),
        "auto_calculate_footprint": scalar(transport_config_text, "autoCalculateFootprint", "bool"),
    }
    null_transport = one(r"^\s*productionTransportPrefab: \{fileID: 0\}$", transport_config_text,
                         "transport unit null production transport")
    need(transport_facts["production_duration_seconds"] > 0 and
         transport_facts["production_transport_max_concurrent"] == 2 and
         transport_facts["production_transport_arrival_seconds"] > 0 and
         transport_facts["production_transport_hold_for_next_ready_seconds"] > 0 and
         transport_facts["is_air_unit"] is True and transport_facts["footprint_cells"] == [1, 1] and
         transport_facts["auto_calculate_footprint"] is False and
         transport_facts["production_transport_requires_airport_runway"] is False and
         transport_facts["production_transport_uses_runway_landing"] is False,
         "Transport authored production metadata outside admitted exact values")
    source_static = {
        "producer": {
            "key": "Building_Barrack",
            "prefab": {"path": str(BARRACK_PREFAB), "guid": barrack_guid,
                       "local_id": None, "local_id_status": "not encoded in the checked-in prefab text; no local ID inferred"},
            "config": {"path": str(BARRACK_CONFIG), "guid": barrack_config_guid},
            "footprint_cells": footprint,
            "max_health": scalar(barrack_cfg, "maxHealth"),
            "credits_cost": scalar(barrack_cfg, "price"),
            "materials_cost": scalar(barrack_cfg, "materialsCost"),
            "construction_duration_seconds": scalar(barrack_cfg, "productionDurationSeconds", "float"),
            "recipes": [recipe],
        },
        "unit": {
            "key": RIFLE_KEY,
            "prefab": {"path": str(RIFLE_PREFAB), "guid": rifle_prefab_guid,
                       "local_id": recipe["prefab"]["local_id"], "local_id_basis": "Barrack authored asset reference"},
            "config": {"path": str(RIFLE_CONFIG), "guid": rifle_config_guid},
            "credits_cost": scalar(rifle_cfg, "price"),
            "materials_cost": scalar(rifle_cfg, "materialsCost"),
            "production_duration_seconds": scalar(rifle_cfg, "productionDurationSeconds", "float"),
            "is_air_unit": scalar(rifle_cfg, "isAirUnit", "bool"),
            "footprint_cells": list(unit_fp),
            "transport": {"path": str(transport_path), "local_id": int(transport.group(1)), "guid": transport.group(2),
                          "local_id_basis": "authored rifle unit config reference",
                          "config_path": str(transport_config_text_path), "config_guid": transport_config_guid,
                          "config_reference_basis": "transport prefab config override"},
            "transport_arrival_seconds": scalar(rifle_cfg, "productionTransportArrivalSeconds", "float"),
            "transport_hold_for_next_ready_seconds": scalar(rifle_cfg, "productionTransportHoldForNextReadySeconds", "float"),
            "transport_max_concurrent": scalar(rifle_cfg, "productionTransportMaxConcurrent"),
            "transport_requires_airport_runway": scalar(rifle_cfg, "productionTransportRequiresAirportRunway", "bool"),
            "transport_uses_runway_landing": scalar(rifle_cfg, "productionTransportUsesRunwayLanding", "bool"),
            "movement_authored": {
                "speed": scalar(rifle_cfg, "speed", "float"),
                "walk_speed": scalar(rifle_cfg, "walkSpeed", "float"),
                "road_speed_multiplier": scalar(rifle_cfg, "roadSpeedMultiplier", "float"),
                "arrive_distance": scalar(rifle_cfg, "arriveDistance", "float"),
                "allow_idle_wander": scalar(rifle_cfg, "allowIdleWander", "bool"),
                "auto_calculate_footprint": scalar(rifle_cfg, "autoCalculateFootprint", "bool"),
                "uses_vehicle_motion": scalar(rifle_cfg, "usesVehicleMotion", "bool"),
            },
        },
        "transport_unit": {**transport_facts, "prefab_variant_lineage": variant_lineage},
    }
    need(source_static["producer"]["max_health"] > 0 and source_static["unit"]["production_duration_seconds"] > 0,
         "authored health and unit production duration must be positive")
    return {
        "schema_version": 1,
        "kind": "BarrackRifleCurrentAuthoredSourceFacts",
        "status": "PassedSourceOnlyDescriptor",
        "authority": "Current checked-in Unity text assets and pinned source files only",
        "source_pins": pins,
        "guid_lookup_facts": guid_lookups,
        "facts": source_static,
        "generic_authored_identity_witness": generic_identity,
        "scope": {
            "source_assets_parsed": True,
            "native_factory_invoked": False,
            "mission_overlay_compiled": False,
            "mission_snapshot_used": False,
            "runtime_admission": False,
            "native_context_qualified": False,
            "mission_ready": False,
            "producer_local_id": "unknown_from_text_source",
            "generic_config_identity_only": True,
            "generic_witness_establishes_s002_s003_membership": False,
            "unity_asset_resolution": False,
        },
    }


def write_descriptor(root: Path, output: Path) -> dict:
    root = root.resolve(strict=True)
    output = Path(output).absolute()
    need(not output.exists(), "fresh output path required")
    payload = build_descriptor(root)
    output.parent.mkdir(parents=True, exist_ok=True)
    data = (json.dumps(payload, indent=2) + "\n").encode("utf-8")
    staged = None
    created_output = False
    try:
        with tempfile.NamedTemporaryFile(prefix=output.name + ".stage-", dir=output.parent, delete=False) as stream:
            staged = Path(stream.name)
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        # Recheck every parsed source and the complete GUID lookup epoch before
        # making the final path visible.
        for rel, digest in payload["source_pins"].items():
            need(sha_bytes(safe_file(root, Path(rel)).read_bytes()) == digest,
                 "source changed during descriptor generation: " + rel)
        for lookup in payload["guid_lookup_facts"]:
            _, current = resolve_guid(root, lookup["guid"])
            need(current == lookup, "GUID lookup epoch changed during descriptor generation: " + lookup["guid"])
        need(not output.exists(), "final output appeared before exclusive publication")
        os.link(staged, output)
        created_output = True
        staged.unlink()
        staged = None
        return payload
    except Exception:
        if created_output:
            try:
                output.unlink()
            except FileNotFoundError:
                pass
        raise
    finally:
        if staged is not None:
            try:
                staged.unlink()
            except FileNotFoundError:
                pass


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        payload = write_descriptor(args.repo, args.output)
        print(json.dumps({"status": payload["status"], "source_pins": len(payload["source_pins"]),
                          "recipes": len(payload["facts"]["producer"]["recipes"]),
                          "native_context_qualified": False, "mission_ready": False}))
        return 0
    except (OSError, UnicodeError, ValueError, KeyError, TypeError) as error:
        print("Barrack/rifle source descriptor failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
