#!/usr/bin/env python3
"""Generate a disposable source-host-only CanPlace memo candidate."""
import argparse, hashlib, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
SOURCE=Path("Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs")
ANCHOR_CONTEXT='''                    FootprintSize = footprint.Size, IsVehicle = vehicle, ManualMove = manual, FactionId = Factions[index]
                };'''
ANCHOR_ROUTE='''                GridPathRoutingKernel.Route(ref context, request, ref state);'''
ANCHOR_CANPLACE='''            public bool CanPlace(int x, int z, int cx, int cz) => UnitPathPlacementValidation.CanPlaceForPathing(
                Job.Grid, Job.Walkable, Job.DynamicBlocked, Job.FriendlyPassFactionIds, Job.Occupied,
                Job.LiveUnitEntities, Job.LiveUnitGrids, Job.LiveUnitFootprints, Job.LiveUnitManualGroupMembers,
                MovingEntity, new int2(x, z), FootprintSize, new int2(cx, cz), IsVehicle, ManualMove, FactionId,
                IgnoredEntity, IgnoredCell, IgnoredSize, Job.LiveUnitCoverage);'''

KEY=r'''        private struct CanPlaceMemoKey : System.IEquatable<CanPlaceMemoKey>
        {
            public Entity Moving, Ignored; public int2 Target, Current, Footprint, IgnoredCell, IgnoredSize;
            public int Width, Height; public float CellSize; public float3 Origin; public byte Faction; public bool Vehicle, Manual;
            public bool Equals(CanPlaceMemoKey other) => Moving.Equals(other.Moving) && Ignored.Equals(other.Ignored) && Target.Equals(other.Target) && Current.Equals(other.Current) && Footprint.Equals(other.Footprint) && IgnoredCell.Equals(other.IgnoredCell) && IgnoredSize.Equals(other.IgnoredSize) && Width==other.Width && Height==other.Height && CellSize.Equals(other.CellSize) && Origin.Equals(other.Origin) && Faction==other.Faction && Vehicle==other.Vehicle && Manual==other.Manual;
            public override bool Equals(object value) => value is CanPlaceMemoKey other && Equals(other);
            public override int GetHashCode(){unchecked{int h=Moving.GetHashCode();h=h*31+Ignored.GetHashCode();h=h*31+Target.GetHashCode();h=h*31+Current.GetHashCode();h=h*31+Footprint.GetHashCode();h=h*31+IgnoredCell.GetHashCode();h=h*31+IgnoredSize.GetHashCode();h=h*31+Width;h=h*31+Height;h=h*31+CellSize.GetHashCode();h=h*31+Origin.GetHashCode();h=h*31+Faction;h=h*31+(Vehicle?1:0);return h*31+(Manual?1:0);}}
        }
'''

def transform(source:str)->str:
    for anchor in (ANCHOR_CONTEXT,ANCHOR_ROUTE,ANCHOR_CANPLACE):
        if source.count(anchor)!=1: raise ValueError("Fail-closed exact PathfindBatchJob anchor required")
    if "CanPlaceMemoKey" in source or "WARLINE_SOURCE_CANPLACE_MEMO" in source: raise ValueError("Memo instrumentation already present")
    source=source.replace("using Unity.Burst;","using Unity.Burst;\nusing System.Collections.Generic;",1)
    # Cache state belongs to this exact synchronous Execute. The native context also pins all non-scalar inputs.
    init=ANCHOR_CONTEXT.replace("\n                };",''',
                    CanPlaceMemo = System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_CANPLACE_MEMO") == "1" ? new Dictionary<CanPlaceMemoKey,bool>() : null,
                    ShadowVerifyCanPlaceMemo = System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_CANPLACE_MEMO_SHADOW") == "1"
                };''')
    source=source.replace(ANCHOR_CONTEXT,init,1)
    route='''                try
                {
                    GridPathRoutingKernel.Route(ref context, request, ref state);
                }
                finally
                {
                    // Request-scoped cache cleanup is guaranteed even if routing or shadow verification throws.
                    context.CanPlaceMemo?.Clear();
                }'''
    source=source.replace(ANCHOR_ROUTE,route,1)
    original=ANCHOR_CANPLACE
    direct=original.replace("public bool CanPlace(int x, int z, int cx, int cz) =>", "private bool CanPlaceOriginal(int x, int z, int cx, int cz) =>")
    method='''            public Dictionary<CanPlaceMemoKey,bool> CanPlaceMemo;
            public bool ShadowVerifyCanPlaceMemo;
            private bool CanPlaceOriginal(int x, int z, int cx, int cz) => UnitPathPlacementValidation.CanPlaceForPathing(
                Job.Grid, Job.Walkable, Job.DynamicBlocked, Job.FriendlyPassFactionIds, Job.Occupied,
                Job.LiveUnitEntities, Job.LiveUnitGrids, Job.LiveUnitFootprints, Job.LiveUnitManualGroupMembers,
                MovingEntity, new int2(x, z), FootprintSize, new int2(cx, cz), IsVehicle, ManualMove, FactionId,
                IgnoredEntity, IgnoredCell, IgnoredSize, Job.LiveUnitCoverage);
            public bool CanPlace(int x,int z,int cx,int cz)
            {
                if(CanPlaceMemo==null)return CanPlaceOriginal(x,z,cx,cz);
                var key=new CanPlaceMemoKey{Moving=MovingEntity,Ignored=IgnoredEntity,Target=new int2(x,z),Current=new int2(cx,cz),Footprint=FootprintSize,IgnoredCell=IgnoredCell,IgnoredSize=IgnoredSize,Width=Job.Grid.Width,Height=Job.Grid.Height,CellSize=Job.Grid.CellSize,Origin=Job.Grid.Origin,Faction=FactionId,Vehicle=IsVehicle,Manual=ManualMove};
                if(CanPlaceMemo.TryGetValue(key,out bool cached))
                {
                    if(ShadowVerifyCanPlaceMemo){bool actual=CanPlaceOriginal(x,z,cx,cz);if(actual!=cached){CanPlaceMemo.Clear();throw new System.InvalidOperationException("CanPlace memo shadow mismatch; request cache cleared");}}
                    return cached;
                }
                bool result=CanPlaceOriginal(x,z,cx,cz);
                if(CanPlaceMemo.Count<4096)CanPlaceMemo.Add(key,result); // Saturated cache bypasses safely.
                return result;
            }'''
    source=source.replace(original,method,1)
    source=source.replace("        private unsafe struct NativeSearchContext : IPathSearchContext",KEY+"\n        private unsafe struct NativeSearchContext : IPathSearchContext",1)
    return source

def generate(repo:Path,output:Path)->dict:
    root=repo.resolve(strict=True); p=root/SOURCE; raw=p.read_bytes(); rendered=transform(raw.decode()).encode()
    if p.read_bytes()!=raw: raise ValueError("Pinned source changed during generation")
    output.mkdir(parents=True,exist_ok=False); name="PathfindBatchJob.CanPlaceMemo.SourceHost.cs";(output/name).write_bytes(rendered)
    receipt={"kind":"experimental-source-host-only-canplace-memo","source":str(SOURCE),"source_sha256":hashlib.sha256(raw).hexdigest(),"candidate":name,"candidate_sha256":hashlib.sha256(rendered).hexdigest(),"output_identity":"sha256(candidate bytes)","opt_in":"WARLINE_SOURCE_CANPLACE_MEMO=1","shadow_opt_in":"WARLINE_SOURCE_CANPLACE_MEMO_SHADOW=1","capacity":4096,"scope":"one synchronous PathfindBatchJob Execute; context-bound and scalar-complete key; full-cache lookups bypass", "shadow":"each hit recomputes original; mismatch clears request memo then throws; Execute finally clears on all exits", "original_route_unchanged":True,"status_or_command_mutation":False,"build_inputs_changed":False,"mission_run":False}
    (output/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n");return receipt

if __name__=="__main__":
    a=argparse.ArgumentParser();a.add_argument("--repo",type=Path,default=ROOT);a.add_argument("--output",type=Path,required=True);x=a.parse_args();print(json.dumps(generate(x.repo,x.output),indent=2))
