#!/usr/bin/env python3
"""Prepare a source-only S002 target with the original compiler; no Unity or World."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import runpy

ROOT = Path(__file__).resolve().parents[2]

def digest(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''): h.update(block)
    return h.hexdigest()

def check(path, expected):
    if digest(path)!=expected: raise ValueError('pinned input changed: '+str(path))

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--build-receipt',required=True,type=Path);p.add_argument('--data-bundle',required=True,type=Path)
    p.add_argument('--seed',required=True,type=int);p.add_argument('--output',required=True,type=Path)
    a=p.parse_args()
    if not -(2**31)<=a.seed<2**31: raise ValueError('Int32 seed required')
    if a.seed in (1,104731): raise ValueError('Existing S002 seeds use the legacy qualified path')
    runpy.run_path(str(ROOT/'Tools/CI/run_skirmish_source_simulation.py'))
    owned=sys.modules['skirmish_owned_source'];pin_owner=owned.require('skirmish_source_build')
    receipt_sha=digest(a.build_receipt);bundle_sha=digest(a.data_bundle);receipt=json.loads(a.build_receipt.read_bytes()); bundle=json.loads(a.data_bundle.read_bytes())
    if receipt.get('status')!='Passed' or receipt.get('mission')!='S002' or Path(receipt['repository_root']).resolve()!=ROOT or bundle.get('mission')!='S002': raise ValueError('Current S002 build and bundle required')
    pins=receipt['source_and_reference_pins'];response=receipt['compiler_response'];check(response['path'],response['sha256'])
    for path,sha in pins.items(): check(path,sha)
    init=bundle['initialization'];check(init['path'],init['sha256'])
    if not Path(init['path']).is_absolute(): raise ValueError('Absolute initialization required')
    initialization,init_pin=pin_owner.read_json_pinned(init['path'])
    if init_pin['sha256']!=init['sha256']:raise ValueError('Initialization drift')
    graph_desc=initialization['source_config_graph'];matrix_desc=initialization['setup_matrix']
    for descriptor in (graph_desc,matrix_desc):
        if type(descriptor) is not dict or set(descriptor)!={'path','sha256'} or not Path(descriptor['path']).is_absolute():raise ValueError('Exact absolute compiler input descriptor required')
        check(descriptor['path'],descriptor['sha256'])
    graph,graph_pin=pin_owner.read_json_pinned(graph_desc['path'])
    if graph_pin['sha256']!=graph_desc['sha256'] or type(graph.get('source_pins')) is not dict or not graph['source_pins']:raise ValueError('Pinned configuration graph required')
    compiler_inputs={};pin_owner.merge_pins(compiler_inputs,{init_pin['path']:init_pin['sha256'],graph_pin['path']:graph_pin['sha256'],str(Path(matrix_desc['path']).resolve(strict=True)):matrix_desc['sha256']});pin_owner.merge_pins(compiler_inputs,graph['source_pins'])
    out=a.output.absolute();out.mkdir(parents=False,exist_ok=False);temp=out/'build';temp.mkdir();data=out/'.staging';data.mkdir(mode=0o700);final=out/'data'
    raw=Path(response['path']).read_text();old_main='"-main:SourceSimulationProgram"';old_out='"-out:'+str(Path(response['path']).parent/'SourceSimulation.exe')+'"'
    if raw.count(old_main)!=1 or raw.count(old_out)!=1: raise ValueError('Unsupported original compiler response')
    exe=temp/'S002DerivedConfigurationTool.exe';raw=raw.replace(old_main,'"-main:S002DerivedConfigurationTool"').replace(old_out,'"-out:'+str(exe)+'"')
    sources=[ROOT/'Tools/CI/SkirmishSourceSimulation/Tests/MetadataBindingControls.cs',ROOT/'Tools/CI/SkirmishSourceSimulation/Tests/S002DerivedConfigurationTool.cs']
    tool_pins={str(source):digest(source) for source in sources};tool_pins[str(Path(__file__).resolve())]=digest(__file__)
    for source in sources: raw+='\n"'+str(source)+'"'
    rsp=temp/'compile.rsp';rsp.write_text(raw+'\n');csc=[Path(path) for path in pins if path.endswith('/Roslyn/bincore/csc.dll')]
    if not csc: raise ValueError('Pinned Roslyn compiler required')
    csc.sort(key=lambda path:tuple(int(part) for part in path.parts[-4].split('.') if part.isdigit()))
    dotnet=shutil.which('dotnet');
    if not dotnet or str(Path(dotnet).resolve()) not in pins: raise ValueError('Pinned dotnet host required')
    def run(cmd,log,env=None):
        result=subprocess.run(cmd,cwd=ROOT,env=env,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True,timeout=300);log.write_text(result.stdout)
        if result.returncode:raise ValueError('command failed: '+str(log))
        return result.stdout
    run([dotnet,str(csc[-1]),'@'+str(rsp)],temp/'compile.log')
    runtime=receipt['runtime_selection'];env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(Path(runtime['newtonsoft']).parent),runtime['engine'],str(ROOT/'Library/ScriptAssemblies'),str(Path(response['path']).parent/'placement')])
    output=run([runtime['mono'],'--optimize=-float32',str(exe),str(Path(init['path'])),str(a.seed),str(ROOT),str(data)],temp/'prepare.log',env)
    if '[S002DerivedConfiguration] result=Passed ' not in output:raise ValueError('Missing preparation pass marker')
    q=data/'qualification.json';newinit=data/'initialization.json';q_sha=digest(q);newinit_sha=digest(newinit);prepared=json.loads(newinit.read_bytes());qualification=json.loads(q.read_bytes())
    if prepared.get('s002_derived_seed')!={'path':str(q),'sha256':q_sha} or prepared.get('selection',{}).get('seed')!=a.seed or qualification.get('seed')!=a.seed or qualification.get('diagnostic_only') is not True or qualification.get('native_context_qualified') is not False or qualification.get('mission_ready') is not False:raise ValueError('Prepared output identity/scope differs')
    check(q,q_sha);check(newinit,newinit_sha);prepared['s002_derived_seed']['path']=str(final/'qualification.json');newinit.write_text(json.dumps(prepared,indent=2)+'\n');newinit_sha=digest(newinit);bundle['seed']=a.seed;bundle['initialization']={'path':str(final/'initialization.json'),'sha256':newinit_sha}
    newbundle=data/'data-bundle.json';newbundle.write_text(json.dumps(bundle,indent=2)+'\n')
    for path,sha in pins.items(): check(path,sha)
    for path,sha in tool_pins.items(): check(path,sha)
    pin_owner.verify(compiler_inputs);owned.pins()
    check(a.build_receipt,receipt_sha);check(a.data_bundle,bundle_sha)
    check(init['path'],init['sha256']);check(response['path'],response['sha256'])
    check(q,q_sha);check(newinit,newinit_sha)
    (data/'preparation-receipt.json').write_text(json.dumps({'kind':'S002SourceOnlyDerivedPreparation','seed':a.seed,'build_receipt_sha256':receipt_sha,'input_bundle_sha256':bundle_sha,'qualification_sha256':q_sha,'initialization_sha256':newinit_sha,'data_bundle_sha256':digest(newbundle),'compiler_input_pins':compiler_inputs,'tool_source_pins':tool_pins,'native_context_qualified':False,'mission_ready':False,'world_allocated':False},indent=2)+'\n')
    os.rename(data,final)
    print(json.dumps({'status':'PreparedSourceOnly','seed':a.seed,'qualification':str(final/'qualification.json'),'data_bundle':str(final/'data-bundle.json'),'native_context_qualified':False,'mission_ready':False}))

if __name__=='__main__':
    try: main()
    except (OSError,ValueError,KeyError,TypeError,subprocess.TimeoutExpired) as e:
        print('Preparation failed: '+str(e),file=sys.stderr);sys.exit(1)
