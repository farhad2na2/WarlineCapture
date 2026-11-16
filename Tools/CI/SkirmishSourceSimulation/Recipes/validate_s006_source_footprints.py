#!/usr/bin/env python3
"""Run original surface and rectangle checks over S006 static proposals."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
BASE=ROOT/'T Logs/SkirmishProduction/s003-actual-importer-current-fixture-20261105-T7'
BUILD=ROOT/'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
PROPOSALS=ROOT/'T Logs/SkirmishProduction/s006-static-footprint-proposals-20261105-T1/report.json'
INPUT=ROOT/'T Logs/SkirmishProduction/s005-campaign-placement-inputs-20261105-T1'
GRAPH=ROOT/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/source-config-graph.json'
MONO_CONFIG=ROOT/'T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml'
CONTROL=ROOT/'Tools/CI/SkirmishSourceSimulation/Tests/S003RefreshedPrefabImportControl.cs'
SOURCE=r'''using System;using System.IO;using System.Collections.Generic;using Game.Components;using Game.Configs;using Game.Runtime;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Unity.Collections;using Unity.Mathematics;using UnityEngine;
internal static class S006SourceFootprintOwnerControl{
 static void Main(string[] args){var proposal=JObject.Parse(File.ReadAllText(args[0]));var graph=JObject.Parse(File.ReadAllText(args[1]));var source=new SourceConfigMaterializer(graph);var asset=source.ByPath<MapSurfaceDataAsset>("Assets/Game/Data/MapSurfaces/Match_Map_MapSurfaceData.asset");
  if(asset==null||!asset.TryCreateRuntimeBlobAsset(Allocator.Persistent,out var blob))throw new Exception("Actual source surface unavailable");
  try{ref var native=ref blob.Value;if(native.Dimensions.x!=2048||native.Dimensions.y!=1024||native.CellSize!=1f)throw new Exception("Source surface frame differs");
   var surface=new MapSurfaceComponent{SurfaceBlob=blob,GridOrigin=native.GridOrigin,CellSize=native.CellSize,Dimensions=native.Dimensions,HasSurfaceData=1};
   var grid=new GridConfig{Width=2048,Height=1024,CellSize=1f,Origin=new float3(0,0,0)};var bytes=File.ReadAllBytes(args[2]);if(bytes.Length!=2049*1025*4||!BitConverter.IsLittleEndian)throw new Exception("Original prefix mismatch");var prefix=new int[2049*1025];Buffer.BlockCopy(bytes,0,prefix,0,bytes.Length);
   var negativeEvaluator=new BuildingSurfacePlacementUtilitySystemHelper();if(BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(new RectInt(-1,0,1,1),grid,null,default,true,prefix,2049,1025,null,null,null)||negativeEvaluator.TryEvaluateFootprint(surface,new Vector2Int(-1,0),new Vector2Int(1,1),.5f,45f,out _))throw new Exception("Out-of-grid owner controls failed");
   var output=new JArray();foreach(JObject group in (JArray)proposal["sizes"]){var occupied=new List<RectInt>();var rows=new JArray();foreach(JObject p in (JArray)group["proposals"]){
    if(p["proposed_origin"].Type==JTokenType.Null){rows.Add(new JObject{["structure_index"]=p["structure_index"],["attempted"]=false});continue;}
    int x=(int)p["proposed_origin"][0],z=(int)p["proposed_origin"][1],w=(int)p["footprint"][0],h=(int)p["footprint"][1];var rect=new RectInt(x,z,w,h);
    bool originalRect=BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(rect,grid,null,default,true,prefix,2049,1025,null,null,r=>{foreach(var old in occupied)if(r.Overlaps(old))return true;return false;});
    var evaluator=new BuildingSurfacePlacementUtilitySystemHelper();bool surfaceRead=evaluator.TryEvaluateFootprint(surface,new Vector2Int(x,z),new Vector2Int(w,h),.5f,45f,out var evaluated);
    rows.Add(new JObject{["structure_index"]=p["structure_index"],["original_rect_valid"]=originalRect,["original_surface_read"]=surfaceRead,["original_surface_valid"]=surfaceRead&&evaluated.IsValid,["height_delta"]=surfaceRead?(JToken)new JValue(evaluated.MaxFootprintHeightDelta):JValue.CreateNull(),["max_slope"]=surfaceRead?(JToken)new JValue(evaluated.MaxFootprintSlopeDegrees):JValue.CreateNull()});occupied.Add(rect);}
    output.Add(new JObject{["size"]=group["size"],["rows"]=rows});}
   var report=new JObject{["kind"]="S006OriginalSourceFootprintChecks",["scope"]="Original rectangle and full surface owners on retained inactive Campaign inputs; no live occupancy, active water, typed pad or placement registration",["outside_grid_negative_controls_passed"]=true,["sizes"]=output,["mission_ready"]=false};File.WriteAllText(args[3],report.ToString(Formatting.Indented));
   Console.WriteLine("[S006SourceFootprintOwners] result=Collected sizes=3 liveWorld=0 readiness=0");}
  finally{blob.Dispose();}}}
'''


def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);p.add_argument('--fixture',type=Path,default=BASE);p.add_argument('--proposals',type=Path,default=PROPOSALS);options=p.parse_args();out=options.output.resolve();base=options.fixture.resolve(strict=True);proposal_path=options.proposals.resolve(strict=True)
    if not out.is_relative_to(ROOT):raise ValueError('Repository output required')
    if not base.is_relative_to(ROOT) or not proposal_path.is_relative_to(ROOT):raise ValueError('Repository evidence required')
    out.mkdir(parents=True,exist_ok=False);fixture=json.loads((base/'receipt.json').read_bytes());proposals=json.loads(proposal_path.read_bytes());graph=json.loads(GRAPH.read_bytes())
    if fixture.get('status')!='PassedDisposableS003ImporterFixture' or proposals.get('kind')!='S006StaticFootprintProposals' or proposals.get('mission_ready') is not False:raise ValueError('Exact source-only evidence required')
    pins=dict(fixture['source_pins'])
    for group in (proposals['input_sha256'],graph['source_pins']):
        for name,digest in group.items():
            if name in pins and pins[name]!=digest:raise ValueError('Conflicting source pin: '+name)
            pins[name]=digest
    for path in (base/'receipt.json',base/'compile.rsp',BUILD,proposal_path,GRAPH,MONO_CONFIG,Path(__file__).resolve()):pins[str(path)]=sha(path)
    def verify():
        for name,digest in pins.items():
            if not Path(name).is_file() or sha(Path(name))!=digest:raise ValueError('Pinned source changed: '+name)
    verify();control=out/'S006SourceFootprintOwnerControl.cs';control.write_text(SOURCE)
    args=[json.loads(line) for line in (base/'compile.rsp').read_text().splitlines() if line.strip()]
    if args.count(str(CONTROL))!=1:raise ValueError('Exact source compile fixture required')
    args=[str(control) if x==str(CONTROL) else '-main:S006SourceFootprintOwnerControl' if x.startswith('-main:') else '-out:'+str(out/'control.exe') if x.startswith('-out:') else x for x in args]
    rsp=out/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in args));csc=next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compile_run=subprocess.run(['dotnet',str(csc),'@'+str(rsp)],capture_output=True,text=True,timeout=300);(out/'compile.log').write_text(compile_run.stdout+compile_run.stderr)
    if compile_run.returncode:raise RuntimeError('Source footprint owner harness failed to compile')
    runtime=json.loads(BUILD.read_bytes())['runtime_selection'];env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((str(BUILD.parent/'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(out)))
    run=subprocess.run([runtime['mono'],'--config',str(MONO_CONFIG),'--optimize=-float32',str(out/'control.exe'),str(proposal_path),str(GRAPH),str(INPUT/'sourceInvalidPrefix.bin'),str(out/'result.json')],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/'run.log').write_text(run.stdout+run.stderr)
    marker='[S006SourceFootprintOwners] result=Collected sizes=3 liveWorld=0 readiness=0'
    if run.returncode or run.stdout.splitlines().count(marker)!=1:raise RuntimeError('Original footprint owner control failed')
    verify();result=json.loads((out/'result.json').read_bytes());(out/'receipt.json').write_text(json.dumps({'status':'CollectedOriginalSourceFootprintChecks','marker':marker,'result_sha256':sha(out/'result.json'),'source_pins':pins,'mission_ready':False},indent=2)+'\n')
    print(json.dumps({'kind':result['kind'],'sizes':[{'size':r['size'],'checks':len(r['rows']),'passed':sum(x.get('original_rect_valid') is True and x.get('original_surface_valid') is True for x in r['rows'])} for r in result['sizes']],'mission_ready':False}))


if __name__=='__main__':main()
