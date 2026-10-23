using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Spawning;

static class SpawnSequence
{
    sealed class Existing{internal string Id;internal int X,Z,Sx,Sz;}
    internal static object Run(JsonElement scenario,SpawnArtifacts artifacts)
    {
        SpawnJson.Shape(scenario,new[]{"id","kind","grid","storage","blocker_update","existing","actors","surface_profiles","has_match","surface_count","emit_arrays","facts_scope","placement_mode"},"expect");
        string scope=scenario.GetProperty("facts_scope").GetString();SpawnJson.Need(scope=="SyntheticSnapshot"||scope=="NativeCapturedSnapshot"||scope=="AuthoredMapAdmissionProbe","Explicit bounded facts scope required");
        string mode=scenario.GetProperty("placement_mode").GetString();SpawnJson.Need(mode=="Untyped"||mode=="CapturedTypedOccupancy","Explicit placement mode required");
        var grid=SpawnJson.Facts<BlockerGrid>(scenario.GetProperty("grid"));int count=SpawnProgram.GridSize(grid);var storage=SpawnStorage.Read(scenario.GetProperty("storage"),artifacts,count);
        if(scope=="AuthoredMapAdmissionProbe")
        {SpawnJson.Need(mode=="Untyped","Authored probes cannot claim captured typed constraints");SpawnJson.Need(storage.Blocked.All(v=>v==0)&&storage.Occupied.All(v=>v==0)&&storage.Reserved.All(v=>v==0)&&storage.Counts.All(v=>v==0)&&storage.Friendly.All(v=>v==255),"Authoring probes require explicitly hypothetical empty runtime overlays");SpawnJson.Need(scenario.GetProperty("blocker_update").ValueKind==JsonValueKind.Null&&scenario.GetProperty("existing").GetArrayLength()==0,"Authoring probe cannot claim runtime blocker/existing actor snapshots");}
        bool arrays=scenario.GetProperty("emit_arrays").GetBoolean();SpawnJson.Need(!arrays||count<=65536,"Large arrays require digest output");
        byte[] actualOccupied=storage.Occupied;object blockers=null;var update=scenario.GetProperty("blocker_update");if(update.ValueKind!=JsonValueKind.Null)
        {SpawnJson.Shape(update,new[]{"owner","grid_identity","new","existing","removed"});blockers=SpawnBlockers.Update(update,grid,ref storage);}
        var profiles=new Dictionary<string,(int X,int Z,bool Vehicle,byte[] Admission)>();
        foreach(var profile in SpawnJson.Array(scenario.GetProperty("surface_profiles"),64))
        {
            SpawnJson.Shape(profile,new[]{"id","footprint","vehicle","admission","provenance"});string id=SpawnJson.Id(profile.GetProperty("id"));var size=SpawnJson.Cell(profile.GetProperty("footprint"),64);SpawnJson.Need(size[0]>0&&size[1]>0,"Positive footprint required");
            string provenance=profile.GetProperty("provenance").GetString();SpawnJson.Need(provenance=="NativeCanTraverseFootprintSnapshot"||provenance=="SyntheticExplicitFootprintFacts"||provenance=="ExportedInfantryCellAdmission1x1","Explicit surface fact provenance required");bool vehicle=profile.GetProperty("vehicle").GetBoolean();
            if(provenance=="ExportedInfantryCellAdmission1x1")SpawnJson.Need(size[0]==1&&size[1]==1&&!vehicle,"Exported cell admission is only equivalent to1x1 infantry footprint");
            SpawnJson.Need((long)(profiles.Count+1)*count<=32L*1024*1024,"Aggregate surface profiles exceed32million cells");SpawnProgram.Charge(count);
            var admission=artifacts.Bytes(profile.GetProperty("admission"),count,true);
            for(int z=0;z<grid.Height;z++)for(int x=0;x<grid.Width;x++)if(admission[x+z*grid.Width]!=0){int mx=x-(size[0]-1)/2,mz=z-(size[1]-1)/2;SpawnJson.Need(mx>=0&&mz>=0&&mx+size[0]<=grid.Width&&mz+size[1]<=grid.Height,"Surface facts admit outside-grid footprint");}
            SpawnJson.Need(profiles.TryAdd(id,(size[0],size[1],vehicle,admission)),"Duplicate surface profile");
        }
        var existing=new List<Existing>();var identities=new HashSet<string>();
        foreach(var item in SpawnJson.Array(scenario.GetProperty("existing"),65536))
        {SpawnJson.Shape(item,new[]{"id","cell","footprint"});var cell=SpawnJson.Cell(item.GetProperty("cell"));var size=SpawnJson.Cell(item.GetProperty("footprint"),64);string id=SpawnJson.Id(item.GetProperty("id"));SpawnJson.Need(identities.Add(id),"Duplicate existing actor");existing.Add(new Existing{Id=id,X=cell[0],Z=cell[1],Sx=size[0],Sz=size[1]});}
        bool hasMatch=scenario.GetProperty("has_match").GetBoolean();int surfaceCount=SpawnJson.Int(scenario.GetProperty("surface_count"),0,64);var results=new List<object>();var actors=SpawnJson.Array(scenario.GetProperty("actors"),512);long work=0;bool failed=false;
        foreach(var actor in actors)
        {
            SpawnJson.Shape(actor,new[]{"id","mission_seed","requested_world","footprint","is_air","vehicle","surface_profile","initialization","typed_occupied","has_footprint_component"});string id=SpawnJson.Id(actor.GetProperty("id"));SpawnJson.Need(identities.Add(id),"Spawn identity must be fresh and unique");
            byte[] utf8=new UTF8Encoding(false,true).GetBytes(id);SpawnJson.Need(utf8.Length<=61,"Stable ID exceeds FixedString64Bytes capacity");var world=SpawnJson.Array(actor.GetProperty("requested_world"),3);SpawnJson.Need(world.Length==3,"World position must have3 scalars");float wx=world[0].GetSingle(),wy=world[1].GetSingle(),wz=world[2].GetSingle();SpawnJson.Need(float.IsFinite(wx)&&float.IsFinite(wy)&&float.IsFinite(wz),"Finite requested world required");
            var frame=new GridCoordinateFrame{OriginX=grid.OriginX,OriginY=grid.OriginY,OriginZ=grid.OriginZ,CellSize=grid.CellSize};var center=GridCoordinateKernel.WorldToCell(frame,wx,wz);SpawnJson.Need(Math.Abs((long)center.X)<=1048576&&Math.Abs((long)center.Z)<=1048576,"Requested cell exceeds bounded profile");
            var size=SpawnJson.Cell(actor.GetProperty("footprint"),64);SpawnJson.Need(size[0]>0&&size[1]>0,"Positive raw footprint required");bool hasFootprint=actor.GetProperty("has_footprint_component").GetBoolean();SpawnJson.Need(hasFootprint||(size[0]==1&&size[1]==1),"Missing native footprint uses exactly1x1 fallback");work+=(long)count*size[0]*size[1];SpawnProgram.Charge((long)count*size[0]*size[1]);SpawnJson.Need(work<=256L*1024*1024,"Worst-case sequence search exceeds256million cell inspections");
            bool air=actor.GetProperty("is_air").GetBoolean(),vehicle=actor.GetProperty("vehicle").GetBoolean();
            if(scope=="AuthoredMapAdmissionProbe")SpawnJson.Need(size[0]==1&&size[1]==1&&!air&&!vehicle&&actor.GetProperty("initialization").ValueKind==JsonValueKind.Null,"Authoring probes support only1x1 infantry admission with unknown actual prefab state");string profileId=actor.GetProperty("surface_profile").GetString();
            SpawnJson.Need(profiles.TryGetValue(profileId,out var profile)&&profile.X==size[0]&&profile.Z==size[1]&&profile.Vehicle==vehicle,"Exact footprint/vehicle surface profile required");
            var typedOccupied=actor.GetProperty("typed_occupied");
            if(mode=="CapturedTypedOccupancy")
            {SpawnJson.Need(typedOccupied.ValueKind!=JsonValueKind.Null,"Complete actor-specific typed occupied snapshot required");SpawnProgram.Charge(count);var capturedOccupied=artifacts.Bytes(typedOccupied,count,true);for(int index=0;index<count;index++)SpawnJson.Need(actualOccupied[index]==0||capturedOccupied[index]!=0,"Typed mask cannot free actual occupancy");storage.Occupied=capturedOccupied;}
            else SpawnJson.Need(typedOccupied.ValueKind==JsonValueKind.Null,"Untyped mode must not imply typed occupied facts");
            SpawnProgram.Charge(count);Array.Clear(storage.Reserved);
            foreach(var old in existing)
            {var bounds=Game.Runtime.Navigation.GridOccupancyKernel.Bounds(grid.Width,grid.Height,old.X,old.Z,old.Sx,old.Sz);SpawnProgram.Charge((long)(bounds.MaxX-bounds.MinX)*(bounds.MaxZ-bounds.MinZ));for(int z=bounds.MinZ;z<bounds.MaxZ;z++)for(int x=bounds.MinX;x<bounds.MaxX;x++)storage.Reserved[x+z*grid.Width]=1;}
            uint seed=PlacementSeed.Begin(actor.GetProperty("mission_seed").GetInt32());foreach(byte b in utf8)seed=PlacementSeed.AddByte(seed,b);seed=PlacementSeed.Finish(seed,center.X,center.Z);var random=SpawnCellRandom.FromSeed(seed);
            bool found=SpawnCellSearchKernel.TryFindInitial(ref storage,ref random,grid.Width,grid.Height,center.X,center.Z,12,size[0],size[1],air,out int xFound,out int zFound);bool initialFound=found;
            if(found&&!air){var surface=new SpawnSurface{Admission=profile.Admission,Width=grid.Width,Height=grid.Height,SizeX=size[0],SizeZ=size[1]};found=SpawnCellSearchKernel.EnsureTraversable(ref storage,ref surface,grid.Width,grid.Height,size[0],size[1],hasMatch,surfaceCount,ref xFound,ref zFound);}
            object initialized=null;var initialization=actor.GetProperty("initialization");if(initialization.ValueKind!=JsonValueKind.Null)
            {
                SpawnJson.Shape(initialization,new[]{"state","overlay","shot_interval"});var state=SpawnJson.Facts<SkirmishActorInitializationState>(initialization.GetProperty("state"));var overlay=SpawnJson.Facts<SkirmishActorOverlayValues>(initialization.GetProperty("overlay"));float interval=initialization.GetProperty("shot_interval").GetSingle();SpawnJson.Need(float.IsFinite(interval),"Finite shot interval required");
                if(found){SkirmishActorInitializationKernel.ApplyHealth(ref state,overlay);SkirmishActorInitializationKernel.BindWeapons(ref state,overlay,interval);initialized=state;}
            }
            GridPositionValue? position=found?GridCoordinateKernel.CellToWorldCenter(frame,xFound,zFound):null;
            results.Add(new{id,requested_cell=new[]{center.X,center.Z},seed,random_state=random.State,initial_found=initialFound,found,cell=new[]{xFound,zFound},ungrounded_cell_center=position,actor_overlay_state=initialized,reserved_sha256=SpawnArtifacts.Hash(storage.Reserved),reserved=arrays?storage.Reserved.Select(v=>(int)v).ToArray():null});
            if(!found){failed=true;break;}if(hasFootprint)existing.Add(new Existing{Id=id,X=xFound,Z=zFound,Sx=size[0],Sz=size[1]});
        }
        return new{id=SpawnJson.Id(scenario.GetProperty("id")),kind="placement_sequence",facts_scope=scope,placement_mode=mode,runtime_overlay_assumption=scope=="AuthoredMapAdmissionProbe"?"Hypothetical empty runtime blockers/occupancy; not captured":null,blocker_update=blockers,actors=results,all_requested_cells_placed=!failed&&results.Count==actors.Length,storage=storage.Snapshot(arrays),grounding_executed=false,typed_pad_constraints_evaluated=false,typed_occupied_facts_applied=mode=="CapturedTypedOccupancy",full_mission_initialized=false,scope_gaps=new[]{"No actual prefab registry/actor component initialization beyond supplied overlay state","No native surface grounding or source-scene/runtime birth attestation","No starting buildings/economy/objective session launch or failure cleanup","Caller supplies query, surface admission and runtime overlay facts; typed-pad policy is not evaluated; only explicitly supplied per-actor constrained occupied masks are applied"}};
    }
}
