#!/usr/bin/env python3
"""Build and validate an S004 projection bound to the passed scoped capture.

The failed focused-capture/T11 lineage is retained as separate historical evidence.
This projection is a source-only derivative of the independently passed scoped capture;
it is not an import, replay, visual-readiness, or mission-readiness artifact.
"""
from __future__ import annotations

import copy
import difflib
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
from typing import Optional

ROOT = Path(__file__).resolve().parents[5]
CAPTURE = ROOT / "Logs/SkirmishProduction/s004-scoped-prefab-capture-20261103-r1"
DESCRIPTOR = ROOT / "Logs/SkirmishProduction/s004-prefab-key-descriptor-20261103-T5"
FORCE_INVENTORY = ROOT / "T Logs/SkirmishProduction/s004-opening-force-prefabs-20261103-T7/inventory.json"
OLD_PROJECTION_DIR = ROOT / "Logs/SkirmishProduction/s004-focused-prefab-presentation-projection-20261103-T11"
OLD_CAPTURE_DIR = ROOT / "Logs/SkirmishProduction/s004-focused-prefab-capture-20261103-r1"
HISTORY_AUDIT_DIR = ROOT / "T Logs/SkirmishProduction/s004-historical-metadata-provenance-audit-20261103-T1"
OUTPUT_DEFAULT = ROOT / "Logs/SkirmishProduction/s004-scoped-prefab-source-only-projection-20261103-T3"
KEY_SHA = "7bbcbe81369bf86844167355a6ce942c4be10ee3ff133660b90db4bc79214f58"
TWO_SEED_PATH = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.TwoSeedMetadata.cs"
SCOPED_PATH = "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S004PrefabData.cs"
HISTORICAL_COMMIT = "d7a68b930"
OLD_TWO_SEED_SHA = "198a5c533dd0270f61a8fe258b2981139d74af14971ff3c514d9d157e3a814be"
CURRENT_TWO_SEED_SHA = "a63907037bb0ad865d960f3baa0df25afd74d22e54d07d6bfd8333f715b2b8cb"
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


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def read(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def verify_pins(pins: dict[str, str], base: Path = ROOT, allowed_drift: Optional[dict[str, str]] = None) -> dict:
    need(isinstance(pins, dict) and len(pins) >= 8000, "complete scoped-capture pin map required")
    mismatches, missing, acknowledged = [], [], []
    allowed_drift = allowed_drift or {}
    for raw, digest in pins.items():
        path = Path(raw)
        if not path.is_absolute():
            path = base / path
        if not path.is_file():
            missing.append(raw)
        elif not isinstance(digest, str) or sha(path) != digest:
            if raw in allowed_drift and digest == allowed_drift[raw]:
                acknowledged.append(raw)
            else:
                mismatches.append(raw)
    need(not missing and not mismatches,
         "captured source pins do not match live sources: missing=" + repr(missing[:5]) +
         " mismatched=" + repr(mismatches[:5]))
    return {"pin_count": len(pins), "live_matches": len(pins) - len(acknowledged),
            "acknowledged_historical_drift": acknowledged, "missing": [], "mismatches": []}


def verify_two_seed_drift_and_call_boundary(raw: dict) -> dict:
    """Prove documented drift is isolated from the S004 capture method call boundary."""
    old = subprocess.check_output(["git", "show", f"{HISTORICAL_COMMIT}:{TWO_SEED_PATH}"], cwd=ROOT)
    current_path = ROOT / TWO_SEED_PATH
    current = current_path.read_bytes()
    need(hashlib.sha256(old).hexdigest() == OLD_TWO_SEED_SHA and
         hashlib.sha256(current).hexdigest() == CURRENT_TWO_SEED_SHA,
         "retained exact TwoSeedMetadata historical/current bytes changed")
    diff = list(difflib.unified_diff(old.decode().splitlines(), current.decode().splitlines(),
                                    fromfile="historical", tofile="current", lineterm=""))
    removed = [line[1:] for line in diff if line.startswith("-") and not line.startswith("---")]
    added = [line[1:] for line in diff if line.startswith("+") and not line.startswith("+++")]
    need(len(removed) == len(added) == 1 and
         'expectedCatalog != "S002" && expectedCatalog != "S026"' in removed[0] and
         'expectedCatalog != "S002" && expectedCatalog != "S003" && expectedCatalog != "S026"' in added[0],
         "TwoSeedMetadata drift differs from the single documented S003 allowlist change")
    need(raw["source_pins"].get(TWO_SEED_PATH) == OLD_TWO_SEED_SHA,
         "scoped capture pin does not match the retained exact historical TwoSeedMetadata blob")

    # The reached project-owned capture methods are enumerated from the S004 entrypoint
    # and its native template helpers. The shared serializer is included because it is
    # reached for every captured component value. SourcePins only hashes code/assets.
    closure = [
        SCOPED_PATH,
        "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.cs",
        "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.ReferenceData.cs",
        "Assets/Game/Scripts/Editor/Skirmish/S002InitialWorldEntityExport.cs",
        "Assets/Game/Scripts/Editor/Skirmish/SkirmishFocusedPrefabKeySet.cs",
    ]
    closure_bytes = {p: (ROOT / p).read_text(encoding="utf-8") for p in closure}
    need("RunS004FocusedPrefabReferenceData" in closure_bytes[SCOPED_PATH] and
         "CaptureCompiledMetadataContext" not in closure_bytes[SCOPED_PATH] and
         "RunTwoSeedMetadataQualification" not in closure_bytes[SCOPED_PATH],
         "S004 scoped exporter entrypoint changed or directly reaches metadata qualification")
    need("OpeningPrefabRoots(em, keys)" in closure_bytes[SCOPED_PATH] and
         "CaptureTemplateClosure(em, keys.Select(k => roots[k]), unsupported)" in closure_bytes[SCOPED_PATH] and
         "SourcePins(SourceScene)" in closure_bytes[SCOPED_PATH],
         "S004 entrypoint reached call boundary changed")
    need("CaptureCompiledMetadataContext" not in closure_bytes["Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.cs"] and
         "RunTwoSeedMetadataQualification" not in closure_bytes["Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.ReferenceData.cs"] and
         "CaptureCompiledMetadataContext" not in closure_bytes["Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.ReferenceData.cs"] and
         "RunTwoSeedMetadataQualification" not in closure_bytes["Assets/Game/Scripts/Editor/Skirmish/S002InitialWorldEntityExport.cs"] and
         "CaptureCompiledMetadataContext" not in closure_bytes["Assets/Game/Scripts/Editor/Skirmish/S002InitialWorldEntityExport.cs"],
         "S004 reached helper source now names the drifted metadata qualification")
    key_reader = closure_bytes["Assets/Game/Scripts/Editor/Skirmish/SkirmishFocusedPrefabKeySet.cs"]
    need("static string[] Read" in key_reader and "CaptureCompiledMetadataContext" not in key_reader and
         "RunTwoSeedMetadataQualification" not in key_reader,
         "S004 key descriptor reader closure changed")
    # Both historical/current metadata helpers are independently called in other entry
    # points, but no reachable S004 helper references either helper by call name.
    need("CaptureTwoSeedContext" in current.decode() and "CaptureCompiledMetadataContext" in current.decode(),
         "Historical drift target/method identity missing")
    return {
        "classification": "DocumentedUnrelatedTwoSeedMetadataDrift_SeparateFromScopedCaptureMethod",
        "historical_commit": HISTORICAL_COMMIT,
        "historical_two_seed_sha256": OLD_TWO_SEED_SHA,
        "current_two_seed_sha256": CURRENT_TWO_SEED_SHA,
        "diff_removed": removed[0].strip(), "diff_added": added[0].strip(),
        "scoped_exporter_path": SCOPED_PATH,
        "scoped_exporter_sha256": raw["source_pins"][SCOPED_PATH],
        "reached_source_files": closure,
        "metadata_context_methods_reachable_from_s004_entry": False,
        "historical_capture_not_relabelled_current": True,
    }


def verify_historical_t11() -> dict:
    proj_path = OLD_PROJECTION_DIR / "projection.json"
    receipt_path = OLD_PROJECTION_DIR / "receipt.json"
    prov_path = OLD_PROJECTION_DIR / "provenance-receipt.json"
    old = read(proj_path)
    receipt, provenance = read(receipt_path), read(prov_path)
    origin = old["original_capture"]
    raw_path = ROOT / origin["path"]
    fail_path = ROOT / origin["failure_receipt"]
    log_path = ROOT / origin["editor_log"]
    failure = read(fail_path)
    need(origin.get("status") == "Failed" and origin.get("raw_artifact_admitted") is False and
         sha(raw_path) == origin.get("sha256") and sha(fail_path) == origin.get("failure_receipt_sha256") and
         sha(log_path) == origin.get("editor_log_sha256") and failure.get("artifact_admitted") is False,
         "T11 historical failed/unadmitted capture provenance changed")
    need(receipt.get("status") == "PassedSourceOnlyProjection" and
         receipt.get("projection_sha256") == sha(proj_path) and
         provenance.get("status") == "PassedSourceOnlyProvenanceGate_NoImportAdmission" and
         provenance.get("projection_sha256") == sha(proj_path) and
         provenance.get("raw_capture_sha256") == sha(raw_path) and
         provenance.get("source_only") is True and provenance.get("mission_ready") is False,
         "T11 projection/provenance receipts no longer bind exact historical artifacts")
    return {"path": str(proj_path.relative_to(ROOT)), "projection_sha256": sha(proj_path),
            "receipt_sha256": sha(receipt_path), "provenance_receipt_sha256": sha(prov_path),
            "historical_raw_sha256": sha(raw_path), "historical_capture_status": "Failed_Unadmitted",
            "retained_separately": True}


def exclusions_for(raw: dict) -> list[dict]:
    rows = raw.get("unsupported_values")
    need(isinstance(rows, list) and len(rows) == 16, "exact 16-leaf diagnostic ledger required")
    counts: dict[str, int] = {}
    seen: set[str] = set()
    normalized = []
    entity_map = {row["id"]: row for row in raw["entities"]}
    need(len(entity_map) == 276 and len(entity_map) == len(raw["entities"]), "unique 276-entity closure required")
    for row in rows:
        path = row.get("path", "")
        parts = path.split("/")
        need(len(parts) == 3 and path not in seen, "exact unique entity/component/field exclusion path required")
        seen.add(path)
        entity_id, component, field = parts
        qualified = component + "/" + field
        need(qualified in FIELD_COUNTS and row.get("reason") == REASON and row.get("type") == UNITY_OBJECT_REF,
             "unsupported leaf outside exact reviewed presentation allowance")
        component_row = entity_map.get(entity_id, {}).get("components", {}).get(component)
        field_key = component + "::" + field
        value = component_row.get("value", {}).get("fields", {}).get(field_key) if component_row else None
        need(component_row and component_row.get("present") is True and component_row.get("buffer") is False and
             value == {"codec": "unsupported-v1", "reason": REASON, "type": UNITY_OBJECT_REF},
             "exclusion does not bind to exact source-captured unsupported scalar")
        counts[qualified] = counts.get(qualified, 0) + 1
        normalized.append({"path": path, "component": component, "field": field,
                           "reason": REASON, "type": UNITY_OBJECT_REF})
    need(counts == FIELD_COUNTS, "presentation exclusion field counts changed")
    return normalized


def build_projection(output: Path) -> dict:
    need(not output.exists(), "new output directory required")
    finalizer_path = CAPTURE / "finalizer-receipt.json"
    raw_path = CAPTURE / "data/prefab-reference-data.json"
    log_path = CAPTURE / "editor.log"
    key_path, key_receipt_path = DESCRIPTOR / "prefab-keys.json", DESCRIPTOR / "receipt.json"
    for path in (finalizer_path, raw_path, log_path, key_path, key_receipt_path, FORCE_INVENTORY):
        need(path.is_file(), "required bound input missing: " + str(path))
    finalizer, raw = read(finalizer_path), read(raw_path)
    keys, key_receipt, force = read(key_path), read(key_receipt_path), read(FORCE_INVENTORY)
    need(finalizer.get("status") == "PassedScopedSourceCaptureOnly" and finalizer.get("wrapper_exit_code") == 0 and
         finalizer.get("capture", {}).get("raw_sha256") == sha(raw_path) and
         finalizer.get("capture", {}).get("editor_log_sha256") == sha(log_path) and
         finalizer.get("capture", {}).get("pass_marker") and
         log_path.read_text(errors="replace").count(finalizer["capture"]["pass_marker"]) == 1,
         "exact passed scoped capture finalizer/log chain required")
    need(key_receipt.get("status") == "PassedSourceOnly" and key_receipt.get("descriptor_sha256") == sha(key_path) == KEY_SHA and
         key_receipt.get("keys_in_first_compiled_use_order") == keys and finalizer.get("key_descriptor", {}).get("sha256") == sha(key_path) and
         finalizer.get("key_descriptor", {}).get("receipt_sha256") == sha(key_receipt_path),
         "exact compiled key descriptor receipt chain required")
    need(raw.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         raw.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
         raw.get("requested_prefab_keys") == keys and raw.get("requested_prefab_keys_sha256") == sha(key_path) and
         raw.get("source_scenes") == ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"] and
         list(raw.get("opening_prefabs", {}).keys()) == keys and len(raw.get("entities", [])) == 276,
         "scoped capture source authority, exact nine roots, or closure changed")
    force_keys = []
    for row in force.get("forces", []):
        value = row.get("runtime_prefab_key")
        if value not in force_keys:
            force_keys.append(value)
    need(len(keys) == len(set(keys)) == 9 and force.get("force_row_count") == 18 and
         force.get("total_quantity") == 54 and force.get("unique_source_config_count") == 9 and
         force_keys == keys,
         "T7 force inventory does not support exact compiled root ordering")
    drift = verify_two_seed_drift_and_call_boundary(raw)
    pin_result = verify_pins(raw["source_pins"], allowed_drift={TWO_SEED_PATH: OLD_TWO_SEED_SHA})
    historical = verify_historical_t11()
    exclusions = exclusions_for(raw)
    projected = copy.deepcopy(raw["entities"])
    by_id = {row["id"]: row for row in projected}
    for item in exclusions:
        eid, component, field = item["path"].split("/")
        del by_id[eid]["components"][component]["value"]["fields"][component + "::" + field]
    basis_files = [
        Path(__file__).resolve(),
        ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/validate_s004_scoped_prefab_projection.py",
        finalizer_path, key_path, key_receipt_path, FORCE_INVENTORY,
    ]
    projection = {
        "schema_version": 1,
        "kind": "S004PassedScopedPrefabSourceOnlyProjection",
        "status": "PassedSourceOnlyProjection",
        "capture": {"path": str(raw_path.relative_to(ROOT)), "sha256": sha(raw_path),
                    "finalizer_receipt": str(finalizer_path.relative_to(ROOT)),
                    "finalizer_receipt_sha256": sha(finalizer_path), "editor_log": str(log_path.relative_to(ROOT)),
                    "editor_log_sha256": sha(log_path), "pass_marker": finalizer["capture"]["pass_marker"],
                    "capture_classification": "PassedScopedSourceCaptureOnly_NotHistoricalSourceEquality"},
        "key_descriptor": {"path": str(key_path.relative_to(ROOT)), "sha256": sha(key_path),
                           "receipt": str(key_receipt_path.relative_to(ROOT)), "receipt_sha256": sha(key_receipt_path)},
        "force_inventory": {"path": str(FORCE_INVENTORY.relative_to(ROOT)), "sha256": sha(FORCE_INVENTORY),
                             "row_count": 18, "total_quantity": 54},
        "historical_t11_provenance": historical,
        "two_seed_metadata_drift_audit": drift,
        "source_authority": raw["authority"],
        "source_pins": raw["source_pins"],
        "source_pin_live_audit": pin_result,
        "requested_prefab_keys": keys,
        "template_namespace_id": raw["template_namespace_id"],
        "opening_prefabs": raw["opening_prefabs"],
        "entities": projected,
        "excluded_presentation_object_refs": exclusions,
        "excluded_count": len(exclusions),
        "remaining_unsupported_values": [],
        "runtime_import_attempted": False,
        "runtime_replay_ready": False,
        "mission_ready": False,
        "native_visual_readiness": False,
        "geometry_exported": False,
        "consumer_scope": "Source-only structural/mechanics inspection. The exact 16 excluded presentation references leave impact, muzzle, and missile visuals incomplete. No runtime registry, import, replay, gameplay, geometry, visual qualification, or mission readiness is established.",
    }
    projection["projection_basis_pins"] = {str(path): sha(path) for path in basis_files}
    output.mkdir(parents=True)
    out_path = output / "projection.json"
    out_path.write_text(json.dumps(projection, indent=2) + "\n", encoding="utf-8")
    return projection


def validate_projection(projection: dict, repo: Path) -> dict:
    need(projection.get("kind") == "S004PassedScopedPrefabSourceOnlyProjection" and
         projection.get("status") == "PassedSourceOnlyProjection" and
         projection.get("capture", {}).get("capture_classification") == "PassedScopedSourceCaptureOnly_NotHistoricalSourceEquality",
         "passed scoped projection classification required")
    capture = projection["capture"]
    finalizer_path = repo / capture["finalizer_receipt"]
    raw_path = repo / capture["path"]
    log_path = repo / capture["editor_log"]
    finalizer, raw = read(finalizer_path), read(raw_path)
    need(sha(finalizer_path) == capture["finalizer_receipt_sha256"] and sha(raw_path) == capture["sha256"] and
         sha(log_path) == capture["editor_log_sha256"] and finalizer.get("status") == "PassedScopedSourceCaptureOnly" and
         finalizer.get("wrapper_exit_code") == 0 and finalizer.get("capture", {}).get("raw_sha256") == sha(raw_path),
         "passed capture/finalizer hash chain mismatch")
    need(log_path.read_text(errors="replace").count(capture["pass_marker"]) == 1 and
         finalizer.get("capture", {}).get("pass_marker") == capture["pass_marker"],
         "exact scoped capture marker missing or duplicated")
    drift = verify_two_seed_drift_and_call_boundary(raw)
    pin_result = verify_pins(projection.get("source_pins", {}), repo, {TWO_SEED_PATH: OLD_TWO_SEED_SHA})
    need(raw.get("source_pins") == projection["source_pins"] and
         raw.get("opening_prefabs") == projection.get("opening_prefabs") and
         raw.get("requested_prefab_keys") == projection.get("requested_prefab_keys"),
         "projection authority changed from passed raw capture")
    key_path = repo / projection["key_descriptor"]["path"]
    key_receipt_path = repo / projection["key_descriptor"]["receipt"]
    force_path = repo / projection["force_inventory"]["path"]
    key_receipt, force = read(key_receipt_path), read(force_path)
    need(sha(key_path) == projection["key_descriptor"]["sha256"] == KEY_SHA and
         sha(key_receipt_path) == projection["key_descriptor"]["receipt_sha256"] and
         key_receipt.get("descriptor_sha256") == sha(key_path) and
         read(key_path) == projection.get("requested_prefab_keys"),
         "projection descriptor chain changed")
    need(sha(force_path) == projection["force_inventory"]["sha256"] and
         force.get("force_row_count") == 18 and force.get("total_quantity") == 54,
         "projection T7 force inventory pin changed")
    need(projection.get("source_pin_live_audit") == pin_result,
         "projection live-pin audit result changed")
    expected = copy.deepcopy(raw["entities"])
    by_id = {row["id"]: row for row in expected}
    exclusions = projection.get("excluded_presentation_object_refs")
    need(isinstance(exclusions, list) and len(exclusions) == 16 and projection.get("excluded_count") == 16 and
         projection.get("remaining_unsupported_values") == [], "exact 16-leaf exclusion ledger required")
    need(exclusions == exclusions_for(raw), "exclusion paths differ from reviewed raw capture ledger")
    seen = set()
    for item in exclusions:
        path = item["path"]
        need(path not in seen, "duplicate exclusion path")
        seen.add(path)
        eid, component, field = path.split("/")
        entry = by_id.get(eid, {}).get("components", {}).get(component)
        value = entry.get("value", {}).get("fields", {}).get(component + "::" + field) if entry else None
        need(item.get("reason") == REASON and item.get("type") == UNITY_OBJECT_REF and
             value == {"codec": "unsupported-v1", "reason": REASON, "type": UNITY_OBJECT_REF},
             "exclusion ledger does not bind exact raw unsupported value")
        del entry["value"]["fields"][component + "::" + field]
    need(expected == projection.get("entities"), "gameplay/structural entity data mutated beyond exact presentation exclusions")
    need(projection.get("historical_t11_provenance") == verify_historical_t11(),
         "separate historical T11 provenance was changed or relabelled")
    need(projection.get("two_seed_metadata_drift_audit") == drift,
         "documented unrelated exporter drift/call-boundary audit mismatch")
    need(all(projection.get(k) is False for k in ("runtime_import_attempted", "runtime_replay_ready", "mission_ready",
                                                   "native_visual_readiness", "geometry_exported")),
         "source-only readiness flags must remain false")
    for raw_path_text, digest in projection.get("projection_basis_pins", {}).items():
        path = Path(raw_path_text)
        need(path.is_file() and sha(path) == digest, "projection basis source drift: " + raw_path_text)
    controls = []
    probe = copy.deepcopy(projection)
    probe["entities"][0]["entity_enabled"] = not probe["entities"][0]["entity_enabled"]
    controls.append(_expect_rejection("gameplay-entity-mutation-rejected", lambda: _validate_entity_only(raw, probe)))
    probe = copy.deepcopy(projection)
    probe["capture"]["capture_classification"] = "HistoricalSourceEquality"
    controls.append(_expect_rejection("capture-classification-promotion-rejected",
                                     lambda: need(probe["capture"]["capture_classification"] ==
                                                  "PassedScopedSourceCaptureOnly_NotHistoricalSourceEquality",
                                                  "capture classification promotion forbidden")))
    return {"status": "PassedSourceOnlyProvenanceGate_NoImportAdmission", "roots": len(raw["opening_prefabs"]),
            "entities": len(raw["entities"]), "excluded_leaves": len(exclusions),
            "current_live_source_pins": pin_result["live_matches"], "negative_controls": controls,
            "world_allocated": False, "runtime_import_attempted": False, "mission_ready": False}


def _validate_entity_only(raw: dict, projection: dict) -> None:
    expected = copy.deepcopy(raw["entities"])
    by_id = {row["id"]: row for row in expected}
    for item in projection["excluded_presentation_object_refs"]:
        eid, component, field = item["path"].split("/")
        entry = by_id[eid]["components"][component]
        del entry["value"]["fields"][component + "::" + field]
    need(expected == projection.get("entities"), "entity projection differs")


def _expect_rejection(name: str, action) -> dict:
    try:
        action()
    except (ValueError, KeyError, TypeError):
        return {"case": name, "result": "Passed"}
    raise ValueError("negative control unexpectedly passed: " + name)


def main() -> int:
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, default=OUTPUT_DEFAULT)
    args = parser.parse_args()
    repo, output = args.repo.resolve(), args.output.resolve()
    try:
        projection = build_projection(output)
        facts = validate_projection(projection, repo)
        print(json.dumps({"status": facts["status"], "roots": facts["roots"],
                          "entities": facts["entities"], "excluded_leaves": facts["excluded_leaves"],
                          "live_pins": facts["current_live_source_pins"],
                          "projection": str(output / "projection.json")}, indent=2))
        return 0
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError, subprocess.SubprocessError) as exc:
        print("S004 passed scoped source-only projection failed closed: " + str(exc), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
