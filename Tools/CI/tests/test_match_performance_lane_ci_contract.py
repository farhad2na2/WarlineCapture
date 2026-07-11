import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
JENKINSFILE = ROOT / "Jenkinsfile.groovy"
WRAPPER = ROOT / "Tools/CI/InvokeUnityMatchPerformanceLane.ps1"


class MatchPerformanceLaneCiContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.jenkins = JENKINSFILE.read_text(encoding="utf-8")
        cls.wrapper = WRAPPER.read_text(encoding="utf-8")

    def test_lane_is_weekly_scheduled_and_manually_selectable(self) -> None:
        self.assertIn("cron('H H * * 1')", self.jenkins)
        self.assertIn("RUN_EDITOR_MATCH_PERFORMANCE", self.jenkins)
        self.assertIn("triggeredBy 'TimerTrigger'", self.jenkins)

    def test_lane_reuses_aph800_execute_method_wrapper(self) -> None:
        self.assertEqual(
            2,
            self.wrapper.count("InvokeUnityExecuteMethodValidation.ps1"),
        )
        self.assertNotIn("InvokeUnity.ps1", self.wrapper)
        self.assertIn("RunPerformanceRegressionBaseline", self.wrapper)
        self.assertIn("MatchGcAllocationCallstackCapture.RunSteadyState", self.wrapper)

    def test_lane_keeps_the_gc_budget_fixed_at_1024_bytes(self) -> None:
        self.assertIn("[int] $GcBudgetBytes = 1024", self.wrapper)
        self.assertIn("$GcBudgetBytes -ne 1024", self.wrapper)
        self.assertIn("-GcBudgetBytes 1024", self.jenkins)

    def test_lane_prepares_the_gc_profiler_directory_on_windows(self) -> None:
        self.assertIn('$env:OS -eq "Windows_NT"', self.wrapper)
        self.assertIn('Join-Path $driveRoot "private/tmp"', self.wrapper)

    def test_lane_archives_logs_and_machine_readable_summary(self) -> None:
        self.assertIn("TestResults/MatchPerformance*.log", self.jenkins)
        self.assertIn("TestResults/MatchPerformanceLaneSummary.json", self.jenkins)
        self.assertIn("performance_regression_match_baseline.json", self.jenkins)
        self.assertIn("perf_match-gc-callstack-capture.md", self.jenkins)

if __name__ == "__main__":
    unittest.main()
