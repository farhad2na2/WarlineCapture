using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed partial class RuntimeCityRoadBuildBridgeSystem : SystemBase
{
    private readonly RuntimeCityRoadBuildBridgeState _state = new();

    public RuntimeCityRoadBuildBridgeState State => _state;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public bool HasRoadRuntimeGenerationSystem => _state.HasRoadRuntimeGenerationSystem;

    public void Configure(
        RoadRuntimeGenerationSystem roadRuntimeGenerationSystem,
        RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext)
    {
        _state.Configure(roadRuntimeGenerationSystem, roadRuntimeGenerationContext);
    }

    public void Clear()
    {
        _state.Clear();
    }

    public bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
    {
        return _state.TryGetRoadCellSizeInGridCells(out roadCellSizeInGridCells);
    }

    public void BeginDeferredRoadEcsSync()
    {
        _state.BeginDeferredRoadEcsSync();
    }

    public void EndDeferredRoadEcsSync()
    {
        _state.EndDeferredRoadEcsSync();
    }

    public bool CreateRoadStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
    {
        return _state.CreateRoadStrokeFromRoadCells(cells);
    }

    public bool CreateAutobahnStrokeFromRoadCells(
        IReadOnlyList<Vector2Int> cells,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd)
    {
        return _state.CreateAutobahnStrokeFromRoadCells(
            cells,
            useAutobahnConnectorAtStart,
            useAutobahnConnectorAtEnd);
    }

    public bool CreateStandaloneStraightRoadChainFromConnector(
        Vector2Int connectorCell,
        Vector2Int direction,
        int length)
    {
        return _state.CreateStandaloneStraightRoadChainFromConnector(connectorCell, direction, length);
    }

    public bool TryGetStandaloneStraightChainEndRoadCell(
        Vector2Int direction,
        out Vector2Int roadConnectionCell)
    {
        return _state.TryGetStandaloneStraightChainEndRoadCell(direction, out roadConnectionCell);
    }
}

internal sealed class RuntimeCityRoadBuildBridgeState
{
    private const int DefaultRoadCellSizeInGridCells = 10;
    private const string RoadCellSizeFallbackFixTag = "RuntimeCityRoadCellFallbackFix_2026-05-26";
    private RoadRuntimeGenerationSystem _roadRuntimeGenerationSystem;
    private RoadRuntimeGenerationSystem.Context _roadRuntimeGenerationContext;

    public bool HasRoadRuntimeGenerationSystem => _roadRuntimeGenerationSystem != null;

    public void Configure(
        RoadRuntimeGenerationSystem roadRuntimeGenerationSystem,
        RoadRuntimeGenerationSystem.Context roadRuntimeGenerationContext)
    {
        _roadRuntimeGenerationSystem = roadRuntimeGenerationSystem;
        _roadRuntimeGenerationContext = roadRuntimeGenerationContext;
    }

    public void Clear()
    {
        _roadRuntimeGenerationSystem = null;
        _roadRuntimeGenerationContext = default;
    }

    public bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
    {
        roadCellSizeInGridCells = 0;
        if (_roadRuntimeGenerationSystem != null &&
            _roadRuntimeGenerationSystem.TryGetRoadCellSizeInGridCells(
                _roadRuntimeGenerationContext,
                out roadCellSizeInGridCells))
        {
            return true;
        }

        if (TryGetGridCellSize(out float gridCellSize) && gridCellSize > 0f)
        {
            roadCellSizeInGridCells = Mathf.Max(1, Mathf.RoundToInt(DefaultRoadCellSizeInGridCells / gridCellSize));
            Debug.LogWarning($"[RuntimeCity] {RoadCellSizeFallbackFixTag} fallback={roadCellSizeInGridCells} gridCellSize={gridCellSize:0.###}");
            return true;
        }

        Debug.LogWarning($"[RuntimeCity] {RoadCellSizeFallbackFixTag} unavailable=missingGridCellSize");
        return false;
    }

    public void BeginDeferredRoadEcsSync()
    {
        _roadRuntimeGenerationSystem?.BeginDeferredRoadEcsSync(_roadRuntimeGenerationContext);
    }

    public void EndDeferredRoadEcsSync()
    {
        _roadRuntimeGenerationSystem?.EndDeferredRoadEcsSync(_roadRuntimeGenerationContext);
    }

    public bool CreateRoadStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
    {
        return _roadRuntimeGenerationSystem != null &&
            _roadRuntimeGenerationSystem.CreateRoadStrokeFromRoadCells(
                _roadRuntimeGenerationContext,
                cells);
    }

    public bool CreateAutobahnStrokeFromRoadCells(
        IReadOnlyList<Vector2Int> cells,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd)
    {
        return _roadRuntimeGenerationSystem != null &&
            _roadRuntimeGenerationSystem.CreateAutobahnStrokeFromRoadCells(
                _roadRuntimeGenerationContext,
                cells,
                useAutobahnConnectorAtStart,
                useAutobahnConnectorAtEnd);
    }

    public bool CreateStandaloneStraightRoadChainFromConnector(
        Vector2Int connectorCell,
        Vector2Int direction,
        int length)
    {
        return _roadRuntimeGenerationSystem != null &&
            _roadRuntimeGenerationSystem.CreateStandaloneStraightRoadChainFromConnector(
                _roadRuntimeGenerationContext,
                connectorCell,
                direction,
                length);
    }

    public bool TryGetStandaloneStraightChainEndRoadCell(
        Vector2Int direction,
        out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;
        return _roadRuntimeGenerationSystem != null &&
            _roadRuntimeGenerationSystem.TryGetStandaloneStraightChainEndRoadCell(
                _roadRuntimeGenerationContext,
                direction,
                out roadConnectionCell);
    }

    private static bool TryGetGridCellSize(out float cellSize)
    {
        cellSize = 0f;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        cellSize = entityManager.GetComponentData<GridConfig>(query.GetSingletonEntity()).CellSize;
        return cellSize > 0f;
    }
}
