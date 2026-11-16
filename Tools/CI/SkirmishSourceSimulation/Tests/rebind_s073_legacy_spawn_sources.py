#!/usr/bin/env python3
"""Rebind S073's legacy spawn readback to fresh prefab and AssetDatabase evidence.

This is a source-only operation. It preserves the existing decoded values and
changes provenance pins only after current YAML roots, exact object-reference
uses, AssetDatabase identities, and captured prefab roots all agree.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def fail(message):
    raise ValueError(message)


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", required=True, type=Path)
    p.add_argument("--current-config", required=True, type=Path,
                   help="existing S073 legacy-initial-spawn-data.json to rebind")
    p.add_argument("--asset-audit", required=True, type=Path,
                   help="fresh/current-yaml-readback.json source reference audit")
    p.add_argument("--name-readback", required=True, type=Path,
                   help="fresh AssetDatabase exact GUID/local-fileID names")
    p.add_argument("--prefab-capture", required=True, type=Path,
                   help="fresh Unity prefab capture containing opening_prefabs")
    p.add_argument("--prefab-receipt", required=True, type=Path)
    p.add_argument("--name-receipt", required=True, type=Path)
    p.add_argument("--output", required=True, type=Path)
    a = p.parse_args()
    repo = a.repo.resolve(strict=True)
    config_path, audit_path = a.current_config.resolve(strict=True), a.asset_audit.resolve(strict=True)
    names_path, capture_path = a.name_readback.resolve(strict=True), a.prefab_capture.resolve(strict=True)
    if a.output.exists():
        fail("Output path already exists; choose a new evidence destination")
    config, audit, names, capture = map(read, (config_path, audit_path, names_path, capture_path))
    capture_receipt, names_receipt = read(a.prefab_receipt.resolve(strict=True)), read(a.name_receipt.resolve(strict=True))
    for receipt, path, field in ((capture_receipt, capture_path, "capture_sha256"), (names_receipt, names_path, "name_readback_sha256")):
        if (receipt.get("validation_status") != "Passed" or receipt.get("wrapper_exit_code") != 0
                or receipt.get("required_marker_present") is not True or receipt.get(field) != sha(path)):
            fail("Completed checked Unity capture receipt required: " + field)
    if config.get("kind") != "S073LegacyInitialSpawnExternalData" or config.get("mission") != "S073":
        fail("Current S073 legacy spawn readback required")
    if audit.get("kind") != "S073LegacySourceAssetReferenceAudit" or audit.get("mission") != "S073":
        fail("S073 current YAML reference audit required")
    if names.get("kind") != "S073LegacyPrefabSourceNameReadback" or names.get("mission") != "S073":
        fail("Fresh S073 AssetDatabase name readback required")
    if (capture.get("kind") != "AuthoredSkirmishPrefabReferenceData"
            or capture.get("authority") != "ActualSourceAssetsAndNativeBakers_NotMissionState"
            or capture.get("fresh_world_allocator_exported") is not False
            or capture.get("full_mission_initialized") is not False
            or capture.get("native_visual_readiness") is not False):
        fail("Exact source-only S073 prefab capture required")
    capture_hash = sha(capture_path)
    if names.get("prefab_capture_sha256") != capture_hash:
        fail("Fresh name readback is not pinned to the supplied prefab capture")
    if names.get("mission_initialized") is not False or names.get("mission_world_allocated") is not False:
        fail("Name readback must explicitly remain source-only")
    if names.get("native_visual_readiness") is not False:
        fail("Name readback must explicitly deny native visual readiness")
    source_yaml_readback_sha = audit.get("readback_artifact_sha256")
    current_yaml_readback = audit_path.parent / "current-yaml-readback.json"
    if (not isinstance(source_yaml_readback_sha, str) or len(source_yaml_readback_sha) != 64
            or any(c not in "0123456789abcdef" for c in source_yaml_readback_sha)
            or not current_yaml_readback.is_file() or sha(current_yaml_readback) != source_yaml_readback_sha
            or names.get("source_readback_sha256") != source_yaml_readback_sha
            or names.get("source_pins", {}).get(str(audit_path)) != sha(audit_path)):
        fail("Fresh source-name readback must bind to the exact refreshed YAML readback and audit")

    # Exact current source root pins are mandatory; this prevents stale config data
    # from being rebound after any authored source change.
    config_pins = config.get("source_artifacts", {}).get("configuration_asset_sha256", {})
    audit_roots = {r.get("path"): r.get("sha256") for r in audit.get("configuration_roots", [])}
    if not audit_roots or len(audit_roots) != len(audit.get("configuration_roots", [])):
        fail("Audit configuration root pins are missing or duplicated")
    for rel, expected in audit_roots.items():
        source = repo / rel
        if not source.is_file() or sha(source) != expected:
            fail("Current configuration source differs from fresh audit pin: " + str(rel))
        if capture.get("source_pins", {}).get(rel) != expected or names.get("source_pins", {}).get(rel) != expected:
            fail("Fresh native data does not pin current configuration source: " + str(rel))
    for rel, expected in config_pins.items():
        if audit_roots.get(rel) != expected:
            fail("Legacy config provenance differs from current audit root: " + rel)
    if not config_pins or not set(config_pins).issubset(audit_roots):
        fail("Legacy config lacks exact configuration source pins")
    preset = config.get("preset_values", {})
    preset_path = preset.get("asset_path")
    if not isinstance(preset_path, str) or not isinstance(preset.get("asset_sha256"), str):
        fail("Legacy config is missing the pinned preset source")
    if not (repo / preset_path).is_file() or sha(repo / preset_path) != preset["asset_sha256"]:
        fail("Current preset asset differs from the exact legacy config provenance pin")

    # Build an exact tuple inventory from the audited YAML; no name is accepted
    # based on a path/name guess alone.
    audit_uses = {}
    audited_identity = {}
    for ref in audit.get("references", []):
        guid, asset = ref.get("guid"), ref.get("asset_path")
        if (not isinstance(asset, str) or not (repo / asset).is_file() or sha(repo / asset) != ref.get("asset_sha256")
                or not (repo / ref.get("meta_path", "")).is_file() or sha(repo / ref["meta_path"]) != ref.get("meta_sha256")):
            fail("Current reference asset differs from audit: " + str(asset))
        audited_identity[guid] = (asset, ref.get("asset_sha256"), ref.get("meta_sha256"), set(ref.get("file_ids", [])))
        for use in ref.get("uses", []):
            key = (use.get("source_path"), use.get("field_path"), use.get("file_id"), guid)
            if key in audit_uses:
                fail("Duplicate reference use in audit: " + str(key))
            audit_uses[key] = asset
    objects = names.get("objects")
    if not isinstance(objects, list) or not objects:
        fail("Fresh AssetDatabase object inventory is empty")
    seen = set()
    for row in objects:
        guid, file_id, asset = row.get("guid"), row.get("file_id"), row.get("asset_path")
        if not isinstance(guid, str) or not isinstance(file_id, int) or not isinstance(asset, str):
            fail("Malformed GUID/fileID/asset identity in name readback")
        identity = (guid, file_id)
        if identity in seen:
            fail("Duplicate AssetDatabase GUID/fileID identity")
        seen.add(identity)
        audited = audited_identity.get(guid)
        if (not audited or audited[0] != asset or file_id not in audited[3] or
                row.get("asset_sha256") != audited[1] or row.get("meta_sha256") != audited[2]):
            fail("Fresh AssetDatabase asset identity differs from current YAML asset audit: " + asset)
        uses = row.get("uses")
        if not isinstance(uses, list) or not uses:
            fail("AssetDatabase identity is missing field uses: " + asset)
        for use in uses:
            key = (use.get("source_path"), use.get("field_path"), use.get("file_id"), guid)
            if audit_uses.get(key) != asset or use.get("file_id") != file_id:
                fail("GUID/fileID/field use does not exactly match current YAML audit: " + str(key))
    # Ensure every audited use relevant to the two source roots is represented.
    def required(k):
        source, field, _, _ = k
        return (source.endswith("/IndustrialBasin/InitialForces.asset") and
                ((".units[" in field and field.endswith(".prefab")) or (".buildings[" in field and field.endswith(".prefab"))
                 or field in ("MonoBehaviour.baseWallPrefab", "MonoBehaviour.baseGatePrefab", "MonoBehaviour.baseCoreBuildingPrefab"))) or (
                source.endswith("/Skirmish/UnitRegistry.asset") and ".unitSpawnPrefabs[" in field)
    expected_uses = {k for k in audit_uses if required(k)}
    actual_uses = {(u.get("source_path"), u.get("field_path"), u.get("file_id"), row.get("guid"))
                   for row in objects for u in row.get("uses", [])}
    if expected_uses != (actual_uses & expected_uses):
        fail("Fresh AssetDatabase readback omits a current source-root reference use")

    roots = capture.get("opening_prefabs")
    old_roots = config.get("captured_unit_prefab_roots")
    if not isinstance(roots, dict) or not isinstance(old_roots, dict) or set(roots) != set(old_roots):
        fail("Fresh prefab capture root set differs from the pinned legacy config roots")
    # Entity numbers are capture-local. Validate each decoded config key against
    # its fresh exact GUID/fileID/field-use readback before rebinding those roots.
    by_field = {}
    for row in objects:
        for use in row["uses"]:
            by_field[(use["source_path"], use["field_path"])] = row
    initial_path = "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset"
    for fi, faction in enumerate(config["initial_spawn"]["Factions"]):
        for kind, field_name, name_field in (("Units", "units", "unit_source_key"), ("Buildings", "buildings", "building_lookup_key")):
            for index, item in enumerate(faction[kind]):
                row = by_field.get((initial_path, f"MonoBehaviour.factions[{fi}].{field_name}[{index}].prefab"))
                key = item["SourceKey"] if kind == "Units" else item["LookupKey"]
                if row is None or row.get(name_field) != key:
                    fail("Decoded config key differs from fresh source-name readback")
    out = copy.deepcopy(config)
    out["captured_unit_prefab_roots"] = dict(roots)
    out.setdefault("source_artifacts", {})["prefab_capture_sha256"] = capture_hash
    out["source_artifacts"]["name_readback_sha256"] = sha(names_path)
    out["source_artifacts"]["source_audit_sha256"] = sha(audit_path)
    # Keep the config-value audit pin distinct from the refreshed raw YAML
    # readback pin consumed by the current AssetDatabase name readback.
    out["source_artifacts"]["source_yaml_readback_sha256"] = source_yaml_readback_sha
    out["source_artifacts"]["configuration_asset_sha256"] = dict(config_pins)
    out["source_artifacts"]["rebind_authority"] = "Fresh prefab capture and exact current AssetDatabase/YAML source identities; data only"
    out["mission_outcome_derived"] = False
    out["native_visual_readiness"] = False
    out["world_initialized"] = False
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(out, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print("[S073SourceRebind] result=Passed")
    print("[S073SourceRebind] prefab_capture_sha256=" + capture_hash)
    print("[S073SourceRebind] name_readback_sha256=" + sha(names_path))
    print("[S073SourceRebind] source_asset_uses=" + str(len(expected_uses)))
    print("[S073SourceRebind] native_visual_readiness=false world_initialized=false")


if __name__ == "__main__":
    main()
