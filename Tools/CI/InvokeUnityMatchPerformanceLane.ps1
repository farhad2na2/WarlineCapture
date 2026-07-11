param(
    [Parameter(Mandatory = $true)]
    [string] $UnityExe,

    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory,

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, [int]::MaxValue)]
    [int] $GcBudgetBytes = 1024,

    [Parameter(Mandatory = $false)]
    [int] $TimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

if ($GcBudgetBytes -ne 1024) {
    throw "[MatchPerformanceLane] The steady-state GC budget is fixed at 1024 bytes; received $GcBudgetBytes."
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$baselineJson = Join-Path $ProjectPath "Design/AgentReports/performance_regression_match_baseline.json"
$baselineMarkdown = Join-Path $ProjectPath "Design/AgentReports/performance_regression_match_baseline.md"
$gcReport = Join-Path $ProjectPath "Design/AgentReports/2026-06-11_perf_match-gc-callstack-capture.md"
$summaryJson = Join-Path $OutputDirectory "MatchPerformanceLaneSummary.json"
$baselineLog = Join-Path $OutputDirectory "MatchPerformanceBaseline.log"
$gcLog = Join-Path $OutputDirectory "MatchPerformanceSteadyStateGc.log"

@($baselineJson, $baselineMarkdown, $gcReport, $summaryJson, $baselineLog, $gcLog) |
    ForEach-Object { Remove-Item -LiteralPath $_ -Force -ErrorAction Ignore }

function Assert-NonEmptyArtifact {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "[MatchPerformanceLane] Required artifact was not created: $Path"
    }
    if ((Get-Item -LiteralPath $Path).Length -le 0) {
        throw "[MatchPerformanceLane] Required artifact is empty: $Path"
    }
}

# MatchGcAllocationCallstackCapture uses /private/tmp for raw profiler data on every
# platform. On Windows that resolves under the project drive root.
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    $driveRoot = [System.IO.Path]::GetPathRoot($ProjectPath)
    if (-not [string]::IsNullOrWhiteSpace($driveRoot)) {
        New-Item -ItemType Directory -Path (Join-Path $driveRoot "private/tmp") -Force | Out-Null
    }
}

& "$PSScriptRoot\InvokeUnityExecuteMethodValidation.ps1" `
    -UnityExe $UnityExe `
    -ProjectPath $ProjectPath `
    -ExecuteMethod "Game.Editor.MatchRuntimeShellSmokeValidation.RunPerformanceRegressionBaseline" `
    -LogFile $baselineLog `
    -RequiredPassMarker "[MatchRuntimeShellSmokeValidation] result=Passed [MatchRuntimeBaselineMetrics] result=Passed" `
    -TimeoutSeconds $TimeoutSeconds

Assert-NonEmptyArtifact $baselineJson
Assert-NonEmptyArtifact $baselineMarkdown

& "$PSScriptRoot\InvokeUnityExecuteMethodValidation.ps1" `
    -UnityExe $UnityExe `
    -ProjectPath $ProjectPath `
    -ExecuteMethod "Game.Editor.MatchGcAllocationCallstackCapture.RunSteadyState" `
    -LogFile $gcLog `
    -RequiredPassMarker "[MatchGcAllocationCallstackCapture] result=Passed frames=300" `
    -TimeoutSeconds $TimeoutSeconds

Assert-NonEmptyArtifact $gcReport

$validator = Join-Path $PSScriptRoot "validate_match_performance_lane.py"
$validatorArguments = @(
    $validator,
    "--baseline-json", $baselineJson,
    "--gc-report", $gcReport,
    "--expected-gc-budget-bytes", $GcBudgetBytes,
    "--output-json", $summaryJson
)

$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -ne $python) {
    & $python.Source @validatorArguments
} else {
    $pythonLauncher = Get-Command py -ErrorAction SilentlyContinue
    if ($null -eq $pythonLauncher) {
        throw "[MatchPerformanceLane] Python 3 was not found as 'python' or 'py'."
    }
    & $pythonLauncher.Source -3 @validatorArguments
}

if ($LASTEXITCODE -ne 0) {
    throw "[MatchPerformanceLane] Evidence validation failed with exit code $LASTEXITCODE."
}

Assert-NonEmptyArtifact $summaryJson

Write-Host "[MatchPerformanceLane] result=Passed gcBudgetBytes=$GcBudgetBytes summary=$summaryJson"
