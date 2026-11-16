"""The source CLI must never silently ignore an authoring input on a run."""
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
CLI = ROOT / 'Tools/CI/run_skirmish_source_simulation.py'


class IgnoredAuthoringInputTests(unittest.TestCase):
    def test_unconsumed_commands_reject_before_creating_output(self):
        with tempfile.TemporaryDirectory() as directory:
            for command, mission in (('run', 'S002'), ('run', 'S026'), ('build', 'S002'), ('resolve', 'S002')):
                output = Path(directory) / (command + mission)
                result = subprocess.run([sys.executable, '-B', str(CLI), command, '--mission', mission,
                    '--output', str(output), '--authoring-input', str(Path(directory) / 'draft.json')],
                    cwd=ROOT, capture_output=True, text=True, timeout=30)
                self.assertEqual(result.returncode, 2, result.stderr)
                self.assertIn('--authoring-input is not consumed by this command', result.stderr)
                self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
