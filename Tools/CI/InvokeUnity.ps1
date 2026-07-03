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
if ([string]::IsNullOrWhiteSpace($logDirectory)) {
    $logDirectory = (Get-Location).Path
}

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

function ConvertTo-ProcessArgument {
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [string] $Argument
    )

    if ($null -eq $Argument) {
        return '""'
    }

    if ($Argument -notmatch '[\s"]') {
        return $Argument
    }

    return '"' + ($Argument -replace '"', '\"') + '"'
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

$argumentLine = ($arguments | ForEach-Object { ConvertTo-ProcessArgument $_ }) -join " "
$logBaseName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedLogFile)
$stdoutLogFile = Join-Path $logDirectory "$logBaseName.stdout.log"
$stderrLogFile = Join-Path $logDirectory "$logBaseName.stderr.log"
Remove-Item -LiteralPath $stdoutLogFile -Force -ErrorAction Ignore
Remove-Item -LiteralPath $stderrLogFile -Force -ErrorAction Ignore

Write-InvocationLog "[UnityInvoke] Arguments: $($arguments -join ' ')"
Write-InvocationLog "[UnityInvoke] ProcessArguments: $argumentLine"
Write-InvocationLog "[UnityInvoke] StdoutLog: $stdoutLogFile"
Write-InvocationLog "[UnityInvoke] StderrLog: $stderrLogFile"

$process = Start-Process `
    -FilePath $resolvedUnityExe `
    -ArgumentList $argumentLine `
    -RedirectStandardOutput $stdoutLogFile `
    -RedirectStandardError $stderrLogFile `
    -PassThru

while (-not $process.HasExited) {
    Start-Sleep -Seconds 1
    $process.Refresh()
}

$process.WaitForExit()
$exitCode = if ($null -eq $process.ExitCode) { 0 } else { $process.ExitCode }
Write-InvocationLog "[UnityInvoke] ExitCode: $exitCode"
if ($NoProcessExit) {
    $global:LASTEXITCODE = $exitCode
    return
}

exit $exitCode
