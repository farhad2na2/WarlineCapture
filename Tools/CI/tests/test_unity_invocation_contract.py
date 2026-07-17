import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[3]
WRAPPER = ROOT / "Tools/CI/InvokeUnity.ps1"


class UnityInvocationContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = WRAPPER.read_text(encoding="utf-8")

    def test_exit_code_is_captured_without_post_wait_refresh(self):
        wait_index = self.source.index("$process.WaitForExit()")
        exit_code_index = self.source.index("$exitCode =", wait_index)
        self.assertNotIn(
            "$process.Refresh()",
            self.source[wait_index:exit_code_index],
        )
        self.assertIn(
            "$exitCode = if ($timedOut) { 124 } else { $process.ExitCode }",
            self.source,
        )

    def test_missing_exit_code_fails_closed(self):
        self.assertIn(
            "if ($null -eq $exitCode)",
            self.source,
        )
        self.assertIn(
            'Write-InvocationLog "[UnityInvoke] ERROR: Unity exited without a readable process exit code. Failing closed."',
            self.source,
        )
        self.assertIn("$exitCode = 1", self.source)

    def test_zero_process_exit_is_overridden_by_explicit_unity_fatal_markers(self):
        self.assertIn("function Find-UnityLoggedFailure", self.source)
        self.assertIn("executeMethod method .+ threw exception", self.source)
        self.assertIn(
            "Application will terminate with return code [1-9][0-9]*",
            self.source,
        )
        self.assertIn(
            "$loggedFailure = Find-UnityLoggedFailure -UnityLogFile $resolvedLogFile",
            self.source,
        )


if __name__ == "__main__":
    unittest.main()
