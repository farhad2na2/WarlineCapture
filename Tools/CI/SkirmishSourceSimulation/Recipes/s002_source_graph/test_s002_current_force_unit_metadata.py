from __future__ import annotations

import copy
import json
from pathlib import Path
import unittest
from unittest.mock import patch

import run_s002_current_force_unit_metadata as runner
import s002_current_force_unit_metadata as checks


class CurrentForceMetadataReadbackTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.receipt, cls.projection = checks.load_t5(
            runner.ROOT, runner.T5_RECEIPT, runner.T5_PROJECTION)
        cls.specs = checks.build_provider_specs(runner.ROOT, cls.projection)

    def test_current_t5_reduces_to_exact_eight_unique_provider_sources(self):
        self.assertEqual({row["key"] for row in self.specs}, checks.EXPECTED_KEYS)
        self.assertEqual(8, len(self.specs))
        self.assertEqual(16, len(self.projection["force_rows"]))
        self.assertEqual(18, len(self.projection["structure_rows"]))
        self.assertEqual(2, len(next(row for row in self.specs if row["key"] == "Unit_Veh_Tank_USA")["config_candidates"]))

    def test_duplicate_force_join_cannot_disagree_about_provider_source(self):
        projection = copy.deepcopy(self.projection)
        projection["force_rows"][1]["provider_prefab"]["path"] += ".mutated"
        with self.assertRaisesRegex(ValueError, "disagree"):
            checks.build_provider_specs(runner.ROOT, projection)

    def test_missing_or_extra_provider_key_rejects(self):
        projection = copy.deepcopy(self.projection)
        projection["force_rows"] = [row for row in projection["force_rows"]
                                    if row["provider_prefab"]["key"] != "Unit_Veh_Truck_Tray"]
        with self.assertRaisesRegex(ValueError, "exact eight"):
            checks.build_provider_specs(runner.ROOT, projection)

    def test_owner_binding_candidate_mismatch_rejects(self):
        projection = copy.deepcopy(self.projection)
        rows = [row for row in projection["force_rows"]
                if row["provider_prefab"]["key"] == "Unit_Veh_Tank_USA"]
        for row in rows:
            row["provider_prefab"]["config"]["owner_bindings"][0]["config_path"] += ".wrong"
        with self.assertRaisesRegex(ValueError, "owner binding"):
            checks.build_provider_specs(runner.ROOT, projection)

    def _valid_shape_observation(self):
        rows = []
        for spec in self.specs:
            candidate = spec["config_candidates"][0]
            overrides = []
            for binding in spec["owner_bindings"]:
                overrides.append({
                    "binding_match": True,
                    "target_identity": {"path": binding["owner_path"], "guid": binding["owner_guid"],
                                        "local_id": binding["owner_local_id"]},
                    "config_identity": {"path": binding["config_path"], "guid": binding["config_guid"],
                                        "local_id": binding["config_local_id"]},
                })
            rows.append({
                "key": spec["key"], "status": "Passed", "asset_path": spec["path"],
                "original_production_metadata_owner":
                    "Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper.TryGetMetadata",
                "actual_asset_identity": {"guid": spec["guid"], "local_id": str(spec["file_id"])},
                "effective_config_identity": candidate, "effective_config_candidate_match": True,
                "config_override_rows": overrides, "config_override_issues": [],
                "production_metadata": {field: None for field in checks.OWNER_METADATA} | {
                    "production_transport_prefab_is_null": True,
                    "transport_reference_matches_config": True,
                    "footprint_cells": {"x": 1, "y": 1},
                    "is_air_unit": False,
                    "production_transport_max_concurrent": 1,
                },
            })
        return {
            "kind": "S002CurrentForceUnitMetadataObservation", "status": "Passed",
            "scope": "ImportedProviderMetadataOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderAdmission_NoReadiness",
            **{key: False for key in ("observer_loaded_or_modified_scene", "observer_created_world",
                                       "observer_invoked_baker", "provider_admission_performed",
                                       "native_capture_used", "source_fit_verified", "mission_ready")},
            "source_pins_before": {}, "source_pins_after": {},
            "providers": rows,
        }

    def test_finalizer_rejects_missing_provider_and_duplicate_key(self):
        observation = self._valid_shape_observation()
        del observation["providers"][-1]
        with self.assertRaisesRegex(ValueError, "eight provider"):
            checks.validate_observation(observation, self.specs)
        observation = self._valid_shape_observation()
        observation["providers"][-1]["key"] = observation["providers"][0]["key"]
        with self.assertRaisesRegex(ValueError, "duplicate"):
            checks.validate_observation(observation, self.specs)

    def test_finalizer_rejects_unpinned_config_transport_disagreement(self):
        observation = self._valid_shape_observation()
        observation["providers"][0]["production_metadata"]["transport_reference_matches_config"] = False
        with self.assertRaisesRegex(ValueError, "transport reference"):
            checks.validate_observation(observation, self.specs)

    def test_finalizer_requires_owner_name_at_provider_level(self):
        observation = self._valid_shape_observation()
        del observation["providers"][0]["original_production_metadata_owner"]
        with self.assertRaisesRegex(ValueError, "original metadata owner"):
            checks.validate_observation(observation, self.specs)

    def test_finalizer_rejects_nested_override_issue_or_unpinned_config(self):
        observation = self._valid_shape_observation()
        observation["providers"][0]["config_override_issues"] = ["unmatched override"]
        with self.assertRaisesRegex(ValueError, "override issues"):
            checks.validate_observation(observation, self.specs)
        observation = self._valid_shape_observation()
        row = observation["providers"][0]
        row["config_override_rows"] = [{"binding_match": True, "config_identity": {"guid": "wrong"}}]
        with self.assertRaisesRegex(ValueError, "not one unique T5-pinned"):
            checks.validate_observation(observation, self.specs)

    def test_distinct_inherited_override_candidate_can_differ_from_effective_root_config(self):
        observation = self._valid_shape_observation()
        spec = next(spec for spec in self.specs if spec["key"] == "Unit_Veh_Tank_USA")
        row = next(row for row in observation["providers"] if row["key"] == spec["key"])
        self.assertEqual(2, len(spec["config_candidates"]))
        row["effective_config_identity"] = spec["config_candidates"][1]
        checks.validate_observation(observation, self.specs)

    def test_finalizer_rejects_missing_and_duplicate_override_binding_rows(self):
        observation = self._valid_shape_observation()
        row = next(row for row in observation["providers"] if row["key"] == "Unit_Veh_Tank_USA")
        row["config_override_rows"].pop()
        with self.assertRaisesRegex(ValueError, "cover each T5 binding exactly once"):
            checks.validate_observation(observation, self.specs)
        observation = self._valid_shape_observation()
        row = next(row for row in observation["providers"] if row["key"] == "Unit_Veh_Tank_USA")
        row["config_override_rows"].append(copy.deepcopy(row["config_override_rows"][0]))
        with self.assertRaisesRegex(ValueError, "cover each T5 binding exactly once"):
            checks.validate_observation(observation, self.specs)

    def test_finalizer_requires_exact_execution_manifest_pin_map(self):
        observation = self._valid_shape_observation()
        observation["source_pins_before"] = {"source.cs": "a"}
        observation["source_pins_after"] = {"source.cs": "a"}
        with self.assertRaisesRegex(ValueError, "manifest"):
            checks.validate_observation(observation, self.specs, {})

    def test_wrapper_command_requires_rtk_proxy_and_checked_repository_wrapper(self):
        self.assertTrue(runner.WRAPPER.is_file())
        self.assertEqual("rtk", Path(runner.RTK).name)
        command = runner.build_wrapper_command(Path("o"), Path("p"), Path("l"), 600)
        self.assertEqual(command[:3], [runner.RTK, "proxy", str(runner.WRAPPER)])
        self.assertEqual(command[command.index("-executeMethod") + 1], runner.EXECUTE_METHOD)
        self.assertEqual("ImportedProviderMetadataOnly", runner.PASS_MARKER.split("scope=")[1])

    def test_postrun_pin_failure_is_returned_for_failed_receipt(self):
        with patch.object(runner, "pins", side_effect=OSError("source disappeared")):
            actual, failure = runner.postrun_pin_snapshot(runner.ROOT, {"source.cs": "abc"})
        self.assertEqual({}, actual)
        self.assertIn("Post-run source pin check failed: source disappeared", failure)

    def test_non_config_imported_reference_remains_a_provider_failure(self):
        source = (runner.ROOT / "Assets/Game/Scripts/Editor/Skirmish/"
                  "S002CurrentForceUnitMetadataObserver.cs").read_text()
        self.assertIn("UnitGridAuthoringConfig unitConfig = configObject as UnitGridAuthoringConfig;", source)
        self.assertIn("if (unitConfig == null)", source)
        self.assertIn('issues.Add("Root UnitGridAuthoring config is not a UnitGridAuthoringConfig.");', source)


if __name__ == "__main__":
    unittest.main()
