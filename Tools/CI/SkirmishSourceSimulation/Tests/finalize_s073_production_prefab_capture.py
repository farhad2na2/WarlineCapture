#!/usr/bin/env python3
"""Strictly qualify the native S073 twelve-root production-template capture."""
import argparse
import hashlib
import json
import re
from pathlib import Path


EXPECTED_KEY_SHA256 = "48e5480735827034f963dc053d9c2eca37bde242d42a71e7965aaaae6897d7b0"
KEYS = [
    "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Fast", "Unit_Veh_APC_Heavy", "Unit_Veh_APC_Slow",
    "Unit_Veh_Light_Armored_Car", "Unit_Veh_Missle_Launcher_Air", "Unit_Veh_Missle_Launcher_Ground",
    "Unit_Veh_Radar_Tank", "Unit_Veh_Tank_USA", "Unit_Veh_Truck_Canopy", "Unit_Veh_Truck_Tanker",
    "Unit_Veh_Truck_Tray",
]
STARTUP_KEYS = {
    "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_Light_Armored_Car",
    "Unit_Veh_Truck_Tanker", "Unit_Veh_Truck_Tray",
}
PRESENTATION_FIELDS = {
    "Game.Components.UnitAttackImpactVfxReference": {"Prefab"},
    "Game.Components.UnitMuzzleFlashVfxReference": {"Prefab"},
    "Game.Components.GroundMissileLauncherVfxReferenceComponent": {
        "LauncherBackfirePrefab", "RocketTrailPrefab", "ImpactExplosionPrefab", "ImpactSmokePrefab",
    },
    "Game.Components.AirMissileLauncherVfxReferenceComponent": {
        "MissileVisualPrefab", "LaunchFlashPrefab", "LaunchSmokePrefab", "MissileTrailPrefab",
        "AirburstExplosionPrefab", "AirTargetImpactPrefab", "InterceptExplosionPrefab",
    },
}
UNITY_OBJECT_REF = (
    "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, "
    "Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"
)


def sha(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def need(condition, message):
    if not condition:
        raise ValueError(message)


def verify_pins(pins, repo, external_key=None):
    need(isinstance(pins, dict) and pins, "Nonempty source pin map required")
    seen = set()
    for raw, expected in pins.items():
        path = Path(raw)
        if not path.is_absolute():
            path = repo / path
        path = path.resolve(strict=True)
        allowed = path.is_relative_to(repo)
        if external_key is not None and path == external_key.resolve(strict=True):
            allowed = True
        need(allowed, "Source pin outside repository/current key input: " + str(path))
        need(path not in seen, "Duplicate normalized source pin: " + str(path))
        seen.add(path)
        need(len(expected) == 64 and sha(path) == expected, "Source pin drift: " + str(path))
    return len(seen)


def validate_log(path, method, output_path, required_args, marker, wrapper_exit):
    need(wrapper_exit == 0, "Checked GUI Unity wrapper must exit zero")
    text = Path(path).read_text(encoding="utf-8", errors="strict")
    need(text.splitlines().count(marker) == 1, "Exact required native pass marker missing/duplicated")
    need("COMMAND LINE ARGUMENTS:" in text and method in text, "Unity command-line method binding missing")
    for argument in [str(output_path.resolve())] + [str(value) for value in required_args]:
        need(argument in text, "Unity log does not bind required exact argument: " + argument)
    return sha(path)


def valid_entity_id(value):
    if not isinstance(value, str) or not re.fullmatch(r"(?:0|[1-9][0-9]*):(?:0|[1-9][0-9]*)", value):
        return False
    index, version = map(int, value.split(":"))
    return index >= 0 and version >= 0 and ((index == 0) == (version == 0))


def validate_unsupported(rows):
    need(isinstance(rows, list), "unsupported_values array required")
    counts = {name: 0 for name in PRESENTATION_FIELDS}
    for row in rows:
        need(isinstance(row, dict), "Unsupported row must be an object")
        parts = row.get("path", "").split("/")
        need(len(parts) == 3 and valid_entity_id(parts[0]), "Canonical entity/component/field path required")
        component, field = parts[1], parts[2]
        need(component in PRESENTATION_FIELDS and field in PRESENTATION_FIELDS[component],
             "Unsupported field is outside original importer presentation allowlist: " + str(row.get("path")))
        need(row.get("reason") == "unity_object_ref_requires_bound_source_identity" and
             row.get("type") == UNITY_OBJECT_REF,
             "Unsupported value is outside original UnityObjectRef<GameObject> allowlist")
        counts[component] += 1
    return counts


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--key-wrapper-exit", required=True, type=int)
    parser.add_argument("--capture-wrapper-exit", required=True, type=int)
    parser.add_argument("--key-path", required=True, type=Path)
    parser.add_argument("--key-provenance", required=True, type=Path)
    parser.add_argument("--key-log", required=True, type=Path)
    parser.add_argument("--capture-path", required=True, type=Path)
    parser.add_argument("--capture-log", required=True, type=Path)
    parser.add_argument("--audit-path", required=True, type=Path)
    parser.add_argument("--receipt", required=True, type=Path)
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    receipt_path = args.receipt.absolute()
    need(repo == Path(__file__).resolve().parents[4], "Exact project root required")
    need(not receipt_path.exists(), "Fresh receipt output path required")
    key_path = args.key_path.resolve(strict=True)
    key_provenance_path = args.key_provenance.resolve(strict=True)
    key_log_path = args.key_log.resolve(strict=True)
    capture_path = args.capture_path.resolve(strict=True)
    capture_log_path = args.capture_log.resolve(strict=True)
    audit_path = args.audit_path.resolve(strict=True)
    key_sha256 = sha(key_path)
    audit_sha256 = sha(audit_path)
    for path in (key_path, key_provenance_path, key_log_path, capture_path, capture_log_path, audit_path):
        need(path.is_file(), "Required current native evidence missing: " + str(path))

    key_bytes = key_path.read_bytes()
    need(key_sha256 == EXPECTED_KEY_SHA256, "Exact current native 12-key bytes required")
    keys = json.loads(key_bytes.decode("utf-8"))
    need(keys == KEYS and len(keys) == len(set(keys)) == 12, "Exact 12 current S073 derived keys required")
    provenance = read(key_provenance_path)
    need(provenance.get("schema_version") == 1 and provenance.get("kind") == "S073OriginalProductionPrefabKeyDerivation" and
         provenance.get("mission") == "S073" and
         provenance.get("authority") == "CurrentYamlAuditAndOriginalConstructionBuildingDefinitionMetadata" and
         provenance.get("key_array_path") == str(key_path) and provenance.get("key_array_sha256") == key_sha256 and
         provenance.get("key_count") == 12 and provenance.get("keys") == KEYS and
         provenance.get("mission_world_loaded") is False and provenance.get("mission_initialized") is False and
         provenance.get("native_readiness") is False and provenance.get("physical_geometry_exported") is False,
         "Exact source-only S073 key provenance required")
    need(provenance.get("source_audit_path") == str(audit_path) and
         provenance.get("source_audit_sha256") == audit_sha256,
         "Exact fresh S073 asset audit binding required")
    audit = read(audit_path)
    need(audit.get("kind") == "S073LegacySourceAssetReferenceAudit" and audit.get("mission") == "S073" and
         audit.get("schema_version") == 1, "Exact current S073 direct-reference audit required")
    pins_key = provenance.get("source_pins_sha256")
    key_pin_count = verify_pins(pins_key, repo)
    construction = provenance.get("construction_buildings")
    need(isinstance(construction, list) and len(construction) == 6, "Six authored Construction metadata records required")
    products = []
    by_building = {}
    for row in construction:
        building = row.get("building", {}).get("name")
        refs = row.get("production_prefabs")
        need(isinstance(building, str) and building not in by_building and isinstance(refs, list),
             "Unique exact authored building/product rows required")
        by_building[building] = refs
        for ref in refs:
            product = ref.get("prefab", {})
            need(product.get("name") in KEYS and len(product.get("guid", "")) == 32 and
                 isinstance(product.get("local_file_id"), int), "Exact production prefab asset identity required")
            products.append(product["name"])
    need(set(by_building) == {"Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
                              "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder"} and
         len(products) == 12 and set(products) == set(KEYS) and
         len(by_building["Building_Barrack"]) == 1 and len(by_building["Building_Fuel_Bladder"]) == 11 and
         all(not by_building[name] for name in by_building if name not in {"Building_Barrack", "Building_Fuel_Bladder"}),
         "Current metadata products do not prove exactly the 12 source roots")
    startup = provenance.get("audited_startup_roots")
    need(isinstance(startup, list) and len(startup) == 4 and
         {row.get("name") for row in startup} == STARTUP_KEYS and STARTUP_KEYS.issubset(KEYS),
         "Exact four audited startup roots must be a subset of the production roots")
    key_marker = ("[S073ProductionPrefabKeyExport] result=Passed keys=12 startup=4 constructionBuildings=6 "
                  "productionReferences=12 mission=0 readiness=0 keySha256=" + key_sha256)
    key_log_sha = validate_log(
        key_log_path, "Game.Editor.S073ProductionPrefabKeyExport.RunS073ProductionPrefabKeyExport", key_path,
        [key_path, audit_path, audit_sha256], key_marker, args.key_wrapper_exit)

    capture = read(capture_path)
    need(capture.get("schema_version") == 1 and capture.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         capture.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
         capture.get("requested_prefab_keys") == KEYS and capture.get("requested_prefab_keys_sha256") == key_sha256 and
         set(capture.get("opening_prefabs", {})) == set(KEYS) and len(capture.get("opening_prefabs", {})) == 12 and
         isinstance(capture.get("entities"), list) and len(capture["entities"]) == 518 and
         capture.get("fresh_world_allocator_exported") is False and capture.get("full_mission_initialized") is False and
         capture.get("native_visual_readiness") is False and capture.get("geometry_exported") is False and
         capture.get("replay_ready") is False,
         "Exact current source-only 12-root native production capture required")
    key_path_pin = {str(key_path): key_sha256}
    need(all(capture.get("source_pins", {}).get(path) == digest for path, digest in key_path_pin.items()),
         "Native production capture must pin the exact input key bytes")
    capture_pin_count = verify_pins(capture.get("source_pins"), repo, key_path)
    unsupported_counts = validate_unsupported(capture.get("unsupported_values"))
    need(sum(unsupported_counts.values()) == 21 and unsupported_counts == {
        "Game.Components.UnitAttackImpactVfxReference": 6,
        "Game.Components.UnitMuzzleFlashVfxReference": 4,
        "Game.Components.GroundMissileLauncherVfxReferenceComponent": 4,
        "Game.Components.AirMissileLauncherVfxReferenceComponent": 7,
    }, "Exact current 21 presentation-only references required")
    marker = ("[SkirmishS073ProductionPrefabReferenceData] result=Passed prefabs=12 entities=518 "
              "unsupportedUnconsumedPresentation=21 replayReady=False fullMission=0")
    capture_log_sha = validate_log(
        capture_log_path, "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS073ProductionPrefabReferenceData",
        capture_path.parent,
        [key_path, key_sha256, capture_path.parent], marker, args.capture_wrapper_exit)

    receipt = {
        "schema_version": 1, "kind": "S073ProductionPrefabCaptureReceipt", "validation_status": "Passed",
        "key_path": str(key_path), "key_sha256": key_sha256, "key_count": 12,
        "key_provenance_path": str(key_provenance_path), "key_provenance_sha256": sha(key_provenance_path),
        "key_log_path": str(key_log_path), "key_log_sha256": key_log_sha,
        "key_wrapper_exit_code": args.key_wrapper_exit, "key_required_marker_present": True,
        "source_audit_path": str(audit_path), "source_audit_sha256": audit_sha256,
        "key_source_pin_count": key_pin_count,
        "capture_path": str(capture_path), "capture_sha256": sha(capture_path),
        "capture_log_path": str(capture_log_path), "capture_log_sha256": capture_log_sha,
        "capture_wrapper_exit_code": args.capture_wrapper_exit, "capture_required_marker_present": True,
        "capture_source_pin_count": capture_pin_count, "opening_roots": 12, "entities": 518,
        "unsupported_presentation_reference_count": 21,
        "unsupported_presentation_fields": unsupported_counts,
        "allowed_unsupported_policy_source": "Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs FocusedUnsupportedValuesAreUnconsumed",
        "replay_ready": False, "full_mission_initialized": False, "native_visual_readiness": False,
        "mission_ready": False, "source_fit_certified": False,
        "scope": "Current 12-root authored source template closure only; four audited startup roots overlap production roots; 21 exact importer-allowlisted VFX UnityObjectRefs are unconsumed presentation data.",
    }
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    with receipt_path.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print(json.dumps({"status": "Passed", "receipt": str(receipt_path), "capture_sha256": receipt["capture_sha256"],
                      "roots": 12, "entities": 518, "unsupported_presentation": 21,
                      "replay_ready": False, "source_pins": capture_pin_count}, indent=2))


if __name__ == "__main__":
    main()
