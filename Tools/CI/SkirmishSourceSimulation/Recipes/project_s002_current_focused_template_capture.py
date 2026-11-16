#!/usr/bin/env python3
"""Project a freshly finalized S002 focused capture into its bounded read domain."""
import argparse
import hashlib
import json
import re
from pathlib import Path

from finalize_epoch_neutral_s002_template_capture import (
    STRICT, finalize as strict_finalize, reject_duplicate_properties,
)

FIELDS = [
    "schema_version", "kind", "authority", "source_scenes", "unity_version",
    "template_namespace_id", "requested_prefab_keys", "requested_prefab_keys_sha256",
    "opening_prefabs", "entities", "unsupported_values", "replay_ready",
    "fresh_world_allocator_exported", "full_mission_initialized", "native_visual_readiness",
    "geometry_exported", "consumer_rule",
]
ALLOWED_COMPONENT_FIELDS = {
    "Game.Components.UnitAttackImpactVfxReference": {"Prefab"},
    "Game.Components.UnitMuzzleFlashVfxReference": {"Prefab"},
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def encode(value):
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def classify_omissions(rows):
    if not isinstance(rows, list):
        raise ValueError("Exact unsupported_values array required")
    affected = set()
    for row in rows:
        path = row.get("path", "").split("/")
        entity = path[0].split(":") if path else []
        canonical = (len(entity) == 2 and
                     all(re.fullmatch(r"0|[1-9][0-9]*", token) for token in entity) and
                     ((int(entity[0]) == 0) == (int(entity[1]) == 0)) and
                     path[0] == f"{int(entity[0])}:{int(entity[1])}")
        if (len(path) != 3 or not canonical or
                path[1] not in ALLOWED_COMPONENT_FIELDS or
                path[2] not in ALLOWED_COMPONENT_FIELDS[path[1]] or
                row.get("reason") != "unity_object_ref_requires_bound_source_identity" or
                not (row.get("type") or "").startswith(
                    "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,")):
            raise ValueError("Unsupported required template value")
        affected.add(path[0])
    if len(rows) != 14:
        raise ValueError("Exact 14 presentation-only omission records required")
    return sorted(affected)


def produce(capture_path, finalization_path, output_path):
    capture_path = Path(capture_path).resolve(strict=True)
    finalization_path = Path(finalization_path).resolve(strict=True)
    output_path = Path(output_path).resolve()
    capture_raw = capture_path.read_bytes()
    finalization_raw = finalization_path.read_bytes()
    capture = json.loads(capture_raw.decode("utf-8"), object_pairs_hook=reject_duplicate_properties)
    finalization = json.loads(finalization_raw.decode("utf-8"), object_pairs_hook=reject_duplicate_properties)
    repo_path = Path(__file__).resolve().parents[4]
    key_path = Path(finalization.get("key_descriptor_path", "")).resolve(strict=True)
    export_capture_path = Path(finalization.get("export_capture_path", "")).resolve(strict=True)
    export_capture_raw = export_capture_path.read_bytes()
    require(capture_raw == export_capture_raw,
            "Durable capture copy must be byte-identical to original exporter output")
    # Recompute the strict receipt from current bytes. A caller-supplied JSON
    # receipt is only accepted when it exactly equals this independently derived
    # result, including the wrapper status, marker, log hash, and pin count.
    pin_count_before = STRICT.verify_pins(capture, repo_path, [key_path])
    recomputed = strict_finalize(
        capture_path,
        export_capture_path,
        finalization.get("capture_log_path", ""),
        finalization.get("wrapper_exit_code", -1),
        repo_path,
        [key_path],
        key_path,
        finalization.get("key_descriptor_sha256", ""),
    )
    require(finalization == recomputed,
            "Supplied current S002 finalization does not match recomputed strict evidence")
    require(finalization.get("kind") == "S002CurrentFocusedTemplateCaptureFinalization" and
            finalization.get("validation_status") == "PassedCurrentPinnedSourceOnly" and
            finalization.get("capture_path") == str(capture_path) and
            finalization.get("capture_sha256") == sha(capture_raw) and
            finalization.get("export_capture_path") == str(export_capture_path) and
            finalization.get("export_capture_sha256") == sha(export_capture_raw) and
            finalization.get("wrapper_exit_code") == 0 and
            finalization.get("required_marker") == STRICT.MARKER and
            finalization.get("required_method") == STRICT.METHOD and
            finalization.get("source_pin_count", 0) >= 1000 and
            finalization.get("mission_ready") is False and
            finalization.get("replay_ready") is False and
            finalization.get("full_mission") is False and
            finalization.get("native_visual_readiness") is False and
            finalization.get("source_fit_ready") is False,
            "Exact successful current S002 strict finalization required")
    strict = finalization.get("strict_finalizer")
    require(isinstance(strict, dict) and
            strict.get("kind") == "S002FocusedPrefabCaptureReceipt" and
            strict.get("validation_status") == "Passed" and
            strict.get("capture_sha256") == sha(capture_raw) and
            strict.get("editor_log_sha256") == finalization.get("capture_log_sha256") and
            strict.get("source_pin_count") == finalization.get("source_pin_count") and
            strict.get("wrapper_exit_code") == 0 and
            strict.get("required_marker_present") is True and
            strict.get("prefab_count") == 12 and strict.get("entity_count") == 352 and
            strict.get("unsupported_presentation_omission_count") == 14 and
            strict.get("mission_ready") is False and strict.get("replay_ready") is False and
            strict.get("full_mission") is False and strict.get("native_visual_readiness") is False,
            "Strict finalizer receipt content mismatch")
    key_raw = key_path.read_bytes()
    log_path = Path(finalization["capture_log_path"]).resolve(strict=True)
    log_raw = log_path.read_bytes()
    require(sha(key_raw) == finalization.get("key_descriptor_sha256") and
            capture.get("requested_prefab_keys_sha256") == sha(key_raw) and
            capture.get("source_pins", {}).get(str(key_path)) == sha(key_raw),
            "Exact current external key descriptor binding required")
    require(capture.get("schema_version") == 1 and
            capture.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
            capture.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
            capture.get("source_scenes") == ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"] and
            capture.get("requested_prefab_keys") == STRICT.EXPECTED_KEYS and
            len(capture.get("opening_prefabs", {})) == 12 and len(capture.get("entities", [])) == 352 and
            capture.get("replay_ready") is False and
            capture.get("fresh_world_allocator_exported") is False and
            capture.get("full_mission_initialized") is False and
            capture.get("native_visual_readiness") is False and capture.get("geometry_exported") is False,
            "Current S002 capture shape or readiness limits mismatch")
    affected = classify_omissions(capture.get("unsupported_values"))
    if output_path.exists():
        raise ValueError("Fresh isolated current S002 projection directory required")
    output_path.mkdir(parents=True)
    domain = {field: capture[field] for field in FIELDS}
    domain["source_reference_sha256"] = sha(capture_raw)
    domain["derived_read_domain"] = "ExactTypedPrefabClosureOnly"
    slice_path = output_path / "s002-current-focused-template-domain.json"
    slice_bytes = encode(domain)
    slice_path.write_bytes(slice_bytes)
    proof = {
        "schema_version": 1,
        "kind": "S002CurrentFocusedTemplateCaptureProof",
        "capture_path": str(capture_path),
        "capture_sha256": sha(capture_raw),
        "export_capture_path": str(export_capture_path),
        "export_capture_sha256": sha(export_capture_raw),
        "finalization_receipt_path": str(finalization_path),
        "finalization_receipt_sha256": sha(finalization_raw),
        "capture_log_path": finalization["capture_log_path"],
        "capture_log_sha256": finalization["capture_log_sha256"],
        "key_descriptor_path": str(key_path),
        "key_descriptor_sha256": sha(key_raw),
        "source_pin_count": finalization["source_pin_count"],
        "unsupported_presentation_omission_count": 14,
        "source_fit_ready": False,
        "native_visual_readiness": False,
        "replay_ready": False,
        "mission_ready": False,
    }
    manifest = {
        "schema_version": 1,
        "kind": "PinnedExactTemplateDomainProjection",
        "source_path": str(capture_path),
        "source_sha256": sha(capture_raw),
        "source_bytes": len(capture_raw),
        "slice_path": str(slice_path),
        "slice_sha256": sha(slice_bytes),
        "slice_bytes": len(slice_bytes),
        "selected_fields": FIELDS,
        "root_count": 12,
        "entity_count": 352,
        "unsupported_presentation_omission_count": 14,
        "unsupported_presentation_omission_entities": affected,
        "current_capture_proof": proof,
        "consumer_read_domain": "ExactTypedPrefabClosureOnly",
        "source_fit_ready": False,
        "native_visual_readiness": False,
        "replay_ready": False,
        "mission_ready": False,
    }
    manifest_path = output_path / "template-slice-manifest.json"
    manifest_bytes = encode(manifest)
    manifest_path.write_bytes(manifest_bytes)
    descriptor = {
        "path": str(slice_path),
        "sha256": sha(slice_bytes),
        "source_reference_path": str(capture_path),
        "source_reference_sha256": sha(capture_raw),
        "projection_manifest_path": str(manifest_path),
        "projection_manifest_sha256": sha(manifest_bytes),
        "current_capture_proof": proof,
        "capture_validation_status": "PassedCurrentPinnedSourceOnly",
        "unsupported_presentation_omission_count": 14,
        "source_fit_ready": False,
        "native_visual_readiness": False,
        "replay_ready": False,
        "mission_ready": False,
        "consumer_qualification_scope": "Current S002 exact typed template closure; source pins and capture receipt verified; 14 presentation-only VFX omissions retained; no replay, source-fit, visual, or mission readiness",
    }
    descriptor_path = output_path / "reference-template-descriptor.json"
    descriptor_bytes = encode(descriptor)
    descriptor_path.write_bytes(descriptor_bytes)
    pin_count_after = STRICT.verify_pins(capture, repo_path, [key_path])
    require(pin_count_before == pin_count_after == finalization["source_pin_count"] and
            capture_path.read_bytes() == capture_raw and finalization_path.read_bytes() == finalization_raw and
            export_capture_path.read_bytes() == export_capture_raw and
            key_path.read_bytes() == key_raw and log_path.read_bytes() == log_raw and
            sha(log_raw) == finalization.get("capture_log_sha256"),
            "Pinned current capture evidence changed during projection")
    return {
        "kind": "S002CurrentFocusedTemplateProjectionReceipt",
        "capture_sha256": sha(capture_raw),
        "finalization_receipt_sha256": sha(finalization_raw),
        "key_descriptor_sha256": sha(key_raw),
        "projection_manifest_sha256": sha(manifest_bytes),
        "slice_sha256": sha(slice_bytes),
        "descriptor_path": str(descriptor_path),
        "descriptor_sha256": sha(descriptor_bytes),
        "source_pin_count": finalization["source_pin_count"],
        "unsupported_presentation_omission_count": 14,
        "source_fit_ready": False,
        "native_visual_readiness": False,
        "replay_ready": False,
        "mission_ready": False,
    }


def require(condition, message):
    if not condition:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture", required=True, type=Path)
    parser.add_argument("--finalization-receipt", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    result = produce(args.capture, args.finalization_receipt, args.output)
    (args.output / "projection-receipt.json").write_bytes(encode(result))
    print(json.dumps(result, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
