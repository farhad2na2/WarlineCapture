param(
    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $GitCommit,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$contractPath = Join-Path $OutputDirectory "AndroidDevelopmentPerformanceContract.json"
$logPath = Join-Path $OutputDirectory "AndroidDevelopmentPerformanceContract.log"
$gatePath = Join-Path $ProjectPath "Tools/CI/android_development_performance_gate.py"
$profilePath = Join-Path $ProjectPath "Tools/CI/android_reference_device_profile.json"
$schemaPath = Join-Path $ProjectPath "Tools/CI/android_development_performance_evidence.schema.json"
$placeholderApkSha256 = "0000000000000000000000000000000000000000000000000000000000000000"
$requiredPassMarker = "[APH-803 AndroidDevelopmentGate] result=ContractGenerated"

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
Remove-Item -LiteralPath $contractPath -Force -ErrorAction Ignore
Remove-Item -LiteralPath $logPath -Force -ErrorAction Ignore

function Write-PreflightLog {
    param([Parameter(Mandatory = $true)][string] $Message)

    $Message | Add-Content -LiteralPath $logPath -Encoding UTF8
    Write-Host $Message
}

function Assert-NonEmptyFile {
    param([Parameter(Mandatory = $true)][string] $Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "[APH-803 CI Preflight] Required output was not created: $Path"
    }
    if ((Get-Item -LiteralPath $Path).Length -le 0) {
        throw "[APH-803 CI Preflight] Required output is empty: $Path"
    }
}

function Invoke-PythonStep {
    param(
        [Parameter(Mandatory = $true)][string] $Label,
        [Parameter(Mandatory = $true)][string[]] $PythonArguments
    )

    Write-PreflightLog "[APH-803 CI Preflight] Starting $Label."
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
        Write-PreflightLog ([string] $line)
    }
    if ($exitCode -ne 0) {
        throw "[APH-803 CI Preflight] $Label failed with exit code $exitCode."
    }
    return ,$output
}

try {
    Write-PreflightLog "[APH-803 CI Preflight] Preparing offline contract validation."

    if ($GitCommit -cnotmatch '^[0-9a-f]{40}$') {
        throw "[APH-803 CI Preflight] GIT_COMMIT must be an exact lowercase 40-hex revision."
    }

    foreach ($requiredInput in @($gatePath, $profilePath, $schemaPath)) {
        Assert-NonEmptyFile $requiredInput
    }

    try {
        $schema = Get-Content -LiteralPath $schemaPath -Raw | ConvertFrom-Json
        $profile = Get-Content -LiteralPath $profilePath -Raw | ConvertFrom-Json
    } catch {
        throw "[APH-803 CI Preflight] Schema or profile JSON could not be parsed: $($_.Exception.Message)"
    }
    if ($schema.properties.schemaVersion.const -ne 1 -or
        $schema.properties.taskId.const -ne "APH-803" -or
        $profile.schemaVersion -ne 1 -or
        $profile.taskId -ne "APH-803") {
        throw "[APH-803 CI Preflight] Schema/profile identity does not match APH-803 version 1."
    }

    # Prefer an installed interpreter over the Windows Store python.exe alias, which
    # reports as an Application but exits with an error when invoked by Jenkins.
    $installedPython = Get-ChildItem `
        -Path (Join-Path $env:LOCALAPPDATA "Programs\Python") `
        -Filter "python.exe" `
        -Recurse `
        -File `
        -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\Lib\\venv\\" } |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($null -ne $installedPython) {
        $pythonExecutable = $installedPython.FullName
        $pythonPrefixArguments = @()
    } else {
        $pythonLauncher = Get-Command py -CommandType Application -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($null -eq $pythonLauncher) {
            throw "[APH-803 CI Preflight] Python 3 was not found. Install Python 3 or set up the Windows py launcher."
        }

        $pythonExecutable = $pythonLauncher.Source
        $pythonPrefixArguments = @("-3")
    }

    Push-Location $ProjectPath
    try {
        Invoke-PythonStep `
            -Label "APH-803 unit suite" `
            -PythonArguments @("-m", "unittest", "Tools.CI.tests.test_android_development_performance_gate") |
            Out-Null

        $gateOutput = Invoke-PythonStep `
            -Label "APH-803 contract command" `
            -PythonArguments @(
                $gatePath,
                "--profile", $profilePath,
                "contract",
                "--expected-revision", $GitCommit,
                "--expected-apk-sha256", $placeholderApkSha256,
                "--output-json", $contractPath
            )
    } finally {
        Pop-Location
    }

    Assert-NonEmptyFile $contractPath
    $gateOutputText = ($gateOutput | ForEach-Object { [string] $_ }) -join [Environment]::NewLine
    if (-not $gateOutputText.Contains($requiredPassMarker)) {
        throw "[APH-803 CI Preflight] Contract command output is missing exact pass marker: $requiredPassMarker"
    }

    try {
        $contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
    } catch {
        throw "[APH-803 CI Preflight] Contract output is not valid JSON: $($_.Exception.Message)"
    }
    if ($contract.schemaVersion -ne 1 -or
        $contract.taskId -ne "APH-803" -or
        $contract.exactCommit -cne $GitCommit -or
        $contract.apkSha256 -cne $placeholderApkSha256) {
        throw "[APH-803 CI Preflight] Contract output does not match the requested revision and placeholder APK hash."
    }

    Write-PreflightLog "[APH-803 CI Preflight] result=Passed contract=$contractPath"
} catch {
    Write-PreflightLog "[APH-803 CI Preflight] result=Failed reason=$($_.Exception.Message)"
    throw
}
