#!/usr/bin/env python3
"""Rebind the retained S003 input shape to strictly validated current source-only evidence.

The output is an offline data-bundle/initialization pair. It does not launch a
mission, allocate a game World, or grant native, fit, or readiness authority.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[4]
OLD_DIR = Path("Logs/private-candidates/s003-active-runway-capture-20261028-r1/s003-profile-reader-evidence-r1/s003-incomplete-bundle-r6")
RELOCATION = Path("Logs/private-candidates/s003-preflight-r6-overlay/relocation-r1/mapped-tmp")
T17_PROJECTION = Path("T Logs/SkirmishProduction/s003-current-final-gate-projection-20261103-T17")
T18_DESCRIPTOR = Path("T Logs/SkirmishProduction/s003-current-source-qualification-descriptor-20261103-T18")
PREFAB_DESCRIPTOR = Path("T Logs/SkirmishProduction/s003-focused-prefab-domain-20261103-T1/data/reference-template-descriptor.json")
CLEARANCE_RELOCATION_RECEIPT = Path("T Logs/SkirmishProduction/s003-clearance-verified-relocation-20261103-T1/receipt.json")
S003_CLEARANCE_SOURCE_SHA = "5a0993af26be09c8605a883e842ff802c67ae56477b57e9513c2b11b3827b010"
S003_CLEARANCE_SOURCE_BYTES = 86684375
S003_CLEARANCE_SOURCE_SCENES = [
    "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity",
    "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity",
    "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime.unity",
]
RETAINED_RUNTIME_INIT = Path("Logs/private-candidates/s003-preflight-r6-overlay/relocation-r1/mapped-tmp/s003-camera-bundle-r15/initialization.json")
RETAINED_RUNTIME_BUNDLE = Path("Logs/private-candidates/s003-preflight-r6-overlay/relocation-r1/mapped-tmp/s003-camera-bundle-r16/data-bundle.json")

S003_RUNTIME_INITIAL_FIELDS = {
    "source_config_graph", "setup_matrix", "canonical_battle_catalog", "selected_operation_map",
    "building_definitions", "building_placement_config", "reference_templates", "navigation_reference",
    "resource_preset", "unit_production_metadata", "building_request_metadata", "metadata_compatibility",
    "engineering_camera_profile", "operation_map_rotation_reference", "death_mechanical_inputs",
    "command_text_data", "static_map", "placement_domain", "metadata_compiler_context",
    "configuration_qualification",
}


def sha_file(path: Path) -> str:
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def read_json(path: Path, limit: int = 64 * 1024 * 1024) -> dict:
    path = Path(path)
    if path.is_symlink() or not path.is_file() or path.stat().st_size < 1 or path.stat().st_size > limit:
        raise ValueError("Bounded regular JSON proof required: " + str(path))
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=no_duplicates,
                      parse_constant=lambda v: (_ for _ in ()).throw(ValueError("Invalid JSON number: " + v)))


def regular_path(raw: str, root: Path = ROOT) -> Path:
    path = Path(raw)
    if not path.is_absolute():
        path = root / path
    lexical = Path(os.path.abspath(path))
    try:
        relative = lexical.relative_to(root)
    except ValueError:
        raise ValueError("Evidence path escapes repository: " + raw)
    cursor = root
    for part in relative.parts:
        cursor = cursor / part
        if cursor.is_symlink():
            raise ValueError("Symlink in evidence path: " + str(cursor))
    resolved = lexical.resolve(strict=True)
    if not resolved.is_relative_to(root) or not resolved.is_file():
        raise ValueError("Regular repository evidence file required: " + raw)
    return resolved


def descriptor(value: dict, label: str) -> tuple[Path, str]:
    if type(value) is not dict or set(value) != {"path", "sha256"} or type(value["path"]) is not str or not re.fullmatch(r"[0-9a-f]{64}", str(value["sha256"])):
        raise ValueError("Exact path/hash descriptor required: " + label)
    path = regular_path(value["path"])
    if sha_file(path) != value["sha256"]:
        raise ValueError("Evidence descriptor hash mismatch: " + label)
    return path, value["sha256"]


def build_file_descriptor(value: dict, label: str) -> tuple[Path, str]:
    if type(value) is not dict or set(value) not in ({"path", "sha256"}, {"path", "sha256", "bytes"}):
        raise ValueError("Exact source-build file descriptor required: " + label)
    path, digest = descriptor({"path": value["path"], "sha256": value["sha256"]}, label)
    if "bytes" in value and (type(value["bytes"]) is not int or value["bytes"] != path.stat().st_size):
        raise ValueError("Source-build file size descriptor mismatch: " + label)
    return path, digest


def file_descriptor(path: Path) -> dict:
    path = regular_path(str(path))
    return {"path": str(path), "sha256": sha_file(path)}


def external_regular_path(raw_path: str, allowed_roots: list[Path]) -> Path:
    path = Path(raw_path)
    if not path.is_absolute():
        raise ValueError("Absolute external source pin required: " + raw_path)
    if ".." in path.parts:
        raise ValueError("Parent traversal in external source pin: " + raw_path)
    lexical = Path(os.path.abspath(path))
    for allowed in allowed_roots:
        allowed = allowed.resolve(strict=True)
        try:
            relative = lexical.relative_to(allowed)
        except ValueError:
            continue
        cursor = allowed
        for part in relative.parts:
            cursor = cursor / part
            if cursor.is_symlink():
                raise ValueError("Symlink in external build pin: " + raw_path)
        resolved = lexical.resolve(strict=True)
        if resolved.is_file() and resolved.is_relative_to(allowed):
            return resolved
    raise ValueError("External pin is outside declared source-build runtime roots: " + raw_path)


def audit_retained_path(value: dict, label: str, root: Path = ROOT) -> tuple[Path, str]:
    if type(value) is not dict or type(value.get("path")) is not str or not re.fullmatch(r"[0-9a-f]{64}", str(value.get("sha256"))):
        raise ValueError("Exact retained input path/hash required: " + label)
    raw = value["path"]
    if raw.startswith("/private/tmp/"):
        candidate = root / RELOCATION / raw.removeprefix("/private/tmp/")
    else:
        candidate = Path(raw)
        if not candidate.is_absolute():
            candidate = root / candidate
    path = regular_path(str(candidate), root)
    if sha_file(path) != value["sha256"]:
        raise ValueError("Retained input hash mismatch: " + label)
    return path, value["sha256"]


def verify_pin_map(pins: dict, label: str, allowed_roots: list[Path] | None = None, repo_root: Path = ROOT) -> int:
    if type(pins) is not dict or not pins:
        raise ValueError("Non-empty source pin map required: " + label)
    repo_root = Path(repo_root).resolve(strict=True)
    normalized: dict[Path, str] = {}
    for raw_path, expected in pins.items():
        if type(raw_path) is not str or not re.fullmatch(r"[0-9a-f]{64}", str(expected)):
            raise ValueError("Malformed source pin in " + label)
        candidate = Path(raw_path)
        if ".." in candidate.parts:
            raise ValueError("Parent traversal in source pin: " + raw_path)
        normalized_path = Path(os.path.abspath(candidate if candidate.is_absolute() else repo_root / candidate))
        prior = normalized.get(normalized_path)
        if prior is not None and prior != expected:
            raise ValueError("Duplicate normalized source pin conflict in " + label + ": " + raw_path)
        normalized[normalized_path] = expected
        if not candidate.is_absolute() or normalized_path.is_relative_to(repo_root):
            path = regular_path(raw_path, repo_root)
        elif allowed_roots:
            path = external_regular_path(raw_path, allowed_roots)
        else:
            raise ValueError("Source pin outside repository root: " + raw_path)
        if sha_file(path) != expected:
            raise ValueError("Source pin mismatch in " + label + ": " + raw_path)
    return len(pins)


def resolve_s003_clearance_source_artifact(root: Path, navigation_reference: dict,
                                           projection_descriptor: dict,
                                           receipt_path: Path | None = None) -> dict:
    """Resolve the exact durable mapped source while preserving the projection's legacy path."""
    receipt_path = receipt_path or (root / CLEARANCE_RELOCATION_RECEIPT)
    receipt_path = regular_path(str(receipt_path), root)
    receipt = read_json(receipt_path, 64 * 1024)
    expected_sha = S003_CLEARANCE_SOURCE_SHA
    expected_bytes = S003_CLEARANCE_SOURCE_BYTES
    if (receipt.get("kind") != "S003VerifiedTemporaryClearanceRelocation" or
            receipt.get("source_and_copy_verified") is not True or receipt.get("source_fit_certified") is not False or
            receipt.get("mission_ready") is not False or receipt.get("sha256") != expected_sha or
            receipt.get("bytes") != expected_bytes):
        raise ValueError("Exact non-admissive S003 clearance relocation receipt required")
    source_path = regular_path(receipt.get("source_path", ""), root)
    if (not source_path.is_relative_to(root / "Logs") or
            navigation_reference.get("sha256") != expected_sha):
        raise ValueError("Durable mapped source path or retained navigation identity mismatch")

    if (type(projection_descriptor) is not dict or set(projection_descriptor) != {"path", "sha256"} or
            type(projection_descriptor.get("path")) is not str or
            not re.fullmatch(r"[0-9a-f]{64}", str(projection_descriptor.get("sha256")))):
        raise ValueError("Exact S003 static-clearance projection descriptor required")
    projection_path = regular_path(projection_descriptor["path"], root)
    if sha_file(projection_path) != projection_descriptor["sha256"]:
        raise ValueError("S003 static-clearance projection descriptor hash mismatch")
    projection = read_json(projection_path, 8 * 1024 * 1024)
    record = projection.get("source_record")
    if (projection.get("schema_version") != 1 or projection.get("kind") != "GeneratedSourceStaticClearanceProjection" or
            projection.get("source_sha256") != expected_sha or
            not isinstance(record, dict) or
            record.get("authority") != "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain" or
            record.get("projection_source") != "Unity.Rendering.RenderBoundsUpdateSystem.BoundsJob:AABB.Transform(LocalToWorld,RenderBounds)" or
            record.get("grid_width") != 2048 or record.get("grid_height") != 1024 or
            record.get("grid_origin") != [0.0, 0.0, 0.0] or record.get("cell_size") != 1.0 or
            record.get("query_rect") != [-118.0, -118.0, 2284.0, 1260.0] or
            record.get("minimum_extent") != 0.25 or record.get("maximum_extent") != 20.0 or
            record.get("source_search_radius_cells") != 96 or record.get("source_padding_cells") != 2 or
            not isinstance(record.get("bounds"), list) or len(record["bounds"]) != 48457):
        raise ValueError("S003 clearance projection source/map frame identity mismatch")

    if sha_file(source_path) != expected_sha or source_path.stat().st_size != expected_bytes:
        raise ValueError("Durable mapped S003 clearance source bytes changed")
    source = read_json(source_path, 96 * 1024 * 1024)
    if source.get("kind") != "AuthoredSkirmishReferenceData" or source.get("source_scenes") != S003_CLEARANCE_SOURCE_SCENES:
        raise ValueError("Durable mapped S003 clearance source scene identity mismatch")
    return {"path": str(source_path), "sha256": expected_sha, "bytes": expected_bytes}


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise ValueError("Required offline validator is unavailable: " + str(path))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def validate_old_bundle(root: Path) -> tuple[dict, dict, dict]:
    old_dir = root / OLD_DIR
    bundle_path = regular_path(str(old_dir / "data-bundle.json"), root)
    init_path = regular_path(str(old_dir / "initialization.json"), root)
    bundle = read_json(bundle_path)
    initial = read_json(init_path)
    if set(bundle) != {"schema_version", "mission", "seed", "runtime_backend", "initialization", "unit_metadata", "scope", "runtime_data_inputs"}:
        raise ValueError("Retained r6 bundle has an unexpected schema")
    if set(initial) != {"mission_id", "selection", "source_config_graph", "setup_matrix", "metadata_compiler_context", "configuration_qualification", "building_definitions", "unit_production_metadata", "building_request_metadata", "canonical_battle_catalog", "selected_operation_map", "building_placement_config", "reference_templates"}:
        raise ValueError("Retained r6 initialization has an unexpected schema")
    if bundle.get("schema_version") != 1 or bundle.get("mission") != "S003" or bundle.get("seed") != 104732 or bundle.get("runtime_backend") != "TypedGroundS026V1":
        raise ValueError("Exact retained S003 r6 bundle identity required")
    if bundle.get("initialization", {}).get("sha256") != sha_file(init_path):
        raise ValueError("Retained bundle does not bind its adjacent initialization bytes")
    if bundle.get("unit_metadata") != initial.get("unit_production_metadata"):
        raise ValueError("Retained bundle/unit metadata descriptors disagree")
    if initial.get("mission_id") != "S003" or initial.get("selection") != {"seed": 104732, "difficulty_id": 2, "size_id": 1}:
        raise ValueError("Retained S003 authored target changed")
    return bundle, initial, {"bundle": file_descriptor(bundle_path), "initialization": file_descriptor(init_path)}


def audit_retained_runtime_inputs(root: Path, initial: dict, bundle: dict) -> dict:
    """Report missing current runtime inputs without promoting stale diagnostics.

    The r15/r16 relocation is an authentic historical source set. Its descriptors
    are rehashed below, but the old navigation, metadata and camera observations
    do not become current compiler/water/camera evidence by being copied forward.
    """
    init_path = regular_path(str(root / RETAINED_RUNTIME_INIT), root)
    bundle_path = regular_path(str(root / RETAINED_RUNTIME_BUNDLE), root)
    retained = read_json(init_path)
    retained_bundle = read_json(bundle_path)
    if (retained.get("mission_id") != "S003" or retained.get("selection") !=
            {"seed": 104732, "difficulty_id": 2, "size_id": 1} or
            retained_bundle.get("initialization") != {"path": "/private/tmp/s003-camera-bundle-r15/initialization.json",
                                                       "sha256": sha_file(init_path)}):
        raise ValueError("Exact retained r15 S003 runtime input initialization required")
    required_missing = sorted(key for key in S003_RUNTIME_INITIAL_FIELDS if key not in initial)
    bundle_missing = sorted(key for key in ("runtime_data_inputs", "command_text_sources") if key not in bundle)
    invalid_current = []
    def resolve_retained(value: dict, label: str) -> tuple[Path, str]:
        if type(value) is not dict or set(value) != {"path", "sha256"}:
            raise ValueError("Exact retained descriptor required: " + label)
        raw = value["path"]
        if type(raw) is not str:
            raise ValueError("Malformed retained path: " + label)
        p = Path(raw)
        if raw.startswith("/private/tmp/"):
            p = root / RELOCATION / raw.removeprefix("/private/tmp/")
        elif p.is_absolute():
            p = Path(os.path.abspath(p))
        else:
            p = root / p
        resolved = p.resolve(strict=True)
        if not resolved.is_file() or sha_file(resolved) != value["sha256"]:
            raise ValueError("Retained descriptor does not rehash: " + label)
        return resolved, value["sha256"]
    # Verify the declared S003 diagnostic inputs by their actual consumer contract.
    # They remain diagnostics; the flags do not qualify water, camera or readiness.
    try:
        nav_path, nav_sha = resolve_retained(initial["navigation_reference"], "navigation_reference")
        nav = read_json(nav_path, 128 * 1024 * 1024)
        if nav_sha != "5a0993af26be09c8605a883e842ff802c67ae56477b57e9513c2b11b3827b010" or nav.get("kind") != "AuthoredSkirmishReferenceData":
            raise ValueError("S003 authored navigation diagnostic identity mismatch")
        camera_path, camera_sha = resolve_retained(initial["engineering_camera_profile"], "engineering_camera_profile")
        camera = read_json(camera_path)
        if (camera_sha != "69f8194ea26be5f39ea77251b59c528040ed7fce5517ac994b0e5d0a7b597071" or
                camera.get("kind") != "S003DefaultCameraSourceDiagnostic" or camera.get("active_airfield_override_checked") is not False or
                camera.get("world_initialized") is not False or camera.get("native_visual_readiness") is not False):
            raise ValueError("S003 default-camera diagnostic identity/scope mismatch")
        placement = initial["placement_domain"]
        if placement.get("kind") != "S003PhysicalSnapshotDiagnostic" or placement.get("active_mission_water_qualified") is not False:
            raise ValueError("S003 physical snapshot must remain an unqualified diagnostic")
        for field, kind in (("packed", None), ("geometry_reference", None), ("water_reference", None), ("original_reference", "AuthoredSkirmishReferenceData")):
            path, _ = resolve_retained(placement[field], "placement_domain." + field)
            if kind is not None and read_json(path, 128 * 1024 * 1024).get("kind") != kind:
                raise ValueError("S003 original placement reference identity mismatch")
    except (OSError, ValueError, KeyError, TypeError) as error:
        invalid_current.append({"field": "S003 source-only diagnostic inputs", "reason": str(error)})
    data = bundle.get("runtime_data_inputs")
    if (type(data) is not dict or set(data) != {"schema_version", "kind", "data_root", "data_files", "source_files"} or
            data.get("kind") != "CurrentRoadSceneRuntimeInputs"):
        bundle_missing.append("exact CurrentRoadSceneRuntimeInputs contract")
    if "command_text_sources" in bundle:
        source = bundle["command_text_sources"]
        if type(source) is not dict or set(source) != {"captured_root", "current_root", "input_pins", "artifact_sha256"}:
            bundle_missing.append("valid command_text_sources contract")
        else:
            try:
                text_path, text_sha = audit_retained_path(initial["command_text_data"], "command_text_data", root)
                text = read_json(text_path)
                captured = Path(source["captured_root"])
                current = Path(source["current_root"])
                expected = {str(current / Path(name).relative_to(captured)): digest
                            for name, digest in text["source_pins"].items()}
                if (source["artifact_sha256"] != text_sha or not captured.is_absolute() or
                        current.resolve(strict=True) != root.resolve(strict=True) or source["input_pins"] != expected):
                    raise ValueError("Command text source manifest does not match the authentic artifact")
                verify_pin_map(expected, "retained R16 current command text closure", repo_root=root)
            except (OSError, ValueError, KeyError, TypeError) as error:
                bundle_missing.append("verified command_text_sources closure: " + str(error))
    return {
        "retained_r15_initialization": file_descriptor(init_path),
        "retained_r16_bundle": file_descriptor(bundle_path),
        "missing_initialization_fields": required_missing,
        "missing_or_invalid_bundle_contract": sorted(set(bundle_missing)),
        "invalid_s003_diagnostic_inputs": invalid_current,
        "current_runtime_input_contract_satisfied": not (required_missing or bundle_missing or invalid_current),
        "scope": "Historical relocated evidence is hash-checked but never silently promoted to current compiler, water or camera evidence",
    }


def validate_relocation(root: Path, initial: dict, current_prefab: dict) -> dict:
    relocation = root / RELOCATION
    slice_path = regular_path(str(relocation / "s003-unit-template-projection-r1/slice.json"), root)
    manifest_path = regular_path(str(relocation / "s003-unit-template-projection-r1/manifest.json"), root)
    raw_path = regular_path(str(relocation / "s003-all-unit-prefabs-r1/prefab-reference-data.json"), root)
    old_ref = initial["reference_templates"]
    slice_data = read_json(slice_path)
    manifest = read_json(manifest_path)
    raw = read_json(raw_path)
    if sha_file(slice_path) != old_ref.get("sha256") or sha_file(raw_path) != old_ref.get("source_reference_sha256") or sha_file(manifest_path) != old_ref.get("projection_manifest_sha256"):
        raise ValueError("Relocated r6 template files do not match original descriptor hashes")
    if (manifest.get("kind") != "PinnedExactTemplateDomainProjection" or manifest.get("slice_sha256") != sha_file(slice_path) or
            manifest.get("source_sha256") != sha_file(raw_path) or manifest.get("root_count") != 19 or manifest.get("entity_count") != 647 or
            manifest.get("full_Unity_replay_ready") is not False or manifest.get("world_created") is not False or
            manifest.get("unsupported_values_preserved") != 32):
        raise ValueError("Relocated r6 template projection manifest identity/scope mismatch")
    current_keys = current_prefab.get("requested_prefab_keys")
    if (slice_data.get("kind") != "AuthoredSkirmishPrefabReferenceData" or
            slice_data.get("requested_prefab_keys") != current_keys or len(current_keys or []) != 19 or
            len(slice_data.get("entities", [])) != 647 or len(raw.get("opening_prefabs", {})) != 19):
        raise ValueError("Retained r6 and current prefab capture do not share the exact 19-key/647-entity closure")
    return {"slice": file_descriptor(slice_path), "manifest": file_descriptor(manifest_path),
            "raw_capture": file_descriptor(raw_path), "root_count": 19, "entity_count": 647,
            "unsupported_values": 32, "matches_current_requested_keys": True}


def validate_current_metadata(root: Path, projection_dir: Path, qualification_dir: Path) -> tuple[dict, dict, dict, dict]:
    builder_path = regular_path(str(root / "Tools/CI/SkirmishSourceSimulation/Recipes/build_s003_current_source_qualification_descriptor.py"), root)
    builder = load_module(builder_path, "s003_bundle_qualification_builder")
    evidence = builder.validate_evidence(root, projection_dir)
    expected_context, expected_qualification = builder.construct_descriptors(evidence)
    context_path = regular_path(str(qualification_dir / "s003-current-source-only-context.json"), root)
    qualification_path = regular_path(str(qualification_dir / "s003-current-source-qualification-descriptor.json"), root)
    build_receipt_path = regular_path(str(qualification_dir / "build-receipt.json"), root)
    context = read_json(context_path)
    qualification = read_json(qualification_path)
    build_receipt = read_json(build_receipt_path)
    expected_qualification["compiler_context"] = {"path": str(context_path), "sha256": sha_file(context_path)}
    if context != expected_context or qualification != expected_qualification:
        raise ValueError("Current S003 T18 descriptor artifacts do not equal freshly validated T17 evidence")
    if (build_receipt.get("kind") != "S003CurrentSourceQualificationDescriptorBuildReceipt" or
            build_receipt.get("status") != "PassedDescriptorOnly" or
            build_receipt.get("context") != {"path": str(context_path), "sha256": sha_file(context_path)} or
            build_receipt.get("qualification_descriptor") != {"path": str(qualification_path), "sha256": sha_file(qualification_path)} or
            build_receipt.get("source_graph_sha256") != "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb" or
            build_receipt.get("runtime_admission") is not False or build_receipt.get("native_context_qualified") is not False or
            build_receipt.get("source_fit_certified") is not False or build_receipt.get("mission_ready") is not False):
        raise ValueError("Exact passed source-only S003 T18 descriptor receipt required")
    if (context.get("target_context", {}).get("setup_hash") != 2394754492 or
            context.get("source_graph", {}).get("sha256") != "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb" or
            context.get("compiler_graph", {}).get("sha256") != "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f"):
        raise ValueError("T18 qualification context does not bind the distinct current compiler/capture graphs")
    return context, qualification, evidence["projection"], {
        "context": file_descriptor(context_path), "qualification": file_descriptor(qualification_path),
        "build_receipt": file_descriptor(build_receipt_path),
    }


def validate_prefab(root: Path, descriptor_path: Path) -> tuple[dict, dict]:
    descriptor_path = regular_path(str(descriptor_path), root)
    descriptor_sha = sha_file(descriptor_path)
    reference_descriptor = read_json(descriptor_path, 2 * 1024 * 1024)
    if set(reference_descriptor) != {"path", "sha256", "source_reference_path", "source_reference_sha256", "projection_manifest_path", "projection_manifest_sha256", "current_capture_proof", "consumer_qualification_scope"}:
        raise ValueError("Exact current S003 template descriptor required")
    domain_path, domain_sha = descriptor({"path": reference_descriptor["path"], "sha256": reference_descriptor["sha256"]}, "template domain")
    source_path, source_sha = descriptor({"path": reference_descriptor["source_reference_path"], "sha256": reference_descriptor["source_reference_sha256"]}, "raw prefab capture")
    manifest_path, manifest_sha = descriptor({"path": reference_descriptor["projection_manifest_path"], "sha256": reference_descriptor["projection_manifest_sha256"]}, "template projection manifest")
    domain = read_json(domain_path)
    manifest = read_json(manifest_path, 4 * 1024 * 1024)
    proof = reference_descriptor["current_capture_proof"]
    if type(proof) is not dict or set(proof) != {"schema_version", "kind", "source_graph", "strict_capture_receipt", "importer_control_receipt"} or proof.get("kind") != "S003FocusedTemplateCaptureQualification":
        raise ValueError("Exact current S003 prefab source proof required")
    graph_path, graph_sha = descriptor(proof["source_graph"], "prefab source graph")
    strict_path, strict_sha = descriptor(proof["strict_capture_receipt"], "strict prefab capture receipt")
    importer_path, importer_sha = descriptor(proof["importer_control_receipt"], "shared importer control receipt")
    graph = read_json(graph_path, 16 * 1024 * 1024)
    strict = read_json(strict_path, 4 * 1024 * 1024)
    importer = read_json(importer_path, 4 * 1024 * 1024)
    if (graph_sha != "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb" or
            strict.get("kind") != "S003FocusedPrefabCaptureReceipt" or strict.get("validation_status") != "PassedDiagnosticSourceOnly" or
            strict.get("capture_path") != str(source_path) or strict.get("capture_sha256") != source_sha or
            strict.get("graph_sha256") != graph_sha or strict.get("seed") != 104732 or strict.get("setup_hash") != 2394754492 or
            strict.get("root_count") != 19 or strict.get("entity_count") != 647 or strict.get("unsupported_count") != 32 or
            strict.get("unclassified_unsupported_count") != 0 or strict.get("unsupported_values_preserved_verbatim") is not True or
            strict.get("utility_world_created") is not True or strict.get("utility_world_disposed") is not True or
            strict.get("mission_world_created") is not False or strict.get("mission_ready") is not False):
        raise ValueError("Current strict S003 focused prefab capture does not match its source-only target")
    if (domain.get("kind") != "AuthoredSkirmishPrefabReferenceData" or domain.get("mission_id") != "S003" or
            domain.get("compiler_seed") != 104732 or domain.get("compiler_setup_hash") != 2394754492 or
            domain.get("diagnostic_status") != "CollectedSourceOnly" or len(domain.get("requested_prefab_keys", [])) != 19 or
            len(domain.get("entities", [])) != 647 or len(domain.get("unsupported_values", [])) != 32 or
            domain.get("full_mission_initialized") is not False or domain.get("native_visual_readiness") is not False or
            domain.get("geometry_exported") is not False or domain.get("mission_ready") is not False):
        raise ValueError("Current prefab projection has an unexpected identity, count, or scope")
    if (manifest.get("kind") != "PinnedExactTemplateDomainProjection" or manifest.get("source_path") != str(source_path) or
            manifest.get("source_sha256") != source_sha or manifest.get("slice_path") != str(domain_path) or
            manifest.get("slice_sha256") != domain_sha or manifest.get("root_count") != 19 or manifest.get("entity_count") != 647 or
            manifest.get("consumer_read_domain") != "ExactTypedPrefabClosureOnly" or manifest.get("world_created") is not False or
            manifest.get("current_capture_proof") != proof):
        raise ValueError("Current prefab projection manifest does not bind the strict capture proof")
    capture_pin_count = verify_pin_map(domain.get("source_pins"), "current prefab capture")
    if capture_pin_count != 16720 or capture_pin_count != strict.get("source_pin_count"):
        raise ValueError("Current mixed-path prefab source pin closure must contain exact 16,720 pins")
    if (importer.get("kind") != "S003FocusedTemplateSourceHostControlsReceipt" or
            importer.get("status") != "Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly" or
            importer.get("capture") != {"path": str(source_path), "sha256": source_sha,
                                         "strict_receipt_path": str(strict_path), "strict_receipt_sha256": strict_sha} or
            importer.get("unity_world_created") is not False or importer.get("mission_manager_created") is not False or
            importer.get("runtime_replay_ready") is not False or importer.get("native_visual_readiness") is not False or
            importer.get("source_fit_certified") is not False or importer.get("native_context_qualified") is not False or
            importer.get("mission_ready") is not False):
        raise ValueError("Actual T12 shared importer control does not bind the exact current capture")
    result = importer.get("import_result", {})
    expected_result = {"roots": 19, "entities": 647, "decoded_components": 3965,
                       "omitted_presentation_components": 26, "unsupported_unconsumed_values": 32,
                       "world_allocated": False, "entity_identities_allocated": 0}
    if result.get("preflight") != expected_result or result.get("status") != "Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly" or result.get("successful_import_cleanup") is not True:
        raise ValueError("T12 importer result does not prove the strict expected typed closure")
    build_desc = importer.get("source_build")
    source_build_path, source_build_sha = descriptor({"path": build_desc.get("path"), "sha256": build_desc.get("sha256")}, "T12 importer source build")
    source_build = read_json(source_build_path, 16 * 1024 * 1024)
    if (source_build.get("kind") != "SkirmishSourceSimulationBuild" or source_build.get("status") != "Passed" or
            source_build.get("mission") != "S003" or source_build.get("compile_exit") != 0 or
            source_build.get("native_or_player_launched") is not False or
            source_build.get("source_count") < 700 or source_build.get("source_and_reference_pins") is None):
        raise ValueError("T12 importer source build is not a passed S003 source-only build")
    build_pins = source_build["source_and_reference_pins"]
    runtime = source_build.get("runtime_selection", {})
    allowed_roots = [Path(runtime["contents"]), Path("/usr/local/share/dotnet")]
    if len(build_pins) != build_desc.get("pin_count") or verify_pin_map(build_pins, "T12 S003 importer source build", allowed_roots) != build_desc.get("pin_count"):
        raise ValueError("T12 source build pin closure does not match its receipt")
    importer_source = build_desc.get("importer_path")
    if build_pins.get(importer_source) != build_desc.get("importer_sha256") or sha_file(regular_path(importer_source)) != build_desc.get("importer_sha256"):
        raise ValueError("T12 source build does not pin the exact shared importer")
    for desc, label in ((source_build.get("compiler_response"), "T12 compiler response"),
                        (source_build.get("executable"), "T12 source executable"),
                        (importer.get("control_executable"), "T12 importer control executable")):
        build_file_descriptor(desc, label)
    control_dir = Path(importer["control_executable"]["path"]).parent
    for name, key in (("compile.rsp", "control_compile_response_sha256"), ("compiler.log", "control_compile_log_sha256"), ("run.log", "control_run_log_sha256")):
        path = regular_path(str(control_dir / name))
        if sha_file(path) != importer.get(key):
            raise ValueError("T12 importer control adjacent proof changed: " + name)
    proof_record = {
        "descriptor": file_descriptor(descriptor_path), "domain": file_descriptor(domain_path),
        "source_capture": file_descriptor(source_path), "manifest": file_descriptor(manifest_path),
        "graph": file_descriptor(graph_path), "strict_receipt": file_descriptor(strict_path),
        "importer_control_receipt": file_descriptor(importer_path),
        "source_build": file_descriptor(source_build_path), "source_build_pin_count": len(build_pins),
        "capture_pin_count": len(domain["source_pins"]), "root_count": 19, "entity_count": 647,
        "decoded_components": 3965, "omitted_presentation_components": 26,
        "unsupported_vfx_values_unconsumed": 32,
    }
    return reference_descriptor, proof_record


def rebind(root: Path, old_bundle_path: Path, old_init_path: Path, projection_dir: Path,
           qualification_dir: Path, prefab_descriptor_path: Path, output: Path) -> dict:
    root = root.resolve(strict=True)
    output = Path(os.path.abspath(output))
    if output.exists():
        raise ValueError("Fresh exclusive output directory required")
    if not output.is_relative_to(root / "T Logs"):
        raise ValueError("Fresh output must be inside repository T Logs")
    output.mkdir(parents=True, exist_ok=False)
    try:
        bundle_path = regular_path(str(old_bundle_path), root)
        init_path = regular_path(str(old_init_path), root)
        old_bundle = read_json(bundle_path)
        old_init = read_json(init_path)
        expected_bundle, expected_init, old_input_descriptors = validate_old_bundle(root)
        if bundle_path != root / OLD_DIR / "data-bundle.json" or init_path != root / OLD_DIR / "initialization.json":
            raise ValueError("Only retained original r6 bundle and adjacent initialization are accepted")
        if old_bundle != expected_bundle or old_init != expected_init:
            raise ValueError("Retained original r6 inputs changed during validation")
        context, qualification, projection, qualification_records = validate_current_metadata(root, projection_dir, qualification_dir)
        if projection.get("current_compiler_graph_closure_sha256") != context["compiler_graph"]["sha256"]:
            raise ValueError("Current metadata projection and qualification context use different compiler graph")
        if projection.get("source_graph", {}).get("sha256") != context["source_graph"]["sha256"]:
            raise ValueError("Current metadata projection and qualification context use different capture graph")

        prefab_path = regular_path(str(prefab_descriptor_path), root)
        prefab, prefab_proof = validate_prefab(root, prefab_path)
        current_prefab_domain = read_json(regular_path(prefab["path"], root))
        unit_move_source_module = load_module(
            root / "Tools/CI/SkirmishSourceSimulation/Recipes/unit_move_projection/run_unit_move_authoring_source_host.py",
            "s003_unit_move_authoring_source_input")
        unit_move_source_path, unit_move_source_input = unit_move_source_module.create_source_input(
            root, output, current_prefab_domain)
        try:
            relocation = {"status": "HistoricalMappedTemplateComparisonPassed",
                          **validate_relocation(root, old_init, current_prefab_domain)}
        except (OSError, ValueError, KeyError, TypeError) as error:
            relocation = {"status": "HistoricalMappedTemplateComparisonNotConsumed",
                          "reason": str(error),
                          "scope": "New bundle binds the current strict T1/T12 template proof directly"}
        current_graph_path, current_graph_sha = descriptor(context["compiler_graph"], "current augmented compiler graph")
        graph_path, capture_graph_sha = descriptor(context["source_graph"], "native capture graph")
        if current_graph_sha != "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f" or capture_graph_sha != "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb":
            raise ValueError("Distinct validated runtime compiler and capture graph identities required")
        extension = context.get("source_selection_extension_receipt")
        if (type(extension) is not dict or set(extension) != {"path", "sha256"} or
                Path(extension["path"]).resolve() != root / "T Logs/SkirmishProduction/s003-source-selection-graph-extension-20261103-T1/extension-receipt.json" or
                sha_file(regular_path(extension["path"], root)) != extension["sha256"]):
            raise ValueError("Current S003 qualification context does not bind the strict selector graph extension receipt")
        grid_surface_extension = context.get("source_grid_surface_extension_receipt")
        if (type(grid_surface_extension) is not dict or set(grid_surface_extension) != {"path", "sha256"} or
                Path(grid_surface_extension["path"]).resolve() != root / "T Logs/SkirmishProduction/s003-final-runtime-source-graph-20261103-T1/extension-receipt.json" or
                sha_file(regular_path(grid_surface_extension["path"], root)) != grid_surface_extension["sha256"]):
            raise ValueError("Current S003 qualification context does not bind the strict grid/surface graph extension receipt")

        retained_init_path = regular_path(str(root / RETAINED_RUNTIME_INIT), root)
        retained_bundle_path = regular_path(str(root / RETAINED_RUNTIME_BUNDLE), root)
        retained_initial = read_json(retained_init_path)
        retained_bundle = read_json(retained_bundle_path)
        if (retained_bundle.get("initialization") != {"path": "/private/tmp/s003-camera-bundle-r15/initialization.json",
                                                        "sha256": sha_file(retained_init_path)} or
                retained_bundle.get("mission") != "S003" or retained_bundle.get("seed") != 104732 or
                retained_bundle.get("runtime_backend") != "TypedGroundS026V1"):
            raise ValueError("Exact retained r15/r16 S003 diagnostic bundle identity required")
        initial = json.loads(json.dumps(retained_initial))

        def rebase_retained_descriptor(value: dict, label: str) -> dict:
            path, digest = audit_retained_path(value, label)
            return {"path": str(path), "sha256": digest}

        initial["source_config_graph"] = {"path": str(current_graph_path), "sha256": current_graph_sha}
        initial["metadata_compiler_context"] = qualification_records["context"]
        initial["configuration_qualification"] = qualification_records["qualification"]
        for field, artifact_name in (("building_definitions", "building-definitions.json"),
                                     ("unit_production_metadata", "unit-production-metadata.json"),
                                     ("building_request_metadata", "configured-metadata.json")):
            artifact = projection["artifacts"].get(artifact_name)
            if artifact is None:
                raise ValueError("Current projection missing required consumer artifact: " + artifact_name)
            path, digest = descriptor(artifact, "current metadata " + field)
            initial[field] = {"path": str(path), "sha256": digest}
        initial["canonical_battle_catalog"] = file_descriptor(root / "Assets/Game/Resources/SkirmishBattleCatalog.asset")
        initial["reference_templates"] = prefab
        initial["metadata_compatibility"] = context["native_factory_capture"]
        initial["unit_move_authoring_source"] = {
            "path": str(unit_move_source_path.resolve()), "sha256": sha_file(unit_move_source_path)}

        # Resolve every retained /private/tmp input only through the byte-identical
        # mapped relocation; current project files remain rooted at this checkout.
        for field in ("navigation_reference", "resource_preset", "engineering_camera_profile",
                      "operation_map_rotation_reference", "command_text_data"):
            initial[field] = rebase_retained_descriptor(initial[field], field)
        for field in ("packed", "geometry_reference", "water_reference", "original_reference"):
            initial["placement_domain"][field] = rebase_retained_descriptor(initial["placement_domain"][field], "placement_domain." + field)
        initial["death_mechanical_inputs"]["path"] = str(audit_retained_path(
            {"path": initial["death_mechanical_inputs"]["path"], "sha256": initial["death_mechanical_inputs"]["sha256"]},
            "death_mechanical_inputs")[0])
        if "native_path" in initial["death_mechanical_inputs"]:
            initial["death_mechanical_inputs"]["native_path"] = str(audit_retained_path(
                {"path": initial["death_mechanical_inputs"]["native_path"], "sha256": initial["death_mechanical_inputs"]["native_sha256"]},
                "death_mechanical_inputs.native")[0])
        for field in ("definition", "grid_config", "surface_data"):
            # The mapped S049 project copies are accepted only if byte-identical to
            # the current Desert Base source assets in this repository.
            current = root / ("Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset" if field == "definition" else
                              "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset" if field == "grid_config" else
                              "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset")
            old = initial["static_map"][field]
            old_path = regular_path(old["path"], root)
            if sha_file(old_path) != old["sha256"] or sha_file(current) != old["sha256"]:
                raise ValueError("Retained static map is not byte-identical to current Desert Base source: " + field)
            initial["static_map"][field] = file_descriptor(current)
        initial["selected_operation_map"] = file_descriptor(root / "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset")

        # Retained Desert Base assets are preserved byte-for-byte and rehashed.
        for field in ("setup_matrix", "selected_operation_map", "building_placement_config"):
            path, digest = descriptor(old_init[field], "retained " + field)
            initial[field] = {"path": str(path), "sha256": digest}
        if initial["selected_operation_map"]["path"] != str(root / "Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset"):
            raise ValueError("Retained S003 Desert Base operation-map asset identity changed")
        if initial["building_placement_config"]["path"] != str(root / "Assets/Game/Configs/Skirmish/Construction.asset"):
            raise ValueError("Retained S003 building-placement asset identity changed")
        expected_catalog = regular_path(str(root / "Assets/Game/Resources/SkirmishBattleCatalog.asset"), root)
        if initial["canonical_battle_catalog"]["path"] != str(expected_catalog) or sha_file(expected_catalog) != "e5d37c633b63d2ad706cfaf35eb18598377d38ca47eebba70ffe59a01f9e1340":
            raise ValueError("Current canonical BattleCatalog asset pin changed")
        graph = read_json(current_graph_path, 16 * 1024 * 1024)
        graph_pins = graph.get("source_pins", {})
        catalog_path = regular_path(str(root / "Assets/Game/Resources/SkirmishBattleCatalog.asset"), root)
        if graph_pins.get(str(catalog_path)) != sha_file(catalog_path):
            raise ValueError("Augmented compiler graph does not pin the exact actual BattleCatalog resource")

        legacy_fields = ("source_config_graph", "metadata_compiler_context", "configuration_qualification",
                         "building_definitions", "unit_production_metadata", "building_request_metadata", "reference_templates")
        legacy_replacements = []
        for field in legacy_fields:
            old_descriptor = old_init.get(field)
            new_descriptor = initial.get(field)
            if type(old_descriptor) is not dict or type(new_descriptor) is not dict:
                raise ValueError("Expected legacy/current descriptor pair missing: " + field)
            old_paths = [old_descriptor.get("path")]
            if field == "reference_templates":
                old_paths.extend(old_descriptor.get(name) for name in ("source_reference_path", "projection_manifest_path"))
            path_status = []
            for raw_path in old_paths:
                if not isinstance(raw_path, str):
                    raise ValueError("Malformed retained legacy descriptor path: " + field)
                path_status.append({"path": raw_path, "exists": Path(raw_path).is_file()})
            legacy_replacements.append({"field": field, "old_descriptor": old_descriptor,
                                       "new_descriptor": new_descriptor, "old_path_status": path_status})

        runtime_inputs = retained_bundle.get("runtime_data_inputs")
        if type(runtime_inputs) is not dict or runtime_inputs.get("kind") != "CurrentRoadSceneRuntimeInputs" or runtime_inputs.get("data_root") != str(root):
            raise ValueError("Retained runtime data input witness is incomplete")
        for section in ("data_files", "source_files"):
            pins = runtime_inputs.get(section)
            if type(pins) is not dict or not pins:
                raise ValueError("Retained runtime input pin set missing: " + section)
            for relative, expected in pins.items():
                path = regular_path(relative, root)
                if sha_file(path) != expected:
                    raise ValueError("Retained runtime data input changed: " + relative)

        # Copy only the authentic R16 run-input descriptors and prove their bytes.
        run_input_names = ("camera_profile", "static_clearance", "camera_values", "camera_motion")
        for name in run_input_names:
            retained_bundle[name] = rebase_retained_descriptor(retained_bundle[name], "run input " + name)
        # The immutable projection still records its historical /private/tmp source
        # path. Carry the byte identity as a separate run input so the consumer can
        # resolve it through this content-addressed descriptor without rewriting the
        # projection or copying the large artifact again.
        clearance_source_artifact = resolve_s003_clearance_source_artifact(
            root, initial["navigation_reference"], retained_bundle["static_clearance"])
        retained_bundle["command_text_data"] = initial["command_text_data"]
        runtime_contract_audit = audit_retained_runtime_inputs(root, initial, retained_bundle)
        if context["compiler_graph"] != initial["source_config_graph"]:
            runtime_contract_audit["source_gate_compatibility"] = {
                "status": "FailedDualGraphAuthorityMismatch",
                "runtime_source_config_graph": initial["source_config_graph"],
                "lifetime_authority_requires_context_source_graph": context["source_graph"],
                "qualification_requires_runtime_compiler_graph": context["compiler_graph"],
                "scope": "No source graph substitution is permitted; current lifetime-authority mismatch blocks emission",
            }
            runtime_contract_audit["current_runtime_input_contract_satisfied"] = False
        if not runtime_contract_audit["current_runtime_input_contract_satisfied"]:
            raise ValueError("Current S003 runtime input contract is incomplete; see failure receipt")

        output_init = output / "initialization.json"
        output_bundle = output / "data-bundle.json"
        init_descriptor = {"path": str(output_init.resolve()), "sha256": hashlib.sha256((json.dumps(initial, indent=2, ensure_ascii=False) + "\n").encode()).hexdigest()}
        new_bundle = {
            "schema_version": 1, "mission": "S003", "seed": 104732,
            "runtime_backend": "TypedGroundS026V1", "initialization": init_descriptor,
            "unit_metadata": initial["unit_production_metadata"],
            "scope": "Current source evidence rebound to retained S003 Desert Base inputs; no mission/world/readiness authority",
            "runtime_data_inputs": runtime_inputs,
            "command_text_sources": retained_bundle["command_text_sources"],
            "camera_profile": retained_bundle["camera_profile"],
            "static_clearance": retained_bundle["static_clearance"],
            "static_clearance_source_artifact": clearance_source_artifact,
            "camera_values": retained_bundle["camera_values"],
            "camera_motion": retained_bundle["camera_motion"],
        }
        raw_init = (json.dumps(initial, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
        raw_bundle = (json.dumps(new_bundle, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
        if hashlib.sha256(raw_init).hexdigest() != init_descriptor["sha256"]:
            raise ValueError("Rebound initialization serialization hash changed")
        with output_init.open("xb") as stream:
            stream.write(raw_init)
        with output_bundle.open("xb") as stream:
            stream.write(raw_bundle)
        # Re-open and re-hash all emitted descriptors before the passed receipt.
        if sha_file(output_init) != init_descriptor["sha256"] or sha_file(output_bundle) != hashlib.sha256(raw_bundle).hexdigest():
            raise ValueError("Fresh S003 rebound outputs changed during emission")
        receipt = {
            "schema_version": 1, "kind": "S003CurrentSourceDataBundleRebindReceipt", "status": "PassedSourceOnlyBundleRebind",
            "source_bundle": old_input_descriptors["bundle"], "source_initialization": old_input_descriptors["initialization"],
            "relocated_r6_template_inputs": relocation,
            "replaced_legacy_dependencies": legacy_replacements,
            "qualification_descriptor": qualification_records,
            "metadata_projection_receipt": file_descriptor(regular_path(str(projection_dir / "projection-receipt.json"), root)),
            "static_clearance_source_relocation_receipt": file_descriptor(regular_path(str(root / CLEARANCE_RELOCATION_RECEIPT), root)),
            "static_clearance_source_artifact": clearance_source_artifact,
            "prefab_proof": prefab_proof,
            "unit_move_authoring_source": {
                "descriptor": file_descriptor(unit_move_source_path),
                "kind": unit_move_source_input["kind"],
                "root_key": unit_move_source_input["root_key"],
                "config_guid": unit_move_source_input["config_guid"],
                "values_bits": unit_move_source_input["values_bits"],
                "source_pin_count": len(unit_move_source_input["source_pins"]),
            },
            "runtime_contract_audit": runtime_contract_audit,
            "retained_r15_initialization": file_descriptor(retained_init_path),
            "retained_r16_bundle": file_descriptor(retained_bundle_path),
            "runtime_compiler_graph": {"path": str(current_graph_path), "sha256": current_graph_sha},
            "source_selection_extension_receipt": extension,
            "source_grid_surface_extension_receipt": grid_surface_extension,
            "prefab_capture_graph": {"path": str(graph_path), "sha256": capture_graph_sha},
            "output_initialization": file_descriptor(output_init), "output_bundle": file_descriptor(output_bundle),
            "same_desert_base_operation_map": initial["selected_operation_map"],
            "runtime_admission": False, "native_context_qualified": False, "source_fit_certified": False,
            "mission_world_created": False, "mission_ready": False,
            "scope": "Offline source evidence bundle rebinding only; no gameplay run or readiness authority",
        }
        with (output / "rebind-receipt.json").open("xb") as stream:
            stream.write((json.dumps(receipt, indent=2, ensure_ascii=False) + "\n").encode("utf-8"))
        print(json.dumps({"status": receipt["status"], "bundle": str(output_bundle), "initialization": str(output_init),
                          "runtime_admission": False, "mission_ready": False}))
        return receipt
    except Exception as exc:
        failure = {"schema_version": 1, "kind": "S003CurrentSourceDataBundleRebindFailure", "status": "Failed",
                   "error": str(exc), "scope": "No bundle admitted; existing retained inputs unchanged"}
        if "runtime_contract_audit" in locals():
            failure["runtime_contract_audit"] = runtime_contract_audit
        with (output / "failure-receipt.json").open("xb") as stream:
            stream.write((json.dumps(failure, indent=2) + "\n").encode("utf-8"))
        raise


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--bundle", type=Path, default=ROOT / OLD_DIR / "data-bundle.json")
    parser.add_argument("--initialization", type=Path, default=ROOT / OLD_DIR / "initialization.json")
    parser.add_argument("--projection", type=Path, default=ROOT / T17_PROJECTION)
    parser.add_argument("--qualification", type=Path, default=ROOT / T18_DESCRIPTOR)
    parser.add_argument("--prefab-descriptor", type=Path, default=ROOT / PREFAB_DESCRIPTOR)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        rebind(args.repo, args.bundle, args.initialization, args.projection,
               args.qualification, args.prefab_descriptor, args.output)
        return 0
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as error:
        print("S003 current source bundle rebind failed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
