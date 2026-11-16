#!/usr/bin/env python3
"""Qualify the checked S004 data-only capture without granting replay admission."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[5]
KEYS = ROOT / "T Logs/SkirmishProduction/s004-missing-four-focused-keys-20261103-T1/keys.json"
INVENTORY = KEYS.parent / "receipt.json"
ALLOWED = {
    "Game.Components.UnitAttackImpactVfxReference/Prefab": 3,
    "Game.Components.UnitMuzzleFlashVfxReference/Prefab": 2,
    "Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactExplosionPrefab": 1,
    "Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactSmokePrefab": 1,
    "Game.Components.GroundMissileLauncherVfxReferenceComponent/LauncherBackfirePrefab": 1,
    "Game.Components.GroundMissileLauncherVfxReferenceComponent/RocketTrailPrefab": 1,
}


def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def need(value, message):
    if not value: raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", required=True)
    parser.add_argument("--wrapper-exit", required=True, type=int)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    directory = Path(args.directory).resolve(strict=True)
    capture, log = directory / "data/prefab-reference-data.json", directory / "editor.log"
    output = Path(args.output).absolute()
    need(args.wrapper_exit == 0 and not output.exists(), "Successful wrapper and fresh output required")
    inputs = {str(p): digest(p) for p in (capture, log, KEYS, INVENTORY)}
    data, keys, inventory = (json.loads(p.read_text()) for p in (capture, KEYS, INVENTORY))
    marker = re.findall(r"\[S004MissingPrefabReference\] result=Collected roots=(\d+) entities=(\d+) unsupported=(\d+) world=0 readiness=0", log.read_text(errors="replace"))
    need(len(marker) == 1, "Exact one capture marker required")
    roots, entities, unsupported_count = map(int, marker[0])
    need(data.get("kind") == "AuthoredSkirmishPrefabReferenceData" and data.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState", "Capture schema/authority mismatch")
    need(roots == len(keys) == 4 and data.get("requested_prefab_keys") == keys and set(data.get("opening_prefabs", {})) == set(keys), "Exact four keys required")
    need(data.get("requested_prefab_keys_sha256") == inputs[str(KEYS)] and data.get("source_key_inventory_sha256") == inputs[str(INVENTORY)], "Capture input lineage changed")
    rows = data.get("entities"); unsupported = data.get("unsupported_values")
    need(isinstance(rows, list) and len(rows) == entities and isinstance(unsupported, list) and len(unsupported) == unsupported_count == 9, "Marker and capture counts differ")
    ids = [row.get("id") for row in rows]
    need(len(ids) == len(set(ids)) and all(value in ids for value in data["opening_prefabs"].values()), "Duplicate/missing source root")
    fields = Counter()
    for row in unsupported:
        parts = row.get("path", "").split("/")
        need(len(parts) == 3 and parts[0] in ids and row.get("reason") == "unity_object_ref_requires_bound_source_identity" and row.get("type", "").startswith("Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,"), "Unsupported gameplay or unknown reference")
        fields["/".join(parts[1:])] += 1
    need(dict(fields) == ALLOWED, "Unexpected unsupported presentation ledger")
    pins = data.get("source_pins")
    need(isinstance(pins, dict) and len(pins) > 1000, "Complete capture source pins required")
    for path, expected in pins.items(): need(Path(path).is_file() and digest(path) == expected, "Source pin changed: " + path)
    for row in inventory["source_assets"]:
        path = Path(row["path"])
        need(pins.get(str(path)) == row["sha256"] and pins.get(str(path) + ".meta") == row["meta_sha256"], "Prefab identity bytes not capture-pinned")
    for field in ("replay_ready", "fresh_world_allocator_exported", "full_mission_initialized", "native_visual_readiness", "source_fit_certified", "mission_ready", "geometry_exported"):
        need(data.get(field) is False, "Capture scope invalid: " + field)
    need({str(p): digest(p) for p in (capture, log, KEYS, INVENTORY)} == inputs, "Capture evidence changed during qualification")
    result = {"status": "PassedScopedSourceOnlyCapture", "wrapper_exit_code": args.wrapper_exit, "marker_present": True,
              "roots": roots, "entities": entities, "unsupported_presentation_values": unsupported_count,
              "source_pins_checked": len(pins), "input_sha256": inputs, "unsupported_fields": dict(fields),
              "runtime_admission": False, "replay_ready": False, "native_visual_readiness": False,
              "source_fit_certified": False, "mission_ready": False}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream: json.dump(result, stream, indent=2); stream.write("\n")
    print(json.dumps(result))


if __name__ == "__main__": main()
