using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

internal sealed partial class RoadRuntimeGenerationSystem : SystemBase
{
    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public delegate bool TryGetRoadCellSizeInGridCellsDelegate(out int roadCellSizeInGridCells);
    public delegate void RuntimeAction();
    public delegate void CreateStrokeDelegate(
        List<Vector2Int> cells,
        bool isAutobahn,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd);

    public readonly struct Context
    {
        public readonly TryGetRoadCellSizeInGridCellsDelegate TryGetRoadCellSizeInGridCells;
        public readonly RuntimeAction BeginDeferredRoadEcsSync;
        public readonly RuntimeAction EndDeferredRoadEcsSync;
        public readonly CreateStrokeDelegate CreateStroke;
        public readonly RoadSpecialVisualSystem SpecialVisualSystem;
        public readonly RoadSpecialVisualSystem.Context SpecialVisualContext;

        public Context(
            TryGetRoadCellSizeInGridCellsDelegate tryGetRoadCellSizeInGridCells,
            RuntimeAction beginDeferredRoadEcsSync,
            RuntimeAction endDeferredRoadEcsSync,
            CreateStrokeDelegate createStroke,
            RoadSpecialVisualSystem specialVisualSystem,
            RoadSpecialVisualSystem.Context specialVisualContext)
        {
            TryGetRoadCellSizeInGridCells = tryGetRoadCellSizeInGridCells;
            BeginDeferredRoadEcsSync = beginDeferredRoadEcsSync;
            EndDeferredRoadEcsSync = endDeferredRoadEcsSync;
            CreateStroke = createStroke;
            SpecialVisualSystem = specialVisualSystem;
            SpecialVisualContext = specialVisualContext;
        }
    }

    public bool TryGetRoadCellSizeInGridCells(Context context, out int roadCellSizeInGridCells)
    {
        roadCellSizeInGridCells = 0;
        return context.TryGetRoadCellSizeInGridCells != null &&
               context.TryGetRoadCellSizeInGridCells(out roadCellSizeInGridCells);
    }

    public void BeginDeferredRoadEcsSync(Context context)
    {
        context.BeginDeferredRoadEcsSync?.Invoke();
    }

    public void EndDeferredRoadEcsSync(Context context)
    {
        context.EndDeferredRoadEcsSync?.Invoke();
    }

    public bool CreateRoadStrokeFromRoadCells(Context context, IReadOnlyList<Vector2Int> cells)
    {
        if (!TryCopyPath(cells, minimumCellCount: 2, out List<Vector2Int> path))
            return false;

        context.CreateStroke?.Invoke(
            path,
            isAutobahn: false,
            useAutobahnConnectorAtStart: false,
            useAutobahnConnectorAtEnd: false);
        return context.CreateStroke != null;
    }

    public bool CreateAutobahnStrokeFromRoadCells(Context context, IReadOnlyList<Vector2Int> cells)
    {
        return CreateAutobahnStrokeFromRoadCells(
            context,
            cells,
            useAutobahnConnectorAtStart: true,
            useAutobahnConnectorAtEnd: false);
    }

    public bool CreateAutobahnStrokeFromRoadCells(
        Context context,
        IReadOnlyList<Vector2Int> cells,
        bool useAutobahnConnectorAtStart,
        bool useAutobahnConnectorAtEnd)
    {
        if (!TryCopyPath(cells, minimumCellCount: 3, out List<Vector2Int> path))
            return false;

        context.CreateStroke?.Invoke(
            path,
            isAutobahn: true,
            useAutobahnConnectorAtStart,
            useAutobahnConnectorAtEnd);
        return context.CreateStroke != null;
    }

    public bool TryGetAutobahnConnectorRoadCell(
        Context context,
        Vector2Int connectorCell,
        out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;
        return context.SpecialVisualSystem != null &&
               context.SpecialVisualSystem.TryGetAutobahnConnectorRoadCell(
                   context.SpecialVisualContext,
                   connectorCell,
                   out roadConnectionCell);
    }

    public bool TryLogRoadConnectMarkers(Context context, Vector2Int roadCell)
    {
        return context.SpecialVisualSystem != null &&
               context.SpecialVisualSystem.TryLogRoadConnectMarkers(context.SpecialVisualContext, roadCell);
    }

    public bool CreateStandaloneStraightRoadChainFromConnector(
        Context context,
        Vector2Int connectorCell,
        Vector2Int direction,
        int length)
    {
        return context.SpecialVisualSystem != null &&
               context.SpecialVisualSystem.CreateStandaloneStraightRoadChainFromConnector(
                   context.SpecialVisualContext,
                   connectorCell,
                   direction,
                   length);
    }

    public bool TryGetStandaloneStraightChainEndRoadCell(
        Context context,
        Vector2Int direction,
        out Vector2Int roadConnectionCell)
    {
        roadConnectionCell = default;
        return context.SpecialVisualSystem != null &&
               context.SpecialVisualSystem.TryGetStandaloneStraightChainEndRoadCell(
                   context.SpecialVisualContext,
                   direction,
                   out roadConnectionCell);
    }

    public bool CreateStandaloneDebugCityRoadNetworkFromStraightChain(
        Context context,
        Vector2Int direction,
        int branchLength)
    {
        return context.SpecialVisualSystem != null &&
               context.SpecialVisualSystem.CreateStandaloneDebugCityRoadNetworkFromStraightChain(
                   context.SpecialVisualContext,
                   direction,
                   branchLength);
    }

    private static bool TryCopyPath(
        IReadOnlyList<Vector2Int> cells,
        int minimumCellCount,
        out List<Vector2Int> path)
    {
        path = null;
        if (cells == null || cells.Count < minimumCellCount)
            return false;

        path = new List<Vector2Int>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
            path.Add(cells[i]);
        return true;
    }
}
