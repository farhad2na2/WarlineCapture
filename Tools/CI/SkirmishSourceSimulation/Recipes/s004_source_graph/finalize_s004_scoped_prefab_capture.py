#!/usr/bin/env python3
"""Finalize a passed S004 scoped native-bake capture without importing or changing it."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import sys

KEY_SHA = "7bbcbe81369bf86844167355a6ce942c4be10ee3ff133660b90db4bc79214f58"
MARKER = ("[SkirmishS004ScopedPrefabReferenceData] result=PassedScoped prefabs=9 entities=276 "
          "retainedPresentationRefs=16 replayReady=False nativeVisualReadiness=False missionReady=False fullMission=0")
REASON = "unity_object_ref_requires_bound_source_identity"
UNITY_OBJECT_REF = ("Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, "
                    "Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")
FIELD_COUNTS = {
    "Game.Components.UnitAttackImpactVfxReference/Prefab": 5,
    "Game.Components.UnitMuzzleFlashVfxReference/Prefab": 4,
    "Game.Components.AirMissileLauncherVfxReferenceComponent/MissileVisualPrefab": 1,
    "Game.Components.AirMissileLauncherVfxReferenceComponent/LaunchFlashPrefab": 1,
    "Game.Components.AirMissileLauncherVfxReferenceComponent/LaunchSmokePrefab": 1,
    "Game.Components.AirMissileLauncherVfxReferenceComponent/MissileTrailPrefab": 1,
    "Game.Components.AirMissileLauncherVfxReferenceComponent/AirburstExplosionPrefab": 1,
    "Game.Components.AirMissileLauncherVfxReferenceComponent/AirTargetImpactPrefab": 1,
    "Game.Components.AirMissileLauncherVfxReferenceComponent/InterceptExplosionPrefab": 1,
}
CAPTURE_SOURCE = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S004PrefabData.cs"
EDITOR_ASSEMBLY_DEFINITION = "Assets/Game/Scripts/Editor/Game.Editor.asmdef"
EDITOR_ASSEMBLY = "Library/ScriptAssemblies/Game.Editor.dll"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def canonical_entity(value):
    parts = value.split(":") if isinstance(value, str) else []
    need(len(parts) == 2 and all(part.isdigit() for part in parts), "canonical entity identity required")
    index, version = (int(part) for part in parts)
    need((index == 0) == (version == 0) and value == f"{index}:{version}", "noncanonical entity identity")


def validate(raw, keys, log_text, wrapper_exit_code, repo):
    need(wrapper_exit_code == 0, "Unity wrapper exit code must be exactly zero")
    need(log_text.count(MARKER) == 1, "exact scoped pass marker must occur once in wrapper log")
    need(raw.get("schema_version") == 1 and raw.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         raw.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState",
         "native source/baker capture schema and authority required")
    need(raw.get("requested_prefab_keys") == keys and len(keys) == 9 and
         len(set(keys)) == 9 and len({key.casefold() for key in keys}) == 9 and
         raw.get("requested_prefab_keys_sha256") == KEY_SHA,
         "exact nine-key compiled descriptor/order/hash required")
    need(raw.get("source_scenes") == ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"],
         "original registry source scene required")
    opening = raw.get("opening_prefabs")
    need(isinstance(opening, dict) and list(opening.keys()) == keys and len(opening) == 9,
         "nine actual source roots in descriptor order required")
    entities = raw.get("entities")
    need(isinstance(entities, list) and len(entities) == 276, "exact captured closure count required")
    entity_ids = [row.get("id") for row in entities if isinstance(row, dict)]
    need(len(entity_ids) == len(entities) and len(set(entity_ids)) == len(entity_ids),
         "captured entity identities must be unique within this namespace")
    for identity in entity_ids:
        canonical_entity(identity)

    unsupported = raw.get("unsupported_values")
    need(isinstance(unsupported, list) and len(unsupported) == 16,
         "exact retained unsupported presentation-reference ledger required")
    counts = {}
    seen = set()
    by_id = {row["id"]: row for row in entities}
    for row in unsupported:
        need(isinstance(row, dict), "unsupported ledger row must be an object")
        path = row.get("path", "")
        parts = path.split("/") if isinstance(path, str) else []
        need(len(parts) == 3, "unsupported reference path must name entity/component/field")
        entity_id, component, field = parts
        canonical_entity(entity_id)
        qualified = component + "/" + field
        need(qualified in FIELD_COUNTS and row.get("reason") == REASON and row.get("type") == UNITY_OBJECT_REF,
             "unsupported value is outside the reviewed presentation-only allowance")
        need(path not in seen, "duplicate retained unsupported path")
        seen.add(path)
        counts[qualified] = counts.get(qualified, 0) + 1
        component_row = by_id[entity_id].get("components", {}).get(component)
        field_key = component + "::" + field
        encoded = component_row.get("value", {}).get("fields", {}).get(field_key) if isinstance(component_row, dict) else None
        need(component_row and component_row.get("present") is True and component_row.get("buffer") is False and
             encoded == {"codec": "unsupported-v1", "reason": REASON, "type": UNITY_OBJECT_REF},
             "retained ledger does not bind to captured unsupported leaf")
    need(counts == FIELD_COUNTS, "reviewed presentation-reference field counts changed")

    for field in ("replay_ready", "fresh_world_allocator_exported", "full_mission_initialized",
                  "native_visual_readiness", "mission_ready", "geometry_exported"):
        need(raw.get(field) is False, "capture scope flag must remain false: " + field)
    pins = raw.get("source_pins")
    need(isinstance(pins, dict) and pins, "source pin map required")
    normalized = {}
    for path_text, digest in pins.items():
        path = Path(path_text)
        if not path.is_absolute():
            path = repo / path
        need(path.is_file() and isinstance(digest, str) and len(digest) == 64 and sha(path) == digest,
             "captured source pin changed or unavailable: " + path_text)
        normalized[str(path.resolve())] = digest
    key_path = repo / "Logs/SkirmishProduction/s004-prefab-key-descriptor-20261103-T5/prefab-keys.json"
    need(normalized.get(str(key_path.resolve())) == KEY_SHA and sha(key_path) == KEY_SHA,
         "compiled key descriptor must be pinned by the capture")

    assembly_facts = {}
    for rel in (CAPTURE_SOURCE, EDITOR_ASSEMBLY_DEFINITION, EDITOR_ASSEMBLY):
        path = repo / rel
        need(path.is_file(), "required current source/assembly file missing: " + rel)
        assembly_facts[rel] = {"sha256": sha(path)}
        if rel != EDITOR_ASSEMBLY:
            need(normalized.get(str(path.resolve())) == assembly_facts[rel]["sha256"],
                 "captured source pin differs from current source: " + rel)
    return {"entity_count": len(entities), "root_count": len(opening),
            "retained_presentation_reference_count": len(unsupported),
            "source_pin_count": len(pins), "verified_source_and_assembly": assembly_facts}


def finalize(capture_dir, descriptor_dir, repo, wrapper_exit_code):
    capture_dir, descriptor_dir, repo = map(lambda path: Path(path).resolve(),
                                            (capture_dir, descriptor_dir, repo))
    output = capture_dir / "finalizer-receipt.json"
    need(not output.exists(), "fresh finalizer receipt path required")
    raw_path = capture_dir / "data/prefab-reference-data.json"
    log_path = capture_dir / "editor.log"
    key_path = descriptor_dir / "prefab-keys.json"
    key_receipt_path = descriptor_dir / "receipt.json"
    for path in (raw_path, log_path, key_path, key_receipt_path):
        need(path.is_file(), "required capture or descriptor evidence missing: " + str(path))
    keys = json.loads(key_path.read_text())
    descriptor_receipt = json.loads(key_receipt_path.read_text())
    key_sha = sha(key_path)
    need(key_sha == KEY_SHA and descriptor_receipt.get("status") == "PassedSourceOnly" and
         descriptor_receipt.get("kind") == "S004FocusedPrefabKeyDescriptorReceipt" and
         descriptor_receipt.get("descriptor_sha256") == key_sha and
         descriptor_receipt.get("key_count") == 9 and
         descriptor_receipt.get("keys_in_first_compiled_use_order") == keys,
         "source-only compiled key descriptor receipt mismatch")
    raw = json.loads(raw_path.read_text())
    log_text = log_path.read_text(errors="replace")
    facts = validate(raw, keys, log_text, wrapper_exit_code, repo)

    controls = []
    probes = [
        ("nonzero-wrapper-exit-rejected", lambda: validate(raw, keys, log_text, 1, repo)),
        ("missing-pass-marker-rejected", lambda: validate(raw, keys, log_text.replace(MARKER, ""), 0, repo)),
        ("key-domain-mutation-rejected", lambda: validate({**raw, "requested_prefab_keys": keys[:-1]}, keys, log_text, 0, repo)),
        ("unsupported-field-mutation-rejected", lambda: _mutated_ledger(raw, keys, log_text, repo)),
        ("readiness-promotion-rejected", lambda: validate({**raw, "mission_ready": True}, keys, log_text, 0, repo)),
    ]
    for name, probe in probes:
        try:
            probe()
        except (ValueError, KeyError, TypeError):
            controls.append({"case": name, "result": "Passed"})
        else:
            raise ValueError("negative control unexpectedly passed: " + name)

    receipt = {
        "schema_version": 1,
        "kind": "S004ScopedPrefabCaptureFinalizerReceipt",
        "status": "PassedScopedSourceCaptureOnly",
        "wrapper_exit_code": wrapper_exit_code,
        "capture": {"raw_path": str(raw_path), "raw_sha256": sha(raw_path),
                    "editor_log_path": str(log_path), "editor_log_sha256": sha(log_path),
                    "pass_marker": MARKER},
        "key_descriptor": {"path": str(key_path), "sha256": key_sha,
                           "receipt_path": str(key_receipt_path), "receipt_sha256": sha(key_receipt_path),
                           "keys": keys},
        "facts": facts,
        "negative_controls": controls,
        "replay_ready": False,
        "runtime_import_attempted": False,
        "native_visual_readiness": False,
        "mission_ready": False,
        "geometry_exported": False,
        "scope": "Original source/baker capture with retained unsupported presentation references. No runtime import, replay, visual qualification, or mission readiness is established.",
    }
    output.write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def _mutated_ledger(raw, keys, log_text, repo):
    probe = copy.deepcopy(raw)
    probe["unsupported_values"][0]["path"] = probe["unsupported_values"][0]["path"].rsplit("/", 1)[0] + "/UnreviewedPrefab"
    return validate(probe, keys, log_text, 0, repo)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture-dir", required=True)
    parser.add_argument("--descriptor-dir", required=True)
    parser.add_argument("--repo", default=str(Path(__file__).resolve().parents[5]))
    parser.add_argument("--wrapper-exit-code", type=int, required=True)
    args = parser.parse_args()
    try:
        receipt = finalize(args.capture_dir, args.descriptor_dir, args.repo, args.wrapper_exit_code)
        print(json.dumps({"status": receipt["status"], "roots": receipt["facts"]["root_count"],
                          "entities": receipt["facts"]["entity_count"],
                          "retained_presentation_refs": receipt["facts"]["retained_presentation_reference_count"],
                          "negative_controls": len(receipt["negative_controls"]),
                          "replay_ready": False, "native_visual_readiness": False,
                          "mission_ready": False,
                          "receipt": str(Path(args.capture_dir).resolve() / "finalizer-receipt.json")}, indent=2))
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError) as error:
        print("S004 scoped prefab capture finalization failed closed: " + str(error), file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
