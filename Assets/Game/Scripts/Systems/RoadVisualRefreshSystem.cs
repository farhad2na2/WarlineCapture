using System.Collections.Generic;
using UnityEngine;
using RoadTileData = RoadNetworkSystem.RoadTileData;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed class RoadVisualRefreshSystem
{
    public readonly struct Context
    {
        public readonly RoadNetworkSystem RoadNetworkSystem;
        public readonly RoadGridProjectionSystem RoadGridProjectionSystem;
        public readonly RoadGridProjectionSystem.Context RoadGridProjectionContext;
        public readonly RoadChunkVisualSystem RoadChunkVisualSystem;
        public readonly RoadChunkVisualSystem.Context RoadChunkVisualContext;
        public readonly RoadSpecialVisualSystem RoadSpecialVisualSystem;
        public readonly RoadSpecialVisualSystem.Context RoadSpecialVisualContext;
        public readonly RoadVisualResolutionSystem RoadVisualResolutionSystem;
        public readonly RoadVisualResolutionSystem.Context RoadVisualResolutionContext;

        public Context(
            RoadNetworkSystem roadNetworkSystem,
            RoadGridProjectionSystem roadGridProjectionSystem,
            RoadGridProjectionSystem.Context roadGridProjectionContext,
            RoadChunkVisualSystem roadChunkVisualSystem,
            RoadChunkVisualSystem.Context roadChunkVisualContext,
            RoadSpecialVisualSystem roadSpecialVisualSystem,
            RoadSpecialVisualSystem.Context roadSpecialVisualContext,
            RoadVisualResolutionSystem roadVisualResolutionSystem,
            RoadVisualResolutionSystem.Context roadVisualResolutionContext)
        {
            RoadNetworkSystem = roadNetworkSystem;
            RoadGridProjectionSystem = roadGridProjectionSystem;
            RoadGridProjectionContext = roadGridProjectionContext;
            RoadChunkVisualSystem = roadChunkVisualSystem;
            RoadChunkVisualContext = roadChunkVisualContext;
            RoadSpecialVisualSystem = roadSpecialVisualSystem;
            RoadSpecialVisualContext = roadSpecialVisualContext;
            RoadVisualResolutionSystem = roadVisualResolutionSystem;
            RoadVisualResolutionContext = roadVisualResolutionContext;
        }
    }

    public void RefreshCells(Context context, HashSet<Vector2Int> dirtyCells)
    {
        foreach (var cell in dirtyCells)
            RefreshCell(context, cell);

        context.RoadGridProjectionSystem.RequestRoadEcsSync(context.RoadGridProjectionContext);
        context.RoadChunkVisualSystem.RebuildDirtyChunks(context.RoadChunkVisualContext);
        RebuildSpecialRoadObjects(context, dirtyCells);
    }

    public void RebuildRoadStateFromCurrentTiles(Context context)
    {
        context.RoadNetworkSystem.RebuildSpecialRoadCellMetadata();

        context.RoadChunkVisualSystem.ClearChunks();
        context.RoadSpecialVisualSystem.ClearSpecialRoadObjects();

        foreach (var cell in context.RoadNetworkSystem.RoadTiles.Keys)
            context.RoadChunkVisualSystem.AddCellToChunk(context.RoadChunkVisualContext, cell);

        context.RoadGridProjectionSystem.SyncRoadCellsToEcs(context.RoadGridProjectionContext);
        context.RoadChunkVisualSystem.RebuildDirtyChunks(context.RoadChunkVisualContext);
        context.RoadSpecialVisualSystem.RebuildSpecialRoadObjects(context.RoadSpecialVisualContext);
    }

    private void RefreshCell(Context context, Vector2Int cell)
    {
        TileConnectionMask mask = context.RoadNetworkSystem.GetMask(cell);
        RoadVisualType targetType = context.RoadVisualResolutionSystem.ResolveVisualType(
            context.RoadVisualResolutionContext,
            cell,
            mask);
        if (targetType == RoadVisualType.None)
        {
            context.RoadNetworkSystem.RoadTiles.Remove(cell);
            context.RoadChunkVisualSystem.RemoveCellFromChunk(context.RoadChunkVisualContext, cell);
            return;
        }

        if (!context.RoadVisualResolutionSystem.TryGetVariant(
                context.RoadVisualResolutionContext,
                targetType,
                mask,
                out VariantData variant))
        {
            return;
        }

        if (context.RoadNetworkSystem.RoadTiles.TryGetValue(cell, out RoadTileData current) &&
            current.Type == targetType &&
            current.Mask.Equals(mask) &&
            current.Rotation == variant.Rotation &&
            current.Scale == variant.Scale)
        {
            return;
        }

        context.RoadNetworkSystem.RoadTiles[cell] = new RoadTileData
        {
            Type = targetType,
            Mask = mask,
            Rotation = variant.Rotation,
            Scale = variant.Scale
        };

        context.RoadChunkVisualSystem.AddCellToChunk(context.RoadChunkVisualContext, cell);
    }

    private void RebuildSpecialRoadObjects(Context context, HashSet<Vector2Int> dirtyCells)
    {
        context.RoadSpecialVisualSystem.RebuildSpecialRoadObjects(context.RoadSpecialVisualContext);
    }
}
