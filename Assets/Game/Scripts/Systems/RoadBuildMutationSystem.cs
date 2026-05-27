using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class RoadBuildMutationSystem
{
    public readonly struct Context
    {
        public readonly RoadNetworkSystem RoadNetworkSystem;
        public readonly Action<HashSet<Vector2Int>> RefreshCells;
        public readonly Action RebuildRoadStateFromCurrentTiles;

        public Context(
            RoadNetworkSystem roadNetworkSystem,
            Action<HashSet<Vector2Int>> refreshCells,
            Action rebuildRoadStateFromCurrentTiles)
        {
            RoadNetworkSystem = roadNetworkSystem;
            RefreshCells = refreshCells;
            RebuildRoadStateFromCurrentTiles = rebuildRoadStateFromCurrentTiles;
        }
    }

    public void CreateStroke(
        Context context,
        List<Vector2Int> cells,
        bool isAutobahn = false,
        bool useAutobahnConnectorAtStart = false,
        bool useAutobahnConnectorAtEnd = false)
    {
        if (context.RoadNetworkSystem == null)
            return;

        if (context.RoadNetworkSystem.CreateStroke(
                cells,
                isAutobahn,
                useAutobahnConnectorAtStart,
                useAutobahnConnectorAtEnd,
                out var dirtyCells))
        {
            context.RefreshCells?.Invoke(dirtyCells);
        }
    }

    public void DeleteStroke(Context context, int strokeId)
    {
        if (context.RoadNetworkSystem == null)
            return;

        if (context.RoadNetworkSystem.DeleteStroke(strokeId, out var dirtyCells))
            context.RefreshCells?.Invoke(dirtyCells);
    }

    public RoadNetworkSystem.Snapshot CaptureRoadBuildSessionSnapshot(Context context)
    {
        return context.RoadNetworkSystem?.CaptureSnapshot();
    }

    public void RestoreRoadBuildSession(Context context, RoadNetworkSystem.Snapshot snapshot)
    {
        if (context.RoadNetworkSystem == null)
            return;

        context.RoadNetworkSystem.RestoreSnapshot(snapshot);
        context.RebuildRoadStateFromCurrentTiles?.Invoke();
    }
}
