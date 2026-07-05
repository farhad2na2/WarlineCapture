using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAirMovementSystem))]
    [UpdateAfter(typeof(UnitGridMovementSystem))]
    public partial struct VehicleFuelConsumptionSystem : ISystem
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
            NativeArray<float> requestedFuelByFaction = new(FactionCapacity, Allocator.Temp);
            ComponentLookup<UnitAirMovement> airMovementLookup = SystemAPI.GetComponentLookup<UnitAirMovement>(true);

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
                    requestedFuelByFaction[faction.ValueRO.Id] += requestedFuel;

                stateRw.LastCell = cell;
            }

            DrainRequestedFuel(ref state, requestedFuelByFaction);
            requestedFuelByFaction.Dispose();
        }

        private static void DrainRequestedFuel(ref SystemState state, NativeArray<float> requestedFuelByFaction)
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

                    float availableFuel = math.max(0f, storage.StoredFuelBarrels - storage.ReservedFuelOutboundBarrels);
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
