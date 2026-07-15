param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $GitCommit,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string] $ApkPath,

    [Parameter(Mandatory = $true)]
    [string] $BuildReportPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$contractPath = Join-Path $OutputDirectory "AndroidReleasePerformanceContract.json"
$logPath = Join-Path $OutputDirectory "AndroidReleasePerformanceContract.log"
$gatePath = Join-Path $ProjectPath "Tools/CI/android_release_performance_gate.py"
$profilePath = Join-Path $ProjectPath "Tools/CI/android_release_30fps_reference_device_profile.json"
$schemaPath = Join-Path $ProjectPath "Tools/CI/android_release_performance_evidence.schema.json"
$expectedBuildReportPath = Join-Path $ProjectPath "Design/AgentReports/architecture_performance_android_apk_build_report.json"
$requiredPassMarker = "[APH-804 AndroidReleaseGate] result=ContractGenerated"

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Remove-Item -LiteralPath $contractPath -Force -ErrorAction Ignore
Remove-Item -LiteralPath $logPath -Force -ErrorAction Ignore

function Write-ContractLog {
    param([Parameter(Mandatory = $true)][string] $Message)

    $Message | Add-Content -LiteralPath $logPath -Encoding UTF8
    Write-Host $Message
}

function Assert-NonEmptyFile {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "[APH-804 CI Contract] Required input or output was not created: $Path"
    }
    if ((Get-Item -LiteralPath $Path).Length -le 0) {
        throw "[APH-804 CI Contract] Required input or output is empty: $Path"
    }
}

function Invoke-PythonStep {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string[]] $PythonArguments
    )

    Write-ContractLog "[APH-804 CI Contract] Starting $Label."
    # unittest writes its normal progress and summary to stderr. In Windows
    # PowerShell 5.1, ErrorActionPreference=Stop otherwise turns that successful
    # native-process output into a terminating NativeCommandError.
    $savedErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = @(& $pythonExecutable @pythonPrefixArguments @PythonArguments 2>&1)
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedErrorActionPreference
    }

    foreach ($line in $output) {
        Write-ContractLog ([string] $line)
    }
    if ($exitCode -ne 0) {
        throw "[APH-804 CI Contract] $Label failed with exit code $exitCode."
    }
    return ,$output
}

try {
    Write-ContractLog "[APH-804 CI Contract] Binding the release contract to the built artifact."

    if ($GitCommit -cnotmatch '^[0-9a-f]{40}$') {
        throw "[APH-804 CI Contract] GIT_COMMIT must be an exact lowercase 40-hex revision."
    }

    foreach ($requiredInput in @($gatePath, $profilePath, $schemaPath, $ApkPath, $BuildReportPath)) {
        Assert-NonEmptyFile $requiredInput
    }

    $resolvedProjectPath = [IO.Path]::GetFullPath($ProjectPath)
    $resolvedApkPath = [IO.Path]::GetFullPath($ApkPath)
    $resolvedBuildReportPath = [IO.Path]::GetFullPath($BuildReportPath)
    if ($resolvedBuildReportPath -cne [IO.Path]::GetFullPath($expectedBuildReportPath)) {
        throw "[APH-804 CI Contract] Build report path must be the canonical Android APK build report."
    }

    try {
        $profile = Get-Content -LiteralPath $profilePath -Raw | ConvertFrom-Json
        $schema = Get-Content -LiteralPath $schemaPath -Raw | ConvertFrom-Json
        $buildReport = Get-Content -LiteralPath $BuildReportPath -Raw | ConvertFrom-Json
    } catch {
        throw "[APH-804 CI Contract] Profile, schema, or build report JSON could not be parsed: $($_.Exception.Message)"
    }

    if ($profile.schemaVersion -ne 1 -or
        $profile.taskId -cne "APH-804" -or
        $schema.properties.schemaVersion.const -ne 1 -or
        $schema.properties.taskId.const -cne "APH-804") {
        throw "[APH-804 CI Contract] Profile/schema identity does not match APH-804 version 1."
    }
    if ($profile.build.apkPath -cne "Build/AndroidAPK/WarlineCapture.apk" -or
        $profile.build.buildType -cne "release" -or
        $profile.build.scriptingBackend -cne "IL2CPP" -or
        $profile.build.architecture -cne "ARM64") {
        throw "[APH-804 CI Contract] Profile does not describe the required release APK."
    }

    $schemaDefinitions = $schema.PSObject.Properties['$defs'].Value
    if ($schemaDefinitions.build.properties.apkPath.const -cne $profile.build.apkPath -or
        $schemaDefinitions.build.properties.buildType.const -cne $profile.build.buildType -or
        $schemaDefinitions.build.properties.scriptingBackend.const -cne $profile.build.scriptingBackend -or
        $schemaDefinitions.build.properties.architecture.const -cne $profile.build.architecture) {
        throw "[APH-804 CI Contract] Schema build identity does not match the release profile."
    }

    $expectedApkPath = [IO.Path]::GetFullPath((Join-Path $resolvedProjectPath $profile.build.apkPath))
    if ($resolvedApkPath -cne $expectedApkPath) {
        throw "[APH-804 CI Contract] APK path does not match the exact profile artifact path."
    }

    $apkFile = Get-Item -LiteralPath $resolvedApkPath
    $apkSizeBytes = $apkFile.Length
    $apkSha256 = (Get-FileHash -LiteralPath $resolvedApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($apkSha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw "[APH-804 CI Contract] Computed APK SHA-256 is invalid."
    }

    $maximumApkSize = $profile.limits.maximumApkSizeBytes
    if ($maximumApkSize.comparison -cne "lessThanOrEqual" -or
        (($maximumApkSize.value -isnot [int]) -and ($maximumApkSize.value -isnot [long])) -or
        $maximumApkSize.value -le 0) {
        throw "[APH-804 CI Contract] Profile maximum APK byte limit is invalid."
    }
    if ($apkSizeBytes -gt $maximumApkSize.value) {
        throw "[APH-804 CI Contract] APK size $apkSizeBytes exceeds profile maximum $($maximumApkSize.value)."
    }

    if ($buildReport.schemaVersion -ne 1 -or $buildReport.taskId -cne "APH-500" -or
        $buildReport.exactCommit -cne $GitCommit -or
        ($buildReport.dirty -isnot [bool]) -or $buildReport.dirty -ne $false -or
        $buildReport.status -cne "complete" -or
        $buildReport.releaseBuildType -cne "release" -or
        $buildReport.packageType -cne "APK" -or
        $buildReport.buildTarget -cne "Android" -or
        $buildReport.scriptingBackend -cne "IL2CPP" -or
        $buildReport.targetArchitecture -cne "ARM64" -or
        ($buildReport.detailedBuildReport -isnot [bool]) -or $buildReport.detailedBuildReport -ne $true -or
        $buildReport.artifactPath -cne $profile.build.apkPath -or
        $buildReport.artifactSha256 -cne $apkSha256 -or
        $buildReport.artifactBytes -ne $apkSizeBytes) {
        throw "[APH-804 CI Contract] Build report provenance or artifact identity does not match the built release APK."
    }

    $pythonLauncher = Get-Command py -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($null -ne $pythonLauncher) {
        $pythonExecutable = $pythonLauncher.Source
        $pythonPrefixArguments = @("-3")
    } else {
        $pythonCommand = Get-Command python -CommandType Application -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($null -eq $pythonCommand) {
            throw "[APH-804 CI Contract] Python 3 was not found as 'py -3' or 'python'."
        }
        $pythonExecutable = $pythonCommand.Source
        $pythonPrefixArguments = @()
    }

    Push-Location $ProjectPath
    try {
        Invoke-PythonStep `
            -Label "APH-804 release gate unit suite" `
            -PythonArguments @("-m", "unittest", "Tools.CI.tests.test_android_release_performance_gate") |
            Out-Null

        $gateOutput = Invoke-PythonStep `
            -Label "APH-804 release artifact contract command" `
            -PythonArguments @(
                $gatePath,
                "--profile", $profilePath,
                "contract",
                "--expected-revision", $GitCommit,
                "--expected-apk-sha256", $apkSha256,
                "--output-json", $contractPath
            )
    } finally {
        Pop-Location
    }

    Assert-NonEmptyFile $contractPath
    $gateOutputText = ($gateOutput | ForEach-Object { [string] $_ }) -join [Environment]::NewLine
    if (-not $gateOutputText.Contains($requiredPassMarker)) {
        throw "[APH-804 CI Contract] Contract command output is missing exact pass marker: $requiredPassMarker"
    }

    try {
        $contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
    } catch {
        throw "[APH-804 CI Contract] Contract output is not valid JSON: $($_.Exception.Message)"
    }

    $unmetRequirements = @($contract.unmetAcceptanceRequirements)
    $expectedUnmetRequirements = @("release-mode-structured-recorder", "validated-release-device-evidence")
    if ($contract.schemaVersion -ne 1 -or
        $contract.taskId -cne "APH-804" -or
        $contract.exactCommit -cne $GitCommit -or
        $contract.apkSha256 -cne $apkSha256 -or
        ($contract.acceptanceReady -isnot [bool]) -or
        $contract.acceptanceReady -ne $false -or
        $unmetRequirements.Count -ne $expectedUnmetRequirements.Count -or
        $unmetRequirements[0] -cne $expectedUnmetRequirements[0] -or
        $unmetRequirements[1] -cne $expectedUnmetRequirements[1]) {
        throw "[APH-804 CI Contract] Contract identity or unmet release evidence requirements are invalid."
    }

    Write-ContractLog "[APH-804 CI Contract] result=Passed contract=$contractPath apkSha256=$apkSha256 apkSizeBytes=$apkSizeBytes"
} catch {
    Write-ContractLog "[APH-804 CI Contract] result=Failed reason=$($_.Exception.Message)"
    throw
}
