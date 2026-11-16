#!/usr/bin/env python3
"""Prepare a strict S073 r22 input bundle while preserving evidence lineage."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/data-bundle-native-ai-water-road-production-source-refreshed-r20.json"
BASE_SHA = "8d56540949246b0be5b926cdf72cd106f9f39d16806d5b72af1fae8b917184af"
BASE_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/production-bridge-bundle-rebind-r20.json"
R21 = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r2/legacy-initial-spawn-data-native-r21.json"
R22 = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r2/legacy-initial-spawn-data-native-r22.json"
AUDIT = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r2/r20-nested-source-pin-drift-audit.json"
PREFAB = Path("/private/tmp/s073-current-offset-prefab-20261101-r2/prefab-reference-data.json")
IDENTITY = Path("/private/tmp/s073-current-offset-names-20261101-r2/source-object-names.json")
PREFAB_RECEIPT = PREFAB.parent / "prefab-capture-receipt.json"
IDENTITY_RECEIPT = PREFAB.parent / "name-readback-receipt.json"
PRODUCTION = Path("/private/tmp/s073-current-offset-production-20261101-r2/prefab-reference-data.json")
PRODUCTION_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r2/production-prefab-capture-receipt-r3.json"
CATALOG_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-current-catalog-metadata-20261101-r1/catalog-receipt.json"
ROAD = ROOT / "Logs/SkirmishProduction/s073-current-offset-road-capture-20261101-r4"
REPLACED = {
    "prefab_capture", "source_identity", "legacy_config", "production_prefabs",
    "native_physical_road_mask", "native_physical_sidewalk_mask", "native_physical_road_receipt",
    "native_configured_catalog_receipt",
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def need(ok, message):
    if not ok:
        raise ValueError(message)


def desc(path):
    path = Path(path).resolve(strict=True)
    need(path.is_file() and not path.is_symlink(), "Regular evidence file required: " + str(path))
    return {"path": str(path), "sha256": sha(path)}


def run(repo, output, receipt_output):
    repo = Path(repo).resolve(strict=True)
    output, receipt_output = Path(output).absolute(), Path(receipt_output).absolute()
    need(repo == ROOT.resolve(strict=True), "Exact repository root required")
    need(sha(BASE) == BASE_SHA, "Reviewed r20 base bundle bytes changed")
    need(not output.exists() and not receipt_output.exists(), "Fresh r22 output paths required")
    base, prior = read(BASE), read(BASE_RECEIPT)
    need(prior.get("validation_status") == "Passed" and prior.get("output_bundle_path") == str(BASE.resolve())
         and prior.get("output_bundle_sha256") == BASE_SHA, "Qualified r20 bundle receipt required")
    need(base.get("kind") == "S073LegacyStartupBundle" and base.get("mission") == "S073"
         and base.get("seed") == 104729, "Exact S073 r20 bundle required")

    r21, r22 = read(R21), read(R22)
    need(r21.get("kind") == r22.get("kind") == "S073LegacyInitialSpawnExternalData"
         and r21.get("mission") == r22.get("mission") == "S073", "Current S073 configs required")
    for field in ("initial_spawn", "captured_unit_prefab_roots", "preset_values"):
        need(r21.get(field) == r22.get(field), "r21/r22 gameplay source values differ: " + field)
    r21_artifacts, r22_artifacts = r21["source_artifacts"], r22["source_artifacts"]
    need(r21_artifacts.get("configuration_asset_sha256") == r22_artifacts.get("configuration_asset_sha256"),
         "r21/r22 InitialForces and preset source hashes differ")
    initial_path = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
    initial_sha = r22_artifacts["configuration_asset_sha256"].get(initial_path)
    need(isinstance(initial_sha, str) and len(initial_sha) == 64 and sha(ROOT / initial_path) == initial_sha,
         "Current InitialForces bytes must bind both configs")

    prefab, identity, production = read(PREFAB), read(IDENTITY), read(PRODUCTION)
    prefab_receipt, identity_receipt = read(PREFAB_RECEIPT), read(IDENTITY_RECEIPT)
    production_receipt = read(PRODUCTION_RECEIPT)
    need(prefab_receipt.get("validation_status") == "Passed" and prefab_receipt.get("wrapper_exit_code") == 0
         and prefab_receipt.get("required_marker_present") is True
         and prefab_receipt.get("capture_sha256") == sha(PREFAB), "Fresh qualified r2 startup prefab receipt required")
    need(identity_receipt.get("validation_status") == "Passed" and identity_receipt.get("wrapper_exit_code") == 0
         and identity_receipt.get("required_marker_present") is True
         and identity_receipt.get("name_readback_sha256") == sha(IDENTITY)
         and identity.get("prefab_capture_sha256") == sha(PREFAB), "Fresh qualified r2 identity receipt required")
    need(r22_artifacts.get("prefab_capture_sha256") == sha(PREFAB)
         and r22_artifacts.get("name_readback_sha256") == sha(IDENTITY)
         and r22.get("captured_unit_prefab_roots") == prefab.get("opening_prefabs"),
         "r22 config must bind the exact r2 prefab/name inputs")
    need(production_receipt.get("kind") == "S073ProductionPrefabCaptureReceipt"
         and production_receipt.get("validation_status") == "Passed"
         and production_receipt.get("capture_path") == str(PRODUCTION.resolve())
         and production_receipt.get("capture_sha256") == sha(PRODUCTION)
         and production_receipt.get("key_count") == 12 and production_receipt.get("opening_roots") == 12
         and production_receipt.get("entities") == 518
         and production_receipt.get("unsupported_presentation_reference_count") == 21
         and production_receipt.get("mission_ready") is False,
         "Fresh qualified r3 production template receipt required")

    road_receipt_path = ROAD / "road-receipt.json"
    road_receipt = read(road_receipt_path)
    road_config_path = Path(road_receipt.get("legacy_config_path", ""))
    need(road_receipt.get("kind") == "S073PhysicalRoadMaskCaptureReceipt"
         and road_receipt.get("validation_status") == "Passed"
         and road_receipt.get("wrapper_exit_code") == 0
         and road_receipt.get("placement_audit_passed") is True
         and road_receipt.get("mission_ready") is False
         and road_config_path.resolve(strict=True) == R21.resolve(strict=True)
         and road_receipt.get("legacy_config_sha256") == sha(R21),
         "Preserved qualified road r4 receipt must retain its original r21 config binding")
    need(road_receipt.get("initial_forces_sha256") == initial_sha,
         "Road r4 and r22 must share the exact current InitialForces bytes")
    catalog_receipt = read(CATALOG_RECEIPT)
    need(road_receipt.get("native_catalog_receipt_path") == str(CATALOG_RECEIPT.resolve())
         and road_receipt.get("native_catalog_receipt_sha256") == sha(CATALOG_RECEIPT)
         and catalog_receipt.get("validation_status") == "Passed"
         and catalog_receipt.get("mission_ready") is False,
         "Road r4 must bind the exact current qualified native catalog receipt")
    road_finalizer_path = Path(__file__).with_name("finalize_s073_current_road_capture.py")
    import importlib.util
    spec = importlib.util.spec_from_file_location("s073_current_road_finalizer", road_finalizer_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    catalog = read(Path(catalog_receipt["catalog_path"]))
    post = module.validate((ROAD / "editor.log").read_text(encoding="utf-8"), read(ROAD / "census.json"),
                           (ROAD / "road.surface.bin").read_bytes(), (ROAD / "road.sidewalk.bin").read_bytes(),
                           0, r22, catalog, (ROAD / "placement-audit.txt").read_bytes(),
                           str((ROAD / "placement-audit.txt").resolve()), True)
    need(post.get("phase") == "Playing" and post.get("entity_count") == 4931,
         "Current road r4 census must remain same-session Playing census")

    # The r20 audit records only old source-pin drift. Replacements above clear
    # its stale startup/config/production/road groups. Preserve the unrecovered
    # campaign source and old building-definition drift as explicit caveats.
    audit = read(AUDIT)
    unresolved = [row for row in audit.get("groups", [])
                  if row.get("descriptor") in {"campaign_physical", "building_definitions"}]
    need({row.get("descriptor") for row in unresolved} == {"campaign_physical", "building_definitions"}
         and sum(row.get("stale_or_missing_count", 0) for row in unresolved) == 73,
         "Expected retained external campaign and stale building-definition caveats changed")
    need(all(row.get("descriptor") not in {"campaign_physical", "building_definitions"}
             for row in audit.get("groups", []) if row.get("descriptor") in REPLACED),
         "A replaced descriptor unexpectedly remains in stale audit groups")

    descriptors = {name: value for name, value in base.items()
                   if isinstance(value, dict) and "path" in value and "sha256" in value}
    for name, value in descriptors.items():
        path = Path(value["path"]).resolve(strict=True)
        need(path.is_file() and not path.is_symlink() and sha(path) == value["sha256"],
             "r20 retained descriptor drift: " + name)
    new = {
        "prefab_capture": desc(PREFAB), "source_identity": desc(IDENTITY), "legacy_config": desc(R22),
        "production_prefabs": desc(PRODUCTION),
        "native_physical_road_mask": desc(ROAD / "road.surface.bin"),
        "native_physical_sidewalk_mask": desc(ROAD / "road.sidewalk.bin"),
        "native_physical_road_receipt": desc(road_receipt_path),
        "native_configured_catalog_receipt": desc(CATALOG_RECEIPT),
    }
    rebound = copy.deepcopy(base)
    rebound.update(new)
    for name, value in base.items():
        if name not in new:
            need(rebound[name] == value, "Unexpected non-descriptor bundle change: " + name)
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")
    need(read(output) == rebound, "Written r22 bundle differs from validated data")

    caveats = [{"descriptor": row["descriptor"], "stale_or_missing_source_pin_count": row["stale_or_missing_count"],
                "source_pin_count": row["unique_source_pin_count"], "drift": row["drift"]}
               for row in unresolved]
    receipt = {
        "kind": "S073CurrentOffsetsBundleRebindReceipt", "schema_version": 1,
        "validation_status": "Passed", "source_bundle_path": str(BASE.resolve()),
        "source_bundle_sha256": BASE_SHA, "output_bundle_path": str(output.resolve()),
        "output_bundle_sha256": sha(output), "replaced_descriptors": new,
        "preserved_descriptor_count": len(descriptors) - len(new),
        "preserved_descriptor_names": sorted(name for name in descriptors if name not in new),
        "road_r4_receipt_preserved_without_mutation": True,
        "road_r4_receipt_path": str(road_receipt_path.resolve()),
        "road_r4_receipt_sha256": sha(road_receipt_path),
        "road_r4_original_config_binding": {"path": str(R21.resolve()), "sha256": sha(R21)},
        "r21_r22_initial_spawn_equal": True, "initial_forces_sha256": initial_sha,
        "mission_ready": False, "source_fit_certified": False,
        "normal_input_playthrough": False, "player_readiness": False,
        "retained_nested_source_pin_caveats": caveats,
        "retained_nested_source_pin_caveat_count": 73,
        "source_execution_qualified": False,
        "source_execution_gate": "Retained campaign physical capture has 37 missing external source pins; retained legacy building definitions have 36 stale source pins."
    }
    receipt_output.parent.mkdir(parents=True, exist_ok=True)
    with receipt_output.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2, sort_keys=True)
        stream.write("\n")
    return {"status": "Passed", "bundle": str(output.resolve()), "bundle_sha256": sha(output),
            "receipt": str(receipt_output.resolve()), "replaced_descriptors": sorted(new),
            "preserved_descriptor_count": len(descriptors) - len(new),
            "retained_nested_caveats": 73, "source_execution_qualified": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--receipt-output", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(run(args.repo, args.output, args.receipt_output), indent=2))


if __name__ == "__main__":
    main()
