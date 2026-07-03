param(
    [Parameter(Mandatory = $true)]
    [string] $UnityExe,

    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $LogFile,

    [Parameter(Mandatory = $false)]
    [switch] $NoProcessExit,

    [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true)]
    [string[]] $UnityArguments = @()
)

$ErrorActionPreference = "Stop"

$resolvedUnityExe = [Environment]::ExpandEnvironmentVariables($UnityExe.Trim())
$resolvedProjectPath = [Environment]::ExpandEnvironmentVariables($ProjectPath.Trim())
$resolvedLogFile = [Environment]::ExpandEnvironmentVariables($LogFile.Trim())

$logDirectory = Split-Path -Parent $resolvedLogFile
if (-not [string]::IsNullOrWhiteSpace($logDirectory) -and -not (Test-Path -LiteralPath $logDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

function Write-InvocationLog {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Message
    )

    Write-Host $Message

    if ([string]::IsNullOrWhiteSpace($resolvedLogFile)) {
        return
    }

    Add-Content -LiteralPath $resolvedLogFile -Value $Message -Encoding UTF8
}

Write-InvocationLog "[UnityInvoke] UnityExe: $resolvedUnityExe"
Write-InvocationLog "[UnityInvoke] ProjectPath: $resolvedProjectPath"
Write-InvocationLog "[UnityInvoke] LogFile: $resolvedLogFile"

if (-not (Test-Path -LiteralPath $resolvedUnityExe -PathType Leaf)) {
    Write-InvocationLog "[UnityInvoke] ERROR: Unity executable does not exist: $resolvedUnityExe"
    throw "Unity executable does not exist: $resolvedUnityExe"
}

if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Container)) {
    Write-InvocationLog "[UnityInvoke] ERROR: Unity project path does not exist: $resolvedProjectPath"
    throw "Unity project path does not exist: $resolvedProjectPath"
}

$arguments = @(
    "-batchmode",
    "-projectPath",
    $resolvedProjectPath,
    "-logFile",
    $resolvedLogFile
)

$arguments += $UnityArguments

Write-InvocationLog "[UnityInvoke] Arguments: $($arguments -join ' ')"
& $resolvedUnityExe @arguments
$exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
Write-InvocationLog "[UnityInvoke] ExitCode: $exitCode"
if ($NoProcessExit) {
    $global:LASTEXITCODE = $exitCode
    return
}

exit $exitCode
