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
    [switch] $GuiLicensing,

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
        $stream = [System.IO.File]::Open(
            $UnityLogFile,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
        try {
            $reader = [System.IO.StreamReader]::new($stream)
            try {
                $logText = $reader.ReadToEnd()
            } finally {
                $reader.Dispose()
            }
        } finally {
            $stream.Dispose()
        }
    } catch {
        Write-Host "[UnityInvoke] WARN: Could not inspect Unity log for fatal markers: $($_.Exception.Message)"
        return $null
    }

    $fatalPatterns = @(
        'executeMethod method .+ threw exception\.',
        'Application will terminate with return code [1-9][0-9]*',
        'No valid Unity Editor license found\.',
        'Licensing initialization failed after [0-9.]+s',
        'The re-connection attempt was UN-successful\.',
        'Test run completed\. Exiting with code [1-9][0-9]* \(Failed\)\.',
        'Aborting batchmode due to failure',
        'Crash!!!',
        'A crash has been intercepted by the crash handler\.'
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

function Find-UnityLoggedSuccess {
    param(
        [Parameter(Mandatory = $true)]
        [string] $UnityLogFile
    )

    if (-not (Test-Path -LiteralPath $UnityLogFile -PathType Leaf)) {
        return $null
    }

    try {
        $stream = [System.IO.File]::Open(
            $UnityLogFile,
            [System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,
            [System.IO.FileShare]::ReadWrite -bor [System.IO.FileShare]::Delete)
        try {
            $reader = [System.IO.StreamReader]::new($stream)
            try {
                $logText = $reader.ReadToEnd()
            } finally {
                $reader.Dispose()
            }
        } finally {
            $stream.Dispose()
        }
    } catch {
        Write-Host "[UnityInvoke] WARN: Could not inspect Unity log for success markers: $($_.Exception.Message)"
        return $null
    }

    $successPatterns = @(
        'Test run completed\. Exiting with code 0 \(Ok\)\. Run completed\.',
        'Test run completed\. Exiting with code 0 \(Ok\)\. All tests passed\.',
        'Exiting batchmode successfully now!',
        'Application will terminate with return code 0'
    )
    foreach ($pattern in $successPatterns) {
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

$arguments = @()
if (-not $GuiLicensing) {
    $arguments += "-batchmode"
}

$arguments += @(
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
Write-InvocationLog "[UnityInvoke] LicensingMode: $(if ($GuiLicensing) { 'gui' } else { 'batchmode' })"
Write-InvocationLog "[UnityInvoke] ProcessArguments: $argumentLine"
Write-InvocationLog "[UnityInvoke] StdoutLog: $stdoutLogFile"
Write-InvocationLog "[UnityInvoke] StderrLog: $stderrLogFile"

# Windows environment-variable names are case-insensitive, but a parent process can
# still supply both Path and PATH. Windows PowerShell's Start-Process rejects that
# duplicate environment block before Unity starts. Remove only the mixed-case Path
# alias when both entries carry the same value, retaining conventional uppercase PATH
# for child tools; fail closed if the values disagree.
$processEnvironment = [System.Environment]::GetEnvironmentVariables(
    [System.EnvironmentVariableTarget]::Process)
$pathKeys = @($processEnvironment.Keys | Where-Object { $_.ToString() -ieq "Path" })
if ($pathKeys.Count -gt 1) {
    $pathValues = @($pathKeys | ForEach-Object { [string] $processEnvironment[$_] } | Select-Object -Unique)
    if ($pathValues.Count -ne 1) {
        throw "Process environment contains conflicting Path/PATH values; refusing to launch Unity."
    }

    [System.Environment]::SetEnvironmentVariable(
        "Path",
        $null,
        [System.EnvironmentVariableTarget]::Process)
    Write-InvocationLog "[UnityInvoke] EnvironmentNormalization: removed redundant Path alias"
}

$process = Start-Process `
    -FilePath $resolvedUnityExe `
    -ArgumentList $argumentLine `
    -RedirectStandardOutput $stdoutLogFile `
    -RedirectStandardError $stderrLogFile `
    -PassThru

function Stop-UnityProcessTree {
    param(
        [Parameter(Mandatory = $true)]
        [System.Diagnostics.Process] $UnityProcess
    )

    $taskkillExe = Join-Path $env:SystemRoot "System32\taskkill.exe"
    if (-not (Test-Path -LiteralPath $taskkillExe -PathType Leaf)) {
        throw "Windows taskkill executable is missing: $taskkillExe"
    }
    & $taskkillExe /PID $UnityProcess.Id /T /F 2>&1 | ForEach-Object {
        Write-InvocationLog "[UnityInvoke] taskkill: $_"
    }
}

$timedOut = $false
$fatalLogFailure = $null
$startedAt = Get-Date
while (-not $process.HasExited) {
    $fatalLogFailure = Find-UnityLoggedFailure -UnityLogFile $resolvedLogFile
    if (-not [string]::IsNullOrWhiteSpace($fatalLogFailure)) {
        Write-InvocationLog "[UnityInvoke] ERROR: Unity reported a fatal log marker while running: $fatalLogFailure"
        Stop-UnityProcessTree -UnityProcess $process
        break
    }

    if ($TimeoutSeconds -gt 0 -and ((Get-Date) - $startedAt).TotalSeconds -ge $TimeoutSeconds) {
        $timedOut = $true
        Write-InvocationLog "[UnityInvoke] ERROR: Unity timed out after $TimeoutSeconds seconds. Killing process tree for PID $($process.Id)."
        Stop-UnityProcessTree -UnityProcess $process
        break
    }

    Start-Sleep -Seconds 1
    $process.Refresh()
}

$process.WaitForExit()
$exitCode = if ($timedOut) {
    124
} elseif (-not [string]::IsNullOrWhiteSpace($fatalLogFailure)) {
    1
} else {
    $process.ExitCode
}
if (-not $timedOut) {
    $loggedFailure = Find-UnityLoggedFailure -UnityLogFile $resolvedLogFile
    if (-not [string]::IsNullOrWhiteSpace($loggedFailure)) {
        Write-InvocationLog "[UnityInvoke] ERROR: Unity reported a fatal log marker: $loggedFailure"
        $exitCode = 1
    } elseif ($null -eq $exitCode) {
        $loggedSuccess = Find-UnityLoggedSuccess -UnityLogFile $resolvedLogFile
        if (-not [string]::IsNullOrWhiteSpace($loggedSuccess)) {
            Write-InvocationLog "[UnityInvoke] WARN: Unity process exit code was unavailable; accepting explicit success marker: $loggedSuccess"
            $exitCode = 0
        } else {
            Write-InvocationLog "[UnityInvoke] ERROR: Unity exited without a readable process exit code or explicit success marker. Failing closed."
            $exitCode = 1
        }
    }
}
Write-InvocationLog "[UnityInvoke] ExitCode: $exitCode"
if ($NoProcessExit) {
    $global:LASTEXITCODE = $exitCode
    return
}

exit $exitCode
