using System;
using Unity.Entities;
using UnityEngine;

public sealed class RtsSelectionRuntimeCameraSystem
{
    public delegate bool TryGetEntityManagerAction(out EntityManager em);
    public delegate bool IsPointerOverGameplayUiAction(Vector2 screenPosition, out string source);

    public readonly struct Context
    {
        public readonly RuntimeGameplayStateSystem RuntimeGameplayStateSystem;
        public readonly RtsSelectionInputSystem InputSystem;
        public readonly RtsCameraSystem CameraSystem;
        public readonly RtsCameraRequestSystem CameraRequestSystem;
        public readonly Camera WorldCamera;
        public readonly MainMenuPlayUI MainMenuPlayUi;
        public readonly RoadBuildSystem RoadBuildSystem;
        public readonly BuildingPlacementInteractionSystem BuildingPlacementInteractionSystem;
        public readonly BuildingPlacementInteractionSystem.Context BuildingPlacementInteractionContext;
        public readonly TryGetEntityManagerAction TryGetDefaultEntityManager;
        public readonly IsPointerOverGameplayUiAction IsPointerOverGameplayUi;
        public readonly Action<Vector2> UpdateLastKnownPointerPosition;
        public readonly Action HideOrderScreenMarkers;
        public readonly float PanSensitivity;
        public readonly float ZoomSpeed;
        public readonly float MinZoomHeight;
        public readonly float MaxZoomHeight;
        public readonly float NormalModeZoomHeight;
        public readonly float BuildModeZoomHeight;
        public readonly float NormalModePitch;
        public readonly float BuildModePitch;
        public readonly float NormalModeYaw;
        public readonly float BuildModeYaw;
        public readonly float NormalModeFieldOfView;
        public readonly float BuildModeFieldOfView;
        public readonly float FullscreenIsoZoomHeight;
        public readonly float FullscreenIsoPitch;
        public readonly float FullscreenIsoYaw;
        public readonly float FullscreenIsoOrthographicSize;
        public readonly float ZoomTransitionSmoothTime;

        public Context(
            RuntimeGameplayStateSystem runtimeGameplayStateSystem,
            RtsSelectionInputSystem inputSystem,
            RtsCameraSystem cameraSystem,
            RtsCameraRequestSystem cameraRequestSystem,
            Camera worldCamera,
            MainMenuPlayUI mainMenuPlayUi,
            RoadBuildSystem roadBuildSystem,
            BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
            BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
            TryGetEntityManagerAction tryGetDefaultEntityManager,
            IsPointerOverGameplayUiAction isPointerOverGameplayUi,
            Action<Vector2> updateLastKnownPointerPosition,
            Action hideOrderScreenMarkers,
            float panSensitivity,
            float zoomSpeed,
            float minZoomHeight,
            float maxZoomHeight,
            float normalModeZoomHeight,
            float buildModeZoomHeight,
            float normalModePitch,
            float buildModePitch,
            float normalModeYaw,
            float buildModeYaw,
            float normalModeFieldOfView,
            float buildModeFieldOfView,
            float fullscreenIsoZoomHeight,
            float fullscreenIsoPitch,
            float fullscreenIsoYaw,
            float fullscreenIsoOrthographicSize,
            float zoomTransitionSmoothTime)
        {
            RuntimeGameplayStateSystem = runtimeGameplayStateSystem;
            InputSystem = inputSystem;
            CameraSystem = cameraSystem;
            CameraRequestSystem = cameraRequestSystem;
            WorldCamera = worldCamera;
            MainMenuPlayUi = mainMenuPlayUi;
            RoadBuildSystem = roadBuildSystem;
            BuildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
            BuildingPlacementInteractionContext = buildingPlacementInteractionContext;
            TryGetDefaultEntityManager = tryGetDefaultEntityManager;
            IsPointerOverGameplayUi = isPointerOverGameplayUi;
            UpdateLastKnownPointerPosition = updateLastKnownPointerPosition;
            HideOrderScreenMarkers = hideOrderScreenMarkers;
            PanSensitivity = panSensitivity;
            ZoomSpeed = zoomSpeed;
            MinZoomHeight = minZoomHeight;
            MaxZoomHeight = maxZoomHeight;
            NormalModeZoomHeight = normalModeZoomHeight;
            BuildModeZoomHeight = buildModeZoomHeight;
            NormalModePitch = normalModePitch;
            BuildModePitch = buildModePitch;
            NormalModeYaw = normalModeYaw;
            BuildModeYaw = buildModeYaw;
            NormalModeFieldOfView = normalModeFieldOfView;
            BuildModeFieldOfView = buildModeFieldOfView;
            FullscreenIsoZoomHeight = fullscreenIsoZoomHeight;
            FullscreenIsoPitch = fullscreenIsoPitch;
            FullscreenIsoYaw = fullscreenIsoYaw;
            FullscreenIsoOrthographicSize = fullscreenIsoOrthographicSize;
            ZoomTransitionSmoothTime = zoomTransitionSmoothTime;
        }
    }

    public bool UpdateRuntimeCameraTick(Context context)
    {
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        RtsCameraSystem camera = context.CameraSystem;
        if (!runtime.PlayRequested)
        {
            ResetCameraSession(context);
            ResetCameraModeSession(context);
            runtime.FullscreenMapOpen = false;
            runtime.FullscreenMapIsoMode = false;
            runtime.InitialCameraFocusRequested = false;
            return false;
        }

        if (runtime.FullscreenMapIsoMode)
        {
            if (context.WorldCamera == null)
                return false;

            UpdateFullscreenIsoZoom(context);
            UpdateFullscreenIsoCameraMode(context);
            HandleFullscreenIsoCameraPan(context);
            return false;
        }

        if (runtime.FullscreenMapOpen)
            return false;

        if (runtime.BuildModeActive)
        {
            if (camera.NormalIsoModeActive)
                ExitNormalIsoMode(context);
            UpdateBuildModeCameraTransition(context);
            UpdateSmoothCameraFocus(context);
            HandleBuildModeCameraPan(context);
            return false;
        }

        if (context.WorldCamera == null)
            return false;

        if (camera.NormalIsoModeActive)
        {
            UpdateFullscreenIsoZoom(context);
            UpdateFullscreenIsoCameraMode(context);
        }
        else
        {
            SyncCameraZoomModeState(context);
            ConsumeInitialCameraFocusRequest(context);
            UpdateZoom(context);
        }

        UpdateSmoothCameraFocus(context);
        return true;
    }

    public void ProcessCameraRequests(Context context, EntityManager em)
    {
        context.CameraRequestSystem.ProcessPendingRequests(
            em,
            context.CameraSystem,
            context.WorldCamera,
            context.HideOrderScreenMarkers);
    }

    public void PanCamera(Context context, Vector2 screenDelta)
    {
        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueuePan(em, screenDelta, context.PanSensitivity);
        ProcessCameraRequests(context, em);
    }

    public void SetCameraDragging(Context context, bool isDragging)
    {
        if (context.CameraSystem.IsDragging == isDragging)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetDragging(em, isDragging);
        ProcessCameraRequests(context, em);
    }

    public void EnterFullscreenMapIsoMode(Context context, Vector3 focusWorldPosition)
    {
        if (context.WorldCamera == null)
            return;

        float targetHeight = Mathf.Clamp(context.FullscreenIsoZoomHeight, context.MinZoomHeight, context.MaxZoomHeight);
        float targetOrthographicSize = Mathf.Clamp(context.FullscreenIsoOrthographicSize, 8f, 48f);
        SetFullscreenIsoTargets(context, targetHeight, targetOrthographicSize);
        if (context.TryGetDefaultEntityManager(out EntityManager em))
        {
            context.CameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
            context.CameraRequestSystem.QueueApplyFullscreenIsoModeInstant(
                em,
                context.CameraSystem.FullscreenIsoTargetHeight,
                context.CameraSystem.FullscreenIsoTargetOrthographicSize,
                context.FullscreenIsoPitch,
                context.FullscreenIsoYaw);
            ProcessCameraRequests(context, em);
        }

        context.RuntimeGameplayStateSystem.FullscreenMapIsoMode = true;
        context.RuntimeGameplayStateSystem.FullscreenMapOpen = true;
        SetCameraDragging(context, false);
    }

    public void ExitFullscreenMapIsoMode(Context context)
    {
        if (context.WorldCamera != null && context.TryGetDefaultEntityManager(out EntityManager em))
        {
            context.CameraRequestSystem.QueueApplyPerspectiveModeInstant(
                em,
                context.NormalModeZoomHeight,
                context.NormalModePitch,
                context.NormalModeYaw,
                context.NormalModeFieldOfView);
            ProcessCameraRequests(context, em);
        }

        context.RuntimeGameplayStateSystem.FullscreenMapIsoMode = false;
        SetCameraDragging(context, false);
    }

    public void ToggleNormalIsoMode(Context context)
    {
        if (context.CameraSystem.NormalIsoModeActive)
            ExitNormalIsoMode(context);
        else
            EnterNormalIsoMode(context);
    }

    public void EnterNormalIsoMode(Context context)
    {
        Camera worldCamera = context.WorldCamera;
        RtsCameraSystem camera = context.CameraSystem;
        if (worldCamera == null)
            return;

        Vector3 focusWorldPosition = camera.GetCameraGroundCenterWorld(worldCamera);
        float currentGroundSpan = camera.GetVisibleGroundVerticalSpan(worldCamera);
        float currentHeight = Mathf.Clamp(worldCamera.transform.position.y, context.MinZoomHeight, context.MaxZoomHeight);
        float targetOrthographicSize = Mathf.Clamp(
            camera.CalculateOrthographicSizeForGroundSpan(
                worldCamera,
                currentGroundSpan,
                currentHeight,
                context.FullscreenIsoPitch,
                context.FullscreenIsoYaw,
                context.FullscreenIsoOrthographicSize),
            8f,
            48f);

        SetFullscreenIsoTargets(context, currentHeight, targetOrthographicSize);
        if (context.TryGetDefaultEntityManager(out EntityManager em))
        {
            context.CameraRequestSystem.QueueApplyFullscreenIsoModeInstant(
                em,
                camera.FullscreenIsoTargetHeight,
                camera.FullscreenIsoTargetOrthographicSize,
                context.FullscreenIsoPitch,
                context.FullscreenIsoYaw);
            context.CameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
            context.CameraRequestSystem.QueueSetNormalIsoModeActive(em, true);
            ProcessCameraRequests(context, em);
        }

        SetCameraDragging(context, false);
    }

    public void ExitNormalIsoMode(Context context)
    {
        Camera worldCamera = context.WorldCamera;
        RtsCameraSystem camera = context.CameraSystem;
        Vector3 focusWorldPosition = worldCamera != null ? camera.GetCameraGroundCenterWorld(worldCamera) : Vector3.zero;
        if (worldCamera != null)
        {
            float currentGroundSpan = camera.GetVisibleGroundVerticalSpan(worldCamera);
            float targetHeight = camera.CalculatePerspectiveHeightForGroundSpan(
                worldCamera,
                currentGroundSpan,
                context.NormalModePitch,
                context.NormalModeYaw,
                context.NormalModeFieldOfView,
                context.MinZoomHeight,
                context.MaxZoomHeight,
                context.NormalModeZoomHeight);
            if (context.TryGetDefaultEntityManager(out EntityManager em))
            {
                context.CameraRequestSystem.QueueApplyPerspectiveModeInstant(
                    em,
                    targetHeight,
                    context.NormalModePitch,
                    context.NormalModeYaw,
                    context.NormalModeFieldOfView);
                context.CameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
                ProcessCameraRequests(context, em);
            }
        }

        SetCameraNormalIsoModeActive(context, false);
        SetCameraDragging(context, false);
    }

    public void MoveCameraGroundCenterTo(Context context, Vector3 focusWorldPosition)
    {
        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
        ProcessCameraRequests(context, em);
    }

    public void SmoothMoveCameraGroundCenterTo(Context context, Vector3 focusWorldPosition)
    {
        if (context.WorldCamera == null)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: true);
        context.CameraRequestSystem.QueueClearDragging(em);
        ProcessCameraRequests(context, em);
    }

    public void FollowCameraGroundCenterTo(Context context, Vector3 focusWorldPosition)
    {
        if (context.WorldCamera == null)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetSmoothFocusTarget(em, focusWorldPosition, resetVelocity: false);
        context.CameraRequestSystem.QueueClearDragging(em);
        ProcessCameraRequests(context, em);
    }

    private void HandleBuildModeCameraPan(Context context)
    {
        Camera worldCamera = context.WorldCamera;
        RtsSelectionInputSystem input = context.InputSystem;
        if (worldCamera == null)
            return;

        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        Vector2 pointerPosition = pointer.Position;
        context.UpdateLastKnownPointerPosition?.Invoke(pointerPosition);
        bool pointerOverGameplayUi = context.IsPointerOverGameplayUi?.Invoke(pointerPosition, out _) == true;
        bool pointerOverBuildToolMenu = context.MainMenuPlayUi != null && context.MainMenuPlayUi.IsPointerOverBuildToolMenu(pointerPosition);
        bool hasPendingBuildingPlacement = context.BuildingPlacementInteractionSystem != null &&
                                           context.BuildingPlacementInteractionSystem.HasPendingBuildingPlacement(context.BuildingPlacementInteractionContext);
        bool roadToolActive = context.RoadBuildSystem != null && context.RoadBuildSystem.IsRoadBuildModeActive;
        bool idleBuildMode = !hasPendingBuildingPlacement && !roadToolActive;
        bool interactionActive =
            (context.RoadBuildSystem != null && context.RoadBuildSystem.IsDraggingBuildInteraction) ||
            (context.BuildingPlacementInteractionSystem != null &&
             context.BuildingPlacementInteractionSystem.IsDraggingPlacementPreview(context.BuildingPlacementInteractionContext));

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

    private void HandleFullscreenIsoCameraPan(Context context)
    {
        RtsSelectionInputSystem input = context.InputSystem;
        if (!GamePointerInput.TryGetPrimaryPointer(out GamePointerState pointer))
            return;

        Vector2 pointerPosition = pointer.Position;
        context.UpdateLastKnownPointerPosition?.Invoke(pointerPosition);
        bool pointerOverGameplayUi = context.IsPointerOverGameplayUi?.Invoke(pointerPosition, out _) == true;

        if (pointer.WasPressedThisFrame)
        {
            input.LastPointerPosition = pointerPosition;
            SetCameraDragging(context, !pointerOverGameplayUi);
        }

        if (pointer.IsPressed && context.CameraSystem.IsDragging && !pointerOverGameplayUi)
        {
            Vector2 frameDelta = pointerPosition - input.LastPointerPosition;
            if (frameDelta.sqrMagnitude > 0f)
                PanCamera(context, frameDelta);
        }

        input.LastPointerPosition = pointerPosition;

        if (pointer.WasReleasedThisFrame || !pointer.IsPressed)
            SetCameraDragging(context, false);
    }

    private void UpdateZoom(Context context)
    {
        RtsCameraSystem camera = context.CameraSystem;
        if (camera.IsZoomTransitionActive)
        {
            float targetHeight = camera.WasBuildModeActive ? context.BuildModeZoomHeight : context.NormalModeZoomHeight;
            float targetPitch = camera.WasBuildModeActive ? context.BuildModePitch : context.NormalModePitch;
            float targetYaw = camera.WasBuildModeActive ? context.BuildModeYaw : context.NormalModeYaw;
            float targetFieldOfView = camera.WasBuildModeActive ? context.BuildModeFieldOfView : context.NormalModeFieldOfView;

            if (!context.TryGetDefaultEntityManager(out EntityManager em))
                return;

            context.CameraRequestSystem.QueueUpdatePerspectiveMode(
                em,
                targetHeight,
                targetPitch,
                targetYaw,
                targetFieldOfView,
                context.ZoomTransitionSmoothTime,
                completeTransitionOnArrive: true);
            ProcessCameraRequests(context, em);
            return;
        }

        float zoomDirection = ResolveZoomDirection(context.RuntimeGameplayStateSystem);
        if (Mathf.Approximately(zoomDirection, 0f))
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager defaultEntityManager))
            return;

        context.CameraRequestSystem.QueuePerspectiveZoom(
            defaultEntityManager,
            zoomDirection,
            context.ZoomSpeed,
            Time.deltaTime,
            context.MinZoomHeight,
            context.MaxZoomHeight);
        ProcessCameraRequests(context, defaultEntityManager);
    }

    private void UpdateFullscreenIsoZoom(Context context)
    {
        if (context.WorldCamera == null)
            return;

        float zoomDirection = ResolveZoomDirection(context.RuntimeGameplayStateSystem);
        if (Mathf.Approximately(zoomDirection, 0f))
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueFullscreenIsoZoom(
            em,
            zoomDirection,
            context.ZoomSpeed,
            Time.deltaTime,
            context.MinZoomHeight,
            context.MaxZoomHeight);
        ProcessCameraRequests(context, em);
    }

    private static float ResolveZoomDirection(RuntimeGameplayStateSystem runtime)
    {
        float zoomDirection = 0f;
        if (runtime.ZoomInHeld)
            zoomDirection += 1f;
        if (runtime.ZoomOutHeld)
            zoomDirection -= 1f;
        return zoomDirection;
    }

    private void UpdateFullscreenIsoCameraMode(Context context)
    {
        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueUpdateFullscreenIsoMode(
            em,
            context.CameraSystem.FullscreenIsoTargetHeight,
            context.CameraSystem.FullscreenIsoTargetOrthographicSize,
            context.FullscreenIsoPitch,
            context.FullscreenIsoYaw,
            context.ZoomTransitionSmoothTime);
        ProcessCameraRequests(context, em);
    }

    private void UpdateBuildModeCameraTransition(Context context)
    {
        if (context.WorldCamera == null)
            return;

        SyncCameraZoomModeState(context);

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueUpdatePerspectiveMode(
            em,
            context.BuildModeZoomHeight,
            context.BuildModePitch,
            context.BuildModeYaw,
            context.BuildModeFieldOfView,
            context.ZoomTransitionSmoothTime,
            completeTransitionOnArrive: false);
        ProcessCameraRequests(context, em);
    }

    private void SyncCameraZoomModeState(Context context)
    {
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        RtsCameraSystem camera = context.CameraSystem;
        if (Chapter01M01PlayableRuntime.IsActiveMission())
        {
            SetCameraWasPlayRequested(context, runtime.PlayRequested);
            SetCameraWasBuildModeActive(context, runtime.BuildModeActive);
            SetCameraZoomTransitionActive(context, false);
            return;
        }

        if (!camera.WasPlayRequested && runtime.PlayRequested)
        {
            Vector3 focusWorldPosition = context.WorldCamera != null ? camera.GetCameraGroundCenterWorld(context.WorldCamera) : Vector3.zero;
            if (context.TryGetDefaultEntityManager(out EntityManager em))
            {
                context.CameraRequestSystem.QueueApplyPerspectiveModeInstant(
                    em,
                    context.NormalModeZoomHeight,
                    context.NormalModePitch,
                    context.NormalModeYaw,
                    context.NormalModeFieldOfView);
                if (context.WorldCamera != null)
                    context.CameraRequestSystem.QueueMoveGroundCenterTo(em, focusWorldPosition);
                context.CameraRequestSystem.QueueResetTransitionVelocities(em);
                ProcessCameraRequests(context, em);
            }

            SetCameraWasPlayRequested(context, true);
            SetCameraWasBuildModeActive(context, runtime.BuildModeActive);
            SetCameraZoomTransitionActive(context, runtime.BuildModeActive);
            return;
        }

        SetCameraWasPlayRequested(context, runtime.PlayRequested);

        if (camera.WasBuildModeActive == runtime.BuildModeActive)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager manager))
            return;

        context.CameraRequestSystem.QueueBeginZoomTransition(manager, runtime.BuildModeActive);
        ProcessCameraRequests(context, manager);
    }

    private void ConsumeInitialCameraFocusRequest(Context context)
    {
        RuntimeGameplayStateSystem runtime = context.RuntimeGameplayStateSystem;
        if (!runtime.InitialCameraFocusRequested || context.WorldCamera == null)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueMoveGroundCenterTo(em, runtime.InitialCameraFocusWorld);
        context.CameraRequestSystem.QueueClearSmoothFocusTarget(em);
        ProcessCameraRequests(context, em);
        runtime.InitialCameraFocusRequested = false;
    }

    private void UpdateSmoothCameraFocus(Context context)
    {
        if (!context.CameraSystem.HasSmoothFocusTarget || context.WorldCamera == null)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueUpdateSmoothFocus(em, context.ZoomTransitionSmoothTime);
        ProcessCameraRequests(context, em);
    }

    private void SetCameraWasPlayRequested(Context context, bool wasPlayRequested)
    {
        if (context.CameraSystem.WasPlayRequested == wasPlayRequested)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetWasPlayRequested(em, wasPlayRequested);
        ProcessCameraRequests(context, em);
    }

    private void SetCameraWasBuildModeActive(Context context, bool wasBuildModeActive)
    {
        if (context.CameraSystem.WasBuildModeActive == wasBuildModeActive)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetWasBuildModeActive(em, wasBuildModeActive);
        ProcessCameraRequests(context, em);
    }

    private void SetCameraZoomTransitionActive(Context context, bool isActive)
    {
        if (context.CameraSystem.IsZoomTransitionActive == isActive)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetZoomTransitionActive(em, isActive);
        ProcessCameraRequests(context, em);
    }

    private void SetCameraNormalIsoModeActive(Context context, bool isActive)
    {
        if (context.CameraSystem.NormalIsoModeActive == isActive)
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetNormalIsoModeActive(em, isActive);
        ProcessCameraRequests(context, em);
    }

    private void SetFullscreenIsoTargets(Context context, float height, float orthographicSize)
    {
        if (Mathf.Approximately(context.CameraSystem.FullscreenIsoTargetHeight, height) &&
            Mathf.Approximately(context.CameraSystem.FullscreenIsoTargetOrthographicSize, orthographicSize))
            return;

        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueSetFullscreenIsoTargets(em, height, orthographicSize);
        ProcessCameraRequests(context, em);
    }

    private void ResetCameraSession(Context context)
    {
        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueResetSession(em);
        ProcessCameraRequests(context, em);
    }

    private void ResetCameraModeSession(Context context)
    {
        if (!context.TryGetDefaultEntityManager(out EntityManager em))
            return;

        context.CameraRequestSystem.QueueResetCameraModeSession(em);
        ProcessCameraRequests(context, em);
    }
}
