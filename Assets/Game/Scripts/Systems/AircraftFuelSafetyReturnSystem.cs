using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitAirMovementSystem))]
    public partial struct AircraftFuelSafetyReturnSystem : ISystem
    {
        private const int FactionCapacity = 256;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitAirMovement>();
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
            ComponentLookup<EngageTarget> engageLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);
            ComponentLookup<UnitPathRequest> pathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true);
            ComponentLookup<ManualMoveOrderTag> manualMoveLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true);
            ComponentLookup<UnitScanOrder> scanOrderLookup = SystemAPI.GetComponentLookup<UnitScanOrder>(true);
            ComponentLookup<UnitTransportAirdropRequest> airdropLookup = SystemAPI.GetComponentLookup<UnitTransportAirdropRequest>(true);
            ComponentLookup<UnitTransportRopeDisembarkRequest> ropeDisembarkLookup = SystemAPI.GetComponentLookup<UnitTransportRopeDisembarkRequest>(true);

            foreach (var (faction, consumption, airState, entity) in SystemAPI
                         .Query<RefRO<Faction>, RefRO<UnitFuelConsumption>, RefRW<UnitAirComponent>>()
                         .WithAll<UnitAirMovement>()
                         .WithEntityAccess())
            {
                if (consumption.ValueRO.Enabled == 0 ||
                    math.max(0f, consumption.ValueRO.AirFuelPerCell) <= 0f ||
                    usableFuelByFaction[faction.ValueRO.Id] > 0.001f ||
                    !IsActiveAircraft(airState.ValueRO, targetLookup.HasComponent(entity), engageLookup.HasComponent(entity), scanOrderLookup.HasComponent(entity), airdropLookup.HasComponent(entity), ropeDisembarkLookup.HasComponent(entity)))
                {
                    continue;
                }

                ref UnitAirComponent air = ref airState.ValueRW;
                air.ReturningHome = 1;
                air.AttackRunActive = 0;
                air.ReturnApproachInitialized = 0;

                RemoveIfPresent<UnitTarget>(ecb, entity, targetLookup);
                RemoveIfPresent<EngageTarget>(ecb, entity, engageLookup);
                RemoveIfPresent<UnitPathRequest>(ecb, entity, pathRequestLookup);
                RemoveIfPresent<ManualMoveOrderTag>(ecb, entity, manualMoveLookup);
                RemoveIfPresent<UnitScanOrder>(ecb, entity, scanOrderLookup);
                RemoveIfPresent<UnitTransportAirdropRequest>(ecb, entity, airdropLookup);
                RemoveIfPresent<UnitTransportRopeDisembarkRequest>(ecb, entity, ropeDisembarkLookup);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            usableFuelByFaction.Dispose();
        }

        private static bool IsActiveAircraft(
            in UnitAirComponent air,
            bool hasTarget,
            bool hasEngage,
            bool hasScan,
            bool hasAirdrop,
            bool hasRopeDisembark)
        {
            return air.Airborne != 0 ||
                   air.TakeoffRolling != 0 ||
                   air.LandingRolling != 0 ||
                   air.ReturningHome != 0 ||
                   hasTarget ||
                   hasEngage ||
                   hasScan ||
                   hasAirdrop ||
                   hasRopeDisembark;
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
