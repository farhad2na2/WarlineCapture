using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Components;

static class SpawnBlockers
{
    internal static object Run(JsonElement scenario,SpawnArtifacts artifacts)
    {
        SpawnJson.Shape(scenario,new[]{"id","kind","grid","storage","owner","grid_identity","new","existing","removed","emit_arrays"},"expect");
        var grid=SpawnJson.Facts<BlockerGrid>(scenario.GetProperty("grid"));int count=SpawnProgram.GridSize(grid);bool arrays=scenario.GetProperty("emit_arrays").GetBoolean();SpawnJson.Need(!arrays||count<=65536,"Large arrays must use hash summary");
        SpawnProgram.Charge(count);
        var input=scenario.GetProperty("storage");SpawnJson.Shape(input,new[]{"counts","blocked","friendly_pass","friendly_pass_created","unavailable_arrays"});
        SpawnJson.Need(input.GetProperty("unavailable_arrays").EnumerateArray().Select(v=>v.GetString()).SequenceEqual(new[]{"walkable","occupied","reserved"}),"Explicit unrelated unavailable arrays required");
        var storage=new SpawnStorage{Counts=artifacts.Counts(input.GetProperty("counts"),count),Blocked=artifacts.Bytes(input.GetProperty("blocked"),count,true),Friendly=artifacts.Bytes(input.GetProperty("friendly_pass"),input.GetProperty("friendly_pass_created").GetBoolean()?count:0,false),HasFriendly=input.GetProperty("friendly_pass_created").GetBoolean(),Walkable=Array.Empty<byte>(),Occupied=Array.Empty<byte>(),Reserved=Array.Empty<byte>()};
        var update=Update(scenario,grid,ref storage);return new{id=SpawnJson.Id(scenario.GetProperty("id")),kind="blockers",update,storage=Snapshot(storage,arrays)};
    }
    internal static object Snapshot(SpawnStorage storage,bool arrays)
    {
        byte[] counts=new byte[storage.Counts.Length*4];for(int i=0;i<storage.Counts.Length;i++)System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(counts.AsSpan(i*4,4),storage.Counts[i]);
        return new{counts_sha256=SpawnArtifacts.Hash(counts),blocked_sha256=SpawnArtifacts.Hash(storage.Blocked),friendly_pass_sha256=SpawnArtifacts.Hash(storage.Friendly),counts=arrays?storage.Counts:null,blocked=arrays?storage.Blocked.Select(v=>(int)v).ToArray():null,friendly_pass=arrays?storage.Friendly.Select(v=>(int)v).ToArray():null,unavailable_arrays=new[]{"walkable","occupied","reserved"}};
    }
    internal static object Update(JsonElement value,BlockerGrid grid,ref SpawnStorage storage)
    {
        var owner=SpawnJson.Facts<BlockerEpochState>(value.GetProperty("owner"));var identity=SpawnJson.Facts<BlockerOwnerIdentity>(value.GetProperty("grid_identity"));
        SpawnJson.Need(identity.Index>=0&&identity.Version>0&&identity.StorageEpoch==owner.StorageEpoch,"Exact grid identity/epoch required");
        SpawnJson.Need(owner.WriteInProgress<=1&&owner.EpochExhausted<=1,"Owner flags must be binary");
        var fresh=SpawnJson.Array(value.GetProperty("new"),4096);var existing=SpawnJson.Array(value.GetProperty("existing"),4096);var removed=SpawnJson.Array(value.GetProperty("removed"),4096);
        SpawnJson.Need(fresh.Length+existing.Length+removed.Length<=4096,"Aggregate blocker limit");
        bool rebuild=StaticBlockerKernel.NeedsRebuild(grid,owner);var ids=new HashSet<string>();
        foreach(var item in existing.Concat(removed))
        {var history=SpawnJson.Facts<BlockerHistory>(item.GetProperty("history"));if(!StaticBlockerKernel.SameOwner(history.Owner,identity))rebuild=true;}
        if(rebuild){SpawnProgram.Charge((long)grid.Width*grid.Height*3);StaticBlockerKernel.Invalidate(ref owner);StaticBlockerKernel.Clear(ref storage);}
        StaticBlockerKernel.BeginWrite(ref owner);identity.StorageEpoch=owner.StorageEpoch;var changes=new List<object>();long work=0;
        foreach(var phase in new[]{("new",fresh),("existing",existing),("removed",removed)})
        foreach(var item in phase.Item2)
        {
            if(phase.Item1=="new")SpawnJson.Shape(item,new[]{"id","origin","size","faction","has_opening","opening","had_opening","previous_opening"});
            else if(phase.Item1=="existing")SpawnJson.Shape(item,new[]{"id","origin","size","faction","has_opening","opening","history","had_opening","previous_opening"});
            else SpawnJson.Shape(item,new[]{"id","history","had_opening","previous_opening"});
            string id=SpawnJson.Id(item.GetProperty("id"));SpawnJson.Need(ids.Add(id),"Entity must occur once across captured query phases");
            var history=phase.Item1=="new"?default:SpawnJson.Facts<BlockerHistory>(item.GetProperty("history"));
            if(phase.Item1!="new")
            {
                long width=Math.Max(0L,Math.Min((long)grid.Width,(long)history.Rect.MaxX)-Math.Max(0L,history.Rect.MinX));
                long height=Math.Max(0L,Math.Min((long)grid.Height,(long)history.Rect.MaxZ)-Math.Max(0L,history.Rect.MinZ));
                long cost=width*height*3;work+=cost;SpawnProgram.Charge(cost);SpawnJson.Need(work<=128L*1024*1024,"Historical blocker work exceeds bounded profile");
            }
            bool hadOpening=item.GetProperty("had_opening").GetBoolean();var oldOpening=SpawnJson.Facts<BlockerOpening>(item.GetProperty("previous_opening"));if(!hadOpening)oldOpening=default;
            BlockerUpdateResult result;bool hasOpening=false;var opening=default(BlockerOpening);
            if(phase.Item1=="removed")result=StaticBlockerKernel.ApplyRemoved(grid,ref storage,rebuild,history,hadOpening,oldOpening);
            else
            {
                int[] origin=SpawnJson.Cell(item.GetProperty("origin")),size=SpawnJson.Cell(item.GetProperty("size"),1024);var rect=StaticBlockerKernel.Bounds(origin[0],origin[1],size[0],size[1]);
                long area=(long)Math.Max(1,size[0])*Math.Max(1,size[1]);work+=area*3;SpawnProgram.Charge(area*3);SpawnJson.Need(work<=128L*1024*1024,"Blocker rectangle work exceeds bounded profile");
                byte faction=item.GetProperty("faction").GetByte();hasOpening=item.GetProperty("has_opening").GetBoolean();opening=SpawnJson.Facts<BlockerOpening>(item.GetProperty("opening"));if(!hasOpening)opening=default;
                result=phase.Item1=="new"?StaticBlockerKernel.ApplyNew(grid,ref storage,rect,faction,hasOpening,opening,identity,out history):StaticBlockerKernel.ApplyExisting(grid,ref storage,rebuild,rect,faction,hasOpening,opening,hadOpening,oldOpening,identity,ref history);
            }
            bool finalOpening=hadOpening;var finalValue=oldOpening;
            switch(result.PreviousOpeningChange){case BlockerOpeningChange.Add:case BlockerOpeningChange.Set:finalOpening=true;finalValue=opening;break;case BlockerOpeningChange.Remove:finalOpening=false;finalValue=default;break;}
            changes.Add(new{id,phase=phase.Item1,applied=result.Applied,opening_change=result.PreviousOpeningChange,has_history=phase.Item1!="removed",history,has_previous_opening=finalOpening,previous_opening=finalValue});
        }
        StaticBlockerKernel.Commit(grid,ref owner);return new{rebuild,owner,changes};
    }
}
