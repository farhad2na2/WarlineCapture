#!/usr/bin/env python3
"""Project a retained failed prefab capture into the existing importer read domain.

This does not mark the raw capture passed or replayable. It preserves the raw
capture and permits only the presentation-only UnityObjectRef leaves that the
compiled focused importer already ignores.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[4]
FIELDS = [
    "schema_version", "kind", "authority", "source_scenes", "unity_version",
    "template_namespace_id", "requested_prefab_keys", "requested_prefab_keys_sha256",
    "opening_prefabs", "entities", "unsupported_values", "replay_ready",
    "fresh_world_allocator_exported", "full_mission_initialized", "native_visual_readiness",
    "geometry_exported", "consumer_rule",
]
ALLOWED = {
    "Game.Components.UnitAttackImpactVfxReference": {"Prefab"},
    "Game.Components.UnitMuzzleFlashVfxReference": {"Prefab"},
    "Game.Components.GroundMissileLauncherVfxReferenceComponent": {
        "LauncherBackfirePrefab", "RocketTrailPrefab", "ImpactExplosionPrefab", "ImpactSmokePrefab"},
    "Game.Components.AirMissileLauncherVfxReferenceComponent": {
        "MissileVisualPrefab", "LaunchFlashPrefab", "LaunchSmokePrefab", "MissileTrailPrefab",
        "AirburstExplosionPrefab", "AirTargetImpactPrefab", "InterceptExplosionPrefab"},
}
OBJECT_REF_CAPTURE = "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def pin(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"Canonical regular file required: {path}")
    raw = path.read_bytes()
    return {"path": str(path), "sha256": sha(raw)}


def read(path):
    return json.loads(Path(path).read_bytes())


def need(condition, message):
    if not condition:
        raise ValueError(message)


def descriptor(path):
    return pin(path)


def attach_unit_owner_evidence(proof, evidence):
    """Keep the optional proof key absent unless direct owner evidence exists."""
    if evidence is not None:
        proof["unit_owner_evidence"] = evidence
    return proof


def verify_source_pins(pins, root):
    need(isinstance(pins, dict) and pins, "Complete captured source pin map required")
    root = root.resolve(strict=True)
    for path_text, expected in pins.items():
        need(isinstance(path_text, str) and isinstance(expected, str) and
             re.fullmatch(r"[0-9a-f]{64}", expected) is not None, "Canonical source pin required")
        path = Path(path_text)
        if path.is_absolute():
            full = path.resolve(strict=True)
            need(full == path, "Noncanonical absolute source pin: " + path_text)
        else:
            need("\\" not in path_text and all(part not in ("", ".", "..") for part in path_text.split("/")) and
                 path.parts[0] in {"Assets", "Packages", "Library", "ProjectSettings", "T Logs"},
                 "Relative source pin outside permitted source domains: " + path_text)
            full = (root / path).resolve(strict=True)
        need(full.is_relative_to(root), "Captured source pin escaped current source root: " + path_text)
        need(not full.is_symlink() and full.is_file() and full.stat().st_size <= 512 * 1024 * 1024 and
             sha(full.read_bytes()) == expected, "Captured source pin changed: " + path_text)


def extract_unit_keys(units):
    rows = units.get("units")
    need(isinstance(rows, list) and rows, "Current unit production metadata rows required")
    keys = []
    for row in rows:
        prefab = row.get("prefab")
        key = prefab.get("key") if isinstance(prefab, dict) else row.get("key")
        need(isinstance(key, str) and key and key == key.strip(), "Exact unit prefab key required")
        keys.append(key)
    need(len(keys) == len(set(keys)), "Duplicate unit prefab keys in current metadata")
    return keys


def _sha_file(path):
    path = Path(path)
    if path.is_symlink() or not path.is_file():
        raise ValueError("Canonical regular provenance file required: " + str(path))
    return sha(path.read_bytes())


def logged_argument_matches(log_text, flag, value):
    lines = [line.strip() for line in log_text.splitlines()]
    return any(lines[i] == flag and i + 1 < len(lines) and lines[i + 1] == value
               for i in range(len(lines)))


def validate_actual_owner_unit_metadata(units, identity, metadata_sha, author,
                                        authoring_result_path, physical_request,
                                        physical_request_path, context, key_document):
    """Validate direct original-unit-owner output without relabeling it as derived metadata."""
    need(units.get("kind") == "AuthoredSkirmishUnitProductionMetadata" and
         units.get("authority") == "OriginalConfiguredAuthoringFactories_NotMissionState",
         "Original configured unit-owner metadata authority required")
    need(all(units.get(flag, False) is False for flag in
             ("mission_ready", "runtime_admission", "source_fit_certified", "native_acceptance", "world_allocated")) and
         units.get("full_mission_initialized") is False and
         units.get("native_visual_readiness") is False and
         units.get("baked_actor_footprint_substituted") is False and
         units.get("ui_catalog_runtime_policy_sampled") is False and
         units.get("runtime_camera_basis_exported") is False,
         "Direct unit-owner metadata must retain all unqualified scope flags")
    selection = units.get("compiler_selection")
    need(isinstance(selection, dict) and set(selection) == {
        "result", "physical_input_request", "catalog_id", "seed", "size", "difficulty", "setup_hash",
        "operation_map_id", "source_content_hash", "layout_fingerprint", "status", "runtime_admission",
        "source_fit_certified", "mission_ready", "native_acceptance", "world_allocated"},
        "Exact compiler-selected unit-owner provenance record required")
    compiler_result_ref = selection["result"]
    physical_request_ref = selection["physical_input_request"]
    need(isinstance(compiler_result_ref, dict) and set(compiler_result_ref) == {"path", "sha256"} and
         isinstance(physical_request_ref, dict) and set(physical_request_ref) == {"path", "sha256"},
         "Exact compiler result and physical request descriptors required")
    authoring_result_path = Path(authoring_result_path).resolve(strict=True)
    physical_request_path = Path(physical_request_path).resolve(strict=True)
    need(compiler_result_ref["path"] == str(authoring_result_path) and
         compiler_result_ref["sha256"] == _sha_file(authoring_result_path),
         "Unit-owner metadata must pin the supplied current compiler result")
    need(physical_request_ref["path"] == str(physical_request_path) and
         physical_request_ref["sha256"] == _sha_file(physical_request_path),
         "Unit-owner metadata must pin its exact physical-input request")
    source_pins = units.get("source_pins")
    need(isinstance(source_pins, dict) and
         source_pins.get(str(authoring_result_path)) == compiler_result_ref["sha256"] and
         source_pins.get(str(physical_request_path)) == physical_request_ref["sha256"],
         "Unit-owner source pins must carry both selected compiler inputs")
    need(selection.get("status") == "ConfigurationCompiled" and
         selection.get("runtime_admission") is False and selection.get("source_fit_certified") is False and
         selection.get("mission_ready") is False and selection.get("native_acceptance") is False and
         selection.get("world_allocated") is False,
         "Compiler-selected unit metadata must preserve every non-readiness flag")

    need(author.get("kind") == "SkirmishMissionAuthoringPreflightResult" and
         author.get("status") == "ConfigurationCompiled" and author.get("compiler_accepted") is True and
         author.get("native_authoring_inputs_returned") is True and
         author.get("resolved_setup", {}).get("setup_hash") == selection.get("setup_hash") and
         author.get("catalog_id") == selection.get("catalog_id") and
         author.get("seed") == selection.get("seed") and
         author.get("size") == selection.get("size") and
         author.get("difficulty") == selection.get("difficulty") and
         author.get("operation_map_id") == selection.get("operation_map_id") and
         isinstance(author.get("layout_fingerprint"), str) and
         re.fullmatch(r"[0-9a-f]{64}", author["layout_fingerprint"]) is not None and
         author.get("world_allocated") is False and author.get("source_fit_verified") is False and
         author.get("mission_ready") is False and author.get("native_acceptance") is False,
         "Direct unit metadata must join the accepted current compiler result without promoting readiness")
    need(selection.get("catalog_id") == "S050" and selection.get("seed") == 104779 and
         selection.get("size") == context.get("target_context", {}).get("size") and
         selection.get("difficulty") == context.get("target_context", {}).get("difficulty") and
         selection.get("setup_hash") == context.get("target_context", {}).get("setup_hash") and
         selection.get("layout_fingerprint") == context.get("target_context", {}).get("layout_binding_fingerprint") and
         selection.get("operation_map_id") == context.get("target_context", {}).get("operation_map_id"),
         "Unit-owner selection must match the pinned S050 compiler context")
    target_context = context.get("target_context", {})
    policy_hash_prefix = (str(target_context.get("definition_id")) + ".v" +
                          str(target_context.get("content_version")) + "/enemy-policy-v1/")
    need(selection.get("source_content_hash", "").startswith(policy_hash_prefix) and
         re.fullmatch(re.escape(policy_hash_prefix) + r"[0-9a-f]{64}", selection["source_content_hash"]) is not None and
         selection.get("layout_fingerprint") == target_context.get("layout_binding_fingerprint") and
         author.get("source_content_hash") == target_context.get("source_content_hash"),
         "Compiler source-policy hash and logical binding fingerprint must match their own context fields")
    need(physical_request.get("kind") == "PinnedCampaignPhysicalInputExport" and
         physical_request.get("source_fit_certified") is False and physical_request.get("mission_ready") is False and
         physical_request.get("operation_map_id") == author.get("operation_map_id") and
         physical_request.get("content_hash") == author.get("source_content_hash") and
         physical_request.get("selection") == compiler_result_ref,
         "Exact physical request must select this compiler result and source map")
    for flag in ("world_allocated", "source_fit_verified", "mission_ready", "native_acceptance",
                 "catalog_mutated", "native_playable", "validation_passed"):
        need(author.get(flag) is False, "Compiler result must retain " + flag + "=false")
    need(author.get("partial_authoring_capture_for_missing_binding") is False and
         author.get("compiler_reasons") == [] and author.get("authoring_capture_reasons") == [],
         "Complete accepted compiler-selected source capture required")

    need(identity.get("kind") == "AuthoredUnitPrefabIdentityReadback" and
         identity.get("catalog_id") == selection["catalog_id"] and identity.get("seed") == selection["seed"] and
         identity.get("setup_hash") == selection["setup_hash"] and
         identity.get("metadata_sha256") == metadata_sha and identity.get("units") == 16 and
         identity.get("runtime_templates_verified") is False and identity.get("mission_ready") is False,
         "Independent unit identity readback must bind these exact metadata bytes and compiler context")

    rows = units.get("units")
    identity_rows = identity.get("identities")
    need(isinstance(rows, list) and len(rows) == 16 and isinstance(identity_rows, list) and len(identity_rows) == 16,
         "Exact 16-row actual unit owner and independent identity domain required")
    identity_by_key = {}
    for row in identity_rows:
        key = row.get("key") if isinstance(row, dict) else None
        need(isinstance(key, str) and key not in identity_by_key, "Unique independent prefab identity key required")
        identity_by_key[key] = row
    keys = []
    source_pins = units["source_pins"]
    for index, row in enumerate(rows):
        need(row.get("index") == index and row.get("has_definition_metadata") is True and
             row.get("has_production_metadata") is True,
             "Ordered original factory metadata row required at index " + str(index))
        properties = row.get("authoring_catalog_properties", {}).get("fields", {})
        need(properties.get("CanRequest") == {"codec": "bool-v1", "value": True},
             "Every focused unit key must come from an explicit CanRequest=true source property")
        prefab = row.get("prefab")
        key = row.get("key")
        need(isinstance(prefab, dict) and prefab.get("codec") == "source-asset-reference-v1" and
             prefab.get("type") == "UnityEngine.GameObject" and prefab.get("key") == key,
             "Exact original unit prefab identity required at index " + str(index))
        need(isinstance(key, str) and key == key.strip() and key not in keys,
             "Canonical unique requestable unit key required")
        ident = identity_by_key.get(key)
        need(ident is not None and ident.get("path") == prefab.get("path") and
             ident.get("guid") == prefab.get("guid") and ident.get("local_id") == prefab.get("local_id"),
             "Independent AssetDatabase identity differs from original factory prefab: " + str(key))
        asset_path = prefab.get("path")
        need(isinstance(asset_path, str) and asset_path.startswith("Assets/Game/Prefabs/") and
             asset_path.endswith(".prefab") and ".." not in Path(asset_path).parts,
             "Canonical project unit prefab path required: " + str(key))
        absolute = str((ROOT / asset_path).resolve())
        need(isinstance(source_pins.get(absolute), str) and
             re.fullmatch(r"[0-9a-f]{64}", source_pins[absolute]) is not None and
             isinstance(source_pins.get(absolute + ".meta"), str) and
             re.fullmatch(r"[0-9a-f]{64}", source_pins[absolute + ".meta"]) is not None,
             "Current unit prefab and .meta source pins required: " + str(key))
        keys.append(key)
    need(key_document == keys and len(keys) == len(set(keys)),
         "Hash-bound focused keys must equal all 16 actual CanRequest prefabs in metadata order")
    return {
        "mode": "OriginalConfiguredAuthoringFactories_CompilerSelected",
        "identity_readback_sha256": identity.get("metadata_sha256"),
        "identity_count": len(identity_rows),
        "requestable_unit_count": len(keys),
        "keys": keys,
        "compiler_result": compiler_result_ref,
        "physical_input_request": physical_request_ref,
        "logical_layout_binding_fingerprint": selection["layout_fingerprint"],
        "compiled_layout_fingerprint": author["layout_fingerprint"],
        "compiler_source_policy_hash": selection["source_content_hash"],
        "physical_map_content_hash": author["source_content_hash"],
        "source_pin_count": len(source_pins),
    }


def validate_unsupported(raw):
    entities = raw.get("entities")
    records = raw.get("unsupported_values")
    need(isinstance(entities, list) and isinstance(records, list), "Captured entities and unsupported rows required")
    by_id = {}
    for entity in entities:
        eid = entity.get("id")
        need(isinstance(eid, str) and re.fullmatch(r"(?:0|[1-9][0-9]*):(?:0|[1-9][0-9]*)", eid),
             "Canonical captured Entity id required")
        need(eid not in by_id, "Duplicate captured Entity id")
        by_id[eid] = entity
    seen = set()
    field_counts = {}
    component_counts = {}
    for entity in entities:
        for component in ALLOWED:
            if component in entity.get("components", {}):
                component_counts[component] = component_counts.get(component, 0) + 1
    for row in records:
        path_text = row.get("path", "")
        parts = path_text.split("/")
        need(len(parts) == 3 and parts[0] in by_id and parts[1] in ALLOWED and parts[2] in ALLOWED[parts[1]],
             "Unsupported row outside existing importer presentation allowlist")
        need(path_text not in seen, "Duplicate unsupported row path")
        seen.add(path_text)
        need(row.get("reason") == "unity_object_ref_requires_bound_source_identity" and
             row.get("type") == OBJECT_REF_CAPTURE, "Unsupported presentation row has unexpected reason/type")
        component = by_id[parts[0]].get("components", {}).get(parts[1])
        value = component.get("value", {}) if isinstance(component, dict) else {}
        field = value.get("fields", {}).get(parts[1] + "::" + parts[2], {})
        need(value.get("codec") == "struct-fields-v1" and field == {
            "codec": "unsupported-v1", "type": OBJECT_REF_CAPTURE,
            "reason": "unity_object_ref_requires_bound_source_identity"},
            "Unsupported row does not match captured component field")
        key = parts[1] + "/" + parts[2]
        field_counts[key] = field_counts.get(key, 0) + 1
    return field_counts, component_counts


def verify_inputs(raw_path, wrapper_log_path, key_path, authoring_result_path,
                  context_path, qualification_path, graph_path, building_path,
                  unit_path, request_path):
    paths = [raw_path, wrapper_log_path, key_path, authoring_result_path, context_path,
             qualification_path, graph_path, building_path, unit_path, request_path]
    for path in paths:
        need(Path(path).is_file() and not Path(path).is_symlink(), "Pinned input unavailable: " + str(path))
    raw_bytes = Path(raw_path).read_bytes()
    raw = read(raw_path)
    log_text = Path(wrapper_log_path).read_text(errors="replace")
    keys = read(key_path)
    author = read(authoring_result_path)
    context = read(context_path)
    qualification = read(qualification_path)
    graph = read(graph_path)
    buildings = read(building_path)
    units = read(unit_path)
    request = read(request_path)
    keys = read(key_path)
    target = context.get("target_context", {})
    catalog_id = target.get("catalog_id")
    need(isinstance(catalog_id, str) and re.fullmatch(r"S(?:00[1-9]|0[1-9][0-9]|1[01][0-9]|120)", catalog_id),
         "Canonical S001-S120 compiler target required")
    setup_hash = target.get("setup_hash")
    need(isinstance(setup_hash, int) and setup_hash > 0, "Compiler setup hash required")
    need(context.get("kind") == "OriginalCompilerDerivedNativeFactoryContext" and
         context.get("world_allocated") is False and context.get("mission_ready") is False and
         context.get("source_fit_certified") is False, "Source-only original compiler context required")
    need(qualification.get("kind") == "ExperimentalGroundConfigurationQualification" and
         qualification.get("compiler_context") == descriptor(context_path) and
         qualification.get("native_context_qualified") is False and qualification.get("mission_ready") is False,
         "Current compiler qualification bound to context required")
    need(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
         graph.get("world_initialized") is False and graph.get("native_world_state_used") is False,
         "Current uncompiled authored source graph required")
    need(buildings.get("catalog_id") == catalog_id and buildings.get("setup_hash") == setup_hash and
         buildings.get("native_S050_capture") is False and buildings.get("full_mission_initialized") is False and
         buildings.get("native_visual_readiness") is False,
         "Current source-only building metadata target required")
    unit_owner_evidence = None
    key_pin = descriptor(key_path)
    if units.get("kind") == "AuthoredSkirmishUnitProductionMetadata":
        identity_path = Path(unit_path).parent / "unit-prefab-identity-readback.json"
        need(identity_path.is_file() and not identity_path.is_symlink(),
             "Independent direct unit-owner identity readback required")
        identity = read(identity_path)
        physical_ref = units.get("compiler_selection", {}).get("physical_input_request", {})
        need(isinstance(physical_ref, dict) and isinstance(physical_ref.get("path"), str),
             "Direct unit-owner exact physical request path required")
        physical_path = Path(physical_ref["path"])
        physical_request = read(physical_path)
        unit_owner_evidence = validate_actual_owner_unit_metadata(
            units, identity, sha(Path(unit_path).read_bytes()), author, authoring_result_path,
            physical_request, physical_path, context, keys)
        unit_owner_evidence["identity_readback"] = descriptor(identity_path)
        verify_source_pins(units.get("source_pins"), ROOT)
        need(request.get("focused_unit_prefab_keys") == keys and
             request.get("death_readset_unit_prefab_keys") == keys and
             request.get("focused_unit_key_descriptor", {}).get("sha256") == key_pin["sha256"] and
             request.get("death_readset_unit_key_descriptor", {}).get("sha256") == key_pin["sha256"],
             "Capture request must bind the exact direct unit-owner key bytes")
    else:
        need(units.get("catalog_id") == catalog_id and units.get("setup_hash") == setup_hash and
             units.get("native_S050_capture") is False and units.get("full_mission_initialized") is False and
             units.get("native_visual_readiness") is False,
             "Current source-only unit metadata target required")
    need(author.get("catalog_id") == catalog_id and author.get("compiler_accepted") is True and
         author.get("resolved_setup", {}).get("setup_hash") == setup_hash and
         author.get("world_allocated") is False and author.get("mission_ready") is False,
         "Accepted source authoring result matching compiler target required")
    expected_keys = keys if unit_owner_evidence is not None else extract_unit_keys(units)
    need(isinstance(keys, list) and keys == expected_keys and len(keys) <= 64,
         "Hash-bound focused unit keys must equal current unit metadata order")
    capture_path = Path(raw_path).resolve(strict=True)
    log_path = Path(wrapper_log_path).resolve(strict=True)
    need(raw.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         raw.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
         raw.get("source_scenes") == ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"] and
         raw.get("requested_prefab_keys") == expected_keys and
         raw.get("requested_prefab_keys_sha256") == key_pin["sha256"] and
         raw.get("source_pins", {}).get(key_pin["path"]) == key_pin["sha256"],
         "Raw capture does not bind exact current unit keys/input descriptor")
    need(len(raw.get("opening_prefabs", {})) == len(expected_keys) and
         len(raw.get("entities", [])) > 0 and raw.get("replay_ready") is False and
         raw.get("fresh_world_allocator_exported") is False and
         raw.get("full_mission_initialized") is False and raw.get("native_visual_readiness") is False and
         raw.get("geometry_exported") is False,
         "Raw capture scope/readiness flags changed")
    field_counts, component_counts = validate_unsupported(raw)
    need(len(raw.get("unsupported_values", [])) > 0, "This projector is only for retained failed captures")
    if unit_owner_evidence is not None:
        need(logged_argument_matches(log_text, "-skirmishFocusedPrefabKeys", key_pin["path"]) and
             logged_argument_matches(log_text, "-skirmishFocusedPrefabKeysSha", key_pin["sha256"]),
             "Current failed wrapper log must bind the exact raw-pinned key descriptor path and bytes")
    need(f"InvalidOperationException: Focused prefab reference contains unsupported values: {len(raw['unsupported_values'])}" in log_text and
         "ExitCode: 0" in log_text and "[SkirmishFocusedPrefabReferenceData] result=Passed" not in log_text and
         "Game.Editor.SkirmishBakedActorSnapshotExporter.RunFocusedPrefabReferenceData" in log_text and
         key_pin["sha256"] in log_text,
         "Raw exporter failure log does not match the retained capture/key input")
    verify_source_pins(raw.get("source_pins"), ROOT)
    return raw, raw_bytes, field_counts, component_counts, target, key_pin, unit_owner_evidence


def run(output, raw_path, wrapper_log_path, key_path, authoring_result_path, context_path,
        qualification_path, graph_path, building_path, unit_path, request_path):
    raw, raw_bytes, field_counts, component_counts, target, key_pin, unit_owner_evidence = verify_inputs(
        raw_path, wrapper_log_path, key_path, authoring_result_path, context_path,
        qualification_path, graph_path, building_path, unit_path, request_path)
    output = Path(output).resolve()
    need(output.is_relative_to(ROOT) and not output.exists(), "Fresh projection output inside source root required")
    output.mkdir(parents=True)
    raw_desc = descriptor(raw_path)
    copied_log = output / "capture-wrapper.log"
    copied_log.write_bytes(Path(wrapper_log_path).read_bytes())
    proof = {
        "schema_version": 1, "kind": "FocusedPrefabSourceOnlyCaptureProof",
        "status": "ProjectedSourceOnlyFromFailedRawCapture",
        "scope": "Exact current unit metadata prefab closure; only the original importer's presentation-only UnityObjectRef exclusions are admitted.",
        "raw_capture": raw_desc, "raw_capture_result": "Failed_UnconsumedPresentationUnityObjectRefs",
        "raw_capture_wrapper_exit_code": 0, "raw_capture_pass_marker_present": False,
        "wrapper_log": descriptor(copied_log), "unit_key_input": key_pin,
        "source_context": descriptor(context_path), "configuration_qualification": descriptor(qualification_path),
        "source_config_graph": descriptor(graph_path), "building_definitions": descriptor(building_path),
        "unit_production_metadata": descriptor(unit_path), "authoring_result": descriptor(authoring_result_path),
        "capture_request": descriptor(request_path),
        "target": {k: target.get(k) for k in ("catalog_id", "seed", "size", "difficulty", "setup_hash", "operation_map_id", "source_content_hash")},
        "source_pin_count": len(raw["source_pins"]), "source_pins_verified_current": True,
        "unsupported_vfx_rows_preserved": len(raw["unsupported_values"]),
        "exact_unconsumed_fields": field_counts,
        "presentation_components_omitted_by_existing_importer": component_counts,
        "raw_capture_replay_ready": False, "runtime_import_attempted": False,
        "runtime_admission": False, "native_visual_readiness": False,
        "source_fit_certified": False, "mission_ready": False,
    }
    attach_unit_owner_evidence(proof, unit_owner_evidence)
    proof_path = output / "focused-prefab-capture-proof.json"
    proof_path.write_bytes((json.dumps(proof, indent=2, ensure_ascii=False) + "\n").encode())
    proof_desc = descriptor(proof_path)
    domain = {key: raw[key] for key in FIELDS}
    domain["source_reference_sha256"] = raw_desc["sha256"]
    domain["derived_read_domain"] = "ExactTypedPrefabClosureOnly"
    domain["raw_capture_status"] = "Failed_UnconsumedPresentationUnityObjectRefs"
    domain["capture_proof"] = proof_desc
    slice_path = output / "focused-prefab-domain.json"
    slice_bytes = (json.dumps(domain, indent=2, ensure_ascii=False) + "\n").encode()
    slice_path.write_bytes(slice_bytes)
    manifest = {
        "schema_version": 1, "kind": "PinnedExactTemplateDomainProjection",
        "source_path": raw_desc["path"], "source_sha256": raw_desc["sha256"],
        "source_bytes": len(raw_bytes), "slice_path": str(slice_path.resolve()),
        "slice_sha256": sha(slice_bytes), "slice_bytes": len(slice_bytes),
        "selected_fields": FIELDS, "root_count": len(raw["opening_prefabs"]),
        "entity_count": len(raw["entities"]), "template_namespace_id": raw["template_namespace_id"],
        "full_Unity_replay_ready": False, "unsupported_values_preserved": len(raw["unsupported_values"]),
        "unsupported_by_type": {"UnityObjectRef<GameObject>": len(raw["unsupported_values"])},
        "consumer_read_domain": "ExactTypedPrefabClosureOnly; only importer-declared presentation VFX components are omitted.",
        "capture_proof": proof_desc,
        "raw_capture_status": "Failed_UnconsumedPresentationUnityObjectRefs",
        "world_created": False, "runtime_import_attempted": False,
        "source_fit_certified": False, "native_visual_readiness": False, "mission_ready": False,
    }
    manifest_path = output / "template-slice-manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n")
    manifest_desc = descriptor(manifest_path)
    descriptor_value = {
        **descriptor(slice_path), "source_reference_path": raw_desc["path"],
        "source_reference_sha256": raw_desc["sha256"],
        "projection_manifest_path": manifest_desc["path"],
        "projection_manifest_sha256": manifest_desc["sha256"],
        "source_only_capture_proof": proof_desc,
    }
    descriptor_path = output / "reference-template-descriptor.json"
    descriptor_path.write_text(json.dumps(descriptor_value, indent=2, ensure_ascii=False) + "\n")
    receipt = {
        "schema_version": 1, "kind": "FocusedPrefabSourceOnlyProjectionReceipt",
        "status": "ProjectedSourceOnlyFromFailedRawCapture",
        "raw_capture": raw_desc, "raw_capture_sha256": raw_desc["sha256"],
        "raw_capture_failure_preserved": True, "raw_capture_replay_ready": False,
        "projection_descriptor": descriptor(descriptor_path),
        "source_pin_count": len(raw["source_pins"]),
        "unsupported_vfx_rows_preserved": len(raw["unsupported_values"]),
        "runtime_import_attempted": False, "runtime_admission": False,
        "native_visual_readiness": False, "source_fit_certified": False, "mission_ready": False,
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2, ensure_ascii=False) + "\n")
    print(json.dumps(receipt, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("raw", "wrapper-log", "key-descriptor", "authoring-result", "context",
                 "qualification", "graph", "building-definitions", "unit-metadata",
                 "capture-request", "output"):
        parser.add_argument("--" + name, required=True)
    args = parser.parse_args()
    run(args.output, args.raw, args.wrapper_log, args.key_descriptor, args.authoring_result,
        args.context, args.qualification, args.graph, args.building_definitions,
        args.unit_metadata, args.capture_request)


if __name__ == "__main__":
    main()
