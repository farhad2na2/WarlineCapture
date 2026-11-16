#!/usr/bin/env python3
"""Prepare a diagnostic same-world S005 original publisher ownership check."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-catalog-binding-control-20261105-T1';OUT=ROOT/'T Logs/SkirmishProduction/s005-retained-publisher-control-20261105-T2'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def once(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact source anchor required: '+a[:120])
 return s.replace(a,b,1)
OUT.mkdir(parents=True,exist_ok=False)
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','derived-source-checkpoint.json','compile.rsp','run.py','preparation.json'):shutil.copyfile(OLD/name,OUT/name)
original=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004RetainedMapPublication.cs';data=once(original.read_text(),'Setup.CatalogId!="S004"','Setup.CatalogId!="S005"')
oldMap='active.OperationMapId.ToString()!=Setup.OperationMapId';newMap='active.OperationMapId.ToString()!=ActualDefinition.CampaignMap.OperationMapId'
if data.count(oldMap)!=2:raise ValueError('Exact original logical map checks required')
data=data.replace(oldMap,newMap);(OUT/'S005RetainedMapPublication.cs').write_text(data)
helper=(OUT/'helper.cs').read_text();a='internal static JObject Run(EntityManager em,Entity session,SkirmishResolvedSetup setup,SkirmishScenarioDefinitionConfig scenarioDefinition)';b='internal static JObject Run(EntityManager em,Entity session,SkirmishResolvedSetup setup,SkirmishScenarioDefinitionConfig scenarioDefinition, Game.Composition.OperationMapRuntimeBootstrapSceneSystemHelper publisher, Entity mapRoot)';helper=once(helper,a,b)
anchor='            int productionSchedulerSteps=0; JObject productionSchedulerLast=null;'
block='''            Need(retainedWorld.ActualDeliveryOwner!=null && root==mapRoot,"Actual source production world and published root required");
            bool nullPublisherRejected=false;try { retainedWorld.AttachS004RetainedMapPublication(null,mapRoot); } catch(InvalidOperationException) { nullPublisherRejected=true; }
            Need(nullPublisherRejected,"Null map publisher must reject");
            retainedWorld.AttachS004RetainedMapPublication(publisher,mapRoot);
            retainedWorld.RequireS004RetainedMapPublication();
''';helper=once(helper,anchor,block+anchor);helper=once(helper,'sourceHost=new JObject {','sourceHost=new JObject { ["retained_source_publisher_attached"]=true,["null_publisher_rejected"]=nullPublisherRejected,');(OUT/'helper.cs').write_text(helper)
session=(OUT/'session.cs').read_text();session=once(session,'S005StartingGrantExperiment.Run(em, entity, setup, definition)','S005StartingGrantExperiment.Run(em, entity, setup, definition, publisher, mapRoot)');(OUT/'session.cs').write_text(session)
a=[json.loads(x) for x in (OUT/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)) for x in a];assert a.count(str(original))==1;a[a.index(str(original))]=str(OUT/'S005RetainedMapPublication.cs');(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));(OUT/'run.py').write_text((OUT/'run.py').read_text().replace(str(OLD),str(OUT)))
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
for p in (Path(__file__).resolve(),original):pins[str(p)]=sha(p)
d['scope']='Same-world original S005 logical publisher attachment; physical scene remains unvalidated';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
