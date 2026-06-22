using System.Collections.Generic;
using UnityEngine;
using CombinedRoadVisualData = RoadGridProjectionSystem.CombinedRoadVisualData;
using MarkerLayoutData = RoadVisualVariantSystem.MarkerLayoutData;
using RoadVisualType = RoadNetworkSystem.RoadVisualType;
using TileConnectionMask = RoadNetworkSystem.TileConnectionMask;
using VariantData = RoadVisualVariantSystem.VariantData;

internal sealed class RoadBuildVisualContextSystem
{
    public readonly struct Context
    {
        public readonly RoadNetworkSystem RoadNetworkSystem;
        public readonly RoadPathPlanningSystem RoadPathPlanningSystem;
        public readonly RoadVisualVariantSystem RoadVisualVariantSystem;
        public readonly RoadBuildStartupSystem RoadBuildStartupSystem;
        public readonly RoadBuildStartupSystem.State StartupState;
        public readonly RoadPreviewSystem.ResolveVisualTypeAction ResolveVisualType;
        public readonly RoadPreviewSystem.TryGetVariantAction PreviewTryGetVariant;
        public readonly RoadSpecialVisualSystem.GetPrefabAction GetPrefab;
        public readonly RoadSpecialVisualSystem.TryGetVariantAction SpecialTryGetVariant;

        public Context(
            RoadNetworkSystem roadNetworkSystem,
            RoadPathPlanningSystem roadPathPlanningSystem,
            RoadVisualVariantSystem roadVisualVariantSystem,
            RoadBuildStartupSystem roadBuildStartupSystem,
            RoadBuildStartupSystem.State startupState,
            RoadPreviewSystem.ResolveVisualTypeAction resolveVisualType,
            RoadPreviewSystem.TryGetVariantAction previewTryGetVariant,
            RoadSpecialVisualSystem.GetPrefabAction getPrefab,
            RoadSpecialVisualSystem.TryGetVariantAction specialTryGetVariant)
        {
            RoadNetworkSystem = roadNetworkSystem;
            RoadPathPlanningSystem = roadPathPlanningSystem;
            RoadVisualVariantSystem = roadVisualVariantSystem;
            RoadBuildStartupSystem = roadBuildStartupSystem;
            StartupState = startupState;
            ResolveVisualType = resolveVisualType;
            PreviewTryGetVariant = previewTryGetVariant;
            GetPrefab = getPrefab;
            SpecialTryGetVariant = specialTryGetVariant;
        }
    }

    public static GameObject GetPrefab(Context context, RoadVisualType type)
    {
        return context.RoadVisualVariantSystem == null
            ? null
            : context.RoadVisualVariantSystem.GetPrefab(
                context.RoadBuildStartupSystem.CreateRoadPrefabSet(context.StartupState),
                type);
    }

    public static RoadChunkVisualSystem.Context CreateChunkContext(Context context)
    {
        RoadRuntimeRootSystem.Roots roots = context.StartupState.RuntimeRoots;
        return new RoadChunkVisualSystem.Context(
            context.RoadNetworkSystem.RoadTiles,
            context.RoadVisualVariantSystem?.VisualData ?? new Dictionary<RoadVisualType, CombinedRoadVisualData>(),
            context.RoadNetworkSystem.AutobahnCells,
            context.RoadNetworkSystem.AutobahnConnectorCells,
            roots.RoadRoot,
            context.StartupState.GridOrigin,
            context.StartupState.BuildPlaneY,
            context.StartupState.RoadGridSize,
            context.StartupState.ChunkSizeInCells);
    }

    public static RoadPreviewSystem.Context CreatePreviewContext(Context context)
    {
        RoadBuildStartupSystem.State startupState = context.StartupState;
        RoadRuntimeRootSystem.Roots roots = startupState.RuntimeRoots;
        return new RoadPreviewSystem.Context(
            context.RoadVisualVariantSystem?.VisualData ?? new Dictionary<RoadVisualType, CombinedRoadVisualData>(),
            roots.RoadRoot,
            startupState.GridOrigin,
            startupState.BuildPlaneY,
            startupState.RoadGridSize,
            startupState.PreviewAlpha,
            startupState.EndPrefab,
            context.RoadPathPlanningSystem,
            context.RoadNetworkSystem,
            context.ResolveVisualType,
            context.PreviewTryGetVariant);
    }

    public static RoadSpecialVisualSystem.Context CreateSpecialContext(Context context)
    {
        RoadBuildStartupSystem.State startupState = context.StartupState;
        RoadRuntimeRootSystem.Roots roots = startupState.RuntimeRoots;
        return new RoadSpecialVisualSystem.Context(
            context.RoadNetworkSystem.RoadTiles,
            context.RoadNetworkSystem.Strokes,
            context.RoadVisualVariantSystem?.MarkerLayouts ?? new Dictionary<RoadVisualType, MarkerLayoutData>(),
            context.RoadVisualVariantSystem?.AutobahnConnectorMarkerData,
            roots.RoadRoot,
            roots.SpecialRoadRoot,
            roots.SpecialRoadConnectorRoot,
            roots.DebugStraightRoadRoot,
            startupState.GridOrigin,
            startupState.BuildPlaneY,
            startupState.RoadGridSize,
            startupState.ChunkSizeInCells,
            context.GetPrefab,
            context.SpecialTryGetVariant);
    }
}
