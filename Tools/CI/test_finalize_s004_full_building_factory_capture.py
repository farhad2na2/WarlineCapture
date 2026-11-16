#!/usr/bin/env python3
"""Negative controls for the offline S004 full factory receipt finalizer."""
from __future__ import annotations

import copy
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CAPTURE = ROOT / "Logs/SkirmishProduction/s004-full-building-factory-capture-20261103-r1"
MODULE_PATH = Path(__file__).with_name("finalize_s004_full_building_factory_capture.py")
spec = importlib.util.spec_from_file_location("s004_full_finalizer", MODULE_PATH)
finalizer = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(finalizer)


class FullFactoryFinalizerNegativeControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.raw = json.loads((CAPTURE / "data/building-definitions.json").read_text())
        cls.log = CAPTURE / "editor.log"

    def reject_raw(self, mutate, message):
        raw = copy.deepcopy(self.raw)
        mutate(raw)
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "raw.json"
            path.write_text(json.dumps(raw))
            with self.assertRaisesRegex(finalizer.FinalizeError, message):
                finalizer.verify(self.log, path, ROOT, 0)

    def test_nonzero_wrapper_exit_rejected(self):
        with self.assertRaisesRegex(finalizer.FinalizeError, "exit must be exactly zero"):
            finalizer.verify(self.log, CAPTURE / "data/building-definitions.json", ROOT, 1)

    def test_setup_hash_mismatch_rejected(self):
        self.reject_raw(lambda raw: raw.__setitem__("setup_hash", 1), "identity/hash mismatch")

    def test_missing_visual_key_rejected(self):
        self.reject_raw(lambda raw: raw["definitions"].pop(), "exact nine derived S004 keys")

    def test_field_set_mismatch_rejected(self):
        self.reject_raw(lambda raw: raw["definitions"][0]["fields"].pop("HasRunway"), "exactly 55 fields")

    def test_nonpositive_runtime_footprint_rejected(self):
        def mutate(raw):
            raw["definitions"][0]["fields"]["FootprintCells"]["fields"]["UnityEngine.Vector2Int::m_X"]["value"] = "0"
        self.reject_raw(mutate, "non-positive")

    def test_prefab_source_identity_mismatch_rejected(self):
        self.reject_raw(lambda raw: raw["definitions"][0]["prefab"].__setitem__("guid", "0" * 32),
                        "prefab GUID differs from current meta")

    def test_readiness_claim_rejected(self):
        self.reject_raw(lambda raw: raw.__setitem__("native_visual_readiness", True),
                        "overclaims mission/visual readiness")


if __name__ == "__main__":
    unittest.main()
