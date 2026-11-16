#!/usr/bin/env python3
"""Strictly qualify a native S073 legacy definition capture without rewriting it."""
import argparse
import hashlib
import json
import re
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
EXPECTED_KEYS = [
    "Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
    "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder",
]
PASS_MARKER = (
    "[S073LegacyAuthoredBuildingDefinitions] result=Passed definitions=6 "
    "unsupported=0 recipeOverlay=0 mission=0 readiness=0"
)
EXPECTED_CONFIG_PINS = {
    "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset",
    "Assets/Game/Configs/Skirmish/UnitRegistry.asset",
}


def parse_json(path):
    def unique(pairs):
        value = {}
        for key, item in pairs:
            if key in value:
                raise ValueError("duplicate JSON key: " + key)
            value[key] = item
        return value

    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def consumer_gaps(document):
    required = {
        "requested_keys": EXPECTED_KEYS,
        "mission_world_allocated": False,
        "full_mission_initialized": False,
        "native_visual_readiness": False,
    }
    gaps = []
    for key, expected in required.items():
        if document.get(key) != expected:
            gaps.append({"field": key, "expected": expected, "actual": document.get(key)})
    selected = document.get("selected_source")
    if not isinstance(selected, dict):
        gaps.append({"field": "selected_source", "expected": "S073 operation map identity", "actual": None})
    else:
        for key, expected in {
            "operation_map_id": "opmap.skirmish.industrial_basin",
            "source_identity_hash": "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900",
        }.items():
            if selected.get(key) != expected:
                gaps.append({"field": "selected_source." + key, "expected": expected, "actual": selected.get(key)})
    return gaps


def validate(repo, capture_path, retained_path, config_path, log_path, wrapper_exit):
    repo = Path(repo).resolve(strict=True)
    capture_path = Path(capture_path).resolve(strict=True)
    retained_path = Path(retained_path).resolve(strict=True)
    config_path = Path(config_path).resolve(strict=True)
    log_path = Path(log_path).resolve(strict=True)
    need(repo == ROOT.resolve(strict=True), "exact repository root required")
    need(wrapper_exit == 0, "native wrapper exit code must be zero")
    capture = parse_json(capture_path)
    retained = parse_json(retained_path)
    config = parse_json(config_path)
    log = log_path.read_text(encoding="utf-8")
    need(PASS_MARKER in log, "exact native exporter pass marker missing")
    need(capture.get("schema_version") == 1 and
         capture.get("kind") == "S073LegacyAuthoredBuildingDefinitions" and
         capture.get("authority") == "OriginalLegacyPresetConstruction_NoRuntimeRecipeOverlay" and
         capture.get("catalog_id") == "S073" and
         capture.get("preset_path") == "Assets/Game/Resources/SkirmishIndustrialBasin.asset" and
         capture.get("building_catalog") == "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset" and
         capture.get("unit_catalog") == "Assets/Game/Configs/Skirmish/UnitRegistry.asset" and
         capture.get("factory") == "BuildingDefinitionPrefabSystemHelper.CreateDefinition" and
         capture.get("production_recipe_overlay_configured") is False and
         capture.get("mission_world_loaded") is False and capture.get("mission_initialized") is False and
         capture.get("native_readiness") is False,
         "native S073 no-overlay source-only capture schema/authority mismatch")
    need(capture.get("unsupported_values") == [], "unsupported definition values are present")
    new_rows = capture.get("definitions")
    old_rows = retained.get("definitions")
    need(isinstance(new_rows, list) and isinstance(old_rows, list) and len(new_rows) == 6 and len(old_rows) == 6,
         "exact six-definition old/new domains required")
    need([row.get("key") for row in new_rows] == EXPECTED_KEYS and
         [row.get("key") for row in old_rows] == EXPECTED_KEYS,
         "ordered S073 definition keys differ")
    semantic_differences = []
    for current, prior in zip(new_rows, old_rows):
        for field in ("key", "prefab", "definition_type", "factory", "fields"):
            if current.get(field) != prior.get(field):
                semantic_differences.append({"key": current.get("key"), "field": field})
        need(len(current.get("fields", {})) == 55, "full 55-field definition required: " + current["key"])
    need(not semantic_differences, "native definition semantics differ from retained evidence")

    pins = capture.get("source_pins")
    need(isinstance(pins, dict) and pins, "nonempty native source pin set required")
    pin_errors = []
    for raw_path, expected in pins.items():
        path = Path(raw_path)
        if not path.is_absolute():
            path = repo / path
        if not re.fullmatch(r"[a-f0-9]{64}", str(expected)):
            pin_errors.append({"path": raw_path, "reason": "invalid SHA-256"})
        elif not path.is_file():
            pin_errors.append({"path": raw_path, "reason": "missing"})
        elif sha(path) != expected:
            pin_errors.append({"path": raw_path, "reason": "hash drift", "current_sha256": sha(path)})
    need(not pin_errors, "source pins missing or drifted: " + str(len(pin_errors)))

    config_pins = config.get("source_artifacts", {}).get("configuration_asset_sha256", {})
    need(isinstance(config_pins, dict) and EXPECTED_CONFIG_PINS.issubset(config_pins),
         "current legacy configuration asset pins required")
    config_pin_gaps = [path for path, digest in config_pins.items() if pins.get(path) != digest]
    need(not config_pin_gaps, "native capture does not pin all current legacy config assets")

    gaps = consumer_gaps(capture)
    return {
        "kind": "S073NativeLegacyBuildingDefinitionsQualification",
        "schema_version": 1,
        "capture_validation": "Passed",
        "capture_path": str(capture_path),
        "capture_sha256": sha(capture_path),
        "retained_path": str(retained_path),
        "retained_sha256": sha(retained_path),
        "wrapper_exit_code": wrapper_exit,
        "pass_marker": PASS_MARKER,
        "definition_count": len(new_rows),
        "fields_per_definition": 55,
        "full_definition_semantics_match_retained": True,
        "unsupported_values": 0,
        "source_pin_count": len(pins),
        "source_pin_drift_count": 0,
        "current_legacy_config_pin_count": len(config_pins),
        "current_legacy_config_pins_covered": True,
        "consumer_compatibility": "NotCompatible" if gaps else "Compatible",
        "consumer_gaps": gaps,
        "artifact_rewritten": False,
        "mission_initialized": False,
        "mission_ready": False,
        "native_readiness": False,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--retained", type=Path, default=ROOT / "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/building-definitions.json")
    parser.add_argument("--legacy-config", type=Path, required=True)
    parser.add_argument("--editor-log", type=Path, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    args = parser.parse_args()
    receipt_path = args.receipt.absolute()
    need(not receipt_path.exists(), "qualification receipt must be create-new")
    report = validate(args.repo, args.capture, args.retained, args.legacy_config, args.editor_log, args.wrapper_exit)
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    with receipt_path.open("x", encoding="utf-8") as stream:
        json.dump(report, stream, indent=2)
        stream.write("\n")
    print(json.dumps(report, indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as error:
        print("[S073NativeLegacyDefinitionsQualification] result=Failed " + str(error), file=sys.stderr)
        sys.exit(1)
