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
    [int] $TimeoutSeconds = 900
)

$ErrorActionPreference = "Stop"

Remove-Item -LiteralPath $LogFile -Force -ErrorAction Ignore

& "$PSScriptRoot\InvokeUnity.ps1" `
    -UnityExe $UnityExe `
    -ProjectPath $ProjectPath `
    -LogFile $LogFile `
    -NoProcessExit `
    -TimeoutSeconds $TimeoutSeconds `
    -UnityArguments @("-quit", "-executeMethod", $ExecuteMethod)
$unityExit = $LASTEXITCODE

if ($unityExit -ne 0) {
    throw "[ArchitectureValidation] '$ExecuteMethod' failed with Unity exit code $unityExit."
}

if (-not (Test-Path -LiteralPath $LogFile -PathType Leaf)) {
    throw "[ArchitectureValidation] '$ExecuteMethod' did not create log '$LogFile'."
}

$logText = [System.IO.File]::ReadAllText($LogFile)
if (-not $logText.Contains($RequiredPassMarker)) {
    throw "[ArchitectureValidation] '$ExecuteMethod' log is missing required pass marker: $RequiredPassMarker"
}

Write-Host "[ArchitectureValidation] method=$ExecuteMethod result=Passed marker=$RequiredPassMarker"
