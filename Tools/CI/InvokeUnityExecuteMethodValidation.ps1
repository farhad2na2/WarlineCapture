param(
    [Parameter(Mandatory = $true)]
    [string] $UnityExe,

    [Parameter(Mandatory = $true)]
    [string] $ProjectPath,

    [Parameter(Mandatory = $true)]
    [string] $ExecuteMethod,

    [Parameter(Mandatory = $true)]
    [string] $LogFile,

    [Parameter(Mandatory = $true)]
    [string] $RequiredPassMarker,

    [Parameter(Mandatory = $false)]
    [string] $BuildTarget = "",

    [Parameter(Mandatory = $false)]
    [int] $TimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

Remove-Item -LiteralPath $LogFile -Force -ErrorAction Ignore

$unityArguments = @("-quit")
if (-not [string]::IsNullOrWhiteSpace($BuildTarget)) {
    $unityArguments += @("-buildTarget", $BuildTarget)
}
$unityArguments += @("-executeMethod", $ExecuteMethod)

& "$PSScriptRoot\InvokeUnity.ps1" `
    -UnityExe $UnityExe `
    -ProjectPath $ProjectPath `
    -LogFile $LogFile `
    -NoProcessExit `
    -TimeoutSeconds $TimeoutSeconds `
    -UnityArguments $unityArguments
$unityExit = $LASTEXITCODE

if ($unityExit -ne 0) {
    throw "[ArchitectureValidation] '$ExecuteMethod' failed with Unity exit code $unityExit."
}

if (-not (Test-Path -LiteralPath $LogFile -PathType Leaf)) {
    throw "[ArchitectureValidation] '$ExecuteMethod' did not create log '$LogFile'."
}

$logText = $null
$readDeadline = [DateTime]::UtcNow.AddSeconds(15)
do {
    try {
        $logText = [System.IO.File]::ReadAllText($LogFile)
    } catch [System.IO.IOException] {
        if ([DateTime]::UtcNow -ge $readDeadline) {
            throw
        }

        Start-Sleep -Milliseconds 250
    }
} while ($null -eq $logText)

if ($logText.Contains("result=Failed") -or
    $logText.Contains("executeMethod method $ExecuteMethod threw exception.") -or
    $logText.Contains("StackOverflowException:")) {
    throw "[ArchitectureValidation] '$ExecuteMethod' log contains a failure marker."
}

if (-not $logText.Contains($RequiredPassMarker)) {
    throw "[ArchitectureValidation] '$ExecuteMethod' log is missing required pass marker: $RequiredPassMarker"
}

Write-Host "[ArchitectureValidation] method=$ExecuteMethod result=Passed marker=$RequiredPassMarker"
