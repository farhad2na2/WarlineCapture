#!/usr/bin/env python3
"""Replace only the retired S073 production prefab descriptor with its current native capture."""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
BASE_BUNDLE = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/data-bundle-native-ai-water-road-source-refreshed-r19.json"
BASE_BUNDLE_SHA256 = "9ad2fdb97b5f7d6f096c178c57e80d221d0154f55ff76d78e687862d5223303c"
BASE_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/production-bridge-bundle-rebind-r19.json"
FINAL_RECEIPT = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/production-prefab-capture-receipt-r1.json"
LEGACY_CONFIG = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/legacy-initial-spawn-data-native-r20.json"
AUDIT_PATH = Path("/private/tmp/s073-fresh-20261101/asset-audit/source-asset-reference-audit.json")
YAML_READBACK_PATH = Path("/private/tmp/s073-fresh-20261101/asset-audit/current-yaml-readback.json")
CONFIG_AUDIT_PATH = Path("/private/tmp/s073-fresh-20261101/config-readback/s073-config-readback-audit.json")
NAME_READBACK_PATH = Path("/private/tmp/s073-fresh-20261101/name-readback/source-object-names.json")
STARTUP_CAPTURE_PATH = Path("/private/tmp/s073-fresh-20261101/prefab-reference/prefab-reference-data.json")
STARTUP_CAPTURE_RECEIPT = Path("/private/tmp/s073-fresh-20261101/prefab-capture-receipt.json")
NAME_READBACK_RECEIPT = Path("/private/tmp/s073-fresh-20261101/name-readback-receipt.json")
CAPTURE = Path("/private/tmp/s073-production-prefabs-20261101-r1/prefab-reference-data.json")
RETIRED_CAPTURE = ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/production-prefab-reference-data.json"
EXPECTED_STALE_CODE_PINS = 34
EXPECTED_RETIRED_STALE_PINS = 35


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


def load_old_drift_auditor():
    path = Path(__file__).with_name("rebind_s073_production_bridge_bundle.py")
    spec = importlib.util.spec_from_file_location("old_s073_production_drift_audit", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module.production_pin_drift


def descriptor(path):
    path = Path(path).resolve(strict=True)
    return {"path": str(path), "sha256": sha(path)}


def rebind(output_path, receipt_path):
    output_path, receipt_path = Path(output_path).absolute(), Path(receipt_path).absolute()
    need(ROOT.resolve(strict=True) == Path(__file__).resolve().parents[4], "Exact project root required")
    need(sha(BASE_BUNDLE) == BASE_BUNDLE_SHA256, "Exact reviewed r19 bundle required")
    need(not output_path.exists() and not receipt_path.exists(), "Fresh r20 output and receipt paths required")
    need(FINAL_RECEIPT.is_file(), "Strict fresh S073 production prefab receipt required")

    base = read(BASE_BUNDLE)
    prior_receipt = read(BASE_RECEIPT)
    current_receipt = read(FINAL_RECEIPT)
    legacy = read(LEGACY_CONFIG)
    source_audit = read(AUDIT_PATH)
    name_readback = read(NAME_READBACK_PATH)
    startup_capture = read(STARTUP_CAPTURE_PATH)
    startup_capture_receipt = read(STARTUP_CAPTURE_RECEIPT)
    name_readback_receipt = read(NAME_READBACK_RECEIPT)
    need(base.get("kind") == "S073LegacyStartupBundle" and base.get("schema_version") == 1 and
         base.get("mission") == "S073" and base.get("seed") == 104729 and
         prior_receipt.get("validation_status") == "Passed" and
         prior_receipt.get("output_bundle_path") == str(BASE_BUNDLE.resolve()) and
         prior_receipt.get("output_bundle_sha256") == BASE_BUNDLE_SHA256,
         "Exact r19 base bundle and prior rebind receipt required")
    need(current_receipt.get("kind") == "S073ProductionPrefabCaptureReceipt" and
         current_receipt.get("validation_status") == "Passed" and
         current_receipt.get("capture_path") == str(CAPTURE) and
         current_receipt.get("capture_sha256") == sha(CAPTURE) and
         current_receipt.get("key_count") == 12 and current_receipt.get("opening_roots") == 12 and
         current_receipt.get("entities") == 518 and
         current_receipt.get("unsupported_presentation_reference_count") == 21 and
         current_receipt.get("replay_ready") is False and current_receipt.get("mission_ready") is False,
         "Current strict 12-root production capture receipt required")

    artifacts = legacy.get("source_artifacts", {})
    source_yaml_sha = source_audit.get("readback_artifact_sha256")
    audited_configuration = {row["path"]: row["sha256"] for row in source_audit.get("configuration_roots", [])}
    config_configuration = artifacts.get("configuration_asset_sha256", {})
    need(legacy.get("kind") == "S073LegacyInitialSpawnExternalData" and legacy.get("mission") == "S073" and
         artifacts.get("source_audit_sha256") == sha(AUDIT_PATH) and
         artifacts.get("source_yaml_readback_sha256") == source_yaml_sha == sha(YAML_READBACK_PATH) and
         name_readback.get("source_readback_sha256") == source_yaml_sha and
         name_readback.get("prefab_capture_sha256") == artifacts.get("prefab_capture_sha256") and
         artifacts.get("name_readback_sha256") == sha(NAME_READBACK_PATH) and
         name_readback.get("source_pins", {}).get(str(AUDIT_PATH)) == sha(AUDIT_PATH) and
         artifacts.get("prefab_capture_sha256") == sha(STARTUP_CAPTURE_PATH) and
         artifacts.get("prefab_capture_sha256") == startup_capture_receipt.get("capture_sha256") and
         startup_capture_receipt.get("validation_status") == "Passed" and
         startup_capture_receipt.get("wrapper_exit_code") == 0 and
         startup_capture_receipt.get("required_marker_present") is True and
         name_readback_receipt.get("name_readback_sha256") == sha(NAME_READBACK_PATH) and
         name_readback_receipt.get("validation_status") == "Passed" and
         name_readback_receipt.get("wrapper_exit_code") == 0 and
         name_readback_receipt.get("required_marker_present") is True and
         legacy.get("captured_unit_prefab_roots") == startup_capture.get("opening_prefabs") and
         artifacts.get("config_readback_sha256") == sha(CONFIG_AUDIT_PATH) and
         len(config_configuration) == 2 and all(audited_configuration.get(path) == digest
                                                 for path, digest in config_configuration.items()) and
         legacy.get("world_initialized") is False and
         legacy.get("mission_outcome_derived") is False and legacy.get("native_visual_readiness") is False,
         "Fresh legacy config must preserve distinct YAML readback and exact source-audit lineage")

    # Recheck every descriptor in r19 before rebinding. Only the production capture
    # slot is allowed to change; all other inputs remain byte-for-byte identical.
    descriptors = {name: value for name, value in base.items()
                   if isinstance(value, dict) and "path" in value and "sha256" in value}
    need("production_prefabs" in descriptors and descriptors["production_prefabs"]["path"] == str(RETIRED_CAPTURE),
         "Exact retired production-prefab descriptor required")
    for name, value in descriptors.items():
        path = Path(value["path"]).resolve(strict=True)
        need(sha(path) == value["sha256"], "r19 descriptor drift before rebind: " + name)

    drift = load_old_drift_auditor()(RETIRED_CAPTURE)
    stale_code = [row for row in drift if row["dependency_kind"] == "source_code"]
    stale_missing = [row for row in drift if row["current_sha256"] is None]
    need(len(stale_code) == EXPECTED_STALE_CODE_PINS and len(drift) == EXPECTED_RETIRED_STALE_PINS and
         len(stale_missing) == 1 and
         stale_missing[0]["path"] == "/private/tmp/s073-focused-prefab-keys-20261030-r1.json",
         "Retired r18 production-capture drift audit changed; preserve and investigate")

    rebound = copy.deepcopy(base)
    new_descriptors = {"production_prefabs": descriptor(CAPTURE), "legacy_config": descriptor(LEGACY_CONFIG)}
    rebound.update(new_descriptors)
    for name, value in base.items():
        if name not in new_descriptors:
            need(rebound[name] == value, "Unexpected change outside production_prefabs descriptor: " + name)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")

    written = read(output_path)
    need(written == rebound, "Written r20 bundle differs from the validated rebound object")
    for name, value in descriptors.items():
        if name not in new_descriptors:
            need(written[name] == value, "Preserved r19 descriptor changed in r20: " + name)
    need(written["legacy_config"] == new_descriptors["legacy_config"] and
         written["production_prefabs"] == new_descriptors["production_prefabs"],
         "Current legacy config and production prefab descriptors differ from receipts")
    receipt = {
        "schema_version": 1,
        "kind": "S073ProductionPrefabBundleRebindReceipt",
        "validation_status": "Passed",
        "source_bundle_path": str(BASE_BUNDLE.resolve()),
        "source_bundle_sha256": BASE_BUNDLE_SHA256,
        "output_bundle_path": str(output_path.resolve()),
        "output_bundle_sha256": sha(output_path),
        "replaced_descriptors": new_descriptors,
        "preserved_descriptor_count": len(descriptors) - len(new_descriptors),
        "preserved_descriptor_names": sorted(name for name in descriptors if name not in new_descriptors),
        "only_legacy_config_and_production_prefabs_descriptors_changed": True,
        "legacy_config_lineage": {
            "path": str(LEGACY_CONFIG.resolve()), "sha256": sha(LEGACY_CONFIG),
            "source_audit_sha256": artifacts["source_audit_sha256"],
            "source_yaml_readback_sha256": artifacts["source_yaml_readback_sha256"],
            "name_readback_sha256": artifacts["name_readback_sha256"],
            "config_readback_sha256": artifacts["config_readback_sha256"],
            "native_visual_readiness": False, "world_initialized": False,
        },
        "retired_production_capture": {
            "path": str(RETIRED_CAPTURE.resolve()), "sha256": sha(RETIRED_CAPTURE),
            "historical_stale_source_code_pin_count": len(stale_code),
            "historical_stale_pin_count": len(drift),
            "historical_missing_external_key": stale_missing[0],
            "superseded_as_active_descriptor": True,
        },
        "current_production_capture": {
            "path": str(CAPTURE.resolve()), "sha256": sha(CAPTURE),
            "key_sha256": current_receipt["key_sha256"],
            "root_count": 12, "entity_count": 518,
            "unsupported_presentation_reference_count": 21,
            "replay_ready": False, "full_mission_initialized": False,
            "native_visual_readiness": False, "mission_ready": False,
            "source_pin_count": current_receipt["capture_source_pin_count"],
            "capture_receipt_path": str(FINAL_RECEIPT.resolve()),
            "capture_receipt_sha256": sha(FINAL_RECEIPT),
        },
    }
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    with receipt_path.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2, sort_keys=True)
        stream.write("\n")
    return {"status": "Passed", "bundle": str(output_path.resolve()),
            "bundle_sha256": sha(output_path), "receipt": str(receipt_path.resolve()),
            "preserved_descriptor_count": len(descriptors) - 2,
            "replaced_descriptors": sorted(new_descriptors), "retired_stale_code_pins": len(stale_code),
            "current_production_roots": 12, "replay_ready": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--receipt-output", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(rebind(args.output, args.receipt_output), indent=2))


if __name__ == "__main__":
    main()
