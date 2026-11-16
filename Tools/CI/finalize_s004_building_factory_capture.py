#!/usr/bin/env python3
"""Offline finalizer for the S004 original building-definition factory capture."""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

CAPTURE_DIR = "Logs/SkirmishProduction/s004-building-factory-capture-20261103-r1"
RAW_DATA = f"{CAPTURE_DIR}/data/building-definitions.json"
CAPTURE_LOG = f"{CAPTURE_DIR}/editor.log"
SETUP_EVIDENCE = "Logs/SkirmishProduction/s004-standard-setup-export-20261103-r1/compiled-setup.json"
MARKER = "[SkirmishS004OriginalFactoryBuildingDefinitions] result=Passed structureEntries=20 distinctStructureIds=10 distinctVisualKeys=9 missionReady=0"
SCHEMA = "s004-building-factory-capture-receipt-v1"
FACTORY = "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition"

# Derived from the original SkirmishStructureIds.VisualKey mapping and checked
# against the freshly compiled setup rows. Watchtower and its approach share a key.
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


def pinned_path(root: Path, value: str) -> Path:
    candidate = Path(value)
    if candidate.is_absolute():
        result = candidate.resolve()
    else:
        result = (root / candidate).resolve()
    try:
        result.relative_to(root.resolve())
    except ValueError as exc:
        raise FinalizeError("source pin escapes project root: " + value) from exc
    return result


def validate_live_pins(raw: dict, root: Path) -> dict[str, str]:
    pins = raw.get("source_pins")
    need(isinstance(pins, dict) and bool(pins), "raw capture has no source pin set")
    observed: dict[str, str] = {}
    for value, digest in pins.items():
        need(isinstance(value, str) and isinstance(digest, str) and re.fullmatch(r"[0-9a-f]{64}", digest) is not None,
             "malformed source pin")
        path = pinned_path(root, value)
        need(path.is_file(), "source pin is missing: " + value)
        need(sha(path) == digest, "source pin differs from live source: " + value)
        observed[value] = digest
    return observed


def validate_setup(raw: dict, evidence_path: Path, root: Path) -> tuple[dict, list[dict]]:
    evidence_ref = raw.get("setup_evidence")
    need(isinstance(evidence_ref, dict), "setup evidence reference missing")
    recorded_path = pinned_path(root, str(evidence_ref.get("path", "")))
    expected_path = pinned_path(root, str(evidence_path))
    need(recorded_path == expected_path, "raw capture refers to a different compiled-setup evidence file")
    need(recorded_path.is_file(), "compiled-setup evidence is missing")
    evidence_sha = sha(recorded_path)
    need(evidence_ref.get("sha256") == evidence_sha, "compiled-setup evidence hash differs from raw capture")
    need(raw.get("source_pins", {}).get(str(evidence_ref.get("path"))) == evidence_sha,
         "compiled-setup evidence is not included in source pins")

    evidence = load_json(recorded_path, "compiled-setup evidence")
    identity = evidence.get("identity")
    compiled = evidence.get("compiled_setup")
    source_bound = evidence.get("source_bound_compiler_result")
    need(evidence.get("status") == "Passed" and evidence.get("kind") == "S004StandardCompiledSetupSourceExport",
         "original S004 setup evidence status/kind mismatch")
    need(isinstance(identity, dict) and isinstance(compiled, dict) and isinstance(source_bound, dict),
         "original compiled setup blocks are missing")
    expected_identity = {
        "catalog_id": "S004", "definition_id": "skirmish.s004",
        "scenario_setup_id": "scenario.skirmish.s004", "content_version": 2,
        "difficulty_id": "Regular", "size_id": "Standard", "seed": 104733,
        "measured_layout_bound": True,
    }
    for key, value in expected_identity.items():
        need(identity.get(key) == value, "compiled-setup identity mismatch: " + key)
    need(source_bound.get("status") == "Compiled" and source_bound.get("compiled") is True and
         source_bound.get("catalog_id") == "S004" and source_bound.get("definition_id") == "skirmish.s004",
         "source-bound original compiler did not report a compiled S004 setup")
    setup_hash = identity.get("setup_hash")
    need(isinstance(setup_hash, int) and not isinstance(setup_hash, bool) and
         compiled.get("SetupHash") == setup_hash and evidence_ref.get("setup_hash") == setup_hash,
         "compiled S004 setup hash disagrees across evidence")

    setup_identity = raw.get("setup_identity")
    need(isinstance(setup_identity, dict), "raw compiled setup identity missing")
    raw_identity = {
        "catalog_id": "catalog_id", "definition_id": "definition_id",
        "scenario_setup_id": "scenario_setup_id", "content_version": "content_version",
        "difficulty_id": "difficulty_id", "size_id": "size_id", "seed": "seed",
        "setup_hash": "setup_hash",
    }
    for evidence_key, raw_key in raw_identity.items():
        need(setup_identity.get(raw_key) == identity.get(evidence_key),
             "raw setup identity differs from compiled-setup evidence: " + raw_key)

    evidence_rows = compiled.get("Structures")
    raw_rows = raw.get("compiled_structure_entries")
    need(isinstance(evidence_rows, list) and isinstance(raw_rows, list) and bool(evidence_rows),
         "compiled setup structure rows missing")
    need(len(raw_rows) == len(evidence_rows), "raw structure row count differs from compiled setup")
    need(all(isinstance(row, dict) and type(row.get("FactionId")) is int and isinstance(row.get("StructureId"), str)
             for row in evidence_rows), "compiled setup contains malformed structure rows")
    need(all(isinstance(row, dict) and type(row.get("faction_id")) is int and isinstance(row.get("structure_id"), str)
             for row in raw_rows), "raw capture contains malformed structure rows")
    normalized_evidence = sorted((row.get("FactionId"), row.get("StructureId")) for row in evidence_rows)
    normalized_raw = sorted((row.get("faction_id"), row.get("structure_id")) for row in raw_rows)
    need(normalized_raw == normalized_evidence, "raw structure identities differ from compiled setup rows")
    for row in raw_rows:
        need(row.get("faction_id") in (1, 2), "invalid compiled structure faction")
        structure_id = row.get("structure_id")
        need(structure_id in STRUCTURE_VISUAL_KEYS, "compiled structure ID has no original VisualKey mapping")
        need(row.get("visual_key") == STRUCTURE_VISUAL_KEYS[structure_id],
             "raw visual key differs from original VisualKey mapping: " + str(structure_id))

    unique_ids = {row["structure_id"] for row in raw_rows}
    unique_keys = {STRUCTURE_VISUAL_KEYS[item] for item in unique_ids}
    need(unique_ids == set(STRUCTURE_VISUAL_KEYS), "compiled setup distinct structure ID set changed")
    need(raw.get("structure_entry_count") == len(evidence_rows), "raw structure entry count is inaccurate")
    need(raw.get("distinct_structure_id_count") == len(unique_ids), "raw distinct structure count is inaccurate")
    need(raw.get("distinct_visual_key_count") == len(unique_keys), "raw distinct visual key count is inaccurate")
    return evidence, raw_rows


def validate_factory_rows(raw: dict, raw_rows: list[dict], root: Path) -> list[dict]:
    definitions = raw.get("factory_definitions")
    need(isinstance(definitions, list), "factory definition rows missing")
    need(isinstance(raw.get("source_pins"), dict), "raw capture source pin set malformed")
    expected_keys = {STRUCTURE_VISUAL_KEYS[item] for item in STRUCTURE_VISUAL_KEYS}
    need({row.get("visual_key") for row in definitions if isinstance(row, dict)} == expected_keys and
         len(definitions) == len(expected_keys), "factory definitions do not cover every unique compiled visual key exactly once")

    by_key = {row["visual_key"]: row for row in definitions}
    for key in expected_keys:
        row = by_key.get(key)
        need(isinstance(row, dict), "factory definition missing: " + key)
        need(row.get("factory") == FACTORY and row.get("definition_type") == "Game.Runtime.BuildingDefinition",
             "factory identity mismatch: " + key)
        footprint = row.get("runtime_footprint_cells")
        need(isinstance(footprint, dict) and type(footprint.get("x")) is int and type(footprint.get("y")) is int and
             footprint["x"] > 0 and footprint["y"] > 0,
             "runtime footprint is missing or non-positive: " + key)
        expected_ids = sorted(item for item, mapped in STRUCTURE_VISUAL_KEYS.items() if mapped == key)
        need(row.get("structure_ids") == expected_ids, "factory structure-to-key identities mismatch: " + key)

        prefab = row.get("prefab")
        authoring = row.get("authoring_component")
        need(isinstance(prefab, dict) and isinstance(authoring, dict), "prefab/authoring source identity missing: " + key)
        need(prefab.get("codec") == "source-asset-reference-v1" and prefab.get("type") == "UnityEngine.GameObject" and
             prefab.get("key") == key and authoring.get("codec") == "source-asset-reference-v1" and
             authoring.get("type") == "Game.Authoring.BuildingDefinitionAuthoring" and authoring.get("key") == key,
             "prefab/authoring source identity mismatch: " + key)
        need(prefab.get("path") == authoring.get("path") and prefab.get("guid") == authoring.get("guid"),
             "authoring component is not attached to the identified prefab: " + key)
        for asset in (prefab, authoring):
            path_value = asset.get("path")
            local_id = asset.get("local_id")
            need(isinstance(path_value, str) and type(local_id) is int and local_id > 0,
                 "source asset path/local identity malformed: " + key)
            path = pinned_path(root, path_value)
            need(path.is_file(), "source prefab asset missing: " + path_value)
            meta = path.with_name(path.name + ".meta")
            need(meta.is_file(), "source prefab metadata missing: " + str(meta))
            guid_match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", meta.read_text(encoding="utf-8"))
            need(guid_match is not None and guid_match.group(1) == asset.get("guid"),
                 "source asset GUID differs from live meta: " + path_value)
            pins = raw.get("source_pins", {})
            need(pins.get(path_value) == sha(path), "source prefab is not pinned or pin differs: " + path_value)
            need(pins.get(path_value + ".meta") == sha(meta), "source prefab meta is not pinned or pin differs: " + path_value)
    return definitions


def verify(log_path: Path, raw_path: Path, evidence_path: Path, root: Path, wrapper_exit: int) -> dict:
    need(wrapper_exit == 0, "Unity wrapper exit must be exactly zero")
    root = root.resolve()
    log_path = log_path.resolve()
    raw_path = raw_path.resolve()
    log = log_path.read_text(encoding="utf-8", errors="strict")
    need(log_path.is_file(), "original Editor log missing")
    marker_lines = [line for line in log.splitlines() if line.startswith("[SkirmishS004OriginalFactoryBuildingDefinitions]")]
    need(marker_lines == [MARKER], "expected exactly one original S004 factory pass marker")
    need("Batchmode quit successfully invoked" in log and "Exiting batchmode successfully now!" in log,
         "original Unity wrapper/editor shutdown markers missing")

    raw = load_json(raw_path, "raw building-definition export")
    need(raw.get("schema_version") == 1 and raw.get("kind") == "S004OriginalFactoryBuildingDefinitions" and
         raw.get("authority") == "OriginalCompiledSetupAndConfiguredPrefabFactory_NoMissionState",
         "raw S004 factory export schema/authority mismatch")
    need(raw.get("scene_loaded") is False and raw.get("gameplay_world_created") is False and
         raw.get("mission_initialized") is False and raw.get("native_visual_readiness") is False and
         raw.get("mission_readiness") is False,
         "raw source-only capture incorrectly claims a loaded scene, gameplay, or readiness")
    need(raw.get("runway_system_supplied_to_factory") is False and
         raw.get("runway_derived_fields_captured") is False and
         isinstance(raw.get("factory_scope"), str) and "runway reader omitted" in raw["factory_scope"] and
         "No full factory/native startup parity claim" in raw["factory_scope"],
         "raw capture overstates runway coverage or factory parity")

    evidence, raw_rows = validate_setup(raw, evidence_path, root)
    definitions = validate_factory_rows(raw, raw_rows, root)
    pins = validate_live_pins(raw, root)
    log_bytes = log_path.read_bytes()
    raw_bytes = raw_path.read_bytes()
    evidence_bytes = pinned_path(root, str(raw["setup_evidence"]["path"])).read_bytes()
    return {
        "schema": SCHEMA,
        "evidenceClass": "OfflineFinalizationOfOriginalS004BuildingFactoryCapture",
        "captureLog": str(log_path),
        "captureLogSha256": hashlib.sha256(log_bytes).hexdigest(),
        "rawExport": str(raw_path),
        "rawExportSha256": hashlib.sha256(raw_bytes).hexdigest(),
        "compiledSetupEvidence": str(evidence_path.resolve()),
        "compiledSetupEvidenceSha256": hashlib.sha256(evidence_bytes).hexdigest(),
        "wrapper": {"path": "Tools/CI/invoke_unity_macos.sh", "sha256": sha(root / "Tools/CI/invoke_unity_macos.sh"),
                    "exitCode": wrapper_exit, "exitCodeAuthority": "CallerObserved", "editorShutdownMarkersPresent": True},
        "originalPassMarker": MARKER,
        "compiledSetup": {"catalogId": "S004", "definitionId": "skirmish.s004",
                          "scenarioSetupId": "scenario.skirmish.s004", "contentVersion": 2,
                          "sizeId": "Standard", "difficultyId": "Regular", "seed": 104733,
                          "setupHash": evidence["identity"]["setup_hash"]},
        "structureEntryCount": len(raw_rows),
        "distinctStructureIdCount": len({row["structure_id"] for row in raw_rows}),
        "distinctVisualKeyCount": len({row["visual_key"] for row in raw_rows}),
        "factoryDefinitionCount": len(definitions),
        "definitions": [{"visualKey": row["visual_key"], "structureIds": row["structure_ids"],
                          "prefabPath": row["prefab"]["path"], "prefabGuid": row["prefab"]["guid"],
                          "prefabLocalId": row["prefab"]["local_id"],
                          "authoringLocalId": row["authoring_component"]["local_id"],
                          "runtimeFootprintCells": row["runtime_footprint_cells"]}
                         for row in sorted(definitions, key=lambda item: item["visual_key"])],
        "sourcePinCount": len(pins),
        "sourcePinsVerified": True,
        "sceneLoaded": False,
        "gameplayWorldCreated": False,
        "runwayData": "NotCaptured_RunwaySystemNotSupplied",
        "fullFactoryParity": "NotClaimed",
        "missionInitialized": False,
        "nativeVisualReadiness": False,
        "missionReadiness": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--log", type=Path)
    parser.add_argument("--raw", type=Path)
    parser.add_argument("--setup-evidence", type=Path)
    parser.add_argument("--wrapper-exit", type=int, required=True, help="observed Unity wrapper exit status")
    parser.add_argument("--out-dir", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    log = args.log if args.log else root / CAPTURE_LOG
    raw = args.raw if args.raw else root / RAW_DATA
    evidence = args.setup_evidence if args.setup_evidence else root / SETUP_EVIDENCE
    out_dir = args.out_dir.resolve() if args.out_dir else root / CAPTURE_DIR
    receipt_path = out_dir / "capture-receipt.json"
    source_path = out_dir / "capture-source.json"
    try:
        need(not receipt_path.exists() and not source_path.exists(), "fresh finalizer receipt outputs required")
        receipt = verify(log, raw, evidence, root, args.wrapper_exit)
        out_dir.mkdir(parents=True, exist_ok=True)
        receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        source = {"schema": SCHEMA, "compiledSetupEvidenceSha256": receipt["compiledSetupEvidenceSha256"],
                  "rawExportSha256": receipt["rawExportSha256"], "sourcePinCount": receipt["sourcePinCount"],
                  "sourcePinsVerified": True, "setupHash": receipt["compiledSetup"]["setupHash"]}
        source_path.write_text(json.dumps(source, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print("[S004BuildingFactoryCaptureFinalizer] result=Passed structureEntries=" +
              str(receipt["structureEntryCount"]) + " distinctStructureIds=" +
              str(receipt["distinctStructureIdCount"]) + " distinctVisualKeys=" +
              str(receipt["distinctVisualKeyCount"]) + " sourcePinsVerified=" +
              str(receipt["sourcePinCount"]) + " runway=NotCaptured readiness=NotClaimed")
        return 0
    except (OSError, UnicodeError, json.JSONDecodeError, FinalizeError) as exc:
        print("[S004BuildingFactoryCaptureFinalizer] result=Failed reason=" + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
