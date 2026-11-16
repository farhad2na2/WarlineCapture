#!/usr/bin/env python3
"""Prepare S005 source-world startup with separately captured production definitions."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
OLD=ROOT/'T Logs/SkirmishProduction/s005-same-world-unit-metadata-20261105-T1'
OUT=ROOT/'T Logs/SkirmishProduction/s005-current-production-world-20261105-T9'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def replace(s,a,b):
 if a not in s:raise ValueError('Missing source anchor: '+a)
 return s.replace(a,b)
OUT.mkdir(parents=True,exist_ok=False)
for n in ['helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','derived-source-checkpoint.json','compile.rsp','run.py','preparation.json']:
 shutil.copyfile(OLD/n,OUT/n)
changes={
 'Logs/SkirmishProduction/s005-current-factory-capture-20261105-T3/full-factory-runway-definitions.json':'Logs/SkirmishProduction/s005-production-factory-capture-20261105-T2/production-factory-runway-definitions.json',
 'Logs/SkirmishProduction/s005-combined-prefab-capture-20261105-T1/selected-unit-prefab-object-references.json':'Logs/SkirmishProduction/s005-combined-prefab-current-20261105-T1/selected-unit-prefab-object-references.json',
 'T Logs/SkirmishProduction/s005-combined-prefab-projection-20261105-T1/projection.json':'T Logs/SkirmishProduction/s005-combined-prefab-current-projection-20261105-T1/projection.json',
 'Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4':'Logs/SkirmishProduction/s005-unit-identity-current-20261105-T1',
 'T Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4-editor.log':'T Logs/SkirmishProduction/s005-unit-identity-current-20261105-T1-editor.log'}
helper=(OUT/'helper.cs').read_text()
for a,b in changes.items(): helper=helper.replace(a,b)
old_factory='a341801cbc93b925de78c6c2f91ca37757c19c06962bab38885c5ab05582565a';new_factory=sha(ROOT/changes['Logs/SkirmishProduction/s005-current-factory-capture-20261105-T3/full-factory-runway-definitions.json'])
helper=replace(helper,old_factory,new_factory).replace('GeneratedBuildingDefinitions.LoadS005Source(factoryPath,','GeneratedBuildingDefinitions.LoadS005ProductionDefinitions(factoryPath,')
helper=replace(helper,'templates.Prefabs,setup);\n        var native=','templates.Prefabs,setup,scenarioDefinition.ArmyProfileConfig);\n        var native=')
helper=replace(helper,'source.BindGeneratedDefinitions(definitions.Definitions,true);','source.BindGeneratedDefinitions(definitions.Definitions.Where(d => entries.OfType<JObject>().Any(row => (string)row[\"visual_key\"] == d.Prefab.name)).ToArray(),true);')
helper=replace(helper,'gridEntity,boundary,definitions.Definitions,true,registry,','gridEntity,boundary,definitions.Definitions.Where(d => entries.OfType<JObject>().Any(row => (string)row[\"visual_key\"] == d.Prefab.name)).ToArray(),true,registry,')
for n in ('unit-production-metadata.json','receipt.json'):
 old=ROOT/'Logs/SkirmishProduction/s005-unit-identity-capture-20261105-T4'/n;new=ROOT/'Logs/SkirmishProduction/s005-unit-identity-current-20261105-T1'/n;helper=replace(helper,sha(old),sha(new))
helper=replace(helper,"using Game.Runtime;","using Game.Runtime;\nusing Game.Simulation;")
catalogCode = r'''            Need(!em.HasComponent<SkirmishProductionCatalogRecord>(session),"Fresh same-world catalog required");
            var roleProjection=setup.RoleOverlays.Where(role=>scenarioDefinition.ArmyProfileConfig.Allows(role.RoleId))
                .ToDictionary(role=>SkirmishRoleCatalogConfig.RuntimePrefabKey(role.RoleKind),role=>role,StringComparer.Ordinal);
            Need(roleProjection.Count==retainedWorld.ActualUnitMetadata.Ordered.Count,"Complete original allowed-role domain required");
            foreach(var row in retainedWorld.ActualUnitMetadata.Ordered) {
                Need(roleProjection.TryGetValue(row.Prefab.name,out var role),"Actual role required for each configured unit");
                string producerKey=GeneratedProductionRecipeSource.ProducerPrefabKey(role.Producer);
                Need(definitions.Assets.TryGetValue(producerKey,out var producer) && definitions.Definitions.Any(d=>Object.ReferenceEquals(d.Prefab,producer)),"Exact captured producer definition required");
                Need(definitions.Assets.TryGetValue(row.Prefab.name,out var unit) && Object.ReferenceEquals(unit,row.Prefab),"Same-world configured unit identity required");
            }
            em.AddComponentData(session,new SkirmishProductionCatalogRecord {Catalog=new SkirmishProductionCatalog(setup,roleProjection,true)});
            var actualCatalog=em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
            Need(Object.ReferenceEquals(actualCatalog.Setup,setup) && roleProjection.All(row=>actualCatalog.TryRole(row.Key,out _)),"Same-world original catalog role projection required");
'''
helper=replace(helper,'            var strategySubset=retainedWorld.RunExpandedRecurringOwnerDiagnostic(',catalogCode+'            var strategySubset=retainedWorld.RunExpandedRecurringOwnerDiagnostic(')
helper=replace(helper,'sourceHost=new JObject { ["unit_metadata_binding"]=unitBinding,','sourceHost=new JObject { ["source_catalog_role_count"]=roleProjection.Count,["source_catalog_same_setup"]=Object.ReferenceEquals(actualCatalog.Setup,setup),["production_owner_bound"]=false,["unit_metadata_binding"]=unitBinding,')
probe = r'''            string placementText=File.ReadAllText(Path.GetFullPath("Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset"));
            var queueMatches=System.Text.RegularExpressions.Regex.Matches(placementText,@"(?m)^  maxQueuedUnitProductions: (\d+)\r?$");
            Need(queueMatches.Count==1,"Unique source queue limit required");
            int queueLimit=Int32.Parse(queueMatches[0].Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
            string compositionText=File.ReadAllText(Path.GetFullPath("Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs"));
            var seedMatches=System.Text.RegularExpressions.Regex.Matches(compositionText,@"\binternal uint BuildingSpawnRandomState = 0x([0-9a-fA-F]+)u;");
            Need(seedMatches.Count==1,"Unique original constructor random seed required");
            uint randomInitial=UInt32.Parse(seedMatches[0].Groups[1].Value,System.Globalization.NumberStyles.HexNumber,System.Globalization.CultureInfo.InvariantCulture);
            string productionError=null;
            try { var transportInputs=new GeneratedS002TransportInputs(retainedWorld.ActualUnitMetadata,Path.GetFullPath("."));
                retainedWorld.PrepareActualProductionInputs(queueLimit,randomInitial,transportInputs.TryMetadata); }
            catch(NotSupportedException error) { productionError=error.GetType().Name+": "+error.Message; }
            catch(InvalidOperationException error) { productionError=error.GetType().Name+": "+error.Message; }
            var productionProbe=new JObject { ["bound"]=retainedWorld.ActualDeliveryOwner!=null,["error"]=productionError,
                ["queue_limit"]=queueLimit,["original_random_initial"]=randomInitial,["live_runway_admitted"]=false,
                ["scope"]="Source diagnostic owner setup, no commands, payment, delivery or native readiness" };
'''
helper=replace(helper,'60);\n            sourceHost=new JObject {', '60);\n'+probe+'            sourceHost=new JObject {')
helper=replace(helper,'["production_owner_bound"]=false,','["production_input_probe"]=productionProbe,')
advance = r'''            int productionSchedulerSteps=0; JObject productionSchedulerLast=null;
            if(retainedWorld.ActualDeliveryOwner!=null) {
                actualCatalog.BindSourceDeliveryMetadata(retainedWorld.ActualUnitMetadata.Ordered.ToDictionary(row=>row.Prefab.name,row=>row.Prefab,StringComparer.Ordinal),retainedWorld.ActualUnitMetadata.TryMetadata);
                var tracePath=Path.Combine(Environment.GetEnvironmentVariable("WARLINE_S005_ROAD_RECEIPT_DIR"),"production-scheduler-trace.jsonl");
                using var trace=new StreamWriter(new FileStream(tracePath,FileMode.CreateNew,FileAccess.Write,FileShare.Read));
                for(int step=0;step<1200;step++) {
                    try {
                        productionSchedulerLast=retainedWorld.AdvanceConstructedSourceOwners();
                        productionSchedulerSteps++;
                        if(productionSchedulerSteps%60==0) { trace.WriteLine(new JObject { ["step"]=productionSchedulerSteps,["owner_receipt"]=productionSchedulerLast,["events"]=JArray.FromObject(retainedWorld.ActualDeliveryOwner.DeliveryEvents) }.ToString(Newtonsoft.Json.Formatting.None));trace.Flush(); }
                        var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                        var objective=em.GetComponentData<SkirmishObjectiveStateComponent>(session);
                        if(state.Phase==SkirmishSessionPhase.Finished || objective.Terminal!=0) break;
                    } catch(Exception error) {trace.WriteLine(new JObject { ["completed_steps"]=productionSchedulerSteps,["failure"]=error.ToString() }.ToString(Newtonsoft.Json.Formatting.None));trace.Flush();throw;}
                }
            }
            var productionScheduler=new JObject { ["completed_steps"]=productionSchedulerSteps,
                ["last_owner_receipt"]=productionSchedulerLast,
                ["source_delivery_events"]=retainedWorld.ActualDeliveryOwner==null ? new JArray() : JArray.FromObject(retainedWorld.ActualDeliveryOwner.DeliveryEvents),
                ["scope"]="Natural source owner scheduling; no injected requests or native readiness" };
'''
helper=replace(helper,'            sourceHost=new JObject {',advance+'            sourceHost=new JObject {')
helper=replace(helper,'["production_input_probe"]=productionProbe,','["production_input_probe"]=productionProbe,["production_scheduler"]=productionScheduler,')

(OUT/'helper.cs').write_text(helper)
args=[json.loads(x) for x in (OUT/'compile.rsp').read_text().splitlines()]
args=[x.replace(str(OLD),str(OUT)) for x in args]
args=[x.replace(str(ROOT/'T Logs/SkirmishProduction/s005-combined-prefab-control-20261105-T1/SourceIdentityHandles.cs'),str(ROOT/'T Logs/SkirmishProduction/s005-current-prefab-import-control-20261105-T3/SourceIdentityHandles.cs')) for x in args]
args.append(str(ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/S005ProductionDefinitions.cs.txt'))
guardOld=str(ROOT/'T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004NoLiveRunwayGuard.cs')
guardSource=Path(guardOld).read_text()
if guardSource.count('Setup.CatalogId!="S004"')!=1 or args.count(guardOld)!=1:raise ValueError('Exact original unplaced-runway guard required')
guard=OUT/'S005NoLiveRunwayGuard.cs';guard.write_text(guardSource.replace('Setup.CatalogId!="S004"','Setup.CatalogId!="S005"'))
args[args.index(guardOld)]=str(guard)

(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in args))
runner=(OUT/'run.py').read_text().replace(str(OLD),str(OUT))
for a,b in changes.items(): runner=runner.replace(a,b)
(OUT/'run.py').write_text(runner)
doc=json.loads((OUT/'preparation.json').read_text());pins=doc['source_pins']
for k in list(pins):
 if k.startswith(str(OLD)) or 's005-unit-identity-capture-20261105-T4' in k or 's005-combined-prefab-capture-20261105-T1' in k:del pins[k]
capture=json.loads((ROOT/changes['Logs/SkirmishProduction/s005-combined-prefab-capture-20261105-T1/selected-unit-prefab-object-references.json']).read_text())
for n in ('capture_assembly','entities_assembly'):
 a=capture[n];pins[a['path']]=a['sha256']
for n in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/n;pins[str(p)]=sha(p)
for p in [Path(__file__).resolve(),ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/S005ProductionDefinitions.cs.txt',ROOT/'T Logs/SkirmishProduction/s005-current-prefab-import-control-20261105-T3/SourceIdentityHandles.cs']:
 pins[str(p)]=sha(p)
for rel in ('Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset','Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs'):
 p=ROOT/rel;pins[str(p)]=sha(p)
for n in ('unit-production-metadata.json','receipt.json','unit-prefab-identity-readback.json'):
 p=ROOT/'Logs/SkirmishProduction/s005-unit-identity-current-20261105-T1'/n;pins[str(p)]=sha(p)
p=ROOT/'T Logs/SkirmishProduction/s005-unit-identity-current-20261105-T1-editor.log';pins[str(p)]=sha(p)
doc['current_production_scope']='Ten captured definitions, unplaced airport only; seven-row startup readback historical; no live runway or native readiness'
(OUT/'preparation.json').write_text(json.dumps(doc,indent=2)+'\n')
print('prepared',OUT)
