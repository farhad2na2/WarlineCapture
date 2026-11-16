import copy
import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


SCRIPT = (Path(__file__).resolve().parents[1] / "Recipes" /
          "generate_s003_focused_prefab_key_descriptor.py")
SPEC = importlib.util.spec_from_file_location("s003_prefab_key_descriptor", SCRIPT)
GENERATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(GENERATOR)


def digest(data):
    return hashlib.sha256(data).hexdigest()


class S003PrefabKeyDescriptorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.repo = Path(self.temp.name)
        self.capture = {
            "context": {
                "catalog_id": "S003", "size": "Standard", "difficulty": "Regular",
                "seed": 104732, "setup_hash": 2394754492,
                "wrong_context_hash_rejected": True, "wrong_target_seed_rejected": True,
                "metadata": {"catalog_inputs": {"catalog_id": "S003"}, "units": []},
            },
            "source_pins": {},
        }
        for index in range(19):
            key = "S003_Unit_%02d" % index
            guid = "%032x" % (index + 1)
            asset_path = "Assets/Game/Prefabs/Units/%s.prefab" % key
            asset = self.repo / asset_path
            asset.parent.mkdir(parents=True, exist_ok=True)
            asset.write_bytes(("prefab-%d" % index).encode())
            meta = Path(str(asset) + ".meta")
            meta.write_text("fileFormatVersion: 2\nguid: %s\n" % guid, encoding="ascii")
            self.capture["source_pins"][asset_path] = digest(asset.read_bytes())
            self.capture["source_pins"][asset_path + ".meta"] = digest(meta.read_bytes())
            self.capture["context"]["metadata"]["units"].append({
                "index": index, "has_definition_metadata": True, "has_production_metadata": True,
                "prefab": {"codec": "source-asset-reference-v1", "type": "UnityEngine.GameObject",
                           "key": key, "guid": guid, "local_id": index + 100,
                           "path": asset_path},
            })

    def tearDown(self):
        self.temp.cleanup()

    def test_derives_ordered_keys_and_asset_identities(self):
        keys, identities = GENERATOR.derive_keys(self.repo, self.capture)
        self.assertEqual(["S003_Unit_%02d" % i for i in range(19)], keys)
        self.assertEqual(19, len(identities))
        self.assertEqual(keys, [row["key"] for row in identities])

    def test_duplicate_casefolded_key_is_rejected(self):
        self.capture["context"]["metadata"]["units"][1]["prefab"]["key"] = "s003_unit_00"
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            GENERATOR.derive_keys(self.repo, self.capture)

    def test_wrong_seed_and_setup_hash_are_rejected(self):
        for field, value in (("seed", 104733), ("setup_hash", 0)):
            changed = copy.deepcopy(self.capture)
            changed["context"][field] = value
            with self.subTest(field=field), self.assertRaisesRegex(ValueError, "context"):
                GENERATOR.derive_keys(self.repo, changed)

    def test_changed_prefab_pin_is_rejected(self):
        changed = copy.deepcopy(self.capture)
        path = changed["context"]["metadata"]["units"][0]["prefab"]["path"]
        changed["source_pins"][path] = "0" * 64
        with self.assertRaisesRegex(ValueError, "pin changed"):
            GENERATOR.derive_keys(self.repo, changed)

    def test_meta_guid_drift_is_rejected(self):
        changed = copy.deepcopy(self.capture)
        row = changed["context"]["metadata"]["units"][0]["prefab"]
        meta = self.repo / (row["path"] + ".meta")
        meta.write_text("fileFormatVersion: 2\nguid: %032x\n" % 999, encoding="ascii")
        changed["source_pins"][row["path"] + ".meta"] = digest(meta.read_bytes())
        with self.assertRaisesRegex(ValueError, "GUID differs"):
            GENERATOR.derive_keys(self.repo, changed)

    def test_missing_metadata_row_or_bad_index_is_rejected(self):
        changed = copy.deepcopy(self.capture)
        changed["context"]["metadata"]["units"][7]["index"] = 8
        with self.assertRaisesRegex(ValueError, "index"):
            GENERATOR.derive_keys(self.repo, changed)
        changed = copy.deepcopy(self.capture)
        changed["context"]["metadata"]["units"][7]["has_production_metadata"] = False
        with self.assertRaisesRegex(ValueError, "index"):
            GENERATOR.derive_keys(self.repo, changed)

    def test_path_traversal_is_rejected(self):
        changed = copy.deepcopy(self.capture)
        changed["context"]["metadata"]["units"][0]["prefab"]["path"] = "Assets/Game/Prefabs/../escape.prefab"
        with self.assertRaisesRegex(ValueError, "path"):
            GENERATOR.derive_keys(self.repo, changed)


if __name__ == "__main__":
    unittest.main()
