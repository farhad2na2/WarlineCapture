# Runs Operations ARIA Play Mode capture for one O001–O003 Regular EN seed on the shadow project.
# Presents Ops-owned win screen, writes PNG + result.en.json (AriaWon). Watch seam not opened.
# Does NOT flip MISSION_CATALOG playable / aria_win_acceptance — Programmer 2 verifies PNGs first.
#
# IMPORTANT: Do NOT pass Unity -quit. EnterPlaymode is async; the capture runner owns process
# lifetime and calls EditorApplication.Exit(0/1) only after PNGs + result.en.json are written
# (or an explicit Failed marker is logged). The focused executeMethod validation helper always
# adds -quit and would shut down before Play Mode completes — call InvokeUnity.ps1 directly.
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
$invokeUnityScript = Join-Path $wrapperRoot "InvokeUnity.ps1"
if (-not (Test-Path -LiteralPath $resolveScript) -or -not (Test-Path -LiteralPath $invokeUnityScript)) {
    throw "Checked Unity wrappers are missing under $wrapperRoot"
}

$unityExe = & $resolveScript -UnityVersion $editorVersion
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($unityExe)) {
    throw "Could not resolve Unity $editorVersion for the shadow project."
}

$requiredPassMarker = "[OperationsAriaPlayModeCapture] result=Passed"
$failMarker = "[OperationsAriaPlayModeCapture] result=Failed"

Write-Host "[OperationsAriaPlayModeCapture] project=$shadow"
Write-Host "[OperationsAriaPlayModeCapture] mission=$folder seed=$seed"
Write-Host "[OperationsAriaPlayModeCapture] unity=$unityExe"
Write-Host "[OperationsAriaPlayModeCapture] method=$executeMethod"
Write-Host "[OperationsAriaPlayModeCapture] log=$LogFile"
Write-Host "[OperationsAriaPlayModeCapture] watch_seam=not_opened"
Write-Host "[OperationsAriaPlayModeCapture] cli_quit=omitted (capture owns EditorApplication.Exit after Play Mode)"

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
    throw "Operations ARIA Play Mode capture did not create log $LogFile."
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
    throw "Operations ARIA Play Mode capture log contains a failure marker. See $LogFile"
}

$evidenceDir = Join-Path $shadow "Design\AgentReports\Operations\host-aria-evidence\$folder\Regular\$seed"
$winPng = Join-Path $evidenceDir "win-screen.en.png"
$resultJson = Join-Path $evidenceDir "result.en.json"
$hasPassMarker = $logText.Contains($requiredPassMarker)
$hasEvidence = (Test-Path -LiteralPath $winPng) -and (Test-Path -LiteralPath $resultJson)

# GuiLicensing + EditorApplication.Exit (no -quit) often yields a null/unreadable process
# exit code in InvokeUnity.ps1 because its success markers are batchmode-oriented.
# Prefer the capture pass marker + written evidence over the wrapper exit code.
if (-not $hasPassMarker) {
    if ($unityExit -ne 0) {
        throw "Operations ARIA Play Mode capture failed for $folder seed $seed with Unity exit code $unityExit (pass marker missing)."
    }

    throw "Operations ARIA Play Mode capture log is missing required pass marker: $requiredPassMarker"
}

if (-not $hasEvidence) {
    if (-not (Test-Path -LiteralPath $winPng)) {
        throw "Expected win screen missing: $winPng"
    }
    if (-not (Test-Path -LiteralPath $resultJson)) {
        throw "Expected result JSON missing: $resultJson"
    }
}

if ($unityExit -ne 0) {
    Write-Host "[OperationsAriaPlayModeCapture] WARN: InvokeUnity exit code=$unityExit ignored because pass marker and evidence are present (EditorApplication.Exit / non-batchmode)."
}

Write-Host "[OperationsAriaPlayModeCapture] result=Passed mission=$folder seed=$seed evidence=$evidenceDir"
Write-Host "[OperationsAriaPlayModeCapture] verify win-screen PNG then flip catalog aria_win_acceptance (Programmer 2). Playable not claimed."
