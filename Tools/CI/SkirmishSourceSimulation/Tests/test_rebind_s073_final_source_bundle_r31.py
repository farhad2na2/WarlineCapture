"""Fail-closed controls for the S073 final source bundle rebind."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).with_name("rebind_s073_final_source_bundle_r31.py")
spec = importlib.util.spec_from_file_location("rebind_s073_final_source_bundle_r31", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


class S073FinalSourceBundleRebindTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.files = {}
        for index, key in enumerate(sorted(module.REPLACED) + [f"retained_{i:02}" for i in range(15)]):
            path = self.root / f"evidence-{index:02}.json"
            path.write_text(json.dumps({"key": key, "index": index}) + "\n")
            self.files[key] = {"path": str(path), "sha256": sha(path)}
        self.base = {
            "schema_version": 1, "kind": "S073LegacyStartupBundle", "mission": "S073", "seed": 104729,
            "source_repository_root": str(module.ROOT.resolve()), "asset_repository_root": str(module.ROOT.resolve()),
            **self.files,
        }
        self.base_path = self.root / "data-bundle-r30.json"
        self.base_path.write_text(json.dumps(self.base, sort_keys=True) + "\n")
        self.receipt_path = self.root / "r30-receipt.json"
        self.receipt_path.write_text(json.dumps({
            "kind": "S073NativeWaterBlockerBundleRebindReceipt", "status": "Passed",
            "output_bundle": {"path": str(self.base_path.resolve()), "sha256": sha(self.base_path)},
            "historical_binary_limitation": copy.deepcopy(module.EXPECTED_HISTORY),
        }) + "\n")

    def tearDown(self):
        self.temp.cleanup()

    def test_r30_lineage_and_seven_binary_caveat_are_required(self):
        with patch.object(module, "BASE", self.base_path):
            with patch.object(module, "BASE_RECEIPT", self.receipt_path):
                descriptors = module.validate_bundle(self.base, json.loads(self.receipt_path.read_text()))
        self.assertEqual(len(descriptors), 30)
        bad = json.loads(self.receipt_path.read_text())
        bad["historical_binary_limitation"]["unresolved_count"] = 6
        with self.assertRaisesRegex(ValueError, "seven unresolved historical DLLs"):
            module.validate_bundle(self.base, bad)

    def test_replacements_preserve_every_unrelated_descriptor(self):
        replacements = {name: {"path": f"/new/{name}", "sha256": "a" * 64} for name in module.REPLACED}
        rebound = module.apply_replacements(self.base, replacements)
        self.assertEqual(set(replacements), module.REPLACED)
        self.assertTrue(all(rebound[name] == self.base[name] for name in self.base if name not in module.REPLACED))
        self.assertTrue(all(rebound[name] == replacements[name] for name in module.REPLACED))
        self.assertEqual(self.base, json.loads(self.base_path.read_text()))

    def test_incomplete_or_extra_replacement_is_rejected(self):
        replacements = {name: {"path": f"/new/{name}", "sha256": "a" * 64} for name in module.REPLACED}
        missing = dict(replacements)
        missing.pop(sorted(missing)[0])
        with self.assertRaisesRegex(ValueError, "replacement descriptor set mismatch"):
            module.apply_replacements(self.base, missing)
        extra = dict(replacements, unrelated={"path": "/new/unrelated", "sha256": "b" * 64})
        with self.assertRaisesRegex(ValueError, "replacement descriptor set mismatch"):
            module.apply_replacements(self.base, extra)

    def test_current_definition_adaptation_preserves_nonreadiness(self):
        keys = module.EXPECTED_BUILDING_KEYS
        native = {
            "definitions": [{"key": key, "fields": {"MaxHealth": {"value": "1"}}} for key in keys],
            "building_order": [{"key": key} for key in keys],
            "mission_world_loaded": False, "mission_initialized": False, "native_readiness": False,
            "unsupported_values": [], "source_pins": {"x": "y"},
        }
        operation_map = self.root / "map.asset"
        operation_map.write_text(
            "operationMapId: opmap.skirmish.industrial_basin\n"
            "sourceIdentityHash: 2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900\n")
        adapted = module.adapt_definitions(native, operation_map)
        self.assertEqual(adapted["definitions"], native["definitions"])
        self.assertEqual(adapted["requested_keys"], keys)
        self.assertEqual(adapted["selected_source"]["operation_map_id"], "opmap.skirmish.industrial_basin")
        self.assertIs(adapted["mission_world_allocated"], False)
        self.assertIs(adapted["full_mission_initialized"], False)
        self.assertIs(adapted["native_visual_readiness"], False)


if __name__ == "__main__":
    unittest.main()
