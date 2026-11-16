#!/usr/bin/env python3
"""Create a strict source-only projection of S004's failed focused prefab capture."""
import argparse
import copy
import hashlib
import json
from collections import Counter
from pathlib import Path

UNITY_OBJECT_REF = "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"
REASON = "unity_object_ref_requires_bound_source_identity"
FIELD_COUNTS = {
    "Game.Components.UnitAttackImpactVfxReference": {"Prefab": 5},
    "Game.Components.UnitMuzzleFlashVfxReference": {"Prefab": 4},
    "Game.Components.AirMissileLauncherVfxReferenceComponent": {
        "MissileVisualPrefab": 1, "LaunchFlashPrefab": 1, "LaunchSmokePrefab": 1,
        "MissileTrailPrefab": 1, "AirburstExplosionPrefab": 1,
        "AirTargetImpactPrefab": 1, "InterceptExplosionPrefab": 1,
    },
}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def read_json(path: Path):
    return json.loads(path.read_text())


def canonical_entity(value):
    parts = value.split(":") if isinstance(value, str) else []
    need(len(parts) == 2 and all(x.isdigit() for x in parts), "canonical entity ID required")
    a, b = (int(x) for x in parts)
    need((a == 0) == (b == 0) and value == f"{a}:{b}", "canonical entity spelling required")
    return value


def validate_exclusions(rows):
    need(isinstance(rows, list) and len(rows) == 16, "exactly 16 reviewed presentation references required")
    seen = set()
    counts = Counter()
    normalized = []
    for row in rows:
        need(isinstance(row, dict), "unsupported-value row must be an object")
        path = row.get("path")
        parts = path.split("/") if isinstance(path, str) else []
        need(len(parts) == 3, "exact entity/component/field path required")
        entity = canonical_entity(parts[0])
        component, field = parts[1], parts[2]
        need(component in FIELD_COUNTS and field in FIELD_COUNTS[component], "unknown presentation exclusion field")
        need(row.get("reason") == REASON and row.get("type") == UNITY_OBJECT_REF,
             "exact UnityObjectRef<GameObject> diagnostic required")
        need(path not in seen, "duplicate presentation exclusion path")
        seen.add(path)
        counts[(component, field)] += 1
        normalized.append({"path": f"{entity}/{component}/{field}", "component": component,
                           "field": field, "reason": REASON, "type": UNITY_OBJECT_REF})
    for component, fields in FIELD_COUNTS.items():
        for field, expected in fields.items():
            need(counts[(component, field)] == expected,
                 f"missing or excess reviewed field {component}/{field}")
    need(sum(counts.values()) == 16, "unexpected presentation exclusion count")
    return normalized


def controls(rows):
    results = []
    unknown = copy.deepcopy(rows)
    unknown[-1]["path"] = unknown[-1]["path"].rsplit("/", 1)[0] + "/UnreviewedPrefab"
    duplicate = copy.deepcopy(rows)
    duplicate[-1] = copy.deepcopy(duplicate[0])
    probes = [
        ("unknown-field-rejected", unknown),
        ("missing-field-rejected", rows[:-1]),
        ("duplicate-field-rejected", duplicate),
    ]
    for name, probe in probes:
        try:
            validate_exclusions(probe)
        except ValueError:
            results.append({"case": name, "result": "Passed"})
        else:
            raise ValueError("negative control failed: " + name)
    return results


def generate(repo: Path, capture_dir: Path, descriptor_dir: Path, output: Path):
    repo, capture_dir, descriptor_dir, output = map(lambda p: p.resolve(),
        (repo, capture_dir, descriptor_dir, output))
    need(not output.exists(), "fresh output directory required")
    raw_path = capture_dir / "data/prefab-reference-data.json"
    failure_path = capture_dir / "failure-receipt.json"
    log_path = capture_dir / "editor.log"
    key_path = descriptor_dir / "prefab-keys.json"
    key_receipt_path = descriptor_dir / "receipt.json"
    raw, failure = read_json(raw_path), read_json(failure_path)
    descriptor, descriptor_receipt = read_json(key_path), read_json(key_receipt_path)
    need(failure.get("status") == "Failed" and failure.get("required_marker_present") is False and
         failure.get("artifact_admitted") is False and failure.get("mission_ready") is False,
         "preserved failed, unadmitted capture receipt required")
    need(failure.get("diagnostic_artifact_sha256") == sha(raw_path) and
         failure.get("editor_log_sha256") == sha(log_path), "failed capture/log pin mismatch")
    need(failure.get("key_descriptor_sha256") == sha(key_path) and
         descriptor_receipt.get("descriptor_sha256") == sha(key_path) and
         descriptor_receipt.get("status") == "PassedSourceOnly", "exact source key descriptor chain required")
    need(raw.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         raw.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
         raw.get("requested_prefab_keys") == descriptor and raw.get("requested_prefab_keys_sha256") == sha(key_path),
         "failed capture key domain does not match descriptor")
    pins = raw.get("source_pins")
    need(isinstance(pins, dict) and pins, "raw source pins required")
    for raw_pin, digest in pins.items():
        path = Path(raw_pin)
        if not path.is_absolute():
            path = repo / path
        need(path.is_absolute() and path.is_file() and isinstance(digest, str) and len(digest) == 64 and sha(path) == digest,
             "raw capture source pin mismatch: " + raw_pin)

    basis_paths = [
        "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/project_s004_focused_prefab_presentation_refs.py",
        "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/run_s004_focused_prefab_compiled_preflight.py",
        "Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs",
        "Tools/CI/SkirmishSourceSimulation/Adapters/SourceSimulationProgram.cs",
        "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073ProductionPrefabData.cs",
        "Assets/Game/Scripts/Components/UnitCombatComponents.cs",
        "Assets/Game/Scripts/Components/AirMissileComponents.cs",
        "Assets/Game/Scripts/Systems/UnitAttackSystem.cs",
        "Assets/Game/Scripts/Systems/AirMissileLauncherSystems.cs",
    ]
    basis_pins = {str(repo / path): sha(repo / path) for path in basis_paths}

    exclusions = validate_exclusions(raw.get("unsupported_values"))
    entities = raw.get("entities")
    need(isinstance(entities, list) and entities, "captured entity closure required")
    entity_rows = {}
    for entity in entities:
        eid = canonical_entity(entity.get("id"))
        need(eid not in entity_rows, "duplicate captured entity ID")
        entity_rows[eid] = entity
    for row in exclusions:
        eid, component, field = row["path"].split("/")
        entry = entity_rows.get(eid, {}).get("components", {}).get(component)
        need(isinstance(entry, dict) and entry.get("present") is True and entry.get("buffer") is False,
             "excluded value must correspond to a present scalar component")
        value_fields = entry.get("value", {}).get("fields", {})
        field_key = component + "::" + field
        field_value = value_fields.get(field_key)
        need(isinstance(field_value, dict) and field_value.get("codec") == "unsupported-v1" and
             field_value.get("reason") == REASON and field_value.get("type") == UNITY_OBJECT_REF,
             "exclusion ledger differs from exact captured nested field")

    projected_entities = copy.deepcopy(entities)
    projected_by_id = {row["id"]: row for row in projected_entities}
    for row in exclusions:
        eid, component, field = row["path"].split("/")
        del projected_by_id[eid]["components"][component]["value"]["fields"][component + "::" + field]

    projection = {
        "schema_version": 1,
        "kind": "S004FocusedPrefabSourceOnlyPresentationExcludedProjection",
        "status": "PassedSourceOnlyProjection",
        "original_capture": {
            "path": str(raw_path.relative_to(repo)), "sha256": sha(raw_path), "status": "Failed",
            "failure_receipt": str(failure_path.relative_to(repo)), "failure_receipt_sha256": sha(failure_path),
            "editor_log": str(log_path.relative_to(repo)), "editor_log_sha256": sha(log_path),
            "raw_artifact_admitted": False,
        },
        "key_descriptor": {"path": str(key_path.relative_to(repo)), "sha256": sha(key_path),
                           "receipt": str(key_receipt_path.relative_to(repo)), "receipt_sha256": sha(key_receipt_path)},
        # Keep the actual bake authority facts needed by the compiled importer
        # preflight. These are descriptive source facts, never import admission.
        "source_authority": raw["authority"],
        "template_namespace_id": raw["template_namespace_id"],
        "source_pins": pins,
        "projection_basis_pins": basis_pins,
        "requested_prefab_keys": raw["requested_prefab_keys"],
        "opening_prefabs": raw["opening_prefabs"],
        "entities": projected_entities,
        "excluded_presentation_object_refs": exclusions,
        "excluded_count": len(exclusions),
        "remaining_unsupported_values": [],
        "runtime_import_attempted": False,
        "runtime_replay_ready": False,
        "mission_ready": False,
        "native_visual_readiness": False,
        "geometry_exported": False,
        "negative_controls": controls(raw["unsupported_values"]),
        "consumer_scope": "Source-only structural/mechanics inspection. The excluded references are consumed by runtime presentation systems; the projection has incomplete muzzle/impact/missile visuals and is not a runtime registry, replay, gameplay, or readiness artifact.",
    }
    output.mkdir(parents=True)
    out_path = output / "projection.json"
    out_path.write_text(json.dumps(projection, indent=2) + "\n")
    receipt = {"schema_version": 1, "kind": "S004FocusedPrefabSourceOnlyProjectionReceipt",
               "status": projection["status"], "projection": out_path.name, "projection_sha256": sha(out_path),
               "excluded_count": 16, "remaining_unsupported_count": 0,
               "original_capture_status": "Failed", "runtime_import_attempted": False,
               "runtime_replay_ready": False, "mission_ready": False,
               "negative_controls": projection["negative_controls"]}
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n")
    return {"status": receipt["status"], "projection_sha256": sha(out_path),
            "receipt_sha256": sha(receipt_path), "excluded": 16, "output": str(output)}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", type=Path, required=True)
    p.add_argument("--capture-dir", type=Path, required=True)
    p.add_argument("--descriptor-dir", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    a = p.parse_args()
    try:
        print(json.dumps(generate(a.repo, a.capture_dir, a.descriptor_dir, a.output), indent=2))
    except Exception as exc:
        raise SystemExit("S004 focused prefab source-only projection failed closed: " + str(exc))


if __name__ == "__main__":
    main()
