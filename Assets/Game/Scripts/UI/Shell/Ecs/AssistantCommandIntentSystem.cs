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
        private const int MaxMessageRows = 16;
        private const int TimeoutFrameWindow = 600;
        private const int ReasonAccepted = 0;
        private const int ReasonUnsupportedIntent = 1;
        private const int ReasonMissingPreviewTarget = 2;
        private const int ReasonMissingSelectionTarget = 3;
        private const int ReasonCancelled = 4;
        private const int ReasonTimedOut = 5;
        private const int RecoveryMessageBaseId = 700000;

        private EntityQuery boundaryQuery;
        private EntityQuery cameraRequestQuery;
        private EntityQuery selectionInputQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<AssistantCommandIntentRequestElement>(),
                ComponentType.ReadWrite<AssistantCommandIntentResultElement>());
            cameraRequestQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsCameraRequestQueueComponent>(),
                ComponentType.ReadWrite<RtsCameraRequestElement>());
            selectionInputQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
                ComponentType.ReadWrite<RtsSelectionInputRequestQueueComponent>(),
                ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
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

            int currentFrame = UnityEngine.Time.frameCount;
            bool needsCameraPreview = false;
            bool needsSelectionCommand = false;
            for (int i = 0; i < requests.Length; i++)
            {
                if (IsTimedOut(requests[i], currentFrame) ||
                    requests[i].Kind == AssistantCommandIntentKind.CancelPreview)
                {
                    continue;
                }

                if (requests[i].Kind == AssistantCommandIntentKind.SelectEntity)
                    needsSelectionCommand = true;

                if (IsPreviewIntent(requests[i].Kind) &&
                    TryResolvePreviewTarget(ref state, requests[i], out _))
                {
                    needsCameraPreview = true;
                    break;
                }
            }

            bool structuralSetupChanged = false;
            if (needsCameraPreview)
            {
                EnsureCameraRequestEntity(ref state);
                structuralSetupChanged = true;
            }

            if (needsSelectionCommand)
            {
                EnsureSelectionInputEntity(ref state);
                structuralSetupChanged = true;
            }

            if (structuralSetupChanged)
            {
                requests = state.EntityManager.GetBuffer<AssistantCommandIntentRequestElement>(boundary);
            }

            AssistantStateComponent assistantState =
                state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            DynamicBuffer<AssistantCommandIntentResultElement> results =
                state.EntityManager.GetBuffer<AssistantCommandIntentResultElement>(boundary);
            DynamicBuffer<AssistantMessageElement> messages =
                state.EntityManager.GetBuffer<AssistantMessageElement>(boundary);
            DynamicBuffer<AssistantPreviewHighlightElement> highlights =
                state.EntityManager.GetBuffer<AssistantPreviewHighlightElement>(boundary);

            bool assistantStateChanged = false;
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantCommandIntentRequestElement request = requests[i];
                if (IsTimedOut(request, currentFrame))
                {
                    ClearPreviewHighlight(highlights);
                    AddResult(results, request, AssistantCommandIntentStatus.TimedOut, ReasonTimedOut, new FixedString64Bytes("Intent timed out."));
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (request.Kind == AssistantCommandIntentKind.CancelPreview)
                {
                    ClearPreviewHighlight(highlights);
                    AddResult(results, request, AssistantCommandIntentStatus.Cancelled, ReasonCancelled, new FixedString64Bytes("Preview cancelled."));
                    assistantState.ControlState = AssistantControlState.Player;
                    assistantState.ActiveRecommendationId = 0;
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (request.Kind == AssistantCommandIntentKind.StopAssistantControl)
                {
                    ClearPreviewHighlight(highlights);
                    AddResult(results, request, AssistantCommandIntentStatus.Cancelled, ReasonCancelled, new FixedString64Bytes("Assistant control stopped."));
                    assistantState.ControlState = AssistantControlState.Player;
                    assistantState.ActiveRecommendationId = 0;
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (request.Kind == AssistantCommandIntentKind.SelectEntity)
                {
                    ClearPreviewHighlight(highlights);
                    if (!TryQueueSelectionCommand(ref state, request))
                    {
                        AddRejectedResult(
                            results,
                            messages,
                            request,
                            ReasonMissingSelectionTarget,
                            new FixedString64Bytes("No selectable target is available."));
                        assistantState.UiDirty = 1;
                        assistantStateChanged = true;
                        continue;
                    }

                    AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted, new FixedString64Bytes("Selection queued."));
                    assistantState.ControlState = AssistantControlState.Guided;
                    assistantState.ActiveRecommendationId = request.RecommendationId;
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (!IsPreviewIntent(request.Kind))
                {
                    ClearPreviewHighlight(highlights);
                    AddRejectedResult(
                        results,
                        messages,
                        request,
                        ReasonUnsupportedIntent,
                        new FixedString64Bytes("Intent is not available yet."));
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (!TryResolvePreviewTarget(ref state, request, out float3 focusWorldPosition))
                {
                    ClearPreviewHighlight(highlights);
                    AddRejectedResult(
                        results,
                        messages,
                        request,
                        ReasonMissingPreviewTarget,
                        new FixedString64Bytes("No preview target is available."));
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                QueueCameraPreview(ref state, focusWorldPosition);
                AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted, new FixedString64Bytes("Preview queued."));
                AddResult(results, request, AssistantCommandIntentStatus.Completed, ReasonAccepted, new FixedString64Bytes("Preview active."));
                SetPreviewHighlight(highlights, request, focusWorldPosition);

                assistantState.ControlState = AssistantControlState.AssistantPreview;
                assistantState.ActiveRecommendationId = request.RecommendationId;
                assistantState.UiDirty = 1;
                assistantStateChanged = true;
            }

            requests.Clear();
            TrimResults(results);
            TrimMessages(messages);

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

        private static bool IsTimedOut(AssistantCommandIntentRequestElement request, int currentFrame)
        {
            return currentFrame - request.Frame > TimeoutFrameWindow;
        }

        private bool TryQueueSelectionCommand(ref SystemState state, AssistantCommandIntentRequestElement request)
        {
            Entity selectionInput = EnsureSelectionInputEntity(ref state);
            RtsSelectionInputRequestQueueComponent queue =
                state.EntityManager.GetComponentData<RtsSelectionInputRequestQueueComponent>(selectionInput);
            DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
                state.EntityManager.GetBuffer<RtsSelectionCommandIntentRequestElement>(selectionInput);

            if (request.TargetKind == AssistantTargetKind.Entity)
            {
                Entity target = ResolveExistingEntity(ref state, request.TargetEntity);
                if (target == Entity.Null)
                    target = ResolveExistingEntity(ref state, request.SourceEntity);
                if (target == Entity.Null)
                    return false;

                queue.LastRequestId++;
                commandRequests.Add(new RtsSelectionCommandIntentRequestElement
                {
                    Kind = RtsSelectionCommandIntentKind.FocusUnit,
                    RequestId = queue.LastRequestId,
                    Frame = request.Frame,
                    SourceEntity = request.SourceEntity,
                    TargetEntity = target,
                    TargetKind = RtsSelectionCommandTargetKind.Entity,
                    WorldPosition = request.WorldPosition,
                    HasSourceEntity = request.SourceEntity != Entity.Null ? (byte)1 : (byte)0,
                    HasTargetEntity = 1,
                    HasWorldPosition = IsFinite(request.WorldPosition) ? (byte)1 : (byte)0
                });
                state.EntityManager.SetComponentData(selectionInput, queue);
                return true;
            }

            if (request.TargetKind == AssistantTargetKind.UiSurface ||
                request.TargetKind == AssistantTargetKind.None)
            {
                queue.LastRequestId++;
                commandRequests.Add(new RtsSelectionCommandIntentRequestElement
                {
                    Kind = RtsSelectionCommandIntentKind.EnterSelectionMode,
                    RequestId = queue.LastRequestId,
                    Frame = request.Frame
                });
                state.EntityManager.SetComponentData(selectionInput, queue);
                return true;
            }

            return false;
        }

        private static Entity ResolveExistingEntity(ref SystemState state, Entity entity)
        {
            return entity != Entity.Null && state.EntityManager.Exists(entity) ? entity : Entity.Null;
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

        private Entity EnsureSelectionInputEntity(ref SystemState state)
        {
            if (!selectionInputQuery.IsEmptyIgnoreFilter)
            {
                Entity entity = selectionInputQuery.GetSingletonEntity();
                EnsureSelectionBuffers(ref state, entity);
                return entity;
            }

            Entity selectionInput = state.EntityManager.CreateEntity(
                typeof(RtsSelectionInputStateComponent),
                typeof(RtsSelectionInputRequestQueueComponent));
            state.EntityManager.SetComponentData(selectionInput, new RtsSelectionInputStateComponent
            {
                QueuedMoveOrderFrame = -1
            });
            EnsureSelectionBuffers(ref state, selectionInput);
            return selectionInput;
        }

        private static void EnsureSelectionBuffers(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasBuffer<RtsSelectionPointerRequestElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionPointerRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<RtsSelectionCommandIntentRequestElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(entity);
            if (!state.EntityManager.HasBuffer<RtsSelectionCommandResultElement>(entity))
                state.EntityManager.AddBuffer<RtsSelectionCommandResultElement>(entity);
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

        private static void AddRejectedResult(
            DynamicBuffer<AssistantCommandIntentResultElement> results,
            DynamicBuffer<AssistantMessageElement> messages,
            AssistantCommandIntentRequestElement request,
            int reasonCode,
            FixedString64Bytes resultMessage)
        {
            AddResult(results, request, AssistantCommandIntentStatus.Rejected, reasonCode, resultMessage);
            AddRecoveryMessage(messages, request, reasonCode);
        }

        private static void AddRecoveryMessage(
            DynamicBuffer<AssistantMessageElement> messages,
            AssistantCommandIntentRequestElement request,
            int reasonCode)
        {
            FixedString128Bytes text = reasonCode switch
            {
                ReasonMissingPreviewTarget => new FixedString128Bytes("ARIA needs a map target before it can show that action."),
                ReasonMissingSelectionTarget => new FixedString128Bytes("ARIA could not find a selectable unit for that action."),
                ReasonUnsupportedIntent => new FixedString128Bytes("That ARIA action is not available yet. Try Show Me first."),
                _ => new FixedString128Bytes("ARIA could not complete that action.")
            };

            messages.Add(new AssistantMessageElement
            {
                MessageId = RecoveryMessageBaseId + math.max(0, request.RequestId),
                SourceVersion = request.Frame,
                Priority = AssistantMessagePriority.High,
                RelatedKind = AssistantRecommendationKind.Explain,
                SuppressionKey = new FixedString64Bytes("assistant.intent.recovery"),
                Text = text,
                CreatedAt = request.Frame,
                RequiresNarration = 0,
                Acknowledged = 0
            });
        }

        private static void ClearPreviewHighlight(DynamicBuffer<AssistantPreviewHighlightElement> highlights)
        {
            if (highlights.Length > 0)
                highlights.Clear();
        }

        private static void SetPreviewHighlight(
            DynamicBuffer<AssistantPreviewHighlightElement> highlights,
            AssistantCommandIntentRequestElement request,
            float3 focusWorldPosition)
        {
            highlights.Clear();
            highlights.Add(new AssistantPreviewHighlightElement
            {
                RequestId = request.RequestId,
                Frame = request.Frame,
                RecommendationId = request.RecommendationId,
                TargetKind = request.TargetKind,
                SourceEntity = request.SourceEntity,
                TargetEntity = request.TargetEntity,
                TargetCell = request.TargetCell,
                WorldPosition = focusWorldPosition,
                Strength = 1f,
                Active = 1
            });
        }

        private static void TrimResults(DynamicBuffer<AssistantCommandIntentResultElement> results)
        {
            while (results.Length > MaxResultRows)
                results.RemoveAt(0);
        }

        private static void TrimMessages(DynamicBuffer<AssistantMessageElement> messages)
        {
            while (messages.Length > MaxMessageRows)
                messages.RemoveAt(0);
        }
    }
}
