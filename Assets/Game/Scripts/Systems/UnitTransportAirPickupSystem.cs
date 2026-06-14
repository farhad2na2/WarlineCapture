using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public readonly struct UnitTransportAirPickupSystem
{
    private const float AirBoardingGroundedHeightTolerance = TransportBoardingData.AirBoardingGroundedHeightTolerance;

    public bool TryPrepareAirTransportPickupForBoarding(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        List<Entity> selectedPassengers,
        int selectedCount,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        UnitMoveOrderSystem moveOrderSystem,
        out int2 pickupCell)
    {
        if (!TryFindAirTransportPickupForBoarding(
                em,
                transport,
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                selectedPassengers,
                selectedCount,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                out pickupCell))
        {
            return false;
        }

        CommandAirTransportPickup(em, transport, grid, pickupCell, moveOrderSystem);
        return true;
    }

    public bool TryFindAirTransportPickupForBoarding(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        int2 transportCell,
        int2 transportSize,
        List<Entity> selectedPassengers,
        int selectedCount,
        in NativeArray<Entity> liveUnitEntities,
        in NativeArray<UnitGrid> liveUnitGrids,
        in NativeArray<UnitFootprint> liveUnitFootprints,
        out int2 pickupCell)
    {
        pickupCell = default;
        if (!em.Exists(transport) ||
            !em.HasComponent<UnitAirMovement>(transport) ||
            !em.HasComponent<UnitAirComponent>(transport) ||
            !em.HasComponent<LocalTransform>(transport))
        {
            return false;
        }

        var querySystem = new UnitTransportBoardingQuerySystem();
        var approachCellSystem = new UnitTransportApproachCellSystem();
        byte factionId = em.HasComponent<Faction>(transport) ? em.GetComponentData<Faction>(transport).Id : (byte)0;
        int count = math.min(selectedCount, selectedPassengers.Count);
        for (int i = 0; i < count; i++)
        {
            Entity passenger = selectedPassengers[i];
            if (!querySystem.IsSoldierBoardingCandidate(em, passenger) || !em.HasComponent<UnitGrid>(passenger))
                continue;

            int2 passengerCell = em.GetComponentData<UnitGrid>(passenger).Cell;
            if (!approachCellSystem.TryFindAirTransportPickupCellNearPassenger(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    transportSize,
                    passengerCell,
                    transport,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    factionId,
                    out pickupCell))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    public void CommandAirTransportPickup(
        EntityManager em,
        Entity transport,
        in GridConfig grid,
        int2 pickupCell,
        UnitMoveOrderSystem moveOrderSystem)
    {
        UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(em, transport);

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : grid.Origin.y;
        float3 pickupPosition = GridUtils.CellToWorldCenter(grid, pickupCell);
        pickupPosition.y = groundY;

        airState.HomePosition = pickupPosition;
        airState.HomeCell = pickupCell;
        airState.HomeInitialized = 1;
        airState.ReturningHome = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        if (transform.Position.y > groundY + AirBoardingGroundedHeightTolerance)
            airState.Airborne = 1;
        em.SetComponentData(transport, airState);

        UnitMoveOrderRequestSystem.EnqueueAndProcessTargetOnlyMoveOrder(em, transport, pickupCell);
    }
}
