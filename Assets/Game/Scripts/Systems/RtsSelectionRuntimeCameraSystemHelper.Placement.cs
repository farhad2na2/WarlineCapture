using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    public sealed partial class RtsSelectionRuntimeCameraSystemHelper
    {
        private bool HoldCameraForPlacementPointer(Context context)
        {
            bool pending = context.BuildingPlacementInteractionCompositionSystemHelper != null &&
                context.BuildingPlacementInteractionCompositionSystemHelper.HasPendingBuildingPlacement(context.BuildingPlacementInteractionContext);
            if (!pending || !GamePointerInput.TryGetPrimaryPointer(out var pointer) ||
                !(pointer.IsPressed || pointer.WasReleasedThisFrame)) return false;
            // A placement gesture owns the camera for its entire press, including stationary holds.
            // Cancel the entry animation as well as pan so screen-to-ground mapping remains stable.
            if (context.TryGetDefaultEntityManager(out EntityManager em))
            {
                context.CameraRequestSystem.QueueClearSmoothFocusTarget(em);
                context.CameraRequestSystem.QueueSetZoomTransitionActive(em, false);
                context.CameraRequestSystem.QueueClearDragging(em);
                ProcessCameraRequests(context, em);
            }
            context.InputSystem.IsDraggingSelection = false;
            return true;
        }
        private void HandleBuildModeCameraPan(Context context)
        {
            Camera worldCamera = context.WorldCamera;
            RtsSelectionInputCompositionSystemHelper input = context.InputSystem;
            if (worldCamera == null)
                return;

            if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
                return;

            Vector2 pointerPosition = pointer.Position;
            context.UpdateLastKnownPointerPosition?.Invoke(pointerPosition);
            bool pointerOverGameplayUi = context.IsPointerOverGameplayUi?.Invoke(pointerPosition, out _) == true;
            bool pointerOverBuildToolMenu = context.MainMenuPlayUi != null && context.MainMenuPlayUi.IsPointerOverBuildToolMenu(pointerPosition);
            bool hasPendingBuildingPlacement = context.BuildingPlacementInteractionCompositionSystemHelper != null &&
                                               context.BuildingPlacementInteractionCompositionSystemHelper.HasPendingBuildingPlacement(context.BuildingPlacementInteractionContext);
            bool roadToolActive = context.RoadBuildReadModel != null && context.RoadBuildReadModel.IsRoadBuildModeActive;
            bool idleBuildMode = !hasPendingBuildingPlacement && !roadToolActive;
            bool interactionActive =
                (context.RoadBuildReadModel != null && context.RoadBuildReadModel.IsDraggingBuildInteraction) ||
                (context.BuildingPlacementInteractionCompositionSystemHelper != null &&
                 context.BuildingPlacementInteractionCompositionSystemHelper.IsDraggingPlacementPreview(context.BuildingPlacementInteractionContext));

            if (pointerOverGameplayUi)
            {
                SetCameraDragging(context, false);
                input.IsDraggingSelection = false;
                return;
            }

            bool panPressed = idleBuildMode && pointer.WasPressedThisFrame;
            bool panHeld = idleBuildMode && pointer.IsPressed;
            bool panReleased = idleBuildMode && pointer.WasReleasedThisFrame;

            if (panPressed)
            {
                input.LastPointerPosition = pointerPosition;
                SetCameraDragging(context, !interactionActive && !pointerOverBuildToolMenu);
            }

            if (panHeld && context.CameraSystem.IsDragging)
            {
                Vector2 frameDelta = pointerPosition - input.LastPointerPosition;
                if (frameDelta.sqrMagnitude > 0f)
                    PanCamera(context, frameDelta);
                input.LastPointerPosition = pointerPosition;
            }

            if (panReleased || !panHeld)
                SetCameraDragging(context, false);

            input.IsDraggingSelection = false;
        }

    }
}
