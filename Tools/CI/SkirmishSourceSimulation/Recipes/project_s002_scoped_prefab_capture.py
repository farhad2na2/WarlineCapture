"""Project the newly qualified S002 source capture into the exact typed read domain.

The retained failed capture is not accepted by this producer. The input capture,
its successful finalizer receipt, and the two presentation-only omissions are
bound to reviewed immutable hashes. This projection does not establish replay,
visual, source-fit, or mission readiness.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path


CAPTURE_SHA = "ed7f792922db66d8d59fb5919a860fefea803b123ef893e226dc18fce3fb24ca"
RECEIPT_SHA = "2d32f0d1a46d18f4fb2ef6d5a8af113c56f937835703a383fcf86a0a7eb41d49"
KEYS_SHA = "f2d08edd3a4e495448d3f4df2ad6608d0180e6428be3d32f4844744dcbf23f09"
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
EXPECTED_KEYS = [
    "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Chr_Soldier_Male_01",
    "Unit_Chr_Soldier_Female_01", "Unit_Chr_Soldier_Female_02_Alt_02",
    "Unit_Chr_Ghillie_Male_01", "Unit_Veh_Light_Armored_Car", "Unit_Veh_APC_Fast",
    "Unit_Veh_APC_Slow", "Unit_Veh_Tank_USA", "Unit_Veh_Radar_Tank",
    "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker",
]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def pairs(rows):
    result = {}
    for key, value in rows:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


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


def produce(capture_path, receipt_path, output_path):
    capture_path = Path(capture_path).resolve(strict=True)
    receipt_path = Path(receipt_path).resolve(strict=True)
    output_path = Path(output_path).resolve()
    capture_raw = capture_path.read_bytes()
    receipt_raw = receipt_path.read_bytes()
    if sha(capture_raw) != CAPTURE_SHA or sha(receipt_raw) != RECEIPT_SHA:
        raise ValueError("Exact successful S002 capture and finalizer receipt required")
    capture = json.loads(capture_raw.decode("utf-8"), object_pairs_hook=pairs)
    receipt = json.loads(receipt_raw.decode("utf-8"), object_pairs_hook=pairs)
    if (receipt.get("kind") != "S002FocusedPrefabCaptureReceipt" or
            receipt.get("validation_status") != "Passed" or
            receipt.get("wrapper_exit_code") != 0 or
            receipt.get("required_marker_present") is not True or
            receipt.get("capture_sha256") != CAPTURE_SHA or
            receipt.get("prefab_count") != 12 or receipt.get("entity_count") != 352 or
            receipt.get("source_pin_count") != 7967 or
            receipt.get("unsupported_presentation_omission_count") != 14 or
            receipt.get("mission_ready") is not False or receipt.get("replay_ready") is not False or
            receipt.get("full_mission") is not False or
            receipt.get("native_visual_readiness") is not False):
        raise ValueError("Exact successful S002 capture receipt and false readiness gates required")
    if (capture.get("schema_version") != 1 or
            capture.get("kind") != "AuthoredSkirmishPrefabReferenceData" or
            capture.get("authority") != "ActualSourceAssetsAndNativeBakers_NotMissionState" or
            capture.get("requested_prefab_keys_sha256") != KEYS_SHA or
            capture.get("requested_prefab_keys") != EXPECTED_KEYS or
            capture.get("source_scenes") != ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"] or
            len(capture.get("opening_prefabs", {})) != 12 or
            len(capture.get("entities", [])) != 352 or
            capture.get("replay_ready") is not False or
            capture.get("fresh_world_allocator_exported") is not False or
            capture.get("full_mission_initialized") is not False or
            capture.get("native_visual_readiness") is not False or
            capture.get("geometry_exported") is not False):
        raise ValueError("Exact S002 capture namespace and false readiness gates required")
    affected = classify_omissions(capture.get("unsupported_values"))
    if output_path.exists():
        raise ValueError("Fresh isolated S002 projection directory required")
    output_path.mkdir(parents=True)
    domain = {field: capture[field] for field in FIELDS}
    domain["source_reference_sha256"] = CAPTURE_SHA
    domain["derived_read_domain"] = "ExactTypedPrefabClosureOnly"
    slice_path = output_path / "s002-focused-template-domain.json"
    slice_bytes = encode(domain)
    slice_path.write_bytes(slice_bytes)
    manifest = {
        "schema_version": 1,
        "kind": "PinnedExactTemplateDomainProjection",
        "source_path": str(capture_path),
        "source_sha256": CAPTURE_SHA,
        "source_bytes": len(capture_raw),
        "capture_validation_status": "Passed",
        "capture_receipt_path": str(receipt_path),
        "capture_receipt_sha256": RECEIPT_SHA,
        "slice_path": str(slice_path),
        "slice_sha256": sha(slice_bytes),
        "slice_bytes": len(slice_bytes),
        "selected_fields": FIELDS,
        "root_count": 12,
        "entity_count": 352,
        "unsupported_presentation_omission_count": 14,
        "unsupported_presentation_omission_entities": affected,
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
        "source_reference_sha256": CAPTURE_SHA,
        "projection_manifest_path": str(manifest_path),
        "projection_manifest_sha256": sha(manifest_bytes),
        "capture_validation_status": "Passed",
        "capture_receipt_path": str(receipt_path),
        "capture_receipt_sha256": RECEIPT_SHA,
        "unsupported_presentation_omission_count": 14,
        "source_fit_ready": False,
        "native_visual_readiness": False,
        "replay_ready": False,
        "mission_ready": False,
        "consumer_qualification_scope": "S002 exact typed source-template diagnostics; presentation-only VFX omissions retained; no visual, source-fit, replay, or mission readiness",
    }
    descriptor_path = output_path / "reference-template-descriptor.json"
    descriptor_bytes = encode(descriptor)
    descriptor_path.write_bytes(descriptor_bytes)
    if (capture_path.read_bytes() != capture_raw or receipt_path.read_bytes() != receipt_raw):
        raise ValueError("S002 capture or finalizer receipt changed during projection")
    return {
        "kind": "S002ScopedPrefabProjectionReceipt",
        "capture_sha256": CAPTURE_SHA,
        "capture_receipt_sha256": RECEIPT_SHA,
        "projection_manifest_sha256": sha(manifest_bytes),
        "slice_sha256": sha(slice_bytes),
        "unsupported_presentation_omission_count": 14,
        "affected_entity_count": len(affected),
        "capture_validation_status": "Passed",
        "source_fit_ready": False,
        "native_visual_readiness": False,
        "replay_ready": False,
        "mission_ready": False,
        "descriptor_path": str(descriptor_path),
        "descriptor_sha256": sha(descriptor_bytes),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture", required=True, type=Path)
    parser.add_argument("--receipt", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    receipt = produce(args.capture, args.receipt, args.output)
    receipt_path = args.output / "projection-receipt.json"
    receipt_path.write_bytes(encode(receipt))
    print(json.dumps(receipt, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
