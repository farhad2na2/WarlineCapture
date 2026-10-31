#!/usr/bin/env python3
"""Rebuild the S073 external legacy startup-data readback from checked YAML.

AssetDatabase object identities are retained from a source-only readback and
joined to the current YAML by exact path/field/fileID/GUID. This tool does not
claim to refresh AssetDatabase state or allocate a Unity world.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def canonical_sha(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode()).hexdigest()


def fail(message):
    raise RuntimeError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    retained_dir = Path(__file__).resolve().parents[1] / "Fixtures/S073/retained"
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--retained-config", type=Path, default=retained_dir / "legacy-initial-spawn-data.json")
    parser.add_argument("--retained-identity", type=Path, default=retained_dir / "source-object-names.json")
    parser.add_argument("--retained-audit", type=Path, default=retained_dir / "source-asset-reference-audit.json")
    parser.add_argument("--retained-building-definitions", type=Path, default=retained_dir / "building-definitions.json")
    parser.add_argument("--retained-manifest", type=Path, default=retained_dir / "provenance.json")
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--verify-retained", action="store_true", help="require parsed source data to equal retained initial_spawn")
    args = parser.parse_args()

    repo = args.repo.resolve(strict=True)
    retained_config_path = args.retained_config.resolve(strict=True)
    retained_identity_path = args.retained_identity.resolve(strict=True)
    retained_audit_path = args.retained_audit.resolve(strict=True)
    retained_definitions_path = args.retained_building_definitions.resolve(strict=True)
    retained_manifest_path = args.retained_manifest.resolve(strict=True)
    retained_manifest = json.loads(retained_manifest_path.read_text())
    if retained_manifest.get("kind") != "S073LegacyRetainedReadbackInputs" or retained_manifest.get("mission") != "S073":
        fail("Retained S073 input provenance manifest required")
    out = args.output_dir.absolute()
    if out.exists():
        fail("Output directory must be new: " + str(out))

    config_rel = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
    registry_rel = "Assets/Game/Configs/Skirmish/UnitRegistry.asset"
    config_path = repo / config_rel
    registry_path = repo / registry_rel
    parser_path = repo / "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb"
    for path in (config_path, registry_path, parser_path):
        if not path.is_file():
            fail("Required source input missing: " + str(path))
    parser_pin = retained_manifest.get("readback_parser")
    if parser_pin != {"path": str(parser_path.relative_to(repo)), "sha256": sha(parser_path)}:
        fail("Required Ruby YAML parser differs from its durable provenance pin")
    retained_input_paths = {
        "legacy-initial-spawn-data.json": retained_config_path,
        "source-object-names.json": retained_identity_path,
        "source-asset-reference-audit.json": retained_audit_path,
        "building-definitions.json": retained_definitions_path,
    }
    retained_input_pins = retained_manifest.get("retained_artifact_sha256")
    if not isinstance(retained_input_pins, dict):
        fail("Retained S073 input provenance hashes are missing")
    for name, path in retained_input_paths.items():
        if retained_input_pins.get(name) != sha(path):
            fail("Retained S073 input differs from its durable provenance pin: " + name)
    source_json = subprocess.run(["ruby", str(parser_path), str(config_path), str(registry_path)],
                                 check=True, text=True, capture_output=True).stdout
    parsed = json.loads(source_json)
    initial = parsed[str(config_path)]["MonoBehaviour"]
    registry = parsed[str(registry_path)]["MonoBehaviour"]
    identity = json.loads(retained_identity_path.read_text())
    retained = json.loads(retained_config_path.read_text())
    audit = json.loads(retained_audit_path.read_text())
    retained_definitions = json.loads(retained_definitions_path.read_text())
    if identity.get("kind") != "S073LegacyPrefabSourceNameReadback" or identity.get("mission") != "S073":
        fail("Retained S073 AssetDatabase identity readback required")
    if retained.get("kind") != "S073LegacyInitialSpawnExternalData" or retained.get("mission") != "S073":
        fail("Retained S073 legacy config artifact required")
    if audit.get("kind") != "S073LegacySourceAssetReferenceAudit" or audit.get("mission") != "S073":
        fail("Retained S073 YAML reference audit required")
    if retained_definitions.get("kind") != "S073LegacyAuthoredBuildingDefinitions":
        fail("Retained S073 authored building definitions required")
    retained_config_pins = retained.get("source_artifacts", {}).get("configuration_asset_sha256", {})
    audit_roots = audit.get("configuration_roots")
    if not isinstance(audit_roots, list):
        fail("Retained source audit has no authoritative configuration root pins")
    seen_roots = set()
    for root in audit_roots:
        path = root.get("path")
        expected = root.get("sha256")
        if not isinstance(path, str) or not isinstance(expected, str) or path in seen_roots:
            fail("Retained source audit configuration root pins are malformed")
        seen_roots.add(path)
        if path == "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset":
            if retained_config_pins.get(path) != expected:
                fail("Retained InitialForces readback and source audit disagree")
            continue
        source_path = repo / path
        if not source_path.is_file() or sha(source_path) != expected:
            fail("Retained source audit input changed: " + path)

    objects = identity.get("objects")
    if not isinstance(objects, list):
        fail("Retained AssetDatabase object identities are missing")
    by_use = {}
    by_path_name = {}
    for row in objects:
        name = row.get("unit_source_key") or row.get("building_lookup_key")
        if name:
            by_path_name.setdefault((row.get("asset_path"), row.get("guid"), row.get("file_id")), name)
        for use in row.get("uses", []):
            by_use.setdefault((use.get("source_path"), use.get("field_path"), use.get("file_id")), []).append(row)

    def resolve(path, field_path, reference, field_type):
        if not isinstance(reference, dict) or not isinstance(reference.get("guid"), str) or not isinstance(reference.get("fileID"), int):
            fail("Malformed Unity object reference at " + field_path)
        matches = by_use.get((path, field_path, reference["fileID"]), [])
        matches = [row for row in matches if row.get("guid") == reference["guid"]]
        if len(matches) != 1:
            fail("Retained exact AssetDatabase identity does not uniquely resolve " + field_path)
        row = matches[0]
        value = row.get("unit_source_key" if field_type == "unit" else "building_lookup_key")
        if not value:
            fail("Resolved AssetDatabase identity has no " + field_type + " key at " + field_path)
        return value

    factions = []
    source_factions = initial.get("factions")
    if not isinstance(source_factions, list) or len(source_factions) != 2:
        fail("Expected the authored two-faction legacy startup config")
    for fi, faction in enumerate(source_factions):
        units = []
        for ui, unit in enumerate(faction.get("units", [])):
            field = f"MonoBehaviour.factions[{fi}].units[{ui}].prefab"
            units.append({"SourceKey": resolve(config_rel, field, unit.get("prefab"), "unit"),
                          "Count": unit["count"], "OffsetX": unit["spawnOffset"]["x"], "OffsetY": unit["spawnOffset"]["y"]})
        buildings = []
        for bi, building in enumerate(faction.get("buildings", [])):
            field = f"MonoBehaviour.factions[{fi}].buildings[{bi}].prefab"
            buildings.append({"LookupKey": resolve(config_rel, field, building.get("prefab"), "building"),
                              "OriginOffsetX": building["originOffset"]["x"], "OriginOffsetY": building["originOffset"]["y"]})
        spawn = faction["spawnCell"]
        factions.append({"FactionId": faction["factionId"], "SpawnX": spawn["x"], "SpawnY": spawn["y"], "Units": units, "Buildings": buildings})

    unit_keys = []
    for index, reference in enumerate(registry.get("unitSpawnPrefabs", [])):
        unit_keys.append(resolve(registry_rel, f"MonoBehaviour.unitSpawnPrefabs[{index}]", reference, "unit"))
    if len(unit_keys) != len(set(unit_keys)):
        fail("Duplicate source keys in UnitRegistry.unitSpawnPrefabs")

    visuals_by_key = {}
    for index, visual in enumerate(registry.get("impostorAtlases", [])):
        field = f"MonoBehaviour.impostorAtlases[{index}].prefab"
        reference = visual.get("prefab")
        identity_matches = [row for row in objects if row.get("unit_source_key") in unit_keys and
                            row.get("guid") == (reference or {}).get("guid") and
                            row.get("file_id") == (reference or {}).get("fileID")]
        if not identity_matches:
            continue
        keys = {row["unit_source_key"] for row in identity_matches}
        if len(keys) != 1:
            fail("UnitRegistry visual prefab identity is ambiguous at " + field)
        key = next(iter(keys))
        if key in visuals_by_key:
            fail("Multiple impostor atlas rows resolve to startup key " + key)
        atlas = visual.get("atlas")
        visuals_by_key[key] = {"SourceKey": key, "HasAtlas": bool(atlas and atlas.get("guid")),
                               "DirectionCount": visual["directionCount"], "Columns": visual["columns"],
                               "Rows": visual["rows"], "WorldSizeX": visual["size"]["x"],
                               "WorldSizeY": visual["size"]["y"]}
    retained_visuals = {row["SourceKey"]: row for row in retained["initial_spawn"].get("VisualRegistry", [])}
    registry_unchanged = sha(registry_path) == retained_config_pins.get(registry_rel)
    visuals = []
    for key in unit_keys:
        if key in visuals_by_key:
            visuals.append(visuals_by_key[key])
        elif registry_unchanged and key in retained_visuals:
            # No serialized atlas row exists for this registry prefab. Preserve
            # its retained source-only visual fallback only while the exact
            # retained UnitRegistry YAML remains unchanged.
            visuals.append(copy.deepcopy(retained_visuals[key]))
        else:
            fail("No serialized visual row or retained source fallback for " + key)

    def root_key(field):
        ref = initial.get(field)
        matched = []
        for row in objects:
            if any(use.get("source_path") == config_rel and use.get("field_path") == "MonoBehaviour." + field and
                   use.get("file_id") == ref.get("fileID") and row.get("guid") == ref.get("guid") for use in row.get("uses", [])):
                matched.append(row)
        if len(matched) != 1 or not matched[0].get("building_lookup_key"):
            fail("Retained exact AssetDatabase identity does not resolve " + field)
        return matched[0]["building_lookup_key"]

    initial_spawn = {
        "BlockerCount": initial["blockerCount"], "SpawnRadiusCells": initial["spawnRadiusCells"],
        "RespawnDelaySeconds": initial["respawnDelaySeconds"], "RandomSeed": initial["randomSeed"],
        "InitialDollars": initial["initialDollars"], "InitialMaterials": initial["initialMaterials"],
        "MaterialsCapacity": initial["materialsCapacity"], "InitialAiMaterials": initial["initialAiMaterials"],
        "AiMaterialsCapacity": initial["aiMaterialsCapacity"], "InitialOil": initial["initialOil"],
        "InitialFuel": initial["initialFuel"], "CreateFactionBases": bool(initial["createFactionBases"]),
        "BaseWallPrefabKey": root_key("baseWallPrefab"), "BaseGatePrefabKey": root_key("baseGatePrefab"),
        "BaseCoreBuildingPrefabKey": root_key("baseCoreBuildingPrefab"),
        "BaseHalfWidthCells": initial["baseHalfWidthCells"], "BaseHalfHeightCells": initial["baseHalfHeightCells"],
        "BaseMinimumUnitsPerFaction": initial["baseMinimumUnitsPerFaction"],
        "EnableBlockerChurn": bool(initial["enableBlockerChurn"]), "ChurnIntervalSeconds": initial["churnIntervalSeconds"],
        "AddRemovePerInterval": initial["addRemovePerInterval"],
        # This count is a retained owner readback result, not a serialized YAML field.
        "MissingVisualReferenceCount": retained["initial_spawn"]["MissingVisualReferenceCount"],
        "UnitPrefabRegistryKeys": unit_keys, "Factions": factions, "VisualRegistry": visuals,
    }
    if args.verify_retained and initial_spawn != retained.get("initial_spawn"):
        for key in sorted(set(initial_spawn) | set(retained.get("initial_spawn", {}))):
            if initial_spawn.get(key) != retained.get("initial_spawn", {}).get(key):
                print("readback difference at initial_spawn." + key, file=sys.stderr)
        fail("Parsed current YAML plus retained AssetDatabase identities differ from retained startup readback")

    config_pins = {config_rel: sha(config_path), registry_rel: sha(registry_path)}
    for path, expected in identity.get("source_pins", {}).items():
        if path.startswith("Assets/Game/Prefabs/"):
            full = repo / path
            if not full.is_file() or sha(full) != expected:
                fail("Retained prefab source identity changed: " + path)

    # Runtime definitions come from Construction; InitialForces does not define
    # their fields. Rebind only their declared config pins, and fail unless each
    # previous binding agrees with the retained config readback.
    definition_source_pins = retained_definitions.get("source_pins")
    retained_config_pins = retained["source_artifacts"]["configuration_asset_sha256"]
    if not isinstance(definition_source_pins, dict):
        fail("Retained definition source pins are missing")
    retained_definition_content = copy.deepcopy(retained_definitions)
    generated_definitions = copy.deepcopy(retained_definitions)
    for path, digest in config_pins.items():
        if definition_source_pins.get(path) != retained_config_pins.get(path):
            fail("Retained building definitions do not bind the retained config: " + path)
        if path not in generated_definitions["source_pins"]:
            fail("Building definitions do not pin required source config: " + path)
        generated_definitions["source_pins"][path] = digest
    restored_definitions = copy.deepcopy(generated_definitions)
    restored_definitions["source_pins"] = copy.deepcopy(definition_source_pins)
    if restored_definitions != retained_definition_content:
        fail("Rebound definitions changed authored data beyond source pin provenance")
    changed_definition_pins = sorted(path for path, digest in generated_definitions["source_pins"].items()
                                     if definition_source_pins.get(path) != digest)
    expected_changed_pins = sorted(path for path, digest in config_pins.items()
                                   if definition_source_pins.get(path) != digest)
    if changed_definition_pins != expected_changed_pins:
        fail("Rebound definition source pin changes exceed current config provenance")
    construction_rel = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset"
    if construction_rel not in definition_source_pins or sha(repo / construction_rel) != definition_source_pins[construction_rel]:
        fail("Original authored building definitions source changed: " + construction_rel)

    generated_identity = copy.deepcopy(identity)
    generated_identity["source_pins"][config_rel] = config_pins[config_rel]
    generated_identity["source_pins"][registry_rel] = config_pins[registry_rel]
    generated_identity["source_readback_sha256"] = "pending"

    audit_payload = {"schema_version": 1, "kind": "S073LegacyConfigYamlReadback", "authority": "Current Unity YAML plus retained AssetDatabase source identities; data only",
                     "mission": "S073", "claim_scope": "Current config values and exact object-reference joins only; no Unity world or mission readiness",
                     "parser_path": str(parser_path.relative_to(repo)), "parser_sha256": sha(parser_path),
                     "source_artifacts": config_pins,
                     "retained_identity_readback_sha256": sha(retained_identity_path),
                     "retained_asset_audit_sha256": sha(retained_audit_path),
                     "retained_building_definitions_sha256": sha(retained_definitions_path),
                     "retained_input_manifest_sha256": sha(retained_manifest_path),
                     "rebound_building_definitions_sha256": canonical_sha(generated_definitions),
                     "retained_definition_content_sha256": canonical_sha(retained_definition_content),
                     "definition_fields_equivalent_except_source_pins": True,
                     "definition_source_pin_changes": {path: {"retained_sha256": definition_source_pins[path],
                                                               "current_sha256": generated_definitions["source_pins"][path]}
                                                        for path in changed_definition_pins},
                     "authored_construction_source_sha256": definition_source_pins[construction_rel],
                     "initial_spawn_sha256": canonical_sha(initial_spawn),
                     "identity_reference_joins": sum(len(row.get("uses", [])) for row in objects),
                     "world_initialized": False, "mission_outcome_derived": False, "native_visual_readiness": False}
    audit_payload["readback_artifact_sha256"] = canonical_sha(audit_payload)
    out.mkdir(parents=True)
    audit_path = out / "s073-config-readback-audit.json"
    audit_path.write_text(json.dumps(audit_payload, indent=2, sort_keys=True) + "\n")
    generated_identity["source_readback_sha256"] = sha(audit_path)
    identity_path = out / "source-object-names.json"
    identity_path.write_text(json.dumps(generated_identity, indent=2, sort_keys=True) + "\n")

    generated = copy.deepcopy(retained)
    generated["initial_spawn"] = initial_spawn
    generated["source_artifacts"]["configuration_asset_sha256"] = config_pins
    generated["source_artifacts"]["config_readback_sha256"] = sha(audit_path)
    generated["source_artifacts"]["source_audit_sha256"] = sha(audit_path)
    generated["source_artifacts"]["name_readback_sha256"] = sha(identity_path)
    config_output = out / "legacy-initial-spawn-data.json"
    config_output.write_text(json.dumps(generated, indent=2, sort_keys=True) + "\n")
    definitions_output = out / "building-definitions.json"
    definitions_output.write_text(json.dumps(generated_definitions, indent=2, sort_keys=True) + "\n")
    print(json.dumps({"status": "Passed", "kind": "S073LegacyConfigYamlReadback", "initial_spawn_matches_retained": initial_spawn == retained.get("initial_spawn"),
                      "initial_spawn_sha256": canonical_sha(initial_spawn), "config_sha256": config_pins[config_rel],
                      "unit_registry_sha256": config_pins[registry_rel], "audit": str(audit_path), "identity": str(identity_path),
                      "config": str(config_output), "building_definitions": str(definitions_output),
                      "world_initialized": False, "mission_outcome_derived": False,
                      "native_visual_readiness": False}, indent=2))


if __name__ == "__main__":
    main()
