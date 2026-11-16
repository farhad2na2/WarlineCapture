import runpy
import unittest
from pathlib import Path


class S003RefreshedSmokeTargetTests(unittest.TestCase):
    def test_source_target_and_scope_controls(self):
        repo = Path(__file__).resolve().parents[3]
        namespace = runpy.run_path(str(repo / "Tools/CI/run_skirmish_source_simulation.py"), run_name="fixture")
        smoke = namespace["owned"].require("skirmish_source_smoke")
        target = {"catalog_id": "S003", "size": "Standard", "difficulty": "Regular", "seed": 104732, "setup_hash": 2394754492}
        context = {"kind": "S003RefreshedSourceOnlyQualificationEvidence", "target": target,
                   "runtime_admission": False, "native_context_qualified": False,
                   "source_fit_certified": False, "mission_ready": False}
        self.assertEqual(target, smoke._source_only_qualification_target(context, "S003"))
        altered = dict(context, mission_ready=True)
        with self.assertRaises(Exception):
            smoke._source_only_qualification_target(altered, "S003")
        with self.assertRaises(Exception):
            smoke._source_only_qualification_target(context, "S002")


if __name__ == "__main__":
    unittest.main()
