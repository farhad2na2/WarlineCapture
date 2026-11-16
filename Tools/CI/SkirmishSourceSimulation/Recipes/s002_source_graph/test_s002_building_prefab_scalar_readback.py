from __future__ import annotations

import copy
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent
HELPER_SPEC = importlib.util.spec_from_file_location("s002_building_prefab_scalar_checks",
                                                     HERE / "s002_building_prefab_scalar_readback.py")
checks = importlib.util.module_from_spec(HELPER_SPEC)
HELPER_SPEC.loader.exec_module(checks)
RUNNER_SPEC = importlib.util.spec_from_file_location("s002_building_prefab_scalar_runner",
                                                     HERE / "run_s002_building_prefab_scalar_readback.py")
runner = importlib.util.module_from_spec(RUNNER_SPEC)
RUNNER_SPEC.loader.exec_module(runner)


class S002BuildingPrefabScalarReadbackTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.receipt, cls.projection, cls.inputs = checks.load_t1(ROOT)
        cls.candidates = checks.build_candidates(ROOT, cls.projection, cls.inputs)
        cls.manifest, _ = runner.prepare(ROOT)

    def observation(self):
        rows = []
        for candidate in self.candidates:
            source = candidate["input"]
            expected = candidate["expected"]
            rows.append({
                "key": candidate["key"], "status": "Passed", "asset_path": source["source_prefab_path"],
                "actual_prefab_main_root_identity": {
                    "path": source["source_prefab_path"], "guid": candidate["provider"]["guid"],
                    "local_id": "observed-root-identity", "identity_resolved": True,
                },
                "actual_prefab_main_root_local_id_is_observed_not_inferred": "observed-root-identity",
                "t1_provider_local_id": candidate["provider"].get("file_id"),
                "t1_provider_local_id_semantics": candidate["provider"].get("local_id_validation"),
                "resolved_original_source_component_identity": {
                    "path": source["source_owner_prefab_path"],
                    "guid": candidate["source_component_guid"],
                    "local_id": str(source["prefab_component_file_id"]), "identity_resolved": True,
                },
                "effective_config_identity": {
                    "path": source["source_config_path"], "guid": candidate["config_guid"],
                    "local_id": "11400000", "identity_resolved": True,
                },
                "original_metadata_owner":
                    "Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper.TryGetBuildingDefinitionMetadata",
                "original_metadata_owner_scalar_values": {field: expected[field] for field in checks.SCALAR_FIELDS},
                "imported_asset_dirty_before": False, "imported_asset_dirty_after_owner": False,
                "prefab_contents_unloaded_without_save": True,
            })
        return {
            "kind": "S002BuildingPrefabScalarMetadataObservation", "status": "Passed",
            "scope": "ImportedPrefabContentsPreviewScenes_NoAuthoredOrMissionScene_NoWorld_NoBaker_NoPrefabSave_NoFit_NoReadiness",
            "t1_receipt_sha256": checks.T1_RECEIPT_SHA256,
            "t1_projection_sha256": checks.T1_PROJECTION_SHA256,
            "t1_inputs_sha256": checks.T1_INPUTS_SHA256,
            "temporary_prefab_contents_preview_scene_used": True,
            "authored_or_mission_scene_loaded": False, "world_created": False, "baker_invoked": False,
            "prefab_asset_saved": False, "source_fit_verified": False, "mission_ready": False,
            "source_pins_before": self.manifest["source_pins"],
            "source_pins_after": self.manifest["source_pins"], "candidates": rows,
        }

    def test_exact_t1_scope_and_eight_compiled_provider_rows(self):
        self.assertEqual(checks.EXPECTED_KEYS, {candidate["key"] for candidate in self.candidates})
        self.assertEqual(18, len(self.projection["structure_rows"]))
        self.assertEqual(8, len(self.candidates))
        self.assertGreaterEqual(len(self.manifest["source_pins"]), 390)
        self.assertTrue(self.manifest["provider_local_ids_are_unqualified"])

    def test_satelite_dish_config_override_is_preserved_in_t1_expected_values(self):
        candidate = next(row for row in self.candidates if row["key"] == "Building_Satelite_Dish")
        self.assertEqual(500, candidate["input"]["prefab_max_health"])
        self.assertEqual(450, candidate["input"]["config_max_health"])
        self.assertEqual(450, candidate["expected"]["max_health"])

    def test_ground_staging_uses_explicit_special_source_and_unqualified_root_id(self):
        candidate = next(row for row in self.candidates if row["key"] == "Building_GroundStaging")
        self.assertTrue(candidate["special_source_binding"])
        self.assertEqual("not_applicable_special_source_binding", candidate["provider"]["local_id_validation"])
        observation = self.observation()
        checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])

    def test_observed_root_local_id_is_not_compared_to_t1_yaml_file_id(self):
        observation = self.observation()
        candidate = next(row for row in self.candidates if row["key"] == "Building_Barrack")
        row = next(row for row in observation["candidates"] if row["key"] == candidate["key"])
        self.assertNotEqual(str(candidate["provider"]["file_id"]),
                            row["actual_prefab_main_root_identity"]["local_id"])
        checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])

    def test_rejects_missing_or_duplicate_candidate(self):
        observation = self.observation()
        observation["candidates"].pop()
        with self.assertRaisesRegex(ValueError, "exact eight"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])
        observation = self.observation()
        observation["candidates"][-1]["key"] = observation["candidates"][0]["key"]
        with self.assertRaisesRegex(ValueError, "duplicated"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])

    def test_rejects_wrong_root_or_source_component_identity(self):
        observation = self.observation()
        observation["candidates"][0]["actual_prefab_main_root_identity"]["guid"] = "wrong"
        with self.assertRaisesRegex(ValueError, "root identity"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])
        observation = self.observation()
        observation["candidates"][0]["resolved_original_source_component_identity"]["local_id"] = "wrong"
        with self.assertRaisesRegex(ValueError, "source authoring component"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])

    def test_rejects_wrong_effective_config_or_scalar_result(self):
        observation = self.observation()
        observation["candidates"][0]["effective_config_identity"]["guid"] = "wrong"
        with self.assertRaisesRegex(ValueError, "effective config"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])
        observation = self.observation()
        observation["candidates"][0]["original_metadata_owner_scalar_values"]["price"] += 1
        with self.assertRaisesRegex(ValueError, "scalar field price"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])

    def test_rejects_saved_or_loaded_mission_scene_and_changed_source_pin(self):
        observation = self.observation()
        observation["prefab_asset_saved"] = True
        with self.assertRaisesRegex(ValueError, "scope flags"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])
        observation = self.observation()
        observation["authored_or_mission_scene_loaded"] = True
        with self.assertRaisesRegex(ValueError, "scope flags"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])
        observation = self.observation()
        observation["source_pins_after"] = {}
        with self.assertRaisesRegex(ValueError, "source pins"):
            checks.validate_observation(observation, self.candidates, self.manifest["source_pins"])

    def test_wrapper_uses_rtk_proxy_and_checked_repository_wrapper(self):
        command = runner.build_command(Path("log"), Path("out"), Path("pins"), 600)
        self.assertEqual(command[:3], [runner.RTK, "proxy", str(runner.WRAPPER)])
        self.assertEqual(command[command.index("-executeMethod") + 1], runner.EXECUTE_METHOD)
        self.assertNotIn("-batchmode", command)

    def test_t1_duplicate_provider_rows_must_match(self):
        projection = copy.deepcopy(self.projection)
        same_key = [row for row in projection["structure_rows"] if row.get("visual_key") == "Building_Barrack"]
        self.assertEqual(2, len(same_key))
        same_key[1]["provider_prefab"]["config"]["config_path"] = "wrong.asset"
        with self.assertRaisesRegex(ValueError, "disagree on provider identity"):
            checks.provider_facts(projection)

    def test_t1_candidate_prefab_path_must_join_to_compiled_provider(self):
        inputs = copy.deepcopy(self.inputs)
        inputs["rows"][0]["source_prefab_path"] = "Assets/Game/Prefabs/Buildings/Other.prefab"
        with self.assertRaisesRegex(ValueError, "candidate path differs"):
            checks.build_candidates(ROOT, self.projection, inputs)

    def test_observer_uses_unloaded_preview_contents_and_never_saves_prefabs(self):
        source = (ROOT / "Assets/Game/Scripts/Editor/Skirmish/"
                  "S002BuildingPrefabScalarMetadataReadback.cs").read_text()
        self.assertIn("PrefabUtility.LoadPrefabContents(path)", source)
        self.assertIn("PrefabUtility.UnloadPrefabContents(prefabContents)", source)
        self.assertIn("AssetDatabase.LoadMainAssetAtPath(path)", source)
        self.assertIn("GetCorrespondingObjectFromOriginalSource(authoring)", source)
        self.assertNotIn("SaveAsPrefabAsset", source)
        self.assertNotIn("SavePrefabAsset", source)


if __name__ == "__main__":
    unittest.main()
