using System.Collections.Generic;
using UnityEngine;

internal sealed class RuntimeCityRoadBuildBridgeSystem
{
    private RoadBuildSystem _roadBuildSystem;

    public bool HasRoadBuildSystem => _roadBuildSystem != null;

    public void Configure(RoadBuildSystem roadBuildSystem)
    {
        _roadBuildSystem = roadBuildSystem;
    }

    public void Clear()
    {
        _roadBuildSystem = null;
    }

    public bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
    {
        roadCellSizeInGridCells = 0;
        return _roadBuildSystem != null &&
            _roadBuildSystem.TryGetRoadCellSizeInGridCells(out roadCellSizeInGridCells);
    }

    public void BeginDeferredRoadEcsSync()
    {
        _roadBuildSystem?.BeginDeferredRoadEcsSync();
    }

    public void EndDeferredRoadEcsSync()
    {
        _roadBuildSystem?.EndDeferredRoadEcsSync();
    }

    public bool CreateRoadStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
    {
        return _roadBuildSystem != null &&
            _roadBuildSystem.CreateRoadStrokeFromRoadCells(cells);
    }

    public bool CreateAutobahnStrokeFromRoadCells(
        IReadOnlyList<Vector2Int> cells,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd)
    {
        return _roadBuildSystem != null &&
            _roadBuildSystem.CreateAutobahnStrokeFromRoadCells(
                cells,
                useAutobahnConnectorAtStart,
                useAutobahnConnectorAtEnd);
    }

    public bool CreateStandaloneStraightRoadChainFromConnector(
        Vector2Int connectorCell,
        Vector2Int direction,
        int length)
    {
        return _roadBuildSystem != null &&
            _roadBuildSystem.CreateStandaloneStraightRoadChainFromConnector(
                connectorCell,
                direction,
                length);
    }

    public bool TryGetStandaloneStraightChainEndRoadCell(
        Vector2Int direction,
        out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;
        return _roadBuildSystem != null &&
            _roadBuildSystem.TryGetStandaloneStraightChainEndRoadCell(
                direction,
                out roadConnectionCell);
    }
}
