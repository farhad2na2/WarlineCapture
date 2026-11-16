#!/usr/bin/env python3
"""Project the exact current S003 factory capture into source-only consumer bundles.

This creates no Unity objects and grants no mission, native-context, fit, or
readiness authority. It preserves the capture's historical run-6 provenance
and independently binds a fresh current source build/compiler epoch.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import stat
import sys


ROOT = Path(__file__).resolve().parents[4]
CAPTURE_REL = Path("Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata.json")
CAPTURE_RECEIPT_REL = CAPTURE_REL.parent / "s003-native-metadata-receipt.json"
SELECTION_COMPILER_GRAPH_REL = Path("T Logs/SkirmishProduction/s003-source-graph-extensions-20261103-T26/source-selection/source-config-graph-with-selected-source-pins.json")
SOURCE_SELECTION_EXTENSION_REL = SELECTION_COMPILER_GRAPH_REL.parent / "extension-receipt.json"
CURRENT_COMPILER_GRAPH_REL = Path("T Logs/SkirmishProduction/s003-source-graph-extensions-20261103-T26/grid-surface/source-config-graph-with-grid-surface-nodes.json")
SOURCE_GRID_SURFACE_EXTENSION_REL = CURRENT_COMPILER_GRAPH_REL.parent / "extension-receipt.json"
BASE_COMPILER_GRAPH_REL = Path("T Logs/SkirmishProduction/s003-canonical-catalog-graph-augmentation-20261103-T1/source-config-graph-with-current-canonical-catalog.json")
FINALIZER_REL = Path("Tools/CI/SkirmishSourceSimulation/Tests/finalize_s003_native_metadata_capture.py")
COMPAT_REL = Path("Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceMetadataCompatibility.s003.cs")
EXPECTED = {
    "capture_sha256": "2f857f965acd8743339ee73cf032ddf54e4ba7f07f278f4cde50d1e1876e91d3",
    "capture_receipt_sha256": None,  # Bound by exact equality with the finalizer's freshly validated receipt.
    "graph_sha256": "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb",
    "run6_receipt_sha256": "3d518a67a576bf223d66fb88adf181fc893bfb890200d902948d4a1a36b13609",
    "run6_audit_sha256": "52b5e97a494dc4adf5e5159e65fbe5921f57ad90b730012c5d0698f820abeda9",
    "run6_graph_sha256": "150cb503bb95dcdb1371e2c52c54764308b9bf1ad5e6305ae7ba3a68bf39cc5b",
    "current_compiler_graph_sha256": "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f",
    "base_compiler_graph_sha256": "349bdd26f0326a57565effe515a848126a72384250a039ec8b8f266d69e9006e",
    "source_selection_extension_receipt_sha256": "68c6e6a3c4538837a11c78003479306db5db12368f4f84804bdfa9baa3924d69",
    "source_grid_surface_extension_receipt_sha256": "3bb17b71c7be1defb607ec3fef98ae947a8a8618cb19333b5b2284effc95ceaa",
    "source_graph_extension_producer_sha256": "cecdf23a24a68110eaa87f89e29354de2ed37b6cdc72cb92bed9fa834c0323d5",
    "selection_compiler_graph_sha256": "e498aa3cd7415419541c1a22dc1543fc66fe930c7c6bc17cae984e37de031c2e",
    "selection_owner_sha256": "47918cb5b8a3a7c37253d68ed1907744b3f2c86d7907c5fbba299aa1ff178cd6",
    "selection_owner_build_sha256": "96761c096b6f1ad213bc31c573499029584dbba1f2273ed3de04bd15fb4a24a8",
    "catalog_sha256": "e206b57a16d0876d504dab66cf5f8f93de13fa96844c5b2e30a234637885e6a8",
    "battle_catalog_sha256": "e5d37c633b63d2ad706cfaf35eb18598377d38ca47eebba70ffe59a01f9e1340",
    "seed": 104732,
    "setup_hash": 2394754492,
    "metadata_key_sha256": "2df9f3e2140f9e8146311e5d5dd5c5c94c99b016e55a1da14968c863cb29e9ae",
    "counts": {"buildings": 10, "units": 19, "recipes": 19, "transports": 1},
}


def validate_augmented_compiler_graph(graph: dict, expected_sha256: str) -> None:
    if expected_sha256 != EXPECTED["current_compiler_graph_sha256"]:
        raise ValueError("Current compiler graph must use the exact selector-extended canonical-catalog identity")
    augmentation = graph.get("canonical_catalog_augmentation")
    audit = graph.get("catalog_membership_audit")
    provenance = graph.get("catalog_closure_provenance")
    nodes = graph.get("nodes")
    pins = graph.get("source_pins")
    if (graph.get("authority") != "AuthoredConfigAssets" or graph.get("compiled_setup") is not False or
            graph.get("world_initialized") is not False or graph.get("native_world_state_used") is not False or
            graph.get("definition_path") != "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset" or
            not isinstance(augmentation, dict) or augmentation.get("path") != "Assets/Game/Resources/SkirmishBattleCatalog.asset" or
            augmentation.get("asset_guid") != "57f18ca43b8ca4fa48aee0224cc72328" or
            augmentation.get("resource_key") != "SkirmishBattleCatalog" or
            augmentation.get("source_sha256") != EXPECTED["battle_catalog_sha256"] or
            not isinstance(provenance, dict) or provenance.get("base_s003_graph_sha256") != EXPECTED["graph_sha256"] or
            provenance.get("catalog_source_sha256") != EXPECTED["catalog_sha256"] or
            provenance.get("overlapping_nodes_identical") is not True or provenance.get("overlapping_pins_identical") is not True or
            provenance.get("all_imported_sources_current_at_extension") is not True or
            not isinstance(audit, dict) or audit.get("scenario_asset_guid") != "7f2c9e14b6a84d0e8f31c5a9d2b70418" or
            audit.get("matching_direct_definition_references") != 1 or
            not isinstance(augmentation.get("catalog_membership_audit"), dict) or
            augmentation["catalog_membership_audit"].get("scenario_id") != "S003" or
            augmentation["catalog_membership_audit"].get("matches") != 1 or
            augmentation["catalog_membership_audit"].get("row", {}).get("DefinitionId") != "skirmish.s003" or
            augmentation["catalog_membership_audit"]["row"].get("ContentVersion") != 2 or
            augmentation["catalog_membership_audit"]["row"].get("ScenarioId") != "S003" or
            not isinstance(nodes, list) or not any(isinstance(n, dict) and n.get("source_path") == augmentation["path"] and n.get("source_sha256") == EXPECTED["battle_catalog_sha256"] for n in nodes) or
            not isinstance(pins, dict) or len(pins) != 147 or pins.get(str(ROOT / augmentation["path"])) != EXPECTED["battle_catalog_sha256"]):
        raise ValueError("Exact final S003 grid/surface runtime graph with actual BattleCatalog membership is required")


def validate_extension_producer(root: Path, receipt: dict) -> None:
    expected = {"path": "Tools/CI/SkirmishSourceSimulation/Recipes/extend_s003_current_source_graph.py",
                "sha256": EXPECTED["source_graph_extension_producer_sha256"]}
    if receipt.get("producer") != expected:
        raise ValueError("Exact T26 source graph extension producer provenance required")
    if _sha_file(_regular_under_root(root, expected["path"])) != expected["sha256"]:
        raise ValueError("Current S003 source graph extension producer bytes differ from T26 provenance")


def validate_source_selection_extension(root: Path, graph: dict, graph_path: Path) -> dict:
    """Prove the current runtime graph is the exact strict selector extension."""
    root = root.resolve(strict=True)
    graph_path = _regular_under_root(root, str(graph_path))
    extension_path = _regular_under_root(root, str(root / SOURCE_SELECTION_EXTENSION_REL))
    base_path = _regular_under_root(root, str(root / BASE_COMPILER_GRAPH_REL))
    receipt_raw = extension_path.read_bytes()
    receipt = _json(extension_path)
    base = _json(base_path)
    base_sha = _sha_file(base_path)
    graph_sha = _sha_file(graph_path)
    if (set(receipt) != {"schema_version", "kind", "status", "producer", "base_graph", "output_graph", "added_nodes", "added_pins", "generated_owner", "direct_reference_audit", "negative_controls", "scope"} or
            base_sha != EXPECTED["base_compiler_graph_sha256"] or graph_sha != EXPECTED["selection_compiler_graph_sha256"] or
            _sha_bytes(receipt_raw) != EXPECTED["source_selection_extension_receipt_sha256"] or
            receipt.get("kind") != "S003SourceSelectionGraphExtensionReceipt" or
            receipt.get("status") != "PassedStrictSourceEvidenceExtension" or
            receipt.get("base_graph") != {"path": str(BASE_COMPILER_GRAPH_REL), "sha256": base_sha, "nodes": 33, "pins": 123} or
            receipt.get("output_graph") != {"path": str(SELECTION_COMPILER_GRAPH_REL), "sha256": graph_sha, "nodes": 35, "pins": 143}):
        raise ValueError("Exact strict S003 source-selection graph extension receipt required")
    validate_extension_producer(root, receipt)
    added_nodes, added_pins = receipt.get("added_nodes"), receipt.get("added_pins")
    if not isinstance(added_nodes, list) or len(added_nodes) != 2 or not isinstance(added_pins, dict) or len(added_pins) != 20:
        raise ValueError("Exact S003 selector extension node and pin additions required")
    if any(n not in graph.get("nodes", []) for n in added_nodes):
        raise ValueError("S003 selector extension nodes differ from current compiler graph")
    base_nodes, graph_nodes = base.get("nodes"), graph.get("nodes")
    node_key = lambda item: json.dumps(item, sort_keys=True, separators=(",", ":"))
    if (not isinstance(base_nodes, list) or not isinstance(graph_nodes, list) or len(base_nodes) != 33 or len(graph_nodes) != 35 or
            any(sum(node_key(n) == node_key(candidate) for candidate in graph_nodes) != 1 for n in base_nodes) or
            sorted(node_key(n) for n in graph_nodes if all(node_key(n) != node_key(base_node) for base_node in base_nodes)) != sorted(node_key(n) for n in added_nodes)):
        raise ValueError("Current compiler graph is not the exact base plus selector nodes")
    base_pins, graph_pins = base.get("source_pins"), graph.get("source_pins")
    if not isinstance(base_pins, dict) or not isinstance(graph_pins, dict) or len(base_pins) != 123 or len(graph_pins) != 143:
        raise ValueError("Current compiler graph base/extension pin counts changed")
    merged_pins = dict(base_pins)
    for path, digest in added_pins.items():
        if path in merged_pins:
            raise ValueError("S003 selector extension duplicates an existing source pin")
        merged_pins[path] = digest
    if merged_pins != graph_pins:
        raise ValueError("Current compiler graph is not the exact base plus selector pins")
    for key in base:
        if key not in ("nodes", "source_pins") and base[key] != graph.get(key):
            raise ValueError("S003 selector extension changed unrelated graph field: " + key)
    if receipt.get("negative_controls") != [
            {"case": "tampered-source-bytes", "result": "RejectedAsExpected"},
            {"case": "duplicate-required-path", "result": "AmbiguousAsExpected"},
            {"case": "lexical-path-escape", "result": "RejectedAsExpected"}]:
        raise ValueError("S003 selector extension negative controls are missing")
    owner = receipt.get("generated_owner", {})
    owner_path = _regular_under_root(root, owner.get("path", ""))
    build_path = _regular_under_root(root, owner.get("build_receipt_path", ""))
    build = _json(build_path)
    owner_sha = _sha_file(owner_path)
    owner_pin = build.get("source_and_reference_pins", {}).get(str(owner_path))
    required_assets = receipt.get("generated_owner", {}).get("required_assets")
    owner_text = owner_path.read_text(encoding="utf-8")
    block = re.search(r"RequiredAssets\s*=\s*\{(.*?)\};", owner_text, re.S)
    actual_assets = dict(re.findall(r'\{"([^"\n]+)","([0-9a-f]{64})"\}', block.group(1))) if block else {}
    if (owner_sha != EXPECTED["selection_owner_sha256"] or owner_sha != owner.get("sha256") or owner_pin != owner_sha or
            owner.get("build_receipt_sha256") != EXPECTED["selection_owner_build_sha256"] or
            _sha_file(build_path) != owner.get("build_receipt_sha256") or
            build.get("status") != "Passed" or build.get("mission") != "S003" or
            owner.get("build_receipt_source_pin_matches") is not True or
            required_assets != actual_assets or len(actual_assets) != 8):
        raise ValueError("S003 selector extension owner/build/RequiredAssets source proof changed")
    for raw_path, digest in graph_pins.items():
        verify_pins(root, {raw_path: digest})
    return {"path": str(extension_path), "sha256": _sha_bytes(receipt_raw)}


def validate_source_grid_surface_extension(root: Path, graph: dict, graph_path: Path) -> dict:
    """Prove the final runtime graph is the exact strict e498→3419 extension."""
    root = root.resolve(strict=True)
    graph_path = _regular_under_root(root, str(graph_path))
    selection_path = _regular_under_root(root, str(root / SELECTION_COMPILER_GRAPH_REL))
    receipt_path = _regular_under_root(root, str(root / SOURCE_GRID_SURFACE_EXTENSION_REL))
    selection_graph = _json(selection_path)
    receipt_raw = receipt_path.read_bytes()
    receipt = _json(receipt_path)
    graph_sha = _sha_file(graph_path)
    selection_sha = _sha_file(selection_path)
    expected_added_pins = {
        str(root / "Assets/Game/Scripts/Configs/Scene/GridAuthoringSceneConfigAsset.cs"): "560e522fb8ebda28b84bf962a72187319b1f63710983198fca32c0bc56097171",
        str(root / "Assets/Game/Scripts/Configs/Scene/GridAuthoringSceneConfigAsset.cs.meta"): "1bf5609452c76fa68ec8ce8cf9a7da476be877014de6d58de52f9cee55a4b593",
        str(root / "Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs"): "0e5626d0856ae274b3753ea8ddccaf969af5e4f63cc0009eabc977776cac4a31",
        str(root / "Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs.meta"): "56b2179f35aa4fb364e7f4e7d261af900b6f7e153461d0fb0660eaa55e244006",
    }
    expected_negative_controls = [
        {"case": "tampered-asset-bytes", "result": "RejectedAsExpected"},
        {"case": "script-guid-class-type-mismatch", "result": "RejectedAsExpected"},
        {"case": "source-backed-class-outside-game-configs", "result": "RejectedAsExpected"},
        {"case": "malformed-packed-surface-payload", "result": "RejectedAsExpected"},
        {"case": "duplicate-guid-target", "result": "AmbiguousAsExpected"},
        {"case": "lexical-path-escape", "result": "RejectedAsExpected"},
    ]
    parser = receipt.get("parser_provenance", {})
    reader = parser.get("strict_yaml_reader", {})
    generator = parser.get("typed_graph_generator_helpers", {})
    added_nodes, added_pins = receipt.get("added_nodes"), receipt.get("added_source_pins")
    if (set(receipt) != {"schema_version", "kind", "status", "producer", "base_graph", "output_graph", "added_nodes", "added_source_pins", "negative_controls", "source_pin_audit", "materializer_bounds", "parser_provenance", "scope"} or
            selection_sha != EXPECTED["selection_compiler_graph_sha256"] or graph_sha != EXPECTED["current_compiler_graph_sha256"] or
            _sha_bytes(receipt_raw) != EXPECTED["source_grid_surface_extension_receipt_sha256"] or
            receipt.get("kind") != "S003GridSurfaceSourceGraphExtensionReceipt" or receipt.get("status") != "PassedStrictSourceOnlyExtension" or
            receipt.get("base_graph") != {"path": str(SELECTION_COMPILER_GRAPH_REL), "sha256": selection_sha, "node_count": 35, "source_pin_count": 143} or
            receipt.get("output_graph") != {"path": str(CURRENT_COMPILER_GRAPH_REL), "sha256": graph_sha, "node_count": 37, "source_pin_count": 147, "serialized_bytes": 3534510} or
            not isinstance(added_nodes, list) or len(added_nodes) != 2 or added_pins != expected_added_pins or
            receipt.get("negative_controls") != expected_negative_controls or
            receipt.get("source_pin_audit") != {"all_base_pins_current": True, "all_output_pins_current": True, "base_nodes_unchanged": True, "exactly_two_nodes_and_four_pins_added": True} or
            receipt.get("materializer_bounds") != {"output_bytes_under_16MiB": True, "node_count_under_64": True, "source_file_max_4MiB": True} or
            reader != {"path": "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb", "sha256": "daf293636c19a83fc3281e0a0161c2d4235737612c8d89257d5fa26d07db216f", "duplicate_keys_aliases_rejected": True} or
            generator.get("path") != "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py" or
            generator.get("sha256") != "50f0610c376cfec013bc374087a2f74d7ec8656b7890d421aac3e63af1c2446f" or
            generator.get("functions") != ["strict_read", "guid_hits", "checked_meta_target", "checked_asset_path", "unity_anchor"]):
        raise ValueError("Exact strict S003 grid/surface graph extension receipt required")
    validate_extension_producer(root, receipt)
    base_nodes, output_nodes = selection_graph.get("nodes"), graph.get("nodes")
    base_pins, output_pins = selection_graph.get("source_pins"), graph.get("source_pins")
    if (not isinstance(base_nodes, list) or not isinstance(output_nodes, list) or len(base_nodes) != 35 or len(output_nodes) != 37 or
            not isinstance(base_pins, dict) or not isinstance(output_pins, dict) or len(base_pins) != 143 or len(output_pins) != 147):
        raise ValueError("S003 grid/surface graph base or output counts changed")
    node_key = lambda item: json.dumps(item, sort_keys=True, separators=(",", ":"))
    expected_nodes = {
        ("GridAuthoringSceneConfigAsset", "b201000000000000000000000000000b", "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset", "8ef1b3f17074774040111a48ea82901b3355da8b8b86c8dc5c6e2a0bcccc2cfb"),
        ("MapSurfaceDataAsset", "12f517deb32ab49698acbfdaf7c3eac7", "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset", "5447c779e2b68ba6b7c6f86a1146f6bf570d49b4455a676a2680ea595fa47d1d"),
    }
    extras = [n for n in output_nodes if all(node_key(n) != node_key(b) for b in base_nodes)]
    actual_nodes = {(n.get("source_type"), n.get("asset_guid"), n.get("source_path"), n.get("source_sha256")) for n in extras if isinstance(n, dict)}
    receipt_nodes = {(n.get("source_type"), n.get("asset_guid"), n.get("asset_path"), n.get("asset_sha256")) for n in added_nodes if isinstance(n, dict)}
    if (any(sum(node_key(n) == node_key(candidate) for candidate in output_nodes) != 1 for n in base_nodes) or
            actual_nodes != expected_nodes or receipt_nodes != expected_nodes or len(extras) != 2):
        raise ValueError("S003 final graph does not contain exactly the two expected grid/surface source nodes")
    merged_pins = dict(base_pins)
    for path, digest in added_pins.items():
        if path in merged_pins:
            raise ValueError("S003 grid/surface extension duplicates an existing source pin")
        merged_pins[path] = digest
    if merged_pins != output_pins:
        raise ValueError("S003 final graph is not exactly the e498 graph plus four grid/surface code pins")
    for key in selection_graph:
        if key not in ("nodes", "source_pins") and selection_graph[key] != graph.get(key):
            raise ValueError("S003 grid/surface extension changed unrelated graph field: " + key)
    for raw_path, digest in output_pins.items():
        verify_pins(root, {raw_path: digest})
    for row in added_nodes:
        for path_key, sha_key in (("asset_path", "asset_sha256"), ("asset_meta_path", "asset_meta_sha256"),
                                  ("script_path", "script_sha256"), ("script_meta_path", "script_meta_sha256")):
            raw_path, expected = row.get(path_key), row.get(sha_key)
            if not isinstance(raw_path, str) or not isinstance(expected, str) or _sha_file(_regular_under_root(root, raw_path)) != expected:
                raise ValueError("S003 grid/surface extension source object changed: " + str(raw_path))
        if row.get("serialized_reference_count") != 0 or type(row.get("field_count")) is not int:
            raise ValueError("Unexpected serialized grid/surface source node references")
    if not any(row.get("source_type") == "GridAuthoringSceneConfigAsset" and row.get("field_count") == 28 and row.get("surface_payload_hex_chars") is None for row in added_nodes):
        raise ValueError("Exact grid authoring scene source node required")
    if not any(row.get("source_type") == "MapSurfaceDataAsset" and row.get("field_count") == 10 and row.get("surface_payload_hex_chars") == 1533400 for row in added_nodes):
        raise ValueError("Exact packed map surface source node required")
    for rel, expected in ((reader["path"], reader["sha256"]), (generator["path"], generator["sha256"])):
        if _sha_file(_regular_under_root(root, rel)) != expected:
            raise ValueError("S003 grid/surface parser provenance changed: " + rel)
    return {"path": str(receipt_path), "sha256": _sha_bytes(receipt_raw)}


def validate_source_graph_extensions(root: Path, graph: dict, graph_path: Path) -> tuple[dict, dict]:
    root = root.resolve(strict=True)
    selection_path = _regular_under_root(root, str(root / SELECTION_COMPILER_GRAPH_REL))
    selection_graph = _json(selection_path)
    selection_receipt = validate_source_selection_extension(root, selection_graph, selection_path)
    grid_receipt = validate_source_grid_surface_extension(root, graph, graph_path)
    return selection_receipt, grid_receipt


def _pairs(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def _json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=_pairs,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Invalid JSON number: " + value)))


def _sha_bytes(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def _sha_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def _regular_under_root(root: Path, raw_path: str) -> Path:
    candidate = Path(raw_path)
    if not candidate.is_absolute():
        candidate = root / candidate
    lexical = Path(os.path.abspath(candidate))
    if not lexical.is_relative_to(root):
        raise ValueError("S003 capture pin escapes repository: " + raw_path)
    cursor = root
    for part in lexical.relative_to(root).parts:
        cursor = cursor / part
        if cursor.is_symlink():
            raise ValueError("Symlink in S003 capture pin: " + str(cursor))
    resolved = lexical.resolve(strict=True)
    if not resolved.is_relative_to(root) or not resolved.is_file():
        raise ValueError("S003 capture pin is not a repository file: " + raw_path)
    return resolved


def verify_pins(root: Path, pins: dict[str, str]) -> None:
    if not isinstance(pins, dict) or not pins:
        raise ValueError("Exact non-empty native source pin map required")
    for raw_path, expected in pins.items():
        if not isinstance(raw_path, str) or not isinstance(expected, str) or not re.fullmatch(r"[0-9a-f]{64}", expected):
            raise ValueError("Malformed native source pin")
        path = _regular_under_root(root, raw_path)
        if _sha_file(path) != expected:
            raise ValueError("S003 native source pin changed: " + raw_path)


def verify_build_pins(root: Path, pins: dict[str, str]) -> None:
    if not isinstance(pins, dict) or len(pins) != 3891:
        raise ValueError("Exact current S003 source-build pin closure required")
    for raw_path, expected in pins.items():
        if not isinstance(raw_path, str) or not isinstance(expected, str) or not re.fullmatch(r"[0-9a-f]{64}", expected):
            raise ValueError("Malformed current S003 build pin")
        candidate = Path(raw_path)
        if not candidate.is_absolute():
            candidate = root / candidate
        if candidate.is_symlink() or not candidate.is_file() or _sha_file(candidate) != expected:
            raise ValueError("Current S003 source-build pin mismatch: " + raw_path)


def validate_current_compiler_epoch(root: Path, compiler_receipt_path: Path, audit_path: Path,
                                    build_path: Path, graph_path: Path) -> dict:
    paths = [Path(p) for p in (compiler_receipt_path, audit_path, build_path, graph_path)]
    if any(not p.is_absolute() for p in paths):
        raise ValueError("Explicit absolute current S003 compiler proof paths required")
    receipt_path, audit_path, build_path, graph_path = paths
    for path in (receipt_path, audit_path, build_path, graph_path):
        try:
            resolved = _regular_under_root(root, str(path))
        except (OSError, ValueError):
            raise ValueError("Current S003 compiler epoch evidence missing or escaped repository: " + str(path))
        if resolved != path.resolve(strict=True):
            raise ValueError("Current S003 compiler epoch evidence missing: " + str(path))
    hashes = {"current_compiler_receipt_sha256": _sha_file(receipt_path),
              "current_compiler_audit_sha256": _sha_file(audit_path),
              "current_build_receipt_sha256": _sha_file(build_path),
              "current_graph_closure_sha256": _sha_file(graph_path)}
    if hashes["current_graph_closure_sha256"] != EXPECTED["current_compiler_graph_sha256"]:
        raise ValueError("Current augmented S003 compiler graph closure identity changed")
    receipt, audit, build, graph = (_json(p) for p in (receipt_path, audit_path, build_path, graph_path))
    selection_extension_receipt, grid_surface_extension_receipt = validate_source_graph_extensions(root, graph, graph_path)
    if receipt.get("kind") != "S003OriginalCompilerHarnessReceipt" or receipt.get("status") != "Compiled" or receipt.get("seed") != EXPECTED["seed"]:
        raise ValueError("Fresh current S003 original compiler receipt required")
    if receipt.get("source_build") != {"path": str(build_path.resolve()), "sha256": hashes["current_build_receipt_sha256"]}:
        raise ValueError("Current S003 compiler does not bind the exact fresh source build")
    expected_setups = [
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "Standard", "difficulty_id": "Regular", "setup_hash": 2394754492, "compiled": True},
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "War", "difficulty_id": "Regular", "setup_hash": 2291016159, "compiled": True},
        {"catalog_id": "S003", "definition_id": "skirmish.s003", "content_version": 2, "seed": 104732, "size_id": "LargeWar", "difficulty_id": "Regular", "setup_hash": 539080277, "compiled": True},
    ]
    if receipt.get("result", {}).get("compiled") is not True or receipt.get("result", {}).get("setups") != expected_setups:
        raise ValueError("Exact current S003 three-size compiler result required")
    if receipt.get("source_graph") != {"path": str(graph_path.resolve()), "sha256": EXPECTED["current_compiler_graph_sha256"]} or receipt.get("source_graph_source_pins") != graph.get("source_pins"):
        raise ValueError("Current compiler graph closure differs from the pinned current source closure")
    validate_augmented_compiler_graph(graph, hashes["current_graph_closure_sha256"])
    if build.get("kind") != "SkirmishSourceSimulationBuild" or build.get("mission") != "S002" or build.get("status") != "Passed" or build.get("source_count") != 729 or build.get("compile_exit") != 0:
        raise ValueError("Fresh canonical S002 source-host build required")
    verify_build_pins(root, build.get("source_and_reference_pins"))
    verify_pins(root, graph.get("source_pins"))
    if audit.get("kind") != "S003CurrentFactoryEpochCompilerPinAudit" or audit.get("status") != "Passed" or audit.get("compiler_receipt") != {"path": str(receipt_path.resolve()), "sha256": hashes["current_compiler_receipt_sha256"]} or audit.get("source_build") != {"path": str(build_path.resolve()), "sha256": hashes["current_build_receipt_sha256"]} or audit.get("source_graph") != {"path": str(graph_path.resolve()), "sha256": hashes["current_graph_closure_sha256"]}:
        raise ValueError("Independent current S003 compiler pin audit identity mismatch")
    capture_path = root / CAPTURE_REL
    if audit.get("capture") != {"path": str(capture_path.resolve()), "sha256": EXPECTED["capture_sha256"]}:
        raise ValueError("Current compiler audit does not bind exact current capture")
    expected_checks = {key: [] for key in ("build_pins", "compiler_graph_pins", "graph_pins", "capture_pins", "overlap_mismatches")}
    if audit.get("checks") != expected_checks or audit.get("source_build_pin_count") != len(build["source_and_reference_pins"]) or audit.get("compiler_graph_pin_count") != len(graph["source_pins"]) or audit.get("source_graph_pin_count") != len(graph["source_pins"]) or audit.get("shared_compiler_graph_pin_count") != len(graph["source_pins"]):
        raise ValueError("Current S003 compiler pin audit does not prove complete rehash")
    return {"compiler_receipt_path": str(receipt_path), "compiler_receipt_sha256": hashes["current_compiler_receipt_sha256"],
            "compiler_audit_path": str(audit_path), "compiler_audit_sha256": hashes["current_compiler_audit_sha256"],
            "build_path": str(build_path), "build_sha256": hashes["current_build_receipt_sha256"],
            "graph_closure_path": str(graph_path), "graph_closure_sha256": hashes["current_graph_closure_sha256"],
            "source_build_pin_count": len(build["source_and_reference_pins"]),
            "compiler_graph_pin_count": len(receipt["source_graph_source_pins"]),
            "source_selection_extension_receipt": selection_extension_receipt,
            "source_grid_surface_extension_receipt": grid_surface_extension_receipt,
            "setups": expected_setups}


def _load_finalizer(root: Path):
    path = root / FINALIZER_REL
    spec = importlib.util.spec_from_file_location("s003_current_capture_finalizer", path)
    if spec is None or spec.loader is None:
        raise ValueError("S003 native capture finalizer unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def validate_current_evidence(root: Path, compiler_receipt_path: Path, audit_path: Path,
                              build_path: Path, graph_path: Path) -> dict:
    root = root.resolve(strict=True)
    capture_path = root / CAPTURE_REL
    receipt_path = root / CAPTURE_RECEIPT_REL
    if not capture_path.is_file() or capture_path.is_symlink() or not receipt_path.is_file() or receipt_path.is_symlink():
        raise ValueError("Exact current S003 capture and receipt files required")

    finalizer = _load_finalizer(root)
    expected_receipt = finalizer.qualify(root, capture_path.parent, 0)
    receipt_raw = receipt_path.read_bytes()
    stored_receipt = json.loads(receipt_raw.decode("utf-8", errors="strict"), object_pairs_hook=_pairs)
    if stored_receipt != expected_receipt:
        raise ValueError("Stored S003 qualification receipt differs from current capture/finalizer evidence")
    if _sha_file(capture_path) != EXPECTED["capture_sha256"]:
        raise ValueError("Unexpected current S003 capture identity")
    if expected_receipt.get("capture_sha256") != EXPECTED["capture_sha256"]:
        raise ValueError("Qualified capture hash differs from the expected r1 artifact")
    capture = _json(capture_path)
    verify_pins(root, capture.get("source_pins"))
    current_compiler = validate_current_compiler_epoch(root, compiler_receipt_path, audit_path, build_path, graph_path)
    audit = _json(Path(current_compiler["compiler_audit_path"]))
    if audit.get("capture", {}).get("sha256") != EXPECTED["capture_sha256"] or audit.get("capture_pin_count") != len(capture["source_pins"]) or audit.get("checks", {}).get("capture_pins") != []:
        raise ValueError("Current compiler audit does not bind/recheck the current native capture closure")
    return {
        "capture_path": str(capture_path),
        "capture_sha256": EXPECTED["capture_sha256"],
        "capture_receipt_path": str(receipt_path),
        "capture_receipt_sha256": _sha_bytes(receipt_raw),
        "qualification_receipt": expected_receipt,
        "finalizer_path": str(root / FINALIZER_REL),
        "finalizer_sha256": _sha_file(root / FINALIZER_REL),
        "current_compiler": current_compiler,
    }


def read_factory_sources(root: Path) -> list[str]:
    path = root / COMPAT_REL
    text = path.read_text(encoding="utf-8")
    matches = re.findall(r"\bFactorySources\s*=\s*\{([^}]+)\}", text, re.S)
    if len(matches) != 1:
        raise ValueError("One current S003 FactorySources list required")
    body = matches[0]
    values = re.findall(r'"([^"\n]+)"', body)
    if not values or re.sub(r'"[^"\n]+"|[\s,]', "", body):
        raise ValueError("FactorySources must remain a literal path list")
    return ["Assets/Game/Scripts/" + value for value in values]


def _identity(row: dict, label: str) -> tuple[str, dict]:
    if not isinstance(row, dict) or type(row.get("index")) is not int:
        raise ValueError("Indexed S003 " + label + " row required")
    prefab = row.get("prefab")
    if not isinstance(prefab, dict) or prefab.get("codec") != "source-asset-reference-v1":
        raise ValueError("Exact source prefab identity required: " + label)
    key = prefab.get("key")
    if not isinstance(key, str) or not key or prefab.get("type") != "UnityEngine.GameObject":
        raise ValueError("Exact GameObject prefab key required: " + label)
    if not isinstance(prefab.get("path"), str) or not re.fullmatch(r"[0-9a-f]{32}", str(prefab.get("guid", ""))) or type(prefab.get("local_id")) is not int or prefab["local_id"] == 0:
        raise ValueError("Complete prefab asset identity required: " + label)
    return key, prefab


def project_payload(capture: dict, capture_sha: str, source_pins: dict[str, str], factory_sources: list[str],
                    expected_capture_sha: str | None = None) -> dict[str, dict]:
    if expected_capture_sha is not None and expected_capture_sha != "7d039dd9a09fdff34e88682be17ce648b14dc926637915ec5748b243d1de2433":
        raise ValueError("Unrecognized refreshed S003 capture")
    if capture_sha != (EXPECTED["capture_sha256"] if expected_capture_sha is None else expected_capture_sha):
        raise ValueError("Exact admitted S003 native capture hash required")
    return _project_payload_checked(capture, capture_sha, source_pins, factory_sources)


def _project_payload_checked(capture: dict, capture_sha: str, source_pins: dict[str, str],
                             factory_sources: list[str]) -> dict[str, dict]:
    """Project after the caller has independently verified the capture authority."""
    if capture.get("kind") != "NativeS003OriginalCompilerFactoryMetadataCapture" or capture.get("schema_version") != 1:
        raise ValueError("Exact current S003 original factory capture required")
    if capture.get("scope") != "CurrentPackagedS003CompilerAndOriginalMetadataFactories_DataOnly_NoMissionWorld":
        raise ValueError("S003 capture scope changed")
    for flag in ("full_mission_initialized", "mission_world_created", "runtime_state_transferred", "source_fit_certified", "native_context_qualified", "mission_ready"):
        if capture.get(flag) is not False:
            raise ValueError("S003 capture must retain false scope flag: " + flag)
    context = capture.get("context")
    if not isinstance(context, dict) or (context.get("catalog_id"), context.get("size"), context.get("difficulty"), context.get("seed"), context.get("setup_hash")) != ("S003", "Standard", "Regular", EXPECTED["seed"], EXPECTED["setup_hash"]):
        raise ValueError("Exact current S003 Standard/Regular compiler context required")
    if context.get("wrong_context_hash_rejected") is not True or context.get("wrong_target_seed_rejected") is not True:
        raise ValueError("S003 capture rejection controls required")
    metadata = context.get("metadata")
    required_metadata = {"providers", "catalog_inputs", "recipes", "delivery_seconds", "buildings", "units", "transports"}
    if not isinstance(metadata, dict) or set(metadata) != required_metadata:
        raise ValueError("Exact S003 factory metadata sections required")
    metadata_sha = _sha_bytes(json.dumps(metadata, separators=(",", ":"), ensure_ascii=False).encode("utf-8"))
    if context.get("metadata_key_sha256") != metadata_sha or not re.fullmatch(r"[0-9a-f]{64}", str(context.get("metadata_key_sha256", ""))):
        raise ValueError("S003 native metadata digest mismatch")
    if metadata["catalog_inputs"].get("catalog_id") != "S003":
        raise ValueError("S003 catalog metadata input identity required")
    domain = context.get("selected_native_domain")
    if not isinstance(domain, dict) or domain.get("building_count") != len(metadata["buildings"]) or domain.get("unit_count") != len(metadata["units"]) or domain.get("recipe_count") != len(metadata["recipes"]) or domain.get("all_recipe_producers_and_units_in_selected_domain") is not True:
        raise ValueError("Exact bounded native S003 provider domain required")
    for section, expected_count in EXPECTED["counts"].items():
        if not isinstance(metadata.get(section), list) or len(metadata[section]) != expected_count:
            raise ValueError("Unexpected S003 factory domain size: " + section)

    origin = {key: context[key] for key in ("catalog_id", "size", "difficulty", "seed", "setup_hash", "metadata_key_sha256")}
    origin.update(kind=capture["kind"], artifact_sha256=capture_sha)
    common = {
        "schema_version": 2,
        "authority": "OriginalNativeMetadataFactories_NotMissionState",
        "catalog_id": "S003",
        "setup_hash": EXPECTED["setup_hash"],
        "seed": EXPECTED["seed"],
        "native_origin": origin,
        "unsupported_values": [],
        "full_mission_initialized": False,
        "native_visual_readiness": False,
    }

    buildings = metadata["buildings"]
    definition_rows = []
    building_keys = set()
    for i, row in enumerate(buildings):
        key, prefab = _identity(row, "building")
        if row["index"] != i or key in building_keys or not isinstance(row.get("fields"), dict):
            raise ValueError("S003 building order, uniqueness, or fields changed")
        building_keys.add(key)
        definition_rows.append({"key": key, "prefab": prefab, "fields": row["fields"]})
    definitions = dict(common)
    definitions.update(kind="NativeQualifiedSkirmishBuildingDefinitions", definitions=definition_rows, source_pins=source_pins)

    units = metadata["units"]
    unit_rows = []
    unit_keys = set()
    for i, row in enumerate(units):
        key, _ = _identity(row, "unit")
        if row["index"] != i or key in unit_keys:
            raise ValueError("S003 unit order or identity changed")
        unit_keys.add(key)
        unit_rows.append(dict(row, key=key))
    unit_projection = dict(common)
    unit_projection.update(kind="NativeQualifiedSkirmishUnitProductionMetadata",
                           baked_actor_footprint_substituted=False,
                           runtime_camera_basis_exported=False,
                           source_pins=source_pins,
                           transports=metadata["transports"],
                           units=unit_rows,
                           ui_catalog_runtime_policy_sampled=False)

    configured_rows = []
    for i, row in enumerate(buildings):
        key, prefab = _identity(row, "configured building")
        entry = row.get("configured_entry", {}).get("fields", {})
        can_request = entry.get("CanRequest", {}).get("value")
        display_name = entry.get("DisplayName", {}).get("value")
        if type(can_request) is not bool or not isinstance(display_name, str):
            raise ValueError("Actual configured building request witness required: " + key)
        construction = row.get("construction_index")
        scene = row.get("scene_index")
        if type(construction) is not int or type(scene) is not int:
            raise ValueError("Actual S003 provider indices required: " + key)
        configured_rows.append({
            "definition_index": i,
            "key": key,
            "prefab": prefab,
            "display_name": display_name,
            "can_request": can_request,
            "construction_catalog_index": construction if construction >= 0 else None,
            "scene_catalog_index": scene if scene >= 0 else None,
        })
    configured_source_pins = {}
    for rel in factory_sources:
        for item in (rel, rel + ".meta"):
            if item not in source_pins:
                raise ValueError("Native capture omits configured consumer source pin: " + item)
            configured_source_pins[item] = source_pins[item]
    configured = dict(common)
    configured.update(kind="NativeQualifiedConfiguredBuildingMetadata",
                      reference={"path": "building-definitions.json", "sha256": "deferred"},
                      source_pins=configured_source_pins, rows=configured_rows)
    return {"building-definitions.json": definitions,
            "unit-production-metadata.json": unit_projection,
            "configured-metadata.json": configured}


def _write_new(path: Path, value: dict) -> str:
    data = (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    with path.open("xb") as stream:
        stream.write(data)
    return _sha_bytes(data)


def project(root: Path, output: Path, compiler_receipt_path: Path, audit_path: Path,
            build_path: Path, graph_path: Path) -> dict:
    root = root.resolve(strict=True)
    output = Path(os.path.abspath(output))
    if output.exists():
        raise ValueError("Fresh exclusive output directory required")
    evidence = validate_current_evidence(root, compiler_receipt_path, audit_path, build_path, graph_path)
    projector_path = Path(__file__).resolve()
    projector_sha = _sha_file(projector_path)
    capture_path = Path(evidence["capture_path"])
    capture_raw = capture_path.read_bytes()
    capture = json.loads(capture_raw.decode("utf-8", errors="strict"), object_pairs_hook=_pairs)
    source_pins = capture.get("source_pins")
    verify_pins(root, source_pins)
    factory_sources = read_factory_sources(root)
    compat_path = root / COMPAT_REL
    compat_sha = _sha_file(compat_path)
    if not compat_path.is_file() or compat_path.is_symlink():
        raise ValueError("Current S003 metadata consumer source unavailable")

    artifacts = project_payload(capture, evidence["capture_sha256"], source_pins, factory_sources)
    output.mkdir(parents=True, exist_ok=False)
    descriptors = {}
    definitions_path = output / "building-definitions.json"
    definitions_sha = _write_new(definitions_path, artifacts[definitions_path.name])
    descriptors[definitions_path.name] = {"path": str(definitions_path.resolve()), "sha256": definitions_sha}
    artifacts["configured-metadata.json"]["reference"] = descriptors[definitions_path.name]
    for name in ("unit-production-metadata.json", "configured-metadata.json"):
        path = output / name
        digest = _write_new(path, artifacts[name])
        descriptors[name] = {"path": str(path.resolve()), "sha256": digest}

    verify_pins(root, source_pins)
    current = evidence["current_compiler"]
    current_build = _json(Path(current["build_path"]))
    current_graph = _json(Path(current["graph_closure_path"]))
    verify_build_pins(root, current_build.get("source_and_reference_pins"))
    verify_pins(root, current_graph.get("source_pins"))
    if _sha_file(capture_path) != evidence["capture_sha256"] or _sha_file(compat_path) != compat_sha:
        raise ValueError("S003 capture or consumer source changed during projection")
    if _sha_file(Path(current["compiler_receipt_path"])) != current["compiler_receipt_sha256"] or _sha_file(Path(current["compiler_audit_path"])) != current["compiler_audit_sha256"] or _sha_file(Path(current["build_path"])) != current["build_sha256"] or _sha_file(Path(current["graph_closure_path"])) != current["graph_closure_sha256"]:
        raise ValueError("S003 current compiler epoch proof changed during projection")
    extension_descriptor = current["source_selection_extension_receipt"]
    if _sha_file(Path(extension_descriptor["path"])) != extension_descriptor["sha256"]:
        raise ValueError("S003 strict selector graph extension receipt changed during projection")
    grid_extension_descriptor = current["source_grid_surface_extension_receipt"]
    if _sha_file(Path(grid_extension_descriptor["path"])) != grid_extension_descriptor["sha256"]:
        raise ValueError("S003 strict grid/surface graph extension receipt changed during projection")
    if _sha_file(projector_path) != projector_sha:
        raise ValueError("S003 metadata projector changed during projection")
    receipt = {
        "schema_version": 1,
        "kind": "S003CurrentFactoryMetadataProjectionReceipt",
        "status": "PassedSourceOnlyProjection",
        "target": {"catalog_id": "S003", "difficulty": "Regular", "size": "Standard",
                   "seed": EXPECTED["seed"], "setup_hash": EXPECTED["setup_hash"]},
        "capture": {"path": str(capture_path), "sha256": evidence["capture_sha256"],
                    "receipt_path": evidence["capture_receipt_path"],
                    "receipt_sha256": evidence["capture_receipt_sha256"],
                    "metadata_key_sha256": EXPECTED["metadata_key_sha256"]},
        "source_graph": {"path": evidence["qualification_receipt"]["graph_path"],
                         "sha256": EXPECTED["graph_sha256"]},
        "run6_compiler_receipt_sha256": EXPECTED["run6_receipt_sha256"],
        "run6_post_run_pin_audit_sha256": EXPECTED["run6_audit_sha256"],
        "current_compiler_receipt_sha256": current["compiler_receipt_sha256"],
        "current_compiler_post_run_pin_audit_sha256": current["compiler_audit_sha256"],
        "current_source_build_receipt_sha256": current["build_sha256"],
        "current_compiler_graph_closure_sha256": current["graph_closure_sha256"],
        "current_compiler_epoch": current,
        "factory_consumer_source": {"path": str(compat_path), "sha256": compat_sha},
        "projector_source": {"path": str(projector_path), "sha256": projector_sha},
        "capture_finalizer_source": {"path": evidence["finalizer_path"], "sha256": evidence["finalizer_sha256"]},
        "source_pin_count": len(source_pins),
        "counts": EXPECTED["counts"],
        "artifacts": descriptors,
        "utility_world_created": True,
        "utility_world_disposed": True,
        "mission_world_created": False,
        "runtime_state_transferred": False,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "mission_ready": False,
        "runtime_admission": False,
        "scope": "Current S003 captured factory facts projected for existing source-only consumers; no mission/readiness authority",
    }
    _write_new(output / "projection-receipt.json", receipt)
    print(json.dumps({"status": receipt["status"], "artifacts": len(descriptors),
                      "buildings": EXPECTED["counts"]["buildings"], "units": EXPECTED["counts"]["units"],
                      "runtime_admission": False, "mission_ready": False}))
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--current-compiler-receipt", type=Path, required=True)
    parser.add_argument("--current-post-run-pin-audit", type=Path, required=True)
    parser.add_argument("--current-source-build-receipt", type=Path, required=True)
    parser.add_argument("--current-compiler-graph-closure", type=Path, required=True)
    args = parser.parse_args()
    try:
        project(args.repo, args.output, args.current_compiler_receipt, args.current_post_run_pin_audit,
                args.current_source_build_receipt, args.current_compiler_graph_closure)
        return 0
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as error:
        print("S003 current factory metadata projection failed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
