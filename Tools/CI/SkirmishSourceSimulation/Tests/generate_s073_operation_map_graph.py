#!/usr/bin/env python3
"""Add the exact S073 selected OperationMap YAML asset to a pinned config graph.

This source-only projection reuses the repository Ruby/Psych parser and the
same graph node shape consumed by SourceConfigMaterializer. It does not create
runtime readiness or certify native scene/rotation state.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


MAP_REL = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
GRID_REL = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"
SURFACE_REL = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset"
SCRIPT_META_REL = "Assets/Game/Scripts/Configs/OperationMapDefinition.cs.meta"
PARSER_REL = "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb"
RETAINED_MANIFEST_REL = "Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/provenance.json"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, message):
    if not ok:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--base-graph", required=True, type=Path)
    parser.add_argument("--base-graph-sha256", required=True)
    parser.add_argument("--selected-map-sha256", required=True)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()

    repo = args.repo.resolve(strict=True)
    graph_path = args.base_graph.resolve(strict=True)
    out = args.output.absolute()
    require(not out.exists(), "Fresh output path required")
    require(sha(graph_path) == args.base_graph_sha256, "Base source graph changed")
    map_path = repo / MAP_REL
    grid_path = repo / GRID_REL
    surface_path = repo / SURFACE_REL
    map_meta = Path(str(map_path) + ".meta")
    parser_path = repo / PARSER_REL
    script_meta = repo / SCRIPT_META_REL
    retained_manifest_path = repo / RETAINED_MANIFEST_REL
    for path in (map_path, grid_path, surface_path, map_meta, parser_path, script_meta,
                 retained_manifest_path):
        require(path.is_file(), "Required source input missing: " + str(path))
    require(sha(map_path) == args.selected_map_sha256, "Selected S073 OperationMap source changed")

    retained_manifest = json.loads(retained_manifest_path.read_text())
    parser_pin = retained_manifest.get("readback_parser")
    require(parser_pin == {"path": PARSER_REL, "sha256": sha(parser_path)},
            "Ruby source parser differs from retained provenance pin")

    graph = json.loads(graph_path.read_text())
    require(graph.get("schema_version") == 1 and graph.get("authority") == "AuthoredConfigAssets",
            "Authored config source graph required")
    require(graph.get("compiled_setup") is False and graph.get("world_initialized") is False and
            graph.get("native_world_state_used") is False,
            "Fresh source-only graph required")
    nodes = graph.get("nodes")
    pins = graph.get("source_pins")
    require(isinstance(nodes, list) and isinstance(pins, dict) and len(nodes) < 64,
            "Bounded source graph required")

    parsed = subprocess.run(["ruby", str(parser_path), str(map_path)], check=True,
                            capture_output=True, text=True, timeout=30)
    document = json.loads(parsed.stdout)[str(map_path)]
    mono = document.get("MonoBehaviour")
    require(isinstance(mono, dict), "Selected map MonoBehaviour readback required")
    script = mono.get("m_Script")
    script_guid = next((line.split(":", 1)[1].strip() for line in script_meta.read_text().splitlines()
                        if line.startswith("guid:")), None)
    require(script == {"fileID": 11500000, "guid": script_guid, "type": 3},
            "Selected YAML does not reference the current OperationMapDefinition script")
    require(mono.get("m_Name") == "OperationMap_IndustrialBasin" and
            mono.get("m_EditorClassIdentifier") == "Game.Configs::Game.Configs.OperationMapDefinition",
            "Selected YAML OperationMap asset identity changed")
    require(mono.get("operationMapId") == "opmap.skirmish.industrial_basin" and
            mono.get("sourceBinding", {}).get("sourceOperationMapId") == "opmap.skirmish.desert_base_01",
            "Selected S073 source-map binding changed")

    meta_guid = next((line.split(":", 1)[1].strip() for line in map_meta.read_text().splitlines()
                      if line.startswith("guid:")), None)
    require(isinstance(meta_guid, str) and len(meta_guid) == 32,
            "Unique selected OperationMap asset GUID required")
    fields = {key: value for key, value in mono.items() if not key.startswith("m_")}
    spellings = document.get("__source_scalar_spellings", {})
    scalar_spellings = {key[len("MonoBehaviour."):]: value for key, value in spellings.items()
                        if key.startswith("MonoBehaviour.")}
    require(scalar_spellings, "Exact serialized scalar spellings required")
    existing = [row for row in nodes if row.get("source_path") == MAP_REL or
                row.get("asset_guid") == meta_guid]
    require(not existing, "Selected map already exists in graph; refusing ambiguous replacement")

    grid_guid = fields.get("gridMetadata", {}).get("assetGuid")
    surface_guid = fields.get("surfaceMetadata", {}).get("assetGuid")
    grid_nodes = [row for row in nodes if row.get("asset_guid") == grid_guid]
    surface_nodes = [row for row in nodes if row.get("asset_guid") == surface_guid]
    require(len(grid_nodes) == 1 and grid_nodes[0].get("source_path") == GRID_REL and
            len(surface_nodes) == 1 and surface_nodes[0].get("source_path") == SURFACE_REL,
            "Selected map grid/surface references do not resolve uniquely in the source graph")
    for source_path, source_path_value in ((grid_path, grid_nodes[0]), (surface_path, surface_nodes[0])):
        require(sha(source_path) == source_path_value.get("source_sha256"),
                "Selected map reference source changed: " + str(source_path))
        require(pins.get(str(source_path)) == source_path_value.get("source_sha256"),
                "Selected map reference is not pinned by the base graph: " + str(source_path))
    require(fields.get("gridMetadata", {}).get("contentHash") == grid_nodes[0].get("source_sha256") and
            fields.get("surfaceMetadata", {}).get("contentHash") == surface_nodes[0].get("source_sha256"),
            "Selected map source metadata hashes differ from actual referenced graph nodes")

    source_sha = sha(map_path)
    node = {"asset_guid": meta_guid, "source_type": "OperationMapDefinition",
            "source_path": MAP_REL, "source_sha256": source_sha,
            "fields": fields, "scalar_spellings": scalar_spellings}
    graph["nodes"].append(node)
    graph["source_pins"][str(map_path)] = source_sha
    graph["source_pins"][str(map_meta)] = sha(map_meta)
    require(len(graph["nodes"]) <= 64, "Extended graph exceeds SourceConfigMaterializer bound")

    out.mkdir(parents=True)
    graph_out = out / "source-config-graph.json"
    raw = (json.dumps(graph, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    graph_out.write_bytes(raw)
    receipt = {"schema_version": 1, "kind": "S073SelectedOperationMapSourceGraphExtension",
               "status": "Passed", "source_graph": {"path": str(graph_path), "sha256": args.base_graph_sha256},
               "selected_map": {"path": str(map_path), "sha256": source_sha, "asset_guid": meta_guid,
                                "source_identity_hash": fields["sourceIdentityHash"],
                                "content_hash": fields["contentHash"]},
               "references": {"grid": grid_nodes[0]["source_path"], "surface": surface_nodes[0]["source_path"]},
               "node_count": len(graph["nodes"]), "output_graph": {"path": str(graph_out),
                                                                         "sha256": sha(graph_out)},
               "scope": "Source YAML projection only; no native rotation oracle, map publication, readiness, or mission outcome."}
    (out / "result.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
