#!/usr/bin/env python3
"""Prepare a source-only S005 same-session catalog decoding control."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-catalog-session-control-20261105-T2';OUT=ROOT/'T Logs/SkirmishProduction/s005-catalog-binding-control-20261105-T1'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def once(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact anchor required: '+a[:100])
 return s.replace(a,b,1)
OUT.mkdir(parents=True,exist_ok=False)
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','derived-source-checkpoint.json','compile.rsp','run.py','preparation.json'):shutil.copyfile(OLD/name,OUT/name)
s=(OUT/'helper.cs').read_text();anchor='            var strategySubset=retainedWorld.RunExpandedRecurringOwnerDiagnostic('
block='''            bool wrongArtifactRejected=false;
            try { GeneratedS002CatalogAvailabilityBinding.Bind(em,session,retainedWorld.ActualUnitMetadata,unitData,new String('0',64)); } catch(InvalidDataException) { wrongArtifactRejected=true; }
            Need(wrongArtifactRejected,"Wrong source metadata artifact must reject");
            var sourceCatalogBinding=GeneratedS002CatalogAvailabilityBinding.Bind(em,session,retainedWorld.ActualUnitMetadata,unitData,retainedWorld.ActualUnitMetadata.ArtifactSha256);
            Need(sourceCatalogBinding.ConfiguredUnits.Count==roleProjection.Count,"Exact configured catalog domain required");
''';s=once(s,anchor,block+anchor);s=once(s,'sourceHost=new JObject {','sourceHost=new JObject { ["source_catalog_binding_count"]=sourceCatalogBinding.ConfiguredUnits.Count,["wrong_artifact_rejected"]=wrongArtifactRejected,');(OUT/'helper.cs').write_text(s)
a=[json.loads(x) for x in (OUT/'compile.rsp').read_text().splitlines()];a=[x.replace(str(OLD),str(OUT)) for x in a];(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in a));(OUT/'run.py').write_text((OUT/'run.py').read_text().replace(str(OLD),str(OUT)))
d=json.loads((OUT/'preparation.json').read_text());pins=d['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
pins[str(Path(__file__).resolve())]=sha(Path(__file__).resolve());d['scope']='Source-only exact S005 session and catalog metadata binding; no dispatcher, commands, live runway or readiness';(OUT/'preparation.json').write_text(json.dumps(d,indent=2)+'\n');print('prepared',OUT)
