#!/usr/bin/env python3
"""Recheck compiler-selected S050 direct Unity metadata captures before projection."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
PRESET_REL = "Assets/Game/Resources/SkirmishBaseAssault.asset"
PRESET_SHA = "ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe"
PRESET_GUID = "fb725bf44513544db88fcde194df7350"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def desc(path):
    path = Path(path).resolve(strict=True)
    if path.is_symlink() or not path.is_file() or not path.is_relative_to(ROOT):
        raise ValueError("Repository-owned regular input required: " + str(path))
    return {"path": str(path), "sha256": sha(path)}


def external_log_desc(path):
    path = Path(path).resolve(strict=True)
    need(path.is_file() and not path.is_symlink(), "Original Unity log must be a regular file")
    return {"path": str(path), "sha256": sha(path)}


def read(path):
    return json.loads(Path(path).read_bytes())


def need(value, message):
    if not value:
        raise ValueError(message)


def verify_pin_map(pins):
    need(isinstance(pins, dict) and pins, "Direct owner source pin closure required")
    mismatches = []
    for name, expected in pins.items():
        path = Path(name)
        path = path if path.is_absolute() else ROOT / path
        if path.is_symlink() or not path.is_file() or sha(path) != expected:
            mismatches.append(name)
    return mismatches


def audit(owner, metadata_path, request_path, result_path, graph_path, physical_path,
          physical_audit_path, context_path, log_path, original_log_path, output_path):
    need(owner in ("unit", "building"), "Exact unit or building owner required")
    metadata_path = Path(metadata_path).resolve(strict=True)
    request_path = Path(request_path).resolve(strict=True)
    result_path = Path(result_path).resolve(strict=True)
    graph_path = Path(graph_path).resolve(strict=True)
    physical_path = Path(physical_path).resolve(strict=True)
    physical_audit_path = Path(physical_audit_path).resolve(strict=True)
    context_path = Path(context_path).resolve(strict=True)
    log_path = Path(log_path).resolve(strict=True)
    original_log_path = Path(original_log_path).resolve(strict=True)
    output_path = Path(output_path).absolute()
    need(output_path.is_relative_to(ROOT) and not output_path.exists(), "Fresh repository-owned audit path required")
    metadata = read(metadata_path)
    request = read(request_path)
    result = read(result_path)
    graph = read(graph_path)
    physical = read(physical_path)
    physical_audit = read(physical_audit_path)
    context = read(context_path)
    author_request_path = result_path.parent / "authoring-request.json"
    author_request = read(author_request_path)
    selection_desc = desc(result_path)
    request_desc = desc(request_path)
    metadata_desc = desc(metadata_path)
    request_selection = request.get("selection", {})
    preset_desc = author_request.get("resource_preset")
    preset_path = (ROOT / PRESET_REL).resolve(strict=True)
    need(request_selection == {"path": str(result_path), "sha256": selection_desc["sha256"]},
         "Physical request must select exact current author result")
    need(result.get("kind") == "SkirmishMissionAuthoringPreflightResult" and
         result.get("status") == "ConfigurationCompiled" and result.get("compiler_accepted") is True and
         result.get("catalog_id") == "S050" and result.get("seed") == 104779 and
         result.get("operation_map_id") == request.get("operation_map_id") and
         result.get("world_allocated") is False and result.get("source_fit_verified") is False and
         result.get("mission_ready") is False and result.get("native_acceptance") is False,
         "Unqualified exact compiler selection required")
    need(preset_desc == {"path": str(preset_path), "sha256": PRESET_SHA} and
         sha(preset_path) == PRESET_SHA and
         result.get("input_pins", {}).get(str(preset_path), {}).get("sha256") == PRESET_SHA,
         "Current author result must pin exact BaseAssault source asset")
    target_hash = result.get("resolved_setup", {}).get("setup_hash")
    need(type(target_hash) is int and metadata.get("catalog_id") == "S050" and
         metadata.get("seed") == 104779 and metadata.get("setup_hash") == target_hash,
         "Direct metadata must match exact S050 compiler setup identity")
    need(metadata.get("kind") == ("AuthoredSkirmishUnitProductionMetadata" if owner == "unit"
                                    else "AuthoredSkirmishBuildingDefinitions") and
         metadata.get("unsupported_values") == [] and metadata.get("full_mission_initialized") is False and
         metadata.get("native_visual_readiness") is False,
         "Unqualified exact direct owner output required")
    compiler = metadata.get("compiler_selection", {})
    expected_selection = {
        "result": selection_desc, "physical_input_request": request_desc,
        "catalog_id": "S050", "seed": 104779, "size": result.get("size"),
        "difficulty": result.get("difficulty"), "setup_hash": target_hash,
        "operation_map_id": result.get("operation_map_id"),
        "source_fit_certified": False, "mission_ready": False,
        "world_allocated": False, "native_acceptance": False,
    }
    need(all(compiler.get(k) == v for k, v in expected_selection.items()),
         "Direct owner compiler selection differs from request/result")
    layout_desc = author_request.get("layout", {})
    binding_desc = author_request.get("binding", {})
    binding_path = Path(binding_desc.get("path", "")).resolve(strict=True)
    need(sha(binding_path) == binding_desc.get("sha256") and
         sha(layout_desc["path"]) == layout_desc["sha256"], "Current layout and binding request pins required")
    target = context.get("target_context", {})
    need(context.get("kind") == "OriginalCompilerDerivedNativeFactoryContext" and
         context.get("world_allocated") is False and context.get("native_S050_capture") is False and
         context.get("source_fit_certified") is False and context.get("mission_ready") is False and
         compiler.get("layout_fingerprint") == target.get("layout_binding_fingerprint") and
         result.get("layout_fingerprint") == target.get("layout_fingerprint") and
         target.get("setup_hash") == target_hash,
         "Direct owner selection/result differs from exact current compiler context")
    need(result.get("input_pins", {}).get(str(layout_desc["path"]), {}).get("sha256") == layout_desc["sha256"] and
         result.get("input_pins", {}).get(str(binding_path), {}).get("sha256") == binding_desc["sha256"] and
         result.get("input_pins", {}).get(str(author_request_path), {}).get("sha256") == sha(author_request_path),
         "Author result does not pin current layout and logical binding")
    need(result.get("source_content_hash") == request.get("content_hash") and
         result.get("operation_map_id") == physical.get("operationMapId") == request.get("operation_map_id") and
         physical.get("sourceSha256") == request.get("source_sha256") and
         physical.get("sourceFitCertified") is False and physical.get("missionReady") is False,
         "Physical source request, readback, and author result do not join")
    semantic_hash = compiler.get("source_content_hash")
    expected_prefix = "skirmish.s050.v2/enemy-policy-v1/"
    need(isinstance(semantic_hash, str) and semantic_hash.startswith(expected_prefix) and
         len(semantic_hash) == len(expected_prefix) + 64,
         "Owner semantic setup identity is not canonical")
    graph_pins = graph.get("source_pins", {})
    preset_nodes = [row for row in graph.get("nodes", []) if row.get("source_path") == PRESET_REL]
    need(graph_pins.get(str(preset_path)) == PRESET_SHA and len(preset_nodes) == 1 and
         preset_nodes[0].get("source_type") == "SkirmishPresetConfig" and
         preset_nodes[0].get("source_sha256") == PRESET_SHA and
         preset_nodes[0].get("fields", {}).get("supplyDaySeconds") == 120 and
         preset_nodes[0].get("scalar_spellings", {}).get("supplyDaySeconds") == "120",
         "Exact source graph BaseAssault selection and supply scalar required")
    need(result.get("input_pins", {}).get(str(graph_path), {}).get("sha256") == sha(graph_path),
         "Author result must pin exact source graph")
    physical_pin_mismatches = verify_pin_map(physical.get("readPins", {}))
    need(physical_audit.get("status") == "CapturedSourceRows" and
         physical_audit.get("physical_reference", {}).get("sha256") == sha(physical_path) and
         physical_audit.get("request", {}).get("sha256") == sha(request_path) and
         physical_audit.get("selected_author_result", {}).get("sha256") == sha(result_path) and
         physical_audit.get("required_pass_marker_count") == 1 and
         physical_audit.get("read_pin_count") == 3753 and
         physical_audit.get("read_pin_mismatches") == [] and not physical_pin_mismatches,
         "Current audited physical source readset required")
    asset_readback = metadata.get("compiler_selection_asset_readback", {})
    identity_rows = asset_readback.get("asset_database_guid_local_id_rows", [])
    preset_rows = [row for row in identity_rows if row.get("source_path") == PRESET_REL]
    excluded = asset_readback.get("excluded_unhydrated_graph_nodes", [])
    need(asset_readback.get("kind") == "UnityAssetDatabaseCompilerGraphIdentityReadback" and
         asset_readback.get("status") == "VerifiedAssetIdentityOnly" and
         asset_readback.get("asset_field_value_parity_proven") is False and
         len(preset_rows) == 1 and preset_rows[0].get("asset_guid") == PRESET_GUID and
         preset_rows[0].get("source_sha256") == PRESET_SHA and
         preset_rows[0].get("actual_local_file_id") == 11400000 and
         {"asset_guid": PRESET_GUID, "source_path": PRESET_REL} in excluded,
         "Unity readback must prove asset identity only and show the preset node remained unhydrated")
    source_pins = metadata.get("source_pins", {})
    mismatches = verify_pin_map(source_pins)
    result_pin_rows = result.get("input_pins", {})
    missing = [path for path, row in result_pin_rows.items()
               if source_pins.get(path) != row.get("sha256")]
    need(not mismatches and not missing, "Owner source closure or compiler input pin differs")
    need(source_pins.get(str(request_path)) == request_desc["sha256"] and
         source_pins.get(str(result_path)) == selection_desc["sha256"] and
         source_pins.get(str(author_request_path)) == sha(author_request_path) and
         source_pins.get(str(preset_path)) == PRESET_SHA,
         "Owner source closure must pin physical request, compiler result and preset")
    log_bytes = log_path.read_bytes()
    need(log_bytes == original_log_path.read_bytes(), "Durable Unity log must byte-match original capture log")
    log_text = log_bytes.decode("utf-8", errors="replace")
    expected_markers = ([
        "[SkirmishAuthoredUnitProductionMetadata] result=Passed units=16 transports=1 unsupported=0 fullMission=0",
        "[SkirmishAuthoredUnitIdentityAudit] result=Passed units=16 distinct=16 transports=1 runtimeTemplates=0 readiness=0",
    ] if owner == "unit" else [
        "[SkirmishAuthoredBuildingDefinitions] result=Passed definitions=9 unsupported=0 fullMission=0",
    ])
    marker_counts = {marker: log_text.splitlines().count(marker) for marker in expected_markers}
    need(all(count == 1 for count in marker_counts.values()) and "Batch mode:              NO" in log_text and
         "-batchmode" not in log_text, "Exact pass marker and GUI-mode Unity capture required")
    if owner == "unit":
        identity_path = metadata_path.with_name("unit-prefab-identity-readback.json")
        identity = read(identity_path)
        metadata_units = metadata.get("units", [])
        identity_rows = identity.get("identities", [])
        identity_by_key = {row.get("key"): row for row in identity_rows}
        prefab_identity_rows = [
            {"key": row.get("key"), "path": row.get("path"), "guid": row.get("guid"),
             "local_id": row.get("local_id")}
            for row in identity_rows
        ]
        expected_identity_rows = [
            {key: row.get("prefab", {}).get(key)
             for key in ("key", "path", "guid", "local_id")}
            for row in metadata_units
        ]
        need(identity.get("kind") == "AuthoredUnitPrefabIdentityReadback" and
             identity.get("metadata_sha256") == metadata_desc["sha256"] and
             identity.get("catalog_id") == "S050" and identity.get("seed") == 104779 and
             identity.get("setup_hash") == target_hash and identity.get("runtime_templates_verified") is False and
             identity.get("mission_ready") is False and
             identity.get("units") == len(metadata_units) and
             identity.get("transports") == len(metadata.get("transports", [])) and
             len(identity_rows) == len(metadata_units) and
             len(identity_by_key) == len(identity_rows) and
             sorted(prefab_identity_rows, key=lambda row: row["key"]) ==
             sorted(expected_identity_rows, key=lambda row: row["key"]),
             "Unit identity readback must join exact owner metadata")
    else:
        identity_path = None
        need(len(metadata.get("definitions", [])) == 9 and
             len({row.get("key") for row in metadata["definitions"]}) == 9,
             "Nine unique original building definitions required")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    owner_output = ({"units": len(metadata.get("units", [])),
                    "transports": len(metadata.get("transports", [])),
                    "unsupported_values": len(metadata["unsupported_values"]),
                    "source_preset_index": metadata.get("source_preset_index"),
                    "full_mission_initialized": metadata["full_mission_initialized"],
                    "native_visual_readiness": metadata["native_visual_readiness"]}
                   if owner == "unit" else
                   {"definitions": len(metadata["definitions"]),
                    "unsupported_values": len(metadata["unsupported_values"]),
                    "full_mission_initialized": metadata["full_mission_initialized"],
                    "native_visual_readiness": metadata["native_visual_readiness"]})
    checks = {"author_result_sha": True, "physical_request_sha": True,
              "preset_descriptor_matches": True, "preset_value_is_120": True,
              "preset_result_input_pin": True, "graph_result_input_pin": True,
              "physical_map_matches_compiler": True, "compiler_selection_author_result": True,
              f"{owner}_unity_assetdb_preset_identity": True,
              f"{owner}_preset_node_not_hydrated": True,
              f"{owner}_all_source_pins_current": True,
              f"{owner}_pins_compiler_result": True,
              f"{owner}_pins_preset_asset": True,
              f"{owner}_pins_physical_request": True,
              f"{owner}_exact_pass_markers": True,
              f"{owner}_editor_gui_mode": True,
              f"{owner}_durable_log_byte_match": True,
              "unity_asset_field_parity_not_claimed": True}
    if owner == "unit":
        checks.update({"unit_identity_metadata_sha": True, "unit_prefab_identity_joins": True})
    audit = {
        "schema_version": 1, "kind": "S050CompilerSelectedPresetUnityOwnerAudit",
        "owner": owner, "status": "Passed",
        "unity_log": desc(log_path), "source_log": external_log_desc(original_log_path),
        "owner_output": owner_output, "owner_metadata": metadata_desc,
        "compiler_selection": {k: compiler[k] for k in
            ("result", "physical_input_request", "catalog_id", "seed", "size", "difficulty",
             "setup_hash", "operation_map_id", "source_content_hash", "layout_fingerprint", "status")},
        "physical_source": {"request_path": str(request_path), "request_sha256": request_desc["sha256"],
                            "operation_map_id": physical["operationMapId"], "content_hash": request["content_hash"]},
        "preset": {"path": str(preset_path), "sha256": PRESET_SHA,
                   "supply_day_seconds_from_pinned_source_graph": 120,
                   "unity_field_value_parity_proven": False,
                   "compiler_result_input_pin": {"sha256": PRESET_SHA, "maximum_bytes": 4194304}},
        "compiler_selection_asset_readback": {"kind": asset_readback["kind"], "status": asset_readback["status"],
            "preset_asset_row": preset_rows[0], "asset_field_value_parity_proven": False},
        "source_pin_count": len(source_pins), "source_pin_missing_count": 0,
        "source_pin_changed_count": 0, "source_pin_mismatches": [],
        "source_pin_audit": {"compiler_result_input_pin_count": len(result_pin_rows),
            "compiler_result_input_pins_missing_from_owner_closure": 0,
            "compiler_result_input_pin_differences": 0, "all_owner_source_pins_current": True},
        "pass_marker_counts": {f"marker_{i + 1}": count for i, count in enumerate(marker_counts.values())},
        "checks": checks,
        "nonclaims": {"world_allocated": False, "runtime_admission": False,
            "source_fit_certified": False, "mission_ready": False, "native_acceptance": False,
            "unity_asset_field_value_parity_proven": False},
        "owner_semantic_source_content_hash": semantic_hash,
        "scope": "Direct original factory metadata for the exact current S050 compiler-selected BaseAssault request/result. AssetDatabase identity only; no asset field parity, runtime placement, source fit or readiness.",
    }
    if identity_path is not None:
        audit["identity_readback"] = desc(identity_path)
    output_path.write_text(json.dumps(audit, indent=2) + "\n")
    return audit


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--owner", required=True, choices=("unit", "building"))
    p.add_argument("--metadata", required=True, type=Path)
    p.add_argument("--request", required=True, type=Path)
    p.add_argument("--result", required=True, type=Path)
    p.add_argument("--graph", required=True, type=Path)
    p.add_argument("--physical-reference", required=True, type=Path)
    p.add_argument("--physical-audit", required=True, type=Path)
    p.add_argument("--context", required=True, type=Path)
    p.add_argument("--unity-log", required=True, type=Path)
    p.add_argument("--source-unity-log", required=True, type=Path)
    p.add_argument("--output", required=True, type=Path)
    args = p.parse_args()
    result = audit(args.owner, args.metadata, args.request, args.result, args.graph,
                   args.physical_reference, args.physical_audit, args.context,
                   args.unity_log, args.source_unity_log, args.output)
    print(json.dumps({"status": result["status"], "owner": result["owner"],
                      "source_pin_count": result["source_pin_count"],
                      "pass_marker_counts": result["pass_marker_counts"],
                      "mission_ready": result["nonclaims"]["mission_ready"]}))


if __name__ == "__main__":
    main()
