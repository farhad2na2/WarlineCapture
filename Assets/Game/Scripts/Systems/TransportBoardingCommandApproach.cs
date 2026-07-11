using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Configs;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public partial struct TransportBoardingCommandSystem
    {
        public static bool TryFindAirTransportPickupCellNearPassenger(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 passengerCell,
            Entity transport,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            byte factionId,
            out int2 pickupCell)
        {
            return TransportBoardingApproachSystemHelper.TryFindAirTransportPickupCellNearPassenger(
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
                out pickupCell);
        }

        private static bool TryFindTransportBoardingGoal(
            EntityManager em,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            int2 passengerFootprint,
            Entity passenger,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            HashSet<int> reservedCells,
            int directBoardingCells,
            byte factionId,
            HashSet<Entity> ignoredLiveEntities,
            HashSet<int> ignoredOccupiedCells,
            out int2 goal)
        {
            if (IsCargoPlaneTransport(em, ignoredOccupancyEntity))
            {
                if (TryFindPlaneRampApproachCell(
                    em,
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    ignoredOccupancySize,
                    referenceCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    reservedCells,
                    factionId,
                    ignoredLiveEntities,
                    ignoredOccupiedCells,
                    out goal))
                {
                    return true;
                }

                int fallbackDirectBoardingCells = math.max(directBoardingCells, TransportBoardingData.BoardingClearanceCells);
                return TryFindTransportApproachCell(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    transportCell,
                    ignoredOccupancySize,
                    referenceCell,
                    passengerFootprint,
                    passenger,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    reservedCells,
                    fallbackDirectBoardingCells,
                    factionId,
                    ignoredLiveEntities,
                    ignoredOccupiedCells,
                    out goal);
            }

            return TryFindTransportApproachCell(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                referenceCell,
                passengerFootprint,
                passenger,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize,
                reservedCells,
                directBoardingCells,
                factionId,
                ignoredLiveEntities,
                ignoredOccupiedCells,
                out goal);
        }

        private static bool TryFindPlaneRampApproachCell(
            EntityManager em,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            int2 passengerFootprint,
            Entity passenger,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            Entity transport,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            HashSet<int> reservedCells,
            byte factionId,
            HashSet<Entity> ignoredLiveEntities,
            HashSet<int> ignoredOccupiedCells,
            out int2 goal)
        {
            goal = default;
            if (!em.Exists(transport) ||
                !em.HasComponent<UnitTransportPlaneDoorReference>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                return false;
            }

            if (!TryResolvePlaneRampApproachCell(em, grid, transport, out int2 rampCell))
                return false;
            int2 clampedFootprint = UnitFootprintUtility.ClampSize(passengerFootprint);
            int maxRadius = CalculatePlaneRampSearchRadius(clampedFootprint);
            int bestScore = int.MaxValue;
            bool found = false;

            for (int radius = 0; radius <= maxRadius; radius++)
            {
                int minX = rampCell.x - radius;
                int minY = rampCell.y - radius;
                int maxX = rampCell.x + radius;
                int maxY = rampCell.y + radius;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!IsPlaneRampSearchRingCandidate(radius, x, y, minX, minY, maxX, maxY))
                            continue;

                        int2 candidate = new int2(x, y);
                        if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                            continue;

                        if (UnitFootprintUtility.Overlaps(candidate, clampedFootprint, transportCell, transportSize))
                            continue;

                        if (!TransportBoardingApproachSystemHelper.IsTransportApproachPassable(
                                grid,
                                walkable,
                                blocked,
                                friendlyPassFactionIds,
                                occupied,
                                candidate,
                                clampedFootprint,
                                referenceCell,
                                passenger,
                                liveUnitEntities,
                                liveUnitGrids,
                                liveUnitFootprints,
                                transport,
                                ignoredOccupancyCell,
                                ignoredOccupancySize,
                                reservedCells,
                                referenceCell,
                                factionId,
                                candidate.Equals(referenceCell),
                                ignoredLiveEntities,
                                ignoredOccupiedCells))
                        {
                            continue;
                        }

                        int score = ScorePlaneRampApproachCandidate(candidate, rampCell, referenceCell);
                        if (score >= bestScore)
                            continue;

                        bestScore = score;
                        goal = candidate;
                        found = true;
                    }
                }

                if (found)
                    return true;
            }

            return false;
        }

        private static int CalculatePlaneRampSearchRadius(int2 clampedFootprint)
        {
            return math.max(
                PlaneRampSearchMinRadius,
                math.max(clampedFootprint.x, clampedFootprint.y) + PlaneRampSearchFootprintPadding);
        }

        private static bool IsPlaneRampSearchRingCandidate(
            int radius,
            int x,
            int y,
            int minX,
            int minY,
            int maxX,
            int maxY)
        {
            return radius == 0 || x == minX || x == maxX || y == minY || y == maxY;
        }

        private static int ScorePlaneRampApproachCandidate(int2 candidate, int2 rampCell, int2 referenceCell)
        {
            int2 rampDelta = candidate - rampCell;
            int2 passengerDelta = candidate - referenceCell;
            return (math.abs(rampDelta.x) + math.abs(rampDelta.y)) * 100 +
                   math.abs(passengerDelta.x) + math.abs(passengerDelta.y);
        }

        internal static int2 ResolvePlaneRampApproachCell(EntityManager em, in GridConfig grid, Entity transport)
        {
            if (TryResolvePlaneRampApproachCell(em, grid, transport, out int2 rampCell))
                return rampCell;

            if (em.Exists(transport) && em.HasComponent<LocalTransform>(transport))
                return GridUtils.WorldToCell(grid, em.GetComponentData<LocalTransform>(transport).Position);

            return em.Exists(transport) && em.HasComponent<UnitGrid>(transport)
                ? em.GetComponentData<UnitGrid>(transport).Cell
                : default;
        }

        internal static bool TryResolvePlaneRampApproachCell(
            EntityManager em,
            in GridConfig grid,
            Entity transport,
            out int2 rampCell)
        {
            rampCell = default;
            if (!em.Exists(transport) ||
                !em.HasComponent<LocalTransform>(transport) ||
                !em.HasComponent<UnitTransportPlaneDoorReference>(transport))
            {
                return false;
            }

            LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
            UnitTransportPlaneDoorReference reference = em.GetComponentData<UnitTransportPlaneDoorReference>(transport);
            float3 localApproach = reference.ApproachLocalPosition * transform.Scale;
            float3 worldApproach = transform.Position + math.mul(transform.Rotation, localApproach);
            rampCell = GridUtils.WorldToCell(grid, worldApproach);
            return true;
        }

        public static bool TryFindTransportApproachCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            int2 passengerFootprint,
            Entity passenger,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            HashSet<int> reservedCells,
            int directBoardingCells,
            byte factionId,
            HashSet<Entity> ignoredLiveEntities,
            HashSet<int> ignoredOccupiedCells,
            out int2 goal)
        {
            return TransportBoardingApproachSystemHelper.TryFindTransportApproachCell(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                transportCell,
                transportSize,
                referenceCell,
                passengerFootprint,
                passenger,
                liveUnitEntities,
                liveUnitGrids,
                liveUnitFootprints,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize,
                reservedCells,
                directBoardingCells,
                factionId,
                ignoredLiveEntities,
                ignoredOccupiedCells,
                out goal);
        }

        public static void ReserveFootprintCells(GridConfig grid, int2 cell, int2 footprintSize, HashSet<int> reservedCells)
        {
            TransportBoardingApproachSystemHelper.ReserveFootprintCells(grid, cell, footprintSize, reservedCells);
        }

        public static bool TryFindTransportDisembarkCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            HashSet<int> reservedCells,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            out int2 goal)
        {
            return TransportBoardingApproachSystemHelper.TryFindTransportDisembarkCell(
                grid,
                walkable,
                blocked,
                occupied,
                reservedCells,
                transportCell,
                transportSize,
                referenceCell,
                out goal);
        }


    }
}
