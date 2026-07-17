param(
    [Parameter(Mandatory = $true)]
    [string] $UnityExe,

    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $LogFile,

    [Parameter(Mandatory = $false)]
    [switch] $NoProcessExit,

    [Parameter(Mandatory = $false)]
    [int] $TimeoutSeconds = 0,

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

    try {
        Add-Content -LiteralPath $resolvedLogFile -Value $Message -Encoding UTF8 -ErrorAction Stop
    } catch {
        Write-Host "[UnityInvoke] WARN: Could not append invocation log: $($_.Exception.Message)"
    }
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

function Find-UnityLoggedFailure {
    param(
        [Parameter(Mandatory = $true)]
        [string] $UnityLogFile
    )

    if (-not (Test-Path -LiteralPath $UnityLogFile -PathType Leaf)) {
        return $null
    }

    try {
        $logText = [System.IO.File]::ReadAllText($UnityLogFile)
    } catch {
        Write-Host "[UnityInvoke] WARN: Could not inspect Unity log for fatal markers: $($_.Exception.Message)"
        return $null
    }

    $fatalPatterns = @(
        'executeMethod method .+ threw exception\.',
        'Application will terminate with return code [1-9][0-9]*',
        'No valid Unity Editor license found\.',
        'Aborting batchmode due to failure'
    )
    foreach ($pattern in $fatalPatterns) {
        $match = [System.Text.RegularExpressions.Regex]::Match(
            $logText,
            $pattern,
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($match.Success) {
            return $match.Value
        }
    }

    return $null
}

Write-InvocationLog "[UnityInvoke] UnityExe: $resolvedUnityExe"
Write-InvocationLog "[UnityInvoke] ProjectPath: $resolvedProjectPath"
Write-InvocationLog "[UnityInvoke] LogFile: $resolvedLogFile"
Write-InvocationLog "[UnityInvoke] TimeoutSeconds: $TimeoutSeconds"

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

$timedOut = $false
$startedAt = Get-Date
while (-not $process.HasExited) {
    if ($TimeoutSeconds -gt 0 -and ((Get-Date) - $startedAt).TotalSeconds -ge $TimeoutSeconds) {
        $timedOut = $true
        Write-InvocationLog "[UnityInvoke] ERROR: Unity timed out after $TimeoutSeconds seconds. Killing process tree for PID $($process.Id)."
        & taskkill.exe /PID $process.Id /T /F 2>&1 | ForEach-Object {
            Write-InvocationLog "[UnityInvoke] taskkill: $_"
        }
        break
    }

    Start-Sleep -Seconds 1
    $process.Refresh()
}

$process.WaitForExit()
$exitCode = if ($timedOut) { 124 } else { $process.ExitCode }
if ($null -eq $exitCode) {
    Write-InvocationLog "[UnityInvoke] ERROR: Unity exited without a readable process exit code. Failing closed."
    $exitCode = 1
} elseif ($exitCode -eq 0) {
    $loggedFailure = Find-UnityLoggedFailure -UnityLogFile $resolvedLogFile
    if (-not [string]::IsNullOrWhiteSpace($loggedFailure)) {
        Write-InvocationLog "[UnityInvoke] ERROR: Unity reported a fatal log marker despite process exit code 0: $loggedFailure"
        $exitCode = 1
    }
}
Write-InvocationLog "[UnityInvoke] ExitCode: $exitCode"
if ($NoProcessExit) {
    $global:LASTEXITCODE = $exitCode
    return
}

exit $exitCode
