#!/usr/bin/env python3
"""Derive an explicitly unqualified S073 bundle using current source readback.

The retained native captures remain byte-identical and are not rebound. Only
the source-only legacy config, object-name readback, and equivalent building
definitions are replaced with outputs of generate_s073_legacy_config_readback.py.
Consumers must opt into the current-source diagnostic route, which reports
native map-blocker parity as unverified.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def descriptor(path):
    path = Path(path).resolve(strict=True)
    if not path.is_file() or path.is_symlink():
        raise ValueError("Regular source-readback artifact required: " + str(path))
    return {"path": str(path), "sha256": sha(path)}


def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def need(value, message):
    if not value:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-bundle", required=True, type=Path)
    parser.add_argument("--readback-dir", required=True, type=Path)
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--omit-native-scene-binding", action="store_true",
                        help="Exclude retained scene-binding observation from this explicitly unqualified diagnostic bundle; never rebind it to changed source")
    args = parser.parse_args()
    base_path = args.base_bundle.resolve(strict=True)
    base_pin = descriptor(base_path)
    base = read(base_path)
    need(base.get("schema_version") == 1 and base.get("kind") == "S073LegacyStartupBundle" and
         base.get("mission") == "S073" and base.get("seed") == 104729,
         "Exact retained S073 source bundle required")
    readback = args.readback_dir.resolve(strict=True)
    config_path = readback / "legacy-initial-spawn-data.json"
    identity_path = readback / "source-object-names.json"
    audit_path = readback / "s073-config-readback-audit.json"
    definitions_path = readback / "building-definitions.json"
    config, identity, audit, definitions = map(read, (config_path, identity_path, audit_path, definitions_path))
    need(config.get("kind") == "S073LegacyInitialSpawnExternalData" and config.get("mission") == "S073" and
         identity.get("kind") == "S073LegacyPrefabSourceNameReadback" and identity.get("mission") == "S073" and
         audit.get("kind") == "S073LegacyConfigYamlReadback" and audit.get("authority") == "Current Unity YAML plus retained AssetDatabase source identities; data only",
         "Current source-only S073 readback lineage required")
    need(audit.get("world_initialized") is False and audit.get("mission_outcome_derived") is False and
         audit.get("native_visual_readiness") is False and
         audit.get("initial_spawn_sha256") == hashlib.sha256(json.dumps(config["initial_spawn"], sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode()).hexdigest(),
         "Readback must retain its source-only scope and exact parsed spawn data")
    initial_rel = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
    initial_path = Path(base["source_repository_root"]) / initial_rel
    current_initial_sha = sha(initial_path)
    need(config.get("source_artifacts", {}).get("configuration_asset_sha256", {}).get(initial_rel) == current_initial_sha,
         "Readback does not bind the current authored InitialForces asset")
    need(identity.get("source_readback_sha256") == sha(audit_path) and
         config.get("source_artifacts", {}).get("name_readback_sha256") == sha(identity_path) and
         config.get("source_artifacts", {}).get("config_readback_sha256") == sha(audit_path),
         "Current config, audit, and identity readbacks are not cross-bound")
    old_definitions = read(base["building_definitions"]["path"])
    old_pins = old_definitions.get("source_pins")
    new_pins = definitions.get("source_pins")
    need(isinstance(old_pins, dict) and isinstance(new_pins, dict),
         "Building definition provenance pin sets required")
    old_rows, new_rows = old_definitions.get("definitions"), definitions.get("definitions")
    need(isinstance(old_rows, list) and isinstance(new_rows, list) and len(old_rows) == len(new_rows) == 6,
         "Complete six-entry S073 definition source readback required")
    for old_row, new_row in zip(old_rows, new_rows):
        need(all(old_row.get(key) == new_row.get(key) for key in ("key", "prefab", "factory", "fields")),
             "Building runtime definition fields, keys, or prefab identities changed")
    config_pins = config.get("source_artifacts", {}).get("configuration_asset_sha256", {})
    need(config_pins.get(initial_rel) == definitions.get("source_pins", {}).get(initial_rel) == current_initial_sha,
         "Rebound definitions do not pin the current authored config")

    base_identity = read(base["source_identity"]["path"])
    need(base_identity.get("prefab_capture_sha256") == base["prefab_capture"]["sha256"] and
         base_identity.get("objects") == identity.get("objects") and
         len(base_identity.get("objects", [])) == len(identity.get("objects", [])),
         "Current and retained prefab identity rows must match exactly before same-capture join")
    out = args.output_dir.absolute()
    out.mkdir(parents=True, exist_ok=False)
    joined_identity = copy.deepcopy(identity)
    joined_identity["prefab_capture_sha256"] = base["prefab_capture"]["sha256"]
    joined_identity["source_pins"] = {
        key: value for key, value in joined_identity.get("source_pins", {}).items()
        if "prefab-reference-data.json" not in key
    }
    joined_identity["source_pins"][base["prefab_capture"]["path"]] = base["prefab_capture"]["sha256"]
    joined_identity_path = out / "source-object-names-current-source-joined.json"
    joined_identity_path.write_text(json.dumps(joined_identity, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    joined_config = copy.deepcopy(config)
    joined_config["source_artifacts"]["prefab_capture_sha256"] = base["prefab_capture"]["sha256"]
    joined_config["source_artifacts"]["name_readback_sha256"] = sha(joined_identity_path)
    joined_config_path = out / "legacy-initial-spawn-data-current-source-joined.json"
    joined_config_path.write_text(json.dumps(joined_config, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    joined_definitions = copy.deepcopy(old_definitions)
    retained_config_pins = read(base["legacy_config"]["path"]).get("source_artifacts", {}).get("configuration_asset_sha256", {})
    definition_pin_changes = {}
    for rel in config_pins:
        need(joined_definitions["source_pins"].get(rel) == retained_config_pins.get(rel) and
             definitions["source_pins"].get(rel) == config_pins[rel],
             "Definition provenance source pin cannot be rebound from the exact readback: " + rel)
        old_digest = joined_definitions["source_pins"][rel]
        joined_definitions["source_pins"][rel] = config_pins[rel]
        if old_digest != config_pins[rel]:
            definition_pin_changes[rel] = {"retained_sha256": old_digest, "current_sha256": config_pins[rel]}
    joined_definitions_path = out / "building-definitions-current-source-joined.json"
    joined_definitions_path.write_text(json.dumps(joined_definitions, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    restored_definitions = copy.deepcopy(joined_definitions)
    restored_definitions["source_pins"] = copy.deepcopy(old_pins)
    need(restored_definitions == old_definitions and bool(definition_pin_changes),
         "Definition source pin rebinding changed payload beyond authored config provenance")
    identity_join = {
        "schema_version": 1,
        "kind": "S073CurrentSourcePrefabIdentityJoin",
        "status": "Passed",
        "scope": "Exact retained AssetDatabase object-name rows joined to current YAML readback; no recapture or world allocation",
        "retained_identity": descriptor(base["source_identity"]["path"]),
        "generated_current_identity": descriptor(identity_path),
        "objects_byte_equivalent": True,
        "object_count": len(joined_identity["objects"]),
        "retained_prefab_capture": base["prefab_capture"],
        "current_initial_forces_sha256": current_initial_sha,
        "native_visual_readiness": False,
        "world_initialized": False,
        "mission_outcome_derived": False,
        "joined_identity": descriptor(joined_identity_path),
        "joined_config": descriptor(joined_config_path),
        "retained_building_definitions": descriptor(base["building_definitions"]["path"]),
        "source_readback_building_definitions": descriptor(readback / "building-definitions.json"),
        "building_definition_runtime_fields_equivalent": True,
        "building_definition_pin_changes": definition_pin_changes,
        "joined_building_definitions": descriptor(joined_definitions_path),
    }
    join_path = out / "identity-join-receipt.json"
    join_path.write_text(json.dumps(identity_join, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    need(joined_config["source_artifacts"]["name_readback_sha256"] == sha(joined_identity_path) and
         joined_config["source_artifacts"]["prefab_capture_sha256"] == base["prefab_capture"]["sha256"],
         "Joined current source readback does not bind the retained capture and identity")

    bundle = copy.deepcopy(base)
    replacements = {
        "legacy_config": descriptor(joined_config_path),
        "source_identity": descriptor(joined_identity_path),
        "building_definitions": descriptor(joined_definitions_path),
    }
    bundle.update(replacements)
    omitted_diagnostic_descriptors = {}
    if args.omit_native_scene_binding:
        native_binding = bundle.pop("native_scene_binding_receipt", None)
        need(native_binding is not None,
             "--omit-native-scene-binding requires a retained native scene-binding descriptor to record")
        omitted_diagnostic_descriptors["native_scene_binding_receipt"] = {
            **native_binding,
            "reason": "Not consumed by current-source diagnostic; retained source pins predate the shared grid-frame seam and were not rebound",
        }
    bundle_path = out / "s073-current-source-diagnostic-bundle.json"
    bundle_path.write_text(json.dumps(bundle, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    receipt = {
        "schema_version": 1,
        "kind": "S073CurrentSourceDiagnosticBundleDerivation",
        "status": "Passed",
        "scope": "Current authored config readback only; retained native observation parity unverified",
        "base_bundle": base_pin,
        "replaced_descriptors": replacements,
        "identity_join_receipt": descriptor(join_path),
        "preserved_descriptors": {key: value for key, value in base.items()
                                   if key in {"prefab_capture", "launch_capture", "campaign_physical", "campaign_water",
                                              "campaign_startup", "authored_mask", "mission_scene", "production_prefabs"}},
        "diagnostic_omitted_descriptors": omitted_diagnostic_descriptors,
        "initial_forces_current_sha256": current_initial_sha,
        "building_definition_data_equivalent_except_source_pins": True,
        "world_initialized": False,
        "mission_outcome_derived": False,
        "native_visual_readiness": False,
        "native_map_blocker_parity_verified": False,
        "output_bundle": descriptor(bundle_path),
    }
    receipt_path = out / "derivation-receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2))


if __name__ == "__main__":
    main()
