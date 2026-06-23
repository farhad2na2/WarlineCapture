using UnityEngine;

internal sealed class BuildingSelectionCompositionSystemHelper
{
    internal delegate bool TryGetGridForSelectionDelegate(
        BuildingGameplaySourceCompositionSystemHelper source,
        out GridConfig grid);

    public BuildingSelectionSystem.Context Create(
        BuildingGameplaySourceCompositionSystemHelper source,
        TryGetGridForSelectionDelegate tryGetGridForSelection,
        System.Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab,
        System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource)
    {
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
                    source.RuntimeObjectPresentationHelper.DestroyRuntimeObject)),
            source.BuildingGameplayDependencySystem.ClearFocusedUnit,
            building => source.BuildingGameplayDependencySystem.ShowHudSelection(
                BuildingSelectionPortraitUiSystemHelper.Resolve(building, resolveSelectionPortraitSpriteFromPrefab)),
            source.BuildingGameplayDependencySystem.SmoothMoveCameraGroundCenterTo,
            source.BuildingGameplayDependencySystem.IsBoardablePlayerTransportClick,
            clickedBuildingId => source.BuildingRuntimeContextSystem.TryAssignSelectedHaulerOrders(
                createRuntimeContextSource(source),
                clickedBuildingId),
            source.BuildingGameplayDependencySystem.TryRequestMoveOrderToBuilding,
            BuildingBarrierSystem.ShouldUseExpandedSelectionArea));
    }
}
