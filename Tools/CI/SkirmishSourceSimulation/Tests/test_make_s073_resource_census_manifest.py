import copy
import importlib.util
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[2] / "make_s073_resource_census_manifest.py"
SPEC = importlib.util.spec_from_file_location("make_s073_resource_census_manifest", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class ResourceCensusManifestTests(unittest.TestCase):
    def test_derives_only_the_s073_census_scope(self):
        base = MODULE.BASE
        original = __import__("json").loads(base.read_text())
        before = copy.deepcopy(original)
        derived = MODULE.derive(original)

        self.assertEqual(original, before)
        self.assertEqual(derived["sources"][:-1], before["sources"])
        self.assertEqual(derived["compiler_defines"][:-1], before["compiler_defines"])
        self.assertEqual(derived["compiler_defines"][-1], MODULE.DEFINE)
        self.assertEqual(derived["sources"][-1]["kind"], "finite_adapter")
        self.assertEqual(derived["sources"][-1]["role"], "host_glue")
        self.assertIs(derived["sources"][-1]["contains_original_gameplay"], False)
        self.assertEqual(derived["sources"][-1]["path"], MODULE.HELPER)
        self.assertNotIn(MODULE.DEFINE, before["compiler_defines"])
        self.assertNotIn(MODULE.HELPER, [row.get("path") for row in before["sources"]])

    def test_diagnostic_wiring_is_gated_by_either_opt_in_build_profile(self):
        source_path = MODULE.ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs"
        source = source_path.read_text()
        gate = "#if WARLINE_SOURCE_S073_AI_EXPERIMENTAL || WARLINE_SOURCE_S073_RESOURCE_CENSUS"
        gated_blocks = [block.split("#endif", 1)[0] for block in source.split(gate)[1:]]
        for marker in ('"diagnostic_startup_before_recurring_ticks"',
                       '"post_recurring_ticks"',
                       '["source_host_resource_economy_runtime_census"]'):
            self.assertTrue(any(marker in block for block in gated_blocks), marker)

    def test_census_uses_observed_source_world_availability_without_invented_identity(self):
        helper = (MODULE.ROOT / MODULE.HELPER).read_text()
        self.assertIn("source.World.IsCreated", helper)
        self.assertIn('"source_world_available"', helper)
        self.assertNotIn("source.IsCreated", helper)
        self.assertNotIn("source.World.Name", helper)
        self.assertIn("using Game.Configs;", helper)

    def test_rejects_non_base_or_already_extended_manifest(self):
        with self.assertRaises(ValueError):
            MODULE.derive({"profile": {"mission": "S073"}, "sources": []})
        base = __import__("json").loads(MODULE.BASE.read_text())
        base["compiler_defines"].append(MODULE.DEFINE)
        with self.assertRaises(ValueError):
            MODULE.derive(base)


if __name__ == "__main__":
    unittest.main()
