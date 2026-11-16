import json
import tempfile
import unittest
from pathlib import Path
from audit_skirmish_source_evidence import audit

class Controls(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.d = Path(self.tmp.name)
        self.receipt = {"status": "SourceOutcomeObserved", "validation_passed": False, "source_result": {"status": "SourceTerminal", "tick": 5, "source_outcome": "Victory"}}
        self.write_receipt(self.receipt)
        self.write_rows([{"tick": 1}, {"tick": 5, "production_diagnostics": {"full_conservation_proved": False}}])
    def write_receipt(self, obj):
        (self.d / "run-receipt.json").write_text(json.dumps(obj))
    def write_rows(self, rows):
        (self.d / "validator-state.jsonl").write_text("".join(json.dumps(r) + "\n" for r in rows))
    def test_terminal_and_order(self):
        result = audit(self.d)
        self.assertTrue(result["terminal_tick_matches_last_snapshot_tick"])
        self.assertTrue(result["snapshot_ticks_monotonic"])
        self.assertFalse(result["full_conservation_proved_reported"])
        self.assertFalse(result["validation_passed"])
        self.write_rows([{"tick": 5}, {"tick": 1}])
        result = audit(self.d)
        self.assertFalse(result["snapshot_ticks_monotonic"])
        self.assertFalse(result["terminal_tick_matches_last_snapshot_tick"])
        self.assertIsNone(result["full_conservation_proved_reported"])
    def test_non_object_inputs(self):
        for value in ([], 1, None, "x"):
            with self.subTest(receipt=value):
                self.write_receipt(value)
                with self.assertRaises(ValueError): audit(self.d)
            self.write_receipt({"source_result": value})
            with self.assertRaises(ValueError): audit(self.d)
            self.write_receipt(self.receipt)
            self.write_rows([{"tick": 5, "production_diagnostics": value}])
            with self.assertRaises(ValueError): audit(self.d)
    def test_bad_reported_values(self):
        for key, value in (("full_conservation_proved", "true"), ("fuel_conservation", []), ("actor_delivery_disposition", {}), ("queue_diagnostic_faults", True), ("queue_diagnostic_failure_flags", "0")):
            with self.subTest(key=key):
                self.write_rows([{"tick": 5, "production_diagnostics": {key: value}}])
                with self.assertRaises(ValueError): audit(self.d)
    def test_sampled_producer_status_counts(self):
        self.write_rows([
            {"tick": 1, "production_diagnostics": {"sampled_material_counter_balance": {"status": "Baseline"}, "queue_diagnostic_faults": 0}},
            {"tick": 5, "production_diagnostics": {"sampled_material_counter_balance": {"status": "Mismatch"}, "queue_diagnostic_faults": 2}},
        ])
        result = audit(self.d)
        self.assertEqual(result["sampled_material_counter_balance_status_counts_reported"], {"Baseline": 1, "Mismatch": 1})
        self.assertEqual(result["sampled_queue_fault_counts_reported"], {"0": 1, "2": 1})
        self.assertEqual(result["sampled_queue_failure_flags_reported"], {"Missing": 2})
        self.write_rows([{"tick": 5, "production_diagnostics": {"sampled_material_counter_balance": []}}])
        with self.assertRaises(ValueError): audit(self.d)
    def test_empty_balance_status_rejected_and_absence_preserved(self):
        self.write_rows([{"tick": 5, "production_diagnostics": {"sampled_material_counter_balance": {"status": ""}}}])
        with self.assertRaises(ValueError): audit(self.d)
        self.write_rows([{"tick": 5, "production_diagnostics": {"sampled_material_counter_balance": {}}}])
        self.assertEqual(audit(self.d)["sampled_material_counter_balance_status_counts_reported"], {"Missing": 1})
    def test_terminal_tick_must_be_integer(self):
        for value in (None, "5", True, 5.0):
            with self.subTest(value=value):
                self.receipt["source_result"]["tick"] = value
                self.write_receipt(self.receipt)
                with self.assertRaises(ValueError): audit(self.d)
        del self.receipt["source_result"]["tick"]
        self.write_receipt(self.receipt)
        with self.assertRaises(ValueError): audit(self.d)
    def test_bad_and_empty_rows(self):
        for text in ("{\n", "", "[]\n", '{"tick": "5"}\n'):
            (self.d / "validator-state.jsonl").write_text(text)
            with self.assertRaises(ValueError): audit(self.d)

if __name__ == "__main__": unittest.main()
