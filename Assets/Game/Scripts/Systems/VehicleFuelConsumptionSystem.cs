using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
            bool steelPush=CampaignSteelPushFuelScope.TryGet(state.EntityManager,out var steelReserve,out var steelRuntime);
            ComponentLookup<UnitAirMovement> airMovementLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true);
            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var physicalGrids = SystemAPI.QueryBuilder().WithAll<GridConfig>().Build();
#if UNITY_EDITOR
            int continuousGridCount = physicalGrids.CalculateEntityCount();
            bool hasGrid = continuousGridCount == 1;
#else
            bool hasGrid = physicalGrids.CalculateEntityCount() == 1;
#endif
            GridConfig physicalGrid = hasGrid ? physicalGrids.GetSingleton<GridConfig>() : default;
            ComponentLookup<SkirmishAttemptOwnedComponent> attemptLookup =
                SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true);
            using var expanded = state.EntityManager.CreateEntityQuery(
                typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupComponent),
                typeof(SkirmishEconomyStockComponent), typeof(SkirmishEnemyStockComponent));
#if UNITY_EDITOR
            int continuousExpandedCount = expanded.CalculateEntityCount();
            bool hasExpanded = continuousExpandedCount == 1;
#else
            bool hasExpanded = expanded.CalculateEntityCount() == 1;
#endif
            Entity sessionEntity = hasExpanded ? expanded.GetSingletonEntity() : Entity.Null;
            // Migrated sessions use the same physical drain as every other mode.
            hasExpanded = hasExpanded && !state.EntityManager.HasComponent<SkirmishSharedSupplyInitialized>(sessionEntity);
            SkirmishExpandedSessionComponent session = hasExpanded
                ? state.EntityManager.GetComponentData<SkirmishExpandedSessionComponent>(sessionEntity) : default;

#if UNITY_EDITOR
            S002PhysicalStartupObservation.ForwardFuelBoundary?.Invoke(state.EntityManager, "before-ground-fuel-consumer", steelPush, expanded.CalculateEntityCount(), hasExpanded);
#endif
#if UNITY_EDITOR
            S002PhysicalStartupObservation.ContinuousFuelStart(state.EntityManager, "before-ground-fuel-consumer", steelPush, continuousExpandedCount, hasExpanded, continuousGridCount, physicalGrid);
#endif
            foreach (var (unitGrid, faction, movement, consumption, consumptionState, entity) in SystemAPI
                         .Query<RefRO<UnitGrid>, RefRO<Faction>, RefRO<UnitMovementBehavior>, RefRO<UnitFuelConsumption>, RefRW<UnitFuelConsumptionState>>()
                         .WithEntityAccess())
            {
#if UNITY_EDITOR
                S002PhysicalStartupObservation.ForwardFuelUnit?.Invoke(state.EntityManager, entity);
#endif
#if UNITY_EDITOR
                if (consumption.ValueRO.Enabled == 0 && S002PhysicalStartupObservation.ContinuousFuel != null && S002PhysicalStartupObservation.ContinuousRead(3))
                {
                    bool continuousAirDisabled = airMovementLookup.HasComponent(entity);
                    bool continuousTransformDisabled = transformLookup.HasComponent(entity);
                    var continuousPositionDisabled = continuousTransformDisabled ? transformLookup[entity].Position : default;
                    if (S002PhysicalStartupObservation.ContinuousRead(20)) S002PhysicalStartupObservation.ContinuousSend(S002PhysicalStartupObservation.ContinuousFuel, new S002PhysicalStartupObservation.ContinuousFuelFacts { Manager = state.EntityManager, Entity = entity, Cell = unitGrid.ValueRO.Cell, Faction = faction.ValueRO.Id, Motion = movement.ValueRO.UsesVehicleMotion, Enabled = consumption.ValueRO.Enabled, GroundRate = consumption.ValueRO.GroundFuelPerCell, AirRate = consumption.ValueRO.AirFuelPerCell, LastCell = consumptionState.ValueRO.LastCell, Initialized = consumptionState.ValueRO.Initialized, Air = continuousAirDisabled, HasTransform = continuousTransformDisabled, Position = continuousPositionDisabled, HasGrid = hasGrid, Grid = physicalGrid });
                }
#endif
                if (consumption.ValueRO.Enabled == 0)
                    continue;
                if(steelPush && (!state.EntityManager.HasComponent<CampaignMissionUnitRoleComponent>(entity)||
                    !state.EntityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity).SessionToken.Equals(steelRuntime.SessionToken)))
                    continue;

                ref UnitFuelConsumptionState stateRw = ref consumptionState.ValueRW;
                bool isAircraft = airMovementLookup.HasComponent(entity);
                // Runway approaches can fly beyond the ground grid. Its bounded
                // membership cell then stays unchanged; charge the aircraft's
                // actual travel without putting external cells into grid buffers.
#if UNITY_EDITOR
                bool continuousHasTransform = false;
                float3 continuousPosition = default;
                bool continuousUseTransform = isAircraft && hasGrid && physicalGrid.CellSize > 0 && (continuousHasTransform = transformLookup.HasComponent(entity));
                int2 cell = continuousUseTransform ? GridUtils.WorldToCell(physicalGrid, continuousPosition = transformLookup[entity].Position) : unitGrid.ValueRO.Cell;
                if (S002PhysicalStartupObservation.ContinuousFuel != null && S002PhysicalStartupObservation.ContinuousRead(20)) S002PhysicalStartupObservation.ContinuousSend(S002PhysicalStartupObservation.ContinuousFuel, new S002PhysicalStartupObservation.ContinuousFuelFacts { Manager = state.EntityManager, Entity = entity, Cell = unitGrid.ValueRO.Cell, Faction = faction.ValueRO.Id, Motion = movement.ValueRO.UsesVehicleMotion, Enabled = consumption.ValueRO.Enabled, GroundRate = consumption.ValueRO.GroundFuelPerCell, AirRate = consumption.ValueRO.AirFuelPerCell, LastCell = stateRw.LastCell, Initialized = stateRw.Initialized, Air = isAircraft, HasTransform = continuousHasTransform, Position = continuousPosition, HasGrid = hasGrid, Grid = physicalGrid });
#else
                int2 cell = isAircraft && hasGrid && physicalGrid.CellSize > 0 && transformLookup.HasComponent(entity)
                    ? GridUtils.WorldToCell(physicalGrid, transformLookup[entity].Position)
                    : unitGrid.ValueRO.Cell;
#endif
                if (stateRw.Initialized == 0)
                {
                    stateRw.LastCell = cell;
                    stateRw.Initialized = 1;
#if UNITY_EDITOR
                    S002PhysicalStartupObservation.ContinuousStatePost(state.EntityManager, entity, stateRw.LastCell, stateRw.Initialized);
#endif
                    continue;
                }

                int2 delta = cell - stateRw.LastCell;
                int movedCells = math.abs(delta.x) + math.abs(delta.y);
                if (movedCells <= 0)
                    continue;

                bool isFuelUsingUnit = movement.ValueRO.UsesVehicleMotion != 0 || isAircraft;
                if (!isFuelUsingUnit)
                {
                    stateRw.LastCell = cell;
#if UNITY_EDITOR
                    S002PhysicalStartupObservation.ContinuousStatePost(state.EntityManager, entity, stateRw.LastCell, stateRw.Initialized);
#endif
                    continue;
                }

                float fuelPerCell = isAircraft
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
#if UNITY_EDITOR
                S002PhysicalStartupObservation.ContinuousStatePost(state.EntityManager, entity, stateRw.LastCell, stateRw.Initialized);
#endif
            }

#if UNITY_EDITOR
            S002PhysicalStartupObservation.ForwardFuelRequested?.Invoke(requestedFuelByFaction);
#endif
#if UNITY_EDITOR
            S002PhysicalStartupObservation.ContinuousSend(S002PhysicalStartupObservation.ContinuousRequested, requestedFuelByFaction);
#endif
            if(steelPush)CampaignSteelPushFuelScope.Drain(state.EntityManager,steelReserve,requestedFuelByFaction[1]);
            else DrainRequestedFuel(ref state, requestedFuelByFaction);
            if (hasExpanded)
                DrainExpandedFuel(ref state, sessionEntity, requestedExpandedFuelByFaction);
#if UNITY_EDITOR
            S002PhysicalStartupObservation.ForwardFuelBoundary?.Invoke(state.EntityManager, "after-ground-fuel-consumer", steelPush, expanded.CalculateEntityCount(), hasExpanded);
#endif
#if UNITY_EDITOR
            S002PhysicalStartupObservation.ContinuousFuelStart(state.EntityManager, "after-ground-fuel-consumer", steelPush, continuousExpandedCount, hasExpanded, continuousGridCount, physicalGrid);
#endif
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

#if UNITY_EDITOR
                foreach (var (storageRef, storageEntity) in SystemAPI.Query<RefRW<BuildingResourceStorageComponent>>().WithEntityAccess())
#else
                foreach (RefRW<BuildingResourceStorageComponent> storageRef in SystemAPI.Query<RefRW<BuildingResourceStorageComponent>>())
#endif
                {
#if UNITY_EDITOR
                    S002PhysicalStartupObservation.ForwardFuelStorageVisit?.Invoke(state.EntityManager, storageEntity, factionId, remaining);
#endif
                    ref BuildingResourceStorageComponent storage = ref storageRef.ValueRW;
#if UNITY_EDITOR
                    S002PhysicalStartupObservation.ContinuousVisit(state.EntityManager, storageEntity, factionId, remaining);
#endif
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
#if UNITY_EDITOR
                    S002PhysicalStartupObservation.ContinuousPost(state.EntityManager, storageEntity, factionId, remaining, storage.StoredFuelBarrels, storage.Version);
#endif
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
