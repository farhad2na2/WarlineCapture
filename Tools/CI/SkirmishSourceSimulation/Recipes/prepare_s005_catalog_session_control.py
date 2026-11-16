#!/usr/bin/env python3
"""Prepare explicit S005 same-session catalog admission diagnostic."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];OLD=ROOT/'T Logs/SkirmishProduction/s005-current-production-world-20261105-T9';OUT=ROOT/'T Logs/SkirmishProduction/s005-catalog-session-control-20261105-T2';BUILD=ROOT/'T Logs/SkirmishProduction/source-authority-current-s002-build-20261105-T1/build.json'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def once(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact source anchor required: '+a[:120])
 return s.replace(a,b,1)
OUT.mkdir(parents=True,exist_ok=False)
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','derived-source-checkpoint.json','compile.rsp','run.py','preparation.json'):shutil.copyfile(OLD/name,OUT/name)
family=ROOT/'T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004DiagnosticCatalogSourceFamily.cs';text=family.read_text();old=next(line for line in text.splitlines() if 'if(setup.CatalogId=="S004")' in line);new='  if(setup.CatalogId=="S005"){SourceS005CompiledSetupGuard.RequireExact(setup,System.IO.Path.GetFullPath("T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json"));return;}'
(OUT/'S005DiagnosticCatalogSourceFamily.cs').write_text(once(text,old,new))
helper=(OUT/'helper.cs').read_text();needle='            var strategySubset=retainedWorld.RunExpandedRecurringOwnerDiagnostic('
block='''            Need(Object.ReferenceEquals(SourceGroundMissionFamily.RequireSession(em,session),setup),"Same-world S005 session must retain exact compiled setup");
            bool wrongSessionRejected=false;try { SourceGroundMissionFamily.RequireSession(em,Entity.Null); } catch(NotSupportedException) { wrongSessionRejected=true; }
            Need(wrongSessionRejected,"Null session must be rejected");
            bool wrongSetupRejected=false;try { SourceGroundMissionFamily.RequireCatalogIdentity(new SkirmishResolvedSetup { CatalogId="S005",Seed=1,SetupHash=1865447784u }); } catch(InvalidDataException) { wrongSetupRejected=true; }
            Need(wrongSetupRejected,"Incomplete compiled setup must reject");
''';helper=once(helper,needle,block+needle);helper=once(helper,'step<1200','step<60');helper=once(helper,'sourceHost=new JObject {','sourceHost=new JObject { ["catalog_session_admitted"]=true,["wrong_session_rejected"]=wrongSessionRejected,["incomplete_setup_rejected"]=wrongSetupRejected,');(OUT/'helper.cs').write_text(helper)
checkpoint=json.loads((OUT/'derived-source-checkpoint.json').read_text());build=json.loads(BUILD.read_text());current=build['source_and_reference_pins'];expected={str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/413_GeneratedUnitProductionMetadata.cs'),str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/473_GeneratedS002CatalogAvailabilityBinding.cs')};changed={}
for group in ('build_sha256','generated_source_sha256'):
 for path,oldhash in checkpoint[group].items():
  actual=sha(path)
  if actual!=oldhash:
   if group!='build_sha256' or path not in expected or current.get(path)!=actual:raise ValueError('Unreviewed checkpoint change: '+path)
   checkpoint[group][path]=actual;changed[path]={'previous':oldhash,'current':actual}
if set(changed)!=expected:raise ValueError('Expected exactly two reviewed changes')
checkpoint['current_build_sha256']=sha(BUILD);checkpoint['diagnostic_source_changes']=changed;(OUT/'derived-source-checkpoint.json').write_text(json.dumps(checkpoint,indent=2)+'\n')
args=[json.loads(x) for x in (OUT/'compile.rsp').read_text().splitlines()];args=[x.replace(str(OLD),str(OUT)) for x in args];oldfamily=str(family);newfamily=str(OUT/'S005DiagnosticCatalogSourceFamily.cs');oldgetter=str(ROOT/'T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004DiagnosticCatalogGetterBinding.cs');newgetter=str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/473_GeneratedS002CatalogAvailabilityBinding.cs')
for old,new in ((oldfamily,newfamily),(oldgetter,newgetter)):
 if args.count(old)!=1:raise ValueError('Expected one adapter '+old)
 args[args.index(old)]=new
(OUT/'compile.rsp').write_text(''.join(json.dumps(x)+'\n' for x in args))
runner=(OUT/'run.py').read_text().replace(str(OLD),str(OUT)).replace('source-unit-domain-s002-build-20261105-T4','source-authority-current-s002-build-20261105-T1');(OUT/'run.py').write_text(runner)
doc=json.loads((OUT/'preparation.json').read_text());pins=doc['source_pins']
for key in list(pins):
 if key.startswith(str(OLD)):del pins[key]
for name in ('helper.cs','outer.cs','session.cs','SourceEnemyStrategySystem.cs','SourceFreshStorageLifetimeAuthority.cs','S005NoLiveRunwayGuard.cs','S005DiagnosticCatalogSourceFamily.cs','derived-source-checkpoint.json','compile.rsp','run.py'):
 p=OUT/name;pins[str(p)]=sha(p)
for p in [Path(__file__).resolve(),BUILD,Path(newgetter),ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/413_GeneratedUnitProductionMetadata.cs']:
 pins[str(p)]=sha(p)
doc['scope']='Exact compiled S005 and same-world session catalog admission control only; no native or player readiness';(OUT/'preparation.json').write_text(json.dumps(doc,indent=2)+'\n');print('prepared',OUT)
