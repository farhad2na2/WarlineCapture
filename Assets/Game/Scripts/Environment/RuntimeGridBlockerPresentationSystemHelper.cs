using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    public sealed class RuntimeGridBlockerPresentationSystemHelper : IRuntimeGridBlockerCellLookup
    {
        private enum BlockerPrefabKind
        {
            Other,
            Tree,
            Rock,
            Bush,
            Grass
        }

        private sealed class RuntimeBlockerRecord
        {
            public int Id;
            public Entity Entity;
            public GameObject RootObject;
            public Vector2Int OriginCell;
            public Vector2Int SizeCells;
        }

        private sealed class PrefabPlacementMetadata
        {
            public Bounds LocalBounds;
            public bool HasBounds;
            public bool BlocksOnlyCenterCell;
            public BlockerPrefabKind Kind;
        }

        private struct PendingBlockerPlacement
        {
            public GameObject Prefab;
            public PrefabPlacementMetadata Metadata;
            public Vector2Int OriginCell;
            public Vector2Int SizeCells;
        }

        private readonly Dictionary<int, RuntimeBlockerRecord> _blockersById = new();
        private readonly Dictionary<GameObject, PrefabPlacementMetadata> _metadataByPrefab = new();
        private int[] _runtimeBlockerCellCounts;
        private int _nextBlockerId = 1;
        private bool _spawnOnStart = true;
        private int _blockerCount = 80;
        private uint _randomSeed = 24680;
        private float _treeSpawnRatio = 0.4f;
        private int _treeClusterCount = 6;
        private int _treeClusterSpacingMinCells = 2;
        private int _treeClusterSpacingMaxCells = 6;
        private int _treeClusterDistanceMinCells;
        private int _treeClusterDistanceMaxCells = 14;
        private float _yPosition;
        private List<GameObject> _prefabs = new();
        private Transform _rootTransform;
        private RuntimeCityReadModelCompositionSystemHelper _cityReadModel;
        private bool _spawned;
        private bool _spawnFinalizing;
        private bool _readyForDependents = true;
        private int _finalizeAfterFrames = -1;
        private Entity _dependencyStateEntity;

        public bool DependentsReadyForPlacement => _readyForDependents;
        public bool HasSpawned => _spawned || !_spawnOnStart || _prefabs == null || _prefabs.Count == 0 || _blockerCount <= 0;

        public void Init(RuntimeGridBlockerSystemConfig config, Transform rootTransform, RuntimeCityReadModelCompositionSystemHelper cityReadModel)
        {
            _rootTransform = rootTransform;
            _cityReadModel = cityReadModel;
            ApplyConfig(config);
            LoadPrefabsIfNeeded();
            _readyForDependents = !_spawnOnStart || _prefabs.Count == 0 || _blockerCount <= 0;
            WriteDependencyState();
        }

        public void Update()
        {
            if (_finalizeAfterFrames >= 0)
            {
                if (_finalizeAfterFrames == 0)
                    FinalizeSpawn();
                else
                    _finalizeAfterFrames--;
            }

            TryAutoSpawn();
            WriteDependencyState();
        }

        public void Dispose()
        {
            foreach (RuntimeBlockerRecord blocker in _blockersById.Values)
            {
                if (blocker.RootObject != null)
                    Object.Destroy(blocker.RootObject);
            }

            _blockersById.Clear();
            _metadataByPrefab.Clear();
            _runtimeBlockerCellCounts = null;
            _rootTransform = null;
            _cityReadModel = null;
            _readyForDependents = true;
            WriteDependencyState();
        }

        private void ApplyConfig(RuntimeGridBlockerSystemConfig config)
        {
            if (config == null)
                return;

            _spawnOnStart = config.SpawnOnStart;
            _blockerCount = config.BlockerCount;
            _randomSeed = config.RandomSeed;
            _treeSpawnRatio = Mathf.Clamp01(config.TreeSpawnRatio);
            _treeClusterCount = Mathf.Max(1, config.TreeClusterCount);
            _treeClusterSpacingMinCells = Mathf.Max(1, config.TreeClusterSpacingMinCells);
            _treeClusterSpacingMaxCells = Mathf.Max(_treeClusterSpacingMinCells, config.TreeClusterSpacingMaxCells);
            _treeClusterDistanceMinCells = Mathf.Max(0, config.TreeClusterDistanceMinCells);
            _treeClusterDistanceMaxCells = Mathf.Max(_treeClusterDistanceMinCells, config.TreeClusterDistanceMaxCells);
            _yPosition = config.YPosition;
            _prefabs = config.Prefabs != null ? new List<GameObject>(config.Prefabs) : new List<GameObject>();
        }

        public bool IsRuntimeBlockerCell(Vector2Int cell)
        {
            if (!TryGetGridData(out _, out GridConfig grid, out _, out _, out _))
                return false;

            return IsRuntimeBlockerCell(cell.x, cell.y, grid.Width, grid.Height);
        }

        public bool IsRuntimeBlockerCell(int x, int y, int gridWidth, int gridHeight)
        {
            if ((uint)x >= (uint)gridWidth || (uint)y >= (uint)gridHeight)
                return false;
            if (_runtimeBlockerCellCounts == null || _runtimeBlockerCellCounts.Length != gridWidth * gridHeight)
                return false;

            return _runtimeBlockerCellCounts[GridUtils.CellToIndex(new int2(x, y), gridWidth)] > 0;
        }

        public void RemoveBlockersOverlappingFootprint(Vector2Int originCell, Vector2Int footprintCells)
        {
            if (_blockersById.Count == 0)
                return;

            var toRemove = new List<int>();
            RectInt footprint = new(originCell, footprintCells);
            foreach (var entry in _blockersById)
            {
                RuntimeBlockerRecord blocker = entry.Value;
                RectInt blockerRect = new(blocker.OriginCell, blocker.SizeCells);
                if (!footprint.Overlaps(blockerRect))
                    continue;

                toRemove.Add(entry.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                RemoveBlockerById(toRemove[i]);
        }

        private void TryAutoSpawn()
        {
            if (!_spawnOnStart || _spawned || _spawnFinalizing)
                return;
            if (HasPendingCityGeneration())
                return;

            LoadPrefabsIfNeeded();
            if (_prefabs == null || _prefabs.Count == 0 || _blockerCount <= 0)
            {
                _readyForDependents = true;
                _spawned = true;
                return;
            }

            if (!TryGetGridData(out _, out GridConfig grid, out DynamicBuffer<GridWalkable> walkable, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData))
                return;

            SpawnBlockers(grid, walkable, roads, blockerData);
        }

        private void SpawnBlockers(GridConfig grid, DynamicBuffer<GridWalkable> walkable, DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockerData)
        {
            _readyForDependents = false;
            _runtimeBlockerCellCounts = new int[grid.Width * grid.Height];
            var pendingPlacements = new List<PendingBlockerPlacement>(_blockerCount);

            if (_randomSeed == 0)
                _randomSeed = 1;

            var rng = new Unity.Mathematics.Random(_randomSeed);
            var reserved = new NativeBitArray(grid.Width * grid.Height, Allocator.Temp);
            List<Vector2Int> treeClusterCenters = BuildTreeClusterCenters(ref rng, grid, _treeClusterCount);

            try
            {
                int maxAttempts = math.max(_blockerCount * 30, 200);
                int attempts = 0;
                int spawnedCount = 0;
                int targetTreeCount = Mathf.Clamp(Mathf.RoundToInt(_blockerCount * _treeSpawnRatio), 0, _blockerCount);
                int spawnedTreeCount = 0;
                int targetTreesPerCluster = Mathf.Max(1, Mathf.CeilToInt(targetTreeCount / (float)Mathf.Max(1, _treeClusterCount)));
                int averageTreeSpacing = Mathf.RoundToInt((_treeClusterSpacingMinCells + _treeClusterSpacingMaxCells) * 0.5f);
                int treeClusterRadius = ComputeTreeClusterRadius(targetTreesPerCluster, averageTreeSpacing);

                while (spawnedCount < _blockerCount && attempts < maxAttempts)
                {
                    attempts++;

                    GameObject prefab = ChooseBlockerPrefab(ref rng, spawnedTreeCount, targetTreeCount);
                    if (prefab == null)
                        continue;

                    PrefabPlacementMetadata metadata = GetMetadata(prefab);
                    Vector2Int sizeCells = ComputeFootprintCells(metadata, grid.CellSize);
                    if (sizeCells.x > grid.Width || sizeCells.y > grid.Height)
                        continue;

                    if (!TryFindBlockerOrigin(
                            ref rng,
                            grid,
                            walkable,
                            roads,
                            blockerData,
                            reserved,
                            pendingPlacements,
                            treeClusterCenters,
                            treeClusterRadius,
                            _treeClusterDistanceMinCells,
                            _treeClusterDistanceMaxCells,
                            _treeClusterSpacingMinCells,
                            _treeClusterSpacingMaxCells,
                            metadata,
                            sizeCells,
                            out Vector2Int originCell))
                    {
                        continue;
                    }

                    int x = originCell.x;
                    int y = originCell.y;
                    if (!CanPlaceFootprint(grid, walkable, roads, blockerData, reserved, x, y, sizeCells))
                        continue;

                    MarkReserved(grid.Width, reserved, originCell, sizeCells);
                    pendingPlacements.Add(new PendingBlockerPlacement
                    {
                        Prefab = prefab,
                        Metadata = metadata,
                        OriginCell = originCell,
                        SizeCells = sizeCells
                    });

                    if (metadata.Kind == BlockerPrefabKind.Tree)
                        spawnedTreeCount++;
                    spawnedCount++;
                }
            }
            finally
            {
                reserved.Dispose();
            }

            for (int i = 0; i < pendingPlacements.Count; i++)
            {
                PendingBlockerPlacement placement = pendingPlacements[i];
                CreateRuntimeBlocker(grid, placement.Prefab, placement.Metadata, placement.OriginCell, placement.SizeCells);
            }

            _spawnFinalizing = true;
            _finalizeAfterFrames = 1;
        }

        private void FinalizeSpawn()
        {
            _finalizeAfterFrames = -1;
            _spawnFinalizing = false;
            _spawned = true;
            _readyForDependents = true;
            WriteDependencyState();
        }

        private bool HasPendingCityGeneration()
        {
            RuntimeCityReadModelCompositionSystemHelper cityReadModel = _cityReadModel;
            return cityReadModel != null && cityReadModel.SpawnOnStartEnabled && !cityReadModel.HasSpawned;
        }

        private void WriteDependencyState()
        {
            if (!TryGetEntityManager(out EntityManager em))
                return;

            if (_dependencyStateEntity == Entity.Null || !em.Exists(_dependencyStateEntity))
            {
                using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadWrite<RuntimeGridBlockerDependencyComponent>());
                _dependencyStateEntity = query.IsEmptyIgnoreFilter
                    ? em.CreateEntity(typeof(RuntimeGridBlockerDependencyComponent))
                    : query.GetSingletonEntity();
            }

            RuntimeCityReadModelCompositionSystemHelper cityReadModel = _cityReadModel;
            bool pendingCity = cityReadModel != null && cityReadModel.SpawnOnStartEnabled && !cityReadModel.HasSpawned;
            em.SetComponentData(_dependencyStateEntity, new RuntimeGridBlockerDependencyComponent
            {
                ReadyForDependents = _readyForDependents ? (byte)1 : (byte)0,
                SpawnOnStart = _spawnOnStart ? (byte)1 : (byte)0,
                Spawned = _spawned ? (byte)1 : (byte)0,
                SpawnFinalizing = _spawnFinalizing ? (byte)1 : (byte)0,
                FinalizeAfterFrames = _finalizeAfterFrames,
                PendingCity = pendingCity ? (byte)1 : (byte)0,
                CityHasSpawned = cityReadModel != null && cityReadModel.HasSpawned ? (byte)1 : (byte)0,
                CityGenerating = cityReadModel != null && cityReadModel.IsGenerating ? (byte)1 : (byte)0
            });
        }

        private void CreateRuntimeBlocker(GridConfig grid, GameObject prefab, PrefabPlacementMetadata metadata, Vector2Int originCell, Vector2Int sizeCells)
        {
            if (_rootTransform == null || !TryGetEntityManager(out EntityManager em))
                return;

            int blockerId = _nextBlockerId++;
            Entity entity = em.CreateEntity();
            em.AddComponentData(entity, new UnitGrid { Cell = new int2(originCell.x, originCell.y) });
            em.AddComponentData(entity, new GridBlockerSize { Size = new int2(sizeCells.x, sizeCells.y) });
            em.AddComponent<StaticGridBlocker>(entity);

            var root = new GameObject($"{prefab.name}_{blockerId}");
            root.transform.SetParent(_rootTransform, false);
            root.transform.position = GetFootprintCenter(originCell, sizeCells, grid, _yPosition);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            GameObject visual = Object.Instantiate(prefab, root.transform);
            visual.name = prefab.name;
            if (metadata.HasBounds)
            {
                root.transform.position = GetFootprintCenter(originCell, sizeCells, grid, _yPosition);
                visual.transform.localPosition = new Vector3(-metadata.LocalBounds.center.x, 0f, -metadata.LocalBounds.center.z);
            }
            else
            {
                visual.transform.localPosition = Vector3.zero;
            }

            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            RegisterBlockerCells(grid.Width, originCell, sizeCells, +1);
            _blockersById[blockerId] = new RuntimeBlockerRecord
            {
                Id = blockerId,
                Entity = entity,
                RootObject = root,
                OriginCell = originCell,
                SizeCells = sizeCells
            };
        }

        private void RemoveBlockerById(int blockerId)
        {
            if (!_blockersById.TryGetValue(blockerId, out RuntimeBlockerRecord blocker))
                return;

            if (TryGetGridData(out _, out GridConfig grid, out _, out _, out _))
                RegisterBlockerCells(grid.Width, blocker.OriginCell, blocker.SizeCells, -1);

            if (TryGetEntityManager(out EntityManager em) && blocker.Entity != Entity.Null && em.Exists(blocker.Entity))
                em.DestroyEntity(blocker.Entity);

            if (blocker.RootObject != null)
                Object.Destroy(blocker.RootObject);

            _blockersById.Remove(blockerId);
        }

        private void RegisterBlockerCells(int gridWidth, Vector2Int originCell, Vector2Int sizeCells, int delta)
        {
            if (_runtimeBlockerCellCounts == null)
                return;

            for (int y = originCell.y; y < originCell.y + sizeCells.y; y++)
            {
                for (int x = originCell.x; x < originCell.x + sizeCells.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), gridWidth);
                    _runtimeBlockerCellCounts[index] = math.max(0, _runtimeBlockerCellCounts[index] + delta);
                }
            }
        }

        private static bool CanPlaceFootprint(
            GridConfig grid,
            DynamicBuffer<GridWalkable> walkable,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            NativeBitArray reserved,
            int startX,
            int startY,
            Vector2Int sizeCells)
        {
            for (int y = startY; y < startY + sizeCells.y; y++)
            {
                for (int x = startX; x < startX + sizeCells.x; x++)
                {
                    int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                    if (walkable[index].Value == 0)
                        return false;
                    if (roads[index].Value != 0)
                        return false;
                    if (reserved.IsSet(index))
                        return false;
                    if (blockerData.Blocked.IsCreated && blockerData.Blocked.IsSet(index))
                        return false;
                }
            }

            return true;
        }

        private static void MarkReserved(int gridWidth, NativeBitArray reserved, Vector2Int originCell, Vector2Int sizeCells)
        {
            for (int y = originCell.y; y < originCell.y + sizeCells.y; y++)
            {
                for (int x = originCell.x; x < originCell.x + sizeCells.x; x++)
                    reserved.Set(GridUtils.CellToIndex(new int2(x, y), gridWidth), true);
            }
        }

        private PrefabPlacementMetadata GetMetadata(GameObject prefab)
        {
            if (_metadataByPrefab.TryGetValue(prefab, out PrefabPlacementMetadata metadata))
                return metadata;

            metadata = BuildMetadata(prefab);
            _metadataByPrefab[prefab] = metadata;
            return metadata;
        }

        private static PrefabPlacementMetadata BuildMetadata(GameObject prefab)
        {
            var metadata = new PrefabPlacementMetadata();
            if (prefab == null)
                return metadata;

            var meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            bool hasBounds = false;
            Bounds bounds = default;
            Matrix4x4 rootWorldToLocal = prefab.transform.worldToLocalMatrix;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Matrix4x4 localToRoot = rootWorldToLocal * meshFilter.transform.localToWorldMatrix;
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;

                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 corner = new(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localCorner = localToRoot.MultiplyPoint3x4(corner);
                            if (!hasBounds)
                            {
                                bounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            metadata.HasBounds = hasBounds;
            metadata.LocalBounds = bounds;
            metadata.BlocksOnlyCenterCell = prefab.name.Contains("Tree");
            metadata.Kind = ClassifyPrefabKind(prefab.name);
            return metadata;
        }

        private static bool TryFindBlockerOrigin(
            ref Unity.Mathematics.Random rng,
            GridConfig grid,
            DynamicBuffer<GridWalkable> walkable,
            DynamicBuffer<GridRoad> roads,
            DynamicBlockerComponent blockerData,
            NativeBitArray reserved,
            List<PendingBlockerPlacement> pendingPlacements,
            List<Vector2Int> treeClusterCenters,
            int treeClusterRadius,
            int treeClusterDistanceMinCells,
            int treeClusterDistanceMaxCells,
            int treeClusterSpacingMinCells,
            int treeClusterSpacingMaxCells,
            PrefabPlacementMetadata metadata,
            Vector2Int sizeCells,
            out Vector2Int originCell)
        {
            int maxX = grid.Width - sizeCells.x;
            int maxY = grid.Height - sizeCells.y;
            originCell = default;
            if (maxX < 0 || maxY < 0)
                return false;

            int anchorAttempts = 10;
            for (int i = 0; i < anchorAttempts; i++)
            {
                if (!TryGetBlockerAnchor(ref rng, pendingPlacements, treeClusterCenters, metadata.Kind, out Vector2Int anchor))
                    break;

                int radius = metadata.Kind == BlockerPrefabKind.Tree ? treeClusterRadius : GetClusterRadius(metadata.Kind);
                int localTreeSpacingCells = metadata.Kind == BlockerPrefabKind.Tree
                    ? SampleSpacing(ref rng, treeClusterSpacingMinCells, treeClusterSpacingMaxCells)
                    : Mathf.Max(1, Mathf.RoundToInt((treeClusterSpacingMinCells + treeClusterSpacingMaxCells) * 0.5f));
                Vector2Int candidate = metadata.Kind == BlockerPrefabKind.Tree
                    ? SampleClusterCell(ref rng, anchor, maxX, maxY, radius, treeClusterDistanceMinCells, treeClusterDistanceMaxCells)
                    : new Vector2Int(
                        math.clamp(anchor.x + rng.NextInt(-radius, radius + 1), 0, maxX),
                        math.clamp(anchor.y + rng.NextInt(-radius, radius + 1), 0, maxY));

                int x = candidate.x;
                int y = candidate.y;
                if (!CanPlaceFootprint(grid, walkable, roads, blockerData, reserved, x, y, sizeCells))
                    continue;
                if (!MatchesLocalPlacementHeuristics(candidate, pendingPlacements, metadata.Kind, localTreeSpacingCells))
                    continue;

                originCell = candidate;
                return true;
            }

            int fallbackAttempts = 20;
            for (int i = 0; i < fallbackAttempts; i++)
            {
                if (metadata.Kind == BlockerPrefabKind.Tree)
                    return false;

                int x = rng.NextInt(0, maxX + 1);
                int y = rng.NextInt(0, maxY + 1);
                var candidate = new Vector2Int(x, y);
                if (!CanPlaceFootprint(grid, walkable, roads, blockerData, reserved, x, y, sizeCells))
                    continue;
                if (!MatchesLocalPlacementHeuristics(candidate, pendingPlacements, metadata.Kind, Mathf.Max(1, Mathf.RoundToInt((treeClusterSpacingMinCells + treeClusterSpacingMaxCells) * 0.5f))))
                    continue;

                originCell = candidate;
                return true;
            }

            return false;
        }

        private static bool TryGetBlockerAnchor(
            ref Unity.Mathematics.Random rng,
            List<PendingBlockerPlacement> pendingPlacements,
            List<Vector2Int> treeClusterCenters,
            BlockerPrefabKind kind,
            out Vector2Int anchor)
        {
            anchor = default;
            if (kind == BlockerPrefabKind.Tree && treeClusterCenters != null && treeClusterCenters.Count > 0)
            {
                anchor = treeClusterCenters[rng.NextInt(0, treeClusterCenters.Count)];
                return true;
            }
            if (pendingPlacements.Count == 0)
                return false;

            bool preferCluster = kind == BlockerPrefabKind.Tree || kind == BlockerPrefabKind.Bush || kind == BlockerPrefabKind.Grass;
            bool preferRocksForBushes = kind == BlockerPrefabKind.Bush && rng.NextFloat() < 0.65f;

            var matchingIndices = new List<int>();
            for (int i = 0; i < pendingPlacements.Count; i++)
            {
                BlockerPrefabKind existingKind = pendingPlacements[i].Metadata.Kind;
                if (preferRocksForBushes)
                {
                    if (existingKind == BlockerPrefabKind.Rock)
                        matchingIndices.Add(i);
                    continue;
                }

                if (existingKind == kind)
                    matchingIndices.Add(i);
            }

            if (matchingIndices.Count == 0 && preferCluster)
            {
                for (int i = 0; i < pendingPlacements.Count; i++)
                {
                    BlockerPrefabKind existingKind = pendingPlacements[i].Metadata.Kind;
                    if (kind == BlockerPrefabKind.Grass && existingKind == BlockerPrefabKind.Bush)
                        matchingIndices.Add(i);
                    else if (kind == BlockerPrefabKind.Bush && existingKind == BlockerPrefabKind.Tree)
                        matchingIndices.Add(i);
                }
            }

            if (matchingIndices.Count == 0)
                return false;

            PendingBlockerPlacement placement = pendingPlacements[matchingIndices[rng.NextInt(0, matchingIndices.Count)]];
            anchor = placement.OriginCell;
            return true;
        }

        private static int GetClusterRadius(BlockerPrefabKind kind)
        {
            return kind switch
            {
                BlockerPrefabKind.Tree => 10,
                BlockerPrefabKind.Rock => 5,
                BlockerPrefabKind.Bush => 4,
                BlockerPrefabKind.Grass => 5,
                _ => 6
            };
        }

        private static bool MatchesLocalPlacementHeuristics(Vector2Int candidate, List<PendingBlockerPlacement> pendingPlacements, BlockerPrefabKind kind, int treeMinSpacingCells)
        {
            if (pendingPlacements.Count == 0)
                return true;

            int sameKindNearby = 0;
            int veryCloseSameKind = 0;
            int rocksNearby = 0;
            int plantsNearby = 0;
            int nearestDistanceSq = int.MaxValue;
            int treeMinSpacingSq = treeMinSpacingCells * treeMinSpacingCells;

            for (int i = 0; i < pendingPlacements.Count; i++)
            {
                Vector2Int existing = pendingPlacements[i].OriginCell;
                int dx = existing.x - candidate.x;
                int dy = existing.y - candidate.y;
                int distanceSq = (dx * dx) + (dy * dy);
                nearestDistanceSq = math.min(nearestDistanceSq, distanceSq);
                if (distanceSq <= treeMinSpacingSq && pendingPlacements[i].Metadata.Kind == kind)
                    veryCloseSameKind++;
                if (distanceSq > 64)
                    continue;

                BlockerPrefabKind existingKind = pendingPlacements[i].Metadata.Kind;
                if (existingKind == kind)
                    sameKindNearby++;
                if (existingKind == BlockerPrefabKind.Rock)
                    rocksNearby++;
                if (existingKind == BlockerPrefabKind.Tree || existingKind == BlockerPrefabKind.Bush || existingKind == BlockerPrefabKind.Grass)
                    plantsNearby++;
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

        private static BlockerPrefabKind ClassifyPrefabKind(string prefabName)
        {
            string name = prefabName.ToLowerInvariant();
            if (name.Contains("tree") || name.Contains("palm"))
                return BlockerPrefabKind.Tree;
            if (name.Contains("rock") || name.Contains("stone") || name.Contains("boulder"))
                return BlockerPrefabKind.Rock;
            if (name.Contains("bush") || name.Contains("shrub"))
                return BlockerPrefabKind.Bush;
            if (name.Contains("grass") || name.Contains("plant") || name.Contains("fern"))
                return BlockerPrefabKind.Grass;

            return BlockerPrefabKind.Other;
        }

        private GameObject ChooseBlockerPrefab(ref Unity.Mathematics.Random rng, int spawnedTreeCount, int targetTreeCount)
        {
            bool preferTrees = spawnedTreeCount < targetTreeCount;
            return ChoosePrefabByTreeTarget(_prefabs, ref rng, preferTrees, static prefab => ClassifyPrefabKind(prefab.name) == BlockerPrefabKind.Tree);
        }

        private static List<Vector2Int> BuildTreeClusterCenters(ref Unity.Mathematics.Random rng, GridConfig grid, int clusterCount)
        {
            int count = Mathf.Max(1, clusterCount);
            var centers = new List<Vector2Int>(count);
            int minDistance = Mathf.Max(8, Mathf.Min(grid.Width, grid.Height) / 6);
            int minDistanceSq = minDistance * minDistance;

            int maxAttempts = count * 12;
            for (int attempt = 0; attempt < maxAttempts && centers.Count < count; attempt++)
            {
                var candidate = new Vector2Int(rng.NextInt(0, grid.Width), rng.NextInt(0, grid.Height));
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
                centers.Add(new Vector2Int(grid.Width / 2, grid.Height / 2));

            return centers;
        }

        private static int ComputeTreeClusterRadius(int targetTreesPerCluster, int treeMinSpacingCells)
        {
            float spacing = Mathf.Max(1f, treeMinSpacingCells);
            float estimatedSide = Mathf.Sqrt(Mathf.Max(1, targetTreesPerCluster)) * spacing;
            return Mathf.Max(10, Mathf.CeilToInt(estimatedSide * 0.6f));
        }

        private static Vector2Int SampleClusterCell(
            ref Unity.Mathematics.Random rng,
            Vector2Int anchor,
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
            return new Vector2Int(x, y);
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

        private static Vector2Int ComputeFootprintCells(PrefabPlacementMetadata metadata, float cellSize)
        {
            if (metadata.BlocksOnlyCenterCell)
                return Vector2Int.one;
            if (!metadata.HasBounds || cellSize <= 0f)
                return Vector2Int.one;

            int width = Mathf.Max(1, Mathf.CeilToInt(metadata.LocalBounds.size.x / cellSize));
            int depth = Mathf.Max(1, Mathf.CeilToInt(metadata.LocalBounds.size.z / cellSize));
            return new Vector2Int(width, depth);
        }

        private static Vector3 GetFootprintCenter(Vector2Int originCell, Vector2Int sizeCells, GridConfig grid, float y)
        {
            return new Vector3(
                grid.Origin.x + (originCell.x + sizeCells.x * 0.5f) * grid.CellSize,
                y,
                grid.Origin.z + (originCell.y + sizeCells.y * 0.5f) * grid.CellSize);
        }

        private void LoadPrefabsIfNeeded()
        {
            if (_prefabs == null)
            {
                _prefabs = new List<GameObject>();
                return;
            }

            _prefabs.RemoveAll(static prefab => prefab == null);
            _prefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        }

        private static bool TryGetGridData(
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridWalkable> walkable,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData)
        {
            gridEntity = Entity.Null;
            grid = default;
            walkable = default;
            roads = default;
            blockerData = default;

            if (!TryGetEntityManager(out EntityManager em))
                return false;

            using var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridWalkable>(),
                ComponentType.ReadOnly<GridRoad>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            gridEntity = query.GetSingletonEntity();
            grid = em.GetComponentData<GridConfig>(gridEntity);
            walkable = em.GetBuffer<GridWalkable>(gridEntity);
            roads = em.GetBuffer<GridRoad>(gridEntity);
            blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            return true;
        }

        private static bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = default;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return false;

            entityManager = world.EntityManager;
            return true;
        }
    }
}
