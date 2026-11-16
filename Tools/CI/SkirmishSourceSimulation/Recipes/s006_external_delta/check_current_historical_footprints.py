#!/usr/bin/env python3
"""Recheck historical S006 candidate rectangles using current source owners.

This is a source-only diagnostic. The retained mask, candidate geometry and
pairwise building rectangles are not a loaded scene or a source-fit certificate.
"""
import hashlib
import importlib.util
import json
import os
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
LOGS=ROOT/'T Logs/SkirmishProduction'
CURRENT=LOGS/'s006-current-config-only-candidate-20261106-T22'
HISTORICAL=LOGS/'s006-canonical-v2-candidate-20261105-T7'
PROPOSAL=LOGS/'s006-static-footprint-proposals-20261105-T1/report.json'
INPUT=LOGS/'s005-campaign-placement-inputs-20261105-T1'
GRAPH=ROOT/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/source-config-graph.json'
MONO_CONFIG=LOGS/'s004-current-physical-input-projection-20261104-T1/mono-config.xml'
RUNTIME_BUILD=LOGS/'s003-current-prefab-import-build-20261104-T1/build.json'
OWNER_RECIPE=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/validate_s006_source_footprints.py'
GENERATOR=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/regenerate_placement_registry.py'
OUT=LOGS/'s006-current-historical-footprint-check-20261106-T2'

def sha(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):h.update(block)
    return h.hexdigest()

def load_module(name,path):
    spec=importlib.util.spec_from_file_location(name,path)
    if spec is None or spec.loader is None:raise ValueError('Exact source recipe required')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def main():
    OUT.mkdir(exist_ok=False)
    try:
        candidate_receipt=json.loads((CURRENT/'receipt.json').read_bytes())
        if candidate_receipt.get('status')!='CollectedUnqualifiedSourceCandidate' or candidate_receipt.get('runtime_admission') is not False or candidate_receipt.get('mission_ready') is not False:
            raise ValueError('Explicit unqualified current candidate required')
        pins=dict(candidate_receipt['source_pins'])
        candidate=json.loads((CURRENT/'result.json').read_bytes());historical=json.loads((HISTORICAL/'result.json').read_bytes());proposals=json.loads(PROPOSAL.read_bytes());graph=json.loads(GRAPH.read_bytes())
        if sha(CURRENT/'result.json')!=candidate_receipt['result_sha256'] or proposals.get('kind')!='S006StaticFootprintProposals' or proposals.get('mission_ready') is not False:
            raise ValueError('Exact candidate and historical proposal artifacts required')
        for name,digest in graph['source_pins'].items():
            if name in pins and pins[name]!=digest:raise ValueError('Conflicting source pin: '+name)
            pins[name]=digest
        for name,digest in proposals['input_sha256'].items():
            if name.endswith('/Recipes/propose_s006_static_starting_footprints.py'):
                continue  # Historical generator drift is recorded, not accepted as current provenance.
            if name in pins and pins[name]!=digest:raise ValueError('Conflicting retained input pin: '+name)
            pins[name]=digest
        for path in (Path(__file__),CURRENT/'receipt.json',CURRENT/'result.json',CURRENT/'compile.rsp',HISTORICAL/'result.json',PROPOSAL,GRAPH,MONO_CONFIG,RUNTIME_BUILD,OWNER_RECIPE,GENERATOR):pins[str(path)]=sha(path)
        def verify():
            for name,digest in pins.items():
                if not Path(name).is_file() or sha(name)!=digest:raise ValueError('Pinned input changed: '+name)
        verify();joined=[]
        for size in ('Standard','War','LargeWar'):
            current=next(x for x in candidate['rows'] if x.get('size')==size and x.get('difficulty')=='Regular')
            prior=next(x for x in historical['rows'] if x.get('size')==size)
            proposed=next(x for x in proposals['sizes'] if x.get('size')==size)
            if not current.get('compiled') or current['setup_hash']!=prior['setup_hash'] or current['setup_hash']!=proposed['setup_hash'] or current['setup']['Structures']!=prior['setup']['Structures'] or current['source_pad_resolution']!=prior['source_pad_resolution']:
                raise ValueError('Historical proposals do not match current candidate structure inputs: '+size)
            if proposed['structure_count']!=len(current['setup']['Structures']) or proposed['candidate_rectangles_found']!=proposed['structure_count']:
                raise ValueError('Complete historical rectangle proposals required: '+size)
            joined.append({'size':size,'setup_hash':current['setup_hash'],'structure_count':proposed['structure_count']})
        generated=OUT/'generated';proof=load_module('current_s006_placement_generator',GENERATOR).generate(ROOT,generated)
        owner_inputs=dict(proof['source_inputs'])
        owner_inputs.update({str(GENERATOR.with_name(name)):digest for name,digest in proof['owned_context_inputs'].items()})
        for name,digest in owner_inputs.items():
            if name in pins and pins[name]!=digest:raise ValueError('Conflicting placement owner pin: '+name)
            pins[name]=digest
        current_placement=generated/'BuildingPlacementValidation.source.cs';pins[str(current_placement)]=sha(current_placement)
        module=load_module('s006_original_footprint_fixture',OWNER_RECIPE);control=OUT/'S006SourceFootprintOwnerControl.cs';control.write_text(module.SOURCE);pins[str(control)]=sha(control)
        options=[json.loads(x) for x in (CURRENT/'compile.rsp').read_text().splitlines() if x.strip()]
        old_control=str(CURRENT/'S006SourceCandidateControl.cs');old_placement=[x for x in options if x.endswith('/331_BuildingPlacementValidation.source.cs')]
        if options.count(old_control)!=1 or len(old_placement)!=1:raise ValueError('Exact source owner replacement seams required')
        options=[str(control) if x==old_control else str(current_placement) if x==old_placement[0] else '-main:S006SourceFootprintOwnerControl' if x.startswith('-main:') else '-out:'+str(OUT/'control.exe') if x.startswith('-out:') else x for x in options]
        rsp=OUT/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in options));compiler=next(Path(x) for x in pins if x.endswith('/Roslyn/bincore/csc.dll'))
        compile_run=subprocess.run(['dotnet',str(compiler),'@'+str(rsp)],capture_output=True,text=True,timeout=300);(OUT/'compile.log').write_text(compile_run.stdout+compile_run.stderr)
        if compile_run.returncode:raise RuntimeError('Current footprint owner compilation failed')
        runtime=json.loads(RUNTIME_BUILD.read_bytes())['runtime_selection'];pins[runtime['mono']]=sha(runtime['mono']);env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((str(RUNTIME_BUILD.parent/'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(OUT)))
        command=[runtime['mono'],'--config',str(MONO_CONFIG),'--optimize=-float32',str(OUT/'control.exe'),str(PROPOSAL),str(GRAPH),str(INPUT/'sourceInvalidPrefix.bin'),str(OUT/'result.json')]
        run=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(OUT/'run.log').write_text(run.stdout+run.stderr)
        marker='[S006SourceFootprintOwners] result=Collected sizes=3 liveWorld=0 readiness=0'
        if run.returncode or run.stdout.splitlines().count(marker)!=1:raise RuntimeError('Current original owner execution failed')
        verify();result=json.loads((OUT/'result.json').read_bytes());counts=[]
        for row in result['sizes']:
            counts.append({'size':row['size'],'checked':len(row['rows']),'valid':sum(x.get('original_rect_valid') is True and x.get('original_surface_valid') is True for x in row['rows'])})
        receipt={'status':'CollectedUnqualifiedFootprintChecks','source_pins':pins,'result_sha256':sha(OUT/'result.json'),'current_candidate_join':joined,'owner_results':counts,
                 'scope':'Current extracted rectangle and surface owners on unqualified retained historical rectangles, inactive masks and pairwise building rectangles; historical proposal generator drifted, so generation provenance is not verified. Not live placement, path blocking, active water or source-fit proof',
                 'historical_generator_provenance_verified':False,
                 'helipad_note':'Pairwise building-overlap fixture is not a navigation-blocker assertion; actual helipad pathing remains owned by the original game.',
                 'physical_source_validated':False,'runtime_admission':False,'mission_ready':False}
        (OUT/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'status':receipt['status'],'owner_results':counts,'mission_ready':False}))
    except Exception as error:
        (OUT/'failure.json').write_text(json.dumps({'status':'Failed','error':repr(error),'runtime_admission':False,'mission_ready':False},indent=2)+'\n');raise

if __name__=='__main__':main()
