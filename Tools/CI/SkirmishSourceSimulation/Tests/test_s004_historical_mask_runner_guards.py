import hashlib
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
RUNNER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/run_s004_bundle_driven_startup.py"

class HistoricalMaskAdmission(unittest.TestCase):
    def invoke(self, temp, *flags):
        output = Path(temp) / "not-created"
        result = subprocess.run([sys.executable, "-B", str(RUNNER), "--build", "/nonexistent",
                                 "--output", str(output), *flags], cwd=ROOT, text=True, capture_output=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(output.exists())
        return result.stderr

    def test_requires_matching_descriptor_and_hash(self):
        with tempfile.TemporaryDirectory() as temp:
            self.assertIn("must be supplied together", self.invoke(temp, "--historical-placement-input", "/nonexistent"))

    def test_diagnostic_requires_authoritative_epoch(self):
        with tempfile.TemporaryDirectory() as temp:
            self.assertIn("explicit authoritative source-only epoch", self.invoke(temp, "--historical-placement-input", "/nonexistent", "--historical-placement-input-sha256", "0" * 64))

    def test_rejects_descriptor_hash_drift(self):
        with tempfile.TemporaryDirectory() as temp:
            descriptor = Path(temp) / "input.json"
            descriptor.write_text("{}")
            self.assertIn("initialization SHA256 mismatch", self.invoke(temp, "--authoritative-source-epoch", "--historical-placement-input", str(descriptor), "--historical-placement-input-sha256", "0" * 64))

if __name__ == "__main__":
    unittest.main()
