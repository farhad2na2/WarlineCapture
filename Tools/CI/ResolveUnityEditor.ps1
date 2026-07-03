param(
    [Parameter(Mandatory = $false)]
    [string] $UnityVersion,

    [Parameter(Mandatory = $false)]
    [string] $PreferredPath
)

$ErrorActionPreference = "Stop"

function Add-Candidate {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.Generic.List[string]] $Candidates,

        [Parameter(Mandatory = $false)]
        [string] $Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return
    }

    $expandedPath = [Environment]::ExpandEnvironmentVariables($Path.Trim())
    if (-not $Candidates.Contains($expandedPath)) {
        $Candidates.Add($expandedPath) | Out-Null
    }
}

$candidates = New-Object "System.Collections.Generic.List[string]"

Add-Candidate -Candidates $candidates -Path $env:UNITY_EXE_OVERRIDE
Add-Candidate -Candidates $candidates -Path $env:UNITY_EDITOR_PATH
Add-Candidate -Candidates $candidates -Path $env:UNITY_PATH
Add-Candidate -Candidates $candidates -Path $env:UNITY_EXE
Add-Candidate -Candidates $candidates -Path $PreferredPath

$programFiles = @(
    [Environment]::GetEnvironmentVariable("ProgramFiles"),
    [Environment]::GetEnvironmentVariable("ProgramFiles(x86)"),
    "C:\Program Files",
    "D:\Program Files"
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

$unityVersions = New-Object "System.Collections.Generic.List[string]"
Add-Candidate -Candidates $unityVersions -Path $UnityVersion

if ($UnityVersion -match '^6\.(\d+)\.(\d+)(f\d+)?$') {
    $suffix = if ([string]::IsNullOrWhiteSpace($Matches[3])) { "f1" } else { $Matches[3] }
    Add-Candidate -Candidates $unityVersions -Path "6000.$($Matches[1]).$($Matches[2])$suffix"
}

if (-not [string]::IsNullOrWhiteSpace($UnityVersion)) {
    foreach ($version in $unityVersions) {
        foreach ($root in $programFiles) {
            Add-Candidate -Candidates $candidates -Path (Join-Path $root "Unity\Hub\Editor\$version\Editor\Unity.exe")
            Add-Candidate -Candidates $candidates -Path (Join-Path $root "Unity\$version\Editor\Unity.exe")
        }
    }
}

foreach ($candidate in $candidates) {
    if (Test-Path -LiteralPath $candidate -PathType Leaf) {
        $resolved = (Get-Item -LiteralPath $candidate).FullName
        Write-Output $resolved
        exit 0
    }
}

$newline = [Environment]::NewLine
$candidateList = ($candidates | ForEach-Object { "  - $_" }) -join $newline
if ([string]::IsNullOrWhiteSpace($candidateList)) {
    $candidateList = "  - <no candidates>"
}

$message = @(
    "Unity editor executable was not found for version '$UnityVersion'.",
    "Checked:",
    $candidateList,
    "Set UNITY_EXE_OVERRIDE to the installed Unity.exe path or install the requested Unity version."
) -join $newline

throw $message
