import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

MODULE = Path(__file__).with_name("finalize_s073_joint_native_water_capture.py")
spec = importlib.util.spec_from_file_location("s073_joint_native_water_capture", MODULE)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class S073JointNativeWaterCaptureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve()
        self.startup_dir = self.root / "startup"
        self.playing_dir = self.root / "playing"
        self.startup_dir.mkdir()
        self.playing_dir.mkdir()
        self.startup_mask = bytes(module.CELL_COUNT)
        self.playing_mask = self.startup_mask
        assembly = self.root / "Library/ScriptAssemblies/Game.Rendering.dll"
        assembly.parent.mkdir(parents=True)
        assembly.write_bytes(b"same captured provider assembly")
        self.pins = {}
        for relative in module.STARTUP_PINS | module.PLAYING_PINS:
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            if relative == module.CAMPAIGN_SCENE_PATH:
                payload = b"same-session loaded Campaign source scene"
            elif relative == module.CAMPAIGN_SCENE_PATH + ".meta":
                payload = ("fileFormatVersion: 2\nguid: " + module.CAMPAIGN_SCENE_GUID + "\n").encode()
            else:
                payload = ("source:" + relative).encode()
            path.write_bytes(payload)
            self.pins[relative] = hashlib.sha256(payload).hexdigest()
        self.startup_pins = {path: self.pins[path] for path in module.STARTUP_PINS}
        self.playing_pins = {path: self.pins[path] for path in module.PLAYING_PINS}
        provider_hash = hashlib.sha256(assembly.read_bytes()).hexdigest()
        provider_path = str(assembly)
        self.startup_observation = self.startup_dir / "startup.json"
        self.startup_mask_path = self.startup_dir / "startup.water.bin"
        self.playing_observation = self.playing_dir / "snapshot.json"
        self.playing_mask_path = self.playing_dir / "snapshot.water.bin"
        self.log_path = self.playing_dir / "editor.log"
        self.startup = {
            "kind": module.STARTUP_KIND,
            "schema_version": 1,
            "scope": module.STARTUP_SCOPE,
            "authority": module.STARTUP_AUTHORITY,
            "unity_version": "6000.5.8f1",
            "world_name": "S073 Native World",
            "world_sequence": "23",
            "session_id": "0123456789abcdef0123456789abcdef",
            "scenario_index": 3,
            "seed": 104729,
            "phase": "Preparing",
            "operation_map_id": module.MAP_ID,
            "source_identity_hash": module.IDENTITY,
            "source_content_hash": module.CONTENT,
            "physical_source_validated": True,
            "request_id": None,
            "request_binding_available": False,
            "request_binding_reason": "OriginalPlacementFacts does not expose BuildingRuntimeSpawnRequest identity",
            "frame": 118,
            "placement_origin": [950, 438],
            "placement_footprint": [28, 15],
            "placement_effective_rect": [950, 438, 28, 15],
            "cache_owner_runtime_identity": 9021,
            "provider_type": module.PROVIDER + ", Game.Runtime, Version=0.0.0.0",
            "provider_assembly_path": provider_path,
            "provider_assembly_sha256": provider_hash,
            "grid": {"width": 2048, "height": 1024, "cell_size": 1.0, "origin": [0.0, 0.0, 0.0]},
            "mask_path": str(self.startup_mask_path),
            "mask_encoding": module.ENCODING,
            "mask_cells": module.CELL_COUNT,
            "water_cells": 0,
            "mask_sha256": hashlib.sha256(self.startup_mask).hexdigest(),
            "entity_order_version_before": 88,
            "entity_order_version_after": 88,
            "source_pins_sha256": self.startup_pins,
            "campaign_gameplay_state_copied": False,
            "mission_ready": False,
            "source_fit_certified": False,
            "normal_input_playthrough": False,
            "player_readiness": False,
        }
        self.playing = {
            "kind": module.PLAYING_KIND,
            "schema_version": 1,
            "scope": module.PLAYING_SCOPE,
            "authority": module.PLAYING_AUTHORITY,
            "unity_version": "6000.5.8f1",
            "world_name": self.startup["world_name"],
            "world_sequence": self.startup["world_sequence"],
            "session_id": self.startup["session_id"],
            "scenario_index": 3,
            "seed": 104729,
            "phase": "Playing",
            "operation_map_id": module.MAP_ID,
            "source_identity_hash": module.IDENTITY,
            "source_content_hash": module.CONTENT,
            "physical_source_validated": True,
            "provider_type": self.startup["provider_type"],
            "provider_assembly_path": provider_path,
            "provider_assembly_sha256": provider_hash,
            "water_footprint_count": 0,
            "grid": self.startup["grid"],
            "mask_path": str(self.playing_mask_path),
            "mask_encoding": module.ENCODING,
            "mask_cells": module.CELL_COUNT,
            "water_cells": 0,
            "mask_sha256": hashlib.sha256(self.playing_mask).hexdigest(),
            "entity_order_version_before": 91,
            "entity_order_version_after": 91,
            "provider_reference_unchanged": True,
            "source_pins_sha256": self.playing_pins,
            "loaded_campaign_source_fit_observation": {
                "kind": "S073SameSessionLoadedCampaignSourceFitObservation",
                "scope": "SamePlayingWorld_LoadedCampaignSceneOwnerAndSpawnInputs_DiagnosticOnly",
                "world_name": self.startup["world_name"], "world_sequence": self.startup["world_sequence"],
                "session_id": self.startup["session_id"], "phase": "Playing",
                "logical_map_id": module.MAP_ID, "campaign_source_map_id": "opmap.skirmish.desert_base_01",
                "source_identity_hash": module.IDENTITY, "source_content_hash": module.CONTENT,
                "source_scene_guid": module.CAMPAIGN_SCENE_GUID,
                "source_scene_path": module.CAMPAIGN_SCENE_PATH,
                "source_scene_sha256": self.playing_pins[module.CAMPAIGN_SCENE_PATH],
                "source_scene_meta_sha256": self.playing_pins[module.CAMPAIGN_SCENE_PATH + ".meta"],
                "s073_config_sha256": self.playing_pins["Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"],
                "scene_loaded": True, "grid_owned_by_campaign_scene": True,
                "blocker_owner_source_sha256": self.playing_pins["Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs"],
                "blocker_contributor_query_count": 0, "blocker_contributors_owned_by_campaign_scene": 0,
                "blocker_contributors": [], "building_owner_query_count": 0,
                "building_entities_owned_by_campaign_scene": 0, "building_entities": [],
                "original_initial_unit_spawn_cells": [], "original_initial_building_spawn_inputs": [],
                "original_initial_spawn_plan_available": False,
                "capture_completeness": {"loaded_campaign_scene": True, "active_grid_scene_ownership": True,
                    "blocker_owner_inputs": False, "building_owner_inputs": False, "original_initial_spawn_plan": False},
                "source_fit_certified": False, "physical_source_admission_changed": False,
                "scheduler_admitted": False, "mission_ready": False, "player_readiness": False,
            },
            "campaign_gameplay_state_copied": False,
            "mission_ready": False,
            "source_fit_certified": False,
            "normal_input_playthrough": False,
            "player_readiness": False,
        }
        self.write_inputs()

    def tearDown(self):
        self.temp.cleanup()

    def write_inputs(self):
        self.startup["mask_path"] = str(self.startup_mask_path)
        self.startup["mask_cells"] = len(self.startup_mask)
        self.startup["water_cells"] = sum(self.startup_mask)
        self.startup["mask_sha256"] = hashlib.sha256(self.startup_mask).hexdigest()
        self.playing["mask_path"] = str(self.playing_mask_path)
        self.playing["mask_cells"] = len(self.playing_mask)
        self.playing["water_cells"] = sum(self.playing_mask)
        self.playing["mask_sha256"] = hashlib.sha256(self.playing_mask).hexdigest()
        self.startup_observation.write_text(json.dumps(self.startup), encoding="utf-8")
        self.playing_observation.write_text(json.dumps(self.playing), encoding="utf-8")
        self.startup_mask_path.write_bytes(self.startup_mask)
        self.playing_mask_path.write_bytes(self.playing_mask)
        self.log_path.write_text(
            f"[S073NativeStartupWaterMask] result=Recorded frame={self.startup['frame']} cells={module.CELL_COUNT} "
            f"waterCells={self.startup['water_cells']} path={self.startup_observation}\n"
            f"[S073LoadedCampaignSourceFit] result=Captured session={self.playing['session_id']} "
            f"sceneGuid={module.CAMPAIGN_SCENE_GUID} spawnPlanAvailable=0\n"
            f"[S073NativeWaterMask] result=Recorded cells={module.CELL_COUNT} "
            f"waterCells={self.playing['water_cells']} path={self.playing_observation}\n",
            encoding="utf-8",
        )
        self.write_playing_receipt()

    def write_playing_receipt(self):
        receipt = {
            "kind": "S073NativeWaterMaskCaptureReceipt",
            "schema_version": 1,
            "validation_status": "Passed",
            "wrapper_exit_code": 0,
            "required_marker_present": True,
            "observation_sha256": hashlib.sha256(self.playing_observation.read_bytes()).hexdigest(),
            "mask_sha256": hashlib.sha256(self.playing_mask_path.read_bytes()).hexdigest(),
            "editor_log_sha256": hashlib.sha256(self.log_path.read_bytes()).hexdigest(),
            "loaded_campaign_source_fit_observation": self.playing["loaded_campaign_source_fit_observation"],
        }
        (self.playing_dir / "water-receipt.json").write_text(json.dumps(receipt), encoding="utf-8")

    def validate(self, wrapper_exit=0):
        return module.validate(self.root, self.startup_observation, self.startup_mask_path,
                               self.playing_dir, wrapper_exit)

    def test_same_session_identical_masks_record_match_without_readiness_claims(self):
        result = self.validate()
        self.assertEqual(result["evidence_validation"], "Passed")
        self.assertEqual(result["parity_status"], "Match")
        self.assertTrue(result["masks_match"])
        self.assertEqual(result["differing_cells"], 0)
        self.assertEqual(result["startup_capture"]["mask_path"], str(self.startup_mask_path))
        self.assertIsNone(result["startup_capture"]["request_id"])
        self.assertFalse(result["startup_capture"]["request_binding_available"])
        self.assertEqual(result["playing_capture"]["mask_path"], str(self.playing_mask_path))
        for field in ("source_fit_certified", "mission_ready", "normal_input_playthrough", "player_readiness"):
            self.assertFalse(result[field])

    def test_mask_difference_is_recorded_as_mismatch_not_capture_failure(self):
        changed = bytearray(self.playing_mask)
        changed[513] = 1
        self.playing_mask = bytes(changed)
        self.write_inputs()
        result = self.validate()
        self.assertEqual(result["evidence_validation"], "Passed")
        self.assertEqual(result["parity_status"], "MismatchRecorded")
        self.assertFalse(result["masks_match"])
        self.assertEqual(result["differing_cells"], 1)
        self.assertEqual(result["playing_capture"]["water_cells"], 1)

    def test_different_session_or_world_is_rejected(self):
        for field, value in (("session_id", "ffffffffffffffffffffffffffffffff"),
                             ("world_sequence", "24"), ("world_name", "Other World")):
            self.playing[field] = value
            self.playing["loaded_campaign_source_fit_observation"][field] = value
            self.write_inputs()
            with self.subTest(field=field), self.assertRaisesRegex(ValueError, "same actual world/session"):
                self.validate()
            self.playing[field] = self.startup[field]
            self.playing["loaded_campaign_source_fit_observation"][field] = self.startup[field]

    def test_wrapper_markers_paths_and_shared_source_pins_fail_closed(self):
        with self.assertRaisesRegex(ValueError, "wrapper"):
            self.validate(1)
        self.log_path.write_text("successful but missing capture markers\n", encoding="utf-8")
        self.write_playing_receipt()
        with self.assertRaisesRegex(ValueError, "Playing water marker"):
            self.validate()
        relative = next(iter(module.COMMON_PINS))
        self.playing_pins[relative] = "0" * 64
        self.playing["source_pins_sha256"] = self.playing_pins
        self.write_inputs()
        with self.assertRaisesRegex(ValueError, "source pin changed"):
            self.validate()

    def test_mask_path_and_receipt_hash_are_bound_to_original_files(self):
        self.write_inputs()
        self.startup["mask_path"] = str(self.playing_mask_path)
        self.startup_observation.write_text(json.dumps(self.startup), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "original capture mask paths"):
            self.validate()
        self.write_inputs()
        receipt_path = self.playing_dir / "water-receipt.json"
        receipt = json.loads(receipt_path.read_text())
        receipt["mask_sha256"] = "0" * 64
        receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Playing capture receipt"):
            self.validate()

    def test_missing_preparing_callback_is_diagnostic_not_a_false_parity_pass(self):
        reason = "NoPreparingPhaseOriginalPlacementCallbackBeforePlaying"
        self.startup = {
            "kind": module.STARTUP_DIAGNOSTIC_KIND,
            "schema_version": 1,
            "status": "NotObserved",
            "diagnostic_reason": reason,
            "scope": "OptInStartupWaterObserverDiagnostic_NotCaptureOrReadiness",
            "unity_version": "6000.5.8f1",
            "world_name": self.playing["world_name"],
            "world_sequence": self.playing["world_sequence"],
            "session_id": self.playing["session_id"],
            "scenario_index": 3,
            "seed": 104729,
            "phase": "Playing",
            "operation_map_id": module.MAP_ID,
            "source_identity_hash": module.IDENTITY,
            "source_content_hash": module.CONTENT,
            "physical_source_validated": True,
            "source_pins_sha256": self.startup_pins,
            "campaign_gameplay_state_copied": False,
            "source_fit_certified": False,
            "mission_ready": False,
            "normal_input_playthrough": False,
            "player_readiness": False,
        }
        self.startup_observation.write_text(json.dumps(self.startup), encoding="utf-8")
        self.log_path.write_text(
            f"[S073NativeStartupWaterMask] result=NotObserved reason={reason} path={self.startup_observation}\n"
            f"[S073LoadedCampaignSourceFit] result=Captured session={self.playing['session_id']} "
            f"sceneGuid={module.CAMPAIGN_SCENE_GUID} spawnPlanAvailable=0\n"
            f"[S073NativeWaterMask] result=Recorded cells={module.CELL_COUNT} "
            f"waterCells={self.playing['water_cells']} path={self.playing_observation}\n",
            encoding="utf-8",
        )
        self.write_playing_receipt()
        result = module.validate(self.root, self.startup_observation, None, self.playing_dir, 0)
        self.assertEqual(result["evidence_validation"], "Passed")
        self.assertEqual(result["parity_status"], "NotComparable_StartupCaptureNotObserved")
        self.assertEqual(result["diagnostic_reason"], reason)
        self.assertIsNone(result["startup_capture"]["mask_path"])
        self.assertIsNone(result["startup_capture"]["request_id"])
        self.assertFalse(result["mission_ready"])


if __name__ == "__main__":
    unittest.main()
