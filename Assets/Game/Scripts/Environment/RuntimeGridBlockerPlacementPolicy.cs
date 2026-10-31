using System;
using System.Collections.Generic;
using Game.Simulation;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    public sealed class RuntimeGridBlockerPrefabData
    {
        public string Name;
        public BlockerPrefabKind Kind;
        public bool BlocksOnlyCenterCell;
        public bool HasBounds;
        public float BoundsWidth;
        public float BoundsDepth;
    }

    public struct RuntimeGridBlockerPlacementData
    {
        public int PrefabIndex;
        public int2 OriginCell;
        public int2 SizeCells;
        public BlockerPrefabKind Kind;
    }

    public interface IRuntimeGridBlockerPlacementGrid
    {
        int Width { get; }
        int Height { get; }
        bool IsWalkable(int cellIndex);
        bool IsRoad(int cellIndex);
        bool IsBlocked(int cellIndex);
    }

    /// <summary>Pure data placement policy shared by the runtime owner and CLI world setup.</summary>
    public static class RuntimeGridBlockerPlacementPolicy
    {
        private struct PendingPlacement
        {
            public int PrefabIndex;
            public int2 OriginCell;
            public int2 SizeCells;
            public BlockerPrefabKind Kind;
        }

        public static List<RuntimeGridBlockerPlacementData> Plan(
            IRuntimeGridBlockerPlacementGrid grid,
            IReadOnlyList<RuntimeGridBlockerPrefabData> prefabs,
            int blockerCount,
            uint randomSeed,
            float cellSize,
            float treeSpawnRatio,
            int treeClusterCount,
            int treeClusterSpacingMinCells,
            int treeClusterSpacingMaxCells,
            int treeClusterDistanceMinCells,
            int treeClusterDistanceMaxCells)
        {
            if (grid == null || prefabs == null)
                throw new ArgumentNullException(grid == null ? nameof(grid) : nameof(prefabs));
            if (grid.Width < 1 || grid.Height < 1 || blockerCount < 0 || !(cellSize > 0f) || float.IsNaN(cellSize) || float.IsInfinity(cellSize))
                throw new ArgumentOutOfRangeException(nameof(blockerCount));
            int gridLength = checked(grid.Width * grid.Height);
            if (prefabs.Count == 0 || blockerCount == 0)
                return new List<RuntimeGridBlockerPlacementData>();

            treeSpawnRatio = math.clamp(treeSpawnRatio, 0f, 1f);
            treeClusterCount = math.max(1, treeClusterCount);
            treeClusterSpacingMinCells = math.max(1, treeClusterSpacingMinCells);
            treeClusterSpacingMaxCells = math.max(treeClusterSpacingMinCells, treeClusterSpacingMaxCells);
            treeClusterDistanceMinCells = math.max(0, treeClusterDistanceMinCells);
            treeClusterDistanceMaxCells = math.max(treeClusterDistanceMinCells, treeClusterDistanceMaxCells);
            if (randomSeed == 0)
                randomSeed = 1;

            var rng = new Unity.Mathematics.Random(randomSeed);
            var reserved = new NativeBitArray(gridLength, Allocator.Temp);
            var pending = new List<PendingPlacement>(blockerCount);
            var results = new List<RuntimeGridBlockerPlacementData>(blockerCount);
            List<int> treePrefabIndices = new();
            List<int> otherPrefabIndices = new();
            string previousPrefabName = null;
            for (int i = 0; i < prefabs.Count; i++)
            {
                RuntimeGridBlockerPrefabData prefab = prefabs[i];
                if (prefab == null || string.IsNullOrEmpty(prefab.Name))
                    throw new ArgumentException("Complete ordered blocker prefab metadata is required", nameof(prefabs));
                if (previousPrefabName != null && string.CompareOrdinal(previousPrefabName, prefab.Name) > 0)
                    throw new ArgumentException("Blocker prefab metadata must retain the original ordinal name order", nameof(prefabs));
                previousPrefabName = prefab.Name;
                if (prefab.Kind == BlockerPrefabKind.Tree)
                    treePrefabIndices.Add(i);
                else
                    otherPrefabIndices.Add(i);
            }

            try
            {
            List<int2> treeClusterCenters = BuildTreeClusterCenters(ref rng, grid.Width, grid.Height, treeClusterCount);
            int maxAttempts = math.max(blockerCount * 30, 200);
            int attempts = 0;
            int spawnedTreeCount = 0;
            int targetTreeCount = math.clamp((int)math.round(blockerCount * treeSpawnRatio), 0, blockerCount);
            int targetTreesPerCluster = math.max(1, (int)math.ceil(targetTreeCount / (float)math.max(1, treeClusterCount)));
            int averageTreeSpacing = (int)math.round((treeClusterSpacingMinCells + treeClusterSpacingMaxCells) * 0.5f);
            int treeClusterRadius = ComputeTreeClusterRadius(targetTreesPerCluster, averageTreeSpacing);

            while (results.Count < blockerCount && attempts < maxAttempts)
            {
                attempts++;
                bool preferTrees = spawnedTreeCount < targetTreeCount;
                List<int> source = preferTrees
                    ? (treePrefabIndices.Count > 0 ? treePrefabIndices : otherPrefabIndices)
                    : (otherPrefabIndices.Count > 0 ? otherPrefabIndices : treePrefabIndices);
                if (source.Count == 0)
                    continue;

                int prefabIndex = source[rng.NextInt(0, source.Count)];
                RuntimeGridBlockerPrefabData metadata = prefabs[prefabIndex];
                int footprintWidth;
                int footprintDepth;
                RuntimeGridBlockerPlacementKernel.ComputeFootprintCells(metadata.BlocksOnlyCenterCell,
                    metadata.HasBounds, metadata.BoundsWidth, metadata.BoundsDepth, cellSize,
                    out footprintWidth, out footprintDepth);
                int2 sizeCells = new(footprintWidth, footprintDepth);
                if (sizeCells.x > grid.Width || sizeCells.y > grid.Height)
                    continue;

                if (!TryFindBlockerOrigin(ref rng, grid, reserved, pending, treeClusterCenters,
                        treeClusterRadius, treeClusterDistanceMinCells, treeClusterDistanceMaxCells,
                        treeClusterSpacingMinCells, treeClusterSpacingMaxCells, metadata, sizeCells,
                        out int2 originCell))
                    continue;

                if (!CanPlaceFootprint(grid, reserved, originCell.x, originCell.y, sizeCells))
                    continue;

                MarkReserved(grid.Width, reserved, originCell, sizeCells);
                pending.Add(new PendingPlacement { PrefabIndex = prefabIndex, OriginCell = originCell,
                    SizeCells = sizeCells, Kind = metadata.Kind });
                results.Add(new RuntimeGridBlockerPlacementData { PrefabIndex = prefabIndex,
                    OriginCell = originCell, SizeCells = sizeCells, Kind = metadata.Kind });
                if (metadata.Kind == BlockerPrefabKind.Tree)
                    spawnedTreeCount++;
            }

            return results;
            }
            finally
            {
                reserved.Dispose();
            }
        }

        private static bool TryFindBlockerOrigin(
            ref Unity.Mathematics.Random rng,
            IRuntimeGridBlockerPlacementGrid grid,
            NativeBitArray reserved,
            List<PendingPlacement> pending,
            List<int2> treeClusterCenters,
            int treeClusterRadius,
            int treeClusterDistanceMinCells,
            int treeClusterDistanceMaxCells,
            int treeClusterSpacingMinCells,
            int treeClusterSpacingMaxCells,
            RuntimeGridBlockerPrefabData metadata,
            int2 sizeCells,
            out int2 originCell)
        {
            int maxX = grid.Width - sizeCells.x;
            int maxY = grid.Height - sizeCells.y;
            originCell = default;
            if (maxX < 0 || maxY < 0)
                return false;

            for (int i = 0; i < 10; i++)
            {
                if (!TryGetBlockerAnchor(ref rng, pending, treeClusterCenters, metadata.Kind, out int2 anchor))
                    break;

                int radius = metadata.Kind == BlockerPrefabKind.Tree ? treeClusterRadius : GetClusterRadius(metadata.Kind);
                int localSpacing = metadata.Kind == BlockerPrefabKind.Tree
                    ? SampleSpacing(ref rng, treeClusterSpacingMinCells, treeClusterSpacingMaxCells)
                    : math.max(1, (int)math.round((treeClusterSpacingMinCells + treeClusterSpacingMaxCells) * 0.5f));
                int2 candidate = metadata.Kind == BlockerPrefabKind.Tree
                    ? SampleClusterCell(ref rng, anchor, maxX, maxY, radius, treeClusterDistanceMinCells, treeClusterDistanceMaxCells)
                    : new int2(math.clamp(anchor.x + rng.NextInt(-radius, radius + 1), 0, maxX),
                               math.clamp(anchor.y + rng.NextInt(-radius, radius + 1), 0, maxY));

                if (!CanPlaceFootprint(grid, reserved, candidate.x, candidate.y, sizeCells) ||
                    !MatchesLocalPlacementHeuristics(candidate, pending, metadata.Kind, localSpacing))
                    continue;
                originCell = candidate;
                return true;
            }

            for (int i = 0; i < 20; i++)
            {
                if (metadata.Kind == BlockerPrefabKind.Tree)
                    return false;
                int2 candidate = new(rng.NextInt(0, maxX + 1), rng.NextInt(0, maxY + 1));
                int localSpacing = math.max(1, (int)math.round((treeClusterSpacingMinCells + treeClusterSpacingMaxCells) * 0.5f));
                if (!CanPlaceFootprint(grid, reserved, candidate.x, candidate.y, sizeCells) ||
                    !MatchesLocalPlacementHeuristics(candidate, pending, metadata.Kind, localSpacing))
                    continue;
                originCell = candidate;
                return true;
            }
            return false;
        }

        private static bool TryGetBlockerAnchor(ref Unity.Mathematics.Random rng, List<PendingPlacement> pending,
            List<int2> treeClusterCenters, BlockerPrefabKind kind, out int2 anchor)
        {
            anchor = default;
            if (kind == BlockerPrefabKind.Tree && treeClusterCenters != null && treeClusterCenters.Count > 0)
            {
                anchor = treeClusterCenters[rng.NextInt(0, treeClusterCenters.Count)];
                return true;
            }
            if (pending.Count == 0)
                return false;

            bool preferCluster = kind == BlockerPrefabKind.Tree || kind == BlockerPrefabKind.Bush || kind == BlockerPrefabKind.Grass;
            bool preferRocksForBushes = kind == BlockerPrefabKind.Bush && rng.NextFloat() < 0.65f;
            var matchingIndices = new List<int>();
            for (int i = 0; i < pending.Count; i++)
            {
                BlockerPrefabKind existingKind = pending[i].Kind;
                if (preferRocksForBushes)
                {
                    if (existingKind == BlockerPrefabKind.Rock) matchingIndices.Add(i);
                    continue;
                }
                if (existingKind == kind) matchingIndices.Add(i);
            }
            if (matchingIndices.Count == 0 && preferCluster)
            {
                for (int i = 0; i < pending.Count; i++)
                {
                    BlockerPrefabKind existingKind = pending[i].Kind;
                    if (kind == BlockerPrefabKind.Grass && existingKind == BlockerPrefabKind.Bush ||
                        kind == BlockerPrefabKind.Bush && existingKind == BlockerPrefabKind.Tree)
                        matchingIndices.Add(i);
                }
            }
            if (matchingIndices.Count == 0)
                return false;
            anchor = pending[matchingIndices[rng.NextInt(0, matchingIndices.Count)]].OriginCell;
            return true;
        }

        private static bool CanPlaceFootprint(IRuntimeGridBlockerPlacementGrid grid, NativeBitArray reserved,
            int startX, int startY, int2 sizeCells)
        {
            for (int y = startY; y < startY + sizeCells.y; y++)
                for (int x = startX; x < startX + sizeCells.x; x++)
                {
                    int index = x + y * grid.Width;
                    if (!grid.IsWalkable(index) || grid.IsRoad(index) || reserved.IsSet(index) || grid.IsBlocked(index))
                        return false;
                }
            return true;
        }

        private static void MarkReserved(int gridWidth, NativeBitArray reserved, int2 origin, int2 size)
        {
            for (int y = origin.y; y < origin.y + size.y; y++)
                for (int x = origin.x; x < origin.x + size.x; x++)
                    reserved.Set(x + y * gridWidth, true);
        }

        private static bool MatchesLocalPlacementHeuristics(int2 candidate, List<PendingPlacement> pending,
            BlockerPrefabKind kind, int minimumSameKindSpacing)
        {
            if (pending.Count == 0)
                return true;
            int sameKindNearby = 0, veryCloseSameKind = 0, rocksNearby = 0, plantsNearby = 0;
            int nearestDistanceSq = int.MaxValue;
            int spacingSq = minimumSameKindSpacing * minimumSameKindSpacing;
            for (int i = 0; i < pending.Count; i++)
            {
                int2 existing = pending[i].OriginCell;
                int dx = existing.x - candidate.x, dy = existing.y - candidate.y;
                int distanceSq = dx * dx + dy * dy;
                nearestDistanceSq = math.min(nearestDistanceSq, distanceSq);
                if (distanceSq <= spacingSq && pending[i].Kind == kind) veryCloseSameKind++;
                if (distanceSq > 64) continue;
                BlockerPrefabKind existingKind = pending[i].Kind;
                if (existingKind == kind) sameKindNearby++;
                if (existingKind == BlockerPrefabKind.Rock) rocksNearby++;
                if (existingKind == BlockerPrefabKind.Tree || existingKind == BlockerPrefabKind.Bush || existingKind == BlockerPrefabKind.Grass) plantsNearby++;
            }
            return kind switch
            {
                BlockerPrefabKind.Tree => veryCloseSameKind == 0,
                BlockerPrefabKind.Bush => rocksNearby >= 1 || sameKindNearby >= 1 || plantsNearby >= 2 || nearestDistanceSq > 100,
                BlockerPrefabKind.Grass => plantsNearby >= 1 || sameKindNearby >= 1 || nearestDistanceSq > 121,
                BlockerPrefabKind.Rock => sameKindNearby >= 1 || nearestDistanceSq > 49,
                _ => true
            };
        }

        private static List<int2> BuildTreeClusterCenters(ref Unity.Mathematics.Random rng, int width, int height, int clusterCount)
        {
            int count = math.max(1, clusterCount);
            var centers = new List<int2>(count);
            int minDistance = math.max(8, math.min(width, height) / 6);
            int minDistanceSq = minDistance * minDistance;
            int maxAttempts = count * 12;
            for (int attempt = 0; attempt < maxAttempts && centers.Count < count; attempt++)
            {
                int2 candidate = new(rng.NextInt(0, width), rng.NextInt(0, height));
                bool farEnough = true;
                for (int i = 0; i < centers.Count; i++)
                {
                    int dx = centers[i].x - candidate.x, dy = centers[i].y - candidate.y;
                    if (dx * dx + dy * dy < minDistanceSq) { farEnough = false; break; }
                }
                if (farEnough) centers.Add(candidate);
            }
            if (centers.Count == 0) centers.Add(new int2(width / 2, height / 2));
            return centers;
        }

        private static int ComputeTreeClusterRadius(int targetTreesPerCluster, int treeMinSpacingCells)
        {
            float spacing = math.max(1f, treeMinSpacingCells);
            float estimatedSide = math.sqrt(math.max(1, targetTreesPerCluster)) * spacing;
            return math.max(10, (int)math.ceil(estimatedSide * 0.6f));
        }

        private static int2 SampleClusterCell(ref Unity.Mathematics.Random rng, int2 anchor, int maxX, int maxY,
            int radius, int distanceMinCells, int distanceMaxCells)
        {
            int maxDistance = math.max(0, math.min(radius, distanceMaxCells));
            int minDistance = math.clamp(distanceMinCells, 0, maxDistance);
            float angle = rng.NextFloat(0f, math.PI * 2f);
            float distance = maxDistance <= 0 ? 0f : rng.NextFloat(minDistance, maxDistance + 1f);
            int x = math.clamp(anchor.x + (int)math.round(math.cos(angle) * distance), 0, maxX);
            int y = math.clamp(anchor.y + (int)math.round(math.sin(angle) * distance), 0, maxY);
            return new int2(x, y);
        }

        private static int SampleSpacing(ref Unity.Mathematics.Random rng, int minCells, int maxCells)
        {
            int min = math.max(1, minCells);
            int max = math.max(min, maxCells);
            return rng.NextInt(min, max + 1);
        }

        private static int GetClusterRadius(BlockerPrefabKind kind) => kind switch
        {
            BlockerPrefabKind.Tree => 10,
            BlockerPrefabKind.Rock => 5,
            BlockerPrefabKind.Bush => 4,
            BlockerPrefabKind.Grass => 5,
            _ => 6
        };
    }
}
