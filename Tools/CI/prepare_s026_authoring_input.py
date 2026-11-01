#!/usr/bin/env python3
"""Package a source-backed S026 authoring draft from explicit existing inputs.

This tool adds the exact current S026 Campaign OperationMap YAML to an
explicitly pinned SourceConfigMaterializer graph, then emits the existing
hash-bound authoring-draft schema. It creates no metadata, certificates,
mission state, or runtime bundle.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys


MAP_REL = "Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset"
GRID_REL = "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"
SURFACE_REL = "Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset"
SCRIPT_META_REL = "Assets/Game/Scripts/Configs/OperationMapDefinition.cs.meta"
PARSER_REL = "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb"
LAYOUT_REL = "Tools/CI/SkirmishSourceSimulation/Fixtures/S026/ground-layout.v2.json"
MATRIX_REL = "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv"
LAYOUT_SHA = "669c8b20f55890b37657605f9c0d67087f6d4da672d2e0eae2ab9d78ca2ca14e"
LAYOUT_FINGERPRINT = "c74d05162216ff09c5b953966a35e7d56bf9f1935a55cf7076d23fe23badc18e"
PROFILES = {
    "objective": "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishObjective_BA.asset",
    "army": "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishArmy_GroundManeuver.asset",
    "start": "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishStart_Established.asset",
    "economy": "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishEconomy_V1.asset",
    "upgrades": "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishUpgrade_V1.asset",
    "intel": "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishIntel_SharedFog.asset",
    "roles": "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishRoleCatalog_V1.asset",
}


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def reject_symlink_ancestry(path):
    path = Path(path).absolute()
    current = Path(path.anchor)
    for part in path.parts[1:]:
        current = current / part
        if current.exists() or current.is_symlink():
            require(not current.is_symlink(), "Symlink path component is not admitted: " + str(current))


def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate JSON key: " + key)
        result[key] = value
    return result


def load_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=no_duplicates,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Nonfinite JSON")))


def pin(path):
    raw_path = Path(path)
    reject_symlink_ancestry(raw_path)
    path = raw_path.resolve(strict=True)
    require(path.is_file(), "Regular input required: " + str(path))
    return {"path": str(path), "sha256": sha(path)}


def descriptor(path, digest):
    reject_symlink_ancestry(path)
    path = Path(path).resolve(strict=True)
    require(path.is_file(), "Regular input required: " + str(path))
    return {"path": str(path), "sha256": digest}


def regular_path(path):
    path = Path(path)
    reject_symlink_ancestry(path)
    resolved = path.resolve(strict=True)
    require(resolved.is_file(), "Regular file input required: " + str(resolved))
    return resolved


def profile_graph_checks(graph, repo):
    nodes = graph.get("nodes")
    pins = graph.get("source_pins")
    require(graph.get("schema_version") == 1 and graph.get("authority") == "AuthoredConfigAssets" and
            graph.get("compiled_setup") is False and graph.get("world_initialized") is False and
            graph.get("native_world_state_used") is False,
            "Source-only AuthoredConfigAssets graph required")
    require(isinstance(nodes, list) and isinstance(pins, dict) and 0 < len(nodes) < 512,
            "Bounded source-config graph required")
    paths = {row.get("source_path") for row in nodes if isinstance(row, dict)}
    require(set(PROFILES.values()).issubset(paths), "Base graph must contain all seven current S026 profiles")
    require("Assets/Game/Resources/SkirmishExpansionCatalog.asset" in paths,
            "Base graph must contain the current authored expansion catalog")
    for rel in [*PROFILES.values(), "Assets/Game/Resources/SkirmishExpansionCatalog.asset"]:
        path = repo / rel
        reject_symlink_ancestry(path)
        require(path.is_file() and pins.get(str(path)) == sha(path),
                "Base graph does not pin current source asset: " + rel)
    for raw_path, digest in pins.items():
        reject_symlink_ancestry(raw_path)
        path = Path(raw_path).resolve(strict=True)
        require(path.is_relative_to(repo) and path.is_file() and sha(path) == digest,
                "Base graph source pin is missing, outside repo, or stale: " + raw_path)
    return nodes, pins


def extend_graph(repo, base_graph_path, base_graph_sha):
    base_graph_path = regular_path(base_graph_path)
    require(base_graph_path.stat().st_size <= 16 * 1024 * 1024, "Base source graph exceeds byte limit")
    require(sha(base_graph_path) == base_graph_sha, "Base source graph hash mismatch")
    graph = load_json(base_graph_path)
    nodes, pins = profile_graph_checks(graph, repo)
    map_path, grid_path, surface_path = (repo / rel for rel in (MAP_REL, GRID_REL, SURFACE_REL))
    map_meta, script_meta, parser = (repo / rel for rel in (MAP_REL + ".meta", SCRIPT_META_REL, PARSER_REL))
    for path in (map_path, map_meta, grid_path, surface_path, script_meta, parser):
        reject_symlink_ancestry(path)
        require(path.is_file(), "Required source input missing: " + str(path))

    source_digests = {str(path): sha(path) for path in
                      (map_path, map_meta, grid_path, surface_path, script_meta, parser)}
    parser_sha = source_digests[str(parser)]
    script_meta_sha = source_digests[str(script_meta)]
    parsed = subprocess.run(["ruby", str(parser), str(map_path)], check=True, capture_output=True,
                            text=True, timeout=30)
    require(all(sha(path) == digest for path, digest in source_digests.items()),
            "S026 OperationMap parser/meta/source input changed during YAML readback")
    document = json.loads(parsed.stdout, object_pairs_hook=no_duplicates)[str(map_path)]
    mono = document.get("MonoBehaviour")
    require(isinstance(mono, dict), "Selected S026 OperationMap YAML readback required")
    script = mono.get("m_Script")
    script_guid = next((line.split(":", 1)[1].strip() for line in script_meta.read_text().splitlines()
                        if line.startswith("guid:")), None)
    require(script == {"fileID": 11500000, "guid": script_guid, "type": 3} and
            mono.get("m_Name") == Path(MAP_REL).stem and
            mono.get("m_EditorClassIdentifier") == "Game.Configs::Game.Configs.OperationMapDefinition" and
            mono.get("operationMapId") == "opmap.skirmish.desert_base_01",
            "Selected S026 source identity or OperationMap script binding changed")

    meta_guid = next((line.split(":", 1)[1].strip() for line in map_meta.read_text().splitlines()
                      if line.startswith("guid:")), None)
    require(isinstance(meta_guid, str) and len(meta_guid) == 32, "Exact source map GUID required")
    existing = [row for row in nodes if row.get("source_path") == MAP_REL or row.get("asset_guid") == meta_guid]
    require(not existing, "Selected S026 source map already exists in graph")

    fields = {key: value for key, value in mono.items() if not key.startswith("m_")}
    grid_guid = fields.get("gridMetadata", {}).get("assetGuid")
    surface_guid = fields.get("surfaceMetadata", {}).get("assetGuid")
    grid_nodes = [row for row in nodes if row.get("asset_guid") == grid_guid]
    surface_nodes = [row for row in nodes if row.get("asset_guid") == surface_guid]
    require(len(grid_nodes) == len(surface_nodes) == 1 and
            grid_nodes[0].get("source_path") == GRID_REL and surface_nodes[0].get("source_path") == SURFACE_REL,
            "Selected map's grid/surface references must resolve uniquely in base graph")
    for path, node in ((grid_path, grid_nodes[0]), (surface_path, surface_nodes[0])):
        digest = source_digests[str(path)]
        require(digest == node.get("source_sha256") and pins.get(str(path)) == digest,
                "Selected map dependency is not pinned to current YAML: " + str(path))
    require(fields.get("gridMetadata", {}).get("contentHash") == grid_nodes[0]["source_sha256"] and
            fields.get("surfaceMetadata", {}).get("contentHash") == surface_nodes[0]["source_sha256"],
            "Selected map's serialized dependency hashes differ from actual source nodes")

    spellings = document.get("__source_scalar_spellings", {})
    scalar_spellings = {key[len("MonoBehaviour."):]: value for key, value in spellings.items()
                        if key.startswith("MonoBehaviour.")}
    require(bool(scalar_spellings), "Serialized source scalar spellings required")
    node = {"asset_guid": meta_guid, "source_type": "OperationMapDefinition", "source_path": MAP_REL,
            "source_sha256": source_digests[str(map_path)], "fields": fields, "scalar_spellings": scalar_spellings}
    nodes.append(node)
    pins[str(map_path)] = source_digests[str(map_path)]
    pins[str(map_meta)] = source_digests[str(map_meta)]
    pins[str(parser)] = parser_sha
    pins[str(script_meta)] = script_meta_sha
    require(all(sha(path) == digest for path, digest in source_digests.items()),
            "S026 OperationMap parser/meta/source input changed before graph emission")
    require(len(nodes) < 512, "Source graph exceeds bounded node count")
    return graph, source_digests, base_graph_path


def verify_pins(pins):
    for path, digest in pins.items():
        reject_symlink_ancestry(path)
        require(sha(path) == digest, "Source input changed before authoring packet emission: " + path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--base-graph", required=True, type=Path)
    parser.add_argument("--base-graph-sha256", required=True)
    parser.add_argument("--setup-matrix", required=True, type=Path)
    parser.add_argument("--setup-matrix-sha256", required=True)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--size", choices=("Standard", "War", "LargeWar"), required=True)
    parser.add_argument("--difficulty", choices=("Recruit", "Regular", "Veteran", "Commander"), default="Regular")
    parser.add_argument("--seed", type=int, default=104755)
    args = parser.parse_args()

    raw_repo = args.repo.absolute()
    reject_symlink_ancestry(raw_repo)
    repo = raw_repo.resolve(strict=True)
    output = args.output.absolute()
    reject_symlink_ancestry(output.parent)
    require(not output.exists(), "Fresh output directory required")
    require(args.seed == 104755 and args.difficulty == "Regular",
            "Only S026 seed 104755 / Regular authoring is currently admitted")
    layout = repo / LAYOUT_REL
    reject_symlink_ancestry(layout)
    require(layout.is_file(), "Checked-in S026 V2 layout fixture is missing")
    layout_sha = sha(layout)
    matrix = regular_path(args.setup_matrix)
    expected_matrix = repo / MATRIX_REL
    reject_symlink_ancestry(expected_matrix)
    expected_matrix = expected_matrix.resolve(strict=True)
    require(matrix == expected_matrix,
            "Use the current authored SkirmishExpansion InitialSetupMatrix.csv")
    matrix_sha = sha(matrix)
    require(layout_sha == LAYOUT_SHA, "Checked-in S026 V2 layout fixture changed")
    require(matrix_sha == args.setup_matrix_sha256, "Setup matrix hash mismatch")

    preparer_script = Path(__file__).absolute()
    reject_symlink_ancestry(preparer_script)
    preparer_script_sha = sha(preparer_script)
    graph, source_digests, base_graph_path = extend_graph(repo, args.base_graph, args.base_graph_sha256)
    source_pins = graph["source_pins"]
    final_pins = dict(source_pins)
    final_pins[str(base_graph_path)] = args.base_graph_sha256
    final_pins[str(layout)] = layout_sha
    final_pins[str(matrix)] = matrix_sha
    final_pins[str(preparer_script)] = preparer_script_sha
    verify_pins(final_pins)
    output.mkdir(parents=True)
    graph_path = output / "source-config-graph.json"
    graph_path.write_text(json.dumps(graph, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    draft = {
        "schema_version": 1,
        "kind": "SkirmishMissionAuthoringDraft",
        "catalog_id": "S026",
        "seed": args.seed,
        "size": args.size,
        "difficulty": args.difficulty,
        "graph": pin(graph_path),
        "profiles": PROFILES,
        "campaign_source_path": MAP_REL,
        "layout": descriptor(layout, layout_sha),
        "setup_matrix": descriptor(matrix, matrix_sha),
    }
    draft_path = output / "s026-authoring-input.json"
    draft_path.write_text(json.dumps(draft, indent=2) + "\n", encoding="utf-8")
    receipt = {
        "schema_version": 1,
        "kind": "S026SourceBackedAuthoringInput",
        "status": "Prepared",
        "selection": {"catalog_id": "S026", "seed": args.seed, "size": args.size,
                      "difficulty": args.difficulty},
        "graph": pin(graph_path),
        "selected_operation_map": descriptor(repo / MAP_REL, source_digests[str(repo / MAP_REL)]),
        "layout": descriptor(layout, layout_sha),
        "layout_fingerprint": LAYOUT_FINGERPRINT,
        "setup_matrix": descriptor(matrix, matrix_sha),
        "profile_source_paths": PROFILES,
        "preparer_script": {"path": str(preparer_script), "sha256": preparer_script_sha},
        "readback_parser": {"path": str(repo / PARSER_REL), "sha256": source_pins[str(repo / PARSER_REL)]},
        "operation_map_script_meta": {"path": str(repo / SCRIPT_META_REL), "sha256": source_pins[str(repo / SCRIPT_META_REL)]},
        "world_allocated": False,
        "metadata_synthesized": False,
        "native_capture_created": False,
        "mission_bundle_created": False,
        "catalog_status_changed": False,
        "scope": "Source-backed authoring input only; run through author for compiler consistency. No runtime or readiness evidence.",
        "authoring_input": pin(draft_path),
    }
    verify_pins(final_pins)
    require(sha(preparer_script) == preparer_script_sha,
            "S026 authoring packager changed before preparation receipt emission")
    (output / "preparation-receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(receipt, separators=(",", ":")))


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, subprocess.SubprocessError, json.JSONDecodeError) as error:
        print("S026 authoring input preparation failed: " + str(error), file=sys.stderr)
        raise SystemExit(2)
