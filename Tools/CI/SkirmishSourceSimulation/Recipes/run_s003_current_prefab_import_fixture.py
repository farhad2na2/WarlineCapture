#!/usr/bin/env python3
"""Exercise current shared importer against independently checked S003 prefab data."""
import argparse,hashlib,json,os,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];sys.path.insert(0,str(ROOT/'Tools/CI/SkirmishSourceSimulation/Tests'))
from finalize_s003_refreshed_prefab_capture import qualify
BUILD=ROOT/'T Logs/SkirmishProduction/source-authority-current-s002-build-20261105-T1/build.json';CAPTURE=ROOT/'Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1/data/prefab-reference-data.json';LOG=ROOT/'T Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1-editor.log';AUDIT=ROOT/'T Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1/independent-audit.json';KEYS=ROOT/'T Logs/SkirmishProduction/s003-refreshed-prefab-keys-20261104-T1/prefab-keys.json';MOVEMENT=ROOT/'T Logs/SkirmishProduction/s003-refreshed-rifle-input-20261104-T1/unit-movement-source-input.json';HARNESS=ROOT/'Tools/CI/SkirmishSourceSimulation/Tests/S003RefreshedPrefabImportControl.cs'
MARKER='[S003RefreshedPrefabImportControl] result=Passed roots=19 entities=647 unsupported=32 omitted=26 negatives=2 sourceProjection=NotChecked world=0 readiness=0'
def sha(p):
 h=hashlib.sha256()
 with Path(p).open('rb') as f:
  for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
 return h.hexdigest()
def main():
 q=argparse.ArgumentParser();q.add_argument('--output',type=Path,required=True);out=q.parse_args().output.resolve();out.mkdir(parents=True,exist_ok=False)
 build=json.loads(BUILD.read_text());audit=json.loads(AUDIT.read_text());
 if build.get('status')!='Passed' or build.get('mission')!='S002' or build.get('compile_exit')!=0:raise ValueError('Current shared S002 source build required as fixture host')
 checked=qualify(ROOT,CAPTURE,LOG,0)
 if audit.get('status')!='PassedDataOnlyCapture' or audit.get('capture',{}).get('sha256')!=sha(CAPTURE) or audit.get('editor_log',{}).get('sha256')!=sha(LOG) or audit.get('mission_ready') is not False or checked['capture']!=audit['capture']:raise ValueError('Exact independently audited prefab capture required')
 pins=dict(build['source_and_reference_pins']);extra={str(p):sha(p) for p in (BUILD,CAPTURE,LOG,AUDIT,KEYS,MOVEMENT,HARNESS,Path(__file__).resolve())}
 for k,v in extra.items():
  if k in pins and pins[k]!=v:raise ValueError('Conflicting source pin: '+k)
  pins[k]=v
 def verify():
  for path,h in pins.items():
   if not Path(path).is_file() or sha(path)!=h:raise ValueError('Input changed: '+path)
 verify();source=HARNESS.read_text();start=source.index('            string movementPath=');end=source.index('            int next=500000;',start);source=source[:start]+source[end:]
 old='GeneratedReferenceTemplateImporter.Import(manager,source,directory,keys,"Unit_Chr_Soldier_Male_02_Alt_04",movement.Projection,movement.Combat,movement.Health,movement.Attack)';new='GeneratedReferenceTemplateImporter.Import(manager,source,directory,keys)'
 if source.count(old)!=1:raise ValueError('Exact importer fixture seam required')
 source=source.replace(old,new).replace('negatives=3 sourceProjection=Passed','negatives=2 sourceProjection=NotChecked');fixture=out/'ImporterOnlyFixture.cs';fixture.write_text(source)
 args=[json.loads(x) for x in Path(build['compiler_response']['path']).read_text().splitlines() if x.strip()];args=[('-main:S003RefreshedPrefabImportControl' if x.startswith('-main:') else '-out:'+str(out/'control.exe') if x.startswith('-out:') else x) for x in args];args.append(str(fixture));rsp=out/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in args))
 csc=next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'));compiled=subprocess.run(['dotnet',str(csc),'@'+str(rsp)],capture_output=True,text=True,timeout=300);(out/'compile.log').write_text(compiled.stdout+compiled.stderr)
 if compiled.returncode:raise RuntimeError('Shared source fixture failed to compile')
 runtime=build['runtime_selection'];env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((str(BUILD.parent/'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(out)))
 run=subprocess.run([runtime['mono'],str(out/'control.exe'),str(CAPTURE),str(KEYS),str(MOVEMENT)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/'run.log').write_text(run.stdout+run.stderr)
 if run.returncode or run.stdout.splitlines().count(MARKER)!=1:raise RuntimeError('Shared source importer fixture failed')
 verify();after=qualify(ROOT,CAPTURE,LOG,0)
 if after['capture']!=audit['capture']:raise ValueError('Captured source changed')
 receipt={'status':'PassedDisposableImporterFixture','shared_host_mission':'S002','target_capture_mission':'S003','marker':MARKER,'source_pins_rechecked':len(pins),'capture_sha256':sha(CAPTURE),'movement_source_projection_checked':False,'scope':'Shared importer source fixture only; movement source projection not checked; no S003 runtime admission, native parity or mission readiness','world_allocated':False,'mission_ready':False};(out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))
if __name__=='__main__':main()
