using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Simulation;

namespace Game.Runtime
{
    public sealed class RuntimeGridBlockerPresentationSystemHelper : IRuntimeGridBlockerCellLookup
    {
        private sealed class PrefabPlacementMetadata
        {
            public Bounds LocalBounds;
            public bool HasBounds;
            public bool BlocksOnlyCenterCell;
            public BlockerPrefabKind Kind;
        }

        private sealed class PlacementGridAdapter : IRuntimeGridBlockerPlacementGrid
        {
            private readonly int _width;
            private readonly DynamicBuffer<GridWalkable> _walkable;
            private readonly DynamicBuffer<GridRoad> _roads;
            private readonly NativeBitArray _blocked;

            public int Width => _width;
            public int Height { get; }

            public PlacementGridAdapter(GridConfig grid, DynamicBuffer<GridWalkable> walkable,
                DynamicBuffer<GridRoad> roads, DynamicBlockerComponent blockers)
            {
                _width = grid.Width;
                Height = grid.Height;
                _walkable = walkable;
                _roads = roads;
                _blocked = blockers.Blocked;
            }

            public bool IsWalkable(int cellIndex) => _walkable[cellIndex].Value != 0;
            public bool IsRoad(int cellIndex) => _roads[cellIndex].Value != 0;
            public bool IsBlocked(int cellIndex) => _blocked.IsCreated && _blocked.IsSet(cellIndex);
        }

        private readonly Dictionary<int, GameObject> _rootObjectsByBlockerId = new();
        private readonly Dictionary<GameObject, PrefabPlacementMetadata> _metadataByPrefab = new();
        private readonly RuntimeGridBlockerOwnershipKernel _ownership = new();
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
        private bool _initialized;
        private bool _dependencyStateDirty;

        public bool DependentsReadyForPlacement => _readyForDependents;
        public bool HasSpawned => _spawned || !_spawnOnStart || _prefabs == null || _prefabs.Count == 0 || _blockerCount <= 0;
        public bool RequiresUpdate => _initialized &&
                                      (_dependencyStateDirty ||
                                      _finalizeAfterFrames >= 0 ||
                                      (_spawnOnStart && !_spawned && !_spawnFinalizing));

        public void Init(RuntimeGridBlockerSystemConfig config, Transform rootTransform, RuntimeCityReadModelCompositionSystemHelper cityReadModel)
        {
            _rootTransform = rootTransform;
            _cityReadModel = cityReadModel;
            _initialized = true;
            ApplyConfig(config);
            LoadPrefabsIfNeeded();
            _readyForDependents = !_spawnOnStart || _prefabs.Count == 0 || _blockerCount <= 0;
            _dependencyStateDirty = true;
            WriteDependencyState();
        }

        public void Update()
        {
            if (!RequiresUpdate)
                return;

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
            foreach (GameObject rootObject in _rootObjectsByBlockerId.Values)
            {
                if (rootObject != null)
                    Object.Destroy(rootObject);
            }

            _rootObjectsByBlockerId.Clear();
            _ownership.Clear();
            _metadataByPrefab.Clear();
            _rootTransform = null;
            _cityReadModel = null;
            _initialized = false;
            _readyForDependents = true;
            _dependencyStateDirty = true;
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
            return _ownership.IsRuntimeBlockerCell(x, y, gridWidth, gridHeight);
        }

        public void RemoveBlockersOverlappingFootprint(Vector2Int originCell, Vector2Int footprintCells)
        {
            if (_ownership.Count == 0)
                return;

            IReadOnlyList<int> toRemove = _ownership.FindOverlappingIds(
                new int2(originCell.x, originCell.y), new int2(footprintCells.x, footprintCells.y));
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
            _ownership.InitializeGrid(grid.Width, grid.Height);
            if (_randomSeed == 0)
                _randomSeed = 1;
            var prefabData = new List<RuntimeGridBlockerPrefabData>(_prefabs.Count);
            for (int i = 0; i < _prefabs.Count; i++)
            {
                GameObject prefab = _prefabs[i];
                PrefabPlacementMetadata metadata = GetMetadata(prefab);
                prefabData.Add(new RuntimeGridBlockerPrefabData
                {
                    Name = prefab.name,
                    Kind = metadata.Kind,
                    BlocksOnlyCenterCell = metadata.BlocksOnlyCenterCell,
                    HasBounds = metadata.HasBounds,
                    BoundsWidth = metadata.LocalBounds.size.x,
                    BoundsDepth = metadata.LocalBounds.size.z
                });
            }

            List<RuntimeGridBlockerPlacementData> placements = RuntimeGridBlockerPlacementPolicy.Plan(
                new PlacementGridAdapter(grid, walkable, roads, blockerData), prefabData,
                _blockerCount, _randomSeed, grid.CellSize, _treeSpawnRatio, _treeClusterCount,
                _treeClusterSpacingMinCells, _treeClusterSpacingMaxCells,
                _treeClusterDistanceMinCells, _treeClusterDistanceMaxCells);
            for (int i = 0; i < placements.Count; i++)
            {
                RuntimeGridBlockerPlacementData placement = placements[i];
                GameObject prefab = _prefabs[placement.PrefabIndex];
                PrefabPlacementMetadata metadata = GetMetadata(prefab);
                CreateRuntimeBlocker(grid, prefab, metadata,
                    new Vector2Int(placement.OriginCell.x, placement.OriginCell.y),
                    new Vector2Int(placement.SizeCells.x, placement.SizeCells.y));
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
            _dependencyStateDirty = true;
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
            {
                _dependencyStateDirty = true;
                return;
            }

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
            _dependencyStateDirty = false;
        }

        private void CreateRuntimeBlocker(GridConfig grid, GameObject prefab, PrefabPlacementMetadata metadata, Vector2Int originCell, Vector2Int sizeCells)
        {
            if (_rootTransform == null || !TryGetEntityManager(out EntityManager em))
                return;

            int blockerId = _ownership.AllocateId();
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

            _ownership.Register(blockerId, entity, new int2(originCell.x, originCell.y),
                new int2(sizeCells.x, sizeCells.y));
            _rootObjectsByBlockerId.Add(blockerId, root);
        }

        private void RemoveBlockerById(int blockerId)
        {
            bool hasGrid = TryGetGridData(out _, out _, out _, out _, out _);
            if (!_ownership.TryRemove(blockerId, hasGrid, out RuntimeGridBlockerOwnershipRecord blocker))
                return;

            if (TryGetEntityManager(out EntityManager em) && blocker.Entity != Entity.Null && em.Exists(blocker.Entity))
                em.DestroyEntity(blocker.Entity);

            if (_rootObjectsByBlockerId.TryGetValue(blockerId, out GameObject rootObject) && rootObject != null)
                Object.Destroy(rootObject);
            _rootObjectsByBlockerId.Remove(blockerId);
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

        private static BlockerPrefabKind ClassifyPrefabKind(string prefabName)
        {
            return RuntimeGridBlockerPlacementKernel.ClassifyPrefabKind(prefabName);
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
