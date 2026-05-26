using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingPlacementStartupSystem
{
    private BuildingPlacementSystemConfig _config;
    private Camera _worldCamera;
    private float _buildPlaneY;
    private float _placementOutlineHeight = 0.15f;
    private Color _placementValidColor = new(0.15f, 0.85f, 0.2f, 1f);
    private Color _placementInvalidColor = new(0.9f, 0.2f, 0.2f, 1f);
    private Transform _runtimeRoot;
    private Transform _buildingRoot;
    private BuildingDefinition _soldierBaseDefinition;
    private BuildingDefinition _soldierTentDefinition;
    private BuildingDefinition _factoryDefinition;
    private RoadFootprintQuerySystem _roadFootprintQuerySystem;
    private RoadFootprintQuerySystem.Context _roadFootprintQueryContext;

    public Camera WorldCamera => _worldCamera;
    public float BuildPlaneY => _buildPlaneY;
    public Transform BuildingRoot => _buildingRoot;
    public BuildingDefinition SoldierBaseDefinition => _soldierBaseDefinition;
    public BuildingDefinition SoldierTentDefinition => _soldierTentDefinition;
    public BuildingDefinition FactoryDefinition => _factoryDefinition;
    public GameObject RoadPreviewPrefab => _config != null ? _config.RoadPreviewPrefab : null;
    public float BuildButtonPreviewDistanceMultiplier => _config != null ? _config.BuildButtonPreviewDistanceMultiplier : 1f;
    public float UnitCommandButtonPreviewDistanceMultiplier => _config != null ? _config.UnitCommandButtonPreviewDistanceMultiplier : 1f;

    public void ConfigureRoadFootprintQuery(
        RoadFootprintQuerySystem roadFootprintQuerySystem,
        RoadFootprintQuerySystem.Context roadFootprintQueryContext)
    {
        _roadFootprintQuerySystem = roadFootprintQuerySystem;
        _roadFootprintQueryContext = roadFootprintQueryContext;
    }

    public void Init(
        BuildingPlacementSystemConfig configAsset,
        Camera sceneWorldCamera,
        Transform runtimeRoot,
        BuildingDefinitionSystem definitionSystem,
        BuildingRunwaySystem runwaySystem,
        BuildingPlacementPreviewSystem previewSystem,
        Action<UnityEngine.Object> destroyRuntimeObject)
    {
        _config = configAsset;
        _worldCamera = sceneWorldCamera;
        _runtimeRoot = runtimeRoot;
        ApplyConfigIfAvailable(definitionSystem);
        CreateBuildingRoot();
        RebuildConfiguredSpawnableDefinitions(definitionSystem, runwaySystem, destroyRuntimeObject);
        previewSystem.Init(
            _runtimeRoot,
            _placementOutlineHeight,
            _placementValidColor,
            _placementInvalidColor,
            destroyRuntimeObject);
    }

    public void ApplyConfigIfAvailable(BuildingDefinitionSystem definitionSystem)
    {
        if (_config == null || definitionSystem == null)
            return;

        if (_config.WorldCamera != null)
            _worldCamera = _config.WorldCamera;

        IReadOnlyList<GameObject> configuredSpawnables = _config.Spawnables ?? new List<GameObject>();
        UnitPrefabRegistryAuthoringConfig configuredUnitPrefabRegistry = _config.UnitPrefabRegistryConfig;
        IReadOnlyList<GameObject> configuredUnitSpawnPrefabs =
            configuredUnitPrefabRegistry != null && configuredUnitPrefabRegistry.UnitSpawnPrefabs != null
                ? configuredUnitPrefabRegistry.UnitSpawnPrefabs
                : new List<GameObject>();
        definitionSystem.RebuildSpawnablesLookup(configuredSpawnables, configuredUnitSpawnPrefabs);
        _buildPlaneY = _config.BuildPlaneY;
        _placementOutlineHeight = _config.PlacementOutlineHeight;
        _placementValidColor = _config.PlacementValidColor;
        _placementInvalidColor = _config.PlacementInvalidColor;
    }

    public void Dispose(
        BuildingDefinitionSystem definitionSystem,
        BuildingPlacementPreviewSystem previewSystem,
        Action<UnityEngine.Object> destroyRuntimeObject)
    {
        definitionSystem.ClearConfiguredSpawnableDefinitions(target => destroyRuntimeObject?.Invoke(target));
        definitionSystem.ClearConfiguredPrefabLookups();
        _soldierBaseDefinition = null;
        _soldierTentDefinition = null;
        _factoryDefinition = null;

        previewSystem.Dispose();
        if (_buildingRoot != null)
            destroyRuntimeObject?.Invoke(_buildingRoot.gameObject);

        _buildingRoot = null;
        _runtimeRoot = null;
        _config = null;
        _worldCamera = null;
        _roadFootprintQuerySystem = null;
        _roadFootprintQueryContext = default;
    }

    public void FillRoadFootprintMask(GridConfig grid, bool[] roadFootprintMask)
    {
        _roadFootprintQuerySystem?.FillRoadFootprintMask(_roadFootprintQueryContext, grid, roadFootprintMask);
    }

    public bool HasRoadInFootprint(GridConfig grid, Vector2Int originCell, Vector2Int footprintCells)
    {
        return _roadFootprintQuerySystem != null &&
               _roadFootprintQuerySystem.HasRoadInFootprint(
                   _roadFootprintQueryContext,
                   grid,
                   originCell,
                   footprintCells);
    }

    private void CreateBuildingRoot()
    {
        _buildingRoot = new GameObject("RuntimeBuildings").transform;
        _buildingRoot.SetParent(_runtimeRoot, false);
        _buildingRoot.localPosition = Vector3.zero;
        _buildingRoot.localRotation = Quaternion.identity;
        _buildingRoot.localScale = Vector3.one;
    }

    private void RebuildConfiguredSpawnableDefinitions(
        BuildingDefinitionSystem definitionSystem,
        BuildingRunwaySystem runwaySystem,
        Action<UnityEngine.Object> destroyRuntimeObject)
    {
        definitionSystem.RebuildConfiguredSpawnableDefinitions(runwaySystem, target => destroyRuntimeObject?.Invoke(target));

        _soldierBaseDefinition = definitionSystem.FindConfiguredDefinition("Soldier Base");
        _soldierTentDefinition = definitionSystem.FindConfiguredDefinition("Soldier Tent");
        _factoryDefinition = definitionSystem.FindConfiguredDefinition("Factory");
    }
}
