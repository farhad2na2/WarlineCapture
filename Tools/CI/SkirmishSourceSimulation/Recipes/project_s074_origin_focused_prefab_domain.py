#!/usr/bin/env python3
"""Project the failed S074 native prefab capture into its exact unconsumed-VFX read domain.

The raw Unity artifact is preserved byte-for-byte. This projection binds it to the
current S074 compiler context and qualification, and permits only the existing
importer's exact presentation-only UnityObjectRef omissions.
"""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[4]
RAW = ROOT / "Logs/SkirmishProduction/s074-owner-data-capture-20261107-r1/prefab-units-16/prefab-reference-data.json"
CAPTURE_LOG = ROOT / "T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/prefab-units-capture.log"
WRAPPER_LOG = ROOT / "T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/prefab-units-wrapper.log"
UNIT_KEYS = ROOT / "T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/s074-unit-16-keys.json"
CONTEXT = ROOT / "T Logs/SkirmishProduction/s074-source-origin-factory-export-20261107-r1/s074/source-factory-context.json"
QUALIFICATION = ROOT / "T Logs/SkirmishProduction/s074-source-origin-runtime-inputs-20261107-r1/qualification.json"
GRAPH = ROOT / "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json"
BUILDINGS = ROOT / "T Logs/SkirmishProduction/s074-source-origin-factory-projection-20261107-r1/building-definitions.json"
UNITS = ROOT / "T Logs/SkirmishProduction/s074-source-origin-factory-projection-20261107-r1/unit-production-metadata.json"

RAW_SHA = "e6c68c4bc85bf959a2de5611f37e199c0b61eb737019369c582f92b75b91c7a9"
UNIT_KEYS_SHA = "2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7"
CONTEXT_SHA = "254fa7f2cf9bb76a0cf8e26e73f5cbcfd2118e06f14d5b56a84b0004f2911d6c"
QUALIFICATION_SHA = "0887f2afb948fd2155334eba30b2fccf8f68130528b302e864f30b52f7f72c6e"
GRAPH_SHA = "edc4081763a3252716679fc4e36dae2b8341aa6e9c8426fa552e143096d510cb"
BUILDINGS_SHA = "14c9979f850f12e315a1f585aa65f48ac50ea7df4760c6c8658ac95d2922cd54"
UNITS_SHA = "cfc835a7f37566fee12f0770669dabe5a579446ef7c8d79c8535c7d6e3d5a365"
CAPTURE_LOG_SHA = "6be195f37bb178b07c97a4fe043a37bd6747c68e71fe19d0dfb70e3ce6799eac"
WRAPPER_LOG_SHA = "61629cf94be315882abacdc98faeee508742d5da7a73ba3bca94f45ecd4f0011"
FIELDS = ["schema_version", "kind", "authority", "source_scenes", "unity_version", "template_namespace_id",
          "requested_prefab_keys", "requested_prefab_keys_sha256", "opening_prefabs", "entities", "unsupported_values",
          "replay_ready", "fresh_world_allocator_exported", "full_mission_initialized", "native_visual_readiness",
          "geometry_exported", "consumer_rule"]
ALLOWED = {
    "Game.Components.UnitAttackImpactVfxReference": {"Prefab"},
    "Game.Components.UnitMuzzleFlashVfxReference": {"Prefab"},
    "Game.Components.GroundMissileLauncherVfxReferenceComponent": {
        "LauncherBackfirePrefab", "RocketTrailPrefab", "ImpactExplosionPrefab", "ImpactSmokePrefab"},
    "Game.Components.AirMissileLauncherVfxReferenceComponent": {
        "MissileVisualPrefab", "LaunchFlashPrefab", "LaunchSmokePrefab", "MissileTrailPrefab",
        "AirburstExplosionPrefab", "AirTargetImpactPrefab", "InterceptExplosionPrefab"},
}
OBJECT_REF = "Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def desc(path):
    raw = path.read_bytes()
    return {"path": str(path.resolve(strict=True)), "sha256": sha(raw)}


def encode(value):
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def verify_source_pins(pins):
    need(isinstance(pins, dict) and len(pins) == 8087, "Exact 8,087 S074 focused-prefab source pins required")
    root = ROOT.resolve(strict=True)
    for path_text, expected in pins.items():
        path = Path(path_text)
        if path.is_absolute():
            resolved = path
            need(path == path.resolve(strict=True), "S074 absolute source pin must be canonical: " + path_text)
        else:
            need("\\" not in path_text and all(part not in ("", ".", "..") for part in path_text.split("/")) and
                 path.parts[0] in {"Assets", "Packages", "Library", "ProjectSettings", "T Logs"},
                 "S074 relative source pin must stay in an owner source domain: " + path_text)
            resolved = root / path
        need(resolved.is_relative_to(root), "S074 focused-prefab source pin must be inside current repository: " + path_text)
        path = resolved
        need(not path.is_symlink() and path.is_file() and sha(path.read_bytes()) == expected,
             "S074 focused-prefab source pin changed: " + path_text)


def verify_inputs():
    known = [(RAW, RAW_SHA), (UNIT_KEYS, UNIT_KEYS_SHA), (CONTEXT, CONTEXT_SHA),
             (QUALIFICATION, QUALIFICATION_SHA), (GRAPH, GRAPH_SHA), (BUILDINGS, BUILDINGS_SHA),
             (UNITS, UNITS_SHA), (CAPTURE_LOG, CAPTURE_LOG_SHA), (WRAPPER_LOG, WRAPPER_LOG_SHA)]
    for path, expected in known:
        need(path.is_file() and not path.is_symlink() and sha(path.read_bytes()) == expected,
             "Exact retained S074 capture input changed: " + str(path))
    raw_bytes = RAW.read_bytes()
    capture = json.loads(raw_bytes)
    context = json.loads(CONTEXT.read_bytes())
    qualification = json.loads(QUALIFICATION.read_bytes())
    graph = json.loads(GRAPH.read_bytes())
    buildings = json.loads(BUILDINGS.read_bytes())
    units = json.loads(UNITS.read_bytes())
    key_list = json.loads(UNIT_KEYS.read_bytes())
    need(context.get("kind") == "OriginalCompilerDerivedNativeFactoryContext" and
         context.get("target_context", {}).get("catalog_id") == "S074" and
         context["target_context"].get("seed") == 104803 and context["target_context"].get("setup_hash") == 1716123768 and
         context["target_context"].get("operation_map_id") == "opmap.skirmish.industrial_basin",
         "Exact S074 origin compiler context required")
    need(qualification.get("kind") == "ExperimentalGroundConfigurationQualification" and
         qualification.get("compiler_context") == {"path": str(CONTEXT), "sha256": CONTEXT_SHA} and
         qualification.get("runtime_backend") == "TypedGroundS026V1" and qualification.get("native_context_qualified") is False and
         qualification.get("mission_ready") is False,
         "Exact external S074 configuration qualification required")
    need(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
         graph.get("world_initialized") is False and graph.get("native_world_state_used") is False,
         "Current uncompiled S074 authored graph required")
    need(buildings.get("catalog_id") == units.get("catalog_id") == "S074" and
         buildings.get("setup_hash") == units.get("setup_hash") == 1716123768 and
         buildings.get("seed") == units.get("seed") == 104803,
         "Current S074 owner metadata identity required")
    actual_keys = [row["key"] for row in units.get("units", [])]
    need(actual_keys == key_list and len(actual_keys) == 16 and len(set(actual_keys)) == 16,
         "Unit key input differs from the actual S074 metadata selection")
    need(capture.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
         capture.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
         capture.get("source_scenes") == ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"] and
         capture.get("requested_prefab_keys") == actual_keys and
         capture.get("requested_prefab_keys_sha256") == UNIT_KEYS_SHA and
         len(capture.get("opening_prefabs", {})) == 16 and len(capture.get("entities", [])) == 528,
         "Exact S074 selected-unit source-baker closure required")
    for name in ("replay_ready", "full_mission_initialized", "native_visual_readiness", "geometry_exported"):
        need(capture.get(name) is False, "Raw failed capture scope must remain explicit: " + name)
    by_id = {row["id"]: row for row in capture["entities"]}
    counts = {}
    for row in capture["unsupported_values"]:
        parts = row.get("path", "").split("/")
        need(len(parts) == 3 and parts[0] in by_id and parts[1] in ALLOWED and parts[2] in ALLOWED[parts[1]] and
             row.get("reason") == "unity_object_ref_requires_bound_source_identity" and row.get("type") == OBJECT_REF,
             "Non-presentation or malformed unsupported value in raw S074 capture")
        component = by_id[parts[0]].get("components", {}).get(parts[1])
        value = component.get("value", {}) if component else {}
        field = value.get("fields", {}).get(parts[1] + "::" + parts[2], {})
        need(component is not None and value.get("codec") == "struct-fields-v1" and
             field == {"codec": "unsupported-v1", "type": OBJECT_REF, "reason": row["reason"]},
             "Unsupported VFX row must match the actual captured component field")
        counts[parts[1] + "/" + parts[2]] = counts.get(parts[1] + "/" + parts[2], 0) + 1
    expected = {
        "Game.Components.UnitAttackImpactVfxReference/Prefab": 10,
        "Game.Components.UnitMuzzleFlashVfxReference/Prefab": 9,
        "Game.Components.GroundMissileLauncherVfxReferenceComponent/LauncherBackfirePrefab": 1,
        "Game.Components.GroundMissileLauncherVfxReferenceComponent/RocketTrailPrefab": 1,
        "Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactExplosionPrefab": 1,
        "Game.Components.GroundMissileLauncherVfxReferenceComponent/ImpactSmokePrefab": 1,
    }
    need(counts == expected, "S074 VFX omission field cardinality changed")
    component_counts = {}
    for row in capture["entities"]:
        for component in ALLOWED:
            if component in row.get("components", {}):
                component_counts[component] = component_counts.get(component, 0) + 1
    need(component_counts == {
        "Game.Components.UnitAttackImpactVfxReference": 10,
        "Game.Components.UnitMuzzleFlashVfxReference": 9,
        "Game.Components.GroundMissileLauncherVfxReferenceComponent": 1,
    }, "S074 exact import-time presentation component omission set changed")
    capture_log = CAPTURE_LOG.read_text(errors="replace")
    need("InvalidOperationException: Focused prefab reference contains unsupported values: 23" in capture_log and
         "[SkirmishFocusedPrefabReferenceData] result=Passed" not in capture_log and "ExitCode: 0" in capture_log,
         "Retained raw capture failure evidence changed")
    verify_source_pins(capture.get("source_pins"))
    return capture, raw_bytes, context, qualification, graph, buildings, units, counts, component_counts


def run(output):
    capture, raw_bytes, context, qualification, graph, buildings, units, counts, component_counts = verify_inputs()
    output = output.resolve()
    need(output.is_relative_to(ROOT) and not output.exists(), "Fresh projection output inside repository required")
    output.mkdir(parents=True)
    capture_desc = desc(RAW)
    proof = {
        "schema_version": 1,
        "kind": "S074FocusedPrefabSourceOnlyCaptureProof",
        "status": "PassedSourceOnlyProjectionOfFailedRawCapture",
        "scope": "Exact captured S074 unit prefab closure; importer omits only the existing presentation-only VFX component set.",
        "raw_capture": capture_desc,
        "raw_capture_result": "Failed_UnconsumedPresentationUnityObjectRefs",
        "raw_capture_wrapper_exit_code": 0,
        "raw_capture_pass_marker_present": False,
        "raw_capture_log": desc(CAPTURE_LOG),
        "wrapper_log": desc(WRAPPER_LOG),
        "source_context": desc(CONTEXT),
        "configuration_qualification": desc(QUALIFICATION),
        "source_config_graph": desc(GRAPH),
        "building_definitions": desc(BUILDINGS),
        "unit_production_metadata": desc(UNITS),
        "unit_key_input": desc(UNIT_KEYS),
        "target": {"mission_id": "S074", "seed": 104803, "size": "Standard", "difficulty": "Regular",
                   "setup_hash": 1716123768, "operation_map_id": "opmap.skirmish.industrial_basin"},
        "source_pin_count": len(capture["source_pins"]),
        "source_pins_verified_current": True,
        "unsupported_vfx_rows_preserved": 23,
        "exact_unconsumed_fields": counts,
        "presentation_components_omitted_by_existing_importer": component_counts,
        "raw_capture_replay_ready": False,
        "runtime_import_attempted": False,
        "native_visual_readiness": False,
        "source_fit_certified": False,
        "mission_ready": False,
        "runtime_admission": False,
    }
    proof_path = output / "s074-focused-prefab-capture-proof.json"
    proof_path.write_bytes(encode(proof))
    proof_desc = desc(proof_path)
    domain = {key: capture[key] for key in FIELDS}
    domain["source_reference_sha256"] = capture_desc["sha256"]
    domain["derived_read_domain"] = "ExactTypedPrefabClosureOnly"
    slice_path = output / "s074-focused-prefab-domain.json"
    slice_bytes = encode(domain)
    slice_path.write_bytes(slice_bytes)
    selected = list(FIELDS)
    manifest = {
        "schema_version": 1,
        "kind": "PinnedExactTemplateDomainProjection",
        "source_path": capture_desc["path"], "source_sha256": capture_desc["sha256"],
        "source_bytes": len(raw_bytes), "slice_path": str(slice_path.resolve()),
        "slice_sha256": sha(slice_bytes), "slice_bytes": len(slice_bytes),
        "selected_fields": selected, "root_count": 16, "entity_count": 528,
        "template_namespace_id": capture["template_namespace_id"],
        "full_Unity_replay_ready": False, "unsupported_values_preserved": 23,
        "unsupported_by_type": {"UnityObjectRef<GameObject>": 23},
        "consumer_read_domain": "ExactTypedPrefabClosureOnly; existing importer omits 20 exact VFX components with 23 unconsumed UnityObjectRef leaves",
        "s074_capture_proof": proof_desc,
        "raw_capture_status": "Failed_UnconsumedPresentationUnityObjectRefs",
        "world_created": False, "runtime_import_attempted": False,
        "source_fit_certified": False, "native_visual_readiness": False, "mission_ready": False,
    }
    manifest_path = output / "template-slice-manifest.json"
    manifest_path.write_bytes(encode(manifest))
    manifest_desc = desc(manifest_path)
    descriptor = {
        **desc(slice_path),
        "source_reference_path": capture_desc["path"],
        "source_reference_sha256": capture_desc["sha256"],
        "projection_manifest_path": manifest_desc["path"],
        "projection_manifest_sha256": manifest_desc["sha256"],
        "s074_capture_proof": proof_desc,
    }
    descriptor_path = output / "reference-template-descriptor.json"
    descriptor_path.write_bytes(encode(descriptor))
    receipt = {
        "schema_version": 1, "kind": "S074FocusedPrefabSourceOnlyProjectionReceipt",
        "status": "PassedSourceOnlyProjectionOfFailedRawCapture",
        "raw_capture": capture_desc, "raw_capture_sha256": capture_desc["sha256"],
        "raw_capture_failure_preserved": True, "raw_capture_replay_ready": False,
        "projection_descriptor": desc(descriptor_path), "source_pin_count": len(capture["source_pins"]),
        "unsupported_vfx_rows_preserved": 23, "importer_omitted_component_count": 20,
        "runtime_import_attempted": False, "runtime_admission": False,
        "native_visual_readiness": False, "source_fit_certified": False, "mission_ready": False,
    }
    (output / "receipt.json").write_bytes(encode(receipt))
    print(json.dumps(receipt, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    run(Path(args.output))


if __name__ == "__main__":
    main()
