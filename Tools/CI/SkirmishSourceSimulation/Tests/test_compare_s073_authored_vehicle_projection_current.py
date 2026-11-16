from __future__ import annotations

import hashlib
import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SCRIPT = Path(__file__).with_name("compare_s073_authored_vehicle_projection.py")
SPEC = importlib.util.spec_from_file_location("s073_authored_vehicle_compare_current", SCRIPT)
compare_module = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(compare_module)

SOURCE = ROOT / "Logs/SkirmishProduction/s073-authored-vehicle-projection-20261102-r3"
NATIVE = ROOT / "Logs/SkirmishProduction/s073-native-census-20261102-r2"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class CurrentAuthoredVehicleProjectionComparisonTests(unittest.TestCase):
    def compare_paths(self, source_receipt=None, source_input=None, source_projection=None,
                      native_receipt=None, native_snapshot=None):
        return compare_module.compare(
            source_receipt or SOURCE / "capture-receipt.json",
            source_input or SOURCE / "authored-vehicle-occupancy-inputs.json",
            source_projection or SOURCE / "projection.json",
            native_receipt or NATIVE / "receipt.json",
            native_snapshot or NATIVE / "snapshot.json", ROOT)

    def test_current_r3_r2_evidence_exactly_matches_and_preserves_readiness_limits(self):
        result = self.compare_paths()
        self.assertEqual(result["status"], "ExactFaction0CoordinateAndVehicleMultisetMatch")
        self.assertEqual(result["scope"],
                         "Independent native census and isolated pre-simulation authored neutral physical projection compared offline")
        self.assertTrue(result["sessions_are_independent"])
        self.assertEqual(result["set_overlap"]["shared_coordinate_count"], 109)
        self.assertEqual(result["set_overlap"]["source_only_count"], 0)
        self.assertEqual(result["set_overlap"]["native_faction_0_only_count"], 0)
        self.assertTrue(result["vehicle_multiset"]["exact_match"])
        self.assertFalse(result["identity_join_performed"])
        self.assertFalse(result["ownership_inferred"])
        self.assertFalse(result["full_parity_claim"])
        self.assertFalse(result["scheduler_admitted"])
        self.assertFalse(result["mission_ready"])
        self.assertFalse(result["source_validation"])
        self.assertFalse(result["state_transfer"])

        recorded_comparison = json.loads((SOURCE / "native-census-comparison-r2.json").read_text())
        self.assertEqual(recorded_comparison, result)

        source_input = json.loads((SOURCE / "authored-vehicle-occupancy-inputs.json").read_text())
        source_receipt = json.loads((SOURCE / "capture-receipt.json").read_text())
        native_snapshot = json.loads((NATIVE / "snapshot.json").read_text())
        native_receipt = json.loads((NATIVE / "receipt.json").read_text())
        self.assertEqual(source_input["vehicle_count"], 22)
        self.assertEqual(source_input["scope"],
                         "IsolatedPreGameStreamedCampaignSourceScene_AuthoredNeutralPhysicalOccupancyInputsOnly")
        self.assertFalse(source_input["source_fit_validated"])
        self.assertFalse(source_input["source_runtime_validated"])
        self.assertFalse(source_input["scheduler_admitted"])
        self.assertFalse(source_input["mission_ready"])
        self.assertFalse(source_receipt["mission_ready"])
        self.assertEqual(native_snapshot["scope"],
                         "RetainedOriginalS073World_ExplicitSyntheticCommand_NotNormalInputOrReadiness")
        self.assertIn("not player input", native_snapshot["input_provenance"])
        self.assertFalse(native_snapshot["normal_input_playthrough"])
        self.assertFalse(native_snapshot["player_readiness"])
        native_census = native_snapshot["pre_enqueue_input_observation"]["occupancy_contributor_census"]
        self.assertTrue(native_census["read_only"])
        self.assertFalse(native_census["state_imported"])
        self.assertFalse(native_census["gameplay_state_mutated"])
        self.assertEqual(native_receipt["source_pin_count"], 19)
        self.assertEqual(native_receipt["scope"], "IndependentInstrumentedNativeCapture_NotNormalInputOrReadiness")
        self.assertEqual(native_receipt["shutdown_intervention"], "None")
        self.assertFalse(native_receipt["mission_ready"])
        self.assertFalse(native_receipt["normal_input_playthrough"])
        self.assertFalse(native_receipt["player_readiness"])

        for capture in (source_input, native_snapshot["pre_enqueue_input_observation"]["occupancy_contributor_census"]):
            pins = capture["source_pins_sha256"]
            self.assertGreaterEqual(len(pins), 15)
            for relative, expected in pins.items():
                source = ROOT / relative
                self.assertTrue(source.is_file(), relative)
                self.assertEqual(sha(source), expected, relative)

    def test_current_native_kernel_tamper_fails_closed_after_receipt_rebind(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            snapshot = json.loads((NATIVE / "snapshot.json").read_text())
            row = next(r for r in snapshot["pre_enqueue_input_observation"]["occupancy_contributor_census"]["entities"]
                       if r["faction_id"] == 0)
            row["kernel_clamped_bounds_min"][0] += 1
            snapshot_path = folder / "snapshot.json"
            snapshot_path.write_text(json.dumps(snapshot))
            receipt = json.loads((NATIVE / "receipt.json").read_text())
            receipt["snapshot_sha256"] = sha(snapshot_path)
            receipt_path = folder / "receipt.json"
            receipt_path.write_text(json.dumps(receipt))
            shutil.copyfile(NATIVE / "editor.log", folder / "editor.log")
            with self.assertRaisesRegex(ValueError, "Native census row bounds"):
                self.compare_paths(native_receipt=receipt_path, native_snapshot=snapshot_path)


if __name__ == "__main__":
    unittest.main()
