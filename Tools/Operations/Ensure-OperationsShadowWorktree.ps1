# Creates or refreshes the Operations shadow worktree.
# Git metadata may be read from the shared repo. Unity must never open that repo.
[CmdletBinding()]
param(
    [string] $ShadowPath = "D:\Projects\WarlineCapture-Operations",
    [string] $SharedPath = "D:\Projects\WarlineCapture",
    [string] $Branch = "cursor/operations-p0-b466"
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

if ($shadow -eq $shared) {
    throw "Shadow path must not be the shared checkout $shared."
}

if ($shadow.StartsWith($shared + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Shadow path must not live inside the shared checkout $shared."
}

Write-Host "[OperationsShadow] shared_git_source=$shared (git only; do not open Unity here)"
Write-Host "[OperationsShadow] shadow_project=$shadow"
Write-Host "[OperationsShadow] branch=$Branch"

if (-not (Test-Path -LiteralPath (Join-Path $shared ".git"))) {
    throw "Shared git checkout was not found at $shared. This script only reads git metadata from that path; it does not open Unity there."
}

$sharedLibrary = Join-Path $shared "Library"
if (Test-Path -LiteralPath $sharedLibrary) {
    Write-Host "[OperationsShadow] leaving_shared_library_untouched=$sharedLibrary"
}

function Invoke-Git {
    param([string[]] $GitArgs)
    & git @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git $($GitArgs -join ' ') failed."
    }
}

Push-Location $shared
try {
    Invoke-Git @("fetch", "origin", $Branch)

    if (Test-Path -LiteralPath $shadow) {
        if (-not (Test-Path -LiteralPath (Join-Path $shadow ".git"))) {
            throw "Shadow path $shadow exists but is not a git worktree. Refusing to overwrite it."
        }

        Invoke-Git @("-C", $shadow, "fetch", "origin", $Branch)
        Invoke-Git @("-C", $shadow, "checkout", "--detach", "origin/$Branch")
        Write-Host "[OperationsShadow] refreshed_detached_at=origin/$Branch"
    }
    else {
        try {
            Invoke-Git @("worktree", "add", "-B", $Branch, $shadow, "origin/$Branch")
        }
        catch {
            Write-Host "[OperationsShadow] named_branch_busy=true; adding detached worktree at origin/$Branch"
            Invoke-Git @("worktree", "add", "--detach", $shadow, "origin/$Branch")
        }
    }
}
finally {
    Pop-Location
}

$sharedLock = Join-Path $shared "Temp\UnityLockfile"
if (Test-Path -LiteralPath $sharedLock) {
    Write-Host "[OperationsShadow] shared_unity_lock_present=true (ignored; do not open or wait on the shared Editor)"
}

Write-Host "[OperationsShadow] result=Ready project=$shadow"
Write-Host "[OperationsShadow] Programmer 2 validates only this shadow project. Do not open $shared."
