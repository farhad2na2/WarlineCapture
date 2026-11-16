#!/usr/bin/env python3
"""Compile-only original S005 transport and pinned supplemental scheduler candidate."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-dispatcher-request-trace-20261105-T2';OUT=ROOT/'T Logs/SkirmishProduction/s005-transport-scheduler-compile-20261105-T2';PREV=ROOT/'T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
OUT.mkdir(parents=True,exist_ok=False);checkpoint=json.loads((OLD/'derived-source-checkpoint.json').read_text());a=[json.loads(line) for line in (OLD/'compile.rsp').read_text().splitlines()]
def swap(suffix,replacement):
 hits=[i for i,v in enumerate(a) if v.endswith(suffix)]
 if len(hits)!=1:raise ValueError('Exactly one source required: '+suffix)
 a[hits[0]]=str(replacement)
def require_pin(p):
 p=str(p);expected=next((checkpoint[g].get(p) for g in ('build_sha256','generated_source_sha256') if p in checkpoint[g]),None)
 if expected is None or expected!=sha(p):raise ValueError('Original scheduler pin changed: '+p)
for name in ('S004AirFuelSafetyOrderMetadata.cs','S004AirFuelSafetyOwnerStep.cs','S004AirFuelSafetySourceOwnerOrder.cs','S004AirFuelSafetySupplementalOwners.cs'):require_pin(PREV/name)
swap('/generic-SkirmishEnemyCapabilityService.cs',ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityService.cs')
swap('/530-generic-SkirmishEnemyCapabilityCommands.cs',ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityCommands.cs')
swap('/312_PaidSourceOwnerStep.retry.cs',PREV/'S004AirFuelSafetyOwnerStep.cs')
swap('/534_SourceOwnerOrderAdmission.cs',PREV/'S004AirFuelSafetySourceOwnerOrder.cs')
source=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/AutonomousWorld.SourceStartup.cs';text=source.read_text();needle='            InitializeGeneratedSourceOwnersForExistingGameplay(day);'
if text.count(needle)!=1:raise ValueError('Exact retained owner initialization required')
extra='''
            if(s004SupplementalTickOwners!=null || s004MissileTickOwners!=null || s004ScanRopeTickOwners!=null)
                throw new InvalidOperationException("Supplemental source owners bind once");
            s004SupplementalTickOwners=new S004SupplementalTickOwners(Entities);
            s004MissileTickOwners=new S004MissileTickOwners(Entities);
            s004ScanRopeTickOwners=new S004ScanRopeTickOwners(Entities);'''
startup=OUT/'S005SourceStartupWithTransport.cs';startup.write_text(text.replace(needle,needle+extra,1));swap('/AutonomousWorld.SourceStartup.cs',startup)
a=[('-out:'+str(OUT/'compile-probe.exe')) if x.startswith('-out:') else x for x in a];(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));paths=[source,PREV/'S004AirFuelSafetyOwnerStep.cs',PREV/'S004AirFuelSafetySourceOwnerOrder.cs',ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityService.cs',ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityCommands.cs',Path(__file__).resolve()];(OUT/'preparation.json').write_text(json.dumps({'status':'CompileCandidate','source_sha256':{str(p):sha(p) for p in paths},'scope':'Original transport methods and pinned supplemental scheduler; not executed or qualified','mission_ready':False},indent=2)+'\n');print('prepared',OUT)
