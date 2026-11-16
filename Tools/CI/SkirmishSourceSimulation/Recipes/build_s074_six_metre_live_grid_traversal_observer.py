#!/usr/bin/env python3
"""Build an opt-in, source-only S074 live grid/surface observation executable."""
from __future__ import annotations
import argparse, hashlib, json, subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE_BUILD = ROOT / "T Logs/SkirmishProduction/s074-six-metre-source-build-20261114-r4"
LAYOUT = ROOT / "T Logs/SkirmishProduction/s074-provisional-six-metre-spawn-pads-20261113-r1/layout.json"
DEFAULT_OUT = ROOT / "T Logs/SkirmishProduction/s074-six-metre-live-grid-observer-20261114-r1"
MARKER = "while(opening.TryBeginMember(out var member))"

def sha(p: Path) -> str: return hashlib.sha256(p.read_bytes()).hexdigest()

def main() -> int:
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--build',type=Path,default=BASE_BUILD);ap.add_argument('--layout',type=Path,default=LAYOUT);ap.add_argument('--out',type=Path,default=DEFAULT_OUT);a=ap.parse_args()
    build=a.build.resolve(strict=True);layout_path=a.layout.resolve(strict=True);out=a.out.absolute()
    if out.exists(): raise SystemExit(f'Refusing non-fresh output: {out}')
    brp=build/'build.json';receipt=json.loads(brp.read_text())
    if receipt.get('kind')!='SkirmishSourceSimulationBuild' or receipt.get('status')!='Passed' or receipt.get('mission')!='S074' or receipt.get('seed')!=104803 or receipt.get('compile_exit')!=0: raise SystemExit('Passed S074 seed-104803 candidate source build required')
    pins=receipt.get('source_and_reference_pins')
    if not isinstance(pins,dict) or not pins: raise SystemExit('Build source pins absent')
    for name,digest in pins.items():
        p=Path(name)
        if not p.is_file() or sha(p)!=digest: raise SystemExit('Stale source build pin: '+name)
    layout=json.loads(layout_path.read_text());wanted={'s074.provisional.spawn.infantry.player.pad','s074.provisional.spawn.infantry.enemy.pad','s074.provisional.spawn.vehicle.player.pad','s074.provisional.spawn.vehicle.enemy.pad'}
    rows=[p for p in layout['pads'] if p['PadId'] in wanted]
    if len(rows)!=4 or {p['PadId'] for p in rows}!=wanted: raise SystemExit('Exact four six-metre pad descriptors required')
    out.mkdir(parents=True)
    generated=build/'source/generated/StartingWorld.cs';source=generated.read_text()
    if source.count(MARKER)!=1: raise SystemExit('Expected exact first-unit insertion point')
    variant=out/'StartingWorld.S074SixMetreLiveGridObserver.cs';variant.write_text(source.replace(MARKER,'S074SixMetreLiveGridObserver.Capture(Entities,GridEntity);'+MARKER))
    helper=out/'S074SixMetreLiveGridObserver.cs';helper.write_text(observer_source(rows))
    argv=[json.loads(s) for s in (build/'compile.rsp').read_text().splitlines() if s]
    ix=[i for i,x in enumerate(argv) if x==str(generated)];ox=[i for i,x in enumerate(argv) if isinstance(x,str) and x.startswith('-out:')]
    if len(ix)!=1 or len(ox)!=1: raise SystemExit('Compiler response source/output must be unique')
    argv[ix[0]]=str(variant);argv.append(str(helper));argv[ox[0]]='-out:'+str(out/'SourceSimulation.exe')
    rsp=out/'compile.rsp';rsp.write_text('\n'.join(json.dumps(x) for x in argv)+'\n')
    dotnet=next((Path(p) for p in pins if Path(p).name=='dotnet'),None);csc=next((Path(p) for p in pins if Path(p).name=='csc.dll'),None)
    if not dotnet or not csc: raise SystemExit('Build-pinned compiler tools missing')
    result=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],capture_output=True,text=True,timeout=120)
    (out/'compile.log').write_text(result.stdout+result.stderr)
    if result.returncode: raise SystemExit(f'Observer compile failed {result.returncode}; see {out}/compile.log')
    build_variant=dict(receipt);build_variant['executable']={'path':str(out/'SourceSimulation.exe'),'sha256':sha(out/'SourceSimulation.exe'),'bytes':(out/'SourceSimulation.exe').stat().st_size}
    build_variant['source_and_reference_pins']=dict(pins,**{str(p):sha(p) for p in (variant,helper,rsp,out/'compile.log',layout_path)})
    build_variant['diagnostic_variant']={'kind':'S074SixMetreLiveGridSurfaceObservation','scope':'Immediately before first original opening unit placement; four 17x17 windows; reports original traversal-owner results, current walkable/blocked/occupied bits and UnitGrid/UnitFootprint rectangles','base_build':{'path':str(brp),'sha256':sha(brp)},'layout':{'path':str(layout_path),'sha256':sha(layout_path)},'placement_logic_changed':False,'mission_data_changed':False,'world_scope':'source-only qualified runner','native_qualification_claim':False,'source_fit_certified':False,'mission_ready':False}
    (out/'build.json').write_text(json.dumps(build_variant,indent=2)+'\n')
    print(json.dumps({'status':'Passed','build':str(out/'build.json'),'exe':str(out/'SourceSimulation.exe'),'compile_log':str(out/'compile.log')},indent=2));return 0

def observer_source(pads):
    data=[{'pad_id':p['PadId'],'x':p['CenterX'],'z':p['CenterZ'],'width':p['WidthMetres'],'depth':p['DepthMetres']} for p in pads]
    template=r'''using System;using Newtonsoft.Json.Linq;using Game.Components;using Game.Configs;using Game.Runtime.Pathfinding;using Unity.Entities;using Unity.Collections;using Unity.Mathematics;
namespace Game.Runtime { internal static class S074SixMetreLiveGridObserver {
const int Radius=8; static readonly string PadsJson=__PADS__;
internal static void Capture(EntityManager em,Entity gridEntity){
if(!em.Exists(gridEntity)||!em.HasComponent<GridConfig>(gridEntity)||!em.HasBuffer<GridWalkable>(gridEntity)||!em.HasComponent<DynamicBlockerComponent>(gridEntity)||!em.HasComponent<DynamicOccupancyComponent>(gridEntity))throw new InvalidOperationException("S074 observer requires live grid state");
using(var sq=em.CreateEntityQuery(typeof(MapSurfaceComponent))){if(sq.CalculateEntityCount()!=1)throw new InvalidOperationException("S074 observer requires exactly one source surface");var g=em.GetComponentData<GridConfig>(gridEntity);var walk=em.GetBuffer<GridWalkable>(gridEntity);var blocked=em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;var occupied=em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;var surface=sq.GetSingleton<MapSurfaceComponent>();var traversal=new MapSurfaceTraversalValidation();var pads=(JArray)JToken.Parse(PadsJson);var windows=new JArray();var reservations=new JArray();
using(var q=em.CreateEntityQuery(typeof(UnitGrid),typeof(UnitFootprint)))using(var es=q.ToEntityArray(Allocator.Temp)){foreach(var e in es){var cell=em.GetComponentData<UnitGrid>(e).Cell;var size=UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(e).Size);var min=UnitFootprintUtility.GetMinCell(cell,size);var near=false;foreach(var p in pads){var c=GridUtils.WorldToCell(g,new float3((float)p["x"],0,(float)p["z"]));if(math.abs(min.x-c.x)<=Radius+size.x&&math.abs(min.y-c.y)<=Radius+size.y){near=true;break;}}if(near)reservations.Add(new JObject{["entity_index"]=e.Index,["entity_version"]=e.Version,["min_cell"]=new JArray(min.x,min.y),["size_cells"]=new JArray(size.x,size.y)});}}
foreach(var p in pads){var c=GridUtils.WorldToCell(g,new float3((float)p["x"],0,(float)p["z"]));var cells=new JArray();for(int z=c.y-Radius;z<=c.y+Radius;z++)for(int x=c.x-Radius;x<=c.x+Radius;x++){int i=z*g.Width+x;bool inside=x>=0&&z>=0&&x<g.Width&&z<g.Height;cells.Add(new JObject{["x"]=x,["z"]=z,["inside_grid"]=inside,["walkable"]=inside&&walk[i].Value!=0,["blocked"]=inside&&blocked.IsSet(i),["occupied"]=inside&&occupied.IsSet(i),["infantry_1x1_surface_traversable"]=traversal.CanTraverseFootprint(surface,1,g,new int2(x,z),new int2(1,1),false),["vehicle_3x3_surface_traversable"]=traversal.CanTraverseFootprint(surface,1,g,new int2(x,z),new int2(3,3),true)});}windows.Add(new JObject{["pad_id"]=(string)p["pad_id"],["center_cell"]=new JArray(c.x,c.y),["source_world_center"]=new JArray((float)p["x"],(float)p["z"]),["source_extent_metres"]=new JArray((float)p["width"],(float)p["depth"]),["radius_cells"]=Radius,["cells"]=cells});}
Console.WriteLine("[S074SixMetreLiveGridObserver] "+new JObject{["kind"]="S074SixMetreLiveGridSurfaceObservation",["scope"]="Diagnostic only; original MapSurfaceTraversalValidation queries; no placement mutation or source-fit/readiness conclusion",["stage"]="Immediately before first original opening unit placement after authored buildings",["grid_width"]=g.Width,["grid_height"]=g.Height,["cell_size"]=g.CellSize,["grid_origin"]=new JArray(g.Origin.x,g.Origin.y,g.Origin.z),["pads"]=windows,["nearby_unitgrid_unitfootprint_rectangles"]=reservations}.ToString(Newtonsoft.Json.Formatting.None));}}
}}'''
    return template.replace('__PADS__',json.dumps(json.dumps(data,separators=(',',':'))))

if __name__=='__main__': raise SystemExit(main())
