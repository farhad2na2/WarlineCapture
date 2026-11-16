#!/usr/bin/env python3
"""Audit the pinned S004 authored map data against its native publication owners.

This is deliberately an owner/source-contract diagnostic. It does not create a
Unity World or claim Baker, EntityScene, scene, or physical-fit evidence.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
R13 = ROOT / "T Logs/SkirmishProduction/s004-cityedge-physical-input-source-projection-20261103-r13"
GRAPH = R13 / "source-config-graph-with-cityedge-physical-inputs.json"
PROJECTION = R13 / "source-projection.json"
OUT = R13.parent / "s004-source-map-publication-diagnostic-20261103-r1"

OWNERS = {
    "operation_map_publisher": "Assets/Game/Scripts/Composition/OperationMapRuntimeBootstrapSceneSystemHelper.cs",
    "scene_owner": "Assets/Game/Scripts/Composition/OperationMapSceneLoadingSceneSystemHelper.cs",
    "scene_view_resolver": "Assets/Game/Scripts/Composition/OperationMapSceneReferenceSceneSystemHelper.cs",
    "startup_projection": "Assets/Game/Scripts/Composition/MatchBootstrapStartupConfigProjection.cs",
    "grid_compatibility_publisher": "Assets/Game/Scripts/Systems/RuntimeGridBootstrapStartupSystemHelper.cs",
    "grid_authoring": "Assets/Game/Scripts/Authorings/GridAuthoring.cs",
    "surface_authoring": "Assets/Game/Scripts/Authorings/MapSurfaceAuthoring.cs",
    "surface_publisher": "Assets/Game/Scripts/Composition/MapSurfaceRuntimeBootstrapSceneSystemHelper.cs",
}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def source(rel):
    path = ROOT / rel
    return path.read_text(encoding="utf-8"), path

def require(text, needle, label):
    if needle not in text:
        raise RuntimeError(f"owner contract missing: {label}: {needle}")

def main():
    graph, projection = json.loads(GRAPH.read_text()), json.loads(PROJECTION.read_text())
    # Pin the exact source assets from the r13 receipt's projection and graph.
    source_map = projection["source_map"]
    grid, surface = projection["grid"], projection["surface"]
    pinned = {
        source_map["path"]: source_map["sha256"],
        grid["path"]: grid["sha256"],
        surface["path"]: surface["sha256"],
    }
    for rel, expected in pinned.items():
        if sha(ROOT / rel) != expected:
            raise RuntimeError(f"pinned input changed: {rel}")
    if graph.get("authority") != "AuthoredConfigAssets":
        raise RuntimeError("typed config graph is not authored asset data")

    owner_text = {key: source(rel)[0] for key, rel in OWNERS.items()}
    require(owner_text["operation_map_publisher"], "OperationMapGridSourceRecordWriter.Bind", "root source binding")
    require(owner_text["scene_owner"], "TryEnsureReady", "EntityScene ownership gate")
    require(owner_text["scene_view_resolver"], "scene.isLoaded", "loaded scene gate")
    require(owner_text["startup_projection"], "runtimeGridBootstrapSystem.Ensure", "grid helper call")
    require(owner_text["startup_projection"], "mapSurfaceRuntimeBootstrapSystem.Ensure", "surface helper call")
    require(owner_text["grid_compatibility_publisher"], "GridWalkableSourceWitness", "compatibility witness invalidation")
    require(owner_text["grid_compatibility_publisher"], "InitializeCells(", "compatibility walkability projection")
    require(owner_text["grid_authoring"], "class GridAuthoring", "authored grid baker owner")
    require(owner_text["surface_authoring"], "class MapSurfaceAuthoring", "authored surface baker owner")
    require(owner_text["surface_publisher"], "TryCreateRuntimeBlobAsset", "original surface decoder")

    OUT.mkdir(parents=True, exist_ok=True)
    result = {
        "kind": "S004SourceMapPublicationOwnerDiagnostic",
        "status": "Passed",
        "mode": "ReadOnlyPinnedSourceContractAudit",
        "inputs": {
            "typed_graph": {"path": str(GRAPH), "sha256": sha(GRAPH)},
            "r13_projection": {"path": str(PROJECTION), "sha256": sha(PROJECTION)},
            "pinned_assets": pinned,
        },
        "source_facts": {
            "source_operation_map_id": source_map["operation_map_id"],
            "source_binding_operation_map_id": source_map["source_binding_operation_map_id"],
            "source_scene_guid": source_map["source_scene_guid"],
            "grid_dimensions": grid["dimensions"],
            "grid_blocked_count": grid["authored_blocked_cell_count"],
            "walkability_projection_scope": grid["projection_scope"],
            "surface_runtime_blob_hash": surface["decoded"]["runtime_blob_hash"],
            "surface_runtime_hash_matches_map_metadata": surface["decoded"]["runtime_blob_hash_metadata_match"],
        },
        "owners": {
            "map_root": OWNERS["operation_map_publisher"],
            "actual_entity_scene_owner": OWNERS["scene_owner"],
            "loaded_scene_view_gate": OWNERS["scene_view_resolver"],
            "normal_startup_orchestrator": OWNERS["startup_projection"],
            "grid_authored_baker": OWNERS["grid_authoring"],
            "surface_authored_baker": OWNERS["surface_authoring"],
            "surface_runtime_blob_publisher": OWNERS["surface_publisher"],
            "compatibility_grid_publisher": OWNERS["grid_compatibility_publisher"],
        },
        "seam_assessment": {
            "single_entity_manager_normal_path": "MatchBootstrapStartupConfigProjection.ProjectRuntimeStartupConfig obtains source-backed config from MatchSceneView, resolves map metadata, then calls the runtime grid and surface helpers on the supplied World EntityManager.",
            "grid_helper_caveat": "RuntimeGridBootstrapStartupSystemHelper.Ensure is not native Baker evidence: it removes GridWalkableSourceWitness, rewrites walkability from blocked cells, and ensures default dynamic components/storage.",
            "smallest_native_source_seam": "Use the actual loaded OperationMapSceneView's GridAuthoringConfig and MapSurfaceAuthoring, with OperationMapRuntimeBootstrapSceneSystemHelper root publication and MapSurfaceRuntimeBootstrapSceneSystemHelper.Ensure in the same World. GridWalkable should come from the actual GridAuthoring Baker output; do not substitute compatibility Ensure if source-witness preservation is required.",
        },
        "execution": {
            "unity_or_baker_run": False,
            "entity_manager_created": False,
            "native_scene_loaded": False,
            "native_grid_readback": False,
            "occupancy_written": False,
            "physical_fit_verified": False,
            "readiness_claimed": False,
            "reason": "This isolated audit validates pinned source and ownership contracts. The source projection itself records no Unity run; executing Unity helpers requires a World and Unity asset/scene loading path, outside this read-only source-host audit.",
        },
    }
    (OUT / "diagnostic.json").write_text(json.dumps(result, indent=2) + "\n")
    receipt = {"kind": result["kind"], "status": result["status"], "diagnostic": str(OUT / "diagnostic.json"),
               "diagnostic_sha256": sha(OUT / "diagnostic.json"), "mode": result["mode"]}
    (OUT / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print("[S004SourceMapPublicationOwnerDiagnostic] result=Passed source_contracts=9 native_world=false scene_loaded=false fit=false")

if __name__ == "__main__":
    main()
