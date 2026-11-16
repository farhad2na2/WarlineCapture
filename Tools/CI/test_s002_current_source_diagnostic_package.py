#!/usr/bin/env python3
"""Focused package derivation and batch-classification controls; no source simulation."""
import hashlib
import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CI = ROOT / "Tools/CI"
sys.path.insert(0, str(CI))
import s002_current_source_diagnostic_package as package_reader

PACKAGE = ROOT / "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T2/package.json"
PACKAGE_SHA = "de74b8fa217fdb515b3bbdbc36c26f8f18761f5b1ffae6156278823a03563628"
SELECTED_MAP_PACKAGE = ROOT / package_reader.SELECTED_MAP_PACKAGE_PATH
GRID_EXTENDED_PACKAGE = ROOT / package_reader.GRID_EXTENDED_PACKAGE_PATH

spec = importlib.util.spec_from_file_location("source_batch", CI / "run_skirmish_source_batch.py")
batch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(batch)


class CurrentSourceDiagnosticPackageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.loaded = package_reader.load_current_s002_package(PACKAGE, PACKAGE_SHA, ROOT)
        cls.base_path = ROOT / "T Logs/SkirmishProduction/s002-current-source-diagnostic-input-20261104-T3/base-initialization-r10.json"
        cls.base = json.loads(cls.base_path.read_text())
        cls.base_sha = hashlib.sha256(cls.base_path.read_bytes()).hexdigest()
        cls.derived, cls.provenance = package_reader.derive_current_s002_initialization(
            cls.base, str(cls.base_path), cls.base_sha, cls.loaded)
        cls.descriptor = {"path": str(PACKAGE), "sha256": PACKAGE_SHA}

    def test_current_package_and_owner_rows_are_bound(self):
        self.assertEqual(len(self.loaded["building_rows"]), 8)
        self.assertEqual(len(self.loaded["unit_rows"]), 12)
        self.assertGreaterEqual(len(self.loaded["source_pins"]), 8000)

    def test_package_digest_tamper_rejected(self):
        with self.assertRaises(ValueError):
            package_reader.load_current_s002_package(PACKAGE, "0" * 64, ROOT)
        with tempfile.TemporaryDirectory() as wrong_root:
            with self.assertRaises(ValueError):
                package_reader.load_current_s002_package(PACKAGE, PACKAGE_SHA, wrong_root)

    def test_derived_selected_map_package_reproduces_without_package_hash_whitelist(self):
        raw = SELECTED_MAP_PACKAGE.read_bytes()
        digest = hashlib.sha256(raw).hexdigest()
        loaded = package_reader.load_current_s002_package(SELECTED_MAP_PACKAGE, digest, ROOT)
        self.assertEqual(digest, loaded["sha256"])
        self.assertEqual(package_reader.SELECTED_MAP_PACKAGE_PATH,
                         Path(loaded["path"]).relative_to(ROOT).as_posix())
        self.assertEqual(package_reader.SELECTED_MAP_GRAPH_PATH,
                         Path(loaded["source_config_graph"]["path"]).relative_to(ROOT).as_posix())
        self.assertFalse(loaded["value"]["native_context_qualified"])
        self.assertFalse(loaded["value"]["source_fit_certified"])
        self.assertFalse(loaded["value"]["mission_ready"])
        self.assertIn(str(SELECTED_MAP_PACKAGE.resolve()), loaded["source_pins"])
        self.assertIn(str((ROOT / package_reader.SELECTED_MAP_GRAPH_PATH).resolve()), loaded["source_pins"])
        with self.assertRaises(ValueError):
            package_reader.load_current_s002_package(SELECTED_MAP_PACKAGE, "0" * 64, ROOT)
        with tempfile.TemporaryDirectory() as temporary:
            alias = Path(temporary) / "package.json"
            alias.write_bytes(raw)
            with self.assertRaises(ValueError):
                package_reader.load_current_s002_package(alias, digest, ROOT)

    def test_grid_extended_package_reproduces_from_current_graph_owner(self):
        raw = GRID_EXTENDED_PACKAGE.read_bytes()
        digest = hashlib.sha256(raw).hexdigest()
        loaded = package_reader.load_current_s002_package(GRID_EXTENDED_PACKAGE, digest, ROOT)
        self.assertEqual(digest, loaded["sha256"])
        self.assertEqual(package_reader.GRID_EXTENDED_GRAPH_PATH,
                         Path(loaded["source_config_graph"]["path"]).relative_to(ROOT).as_posix())
        graph = json.loads((ROOT / package_reader.GRID_EXTENDED_GRAPH_PATH).read_text())
        grid = [row for row in graph["nodes"] if row.get("source_path") == "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"]
        self.assertEqual(1, len(grid))
        self.assertEqual("b201000000000000000000000000000b", grid[0]["asset_guid"])
        self.assertFalse(loaded["value"]["mission_ready"])
        self.assertIn(str(GRID_EXTENDED_PACKAGE.resolve()), loaded["source_pins"])
        with self.assertRaises(ValueError):
            package_reader.load_current_s002_package(GRID_EXTENDED_PACKAGE, "0" * 64, ROOT)

    def test_parent_symlink_and_wrong_base_initialization_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            target = root / "target"
            target.mkdir()
            (target / "input.json").write_text("{}")
            (root / "linked").symlink_to(target, target_is_directory=True)
            with self.assertRaises(ValueError):
                package_reader._canonical(root, "linked/input.json")
        with self.assertRaises(ValueError):
            package_reader.derive_current_s002_initialization(
                self.base, str(ROOT / "wrong-base.json"), self.base_sha, self.loaded)

    def test_external_template_key_requires_canonical_path_and_all_components_non_symlink(self):
        key_path, key_sha = package_reader.TEMPLATE_KEY_DESCRIPTOR
        raw = package_reader._read_exact_external_pin(key_path, key_path, key_sha, key_sha)
        self.assertEqual(key_sha, hashlib.sha256(raw).hexdigest())
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            target = root / "target"
            target.mkdir()
            payload = target / "key.json"
            payload.write_text("{}")
            digest = hashlib.sha256(payload.read_bytes()).hexdigest()
            parent_link = root / "linked-parent"
            parent_link.symlink_to(target, target_is_directory=True)
            linked_child = parent_link / "key.json"
            with self.assertRaises(ValueError):
                package_reader._read_exact_external_pin(str(linked_child), str(linked_child), digest, digest)
            leaf_link = root / "linked-key.json"
            leaf_link.symlink_to(payload)
            with self.assertRaises(ValueError):
                package_reader._read_exact_external_pin(str(leaf_link), str(leaf_link), digest, digest)
            with self.assertRaises(ValueError):
                package_reader._read_exact_external_pin(str(payload), str(root / "different.json"), digest, digest)

    def test_derivation_selects_diagnostic_epoch_and_removes_legacy_fields(self):
        self.assertEqual(self.derived["selection"]["seed"], 104731)
        self.assertTrue(self.derived["source_epoch"].startswith("S002-current-owner-source-diagnostic-v1:"))
        for key in ("metadata_compatibility", "metadata_source_origin", "s002_derived_seed"):
            self.assertNotIn(key, self.derived)
        self.assertFalse(self.provenance["native_context_qualified"])
        self.assertFalse(self.provenance["source_fit_certified"])
        self.assertFalse(self.provenance["mission_ready"])

    def test_historical_placement_graft_preserves_t3_entry_and_epoch(self):
        binding = package_reader.load_s002_historical_placement_binding(ROOT)
        derived, _ = package_reader.derive_current_s002_initialization(
            self.base, str(self.base_path), self.base_sha, self.loaded, binding)
        baseline, _ = package_reader.derive_current_s002_initialization(
            self.base, str(self.base_path), self.base_sha, self.loaded)
        derivation = package_reader.make_s002_historical_placement_derivation(binding)
        self.assertTrue(package_reader.require_s002_historical_placement_derivation(
            derived, self.base, binding, derivation))
        self.assertEqual(self.base["placement_domain"]["s002_diagnostic_capture"],
                         derived["placement_domain"]["s002_diagnostic_capture"])
        self.assertEqual(binding["placement_descriptor"],
                         derived["placement_domain"]["s002_original_owner_diagnostic"])
        self.assertEqual(baseline["source_epoch"], derived["source_epoch"])
        self.assertNotIn("s002_historical_placement_derivation", derived)
        self.assert_schema_accepts_initialization(derived)
        self.assertEqual(5, len(binding["source_pins"]))

    def test_historical_placement_graft_rejects_mismatched_or_omitted_descriptor(self):
        binding = package_reader.load_s002_historical_placement_binding(ROOT)
        with self.assertRaises(ValueError):
            package_reader.derive_current_s002_initialization(
                self.base, str(self.base_path), self.base_sha, self.loaded,
                {**binding, "placement_descriptor": {}})
        derived, _ = package_reader.derive_current_s002_initialization(
            self.base, str(self.base_path), self.base_sha, self.loaded, binding)
        omitted = json.loads(json.dumps(derived))
        omitted["placement_domain"].pop("s002_original_owner_diagnostic")
        with self.assertRaises(ValueError):
            package_reader.require_s002_historical_placement_derivation(
                omitted, self.base, binding, package_reader.make_s002_historical_placement_derivation(binding))
        mismatched = json.loads(json.dumps(derived))
        mismatched["placement_domain"]["s002_original_owner_diagnostic"]["physical"]["sha256"] = "0" * 64
        with self.assertRaises(ValueError):
            package_reader.require_s002_historical_placement_derivation(
                mismatched, self.base, binding, package_reader.make_s002_historical_placement_derivation(binding))

    def assert_schema_accepts_initialization(self, initial):
        import re
        adapter = CI / "SkirmishSourceSimulation/Adapters/513_FreshSourceStorageLifetimeAuthority.cs"
        source = adapter.read_text()
        match = re.search(r'new HashSet<string>\(new\[\]\{([^}]+)\}', source)
        self.assertIsNotNone(match)
        allowed = set(re.findall(r'"([^"]+)"', match.group(1)))
        self.assertTrue(set(initial).issubset(allowed), sorted(set(initial) - allowed))

    def make_receipt(self, descriptor=None):
        historical = {
            "kind": "HistoricalInputOnlyDiagnostic_Unqualified", "historical_input_epoch": "2026-11-01",
            "physical_sha256": "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
            "water_sha256": "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
            "startup_sha256": "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
            "preservation_sha256": "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297",
            "historical_producer_digest_bytes_verified": False, "historical_producer_digest_only_count": 7,
            "preserved_source_pin_count": 6526, "preservation_project_file_count": 6522,
            "preservation_external_pin_count": 4, "historical_navigation_row_count": 4954,
            "current_source_equivalence": False, "current_blocker_equivalence": False,
            "current_surface_equivalence": False, "physical_source_fit": False, "water_equivalence": False,
            "scene_fit_certified": False, "mission_admitted": False, "mission_ready": False,
            "native_world_created": False, "native_scene_loaded": False, "native_input_observed": False,
            "player_order_observed": False, "validated_victory": False,
        }
        raw = {"status": "BoundedPrefix", "source_outcome": None, "historical_input_classification": historical}
        historical_pins = {
            str(ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json"): "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
            str(ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-water-capture.json"): "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5",
            str(ROOT / "Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-startup-observation.json"): "ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03",
            str(ROOT / "Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json"): "68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297",
        }
        historical_pins.update(self.loaded["source_pins"])
        if descriptor is not None:
            raw["s002_current_source_diagnostic_package"] = descriptor
        return {
            "kind": "SkirmishSourceSimulationRunReceipt", "schema_version": 1,
            "mission": "S002", "seed": 104731, "diagnostic_only": True,
            "validation_passed": False, "camera_qualified": False,
            "active_mission_water_qualified": False, "source_fit_certified": False,
            "native_acceptance": False, "runtime_presentation_qualified": False,
            "mission_ready": False, "status": "BoundedPrefix", "process_exit": 0,
            "timed_out": False, "full_outcome_observed": False,
            "diagnostic_prefix_completed": True,
            "data_repository_root": str(ROOT), "input_and_runtime_pins": historical_pins,
            "s002_current_source_diagnostic_package_bound_by_worker": True,
            "s002_current_source_diagnostic_package": descriptor,
            "source_result": raw,
        }

    def test_batch_classifies_terminal_diagnostic_with_null_victory(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True,
               "s002_current_source_diagnostic_package": self.descriptor}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 0}
        result = batch.classify(job, process, self.make_receipt(self.descriptor))
        self.assertTrue(result["execution_complete"])
        self.assertIsNone(result["mission_victory"])
        self.assertFalse(result["validation_passed"])

    def test_batch_rejects_omitted_or_aliased_receipt(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True,
               "s002_current_source_diagnostic_package": self.descriptor}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 0}
        for desc in (None, {"path": "/tmp/copied-package.json", "sha256": PACKAGE_SHA}):
            result = batch.classify(job, process, self.make_receipt(desc))
            self.assertEqual(result["status"], "Failed")
            self.assertIsNone(result["mission_victory"])

    def test_batch_rejects_unrequested_package_receipt(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 0}
        result = batch.classify(job, process, self.make_receipt(self.descriptor))
        self.assertEqual(result["status"], "Failed")

    def test_batch_requires_matching_historical_scope_and_cli_shape(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True,
               "s002_current_source_diagnostic_package": self.descriptor}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 0}
        receipt = self.make_receipt(self.descriptor)
        receipt["source_result"]["historical_input_classification"]["physical_source_fit"] = True
        self.assertEqual(batch.classify(job, process, receipt)["status"], "Failed")
        receipt = self.make_receipt(self.descriptor)
        self.assertEqual(batch.classify(job, {**process, "cli_exit": 1}, receipt)["status"], "Failed")

    def test_package_lane_requires_historical_descriptor_pins(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True,
               "s002_current_source_diagnostic_package": self.descriptor}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 0}
        for mode in ("omitted", "mismatched"):
            receipt = self.make_receipt(self.descriptor)
            if mode == "omitted":
                receipt["input_and_runtime_pins"] = {}
            else:
                path = next(iter(receipt["input_and_runtime_pins"]))
                receipt["input_and_runtime_pins"][path] = "0" * 64
            result = batch.classify(job, process, receipt)
            self.assertEqual(result["status"], "Failed")
            self.assertIsNone(result["mission_victory"])

    def test_batch_requires_reproduced_package_owner_pins(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True,
               "s002_current_source_diagnostic_package": self.descriptor}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 0}
        receipt = self.make_receipt(self.descriptor)
        self.assertTrue(batch.valid_current_s002_package_binding(
            self.descriptor, receipt["input_and_runtime_pins"]))
        path = next(path for path in self.loaded["source_pins"] if path.endswith("/package.json"))
        receipt["input_and_runtime_pins"].pop(path)
        result = batch.classify(job, process, receipt)
        self.assertEqual("CurrentS002DiagnosticPackageSourceOwnerMismatch", result["reason"])

    def test_false_worker_package_binding_rejected(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True,
               "s002_current_source_diagnostic_package": self.descriptor}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 0}
        receipt = self.make_receipt(self.descriptor)
        receipt["s002_current_source_diagnostic_package_bound_by_worker"] = False
        self.assertEqual(batch.classify(job, process, receipt)["status"], "Failed")

    def test_terminal_diagnostic_requires_terminal_worker_and_cli_one(self):
        job = {"mission": "S002", "seed": 104731, "max_ticks": 100,
               "s002_historical_input_diagnostic": True,
               "s002_current_source_diagnostic_package": self.descriptor}
        process = {"owned_descendants_survived_cli": False, "outer_timeout": False, "cli_exit": 1}
        receipt = self.make_receipt(self.descriptor)
        receipt.update(status="SourceOutcomeObserved", full_outcome_observed=True,
                       diagnostic_prefix_completed=False)
        receipt["source_result"].update(status="SourceTerminal", source_outcome="Victory")
        self.assertIsNone(batch.classify(job, process, receipt)["mission_victory"])
        receipt["source_result"]["status"] = "BoundedPrefix"
        self.assertEqual(batch.classify(job, process, receipt)["status"], "Failed")

    def test_batch_classifier_self_pin_is_bound(self):
        pin = batch.batch_classifier_pin()
        batch.require_batch_classifier_pin(pin)
        with self.assertRaises(ValueError):
            batch.require_batch_classifier_pin({**pin, "sha256": "0" * 64})


if __name__ == "__main__":
    unittest.main(verbosity=2)
