#!/usr/bin/env python3
"""Prepare a source-only S005 faction dispatcher diagnostic."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-retained-publisher-control-20261105-T2';OUT=ROOT/'T Logs/SkirmishProduction/s005-dispatcher-source-control-20261105-T2';CLEARANCE=ROOT/'T Logs/SkirmishProduction/s002-clearance-projection-20261104-T1/static-clearance.json'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def once(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact source anchor required: '+a[:120])
 return s.replace(a,b,1)
OUT.mkdir(parents=True,exist_ok=False)
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','compile.rsp','run.py','preparation.json'):shutil.copyfile(OLD/name,OUT/name)
helper=(OUT/'helper.cs').read_text()
priorBinding='''            var sourceCatalogBinding=GeneratedS002CatalogAvailabilityBinding.Bind(em,session,retainedWorld.ActualUnitMetadata,unitData,retainedWorld.ActualUnitMetadata.ArtifactSha256);
            Need(sourceCatalogBinding.ConfiguredUnits.Count==roleProjection.Count,"Exact configured catalog domain required");
'''
helper=once(helper,priorBinding,'')
helper=once(helper,'["source_catalog_binding_count"]=sourceCatalogBinding.ConfiguredUnits.Count,','["source_catalog_binding_count"]=retainedWorld.ActualCatalog.ConfiguredUnits.Count,')
anchor='            int productionSchedulerSteps=0; JObject productionSchedulerLast=null;'
block='''            Need(setup.OperationMapId=="opmap.skirmish.desert_base_01" && retainedWorld.SourceNumericCamera==null,"Exact Campaign source and absent source-host camera required");
            var clearanceDocument=JObject.Parse(File.ReadAllText(Path.GetFullPath("CLEARANCE_PATH")));
            Need(Path.GetFullPath((string)clearanceDocument["source_path"])==Path.GetFullPath(physicalPath),"Same retained Campaign geometry input required");
            var sourceClearance=new GeneratedS002DropClearance(Path.GetFullPath("CLEARANCE_PATH"),"CLEARANCE_SHA");
            var transportInputsForDispatch=new GeneratedS002TransportInputs(retainedWorld.ActualUnitMetadata,Path.GetFullPath("."));
            retainedWorld.ActualDeliveryOwner.Helicopters=new SourceHelicopterDelivery(retainedWorld.ActualDeliveryOwner,transportInputsForDispatch.Templates,
                cameraKnown:true,hasCamera:false,actualCameraRight:float3.zero,actualCameraForward:float3.zero,reserveActualStaticBounds:sourceClearance.Reserve);
            retainedWorld.BindActualPaidCatalog(unitData,retainedWorld.ActualUnitMetadata.ArtifactSha256);
            Need(retainedWorld.ActualFactionDispatcher!=null && retainedWorld.ActualCatalog!=null,"Actual source faction dispatcher binding required");
'''.replace('CLEARANCE_PATH',str(CLEARANCE.relative_to(ROOT))).replace('CLEARANCE_SHA',sha(CLEARANCE))
helper=once(helper,anchor,block+anchor)
timing='                actualCatalog.BindSourceDeliveryMetadata(retainedWorld.ActualUnitMetadata.Ordered.ToDictionary(row=>row.Prefab.name,row=>row.Prefab,StringComparer.Ordinal),retainedWorld.ActualUnitMetadata.TryMetadata);\n';helper=once(helper,timing,'')
helper=once(helper,'sourceHost=new JObject {','sourceHost=new JObject { ["faction_dispatcher_bound"]=retainedWorld.ActualFactionDispatcher!=null,["source_camera_present"]=false,["source_static_clearance_scope"]="Pinned historical Campaign capture; live/native parity unproven",');(OUT/'helper.cs').write_text(helper)
a=[json.loads(x) for x in (OUT/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)) for x in a];(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));(OUT/'run.py').write_text((OUT/'run.py').read_text().replace(str(OLD),str(OUT)))
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','S005RetainedMapPublication.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
for p in (Path(__file__).resolve(),CLEARANCE):pins[str(p)]=sha(p)
d['scope']='Source-only S005 dispatcher using absent-camera and historical Campaign clearance assumptions; no native/player readiness';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
