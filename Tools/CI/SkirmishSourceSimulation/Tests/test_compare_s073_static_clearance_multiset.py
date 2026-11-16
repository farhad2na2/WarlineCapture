#!/usr/bin/env python3
"""Focused controls for the S073 static-clearance multiset diagnostic."""
import importlib.util
from collections import Counter
from pathlib import Path
import unittest


SCRIPT = Path(__file__).with_name("compare_s073_static_clearance_multiset.py")
SPEC = importlib.util.spec_from_file_location("compare_s073_static_clearance_multiset", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class StaticClearanceMultisetTests(unittest.TestCase):
    def test_filter_applies_original_extent_and_rect_edge_rules(self):
        # The first box merely touches the Rect's right edge and is excluded by
        # the original xMin >= xMax check; the second overlaps by one float32 bit.
        bounds = [
            {"center": [11.0, 0.0, 5.0], "extents": [1.0, 0.0, 1.0]},
            {"center": [10.999999, 0.0, 5.0], "extents": [1.0, 0.0, 1.0]},
            {"center": [4.0, 0.0, 5.0], "extents": [0.249999, 0.0, 0.249999]},
            {"center": [5.0, 0.0, 5.0], "extents": [1.0, 0.0, 1.0]},
            {"center": [6.0, 0.0, 5.0], "extents": [20.000002, 0.0, 1.0]},
        ]
        selected, facts = MODULE.filter_source_bounds(bounds, [0, 0, 10, 10], .25, 20)
        self.assertEqual(facts["input_bounds"], 5)
        self.assertEqual(facts["selected_bounds"], 2)
        self.assertEqual(sum(selected.values()), 2)

    def test_bounds_key_preserves_single_precision_center_extent_arithmetic(self):
        bounds = {"center": [0.1, 2.0, 3.0], "extents": [0.2, 0.5, 0.25]}
        key, center, extents = MODULE._source_box(bounds)
        self.assertEqual(center[0], MODULE.f32(.1))
        self.assertEqual(extents[0], MODULE.f32(.2))
        self.assertEqual(MODULE._key_values(key)["min"][0], MODULE.f32(MODULE.f32(.1) - MODULE.f32(.2)))

    def test_multiset_reports_duplicates_and_exact_unmatched_counts(self):
        first = MODULE._box_key([0, 0, 0], [1, 1, 1])
        second = MODULE._box_key([2, 0, 0], [3, 1, 1])
        result = MODULE.compare_multisets(Counter({first: 2, second: 1}), Counter({first: 1}), 8)
        self.assertEqual(result["status"], "MultisetDifferencesObserved")
        self.assertEqual(result["source_duplicate_key_count"], 1)
        self.assertEqual(result["source_duplicate_excess_count"], 1)
        self.assertEqual(result["source_only_excess_count"], 2)
        self.assertEqual(result["native_only_excess_count"], 0)
        self.assertEqual(len(result["source_only_examples"]), 2)

    def test_retained_evidence_identity_and_receipt_chain(self):
        result = MODULE.compare(Path(__file__).resolve().parents[4], example_limit=4)
        self.assertEqual(result["native_observation"]["diagnostic_validation_status"], "Passed")
        self.assertEqual(result["multiset"]["source_count"], 2400)
        self.assertEqual(result["multiset"]["native_count"], 1899)
        self.assertEqual(result["multiset"]["source_only_excess_count"], 522)
        self.assertEqual(result["multiset"]["native_only_excess_count"], 21)
        self.assertEqual(result["source_capture"]["sha256"], MODULE.SOURCE_CAPTURE_SHA256)
        self.assertEqual(result["source_projection"]["sha256"], MODULE.PROJECTION_SHA256)
        self.assertEqual(result["native_observation"]["qualification_receipt_sha256"], MODULE.NATIVE_RECEIPT_SHA256)
        self.assertFalse(result["claims"]["native_state_imported"])
        self.assertFalse(result["claims"]["provider_parity_claim"])
        self.assertFalse(result["claims"]["current_source_equivalence_claim"])
        self.assertFalse(result["claims"]["mission_ready"])
        self.assertFalse(result["sessions_and_epochs"]["same_session"])
        self.assertIn("predates later S073 terrain/placement edits",
                      result["sessions_and_epochs"]["known_temporal_limit"])


if __name__ == "__main__":
    unittest.main()
