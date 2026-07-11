import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI.validate_match_performance_lane import ValidationError, validate


class MatchPerformanceLaneValidationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.baseline = self.root / "baseline.json"
        self.gc_report = self.root / "gc.md"
        self.write_baseline()
        self.write_gc("Passed", 512, 1024)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def write_baseline(self, **overrides) -> None:
        payload = {
            "source": "Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline",
            "frameCount": 800,
            "p95FrameMs": 8.25,
            "editorP95FrameBudgetMs": 50.0,
            "editorP95FrameBudgetPassed": True,
        }
        payload.update(overrides)
        self.baseline.write_text(json.dumps(payload), encoding="utf-8")

    def write_gc(self, status: str, measured: int, budget: int) -> None:
        self.gc_report.write_text(
            "# Capture\n"
            f"- Steady-state player-relevant GC budget: {status} "
            f"({measured} / {budget} bytes)\n",
            encoding="utf-8",
        )

    def test_accepts_passing_baseline_and_gc_budget(self) -> None:
        summary = validate(self.baseline, self.gc_report, 1024)
        self.assertEqual("Passed", summary["result"])
        self.assertEqual(512, summary["steadyStateGc"]["measuredBytes"])

    def test_rejects_measured_gc_above_budget(self) -> None:
        self.write_gc("Failed", 1025, 1024)
        with self.assertRaisesRegex(ValidationError, "GC budget failed"):
            validate(self.baseline, self.gc_report, 1024)

    def test_rejects_changed_gc_budget(self) -> None:
        self.write_gc("Passed", 512, 2048)
        with self.assertRaisesRegex(ValidationError, "budget changed"):
            validate(self.baseline, self.gc_report, 1024)

    def test_rejects_attempt_to_weaken_expected_budget(self) -> None:
        with self.assertRaisesRegex(ValidationError, "fixed at 1024"):
            validate(self.baseline, self.gc_report, 2048)

    def test_rejects_failed_editor_p95_gate(self) -> None:
        self.write_baseline(editorP95FrameBudgetPassed=False)
        with self.assertRaisesRegex(ValidationError, "p95 budget did not pass"):
            validate(self.baseline, self.gc_report, 1024)

    def test_rejects_missing_evidence(self) -> None:
        self.gc_report.unlink()
        with self.assertRaisesRegex(ValidationError, "missing steady-state GC report"):
            validate(self.baseline, self.gc_report, 1024)

    def test_rejects_ambiguous_gc_budget_rows(self) -> None:
        row = self.gc_report.read_text(encoding="utf-8")
        self.gc_report.write_text(row + row, encoding="utf-8")
        with self.assertRaisesRegex(ValidationError, "exactly one"):
            validate(self.baseline, self.gc_report, 1024)


if __name__ == "__main__":
    unittest.main()
