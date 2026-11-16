import subprocess
import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
CLI = ROOT / "Tools/CI/run_skirmish_source_simulation.py"


class S073HistoricalInputCliControls(unittest.TestCase):
    def reject(self, mission, *options):
        result = subprocess.run([sys.executable, str(CLI), "run", "--mission", mission,
                                 "--output", "/private/tmp/s073-historical-cli-must-not-create", *options],
                                cwd=ROOT, text=True, capture_output=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("--s073-historical-input-diagnostic requires an S073 run with both source diagnostics", result.stderr)

    def test_unbounded_without_current_source_diagnostic_rejected(self):
        self.reject("S073", "--s073-historical-input-diagnostic", "--s073-source-host-diagnostic-admission")

    def test_missing_current_source_diagnostic_rejected(self):
        self.reject("S073", "--s073-historical-input-diagnostic", "--s073-source-host-diagnostic-admission", "--max-ticks", "2")

    def test_missing_host_diagnostic_rejected(self):
        self.reject("S073", "--s073-historical-input-diagnostic", "--s073-current-source-diagnostic", "--max-ticks", "2")

    def test_other_mission_rejected(self):
        self.reject("S002", "--s073-historical-input-diagnostic", "--max-ticks", "2")


if __name__ == "__main__":
    unittest.main()
