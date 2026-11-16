#!/usr/bin/env python3
"""Build or invoke the unqualified S006 external-binding source inspection CLI."""
import argparse
import hashlib
import json
import os
import subprocess
import time
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
LOGS=ROOT/'T Logs/SkirmishProduction'
BASE=LOGS/'s006-current-config-only-candidate-20261106-T22'
RUNTIME_BUILD=LOGS/'s003-current-prefab-import-build-20261104-T1/build.json'
WORKER=Path(__file__).with_name('S006ExternalBindingInspection.cs')

def sha(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        for part in iter(lambda:f.read(1024*1024),b''):h.update(part)
    return h.hexdigest()

def verify(pins):
    for name,digest in pins.items():
        if not Path(name).is_file() or sha(name)!=digest:raise ValueError('Pinned input changed: '+name)

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('command',choices=('build','inspect'));p.add_argument('--output',type=Path,required=True)
    p.add_argument('--build-receipt',type=Path);p.add_argument('--graph',type=Path);p.add_argument('--graph-sha256')
    p.add_argument('--matrix',type=Path);p.add_argument('--matrix-sha256');p.add_argument('--candidate',type=Path);p.add_argument('--candidate-sha256')
    p.add_argument('--size',choices=('Standard','War','LargeWar'));p.add_argument('--difficulty',choices=('Recruit','Regular','Veteran','Commander'))
    a=p.parse_args();out=a.output.resolve();base=LOGS.resolve()
    if not out.is_relative_to(base) or out.exists():raise ValueError('Fresh T: diagnostic output required')
    out.mkdir(parents=True);started=time.monotonic()
    try:
        if a.command=='build':
            prior=json.loads((BASE/'receipt.json').read_bytes())
            if prior.get('status')!='CollectedUnqualifiedSourceCandidate' or prior.get('runtime_admission') is not False:raise ValueError('Pinned source diagnostic closure required')
            pins=dict(prior['source_pins']);verify(pins)
            options=[json.loads(x) for x in (BASE/'compile.rsp').read_text().splitlines() if x.strip()]
            old=str(BASE/'S006SourceCandidateControl.cs')
            if options.count(old)!=1:raise ValueError('Exact source candidate entry required')
            options=[str(WORKER) if x==old else '-main:S006ExternalBindingInspection' if x.startswith('-main:') else '-out:'+str(out/'inspect.exe') if x.startswith('-out:') else x for x in options]
            for path in (Path(__file__),WORKER,BASE/'receipt.json',BASE/'compile.rsp',RUNTIME_BUILD):pins[str(path)]=sha(path)
            rsp=out/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in options))
            compiler=next(Path(x) for x in pins if x.endswith('/Roslyn/bincore/csc.dll'))
            process=subprocess.run(['dotnet',str(compiler),'@'+str(rsp)],capture_output=True,text=True,timeout=300)
            (out/'compile.log').write_text(process.stdout+process.stderr);verify(pins)
            report={'status':'PassedSourceOnlyBuild' if process.returncode==0 else 'FailedSourceOnlyBuild','compile_exit':process.returncode,
                    'source_pins':pins,'executable_sha256':sha(out/'inspect.exe') if process.returncode==0 else None,
                    'runtime_build':str(RUNTIME_BUILD),'scope':'S006 config-only diagnostic worker; no world or admission','mission_ready':False}
            (out/'build-receipt.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'status':report['status'],'compile_exit':process.returncode}));return 0 if process.returncode==0 else 1
        if any(x is None for x in (a.build_receipt,a.graph,a.graph_sha256,a.matrix,a.matrix_sha256,a.candidate,a.candidate_sha256,a.size,a.difficulty)):
            raise ValueError('Explicit build, graph, matrix, candidate descriptors, size and difficulty required')
        receipt_path=a.build_receipt.resolve(strict=True);build=json.loads(receipt_path.read_bytes())
        if build.get('status')!='PassedSourceOnlyBuild' or build.get('mission_ready') is not False:raise ValueError('Passed unqualified build required')
        pins=dict(build['source_pins']);verify(pins);pins[str(receipt_path)]=sha(receipt_path)
        binary=receipt_path.parent/'inspect.exe';
        if sha(binary)!=build['executable_sha256']:raise ValueError('Diagnostic executable changed')
        pins[str(binary)]=sha(binary)
        descriptors=[]
        for path,digest in ((a.graph,a.graph_sha256),(a.matrix,a.matrix_sha256),(a.candidate,a.candidate_sha256)):
            target=path.resolve(strict=True)
            if not target.is_relative_to(ROOT) or sha(target)!=digest:raise ValueError('Exact repository-owned input and hash required: '+str(target))
            pins[str(target)]=digest;descriptors.extend((str(target),digest))
        runtime=json.loads(Path(build['runtime_build']).read_bytes())['runtime_selection'];env=os.environ.copy()
        env['MONO_PATH']=os.pathsep.join((str(Path(build['runtime_build']).parent/'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(receipt_path.parent)))
        command=[runtime['mono'],str(binary),*descriptors,a.size,a.difficulty,str(out/'result.json')]
        process=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/'process.log').write_text(process.stdout+process.stderr);verify(pins)
        result=json.loads((out/'result.json').read_bytes()) if (out/'result.json').exists() else None
        valid=process.returncode==0 and isinstance(result,dict) and result.get('kind')=='S006UnqualifiedExternalBindingInspection' and result.get('catalog_id')=='S006' and result.get('size')==a.size and result.get('difficulty')==a.difficulty and all(result.get(x) is False for x in ('world_allocated','source_fit_verified','visual_reviewed','runtime_admission','mission_ready'))
        report={'status':'InspectedUnqualifiedCandidate' if valid else 'Failed','process_exit':process.returncode,'command':command,'result':result,'input_pins':pins,'wall_seconds':time.monotonic()-started,'world_allocated':False,'runtime_admission':False,'mission_ready':False}
        (out/'receipt.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'status':report['status'],'process_exit':process.returncode,'binding_compiler_accepted':result.get('binding_compiler_accepted') if result else None}));return 0 if valid else 1
    except Exception as error:
        (out/'failure.json').write_text(json.dumps({'status':'Failed','error':repr(error),'world_allocated':False,'runtime_admission':False,'mission_ready':False},indent=2)+'\n');raise

if __name__=='__main__':raise SystemExit(main())
