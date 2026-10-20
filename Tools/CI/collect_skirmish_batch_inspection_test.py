"""Modest synthetic collection joins; actual inspector/native APIs are reused, no native credit."""
import copy
import csv
import io
import json
from pathlib import Path
import shutil
import sys
import unittest

HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
from skirmish_operational_test_fixture import (ROOT,OperationalFixture,owned_workspace,cleanup_workspace,build_baseline)
import collect_skirmish_batch_inspection as collector
import skirmish_production_operational_inspector as inspector
from skirmish_production_queue import digest
if any(Path(m.__file__).resolve().parent!=HERE for m in (collector,inspector)):
    raise RuntimeError("Actual production copies must be imported from integration Tools/CI")

class CollectionControls(unittest.TestCase):
    number = 0
    @classmethod
    def setUpClass(cls):
        cls.temporary=owned_workspace("c");cls.work=Path(cls.temporary.name)
        cls.plan=build_baseline(ROOT)
    @classmethod
    def tearDownClass(cls):cleanup_workspace(cls.temporary)
    def setUp(self):
        type(self).number+=1
        self.f=OperationalFixture(self.work/str(type(self).number),self.plan)
        for i, paths in enumerate(self.f.paths):
            claim = json.loads(paths["claim"].read_bytes())
            parent = Path(claim["artifact_directory"]) / "candidates" / self.f.store["candidate_sha256"]
            rid = self.f.csvrows[i]["run_id"].lower()
            paths["final"] = parent / (rid + ".jsonl")
            paths["contacts"] = Path(str(paths["final"]) + ".touch.jsonl")
            paths["log"] = parent / (rid + "-log.txt")
            self.f.csvrows[i]["trace_path"] = paths["final"].relative_to(self.f.root).as_posix()
            self.f.csvrows[i]["log_path"] = paths["log"].relative_to(self.f.root).as_posix()
        self.spec = self.f.materialize()
        for source, target in [("events.jsonl", "batch-events.jsonl"), ("outer.json", "outer-wrapper-receipt.json"),
                               ("before.json", "before-source-receipt.json"), ("after.json", "after-source-receipt.json")]:
            shutil.copyfile(self.f.e / source, self.f.e / target)
        for paths in self.f.paths:
            shutil.copyfile(self.f.e / "runs.csv", paths["final"].parent / "runs.csv")
        self.ref = {"path": str(self.f.descriptorpath), "sha256": digest(self.f.descriptorpath)}
    def inspect(self):
        before = {p: digest(p) for p in self.f.root.rglob("*") if p.is_file()}
        result = collector.collect_and_inspect(self.f.root, self.ref)
        self.assertFalse(result["qualified"])
        self.assertFalse(result["queue_success_enabled"])
        self.assertEqual(6, len(result["admission"]["missing"]))
        self.assertEqual(before, {p: digest(p) for p in self.f.root.rglob("*") if p.is_file()})
        return result
    def test_valid_victory_defeat_draw_remain_unqualified(self):
        result = self.inspect()
        self.assertEqual("NormalCompletedUnqualified", result["result"], result)
        self.assertEqual([1, 0, 0], [a["wins"] for a in result["attempts"]])
    def test_actual_nonzero_wrapper_refuses(self):
        path = self.f.e / "outer-wrapper-receipt.json"
        value = json.loads(path.read_bytes()); value["checked_wrapper_exit"] = 2
        path.write_text(json.dumps(value))
        self.assertEqual("Incomplete", self.inspect()["result"])
    def test_wrong_descriptor_pin(self):
        self.ref["sha256"] = "0" * 64
        self.assertEqual("Incomplete", self.inspect()["result"])
    def test_missing_native_stream(self):
        self.f.paths[0]["native"].unlink()
        self.assertEqual("Incomplete", self.inspect()["result"])
    def test_duplicate_csv_live_match_refuses(self):
        path = self.f.paths[0]["final"].parent / "runs.csv"
        rows = list(csv.DictReader(io.StringIO(path.read_text())))
        buf = io.StringIO(); writer = csv.DictWriter(buf, fieldnames=collector.HEADER)
        writer.writeheader(); writer.writerows(rows + [rows[0]])
        path.write_text(buf.getvalue())
        self.assertEqual("Incomplete", self.inspect()["result"])
    def test_claim_change_refuses(self):
        path = self.f.paths[0]["claim"]
        claim = json.loads(path.read_bytes()); claim["worker"] = "foreign"
        path.write_text(json.dumps(claim))
        self.assertEqual("Incomplete", self.inspect()["result"])
    def test_failed_batch_fact_preserved_even_if_trace_missing(self):
        path = self.f.e / "batch-events.jsonl"
        with path.open("a") as stream:
            stream.write(json.dumps({"kind": "BatchFailed", "facts": {"error": "ariaStopped:Blocked"}}) + "\n")
        self.f.paths[0]["native"].unlink()
        result = self.inspect()
        self.assertEqual("Incomplete", result["result"])
        self.assertEqual("ariaStopped:Blocked", result["failure_records"][0]["error"])
    def test_traversal_refused(self):
        self.ref["path"] = "../foreign/batch.json"
        self.assertEqual("Incomplete", self.inspect()["result"])

if __name__ == "__main__": unittest.main(verbosity=2)
