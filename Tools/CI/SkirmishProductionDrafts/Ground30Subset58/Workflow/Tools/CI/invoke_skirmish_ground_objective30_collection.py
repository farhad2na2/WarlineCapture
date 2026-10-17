"""Plan first; root-owned checked four-source collection, never a queue/readiness runner."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time

from qualify_skirmish_ground_objective30_collection import ROOT,MAP,MAP_SHA,INPUT,INPUT_SHA,METHOD,MARKER,PREPARATION,PROFILE_FREE_PREPARATION,preparation_scope,WORKFLOW_TOOLS,load,sha
from skirmish_ground_objective30_contract import validate
from s005_windows_source_snapshot import snapshot,build_snapshot,candidate_hash

CHILD=r'''param($ProjectPath,$UnityExe,$LogFile,$RawOutput,$TimeoutSeconds)
$ErrorActionPreference='Stop'
$env:WARLINE_GROUND_OBJECTIVE30_OUTPUT=$RawOutput
$ownedExit=1
try {
    & (Join-Path $ProjectPath 'Tools/CI/InvokeUnityExecuteMethodValidation.ps1') -UnityExe $UnityExe -ProjectPath $ProjectPath -ExecuteMethod 'Game.Editor.SkirmishGroundSourceFitSurvey.RunGroundObjective30QualificationCollection' -LogFile $LogFile -RequiredPassMarker '[SkirmishGroundObjective30Collection] result=Collected missions=30 sizeRows=90 physicalSources=4 sourceFitCertified=0 output=' -GuiLicensing -TimeoutSeconds $TimeoutSeconds
    if($global:LASTEXITCODE -ne 0){throw 'Checked collection wrapper returned nonzero'}
    $ownedExit=0
} catch { [Console]::Error.WriteLine($_.ToString()) }
finally {
    [IO.File]::WriteAllText((Join-Path (Split-Path -Parent $LogFile) 'checked-child-exit.txt'),[string]$ownedExit)
}
exit $ownedExit
'''

def write(path,value):
    with Path(path).open('x',encoding='utf-8') as stream:json.dump(value,stream,indent=2);stream.write('\n')

def powershell(arguments,**kwargs):
    return subprocess.run(['rtk','proxy','powershell.exe','-NoProfile','-ExecutionPolicy','Bypass',*arguments],cwd=ROOT,**kwargs)

def inventory():
    code="ConvertTo-Json -Depth 3 -InputObject @(Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe' OR Name = 'Unity Hub.exe' OR Name = 'UnityHub.exe'\" | Select-Object ProcessId,Name,CommandLine)"
    result=powershell(['-Command',code],capture_output=True,text=True)
    if result.returncode:raise ValueError('Read-only process census failed')
    return json.loads(result.stdout or '[]')

def check_lane(rows):
    project=str(ROOT.resolve()).replace('/','\\').casefold()
    for row in rows:
        if row['Name'].casefold()=='unity.exe' and (not row.get('CommandLine') or project in row['CommandLine'].replace('/','\\').casefold()):
            raise ValueError('Active or uninspectable Unity process may own project; preserve it')
    if not any(row['Name'].casefold() in ('unity hub.exe','unityhub.exe') for row in rows):raise ValueError('Keep Unity Hub open and signed in')

def check_preparation(path):
    text=Path(path).read_text(encoding='utf-8-sig',errors='replace')
    preparation_scope(text)
    return sha(path)

def check_installed():
    if sha(ROOT/MAP)!=MAP_SHA or sha(ROOT/INPUT)!=INPUT_SHA:raise ValueError('Exact reviewed map/input changed')
    manifest=load(ROOT/MAP);plan=validate(load(ROOT/INPUT),ROOT)
    for row in manifest['files']:
        if sha(ROOT/row['target'])!=row['sha256']:raise ValueError('Installed reviewed source differs: '+row['target'])
    for row in manifest['artifacts']:
        if sha(ROOT/row['path'])!=row['sha256']:raise ValueError('Reviewed authoring authority differs: '+row['path'])
    return plan

def capture(path):
    started=time.perf_counter();sources=snapshot(ROOT);builds,build_sha=build_snapshot(ROOT)
    value=dict(source_manifest=sources,candidate_sha256=candidate_hash(sources),build_manifest=builds,build_sha256=build_sha)
    write(path,value);return time.perf_counter()-started

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run-directory',type=Path,required=True)
    parser.add_argument('--preparation-log',type=Path)
    parser.add_argument('--timeout-seconds',type=int,default=1800)
    parser.add_argument('--execute',action='store_true')
    args=parser.parse_args();run=args.run_directory.resolve()
    allowed=(ROOT/'Logs/SkirmishProduction').resolve()
    if not run.is_relative_to(allowed) or run==allowed:raise ValueError('Unique run directory must be under project Logs/SkirmishProduction')
    if not 60<=args.timeout_seconds<=7200:raise ValueError('Explicit bounded timeout must be60..7200 seconds')
    if not args.execute:
        print(json.dumps(dict(result='UnexecutedPlan',method=METHOD,reviewed_map_sha256=MAP_SHA,input_sha256=INPUT_SHA,
            physical_source_worlds=4,mission_rows=30,size_rows=90,wrapper='Tools/CI/InvokeUnityExecuteMethodValidation.ps1',
            gui_licensing=True,quit_owner='Checked focused wrapper, synchronous static collector',run_directory=str(run),timeout_seconds=args.timeout_seconds,
            required_preparation_markers=[PREPARATION,PROFILE_FREE_PREPARATION],full_source_dll_endpoints=True,qualified=False,mission_readiness=0),indent=2));return 0
    if args.preparation_log is None:raise ValueError('Checked preparation log required')
    preparation_sha=check_preparation(args.preparation_log);check_installed();check_lane(inventory())
    run.mkdir(parents=True,exist_ok=False)
    before=run/'source-before.json';after=run/'source-after.json';raw=run/'ground-objective30.json';log=run/'unity.log';receipt_path=run/'wrapper-receipt.json'
    tool_pins=[dict(path=p,sha256=sha(ROOT/p)) for p in WORKFLOW_TOOLS]
    receipt=dict(scope='GroundObjective30StaticCollection',method=METHOD,wrapper_exit=1,input_sha256=INPUT_SHA,import_map_sha256=MAP_SHA,
        preparation_log=str(args.preparation_log.resolve()),preparation_sha256=preparation_sha,preparation_checked_zero=True,
        tool_pins=tool_pins,qualified=False,mission_readiness=0,timing_seconds={})
    try:
        receipt['timing_seconds']['before_source_dll']=capture(before)
        # Source scan can take time. Recheck live ownership and exact installed data
        # immediately before invoking the checked wrapper; no ownership is inferred.
        check_lane(inventory());check_installed()
        version=re.search(r'(?m)^m_EditorVersion:\s*(\S+)',(ROOT/'ProjectSettings/ProjectVersion.txt').read_text()).group(1).replace('\r','')
        editor_path=run/'unity-editor.txt'
        resolved=powershell(['-File',str(ROOT/'Tools/CI/ResolveUnityEditor.ps1'),'-UnityVersion',version,'-OutputPath',str(editor_path),'-LogFile',str(run/'resolver.log')])
        if resolved.returncode:raise ValueError('Checked Editor resolution failed')
        unity=editor_path.read_text(encoding='utf-8-sig').strip();child=run/'checked-collection.ps1'
        with child.open('x',encoding='utf-8-sig') as stream:stream.write(CHILD)
        argv=['-File',str(child),'-ProjectPath',str(ROOT),'-UnityExe',unity,'-LogFile',str(log),'-RawOutput',str(raw),'-TimeoutSeconds',str(args.timeout_seconds)]
        write(run/'checked-arguments.json',argv)
        started=time.perf_counter();result=powershell(argv);receipt['timing_seconds']['checked_editor_total']=time.perf_counter()-started
        receipt['wrapper_exit']=result.returncode
    except (OSError,ValueError,AttributeError) as error:receipt['launcher_error']=str(error)
    finally:
        try:receipt['timing_seconds']['after_source_dll']=capture(after)
        except (OSError,ValueError) as error:receipt['after_capture_error']=str(error)
        try:receipt['tool_bytes_unchanged']=all(sha(ROOT/row['path'])==row['sha256'] for row in tool_pins)
        except OSError as error:receipt['tool_bytes_unchanged']=False;receipt['tool_pin_error']=str(error)
        for field,path in (('raw_sha256',raw),('log_sha256',log),('before_sha256',before),('after_sha256',after)):
            if path.exists():receipt[field]=sha(path)
        child_exit=run/'checked-child-exit.txt'
        if child_exit.exists():receipt['checked_child_exit_sha256']=sha(child_exit)
        write(receipt_path,receipt)
    command=['rtk','proxy',sys.executable,str(ROOT/'Tools/CI/qualify_skirmish_ground_objective30_collection.py'),
        '--raw',str(raw),'--receipt',str(receipt_path),'--log',str(log),'--before',str(before),'--after',str(after),
        '--preparation-log',str(args.preparation_log.resolve()),'--output',str(run/'collection-evidence.json')]
    return subprocess.run(command,cwd=ROOT).returncode

if __name__=='__main__':raise SystemExit(main())
