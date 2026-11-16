#!/usr/bin/env python3
"""Fail-closed offline qualification of the S004 configured-spawnable readmodel export."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path
from typing import Any

CAPTURE_DIR = "Logs/SkirmishProduction/s004-configured-building-readmodel-capture-20261103-r3"
RAW_REL = CAPTURE_DIR + "/data/configured-spawnable-readmodel.json"
LOG_REL = CAPTURE_DIR + "/editor.log"
SETUP_REL = "Logs/SkirmishProduction/s004-standard-setup-export-20261103-r1/compiled-setup.json"
PASS_MARKER = "[SkirmishS004ConfiguredSpawnableReadModel] result=Passed keys=9 scene=0 missionBuildings=0 readiness=0"
METHOD = "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS004ConfiguredSpawnableReadModelReferenceData"
PUBLISHER = "BuildingRuntimeProcessingCompositionSystemHelper.PublishConfiguredSpawnablesReadModel"
FACTORY = "BuildingDefinitionPrefabSystemHelper.RebuildConfiguredSpawnableDefinitions"
FIELDS = ("BuildingId", "DisplayName", "Price", "MaterialsCost", "FootprintCells", "CanRequest")
SCHEMA = "s004-configured-spawnable-readmodel-receipt-v1"
STRUCTURE_VISUAL_KEYS = {
    "building.barracks": "Building_Barrack",
    "building.ground_staging": "Building_GroundStaging",
    "building.oil_pump": "Building_OilPump",
    "building.refinery": "Building_Refinery",
    "building.fuel_bladder": "Building_Fuel_Bladder",
    "building.fabrication_depot": "Building_Ammunition_Depot",
    "building.watchtower": "Building_GuardTower",
    "building.watchtower.approach": "Building_GuardTower",
    "building.satellite_dish": "Building_Satelite_Dish",
    "building.helipad": "Building_Helipad",
}


class FinalizeError(ValueError):
    pass


def need(condition: bool, message: str) -> None:
    if not condition:
        raise FinalizeError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_json(path: Path, label: str) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exc:
        raise FinalizeError(label + " is unreadable or invalid JSON") from exc


def project_path(root: Path, raw_path: str) -> Path:
    candidate = Path(raw_path)
    path = candidate.resolve() if candidate.is_absolute() else (root / candidate).resolve()
    try:
        path.relative_to(root.resolve())
    except ValueError as exc:
        raise FinalizeError("source pin escapes project root: " + raw_path) from exc
    return path


def load_s004_mapping_module():
    path = Path(__file__).with_name("finalize_s004_full_building_factory_capture.py")
    spec = importlib.util.spec_from_file_location("s004_original_mapping", path)
    if spec is None or spec.loader is None:
        raise FinalizeError("original S004 VisualKey mapping verifier unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def validate_live_pins(raw: dict, root: Path) -> dict[str, str]:
    pins = raw.get("source_pins")
    need(isinstance(pins, dict) and bool(pins), "source pin set missing")
    required = {
        "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs",
        "Assets/Game/Scripts/Systems/BuildingRuntimeBoundaryCommands.cs",
        "Assets/Game/Scripts/Systems/SpawnableKeyText.cs",
        "Assets/Game/Scripts/Systems/SpawnableKeyText.cs",
        "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs",
        "Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs",
        "Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs",
        "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S004ConfiguredSpawnableReadModel.cs",
    }
    need(required.issubset(pins), "required original publisher/compiler source pins missing")
    for rel, digest in pins.items():
        need(isinstance(rel, str) and isinstance(digest, str) and re.fullmatch(r"[0-9a-f]{64}", digest) is not None,
             "malformed source pin")
        path = project_path(root, rel)
        need(path.is_file(), "pinned source missing: " + rel)
        need(sha(path) == digest, "pinned source differs from current file: " + rel)
    return pins


def validate_setup(raw: dict, evidence_path: Path, root: Path) -> tuple[dict, list[dict], dict[str, str]]:
    evidence_ref = raw.get("setup_evidence")
    need(isinstance(evidence_ref, dict), "compiled setup evidence reference missing")
    recorded = project_path(root, str(evidence_ref.get("path", "")))
    expected = project_path(root, str(evidence_path))
    need(recorded == expected and recorded.is_file(), "capture references an unexpected/missing S004 setup evidence file")
    evidence_hash = sha(recorded)
    need(evidence_ref.get("sha256") == evidence_hash, "compiled setup evidence hash mismatch")
    evidence = load_json(recorded, "compiled S004 setup evidence")
    need(evidence.get("status") == "Passed" and evidence.get("kind") == "S004StandardCompiledSetupSourceExport",
         "compiled S004 setup evidence status/kind mismatch")
    identity, compiled, source_bound = evidence.get("identity"), evidence.get("compiled_setup"), evidence.get("source_bound_compiler_result")
    need(isinstance(identity, dict) and isinstance(compiled, dict) and isinstance(source_bound, dict),
         "compiled setup identity blocks missing")
    exact_identity = {"catalog_id": "S004", "definition_id": "skirmish.s004",
                      "scenario_setup_id": "scenario.skirmish.s004", "content_version": 2,
                      "difficulty_id": "Regular", "size_id": "Standard", "seed": 104733,
                      "measured_layout_bound": True}
    for key, value in exact_identity.items():
        need(identity.get(key) == value, "compiled S004 setup identity mismatch: " + key)
    setup_hash = identity.get("setup_hash")
    need(type(setup_hash) is int and compiled.get("SetupHash") == setup_hash and
         source_bound.get("status") == "Compiled" and source_bound.get("compiled") is True and
         source_bound.get("catalog_id") == "S004" and source_bound.get("definition_id") == "skirmish.s004",
         "compiled setup source-bound hash/result mismatch")
    need(raw.get("catalog_id") == "S004" and raw.get("seed") == 104733 and raw.get("setup_hash") == setup_hash and
         evidence_ref.get("setup_hash") == setup_hash,
         "readmodel capture setup identity differs from compiled evidence")
    rows = compiled.get("Structures")
    need(isinstance(rows, list) and len(rows) == 20, "compiled S004 structure rows missing or count mismatch")
    mapping = STRUCTURE_VISUAL_KEYS
    observed_ids = set()
    mapped_rows = []
    for row in rows:
        need(isinstance(row, dict) and type(row.get("FactionId")) is int and row.get("FactionId") in (1, 2) and
             isinstance(row.get("StructureId"), str), "malformed compiled structure entry")
        structure_id = row["StructureId"]
        need(structure_id in mapping, "compiled structure has no original VisualKey mapping: " + structure_id)
        observed_ids.add(structure_id)
        mapped_rows.append({"factionId": row["FactionId"], "structureId": structure_id, "visualKey": mapping[structure_id]})
    need(observed_ids == set(mapping), "compiled S004 unique structure ID set differs from original mapping")
    return evidence, mapped_rows, {"setupHash": str(setup_hash), "evidenceSha256": evidence_hash}


def read_codec_string(value: Any, label: str) -> str:
    need(isinstance(value, dict) and value.get("codec") == "utf16-v1" and isinstance(value.get("value"), str),
         label + " source codec mismatch")
    return value["value"]


def read_codec_int(value: Any, label: str) -> int:
    need(isinstance(value, dict) and value.get("codec") == "integer-v1" and value.get("type") == "System.Int32",
         label + " source codec mismatch")
    try:
        number = int(value["value"])
    except (KeyError, TypeError, ValueError) as exc:
        raise FinalizeError(label + " source integer malformed") from exc
    need(str(number) == value["value"], label + " source integer is not canonical")
    return number


def normalize_key(value: str) -> str:
    # Exact pinned SpawnableKeyText.Normalize behavior for ASCII S004 prefab keys.
    return value.strip().lower()


def verify_key_normalizer(root: Path, pins: dict[str, str]) -> None:
    rel = "Assets/Game/Scripts/Systems/SpawnableKeyText.cs"
    path = project_path(root, rel)
    source = path.read_text(encoding="utf-8")
    need(pins.get(rel) == sha(path), "original SpawnableKeyText source is not pinned")
    need("return value.Trim().ToLowerInvariant();" in source and
         "if (string.IsNullOrWhiteSpace(value))" in source,
         "live original SpawnableKeyText.Normalize semantics differ from the qualified key transform")


def validate_projection(source: dict, model: dict, key: str) -> None:
    entry_display = source.get("display_name")
    need(isinstance(entry_display, str) and isinstance(source.get("can_request"), bool),
         "configured entry display/request source facts malformed: " + key)
    inputs = source.get("definition_projection_inputs")
    need(isinstance(inputs, dict) and set(inputs) == {"Prefab", "DisplayName", "CreditsCost", "MaterialsCost", "FootprintCells"},
         "original definition projection inputs incomplete: " + key)
    prefab = source["prefab"]
    need(isinstance(inputs["Prefab"], dict) and inputs["Prefab"].get("codec") == "source-asset-reference-v1" and
         inputs["Prefab"].get("path") == prefab["path"] and inputs["Prefab"].get("guid") == prefab["guid"] and
         inputs["Prefab"].get("local_id") == prefab.get("local_id"),
         "definition Prefab input identity differs from configured overlay: " + key)
    definition_display = read_codec_string(inputs["DisplayName"], key + "/DisplayName")
    credits = read_codec_int(inputs["CreditsCost"], key + "/CreditsCost")
    materials = read_codec_int(inputs["MaterialsCost"], key + "/MaterialsCost")
    footprint = inputs["FootprintCells"]
    need(isinstance(footprint, dict) and footprint.get("codec") == "struct-fields-v1" and
         footprint.get("type") == "UnityEngine.Vector2Int" and isinstance(footprint.get("fields"), dict),
         "definition FootprintCells source codec mismatch: " + key)
    xy = []
    for axis in ("m_X", "m_Y"):
        xy.append(read_codec_int(footprint["fields"].get("UnityEngine.Vector2Int::" + axis), key + "/FootprintCells"))
    expected_rules = {
        "BuildingId": "Original ResolveBoundaryId(prefab, entry.DisplayName): normalized prefab name with entry display-name fallback",
        "DisplayName": "ConfiguredSpawnableEntry.DisplayName",
        "Price": "max(0, BuildingDefinition.CreditsCost)",
        "MaterialsCost": "max(0, BuildingDefinition.MaterialsCost)",
        "FootprintCells": "componentwise max(1, BuildingDefinition.FootprintCells)",
        "CanRequest": "ConfiguredSpawnableEntry.CanRequest",
    }
    need(source.get("projection_rules") == expected_rules, "recorded source projection rule mismatch: " + key)
    model_footprint = model.get("FootprintCells")
    need(model["BuildingId"] == normalize_key(key) and model["DisplayName"] == entry_display and
         definition_display == entry_display and isinstance(model_footprint, dict) and
         type(model_footprint.get("x")) is int and type(model_footprint.get("y")) is int and
         type(model["Price"]) is int and model["Price"] == max(0, credits) and
         type(model["MaterialsCost"]) is int and model["MaterialsCost"] == max(0, materials) and
         model_footprint == {"x": max(1, xy[0]), "y": max(1, xy[1])} and
         type(model["CanRequest"]) is int and model["CanRequest"] == (1 if source["can_request"] else 0),
         "published readmodel values differ from original projection inputs: " + key)


def validate_prefab_source(prefab: Any, key: str, root: Path) -> dict:
    need(isinstance(prefab, dict) and prefab.get("codec") == "source-asset-reference-v1" and
         prefab.get("type") == "UnityEngine.GameObject" and prefab.get("key") == key,
         "source prefab identity mismatch: " + key)
    path = prefab.get("path")
    guid = prefab.get("guid")
    local_id = prefab.get("local_id")
    need(isinstance(path, str) and path.startswith("Assets/") and isinstance(guid, str) and
         re.fullmatch(r"[0-9a-f]{32}", guid) is not None and type(local_id) is int and local_id > 0,
         "source prefab identity malformed: " + key)
    asset = project_path(root, path)
    meta = project_path(root, path + ".meta")
    need(asset.is_file() and meta.is_file(), "source prefab or meta missing: " + key)
    match = re.search(r"^guid:\s*([0-9a-f]{32})\s*$", meta.read_text(encoding="utf-8"), re.MULTILINE)
    need(match is not None and match.group(1) == guid, "source prefab GUID differs from current meta: " + key)
    return {"path": path, "guid": guid, "localId": local_id}


def validate_entries(raw: dict, mapped_setup_rows: list[dict], root: Path) -> list[dict]:
    need(raw.get("kind") == "S004OriginalConfiguredSpawnableReadModel" and
         raw.get("authority") == "OriginalS004ProductionOverlayAndPublishConfiguredSpawnablesReadModel" and
         raw.get("publisher") == PUBLISHER,
         "raw authority/publisher identity mismatch")
    need(raw.get("scene_loaded") is False and raw.get("mission_building_spawned") is False and
         raw.get("mission_initialized") is False and raw.get("native_visual_readiness") is False and
         raw.get("mission_readiness") is False,
         "raw export claims scene, mission spawn, or readiness")
    need("DisposableUtilityWorld" in str(raw.get("capture_world_scope")) and
         "BuildingRunwaySystem" in str(raw.get("capture_world_scope")) and
         "WorldDisposedBeforeArtifactWrite" in str(raw.get("capture_world_scope")),
         "capture utility World scope/disposal not accurately declared")
    need("CacheBuildingBounds" in str(raw.get("temporary_bounds_probes")) and
         "destroys" in str(raw.get("temporary_bounds_probes")),
         "temporary prefab-probe lifecycle is not declared")
    need(raw.get("publisher_field_order") == list(FIELDS), "publisher field order mismatch")
    rows = raw.get("entries")
    expected_by_key: dict[str, set[str]] = {}
    for row in mapped_setup_rows:
        expected_by_key.setdefault(row["visualKey"], set()).add(row["structureId"])
    expected_keys = sorted(expected_by_key)
    need(len(expected_keys) == 9 and isinstance(rows, list) and len(rows) == 9,
         "capture must contain the exact nine unique compiled visual keys")
    need([row.get("visual_key") for row in rows if isinstance(row, dict)] == expected_keys,
         "readmodel rows must be ordered by exact compiled visual key")
    qualified = []
    for row in rows:
        key = row["visual_key"]
        source = row.get("source_definition")
        model = row.get("read_model")
        need(isinstance(source, dict) and isinstance(model, dict) and tuple(model.keys()) == FIELDS,
             "source projection or exact six-field readmodel shape/order mismatch: " + key)
        expected_ids = sorted(expected_by_key[key])
        need(row.get("structure_ids") == expected_ids, "compiled structure-to-visual-key provenance mismatch: " + key)
        prefab = validate_prefab_source(source.get("prefab"), key, root)
        authoring = source.get("authoring_component")
        need(isinstance(authoring, dict) and authoring.get("codec") == "source-asset-reference-v1" and
             authoring.get("type") == "Game.Authoring.BuildingDefinitionAuthoring" and
             authoring.get("path") == prefab["path"] and authoring.get("guid") == prefab["guid"],
             "source authoring identity does not match actual configured prefab: " + key)
        need(source.get("factory") == FACTORY and source.get("definition_type") == "Game.Runtime.BuildingDefinition",
             "original configured definition owner/type mismatch: " + key)
        validate_projection(source, model, key)
        entry_display = source["display_name"]
        qualified.append({"visualKey": key, "structureIds": expected_ids, "buildingId": model["BuildingId"],
                          "prefab": prefab, "displayName": model["DisplayName"], "price": model["Price"],
                          "materialsCost": model["MaterialsCost"], "footprintCells": model["FootprintCells"],
                          "canRequest": model["CanRequest"]})
    return qualified


def validate_wrapper_command(command: str, root: Path, raw_path: Path, evidence_path: Path) -> None:
    tokens = [line.strip() for line in command.splitlines() if line.strip()]
    need("-batchmode" not in tokens, "checked wrapper command must not use batchmode")
    def argument(flag: str) -> str:
        need(tokens.count(flag) == 1, "checked wrapper argument missing or duplicated: " + flag)
        index = tokens.index(flag)
        need(index + 1 < len(tokens), "checked wrapper argument value missing: " + flag)
        return tokens[index + 1]
    need(argument("-executeMethod") == METHOD, "checked wrapper method mismatch")
    for flag, expected in (("-projectPath", root),
                           ("-skirmishS004ConfiguredSpawnableReadModel", raw_path.parent),
                           ("-skirmishS004ConfiguredSpawnableSetupEvidence", evidence_path)):
        actual = Path(argument(flag))
        if not actual.is_absolute(): actual = root / actual
        need(actual.resolve() == expected.resolve(), "checked wrapper argument path mismatch: " + flag)


def verify(log_path: Path, raw_path: Path, evidence_path: Path, root: Path, wrapper_exit: int) -> dict:
    need(wrapper_exit == 0, "checked Unity wrapper exit must be exactly zero")
    log = log_path.read_text(encoding="utf-8", errors="strict")
    markers = [line for line in log.splitlines() if line.startswith("[SkirmishS004ConfiguredSpawnableReadModel]")]
    need(markers == [PASS_MARKER], "expected exactly one configured readmodel pass marker")
    need("Batch mode:              NO" in log and "Batchmode quit successfully invoked" in log and
         "Exiting batchmode successfully now!" in log,
         "checked wrapper log mode/shutdown evidence mismatch")
    command = log.split("COMMAND LINE ARGUMENTS:", 1)[1].split("Successfully changed project path to:", 1)[0]
    validate_wrapper_command(command, root, raw_path, evidence_path)
    raw = load_json(raw_path, "configured S004 readmodel export")
    pins = validate_live_pins(raw, root)
    verify_key_normalizer(root, pins)
    evidence, setup_rows, identity = validate_setup(raw, evidence_path, root)
    mapping_module = load_s004_mapping_module()
    mapping_module.verify_original_mapping(root, pins)
    qualified_rows = validate_entries(raw, setup_rows, root)
    need(raw.get("source_pins", {}).get(str(evidence_path.resolve())) == sha(evidence_path),
         "compiled setup evidence is not source-pinned by the raw capture")
    wrapper_path = root / "Tools/CI/invoke_unity_macos.sh"
    need(wrapper_path.is_file(), "checked Unity wrapper missing")
    return {
        "schema": SCHEMA,
        "evidenceClass": "OfflineQualificationOfOriginalS004ConfiguredSpawnableReadModel",
        "captureEpoch": ("S004ConfiguredReadModel_CurrentOriginalPublisher_2026-11-03-r3"
                         if raw_path.resolve() == (root / RAW_REL).resolve()
                         else "S004ConfiguredReadModel_CurrentOriginalPublisher_" + raw_path.parent.parent.name),
        "sourceEpochNote": "Distinct from earlier original factory-only captures; no definition-field parity is inferred across capture methods.",
        "captureLog": str(log_path.resolve()), "captureLogSha256": sha(log_path),
        "rawExport": str(raw_path.resolve()), "rawExportSha256": sha(raw_path),
        "wrapper": {"path": "Tools/CI/invoke_unity_macos.sh", "sha256": sha(wrapper_path),
                    "exitCode": wrapper_exit, "exitCodeAuthority": "CallerObserved", "editorMode": "GUI/BatchModeNO"},
        "passMarker": PASS_MARKER,
        "compiledSetup": {"catalogId": "S004", "definitionId": "skirmish.s004", "scenarioSetupId": "scenario.skirmish.s004",
                          "contentVersion": 2, "sizeId": "Standard", "difficultyId": "Regular", "seed": 104733,
                          "setupHash": identity["setupHash"], "evidenceSha256": identity["evidenceSha256"]},
        "structureEntryCount": len(setup_rows), "distinctStructureIdCount": len({r["structureId"] for r in setup_rows}),
        "distinctVisualKeyCount": len(qualified_rows), "publisher": PUBLISHER, "publisherFieldCount": len(FIELDS),
        "entries": qualified_rows, "sourcePinCount": len(pins), "sourcePinsVerified": True,
        "captureWorldScope": raw["capture_world_scope"], "temporaryBoundsProbes": raw["temporary_bounds_probes"],
        "sceneLoaded": False, "missionBuildingSpawned": False, "missionInitialized": False,
        "nativeVisualReadiness": False, "missionReadiness": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--log", type=Path)
    parser.add_argument("--raw", type=Path)
    parser.add_argument("--setup-evidence", type=Path)
    parser.add_argument("--wrapper-exit", type=int, required=True, help="caller-observed checked wrapper exit status")
    parser.add_argument("--out-dir", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    log = args.log or root / LOG_REL
    raw = args.raw or root / RAW_REL
    evidence = args.setup_evidence or root / SETUP_REL
    out = (args.out_dir or root / CAPTURE_DIR).resolve()
    receipt_path, source_path = out / "capture-receipt.json", out / "capture-source.json"
    try:
        need(not receipt_path.exists() and not source_path.exists(), "fresh finalizer receipt outputs required")
        receipt = verify(log, raw, evidence, root, args.wrapper_exit)
        out.mkdir(parents=True, exist_ok=True)
        receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        source_path.write_text(json.dumps({"schema": SCHEMA, "rawExportSha256": receipt["rawExportSha256"],
            "compiledSetupEvidenceSha256": receipt["compiledSetup"]["evidenceSha256"],
            "sourcePinCount": receipt["sourcePinCount"], "sourcePinsVerified": True,
            "captureEpoch": receipt["captureEpoch"]}, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print("[S004ConfiguredSpawnableReadModelFinalizer] result=Passed keys=" + str(receipt["distinctVisualKeyCount"]) +
              " publisherFields=" + str(receipt["publisherFieldCount"]) + " sourcePinsVerified=" +
              str(receipt["sourcePinCount"]) + " readiness=NotClaimed")
        return 0
    except (OSError, UnicodeError, json.JSONDecodeError, FinalizeError, IndexError, KeyError, TypeError) as exc:
        print("[S004ConfiguredSpawnableReadModelFinalizer] result=Failed reason=" + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
