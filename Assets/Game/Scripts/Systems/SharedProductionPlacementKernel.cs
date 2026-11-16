using Game.Components;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;using UnityEngine;namespace Game.Runtime { public static class SharedProductionPlacementKernel {
        public static bool TryFindStrictSpawnCell(
            System.Func<int2,int2,bool> overlapsRecent,
            System.Func<int2,int2,bool> overlapsExisting,
            ref Unity.Mathematics.Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 center,
            int radiusCells,
            int2 footprintSize,
            out int2 result)
        {
            result = default;
            const int randomTries = 192;
            for (int i = 0; i < randomTries; i++)
            {
                int2 candidate = new(
                    center.x + rng.NextInt(-radiusCells, radiusCells + 1),
                    center.y + rng.NextInt(-radiusCells, radiusCells + 1));

                if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, footprintSize))
                    continue;
                if (overlapsRecent(candidate, footprintSize))
                    continue;
                if (overlapsExisting(candidate, footprintSize))
                    continue;

                result = candidate;
                return true;
            }

            int maxRadius = math.max(8, radiusCells + 32);
            for (int r = 0; r <= maxRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (math.abs(dx) != r && math.abs(dy) != r)
                            continue;

                        int2 candidate = new(center.x + dx, center.y + dy);
                        if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, footprintSize))
                            continue;
                        if (overlapsRecent(candidate, footprintSize))
                            continue;
                        if (overlapsExisting(candidate, footprintSize))
                            continue;

                        result = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryFindStrictSpawnCellAdjacentToBuilding(
            System.Func<int2,int2,bool> overlapsRecent,
            System.Func<int2,int2,bool> overlapsExisting,
            ref Unity.Mathematics.Random rng,
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            Vector2Int originCell,
            Vector2Int footprintCells,
            int2 unitFootprint,
            out int2 result)
        {
            result = default;
            int maxExtraRadius = math.max(6, math.max(unitFootprint.x, unitFootprint.y) + 2);
            for (int extraRadius = 1; extraRadius <= maxExtraRadius; extraRadius++)
            {
                var candidates = new NativeList<int2>(Allocator.Temp);
                try
                {
                    int minX = originCell.x - extraRadius;
                    int minY = originCell.y - extraRadius;
                    int maxX = originCell.x + footprintCells.x - 1 + extraRadius;
                    int maxY = originCell.y + footprintCells.y - 1 + extraRadius;

                    for (int x = minX; x <= maxX; x++)
                    {
                        candidates.Add(new int2(x, minY));
                        if (maxY != minY)
                            candidates.Add(new int2(x, maxY));
                    }

                    for (int y = minY + 1; y < maxY; y++)
                    {
                        candidates.Add(new int2(minX, y));
                        if (maxX != minX)
                            candidates.Add(new int2(maxX, y));
                    }

                    if (candidates.Length == 0)
                        continue;

                    int startIndex = rng.NextInt(candidates.Length);
                    for (int offset = 0; offset < candidates.Length; offset++)
                    {
                        int2 candidate = candidates[(startIndex + offset) % candidates.Length];
                        if (!TryReserveSpawnCandidate(grid, walkable, blocked, occupied, ref reserved, candidate, unitFootprint))
                            continue;
                        if (overlapsRecent(candidate, unitFootprint))
                            continue;
                        if (overlapsExisting(candidate, unitFootprint))
                            continue;

                        result = candidate;
                        return true;
                    }
                }
                finally
                {
                    if (candidates.IsCreated)
                        candidates.Dispose();
                }
            }

            return false;
        }

        public static bool TryReserveSpawnCandidate(
            in GridConfig grid,
            in NativeArray<GridWalkable> walkable,
            in NativeBitArray blocked,
            in NativeBitArray occupied,
            ref NativeBitArray reserved,
            int2 cell,
            int2 footprintSize)
        {
            int2 size = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(cell, size);
            int2 max = min + size;
            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    int idx = row + x;
                    if (walkable[idx].Value == 0)
                        return false;
                    if (blocked.IsSet(idx) || occupied.IsSet(idx) || reserved.IsSet(idx))
                        return false;
                }
            }

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                    reserved.Set(row + x, true);
            }

            return true;
        }


}}
