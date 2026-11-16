#!/usr/bin/env python3
"""Verify the checked S003 source-template capture without admitting import."""
import argparse
import hashlib
import json
from pathlib import Path

MARKER = "[S003RefreshedPrefabReference] result=Collected roots=19 entities=647 unsupported=32 world=0 readiness=0"
METHOD = "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS003RefreshedPrefabReferenceData"
VFX = {"Game.Components.UnitAttackImpactVfxReference", "Game.Components.UnitMuzzleFlashVfxReference",
       "Game.Components.GroundMissileLauncherVfxReferenceComponent", "Game.Components.AirMissileLauncherVfxReferenceComponent"}


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def qualify(root, capture_path, log_path, wrapper_exit):
    root = root.resolve(strict=True)
    raw, lograw = capture_path.read_bytes(), log_path.read_bytes()
    capture = json.loads(raw)
    log = lograw.decode("utf-8", errors="strict")
    if wrapper_exit != 0 or sum(line.endswith(MARKER) for line in log.splitlines()) != 1 or METHOD not in log:
        raise ValueError("Checked wrapper and exact capture marker required")
    if (capture.get("kind") != "AuthoredSkirmishPrefabReferenceData" or capture.get("mission_id") != "S003"
        or capture.get("diagnostic_status") != "CollectedSourceOnly" or capture.get("compiler_seed") != 104732
        or capture.get("compiler_setup_hash") != 2394754492):
        raise ValueError("Exact source-only S003 capture target required")
    for flag in ("replay_ready", "fresh_world_allocator_exported", "full_mission_initialized", "mission_world_created",
                 "native_visual_readiness", "source_fit_certified", "native_context_qualified", "mission_ready", "geometry_exported"):
        if capture.get(flag) is not False:
            raise ValueError("Capture scope changed: " + flag)
    if capture.get("utility_world_created") is not True or capture.get("utility_world_disposed") is not True:
        raise ValueError("Disposed capture world required")
    keys_path = root / "T Logs/SkirmishProduction/s003-refreshed-prefab-keys-20261104-T1/prefab-keys.json"
    keys_raw = keys_path.read_bytes()
    keys = json.loads(keys_raw)
    if (len(keys) != 19 or len(set(keys)) != 19 or capture.get("requested_prefab_keys") != keys
        or capture.get("requested_prefab_keys_sha256") != sha(keys_raw)):
        raise ValueError("Exact ordered factory-derived prefab keys required")
    roots, rows = capture.get("opening_prefabs"), capture.get("entities")
    if not isinstance(roots, dict) or list(roots) != keys or not isinstance(rows, list) or len(rows) != 647:
        raise ValueError("Complete captured root/entity domain required")
    ids = [row.get("id") for row in rows]
    if len(set(ids)) != len(ids) or any(root_id not in ids for root_id in roots.values()):
        raise ValueError("Unique in-graph prefab roots required")
    unsupported = capture.get("unsupported_values")
    if not isinstance(unsupported, list) or len(unsupported) != 32 or capture.get("unsupported_values_preserved_verbatim") is not True:
        raise ValueError("Complete unsupported value record required")
    for row in unsupported:
        parts = row.get("path", "").split("/")
        if (len(parts) != 3 or parts[0] not in ids or parts[1] not in VFX
            or row.get("reason") != "unity_object_ref_requires_bound_source_identity"
            or not row.get("type", "").startswith("Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,")):
            raise ValueError("Unexpected unsupported value")
    pins = capture.get("source_pins")
    if not isinstance(pins, dict) or len(pins) < 16000:
        raise ValueError("Complete source pin set required")
    for name, expected in pins.items():
        path = Path(name)
        path = path if path.is_absolute() else root / path
        if not path.is_file() or path.is_symlink() or sha(path.read_bytes()) != expected:
            raise ValueError("Capture source changed: " + name)
    return {"status": "PassedDataOnlyCapture", "wrapper_exit_code": wrapper_exit, "required_marker_present": True,
            "capture": {"path": str(capture_path.resolve()), "sha256": sha(raw)},
            "editor_log": {"path": str(log_path.resolve()), "sha256": sha(lograw)},
            "source_pin_entries_verified": len(pins), "roots": len(roots), "entities": len(rows),
            "unsupported_presentation_values": len(unsupported), "import_admitted": False,
            "native_metadata_predates_added_capture_observer": True,
            "source_fit_certified": False, "native_visual_readiness": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("repo", "capture", "log", "output"):
        parser.add_argument("--" + name, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    args = parser.parse_args()
    receipt = qualify(Path(args.repo), Path(args.capture), Path(args.log), args.wrapper_exit)
    with Path(args.output).open("x") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
