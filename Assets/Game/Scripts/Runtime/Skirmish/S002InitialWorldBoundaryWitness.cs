#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Game.Components;
using Game.Skirmish.Contracts;

namespace Game.Runtime
{
    // Burst entry points touch only this holder: its initializer contains no managed
    // records, environment reads, dictionaries, or UnityEngine calls.
    internal static class S002InitialWorldObserverControl
    {
        internal struct ArmState { public byte Enabled;public ulong Epoch; }
        sealed class ArmKey { }
        internal static readonly SharedStatic<ArmState> Arm=SharedStatic<ArmState>.GetOrCreate<ArmKey>();
        internal static bool ObserverEnabled=>Arm.Data.Enabled==1;
        internal static ulong ObserverEpoch=>Arm.Data.Epoch;
    }

    // Observer-owned state only. Primitive operations remain present in Burst entry points.
    internal struct S002InitialWorldBoundaryCounter
    {
        public byte CreatedObserved, UpdatedWithoutCreate, WorldMismatch, EpochMismatch, CountExhausted;
        public ulong WorldSequence, ConfigEpoch, UpdateEntries;
        public uint CreatedVersion, LastEntryVersion;
        public double CreatedTime, LastEntryTime;
        public void Created(ulong world,uint version,double time,ulong epoch,bool enabled)
        {
            if(!enabled)return;
            this=new S002InitialWorldBoundaryCounter{CreatedObserved=1,WorldSequence=world,ConfigEpoch=epoch,CreatedVersion=version,CreatedTime=time};
        }
        public void Enter(ulong world,uint version,double time,ulong epoch,bool enabled)
        {
            if(!enabled)return;
            if(CreatedObserved==0)UpdatedWithoutCreate=1;
            if(WorldSequence!=world)WorldMismatch=1;
            if(ConfigEpoch!=epoch)EpochMismatch=1;
            if(UpdateEntries==ulong.MaxValue)CountExhausted=1;else UpdateEntries++;
            LastEntryVersion=version;LastEntryTime=time;
        }
        public bool MatchesBaseline(S002InitialWorldBoundaryCounter baseline,ulong world,ulong epoch)
        {
            return CreatedObserved==1&&baseline.CreatedObserved==1&&UpdatedWithoutCreate==0&&WorldMismatch==0&&EpochMismatch==0&&CountExhausted==0&&
                baseline.UpdatedWithoutCreate==0&&baseline.WorldMismatch==0&&baseline.EpochMismatch==0&&baseline.CountExhausted==0&&
                WorldSequence==world&&baseline.WorldSequence==world&&ConfigEpoch==epoch&&baseline.ConfigEpoch==epoch&&UpdateEntries==baseline.UpdateEntries&&
                CreatedVersion==baseline.CreatedVersion&&CreatedTime==baseline.CreatedTime;
        }
    }

    public static class S002InitialWorldBoundaryWitness
    {
        static bool configured;
        sealed class SpawnRecord
        {
            public Entity SessionEntity;
            public SkirmishExpandedSessionComponent Session;
            public S002InitialWorldBoundaryCounter Path,Occupancy;
            public int EngineFrame;
            public double WorldElapsed;
            public bool DuplicateSpawnObserved;
        }
        static readonly Dictionary<ulong,SpawnRecord> Spawns=new Dictionary<ulong,SpawnRecord>();
        sealed class RosterRecord { public int BeforeOrder,AfterOrder,EngineFrame;public double WorldElapsed;public bool CallbackActive; }
        static readonly Dictionary<ulong,RosterRecord> Rosters=new Dictionary<ulong,RosterRecord>();
        public static Action<EntityManager,int,int> RosterTailObserved;
        internal static bool ObserverEnabled=>S002InitialWorldObserverControl.ObserverEnabled;
        internal static ulong ObserverEpoch=>S002InitialWorldObserverControl.ObserverEpoch;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ConfigureFromEnvironment()
        {
            bool enable=Environment.GetEnvironmentVariable("WARLINE_S002_INITIAL_WORLD_BUNDLE")=="1";
            if(!configured||(S002InitialWorldObserverControl.Arm.Data.Enabled==1)!=enable)
            {
                if(S002InitialWorldObserverControl.Arm.Data.Epoch==ulong.MaxValue){S002InitialWorldObserverControl.Arm.Data.Enabled=0;throw new InvalidOperationException("Initial-world observer epoch exhausted");}
                S002InitialWorldObserverControl.Arm.Data.Epoch++;Spawns.Clear();Rosters.Clear();configured=true;
            }
            S002InitialWorldObserverControl.Arm.Data.Enabled=(byte)(enable?1:0);
        }
        // Root calls this only at the actual successful spawn tail, after ProjectMatchPhase.
        public static void RecordSpawnComplete(EntityManager em,Entity sessionEntity)
        {
            if(!ObserverEnabled)return;
            if(!em.Exists(sessionEntity)||!em.HasComponent<SkirmishExpandedSessionComponent>(sessionEntity))return;
            var session=em.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity);
            if(session.CatalogId.ToString()!="S002"||session.IsLegacy!=0||session.SpawnComplete!=1||session.Phase!=SkirmishSessionPhase.Playing)return;
            var world=em.World;ulong key=world.SequenceNumber;
            if(Spawns.TryGetValue(key,out var previous)){previous.DuplicateSpawnObserved=true;return;}
            Spawns.Add(key,new SpawnRecord{SessionEntity=sessionEntity,Session=session,Path=UnitPathfindingSystem.InitialWorldCounter(world),
                Occupancy=DynamicOccupancyRebuildSystem.InitialWorldCounter(world),EngineFrame=Time.frameCount,WorldElapsed=world.Time.ElapsedTime});
        }
        // Invoked synchronously at the actual roster OnUpdate tail. Root's capture
        // callback must preflight private jobs and producer dependencies before census.
        public static void RecordRosterTail(EntityManager em,int orderBefore)
        {
            if(!ObserverEnabled)return;
            int after=em.EntityOrderVersion;var world=em.World;
            var roster=new RosterRecord{BeforeOrder=orderBefore,AfterOrder=after,EngineFrame=Time.frameCount,WorldElapsed=world.Time.ElapsedTime,CallbackActive=true};
            Rosters[world.SequenceNumber]=roster;
            try{RosterTailObserved?.Invoke(em,orderBefore,after);}finally{roster.CallbackActive=false;}
        }
        public static Dictionary<string,object> Capture(World world)
        {
            if(world==null||!world.IsCreated)throw new InvalidOperationException("Live same World required");
            var path=UnitPathfindingSystem.InitialWorldCounter(world);var occ=DynamicOccupancyRebuildSystem.InitialWorldCounter(world);
            bool found=Spawns.TryGetValue(world.SequenceNumber,out var spawn);bool sameSession=false;
            SkirmishExpandedSessionComponent current=default;
            if(found&&world.EntityManager.Exists(spawn.SessionEntity)&&world.EntityManager.HasComponent<SkirmishExpandedSessionComponent>(spawn.SessionEntity))
            {
                current=world.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(spawn.SessionEntity);
                sameSession=current.SessionId.Equals(spawn.Session.SessionId)&&current.CatalogId.Equals(spawn.Session.CatalogId)&&current.SetupHash==spawn.Session.SetupHash&&
                    current.SpawnComplete==1&&current.Phase==SkirmishSessionPhase.Playing&&current.TickClock==spawn.Session.TickClock;
            }
            bool hasRoster=Rosters.TryGetValue(world.SequenceNumber,out var roster);
            bool rosterSync=hasRoster&&roster.CallbackActive&&roster.BeforeOrder!=roster.AfterOrder&&roster.EngineFrame==Time.frameCount&&world.EntityManager.EntityOrderVersion==roster.AfterOrder;
            bool valid=ObserverEnabled&&found&&!spawn.DuplicateSpawnObserved&&sameSession&&rosterSync&&path.MatchesBaseline(spawn.Path,world.SequenceNumber,ObserverEpoch)&&occ.MatchesBaseline(spawn.Occupancy,world.SequenceNumber,ObserverEpoch);
            return new Dictionary<string,object>{{"enabled",ObserverEnabled},{"observer_epoch",ObserverEpoch},{"world_sequence",world.SequenceNumber},
                {"captured_frame",Time.frameCount},{"captured_world_elapsed",world.Time.ElapsedTime},{"has_actual_spawn_boundary",found},{"duplicate_spawn_observed",found&&spawn.DuplicateSpawnObserved},{"same_session",sameSession},
                {"boundary_valid",valid},{"roster_structural_sync_proven",rosterSync},{"roster_callback_active",hasRoster&&roster.CallbackActive},{"roster_order_before",hasRoster?(object)roster.BeforeOrder:null},
                {"roster_order_after",hasRoster?(object)roster.AfterOrder:null},{"roster_frame",hasRoster?(object)roster.EngineFrame:null},{"roster_world_elapsed",hasRoster?(object)roster.WorldElapsed:null},{"scope","pathfinding-and-dynamic-occupancy entry witnesses only; roster/managed domains require envelope guards"},
                {"pathfinding_now",path},{"occupancy_now",occ},{"spawn_frame",found?(object)spawn.EngineFrame:null},{"spawn_world_elapsed",found?(object)spawn.WorldElapsed:null},
                {"pathfinding_at_spawn",found?(object)spawn.Path:null},{"occupancy_at_spawn",found?(object)spawn.Occupancy:null},
                {"session_entity",found?(object)spawn.SessionEntity:null},{"session_at_spawn",found?(object)spawn.Session:null},{"session_at_capture",current}};
        }

        public static IDisposable BeginPrivateCaptureBudget()=>S002InitialWorldPrivateCopyBudget.Begin();

        internal static Dictionary<string,object> Fields(object value,string prefix="")
        {
            var output=new Dictionary<string,object>();
            foreach(var field in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            {
                if(field.FieldType==typeof(EntityQuery))continue;
                string path=prefix.Length==0?field.Name:prefix+"."+field.Name;
                output.Add(field.Name,Value(field.GetValue(value),path));
            }
            return output;
        }
        static object Value(object value,string path)
        {
            if(value==null)return null;var type=value.GetType();
            if(type.IsPrimitive||type.IsEnum||type==typeof(string)||type==typeof(decimal)||type==typeof(Entity)||type.Namespace=="Unity.Mathematics"||
                type.Namespace=="Game.Components"||type.Namespace=="Game.Runtime.Pathfinding"||type.Name.StartsWith("FixedString",StringComparison.Ordinal))return value;
            if(type==typeof(JobHandle))return new Dictionary<string,object>{{"kind","inactive-job-handle"},{"is_default",((JobHandle)value).Equals(default(JobHandle))},{"rebind_on_import",true}};
            if(type==typeof(NativeStream))return new Dictionary<string,object>{{"kind","native-stream"},{"is_created",((NativeStream)value).IsCreated}};
            if(type==typeof(NativeBitArray))
            {var bits=(NativeBitArray)value;bool[] data=null;if(bits.IsCreated){S002InitialWorldPrivateCopyBudget.Charge(bits.Length,UnsafeUtility.SizeOf(typeof(bool)),S002InitialWorldPrivateCopyBudget.MaximumGridElements,path);data=new bool[bits.Length];for(int i=0;i<data.Length;i++)data[i]=bits.IsSet(i);}return new Dictionary<string,object>{{"is_created",bits.IsCreated},{"length",bits.IsCreated?bits.Length:0},{"data",data}};}
            if(type.IsGenericType&&(type.GetGenericTypeDefinition()==typeof(NativeArray<>)||type.GetGenericTypeDefinition()==typeof(NativeList<>)))
            {
                bool created=(bool)type.GetProperty("IsCreated").GetValue(value);var result=new Dictionary<string,object>{{"is_created",created},{"type",type.FullName}};
                if(!created)return result;
                int length=(int)type.GetProperty("Length").GetValue(value);
                long maximum=path.StartsWith("_liveUnitSnapshot.",StringComparison.Ordinal)||path.StartsWith("_requestBuffers.",StringComparison.Ordinal)?S002InitialWorldPrivateCopyBudget.MaximumEntities:S002InitialWorldPrivateCopyBudget.MaximumGridElements;
                S002InitialWorldPrivateCopyBudget.Bound(length,maximum,path);result["length"]=length;
                var capacity=type.GetProperty("Capacity");if(capacity!=null){int reserved=(int)capacity.GetValue(value);S002InitialWorldPrivateCopyBudget.Bound(reserved,S002InitialWorldPrivateCopyBudget.MaximumGridElements,path+".capacity");if(reserved<length)throw new InvalidOperationException("Native capacity smaller than logical length: "+path);result["capacity"]=reserved;}
                if(path.StartsWith("_scratchWorkspace.",StringComparison.Ordinal)&&
                    (path.EndsWith(".CameFrom",StringComparison.Ordinal)||path.EndsWith(".GScore",StringComparison.Ordinal)||path.EndsWith(".Open",StringComparison.Ordinal)||path.EndsWith(".Path",StringComparison.Ordinal)))
                {result["contents_status"]="not-live-uninitialized-scratch; only admitted with searchEpoch1 and zero Epoch plane";return result;}
                int elementSize=UnsafeUtility.SizeOf(type.GetGenericArguments()[0]);
                S002InitialWorldPrivateCopyBudget.Charge(length,elementSize,maximum,path);
                object array=value;if(type.GetGenericTypeDefinition()==typeof(NativeList<>))array=type.GetMethod("AsArray",Type.EmptyTypes).Invoke(value,null);
                result["data"]=array.GetType().GetMethod("ToArray",Type.EmptyTypes).Invoke(array,null);return result;
            }
            if(type==typeof(EntityTypeHandle)||type.IsGenericType&&(type.GetGenericTypeDefinition()==typeof(ComponentLookup<>)||type.GetGenericTypeDefinition()==typeof(ComponentTypeHandle<>)||type.GetGenericTypeDefinition()==typeof(BufferLookup<>)||type.GetGenericTypeDefinition()==typeof(BufferTypeHandle<>)))
                return new Dictionary<string,object>{{"kind","runtime-binding"},{"type",type.FullName},{"rebind_on_import",true}};
            if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(NativeParallelHashMap<,>))return value;
            if(type.IsPointer||!type.IsValueType)throw new InvalidOperationException("Unsupported private authority type at "+path+": "+type.FullName);
            return Fields(value,path);
        }
        internal static void BoundGrid(World world,EntityQuery query)
        {
            if(query.CalculateEntityCount()!=1)throw new InvalidOperationException("Unique live grid required before private copies");
            var grid=world.EntityManager.GetComponentData<GridConfig>(query.GetSingletonEntity());
            if(grid.Width<=0||grid.Height<=0)throw new InvalidOperationException("Positive grid dimensions required before private copies");
            S002InitialWorldPrivateCopyBudget.Bound(checked((long)grid.Width*grid.Height),S002InitialWorldPrivateCopyBudget.MaximumGridElements,"grid.cells");
        }
        internal static void ValidateQueryFilters(object owner)
        {
            if(owner==null)return;
            foreach(var field in owner.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            {
                var type=field.FieldType;var value=field.GetValue(owner);
                if(type==typeof(EntityQuery))
                {
                    var query=(EntityQuery)value;if(query.Equals(default(EntityQuery)))continue;
                    var filter=S002InitialWorldQueryFilterAuthority.Capture(query);
                    if(!filter.TryGetValue("supported",out var supported)||!(supported is bool ok&&ok))throw new InvalidOperationException("Unsupported mutable query filter: "+field.Name);
                }
                else if(type.IsValueType&&!type.IsPrimitive&&!type.IsEnum&&type.Namespace!=null&&type.Namespace.StartsWith("Game.",StringComparison.Ordinal))ValidateQueryFilters(value);
            }
        }
        internal static Dictionary<string,object> CaptureQueries(World world,object owner)
        {
            var output=new Dictionary<string,object>();CollectQueries(world,owner,"",output);return output;
        }
        static void CollectQueries(World world,object value,string prefix,Dictionary<string,object> output)
        {
            if(value==null)return;
            foreach(var field in value.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            {
                string path=prefix.Length==0?field.Name:prefix+"."+field.Name;var type=field.FieldType;var current=field.GetValue(value);
                if(type==typeof(EntityQuery)){output[path]=Query((EntityQuery)current,world.EntityManager.GetEntityTypeHandle());continue;}
                // Query owners are Game.Runtime value structs. Native storage and ECS
                // binding internals are deliberately not traversed as logical owners.
                if(type.IsValueType&&!type.IsPrimitive&&!type.IsEnum&&type.Namespace!=null&&type.Namespace.StartsWith("Game.",StringComparison.Ordinal))CollectQueries(world,current,path,output);
            }
        }
        internal static Dictionary<string,object> Query(EntityQuery query,EntityTypeHandle entityType)
        {
            if(query.Equals(default(EntityQuery)))return new Dictionary<string,object>{{"available",false}};
            var filter=S002InitialWorldQueryFilterAuthority.Capture(query);
            if(!filter.TryGetValue("supported",out var supported)||!(supported is bool ok&&ok))throw new InvalidOperationException("Unsupported mutable query filter before query copies");
            int upperChunks=query.CalculateChunkCountWithoutFiltering(),upperEntities=query.CalculateEntityCountWithoutFiltering();
            S002InitialWorldPrivateCopyBudget.Bound(upperChunks,S002InitialWorldPrivateCopyBudget.MaximumChunks,"query.unfiltered_chunks");
            S002InitialWorldPrivateCopyBudget.Bound(upperEntities,S002InitialWorldPrivateCopyBudget.MaximumEntities,"query.unfiltered_entities");
            int entityCount=query.CalculateEntityCount(),chunkCount=query.CalculateChunkCount();
            if(entityCount<0||entityCount>upperEntities||chunkCount<0||chunkCount>upperChunks)throw new InvalidOperationException("Query count changed or invalid");
            S002InitialWorldPrivateCopyBudget.Charge(upperChunks,UnsafeUtility.SizeOf(typeof(ArchetypeChunk)),S002InitialWorldPrivateCopyBudget.MaximumChunks,"query.native_chunks",64);
            S002InitialWorldPrivateCopyBudget.Charge(chunkCount,IntPtr.Size,S002InitialWorldPrivateCopyBudget.MaximumChunks,"query.chunk_references",checked(32L+32L*chunkCount));
            S002InitialWorldPrivateCopyBudget.Charge(entityCount,checked(UnsafeUtility.SizeOf(typeof(Entity))*2),S002InitialWorldPrivateCopyBudget.MaximumEntities,"query.flat_and_chunk_entities",64);
            using var chunks=query.ToArchetypeChunkArray(Allocator.Temp);
            if(chunks.Length!=chunkCount)throw new InvalidOperationException("Query chunk count changed before copy");
            var entities=new Entity[entityCount];var chunkOrders=new Entity[chunkCount][];int offset=0;
            for(int i=0;i<chunks.Length;i++)
            {
                var source=chunks[i].GetNativeArray(entityType);
                if(source.Length>entityCount-offset)throw new InvalidOperationException("Per-entity enableable filtering or changed query requires an explicit adapter");
                chunkOrders[i]=source.ToArray();Array.Copy(chunkOrders[i],0,entities,offset,source.Length);offset+=source.Length;
            }
            if(offset!=entityCount)throw new InvalidOperationException("Query entity count changed during copy");
            return new Dictionary<string,object>{{"available",true},{"entities",entities},{"chunks",chunkOrders},{"descriptions",query.GetEntityQueryDescs()},{"filter",filter},
                {"precondition","actual spawn/roster initialization barrier; no pending path batch; source chunk API can synchronize change/enableable filters"}};
        }
        internal static Dictionary<string,object> SystemStateValues(ref SystemState state)=>new Dictionary<string,object>
            {{"last_system_version",state.LastSystemVersion},{"global_system_version",state.GlobalSystemVersion},{"enabled",state.Enabled}};
    }
}
#endif
