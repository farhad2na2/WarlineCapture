using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitGridMovementSystem))]
    public partial struct GroundVehicleFuelHoldSystem : ISystem
    {
        private const int FactionCapacity = 256;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitFuelConsumption>();
            state.RequireForUpdate<BuildingResourceStorageComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeArray<float> usableFuelByFaction = new(FactionCapacity, Allocator.Temp);
            foreach (RefRO<BuildingResourceStorageComponent> storageRef in SystemAPI.Query<RefRO<BuildingResourceStorageComponent>>())
            {
                BuildingResourceStorageComponent storage = storageRef.ValueRO;
                if (!IsUsableFuelStorage(storage))
                    continue;

                usableFuelByFaction[storage.OwnerFactionId] += math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels);
            }

            EntityCommandBuffer ecb = new(Allocator.Temp);
            ComponentLookup<UnitTarget> targetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true);
            ComponentLookup<UnitPathRequest> pathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true);
            ComponentLookup<UnitPathFollow> pathFollowLookup = SystemAPI.GetComponentLookup<UnitPathFollow>(true);
            ComponentLookup<UnitPathRange> pathRangeLookup = SystemAPI.GetComponentLookup<UnitPathRange>(true);
            ComponentLookup<UnitPathRetryCooldown> retryLookup = SystemAPI.GetComponentLookup<UnitPathRetryCooldown>(true);
            ComponentLookup<UnitLongDistanceMove> longDistanceLookup = SystemAPI.GetComponentLookup<UnitLongDistanceMove>(true);
            ComponentLookup<ManualMoveOrderTag> manualMoveLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true);
            ComponentLookup<UnitVehicleKinematics> kinematicsLookup = SystemAPI.GetComponentLookup<UnitVehicleKinematics>(true);

            foreach (var (faction, movement, consumption, entity) in SystemAPI
                         .Query<RefRO<Faction>, RefRO<UnitMovementBehavior>, RefRO<UnitFuelConsumption>>()
                         .WithNone<UnitAirMovement>()
                         .WithEntityAccess())
            {
                if (movement.ValueRO.UsesVehicleMotion == 0 ||
                    consumption.ValueRO.Enabled == 0 ||
                    math.max(0f, consumption.ValueRO.GroundFuelPerCell) <= 0f ||
                    usableFuelByFaction[faction.ValueRO.Id] > 0.001f ||
                    !HasActiveMovement(entity, targetLookup, pathRequestLookup, pathFollowLookup, manualMoveLookup))
                {
                    continue;
                }

                RemoveIfPresent<UnitTarget>(ecb, entity, targetLookup);
                RemoveIfPresent<UnitPathRequest>(ecb, entity, pathRequestLookup);
                RemoveIfPresent<UnitPathFollow>(ecb, entity, pathFollowLookup);
                RemoveIfPresent<UnitPathRange>(ecb, entity, pathRangeLookup);
                RemoveIfPresent<UnitPathRetryCooldown>(ecb, entity, retryLookup);
                RemoveIfPresent<UnitLongDistanceMove>(ecb, entity, longDistanceLookup);
                RemoveIfPresent<ManualMoveOrderTag>(ecb, entity, manualMoveLookup);

                if (kinematicsLookup.HasComponent(entity))
                {
                    ecb.SetComponent(entity, new UnitVehicleKinematics
                    {
                        CurrentSpeed = 0f,
                        StallSeconds = 0f
                    });
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            usableFuelByFaction.Dispose();
        }

        private static bool HasActiveMovement(
            Entity entity,
            ComponentLookup<UnitTarget> targetLookup,
            ComponentLookup<UnitPathRequest> pathRequestLookup,
            ComponentLookup<UnitPathFollow> pathFollowLookup,
            ComponentLookup<ManualMoveOrderTag> manualMoveLookup)
        {
            return targetLookup.HasComponent(entity) ||
                   pathRequestLookup.HasComponent(entity) ||
                   pathFollowLookup.HasComponent(entity) ||
                   manualMoveLookup.HasComponent(entity);
        }

        private static bool IsUsableFuelStorage(in BuildingResourceStorageComponent storage)
        {
            return storage.FuelStorageCapacity > 0 &&
                   storage.FuelBarrelsPerDay <= 0f &&
                   storage.OilBarrelsPerDay <= 0f;
        }

        private static void RemoveIfPresent<T>(
            EntityCommandBuffer ecb,
            Entity entity,
            ComponentLookup<T> lookup)
            where T : unmanaged, IComponentData
        {
            if (lookup.HasComponent(entity))
                ecb.RemoveComponent<T>(entity);
        }
    }
}
