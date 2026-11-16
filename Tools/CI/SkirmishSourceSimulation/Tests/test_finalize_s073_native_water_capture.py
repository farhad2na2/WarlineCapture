import hashlib
import importlib.util
import json
import struct
from pathlib import Path
import tempfile
import unittest

MODULE = Path(__file__).with_name("finalize_s073_native_water_capture.py")
SOURCE_SCENE = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity"
SOURCE_SCENE_GUID = "c00140f2e94a04c3084c8dcb0c18cbd0"
spec = importlib.util.spec_from_file_location("s073_native_water_capture", MODULE)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class S073NativeWaterCaptureTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve()
        self.directory = self.root / "capture"
        self.directory.mkdir()
        self.mask = bytes(2048 * 1024)
        self.mask_hash = hashlib.sha256(self.mask).hexdigest()
        self.source_scene = self.root / SOURCE_SCENE
        assembly = self.root / "Library/ScriptAssemblies/Game.Rendering.dll"
        assembly.parent.mkdir(parents=True, exist_ok=True)
        assembly.write_bytes(b"fixture assembly")
        self.pins = {}
        for relative in module.PIN_PATHS:
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            if relative == SOURCE_SCENE:
                payload = b"same-session loaded Campaign source scene"
            elif relative == SOURCE_SCENE + ".meta":
                payload = ("fileFormatVersion: 2\nguid: " + SOURCE_SCENE_GUID + "\n").encode()
            else:
                payload = ("source:" + relative).encode()
            path.write_bytes(payload)
            self.pins[relative] = hashlib.sha256(payload).hexdigest()
        self.data = {
            "kind": "S073NativeWaterMaskObservation",
            "schema_version": 1,
            "scope": module.SCOPE,
            "authority": module.AUTHORITY,
            "unity_version": "6000.5.8f1",
            "world_name": "S073 world",
            "world_sequence": "17",
            "session_id": "0123456789abcdef0123456789abcdef",
            "scenario_index": 3,
            "seed": 104729,
            "phase": "Playing",
            "operation_map_id": module.MAP_ID,
            "source_identity_hash": module.IDENTITY,
            "source_content_hash": module.CONTENT,
            "physical_source_validated": True,
            "provider_type": module.PROVIDER + ", Game.Runtime, Version=0.0.0.0",
            "provider_assembly_path": str(assembly),
            "provider_assembly_sha256": hashlib.sha256(assembly.read_bytes()).hexdigest(),
            "water_footprint_count": 0,
            "grid": {"width": 2048, "height": 1024, "cell_size": 1.0, "origin": [0.0, 0.0, 0.0]},
            "mask_path": str(self.directory / "snapshot.water.bin"),
            "mask_encoding": module.ENCODING,
            "mask_cells": len(self.mask),
            "water_cells": 0,
            "mask_sha256": self.mask_hash,
            "entity_order_version_before": 42,
            "entity_order_version_after": 42,
            "provider_reference_unchanged": True,
            "source_pins_sha256": self.pins,
            "loaded_campaign_source_fit_observation": {
                "kind": "S073SameSessionLoadedCampaignSourceFitObservation",
                "scope": "SamePlayingWorld_LoadedCampaignSceneOwnerAndSpawnInputs_DiagnosticOnly",
                "world_name": "S073 world", "world_sequence": "17",
                "session_id": "0123456789abcdef0123456789abcdef", "phase": "Playing",
                "logical_map_id": module.MAP_ID, "campaign_source_map_id": "opmap.skirmish.desert_base_01",
                "source_identity_hash": module.IDENTITY, "source_content_hash": module.CONTENT,
                "source_scene_guid": SOURCE_SCENE_GUID,
                "source_scene_path": SOURCE_SCENE,
                "source_scene_sha256": hashlib.sha256(self.source_scene.read_bytes()).hexdigest(),
                "source_scene_meta_sha256": hashlib.sha256(self.source_scene.with_name(self.source_scene.name + ".meta").read_bytes()).hexdigest(),
                "s073_config_sha256": self.pins["Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset"],
                "scene_loaded": True, "grid_owned_by_campaign_scene": True,
                "blocker_owner_source_sha256": self.pins["Assets/Game/Scripts/Systems/StaticGridBlockerUpdateSystem.cs"],
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
        self.write_capture()

    def tearDown(self):
        self.temp.cleanup()

    def write_capture(self):
        output = self.directory / "snapshot.json"
        output.write_text(json.dumps(self.data), encoding="utf-8")
        (self.directory / "snapshot.water.bin").write_bytes(self.mask)
        (self.directory / "editor.log").write_text(
            f"[S073LoadedCampaignSourceFit] result=Captured session={self.data['session_id']} sceneGuid=c00140f2e94a04c3084c8dcb0c18cbd0 spawnPlanAvailable=0\n"
            f"[S073NativeWaterMask] result=Recorded cells=2097152 waterCells={self.data['water_cells']} path={output}\n",
            encoding="utf-8")
        passive = self.data.get("playing_passive_input_observation")
        if passive is not None:
            with (self.directory / "editor.log").open("a", encoding="utf-8") as log:
                log.write(f"[S073NativeBlockerOccupancy] result=Recorded rawSha256={passive['raw_marker_sha256']} path={passive['raw_marker_path']}\n")

    def test_qualifies_capture_and_keeps_footprint_count_separate(self):
        receipt = module.validate(self.root, self.directory, 0)
        self.assertEqual(receipt["validation_status"], "Passed")
        self.assertEqual(receipt["water_footprint_count"], 0)
        self.assertEqual(receipt["water_cells"], 0)
        self.assertEqual(receipt["mask_sha256"], self.mask_hash)
        self.assertFalse(receipt["mission_ready"])
        self.assertFalse(receipt["player_readiness"])

    def test_loaded_campaign_scene_and_meta_are_source_pins(self):
        receipt = module.validate(self.root, self.directory, 0)
        source_fit = receipt["loaded_campaign_source_fit_observation"]
        self.assertEqual(self.pins[SOURCE_SCENE], source_fit["source_scene_sha256"])
        self.assertEqual(self.pins[SOURCE_SCENE + ".meta"], source_fit["source_scene_meta_sha256"])
        self.assertEqual(SOURCE_SCENE_GUID, source_fit["source_scene_guid"])

    def test_loaded_campaign_scene_meta_guid_change_fails_closed(self):
        meta = self.root / (SOURCE_SCENE + ".meta")
        meta.write_text("fileFormatVersion: 2\nguid: " + "0" * 32 + "\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "Loaded Campaign source scene or GUID meta changed"):
            module.validate(self.root, self.directory, 0)

    def test_persists_same_session_passive_blocker_raw_capture(self):
        cells = 2048 * 1024
        raw = (b"S073BOC1" + struct.pack("<IIffffQQQI", 2048, 1024, 1.0, 0.0, 0.0, 0.0,
                                          7, 7, 11, cells) + bytes((cells + 7) // 8) +
               bytes(cells * 4) + bytes(cells) + bytes((cells + 7) // 8))
        raw_path = self.root / "native-raw.bin"
        raw_path.write_bytes(raw)
        sidecar = {
            "kind": "S073BlockerOccupancyRawSnapshot", "schema_version": 1, "status": "Captured",
            "producer": "native", "observation": "playing_passive_input_observation",
            "raw_path": str(raw_path), "raw_sha256": hashlib.sha256(raw).hexdigest(), "byte_length": len(raw),
            "width": 2048, "height": 1024, "cell_size": 1.0, "origin": [0.0, 0.0, 0.0],
            "storage_epoch": "7", "applied_storage_epoch": "7", "applied_revision": "11",
            "mission": "S073", "seed": 104729, "source_map_id": module.MAP_ID,
            "source_campaign_map_id": "opmap.skirmish.desert_base_01",
            "source_identity_hash": module.IDENTITY, "source_content_hash": module.CONTENT,
            "native_source_pins_sha256": self.pins,
            "native_session_id": self.data["session_id"], "source_host_state_transferred": False,
            "campaign_state_transferred": False, "readiness_claimed": False,
        }
        self.data["source_map_id"] = module.MAP_ID
        self.data["source_campaign_map_id"] = "opmap.skirmish.desert_base_01"
        self.data["playing_passive_input_observation"] = {
            "observation_name": "playing_passive_input_observation", "phase": "Playing",
            "session_id": self.data["session_id"], "input_injected": False,
            "gameplay_state_mutated": False, "raw_blocker_occupancy_snapshot": sidecar,
            "raw_marker_sha256": hashlib.sha256(raw).hexdigest(), "raw_marker_path": str(raw_path),
        }
        self.write_capture()
        receipt = module.validate(self.root, self.directory, 0)
        persistent = self.directory / "blocker-occupancy.bin"
        normalized = json.loads((self.directory / "snapshot.json").read_text())
        self.assertEqual(receipt["blocker_occupancy_raw_capture"]["raw"]["sha256"], hashlib.sha256(raw).hexdigest())
        self.assertEqual(persistent.read_bytes(), raw)
        self.assertEqual(normalized["playing_passive_input_observation"]["raw_blocker_occupancy_snapshot"]["raw_path"], str(persistent))

    def test_wrapper_and_marker_fail_closed(self):
        with self.assertRaisesRegex(ValueError, "wrapper"):
            module.validate(self.root, self.directory, 1)
        (self.directory / "editor.log").write_text("successful but unrelated\n")
        with self.assertRaisesRegex(ValueError, "marker"):
            module.validate(self.root, self.directory, 0)

    def test_same_session_map_provider_and_grid_are_required(self):
        for field, value in (("session_id", "bad"), ("operation_map_id", "wrong"),
                             ("source_content_hash", "0" * 64), ("provider_type", "other, asm")):
            original = self.data[field]
            self.data[field] = value
            self.write_capture()
            with self.subTest(field=field), self.assertRaises(ValueError):
                module.validate(self.root, self.directory, 0)
            self.data[field] = original
        self.data["grid"]["origin"] = [1, 0, 0]
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "grid"):
            module.validate(self.root, self.directory, 0)

    def test_mask_bytes_hash_and_counts_are_exact(self):
        changed = bytearray(self.mask)
        changed[19] = 2
        self.mask = bytes(changed)
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "bool01"):
            module.validate(self.root, self.directory, 0)
        self.mask = bytes(2048 * 1024)
        self.data["water_cells"] = 1
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "count or hash"):
            module.validate(self.root, self.directory, 0)

    def test_structural_change_and_readiness_claim_rejected(self):
        self.data["entity_order_version_after"] += 1
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "structural version"):
            module.validate(self.root, self.directory, 0)
        self.data["entity_order_version_after"] -= 1
        self.data["mission_ready"] = True
        self.write_capture()
        with self.assertRaisesRegex(ValueError, "non-readiness"):
            module.validate(self.root, self.directory, 0)

    def test_all_source_pins_must_match_current_tree(self):
        changed = next(relative for relative in module.PIN_PATHS
                       if relative not in (SOURCE_SCENE, SOURCE_SCENE + ".meta"))
        (self.root / changed).write_text("modified source")
        with self.assertRaisesRegex(ValueError, "Capture source changed"):
            module.validate(self.root, self.directory, 0)


if __name__ == "__main__":
    unittest.main()
