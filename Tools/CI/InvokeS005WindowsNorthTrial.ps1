param(
    [Parameter(Mandatory=$true)][string]$UnityExe,
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$JobFile,
    [Parameter(Mandatory=$true)][string]$ClaimFile,
    [Parameter(Mandatory=$true)][string]$PreparedDirectory,
    [Parameter(Mandatory=$true)][string]$InstalledObserver,
    [Parameter(Mandatory=$true)][string]$RunDirectory,
    [Parameter(Mandatory=$false)][string]$ReviewedObserverSha256="",
    [Parameter(Mandatory=$false)][string]$Python="python",
    [Parameter(Mandatory=$false)][int]$TimeoutSeconds=2100,
    [Parameter(Mandatory=$false)][switch]$Run,
    [Parameter(Mandatory=$false)][switch]$GuiLicensing
)
$ErrorActionPreference="Stop"
function Get-S005Sha256([string]$LiteralPath){
    $stream=[IO.File]::Open($LiteralPath,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    $sha=[Security.Cryptography.SHA256]::Create()
    try{return [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant()}
    finally{$sha.Dispose();$stream.Dispose()}
}
$projectRoot=(Resolve-Path -LiteralPath $ProjectPath).Path
$preparation=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $PreparedDirectory "PREPARATION.json") | ConvertFrom-Json
$job=Get-Content -Raw -Encoding UTF8 -LiteralPath $JobFile | ConvertFrom-Json
$claim=Get-Content -Raw -Encoding UTF8 -LiteralPath $ClaimFile | ConvertFrom-Json
$runId=[string]$preparation.runId
if($runId -notmatch '^\d{4,}$' -or [int]$runId -le 1631){throw "Fresh S005 run ID required"}
if($job.catalog_id -ne "S005" -or $job.seed -ne 155926 -or $job.locale -ne "FA" -or
   $job.control_mode -ne "manual" -or $job.size -ne "Standard" -or $job.difficulty -ne "Regular"){
    throw "Job differs from exact prepared S005 configuration"
}
& rtk proxy $Python (Join-Path $PSScriptRoot "skirmish_production_queue.py") preflight --job $JobFile --project $projectRoot
if($LASTEXITCODE -ne 0){throw "Shared pinned-job preflight failed"}
$draft=Join-Path $PreparedDirectory $preparation.candidate
$draftHash=Get-S005Sha256 $draft
if($draftHash -ne $preparation.candidateSha256){throw "Prepared observer bytes changed"}
$method="S005NormalNorthFlank${runId}Observer.RunValidation"
$plan=[ordered]@{result="PreparedPlanOnly";executeMethod=$method;runId=$runId;job_id=$job.job_id;
    attempt_id=$claim.attempt_id;wrapper="Tools/CI/InvokeUnity.ps1";timeoutSeconds=$TimeoutSeconds;
    candidateSha256=$draftHash;dispatchEnabled=$false;
    pending="Independent review; installed Editor-only observer; fresh complete job source/build pin; exclusive lane"}
if(-not $Run){$plan|ConvertTo-Json -Depth 5;exit 0}
if($ReviewedObserverSha256 -ne $draftHash){throw "Independent exact observer review required"}
$installed=(Resolve-Path -LiteralPath $InstalledObserver).Path
$assetsPrefix=$projectRoot.TrimEnd('\')+'\Assets\'
if(-not $installed.StartsWith($assetsPrefix,[StringComparison]::OrdinalIgnoreCase) -or
   -not $installed.EndsWith('.cs') -or
   (Get-S005Sha256 $installed) -ne $draftHash){
    throw "Reviewed external observer must be installed as a unique Editor-only .cs before source freeze"
}
if($installed -notmatch '[\\/]Editor[\\/]'){throw "Observer installation must be Editor-only"}
$relative=$installed.Substring($projectRoot.Length+1).Replace('\','/')
if(-not @($job.source_manifest | Where-Object {$_.path -eq $relative -and $_.sha256 -eq $draftHash}).Count){
    throw "Pinned source manifest omits installed observer"
}
if($TimeoutSeconds -le 0 -or $TimeoutSeconds -gt $job.timeout_seconds){throw "Timeout exceeds pinned job"}
if($claim.token -notmatch '.+' -or $claim.attempt_id -notmatch '^attempt-' -or -not $claim.worker){throw "Queue claim ownership absent"}
$artifact=[IO.Path]::GetFullPath($claim.artifact_directory)
if([IO.Path]::GetFullPath($RunDirectory) -ne $artifact){throw "Use exact queue-allocated attempt artifact directory"}
$queueDirectory=Split-Path -Parent $artifact
$liveClaim=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $queueDirectory "claim.json") | ConvertFrom-Json
foreach($field in @('attempt_id','token','worker','started_utc','job_sha256','artifact_directory')){
    if($claim.$field -ne $liveClaim.$field){throw "Canonical live queue claim differs: $field"}
}
if(Test-Path -LiteralPath (Join-Path $artifact 'result.json')){throw "Queue attempt already finished"}
$state=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $projectRoot "Design/AgentReports/SkirmishExpansion/S005/TECHNICAL_READINESS_CURRENT.json") | ConvertFrom-Json
if($null -ne $state.activeRun){throw "Existing S005 native lane ownership remains"}
if(-not (Get-Process -Name 'Unity Hub' -ErrorAction SilentlyContinue)){throw "Keep Unity Hub open and signed in"}
$editors=@(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'")
foreach($editor in $editors){
    if(-not $editor.CommandLine){throw "Cannot verify existing Unity project ownership"}
    if($editor.CommandLine.Replace('/','\').IndexOf($projectRoot,[StringComparison]::OrdinalIgnoreCase) -ge 0){throw "Existing Editor owns project; preserve it"}
}
New-Item -ItemType Directory -Path $RunDirectory -Force | Out-Null
$started=Join-Path $RunDirectory "s005-native-started.json"
if(Test-Path -LiteralPath $started){throw "Preserve existing attempt: no repeated dispatch"}
$jobHash=& rtk proxy $Python -c "import json,hashlib,sys; j=json.load(open(sys.argv[1])); print(hashlib.sha256(json.dumps(j,sort_keys=True,separators=(',',':')).encode()).hexdigest())" $JobFile
if($LASTEXITCODE -ne 0 -or $jobHash.Trim() -ne $claim.job_sha256){throw "Claim/job fingerprint mismatch"}
$canonicalJob=Join-Path $queueDirectory 'job.json'
$canonicalHash=& rtk proxy $Python -c "import json,hashlib,sys; j=json.load(open(sys.argv[1])); print(hashlib.sha256(json.dumps(j,sort_keys=True,separators=(',',':')).encode()).hexdigest())" $canonicalJob
if($LASTEXITCODE -ne 0 -or $canonicalHash.Trim() -ne $claim.job_sha256){throw "Canonical queue job changed"}
$report=Join-Path $projectRoot "Design/AgentReports/SkirmishExpansion/S005"
if(@(Get-ChildItem -LiteralPath $report -Filter "native${runId}-*" -ErrorAction SilentlyContinue).Count){throw "Preserve existing native evidence identity"}
$log=Join-Path $RunDirectory "unity.log"
$before=Join-Path $RunDirectory "source-before.json"
$after=Join-Path $RunDirectory "source-after.json"
& rtk proxy $Python (Join-Path $PSScriptRoot "s005_windows_source_snapshot.py") --project $projectRoot --output $before --job $JobFile
if($LASTEXITCODE -ne 0){throw "Complete source pin preflight failed"}
$plan.result="WrapperStarting";$plan.dispatchEnabled=$true
$metadataLock=Join-Path $queueDirectory '.metadata-lock'
New-Item -ItemType Directory -Path $metadataLock -ErrorAction Stop | Out-Null
try{
    $liveClaim=Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $queueDirectory "claim.json") | ConvertFrom-Json
    foreach($field in @('attempt_id','token','worker','started_utc','job_sha256','artifact_directory')){
        if($claim.$field -ne $liveClaim.$field){throw "Live claim changed during source snapshot: $field"}
    }
    $canonicalHash=& rtk proxy $Python -c "import json,hashlib,sys; j=json.load(open(sys.argv[1])); print(hashlib.sha256(json.dumps(j,sort_keys=True,separators=(',',':')).encode()).hexdigest())" $canonicalJob
    if($LASTEXITCODE -ne 0 -or $canonicalHash.Trim() -ne $claim.job_sha256){throw "Canonical job changed during source snapshot"}
    $startStream=[IO.File]::Open($started,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try{
        $startBytes=[Text.Encoding]::UTF8.GetBytes(($plan | ConvertTo-Json -Depth 5))
        $startStream.Write($startBytes,0,$startBytes.Length)
    }finally{$startStream.Dispose()}
}finally{Remove-Item -LiteralPath $metadataLock -ErrorAction Stop}
$wrapperExit=1
function Quote-S005Literal([string]$value){return "'"+$value.Replace("'","''")+"'"}
$launchFile=Join-Path $RunDirectory "checked-wrapper-launch.ps1"
$launchText="`$ErrorActionPreference='Stop'`n& "+(Quote-S005Literal (Join-Path $PSScriptRoot "InvokeUnity.ps1"))+
    " -UnityExe "+(Quote-S005Literal $UnityExe)+" -ProjectPath "+(Quote-S005Literal $projectRoot)+
    " -LogFile "+(Quote-S005Literal $log)+" -TimeoutSeconds "+$TimeoutSeconds+
    " -UnityArguments @('-executeMethod',"+(Quote-S005Literal $method)+")"
if($GuiLicensing){$launchText+=" -GuiLicensing"}
$launchText+="`nexit `$global:LASTEXITCODE"
$launchText | Out-File -LiteralPath $launchFile -Encoding utf8
try{
    # No -quit: the asynchronous native harness owns its normal validation exit.
    & rtk proxy powershell.exe -NoProfile -ExecutionPolicy Bypass -File $launchFile
    $wrapperExit=$LASTEXITCODE
}finally{
    @{wrapperExit=$wrapperExit;attempt_id=$claim.attempt_id;job_sha256=$claim.job_sha256;executeMethod=$method} |
        ConvertTo-Json | Out-File -LiteralPath (Join-Path $RunDirectory "wrapper-receipt.json") -Encoding utf8
    & rtk proxy $Python (Join-Path $PSScriptRoot "s005_windows_source_snapshot.py") --project $projectRoot --output $after --job $JobFile
}
& rtk proxy $Python (Join-Path $PSScriptRoot "s005_windows_north_qualification.py") --report $report --run-id $runId `
    --log $log --wrapper-exit $wrapperExit --source-before $before --source-after $after --job $JobFile --claim $ClaimFile `
    --wrapper-receipt (Join-Path $RunDirectory "wrapper-receipt.json") |
    Out-File -LiteralPath (Join-Path $RunDirectory "qualification.json") -Encoding utf8
exit $LASTEXITCODE
