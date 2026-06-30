using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed partial class RtsCameraRequestSystem : SystemBase
{
    private Entity _cameraEntity;

    protected override void OnCreate()
    {
        Enabled = false;
    }

    protected override void OnUpdate()
    {
    }

    public Entity EnsureCameraEntity(EntityManager entityManager)
    {
        if (_cameraEntity != Entity.Null &&
            entityManager.Exists(_cameraEntity) &&
            entityManager.HasComponent<RtsCameraRequestQueueComponent>(_cameraEntity))
        {
            EnsureCameraComponents(entityManager, _cameraEntity);
            return _cameraEntity;
        }

        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RtsCameraRequestQueueComponent>());
        if (!query.IsEmptyIgnoreFilter)
        {
            _cameraEntity = query.GetSingletonEntity();
            EnsureCameraComponents(entityManager, _cameraEntity);
            return _cameraEntity;
        }

        _cameraEntity = entityManager.CreateEntity(
            typeof(RtsCameraRequestQueueComponent),
            typeof(RtsCameraStateComponent));
        entityManager.SetName(_cameraEntity, "RtsCameraRequests");
        entityManager.AddBuffer<RtsCameraRequestElement>(_cameraEntity);
        return _cameraEntity;
    }

    public bool TryReadState(EntityManager entityManager, out RtsCameraStateComponent state)
    {
        state = default;
        Entity entity = EnsureCameraEntity(entityManager);
        if (entity == Entity.Null || !entityManager.Exists(entity) || !entityManager.HasComponent<RtsCameraStateComponent>(entity))
            return false;

        state = entityManager.GetComponentData<RtsCameraStateComponent>(entity);
        return true;
    }

    public bool TryEnqueue(EntityManager entityManager, RtsCameraRequestElement request)
    {
        Entity entity = EnsureCameraEntity(entityManager);
        if (entity == Entity.Null || !entityManager.Exists(entity))
            return false;

        RtsCameraRequestQueueComponent queue = entityManager.GetComponentData<RtsCameraRequestQueueComponent>(entity);
        queue.LastRequestId++;
        request.RequestId = queue.LastRequestId;
        entityManager.SetComponentData(entity, queue);
        entityManager.GetBuffer<RtsCameraRequestElement>(entity).Add(request);
        return true;
    }

    public bool QueueResetSession(EntityManager entityManager)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.ResetSession));
    }

    public bool QueueResetCameraModeSession(EntityManager entityManager)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.ResetCameraModeSession));
    }

    public bool QueueSetDragging(EntityManager entityManager, bool isDragging)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetDragging, flag: isDragging));
    }

    public bool QueueClearDragging(EntityManager entityManager)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.ClearDragging));
    }

    public bool QueueSetWasPlayRequested(EntityManager entityManager, bool wasPlayRequested)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetWasPlayRequested, flag: wasPlayRequested));
    }

    public bool QueueSetWasBuildModeActive(EntityManager entityManager, bool wasBuildModeActive)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetWasBuildModeActive, flag: wasBuildModeActive));
    }

    public bool QueueSetZoomTransitionActive(EntityManager entityManager, bool isActive)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetZoomTransitionActive, flag: isActive));
    }

    public bool QueueSetNormalIsoModeActive(EntityManager entityManager, bool isActive)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetNormalIsoModeActive, flag: isActive));
    }

    public bool QueueSetFullscreenIsoTargets(EntityManager entityManager, float height, float orthographicSize)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetFullscreenIsoTargets, height, orthographicSize));
    }

    public bool QueueBeginZoomTransition(EntityManager entityManager, bool buildModeActive)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.BeginZoomTransition, flag: buildModeActive));
    }

    public bool QueueCompleteZoomTransition(EntityManager entityManager)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.CompleteZoomTransition));
    }

    public bool QueueResetTransitionVelocities(EntityManager entityManager)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.ResetTransitionVelocities));
    }

    public bool QueuePan(EntityManager entityManager, Vector2 screenDelta, float panSensitivity)
    {
        return TryEnqueue(entityManager, new RtsCameraRequestElement
        {
            Kind = RtsCameraRequestKind.Pan,
            ScreenDelta = new float2(screenDelta.x, screenDelta.y),
            Value = panSensitivity
        });
    }

    public bool QueuePerspectiveZoom(EntityManager entityManager, float zoomDirection, float zoomSpeed, float deltaTime, float minZoomHeight, float maxZoomHeight)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.PerspectiveZoom, zoomDirection, zoomSpeed, deltaTime, minZoomHeight, maxZoomHeight));
    }

    public bool QueueFullscreenIsoZoom(EntityManager entityManager, float zoomDirection, float zoomSpeed, float deltaTime, float minZoomHeight, float maxZoomHeight)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.FullscreenIsoZoom, zoomDirection, zoomSpeed, deltaTime, minZoomHeight, maxZoomHeight));
    }

    public bool QueueUpdatePerspectiveMode(EntityManager entityManager, float targetHeight, float targetPitch, float targetYaw, float targetFieldOfView, float smoothTime, bool completeTransitionOnArrive)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.UpdatePerspectiveMode, targetHeight, targetPitch, targetYaw, targetFieldOfView, smoothTime, flag: completeTransitionOnArrive));
    }

    public bool QueueUpdateFullscreenIsoMode(EntityManager entityManager, float targetHeight, float targetOrthographicSize, float targetPitch, float targetYaw, float smoothTime)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.UpdateFullscreenIsoMode, targetHeight, targetOrthographicSize, targetPitch, targetYaw, smoothTime));
    }

    public bool QueueApplyPerspectiveModeInstant(EntityManager entityManager, float height, float pitch, float yaw, float fieldOfView)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.ApplyPerspectiveModeInstant, height, pitch, yaw, fieldOfView));
    }

    public bool QueueApplyFullscreenIsoModeInstant(EntityManager entityManager, float height, float orthographicSize, float pitch, float yaw)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.ApplyFullscreenIsoModeInstant, height, orthographicSize, pitch, yaw));
    }

    public bool QueueMoveGroundCenterTo(EntityManager entityManager, Vector3 focusWorldPosition)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.MoveGroundCenterTo, focusWorldPosition));
    }

    public bool QueueSetSmoothFocusTarget(EntityManager entityManager, Vector3 focusWorldPosition, bool resetVelocity)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetSmoothFocusTarget, focusWorldPosition, flag: resetVelocity));
    }

    public bool QueueClearSmoothFocusTarget(EntityManager entityManager)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.ClearSmoothFocusTarget));
    }

    public bool QueueUpdateSmoothFocus(EntityManager entityManager, float smoothTime)
    {
        return TryEnqueue(entityManager, Request(RtsCameraRequestKind.UpdateSmoothFocus, smoothTime));
    }

    public bool QueueUpdateTacticalFollowPose(
        EntityManager entityManager,
        Vector3 desiredPosition,
        Vector3 lookAt,
        float fieldOfView,
        float smoothTime,
        bool orthographic = false,
        float orthographicSize = 0f)
    {
        return TryEnqueue(entityManager, new RtsCameraRequestElement
        {
            Kind = RtsCameraRequestKind.UpdateTacticalFollowPose,
            WorldPosition = ToFloat3(desiredPosition),
            Value = lookAt.x,
            Value2 = lookAt.y,
            Value3 = lookAt.z,
            Value4 = fieldOfView,
            Value5 = smoothTime,
            Value6 = orthographicSize,
            Flag = orthographic ? (byte)1 : (byte)0
        });
    }

    public void ProcessPendingRequests(EntityManager entityManager, RtsCameraSystem cameraSystem, Camera worldCamera, Action orderMarkersHideRequested = null)
    {
        if (cameraSystem == null)
            return;

        SyncGroundBoundary(entityManager, cameraSystem, worldCamera);

        Entity entity = EnsureCameraEntity(entityManager);
        DynamicBuffer<RtsCameraRequestElement> requests = entityManager.GetBuffer<RtsCameraRequestElement>(entity);
        for (int i = 0; i < requests.Length; i++)
            ProcessRequest(requests[i], cameraSystem, worldCamera, orderMarkersHideRequested);

        requests.Clear();
        cameraSystem.ClampCameraToGroundBoundary(worldCamera);
        MirrorState(entityManager, entity, cameraSystem);
    }

    private static void SyncGroundBoundary(EntityManager entityManager, RtsCameraSystem cameraSystem, Camera worldCamera)
    {
        if (TryGetGridConfig(entityManager, out GridConfig grid))
        {
            cameraSystem.SetGroundBoundary(ToGroundBoundary(grid));
            cameraSystem.ClampCameraToGroundBoundary(worldCamera);
        }
        else
        {
            cameraSystem.ClearGroundBoundary();
        }
    }

    private static bool TryGetGridConfig(EntityManager entityManager, out GridConfig grid)
    {
        grid = default;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        grid = entityManager.GetComponentData<GridConfig>(query.GetSingletonEntity());
        return grid.Width > 0 && grid.Height > 0 && grid.CellSize > 0.01f;
    }

    private static Rect ToGroundBoundary(GridConfig grid)
    {
        float minX = grid.Origin.x;
        float minZ = grid.Origin.z;
        return new Rect(
            minX,
            minZ,
            grid.Width * grid.CellSize,
            grid.Height * grid.CellSize);
    }

    private static void ProcessRequest(RtsCameraRequestElement request, RtsCameraSystem cameraSystem, Camera worldCamera, Action orderMarkersHideRequested)
    {
        switch (request.Kind)
        {
            case RtsCameraRequestKind.ResetSession:
                cameraSystem.ResetSession();
                break;
            case RtsCameraRequestKind.ResetCameraModeSession:
                cameraSystem.ResetCameraModeSession();
                break;
            case RtsCameraRequestKind.SetDragging:
                cameraSystem.SetDragging(request.Flag != 0);
                break;
            case RtsCameraRequestKind.ClearDragging:
                cameraSystem.ClearDragging();
                break;
            case RtsCameraRequestKind.SetWasPlayRequested:
                cameraSystem.WasPlayRequested = request.Flag != 0;
                break;
            case RtsCameraRequestKind.SetWasBuildModeActive:
                cameraSystem.WasBuildModeActive = request.Flag != 0;
                break;
            case RtsCameraRequestKind.SetZoomTransitionActive:
                cameraSystem.IsZoomTransitionActive = request.Flag != 0;
                break;
            case RtsCameraRequestKind.SetFullscreenIsoTargets:
                cameraSystem.FullscreenIsoTargetHeight = request.Value;
                cameraSystem.FullscreenIsoTargetOrthographicSize = request.Value2;
                break;
            case RtsCameraRequestKind.SetNormalIsoModeActive:
                cameraSystem.NormalIsoModeActive = request.Flag != 0;
                break;
            case RtsCameraRequestKind.BeginZoomTransition:
                cameraSystem.BeginZoomTransition(request.Flag != 0);
                break;
            case RtsCameraRequestKind.CompleteZoomTransition:
                cameraSystem.CompleteZoomTransition();
                break;
            case RtsCameraRequestKind.ResetTransitionVelocities:
                cameraSystem.ResetTransitionVelocities();
                break;
            case RtsCameraRequestKind.Pan:
                if (cameraSystem.PanCamera(worldCamera, new Vector2(request.ScreenDelta.x, request.ScreenDelta.y), request.Value))
                    orderMarkersHideRequested?.Invoke();
                break;
            case RtsCameraRequestKind.PerspectiveZoom:
                cameraSystem.UpdatePerspectiveZoom(worldCamera, request.Value, request.Value2, request.Value3, request.Value4, request.Value5);
                break;
            case RtsCameraRequestKind.FullscreenIsoZoom:
                cameraSystem.UpdateFullscreenIsoZoom(request.Value, request.Value2, request.Value3, request.Value4, request.Value5);
                break;
            case RtsCameraRequestKind.UpdatePerspectiveMode:
                if (cameraSystem.UpdatePerspectiveCameraMode(worldCamera, request.Value, request.Value2, request.Value3, request.Value4, request.Value5) && request.Flag != 0)
                    cameraSystem.CompleteZoomTransition();
                break;
            case RtsCameraRequestKind.UpdateFullscreenIsoMode:
                cameraSystem.UpdateFullscreenIsoCameraMode(worldCamera, request.Value, request.Value2, request.Value3, request.Value4, request.Value5);
                break;
            case RtsCameraRequestKind.ApplyPerspectiveModeInstant:
                cameraSystem.ApplyPerspectiveCameraModeInstant(worldCamera, request.Value, request.Value2, request.Value3, request.Value4);
                break;
            case RtsCameraRequestKind.ApplyFullscreenIsoModeInstant:
                cameraSystem.ApplyFullscreenIsoCameraModeInstant(worldCamera, request.Value, request.Value2, request.Value3, request.Value4);
                break;
            case RtsCameraRequestKind.MoveGroundCenterTo:
                cameraSystem.MoveCameraGroundCenterTo(worldCamera, ToVector3(request.WorldPosition));
                break;
            case RtsCameraRequestKind.SetSmoothFocusTarget:
                cameraSystem.SetSmoothFocusTarget(ToVector3(request.WorldPosition), request.Flag != 0);
                break;
            case RtsCameraRequestKind.ClearSmoothFocusTarget:
                cameraSystem.ClearSmoothFocusTarget();
                break;
            case RtsCameraRequestKind.UpdateSmoothFocus:
                if (cameraSystem.HasSmoothFocusTarget && worldCamera != null)
                {
                    Vector3 currentGroundCenter = cameraSystem.GetCameraGroundCenterWorld(worldCamera);
                    Vector3 smoothedCenter = cameraSystem.UpdateSmoothFocus(currentGroundCenter, request.Value);
                    cameraSystem.MoveCameraGroundCenterTo(worldCamera, smoothedCenter);
                }
                break;
            case RtsCameraRequestKind.UpdateTacticalFollowPose:
                cameraSystem.UpdateTacticalFollowPose(
                    worldCamera,
                    ToVector3(request.WorldPosition),
                    new Vector3(request.Value, request.Value2, request.Value3),
                    request.Value4,
                    request.Value5,
                    request.Flag != 0,
                    request.Value6);
                break;
        }
    }

    private static void EnsureCameraComponents(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<RtsCameraStateComponent>(entity))
            entityManager.AddComponentData(entity, default(RtsCameraStateComponent));
        if (!entityManager.HasBuffer<RtsCameraRequestElement>(entity))
            entityManager.AddBuffer<RtsCameraRequestElement>(entity);
    }

    private static void MirrorState(EntityManager entityManager, Entity entity, RtsCameraSystem cameraSystem)
    {
        entityManager.SetComponentData(entity, new RtsCameraStateComponent
        {
            IsDragging = ToByte(cameraSystem.IsDragging),
            HasSmoothFocusTarget = ToByte(cameraSystem.HasSmoothFocusTarget),
            SmoothFocusTarget = ToFloat3(cameraSystem.SmoothFocusTarget),
            WasPlayRequested = ToByte(cameraSystem.WasPlayRequested),
            WasBuildModeActive = ToByte(cameraSystem.WasBuildModeActive),
            IsZoomTransitionActive = ToByte(cameraSystem.IsZoomTransitionActive),
            FullscreenIsoTargetHeight = cameraSystem.FullscreenIsoTargetHeight,
            FullscreenIsoTargetOrthographicSize = cameraSystem.FullscreenIsoTargetOrthographicSize,
            NormalIsoModeActive = ToByte(cameraSystem.NormalIsoModeActive)
        });
    }

    private static RtsCameraRequestElement Request(RtsCameraRequestKind kind, bool flag = false)
    {
        return new RtsCameraRequestElement { Kind = kind, Flag = ToByte(flag) };
    }

    private static RtsCameraRequestElement Request(RtsCameraRequestKind kind, float value, bool flag = false)
    {
        return new RtsCameraRequestElement { Kind = kind, Value = value, Flag = ToByte(flag) };
    }

    private static RtsCameraRequestElement Request(RtsCameraRequestKind kind, float value, float value2, bool flag = false)
    {
        return new RtsCameraRequestElement { Kind = kind, Value = value, Value2 = value2, Flag = ToByte(flag) };
    }

    private static RtsCameraRequestElement Request(RtsCameraRequestKind kind, float value, float value2, float value3, float value4, bool flag = false)
    {
        return new RtsCameraRequestElement
        {
            Kind = kind,
            Value = value,
            Value2 = value2,
            Value3 = value3,
            Value4 = value4,
            Flag = ToByte(flag)
        };
    }

    private static RtsCameraRequestElement Request(RtsCameraRequestKind kind, float value, float value2, float value3, float value4, float value5, bool flag = false)
    {
        return new RtsCameraRequestElement
        {
            Kind = kind,
            Value = value,
            Value2 = value2,
            Value3 = value3,
            Value4 = value4,
            Value5 = value5,
            Flag = ToByte(flag)
        };
    }

    private static RtsCameraRequestElement Request(RtsCameraRequestKind kind, Vector3 worldPosition, bool flag = false)
    {
        return new RtsCameraRequestElement
        {
            Kind = kind,
            WorldPosition = ToFloat3(worldPosition),
            Flag = ToByte(flag)
        };
    }

    private static byte ToByte(bool value)
    {
        return value ? (byte)1 : (byte)0;
    }

    private static float3 ToFloat3(Vector3 value)
    {
        return new float3(value.x, value.y, value.z);
    }

    private static Vector3 ToVector3(float3 value)
    {
        return new Vector3(value.x, value.y, value.z);
    }
}
