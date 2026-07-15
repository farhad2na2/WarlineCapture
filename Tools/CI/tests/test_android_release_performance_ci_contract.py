import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
JENKINSFILE = ROOT / "Jenkinsfile.groovy"
WRAPPER = ROOT / "Tools/CI/InvokeAndroidReleasePerformanceContract.ps1"
BUILD_SCRIPT = ROOT / "Assets/Game/Scripts/Editor/BuildScript.cs"
ARCHITECTURE_RUNNER = ROOT / "Assets/Tests/Editor/ArchitectureHardeningCloseoutValidationRunner.cs"
BUILD_STAGE = "Build Android APK"
CONTRACT_STAGE = "APH-804 Android Release Artifact Contract"
DEPLOY_STAGE = "Deploy Android APK"
BUILD_CONDITION = (
    "params.BUILD_ANDROID_APK == true || "
    "params.BUILD_ANDROID_APK?.toString()?.equalsIgnoreCase('true')"
)


class AndroidReleasePerformanceCiContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.jenkins = JENKINSFILE.read_text(encoding="utf-8")
        cls.wrapper = WRAPPER.read_text(encoding="utf-8")
        cls.build_script = BUILD_SCRIPT.read_text(encoding="utf-8")
        cls.architecture_runner = ARCHITECTURE_RUNNER.read_text(encoding="utf-8")
        checkout_start = cls.jenkins.index("stage('Checkout Unity Project')")
        checkout_end = cls.jenkins.index("stage('APH-803 Android Development Performance Preflight')", checkout_start)
        cls.checkout_stage = cls.jenkins[checkout_start:checkout_end]
        smoke_start = cls.jenkins.index("stage('Run Unity EditMode Smoke Tests')")
        smoke_end = cls.jenkins.index("stage('Run Architecture Validations')", smoke_start)
        cls.smoke_stage = cls.jenkins[smoke_start:smoke_end]
        architecture_start = smoke_end
        architecture_end = cls.jenkins.index("stage('Run Editor Match Performance Lane')", architecture_start)
        cls.architecture_stage = cls.jenkins[architecture_start:architecture_end]
        contract_start = cls.jenkins.index(f"stage('{CONTRACT_STAGE}')")
        contract_end = cls.jenkins.index(f"stage('{DEPLOY_STAGE}')", contract_start)
        cls.contract_stage = cls.jenkins[contract_start:contract_end]

    def test_stage_immediately_follows_apk_build_and_uses_same_gate(self) -> None:
        stage_names = re.findall(r"stage\('([^']+)'\)", self.jenkins)
        build_index = stage_names.index(BUILD_STAGE)
        self.assertEqual(CONTRACT_STAGE, stage_names[build_index + 1])
        self.assertEqual(1, self.contract_stage.count(BUILD_CONDITION))

        build_start = self.jenkins.index(f"stage('{BUILD_STAGE}')")
        contract_start = self.jenkins.index(f"stage('{CONTRACT_STAGE}')", build_start)
        build_stage = self.jenkins[build_start:contract_start]
        self.assertEqual(1, build_stage.count(BUILD_CONDITION))

    def test_checkout_preserves_versioned_unity_library_for_incremental_runs(self) -> None:
        self.assertNotIn("deleteDir()", self.checkout_stage)
        self.assertIn('if not exist "%PROJECT_PATH%\\\\.git"', self.checkout_stage)
        self.assertIn("git reset --hard FETCH_HEAD", self.checkout_stage)
        self.assertIn("git clean -ffdx -e Library/", self.checkout_stage)
        self.assertIn('Join-Path $libraryPath ".jenkins-unity-version"', self.checkout_stage)
        self.assertIn("CLEAN_ANDROID_BUILD_RESOLVED", self.checkout_stage)
        self.assertIn("TimerTrigger$TimerTriggerCause", self.checkout_stage)

    def test_android_lane_stays_on_target_and_defaults_to_incremental_player_builds(self) -> None:
        self.assertIn('"-buildTarget", "Android", "-runTests"', self.smoke_stage)
        self.assertEqual(1, self.architecture_stage.count("InvokeUnityExecuteMethodValidation.ps1"))
        self.assertIn('-BuildTarget "Android"', self.architecture_stage)
        self.assertIn("RunJenkinsArchitectureValidation", self.architecture_stage)
        self.assertIn("[JenkinsArchitectureValidation] result=Passed tests=41", self.architecture_runner)
        self.assertIn("ReleaseAndroidBuildOptions =\n            BuildOptions.DetailedBuildReport;", self.build_script)
        self.assertIn("CleanReleaseAndroidBuildOptions", self.build_script)
        self.assertIn('string.Equals(value, "-cleanBuild"', self.build_script)
        self.assertIn('$unityArguments += "-cleanBuild"', self.jenkins)
        self.assertGreaterEqual(self.jenkins.count('"-buildTarget", "Android"'), 3)

    def test_stage_passes_revision_output_apk_and_build_report(self) -> None:
        expected_arguments = (
            '-ProjectPath "$env:PROJECT_PATH"',
            '-GitCommit "$env:GIT_COMMIT"',
            '-OutputDirectory "$env:PROJECT_PATH\\\\TestResults"',
            '-ApkPath "$env:PROJECT_PATH\\\\Build\\\\AndroidAPK\\\\WarlineCapture.apk"',
            '-BuildReportPath "$env:PROJECT_PATH\\\\Design\\\\AgentReports\\\\architecture_performance_android_apk_build_report.json"',
        )
        self.assertIn("InvokeAndroidReleasePerformanceContract.ps1", self.contract_stage)
        for argument in expected_arguments:
            with self.subTest(argument=argument):
                self.assertIn(argument, self.contract_stage)

    def test_stage_always_archives_contract_and_log(self) -> None:
        self.assertIn("post {", self.contract_stage)
        self.assertIn("always {", self.contract_stage)
        self.assertIn("TestResults/AndroidReleasePerformanceContract.json", self.contract_stage)
        self.assertIn("TestResults/AndroidReleasePerformanceContract.log", self.contract_stage)
        self.assertIn("allowEmptyArchive: true", self.contract_stage)

    def test_wrapper_fail_closes_inputs_revision_and_json(self) -> None:
        self.assertIn("'^[0-9a-f]{40}$'", self.wrapper)
        self.assertIn("$gatePath, $profilePath, $schemaPath, $ApkPath, $BuildReportPath", self.wrapper)
        self.assertIn("Assert-NonEmptyFile $requiredInput", self.wrapper)
        for json_path in ("$profilePath", "$schemaPath", "$BuildReportPath", "$contractPath"):
            with self.subTest(json_path=json_path):
                self.assertIn(
                    f"Get-Content -LiteralPath {json_path} -Raw | ConvertFrom-Json",
                    self.wrapper,
                )
        self.assertIn("Profile/schema identity does not match APH-804 version 1", self.wrapper)
        self.assertIn("Build report path must be the canonical Android APK build report", self.wrapper)

    def test_wrapper_binds_report_provenance_hash_and_size(self) -> None:
        required_report_checks = (
            '$buildReport.exactCommit -cne $GitCommit',
            '$buildReport.dirty -ne $false',
            '$buildReport.status -cne "complete"',
            '$buildReport.releaseBuildType -cne "release"',
            '$buildReport.packageType -cne "APK"',
            '$buildReport.buildTarget -cne "Android"',
            '$buildReport.scriptingBackend -cne "IL2CPP"',
            '$buildReport.targetArchitecture -cne "ARM64"',
            '$buildReport.detailedBuildReport -ne $true',
            '$buildReport.artifactPath -cne $profile.build.apkPath',
            '$buildReport.artifactSha256 -cne $apkSha256',
            '$buildReport.artifactBytes -ne $apkSizeBytes',
        )
        for check in required_report_checks:
            with self.subTest(check=check):
                self.assertIn(check, self.wrapper)

    def test_wrapper_uses_actual_apk_hash_size_and_profile_maximum(self) -> None:
        self.assertIn("Get-FileHash -LiteralPath $resolvedApkPath -Algorithm SHA256", self.wrapper)
        self.assertIn("$apkSizeBytes = $apkFile.Length", self.wrapper)
        self.assertIn("$apkSizeBytes -gt $maximumApkSize.value", self.wrapper)
        self.assertIn('$maximumApkSize.comparison -cne "lessThanOrEqual"', self.wrapper)
        self.assertIn('"--expected-apk-sha256", $apkSha256', self.wrapper)
        self.assertNotIn("placeholder", self.wrapper.lower())
        self.assertNotIn("0" * 64, self.wrapper)

    def test_wrapper_runs_release_suite_and_requires_exact_contract_output(self) -> None:
        self.assertIn("Tools.CI.tests.test_android_release_performance_gate", self.wrapper)
        self.assertIn('"contract"', self.wrapper)
        self.assertIn('"--expected-revision", $GitCommit', self.wrapper)
        self.assertIn('"--output-json", $contractPath', self.wrapper)
        self.assertIn(
            '$requiredPassMarker = "[APH-804 AndroidReleaseGate] result=ContractGenerated"',
            self.wrapper,
        )
        self.assertIn("$gateOutputText.Contains($requiredPassMarker)", self.wrapper)
        self.assertIn("$contract.acceptanceReady -ne $false", self.wrapper)
        self.assertIn("release-mode-structured-recorder", self.wrapper)
        self.assertIn("validated-release-device-evidence", self.wrapper)

    def test_python_resolution_and_fail_closed_exit_handling(self) -> None:
        self.assertLess(self.wrapper.index("Get-Command py"), self.wrapper.index("Get-Command python"))
        self.assertIn('$pythonPrefixArguments = @("-3")', self.wrapper)
        self.assertIn("$savedErrorActionPreference = $ErrorActionPreference", self.wrapper)
        self.assertIn('$ErrorActionPreference = "Continue"', self.wrapper)
        self.assertIn("$ErrorActionPreference = $savedErrorActionPreference", self.wrapper)
        self.assertIn("if ($exitCode -ne 0)", self.wrapper)
        self.assertIn("throw", self.wrapper)

    def test_wrapper_does_not_invoke_editor_or_device_tools(self) -> None:
        self.assertNotIn("unity", self.wrapper.lower())
        self.assertNotIn("adb", self.wrapper.lower())
        self.assertNotRegex(self.wrapper, re.compile(r"(?im)^\s*(?:&|Start-Process).*\bUnity(?:\.exe)?\b"))
        self.assertNotRegex(self.wrapper, re.compile(r"(?im)^\s*(?:&|Start-Process).*\badb(?:\.exe)?\b"))


if __name__ == "__main__":
    unittest.main()
