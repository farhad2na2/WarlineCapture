param(
    [Parameter(Mandatory = $true)]
    [string] $UnityExe,

    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $LogFile,

    [Parameter(Mandatory = $false)]
    [string[]] $UnityArguments = @()
)

$ErrorActionPreference = "Stop"

$resolvedUnityExe = [Environment]::ExpandEnvironmentVariables($UnityExe.Trim())
$resolvedProjectPath = [Environment]::ExpandEnvironmentVariables($ProjectPath.Trim())
$resolvedLogFile = [Environment]::ExpandEnvironmentVariables($LogFile.Trim())

Write-Host "[UnityInvoke] UnityExe: $resolvedUnityExe"
Write-Host "[UnityInvoke] ProjectPath: $resolvedProjectPath"
Write-Host "[UnityInvoke] LogFile: $resolvedLogFile"

if (-not (Test-Path -LiteralPath $resolvedUnityExe -PathType Leaf)) {
    throw "Unity executable does not exist: $resolvedUnityExe"
}

if (-not (Test-Path -LiteralPath $resolvedProjectPath -PathType Container)) {
    throw "Unity project path does not exist: $resolvedProjectPath"
}

$logDirectory = Split-Path -Parent $resolvedLogFile
if (-not [string]::IsNullOrWhiteSpace($logDirectory) -and -not (Test-Path -LiteralPath $logDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
}

$arguments = @(
    "-batchmode",
    "-projectPath",
    $resolvedProjectPath,
    "-logFile",
    $resolvedLogFile
)

$arguments += $UnityArguments

Write-Host "[UnityInvoke] Arguments: $($arguments -join ' ')"
& $resolvedUnityExe @arguments
$exitCode = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
Write-Host "[UnityInvoke] ExitCode: $exitCode"
exit $exitCode
