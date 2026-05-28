using UnityEngine;

internal sealed class BuildingSelectionCompositionSystem
{
    internal delegate bool TryGetGridForSelectionDelegate(
        BuildingGameplayCompositionSourceSystem source,
        out GridConfig grid);

    public BuildingSelectionSystem.Context Create(
        BuildingGameplayCompositionSourceSystem source,
        TryGetGridForSelectionDelegate tryGetGridForSelection,
        System.Func<BuildingGameplayCompositionSourceSystem, BuildingRuntimeContextSystem.RuntimeSource> createRuntimeContextSource)
    {
        return source.BuildingSelectionSystem.CreateContext(new BuildingSelectionSystem.Source(
            source.RuntimeBuildingSystem,
            source.RuntimeBuildingSystem.Buildings,
            (out GridConfig grid) => tryGetGridForSelection(source, out grid),
            (origin, footprint, grid) => source.BuildingPlacementGridSystem.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystem.BuildPlaneY),
            () => source.RuntimeGameplayStateSystem.SuppressNextWorldClick = true,
            () => source.BuildingRuntimeVisualSystem.RefreshBuildingMarkerVisibility(
                source.BuildingRuntimeContextSystem.CreateRuntimeVisualContext(createRuntimeContextSource(source))),
            source.BuildingGameplayDependencySystem.ClearFocusedUnit,
            source.BuildingGameplayDependencySystem.SmoothMoveCameraGroundCenterTo,
            source.BuildingGameplayDependencySystem.IsBoardablePlayerTransportClick,
            clickedBuildingId => source.BuildingRuntimeContextSystem.TryAssignSelectedHaulerOrders(
                createRuntimeContextSource(source),
                clickedBuildingId),
            source.BuildingGameplayDependencySystem.TryIssueMoveOrderToBuilding,
            BuildingBarrierSystem.ShouldUseExpandedSelectionArea));
    }
}
