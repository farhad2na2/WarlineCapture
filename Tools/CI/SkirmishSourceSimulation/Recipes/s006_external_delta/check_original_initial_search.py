#!/usr/bin/env python3
"""Exercise current original initial search on unqualified historical candidate inputs."""
import hashlib
import importlib.util
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's006-current-historical-footprint-check-20261106-T2'
OUT = LOGS / 's006-original-initial-search-20261106-T3'
RECIPE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/validate_s006_source_footprints.py'
PROPOSAL = LOGS / 's006-static-footprint-proposals-20261105-T1/report.json'
GRAPH = ROOT / 'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/source-config-graph.json'
PREFIX = LOGS / 's005-campaign-placement-inputs-20261105-T1/sourceInvalidPrefix.bin'
BUILD = LOGS / 's003-current-prefab-import-build-20261104-T1/build.json'
CONFIG = LOGS / 's004-current-physical-input-projection-20261104-T1/mono-config.xml'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def main():
    OUT.mkdir(exist_ok=False)
    try:
        prior = json.loads((BASE / 'receipt.json').read_bytes())
        if prior.get('status') != 'CollectedUnqualifiedFootprintChecks' or prior.get('mission_ready') is not False:
            raise ValueError('Exact unqualified original-footprint receipt required')
        pins = dict(prior['source_pins'])
        for path in (Path(__file__), RECIPE, BASE / 'receipt.json', BASE / 'compile.rsp', BASE / 'generated/BuildingInitialPlacement.source.cs', PROPOSAL, GRAPH, PREFIX, BUILD, CONFIG):
            pins[str(path)] = sha(path)
        def verify():
            for path, expected in pins.items():
                if not Path(path).is_file() or sha(path) != expected:
                    raise ValueError('Pinned input changed: ' + path)
        verify()
        spec = importlib.util.spec_from_file_location('footprint_recipe', RECIPE)
        recipe = importlib.util.module_from_spec(spec); spec.loader.exec_module(recipe)
        source = recipe.SOURCE
        start = source.index('    int x=(int)p["proposed_origin"][0]')
        end = source.index('occupied.Add(rect);}', start) + len('occupied.Add(rect);}')
        replacement = r'''    int w=(int)p["footprint"][0],h=(int)p["footprint"][1];var footprint=new Vector2Int(w,h);
    var def=new RuntimeDefinition{FootprintCells=footprint};var owner=new BuildingRuntimeSpawnCompositionSystemHelper();
    var ctx=new BuildingRuntimeSpawnCompositionSystemHelper.Context{
      GetPlacementFootprint=(d,r)=>d.FootprintCells,
      GetEffectivePlacementRect=(d,at,g,r)=>new RectInt(at,d.FootprintCells),
      IsPlacementValid=(d,at,fp,r,g,roads,blockers)=>{
        var rect=new RectInt(at,fp);
        bool valid=BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(rect,g,roads,blockers,true,prefix,2049,1025,null,null,q=>{foreach(var old in occupied)if(q.Overlaps(old))return true;return false;});
        var evaluator=new BuildingSurfacePlacementUtilitySystemHelper();
        return valid&&evaluator.TryEvaluateFootprint(surface,at,fp,.5f,45f,out var evaluated)&&evaluated.IsValid;
      }};
    var preferred=new Vector2Int((int)p["preferred_origin"][0],(int)p["preferred_origin"][1]);
    bool found=owner.TryFindValidInitialBuildingOrigin(ctx,def,preferred,false,grid,null,default,out var origin,emitCensusEvents:false);
    rows.Add(new JObject{["structure_index"]=p["structure_index"],["preferred"]=new JArray(preferred.x,preferred.y),["found"]=found,["origin"]=found?(JToken)new JArray(origin.x,origin.y):JValue.CreateNull(),["historical_proposal"]=p["proposed_origin"]});
    if(found)occupied.Add(new RectInt(origin,footprint));}'''
        source = source[:start] + replacement + source[end:]
        anchor = '   var output=new JArray();foreach(JObject group'
        if source.count(anchor) != 1:
            raise ValueError('Exact source control insertion point required')
        control = r'''   var ownerControl=new BuildingRuntimeSpawnCompositionSystemHelper();var testDefinition=new RuntimeDefinition{FootprintCells=new Vector2Int(2,2)};
   var testContext=new BuildingRuntimeSpawnCompositionSystemHelper.Context{
    GetPlacementFootprint=(d,r)=>d.FootprintCells,GetEffectivePlacementRect=(d,at,g,r)=>new RectInt(at,d.FootprintCells),
    IsPlacementValid=(d,at,fp,r,g,roads,blockers)=>false};
   if(ownerControl.TryFindValidInitialBuildingOrigin(testContext,testDefinition,new Vector2Int(100,100),false,grid,null,default,out _,emitCensusEvents:false,maxSearchRadiusLimit:1))throw new Exception("All-invalid source search must fail");
   testContext.IsPlacementValid=(d,at,fp,r,g,roads,blockers)=>at.x==101&&at.y==100;
   if(!ownerControl.TryFindValidInitialBuildingOrigin(testContext,testDefinition,new Vector2Int(100,100),false,grid,null,default,out var testOrigin,emitCensusEvents:false)||testOrigin.x!=101||testOrigin.y!=100)throw new Exception("Original relocation control failed");
'''
        source = source.replace(anchor, control + anchor, 1)
        source = source.replace('foreach(JObject group in (JArray)proposal["sizes"])', 'foreach(JObject group in new JArray())', 1)
        source = source.replace('S006OriginalSourceFootprintChecks', 'S006OriginalInitialSearchControls').replace('[S006SourceFootprintOwners] result=Collected sizes=3 liveWorld=0 readiness=0', '[S006OriginalInitialSearch] result=ControlsOnly sizes=0 liveWorld=0 readiness=0')
        source = source.replace('Original rectangle and full surface owners on retained inactive Campaign inputs; no live occupancy, active water, typed pad or placement registration', 'Original initial search with source rectangle and surface owners on retained inactive inputs and fixture occupancy; no live occupancy, active water, typed pad or placement registration')
        control = OUT / 'S006OriginalInitialSearchControl.cs'; control.write_text(source); pins[str(control)] = sha(control)
        args = [json.loads(x) for x in (BASE / 'compile.rsp').read_text().splitlines() if x.strip()]
        old_control = str(BASE / 'S006SourceFootprintOwnerControl.cs')
        old_initial = [x for x in args if x.endswith('/330_BuildingInitialPlacement.source.cs')]
        if args.count(old_control) != 1 or len(old_initial) != 1:
            raise ValueError('Exact current owner extraction replacements required')
        args = [str(control) if x == old_control else str(BASE / 'generated/BuildingInitialPlacement.source.cs') if x == old_initial[0] else '-out:' + str(OUT / 'control.exe') if x.startswith('-out:') else x for x in args]
        rsp = OUT / 'compile.rsp'; rsp.write_text(''.join(json.dumps(x) + '\n' for x in args))
        compiler = next(Path(x) for x in pins if x.endswith('/Roslyn/bincore/csc.dll'))
        build = subprocess.run(['dotnet', str(compiler), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
        (OUT / 'compile.log').write_text(build.stdout + build.stderr)
        if build.returncode:
            raise RuntimeError('Original initial search fixture compilation failed')
        runtime = json.loads(BUILD.read_bytes())['runtime_selection']
        env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(OUT)))
        run = subprocess.run([runtime['mono'], '--config', str(CONFIG), '--optimize=-float32', str(OUT / 'control.exe'), str(PROPOSAL), str(GRAPH), str(PREFIX), str(OUT / 'result.json')], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
        (OUT / 'run.log').write_text(run.stdout + run.stderr)
        marker = '[S006OriginalInitialSearch] result=ControlsOnly sizes=0 liveWorld=0 readiness=0'
        if run.returncode or run.stdout.splitlines().count(marker) != 1:
            raise RuntimeError('Original initial search fixture failed')
        verify(); result = json.loads((OUT / 'result.json').read_bytes())
        counts = [{'size': x['size'], 'found': sum(y.get('found') is True for y in x['rows']), 'total': len(x['rows']), 'historical_matches': sum(y.get('found') is True and y['origin'] == y['historical_proposal'] for y in x['rows'])} for x in result['sizes']]
        receipt = {'status': 'PassedOriginalInitialSearchControls', 'source_pins': pins, 'result_sha256': sha(OUT / 'result.json'), 'counts': counts, 'original_search_controls': ['all-invalid rejected', 'single valid alternative found'], 'scope': 'Synthetic original search controls only; prior candidate run not repeated. Required-preferred behavior is untested because it requires a same-world diagnostic context. No world, registration, live water, physical source validation or readiness.', 'physical_source_validated': False, 'runtime_admission': False, 'mission_ready': False}
        (OUT / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
        print(json.dumps({'status': receipt['status'], 'counts': counts, 'mission_ready': False}))
    except Exception as error:
        (OUT / 'failure.json').write_text(json.dumps({'status': 'Failed', 'error': repr(error), 'mission_ready': False}, indent=2) + '\n')
        raise


if __name__ == '__main__':
    main()
