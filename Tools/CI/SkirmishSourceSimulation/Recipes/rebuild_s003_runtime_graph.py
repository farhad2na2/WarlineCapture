#!/usr/bin/env python3
"""Rebuild S003's runtime graph from fresh closure and byte-verified retained nodes."""
import argparse
import hashlib
import json
from pathlib import Path

REUSED_NODES = {
    "Assets/Game/Configs/OperationMaps/OperationMapCatalog_Compatibility.asset",
    "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset",
    "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset",
    "Assets/Game/Resources/SkirmishBaseAssault.asset",
    "Assets/Game/Resources/SkirmishBattleCatalog.asset",
}
EXPECTED_OLD = "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f"
EXPECTED_FRESH = "f6f4e90b4ef11a93364682d442e7466812c9389c9693fed7b6da87cc69ec0652"


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def rebuild(repo, fresh, retained):
    if (fresh.get("authority") != "AuthoredConfigAssets" or retained.get("authority") != "AuthoredConfigAssets"
        or any(graph.get(field) is not False for graph in (fresh, retained)
               for field in ("compiled_setup", "world_initialized", "native_world_state_used"))):
        raise ValueError("Uninitialized authored graphs required")
    fresh_nodes = {row["source_path"]: row for row in fresh["nodes"]}
    old_nodes = {row["source_path"]: row for row in retained["nodes"]}
    if len(fresh_nodes) != len(fresh["nodes"]) or len(old_nodes) != len(retained["nodes"]):
        raise ValueError("Duplicate graph node")
    if set(old_nodes) - set(fresh_nodes) != REUSED_NODES:
        raise ValueError("Exact retained runtime nodes required")
    if any(path not in old_nodes or old_nodes[path] != row for path, row in fresh_nodes.items()):
        raise ValueError("Fresh and retained shared nodes differ")
    pins = dict(retained["source_pins"])
    differences = {path for path in pins.keys() & fresh["source_pins"].keys()
                   if pins[path] != fresh["source_pins"][path]}
    expected = str(repo / "Assets/Game/Scripts/Configs/Skirmish/SkirmishExpansionCatalogConfig.cs")
    if differences != {expected}:
        raise ValueError("Unexpected source closure differences")
    pins.update(fresh["source_pins"])
    for path, digest in pins.items():
        file = Path(path)
        file = file if file.is_absolute() else repo / file
        resolved = file.resolve(strict=True)
        if not resolved.is_relative_to(repo) or file.is_symlink() or sha(file.read_bytes()) != digest:
            raise ValueError("Stale runtime graph input: " + path)
    result = dict(fresh)
    result["source_pins"] = dict(sorted(pins.items()))
    result["nodes"] = fresh["nodes"] + [row for row in retained["nodes"] if row["source_path"] in REUSED_NODES]
    result["canonical_catalog_augmentation"] = retained["canonical_catalog_augmentation"]
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("repo", "fresh", "retained", "output"):
        parser.add_argument("--" + name, required=True)
    args = parser.parse_args()
    repo = Path(args.repo).resolve(strict=True)
    paths = [Path(args.fresh).resolve(strict=True), Path(args.retained).resolve(strict=True)]
    raw = [path.read_bytes() for path in paths]
    if [sha(value) for value in raw] != [EXPECTED_FRESH, EXPECTED_OLD]:
        raise ValueError("Exact fresh and retained source graph evidence required")
    graph = rebuild(repo, *(json.loads(value) for value in raw))
    graph["runtime_graph_reconstruction"] = {
        "fresh_closure": {"path": str(paths[0]), "sha256": sha(raw[0])},
        "retained_runtime_graph": {"path": str(paths[1]), "sha256": sha(raw[1])},
        "reused_node_paths": sorted(REUSED_NODES), "all_result_source_pins_current": True,
        "scope": "Source data only; no native scene, runtime admission, or readiness."}
    out = Path(args.output).absolute()
    out.mkdir(parents=True, exist_ok=False)
    path = out / "source-config-graph.json"
    data = (json.dumps(graph, indent=2) + "\n").encode()
    path.write_bytes(data)
    receipt = {"status": "PassedSourceOnlyRuntimeGraph", "graph": {"path": str(path), "sha256": sha(data)},
               "nodes": len(graph["nodes"]), "source_pins": len(graph["source_pins"]),
               "runtime_admission": False, "source_fit_certified": False, "mission_ready": False}
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
