import hashlib
import importlib.util
import shutil
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).parent

def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

GEN = load_module("barrack_generator_for_validator_tests", HERE / "generate_barrack_rifle_source_descriptor.py")
VALIDATOR = load_module("barrack_validator_tests", HERE / "validate_barrack_rifle_source_descriptor.py")
REPO = GEN.ROOT


class BarrackRifleSourceHostPreflightTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="barrack-rifle-host-preflight-")
        self.root = Path(self.temp.name).resolve()
        rels = [GEN.BARRACK_CONFIG, GEN.BARRACK_CONFIG.with_suffix(GEN.BARRACK_CONFIG.suffix + ".meta"),
                GEN.BARRACK_PREFAB, GEN.BARRACK_PREFAB.with_suffix(GEN.BARRACK_PREFAB.suffix + ".meta"),
                GEN.RIFLE_CONFIG, GEN.RIFLE_CONFIG.with_suffix(GEN.RIFLE_CONFIG.suffix + ".meta"),
                GEN.RIFLE_PREFAB, GEN.RIFLE_PREFAB.with_suffix(GEN.RIFLE_PREFAB.suffix + ".meta"),
                Path("Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"),
                Path("Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab.meta"),
                GEN.GENERIC_CONSTRUCTION, GEN.GENERIC_CONSTRUCTION.with_suffix(GEN.GENERIC_CONSTRUCTION.suffix + ".meta"),
                GEN.GENERIC_INITIAL_FORCES, GEN.GENERIC_INITIAL_FORCES.with_suffix(GEN.GENERIC_INITIAL_FORCES.suffix + ".meta"),
                GEN.GENERIC_PRESET, *GEN.EXPANSION_SCENARIOS, *GEN.SOURCE_FILES]
        for rel in rels:
            target = self.root / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO / rel, target)
        self.descriptor_path = self.root / "package/descriptor.json"
        self.descriptor_path.parent.mkdir(parents=True)
        GEN.write_descriptor(self.root, self.descriptor_path)
        self.descriptor_sha = hashlib.sha256(self.descriptor_path.read_bytes()).hexdigest()

    def tearDown(self):
        self.temp.cleanup()

    def load_doc(self):
        import json
        return json.loads(self.descriptor_path.read_text(encoding="utf-8"))

    def save_doc(self, doc):
        import json
        self.descriptor_path.write_text(json.dumps(doc, indent=2) + "\n", encoding="utf-8")
        self.descriptor_sha = hashlib.sha256(self.descriptor_path.read_bytes()).hexdigest()

    def test_current_source_preflight_passes_but_core_projection_blocks(self):
        receipt = VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)
        self.assertEqual(receipt["status"], "PassedAuthoredSourcePreflight_CoreProjectionBlocked")
        self.assertEqual(receipt["recipe_count"], 1)
        core = receipt["producer_core_projection"]
        self.assertFalse(core["projected"])
        self.assertFalse(core["authored_footprint_used_as_factory_authority"])
        self.assertIn("Barrack prefab local ID from current factory/source identity", core["missing_facts"])
        self.assertIn("current factory-derived normalized producer footprint authority", core["missing_facts"])
        self.assertFalse(receipt["scope"]["native_factory_invoked"])

    def test_wrong_descriptor_hash_rejected(self):
        with self.assertRaisesRegex(ValueError, "descriptor SHA-256 mismatch"):
            VALIDATOR.validate_descriptor(self.root, self.descriptor_path, "0" * 64)

    def test_json_value_tampering_rejected_even_when_file_hash_is_updated(self):
        doc = self.load_doc()
        doc["facts"]["producer"]["footprint_cells"] = [99, 99]
        self.save_doc(doc)
        with self.assertRaisesRegex(ValueError, "differ from current source"):
            VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)

    def test_source_pin_and_authored_fact_drift_rejected(self):
        config = self.root / GEN.BARRACK_CONFIG
        config.write_text(config.read_text(encoding="utf-8").replace("price: 40000", "price: 40001"), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "differ from current source"):
            VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)

    def test_duplicate_source_identity_rejected(self):
        extra = self.root / "Assets/Other/Duplicate.meta"
        extra.parent.mkdir(parents=True)
        extra.write_text("fileFormatVersion: 2\nguid: ff73c8b7cbf844778916da330e9cd3e3\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "exactly one asset meta"):
            VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)

    def test_injected_factory_authority_claim_rejected(self):
        doc = self.load_doc()
        doc["factory_footprint_authority"] = {"footprint_cells": [28, 15], "kind": "NativeFactory"}
        self.save_doc(doc)
        with self.assertRaisesRegex(ValueError, "differ from current source"):
            VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)

    def test_tampered_generic_reference_witness_rejected_even_with_new_descriptor_hash(self):
        doc = self.load_doc()
        doc["generic_authored_identity_witness"]["barrack_pair"]["local_id"] += 1
        self.save_doc(doc)
        with self.assertRaisesRegex(ValueError, "differ from current source"):
            VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)

    def test_generic_authored_witness_does_not_unblock_factory_authority(self):
        receipt = VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)
        self.assertEqual(receipt["status"], "PassedAuthoredSourcePreflight_CoreProjectionBlocked")
        self.assertFalse(receipt["producer_core_projection"]["projected"])
        self.assertFalse(receipt["scope"]["native_factory_invoked"])

    def test_symlinked_descriptor_ancestor_rejected(self):
        real_dir = self.root / "package"
        moved = self.root / "package-real"
        real_dir.rename(moved)
        real_dir.symlink_to(moved, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, "symlink/reparse path component"):
            VALIDATOR.validate_descriptor(self.root, self.descriptor_path, self.descriptor_sha)


if __name__ == "__main__":
    unittest.main()
