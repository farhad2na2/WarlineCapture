#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    public partial struct DynamicOccupancyRebuildSystem
    {
        private S002InitialWorldBoundaryCounter _s002InitialWorldWitness;
        private void ObserveInitialWorldCreate(ref SystemState state)=>_s002InitialWorldWitness.Created(state.WorldUnmanaged.SequenceNumber,state.GlobalSystemVersion,state.WorldUnmanaged.Time.ElapsedTime,S002InitialWorldObserverControl.ObserverEpoch,S002InitialWorldObserverControl.ObserverEnabled);
        private void ObserveInitialWorldUpdateEntry(ref SystemState state)=>_s002InitialWorldWitness.Enter(state.WorldUnmanaged.SequenceNumber,state.GlobalSystemVersion,state.WorldUnmanaged.Time.ElapsedTime,S002InitialWorldObserverControl.ObserverEpoch,S002InitialWorldObserverControl.ObserverEnabled);
        internal static S002InitialWorldBoundaryCounter InitialWorldCounter(World world)
        {
            var handle=world.Unmanaged.GetExistingUnmanagedSystem<DynamicOccupancyRebuildSystem>();
            return handle==SystemHandle.Null?default:world.Unmanaged.GetUnsafeSystemRef<DynamicOccupancyRebuildSystem>(handle)._s002InitialWorldWitness;
        }
        public static Dictionary<string,object> CaptureInitialWorldAuthority(World world)
        {
            using var privateCopyBudget=S002InitialWorldBoundaryWitness.BeginPrivateCaptureBudget();
            if(world==null||!world.IsCreated)throw new InvalidOperationException("Live existing World required");
            var handle=world.Unmanaged.GetExistingUnmanagedSystem<DynamicOccupancyRebuildSystem>();
            var result=new Dictionary<string,object>{{"supported",false},{"world_sequence",world.SequenceNumber},{"observer_enabled",S002InitialWorldBoundaryWitness.ObserverEnabled}};
            if(!S002InitialWorldBoundaryWitness.ObserverEnabled){result["reason"]="observer-not-enabled";return result;}
            if(handle==SystemHandle.Null){result["reason"]="occupancy-system-missing";return result;}
            ref var system=ref world.Unmanaged.GetUnsafeSystemRef<DynamicOccupancyRebuildSystem>(handle);
            ref var state=ref world.Unmanaged.ResolveSystemStateRef(handle);
            var boundary=S002InitialWorldBoundaryWitness.Capture(world);result["boundary_witness"]=boundary;
            if(!(bool)boundary["boundary_valid"]){result["reason"]="same-world-pre-consumer-spawn-boundary-not-proven";return result;}
            result["system_state"]=S002InitialWorldBoundaryWitness.SystemStateValues(ref state);
            S002InitialWorldBoundaryWitness.BoundGrid(world,system._gridQuery);
            S002InitialWorldBoundaryWitness.ValidateQueryFilters(system);
            var fields=S002InitialWorldBoundaryWitness.Fields(system);
            var records=new Dictionary<string,object>{{"is_created",system._occupancyRecords.IsCreated}};
            if(system._occupancyRecords.IsCreated)
            {
                int count=system._occupancyRecords.Count(),capacity=system._occupancyRecords.Capacity;
                S002InitialWorldPrivateCopyBudget.Bound(count,S002InitialWorldPrivateCopyBudget.MaximumEntities,"occupancy.records.count");
                S002InitialWorldPrivateCopyBudget.Bound(capacity,S002InitialWorldPrivateCopyBudget.MaximumMapCapacity,"occupancy.records.capacity");
                if(capacity<count)throw new InvalidOperationException("Occupancy record capacity below count");
                int keys=Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(typeof(Entity));
                int entry=Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf(typeof(OccupancyRecord));
                S002InitialWorldPrivateCopyBudget.Charge(count,checked(keys*2+entry+IntPtr.Size+512),S002InitialWorldPrivateCopyBudget.MaximumEntities,"occupancy.native_kv_managed_keys_and_rows",128);
                using var pairs=system._occupancyRecords.GetKeyValueArrays(Allocator.Temp);
                if(pairs.Keys.Length!=count||pairs.Values.Length!=count)throw new InvalidOperationException("Occupancy map changed before copy");
                var values=new Dictionary<string,object>[count];
                for(int i=0;i<values.Length;i++)values[i]=new Dictionary<string,object>{{"cell",pairs.Values[i].Cell},{"size",pairs.Values[i].Size}};
                records["keys"]=pairs.Keys.ToArray();records["values"]=values;records["capacity"]=system._occupancyRecords.Capacity;
            }
            fields["_occupancyRecords"]=records;result["private_fields"]=fields;
            result["query_order"]=S002InitialWorldBoundaryWitness.CaptureQueries(world,system);
            if(system._gridQuery.CalculateEntityCount()!=1){result["reason"]="unique-live-grid-required";return result;}
            var grid=system._gridQuery.GetSingletonEntity();result["grid_entity"]=grid;
            if(!world.EntityManager.HasComponent<DynamicOccupancyComponent>(grid)){result["reason"]="occupancy-component-missing";return result;}
            result["dynamic_occupancy"]=S002InitialWorldBoundaryWitness.Fields(world.EntityManager.GetComponentData<DynamicOccupancyComponent>(grid));
            result["unit_grid_component_order_version"]=world.EntityManager.GetComponentOrderVersion<UnitGrid>();
            result["supported"]=true;return result;
        }
    }
}
#endif
