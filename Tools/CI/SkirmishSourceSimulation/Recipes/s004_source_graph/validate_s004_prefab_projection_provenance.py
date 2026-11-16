#!/usr/bin/env python3
"""Fail-closed provenance gate for the S004 source-only prefab diagnostic."""
import copy
import hashlib
import json
from pathlib import Path


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(ok, message):
    if not ok:
        raise ValueError(message)


def read(path):
    return json.loads(Path(path).read_text())


def verify_hash_map(pins):
    need(isinstance(pins, dict) and pins, "nonempty source pin map required")
    for raw_path, digest in pins.items():
        path = Path(raw_path)
        need(path.is_file() and len(digest) == 64 and sha(path) == digest,
             "current pinned source changed or unavailable: " + raw_path)


def validate_entity_projection(raw, p):
    # The only allowed data change is deletion of the 16 reviewed unsupported leaves.
    exclusions = p.get("excluded_presentation_object_refs")
    need(isinstance(exclusions, list) and len(exclusions) == 16 and p.get("excluded_count") == 16 and
         p.get("remaining_unsupported_values") == [], "exact reviewed exclusion ledger required")
    expected = copy.deepcopy(raw["entities"])
    expected_by_id = {row["id"]: row for row in expected}
    need(len(expected_by_id) == len(expected), "raw entity identities must be unique")
    seen = set()
    for item in exclusions:
        path = item.get("path", "").split("/")
        need(len(path) == 3 and path[0] + "/" + path[1] + "/" + path[2] not in seen,
             "exclusion path must be unique and exact")
        seen.add(path[0] + "/" + path[1] + "/" + path[2])
        eid, component, field = path
        entry = expected_by_id.get(eid, {}).get("components", {}).get(component)
        field_key = component + "::" + field
        encoded = entry.get("value", {}).get("fields", {}).get(field_key) if entry else None
        need(entry is not None and entry.get("present") is True and entry.get("buffer") is False and
             encoded == {"codec": "unsupported-v1", "reason": item.get("reason"), "type": item.get("type")} and
             item.get("component") == component and item.get("field") == field,
             "exclusion ledger does not match exact raw unsupported leaf")
        del entry["value"]["fields"][field_key]
    need(expected == p.get("entities"),
         "projected gameplay or structural value differs from raw capture beyond exact exclusion leaves")
    need(p.get("opening_prefabs") == raw.get("opening_prefabs"), "root identity map changed")
    return len(exclusions)


def gameplay_mutation_control(raw, p):
    probe = copy.deepcopy(p)
    changed = False
    for entity in probe["entities"]:
        for component in entity["components"].values():
            fields = component.get("value", {}).get("fields", {})
            for value in fields.values():
                if value.get("codec") == "f32-bits-v1":
                    value["bits"] = "00000000" if value.get("bits") != "00000000" else "3f800000"
                    changed = True
                    break
            if changed:
                break
        if changed:
            break
    need(changed, "gameplay mutation control requires a captured scalar field")
    try:
        validate_entity_projection(raw, probe)
    except ValueError:
        return {"case": "gameplay-field-mutation-rejected", "result": "Passed"}
    raise ValueError("gameplay-field mutation control unexpectedly passed")


def validate(repo, projection_path, preflight_receipt_path):
    repo = Path(repo).resolve()
    projection_path = Path(projection_path).resolve()
    p = read(projection_path)
    need(p.get("kind") == "S004FocusedPrefabSourceOnlyPresentationExcludedProjection" and
         p.get("status") == "PassedSourceOnlyProjection" and p.get("source_authority") ==
         "ActualSourceAssetsAndNativeBakers_NotMissionState" and
         p.get("runtime_import_attempted") is False and p.get("runtime_replay_ready") is False and
         p.get("mission_ready") is False and p.get("native_visual_readiness") is False and
         p.get("geometry_exported") is False, "source-only projection status required")

    origin = p["original_capture"]
    raw_path = repo / origin["path"]
    failure_path = repo / origin["failure_receipt"]
    log_path = repo / origin["editor_log"]
    need(origin.get("status") == "Failed" and origin.get("raw_artifact_admitted") is False and
         sha(raw_path) == origin.get("sha256") and sha(failure_path) == origin.get("failure_receipt_sha256") and
         sha(log_path) == origin.get("editor_log_sha256"), "failed raw capture provenance hash mismatch")
    failure = read(failure_path)
    need(failure.get("status") == "Failed" and failure.get("required_marker_present") is False and
         failure.get("artifact_admitted") is False and failure.get("mission_ready") is False and
         failure.get("diagnostic_artifact_sha256") == sha(raw_path) and
         failure.get("editor_log_sha256") == sha(log_path), "failed capture receipt not preserved/unadmitted")

    key = p["key_descriptor"]
    key_path = repo / key["path"]
    key_receipt_path = repo / key["receipt"]
    need(sha(key_path) == key.get("sha256") and sha(key_receipt_path) == key.get("receipt_sha256"),
         "key descriptor provenance hash mismatch")
    key_receipt = read(key_receipt_path)
    descriptor = read(key_path)
    need(key_receipt.get("status") == "PassedSourceOnly" and
         key_receipt.get("descriptor_sha256") == sha(key_path) and
         failure.get("key_descriptor_sha256") == sha(key_path), "source key descriptor receipt mismatch")

    raw = read(raw_path)
    need(raw.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         raw.get("authority") == p["source_authority"] and raw.get("source_pins") == p.get("source_pins") and
         raw.get("template_namespace_id") == p.get("template_namespace_id") and
         raw.get("requested_prefab_keys") == descriptor == p.get("requested_prefab_keys") and
         raw.get("requested_prefab_keys_sha256") == sha(key_path),
         "raw capture, descriptor, and projection authority domains disagree")
    verify_hash_map(raw["source_pins"])
    verify_hash_map(p.get("projection_basis_pins"))

    excluded_count = validate_entity_projection(raw, p)
    controls = [gameplay_mutation_control(raw, p)]

    preflight = read(preflight_receipt_path)
    facts = preflight.get("preflight_result", {})
    need(preflight.get("status") == "PassedSourceOnlyCompiledPreflight" and
         preflight.get("projection", {}).get("sha256") == sha(projection_path) and
         facts.get("status") == "Decoded_SourceOnly_NotImportAdmission" and
         facts.get("roots") == 9 and facts.get("entities") == 276 and
         facts.get("compiled_importer_type_and_value_preflight_passed") is True and
         facts.get("world_allocated") is False and facts.get("runtime_import_attempted") is False and
         facts.get("mission_ready") is False and facts.get("native_visual_readiness") is False,
         "compiled codec preflight must remain diagnostic and bind this exact projection")
    return {"raw_capture_sha256": sha(raw_path), "projection_sha256": sha(projection_path),
            "roots": len(raw.get("opening_prefabs", {})), "entities": len(raw["entities"]),
            "excluded_leaves": excluded_count, "source_pin_count": len(raw["source_pins"]),
            "negative_controls": controls,
            "status": "PassedSourceOnlyProvenanceGate_NoImportAdmission"}


def main():
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--projection", required=True)
    parser.add_argument("--preflight-receipt", required=True)
    parser.add_argument("--receipt", required=True)
    args = parser.parse_args()
    result = validate(args.repo, args.projection, args.preflight_receipt)
    result.update({"schema_version": 1,
                   "kind": "S004PrefabProjectionProvenanceGateReceipt",
                   "projection_path": str(Path(args.projection).resolve()),
                   "preflight_receipt_path": str(Path(args.preflight_receipt).resolve()),
                   "source_only": True, "world_allocated": False,
                   "runtime_import_attempted": False, "runtime_replay_ready": False,
                   "mission_ready": False, "native_visual_readiness": False})
    Path(args.receipt).write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
