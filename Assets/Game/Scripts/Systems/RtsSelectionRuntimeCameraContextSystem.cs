using System;
using UnityEngine;

public sealed class RtsSelectionRuntimeCameraContextSystem
{
    public RtsSelectionRuntimeCameraSystem.Context Create(
        RuntimeGameplayStateSystem runtimeGameplayStateSystem,
        RtsSelectionInputSystem inputSystem,
        RtsCameraSystem cameraSystem,
        RtsCameraRequestSystem cameraRequestSystem,
        SelectionRuntimeConfigSystem.State runtimeConfig,
        MainMenuPlayUI mainMenuPlayUi,
        RoadBuildReadModelSystem roadBuildReadModel,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        RtsSelectionRuntimeCameraSystem.TryGetEntityManagerAction tryGetDefaultEntityManager,
        RtsSelectionRuntimeCameraSystem.IsPointerOverGameplayUiAction isPointerOverGameplayUi,
        Action<Vector2> updateLastKnownPointerPosition,
        Action hideOrderScreenMarkers)
    {
        return new RtsSelectionRuntimeCameraSystem.Context(
            runtimeGameplayStateSystem,
            inputSystem,
            cameraSystem,
            cameraRequestSystem,
            runtimeConfig.WorldCamera,
            mainMenuPlayUi,
            roadBuildReadModel,
            buildingPlacementInteractionSystem,
            buildingPlacementInteractionContext,
            tryGetDefaultEntityManager,
            isPointerOverGameplayUi,
            updateLastKnownPointerPosition,
            hideOrderScreenMarkers,
            runtimeConfig.PanSensitivity,
            runtimeConfig.ZoomSpeed,
            runtimeConfig.MinZoomHeight,
            runtimeConfig.MaxZoomHeight,
            runtimeConfig.NormalModeZoomHeight,
            runtimeConfig.BuildModeZoomHeight,
            runtimeConfig.NormalModePitch,
            runtimeConfig.BuildModePitch,
            runtimeConfig.NormalModeYaw,
            runtimeConfig.BuildModeYaw,
            runtimeConfig.NormalModeFieldOfView,
            runtimeConfig.BuildModeFieldOfView,
            runtimeConfig.FullscreenIsoZoomHeight,
            runtimeConfig.FullscreenIsoPitch,
            runtimeConfig.FullscreenIsoYaw,
            runtimeConfig.FullscreenIsoOrthographicSize,
            runtimeConfig.ZoomTransitionSmoothTime);
    }
}
