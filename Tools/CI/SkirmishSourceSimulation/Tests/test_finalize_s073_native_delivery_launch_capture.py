import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import uuid


MODULE = Path(__file__).with_name("finalize_s073_native_delivery_launch_capture.py")
spec = importlib.util.spec_from_file_location("s073_delivery_launch_receipt", MODULE)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
PREPARE_MODULE = Path(__file__).with_name("prepare_s073_native_delivery_launch_capture.py")
prepare_spec = importlib.util.spec_from_file_location("s073_delivery_launch_prep", PREPARE_MODULE)
prepare_module = importlib.util.module_from_spec(prepare_spec)
prepare_spec.loader.exec_module(prepare_module)


class NativeDeliveryLaunchCaptureFinalizerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.repo = Path(self.temp.name) / "repo"
        self.repo.mkdir()
        self.out = self.repo / "run"
        self.out.mkdir()
        self.session = "session-fixture-073"
        for index, relative in enumerate(sorted(module.REQUIRED_SOURCE_PINS)):
            path = self.repo / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            payload = f"fixture source {index}".encode()
            path.write_bytes(payload)
        nonce = uuid.uuid4().hex
        original_dir = Path("/private/tmp")
        self.window_path = original_dir / ("s073-fixture-" + nonce + "-result.json")
        self.delivery_path = original_dir / ("s073-fixture-" + nonce + "-delivery.jsonl")
        self.pre_run_receipt_path = self.out / "pre-run-receipt.json"
        prepare_module.create_receipt(self.repo, self.pre_run_receipt_path,
                                      self.window_path, self.delivery_path)
        self.bounds = {
            "kind": "S073NativeProductionStaticRenderBoundsObservation",
            "session_id": self.session,
            "frame": 1046,
            "unity_time": 16.25,
            "static_render_bounds_observed": True,
            "static_render_bounds_count": 2,
            "static_bounds_provider_type": "Game.Rendering.FixtureProvider",
            "static_bounds_provider_identity": 4711,
            "static_bounds_provider_reference_stable": True,
            "static_bounds_grid_entity_index": 22,
            "static_bounds_grid_entity_version": 3,
            "static_bounds_grid_width": 2048,
            "static_bounds_grid_height": 1024,
            "static_bounds_grid_cell_size": 1.0,
            "static_bounds_grid_origin": [0.0, 0.0, 0.0],
            "static_bounds_preferred_cell_x": 1001,
            "static_bounds_preferred_cell_y": 430,
            "static_bounds_area": [903.0, 332.0, 197.0, 197.0],
            "static_bounds_blocker_grid_size": 2097152,
            "static_bounds_blocker_storage_epoch": 7,
            "static_bounds_blocker_applied_storage_epoch": 7,
            "static_bounds_blocker_applied_revision": 19,
            "static_bounds_blocker_component_present": True,
            "static_bounds": [
                {"min": [10.0, 0.0, 20.0], "max": [12.0, 3.0, 22.0]},
                {"min": [30.0, -1.0, 40.0], "max": [31.0, 4.0, 42.0]},
            ],
        }
        self.launch = {
            "kind": "S073NativeProductionDropSearchObservation",
            "session_id": self.session,
            "frame": 1046,
            "unity_time": 16.25,
            "launch_frame": 1046,
            "launch_random_state_before": 305419896,
            "launch_random_state_after": 305419896,
            "launch_random_state_observed": False,
            "launch_random_state_local_value_observed": True,
            "launch_random_state_source_hooks_available": True,
            "launch_camera_present": True,
            "launch_camera_position": [834.0, 60.0, 556.0],
            "launch_camera_forward": [0.0, -0.848, 0.53],
            "launch_camera_right": [1.0, 0.0, 0.0],
            "drop_search_preferred_cell_x": 1001,
            "drop_search_preferred_cell_y": 430,
        }
        self.tick = {
            "kind": "S073NativeProductionDeliveryObservation",
            "session_id": self.session,
            "frame": 1046,
            "random_state_before": 305419896,
            "random_state_after": 305419896,
            "random_state_observed": True,
            "random_state_source_hooks_available": True,
            "random_state_getter_verification_passed": True,
            "random_state_getter_before_observed": True,
            "random_state_getter_before": 305419896,
            "random_state_getter_before_matches_tick_input": True,
            "random_state_getter_after_observed": True,
            "random_state_getter_after": 305419896,
            "random_state_getter_after_matches_tick_output": True,
        }
        self.second_launch = {
            **self.launch,
            "frame": 1786,
            "launch_frame": 1786,
            "unity_time": 56.25,
            "launch_random_state_before": 305419904,
            "launch_random_state_after": 305419904,
        }
        self.result = {
            "kind": "S073NativeProductionDeliveryWindow",
            "schema_version": 3,
            "result": "ObservationWindowElapsed",
            "result": "ObservationWindowElapsed",
            "mission": "S073",
            "scenario_index": 3,
            "seed": 104729,
            "session_id": self.session,
            "terminal_match_phase": "Playing",
            "scene_operation_map_at_playing": self._scene_map_snapshot(),
            "scene_operation_map_latest_same_world_session": self._scene_map_snapshot(),
            "production_delivery_observer": {
                "status": "NaturalProductionStateObserved",
                "path": str(self.delivery_path),
                "observer_error_count": 0,
                "rows": 4,
                "same_session_rows": 4,
                "static_bounds_observation_count": 1,
                "static_bounds_skipped_overflow_count": 0,
            },
            "observer_error_count": 0,
            "mission_ready": False,
            "source_fit_certified": False,
            "normal_input_playthrough": False,
            "player_readiness": False,
        }
        self._write_inputs([self.bounds, self.launch, self.tick, self.second_launch])

    def _scene_map_snapshot(self):
        return {
            "capture_status": "Observed",
            "world_name": "FixtureWorld",
            "session_id": self.session,
            "frame": 1000,
            "loaded_scene_sequence": [{
                "sequence_index": 0, "name": "Match", "path": "Assets/Game/Scenes/Match.unity",
                "handle": 1, "is_valid": True, "is_loaded": True, "is_active": True,
            }],
            "operation_map_root_query_cardinality": 1,
            "operation_map_root_entity_ids": [{"entity_index": 8, "entity_version": 2}],
            "active_operation_map_query_cardinality": 1,
            "active_operation_map_rows": [{
                "entity_index": 8, "entity_version": 2, "operation_map_id": "fixture.map",
                "scenario_id": "fixture.scenario", "mission_id": "fixture.mission", "generation": 3,
            }],
            "operation_map_metadata_query_cardinality": 1,
            "operation_map_metadata_rows": [{
                "entity_index": 8, "entity_version": 2, "metadata_hash": "fixture-hash",
                "generation": 3, "physical_source_validated": False,
            }],
            "unique_consistent_operation_map_identity_available": True,
            "unique_operation_map_id": "fixture.map",
            "unique_scenario_id": "fixture.scenario",
            "unique_mission_id": "fixture.mission",
            "unique_operation_map_generation": 3,
            "unique_metadata_generation": 3,
            "unique_metadata_hash": "fixture-hash",
            "unique_physical_source_validated": False,
            "unique_identity_matches_metadata": True,
        }

    def tearDown(self):
        self.window_path.unlink(missing_ok=True)
        self.delivery_path.unlink(missing_ok=True)
        self.temp.cleanup()

    def _write_inputs(self, rows):
        self.result["production_delivery_observer"]["rows"] = len(rows)
        self.result["production_delivery_observer"]["same_session_rows"] = len(rows)
        self.result["production_delivery_observer"]["static_bounds_observation_count"] = sum(
            row.get("kind") == "S073NativeProductionStaticRenderBoundsObservation" for row in rows)
        result_bytes = ("\ufeff" + json.dumps(self.result)).encode("utf-8")
        delivery_bytes = ("\ufeff" + "\n".join(json.dumps(row) for row in rows) + "\n").encode("utf-8")
        self.window_path.write_bytes(result_bytes)
        self.delivery_path.write_bytes(delivery_bytes)
        (self.out / "fixture-result.json").write_bytes(result_bytes)
        (self.out / "fixture-delivery.jsonl").write_bytes(delivery_bytes)
        started = ("[S073NativeProductionWindow] started windowOutput=" + str(self.window_path) +
                   " deliveryOutput=" + str(self.delivery_path))
        (self.out / "editor.log").write_text(
            "COMMAND LINE ARGUMENTS:\n" + started + "\n" + module.REQUIRED_MARKER + "\n" +
            module.REQUIRED_SHUTDOWN_MARKER + "\n",
            encoding="utf-8-sig")

    def _qualify(self, wrapper_exit=0):
        return module.qualify(self.repo, self.out, wrapper_exit, self.pre_run_receipt_path)

    def test_qualifies_exact_first_launch_and_preserves_unpaired_second_as_unknown(self):
        receipt = self._qualify()
        self.assertEqual(receipt["validation_status"], "Passed")
        self.assertTrue(receipt["gates"]["same_session_frame_bounds_launch"])
        self.assertTrue(receipt["gates"]["local_and_tick_rng_flags"])
        self.assertTrue(receipt["gates"]["same_session_scene_operation_map"])
        rng_verification = receipt["capture"]["same_frame_source_tick_rng_verification"]
        self.assertTrue(rng_verification["verification_passed"])
        self.assertEqual(rng_verification["getter_before"], 305419896)
        self.assertEqual(receipt["session"]["scene_operation_map_at_playing"]["unique_metadata_hash"],
                         "fixture-hash")
        self.assertFalse(receipt["capture"]["launch_random_state_observed_field"])
        self.assertTrue(receipt["capture"]["launch_random_state_local_value_observed"])
        self.assertTrue(receipt["capture"]["same_frame_source_tick_random_state_observed"])
        self.assertEqual(receipt["session"]["launch_frame"], 1046)
        self.assertEqual(receipt["capture"]["bounds_order"], "collector_return_order_preserved")
        self.assertEqual(receipt["capture"]["bounds_count"], 2)
        self.assertEqual(len(receipt["capture"]["additional_launch_rng_observations"]), 1)
        self.assertIsNone(receipt["capture"]["additional_launch_rng_observations"][0]["same_frame_tick_pair"])
        self.assertIsNone(receipt["capture"]["additional_launch_rng_observations"][0]["bounds_same_frame"])
        self.assertEqual(receipt["source_pins"]["status"], "Passed")
        self.assertTrue(set(prepare_module.REQUIRED_SOURCE_PINS).issubset(
            receipt["source_pins"]["verified_paths"]))
        self.assertFalse(receipt["mission_ready"])
        self.assertFalse(receipt["player_readiness"])
        self.assertEqual(receipt["adjacent_observations"]["predicate_capture"],
                         "Unknown_NotCapturedByThisLaunchInputFinalizer")
        self.assertEqual(receipt["adjacent_observations"]["mask_capture"],
                         "Unknown_NotCapturedByThisLaunchInputFinalizer")

    def test_multiple_distinct_launch_pairs_are_allowed_but_ambiguous_pair_fails(self):
        second_bounds = {**self.bounds, "frame": 1786, "unity_time": 56.25,
                         "static_bounds_preferred_cell_x": 1002}
        second_launch = {**self.second_launch, "drop_search_preferred_cell_x": 1002,
                         "drop_search_preferred_cell_y": 430}
        self._write_inputs([self.bounds, self.launch, self.tick, second_bounds, second_launch])
        receipt = self._qualify()
        self.assertEqual(receipt["validation_status"], "Passed")
        self.assertEqual(receipt["capture"]["bounds_count"], 2)
        self.assertEqual(receipt["session"]["launch_frame"], 1046)

        duplicate_bounds = {**self.bounds}
        self._write_inputs([self.bounds, duplicate_bounds, self.launch, self.tick])
        ambiguous = self._qualify()
        self.assertEqual(ambiguous["validation_status"], "Failed")
        self.assertFalse(ambiguous["gates"]["same_session_frame_bounds_launch"])

    def test_missing_pins_and_tick_pair_remain_incomplete_not_passed(self):
        self.pre_run_receipt_path.unlink()
        self._write_inputs([self.bounds, self.launch])
        receipt = self._qualify()
        self.assertEqual(receipt["validation_status"], "Incomplete")
        self.assertIsNone(receipt["gates"]["source_pin_hashes"])
        self.assertIsNone(receipt["gates"]["local_and_tick_rng_flags"])
        self.assertEqual(receipt["capture"]["provider_type"], "Game.Rendering.FixtureProvider")

    def test_missing_launch_facts_are_unknown_and_bounds_hash_preserves_order(self):
        self._write_inputs([self.tick])
        missing = self._qualify()
        self.assertEqual(missing["validation_status"], "Incomplete")
        self.assertIsNone(missing["gates"]["same_session_frame_bounds_launch"])
        self.assertIsNone(missing["gates"]["finite_camera_grid_and_epochs"])

        self._write_inputs([self.bounds, self.launch, self.tick])
        ordered_hash = self._qualify()["capture"]["ordered_bounds_sha256"]
        self.bounds["static_bounds"].reverse()
        self._write_inputs([self.bounds, self.launch, self.tick])
        reversed_hash = self._qualify()["capture"]["ordered_bounds_sha256"]
        self.assertNotEqual(ordered_hash, reversed_hash)

    def test_same_frame_rng_mismatch_and_missing_grid_epoch_are_rejected(self):
        bad_tick = {**self.tick, "random_state_after": 305419904}
        self._write_inputs([self.bounds, self.launch, bad_tick])
        receipt = self._qualify()
        self.assertFalse(receipt["gates"]["local_and_tick_rng_flags"])

        self.bounds["static_bounds_blocker_applied_revision"] = None
        self._write_inputs([self.bounds, self.launch, self.tick])
        receipt = self._qualify()
        self.assertFalse(receipt["gates"]["finite_camera_grid_and_epochs"])

    def test_rng_getter_mismatch_fails_and_missing_getter_fact_stays_incomplete(self):
        mismatched = {**self.tick, "random_state_getter_after": 305419904,
                      "random_state_getter_after_matches_tick_output": False,
                      "random_state_getter_verification_passed": False,
                      "random_state_observed": False}
        self._write_inputs([self.bounds, self.launch, mismatched])
        failed = self._qualify()
        self.assertFalse(failed["gates"]["local_and_tick_rng_flags"])
        self.assertEqual(failed["validation_status"], "Failed")

        missing = dict(self.tick)
        del missing["random_state_getter_after"]
        self._write_inputs([self.bounds, self.launch, missing])
        incomplete = self._qualify()
        self.assertIsNone(incomplete["gates"]["local_and_tick_rng_flags"])
        self.assertEqual(incomplete["validation_status"], "Incomplete")

    def test_scene_map_baseline_latest_tamper_fails_and_missing_snapshot_is_incomplete(self):
        self.result["scene_operation_map_latest_same_world_session"]["unique_metadata_hash"] = "tampered"
        self._write_inputs([self.bounds, self.launch, self.tick])
        tampered = self._qualify()
        self.assertFalse(tampered["gates"]["same_session_scene_operation_map"])
        self.assertEqual(tampered["validation_status"], "Failed")

        self.result["scene_operation_map_latest_same_world_session"] = None
        self._write_inputs([self.bounds, self.launch, self.tick])
        missing = self._qualify()
        self.assertIsNone(missing["gates"]["same_session_scene_operation_map"])
        self.assertEqual(missing["validation_status"], "Incomplete")

    def test_scene_without_loaded_match_and_cross_session_map_are_rejected(self):
        latest = self.result["scene_operation_map_latest_same_world_session"]
        latest["loaded_scene_sequence"][0]["is_loaded"] = False
        self._write_inputs([self.bounds, self.launch, self.tick])
        no_match_scene = self._qualify()
        self.assertFalse(no_match_scene["gates"]["same_session_scene_operation_map"])

        latest["loaded_scene_sequence"][0]["is_loaded"] = True
        latest["session_id"] = "other-session"
        self._write_inputs([self.bounds, self.launch, self.tick])
        cross_session = self._qualify()
        self.assertFalse(cross_session["gates"]["same_session_scene_operation_map"])

    def test_wrapper_marker_provider_and_source_pin_failures_do_not_pass(self):
        failed = self._qualify(wrapper_exit=1)
        self.assertNotEqual(failed["validation_status"], "Passed")
        self.assertIn("Checked Unity wrapper did not exit successfully", failed["unknown_or_failed_facts"])
        (self.out / "editor.log").write_text("marker missing\n", encoding="utf-8-sig")
        self.assertFalse(self._qualify()["gates"]["completion_marker"])

        (self.out / "editor.log").write_text(module.REQUIRED_MARKER + "\n", encoding="utf-8-sig")
        self.bounds["static_bounds_provider_reference_stable"] = False
        self._write_inputs([self.bounds, self.launch, self.tick, self.second_launch])
        receipt = self._qualify()
        self.assertFalse(receipt["gates"]["ordered_bounds_and_provider_stability"])
        self.assertNotEqual(receipt["validation_status"], "Passed")

        pins = json.loads(self.pre_run_receipt_path.read_text(encoding="utf-8-sig"))
        first = next(iter(pins["source_pins_sha256"]))
        pins["source_pins_sha256"][first] = "0" * 64
        self.pre_run_receipt_path.write_text(json.dumps(pins), encoding="utf-8-sig")
        self.assertFalse(self._qualify()["gates"]["source_pin_hashes"])

    def test_shutdown_marker_is_required_and_timeout_marker_is_rejected(self):
        log_path = self.out / "editor.log"
        successful_log = log_path.read_text(encoding="utf-8-sig")
        log_path.write_text(successful_log.replace(module.REQUIRED_SHUTDOWN_MARKER + "\n", ""),
                            encoding="utf-8-sig")
        missing = self._qualify()
        self.assertFalse(missing["gates"]["edit_mode_shutdown"])
        self.assertNotEqual(missing["validation_status"], "Passed")

        log_path.write_text(successful_log + module.TIMEOUT_SHUTDOWN_MARKER + " edit_mode_confirmed=false\n",
                            encoding="utf-8-sig")
        timed_out = self._qualify()
        self.assertFalse(timed_out["gates"]["edit_mode_shutdown"])
        self.assertGreater(timed_out["wrapper"]["timeout_shutdown_marker_count"], 0)
        self.assertNotEqual(timed_out["validation_status"], "Passed")

    def test_pre_run_path_and_artifact_byte_mismatches_are_rejected(self):
        receipt = json.loads(self.pre_run_receipt_path.read_text(encoding="utf-8-sig"))
        receipt["window_output_path"] = str(self.window_path.parent / "other-window.json")
        self.pre_run_receipt_path.write_text(json.dumps(receipt), encoding="utf-8-sig")
        qualified = self._qualify()
        self.assertFalse(qualified["gates"]["pre_run_output_path_binding"])
        self.assertNotEqual(qualified["validation_status"], "Passed")

        receipt["window_output_path"] = str(self.window_path)
        self.pre_run_receipt_path.write_text(json.dumps(receipt), encoding="utf-8-sig")
        (self.out / "fixture-result.json").write_bytes(b"tampered durable result")
        qualified = self._qualify()
        self.assertFalse(qualified["gates"]["output_artifact_byte_identity"])
        self.assertNotEqual(qualified["validation_status"], "Passed")

    def test_pre_run_receipt_generator_requires_unique_fresh_outputs_and_checked_wrapper(self):
        with self.assertRaises(ValueError):
            prepare_module.create_receipt(self.repo, self.out / "same-path.json",
                                          self.window_path, self.window_path)
        with self.assertRaises(FileExistsError):
            prepare_module.create_receipt(self.repo, self.out / "existing-output.json",
                                          self.window_path, self.delivery_path)
        with self.assertRaises(ValueError):
            prepare_module.create_receipt(self.repo, self.out / "wrong-wrapper.json",
                                          self.window_path.parent / "fresh.json",
                                          self.delivery_path.parent / "fresh.json",
                                          "Tools/CI/other-wrapper.sh")

    def test_nonfinite_camera_bounds_and_readiness_claim_are_rejected(self):
        self.launch["launch_camera_position"] = [float("nan"), 60.0, 556.0]
        self.bounds["static_bounds"][0]["min"] = [13.0, 0.0, 20.0]
        self.result["mission_ready"] = True
        self._write_inputs([self.bounds, self.launch, self.tick])
        receipt = self._qualify()
        self.assertFalse(receipt["gates"]["finite_camera_grid_and_epochs"])
        self.assertFalse(receipt["gates"]["ordered_bounds_and_provider_stability"])
        self.assertFalse(receipt["gates"]["no_readiness_claim"])
        self.assertNotEqual(receipt["validation_status"], "Passed")


if __name__ == "__main__":
    unittest.main()
