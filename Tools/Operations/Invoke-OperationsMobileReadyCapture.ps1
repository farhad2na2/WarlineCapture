# Game View capture for O001–O003 mobile-ready content bound into the landed presentation shell.
# Writes PNGs under Design/AgentReports/Operations/mobile-ready-o001-o003/_Evidence/.
# Shadow project only: D:\Projects\WarlineCapture-Operations. Never D:\Projects\WarlineCapture.
#
# IMPORTANT: Do NOT pass Unity -quit. EnterPlaymode is async; the capture runner owns process
# lifetime and calls EditorApplication.Exit(0/1) only after the PNGs are written
# (or an explicit Failed marker is logged).
[CmdletBinding()]
param(
    [ValidateSet("All", "Coach", "Escort", "Result")]
    [string] $Shot = "All",

    [string] $ShadowPath = "D:\Projects\WarlineCapture-Operations",
    [string] $SharedPath = "D:\Projects\WarlineCapture",
    [string] $LogFile = "",
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
    throw "Refusing to capture on the shared checkout $shared. Use $shadow."
}

if (-not (Test-Path -LiteralPath $shadow -PathType Container)) {
    throw "Shadow project $shadow does not exist. Refresh the Operations shadow worktree onto this branch before capturing."
}

$shotKey = $Shot.ToUpperInvariant()
switch ($shotKey) {
    "COACH" {
        $executeMethod = "Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunO001Coach"
        $pngs = @(
            "o001-coach-scan.en.png",
            "o001-coach-evidence.en.png",
            "o001-coach-extract.en.png"
        )
    }
    "ESCORT" {
        $executeMethod = "Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunO002Escort"
        $pngs = @("o002-escort-chips.en.png")
    }
    "RESULT" {
        $executeMethod = "Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunResultDeltas"
        $pngs = @("result-trust-intel-heat.en.png")
    }
    default {
        $executeMethod = "Game.Tests.Editor.Operations.OperationsMobileReadyPlayModeCapture.RunAllEvidence"
        $pngs = @(
            "o001-coach-scan.en.png",
            "o001-coach-evidence.en.png",
            "o001-coach-extract.en.png",
            "o002-escort-chips.en.png",
            "result-trust-intel-heat.en.png"
        )
    }
}

if ([string]::IsNullOrWhiteSpace($LogFile)) {
    $LogFile = "$env:TEMP\operations-mobile-ready-capture-$shotKey.log"
}

$versionFile = Join-Path $shadow "ProjectSettings\ProjectVersion.txt"
if (-not (Test-Path -LiteralPath $versionFile)) {
    throw "Shadow project is missing $versionFile"
}

$editorVersion = (Select-String -LiteralPath $versionFile -Pattern "m_EditorVersion:\s*(.+)$").Matches.Groups[1].Value.Trim()
$wrapperRoot = Join-Path $shadow "Tools\CI"
$resolveScript = Join-Path $wrapperRoot "ResolveUnityEditor.ps1"
$invokeUnityScript = Join-Path $wrapperRoot "InvokeUnity.ps1"
if (-not (Test-Path -LiteralPath $resolveScript) -or -not (Test-Path -LiteralPath $invokeUnityScript)) {
    throw "Checked Unity wrappers are missing under $wrapperRoot"
}

$unityExe = & $resolveScript -UnityVersion $editorVersion
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($unityExe)) {
    throw "Could not resolve Unity $editorVersion for the shadow project."
}

$requiredPassMarker = "[OperationsMobileReadyPlayModeCapture] result=Passed"
$failMarker = "[OperationsMobileReadyPlayModeCapture] result=Failed"

Write-Host "[OperationsMobileReadyPlayModeCapture] project=$shadow"
Write-Host "[OperationsMobileReadyPlayModeCapture] shot=$shotKey"
Write-Host "[OperationsMobileReadyPlayModeCapture] unity=$unityExe"
Write-Host "[OperationsMobileReadyPlayModeCapture] method=$executeMethod"
Write-Host "[OperationsMobileReadyPlayModeCapture] log=$LogFile"
Write-Host "[OperationsMobileReadyPlayModeCapture] shell=OperationsAriaPlayModePresentation"
Write-Host "[OperationsMobileReadyPlayModeCapture] watch_seam=not_opened"
Write-Host "[OperationsMobileReadyPlayModeCapture] cli_quit=omitted (capture owns EditorApplication.Exit after Play Mode)"

$gui = $true
if ($PSBoundParameters.ContainsKey("GuiLicensing")) {
    $gui = [bool] $GuiLicensing
}

Remove-Item -LiteralPath $LogFile -Force -ErrorAction Ignore

# Omit -quit: executeMethod only starts EnterPlaymode; capture finishes asynchronously.
$unityArguments = @("-executeMethod", $executeMethod)

& $invokeUnityScript `
    -UnityExe $unityExe `
    -ProjectPath $shadow `
    -LogFile $LogFile `
    -NoProcessExit `
    -GuiLicensing:$gui `
    -TimeoutSeconds $TimeoutSeconds `
    -UnityArguments $unityArguments
$unityExit = $LASTEXITCODE

if (-not (Test-Path -LiteralPath $LogFile -PathType Leaf)) {
    throw "Operations mobile-ready capture did not create log $LogFile."
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

if ($logText.Contains($failMarker) -or
    $logText.Contains("executeMethod method $executeMethod threw exception.") -or
    $logText.Contains("StackOverflowException:")) {
    throw "Operations mobile-ready capture log contains a failure marker. See $LogFile"
}

$evidenceDir = Join-Path $shadow "Design\AgentReports\Operations\mobile-ready-o001-o003\_Evidence"
$missing = @()
foreach ($png in $pngs) {
    $path = Join-Path $evidenceDir $png
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        $missing += $path
    }
}

$hasPassMarker = $logText.Contains($requiredPassMarker)
if (-not $hasPassMarker) {
    if ($unityExit -ne 0) {
        throw "Operations mobile-ready capture failed for $shotKey with Unity exit code $unityExit (pass marker missing)."
    }

    throw "Operations mobile-ready capture log is missing required pass marker: $requiredPassMarker"
}

if ($missing.Count -gt 0) {
    throw ("Expected Game View PNG missing: " + ($missing -join "; "))
}

if ($unityExit -ne 0) {
    Write-Host "[OperationsMobileReadyPlayModeCapture] WARN: InvokeUnity exit code=$unityExit ignored because pass marker and PNGs are present (EditorApplication.Exit / non-batchmode)."
}

Write-Host "[OperationsMobileReadyPlayModeCapture] result=Passed shot=$shotKey evidence=$evidenceDir"
Write-Host "[OperationsMobileReadyPlayModeCapture] Playable / AriaWon not claimed."
