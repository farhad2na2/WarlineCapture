from __future__ import annotations

import importlib.util
import json
import shutil
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]
SCRIPT = Path(__file__).with_name("compare_s073_authored_vehicle_projection.py")
SPEC = importlib.util.spec_from_file_location("s073_authored_vehicle_compare", SCRIPT)
compare_module = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(compare_module)

BASE = ROOT / "Logs/SkirmishProduction"
SOURCE = BASE / "s073-authored-vehicle-projection-20261101-r3"
NATIVE = BASE / "s073-native-occupancy-census-20261101-r3"


class AuthoredVehicleProjectionComparisonTests(unittest.TestCase):
    def compare_paths(self, source_receipt=None, source_input=None, source_projection=None,
                      native_receipt=None, native_snapshot=None):
        return compare_module.compare(
            source_receipt or SOURCE / "capture-receipt.json",
            source_input or SOURCE / "authored-vehicle-occupancy-inputs.json",
            source_projection or SOURCE / "projection.json",
            native_receipt or NATIVE / "receipt.json",
            native_snapshot or NATIVE / "snapshot.json", ROOT)

    def test_actual_r3_evidence_exactly_matches_faction_zero_coordinate_and_vehicle_multisets(self):
        result = self.compare_paths()
        self.assertEqual(result["status"], "ExactFaction0CoordinateAndVehicleMultisetMatch")
        self.assertEqual(result["set_overlap"]["shared_coordinate_count"], 109)
        self.assertEqual(result["vehicle_multiset"]["exact_match"], True)
        self.assertFalse(result["identity_join_performed"])
        self.assertFalse(result["ownership_inferred"])
        self.assertFalse(result["mission_ready"])

    def test_source_receipt_tamper_fails_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            receipt = json.loads((SOURCE / "capture-receipt.json").read_text())
            receipt["input_sha256"] = "0" * 64
            (folder / "capture-receipt.json").write_text(json.dumps(receipt))
            shutil.copyfile(SOURCE / "editor.log", folder / "editor.log")
            with self.assertRaisesRegex(ValueError, "Source capture receipt/input/editor log"):
                self.compare_paths(source_receipt=folder / "capture-receipt.json")

    def test_native_kernel_row_tamper_fails_closed_even_with_rebound_receipt_hash(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            snapshot = json.loads((NATIVE / "snapshot.json").read_text())
            row = next(r for r in snapshot["pre_enqueue_input_observation"]["occupancy_contributor_census"]["entities"]
                       if r["faction_id"] == 0)
            row["kernel_clamped_bounds_min"][0] += 1
            snapshot_path = folder / "snapshot.json"
            snapshot_path.write_text(json.dumps(snapshot))
            receipt = json.loads((NATIVE / "receipt.json").read_text())
            import hashlib
            receipt["snapshot_sha256"] = hashlib.sha256(snapshot_path.read_bytes()).hexdigest()
            receipt_path = folder / "receipt.json"
            receipt_path.write_text(json.dumps(receipt))
            shutil.copyfile(NATIVE / "editor.log", folder / "editor.log")
            with self.assertRaisesRegex(ValueError, "Native census row bounds"):
                self.compare_paths(native_receipt=receipt_path, native_snapshot=snapshot_path)


if __name__ == "__main__":
    unittest.main()
