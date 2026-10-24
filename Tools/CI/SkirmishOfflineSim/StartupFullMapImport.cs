using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Game.Components;
using Game.Runtime.Navigation;

// Strict adapter into the existing world runner. Imports only an actual S002
// derived startup, preserving all actor/footprint facts. Does not advance time.
internal static class StartupFullMapImport
{
    const int MaximumCells=16777216;
    static void Need(bool ok,string text)=>WorldJson.Need(ok,text);
    static string Hash(byte[] raw)=>Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant();
    static byte[] PinnedFile(JsonElement descriptor,long maximum)
    {
        WorldJson.Shape(descriptor,new[]{"path","sha256"});string path=descriptor.GetProperty("path").GetString();
        Need(!string.IsNullOrWhiteSpace(path)&&Path.IsPathRooted(path),"Pinned absolute import path required");
        return BoundedPinnedFile(path,descriptor.GetProperty("sha256").GetString(),maximum);
    }
    internal static byte[] ReadPinnedArtifact(string path,string hash,long maximum,long? length=null)
    {
        Need(!string.IsNullOrWhiteSpace(path)&&Path.IsPathRooted(path),"Absolute pinned artifact required");
        for(var item=new FileInfo(path)as FileSystemInfo;item!=null;item=item is FileInfo f?f.Directory:((DirectoryInfo)item).Parent)
            Need((item.Attributes&FileAttributes.ReparsePoint)==0,"Artifact symlink rejected");
        return BoundedPinnedFile(path,hash,maximum,length);
    }
    static byte[] BoundedPinnedFile(string path,string expectedHash,long maximum,long? expectedLength=null)
    {
        Need(!expectedLength.HasValue||expectedLength.Value>0&&expectedLength.Value<=maximum,"Positive bounded artifact descriptor required");
        var info=new FileInfo(path);
        Need(info.Exists&&(info.Attributes&(FileAttributes.ReparsePoint|FileAttributes.Directory|FileAttributes.Device))==0&&info.Length>0&&info.Length<=maximum,"Bounded regular import file required");
        long length=info.Length;
        Need(!expectedLength.HasValue||length==expectedLength.Value,"Artifact length differs before read");
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        Need(stream.CanSeek&&stream.Length==length,"Import file budget/type changed");
        byte[] bytes=new byte[checked((int)length)];stream.ReadExactly(bytes);
        Need(stream.ReadByte()==-1&&Hash(bytes)==expectedHash,"Import bytes differ");return bytes;
    }

    struct Storage:IGridOccupancyStorage
    {
        internal int[] Counts;internal byte[] Occupied;
        public int GetCount(int index)=>Counts[index];public void SetCount(int index,int value)=>Counts[index]=value;
        public void SetOccupied(int index,bool value)=>Occupied[index]=(byte)(value?1:0);
    }
    static float F(JsonElement v,string n)=>v.GetProperty(n).GetSingle();
    static void SameFloat(float a,float b,string message)=>Need(float.IsFinite(a)&&float.IsFinite(b)&&BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b),message);
    internal static object Run(JsonElement row)
    {
        WorldJson.Shape(row,new[]{"id","kind","derived_result","capture","require_forward"});
        byte[] captureRaw=PinnedFile(row.GetProperty("capture"),128L*1024*1024),derivedRaw=PinnedFile(row.GetProperty("derived_result"),32L*1024*1024);
        using var cd=JsonDocument.Parse(captureRaw);using var dd=JsonDocument.Parse(derivedRaw);var capture=cd.RootElement;var report=dd.RootElement;
        WorldJson.UniqueKeys(capture);WorldJson.UniqueKeys(report);
        Need(report.GetProperty("result").GetString()=="Passed"&&report.GetProperty("unchanged").GetBoolean()&&report.GetProperty("grounding_required").GetBoolean(),"Verified grounded derivation report required");
        Need(report.GetProperty("sources_before").GetRawText()==report.GetProperty("sources_after").GetRawText(),"Derivation source closure changed");
        var derived=report.GetProperty("actual");Need(derived.GetProperty("source_capture_sha256").GetString()==Hash(captureRaw),"Derived/capture identity differs");
        Need(capture.GetProperty("scope").GetString()=="ActualS002World_PostRoster_FrameBoundaryObservation"&&capture.GetProperty("session_phase").GetString()=="Playing"&&capture.GetProperty("tick_clock").GetInt32()==0,"Exact post-roster tick-zero capture required");
        Need(capture.GetProperty("resolved_setup").GetProperty("CatalogId").GetString()=="S002","Only actual S002 startup import supported");
        var grid=capture.GetProperty("grid");int width=grid.GetProperty("Width").GetInt32(),height=grid.GetProperty("Height").GetInt32();long cells=(long)width*height;
        Need(width>0&&height>0&&cells<=MaximumCells,"Complete bounded map required");
        var o=grid.GetProperty("Origin");var frame=new GridCoordinateFrame{OriginX=F(o,"x"),OriginY=F(o,"y"),OriginZ=F(o,"z"),CellSize=F(grid,"CellSize")};
        Need(float.IsFinite(frame.CellSize)&&frame.CellSize>0&&float.IsFinite(frame.OriginX)&&float.IsFinite(frame.OriginY)&&float.IsFinite(frame.OriginZ),"Finite actual coordinate frame required");
        Need(width==2048&&height==1024,"S002 full 2048x1024 source must not be cropped");
        var states=derived.GetProperty("derived_actor_states").EnumerateArray().ToArray();var placements=derived.GetProperty("actors").EnumerateArray().ToDictionary(a=>a.GetProperty("id").GetString());
        var native=capture.GetProperty("actors").EnumerateArray().Where(a=>!a.GetProperty("is_structure").GetBoolean()).ToDictionary(a=>a.GetProperty("stable_id").GetString());
        int expected=capture.GetProperty("expected_force_count").GetInt32();
        Need(expected==52&&states.Length==expected&&placements.Count==expected&&native.Count==expected&&derived.GetProperty("force_actors").GetInt32()==expected,"All52 actual actors required; roster reduction rejected");
        var store=new Storage{Counts=new int[(int)cells],Occupied=new byte[(int)cells]};
        var actorRows=new List<object>();var seen=new HashSet<string>();var observedIdentities=new HashSet<string>();int vehicles=0,footprintReferences=0,eligible=0;var missingComponents=new Dictionary<string,int>();
        foreach(var state in states)
        {
            string id=state.GetProperty("id").GetString();Need(seen.Add(id)&&native.TryGetValue(id,out _),"Exact unique native actor identity required");var n=native[id];var placement=placements[id];
            int entityIndex=n.GetProperty("entity_index").GetInt32(),entityVersion=n.GetProperty("entity_version").GetInt32();Need(entityIndex>0&&entityVersion>0&&observedIdentities.Add(entityIndex+":"+entityVersion),"Complete unique runtime actor identity required");
            Need(state.GetProperty("source_key").GetString()==n.GetProperty("source_key").GetString()&&state.GetProperty("faction").GetByte()==n.GetProperty("faction").GetByte(),"Derived source/faction differs");
            Need(state.GetProperty("entity_enabled").GetBoolean()==n.GetProperty("entity_enabled").GetBoolean(),"Derived entity enablement differs");
            WorldJson.Expect(state.GetProperty("enableable_components"),n.GetProperty("enableable_components"),"derived enablement");
            var comp=state.GetProperty("components");var nc=n.GetProperty("components");
            var compare=JsonNode.Parse(comp.GetRawText()).AsObject();compare.Remove("LocalTransform");WorldJson.Expect(JsonSerializer.SerializeToElement(compare),nc,"derived serialized actor");
            var position=comp.GetProperty("LocalTransform").GetProperty("Position");var nativePosition=nc.GetProperty("LocalTransform").GetProperty("Position");
            foreach(string axis in new[]{"x","y","z"})SameFloat(F(position,axis),F(nativePosition,axis),"Derived/native transform differs");
            var cell=comp.GetProperty("UnitGrid").GetProperty("Cell");int x=cell.GetProperty("x").GetInt32(),z=cell.GetProperty("y").GetInt32();var projected=GridCoordinateKernel.WorldToCell(frame,F(position,"x"),F(position,"z"));
            Need(x==projected.X&&z==projected.Z&&x==placement.GetProperty("cell")[0].GetInt32()&&z==placement.GetProperty("cell")[1].GetInt32(),"Imported actual cell/frame differs");
            var size=comp.GetProperty("UnitFootprint").GetProperty("Size");int sx=size.GetProperty("x").GetInt32(),sz=size.GetProperty("y").GetInt32();
            Need(sx>=1&&sz>=1&&sx<=64&&sz<=64&&sx==placement.GetProperty("footprint")[0].GetInt32()&&sz==placement.GetProperty("footprint")[1].GetInt32(),"Actual footprint must be preserved");
            bool vehicle=comp.GetProperty("UnitMovementBehavior").GetProperty("UsesVehicleMotion").GetByte()!=0;
            Need(vehicle==placement.GetProperty("vehicle").GetBoolean(),"Actual vehicle policy must be preserved");if(vehicle)vehicles++;
            var types=n.GetProperty("component_types").EnumerateArray().Select(v=>v.GetString()).ToHashSet();
            // Match pinned DynamicOccupancyRebuildSystem membership, including default
            // query exclusion of disabled entities and prefabs. Simulate enablement is
            // not a membership condition in that actual query.
            bool membership=n.GetProperty("entity_enabled").GetBoolean()&&!types.Contains("Unity.Entities.Prefab")&&!types.Contains("Unity.Entities.Disabled")&&
                types.Contains("Game.Components.UnitGrid")&&types.Contains("Game.Components.UnitFootprint")&&
                !types.Contains("Game.Components.StaticGridBlocker")&&!types.Contains("Game.Components.RuntimeBuildingCombatTag")&&!types.Contains("Game.Components.UnitDeathAnimationComponent");
            var bounds=GridOccupancyKernel.Bounds(width,height,x,z,sx,sz);
            Need(bounds.MaxX-bounds.MinX==sx&&bounds.MaxZ-bounds.MinZ==sz,"Full actor footprint outside actual map");
            if(membership){GridOccupancyKernel.Add(width,ref store,bounds);eligible++;footprintReferences=checked(footprintReferences+sx*sz);}
            var missing=new List<string>();
            foreach(string name in new[]{"UnitAttackAnimationComponent","UnitAnimationOrderEntry","UnitGroundOffsetComponent","UnitAnimationGroundOffset","UnitPrevWorldPos"})
                if(types.Contains("Game.Components."+name)&&!comp.TryGetProperty(name,out _)){missing.Add(name);missingComponents[name]=missingComponents.GetValueOrDefault(name)+1;}
            actorRows.Add(new{id,entity_index=entityIndex,entity_version=entityVersion,vehicle,footprint_x=sx,footprint_z=sz,movement_occupancy_eligible=membership,
                derived_state=state.Clone(),captured_component_membership=n.GetProperty("component_types").Clone(),captured_observation=n.Clone(),missing_forward_component_values=missing});
        }
        Need(vehicles==12&&states.Count(s=>s.GetProperty("components").GetProperty("UnitFootprint").GetProperty("Size").GetProperty("x").GetInt32()==3)==12,"All12 actual3x3 vehicles required; no infantry coercion");
        // Keep observed masks separate. A post-roster mask has its own boundary;
        // imported attempt-owned reduction is not a whole-world native parity proof.
        string root=Path.GetDirectoryName(row.GetProperty("capture").GetProperty("path").GetString());long artifactBytes=0;byte[] observedOccupied=null;int artifactCount=0;
        foreach(var artifact in capture.GetProperty("artifacts").EnumerateObject())
        {
            string name=artifact.Name;Need(Path.GetFileName(name)==name&&name!="."&&name!="..","Artifact path escape");var spec=artifact.Value;long length=spec.GetProperty("bytes").GetInt64();
            Need(length>0&&length<=128L*1024*1024&&artifactBytes<=512L*1024*1024-length,"Artifact byte budget exceeded");artifactBytes+=length;string path=Path.Combine(root,name);
            for(var item=new FileInfo(path)as FileSystemInfo;item!=null;item=item is FileInfo f?f.Directory:((DirectoryInfo)item).Parent)Need((item.Attributes&FileAttributes.ReparsePoint)==0,"Artifact symlink rejected");
            byte[] bytes=BoundedPinnedFile(path,spec.GetProperty("sha256").GetString(),128L*1024*1024,length);artifactCount++;
            if(name=="dynamic-occupied.u8"){Need(length==cells&&bytes.All(b=>b<=1),"Complete observed occupancy mask required");observedOccupied=bytes;}
        }
        Need(observedOccupied!=null,"Observed occupancy artifact absent");
        long sum=0;int occupied=0,observedOverlap=0;for(int i=0;i<store.Counts.Length;i++){sum+=store.Counts[i];if(store.Occupied[i]!=0){occupied++;if(observedOccupied[i]!=0)observedOverlap++;}}
        Need(sum==footprintReferences,"Shared occupancy count conservation failed");
        var gaps=new[]{
            "HostBodyForwardAdmission: bounded128actor host carries52 bodies; actual vehicle caller/combat/animation/lifecycle admission remains unsupported",
            "FullMapPolicyApplicability: caller threads artifact-root/full-map opt-in, but actual3x3 movement surface/cost authority remains absent",
            "PerBodyMovementAuthority: footprint plumbing preserves3x3 occupancy/goals/path admission; actual vehicle caller/chase and animation/grounding policies remain unwired",
            "MissingMovementPolicyFacts: captured spawn-admission masks are not complete per-profile movement cell/footprint legality or cardinal/diagonal traversal costs",
            "MissingDynamicOccupancyBoundary: observed mask has no complete eligible-world membership/counts/epoch; attempt-owned reduction must remain separate",
            "MissingInitialRuntimeValues: require explicit values for every present animation/order/ground-offset/previous-world component; see missing_forward_component_counts",
            "MissingNativeForwardTrace: exact accepted orders, frame timing, per-system query order, path completion/budget facts and same-boundary observations are not captured"
        };
        if(row.GetProperty("require_forward").GetBoolean())throw new ArgumentException("ForwardStartupImportUnsupported: "+string.Join("; ",gaps));
        return new{result="ImportedForwardUnsupported",actor_count=states.Length,vehicle_count=vehicles,width,height,cells,eligible_owned_movement_actors=eligible,
            attempt_owned_occupancy=new{footprint_cell_references=footprintReferences,occupied_cell_count=occupied,counts_sum=sum,counts_sha256=Hash(System.Runtime.InteropServices.MemoryMarshal.AsBytes(store.Counts.AsSpan()).ToArray()),occupied_u8_sha256=Hash(store.Occupied),native_parity=false},
            observed_occupancy=new{source="dynamic-occupied.u8",boundary="ActualS002World_PostRoster_FrameBoundaryObservation",same_boundary_reduction_compared=false,overlap_with_owned_reduction_cells=observedOverlap,storage_epoch=(int?)null},
            static_blocker_observation=new{storage_epoch=capture.GetProperty("blocker_storage_epoch").Clone(),revision=capture.GetProperty("blocker_revision").Clone(),is_movement_occupancy_epoch=false},
            spawn_reservation_observation=new{initial_census=capture.GetProperty("allocator_transactions")[0].GetProperty("reservation_census_native_query_order").GetArrayLength(),derived_allocator_placements=states.Length,is_movement_occupancy_census=false},
            goal_reservations_initialized=false,artifact_count=artifactCount,artifact_bytes_verified=artifactBytes,missing_forward_component_counts=missingComponents,actors=actorRows,
            forward_supported=false,forward_blockers=gaps,advanced_frames=0,full_mission_initialized=false,mission_qualified=false,capture_sha256=Hash(captureRaw),derived_result_sha256=Hash(derivedRaw)};
    }
}
