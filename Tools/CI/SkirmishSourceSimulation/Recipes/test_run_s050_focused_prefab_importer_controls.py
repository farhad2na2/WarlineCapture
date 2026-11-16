#!/usr/bin/env python3
"""Input pin behavior controls for the compiled S050 importer runner."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).with_name("run_s050_focused_prefab_importer_controls.py")
spec = importlib.util.spec_from_file_location("s050_importer_control_runner_test", SCRIPT)
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class S050ImporterControlRunnerTests(unittest.TestCase):
    def test_accepts_current_exact_pin_and_rejects_drift(self):
        with tempfile.TemporaryDirectory(prefix="s050-importer-pins-") as temp:
            source = Path(temp) / "source.cs"
            source.write_text("class Current {}\n")
            digest = runner.sha(source)
            runner.verify_pins({str(source): digest})
            with self.assertRaisesRegex(ValueError, "pin drift"):
                runner.verify_pins({str(source): "0" * 64})

    def test_compiled_control_requires_explicit_nonreadiness_marker(self):
        source = (SCRIPT.parent.parent / "Tests/S050FocusedPrefabImporterControls.cs").read_text()
        self.assertIn("GeneratedReferenceTemplateView.Read", source)
        self.assertIn("GeneratedReferenceTemplateImporter.Preflight", source)
        self.assertIn("result=Passed positive=1 negative=7 worldAllocated=False missionReady=False", source)
        for control in ("missing-required-root", "tampered-unsupported-reason",
                        "non-presentation-unsupported-component", "tampered-requested-root"):
            self.assertIn(control, source)
        self.assertIn("badRequestable", source)
        self.assertIn("badSelection", source)


if __name__ == "__main__":
    unittest.main(verbosity=2)
