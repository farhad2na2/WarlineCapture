#!/usr/bin/env python3
"""Prepare natural S005 source requests trace beyond its opening restraint."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-dispatcher-extended-20261105-T1';OUT=ROOT/'T Logs/SkirmishProduction/s005-dispatcher-request-trace-20261105-T2'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def once(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact source anchor required: '+a[:100])
 return s.replace(a,b,1)
OUT.mkdir(parents=True,exist_ok=False)
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','compile.rsp','run.py','preparation.json'):shutil.copyfile(OLD/name,OUT/name)
p=OUT/'helper.cs';s=p.read_text();s=once(s,'for(int step=0;step<1200;step++) {','var seenRequests=new HashSet<string>(StringComparer.Ordinal);\n                for(int step=0;step<5400;step++) {')
anchor='                        productionSchedulerSteps++;\n';block='''                        if(em.HasBuffer<BuildingFactionUnitProductionRequest>(boundary)) {
                            var factionRequestRows=em.GetBuffer<BuildingFactionUnitProductionRequest>(boundary);
                            for(int index=0;index<factionRequestRows.Length;index++) {
                                var request=factionRequestRows[index];
                                string signature=request.RequestId+":"+request.FactionId+":"+request.UnitId+":"+request.Status+":"+request.ResultCode;
                                if(seenRequests.Add(signature)) { trace.WriteLine(new JObject { ["kind"]="post_scheduler_request_sample",["step"]=productionSchedulerSteps,["request_id"]=request.RequestId,["faction"]=request.FactionId,["unit_id"]=request.UnitId.ToString(),["status"]=request.Status,["result_code"]=request.ResultCode,["materials_cost"]=request.MaterialsCost,["resources_reserved"]=request.ResourcesReserved }.ToString(Newtonsoft.Json.Formatting.None));trace.Flush(); }
                            }
                        }
''';s=once(s,anchor,anchor+block);p.write_text(s)
a=[json.loads(x) for x in (OUT/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)) for x in a];(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));(OUT/'run.py').write_text((OUT/'run.py').read_text().replace(str(OLD),str(OUT)))
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
pins[str(Path(__file__).resolve())]=sha(Path(__file__).resolve());d['scope']='5400 natural source ticks; post-scheduler request samples can miss transient requests; historical clearance and absent camera, no injected input or native readiness';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
