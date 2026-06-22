using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class RuntimeDecorationSpawnerSystem
{
    private enum DecorationPrefabKind
    {
        Other,
        Tree,
        Rock,
        Bush,
        Grass
    }

    private struct DecorationSeed
    {
        public int2 Cell;
        public DecorationPrefabKind Kind;
    }

    private bool _spawnOnStart = true;
    private int _decorationCount = 150;
    private uint _randomSeed = 12345;
    private float _treeSpawnRatio = 0.3f;
    private int _treeClusterCount = 5;
    private int _treeClusterSpacingMinCells = 2;
    private int _treeClusterSpacingMaxCells = 5;
    private int _treeClusterDistanceMinCells;
    private int _treeClusterDistanceMaxCells = 12;
    private float _yPosition;
    private List<GameObject> _prefabs = new();
    private Transform _rootTransform;
    private CombinedMeshBaker _combinedMeshBaker;
    private RuntimeCityReadModelSystem _cityReadModel;
    private RuntimeGridBlockerSystem _gridBlockers;
    private bool _combinePending;
    private bool _spawned;
    private int _combineAfterFrames = -1;
    public bool HasSpawned => _spawned || !_spawnOnStart || _prefabs == null || _prefabs.Count == 0 || _decorationCount <= 0;

    public void Init(RuntimeDecorationSpawnerSystemConfig config, Transform rootTransform, CombinedMeshBaker combinedMeshBaker, RuntimeCityReadModelSystem cityReadModel, RuntimeGridBlockerSystem gridBlockers)
    {
        _rootTransform = rootTransform;
        _combinedMeshBaker = combinedMeshBaker;
        _cityReadModel = cityReadModel;
        _gridBlockers = gridBlockers;
        ApplyConfig(config);
    }

    public void Update()
    {
        if (_combineAfterFrames >= 0)
        {
            if (_combineAfterFrames == 0)
                FinalizeCombine();
            else
                _combineAfterFrames--;
        }

        TryAutoSpawn();
    }

    public void Dispose()
    {
        ClearSpawnedDecorations();
        _rootTransform = null;
        _combinedMeshBaker = null;
        _cityReadModel = null;
        _gridBlockers = null;
    }

    private void ApplyConfig(RuntimeDecorationSpawnerSystemConfig config)
    {
        if (config == null)
            return;

        _spawnOnStart = config.SpawnOnStart;
        _decorationCount = config.DecorationCount;
        _randomSeed = config.RandomSeed;
        _treeSpawnRatio = Mathf.Clamp01(config.TreeSpawnRatio);
        _treeClusterCount = Mathf.Max(1, config.TreeClusterCount);
        _treeClusterSpacingMinCells = Mathf.Max(1, config.TreeClusterSpacingMinCells);
        _treeClusterSpacingMaxCells = Mathf.Max(_treeClusterSpacingMinCells, config.TreeClusterSpacingMaxCells);
        _treeClusterDistanceMinCells = Mathf.Max(0, config.TreeClusterDistanceMinCells);
        _treeClusterDistanceMaxCells = Mathf.Max(_treeClusterDistanceMinCells, config.TreeClusterDistanceMaxCells);
        _yPosition = config.YPosition;
        _prefabs = config.Prefabs ?? new List<GameObject>();
    }

    public void SpawnDecorations()
    {
        if (_spawned || _combinePending || _rootTransform == null)
            return;
        if (_prefabs == null || _prefabs.Count == 0 || _decorationCount <= 0)
            return;
        if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
            return;

        ClearSpawnedDecorations();

        if (_randomSeed == 0)
            _randomSeed = 1;

        var rng = new Unity.Mathematics.Random(_randomSeed);
        var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
        var seeds = new List<DecorationSeed>(_decorationCount);
        List<int2> treeClusterCenters = BuildTreeClusterCenters(ref rng, grid, _treeClusterCount);

        try
        {
            int spawnedCount = 0;
            int maxAttempts = math.max(_decorationCount * 20, 100);
            int attempts = 0;
            int targetTreeCount = Mathf.Clamp(Mathf.RoundToInt(_decorationCount * _treeSpawnRatio), 0, _decorationCount);
            int spawnedTreeCount = 0;
            int targetTreesPerCluster = Mathf.Max(1, Mathf.CeilToInt(targetTreeCount / (float)Mathf.Max(1, _treeClusterCount)));
            int averageTreeSpacing = Mathf.RoundToInt((_treeClusterSpacingMinCells + _treeClusterSpacingMaxCells) * 0.5f);
            int treeClusterRadius = ComputeTreeClusterRadius(targetTreesPerCluster, averageTreeSpacing);

            while (spawnedCount < _decorationCount && attempts < maxAttempts)
            {
                attempts++;

                GameObject prefab = ChooseDecorationPrefab(ref rng, spawnedTreeCount, targetTreeCount);
                if (prefab == null)
                    continue;

                DecorationPrefabKind kind = ClassifyDecorationKind(prefab.name);
                if (!TryFindDecorationCell(ref rng, grid, roads, blockerData, reserved, seeds, treeClusterCenters, treeClusterRadius, _treeClusterDistanceMinCells, _treeClusterDistanceMaxCells, _treeClusterSpacingMinCells, _treeClusterSpacingMaxCells, kind, out int2 cell))
                    continue;

                int index = GridUtils.CellToIndex(cell, grid.Width);
                GameObject instance = Object.Instantiate(prefab, _rootTransform);
                float3 worldCenter = GridUtils.CellToWorldCenter(grid, cell);
                instance.transform.SetPositionAndRotation(
                    new Vector3(worldCenter.x, _yPosition, worldCenter.z),
                    Quaternion.Euler(0f, rng.NextFloat(0f, 360f), 0f));
                instance.transform.localScale = prefab.transform.localScale;

                reserved.Set(index, true);
                seeds.Add(new DecorationSeed { Cell = cell, Kind = kind });
                if (kind == DecorationPrefabKind.Tree)
                    spawnedTreeCount++;
                spawnedCount++;
            }
        }
        finally
        {
            reserved.Dispose();
        }

        if (_combinedMeshBaker == null)
        {
            _spawned = true;
            return;
        }

        _combinePending = true;
        _combineAfterFrames = 1;
    }

    private void TryAutoSpawn()
    {
        if (!_spawnOnStart || _spawned || _combinePending)
            return;
        if (HasPendingCityGeneration())
            return;
        if (_gridBlockers != null && !_gridBlockers.DependentsReadyForPlacement)
            return;
        if (!TryGetGridData(out _, out _, out _, out _))
            return;

        SpawnDecorations();
    }

    private void FinalizeCombine()
    {
        _combineAfterFrames = -1;
        _combinedMeshBaker?.CombineAtRuntime();
        _combinePending = false;
        _spawned = true;
    }

    private void ClearSpawnedDecorations()
    {
        if (_rootTransform == null)
            return;

        Transform combinedRoot = _combinedMeshBaker != null ? _combinedMeshBaker.CombinedRoot : null;

        for (int i = _rootTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = _rootTransform.GetChild(i);
            if (combinedRoot != null && child == combinedRoot)
                continue;

            Object.Destroy(child.gameObject);
        }
    }

    private bool HasPendingCityGeneration()
    {
        RuntimeCityReadModelSystem cityReadModel = _cityReadModel;
        return cityReadModel != null && cityReadModel.SpawnOnStartEnabled && !cityReadModel.HasSpawned;
    }

    private static bool TryGetGridData(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData)
    {
        gridEntity = Entity.Null;
        grid = default;
        roads = default;
        blockerData = default;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager em = world.EntityManager;
        using var query = em.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridRoad>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        gridEntity = query.GetSingletonEntity();
        grid = em.GetComponentData<GridConfig>(gridEntity);
        roads = em.GetBuffer<GridRoad>(gridEntity);
        blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        return true;
    }

    private bool TryFindDecorationCell(
        ref Unity.Mathematics.Random rng,
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData,
        NativeBitArray reserved,
        List<DecorationSeed> seeds,
        List<int2> treeClusterCenters,
        int treeClusterRadius,
        int treeClusterDistanceMinCells,
        int treeClusterDistanceMaxCells,
        int treeClusterSpacingMinCells,
        int treeClusterSpacingMaxCells,
        DecorationPrefabKind kind,
        out int2 cell)
    {
        cell = default;

        int clusteredAttempts = 16;
        for (int i = 0; i < clusteredAttempts; i++)
        {
            if (!TryGetDecorationAnchor(ref rng, grid, seeds, treeClusterCenters, kind, out int2 anchor))
                break;

            int radius = kind == DecorationPrefabKind.Tree ? treeClusterRadius : GetDecorationClusterRadius(kind);
            int localTreeSpacingCells = kind == DecorationPrefabKind.Tree
                ? SampleSpacing(ref rng, treeClusterSpacingMinCells, treeClusterSpacingMaxCells)
                : Mathf.Max(1, Mathf.RoundToInt((treeClusterSpacingMinCells + treeClusterSpacingMaxCells) * 0.5f));
            int2 candidate = kind == DecorationPrefabKind.Tree
                ? SampleClusterCell(ref rng, anchor, grid.Width - 1, grid.Height - 1, radius, treeClusterDistanceMinCells, treeClusterDistanceMaxCells)
                : new int2(
                    math.clamp(anchor.x + rng.NextInt(-radius, radius + 1), 0, grid.Width - 1),
                    math.clamp(anchor.y + rng.NextInt(-radius, radius + 1), 0, grid.Height - 1));
            if (!CanPlaceDecoration(grid, roads, blockerData, reserved, candidate))
                continue;
            if (!MatchesDecorationHeuristics(candidate, seeds, kind, localTreeSpacingCells))
                continue;

            cell = candidate;
            return true;
        }

        int fallbackAttempts = 24;
        for (int i = 0; i < fallbackAttempts; i++)
        {
            if (kind == DecorationPrefabKind.Tree)
                return false;

            int2 candidate = new(rng.NextInt(0, grid.Width), rng.NextInt(0, grid.Height));
            if (!CanPlaceDecoration(grid, roads, blockerData, reserved, candidate))
                continue;
            if (!MatchesDecorationHeuristics(candidate, seeds, kind, Mathf.Max(1, Mathf.RoundToInt((treeClusterSpacingMinCells + treeClusterSpacingMaxCells) * 0.5f))))
                continue;

            cell = candidate;
            return true;
        }

        return false;
    }

    private bool TryGetDecorationAnchor(ref Unity.Mathematics.Random rng, GridConfig grid, List<DecorationSeed> seeds, List<int2> treeClusterCenters, DecorationPrefabKind kind, out int2 anchor)
    {
        anchor = default;

        if (kind == DecorationPrefabKind.Tree && treeClusterCenters != null && treeClusterCenters.Count > 0)
        {
            anchor = treeClusterCenters[rng.NextInt(0, treeClusterCenters.Count)];
            return true;
        }

        bool preferBlockerEdge = (kind == DecorationPrefabKind.Bush || kind == DecorationPrefabKind.Grass) &&
            _gridBlockers != null &&
            rng.NextFloat() < 0.7f;
        if (preferBlockerEdge && TryFindCellNearExistingBlocker(ref rng, grid, kind, out anchor))
            return true;

        if (seeds.Count == 0)
            return false;

        var matching = new List<int>();
        for (int i = 0; i < seeds.Count; i++)
        {
            DecorationPrefabKind existingKind = seeds[i].Kind;
            if (existingKind == kind)
                matching.Add(i);
            else if (kind == DecorationPrefabKind.Grass && existingKind == DecorationPrefabKind.Bush)
                matching.Add(i);
            else if (kind == DecorationPrefabKind.Bush && (existingKind == DecorationPrefabKind.Rock || existingKind == DecorationPrefabKind.Tree))
                matching.Add(i);
        }

        if (matching.Count == 0)
            return false;

        anchor = seeds[matching[rng.NextInt(0, matching.Count)]].Cell;
        return true;
    }

    private bool TryFindCellNearExistingBlocker(ref Unity.Mathematics.Random rng, GridConfig grid, DecorationPrefabKind kind, out int2 cell)
    {
        cell = default;
        if (_gridBlockers == null)
            return false;

        int radius = kind == DecorationPrefabKind.Bush ? 4 : 6;
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int2 blockerCell = new(rng.NextInt(0, grid.Width), rng.NextInt(0, grid.Height));
            if (!_gridBlockers.IsRuntimeBlockerCell(blockerCell.x, blockerCell.y, grid.Width, grid.Height))
                continue;

            cell = new int2(
                math.clamp(blockerCell.x + rng.NextInt(-radius, radius + 1), 0, grid.Width - 1),
                math.clamp(blockerCell.y + rng.NextInt(-radius, radius + 1), 0, grid.Height - 1));
            return true;
        }

        return false;
    }

    private static bool CanPlaceDecoration(
        GridConfig grid,
        DynamicBuffer<GridRoad> roads,
        DynamicBlockerComponent blockerData,
        NativeBitArray reserved,
        int2 cell)
    {
        int index = GridUtils.CellToIndex(cell, grid.Width);
        if (reserved.IsSet(index))
            return false;
        if (roads[index].Value != 0)
            return false;
        if (blockerData.Blocked.IsCreated && blockerData.Blocked.IsSet(index))
            return false;

        return true;
    }

    private bool MatchesDecorationHeuristics(int2 candidate, List<DecorationSeed> seeds, DecorationPrefabKind kind, int treeMinSpacingCells)
    {
        int sameKindNearby = 0;
        int veryCloseSameKind = 0;
        int nearbyPlants = 0;
        int nearbyBlockers = CountNearbyBlockerCells(candidate, 4);
        int nearestDistanceSq = int.MaxValue;
        int treeMinSpacingSq = treeMinSpacingCells * treeMinSpacingCells;

        for (int i = 0; i < seeds.Count; i++)
        {
            int dx = seeds[i].Cell.x - candidate.x;
            int dy = seeds[i].Cell.y - candidate.y;
            int distanceSq = (dx * dx) + (dy * dy);
            nearestDistanceSq = math.min(nearestDistanceSq, distanceSq);
            if (distanceSq <= treeMinSpacingSq && seeds[i].Kind == kind)
                veryCloseSameKind++;
            if (distanceSq > 49)
                continue;

            if (seeds[i].Kind == kind)
                sameKindNearby++;
            if (seeds[i].Kind == DecorationPrefabKind.Bush || seeds[i].Kind == DecorationPrefabKind.Grass || seeds[i].Kind == DecorationPrefabKind.Tree)
                nearbyPlants++;
        }

        return kind switch
        {
            DecorationPrefabKind.Tree => veryCloseSameKind == 0,
            DecorationPrefabKind.Bush => nearbyBlockers >= 1 || sameKindNearby >= 1 || nearbyPlants >= 2 || nearestDistanceSq > 64,
            DecorationPrefabKind.Grass => nearbyBlockers >= 1 || nearbyPlants >= 1 || sameKindNearby >= 2 || nearestDistanceSq > 81,
            DecorationPrefabKind.Rock => sameKindNearby >= 1 || nearestDistanceSq > 49,
            _ => true
        };
    }

    private int CountNearbyBlockerCells(int2 candidate, int radius)
    {
        if (_gridBlockers == null)
            return 0;

        int count = 0;
        for (int y = candidate.y - radius; y <= candidate.y + radius; y++)
        {
            for (int x = candidate.x - radius; x <= candidate.x + radius; x++)
            {
                if (_gridBlockers.IsRuntimeBlockerCell(new Vector2Int(x, y)))
                    count++;
            }
        }

        return count;
    }

    private static int GetDecorationClusterRadius(DecorationPrefabKind kind)
    {
        return kind switch
        {
            DecorationPrefabKind.Tree => 10,
            DecorationPrefabKind.Rock => 5,
            DecorationPrefabKind.Bush => 4,
            DecorationPrefabKind.Grass => 6,
            _ => 7
        };
    }

    private static DecorationPrefabKind ClassifyDecorationKind(string prefabName)
    {
        string name = prefabName.ToLowerInvariant();
        if (name.Contains("tree") || name.Contains("palm"))
            return DecorationPrefabKind.Tree;
        if (name.Contains("rock") || name.Contains("stone") || name.Contains("boulder"))
            return DecorationPrefabKind.Rock;
        if (name.Contains("bush") || name.Contains("shrub"))
            return DecorationPrefabKind.Bush;
        if (name.Contains("grass") || name.Contains("plant") || name.Contains("fern"))
            return DecorationPrefabKind.Grass;

        return DecorationPrefabKind.Other;
    }

    private GameObject ChooseDecorationPrefab(ref Unity.Mathematics.Random rng, int spawnedTreeCount, int targetTreeCount)
    {
        bool preferTrees = spawnedTreeCount < targetTreeCount;
        return ChoosePrefabByTreeTarget(_prefabs, ref rng, preferTrees, static prefab => ClassifyDecorationKind(prefab.name) == DecorationPrefabKind.Tree);
    }

    private static List<int2> BuildTreeClusterCenters(ref Unity.Mathematics.Random rng, GridConfig grid, int clusterCount)
    {
        int count = Mathf.Max(1, clusterCount);
        var centers = new List<int2>(count);
        int minDistance = Mathf.Max(8, Mathf.Min(grid.Width, grid.Height) / 6);
        int minDistanceSq = minDistance * minDistance;

        int maxAttempts = count * 12;
        for (int attempt = 0; attempt < maxAttempts && centers.Count < count; attempt++)
        {
            int2 candidate = new(rng.NextInt(0, grid.Width), rng.NextInt(0, grid.Height));
            bool farEnough = true;
            for (int i = 0; i < centers.Count; i++)
            {
                int dx = centers[i].x - candidate.x;
                int dy = centers[i].y - candidate.y;
                if ((dx * dx) + (dy * dy) < minDistanceSq)
                {
                    farEnough = false;
                    break;
                }
            }

            if (farEnough)
                centers.Add(candidate);
        }

        if (centers.Count == 0)
            centers.Add(new int2(grid.Width / 2, grid.Height / 2));

        return centers;
    }

    private static int ComputeTreeClusterRadius(int targetTreesPerCluster, int treeMinSpacingCells)
    {
        float spacing = Mathf.Max(1f, treeMinSpacingCells);
        float estimatedSide = Mathf.Sqrt(Mathf.Max(1, targetTreesPerCluster)) * spacing;
        return Mathf.Max(10, Mathf.CeilToInt(estimatedSide * 0.6f));
    }

    private static int2 SampleClusterCell(
        ref Unity.Mathematics.Random rng,
        int2 anchor,
        int maxX,
        int maxY,
        int radius,
        int distanceMinCells,
        int distanceMaxCells)
    {
        int clampedMaxDistance = Mathf.Max(0, Mathf.Min(radius, distanceMaxCells));
        int clampedMinDistance = Mathf.Clamp(distanceMinCells, 0, clampedMaxDistance);
        float angle = rng.NextFloat(0f, math.PI * 2f);
        float distance = clampedMaxDistance <= 0
            ? 0f
            : rng.NextFloat(clampedMinDistance, clampedMaxDistance + 1f);

        int x = math.clamp(anchor.x + Mathf.RoundToInt(math.cos(angle) * distance), 0, maxX);
        int y = math.clamp(anchor.y + Mathf.RoundToInt(math.sin(angle) * distance), 0, maxY);
        return new int2(x, y);
    }

    private static int SampleSpacing(ref Unity.Mathematics.Random rng, int minCells, int maxCells)
    {
        int clampedMin = Mathf.Max(1, minCells);
        int clampedMax = Mathf.Max(clampedMin, maxCells);
        return rng.NextInt(clampedMin, clampedMax + 1);
    }

    private static GameObject ChoosePrefabByTreeTarget(
        List<GameObject> prefabs,
        ref Unity.Mathematics.Random rng,
        bool preferTrees,
        System.Predicate<GameObject> isTreePrefab)
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        var treePrefabs = new List<GameObject>();
        var otherPrefabs = new List<GameObject>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
                continue;
            if (isTreePrefab(prefab))
                treePrefabs.Add(prefab);
            else
                otherPrefabs.Add(prefab);
        }

        List<GameObject> source = preferTrees
            ? (treePrefabs.Count > 0 ? treePrefabs : otherPrefabs)
            : (otherPrefabs.Count > 0 ? otherPrefabs : treePrefabs);
        return source.Count == 0 ? null : source[rng.NextInt(0, source.Count)];
    }
}
