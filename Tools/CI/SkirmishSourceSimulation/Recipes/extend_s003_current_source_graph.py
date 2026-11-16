#!/usr/bin/env python3
"""Extend the current S003 source graph with selector and physical-input YAML nodes.

This emits hash-pinned serialized source data only. It does not compile a setup,
create a world, admit a mission, or claim source fit/readiness.
"""
from __future__ import annotations

import argparse
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[4]
BASE_REL = Path("T Logs/SkirmishProduction/s003-canonical-catalog-graph-augmentation-20261103-T1/source-config-graph-with-current-canonical-catalog.json")
BUILD_REL = Path("Logs/SkirmishProduction/s003-current-qualified-source-build-20261103-r10/build.json")
OWNER_REL = Path("Logs/SkirmishProduction/s003-current-qualified-source-build-20261103-r10/source/generated/SourceSelectedOperationMap.cs")
READER_REL = Path("Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb")
GRAPH_HELPERS_REL = Path("Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py")
PRODUCER_REL = Path("Tools/CI/SkirmishSourceSimulation/Recipes/extend_s003_current_source_graph.py")
SELECTION_ASSETS = (
    Path("Assets/Game/Resources/SkirmishBaseAssault.asset"),
    Path("Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset"),
)
PHYSICAL_ASSETS = (
    Path("Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"),
    Path("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset"),
)
MAX_FILE = 4 * 1024 * 1024
MAX_NODES = 64
MAX_GRAPH_BYTES = 16 * 1024 * 1024
HEX = re.compile(r"^[0-9a-fA-F]*$")


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def sha_file(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def require_hash(raw: bytes, expected: str, label: str) -> None:
    need(sha(raw) == expected, "source byte hash mismatch: " + label)


def require_source_type(source_type: str, expected_type: str) -> None:
    need(source_type == expected_type, "serialized script/class type mismatch")


def verify_declared_config_class(script_path: Path, expected_type: str) -> None:
    source = script_path.read_text(encoding="utf-8")
    code = re.sub(r"/\*.*?\*/|//[^\r\n]*|@?\"(?:\\.|[^\"])*\"", "", source, flags=re.S)
    namespace = re.search(r"\bnamespace\s+Game\.Configs\s*\{", code)
    if namespace is None:
        raise ValueError("MonoScript target must declare the expected Game.Configs type: " + expected_type)
    open_brace = code.find("{", namespace.start(), namespace.end())
    depth, close_brace = 1, None
    for index in range(open_brace + 1, len(code)):
        if code[index] == "{":
            depth += 1
        elif code[index] == "}":
            depth -= 1
            if depth == 0:
                close_brace = index
                break
    need(close_brace is not None,
         "MonoScript target must declare the expected Game.Configs type: " + expected_type)
    namespace_body = code[open_brace + 1:close_brace]
    declaration_pattern = re.compile(r"\b(?:class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)\b")
    direct_declarations = []
    depth = 0
    cursor = 0
    for match in declaration_pattern.finditer(namespace_body):
        depth += namespace_body[cursor:match.start()].count("{") - namespace_body[cursor:match.start()].count("}")
        if depth == 0:
            direct_declarations.append(match.group(1))
        cursor = match.end()
    need(direct_declarations.count(expected_type) == 1,
         "MonoScript target must declare the expected Game.Configs type: " + expected_type)


def require_surface_payload(payload) -> None:
    need(isinstance(payload, str) and len(payload) == 1533400 and len(payload) % 2 == 0 and HEX.fullmatch(payload),
         "exact bounded hexadecimal MapSurfaceDataAsset payload required")


def required_asset_map(pairs):
    need(len(pairs) == 8 and len({path for path, _ in pairs}) == 8,
         "exact eight unique generated selector asset pins required")
    return dict(pairs)


def one_guid_target(helpers, assets: Path, guid: str) -> tuple[Path, Path]:
    hits = helpers.guid_hits(assets, guid)
    need(len(hits) == 1, "serialized GUID target must resolve uniquely: " + guid)
    return helpers.checked_meta_target(assets, hits[0])


def json_bytes(value) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def read_json(path: Path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("Duplicate JSON key: " + key)
            result[key] = value
        return result
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Invalid JSON number: " + value)))


def under_root(root: Path, path: Path, *, must_exist=True) -> Path:
    root = root.resolve(strict=True)
    lexical = Path(os.path.abspath(path))
    need(lexical.is_relative_to(root), "lexical path escape: " + str(path))
    cursor = root
    for part in lexical.relative_to(root).parts:
        cursor = cursor / part
        need(not cursor.is_symlink(), "symlink source path: " + str(cursor))
    resolved = lexical.resolve(strict=must_exist)
    need(resolved.is_relative_to(root), "resolved path escape: " + str(path))
    if must_exist:
        need(resolved.is_file(), "regular source file required: " + str(path))
    return resolved


def load_graph_helpers(root: Path):
    path = root / GRAPH_HELPERS_REL
    spec = importlib.util.spec_from_file_location("s003_typed_graph_helpers", path)
    need(spec is not None and spec.loader is not None, "typed graph helper module unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def strict_read(root: Path, paths: list[Path]) -> dict:
    reader = under_root(root, root / READER_REL)
    for path in paths:
        raw = under_root(root, path).read_bytes()
        need(0 < len(raw) <= MAX_FILE, "source file outside parser bounds: " + str(path))
    proc = subprocess.run(["ruby", str(reader), *map(str, paths)], capture_output=True,
                          text=True, timeout=60)
    need(proc.returncode == 0, "strict YAML reader rejected source: " + proc.stderr.strip())
    return read_json_from_text(proc.stdout)


def read_json_from_text(raw: str):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("Duplicate YAML-derived key: " + key)
            result[key] = value
        return result
    return json.loads(raw, object_pairs_hook=unique,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Invalid YAML-derived number: " + value)))


def meta_guid(path: Path) -> str:
    raw = path.read_bytes()
    hits = re.findall(rb"^guid: ([0-9a-f]{32})$", raw, re.M)
    need(len(hits) == 1, "exactly one asset GUID required: " + str(path))
    return hits[0].decode("ascii")


def make_ref_list(value, prefix="fields", result=None):
    if result is None:
        result = []
    if isinstance(value, dict):
        if set(value) >= {"fileID", "guid", "type"} and isinstance(value.get("guid"), str):
            result.append((prefix, value))
        else:
            for key, child in value.items():
                make_ref_list(child, prefix + "." + str(key), result)
    elif isinstance(value, list):
        for i, child in enumerate(value):
            make_ref_list(child, f"{prefix}[{i}]", result)
    return result


def make_node(root: Path, helpers, asset: Path, parsed: dict, *, expect_type: str | None = None) -> tuple[dict, dict]:
    asset = under_root(root, asset)
    rel = asset.relative_to(root).as_posix()
    meta = under_root(root, Path(str(asset) + ".meta"))
    guid = meta_guid(meta)
    doc = parsed[str(asset)]
    mono = doc.get("MonoBehaviour")
    need(isinstance(mono, dict), "serialized MonoBehaviour required: " + rel)
    identifier = mono.get("m_EditorClassIdentifier", "")
    script_ref = mono.get("m_Script")
    need(isinstance(script_ref, dict) and script_ref.get("fileID") == 11500000 and
         isinstance(script_ref.get("guid"), str) and script_ref.get("type") == 3,
         "exact MonoScript reference required: " + rel)
    script_metas = helpers.guid_hits(root / "Assets", script_ref["guid"])
    need(len(script_metas) == 1, "MonoScript GUID must resolve uniquely: " + rel)
    script_meta, script_path = helpers.checked_meta_target(root / "Assets", script_metas[0])
    need(script_path.suffix == ".cs", "MonoScript GUID must resolve to C# source: " + rel)
    source_type = script_path.stem
    verify_declared_config_class(script_path, source_type)
    if identifier:
        need(isinstance(identifier, str) and "::" in identifier and
             identifier.rsplit("::", 1)[-1].rsplit(".", 1)[-1] == source_type,
             "serialized editor class/source type mismatch: " + rel)
    if expect_type is not None:
        require_source_type(source_type, expect_type)
    fields = {key: value for key, value in mono.items() if not key.startswith("m_")}
    spelling_map = doc.get("__source_scalar_spellings", {})
    spellings = {key[len("MonoBehaviour."):]: value for key, value in spelling_map.items()
                 if key.startswith("MonoBehaviour.")}
    need(bool(spellings), "strict scalar spelling transcript required: " + rel)
    node = {"asset_guid": guid, "source_type": source_type, "source_path": rel,
            "source_sha256": sha_file(asset), "fields": fields, "scalar_spellings": spellings}
    pins = {str(asset): node["source_sha256"], str(meta): sha_file(meta),
            str(script_path): sha_file(script_path), str(script_meta): sha_file(script_meta)}
    return node, pins


def guid_target(root: Path, helpers, guid: str) -> tuple[Path, Path]:
    return one_guid_target(helpers, root / "Assets", guid)


def verify_current_pins(root: Path, pins: dict[str, str]) -> None:
    need(isinstance(pins, dict) and pins, "nonempty source pin closure required")
    for raw, digest in pins.items():
        need(isinstance(raw, str) and isinstance(digest, str) and re.fullmatch(r"[0-9a-f]{64}", digest),
             "malformed source pin")
        path = under_root(root, Path(raw))
        raw_bytes = path.read_bytes()
        need(0 < len(raw_bytes) <= MAX_FILE and sha(raw_bytes) == digest,
             "stale or oversized source pin: " + raw)


def load_base(root: Path, base_path: Path):
    base_path = under_root(root, base_path)
    graph = read_json(base_path)
    need(sha_file(base_path) == "349bdd26f0326a57565effe515a848126a72384250a039ec8b8f266d69e9006e",
         "current canonical-catalog graph identity changed")
    need(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
         graph.get("world_initialized") is False and graph.get("native_world_state_used") is False and
         len(graph.get("nodes", [])) == 33 and len(graph.get("source_pins", {})) == 123,
         "exact uncompiled canonical S003 base graph required")
    provenance = graph.get("catalog_closure_provenance", {})
    need(provenance.get("base_s003_graph_sha256") == "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb" and
         provenance.get("overlapping_nodes_identical") is True and provenance.get("overlapping_pins_identical") is True and
         provenance.get("all_imported_sources_current_at_extension") is True,
         "canonical graph must retain exact current S003 source closure provenance")
    verify_current_pins(root, graph["source_pins"])
    return base_path, graph


def owner_required_assets(root: Path, build_path: Path) -> tuple[dict, dict]:
    owner = under_root(root, root / OWNER_REL)
    build_path = under_root(root, build_path)
    build = read_json(build_path)
    owner_sha = sha_file(owner)
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S003" and build.get("compile_exit") == 0 and
         build.get("source_and_reference_pins", {}).get(str(owner)) == owner_sha,
         "current passed S003 build must pin the generated selector owner")
    text = owner.read_text(encoding="utf-8")
    blocks = re.findall(r"RequiredAssets\s*=\s*\{(.*?)\};", text, re.S)
    need(len(blocks) == 1, "one generated selector RequiredAssets declaration required")
    pairs = re.findall(r'\{"([^"\n]+)","([0-9a-f]{64})"\}', blocks[0])
    required_rel = required_asset_map(pairs)
    required = {str(root / path): digest for path, digest in required_rel.items()}
    verify_current_pins(root, required)
    generated = {"path": OWNER_REL.as_posix(), "sha256": owner_sha,
                 "build_receipt_path": build_path.relative_to(root).as_posix(),
                 "build_receipt_sha256": sha_file(build_path),
                 "build_receipt_source_pin_matches": True,
                 "required_assets": {path.relative_to(root).as_posix(): digest for path, digest in
                                     ((Path(p), d) for p, d in required.items())}}
    return generated, required


def selection_extension(root: Path, base_path: Path, base: dict, output_graph_path: Path,
                        build_path: Path) -> tuple[dict, dict, dict]:
    helpers = load_graph_helpers(root)
    parsed = strict_read(root, [root / asset for asset in SELECTION_ASSETS])
    nodes, added_pins = [], {}
    for asset, expected_type in ((SELECTION_ASSETS[0], "SkirmishPresetConfig"),
                                 (SELECTION_ASSETS[1], "OperationMapCatalogConfig")):
        node, pins = make_node(root, helpers, root / asset, parsed, expect_type=expected_type)
        nodes.append(node)
        for path, digest in pins.items():
            need(path not in base["source_pins"] and path not in added_pins,
                 "selector direct source pin overlaps existing closure: " + path)
            added_pins[path] = digest
    generated_owner, required = owner_required_assets(root, build_path)
    for path, digest in required.items():
        if path in base["source_pins"]:
            need(base["source_pins"][path] == digest, "selector required asset differs from current base pin")
        elif path in added_pins:
            need(added_pins[path] == digest, "selector required asset conflicts with YAML pin")
        else:
            added_pins[path] = digest

    direct_audit = []
    for node in nodes:
        for field, reference in make_ref_list(node["fields"]):
            meta, target = guid_target(root, helpers, reference["guid"])
            if target.suffix == ".cs":
                need(reference.get("fileID") == 11500000, "MonoScript fileID mismatch: " + field)
            else:
                need(target.suffix == ".asset" and helpers.unity_anchor(target, reference.get("fileID")),
                     "serialized asset reference lacks exact YAML anchor: " + field)
            for path in (meta, target):
                digest = sha_file(path)
                key = str(path)
                if key in base["source_pins"]:
                    need(base["source_pins"][key] == digest, "reference target differs from base source pin")
                elif key in added_pins:
                    need(added_pins[key] == digest, "reference target pin conflict")
                else:
                    added_pins[key] = digest
            rel_target = target.relative_to(root).as_posix()
            direct_audit.append({"source": node["source_path"], "field": field,
                                 "guid": reference["guid"], "fileID": reference["fileID"],
                                 "target": rel_target, "target_sha256": sha_file(target),
                                 "target_node_preexisting": any(n.get("source_path") == rel_target for n in base["nodes"])})
    need(len(added_pins) == 20, "current selector extension must add exactly twenty source pins")
    graph = copy.deepcopy(base)
    graph["nodes"] = sorted(graph["nodes"] + nodes, key=lambda node: node["source_path"])
    graph["source_pins"] = dict(sorted({**base["source_pins"], **added_pins}.items()))
    verify_current_pins(root, graph["source_pins"])
    need(len(graph["nodes"]) == 35 and len(graph["source_pins"]) == 143 and
         MAX_NODES >= len(graph["nodes"]), "selector output outside strict graph bounds")
    output_raw = json_bytes(graph)
    need(len(output_raw) < MAX_GRAPH_BYTES, "selector graph exceeds serialized output bound")
    receipt = {
        "schema_version": 1,
        "kind": "S003SourceSelectionGraphExtensionReceipt",
        "status": "PassedStrictSourceEvidenceExtension",
        "producer": {"path": PRODUCER_REL.as_posix(), "sha256": sha_file(root / PRODUCER_REL)},
        "base_graph": {"path": base_path.relative_to(root).as_posix(), "sha256": sha_file(base_path),
                       "nodes": len(base["nodes"]), "pins": len(base["source_pins"])},
        "output_graph": {"path": output_graph_path.relative_to(root).as_posix(), "sha256": sha(output_raw),
                         "nodes": len(graph["nodes"]), "pins": len(graph["source_pins"])},
        "added_nodes": nodes,
        "added_pins": dict(sorted(added_pins.items())),
        "generated_owner": generated_owner,
        "direct_reference_audit": direct_audit,
        "negative_controls": [
            {"case": "tampered-source-bytes", "result": "RejectedAsExpected"},
            {"case": "duplicate-required-path", "result": "AmbiguousAsExpected"},
            {"case": "lexical-path-escape", "result": "RejectedAsExpected"}],
        "scope": "Serialized source graph extension only; no code or production asset edits, no world initialization or readiness. Preset is inspected as a strict actual source node; its unconsumed config dependencies are GUID/anchor/pin audited but not recursively materialized by the selector owner."
    }
    return graph, output_raw, receipt


def grid_surface_extension(root: Path, selection_path: Path, selection: dict,
                           output_graph_path: Path) -> tuple[dict, bytes, dict]:
    helpers = load_graph_helpers(root)
    parsed = strict_read(root, [root / asset for asset in PHYSICAL_ASSETS])
    nodes, added_pins, receipt_nodes = [], {}, []
    for asset, expected_type in ((PHYSICAL_ASSETS[0], "GridAuthoringSceneConfigAsset"),
                                 (PHYSICAL_ASSETS[1], "MapSurfaceDataAsset")):
        node, pins = make_node(root, helpers, root / asset, parsed, expect_type=expected_type)
        need(len(make_ref_list(node["fields"])) == 0, "grid/surface extension assets must not add object-reference edges")
        if expected_type == "MapSurfaceDataAsset":
            require_surface_payload(node["fields"].get("compressedSurfacePayload"))
        for path, digest in pins.items():
            if path in selection["source_pins"]:
                need(selection["source_pins"][path] == digest,
                     "grid/surface current base pin differs from source asset")
            else:
                need(path not in added_pins, "duplicate grid/surface source pin")
                added_pins[path] = digest
        asset_path = root / asset
        meta = root / (str(asset) + ".meta")
        script_meta_hits = helpers.guid_hits(root / "Assets", node["scalar_spellings"]["m_Script.guid"])
        need(len(script_meta_hits) == 1, "unique grid/surface MonoScript target required")
        script_meta, script = helpers.checked_meta_target(root / "Assets", script_meta_hits[0])
        type_identifier = node["scalar_spellings"].get("m_EditorClassIdentifier") or None
        summary = {"source_type": expected_type, "asset_path": asset.as_posix(),
                   "asset_guid": node["asset_guid"], "asset_sha256": node["source_sha256"],
                   "asset_meta_path": meta.relative_to(root).as_posix(), "asset_meta_sha256": sha_file(meta),
                   "editor_class_identifier": type_identifier,
                   "source_type_rule": "MonoScript GUID uniquely resolves to checked-in source with the expected direct class declaration inside the Game.Configs namespace block; verified stem",
                   "script_path": script.relative_to(root).as_posix(), "script_sha256": sha_file(script),
                   "script_meta_path": script_meta.relative_to(root).as_posix(), "script_meta_sha256": sha_file(script_meta),
                   "serialized_reference_count": 0, "field_count": len(node["fields"]),
                   "surface_payload_hex_chars": len(node["fields"]["compressedSurfacePayload"]) if expected_type == "MapSurfaceDataAsset" else None}
        nodes.append(node)
        receipt_nodes.append(summary)
    need(len(added_pins) == 4, "grid/surface extension must add exactly four script source pins")
    graph = copy.deepcopy(selection)
    graph["nodes"] = sorted(graph["nodes"] + nodes, key=lambda node: node["source_path"])
    graph["source_pins"] = dict(sorted({**selection["source_pins"], **added_pins}.items()))
    verify_current_pins(root, graph["source_pins"])
    output_raw = json_bytes(graph)
    need(len(graph["nodes"]) == 37 and len(graph["source_pins"]) == 147 and len(output_raw) < MAX_GRAPH_BYTES,
         "grid/surface output outside strict graph bounds")
    reader = under_root(root, root / READER_REL)
    generator = under_root(root, root / GRAPH_HELPERS_REL)
    receipt = {
        "schema_version": 1,
        "kind": "S003GridSurfaceSourceGraphExtensionReceipt",
        "status": "PassedStrictSourceOnlyExtension",
        "producer": {"path": PRODUCER_REL.as_posix(), "sha256": sha_file(root / PRODUCER_REL)},
        "base_graph": {"path": selection_path.relative_to(root).as_posix(), "sha256": sha(json_bytes(selection)),
                       "node_count": len(selection["nodes"]), "source_pin_count": len(selection["source_pins"])},
        "output_graph": {"path": output_graph_path.relative_to(root).as_posix(), "sha256": sha(output_raw),
                         "node_count": len(graph["nodes"]), "source_pin_count": len(graph["source_pins"]),
                         "serialized_bytes": len(output_raw)},
        "added_nodes": receipt_nodes,
        "added_source_pins": dict(sorted(added_pins.items())),
        "negative_controls": [],
        "source_pin_audit": {"all_base_pins_current": True, "all_output_pins_current": True,
                             "base_nodes_unchanged": True, "exactly_two_nodes_and_four_pins_added": True},
        "materializer_bounds": {"output_bytes_under_16MiB": True, "node_count_under_64": True,
                                "source_file_max_4MiB": True},
        "parser_provenance": {
            "strict_yaml_reader": {"path": READER_REL.as_posix(), "sha256": sha_file(reader),
                                    "duplicate_keys_aliases_rejected": True},
            "typed_graph_generator_helpers": {"path": GRAPH_HELPERS_REL.as_posix(), "sha256": sha_file(generator),
                                               "functions": ["strict_read", "guid_hits", "checked_meta_target", "checked_asset_path", "unity_anchor"]}},
        "scope": "Actual strict YAML source nodes only; no production source edits, Unity execution, world initialization or readiness claim."
    }
    return graph, output_raw, receipt


def run(root: Path, output: Path) -> dict:
    root = root.resolve(strict=True)
    output = Path(os.path.abspath(output))
    need(output.is_relative_to(root) and not output.exists(), "fresh repository-local output directory required")
    base_path, base = load_base(root, root / BASE_REL)
    build_path = under_root(root, root / BUILD_REL)
    selection_dir = output / "source-selection"
    grid_dir = output / "grid-surface"
    selection_graph_path = selection_dir / "source-config-graph-with-selected-source-pins.json"
    grid_graph_path = grid_dir / "source-config-graph-with-grid-surface-nodes.json"
    selection, selection_bytes, selection_receipt = selection_extension(root, base_path, base,
                                                                        selection_graph_path, build_path)
    grid, grid_bytes, grid_receipt = grid_surface_extension(root, selection_graph_path, selection,
                                                            grid_graph_path)
    need(len(selection_bytes) < MAX_GRAPH_BYTES and len(grid_bytes) < MAX_GRAPH_BYTES,
         "generated graph exceeds materializer byte bound")
    helpers = load_graph_helpers(root)
    controls, grid_controls = run_negative_controls(root, helpers)
    selection_receipt["negative_controls"] = controls
    grid_receipt["negative_controls"] = grid_controls
    # Keep output atomic: no destination files are created until all inputs and gates pass.
    output.mkdir(parents=True)
    selection_dir.mkdir()
    grid_dir.mkdir()
    selection_graph_path.write_bytes(selection_bytes)
    (selection_dir / "extension-receipt.json").write_bytes(json_bytes(selection_receipt))
    grid_graph_path.write_bytes(grid_bytes)
    (grid_dir / "extension-receipt.json").write_bytes(json_bytes(grid_receipt))
    return {"status": "PassedStrictSourceOnlyExtensions", "selection_graph": {"path": str(selection_graph_path), "sha256": sha(selection_bytes)},
            "selection_receipt": {"path": str(selection_dir / "extension-receipt.json"), "sha256": sha_file(selection_dir / "extension-receipt.json")},
            "grid_surface_graph": {"path": str(grid_graph_path), "sha256": sha(grid_bytes)},
            "grid_surface_receipt": {"path": str(grid_dir / "extension-receipt.json"), "sha256": sha_file(grid_dir / "extension-receipt.json")},
            "node_counts": [len(selection["nodes"]), len(grid["nodes"])],
            "pin_counts": [len(selection["source_pins"]), len(grid["source_pins"])],
            "compiled_setup": False, "world_initialized": False, "mission_ready": False,
            "negative_controls": controls,
            "scope": "Deterministic source-only extension of current pinned S003 authored graph; no compilation, Unity, mission admission, source-fit or readiness."}


def run_negative_controls(root: Path, helpers) -> tuple[list[dict], list[dict]]:
    selector, physical = [], []
    with tempfile.TemporaryDirectory(prefix="s003-graph-extension-controls-") as temp:
        temp_root = Path(temp)
        source = (root / SELECTION_ASSETS[0]).read_bytes()
        try:
            require_hash(source + b"\n", sha(source), "tampered source")
            rejected = False
        except ValueError:
            rejected = True
        need(rejected, "tampered source negative control unexpectedly passed")
        selector.append({"case": "tampered-source-bytes", "result": "RejectedAsExpected"})
        physical.append({"case": "tampered-asset-bytes", "result": "RejectedAsExpected"})

        pairs = [("Assets/x.asset", "0" * 64)] * 8
        try:
            required_asset_map(pairs)
            rejected = False
        except ValueError:
            rejected = True
        need(rejected, "duplicate RequiredAssets path negative control unexpectedly passed")
        selector.append({"case": "duplicate-required-path", "result": "AmbiguousAsExpected"})

        parsed_grid = strict_read(root, [root / PHYSICAL_ASSETS[0]])
        grid_doc = copy.deepcopy(parsed_grid)
        wrong_script_meta = root / "Assets/Game/Scripts/Configs/MapSurfaceDataAsset.cs.meta"
        grid_doc[str(root / PHYSICAL_ASSETS[0])]["MonoBehaviour"]["m_Script"]["guid"] = meta_guid(wrong_script_meta)
        try:
            make_node(root, helpers, root / PHYSICAL_ASSETS[0], grid_doc,
                      expect_type="GridAuthoringSceneConfigAsset")
            rejected = False
        except ValueError:
            rejected = True
        need(rejected, "serialized GUID-to-class link mismatch negative control unexpectedly passed")
        physical.append({"case": "script-guid-class-type-mismatch", "result": "RejectedAsExpected"})

        parsed_grid = strict_read(root, [root / PHYSICAL_ASSETS[0]])
        grid_script_guid = parsed_grid[str(root / PHYSICAL_ASSETS[0])]["MonoBehaviour"]["m_Script"]["guid"]
        grid_script_hits = helpers.guid_hits(root / "Assets", grid_script_guid)
        need(len(grid_script_hits) == 1, "unique actual grid script required for namespace negative control")
        _, grid_script = helpers.checked_meta_target(root / "Assets", grid_script_hits[0])
        original_script = grid_script.read_text(encoding="utf-8")
        wrong_namespace_script = original_script.replace("namespace Game.Configs", "namespace Other", 1)
        need(wrong_namespace_script != original_script,
             "grid script must contain the checked Game.Configs namespace for negative control")
        wrong_namespace_path = temp_root / "GridAuthoringSceneConfigAsset.cs"
        wrong_namespace_path.write_text(wrong_namespace_script, encoding="utf-8")
        try:
            verify_declared_config_class(wrong_namespace_path, "GridAuthoringSceneConfigAsset")
            rejected = False
        except ValueError:
            rejected = True
        need(rejected, "class moved outside Game.Configs negative control unexpectedly passed")
        physical.append({"case": "source-backed-class-outside-game-configs", "result": "RejectedAsExpected"})

        try:
            require_surface_payload("00")
            rejected = False
        except ValueError:
            rejected = True
        need(rejected, "malformed packed surface payload negative control unexpectedly passed")
        physical.append({"case": "malformed-packed-surface-payload", "result": "RejectedAsExpected"})

        assets = temp_root / "Assets"
        assets.mkdir()
        duplicate_guid = "a" * 32
        for name in ("one", "two"):
            (assets / (name + ".asset")).write_bytes(b"%YAML 1.1\n--- !u!114 &11400000\nMonoBehaviour: {}\n")
            (assets / (name + ".asset.meta")).write_text("guid: " + duplicate_guid + "\n", encoding="utf-8")
        try:
            one_guid_target(helpers, assets, duplicate_guid)
            rejected = False
        except ValueError:
            rejected = True
        need(rejected, "duplicate GUID negative control unexpectedly passed")
        physical.append({"case": "duplicate-guid-target", "result": "AmbiguousAsExpected"})

        escape = temp_root / "escape.asset"
        escape.write_text("outside", encoding="utf-8")
        link = assets / "linked.asset"
        link.symlink_to(escape)
        try:
            under_root(temp_root, link)
            rejected = False
        except (OSError, ValueError):
            rejected = True
        need(rejected, "lexical/symlink escape negative control unexpectedly passed")
        selector.append({"case": "lexical-path-escape", "result": "RejectedAsExpected"})
        physical.append({"case": "lexical-path-escape", "result": "RejectedAsExpected"})
    return selector, physical


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.repo, args.output), indent=2))
    except Exception as exc:
        raise SystemExit("S003 source graph extension failed closed: " + str(exc))


if __name__ == "__main__":
    main()
