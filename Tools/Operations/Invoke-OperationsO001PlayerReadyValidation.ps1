# Runs the Operations P4R O001 shared-launch marker against the shadow project only.
# Passing it is not a player-ready certification.
# Design-APPROVED O001–O003 and historical Ops AriaWon captures are WIP/foundation.
# Programmer 2 records Manual and Regular EN visible-control evidence after this passes.
[CmdletBinding()]
param(
    [string] $ShadowPath = "D:\Projects\WarlineCapture-Operations",
    [string] $SharedPath = "D:\Projects\WarlineCapture",
    [string] $LogFile = "$env:TEMP\operations-o001-player-ready.log",
    [int] $TimeoutSeconds = 900,
    [switch] $GuiLicensing
)

$ErrorActionPreference = "Stop"

function Normalize-WindowsProjectPath([string] $PathValue) {
    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        return ""
    }

    $unified = $PathValue.Trim().Replace("/", "\").TrimEnd("\")
    if ($unified.Length -ge 2 -and $unified[1] -eq ":") {
        return ([char]::ToUpperInvariant($unified[0])).ToString() + $unified.Substring(1)
    }

    return $unified
}

$shadow = Normalize-WindowsProjectPath $ShadowPath
$shared = Normalize-WindowsProjectPath $SharedPath

if ($shadow -eq $shared -or $shadow.StartsWith($shared + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to validate the shared checkout $shared. Use $shadow."
}

if (-not (Test-Path -LiteralPath $shadow -PathType Container)) {
    throw "Shadow project $shadow does not exist. Refresh the Operations shadow worktree onto this branch before validating."
}

$sharedLibrary = Join-Path $shared "Library"
if (Test-Path -LiteralPath $sharedLibrary) {
    Write-Host "[OperationsO001PlayerReady] leaving_shared_library_untouched=$sharedLibrary"
}

$versionFile = Join-Path $shadow "ProjectSettings\ProjectVersion.txt"
if (-not (Test-Path -LiteralPath $versionFile)) {
    throw "Shadow project is missing $versionFile"
}

$editorVersion = (Select-String -LiteralPath $versionFile -Pattern "m_EditorVersion:\s*(.+)$").Matches.Groups[1].Value.Trim()
$wrapperRoot = Join-Path $shadow "Tools\CI"
$resolveScript = Join-Path $wrapperRoot "ResolveUnityEditor.ps1"
$validateScript = Join-Path $wrapperRoot "InvokeUnityExecuteMethodValidation.ps1"
if (-not (Test-Path -LiteralPath $resolveScript) -or -not (Test-Path -LiteralPath $validateScript)) {
    throw "Checked Unity wrappers are missing under $wrapperRoot"
}

$unityExe = & $resolveScript -UnityVersion $editorVersion
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($unityExe)) {
    throw "Could not resolve Unity $editorVersion for the shadow project."
}

Write-Host "[OperationsO001PlayerReady] project=$shadow"
Write-Host "[OperationsO001PlayerReady] unity=$unityExe"
Write-Host "[OperationsO001PlayerReady] log=$LogFile"

$gui = $true
if ($PSBoundParameters.ContainsKey("GuiLicensing")) {
    $gui = [bool] $GuiLicensing
}

& $validateScript `
    -UnityExe $unityExe `
    -ProjectPath $shadow `
    -ExecuteMethod "Game.Tests.Editor.Operations.OperationsO001PlayerReadyValidation.RunFocusedValidation" `
    -LogFile $LogFile `
    -RequiredPassMarker "[OperationsO001PlayerShellValidation] result=Passed checks=5" `
    -GuiLicensing:$gui `
    -TimeoutSeconds $TimeoutSeconds

if ($LASTEXITCODE -ne 0) {
    throw "Operations O001 shared-launch shadow validation failed. This marker is not a player-ready certification."
}

Write-Host "[OperationsO001PlayerReady] result=Passed project=$shadow"
