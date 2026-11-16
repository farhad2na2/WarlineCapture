#!/usr/bin/env python3
"""Prepare optional authored NAV facts and a new data bundle; never create a world.

Install beside skirmish_source_build.py. The built CLI must include the reachable
prepare-navigation-facts command and current generated producer identity.
"""
from __future__ import annotations
import argparse, json, math, os, subprocess, sys, time
from pathlib import Path
sys.dont_write_bytecode = True
def _bootstrap_owned_source():
 import hashlib,types
 root=Path(__file__).resolve().parent;path=root/'skirmish_owned_source.py';raw=path.read_bytes()
 if len(raw)>2*1024*1024:raise ValueError('Bounded owned loader source required')
 if 'skirmish_owned_source' in sys.modules:raise ValueError('Unowned loader module collision')
 module=types.ModuleType('skirmish_owned_source');module.__file__=str(path);module.__executed_source_pin__={'path':str(path),'sha256':hashlib.sha256(raw).hexdigest()};sys.modules[module.__name__]=module
 try:
  exec(compile(raw,str(path),'exec'),module.__dict__)
  if path.read_bytes()!=raw:raise ValueError('Owned loader source changed during bootstrap')
  module.load_cli(root)
 except BaseException:
  if sys.modules.get(module.__name__)is module:del sys.modules[module.__name__]
  raise
 return module
owned=_bootstrap_owned_source()
from skirmish_source_build import need, verify, sha, pin, write, merge_pins, prepare_mono_config
from skirmish_source_manifest import UnsupportedSourceShape

def read_json_pinned(path):
    # Parse exactly the bytes hashed. Do not normalize duplicate initialization
    # keys into a new valid bundle that the original strict C# reader would reject.
    p=Path(path).resolve(strict=True);raw=p.read_bytes()
    def unique(pairs):
        result={}
        for key,value in pairs:
            if key in result: raise ValueError('Duplicate JSON key: '+key)
            result[key]=value
        return result
    def reject_constant(value): raise ValueError('Nonfinite JSON constant: '+value)
    return json.loads(raw,object_pairs_hook=unique,parse_constant=reject_constant),{'path':str(p),'sha256':sha(raw),'bytes':len(raw)}

def prepare(build_receipt, data_bundle, output, timeout=60):
    out=Path(output).absolute();need(math.isfinite(timeout) and timeout>0,'Finite positive timeout required')
    need(not out.exists(),'Fresh cache preparation output required');out.mkdir(parents=True)
    build,bp=read_json_pinned(build_receipt);bundle,dp=read_json_pinned(data_bundle)
    need(build.get('status')=='Passed' and build.get('mission')=='S002' and build.get('seed')==104731,'Qualified supported build required')
    need(bundle.get('schema_version')==1 and bundle.get('mission')=='S002' and bundle.get('seed')==104731,'Supported explicit data bundle required')
    verify(build['source_and_reference_pins']);exe=build['executable'];need(sha(Path(exe['path']).read_bytes())==exe['sha256'],'Built image changed')
    tools={k:Path(v).resolve(strict=True) for k,v in build['runtime_selection'].items()}
    need(set(tools)=={'contents','script','engine','mono','newtonsoft'},'Build-recorded runtime paths required')
    need(str(tools['mono']) in build['source_and_reference_pins'] and str(tools['newtonsoft']) in build['source_and_reference_pins'],'Bound runtime selection required')
    initialization,ip=read_json_pinned(bundle['initialization']['path']);need(ip['sha256']==bundle['initialization']['sha256'],'Initialization changed')
    raw=initialization['navigation_reference'];need('facts' not in raw,'Preparation starts from retained raw descriptor')
    descriptor=out/'raw-descriptor.json';write(descriptor,raw);config,extra=prepare_mono_config(tools,out)
    pins=dict(build['source_and_reference_pins']);merge_pins(pins,owned.pins());merge_pins(pins,extra);merge_pins(pins,{bp['path']:bp['sha256'],dp['path']:dp['sha256'],ip['path']:ip['sha256'],exe['path']:exe['sha256'],str(descriptor):sha(descriptor.read_bytes()),raw['path']:raw['sha256']});verify(pins)
    env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(Path(build['repository_root'])/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(build['placement_build']).parent)])
    command=[str(tools['mono']),'--config',str(config),'--optimize=-float32',exe['path'],'prepare-navigation-facts',str(descriptor),str(out/'facts')]
    start=time.monotonic();timed_out=False
    with (out/'prepare.log').open('w') as log:
        child=subprocess.Popen(command,env=env,stdout=log,stderr=subprocess.STDOUT)
        try: child.wait(timeout=timeout)
        except subprocess.TimeoutExpired: timed_out=True
        finally:
            if child.poll() is None:
                child.terminate()
                try: child.wait(timeout=5)
                except subprocess.TimeoutExpired: child.kill();child.wait(timeout=5)
    verify(pins);need(not timed_out and child.returncode==0,'Preparation child failed or timed out')
    need('[NavigationFactsPrepare] result=Passed' in (out/'prepare.log').read_text(),'Missing preparation pass marker')
    facts,fp=read_json_pinned(out/'facts/descriptor.json')
    need(facts['path']==raw['path'] and facts['sha256']==raw['sha256'],'Raw source identity must remain')
    initialization['navigation_reference']=facts;newinit=out/'initialization.json';write(newinit,initialization)
    bundle['initialization']=pin(newinit);newbundle=out/'data-bundle.json';write(newbundle,bundle)
    result={'schema_version':1,'kind':'PreparedNavigationFactsBundle','status':'Passed','process_exit':child.returncode,'timed_out':timed_out,'wall_seconds':time.monotonic()-start,'build_receipt':bp,'original_bundle':dp,'original_initialization':ip,'prepared_descriptor':fp,'data_bundle':pin(newbundle),'input_and_runtime_pins':pins,'scope':'Immutable decoded authored inputs only; original fresh-world materializer still executes; raw descriptor retained.'}
    write(out/'receipt.json',result);return result

if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--build-receipt',required=True);p.add_argument('--data-bundle',required=True);p.add_argument('--output',required=True);p.add_argument('--timeout',type=float,default=60);a=p.parse_args()
    try: print(json.dumps(prepare(a.build_receipt,a.data_bundle,a.output,a.timeout)))
    except (UnsupportedSourceShape,ValueError,KeyError,OSError) as error: print('Navigation facts preparation failed: '+str(error),file=sys.stderr);raise SystemExit(2)
