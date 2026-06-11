using UnityEngine;

internal sealed class BuildingSelectionCompositionSystem
{
    internal delegate bool TryGetGridForSelectionDelegate(
        BuildingGameplayCompositionSourceSystem source,
        out GridConfig grid);

    public BuildingSelectionSystem.Context Create(
        BuildingGameplayCompositionSourceSystem source,
        TryGetGridForSelectionDelegate tryGetGridForSelection,
        System.Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource)
    {
        Sprite ResolveSelectionPortraitSprite(RuntimeBuildingEntity building)
        {
            if (building == null)
                return null;

            Sprite sprite = resolveSelectionPortraitSpriteFromPrefab?.Invoke(building.Definition?.Prefab);
            return sprite != null
                ? sprite
                : resolveSelectionPortraitSpriteFromPrefab?.Invoke(building.Instance);
        }

        return source.BuildingSelectionSystem.CreateContext(new BuildingSelectionSystem.Source(
            source.RuntimeBuildingSystem,
            source.RuntimeBuildingSystem.Buildings,
            source.BuildingPlacementStartupSystem.WorldCamera,
            (out GridConfig grid) => tryGetGridForSelection(source, out grid),
            (origin, footprint, grid) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystem.BuildPlaneY),
            () => source.RuntimeGameplayStateSystem.SuppressNextWorldClick = true,
            () => source.BuildingSelectionMarkerSystem.Refresh(
                source.BuildingRuntimeContextSystem.CreateSelectionMarkerContext(
                    createRuntimeContextSource(source),
                    source.BuildingPlacementStartupSystem.BuildingSelectionMarkerPrefab,
                    source.BuildingPlacementStartupSystem.BuildingRoot,
                    null,
                    source.BuildingRuntimeObjectSystem.DestroyRuntimeObject)),
            source.BuildingGameplayDependencySystem.ClearFocusedUnit,
            building => source.BuildingGameplayDependencySystem.ShowHudSelection(
                ResolveSelectionPortraitSprite(building)),
            source.BuildingGameplayDependencySystem.SmoothMoveCameraGroundCenterTo,
            source.BuildingGameplayDependencySystem.IsBoardablePlayerTransportClick,
            clickedBuildingId => source.BuildingRuntimeContextSystem.TryAssignSelectedHaulerOrders(
                createRuntimeContextSource(source),
                clickedBuildingId),
            source.BuildingGameplayDependencySystem.TryIssueMoveOrderToBuilding,
            BuildingBarrierSystem.ShouldUseExpandedSelectionArea));
    }
}
