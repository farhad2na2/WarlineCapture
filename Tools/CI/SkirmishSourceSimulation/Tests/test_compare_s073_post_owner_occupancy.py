import copy
import json
import tempfile
import unittest
from pathlib import Path

import compare_s073_post_owner_occupancy as subject


ROOT = Path(__file__).resolve().parents[4]
RUN = ROOT / "Logs/SkirmishProduction/s073-source-neutral-projection-run-20261101-r1"
PROJECTION_DIR = ROOT / "Logs/SkirmishProduction/s073-authored-vehicle-projection-20261101-r3"


class PostOwnerMaskTamperingTests(unittest.TestCase):
    def test_actual_mask_tamper_is_detected_even_with_updated_run_result_hash(self):
        result_path = RUN / "result.json"
        receipt_path = RUN / "run-receipt.json"
        result = json.loads(result_path.read_text(encoding="utf-8"))
        receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
        captured = result["building_and_unit_startup_diagnostic"]["neutral_occupancy_projection_import"]
        actual = captured["occupancy_union_comparison"]["actual_occupied_row_major_cells"]
        expected = set(actual)
        projection = json.loads((PROJECTION_DIR / "projection.json").read_text(encoding="utf-8"))
        source_sidecar = ROOT / "Logs/SkirmishProduction/s073-blocker-occupancy-comparison-20261101-r1/source-run/run/source-blocker-occupancy-r1.bin.json"
        source = Path(json.loads(source_sidecar.read_text(encoding="utf-8"))["raw_path"])
        raw_compare = subject.load_module("post_owner_tamper_raw_compare",
            ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/compare_s073_blocker_occupancy.py")
        _, baseline = raw_compare.load_capture(source, source_sidecar, "source")
        expected_union = {i for i in range(2048 * 1024) if raw_compare.bit(baseline["occupied"], i)}
        expected_union.update(projection["occupied_row_major_cells"])
        self.assertEqual(actual, sorted(actual))
        self.assertEqual(subject.occupancy_diff(actual, expected_union, 2048 * 1024), ([], []))

        tampered_result = copy.deepcopy(result)
        mask = tampered_result["building_and_unit_startup_diagnostic"]["neutral_occupancy_projection_import"]["occupancy_union_comparison"]["actual_occupied_row_major_cells"]
        replacement = next(i for i in range(2048 * 1024) if i not in set(mask))
        mask[0] = replacement
        with tempfile.TemporaryDirectory() as temp:
            temp = Path(temp)
            tampered_path = temp / "tampered-result.json"
            tampered_receipt_path = temp / "tampered-run-receipt.json"
            tampered_path.write_text(json.dumps(tampered_result), encoding="utf-8")
            receipt["worker_result"].update(path=str(tampered_path.resolve()),
                sha256=subject.sha(tampered_path), bytes=tampered_path.stat().st_size)
            tampered_receipt_path.write_text(json.dumps(receipt), encoding="utf-8")
            _, _, verified = subject.verify_source_run(tampered_path, tampered_receipt_path, ROOT)
            tampered_actual = verified["result"]["building_and_unit_startup_diagnostic"]["neutral_occupancy_projection_import"]["occupancy_union_comparison"]["actual_occupied_row_major_cells"]
            missing, extra = subject.occupancy_diff(tampered_actual, expected_union, 2048 * 1024)
            self.assertEqual(len(missing), 1)
            self.assertEqual(len(extra), 1)
            self.assertNotEqual(missing, extra)


if __name__ == "__main__":
    unittest.main()
