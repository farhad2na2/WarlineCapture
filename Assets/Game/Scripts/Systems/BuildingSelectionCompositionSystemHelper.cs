using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingSelectionCompositionSystemHelper
    {
        internal delegate bool TryGetGridForSelectionDelegate(
            BuildingGameplaySourceCompositionSystemHelper source,
            out GridConfig grid);

        public BuildingSelectionRuntimeCompositionSystemHelper.Context Create(
            BuildingGameplaySourceCompositionSystemHelper source,
            TryGetGridForSelectionDelegate tryGetGridForSelection,
            System.Func<GameObject, Sprite> resolveSelectionPortraitSpriteFromPrefab,
            System.Func<BuildingGameplaySourceCompositionSystemHelper, BuildingRuntimeContextFactoryCompositionSystemHelper.RuntimeSource> createRuntimeContextSource)
        {
            return source.BuildingSelectionRuntimeCompositionSystemHelper.CreateContext(new BuildingSelectionRuntimeCompositionSystemHelper.Source(
                source.RuntimeBuildingSystem,
                source.RuntimeBuildingSystem.Buildings,
                source.BuildingPlacementStartupSystemHelper.WorldCamera,
                (out GridConfig grid) => tryGetGridForSelection(source, out grid),
                (origin, footprint, grid) => source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint, grid, source.BuildingPlacementStartupSystemHelper.BuildPlaneY),
                () => source.RuntimeGameplayStateSystem.SuppressNextWorldClick = true,
                () => source.BuildingSelectionMarkerPresentationSystemHelper.Refresh(
                    source.BuildingRuntimeContextFactoryCompositionSystemHelper.CreateSelectionMarkerContext(
                        createRuntimeContextSource(source),
                        source.BuildingPlacementStartupSystemHelper.BuildingSelectionMarkerPrefab,
                        source.BuildingPlacementStartupSystemHelper.BuildingRoot,
                        null,
                        source.RuntimeObjectPresentationHelper.DestroyRuntimeObject)),
                source.BuildingGameplayDependencyCompositionSystemHelper.ClearFocusedUnit,
                building => source.BuildingGameplayDependencyCompositionSystemHelper.ShowHudSelection(
                    BuildingSelectionPortraitUiSystemHelper.Resolve(building, resolveSelectionPortraitSpriteFromPrefab)),
                source.BuildingGameplayDependencyCompositionSystemHelper.SmoothMoveCameraGroundCenterTo,
                source.BuildingGameplayDependencyCompositionSystemHelper.IsBoardablePlayerTransportClick,
                clickedBuildingId => source.BuildingRuntimeContextFactoryCompositionSystemHelper.TryAssignSelectedHaulerOrders(
                    createRuntimeContextSource(source),
                    clickedBuildingId),
                source.BuildingGameplayDependencyCompositionSystemHelper.TryRequestMoveOrderToBuilding,
                BuildingBarrierUtilitySystemHelper.ShouldUseExpandedSelectionArea));
        }
    }
}
