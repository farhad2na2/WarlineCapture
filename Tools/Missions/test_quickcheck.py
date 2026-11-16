"""End-to-end authoring controls with the actual .NET executables, no Unity."""
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

HERE = Path(__file__).resolve().parent
EXAMPLES = HERE / "examples"


class QuickcheckTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.run_count = 0
        shutil.copy(EXAMPLES / "old-quarter-source.json", self.root)

    def run_cli(self, *args, code=0):
        self.run_count += 1
        out = self.root / str(self.run_count)
        proc = subprocess.run([sys.executable, "-B", str(HERE / "quickcheck.py"), *map(str, args),
                               "--out", str(out)], capture_output=True, text=True, timeout=120)
        self.assertEqual(proc.returncode, code, proc.stdout + proc.stderr)
        return out, json.loads((out / "summary.json").read_bytes())

    def test_same_binary_for_changed_orders_and_several_seeded_missions(self):
        draft = json.loads((EXAMPLES / "o004-quiet-archive.json").read_bytes())
        file = self.root / "draft.json"
        file.write_text(json.dumps(draft))
        out, first = self.run_cli("--missions", "S019,S020,O001,O002,O003,O004", "--sizes", "War",
                                  "--skirmish-trace", EXAMPLES / "convoy-dwell.json", "--operation-draft", file,
                                  "--operation-seed-offsets", "0,7")
        self.assertEqual(first["skirmish"]["custom_rule_probes"], 2)
        self.assertEqual(first["operations"]["host_runs"], 6)
        missions = json.loads((out / "operations.json").read_bytes())["missions"]
        self.assertTrue(all(r["returned"] and r["settlement_revision"] >= 0 for m in missions[:3] for r in m["runs"]))
        self.assertEqual([r["outcome"] for r in missions[3]["runs"]], ["Victory", "Partial", "Defeat", "Withdrawn"])
        self.assertIn("archive", first["operations"]["unresolved_physical_roles"]["O004"])
        self.assertFalse(first["physical_source_fit_verified"])
        # An actual data edit changes when withdrawal occurs. No new build or per-mission adapter.
        draft["cases"][-1]["orders"][0]["at"] = 17
        file.write_text(json.dumps(draft))
        out, second = self.run_cli("--missions", "O004", "--operation-draft", file)
        self.assertEqual(first["operations"]["build"]["key"], second["operations"]["build"]["key"])
        self.assertTrue(second["operations"]["build"]["cache_hit"])
        self.assertEqual(json.loads((out / "operations.json").read_bytes())["missions"][0]["runs"][-1]["tick"], 17)

    def test_catalog_fallback_does_not_play_an_unauthored_operation(self):
        _, result = self.run_cli("--missions", "O004", code=2)
        self.assertEqual(result["status"], "missing_graph")
        self.assertEqual(result["operations"]["host_runs"], 0)

    def test_draft_cannot_shadow_a_coded_mission(self):
        draft = json.loads((EXAMPLES / "o004-quiet-archive.json").read_bytes())
        draft["mission"] = "O001"
        file = self.root / "shadow.json"
        file.write_text(json.dumps(draft))
        _, result = self.run_cli("--missions", "O001", "--operation-draft", file, code=1)
        self.assertIn("cannot replace an existing authored binding", str(result["operations"]["failures"]))

    def test_actual_failure_reasons_survive_the_report(self):
        draft = json.loads((EXAMPLES / "o004-quiet-archive.json").read_bytes())
        file = self.root / "invalid.json"
        draft["groups"][0]["count"] -= 1
        file.write_text(json.dumps(draft))
        _, result = self.run_cli("--missions", "O004", "--operation-draft", file, code=1)
        self.assertIn("fixed Player package", str(result["operations"]["failures"]))
        draft["groups"][0]["count"] += 1
        draft["graph"]["Nodes"][1]["Prerequisites"] = ["not_a_node"]
        file.write_text(json.dumps(draft))
        _, result = self.run_cli("--missions", "O004", "--operation-draft", file, code=1)
        self.assertIn("missing_prerequisite", str(result["operations"]["failures"]))
        trace = json.loads((EXAMPLES / "convoy-dwell.json").read_bytes())
        trace["cases"][0]["expect"]["Outcome"] = "Defeat"
        file.write_text(json.dumps(trace))
        out, result = self.run_cli("--missions", "S019", "--sizes", "Standard", "--skirmish-trace", file, code=1)
        failed = [r for r in json.loads((out / "skirmish-replay/report.json").read_bytes())["cases"] if r["status"] == "failed"]
        self.assertEqual([r["id"] for r in failed], ["S019.Standard.custom.leaving_destination_resets_dwell"])

    def test_changed_campaign_source_pin_is_rejected(self):
        file = self.root / "old-quarter-source.json"
        pin = json.loads(file.read_bytes())
        pin["physical_source"]["sha256"] = "0" * 64
        file.write_text(json.dumps(pin))
        draft = self.root / "draft.json"
        shutil.copy(EXAMPLES / "o004-quiet-archive.json", draft)
        _, result = self.run_cli("--missions", "O004", "--operation-draft", draft, code=1)
        self.assertIn("physical_source: selected map file changed", str(result["errors"]))


if __name__ == "__main__":
    unittest.main()
