import hashlib
import importlib.util
import tempfile
import unittest
from pathlib import Path

HERE=Path(__file__).resolve().parent
SPEC=importlib.util.spec_from_file_location("pathfind_diag",HERE/"generate.py")
MODULE=importlib.util.module_from_spec(SPEC);SPEC.loader.exec_module(MODULE)

class Controls(unittest.TestCase):
    def setUp(self):
        self.source=(MODULE.ROOT/MODULE.SOURCE).read_text()

    def test_exact_reflection_execute_and_target_filter(self):
        candidate=MODULE.transform(self.source)
        self.assertEqual(candidate.count("m.Invoke(boxed,new object[]{i})"),1)
        self.assertIn('typeof(T).FullName=="Game.Runtime.PathfindBatchJob"',candidate)
        self.assertIn('GetEnvironmentVariable("WARLINE_PATHFIND_DIAGNOSTIC")=="1"',candidate)
        self.assertIn("Limit=256",candidate)
        self.assertIn("UnityEngine.Time.frameCount",candidate)
        for part in ("Entities", "UnitGrids", "RequestedGoals", "Goals", "Status", "FailureCodes", "ExpansionCounts", "AlternateAttempts", "Stopwatch.GetTimestamp"):
            self.assertIn(part,candidate)

    def test_fail_closed_anchors(self):
        with self.assertRaisesRegex(ValueError,"exactly one"):
            MODULE.transform(self.source.replace(MODULE.ANCHOR,"",1))
        with self.assertRaisesRegex(ValueError,"exactly one"):
            MODULE.transform(self.source+MODULE.ANCHOR)
        with self.assertRaisesRegex(ValueError,"namespace boundary"):
            MODULE.transform(self.source.replace("}\nnamespace Unity.Collections{","}",1))

    def test_output_hash_and_source_unchanged(self):
        source_path=MODULE.ROOT/MODULE.SOURCE;before=source_path.read_bytes()
        with tempfile.TemporaryDirectory() as tmp:
            manifest=MODULE.generate(MODULE.ROOT,Path(tmp)/"out")
            output=(Path(tmp)/"out"/manifest["candidate"]).read_bytes()
            self.assertEqual(hashlib.sha256(output).hexdigest(),manifest["candidate_sha256"])
            self.assertEqual("sha256(candidate bytes)",manifest["output_identity"])
            self.assertFalse(manifest["build_inputs_changed"])
        self.assertEqual(before,source_path.read_bytes())

if __name__=="__main__": unittest.main()
