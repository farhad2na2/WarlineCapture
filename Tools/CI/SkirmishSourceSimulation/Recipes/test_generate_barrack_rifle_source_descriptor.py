import importlib.util
import shutil
import tempfile
import unittest
from unittest import mock
from pathlib import Path

MODULE_PATH = Path(__file__).with_name("generate_barrack_rifle_source_descriptor.py")
SPEC = importlib.util.spec_from_file_location("barrack_rifle_descriptor", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
REPO = MODULE.ROOT


class BarrackRifleSourceDescriptorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="barrack-rifle-source-")
        self.root = Path(self.temp.name)
        rels = [MODULE.BARRACK_CONFIG, MODULE.BARRACK_CONFIG.with_suffix(MODULE.BARRACK_CONFIG.suffix + ".meta"),
                MODULE.BARRACK_PREFAB, MODULE.BARRACK_PREFAB.with_suffix(MODULE.BARRACK_PREFAB.suffix + ".meta"),
                MODULE.RIFLE_CONFIG, MODULE.RIFLE_CONFIG.with_suffix(MODULE.RIFLE_CONFIG.suffix + ".meta"),
                MODULE.RIFLE_PREFAB, MODULE.RIFLE_PREFAB.with_suffix(MODULE.RIFLE_PREFAB.suffix + ".meta"),
                MODULE.TRANSPORT_CONFIG, MODULE.TRANSPORT_CONFIG.with_suffix(MODULE.TRANSPORT_CONFIG.suffix + ".meta"),
                *MODULE.SOURCE_FILES,
                Path("Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"),
                Path("Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab.meta"),
                MODULE.GENERIC_CONSTRUCTION, MODULE.GENERIC_CONSTRUCTION.with_suffix(MODULE.GENERIC_CONSTRUCTION.suffix + ".meta"),
                MODULE.GENERIC_INITIAL_FORCES, MODULE.GENERIC_INITIAL_FORCES.with_suffix(MODULE.GENERIC_INITIAL_FORCES.suffix + ".meta"),
                MODULE.GENERIC_PRESET, *MODULE.EXPANSION_SCENARIOS]
        for rel in rels:
            target = self.root / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(REPO / rel, target)

    def tearDown(self):
        self.temp.cleanup()

    def text(self, rel):
        return (self.root / rel).read_text(encoding="utf-8")

    def write(self, rel, value):
        (self.root / rel).write_text(value, encoding="utf-8")

    def test_current_static_facts_are_derived_and_scope_is_explicit(self):
        result = MODULE.build_descriptor(self.root)
        facts = result["facts"]
        self.assertEqual(facts["producer"]["footprint_cells"], [28, 15])
        self.assertEqual(facts["producer"]["credits_cost"], 40000)
        self.assertEqual(facts["producer"]["materials_cost"], 90)
        self.assertEqual(facts["producer"]["construction_duration_seconds"], 30.0)
        self.assertEqual(facts["producer"]["recipes"][0]["unit_key"], MODULE.RIFLE_KEY)
        self.assertEqual(facts["producer"]["recipes"][0]["quantity"], 4)
        self.assertEqual(facts["unit"]["production_duration_seconds"], 5.0)
        self.assertEqual(facts["unit"]["footprint_cells"], [1, 1])
        self.assertFalse(result["scope"]["native_context_qualified"])
        self.assertFalse(result["scope"]["mission_ready"])
        self.assertIn(str(MODULE.PARSER), result["source_pins"])
        self.assertEqual(result["source_pins"][str(MODULE.PARSER)], MODULE.PARSER_SHA256)
        self.assertIsNone(facts["producer"]["prefab"]["local_id"])
        witness = result["generic_authored_identity_witness"]
        self.assertEqual(witness["barrack_pair"], {
            "guid": "683195b1e21c94df7add42ab26cd2a33",
            "local_id": 6603657636351658259,
            "basis": "exact authored YAML reference pair; no Unity importer resolution claimed"})
        self.assertEqual([x["exact_reference_count"] for x in witness["configs"]], [1, 2])
        self.assertEqual(witness["scope"], "generic SkirmishBaseAssault template only")
        self.assertFalse(witness["unity_resolution"])
        self.assertEqual(witness["expansion_membership"]["S002"], "not established; direct entry references absent")
        self.assertEqual(witness["expansion_membership"]["S003"], "not established; direct entry references absent")
        self.assertFalse(result["scope"]["generic_witness_establishes_s002_s003_membership"])

    def test_generic_authored_identity_reference_count_is_exact(self):
        rel = MODULE.GENERIC_INITIAL_FORCES
        source = self.text(rel)
        needle = "{fileID: 6603657636351658259, guid: 683195b1e21c94df7add42ab26cd2a33, type: 3}"
        self.assertEqual(source.count(needle), 2)
        self.write(rel, source.replace(needle, "{fileID: 6603657636351658259, guid: " + "0" * 32 + ", type: 3}", 1))
        with self.assertRaisesRegex(ValueError, "exact Barrack GUID/localID refs"):
            MODULE.build_descriptor(self.root)

    def test_generic_config_chain_mismatch_rejected(self):
        rel = MODULE.GENERIC_CONSTRUCTION
        source = self.text(rel)
        self.write(rel, source.replace("guid: 4a951f29e509445c9ad4142e099a4018", "guid: " + "0" * 32, 1))
        with self.assertRaisesRegex(ValueError, "does not point to pinned InitialForces"):
            MODULE.build_descriptor(self.root)

    def test_s002_s003_direct_generic_membership_tampering_rejected(self):
        rel = MODULE.EXPANSION_SCENARIOS[0]
        self.write(rel, self.text(rel) + "\n# {fileID: 6603657636351658259, guid: 683195b1e21c94df7add42ab26cd2a33, type: 3}\n")
        with self.assertRaisesRegex(ValueError, "directly contains generic Barrack reference"):
            MODULE.build_descriptor(self.root)

    def test_duplicate_authored_scalar_rejected(self):
        rel = MODULE.BARRACK_CONFIG
        self.write(rel, self.text(rel) + "  maxHealth: 1200\n")
        with self.assertRaisesRegex(ValueError, "exactly one authored maxHealth"):
            MODULE.build_descriptor(self.root)

    def test_multiple_recipe_rows_rejected(self):
        rel = MODULE.BARRACK_CONFIG
        source = self.text(rel)
        row = "  - spawnUnitPrefab: {fileID: 3282182154612916099, guid: ff73c8b7cbf844778916da330e9cd3e3, type: 3}\n    quantity: 4"
        self.write(rel, source.replace(row, row + "\n" + row))
        with self.assertRaisesRegex(ValueError, "exactly one unambiguous authored production row"):
            MODULE.build_descriptor(self.root)

    def test_recipe_guid_ambiguity_rejected(self):
        duplicate = self.root / "Assets/Negative/Duplicate.prefab.meta"
        duplicate.parent.mkdir(parents=True)
        duplicate.write_text("fileFormatVersion: 2\nguid: ff73c8b7cbf844778916da330e9cd3e3\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "exactly one asset meta"):
            MODULE.build_descriptor(self.root)

    def test_recipe_identity_mismatch_rejected(self):
        rel = MODULE.BARRACK_CONFIG
        self.write(rel, self.text(rel).replace("guid: ff73c8b7cbf844778916da330e9cd3e3", "guid: " + "0" * 32))
        with self.assertRaisesRegex(ValueError, "exactly one asset meta"):
            MODULE.build_descriptor(self.root)

    def test_missing_meta_rejected(self):
        (self.root / MODULE.RIFLE_PREFAB.with_suffix(MODULE.RIFLE_PREFAB.suffix + ".meta")).unlink()
        with self.assertRaisesRegex((OSError, ValueError), "missing|meta|No such file"):
            MODULE.build_descriptor(self.root)


    def test_equal_construction_and_unit_duration_is_valid(self):
        rel = MODULE.RIFLE_CONFIG
        source = self.text(rel)
        self.assertEqual(source.count("productionDurationSeconds: 5"), 1)
        self.write(rel, source.replace("productionDurationSeconds: 5", "productionDurationSeconds: 30"))
        result = MODULE.build_descriptor(self.root)
        self.assertEqual(result["facts"]["producer"]["construction_duration_seconds"], 30.0)
        self.assertEqual(result["facts"]["unit"]["production_duration_seconds"], 30.0)

    def test_repository_parser_drift_rejected(self):
        parser = self.root / MODULE.PARSER
        parser.write_text(parser.read_text(encoding="utf-8") + "\n# changed parser\n", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "parser source pin drift"):
            MODULE.build_descriptor(self.root)

    def test_reused_exact_scalar_parsers_accept_and_reject_focused_values(self):
        parser = MODULE._UNIT_MOVE_PARSER
        self.assertEqual(parser.one_float("productionDurationSeconds: 5.25e1\n", "productionDurationSeconds"), 52.5)
        self.assertTrue(parser.one_bool("isAirUnit: 1\n", "isAirUnit"))
        self.assertFalse(parser.one_bool("isAirUnit: 0\n", "isAirUnit"))
        self.assertEqual(parser.one_int("footprintCells: {x: 2, y: 3}\n", "footprintCells", "y"), 3)
        with self.assertRaisesRegex(ValueError, "expected one exact authored float field"):
            parser.one_float("productionDurationSeconds: 5\nproductionDurationSeconds: 6\n", "productionDurationSeconds")
        with self.assertRaisesRegex(ValueError, "expected one exact authored boolean field"):
            parser.one_bool("isAirUnit: true\n", "isAirUnit")
        with self.assertRaisesRegex(ValueError, "expected one exact authored integer field"):
            parser.one_int("footprintCells: {x: 2, y: 3}\nfootprintCells: {x: 2, y: 4}\n", "footprintCells", "y")

    def test_symlinked_ancestor_rejected(self):
        parent = self.root / "Assets/Game/Configs/Prefabs"
        moved = self.root / "Assets/Game/Configs/Prefabs-real"
        parent.rename(moved)
        parent.symlink_to(moved, target_is_directory=True)
        with self.assertRaisesRegex(ValueError, "symlink/reparse source path component"):
            MODULE.build_descriptor(self.root)

    def test_guid_lookup_epoch_change_leaves_no_descriptor_or_stage(self):
        output = self.root / "out/descriptor.json"
        original = MODULE.build_descriptor

        def mutate_after_build(root):
            result = original(root)
            duplicate = root / "Assets/Negative/Duplicate.prefab.meta"
            duplicate.parent.mkdir(parents=True)
            duplicate.write_text("fileFormatVersion: 2\nguid: ff73c8b7cbf844778916da330e9cd3e3\n", encoding="utf-8")
            return result

        with mock.patch.object(MODULE, "build_descriptor", side_effect=mutate_after_build):
            with self.assertRaisesRegex(ValueError, "GUID lookup epoch changed|exactly one asset meta"):
                MODULE.write_descriptor(self.root, output)
        self.assertFalse(output.exists())
        self.assertEqual(list(output.parent.glob("descriptor.json.stage-*")), [])

    def test_symlink_source_rejected(self):
        rel = MODULE.RIFLE_CONFIG
        original = self.root / rel
        replacement = original.with_suffix(".copy")
        replacement.write_bytes(original.read_bytes())
        original.unlink()
        original.symlink_to(replacement)
        with self.assertRaisesRegex(ValueError, "symlink/reparse source path component"):
            MODULE.build_descriptor(self.root)


if __name__ == "__main__":
    unittest.main()
