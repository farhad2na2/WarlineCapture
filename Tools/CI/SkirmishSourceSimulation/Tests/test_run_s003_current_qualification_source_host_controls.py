import importlib.util
from pathlib import Path
import unittest


MODULE_PATH = Path(__file__).resolve().parents[1] / "Recipes/run_s003_current_qualification_source_host_controls.py"
SPEC = importlib.util.spec_from_file_location("s003_current_qualification_runner", MODULE_PATH)
runner = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(runner)
EXPECTED_COMPILER_GRAPH_SHA256 = "349bdd26f0326a57565effe515a848126a72384250a039ec8b8f266d69e9006e"


def valid_report():
    report = {
        "kind": "S003CurrentQualificationSourceHostControls",
        "status": runner.CONTROL_STATUS,
        "capture_source_graph_sha256": "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb",
        "runtime_compiler_graph_sha256": EXPECTED_COMPILER_GRAPH_SHA256,
        "capture_graph_is_runtime_graph": False,
        "building_count": 10,
        "unit_count": 19,
    }
    report.update({key: True for key in runner.CONTROL_TRUE_FIELDS})
    report.update({key: False for key in runner.CONTROL_FALSE_FIELDS})
    return report


class CurrentS003ControlRunnerTests(unittest.TestCase):
    def test_accepts_exact_current_marker_and_source_only_status(self):
        self.assertEqual(runner.CONTROL_STATUS,
                         runner.validate_control_output(valid_report(), runner.PASS + "\n",
                                                        EXPECTED_COMPILER_GRAPH_SHA256))

    def test_rejects_obsolete_t4_marker(self):
        obsolete = "[S003CurrentQualificationSourceHostControls] result=Passed context=ActualT4 metadata=SourceOnly mutants=6 historical=Structural world=0 readiness=0"
        with self.assertRaises(RuntimeError):
            runner.validate_control_output(valid_report(), obsolete + "\n", EXPECTED_COMPILER_GRAPH_SHA256)

    def test_rejects_missing_or_duplicate_current_marker(self):
        for output in ("", runner.PASS + "\n" + runner.PASS + "\n"):
            with self.subTest(output=output), self.assertRaises(RuntimeError):
                runner.validate_control_output(valid_report(), output, EXPECTED_COMPILER_GRAPH_SHA256)

    def test_rejects_failed_current_control_field(self):
        report = valid_report()
        report["altered_canonical_catalog_row_rejected"] = False
        with self.assertRaisesRegex(RuntimeError, "altered_canonical_catalog_row_rejected"):
            runner.validate_control_output(report, runner.PASS + "\n", EXPECTED_COMPILER_GRAPH_SHA256)

    def test_rejects_promoted_runtime_status(self):
        report = valid_report()
        report["mission_ready"] = True
        with self.assertRaisesRegex(RuntimeError, "mission_ready"):
            runner.validate_control_output(report, runner.PASS + "\n", EXPECTED_COMPILER_GRAPH_SHA256)

    def test_rejects_control_for_different_compiler_graph(self):
        report = valid_report()
        report["runtime_compiler_graph_sha256"] = "34194993cb3430293923140addf3d19d6222e86a675fb82a2fe06b83f84eaf9f"
        with self.assertRaisesRegex(RuntimeError, "Current S003 graph identities"):
            runner.validate_control_output(report, runner.PASS + "\n", EXPECTED_COMPILER_GRAPH_SHA256)


if __name__ == "__main__":
    unittest.main()
