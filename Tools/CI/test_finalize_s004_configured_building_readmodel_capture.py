#!/usr/bin/env python3
"""Focused controls for offline S004 configured readmodel qualification."""
from __future__ import annotations

import copy
import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CAPTURE_DIR = ROOT / "Logs/SkirmishProduction/s004-configured-building-readmodel-capture-20261103-r3"
RAW_PATH = CAPTURE_DIR / "data/configured-spawnable-readmodel.json"
LOG_PATH = CAPTURE_DIR / "editor.log"
SETUP_PATH = ROOT / "Logs/SkirmishProduction/s004-standard-setup-export-20261103-r1/compiled-setup.json"
spec = importlib.util.spec_from_file_location("s004_readmodel_finalizer", Path(__file__).with_name("finalize_s004_configured_building_readmodel_capture.py"))
finalizer = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(finalizer)


class ConfiguredReadModelFinalizerControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.raw = json.loads(RAW_PATH.read_text(encoding="utf-8"))
        cls.evidence, cls.setup_rows, _ = finalizer.validate_setup(cls.raw, SETUP_PATH, ROOT)

    def test_actual_r3_capture_qualifies_without_writing_receipt(self):
        receipt = finalizer.verify(LOG_PATH, RAW_PATH, SETUP_PATH, ROOT, 0)
        self.assertEqual(receipt["distinctVisualKeyCount"], 9)
        self.assertEqual(receipt["publisherFieldCount"], 6)
        self.assertEqual(receipt["sourceEpochNote"],
                         "Distinct from earlier original factory-only captures; no definition-field parity is inferred across capture methods.")

    def test_nonzero_wrapper_exit_rejected(self):
        with self.assertRaisesRegex(finalizer.FinalizeError, "exit must be exactly zero"):
            finalizer.verify(LOG_PATH, RAW_PATH, SETUP_PATH, ROOT, 1)

    def test_real_r2_failure_log_without_pass_marker_rejected(self):
        old_dir = ROOT / "Logs/SkirmishProduction/s004-configured-building-readmodel-capture-20261103-r2"
        with self.assertRaisesRegex(finalizer.FinalizeError, "exactly one configured readmodel pass marker"):
            finalizer.verify(old_dir / "editor.log", old_dir / "data/configured-spawnable-readmodel.json",
                             SETUP_PATH, ROOT, 0)

    def test_setup_hash_mismatch_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["setup_hash"] += 1
        with self.assertRaisesRegex(finalizer.FinalizeError, "setup identity differs"):
            finalizer.validate_setup(raw, SETUP_PATH, ROOT)

    def test_source_pin_drift_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["source_pins"]["Assets/Game/Scripts/Systems/SpawnableKeyText.cs"] = "0" * 64
        with self.assertRaisesRegex(finalizer.FinalizeError, "differs from current file"):
            finalizer.validate_live_pins(raw, ROOT)

    def test_projection_price_mismatch_rejected(self):
        row = copy.deepcopy(self.raw["entries"][0])
        row["read_model"]["Price"] += 1
        with self.assertRaisesRegex(finalizer.FinalizeError, "differ from original projection inputs"):
            finalizer.validate_projection(row["source_definition"], row["read_model"], row["visual_key"])

    def test_projection_footprint_mismatch_rejected(self):
        row = copy.deepcopy(self.raw["entries"][0])
        row["read_model"]["FootprintCells"]["x"] += 1
        with self.assertRaisesRegex(finalizer.FinalizeError, "differ from original projection inputs"):
            finalizer.validate_projection(row["source_definition"], row["read_model"], row["visual_key"])

    def test_definition_display_name_binding_mismatch_rejected(self):
        row = copy.deepcopy(self.raw["entries"][0])
        row["source_definition"]["definition_projection_inputs"]["DisplayName"]["value"] = "Different"
        with self.assertRaisesRegex(finalizer.FinalizeError, "differ from original projection inputs"):
            finalizer.validate_projection(row["source_definition"], row["read_model"], row["visual_key"])

    def test_duplicate_visual_key_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["entries"][1]["visual_key"] = raw["entries"][0]["visual_key"]
        with self.assertRaisesRegex(finalizer.FinalizeError, "ordered by exact compiled visual key"):
            finalizer.validate_entries(raw, self.setup_rows, ROOT)

    def test_extra_readmodel_field_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["entries"][0]["read_model"]["SceneReady"] = True
        with self.assertRaisesRegex(finalizer.FinalizeError, "six-field readmodel shape/order"):
            finalizer.validate_entries(raw, self.setup_rows, ROOT)

    def test_readiness_claim_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["native_visual_readiness"] = True
        with self.assertRaisesRegex(finalizer.FinalizeError, "claims scene, mission spawn, or readiness"):
            finalizer.validate_entries(raw, self.setup_rows, ROOT)


if __name__ == "__main__":
    unittest.main()
