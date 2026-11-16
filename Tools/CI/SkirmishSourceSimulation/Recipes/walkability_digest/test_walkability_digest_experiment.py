import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]


def load(name):
    path = Path(__file__).with_name(name + ".py")
    spec = importlib.util.spec_from_file_location("digest_" + name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


class WalkabilityDigestExperimentTests(unittest.TestCase):
    def setUp(self):
        self.generator = load("generate")
        self.witness, self.grid, self.shim = [(ROOT / p).read_text() for p in
                                              (self.generator.WITNESS, self.generator.GRID, self.generator.SHIM)]

    def test_exact_original_fallback_and_opt_in(self):
        output = self.generator.transform(self.witness, self.grid, self.shim)
        self.assertIn("WARLINE_SOURCE_FAST_WALKABLE_DIGEST", output)
        self.assertIn("SourceFastWalkabilityDigestEnabled", output)
        self.assertIn("sha.TransformBlock(bytes,0,count,bytes,0)", output)
        self.assertIn("WalkableHash.Equals(Digest(cells))", output)

    def test_changed_layout_is_rejected(self):
        with self.assertRaises(ValueError):
            self.generator.transform(self.witness, self.grid.replace("public byte Value; // 1 = walkable", "public int Value; // 1 = walkable", 1), self.shim)

    def test_changed_buffer_or_witness_is_rejected(self):
        with self.assertRaises(ValueError):
            self.generator.transform(self.witness, self.grid, self.shim.replace("internal T[] Copy()=>rows.ToArray();", "", 1))
        with self.assertRaises(ValueError):
            self.generator.transform(self.witness.replace(self.generator.SIGNATURE, "", 1), self.grid, self.shim)

    def test_manifest_rewrite_leaves_default_unchanged(self):
        prepare = load("prepare_manifest")
        source = ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json"
        before = hashlib.sha256(source.read_bytes()).hexdigest()
        with tempfile.TemporaryDirectory() as temporary:
            target = Path(temporary) / "manifest.json"; prepare.prepare(source, target)
            rows = [row for row in json.loads(target.read_bytes())["sources"] if row.get("id") == "Shared_GridWalkableSourceWitness.cs"]
            self.assertEqual(1, len(rows)); self.assertEqual("current_original", rows[0]["kind"])
            self.assertEqual(prepare.INPUTS, rows[0]["generator_inputs"])
        self.assertEqual(before, hashlib.sha256(source.read_bytes()).hexdigest())


if __name__ == "__main__":
    unittest.main()
