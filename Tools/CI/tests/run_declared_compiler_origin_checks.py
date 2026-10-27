"""Portable version of the qualified nine declared-authoring root checks; no C# or World."""
from pathlib import Path
import argparse,hashlib,json,runpy,sys,tempfile
sys.dont_write_bytecode=True
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--ci-root',type=Path,default=Path(__file__).resolve().parents[1]);p.add_argument('--output',type=Path,required=True);p.add_argument('--request',type=Path,action='append',default=[]);p.add_argument('--source-repo',type=Path);p.add_argument('--data-repo',type=Path);args=p.parse_args()
if args.request and (args.source_repo is None or args.data_repo is None):p.error('Graph requests require explicit --source-repo and --data-repo')
ci=args.ci_root.resolve(strict=True);sys.path.insert(0,str(ci));runpy.run_path(str(ci/'run_skirmish_source_simulation.py'),run_name='qualified_test_bootstrap');import skirmish_source_authoring as a
checks=[];h=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
def need(value,name):
 if not value:raise AssertionError(name)
 checks.append(name)
def reject(action,name,message):
 try:action()
 except Exception as e:
  if message not in str(e):raise AssertionError(name+': unexpected failure '+str(e)) from e
  checks.append(name);return
 raise AssertionError(name+': accepted')
with tempfile.TemporaryDirectory(prefix='declared-origin-roots-') as t:
 base=Path(t).resolve();repo=base/'repo';data=base/'data';origin=base/'origin';historic=base/'historic'
 for d in [repo,data,origin,historic]:d.mkdir()
 doc={'schema_version':1,'kind':'QualifiedNativeMetadataOrigin','native_metadata':{'path':'ExternalPinnedNativeMetadataNotLoadedInPureRootTest','sha256':'0'*64},'source_root':str(origin),'data_root':str(historic),'input_pins':{}}
 file=base/'origin.json';file.write_text(json.dumps(doc));descriptor={'path':str(file),'sha256':h(file)};roots,pin=a.declared_origin_roots(descriptor);need(roots==(origin,historic),'exact_pinverified_origin_roots')
 for d,name in [(repo,'current_cs'),(data,'current_asset'),(origin,'historical_source'),(historic,'historical_data')]:
  f=d/('sample.cs' if d==repo else 'sample.asset');f.write_text(name);need(a.declared_graph_pin(str(f),h(f),repo,data,roots)['sha256']==h(f),name)
 reject(lambda:a.declared_origin_roots({'path':str(file),'sha256':'0'*64}),'unpinned_origin_rejected','Declared native origin descriptor changed')
 bad=base/'bad.json';bad.write_text(json.dumps(dict(doc,kind='WrongOrigin')));reject(lambda:a.declared_origin_roots({'path':str(bad),'sha256':h(bad)}),'wrong_origin_kind_rejected','Exact qualified native origin shape required')
 f=base/'outside.asset';f.write_text('outside');reject(lambda:a.declared_graph_pin(str(f),h(f),repo,data,roots),'outside_allroots_rejected','outside explicit current/native-origin')
 f=origin/'sample.asset';reject(lambda:a.declared_graph_pin(str(f),'0'*64,repo,data,roots),'stale_graph_digest_rejected','Actual source/profile asset changed')
actual=[]
for path in args.request:
 request=json.loads(path.read_text());roots,pin=a.declared_origin_roots(request['metadata_source_origin']);descriptor=request['graph'];assert h(descriptor['path'])==descriptor['sha256'];graph=json.loads(Path(descriptor['path']).read_text())
 for file,digest in graph['source_pins'].items():a.declared_graph_pin(file,digest,args.source_repo.resolve(strict=True),args.data_repo.resolve(strict=True),roots)
 actual.append({'catalog_id':request['catalog_id'],'request_sha256':h(path),'actual_graph_pins_verified':len(graph['source_pins'])})
assert len(checks)==9
report={'result':'Passed','checks':checks,'actual_graphs':actual,'world':False,'scope':'Root/hash checks only; original C# still validates complete native provenance/factory inputs.'}
with args.output.open('x') as f:f.write(json.dumps(report,indent=2)+'\n')
print('[DeclaredOriginRootControls] result=Passed checks=9 sources='+str(len(actual))+' world=False')
