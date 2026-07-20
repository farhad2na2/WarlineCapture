using System;
using System.Collections.Generic;
using System.IO;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal static class DenseMiddleEasternCityEditModeBuilder
    {
        internal readonly struct Result
        {
            public readonly int RoadTiles;
            public readonly int RoadChunks;
            public readonly int Buildings;
            public readonly int Parks;
            public readonly int AuthoredCoreRenderers;

            public Result(int roadTiles, int roadChunks, int buildings, int parks, int authoredCoreRenderers)
            {
                RoadTiles = roadTiles;
                RoadChunks = roadChunks;
                Buildings = buildings;
                Parks = parks;
                AuthoredCoreRenderers = authoredCoreRenderers;
            }
        }

        private readonly struct PrefabFootprint
        {
            public readonly GameObject Prefab;
            public readonly float Width;
            public readonly float Depth;
            public readonly float VisualScale;

            public PrefabFootprint(GameObject prefab, float width, float depth, float visualScale)
            {
                Prefab = prefab;
                VisualScale = Mathf.Max(0.01f, visualScale);
                Width = Mathf.Max(3f, width * VisualScale);
                Depth = Mathf.Max(3f, depth * VisualScale);
            }
        }

        private sealed class SurfacePlacementContext : IDisposable
        {
            private readonly BlobAssetReference<MapSurfaceBlob> _surfaceBlob;
            private readonly MapSurfaceSampler _sampler = new();
            private readonly MapSurfaceSampler.Context _samplerContext;

            public MapSurfaceComponent Surface { get; }

            private SurfacePlacementContext(
                BlobAssetReference<MapSurfaceBlob> surfaceBlob,
                MapSurfaceComponent surface)
            {
                _surfaceBlob = surfaceBlob;
                Surface = surface;
                _samplerContext = new MapSurfaceSampler.Context(surface);
            }

            public static SurfacePlacementContext Create()
            {
                MapSurfaceAuthoring authoring = UnityEngine.Object.FindAnyObjectByType<MapSurfaceAuthoring>(
                    FindObjectsInactive.Include);
                if (authoring == null || authoring.BakedSurfaceData == null)
                    return null;
                if (!authoring.BakedSurfaceData.TryCreateRuntimeBlobAsset(
                        Allocator.Persistent,
                        out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
                {
                    return null;
                }

                ref MapSurfaceBlob blob = ref surfaceBlob.Value;
                var surface = new MapSurfaceComponent
                {
                    SurfaceBlob = surfaceBlob,
                    GridOrigin = blob.GridOrigin,
                    CellSize = blob.CellSize,
                    Dimensions = blob.Dimensions,
                    HasSurfaceData = 1
                };
                return new SurfacePlacementContext(surfaceBlob, surface);
            }

            public float SampleHeight(Vector3 worldPosition)
            {
                return _sampler.TrySampleBilinearHeight(_samplerContext, worldPosition, out float height)
                    ? height
                    : worldPosition.y;
            }

            public bool TryEvaluatePatch(
                Vector2 center,
                float halfWidth,
                float halfDepth,
                out SurfacePatchEvaluation evaluation)
            {
                evaluation = default;
                float insetWidth = Mathf.Max(0.1f, halfWidth * 0.94f);
                float insetDepth = Mathf.Max(0.1f, halfDepth * 0.94f);
                Vector2[] offsets =
                {
                    Vector2.zero,
                    new(-insetWidth, -insetDepth),
                    new(insetWidth, -insetDepth),
                    new(-insetWidth, insetDepth),
                    new(insetWidth, insetDepth),
                    new(-insetWidth, 0f),
                    new(insetWidth, 0f),
                    new(0f, -insetDepth),
                    new(0f, insetDepth)
                };

                float minimum = float.PositiveInfinity;
                float maximum = float.NegativeInfinity;
                float total = 0f;
                float maximumSlope = 0f;
                int samples = 0;
                for (int index = 0; index < offsets.Length; index++)
                {
                    Vector2 sample = center + offsets[index];
                    var world = new Vector3(sample.x, 0f, sample.y);
                    if (!_sampler.TrySampleBilinearHeight(_samplerContext, world, out float height))
                        continue;

                    minimum = Mathf.Min(minimum, height);
                    maximum = Mathf.Max(maximum, height);
                    total += height;
                    samples++;
                    if (_sampler.TrySampleBilinearNormal(_samplerContext, world, out Unity.Mathematics.float3 normal))
                    {
                        float slope = Vector3.Angle(
                            Vector3.up,
                            new Vector3(normal.x, normal.y, normal.z));
                        maximumSlope = Mathf.Max(maximumSlope, slope);
                    }
                }

                if (samples == 0)
                    return false;

                evaluation = new SurfacePatchEvaluation(
                    minimum,
                    maximum,
                    total / samples,
                    maximumSlope,
                    samples);
                return true;
            }

            public void Dispose()
            {
                if (_surfaceBlob.IsCreated)
                    _surfaceBlob.Dispose();
            }
        }

        private readonly struct SurfacePatchEvaluation
        {
            public readonly float MinimumHeight;
            public readonly float MaximumHeight;
            public readonly float AverageHeight;
            public readonly float MaximumSlopeDegrees;
            public readonly int SampleCount;

            public SurfacePatchEvaluation(
                float minimumHeight,
                float maximumHeight,
                float averageHeight,
                float maximumSlopeDegrees,
                int sampleCount)
            {
                MinimumHeight = minimumHeight;
                MaximumHeight = maximumHeight;
                AverageHeight = averageHeight;
                MaximumSlopeDegrees = maximumSlopeDegrees;
                SampleCount = sampleCount;
            }

            public float HeightDelta => MaximumHeight - MinimumHeight;
        }

        private enum TerrainClassification
        {
            Flat,
            Terraceable,
            RoadOnly,
            Unsuitable
        }

        private sealed class TerrainViabilityMap
        {
            private readonly SurfacePlacementContext _surface;
            private readonly Vector3 _mapOrigin;
            private readonly CityFootprint _cityFootprint;
            private readonly float _authoredGradeElevation;
            private readonly Dictionary<Vector2Int, SurfacePatchEvaluation> _roadPatches = new();

            public TerrainViabilityMap(
                SurfacePlacementContext surface,
                Vector3 mapOrigin,
                CityFootprint cityFootprint,
                float authoredGradeElevation)
            {
                _surface = surface;
                _mapOrigin = mapOrigin;
                _cityFootprint = cityFootprint;
                _authoredGradeElevation = authoredGradeElevation;
            }

            public bool TryGetRoadPatch(Vector2Int cell, out SurfacePatchEvaluation patch)
            {
                if (_roadPatches.TryGetValue(cell, out patch))
                    return true;

                Vector2 center = RoadCellWorldCenter(cell, _mapOrigin);
                if (!_cityFootprint.IsAreaClear(
                        center,
                        RoadGridSize * 1.5f,
                        RoadGridSize * 1.5f))
                {
                    return false;
                }

                if (_cityFootprint.Contains(center))
                {
                    patch = CreateAuthoredGradePatch();
                    _roadPatches.Add(cell, patch);
                    return true;
                }

                if (_surface == null ||
                    !_surface.TryEvaluatePatch(center, RoadGridSize * 0.5f, RoadGridSize * 0.5f, out patch))
                {
                    return false;
                }

                _roadPatches.Add(cell, patch);
                return true;
            }

            public bool TryEvaluateBuilding(
                Vector2 center,
                float width,
                float depth,
                out SurfacePatchEvaluation patch)
            {
                patch = default;
                if (!_cityFootprint.IsAreaClear(center, width * 0.5f, depth * 0.5f))
                    return false;

                if (_cityFootprint.Contains(center))
                {
                    patch = CreateAuthoredGradePatch();
                    return true;
                }

                return _surface != null &&
                       _surface.TryEvaluatePatch(center, width * 0.5f, depth * 0.5f, out patch);
            }

            private SurfacePatchEvaluation CreateAuthoredGradePatch() =>
                new(
                    _authoredGradeElevation,
                    _authoredGradeElevation,
                    _authoredGradeElevation,
                    0f,
                    9);

            public TerrainClassification ClassifyRoad(Vector2Int cell)
            {
                return TryGetRoadPatch(cell, out SurfacePatchEvaluation patch)
                    ? Classify(patch)
                    : TerrainClassification.Unsuitable;
            }

            public static TerrainClassification Classify(SurfacePatchEvaluation patch)
            {
                if (patch.HeightDelta <= 0.5f && patch.MaximumSlopeDegrees <= 7f)
                    return TerrainClassification.Flat;
                if (patch.HeightDelta <= 2.5f && patch.MaximumSlopeDegrees <= 22f)
                    return TerrainClassification.Terraceable;
                if (patch.HeightDelta <= 5.5f && patch.MaximumSlopeDegrees <= 35f)
                    return TerrainClassification.RoadOnly;
                return TerrainClassification.Unsuitable;
            }

            public bool CanPlaceRoad(Vector2Int cell) =>
                ClassifyRoad(cell) != TerrainClassification.Unsuitable;

            public bool CanPlaceBuilding(SurfacePatchEvaluation patch) =>
                Classify(patch) is TerrainClassification.Flat or TerrainClassification.Terraceable;

            public void LogAudit(CityFootprint footprint, int maximumColumn, int maximumRow)
            {
                int flat = 0;
                int terraceable = 0;
                int roadOnly = 0;
                int unsuitable = 0;
                float maximumDelta = 0f;
                for (int row = 1; row < maximumRow; row++)
                {
                    for (int column = 1; column < maximumColumn; column++)
                    {
                        var cell = new Vector2Int(column, row);
                        if (!footprint.Contains(RoadCellWorldCenter(cell, _mapOrigin)))
                            continue;

                        if (TryGetRoadPatch(cell, out SurfacePatchEvaluation patch))
                            maximumDelta = Mathf.Max(maximumDelta, patch.HeightDelta);
                        switch (ClassifyRoad(cell))
                        {
                            case TerrainClassification.Flat:
                                flat++;
                                break;
                            case TerrainClassification.Terraceable:
                                terraceable++;
                                break;
                            case TerrainClassification.RoadOnly:
                                roadOnly++;
                                break;
                            default:
                                unsuitable++;
                                break;
                        }
                    }
                }

                Debug.Log(
                    $"[DenseCityTerrainAudit] flat={flat} terraceable={terraceable} " +
                    $"roadOnly={roadOnly} unsuitable={unsuitable} maxPatchDelta={maximumDelta:0.00}m");
            }
        }

        private sealed class RoadElevationPlan
        {
            private const float MaximumRoadStep = 0.35f;
            private static readonly Vector2Int[] ForwardNeighbors =
            {
                Vector2Int.right,
                Vector2Int.up
            };

            private readonly Dictionary<Vector2Int, float> _elevations = new();

            public float GetElevation(Vector2Int cell, float fallback) =>
                _elevations.TryGetValue(cell, out float elevation) ? elevation : fallback;

            public static RoadElevationPlan Build(
                RoadNetworkCompositionSystemHelper network,
                TerrainViabilityMap terrainMap)
            {
                var plan = new RoadElevationPlan();
                foreach (Vector2Int cell in network.StrokeIdsByCell.Keys)
                {
                    if (terrainMap.TryGetRoadPatch(cell, out SurfacePatchEvaluation patch))
                        plan._elevations[cell] = patch.MaximumHeight + 0.08f;
                }

                for (int iteration = 0; iteration < 32; iteration++)
                {
                    bool changed = false;
                    foreach (Vector2Int cell in network.StrokeIdsByCell.Keys)
                    {
                        if (!plan._elevations.TryGetValue(cell, out float current))
                            continue;

                        for (int directionIndex = 0; directionIndex < ForwardNeighbors.Length; directionIndex++)
                        {
                            Vector2Int neighbor = cell + ForwardNeighbors[directionIndex];
                            if (!network.StrokeIdsByCell.ContainsKey(neighbor) ||
                                !plan._elevations.TryGetValue(neighbor, out float neighborHeight))
                            {
                                continue;
                            }

                            if (current > neighborHeight + MaximumRoadStep)
                            {
                                plan._elevations[neighbor] = current - MaximumRoadStep;
                                changed = true;
                            }
                            else if (neighborHeight > current + MaximumRoadStep)
                            {
                                current = neighborHeight - MaximumRoadStep;
                                plan._elevations[cell] = current;
                                changed = true;
                            }
                        }
                    }

                    if (!changed)
                        break;
                }

                return plan;
            }
        }

        private sealed class BuildingPalette
        {
            public readonly List<PrefabFootprint> Houses = new();
            public readonly List<PrefabFootprint> Shops = new();
            public readonly List<PrefabFootprint> Other = new();
            public readonly List<PrefabFootprint> Park = new();
            public readonly List<PrefabFootprint> Fountains = new();
        }

        private sealed class CityFootprint
        {
            private readonly Vector2 _center;
            private readonly float _radiusX;
            private readonly float _radiusZ;
            private readonly float _phase;
            private readonly ProtectedAreaMap _protectedAreas;

            public CityFootprint(
                Vector2 center,
                float radiusX,
                float radiusZ,
                uint seed,
                ProtectedAreaMap protectedAreas)
            {
                _center = center;
                _radiusX = Mathf.Max(1f, radiusX);
                _radiusZ = Mathf.Max(1f, radiusZ);
                _phase = ((seed % 997u) / 997f) * Mathf.PI * 2f;
                _protectedAreas = protectedAreas;
            }

            public float NormalizedDistance(Vector2 worldPosition)
            {
                float x = (worldPosition.x - _center.x) / _radiusX;
                float z = (worldPosition.y - _center.y) / _radiusZ;
                float angle = Mathf.Atan2(z, x);
                float shapedBoundary = 0.9f +
                                       Mathf.Sin(angle * 3f + _phase) * 0.09f +
                                       Mathf.Sin(angle * 7f - _phase * 0.6f) * 0.055f +
                                       Mathf.Cos(angle * 11f + _phase * 0.35f) * 0.025f;
                return Mathf.Sqrt(x * x + z * z) / Mathf.Max(0.72f, shapedBoundary);
            }

            public bool Contains(Vector2 worldPosition, float margin = 0f) =>
                NormalizedDistance(worldPosition) <= 1f - margin &&
                (_protectedAreas == null || !_protectedAreas.Intersects(worldPosition));

            public bool IsAreaClear(Vector2 center, float halfWidth, float halfDepth) =>
                _protectedAreas == null || !_protectedAreas.Intersects(center, halfWidth, halfDepth);

            public bool IsAreaClear(Rect area) =>
                IsAreaClear(area.center, area.width * 0.5f, area.height * 0.5f);
        }

        private sealed class ProtectedAreaMap
        {
            private const float CellSize = RoadGridSize;
            private readonly HashSet<Vector2Int> _cells = new();
            private readonly List<Rect> _bounds = new();
            private readonly Dictionary<Vector2Int, List<int>> _boundsByCell = new();
            private readonly Dictionary<string, int> _rendererCounts = new(StringComparer.Ordinal);

            public int CellCount => _cells.Count;

            public void AddRenderer(string category, Renderer renderer, float margin)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    return;

                Bounds bounds = renderer.bounds;
                AddBounds(bounds, margin);
                _rendererCounts.TryGetValue(category, out int count);
                _rendererCounts[category] = count + 1;
            }

            public bool Intersects(Vector2 center, float halfWidth = 0f, float halfDepth = 0f)
            {
                int minimumX = Mathf.FloorToInt((center.x - halfWidth) / CellSize);
                int maximumX = Mathf.FloorToInt((center.x + halfWidth) / CellSize);
                int minimumZ = Mathf.FloorToInt((center.y - halfDepth) / CellSize);
                int maximumZ = Mathf.FloorToInt((center.y + halfDepth) / CellSize);
                for (int x = minimumX; x <= maximumX; x++)
                {
                    for (int z = minimumZ; z <= maximumZ; z++)
                    {
                        if (_cells.Contains(new Vector2Int(x, z)))
                            return true;
                    }
                }

                return false;
            }

            public bool Intersects(Bounds bounds)
            {
                var candidate = Rect.MinMaxRect(
                    bounds.min.x,
                    bounds.min.z,
                    bounds.max.x,
                    bounds.max.z);
                int minimumX = Mathf.FloorToInt(candidate.xMin / CellSize);
                int maximumX = Mathf.FloorToInt(candidate.xMax / CellSize);
                int minimumZ = Mathf.FloorToInt(candidate.yMin / CellSize);
                int maximumZ = Mathf.FloorToInt(candidate.yMax / CellSize);
                var visited = new HashSet<int>();
                for (int x = minimumX; x <= maximumX; x++)
                {
                    for (int z = minimumZ; z <= maximumZ; z++)
                    {
                        if (!_boundsByCell.TryGetValue(new Vector2Int(x, z), out List<int> indices))
                            continue;
                        for (int index = 0; index < indices.Count; index++)
                        {
                            int boundsIndex = indices[index];
                            if (visited.Add(boundsIndex) && _bounds[boundsIndex].Overlaps(candidate, true))
                                return true;
                        }
                    }
                }

                return false;
            }

            public string Describe()
            {
                var parts = new List<string>(_rendererCounts.Count);
                foreach (KeyValuePair<string, int> pair in _rendererCounts)
                    parts.Add($"{pair.Key}:{pair.Value}");
                parts.Sort(StringComparer.Ordinal);
                return string.Join(",", parts);
            }

            private void AddBounds(Bounds bounds, float margin)
            {
                int minimumX = Mathf.FloorToInt((bounds.min.x - margin) / CellSize);
                int maximumX = Mathf.FloorToInt((bounds.max.x + margin) / CellSize);
                int minimumZ = Mathf.FloorToInt((bounds.min.z - margin) / CellSize);
                int maximumZ = Mathf.FloorToInt((bounds.max.z + margin) / CellSize);
                var protectedBounds = Rect.MinMaxRect(
                    bounds.min.x - margin,
                    bounds.min.z - margin,
                    bounds.max.x + margin,
                    bounds.max.z + margin);
                int boundsIndex = _bounds.Count;
                _bounds.Add(protectedBounds);
                for (int x = minimumX; x <= maximumX; x++)
                {
                    for (int z = minimumZ; z <= maximumZ; z++)
                    {
                        var cell = new Vector2Int(x, z);
                        _cells.Add(new Vector2Int(x, z));
                        if (!_boundsByCell.TryGetValue(cell, out List<int> indices))
                        {
                            indices = new List<int>();
                            _boundsByCell.Add(cell, indices);
                        }
                        indices.Add(boundsIndex);
                    }
                }
            }
        }

        private sealed class BuildingPlacementContext
        {
            private const float BuildingClearance = 0.35f;
            private const float RoadVisualHalfExtent = 9f;
            private const float OccupancyCellSize = 12f;

            private readonly HashSet<Vector2Int> _roadCells;
            private readonly Vector3 _roadOrigin;
            private readonly List<Rect> _occupiedBounds = new();
            private readonly Dictionary<Vector2Int, List<int>> _occupiedByCell = new();

            public int ReservedCount => _occupiedBounds.Count;

            public BuildingPlacementContext(HashSet<Vector2Int> roadCells, Vector3 roadOrigin)
            {
                _roadCells = roadCells ?? new HashSet<Vector2Int>();
                _roadOrigin = roadOrigin;
            }

            public bool CanPlace(PrefabFootprint info, float rotationDegrees, Vector2 center)
            {
                Rect candidate = CreateFootprint(info, rotationDegrees, center, BuildingClearance);
                return !OverlapsOccupied(candidate) && !OverlapsRoad(candidate);
            }

            public bool TryReserve(Bounds worldBounds)
            {
                Rect candidate = Rect.MinMaxRect(
                    worldBounds.min.x - BuildingClearance,
                    worldBounds.min.z - BuildingClearance,
                    worldBounds.max.x + BuildingClearance,
                    worldBounds.max.z + BuildingClearance);
                if (OverlapsOccupied(candidate) || OverlapsRoad(candidate))
                    return false;

                int boundsIndex = _occupiedBounds.Count;
                _occupiedBounds.Add(candidate);
                VisitCells(candidate, cell =>
                {
                    if (!_occupiedByCell.TryGetValue(cell, out List<int> indices))
                    {
                        indices = new List<int>();
                        _occupiedByCell.Add(cell, indices);
                    }
                    indices.Add(boundsIndex);
                });
                return true;
            }

            private bool OverlapsOccupied(Rect candidate)
            {
                bool overlaps = false;
                var visited = new HashSet<int>();
                VisitCells(candidate, cell =>
                {
                    if (overlaps || !_occupiedByCell.TryGetValue(cell, out List<int> indices))
                        return;
                    for (int index = 0; index < indices.Count; index++)
                    {
                        int boundsIndex = indices[index];
                        if (visited.Add(boundsIndex) &&
                            _occupiedBounds[boundsIndex].Overlaps(candidate, true))
                        {
                            overlaps = true;
                            return;
                        }
                    }
                });
                return overlaps;
            }

            private bool OverlapsRoad(Rect candidate)
            {
                int minimumColumn = Mathf.FloorToInt(
                    (candidate.xMin - RoadVisualHalfExtent - _roadOrigin.x) / RoadGridSize);
                int maximumColumn = Mathf.CeilToInt(
                    (candidate.xMax + RoadVisualHalfExtent - _roadOrigin.x) / RoadGridSize);
                int minimumRow = Mathf.FloorToInt(
                    (candidate.yMin - RoadVisualHalfExtent - _roadOrigin.z) / RoadGridSize);
                int maximumRow = Mathf.CeilToInt(
                    (candidate.yMax + RoadVisualHalfExtent - _roadOrigin.z) / RoadGridSize);
                for (int column = minimumColumn; column <= maximumColumn; column++)
                {
                    for (int row = minimumRow; row <= maximumRow; row++)
                    {
                        if (!_roadCells.Contains(new Vector2Int(column, row)))
                            continue;

                        float roadX = _roadOrigin.x + column * RoadGridSize;
                        float roadZ = _roadOrigin.z + row * RoadGridSize;
                        var roadBounds = new Rect(
                            roadX - RoadVisualHalfExtent,
                            roadZ - RoadVisualHalfExtent,
                            RoadVisualHalfExtent * 2f,
                            RoadVisualHalfExtent * 2f);
                        if (roadBounds.Overlaps(candidate, true))
                            return true;
                    }
                }

                return false;
            }

            private static Rect CreateFootprint(
                PrefabFootprint info,
                float rotationDegrees,
                Vector2 center,
                float clearance)
            {
                bool quarterTurn = Mathf.RoundToInt(rotationDegrees / 90f) % 2 != 0;
                float halfWidth = (quarterTurn ? info.Depth : info.Width) * 0.5f + clearance;
                float halfDepth = (quarterTurn ? info.Width : info.Depth) * 0.5f + clearance;
                return Rect.MinMaxRect(
                    center.x - halfWidth,
                    center.y - halfDepth,
                    center.x + halfWidth,
                    center.y + halfDepth);
            }

            private static void VisitCells(Rect bounds, Action<Vector2Int> visitor)
            {
                int minimumX = Mathf.FloorToInt(bounds.xMin / OccupancyCellSize);
                int maximumX = Mathf.FloorToInt(bounds.xMax / OccupancyCellSize);
                int minimumZ = Mathf.FloorToInt(bounds.yMin / OccupancyCellSize);
                int maximumZ = Mathf.FloorToInt(bounds.yMax / OccupancyCellSize);
                for (int x = minimumX; x <= maximumX; x++)
                {
                    for (int z = minimumZ; z <= maximumZ; z++)
                        visitor(new Vector2Int(x, z));
                }
            }
        }

        private enum DistrictZone
        {
            Civic,
            InnerCity,
            Residential,
            Fringe
        }

        private const float RoadGridSize = 10f;
        private const float WestCityExpansion = 512f;
        private const float SouthCityExpansion = 128f;
        private const float NorthCityExpansion = 128f;
        private const int RoadChunkSize = 16;
        private const float BuildingVisualScale = 0.82f;
        private const float SidewalkBuildingRoadSetback = 2.75f;
        private const float DirtBuildingRoadSetback = 0.45f;
        private const string RoadBuildConfigGuid = "b2010000000000000000000000000003";
        private const string DirtRoadEndGuid = "16612f70af20e42ab9a6a65e4043907f";
        private const string DirtRoadStraightGuid = "ad3b72115e0cd44f099f64f43f090d1c";
        private const string DirtRoadCornerGuid = "a0db9e22483eb4659af332beb21c89b3";
        private const string DirtRoadTIntersectionGuid = "85399051089764c64a8b3d3e01454044";
        private const string DirtRoadIntersectionGuid = "5c9b518346d37449587a4d7a10a7a470";
        private const string AsphaltRoadEndGuid = "fa0a16026cf90474c84e43de668567d7";
        private const string AsphaltRoadStraightGuid = "095cd66c53a054737955d9773c3d4060";
        private const string AsphaltRoadCornerGuid = "8a34e9514dfe04fd7a308e8dded1b154";
        private const string AsphaltRoadTIntersectionGuid = "65241ad8beab543e589a7e3c7334b214";
        private const string AsphaltRoadIntersectionGuid = "b4e31794b94814524a6f32f65cdd82d4";
        private const string GroundVariationMaterialGuid = "e581a57183ed647799810867dc55e965";
        private static readonly string[] NaturalGroundPrefabGuids =
        {
            "87f34f6fda934c743bede9cef5dd324a",
            "a9eb6b5686ff9db48a4766e125fd75d3",
            "75631c8e76821f2479c1c06a4709a9b7",
            "5865dda7cc6f9e84495a47e8f9811563"
        };

        private static Material _groundVariationMaterial;
        private static GameObject[] _naturalGroundPrefabs;
        private const string GeneratedRoadMeshFolder =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/dense_city_roads";

        private static readonly string[] ParkPrefabPaths =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_02.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Round_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Small_01.prefab",
            "Assets/Game/Prefabs/Environment/City/SM_Bld_Fountain_01.prefab",
            "Assets/Game/Prefabs/Environment/City/SM_Bld_Fountain_02.prefab"
        };

        private static readonly string[] RooftopWaterTankPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_WaterTank_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_WaterTank_03.prefab"
        };

        private static readonly string[] StreetPropPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Box_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_BarrelPile_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Sack_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Sack_Large_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Wood_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Cart_Stall_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Basket_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Pallet_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Rubbish_Bag_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Generator_Small_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Chair_01.prefab"
        };

        private static readonly string[] DenseTreePrefabPaths =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Large_03.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Small_01.prefab"
        };

        private static readonly string[] UrbanRockPrefabPaths =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_02.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_04.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_Flat_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Rock_Flat_02.prefab"
        };

        private const string CourtyardWallPrefabPath =
            "Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_02.prefab";

        private const string CourtyardPillarPrefabPath =
            "Assets/Game/Prefabs/Environment/CityWalls/SM_Bld_Village_Wall_Pillar_01.prefab";

        private const string CourtyardWellPrefabPath =
            "Assets/Game/Prefabs/Environment/CityDecorations/SM_Bld_Village_Well_01.prefab";

        private const string CourtyardBushPrefabPath =
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Bush_03.prefab";

        private const string PowerPolePrefabPath =
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Powerpole_01.prefab";

        private const string PowerLinePrefabPath =
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Powerline_02.prefab";

        private const string StreetLightPrefabPath =
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Road_Lights_01.prefab";

        private const string GrassPrefabPath =
            "Assets/Game/Prefabs/Environment/Decorations/SM_Env_Grass_04.prefab";

        private const string MainStreetBushPrefabPath =
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Bush_01.prefab";

        private const string BuildingMaterialAPath =
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_A.mat";

        private const string BuildingMaterialBPath =
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_B.mat";

        private const string BuildingMaterialCPath =
            "Assets/PolygonMilitary/Materials/PolygonMilitary_Mat_01_C.mat";

        private static readonly string[] CleanStandaloneShopPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_04.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_08.prefab"
        };

        private const string RoofCap03PrefabPath =
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Roof_Cap_03.prefab";

        private static readonly Dictionary<GameObject, bool> DenseCityPrefabUsabilityCache = new();

        public static Result Build(
            RuntimeCityRAndDMapView view,
            Transform generatedRoot,
            RuntimeCitySpawnerSystemConfig config)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            if (generatedRoot == null)
                throw new ArgumentNullException(nameof(generatedRoot));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Vector3 runtimeGridOrigin = view.GridOrigin;
            float runtimeGridWidth = view.GridWidth * view.GridCellSize;
            float runtimeGridDepth = view.GridHeight * view.GridCellSize;
            Vector3 mapCenter = runtimeGridOrigin +
                                new Vector3(runtimeGridWidth * 0.5f, 0f, runtimeGridDepth * 0.5f);
            Vector3 cityOrigin = runtimeGridOrigin -
                                 new Vector3(WestCityExpansion, 0f, SouthCityExpansion);
            float cityWidth = runtimeGridWidth + WestCityExpansion;
            float cityDepth = runtimeGridDepth + SouthCityExpansion + NorthCityExpansion;
            Vector3 cityCenter = cityOrigin + new Vector3(cityWidth * 0.5f, 0f, cityDepth * 0.5f);
            var authoredCoreBounds = new Rect(
                mapCenter.x - 130f,
                mapCenter.z - 95f,
                260f,
                190f);
            ProtectedAreaMap protectedAreas = BuildProtectedAreaMap();
            var cityFootprint = new CityFootprint(
                new Vector2(cityCenter.x, cityCenter.z),
                cityWidth * 0.48f,
                cityDepth * 0.46f,
                config.RandomSeed,
                protectedAreas);

            Debug.Log(
                $"[DenseCityExpansion] origin=({cityOrigin.x:0},{cityOrigin.z:0}) " +
                $"size={cityWidth:0}x{cityDepth:0} protectedCells={protectedAreas.CellCount} " +
                $"protectedRenderers={protectedAreas.Describe()}");

            int suppressedTerrainObjects = PrepareTerrainForDenseCity(cityFootprint);
            float authoredGradeElevation = cityOrigin.y;
            Debug.Log(
                $"[DenseCityTerrainGrade] elevation={authoredGradeElevation:0.00} " +
                $"suppressedInteriorObjects={suppressedTerrainObjects}");

            using SurfacePlacementContext surface = SurfacePlacementContext.Create();
            var terrainMap = new TerrainViabilityMap(
                surface,
                cityOrigin,
                cityFootprint,
                authoredGradeElevation);
            terrainMap.LogAudit(
                cityFootprint,
                Mathf.FloorToInt(cityWidth / RoadGridSize) - 1,
                Mathf.FloorToInt(cityDepth / RoadGridSize) - 1);
            int authoredCoreRenderers = BakeCivicBazaarCore(
                generatedRoot,
                view,
                config,
                mapCenter,
                terrainMap,
                surface);
            RoadBakeResult roadResult = BakeRoadNetwork(
                generatedRoot,
                cityOrigin,
                cityWidth,
                cityDepth,
                authoredCoreBounds,
                cityFootprint,
                terrainMap,
                config.RandomSeed,
                surface);
            BuildingBakeResult buildingResult = BakeDenseDistricts(
                generatedRoot,
                view,
                config,
                cityOrigin,
                cityWidth,
                cityDepth,
                roadResult.StreetColumns,
                roadResult.StreetRows,
                roadResult.RoadCells,
                roadResult.DirtRoadCells,
                authoredCoreBounds,
                cityFootprint,
                terrainMap,
                surface);

            BuildingMaterialVariantResult materialVariants = ApplyBuildingMaterialVariants(
                generatedRoot,
                config.RandomSeed);
            Debug.Log(
                $"[DenseCityBuildingMaterials] buildingsA={materialVariants.BuildingsA} " +
                $"buildingsB={materialVariants.BuildingsB} buildingsC={materialVariants.BuildingsC} " +
                $"materialSlotsChanged={materialVariants.MaterialSlotsChanged}");

            int roofDetails = AddShopRoofDetails(generatedRoot);
            int openGroundDetails = AddOpenGroundDetails(
                generatedRoot,
                cityOrigin,
                cityWidth,
                cityDepth,
                cityFootprint,
                authoredCoreBounds,
                roadResult.RoadCells,
                authoredGradeElevation,
                config.RandomSeed);
            Debug.Log(
                $"[DenseCityDetailPass] roofCaps={roofDetails} " +
                $"openGroundPatches={openGroundDetails}");

            UrbanDetailResult urbanDetails = AddUrbanDetailProps(
                generatedRoot,
                cityOrigin,
                cityWidth,
                cityDepth,
                cityFootprint,
                authoredCoreBounds,
                roadResult.RoadCells,
                roadResult.DirtRoadCells,
                authoredGradeElevation,
                config.RandomSeed);
            Debug.Log(
                $"[DenseCityUrbanProps] waterTanks={urbanDetails.WaterTanks} " +
                $"streetProps={urbanDetails.StreetProps} trees={urbanDetails.Trees} " +
                $"rocks={urbanDetails.Rocks} courtyards={urbanDetails.Courtyards} " +
                $"courtyardWalls={urbanDetails.CourtyardWalls} " +
                $"courtyardPillars={urbanDetails.CourtyardPillars} " +
                $"courtyardWells={urbanDetails.CourtyardWells} " +
                $"courtyardBushes={urbanDetails.CourtyardBushes} " +
                $"courtyardGroundPatchesRemoved={urbanDetails.CourtyardGroundPatchesRemoved} " +
                $"powerPoles={urbanDetails.PowerPoles} powerLines={urbanDetails.PowerLines} " +
                $"streetLights={urbanDetails.StreetLights} " +
                $"grassPatches={urbanDetails.GrassPatches} " +
                $"mainStreetBushes={urbanDetails.MainStreetBushes}");

            int removedFloatingBranches = RemoveUnsupportedElevatedVisualBranches(generatedRoot);
            Debug.Log($"[DenseCityFloatingItemCleanup] removedBranches={removedFloatingBranches}");

            int protectedOverlaps = AuditGeneratedProtectedOverlaps(generatedRoot, protectedAreas);
            if (protectedOverlaps > 0)
            {
                throw new InvalidOperationException(
                    $"Dense city generation produced {protectedOverlaps} renderer bounds overlapping " +
                    "protected authored map geometry. See [DenseCityProtectedOverlap] diagnostics.");
            }

            return new Result(
                roadResult.TileCount,
                roadResult.ChunkCount,
                buildingResult.BuildingCount,
                buildingResult.ParkCount,
                authoredCoreRenderers);
        }

        private static int AuditGeneratedProtectedOverlaps(
            Transform generatedRoot,
            ProtectedAreaMap protectedAreas)
        {
            int overlapCount = 0;
            Renderer[] renderers = generatedRoot.GetComponentsInChildren<Renderer>(false);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || !protectedAreas.Intersects(renderer.bounds))
                    continue;

                if (overlapCount < 20)
                {
                    Debug.LogError(
                        $"[DenseCityProtectedOverlap] path={GetTransformPath(renderer.transform)} " +
                        $"center={renderer.bounds.center} size={renderer.bounds.size}");
                }
                overlapCount++;
            }

            Debug.Log(
                $"[DenseCityProtectedAudit] generatedRenderers={renderers.Length} overlaps={overlapCount}");
            return overlapCount;
        }

        private static int RemoveUnsupportedElevatedVisualBranches(Transform generatedRoot)
        {
            const float groundedTolerance = 0.75f;
            const float maximumVerticalJoinGap = 1.35f;
            const float horizontalJoinMargin = 0.35f;

            int removedBranches = 0;
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform wrapper = transforms[transformIndex];
                if (wrapper == null ||
                    !wrapper.name.EndsWith("_Visual", StringComparison.Ordinal) ||
                    wrapper.parent == null ||
                    wrapper.parent.name != "RuntimeCityVisuals")
                {
                    continue;
                }

                Renderer[] renderers = wrapper.GetComponentsInChildren<Renderer>(false);
                if (renderers.Length < 2)
                    continue;

                float groundedHeight = wrapper.position.y + groundedTolerance;
                var supported = new HashSet<Renderer>();
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null && renderer.bounds.min.y <= groundedHeight)
                        supported.Add(renderer);
                }

                bool addedSupport;
                do
                {
                    addedSupport = false;
                    for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    {
                        Renderer candidate = renderers[rendererIndex];
                        if (candidate == null || supported.Contains(candidate))
                            continue;

                        foreach (Renderer support in supported)
                        {
                            if (IsRendererSupportedBy(
                                    candidate.bounds,
                                    support.bounds,
                                    maximumVerticalJoinGap,
                                    horizontalJoinMargin))
                            {
                                supported.Add(candidate);
                                addedSupport = true;
                                break;
                            }
                        }
                    }
                }
                while (addedSupport);

                var unsupportedObjects = new HashSet<GameObject>();
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null && !supported.Contains(renderer))
                        unsupportedObjects.Add(renderer.gameObject);
                }

                foreach (GameObject unsupportedObject in unsupportedObjects)
                {
                    if (unsupportedObject == null ||
                        HasUnsupportedAncestor(unsupportedObject.transform, wrapper, unsupportedObjects))
                    {
                        continue;
                    }

                    UnityEngine.Object.DestroyImmediate(unsupportedObject);
                    removedBranches++;
                }
            }

            return removedBranches;
        }

        private static bool IsRendererSupportedBy(
            Bounds candidate,
            Bounds support,
            float maximumVerticalJoinGap,
            float horizontalJoinMargin)
        {
            float verticalGap = candidate.min.y - support.max.y;
            if (verticalGap < -0.1f || verticalGap > maximumVerticalJoinGap)
                return false;

            return candidate.min.x <= support.max.x + horizontalJoinMargin &&
                   candidate.max.x >= support.min.x - horizontalJoinMargin &&
                   candidate.min.z <= support.max.z + horizontalJoinMargin &&
                   candidate.max.z >= support.min.z - horizontalJoinMargin;
        }

        private static bool HasUnsupportedAncestor(
            Transform candidate,
            Transform wrapper,
            HashSet<GameObject> unsupportedObjects)
        {
            Transform ancestor = candidate.parent;
            while (ancestor != null && ancestor != wrapper)
            {
                if (unsupportedObjects.Contains(ancestor.gameObject))
                    return true;
                ancestor = ancestor.parent;
            }

            return false;
        }

        private static int PrepareTerrainForDenseCity(CityFootprint footprint)
        {
            GameObject map = FindSceneObjectByName("Map");
            if (map == null)
                throw new MissingReferenceException("Dense city terrain grading requires the scene Map hierarchy.");

            GameObject archive = FindSceneObjectByName("DenseCity_GradingArchive");
            if (archive == null)
            {
                archive = new GameObject("DenseCity_GradingArchive");
                Undo.RegisterCreatedObjectUndo(archive, "Create Dense City Grading Archive");
                archive.SetActive(false);
            }

            string[] categories =
            {
                "Ground",
                "GroundHills",
                "Rocks",
                "Grass",
                "Concrete"
            };
            int suppressed = 0;
            for (int categoryIndex = 0; categoryIndex < categories.Length; categoryIndex++)
            {
                Transform category = FindDescendant(map.transform, categories[categoryIndex]);
                if (category == null)
                    continue;

                var candidates = new List<Transform>();
                for (int childIndex = 0; childIndex < category.childCount; childIndex++)
                {
                    Transform child = category.GetChild(childIndex);
                    if (child == null)
                        continue;
                    if (category.name == "Ground" && ApplyGroundVariationToRetainedBase(child))
                        continue;
                    if (!ShouldSuppressTerrainChild(category.name, child, footprint))
                        continue;

                    candidates.Add(child);
                }

                if (candidates.Count == 0)
                    continue;

                Transform archiveCategory = FindDirectChild(archive.transform, category.name);
                if (archiveCategory == null)
                {
                    var archiveCategoryObject = new GameObject(category.name);
                    Undo.RegisterCreatedObjectUndo(archiveCategoryObject, "Create Dense City Grading Category");
                    archiveCategoryObject.transform.SetParent(archive.transform, false);
                    archiveCategory = archiveCategoryObject.transform;
                }

                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    Transform child = candidates[candidateIndex];
                    Undo.RecordObject(child.gameObject, "Grade Dense City Terrain");
                    Undo.SetTransformParent(child, archiveCategory, "Archive Dense City Terrain");
                    child.gameObject.SetActive(false);
                    suppressed++;
                }
            }

            return suppressed;
        }

        private static ProtectedAreaMap BuildProtectedAreaMap()
        {
            GameObject map = FindSceneObjectByName("Map");
            if (map == null)
                throw new MissingReferenceException("Dense city expansion requires the scene Map hierarchy.");

            var protectedAreas = new ProtectedAreaMap();
            AddProtectedCategory("Buildings", 8f);
            AddProtectedCategory("_UnmappedBuildings", 8f);
            AddProtectedCategory("Roads", 5f);
            AddProtectedCategory("Bridges", 8f);
            AddProtectedCategory("Runways", 10f);
            AddProtectedCategory("ResourceAreas", 10f);
            AddProtectedCategory("Mountains", 14f);
            return protectedAreas;

            void AddProtectedCategory(string categoryName, float margin)
            {
                Transform category = FindDescendant(map.transform, categoryName);
                if (category == null)
                    return;

                Renderer[] renderers = category.GetComponentsInChildren<Renderer>(false);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    protectedAreas.AddRenderer(categoryName, renderers[rendererIndex], margin);
            }
        }

        private static bool ApplyGroundVariationToRetainedBase(Transform child)
        {
            if (!TryGetWorldBounds(child, out Bounds bounds) ||
                bounds.size.x <= 1000f ||
                bounds.size.z <= 1000f ||
                bounds.size.y >= 0.2f)
            {
                return false;
            }

            Material material = GetGroundVariationMaterial();
            if (material == null)
                throw new InvalidOperationException("Dense city ground variation material could not be loaded.");

            Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                    continue;
                Undo.RecordObject(renderer, "Apply Dense City Ground Variation");
                renderer.sharedMaterial = material;
            }

            Debug.Log(
                $"[DenseCityGroundVariation] retainedBase={child.name} renderers={renderers.Length} " +
                $"size={bounds.size.x:0}x{bounds.size.z:0}");
            return true;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
                return null;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child != null && child.name == name)
                    return child;
            }

            return null;
        }

        private static bool ShouldSuppressTerrainChild(
            string category,
            Transform child,
            CityFootprint footprint)
        {
            if (!TryGetWorldBounds(child, out Bounds bounds))
                return false;

            Vector2 center = new(bounds.center.x, bounds.center.z);
            if (!footprint.Contains(center, 0.06f))
                return false;

            string objectName = child.name;
            if (category == "Ground")
            {
                bool isRelief = objectName.IndexOf("SandDunes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                objectName.IndexOf("Ground_Hill", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isRelief)
                    return false;

                // The operation map's very large flat ground remains the retained city base.
                if (bounds.size.x > 1000f && bounds.size.z > 1000f && bounds.size.y < 0.2f)
                    return false;
            }

            return true;
        }

        private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool found = false;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer renderer = renderers[index];
                if (renderer == null)
                    continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        public static void CaptureVisualProof(RuntimeCityRAndDMapView view)
        {
            if (view == null || view.GeneratedRoot == null)
                throw new InvalidOperationException("Dense city visual proof requires a generated city root.");

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                                 throw new InvalidOperationException("Could not resolve the Unity project root.");
            string outputFolder = Path.Combine(
                projectRoot,
                "Design/VisualLockLayered/_OperationMapDenseCity");
            Directory.CreateDirectory(outputFolder);

            Vector3 origin = view.GridOrigin;
            Vector3 center = origin + new Vector3(
                view.GridWidth * view.GridCellSize * 0.5f,
                0f,
                view.GridHeight * view.GridCellSize * 0.5f);
            Vector3 fullMapCenter = center;
            float fullMapOrthographicSize = 545f;
            if (TryGetWorldBounds(view.GeneratedRoot, out Bounds generatedBounds))
            {
                fullMapCenter = new Vector3(
                    generatedBounds.center.x,
                    center.y,
                    generatedBounds.center.z);
                fullMapOrthographicSize = Mathf.Max(
                    generatedBounds.extents.z * 1.08f,
                    generatedBounds.extents.x * 0.54f);
            }
            GameObject clouds = FindSceneObjectByName("Clouds");
            bool cloudsWereActive = clouds != null && clouds.activeSelf;
            if (cloudsWereActive)
                clouds.SetActive(false);
            try
            {
                Capture(
                    fullMapCenter + new Vector3(0f, 2200f, 0f),
                    fullMapCenter,
                    orthographic: true,
                    orthographicSize: fullMapOrthographicSize,
                    2048,
                    1024,
                    Path.Combine(outputFolder, "dense_city_full_map.png"));
                Capture(
                    center + new Vector3(-275f, 175f, -295f),
                    center + new Vector3(0f, 0f, 15f),
                    orthographic: false,
                    orthographicSize: 0f,
                    1920,
                    1080,
                    Path.Combine(outputFolder, "dense_city_civic_bazaar_oblique.png"));
                Capture(
                    center + new Vector3(-135f, 92f, -145f),
                    center + new Vector3(0f, 0f, 12f),
                    orthographic: false,
                    orthographicSize: 0f,
                    1920,
                    1080,
                    Path.Combine(outputFolder, "dense_city_bazaar_close.png"));
                Capture(
                    center + new Vector3(-58f, 24f, -68f),
                    center + new Vector3(0f, 9f, 4f),
                    orthographic: false,
                    orthographicSize: 0f,
                    1920,
                    1080,
                    Path.Combine(outputFolder, "dense_city_bazaar_street_level.png"));
                if (TryFindCourtyardProofView(
                        view.GeneratedRoot,
                        out Vector3 courtyardCamera,
                        out Vector3 courtyardTarget))
                {
                    Capture(
                        courtyardCamera,
                        courtyardTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_house_courtyard.png"));
                }
                if (TryFindPowerlineProofView(
                        view.GeneratedRoot,
                        out Vector3 powerlineCamera,
                        out Vector3 powerlineTarget))
                {
                    Capture(
                        powerlineCamera,
                        powerlineTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_roadside_power.png"));
                }
                if (TryFindStreetLightProofView(
                        view.GeneratedRoot,
                        out Vector3 streetLightCamera,
                        out Vector3 streetLightTarget))
                {
                    Capture(
                        streetLightCamera,
                        streetLightTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_sidewalk_streetlights.png"));
                }
                if (TryFindLandscapingProofView(
                        view.GeneratedRoot,
                        out Vector3 landscapingCamera,
                        out Vector3 landscapingTarget))
                {
                    Capture(
                        landscapingCamera,
                        landscapingTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_roadside_landscaping.png"));
                }
            }
            finally
            {
                if (cloudsWereActive)
                    clouds.SetActive(true);
            }
            AssetDatabase.Refresh();
            Debug.Log($"[DenseCityVisualProof] result=Captured output={outputFolder}", view);
        }

        private static bool TryFindCourtyardProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform leftPillar = transforms[index];
                if (leftPillar == null ||
                    !leftPillar.name.StartsWith("SM_Bld_Village_Wall_Pillar_01_Courtyard_", StringComparison.Ordinal) ||
                    !leftPillar.name.EndsWith("_GateLeft", StringComparison.Ordinal))
                {
                    continue;
                }

                int courtyardMarker = leftPillar.name.IndexOf("_Courtyard_", StringComparison.Ordinal);
                int courtyardIndexStart = courtyardMarker + "_Courtyard_".Length;
                if (courtyardMarker < 0 || leftPillar.name.Length < courtyardIndexStart + 4)
                    continue;

                string courtyardIndex = leftPillar.name.Substring(courtyardIndexStart, 4);
                Transform rightPillar = null;
                Transform well = null;
                string rightName = $"SM_Bld_Village_Wall_Pillar_01_Courtyard_{courtyardIndex}_GateRight";
                string wellName = $"SM_Bld_Village_Well_01_Courtyard_{courtyardIndex}";
                for (int candidateIndex = 0; candidateIndex < transforms.Length; candidateIndex++)
                {
                    Transform candidate = transforms[candidateIndex];
                    if (candidate == null)
                        continue;
                    if (candidate.name == rightName)
                        rightPillar = candidate;
                    else if (candidate.name == wellName)
                        well = candidate;
                }

                if (rightPillar == null || well == null)
                    continue;

                Vector3 gateCenter = (leftPillar.position + rightPillar.position) * 0.5f;
                Vector3 inward = well.position - gateCenter;
                inward.y = 0f;
                if (inward.sqrMagnitude < 0.25f)
                    continue;
                inward.Normalize();

                target = Vector3.Lerp(gateCenter, well.position, 0.58f) + Vector3.up * 0.9f;
                cameraPosition = gateCenter - inward * 11f + Vector3.up * 8f;
                return true;
            }

            cameraPosition = default;
            target = default;
            return false;
        }

        private static bool TryFindPowerlineProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    !candidate.name.StartsWith("SM_Prop_Powerline_02_Roadside_", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                Vector3 spanDirection = candidate.forward;
                spanDirection.y = 0f;
                if (spanDirection.sqrMagnitude < 0.1f)
                    spanDirection = Vector3.forward;
                spanDirection.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, spanDirection).normalized;
                target = bounds.center - Vector3.up * 1.5f;
                cameraPosition = target + side * 15f - spanDirection * 10f + Vector3.up * 8f;
                return true;
            }

            cameraPosition = default;
            target = default;
            return false;
        }

        private static bool TryFindStreetLightProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    !candidate.name.StartsWith("SM_Env_Road_Lights_01_Sidewalk_", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                Vector3 roadFacing = candidate.forward;
                roadFacing.y = 0f;
                if (roadFacing.sqrMagnitude < 0.1f)
                    roadFacing = Vector3.forward;
                roadFacing.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, roadFacing).normalized;
                target = bounds.center + roadFacing * 3f - Vector3.up * 1.4f;
                cameraPosition = target - roadFacing * 11f + side * 9f + Vector3.up * 6f;
                return true;
            }

            cameraPosition = default;
            target = default;
            return false;
        }

        private static bool TryFindLandscapingProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    !candidate.name.StartsWith("SM_Env_Tree_Bush_01_MainStreet_", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                target = bounds.center + Vector3.up * 0.35f;
                cameraPosition = target + new Vector3(13f, 8f, -13f);
                return true;
            }

            cameraPosition = default;
            target = default;
            return false;
        }

        private static void Capture(
            Vector3 cameraPosition,
            Vector3 target,
            bool orthographic,
            float orthographicSize,
            int width,
            int height,
            string outputPath)
        {
            var cameraObject = new GameObject("DenseCityVisualProofCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("DenseCityVisualProofLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1
            };
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.backgroundColor = new Color(0.48f, 0.57f, 0.64f, 1f);
                camera.nearClipPlane = 0.5f;
                camera.farClipPlane = 5000f;
                camera.fieldOfView = 54f;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
                camera.transform.position = cameraPosition;
                camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, Vector3.up);
                camera.targetTexture = renderTexture;

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.color = new Color(1f, 0.91f, 0.75f);
                light.shadows = LightShadows.None;
                light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private readonly struct RoadBakeResult
        {
            public readonly int TileCount;
            public readonly int ChunkCount;
            public readonly List<int> StreetColumns;
            public readonly List<int> StreetRows;
            public readonly HashSet<Vector2Int> DirtRoadCells;
            public readonly HashSet<Vector2Int> RoadCells;

            public RoadBakeResult(
                int tileCount,
                int chunkCount,
                List<int> streetColumns,
                List<int> streetRows,
                HashSet<Vector2Int> dirtRoadCells,
                HashSet<Vector2Int> roadCells)
            {
                TileCount = tileCount;
                ChunkCount = chunkCount;
                StreetColumns = streetColumns;
                StreetRows = streetRows;
                DirtRoadCells = dirtRoadCells;
                RoadCells = roadCells;
            }
        }

        private readonly struct BuildingBakeResult
        {
            public readonly int BuildingCount;
            public readonly int ParkCount;

            public BuildingBakeResult(int buildingCount, int parkCount)
            {
                BuildingCount = buildingCount;
                ParkCount = parkCount;
            }
        }

        private readonly struct BuildingMaterialVariantResult
        {
            public readonly int BuildingsA;
            public readonly int BuildingsB;
            public readonly int BuildingsC;
            public readonly int MaterialSlotsChanged;

            public BuildingMaterialVariantResult(
                int buildingsA,
                int buildingsB,
                int buildingsC,
                int materialSlotsChanged)
            {
                BuildingsA = buildingsA;
                BuildingsB = buildingsB;
                BuildingsC = buildingsC;
                MaterialSlotsChanged = materialSlotsChanged;
            }
        }

        private readonly struct GeneratedBuildingInfo
        {
            public readonly Transform Wrapper;
            public readonly Bounds Bounds;
            public readonly Rect Footprint;
            public readonly bool IsShop;
            public readonly bool IsHouse;

            public GeneratedBuildingInfo(Transform wrapper, Bounds bounds)
            {
                Wrapper = wrapper;
                Bounds = bounds;
                Footprint = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                IsShop = wrapper.name.IndexOf("Shop", StringComparison.OrdinalIgnoreCase) >= 0;
                IsHouse = wrapper.name.IndexOf("House", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          wrapper.name.IndexOf("Village", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private readonly struct UrbanDetailResult
        {
            public readonly int WaterTanks;
            public readonly int StreetProps;
            public readonly int Trees;
            public readonly int Rocks;
            public readonly int Courtyards;
            public readonly int CourtyardWalls;
            public readonly int CourtyardPillars;
            public readonly int CourtyardWells;
            public readonly int CourtyardBushes;
            public readonly int CourtyardGroundPatchesRemoved;
            public readonly int PowerPoles;
            public readonly int PowerLines;
            public readonly int StreetLights;
            public readonly int GrassPatches;
            public readonly int MainStreetBushes;

            public UrbanDetailResult(
                int waterTanks,
                int streetProps,
                int trees,
                int rocks,
                int courtyards,
                int courtyardWalls,
                int courtyardPillars,
                int courtyardWells,
                int courtyardBushes,
                int courtyardGroundPatchesRemoved,
                int powerPoles,
                int powerLines,
                int streetLights,
                int grassPatches,
                int mainStreetBushes)
            {
                WaterTanks = waterTanks;
                StreetProps = streetProps;
                Trees = trees;
                Rocks = rocks;
                Courtyards = courtyards;
                CourtyardWalls = courtyardWalls;
                CourtyardPillars = courtyardPillars;
                CourtyardWells = courtyardWells;
                CourtyardBushes = courtyardBushes;
                CourtyardGroundPatchesRemoved = courtyardGroundPatchesRemoved;
                PowerPoles = powerPoles;
                PowerLines = powerLines;
                StreetLights = streetLights;
                GrassPatches = grassPatches;
                MainStreetBushes = mainStreetBushes;
            }
        }

        private static int BakeCivicBazaarCore(
            Transform generatedRoot,
            RuntimeCityRAndDMapView view,
            RuntimeCitySpawnerSystemConfig config,
            Vector3 mapCenter,
            TerrainViabilityMap terrainMap,
            SurfacePlacementContext surface)
        {
            var coreObject = new GameObject("DenseCity_PedestrianCivicBazaarCore");
            coreObject.transform.SetParent(generatedRoot, false);
            var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
            visualSystem.SetRuntimeRoot(coreObject.transform);
            if (surface != null)
                visualSystem.ConfigureSurface(surface.Surface);

            GridConfig grid = CreateGrid(view);
            var placementContext = new BuildingPlacementContext(
                new HashSet<Vector2Int>(),
                view.GridOrigin);
            GameObject hallPrefab = FirstPrefab(config.HallPrefabs) ??
                                    throw new InvalidOperationException("Dense city config requires a hall prefab.");
            PrefabFootprint hall = MeasurePrefab(hallPrefab, 0.95f);
            SpawnBuilding(
                visualSystem,
                hall,
                mapCenter + new Vector3(0f, 0f, 55f),
                180f,
                grid,
                terrainMap,
                placementContext);

            var market = new List<PrefabFootprint>();
            AddPrefabList(config.ShopPrefabs, market, 0.9f);
            if (market.Count == 0)
                throw new InvalidOperationException("Dense city config requires shop prefabs for its bazaar.");

            var random = new System.Random(unchecked((int)config.RandomSeed) ^ 0x2ca44f);
            int shopIndex = 0;
            float[] marketRows = { -78f, -62f, -46f, -30f, -14f, 2f, 18f, 34f, 50f };
            for (int rowIndex = 0; rowIndex < marketRows.Length; rowIndex++)
            {
                float rowZ = marketRows[rowIndex];
                float facing = rowIndex % 2 == 0 ? 0f : 180f;
                float offset = rowIndex % 2 == 0 ? 0f : 5.75f;
                for (float x = -104f + offset; x <= 104f; x += 11.5f)
                {
                    if (rowZ >= 18f && Mathf.Abs(x) < 48f)
                        continue;
                    SpawnBuilding(
                        visualSystem,
                        market[shopIndex++ % market.Count],
                        mapCenter + new Vector3(x, 0f, rowZ),
                        facing,
                        grid,
                        terrainMap,
                        placementContext);
                }
            }

            for (float z = -72f; z <= 54f; z += 14f)
            {
                SpawnBuilding(
                    visualSystem,
                    market[shopIndex++ % market.Count],
                    mapCenter + new Vector3(-116f, 0f, z),
                    90f,
                    grid,
                    terrainMap,
                    placementContext);
                SpawnBuilding(
                    visualSystem,
                    market[shopIndex++ % market.Count],
                    mapCenter + new Vector3(116f, 0f, z),
                    270f,
                    grid,
                    terrainMap,
                    placementContext);
            }

            GameObject fountain01 = AssetDatabase.LoadAssetAtPath<GameObject>(ParkPrefabPaths[4]);
            GameObject fountain02 = AssetDatabase.LoadAssetAtPath<GameObject>(ParkPrefabPaths[5]);
            if (fountain01 != null && fountain02 != null)
            {
                PrefabFootprint fountain = MeasurePrefab(fountain01, 0.8f);
                SpawnBuilding(
                    visualSystem,
                    fountain,
                    mapCenter + new Vector3(-42f, 0f, 29f),
                    random.Next(0, 4) * 90f,
                    grid,
                    terrainMap,
                    placementContext);
                fountain = MeasurePrefab(fountain02, 0.8f);
                SpawnBuilding(
                    visualSystem,
                    fountain,
                    mapCenter + new Vector3(42f, 0f, 29f),
                    random.Next(0, 4) * 90f,
                    grid,
                    terrainMap,
                    placementContext);
            }

            AddCivicPromenadeTrees(
                visualSystem,
                grid,
                mapCenter,
                terrainMap,
                placementContext,
                random);

            Debug.Log(
                $"[DenseCityCivicPlacementAudit] reserved={placementContext.ReservedCount} overlaps=0");

            DisableColliders(coreObject);
            SetStaticRecursively(coreObject);
            return CountActiveRenderers(coreObject);
        }

        private static void AddCivicPromenadeTrees(
            RuntimeCityVisualPresentationSystemHelper visuals,
            GridConfig grid,
            Vector3 mapCenter,
            TerrainViabilityMap terrainMap,
            BuildingPlacementContext placementContext,
            System.Random random)
        {
            var treePrefabs = new List<PrefabFootprint>();
            for (int index = 0; index < 4 && index < ParkPrefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ParkPrefabPaths[index]);
                if (prefab != null)
                    treePrefabs.Add(MeasurePrefab(prefab, 0.9f));
            }

            if (treePrefabs.Count == 0)
                return;

            for (float x = -102f; x <= 102f; x += 24f)
            {
                PrefabFootprint tree = treePrefabs[random.Next(treePrefabs.Count)];
                SpawnBuilding(
                    visuals,
                    tree,
                    mapCenter + new Vector3(x, 0f, 28f),
                    random.Next(4) * 90f,
                    grid,
                    terrainMap,
                    placementContext);
                tree = treePrefabs[random.Next(treePrefabs.Count)];
                SpawnBuilding(
                    visuals,
                    tree,
                    mapCenter + new Vector3(x, 0f, 82f),
                    random.Next(4) * 90f,
                    grid,
                    terrainMap,
                    placementContext);
            }
        }

        private static RoadBakeResult BakeRoadNetwork(
            Transform generatedRoot,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            Rect authoredCoreBounds,
            CityFootprint cityFootprint,
            TerrainViabilityMap terrainMap,
            uint seed,
            SurfacePlacementContext surface)
        {
            var roadObject = new GameObject("DenseCity_ConnectedSidewalkRoadNetwork");
            roadObject.transform.SetParent(generatedRoot, false);

            int maximumColumn = Mathf.FloorToInt(mapWidth / RoadGridSize) - 1;
            int maximumRow = Mathf.FloorToInt(mapDepth / RoadGridSize) - 1;
            var random = new System.Random(unchecked((int)(seed == 0 ? 26071501u : seed)) ^ 0x4a17b2);
            List<int> streetColumns = BuildIrregularStreetCoordinates(maximumColumn, random, 4, 13);
            List<int> streetRows = BuildIrregularStreetCoordinates(maximumRow, random, 4, 12);
            int centerColumn = maximumColumn / 2;
            int centerRow = maximumRow / 2;
            int eastArterial = centerColumn + 15;
            int southArterial = centerRow - 12;
            EnsureStreetCoordinate(streetColumns, eastArterial);
            EnsureStreetCoordinate(streetRows, southArterial);
            var network = new RoadNetworkCompositionSystemHelper();

            AddVerticalRoad(
                network,
                eastArterial,
                maximumRow,
                mapOrigin,
                authoredCoreBounds,
                cityFootprint,
                terrainMap,
                isAutobahn: true);
            AddHorizontalRoad(
                network,
                southArterial,
                maximumColumn,
                mapOrigin,
                authoredCoreBounds,
                cityFootprint,
                terrainMap,
                isAutobahn: true);
            for (int index = 0; index < streetRows.Count; index++)
            {
                if (streetRows[index] == southArterial)
                    continue;
                var cells = new List<Vector2Int>(maximumColumn + 1);
                for (int column = 1; column < maximumColumn; column++)
                    cells.Add(new Vector2Int(column, streetRows[index]));
                AddMaskedRoadStroke(
                    network,
                    cells,
                    mapOrigin,
                    authoredCoreBounds,
                    cityFootprint,
                    terrainMap,
                    isAutobahn: false);
            }

            for (int index = 0; index < streetColumns.Count; index++)
            {
                if (streetColumns[index] == eastArterial)
                    continue;
                var cells = new List<Vector2Int>(maximumRow + 1);
                for (int row = 1; row < maximumRow; row++)
                    cells.Add(new Vector2Int(streetColumns[index], row));
                AddMaskedRoadStroke(
                    network,
                    cells,
                    mapOrigin,
                    authoredCoreBounds,
                    cityFootprint,
                    terrainMap,
                    isAutobahn: false);
            }

            var dirtRoadCells = new HashSet<Vector2Int>();
            AddNeighborhoodAlleys(
                network,
                streetColumns,
                streetRows,
                mapOrigin,
                authoredCoreBounds,
                cityFootprint,
                terrainMap,
                random,
                dirtRoadCells);

            foreach (Vector2Int cell in network.StrokeIdsByCell.Keys)
            {
                if (cityFootprint.NormalizedDistance(RoadCellWorldCenter(cell, mapOrigin)) >= 0.72f)
                    dirtRoadCells.Add(cell);
            }
            dirtRoadCells.ExceptWith(network.AutobahnCells);
            dirtRoadCells.ExceptWith(network.AutobahnConnectorCells);

            RoadVisualVariantSystem.Prefabs prefabs = LoadRoadPrefabs();
            RoadVisualVariantSystem.Prefabs dirtRoadPrefabs = LoadDirtRoadPrefabs();
            RoadVisualVariantSystem.Prefabs asphaltRoadPrefabs = LoadAsphaltRoadPrefabs();
            using var world = new World("DenseCityRoadBakeWorld");
            using var dirtRoadWorld = new World("DenseCityDirtRoadBakeWorld");
            using var asphaltRoadWorld = new World("DenseCityAsphaltRoadBakeWorld");
            RoadVisualVariantSystem variants = world.CreateSystemManaged<RoadVisualVariantSystem>();
            RoadVisualVariantSystem dirtRoadVariants = dirtRoadWorld.CreateSystemManaged<RoadVisualVariantSystem>();
            RoadVisualVariantSystem asphaltRoadVariants = asphaltRoadWorld.CreateSystemManaged<RoadVisualVariantSystem>();
            variants.CacheVariants(prefabs);
            dirtRoadVariants.CacheVariants(dirtRoadPrefabs);
            asphaltRoadVariants.CacheVariants(asphaltRoadPrefabs);
            var resolutionContext = new RoadVisualResolutionSystem.Context(network, variants, default);

            foreach (Vector2Int cell in network.StrokeIdsByCell.Keys)
            {
                RoadNetworkCompositionSystemHelper.TileConnectionMask mask = network.GetMask(cell);
                bool useAsphaltRoad = network.AutobahnCells.Contains(cell) ||
                                      network.AutobahnConnectorCells.Contains(cell);
                RoadVisualVariantSystem resolutionVariants = useAsphaltRoad ? asphaltRoadVariants : variants;
                RoadNetworkCompositionSystemHelper.RoadVisualType type = useAsphaltRoad
                    ? ResolveStandardRoadVisualType(mask)
                    : RoadVisualResolutionSystem.ResolveVisualType(resolutionContext, cell, mask);
                if (type == RoadNetworkCompositionSystemHelper.RoadVisualType.None ||
                    !resolutionVariants.TryGetVariant(type, mask, out RoadVisualVariantSystem.VariantData variant))
                {
                    continue;
                }

                network.RoadTiles[cell] = new RoadNetworkCompositionSystemHelper.RoadTileData
                {
                    Type = type,
                    Mask = mask,
                    Rotation = variant.Rotation,
                    Scale = variant.Scale
                };
            }

            if (AssetDatabase.IsValidFolder(GeneratedRoadMeshFolder))
                AssetDatabase.DeleteAsset(GeneratedRoadMeshFolder);
            var placementContext = new RoadChunkVisualSystem.Context(
                network.RoadTiles,
                variants.VisualData,
                network.AutobahnCells,
                network.AutobahnConnectorCells,
                roadObject.transform,
                mapOrigin,
                mapOrigin.y,
                RoadGridSize,
                RoadChunkSize);
            RoadElevationPlan elevationPlan = RoadElevationPlan.Build(network, terrainMap);
            int chunkCount = InstantiateRoadPrefabTiles(
                network,
                variants,
                prefabs,
                dirtRoadVariants,
                dirtRoadPrefabs,
                asphaltRoadVariants,
                asphaltRoadPrefabs,
                placementContext,
                roadObject.transform,
                dirtRoadCells,
                terrainMap,
                elevationPlan,
                surface);
            SetStaticRecursively(roadObject);
            return new RoadBakeResult(
                network.RoadTiles.Count,
                chunkCount,
                streetColumns,
                streetRows,
                dirtRoadCells,
                new HashSet<Vector2Int>(network.RoadTiles.Keys));
        }

        private static List<int> BuildIrregularStreetCoordinates(
            int maximum,
            System.Random random,
            int minimumBlockCells,
            int maximumBlockCells)
        {
            var coordinates = new List<int> { 2 };
            int cursor = 2;
            while (cursor < maximum - minimumBlockCells - 2)
            {
                cursor += random.Next(minimumBlockCells, maximumBlockCells + 1);
                if (cursor < maximum - 2)
                    coordinates.Add(cursor);
            }

            if (coordinates[coordinates.Count - 1] != maximum - 2)
                coordinates.Add(maximum - 2);
            return coordinates;
        }

        private static void EnsureStreetCoordinate(List<int> coordinates, int coordinate)
        {
            if (!coordinates.Contains(coordinate))
            {
                coordinates.Add(coordinate);
                coordinates.Sort();
            }
        }

        private static void AddHorizontalRoad(
            RoadNetworkCompositionSystemHelper network,
            int row,
            int maximumColumn,
            Vector3 mapOrigin,
            Rect exclusion,
            CityFootprint footprint,
            TerrainViabilityMap terrainMap,
            bool isAutobahn)
        {
            var cells = new List<Vector2Int>(maximumColumn + 1);
            for (int column = 1; column < maximumColumn; column++)
                cells.Add(new Vector2Int(column, row));
            AddMaskedRoadStroke(network, cells, mapOrigin, exclusion, footprint, terrainMap, isAutobahn);
        }

        private static void AddVerticalRoad(
            RoadNetworkCompositionSystemHelper network,
            int column,
            int maximumRow,
            Vector3 mapOrigin,
            Rect exclusion,
            CityFootprint footprint,
            TerrainViabilityMap terrainMap,
            bool isAutobahn)
        {
            var cells = new List<Vector2Int>(maximumRow + 1);
            for (int row = 1; row < maximumRow; row++)
                cells.Add(new Vector2Int(column, row));
            AddMaskedRoadStroke(network, cells, mapOrigin, exclusion, footprint, terrainMap, isAutobahn);
        }

        private static void AddMaskedRoadStroke(
            RoadNetworkCompositionSystemHelper network,
            List<Vector2Int> cells,
            Vector3 mapOrigin,
            Rect exclusion,
            CityFootprint footprint,
            TerrainViabilityMap terrainMap,
            bool isAutobahn,
            bool sparseFringe = false)
        {
            var segment = new List<Vector2Int>();
            bool horizontal = cells.Count > 1 && cells[0].y == cells[1].y;
            for (int index = 0; index < cells.Count; index++)
            {
                Vector2Int cell = cells[index];
                Vector2 worldCenter = RoadCellWorldCenter(cell, mapOrigin);
                bool omitFringeSegment = sparseFringe &&
                                         footprint.NormalizedDistance(worldCenter) > 0.7f &&
                                         ShouldOmitFringeRoadSegment(cell, horizontal);
                if (!exclusion.Contains(worldCenter) &&
                    footprint.Contains(worldCenter) &&
                    terrainMap.CanPlaceRoad(cell) &&
                    !omitFringeSegment)
                {
                    segment.Add(cell);
                    continue;
                }

                CommitRoadStroke(network, segment, isAutobahn);
                segment.Clear();
            }

            CommitRoadStroke(network, segment, isAutobahn);
        }

        private static bool ShouldOmitFringeRoadSegment(Vector2Int cell, bool horizontal)
        {
            int along = horizontal ? cell.x / 8 : cell.y / 8;
            int across = horizontal ? cell.y : cell.x;
            int hash = unchecked((along * 73856093) ^ (across * 19349663));
            return Mathf.Abs(hash % 9) == 0;
        }

        private static void CommitRoadStroke(
            RoadNetworkCompositionSystemHelper network,
            List<Vector2Int> segment,
            bool isAutobahn = false)
        {
            if (segment.Count < 2)
                return;

            network.CreateStroke(
                new List<Vector2Int>(segment),
                isAutobahn,
                useAutobahnConnectorAtStart: false,
                useAutobahnConnectorAtEnd: false,
                out _);
        }

        private static void AddNeighborhoodAlleys(
            RoadNetworkCompositionSystemHelper network,
            List<int> streetColumns,
            List<int> streetRows,
            Vector3 mapOrigin,
            Rect exclusion,
            CityFootprint footprint,
            TerrainViabilityMap terrainMap,
            System.Random random,
            HashSet<Vector2Int> dirtRoadCells)
        {
            for (int columnIndex = 0; columnIndex < streetColumns.Count - 1; columnIndex++)
            {
                for (int rowIndex = 0; rowIndex < streetRows.Count - 1; rowIndex++)
                {
                    if (random.NextDouble() > 0.58d)
                        continue;

                    int left = streetColumns[columnIndex];
                    int right = streetColumns[columnIndex + 1];
                    int bottom = streetRows[rowIndex];
                    int top = streetRows[rowIndex + 1];
                    if (right - left < 6 || top - bottom < 6)
                        continue;

                    bool horizontal = random.Next(2) == 0;
                    var alley = new List<Vector2Int>();
                    if (horizontal)
                    {
                        int row = (bottom + top) / 2;
                        int start = random.Next(2) == 0 ? left : right;
                        int direction = start == left ? 1 : -1;
                        for (int step = 0; step <= Mathf.Min(3, right - left - 2); step++)
                            alley.Add(new Vector2Int(start + direction * step, row));
                    }
                    else
                    {
                        int column = (left + right) / 2;
                        int start = random.Next(2) == 0 ? bottom : top;
                        int direction = start == bottom ? 1 : -1;
                        for (int step = 0; step <= Mathf.Min(3, top - bottom - 2); step++)
                            alley.Add(new Vector2Int(column, start + direction * step));
                    }

                    if (alley.Count < 2 ||
                        exclusion.Contains(RoadCellWorldCenter(alley[1], mapOrigin)) ||
                        !footprint.Contains(RoadCellWorldCenter(alley[1], mapOrigin)) ||
                        !terrainMap.CanPlaceRoad(alley[1]))
                        continue;
                    CommitRoadStroke(network, alley);
                    for (int alleyIndex = 1; alleyIndex < alley.Count; alleyIndex++)
                        dirtRoadCells.Add(alley[alleyIndex]);
                }
            }
        }

        private static Vector2 RoadCellWorldCenter(Vector2Int cell, Vector3 origin) =>
            new(
                origin.x + (cell.x + 0.5f) * RoadGridSize,
                origin.z + (cell.y + 0.5f) * RoadGridSize);

        private static RoadVisualVariantSystem.Prefabs LoadRoadPrefabs()
        {
            string configPath = AssetDatabase.GUIDToAssetPath(RoadBuildConfigGuid);
            RoadBuildSystemConfig config = AssetDatabase.LoadAssetAtPath<RoadBuildSystemConfig>(configPath);
            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Missing RoadBuildSystemConfig asset for GUID {RoadBuildConfigGuid}. " +
                    "The dense-city builder requires the shared road-build configuration.");
            }

            GameObject Require(GameObject prefab, string fieldName) => prefab != null
                ? prefab
                : throw new InvalidOperationException(
                    $"RoadBuildSystemConfig '{configPath}' has no {fieldName} reference.");

            return new RoadVisualVariantSystem.Prefabs(
                Require(config.EndPrefab, nameof(config.EndPrefab)),
                Require(config.StraightPrefab, nameof(config.StraightPrefab)),
                Require(config.CornerPrefab, nameof(config.CornerPrefab)),
                Require(config.TIntersectionPrefab, nameof(config.TIntersectionPrefab)),
                Require(config.IntersectionPrefab, nameof(config.IntersectionPrefab)),
                null,
                null);
        }

        private static RoadVisualVariantSystem.Prefabs LoadDirtRoadPrefabs()
        {
            GameObject Load(string guid, string role)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                       throw new InvalidOperationException(
                           $"Missing Road_Dirt {role} prefab for GUID {guid}.");
            }

            return new RoadVisualVariantSystem.Prefabs(
                Load(DirtRoadEndGuid, "end"),
                Load(DirtRoadStraightGuid, "straight"),
                Load(DirtRoadCornerGuid, "corner"),
                Load(DirtRoadTIntersectionGuid, "T-intersection"),
                Load(DirtRoadIntersectionGuid, "intersection"),
                null,
                null);
        }

        private static RoadVisualVariantSystem.Prefabs LoadAsphaltRoadPrefabs()
        {
            GameObject Load(string guid, string role)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                       throw new InvalidOperationException(
                           $"Missing Road_Asphalt_With_Sidewalk {role} prefab for GUID {guid}.");
            }

            return new RoadVisualVariantSystem.Prefabs(
                Load(AsphaltRoadEndGuid, "end"),
                Load(AsphaltRoadStraightGuid, "straight"),
                Load(AsphaltRoadCornerGuid, "corner"),
                Load(AsphaltRoadTIntersectionGuid, "T-intersection"),
                Load(AsphaltRoadIntersectionGuid, "intersection"),
                null,
                null);
        }

        private static RoadNetworkCompositionSystemHelper.RoadVisualType ResolveStandardRoadVisualType(
            RoadNetworkCompositionSystemHelper.TileConnectionMask mask)
        {
            return mask.Count switch
            {
                0 => RoadNetworkCompositionSystemHelper.RoadVisualType.None,
                1 => RoadNetworkCompositionSystemHelper.RoadVisualType.End,
                2 when (mask.North && mask.South) || (mask.East && mask.West) =>
                    RoadNetworkCompositionSystemHelper.RoadVisualType.Straight,
                2 => RoadNetworkCompositionSystemHelper.RoadVisualType.Corner,
                3 => RoadNetworkCompositionSystemHelper.RoadVisualType.TIntersection,
                _ => RoadNetworkCompositionSystemHelper.RoadVisualType.Intersection
            };
        }

        private static int InstantiateRoadPrefabTiles(
            RoadNetworkCompositionSystemHelper network,
            RoadVisualVariantSystem variants,
            RoadVisualVariantSystem.Prefabs prefabs,
            RoadVisualVariantSystem dirtRoadVariants,
            RoadVisualVariantSystem.Prefabs dirtRoadPrefabs,
            RoadVisualVariantSystem asphaltRoadVariants,
            RoadVisualVariantSystem.Prefabs asphaltRoadPrefabs,
            RoadChunkVisualSystem.Context placementContext,
            Transform targetRoot,
            HashSet<Vector2Int> dirtRoadCells,
            TerrainViabilityMap terrainMap,
            RoadElevationPlan elevationPlan,
            SurfacePlacementContext surface)
        {
            var chunkRoots = new Dictionary<Vector2Int, Transform>();
            foreach (KeyValuePair<Vector2Int, RoadNetworkCompositionSystemHelper.RoadTileData> entry in network.RoadTiles)
            {
                Vector2Int cell = entry.Key;
                RoadNetworkCompositionSystemHelper.RoadTileData tile = entry.Value;
                bool useAsphaltRoad = network.AutobahnCells.Contains(cell) ||
                                      network.AutobahnConnectorCells.Contains(cell);
                bool useDirtRoad = dirtRoadCells.Contains(cell) &&
                                   !useAsphaltRoad;
                RoadVisualVariantSystem selectedVariants = useAsphaltRoad
                    ? asphaltRoadVariants
                    : useDirtRoad
                        ? dirtRoadVariants
                        : variants;
                RoadVisualVariantSystem.Prefabs selectedPrefabs = useAsphaltRoad
                    ? asphaltRoadPrefabs
                    : useDirtRoad
                        ? dirtRoadPrefabs
                        : prefabs;
                GameObject prefab = selectedVariants.GetPrefab(selectedPrefabs, tile.Type);
                if (prefab == null)
                    continue;

                RoadNetworkCompositionSystemHelper.TileConnectionMask mask = network.GetMask(cell);
                RoadVisualVariantSystem.VariantData variant =
                    selectedVariants.TryGetVariant(tile.Type, mask, out RoadVisualVariantSystem.VariantData selectedVariant)
                        ? selectedVariant
                        : new RoadVisualVariantSystem.VariantData(tile.Rotation, tile.Scale);

                Vector2Int chunkCoordinate = new(
                    Mathf.FloorToInt((float)cell.x / RoadChunkSize),
                    Mathf.FloorToInt((float)cell.y / RoadChunkSize));
                if (!chunkRoots.TryGetValue(chunkCoordinate, out Transform chunkRoot))
                {
                    var chunk = new GameObject($"RoadChunk_{chunkCoordinate.x}_{chunkCoordinate.y}");
                    chunk.transform.SetParent(targetRoot, false);
                    chunkRoot = chunk.transform;
                    chunkRoots.Add(chunkCoordinate, chunkRoot);
                }

                GameObject road = (GameObject)PrefabUtility.InstantiatePrefab(prefab, chunkRoot);
                road.name = $"{prefab.name}_{cell.x}_{cell.y}";
                Vector3 placement = RoadChunkVisualSystem.GetPlacementPosition(placementContext, cell, variant);
                Vector3 samplePoint = placementContext.GridOrigin + new Vector3(
                    (cell.x + 0.5f) * RoadGridSize,
                    0f,
                    (cell.y + 0.5f) * RoadGridSize);
                float fallbackHeight = (surface?.SampleHeight(samplePoint) ?? placement.y) + 0.025f;
                placement.y = elevationPlan.GetElevation(cell, fallbackHeight);
                road.transform.SetPositionAndRotation(placement, variant.Rotation);
                road.transform.localScale = variant.Scale;
                DisableColliders(road);
                float patchHeight = 0.24f;
                if (terrainMap.TryGetRoadPatch(cell, out SurfacePatchEvaluation roadPatch))
                    patchHeight = Mathf.Clamp(placement.y - roadPatch.MinimumHeight + 0.16f, 0.2f, 0.65f);
                CreateNaturalGroundPatch(
                    chunkRoot,
                    $"RoadGroundPatch_{cell.x}_{cell.y}",
                    placement,
                    RoadGridSize * 1.14f,
                    RoadGridSize * 1.14f,
                    patchHeight,
                    HashGroundPatch(cell.x, cell.y, 0x51f2));
            }

            return chunkRoots.Count;
        }

        private static BuildingBakeResult BakeDenseDistricts(
            Transform generatedRoot,
            RuntimeCityRAndDMapView view,
            RuntimeCitySpawnerSystemConfig config,
            Vector3 cityOrigin,
            float cityWidth,
            float cityDepth,
            List<int> streetColumns,
            List<int> streetRows,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            Rect authoredCoreBounds,
            CityFootprint cityFootprint,
            TerrainViabilityMap terrainMap,
            SurfacePlacementContext surface)
        {
            var buildingObject = new GameObject("DenseCity_TightlyPackedUrbanBlocks");
            buildingObject.transform.SetParent(generatedRoot, false);
            var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
            visualSystem.SetRuntimeRoot(buildingObject.transform);
            if (surface != null)
                visualSystem.ConfigureSurface(surface.Surface);
            BuildingPalette palette = BuildPalette(config);
            GridConfig grid = CreateGrid(
                cityOrigin,
                cityWidth,
                cityDepth,
                view.GridCellSize);
            var random = new System.Random(unchecked((int)(config.RandomSeed == 0 ? 26071501u : config.RandomSeed)) ^ 0x1d45ac);
            var placementContext = new BuildingPlacementContext(roadCells, cityOrigin);
            int buildingCount = 0;
            int parkCount = 0;
            int blockIndex = 0;

            for (int columnIndex = 0; columnIndex < streetColumns.Count - 1; columnIndex++)
            {
                for (int rowIndex = 0; rowIndex < streetRows.Count - 1; rowIndex++)
                {
                    Rect block = CreateBlockRect(
                        cityOrigin,
                        streetColumns[columnIndex],
                        streetColumns[columnIndex + 1],
                        streetRows[rowIndex],
                        streetRows[rowIndex + 1],
                        dirtRoadCells);
                    float normalizedDistance = cityFootprint.NormalizedDistance(block.center);
                    if (block.width < 18f ||
                        block.height < 18f ||
                        normalizedDistance > 0.97f ||
                        !cityFootprint.IsAreaClear(block) ||
                        authoredCoreBounds.Contains(block.center))
                        continue;

                    blockIndex++;
                    DistrictZone zone = ClassifyDistrict(normalizedDistance);
                    int parkFrequency = zone == DistrictZone.InnerCity ? 23 : 13;
                    bool park = blockIndex % parkFrequency == 0 ||
                                (zone == DistrictZone.Residential && blockIndex % 37 == 0 && block.width > 55f);
                    if (park)
                    {
                        BuildParkBlock(
                            visualSystem,
                            palette,
                            grid,
                            block,
                            terrainMap,
                            placementContext,
                            addFountain: parkCount % 3 == 0,
                            random);
                        parkCount++;
                        continue;
                    }

                    Vector2 blockCenter = block.center;
                    Vector2 coreCenter = authoredCoreBounds.center;
                    bool bazaar = Vector2.Distance(blockCenter, coreCenter) < 340f ||
                                  (zone == DistrictZone.InnerCity && blockIndex % 11 == 0);
                    buildingCount += BuildUrbanBlock(
                        visualSystem,
                        palette,
                        grid,
                        block,
                        bazaar,
                        zone,
                        terrainMap,
                        placementContext,
                        dirtRoadCells,
                        cityOrigin,
                        random);
                }
            }

            SetStaticRecursively(buildingObject);
            Debug.Log(
                $"[DenseCityBuildingPlacementAudit] reserved={placementContext.ReservedCount} " +
                "buildingOverlaps=0 roadOverlaps=0");
            return new BuildingBakeResult(buildingCount, parkCount);
        }

        private static DistrictZone ClassifyDistrict(float normalizedDistance)
        {
            if (normalizedDistance < 0.27f)
                return DistrictZone.Civic;
            if (normalizedDistance < 0.58f)
                return DistrictZone.InnerCity;
            if (normalizedDistance < 0.8f)
                return DistrictZone.Residential;
            return DistrictZone.Fringe;
        }

        private static Rect CreateBlockRect(
            Vector3 origin,
            int leftStreet,
            int rightStreet,
            int bottomStreet,
            int topStreet,
            HashSet<Vector2Int> dirtRoadCells)
        {
            int middleColumn = (leftStreet + rightStreet) / 2;
            int middleRow = (bottomStreet + topStreet) / 2;
            float leftSetback = ResolveRoadSetback(dirtRoadCells, new Vector2Int(leftStreet, middleRow));
            float rightSetback = ResolveRoadSetback(dirtRoadCells, new Vector2Int(rightStreet, middleRow));
            float bottomSetback = ResolveRoadSetback(dirtRoadCells, new Vector2Int(middleColumn, bottomStreet));
            float topSetback = ResolveRoadSetback(dirtRoadCells, new Vector2Int(middleColumn, topStreet));
            float minX = origin.x + (leftStreet + 1) * RoadGridSize + leftSetback;
            float maxX = origin.x + rightStreet * RoadGridSize - rightSetback;
            float minZ = origin.z + (bottomStreet + 1) * RoadGridSize + bottomSetback;
            float maxZ = origin.z + topStreet * RoadGridSize - topSetback;
            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        private static float ResolveRoadSetback(
            HashSet<Vector2Int> dirtRoadCells,
            Vector2Int roadCell) =>
            dirtRoadCells != null && dirtRoadCells.Contains(roadCell)
                ? DirtBuildingRoadSetback
                : SidewalkBuildingRoadSetback;

        private static int BuildUrbanBlock(
            RuntimeCityVisualPresentationSystemHelper visuals,
            BuildingPalette palette,
            GridConfig grid,
            Rect block,
            bool bazaar,
            DistrictZone zone,
            TerrainViabilityMap terrainMap,
            BuildingPlacementContext placementContext,
            HashSet<Vector2Int> dirtRoadCells,
            Vector3 mapOrigin,
            System.Random random)
        {
            int count = 0;
            count += PlaceHorizontalFrontage(visuals, palette, grid, block, true, 0f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random);
            count += PlaceHorizontalFrontage(visuals, palette, grid, block, false, 180f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random);
            count += PlaceVerticalFrontage(visuals, palette, grid, block, true, 90f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random);
            count += PlaceVerticalFrontage(visuals, palette, grid, block, false, 270f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random);

            Rect interior = Rect.MinMaxRect(
                block.xMin + 8f,
                block.yMin + 8f,
                block.xMax - 8f,
                block.yMax - 8f);
            if (interior.width > 8f && interior.height > 8f)
            {
                float spacing = zone switch
                {
                    DistrictZone.Civic => 7.5f,
                    DistrictZone.InnerCity => 8f,
                    DistrictZone.Residential => 9.5f,
                    _ => 12f
                };
                double skipChance = zone switch
                {
                    DistrictZone.Civic => 0.02d,
                    DistrictZone.InnerCity => 0.06d,
                    DistrictZone.Residential => 0.18d,
                    _ => 0.42d
                };
                for (float z = interior.yMin + 3.5f; z <= interior.yMax - 3f; z += spacing)
                {
                    for (float x = interior.xMin + 3.5f; x <= interior.xMax - 3f; x += spacing)
                    {
                        if (random.NextDouble() < skipChance)
                            continue;
                        bool preferShop = bazaar && random.NextDouble() < 0.6d;
                        PrefabFootprint info = SelectBuilding(palette, preferShop, random);
                        if (info.Prefab == null)
                            continue;
                        float rotation = random.Next(0, 4) * 90f;
                        var center = new Vector2(x, z);
                        if (FitsInsideBlock(info, rotation, center, block) &&
                            DoesNotOverlapDirtRoad(info, rotation, center, dirtRoadCells, mapOrigin) &&
                            SpawnBuilding(visuals, info, new Vector3(x, grid.Origin.y, z), rotation, grid, terrainMap, placementContext))
                            count++;
                    }
                }
            }

            return count;
        }

        private static int PlaceHorizontalFrontage(
            RuntimeCityVisualPresentationSystemHelper visuals,
            BuildingPalette palette,
            GridConfig grid,
            Rect block,
            bool minimumEdge,
            float rotation,
            bool bazaar,
            TerrainViabilityMap terrainMap,
            BuildingPlacementContext placementContext,
            HashSet<Vector2Int> dirtRoadCells,
            Vector3 mapOrigin,
            System.Random random)
        {
            int count = 0;
            float cursor = block.xMin + 4f;
            float limit = block.xMax - 4f;
            while (cursor < limit)
            {
                PrefabFootprint info = SelectBuilding(palette, bazaar, random);
                if (info.Prefab == null)
                    break;
                float width = rotation % 180f == 0f ? info.Width : info.Depth;
                float center = cursor + width * 0.5f;
                if (center + width * 0.5f > limit)
                    break;
                float perpendicularDepth = rotation % 180f == 0f ? info.Depth : info.Width;
                float z = minimumEdge
                    ? block.yMin + perpendicularDepth * 0.5f
                    : block.yMax - perpendicularDepth * 0.5f;
                var worldCenter = new Vector2(center, z);
                if (FitsInsideBlock(info, rotation, worldCenter, block) &&
                    DoesNotOverlapDirtRoad(info, rotation, worldCenter, dirtRoadCells, mapOrigin) &&
                    SpawnBuilding(visuals, info, new Vector3(center, grid.Origin.y, z), rotation, grid, terrainMap, placementContext))
                    count++;
                cursor += width + 0.7f;
            }
            return count;
        }

        private static int PlaceVerticalFrontage(
            RuntimeCityVisualPresentationSystemHelper visuals,
            BuildingPalette palette,
            GridConfig grid,
            Rect block,
            bool minimumEdge,
            float rotation,
            bool bazaar,
            TerrainViabilityMap terrainMap,
            BuildingPlacementContext placementContext,
            HashSet<Vector2Int> dirtRoadCells,
            Vector3 mapOrigin,
            System.Random random)
        {
            int count = 0;
            float cursor = block.yMin + 9f;
            float limit = block.yMax - 9f;
            while (cursor < limit)
            {
                PrefabFootprint info = SelectBuilding(palette, bazaar, random);
                if (info.Prefab == null)
                    break;
                float depth = rotation % 180f == 0f ? info.Depth : info.Width;
                float center = cursor + depth * 0.5f;
                if (center + depth * 0.5f > limit)
                    break;
                float perpendicularWidth = rotation % 180f == 0f ? info.Width : info.Depth;
                float x = minimumEdge
                    ? block.xMin + perpendicularWidth * 0.5f
                    : block.xMax - perpendicularWidth * 0.5f;
                var worldCenter = new Vector2(x, center);
                if (FitsInsideBlock(info, rotation, worldCenter, block) &&
                    DoesNotOverlapDirtRoad(info, rotation, worldCenter, dirtRoadCells, mapOrigin) &&
                    SpawnBuilding(visuals, info, new Vector3(x, grid.Origin.y, center), rotation, grid, terrainMap, placementContext))
                    count++;
                cursor += depth + 0.7f;
            }
            return count;
        }

        private static bool FitsInsideBlock(
            PrefabFootprint info,
            float rotationDegrees,
            Vector2 center,
            Rect block)
        {
            bool quarterTurn = Mathf.RoundToInt(rotationDegrees / 90f) % 2 != 0;
            float halfWidth = (quarterTurn ? info.Depth : info.Width) * 0.5f;
            float halfDepth = (quarterTurn ? info.Width : info.Depth) * 0.5f;
            const float tolerance = 0.01f;
            return center.x - halfWidth >= block.xMin - tolerance &&
                   center.x + halfWidth <= block.xMax + tolerance &&
                   center.y - halfDepth >= block.yMin - tolerance &&
                   center.y + halfDepth <= block.yMax + tolerance;
        }

        private static bool DoesNotOverlapDirtRoad(
            PrefabFootprint info,
            float rotationDegrees,
            Vector2 center,
            HashSet<Vector2Int> dirtRoadCells,
            Vector3 mapOrigin)
        {
            if (dirtRoadCells == null || dirtRoadCells.Count == 0)
                return true;

            bool quarterTurn = Mathf.RoundToInt(rotationDegrees / 90f) % 2 != 0;
            float halfWidth = (quarterTurn ? info.Depth : info.Width) * 0.5f;
            float halfDepth = (quarterTurn ? info.Width : info.Depth) * 0.5f;
            float minX = center.x - halfWidth;
            float maxX = center.x + halfWidth;
            float minZ = center.y - halfDepth;
            float maxZ = center.y + halfDepth;
            int minimumColumn = Mathf.FloorToInt((minX - DirtBuildingRoadSetback - mapOrigin.x) / RoadGridSize);
            int maximumColumn = Mathf.FloorToInt((maxX + DirtBuildingRoadSetback - mapOrigin.x) / RoadGridSize);
            int minimumRow = Mathf.FloorToInt((minZ - DirtBuildingRoadSetback - mapOrigin.z) / RoadGridSize);
            int maximumRow = Mathf.FloorToInt((maxZ + DirtBuildingRoadSetback - mapOrigin.z) / RoadGridSize);

            for (int column = minimumColumn; column <= maximumColumn; column++)
            {
                for (int row = minimumRow; row <= maximumRow; row++)
                {
                    if (!dirtRoadCells.Contains(new Vector2Int(column, row)))
                        continue;

                    float roadMinX = mapOrigin.x + column * RoadGridSize - DirtBuildingRoadSetback;
                    float roadMaxX = mapOrigin.x + (column + 1) * RoadGridSize + DirtBuildingRoadSetback;
                    float roadMinZ = mapOrigin.z + row * RoadGridSize - DirtBuildingRoadSetback;
                    float roadMaxZ = mapOrigin.z + (row + 1) * RoadGridSize + DirtBuildingRoadSetback;
                    if (maxX > roadMinX && minX < roadMaxX && maxZ > roadMinZ && minZ < roadMaxZ)
                        return false;
                }
            }

            return true;
        }

        private static bool SpawnBuilding(
            RuntimeCityVisualPresentationSystemHelper visuals,
            PrefabFootprint info,
            Vector3 center,
            float rotationDegrees,
            GridConfig grid,
            TerrainViabilityMap terrainMap,
            BuildingPlacementContext placementContext = null)
        {
            bool quarterTurn = Mathf.RoundToInt(rotationDegrees / 90f) % 2 != 0;
            float worldWidth = quarterTurn ? info.Depth : info.Width;
            float worldDepth = quarterTurn ? info.Width : info.Depth;
            var worldCenter = new Vector2(center.x, center.z);
            if (placementContext != null &&
                !placementContext.CanPlace(info, rotationDegrees, worldCenter))
            {
                return false;
            }

            if (!terrainMap.TryEvaluateBuilding(
                    worldCenter,
                    worldWidth,
                    worldDepth,
                    out SurfacePatchEvaluation patch) ||
                !terrainMap.CanPlaceBuilding(patch))
            {
                return false;
            }

            int footprintX = Mathf.Max(1, Mathf.CeilToInt(quarterTurn ? info.Depth : info.Width));
            int footprintZ = Mathf.Max(1, Mathf.CeilToInt(quarterTurn ? info.Width : info.Depth));
            int originX = Mathf.Clamp(
                Mathf.RoundToInt(center.x - grid.Origin.x - footprintX * 0.5f),
                0,
                grid.Width - footprintX);
            int originZ = Mathf.Clamp(
                Mathf.RoundToInt(center.z - grid.Origin.z - footprintZ * 0.5f),
                0,
                grid.Height - footprintZ);
            GameObject wrapper = visuals.SpawnVisualOnlyPrefab(
                info.Prefab,
                new Vector2Int(originX, originZ),
                new Vector2Int(footprintX, footprintZ),
                Quaternion.Euler(0f, rotationDegrees, 0f),
                grid);
            if (wrapper == null)
                return false;

            float foundationHeight = patch.MaximumHeight + 0.035f;
            Vector3 wrapperPosition = wrapper.transform.position;
            wrapperPosition.y = foundationHeight;
            wrapper.transform.position = wrapperPosition;
            wrapper.transform.localScale = Vector3.one * info.VisualScale;
            if (placementContext != null)
            {
                if (!TryGetWorldBounds(wrapper.transform, out Bounds actualBounds) ||
                    !placementContext.TryReserve(actualBounds))
                {
                    UnityEngine.Object.DestroyImmediate(wrapper);
                    return false;
                }
            }

            CreateBuildingGroundPatch(
                wrapper,
                worldWidth,
                worldDepth,
                patch,
                foundationHeight);
            return true;
        }

        private static int AddShopRoofDetails(Transform generatedRoot)
        {
            GameObject roofCapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoofCap03PrefabPath);
            if (roofCapPrefab == null)
                throw new InvalidOperationException($"Missing roof-cap prefab {RoofCap03PrefabPath}.");

            var detailRootObject = new GameObject("DenseCity_ShopRoofDetails");
            detailRootObject.transform.SetParent(generatedRoot, false);
            int count = AddRoofCapsForShop(
                generatedRoot,
                detailRootObject.transform,
                roofCapPrefab,
                "SM_Bld_Shop_04_Visual",
                "SM_Bld_Roof_Cap_03 (3)",
                int.MaxValue);
            count += AddRoofCapsForShop(
                generatedRoot,
                detailRootObject.transform,
                roofCapPrefab,
                "SM_Bld_Shop_08_Visual",
                "SM_Bld_Roof_Cap_03 (2)",
                int.MaxValue);
            SetStaticRecursively(detailRootObject);
            return count;
        }

        private static int AddRoofCapsForShop(
            Transform generatedRoot,
            Transform detailRoot,
            GameObject roofCapPrefab,
            string shopWrapperName,
            string roofCapName,
            int maximumCount)
        {
            var candidates = new List<Transform>();
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate != null && candidate.name == shopWrapperName)
                    candidates.Add(candidate);
            }

            candidates.Sort((left, right) =>
            {
                uint leftHash = HashGroundPatch(
                    Mathf.RoundToInt(left.position.x * 10f),
                    Mathf.RoundToInt(left.position.z * 10f),
                    0x73c1);
                uint rightHash = HashGroundPatch(
                    Mathf.RoundToInt(right.position.x * 10f),
                    Mathf.RoundToInt(right.position.z * 10f),
                    0x73c1);
                return leftHash.CompareTo(rightHash);
            });

            int count = Mathf.Min(maximumCount, candidates.Count);
            for (int index = 0; index < count; index++)
            {
                Transform shop = candidates[index];
                if (!TryGetWorldBounds(shop, out Bounds shopBounds))
                    continue;

                GameObject roofCap = (GameObject)PrefabUtility.InstantiatePrefab(roofCapPrefab, detailRoot);
                roofCap.name = $"{roofCapName}_{index:00}";
                roofCap.transform.SetPositionAndRotation(
                    new Vector3(shopBounds.center.x, 0f, shopBounds.center.z),
                    shop.rotation);
                float scale = Mathf.Max(0.01f, shop.lossyScale.x);
                roofCap.transform.localScale = Vector3.one * scale;
                if (!TryGetRendererBounds(roofCap, out Bounds capBounds))
                    throw new InvalidOperationException($"Roof-cap prefab '{roofCapPrefab.name}' has no renderer bounds.");
                Vector3 position = roofCap.transform.position;
                position.y += shopBounds.max.y + 0.02f - capBounds.min.y;
                roofCap.transform.position = position;
                DisableColliders(roofCap);
            }

            return count;
        }

        private static int AddOpenGroundDetails(
            Transform generatedRoot,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            float gradeElevation,
            uint seed)
        {
            var rootObject = new GameObject("DenseCity_OpenGroundRoundDetails");
            rootObject.transform.SetParent(generatedRoot, false);
            List<Rect> buildingFootprints = CollectGeneratedBuildingFootprints(generatedRoot);
            int count = 0;
            const float spacing = 10f;
            for (float z = spacing * 0.5f; z < mapDepth; z += spacing)
            {
                for (float x = spacing * 0.5f; x < mapWidth; x += spacing)
                {
                    uint cellHash = HashGroundPatch(
                        Mathf.RoundToInt(x / spacing),
                        Mathf.RoundToInt(z / spacing),
                        unchecked((int)seed) ^ 0x1ac7);
                    float jitterX = Mathf.Lerp(-3f, 3f, Hash01(cellHash ^ 0xb6312a17u));
                    float jitterZ = Mathf.Lerp(-3f, 3f, Hash01(cellHash ^ 0x47d3c985u));
                    Vector2 point = new(mapOrigin.x + x + jitterX, mapOrigin.z + z + jitterZ);
                    if (!cityFootprint.Contains(point, 0.025f))
                        continue;

                    uint hash = HashGroundPatch(
                        Mathf.RoundToInt(point.x),
                        Mathf.RoundToInt(point.y),
                        unchecked((int)seed) ^ 0x691d);
                    float width = Mathf.Lerp(6f, 12f, Hash01(hash ^ 0x8d21f31bu));
                    float depth = Mathf.Lerp(6f, 12f, Hash01(hash ^ 0x2c71e4a9u));
                    var patchBounds = new Rect(
                        point.x - width * 0.5f,
                        point.y - depth * 0.5f,
                        width,
                        depth);
                    if (!cityFootprint.IsAreaClear(point, width * 0.5f, depth * 0.5f) ||
                        patchBounds.Overlaps(authoredCoreBounds) ||
                        OverlapsGeneratedBuilding(patchBounds, buildingFootprints) ||
                        OverlapsRoadCell(patchBounds, roadCells, mapOrigin))
                    {
                        continue;
                    }

                    float visibleRelief = Mathf.Lerp(0.4f, 0.9f, Hash01(hash ^ 0xd04c39a7u));
                    float patchHeight = Mathf.Lerp(1.1f, 2.2f, Hash01(hash ^ 0x36eb52d1u));
                    CreateNaturalGroundPatch(
                        rootObject.transform,
                        $"SM_Env_Ground_Round_01_Open_{count:0000}",
                        new Vector3(point.x, gradeElevation + visibleRelief + 0.025f, point.y),
                        width,
                        depth,
                        patchHeight,
                        hash,
                        forcePrimaryGroundPrefab: true);
                    count++;
                }
            }

            SetStaticRecursively(rootObject);
            return count;
        }

        private static List<Rect> CollectGeneratedBuildingFootprints(Transform generatedRoot)
        {
            var footprints = new List<Rect>();
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform wrapper = transforms[index];
                if (wrapper == null ||
                    wrapper.parent == null ||
                    wrapper.parent.name != "RuntimeCityVisuals" ||
                    !wrapper.name.EndsWith("_Visual", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(wrapper, out Bounds bounds))
                {
                    continue;
                }

                const float clearance = 0.8f;
                footprints.Add(Rect.MinMaxRect(
                    bounds.min.x - clearance,
                    bounds.min.z - clearance,
                    bounds.max.x + clearance,
                    bounds.max.z + clearance));
            }

            return footprints;
        }

        private static bool OverlapsGeneratedBuilding(Rect patchBounds, List<Rect> buildingFootprints)
        {
            for (int index = 0; index < buildingFootprints.Count; index++)
            {
                if (patchBounds.Overlaps(buildingFootprints[index]))
                    return true;
            }

            return false;
        }

        private static bool OverlapsRoadCell(
            Rect patchBounds,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin)
        {
            int minX = Mathf.FloorToInt((patchBounds.xMin - mapOrigin.x) / RoadGridSize);
            int maxX = Mathf.FloorToInt((patchBounds.xMax - mapOrigin.x) / RoadGridSize);
            int minZ = Mathf.FloorToInt((patchBounds.yMin - mapOrigin.z) / RoadGridSize);
            int maxZ = Mathf.FloorToInt((patchBounds.yMax - mapOrigin.z) / RoadGridSize);
            for (int z = minZ; z <= maxZ; z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (roadCells.Contains(new Vector2Int(x, z)))
                        return true;
                }
            }

            return false;
        }

        private static UrbanDetailResult AddUrbanDetailProps(
            Transform generatedRoot,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            float gradeElevation,
            uint seed)
        {
            List<GeneratedBuildingInfo> buildings = CollectGeneratedBuildings(generatedRoot);
            GameObject[] waterTanks = LoadRequiredPrefabs(RooftopWaterTankPrefabPaths);
            GameObject[] streetProps = LoadRequiredPrefabs(StreetPropPrefabPaths);
            GameObject[] trees = LoadRequiredPrefabs(DenseTreePrefabPaths);
            GameObject[] rocks = LoadRequiredPrefabs(UrbanRockPrefabPaths);
            GameObject courtyardWall = LoadRequiredPrefab(CourtyardWallPrefabPath);
            GameObject courtyardPillar = LoadRequiredPrefab(CourtyardPillarPrefabPath);
            GameObject courtyardWell = LoadRequiredPrefab(CourtyardWellPrefabPath);
            GameObject courtyardBush = LoadRequiredPrefab(CourtyardBushPrefabPath);
            GameObject powerPole = LoadRequiredPrefab(PowerPolePrefabPath);
            GameObject powerLine = LoadRequiredPrefab(PowerLinePrefabPath);
            GameObject streetLight = LoadRequiredPrefab(StreetLightPrefabPath);
            GameObject grass = LoadRequiredPrefab(GrassPrefabPath);
            GameObject mainStreetBush = LoadRequiredPrefab(MainStreetBushPrefabPath);

            var rooftopRootObject = new GameObject("DenseCity_RooftopWaterTanks");
            rooftopRootObject.transform.SetParent(generatedRoot, false);
            var streetPropRootObject = new GameObject("DenseCity_GroundedStreetProps");
            streetPropRootObject.transform.SetParent(generatedRoot, false);
            var treeRootObject = new GameObject("DenseCity_DenseTreeClusters");
            treeRootObject.transform.SetParent(generatedRoot, false);
            var rockRootObject = new GameObject("DenseCity_UrbanRocks");
            rockRootObject.transform.SetParent(generatedRoot, false);
            var courtyardRootObject = new GameObject("DenseCity_HouseCourtyards");
            courtyardRootObject.transform.SetParent(generatedRoot, false);
            var utilityRootObject = new GameObject("DenseCity_RoadsidePowerNetwork");
            utilityRootObject.transform.SetParent(generatedRoot, false);
            var streetLightRootObject = new GameObject("DenseCity_SidewalkStreetLights");
            streetLightRootObject.transform.SetParent(generatedRoot, false);
            var grassRootObject = new GameObject("DenseCity_FreeGroundGrass");
            grassRootObject.transform.SetParent(generatedRoot, false);
            var mainStreetBushRootObject = new GameObject("DenseCity_MainStreetBushes");
            mainStreetBushRootObject.transform.SetParent(generatedRoot, false);

            int waterTankCount = AddRooftopWaterTanks(
                rooftopRootObject.transform,
                buildings,
                waterTanks,
                seed);
            CourtyardDetailResult courtyardDetails = AddHouseCourtyards(
                courtyardRootObject.transform,
                buildings,
                courtyardWall,
                courtyardPillar,
                courtyardWell,
                courtyardBush,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                mapOrigin,
                gradeElevation,
                seed);
            var reservedDetailAreas = new List<Rect>(courtyardDetails.ReservedAreas);
            var landscapingDetails = new LandscapingDetailResult();
            AddMainStreetBushes(
                mainStreetBushRootObject.transform,
                buildings,
                mainStreetBush,
                mapOrigin,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                dirtRoadCells,
                reservedDetailAreas,
                landscapingDetails,
                gradeElevation,
                seed);
            reservedDetailAreas.AddRange(landscapingDetails.ReservedAreas);
            UtilityDetailResult utilityDetails = AddRoadsidePowerNetwork(
                utilityRootObject.transform,
                buildings,
                powerPole,
                powerLine,
                mapOrigin,
                mapWidth,
                mapDepth,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                dirtRoadCells,
                reservedDetailAreas,
                gradeElevation,
                seed);
            reservedDetailAreas.AddRange(utilityDetails.ReservedAreas);
            StreetLightDetailResult streetLightDetails = AddSidewalkStreetLights(
                streetLightRootObject.transform,
                buildings,
                streetLight,
                mapOrigin,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                dirtRoadCells,
                reservedDetailAreas,
                gradeElevation,
                seed);
            reservedDetailAreas.AddRange(streetLightDetails.ReservedAreas);
            int bushReservedAreaCount = landscapingDetails.ReservedAreas.Count;
            AddFreeGroundGrass(
                grassRootObject.transform,
                buildings,
                grass,
                mapOrigin,
                mapWidth,
                mapDepth,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                reservedDetailAreas,
                landscapingDetails,
                gradeElevation,
                seed);
            for (int index = bushReservedAreaCount; index < landscapingDetails.ReservedAreas.Count; index++)
                reservedDetailAreas.Add(landscapingDetails.ReservedAreas[index]);
            int streetPropCount = AddGroundedBuildingProps(
                streetPropRootObject.transform,
                buildings,
                streetProps,
                authoredCoreBounds.center,
                roadCells,
                reservedDetailAreas,
                mapOrigin,
                gradeElevation,
                seed);
            (int treeCount, int rockCount) = AddDenseTreeAndRockClusters(
                treeRootObject.transform,
                rockRootObject.transform,
                buildings,
                trees,
                rocks,
                mapOrigin,
                mapWidth,
                mapDepth,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                reservedDetailAreas,
                gradeElevation,
                seed);

            ValidateNoRoadOverlappingDetails(streetPropRootObject.transform, roadCells, mapOrigin);
            ValidateNoRoadOverlappingDetails(rockRootObject.transform, roadCells, mapOrigin);
            ValidateNoRoadOverlappingDetails(grassRootObject.transform, roadCells, mapOrigin);
            ValidateNoRoadOverlappingDetails(mainStreetBushRootObject.transform, roadCells, mapOrigin);

            SetStaticRecursively(rooftopRootObject);
            SetStaticRecursively(streetPropRootObject);
            SetStaticRecursively(treeRootObject);
            SetStaticRecursively(rockRootObject);
            SetStaticRecursively(courtyardRootObject);
            SetStaticRecursively(utilityRootObject);
            SetStaticRecursively(streetLightRootObject);
            SetStaticRecursively(grassRootObject);
            SetStaticRecursively(mainStreetBushRootObject);
            return new UrbanDetailResult(
                waterTankCount,
                streetPropCount,
                treeCount,
                rockCount,
                courtyardDetails.Courtyards,
                courtyardDetails.Walls,
                courtyardDetails.Pillars,
                courtyardDetails.Wells,
                courtyardDetails.Bushes,
                courtyardDetails.GroundPatchesRemoved,
                utilityDetails.Poles,
                utilityDetails.Lines,
                streetLightDetails.Lights,
                landscapingDetails.GrassPatches,
                landscapingDetails.MainStreetBushes);
        }

        private sealed class UtilityDetailResult
        {
            public readonly List<Rect> ReservedAreas = new();
            public int Poles;
            public int Lines;
        }

        private readonly struct UtilityPolePoint
        {
            public readonly Vector3 Position;
            public readonly float WireHeight;
            public readonly int Side;

            public UtilityPolePoint(Vector3 position, float wireHeight, int side)
            {
                Position = position;
                WireHeight = wireHeight;
                Side = side;
            }
        }

        private readonly struct UtilityPoleCandidate
        {
            public readonly Vector2 Position;
            public readonly int Side;

            public UtilityPoleCandidate(Vector2 position, int side)
            {
                Position = position;
                Side = side;
            }
        }

        private static UtilityDetailResult AddRoadsidePowerNetwork(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject polePrefab,
            GameObject linePrefab,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            List<Rect> reservedAreas,
            float gradeElevation,
            uint seed)
        {
            var result = new UtilityDetailResult();
            int maximumColumn = Mathf.FloorToInt(mapWidth / RoadGridSize) - 1;
            int maximumRow = Mathf.FloorToInt(mapDepth / RoadGridSize) - 1;
            var utilityRows = new HashSet<int>();
            var utilityColumns = new HashSet<int>();
            foreach (Vector2Int cell in roadCells)
            {
                if (roadCells.Contains(cell + Vector2Int.left) || roadCells.Contains(cell + Vector2Int.right))
                    utilityRows.Add(cell.y);
                if (roadCells.Contains(cell + Vector2Int.down) || roadCells.Contains(cell + Vector2Int.up))
                    utilityColumns.Add(cell.x);
            }
            var sortedRows = new List<int>(utilityRows);
            var sortedColumns = new List<int>(utilityColumns);
            sortedRows.Sort();
            sortedColumns.Sort();

            for (int rowIndex = 0; rowIndex < sortedRows.Count; rowIndex++)
            {
                int row = sortedRows[rowIndex];
                uint corridorHash = HashGroundPatch(row, rowIndex, unchecked((int)seed) ^ 0x3c71);
                AddPowerCorridor(
                    parent,
                    buildings,
                    polePrefab,
                    linePrefab,
                    mapOrigin,
                    cityFootprint,
                    authoredCoreBounds,
                    roadCells,
                    dirtRoadCells,
                    reservedAreas,
                    result,
                    horizontal: true,
                    fixedCoordinate: row,
                    maximumAlongCoordinate: maximumColumn,
                    gradeElevation,
                    corridorHash);
            }

            for (int columnIndex = 0; columnIndex < sortedColumns.Count; columnIndex++)
            {
                int column = sortedColumns[columnIndex];
                uint corridorHash = HashGroundPatch(column, columnIndex, unchecked((int)seed) ^ 0x56a9);
                AddPowerCorridor(
                    parent,
                    buildings,
                    polePrefab,
                    linePrefab,
                    mapOrigin,
                    cityFootprint,
                    authoredCoreBounds,
                    roadCells,
                    dirtRoadCells,
                    reservedAreas,
                    result,
                    horizontal: false,
                    fixedCoordinate: column,
                    maximumAlongCoordinate: maximumRow,
                    gradeElevation,
                    corridorHash);
            }

            return result;
        }

        private static void AddPowerCorridor(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject polePrefab,
            GameObject linePrefab,
            Vector3 mapOrigin,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            List<Rect> courtyardAreas,
            UtilityDetailResult result,
            bool horizontal,
            int fixedCoordinate,
            int maximumAlongCoordinate,
            float gradeElevation,
            uint corridorHash)
        {
            int preferredSide = Hash01(corridorHash ^ 0x6729c4f1u) < 0.5f ? -1 : 1;
            int runStart = -1;
            for (int along = 1; along <= maximumAlongCoordinate; along++)
            {
                Vector2Int cell = horizontal
                    ? new Vector2Int(along, fixedCoordinate)
                    : new Vector2Int(fixedCoordinate, along);
                bool hasRoad = along < maximumAlongCoordinate && roadCells.Contains(cell);
                if (hasRoad)
                {
                    if (runStart < 0)
                        runStart = along;
                    continue;
                }
                if (runStart >= 0)
                    AddPowerRoadRun(
                        parent,
                        buildings,
                        polePrefab,
                        linePrefab,
                        mapOrigin,
                        cityFootprint,
                        authoredCoreBounds,
                        roadCells,
                        dirtRoadCells,
                        courtyardAreas,
                        result,
                        horizontal,
                        fixedCoordinate,
                        runStart,
                        along - 1,
                        preferredSide,
                        gradeElevation);
                runStart = -1;
            }
        }

        private static void AddPowerRoadRun(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject polePrefab,
            GameObject linePrefab,
            Vector3 mapOrigin,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            List<Rect> courtyardAreas,
            UtilityDetailResult result,
            bool horizontal,
            int fixedCoordinate,
            int runStart,
            int runEnd,
            int preferredSide,
            float gradeElevation)
        {
            if (runEnd - runStart < 2)
                return;

            var chain = new List<UtilityPoleCandidate>();
            for (int along = runStart; along <= runEnd; along += 2)
            {
                Vector2Int cell = horizontal
                    ? new Vector2Int(along, fixedCoordinate)
                    : new Vector2Int(fixedCoordinate, along);
                if (IsRoadJunction(cell, horizontal, roadCells))
                    continue;

                Vector2 roadCenter = RoadCellWorldCenter(cell, mapOrigin);
                bool isDirtRoad = dirtRoadCells.Contains(cell);
                float roadsideOffset = isDirtRoad ? 4.65f : 4.25f;
                if (!TryResolvePowerPolePosition(
                        roadCenter,
                        horizontal,
                        preferredSide,
                        roadsideOffset,
                        cityFootprint,
                        authoredCoreBounds,
                        buildings,
                        courtyardAreas,
                        result.ReservedAreas,
                        out Vector2 polePosition,
                        out int resolvedSide))
                {
                    continue;
                }

                if (chain.Count > 0)
                {
                    UtilityPoleCandidate previous = chain[chain.Count - 1];
                    float gap = Vector2.Distance(previous.Position, polePosition);
                    if (previous.Side != resolvedSide || gap > 45f)
                    {
                        FlushPowerCandidateChain(
                            parent,
                            polePrefab,
                            linePrefab,
                            chain,
                            horizontal,
                            gradeElevation,
                            result);
                        chain.Clear();
                    }
                }

                chain.Add(new UtilityPoleCandidate(polePosition, resolvedSide));
            }

            if ((runEnd - runStart) % 2 != 0)
            {
                int along = runEnd;
                Vector2Int cell = horizontal
                    ? new Vector2Int(along, fixedCoordinate)
                    : new Vector2Int(fixedCoordinate, along);
                if (!IsRoadJunction(cell, horizontal, roadCells))
                {
                    Vector2 roadCenter = RoadCellWorldCenter(cell, mapOrigin);
                    float roadsideOffset = dirtRoadCells.Contains(cell) ? 4.65f : 4.25f;
                    if (TryResolvePowerPolePosition(
                            roadCenter,
                            horizontal,
                            preferredSide,
                            roadsideOffset,
                            cityFootprint,
                            authoredCoreBounds,
                            buildings,
                            courtyardAreas,
                            result.ReservedAreas,
                            out Vector2 polePosition,
                            out int resolvedSide))
                    {
                        if (chain.Count > 0)
                        {
                            UtilityPoleCandidate previous = chain[chain.Count - 1];
                            float gap = Vector2.Distance(previous.Position, polePosition);
                            if (previous.Side != resolvedSide || gap > 45f)
                            {
                                FlushPowerCandidateChain(
                                    parent,
                                    polePrefab,
                                    linePrefab,
                                    chain,
                                    horizontal,
                                    gradeElevation,
                                    result);
                                chain.Clear();
                            }
                        }

                        if (chain.Count == 0 || Vector2.Distance(chain[chain.Count - 1].Position, polePosition) >= 9f)
                            chain.Add(new UtilityPoleCandidate(polePosition, resolvedSide));
                    }
                }
            }

            FlushPowerCandidateChain(
                parent,
                polePrefab,
                linePrefab,
                chain,
                horizontal,
                gradeElevation,
                result);
        }

        private static void FlushPowerCandidateChain(
            Transform parent,
            GameObject polePrefab,
            GameObject linePrefab,
            List<UtilityPoleCandidate> candidates,
            bool horizontal,
            float gradeElevation,
            UtilityDetailResult result)
        {
            if (candidates.Count < 2)
                return;

            UtilityPolePoint? previousPole = null;
            float poleRotation = horizontal ? 90f : 0f;
            for (int index = 0; index < candidates.Count; index++)
            {
                UtilityPoleCandidate candidate = candidates[index];
                if (!InstantiateGroundedPowerPole(
                        polePrefab,
                        parent,
                        $"{polePrefab.name}_Roadside_{result.Poles:0000}",
                        candidate.Position,
                        gradeElevation + 0.02f,
                        poleRotation,
                        out UtilityPolePoint polePoint,
                        candidate.Side))
                {
                    previousPole = null;
                    continue;
                }

                result.ReservedAreas.Add(new Rect(
                    candidate.Position.x - 0.85f,
                    candidate.Position.y - 0.85f,
                    1.7f,
                    1.7f));
                result.Poles++;
                if (previousPole.HasValue &&
                    InstantiatePowerLineSpan(
                        linePrefab,
                        parent,
                        $"{linePrefab.name}_Roadside_{result.Lines:0000}",
                        previousPole.Value,
                        polePoint))
                {
                    result.Lines++;
                }

                previousPole = polePoint;
            }
        }

        private static bool IsRoadJunction(
            Vector2Int cell,
            bool horizontal,
            HashSet<Vector2Int> roadCells)
        {
            Vector2Int across = horizontal ? Vector2Int.up : Vector2Int.right;
            return roadCells.Contains(cell - across) || roadCells.Contains(cell + across);
        }

        private static bool TryResolvePowerPolePosition(
            Vector2 roadCenter,
            bool horizontal,
            int preferredSide,
            float roadsideOffset,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            List<GeneratedBuildingInfo> buildings,
            List<Rect> courtyardAreas,
            List<Rect> poleAreas,
            out Vector2 position,
            out int resolvedSide)
        {
            Vector2 perpendicular = horizontal ? Vector2.up : Vector2.right;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                resolvedSide = attempt == 0 ? preferredSide : -preferredSide;
                position = roadCenter + perpendicular * (roadsideOffset * resolvedSide);
                var clearance = new Rect(position.x - 0.75f, position.y - 0.75f, 1.5f, 1.5f);
                if (cityFootprint.Contains(position, 0.025f) &&
                    !authoredCoreBounds.Overlaps(clearance) &&
                    !OverlapsAnyBuilding(clearance, buildings) &&
                    !OverlapsAnyRect(clearance, courtyardAreas) &&
                    !OverlapsAnyRect(clearance, poleAreas))
                {
                    return true;
                }
            }

            position = default;
            resolvedSide = 0;
            return false;
        }

        private static bool InstantiateGroundedPowerPole(
            GameObject prefab,
            Transform parent,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotation,
            out UtilityPolePoint polePoint,
            int side)
        {
            GameObject pole = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (pole == null)
            {
                polePoint = default;
                return false;
            }

            pole.name = objectName;
            pole.transform.SetPositionAndRotation(
                new Vector3(position.x, 0f, position.y),
                Quaternion.Euler(0f, rotation, 0f));
            if (!TryGetRendererBounds(pole, out Bounds bounds))
            {
                UnityEngine.Object.DestroyImmediate(pole);
                polePoint = default;
                return false;
            }

            Vector3 groundedPosition = pole.transform.position;
            groundedPosition.y += supportHeight - bounds.min.y;
            pole.transform.position = groundedPosition;
            if (!TryGetRendererBounds(pole, out bounds))
            {
                UnityEngine.Object.DestroyImmediate(pole);
                polePoint = default;
                return false;
            }

            DisableColliders(pole);
            polePoint = new UtilityPolePoint(pole.transform.position, bounds.max.y - 0.12f, side);
            return true;
        }

        private static bool InstantiatePowerLineSpan(
            GameObject prefab,
            Transform parent,
            string objectName,
            UtilityPolePoint start,
            UtilityPolePoint end)
        {
            Vector3 direction = end.Position - start.Position;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance < 0.1f)
                return false;

            GameObject line = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (line == null)
                return false;

            line.name = objectName;
            line.transform.SetPositionAndRotation(
                new Vector3(start.Position.x, (start.WireHeight + end.WireHeight) * 0.5f, start.Position.z),
                Quaternion.identity);
            line.transform.localScale = Vector3.one;
            if (!TryGetRendererBounds(line, out Bounds sourceBounds))
            {
                UnityEngine.Object.DestroyImmediate(line);
                return false;
            }

            float sourceLength = Mathf.Max(0.1f, sourceBounds.size.z);
            line.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            line.transform.localScale = new Vector3(1f, 1f, distance / sourceLength);
            DisableColliders(line);
            return true;
        }

        private sealed class StreetLightDetailResult
        {
            public readonly List<Rect> ReservedAreas = new();
            public int Lights;
        }

        private static StreetLightDetailResult AddSidewalkStreetLights(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject lightPrefab,
            Vector3 mapOrigin,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            List<Rect> reservedAreas,
            float gradeElevation,
            uint seed)
        {
            var result = new StreetLightDetailResult();
            var sortedRoadCells = new List<Vector2Int>(roadCells);
            sortedRoadCells.Sort((left, right) =>
            {
                int rowComparison = left.y.CompareTo(right.y);
                return rowComparison != 0 ? rowComparison : left.x.CompareTo(right.x);
            });

            for (int index = 0; index < sortedRoadCells.Count; index++)
            {
                Vector2Int cell = sortedRoadCells[index];
                if (dirtRoadCells.Contains(cell))
                    continue;

                bool left = roadCells.Contains(cell + Vector2Int.left);
                bool right = roadCells.Contains(cell + Vector2Int.right);
                bool down = roadCells.Contains(cell + Vector2Int.down);
                bool up = roadCells.Contains(cell + Vector2Int.up);
                bool horizontal = left && right && !down && !up;
                bool vertical = down && up && !left && !right;
                if (!horizontal && !vertical)
                    continue;

                int along = horizontal ? cell.x : cell.y;
                int fixedCoordinate = horizontal ? cell.y : cell.x;
                uint corridorHash = HashGroundPatch(
                    fixedCoordinate,
                    horizontal ? 1 : 2,
                    unchecked((int)seed) ^ 0x64d3);
                int phase = (int)(corridorHash % 3u);
                if (along % 3 != phase)
                    continue;

                int sequence = (along - phase) / 3;
                int preferredSide = ((sequence + fixedCoordinate) & 1) == 0 ? -1 : 1;
                Vector2 roadCenter = RoadCellWorldCenter(cell, mapOrigin);
                if (!TryResolveStreetLightPosition(
                        roadCenter,
                        horizontal,
                        preferredSide,
                        cityFootprint,
                        authoredCoreBounds,
                        buildings,
                        reservedAreas,
                        result.ReservedAreas,
                        out Vector2 lightPosition,
                        out int resolvedSide))
                {
                    continue;
                }

                float rotation = horizontal
                    ? resolvedSide > 0 ? 180f : 0f
                    : resolvedSide > 0 ? 270f : 90f;
                if (!InstantiateGroundedDetail(
                        lightPrefab,
                        parent,
                        $"{lightPrefab.name}_Sidewalk_{result.Lights:0000}",
                        lightPosition,
                        gradeElevation + 0.025f,
                        rotation,
                        1f))
                {
                    continue;
                }

                var occupiedArea = new Rect(
                    lightPosition.x - 0.7f,
                    lightPosition.y - 0.7f,
                    1.4f,
                    1.4f);
                result.ReservedAreas.Add(occupiedArea);
                result.Lights++;
            }

            return result;
        }

        private static bool TryResolveStreetLightPosition(
            Vector2 roadCenter,
            bool horizontal,
            int preferredSide,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            List<GeneratedBuildingInfo> buildings,
            List<Rect> reservedAreas,
            List<Rect> lightAreas,
            out Vector2 position,
            out int resolvedSide)
        {
            const float sidewalkOffset = 3.8f;
            Vector2 perpendicular = horizontal ? Vector2.up : Vector2.right;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                resolvedSide = attempt == 0 ? preferredSide : -preferredSide;
                position = roadCenter + perpendicular * (sidewalkOffset * resolvedSide);
                var clearance = new Rect(position.x - 0.7f, position.y - 0.7f, 1.4f, 1.4f);
                if (cityFootprint.Contains(position, 0.025f) &&
                    !authoredCoreBounds.Overlaps(clearance) &&
                    !OverlapsAnyBuilding(clearance, buildings) &&
                    !OverlapsAnyRect(clearance, reservedAreas) &&
                    !OverlapsAnyRect(clearance, lightAreas))
                {
                    return true;
                }
            }

            position = default;
            resolvedSide = 0;
            return false;
        }

        private sealed class LandscapingDetailResult
        {
            public readonly List<Rect> ReservedAreas = new();
            public int GrassPatches;
            public int MainStreetBushes;
        }

        private static void AddFreeGroundGrass(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject prefab,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            LandscapingDetailResult result,
            float gradeElevation,
            uint seed)
        {
            const float spacing = 14f;
            for (float z = spacing * 0.5f; z < mapDepth; z += spacing)
            {
                for (float x = spacing * 0.5f; x < mapWidth; x += spacing)
                {
                    uint hash = HashGroundPatch(
                        Mathf.RoundToInt(x / spacing),
                        Mathf.RoundToInt(z / spacing),
                        unchecked((int)seed) ^ 0x5b27);
                    var position = new Vector2(
                        mapOrigin.x + x + Mathf.Lerp(-4.5f, 4.5f, Hash01(hash ^ 0x72a91c4du)),
                        mapOrigin.z + z + Mathf.Lerp(-4.5f, 4.5f, Hash01(hash ^ 0xc18f3a27u)));
                    if (!cityFootprint.Contains(position, 0.025f) ||
                        !cityFootprint.IsAreaClear(position, 2f, 2f))
                        continue;

                    bool nearRoad = IsNearRoadCell(position, roadCells, mapOrigin, 2);
                    float placementChance = nearRoad ? 0.72f : 0.38f;
                    if (Hash01(hash ^ 0x8f64d731u) > placementChance)
                        continue;

                    if (!TryInstantiateGroundedFreeDetail(
                            prefab,
                            parent,
                            $"{prefab.name}_FreeGround_{result.GrassPatches:0000}",
                            position,
                            gradeElevation + 0.025f,
                            Hash01(hash ^ 0x3a5769bdu) * 360f,
                            Mathf.Lerp(0.72f, 1.18f, Hash01(hash ^ 0xe271b349u)),
                            roadCells,
                            mapOrigin,
                            buildings,
                            reservedAreas,
                            result.ReservedAreas,
                            authoredCoreBounds,
                            out Rect occupiedArea))
                    {
                        continue;
                    }

                    result.ReservedAreas.Add(occupiedArea);
                    result.GrassPatches++;
                }
            }
        }

        private static void AddMainStreetBushes(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject prefab,
            Vector3 mapOrigin,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            List<Rect> reservedAreas,
            LandscapingDetailResult result,
            float gradeElevation,
            uint seed)
        {
            var sortedRoadCells = new List<Vector2Int>(roadCells);
            sortedRoadCells.Sort((left, right) =>
            {
                int rowComparison = left.y.CompareTo(right.y);
                return rowComparison != 0 ? rowComparison : left.x.CompareTo(right.x);
            });

            for (int index = 0; index < sortedRoadCells.Count; index++)
            {
                Vector2Int cell = sortedRoadCells[index];
                if (dirtRoadCells.Contains(cell))
                    continue;

                bool left = roadCells.Contains(cell + Vector2Int.left);
                bool right = roadCells.Contains(cell + Vector2Int.right);
                bool down = roadCells.Contains(cell + Vector2Int.down);
                bool up = roadCells.Contains(cell + Vector2Int.up);
                bool horizontal = left && right && !down && !up;
                bool vertical = down && up && !left && !right;
                if (!horizontal && !vertical)
                    continue;

                int along = horizontal ? cell.x : cell.y;
                int fixedCoordinate = horizontal ? cell.y : cell.x;
                uint corridorHash = HashGroundPatch(
                    fixedCoordinate,
                    horizontal ? 3 : 4,
                    unchecked((int)seed) ^ 0x39b5);
                int phase = (int)(corridorHash % 3u);
                if (along % 3 != phase)
                    continue;

                int sequence = (along - phase) / 3;
                int preferredSide = ((sequence + fixedCoordinate) & 1) == 0 ? -1 : 1;
                Vector2 perpendicular = horizontal ? Vector2.up : Vector2.right;
                Vector2 alongDirection = horizontal ? Vector2.right : Vector2.up;
                float alongOffset = (sequence & 1) == 0 ? 3.15f : -3.15f;
                Vector2 roadCenter = RoadCellWorldCenter(cell, mapOrigin);
                bool placed = false;
                for (int sideAttempt = 0; sideAttempt < 2 && !placed; sideAttempt++)
                {
                    int side = sideAttempt == 0 ? preferredSide : -preferredSide;
                    Vector2 position = roadCenter +
                                       alongDirection * alongOffset +
                                       perpendicular * (6.35f * side);
                    if (!cityFootprint.Contains(position, 0.025f))
                        continue;

                    uint detailHash = corridorHash ^ (uint)(along * 0x45d9f3b) ^ (uint)sideAttempt;
                    if (!TryInstantiateGroundedFreeDetail(
                            prefab,
                            parent,
                            $"{prefab.name}_MainStreet_{result.MainStreetBushes:0000}",
                            position,
                            gradeElevation + 0.025f,
                            Hash01(detailHash ^ 0xa365f19du) * 360f,
                            Mathf.Lerp(0.72f, 0.86f, Hash01(detailHash ^ 0x61c87f2bu)),
                            roadCells,
                            mapOrigin,
                            buildings,
                            reservedAreas,
                            result.ReservedAreas,
                            authoredCoreBounds,
                            out Rect occupiedArea))
                    {
                        continue;
                    }

                    result.ReservedAreas.Add(occupiedArea);
                    result.MainStreetBushes++;
                    placed = true;
                }
            }
        }

        private static bool IsNearRoadCell(
            Vector2 position,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin,
            int radiusInCells)
        {
            int centerX = Mathf.FloorToInt((position.x - mapOrigin.x) / RoadGridSize);
            int centerZ = Mathf.FloorToInt((position.y - mapOrigin.z) / RoadGridSize);
            for (int z = centerZ - radiusInCells; z <= centerZ + radiusInCells; z++)
            {
                for (int x = centerX - radiusInCells; x <= centerX + radiusInCells; x++)
                {
                    if (roadCells.Contains(new Vector2Int(x, z)))
                        return true;
                }
            }

            return false;
        }

        private sealed class CourtyardDetailResult
        {
            public readonly List<Rect> ReservedAreas = new();
            public int Courtyards;
            public int Walls;
            public int Pillars;
            public int Wells;
            public int Bushes;
            public int GroundPatchesRemoved;
        }

        private static CourtyardDetailResult AddHouseCourtyards(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject wallPrefab,
            GameObject pillarPrefab,
            GameObject wellPrefab,
            GameObject bushPrefab,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin,
            float gradeElevation,
            uint seed)
        {
            var result = new CourtyardDetailResult();
            for (int buildingIndex = 0; buildingIndex < buildings.Count; buildingIndex++)
            {
                GeneratedBuildingInfo building = buildings[buildingIndex];
                if (!building.IsHouse || building.Footprint.width < 4.5f || building.Footprint.height < 4.5f)
                    continue;

                uint hash = HashGroundPatch(
                    Mathf.RoundToInt(building.Bounds.center.x * 10f),
                    Mathf.RoundToInt(building.Bounds.center.z * 10f),
                    unchecked((int)seed) ^ 0x7a13);
                if (Hash01(hash ^ 0xa761d0f3u) > 0.11f)
                    continue;

                bool placed = false;
                int firstSide = (int)(hash & 3u);
                for (int sideOffset = 0; sideOffset < 4 && !placed; sideOffset++)
                {
                    int side = (firstSide + sideOffset) & 3;
                    uint sideHash = hash ^ (uint)(side * 0x45d9f3b);
                    float yardDepth = Mathf.Lerp(5.2f, 7.2f, Hash01(sideHash ^ 0xb7193a41u));
                    float yardSpan = side < 2
                        ? Mathf.Clamp(building.Footprint.height * 0.82f, 5.5f, 9f)
                        : Mathf.Clamp(building.Footprint.width * 0.82f, 5.5f, 9f);
                    const float buildingGap = 0.2f;
                    Rect yard = CreateCourtyardRect(building.Footprint, side, yardDepth, yardSpan, buildingGap);
                    if (!cityFootprint.Contains(yard.center, 0.035f) ||
                        authoredCoreBounds.Overlaps(yard) ||
                        OverlapsRoadCell(yard, roadCells, mapOrigin) ||
                        OverlapsOtherBuilding(yard, buildings, buildingIndex) ||
                        OverlapsAnyRect(yard, result.ReservedAreas))
                    {
                        continue;
                    }

                    int courtyardIndex = result.Courtyards;
                    result.GroundPatchesRemoved += RemoveOpenGroundDetailsUnderCourtyard(parent.parent, yard);
                    const float wallScale = 1f;
                    PlaceCourtyardWalls(
                        wallPrefab,
                        pillarPrefab,
                        parent,
                        yard,
                        side,
                        gradeElevation,
                        wallScale,
                        courtyardIndex,
                        ref result.Walls,
                        ref result.Pillars);

                    Vector2 interiorCenter = yard.center;
                    if (Hash01(sideHash ^ 0x941ce2b7u) < 0.58f &&
                        InstantiateGroundedDetail(
                            wellPrefab,
                            parent,
                            $"{wellPrefab.name}_Courtyard_{courtyardIndex:0000}",
                            interiorCenter,
                            gradeElevation + 0.025f,
                            Hash01(sideHash ^ 0xe61b89a3u) * 360f,
                            Mathf.Lerp(0.78f, 1.02f, Hash01(sideHash ^ 0x1f35ca9du))))
                    {
                        result.Wells++;
                    }

                    int bushTarget = 2 + (int)(sideHash % 4u);
                    for (int bushIndex = 0; bushIndex < bushTarget; bushIndex++)
                    {
                        uint bushHash = sideHash ^ (uint)(bushIndex * 0x9e3779b9);
                        Vector2 bushPosition = new(
                            Mathf.Lerp(yard.xMin + 0.9f, yard.xMax - 0.9f, Hash01(bushHash ^ 0x572ab39du)),
                            Mathf.Lerp(yard.yMin + 0.9f, yard.yMax - 0.9f, Hash01(bushHash ^ 0x8c15d7e3u)));
                        if (Vector2.Distance(bushPosition, interiorCenter) < 1.15f)
                            bushPosition = Vector2.Lerp(bushPosition, yard.min + Vector2.one, 0.65f);

                        if (InstantiateGroundedDetail(
                                bushPrefab,
                                parent,
                                $"{bushPrefab.name}_Courtyard_{courtyardIndex:0000}_{bushIndex:00}",
                                bushPosition,
                                gradeElevation + 0.02f,
                                Hash01(bushHash ^ 0x4f7812c9u) * 360f,
                                Mathf.Lerp(0.78f, 1.16f, Hash01(bushHash ^ 0xc7a4e591u))))
                        {
                            result.Bushes++;
                        }
                    }

                    result.ReservedAreas.Add(yard);
                    result.Courtyards++;
                    placed = true;
                }
            }

            return result;
        }

        private static int RemoveOpenGroundDetailsUnderCourtyard(Transform generatedRoot, Rect courtyard)
        {
            Transform openGroundRoot = FindDirectChild(generatedRoot, "DenseCity_OpenGroundRoundDetails");
            if (openGroundRoot == null)
                return 0;

            int removed = 0;
            for (int childIndex = openGroundRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = openGroundRoot.GetChild(childIndex);
                if (child == null || !TryGetWorldBounds(child, out Bounds bounds))
                    continue;

                var footprint = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                if (!footprint.Overlaps(courtyard))
                    continue;

                UnityEngine.Object.DestroyImmediate(child.gameObject);
                removed++;
            }

            return removed;
        }

        private static Rect CreateCourtyardRect(
            Rect building,
            int side,
            float depth,
            float span,
            float gap)
        {
            return side switch
            {
                0 => new Rect(building.xMin - gap - depth, building.center.y - span * 0.5f, depth, span),
                1 => new Rect(building.xMax + gap, building.center.y - span * 0.5f, depth, span),
                2 => new Rect(building.center.x - span * 0.5f, building.yMin - gap - depth, span, depth),
                _ => new Rect(building.center.x - span * 0.5f, building.yMax + gap, span, depth)
            };
        }

        private static void PlaceCourtyardWalls(
            GameObject wallPrefab,
            GameObject pillarPrefab,
            Transform parent,
            Rect yard,
            int buildingSide,
            float gradeElevation,
            float wallScale,
            int courtyardIndex,
            ref int wallCount,
            ref int pillarCount)
        {
            const float edgeInset = 0.25f;
            const float cornerClearance = 0.5f;
            if (buildingSide < 2)
            {
                float outerX = buildingSide == 0 ? yard.xMin + edgeInset : yard.xMax - edgeInset;
                AddCourtyardEntrance(
                    wallPrefab,
                    pillarPrefab,
                    parent,
                    courtyardIndex,
                    new Vector2(outerX, yard.center.y),
                    90f,
                    yard.height - cornerClearance * 2f,
                    gradeElevation,
                    wallScale,
                    ref wallCount,
                    ref pillarCount);
                float sideCenterX = yard.center.x + (buildingSide == 0 ? cornerClearance * 0.5f : -cornerClearance * 0.5f);
                AddCourtyardWall(wallPrefab, parent, courtyardIndex, "North", new Vector2(sideCenterX, yard.yMax - edgeInset), 0f, yard.width - cornerClearance, gradeElevation, wallScale, ref wallCount);
                AddCourtyardWall(wallPrefab, parent, courtyardIndex, "South", new Vector2(sideCenterX, yard.yMin + edgeInset), 0f, yard.width - cornerClearance, gradeElevation, wallScale, ref wallCount);
            }
            else
            {
                float outerZ = buildingSide == 2 ? yard.yMin + edgeInset : yard.yMax - edgeInset;
                AddCourtyardEntrance(
                    wallPrefab,
                    pillarPrefab,
                    parent,
                    courtyardIndex,
                    new Vector2(yard.center.x, outerZ),
                    0f,
                    yard.width - cornerClearance * 2f,
                    gradeElevation,
                    wallScale,
                    ref wallCount,
                    ref pillarCount);
                float sideCenterZ = yard.center.y + (buildingSide == 2 ? cornerClearance * 0.5f : -cornerClearance * 0.5f);
                AddCourtyardWall(wallPrefab, parent, courtyardIndex, "West", new Vector2(yard.xMin + edgeInset, sideCenterZ), 90f, yard.height - cornerClearance, gradeElevation, wallScale, ref wallCount);
                AddCourtyardWall(wallPrefab, parent, courtyardIndex, "East", new Vector2(yard.xMax - edgeInset, sideCenterZ), 90f, yard.height - cornerClearance, gradeElevation, wallScale, ref wallCount);
            }
        }

        private static void AddCourtyardEntrance(
            GameObject wallPrefab,
            GameObject pillarPrefab,
            Transform parent,
            int courtyardIndex,
            Vector2 center,
            float wallRotation,
            float totalLength,
            float gradeElevation,
            float wallScale,
            ref int wallCount,
            ref int pillarCount)
        {
            float gateWidth = Mathf.Clamp(totalLength * 0.3f, 1.8f, 2.4f);
            float segmentLength = Mathf.Max(1.25f, (totalLength - gateWidth) * 0.5f);
            float centerOffset = gateWidth * 0.5f + segmentLength * 0.5f;
            Vector2 axis = wallRotation % 180f == 0f ? Vector2.right : Vector2.up;

            AddCourtyardWall(
                wallPrefab,
                parent,
                courtyardIndex,
                "EntranceLeft",
                center - axis * centerOffset,
                wallRotation,
                segmentLength,
                gradeElevation,
                wallScale,
                ref wallCount);
            AddCourtyardWall(
                wallPrefab,
                parent,
                courtyardIndex,
                "EntranceRight",
                center + axis * centerOffset,
                wallRotation,
                segmentLength,
                gradeElevation,
                wallScale,
                ref wallCount);

            AddCourtyardPillar(
                pillarPrefab,
                parent,
                courtyardIndex,
                "GateLeft",
                center - axis * (gateWidth * 0.5f),
                wallRotation,
                gradeElevation,
                wallScale,
                ref pillarCount);
            AddCourtyardPillar(
                pillarPrefab,
                parent,
                courtyardIndex,
                "GateRight",
                center + axis * (gateWidth * 0.5f),
                wallRotation,
                gradeElevation,
                wallScale,
                ref pillarCount);
        }

        private static void AddCourtyardPillar(
            GameObject prefab,
            Transform parent,
            int courtyardIndex,
            string pillarName,
            Vector2 position,
            float rotation,
            float gradeElevation,
            float scale,
            ref int pillarCount)
        {
            if (InstantiateGroundedDetail(
                    prefab,
                    parent,
                    $"{prefab.name}_Courtyard_{courtyardIndex:0000}_{pillarName}",
                    position,
                    gradeElevation + 0.02f,
                    rotation,
                    scale))
            {
                pillarCount++;
            }
        }

        private static void AddCourtyardWall(
            GameObject prefab,
            Transform parent,
            int courtyardIndex,
            string edgeName,
            Vector2 position,
            float rotation,
            float targetLength,
            float gradeElevation,
            float heightScale,
            ref int wallCount)
        {
            GameObject wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (wall == null)
                return;

            wall.name = $"{prefab.name}_Courtyard_{courtyardIndex:0000}_{edgeName}";
            wall.transform.SetPositionAndRotation(
                new Vector3(position.x, 0f, position.y),
                Quaternion.Euler(0f, rotation, 0f));
            if (!TryGetRendererBounds(wall, out Bounds initialBounds))
            {
                UnityEngine.Object.DestroyImmediate(wall);
                return;
            }

            float sourceLength = rotation % 180f == 0f ? initialBounds.size.x : initialBounds.size.z;
            float lengthScale = Mathf.Clamp(targetLength / Mathf.Max(0.1f, sourceLength), 0.22f, 1.65f);
            wall.transform.localScale = new Vector3(lengthScale, heightScale, 1f);
            if (!TryGetRendererBounds(wall, out Bounds scaledBounds))
            {
                UnityEngine.Object.DestroyImmediate(wall);
                return;
            }

            Vector3 groundedPosition = wall.transform.position;
            groundedPosition.y += gradeElevation + 0.02f - scaledBounds.min.y;
            wall.transform.position = groundedPosition;
            DisableColliders(wall);
            wallCount++;
        }

        private static BuildingMaterialVariantResult ApplyBuildingMaterialVariants(
            Transform generatedRoot,
            uint seed)
        {
            Material materialA = AssetDatabase.LoadAssetAtPath<Material>(BuildingMaterialAPath) ??
                                 throw new InvalidOperationException(
                                     $"Missing building material {BuildingMaterialAPath}.");
            Material materialB = AssetDatabase.LoadAssetAtPath<Material>(BuildingMaterialBPath) ??
                                 throw new InvalidOperationException(
                                     $"Missing building material {BuildingMaterialBPath}.");
            Material materialC = AssetDatabase.LoadAssetAtPath<Material>(BuildingMaterialCPath) ??
                                 throw new InvalidOperationException(
                                     $"Missing building material {BuildingMaterialCPath}.");
            Material[] variants = { materialA, materialB, materialC };
            int buildingsA = 0;
            int buildingsB = 0;
            int buildingsC = 0;
            int materialSlotsChanged = 0;

            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform wrapper = transforms[transformIndex];
                if (wrapper == null ||
                    wrapper.parent == null ||
                    wrapper.parent.name != "RuntimeCityVisuals" ||
                    !wrapper.name.EndsWith("_Visual", StringComparison.Ordinal))
                {
                    continue;
                }

                Renderer[] renderers = wrapper.GetComponentsInChildren<Renderer>(true);
                bool usesMaterialA = false;
                for (int rendererIndex = 0; rendererIndex < renderers.Length && !usesMaterialA; rendererIndex++)
                {
                    Material[] sharedMaterials = renderers[rendererIndex].sharedMaterials;
                    for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                    {
                        if (sharedMaterials[materialIndex] == materialA)
                        {
                            usesMaterialA = true;
                            break;
                        }
                    }
                }

                if (!usesMaterialA)
                    continue;

                uint hash = HashGroundPatch(
                    Mathf.RoundToInt(wrapper.position.x * 10f),
                    Mathf.RoundToInt(wrapper.position.z * 10f),
                    unchecked((int)seed) ^ 0x4c39);
                int variantIndex = (int)(hash % 3u);
                Material selectedMaterial = variants[variantIndex];
                if (variantIndex == 0)
                    buildingsA++;
                else if (variantIndex == 1)
                    buildingsB++;
                else
                    buildingsC++;

                if (selectedMaterial == materialA)
                    continue;

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    Material[] sharedMaterials = renderer.sharedMaterials;
                    bool changed = false;
                    for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                    {
                        if (sharedMaterials[materialIndex] != materialA)
                            continue;

                        sharedMaterials[materialIndex] = selectedMaterial;
                        materialSlotsChanged++;
                        changed = true;
                    }

                    if (changed)
                        renderer.sharedMaterials = sharedMaterials;
                }
            }

            if (buildingsB == 0 || buildingsC == 0)
            {
                throw new InvalidOperationException(
                    "Dense city building material variation did not produce both B and C variants.");
            }

            return new BuildingMaterialVariantResult(
                buildingsA,
                buildingsB,
                buildingsC,
                materialSlotsChanged);
        }

        private static List<GeneratedBuildingInfo> CollectGeneratedBuildings(Transform generatedRoot)
        {
            var buildings = new List<GeneratedBuildingInfo>();
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform wrapper = transforms[index];
                if (wrapper == null ||
                    wrapper.parent == null ||
                    wrapper.parent.name != "RuntimeCityVisuals" ||
                    !wrapper.name.EndsWith("_Visual", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(wrapper, out Bounds bounds))
                {
                    continue;
                }

                buildings.Add(new GeneratedBuildingInfo(wrapper, bounds));
            }

            return buildings;
        }

        private static int AddRooftopWaterTanks(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject[] waterTankPrefabs,
            uint seed)
        {
            int count = 0;
            for (int index = 0; index < buildings.Count; index++)
            {
                GeneratedBuildingInfo building = buildings[index];
                float roofArea = building.Bounds.size.x * building.Bounds.size.z;
                if ((!building.IsShop && !building.IsHouse) ||
                    roofArea < 82f ||
                    building.Bounds.size.y < 4.5f)
                {
                    continue;
                }

                uint hash = HashGroundPatch(
                    Mathf.RoundToInt(building.Bounds.center.x * 10f),
                    Mathf.RoundToInt(building.Bounds.center.z * 10f),
                    unchecked((int)seed) ^ 0x4f37);
                float placementChance = building.IsShop ? 0.46f : 0.24f;
                if (Hash01(hash ^ 0x74b21e63u) > placementChance)
                    continue;

                GameObject prefab = waterTankPrefabs[hash % (uint)waterTankPrefabs.Length];
                float offsetX = Mathf.Lerp(-0.2f, 0.2f, Hash01(hash ^ 0x83b9d20du)) * building.Bounds.size.x;
                float offsetZ = Mathf.Lerp(-0.2f, 0.2f, Hash01(hash ^ 0x28cc61fbu)) * building.Bounds.size.z;
                if (InstantiateGroundedDetail(
                        prefab,
                        parent,
                        $"{prefab.name}_Roof_{count:0000}",
                        new Vector2(building.Bounds.center.x + offsetX, building.Bounds.center.z + offsetZ),
                        building.Bounds.max.y + 0.025f,
                        Hash01(hash ^ 0x5e3b7421u) * 360f,
                        Mathf.Lerp(0.82f, 1.08f, Hash01(hash ^ 0xcf5087abu))))
                {
                    count++;
                }
            }

            return count;
        }

        private static int AddGroundedBuildingProps(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject[] propPrefabs,
            Vector2 civicCenter,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            Vector3 mapOrigin,
            float gradeElevation,
            uint seed)
        {
            int count = 0;
            for (int buildingIndex = 0; buildingIndex < buildings.Count; buildingIndex++)
            {
                GeneratedBuildingInfo building = buildings[buildingIndex];
                uint hash = HashGroundPatch(
                    Mathf.RoundToInt(building.Bounds.center.x * 10f),
                    Mathf.RoundToInt(building.Bounds.center.z * 10f),
                    unchecked((int)seed) ^ 0x29d1);
                bool nearCivicCenter = Vector2.Distance(
                    new Vector2(building.Bounds.center.x, building.Bounds.center.z),
                    civicCenter) < 240f;
                int desiredCount = building.IsShop
                    ? nearCivicCenter ? 4 : 2
                    : building.IsHouse && Hash01(hash ^ 0x70f92a6du) < (nearCivicCenter ? 0.55f : 0.28f)
                        ? 1
                        : 0;

                for (int detailIndex = 0; detailIndex < desiredCount; detailIndex++)
                {
                    uint detailHash = hash ^ (uint)(detailIndex * 0x45d9f3b);
                    if (!TryFindGroundDetailPosition(
                            buildingIndex,
                            building,
                            buildings,
                            roadCells,
                            reservedAreas,
                            mapOrigin,
                            detailHash,
                            out Vector2 position))
                    {
                        continue;
                    }

                    GameObject prefab = propPrefabs[detailHash % (uint)propPrefabs.Length];
                    if (InstantiateGroundedDetailClearOfRoads(
                            prefab,
                            parent,
                            $"{prefab.name}_Street_{count:0000}",
                            position,
                            gradeElevation + 0.035f,
                            Hash01(detailHash ^ 0xa1de7c35u) * 360f,
                            Mathf.Lerp(0.82f, 1.12f, Hash01(detailHash ^ 0x9c13b5e7u)),
                            roadCells,
                            mapOrigin))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool TryFindGroundDetailPosition(
            int sourceBuildingIndex,
            GeneratedBuildingInfo building,
            List<GeneratedBuildingInfo> buildings,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            Vector3 mapOrigin,
            uint hash,
            out Vector2 position)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                uint attemptHash = hash ^ (uint)(attempt * 0x27d4eb2d);
                int side = (int)(attemptHash & 3u);
                float along = Mathf.Lerp(-0.36f, 0.36f, Hash01(attemptHash ^ 0x68931acfu));
                float clearance = Mathf.Lerp(0.65f, 1.25f, Hash01(attemptHash ^ 0x13f76a49u));
                position = side switch
                {
                    0 => new Vector2(building.Footprint.xMin - clearance, building.Footprint.center.y + along * building.Footprint.height),
                    1 => new Vector2(building.Footprint.xMax + clearance, building.Footprint.center.y + along * building.Footprint.height),
                    2 => new Vector2(building.Footprint.center.x + along * building.Footprint.width, building.Footprint.yMin - clearance),
                    _ => new Vector2(building.Footprint.center.x + along * building.Footprint.width, building.Footprint.yMax + clearance)
                };
                var detailBounds = new Rect(position.x - 0.45f, position.y - 0.45f, 0.9f, 0.9f);
                if (!OverlapsRoadCell(detailBounds, roadCells, mapOrigin) &&
                    !OverlapsAnyRect(detailBounds, reservedAreas) &&
                    !OverlapsOtherBuilding(detailBounds, buildings, sourceBuildingIndex))
                {
                    return true;
                }
            }

            position = default;
            return false;
        }

        private static (int trees, int rocks) AddDenseTreeAndRockClusters(
            Transform treeParent,
            Transform rockParent,
            List<GeneratedBuildingInfo> buildings,
            GameObject[] treePrefabs,
            GameObject[] rockPrefabs,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            float gradeElevation,
            uint seed)
        {
            int treeCount = 0;
            int rockCount = 0;
            const float clusterSpacing = 24f;
            for (float z = clusterSpacing * 0.5f; z < mapDepth; z += clusterSpacing)
            {
                for (float x = clusterSpacing * 0.5f; x < mapWidth; x += clusterSpacing)
                {
                    uint hash = HashGroundPatch(
                        Mathf.RoundToInt(x / clusterSpacing),
                        Mathf.RoundToInt(z / clusterSpacing),
                        unchecked((int)seed) ^ 0x6d25);
                    if (Hash01(hash ^ 0x7b309f1du) > 0.62f)
                        continue;

                    var center = new Vector2(
                        mapOrigin.x + x + Mathf.Lerp(-8f, 8f, Hash01(hash ^ 0x93a7dc41u)),
                        mapOrigin.z + z + Mathf.Lerp(-8f, 8f, Hash01(hash ^ 0x35f21b8du)));
                    if (!cityFootprint.Contains(center, 0.035f) || authoredCoreBounds.Contains(center))
                        continue;

                    int clusterSize = 4 + (int)(hash % 6u);
                    for (int detailIndex = 0; detailIndex < clusterSize; detailIndex++)
                    {
                        uint detailHash = hash ^ (uint)(detailIndex * 0x9e3779b9);
                        float angle = Hash01(detailHash ^ 0x89a23cf1u) * Mathf.PI * 2f;
                        float radius = Mathf.Lerp(0.8f, 7f, Hash01(detailHash ^ 0x4ba61d27u));
                        var position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                        var detailBounds = new Rect(position.x - 0.8f, position.y - 0.8f, 1.6f, 1.6f);
                        if (!cityFootprint.Contains(position, 0.025f) ||
                            !cityFootprint.IsAreaClear(position, 7f, 7f) ||
                            authoredCoreBounds.Overlaps(detailBounds) ||
                            OverlapsRoadCell(detailBounds, roadCells, mapOrigin) ||
                            OverlapsAnyRect(detailBounds, reservedAreas) ||
                            OverlapsAnyBuilding(detailBounds, buildings))
                        {
                            continue;
                        }

                        GameObject prefab = treePrefabs[Hash01(detailHash ^ 0xf28c4b13u) < 0.42f ? 0 : 1];
                        if (InstantiateGroundedDetail(
                                prefab,
                                treeParent,
                                $"{prefab.name}_Cluster_{treeCount:0000}",
                                position,
                                gradeElevation + 0.03f,
                                Hash01(detailHash ^ 0x62ae91d5u) * 360f,
                                Mathf.Lerp(0.82f, 1.24f, Hash01(detailHash ^ 0xd12047c9u))))
                        {
                            treeCount++;
                        }
                    }

                    if (Hash01(hash ^ 0x2419df73u) < 0.34f)
                    {
                        var rockPosition = center + new Vector2(
                            Mathf.Lerp(-5f, 5f, Hash01(hash ^ 0xb17d63e5u)),
                            Mathf.Lerp(-5f, 5f, Hash01(hash ^ 0x48a32c9fu)));
                        var rockBounds = new Rect(rockPosition.x - 0.75f, rockPosition.y - 0.75f, 1.5f, 1.5f);
                        if (cityFootprint.IsAreaClear(rockPosition, 10f, 10f) &&
                            !authoredCoreBounds.Overlaps(rockBounds) &&
                            !OverlapsRoadCell(rockBounds, roadCells, mapOrigin) &&
                            !OverlapsAnyRect(rockBounds, reservedAreas) &&
                            !OverlapsAnyBuilding(rockBounds, buildings))
                        {
                            GameObject prefab = rockPrefabs[hash % (uint)rockPrefabs.Length];
                            if (InstantiateGroundedDetailClearOfRoads(
                                    prefab,
                                    rockParent,
                                    $"{prefab.name}_Urban_{rockCount:0000}",
                                    rockPosition,
                                    gradeElevation + 0.02f,
                                    Hash01(hash ^ 0xc391f287u) * 360f,
                                    Mathf.Lerp(0.65f, 1.25f, Hash01(hash ^ 0x3b8c592du)),
                                    roadCells,
                                    mapOrigin))
                            {
                                rockCount++;
                            }
                        }
                    }
                }
            }

            return (treeCount, rockCount);
        }

        private static bool OverlapsOtherBuilding(
            Rect bounds,
            List<GeneratedBuildingInfo> buildings,
            int ignoredBuildingIndex)
        {
            for (int index = 0; index < buildings.Count; index++)
            {
                if (index != ignoredBuildingIndex && bounds.Overlaps(buildings[index].Footprint))
                    return true;
            }

            return false;
        }

        private static bool OverlapsAnyBuilding(Rect bounds, List<GeneratedBuildingInfo> buildings)
        {
            for (int index = 0; index < buildings.Count; index++)
            {
                if (bounds.Overlaps(buildings[index].Footprint))
                    return true;
            }

            return false;
        }

        private static bool OverlapsAnyRect(Rect bounds, List<Rect> reservedAreas)
        {
            for (int index = 0; index < reservedAreas.Count; index++)
            {
                if (bounds.Overlaps(reservedAreas[index]))
                    return true;
            }

            return false;
        }

        private static bool InstantiateGroundedDetail(
            GameObject prefab,
            Transform parent,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
                return false;

            instance.name = objectName;
            instance.transform.SetPositionAndRotation(
                new Vector3(position.x, 0f, position.y),
                Quaternion.Euler(0f, rotationDegrees, 0f));
            instance.transform.localScale = Vector3.one * scale;
            if (!TryGetRendererBounds(instance, out Bounds bounds))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            Vector3 worldPosition = instance.transform.position;
            worldPosition.y += supportHeight - bounds.min.y;
            instance.transform.position = worldPosition;
            DisableColliders(instance);
            return true;
        }

        private static bool InstantiateGroundedDetailClearOfRoads(
            GameObject prefab,
            Transform parent,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
                return false;

            instance.name = objectName;
            instance.transform.SetPositionAndRotation(
                new Vector3(position.x, 0f, position.y),
                Quaternion.Euler(0f, rotationDegrees, 0f));
            instance.transform.localScale = Vector3.one * scale;
            if (!TryGetRendererBounds(instance, out Bounds bounds))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            const float roadClearance = 0.12f;
            var actualFootprint = Rect.MinMaxRect(
                bounds.min.x - roadClearance,
                bounds.min.z - roadClearance,
                bounds.max.x + roadClearance,
                bounds.max.z + roadClearance);
            if (OverlapsRoadCell(actualFootprint, roadCells, mapOrigin))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                return false;
            }

            Vector3 worldPosition = instance.transform.position;
            worldPosition.y += supportHeight - bounds.min.y;
            instance.transform.position = worldPosition;
            DisableColliders(instance);
            return true;
        }

        private static bool TryInstantiateGroundedFreeDetail(
            GameObject prefab,
            Transform parent,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin,
            List<GeneratedBuildingInfo> buildings,
            List<Rect> reservedAreas,
            List<Rect> localReservedAreas,
            Rect authoredCoreBounds,
            out Rect occupiedArea)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null)
            {
                occupiedArea = default;
                return false;
            }

            instance.name = objectName;
            instance.transform.SetPositionAndRotation(
                new Vector3(position.x, 0f, position.y),
                Quaternion.Euler(0f, rotationDegrees, 0f));
            instance.transform.localScale = Vector3.one * scale;
            if (!TryGetRendererBounds(instance, out Bounds bounds))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                occupiedArea = default;
                return false;
            }

            const float detailClearance = 0.08f;
            occupiedArea = Rect.MinMaxRect(
                bounds.min.x - detailClearance,
                bounds.min.z - detailClearance,
                bounds.max.x + detailClearance,
                bounds.max.z + detailClearance);
            if (authoredCoreBounds.Overlaps(occupiedArea) ||
                OverlapsRoadCell(occupiedArea, roadCells, mapOrigin) ||
                OverlapsAnyBuilding(occupiedArea, buildings) ||
                OverlapsAnyRect(occupiedArea, reservedAreas) ||
                OverlapsAnyRect(occupiedArea, localReservedAreas))
            {
                UnityEngine.Object.DestroyImmediate(instance);
                occupiedArea = default;
                return false;
            }

            Vector3 worldPosition = instance.transform.position;
            worldPosition.y += supportHeight - bounds.min.y;
            instance.transform.position = worldPosition;
            DisableColliders(instance);
            return true;
        }

        private static void ValidateNoRoadOverlappingDetails(
            Transform detailRoot,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin)
        {
            for (int index = 0; index < detailRoot.childCount; index++)
            {
                Transform detail = detailRoot.GetChild(index);
                if (detail == null || !TryGetWorldBounds(detail, out Bounds bounds))
                    continue;

                var actualFootprint = Rect.MinMaxRect(
                    bounds.min.x,
                    bounds.min.z,
                    bounds.max.x,
                    bounds.max.z);
                if (OverlapsRoadCell(actualFootprint, roadCells, mapOrigin))
                {
                    throw new InvalidOperationException(
                        $"Generated detail '{detail.name}' overlaps an authored road cell.");
                }
            }
        }

        private static GameObject[] LoadRequiredPrefabs(string[] paths)
        {
            var prefabs = new GameObject[paths.Length];
            for (int index = 0; index < paths.Length; index++)
            {
                prefabs[index] = AssetDatabase.LoadAssetAtPath<GameObject>(paths[index]) ??
                                 throw new InvalidOperationException($"Missing authored detail prefab {paths[index]}.");
            }

            return prefabs;
        }

        private static GameObject LoadRequiredPrefab(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
            throw new InvalidOperationException($"Missing authored detail prefab {path}.");

        private static void CreateBuildingGroundPatch(
            GameObject buildingWrapper,
            float width,
            float depth,
            SurfacePatchEvaluation patch,
            float foundationHeight)
        {
            Transform parent = buildingWrapper.transform.parent;
            if (parent == null)
                return;

            float top = foundationHeight - 0.02f;
            float bottom = patch.MinimumHeight - 0.08f;
            float height = Mathf.Clamp(top - bottom + 0.1f, 0.22f, 0.8f);
            Vector3 position = buildingWrapper.transform.position;
            position.y = top;
            CreateNaturalGroundPatch(
                parent,
                buildingWrapper.name + "_GroundPatch",
                position,
                width + 1.4f,
                depth + 1.4f,
                height,
                HashGroundPatch(
                    Mathf.RoundToInt(position.x * 10f),
                    Mathf.RoundToInt(position.z * 10f),
                    0x2a97));
        }

        private static void CreateNaturalGroundPatch(
            Transform parent,
            string objectName,
            Vector3 topCenter,
            float targetWidth,
            float targetDepth,
            float targetHeight,
            uint hash,
            bool forcePrimaryGroundPrefab = false)
        {
            GameObject[] prefabs = LoadNaturalGroundPrefabs();
            int prefabIndex;
            if (forcePrimaryGroundPrefab || prefabs.Length == 1 || Hash01(hash ^ 0x19a53b71u) < 0.9f)
                prefabIndex = 0;
            else
                prefabIndex = 1 + (int)(hash % (uint)(prefabs.Length - 1));
            GameObject prefab = prefabs[prefabIndex];
            GameObject patch = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            patch.name = objectName;
            patch.transform.SetPositionAndRotation(
                new Vector3(topCenter.x, 0f, topCenter.z),
                Quaternion.identity);
            patch.transform.localScale = Vector3.one;

            if (!TryGetRendererBounds(patch, out Bounds sourceBounds))
                throw new InvalidOperationException($"Natural ground prefab '{prefab.name}' has no renderer bounds.");

            float widthVariation = Mathf.Lerp(0.92f, 1.12f, Hash01(hash ^ 0x68bc21ebu));
            float depthVariation = Mathf.Lerp(0.92f, 1.12f, Hash01(hash ^ 0x02e5be93u));
            float heightVariation = Mathf.Lerp(0.82f, 1.18f, Hash01(hash ^ 0x967a889bu));
            patch.transform.localScale = new Vector3(
                targetWidth * widthVariation / Mathf.Max(0.01f, sourceBounds.size.x),
                targetHeight * heightVariation / Mathf.Max(0.01f, sourceBounds.size.y),
                targetDepth * depthVariation / Mathf.Max(0.01f, sourceBounds.size.z));
            patch.transform.rotation = Quaternion.Euler(0f, Hash01(hash ^ 0x4f1bbcdcu) * 360f, 0f);

            if (!TryGetRendererBounds(patch, out Bounds scaledBounds))
                throw new InvalidOperationException($"Natural ground prefab '{prefab.name}' lost renderer bounds after scaling.");
            Vector3 position = patch.transform.position;
            position.y += topCenter.y - 0.025f - scaledBounds.max.y;
            patch.transform.position = position;

            DisableColliders(patch);
            Material material = GetGroundVariationMaterial();
            if (material == null)
                return;
            Renderer[] renderers = patch.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
                renderers[index].sharedMaterial = material;
        }

        private static GameObject[] LoadNaturalGroundPrefabs()
        {
            if (_naturalGroundPrefabs != null)
                return _naturalGroundPrefabs;

            _naturalGroundPrefabs = new GameObject[NaturalGroundPrefabGuids.Length];
            for (int index = 0; index < NaturalGroundPrefabGuids.Length; index++)
            {
                string guid = NaturalGroundPrefabGuids[index];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                _naturalGroundPrefabs[index] = AssetDatabase.LoadAssetAtPath<GameObject>(path) ??
                                               throw new InvalidOperationException(
                                                   $"Missing SM_Env_Ground_Round prefab for GUID {guid}.");
            }

            return _naturalGroundPrefabs;
        }

        private static bool TryGetRendererBounds(GameObject gameObject, out Bounds bounds)
        {
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return true;
        }

        private static uint HashGroundPatch(int x, int z, int salt)
        {
            uint hash = unchecked((uint)(x * 73856093) ^ (uint)(z * 19349663) ^ (uint)salt);
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            return hash ^ (hash >> 16);
        }

        private static float Hash01(uint hash) => (hash & 0x00ffffffu) / 16777215f;

        private static Material GetGroundVariationMaterial()
        {
            if (_groundVariationMaterial != null)
                return _groundVariationMaterial;

            string path = AssetDatabase.GUIDToAssetPath(GroundVariationMaterialGuid);
            _groundVariationMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            return _groundVariationMaterial;
        }

        private static void BuildParkBlock(
            RuntimeCityVisualPresentationSystemHelper visuals,
            BuildingPalette palette,
            GridConfig grid,
            Rect block,
            TerrainViabilityMap terrainMap,
            BuildingPlacementContext placementContext,
            bool addFountain,
            System.Random random)
        {
            if (palette.Park.Count == 0)
                return;

            if (addFountain && palette.Fountains.Count > 0)
            {
                PrefabFootprint fountain = palette.Fountains[random.Next(palette.Fountains.Count)];
                SpawnBuilding(
                    visuals,
                    fountain,
                    new Vector3(block.center.x, grid.Origin.y, block.center.y),
                    random.Next(0, 4) * 90f,
                    grid,
                    terrainMap,
                    placementContext);
            }

            for (int index = 0; index < 10; index++)
            {
                PrefabFootprint info = palette.Park[random.Next(palette.Park.Count)];
                float x = Mathf.Lerp(block.xMin + 7f, block.xMax - 7f, (float)random.NextDouble());
                float z = Mathf.Lerp(block.yMin + 7f, block.yMax - 7f, (float)random.NextDouble());
                SpawnBuilding(
                    visuals,
                    info,
                    new Vector3(x, grid.Origin.y, z),
                    random.Next(0, 4) * 90f,
                    grid,
                    terrainMap,
                    placementContext);
            }
        }

        private static BuildingPalette BuildPalette(RuntimeCitySpawnerSystemConfig config)
        {
            var palette = new BuildingPalette();
            AddPrefabList(config.HousePrefabs, palette.Houses);
            AddPrefabList(config.ShopPrefabs, palette.Shops);
            AddPrefabList(config.OtherBuildingPrefabs, palette.Other);
            for (int index = 0; index < CleanStandaloneShopPrefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CleanStandaloneShopPrefabPaths[index]);
                if (prefab != null)
                    palette.Shops.Add(MeasurePrefab(prefab, BuildingVisualScale));
            }
            for (int index = 0; index < ParkPrefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ParkPrefabPaths[index]);
                if (prefab != null)
                {
                    palette.Park.Add(MeasurePrefab(prefab, 1f));
                    if (index >= 4)
                        palette.Fountains.Add(MeasurePrefab(prefab, 0.85f));
                }
            }

            if (palette.Houses.Count == 0 || palette.Shops.Count == 0)
                throw new InvalidOperationException("Dense city config requires both house and shop prefabs.");
            return palette;
        }

        private static void AddPrefabList(
            List<GameObject> source,
            List<PrefabFootprint> target,
            float visualScale = BuildingVisualScale)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Count; index++)
            {
                GameObject prefab = source[index];
                if (prefab != null && IsDenseCityPrefabUsable(prefab))
                    target.Add(MeasurePrefab(prefab, visualScale));
            }
        }

        private static bool IsDenseCityPrefabUsable(GameObject prefab)
        {
            if (prefab == null)
                return false;

            if (DenseCityPrefabUsabilityCache.TryGetValue(prefab, out bool usable))
                return usable;

            usable = !HasUnsupportedCombinedMeshIslands(prefab, out int unsupportedIslandCount);
            DenseCityPrefabUsabilityCache[prefab] = usable;
            if (!usable)
            {
                Debug.LogWarning(
                    $"[DenseCityPrefabAudit] excluded={prefab.name} " +
                    $"unsupportedCombinedMeshIslands={unsupportedIslandCount}");
            }

            return usable;
        }

        private static bool HasUnsupportedCombinedMeshIslands(
            GameObject prefab,
            out int unsupportedIslandCount)
        {
            const float groundedTolerance = 0.75f;
            const float maximumVerticalJoinGap = 1.35f;
            const float horizontalJoinMargin = 0.35f;

            unsupportedIslandCount = 0;
            Transform combinedRoot = FindDescendant(prefab.transform, "CombinedMesh");
            if (combinedRoot == null)
                return false;

            var islands = new List<Bounds>();
            MeshFilter[] filters = combinedRoot.GetComponentsInChildren<MeshFilter>(true);
            for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
                AddConnectedMeshIslandBounds(filters[filterIndex], combinedRoot, islands);

            if (islands.Count < 2)
                return false;

            float groundedHeight = float.PositiveInfinity;
            for (int islandIndex = 0; islandIndex < islands.Count; islandIndex++)
                groundedHeight = Mathf.Min(groundedHeight, islands[islandIndex].min.y);
            groundedHeight += groundedTolerance;

            var supported = new HashSet<int>();
            for (int islandIndex = 0; islandIndex < islands.Count; islandIndex++)
            {
                if (islands[islandIndex].min.y <= groundedHeight)
                    supported.Add(islandIndex);
            }

            bool addedSupport;
            do
            {
                addedSupport = false;
                for (int candidateIndex = 0; candidateIndex < islands.Count; candidateIndex++)
                {
                    if (supported.Contains(candidateIndex))
                        continue;

                    foreach (int supportIndex in supported)
                    {
                        if (IsRendererSupportedBy(
                                islands[candidateIndex],
                                islands[supportIndex],
                                maximumVerticalJoinGap,
                                horizontalJoinMargin))
                        {
                            supported.Add(candidateIndex);
                            addedSupport = true;
                            break;
                        }
                    }
                }
            }
            while (addedSupport);

            unsupportedIslandCount = islands.Count - supported.Count;
            return unsupportedIslandCount > 0;
        }

        private static void AddConnectedMeshIslandBounds(
            MeshFilter filter,
            Transform combinedRoot,
            List<Bounds> islands)
        {
            if (filter == null || filter.sharedMesh == null)
                return;

            Mesh readableMesh = null;
            try
            {
                readableMesh = StaticMapMeshReadbackUtility.CreateReadableClone(filter.sharedMesh);
                Vector3[] vertices = readableMesh.vertices;
                if (vertices.Length == 0)
                    return;

                var parents = new int[vertices.Length];
                var used = new bool[vertices.Length];
                for (int vertexIndex = 0; vertexIndex < parents.Length; vertexIndex++)
                    parents[vertexIndex] = vertexIndex;

                var coincidentVertices = new Dictionary<Vector3Int, int>();
                for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    Vector3 vertex = vertices[vertexIndex];
                    var key = new Vector3Int(
                        Mathf.RoundToInt(vertex.x * 1000f),
                        Mathf.RoundToInt(vertex.y * 1000f),
                        Mathf.RoundToInt(vertex.z * 1000f));
                    if (coincidentVertices.TryGetValue(key, out int coincidentIndex))
                        UnionMeshVertices(parents, vertexIndex, coincidentIndex);
                    else
                        coincidentVertices.Add(key, vertexIndex);
                }

                for (int subMeshIndex = 0; subMeshIndex < readableMesh.subMeshCount; subMeshIndex++)
                {
                    if (readableMesh.GetTopology(subMeshIndex) != MeshTopology.Triangles)
                        continue;

                    int[] indices = readableMesh.GetIndices(subMeshIndex, false);
                    for (int triangleIndex = 0; triangleIndex + 2 < indices.Length; triangleIndex += 3)
                    {
                        int a = indices[triangleIndex];
                        int b = indices[triangleIndex + 1];
                        int c = indices[triangleIndex + 2];
                        used[a] = used[b] = used[c] = true;
                        UnionMeshVertices(parents, a, b);
                        UnionMeshVertices(parents, a, c);
                    }
                }

                Matrix4x4 toCombinedRoot = combinedRoot.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var componentBounds = new Dictionary<int, Bounds>();
                for (int vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    if (!used[vertexIndex])
                        continue;

                    int root = FindMeshVertexRoot(parents, vertexIndex);
                    Vector3 point = toCombinedRoot.MultiplyPoint3x4(vertices[vertexIndex]);
                    if (componentBounds.TryGetValue(root, out Bounds bounds))
                    {
                        bounds.Encapsulate(point);
                        componentBounds[root] = bounds;
                    }
                    else
                    {
                        componentBounds.Add(root, new Bounds(point, Vector3.zero));
                    }
                }

                foreach (Bounds bounds in componentBounds.Values)
                    islands.Add(bounds);
            }
            finally
            {
                if (readableMesh != null)
                    UnityEngine.Object.DestroyImmediate(readableMesh);
            }
        }

        private static int FindMeshVertexRoot(int[] parents, int vertex)
        {
            while (parents[vertex] != vertex)
            {
                parents[vertex] = parents[parents[vertex]];
                vertex = parents[vertex];
            }

            return vertex;
        }

        private static void UnionMeshVertices(int[] parents, int a, int b)
        {
            int rootA = FindMeshVertexRoot(parents, a);
            int rootB = FindMeshVertexRoot(parents, b);
            if (rootA != rootB)
                parents[rootB] = rootA;
        }

        private static GameObject FirstPrefab(List<GameObject> prefabs)
        {
            if (prefabs == null)
                return null;
            for (int index = 0; index < prefabs.Count; index++)
            {
                if (prefabs[index] != null)
                    return prefabs[index];
            }

            return null;
        }

        private static PrefabFootprint MeasurePrefab(GameObject prefab, float visualScale)
        {
            Transform visualRoot = FindDescendant(prefab.transform, "CombinedMesh") ?? prefab.transform;
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new PrefabFootprint(prefab, 10f, 10f, visualScale);

            Matrix4x4 worldToRoot = prefab.transform.worldToLocalMatrix;
            Bounds bounds = default;
            bool hasBounds = false;
            for (int index = 0; index < renderers.Length; index++)
            {
                Bounds rendererBounds = renderers[index].bounds;
                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;
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
                            Vector3 localCorner = worldToRoot.MultiplyPoint3x4(corner);
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

            return hasBounds
                ? new PrefabFootprint(prefab, bounds.size.x, bounds.size.z, visualScale)
                : new PrefabFootprint(prefab, 10f, 10f, visualScale);
        }

        private static PrefabFootprint SelectBuilding(
            BuildingPalette palette,
            bool preferShop,
            System.Random random)
        {
            List<PrefabFootprint> source;
            int roll = random.Next(100);
            if (preferShop && roll < 70)
                source = palette.Shops;
            else if (roll < 75)
                source = palette.Houses;
            else if (roll < 94)
                source = palette.Shops;
            else
                source = palette.Other.Count > 0 ? palette.Other : palette.Houses;
            return source.Count > 0 ? source[random.Next(source.Count)] : default;
        }

        private static GridConfig CreateGrid(RuntimeCityRAndDMapView view)
        {
            Vector3 origin = view.GridOrigin;
            return CreateGrid(
                origin,
                view.GridWidth * view.GridCellSize,
                view.GridHeight * view.GridCellSize,
                view.GridCellSize);
        }

        private static GridConfig CreateGrid(
            Vector3 origin,
            float worldWidth,
            float worldDepth,
            float cellSize)
        {
            float safeCellSize = Mathf.Max(0.01f, cellSize);
            return new GridConfig
            {
                Width = Mathf.Max(1, Mathf.CeilToInt(worldWidth / safeCellSize)),
                Height = Mathf.Max(1, Mathf.CeilToInt(worldDepth / safeCellSize)),
                CellSize = safeCellSize,
                Origin = new Unity.Mathematics.float3(origin.x, origin.y, origin.z)
            };
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindDescendant(root.GetChild(index), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static GameObject FindSceneObjectByName(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDescendant(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        private static void DisableColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int index = 0; index < colliders.Length; index++)
                colliders[index].enabled = false;
        }

        private static void SetStaticRecursively(GameObject root)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
                transforms[index].gameObject.isStatic = true;
        }

        private static int CountActiveRenderers(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
            int count = 0;
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index].enabled)
                    count++;
            }
            return count;
        }
    }
}
