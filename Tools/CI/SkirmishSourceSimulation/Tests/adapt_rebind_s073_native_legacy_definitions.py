#!/usr/bin/env python3
"""Derive a consumer-shaped S073 definition document from verified native evidence."""
import argparse
import copy
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
SHA = re.compile(r"^[a-f0-9]{64}$")


def read_json(path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("duplicate JSON field: " + key)
            result[key] = value
        return result
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique)


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(ok, message):
    if not ok:
        raise ValueError(message)


def consumer_scope_flags(native, names):
    """Project source scope flags without inverting negative readiness facts."""
    need(isinstance(native, dict) and isinstance(names, dict), "native scope sources must be objects")
    need(native.get("mission_world_loaded") is False and
         native.get("mission_initialized") is False and
         native.get("native_readiness") is False and
         names.get("mission_world_allocated") is False and
         names.get("mission_initialized") is False and
         names.get("native_visual_readiness") is False,
         "native mission/readiness source flags must all be false")
    return {
        "mission_world_allocated": names["mission_world_allocated"],
        "full_mission_initialized": native["mission_initialized"],
        "native_visual_readiness": native["native_readiness"],
    }


def pinned_file(path, expected, repo=ROOT):
    p = Path(path)
    if not p.is_absolute():
        p = repo / p
    need(SHA.fullmatch(str(expected) or "") is not None, "invalid SHA-256 pin: " + str(path))
    need(p.is_file() and digest(p) == expected, "missing/drifted pinned input: " + str(p))
    return p.resolve()


def descriptor(bundle, name):
    value = bundle.get(name)
    need(isinstance(value, dict) and set(value) == {"path", "sha256"}, "exact descriptor required: " + name)
    return pinned_file(value["path"], value["sha256"])


def audit_campaign_relocation(bundle):
    physical_path = descriptor(bundle, "campaign_physical")
    receipt_path = descriptor(bundle, "capture_relocation")
    physical = read_json(physical_path)
    receipt = read_json(receipt_path)
    need(receipt.get("kind") == "S073CapturedSourcePinPreservation" and receipt.get("status") == "Passed" and
         receipt.get("mission_ready") is False, "checked captured source relocation receipt required")
    original_root = physical.get("capturedProjectRoot")
    need(isinstance(original_root, str) and Path(original_root).is_absolute() and
         receipt.get("original_root") == original_root, "relocation receipt must bind exact captured root")

    relocated = {}
    preserved_root = Path(receipt["preserved_root"])
    need(preserved_root.is_absolute(), "absolute preserved source root required")
    for row in receipt.get("files", []):
        rel, expected = row.get("relative_path"), row.get("sha256")
        need(isinstance(rel, str) and not Path(rel).is_absolute() and ".." not in Path(rel).parts,
             "safe relative relocation path required")
        target = (preserved_root / rel).resolve(strict=True)
        need(target.is_file() and digest(target) == expected, "preserved source file hash mismatch: " + rel)
        original = str((Path(original_root) / rel).resolve())
        need(original not in relocated, "duplicate relocation mapping")
        relocated[original] = (target, expected)
    external_root = receipt_path.parent.resolve()
    for row in receipt.get("external_pins_verified_in_place", []):
        original, expected, preserved = row.get("path"), row.get("sha256"), row.get("preserved_path")
        if not preserved:
            continue
        target = Path(preserved).resolve(strict=True)
        need(str(target).startswith(str(external_root) + "/") and digest(target) == expected,
             "external relocation target/hash mismatch: " + str(original))
        need(original not in relocated, "duplicate external relocation mapping")
        relocated[original] = (target, expected)

    read_pins = physical.get("readPins")
    need(isinstance(read_pins, dict) and read_pins, "campaign physical read pin set required")
    historical = []
    relocated_count = 0
    in_place_count = 0
    unresolved = []
    historical_root = str(Path(original_root) / "Library/ScriptAssemblies") + "/"
    for raw, expected in read_pins.items():
        original = Path(raw) if Path(raw).is_absolute() else Path(original_root) / raw
        original = str(original.resolve())
        if original in relocated:
            target, mapped_hash = relocated[original]
            need(mapped_hash == expected, "relocation map hash differs from read pin: " + raw)
            relocated_count += 1
            continue
        # Preserve missing historical compiler outputs as a provenance limitation.
        if original.startswith(historical_root) and original.endswith(".dll"):
            historical.append({"path": raw, "sha256": expected, "status": "HistoricalBinaryUnavailable_NotReplaced"})
            continue
        path = Path(original)
        if path.is_file() and digest(path) == expected:
            in_place_count += 1
        else:
            unresolved.append({"path": raw, "sha256": expected, "status": "Unresolved"})
    need(not unresolved, "campaign relocation has unresolved non-historical pins: " + str(len(unresolved)))
    need(len(historical) == 7, "expected exact seven unavailable historical script assemblies; found " + str(len(historical)))
    return {
        "raw_campaign_read_pin_count": len(read_pins),
        "receipt_relocated_pin_count": relocated_count,
        "verified_in_place_pin_count": in_place_count,
        "unresolved_historical_binary_count": len(historical),
        "unresolved_historical_binaries": historical,
        "unresolved_non_historical_pin_count": len(unresolved),
        "all_relocated_bytes_verified": True,
        "source_execution_provenance_complete": False,
    }


def build(args):
    repo = args.repo.resolve(strict=True)
    need(repo == ROOT.resolve(strict=True), "exact repository root required")
    source_bundle_path = args.bundle.resolve(strict=True)
    source_bundle_sha = digest(source_bundle_path)
    bundle = read_json(source_bundle_path)
    need(bundle.get("schema_version") == 1 and bundle.get("kind") == "S073LegacyStartupBundle" and
         bundle.get("mission") == "S073" and bundle.get("seed") == 104729,
         "exact current r22 S073 bundle required")
    need(bundle.get("source_repository_root") == str(repo), "bundle source root mismatch")

    rebind_receipt = read_json(args.bundle_receipt.resolve(strict=True))
    need(rebind_receipt.get("kind") == "S073CurrentOffsetsBundleRebindReceipt" and
         rebind_receipt.get("validation_status") == "Passed" and
         rebind_receipt.get("output_bundle_path") == str(source_bundle_path) and
         rebind_receipt.get("output_bundle_sha256") == source_bundle_sha,
         "current-offset bundle receipt does not bind exact r22 bundle")

    native_path = args.native.resolve(strict=True)
    native_sha = digest(native_path)
    qualification = read_json(args.qualification.resolve(strict=True))
    need(qualification.get("kind") == "S073NativeLegacyBuildingDefinitionsQualification" and
         qualification.get("capture_validation") == "Passed" and
         qualification.get("capture_path") == str(native_path) and
         qualification.get("capture_sha256") == native_sha and
         qualification.get("full_definition_semantics_match_retained") is True and
         qualification.get("source_pin_drift_count") == 0 and
         qualification.get("consumer_compatibility") == "NotCompatible",
         "strict native definition qualification receipt mismatch")
    native = read_json(native_path)
    pins = native.get("source_pins")
    need(isinstance(pins, dict) and len(pins) == qualification.get("source_pin_count"),
         "native capture pin count differs from qualification")
    for path, expected in pins.items():
        pinned_file(path, expected, repo)

    config_path = descriptor(bundle, "legacy_config")
    config = read_json(config_path)
    config_artifacts = config.get("source_artifacts", {})
    config_asset_pins = config_artifacts.get("configuration_asset_sha256")
    need(isinstance(config_asset_pins, dict) and config_asset_pins, "current legacy config asset pins required")
    for path, expected in config_asset_pins.items():
        need(pins.get(path) == expected, "native definition capture lacks current config pin: " + path)

    name_path = descriptor(bundle, "source_identity")
    names = read_json(name_path)
    name_receipt_path = Path(bundle["prefab_capture"]["path"]).parent / "name-readback-receipt.json"
    name_receipt = read_json(name_receipt_path)
    prefab_path = descriptor(bundle, "prefab_capture")
    need(name_receipt.get("kind") == "S073LegacyPrefabNameReadbackReceipt" and
         name_receipt.get("validation_status") == "Passed" and name_receipt.get("wrapper_exit_code") == 0 and
         name_receipt.get("required_marker_present") is True and
         name_receipt.get("name_readback_sha256") == digest(name_path) and
         name_receipt.get("prefab_capture_sha256") == digest(prefab_path) and
         names.get("prefab_capture_sha256") == digest(prefab_path) and
         names.get("mission_world_allocated") is False and names.get("mission_initialized") is False and
         names.get("native_visual_readiness") is False,
         "current name readback receipt/prefab binding failed")
    for path, expected in names.get("source_pins", {}).items():
        pinned_file(path, expected, repo)

    yaml_path = args.yaml_readback.resolve(strict=True)
    yaml_sha = digest(yaml_path)
    need(config_artifacts.get("source_yaml_readback_sha256") == yaml_sha and
         names.get("source_readback_sha256") == yaml_sha,
         "current YAML readback must match both r22 config and name readback")
    yaml_readback = read_json(yaml_path)
    selected_asset_path = bundle["selected_map"]["path"]
    selected_asset_sha = bundle["selected_map"]["sha256"]
    selected_asset = Path(selected_asset_path).resolve(strict=True)
    selected_asset_rel = str(selected_asset.relative_to(repo))
    need(names.get("source_pins", {}).get(selected_asset_rel) == selected_asset_sha and
         pins.get(selected_asset_rel) == selected_asset_sha,
         "selected map asset is not bound by both current identity and native definition pins")
    map_yaml = yaml_readback.get(str(selected_asset), {}).get("MonoBehaviour", {})
    operation_map_id = map_yaml.get("operationMapId")
    source_identity_hash = map_yaml.get("sourceIdentityHash")
    need(operation_map_id == "opmap.skirmish.industrial_basin" and SHA.fullmatch(source_identity_hash or ""),
         "selected source identity must be read from current map YAML")
    preset_path = "Assets/Game/Resources/SkirmishIndustrialBasin.asset"
    preset_hash = names.get("source_pins", {}).get(preset_path)
    preset_absolute = str(repo / preset_path)
    preset_mono = yaml_readback.get(preset_absolute, {}).get("MonoBehaviour", {})
    need(preset_hash and pins.get(preset_path) == preset_hash and
         preset_mono.get("operationMap", {}).get("fileID") == 11400000,
         "current S073 preset operationMap reference required")
    map_meta_path = selected_asset.with_name(selected_asset.name + ".meta")
    map_meta_text = map_meta_path.read_text(encoding="utf-8")
    match = re.search(r"^guid:\s*([a-f0-9]{32})\s*$", map_meta_text, re.MULTILINE)
    preset_map_guid = preset_mono.get("operationMap", {}).get("guid")
    need(match is not None and match.group(1) == preset_map_guid and
         pins.get(str(map_meta_path.relative_to(repo))) == digest(map_meta_path),
         "preset's selected OperationMap GUID must resolve to the pinned native map asset")
    audit_rel = "Logs/SkirmishProduction/s073-source-inputs-20261101-r2/asset-audit-r2b/source-asset-reference-audit.json"
    audit_path = repo / audit_rel
    audit_pin = names.get("source_pins", {}).get(str(audit_path))
    need(audit_pin and digest(audit_path) == audit_pin and
         read_json(audit_path).get("readback_artifact_sha256") == yaml_sha,
         "current source asset reference audit must bind the exact YAML readback")

    exporter_path = "Assets/Game/Scripts/Editor/Skirmish/S073LegacyBuildingDefinitionsExport.cs"
    exporter_sha = pins.get(exporter_path)
    exporter_text = (repo / exporter_path).read_text(encoding="utf-8")
    need(exporter_sha and digest(repo / exporter_path) == exporter_sha and
         'using (var world = new World("S073LegacyAuthoredDefinitionMetadata", WorldFlags.Game))' in exporter_text and
         '["mission_initialized"] = false' in exporter_text and
         '["mission_world_loaded"] = false' in exporter_text,
         "isolated metadata-world producer scope is not source-pinned")

    keys = [row.get("key") for row in native.get("definitions", [])]
    need(keys == EXPECTED_KEYS and [row.get("key") for row in native.get("building_order", [])] == EXPECTED_KEYS,
         "native capture keys/order must directly provide requested_keys")
    adapted = copy.deepcopy(native)
    adapted["requested_keys"] = keys
    adapted["selected_source"] = {
        "operation_map_id": operation_map_id,
        "source_identity_hash": source_identity_hash,
        "resolution": {"mode": "PresetOperationMap", "presetOperationMapPresent": True},
    }
    adapted.update(consumer_scope_flags(native, names))

    relocation_audit = audit_campaign_relocation(bundle)
    output_doc = args.output_document.absolute()
    output_bundle = args.output_bundle.absolute()
    output_receipt = args.output_receipt.absolute()
    need(not output_doc.exists() and not output_bundle.exists() and not output_receipt.exists(),
         "all adapter/rebind outputs must be create-new")
    need(output_doc.parent.is_dir() and output_bundle.parent.is_dir() and output_receipt.parent.is_dir(),
         "output directories must already exist")

    preserved = {}
    for key, value in bundle.items():
        if isinstance(value, dict) and "path" in value:
            path = descriptor(bundle, key)
            preserved[key] = {"path": str(path), "sha256": digest(path)}

    with output_doc.open("x", encoding="utf-8") as stream:
        json.dump(adapted, stream, indent=2)
        stream.write("\n")
    adapted_sha = digest(output_doc)
    rebound = copy.deepcopy(bundle)
    rebound["building_definitions"] = {"path": str(output_doc.resolve()), "sha256": adapted_sha}
    with output_bundle.open("x", encoding="utf-8") as stream:
        json.dump(rebound, stream, indent=2)
        stream.write("\n")
    bundle_sha = digest(output_bundle)
    for key, expected in preserved.items():
        if key == "building_definitions":
            continue
        need(descriptor(bundle, key) == Path(expected["path"]) and digest(expected["path"]) == expected["sha256"],
             "source bundle descriptor changed during rebind: " + key)
    receipt = {
        "kind": "S073NativeLegacyDefinitionAdapterRebindReceipt",
        "schema_version": 1,
        "validation_status": "Passed",
        "native_capture": {"path": str(native_path), "sha256": native_sha},
        "native_qualification": {"path": str(args.qualification.resolve()), "sha256": digest(args.qualification)},
        "adapted_document": {"path": str(output_doc.resolve()), "sha256": adapted_sha},
        "source_bundle": {"path": str(source_bundle_path), "sha256": source_bundle_sha},
        "rebound_bundle": {"path": str(output_bundle.resolve()), "sha256": bundle_sha},
        "current_legacy_config": {"path": str(config_path), "sha256": digest(config_path)},
        "current_name_readback": {"path": str(name_path), "sha256": digest(name_path),
                                  "receipt_path": str(name_receipt_path), "receipt_sha256": digest(name_receipt_path)},
        "current_yaml_readback": {"path": str(yaml_path), "sha256": yaml_sha},
        "selected_source_identity": {"operation_map_id": operation_map_id, "source_identity_hash": source_identity_hash,
                                     "derived_from": str(selected_asset_rel), "source_sha256": selected_asset_sha,
                                     "preset_reference_guid": preset_map_guid},
        "field_derivations": {
            "requested_keys": "Exact ordered native definition rows and building_order",
            "selected_source": "Current pinned OperationMap YAML plus preset GUID reference",
            "mission_world_allocated": "False for mission world; native producer uses a separate scoped metadata-only World",
            "full_mission_initialized": "Native mission_initialized=false",
            "native_visual_readiness": "Native native_readiness=false",
        },
        "isolated_metadata_world": {"allocated": True, "disposed_by_using_scope": True,
                                    "producer_source_path": exporter_path, "producer_source_sha256": exporter_sha},
        "campaign_relocation_audit": relocation_audit,
        "preserved_source_descriptor_count": len(preserved),
        "preserved_source_descriptors": preserved,
        "source_execution_qualified": False if relocation_audit["unresolved_historical_binary_count"] else True,
        "source_run_performed": False,
        "source_run_gate": "Not run: seven historical ScriptAssemblies DLL contents remain unavailable; relocation only covers captured assets and two external artifacts.",
        "road_receipt_preserved": True,
        "mission_ready": False,
        "source_fit_certified": False,
        "normal_input_playthrough": False,
        "player_readiness": False,
        "native_capture_rewritten": False,
    }
    with output_receipt.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    return {"status": "Passed", "adapter_document": str(output_doc), "adapter_sha256": adapted_sha,
            "bundle": str(output_bundle), "bundle_sha256": bundle_sha, "receipt": str(output_receipt),
            "unresolved_historical_binaries": relocation_audit["unresolved_historical_binary_count"],
            "source_run_performed": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--bundle", type=Path, required=True)
    parser.add_argument("--bundle-receipt", type=Path, required=True)
    parser.add_argument("--native", type=Path, required=True)
    parser.add_argument("--qualification", type=Path, required=True)
    parser.add_argument("--yaml-readback", type=Path, required=True)
    parser.add_argument("--output-document", type=Path, required=True)
    parser.add_argument("--output-bundle", type=Path, required=True)
    parser.add_argument("--output-receipt", type=Path, required=True)
    args = parser.parse_args()
    result = build(args)
    print(json.dumps(result, indent=2))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as error:
        print("[S073NativeLegacyDefinitionAdapter] result=Failed " + str(error), file=sys.stderr)
        sys.exit(1)
