using System.Collections.Generic;
using UnityEngine;

internal sealed class RuntimeCityRoadBuildBridgeSystem
{
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
        return _roadRuntimeGenerationSystem != null &&
            _roadRuntimeGenerationSystem.TryGetRoadCellSizeInGridCells(
                _roadRuntimeGenerationContext,
                out roadCellSizeInGridCells);
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
}
