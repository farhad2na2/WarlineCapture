#!/usr/bin/env python3
"""Combine pinned authored graph projections without changing source assets.

The output is configuration input only: it does not admit a mission or certify
the accuracy of either input capture, physical source, or native player state.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path

CATALOG = "Assets/Game/Resources/SkirmishBattleCatalog.asset"


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def pairs(rows):
    result = {}
    for key, value in rows:
        if key in result:
            raise ValueError("Duplicate JSON field: " + key)
        result[key] = value
    return result


def read_graph(path, expected, root):
    path = Path(path)
    if not path.is_absolute() or path.is_symlink() or str(path.resolve(strict=True)) != str(path):
        raise ValueError("Canonical absolute graph file required")
    raw = path.read_bytes()
    if not raw or len(raw) > 16 * 1024 * 1024 or digest(raw) != expected:
        raise ValueError("Pinned graph bytes differ")
    graph = json.loads(raw, object_pairs_hook=pairs, parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Nonfinite JSON")))
    if (graph.get("schema_version") != 1 or graph.get("authority") != "AuthoredConfigAssets" or
            any(graph.get(key) is not False for key in ("compiled_setup", "world_initialized", "native_world_state_used"))):
        raise ValueError("Uncompiled authored graph required")
    pins = graph.get("source_pins")
    if not isinstance(pins, dict) or not pins:
        raise ValueError("Pinned source graph closure required")
    for name, expected_pin in pins.items():
        source = Path(name)
        if not source.is_absolute() or source.is_symlink() or not source.resolve(strict=True).is_relative_to(root):
            raise ValueError("Source pin outside exact repository")
        if digest(source.read_bytes()) != expected_pin:
            raise ValueError("Authored source pin changed: " + name)
    return graph, digest(raw)


def has_object_reference(value):
    if isinstance(value, list):
        return any(has_object_reference(item) for item in value)
    if isinstance(value, dict):
        return ("guid" in value and "fileID" in value) or any(has_object_reference(item) for item in value.values())
    return False


def merge(mission_graph, catalog_graph, root):
    rows = catalog_graph.get("nodes")
    if not isinstance(rows, list):
        raise ValueError("Catalog graph nodes required")
    candidates = [row for row in rows if row.get("source_type") == "SkirmishBattleCatalogConfig" and row.get("source_path") == CATALOG]
    if len(candidates) != 1 or has_object_reference(candidates[0].get("fields")):
        raise ValueError("One self-contained authored catalog node required")
    node = candidates[0]
    source_path = str(root / CATALOG)
    source_hash = catalog_graph["source_pins"].get(source_path)
    if not source_hash or node.get("source_sha256") != source_hash:
        raise ValueError("Catalog node must match its pinned source asset")
    nodes = mission_graph.get("nodes")
    if not isinstance(nodes, list) or len(nodes) >= 64:
        raise ValueError("Bounded mission graph required")
    if any(row.get("asset_guid") == node.get("asset_guid") or row.get("source_path") == CATALOG for row in nodes):
        raise ValueError("Catalog identity already exists in mission graph")
    result = copy.deepcopy(mission_graph)
    result["nodes"].append(copy.deepcopy(node))
    for path in (source_path, source_path + ".meta"):
        value = catalog_graph["source_pins"].get(path)
        if value is None or path in result["source_pins"] and result["source_pins"][path] != value:
            raise ValueError("Conflicting or absent catalog source pin")
        result["source_pins"][path] = value
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--mission-graph", type=Path, required=True)
    parser.add_argument("--mission-sha256", required=True)
    parser.add_argument("--catalog-graph", type=Path, required=True)
    parser.add_argument("--catalog-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve(strict=True)
    mission, mission_hash = read_graph(args.mission_graph, args.mission_sha256, root)
    catalog, catalog_hash = read_graph(args.catalog_graph, args.catalog_sha256, root)
    result = merge(mission, catalog, root)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    graph_path = output / "source-config-graph.json"
    graph_path.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    # The captured inputs and every declared source remain unchanged after writing.
    read_graph(args.mission_graph, mission_hash, root)
    read_graph(args.catalog_graph, catalog_hash, root)
    receipt = {"status": "PreparedAuthoredGraphComposition", "mission_graph_sha256": mission_hash,
               "catalog_graph_sha256": catalog_hash, "graph_sha256": digest(graph_path.read_bytes()),
               "added_catalog_asset": CATALOG, "added_nodes": 1, "scope": "Source-only graph composition; no runtime or native admission",
               "world_initialized": False, "source_fit_certified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
