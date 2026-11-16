#!/usr/bin/env python3
"""Merge freshly generated S003 and catalog dependency graphs, without admission."""
import argparse
import hashlib
import json
from pathlib import Path


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def merge(base, catalog, repo):
    if any(g.get("authority") != "AuthoredConfigAssets" or g.get("compiled_setup") is not False
           or g.get("world_initialized") is not False or g.get("native_world_state_used") is not False
           for g in (base, catalog)):
        raise ValueError("Fresh uninitialized authored graphs required")
    if base.get("catalog_path") != catalog.get("catalog_path"):
        raise ValueError("Catalog identity mismatch")
    pins = dict(base["source_pins"])
    for path, sha in catalog["source_pins"].items():
        if path in pins and pins[path] != sha:
            raise ValueError("Conflicting source pin: " + path)
        pins[path] = sha
    for path, expected in pins.items():
        p = Path(path)
        p = p if p.is_absolute() else repo / p
        if not p.is_file() or p.is_symlink() or digest(p.read_bytes()) != expected:
            raise ValueError("Stale or invalid source pin: " + path)
    nodes = {node["source_path"]: node for node in base["nodes"]}
    if len(nodes) != len(base["nodes"]):
        raise ValueError("Duplicate base node")
    seen = set()
    for node in catalog["nodes"]:
        path = node["source_path"]
        if path in seen or (path in nodes and nodes[path] != node):
            raise ValueError("Duplicate or conflicting catalog node: " + path)
        seen.add(path)
        nodes.setdefault(path, node)
    out = dict(base)
    out["source_pins"] = dict(sorted(pins.items()))
    out["nodes"] = list(nodes.values())
    return out


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", required=True)
    p.add_argument("--base", required=True)
    p.add_argument("--catalog", required=True)
    p.add_argument("--output", required=True)
    args = p.parse_args()
    repo = Path(args.repo).resolve(strict=True)
    paths = [Path(args.base).resolve(strict=True), Path(args.catalog).resolve(strict=True)]
    raw = [path.read_bytes() for path in paths]
    graphs = [json.loads(value) for value in raw]
    result = merge(*graphs, repo)
    catalog_path = result["catalog_path"]
    result["catalog_closure_provenance"] = {
        "base_s003_graph_path": str(paths[0]), "base_s003_graph_sha256": digest(raw[0]),
        "catalog_dependency_graph_path": str(paths[1]), "catalog_dependency_graph_sha256": digest(raw[1]),
        "catalog_source_sha256": result["source_pins"][str(repo / catalog_path)],
        "overlapping_nodes_identical": True, "overlapping_pins_identical": True,
        "all_imported_sources_current_at_extension": True,
        "purpose": "Actual authored catalog dependency closure; no mission admission or readiness."}
    output = Path(args.output)
    output.mkdir(parents=True, exist_ok=False)
    path = output / "source-config-graph.json"
    encoded = (json.dumps(result, indent=2) + "\n").encode()
    path.write_bytes(encoded)
    receipt = {"status": "PassedSourceOnlyGraphMerge", "graph": str(path.resolve()),
               "sha256": digest(encoded), "nodes": len(result["nodes"]), "source_pins": len(result["source_pins"]),
               "world_initialized": False, "source_fit_certified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
