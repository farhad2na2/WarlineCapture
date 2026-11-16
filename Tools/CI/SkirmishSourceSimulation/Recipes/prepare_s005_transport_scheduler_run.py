#!/usr/bin/env python3
"""Prepare natural S005 diagnostic using original transport scheduler owners."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-dispatcher-request-trace-20261105-T2';CANDIDATE=ROOT/'T Logs/SkirmishProduction/s005-transport-scheduler-compile-20261105-T2';OUT=ROOT/'T Logs/SkirmishProduction/s005-transport-scheduler-run-20261105-T1'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
OUT.mkdir(parents=True,exist_ok=False)
for n in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','run.py','preparation.json'):shutil.copyfile(OLD/n,OUT/n)
shutil.copyfile(CANDIDATE/'S005SourceStartupWithTransport.cs',OUT/'S005SourceStartupWithTransport.cs')
a=[json.loads(x) for x in (CANDIDATE/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)).replace(str(CANDIDATE/'S005SourceStartupWithTransport.cs'),str(OUT/'S005SourceStartupWithTransport.cs')) for x in a];a=[('-out:'+str(OUT/'source-force-ledger.exe')) if x.startswith('-out:') else x for x in a];(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));(OUT/'run.py').write_text((OUT/'run.py').read_text().replace(str(OLD),str(OUT)))
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','S005SourceStartupWithTransport.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
for p in [Path(__file__).resolve(),ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityService.cs',ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityCommands.cs',CANDIDATE/'preparation.json']:
 pins[str(p)]=sha(p)
d['scope']='Natural source transport scheduler diagnostic; pinned historical clearance and absent camera; no native/player readiness';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
