#!/usr/bin/env python3
"""Replay retained unqualified S006 pad deltas with the current original compiler."""
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's006-current-config-only-candidate-20261106-T22'
OUT = LOGS / 's006-current-typed-pad-replay-20261106-T7'
DELTA = LOGS / 's006-external-config-candidate-20261105-T1/data.json'
GRAPH = LOGS / 's005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
BUILD = LOGS / 's003-current-prefab-import-build-20261104-T1/build.json'
RECIPE = Path(__file__).resolve().parent
SEARCH = LOGS / 's006-original-initial-search-20261106-T1'
PROPOSAL = LOGS / 's006-static-footprint-proposals-20261105-T1/report.json'
SURFACE_GRAPH = ROOT / 'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/source-config-graph.json'
PREFIX = LOGS / 's005-campaign-placement-inputs-20261105-T1/sourceInvalidPrefix.bin'
MONO_CONFIG = LOGS / "s004-current-physical-input-projection-20261104-T1/mono-config.xml"
INITIAL_OWNER = LOGS / 's006-current-historical-footprint-check-20261106-T2/generated/BuildingInitialPlacement.source.cs'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    OUT.mkdir(exist_ok=False)
    try:
        receipt = json.loads((BASE / 'receipt.json').read_bytes())
        if receipt.get('status') != 'CollectedUnqualifiedSourceCandidate' or receipt.get('mission_ready') is not False:
            raise ValueError('Exact current diagnostic source candidate required')
        pins = dict(receipt['source_pins'])
        for path in (Path(__file__), BASE / 'receipt.json', BASE / 'compile.rsp', DELTA, GRAPH, MATRIX, BUILD, SEARCH / 'receipt.json', SEARCH / 'result.json', PROPOSAL, SURFACE_GRAPH, PREFIX, INITIAL_OWNER, MONO_CONFIG, RECIPE / 'SourceS006ExternalDeltaControl.cs', RECIPE / 'SourceS006ExternalDeltaMaterializer.cs'):
            pins[str(path)] = sha(path)
        def verify():
            for path, expected in pins.items():
                if not Path(path).is_file() or sha(path) != expected:
                    raise ValueError('Input changed: ' + path)
        verify()
        search_receipt = json.loads((SEARCH / 'receipt.json').read_bytes())
        if search_receipt.get('result_sha256') != pins[str(SEARCH / 'result.json')] or search_receipt.get('source_pins', {}).get(str(PROPOSAL)) != pins[str(PROPOSAL)]:
            raise ValueError('Exact pinned historical search and proposal join required')
        for path in (SURFACE_GRAPH, PREFIX, INITIAL_OWNER):
            if search_receipt['source_pins'].get(str(path)) != pins[str(path)]:
                raise ValueError('Search owner or input pin differs: ' + str(path))
        delta = json.loads(DELTA.read_bytes())
        if delta.get('kind') != 'S006ExternalConfigDeltaCandidate' or any(delta.get(k) is not False for k in ('runtime_admission', 'source_fit_certified', 'mission_ready')):
            raise ValueError('Explicitly unqualified delta required')
        args = [json.loads(line) for line in (BASE / 'compile.rsp').read_text().splitlines() if line.strip()]
        old = str(BASE / 'S006SourceCandidateControl.cs')
        if args.count(old) != 1:
            raise ValueError('Exact candidate driver required')
        original_control = (RECIPE / 'SourceS006ExternalDeltaControl.cs').read_text()
        anchor = ' return new JObject{["compiled"]=compiled,'
        if original_control.count(anchor) != 1:
            raise ValueError('Exact current control result seam required')
        replacement = ''' var resolvedPads=new JArray();var proposalGroup=((JArray)sourceProposals["sizes"]).OfType<JObject>().Single(x=>(string)x["size"]==setup.SizeId.ToString());var searchGroup=((JArray)sourceSearch["sizes"]).OfType<JObject>().Single(x=>(string)x["size"]==setup.SizeId.ToString());var candidates=(JArray)proposalGroup["proposals"];var found=(JArray)searchGroup["rows"];if(actual==null||candidates.Count!=actual.Structures.Length||found.Count!=actual.Structures.Length)throw new InvalidOperationException("Exact structure search domain required");
 var grid=new Game.Components.GridConfig{Width=2048,Height=1024,CellSize=1f,Origin=new Unity.Mathematics.float3(0,0,0)};
 if(compiled)for(int index=0;index<actual.Structures.Length;index++){var c=(JObject)candidates[index];var s=(JObject)found[index];if((int)c["structure_index"]!=index||(int)s["structure_index"]!=index||(string)c["structure_id"]!=actual.Structures[index].StructureId||(int)c["faction_id"]!=actual.Structures[index].FactionId||s["found"].Type!=JTokenType.Boolean)throw new InvalidOperationException("Exact candidate identity required");bool ok=SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout,actual,index,out var typedPad);bool has=(bool)s["found"];bool contained=false;if(ok&&has){var origin=(JArray)s["origin"];var size=(JArray)c["footprint"];contained=SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout,typedPad,grid,map.Bounds,new RectInt((int)origin[0],(int)origin[1],(int)size[0],(int)size[1]));}resolvedPads.Add(new JObject{["structure_index"]=index,["resolved"]=ok,["pad_id"]=ok?typedPad.PadId:null,["search_found"]=has,["search_origin_contained"]=contained});}
 var surfaceSource=new SourceConfigMaterializer(sourceSurfaceGraph);var surfaceAsset=surfaceSource.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
 if(surfaceAsset==null||!surfaceAsset.TryCreateRuntimeBlobAsset(Unity.Collections.Allocator.Persistent,out var blob))throw new InvalidOperationException("Pinned source surface required");
 var constrained=new JArray();try{ref var surfaceNative=ref blob.Value;if(surfaceNative.Dimensions.x!=grid.Width||surfaceNative.Dimensions.y!=grid.Height||surfaceNative.CellSize!=grid.CellSize)throw new InvalidOperationException("Exact surface grid required");var surface=new Game.Components.MapSurfaceComponent{SurfaceBlob=blob,GridOrigin=surfaceNative.GridOrigin,CellSize=surfaceNative.CellSize,Dimensions=surfaceNative.Dimensions,HasSurfaceData=1};var bytes=File.ReadAllBytes(sourcePrefixPath);if(bytes.Length!=2049*1025*4||!BitConverter.IsLittleEndian)throw new InvalidOperationException("Pinned source prefix required");var prefix=new int[2049*1025];Buffer.BlockCopy(bytes,0,prefix,0,bytes.Length);var occupied=new List<RectInt>();
 for(int index=0;index<actual.Structures.Length;index++){var c=(JObject)candidates[index];var sz=(JArray)c["footprint"];var fp=new Vector2Int((int)sz[0],(int)sz[1]);var runtimeDefinition=new RuntimeDefinition{FootprintCells=fp};if(!SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout,actual,index,out var pad))throw new InvalidOperationException("Resolved typed pad required");var owner=new BuildingRuntimeSpawnCompositionSystemHelper();var ctx=new BuildingRuntimeSpawnCompositionSystemHelper.Context{GetPlacementFootprint=(d,r)=>d.FootprintCells,GetEffectivePlacementRect=(d,at,g,r)=>new RectInt(at,d.FootprintCells),IsPlacementValid=(d,at,foot,r,g,roads,blockers)=>{var rect=new RectInt(at,foot);if(!SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout,pad,g,map.Bounds,rect))return false;if(!BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(rect,g,roads,blockers,true,prefix,2049,1025,null,null,q=>{foreach(var old in occupied)if(q.Overlaps(old))return true;return false;}))return false;var evaluator=new BuildingSurfacePlacementUtilitySystemHelper();return evaluator.TryEvaluateFootprint(surface,at,foot,.5f,45f,out var evaluated)&&evaluated.IsValid;}};
 var structure=actual.Structures[index];var center=Game.Components.GridUtils.WorldToCell(grid,new Unity.Mathematics.float3(structure.SpawnWorldX,0,structure.SpawnWorldZ));var preferred=new Vector2Int(center.x-fp.x/2,center.y-fp.y/2);bool located=owner.TryFindValidInitialBuildingOrigin(ctx,runtimeDefinition,preferred,false,grid,null,default,out var origin,emitCensusEvents:false);constrained.Add(new JObject{["structure_index"]=index,["preferred"]=new JArray(preferred.x,preferred.y),["found"]=located,["origin"]=located?(JToken)new JArray(origin.x,origin.y):JValue.CreateNull(),["pad_id"]=pad.PadId});if(located)occupied.Add(new RectInt(origin,fp));}
 }finally{blob.Dispose();}
 return new JObject{["constrained_search"]=constrained,["resolved_pads"]=resolvedPads,["compiled"]=compiled,'''
        driver = OUT / 'S006TypedPadResolutionControl.cs'
        entry = ' static void Main(string[] a){'
        if original_control.count(entry) != 1:
            raise ValueError('Exact diagnostic source entry required')
        original_control = original_control.replace(entry, ' static JObject sourceProposals,sourceSearch,sourceSurfaceGraph;static string sourcePrefixPath;\n static void Main(string[] a){sourceProposals=JObject.Parse(File.ReadAllText(a[7]));sourceSearch=JObject.Parse(File.ReadAllText(a[8]));sourceSurfaceGraph=JObject.Parse(File.ReadAllText(a[9]));sourcePrefixPath=a[10];', 1)
        driver.write_text(original_control.replace(anchor, replacement, 1)); pins[str(driver)] = sha(driver)
        initial = [x for x in args if x.endswith('/330_BuildingInitialPlacement.source.cs')]
        if len(initial) != 1: raise ValueError('Exact current initial-search replacement required')
        args = [str(driver) if x == old else str(INITIAL_OWNER) if x == initial[0] else '-out:' + str(OUT / 'control.exe') if x.startswith('-out:') else x for x in args]
        args.append(str(RECIPE / 'SourceS006ExternalDeltaMaterializer.cs'))
        rsp = OUT / 'compile.rsp'; rsp.write_text(''.join(json.dumps(x) + '\n' for x in args))
        csc = next(Path(x) for x in pins if x.endswith('/Roslyn/bincore/csc.dll'))
        build = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], cwd=ROOT, capture_output=True, text=True, timeout=300)
        (OUT / 'compile.log').write_text(build.stdout + build.stderr)
        if build.returncode:
            raise RuntimeError('Current original compiler replay failed to compile')
        runtime = json.loads(BUILD.read_bytes())['runtime_selection']; env = os.environ.copy()
        env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(OUT)))
        run = subprocess.run([runtime['mono'], '--config', str(MONO_CONFIG), '--optimize=-float32', str(OUT / 'control.exe'), str(GRAPH), str(MATRIX), str(OUT / 'result.json'), str(OUT / 'layout.json'), '1', str(DELTA), pins[str(DELTA)], str(PROPOSAL), str(SEARCH / 'result.json'), str(SURFACE_GRAPH), str(PREFIX)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
        (OUT / 'run.log').write_text(run.stdout + run.stderr)
        if run.returncode or run.stdout.splitlines().count('[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0') != 1:
            raise RuntimeError('Current typed-pad source replay failed')
        verify(); result = json.loads((OUT / 'result.json').read_bytes())
        rows = []
        for row in result['rows']:
            p = row['disposable_pad_projection']
            pads = p['resolved_pads']
            rows.append({'size': row['size'], 'compiled': p['compiled'], 'setup_hash': p['setup_hash'], 'expected_setup_hash': p['expected_setup_hash'], 'negative_rejections': p['negative_rejections'], 'byte_negative_rejections': p['byte_negative_rejections'], 'structures': len(pads), 'pads_resolved': sum(x['resolved'] is True for x in pads), 'prior_search_origins_contained': sum(x['search_origin_contained'] is True for x in pads), 'constrained_origins_found': sum(x['found'] is True for x in p['constrained_search'])})
        report = {'status': 'CollectedUnqualifiedCurrentTypedPadReplay', 'source_pins': pins, 'result_sha256': sha(OUT / 'result.json'), 'rows': rows, 'scope': 'Current original compiler, typed containment and initial search owners with retained unqualified delta, inactive surface/mask and fixture occupancy. No world, registration, source-fit or native readiness.', 'runtime_admission': False, 'mission_ready': False}
        (OUT / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps({'status': report['status'], 'rows': rows, 'mission_ready': False}))
    except Exception as error:
        (OUT / 'failure.json').write_text(json.dumps({'status': 'Failed', 'error': repr(error), 'mission_ready': False}, indent=2) + '\n')
        raise


if __name__ == '__main__':
    main()
