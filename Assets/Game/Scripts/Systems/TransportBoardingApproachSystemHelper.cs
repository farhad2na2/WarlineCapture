using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal static class TransportBoardingApproachSystemHelper
    {
        private const int TransportRingSearchMinRadius = 8;
        private const int TransportRingSearchFootprintPadding = 6;

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
            pickupCell = default;
            for (int radius = 2; radius <= 10; radius++)
            {
                int bestScore = int.MaxValue;
                bool found = false;
                int minX = passengerCell.x - radius;
                int minY = passengerCell.y - radius;
                int maxX = passengerCell.x + radius;
                int maxY = passengerCell.y + radius;
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (x != minX && x != maxX && y != minY && y != maxY)
                            continue;

                        int2 candidate = new int2(x, y);
                        if (!IsTransportApproachPassable(
                                grid,
                                walkable,
                                blocked,
                                friendlyPassFactionIds,
                                occupied,
                                candidate,
                                transportSize,
                                transportCell,
                                transport,
                                liveUnitEntities,
                                liveUnitGrids,
                                liveUnitFootprints,
                                Entity.Null,
                                default,
                                default,
                                null,
                                candidate,
                                factionId,
                                false))
                        {
                            continue;
                        }

                        int2 delta = candidate - passengerCell;
                        int score = math.abs(delta.x) + math.abs(delta.y);
                        if (score >= bestScore)
                            continue;

                        bestScore = score;
                        pickupCell = candidate;
                        found = true;
                    }
                }

                if (found)
                    return true;
            }

            return false;
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
            return TryFindNearbyTransportApproachCell(
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
            if (reservedCells == null)
                return;

            int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
            int2 max = min + clamped;
            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                {
                    int2 reservedCell = new int2(x, y);
                    if (GridUtils.InBounds(reservedCell, grid.Width, grid.Height))
                        reservedCells.Add(GridUtils.CellToIndex(reservedCell, grid.Width));
                }
            }
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
            return TryFindTransportRingCell(
                grid,
                walkable,
                blocked,
                occupied,
                reservedCells,
                transportCell,
                transportSize,
                referenceCell,
                TransportBoardingData.BoardingClearanceCells,
                false,
                out goal);
        }


        private static bool TryFindNearbyTransportApproachCell(
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
            goal = default;
            if (!GridUtils.InBounds(referenceCell, grid.Width, grid.Height))
                return false;

            int gridSize = grid.Width * grid.Height;
            if (gridSize <= 0 || walkable.Length < gridSize)
                return false;

            int2 size = UnitFootprintUtility.ClampSize(transportSize);
            int2 min = UnitFootprintUtility.GetMinCell(transportCell, size);
            int2 max = min + size;
            if (directBoardingCells > TransportBoardingData.BoardingClearanceCells &&
                UnitFootprintUtility.ContainsCellWithPadding(transportCell, size, referenceCell, directBoardingCells))
            {
                goal = referenceCell;
                return true;
            }

            int maxRadius = math.max(1, directBoardingCells);
            int bestScore = int.MaxValue;
            bool found = false;
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                int minX = min.x - radius;
                int minY = min.y - radius;
                int maxX = max.x - 1 + radius;
                int maxY = max.y - 1 + radius;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        bool onRing = x == minX || x == maxX || y == minY || y == maxY;
                        if (!onRing)
                            continue;

                        int2 candidate = new int2(x, y);
                        if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                            continue;

                        if (!IsTransportApproachPassable(
                                grid,
                                walkable,
                                blocked,
                                friendlyPassFactionIds,
                                occupied,
                                candidate,
                                passengerFootprint,
                                referenceCell,
                                passenger,
                                liveUnitEntities,
                                liveUnitGrids,
                                liveUnitFootprints,
                                ignoredOccupancyEntity,
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

                        int2 delta = candidate - referenceCell;
                        int score = math.abs(delta.x) + math.abs(delta.y);
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


        public static bool IsTransportApproachPassable(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeArray<byte> friendlyPassFactionIds,
            in NativeBitArray occupied,
            int2 cell,
            int2 footprintSize,
            int2 currentCell,
            Entity movingEntity,
            in NativeArray<Entity> liveUnitEntities,
            in NativeArray<UnitGrid> liveUnitGrids,
            in NativeArray<UnitFootprint> liveUnitFootprints,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            HashSet<int> reservedCells,
            int2 referenceCell,
            byte factionId,
            bool allowReferenceCellOccupied,
            HashSet<Entity> ignoredLiveEntities = null,
            HashSet<int> ignoredOccupiedCells = null)
        {
            int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
            int2 max = min + clamped;
            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    int index = row + x;
                    if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
                        return false;
                    if (reservedCells != null && reservedCells.Contains(index))
                        return false;

                    if (blocked.IsCreated && blocked.IsSet(index) &&
                        (!friendlyPassFactionIds.IsCreated || (uint)index >= (uint)friendlyPassFactionIds.Length || friendlyPassFactionIds[index] != factionId))
                    {
                        return false;
                    }

                    bool isReferenceCell = x == referenceCell.x && y == referenceCell.y;
                    bool isCurrentFootprintCell = UnitFootprintUtility.ContainsCell(currentCell, clamped, new int2(x, y));
                    bool isIgnoredOccupancyCell =
                        ignoredOccupancyEntity != Entity.Null &&
                        UnitFootprintUtility.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new int2(x, y));
                    bool isIgnoredSelectedOccupiedCell = ignoredOccupiedCells != null && ignoredOccupiedCells.Contains(index);
                    if (!isCurrentFootprintCell &&
                        occupied.IsCreated &&
                        occupied.IsSet(index) &&
                        (!allowReferenceCellOccupied || !isReferenceCell) &&
                        !isIgnoredOccupancyCell &&
                        !isIgnoredSelectedOccupiedCell)
                    {
                        return false;
                    }
                }
            }

            for (int i = 0; i < liveUnitEntities.Length; i++)
            {
                Entity other = liveUnitEntities[i];
                if (other == movingEntity ||
                    other == ignoredOccupancyEntity ||
                    (ignoredLiveEntities != null && ignoredLiveEntities.Contains(other)))
                {
                    continue;
                }

                int2 otherCell = liveUnitGrids[i].Cell;
                int2 otherSize = liveUnitFootprints[i].Size;
                if (UnitFootprintUtility.Overlaps(cell, clamped, otherCell, otherSize) &&
                    !UnitFootprintUtility.Overlaps(currentCell, clamped, otherCell, otherSize))
                {
                    return false;
                }
            }

            return true;
        }


        private static bool TryFindTransportRingCell(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            HashSet<int> reservedCells,
            int2 transportCell,
            int2 transportSize,
            int2 referenceCell,
            int minRadius,
            bool allowReferenceCellOccupied,
            out int2 goal)
        {
            goal = default;
            int2 size = UnitFootprintUtility.ClampSize(transportSize);
            int2 min = UnitFootprintUtility.GetMinCell(transportCell, size);
            int2 max = min + size;
            int bestScore = int.MaxValue;
            bool found = false;
            int startRadius = math.max(1, minRadius);
            int maxRadius = math.max(
                TransportRingSearchMinRadius,
                math.max(size.x, size.y) + TransportRingSearchFootprintPadding);

            for (int radius = startRadius; radius <= maxRadius; radius++)
            {
                int minX = min.x - radius;
                int minY = min.y - radius;
                int maxX = max.x - 1 + radius;
                int maxY = max.y - 1 + radius;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        bool onRing = x == minX || x == maxX || y == minY || y == maxY;
                        if (!onRing)
                            continue;

                        int2 candidate = new int2(x, y);
                        if (!GridUtils.InBounds(candidate, grid.Width, grid.Height))
                            continue;

                        int index = GridUtils.CellToIndex(candidate, grid.Width);
                        if (reservedCells != null && reservedCells.Contains(index))
                            continue;
                        if (walkable[index].Value == 0)
                            continue;
                        if (blocked.IsCreated && blocked.IsSet(index))
                            continue;

                        bool isReferenceCell = candidate.Equals(referenceCell);
                        if (occupied.IsCreated && occupied.IsSet(index) && (!allowReferenceCellOccupied || !isReferenceCell))
                            continue;

                        int2 delta = candidate - referenceCell;
                        int score = math.abs(delta.x) + math.abs(delta.y);
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


    }
}
