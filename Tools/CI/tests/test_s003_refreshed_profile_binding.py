import json
import runpy
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class S003RefreshedProfileBindingTests(unittest.TestCase):
    def test_current_descriptor_binds_qualified_seed_and_rejects_tampering(self):
        repo = Path(__file__).resolve().parents[3]
        cli = repo / "Tools/CI/run_skirmish_source_simulation.py"
        evidence_root = repo / "T Logs/SkirmishProduction"
        evidence = evidence_root / "s003-dynamic-evidence-qualification-20261104-T1/source-only-qualification-evidence.json"
        code = """import runpy,sys
n=runpy.run_path(sys.argv[1],run_name='fixture'); p=n['owned'].require('skirmish_source_profiles')
p.bind_configuration_qualification(sys.argv[2]); p.require_profile('S003',104732)
assert p.runtime_backend('S003')=='TypedGroundS026V1'
try: p.require_profile('S003',104733)
except ValueError: pass
else: raise AssertionError('wrong seed admitted')
"""
        result = subprocess.run([sys.executable, "-B", "-c", code, str(cli), str(evidence)], capture_output=True, text=True)
        self.assertEqual(0, result.returncode, result.stderr)
        with tempfile.TemporaryDirectory() as directory:
            alias = Path(directory).resolve() / evidence.name
            alias.write_bytes(evidence.read_bytes())
            result = subprocess.run([sys.executable, "-B", "-c", code, str(cli), str(alias)], capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("Repository-owned S003 evidence required", result.stderr)
        with tempfile.TemporaryDirectory(dir=evidence_root) as directory:
            copied = Path(directory).resolve() / evidence.name
            copied.write_bytes(evidence.read_bytes())
            result = subprocess.run([sys.executable, "-B", "-c", code, str(cli), str(copied)], capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stderr)
            changed = json.loads(copied.read_bytes())
            changed["target"]["seed"] = 104733
            copied.write_text(json.dumps(changed))
            result = subprocess.run([sys.executable, "-B", "-c", code, str(cli), str(copied)], capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertIn("Refreshed S003 evidence reconstruction mismatch", result.stderr)


if __name__ == "__main__":
    unittest.main()
