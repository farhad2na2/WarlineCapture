#!/usr/bin/env python3
import copy
import importlib.util
from pathlib import Path
import unittest


SCRIPT = Path(__file__).with_name("bridge_s073_source_clearance_identity.py")
SPEC = importlib.util.spec_from_file_location("bridge_s073_clearance", SCRIPT)
BRIDGE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(BRIDGE)
REPO = SCRIPT.resolve().parents[4]


class S073ClearanceIdentityBridgeTests(unittest.TestCase):
    def test_current_physical_capture_and_s073_binding_join(self):
        receipt = BRIDGE.build_receipt(REPO)
        self.assertEqual(receipt["status"], "IdentityBound")
        self.assertEqual(receipt["source_geometry"]["bounds_count"], 90865)
        self.assertEqual(receipt["s073_source_binding"]["source_map_id"], BRIDGE.SOURCE_MAP_ID)
        self.assertFalse(receipt["provider_parity_established"])
        self.assertFalse(receipt["historical_capture_dependency_pins_revalidated"])
        self.assertFalse(receipt["source_capture_current_producer_equivalence"])
        self.assertFalse(receipt["capture_relocation_path_remapping_validated"])
        self.assertFalse(receipt["delivery_established"])
        self.assertFalse(receipt["mission_ready"])

    def test_changed_bounds_cardinality_rejected(self):
        physical = BRIDGE.read_json(REPO / BRIDGE.PHYSICAL_CAPTURE, 160 * 1024 * 1024)
        record = copy.deepcopy(physical["fixedMapReadDomain"]["dropClearanceBounds"])
        record["bounds"].pop()
        with self.assertRaisesRegex(ValueError, "Complete original source-frame"):
            BRIDGE.validate_clearance_record(record)

    def test_wrong_source_binding_rejected(self):
        selected = (REPO / BRIDGE.S073_MAP).read_text(encoding="utf-8").replace(
            BRIDGE.SOURCE_MAP_ID, "opmap.skirmish.other_map")
        runtime = (REPO / BRIDGE.RUNTIME_SCENE).read_text(encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "no longer binds"):
            BRIDGE.validate_s073_map_binding(selected, BRIDGE.S073_MAP_GUID,
                                             BRIDGE.RUNTIME_SCENE_GUID, runtime)

    def test_geometric_relocation_is_rejected_and_distinguished_from_path_remapping(self):
        with self.assertRaisesRegex(ValueError, "Geometric relocation is rejected"):
            BRIDGE.build_receipt(REPO, geometry_relocation=Path("/private/tmp/translation.json"))
        receipt = BRIDGE.build_receipt(REPO)
        self.assertIn("unrelated to capture path remapping", " ".join(receipt["limits"]))

    def test_runtime_scene_without_exact_source_scene_reference_rejected(self):
        selected = (REPO / BRIDGE.S073_MAP).read_text(encoding="utf-8")
        runtime = (REPO / BRIDGE.RUNTIME_SCENE).read_text(encoding="utf-8").replace(
            BRIDGE.SCENE_GUID, "0" * 32)
        with self.assertRaisesRegex(ValueError, "no longer embeds"):
            BRIDGE.validate_s073_map_binding(selected, BRIDGE.S073_MAP_GUID,
                                             BRIDGE.RUNTIME_SCENE_GUID, runtime)


if __name__ == "__main__":
    unittest.main()
