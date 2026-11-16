import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from compare_s003_lazy_defense_pair import compare


class LazyDefenseComparisonTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.dirs = [self.root / "baseline", self.root / "candidate"]
        key = "/repo/Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.s003.cs"
        for i, directory in enumerate(self.dirs):
            directory.mkdir()
            build = {"status": "Passed", "mission": "S003", "repository_root": "/repo",
                     "source_and_reference_pins": {key: str(i)}}
            build_path = directory / "build.json"; build_path.write_text(json.dumps(build))
            result = {"source_outcome": "None", "tick": 1204, "advance_wall_seconds": 2 + i,
                      "cumulative_aria_wall_seconds": {"observation": 1 + i}}
            receipt = {"mission": "S003", "seed": 104732, "status": "BoundedPrefix", "process_exit": 0,
                       "timed_out": False, "validation_passed": False, "native_acceptance": False,
                       "mission_ready": False, "source_result": result,
                       "build_receipt": {"path": str(build_path), "sha256": hashlib.sha256(build_path.read_bytes()).hexdigest()}}
            (directory / "run-receipt.json").write_text(json.dumps(receipt))
            (directory / "result.json").write_text(json.dumps(result))
            (directory / "run-request.json").write_text(json.dumps({"max_ticks": 1200, "output_directory": str(directory), "runtime_config": str(directory)}))
            rows = []
            for frame in range(1200):
                row = {"frame": frame, "observation": {"Infantry": 12}, "scope": "LogicalPublicData",
                       "logical_defense_source_diagnostic": True, "native_rendered_input": False,
                       "s003_defense_site_diagnostics": [{"cell": 1}], "s003_site_projection_observed": True}
                if i == 1 and frame:
                    row.update(s003_defense_site_diagnostics=None, s003_site_projection_observed=False,
                               s003_site_projection_required=False)
                rows.append(json.dumps(row))
            (directory / "aria-trace.jsonl").write_text("\n".join(rows) + "\n")
            (directory / "validator-state.jsonl").write_text(json.dumps({"world_id": str(i), "tick": 1204, "actors": [1]}) + "\n")

    def tearDown(self):
        self.tmp.cleanup()

    def run_comparison(self):
        compare(*self.dirs, self.root / "comparison.json")

    def test_matching_pair(self):
        self.run_comparison()
        self.assertTrue((self.root / "comparison.json").exists())

    def test_same_build_pair(self):
        path = self.dirs[1] / "run-receipt.json"
        receipt = json.loads(path.read_bytes())
        receipt["build_receipt"] = json.loads((self.dirs[0] / "run-receipt.json").read_bytes())["build_receipt"]
        path.write_text(json.dumps(receipt))
        self.run_comparison()
        self.assertTrue(json.loads((self.root / "comparison.json").read_bytes())["same_build"])

    def test_rejects_changed_action(self):
        path = self.dirs[1] / "aria-trace.jsonl"
        rows = path.read_text().splitlines(); row = json.loads(rows[2]); row["action"] = "fabricated"; rows[2] = json.dumps(row)
        path.write_text("\n".join(rows) + "\n")
        with self.assertRaises(ValueError):
            self.run_comparison()

    def test_rejects_world_state_difference(self):
        (self.dirs[1] / "validator-state.jsonl").write_text(json.dumps({"world_id": "1", "tick": 1204, "actors": [2]}) + "\n")
        with self.assertRaises(ValueError):
            self.run_comparison()

    def test_rejects_missing_required_projection(self):
        path = self.dirs[1] / "aria-trace.jsonl"
        rows = path.read_text().splitlines(); row = json.loads(rows[2]); row["s003_site_projection_required"] = True; rows[2] = json.dumps(row)
        path.write_text("\n".join(rows) + "\n")
        with self.assertRaises(ValueError):
            self.run_comparison()


if __name__ == "__main__":
    unittest.main()
