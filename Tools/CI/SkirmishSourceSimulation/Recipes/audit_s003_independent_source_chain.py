#!/usr/bin/env python3
"""Audit fresh S003 source compiler and independent factory data without admission."""
import argparse,hashlib,json
from pathlib import Path
from project_s003_current_factory_metadata import _project_payload_checked,read_factory_sources
ROOT=Path(__file__).resolve().parents[4]
def sha(p):
 h=hashlib.sha256()
 with Path(p).open('rb') as f:
  for part in iter(lambda:f.read(1024*1024),b''):h.update(part)
 return h.hexdigest()
def load(p):return json.loads(Path(p).read_bytes())
def verify_pinmap(pins):
 if not isinstance(pins,dict) or not pins:raise ValueError('Nonempty actual source pins required')
 for name,expected in pins.items():
  p=Path(name);p=p if p.is_absolute() else ROOT/p
  if not p.is_file() or p.is_symlink() or sha(p)!=expected:raise ValueError('Source pin changed: '+name)
def desc(p):return {'path':str(Path(p).resolve()),'sha256':sha(p)}
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--output',required=True,type=Path);parser.add_argument('--compiler',type=Path,default=ROOT/'T Logs/SkirmishProduction/s003-current-source-compiler-20261105-T1/receipt.json');parser.add_argument('--source-build',type=Path,default=ROOT/'T Logs/SkirmishProduction/source-authority-current-s002-build-20261105-T1/build.json');args=parser.parse_args()
 compiler=args.compiler.resolve(strict=True);build=args.source_build.resolve(strict=True);graph=ROOT/'T Logs/SkirmishProduction/s003-runtime-graph-refresh-20261104-T1/source-config-graph.json';capture=ROOT/'Logs/SkirmishProduction/s003-current-metadata-capture-20261105-T1/data/s003-native-metadata.json';audit=ROOT/'T Logs/SkirmishProduction/s003-current-metadata-capture-20261105-T1/independent-audit.json';log=ROOT/'T Logs/SkirmishProduction/s003-current-metadata-capture-20261105-T1-editor.log';projection=ROOT/'T Logs/SkirmishProduction/s003-current-factory-projection-20261105-T1/projection-receipt.json'
 index=build.parent/'s003-evidence-index.json'
 if index.exists():
  values=load(index)
  if set(values)!={'capture','audit','log','projection'}:raise ValueError('Exact fresh evidence index required')
  paths=[]
  for key in ('capture','audit','log','projection'):
   value=values[key]
   if type(value)is not dict or set(value)!={'path','sha256'}:raise ValueError('Exact evidence descriptor required')
   original=Path(value['path']);path=original.resolve(strict=True)
   if not original.is_absolute() or original.is_symlink() or original!=path or not path.is_relative_to(ROOT) or not path.is_file() or sha(path)!=value['sha256']:raise ValueError('Fresh evidence descriptor differs: '+key)
   paths.append(path)
  capture,audit,log,projection=paths
 if not compiler.is_relative_to(ROOT) or not build.is_relative_to(ROOT):raise ValueError('Repository-owned compiler and source build required')
 c,b,g,n,a,p=map(load,(compiler,build,graph,capture,audit,projection));m=b'[SkirmishS003MetadataCapture] result=Passed catalog=S003 seed=104732 scope=DataOnly native_context_qualified=false mission_ready=false';logBytes=log.read_bytes()
 if c.get('status')!='Compiled' or c.get('source_build')!=desc(build) or c.get('source_graph')!=desc(graph) or c.get('source_graph_source_pins')!=g.get('source_pins') or c.get('world_initialized') is not False or c.get('mission_readiness') is not False:raise ValueError('Original current compiler/graph/build chain differs')
 if b.get('status')!='Passed' or b.get('compile_exit')!=0:raise ValueError('Fresh source build required')
 rows=[x for x in c.get('result',{}).get('setups',[]) if x.get('size_id')=='Standard'];ctx=n.get('context',{})
 if len(rows)!=1 or rows[0].get('setup_hash')!=2394754492 or ctx.get('setup_hash')!=2394754492 or ctx.get('catalog_id')!='S003' or ctx.get('seed')!=104732:raise ValueError('Original compiler and captured S003 context must agree')
 if a.get('status')!='Passed' or a.get('capture_sha256')!=sha(capture) or a.get('log_sha256')!=sha(log) or a.get('wrapper_exit_observed')!=0 or a.get('marker_count')!=1 or a.get('source_pin_mismatches')!=[] or logBytes.count(m)!=1 or b'\0' in logBytes:raise ValueError('Independent capture audit/log mismatch')
 for key in ('mission_ready','native_context_qualified'):
  if a.get(key) is not False or n.get(key) is not False or p.get(key) is not False:raise ValueError('Evidence scope promoted: '+key)
 if n.get('full_mission_initialized') is not False or n.get('mission_world_created') is not False or n.get('runtime_state_transferred') is not False or n.get('utility_world_disposed') is not True:raise ValueError('Capture must remain asset-only')
 if p.get('kind')!='S003IndependentCurrentFactoryMetadataProjection' or p.get('status')!='PassedSourceOnlyProjection' or p.get('capture')!=desc(capture) or p.get('audit')!=desc(audit) or p.get('runtime_admission') is not False or p.get('source_fit_certified') is not False:raise ValueError('Independent projection chain mismatch')
 groups={'graph':g['source_pins'],'capture':n['source_pins'],'build':b['source_and_reference_pins']}
 for pins in groups.values():verify_pinmap(pins)
 expected=_project_payload_checked(n,sha(capture),n['source_pins'],read_factory_sources(ROOT));art=p['artifacts'];expected['configured-metadata.json']['reference']=art['building-definitions.json']
 for name,value in expected.items():
  descriptor=art[name];path=Path(descriptor['path'])
  if descriptor!=desc(path) or load(path)!=value:raise ValueError('Recomputed original factory projection differs: '+name)
 for pins in groups.values():verify_pinmap(pins)
 result={'kind':'S003IndependentSourceEvidenceChainAudit','status':'PassedSourceOnlyEvidence','compiler':desc(compiler),'source_build':desc(build),'runtime_graph':desc(graph),'native_capture':desc(capture),'independent_audit':desc(audit),'projection_receipt':desc(projection),'factory_artifacts':art,'pin_counts':{name:len(pins) for name,pins in groups.items()},'setup_hash':2394754492,'runtime_admission':False,'native_context_qualified':False,'source_fit_certified':False,'mission_ready':False}
 if index.exists():result['evidence_index']=desc(index)
 args.output.parent.mkdir(parents=True,exist_ok=True)
 with args.output.open('x') as f:json.dump(result,f,indent=2);f.write('\n')
 print(json.dumps({'status':result['status'],'pin_counts':result['pin_counts'],'mission_ready':False}))
if __name__=='__main__':main()
