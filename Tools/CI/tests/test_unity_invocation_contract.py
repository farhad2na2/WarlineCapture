import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[3]
WRAPPER = ROOT / "Tools/CI/InvokeUnity.ps1"


class UnityInvocationContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = WRAPPER.read_text(encoding="utf-8")

    def test_process_is_refreshed_after_waiting_for_exit(self):
        self.assertIn("$process.WaitForExit()\n$process.Refresh()", self.source)

    def test_missing_exit_code_fails_closed(self):
        self.assertIn(
            "elseif ($null -eq $process.ExitCode) { 1 }",
            self.source,
        )
        self.assertNotIn(
            "elseif ($null -eq $process.ExitCode) { 0 }",
            self.source,
        )


if __name__ == "__main__":
    unittest.main()
