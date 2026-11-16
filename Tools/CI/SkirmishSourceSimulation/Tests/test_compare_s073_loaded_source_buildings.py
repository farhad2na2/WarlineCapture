import importlib.util
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SCRIPT = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/compare_s073_loaded_source_buildings.py"
SPEC = importlib.util.spec_from_file_location("compare_s073_loaded_source_buildings", SCRIPT)
COMPARE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(COMPARE)


def fixture():
    native_rows = []
    host_rows = []
    for index in range(4931):
        stable = "stable-" + str(index)
        source = "global-" + str(index)
        position = [float(index), 1.0, float(index + 2)]
        native_rows.append({
            "stable_id": stable, "source_global_object_id": source,
            "placement_index": index, "operation_map_id": COMPARE.SOURCE_MAP,
            "bound_campaign_scene_owned": True, "faction_id": 0, "position": position,
        })
        host_rows.append({
            "stable_id": stable, "source_global_object_id": source,
            "placement_index": index, "faction": 0, "ai": False,
            "combat_owner": 0, "health_current": 5, "health_max": 5,
            "position": list(position),
        })
    native = {
        "kind": "S073NativeWaterMaskObservation", "phase": "Playing",
        "scenario_index": 3, "seed": 104729,
        "operation_map_id": COMPARE.LOGICAL_MAP,
        "source_campaign_map_id": COMPARE.SOURCE_MAP,
        "campaign_gameplay_state_copied": False, "mission_ready": False,
        "source_fit_certified": False, "session_id": "native-session",
        "source_pins_sha256": {COMPARE.SCENE_PATH: "a" * 64, COMPARE.SCENE_PATH + ".meta": "b" * 64},
        "loaded_campaign_source_fit_observation": {
            "scene_loaded": True, "source_scene_guid": COMPARE.SCENE_GUID,
            "source_scene_path": COMPARE.SCENE_PATH,
            "campaign_source_map_id": COMPARE.SOURCE_MAP,
            "source_fit_certified": False, "scheduler_admitted": False,
            "mission_ready": False, "player_readiness": False,
            "world_name": "native-world", "building_entities": native_rows,
        },
    }
    host = {
        "kind": "SkirmishSourceSimulationRunReceipt", "mission": "S073",
        "seed": 104729, "status": "PreparingPhysicalSourceRequired",
        "scheduler_tick_executed": False, "mission_ready": False,
        "operation_map_id": COMPARE.LOGICAL_MAP,
        "operation_map_physical_source_validated": False,
        "session_id": "host-session",
        "experimental_physical_blocker_projection": {
            "neutral_scenery_readback": {
                "count": 4931,
                "scope": "Authoring-derived neutral identity, health and transforms; no resource, production or session state",
            },
        },
    }
    readback = {
        "kind": "S073SourceNeutralSceneryReadback",
        "source_capture_sha256": COMPARE.SOURCE_CAPTURE_SHA256,
        "campaign_runtime_state_copied": False,
        "complete_gameplay_payload": False,
        "mission_ready": False,
        "entities": host_rows,
    }
    return native, host, readback


class LoadedSourceBuildingComparisonTests(unittest.TestCase):
    def test_full_identity_and_position_join_stays_diagnostic(self):
        result = COMPARE.compare(*fixture())
        self.assertEqual(result["status"], "IdentityAndPositionMatch")
        self.assertEqual(result["identity_matches"], 4931)
        self.assertEqual(result["max_position_delta"], 0)
        self.assertFalse(result["native_scene_loaded_in_source_host"])
        self.assertFalse(result["scheduler_admitted"])

    def test_identity_mismatch_rejected(self):
        native, host, readback = fixture()
        readback["entities"][0]["stable_id"] = "other"
        with self.assertRaisesRegex(ValueError, "identities differ"):
            COMPARE.compare(native, host, readback)

    def test_position_drift_is_not_a_pass(self):
        native, host, readback = fixture()
        readback["entities"][0]["position"][1] += 0.01
        result = COMPARE.compare(native, host, readback)
        self.assertEqual(result["status"], "Mismatch")
        self.assertAlmostEqual(result["max_position_delta"], 0.01)

    def test_scheduler_or_readiness_claim_rejected(self):
        native, host, readback = fixture()
        host["scheduler_tick_executed"] = True
        with self.assertRaisesRegex(ValueError, "pre-scheduler"):
            COMPARE.compare(native, host, readback)


if __name__ == "__main__":
    unittest.main()
