#!/usr/bin/env python3
"""Prepare a longer natural S005 source dispatcher diagnostic."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-dispatcher-source-control-20261105-T2';OUT=ROOT/'T Logs/SkirmishProduction/s005-dispatcher-extended-20261105-T1'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
OUT.mkdir(parents=True,exist_ok=False)
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','compile.rsp','run.py','preparation.json'):shutil.copyfile(OLD/name,OUT/name)
p=OUT/'helper.cs';s=p.read_text();assert s.count('step<60')==1;p.write_text(s.replace('step<60','step<1200',1))
a=[json.loads(x) for x in (OUT/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)) for x in a];(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));(OUT/'run.py').write_text((OUT/'run.py').read_text().replace(str(OLD),str(OUT)))
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
pins[str(Path(__file__).resolve())]=sha(Path(__file__).resolve());d['scope']='1200 natural source dispatcher ticks; historical clearance, absent camera, no injected input or native readiness';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
