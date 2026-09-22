# Runs Operations ARIA Play Mode capture for one O001–O003 Regular EN seed on the shadow project.
# Presents Ops-owned win screen, writes PNG + result.en.json (AriaWon). Watch seam not opened.
# Does NOT flip MISSION_CATALOG playable / aria_win_acceptance — Programmer 2 verifies PNGs first.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("O001", "O002", "O003", "operation.o001", "operation.o002", "operation.o003")]
    [string] $Mission,

    [string] $ShadowPath = "D:\Projects\WarlineCapture-Operations",
    [string] $SharedPath = "D:\Projects\WarlineCapture",
    [string] $LogFile = "",
    [int] $TimeoutSeconds = 1200,
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
    throw "Refusing to capture on the shared checkout $shared. Use $shadow."
}

if (-not (Test-Path -LiteralPath $shadow -PathType Container)) {
    throw "Shadow project $shadow does not exist. Refresh the Operations shadow worktree onto this branch before capturing."
}

$missionKey = $Mission.ToUpperInvariant()
switch ($missionKey) {
    "O001" { $executeMethod = "Game.Tests.Editor.Operations.OperationsAriaPlayModeCapture.RunO001RegularEn"; $seed = 1102; $folder = "operation.o001" }
    "OPERATION.O001" { $executeMethod = "Game.Tests.Editor.Operations.OperationsAriaPlayModeCapture.RunO001RegularEn"; $seed = 1102; $folder = "operation.o001" }
    "O002" { $executeMethod = "Game.Tests.Editor.Operations.OperationsAriaPlayModeCapture.RunO002RegularEn"; $seed = 1103; $folder = "operation.o002" }
    "OPERATION.O002" { $executeMethod = "Game.Tests.Editor.Operations.OperationsAriaPlayModeCapture.RunO002RegularEn"; $seed = 1103; $folder = "operation.o002" }
    "O003" { $executeMethod = "Game.Tests.Editor.Operations.OperationsAriaPlayModeCapture.RunO003RegularEn"; $seed = 1104; $folder = "operation.o003" }
    "OPERATION.O003" { $executeMethod = "Game.Tests.Editor.Operations.OperationsAriaPlayModeCapture.RunO003RegularEn"; $seed = 1104; $folder = "operation.o003" }
    default { throw "Unsupported mission $Mission" }
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $LogFile = "$env:TEMP\operations-aria-playmode-capture-$($folder)-$seed.log"
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

Write-Host "[OperationsAriaPlayModeCapture] project=$shadow"
Write-Host "[OperationsAriaPlayModeCapture] mission=$folder seed=$seed"
Write-Host "[OperationsAriaPlayModeCapture] unity=$unityExe"
Write-Host "[OperationsAriaPlayModeCapture] method=$executeMethod"
Write-Host "[OperationsAriaPlayModeCapture] log=$LogFile"
Write-Host "[OperationsAriaPlayModeCapture] watch_seam=not_opened"

$gui = $true
if ($PSBoundParameters.ContainsKey("GuiLicensing")) {
    $gui = [bool] $GuiLicensing
}

& $validateScript `
    -UnityExe $unityExe `
    -ProjectPath $shadow `
    -ExecuteMethod $executeMethod `
    -LogFile $LogFile `
    -RequiredPassMarker "[OperationsAriaPlayModeCapture] result=Passed" `
    -GuiLicensing:$gui `
    -TimeoutSeconds $TimeoutSeconds

if ($LASTEXITCODE -ne 0) {
    throw "Operations ARIA Play Mode capture failed for $folder seed $seed."
}

$evidenceDir = Join-Path $shadow "Design\AgentReports\Operations\host-aria-evidence\$folder\Regular\$seed"
$winPng = Join-Path $evidenceDir "win-screen.en.png"
$resultJson = Join-Path $evidenceDir "result.en.json"
if (-not (Test-Path -LiteralPath $winPng)) {
    throw "Expected win screen missing: $winPng"
}
if (-not (Test-Path -LiteralPath $resultJson)) {
    throw "Expected result JSON missing: $resultJson"
}

Write-Host "[OperationsAriaPlayModeCapture] result=Passed mission=$folder seed=$seed evidence=$evidenceDir"
Write-Host "[OperationsAriaPlayModeCapture] verify win-screen PNG then flip catalog aria_win_acceptance (Programmer 2). Playable not claimed."
