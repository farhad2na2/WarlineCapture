#!/usr/bin/env python3
"""Validate an exact S002 source-prefab export without claiming replay readiness."""
import argparse
import hashlib
import json
import re
from pathlib import Path


KEYS_SHA = "f2d08edd3a4e495448d3f4df2ad6608d0180e6428be3d32f4844744dcbf23f09"
METHOD = "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS002FocusedPrefabReferenceData"
MARKER = (
    "[SkirmishS002FocusedPrefabReferenceData] result=Passed prefabs=12 entities=352 "
    "unsupportedUnconsumedPresentation=14 replayReady=False fullMission=0"
)
ALLOWED_PRESENTATION = {
    "Game.Components.UnitAttackImpactVfxReference",
    "Game.Components.UnitMuzzleFlashVfxReference",
}
EXPECTED_KEYS = [
    "Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Chr_Soldier_Male_01",
    "Unit_Chr_Soldier_Female_01", "Unit_Chr_Soldier_Female_02_Alt_02",
    "Unit_Chr_Ghillie_Male_01", "Unit_Veh_Light_Armored_Car",
    "Unit_Veh_APC_Fast", "Unit_Veh_APC_Slow", "Unit_Veh_Tank_USA",
    "Unit_Veh_Radar_Tank", "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker",
]


def sha_bytes(data):
    return hashlib.sha256(data).hexdigest()


def classify_unsupported(rows):
    if not isinstance(rows, list):
        raise ValueError("unsupported_values array required")
    for row in rows:
        parts = row.get("path", "").split("/")
        entity_parts = parts[0].split(":") if parts else []
        canonical_entity = (len(entity_parts) == 2 and
                           all(re.fullmatch(r"0|[1-9][0-9]*", part) for part in entity_parts) and
                           ((int(entity_parts[0]) == 0) == (int(entity_parts[1]) == 0)) and
                           parts[0] == f"{int(entity_parts[0])}:{int(entity_parts[1])}")
        if (len(parts) != 3 or not canonical_entity or parts[1] not in ALLOWED_PRESENTATION or parts[2] != "Prefab"
                or row.get("reason") != "unity_object_ref_requires_bound_source_identity"
                or not (row.get("type") or "").startswith(
                    "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,")):
            raise ValueError("Unsupported required template value")
    if len(rows) != 14:
        raise ValueError("Exact 14 presentation-only omissions required")
    return sorted({row["path"].split("/")[0] for row in rows})


def verify_pins(document, repo, external_roots):
    pins = document.get("source_pins")
    if not isinstance(pins, dict) or len(pins) < 1000:
        raise ValueError("Large nonempty current Unity source pin set required")
    for raw_path, expected in pins.items():
        path = Path(raw_path).resolve(strict=True)
        if not (path.is_relative_to(repo) or any(path.is_relative_to(root) for root in external_roots)):
            raise ValueError("Source pin outside explicitly authorized roots: " + str(path))
        if len(expected) != 64 or sha_bytes(path.read_bytes()) != expected:
            raise ValueError("Source pin drift: " + str(path))
    return len(pins)


def validate(capture_path, log_path, wrapper_exit, repo_path, external_source_roots):
    capture_path = Path(capture_path).resolve(strict=True)
    log_path = Path(log_path).resolve(strict=True)
    repo = Path(repo_path).resolve(strict=True)
    external_roots = [Path(root).resolve(strict=True) for root in external_source_roots]
    if wrapper_exit != 0:
        raise ValueError("Checked Unity wrapper exit code must be zero")
    raw = capture_path.read_bytes()
    def reject_duplicate_properties(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("Duplicate JSON property: " + key)
            result[key] = value
        return result

    capture = json.loads(raw.decode("utf-8"), object_pairs_hook=reject_duplicate_properties)
    if (capture.get("schema_version") != 1 or
            capture.get("kind") != "AuthoredSkirmishPrefabReferenceData" or
            capture.get("authority") != "ActualSourceAssetsAndNativeBakers_NotMissionState" or
            capture.get("source_scenes") != ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"] or
            capture.get("requested_prefab_keys_sha256") != KEYS_SHA or
            capture.get("replay_ready") is not False or
            capture.get("fresh_world_allocator_exported") is not False or
            capture.get("full_mission_initialized") is not False or
            capture.get("native_visual_readiness") is not False or
            capture.get("geometry_exported") is not False or
            capture.get("requested_prefab_keys") != EXPECTED_KEYS or
            len(capture.get("opening_prefabs", {})) != 12 or
            len(capture.get("entities", [])) != 352):
        raise ValueError("Exact scoped S002 12-prefab capture required")
    if not re.fullmatch(r"[0-9a-f]{32}", capture.get("template_namespace_id", "")):
        raise ValueError("One valid captured prefab namespace required")
    entity_ids = [row.get("id") for row in capture["entities"]]
    if (len(set(entity_ids)) != 352 or
            any(not re.fullmatch(r"(?:0|[1-9][0-9]*):(?:0|[1-9][0-9]*)", entity_id or "") or
                ((int(entity_id.split(":")[0]) == 0) != (int(entity_id.split(":")[1]) == 0))
                for entity_id in entity_ids) or
            not set(capture["opening_prefabs"].values()).issubset(set(entity_ids))):
        raise ValueError("Exact unique captured entity namespace and opening roots required")
    affected_entities = classify_unsupported(capture.get("unsupported_values"))
    pin_count = verify_pins(capture, repo, external_roots)
    if KEYS_SHA not in capture["source_pins"].values():
        raise ValueError("Exact S002 prefab keyset must be a hash-pinned source input")
    log = log_path.read_text(encoding="utf-8", errors="strict")
    if log.splitlines().count(MARKER) != 1:
        raise ValueError("Exact S002 pass marker missing or duplicated")
    if METHOD not in log or str(capture_path.parent) not in log:
        raise ValueError("Unity log does not bind exact method and output directory")
    if capture_path.read_bytes() != raw:
        raise ValueError("Capture changed during receipt validation")
    return {
        "kind": "S002FocusedPrefabCaptureReceipt",
        "validation_status": "Passed",
        "wrapper_exit_code": 0,
        "required_marker_present": True,
        "capture_sha256": sha_bytes(raw),
        "editor_log_sha256": sha_bytes(log.encode("utf-8")),
        "source_pin_count": pin_count,
        "prefab_count": 12,
        "entity_count": 352,
        "unsupported_presentation_omission_count": 14,
        "affected_entity_count": len(affected_entities),
        "mission_ready": False,
        "replay_ready": False,
        "full_mission": False,
        "native_visual_readiness": False,
        "scope": "Exact S002 12-root source template closure; VFX reference fields are presentation-only omissions; no replay or mission readiness",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--capture", required=True, type=Path)
    parser.add_argument("--capture-log", required=True, type=Path)
    parser.add_argument("--capture-wrapper-exit", required=True, type=int)
    parser.add_argument("--external-source-root", action="append", type=Path, default=[])
    parser.add_argument("--receipt", required=True, type=Path)
    args = parser.parse_args()
    receipt_path = args.receipt.resolve()
    if receipt_path.exists():
        raise ValueError("Fresh receipt path required")
    receipt = validate(args.capture, args.capture_log, args.capture_wrapper_exit,
                       args.repo, args.external_source_root)
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
