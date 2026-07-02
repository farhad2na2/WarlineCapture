using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    using RoadTileData = RoadNetworkCompositionSystemHelper.RoadTileData;
    using RoadVisualType = RoadNetworkCompositionSystemHelper.RoadVisualType;

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public sealed partial class RoadGridProjectionSystem : SystemBase
    {
        public enum RoadFootprintKind
        {
            Dirt,
            Sidewalk
        }

        public sealed class RoadFootprintBoundsData
        {
            public Bounds Bounds;
            public RoadFootprintKind Kind;
        }

        public sealed class CombinedRoadVisualData
        {
            public Mesh Mesh;
            public Material[] Materials;
            public List<RoadFootprintBoundsData> FootprintBounds = new();
        }

        public readonly struct RoadFootprintState
        {
            public readonly IReadOnlyDictionary<Vector2Int, RoadTileData> RoadTiles;
            public readonly IReadOnlyDictionary<Vector2Int, GameObject> SpecialRoadObjects;
            public readonly IReadOnlyDictionary<RoadVisualType, CombinedRoadVisualData> VisualData;
            public readonly Vector3 GridOrigin;
            public readonly float BuildPlaneY;
            public readonly float RoadGridSize;

            public RoadFootprintState(
                IReadOnlyDictionary<Vector2Int, RoadTileData> roadTiles,
                IReadOnlyDictionary<Vector2Int, GameObject> specialRoadObjects,
                IReadOnlyDictionary<RoadVisualType, CombinedRoadVisualData> visualData,
                Vector3 gridOrigin,
                float buildPlaneY,
                float roadGridSize)
            {
                RoadTiles = roadTiles;
                SpecialRoadObjects = specialRoadObjects;
                VisualData = visualData;
                GridOrigin = gridOrigin;
                BuildPlaneY = buildPlaneY;
                RoadGridSize = roadGridSize;
            }
        }

        public readonly struct Context
        {
            public readonly IReadOnlyDictionary<Vector2Int, RoadTileData> RoadTiles;
            public readonly RoadFootprintState FootprintState;
            public readonly float RoadGridSize;

            public Context(
                IReadOnlyDictionary<Vector2Int, RoadTileData> roadTiles,
                RoadFootprintState footprintState,
                float roadGridSize)
            {
                RoadTiles = roadTiles;
                FootprintState = footprintState;
                RoadGridSize = roadGridSize;
            }
        }

        private struct RoadBuffersData
        {
            public DynamicBuffer<GridRoad> Roads;
            public DynamicBuffer<GridRoadSidewalk> Sidewalks;
            public DynamicBuffer<GridRoadDirt> DirtRoads;
            public GridConfig Grid;

            public RoadBuffersData(
                DynamicBuffer<GridRoad> roads,
                DynamicBuffer<GridRoadSidewalk> sidewalks,
                DynamicBuffer<GridRoadDirt> dirtRoads,
                GridConfig grid)
            {
                Roads = roads;
                Sidewalks = sidewalks;
                DirtRoads = dirtRoads;
                Grid = grid;
            }
        }

        private EntityQuery _gridDataQuery;
        private EntityQuery _roadBufferQuery;
        private EntityQuery _roadBuffersQuery;
        private int _deferRoadEcsSyncDepth;
        private bool _pendingRoadEcsSync;

        protected override void OnCreate()
        {
            _gridDataQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridRoad>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>());
            _roadBufferQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadWrite<GridRoad>());
            _roadBuffersQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadWrite<GridRoad>(),
                ComponentType.ReadWrite<GridRoadSidewalk>(),
                ComponentType.ReadWrite<GridRoadDirt>());
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        public void BeginDeferredRoadEcsSync()
        {
            _deferRoadEcsSyncDepth++;
        }

        public void EndDeferredRoadEcsSync(Context context)
        {
            if (_deferRoadEcsSyncDepth <= 0)
                return;

            _deferRoadEcsSyncDepth--;
            if (_deferRoadEcsSyncDepth == 0 && _pendingRoadEcsSync)
            {
                SyncRoadCellsToEcs(context);
                _pendingRoadEcsSync = false;
            }
        }

        public void RequestRoadEcsSync(Context context)
        {
            if (_deferRoadEcsSyncDepth > 0)
            {
                _pendingRoadEcsSync = true;
                return;
            }

            SyncRoadCellsToEcs(context);
        }

        public void SyncRoadCellsToEcs(Context context)
        {
            if (!TryGetRoadBuffers(out var roadBuffers))
                return;

            ClearRoadBuffers(roadBuffers);

            GridConfig grid = roadBuffers.Grid;
            if (context.RoadGridSize <= 0f || grid.CellSize <= 0f || context.RoadTiles == null)
                return;

            foreach (var entry in context.RoadTiles)
            {
                Vector2Int roadCell = entry.Key;
                ForEachRoadWorldFootprintKind(context.FootprintState, roadCell, entry.Value, (worldMin, worldMax, kind) =>
                {
                    GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                    for (int y = minY; y < maxY; y++)
                    {
                        for (int x = minX; x < maxX; x++)
                        {
                            if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                                continue;

                            int index = GridUtils.CellToIndex(new int2(x, y), grid.Width);
                            roadBuffers.Roads[index] = new GridRoad { Value = 1 };
                            if (kind == RoadFootprintKind.Sidewalk)
                                roadBuffers.Sidewalks[index] = new GridRoadSidewalk { Value = 1 };
                            else
                                roadBuffers.DirtRoads[index] = new GridRoadDirt { Value = 1 };
                        }
                    }

                    return true;
                });
            }
        }

        public void ClearRoadDataInEcs()
        {
            if (!TryGetRoadBuffers(out var roadBuffers))
                return;

            ClearRoadBuffers(roadBuffers);
        }

        public void RemoveRuntimeBlockersUnderRoads(Context context, RuntimeGridBlockerPresentationSystemHelper runtimeGridBlockers)
        {
            if (runtimeGridBlockers == null || !TryGetRoadBuffer(out _, out var grid) || context.RoadTiles == null)
                return;

            foreach (var entry in context.RoadTiles)
            {
                Vector2Int roadCell = entry.Key;
                ForEachRoadWorldFootprint(context.FootprintState, roadCell, entry.Value, (worldMin, worldMax) =>
                {
                    GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                    int overlapMinX = int.MaxValue;
                    int overlapMinY = int.MaxValue;
                    int overlapMaxX = int.MinValue;
                    int overlapMaxY = int.MinValue;

                    for (int y = minY; y < maxY; y++)
                    {
                        for (int x = minX; x < maxX; x++)
                        {
                            if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                                continue;

                            overlapMinX = Mathf.Min(overlapMinX, x);
                            overlapMinY = Mathf.Min(overlapMinY, y);
                            overlapMaxX = Mathf.Max(overlapMaxX, x + 1);
                            overlapMaxY = Mathf.Max(overlapMaxY, y + 1);
                        }
                    }

                    if (overlapMaxX > overlapMinX && overlapMaxY > overlapMinY)
                    {
                        runtimeGridBlockers.RemoveBlockersOverlappingFootprint(
                            new Vector2Int(overlapMinX, overlapMinY),
                            new Vector2Int(overlapMaxX - overlapMinX, overlapMaxY - overlapMinY));
                    }

                    return true;
                });
            }
        }

        public static bool HasRoadInFootprint(RoadFootprintState state, GridConfig grid, Vector2Int originCell, Vector2Int footprintCells)
        {
            if (state.RoadTiles == null || state.RoadTiles.Count == 0)
                return false;

            int buildingMinX = originCell.x;
            int buildingMinY = originCell.y;
            int buildingMaxX = originCell.x + footprintCells.x;
            int buildingMaxY = originCell.y + footprintCells.y;

            foreach (var entry in state.RoadTiles)
            {
                bool foundOverlap = false;
                ForEachRoadWorldFootprint(state, entry.Key, entry.Value, (worldMin, worldMax) =>
                {
                    GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                    bool overlaps = false;
                    for (int y = minY; y < maxY && !overlaps; y++)
                    {
                        for (int x = minX; x < maxX; x++)
                        {
                            if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                                continue;

                            if (x < buildingMinX || y < buildingMinY || x >= buildingMaxX || y >= buildingMaxY)
                                continue;

                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                        return true;

                    foundOverlap = true;
                    return false;
                });

                if (foundOverlap)
                    return true;
            }

            return false;
        }

        public static void FillRoadFootprintMask(RoadFootprintState state, GridConfig grid, bool[] occupiedCells)
        {
            if (occupiedCells == null || occupiedCells.Length < grid.Width * grid.Height || state.RoadTiles == null)
                return;

            foreach (var entry in state.RoadTiles)
            {
                ForEachRoadWorldFootprint(state, entry.Key, entry.Value, (worldMin, worldMax) =>
                {
                    GetGridBounds(grid, worldMin, worldMax, out int minX, out int minY, out int maxX, out int maxY);

                    for (int y = minY; y < maxY; y++)
                    {
                        for (int x = minX; x < maxX; x++)
                        {
                            if (!IsGridCellCenterInsideBounds(grid, x, y, worldMin, worldMax))
                                continue;

                            occupiedCells[GridUtils.CellToIndex(new int2(x, y), grid.Width)] = true;
                        }
                    }

                    return true;
                });
            }
        }

        public static void GetRoadWorldFootprint(RoadFootprintState state, Vector2Int roadCell, RoadTileData tile, out Vector3 worldMin, out Vector3 worldMax)
        {
            bool hasBounds = false;
            Bounds combinedBounds = default;

            ForEachRoadWorldFootprint(state, roadCell, tile, (footprintMin, footprintMax) =>
            {
                var footprintBounds = new Bounds((footprintMin + footprintMax) * 0.5f, footprintMax - footprintMin);
                if (!hasBounds)
                {
                    combinedBounds = footprintBounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(footprintMin);
                    combinedBounds.Encapsulate(footprintMax);
                }

                return true;
            });

            if (hasBounds)
            {
                worldMin = combinedBounds.min;
                worldMax = combinedBounds.max;
                return;
            }

            worldMin = state.GridOrigin + new Vector3(roadCell.x * state.RoadGridSize, 0f, roadCell.y * state.RoadGridSize);
            worldMax = worldMin + new Vector3(state.RoadGridSize, 0f, state.RoadGridSize);
        }

        public static void ForEachRoadWorldFootprint(RoadFootprintState state, Vector2Int roadCell, RoadTileData tile, Func<Vector3, Vector3, bool> visitor)
        {
            ForEachRoadWorldFootprintKind(state, roadCell, tile, (worldMin, worldMax, _) => visitor(worldMin, worldMax));
        }

        public static void ForEachRoadWorldFootprintKind(
            RoadFootprintState state,
            Vector2Int roadCell,
            RoadTileData tile,
            Func<Vector3, Vector3, RoadFootprintKind, bool> visitor)
        {
            if (state.SpecialRoadObjects != null &&
                state.SpecialRoadObjects.TryGetValue(roadCell, out var specialRoadObject) &&
                specialRoadObject != null)
            {
                MeshFilter[] meshFilters = specialRoadObject.GetComponentsInChildren<MeshFilter>(true);
                bool foundSpecialBounds = false;
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    MeshFilter meshFilter = meshFilters[i];
                    if (meshFilter.sharedMesh == null)
                        continue;
                    if (!TryGetFootprintKind(
                            meshFilter.transform,
                            tile.Type == RoadVisualType.Autobahn || tile.Type == RoadVisualType.AutobahnConnect,
                            out RoadFootprintKind footprintKind))
                        continue;

                    Bounds worldBounds = TransformBounds(meshFilter.sharedMesh.bounds, meshFilter.transform.localToWorldMatrix);
                    foundSpecialBounds = true;
                    if (!visitor(worldBounds.min, worldBounds.max, footprintKind))
                        return;
                }

                if (foundSpecialBounds)
                    return;
            }

            if (state.VisualData != null &&
                state.VisualData.TryGetValue(tile.Type, out var visualData) &&
                visualData.FootprintBounds != null &&
                visualData.FootprintBounds.Count > 0)
            {
                Vector3 basePosition = GetPlacementPosition(state, roadCell, tile.Rotation, tile.Scale);
                for (int boundsIndex = 0; boundsIndex < visualData.FootprintBounds.Count; boundsIndex++)
                {
                    RoadFootprintBoundsData footprintData = visualData.FootprintBounds[boundsIndex];
                    if (!VisitTransformedBounds(
                            footprintData.Bounds,
                            basePosition,
                            tile.Rotation,
                            tile.Scale,
                            (worldMin, worldMax) => visitor(worldMin, worldMax, footprintData.Kind)))
                    {
                        return;
                    }
                }

                return;
            }

            if (state.VisualData != null &&
                state.VisualData.TryGetValue(tile.Type, out var fallbackVisualData) &&
                fallbackVisualData.Mesh != null)
            {
                Vector3 basePosition = GetPlacementPosition(state, roadCell, tile.Rotation, tile.Scale);
                VisitTransformedBounds(
                    fallbackVisualData.Mesh.bounds,
                    basePosition,
                    tile.Rotation,
                    tile.Scale,
                    (worldMin, worldMax) => visitor(worldMin, worldMax, RoadFootprintKind.Dirt));
                return;
            }

            Vector3 fallbackMin = state.GridOrigin + new Vector3(roadCell.x * state.RoadGridSize, 0f, roadCell.y * state.RoadGridSize);
            Vector3 fallbackMax = fallbackMin + new Vector3(state.RoadGridSize, 0f, state.RoadGridSize);
            visitor(fallbackMin, fallbackMax, RoadFootprintKind.Dirt);
        }

        public static bool ShouldReserveRoadRenderer(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return IsReserveMarkerName(name) ||
                   name.IndexOf("sm_env_dirt", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf("sm_env_sidewalk", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryGetFootprintKind(Transform transform, bool typeOverride, out RoadFootprintKind kind)
        {
            Transform current = transform;
            while (current != null)
            {
                if (IsSidewalkMarkerName(current.name))
                {
                    kind = RoadFootprintKind.Sidewalk;
                    return true;
                }

                if (IsDirtMarkerName(current.name))
                {
                    kind = RoadFootprintKind.Dirt;
                    return true;
                }

                if (!typeOverride && ShouldReserveRoadRenderer(current.name))
                {
                    kind = current.name.IndexOf("sidewalk", StringComparison.OrdinalIgnoreCase) >= 0
                        ? RoadFootprintKind.Sidewalk
                        : RoadFootprintKind.Dirt;
                    return true;
                }

                current = current.parent;
            }

            kind = RoadFootprintKind.Dirt;
            return false;
        }

        public static bool IsGridCellCenterInsideBounds(GridConfig grid, int x, int y, Vector3 worldMin, Vector3 worldMax)
        {
            Vector3 center = (Vector3)grid.Origin + new Vector3((x + 0.5f) * grid.CellSize, 0f, (y + 0.5f) * grid.CellSize);
            return center.x >= worldMin.x && center.x < worldMax.x &&
                   center.z >= worldMin.z && center.z < worldMax.z;
        }

        public static Bounds TransformBounds(Bounds localBounds, Matrix4x4 matrix)
        {
            Vector3[] corners =
            {
                new(localBounds.min.x, localBounds.min.y, localBounds.min.z),
                new(localBounds.min.x, localBounds.min.y, localBounds.max.z),
                new(localBounds.min.x, localBounds.max.y, localBounds.min.z),
                new(localBounds.min.x, localBounds.max.y, localBounds.max.z),
                new(localBounds.max.x, localBounds.min.y, localBounds.min.z),
                new(localBounds.max.x, localBounds.min.y, localBounds.max.z),
                new(localBounds.max.x, localBounds.max.y, localBounds.min.z),
                new(localBounds.max.x, localBounds.max.y, localBounds.max.z)
            };

            Vector3 min = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 worldCorner = matrix.MultiplyPoint3x4(corners[i]);
                min = Vector3.Min(min, worldCorner);
                max = Vector3.Max(max, worldCorner);
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        public bool TryGetGridData(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockerData)
        {
            gridEntity = Entity.Null;
            grid = default;
            roads = default;
            blockerData = default;

            if (!TryGetEntityManager(out EntityManager em))
                return false;

            if (_gridDataQuery.IsEmptyIgnoreFilter)
                return false;

            gridEntity = _gridDataQuery.GetSingletonEntity();
            grid = em.GetComponentData<GridConfig>(gridEntity);
            roads = em.GetBuffer<GridRoad>(gridEntity);
            blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            return true;
        }

        public bool TryGetGridConfig(out GridConfig grid)
        {
            grid = default;

            if (!TryGetEntityManager(out EntityManager entityManager))
                return false;

            if (_gridDataQuery.IsEmptyIgnoreFilter)
                return false;

            grid = entityManager.GetComponentData<GridConfig>(_gridDataQuery.GetSingletonEntity());
            return true;
        }

        private bool TryGetRoadBuffer(out DynamicBuffer<GridRoad> roads, out GridConfig grid)
        {
            roads = default;
            grid = default;

            if (!TryGetEntityManager(out EntityManager entityManager))
                return false;

            if (_roadBufferQuery.IsEmptyIgnoreFilter)
                return false;

            Entity gridEntity = _roadBufferQuery.GetSingletonEntity();
            grid = entityManager.GetComponentData<GridConfig>(gridEntity);
            roads = entityManager.GetBuffer<GridRoad>(gridEntity);
            return true;
        }

        private bool TryGetRoadBuffers(out RoadBuffersData roadBuffers)
        {
            roadBuffers = default;

            if (!TryGetEntityManager(out EntityManager entityManager))
                return false;

            if (_roadBuffersQuery.IsEmptyIgnoreFilter)
                return false;

            Entity gridEntity = _roadBuffersQuery.GetSingletonEntity();
            roadBuffers = new RoadBuffersData(
                entityManager.GetBuffer<GridRoad>(gridEntity),
                entityManager.GetBuffer<GridRoadSidewalk>(gridEntity),
                entityManager.GetBuffer<GridRoadDirt>(gridEntity),
                entityManager.GetComponentData<GridConfig>(gridEntity));
            return true;
        }

        private static void ClearRoadBuffers(RoadBuffersData roadBuffers)
        {
            for (int i = 0; i < roadBuffers.Roads.Length; i++)
            {
                roadBuffers.Roads[i] = new GridRoad { Value = 0 };
                roadBuffers.Sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                roadBuffers.DirtRoads[i] = new GridRoadDirt { Value = 0 };
            }
        }

        private static bool VisitTransformedBounds(
            Bounds localBounds,
            Vector3 basePosition,
            Quaternion rotation,
            Vector3 scale,
            Func<Vector3, Vector3, bool> visitor)
        {
            Vector3[] corners =
            {
                new(localBounds.min.x, localBounds.min.y, localBounds.min.z),
                new(localBounds.min.x, localBounds.min.y, localBounds.max.z),
                new(localBounds.min.x, localBounds.max.y, localBounds.min.z),
                new(localBounds.min.x, localBounds.max.y, localBounds.max.z),
                new(localBounds.max.x, localBounds.min.y, localBounds.min.z),
                new(localBounds.max.x, localBounds.min.y, localBounds.max.z),
                new(localBounds.max.x, localBounds.max.y, localBounds.min.z),
                new(localBounds.max.x, localBounds.max.y, localBounds.max.z)
            };

            Vector3 worldMin = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 worldMax = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 worldCorner = basePosition + rotation * Vector3.Scale(corners[i], scale);
                worldMin = Vector3.Min(worldMin, worldCorner);
                worldMax = Vector3.Max(worldMax, worldCorner);
            }

            return visitor(worldMin, worldMax);
        }

        private static void GetGridBounds(GridConfig grid, Vector3 worldMin, Vector3 worldMax, out int minX, out int minY, out int maxX, out int maxY)
        {
            float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
            float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

            minX = Mathf.Clamp(Mathf.FloorToInt(localMin.x / grid.CellSize), 0, grid.Width);
            minY = Mathf.Clamp(Mathf.FloorToInt(localMin.z / grid.CellSize), 0, grid.Height);
            maxX = Mathf.Clamp(Mathf.CeilToInt(localMax.x / grid.CellSize), 0, grid.Width);
            maxY = Mathf.Clamp(Mathf.CeilToInt(localMax.z / grid.CellSize), 0, grid.Height);
        }

        private static Vector3 GetPlacementPosition(RoadFootprintState state, Vector2Int cell, Quaternion rotation, Vector3 scale)
        {
            Vector3 basePosition = state.GridOrigin + new Vector3(cell.x * state.RoadGridSize, state.BuildPlaneY, cell.y * state.RoadGridSize);
            Vector3[] corners =
            {
                new(0f, 0f, 0f),
                new(state.RoadGridSize, 0f, 0f),
                new(0f, 0f, state.RoadGridSize),
                new(state.RoadGridSize, 0f, state.RoadGridSize)
            };

            float minX = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 transformed = rotation * Vector3.Scale(corners[i], scale);
                if (transformed.x < minX)
                    minX = transformed.x;
                if (transformed.z < minZ)
                    minZ = transformed.z;
            }

            return basePosition - new Vector3(minX, 0f, minZ);
        }

        private static bool IsReserveMarkerName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return IsDirtMarkerName(name) || IsSidewalkMarkerName(name);
        }

        private static bool IsDirtMarkerName(string name)
        {
            return string.Equals(name, "Dirt", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSidewalkMarkerName(string name)
        {
            return string.Equals(name, "Sidewalk", StringComparison.OrdinalIgnoreCase);
        }

        private bool TryGetEntityManager(out EntityManager entityManager)
        {
            entityManager = default;
            World world = World;
            if (world == null || !world.IsCreated)
                return false;

            entityManager = EntityManager;
            return true;
        }
    }
}
