import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
WRAPPER = ROOT / "Tools/CI/InvokeUnityExecuteMethodValidation.ps1"
VALIDATION_EXIT = ROOT / "Assets/Tests/Editor/ValidationExit.cs"


class UnityExecuteMethodValidationContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.wrapper = WRAPPER.read_text(encoding="utf-8")
        cls.validation_exit = VALIDATION_EXIT.read_text(encoding="utf-8")

    def test_unity_quit_flag_owns_editor_shutdown(self) -> None:
        self.assertIn(
            '$unityArguments = @("-quit")',
            self.wrapper,
        )
        self.assertIn('$unityArguments += @("-executeMethod", $ExecuteMethod)', self.wrapper)
        self.assertIn(
            'Array.IndexOf(commandLineArgs, "-quit") < 0',
            self.validation_exit,
        )

    def test_optional_build_target_precedes_execute_method(self) -> None:
        target_condition = 'if (-not [string]::IsNullOrWhiteSpace($BuildTarget))'
        target_arguments = '$unityArguments += @("-buildTarget", $BuildTarget)'
        execute_arguments = '$unityArguments += @("-executeMethod", $ExecuteMethod)'
        self.assertIn('[string] $BuildTarget = ""', self.wrapper)
        self.assertLess(self.wrapper.index(target_condition), self.wrapper.index(target_arguments))
        self.assertLess(self.wrapper.index(target_arguments), self.wrapper.index(execute_arguments))

    def test_log_read_retries_transient_file_locks(self) -> None:
        self.assertIn("[System.IO.IOException]", self.wrapper)
        self.assertIn("Start-Sleep -Milliseconds 250", self.wrapper)
        self.assertIn("AddSeconds(15)", self.wrapper)

    def test_failure_markers_override_a_pass_marker(self) -> None:
        self.assertIn('Contains("result=Failed")', self.wrapper)
        self.assertIn('Contains("StackOverflowException:")', self.wrapper)


if __name__ == "__main__":
    unittest.main()
