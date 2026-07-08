using Game.Components;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantRecommendationSystem))]
    public partial struct AssistantCommandIntentSystem : ISystem
    {
        private const int MaxResultRows = 16;
        private const int ReasonAccepted = 0;
        private const int ReasonUnsupportedIntent = 1;
        private const int ReasonMissingPreviewTarget = 2;

        private EntityQuery boundaryQuery;
        private EntityQuery cameraRequestQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<AssistantCommandIntentRequestElement>(),
                ComponentType.ReadWrite<AssistantCommandIntentResultElement>());
            cameraRequestQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsCameraRequestQueueComponent>(),
                ComponentType.ReadWrite<RtsCameraRequestElement>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);

            DynamicBuffer<AssistantCommandIntentRequestElement> requests =
                state.EntityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
            if (requests.Length == 0)
                return;

            bool needsCameraPreview = false;
            for (int i = 0; i < requests.Length; i++)
            {
                if (IsPreviewIntent(requests[i].Kind) &&
                    TryResolvePreviewTarget(ref state, requests[i], out _))
                {
                    needsCameraPreview = true;
                    break;
                }
            }

            if (needsCameraPreview)
            {
                EnsureCameraRequestEntity(ref state);
                requests = state.EntityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
            }

            AssistantStateComponent assistantState =
                state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            DynamicBuffer<AssistantCommandIntentResultElement> results =
                state.EntityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);

            bool assistantStateChanged = false;
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantCommandIntentRequestElement request = requests[i];
                if (!IsPreviewIntent(request.Kind))
                {
                    AddResult(results, request, AssistantCommandIntentStatus.Rejected, ReasonUnsupportedIntent, new FixedString64Bytes("Intent is not available yet."));
                    continue;
                }

                if (!TryResolvePreviewTarget(ref state, request, out float3 focusWorldPosition))
                {
                    AddResult(results, request, AssistantCommandIntentStatus.Rejected, ReasonMissingPreviewTarget, new FixedString64Bytes("No preview target is available."));
                    continue;
                }

                QueueCameraPreview(ref state, focusWorldPosition);
                AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted, new FixedString64Bytes("Preview queued."));

                assistantState.ControlState = AssistantControlState.AssistantPreview;
                assistantState.ActiveRecommendationId = request.RecommendationId;
                assistantState.UiDirty = 1;
                assistantStateChanged = true;
            }

            requests.Clear();
            TrimResults(results);

            if (assistantStateChanged)
                state.EntityManager.SetComponentData(boundary, assistantState);
        }

        private static bool IsPreviewIntent(AssistantCommandIntentKind kind)
        {
            return kind == AssistantCommandIntentKind.ShowRecommendation ||
                   kind == AssistantCommandIntentKind.FocusCamera;
        }

        private static bool TryResolvePreviewTarget(
            ref SystemState state,
            AssistantCommandIntentRequestElement request,
            out float3 focusWorldPosition)
        {
            focusWorldPosition = default;

            if (request.TargetKind == AssistantTargetKind.WorldPosition &&
                IsFinite(request.WorldPosition))
            {
                focusWorldPosition = request.WorldPosition;
                return true;
            }

            if (request.TargetKind == AssistantTargetKind.Entity)
            {
                if (TryReadEntityPosition(ref state, request.TargetEntity, out focusWorldPosition))
                    return true;

                if (TryReadEntityPosition(ref state, request.SourceEntity, out focusWorldPosition))
                    return true;

                if (IsFinite(request.WorldPosition))
                {
                    focusWorldPosition = request.WorldPosition;
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadEntityPosition(ref SystemState state, Entity entity, out float3 position)
        {
            position = default;
            if (entity == Entity.Null ||
                !state.EntityManager.Exists(entity) ||
                !state.EntityManager.HasComponent<LocalTransform>(entity))
            {
                return false;
            }

            position = state.EntityManager.GetComponentData<LocalTransform>(entity).Position;
            return IsFinite(position);
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
        }

        private void QueueCameraPreview(ref SystemState state, float3 focusWorldPosition)
        {
            Entity cameraEntity = EnsureCameraRequestEntity(ref state);
            RtsCameraRequestQueueComponent queue =
                state.EntityManager.GetComponentData<RtsCameraRequestQueueComponent>(cameraEntity);
            DynamicBuffer<RtsCameraRequestElement> cameraRequests =
                state.EntityManager.GetBuffer<RtsCameraRequestElement>(cameraEntity);

            queue.LastRequestId++;
            cameraRequests.Add(new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.SetSmoothFocusTarget,
                RequestId = queue.LastRequestId,
                WorldPosition = focusWorldPosition,
                Flag = 1
            });

            queue.LastRequestId++;
            cameraRequests.Add(new RtsCameraRequestElement
            {
                Kind = RtsCameraRequestKind.ClearDragging,
                RequestId = queue.LastRequestId
            });

            state.EntityManager.SetComponentData(cameraEntity, queue);
        }

        private Entity EnsureCameraRequestEntity(ref SystemState state)
        {
            if (!cameraRequestQuery.IsEmptyIgnoreFilter)
                return cameraRequestQuery.GetSingletonEntity();

            Entity cameraEntity = state.EntityManager.CreateEntity(
                typeof(RtsCameraRequestQueueComponent),
                typeof(RtsCameraStateComponent));
            state.EntityManager.AddBuffer<RtsCameraRequestElement>(cameraEntity);
            return cameraEntity;
        }

        private static void AddResult(
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            AssistantCommandIntentRequestElement request,
            AssistantCommandIntentStatus status,
            int reasonCode,
            FixedString64Bytes message)
        {
            results.Add(new AssistantCommandIntentResultElement
            {
                RequestId = request.RequestId,
                Frame = request.Frame,
                RecommendationId = request.RecommendationId,
                Kind = request.Kind,
                Status = status,
                TargetKind = request.TargetKind,
                SourceEntity = request.SourceEntity,
                TargetEntity = request.TargetEntity,
                TargetCell = request.TargetCell,
                WorldPosition = request.WorldPosition,
                ReasonCode = reasonCode,
                Message = message
            });
        }

        private static void TrimResults(DynamicBuffer<AssistantCommandIntentResultElement> results)
        {
            while (results.Length > MaxResultRows)
                results.RemoveAt(0);
        }
    }
}
