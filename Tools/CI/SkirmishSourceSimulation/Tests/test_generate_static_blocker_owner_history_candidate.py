import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / "Recipes/static_blocker_owner_history/generate.py"
SPEC = importlib.util.spec_from_file_location("static_blocker_candidate", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
REPO = SCRIPT.resolve().parents[5]


class CandidateGeneratorTests(unittest.TestCase):
    def test_exact_pinned_transform_changes_only_preflight_scan_region(self):
        source_path = REPO / MODULE.SOURCE
        before = source_path.read_bytes()
        with tempfile.TemporaryDirectory() as temp:
            output = Path(temp) / "candidate"
            proof = MODULE.generate(REPO, output)
            candidate = (output / proof["output"]).read_bytes()
            self.assertEqual(before.count(MODULE.BEGIN.encode()), 1)
            self.assertEqual(candidate, before.replace(MODULE.BEGIN.encode(), MODULE.CANDIDATE.encode(), 1))
            self.assertEqual(source_path.read_bytes(), before)
            self.assertFalse(proof["compiled_or_executed"])

    def test_stale_source_pin_fails_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            copy = Path(temp) / "repo"
            (copy / MODULE.SOURCE).parent.mkdir(parents=True)
            (copy / MODULE.SOURCE).write_text("changed source")
            with self.assertRaisesRegex(ValueError, "hash mismatch"):
                MODULE.generate(copy, Path(temp) / "candidate")

    def test_alternate_manifest_is_experimental_and_default_is_read_only(self):
        prepare_path = SCRIPT.with_name("prepare_manifest.py")
        spec = importlib.util.spec_from_file_location("static_blocker_manifest", prepare_path)
        prepare = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(prepare)
        source = REPO / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json"
        before = source.read_bytes()
        with tempfile.TemporaryDirectory() as temp:
            destination = Path(temp) / "candidate-manifest.json"
            prepare.prepare(source, destination)
            manifest = json.loads(destination.read_text())
        self.assertTrue(manifest["experimental_only"])
        self.assertEqual(manifest["experiment"], "static_blocker_owner_history_combined_scan")
        row = next(row for row in manifest["sources"] if row["id"] == prepare.IDENTITY)
        self.assertEqual(row["kind"], "current_original")
        self.assertEqual(row["generator"], prepare.GENERATOR)
        self.assertEqual(row["generator_inputs"], prepare.INPUTS)
        self.assertEqual(row["generator_output"], prepare.OUTPUT)
        self.assertEqual(source.read_bytes(), before)


if __name__ == "__main__":
    unittest.main()
