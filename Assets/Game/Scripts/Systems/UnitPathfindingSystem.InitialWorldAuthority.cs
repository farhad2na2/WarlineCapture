#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    public partial struct UnitPathfindingSystem
    {
        private S002InitialWorldBoundaryCounter _s002InitialWorldWitness;
        private void ObserveInitialWorldCreate(ref SystemState state)=>_s002InitialWorldWitness.Created(state.WorldUnmanaged.SequenceNumber,state.GlobalSystemVersion,state.WorldUnmanaged.Time.ElapsedTime,S002InitialWorldObserverControl.ObserverEpoch,S002InitialWorldObserverControl.ObserverEnabled);
        private void ObserveInitialWorldUpdateEntry(ref SystemState state)=>_s002InitialWorldWitness.Enter(state.WorldUnmanaged.SequenceNumber,state.GlobalSystemVersion,state.WorldUnmanaged.Time.ElapsedTime,S002InitialWorldObserverControl.ObserverEpoch,S002InitialWorldObserverControl.ObserverEnabled);
        internal static S002InitialWorldBoundaryCounter InitialWorldCounter(World world)
        {
            var handle=world.Unmanaged.GetExistingUnmanagedSystem<UnitPathfindingSystem>();
            return handle==SystemHandle.Null?default:world.Unmanaged.GetUnsafeSystemRef<UnitPathfindingSystem>(handle)._s002InitialWorldWitness;
        }
        public static bool HasPendingInitialWorldWork(World world)
        {
            if(world==null||!world.IsCreated)return true;
            var handle=world.Unmanaged.GetExistingUnmanagedSystem<UnitPathfindingSystem>();
            if(handle==SystemHandle.Null)return true;
            ref var system=ref world.Unmanaged.GetUnsafeSystemRef<UnitPathfindingSystem>(handle);
            return system._hasPendingPathJob||system._pendingPathStream.IsCreated;
        }
        public static Dictionary<string,object> CaptureInitialWorldAuthority(World world)
        {
            using var privateCopyBudget=S002InitialWorldBoundaryWitness.BeginPrivateCaptureBudget();
            if(world==null||!world.IsCreated)throw new InvalidOperationException("Live existing World required");
            var handle=world.Unmanaged.GetExistingUnmanagedSystem<UnitPathfindingSystem>();
            var result=new Dictionary<string,object>{{"supported",false},{"world_sequence",world.SequenceNumber},{"observer_enabled",S002InitialWorldBoundaryWitness.ObserverEnabled}};
            if(!S002InitialWorldBoundaryWitness.ObserverEnabled){result["reason"]="observer-not-enabled";return result;}
            if(handle==SystemHandle.Null){result["reason"]="pathfinding-system-missing";return result;}
            ref var system=ref world.Unmanaged.GetUnsafeSystemRef<UnitPathfindingSystem>(handle);
            ref var state=ref world.Unmanaged.ResolveSystemStateRef(handle);
            var boundary=S002InitialWorldBoundaryWitness.Capture(world);result["boundary_witness"]=boundary;
            if(!(bool)boundary["boundary_valid"]){result["reason"]="same-world-pre-consumer-spawn-boundary-not-proven";return result;}
            result["system_state"]=S002InitialWorldBoundaryWitness.SystemStateValues(ref state);
            result["counter"]=system._s002InitialWorldWitness;
            result["adaptive_budget"]=S002InitialWorldBoundaryWitness.Fields(system._budget);
            result["pending_metadata"]=new Dictionary<string,object>{{"request_count",system._pendingRequestCount},{"request_budget",system._pendingRequestBudget},
                {"live_unit_count",system._pendingLiveUnitCount},{"grid_width",system._pendingGridWidth},{"grid_height",system._pendingGridHeight},
                {"schedule_frame",system._pendingScheduleFrame},{"schedule_time",system._pendingScheduleTime}};
            result["has_pending_path_job"]=system._hasPendingPathJob;result["pending_stream_created"]=system._pendingPathStream.IsCreated;
            if(system._hasPendingPathJob||system._pendingPathStream.IsCreated)
            {result["reason"]="pending-or-orphan-stream-no-completion-or-IsCompleted-read-permitted";return result;}
            S002InitialWorldBoundaryWitness.BoundGrid(world,system._queries.GridQuery);
            // No unknown bytes from UninitializedMemory scratch or partially populated
            // request outputs are read. Warm support requires a separate valid-region proof.
            int searchEpoch=(int)typeof(UnitPathScratchWorkspace).GetField("_searchEpoch",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(system._scratchWorkspace);
            result["scratch_search_epoch"]=searchEpoch;result["scratch_created"]=system._scratchWorkspace.CameFrom.IsCreated;
            result["coarse_created"]=system._coarseWorkspace.CameFrom.IsCreated;
            bool requestsEmpty=true;
            foreach(var field in typeof(UnitPathRequestBuffer).GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            {
                var list=field.GetValue(system._requestBuffers);var type=field.FieldType;
                if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(Unity.Collections.NativeList<>)&&
                    (bool)type.GetProperty("IsCreated").GetValue(list)&&(int)type.GetProperty("Length").GetValue(list)!=0)requestsEmpty=false;
            }
            result["private_request_lists_empty"]=requestsEmpty;
            if(system._coarseWorkspace.CameFrom.IsCreated||searchEpoch!=1||!requestsEmpty)
            {result["reason"]="warm-private-workspaces-require-valid-region-proof";return result;}
            if(system._scratchWorkspace.Epoch.IsCreated)
                S002InitialWorldPrivateCopyBudget.Bound(system._scratchWorkspace.Epoch.Length,S002InitialWorldPrivateCopyBudget.MaximumGridElements,"scratch.epoch_plane");
            if(system._scratchWorkspace.Epoch.IsCreated)
                for(int i=0;i<system._scratchWorkspace.Epoch.Length;i++)if(system._scratchWorkspace.Epoch[i]!=0)
                {result["reason"]="nonzero-scratch-epoch-plane-requires-valid-region-proof";return result;}
            S002InitialWorldBoundaryWitness.ValidateQueryFilters(system);
            result["private_fields"]=S002InitialWorldBoundaryWitness.Fields(system);
            result["query_order"]=S002InitialWorldBoundaryWitness.CaptureQueries(world,system);
            int grids=system._queries.GridQuery.CalculateEntityCount();result["grid_query_count"]=grids;
            if(grids!=1){result["reason"]="unique-live-grid-required";return result;}
            var grid=system._queries.GridQuery.GetSingletonEntity();result["grid_entity"]=grid;
            if(!world.EntityManager.HasComponent<PathPoolComponent>(grid)){result["reason"]="live-path-pool-missing";return result;}
            result["path_pool"]=S002InitialWorldBoundaryWitness.Fields(world.EntityManager.GetComponentData<PathPoolComponent>(grid));
            int pendingCount=system._pendingStateQuery.CalculateEntityCount();result["pending_projection_count"]=pendingCount;
            if(pendingCount!=1){result["reason"]="unique-pending-projection-required";return result;}
            result["pending_projection"]=system._pendingStateQuery.GetSingleton<UnitPathfindingPendingStateComponent>();
            result["supported"]=true;result["scratch_contents_policy"]="uninitialized/non-live storage represented by actual creation/length; Epoch1 and zero plane verified";
            return result;
        }
    }
}
#endif
