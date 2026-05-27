using System.Collections.Generic;
using UnityEngine;
using RoadTileData = RoadNetworkSystem.RoadTileData;

internal sealed class RoadBuildDisposalSystem
{
    public readonly struct Context
    {
        public readonly RoadBuildStartupSystem StartupSystem;
        public readonly RoadBuildStartupSystem.State StartupState;
        public readonly RoadRuntimeRootSystem RuntimeRootSystem;
        public readonly BuildingRoadLegacyPlacementVisualSystem PlacementVisualSystem;
        public readonly BuildingRoadLegacyPlacementVisualSystem.State PlacementVisualState;
        public readonly RoadVisualVariantSystem VisualVariantSystem;
        public readonly RoadPreviewSystem PreviewSystem;
        public readonly RoadChunkVisualSystem ChunkVisualSystem;
        public readonly BuildingRoadLegacyEcsSystem LegacyEcsSystem;
        public readonly BuildingRoadLegacyStorageSystem LegacyStorageSystem;
        public readonly RoadSpecialVisualSystem SpecialVisualSystem;
        public readonly RoadMinimapEventSystem MinimapEventSystem;
        public readonly RoadGridProjectionSystem GridProjectionSystem;
        public readonly IDictionary<Vector2Int, RoadTileData> RoadTiles;

        public Context(
            RoadBuildStartupSystem startupSystem,
            RoadBuildStartupSystem.State startupState,
            RoadRuntimeRootSystem runtimeRootSystem,
            BuildingRoadLegacyPlacementVisualSystem placementVisualSystem,
            BuildingRoadLegacyPlacementVisualSystem.State placementVisualState,
            RoadVisualVariantSystem visualVariantSystem,
            RoadPreviewSystem previewSystem,
            RoadChunkVisualSystem chunkVisualSystem,
            BuildingRoadLegacyEcsSystem legacyEcsSystem,
            BuildingRoadLegacyStorageSystem legacyStorageSystem,
            RoadSpecialVisualSystem specialVisualSystem,
            RoadMinimapEventSystem minimapEventSystem,
            RoadGridProjectionSystem gridProjectionSystem,
            IDictionary<Vector2Int, RoadTileData> roadTiles)
        {
            StartupSystem = startupSystem;
            StartupState = startupState;
            RuntimeRootSystem = runtimeRootSystem;
            PlacementVisualSystem = placementVisualSystem;
            PlacementVisualState = placementVisualState;
            VisualVariantSystem = visualVariantSystem;
            PreviewSystem = previewSystem;
            ChunkVisualSystem = chunkVisualSystem;
            LegacyEcsSystem = legacyEcsSystem;
            LegacyStorageSystem = legacyStorageSystem;
            SpecialVisualSystem = specialVisualSystem;
            MinimapEventSystem = minimapEventSystem;
            GridProjectionSystem = gridProjectionSystem;
            RoadTiles = roadTiles;
        }
    }

    public void Dispose(Context context)
    {
        context.StartupSystem.DisposeRuntimeRoots(context.StartupState, context.RuntimeRootSystem);
        context.PlacementVisualSystem.Dispose(context.PlacementVisualState);
        context.VisualVariantSystem.DisposeCachedVisualData();
        context.PreviewSystem.DisposePreview();
        context.ChunkVisualSystem.DisposeChunks();
        context.LegacyEcsSystem.DisposeRuntimeBuildings(context.LegacyStorageSystem.RuntimeBuildings);
        context.SpecialVisualSystem.DisposeVisuals();
        context.MinimapEventSystem.Clear();
        context.GridProjectionSystem.ClearRoadDataInEcs();
        context.RoadTiles.Clear();
        context.LegacyStorageSystem.Clear();
    }
}
