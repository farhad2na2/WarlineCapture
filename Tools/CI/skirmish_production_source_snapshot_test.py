import copy
import json
from pathlib import Path
import tempfile
import subprocess
import sys
import unittest

from skirmish_production_source_snapshot import capture, compare


class SourceMutationAttributionTests(unittest.TestCase):
    def setUp(self):
        fixtures = Path(__file__).resolve().parents[2] / "Temp/SkirmishSourceMutationOfflineTests"
        fixtures.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="case-", dir=fixtures)
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        for name in ("Assets", "Packages", "ProjectSettings"):
            (self.root / name).mkdir()
        (self.root / "Assets/known.cs").write_text("pinned", encoding="utf-8")

    def test_generated_scene_metadata_is_included_and_attributed(self):
        before = capture(self.root, self.root / "before.json")
        folder = self.root / "Assets/SceneDependencyCache"
        folder.mkdir()
        (folder / "generated.sceneWithBuildSettings").write_bytes(b"excluded by original extension rules")
        no_meta = capture(self.root, self.root / "no-meta.json")
        self.assertEqual(compare(before, no_meta)["result"], "Unchanged")
        (folder / "generated.sceneWithBuildSettings.meta").write_text("generated metadata", encoding="utf-8")
        after = capture(self.root, self.root / "after.json")
        result = compare(before, after)
        self.assertEqual(result["result"], "SourceChanged")
        self.assertEqual(result["added"][0]["path"], "Assets/SceneDependencyCache/generated.sceneWithBuildSettings.meta")
        self.assertEqual(result["changed"], [])

    def test_exact_changed_removed_and_corrupted_aggregate(self):
        before = capture(self.root, self.root / "before.json")
        (self.root / "Assets/known.cs").write_text("changed", encoding="utf-8")
        after = capture(self.root, self.root / "after.json")
        self.assertEqual(compare(before, after)["changed"][0]["path"], "Assets/known.cs")
        bad = copy.deepcopy(after)
        bad["candidate_sha256"] = "a" * 64
        with self.assertRaises(ValueError):
            compare(before, bad)
        with self.assertRaises(FileExistsError):
            capture(self.root, self.root / "before.json")

    def test_snapshot_output_cannot_change_hashed_source(self):
        for directory in ("Assets", "Packages", "ProjectSettings"):
            with self.subTest(directory=directory), self.assertRaises(ValueError):
                capture(self.root, self.root / directory / "snapshot.json")

    def test_cli_compare_writes_exclusive_output_and_rejects_source_tree(self):
        capture(self.root, self.root / "before.json")
        capture(self.root, self.root / "after.json")
        output = self.root / "comparison.json"
        command = [sys.executable, str(Path(__file__).with_name("skirmish_production_source_snapshot.py")), "compare", "--before", str(self.root / "before.json"), "--after", str(self.root / "after.json"), "--output"]
        result = subprocess.run(command + [str(output)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(json.loads(output.read_text())["result"], "Unchanged")
        saved = output.read_bytes()
        self.assertEqual(subprocess.run(command + [str(output)], capture_output=True).returncode, 2)
        self.assertEqual(output.read_bytes(), saved)
        for directory in ("Assets", "Packages", "ProjectSettings"):
            bad = self.root / directory / "comparison.json"
            self.assertEqual(subprocess.run(command + [str(bad)], capture_output=True).returncode, 2)
            self.assertFalse(bad.exists())


if __name__ == "__main__":
    unittest.main(verbosity=2)
