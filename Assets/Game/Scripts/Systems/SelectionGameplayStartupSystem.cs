using Game.Scripts.UI;
using UnityEngine;

internal sealed class SelectionGameplayStartupSystem
{
    public readonly struct Result
    {
        public readonly System.Action<MainMenuPlayUI> BindSelectionMainMenu;
        public readonly System.Action SelectionRuntimeUpdate;
        public readonly System.Action DisposeSelection;
        public readonly SelectionUiCommandSystem SelectionUiCommand;
        public readonly SelectionUiReadModelSystem SelectionUiReadModel;
        public readonly SelectionUiCameraSystem SelectionUiCamera;
        public readonly SelectionBuildingInteractionSystem SelectionBuildingInteraction;
        public readonly SelectionScreenMarkerSystem SelectionScreenMarkers;
        public readonly SelectionRectangleView SelectionRectangleView;

        public Result(
            System.Action<MainMenuPlayUI> bindSelectionMainMenu,
            System.Action selectionRuntimeUpdate,
            System.Action disposeSelection,
            SelectionUiCommandSystem selectionUiCommand,
            SelectionUiReadModelSystem selectionUiReadModel,
            SelectionUiCameraSystem selectionUiCamera,
            SelectionBuildingInteractionSystem selectionBuildingInteraction,
            SelectionScreenMarkerSystem selectionScreenMarkers,
            SelectionRectangleView selectionRectangleView)
        {
            BindSelectionMainMenu = bindSelectionMainMenu;
            SelectionRuntimeUpdate = selectionRuntimeUpdate;
            DisposeSelection = disposeSelection;
            SelectionUiCommand = selectionUiCommand;
            SelectionUiReadModel = selectionUiReadModel;
            SelectionUiCamera = selectionUiCamera;
            SelectionBuildingInteraction = selectionBuildingInteraction;
            SelectionScreenMarkers = selectionScreenMarkers;
            SelectionRectangleView = selectionRectangleView;
        }
    }

    public Result Initialize(
        RTSSelectionSystemConfig rtsSelectionConfig,
        Camera worldCamera,
        Transform runtimeUiRoot,
        RoadBuildSystem roadBuild,
        BuildingPlacementInteractionSystem buildingInteraction,
        BuildingPlacementInteractionSystem.Context buildingInteractionContext,
        FactionVisualSettings factionVisuals)
    {
        var selection = new SelectionRuntimeUpdateSystem();
        var selectionUiCommand = new SelectionUiCommandSystem();
        var selectionUiReadModel = new SelectionUiReadModelSystem();
        var rtsCamera = new RtsCameraSystem();
        var rtsCameraRequests = new RtsCameraRequestSystem();
        var selectionUiCamera = new SelectionUiCameraSystem(rtsCamera, rtsCameraRequests);
        var selectionState = new SelectionStateSystem();
        var selectionBuildingInteraction = new SelectionBuildingInteractionSystem();
        var selectionScreenMarkers = new SelectionScreenMarkerSystem();

        selectionUiCamera.Init(rtsSelectionConfig, worldCamera);
        selection.BindSelectionState(selectionState);
        selection.BindCameraBoundary(rtsCamera, rtsCameraRequests, selectionScreenMarkers);
        selectionBuildingInteraction.Init(selectionState, selectionScreenMarkers, worldCamera);
        selection.Init(
            rtsSelectionConfig,
            worldCamera,
            runtimeUiRoot,
            null,
            roadBuild,
            buildingInteraction,
            buildingInteractionContext,
            factionVisuals);

        return new Result(
            mainMenu => selection.BindDependencies(
                mainMenu,
                roadBuild,
                buildingInteraction,
                buildingInteractionContext),
            selection.Update,
            selection.Dispose,
            selectionUiCommand,
            selectionUiReadModel,
            selectionUiCamera,
            selectionBuildingInteraction,
            selectionScreenMarkers,
            EnsureSelectionRectangleView(runtimeUiRoot, rtsSelectionConfig));
    }

    private static SelectionRectangleView EnsureSelectionRectangleView(
        Transform runtimeUiRoot,
        RTSSelectionSystemConfig rtsSelectionConfig)
    {
        if (runtimeUiRoot == null)
            return null;

        SelectionRectangleView view = runtimeUiRoot.GetComponent<SelectionRectangleView>();
        if (view == null)
            view = runtimeUiRoot.gameObject.AddComponent<SelectionRectangleView>();

        view.ApplyConfig(rtsSelectionConfig);
        return view;
    }
}
