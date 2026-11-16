#!/usr/bin/env python3
"""Refresh only current-qualified S073 source and native road bundle inputs.

The retained 16-root production prefab descriptor is preserved as-is and its
source-pin drift is recorded as an unresolved prerequisite. This tool never
substitutes the four-root startup capture for that production closure.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SOURCE_BUNDLE = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/data-bundle-native-ai-water-refreshed-r18.json"
SOURCE_BUNDLE_SHA = "0bdd327bbe96483e70ac01e5636dbe807e92a40541c86ebf9a7d3475402d4a1a"
FRESH_ROOT = Path("/private/tmp/s073-fresh-20261101")
ROAD_DIRECTORY = ROOT / "Logs/SkirmishProduction/s073-current-road-capture-20261101-r5"
REPLACE = {
    "prefab_capture", "source_identity", "legacy_config", "building_definitions",
    "native_physical_road_mask", "native_physical_sidewalk_mask", "native_physical_road_receipt",
}
PRODUCTION_ROOTS = {
    "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_APC_Fast", "Unit_Veh_APC_Heavy",
    "Unit_Veh_APC_Slow", "Unit_Veh_Light_Armored_Car", "Unit_Veh_Missle_Launcher_Air",
    "Unit_Veh_Missle_Launcher_Ground", "Unit_Veh_Radar_Tank", "Unit_Veh_Tank_USA",
    "Unit_Veh_Truck_Canopy", "Unit_Veh_Truck_Tanker", "Unit_Veh_Truck_Tray",
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": sha(path)}


def load_module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def production_pin_drift(path):
    document = json.loads(Path(path).read_text(encoding="utf-8"))
    roots = document.get("opening_prefabs")
    if (document.get("kind") != "AuthoredSkirmishPrefabReferenceData" or
            document.get("authority") != "ActualSourceAssetsAndNativeBakers_NotMissionState" or
            not isinstance(roots, dict) or len(roots) != 16 or not PRODUCTION_ROOTS.issubset(roots)):
        raise ValueError("Retained exact 16-root production template closure required")
    drift = []
    for raw, expected in document.get("source_pins", {}).items():
        path = Path(raw)
        if not path.is_absolute():
            path = ROOT / path
        if not path.is_file():
            current = None
            kind = "external_capture_artifact" if str(path).startswith("/private/tmp/") else "missing_source"
        else:
            current = sha(path)
            kind = "source_code" if path.suffix in {".cs", ".asmdef"} else "authored_or_scene_asset"
        if current != expected:
            drift.append({"path": str(path), "expected_sha256": expected,
                          "current_sha256": current, "dependency_kind": kind})
    return drift


def rebind(repo, output, receipt_output):
    repo = Path(repo).resolve(strict=True)
    source = SOURCE_BUNDLE.resolve(strict=True)
    fresh = FRESH_ROOT.resolve(strict=True)
    road = ROAD_DIRECTORY.resolve(strict=True)
    output = Path(output).absolute()
    receipt_output = Path(receipt_output).absolute()
    if source != SOURCE_BUNDLE or sha(source) != SOURCE_BUNDLE_SHA:
        raise ValueError("Exact retained r18 source bundle required")
    if repo != ROOT.resolve(strict=True) or output.exists() or receipt_output.exists():
        raise ValueError("Exact repository and fresh output/receipt paths required")

    bundle = json.loads(source.read_text(encoding="utf-8"))
    if (bundle.get("schema_version") != 1 or bundle.get("kind") != "S073LegacyStartupBundle" or
            bundle.get("mission") != "S073" or bundle.get("seed") != 104729 or not REPLACE.issubset(bundle)):
        raise ValueError("Exact r18 S073 bundle with all road/source descriptors required")

    current = {
        "prefab_capture": fresh / "prefab-reference/prefab-reference-data.json",
        "source_identity": fresh / "name-readback/source-object-names.json",
        "legacy_config": repo / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/legacy-initial-spawn-data-native-r19.json",
        "building_definitions": fresh / "config-readback/building-definitions.json",
        "native_physical_road_mask": road / "road.surface.bin",
        "native_physical_sidewalk_mask": road / "road.sidewalk.bin",
        "native_physical_road_receipt": road / "road-receipt.json",
    }
    for name, path in current.items():
        if not path.is_file():
            raise ValueError("Missing exact current evidence for " + name + ": " + str(path))

    capture_receipt = json.loads((fresh / "prefab-capture-receipt.json").read_text())
    name_receipt = json.loads((fresh / "name-readback-receipt.json").read_text())
    capture = json.loads(current["prefab_capture"].read_text())
    identity = json.loads(current["source_identity"].read_text())
    legacy = json.loads(current["legacy_config"].read_text())
    definitions = json.loads(current["building_definitions"].read_text())
    if (capture_receipt.get("validation_status") != "Passed" or capture_receipt.get("wrapper_exit_code") != 0 or
            capture_receipt.get("required_marker_present") is not True or
            capture_receipt.get("capture_sha256") != sha(current["prefab_capture"]) or
            name_receipt.get("validation_status") != "Passed" or name_receipt.get("wrapper_exit_code") != 0 or
            name_receipt.get("required_marker_present") is not True or
            name_receipt.get("name_readback_sha256") != sha(current["source_identity"]) or
            identity.get("prefab_capture_sha256") != sha(current["prefab_capture"]) or
            legacy.get("source_artifacts", {}).get("prefab_capture_sha256") != sha(current["prefab_capture"]) or
            legacy.get("source_artifacts", {}).get("name_readback_sha256") != sha(current["source_identity"]) or
            definitions.get("kind") != "S073LegacyAuthoredBuildingDefinitions"):
        raise ValueError("Fresh, hash-bound source prefab/name/config readbacks required")

    road_finalizer = load_module(Path(__file__).with_name("finalize_s073_current_road_capture.py"), "s073_road_finalizer")
    road_result = road_finalizer.validate(
        (road / "editor.log").read_text(encoding="utf-8"),
        json.loads((road / "census.json").read_text(encoding="utf-8")),
        (road / "road.surface.bin").read_bytes(),
        (road / "road.sidewalk.bin").read_bytes(), 0)
    road_receipt = json.loads(current["native_physical_road_receipt"].read_text())
    if (road_receipt.get("validation_status") != "Passed" or road_receipt.get("wrapper_exit_code") != 0 or
            road_receipt.get("surface_mask_sha256") != sha(current["native_physical_road_mask"]) or
            road_receipt.get("sidewalk_mask_sha256") != sha(current["native_physical_sidewalk_mask"]) or
            road_result.get("entity_count") != 4931):
        raise ValueError("Exact strict current native road capture/finalizer required")

    # Validate every descriptor in the source bundle before preserving/replacing.
    preserved = {}
    for name, value in bundle.items():
        if not isinstance(value, dict) or "path" not in value:
            continue
        path = Path(value["path"]).resolve(strict=True)
        if not path.is_file() or sha(path) != value.get("sha256"):
            raise ValueError("Retained r18 descriptor changed before rebind: " + name)
        if name not in REPLACE:
            preserved[name] = value["sha256"]

    production = Path(bundle["production_prefabs"]["path"]).resolve(strict=True)
    production_drift = production_pin_drift(production)
    stale_code = [row for row in production_drift if row["dependency_kind"] == "source_code"]
    if len(stale_code) != 34 or len(production_drift) != 35:
        raise ValueError("Retained production-template source-pin audit changed; re-audit before proceeding")

    rebound = copy.deepcopy(bundle)
    rebound.update({name: descriptor(path) for name, path in current.items()})
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")

    receipt = {
        "kind": "S073ProductionBridgeBundleRebindReceipt", "schema_version": 1,
        "validation_status": "Passed", "source_bundle_path": str(source),
        "source_bundle_sha256": SOURCE_BUNDLE_SHA, "output_bundle_path": str(output.resolve()),
        "output_bundle_sha256": sha(output), "replaced_descriptors": {name: descriptor(path) for name, path in current.items()},
        "preserved_descriptor_count": len(preserved), "preserved_descriptors_sha256": preserved,
        "native_road_capture_status": "Passed", "native_road_capture_entity_count": 4931,
        "startup_prefab_capture": {"sha256": sha(current["prefab_capture"]), "opening_roots": 4,
                                   "entities": 140, "unsupported_unconsumed_presentation": 4,
                                   "replay_ready": False, "full_mission": False},
        "production_prefab_closure": {"path": str(production), "sha256": sha(production),
                                      "opening_roots": 16, "stale_source_code_pin_count": len(stale_code),
                                      "stale_pins": production_drift,
                                      "recapture_required": True,
                                      "scope": "Retained 16-root production closure preserved byte-for-byte; not current-qualified"},
        "mission_ready": False, "source_fit_certified": False,
        "normal_input_playthrough": False, "player_readiness": False,
    }
    receipt_output.parent.mkdir(parents=True, exist_ok=True)
    with receipt_output.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    return {"status": "Passed", "bundle": str(output.resolve()), "bundle_sha256": sha(output),
            "preserved_descriptor_count": len(preserved), "replaced_descriptor_count": len(current),
            "native_road_capture": "Passed", "production_template_stale_source_pins": len(stale_code),
            "production_template_recapture_required": True}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", required=True, type=Path)
    p.add_argument("--output", required=True, type=Path)
    p.add_argument("--receipt-output", required=True, type=Path)
    a = p.parse_args()
    print(json.dumps(rebind(a.repo, a.output, a.receipt_output), indent=2))


if __name__ == "__main__":
    main()
