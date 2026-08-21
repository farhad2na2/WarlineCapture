using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public sealed partial class RtsCameraRequestSystem : SystemBase
    {
        private Entity _cameraEntity;
        private EntityQuery _cameraQueueQuery;
        private EntityQuery _gridConfigQuery;
        private EntityQuery _activeOperationMapBoundsQuery;
        private EntityQuery _tacticalFollowPoseQuery;

        protected override void OnCreate()
        {
            _cameraQueueQuery = GetEntityQuery(ComponentType.ReadOnly<RtsCameraRequestQueueComponent>());
            _gridConfigQuery = GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _activeOperationMapBoundsQuery = GetEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>(),
                ComponentType.ReadOnly<OperationMapBoundsComponent>());
            _tacticalFollowPoseQuery = GetEntityQuery(ComponentType.ReadOnly<TacticalFollowCameraPoseComponent>());
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

            if (!_cameraQueueQuery.IsEmptyIgnoreFilter)
            {
                _cameraEntity = _cameraQueueQuery.GetSingletonEntity();
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

        public bool QueueSetMatchIntroZoomSettlePending(EntityManager entityManager, bool isPending)
        {
            return TryEnqueue(entityManager, Request(RtsCameraRequestKind.SetMatchIntroZoomSettlePending, flag: isPending));
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

        public bool QueueUpdateTacticalFollowPose(
            EntityManager entityManager,
            Vector3 desiredPosition,
            Vector3 lookAt,
            float fieldOfView,
            float smoothTime,
            bool orthographic = false,
            float orthographicSize = 0f,
            bool resetVelocity = false,
            Quaternion? targetRotation = null)
        {
            bool hasTargetRotation = targetRotation.HasValue;
            return TryEnqueue(entityManager, new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.UpdateTacticalFollowPose,
                WorldPosition = ToFloat3(desiredPosition),
                Rotation = hasTargetRotation ? ToFloat4(targetRotation.Value) : default,
                Value = lookAt.x,
                Value2 = lookAt.y,
                Value3 = lookAt.z,
                Value4 = fieldOfView,
                Value5 = smoothTime,
                Value6 = orthographicSize,
                Flag = orthographic ? (byte)1 : (byte)0,
                Flag2 = resetVelocity ? (byte)1 : (byte)0,
                Flag3 = hasTargetRotation ? (byte)1 : (byte)0
            });
        }

        public int RemoveRequestsSuppressedByTacticalFollow(EntityManager entityManager)
        {
            Entity entity = EnsureCameraEntity(entityManager);
            if (entity == Entity.Null || !entityManager.Exists(entity) || !entityManager.HasBuffer<RtsCameraRequestElement>(entity))
                return 0;

            DynamicBuffer<RtsCameraRequestElement> requests = entityManager.GetBuffer<RtsCameraRequestElement>(entity);
            int removed = 0;
            for (int i = requests.Length - 1; i >= 0; i--)
            {
                if (!IsSuppressedByTacticalFollow(requests[i].Kind))
                    continue;

                requests.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        public void ProcessPendingRequests(EntityManager entityManager, RtsCameraSystem cameraSystem, Camera worldCamera, Action orderMarkersHideRequested = null)
        {
            if (cameraSystem == null)
                return;

            bool tacticalFollowPoseValid = HasValidTacticalFollowPose(entityManager);
            SyncGroundBoundary(entityManager, cameraSystem, worldCamera, skipClamp: tacticalFollowPoseValid);

            Entity entity = EnsureCameraEntity(entityManager);
            DynamicBuffer<RtsCameraRequestElement> requests = entityManager.GetBuffer<RtsCameraRequestElement>(entity);
            bool processedTacticalFollowPose = false;
            for (int i = 0; i < requests.Length; i++)
            {
                if (requests[i].Kind == RtsCameraRequestKind.UpdateTacticalFollowPose)
                    processedTacticalFollowPose = true;
                else if (tacticalFollowPoseValid && IsSuppressedByTacticalFollow(requests[i].Kind))
                    continue;

                ProcessRequest(requests[i], cameraSystem, worldCamera, orderMarkersHideRequested);
            }

            requests.Clear();
            if (!processedTacticalFollowPose && !tacticalFollowPoseValid)
                cameraSystem.ClampCameraToGroundBoundary(worldCamera);
            MirrorState(entityManager, entity, cameraSystem);
        }

        private static bool IsSuppressedByTacticalFollow(RtsCameraRequestKind kind)
        {
            return kind == RtsCameraRequestKind.BeginZoomTransition ||
                   kind == RtsCameraRequestKind.CompleteZoomTransition ||
                   kind == RtsCameraRequestKind.ResetTransitionVelocities ||
                   kind == RtsCameraRequestKind.Pan ||
                   kind == RtsCameraRequestKind.PerspectiveZoom ||
                   kind == RtsCameraRequestKind.FullscreenIsoZoom ||
                   kind == RtsCameraRequestKind.UpdatePerspectiveMode ||
                   kind == RtsCameraRequestKind.UpdateFullscreenIsoMode ||
                   kind == RtsCameraRequestKind.ApplyPerspectiveModeInstant ||
                   kind == RtsCameraRequestKind.ApplyFullscreenIsoModeInstant ||
                   kind == RtsCameraRequestKind.MoveGroundCenterTo ||
                   kind == RtsCameraRequestKind.SetSmoothFocusTarget ||
                   kind == RtsCameraRequestKind.UpdateSmoothFocus;
        }

        private bool HasValidTacticalFollowPose(EntityManager entityManager)
        {
            if (_tacticalFollowPoseQuery.IsEmptyIgnoreFilter)
                return false;

            TacticalFollowCameraPoseComponent pose =
                entityManager.GetComponentData<TacticalFollowCameraPoseComponent>(_tacticalFollowPoseQuery.GetSingletonEntity());
            return pose.Valid != 0;
        }

        private void SyncGroundBoundary(EntityManager entityManager, RtsCameraSystem cameraSystem, Camera worldCamera, bool skipClamp)
        {
            Rect boundary;
            if (TryGetActiveOperationMapCameraBoundary(entityManager, out boundary) ||
                TryGetGridConfig(entityManager, out GridConfig grid) &&
                TryGetGridBoundary(in grid, out boundary))
            {
                cameraSystem.SetGroundBoundary(boundary);
                if (!skipClamp)
                    cameraSystem.ClampCameraToGroundBoundary(worldCamera);
            }
            else
            {
                cameraSystem.ClearGroundBoundary();
            }
        }

        private bool TryGetActiveOperationMapCameraBoundary(
            EntityManager entityManager,
            out Rect boundary)
        {
            boundary = default;
            if (_activeOperationMapBoundsQuery.CalculateEntityCount() != 1)
                return false;

            Entity entity = _activeOperationMapBoundsQuery.GetSingletonEntity();
            OperationMapBoundsComponent bounds =
                entityManager.GetComponentData<OperationMapBoundsComponent>(entity);
            float2 minimum = bounds.CameraMin.xz;
            float2 maximum = bounds.CameraMax.xz;
            if (!math.all(math.isfinite(minimum)) ||
                !math.all(math.isfinite(maximum)) ||
                math.any(maximum - minimum <= new float2(0.01f)))
            {
                return false;
            }

            boundary = new Rect(
                minimum.x,
                minimum.y,
                maximum.x - minimum.x,
                maximum.y - minimum.y);
            return true;
        }

        private bool TryGetGridConfig(EntityManager entityManager, out GridConfig grid)
        {
            grid = default;
            if (_gridConfigQuery.IsEmptyIgnoreFilter)
                return false;

            grid = entityManager.GetComponentData<GridConfig>(_gridConfigQuery.GetSingletonEntity());
            return grid.Width > 0 && grid.Height > 0 && grid.CellSize > 0.01f;
        }

        private static bool TryGetGridBoundary(in GridConfig grid, out Rect boundary)
        {
            float minX = grid.Origin.x;
            float minZ = grid.Origin.z;
            boundary = new Rect(
                minX,
                minZ,
                grid.Width * grid.CellSize,
                grid.Height * grid.CellSize);
            return true;
        }

        private static void ProcessRequest(RtsCameraRequestElement request, RtsCameraSystem cameraSystem, Camera worldCamera, Action orderMarkersHideRequested)
        {
            if (TryProcessSmoothRequest(request, cameraSystem, worldCamera))
                return;

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
                case RtsCameraRequestKind.SetMatchIntroZoomSettlePending:
                    cameraSystem.MatchIntroZoomSettlePending = request.Flag != 0;
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
                case RtsCameraRequestKind.UpdateTacticalFollowPose:
                    cameraSystem.UpdateTacticalFollowPose(
                        worldCamera,
                        ToVector3(request.WorldPosition),
                        new Vector3(request.Value, request.Value2, request.Value3),
                        request.Value4,
                        request.Value5,
                        request.Flag != 0,
                        request.Value6,
                        request.Flag2 != 0,
                        request.Flag3 != 0 ? ToQuaternion(request.Rotation) : null);
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
                HasSmoothPerspectiveTarget = ToByte(cameraSystem.HasSmoothPerspectiveTarget),
                WasPlayRequested = ToByte(cameraSystem.WasPlayRequested),
                WasBuildModeActive = ToByte(cameraSystem.WasBuildModeActive),
                IsZoomTransitionActive = ToByte(cameraSystem.IsZoomTransitionActive),
                MatchIntroZoomSettlePending = ToByte(cameraSystem.MatchIntroZoomSettlePending),
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

        private static float4 ToFloat4(Quaternion value)
        {
            return new float4(value.x, value.y, value.z, value.w);
        }

        private static Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private static Quaternion ToQuaternion(float4 value)
        {
            return new Quaternion(value.x, value.y, value.z, value.w);
        }
    }
}
