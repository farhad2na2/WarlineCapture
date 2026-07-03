param(
    [Parameter(Mandatory = $false)]
    [string] $UnityVersion,

    [Parameter(Mandatory = $false)]
    [string] $PreferredPath
)

$ErrorActionPreference = "Stop"

function Add-Candidate {
    param(
        [Parameter(Mandatory = $false)]
        [System.Collections.Generic.List[string]] $Candidates,

        [Parameter(Mandatory = $false)]
        [string] $Path
    )

    if ($null -eq $Candidates) {
        return
    }

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
    Add-Candidate -Candidates $unityVersions -Path "6000.$($Matches[1]).$($Matches[2])"
    Add-Candidate -Candidates $unityVersions -Path "6000.$($Matches[1]).$($Matches[2])$suffix"
}

if ($UnityVersion -match '^6000\.(\d+)\.(\d+)(f\d+)?$') {
    $suffix = if ([string]::IsNullOrWhiteSpace($Matches[3])) { "" } else { $Matches[3] }
    Add-Candidate -Candidates $unityVersions -Path "6000.$($Matches[1]).$($Matches[2])"
    Add-Candidate -Candidates $unityVersions -Path "6.$($Matches[1]).$($Matches[2])"
    if (-not [string]::IsNullOrWhiteSpace($suffix)) {
        Add-Candidate -Candidates $unityVersions -Path "6.$($Matches[1]).$($Matches[2])$suffix"
    }
}

if (-not [string]::IsNullOrWhiteSpace($UnityVersion)) {
    foreach ($version in $unityVersions) {
        foreach ($root in $programFiles) {
            Add-Candidate -Candidates $candidates -Path (Join-Path $root "Unity\Hub\Editor\$version\Editor\Unity.exe")
            Add-Candidate -Candidates $candidates -Path (Join-Path $root "Unity\$version\Editor\Unity.exe")
            Add-Candidate -Candidates $candidates -Path (Join-Path $root "Unity $version\Editor\Unity.exe")
            Add-Candidate -Candidates $candidates -Path (Join-Path $root "Unity-$version\Editor\Unity.exe")
        }
    }
}

foreach ($root in $programFiles) {
    foreach ($unityRoot in @((Join-Path $root "Unity"), (Join-Path $root "Unity\Hub\Editor"))) {
        if (-not [System.IO.Directory]::Exists($unityRoot)) {
            continue
        }

        Get-ChildItem -LiteralPath $unityRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object {
                [string]::IsNullOrWhiteSpace($UnityVersion) -or
                $unityVersions.Contains($_.Name) -or
                $_.Name -like "*$UnityVersion*"
            } |
            ForEach-Object {
                Add-Candidate -Candidates $candidates -Path (Join-Path $_.FullName "Editor\Unity.exe")
            }
    }
}

foreach ($candidate in $candidates) {
    if ([System.IO.File]::Exists($candidate)) {
        $resolved = [System.IO.Path]::GetFullPath($candidate)
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

Write-Error $message
exit 1
