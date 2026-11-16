#!/usr/bin/env python3
"""Offline qualification of the full S004 runtime building-definition capture."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path
from typing import Any

CAPTURE_DIR = "Logs/SkirmishProduction/s004-full-building-factory-capture-20261103-r1"
RAW_DATA = f"{CAPTURE_DIR}/data/building-definitions.json"
CAPTURE_LOG = f"{CAPTURE_DIR}/editor.log"
FOOTPRINT_DIR = "Logs/SkirmishProduction/s004-building-factory-capture-20261103-r1"
FOOTPRINT_RAW = f"{FOOTPRINT_DIR}/data/building-definitions.json"
FOOTPRINT_LOG = f"{FOOTPRINT_DIR}/editor.log"
FOOTPRINT_RECEIPT = f"{FOOTPRINT_DIR}/capture-receipt.json"
SETUP_EVIDENCE = "Logs/SkirmishProduction/s004-standard-setup-export-20261103-r1/compiled-setup.json"
PASS_MARKER = "[SkirmishAuthoredBuildingDefinitions] result=Passed definitions=9 unsupported=0 fullMission=0"
SCHEMA = "s004-full-building-factory-capture-receipt-v1"
FACTORY = "BuildingDefinitionPrefabSystemHelper.CreateRuntimeBuildingDefinition"

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

BUILDING_DEFINITION_FIELDS = frozenset({
    "DisplayName", "Description", "MaxHealth", "ProductionSlots", "SpawnUnitPrefab",
    "SecondarySpawnUnitPrefab", "TertiarySpawnUnitPrefab", "QuaternarySpawnUnitPrefab", "Prefab",
    "DestroyedVisualPrefab", "SelectionPortraitSprite", "CardPortraitSprite", "FootprintCells", "Role",
    "IsWall", "CreditsCost", "MaterialsCost", "ProductionDurationSeconds", "OilBarrelsPerDay",
    "OilStorageCapacity", "FuelBarrelsPerDay", "FuelStorageCapacity", "MaterialFabricationEnabled",
    "MaterialFabricationOilConsumedPerCycle", "MaterialFabricationMaterialsOutputPerCycle",
    "MaterialFabricationCycleDurationSeconds", "MaterialFabricationOutputCapacityPolicy", "RefugeeCapacity",
    "RefugeeUpkeepPerCitizenPerDay", "ThreatDetectionKind", "ThreatDetectionRadiusCells", "CanAttack",
    "MaxConcurrentAttacks", "AttackRange", "AttackCooldownSeconds", "AttackDamage", "AttackImpactPrefab",
    "MuzzleFlashPrefab", "MuzzleFlashHeightOffset", "MuzzleFlashForwardOffset", "AttackTraceColor",
    "AttackTraceWidth", "AttackTraceScrollSpeed", "AttackTraceDashDensity", "AttackTraceVisibleSeconds",
    "AttackTracerEveryNthShot", "LocalBounds", "HasLocalBounds", "VisualTemplate", "GeneratedMeshes",
    "ProductionSpawnLocalPositions", "HasRunway", "RunwayLocalPosition", "RunwayLocalRotation",
    "RunwayHalfExtents",
})


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
        raise FinalizeError("source path escapes project root: " + raw_path) from exc
    return path


def verify_pins(raw: dict, root: Path, label: str) -> dict[str, str]:
    pins = raw.get("source_pins")
    need(isinstance(pins, dict) and bool(pins), label + " source pin set missing")
    relevant = {
        "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.BuildingData.cs",
        "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
        "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
        "Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionCatalog.cs",
        "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs",
        "Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs",
    }
    need(relevant.issubset(pins), label + " does not pin the required compiler/factory/mapping source files")
    for raw_path, digest in pins.items():
        need(isinstance(raw_path, str) and isinstance(digest, str) and re.fullmatch(r"[0-9a-f]{64}", digest) is not None,
             label + " contains a malformed source pin")
        path = project_path(root, raw_path)
        need(path.is_file(), label + " source pin is missing: " + raw_path)
        need(sha(path) == digest, label + " source pin differs from live source: " + raw_path)
    return pins


def load_footprint_finalizer():
    path = Path(__file__).with_name("finalize_s004_building_factory_capture.py")
    spec = importlib.util.spec_from_file_location("s004_footprint_capture_finalizer", path)
    if spec is None or spec.loader is None:
        raise FinalizeError("S004 footprint finalizer module unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def verify_original_mapping(root: Path, pins: dict[str, str]) -> None:
    source_rel = "Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs"
    source_path = project_path(root, source_rel)
    source = source_path.read_text(encoding="utf-8")
    need(pins.get(source_rel) == sha(source_path), "original VisualKey mapping source is not pinned")
    required_fragments = (
        'if (structureId == GroundStaging)\n                return "Building_GroundStaging";',
        'if (structureId == Helipad)\n                return "Building_Helipad";',
        'if (structureId == OilPump)\n                return "Building_OilPump";',
        'if (structureId == Refinery || structureId == RefineryModule)\n                return "Building_Refinery";',
        'if (structureId == FuelBladder)\n                return "Building_Fuel_Bladder";',
        'if (structureId == FabricationDepot)\n                return "Building_Ammunition_Depot";',
        'if (structureId == Watchtower || structureId == WatchtowerApproach || structureId == DefenseApproachA || structureId == DefenseApproachB)\n                return "Building_GuardTower";',
        'if (structureId == SatelliteDish)\n                return "Building_Satelite_Dish";',
        'structureId.StartsWith(Barracks, System.StringComparison.Ordinal))\n                return "Building_Barrack";',
    )
    need(all(fragment in source for fragment in required_fragments),
         "live original VisualKey mapping differs from the qualified S004 key table")


def verify_full_capture_world_scope(root: Path, pins: dict[str, str]) -> None:
    source_rel = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.BuildingData.cs"
    source_path = project_path(root, source_rel)
    source = source_path.read_text(encoding="utf-8")
    need(pins.get(source_rel) == sha(source_path), "full factory capture source is not pinned")
    start = source.find("public static void RunBuildingReferenceData()")
    need(start >= 0, "original full factory capture method missing")
    opening = source.find("{", start)
    depth = 0
    end = -1
    for index in range(opening, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                end = index + 1
                break
    need(end > opening, "original capture method body could not be bounded")
    method = source[start:end]
    need('using (var world = new World("SourceBuildingDefinitionMetadata", WorldFlags.Game))' in method and
         'world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"))' in method,
         "full capture no longer uses the pinned disposable factory utility World/runway system scope")
    need("SceneSystem.LoadSceneAsync" not in method and "DefaultWorldInitialization" not in method and
         "EditorApplication.isPlaying" in method,
         "full capture method now loads a scene or initializes gameplay systems")
    need("using (var world =" in method and method.index("using (var world =") < method.index("Directory.CreateDirectory(output)"),
         "factory utility World is not disposed before artifact output")


def decode_footprint(fields: dict, key: str) -> tuple[int, int]:
    value = fields.get("FootprintCells")
    need(isinstance(value, dict) and value.get("codec") == "struct-fields-v1" and
         value.get("type") == "UnityEngine.Vector2Int", "FootprintCells codec/type mismatch: " + key)
    components = value.get("fields")
    need(isinstance(components, dict), "FootprintCells components missing: " + key)
    coords = []
    for suffix in ("m_X", "m_Y"):
        component = components.get("UnityEngine.Vector2Int::" + suffix)
        need(isinstance(component, dict) and component.get("codec") == "integer-v1" and
             component.get("type") == "System.Int32", "FootprintCells integer component missing: " + key)
        try:
            coords.append(int(component["value"]))
        except (KeyError, TypeError, ValueError) as exc:
            raise FinalizeError("FootprintCells integer is malformed: " + key) from exc
    need(coords[0] > 0 and coords[1] > 0, "runtime footprint is non-positive: " + key)
    return coords[0], coords[1]


def source_asset_identity(row: dict, key: str, root: Path) -> dict:
    prefab = row.get("prefab")
    need(isinstance(prefab, dict) and prefab.get("codec") == "source-asset-reference-v1" and
         prefab.get("type") == "UnityEngine.GameObject" and prefab.get("key") == key,
         "prefab source identity mismatch: " + key)
    path_value = prefab.get("path")
    need(isinstance(path_value, str) and type(prefab.get("local_id")) is int and prefab["local_id"] > 0 and
         re.fullmatch(r"[0-9a-f]{32}", str(prefab.get("guid", ""))) is not None,
         "prefab source identity path/GUID/local ID malformed: " + key)
    path = project_path(root, path_value)
    meta_path = path.with_name(path.name + ".meta")
    need(path.is_file() and meta_path.is_file(), "prefab or its meta file is missing: " + key)
    match = re.search(r"(?m)^guid:\s*([0-9a-f]{32})\s*$", meta_path.read_text(encoding="utf-8"))
    need(match is not None and match.group(1) == prefab["guid"], "prefab GUID differs from current meta: " + key)
    return prefab


def verify(log_path: Path, raw_path: Path, root: Path, wrapper_exit: int) -> dict:
    need(wrapper_exit == 0, "full factory capture wrapper exit must be exactly zero")
    root = root.resolve()
    log_path, raw_path = log_path.resolve(), raw_path.resolve()
    log = log_path.read_text(encoding="utf-8", errors="strict")
    marker_lines = [line for line in log.splitlines() if line.startswith("[SkirmishAuthoredBuildingDefinitions]")]
    need(marker_lines == [PASS_MARKER], "expected exactly one full factory pass marker")
    need("Batch mode:              NO" in log and "Exiting batchmode successfully now!" in log and
         "Batchmode quit successfully invoked" in log,
         "original Unity log mode or graceful shutdown markers mismatch")
    command = log.split("COMMAND LINE ARGUMENTS:", 1)[1].split("Successfully changed project path to:", 1)[0]
    expected_args = (
        "-executeMethod\nGame.Editor.SkirmishBakedActorSnapshotExporter.RunBuildingReferenceData",
        "-skirmishBuildingReferenceCatalog\nS004",
        "-skirmishBuildingReferenceSeed\n104733",
        "-skirmishBuildingReferenceKeys\nBuilding_Ammunition_Depot,Building_Barrack,Building_Fuel_Bladder,Building_GroundStaging,Building_GuardTower,Building_Helipad,Building_OilPump,Building_Refinery,Building_Satelite_Dish",
    )
    need(all(fragment in command for fragment in expected_args) and "-batchmode" not in command and
         "-skirmishBuildingReferenceGeometry" not in command,
         "original Unity capture command arguments differ or request geometry")
    need(str(root) in command, "Unity capture log project path is not this repository")

    raw = load_json(raw_path, "full S004 runtime factory export")
    need(raw.get("schema_version") == 1 and raw.get("kind") == "AuthoredSkirmishBuildingDefinitions" and
         raw.get("authority") == "OriginalConfiguredPrefabMetadataFactory_NotMissionState" and
         raw.get("catalog_id") == "S004" and raw.get("seed") == 104733 and
         raw.get("setup_hash") == 1130726651,
         "full S004 runtime factory export identity/hash mismatch")
    need(raw.get("full_mission_initialized") is False and raw.get("native_visual_readiness") is False and
         raw.get("unsupported_values") == [],
         "full capture overclaims mission/visual readiness or contains unsupported field values")
    rows = raw.get("definitions")
    requested = raw.get("requested_keys")
    expected_keys = sorted(set(STRUCTURE_VISUAL_KEYS.values()))
    need(isinstance(rows, list) and isinstance(requested, list) and requested == expected_keys and
         len(rows) == len(expected_keys), "full capture does not contain the exact nine derived S004 keys")
    need([row.get("key") for row in rows] == expected_keys, "full factory definition rows are not in exact requested-key order")

    previous = load_json(root / FOOTPRINT_RECEIPT, "prior footprint-only finalizer receipt")
    footprint_finalizer = load_footprint_finalizer()
    previous_verified = footprint_finalizer.verify(root / FOOTPRINT_LOG, root / FOOTPRINT_RAW,
                                                    root / SETUP_EVIDENCE, root, 0)
    need(previous.get("schema") == "s004-building-factory-capture-receipt-v1" and
         previous.get("rawExportSha256") == previous_verified.get("rawExportSha256") and
         previous.get("captureLogSha256") == previous_verified.get("captureLogSha256") and
         previous.get("compiledSetupEvidenceSha256") == previous_verified.get("compiledSetupEvidenceSha256") and
         previous.get("compiledSetup", {}).get("setupHash") == previous_verified.get("compiledSetup", {}).get("setupHash") and
         previous.get("wrapper", {}).get("exitCode") == 0,
         "prior footprint-only receipt is stale or does not validate against its original capture")
    need(previous_verified["compiledSetup"]["setupHash"] == raw["setup_hash"] and
         previous_verified["compiledSetup"]["seed"] == raw["seed"] and
         previous_verified["compiledSetup"]["catalogId"] == raw["catalog_id"],
         "full capture does not bind the prior compiled S004 setup identity")
    previous_rows = {row["visualKey"]: row for row in previous_verified["definitions"]}
    need(set(previous_rows) == set(expected_keys), "prior footprint receipt does not cover the exact S004 visual key set")

    full_pins = verify_pins(raw, root, "full 55-field runtime capture")
    verify_original_mapping(root, full_pins)
    verify_full_capture_world_scope(root, full_pins)

    normalized = []
    for row in rows:
        key = row.get("key")
        need(key in expected_keys and row.get("factory") == FACTORY and
             row.get("definition_type") == "Game.Runtime.BuildingDefinition",
             "factory identity mismatch: " + str(key))
        fields = row.get("fields")
        need(isinstance(fields, dict) and len(fields) == 55 and set(fields) == BUILDING_DEFINITION_FIELDS,
             "full runtime BuildingDefinition field set is not exactly 55 fields: " + str(key))
        footprint = decode_footprint(fields, key)
        prefab = source_asset_identity(row, key, root)
        need(fields.get("Prefab", {}).get("codec") == "source-asset-reference-v1" and
             fields["Prefab"].get("path") == prefab["path"] and fields["Prefab"].get("guid") == prefab["guid"] and
             fields["Prefab"].get("local_id") == prefab["local_id"],
             "runtime definition prefab field differs from factory row source identity: " + key)

        prior = previous_rows[key]
        need(prior.get("prefabPath") == prefab["path"] and prior.get("prefabGuid") == prefab["guid"] and
             prior.get("prefabLocalId") == prefab["local_id"] and
             prior.get("runtimeFootprintCells") == {"x": footprint[0], "y": footprint[1]},
             "full-factory source identity or footprint differs from prior S004 capture: " + key)
        normalized.append({"key": key, "prefab": prefab, "footprintCells": {"x": footprint[0], "y": footprint[1]},
                           "hasRunway": fields.get("HasRunway"), "fieldCount": len(fields)})

    log_bytes, raw_bytes = log_path.read_bytes(), raw_path.read_bytes()
    previous_raw_path = root / FOOTPRINT_RAW
    previous_log_path = root / FOOTPRINT_LOG
    evidence_path = root / SETUP_EVIDENCE
    return {
        "schema": SCHEMA,
        "evidenceClass": "OfflineFinalizationOfOriginalS004FullBuildingDefinitions",
        "captureLog": str(log_path),
        "captureLogSha256": hashlib.sha256(log_bytes).hexdigest(),
        "rawExport": str(raw_path),
        "rawExportSha256": hashlib.sha256(raw_bytes).hexdigest(),
        "wrapper": {"path": "Tools/CI/invoke_unity_macos.sh", "exitCode": wrapper_exit,
                    "exitCodeAuthority": "CallerObserved", "editorMode": "GUI/BatchModeNO"},
        "passMarker": PASS_MARKER,
        "compiledSetup": previous_verified["compiledSetup"],
        "priorFootprintCapture": {
            "receipt": str(root / FOOTPRINT_RECEIPT),
            "rawExport": str(previous_raw_path),
            "rawExportSha256": previous["rawExportSha256"],
            "captureLogSha256": previous["captureLogSha256"],
            "compiledSetupEvidence": str(evidence_path),
            "compiledSetupEvidenceSha256": previous["compiledSetupEvidenceSha256"],
            "wrapperExit": previous["wrapper"]["exitCode"],
        },
        "factory": FACTORY,
        "definitionFieldCount": 55,
        "definitionCount": len(normalized),
        "sourcePinCount": len(full_pins),
        "sourcePinsVerified": True,
        "definitions": normalized,
        "captureWorldScope": "OriginalDisposableUtilityWorldOnlyForBuildingRunwaySystem;NoSceneStreamOrGameplaySystems;WorldDisposedBeforeArtifactWrite",
        "sceneLoaded": False,
        "visualGeometryCaptured": False,
        "nativeVisualReadiness": False,
        "missionInitialized": False,
        "missionReadiness": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--log", type=Path)
    parser.add_argument("--raw", type=Path)
    parser.add_argument("--wrapper-exit", type=int, required=True, help="observed Unity wrapper exit status")
    parser.add_argument("--out-dir", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    log = args.log if args.log else root / CAPTURE_LOG
    raw = args.raw if args.raw else root / RAW_DATA
    out_dir = args.out_dir.resolve() if args.out_dir else root / CAPTURE_DIR
    receipt_path, source_path = out_dir / "capture-receipt.json", out_dir / "capture-source.json"
    try:
        need(not receipt_path.exists() and not source_path.exists(), "fresh full factory receipt outputs required")
        receipt = verify(log, raw, root, args.wrapper_exit)
        out_dir.mkdir(parents=True, exist_ok=True)
        receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        source = {"schema": SCHEMA, "rawExportSha256": receipt["rawExportSha256"],
                  "compiledSetupEvidenceSha256": receipt["priorFootprintCapture"]["compiledSetupEvidenceSha256"],
                  "sourcePinCount": receipt["sourcePinCount"], "sourcePinsVerified": True,
                  "priorFootprintExportSha256": receipt["priorFootprintCapture"]["rawExportSha256"]}
        source_path.write_text(json.dumps(source, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print("[S004FullBuildingFactoryFinalizer] result=Passed definitions=" + str(receipt["definitionCount"]) +
              " fieldsPerDefinition=" + str(receipt["definitionFieldCount"]) +
              " sourcePinsVerified=" + str(receipt["sourcePinCount"]) + " scene=NotLoaded visualReadiness=0 missionReadiness=0")
        return 0
    except (OSError, UnicodeError, json.JSONDecodeError, FinalizeError) as exc:
        print("[S004FullBuildingFactoryFinalizer] result=Failed reason=" + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
