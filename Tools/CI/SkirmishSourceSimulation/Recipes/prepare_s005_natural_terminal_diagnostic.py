#!/usr/bin/env python3
"""Prepare an idle-player, original-owner S005 terminal diagnostic."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-transport-scheduler-run-20261105-T1';OUT=ROOT/'T Logs/SkirmishProduction/s005-natural-terminal-diagnostic-20261105-T1'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def once(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact source anchor required: '+a[:100])
 return s.replace(a,b,1)
OUT.mkdir(parents=True,exist_ok=False)
for n in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','S005SourceStartupWithTransport.cs','derived-source-checkpoint.json','run.py','preparation.json'):shutil.copyfile(OLD/n,OUT/n)
helper=(OUT/'helper.cs').read_text();helper=once(helper,'step<5400','step<60000');anchor='            var productionScheduler=new JObject {';block='''            var observedSession=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var observedObjective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
            var observedMatch=em.GetComponentData<SkirmishMatchState>(session);
            var observedOutcome=new JObject { ["expanded_phase"]=observedSession.Phase.ToString(),["objective_terminal"]=(int)observedObjective.Terminal,
                ["objective_outcome"]=observedObjective.Outcome.ToString(),["match_phase"]=observedMatch.Phase.ToString(),["match_outcome"]=observedMatch.Outcome.ToString(),
                ["steps_completed"]=productionSchedulerSteps,["player_input_claimed"]=false,["native_readiness"]=false };
''';helper=once(helper,anchor,block+anchor);helper=once(helper,'sourceHost=new JObject {','sourceHost=new JObject { ["observed_outcome"]=observedOutcome,');(OUT/'helper.cs').write_text(helper)
a=[json.loads(x) for x in (OLD/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)) for x in a];(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));runner=(OUT/'run.py').read_text().replace(str(OLD),str(OUT));runner=once(runner,'timeout=300,env=env','timeout=900,env=env');(OUT/'run.py').write_text(runner)
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for n in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','S005SourceStartupWithTransport.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/n;pins[str(p)]=sha(p)
pins[str(Path(__file__).resolve())]=sha(Path(__file__).resolve());d['scope']='Idle-player natural source diagnostic; original owners, fixed 1/60 steps, historical clearance, absent camera; no claimed input or readiness';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
