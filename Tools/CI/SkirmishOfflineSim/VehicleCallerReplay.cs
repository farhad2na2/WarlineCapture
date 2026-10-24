using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using System.Text.Json;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// A bounded bridge from the existing passive trace into the original native jobs.
// Native after-values are assertions only. No world tick or ECB playback occurs.
internal static class VehicleCallerReplay
{
    static readonly JsonSerializerOptions Fields = new() { IncludeFields=true };
    static void Need(bool ok,string message) { if(!ok)throw new ArgumentException(message); }
    static JsonElement P(JsonElement e,string n)=>e.GetProperty(n);
    static void ComponentShape(Type type,JsonElement value)
    {
        if(type.IsPrimitive||type.IsEnum)
        {
            Need(value.ValueKind==(type==typeof(bool)?JsonValueKind.True:JsonValueKind.Number)||(type==typeof(bool)&&value.ValueKind==JsonValueKind.False),"Explicit typed component scalar required");
            if(type==typeof(float))Need(float.IsFinite(value.GetSingle()),"Finite captured float required");
            if(type==typeof(double))Need(double.IsFinite(value.GetDouble()),"Finite captured double required");
            return;
        }
        var fields=type.GetFields(BindingFlags.Instance|BindingFlags.Public);
        Need(value.ValueKind==JsonValueKind.Object,"Explicit captured component object required: "+type.Name);
        WorldJson.Shape(value,fields.Select(f=>f.Name).ToArray());
        foreach(var field in fields)ComponentShape(field.FieldType,value.GetProperty(field.Name));
    }
    static T Data<T>(JsonElement e) where T:struct { ComponentShape(typeof(T),e);return e.Deserialize<T>(Fields); }
    static Entity EntityId(JsonElement e)=>new(){Index=P(e,"index").GetInt32(),Version=P(e,"version").GetInt32()};
    static string Id(Entity e)=>e.Index+":"+e.Version;
    static int Bits(float f)=>BitConverter.SingleToInt32Bits(f);
    static int[] StateBits(CapturedVehicleCaller.State s)=>new[]{Bits(s.Transform.Position.x),Bits(s.Transform.Position.y),Bits(s.Transform.Position.z),Bits(s.Transform.Rotation.value.x),Bits(s.Transform.Rotation.value.y),Bits(s.Transform.Rotation.value.z),Bits(s.Transform.Rotation.value.w),Bits(s.Transform.Scale),Bits(s.Kinematics.CurrentSpeed),Bits(s.Kinematics.StallSeconds),s.Grid.Cell.x,s.Grid.Cell.y,s.Follow.PathIndex};
    static void Lookup<T>(JsonElement row,string name,Entity e,Dictionary<Entity,T> target) where T:struct
    {var value=P(row,name);Need(value.ValueKind==JsonValueKind.Null||value.ValueKind==JsonValueKind.Object,"Explicit lookup presence required");if(value.ValueKind==JsonValueKind.Object)target.Add(e,Data<T>(value));}
    static byte[] Artifact(string directory,JsonElement spec,long maximum,ref long aggregate)
    {
        WorldJson.Shape(spec,new[]{"path","bytes","sha256"});string name=P(spec,"path").GetString();
        Need(!string.IsNullOrWhiteSpace(name)&&name==Path.GetFileName(name)&&name!="."&&name!="..","Caller artifact path escape");
        long length=P(spec,"bytes").GetInt64();Need(length>0&&length<=maximum&&aggregate<=64L*1024*1024-length,"Caller artifact aggregate byte bound exceeded");aggregate+=length;
        return StartupFullMapImport.ReadPinnedArtifact(Path.Combine(directory,name),P(spec,"sha256").GetString(),maximum,length);
    }
    internal static object Run(JsonElement row)
    {
        BodyTransitionSourcePins.RequireVerified();WorldJson.Shape(row,new[]{"id","kind","capture","require_ecb_playback","work_limit"});
        Need(!P(row,"require_ecb_playback").GetBoolean(),"Global ECB playback remains unsupported");
        var descriptor=P(row,"capture");WorldJson.Shape(descriptor,new[]{"path","sha256"});
        string path=P(descriptor,"path").GetString();Need(Path.IsPathRooted(path),"Absolute pinned native trace required");
        var raw=StartupFullMapImport.ReadPinnedArtifact(path,P(descriptor,"sha256").GetString(),32L*1024*1024);
        using var document=JsonDocument.Parse(raw);var trace=document.RootElement;
        Need(P(trace,"schema_version").GetInt32()==1&&P(trace,"capture_complete").GetBoolean()&&P(trace,"non_idle_vehicle_utility_interval").GetBoolean()&&P(trace,"capture_failure").ValueKind==JsonValueKind.Null,"Complete real non-idle capture required");
        Need(P(trace,"command_receipt_violations").GetInt32()==0&&P(trace,"completed_touch_releases").GetInt32()>0&&P(trace,"accepted_normal_commands").EnumerateArray().Any(c=>(P(c,"operation").GetString()=="Move"||P(c,"operation").GetString()=="AttackMove")&&P(c,"verified_touch_release").GetBoolean()&&P(c,"receipt").GetUInt32()>0),"Verified ordinary movement input required");
        var facts=P(trace,"before_grid_borrowed_facts");Need(P(facts,"live_query_snapshot_complete").GetBoolean()&&P(facts,"cleanup_scheduled_after_move").GetBoolean()&&!P(facts,"global_ecb_playback_proven").GetBoolean(),"Actual borrowed snapshot/cleanup boundary required");
        var map=new CapturedVehicleCaller.Snapshot { Grid=Data<GridConfig>(P(facts,"grid")),DeltaTime=BitConverter.Int32BitsToSingle(P(trace,"actual_grid_delta_time_bits").GetInt32()),KnownEntities=new(),AutoWander=new(),Targets=new(),LongDistance=new(),ManualGroup=new(),Manual=new(),Guided=new(),Boarding=new() };
        int cells=checked(map.Grid.Width*map.Grid.Height);Need(map.Grid.Width==2048&&map.Grid.Height==1024,"Actual full S002 map required");
        var specs=P(facts,"buffers");string directory=Path.GetDirectoryName(path);long aggregate=0;
        map.Walkable=Artifact(directory,P(specs,"walkable"),cells,ref aggregate);map.Roads=Artifact(directory,P(specs,"road"),cells,ref aggregate);map.Sidewalk=Artifact(directory,P(specs,"sidewalk"),cells,ref aggregate);map.Dirt=Artifact(directory,P(specs,"dirt"),cells,ref aggregate);map.Friendly=Artifact(directory,P(specs,"friendly_pass"),cells,ref aggregate);
        var blocked=Artifact(directory,P(specs,"dynamic_blocked"),cells,ref aggregate);var occupied=Artifact(directory,P(specs,"occupied"),cells,ref aggregate);Need(blocked.All(b=>b<=1)&&occupied.All(b=>b<=1),"Native binary bit-mask representation required");map.Blocked=blocked.Select(b=>b!=0).ToArray();map.Occupied=occupied.Select(b=>b!=0).ToArray();
        int nodes=P(facts,"path_pool_count").GetInt32();Need(nodes>0&&nodes<=1048576,"Bounded path pool required");var pool=Artifact(directory,P(specs,"path_pool_i32le_xy"),8L*1048576,ref aggregate);Need(pool.Length==nodes*8,"Path pool descriptor count differs");map.Pool=new int2[nodes];for(int i=0;i<nodes;i++)map.Pool[i]=new(System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(pool.AsSpan(i*8,4)),System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(pool.AsSpan(i*8+4,4)));
        var live=P(facts,"full_live_units_snapshot").EnumerateArray().ToArray();Need(live.Length<=512,"Native global live snapshot exceeds caller bound");map.LiveEntities=new Entity[live.Length];map.LiveGrids=new UnitGrid[live.Length];map.LiveFootprints=new UnitFootprint[live.Length];
        for(int i=0;i<live.Length;i++) {var value=live[i];Need(P(value,"index").GetInt32()==i,"Dense native live query order required");var e=EntityId(P(value,"entity"));Need(map.KnownEntities.Add(e),"Duplicate live identity");map.LiveEntities[i]=e;map.LiveGrids[i]=Data<UnitGrid>(P(value,"grid"));map.LiveFootprints[i]=Data<UnitFootprint>(P(value,"footprint"));var lookups=P(value,"lookups");WorldJson.Shape(lookups,new[]{"AutoWanderMoveTag","UnitTarget","UnitLongDistanceMove","ManualMoveGroupMemberTag","ManualMoveOrderTag","CampaignMissionGuidedMoveInProgressTag","UnitTransportBoardingTarget"});Lookup(lookups,"AutoWanderMoveTag",e,map.AutoWander);Lookup(lookups,"UnitTarget",e,map.Targets);Lookup(lookups,"UnitLongDistanceMove",e,map.LongDistance);Lookup(lookups,"ManualMoveGroupMemberTag",e,map.ManualGroup);Lookup(lookups,"ManualMoveOrderTag",e,map.Manual);Lookup(lookups,"CampaignMissionGuidedMoveInProgressTag",e,map.Guided);Lookup(lookups,"UnitTransportBoardingTarget",e,map.Boarding);}
        var before=P(trace,"before_grid_roster").EnumerateArray().ToDictionary(a=>P(a,"query_id").GetString());var after=P(trace,"before_fuel_roster").EnumerateArray().ToDictionary(a=>P(a,"query_id").GetString());Need(before.Count==52&&after.Count==52&&before.Keys.ToHashSet().SetEquals(after.Keys),"Same full52 roster required");
        var cleanup=P(trace,"cleanup_query_order").EnumerateArray().ToArray();Need(cleanup.Length==P(facts,"cleanup_readonly_count_assertion").GetInt32()&&cleanup.Select((c,i)=>P(c,"query_index").GetInt32()==i).All(v=>v),"Complete dense native cleanup indices required");var cleanupIndices=cleanup.ToDictionary(c=>Id(EntityId(P(c,"entity"))),c=>P(c,"query_index").GetInt32());
        var moves=P(trace,"grid_job_query_order_and_utility_calls").EnumerateArray().ToArray();Need(moves.Length==P(trace,"readonly_move_query_count_assertion").GetInt32()&&moves.Select((m,i)=>P(m,"query_index").GetInt32()==i).All(v=>v),"Complete dense native movement indices required");
        long workLimit=P(row,"work_limit").GetInt64();Need(workLimit>0&&workLimit<=2000000&&(long)Math.Max(1,moves.Length)*Math.Max(1,live.Length)*16*16<=workLimit,"Actual caller work bound exceeded");var results=new List<object>();int totalDeferred=0;
        foreach(var move in moves) {
            var e=EntityId(P(move,"entity"));string id=Id(e);Need(before.ContainsKey(id)&&after.ContainsKey(id),"Movement actor outside captured full52 roster");var c=P(before[id],"components");var behavior=Data<UnitMovementBehavior>(P(c,"UnitMovementBehavior"));if(behavior.UsesVehicleMotion==0)continue;
            Need(cleanupIndices.ContainsKey(id),"Actual cleanup query identity required");
            var state=new CapturedVehicleCaller.State { Entity=e,Transform=Data<LocalTransform>(P(c,"LocalTransform")),Grid=Data<UnitGrid>(P(c,"UnitGrid")),Follow=Data<UnitPathFollow>(P(c,"UnitPathFollow")),Kinematics=Data<UnitVehicleKinematics>(P(c,"UnitVehicleKinematics")),Move=Data<UnitMove>(P(c,"UnitMove")),Range=Data<UnitPathRange>(P(c,"UnitPathRange")),Footprint=Data<UnitFootprint>(P(c,"UnitFootprint")),Behavior=behavior,VehicleMovement=Data<UnitVehicleMovement>(P(c,"UnitVehicleMovement")),Faction=Data<Faction>(P(c,"Faction")) };
            var authority=new CapturedVehicleCaller.Authority {InputEvidenceSha256=P(descriptor,"sha256").GetString(),CompleteComponentValues=true,ExactMoveQueryMembership=true,ExactCleanupQueryMembership=true,ImmutableOccupancyAtGridDispatch=true,CompleteLiveQuerySnapshot=true,CompleteLookupPresence=true,ActualClockInput=true,CleanupAfterMoveBarrier=true,MoveQueryIndex=P(move,"query_index").GetInt32(),CleanupQueryIndex=cleanupIndices[id],RequireEcbPlayback=false};
            // The original jobs consume only before-state, masks, paths and lookups.
            var derived=CapturedVehicleCaller.Step(authority,map,state);
            var expected=P(after[id],"components");var asserted=new CapturedVehicleCaller.State {Transform=Data<LocalTransform>(P(expected,"LocalTransform")),Kinematics=Data<UnitVehicleKinematics>(P(expected,"UnitVehicleKinematics")),Grid=Data<UnitGrid>(P(expected,"UnitGrid")),Follow=Data<UnitPathFollow>(P(expected,"UnitPathFollow"))};
            Need(StateBits(derived.State).SequenceEqual(StateBits(asserted)),"Derived caller differs from native before-fuel boundary: "+id);
            totalDeferred=checked(totalDeferred+derived.Deferred.Length);Need(totalDeferred<=4096,"Aggregate deferred intent bound exceeded");results.Add(new{id,post_bits=StateBits(derived.State),deferred=derived.Deferred});
        }
        Need(results.Count>0,"Actual vehicle query rows required");return new {scope="NativeSourceLinkedVehicleCallerAndCleanupOnly",roster=52,map_width=2048,map_height=1024,global_live_query_rows=live.Length,movement_query_rows=moves.Length,vehicle_callers=results.Count,native_boundary_assertions_matched=true,native_assertion_boundary="SerializedBeforeFuelRoster",derived=results,deferred_intents=totalDeferred,ecb_playback_applied=false,full_frame_derived=false,mission_frames_advanced=0,mission_qualified=false};
    }
}
