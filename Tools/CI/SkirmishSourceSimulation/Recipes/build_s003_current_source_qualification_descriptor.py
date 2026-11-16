#!/usr/bin/env python3
"""Build a source-only S003 qualification descriptor from exact current evidence.

The output is provenance groundwork only. It does not activate a runtime
profile, authorize mission execution, or claim native readiness.
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
GRAPH_REL = Path("Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json")
CAPTURE_REL = Path("Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata.json")
CAPTURE_RECEIPT_REL = CAPTURE_REL.parent / "s003-native-metadata-receipt.json"
PROJECTION_REL = Path("T Logs/SkirmishProduction/s003-current-final-gate-projection-20261103-T3")
RUN6_REL = Path("T Logs/SkirmishProduction/s003-original-compiler-diagnostic-20261103-T1/run-6-seed-104732")
RUN6_GRAPH_REL = RUN6_REL.parent / "source-config-graph-with-current-catalog-closure.json"
CURRENT_COMPILER_GRAPH_REL = Path("T Logs/SkirmishProduction/s003-final-runtime-source-graph-20261103-T1/source-config-graph-with-grid-surface-nodes.json")
FINALIZER_REL = Path("Tools/CI/SkirmishSourceSimulation/Tests/finalize_s003_native_metadata_capture.py")
PROJECTOR_REL = Path("Tools/CI/SkirmishSourceSimulation/Recipes/project_s003_current_factory_metadata.py")

PINNED = {
    "graph": "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb",
    "capture": "2f857f965acd8743339ee73cf032ddf54e4ba7f07f278f4cde50d1e1876e91d3",
    "capture_receipt": "aff49e834d5f2c74fe6b5a8c9666fdfeb297fb18b624ed81b5d5f4348db494f0",
    "run6_receipt": "3d518a67a576bf223d66fb88adf181fc893bfb890200d902948d4a1a36b13609",
    "run6_audit": "52b5e97a494dc4adf5e5159e65fbe5921f57ad90b730012c5d0698f820abeda9",
    "run6_graph": "150cb503bb95dcdb1371e2c52c54764308b9bf1ad5e6305ae7ba3a68bf39cc5b",
    "current_compiler_graph": "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f",
    "setup_hash": 2394754492,
    "metadata_key_sha256": "2df9f3e2140f9e8146311e5d5dd5c5c94c99b016e55a1da14968c863cb29e9ae",
}


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def sha_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def pairs(items):
    result = {}
    for key, value in items:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=pairs,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Invalid JSON number: " + value)))


def descriptor(path: Path) -> dict:
    path = path.resolve(strict=True)
    return {"path": str(path), "sha256": sha_file(path)}


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise ValueError("Required evidence validator unavailable: " + str(path))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def ensure_repo_file(root: Path, rel: Path, expected: str | None = None) -> Path:
    path = root / rel
    if path.is_symlink() or not path.is_file():
        raise ValueError("Required regular S003 evidence file missing: " + str(rel))
    resolved = path.resolve(strict=True)
    if not resolved.is_relative_to(root):
        raise ValueError("S003 evidence file escaped repository: " + str(rel))
    actual = sha_file(resolved)
    if expected is not None and actual != expected:
        raise ValueError("S003 evidence hash mismatch: " + str(rel))
    return resolved


def pinned_path(root: Path, raw_path: str) -> Path:
    candidate = Path(raw_path)
    if not candidate.is_absolute():
        candidate = root / candidate
    lexical = Path(os.path.abspath(candidate))
    if not lexical.is_relative_to(root):
        # The exact pinned source-build closure also contains host Unity/.NET
        # reference assemblies. Their provenance is the pinned build receipt.
        resolved = lexical.resolve(strict=True)
        if not resolved.is_file() or lexical.is_symlink():
            raise ValueError("S003 external toolchain pin is not a regular file: " + raw_path)
        return resolved
    cursor = root
    for part in lexical.relative_to(root).parts:
        cursor = cursor / part
        if cursor.is_symlink():
            raise ValueError("Symlink in S003 evidence pin: " + raw_path)
    resolved = lexical.resolve(strict=True)
    if not resolved.is_relative_to(root) or not resolved.is_file():
        raise ValueError("S003 evidence pin is not a repository file: " + raw_path)
    return resolved


def descriptor_path(root: Path, value: dict, label: str) -> Path:
    if type(value) is not dict or set(value) != {"path", "sha256"} or type(value["path"]) is not str or type(value["sha256"]) is not str:
        raise ValueError("Exact " + label + " descriptor required")
    candidate = Path(value["path"])
    if not candidate.is_absolute() or not re.fullmatch(r"[0-9a-f]{64}", value["sha256"]):
        raise ValueError("Canonical " + label + " descriptor required")
    try:
        relative = candidate.relative_to(root)
    except ValueError:
        raise ValueError(label + " escaped repository")
    return ensure_repo_file(root, relative, value["sha256"])


def validate_evidence(root: Path, projection_dir: Path) -> dict:
    root = root.resolve(strict=True)
    graph_path = ensure_repo_file(root, GRAPH_REL, PINNED["graph"])
    capture_path = ensure_repo_file(root, CAPTURE_REL, PINNED["capture"])
    capture_receipt_path = ensure_repo_file(root, CAPTURE_RECEIPT_REL, PINNED["capture_receipt"])
    finalizer_path = ensure_repo_file(root, FINALIZER_REL)
    projector_path = ensure_repo_file(root, PROJECTOR_REL)

    finalizer = load_module(finalizer_path, "s003_descriptor_capture_finalizer")
    freshly_qualified = finalizer.qualify(root, capture_path.parent, 0)
    capture_receipt = read_json(capture_receipt_path)
    if capture_receipt != freshly_qualified or capture_receipt.get("validation_status") != "Passed":
        raise ValueError("Current S003 capture receipt does not match fresh finalizer validation")
    if any(capture_receipt.get(flag) is not False for flag in ("mission_world_created", "native_context_qualified", "source_fit_certified", "mission_ready")):
        raise ValueError("S003 capture receipt scope promotion rejected")

    graph = read_json(graph_path)
    if graph.get("authority") != "AuthoredConfigAssets" or graph.get("compiled_setup") is not False or graph.get("world_initialized") is not False or graph.get("native_world_state_used") is not False:
        raise ValueError("Current uncompiled S003 graph required")
    capture = read_json(capture_path)

    projection_dir = Path(os.path.abspath(projection_dir))
    if not projection_dir.is_relative_to(root) or projection_dir.is_symlink() or not projection_dir.is_dir():
        raise ValueError("S003 projection directory must be a current repository directory")
    projection_receipt_path = ensure_repo_file(root, projection_dir.relative_to(root) / "projection-receipt.json")
    projection = read_json(projection_receipt_path)
    if projection.get("kind") != "S003CurrentFactoryMetadataProjectionReceipt" or projection.get("status") != "PassedSourceOnlyProjection":
        raise ValueError("Exact current S003 source-only projection receipt required")
    if projection.get("target") != {"catalog_id": "S003", "difficulty": "Regular", "size": "Standard", "seed": 104732, "setup_hash": PINNED["setup_hash"]}:
        raise ValueError("S003 projected compiler target changed")
    if projection.get("capture", {}).get("sha256") != PINNED["capture"] or projection.get("capture", {}).get("metadata_key_sha256") != PINNED["metadata_key_sha256"]:
        raise ValueError("Projection does not bind exact S003 capture metadata")
    if projection.get("capture", {}).get("path") != str(capture_path):
        raise ValueError("Projection capture path differs from exact current capture")
    if projection.get("source_graph", {}).get("sha256") != PINNED["graph"] or projection.get("run6_compiler_receipt_sha256") != PINNED["run6_receipt"] or projection.get("run6_post_run_pin_audit_sha256") != PINNED["run6_audit"]:
        raise ValueError("Projection compiler/source graph proof differs")
    compatibility_path = root / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceMetadataCompatibility.s003.cs"
    if projection.get("factory_consumer_source") != {"path": str(compatibility_path), "sha256": sha_file(compatibility_path)} or projection.get("projector_source") != {"path": str(projector_path), "sha256": sha_file(projector_path)} or projection.get("capture_finalizer_source") != {"path": str(finalizer_path), "sha256": sha_file(finalizer_path)}:
        raise ValueError("Projection tooling or current factory consumer source provenance differs")
    if projection.get("source_pin_count") != 8782 or projection.get("counts") != {"buildings": 10, "units": 19, "recipes": 19, "transports": 1}:
        raise ValueError("Projection source/domain counts changed")
    for flag in ("native_context_qualified", "source_fit_certified", "mission_ready", "runtime_admission"):
        if projection.get(flag) is not False:
            raise ValueError("Projection scope promotion rejected: " + flag)

    artifacts = projection.get("artifacts")
    artifact_names = {"building-definitions.json", "unit-production-metadata.json", "configured-metadata.json"}
    if not isinstance(artifacts, dict) or set(artifacts) != artifact_names:
        raise ValueError("Exact three S003 factory consumer artifacts required")
    artifact_paths = {}
    artifact_values = {}
    for name in sorted(artifact_names):
        item = artifacts[name]
        if not isinstance(item, dict) or set(item) != {"path", "sha256"} or not isinstance(item.get("sha256"), str) or not re.fullmatch(r"[0-9a-f]{64}", item["sha256"]):
            raise ValueError("S003 projected artifact descriptor changed: " + name)
        path = Path(item["path"])
        if not path.is_absolute() or path.resolve(strict=True).parent != projection_dir.resolve(strict=True):
            raise ValueError("Projected artifact escaped its exclusive output directory: " + name)
        if path.is_symlink() or sha_file(path) != item["sha256"]:
            raise ValueError("Projected artifact file changed: " + name)
        value = read_json(path)
        artifact_paths[name] = path.resolve(strict=True)
        artifact_values[name] = value
        if value.get("native_origin", {}).get("artifact_sha256") != PINNED["capture"] or value.get("native_origin", {}).get("metadata_key_sha256") != PINNED["metadata_key_sha256"]:
            raise ValueError("Projected artifact origin mismatch: " + name)
        if value.get("full_mission_initialized") is not False or value.get("native_visual_readiness") is not False or value.get("unsupported_values") != []:
            raise ValueError("Projected artifact flags or unsupported values changed: " + name)

    definitions = artifact_values["building-definitions.json"]
    units = artifact_values["unit-production-metadata.json"]
    configured = artifact_values["configured-metadata.json"]
    if definitions.get("kind") != "NativeQualifiedSkirmishBuildingDefinitions" or len(definitions.get("definitions", [])) != 10:
        raise ValueError("S003 building definition projection domain mismatch")
    if units.get("kind") != "NativeQualifiedSkirmishUnitProductionMetadata" or len(units.get("units", [])) != 19 or len(units.get("transports", [])) != 1:
        raise ValueError("S003 unit/transport projection domain mismatch")
    if configured.get("kind") != "NativeQualifiedConfiguredBuildingMetadata" or len(configured.get("rows", [])) != 10:
        raise ValueError("S003 configured projection domain mismatch")
    reference = configured.get("reference")
    if not isinstance(reference, dict) or Path(reference.get("path", "")).resolve() != artifact_paths["building-definitions.json"] or reference.get("sha256") != artifacts["building-definitions.json"]["sha256"]:
        raise ValueError("Configured metadata reference does not bind definitions artifact")

    if projection.get("capture", {}).get("receipt_path") != str(capture_receipt_path) or projection.get("capture", {}).get("receipt_sha256") != PINNED["capture_receipt"]:
        raise ValueError("Projection does not bind the exact native capture qualification receipt")
    current_epoch = projection.get("current_compiler_epoch")
    if type(current_epoch) is not dict or set(current_epoch) != {"compiler_receipt_path", "compiler_receipt_sha256", "compiler_audit_path", "compiler_audit_sha256", "build_path", "build_sha256", "graph_closure_path", "graph_closure_sha256", "source_build_pin_count", "compiler_graph_pin_count", "source_selection_extension_receipt", "source_grid_surface_extension_receipt", "setups"}:
        raise ValueError("Exact current S003 compiler epoch descriptor required")
    current_compiler_path = descriptor_path(root, {"path": current_epoch["compiler_receipt_path"], "sha256": current_epoch["compiler_receipt_sha256"]}, "current compiler receipt")
    current_audit_path = descriptor_path(root, {"path": current_epoch["compiler_audit_path"], "sha256": current_epoch["compiler_audit_sha256"]}, "current compiler pin audit")
    current_build_path = descriptor_path(root, {"path": current_epoch["build_path"], "sha256": current_epoch["build_sha256"]}, "current source build receipt")
    current_graph_path = descriptor_path(root, {"path": current_epoch["graph_closure_path"], "sha256": current_epoch["graph_closure_sha256"]}, "current compiler graph closure")
    extension_descriptor = current_epoch["source_selection_extension_receipt"]
    for field, expected in (("current_compiler_receipt_sha256", current_epoch["compiler_receipt_sha256"]),
                            ("current_compiler_post_run_pin_audit_sha256", current_epoch["compiler_audit_sha256"]),
                            ("current_source_build_receipt_sha256", current_epoch["build_sha256"]),
                            ("current_compiler_graph_closure_sha256", PINNED["current_compiler_graph"])):
        if projection.get(field) != expected:
            raise ValueError("Projection current S003 proof descriptor mismatch: " + field)
    if current_epoch["graph_closure_sha256"] != PINNED["current_compiler_graph"]:
        raise ValueError("Current augmented S003 compiler graph closure identity changed")
    current_compiler = read_json(current_compiler_path)
    current_audit = read_json(current_audit_path)
    current_build = read_json(current_build_path)
    current_graph = read_json(current_graph_path)
    current_graph_validator = load_module(projector_path, "s003_current_graph_validator")
    selection_extension, grid_surface_extension = current_graph_validator.validate_source_graph_extensions(root, current_graph, current_graph_path)
    selection_extension_path = descriptor_path(root, extension_descriptor, "current selector graph extension receipt")
    if extension_descriptor != selection_extension or extension_descriptor != {"path": str(selection_extension_path), "sha256": sha_file(selection_extension_path)}:
        raise ValueError("Current S003 context does not bind the exact 349→e498 selector graph extension receipt")
    grid_surface_descriptor = current_epoch["source_grid_surface_extension_receipt"]
    grid_surface_path = descriptor_path(root, grid_surface_descriptor, "current grid/surface graph extension receipt")
    if grid_surface_descriptor != grid_surface_extension or grid_surface_descriptor != {"path": str(grid_surface_path), "sha256": sha_file(grid_surface_path)}:
        raise ValueError("Current S003 context does not bind the exact e498→3419 grid/surface graph extension receipt")
    current_graph_validator.validate_augmented_compiler_graph(current_graph, current_epoch["graph_closure_sha256"])
    if current_compiler.get("kind") != "S003OriginalCompilerHarnessReceipt" or current_compiler.get("status") != "Compiled" or current_compiler.get("seed") != 104732:
        raise ValueError("Fresh current S003 compiler receipt required")
    expected_setups = [
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "Standard", "difficulty_id": "Regular", "setup_hash": 2394754492, "compiled": True},
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "War", "difficulty_id": "Regular", "setup_hash": 2291016159, "compiled": True},
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "LargeWar", "difficulty_id": "Regular", "setup_hash": 539080277, "compiled": True},
    ]
    if current_epoch.get("setups") != expected_setups or current_compiler.get("result", {}).get("compiled") is not True or current_compiler.get("result", {}).get("setups") != expected_setups:
        raise ValueError("Exact current S003 three-size compiler result required")
    if current_compiler.get("source_build") != {"path": str(current_build_path), "sha256": current_epoch["build_sha256"]} or current_compiler.get("source_graph") != {"path": str(current_graph_path), "sha256": PINNED["current_compiler_graph"]}:
        raise ValueError("Current S003 compiler does not bind its exact build and graph inputs")
    if current_compiler.get("source_graph_source_pins") != current_graph.get("source_pins"):
        raise ValueError("Current compiler graph source pins differ")
    if current_build.get("kind") != "SkirmishSourceSimulationBuild" or current_build.get("mission") != "S002" or current_build.get("status") != "Passed" or current_build.get("source_count") != 729 or current_build.get("compile_exit") != 0:
        raise ValueError("Fresh passing S002 source-host build required")
    source_pins = capture.get("source_pins")
    if not isinstance(source_pins, dict) or len(source_pins) != 8782:
        raise ValueError("Exact 8,782-entry native capture pin closure required")
    for raw_path, expected in source_pins.items():
        if sha_file(pinned_path(root, raw_path)) != expected:
            raise ValueError("Current native capture source pin mismatch: " + raw_path)
    current_build_pins = current_build.get("source_and_reference_pins")
    current_graph_pins = current_graph.get("source_pins")
    if type(current_build_pins) is not dict or not current_build_pins or type(current_graph_pins) is not dict or not current_graph_pins:
        raise ValueError("Non-empty current compiler/build source-pin closures required")
    for raw_path, expected in current_build_pins.items():
        if sha_file(pinned_path(root, raw_path)) != expected:
            raise ValueError("Current source-build pin mismatch: " + raw_path)
    for raw_path, expected in current_graph_pins.items():
        if sha_file(pinned_path(root, raw_path)) != expected:
            raise ValueError("Current compiler graph pin mismatch: " + raw_path)
    if current_audit.get("kind") != "S003CurrentFactoryEpochCompilerPinAudit" or current_audit.get("status") != "Passed":
        raise ValueError("Independent current compiler pin audit must pass")
    expected_audit_checks = {key: [] for key in ("build_pins", "compiler_graph_pins", "graph_pins", "capture_pins", "overlap_mismatches")}
    if current_audit.get("compiler_receipt") != {"path": str(current_compiler_path), "sha256": current_epoch["compiler_receipt_sha256"]} or current_audit.get("source_build") != {"path": str(current_build_path), "sha256": current_epoch["build_sha256"]} or current_audit.get("source_graph") != {"path": str(current_graph_path), "sha256": PINNED["current_compiler_graph"]} or current_audit.get("capture") != {"path": str(capture_path), "sha256": PINNED["capture"]}:
        raise ValueError("Independent current S003 compiler audit identity mismatch")
    if current_audit.get("checks") != expected_audit_checks or current_audit.get("source_build_pin_count") != len(current_build_pins) or current_audit.get("compiler_graph_pin_count") != len(current_graph_pins) or current_audit.get("source_graph_pin_count") != len(current_graph_pins) or current_audit.get("capture_pin_count") != len(source_pins) or current_audit.get("shared_compiler_graph_pin_count") != len(current_graph_pins):
        raise ValueError("Current S003 compiler pin audit does not prove complete rehash")

    # Reconstruct every projected artifact from the qualified native capture.
    projector = load_module(projector_path, "s003_descriptor_projector")
    factory_sources = projector.read_factory_sources(root)
    expected_artifacts = projector.project_payload(capture, PINNED["capture"], source_pins, factory_sources)
    expected_artifacts["configured-metadata.json"]["reference"] = artifacts["building-definitions.json"]
    if any(artifact_values[name] != expected_artifacts[name] for name in artifact_names):
        raise ValueError("Projected factory artifact contents differ from the qualified native capture")

    run6_dir = root / RUN6_REL
    run6_path = ensure_repo_file(root, RUN6_REL / "receipt.json", PINNED["run6_receipt"])
    audit_path = ensure_repo_file(root, RUN6_REL / "post-run-pin-audit.json", PINNED["run6_audit"])
    run6 = read_json(run6_path)
    audit = read_json(audit_path)
    if run6.get("status") != "Compiled" or run6.get("seed") != 104732 or run6.get("result", {}).get("compiled") is not True:
        raise ValueError("Exact successful run-6 source compiler proof required")
    if run6.get("result", {}).get("setups") != [
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "Standard", "difficulty_id": "Regular", "setup_hash": 2394754492, "compiled": True},
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "War", "difficulty_id": "Regular", "setup_hash": 2291016159, "compiled": True},
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "LargeWar", "difficulty_id": "Regular", "setup_hash": 539080277, "compiled": True},
    ]:
        raise ValueError("Exact three-size run-6 source compiler results required")
    run_graph_path = ensure_repo_file(root, RUN6_GRAPH_REL, PINNED["run6_graph"])
    if not run_graph_path.is_file() or run_graph_path.is_symlink() or sha_file(run_graph_path) != PINNED["run6_graph"]:
        raise ValueError("Exact run-6 current catalog-closure graph required")
    if audit.get("source_graph_pin_mismatches") != [] or audit.get("source_build_pin_mismatches") != [] or audit.get("source_graph_sha256_after") != PINNED["run6_graph"]:
        raise ValueError("Run-6 post-run pin audit did not pass")

    # The projection receipt supplies explicit proof paths; validate and rehash
    # those exact files rather than binding one run directory by convention.
    for pins, label in ((source_pins, "native capture"), (current_build_pins, "source build"), (current_graph_pins, "compiler graph")):
        for raw_path, expected in pins.items():
            if sha_file(pinned_path(root, raw_path)) != expected:
                raise ValueError("Current S003 " + label + " pin changed during qualification: " + raw_path)
    for proof_path, expected, label in ((current_compiler_path, current_epoch["compiler_receipt_sha256"], "compiler receipt"),
                                        (current_audit_path, current_epoch["compiler_audit_sha256"], "compiler pin audit"),
                                        (current_build_path, current_epoch["build_sha256"], "source build receipt"),
                                        (current_graph_path, PINNED["current_compiler_graph"], "compiler graph closure")):
        if sha_file(proof_path) != expected:
            raise ValueError("Current S003 " + label + " changed during qualification")

    if sha_file(graph_path) != PINNED["graph"] or sha_file(capture_path) != PINNED["capture"]:
        raise ValueError("S003 graph or capture changed during qualification")

    return {
        "root": root,
        "graph_path": graph_path,
        "capture_path": capture_path,
        "capture_receipt_path": capture_receipt_path,
        "projection_dir": projection_dir,
        "projection_receipt_path": projection_receipt_path,
        "projection_sha256": sha_file(projection_receipt_path),
        "projection": projection,
        "artifact_paths": artifact_paths,
        "run6_path": run6_path,
        "audit_path": audit_path,
        "current_compiler_path": current_compiler_path,
        "current_audit_path": current_audit_path,
        "current_graph_path": current_graph_path,
        "current_graph_pins": current_graph_pins,
        "current_compiler": current_compiler,
        "current_epoch": current_epoch,
        "build_path": current_build_path,
        "source_pins": source_pins,
        "build_pins": current_build_pins,
        "finalizer_path": finalizer_path,
        "projector_path": projector_path,
        "builder_path": Path(__file__).resolve(),
    }


def construct_descriptors(evidence: dict) -> tuple[dict, dict]:
    target = {"catalog_id": "S003", "definition_id": "skirmish.s003", "seed": 104732,
              "size": "Standard", "difficulty": "Regular", "setup_hash": PINNED["setup_hash"],
              "layout_binding_fingerprint": "", "mission_logical_binding_fingerprint": ""}
    context = {
        "schema_version": 1,
        "kind": "OriginalS003CurrentSourceOnlyFactoryContext",
        "authority": "CurrentAuthoredS003Graph_OriginalCompilerAndFactoryData_DescriptorOnly",
        "target_context": target,
        "source_graph": descriptor(evidence["graph_path"]),
        "compiler_receipt": descriptor(evidence["current_compiler_path"]),
        "compiler_post_run_pin_audit": descriptor(evidence["current_audit_path"]),
        "source_build_receipt": descriptor(evidence["build_path"]),
        "compiler_graph": descriptor(evidence["current_graph_path"]),
        "source_selection_extension_receipt": evidence["current_epoch"]["source_selection_extension_receipt"],
        "source_grid_surface_extension_receipt": evidence["current_epoch"]["source_grid_surface_extension_receipt"],
        "factory_capture_historical_compiler_receipt": descriptor(evidence["run6_path"]),
        "factory_capture_historical_compiler_audit": descriptor(evidence["audit_path"]),
        "native_factory_capture": descriptor(evidence["capture_path"]),
        "native_factory_capture_receipt": descriptor(evidence["capture_receipt_path"]),
        "factory_projection_receipt": descriptor(evidence["projection_receipt_path"]),
        "factory_projection_artifacts": {name: descriptor(path) for name, path in evidence["artifact_paths"].items()},
        "runtime_admission": False,
        "mission_ready": False,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "full_mission_initialized": False,
        "mission_world_created": False,
        "runtime_state_transferred": False,
        "scope": "Current S003 compiler and factory projection provenance only; descriptor is not runtime admission",
        "descriptor_builder_source": descriptor(evidence["builder_path"]),
    }
    qualification = {
        "schema_version": 1,
        "kind": "ExperimentalS003CurrentSourceQualificationDescriptor",
        "scope": "DescriptorOnly_CurrentCompilerAndFactoryData_SourceOnly",
        "compiler_context": None,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "runtime_admission": False,
        "mission_ready": False,
    }
    return context, qualification


def _write_exclusive(path: Path, value: dict) -> None:
    raw = (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    with path.open("xb") as stream:
        stream.write(raw)


def validate_descriptor_scope(context: dict, qualification: dict) -> None:
    for value in (context, qualification):
        for flag in ("runtime_admission", "mission_ready", "native_context_qualified", "source_fit_certified"):
            if value.get(flag) is not False:
                raise ValueError("S003 descriptor scope promotion rejected: " + flag)
    if context.get("kind") != "OriginalS003CurrentSourceOnlyFactoryContext" or qualification.get("kind") != "ExperimentalS003CurrentSourceQualificationDescriptor":
        raise ValueError("Exact descriptor-only S003 artifact kinds required")
    if qualification.get("compiler_context") is not None:
        raise ValueError("Qualification descriptor must not embed runtime compiler admission")


def build(root: Path, projection_dir: Path, output: Path) -> dict:
    root = root.resolve(strict=True)
    output = Path(os.path.abspath(output))
    if output.exists():
        raise ValueError("Fresh exclusive descriptor output directory required")
    evidence = validate_evidence(root, projection_dir)
    context, qualification = construct_descriptors(evidence)
    validate_descriptor_scope(context, qualification)
    output.mkdir(parents=True, exist_ok=False)
    context_path = output / "s003-current-source-only-context.json"
    context_sha = sha((json.dumps(context, indent=2, ensure_ascii=False) + "\n").encode("utf-8"))
    context_desc = {"path": str(context_path.resolve()), "sha256": context_sha}
    qualification["compiler_context"] = context_desc
    qualification_path = output / "s003-current-source-qualification-descriptor.json"
    _write_exclusive(context_path, context)
    _write_exclusive(qualification_path, qualification)

    # Recheck key identity proofs after emission. Full source/build closures are
    # checked again to ensure the descriptors bind the validated same epoch.
    if sha_file(context_path) != context_sha:
        raise ValueError("S003 context descriptor changed while writing")
    if sha_file(evidence["graph_path"]) != PINNED["graph"] or sha_file(evidence["capture_path"]) != PINNED["capture"]:
        raise ValueError("S003 current graph or capture changed after descriptor construction")
    if sha_file(evidence["projection_receipt_path"]) != evidence["projection_sha256"]:
        raise ValueError("S003 projection receipt changed after descriptor construction")
    # Full source/build closures are rehashed after output creation as well.
    for raw_path, expected in evidence["source_pins"].items():
        if sha_file(pinned_path(evidence["root"], raw_path)) != expected:
            raise ValueError("S003 capture pin changed after descriptor construction: " + raw_path)
    for raw_path, expected in evidence["build_pins"].items():
        if sha_file(pinned_path(evidence["root"], raw_path)) != expected:
            raise ValueError("S003 source build pin changed after descriptor construction: " + raw_path)
    for raw_path, expected in evidence["current_graph_pins"].items():
        if sha_file(pinned_path(evidence["root"], raw_path)) != expected:
            raise ValueError("S003 current compiler graph pin changed after descriptor construction: " + raw_path)
    for proof_name, proof_path, expected in (
        ("current compiler receipt", evidence["current_compiler_path"], evidence["current_epoch"]["compiler_receipt_sha256"]),
        ("current compiler audit", evidence["current_audit_path"], evidence["current_epoch"]["compiler_audit_sha256"]),
        ("historical capture compiler receipt", evidence["run6_path"], PINNED["run6_receipt"]),
        ("historical capture compiler audit", evidence["audit_path"], PINNED["run6_audit"]),
        ("current compiler graph", evidence["current_graph_path"], PINNED["current_compiler_graph"]),
        ("current build receipt", evidence["build_path"], evidence["current_epoch"]["build_sha256"]),
    ):
        if sha_file(proof_path) != expected:
            raise ValueError("S003 " + proof_name + " changed after descriptor construction")
    builder_source = descriptor(evidence["builder_path"])
    if builder_source != context["descriptor_builder_source"]:
        raise ValueError("S003 descriptor builder changed while constructing evidence")

    receipt = {
        "schema_version": 1,
        "kind": "S003CurrentSourceQualificationDescriptorBuildReceipt",
        "status": "PassedDescriptorOnly",
        "context": context_desc,
        "qualification_descriptor": descriptor(qualification_path),
        "source_graph_sha256": PINNED["graph"],
        "compiler_receipt_sha256": evidence["current_epoch"]["compiler_receipt_sha256"],
        "compiler_post_run_pin_audit_sha256": evidence["current_epoch"]["compiler_audit_sha256"],
        "source_build_receipt_sha256": evidence["current_epoch"]["build_sha256"],
        "native_capture_sha256": PINNED["capture"],
        "factory_projection_receipt_sha256": evidence["projection_sha256"],
        "descriptor_builder_source": builder_source,
        "capture_pin_count": len(evidence["source_pins"]),
        "source_build_pin_count": len(evidence["build_pins"]),
        "runtime_admission": False,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "mission_ready": False,
        "scope": "Source evidence descriptor only; no consumer gate changed or runtime authority granted",
    }
    _write_exclusive(output / "build-receipt.json", receipt)
    print(json.dumps({"status": receipt["status"], "runtime_admission": False,
                      "mission_ready": False, "context": context_desc}))
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--projection", type=Path, default=ROOT / PROJECTION_REL)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        build(args.repo, args.projection, args.output)
        return 0
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as error:
        print("S003 qualification descriptor build failed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
