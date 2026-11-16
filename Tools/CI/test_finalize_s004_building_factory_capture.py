#!/usr/bin/env python3
"""Negative controls for the S004 original building-factory capture finalizer."""
from __future__ import annotations

import copy
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("finalize_s004_building_factory_capture.py")
SPEC = importlib.util.spec_from_file_location("s004_building_factory_finalizer", SCRIPT)
finalizer = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(finalizer)


class S004BuildingFactoryFinalizerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = SCRIPT.resolve().parents[2]
        cls.raw_path = cls.root / finalizer.RAW_DATA
        cls.log_path = cls.root / finalizer.CAPTURE_LOG
        cls.evidence_path = cls.root / finalizer.SETUP_EVIDENCE
        cls.raw = json.loads(cls.raw_path.read_text())
        cls.log = cls.log_path.read_text()

    def verify_mutation(self, mutate, wrapper_exit=0, log=None):
        candidate = copy.deepcopy(self.raw)
        mutate(candidate)
        with tempfile.TemporaryDirectory() as directory:
            temp = Path(directory)
            raw_path = temp / "building-definitions.json"
            log_path = temp / "editor.log"
            raw_path.write_text(json.dumps(candidate))
            log_path.write_text(self.log if log is None else log)
            return finalizer.verify(log_path, raw_path, self.evidence_path, self.root, wrapper_exit)

    def assert_rejected(self, mutate, wrapper_exit=0, log=None):
        with self.assertRaises(finalizer.FinalizeError):
            self.verify_mutation(mutate, wrapper_exit, log)

    def test_observed_capture_passes_and_keeps_scope_limits(self):
        receipt = finalizer.verify(self.log_path, self.raw_path, self.evidence_path, self.root, 0)
        self.assertEqual(receipt["structureEntryCount"], 20)
        self.assertEqual(receipt["distinctStructureIdCount"], 10)
        self.assertEqual(receipt["distinctVisualKeyCount"], 9)
        self.assertEqual(receipt["sourcePinCount"], len(self.raw["source_pins"]))
        self.assertEqual(receipt["runwayData"], "NotCaptured_RunwaySystemNotSupplied")
        self.assertFalse(receipt["missionReadiness"])

    def test_nonzero_wrapper_exit_rejected(self):
        self.assert_rejected(lambda _data: None, wrapper_exit=1)

    def test_missing_or_duplicate_original_marker_rejected(self):
        self.assert_rejected(lambda _data: None, log=self.log.replace(finalizer.MARKER, "", 1))
        self.assert_rejected(lambda _data: None, log=self.log + "\n" + finalizer.MARKER + "\n")

    def test_compiled_setup_hash_mismatch_rejected(self):
        self.assert_rejected(lambda data: data["setup_identity"].update(setup_hash=17))

    def test_structure_rows_cannot_be_reordered_or_rewritten_without_setup_match(self):
        self.assert_rejected(lambda data: data["compiled_structure_entries"][0].update(structure_id="building.unknown"))

    def test_wrong_visual_key_mapping_rejected(self):
        self.assert_rejected(lambda data: data["compiled_structure_entries"][0].update(visual_key="Building_GuardTower"))

    def test_nonpositive_runtime_footprint_rejected(self):
        self.assert_rejected(lambda data: data["factory_definitions"][0]["runtime_footprint_cells"].update(x=0))

    def test_source_pin_mutation_rejected(self):
        self.assert_rejected(lambda data: data["source_pins"].update({
            "Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs": "0" * 64}))

    def test_runway_or_readiness_overclaim_rejected(self):
        self.assert_rejected(lambda data: data.update(runway_derived_fields_captured=True))
        self.assert_rejected(lambda data: data.update(mission_readiness=True))


if __name__ == "__main__":
    unittest.main()
