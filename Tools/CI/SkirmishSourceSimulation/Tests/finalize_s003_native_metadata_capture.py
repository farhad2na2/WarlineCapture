"""Finalize the exact S003 original-compiler metadata capture, not mission readiness."""
import argparse
import hashlib
import json
import os
from pathlib import Path


GRAPH_RELATIVE_PATH = Path(
    "Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json"
)
GRAPH_SHA256 = "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb"
CAPTURE_RELATIVE_PATH = Path(
    "Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata.json"
)
CAPTURE_LOG_RELATIVE_PATH = Path(
    "Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/editor.log"
)
EXPECTED_SETUP_HASH = 2394754492
EXPECTED_CATALOG_SHA256 = "e206b57a16d0876d504dab66cf5f8f93de13fa96844c5b2e30a234637885e6a8"
RUN6_RELATIVE_DIR = Path(
    "T Logs/SkirmishProduction/s003-original-compiler-diagnostic-20261103-T1/run-6-seed-104732"
)
RUN6_RECEIPT_SHA256 = "3d518a67a576bf223d66fb88adf181fc893bfb890200d902948d4a1a36b13609"
RUN6_POST_RUN_AUDIT_SHA256 = "52b5e97a494dc4adf5e5159e65fbe5921f57ad90b730012c5d0698f820abeda9"
RUN6_GRAPH_SHA256 = "150cb503bb95dcdb1371e2c52c54764308b9bf1ad5e6305ae7ba3a68bf39cc5b"
RUN6_BUILD_RECEIPT_SHA256 = "703af14bff2585e237a819b13f257de95f85c1039c19c0ad51fc48d20caac50b"
REQUIRED_OWNER_SOURCES = {
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S003Metadata.cs",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S003Metadata.cs.meta",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.TwoSeedMetadata.cs",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.TwoSeedMetadata.cs.meta",
}
METHOD = "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS003MetadataCapture"
PASS_MARKER = (
    "[SkirmishS003MetadataCapture] result=Passed catalog=S003 seed=104732 "
    "scope=DataOnly native_context_qualified=false mission_ready=false"
)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def _read_json(path):
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_unique_object,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Invalid JSON number: " + value)))


def _verify_pin(repo, raw_path, expected_sha):
    path = Path(raw_path)
    source = path.resolve(strict=True) if path.is_absolute() else (repo / path).resolve(strict=True)
    if not source.is_relative_to(repo):
        raise ValueError("Source pin escapes repository: " + str(raw_path))
    if sha(source.read_bytes()) != expected_sha:
        raise ValueError("Capture source changed: " + str(raw_path))
    return source


def qualify(repo, directory, wrapper_exit):
    repo = Path(repo).resolve(strict=True)
    directory = Path(os.path.abspath(directory))
    expected_graph_path = (repo / GRAPH_RELATIVE_PATH).resolve(strict=True)
    capture_path = directory / "s003-native-metadata.json"
    log_path = directory.parent / "editor.log"
    graph_path = expected_graph_path

    graph_raw = graph_path.read_bytes()
    if sha(graph_raw) != GRAPH_SHA256:
        raise ValueError("Exact current S003 graph-r5 identity required")
    graph = json.loads(graph_raw.decode("utf-8", errors="strict"), object_pairs_hook=_unique_object)
    if graph.get("schema_version") != 1 or graph.get("authority") != "AuthoredConfigAssets":
        raise ValueError("Exact authored S003 graph required")

    run6_dir = repo / RUN6_RELATIVE_DIR
    run6_receipt_path = run6_dir / "receipt.json"
    run6_audit_path = run6_dir / "post-run-pin-audit.json"
    run6_receipt_raw = run6_receipt_path.read_bytes()
    run6_audit_raw = run6_audit_path.read_bytes()
    if sha(run6_receipt_raw) != RUN6_RECEIPT_SHA256 or sha(run6_audit_raw) != RUN6_POST_RUN_AUDIT_SHA256:
        raise ValueError("Exact run-6 compiler receipt and post-run audit required")
    run6 = json.loads(run6_receipt_raw.decode("utf-8", errors="strict"), object_pairs_hook=_unique_object)
    audit = json.loads(run6_audit_raw.decode("utf-8", errors="strict"), object_pairs_hook=_unique_object)
    run6_graph_descriptor = run6.get("source_graph")
    run6_graph_path = Path(run6_graph_descriptor.get("path", "")).resolve(strict=True)
    run6_graph_raw = run6_graph_path.read_bytes()
    run6_graph = json.loads(run6_graph_raw.decode("utf-8", errors="strict"), object_pairs_hook=_unique_object)
    if (run6.get("kind") != "S003OriginalCompilerHarnessReceipt" or run6.get("status") != "Compiled" or
            run6.get("seed") != 104732 or not isinstance(run6_graph_descriptor, dict) or
            run6_graph_descriptor.get("sha256") != RUN6_GRAPH_SHA256 or sha(run6_graph_raw) != RUN6_GRAPH_SHA256 or
            Path(run6_graph_descriptor.get("path", "")).resolve() != run6_graph_path or
            run6_graph.get("source_pins") != run6.get("source_graph_source_pins")):
        raise ValueError("Run-6 compiler provenance must bind its exact source graph and seed")
    run6_result = run6.get("result", {})
    run6_standard = [row for row in run6_result.get("setups", []) if row.get("size_id") == "Standard"]
    if (run6_result.get("kind") != "S003OriginalCompilerSourceOnlyResult" or
            run6_result.get("compiled") is not True or run6_result.get("catalog_id") != "S003" or
            run6_result.get("definition_id") != "skirmish.s003" or len(run6_standard) != 1 or
            run6_standard[0].get("seed") != 104732 or run6_standard[0].get("difficulty_id") != "Regular" or
            run6_standard[0].get("setup_hash") != EXPECTED_SETUP_HASH or
            run6_result.get("world_initialized") is not False or
            run6_result.get("physical_source_fit_verified") is not False or
            run6_result.get("mission_readiness") is not False):
        raise ValueError("Run-6 Standard/Regular S003 compiler target mismatch")
    run6_build = run6.get("source_build", {})
    build_receipt_path = Path(run6_build.get("path", "")).resolve(strict=True)
    if build_receipt_path != (repo / "Logs/SkirmishProduction/s004-import-rollback-source-build-20261103-r1/build.json").resolve():
        raise ValueError("Run-6 source-build receipt path mismatch")
    if sha(build_receipt_path.read_bytes()) != RUN6_BUILD_RECEIPT_SHA256 or run6_build.get("sha256") != RUN6_BUILD_RECEIPT_SHA256:
        raise ValueError("Run-6 pinned source-build receipt changed")
    if (audit.get("kind") != "S003CurrentSourceCompilerPostRunPinAudit" or audit.get("seed") != 104732 or
            audit.get("source_graph_sha256_expected") != RUN6_GRAPH_SHA256 or
            audit.get("source_graph_sha256_after") != RUN6_GRAPH_SHA256 or
            audit.get("source_graph_pin_mismatches") != [] or
            audit.get("source_build_receipt_sha256_expected") != RUN6_BUILD_RECEIPT_SHA256 or
            audit.get("source_build_receipt_sha256_after") != RUN6_BUILD_RECEIPT_SHA256 or
            audit.get("source_build_pin_mismatches") != [] or audit.get("world_initialized") is not False or
            audit.get("physical_source_fit_verified") is not False or audit.get("mission_readiness") is not False):
        raise ValueError("Run-6 post-run source pin audit failed")
    for path, expected in run6.get("source_graph_source_pins", {}).items():
        _verify_pin(repo, path, expected)
    if any(run6.get("source_graph_source_pins", {}).get(path) != expected
           for path, expected in graph.get("source_pins", {}).items()):
        raise ValueError("Current graph-r5 pins are not the exact subset of run-6 compiler graph pins")

    capture_raw = capture_path.read_bytes()
    capture = json.loads(capture_raw.decode("utf-8", errors="strict"), object_pairs_hook=_unique_object)
    log_raw = log_path.read_bytes()
    log = log_raw.decode("utf-8", errors="strict")
    marker_count = sum(line.endswith(PASS_MARKER) for line in log.splitlines())
    if (wrapper_exit != 0 or marker_count != 1 or "COMMAND LINE ARGUMENTS:" not in log or
            METHOD not in log or "-skirmishS003Metadata" not in log or "-skirmishS003Graph" not in log):
        raise ValueError("Successful checked wrapper, exact method, and unique pass marker required")

    expected_capture_path = (repo / CAPTURE_RELATIVE_PATH).resolve()
    if capture_path.resolve() != expected_capture_path:
        raise ValueError("Capture must be the designated current S003 data artifact")
    expected_log_path = (repo / CAPTURE_LOG_RELATIVE_PATH).resolve()
    if log_path.resolve() != expected_log_path:
        raise ValueError("Capture must use the designated Editor log")

    if (capture.get("schema_version") != 1 or
            capture.get("kind") != "NativeS003OriginalCompilerFactoryMetadataCapture" or
            capture.get("scope") != "CurrentPackagedS003CompilerAndOriginalMetadataFactories_DataOnly_NoMissionWorld" or
            capture.get("full_mission_initialized") is not False or
            capture.get("utility_world_created") is not True or
            capture.get("utility_world_disposed") is not True or
            capture.get("mission_world_created") is not False or
            capture.get("runtime_state_transferred") is not False or
            capture.get("source_fit_certified") is not False or
            capture.get("native_context_qualified") is not False or
            capture.get("mission_ready") is not False):
        raise ValueError("Exact data-only S003 scope and utility World disclosure required")

    graph_descriptor = capture.get("source_graph")
    if (not isinstance(graph_descriptor, dict) or graph_descriptor.get("sha256") != GRAPH_SHA256 or
            Path(graph_descriptor.get("path", "")).resolve() != graph_path.resolve()):
        raise ValueError("Capture must bind the exact current graph-r5 artifact")
    graph_pins = graph.get("source_pins")
    capture_pins = capture.get("source_pins")
    if not isinstance(graph_pins, dict) or not isinstance(capture_pins, dict):
        raise ValueError("Graph and capture source pin maps required")
    if capture_pins.get(str(graph_path)) != GRAPH_SHA256:
        raise ValueError("Native capture must pin graph-r5 itself")
    if not REQUIRED_OWNER_SOURCES.issubset(capture_pins):
        raise ValueError("Current S003 exporter and shared factory source pins required")
    for path, expected in graph_pins.items():
        if capture_pins.get(path) != expected:
            raise ValueError("Capture graph pin mismatch: " + path)
    for path, expected in capture_pins.items():
        if not isinstance(expected, str) or len(expected) != 64:
            raise ValueError("Malformed capture source pin: " + str(path))
        _verify_pin(repo, path, expected)

    catalog_path = "Assets/Game/Resources/SkirmishExpansionCatalog.asset"
    catalog_pin = capture_pins.get(catalog_path)
    if catalog_pin != EXPECTED_CATALOG_SHA256:
        raise ValueError("Exact current S003 catalog source pin required")

    context = capture.get("context")
    if (not isinstance(context, dict) or context.get("catalog_id") != "S003" or
            context.get("size") != "Standard" or context.get("difficulty") != "Regular" or
            context.get("seed") != 104732 or context.get("setup_hash") != EXPECTED_SETUP_HASH or
            context.get("wrong_context_hash_rejected") is not True or
            context.get("wrong_target_seed_rejected") is not True):
        raise ValueError("Exact run-6 current S003 compiler target and rejection controls required")
    metadata = context.get("metadata")
    if not isinstance(metadata, dict) or metadata.get("catalog_inputs", {}).get("catalog_id") != "S003":
        raise ValueError("Current S003 factory metadata required")
    metadata_digest = sha(json.dumps(metadata, separators=(",", ":"), ensure_ascii=False).encode("utf-8"))
    if context.get("metadata_key_sha256") != metadata_digest:
        raise ValueError("Native metadata digest does not match captured provider facts")
    providers = metadata.get("providers")
    if not isinstance(providers, dict) or not all(key in providers for key in
            ("baseline_asset", "scene_asset", "unit_registry_asset", "baseline", "scene", "units")):
        raise ValueError("Original Desert Base provider metadata required")
    for key in ("baseline_asset", "scene_asset", "unit_registry_asset"):
        identity = providers[key]
        if not isinstance(identity, dict) or not isinstance(identity.get("guid"), str) or \
                len(identity["guid"]) != 32 or type(identity.get("local_id")) is not int or identity["local_id"] == 0:
            raise ValueError("Complete original provider asset identity required: " + key)
    domain = context.get("selected_native_domain")
    if (not isinstance(domain, dict) or type(domain.get("building_count")) is not int or
            type(domain.get("unit_count")) is not int or not 1 <= domain["building_count"] <= 64 or
            not 1 <= domain["unit_count"] <= 64 or domain.get("building_count") != len(metadata.get("buildings", [])) or
            domain.get("unit_count") != len(metadata.get("units", [])) or
            domain.get("recipe_count") != len(metadata.get("recipes", [])) or
            domain.get("all_recipe_producers_and_units_in_selected_domain") is not True):
        raise ValueError("Finite current S003 native provider domain required")
    if not isinstance(context.get("metadata_key_sha256"), str) or len(context["metadata_key_sha256"]) != 64:
        raise ValueError("Native metadata digest required")

    return {
        "schema_version": 1,
        "kind": "S003OriginalCompilerFactoryMetadataCaptureReceipt",
        "validation_status": "Passed",
        "wrapper_exit_code": 0,
        "required_marker_count": marker_count,
        "capture_path": str(capture_path.resolve()),
        "capture_sha256": sha(capture_raw),
        "editor_log_path": str(log_path.resolve()),
        "editor_log_sha256": sha(log_raw),
        "graph_path": str(graph_path),
        "graph_sha256": GRAPH_SHA256,
        "run6_receipt_sha256": RUN6_RECEIPT_SHA256,
        "run6_post_run_audit_sha256": RUN6_POST_RUN_AUDIT_SHA256,
        "run6_graph_sha256": RUN6_GRAPH_SHA256,
        "catalog_sha256": EXPECTED_CATALOG_SHA256,
        "seed": 104732,
        "setup_hash": EXPECTED_SETUP_HASH,
        "building_count": domain["building_count"],
        "unit_count": domain["unit_count"],
        "recipe_count": domain["recipe_count"],
        "metadata_key_sha256": metadata_digest,
        "source_pin_count": len(capture_pins),
        "utility_world_created": True,
        "utility_world_disposed": True,
        "mission_world_created": False,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "mission_ready": False,
        "scope": "Original S003 compiler and metadata factories only; no mission world or readiness",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--directory", required=True, help="Capture data directory")
    parser.add_argument("--wrapper-exit", required=True, type=int)
    args = parser.parse_args()
    receipt = qualify(args.repo, args.directory, args.wrapper_exit)
    output = Path(os.path.abspath(args.directory)) / "s003-native-metadata-receipt.json"
    with output.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
