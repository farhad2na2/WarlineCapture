#!/usr/bin/env python3
"""Strict offline reduction of isolated authored-scene vehicle placement facts."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import struct
from pathlib import Path

SCENE_GUID = "c00140f2e94a04c3084c8dcb0c18cbd0"
LOGICAL_MAP_ID = "opmap.skirmish.industrial_basin"
SOURCE_MAP_ID = "opmap.skirmish.desert_base_01"
MAP_CONFIG = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
EXPECTED_COUNT = 22
ALLOWED_TOP = {
    "kind", "schema_version", "status", "scope", "logical_operation_map_id", "source_campaign_map_id",
    "source_scene_guid", "source_scene_path", "source_scene_sha256", "logical_map_config_path",
    "source_scene_meta_sha256", "logical_map_config_sha256", "unity_version", "source_streamed_in_isolated_world", "gameplay_systems_ran",
    "runtime_campaign_state_read", "runtime_campaign_state_copied", "faction_or_economy_values_captured",
    "presentation_root_count", "grid", "occupancy_query", "cell_projection_policy", "neutralization_policy", "vehicles", "vehicle_count",
    "source_pins_sha256", "source_fit_validated", "source_runtime_validated", "scheduler_admitted", "mission_ready",
}
ALLOWED_ROW = {"source_global_object_id", "source_operation_map_id", "presentation_role", "placement_index",
               "unit_source_prefab_key", "scene_guid", "baked_unit_grid_cell", "authored_local_transform_position",
               "unit_air_movement_component_present", "unit_grid_initialized_component_present",
               "candidate_cell_from_authored_transform",
               "pre_simulation_effective_cell", "authored_footprint", "kernel_footprint",
               "kernel_nominal_bounds_min", "kernel_nominal_bounds_max_exclusive",
               "kernel_bounds_min", "kernel_bounds_max_exclusive"}


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bounds(width: int, height: int, x: int, y: int, sx: int, sy: int) -> tuple[list[int], list[int]]:
    sx, sy = max(1, sx), max(1, sy)
    min_x, min_y = x - (sx - 1) // 2, y - (sy - 1) // 2
    return ([max(0, min(width, min_x)), max(0, min(height, min_y))],
            [max(0, min(width, min_x + sx)), max(0, min(height, min_y + sy))])


def f32(value: float) -> float:
    return struct.unpack("<f", struct.pack("<f", value))[0]


def grid_utils_world_to_cell(position: list[float], origin: list[float], cell_size: float) -> list[int]:
    local_x = f32(f32(position[0]) - f32(origin[0]))
    local_z = f32(f32(position[2]) - f32(origin[2]))
    quotient_x = f32(local_x / f32(cell_size))
    quotient_z = f32(local_z / f32(cell_size))
    need(math.isfinite(quotient_x) and math.isfinite(quotient_z), "Grid conversion produced a non-finite cell")
    return [math.floor(quotient_x), math.floor(quotient_z)]


def finalize(capture_path: Path, project_root: Path) -> dict:
    capture_path = capture_path.resolve(strict=True)
    root = project_root.resolve(strict=True)
    capture = json.loads(capture_path.read_text(encoding="utf-8"))
    need(set(capture) == ALLOWED_TOP, "Projection input has unknown/missing fields; runtime state is rejected")
    need(capture.get("kind") == "S073AuthoredVehicleOccupancyProjectionInputs" and
         capture.get("schema_version") == 1 and capture.get("status") == "Captured" and
         capture.get("scope") == "IsolatedPreGameStreamedCampaignSourceScene_AuthoredNeutralPhysicalOccupancyInputsOnly",
         "Exact isolated authored-scene input scope required")
    need(capture.get("logical_operation_map_id") == LOGICAL_MAP_ID and
         capture.get("source_campaign_map_id") == SOURCE_MAP_ID and
         capture.get("source_scene_guid") == SCENE_GUID and capture.get("source_scene_path", "").startswith("Assets/"),
         "Selected map/source scene identity mismatch")
    need(capture.get("source_streamed_in_isolated_world") is True and
         capture.get("gameplay_systems_ran") is False and
         capture.get("runtime_campaign_state_read") is False and
         capture.get("runtime_campaign_state_copied") is False and
         capture.get("faction_or_economy_values_captured") is False,
         "Isolated source-only/no Campaign state contract failed")
    need(capture.get("source_fit_validated") is False and capture.get("source_runtime_validated") is False and
         capture.get("scheduler_admitted") is False and capture.get("mission_ready") is False,
         "Projection must disclaim source fit, runtime, scheduler and readiness")
    need(capture.get("presentation_root_count") == 1 and
         capture.get("neutralization_policy") ==
         "SkirmishWorldSetup.NormalizeScenery assigns faction 0 and removes AI/squad membership; no faction value was read or serialized here",
         "Selected authored GameplayVehicles root or neutralization policy mismatch")

    scene_rel = capture["source_scene_path"]
    need(scene_rel == "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity",
         "Scene path differs from selected source scene")
    map_path = root / MAP_CONFIG
    scene_path = root / scene_rel
    scene_meta_path = root / (scene_rel + ".meta")
    need(map_path.is_file() and scene_path.is_file() and scene_meta_path.is_file(), "Pinned source scene/map asset missing")
    need(sha(map_path) == capture["logical_map_config_sha256"] and
         sha(scene_path) == capture["source_scene_sha256"] and
         sha(scene_meta_path) == capture["source_scene_meta_sha256"], "Map or scene asset hash differs from capture")
    need("guid: " + SCENE_GUID in scene_meta_path.read_text(encoding="utf-8"), "Scene meta GUID differs from selected map binding")
    map_text = map_path.read_text(encoding="utf-8")
    need("operationMapId: " + LOGICAL_MAP_ID in map_text and
         "sourceOperationMapId: " + SOURCE_MAP_ID in map_text and
         "authoredSubSceneGuid: " + SCENE_GUID in map_text,
         "Selected logical map no longer binds the captured Campaign source scene")

    pins = capture.get("source_pins_sha256")
    need(isinstance(pins, dict) and len(pins) >= 15, "Complete source code/config pin set required")
    for relative, expected in pins.items():
        path = root / relative
        need(not Path(relative).is_absolute() and ".." not in Path(relative).parts and path.is_file() and
             isinstance(expected, str) and sha(path) == expected,
             "Captured source pin mismatch: " + relative)
    required_pins = {MAP_CONFIG, scene_rel, scene_rel + ".meta",
        "Assets/Game/Scripts/Editor/Skirmish/S073AuthoredVehicleOccupancyProjectionCapture.cs",
        "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
        "Assets/Game/Scripts/Authorings/OperationMapEntityPresentationIdentityAuthoring.cs",
        "Assets/Game/Scripts/Authorings/OperationMapAuthoredVehicleOwnershipAuthoring.cs",
        "Assets/Game/Scripts/Editor/OperationMapVehicleCandidateMigrationEditor.cs",
        "Assets/Game/Scripts/Components/OperationMapEntityPresentationComponents.cs",
        "Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs",
        "Assets/Game/Scripts/Systems/GridOccupancyKernel.cs",
        "Assets/Game/Scripts/Systems/UnitGridSnapSystem.cs",
        "Assets/Game/Scripts/Components/GridCoordinateKernel.cs",
        "Assets/Game/Scripts/Components/GridComponents.cs",
        "Assets/Game/Scripts/Components/UnitVisualComponents.cs",
        "Design/AgentReports/2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory.json"}
    need(required_pins <= set(pins), "Required authoring/query/kernel provenance pins absent")

    grid = capture.get("grid") or {}
    need(set(grid) == {"width", "height", "cell_size", "origin"} and
         grid.get("width") == 2048 and grid.get("height") == 1024 and grid.get("cell_size") == 1.0 and
         grid.get("origin") == [0.0, 0.0, 0.0],
         "Expected full S073 source grid frame required")
    query = capture.get("occupancy_query") or {}
    need(query.get("all") == ["UnitGrid", "UnitFootprint", "UnitSourcePrefabKey", "LocalTransform", "SceneTag"] and
         query.get("required_vehicle_marker") == "OperationMapAuthoredVehiclePresentation" and
         query.get("identity_join") == "UnitDetailedVisualReference.Root -> OperationMapEntityPresentationIdentity" and
         query.get("none") == ["Prefab", "Disabled"] and
         query.get("additional_occupancy_exclusions_verified_absent") ==
         ["StaticGridBlocker", "RuntimeBuildingCombatTag", "UnitDeathAnimationComponent"] and
         query.get("reference_owner") == "DynamicOccupancyRebuildSystem._trackedUnitsQuery",
         "Authored vehicle query/membership contract mismatch")
    need(capture.get("cell_projection_policy") ==
         "Read-only reproduction of UnitGridSnapSystem.InitializeAuthoredVehicleGridJob: authored vehicles without UnitAirMovement or UnitGridInitialized use GridUtils.WorldToCell(LocalTransform.Position); either excluded component retains the captured baked UnitGrid cell. This is pre-simulation projection, not proof the owner ran.",
         "Authored cell projection policy mismatch")
    rows = capture.get("vehicles")
    need(isinstance(rows, list) and len(rows) == EXPECTED_COUNT and capture.get("vehicle_count") == EXPECTED_COUNT,
         "Exactly 22 source GameplayVehicles placements required")
    placements, global_ids = set(), set()
    by_vehicle = []
    occupied = set()
    out_of_bounds_references = 0
    partially_clipped = 0
    wholly_outside = 0
    effective_centers_outside = 0
    transform_centers_outside = 0
    for row in rows:
        need(set(row) == ALLOWED_ROW, "Vehicle row contains unknown fields (including forbidden runtime state)")
        source_id, index, key = row.get("source_global_object_id"), row.get("placement_index"), row.get("unit_source_prefab_key")
        need(isinstance(source_id, str) and source_id.startswith("GlobalObjectId_V1-") and source_id not in global_ids and
             isinstance(index, int) and 0 <= index < EXPECTED_COUNT and index not in placements and
             isinstance(key, str) and key.startswith("Unit_Veh_"),
             "Unique stable source identity/placement and vehicle prefab key required")
        need(row.get("source_operation_map_id") == SOURCE_MAP_ID and
             row.get("presentation_role") == "GameplayVehicles" and row.get("scene_guid") == SCENE_GUID,
             "Vehicle row is not an authored source GameplayVehicles placement")
        baked_cell = row.get("baked_unit_grid_cell")
        transform_position = row.get("authored_local_transform_position")
        air = row.get("unit_air_movement_component_present")
        initialized = row.get("unit_grid_initialized_component_present")
        candidate_cell = row.get("candidate_cell_from_authored_transform")
        effective_cell = row.get("pre_simulation_effective_cell")
        authored = row.get("authored_footprint")
        need(isinstance(baked_cell, list) and len(baked_cell) == 2 and all(isinstance(v, int) for v in baked_cell) and
             isinstance(transform_position, list) and len(transform_position) == 3 and
             all(isinstance(v, (int, float)) and math.isfinite(v) for v in transform_position) and
             isinstance(air, bool) and isinstance(initialized, bool) and
             isinstance(candidate_cell, list) and len(candidate_cell) == 2 and
             all(isinstance(v, int) for v in candidate_cell) and
             isinstance(effective_cell, list) and len(effective_cell) == 2 and all(isinstance(v, int) for v in effective_cell) and
             isinstance(authored, list) and len(authored) == 2 and all(isinstance(v, int) and v >= 1 for v in authored),
             "Invalid source UnitGrid/LocalTransform/UnitFootprint facts")
        expected_candidate = grid_utils_world_to_cell(transform_position, grid["origin"], grid["cell_size"])
        need(candidate_cell == expected_candidate, "Candidate cell differs from pinned GridUtils.WorldToCell")
        expected_effective = baked_cell if air or initialized else expected_candidate
        need(effective_cell == expected_effective,
             "Effective pre-simulation cell does not follow UnitGridSnapSystem air exclusion")
        expected_min, expected_max = bounds(grid["width"], grid["height"], effective_cell[0], effective_cell[1], authored[0], authored[1])
        expected_size = [max(1, authored[0]), max(1, authored[1])]
        nominal_min = [effective_cell[0] - (expected_size[0] - 1) // 2,
                       effective_cell[1] - (expected_size[1] - 1) // 2]
        nominal_max = [nominal_min[0] + expected_size[0], nominal_min[1] + expected_size[1]]
        need(row.get("kernel_nominal_bounds_min") == nominal_min and
             row.get("kernel_nominal_bounds_max_exclusive") == nominal_max,
             "Nominal footprint differs from production kernel center/size semantics")
        need(row.get("kernel_footprint") == expected_size and row.get("kernel_bounds_min") == expected_min and
             row.get("kernel_bounds_max_exclusive") == expected_max,
             "Source row differs from production GridOccupancyKernel.Bounds")
        cells = {y * grid["width"] + x for y in range(expected_min[1], expected_max[1])
                 for x in range(expected_min[0], expected_max[0])}
        clipped_refs = expected_size[0] * expected_size[1] - len(cells)
        out_of_bounds_references += clipped_refs
        if clipped_refs:
            partially_clipped += 1
        if not cells:
            wholly_outside += 1
        if not (0 <= effective_cell[0] < grid["width"] and 0 <= effective_cell[1] < grid["height"]):
            effective_centers_outside += 1
        if not (0 <= candidate_cell[0] < grid["width"] and 0 <= candidate_cell[1] < grid["height"]):
            transform_centers_outside += 1
        occupied |= cells
        placements.add(index); global_ids.add(source_id)
        by_vehicle.append({"source_global_object_id": source_id, "placement_index": index,
                           "unit_source_prefab_key": key, "baked_unit_grid_cell": baked_cell,
                           "authored_local_transform_position": transform_position,
                           "unit_air_movement_component_present": air,
                           "unit_grid_initialized_component_present": initialized,
                           "candidate_cell_from_authored_transform": candidate_cell,
                           "pre_simulation_effective_cell": effective_cell, "footprint": authored,
                           "nominal_bounds_min": nominal_min, "nominal_bounds_max_exclusive": nominal_max,
                           "clipped_bounds_min": expected_min, "clipped_bounds_max_exclusive": expected_max,
                           "out_of_bounds_footprint_cell_references": clipped_refs,
                           "covered_row_major_cells": sorted(cells)})
    need(placements == set(range(EXPECTED_COUNT)), "Placement indices must cover exactly 0..21")
    by_vehicle.sort(key=lambda v: v["placement_index"])
    return {
        "kind": "S073AuthoredNeutralPhysicalOccupancyProjection",
        "schema_version": 1,
        "status": "ProjectedWithClippingEvidence" if out_of_bounds_references else "Projected",
        "scope": "PreSimulationProjectionFromIsolatedBakedCampaignScene_NotRuntimeCaptureOrSourceValidation",
        "logical_operation_map_id": LOGICAL_MAP_ID,
        "source_campaign_map_id": SOURCE_MAP_ID,
        "source_scene_guid": SCENE_GUID,
        "source_scene_sha256": capture["source_scene_sha256"],
        "source_scene_meta_sha256": capture["source_scene_meta_sha256"],
        "logical_map_config_sha256": capture["logical_map_config_sha256"],
        "capture_input_sha256": sha(capture_path),
        "grid": {k: grid[k] for k in ("width", "height", "cell_size", "origin")},
        "neutralization_basis": "Skirmish authored scenery policy; source ownership/faction state was not read or copied",
        "vehicle_count": len(by_vehicle),
        "vehicle_footprint_cell_references": sum(len(v["covered_row_major_cells"]) for v in by_vehicle),
        "nominal_footprint_cell_references": sum(v["footprint"][0] * v["footprint"][1] for v in by_vehicle),
        "out_of_bounds_footprint_cell_references": out_of_bounds_references,
        "vehicles_with_partially_clipped_footprints": partially_clipped,
        "vehicles_wholly_outside_grid": wholly_outside,
        "effective_center_cells_outside_grid": effective_centers_outside,
        "transform_candidate_cells_outside_grid": transform_centers_outside,
        "occupied_cell_count": len(occupied),
        "occupied_row_major_cells": sorted(occupied),
        "vehicles": by_vehicle,
        "source_pins_sha256": pins,
        "source_state_imported": False,
        "campaign_state_copied": False,
        "state_transfer": False,
        "source_fit_validated": False,
        "scheduler_admitted": False,
        "mission_ready": False,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = finalize(args.capture, args.project_root)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": result["status"], "vehicle_count": result["vehicle_count"],
                      "occupied_cell_count": result["occupied_cell_count"], "mission_ready": False}))


if __name__ == "__main__":
    main()
