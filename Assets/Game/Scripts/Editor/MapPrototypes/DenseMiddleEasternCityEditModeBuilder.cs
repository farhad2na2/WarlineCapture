using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
            public readonly int SemanticBuildings;
            public readonly int SemanticBuildingAttachments;
            public readonly int SemanticSurfaces;
            public readonly int SemanticPresentations;
            public readonly int SemanticRoadShoulders;
            public readonly int SemanticCanalWaterExclusions;
            public readonly int SemanticCanalBankTerrains;
            public readonly int SemanticCanalParkTerrains;
            public readonly int SemanticCanalTrees;
            public readonly int SemanticCanalBushes;
            public readonly int SemanticCanalLights;
            public readonly int SemanticCivicBuildings;
            public readonly int SemanticCivicRoads;
            public readonly int SemanticHorizonMountains;
            public readonly int SemanticBoulevardMedianTrees;
            public readonly int SemanticBoulevardMedianLights;
            public readonly int SemanticSidewalkStreetLights;
            public readonly int SemanticGrassPatches;
            public readonly int SemanticMainStreetBushes;
            public readonly int SemanticPowerPoles;
            public readonly int SemanticPowerLines;
            public readonly int SemanticCourtyardWalls;
            public readonly int SemanticCourtyardPillars;
            public readonly int SemanticCourtyardWells;
            public readonly int SemanticCourtyardBushes;
            public readonly int SemanticStreetProps;
            public readonly int SemanticUrbanTrees;
            public readonly int SemanticUrbanRocks;
            public readonly int SemanticCivicFountains;
            public readonly int SemanticOpenGroundTerrains;
            public readonly DenseCityGenerationRecordSnapshot Records;

            public Result(
                int roadTiles,
                int roadChunks,
                int buildings,
                int parks,
                int authoredCoreRenderers,
                int semanticBuildings,
                int semanticBuildingAttachments,
                int semanticSurfaces,
                int semanticPresentations,
                int semanticRoadShoulders,
                int semanticCanalWaterExclusions,
                int semanticCanalBankTerrains,
                int semanticCanalParkTerrains,
                int semanticCanalTrees,
                int semanticCanalBushes,
                int semanticCanalLights,
                int semanticCivicBuildings,
                int semanticCivicRoads,
                int semanticHorizonMountains,
                int semanticBoulevardMedianTrees,
                int semanticBoulevardMedianLights,
                int semanticSidewalkStreetLights,
                int semanticGrassPatches,
                int semanticMainStreetBushes,
                int semanticPowerPoles,
                int semanticPowerLines,
                int semanticCourtyardWalls,
                int semanticCourtyardPillars,
                int semanticCourtyardWells,
                int semanticCourtyardBushes,
                int semanticStreetProps,
                int semanticUrbanTrees,
                int semanticUrbanRocks,
                int semanticCivicFountains,
                int semanticOpenGroundTerrains,
                DenseCityGenerationRecordSnapshot records)
            {
                RoadTiles = roadTiles;
                RoadChunks = roadChunks;
                Buildings = buildings;
                Parks = parks;
                AuthoredCoreRenderers = authoredCoreRenderers;
                SemanticBuildings = semanticBuildings;
                SemanticBuildingAttachments = semanticBuildingAttachments;
                SemanticSurfaces = semanticSurfaces;
                SemanticPresentations = semanticPresentations;
                SemanticRoadShoulders = semanticRoadShoulders;
                SemanticCanalWaterExclusions = semanticCanalWaterExclusions;
                SemanticCanalBankTerrains = semanticCanalBankTerrains;
                SemanticCanalParkTerrains = semanticCanalParkTerrains;
                SemanticCanalTrees = semanticCanalTrees;
                SemanticCanalBushes = semanticCanalBushes;
                SemanticCanalLights = semanticCanalLights;
                SemanticCivicBuildings = semanticCivicBuildings;
                SemanticCivicRoads = semanticCivicRoads;
                SemanticHorizonMountains = semanticHorizonMountains;
                SemanticBoulevardMedianTrees = semanticBoulevardMedianTrees;
                SemanticBoulevardMedianLights = semanticBoulevardMedianLights;
                SemanticSidewalkStreetLights = semanticSidewalkStreetLights;
                SemanticGrassPatches = semanticGrassPatches;
                SemanticMainStreetBushes = semanticMainStreetBushes;
                SemanticPowerPoles = semanticPowerPoles;
                SemanticPowerLines = semanticPowerLines;
                SemanticCourtyardWalls = semanticCourtyardWalls;
                SemanticCourtyardPillars = semanticCourtyardPillars;
                SemanticCourtyardWells = semanticCourtyardWells;
                SemanticCourtyardBushes = semanticCourtyardBushes;
                SemanticStreetProps = semanticStreetProps;
                SemanticUrbanTrees = semanticUrbanTrees;
                SemanticUrbanRocks = semanticUrbanRocks;
                SemanticCivicFountains = semanticCivicFountains;
                SemanticOpenGroundTerrains = semanticOpenGroundTerrains;
                Records = records ??
                    throw new ArgumentNullException(nameof(records));
            }
        }

        internal readonly struct PrefabFootprint
        {
            public readonly GameObject Prefab;
            public readonly float Width;
            public readonly float Depth;
            public readonly float Height;
            public readonly float VisualScale;
            public readonly DenseCityPresentationCategory PresentationCategory;
            public readonly GeneratedCityBuildingRole BuildingRole;
            public readonly bool ApplyMaterialVariants;

            public PrefabFootprint(
                GameObject prefab,
                float width,
                float depth,
                float height,
                float visualScale,
                DenseCityPresentationCategory presentationCategory,
                GeneratedCityBuildingRole buildingRole,
                bool applyMaterialVariants = true)
            {
                if (presentationCategory is not (DenseCityPresentationCategory.GameplayBuildingIntact or
                    DenseCityPresentationCategory.Vegetation or DenseCityPresentationCategory.Prop))
                {
                    throw new ArgumentOutOfRangeException(nameof(presentationCategory));
                }
                bool isBuilding = presentationCategory == DenseCityPresentationCategory.GameplayBuildingIntact;
                if (isBuilding == (buildingRole == GeneratedCityBuildingRole.None))
                    throw new ArgumentOutOfRangeException(nameof(buildingRole));

                Prefab = prefab;
                VisualScale = Mathf.Max(0.01f, visualScale);
                Width = Mathf.Max(3f, width * VisualScale);
                Depth = Mathf.Max(3f, depth * VisualScale);
                Height = Mathf.Max(1f, height * VisualScale);
                PresentationCategory = presentationCategory;
                BuildingRole = buildingRole;
                ApplyMaterialVariants = applyMaterialVariants;
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
            public readonly List<PrefabFootprint> CentralLandmarks = new();
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
            private const float BuildingClearance = 0.04f;
            private const float RoadVisualHalfExtent = 9f;
            private const float DirtRoadVisualHalfExtent = RoadGridSize * 0.5f;
            private const float OccupancyCellSize = 12f;

            private readonly HashSet<Vector2Int> _roadCells;
            private readonly HashSet<Vector2Int> _dirtRoadCells;
            private readonly Vector3 _roadOrigin;
            private readonly RuntimeCitySpawnerSystemConfig _config;
            private readonly DenseCityBuildingMaterialLibrary _materialLibrary;
            private readonly DenseCityBuildingDefinitionLibrary _definitionLibrary;
            private readonly DenseCityGenerationTransactionContext _generationTransactions;
            private readonly Transform _presentationParent;
            private readonly Rect? _approvedMapSurfaceBounds;
            private readonly List<Rect> _occupiedBounds = new();
            private readonly Dictionary<Vector2Int, List<int>> _occupiedByCell = new();
            private int _districtId = -1;

            public int ReservedCount => _occupiedBounds.Count;

            public BuildingPlacementContext(
                HashSet<Vector2Int> roadCells,
                Vector3 roadOrigin,
                HashSet<Vector2Int> dirtRoadCells,
                RuntimeCitySpawnerSystemConfig config,
                DenseCityBuildingMaterialLibrary materialLibrary,
                DenseCityGenerationTransactionContext generationTransactions,
                Transform presentationParent,
                Rect? approvedMapSurfaceBounds = null)
            {
                _roadCells = roadCells ?? new HashSet<Vector2Int>();
                _dirtRoadCells = dirtRoadCells ?? new HashSet<Vector2Int>();
                _roadOrigin = roadOrigin;
                _config = config ?? throw new ArgumentNullException(nameof(config));
                _materialLibrary = materialLibrary ?? throw new ArgumentNullException(nameof(materialLibrary));
                _definitionLibrary = DenseCityBuildingDefinitionLibrary.LoadExisting();
                _generationTransactions = generationTransactions ??
                    throw new ArgumentNullException(nameof(generationTransactions));
                _presentationParent = presentationParent != null
                    ? presentationParent
                    : throw new ArgumentNullException(nameof(presentationParent));
                _approvedMapSurfaceBounds = approvedMapSurfaceBounds;
            }

            public Matrix4x4 PresentationParentLocalToWorldMatrix => _presentationParent.localToWorldMatrix;

            public Matrix4x4 PresentationParentWorldToLocalMatrix => _presentationParent.worldToLocalMatrix;

            public void SetDistrict(int districtId)
            {
                if (districtId < 0)
                    throw new ArgumentOutOfRangeException(nameof(districtId));
                _districtId = districtId;
            }

            public bool TryPlaceSemanticBuilding(
                PrefabFootprint info,
                DenseCityBuildingPlacementPlan plan,
                Func<bool> realize,
                out DenseCityBuildingBakeRecord acceptedBuilding)
            {
                if (info.PresentationCategory != DenseCityPresentationCategory.GameplayBuildingIntact)
                {
                    acceptedBuilding = default;
                    return realize();
                }
                if (_districtId < 0)
                    throw new InvalidOperationException("Dense-city building district must be explicit before placement.");
                bool insideApprovedMapSurface = true;
                if (_approvedMapSurfaceBounds.HasValue)
                {
                    Rect bounds = _approvedMapSurfaceBounds.Value;
                    Bounds blockerBounds = plan.BlockerBounds;
                    if (blockerBounds.min.x < bounds.xMin || blockerBounds.max.x > bounds.xMax ||
                        blockerBounds.min.z < bounds.yMin || blockerBounds.max.z > bounds.yMax)
                    {
                        insideApprovedMapSurface = false;
                    }
                }

                DenseCityBuildingMaterialSelection materialSelection = _materialLibrary.Select(
                    info.Prefab,
                    plan.WorldMatrix.GetColumn(3),
                    _config.RandomSeed,
                    info.BuildingRole);
                GameObject destroyedPrefab = _config.GetGeneratedDestroyedVisualPrefab(info.BuildingRole);
                return _generationTransactions.TryPlaceBuilding(
                    _districtId,
                    sequence => DenseCityBuildingPlacementRecordBuilder.Create(
                        new DenseCityBuildingPlacementRecordRequest(
                            DenseCityGeneratorSchema,
                            unchecked((int)_config.RandomSeed),
                            _districtId,
                            sequence,
                            info.Prefab,
                            destroyedPrefab,
                            materialSelection,
                            plan.WorldMatrix,
                            plan.OriginCell,
                            plan.FootprintCells,
                            plan.FootprintSize,
                            plan.FoundationElevation,
                            plan.BlockerBounds,
                            plan.FrontageDirection,
                            info.BuildingRole,
                            _definitionLibrary.ResolveAssetGuid(info.BuildingRole),
                            0,
                            _config.DefaultBuildingMaxHealth,
                            DenseCityBuildingMovementMask,
                            DenseCityBuildingSurfaceLayer,
                            plan.Chunk,
                            info.BuildingRole == GeneratedCityBuildingRole.Civic ? "civic" : null),
                        _materialLibrary),
                    insideApprovedMapSurface ? realize : static () => false,
                    out acceptedBuilding);
            }

            public void RegisterRealizedBuildingOwner(
                DenseCityBuildingBakeRecord building,
                Transform intactPresentationRoot,
                PrefabFootprint info)
            {
                _generationTransactions.RegisterRealizedBuildingOwner(
                    building,
                    intactPresentationRoot,
                    info.Prefab,
                    info.BuildingRole,
                    info.ApplyMaterialVariants);
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

                        var roadCell = new Vector2Int(column, row);
                        bool dirtRoad = _dirtRoadCells.Contains(roadCell);
                        float roadHalfExtent = dirtRoad
                            ? DirtRoadVisualHalfExtent
                            : RoadVisualHalfExtent;
                        float centerOffset = dirtRoad ? RoadGridSize * 0.5f : 0f;
                        float roadX = _roadOrigin.x + column * RoadGridSize + centerOffset;
                        float roadZ = _roadOrigin.z + row * RoadGridSize + centerOffset;
                        var roadBounds = new Rect(
                            roadX - roadHalfExtent,
                            roadZ - roadHalfExtent,
                            roadHalfExtent * 2f,
                            roadHalfExtent * 2f);
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

        private enum FrontageSnapEdge
        {
            None,
            MinimumX,
            MaximumX,
            MinimumZ,
            MaximumZ
        }

        private const float RoadGridSize = 10f;
        private const float WestCityExpansion = 512f;
        private const float SouthCityExpansion = 128f;
        private const float NorthCityExpansion = 128f;
        private const int RoadChunkSize = 16;
        private const float BuildingVisualScale = 0.82f;
        private const string DenseCityGeneratorSchema = "dense-city-v1";
        private const int DenseCityGenerationBuildingCapacity = 100_000;
        private const int DenseCityGenerationSurfaceCapacity = DenseCityGenerationBuildingCapacity * 2;
        private const int DenseCityGenerationPresentationCapacity = DenseCityGenerationBuildingCapacity * 8;
        private const int DenseCityBuildingSurfaceLayer = 0;
        private const uint DenseCityBuildingMovementMask =
            (uint)(MapSurfaceMovementMask.AllGroundUnits |
                   MapSurfaceMovementMask.AirGrounded |
                   MapSurfaceMovementMask.BuildingPlacement);
        private const float CivicHallVisualScale = 2.75f;
        private const float CentralHallVisualScale = 2.35f;
        private const float CentralClockTowerVisualScale = 3.25f;
        private const float CentralTowerVisualScale = 2.85f;
        private const float CentralLargeBuildingVisualScale = 2.5f;
        private const float SidewalkBuildingRoadSetback = 0f;
        private const float DirtBuildingRoadSetback = 0.45f;
        private const int BoulevardLaneSeparationCells = 1;
        private const float BoulevardCenterStripWidth = 1.35f;
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

        private static readonly string[] RooftopUtilityPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Satellite_Dish_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Satellite_Dish_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_TV_Antenna_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_TV_Antenna_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Airvent_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Military/SM_Prop_Airvent_02.prefab"
        };

        private static readonly string[] ShopWallPropPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Airconditioner_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Airconditioner_04.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Airconditioner_06.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Shop_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Shop_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Shop_04.prefab",
            "Assets/PolygonMilitary/Prefabs/Props/Signs/SM_Prop_Sign_Shop_06.prefab"
        };

        private const string CivicClothCoverPrefabPath =
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Village_ClothCover_Large_02.prefab";

        private const string CivicUmbrellaPrefabPath =
            "Assets/PolygonMilitary/Prefabs/Props/SM_Prop_Umbrella_02.prefab";

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

        private static readonly string[] Demo2CanalTreePrefabPaths =
        {
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Tree_01.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Tree_02.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Tree_03.prefab",
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Generic/SM_Generic_Tree_04.prefab"
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

        private const string BoulevardMedianTreePrefabPath =
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_01.prefab";

        private const string GrassPrefabPath =
            "Assets/Game/Prefabs/Environment/Decorations/SM_Env_Grass_04.prefab";

        private const string MainStreetBushPrefabPath =
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Tree_Bush_01.prefab";

        private static readonly string[] CanalBankPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Round_01.prefab",
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Round_02.prefab",
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Round_03.prefab",
            "Assets/PolygonMilitary/Prefabs/Environment/SM_Env_Ground_Round_04.prefab"
        };

        private const string CanalBridgePrefabPath =
            "Assets/Synty/PolygonBattleRoyale/Prefabs/Environments/SM_Env_Bridge_01.prefab";

        private static readonly string[] HorizonMountainPrefabPaths =
        {
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_01.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_02.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_03.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_04.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_05.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_06.prefab",
            "Assets/Game/Prefabs/Environment/Blockers/SM_Env_Mountain_07.prefab"
        };

        private const string CanalWaterMaterialPath =
            "Assets/Synty/PolygonBattleRoyale/Materials/FX/PolygonBattleRoyale_Water.mat";

        private const string CanalSurfacePrefabPath =
            "Assets/Synty/PolygonGeneric/Prefabs/Environment/SM_Gen_Env_Water_Plane_01.prefab";

        private const string CanalBedMaterialPath =
            "Assets/Synty/PolygonGeneric/Materials/Generic_Water.mat";

        private const string CanalGreenMaterialPath =
            "Assets/Synty/PolygonGeneric/Materials/Generic_Grass.mat";

        private static readonly string[] CleanStandaloneShopPrefabPaths =
        {
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_04.prefab",
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Shop_08.prefab"
        };

        private const string RoofCap03PrefabPath =
            "Assets/PolygonMilitary/Prefabs/Buildings/SM_Bld_Roof_Cap_03.prefab";

        private static readonly Dictionary<GameObject, bool> DenseCityPrefabUsabilityCache = new();

        internal static Vector2 GetRoadGridOrigin(RuntimeCityRAndDMapView view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));
            return new Vector2(
                view.GridOrigin.x - WestCityExpansion,
                view.GridOrigin.z - SouthCityExpansion);
        }

        public static Result Build(
            RuntimeCityRAndDMapView view,
            Transform generatedRoot,
            RuntimeCitySpawnerSystemConfig config)
        {
            return Build(view, generatedRoot, config, null);
        }

        internal static Result Build(
            RuntimeCityRAndDMapView view,
            Transform generatedRoot,
            RuntimeCitySpawnerSystemConfig config,
            DenseCityProtectedAutobahnRouteDescriptor protectedAutobahnReplacement)
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
            var approvedMapSurfaceBounds = new Rect(
                runtimeGridOrigin.x,
                runtimeGridOrigin.z,
                runtimeGridWidth,
                runtimeGridDepth);
            var authoredCoreBounds = new Rect(
                mapCenter.x - 130f,
                mapCenter.z - 95f,
                260f,
                240f);
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
            using var generationTransactions = new DenseCityGenerationTransactionContext(
                DenseCityGenerationBuildingCapacity,
                DenseCityGenerationSurfaceCapacity,
                DenseCityGenerationPresentationCapacity);
            var protectedOverlapRenderers = new HashSet<Renderer>();
            RoadBakeResult roadResult = BakeRoadNetwork(
                generatedRoot,
                cityOrigin,
                cityWidth,
                cityDepth,
                approvedMapSurfaceBounds,
                authoredCoreBounds,
                new Vector2(mapCenter.x, mapCenter.z),
                cityFootprint,
                terrainMap,
                config.RandomSeed,
                surface,
                generationTransactions,
                protectedAutobahnReplacement);
            CanalBakeResult canalResult = BakeWaterCanals(
                generatedRoot,
                cityOrigin,
                cityWidth,
                cityDepth,
                authoredCoreBounds,
                new Vector2(mapCenter.x, mapCenter.z),
                cityFootprint,
                terrainMap,
                roadResult,
                authoredGradeElevation,
                config.RandomSeed,
                approvedMapSurfaceBounds,
                generationTransactions,
                protectedOverlapRenderers);
            Debug.Log(
                $"[DenseCityCanals] routes={canalResult.RouteCount} waterTiles={canalResult.WaterTiles} " +
                $"bridges={canalResult.Bridges} greenBanks={canalResult.GreenBanks} " +
                $"parkAreas={canalResult.ParkAreas} trees={canalResult.Trees} " +
                $"bushes={canalResult.Bushes} streetLights={canalResult.StreetLights} " +
                $"highwayConflicts={canalResult.HighwayConflicts}");
            if (canalResult.HighwayConflicts > 0)
            {
                throw new InvalidOperationException(
                    $"Dense city canal generation crossed {canalResult.HighwayConflicts} highway cells.");
            }
            DenseCityBuildingMaterialLibrary materialLibrary =
                DenseCityBuildingMaterialLibrary.LoadExisting();
            int authoredCoreRenderers = BakeCivicBazaarCore(
                generatedRoot,
                view,
                config,
                mapCenter,
                terrainMap,
                surface,
                roadResult.RoadCells,
                roadResult.DirtRoadCells,
                cityOrigin,
                materialLibrary,
                generationTransactions);
            BuildingBakeResult buildingResult = BakeDenseDistricts(
                generatedRoot,
                view,
                config,
                cityOrigin,
                cityWidth,
                cityDepth,
                approvedMapSurfaceBounds,
                roadResult.StreetColumns,
                roadResult.StreetRows,
                roadResult.RoadCells,
                roadResult.DirtRoadCells,
                roadResult.BoulevardRoadCells,
                authoredCoreBounds,
                cityFootprint,
                terrainMap,
                surface,
                materialLibrary,
                generationTransactions);

            BuildingMaterialVariantResult materialVariants = ApplyBuildingMaterialVariants(
                generationTransactions.RealizedBuildingOwners,
                config.RandomSeed,
                materialLibrary);
            Debug.Log(
                $"[DenseCityBuildingMaterials] buildingsA={materialVariants.BuildingsA} " +
                $"buildingsB={materialVariants.BuildingsB} buildingsC={materialVariants.BuildingsC} " +
                $"materialSlotsChanged={materialVariants.MaterialSlotsChanged}");

            int roofDetails = AddShopRoofDetails(
                generationTransactions.RealizedBuildingOwners,
                generationTransactions);
            List<Rect> openGroundBuildingFootprints = CollectGeneratedBuildingFootprints(
                generationTransactions.RealizedBuildingOwners);
            UrbanDetailResult urbanDetails = AddUrbanDetailProps(
                generatedRoot,
                generationTransactions.RealizedBuildingOwners,
                generationTransactions,
                cityOrigin,
                cityWidth,
                cityDepth,
                approvedMapSurfaceBounds,
                cityFootprint,
                authoredCoreBounds,
                roadResult.RoadCells,
                roadResult.DirtRoadCells,
                roadResult.BoulevardRoadCells,
                roadResult.BoulevardMedianCells,
                authoredGradeElevation,
                config.RandomSeed);
            OpenGroundDetailResult openGroundDetails = AddOpenGroundDetails(
                generatedRoot,
                cityOrigin,
                cityWidth,
                cityDepth,
                approvedMapSurfaceBounds,
                cityFootprint,
                authoredCoreBounds,
                roadResult.RoadCells,
                openGroundBuildingFootprints,
                urbanDetails.OpenGroundExclusionAreas,
                authoredGradeElevation,
                config.RandomSeed,
                generationTransactions);
            Debug.Log(
                $"[DenseCityDetailPass] roofCaps={roofDetails} " +
                $"openGroundPatches={openGroundDetails.Visuals}");
            Debug.Log(
                $"[DenseCityUrbanProps] waterTanks={urbanDetails.WaterTanks} " +
                $"rooftopUtilities={urbanDetails.RooftopUtilities} " +
                $"shopWallProps={urbanDetails.ShopWallProps} " +
                $"streetProps={urbanDetails.StreetProps} trees={urbanDetails.Trees} " +
                $"rocks={urbanDetails.Rocks} courtyards={urbanDetails.Courtyards} " +
                $"courtyardWalls={urbanDetails.CourtyardWalls} " +
                $"courtyardPillars={urbanDetails.CourtyardPillars} " +
                $"courtyardWells={urbanDetails.CourtyardWells} " +
                $"courtyardBushes={urbanDetails.CourtyardBushes} " +
                $"courtyardGroundPatchesRemoved={urbanDetails.CourtyardGroundPatchesRemoved} " +
                $"powerPoles={urbanDetails.PowerPoles} powerLines={urbanDetails.PowerLines} " +
                $"streetLights={urbanDetails.StreetLights} " +
                $"boulevardMedianTrees={urbanDetails.BoulevardMedianTrees} " +
                $"boulevardMedianLights={urbanDetails.BoulevardMedianLights} " +
                $"grassPatches={urbanDetails.GrassPatches} " +
                $"mainStreetBushes={urbanDetails.MainStreetBushes}");

            int horizonMountains = BakeHorizonMountainPerimeter(
                generatedRoot,
                cityOrigin,
                cityWidth,
                cityDepth,
                authoredGradeElevation,
                protectedAreas,
                config.RandomSeed,
                generationTransactions);
            Debug.Log($"[DenseCityHorizon] backgroundMountains={horizonMountains}");

            generationTransactions.Seal();
            ValidateRealizedBuildingAttachmentOwnership(generationTransactions);
            int semanticRoadShoulders = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "road-shoulder");
            int semanticCanalWaterExclusions = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "canal-water-exclusion");
            int semanticCanalBedPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-bed-visual");
            int semanticCanalWaterPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-water-visual");
            int semanticCanalBankTerrains = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "canal-bank-terrain");
            int semanticCanalBankBasePresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-bank-base-visual");
            int semanticCanalBankPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-bank-visual");
            int semanticCanalParkTerrains = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "canal-park-terrain");
            int semanticCanalParkBasePresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-park-base-visual");
            int semanticCanalParkPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-park-visual");
            int semanticCanalTrees = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-tree-visual");
            int semanticCanalBushes = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-bush-visual");
            int semanticCanalLights = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "canal-light-visual");
            int semanticCivicBuildings = CountBuildingRecords(
                generationTransactions.Records.Buildings,
                "civic-building");
            int semanticCivicFoundations = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "civic-foundation");
            int semanticCivicBlockers = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "civic-blocker");
            int semanticCivicIntactPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "civic-building-intact");
            int semanticCivicDestroyedPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "civic-building-destroyed");
            int semanticCivicRoads = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "civic-road");
            int semanticCivicRoadPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "civic-road-visual");
            int semanticCivicRoadTerrains = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "civic-road-terrain-patch");
            int semanticCivicRoadTerrainPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "civic-road-terrain-patch-visual");
            int semanticHorizonMountains = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "horizon-mountain-visual");
            int semanticBoulevardMedianTrees = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "boulevard-median-tree-visual");
            int semanticBoulevardMedianLights = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "boulevard-median-light-visual");
            int semanticSidewalkStreetLights = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "sidewalk-street-light-visual");
            int semanticGrassPatches = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "free-ground-grass-visual");
            int semanticMainStreetBushes = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "main-street-bush-visual");
            int semanticPowerPoles = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "power-pole-visual");
            int semanticPowerLines = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "power-line-visual");
            int semanticCourtyardWalls = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "courtyard-wall");
            int semanticCourtyardWallPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "courtyard-wall-visual");
            int semanticCourtyardPillars = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "courtyard-pillar-visual");
            int semanticCourtyardWells = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "courtyard-well-visual");
            int semanticCourtyardBushes = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "courtyard-bush-visual");
            int semanticStreetProps = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "street-prop-visual");
            int semanticUrbanTrees = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "urban-tree-visual");
            int semanticUrbanRocks = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "urban-rock");
            int semanticUrbanRockPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "urban-rock-visual");
            int semanticCivicFountains = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "civic-fountain-visual");
            int semanticOpenGroundTerrains = CountSurfaceRecords(
                generationTransactions.Records.Surfaces,
                "open-ground-terrain");
            int semanticOpenGroundPresentations = CountPresentationRecords(
                generationTransactions.Records.Presentations,
                "open-ground-visual");
            if (semanticCanalWaterExclusions != canalResult.GameplayWaterExclusions ||
                semanticCanalBedPresentations != canalResult.WaterTiles ||
                semanticCanalWaterPresentations != canalResult.WaterTiles)
            {
                throw new InvalidOperationException(
                    $"Canal semantic parity failed: tiles={canalResult.WaterTiles} " +
                    $"exclusions={semanticCanalWaterExclusions}/" +
                    $"{canalResult.GameplayWaterExclusions} beds={semanticCanalBedPresentations} " +
                    $"water={semanticCanalWaterPresentations}.");
            }
            if (canalResult.GreenBanks % 3 != 0 ||
                semanticCanalBankTerrains != canalResult.GameplayBankTerrains ||
                semanticCanalBankBasePresentations != canalResult.GreenBanks ||
                semanticCanalBankPresentations != canalResult.GreenBanks)
            {
                throw new InvalidOperationException(
                    $"Canal bank semantic parity failed: patches={canalResult.GreenBanks} " +
                    $"terrains={semanticCanalBankTerrains}/" +
                    $"{canalResult.GameplayBankTerrains} bases={semanticCanalBankBasePresentations} " +
                    $"visuals={semanticCanalBankPresentations}.");
            }
            const int CanalParkPatches = 5;
            if (semanticCanalParkTerrains != canalResult.GameplayParkTerrains ||
                semanticCanalParkBasePresentations != canalResult.ParkAreas * CanalParkPatches ||
                semanticCanalParkPresentations != canalResult.ParkAreas * CanalParkPatches)
            {
                throw new InvalidOperationException(
                    $"Canal park semantic parity failed: parks={canalResult.ParkAreas} " +
                    $"terrains={semanticCanalParkTerrains}/" +
                    $"{canalResult.GameplayParkTerrains} bases={semanticCanalParkBasePresentations} " +
                    $"visuals={semanticCanalParkPresentations}.");
            }
            if (semanticCanalTrees != canalResult.Trees ||
                semanticCanalBushes != canalResult.Bushes ||
                semanticCanalLights != canalResult.StreetLights)
            {
                throw new InvalidOperationException(
                    $"Canal detail semantic parity failed: " +
                    $"trees={canalResult.Trees}/{semanticCanalTrees} " +
                    $"bushes={canalResult.Bushes}/{semanticCanalBushes} " +
                    $"lights={canalResult.StreetLights}/{semanticCanalLights}.");
            }
            if (semanticCivicBuildings <= 0 ||
                semanticCivicFoundations != semanticCivicBuildings ||
                semanticCivicBlockers != semanticCivicBuildings ||
                semanticCivicIntactPresentations != semanticCivicBuildings ||
                semanticCivicDestroyedPresentations != semanticCivicBuildings)
            {
                throw new InvalidOperationException(
                    $"Civic building semantic parity failed: buildings={semanticCivicBuildings} " +
                    $"foundations={semanticCivicFoundations} blockers={semanticCivicBlockers} " +
                    $"intact={semanticCivicIntactPresentations} " +
                    $"destroyed={semanticCivicDestroyedPresentations}.");
            }
            if (semanticCivicRoads <= 0 ||
                semanticCivicRoadPresentations != semanticCivicRoads ||
                semanticCivicRoadTerrains <= 0 ||
                semanticCivicRoadTerrains != semanticCivicRoadTerrainPresentations)
            {
                throw new InvalidOperationException(
                    $"Civic road semantic parity failed: roads={semanticCivicRoads} " +
                    $"roadVisuals={semanticCivicRoadPresentations} " +
                    $"terrain={semanticCivicRoadTerrains} " +
                    $"terrainVisuals={semanticCivicRoadTerrainPresentations}.");
            }
            if (semanticHorizonMountains <= 0 || semanticHorizonMountains != horizonMountains)
            {
                throw new InvalidOperationException(
                    $"Horizon mountain semantic parity failed: " +
                    $"realized={horizonMountains} semantic={semanticHorizonMountains}.");
            }
            if (semanticBoulevardMedianTrees != urbanDetails.BoulevardMedianTrees ||
                semanticBoulevardMedianLights != urbanDetails.BoulevardMedianLights ||
                semanticSidewalkStreetLights != urbanDetails.StreetLights)
            {
                throw new InvalidOperationException(
                    $"Street-detail semantic parity failed: " +
                    $"medianTrees={urbanDetails.BoulevardMedianTrees}/{semanticBoulevardMedianTrees} " +
                    $"medianLights={urbanDetails.BoulevardMedianLights}/{semanticBoulevardMedianLights} " +
                    $"sidewalkLights={urbanDetails.StreetLights}/{semanticSidewalkStreetLights}.");
            }
            if (semanticGrassPatches != urbanDetails.GrassPatches ||
                semanticMainStreetBushes != urbanDetails.MainStreetBushes)
            {
                throw new InvalidOperationException(
                    $"Landscaping semantic parity failed: " +
                    $"grass={urbanDetails.GrassPatches}/{semanticGrassPatches} " +
                    $"mainStreetBushes={urbanDetails.MainStreetBushes}/{semanticMainStreetBushes}.");
            }
            if (semanticPowerPoles != urbanDetails.PowerPoles ||
                semanticPowerLines != urbanDetails.PowerLines)
            {
                throw new InvalidOperationException(
                    $"Power-network semantic parity failed: " +
                    $"poles={urbanDetails.PowerPoles}/{semanticPowerPoles} " +
                    $"lines={urbanDetails.PowerLines}/{semanticPowerLines}.");
            }
            if (semanticCourtyardWalls != urbanDetails.CourtyardWalls ||
                semanticCourtyardWallPresentations != urbanDetails.CourtyardWallVisuals ||
                semanticCourtyardPillars != urbanDetails.CourtyardPillars ||
                semanticCourtyardWells != urbanDetails.CourtyardWells ||
                semanticCourtyardBushes != urbanDetails.CourtyardBushes)
            {
                throw new InvalidOperationException(
                    $"Courtyard semantic parity failed: " +
                    $"walls={urbanDetails.CourtyardWalls}/{semanticCourtyardWalls} " +
                    $"visuals={urbanDetails.CourtyardWallVisuals}/" +
                    $"{semanticCourtyardWallPresentations} " +
                    $"pillars={urbanDetails.CourtyardPillars}/{semanticCourtyardPillars} " +
                    $"wells={urbanDetails.CourtyardWells}/{semanticCourtyardWells} " +
                    $"bushes={urbanDetails.CourtyardBushes}/{semanticCourtyardBushes}.");
            }
            if (semanticStreetProps != urbanDetails.StreetProps ||
                semanticUrbanTrees != urbanDetails.Trees ||
                semanticUrbanRocks != urbanDetails.GameplayRocks ||
                semanticUrbanRockPresentations != urbanDetails.Rocks)
            {
                throw new InvalidOperationException(
                    $"Natural-detail semantic parity failed: " +
                    $"streetProps={urbanDetails.StreetProps}/{semanticStreetProps} " +
                    $"trees={urbanDetails.Trees}/{semanticUrbanTrees} " +
                    $"rocks={urbanDetails.GameplayRocks}/{semanticUrbanRocks} " +
                    $"visuals={urbanDetails.Rocks}/" +
                    $"{semanticUrbanRockPresentations}.");
            }
            if (semanticCivicFountains != 2)
            {
                throw new InvalidOperationException(
                    $"Civic fountain semantic parity failed: expected=2 actual={semanticCivicFountains}.");
            }
            if (semanticOpenGroundTerrains != openGroundDetails.GameplayTerrains ||
                semanticOpenGroundPresentations != openGroundDetails.Visuals)
            {
                throw new InvalidOperationException(
                    $"Open-ground semantic parity failed: realized={openGroundDetails.Visuals} " +
                    $"terrain={semanticOpenGroundTerrains}/" +
                    $"{openGroundDetails.GameplayTerrains} visuals={semanticOpenGroundPresentations}.");
            }
            Debug.Log(
                $"[DenseCitySemanticRecords] buildings={generationTransactions.Records.Buildings.Count} " +
                $"surfaces={generationTransactions.Records.Surfaces.Count} " +
                $"presentations={generationTransactions.Records.Presentations.Count} " +
                $"roadShoulders={semanticRoadShoulders} " +
                $"canalWaterExclusions={semanticCanalWaterExclusions} " +
                $"canalBankTerrains={semanticCanalBankTerrains} " +
                $"canalParkTerrains={semanticCanalParkTerrains} " +
                $"canalTrees={semanticCanalTrees} canalBushes={semanticCanalBushes} " +
                $"canalLights={semanticCanalLights} " +
                $"civicBuildings={semanticCivicBuildings} civicRoads={semanticCivicRoads} " +
                $"horizonMountains={semanticHorizonMountains} " +
                $"boulevardMedianTrees={semanticBoulevardMedianTrees} " +
                $"boulevardMedianLights={semanticBoulevardMedianLights} " +
                $"sidewalkStreetLights={semanticSidewalkStreetLights} " +
                $"grassPatches={semanticGrassPatches} " +
                $"mainStreetBushes={semanticMainStreetBushes} " +
                $"powerPoles={semanticPowerPoles} powerLines={semanticPowerLines} " +
                $"courtyardWalls={semanticCourtyardWalls} " +
                $"courtyardPillars={semanticCourtyardPillars} " +
                $"courtyardWells={semanticCourtyardWells} " +
                $"courtyardBushes={semanticCourtyardBushes} " +
                $"streetProps={semanticStreetProps} urbanTrees={semanticUrbanTrees} " +
                $"urbanRocks={semanticUrbanRocks} civicFountains={semanticCivicFountains} " +
                $"openGroundTerrains={semanticOpenGroundTerrains}");

            int protectedOverlaps = AuditGeneratedProtectedOverlaps(
                generatedRoot,
                protectedAreas,
                protectedOverlapRenderers);
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
                authoredCoreRenderers,
                generationTransactions.Records.Buildings.Count,
                generationTransactions.RealizedBuildingAttachments.Count,
                generationTransactions.Records.Surfaces.Count,
                generationTransactions.Records.Presentations.Count,
                semanticRoadShoulders,
                semanticCanalWaterExclusions,
                semanticCanalBankTerrains,
                semanticCanalParkTerrains,
                semanticCanalTrees,
                semanticCanalBushes,
                semanticCanalLights,
                semanticCivicBuildings,
                semanticCivicRoads,
                semanticHorizonMountains,
                semanticBoulevardMedianTrees,
                semanticBoulevardMedianLights,
                semanticSidewalkStreetLights,
                semanticGrassPatches,
                semanticMainStreetBushes,
                semanticPowerPoles,
                semanticPowerLines,
                semanticCourtyardWalls,
                semanticCourtyardPillars,
                semanticCourtyardWells,
                semanticCourtyardBushes,
                semanticStreetProps,
                semanticUrbanTrees,
                semanticUrbanRocks,
                semanticCivicFountains,
                semanticOpenGroundTerrains,
                generationTransactions.Records.CreateSnapshot());
        }

        private static int CountBuildingRecords(
            IReadOnlyList<DenseCityBuildingBakeRecord> buildings,
            string recordKind)
        {
            int count = 0;
            for (int index = 0; index < buildings.Count; index++)
            {
                if (string.Equals(
                        buildings[index].Identity.Kind,
                        recordKind,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountSurfaceRecords(
            IReadOnlyList<DenseCitySurfaceBakeRecord> surfaces,
            string recordKind)
        {
            int count = 0;
            for (int index = 0; index < surfaces.Count; index++)
            {
                if (string.Equals(
                        surfaces[index].Identity.Kind,
                        recordKind,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountPresentationRecords(
            IReadOnlyList<DenseCityPresentationBakeRecord> presentations,
            string recordKind)
        {
            int count = 0;
            for (int index = 0; index < presentations.Count; index++)
            {
                if (string.Equals(
                        presentations[index].Identity.Kind,
                        recordKind,
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }
            return count;
        }

        private static void ValidateRealizedBuildingAttachmentOwnership(
            DenseCityGenerationTransactionContext generationTransactions)
        {
            IReadOnlyList<DenseCityRealizedBuildingOwner> owners =
                generationTransactions.RealizedBuildingOwners;
            var rootsByStableKey = new Dictionary<string, Transform>(owners.Count, StringComparer.Ordinal);
            for (int index = 0; index < owners.Count; index++)
            {
                DenseCityRealizedBuildingOwner owner = owners[index];
                rootsByStableKey.Add(owner.Building.Identity.StableKey, owner.IntactPresentationRoot);
            }

            IReadOnlyList<DenseCityRealizedBuildingAttachment> realizedAttachments =
                generationTransactions.RealizedBuildingAttachments;
            int semanticAttachmentCount = 0;
            IReadOnlyList<DenseCityPresentationBakeRecord> presentations =
                generationTransactions.Records.Presentations;
            for (int index = 0; index < presentations.Count; index++)
            {
                if (presentations[index].Category is DenseCityPresentationCategory.BuildingAttachmentIntact or
                    DenseCityPresentationCategory.BuildingAttachmentDestroyed)
                {
                    semanticAttachmentCount++;
                }
            }
            if (semanticAttachmentCount != realizedAttachments.Count)
            {
                throw new InvalidOperationException(
                    "Dense-city semantic and realized attachment counts differ. " +
                    $"semantic={semanticAttachmentCount} realized={realizedAttachments.Count}.");
            }

            for (int index = 0; index < realizedAttachments.Count; index++)
            {
                DenseCityRealizedBuildingAttachment attachment = realizedAttachments[index];
                string ownerStableKey = attachment.Presentation.BuildingOwnerStableKey;
                if (!rootsByStableKey.TryGetValue(ownerStableKey, out Transform ownerRoot) ||
                    attachment.PresentationRoot == null ||
                    attachment.PresentationRoot.parent != ownerRoot)
                {
                    throw new InvalidOperationException(
                        $"Dense-city attachment hierarchy owner mismatch: '{attachment.Presentation.Identity.StableKey}'.");
                }

                Matrix4x4 actualMatrix = attachment.PresentationRoot.localToWorldMatrix;
                for (int matrixIndex = 0; matrixIndex < 16; matrixIndex++)
                {
                    if (Mathf.Abs(actualMatrix[matrixIndex] - attachment.Presentation.WorldMatrix[matrixIndex]) > 0.0001f)
                    {
                        throw new InvalidOperationException(
                            $"Dense-city attachment transform drift: '{attachment.Presentation.Identity.StableKey}'.");
                    }
                }
            }
        }

        private static int AuditGeneratedProtectedOverlaps(
            Transform generatedRoot,
            ProtectedAreaMap protectedAreas,
            ISet<Renderer> protectedOverlapRenderers)
        {
            int overlapCount = 0;
            int protectedAutobahnReplacementRendererCount = 0;
            Renderer[] renderers = generatedRoot.GetComponentsInChildren<Renderer>(false);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                    continue;
                if (renderer.GetComponentInParent<
                        DenseCityProtectedAutobahnReplacementTileMarker>(true) != null)
                {
                    protectedAutobahnReplacementRendererCount++;
                    continue;
                }
                if (protectedOverlapRenderers.Contains(renderer) ||
                    !protectedAreas.Intersects(renderer.bounds))
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
                $"[DenseCityProtectedAudit] generatedRenderers={renderers.Length} " +
                $"protectedAutobahnReplacementRenderers=" +
                $"{protectedAutobahnReplacementRendererCount} overlaps={overlapCount}");
            return overlapCount;
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
            if (TryGetGeneratedCityBoundsForProof(view.GeneratedRoot, out Bounds generatedBounds))
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
                if (TryFindSidewalkFrontageProofView(
                        view.GeneratedRoot,
                        center,
                        out Vector3 frontageCamera,
                        out Vector3 frontageTarget))
                {
                    Capture(
                        frontageCamera,
                        frontageTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_sidewalk_frontage_snap.png"));
                }
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
                if (TryFindBoulevardMedianProofView(
                        view.GeneratedRoot,
                        out Vector3 boulevardCamera,
                        out Vector3 boulevardTarget))
                {
                    Capture(
                        boulevardCamera,
                        boulevardTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_asphalt_boulevard_median.png"));
                }
                if (TryFindCanalProofView(
                        view.GeneratedRoot,
                        out Vector3 canalCamera,
                        out Vector3 canalTarget))
                {
                    Capture(
                        canalCamera,
                        canalTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_water_canal_bridge_park.png"));
                }
                if (TryFindPairedCanalBridgeProofView(
                        view.GeneratedRoot,
                        out Vector3 pairedBridgeCamera,
                        out Vector3 pairedBridgeTarget))
                {
                    Capture(
                        pairedBridgeCamera,
                        pairedBridgeTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_canal_boulevard_bridge_pair.png"));
                }
                if (TryFindCanalParkProofView(
                        view.GeneratedRoot,
                        out Vector3 canalParkCamera,
                        out Vector3 canalParkTarget))
                {
                    Capture(
                        canalParkCamera,
                        canalParkTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_canal_pocket_park.png"));
                }
                if (TryFindRooftopPropProofView(
                        view.GeneratedRoot,
                        "Satellite_Dish",
                        "_RoofUtility_",
                        out Vector3 rooftopPropCamera,
                        out Vector3 rooftopPropTarget))
                {
                    Capture(
                        rooftopPropCamera,
                        rooftopPropTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_rooftop_satellite_attachment.png"));
                }
                if (TryFindRooftopPropProofView(
                        view.GeneratedRoot,
                        "WaterTank",
                        "_Roof_",
                        out Vector3 rooftopTankCamera,
                        out Vector3 rooftopTankTarget))
                {
                    Capture(
                        rooftopTankCamera,
                        rooftopTankTarget,
                        orthographic: false,
                        orthographicSize: 0f,
                        1920,
                        1080,
                        Path.Combine(outputFolder, "dense_city_rooftop_water_tank_attachment.png"));
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

        private static bool TryFindRooftopPropProofView(
            Transform generatedRoot,
            string prefabNameToken,
            string placementNameToken,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            Transform selectedProp = null;
            Bounds selectedPropBounds = default;
            Bounds selectedBuildingBounds = default;
            float largestPropExtent = float.NegativeInfinity;
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    candidate.parent == null ||
                    !candidate.name.Contains(prefabNameToken, StringComparison.Ordinal) ||
                    !candidate.name.Contains(placementNameToken, StringComparison.Ordinal) ||
                    !TryGetWorldBounds(candidate, out Bounds propBounds) ||
                    !TryGetWorldBounds(candidate.parent, out Bounds buildingBounds))
                {
                    continue;
                }

                float propExtent = propBounds.extents.magnitude;
                if (propExtent <= largestPropExtent)
                    continue;

                selectedProp = candidate;
                selectedPropBounds = propBounds;
                selectedBuildingBounds = buildingBounds;
                largestPropExtent = propExtent;
            }

            if (selectedProp == null)
            {
                cameraPosition = default;
                target = default;
                return false;
            }

            target = new Vector3(
                selectedPropBounds.center.x,
                Mathf.Lerp(selectedBuildingBounds.max.y, selectedPropBounds.center.y, 0.45f),
                selectedPropBounds.center.z);
            Vector3 viewDirection = new Vector3(-1f, 0f, -1f).normalized;
            float framingDistance = Mathf.Clamp(
                Mathf.Max(
                    Mathf.Max(selectedBuildingBounds.size.x, selectedBuildingBounds.size.z) * 1.7f,
                    largestPropExtent * 4.5f),
                18f,
                42f);
            cameraPosition = target + viewDirection * framingDistance + Vector3.up * 10f;
            return true;
        }

        private static bool TryFindCanalProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    !candidate.name.StartsWith("CanalBridge_", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                target = bounds.center + Vector3.up * 1.5f;
                cameraPosition = target + new Vector3(-34f, 29f, -40f);
                return true;
            }

            cameraPosition = default;
            target = default;
            return false;
        }

        private static bool TryFindPairedCanalBridgeProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            var bridges = new List<Transform>();
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate != null &&
                    candidate.name.StartsWith("CanalBridge_", StringComparison.Ordinal))
                {
                    bridges.Add(candidate);
                }
            }

            for (int firstIndex = 0; firstIndex < bridges.Count; firstIndex++)
            {
                if (!TryGetWorldBounds(bridges[firstIndex], out Bounds firstBounds))
                    continue;
                for (int secondIndex = firstIndex + 1; secondIndex < bridges.Count; secondIndex++)
                {
                    if (!TryGetWorldBounds(bridges[secondIndex], out Bounds secondBounds))
                        continue;

                    Vector2 firstCenter = new(firstBounds.center.x, firstBounds.center.z);
                    Vector2 secondCenter = new(secondBounds.center.x, secondBounds.center.z);
                    float separation = Vector2.Distance(firstCenter, secondCenter);
                    if (separation < RoadGridSize * 0.75f ||
                        separation > RoadGridSize * 1.25f)
                    {
                        continue;
                    }

                    target = (firstBounds.center + secondBounds.center) * 0.5f + Vector3.up * 1.5f;
                    cameraPosition = target + new Vector3(-30f, 32f, -38f);
                    return true;
                }
            }

            cameraPosition = default;
            target = default;
            return false;
        }

        private static bool TryFindCanalParkProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    !candidate.name.StartsWith("CanalPocketPark_", StringComparison.Ordinal) ||
                    candidate.name.Contains("_Round_", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                target = bounds.center + Vector3.up * 1.25f;
                cameraPosition = target + new Vector3(-24f, 20f, -28f);
                return true;
            }

            cameraPosition = default;
            target = default;
            return false;
        }

        private static bool TryFindSidewalkFrontageProofView(
            Transform generatedRoot,
            Vector3 civicCenter,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform selected = null;
            Bounds selectedBounds = default;
            float bestDistance = float.PositiveInfinity;
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    candidate.name.IndexOf("_SidewalkFrontage_", StringComparison.Ordinal) < 0 ||
                    !TryGetWorldBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                Vector2 candidatePosition = new(bounds.center.x, bounds.center.z);
                Vector2 centerPosition = new(civicCenter.x, civicCenter.z);
                float distance = Vector2.Distance(candidatePosition, centerPosition);
                if (distance < bestDistance)
                {
                    selected = candidate;
                    selectedBounds = bounds;
                    bestDistance = distance;
                }
            }

            if (selected == null)
            {
                cameraPosition = default;
                target = default;
                return false;
            }

            Vector3 roadSide = selected.name.EndsWith("MinimumX", StringComparison.Ordinal)
                ? Vector3.left
                : selected.name.EndsWith("MaximumX", StringComparison.Ordinal)
                    ? Vector3.right
                    : selected.name.EndsWith("MinimumZ", StringComparison.Ordinal)
                        ? Vector3.back
                        : Vector3.forward;
            Vector3 alongFrontage = Vector3.Cross(Vector3.up, roadSide).normalized;
            target = selectedBounds.center + Vector3.up * Mathf.Min(2f, selectedBounds.extents.y * 0.25f);
            cameraPosition = target + roadSide * 22f + alongFrontage * 12f + Vector3.up * 11f;
            return true;
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

        private static bool TryFindBoulevardMedianProofView(
            Transform generatedRoot,
            out Vector3 cameraPosition,
            out Vector3 target)
        {
            Transform[] transforms = generatedRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                if (candidate == null ||
                    !candidate.name.StartsWith("SM_Env_Road_Lights_01_BoulevardMedianLight_", StringComparison.Ordinal) ||
                    !TryGetWorldBounds(candidate, out Bounds bounds))
                {
                    continue;
                }

                Vector3 corridorDirection = candidate.right;
                corridorDirection.y = 0f;
                if (corridorDirection.sqrMagnitude < 0.1f)
                    corridorDirection = Vector3.right;
                corridorDirection.Normalize();
                Vector3 acrossCorridor = Vector3.Cross(Vector3.up, corridorDirection).normalized;
                target = bounds.center + corridorDirection * 10f - Vector3.up * 1.5f;
                cameraPosition = target - corridorDirection * 30f + acrossCorridor * 23f + Vector3.up * 16f;
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

        private static bool TryGetGeneratedCityBoundsForProof(Transform generatedRoot, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            for (int childIndex = 0; childIndex < generatedRoot.childCount; childIndex++)
            {
                Transform child = generatedRoot.GetChild(childIndex);
                if (child == null || child.name == "DenseCity_HorizonMountainPerimeter")
                    continue;
                if (!TryGetWorldBounds(child, out Bounds childBounds))
                    continue;

                if (!found)
                {
                    bounds = childBounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(childBounds);
                }
            }

            return found;
        }

        private readonly struct RoadBakeResult
        {
            public readonly int TileCount;
            public readonly int ChunkCount;
            public readonly List<int> StreetColumns;
            public readonly List<int> StreetRows;
            public readonly HashSet<Vector2Int> DirtRoadCells;
            public readonly HashSet<Vector2Int> CivicRoadCells;
            public readonly HashSet<Vector2Int> RoadCells;
            public readonly HashSet<Vector2Int> BoulevardRoadCells;
            public readonly List<BoulevardMedianCell> BoulevardMedianCells;
            public readonly Dictionary<Vector2Int, GameObject> RoadTileObjects;
            public readonly Dictionary<Vector2Int, GameObject> RoadGroundPatchObjects;

            public RoadBakeResult(
                int tileCount,
                int chunkCount,
                List<int> streetColumns,
                List<int> streetRows,
                HashSet<Vector2Int> dirtRoadCells,
                HashSet<Vector2Int> civicRoadCells,
                HashSet<Vector2Int> roadCells,
                HashSet<Vector2Int> boulevardRoadCells,
                List<BoulevardMedianCell> boulevardMedianCells,
                Dictionary<Vector2Int, GameObject> roadTileObjects,
                Dictionary<Vector2Int, GameObject> roadGroundPatchObjects)
            {
                TileCount = tileCount;
                ChunkCount = chunkCount;
                StreetColumns = streetColumns;
                StreetRows = streetRows;
                DirtRoadCells = dirtRoadCells;
                CivicRoadCells = civicRoadCells;
                RoadCells = roadCells;
                BoulevardRoadCells = boulevardRoadCells;
                BoulevardMedianCells = boulevardMedianCells;
                RoadTileObjects = roadTileObjects;
                RoadGroundPatchObjects = roadGroundPatchObjects;
            }
        }

        private readonly struct NaturalGroundPatchPlan
        {
            public NaturalGroundPatchPlan(
                GameObject prefab,
                Material material,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Prefab = prefab;
                Material = material;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                WorldMatrix = Matrix4x4.TRS(position, rotation, scale);
            }

            public readonly GameObject Prefab;
            public readonly Material Material;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;
            public readonly Matrix4x4 WorldMatrix;
        }

        private readonly struct CanalBakeResult
        {
            public readonly int RouteCount;
            public readonly int WaterTiles;
            public readonly int GameplayWaterExclusions;
            public readonly int Bridges;
            public readonly int GreenBanks;
            public readonly int GameplayBankTerrains;
            public readonly int ParkAreas;
            public readonly int GameplayParkTerrains;
            public readonly int Trees;
            public readonly int Bushes;
            public readonly int StreetLights;
            public readonly int HighwayConflicts;

            public CanalBakeResult(
                int routeCount,
                int waterTiles,
                int gameplayWaterExclusions,
                int bridges,
                int greenBanks,
                int gameplayBankTerrains,
                int parkAreas,
                int gameplayParkTerrains,
                int trees,
                int bushes,
                int streetLights,
                int highwayConflicts)
            {
                RouteCount = routeCount;
                WaterTiles = waterTiles;
                GameplayWaterExclusions = gameplayWaterExclusions;
                Bridges = bridges;
                GreenBanks = greenBanks;
                GameplayBankTerrains = gameplayBankTerrains;
                ParkAreas = parkAreas;
                GameplayParkTerrains = gameplayParkTerrains;
                Trees = trees;
                Bushes = bushes;
                StreetLights = streetLights;
                HighwayConflicts = highwayConflicts;
            }
        }

        private sealed class CanalRoute
        {
            public readonly bool Horizontal;
            public readonly List<Vector2Int> Cells;

            public CanalRoute(bool horizontal, List<Vector2Int> cells)
            {
                Horizontal = horizontal;
                Cells = cells;
            }
        }

        private readonly struct BoulevardCorridor
        {
            public readonly bool Horizontal;
            public readonly int FirstLaneCoordinate;
            public readonly int SecondLaneCoordinate;

            public BoulevardCorridor(bool horizontal, int firstLaneCoordinate)
            {
                Horizontal = horizontal;
                FirstLaneCoordinate = firstLaneCoordinate;
                SecondLaneCoordinate = firstLaneCoordinate + BoulevardLaneSeparationCells;
            }
        }

        private readonly struct BoulevardMedianCell
        {
            public readonly Vector2Int FirstLaneCell;
            public readonly Vector2Int SecondLaneCell;
            public readonly bool Horizontal;

            public BoulevardMedianCell(
                Vector2Int firstLaneCell,
                Vector2Int secondLaneCell,
                bool horizontal)
            {
                FirstLaneCell = firstLaneCell;
                SecondLaneCell = secondLaneCell;
                Horizontal = horizontal;
            }

            public Vector2 WorldCenter(Vector3 mapOrigin) =>
                (RoadCellWorldCenter(FirstLaneCell, mapOrigin) +
                 RoadCellWorldCenter(SecondLaneCell, mapOrigin)) * 0.5f;
        }

        private readonly struct BuildingBakeResult
        {
            public readonly int BuildingCount;
            public readonly int ParkCount;
            public readonly int CentralLandmarkCount;
            public readonly int SnappedFrontageCount;

            public BuildingBakeResult(
                int buildingCount,
                int parkCount,
                int centralLandmarkCount,
                int snappedFrontageCount)
            {
                BuildingCount = buildingCount;
                ParkCount = parkCount;
                CentralLandmarkCount = centralLandmarkCount;
                SnappedFrontageCount = snappedFrontageCount;
            }
        }

        private readonly struct UrbanBlockBakeResult
        {
            public readonly int BuildingCount;
            public readonly int CentralLandmarkCount;
            public readonly int FrontageBuildingCount;

            public UrbanBlockBakeResult(
                int buildingCount,
                int centralLandmarkCount,
                int frontageBuildingCount = 0)
            {
                BuildingCount = buildingCount;
                CentralLandmarkCount = centralLandmarkCount;
                FrontageBuildingCount = frontageBuildingCount;
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
            public readonly DenseCityRealizedBuildingOwner Owner;
            public readonly DenseCityBuildingBakeRecord BuildingRecord;
            public readonly Transform Wrapper;
            public readonly GameObject SourcePrefab;
            public readonly Bounds Bounds;
            public readonly Bounds LocalBounds;
            public readonly Rect Footprint;
            public readonly bool IsShop;
            public readonly bool IsHouse;

            public GeneratedBuildingInfo(
                DenseCityRealizedBuildingOwner owner,
                Bounds bounds,
                Bounds localBounds)
            {
                Owner = owner;
                BuildingRecord = owner.Building;
                Wrapper = owner.IntactPresentationRoot;
                SourcePrefab = owner.SourcePrefab;
                Bounds = bounds;
                LocalBounds = localBounds;
                Footprint = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                IsShop = owner.Role == GeneratedCityBuildingRole.Shop;
                IsHouse = owner.Role == GeneratedCityBuildingRole.House;
            }
        }

        private readonly struct RoofTriangle
        {
            public readonly Vector3 A;
            public readonly Vector3 B;
            public readonly Vector3 C;

            public RoofTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
            }
        }

        private readonly struct UrbanDetailResult
        {
            public readonly List<Rect> OpenGroundExclusionAreas;
            public readonly int WaterTanks;
            public readonly int RooftopUtilities;
            public readonly int ShopWallProps;
            public readonly int StreetProps;
            public readonly int Trees;
            public readonly int Rocks;
            public readonly int GameplayRocks;
            public readonly int Courtyards;
            public readonly int CourtyardWalls;
            public readonly int CourtyardWallVisuals;
            public readonly int CourtyardPillars;
            public readonly int CourtyardWells;
            public readonly int CourtyardBushes;
            public readonly int CourtyardGroundPatchesRemoved;
            public readonly int PowerPoles;
            public readonly int PowerLines;
            public readonly int StreetLights;
            public readonly int BoulevardMedianTrees;
            public readonly int BoulevardMedianLights;
            public readonly int GrassPatches;
            public readonly int MainStreetBushes;

            public UrbanDetailResult(
                List<Rect> openGroundExclusionAreas,
                int waterTanks,
                int rooftopUtilities,
                int shopWallProps,
                int streetProps,
                int trees,
                int rocks,
                int gameplayRocks,
                int courtyards,
                int courtyardWalls,
                int courtyardWallVisuals,
                int courtyardPillars,
                int courtyardWells,
                int courtyardBushes,
                int courtyardGroundPatchesRemoved,
                int powerPoles,
                int powerLines,
                int streetLights,
                int boulevardMedianTrees,
                int boulevardMedianLights,
                int grassPatches,
                int mainStreetBushes)
            {
                OpenGroundExclusionAreas = openGroundExclusionAreas;
                WaterTanks = waterTanks;
                RooftopUtilities = rooftopUtilities;
                ShopWallProps = shopWallProps;
                StreetProps = streetProps;
                Trees = trees;
                Rocks = rocks;
                GameplayRocks = gameplayRocks;
                Courtyards = courtyards;
                CourtyardWalls = courtyardWalls;
                CourtyardWallVisuals = courtyardWallVisuals;
                CourtyardPillars = courtyardPillars;
                CourtyardWells = courtyardWells;
                CourtyardBushes = courtyardBushes;
                CourtyardGroundPatchesRemoved = courtyardGroundPatchesRemoved;
                PowerPoles = powerPoles;
                PowerLines = powerLines;
                StreetLights = streetLights;
                BoulevardMedianTrees = boulevardMedianTrees;
                BoulevardMedianLights = boulevardMedianLights;
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
            SurfacePlacementContext surface,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            Vector3 roadOrigin,
            DenseCityBuildingMaterialLibrary materialLibrary,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            var coreObject = new GameObject("DenseCity_PedestrianCivicBazaarCore");
            coreObject.transform.SetParent(generatedRoot, false);
            var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
            visualSystem.SetRuntimeRoot(coreObject.transform);
            visualSystem.EnsureCityVisualRoot();
            if (surface != null)
                visualSystem.ConfigureSurface(surface.Surface);

            GridConfig grid = CreateGrid(view);
            var placementContext = new BuildingPlacementContext(
                roadCells,
                roadOrigin,
                dirtRoadCells,
                config,
                materialLibrary,
                generationTransactions,
                visualSystem.CityVisualRoot);
            placementContext.SetDistrict(0);
            GameObject hallPrefab = FirstPrefab(config.HallPrefabs) ??
                                    throw new InvalidOperationException("Dense city config requires a hall prefab.");
            PrefabFootprint hall = MeasurePrefab(
                hallPrefab,
                CivicHallVisualScale,
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Civic,
                applyMaterialVariants: false);
            Vector3 hallPosition = mapCenter + new Vector3(0f, 0f, 55f);
            var hallCenter = new Vector2(hallPosition.x, hallPosition.z);
            bool hallRoadClearance = placementContext.CanPlace(hall, 180f, hallCenter);
            bool hallTerrainEvaluated = terrainMap.TryEvaluateBuilding(
                hallCenter,
                hall.Width,
                hall.Depth,
                out SurfacePatchEvaluation hallPatch);
            bool hallTerrainClearance = hallTerrainEvaluated && terrainMap.CanPlaceBuilding(hallPatch);
            Debug.Log(
                $"[DenseCityCivicHallPlacement] prefab={hall.Prefab.name} " +
                $"size={hall.Width:0.0}x{hall.Height:0.0}x{hall.Depth:0.0} " +
                $"scale={hall.VisualScale:0.00} roadClear={hallRoadClearance} " +
                $"terrainEvaluated={hallTerrainEvaluated} terrainClear={hallTerrainClearance}");
            int ownerCountBeforeHall = generationTransactions.RealizedBuildingOwners.Count;
            if (!SpawnBuilding(
                    visualSystem,
                    hall,
                    hallPosition,
                    180f,
                    grid,
                    terrainMap,
                    placementContext))
            {
                throw new InvalidOperationException(
                    "Dense city civic hall could not be placed inside its road loop.");
            }

            IReadOnlyList<DenseCityRealizedBuildingOwner> realizedOwners =
                generationTransactions.RealizedBuildingOwners;
            if (realizedOwners.Count != ownerCountBeforeHall + 1)
            {
                throw new InvalidOperationException(
                    "Dense city civic hall placement did not publish exactly one building owner.");
            }
            Transform hallVisual = realizedOwners[ownerCountBeforeHall].IntactPresentationRoot;
            if (!TryGetWorldBounds(hallVisual, out Bounds hallBounds))
                throw new InvalidOperationException("Dense city civic hall has no renderer bounds.");

            const float hallPlazaClearance = 28f;
            Rect hallPlazaExclusion = Rect.MinMaxRect(
                hallBounds.min.x - hallPlazaClearance,
                hallBounds.min.z - hallPlazaClearance,
                hallBounds.max.x + hallPlazaClearance,
                hallBounds.max.z + hallPlazaClearance);

            var market = new List<PrefabFootprint>();
            AddPrefabList(
                config.ShopPrefabs,
                market,
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Shop,
                0.9f);
            if (market.Count == 0)
                throw new InvalidOperationException("Dense city config requires shop prefabs for its bazaar.");

            var random = new System.Random(unchecked((int)config.RandomSeed) ^ 0x2ca44f);
            int shopIndex = 0;
            int civicRoadsideShops = AddCivicRoadsideBazaar(
                visualSystem,
                market,
                grid,
                mapCenter,
                roadOrigin,
                terrainMap,
                placementContext,
                random,
                hallPlazaExclusion);
            float[] marketRows = { -78f, -62f, -46f, -30f, -14f, 2f, 18f, 34f, 50f };
            for (int rowIndex = 0; rowIndex < marketRows.Length; rowIndex++)
            {
                float rowZ = marketRows[rowIndex];
                float facing = rowIndex % 2 == 0 ? 0f : 180f;
                float offset = rowIndex % 2 == 0 ? 0f : 5.75f;
                float cursor = -104f + offset;
                float limit = 104f;
                while (cursor < limit)
                {
                    PrefabFootprint shop = market[shopIndex++ % market.Count];
                    float centerX = cursor + shop.Width * 0.5f;
                    if (centerX + shop.Width * 0.5f > limit)
                        break;

                    Vector3 position = mapCenter + new Vector3(centerX, 0f, rowZ);
                    if (FootprintOverlapsRect(shop, facing, position, hallPlazaExclusion))
                    {
                        cursor += shop.Width + 0.08f;
                        continue;
                    }
                    SpawnBuilding(
                        visualSystem,
                        shop,
                        position,
                        facing,
                        grid,
                        terrainMap,
                        placementContext);
                    cursor += shop.Width + 0.08f;
                }
            }

            for (float z = -72f; z <= 54f; z += 14f)
            {
                PrefabFootprint leftShop = market[shopIndex++ % market.Count];
                Vector3 leftPosition = mapCenter + new Vector3(-116f, 0f, z);
                if (!FootprintOverlapsRect(leftShop, 90f, leftPosition, hallPlazaExclusion))
                {
                    SpawnBuilding(
                        visualSystem,
                        leftShop,
                        leftPosition,
                        90f,
                        grid,
                        terrainMap,
                        placementContext);
                }

                PrefabFootprint rightShop = market[shopIndex++ % market.Count];
                Vector3 rightPosition = mapCenter + new Vector3(116f, 0f, z);
                if (!FootprintOverlapsRect(rightShop, 270f, rightPosition, hallPlazaExclusion))
                {
                    SpawnBuilding(
                        visualSystem,
                        rightShop,
                        rightPosition,
                        270f,
                        grid,
                        terrainMap,
                        placementContext);
                }
            }

            int plazaDetails = AddCivicMarketPlazaDetails(
                coreObject.transform,
                generationTransactions.GetRequiredRealizedBuildingOwner(hallVisual),
                generationTransactions,
                hallBounds,
                hallPatch.MaximumHeight + 0.035f,
                config.RandomSeed,
                random);

            AddCivicPromenadeTrees(
                visualSystem,
                grid,
                mapCenter,
                terrainMap,
                placementContext,
                random);

            Debug.Log(
                $"[DenseCityCivicPlacementAudit] reserved={placementContext.ReservedCount} " +
                $"roadsideShops={civicRoadsideShops} plazaDetails={plazaDetails} " +
                $"hall={hallVisual.name} overlaps=0");

            DisableColliders(coreObject);
            SetStaticRecursively(coreObject);
            return CountActiveRenderers(coreObject);
        }

        private static int AddCivicRoadsideBazaar(
            RuntimeCityVisualPresentationSystemHelper visuals,
            List<PrefabFootprint> market,
            GridConfig grid,
            Vector3 civicCenter,
            Vector3 roadOrigin,
            TerrainViabilityMap terrainMap,
            BuildingPlacementContext placementContext,
            System.Random random,
            Rect hallPlazaExclusion)
        {
            const float frontageGap = DirtBuildingRoadSetback + 0.2f;
            const float packingGap = 0.08f;
            int count = 0;

            float leftRoadX = SnapRoadX(civicCenter.x - 60f);
            float rightRoadX = SnapRoadX(civicCenter.x + 60f);
            float southRoadZ = SnapRoadZ(civicCenter.z - 15f);
            float northRoadZ = SnapRoadZ(civicCenter.z + 125f);
            float hallRoadZ = SnapRoadZ(civicCenter.z + 55f);
            float centerRoadX = SnapRoadX(civicCenter.x);

            PlaceHorizontal(
                civicCenter.x - 122f,
                civicCenter.x + 122f,
                southRoadZ,
                placeOnPositiveSide: false);
            PlaceHorizontal(
                civicCenter.x - 122f,
                civicCenter.x + 122f,
                southRoadZ,
                placeOnPositiveSide: true);
            PlaceHorizontal(
                civicCenter.x - 122f,
                civicCenter.x + 122f,
                northRoadZ,
                placeOnPositiveSide: false);
            PlaceHorizontal(
                civicCenter.x - 122f,
                civicCenter.x + 122f,
                northRoadZ,
                placeOnPositiveSide: true);

            PlaceVertical(
                civicCenter.z - 86f,
                civicCenter.z + 116f,
                leftRoadX,
                placeOnPositiveSide: false);
            PlaceVertical(
                civicCenter.z - 86f,
                civicCenter.z + 116f,
                leftRoadX,
                placeOnPositiveSide: true);
            PlaceVertical(
                civicCenter.z - 86f,
                civicCenter.z + 116f,
                rightRoadX,
                placeOnPositiveSide: false);
            PlaceVertical(
                civicCenter.z - 86f,
                civicCenter.z + 116f,
                rightRoadX,
                placeOnPositiveSide: true);

            PlaceVertical(
                civicCenter.z - 88f,
                civicCenter.z - 25f,
                centerRoadX,
                placeOnPositiveSide: false);
            PlaceVertical(
                civicCenter.z - 88f,
                civicCenter.z - 25f,
                centerRoadX,
                placeOnPositiveSide: true);
            PlaceHorizontal(
                civicCenter.x - 124f,
                civicCenter.x - 70f,
                hallRoadZ,
                placeOnPositiveSide: false);
            PlaceHorizontal(
                civicCenter.x - 124f,
                civicCenter.x - 70f,
                hallRoadZ,
                placeOnPositiveSide: true);
            PlaceHorizontal(
                civicCenter.x + 70f,
                civicCenter.x + 124f,
                hallRoadZ,
                placeOnPositiveSide: false);
            PlaceHorizontal(
                civicCenter.x + 70f,
                civicCenter.x + 124f,
                hallRoadZ,
                placeOnPositiveSide: true);

            return count;

            float SnapRoadX(float worldX)
            {
                int column = Mathf.RoundToInt((worldX - roadOrigin.x) / RoadGridSize - 0.5f);
                return RoadCellWorldCenter(new Vector2Int(column, 0), roadOrigin).x;
            }

            float SnapRoadZ(float worldZ)
            {
                int row = Mathf.RoundToInt((worldZ - roadOrigin.z) / RoadGridSize - 0.5f);
                return RoadCellWorldCenter(new Vector2Int(0, row), roadOrigin).y;
            }

            void PlaceHorizontal(
                float minimumX,
                float maximumX,
                float roadZ,
                bool placeOnPositiveSide)
            {
                float cursor = minimumX;
                while (cursor < maximumX)
                {
                    PrefabFootprint info = market[random.Next(market.Count)];
                    float rotation = placeOnPositiveSide ? 180f : 0f;
                    float centerX = cursor + info.Width * 0.5f;
                    if (centerX + info.Width * 0.5f > maximumX)
                        break;
                    float direction = placeOnPositiveSide ? 1f : -1f;
                    float centerZ = roadZ + direction *
                        (RoadGridSize * 0.5f + frontageGap + info.Depth * 0.5f);
                    Vector3 position = new(centerX, grid.Origin.y, centerZ);
                    if (FootprintOverlapsRect(info, rotation, position, hallPlazaExclusion))
                    {
                        cursor += info.Width + packingGap;
                        continue;
                    }
                    if (SpawnBuilding(
                            visuals,
                            info,
                            position,
                            rotation,
                            grid,
                            terrainMap,
                            placementContext))
                    {
                        count++;
                    }

                    cursor += info.Width + packingGap;
                }
            }

            void PlaceVertical(
                float minimumZ,
                float maximumZ,
                float roadX,
                bool placeOnPositiveSide)
            {
                float cursor = minimumZ;
                while (cursor < maximumZ)
                {
                    PrefabFootprint info = market[random.Next(market.Count)];
                    float rotation = placeOnPositiveSide ? 270f : 90f;
                    float centerZ = cursor + info.Width * 0.5f;
                    if (centerZ + info.Width * 0.5f > maximumZ)
                        break;
                    float direction = placeOnPositiveSide ? 1f : -1f;
                    float centerX = roadX + direction *
                        (RoadGridSize * 0.5f + frontageGap + info.Depth * 0.5f);
                    Vector3 position = new(centerX, grid.Origin.y, centerZ);
                    if (FootprintOverlapsRect(info, rotation, position, hallPlazaExclusion))
                    {
                        cursor += info.Width + packingGap;
                        continue;
                    }
                    if (SpawnBuilding(
                            visuals,
                            info,
                            position,
                            rotation,
                            grid,
                            terrainMap,
                            placementContext))
                    {
                        count++;
                    }

                    cursor += info.Width + packingGap;
                }
            }
        }

        private static bool FootprintOverlapsRect(
            PrefabFootprint info,
            float rotationDegrees,
            Vector3 position,
            Rect exclusion)
        {
            bool quarterTurn = Mathf.Abs(Mathf.RoundToInt(rotationDegrees / 90f)) % 2 != 0;
            float halfWidth = (quarterTurn ? info.Depth : info.Width) * 0.5f;
            float halfDepth = (quarterTurn ? info.Width : info.Depth) * 0.5f;
            var footprint = Rect.MinMaxRect(
                position.x - halfWidth,
                position.z - halfDepth,
                position.x + halfWidth,
                position.z + halfDepth);
            return footprint.Overlaps(exclusion, true);
        }

        private static int AddCivicMarketPlazaDetails(
            Transform civicRoot,
            DenseCityRealizedBuildingOwner hallOwner,
            DenseCityGenerationTransactionContext generationTransactions,
            Bounds hallBounds,
            float supportHeight,
            uint seed,
            System.Random random)
        {
            GameObject clothCover = LoadRequiredPrefab(CivicClothCoverPrefabPath);
            GameObject umbrella = LoadRequiredPrefab(CivicUmbrellaPrefabPath);
            GameObject fountain01 = LoadRequiredPrefab(ParkPrefabPaths[4]);
            GameObject fountain02 = LoadRequiredPrefab(ParkPrefabPaths[5]);
            DenseCityVisualAssetMetadata fountain01Metadata =
                DenseCityVisualAssetMetadataExtractor.Extract(fountain01);
            DenseCityVisualAssetMetadata fountain02Metadata =
                DenseCityVisualAssetMetadataExtractor.Extract(fountain02);
            var rootObject = new GameObject("DenseCity_CivicMarketPlazaDetails");
            rootObject.transform.SetParent(civicRoot, false);

            float centerX = hallBounds.center.x;
            float centerZ = hallBounds.center.z;
            float frontZ = hallBounds.min.z - 13f;
            float outerFrontZ = hallBounds.min.z - 27f;
            float sideOffset = Mathf.Max(16f, hallBounds.extents.x + 13f);
            Vector2[] clothPositions =
            {
                new(centerX - 34f, frontZ),
                new(centerX, frontZ),
                new(centerX + 34f, frontZ),
                new(centerX - sideOffset, centerZ - 20f),
                new(centerX + sideOffset, centerZ - 20f),
                new(centerX - sideOffset, centerZ + 20f),
                new(centerX + sideOffset, centerZ + 20f)
            };
            Vector2[] umbrellaPositions =
            {
                new(centerX - 50f, outerFrontZ),
                new(centerX - 18f, outerFrontZ),
                new(centerX + 18f, outerFrontZ),
                new(centerX + 50f, outerFrontZ),
                new(centerX - sideOffset - 5f, centerZ),
                new(centerX + sideOffset + 5f, centerZ)
            };

            int count = 0;
            for (int index = 0; index < clothPositions.Length; index++)
            {
                if (InstantiateOwnedBuildingAttachment(
                        clothCover,
                        hallOwner,
                        $"{clothCover.name}_CivicPlaza_{index:00}",
                        clothPositions[index],
                        supportHeight,
                        index % 2 == 0 ? 0f : 90f,
                        1f,
                        generationTransactions))
                {
                    count++;
                }
            }

            for (int index = 0; index < umbrellaPositions.Length; index++)
            {
                if (InstantiateOwnedBuildingAttachment(
                        umbrella,
                        hallOwner,
                        $"{umbrella.name}_CivicPlaza_{index:00}",
                        umbrellaPositions[index],
                        supportHeight,
                        random.Next(0, 4) * 90f,
                        1.05f,
                        generationTransactions))
                {
                    count++;
                }
            }

            Vector2 fountainPosition01 = new(centerX - 26f, hallBounds.min.z - 43f);
            Vector2 fountainPosition02 = new(centerX + 26f, hallBounds.min.z - 43f);
            if (InstantiateTransactionalGroundedDetail(
                    fountain01,
                    fountain01Metadata,
                    rootObject.transform,
                    $"{fountain01.name}_CivicPlaza",
                    fountainPosition01,
                    supportHeight,
                    0f,
                    1.1f,
                    DenseCityPresentationCategory.Prop,
                    "civic-fountain-visual",
                    seed,
                    generationTransactions))
            {
                count++;
            }
            if (InstantiateTransactionalGroundedDetail(
                    fountain02,
                    fountain02Metadata,
                    rootObject.transform,
                    $"{fountain02.name}_CivicPlaza",
                    fountainPosition02,
                    supportHeight,
                    0f,
                    1.1f,
                    DenseCityPresentationCategory.Prop,
                    "civic-fountain-visual",
                    seed,
                    generationTransactions))
            {
                count++;
            }

            DisableColliders(rootObject);
            SetStaticRecursively(rootObject);
            Debug.Log(
                $"[DenseCityCivicMarketPlaza] clothCovers={clothPositions.Length} " +
                $"umbrellas={umbrellaPositions.Length} fountains=2 details={count}");
            return count;
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
                    treePrefabs.Add(MeasurePrefab(
                        prefab,
                        0.9f,
                        DenseCityPresentationCategory.Vegetation,
                        GeneratedCityBuildingRole.None));
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
            Rect mapSurfaceBounds,
            Rect authoredCoreBounds,
            Vector2 civicCenter,
            CityFootprint cityFootprint,
            TerrainViabilityMap terrainMap,
            uint seed,
            SurfacePlacementContext surface,
            DenseCityGenerationTransactionContext generationTransactions,
            DenseCityProtectedAutobahnRouteDescriptor protectedAutobahnReplacement)
        {
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));
            var roadObject = new GameObject("DenseCity_ConnectedSidewalkRoadNetwork");
            roadObject.transform.SetParent(generatedRoot, false);

            int maximumColumn = Mathf.FloorToInt(mapWidth / RoadGridSize) - 1;
            int maximumRow = Mathf.FloorToInt(mapDepth / RoadGridSize) - 1;
            var random = new System.Random(unchecked((int)(seed == 0 ? 26071501u : seed)) ^ 0x4a17b2);
            List<int> streetColumns = BuildIrregularStreetCoordinates(maximumColumn, random, 4, 13);
            List<int> streetRows = BuildIrregularStreetCoordinates(maximumRow, random, 4, 12);
            int centerColumn = maximumColumn / 2;
            int centerRow = maximumRow / 2;
            List<BoulevardCorridor> boulevardCorridors = BuildBoulevardCorridors(
                maximumColumn,
                maximumRow,
                centerColumn,
                centerRow);
            var boulevardColumns = new HashSet<int>();
            var boulevardRows = new HashSet<int>();
            for (int index = 0; index < boulevardCorridors.Count; index++)
            {
                BoulevardCorridor corridor = boulevardCorridors[index];
                HashSet<int> coordinates = corridor.Horizontal ? boulevardRows : boulevardColumns;
                List<int> streets = corridor.Horizontal ? streetRows : streetColumns;
                coordinates.Add(corridor.FirstLaneCoordinate);
                coordinates.Add(corridor.SecondLaneCoordinate);
                EnsureStreetCoordinate(streets, corridor.FirstLaneCoordinate);
                EnsureStreetCoordinate(streets, corridor.SecondLaneCoordinate);
            }
            var network = new RoadNetworkCompositionSystemHelper();

            for (int index = 0; index < boulevardCorridors.Count; index++)
            {
                BoulevardCorridor corridor = boulevardCorridors[index];
                if (corridor.Horizontal)
                {
                    AddHorizontalRoad(network, corridor.FirstLaneCoordinate, maximumColumn, mapOrigin, authoredCoreBounds, cityFootprint, terrainMap, isAutobahn: true, ignoreExclusion: true);
                    AddHorizontalRoad(network, corridor.SecondLaneCoordinate, maximumColumn, mapOrigin, authoredCoreBounds, cityFootprint, terrainMap, isAutobahn: true, ignoreExclusion: true);
                }
                else
                {
                    AddVerticalRoad(network, corridor.FirstLaneCoordinate, maximumRow, mapOrigin, authoredCoreBounds, cityFootprint, terrainMap, isAutobahn: true, ignoreExclusion: true);
                    AddVerticalRoad(network, corridor.SecondLaneCoordinate, maximumRow, mapOrigin, authoredCoreBounds, cityFootprint, terrainMap, isAutobahn: true, ignoreExclusion: true);
                }
            }

            for (int index = 0; index < streetRows.Count; index++)
            {
                if (boulevardRows.Contains(streetRows[index]))
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
                if (boulevardColumns.Contains(streetColumns[index]))
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
            var civicRoadCells = new HashSet<Vector2Int>();
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
            AddCivicCoreDirtRoads(
                network,
                mapOrigin,
                authoredCoreBounds,
                civicCenter,
                cityFootprint,
                terrainMap,
                dirtRoadCells,
                civicRoadCells);

            HashSet<Vector2Int> protectedAutobahnReplacementCells =
                AddProtectedAutobahnReplacement(
                    network,
                    protectedAutobahnReplacement);

            foreach (Vector2Int cell in network.StrokeIdsByCell.Keys)
            {
                if (cityFootprint.NormalizedDistance(RoadCellWorldCenter(cell, mapOrigin)) >= 0.72f)
                    dirtRoadCells.Add(cell);
            }
            dirtRoadCells.ExceptWith(network.AutobahnCells);
            dirtRoadCells.ExceptWith(network.AutobahnConnectorCells);
            List<BoulevardMedianCell> boulevardMedianCells = CollectBoulevardMedianCells(
                network,
                boulevardCorridors,
                maximumColumn,
                maximumRow,
                mapOrigin,
                authoredCoreBounds,
                cityFootprint);

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
                civicRoadCells,
                terrainMap,
                elevationPlan,
                surface,
                cityFootprint,
                mapSurfaceBounds,
                seed,
                generationTransactions,
                protectedAutobahnReplacementCells,
                out Dictionary<Vector2Int, GameObject> roadTileObjects,
                out Dictionary<Vector2Int, GameObject> roadGroundPatchObjects);
            SetStaticRecursively(roadObject);
            return new RoadBakeResult(
                network.RoadTiles.Count,
                chunkCount,
                streetColumns,
                streetRows,
                dirtRoadCells,
                civicRoadCells,
                new HashSet<Vector2Int>(network.RoadTiles.Keys),
                new HashSet<Vector2Int>(network.AutobahnCells),
                boulevardMedianCells,
                roadTileObjects,
                roadGroundPatchObjects);
        }

        internal static HashSet<Vector2Int> AddProtectedAutobahnReplacement(
            RoadNetworkCompositionSystemHelper network,
            DenseCityProtectedAutobahnRouteDescriptor descriptor)
        {
            var replacementCells = new HashSet<Vector2Int>();
            if (descriptor == null)
                return replacementCells;
            if (!DenseCityProtectedAutobahnReplacementPlanner.TryValidate(
                    descriptor,
                    out string error))
            {
                throw new InvalidOperationException(error);
            }

            var existingCells = new HashSet<Vector2Int>(network.StrokeIdsByCell.Keys);
            for (int rangeIndex = 0; rangeIndex < descriptor.LaneRanges.Count; rangeIndex++)
            {
                DenseCityProtectedAutobahnLaneRange range = descriptor.LaneRanges[rangeIndex];
                var lane = new List<Vector2Int>(
                    range.MaximumColumn - range.MinimumColumn + 1);
                for (int column = range.MinimumColumn;
                     column <= range.MaximumColumn;
                     column++)
                {
                    lane.Add(new Vector2Int(column, range.Row));
                }

                CommitRoadStroke(network, lane, isAutobahn: true);
            }

            const int MaximumConnectorDistanceCells = 8;
            int firstLaneRow = descriptor.LaneRanges[0].Row;
            int secondLaneRow = descriptor.LaneRanges[1].Row;
            var crossingColumns = existingCells
                .Where(cell =>
                    cell.x >= descriptor.LaneRanges[0].MinimumColumn &&
                    cell.x <= descriptor.LaneRanges[0].MaximumColumn &&
                    cell.y >= firstLaneRow - MaximumConnectorDistanceCells &&
                    cell.y <= secondLaneRow + MaximumConnectorDistanceCells &&
                    (network.HasEdge(cell, cell + Vector2Int.up) ||
                     network.HasEdge(cell, cell + Vector2Int.down)))
                .Select(cell => cell.x)
                .Distinct()
                .OrderBy(column => column)
                .ToArray();
            for (int index = 0; index < crossingColumns.Length; index++)
            {
                int column = crossingColumns[index];
                int? south = existingCells
                    .Where(cell =>
                        cell.x == column &&
                        cell.y < firstLaneRow &&
                        firstLaneRow - cell.y <= MaximumConnectorDistanceCells)
                    .Select(cell => (int?)cell.y)
                    .Max();
                int? north = existingCells
                    .Where(cell =>
                        cell.x == column &&
                        cell.y > secondLaneRow &&
                        cell.y - secondLaneRow <= MaximumConnectorDistanceCells)
                    .Select(cell => (int?)cell.y)
                    .Min();
                if (!south.HasValue && !north.HasValue)
                    continue;

                int minimumRow = south ?? firstLaneRow;
                int maximumRow = north ?? secondLaneRow;
                var connector = new List<Vector2Int>(maximumRow - minimumRow + 1);
                for (int row = minimumRow; row <= maximumRow; row++)
                    connector.Add(new Vector2Int(column, row));
                CommitRoadStroke(network, connector, isAutobahn: true);
            }

            foreach (Vector2Int cell in network.StrokeIdsByCell.Keys)
            {
                if (!existingCells.Contains(cell))
                    replacementCells.Add(cell);
            }
            if (replacementCells.Count == 0)
            {
                throw new InvalidOperationException(
                    "Protected Autobahn replacement did not add any road cells.");
            }

            return replacementCells;
        }

        private static int BakeHorizonMountainPerimeter(
            Transform generatedRoot,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            float gradeElevation,
            ProtectedAreaMap protectedAreas,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));
            GameObject[] mountainPrefabs = LoadRequiredPrefabs(HorizonMountainPrefabPaths);
            var metadataByPrefab = new Dictionary<GameObject, DenseCityVisualAssetMetadata>();
            var localBoundsByPrefab = new Dictionary<GameObject, Bounds>();
            for (int index = 0; index < mountainPrefabs.Length; index++)
            {
                GameObject prefab = mountainPrefabs[index];
                metadataByPrefab.Add(prefab, DenseCityVisualAssetMetadataExtractor.Extract(prefab));
                if (!TryGetPrefabLocalRendererBounds(prefab.transform, out Bounds localBounds))
                {
                    throw new InvalidOperationException(
                        $"Horizon mountain prefab '{prefab.name}' has no renderer bounds.");
                }
                localBoundsByPrefab.Add(prefab, localBounds);
            }
            var rootObject = new GameObject("DenseCity_HorizonMountainPerimeter");
            rootObject.transform.SetParent(generatedRoot, false);
            int created = 0;
            int longSideCount = Mathf.Max(10, Mathf.CeilToInt(mapWidth / 145f) + 2);
            int shortSideCount = Mathf.Max(9, Mathf.CeilToInt(mapDepth / 145f) + 2);

            for (int index = 0; index < longSideCount; index++)
            {
                float t = (index + 0.5f) / longSideCount;
                float x = mapOrigin.x + mapWidth * t;
                TryPlaceMountain(new Vector2(x, mapOrigin.z), Vector2.down, index, "South");
                TryPlaceMountain(new Vector2(x, mapOrigin.z + mapDepth), Vector2.up, index, "North");
            }

            for (int index = 0; index < shortSideCount; index++)
            {
                float t = (index + 0.5f) / shortSideCount;
                if (Mathf.Abs(t - 0.5f) < 0.12f)
                    continue;

                float z = mapOrigin.z + mapDepth * t;
                TryPlaceMountain(new Vector2(mapOrigin.x, z), Vector2.left, index, "West");
                TryPlaceMountain(new Vector2(mapOrigin.x + mapWidth, z), Vector2.right, index, "East");
            }

            if (created == 0)
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                return 0;
            }

            DisableColliders(rootObject);
            SetStaticRecursively(rootObject);
            return created;

            void TryPlaceMountain(Vector2 edgeCenter, Vector2 outward, int index, string sideName)
            {
                uint hash = HashGroundPatch(index, sideName[0], unchecked((int)seed) ^ 0x7a41);
                GameObject prefab = mountainPrefabs[(int)(hash % (uint)mountainPrefabs.Length)];
                DenseCityVisualAssetMetadata metadata = metadataByPrefab[prefab];
                Bounds localBounds = localBoundsByPrefab[prefab];
                Quaternion rotation = Quaternion.Euler(0f, Hash01(hash ^ 0x42d913afu) * 360f, 0f);
                Bounds sourceBounds = TransformLocalBounds(
                    localBounds,
                    Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one));
                float targetSpan = Mathf.Lerp(240f, 340f, Hash01(hash ^ 0x1d73b549u));
                float uniformScale = targetSpan /
                                     Mathf.Max(1f, Mathf.Max(sourceBounds.size.x, sourceBounds.size.z));
                Vector3 scale = Vector3.one * uniformScale;
                Bounds scaledBounds = TransformLocalBounds(
                    localBounds,
                    Matrix4x4.TRS(Vector3.zero, rotation, scale));
                float baseOffset = targetSpan * 0.3f + 55f;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    Vector2 target = edgeCenter + outward * (baseOffset + attempt * 180f);
                    var position = new Vector3(
                        target.x - scaledBounds.center.x,
                        gradeElevation - 18f - scaledBounds.min.y,
                        target.y - scaledBounds.center.z);
                    Matrix4x4 worldMatrix = Matrix4x4.TRS(position, rotation, scale);
                    Bounds placedBounds = TransformLocalBounds(localBounds, worldMatrix);
                    if (protectedAreas.Intersects(placedBounds))
                        continue;

                    GameObject instance = null;
                    try
                    {
                        bool accepted = generationTransactions.TryPlaceRenderOnlyPresentation(
                            0,
                            sequence => DenseCityRenderOnlyPresentationRecordFactory.Create(
                                new DenseCityRenderOnlyPresentationRecordInput(
                                    DenseCityGeneratorSchema,
                                    unchecked((int)seed),
                                    0,
                                    sequence,
                                    "horizon-mountain-visual",
                                    DenseCityPresentationCategory.Horizon,
                                    metadata.PrefabAssetGuid,
                                    metadata.PrefabLocalId,
                                    metadata.MaterialAssetGuids,
                                    worldMatrix,
                                    true,
                                    true,
                                    1)),
                            () =>
                            {
                                instance = DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                                    prefab,
                                    rootObject.transform);
                                if (instance == null)
                                    return false;
                                instance.name = $"HorizonMountain_{sideName}_{index:00}";
                                instance.transform.SetPositionAndRotation(position, rotation);
                                instance.transform.localScale = scale;
                                ValidateWorldMatrix(instance.transform, worldMatrix, "horizon mountain");
                                DisableColliders(instance);
                                return true;
                            });
                        if (accepted)
                            created++;
                        return;
                    }
                    catch
                    {
                        if (instance != null)
                            UnityEngine.Object.DestroyImmediate(instance);
                        throw;
                    }
                }
            }
        }

        private static Bounds TransformLocalBounds(Bounds localBounds, Matrix4x4 worldMatrix)
        {
            Vector3 center = worldMatrix.MultiplyPoint3x4(localBounds.center);
            Vector3 extents = localBounds.extents;
            Vector3 worldExtents = new(
                Mathf.Abs(worldMatrix.m00) * extents.x +
                Mathf.Abs(worldMatrix.m01) * extents.y +
                Mathf.Abs(worldMatrix.m02) * extents.z,
                Mathf.Abs(worldMatrix.m10) * extents.x +
                Mathf.Abs(worldMatrix.m11) * extents.y +
                Mathf.Abs(worldMatrix.m12) * extents.z,
                Mathf.Abs(worldMatrix.m20) * extents.x +
                Mathf.Abs(worldMatrix.m21) * extents.y +
                Mathf.Abs(worldMatrix.m22) * extents.z);
            return new Bounds(center, worldExtents * 2f);
        }

        private static void ValidateWorldMatrix(
            Transform instance,
            Matrix4x4 expected,
            string context)
        {
            Matrix4x4 actual = instance.localToWorldMatrix;
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(actual[index] - expected[index]) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        $"Dense-city {context} transform parity failed at matrix index {index}.");
                }
            }
        }

        private static CanalBakeResult BakeWaterCanals(
            Transform generatedRoot,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            Rect authoredCoreBounds,
            Vector2 civicCenter,
            CityFootprint cityFootprint,
            TerrainViabilityMap terrainMap,
            RoadBakeResult roadResult,
            float gradeElevation,
            uint seed,
            Rect mapSurfaceBounds,
            DenseCityGenerationTransactionContext generationTransactions,
            ISet<Renderer> protectedOverlapRenderers)
        {
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));
            if (protectedOverlapRenderers == null)
                throw new ArgumentNullException(nameof(protectedOverlapRenderers));
            GameObject[] bankPrefabs = LoadRequiredPrefabs(CanalBankPrefabPaths);
            GameObject bridgePrefab = LoadRequiredPrefab(CanalBridgePrefabPath);
            DenseCityVisualAssetMetadata bridgeMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(bridgePrefab);
            GameObject[] canalTreePrefabs = LoadRequiredPrefabs(Demo2CanalTreePrefabPaths);
            var canalTreeMetadata = new DenseCityVisualAssetMetadata[canalTreePrefabs.Length];
            for (int index = 0; index < canalTreePrefabs.Length; index++)
                canalTreeMetadata[index] = DenseCityVisualAssetMetadataExtractor.Extract(canalTreePrefabs[index]);
            GameObject bushPrefab = LoadRequiredPrefab(MainStreetBushPrefabPath);
            DenseCityVisualAssetMetadata bushMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(bushPrefab);
            GameObject streetLightPrefab = LoadRequiredPrefab(StreetLightPrefabPath);
            DenseCityVisualAssetMetadata streetLightMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(streetLightPrefab);
            Material waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(CanalWaterMaterialPath) ??
                                     throw new InvalidOperationException(
                                         $"Missing Demo2 canal water material {CanalWaterMaterialPath}.");
            Material canalBedMaterial = AssetDatabase.LoadAssetAtPath<Material>(CanalBedMaterialPath) ??
                                        throw new InvalidOperationException(
                                            $"Missing Demo2 canal bed material {CanalBedMaterialPath}.");
            Material canalGreenMaterial = AssetDatabase.LoadAssetAtPath<Material>(CanalGreenMaterialPath) ??
                                          throw new InvalidOperationException(
                                              $"Missing canal green-ground material {CanalGreenMaterialPath}.");
            var bankMetadata = new DenseCityVisualAssetMetadata[bankPrefabs.Length];
            for (int index = 0; index < bankPrefabs.Length; index++)
            {
                bankMetadata[index] = DenseCityVisualAssetMetadataExtractor.Extract(
                    bankPrefabs[index],
                    _ => canalGreenMaterial);
            }
            GameObject canalSurfacePrefab = LoadRequiredPrefab(CanalSurfacePrefabPath);
            DenseCityVisualAssetMetadata canalBedMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(canalSurfacePrefab, _ => canalBedMaterial);
            DenseCityVisualAssetMetadata canalWaterMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(canalSurfacePrefab, _ => waterMaterial);

            var canalObject = new GameObject("DenseCity_WaterCanalsAndParks");
            canalObject.transform.SetParent(generatedRoot, false);
            var waterRootObject = new GameObject("CanalWaterSurfaces");
            waterRootObject.transform.SetParent(canalObject.transform, false);
            var bedRootObject = new GameObject("CanalBeds");
            bedRootObject.transform.SetParent(canalObject.transform, false);
            var bankRootObject = new GameObject("CanalGreenBanks");
            bankRootObject.transform.SetParent(canalObject.transform, false);
            var bridgeRootObject = new GameObject("CanalStreetBridges");
            bridgeRootObject.transform.SetParent(canalObject.transform, false);
            var parkRootObject = new GameObject("CanalPocketParks");
            parkRootObject.transform.SetParent(canalObject.transform, false);
            var detailRootObject = new GameObject("CanalTreesBushesAndLights");
            detailRootObject.transform.SetParent(canalObject.transform, false);

            int maximumColumn = Mathf.FloorToInt(mapWidth / RoadGridSize) - 1;
            int maximumRow = Mathf.FloorToInt(mapDepth / RoadGridSize) - 1;
            var originalRoadCells = new HashSet<Vector2Int>(roadResult.RoadCells);
            List<CanalRoute> routes = BuildCanalRoutes(
                mapOrigin,
                maximumColumn,
                maximumRow,
                authoredCoreBounds,
                civicCenter,
                cityFootprint,
                terrainMap,
                roadResult,
                seed);

            int waterTiles = 0;
            int gameplayWaterExclusions = 0;
            int bridges = 0;
            int greenBanks = 0;
            int gameplayBankTerrains = 0;
            int parkAreas = 0;
            int gameplayParkTerrains = 0;
            int trees = 0;
            int bushes = 0;
            int streetLights = 0;
            int highwayConflicts = 0;
            int highwayUnderpasses = 0;
            int removedCrossingRoads = 0;
            int removedUnbridgedCrossings = 0;
            int removedUnbridgedRoadStubTiles = 0;
            var bridgeCells = new List<Vector2Int>();
            var canalCellUseCounts = new Dictionary<Vector2Int, int>();
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                for (int cellIndex = 0; cellIndex < routes[routeIndex].Cells.Count; cellIndex++)
                {
                    Vector2Int cell = routes[routeIndex].Cells[cellIndex];
                    canalCellUseCounts.TryGetValue(cell, out int useCount);
                    canalCellUseCounts[cell] = useCount + 1;
                }
            }
            var bakedCanalCells = new HashSet<Vector2Int>();
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                CanalRoute route = routes[routeIndex];
                for (int cellIndex = 0; cellIndex < route.Cells.Count; cellIndex++)
                {
                    Vector2Int cell = route.Cells[cellIndex];
                    if (!bakedCanalCells.Add(cell))
                        continue;

                    bool junction = canalCellUseCounts[cell] > 1;
                    Vector2 center = RoadCellWorldCenter(cell, mapOrigin);
                    bool authoredHighwayUnderpass =
                        !cityFootprint.IsAreaClear(
                            center,
                            RoadGridSize * 0.55f,
                            RoadGridSize * 0.55f);
                    // Generated boulevards are part of the city street network and
                    // need a visible bridge for each carriageway. Only the protected
                    // authored highway is allowed to carry the canal below grade.
                    bool highwayUnderpass = authoredHighwayUnderpass;
                    bool streetCrossing = originalRoadCells.Contains(cell);
                    roadResult.RoadCells.Add(cell);
                    float waterWidth = junction
                        ? RoadGridSize * 1.04f
                        : route.Horizontal ? RoadGridSize * 1.04f : RoadGridSize * 0.64f;
                    float waterDepth = junction
                        ? RoadGridSize * 1.04f
                        : route.Horizontal ? RoadGridSize * 0.64f : RoadGridSize * 1.04f;
                    float waterTopElevation = highwayUnderpass
                        ? gradeElevation - 1.25f
                        : gradeElevation + 0.075f;
                    CanalSurfacePlan bedPlan = PlanCanalSurface(
                        canalSurfacePrefab,
                        center,
                        waterTopElevation - 0.04f,
                        waterWidth * 0.96f,
                        waterDepth * 0.92f);
                    CanalSurfacePlan waterPlan = PlanCanalSurface(
                        canalSurfacePrefab,
                        center,
                        waterTopElevation,
                        waterWidth,
                        waterDepth);
                    Vector2Int canalChunk = new(
                        Mathf.FloorToInt((float)cell.x / RoadChunkSize),
                        Mathf.FloorToInt((float)cell.y / RoadChunkSize));
                    GameObject bedSurface = null;
                    GameObject waterSurface = null;
                    try
                    {
                        bool hasGameplayExclusion = TryResolveClippedSurface(
                            center,
                            waterWidth,
                            waterDepth,
                            waterTopElevation,
                            mapSurfaceBounds,
                            out Matrix4x4 exclusionMatrix,
                            out Vector2 exclusionSize);
                        var waterPresentations = new[]
                        {
                            new DenseCityTerrainVisualPresentationInput(
                                "canal-bed-visual",
                                canalWaterMetadata.PrefabAssetGuid,
                                canalWaterMetadata.PrefabLocalId,
                                canalBedMetadata.MaterialAssetGuids,
                                bedPlan.WorldMatrix,
                                false,
                                true,
                                1,
                                highwayUnderpass),
                            new DenseCityTerrainVisualPresentationInput(
                                "canal-water-visual",
                                canalWaterMetadata.PrefabAssetGuid,
                                canalWaterMetadata.PrefabLocalId,
                                canalWaterMetadata.MaterialAssetGuids,
                                waterPlan.WorldMatrix,
                                false,
                                true,
                                2,
                                highwayUnderpass)
                        };
                        bool accepted = hasGameplayExclusion
                            ? generationTransactions.TryPlaceCanalWater(
                                0,
                                sequence => DenseCityCanalWaterRecordFactory.Create(
                                    new DenseCityCanalWaterRecordInput(
                                        DenseCityGeneratorSchema,
                                        unchecked((int)seed),
                                        0,
                                        sequence,
                                        canalWaterMetadata.PrefabAssetGuid,
                                        canalWaterMetadata.PrefabLocalId,
                                        canalBedMetadata.MaterialAssetGuids,
                                        canalWaterMetadata.MaterialAssetGuids,
                                        bedPlan.WorldMatrix,
                                        waterPlan.WorldMatrix,
                                        exclusionSize,
                                        waterTopElevation,
                                        DenseCityBuildingSurfaceLayer,
                                        canalChunk,
                                        exclusionMatrix,
                                        highwayUnderpass)),
                                RealizeWater)
                            : generationTransactions.TryPlacePresentationOnlyTerrainVisuals(
                                0,
                                waterPresentations.Length,
                                sequence => CreatePresentationOnlyTerrainRecords(
                                    waterPresentations,
                                    unchecked((int)seed),
                                    sequence),
                                RealizeWater);
                        if (!accepted)
                            continue;
                        if (hasGameplayExclusion)
                            gameplayWaterExclusions++;

                        bool RealizeWater()
                        {
                            string underpassSuffix = highwayUnderpass ? "_Underpass" : string.Empty;
                            bedSurface = RealizeCanalSurface(
                                bedRootObject.transform,
                                $"CanalBed_{routeIndex:00}_{cellIndex:000}{underpassSuffix}",
                                bedPlan,
                                canalBedMaterial);
                            waterSurface = RealizeCanalSurface(
                                waterRootObject.transform,
                                $"CanalWater_{routeIndex:00}_{cellIndex:000}{underpassSuffix}",
                                waterPlan,
                                waterMaterial);
                            ValidateCanalSurfaceMatrix(bedSurface, bedPlan);
                            ValidateCanalSurfaceMatrix(waterSurface, waterPlan);
                            if (highwayUnderpass)
                            {
                                foreach (Renderer renderer in
                                         bedSurface.GetComponentsInChildren<Renderer>(true))
                                {
                                    protectedOverlapRenderers.Add(renderer);
                                }
                                foreach (Renderer renderer in
                                         waterSurface.GetComponentsInChildren<Renderer>(true))
                                {
                                    protectedOverlapRenderers.Add(renderer);
                                }
                            }
                            return true;
                        }
                    }
                    catch
                    {
                        if (bedSurface != null)
                            UnityEngine.Object.DestroyImmediate(bedSurface);
                        if (waterSurface != null)
                            UnityEngine.Object.DestroyImmediate(waterSurface);
                        throw;
                    }
                    waterTiles++;

                    if (highwayUnderpass)
                    {
                        // Preserve the multilane highway in place and carry the canal below it.
                        highwayUnderpasses++;
                        continue;
                    }

                    if (streetCrossing)
                    {
                        bool hasThroughRoad = TryResolveBridgeThroughRoadAxisAlongX(
                            cell,
                            originalRoadCells,
                            roadResult.BoulevardRoadCells,
                            route.Horizontal,
                            out bool roadAxisAlongWorldX);
                        float roadElevation = RemoveRoadVisualAtCanalCrossing(roadResult, cell);
                        removedCrossingRoads++;
                        bool boulevardCrossing = roadResult.BoulevardRoadCells.Contains(cell);
                        bool bridgeHasClearance = hasThroughRoad &&
                                                  (boulevardCrossing ||
                                                   HasBridgeSpacing(cell, bridgeCells, 14));
                        if (bridgeHasClearance)
                        {
                            CanalBridgePlan bridgePlan = PlanCanalBridge(
                                bridgePrefab,
                                center,
                                roadElevation,
                                roadAxisAlongWorldX);
                            Vector3 roadAxis = roadAxisAlongWorldX ? Vector3.right : Vector3.forward;
                            const float approachLength = RoadGridSize;
                            float approachOffset = RoadGridSize * 1.28f * 0.5f + approachLength * 0.5f;
                            var crossingCenter = new Vector3(center.x, roadElevation, center.y);
                            Vector3 firstApproachCenter = crossingCenter - roadAxis * approachOffset;
                            Vector3 secondApproachCenter = crossingCenter + roadAxis * approachOffset;
                            Matrix4x4 firstApproachMatrix = Matrix4x4.TRS(
                                firstApproachCenter,
                                bridgePlan.Rotation,
                                Vector3.one);
                            Matrix4x4 secondApproachMatrix = Matrix4x4.TRS(
                                secondApproachCenter,
                                bridgePlan.Rotation,
                                Vector3.one);
                            Vector2Int firstApproachCell = cell +
                                (roadAxisAlongWorldX ? Vector2Int.left : Vector2Int.down);
                            Vector2Int secondApproachCell = cell +
                                (roadAxisAlongWorldX ? Vector2Int.right : Vector2Int.up);
                            Vector2Int bridgeChunk = new(
                                Mathf.FloorToInt((float)cell.x / RoadChunkSize),
                                Mathf.FloorToInt((float)cell.y / RoadChunkSize));
                            Vector2Int firstApproachChunk = new(
                                Mathf.FloorToInt((float)firstApproachCell.x / RoadChunkSize),
                                Mathf.FloorToInt((float)firstApproachCell.y / RoadChunkSize));
                            Vector2Int secondApproachChunk = new(
                                Mathf.FloorToInt((float)secondApproachCell.x / RoadChunkSize),
                                Mathf.FloorToInt((float)secondApproachCell.y / RoadChunkSize));
                            GameObject bridge = null;
                            try
                            {
                                bool accepted = generationTransactions.TryPlaceBridge(
                                    0,
                                    sequence => DenseCityInfrastructureRecordFactory.CreateBridgeWithApproaches(
                                        new DenseCityInfrastructureRecordInput(
                                            DenseCityGeneratorSchema,
                                            unchecked((int)seed),
                                            0,
                                            sequence,
                                            "canal-bridge",
                                            DenseCitySurfaceRecordKind.Bridge,
                                            bridgeMetadata.PrefabAssetGuid,
                                            bridgeMetadata.PrefabLocalId,
                                            bridgeMetadata.MaterialAssetGuids,
                                            bridgePlan.WorldMatrix,
                                            new Vector2(RoadGridSize * 0.86f, RoadGridSize * 1.28f),
                                            roadElevation,
                                            (uint)(MapSurfaceMovementMask.AllGroundUnits |
                                                   MapSurfaceMovementMask.AirGrounded),
                                            DenseCityBuildingSurfaceLayer,
                                            bridgeChunk,
                                            true,
                                            true,
                                            2),
                                        new DenseCityBridgeApproachRecordInput(
                                            "canal-bridge-ramp-a",
                                            firstApproachMatrix,
                                            new Vector2(RoadGridSize * 0.86f, approachLength),
                                            roadElevation,
                                            firstApproachChunk),
                                        new DenseCityBridgeApproachRecordInput(
                                            "canal-bridge-ramp-b",
                                            secondApproachMatrix,
                                            new Vector2(RoadGridSize * 0.86f, approachLength),
                                            roadElevation,
                                            secondApproachChunk)),
                                    () =>
                                    {
                                        bridge = RealizeCanalBridge(
                                            bridgeRootObject.transform,
                                            $"CanalBridge_{routeIndex:00}_{cellIndex:000}",
                                            bridgePlan);
                                        return true;
                                    });
                                if (!accepted)
                                    continue;
                            }
                            catch
                            {
                                if (bridge != null)
                                    UnityEngine.Object.DestroyImmediate(bridge);
                                throw;
                            }
                            bridges++;
                            bridgeCells.Add(cell);
                        }
                        else
                        {
                            removedUnbridgedCrossings++;
                            removedUnbridgedRoadStubTiles += RemoveUnbridgedCanalRoadApproaches(
                                roadResult,
                                originalRoadCells,
                                cell,
                                roadAxisAlongWorldX);
                        }
                        continue;
                    }

                    if (junction)
                        continue;

                    Vector2 bankAxis = route.Horizontal ? Vector2.up : Vector2.right;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float bankThickness = ResolveCanalBankThickness(
                            route,
                            routeIndex,
                            cellIndex,
                            side,
                            center,
                            bankAxis,
                            originalRoadCells,
                            mapOrigin);
                        float canalHalfWidth = RoadGridSize * 0.32f;
                        Vector2 bankCenter = center + bankAxis *
                            ((canalHalfWidth + bankThickness * 0.5f - 0.12f) * side);
                        float bankWidth = route.Horizontal ? RoadGridSize * 1.04f : bankThickness;
                        float bankDepth = route.Horizontal ? bankThickness : RoadGridSize * 1.04f;
                        var bankBounds = new Rect(
                            bankCenter.x - bankWidth * 0.5f,
                            bankCenter.y - bankDepth * 0.5f,
                            bankWidth,
                            bankDepth);
                        int bankPatchCount = InstantiateRoundCanalBankCluster(
                            bankPrefabs,
                            bankMetadata,
                            bankRootObject.transform,
                            $"CanalRoundBank_{routeIndex:00}_{cellIndex:000}_{(side < 0 ? "A" : "B")}",
                            bankCenter,
                            gradeElevation + 0.065f,
                            bankWidth,
                            bankDepth,
                            route.Horizontal,
                            detailHashSeed: HashGroundPatch(
                                cell.x,
                                cell.y,
                                0x294d + routeIndex * 43 + side * 13),
                            materialOverride: canalGreenMaterial,
                            seed: seed,
                            chunk: canalChunk,
                            mapSurfaceBounds: mapSurfaceBounds,
                            generationTransactions: generationTransactions);
                        if (bankPatchCount > 0 &&
                            TryResolveClippedSurface(
                                bankCenter,
                                bankWidth,
                                bankDepth,
                                gradeElevation + 0.065f,
                                mapSurfaceBounds,
                                out _,
                                out _))
                        {
                            gameplayBankTerrains++;
                        }
                        ReserveRectAsCells(bankBounds, roadResult.RoadCells, mapOrigin);
                        greenBanks += bankPatchCount;

                        uint detailHash = HashGroundPatch(cell.x, cell.y, 0x6d2f + routeIndex * 31 + side);
                        Rect detailFootprint = new(
                            bankCenter.x - 1.2f,
                            bankCenter.y - 1.2f,
                            2.4f,
                            2.4f);
                        if (OverlapsRoadCell(detailFootprint, originalRoadCells, mapOrigin))
                            continue;

                        if ((cellIndex + routeIndex + side + 3) % 5 == 0)
                        {
                            if (InstantiateTransactionalGroundedDetail(
                                    streetLightPrefab,
                                    streetLightMetadata,
                                    detailRootObject.transform,
                                    $"CanalStreetLight_{routeIndex:00}_{cellIndex:000}_{side}",
                                    bankCenter,
                                    gradeElevation + 0.03f,
                                    route.Horizontal ? 0f : 90f,
                                    1f,
                                    DenseCityPresentationCategory.Infrastructure,
                                    "canal-light-visual",
                                    seed,
                                    generationTransactions))
                            {
                                streetLights++;
                            }
                        }
                        else if ((cellIndex + routeIndex + side + 3) % 3 == 0)
                        {
                            int treePrefabIndex = (int)(detailHash % (uint)canalTreePrefabs.Length);
                            GameObject treePrefab = canalTreePrefabs[treePrefabIndex];
                            if (InstantiateTransactionalGroundedDetail(
                                    treePrefab,
                                    canalTreeMetadata[treePrefabIndex],
                                    detailRootObject.transform,
                                    $"CanalTree_{routeIndex:00}_{cellIndex:000}_{side}",
                                    bankCenter,
                                    gradeElevation + 0.03f,
                                    Hash01(detailHash) * 360f,
                                    Mathf.Lerp(0.78f, 1.08f, Hash01(detailHash ^ 0x45f0a113u)),
                                    DenseCityPresentationCategory.Vegetation,
                                    "canal-tree-visual",
                                    seed,
                                    generationTransactions))
                            {
                                trees++;
                            }
                        }
                        else if ((cellIndex + side + 5) % 2 == 0)
                        {
                            if (InstantiateTransactionalGroundedDetail(
                                    bushPrefab,
                                    bushMetadata,
                                    detailRootObject.transform,
                                    $"CanalBush_{routeIndex:00}_{cellIndex:000}_{side}",
                                    bankCenter,
                                    gradeElevation + 0.03f,
                                    Hash01(detailHash ^ 0x18d7b3a5u) * 360f,
                                    Mathf.Lerp(0.72f, 1.05f, Hash01(detailHash ^ 0xa250c711u)),
                                    DenseCityPresentationCategory.Vegetation,
                                    "canal-bush-visual",
                                    seed,
                                    generationTransactions))
                            {
                                bushes++;
                            }
                        }
                    }
                }

                if (TryAddCanalPocketPark(
                        route,
                        routeIndex,
                        mapOrigin,
                        mapSurfaceBounds,
                        gradeElevation,
                        authoredCoreBounds,
                        cityFootprint,
                        originalRoadCells,
                        roadResult.RoadCells,
                        bankPrefabs,
                        bankMetadata,
                        canalTreePrefabs[(routeIndex + 1) % canalTreePrefabs.Length],
                        canalTreeMetadata[(routeIndex + 1) % canalTreePrefabs.Length],
                        bushPrefab,
                        bushMetadata,
                        streetLightPrefab,
                        streetLightMetadata,
                        canalGreenMaterial,
                        parkRootObject.transform,
                        detailRootObject.transform,
                        seed,
                        generationTransactions,
                        out int parkTrees,
                        out int parkBushes,
                        out int parkLights,
                        out bool parkHasGameplaySurface))
                {
                    parkAreas++;
                    if (parkHasGameplaySurface)
                        gameplayParkTerrains++;
                    trees += parkTrees;
                    bushes += parkBushes;
                    streetLights += parkLights;
                }
            }

            if (removedCrossingRoads != bridges + removedUnbridgedCrossings)
            {
                throw new InvalidOperationException(
                    $"Dense city canal bridge audit failed: bridges={bridges} " +
                    $"unbridgedCrossings={removedUnbridgedCrossings} " +
                    $"removedCrossingRoads={removedCrossingRoads}.");
            }
            Debug.Log(
                $"[DenseCityCanalBridgeAudit] bridges={bridges} " +
                $"removedCrossingRoads={removedCrossingRoads} " +
                $"unbridgedCrossings={removedUnbridgedCrossings} " +
                $"removedUnbridgedRoadStubTiles={removedUnbridgedRoadStubTiles} " +
                $"highwayUnderpasses={highwayUnderpasses} orientation=roadAxis");

            SetStaticRecursively(canalObject);
            return new CanalBakeResult(
                routes.Count,
                waterTiles,
                gameplayWaterExclusions,
                bridges,
                greenBanks,
                gameplayBankTerrains,
                parkAreas,
                gameplayParkTerrains,
                trees,
                bushes,
                streetLights,
                highwayConflicts);
        }

        private static List<CanalRoute> BuildCanalRoutes(
            Vector3 mapOrigin,
            int maximumColumn,
            int maximumRow,
            Rect authoredCoreBounds,
            Vector2 civicCenter,
            CityFootprint cityFootprint,
            TerrainViabilityMap terrainMap,
            RoadBakeResult roadResult,
            uint seed)
        {
            var routes = new List<CanalRoute>();
            float[] preferredFractions = { 0.31f, 0.69f };
            var civicHallAndBazaarExclusion = Rect.MinMaxRect(
                civicCenter.x - 175f,
                civicCenter.y - 120f,
                civicCenter.x + 175f,
                civicCenter.y + 195f);
            var selectedRows = new List<int>();
            var selectedColumns = new List<int>();
            int mountainBuryCells = 12;
            int minimumCanalSpacingCells = 12;
            _ = seed;
            _ = cityFootprint;
            _ = terrainMap;

            for (int specificationIndex = 0; specificationIndex < preferredFractions.Length; specificationIndex++)
            {
                int preferredRow = Mathf.RoundToInt(maximumRow * preferredFractions[specificationIndex]);
                if (TryFindStraightCanalRow(preferredRow, out int selectedRow))
                {
                    var cells = new List<Vector2Int>(maximumColumn + mountainBuryCells * 2 + 1);
                    for (int column = -mountainBuryCells;
                         column <= maximumColumn + mountainBuryCells;
                         column++)
                    {
                        cells.Add(new Vector2Int(column, selectedRow));
                    }

                    routes.Add(new CanalRoute(horizontal: true, cells));
                    selectedRows.Add(selectedRow);
                    Debug.Log(
                        $"[DenseCityCanalRoute] waterway={specificationIndex:00} cells={cells.Count} " +
                        $"row={selectedRow} axis=west-east straight=1 turns=0 mountainBuriedEnds=1");
                    continue;
                }

                int preferredColumn = Mathf.RoundToInt(maximumColumn * preferredFractions[specificationIndex]);
                if (!TryFindStraightCanalColumn(preferredColumn, out int selectedColumn))
                {
                    Debug.LogWarning(
                        $"[DenseCityCanalRoute] skipped preferredRow={preferredRow} " +
                        $"preferredColumn={preferredColumn} " +
                        "because no straight protected-area-clear corridor was available.");
                    continue;
                }

                var verticalCells = new List<Vector2Int>(maximumRow + mountainBuryCells * 2 + 1);
                for (int row = -mountainBuryCells;
                     row <= maximumRow + mountainBuryCells;
                     row++)
                {
                    verticalCells.Add(new Vector2Int(selectedColumn, row));
                }

                routes.Add(new CanalRoute(horizontal: false, verticalCells));
                selectedColumns.Add(selectedColumn);
                Debug.Log(
                    $"[DenseCityCanalRoute] waterway={specificationIndex:00} cells={verticalCells.Count} " +
                    $"column={selectedColumn} axis=north-south straight=1 turns=0 mountainBuriedEnds=1");
            }

            if (routes.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Dense city requires at least two straight mountain-to-mountain canals; " +
                    $"generated={routes.Count}.");
            }
            return routes;

            bool TryFindStraightCanalRow(int preferredRow, out int selectedRow)
            {
                int bestRow = -1;
                int bestBlockerCount = int.MaxValue;
                for (int radius = 0; radius <= maximumRow / 2; radius++)
                {
                    int preferredDirection = ((seed + (uint)radius) & 1u) == 0u ? 1 : -1;
                    int attemptCount = radius == 0 ? 1 : 2;
                    for (int attempt = 0; attempt < attemptCount; attempt++)
                    {
                        int row = preferredRow + radius * (attempt == 0
                            ? preferredDirection
                            : -preferredDirection);
                        if (row < 2 || row > maximumRow - 2 ||
                            IsNearSelectedCanal(row))
                        {
                            continue;
                        }

                        int blockerCount = CountStraightCorridorBlockers(row);
                        if (blockerCount < bestBlockerCount)
                        {
                            bestBlockerCount = blockerCount;
                            bestRow = row;
                        }
                        if (blockerCount > 0)
                            continue;

                        selectedRow = row;
                        return true;
                    }
                }

                Debug.LogWarning(
                    $"[DenseCityCanalRouteAudit] preferredRow={preferredRow} " +
                    $"bestRow={bestRow} blockers={bestBlockerCount}");
                selectedRow = default;
                return false;
            }

            bool IsNearSelectedCanal(int row)
            {
                for (int index = 0; index < selectedRows.Count; index++)
                {
                    if (Mathf.Abs(selectedRows[index] - row) < minimumCanalSpacingCells)
                        return true;
                }
                return false;
            }

            bool TryFindStraightCanalColumn(int preferredColumn, out int selectedColumn)
            {
                int bestColumn = -1;
                int bestBlockerCount = int.MaxValue;
                for (int radius = 0; radius <= maximumColumn / 2; radius++)
                {
                    int preferredDirection = ((seed + (uint)radius) & 1u) == 0u ? 1 : -1;
                    int attemptCount = radius == 0 ? 1 : 2;
                    for (int attempt = 0; attempt < attemptCount; attempt++)
                    {
                        int column = preferredColumn + radius * (attempt == 0
                            ? preferredDirection
                            : -preferredDirection);
                        if (column < 2 || column > maximumColumn - 2 ||
                            IsNearSelectedColumn(column))
                        {
                            continue;
                        }

                        int blockerCount = CountStraightColumnBlockers(column);
                        if (blockerCount < bestBlockerCount)
                        {
                            bestBlockerCount = blockerCount;
                            bestColumn = column;
                        }
                        if (blockerCount > 0 &&
                            !IsContiguousProtectedUnderpassColumn(column, maximumCells: 6))
                            continue;

                        selectedColumn = column;
                        return true;
                    }
                }

                Debug.LogWarning(
                    $"[DenseCityCanalRouteAudit] preferredColumn={preferredColumn} " +
                    $"bestColumn={bestColumn} blockers={bestBlockerCount}");
                LogStraightColumnBlockerBreakdown(bestColumn);
                selectedColumn = default;
                return false;
            }

            bool IsNearSelectedColumn(int column)
            {
                for (int index = 0; index < selectedColumns.Count; index++)
                {
                    if (Mathf.Abs(selectedColumns[index] - column) < minimumCanalSpacingCells)
                        return true;
                }
                return false;
            }

            int CountStraightCorridorBlockers(int row)
            {
                int blockers = 0;
                for (int column = 2; column <= maximumColumn - 2; column++)
                {
                    var cell = new Vector2Int(column, row);
                    Vector2 center = RoadCellWorldCenter(cell, mapOrigin);
                    if (authoredCoreBounds.Contains(center) ||
                        civicHallAndBazaarExclusion.Contains(center) ||
                        !cityFootprint.IsAreaClear(
                            center,
                            RoadGridSize * 0.55f,
                            RoadGridSize * 0.55f) ||
                        IsNearParallelBoulevard(cell, 4, horizontalCanal: true))
                    {
                        blockers++;
                    }
                }

                return blockers;
            }

            bool IsContiguousProtectedUnderpassColumn(int column, int maximumCells)
            {
                int protectedCellCount = 0;
                int previousProtectedRow = -2;
                for (int row = 2; row <= maximumRow - 2; row++)
                {
                    var cell = new Vector2Int(column, row);
                    Vector2 center = RoadCellWorldCenter(cell, mapOrigin);
                    if (authoredCoreBounds.Contains(center) ||
                        civicHallAndBazaarExclusion.Contains(center) ||
                        IsNearParallelBoulevard(cell, 4, horizontalCanal: false))
                    {
                        return false;
                    }

                    if (cityFootprint.IsAreaClear(
                            center,
                            RoadGridSize * 0.55f,
                            RoadGridSize * 0.55f))
                    {
                        continue;
                    }

                    if (protectedCellCount > 0 && row != previousProtectedRow + 1)
                        return false;

                    protectedCellCount++;
                    previousProtectedRow = row;
                    if (protectedCellCount > maximumCells)
                        return false;
                }

                return protectedCellCount > 0;
            }

            int CountStraightColumnBlockers(int column)
            {
                int blockers = 0;
                for (int row = 2; row <= maximumRow - 2; row++)
                {
                    var cell = new Vector2Int(column, row);
                    Vector2 center = RoadCellWorldCenter(cell, mapOrigin);
                    if (authoredCoreBounds.Contains(center) ||
                        civicHallAndBazaarExclusion.Contains(center) ||
                        !cityFootprint.IsAreaClear(
                            center,
                            RoadGridSize * 0.55f,
                            RoadGridSize * 0.55f) ||
                        IsNearParallelBoulevard(cell, 4, horizontalCanal: false))
                    {
                        blockers++;
                    }
                }

                return blockers;
            }

            void LogStraightColumnBlockerBreakdown(int column)
            {
                int authoredCore = 0;
                int civic = 0;
                int protectedGeometry = 0;
                int parallelBoulevard = 0;
                var protectedRows = new List<int>();
                for (int row = 2; row <= maximumRow - 2; row++)
                {
                    var cell = new Vector2Int(column, row);
                    Vector2 center = RoadCellWorldCenter(cell, mapOrigin);
                    if (authoredCoreBounds.Contains(center))
                        authoredCore++;
                    if (civicHallAndBazaarExclusion.Contains(center))
                        civic++;
                    if (!cityFootprint.IsAreaClear(
                            center,
                            RoadGridSize * 0.55f,
                            RoadGridSize * 0.55f))
                    {
                        protectedGeometry++;
                        protectedRows.Add(row);
                    }
                    if (IsNearParallelBoulevard(cell, 4, horizontalCanal: false))
                        parallelBoulevard++;
                }

                Debug.LogWarning(
                    $"[DenseCityCanalRouteAuditBreakdown] column={column} " +
                    $"authoredCore={authoredCore} civic={civic} protected={protectedGeometry} " +
                    $"parallelBoulevard={parallelBoulevard} " +
                    $"protectedRows={string.Join(",", protectedRows)}");
            }

            bool IsNearParallelBoulevard(
                Vector2Int cell,
                int clearanceCells,
                bool horizontalCanal)
            {
                int minimumColumnOffset = horizontalCanal ? -1 : -clearanceCells;
                int maximumColumnOffset = horizontalCanal ? 1 : clearanceCells;
                int minimumRowOffset = horizontalCanal ? -clearanceCells : -1;
                int maximumRowOffset = horizontalCanal ? clearanceCells : 1;
                for (int rowOffset = minimumRowOffset; rowOffset <= maximumRowOffset; rowOffset++)
                {
                    for (int columnOffset = minimumColumnOffset;
                         columnOffset <= maximumColumnOffset;
                         columnOffset++)
                    {
                        var nearbyCell = cell + new Vector2Int(columnOffset, rowOffset);
                        if (!roadResult.BoulevardRoadCells.Contains(nearbyCell))
                            continue;

                        int horizontalConnections =
                            (roadResult.BoulevardRoadCells.Contains(nearbyCell + Vector2Int.left) ? 1 : 0) +
                            (roadResult.BoulevardRoadCells.Contains(nearbyCell + Vector2Int.right) ? 1 : 0);
                        int verticalConnections =
                            (roadResult.BoulevardRoadCells.Contains(nearbyCell + Vector2Int.down) ? 1 : 0) +
                            (roadResult.BoulevardRoadCells.Contains(nearbyCell + Vector2Int.up) ? 1 : 0);
                        bool parallelBoulevard = horizontalCanal
                            ? horizontalConnections > verticalConnections
                            : verticalConnections > horizontalConnections;
                        if (parallelBoulevard)
                            return true;
                    }
                }

                return false;
            }

        }

        private static bool TryAddCanalPocketPark(
            CanalRoute route,
            int routeIndex,
            Vector3 mapOrigin,
            Rect mapSurfaceBounds,
            float gradeElevation,
            Rect authoredCoreBounds,
            CityFootprint cityFootprint,
            HashSet<Vector2Int> originalRoadCells,
            HashSet<Vector2Int> reservedCells,
            GameObject[] roundGroundPrefabs,
            DenseCityVisualAssetMetadata[] roundGroundMetadata,
            GameObject treePrefab,
            DenseCityVisualAssetMetadata treeMetadata,
            GameObject bushPrefab,
            DenseCityVisualAssetMetadata bushMetadata,
            GameObject streetLightPrefab,
            DenseCityVisualAssetMetadata streetLightMetadata,
            Material greenMaterial,
            Transform parkRoot,
            Transform detailRoot,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            out int trees,
            out int bushes,
            out int lights,
            out bool hasGameplaySurface)
        {
            trees = 0;
            bushes = 0;
            lights = 0;
            hasGameplaySurface = false;
            if (route.Cells.Count < 9)
                return false;
            if (roundGroundMetadata == null || roundGroundMetadata.Length != roundGroundPrefabs.Length)
                throw new ArgumentException("Canal park metadata must match the prefab set.", nameof(roundGroundMetadata));
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));

            Vector2 parkCenter = default;
            Rect parkBounds = default;
            bool foundParkSite = false;
            int middle = route.Cells.Count / 2;
            for (int radius = 0; radius <= middle && !foundParkSite; radius++)
            {
                for (int directionIndex = 0; directionIndex < (radius == 0 ? 1 : 2) && !foundParkSite; directionIndex++)
                {
                    int routeCellIndex = middle + radius * (directionIndex == 0 ? 1 : -1);
                    if (routeCellIndex < 0 || routeCellIndex >= route.Cells.Count)
                        continue;
                    Vector2Int routeCell = route.Cells[routeCellIndex];
                    for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                    {
                        int side = ((routeIndex + sideIndex) & 1) == 0 ? 1 : -1;
                        Vector2Int parkCell = routeCell + (route.Horizontal
                            ? new Vector2Int(0, side)
                            : new Vector2Int(side, 0));
                        if (originalRoadCells.Contains(routeCell) || originalRoadCells.Contains(parkCell))
                            continue;

                        parkCenter = RoadCellWorldCenter(parkCell, mapOrigin);
                        parkBounds = new Rect(parkCenter.x - 4.2f, parkCenter.y - 4.2f, 8.4f, 8.4f);
                        if (!authoredCoreBounds.Overlaps(parkBounds) &&
                            cityFootprint.IsAreaClear(parkBounds) &&
                            cityFootprint.Contains(parkCenter, 0.04f) &&
                            !OverlapsRoadCell(parkBounds, originalRoadCells, mapOrigin))
                        {
                            foundParkSite = true;
                            break;
                        }
                    }
                }
            }

            if (!foundParkSite)
                return false;

            Vector2Int parkCellForChunk = new(
                Mathf.FloorToInt((parkCenter.x - mapOrigin.x) / RoadGridSize),
                Mathf.FloorToInt((parkCenter.y - mapOrigin.z) / RoadGridSize));
            Vector2Int parkChunk = new(
                Mathf.FloorToInt((float)parkCellForChunk.x / RoadChunkSize),
                Mathf.FloorToInt((float)parkCellForChunk.y / RoadChunkSize));
            hasGameplaySurface = TryResolveClippedSurface(
                parkCenter,
                parkBounds.width,
                parkBounds.height,
                gradeElevation + 0.065f,
                mapSurfaceBounds,
                out _,
                out _);
            bool accepted = InstantiateOrganicCanalPark(
                roundGroundPrefabs,
                roundGroundMetadata,
                parkRoot,
                $"CanalPocketPark_{routeIndex:00}",
                parkCenter,
                gradeElevation + 0.065f,
                parkBounds.width,
                parkBounds.height,
                HashGroundPatch(routeIndex, route.Cells.Count, 0x4ac3),
                greenMaterial,
                seed,
                parkChunk,
                mapSurfaceBounds,
                generationTransactions);
            if (!accepted)
            {
                hasGameplaySurface = false;
                return false;
            }
            ReserveRectAsCells(parkBounds, reservedCells, mapOrigin);

            Vector2[] treeOffsets =
            {
                new(-2.5f, -2.3f),
                new(2.5f, 2.3f)
            };
            for (int index = 0; index < treeOffsets.Length; index++)
            {
                if (InstantiateTransactionalGroundedDetail(
                        treePrefab,
                        treeMetadata,
                        detailRoot,
                        $"CanalParkTree_{routeIndex:00}_{index:00}",
                        parkCenter + treeOffsets[index],
                        gradeElevation + 0.03f,
                        (routeIndex * 73f + index * 91f) % 360f,
                        0.62f + index % 2 * 0.1f,
                        DenseCityPresentationCategory.Vegetation,
                        "canal-tree-visual",
                        seed,
                        generationTransactions))
                {
                    trees++;
                }
            }

            Vector2[] bushOffsets = { new(-1.8f, 1.7f), new(1.8f, -1.7f) };
            for (int index = 0; index < bushOffsets.Length; index++)
            {
                if (InstantiateTransactionalGroundedDetail(
                        bushPrefab,
                        bushMetadata,
                        detailRoot,
                        $"CanalParkBush_{routeIndex:00}_{index:00}",
                        parkCenter + bushOffsets[index],
                        gradeElevation + 0.03f,
                        index * 180f,
                        0.9f,
                        DenseCityPresentationCategory.Vegetation,
                        "canal-bush-visual",
                        seed,
                        generationTransactions))
                {
                    bushes++;
                }
            }

            Vector2 lightOffset = route.Horizontal ? new Vector2(0f, 3.1f) : new Vector2(3.1f, 0f);
            if (InstantiateTransactionalGroundedDetail(
                    streetLightPrefab,
                    streetLightMetadata,
                    detailRoot,
                    $"CanalParkLight_{routeIndex:00}",
                    parkCenter + lightOffset,
                    gradeElevation + 0.03f,
                    route.Horizontal ? 90f : 0f,
                    1f,
                    DenseCityPresentationCategory.Infrastructure,
                    "canal-light-visual",
                    seed,
                    generationTransactions))
            {
                lights++;
            }

            return true;
        }

        private static void ReserveRectAsCells(
            Rect bounds,
            HashSet<Vector2Int> cells,
            Vector3 mapOrigin)
        {
            int minimumColumn = Mathf.FloorToInt((bounds.xMin - mapOrigin.x) / RoadGridSize);
            int maximumColumn = Mathf.FloorToInt((bounds.xMax - mapOrigin.x) / RoadGridSize);
            int minimumRow = Mathf.FloorToInt((bounds.yMin - mapOrigin.z) / RoadGridSize);
            int maximumRow = Mathf.FloorToInt((bounds.yMax - mapOrigin.z) / RoadGridSize);
            for (int column = minimumColumn; column <= maximumColumn; column++)
            {
                for (int row = minimumRow; row <= maximumRow; row++)
                    cells.Add(new Vector2Int(column, row));
            }
        }

        private static int InstantiateRoundCanalBankCluster(
            GameObject[] roundBankPrefabs,
            DenseCityVisualAssetMetadata[] roundBankMetadata,
            Transform parent,
            string objectName,
            Vector2 center,
            float topElevation,
            float targetWidth,
            float targetDepth,
            bool horizontalCanal,
            uint detailHashSeed,
            Material materialOverride,
            uint seed,
            Vector2Int chunk,
            Rect mapSurfaceBounds,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            if (roundBankPrefabs == null || roundBankPrefabs.Length == 0)
                throw new InvalidOperationException("Round canal bank prefabs are required.");
            if (roundBankMetadata == null || roundBankMetadata.Length != roundBankPrefabs.Length)
                throw new ArgumentException("Canal bank metadata must match the prefab set.", nameof(roundBankMetadata));
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));

            float alongLength = horizontalCanal ? targetWidth : targetDepth;
            float crossLength = horizontalCanal ? targetDepth : targetWidth;
            Vector2 alongAxis = horizontalCanal ? Vector2.right : Vector2.up;
            float asymmetry = Mathf.Lerp(-0.08f, 0.08f, Hash01(detailHashSeed));
            float edgeOffset = alongLength * (0.27f + asymmetry);
            var plans = new List<CanalSurfacePlan>(6);
            var names = new List<string>(6);
            var metadata = new List<DenseCityVisualAssetMetadata>(6);
            int createdPatches = 0;

            CreatePatch("Core", Vector2.zero, alongLength * 0.8f, crossLength, detailHashSeed);
            CreatePatch("Leading", -alongAxis * edgeOffset, alongLength * 0.62f, crossLength * 0.88f,
                detailHashSeed ^ 0x61e5a2b7u);
            CreatePatch("Trailing", alongAxis * edgeOffset, alongLength * 0.58f, crossLength * 0.94f,
                detailHashSeed ^ 0x9d34c117u);
            var presentationInputs = new DenseCityTerrainVisualPresentationInput[plans.Count];
            for (int index = 0; index < plans.Count; index++)
            {
                DenseCityVisualAssetMetadata visualMetadata = metadata[index];
                presentationInputs[index] = new DenseCityTerrainVisualPresentationInput(
                    (index & 1) == 0 ? "canal-bank-base-visual" : "canal-bank-visual",
                    visualMetadata.PrefabAssetGuid,
                    visualMetadata.PrefabLocalId,
                    visualMetadata.MaterialAssetGuids,
                    plans[index].WorldMatrix,
                    false,
                    true,
                    1);
            }

            var realized = new List<GameObject>(plans.Count);
            try
            {
                bool hasGameplaySurface = TryResolveClippedSurface(
                    center,
                    targetWidth,
                    targetDepth,
                    topElevation,
                    mapSurfaceBounds,
                    out Matrix4x4 surfaceMatrix,
                    out Vector2 surfaceSize);
                bool accepted = hasGameplaySurface
                    ? generationTransactions.TryPlaceTerrainVisuals(
                        0,
                        plans.Count,
                        sequence => DenseCityTerrainVisualRecordFactory.Create(
                            new DenseCityTerrainVisualRecordInput(
                                DenseCityGeneratorSchema,
                                unchecked((int)seed),
                                0,
                                sequence,
                                "canal-bank-terrain",
                                surfaceMatrix,
                                surfaceSize,
                                topElevation,
                                (uint)(MapSurfaceMovementMask.AllGroundUnits |
                                       MapSurfaceMovementMask.AirGrounded),
                                DenseCityBuildingSurfaceLayer,
                                chunk,
                                presentationInputs)),
                        RealizeAll)
                    : generationTransactions.TryPlacePresentationOnlyTerrainVisuals(
                        0,
                        plans.Count,
                        sequence => CreatePresentationOnlyTerrainRecords(
                            presentationInputs,
                            unchecked((int)seed),
                            sequence),
                        RealizeAll);
                return accepted ? createdPatches : 0;

                bool RealizeAll()
                {
                    for (int index = 0; index < plans.Count; index++)
                    {
                        GameObject surface = RealizeCanalSurface(
                            parent,
                            names[index],
                            plans[index],
                            materialOverride);
                        realized.Add(surface);
                        ValidateCanalSurfaceMatrix(surface, plans[index]);
                    }
                    return true;
                }
            }
            catch
            {
                for (int index = realized.Count - 1; index >= 0; index--)
                {
                    if (realized[index] != null)
                        UnityEngine.Object.DestroyImmediate(realized[index]);
                }
                throw;
            }

            void CreatePatch(string suffix, Vector2 offset, float patchAlong, float patchCross, uint hash)
            {
                int prefabIndex = (int)(hash % (uint)roundBankPrefabs.Length);
                GameObject prefab = roundBankPrefabs[prefabIndex];
                DenseCityVisualAssetMetadata visualMetadata = roundBankMetadata[prefabIndex];
                float width = horizontalCanal ? patchAlong : patchCross;
                float depth = horizontalCanal ? patchCross : patchAlong;
                plans.Add(PlanCanalSurface(
                    prefab,
                    center + offset,
                    topElevation - 0.006f,
                    width * 1.04f,
                    depth * 1.04f));
                names.Add($"{objectName}_{suffix}_GreenBase");
                metadata.Add(visualMetadata);
                plans.Add(PlanCanalSurface(
                    prefab,
                    center + offset,
                    topElevation + createdPatches * 0.002f,
                    width,
                    depth));
                names.Add($"{objectName}_{suffix}");
                metadata.Add(visualMetadata);
                createdPatches++;
            }
        }

        private static bool TryResolveClippedSurface(
            Vector2 center,
            float width,
            float depth,
            float elevation,
            Rect mapSurfaceBounds,
            out Matrix4x4 surfaceMatrix,
            out Vector2 surfaceSize)
        {
            float minimumX = Mathf.Max(center.x - width * 0.5f, mapSurfaceBounds.xMin);
            float maximumX = Mathf.Min(center.x + width * 0.5f, mapSurfaceBounds.xMax);
            float minimumY = Mathf.Max(center.y - depth * 0.5f, mapSurfaceBounds.yMin);
            float maximumY = Mathf.Min(center.y + depth * 0.5f, mapSurfaceBounds.yMax);
            if (minimumX >= maximumX || minimumY >= maximumY)
            {
                surfaceMatrix = default;
                surfaceSize = default;
                return false;
            }

            // Reconstructing center +/- half-size can round one ULP back outside an exact edge.
            const float BoundaryInset = 0.0001f;
            if (minimumX <= mapSurfaceBounds.xMin)
                minimumX += BoundaryInset;
            if (maximumX >= mapSurfaceBounds.xMax)
                maximumX -= BoundaryInset;
            if (minimumY <= mapSurfaceBounds.yMin)
                minimumY += BoundaryInset;
            if (maximumY >= mapSurfaceBounds.yMax)
                maximumY -= BoundaryInset;
            if (minimumX >= maximumX || minimumY >= maximumY)
            {
                surfaceMatrix = default;
                surfaceSize = default;
                return false;
            }

            surfaceSize = new Vector2(maximumX - minimumX, maximumY - minimumY);
            surfaceMatrix = Matrix4x4.TRS(
                new Vector3(
                    (minimumX + maximumX) * 0.5f,
                    elevation,
                    (minimumY + maximumY) * 0.5f),
                Quaternion.identity,
                Vector3.one);
            return true;
        }

        private static bool IsSurfaceInsideBounds(
            DenseCitySurfaceBakeRecord surface,
            Rect bounds)
        {
            ReadOnlySpan<Vector2> polygon = surface.Polygon.Span;
            for (int index = 0; index < polygon.Length; index++)
            {
                Vector2 point = polygon[index];
                if (point.x < bounds.xMin || point.x > bounds.xMax ||
                    point.y < bounds.yMin || point.y > bounds.yMax)
                {
                    return false;
                }
            }
            return true;
        }

        private static DenseCityPresentationBakeRecord[] CreatePresentationOnlyTerrainRecords(
            IReadOnlyList<DenseCityTerrainVisualPresentationInput> inputs,
            int seed,
            int sequenceStart)
        {
            var presentations = new DenseCityPresentationBakeRecord[inputs.Count];
            for (int index = 0; index < presentations.Length; index++)
            {
                DenseCityTerrainVisualPresentationInput input = inputs[index];
                presentations[index] = DenseCityRenderOnlyPresentationRecordFactory.Create(
                    new DenseCityRenderOnlyPresentationRecordInput(
                        DenseCityGeneratorSchema,
                        seed,
                        0,
                        sequenceStart + index + 1,
                        input.RecordKind,
                        DenseCityPresentationCategory.Infrastructure,
                        input.SourceAssetGuid,
                        input.SourceLocalId,
                        input.MaterialAssetGuids,
                        input.WorldMatrix,
                        input.CastsShadows,
                        input.BatchingEligible,
                        input.LodImportance,
                        input.AllowsProtectedOverlap));
            }
            return presentations;
        }

        private static bool InstantiateOrganicCanalPark(
            GameObject[] roundGroundPrefabs,
            DenseCityVisualAssetMetadata[] roundGroundMetadata,
            Transform parkRoot,
            string objectName,
            Vector2 center,
            float topElevation,
            float targetWidth,
            float targetDepth,
            uint detailHashSeed,
            Material materialOverride,
            uint seed,
            Vector2Int chunk,
            Rect mapSurfaceBounds,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            if (roundGroundPrefabs == null || roundGroundPrefabs.Length == 0)
                throw new InvalidOperationException("Round canal park prefabs are required.");
            if (roundGroundMetadata == null || roundGroundMetadata.Length != roundGroundPrefabs.Length)
                throw new ArgumentException("Canal park metadata must match the prefab set.", nameof(roundGroundMetadata));
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));

            Vector2[] offsets =
            {
                Vector2.zero,
                new(-targetWidth * 0.24f, -targetDepth * 0.1f),
                new(targetWidth * 0.23f, targetDepth * 0.12f),
                new(-targetWidth * 0.06f, targetDepth * 0.25f),
                new(targetWidth * 0.08f, -targetDepth * 0.24f)
            };
            var plans = new List<CanalSurfacePlan>(offsets.Length * 2);
            var names = new List<string>(offsets.Length * 2);
            var metadata = new List<DenseCityVisualAssetMetadata>(offsets.Length * 2);
            for (int index = 0; index < offsets.Length; index++)
            {
                uint hash = detailHashSeed ^ (uint)(0x19d7 + index * 0x2719);
                int prefabIndex = (int)(hash % (uint)roundGroundPrefabs.Length);
                GameObject prefab = roundGroundPrefabs[prefabIndex];
                DenseCityVisualAssetMetadata visualMetadata = roundGroundMetadata[prefabIndex];
                float widthScale = index == 0 ? 0.78f : Mathf.Lerp(0.48f, 0.62f, Hash01(hash));
                float depthScale = index == 0 ? 0.76f : Mathf.Lerp(0.46f, 0.6f, Hash01(hash ^ 0x7f2419adu));
                plans.Add(PlanCanalSurface(
                    prefab,
                    center + offsets[index],
                    topElevation - 0.006f + index * 0.002f,
                    targetWidth * widthScale * 1.04f,
                    targetDepth * depthScale * 1.04f));
                names.Add($"Ground_Round_{index:00}_GreenBase");
                metadata.Add(visualMetadata);
                plans.Add(PlanCanalSurface(
                    prefab,
                    center + offsets[index],
                    topElevation + index * 0.002f,
                    targetWidth * widthScale,
                    targetDepth * depthScale));
                names.Add($"Ground_Round_{index:00}");
                metadata.Add(visualMetadata);
            }
            var presentationInputs = new DenseCityTerrainVisualPresentationInput[plans.Count];
            for (int index = 0; index < plans.Count; index++)
            {
                DenseCityVisualAssetMetadata visualMetadata = metadata[index];
                presentationInputs[index] = new DenseCityTerrainVisualPresentationInput(
                    (index & 1) == 0 ? "canal-park-base-visual" : "canal-park-visual",
                    visualMetadata.PrefabAssetGuid,
                    visualMetadata.PrefabLocalId,
                    visualMetadata.MaterialAssetGuids,
                    plans[index].WorldMatrix,
                    false,
                    true,
                    1);
            }

            GameObject parkObject = null;
            var realized = new List<GameObject>(plans.Count);
            try
            {
                bool hasGameplaySurface = TryResolveClippedSurface(
                    center,
                    targetWidth,
                    targetDepth,
                    topElevation,
                    mapSurfaceBounds,
                    out Matrix4x4 surfaceMatrix,
                    out Vector2 surfaceSize);
                bool accepted = hasGameplaySurface
                    ? generationTransactions.TryPlaceTerrainVisuals(
                        0,
                        plans.Count,
                        sequence => DenseCityTerrainVisualRecordFactory.Create(
                            new DenseCityTerrainVisualRecordInput(
                                DenseCityGeneratorSchema,
                                unchecked((int)seed),
                                0,
                                sequence,
                                "canal-park-terrain",
                                surfaceMatrix,
                                surfaceSize,
                                topElevation,
                                (uint)(MapSurfaceMovementMask.AllGroundUnits |
                                       MapSurfaceMovementMask.AirGrounded),
                                DenseCityBuildingSurfaceLayer,
                                chunk,
                                presentationInputs)),
                        RealizeAll)
                    : generationTransactions.TryPlacePresentationOnlyTerrainVisuals(
                        0,
                        plans.Count,
                        sequence => CreatePresentationOnlyTerrainRecords(
                            presentationInputs,
                            unchecked((int)seed),
                            sequence),
                        RealizeAll);
                return accepted;

                bool RealizeAll()
                {
                    parkObject = new GameObject(objectName);
                    parkObject.transform.SetParent(parkRoot, false);
                    for (int index = 0; index < plans.Count; index++)
                    {
                        GameObject surface = RealizeCanalSurface(
                            parkObject.transform,
                            names[index],
                            plans[index],
                            materialOverride);
                        realized.Add(surface);
                        ValidateCanalSurfaceMatrix(surface, plans[index]);
                    }
                    return true;
                }
            }
            catch
            {
                if (parkObject != null)
                    UnityEngine.Object.DestroyImmediate(parkObject);
                else
                {
                    for (int index = realized.Count - 1; index >= 0; index--)
                    {
                        if (realized[index] != null)
                            UnityEngine.Object.DestroyImmediate(realized[index]);
                    }
                }
                throw;
            }
        }

        private readonly struct CanalSurfacePlan
        {
            internal CanalSurfacePlan(GameObject prefab, Vector3 position, Vector3 scale)
            {
                Prefab = prefab;
                Position = position;
                Scale = scale;
                WorldMatrix = Matrix4x4.TRS(position, Quaternion.identity, scale);
            }

            internal GameObject Prefab { get; }
            internal Vector3 Position { get; }
            internal Vector3 Scale { get; }
            internal Matrix4x4 WorldMatrix { get; }
        }

        private static CanalSurfacePlan PlanCanalSurface(
            GameObject prefab,
            Vector2 center,
            float topElevation,
            float targetWidth,
            float targetDepth)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (!TryGetRendererBounds(prefab, out Bounds sourceBounds))
                throw new InvalidOperationException($"Canal surface prefab '{prefab.name}' has no renderer bounds.");
            if (!float.IsFinite(targetWidth) || !float.IsFinite(targetDepth) ||
                targetWidth <= 0f || targetDepth <= 0f || !float.IsFinite(topElevation))
            {
                throw new ArgumentOutOfRangeException(nameof(targetWidth));
            }

            var scale = new Vector3(
                targetWidth / Mathf.Max(0.01f, sourceBounds.size.x),
                1f,
                targetDepth / Mathf.Max(0.01f, sourceBounds.size.z));
            var position = new Vector3(
                center.x - sourceBounds.center.x * scale.x,
                topElevation - sourceBounds.max.y * scale.y,
                center.y - sourceBounds.center.z * scale.z);
            return new CanalSurfacePlan(prefab, position, scale);
        }

        private static GameObject RealizeCanalSurface(
            Transform parent,
            string objectName,
            CanalSurfacePlan plan,
            Material material)
        {
            GameObject surface =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(plan.Prefab, parent);
            surface.name = objectName;
            surface.transform.SetPositionAndRotation(plan.Position, Quaternion.identity);
            surface.transform.localScale = plan.Scale;
            Renderer[] renderers = surface.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
                renderers[index].sharedMaterial = material;
            DisableColliders(surface);
            return surface;
        }

        private static void ValidateCanalSurfaceMatrix(GameObject surface, CanalSurfacePlan plan)
        {
            Matrix4x4 actual = surface.transform.localToWorldMatrix;
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(actual[index] - plan.WorldMatrix[index]) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        $"Canal surface transform parity failed for '{surface.name}' at matrix index {index}.");
                }
            }
        }

        private readonly struct CanalBridgePlan
        {
            internal CanalBridgePlan(
                GameObject prefab,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Prefab = prefab;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                WorldMatrix = Matrix4x4.TRS(position, rotation, scale);
            }

            internal GameObject Prefab { get; }
            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }
            internal Matrix4x4 WorldMatrix { get; }
        }

        private static CanalBridgePlan PlanCanalBridge(
            GameObject prefab,
            Vector2 center,
            float supportElevation,
            bool roadAxisAlongWorldX)
        {
            if (!TryGetPrefabLocalRendererBounds(prefab.transform, out Bounds sourceBounds))
                throw new InvalidOperationException($"Canal bridge prefab '{prefab.name}' has no renderer bounds.");

            // SM_Env_Bridge_01 is authored with its traversable road deck on local Z.
            Quaternion rotation = roadAxisAlongWorldX
                ? Quaternion.Euler(0f, 90f, 0f)
                : Quaternion.identity;
            float crossAxisScale = RoadGridSize * 0.86f / Mathf.Max(0.01f, sourceBounds.size.x);
            float roadAxisScale = RoadGridSize * 1.28f / Mathf.Max(0.01f, sourceBounds.size.z);
            var scale = new Vector3(
                crossAxisScale,
                Mathf.Min(roadAxisScale, 1.15f),
                roadAxisScale);
            Vector3 transformedCenterOffset = rotation * Vector3.Scale(sourceBounds.center, scale);
            var position = new Vector3(
                center.x - transformedCenterOffset.x,
                supportElevation,
                center.y - transformedCenterOffset.z);
            return new CanalBridgePlan(prefab, position, rotation, scale);
        }

        private static GameObject RealizeCanalBridge(
            Transform parent,
            string objectName,
            CanalBridgePlan plan)
        {
            GameObject instance =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(plan.Prefab, parent);
            try
            {
                instance.name = objectName;
                instance.transform.SetPositionAndRotation(plan.Position, plan.Rotation);
                instance.transform.localScale = plan.Scale;
                Vector3 expectedRoadAxis = plan.Rotation * Vector3.forward;
                if (Mathf.Abs(Vector3.Dot(instance.transform.forward, expectedRoadAxis)) < 0.999f)
                {
                    throw new InvalidOperationException(
                        $"Canal bridge '{objectName}' does not align with its connected road axis.");
                }
                if (!TryGetPrefabLocalRendererBounds(plan.Prefab.transform, out Bounds sourceBounds))
                {
                    throw new InvalidOperationException(
                        $"Canal bridge prefab '{plan.Prefab.name}' lost renderer bounds after scaling.");
                }
                Matrix4x4 actualMatrix = instance.transform.localToWorldMatrix;
                for (int matrixIndex = 0; matrixIndex < 16; matrixIndex++)
                {
                    if (Mathf.Abs(actualMatrix[matrixIndex] - plan.WorldMatrix[matrixIndex]) > 0.0001f)
                        throw new InvalidOperationException($"Canal bridge '{objectName}' transform parity failed.");
                }
                Vector3 plannedCenter = plan.WorldMatrix.MultiplyPoint3x4(sourceBounds.center);
                if (!TryGetRendererBounds(instance, out Bounds placedBounds) ||
                    Vector2.Distance(
                        new Vector2(placedBounds.center.x, placedBounds.center.z),
                        new Vector2(plannedCenter.x, plannedCenter.z)) > 0.01f)
                {
                    throw new InvalidOperationException(
                        $"Canal bridge '{objectName}' visual center does not match its canal crossing center.");
                }
                if (Mathf.Abs(instance.transform.position.y - plan.Position.y) > 0.001f)
                {
                    throw new InvalidOperationException(
                        $"Canal bridge '{objectName}' deck grade does not match its connected road grade.");
                }
                DisableColliders(instance);
                return instance;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(instance);
                throw;
            }
        }

        private static bool TryResolveBridgeThroughRoadAxisAlongX(
            Vector2Int cell,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> boulevardRoadCells,
            bool horizontalCanal,
            out bool roadAxisAlongWorldX)
        {
            bool axisAlongWorldX = !horizontalCanal;
            roadAxisAlongWorldX = axisAlongWorldX;
            Vector2Int negativeDirection = axisAlongWorldX
                ? Vector2Int.left
                : Vector2Int.down;
            Vector2Int positiveDirection = -negativeDirection;

            // Immediate neighbours are insufficient: a short road fragment can
            // exist on both banks and still terminate shortly after the bridge.
            // Require each approach to reconnect to a perpendicular street or a
            // boulevard before permitting a bridge.
            return BranchReconnectsToRoadNetwork(negativeDirection) &&
                   BranchReconnectsToRoadNetwork(positiveDirection);

            bool BranchReconnectsToRoadNetwork(Vector2Int direction)
            {
                Vector2Int cursor = cell + direction;
                for (int distance = 0; distance < 128; distance++, cursor += direction)
                {
                    if (!roadCells.Contains(cursor))
                        return false;
                    if (boulevardRoadCells.Contains(cursor))
                        return true;

                    bool hasPerpendicularConnection = axisAlongWorldX
                        ? roadCells.Contains(cursor + Vector2Int.up) ||
                          roadCells.Contains(cursor + Vector2Int.down)
                        : roadCells.Contains(cursor + Vector2Int.left) ||
                          roadCells.Contains(cursor + Vector2Int.right);
                    if (hasPerpendicularConnection)
                        return true;
                }

                return false;
            }
        }

        private static bool HasBridgeSpacing(
            Vector2Int candidate,
            List<Vector2Int> existingBridgeCells,
            int minimumCellDistance)
        {
            for (int index = 0; index < existingBridgeCells.Count; index++)
            {
                Vector2Int delta = existingBridgeCells[index] - candidate;
                int gridDistance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
                if (gridDistance < minimumCellDistance)
                    return false;
            }

            return true;
        }

        private static float ResolveCanalBankThickness(
            CanalRoute route,
            int routeIndex,
            int cellIndex,
            int side,
            Vector2 canalCenter,
            Vector2 bankAxis,
            HashSet<Vector2Int> originalRoadCells,
            Vector3 mapOrigin)
        {
            int nearestCrossingDistance = route.Cells.Count;
            for (int index = 0; index < route.Cells.Count; index++)
            {
                if (originalRoadCells.Contains(route.Cells[index]))
                    nearestCrossingDistance = Mathf.Min(nearestCrossingDistance, Mathf.Abs(index - cellIndex));
            }

            float wave = 0.5f + 0.5f * Mathf.Sin((cellIndex + routeIndex * 9) * 0.31f);
            uint widthHash = HashGroundPatch(
                route.Cells[cellIndex].x,
                route.Cells[cellIndex].y,
                0x51d3 + routeIndex * 37 + side * 11);
            float irregularity = Hash01(widthHash ^ 0x87c91a3du);
            float widthBlend = Mathf.Clamp01(wave * 0.72f + irregularity * 0.28f);
            float desiredThickness = Mathf.Lerp(
                RoadGridSize * 0.48f,
                RoadGridSize * 1.42f,
                widthBlend);
            if (nearestCrossingDistance <= 1)
                desiredThickness = RoadGridSize * 0.28f;
            else if (nearestCrossingDistance == 2)
                desiredThickness = Mathf.Min(desiredThickness, RoadGridSize * 0.52f);

            const float minimumThickness = RoadGridSize * 0.26f;
            float canalHalfWidth = RoadGridSize * 0.32f;
            for (float thickness = desiredThickness;
                 thickness >= minimumThickness;
                 thickness -= RoadGridSize * 0.12f)
            {
                Vector2 center = canalCenter + bankAxis *
                    ((canalHalfWidth + thickness * 0.5f - 0.12f) * side);
                float width = route.Horizontal ? RoadGridSize * 1.02f : thickness;
                float depth = route.Horizontal ? thickness : RoadGridSize * 1.02f;
                var bounds = new Rect(
                    center.x - width * 0.5f + 0.04f,
                    center.y - depth * 0.5f + 0.04f,
                    width - 0.08f,
                    depth - 0.08f);
                if (!OverlapsRoadCell(bounds, originalRoadCells, mapOrigin))
                    return thickness;
            }

            return minimumThickness;
        }

        private static float RemoveRoadVisualAtCanalCrossing(
            RoadBakeResult roadResult,
            Vector2Int cell)
        {
            if (!roadResult.RoadTileObjects.TryGetValue(cell, out GameObject roadTile) || roadTile == null)
            {
                throw new InvalidOperationException(
                    $"Canal crossing at road cell {cell} has no instantiated road tile to replace.");
            }

            float roadElevation = roadTile.transform.position.y;
            roadResult.RoadTileObjects.Remove(cell);
            UnityEngine.Object.DestroyImmediate(roadTile);
            if (roadResult.RoadGroundPatchObjects.TryGetValue(cell, out GameObject groundPatch))
            {
                roadResult.RoadGroundPatchObjects.Remove(cell);
                if (groundPatch != null)
                    UnityEngine.Object.DestroyImmediate(groundPatch);
            }

            return roadElevation;
        }

        private static int RemoveUnbridgedCanalRoadApproaches(
            RoadBakeResult roadResult,
            HashSet<Vector2Int> originalRoadCells,
            Vector2Int crossingCell,
            bool roadAxisAlongWorldX)
        {
            originalRoadCells.Remove(crossingCell);
            roadResult.DirtRoadCells.Remove(crossingCell);

            Vector2Int negativeDirection = roadAxisAlongWorldX
                ? Vector2Int.left
                : Vector2Int.down;
            Vector2Int positiveDirection = -negativeDirection;
            int removed = 0;
            removed += RemoveApproach(negativeDirection);
            removed += RemoveApproach(positiveDirection);
            return removed;

            int RemoveApproach(Vector2Int direction)
            {
                int removedInDirection = 0;
                Vector2Int cursor = crossingCell + direction;
                for (int distance = 0; distance < 128; distance++, cursor += direction)
                {
                    if (!originalRoadCells.Contains(cursor) ||
                        roadResult.BoulevardRoadCells.Contains(cursor) ||
                        IsConnectedRoadJunction(cursor))
                    {
                        break;
                    }

                    RemoveRoadVisualIfPresent(roadResult, cursor);
                    originalRoadCells.Remove(cursor);
                    roadResult.RoadCells.Remove(cursor);
                    roadResult.DirtRoadCells.Remove(cursor);
                    removedInDirection++;
                }

                return removedInDirection;
            }

            bool IsConnectedRoadJunction(Vector2Int cell)
            {
                if (roadAxisAlongWorldX)
                {
                    return originalRoadCells.Contains(cell + Vector2Int.up) ||
                           originalRoadCells.Contains(cell + Vector2Int.down);
                }

                return originalRoadCells.Contains(cell + Vector2Int.left) ||
                       originalRoadCells.Contains(cell + Vector2Int.right);
            }
        }

        private static void RemoveRoadVisualIfPresent(
            RoadBakeResult roadResult,
            Vector2Int cell)
        {
            if (roadResult.RoadTileObjects.TryGetValue(cell, out GameObject roadTile))
            {
                roadResult.RoadTileObjects.Remove(cell);
                if (roadTile != null)
                    UnityEngine.Object.DestroyImmediate(roadTile);
            }

            if (roadResult.RoadGroundPatchObjects.TryGetValue(cell, out GameObject groundPatch))
            {
                roadResult.RoadGroundPatchObjects.Remove(cell);
                if (groundPatch != null)
                    UnityEngine.Object.DestroyImmediate(groundPatch);
            }
        }

        private static List<BoulevardCorridor> BuildBoulevardCorridors(
            int maximumColumn,
            int maximumRow,
            int centerColumn,
            int centerRow)
        {
            int ClampAnchor(int coordinate, int maximum) =>
                Mathf.Clamp(coordinate, 3, maximum - BoulevardLaneSeparationCells - 3);

            var corridors = new List<BoulevardCorridor>(6)
            {
                new(false, ClampAnchor(maximumColumn / 4, maximumColumn)),
                new(false, ClampAnchor(centerColumn + 15, maximumColumn)),
                new(false, ClampAnchor(maximumColumn * 3 / 4, maximumColumn)),
                new(true, ClampAnchor(maximumRow / 4, maximumRow)),
                new(true, ClampAnchor(centerRow - 12, maximumRow)),
                new(true, ClampAnchor(maximumRow * 3 / 4, maximumRow))
            };

            for (int index = corridors.Count - 1; index >= 0; index--)
            {
                BoulevardCorridor candidate = corridors[index];
                for (int previousIndex = 0; previousIndex < index; previousIndex++)
                {
                    BoulevardCorridor previous = corridors[previousIndex];
                    if (candidate.Horizontal == previous.Horizontal &&
                        Mathf.Abs(candidate.FirstLaneCoordinate - previous.FirstLaneCoordinate) < 8)
                    {
                        corridors.RemoveAt(index);
                        break;
                    }
                }
            }

            return corridors;
        }

        private static List<BoulevardMedianCell> CollectBoulevardMedianCells(
            RoadNetworkCompositionSystemHelper network,
            List<BoulevardCorridor> corridors,
            int maximumColumn,
            int maximumRow,
            Vector3 mapOrigin,
            Rect authoredCoreBounds,
            CityFootprint cityFootprint)
        {
            var medianCells = new List<BoulevardMedianCell>();
            var claimedPairs = new HashSet<(Vector2Int First, Vector2Int Second)>();
            for (int corridorIndex = 0; corridorIndex < corridors.Count; corridorIndex++)
            {
                BoulevardCorridor corridor = corridors[corridorIndex];
                int maximumAlong = corridor.Horizontal ? maximumColumn : maximumRow;
                for (int along = 1; along < maximumAlong; along++)
                {
                    Vector2Int firstLane = corridor.Horizontal
                        ? new Vector2Int(along, corridor.FirstLaneCoordinate)
                        : new Vector2Int(corridor.FirstLaneCoordinate, along);
                    Vector2Int secondLane = corridor.Horizontal
                        ? new Vector2Int(along, corridor.SecondLaneCoordinate)
                        : new Vector2Int(corridor.SecondLaneCoordinate, along);
                    if (!network.StrokeIdsByCell.ContainsKey(firstLane) ||
                        !network.StrokeIdsByCell.ContainsKey(secondLane) ||
                        IsBoulevardPairNearIntersection(
                            network,
                            firstLane,
                            secondLane,
                            corridor.Horizontal))
                    {
                        continue;
                    }

                    Vector2 center =
                        (RoadCellWorldCenter(firstLane, mapOrigin) +
                         RoadCellWorldCenter(secondLane, mapOrigin)) * 0.5f;
                    if (authoredCoreBounds.Contains(center) ||
                        !cityFootprint.Contains(center, 0.02f) ||
                        !claimedPairs.Add((firstLane, secondLane)))
                    {
                        continue;
                    }

                    medianCells.Add(new BoulevardMedianCell(
                        firstLane,
                        secondLane,
                        corridor.Horizontal));
                }
            }

            return medianCells;
        }

        private static bool IsBoulevardPairNearIntersection(
            RoadNetworkCompositionSystemHelper network,
            Vector2Int firstLane,
            Vector2Int secondLane,
            bool horizontal)
        {
            Vector2Int along = horizontal ? Vector2Int.right : Vector2Int.up;
            Vector2Int outsideFirst = horizontal ? Vector2Int.down : Vector2Int.left;
            Vector2Int outsideSecond = horizontal ? Vector2Int.up : Vector2Int.right;
            for (int offset = -1; offset <= 1; offset++)
            {
                if (network.StrokeIdsByCell.ContainsKey(firstLane + along * offset + outsideFirst) ||
                    network.StrokeIdsByCell.ContainsKey(secondLane + along * offset + outsideSecond))
                {
                    return true;
                }
            }

            return false;
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
            bool isAutobahn,
            bool ignoreExclusion = false)
        {
            var cells = new List<Vector2Int>(maximumColumn + 1);
            for (int column = 1; column < maximumColumn; column++)
                cells.Add(new Vector2Int(column, row));
            AddMaskedRoadStroke(
                network,
                cells,
                mapOrigin,
                exclusion,
                footprint,
                terrainMap,
                isAutobahn,
                ignoreExclusion: ignoreExclusion);
        }

        private static void AddVerticalRoad(
            RoadNetworkCompositionSystemHelper network,
            int column,
            int maximumRow,
            Vector3 mapOrigin,
            Rect exclusion,
            CityFootprint footprint,
            TerrainViabilityMap terrainMap,
            bool isAutobahn,
            bool ignoreExclusion = false)
        {
            var cells = new List<Vector2Int>(maximumRow + 1);
            for (int row = 1; row < maximumRow; row++)
                cells.Add(new Vector2Int(column, row));
            AddMaskedRoadStroke(
                network,
                cells,
                mapOrigin,
                exclusion,
                footprint,
                terrainMap,
                isAutobahn,
                ignoreExclusion: ignoreExclusion);
        }

        private static void AddMaskedRoadStroke(
            RoadNetworkCompositionSystemHelper network,
            List<Vector2Int> cells,
            Vector3 mapOrigin,
            Rect exclusion,
            CityFootprint footprint,
            TerrainViabilityMap terrainMap,
            bool isAutobahn,
            bool sparseFringe = false,
            bool ignoreExclusion = false)
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
                if ((ignoreExclusion || !exclusion.Contains(worldCenter)) &&
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

        private static void AddCivicCoreDirtRoads(
            RoadNetworkCompositionSystemHelper network,
            Vector3 mapOrigin,
            Rect authoredCoreBounds,
            Vector2 civicCenter,
            CityFootprint footprint,
            TerrainViabilityMap terrainMap,
            HashSet<Vector2Int> dirtRoadCells,
            HashSet<Vector2Int> civicRoadCells)
        {
            int Column(float worldX) => Mathf.RoundToInt(
                (worldX - mapOrigin.x) / RoadGridSize - 0.5f);
            int Row(float worldZ) => Mathf.RoundToInt(
                (worldZ - mapOrigin.z) / RoadGridSize - 0.5f);

            int centerColumn = Column(civicCenter.x);
            int centerRow = Row(civicCenter.y + 55f);
            int leftColumn = Column(civicCenter.x - 60f);
            int rightColumn = Column(civicCenter.x + 60f);
            int southRow = Row(civicCenter.y - 15f);
            int northRow = Row(civicCenter.y + 125f);
            int westApproachColumn = Column(authoredCoreBounds.xMin - RoadGridSize);
            int eastApproachColumn = Column(authoredCoreBounds.xMax + RoadGridSize);
            int southApproachRow = Row(authoredCoreBounds.yMin - RoadGridSize);
            int northApproachRow = Row(authoredCoreBounds.yMax + RoadGridSize);

            AddCivicRoadStroke(leftColumn, rightColumn, southRow, horizontal: true);
            AddCivicRoadStroke(leftColumn, rightColumn, northRow, horizontal: true);
            AddCivicRoadStroke(southRow, northRow, leftColumn, horizontal: false);
            AddCivicRoadStroke(southRow, northRow, rightColumn, horizontal: false);
            AddCivicRoadStroke(southApproachRow, southRow, centerColumn, horizontal: false);
            AddCivicRoadStroke(northRow, northApproachRow, centerColumn, horizontal: false);
            AddCivicRoadStroke(westApproachColumn, leftColumn, centerRow, horizontal: true);
            AddCivicRoadStroke(rightColumn, eastApproachColumn, centerRow, horizontal: true);

            Debug.Log(
                $"[DenseCityCivicRoadAudit] dirtCells={dirtRoadCells.Count} " +
                $"loopColumns={leftColumn}..{rightColumn} loopRows={southRow}..{northRow}");

            void AddCivicRoadStroke(int start, int end, int fixedCoordinate, bool horizontal)
            {
                var cells = new List<Vector2Int>(Mathf.Abs(end - start) + 1);
                int direction = start <= end ? 1 : -1;
                for (int coordinate = start; ; coordinate += direction)
                {
                    Vector2Int cell = horizontal
                        ? new Vector2Int(coordinate, fixedCoordinate)
                        : new Vector2Int(fixedCoordinate, coordinate);
                    Vector2 worldCenter = RoadCellWorldCenter(cell, mapOrigin);
                    if (footprint.Contains(worldCenter) && terrainMap.CanPlaceRoad(cell))
                    {
                        cells.Add(cell);
                        dirtRoadCells.Add(cell);
                        civicRoadCells.Add(cell);
                    }

                    if (coordinate == end)
                        break;
                }

                CommitRoadStroke(network, cells);
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
            HashSet<Vector2Int> civicRoadCells,
            TerrainViabilityMap terrainMap,
            RoadElevationPlan elevationPlan,
            SurfacePlacementContext surface,
            CityFootprint cityFootprint,
            Rect mapSurfaceBounds,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            HashSet<Vector2Int> protectedAutobahnReplacementCells,
            out Dictionary<Vector2Int, GameObject> roadTileObjects,
            out Dictionary<Vector2Int, GameObject> roadGroundPatchObjects)
        {
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));
            var chunkRoots = new Dictionary<Vector2Int, Transform>();
            var metadataByPrefab = new Dictionary<GameObject, DenseCityVisualAssetMetadata>();
            var groundMetadataByPrefab = new Dictionary<GameObject, DenseCityVisualAssetMetadata>();
            roadTileObjects = new Dictionary<Vector2Int, GameObject>();
            roadGroundPatchObjects = new Dictionary<Vector2Int, GameObject>();
            foreach (KeyValuePair<Vector2Int, RoadNetworkCompositionSystemHelper.RoadTileData> entry in network.RoadTiles)
            {
                Vector2Int cell = entry.Key;
                RoadNetworkCompositionSystemHelper.RoadTileData tile = entry.Value;
                bool useAsphaltRoad = network.AutobahnCells.Contains(cell) ||
                                      network.AutobahnConnectorCells.Contains(cell);
                bool useDirtRoad = dirtRoadCells.Contains(cell) &&
                                   !useAsphaltRoad;
                bool useCivicRoad = civicRoadCells.Contains(cell) &&
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

                Vector3 placement = RoadChunkVisualSystem.GetPlacementPosition(placementContext, cell, variant);
                Vector3 samplePoint = placementContext.GridOrigin + new Vector3(
                    (cell.x + 0.5f) * RoadGridSize,
                    0f,
                    (cell.y + 0.5f) * RoadGridSize);
                float fallbackHeight = (surface?.SampleHeight(samplePoint) ?? placement.y) + 0.025f;
                placement.y = protectedAutobahnReplacementCells.Contains(cell)
                    ? placementContext.BuildPlaneY + 0.08f
                    : elevationPlan.GetElevation(cell, fallbackHeight);
                Matrix4x4 roadWorldMatrix = Matrix4x4.TRS(
                    placement,
                    variant.Rotation,
                    variant.Scale);
                if (!metadataByPrefab.TryGetValue(prefab, out DenseCityVisualAssetMetadata metadata))
                {
                    metadata = DenseCityVisualAssetMetadataExtractor.Extract(prefab);
                    metadataByPrefab.Add(prefab, metadata);
                }
                IReadOnlyList<RoadGridProjectionSystem.RoadFootprintBoundsData> roadFootprints =
                    selectedVariants.VisualData.TryGetValue(
                        tile.Type,
                        out RoadGridProjectionSystem.CombinedRoadVisualData roadVisualData)
                        ? roadVisualData.FootprintBounds
                        : Array.Empty<RoadGridProjectionSystem.RoadFootprintBoundsData>();
                DenseCityRoadShoulderRecordInput[] shoulderInputs =
                    DenseCityRoadShoulderRecordPlanner.Create(
                        roadFootprints,
                        roadWorldMatrix,
                        chunkCoordinate);

                GameObject road = null;
                try
                {
                    DenseCityRoadRecordGroup plannedGroup = CreateRoadGroup(0);
                    bool hasGameplayRoad =
                        !protectedAutobahnReplacementCells.Contains(cell) &&
                        IsSurfaceInsideBounds(
                        plannedGroup.Road,
                        mapSurfaceBounds);
                    for (int index = 0; index < plannedGroup.Shoulders.Length && hasGameplayRoad; index++)
                    {
                        hasGameplayRoad = IsSurfaceInsideBounds(
                            plannedGroup.Shoulders[index],
                            mapSurfaceBounds);
                    }

                    int reservedSequenceCount = checked(2 + shoulderInputs.Length);
                    bool accepted = hasGameplayRoad
                        ? generationTransactions.TryPlaceRoad(
                            0,
                            shoulderInputs.Length,
                            CreateRoadGroup,
                            RealizeRoad)
                        : generationTransactions.TryPlacePresentationOnlyVisuals(
                            0,
                            reservedSequenceCount,
                            1,
                            sequence => new[] { CreateRoadGroup(sequence).Presentation },
                            RealizeRoad);
                    if (!accepted)
                        continue;

                    DenseCityRoadRecordGroup CreateRoadGroup(int sequence) =>
                        DenseCityInfrastructureRecordFactory.CreateRoadWithShoulders(
                            new DenseCityInfrastructureRecordInput(
                                DenseCityGeneratorSchema,
                                unchecked((int)seed),
                                0,
                                sequence,
                                useCivicRoad ? "civic-road" : "road",
                                DenseCitySurfaceRecordKind.Road,
                                metadata.PrefabAssetGuid,
                                metadata.PrefabLocalId,
                                metadata.MaterialAssetGuids,
                                roadWorldMatrix,
                                new Vector2(RoadGridSize, RoadGridSize),
                                placement.y,
                                (uint)(MapSurfaceMovementMask.AllGroundUnits |
                                       MapSurfaceMovementMask.AirGrounded),
                                DenseCityBuildingSurfaceLayer,
                                chunkCoordinate,
                                true,
                                true,
                                2),
                            shoulderInputs);

                    bool RealizeRoad()
                    {
                        road = DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                            prefab,
                            chunkRoot);
                        road.name = $"{prefab.name}_{cell.x}_{cell.y}";
                        road.transform.SetPositionAndRotation(placement, variant.Rotation);
                        road.transform.localScale = variant.Scale;
                        DisableColliders(road);
                        if (protectedAutobahnReplacementCells.Contains(cell))
                        {
                            road.AddComponent<
                                    DenseCityProtectedAutobahnReplacementTileMarker>()
                                .Configure(cell);
                        }
                        Matrix4x4 actualMatrix = road.transform.localToWorldMatrix;
                        for (int matrixIndex = 0; matrixIndex < 16; matrixIndex++)
                        {
                            if (Mathf.Abs(actualMatrix[matrixIndex] - roadWorldMatrix[matrixIndex]) > 0.0001f)
                            {
                                throw new InvalidOperationException(
                                    $"Dense-city road transform parity failed at cell {cell}.");
                            }
                        }
                        return true;
                    }
                }
                catch
                {
                    if (road != null)
                        UnityEngine.Object.DestroyImmediate(road);
                    throw;
                }
                roadTileObjects.Add(cell, road);
                float patchHeight = 0.24f;
                if (terrainMap.TryGetRoadPatch(cell, out SurfacePatchEvaluation roadPatch))
                    patchHeight = Mathf.Clamp(placement.y - roadPatch.MinimumHeight + 0.16f, 0.2f, 0.65f);
                Vector2 patchCenter = new(placement.x, placement.z);
                const float patchClearance = RoadGridSize * 0.78f;
                if (!protectedAutobahnReplacementCells.Contains(cell) &&
                    cityFootprint.IsAreaClear(
                        patchCenter,
                        patchClearance,
                        patchClearance))
                {
                    NaturalGroundPatchPlan patchPlan = PlanNaturalGroundPatch(
                        placement,
                        RoadGridSize * 1.14f,
                        RoadGridSize * 1.14f,
                        patchHeight,
                        HashGroundPatch(cell.x, cell.y, 0x51f2));
                    if (!groundMetadataByPrefab.TryGetValue(
                            patchPlan.Prefab,
                            out DenseCityVisualAssetMetadata groundMetadata))
                    {
                        groundMetadata = patchPlan.Material != null
                            ? DenseCityVisualAssetMetadataExtractor.Extract(
                                patchPlan.Prefab,
                                _ => patchPlan.Material)
                            : DenseCityVisualAssetMetadataExtractor.Extract(patchPlan.Prefab);
                        groundMetadataByPrefab.Add(patchPlan.Prefab, groundMetadata);
                    }

                    GameObject groundPatch = null;
                    try
                    {
                        DenseCityInfrastructureRecordGroup plannedGroup = CreatePatchGroup(0);
                        bool accepted = IsSurfaceInsideBounds(
                                plannedGroup.Surface,
                                mapSurfaceBounds)
                            ? generationTransactions.TryPlaceInfrastructure(
                                0,
                                CreatePatchGroup,
                                RealizePatch)
                            : generationTransactions.TryPlacePresentationOnlyVisuals(
                                0,
                                2,
                                1,
                                sequence => new[] { CreatePatchGroup(sequence).Presentation },
                                RealizePatch);
                        if (accepted)
                            roadGroundPatchObjects.Add(cell, groundPatch);

                        DenseCityInfrastructureRecordGroup CreatePatchGroup(int sequence) =>
                            DenseCityInfrastructureRecordFactory.CreateVisualized(
                                new DenseCityInfrastructureRecordInput(
                                    DenseCityGeneratorSchema,
                                    unchecked((int)seed),
                                    0,
                                    sequence,
                                    useCivicRoad ? "civic-road-terrain-patch" : "road-terrain-patch",
                                    DenseCitySurfaceRecordKind.Terrain,
                                    groundMetadata.PrefabAssetGuid,
                                    groundMetadata.PrefabLocalId,
                                    groundMetadata.MaterialAssetGuids,
                                    patchPlan.WorldMatrix,
                                    new Vector2(RoadGridSize * 1.14f, RoadGridSize * 1.14f),
                                    placement.y - 0.025f,
                                    DenseCityBuildingMovementMask,
                                    DenseCityBuildingSurfaceLayer,
                                    chunkCoordinate,
                                    true,
                                    true,
                                    1));

                        bool RealizePatch()
                        {
                            groundPatch = RealizeNaturalGroundPatch(
                                chunkRoot,
                                $"RoadGroundPatch_{cell.x}_{cell.y}",
                                patchPlan);
                            Matrix4x4 actualMatrix = groundPatch.transform.localToWorldMatrix;
                            for (int matrixIndex = 0; matrixIndex < 16; matrixIndex++)
                            {
                                if (Mathf.Abs(actualMatrix[matrixIndex] - patchPlan.WorldMatrix[matrixIndex]) > 0.0001f)
                                {
                                    throw new InvalidOperationException(
                                        $"Dense-city road terrain-patch transform parity failed at cell {cell}.");
                                }
                            }
                            return true;
                        }
                    }
                    catch
                    {
                        if (groundPatch != null)
                            UnityEngine.Object.DestroyImmediate(groundPatch);
                        throw;
                    }
                }
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
            Rect mapSurfaceBounds,
            List<int> streetColumns,
            List<int> streetRows,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            HashSet<Vector2Int> boulevardRoadCells,
            Rect authoredCoreBounds,
            CityFootprint cityFootprint,
            TerrainViabilityMap terrainMap,
            SurfacePlacementContext surface,
            DenseCityBuildingMaterialLibrary materialLibrary,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            var buildingObject = new GameObject("DenseCity_TightlyPackedUrbanBlocks");
            buildingObject.transform.SetParent(generatedRoot, false);
            var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
            visualSystem.SetRuntimeRoot(buildingObject.transform);
            visualSystem.EnsureCityVisualRoot();
            if (surface != null)
                visualSystem.ConfigureSurface(surface.Surface);
            BuildingPalette palette = BuildPalette(config);
            GridConfig grid = CreateGrid(
                cityOrigin,
                cityWidth,
                cityDepth,
                view.GridCellSize);
            var random = new System.Random(unchecked((int)(config.RandomSeed == 0 ? 26071501u : config.RandomSeed)) ^ 0x1d45ac);
            var placementContext = new BuildingPlacementContext(
                roadCells,
                cityOrigin,
                dirtRoadCells,
                config,
                materialLibrary,
                generationTransactions,
                visualSystem.CityVisualRoot,
                mapSurfaceBounds);
            int buildingCount = 0;
            int parkCount = 0;
            int centralLandmarkCount = 0;
            int snappedFrontageCount = 0;
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
                    placementContext.SetDistrict(blockIndex);
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
                    bool boulevardFrontage = IsBlockAlongBoulevard(
                        block,
                        boulevardRoadCells,
                        cityOrigin);
                    float landmarkChance = ResolveCentralLandmarkChance(
                        zone,
                        Vector2.Distance(blockCenter, coreCenter),
                        boulevardFrontage);
                    UrbanBlockBakeResult blockResult = BuildUrbanBlock(
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
                        random,
                        landmarkChance);
                    buildingCount += blockResult.BuildingCount;
                    centralLandmarkCount += blockResult.CentralLandmarkCount;
                    snappedFrontageCount += blockResult.FrontageBuildingCount;
                }
            }

            SetStaticRecursively(buildingObject);
            Debug.Log(
                $"[DenseCityBuildingPlacementAudit] reserved={placementContext.ReservedCount} " +
                $"centralLandmarks={centralLandmarkCount} " +
                $"snappedFrontages={snappedFrontageCount} buildingOverlaps=0 roadOverlaps=0");
            return new BuildingBakeResult(
                buildingCount,
                parkCount,
                centralLandmarkCount,
                snappedFrontageCount);
        }

        private static bool IsBlockAlongBoulevard(
            Rect block,
            HashSet<Vector2Int> boulevardRoadCells,
            Vector3 mapOrigin)
        {
            if (boulevardRoadCells == null || boulevardRoadCells.Count == 0)
                return false;

            const float frontageProbeDepth = SidewalkBuildingRoadSetback + 1.25f;
            var frontageProbe = Rect.MinMaxRect(
                block.xMin - frontageProbeDepth,
                block.yMin - frontageProbeDepth,
                block.xMax + frontageProbeDepth,
                block.yMax + frontageProbeDepth);
            return OverlapsRoadCell(frontageProbe, boulevardRoadCells, mapOrigin);
        }

        private static float ResolveCentralLandmarkChance(
            DistrictZone zone,
            float distanceFromCivicCenter,
            bool boulevardFrontage)
        {
            if (distanceFromCivicCenter > 520f || zone == DistrictZone.Fringe)
                return 0f;

            if (boulevardFrontage)
            {
                return zone switch
                {
                    DistrictZone.Civic => 0.46f,
                    DistrictZone.InnerCity => 0.32f,
                    _ => distanceFromCivicCenter < 420f ? 0.16f : 0.08f
                };
            }

            return zone switch
            {
                DistrictZone.Civic => 0.2f,
                DistrictZone.InnerCity when distanceFromCivicCenter < 360f => 0.12f,
                _ => 0f
            };
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

        private static UrbanBlockBakeResult BuildUrbanBlock(
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
            System.Random random,
            float centralLandmarkChance)
        {
            int count = 0;
            int centralLandmarks = 0;
            int frontageBuildings = 0;
            bool compactDistrict = zone == DistrictZone.Civic ||
                                   zone == DistrictZone.InnerCity;
            float frontageEdgePadding = zone switch
            {
                DistrictZone.Civic => 0.35f,
                DistrictZone.InnerCity => 0.5f,
                DistrictZone.Residential => 1.25f,
                _ => 2.5f
            };
            UrbanBlockBakeResult frontage = PlaceHorizontalFrontage(visuals, palette, grid, block, true, 0f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random, centralLandmarkChance, frontageEdgePadding);
            count += frontage.BuildingCount;
            centralLandmarks += frontage.CentralLandmarkCount;
            frontageBuildings += frontage.FrontageBuildingCount;
            frontage = PlaceHorizontalFrontage(visuals, palette, grid, block, false, 180f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random, centralLandmarkChance, frontageEdgePadding);
            count += frontage.BuildingCount;
            centralLandmarks += frontage.CentralLandmarkCount;
            frontageBuildings += frontage.FrontageBuildingCount;
            frontage = PlaceVerticalFrontage(visuals, palette, grid, block, true, 90f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random, centralLandmarkChance, frontageEdgePadding);
            count += frontage.BuildingCount;
            centralLandmarks += frontage.CentralLandmarkCount;
            frontageBuildings += frontage.FrontageBuildingCount;
            frontage = PlaceVerticalFrontage(visuals, palette, grid, block, false, 270f, bazaar, terrainMap, placementContext, dirtRoadCells, mapOrigin, random, centralLandmarkChance, frontageEdgePadding);
            count += frontage.BuildingCount;
            centralLandmarks += frontage.CentralLandmarkCount;
            frontageBuildings += frontage.FrontageBuildingCount;

            Rect interior = Rect.MinMaxRect(
                block.xMin + (compactDistrict ? 6.25f : 8f),
                block.yMin + (compactDistrict ? 6.25f : 8f),
                block.xMax - (compactDistrict ? 6.25f : 8f),
                block.yMax - (compactDistrict ? 6.25f : 8f));
            if (interior.width > 8f && interior.height > 8f)
            {
                float spacing = zone switch
                {
                    DistrictZone.Civic => 6.75f,
                    DistrictZone.InnerCity => 7.25f,
                    DistrictZone.Residential => 8.75f,
                    _ => 12f
                };
                double skipChance = zone switch
                {
                    DistrictZone.Civic => 0d,
                    DistrictZone.InnerCity => 0.02d,
                    DistrictZone.Residential => 0.1d,
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

            return new UrbanBlockBakeResult(count, centralLandmarks, frontageBuildings);
        }

        private static UrbanBlockBakeResult PlaceHorizontalFrontage(
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
            System.Random random,
            float centralLandmarkChance,
            float edgePadding)
        {
            int count = 0;
            int centralLandmarks = 0;
            float cursor = block.xMin + edgePadding;
            float limit = block.xMax - edgePadding;
            while (cursor < limit)
            {
                PrefabFootprint info = SelectFrontageBuilding(
                    palette,
                    bazaar,
                    centralLandmarkChance,
                    random,
                    out bool isCentralLandmark);
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
                    SpawnBuilding(
                        visuals,
                        info,
                        new Vector3(center, grid.Origin.y, z),
                        rotation,
                        grid,
                        terrainMap,
                        placementContext,
                        minimumEdge ? FrontageSnapEdge.MinimumZ : FrontageSnapEdge.MaximumZ,
                        minimumEdge ? block.yMin : block.yMax))
                {
                    count++;
                    if (isCentralLandmark)
                        centralLandmarks++;
                }
                cursor += width + (bazaar ? 0.08f : 0.7f);
            }
            return new UrbanBlockBakeResult(count, centralLandmarks, count);
        }

        private static UrbanBlockBakeResult PlaceVerticalFrontage(
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
            System.Random random,
            float centralLandmarkChance,
            float edgePadding)
        {
            int count = 0;
            int centralLandmarks = 0;
            float cursor = block.yMin + edgePadding;
            float limit = block.yMax - edgePadding;
            while (cursor < limit)
            {
                PrefabFootprint info = SelectFrontageBuilding(
                    palette,
                    bazaar,
                    centralLandmarkChance,
                    random,
                    out bool isCentralLandmark);
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
                    SpawnBuilding(
                        visuals,
                        info,
                        new Vector3(x, grid.Origin.y, center),
                        rotation,
                        grid,
                        terrainMap,
                        placementContext,
                        minimumEdge ? FrontageSnapEdge.MinimumX : FrontageSnapEdge.MaximumX,
                        minimumEdge ? block.xMin : block.xMax))
                {
                    count++;
                    if (isCentralLandmark)
                        centralLandmarks++;
                }
                cursor += depth + (bazaar ? 0.08f : 0.7f);
            }
            return new UrbanBlockBakeResult(count, centralLandmarks, count);
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
            BuildingPlacementContext placementContext = null,
            FrontageSnapEdge frontageSnapEdge = FrontageSnapEdge.None,
            float frontageBoundary = 0f)
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

            float foundationHeight = patch.MaximumHeight + 0.035f;
            DenseCityBuildingPlacementPlan plan = DenseCityBuildingPlacementPlanner.Create(
                center,
                rotationDegrees,
                info.Width,
                info.Depth,
                info.Height,
                info.VisualScale,
                foundationHeight,
                grid,
                ToDenseCityFrontageEdge(frontageSnapEdge),
                frontageBoundary,
                placementContext?.PresentationParentLocalToWorldMatrix ?? Matrix4x4.identity,
                placementContext?.PresentationParentWorldToLocalMatrix ?? Matrix4x4.identity);
            Transform realizedWrapper = null;
            if (placementContext == null)
                return Realize();
            bool accepted = placementContext.TryPlaceSemanticBuilding(
                info,
                plan,
                Realize,
                out DenseCityBuildingBakeRecord acceptedBuilding);
            if (accepted && info.PresentationCategory == DenseCityPresentationCategory.GameplayBuildingIntact)
            {
                placementContext.RegisterRealizedBuildingOwner(
                    acceptedBuilding,
                    realizedWrapper,
                    info);
            }
            return accepted;

            bool Realize()
            {
                GameObject wrapper = null;
                try
                {
                    wrapper = visuals.SpawnVisualOnlyPrefab(
                        info.Prefab,
                        plan.OriginCell,
                        plan.FootprintCells,
                        Quaternion.Euler(0f, rotationDegrees, 0f),
                        grid);
                    if (wrapper == null)
                        return false;
                    DenseCityPhysicsComponentStripper.StripInstanceHierarchy(wrapper);

                    wrapper.transform.localPosition = plan.RealizationLocalPosition;
                    wrapper.transform.localRotation = plan.RealizationLocalRotation;
                    wrapper.transform.localScale = Vector3.one * info.VisualScale;

                    if (!TryGetWorldBounds(wrapper.transform, out Bounds actualBounds) ||
                        !DenseCityBuildingPlacementPlanner.MatchesRealization(
                            plan,
                            wrapper.transform.localToWorldMatrix,
                            actualBounds))
                    {
                        float matrixResidual = 0f;
                        Matrix4x4 actualMatrix = wrapper.transform.localToWorldMatrix;
                        for (int row = 0; row < 4; row++)
                        {
                            for (int column = 0; column < 4; column++)
                            {
                                matrixResidual = Mathf.Max(
                                    matrixResidual,
                                    Mathf.Abs(plan.WorldMatrix[row, column] - actualMatrix[row, column]));
                            }
                        }
                        Debug.LogWarning(
                            $"[DenseCityBuildingPlacementParity] rejected={info.Prefab.name} " +
                            $"matrixResidual={matrixResidual:R} " +
                            $"centerResidual=({Mathf.Abs(plan.BlockerBounds.center.x - actualBounds.center.x):R}," +
                            $"{Mathf.Abs(plan.BlockerBounds.center.z - actualBounds.center.z):R}) " +
                            $"footingResidual={Mathf.Abs(plan.BlockerBounds.min.y - actualBounds.min.y):R} " +
                            $"overflowMin=({plan.BlockerBounds.min.x - actualBounds.min.x:R}," +
                            $"{plan.BlockerBounds.min.z - actualBounds.min.z:R}) " +
                            $"overflowMax=({actualBounds.max.x - plan.BlockerBounds.max.x:R}," +
                            $"{actualBounds.max.y - plan.BlockerBounds.max.y:R}," +
                            $"{actualBounds.max.z - plan.BlockerBounds.max.z:R}) " +
                            $"plannedMatrix={plan.WorldMatrix} actualMatrix={actualMatrix} " +
                            $"plannedBounds={plan.BlockerBounds} actualBounds={actualBounds}");
                        UnityEngine.Object.DestroyImmediate(wrapper);
                        return false;
                    }
                    if (placementContext != null && !placementContext.TryReserve(actualBounds))
                    {
                        UnityEngine.Object.DestroyImmediate(wrapper);
                        return false;
                    }

                    if (frontageSnapEdge != FrontageSnapEdge.None)
                        wrapper.name += $"_SidewalkFrontage_{frontageSnapEdge}";

                    CreateBuildingGroundPatch(
                        wrapper,
                        worldWidth,
                        worldDepth,
                        patch,
                        foundationHeight);
                    realizedWrapper = wrapper.transform;
                    return true;
                }
                catch
                {
                    if (wrapper != null)
                        UnityEngine.Object.DestroyImmediate(wrapper);
                    throw;
                }
            }
        }

        private static DenseCityFrontageEdge ToDenseCityFrontageEdge(FrontageSnapEdge edge) => edge switch
        {
            FrontageSnapEdge.None => DenseCityFrontageEdge.None,
            FrontageSnapEdge.MinimumX => DenseCityFrontageEdge.MinimumX,
            FrontageSnapEdge.MaximumX => DenseCityFrontageEdge.MaximumX,
            FrontageSnapEdge.MinimumZ => DenseCityFrontageEdge.MinimumZ,
            FrontageSnapEdge.MaximumZ => DenseCityFrontageEdge.MaximumZ,
            _ => throw new ArgumentOutOfRangeException(nameof(edge))
        };

        private static bool TrySnapBuildingRendererToFrontage(
            Transform building,
            FrontageSnapEdge edge,
            float boundary)
        {
            if (!TryGetWorldBounds(building, out Bounds before))
                return false;

            float currentEdge = edge switch
            {
                FrontageSnapEdge.MinimumX => before.min.x,
                FrontageSnapEdge.MaximumX => before.max.x,
                FrontageSnapEdge.MinimumZ => before.min.z,
                FrontageSnapEdge.MaximumZ => before.max.z,
                _ => boundary
            };
            float delta = boundary - currentEdge;
            Vector3 position = building.position;
            if (edge is FrontageSnapEdge.MinimumX or FrontageSnapEdge.MaximumX)
                position.x += delta;
            else if (edge is FrontageSnapEdge.MinimumZ or FrontageSnapEdge.MaximumZ)
                position.z += delta;
            building.position = position;

            if (!TryGetWorldBounds(building, out Bounds after))
                return false;

            float snappedEdge = edge switch
            {
                FrontageSnapEdge.MinimumX => after.min.x,
                FrontageSnapEdge.MaximumX => after.max.x,
                FrontageSnapEdge.MinimumZ => after.min.z,
                FrontageSnapEdge.MaximumZ => after.max.z,
                _ => boundary
            };
            return Mathf.Abs(snappedEdge - boundary) <= 0.025f;
        }

        private static int AddShopRoofDetails(
            IReadOnlyList<DenseCityRealizedBuildingOwner> buildingOwners,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            GameObject roofCapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoofCap03PrefabPath);
            if (roofCapPrefab == null)
                throw new InvalidOperationException($"Missing roof-cap prefab {RoofCap03PrefabPath}.");
            GameObject shop04Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CleanStandaloneShopPrefabPaths[0]);
            GameObject shop08Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CleanStandaloneShopPrefabPaths[1]);
            if (shop04Prefab == null || shop08Prefab == null)
                throw new InvalidOperationException("Missing explicit dense-city shop attachment owners.");

            int count = AddRoofCapsForShop(
                buildingOwners,
                generationTransactions,
                roofCapPrefab,
                shop04Prefab,
                "SM_Bld_Roof_Cap_03 (3)",
                int.MaxValue);
            count += AddRoofCapsForShop(
                buildingOwners,
                generationTransactions,
                roofCapPrefab,
                shop08Prefab,
                "SM_Bld_Roof_Cap_03 (2)",
                int.MaxValue);
            return count;
        }

        private static int AddRoofCapsForShop(
            IReadOnlyList<DenseCityRealizedBuildingOwner> buildingOwners,
            DenseCityGenerationTransactionContext generationTransactions,
            GameObject roofCapPrefab,
            GameObject shopPrefab,
            string roofCapName,
            int maximumCount)
        {
            var candidates = new List<DenseCityRealizedBuildingOwner>();
            for (int index = 0; index < buildingOwners.Count; index++)
            {
                DenseCityRealizedBuildingOwner candidate = buildingOwners[index];
                if (candidate.SourcePrefab == shopPrefab)
                    candidates.Add(candidate);
            }

            candidates.Sort((left, right) =>
            {
                uint leftHash = HashGroundPatch(
                    Mathf.RoundToInt(left.IntactPresentationRoot.position.x * 10f),
                    Mathf.RoundToInt(left.IntactPresentationRoot.position.z * 10f),
                    0x73c1);
                uint rightHash = HashGroundPatch(
                    Mathf.RoundToInt(right.IntactPresentationRoot.position.x * 10f),
                    Mathf.RoundToInt(right.IntactPresentationRoot.position.z * 10f),
                    0x73c1);
                int hashOrder = leftHash.CompareTo(rightHash);
                return hashOrder != 0
                    ? hashOrder
                    : string.Compare(
                        left.Building.Identity.StableKey,
                        right.Building.Identity.StableKey,
                        StringComparison.Ordinal);
            });

            int count = Mathf.Min(maximumCount, candidates.Count);
            for (int index = 0; index < count; index++)
            {
                DenseCityRealizedBuildingOwner owner = candidates[index];
                Transform shop = owner.IntactPresentationRoot;
                if (!TryGetWorldBounds(shop, out Bounds shopBounds))
                    continue;

                GameObject roofCap =
                    DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                        roofCapPrefab,
                        shop);
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
                try
                {
                    if (!generationTransactions.TryPlaceBuildingAttachment(
                            owner,
                            roofCapPrefab,
                            roofCap.transform,
                            roofCap.transform.localToWorldMatrix,
                            DenseCityPresentationCategory.BuildingAttachmentIntact,
                            () => roofCap.transform.parent == shop))
                    {
                        UnityEngine.Object.DestroyImmediate(roofCap);
                        continue;
                    }
                    DisableColliders(roofCap);
                    SetStaticRecursively(roofCap);
                }
                catch
                {
                    UnityEngine.Object.DestroyImmediate(roofCap);
                    throw;
                }
            }

            return count;
        }

        private readonly struct OpenGroundDetailResult
        {
            internal OpenGroundDetailResult(int visuals, int gameplayTerrains)
            {
                Visuals = visuals;
                GameplayTerrains = gameplayTerrains;
            }

            internal int Visuals { get; }
            internal int GameplayTerrains { get; }
        }

        private static OpenGroundDetailResult AddOpenGroundDetails(
            Transform generatedRoot,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            Rect mapSurfaceBounds,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            List<Rect> buildingFootprints,
            List<Rect> exclusionAreas,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));
            var rootObject = new GameObject("DenseCity_OpenGroundRoundDetails");
            rootObject.transform.SetParent(generatedRoot, false);
            var metadataByPrefab = new Dictionary<GameObject, DenseCityVisualAssetMetadata>();
            var localBoundsByPrefab = new Dictionary<GameObject, Bounds>();
            int count = 0;
            int gameplayTerrains = 0;
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
                    NaturalGroundPatchPlan plan = PlanNaturalGroundPatch(
                        new Vector3(point.x, gradeElevation + visibleRelief + 0.025f, point.y),
                        width,
                        depth,
                        patchHeight,
                        hash,
                        forcePrimaryGroundPrefab: true);
                    if (!localBoundsByPrefab.TryGetValue(plan.Prefab, out Bounds localBounds))
                    {
                        if (!TryGetPrefabLocalRendererBounds(plan.Prefab.transform, out localBounds))
                        {
                            throw new InvalidOperationException(
                                $"Open-ground prefab '{plan.Prefab.name}' has no renderer bounds.");
                        }
                        localBoundsByPrefab.Add(plan.Prefab, localBounds);
                    }
                    Bounds plannedBounds = TransformLocalBounds(localBounds, plan.WorldMatrix);
                    var plannedFootprint = Rect.MinMaxRect(
                        plannedBounds.min.x,
                        plannedBounds.min.z,
                        plannedBounds.max.x,
                        plannedBounds.max.z);
                    if (OverlapsAnyRect(plannedFootprint, exclusionAreas))
                        continue;

                    if (!metadataByPrefab.TryGetValue(plan.Prefab, out DenseCityVisualAssetMetadata metadata))
                    {
                        metadata = DenseCityVisualAssetMetadataExtractor.Extract(
                            plan.Prefab,
                            _ => plan.Material);
                        metadataByPrefab.Add(plan.Prefab, metadata);
                    }
                    var roadCell = new Vector2Int(
                        Mathf.FloorToInt((point.x - mapOrigin.x) / RoadGridSize),
                        Mathf.FloorToInt((point.y - mapOrigin.z) / RoadGridSize));
                    var chunk = new Vector2Int(
                        Mathf.FloorToInt((float)roadCell.x / RoadChunkSize),
                        Mathf.FloorToInt((float)roadCell.y / RoadChunkSize));
                    GameObject patch = null;
                    try
                    {
                        bool hasGameplayTerrain = TryResolveClippedSurface(
                            new Vector2(plannedBounds.center.x, plannedBounds.center.z),
                            plannedBounds.size.x,
                            plannedBounds.size.z,
                            plannedBounds.max.y,
                            mapSurfaceBounds,
                            out Matrix4x4 terrainMatrix,
                            out Vector2 terrainSize);
                        var presentationInputs = new[]
                        {
                            new DenseCityTerrainVisualPresentationInput(
                                "open-ground-visual",
                                metadata.PrefabAssetGuid,
                                metadata.PrefabLocalId,
                                metadata.MaterialAssetGuids,
                                plan.WorldMatrix,
                                true,
                                true,
                                1)
                        };
                        bool accepted = hasGameplayTerrain
                            ? generationTransactions.TryPlaceTerrainVisuals(
                                0,
                                presentationInputs.Length,
                                sequence => DenseCityTerrainVisualRecordFactory.Create(
                                    new DenseCityTerrainVisualRecordInput(
                                        DenseCityGeneratorSchema,
                                        unchecked((int)seed),
                                        0,
                                        sequence,
                                        "open-ground-terrain",
                                        terrainMatrix,
                                        terrainSize,
                                        plannedBounds.max.y,
                                        DenseCityBuildingMovementMask,
                                        DenseCityBuildingSurfaceLayer,
                                        chunk,
                                        presentationInputs)),
                                RealizePatch)
                            : generationTransactions.TryPlacePresentationOnlyTerrainVisuals(
                                0,
                                presentationInputs.Length,
                                sequence => CreatePresentationOnlyTerrainRecords(
                                    presentationInputs,
                                    unchecked((int)seed),
                                    sequence),
                                RealizePatch);
                        if (accepted)
                        {
                            count++;
                            if (hasGameplayTerrain)
                                gameplayTerrains++;
                        }

                        bool RealizePatch()
                        {
                            patch = RealizeNaturalGroundPatch(
                                rootObject.transform,
                                $"SM_Env_Ground_Round_01_Open_{count:0000}",
                                plan);
                            ValidateWorldMatrix(patch.transform, plan.WorldMatrix, "open-ground patch");
                            return true;
                        }
                    }
                    catch
                    {
                        if (patch != null)
                            UnityEngine.Object.DestroyImmediate(patch);
                        throw;
                    }
                }
            }

            SetStaticRecursively(rootObject);
            return new OpenGroundDetailResult(count, gameplayTerrains);
        }

        private static List<Rect> CollectGeneratedBuildingFootprints(
            IReadOnlyList<DenseCityRealizedBuildingOwner> realizedOwners)
        {
            var footprints = new List<Rect>(realizedOwners.Count);
            for (int index = 0; index < realizedOwners.Count; index++)
            {
                Transform wrapper = realizedOwners[index].IntactPresentationRoot;
                if (!TryGetWorldBounds(wrapper, out Bounds bounds))
                    continue;

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
            IReadOnlyList<DenseCityRealizedBuildingOwner> buildingOwners,
            DenseCityGenerationTransactionContext generationTransactions,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            Rect mapSurfaceBounds,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            HashSet<Vector2Int> boulevardRoadCells,
            List<BoulevardMedianCell> boulevardMedianCells,
            float gradeElevation,
            uint seed)
        {
            List<GeneratedBuildingInfo> buildings = CollectGeneratedBuildings(buildingOwners);
            GameObject[] waterTanks = LoadRequiredPrefabs(RooftopWaterTankPrefabPaths);
            GameObject[] rooftopUtilities = LoadRequiredPrefabs(RooftopUtilityPrefabPaths);
            GameObject[] shopWallProps = LoadRequiredPrefabs(ShopWallPropPrefabPaths);
            GameObject[] streetProps = LoadRequiredPrefabs(StreetPropPrefabPaths);
            GameObject[] trees = LoadRequiredPrefabs(DenseTreePrefabPaths);
            GameObject[] rocks = LoadRequiredPrefabs(UrbanRockPrefabPaths);
            DenseCityVisualAssetMetadata[] streetPropMetadata = ExtractVisualMetadata(streetProps);
            DenseCityVisualAssetMetadata[] treeMetadata = ExtractVisualMetadata(trees);
            DenseCityVisualAssetMetadata[] rockMetadata = ExtractVisualMetadata(rocks);
            GameObject courtyardWall = LoadRequiredPrefab(CourtyardWallPrefabPath);
            GameObject courtyardPillar = LoadRequiredPrefab(CourtyardPillarPrefabPath);
            GameObject courtyardWell = LoadRequiredPrefab(CourtyardWellPrefabPath);
            GameObject courtyardBush = LoadRequiredPrefab(CourtyardBushPrefabPath);
            GameObject powerPole = LoadRequiredPrefab(PowerPolePrefabPath);
            GameObject powerLine = LoadRequiredPrefab(PowerLinePrefabPath);
            GameObject streetLight = LoadRequiredPrefab(StreetLightPrefabPath);
            GameObject boulevardMedianTree = LoadRequiredPrefab(BoulevardMedianTreePrefabPath);
            GameObject grass = LoadRequiredPrefab(GrassPrefabPath);
            GameObject mainStreetBush = LoadRequiredPrefab(MainStreetBushPrefabPath);
            DenseCityVisualAssetMetadata boulevardMedianTreeMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(boulevardMedianTree);
            DenseCityVisualAssetMetadata streetLightMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(streetLight);
            DenseCityVisualAssetMetadata grassMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(grass);
            DenseCityVisualAssetMetadata mainStreetBushMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(mainStreetBush);
            DenseCityVisualAssetMetadata courtyardWallMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(courtyardWall);
            DenseCityVisualAssetMetadata courtyardPillarMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(courtyardPillar);
            DenseCityVisualAssetMetadata courtyardWellMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(courtyardWell);
            DenseCityVisualAssetMetadata courtyardBushMetadata =
                DenseCityVisualAssetMetadataExtractor.Extract(courtyardBush);
            if (!TryGetPrefabLocalRendererBounds(courtyardWall.transform, out Bounds courtyardWallLocalBounds))
            {
                throw new InvalidOperationException(
                    $"Courtyard wall prefab '{courtyardWall.name}' has no renderer bounds.");
            }

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
            var boulevardMedianRootObject = new GameObject("DenseCity_LandscapedAsphaltBoulevardMedians");
            boulevardMedianRootObject.transform.SetParent(generatedRoot, false);
            var grassRootObject = new GameObject("DenseCity_FreeGroundGrass");
            grassRootObject.transform.SetParent(generatedRoot, false);
            var mainStreetBushRootObject = new GameObject("DenseCity_MainStreetBushes");
            mainStreetBushRootObject.transform.SetParent(generatedRoot, false);

            int waterTankCount = AddRooftopWaterTanks(
                buildings,
                waterTanks,
                seed,
                generationTransactions);
            int rooftopUtilityCount = AddRooftopUtilityProps(
                buildings,
                rooftopUtilities,
                seed,
                generationTransactions);
            int shopWallPropCount = AddShopWallProps(
                buildings,
                shopWallProps,
                seed,
                generationTransactions);
            CourtyardDetailResult courtyardDetails = AddHouseCourtyards(
                courtyardRootObject.transform,
                buildings,
                courtyardWall,
                courtyardWallMetadata,
                courtyardWallLocalBounds,
                courtyardPillar,
                courtyardPillarMetadata,
                courtyardWell,
                courtyardWellMetadata,
                courtyardBush,
                courtyardBushMetadata,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                mapOrigin,
                mapSurfaceBounds,
                gradeElevation,
                seed,
                generationTransactions);
            var reservedDetailAreas = new List<Rect>(courtyardDetails.ReservedAreas);
            BoulevardMedianDetailResult boulevardMedianDetails = AddBoulevardMedianDetails(
                boulevardMedianRootObject.transform,
                buildings,
                boulevardMedianTree,
                boulevardMedianTreeMetadata,
                streetLight,
                streetLightMetadata,
                boulevardMedianCells,
                mapOrigin,
                authoredCoreBounds,
                roadCells,
                reservedDetailAreas,
                gradeElevation,
                seed,
                generationTransactions);
            var openGroundExclusionAreas = new List<Rect>(courtyardDetails.ReservedAreas);
            openGroundExclusionAreas.AddRange(boulevardMedianDetails.ReservedAreas);
            reservedDetailAreas.AddRange(boulevardMedianDetails.ReservedAreas);
            var landscapingDetails = new LandscapingDetailResult();
            AddMainStreetBushes(
                mainStreetBushRootObject.transform,
                buildings,
                mainStreetBush,
                mainStreetBushMetadata,
                mapOrigin,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                dirtRoadCells,
                boulevardRoadCells,
                reservedDetailAreas,
                landscapingDetails,
                gradeElevation,
                seed,
                generationTransactions);
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
                boulevardRoadCells,
                reservedDetailAreas,
                gradeElevation,
                seed,
                generationTransactions);
            reservedDetailAreas.AddRange(utilityDetails.ReservedAreas);
            StreetLightDetailResult streetLightDetails = AddSidewalkStreetLights(
                streetLightRootObject.transform,
                buildings,
                streetLight,
                streetLightMetadata,
                mapOrigin,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                dirtRoadCells,
                boulevardRoadCells,
                reservedDetailAreas,
                gradeElevation,
                seed,
                generationTransactions);
            reservedDetailAreas.AddRange(streetLightDetails.ReservedAreas);
            int bushReservedAreaCount = landscapingDetails.ReservedAreas.Count;
            AddFreeGroundGrass(
                grassRootObject.transform,
                buildings,
                grass,
                grassMetadata,
                mapOrigin,
                mapWidth,
                mapDepth,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                reservedDetailAreas,
                landscapingDetails,
                gradeElevation,
                seed,
                generationTransactions);
            for (int index = bushReservedAreaCount; index < landscapingDetails.ReservedAreas.Count; index++)
                reservedDetailAreas.Add(landscapingDetails.ReservedAreas[index]);
            int streetPropCount = AddGroundedBuildingProps(
                streetPropRootObject.transform,
                buildings,
                streetProps,
                streetPropMetadata,
                authoredCoreBounds.center,
                roadCells,
                reservedDetailAreas,
                mapOrigin,
                gradeElevation,
                seed,
                generationTransactions);
            (int treeCount, int rockCount, int gameplayRockCount) = AddDenseTreeAndRockClusters(
                treeRootObject.transform,
                rockRootObject.transform,
                buildings,
                trees,
                treeMetadata,
                rocks,
                rockMetadata,
                mapOrigin,
                mapWidth,
                mapDepth,
                mapSurfaceBounds,
                cityFootprint,
                authoredCoreBounds,
                roadCells,
                reservedDetailAreas,
                gradeElevation,
                seed,
                generationTransactions);

            ValidateNoRoadOverlappingDetails(streetPropRootObject.transform, roadCells, mapOrigin);
            ValidateNoRoadOverlappingDetails(rockRootObject.transform, roadCells, mapOrigin);
            ValidateNoRoadOverlappingDetails(grassRootObject.transform, roadCells, mapOrigin);
            ValidateNoRoadOverlappingDetails(mainStreetBushRootObject.transform, roadCells, mapOrigin);
            ValidateNoNaturalDetailOverlaps(
                treeRootObject.transform,
                rockRootObject.transform,
                buildings);
            ValidateBoulevardMedianDetailAnchors(
                boulevardMedianRootObject.transform,
                boulevardMedianCells,
                roadCells,
                mapOrigin);

            SetStaticRecursively(streetPropRootObject);
            SetStaticRecursively(treeRootObject);
            SetStaticRecursively(rockRootObject);
            SetStaticRecursively(courtyardRootObject);
            SetStaticRecursively(utilityRootObject);
            SetStaticRecursively(streetLightRootObject);
            SetStaticRecursively(boulevardMedianRootObject);
            SetStaticRecursively(grassRootObject);
            SetStaticRecursively(mainStreetBushRootObject);
            return new UrbanDetailResult(
                openGroundExclusionAreas,
                waterTankCount,
                rooftopUtilityCount,
                shopWallPropCount,
                streetPropCount,
                treeCount,
                rockCount,
                gameplayRockCount,
                courtyardDetails.Courtyards,
                courtyardDetails.Walls,
                courtyardDetails.WallVisuals,
                courtyardDetails.Pillars,
                courtyardDetails.Wells,
                courtyardDetails.Bushes,
                courtyardDetails.GroundPatchesRemoved,
                utilityDetails.Poles,
                utilityDetails.Lines,
                streetLightDetails.Lights,
                boulevardMedianDetails.Trees,
                boulevardMedianDetails.Lights,
                landscapingDetails.GrassPatches,
                landscapingDetails.MainStreetBushes);
        }

        private sealed class BoulevardMedianDetailResult
        {
            public readonly List<Rect> ReservedAreas = new();
            public int Trees;
            public int Lights;
            public int GroundDetailsRemoved;
        }

        private static BoulevardMedianDetailResult AddBoulevardMedianDetails(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject treePrefab,
            DenseCityVisualAssetMetadata treeMetadata,
            GameObject lightPrefab,
            DenseCityVisualAssetMetadata lightMetadata,
            List<BoulevardMedianCell> medianCells,
            Vector3 mapOrigin,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            var result = new BoulevardMedianDetailResult();
            medianCells.Sort((left, right) =>
            {
                int orientationComparison = left.Horizontal.CompareTo(right.Horizontal);
                if (orientationComparison != 0)
                    return orientationComparison;
                int fixedComparison = (left.Horizontal ? left.FirstLaneCell.y : left.FirstLaneCell.x)
                    .CompareTo(right.Horizontal ? right.FirstLaneCell.y : right.FirstLaneCell.x);
                if (fixedComparison != 0)
                    return fixedComparison;
                return (left.Horizontal ? left.FirstLaneCell.x : left.FirstLaneCell.y)
                    .CompareTo(right.Horizontal ? right.FirstLaneCell.x : right.FirstLaneCell.y);
            });

            for (int index = 0; index < medianCells.Count; index++)
            {
                BoulevardMedianCell median = medianCells[index];
                Vector2 position = median.WorldCenter(mapOrigin);
                result.ReservedAreas.Add(median.Horizontal
                    ? new Rect(
                        position.x - RoadGridSize * 0.48f,
                        position.y - BoulevardCenterStripWidth * 0.5f,
                        RoadGridSize * 0.96f,
                        BoulevardCenterStripWidth)
                    : new Rect(
                        position.x - BoulevardCenterStripWidth * 0.5f,
                        position.y - RoadGridSize * 0.48f,
                        BoulevardCenterStripWidth,
                        RoadGridSize * 0.96f));
                int along = median.Horizontal ? median.FirstLaneCell.x : median.FirstLaneCell.y;
                int fixedCoordinate = median.Horizontal
                    ? median.FirstLaneCell.y
                    : median.FirstLaneCell.x;
                uint hash = HashGroundPatch(
                    along,
                    fixedCoordinate,
                    unchecked((int)seed) ^ (median.Horizontal ? 0x21b7 : 0x73d1));
                bool placeTree = (along + fixedCoordinate) % 2 == 0;
                bool placeLight = !placeTree && Mathf.Abs((along + fixedCoordinate) % 4) == 1;
                GameObject prefab = placeTree ? treePrefab : placeLight ? lightPrefab : null;
                if (prefab == null)
                    continue;

                float rotation = median.Horizontal ? 0f : 90f;
                float scale = placeTree
                    ? Mathf.Lerp(0.68f, 0.82f, Hash01(hash ^ 0xb62c318du))
                    : 1f;
                if (placeTree)
                {
                    if (InstantiateTransactionalGroundedDetail(
                            prefab,
                            treeMetadata,
                            parent,
                            $"{prefab.name}_BoulevardMedianTree_{result.Trees:0000}",
                            position,
                            gradeElevation + 0.025f,
                            rotation,
                            scale,
                            DenseCityPresentationCategory.Vegetation,
                            "boulevard-median-tree-visual",
                            seed,
                            generationTransactions))
                    {
                        result.Trees++;
                    }

                    continue;
                }

                if (!InstantiateTransactionalGroundedDetail(
                        prefab,
                        lightMetadata,
                        parent,
                        $"{prefab.name}_BoulevardMedianLight_{result.Lights:0000}",
                        position,
                        gradeElevation + 0.025f,
                        rotation,
                        scale,
                        DenseCityPresentationCategory.Infrastructure,
                        "boulevard-median-light-visual",
                        seed,
                        generationTransactions))
                {
                    continue;
                }

                result.Lights++;
            }

            result.GroundDetailsRemoved = RemoveOpenGroundDetailsUnderAreas(
                parent.parent,
                result.ReservedAreas);
            Debug.Log(
                $"[DenseCityBoulevardMedian] cells={medianCells.Count} trees={result.Trees} " +
                $"lights={result.Lights} removedGroundDetails={result.GroundDetailsRemoved}");
            return result;
        }

        private sealed class UtilityDetailResult
        {
            public UtilityDetailResult(
                DenseCityVisualAssetMetadata poleMetadata,
                Bounds poleLocalBounds,
                DenseCityVisualAssetMetadata lineMetadata,
                Bounds lineLocalBounds,
                uint seed,
                DenseCityGenerationTransactionContext generationTransactions)
            {
                PoleMetadata = poleMetadata;
                PoleLocalBounds = poleLocalBounds;
                LineMetadata = lineMetadata;
                LineLocalBounds = lineLocalBounds;
                Seed = seed;
                GenerationTransactions = generationTransactions;
            }

            public readonly List<Rect> ReservedAreas = new();
            public DenseCityVisualAssetMetadata PoleMetadata { get; }
            public Bounds PoleLocalBounds { get; }
            public DenseCityVisualAssetMetadata LineMetadata { get; }
            public Bounds LineLocalBounds { get; }
            public uint Seed { get; }
            public DenseCityGenerationTransactionContext GenerationTransactions { get; }
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
            HashSet<Vector2Int> boulevardRoadCells,
            List<Rect> reservedAreas,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));
            if (!TryGetPrefabLocalRendererBounds(polePrefab.transform, out Bounds poleLocalBounds))
                throw new InvalidOperationException($"Power pole prefab '{polePrefab.name}' has no renderer bounds.");
            if (!TryGetPrefabLocalRendererBounds(linePrefab.transform, out Bounds lineLocalBounds))
                throw new InvalidOperationException($"Power line prefab '{linePrefab.name}' has no renderer bounds.");
            var result = new UtilityDetailResult(
                DenseCityVisualAssetMetadataExtractor.Extract(polePrefab),
                poleLocalBounds,
                DenseCityVisualAssetMetadataExtractor.Extract(linePrefab),
                lineLocalBounds,
                seed,
                generationTransactions);
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
                    boulevardRoadCells,
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
                    boulevardRoadCells,
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
            HashSet<Vector2Int> boulevardRoadCells,
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
                bool hasRoad = along < maximumAlongCoordinate &&
                               roadCells.Contains(cell) &&
                               !boulevardRoadCells.Contains(cell);
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
                        result,
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
                        polePoint,
                        result))
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
            UtilityDetailResult result,
            out UtilityPolePoint polePoint,
            int side)
        {
            GroundedDetailPlan plan = PlanGroundedDetail(
                prefab,
                position,
                supportHeight,
                rotation,
                1f);
            Bounds plannedBounds = TransformLocalBounds(result.PoleLocalBounds, plan.WorldMatrix);
            GameObject pole = null;
            try
            {
                bool accepted = result.GenerationTransactions.TryPlaceRenderOnlyPresentation(
                    0,
                    sequence => DenseCityRenderOnlyPresentationRecordFactory.Create(
                        new DenseCityRenderOnlyPresentationRecordInput(
                            DenseCityGeneratorSchema,
                            unchecked((int)result.Seed),
                            0,
                            sequence,
                            "power-pole-visual",
                            DenseCityPresentationCategory.Infrastructure,
                            result.PoleMetadata.PrefabAssetGuid,
                            result.PoleMetadata.PrefabLocalId,
                            result.PoleMetadata.MaterialAssetGuids,
                            plan.WorldMatrix,
                            true,
                            true,
                            1)),
                    () =>
                    {
                        pole = DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                            prefab,
                            parent);
                        if (pole == null)
                            return false;
                        pole.name = objectName;
                        pole.transform.SetPositionAndRotation(plan.Position, plan.Rotation);
                        pole.transform.localScale = plan.Scale;
                        ValidateWorldMatrix(pole.transform, plan.WorldMatrix, "power pole");
                        DisableColliders(pole);
                        return true;
                    });
                if (!accepted)
                {
                    polePoint = default;
                    return false;
                }

                polePoint = new UtilityPolePoint(plan.Position, plannedBounds.max.y - 0.12f, side);
                return true;
            }
            catch
            {
                if (pole != null)
                    UnityEngine.Object.DestroyImmediate(pole);
                throw;
            }
        }

        private static bool InstantiatePowerLineSpan(
            GameObject prefab,
            Transform parent,
            string objectName,
            UtilityPolePoint start,
            UtilityPolePoint end,
            UtilityDetailResult result)
        {
            Vector3 direction = end.Position - start.Position;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance < 0.1f)
                return false;

            float sourceLength = Mathf.Max(0.1f, result.LineLocalBounds.size.z);
            var position = new Vector3(
                start.Position.x,
                (start.WireHeight + end.WireHeight) * 0.5f,
                start.Position.z);
            Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var scale = new Vector3(1f, 1f, distance / sourceLength);
            Matrix4x4 worldMatrix = Matrix4x4.TRS(position, rotation, scale);
            GameObject line = null;
            try
            {
                return result.GenerationTransactions.TryPlaceRenderOnlyPresentation(
                    0,
                    sequence => DenseCityRenderOnlyPresentationRecordFactory.Create(
                        new DenseCityRenderOnlyPresentationRecordInput(
                            DenseCityGeneratorSchema,
                            unchecked((int)result.Seed),
                            0,
                            sequence,
                            "power-line-visual",
                            DenseCityPresentationCategory.Infrastructure,
                            result.LineMetadata.PrefabAssetGuid,
                            result.LineMetadata.PrefabLocalId,
                            result.LineMetadata.MaterialAssetGuids,
                            worldMatrix,
                            true,
                            true,
                            1)),
                    () =>
                    {
                        line = DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                            prefab,
                            parent);
                        if (line == null)
                            return false;
                        line.name = objectName;
                        line.transform.SetPositionAndRotation(position, rotation);
                        line.transform.localScale = scale;
                        ValidateWorldMatrix(line.transform, worldMatrix, "power line");
                        DisableColliders(line);
                        return true;
                    });
            }
            catch
            {
                if (line != null)
                    UnityEngine.Object.DestroyImmediate(line);
                throw;
            }
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
            DenseCityVisualAssetMetadata lightMetadata,
            Vector3 mapOrigin,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            HashSet<Vector2Int> boulevardRoadCells,
            List<Rect> reservedAreas,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
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
                if (dirtRoadCells.Contains(cell) || boulevardRoadCells.Contains(cell))
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
                if (!InstantiateTransactionalGroundedDetail(
                        lightPrefab,
                        lightMetadata,
                        parent,
                        $"{lightPrefab.name}_Sidewalk_{result.Lights:0000}",
                        lightPosition,
                        gradeElevation + 0.025f,
                        rotation,
                        1f,
                        DenseCityPresentationCategory.Infrastructure,
                        "sidewalk-street-light-visual",
                        seed,
                        generationTransactions))
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
            DenseCityVisualAssetMetadata metadata,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            LandscapingDetailResult result,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
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

                    if (!TryInstantiateTransactionalGroundedFreeDetail(
                            prefab,
                            metadata,
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
                            DenseCityPresentationCategory.Vegetation,
                            "free-ground-grass-visual",
                            seed,
                            generationTransactions,
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
            DenseCityVisualAssetMetadata metadata,
            Vector3 mapOrigin,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            HashSet<Vector2Int> dirtRoadCells,
            HashSet<Vector2Int> boulevardRoadCells,
            List<Rect> reservedAreas,
            LandscapingDetailResult result,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
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
                if (dirtRoadCells.Contains(cell) || boulevardRoadCells.Contains(cell))
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
                    if (!TryInstantiateTransactionalGroundedFreeDetail(
                            prefab,
                            metadata,
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
                            DenseCityPresentationCategory.Vegetation,
                            "main-street-bush-visual",
                            seed,
                            generationTransactions,
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
            public int WallVisuals;
            public int Pillars;
            public int Wells;
            public int Bushes;
            public int GroundPatchesRemoved;
        }

        private static CourtyardDetailResult AddHouseCourtyards(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject wallPrefab,
            DenseCityVisualAssetMetadata wallMetadata,
            Bounds wallLocalBounds,
            GameObject pillarPrefab,
            DenseCityVisualAssetMetadata pillarMetadata,
            GameObject wellPrefab,
            DenseCityVisualAssetMetadata wellMetadata,
            GameObject bushPrefab,
            DenseCityVisualAssetMetadata bushMetadata,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin,
            Rect mapSurfaceBounds,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
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
                        wallMetadata,
                        wallLocalBounds,
                        pillarPrefab,
                        pillarMetadata,
                        parent,
                        yard,
                        side,
                        gradeElevation,
                        wallScale,
                        courtyardIndex,
                        mapOrigin,
                        mapSurfaceBounds,
                        seed,
                        generationTransactions,
                        ref result.Walls,
                        ref result.WallVisuals,
                        ref result.Pillars);

                    Vector2 interiorCenter = yard.center;
                    if (Hash01(sideHash ^ 0x941ce2b7u) < 0.58f &&
                        InstantiateTransactionalGroundedDetail(
                            wellPrefab,
                            wellMetadata,
                            parent,
                            $"{wellPrefab.name}_Courtyard_{courtyardIndex:0000}",
                            interiorCenter,
                            gradeElevation + 0.025f,
                            Hash01(sideHash ^ 0xe61b89a3u) * 360f,
                            Mathf.Lerp(0.78f, 1.02f, Hash01(sideHash ^ 0x1f35ca9du)),
                            DenseCityPresentationCategory.Prop,
                            "courtyard-well-visual",
                            seed,
                            generationTransactions))
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

                        if (InstantiateTransactionalGroundedDetail(
                                bushPrefab,
                                bushMetadata,
                                parent,
                                $"{bushPrefab.name}_Courtyard_{courtyardIndex:0000}_{bushIndex:00}",
                                bushPosition,
                                gradeElevation + 0.02f,
                                Hash01(bushHash ^ 0x4f7812c9u) * 360f,
                                Mathf.Lerp(0.78f, 1.16f, Hash01(bushHash ^ 0xc7a4e591u)),
                                DenseCityPresentationCategory.Vegetation,
                                "courtyard-bush-visual",
                                seed,
                                generationTransactions))
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

        private static int RemoveOpenGroundDetailsUnderAreas(
            Transform generatedRoot,
            List<Rect> areas)
        {
            Transform openGroundRoot = FindDirectChild(generatedRoot, "DenseCity_OpenGroundRoundDetails");
            if (openGroundRoot == null || areas == null || areas.Count == 0)
                return 0;

            int removed = 0;
            for (int childIndex = openGroundRoot.childCount - 1; childIndex >= 0; childIndex--)
            {
                Transform child = openGroundRoot.GetChild(childIndex);
                if (child == null || !TryGetWorldBounds(child, out Bounds bounds))
                    continue;

                var footprint = Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z);
                if (!OverlapsAnyRect(footprint, areas))
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
            DenseCityVisualAssetMetadata wallMetadata,
            Bounds wallLocalBounds,
            GameObject pillarPrefab,
            DenseCityVisualAssetMetadata pillarMetadata,
            Transform parent,
            Rect yard,
            int buildingSide,
            float gradeElevation,
            float wallScale,
            int courtyardIndex,
            Vector3 mapOrigin,
            Rect mapSurfaceBounds,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            ref int wallCount,
            ref int wallVisualCount,
            ref int pillarCount)
        {
            const float edgeInset = 0.25f;
            const float cornerClearance = 0.5f;
            if (buildingSide < 2)
            {
                float outerX = buildingSide == 0 ? yard.xMin + edgeInset : yard.xMax - edgeInset;
                AddCourtyardEntrance(
                    wallPrefab,
                    wallMetadata,
                    wallLocalBounds,
                    pillarPrefab,
                    pillarMetadata,
                    parent,
                    courtyardIndex,
                    new Vector2(outerX, yard.center.y),
                    90f,
                    yard.height - cornerClearance * 2f,
                    gradeElevation,
                    wallScale,
                    mapOrigin,
                    mapSurfaceBounds,
                    seed,
                    generationTransactions,
                    ref wallCount,
                    ref wallVisualCount,
                    ref pillarCount);
                float sideCenterX = yard.center.x + (buildingSide == 0 ? cornerClearance * 0.5f : -cornerClearance * 0.5f);
                AddCourtyardWall(wallPrefab, wallMetadata, wallLocalBounds, parent, courtyardIndex, "North", new Vector2(sideCenterX, yard.yMax - edgeInset), 0f, yard.width - cornerClearance, gradeElevation, wallScale, mapOrigin, mapSurfaceBounds, seed, generationTransactions, ref wallCount, ref wallVisualCount);
                AddCourtyardWall(wallPrefab, wallMetadata, wallLocalBounds, parent, courtyardIndex, "South", new Vector2(sideCenterX, yard.yMin + edgeInset), 0f, yard.width - cornerClearance, gradeElevation, wallScale, mapOrigin, mapSurfaceBounds, seed, generationTransactions, ref wallCount, ref wallVisualCount);
            }
            else
            {
                float outerZ = buildingSide == 2 ? yard.yMin + edgeInset : yard.yMax - edgeInset;
                AddCourtyardEntrance(
                    wallPrefab,
                    wallMetadata,
                    wallLocalBounds,
                    pillarPrefab,
                    pillarMetadata,
                    parent,
                    courtyardIndex,
                    new Vector2(yard.center.x, outerZ),
                    0f,
                    yard.width - cornerClearance * 2f,
                    gradeElevation,
                    wallScale,
                    mapOrigin,
                    mapSurfaceBounds,
                    seed,
                    generationTransactions,
                    ref wallCount,
                    ref wallVisualCount,
                    ref pillarCount);
                float sideCenterZ = yard.center.y + (buildingSide == 2 ? cornerClearance * 0.5f : -cornerClearance * 0.5f);
                AddCourtyardWall(wallPrefab, wallMetadata, wallLocalBounds, parent, courtyardIndex, "West", new Vector2(yard.xMin + edgeInset, sideCenterZ), 90f, yard.height - cornerClearance, gradeElevation, wallScale, mapOrigin, mapSurfaceBounds, seed, generationTransactions, ref wallCount, ref wallVisualCount);
                AddCourtyardWall(wallPrefab, wallMetadata, wallLocalBounds, parent, courtyardIndex, "East", new Vector2(yard.xMax - edgeInset, sideCenterZ), 90f, yard.height - cornerClearance, gradeElevation, wallScale, mapOrigin, mapSurfaceBounds, seed, generationTransactions, ref wallCount, ref wallVisualCount);
            }
        }

        private static void AddCourtyardEntrance(
            GameObject wallPrefab,
            DenseCityVisualAssetMetadata wallMetadata,
            Bounds wallLocalBounds,
            GameObject pillarPrefab,
            DenseCityVisualAssetMetadata pillarMetadata,
            Transform parent,
            int courtyardIndex,
            Vector2 center,
            float wallRotation,
            float totalLength,
            float gradeElevation,
            float wallScale,
            Vector3 mapOrigin,
            Rect mapSurfaceBounds,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            ref int wallCount,
            ref int wallVisualCount,
            ref int pillarCount)
        {
            float gateWidth = Mathf.Clamp(totalLength * 0.3f, 1.8f, 2.4f);
            float segmentLength = Mathf.Max(1.25f, (totalLength - gateWidth) * 0.5f);
            float centerOffset = gateWidth * 0.5f + segmentLength * 0.5f;
            Vector2 axis = wallRotation % 180f == 0f ? Vector2.right : Vector2.up;

            AddCourtyardWall(
                wallPrefab,
                wallMetadata,
                wallLocalBounds,
                parent,
                courtyardIndex,
                "EntranceLeft",
                center - axis * centerOffset,
                wallRotation,
                segmentLength,
                gradeElevation,
                wallScale,
                mapOrigin,
                mapSurfaceBounds,
                seed,
                generationTransactions,
                ref wallCount,
                ref wallVisualCount);
            AddCourtyardWall(
                wallPrefab,
                wallMetadata,
                wallLocalBounds,
                parent,
                courtyardIndex,
                "EntranceRight",
                center + axis * centerOffset,
                wallRotation,
                segmentLength,
                gradeElevation,
                wallScale,
                mapOrigin,
                mapSurfaceBounds,
                seed,
                generationTransactions,
                ref wallCount,
                ref wallVisualCount);

            AddCourtyardPillar(
                pillarPrefab,
                pillarMetadata,
                parent,
                courtyardIndex,
                "GateLeft",
                center - axis * (gateWidth * 0.5f),
                wallRotation,
                gradeElevation,
                wallScale,
                seed,
                generationTransactions,
                ref pillarCount);
            AddCourtyardPillar(
                pillarPrefab,
                pillarMetadata,
                parent,
                courtyardIndex,
                "GateRight",
                center + axis * (gateWidth * 0.5f),
                wallRotation,
                gradeElevation,
                wallScale,
                seed,
                generationTransactions,
                ref pillarCount);
        }

        private static void AddCourtyardPillar(
            GameObject prefab,
            DenseCityVisualAssetMetadata metadata,
            Transform parent,
            int courtyardIndex,
            string pillarName,
            Vector2 position,
            float rotation,
            float gradeElevation,
            float scale,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            ref int pillarCount)
        {
            if (InstantiateTransactionalGroundedDetail(
                    prefab,
                    metadata,
                    parent,
                    $"{prefab.name}_Courtyard_{courtyardIndex:0000}_{pillarName}",
                    position,
                    gradeElevation + 0.02f,
                    rotation,
                    scale,
                    DenseCityPresentationCategory.Infrastructure,
                    "courtyard-pillar-visual",
                    seed,
                    generationTransactions))
            {
                pillarCount++;
            }
        }

        private static void AddCourtyardWall(
            GameObject prefab,
            DenseCityVisualAssetMetadata metadata,
            Bounds localBounds,
            Transform parent,
            int courtyardIndex,
            string edgeName,
            Vector2 position,
            float rotation,
            float targetLength,
            float gradeElevation,
            float heightScale,
            Vector3 mapOrigin,
            Rect mapSurfaceBounds,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            ref int wallCount,
            ref int wallVisualCount)
        {
            Quaternion worldRotation = Quaternion.Euler(0f, rotation, 0f);
            Bounds initialBounds = TransformLocalBounds(
                localBounds,
                Matrix4x4.TRS(new Vector3(position.x, 0f, position.y), worldRotation, Vector3.one));
            float sourceLength = rotation % 180f == 0f ? initialBounds.size.x : initialBounds.size.z;
            float lengthScale = Mathf.Clamp(targetLength / Mathf.Max(0.1f, sourceLength), 0.22f, 1.65f);
            var scale = new Vector3(lengthScale, heightScale, 1f);
            Bounds scaledBounds = TransformLocalBounds(
                localBounds,
                Matrix4x4.TRS(new Vector3(position.x, 0f, position.y), worldRotation, scale));
            var groundedPosition = new Vector3(
                position.x,
                gradeElevation + 0.02f - scaledBounds.min.y,
                position.y);
            Matrix4x4 worldMatrix = Matrix4x4.TRS(groundedPosition, worldRotation, scale);
            var roadCell = new Vector2Int(
                Mathf.FloorToInt((position.x - mapOrigin.x) / RoadGridSize),
                Mathf.FloorToInt((position.y - mapOrigin.z) / RoadGridSize));
            var chunk = new Vector2Int(
                Mathf.FloorToInt((float)roadCell.x / RoadChunkSize),
                Mathf.FloorToInt((float)roadCell.y / RoadChunkSize));
            var blockerSize = new Vector2(
                Mathf.Max(0.1f, localBounds.size.x * lengthScale),
                Mathf.Max(0.1f, localBounds.size.z));
            GameObject wall = null;
            try
            {
                bool hasGameplayBlocker = TryResolveClippedSurface(
                    position,
                    scaledBounds.size.x,
                    scaledBounds.size.z,
                    gradeElevation + 0.02f,
                    mapSurfaceBounds,
                    out Matrix4x4 blockerMatrix,
                    out Vector2 clippedBlockerSize);
                bool accepted = hasGameplayBlocker
                    ? generationTransactions.TryPlaceVisualBlocker(
                        0,
                        sequence => DenseCityVisualBlockerRecordFactory.Create(
                            new DenseCityVisualBlockerRecordInput(
                                DenseCityGeneratorSchema,
                                unchecked((int)seed),
                                0,
                                sequence,
                                "courtyard-wall",
                                DenseCityPresentationCategory.Infrastructure,
                                metadata.PrefabAssetGuid,
                                metadata.PrefabLocalId,
                                metadata.MaterialAssetGuids,
                                worldMatrix,
                                blockerSize,
                                gradeElevation + 0.02f,
                                DenseCityBuildingSurfaceLayer,
                                chunk,
                                true,
                                true,
                                1,
                                blockerMatrix,
                                clippedBlockerSize)),
                        RealizeWall)
                    : generationTransactions.TryPlacePresentationOnlyTerrainVisuals(
                        0,
                        1,
                        sequence => CreatePresentationOnlyTerrainRecords(
                            new[]
                            {
                                new DenseCityTerrainVisualPresentationInput(
                                    "courtyard-wall-visual",
                                    metadata.PrefabAssetGuid,
                                    metadata.PrefabLocalId,
                                    metadata.MaterialAssetGuids,
                                    worldMatrix,
                                    true,
                                    true,
                                    1)
                            },
                            unchecked((int)seed),
                            sequence),
                        RealizeWall);
                if (accepted)
                {
                    wallVisualCount++;
                    if (hasGameplayBlocker)
                        wallCount++;
                }

                bool RealizeWall()
                {
                    wall = DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                        prefab,
                        parent);
                    if (wall == null)
                        return false;
                    wall.name = $"{prefab.name}_Courtyard_{courtyardIndex:0000}_{edgeName}";
                    wall.transform.SetPositionAndRotation(groundedPosition, worldRotation);
                    wall.transform.localScale = scale;
                    ValidateWorldMatrix(wall.transform, worldMatrix, "courtyard wall");
                    DisableColliders(wall);
                    return true;
                }
            }
            catch
            {
                if (wall != null)
                    UnityEngine.Object.DestroyImmediate(wall);
                throw;
            }
        }

        private static BuildingMaterialVariantResult ApplyBuildingMaterialVariants(
            IReadOnlyList<DenseCityRealizedBuildingOwner> realizedOwners,
            uint seed,
            DenseCityBuildingMaterialLibrary materialLibrary)
        {
            if (materialLibrary == null)
                throw new ArgumentNullException(nameof(materialLibrary));
            int buildingsA = 0;
            int buildingsB = 0;
            int buildingsC = 0;
            int materialSlotsChanged = 0;
            int shop05VisibleSlotsChanged = 0;
            int shop05PinkVisibleSlotsAssigned = 0;
            int[] shop05PaletteCounts = new int[DenseCityBuildingMaterialLibrary.ShopToneCount + 1];

            for (int ownerIndex = 0; ownerIndex < realizedOwners.Count; ownerIndex++)
            {
                DenseCityRealizedBuildingOwner owner = realizedOwners[ownerIndex];
                if (!owner.ApplyMaterialVariants)
                    continue;
                Transform wrapper = owner.IntactPresentationRoot;

                Renderer[] renderers = wrapper.GetComponentsInChildren<Renderer>(true);
                bool usesFacadeMaterialFamily = false;
                bool usesShop05MaterialFamily = false;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Material[] sharedMaterials = renderers[rendererIndex].sharedMaterials;
                    for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                    {
                        Material material = sharedMaterials[materialIndex];
                        usesFacadeMaterialFamily |= materialLibrary.IsFacadeFamily(material);
                        usesShop05MaterialFamily |= materialLibrary.IsShopFamily(material);
                    }
                }

                bool usesBuildingMaterialFamily =
                    usesFacadeMaterialFamily || usesShop05MaterialFamily;
                if (!usesBuildingMaterialFamily)
                    continue;

                DenseCityBuildingMaterialSelection selection =
                    DenseCityBuildingMaterialVariantSelector.Select(
                        wrapper.position,
                        seed,
                        usesShop05MaterialFamily
                            ? GeneratedCityBuildingRole.Shop
                            : GeneratedCityBuildingRole.Other,
                        true,
                        usesShop05MaterialFamily);
                if (usesShop05MaterialFamily)
                    shop05PaletteCounts[selection.PaletteIndex]++;
                if (selection.PaletteIndex % 3 == 0)
                    buildingsA++;
                else if (selection.PaletteIndex % 3 == 1)
                    buildingsB++;
                else
                    buildingsC++;

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    Material[] sharedMaterials = renderer.sharedMaterials;
                    bool changed = false;
                    for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                    {
                        Material currentMaterial = sharedMaterials[materialIndex];
                        bool isShopMaterial = materialLibrary.IsShopFamily(currentMaterial);
                        Material selectedMaterial = materialLibrary.Resolve(currentMaterial, selection);
                        if (isShopMaterial &&
                            selection.UseOriginalShopMaterial &&
                            renderer.gameObject.activeInHierarchy)
                        {
                            shop05PinkVisibleSlotsAssigned++;
                        }
                        if (currentMaterial == selectedMaterial)
                            continue;

                        sharedMaterials[materialIndex] = selectedMaterial;
                        materialSlotsChanged++;
                        if (isShopMaterial && renderer.gameObject.activeInHierarchy)
                            shop05VisibleSlotsChanged++;
                        changed = true;
                    }

                    if (changed)
                    {
                        renderer.sharedMaterials = sharedMaterials;
                        EditorUtility.SetDirty(renderer);
                    }
                }
            }

            int shop05OriginalVisibleSlotsRemaining = 0;
            for (int ownerIndex = 0; ownerIndex < realizedOwners.Count; ownerIndex++)
            {
                DenseCityRealizedBuildingOwner owner = realizedOwners[ownerIndex];
                if (!owner.ApplyMaterialVariants)
                    continue;
                Transform wrapper = owner.IntactPresentationRoot;

                Renderer[] renderers = wrapper.GetComponentsInChildren<Renderer>(true);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (!renderer.gameObject.activeInHierarchy)
                        continue;

                    Material[] sharedMaterials = renderer.sharedMaterials;
                    for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
                    {
                        if (materialLibrary.IsOriginalShopMaterial(sharedMaterials[materialIndex]))
                            shop05OriginalVisibleSlotsRemaining++;
                    }
                }
            }

            if (shop05VisibleSlotsChanged == 0 ||
                shop05PinkVisibleSlotsAssigned == 0 ||
                shop05OriginalVisibleSlotsRemaining != shop05PinkVisibleSlotsAssigned)
            {
                throw new InvalidOperationException(
                    "Shop_05 visible material replacement failed. " +
                    $"changed={shop05VisibleSlotsChanged} " +
                    $"pinkAssigned={shop05PinkVisibleSlotsAssigned} " +
                    $"originalRemaining={shop05OriginalVisibleSlotsRemaining}.");
            }

            if (buildingsB == 0 || buildingsC == 0)
            {
                throw new InvalidOperationException(
                    "Dense city building material variation did not produce both B and C variants.");
            }

            Debug.Log(
                $"[DenseCityShop05Materials] originalPink={shop05PaletteCounts[0]} " +
                $"limestone={shop05PaletteCounts[1]} blueGray={shop05PaletteCounts[2]} " +
                $"sageGray={shop05PaletteCounts[3]} taupeGray={shop05PaletteCounts[4]} " +
                $"charcoalGray={shop05PaletteCounts[5]} " +
                $"visibleSlotsChanged={shop05VisibleSlotsChanged} " +
                $"originalVisibleSlotsRemaining={shop05OriginalVisibleSlotsRemaining}");

            return new BuildingMaterialVariantResult(
                buildingsA,
                buildingsB,
                buildingsC,
                materialSlotsChanged);
        }

        private static List<GeneratedBuildingInfo> CollectGeneratedBuildings(
            IReadOnlyList<DenseCityRealizedBuildingOwner> owners)
        {
            var buildings = new List<GeneratedBuildingInfo>(owners.Count);
            for (int index = 0; index < owners.Count; index++)
            {
                DenseCityRealizedBuildingOwner owner = owners[index];
                Transform wrapper = owner.IntactPresentationRoot;
                if (wrapper == null ||
                    !TryGetWorldBounds(wrapper, out Bounds bounds) ||
                    !TryGetLocalRendererBounds(wrapper, out Bounds localBounds))
                {
                    continue;
                }

                buildings.Add(new GeneratedBuildingInfo(owner, bounds, localBounds));
            }

            return buildings;
        }

        private static int AddRooftopWaterTanks(
            List<GeneratedBuildingInfo> buildings,
            GameObject[] waterTankPrefabs,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
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
                float placementChance = building.IsShop ? 0.78f : 0.42f;
                if (Hash01(hash ^ 0x74b21e63u) > placementChance)
                    continue;

                GameObject prefab = waterTankPrefabs[hash % (uint)waterTankPrefabs.Length];
                if (!TryFindLowerRoofAnchor(building, hash ^ 0x83b9d20du, out Vector3 roofAnchor))
                    continue;

                if (InstantiateOwnedBuildingAttachment(
                        prefab,
                        building,
                        $"{prefab.name}_Roof_{count:0000}",
                        new Vector2(roofAnchor.x, roofAnchor.z),
                        roofAnchor.y + 0.025f,
                        building.Wrapper.eulerAngles.y + Hash01(hash ^ 0x5e3b7421u) * 360f,
                        Mathf.Lerp(0.82f, 1.08f, Hash01(hash ^ 0xcf5087abu)),
                        generationTransactions))
                {
                    count++;
                }
            }

            return count;
        }

        private static int AddRooftopUtilityProps(
            List<GeneratedBuildingInfo> buildings,
            GameObject[] utilityPrefabs,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            int count = 0;
            for (int buildingIndex = 0; buildingIndex < buildings.Count; buildingIndex++)
            {
                GeneratedBuildingInfo building = buildings[buildingIndex];
                float roofArea = building.Bounds.size.x * building.Bounds.size.z;
                if ((!building.IsShop && !building.IsHouse) ||
                    roofArea < 62f ||
                    building.Bounds.size.y < 4.5f)
                {
                    continue;
                }

                uint hash = HashGroundPatch(
                    Mathf.RoundToInt(building.Bounds.center.x * 10f),
                    Mathf.RoundToInt(building.Bounds.center.z * 10f),
                    unchecked((int)seed) ^ 0x6a31);
                float placementChance = building.IsShop ? 0.82f : 0.48f;
                if (Hash01(hash ^ 0x11b457d3u) > placementChance)
                    continue;

                int desiredCount = building.IsShop && roofArea > 150f &&
                                   Hash01(hash ^ 0xb5226f41u) < 0.52f
                    ? 2
                    : 1;
                for (int detailIndex = 0; detailIndex < desiredCount; detailIndex++)
                {
                    uint detailHash = hash ^ (uint)(detailIndex * 0x9e3779b9);
                    GameObject prefab = utilityPrefabs[detailHash % (uint)utilityPrefabs.Length];
                    if (!TryFindLowerRoofAnchor(building, detailHash ^ 0x945d32abu, out Vector3 roofAnchor))
                        continue;

                    if (InstantiateOwnedBuildingAttachment(
                            prefab,
                            building,
                            $"{prefab.name}_RoofUtility_{count:0000}",
                            new Vector2(roofAnchor.x, roofAnchor.z),
                            roofAnchor.y + 0.025f,
                            building.Wrapper.eulerAngles.y + Hash01(detailHash ^ 0x71e9042fu) * 360f,
                            Mathf.Lerp(0.82f, 1.05f, Hash01(detailHash ^ 0x3c85d719u)),
                            generationTransactions))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int AddShopWallProps(
            List<GeneratedBuildingInfo> buildings,
            GameObject[] wallPropPrefabs,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            int count = 0;
            for (int buildingIndex = 0; buildingIndex < buildings.Count; buildingIndex++)
            {
                GeneratedBuildingInfo building = buildings[buildingIndex];
                if (!building.IsShop || building.Bounds.size.y < 4f)
                    continue;

                uint hash = HashGroundPatch(
                    Mathf.RoundToInt(building.Bounds.center.x * 10f),
                    Mathf.RoundToInt(building.Bounds.center.z * 10f),
                    unchecked((int)seed) ^ 0x7e29);
                int desiredCount = building.Bounds.size.x * building.Bounds.size.z > 115f ? 2 : 1;
                for (int detailIndex = 0; detailIndex < desiredCount; detailIndex++)
                {
                    uint detailHash = hash ^ (uint)(detailIndex * 0x45d9f3b);
                    GameObject prefab = wallPropPrefabs[detailHash % (uint)wallPropPrefabs.Length];
                    int face = (int)((detailHash >> 4) & 3u);
                    float along = Mathf.Lerp(-0.28f, 0.28f, Hash01(detailHash ^ 0x8a31fc55u));
                    float localYaw = face switch
                    {
                        0 => 90f,
                        1 => 270f,
                        2 => 0f,
                        _ => 180f
                    };
                    Vector3 localNormal = face switch
                    {
                        0 => Vector3.left,
                        1 => Vector3.right,
                        2 => Vector3.back,
                        _ => Vector3.forward
                    };
                    float localHeight = Mathf.Lerp(
                        building.LocalBounds.min.y + building.LocalBounds.size.y * 0.42f,
                        building.LocalBounds.min.y + building.LocalBounds.size.y * 0.68f,
                        Hash01(detailHash ^ 0x2b1975e3u));
                    Vector3 localAnchor = face switch
                    {
                        0 => new Vector3(
                            building.LocalBounds.min.x,
                            localHeight,
                            building.LocalBounds.center.z + along * building.LocalBounds.size.z),
                        1 => new Vector3(
                            building.LocalBounds.max.x,
                            localHeight,
                            building.LocalBounds.center.z + along * building.LocalBounds.size.z),
                        2 => new Vector3(
                            building.LocalBounds.center.x + along * building.LocalBounds.size.x,
                            localHeight,
                            building.LocalBounds.min.z),
                        _ => new Vector3(
                            building.LocalBounds.center.x + along * building.LocalBounds.size.x,
                            localHeight,
                            building.LocalBounds.max.z)
                    };
                    Vector3 anchor = building.Wrapper.TransformPoint(localAnchor);
                    Vector3 outwardNormal = building.Wrapper.TransformDirection(localNormal).normalized;

                    GameObject instance =
                        DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                            prefab,
                            building.Wrapper);
                    if (instance == null)
                        continue;

                    instance.name = $"{prefab.name}_ShopWall_{count:0000}";
                    instance.transform.SetPositionAndRotation(
                        anchor,
                        Quaternion.Euler(0f, building.Wrapper.eulerAngles.y + localYaw, 0f));
                    instance.transform.localScale = Vector3.one *
                                                    Mathf.Lerp(0.82f, 1.05f, Hash01(detailHash ^ 0xe2c631a7u));
                    if (!TryGetRendererBounds(instance, out Bounds propBounds))
                    {
                        UnityEngine.Object.DestroyImmediate(instance);
                        continue;
                    }

                    Vector3 position = instance.transform.position;
                    position.y += anchor.y - propBounds.center.y;
                    float projectedExtent =
                        Mathf.Abs(outwardNormal.x) * propBounds.extents.x +
                        Mathf.Abs(outwardNormal.y) * propBounds.extents.y +
                        Mathf.Abs(outwardNormal.z) * propBounds.extents.z;
                    float innerProjection = Vector3.Dot(propBounds.center, outwardNormal) - projectedExtent;
                    float wallProjection = Vector3.Dot(anchor, outwardNormal);
                    position += outwardNormal * (wallProjection + 0.025f - innerProjection);
                    instance.transform.position = position;
                    try
                    {
                        if (!generationTransactions.TryPlaceBuildingAttachment(
                                building.Owner,
                                prefab,
                                instance.transform,
                                instance.transform.localToWorldMatrix,
                                DenseCityPresentationCategory.BuildingAttachmentIntact,
                                () => instance.transform.parent == building.Wrapper))
                        {
                            UnityEngine.Object.DestroyImmediate(instance);
                            continue;
                        }
                        DisableColliders(instance);
                        SetStaticRecursively(instance);
                        count++;
                    }
                    catch
                    {
                        UnityEngine.Object.DestroyImmediate(instance);
                        throw;
                    }
                }
            }

            return count;
        }

        private static bool TryFindLowerRoofAnchor(
            GeneratedBuildingInfo building,
            uint hash,
            out Vector3 anchor)
        {
            anchor = default;
            MeshFilter[] meshFilters = building.Wrapper.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length == 0)
                return false;

            var roofTriangles = new List<RoofTriangle>(256);
            for (int filterIndex = 0; filterIndex < meshFilters.Length; filterIndex++)
            {
                MeshFilter meshFilter = meshFilters[filterIndex];
                if (!meshFilter.gameObject.activeInHierarchy || meshFilter.sharedMesh == null)
                    continue;

                CollectRoofTriangles(building.Wrapper, meshFilter, roofTriangles);
            }

            if (roofTriangles.Count == 0)
                return false;

            float minimumLocalRoofHeight = building.LocalBounds.min.y + building.LocalBounds.size.y * 0.55f;
            float maximumLocalRoofHeight = building.LocalBounds.max.y + 0.05f;
            float lowestLocalRoofHeight = float.PositiveInfinity;
            float phase = Hash01(hash ^ 0x9e3779b9u) * Mathf.PI * 2f;

            for (int sampleIndex = 0; sampleIndex < 16; sampleIndex++)
            {
                float angle = phase + sampleIndex * Mathf.PI * 0.61803398875f;
                float radius = Mathf.Lerp(
                    0.24f,
                    0.43f,
                    Hash01(hash ^ (uint)(sampleIndex * 0x45d9f3b)));
                Vector3 localColumn = new(
                    building.LocalBounds.center.x + Mathf.Cos(angle) * building.LocalBounds.size.x * radius,
                    0f,
                    building.LocalBounds.center.z + Mathf.Sin(angle) * building.LocalBounds.size.z * radius);
                float sampleTop = float.NegativeInfinity;
                for (int triangleIndex = 0; triangleIndex < roofTriangles.Count; triangleIndex++)
                {
                    RoofTriangle triangle = roofTriangles[triangleIndex];
                    if (!TryInterpolateTriangleHeight(
                            triangle,
                            localColumn.x,
                            localColumn.z,
                            out float triangleHeight) ||
                        triangleHeight <= sampleTop)
                    {
                        continue;
                    }

                    sampleTop = triangleHeight;
                }

                if (sampleTop < minimumLocalRoofHeight ||
                    sampleTop > maximumLocalRoofHeight ||
                    sampleTop >= lowestLocalRoofHeight)
                    continue;

                lowestLocalRoofHeight = sampleTop;
                anchor = building.Wrapper.TransformPoint(new Vector3(
                    localColumn.x,
                    sampleTop,
                    localColumn.z));
            }

            return !float.IsPositiveInfinity(lowestLocalRoofHeight);
        }

        private static void CollectRoofTriangles(
            Transform wrapper,
            MeshFilter meshFilter,
            List<RoofTriangle> output)
        {
            Mesh mesh = meshFilter.sharedMesh;
            using Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            Mesh.MeshData meshData = meshDataArray[0];
            if (!meshData.HasVertexAttribute(VertexAttribute.Position) ||
                meshData.GetVertexAttributeFormat(VertexAttribute.Position) != VertexAttributeFormat.Float32 ||
                meshData.GetVertexAttributeDimension(VertexAttribute.Position) < 3)
            {
                return;
            }

            int positionStream = meshData.GetVertexAttributeStream(VertexAttribute.Position);
            int positionOffset = meshData.GetVertexAttributeOffset(VertexAttribute.Position);
            int vertexStride = meshData.GetVertexBufferStride(positionStream);
            NativeArray<byte> vertexData = meshData.GetVertexData<byte>(positionStream);
            var localVertices = new Vector3[meshData.vertexCount];
            Matrix4x4 meshToWrapper = wrapper.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
            for (int vertexIndex = 0; vertexIndex < localVertices.Length; vertexIndex++)
            {
                int byteOffset = vertexIndex * vertexStride + positionOffset;
                Vector3 meshVertex = new(
                    ReadFloat(vertexData, byteOffset),
                    ReadFloat(vertexData, byteOffset + 4),
                    ReadFloat(vertexData, byteOffset + 8));
                localVertices[vertexIndex] = meshToWrapper.MultiplyPoint3x4(meshVertex);
            }

            if (meshData.indexFormat == IndexFormat.UInt16)
            {
                NativeArray<ushort> indices = meshData.GetIndexData<ushort>();
                for (int subMeshIndex = 0; subMeshIndex < meshData.subMeshCount; subMeshIndex++)
                    AppendRoofTriangles(meshData.GetSubMesh(subMeshIndex), indices, localVertices, output);
            }
            else
            {
                NativeArray<uint> indices = meshData.GetIndexData<uint>();
                for (int subMeshIndex = 0; subMeshIndex < meshData.subMeshCount; subMeshIndex++)
                    AppendRoofTriangles(meshData.GetSubMesh(subMeshIndex), indices, localVertices, output);
            }
        }

        private static void AppendRoofTriangles<TIndex>(
            SubMeshDescriptor subMesh,
            NativeArray<TIndex> indices,
            Vector3[] vertices,
            List<RoofTriangle> output)
            where TIndex : unmanaged
        {
            if (subMesh.topology != MeshTopology.Triangles)
                return;

            int end = subMesh.indexStart + subMesh.indexCount;
            for (int index = subMesh.indexStart; index + 2 < end; index += 3)
            {
                int aIndex = ReadIndex(indices[index]) + subMesh.baseVertex;
                int bIndex = ReadIndex(indices[index + 1]) + subMesh.baseVertex;
                int cIndex = ReadIndex(indices[index + 2]) + subMesh.baseVertex;
                if ((uint)aIndex >= vertices.Length ||
                    (uint)bIndex >= vertices.Length ||
                    (uint)cIndex >= vertices.Length)
                {
                    continue;
                }

                Vector3 a = vertices[aIndex];
                Vector3 b = vertices[bIndex];
                Vector3 c = vertices[cIndex];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 0.000001f || normal.normalized.y < 0.72f)
                    continue;

                output.Add(new RoofTriangle(a, b, c));
            }

            static int ReadIndex(TIndex value)
            {
                if (typeof(TIndex) == typeof(ushort))
                    return (ushort)(object)value;
                return checked((int)(uint)(object)value);
            }
        }

        private static bool TryInterpolateTriangleHeight(
            RoofTriangle triangle,
            float x,
            float z,
            out float height)
        {
            float denominator =
                (triangle.B.z - triangle.C.z) * (triangle.A.x - triangle.C.x) +
                (triangle.C.x - triangle.B.x) * (triangle.A.z - triangle.C.z);
            if (Mathf.Abs(denominator) < 0.000001f)
            {
                height = 0f;
                return false;
            }

            float aWeight =
                ((triangle.B.z - triangle.C.z) * (x - triangle.C.x) +
                 (triangle.C.x - triangle.B.x) * (z - triangle.C.z)) / denominator;
            float bWeight =
                ((triangle.C.z - triangle.A.z) * (x - triangle.C.x) +
                 (triangle.A.x - triangle.C.x) * (z - triangle.C.z)) / denominator;
            float cWeight = 1f - aWeight - bWeight;
            const float epsilon = -0.0001f;
            if (aWeight < epsilon || bWeight < epsilon || cWeight < epsilon)
            {
                height = 0f;
                return false;
            }

            height =
                triangle.A.y * aWeight +
                triangle.B.y * bWeight +
                triangle.C.y * cWeight;
            return true;
        }

        private static float ReadFloat(NativeArray<byte> bytes, int offset)
        {
            int bits = bytes[offset] |
                       bytes[offset + 1] << 8 |
                       bytes[offset + 2] << 16 |
                       bytes[offset + 3] << 24;
            return BitConverter.Int32BitsToSingle(bits);
        }

        private static int AddGroundedBuildingProps(
            Transform parent,
            List<GeneratedBuildingInfo> buildings,
            GameObject[] propPrefabs,
            DenseCityVisualAssetMetadata[] propMetadata,
            Vector2 civicCenter,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            Vector3 mapOrigin,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
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

                    int prefabIndex = (int)(detailHash % (uint)propPrefabs.Length);
                    GameObject prefab = propPrefabs[prefabIndex];
                    if (InstantiateTransactionalGroundedDetailClearOfRoads(
                            prefab,
                            propMetadata[prefabIndex],
                            parent,
                            $"{prefab.name}_Street_{count:0000}",
                            position,
                            gradeElevation + 0.035f,
                            Hash01(detailHash ^ 0xa1de7c35u) * 360f,
                            Mathf.Lerp(0.82f, 1.12f, Hash01(detailHash ^ 0x9c13b5e7u)),
                            roadCells,
                            mapOrigin,
                            seed,
                            generationTransactions))
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

        private static (int trees, int rocks, int gameplayRocks) AddDenseTreeAndRockClusters(
            Transform treeParent,
            Transform rockParent,
            List<GeneratedBuildingInfo> buildings,
            GameObject[] treePrefabs,
            DenseCityVisualAssetMetadata[] treeMetadata,
            GameObject[] rockPrefabs,
            DenseCityVisualAssetMetadata[] rockMetadata,
            Vector3 mapOrigin,
            float mapWidth,
            float mapDepth,
            Rect mapSurfaceBounds,
            CityFootprint cityFootprint,
            Rect authoredCoreBounds,
            HashSet<Vector2Int> roadCells,
            List<Rect> reservedAreas,
            float gradeElevation,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            int treeCount = 0;
            int rockCount = 0;
            int gameplayRockCount = 0;
            var treeOccupiedAreas = new List<Rect>();
            var rockOccupiedAreas = new List<Rect>();
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

                        int prefabIndex = Hash01(detailHash ^ 0xf28c4b13u) < 0.42f ? 0 : 1;
                        GameObject prefab = treePrefabs[prefabIndex];
                        if (TryInstantiateTransactionalGroundedFreeDetail(
                                prefab,
                                treeMetadata[prefabIndex],
                                treeParent,
                                $"{prefab.name}_Cluster_{treeCount:0000}",
                                position,
                                gradeElevation + 0.03f,
                                Hash01(detailHash ^ 0x62ae91d5u) * 360f,
                                Mathf.Lerp(0.82f, 1.24f, Hash01(detailHash ^ 0xd12047c9u)),
                                roadCells,
                                mapOrigin,
                                buildings,
                                reservedAreas,
                                rockOccupiedAreas,
                                authoredCoreBounds,
                                DenseCityPresentationCategory.Vegetation,
                                "urban-tree-visual",
                                seed,
                                generationTransactions,
                                out Rect treeOccupiedArea))
                        {
                            treeOccupiedAreas.Add(treeOccupiedArea);
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
                            int prefabIndex = (int)(hash % (uint)rockPrefabs.Length);
                            GameObject prefab = rockPrefabs[prefabIndex];
                            if (TryInstantiateTransactionalGroundedFreeVisualBlocker(
                                    prefab,
                                    rockMetadata[prefabIndex],
                                    rockParent,
                                    $"{prefab.name}_Urban_{rockCount:0000}",
                                    rockPosition,
                                    gradeElevation + 0.02f,
                                    Hash01(hash ^ 0xc391f287u) * 360f,
                                    Mathf.Lerp(0.65f, 1.25f, Hash01(hash ^ 0x3b8c592du)),
                                    roadCells,
                                    mapOrigin,
                                    buildings,
                                    reservedAreas,
                                    treeOccupiedAreas,
                                    authoredCoreBounds,
                                    mapSurfaceBounds,
                                    "urban-rock",
                                    seed,
                                    generationTransactions,
                                    out Rect rockOccupiedArea))
                            {
                                rockOccupiedAreas.Add(rockOccupiedArea);
                                rockCount++;
                                if (mapSurfaceBounds.Overlaps(rockOccupiedArea))
                                    gameplayRockCount++;
                            }
                        }
                    }
                }
            }

            return (treeCount, rockCount, gameplayRockCount);
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

        private static bool InstantiateOwnedBuildingAttachment(
            GameObject prefab,
            GeneratedBuildingInfo building,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            return InstantiateOwnedBuildingAttachment(
                prefab,
                building.Owner,
                objectName,
                position,
                supportHeight,
                rotationDegrees,
                scale,
                generationTransactions);
        }

        private static bool InstantiateOwnedBuildingAttachment(
            GameObject prefab,
            DenseCityRealizedBuildingOwner owner,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            Transform ownerRoot = owner.IntactPresentationRoot;
            GameObject instance =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(prefab, ownerRoot);
            if (instance == null)
                return false;

            try
            {
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
                if (!generationTransactions.TryPlaceBuildingAttachment(
                        owner,
                        prefab,
                        instance.transform,
                        instance.transform.localToWorldMatrix,
                        DenseCityPresentationCategory.BuildingAttachmentIntact,
                        () => instance.transform.parent == ownerRoot))
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                    return false;
                }

                DisableColliders(instance);
                SetStaticRecursively(instance);
                return true;
            }
            catch
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
                throw;
            }
        }

        private readonly struct GroundedDetailPlan
        {
            internal GroundedDetailPlan(
                GameObject prefab,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Prefab = prefab;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                WorldMatrix = Matrix4x4.TRS(position, rotation, scale);
            }

            internal GameObject Prefab { get; }
            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal Vector3 Scale { get; }
            internal Matrix4x4 WorldMatrix { get; }
        }

        private static bool InstantiateTransactionalGroundedDetail(
            GameObject prefab,
            DenseCityVisualAssetMetadata metadata,
            Transform parent,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale,
            DenseCityPresentationCategory category,
            string recordKind,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            if (generationTransactions == null)
                throw new ArgumentNullException(nameof(generationTransactions));
            GroundedDetailPlan plan = PlanGroundedDetail(
                prefab,
                position,
                supportHeight,
                rotationDegrees,
                scale);
            GameObject instance = null;
            try
            {
                return generationTransactions.TryPlaceRenderOnlyPresentation(
                    0,
                    sequence => DenseCityRenderOnlyPresentationRecordFactory.Create(
                        new DenseCityRenderOnlyPresentationRecordInput(
                            DenseCityGeneratorSchema,
                            unchecked((int)seed),
                            0,
                            sequence,
                            recordKind,
                            category,
                            metadata.PrefabAssetGuid,
                            metadata.PrefabLocalId,
                            metadata.MaterialAssetGuids,
                            plan.WorldMatrix,
                            true,
                            true,
                            1)),
                    () =>
                    {
                        instance = DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                            plan.Prefab,
                            parent);
                        if (instance == null)
                            return false;
                        instance.name = objectName;
                        instance.transform.SetPositionAndRotation(plan.Position, plan.Rotation);
                        instance.transform.localScale = plan.Scale;
                        ValidateGroundedDetailMatrix(instance, plan);
                        DisableColliders(instance);
                        return true;
                    });
            }
            catch
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
                throw;
            }
        }

        private static GroundedDetailPlan PlanGroundedDetail(
            GameObject prefab,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) ||
                !float.IsFinite(supportHeight) || !float.IsFinite(rotationDegrees) ||
                !float.IsFinite(scale) || scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale));
            }
            if (!TryGetPrefabLocalRendererBounds(prefab.transform, out Bounds localBounds))
                throw new InvalidOperationException($"Grounded detail prefab '{prefab.name}' has no renderer bounds.");

            Quaternion rotation = Quaternion.Euler(0f, rotationDegrees, 0f);
            var resolvedScale = Vector3.one * scale;
            var rootPosition = new Vector3(
                position.x,
                supportHeight - localBounds.min.y * scale,
                position.y);
            return new GroundedDetailPlan(prefab, rootPosition, rotation, resolvedScale);
        }

        private static void ValidateGroundedDetailMatrix(GameObject instance, GroundedDetailPlan plan)
        {
            Matrix4x4 actual = instance.transform.localToWorldMatrix;
            for (int index = 0; index < 16; index++)
            {
                if (Mathf.Abs(actual[index] - plan.WorldMatrix[index]) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        $"Grounded detail transform parity failed for '{instance.name}' at matrix index {index}.");
                }
            }
        }

        private static bool InstantiateGroundedDetail(
            GameObject prefab,
            Transform parent,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale,
            Transform attachmentParent = null)
        {
            GameObject instance =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(prefab, parent);
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
            if (attachmentParent != null)
                instance.transform.SetParent(attachmentParent, true);
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
            GameObject instance =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(prefab, parent);
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

        private static bool InstantiateTransactionalGroundedDetailClearOfRoads(
            GameObject prefab,
            DenseCityVisualAssetMetadata metadata,
            Transform parent,
            string objectName,
            Vector2 position,
            float supportHeight,
            float rotationDegrees,
            float scale,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions)
        {
            GroundedDetailPlan plan = PlanGroundedDetail(
                prefab,
                position,
                supportHeight,
                rotationDegrees,
                scale);
            if (!TryGetPrefabLocalRendererBounds(prefab.transform, out Bounds localBounds))
                throw new InvalidOperationException($"Street prop prefab '{prefab.name}' has no renderer bounds.");
            Bounds plannedBounds = TransformLocalBounds(localBounds, plan.WorldMatrix);
            const float roadClearance = 0.12f;
            var actualFootprint = Rect.MinMaxRect(
                plannedBounds.min.x - roadClearance,
                plannedBounds.min.z - roadClearance,
                plannedBounds.max.x + roadClearance,
                plannedBounds.max.z + roadClearance);
            if (OverlapsRoadCell(actualFootprint, roadCells, mapOrigin))
                return false;

            return InstantiateTransactionalGroundedDetail(
                prefab,
                metadata,
                parent,
                objectName,
                position,
                supportHeight,
                rotationDegrees,
                scale,
                DenseCityPresentationCategory.Prop,
                "street-prop-visual",
                seed,
                generationTransactions);
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
            GameObject instance =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(prefab, parent);
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

        private static bool TryInstantiateTransactionalGroundedFreeDetail(
            GameObject prefab,
            DenseCityVisualAssetMetadata metadata,
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
            DenseCityPresentationCategory category,
            string recordKind,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            out Rect occupiedArea)
        {
            GroundedDetailPlan plan = PlanGroundedDetail(
                prefab,
                position,
                supportHeight,
                rotationDegrees,
                scale);
            if (!TryGetPrefabLocalRendererBounds(prefab.transform, out Bounds localBounds))
                throw new InvalidOperationException($"Free detail prefab '{prefab.name}' has no renderer bounds.");
            Bounds plannedBounds = TransformLocalBounds(localBounds, plan.WorldMatrix);
            const float detailClearance = 0.08f;
            occupiedArea = Rect.MinMaxRect(
                plannedBounds.min.x - detailClearance,
                plannedBounds.min.z - detailClearance,
                plannedBounds.max.x + detailClearance,
                plannedBounds.max.z + detailClearance);
            if (authoredCoreBounds.Overlaps(occupiedArea) ||
                OverlapsRoadCell(occupiedArea, roadCells, mapOrigin) ||
                OverlapsAnyBuilding(occupiedArea, buildings) ||
                OverlapsAnyRect(occupiedArea, reservedAreas) ||
                OverlapsAnyRect(occupiedArea, localReservedAreas))
            {
                occupiedArea = default;
                return false;
            }

            bool accepted = InstantiateTransactionalGroundedDetail(
                prefab,
                metadata,
                parent,
                objectName,
                position,
                supportHeight,
                rotationDegrees,
                scale,
                category,
                recordKind,
                seed,
                generationTransactions);
            if (!accepted)
                occupiedArea = default;
            return accepted;
        }

        private static bool TryInstantiateTransactionalGroundedFreeVisualBlocker(
            GameObject prefab,
            DenseCityVisualAssetMetadata metadata,
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
            Rect mapSurfaceBounds,
            string recordKind,
            uint seed,
            DenseCityGenerationTransactionContext generationTransactions,
            out Rect occupiedArea)
        {
            GroundedDetailPlan plan = PlanGroundedDetail(
                prefab,
                position,
                supportHeight,
                rotationDegrees,
                scale);
            if (!TryGetPrefabLocalRendererBounds(prefab.transform, out Bounds localBounds))
                throw new InvalidOperationException($"Blocked free-detail prefab '{prefab.name}' has no renderer bounds.");
            Bounds plannedBounds = TransformLocalBounds(localBounds, plan.WorldMatrix);
            const float detailClearance = 0.08f;
            occupiedArea = Rect.MinMaxRect(
                plannedBounds.min.x - detailClearance,
                plannedBounds.min.z - detailClearance,
                plannedBounds.max.x + detailClearance,
                plannedBounds.max.z + detailClearance);
            if (authoredCoreBounds.Overlaps(occupiedArea) ||
                OverlapsRoadCell(occupiedArea, roadCells, mapOrigin) ||
                OverlapsAnyBuilding(occupiedArea, buildings) ||
                OverlapsAnyRect(occupiedArea, reservedAreas) ||
                OverlapsAnyRect(occupiedArea, localReservedAreas))
            {
                occupiedArea = default;
                return false;
            }

            var roadCell = new Vector2Int(
                Mathf.FloorToInt((position.x - mapOrigin.x) / RoadGridSize),
                Mathf.FloorToInt((position.y - mapOrigin.z) / RoadGridSize));
            var chunk = new Vector2Int(
                Mathf.FloorToInt((float)roadCell.x / RoadChunkSize),
                Mathf.FloorToInt((float)roadCell.y / RoadChunkSize));
            var blockerSize = new Vector2(
                Mathf.Max(0.1f, localBounds.size.x * scale),
                Mathf.Max(0.1f, localBounds.size.z * scale));
            GameObject instance = null;
            try
            {
                bool hasGameplayBlocker = TryResolveClippedSurface(
                    new Vector2(plannedBounds.center.x, plannedBounds.center.z),
                    plannedBounds.size.x,
                    plannedBounds.size.z,
                    supportHeight,
                    mapSurfaceBounds,
                    out Matrix4x4 blockerMatrix,
                    out Vector2 clippedBlockerSize);
                bool accepted = hasGameplayBlocker
                    ? generationTransactions.TryPlaceVisualBlocker(
                        0,
                        sequence => DenseCityVisualBlockerRecordFactory.Create(
                            new DenseCityVisualBlockerRecordInput(
                                DenseCityGeneratorSchema,
                                unchecked((int)seed),
                                0,
                                sequence,
                                recordKind,
                                DenseCityPresentationCategory.Prop,
                                metadata.PrefabAssetGuid,
                                metadata.PrefabLocalId,
                                metadata.MaterialAssetGuids,
                                plan.WorldMatrix,
                                blockerSize,
                                supportHeight,
                                DenseCityBuildingSurfaceLayer,
                                chunk,
                                true,
                                true,
                                1,
                                blockerMatrix,
                                clippedBlockerSize)),
                        RealizeBlocker)
                    : generationTransactions.TryPlacePresentationOnlyVisuals(
                        0,
                        2,
                        1,
                        sequence => new[]
                        {
                            DenseCityRenderOnlyPresentationRecordFactory.Create(
                                new DenseCityRenderOnlyPresentationRecordInput(
                                    DenseCityGeneratorSchema,
                                    unchecked((int)seed),
                                    0,
                                    sequence + 1,
                                    string.Concat(recordKind, "-visual"),
                                    DenseCityPresentationCategory.Prop,
                                    metadata.PrefabAssetGuid,
                                    metadata.PrefabLocalId,
                                    metadata.MaterialAssetGuids,
                                    plan.WorldMatrix,
                                    true,
                                    true,
                                    1))
                        },
                        RealizeBlocker);
                if (!accepted)
                    occupiedArea = default;
                return accepted;

                bool RealizeBlocker()
                {
                    instance = DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(
                        prefab,
                        parent);
                    if (instance == null)
                        return false;
                    instance.name = objectName;
                    instance.transform.SetPositionAndRotation(plan.Position, plan.Rotation);
                    instance.transform.localScale = plan.Scale;
                    ValidateGroundedDetailMatrix(instance, plan);
                    DisableColliders(instance);
                    return true;
                }
            }
            catch
            {
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
                occupiedArea = default;
                throw;
            }
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

        private static void ValidateNoNaturalDetailOverlaps(
            Transform treeRoot,
            Transform rockRoot,
            List<GeneratedBuildingInfo> buildings)
        {
            var treeAreas = new List<Rect>(treeRoot.childCount);
            ValidateCategory(treeRoot, "tree", treeAreas);
            var rockAreas = new List<Rect>(rockRoot.childCount);
            ValidateCategory(rockRoot, "rock", rockAreas);

            for (int treeIndex = 0; treeIndex < treeAreas.Count; treeIndex++)
            {
                for (int rockIndex = 0; rockIndex < rockAreas.Count; rockIndex++)
                {
                    if (treeAreas[treeIndex].Overlaps(rockAreas[rockIndex]))
                    {
                        throw new InvalidOperationException(
                            $"Generated dense-city tree footprint {treeAreas[treeIndex]} " +
                            $"overlaps urban rock footprint {rockAreas[rockIndex]}.");
                    }
                }
            }

            Debug.Log(
                $"[DenseCityNaturalDetailAudit] trees={treeAreas.Count} rocks={rockAreas.Count} " +
                "buildingOverlaps=0 treeRockOverlaps=0");

            void ValidateCategory(Transform categoryRoot, string categoryName, List<Rect> areas)
            {
                for (int index = 0; index < categoryRoot.childCount; index++)
                {
                    Transform detail = categoryRoot.GetChild(index);
                    if (detail == null || !TryGetWorldBounds(detail, out Bounds bounds))
                        continue;

                    var footprint = Rect.MinMaxRect(
                        bounds.min.x,
                        bounds.min.z,
                        bounds.max.x,
                        bounds.max.z);
                    if (OverlapsAnyBuilding(footprint, buildings))
                    {
                        throw new InvalidOperationException(
                            $"Generated dense-city {categoryName} '{detail.name}' overlaps a building footprint.");
                    }

                    areas.Add(footprint);
                }
            }
        }

        private static void ValidateBoulevardMedianDetailAnchors(
            Transform detailRoot,
            List<BoulevardMedianCell> medianCells,
            HashSet<Vector2Int> roadCells,
            Vector3 mapOrigin)
        {
            var allowedCenters = new List<Vector2>(medianCells.Count);
            for (int index = 0; index < medianCells.Count; index++)
                allowedCenters.Add(medianCells[index].WorldCenter(mapOrigin));

            for (int index = 0; index < detailRoot.childCount; index++)
            {
                Transform detail = detailRoot.GetChild(index);
                if (detail == null)
                    continue;

                Vector3 position = detail.position;
                Vector2 anchor = new(position.x, position.z);
                bool isOnCenterSeam = false;
                for (int centerIndex = 0; centerIndex < allowedCenters.Count; centerIndex++)
                {
                    if ((allowedCenters[centerIndex] - anchor).sqrMagnitude <= 0.05f * 0.05f)
                    {
                        isOnCenterSeam = true;
                        break;
                    }
                }

                if (!isOnCenterSeam)
                {
                    throw new InvalidOperationException(
                        $"Boulevard median detail '{detail.name}' is not anchored to a paired-road center seam.");
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

        private static DenseCityVisualAssetMetadata[] ExtractVisualMetadata(GameObject[] prefabs)
        {
            var metadata = new DenseCityVisualAssetMetadata[prefabs.Length];
            for (int index = 0; index < prefabs.Length; index++)
                metadata[index] = DenseCityVisualAssetMetadataExtractor.Extract(prefabs[index]);
            return metadata;
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

        private static GameObject CreateNaturalGroundPatch(
            Transform parent,
            string objectName,
            Vector3 topCenter,
            float targetWidth,
            float targetDepth,
            float targetHeight,
            uint hash,
            bool forcePrimaryGroundPrefab = false)
        {
            NaturalGroundPatchPlan plan = PlanNaturalGroundPatch(
                topCenter,
                targetWidth,
                targetDepth,
                targetHeight,
                hash,
                forcePrimaryGroundPrefab);
            return RealizeNaturalGroundPatch(parent, objectName, plan);
        }

        private static NaturalGroundPatchPlan PlanNaturalGroundPatch(
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
            if (!TryGetRendererBounds(prefab, out Bounds sourceBounds))
                throw new InvalidOperationException($"Natural ground prefab '{prefab.name}' has no renderer bounds.");

            float widthVariation = Mathf.Lerp(0.92f, 1.12f, Hash01(hash ^ 0x68bc21ebu));
            float depthVariation = Mathf.Lerp(0.92f, 1.12f, Hash01(hash ^ 0x02e5be93u));
            float heightVariation = Mathf.Lerp(0.82f, 1.18f, Hash01(hash ^ 0x967a889bu));
            var scale = new Vector3(
                targetWidth * widthVariation / Mathf.Max(0.01f, sourceBounds.size.x),
                targetHeight * heightVariation / Mathf.Max(0.01f, sourceBounds.size.y),
                targetDepth * depthVariation / Mathf.Max(0.01f, sourceBounds.size.z));
            Quaternion rotation = Quaternion.Euler(0f, Hash01(hash ^ 0x4f1bbcdcu) * 360f, 0f);
            var position = new Vector3(
                topCenter.x,
                topCenter.y - 0.025f - sourceBounds.max.y * scale.y,
                topCenter.z);
            return new NaturalGroundPatchPlan(
                prefab,
                GetGroundVariationMaterial(),
                position,
                rotation,
                scale);
        }

        private static GameObject RealizeNaturalGroundPatch(
            Transform parent,
            string objectName,
            NaturalGroundPatchPlan plan)
        {
            GameObject patch =
                DenseCityPhysicsComponentStripper.InstantiatePrefabWithoutPhysics(plan.Prefab, parent);
            patch.name = objectName;
            patch.transform.SetPositionAndRotation(plan.Position, plan.Rotation);
            patch.transform.localScale = plan.Scale;

            DisableColliders(patch);
            if (plan.Material == null)
                return patch;
            Renderer[] renderers = patch.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
                renderers[index].sharedMaterial = plan.Material;
            return patch;
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

        private static bool TryGetLocalRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                Bounds rendererBounds = renderer.localBounds;
                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 rendererPoint = new(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localPoint = root.InverseTransformPoint(
                                renderer.transform.TransformPoint(rendererPoint));
                            if (!hasBounds)
                            {
                                bounds = new Bounds(localPoint, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localPoint);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        private static bool TryGetPrefabLocalRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                if (renderer == null)
                    continue;

                Bounds rendererBounds = renderer.localBounds;
                Vector3 min = rendererBounds.min;
                Vector3 max = rendererBounds.max;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 rendererPoint = new(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localPoint = root.InverseTransformPoint(
                                renderer.transform.TransformPoint(rendererPoint));
                            if (!hasBounds)
                            {
                                bounds = new Bounds(localPoint, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(localPoint);
                            }
                        }
                    }
                }
            }

            return hasBounds;
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
            AddPrefabList(
                config.HousePrefabs,
                palette.Houses,
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.House);
            AddPrefabList(
                config.ShopPrefabs,
                palette.Shops,
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Shop);
            AddPrefabList(
                config.OtherBuildingPrefabs,
                palette.Other,
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Other);
            AddPrefabList(
                config.HallPrefabs,
                palette.CentralLandmarks,
                DenseCityPresentationCategory.GameplayBuildingIntact,
                GeneratedCityBuildingRole.Civic,
                CentralHallVisualScale,
                applyMaterialVariants: false);
            if (config.ClockTowerPrefab != null && IsDenseCityPrefabUsable(config.ClockTowerPrefab))
            {
                AddCentralLandmark(
                    palette.CentralLandmarks,
                    MeasurePrefab(
                        config.ClockTowerPrefab,
                        CentralClockTowerVisualScale,
                        DenseCityPresentationCategory.GameplayBuildingIntact,
                        GeneratedCityBuildingRole.Civic));
            }
            AddTallOrLargeCandidates(palette.Houses, palette.CentralLandmarks);
            AddTallOrLargeCandidates(palette.Shops, palette.CentralLandmarks);
            AddTallOrLargeCandidates(palette.Other, palette.CentralLandmarks);
            for (int index = 0; index < CleanStandaloneShopPrefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CleanStandaloneShopPrefabPaths[index]);
                if (prefab != null)
                    palette.Shops.Add(MeasurePrefab(
                        prefab,
                        BuildingVisualScale,
                        DenseCityPresentationCategory.GameplayBuildingIntact,
                        GeneratedCityBuildingRole.Shop));
            }
            for (int index = 0; index < ParkPrefabPaths.Length; index++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ParkPrefabPaths[index]);
                if (prefab != null)
                {
                    DenseCityPresentationCategory category = index >= 4
                        ? DenseCityPresentationCategory.Prop
                        : DenseCityPresentationCategory.Vegetation;
                    palette.Park.Add(MeasurePrefab(
                        prefab,
                        1f,
                        category,
                        GeneratedCityBuildingRole.None));
                    if (index >= 4)
                    {
                        palette.Fountains.Add(MeasurePrefab(
                            prefab,
                            0.85f,
                            DenseCityPresentationCategory.Prop,
                            GeneratedCityBuildingRole.None));
                    }
                }
            }

            if (palette.Houses.Count == 0 || palette.Shops.Count == 0)
                throw new InvalidOperationException("Dense city config requires both house and shop prefabs.");
            if (palette.CentralLandmarks.Count == 0)
                throw new InvalidOperationException("Dense city config requires central landmark prefabs.");

            Debug.Log(
                $"[DenseCityBuildingPalette] houses={palette.Houses.Count} shops={palette.Shops.Count} " +
                $"other={palette.Other.Count} centralLandmarks={palette.CentralLandmarks.Count}");
            for (int index = 0; index < palette.CentralLandmarks.Count; index++)
            {
                PrefabFootprint landmark = palette.CentralLandmarks[index];
                Debug.Log(
                    $"[DenseCityCentralLandmarkPalette] prefab={landmark.Prefab.name} " +
                    $"size={landmark.Width:0.0}x{landmark.Height:0.0}x{landmark.Depth:0.0} " +
                    $"scale={landmark.VisualScale:0.00}");
            }
            return palette;
        }

        private static void AddTallOrLargeCandidates(
            List<PrefabFootprint> source,
            List<PrefabFootprint> target)
        {
            for (int index = 0; index < source.Count; index++)
            {
                PrefabFootprint candidate = source[index];
                if (candidate.Height >= 11.5f || candidate.Width * candidate.Depth >= 175f)
                {
                    float landmarkScale = candidate.Prefab.name.IndexOf(
                        "Tower",
                        StringComparison.OrdinalIgnoreCase) >= 0
                        ? CentralTowerVisualScale
                        : CentralLargeBuildingVisualScale;
                    AddCentralLandmark(
                        target,
                        MeasurePrefab(
                            candidate.Prefab,
                            landmarkScale,
                            candidate.PresentationCategory,
                            candidate.BuildingRole));
                }
            }
        }

        private static void AddCentralLandmark(
            List<PrefabFootprint> target,
            PrefabFootprint candidate)
        {
            if (candidate.Prefab == null)
                return;

            for (int index = 0; index < target.Count; index++)
            {
                if (target[index].Prefab == candidate.Prefab)
                    return;
            }

            target.Add(candidate);
        }

        private static void AddPrefabList(
            List<GameObject> source,
            List<PrefabFootprint> target,
            DenseCityPresentationCategory presentationCategory,
            GeneratedCityBuildingRole buildingRole,
            float visualScale = BuildingVisualScale,
            bool applyMaterialVariants = true)
        {
            if (source == null)
                return;
            for (int index = 0; index < source.Count; index++)
            {
                GameObject prefab = source[index];
                if (prefab != null && IsDenseCityPrefabUsable(prefab))
                {
                    target.Add(MeasurePrefab(
                        prefab,
                        visualScale,
                        presentationCategory,
                        buildingRole,
                        applyMaterialVariants));
                }
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

        private static PrefabFootprint MeasurePrefab(
            GameObject prefab,
            float visualScale,
            DenseCityPresentationCategory presentationCategory,
            GeneratedCityBuildingRole buildingRole,
            bool applyMaterialVariants = true)
        {
            Transform visualRoot = FindDescendant(prefab.transform, "CombinedMesh") ?? prefab.transform;
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new PrefabFootprint(
                    prefab,
                    10f,
                    10f,
                    8f,
                    visualScale,
                    presentationCategory,
                    buildingRole,
                    applyMaterialVariants);
            }

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
                ? new PrefabFootprint(
                    prefab,
                    bounds.size.x,
                    bounds.size.z,
                    bounds.size.y,
                    visualScale,
                    presentationCategory,
                    buildingRole,
                    applyMaterialVariants)
                : new PrefabFootprint(
                    prefab,
                    10f,
                    10f,
                    8f,
                    visualScale,
                    presentationCategory,
                    buildingRole,
                    applyMaterialVariants);
        }

        private static PrefabFootprint SelectFrontageBuilding(
            BuildingPalette palette,
            bool preferShop,
            float centralLandmarkChance,
            System.Random random,
            out bool isCentralLandmark)
        {
            isCentralLandmark = palette.CentralLandmarks.Count > 0 &&
                                centralLandmarkChance > 0f &&
                                random.NextDouble() < centralLandmarkChance;
            if (isCentralLandmark)
            {
                return palette.CentralLandmarks[
                    random.Next(palette.CentralLandmarks.Count)];
            }

            return SelectBuilding(palette, preferShop, random);
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
