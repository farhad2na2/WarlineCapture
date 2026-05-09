param(
    [Parameter(Mandatory = $true)]
    [string] $TaskDir,

    [Parameter(Mandatory = $false)]
    [string] $ProjectPath = $env:PROJECT_PATH,

    [Parameter(Mandatory = $false)]
    [string] $BuildLog = $env:BUILD_LOG
)

$ErrorActionPreference = "Stop"

function Copy-IfPresent {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Source,

        [Parameter(Mandatory = $true)]
        [string] $Destination
    )

    if ([string]::IsNullOrWhiteSpace($Source) -or -not (Test-Path -LiteralPath $Source)) {
        return
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

New-Item -ItemType Directory -Path $TaskDir -Force | Out-Null

$safeJobName = ($env:JOB_NAME -replace "[^A-Za-z0-9_.-]", "_")
if ([string]::IsNullOrWhiteSpace($safeJobName)) {
    $safeJobName = "jenkins"
}

$buildNumber = $env:BUILD_NUMBER
if ([string]::IsNullOrWhiteSpace($buildNumber)) {
    $buildNumber = "unknown"
}

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$bundleName = "jenkins-failure-$safeJobName-$buildNumber-$stamp"
$bundleDir = Join-Path $TaskDir $bundleName
$logsDir = Join-Path $bundleDir "logs"
New-Item -ItemType Directory -Path $logsDir -Force | Out-Null

Copy-IfPresent -Source $BuildLog -Destination (Join-Path $logsDir "build.log")

if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
    $testResultsDir = Join-Path $ProjectPath "TestResults"
    if (Test-Path -LiteralPath $testResultsDir) {
        Get-ChildItem -LiteralPath $testResultsDir -Force -ErrorAction SilentlyContinue |
            Copy-Item -Destination $logsDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$metadata = [ordered]@{
    job_name = $env:JOB_NAME
    build_number = $env:BUILD_NUMBER
    build_url = $env:BUILD_URL
    branch_name = $env:BRANCH_NAME
    git_branch = $env:GIT_BRANCH
    git_commit = $env:GIT_COMMIT
    node_name = $env:NODE_NAME
    workspace = $env:WORKSPACE
    project_path = $ProjectPath
    build_log = $BuildLog
    created_at = (Get-Date).ToString("o")
}

$metadata | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $bundleDir "metadata.json") -Encoding UTF8

$task = @"
Jenkins build failed for WarlineCapture. Investigate the failure from the copied Jenkins/Unity logs, identify the root cause, and make a scoped fix if it is safe.

Use this workflow:
1. Read metadata.json and the logs in the failure bundle.
2. Prefer fixing source/config/test issues over changing CI unless the log clearly points at CI.
3. Run the smallest relevant Unity validation or tests available from this checkout.
4. Summarize root cause, changed files, validation, and remaining risk.

Jenkins job: $($env:JOB_NAME)
Build number: $($env:BUILD_NUMBER)
Build URL: $($env:BUILD_URL)
Git commit: $($env:GIT_COMMIT)
"@

$queueItem = [ordered]@{
    task = $task
    bundle_dir_name = $bundleName
    created_at = (Get-Date).ToString("o")
}

$queuePath = Join-Path $TaskDir "$bundleName.json"
$queueItem | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $queuePath -Encoding UTF8

Write-Host "[CodexQueue] Queued Codex Jenkins failure task: $queuePath"
