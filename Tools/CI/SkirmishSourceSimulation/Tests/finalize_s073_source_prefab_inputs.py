#!/usr/bin/env python3
"""Validate checked S073 prefab/name Unity outputs and emit narrow receipts.

These receipts qualify the source-only artifacts for provenance rebinding. They
do not qualify replay readiness, a full production template closure, or mission
readiness.
"""
import argparse
import hashlib
import json
from pathlib import Path


CAPTURE_MARKER = (
    "[SkirmishS073FocusedPrefabReferenceData] result=Passed prefabs=4 entities=140 "
    "unsupportedUnconsumedPresentation=4 replayReady=False fullMission=0"
)
NAME_MARKER = (
    "[SkirmishS073LegacyPrefabNameReadback] result=Passed objects=12 unitObjects=4 "
    "unityWorld=0 mission=0"
)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def validate_log(log_path, expected_marker, method, output_path, wrapper_exit):
    if wrapper_exit != 0:
        raise ValueError("Checked Unity wrapper exit code must be zero")
    data = Path(log_path).read_text(encoding="utf-8", errors="strict")
    if data.splitlines().count(expected_marker) != 1:
        raise ValueError("Exact required Unity output marker missing or duplicated")
    if method not in data or str(Path(output_path).resolve()) not in data:
        raise ValueError("Unity log does not bind the exact method and output path")
    return sha(log_path)


def verify_pins(document, repo, allowed_external_roots):
    pins = document.get("source_pins")
    if not isinstance(pins, dict) or not pins:
        raise ValueError("Nonempty source_pins object required")
    for raw_path, expected in pins.items():
        path = Path(raw_path)
        if not path.is_absolute():
            path = repo / path
        path = path.resolve(strict=True)
        if not (path.is_relative_to(repo) or any(path.is_relative_to(root) for root in allowed_external_roots)):
            raise ValueError("Source pin escaped the repository and authorized evidence roots: " + str(path))
        if len(expected) != 64 or sha(path) != expected:
            raise ValueError("Source pin drift: " + str(path))
    return len(pins)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", required=True, type=Path)
    p.add_argument("--capture", required=True, type=Path)
    p.add_argument("--capture-log", required=True, type=Path)
    p.add_argument("--capture-wrapper-exit", required=True, type=int)
    p.add_argument("--name-readback", required=True, type=Path)
    p.add_argument("--name-log", required=True, type=Path)
    p.add_argument("--name-wrapper-exit", required=True, type=int)
    p.add_argument("--external-source-root", action="append", type=Path, default=[],
                   help="Explicit authorized root for source files referenced outside the repository; repeat as needed")
    p.add_argument("--output-dir", required=True, type=Path)
    a = p.parse_args()
    repo = a.repo.resolve(strict=True)
    capture_path = a.capture.resolve(strict=True)
    name_path = a.name_readback.resolve(strict=True)
    capture = json.loads(capture_path.read_text(encoding="utf-8"))
    names = json.loads(name_path.read_text(encoding="utf-8"))
    if (capture.get("schema_version") != 1 or capture.get("kind") != "AuthoredSkirmishPrefabReferenceData" or
            capture.get("authority") != "ActualSourceAssetsAndNativeBakers_NotMissionState" or
            capture.get("fresh_world_allocator_exported") is not False or
            capture.get("full_mission_initialized") is not False or
            capture.get("native_visual_readiness") is not False or capture.get("replay_ready") is not False or
            len(capture.get("opening_prefabs", {})) != 4 or len(capture.get("entities", [])) != 140):
        raise ValueError("Expected exact scoped source-only S073 prefab capture")
    if (names.get("schema_version") != 1 or names.get("kind") != "S073LegacyPrefabSourceNameReadback" or
            names.get("mission") != "S073" or names.get("prefab_capture_sha256") != sha(capture_path) or
            names.get("mission_initialized") is not False or names.get("mission_world_allocated") is not False or
            names.get("unity_world_allocated") is not False or names.get("native_visual_readiness") is not False or
            len(names.get("objects", [])) != 12):
        raise ValueError("Expected exact source-only AssetDatabase name readback tied to the capture")
    external_roots = [root.resolve(strict=True) for root in a.external_source_root]
    if len(set(external_roots)) != len(external_roots):
        raise ValueError("Duplicate explicit external source roots")
    capture_pins = verify_pins(capture, repo, external_roots)
    name_pins = verify_pins(names, repo, external_roots)
    capture_log_sha = validate_log(a.capture_log, CAPTURE_MARKER,
                                   "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS073FocusedPrefabReferenceData",
                                   capture_path.parent, a.capture_wrapper_exit)
    name_log_sha = validate_log(a.name_log, NAME_MARKER,
                                "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS073LegacyPrefabNameReadback",
                                name_path.parent, a.name_wrapper_exit)
    out = a.output_dir.resolve(strict=True)
    receipts = [
        (out / "prefab-capture-receipt.json", {
            "kind": "S073FocusedPrefabCaptureReceipt", "validation_status": "Passed",
            "wrapper_exit_code": 0, "required_marker_present": True,
            "capture_sha256": sha(capture_path), "editor_log_sha256": capture_log_sha,
            "source_pin_count": capture_pins, "mission_ready": False,
            "replay_ready": False, "full_mission": False,
            "scope": "Four exact startup source prefab roots; 140 source entities; four unsupported unconsumed presentation components; not a production-template closure"
        }),
        (out / "name-readback-receipt.json", {
            "kind": "S073LegacyPrefabNameReadbackReceipt", "validation_status": "Passed",
            "wrapper_exit_code": 0, "required_marker_present": True,
            "name_readback_sha256": sha(name_path), "prefab_capture_sha256": sha(capture_path),
            "editor_log_sha256": name_log_sha, "source_pin_count": name_pins,
            "mission_ready": False, "scope": "Exact AssetDatabase GUID/local-fileID name readback; source data only"
        })
    ]
    if any(path.exists() for path, _ in receipts):
        raise ValueError("Fresh receipt paths required")
    for path, receipt in receipts:
        path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": "Passed", "capture_sha256": sha(capture_path),
                      "name_readback_sha256": sha(name_path), "capture_source_pins": capture_pins,
                      "name_source_pins": name_pins, "receipts": [str(path) for path, _ in receipts],
                      "replay_ready": False, "production_template_closure": False}, indent=2))


if __name__ == "__main__":
    main()
