using System.Collections.Generic;
using UnityEngine;
using RoadTileData = RoadNetworkCompositionSystemHelper.RoadTileData;
using RoadVisualType = RoadNetworkCompositionSystemHelper.RoadVisualType;
using TileConnectionMask = RoadNetworkCompositionSystemHelper.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed class RoadVisualRefreshPresentationSystemHelper
{
    public readonly struct Context
    {
        public readonly RoadNetworkCompositionSystemHelper RoadNetworkCompositionSystemHelper;
        public readonly RoadGridProjectionSystem RoadGridProjectionSystem;
        public readonly RoadGridProjectionSystem.Context RoadGridProjectionContext;
        public readonly RoadChunkVisualSystem RoadChunkVisualSystem;
        public readonly RoadChunkVisualSystem.Context RoadChunkVisualContext;
        public readonly RoadSpecialVisualSystem RoadSpecialVisualSystem;
        public readonly RoadSpecialVisualSystem.Context RoadSpecialVisualContext;
        public readonly RoadVisualResolutionSystem.Context RoadVisualResolutionContext;

        public Context(
            RoadNetworkCompositionSystemHelper roadNetworkSystem,
            RoadGridProjectionSystem roadGridProjectionSystem,
            RoadGridProjectionSystem.Context roadGridProjectionContext,
            RoadChunkVisualSystem roadChunkVisualSystem,
            RoadChunkVisualSystem.Context roadChunkVisualContext,
            RoadSpecialVisualSystem roadSpecialVisualSystem,
            RoadSpecialVisualSystem.Context roadSpecialVisualContext,
            RoadVisualResolutionSystem.Context roadVisualResolutionContext)
        {
            RoadNetworkCompositionSystemHelper = roadNetworkSystem;
            RoadGridProjectionSystem = roadGridProjectionSystem;
            RoadGridProjectionContext = roadGridProjectionContext;
            RoadChunkVisualSystem = roadChunkVisualSystem;
            RoadChunkVisualContext = roadChunkVisualContext;
            RoadSpecialVisualSystem = roadSpecialVisualSystem;
            RoadSpecialVisualContext = roadSpecialVisualContext;
            RoadVisualResolutionContext = roadVisualResolutionContext;
        }
    }

    public static void RefreshCells(Context context, HashSet<Vector2Int> dirtyCells)
    {
        foreach (var cell in dirtyCells)
            RefreshCell(context, cell);

        context.RoadGridProjectionSystem?.RequestRoadEcsSync(context.RoadGridProjectionContext);
        context.RoadChunkVisualSystem?.RebuildDirtyChunks(context.RoadChunkVisualContext);
        RebuildSpecialRoadObjects(context, dirtyCells);
    }

    public static void RebuildRoadStateFromCurrentTiles(Context context)
    {
        context.RoadNetworkCompositionSystemHelper.RebuildSpecialRoadCellMetadata();

        context.RoadChunkVisualSystem?.ClearChunks();
        context.RoadSpecialVisualSystem?.ClearSpecialRoadObjects();

        foreach (var cell in context.RoadNetworkCompositionSystemHelper.RoadTiles.Keys)
            context.RoadChunkVisualSystem?.AddCellToChunk(context.RoadChunkVisualContext, cell);

        context.RoadGridProjectionSystem?.SyncRoadCellsToEcs(context.RoadGridProjectionContext);
        context.RoadChunkVisualSystem?.RebuildDirtyChunks(context.RoadChunkVisualContext);
        context.RoadSpecialVisualSystem?.RebuildSpecialRoadObjects(context.RoadSpecialVisualContext);
    }

    private static void RefreshCell(Context context, Vector2Int cell)
    {
        TileConnectionMask mask = context.RoadNetworkCompositionSystemHelper.GetMask(cell);
        RoadVisualType targetType = RoadVisualResolutionSystem.ResolveVisualType(
            context.RoadVisualResolutionContext,
            cell,
            mask);
        if (targetType == RoadVisualType.None)
        {
            context.RoadNetworkCompositionSystemHelper.RoadTiles.Remove(cell);
            context.RoadChunkVisualSystem?.RemoveCellFromChunk(context.RoadChunkVisualContext, cell);
            return;
        }

        if (!RoadVisualResolutionSystem.TryGetVariant(
                context.RoadVisualResolutionContext,
                targetType,
                mask,
                out VariantData variant))
        {
            return;
        }

        if (context.RoadNetworkCompositionSystemHelper.RoadTiles.TryGetValue(cell, out RoadTileData current) &&
            current.Type == targetType &&
            current.Mask.Equals(mask) &&
            current.Rotation == variant.Rotation &&
            current.Scale == variant.Scale)
        {
            return;
        }

        context.RoadNetworkCompositionSystemHelper.RoadTiles[cell] = new RoadTileData
        {
            Type = targetType,
            Mask = mask,
            Rotation = variant.Rotation,
            Scale = variant.Scale
        };

        context.RoadChunkVisualSystem?.AddCellToChunk(context.RoadChunkVisualContext, cell);
    }

    private static void RebuildSpecialRoadObjects(Context context, HashSet<Vector2Int> dirtyCells)
    {
        context.RoadSpecialVisualSystem?.RebuildSpecialRoadObjects(context.RoadSpecialVisualContext);
    }
}
