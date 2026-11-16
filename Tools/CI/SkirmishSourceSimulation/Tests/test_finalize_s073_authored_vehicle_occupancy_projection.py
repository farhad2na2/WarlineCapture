import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from finalize_s073_authored_vehicle_occupancy_projection import finalize


SCENE_GUID = "c00140f2e94a04c3084c8dcb0c18cbd0"
MAP_CONFIG = "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"
SCENE_PATH = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
SCENE_META = SCENE_PATH + ".meta"
PIN_PATHS = [MAP_CONFIG, SCENE_PATH, SCENE_META,
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
    "Design/AgentReports/2026-07-21_dense_city_phase0a_vehicle_ecs_conversion_inventory.json"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def make_evidence(root):
    for relative in PIN_PATHS:
        path = root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        content = ("operationMapId: opmap.skirmish.industrial_basin\n"
                   "sourceOperationMapId: opmap.skirmish.desert_base_01\n"
                   "authoredSubSceneGuid: " + SCENE_GUID + "\n") if relative == MAP_CONFIG else (
                       "guid: " + SCENE_GUID + "\n" if relative == SCENE_META else relative)
        path.write_text(content)
    pins = {p: digest(root / p) for p in PIN_PATHS}
    vehicles = []
    for i in range(22):
        air = i < 10
        baked_cell = [0, 0]
        position = [200.25 + i, 3.0, 200.25] if air else [i + 0.25, 0.0, 10.25]
        candidate_cell = [int(position[0]), int(position[2])]
        effective_cell = baked_cell if air else candidate_cell
        size = [3, 3] if i == 0 else [1, 1]
        min_x = effective_cell[0] - (size[0] - 1) // 2
        min_y = effective_cell[1] - (size[1] - 1) // 2
        min_clamped = [max(0, min(2048, min_x)), max(0, min(1024, min_y))]
        max_clamped = [max(0, min(2048, min_x + size[0])), max(0, min(1024, min_y + size[1]))]
        vehicles.append({"source_global_object_id": "GlobalObjectId_V1-source-" + str(i),
                         "source_operation_map_id": "opmap.skirmish.desert_base_01",
                         "presentation_role": "GameplayVehicles", "placement_index": i,
                         "unit_source_prefab_key": "Unit_Veh_Test", "scene_guid": SCENE_GUID,
                         "baked_unit_grid_cell": baked_cell,
                         "authored_local_transform_position": position,
                         "unit_air_movement_component_present": air,
                         "unit_grid_initialized_component_present": False,
                         "candidate_cell_from_authored_transform": candidate_cell,
                         "pre_simulation_effective_cell": effective_cell,
                         "authored_footprint": size, "kernel_footprint": size,
                         "kernel_nominal_bounds_min": [min_x, min_y],
                         "kernel_nominal_bounds_max_exclusive": [min_x + size[0], min_y + size[1]],
                         "kernel_bounds_min": min_clamped, "kernel_bounds_max_exclusive": max_clamped})
    return {
        "kind": "S073AuthoredVehicleOccupancyProjectionInputs", "schema_version": 1,
        "status": "Captured", "scope": "IsolatedPreGameStreamedCampaignSourceScene_AuthoredNeutralPhysicalOccupancyInputsOnly",
        "logical_operation_map_id": "opmap.skirmish.industrial_basin",
        "source_campaign_map_id": "opmap.skirmish.desert_base_01", "source_scene_guid": SCENE_GUID,
        "source_scene_path": SCENE_PATH, "source_scene_sha256": pins[SCENE_PATH],
        "source_scene_meta_sha256": pins[SCENE_META],
        "logical_map_config_path": MAP_CONFIG, "logical_map_config_sha256": pins[MAP_CONFIG],
        "unity_version": "test", "source_streamed_in_isolated_world": True, "gameplay_systems_ran": False,
        "runtime_campaign_state_read": False, "runtime_campaign_state_copied": False,
        "faction_or_economy_values_captured": False, "presentation_root_count": 1,
        "grid": {"width": 2048, "height": 1024, "cell_size": 1.0, "origin": [0.0, 0.0, 0.0]},
        "occupancy_query": {"all": ["UnitGrid", "UnitFootprint", "UnitSourcePrefabKey", "LocalTransform", "SceneTag"],
            "required_vehicle_marker": "OperationMapAuthoredVehiclePresentation",
            "identity_join": "UnitDetailedVisualReference.Root -> OperationMapEntityPresentationIdentity",
            "none": ["Prefab", "Disabled"],
            "additional_occupancy_exclusions_verified_absent": ["StaticGridBlocker", "RuntimeBuildingCombatTag", "UnitDeathAnimationComponent"],
            "reference_owner": "DynamicOccupancyRebuildSystem._trackedUnitsQuery"},
        "cell_projection_policy": "Read-only reproduction of UnitGridSnapSystem.InitializeAuthoredVehicleGridJob: authored vehicles without UnitAirMovement or UnitGridInitialized use GridUtils.WorldToCell(LocalTransform.Position); either excluded component retains the captured baked UnitGrid cell. This is pre-simulation projection, not proof the owner ran.",
        "neutralization_policy": "SkirmishWorldSetup.NormalizeScenery assigns faction 0 and removes AI/squad membership; no faction value was read or serialized here",
        "vehicles": vehicles, "vehicle_count": 22, "source_pins_sha256": pins,
        "source_fit_validated": False, "source_runtime_validated": False,
        "scheduler_admitted": False, "mission_ready": False,
    }


class AuthoredVehicleProjectionTests(unittest.TestCase):
    def test_projection_is_cell_union_not_runtime_state(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            capture = root / "capture.json"
            capture.write_text(json.dumps(make_evidence(root)))
            result = finalize(capture, root)
            self.assertEqual(result["vehicle_count"], 22)
            self.assertEqual(result["occupied_cell_count"], 16)
            self.assertEqual(result["out_of_bounds_footprint_cell_references"], 5)
            self.assertEqual(result["vehicles_with_partially_clipped_footprints"], 1)
            self.assertEqual(result["status"], "ProjectedWithClippingEvidence")
            self.assertEqual(result["vehicles"][0]["candidate_cell_from_authored_transform"], [200, 200])
            self.assertEqual(result["vehicles"][0]["pre_simulation_effective_cell"], [0, 0])
            self.assertFalse(result["campaign_state_copied"])
            self.assertFalse(result["mission_ready"])

    def test_rejects_faction_or_runtime_state_fields(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            capture_data = make_evidence(root)
            capture_data["vehicles"][0]["faction_id"] = 1
            capture = root / "capture.json"
            capture.write_text(json.dumps(capture_data))
            with self.assertRaisesRegex(ValueError, "unknown fields"):
                finalize(capture, root)

    def test_rejects_kernel_bounds_tamper(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            capture_data = make_evidence(root)
            capture_data["vehicles"][0]["kernel_bounds_min"] = [1, 10]
            capture = root / "capture.json"
            capture.write_text(json.dumps(capture_data))
            with self.assertRaisesRegex(ValueError, "production GridOccupancyKernel.Bounds"):
                finalize(capture, root)


if __name__ == "__main__":
    unittest.main()
