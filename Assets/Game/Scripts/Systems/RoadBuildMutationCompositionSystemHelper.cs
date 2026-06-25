using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class RoadBuildMutationCompositionSystemHelper
{
    public readonly struct Context
    {
        public readonly RoadNetworkCompositionSystemHelper RoadNetworkCompositionSystemHelper;
        public readonly Action<HashSet<Vector2Int>> RefreshCells;
        public readonly Action RebuildRoadStateFromCurrentTiles;

        public Context(
            RoadNetworkCompositionSystemHelper roadNetworkSystem,
            Action<HashSet<Vector2Int>> refreshCells,
            Action rebuildRoadStateFromCurrentTiles)
        {
            RoadNetworkCompositionSystemHelper = roadNetworkSystem;
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
        if (context.RoadNetworkCompositionSystemHelper == null)
            return;

        if (context.RoadNetworkCompositionSystemHelper.CreateStroke(
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
        if (context.RoadNetworkCompositionSystemHelper == null)
            return;

        if (context.RoadNetworkCompositionSystemHelper.DeleteStroke(strokeId, out var dirtyCells))
            context.RefreshCells?.Invoke(dirtyCells);
    }

    public RoadNetworkCompositionSystemHelper.Snapshot CaptureRoadBuildSessionSnapshot(Context context)
    {
        return context.RoadNetworkCompositionSystemHelper?.CaptureSnapshot();
    }

    public void RestoreRoadBuildSession(Context context, RoadNetworkCompositionSystemHelper.Snapshot snapshot)
    {
        if (context.RoadNetworkCompositionSystemHelper == null)
            return;

        context.RoadNetworkCompositionSystemHelper.RestoreSnapshot(snapshot);
        context.RebuildRoadStateFromCurrentTiles?.Invoke();
    }
}
