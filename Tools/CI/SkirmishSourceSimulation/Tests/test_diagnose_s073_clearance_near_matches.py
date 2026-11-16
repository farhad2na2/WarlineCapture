#!/usr/bin/env python3
"""Focused tests for the strict pinned S073 near-match diagnostic."""
from collections import Counter
import importlib.util
from pathlib import Path
import unittest


SCRIPT = Path(__file__).with_name("diagnose_s073_clearance_near_matches.py")
SPEC = importlib.util.spec_from_file_location("diagnose_s073_clearance_near_matches", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
EXACT = MODULE._load_exact_comparator()


def box(x_min):
    return EXACT._box_key([x_min, 0, 0], [x_min + 1, 1, 1])


class ClearanceNearMatchTests(unittest.TestCase):
    def test_tolerance_is_inclusive_and_each_occurrence_is_consumed_once(self):
        source = EXACT._box_key([0, 0, 0], [1, 1, 1])
        delta = EXACT.f32(MODULE.TOLERANCE_METERS)
        native = EXACT._box_key([delta, 0, 0], [1, 1, 1])
        result = MODULE.greedy_near_match(Counter({source: 1}), Counter({native: 2}), delta)
        self.assertEqual(len(result["pairs"]), 1)
        self.assertEqual(sum(result["unmatched_native"].values()), 1)
        self.assertEqual(sum(result["remaining_source_only"].values()), 0)

    def test_deterministic_nearest_then_key_tie_break(self):
        source_low = box(4.0)
        source_high = box(4.00001)
        native = box(4.000004)
        first = MODULE.greedy_near_match(Counter({source_low: 1, source_high: 1}), Counter({native: 1}))
        second = MODULE.greedy_near_match(Counter({source_high: 1, source_low: 1}), Counter({native: 1}))
        self.assertEqual(first["pairs"][0]["source_key"], second["pairs"][0]["source_key"])
        self.assertEqual(first["pairs"][0]["source_key"], source_low)

    def test_rejects_outside_tolerance(self):
        result = MODULE.greedy_near_match(Counter({box(1.0): 1}), Counter({box(1.001): 1}))
        self.assertEqual(result["pairs"], [])
        self.assertEqual(sum(result["unmatched_native"].values()), 1)
        self.assertEqual(sum(result["remaining_source_only"].values()), 1)

    def test_pinned_historical_result_keeps_exact_differences_and_false_claims(self):
        result = MODULE.analyze(Path(__file__).resolve().parents[4], example_limit=4)
        self.assertEqual(result["exact_comparison"]["status"], "MultisetDifferencesObserved")
        self.assertEqual(result["exact_comparison"]["source_only_excess_count"], 522)
        self.assertEqual(result["exact_comparison"]["native_only_excess_count"], 21)
        self.assertEqual(result["near_match_policy"]["tolerance_meters"], 1e-4)
        self.assertEqual(result["near_match_counts"]["native_only_excess_matched"], 21)
        self.assertEqual(result["near_match_counts"]["native_only_excess_unmatched"], 0)
        self.assertEqual(result["near_match_counts"]["source_only_excess_remaining"], 501)
        self.assertEqual(result["near_match_counts"]["maximum_matched_component_delta_m"], 6.103515625e-05)
        self.assertFalse(result["epochs_and_claims"]["same_session"])
        self.assertFalse(result["epochs_and_claims"]["native_state_imported"])
        self.assertFalse(result["epochs_and_claims"]["provider_parity_claim"])
        self.assertFalse(result["epochs_and_claims"]["causal_claim"])
        self.assertFalse(result["epochs_and_claims"]["current_source_equivalence_claim"])
        self.assertFalse(result["epochs_and_claims"]["mission_ready"])


if __name__ == "__main__":
    unittest.main()
