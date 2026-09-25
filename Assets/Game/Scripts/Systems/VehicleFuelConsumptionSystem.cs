using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    public partial struct VehicleFuelConsumptionSystem : ISystem
    {
        private const int FactionCapacity = 256;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitFuelConsumption>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeArray<float> requestedFuelByFaction = new(FactionCapacity, Allocator.Temp);
            NativeArray<float> requestedExpandedFuelByFaction = new(FactionCapacity, Allocator.Temp);
            ComponentLookup<UnitAirMovement> airMovementLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true);
            ComponentLookup<SkirmishAttemptOwnedComponent> attemptLookup =
                SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true);
            using var expanded = state.EntityManager.CreateEntityQuery(
                typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupComponent),
                typeof(SkirmishEconomyStockComponent), typeof(SkirmishEnemyStockComponent));
            bool hasExpanded = expanded.CalculateEntityCount() == 1;
            Entity sessionEntity = hasExpanded ? expanded.GetSingletonEntity() : Entity.Null;
            // Migrated sessions use the same physical drain as every other mode.
            hasExpanded = hasExpanded && !state.EntityManager.HasComponent<SkirmishSharedSupplyInitialized>(sessionEntity);
            SkirmishExpandedSessionComponent session = hasExpanded
                ? state.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity) : default;

            foreach (var (unitGrid, faction, movement, consumption, consumptionState, entity) in SystemAPI
                         .Query<RefRO<UnitGrid>, RefRO<Faction>, RefRO<UnitMovementBehavior>, RefRO<UnitFuelConsumption>, RefRW<UnitFuelConsumptionState>>()
                         .WithEntityAccess())
            {
                if (consumption.ValueRO.Enabled == 0)
                    continue;

                ref UnitFuelConsumptionState stateRw = ref consumptionState.ValueRW;
                int2 cell = unitGrid.ValueRO.Cell;
                if (stateRw.Initialized == 0)
                {
                    stateRw.LastCell = cell;
                    stateRw.Initialized = 1;
                    continue;
                }

                int2 delta = cell - stateRw.LastCell;
                int movedCells = math.abs(delta.x) + math.abs(delta.y);
                if (movedCells <= 0)
                    continue;

                bool isFuelUsingUnit = movement.ValueRO.UsesVehicleMotion != 0 || airMovementLookup.HasComponent(entity);
                if (!isFuelUsingUnit)
                {
                    stateRw.LastCell = cell;
                    continue;
                }

                float fuelPerCell = airMovementLookup.HasComponent(entity)
                    ? math.max(0f, consumption.ValueRO.AirFuelPerCell)
                    : math.max(0f, consumption.ValueRO.GroundFuelPerCell);
                float requestedFuel = movedCells * fuelPerCell;
                if (requestedFuel > 0f)
                {
                    bool expandedUnit = hasExpanded && attemptLookup.HasComponent(entity) &&
                        attemptLookup[entity].SessionId.Equals(session.SessionId);
                    if (expandedUnit)
                        requestedExpandedFuelByFaction[faction.ValueRO.Id] += requestedFuel;
                    else
                        requestedFuelByFaction[faction.ValueRO.Id] += requestedFuel;
                }

                stateRw.LastCell = cell;
            }

            DrainRequestedFuel(ref state, requestedFuelByFaction);
            if (hasExpanded)
                DrainExpandedFuel(ref state, sessionEntity, requestedExpandedFuelByFaction);
            requestedFuelByFaction.Dispose();
            requestedExpandedFuelByFaction.Dispose();
        }

        private static void DrainExpandedFuel(ref SystemState state, Entity sessionEntity,
            NativeArray<float> requested)
        {
            EntityManager em = state.EntityManager;
            SkirmishResolvedSetupComponent setup = em.GetComponentData<SkirmishResolvedSetupComponent>(sessionEntity);
            SkirmishVehicleFuelRemainderComponent remainder =
                em.HasComponent<SkirmishVehicleFuelRemainderComponent>(sessionEntity)
                    ? em.GetComponentData<SkirmishVehicleFuelRemainderComponent>(sessionEntity) : default;
            SkirmishEconomyStockComponent player = em.GetComponentData<SkirmishEconomyStockComponent>(sessionEntity);
            SkirmishEnemyStockComponent enemy = em.GetComponentData<SkirmishEnemyStockComponent>(sessionEntity);
            Drain(ref player.Fuel, ref remainder.Player, requested[(byte)setup.PlayerFaction]);
            Drain(ref enemy.Fuel, ref remainder.Enemy, requested[(byte)setup.EnemyFaction]);
            em.SetComponentData(sessionEntity, player);
            em.SetComponentData(sessionEntity, enemy);
            if (em.HasComponent<SkirmishVehicleFuelRemainderComponent>(sessionEntity))
                em.SetComponentData(sessionEntity, remainder);
            else
                em.AddComponentData(sessionEntity, remainder);
        }

        private static void Drain(ref int stock, ref float remainder, float requested)
        {
            if (stock <= 0)
            {
                remainder = 0f;
                return;
            }
            float total = math.max(0f, remainder + requested);
            int whole = (int)math.floor(total);
            int paid = math.min(stock, whole);
            stock -= paid;
            remainder = stock > 0 ? total - whole : 0f;
        }

        private void DrainRequestedFuel(ref SystemState state, NativeArray<float> requestedFuelByFaction)
        {
            for (int factionId = 0; factionId < requestedFuelByFaction.Length; factionId++)
            {
                float remaining = requestedFuelByFaction[factionId];
                if (remaining <= 0f)
                    continue;

                foreach (RefRW<BuildingResourceStorageComponent> storageRef in SystemAPI.Query<RefRW<BuildingResourceStorageComponent>>())
                {
                    ref BuildingResourceStorageComponent storage = ref storageRef.ValueRW;
                    if (storage.OwnerFactionId != (byte)factionId ||
                        !IsUsableFuelStorage(storage))
                    {
                        continue;
                    }

                    float availableFuel = math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels - storage.CivilianFuelReserveBarrels);
                    float drained = math.min(availableFuel, remaining);
                    if (drained <= 0f)
                        continue;

                    storage.StoredFuelBarrels = math.max(0f, storage.StoredFuelBarrels - drained);
                    storage.Version++;
                    remaining -= drained;
                    if (remaining <= 0.001f)
                        break;
                }
            }
        }

        private static bool IsUsableFuelStorage(in BuildingResourceStorageComponent storage)
        {
            return storage.FuelStorageCapacity > 0 &&
                   storage.FuelBarrelsPerDay <= 0f &&
                   storage.OilBarrelsPerDay <= 0f;
        }
    }
}
