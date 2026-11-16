#!/usr/bin/env python3
"""Build a new, fail-closed S073 source bundle from the finalized Nov 2 captures.

The r30 bundle and its seven unresolved historical DLLs remain preserved. This
rebind updates only descriptors whose current evidence was freshly qualified.
It does not claim source execution or mission readiness.
"""
import argparse
import copy
import hashlib
import json
import re
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
BASE_DIR = ROOT / "Logs/SkirmishProduction/s073-native-playing-occupancy-20261102-r3-finalized-r2"
BASE = BASE_DIR / "data-bundle-r30.json"
BASE_RECEIPT = BASE_DIR / "bundle-rebind-r30-receipt.json"
CURRENT = Path("/private/tmp/s073-final-current-20261102")
STARTUP_CAPTURE = Path("/private/tmp/s073-final-prefab-20261102-r1/prefab-reference-data.json")
STARTUP_NAMES = Path("/private/tmp/s073-final-names-20261102-r1/source-object-names.json")
STARTUP_RECEIPTS = Path("/private/tmp/s073-final-source-prefab-finalized-20261102-r1")
CONFIG_DIR = CURRENT / "native-config-readback-r1"
PRODUCTION_CAPTURE = Path("/private/tmp/s073-final-production-20261102-r1/prefab-reference-data.json")
PRODUCTION_RECEIPT = Path("/private/tmp/s073-final-production-20261102-r1/production-prefab-capture-receipt.json")
DEFINITIONS = ROOT / "Logs/SkirmishProduction/s073-final-native-legacy-definitions-20261102-r1/definitions.json"
DEFINITION_QUALIFICATION = Path("/private/tmp/s073-final-native-legacy-definitions-20261102-r1/qualification.json")
UNIT_DIR = ROOT / "Logs/SkirmishProduction/s073-final-unit-catalog-20261102-r1"
AI_DIR = Path("/private/tmp/s073-final-ai-startup-20261102-r1")
ROAD_DIR = ROOT / "Logs/SkirmishProduction/s073-current-position-road-20261102-r1"
CATALOG_DIR = ROOT / "Logs/SkirmishProduction/s073-current-catalog-metadata-20261102-current-position-r1"
WATER_DIR = ROOT / "Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2"
REPLACED = {
    "prefab_capture", "source_identity", "legacy_config", "production_prefabs",
    "building_definitions", "native_physical_road_mask", "native_physical_sidewalk_mask",
    "native_physical_road_receipt", "native_configured_catalog_receipt",
    "native_configured_unit_receipt", "native_ai_snapshot_receipt",
    "native_water_capture_receipt", "native_water_capture_snapshot",
    "native_water_capture_mask", "native_water_startup_parity_receipt",
}
EXPECTED_BUILDING_KEYS = [
    "Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
    "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder",
]
EXPECTED_HISTORY = {
    "unresolved_count": 7,
    "qualification_effect": "Source-execution qualification remains unclaimed until historical binary content is verified.",
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def read(path):
    def unique(pairs):
        out = {}
        for key, value in pairs:
            if key in out:
                raise ValueError("duplicate JSON key: " + key)
            out[key] = value
        return out
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique)


def need(value, message):
    if not value:
        raise ValueError(message)


def descriptor(path):
    path = Path(path).resolve(strict=True)
    need(path.is_file() and not path.is_symlink(), "regular evidence file required: " + str(path))
    return {"path": str(path), "sha256": sha(path)}


def verify_source_pins(pins, label, external_roots=()):
    need(isinstance(pins, dict) and pins, label + " source pin map required")
    for raw, expected in pins.items():
        path = Path(raw)
        if not path.is_absolute():
            path = ROOT / path
        path = path.resolve(strict=True)
        need(path.is_relative_to(ROOT.resolve(strict=True)) or
             any(path.is_relative_to(Path(root).resolve(strict=True)) for root in external_roots),
             label + " source pin escaped authorized roots: " + str(path))
        need(re.fullmatch(r"[0-9a-f]{64}", str(expected)) and sha(path) == expected,
             label + " source pin drift: " + str(path))
    return len(pins)


def validate_bundle(base, prior_receipt):
    need(base.get("schema_version") == 1 and base.get("kind") == "S073LegacyStartupBundle" and
         base.get("mission") == "S073" and base.get("seed") == 104729 and
         base.get("source_repository_root") == str(ROOT.resolve()), "exact S073 r30 base required")
    need(prior_receipt.get("kind") == "S073NativeWaterBlockerBundleRebindReceipt" and
         prior_receipt.get("status") == "Passed" and
         prior_receipt.get("output_bundle", {}).get("path") == str(BASE.resolve()) and
         prior_receipt.get("output_bundle", {}).get("sha256") == sha(BASE) and
         prior_receipt.get("historical_binary_limitation") == EXPECTED_HISTORY,
         "passed r30 receipt with seven unresolved historical DLLs required")
    descriptors = {}
    for key, value in base.items():
        if isinstance(value, dict) and "path" in value:
            need(set(value) == {"path", "sha256"}, "malformed r30 descriptor: " + key)
            path = Path(value["path"]).resolve(strict=True)
            need(path.is_file() and not path.is_symlink() and sha(path) == value["sha256"],
                 "r30 retained descriptor drift: " + key)
            descriptors[key] = {"path": str(path), "sha256": value["sha256"]}
    need(len(descriptors) == 30 and REPLACED <= set(descriptors), "unexpected r30 descriptor inventory")
    return descriptors


def apply_replacements(base, replacements):
    need(set(replacements) == REPLACED, "replacement descriptor set mismatch")
    bundle = copy.deepcopy(base)
    bundle.update(replacements)
    need(all(bundle[key] == value for key, value in replacements.items()), "descriptor replacement mismatch")
    need(all(bundle[key] == base[key] for key in base if key not in REPLACED),
         "unrelated r30 bundle descriptor changed")
    return bundle


def validate_current_inputs():
    config = CONFIG_DIR / "legacy-initial-spawn-data.json"
    identity = CONFIG_DIR / "source-object-names.json"
    config_audit = CONFIG_DIR / "s073-config-readback-audit.json"
    asset_audit = CURRENT / "asset-audit/source-asset-reference-audit.json"
    config_doc, identity_doc = read(config), read(identity)
    need(config_doc.get("kind") == "S073LegacyInitialSpawnExternalData" and config_doc.get("mission") == "S073",
         "fresh S073 config readback required")
    need(config_doc.get("world_initialized") is False and config_doc.get("mission_outcome_derived") is False and
         config_doc.get("native_visual_readiness") is False, "current config must remain data-only")
    artifacts = config_doc.get("source_artifacts", {})
    need(artifacts.get("config_readback_sha256") == sha(config_audit) and
         artifacts.get("name_readback_sha256") == sha(identity) and
         artifacts.get("configuration_asset_sha256", {}).get(
             "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset") ==
         sha(ROOT / "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"),
         "fresh config/identity/current asset bindings disagree")
    need(identity_doc.get("prefab_capture_sha256") == sha(STARTUP_CAPTURE) and
         identity_doc.get("source_readback_sha256") == sha(config_audit) and
         identity_doc.get("mission_world_allocated") is False and
         identity_doc.get("mission_initialized") is False and
         identity_doc.get("native_visual_readiness") is False,
         "generated current identity is not bound to the native prefab and config readback")
    verify_source_pins(identity_doc.get("source_pins"), "current source identity",
                       [CURRENT, STARTUP_CAPTURE.parent, STARTUP_NAMES.parent])
    startup_doc = read(STARTUP_CAPTURE)
    verify_source_pins(startup_doc.get("source_pins"), "startup prefab capture",
                       [CURRENT, STARTUP_CAPTURE.parent, STARTUP_NAMES.parent])
    verify_source_pins(read(STARTUP_NAMES).get("source_pins"), "native startup names",
                       [CURRENT, STARTUP_CAPTURE.parent, STARTUP_NAMES.parent])
    name_receipt, prefab_receipt = read(STARTUP_RECEIPTS / "name-readback-receipt.json"), read(
        STARTUP_RECEIPTS / "prefab-capture-receipt.json")
    need(name_receipt.get("validation_status") == prefab_receipt.get("validation_status") == "Passed" and
         name_receipt.get("prefab_capture_sha256") == sha(STARTUP_CAPTURE) and
         name_receipt.get("name_readback_sha256") == sha(STARTUP_NAMES) and
         prefab_receipt.get("capture_sha256") == sha(STARTUP_CAPTURE) and
         name_receipt.get("source_pin_count") == 36 and prefab_receipt.get("source_pin_count") == 8211,
         "strict startup prefab/name receipts do not bind exact current captures")
    need(read(asset_audit).get("kind") == "S073LegacySourceAssetReferenceAudit" and
         sha(asset_audit) == "a2c811594fbfea6a02b896082e673a8b9740e9706c3f3ce37bd6c5d7c9146613",
         "exact current 12-root asset audit required")
    production_receipt = read(PRODUCTION_RECEIPT)
    production = read(PRODUCTION_CAPTURE)
    need(production_receipt.get("validation_status") == "Passed" and
         production_receipt.get("capture_sha256") == sha(PRODUCTION_CAPTURE) and
         production_receipt.get("source_audit_sha256") == sha(asset_audit) and
         production_receipt.get("source_audit_path") == str(asset_audit.resolve()) and
         production_receipt.get("opening_roots") == 12 and production_receipt.get("entities") == 518 and
         production_receipt.get("unsupported_presentation_reference_count") == 21 and
         production_receipt.get("replay_ready") is False,
         "strict production prefab receipt/audit mismatch")
    prod_pin_count = verify_source_pins(production.get("source_pins"), "production prefab",
                                        [PRODUCTION_CAPTURE.parent, Path("/private/tmp/s073-final-production-keys-20261102-r1")])
    need(prod_pin_count == production_receipt.get("capture_source_pin_count"),
         "production prefab source pin count changed")

    def_qual = read(DEFINITION_QUALIFICATION)
    native_defs = read(DEFINITIONS)
    need(def_qual.get("capture_validation") == "Passed" and
         def_qual.get("capture_path") == str(DEFINITIONS.resolve()) and
         def_qual.get("capture_sha256") == sha(DEFINITIONS) and
         def_qual.get("full_definition_semantics_match_retained") is True and
         def_qual.get("source_pin_drift_count") == 0 and
         def_qual.get("consumer_compatibility") == "NotCompatible" and
         native_defs.get("mission_world_loaded") is False and
         native_defs.get("mission_initialized") is False and native_defs.get("native_readiness") is False,
         "strict current native definitions qualification required")
    def_pin_count = verify_source_pins(native_defs.get("source_pins"), "native definitions")
    need(def_pin_count == def_qual.get("source_pin_count") and
         def_qual.get("current_legacy_config_pins_covered") is True,
         "native definition source pins/config coverage mismatch")

    unit_path = UNIT_DIR / "unit-catalog.json"
    unit_receipt_path = UNIT_DIR / "unit-catalog-receipt.json"
    unit, unit_receipt = read(unit_path), read(unit_receipt_path)
    need(unit_receipt.get("validation_status") == "Passed" and unit_receipt.get("mission_ready") is False and
         unit_receipt.get("catalog_sha256") == sha(unit_path), "fresh unit catalog receipt required")
    unit_pin_count = verify_source_pins(unit.get("source_dependencies_sha256"), "unit catalog")
    need(unit_pin_count == unit_receipt.get("source_pin_count"), "unit catalog source pin count changed")

    ai_path, ai_receipt_path = AI_DIR / "snapshot.json", AI_DIR / "snapshot-receipt.json"
    ai, ai_receipt = read(ai_path), read(ai_receipt_path)
    need(ai_receipt.get("validation_status") == "Passed" and ai_receipt.get("mission_ready") is False and
         ai_receipt.get("snapshot_sha256") == sha(ai_path) and ai.get("normal_input_playthrough") is False and
         ai.get("player_readiness") is False, "fresh AI snapshot receipt required")
    ai_pin_count = verify_source_pins(ai.get("source_pins_sha256"), "AI snapshot")
    need(ai_pin_count == ai_receipt.get("source_pin_count"), "AI source pin count changed")

    road_receipt = read(ROAD_DIR / "road-receipt.json")
    need(road_receipt.get("validation_status") == "Passed" and road_receipt.get("placement_audit_passed") is True and
         road_receipt.get("mission_ready") is False and
         road_receipt.get("initial_forces_sha256") == sha(ROOT / "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"),
         "current-position road/placement receipt must bind current config")
    road_pin_count = verify_source_pins(road_receipt.get("source_pins"), "current-position road")
    catalog_receipt = read(CATALOG_DIR / "catalog-receipt.json")
    need(catalog_receipt.get("validation_status") == "Passed" and catalog_receipt.get("mission_ready") is False,
         "current-position configured catalog receipt required")

    water_receipt, snapshot = read(WATER_DIR / "water-receipt.json"), read(WATER_DIR / "snapshot.json")
    joint = read(WATER_DIR / "joint-receipt.json")
    need(water_receipt.get("validation_status") == "Passed" and water_receipt.get("wrapper_exit_code") == 0 and
         water_receipt.get("mask_sha256") == sha(WATER_DIR / "snapshot.water.bin") and
         water_receipt.get("mission_ready") is False and
         joint.get("evidence_validation") == "Passed" and joint.get("parity_status") == "Match" and
         joint.get("masks_match") is True and joint.get("differing_cells") == 0 and
         joint.get("session_id") == water_receipt.get("session_id") and
         joint.get("mission_ready") is False and joint.get("source_fit_certified") is False,
         "same-session current startup/playing water parity receipts required")
    need(snapshot.get("source_identity_hash") == water_receipt.get("source_identity_hash") and
         snapshot.get("source_content_hash") == water_receipt.get("source_content_hash"),
         "water snapshot/receipt source identity mismatch")
    verify_source_pins(joint.get("shared_source_pins_sha256"), "joint water parity")
    # All captured data maps must identify the same authored S073 source config.
    need(unit.get("mission") == "S073" and ai.get("map_id") == "opmap.skirmish.industrial_basin" and
         water_receipt.get("operation_map_id") == ai.get("map_id"),
         "native AI/unit catalog mission identity mismatch")
    return {
        "config": config, "identity": identity, "config_audit": config_audit,
        "asset_audit": asset_audit, "production": production, "production_receipt": production_receipt,
        "native_defs": native_defs, "def_qual": def_qual, "unit_receipt_path": unit_receipt_path,
        "ai_receipt_path": ai_receipt_path, "road_receipt": road_receipt,
        "catalog_receipt": CATALOG_DIR / "catalog-receipt.json", "water_receipt": WATER_DIR / "water-receipt.json",
        "water_snapshot": WATER_DIR / "snapshot.json", "water_mask": WATER_DIR / "snapshot.water.bin",
        "joint_receipt": WATER_DIR / "joint-receipt.json",
        "pin_counts": {"production": prod_pin_count, "definitions": def_pin_count, "unit": unit_pin_count,
                       "ai": ai_pin_count, "road": road_pin_count},
    }


def adapt_definitions(native, selected_map):
    expected = EXPECTED_BUILDING_KEYS
    keys = [row.get("key") for row in native.get("definitions", [])]
    need(keys == expected and [row.get("key") for row in native.get("building_order", [])] == expected,
         "native definition keys/order mismatch")
    text = Path(selected_map).read_text(encoding="utf-8")
    map_id = re.search(r"^\s*operationMapId:\s*(\S+)\s*$", text, re.MULTILINE)
    identity = re.search(r"^\s*sourceIdentityHash:\s*([0-9a-f]{64})\s*$", text, re.MULTILINE)
    need(map_id and map_id.group(1) == "opmap.skirmish.industrial_basin" and identity,
         "current selected map source identity unresolved")
    adapted = copy.deepcopy(native)
    adapted["requested_keys"] = keys
    adapted["selected_source"] = {
        "operation_map_id": map_id.group(1), "source_identity_hash": identity.group(1),
        "resolution": {"mode": "PresetOperationMap", "presetOperationMapPresent": True},
    }
    adapted["mission_world_allocated"] = False
    adapted["full_mission_initialized"] = False
    adapted["native_visual_readiness"] = False
    return adapted


def run(output_dir):
    output_dir = Path(output_dir).absolute()
    need(not output_dir.exists(), "new r31 output directory required")
    base, prior = read(BASE), read(BASE_RECEIPT)
    preserved = validate_bundle(base, prior)
    current = validate_current_inputs()
    output_dir.mkdir(parents=True)

    # Keep the config's semantic YAML readback and replace only its native prefab lineage.
    rebound_config = copy.deepcopy(read(current["config"]))
    rebound_config["source_artifacts"]["prefab_capture_sha256"] = sha(STARTUP_CAPTURE)
    config_path = output_dir / "legacy-initial-spawn-data.json"
    config_path.write_text(json.dumps(rebound_config, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    definitions_doc = adapt_definitions(current["native_defs"], ROOT / base["selected_map"]["path"])
    definitions_path = output_dir / "building-definitions-consumer-compatible.json"
    definitions_path.write_text(json.dumps(definitions_doc, indent=2, sort_keys=True) + "\n", encoding="utf-8")

    replacements = {
        "prefab_capture": descriptor(STARTUP_CAPTURE),
        "source_identity": descriptor(CONFIG_DIR / "source-object-names.json"),
        "legacy_config": descriptor(config_path),
        "production_prefabs": descriptor(PRODUCTION_CAPTURE),
        "building_definitions": descriptor(definitions_path),
        "native_physical_road_mask": descriptor(ROAD_DIR / "road.surface.bin"),
        "native_physical_sidewalk_mask": descriptor(ROAD_DIR / "road.sidewalk.bin"),
        "native_physical_road_receipt": descriptor(ROAD_DIR / "road-receipt.json"),
        "native_configured_catalog_receipt": descriptor(CATALOG_DIR / "catalog-receipt.json"),
        "native_configured_unit_receipt": descriptor(current["unit_receipt_path"]),
        "native_ai_snapshot_receipt": descriptor(current["ai_receipt_path"]),
        "native_water_capture_receipt": descriptor(current["water_receipt"]),
        "native_water_capture_snapshot": descriptor(current["water_snapshot"]),
        "native_water_capture_mask": descriptor(current["water_mask"]),
        "native_water_startup_parity_receipt": descriptor(current["joint_receipt"]),
    }
    bundle = apply_replacements(base, replacements)
    out_bundle = output_dir / "data-bundle-r31.json"
    out_receipt = output_dir / "bundle-rebind-r31-receipt.json"
    out_bundle.write_text(json.dumps(bundle, indent=2) + "\n", encoding="utf-8")

    receipt = {
        "kind": "S073FinalSourceBundleRebindReceipt", "schema_version": 1, "status": "Passed",
        "source_bundle": descriptor(BASE), "source_bundle_receipt": descriptor(BASE_RECEIPT),
        "output_bundle": descriptor(out_bundle), "output_config": descriptor(config_path),
        "output_building_definitions": descriptor(definitions_path),
        "replaced_descriptors": replacements, "replaced_descriptor_count": len(replacements),
        "preserved_descriptor_count": len(preserved) - len(replacements),
        "preserved_descriptors": {k: v for k, v in preserved.items() if k not in REPLACED},
        "current_source_receipts": {
            "startup_prefab_capture": descriptor(STARTUP_RECEIPTS / "prefab-capture-receipt.json"),
            "startup_name_readback": descriptor(STARTUP_RECEIPTS / "name-readback-receipt.json"),
            "production_prefab_capture": descriptor(PRODUCTION_RECEIPT),
            "native_definitions_qualification": descriptor(DEFINITION_QUALIFICATION),
            "unit_catalog": descriptor(current["unit_receipt_path"]),
            "ai_snapshot": descriptor(current["ai_receipt_path"]),
            "water_capture": descriptor(current["water_receipt"]),
            "water_joint_parity": descriptor(current["joint_receipt"]),
            "current_config_readback": descriptor(CONFIG_DIR / "s073-config-readback-audit.json"),
            "current_asset_audit": descriptor(CURRENT / "asset-audit/source-asset-reference-audit.json"),
        },
        "source_pin_counts_verified": current["pin_counts"],
        "historical_binary_limitation": EXPECTED_HISTORY,
        "source_execution_qualified": False, "mission_ready": False,
        "source_fit_certified": False, "normal_input_playthrough": False, "player_readiness": False,
        "native_capture_rewritten": False,
    }
    out_receipt.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return {"status": "Passed", "bundle": str(out_bundle), "bundle_sha256": sha(out_bundle),
            "receipt": str(out_receipt), "replaced_descriptors": len(replacements),
            "preserved_descriptors": len(preserved) - len(replacements),
            "unresolved_historical_binaries": 7, "source_execution_qualified": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(run(args.output_dir), indent=2))


if __name__ == "__main__":
    main()
