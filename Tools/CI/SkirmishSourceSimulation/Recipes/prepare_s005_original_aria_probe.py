#!/usr/bin/env python3
"""Prepare read-only original ARIA UI-lifecycle inspection in the S005 source world."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-transport-scheduler-run-20261105-T1';OUT=ROOT/'T Logs/SkirmishProduction/s005-original-aria-world-probe-20261105-T2';INSPECTOR=ROOT/'T Logs/SkirmishProduction/s004-original-planner-world-overlay-20261105-T2/SourceS004OriginalAriaStepDiagnostic.cs';OVERLAY=INSPECTOR.with_name('receipt.json')
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def once(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact source anchor required: '+a[:110])
 return s.replace(a,b,1)
proof=json.loads(OVERLAY.read_text())
for group in ('source_sha256','original_planner_input_sha256','ui_lifecycle_input_sha256'):
 for p,h in proof[group].items():
  if sha(p)!=h:raise ValueError('Planner source changed: '+p)
OUT.mkdir(parents=True,exist_ok=False)
for n in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','S005SourceStartupWithTransport.cs','derived-source-checkpoint.json','run.py','preparation.json'):shutil.copyfile(OLD/n,OUT/n)
s=INSPECTOR.read_text().replace('SourceS004OriginalAriaStepDiagnostic','SourceS005OriginalAriaStepDiagnostic').replace('S004OriginalAriaStepSameWorldDiagnostic','S005OriginalAriaStepSameWorldDiagnostic').replace('OriginalS004ExpandedAirStepPreconditionsNotMet','OriginalS005ExpandedCombinedStepPreconditionsNotMet')
s=once(s,'view.SourceScenarioIndex != 6 || match.ScenarioIndex != 6 || !view.ExpandedSession || !view.ExpandedBaseAssault || !view.AirProfile','view.SourceScenarioIndex != 7 || match.ScenarioIndex != 7 || !view.ExpandedSession || !view.ExpandedBaseAssault || view.AirProfile || !view.CombinedArmsField')
inspector=OUT/'SourceS005OriginalAriaStepDiagnostic.cs';inspector.write_text(s)
helper=(OUT/'helper.cs').read_text();helper=once(helper,'step<5400','step<1');anchor='            sourceHost=new JObject {';block='''            var ariaProbe=SourceS005OriginalAriaStepDiagnostic.Capture(em,em.GetComponentData<SkirmishMatchState>(session),"WARLINE_S005_ORIGINAL_ARIA_STEP_PROBE");
            Need(ariaProbe!=null,"Original planner diagnostic must run");
''';helper=once(helper,anchor,block+anchor);helper=once(helper,'sourceHost=new JObject {','sourceHost=new JObject { ["original_aria_probe"]=ariaProbe,');(OUT/'helper.cs').write_text(helper)
a=[json.loads(x) for x in (OLD/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)) for x in a];old=str(INSPECTOR);assert a.count(old)==1;a.append(str(inspector));(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));run=(OUT/'run.py').read_text().replace(str(OLD),str(OUT));run=once(run,"env=os.environ.copy();env['MONO_PATH']=","env=os.environ.copy();env['WARLINE_S005_ORIGINAL_ARIA_STEP_PROBE']='1';env['MONO_PATH']=");(OUT/'run.py').write_text(run)
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','S005SourceStartupWithTransport.cs','SourceS005OriginalAriaStepDiagnostic.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
for p in (Path(__file__).resolve(),INSPECTOR,OVERLAY):pins[str(p)]=sha(p)
for group in ('source_sha256','original_planner_input_sha256','ui_lifecycle_input_sha256'):
 for k,h in proof[group].items():
  if k in pins and pins[k]!=h:raise ValueError('Conflicting planner source pin '+k)
  pins[k]=h
d['scope']='Read-only same-world original S005 planner UI-state inspection; no synthetic input or readiness';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
