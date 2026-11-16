#!/usr/bin/env python3
"""Build the exact source-only S003 19-root prefab projection for consumer 419."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[4]
CAPTURE_REL = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/data/prefab-reference-data.json")
STRICT_REL = CAPTURE_REL.parent / "strict-receipt.json"
GRAPH_REL = Path("Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json")
GRAPH_SHA = "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb"
FINALIZER_REL = Path("Tools/CI/SkirmishSourceSimulation/Tests/finalize_s003_focused_prefab_capture.py")
IMPORTER_RUNNER_REL = Path("Tools/CI/SkirmishSourceSimulation/Recipes/run_s003_focused_template_source_host_controls.py")
FIELDS = [
    "schema_version", "kind", "authority", "mission_id", "source_scenes", "source_pins", "unity_version",
    "template_namespace_id", "requested_prefab_keys", "requested_prefab_keys_sha256",
    "source_key_descriptor_receipt_sha256", "native_metadata_capture_sha256",
    "native_metadata_capture_receipt_sha256", "compiler_seed", "compiler_setup_hash", "opening_prefabs",
    "entities", "unsupported_values", "unsupported_values_preserved_verbatim", "diagnostic_status",
    "replay_ready", "fresh_world_allocator_exported", "full_mission_initialized", "utility_world_created",
    "utility_world_disposed", "mission_world_created", "native_visual_readiness", "source_fit_certified",
    "native_context_qualified", "mission_ready", "geometry_exported", "consumer_rule",
]
PROOF_KIND = "S003FocusedTemplateCaptureQualification"
IMPORTER_STATUS = "Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly"
IMPORTER_MARKER = ("[S003FocusedTemplateSourceHostControls] result=Passed roots=19 entities=647 components=3965 "
                   "unsupportedLeaves=32 omittedVfxComponents=26 cleanup=Passed preflightRejected=2 world=0 mission=0 readiness=0")
PRESENTATION_COMPONENTS = {
    "Game.Components.UnitAttackImpactVfxReference", "Game.Components.UnitMuzzleFlashVfxReference",
    "Game.Components.GroundMissileLauncherVfxReferenceComponent", "Game.Components.AirMissileLauncherVfxReferenceComponent",
}
ALLOWED_VFX_FIELDS = {
    "Game.Components.UnitAttackImpactVfxReference": {"Prefab"},
    "Game.Components.UnitMuzzleFlashVfxReference": {"Prefab"},
    "Game.Components.AirMissileLauncherVfxReferenceComponent": {
        "AirTargetImpactPrefab", "AirburstExplosionPrefab", "InterceptExplosionPrefab", "LaunchFlashPrefab",
        "LaunchSmokePrefab", "MissileTrailPrefab", "MissileVisualPrefab",
    },
    "Game.Components.GroundMissileLauncherVfxReferenceComponent": {
        "ExplosionPrefab", "LaunchFlashPrefab", "LaunchSmokePrefab", "MissileTrailPrefab", "MissileVisualPrefab",
    },
}
UNITY_GAMEOBJECT_REF = ("Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, UnityEngine.CoreModule, "
                        "Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def unique_pairs(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


def load_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique_pairs)


def encode(value):
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def require(ok, message):
    if not ok:
        raise ValueError(message)


def validate_unsupported(rows):
    require(isinstance(rows, list) and len(rows) == 32, "Exact 32 raw unsupported values required")
    seen = set()
    for row in rows:
        require(isinstance(row, dict), "Unsupported value rows must remain objects")
        parts = row.get("path", "").split("/")
        require(len(parts) == 3 and re.fullmatch(r"(?:0|[1-9][0-9]*):(?:0|[1-9][0-9]*)", parts[0]) and
                parts[1] in ALLOWED_VFX_FIELDS and parts[2] in ALLOWED_VFX_FIELDS[parts[1]] and
                row.get("type") == UNITY_GAMEOBJECT_REF and
                row.get("reason") == "unity_object_ref_requires_bound_source_identity" and
                row["path"] not in seen,
                "Unsupported entry is outside exact unresolved VFX reference scope")
        seen.add(row["path"])


def descriptor(path):
    path = Path(path).resolve(strict=True)
    raw = path.read_bytes()
    return {"path": str(path), "sha256": sha(raw)}


def proof_object(graph_path, strict_path, importer_receipt_path):
    return {"schema_version": 1, "kind": PROOF_KIND,
            "source_graph": descriptor(graph_path),
            "strict_capture_receipt": descriptor(strict_path),
            "importer_control_receipt": descriptor(importer_receipt_path)}


def _validate_importer_control(repo, receipt_path, evidence):
    receipt_path = Path(receipt_path).resolve(strict=True)
    require(receipt_path.is_relative_to(repo), "Importer control receipt must be inside the repository")
    receipt = load_json(receipt_path)
    raw = evidence["raw_path"]
    strict_path = evidence["receipt_path"]
    require(receipt.get("kind") == "S003FocusedTemplateSourceHostControlsReceipt" and
            receipt.get("status") == IMPORTER_STATUS, "Passed actual shared importer receipt required")
    require(receipt.get("capture") == {"path": str(raw), "sha256": evidence["raw_sha"],
            "strict_receipt_path": str(strict_path), "strict_receipt_sha256": evidence["strict_receipt_sha"]},
            "Importer receipt must bind exact current capture and strict receipt")
    require(receipt.get("key_descriptor", {}).get("keys") == evidence["keys"] and
            receipt["key_descriptor"].get("sha256") == evidence["key_sha"] and
            receipt["key_descriptor"].get("receipt_sha256") == evidence["key_receipt_sha"],
            "Importer receipt ordered key descriptor differs from capture")
    build = receipt.get("source_build", {})
    require(build.get("path") == str(evidence["build_path"]) and build.get("sha256") == evidence["build_sha"] and
            build.get("pin_count") == evidence["build_pin_count"] and
            build.get("importer_path") == str(evidence["importer_path"]) and
            build.get("importer_sha256") == evidence["importer_sha"] and
            build.get("source_response_sha256") == evidence["build"]["compiler_response"]["sha256"] and
            build.get("executable_sha256") == evidence["build"]["executable"]["sha256"],
            "Importer receipt does not bind the explicit current source build")
    imported = receipt.get("import_result", {})
    require(imported.get("kind") == "S003FocusedTemplateSourceHostImportControl" and
            imported.get("status") == IMPORTER_STATUS and
            (imported.get("roots_imported"), imported.get("entities_imported"), imported.get("components_imported"),
             imported.get("unsupported_leaf_references_in_source"), imported.get("omitted_presentation_components")) ==
            (19, 647, 3965, 32, 26), "Importer control counts changed")
    for key in ("preflight_no_identity_allocation", "unsupported_non_vfx_rejected_before_allocation",
                "foreign_required_key_rejected_before_allocation", "inconsistent_disabled_state_rejected_before_allocation",
                "partial_identity_allocation_rollback", "successful_import_cleanup"):
        require(imported.get(key) is True, "Importer negative/lifecycle control missing: " + key)
    preflight = imported.get("preflight", {})
    require((preflight.get("roots"), preflight.get("entities"), preflight.get("decoded_components"),
             preflight.get("unsupported_unconsumed_values")) == (19, 647, 3965, 32),
            "Importer preflight counts changed")
    omitted = imported.get("excluded_presentation_component_types")
    require(isinstance(omitted, list) and len(omitted) == 4 and set(omitted) == PRESENTATION_COMPONENTS,
            "Importer may omit only the four named presentation VFX components")
    for key in ("unity_world_created", "mission_manager_created", "mission_initialized", "source_authority_rewritten",
                "runtime_replay_ready", "native_visual_readiness", "source_fit_certified", "native_context_qualified", "mission_ready"):
        require(imported.get(key) is False, "Importer result must disclose false mission/readiness state: " + key)
    for key in ("unity_world_created", "mission_manager_created", "source_authority_rewritten", "runtime_replay_ready",
                "native_visual_readiness", "source_fit_certified", "native_context_qualified", "mission_ready"):
        require(receipt.get(key) is False, "Importer receipt must keep mission/readiness false: " + key)
    control = Path(receipt["control_executable"]["path"]).resolve(strict=True)
    control_dir = control.parent
    require(control.is_relative_to(repo), "Importer control executable escapes repository")
    require(sha(control.read_bytes()) == receipt["control_executable"].get("sha256"),
            "Importer control executable hash mismatch")
    # Bind outputs using the hashes emitted by the source-host runner and verify the exact pass marker.
    for filename, key in (("compile.rsp", "control_compile_response_sha256"),
                          ("compiler.log", "control_compile_log_sha256"), ("run.log", "control_run_log_sha256")):
        data = (control_dir / filename).read_bytes()
        require(sha(data) == receipt.get(key), "Importer control artifact hash mismatch: " + filename)
    run_log = (control_dir / "run.log").read_text(encoding="utf-8")
    require(run_log.splitlines().count(IMPORTER_MARKER) == 1, "Importer control exact pass marker missing or duplicated")
    control_json = load_json(control_dir / "control.json")
    require(control_json == imported, "Importer control output differs from its strict receipt")
    for key in ("unity_world_created", "mission_manager_created", "mission_initialized", "source_authority_rewritten",
                "runtime_replay_ready", "native_visual_readiness", "source_fit_certified", "native_context_qualified", "mission_ready"):
        if key in control_json:
            require(control_json[key] is False, "Control JSON readiness flag must remain false: " + key)
    return receipt, receipt_path


def validate_inputs(repo, importer_receipt_path, source_build_dir):
    repo = Path(repo).resolve(strict=True)
    runner_spec = importlib.util.spec_from_file_location("s003_focused_prefab_host_runner", repo / IMPORTER_RUNNER_REL)
    runner = importlib.util.module_from_spec(runner_spec)
    runner_spec.loader.exec_module(runner)
    evidence = runner.validate_evidence(repo, source_build_dir)
    # The shared runner performs the full raw source pin closure, exact wrapper/finalizer,
    # key descriptor and current source-build pin verification before any output is created.
    _validate_importer_control(repo, importer_receipt_path, evidence)
    graph_path = (repo / GRAPH_REL).resolve(strict=True)
    require(sha(graph_path.read_bytes()) == GRAPH_SHA, "Exact current S003 graph-r5 source pin required")
    graph = load_json(graph_path)
    require(graph.get("schema_version") == 1 and isinstance(graph.get("source_pins"), dict) and
            isinstance(graph.get("nodes"), list), "Current S003 graph-r5 closure malformed")
    return repo, evidence, graph_path


def project_payload(capture, proof, capture_sha):
    require(isinstance(capture, dict) and isinstance(proof, dict), "Capture and proof objects required")
    require(capture.get("mission_id") == "S003" and capture.get("diagnostic_status") == "CollectedSourceOnly",
            "Current S003 diagnostic capture required")
    require(len(capture.get("opening_prefabs", {})) == 19 and len(capture.get("entities", [])) == 647 and
            len(capture.get("unsupported_values", [])) == 32, "Exact captured 19-root/647-entity/32-unsupported domain required")
    for field in ("replay_ready", "fresh_world_allocator_exported", "full_mission_initialized", "mission_world_created",
                  "native_visual_readiness", "source_fit_certified", "native_context_qualified", "mission_ready", "geometry_exported"):
        require(capture.get(field) is False, "Capture must preserve false source-only scope: " + field)
    require(capture.get("utility_world_created") is True and capture.get("utility_world_disposed") is True,
            "Capture must disclose disposed utility World")
    require(capture.get("unsupported_values_preserved_verbatim") is True, "Exact unsupported values must be preserved verbatim")
    validate_unsupported(capture["unsupported_values"])
    require(set(proof) == {"schema_version", "kind", "source_graph", "strict_capture_receipt", "importer_control_receipt"} and
            proof.get("schema_version") == 1 and proof.get("kind") == PROOF_KIND,
            "Exact current S003 capture proof schema required")
    domain = {field: capture[field] for field in FIELDS}
    require(re.fullmatch(r"[0-9a-f]{64}", capture_sha or "") is not None, "Canonical raw capture SHA256 required")
    domain["source_reference_sha256"] = capture_sha
    domain["derived_read_domain"] = "ExactTypedPrefabClosureOnly"
    return domain


def produce(repo, importer_receipt_path, source_build_dir, output_path):
    repo, evidence, graph_path = validate_inputs(repo, importer_receipt_path, source_build_dir)
    capture_path = evidence["raw_path"]
    capture_raw = capture_path.read_bytes()
    capture = json.loads(capture_raw.decode("utf-8"), object_pairs_hook=unique_pairs)
    strict_path = evidence["receipt_path"]
    importer = load_json(importer_receipt_path)
    proof = {"schema_version": 1, "kind": PROOF_KIND,
             "source_graph": descriptor(graph_path), "strict_capture_receipt": descriptor(strict_path),
             "importer_control_receipt": descriptor(importer_receipt_path)}
    domain = project_payload(capture, proof, evidence["raw_sha"])
    output_path = Path(output_path).absolute()
    require(output_path.is_relative_to(repo) and not output_path.exists(), "Fresh isolated output directory inside repository required")
    # Recheck all evidence before output allocation; validate_inputs checks every current raw/source pin.
    validate_inputs(repo, importer_receipt_path, source_build_dir)
    output_path.mkdir(parents=True, exist_ok=False)
    slice_path = output_path / "s003-focused-template-domain.json"
    slice_raw = encode(domain)
    with slice_path.open("xb") as stream:
        stream.write(slice_raw)
    manifest = {
        "schema_version": 1, "kind": "PinnedExactTemplateDomainProjection",
        "source_path": str(capture_path), "source_sha256": evidence["raw_sha"], "source_bytes": len(capture_raw),
        "slice_path": str(slice_path), "slice_sha256": sha(slice_raw), "slice_bytes": len(slice_raw),
        "selected_fields": FIELDS, "root_count": 19, "entity_count": 647,
        "unsupported_values_preserved": 32, "unsupported_values_preserved_verbatim": True,
        "current_capture_proof": proof, "consumer_read_domain": "ExactTypedPrefabClosureOnly", "world_created": False,
        "utility_world_created": True, "utility_world_disposed": True, "mission_world_created": False,
        "runtime_replay_ready": False, "native_visual_readiness": False, "source_fit_certified": False,
        "native_context_qualified": False, "mission_ready": False,
        "scope": "Exact S003 original 19-root typed prefab closure; unsupported VFX values remain raw and unresolved; source-host import control only; no gameplay World or readiness authority.",
    }
    manifest_path = output_path / "template-slice-manifest.json"
    manifest_raw = encode(manifest)
    with manifest_path.open("xb") as stream:
        stream.write(manifest_raw)
    descriptor_body = {
        "path": str(slice_path), "sha256": sha(slice_raw),
        "source_reference_path": str(capture_path), "source_reference_sha256": evidence["raw_sha"],
        "projection_manifest_path": str(manifest_path), "projection_manifest_sha256": sha(manifest_raw),
        "current_capture_proof": proof,
        "consumer_qualification_scope": "S003 exact typed source-template closure and shared importer source-host controls; no replay, visual, source-fit, context, or mission readiness",
    }
    descriptor_path = output_path / "reference-template-descriptor.json"
    descriptor_raw = encode(descriptor_body)
    with descriptor_path.open("xb") as stream:
        stream.write(descriptor_raw)
    # Keep evidence stable across serialization and fail closed if any pinned input changed during projection.
    validate_inputs(repo, importer_receipt_path, source_build_dir)
    result = {"schema_version": 1, "kind": "S003FocusedPrefabDomainProjectionReceipt", "status": "PassedSourceOnly",
              "capture_sha256": evidence["raw_sha"], "strict_capture_receipt_sha256": evidence["strict_receipt_sha"],
              "graph_sha256": GRAPH_SHA, "importer_control_receipt_sha256": sha(Path(importer_receipt_path).read_bytes()),
              "slice_sha256": sha(slice_raw), "manifest_sha256": sha(manifest_raw),
              "descriptor_path": str(descriptor_path), "descriptor_sha256": sha(descriptor_raw),
              "root_count": 19, "entity_count": 647, "unsupported_count": 32,
              "unsupported_values_preserved_verbatim": True, "utility_world_created": True,
              "utility_world_disposed": True, "mission_world_created": False,
              "runtime_replay_ready": False, "native_visual_readiness": False, "source_fit_certified": False,
              "native_context_qualified": False, "mission_ready": False, "geometry_exported": False}
    with (output_path / "projection-receipt.json").open("xb") as stream:
        stream.write(encode(result))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--importer-control-receipt", type=Path, required=True)
    parser.add_argument("--source-build-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(produce(args.repo, args.importer_control_receipt, args.source_build_dir, args.output), indent=2))


if __name__ == "__main__":
    main()
