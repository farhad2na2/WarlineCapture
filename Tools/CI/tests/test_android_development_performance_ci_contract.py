import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
JENKINSFILE = ROOT / "Jenkinsfile.groovy"
WRAPPER = ROOT / "Tools/CI/InvokeAndroidDevelopmentPerformanceContract.ps1"


class AndroidDevelopmentPerformanceCiContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.jenkins = JENKINSFILE.read_text(encoding="utf-8")
        cls.wrapper = WRAPPER.read_text(encoding="utf-8")
        stage_start = cls.jenkins.index(
            "stage('APH-803 Android Development Performance Preflight')"
        )
        stage_end = cls.jenkins.index("stage('Resolve Unity Editor')", stage_start)
        cls.preflight_stage = cls.jenkins[stage_start:stage_end]

    def test_stage_is_unconditional_and_runs_before_unity_resolution(self) -> None:
        self.assertLess(
            self.jenkins.index("stage('APH-803 Android Development Performance Preflight')"),
            self.jenkins.index("stage('Resolve Unity Editor')"),
        )
        self.assertNotIn("when {", self.preflight_stage)
        self.assertIn("InvokeAndroidDevelopmentPerformanceContract.ps1", self.preflight_stage)
        self.assertIn('-GitCommit "$env:GIT_COMMIT"', self.preflight_stage)

    def test_wrapper_runs_the_existing_unit_suite_and_contract_command(self) -> None:
        self.assertIn("Tools.CI.tests.test_android_development_performance_gate", self.wrapper)
        self.assertIn('"contract"', self.wrapper)
        self.assertIn('"--expected-revision", $GitCommit', self.wrapper)
        self.assertIn('"--expected-apk-sha256", $placeholderApkSha256', self.wrapper)
        self.assertIn("0" * 64, self.wrapper)

    def test_python_resolution_and_nonzero_results_fail_closed(self) -> None:
        self.assertLess(
            self.wrapper.index("$installedPython = Get-ChildItem"),
            self.wrapper.index("Get-Command py"),
        )
        self.assertNotIn("Get-Command python", self.wrapper)
        self.assertIn('$pythonPrefixArguments = @("-3")', self.wrapper)
        self.assertIn("if ($exitCode -ne 0)", self.wrapper)
        self.assertIn("failed with exit code $exitCode", self.wrapper)
        self.assertIn("throw", self.wrapper)

    def test_stage_archives_contract_and_log(self) -> None:
        self.assertIn("TestResults/AndroidDevelopmentPerformanceContract.json", self.preflight_stage)
        self.assertIn("TestResults/AndroidDevelopmentPerformanceContract.log", self.preflight_stage)
        self.assertIn("archiveArtifacts", self.preflight_stage)
        self.assertIn("allowEmptyArchive: true", self.preflight_stage)

    def test_wrapper_references_gate_profile_and_schema(self) -> None:
        self.assertIn("android_development_performance_gate.py", self.wrapper)
        self.assertIn("android_reference_device_profile.json", self.wrapper)
        self.assertIn("android_development_performance_evidence.schema.json", self.wrapper)
        self.assertIn("ConvertFrom-Json", self.wrapper)

    def test_wrapper_requires_output_json_and_exact_pass_marker(self) -> None:
        self.assertIn('"--output-json", $contractPath', self.wrapper)
        self.assertIn("Assert-NonEmptyFile $contractPath", self.wrapper)
        self.assertIn("Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json", self.wrapper)
        self.assertIn(
            '$requiredPassMarker = "[APH-803 AndroidDevelopmentGate] result=ContractGenerated"',
            self.wrapper,
        )
        self.assertIn("$gateOutputText.Contains($requiredPassMarker)", self.wrapper)

    def test_wrapper_has_no_device_or_editor_invocation(self) -> None:
        self.assertNotIn("unity", self.wrapper.lower())
        self.assertNotIn("adb", self.wrapper.lower())
        self.assertNotRegex(self.wrapper, re.compile(r"(?im)^\s*(?:&|Start-Process).*\bUnity(?:\.exe)?\b"))
        self.assertNotRegex(self.wrapper, re.compile(r"(?im)^\s*(?:&|Start-Process).*\badb(?:\.exe)?\b"))


if __name__ == "__main__":
    unittest.main()
