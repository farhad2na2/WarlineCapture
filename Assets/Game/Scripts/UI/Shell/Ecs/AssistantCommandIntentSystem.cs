using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
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
        private const int MaxDispatchRows = 8;
        private const int TimeoutFrameWindow = 600;
        private const int ReasonAccepted = (int)TacticalCommandReasonCode.None;
        private const int ReasonUnsupportedIntent = (int)TacticalCommandReasonCode.CommandUnavailable;
        private const int ReasonMissingPreviewTarget = (int)TacticalCommandReasonCode.CameraJumpUnavailable;
        private const int ReasonMissingSelectionTarget = (int)TacticalCommandReasonCode.NoSelection;
        private const int ReasonCancelled = (int)TacticalCommandReasonCode.None;
        private const int ReasonTimedOut = (int)TacticalCommandReasonCode.CommandUnavailable;
        private const int RecoveryMessageBaseId = 700000;
        private EntityQuery boundaryQuery;
        private EntityQuery cameraRequestQuery;
        private EntityQuery selectionInputQuery;
        private EntityQuery gridQuery;
        private EntityQuery operationMapMetadataQuery;

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
            gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            operationMapMetadataQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>(),
                ComponentType.ReadOnly<OperationMapMetadataComponent>(),
                ComponentType.ReadOnly<OperationMapReadinessComponent>());
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
            bool needsMoveCommand = false;
            bool needsAttackCommand = false;
            for (int i = 0; i < requests.Length; i++)
            {
                AssistantCommandIntentRequestElement pending = requests[i];
                if (IsTimedOut(pending, currentFrame) ||
                    pending.Kind == AssistantCommandIntentKind.CancelPreview)
                {
                    continue;
                }

                if (pending.Kind == AssistantCommandIntentKind.SelectEntity)
                    needsSelectionCommand = needsCameraPreview = true;
                else if (pending.Kind == AssistantCommandIntentKind.MoveToWorldPosition)
                    needsMoveCommand = true;
                else if (pending.Kind == AssistantCommandIntentKind.AttackEntity)
                    needsAttackCommand = true;

                if (IsPreviewIntent(pending.Kind) &&
                    pending.RecommendationKind != AssistantRecommendationKind.Move &&
                    pending.RecommendationKind != AssistantRecommendationKind.Attack &&
                    AssistantPreviewTargetUtility.TryResolve(
                        state.EntityManager,
                        operationMapMetadataQuery,
                        in pending,
                        out _))
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

            if (needsMoveCommand)
            {
                UnitMoveOrderRequestSystem.EnsureQueueEntity(state.EntityManager);
                structuralSetupChanged = true;
            }

            if (needsAttackCommand)
            {
                UnitAttackOrderRequestSystem.EnsureQueueEntity(state.EntityManager);
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
            DynamicBuffer<AssistantCommandDispatchElement> dispatches =
                state.EntityManager.GetBuffer<AssistantCommandDispatchElement>(boundary);
            DynamicBuffer<AssistantRecommendationElement> recommendations =
                state.EntityManager.GetBuffer<AssistantRecommendationElement>(boundary, true);

            bool assistantStateChanged = false;
            bool continueMissionSquadAttack = false;
            float now = (float)SystemAPI.Time.ElapsedTime;
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
                    CancelPendingDispatches(dispatches);
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
                    if (!TryValidateRecommendation(recommendations, request, out TacticalCommandReasonCode validationReason) ||
                        !TryQueueSelectionCommand(ref state, request, out int downstreamRequestId))
                    {
                        AddRejectedResult(
                            results,
                            messages,
                            request,
                            validationReason == TacticalCommandReasonCode.None
                                ? (int)TacticalCommandReasonCode.NoSelection
                                : (int)validationReason,
                            new FixedString64Bytes("No selectable target is available."));
                        assistantState.UiDirty = 1;
                        assistantStateChanged = true;
                        continue;
                    }

                    AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted, new FixedString64Bytes("Selection queued."));
                    AddDispatch(
                        dispatches,
                        request,
                        AssistantDownstreamCommandKind.Selection,
                        downstreamRequestId,
                        now);
                    AssistantPreviewTargetUtility.TryQueueResolvedCameraPreview(
                        state.EntityManager, operationMapMetadataQuery, cameraRequestQuery, in request);
                    assistantState.ControlState = request.FromTakeover != 0
                        ? AssistantControlState.AssistantTakeover
                        : AssistantControlState.Guided;
                    assistantState.ActiveRecommendationId = request.RecommendationId;
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (request.Kind == AssistantCommandIntentKind.MoveToWorldPosition)
                {
                    ClearPreviewHighlight(highlights);
                    if (!TryValidateRecommendation(recommendations, request, out TacticalCommandReasonCode validationReason) ||
                        !TryQueueMoveCommand(ref state, request, out int downstreamRequestId, out validationReason))
                    {
                        AddRejectedResult(
                            results,
                            messages,
                            request,
                            (int)(validationReason == TacticalCommandReasonCode.None
                                ? TacticalCommandReasonCode.CommandUnavailable
                                : validationReason),
                            ReasonMessage(validationReason));
                        assistantState.UiDirty = 1;
                        assistantStateChanged = true;
                        continue;
                    }

                    AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted, new FixedString64Bytes("Move order queued."));
                    AddDispatch(dispatches, request, AssistantDownstreamCommandKind.MoveOrder, downstreamRequestId, now);
                    assistantState.ControlState = request.FromTakeover != 0
                        ? AssistantControlState.AssistantTakeover
                        : AssistantControlState.Guided;
                    assistantState.ActiveRecommendationId = request.RecommendationId;
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (request.Kind == AssistantCommandIntentKind.AttackEntity)
                {
                    ClearPreviewHighlight(highlights);
                    if (!TryValidateRecommendation(recommendations, request, out TacticalCommandReasonCode validationReason) ||
                        !TryQueueAttackCommand(ref state, request, out int downstreamRequestId, out validationReason))
                    {
                        AddRejectedResult(
                            results,
                            messages,
                            request,
                            (int)(validationReason == TacticalCommandReasonCode.None
                                ? TacticalCommandReasonCode.CommandUnavailable
                                : validationReason),
                            ReasonMessage(validationReason));
                        assistantState.UiDirty = 1;
                        assistantStateChanged = true;
                        continue;
                    }

                    AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted, new FixedString64Bytes("Attack order queued."));
                    AddDispatch(dispatches, request, AssistantDownstreamCommandKind.AttackOrder, downstreamRequestId, now);
                    continueMissionSquadAttack = true;
                    assistantState.ControlState = request.FromTakeover != 0
                        ? AssistantControlState.AssistantTakeover
                        : AssistantControlState.Guided;
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

                if (!TryValidateRecommendation(recommendations, request, out TacticalCommandReasonCode previewValidationReason))
                {
                    ClearPreviewHighlight(highlights);
                    AddRejectedResult(
                        results,
                        messages,
                        request,
                        (int)previewValidationReason,
                        ReasonMessage(previewValidationReason));
                    assistantState.UiDirty = 1;
                    assistantStateChanged = true;
                    continue;
                }

                if (!AssistantPreviewTargetUtility.TryResolve(
                        state.EntityManager,
                        operationMapMetadataQuery,
                        in request,
                        out float3 focusWorldPosition))
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

                // Guided command previews teach the next input before changing the view.
                // The command-button cue advances to the world target after the player
                // enters Move or Attack mode, so camera motion here would hide both the
                // button and the player's current battlefield context.
                if (request.RecommendationKind != AssistantRecommendationKind.Move &&
                    request.RecommendationKind != AssistantRecommendationKind.Attack)
                {
                    AssistantPreviewTargetUtility.QueueCameraPreview(
                        state.EntityManager, EnsureCameraRequestEntity(ref state), focusWorldPosition);
                }
                AddResult(results, request, AssistantCommandIntentStatus.Accepted, ReasonAccepted, new FixedString64Bytes("Preview queued."));
                AddResult(results, request, AssistantCommandIntentStatus.Completed, ReasonAccepted, new FixedString64Bytes("Preview active."));
                SetPreviewHighlight(highlights, request, focusWorldPosition);

                if (request.Kind == AssistantCommandIntentKind.FocusCamera)
                {
                    AddDispatch(dispatches, request, AssistantDownstreamCommandKind.Camera, 0, now);
                    AssistantCommandDispatchElement cameraDispatch = dispatches[dispatches.Length - 1];
                    cameraDispatch.Status = AssistantCommandIntentStatus.Completed;
                    dispatches[dispatches.Length - 1] = cameraDispatch;
                }

                assistantState.ControlState = AssistantControlState.AssistantPreview;
                assistantState.ActiveRecommendationId = request.RecommendationId;
                assistantState.UiDirty = 1;
                assistantStateChanged = true;
            }

            requests.Clear();
            TrimResults(results);
            TrimMessages(messages);
            TrimDispatches(dispatches);

            if (assistantStateChanged)
                state.EntityManager.SetComponentData(boundary, assistantState);

            // Group attack adds EngageTarget components to squad members. Do that structural
            // work only after every assistant boundary buffer has been consumed and written;
            // otherwise Unity invalidates the live result/dispatch handles before AddResult.
            if (continueMissionSquadAttack)
                CampaignMissionGroupAttackUtility.TryContinueActiveMissionSquadAttack(state.EntityManager);
        }

        private static bool IsPreviewIntent(AssistantCommandIntentKind kind)
        {
            return kind == AssistantCommandIntentKind.ShowRecommendation ||
                   kind == AssistantCommandIntentKind.FocusCamera;
        }

        private static bool IsTimedOut(AssistantCommandIntentRequestElement request, int currentFrame)
        {
            return currentFrame - request.Frame > TimeoutFrameWindow;
        }

        private bool TryQueueSelectionCommand(
            ref SystemState state,
            AssistantCommandIntentRequestElement request,
            out int downstreamRequestId)
        {
            Entity selectionInput = EnsureSelectionInputEntity(ref state);
            return AssistantSelectionCommandUtility.TryQueue(
                state.EntityManager, selectionInput, in request, out downstreamRequestId);
        }

        private static bool TryValidateRecommendation(
            DynamicBuffer<AssistantRecommendationElement> recommendations,
            AssistantCommandIntentRequestElement request,
            out TacticalCommandReasonCode reason)
        {
            reason = TacticalCommandReasonCode.None;
            if (request.Kind == AssistantCommandIntentKind.StopAssistantControl ||
                request.Kind == AssistantCommandIntentKind.CancelPreview)
            {
                return true;
            }

            // Legacy synthetic requests created before source-version correlation remain
            // preview/test compatible. Runtime gateway requests always carry a version.
            if (request.RecommendationSourceVersion == 0)
                return true;

            if (recommendations.Length == 0)
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            AssistantRecommendationElement current = recommendations[0];
            if (current.RecommendationId != request.RecommendationId ||
                (request.RecommendationSourceVersion != 0 &&
                 current.SourceVersion != request.RecommendationSourceVersion) ||
                current.SourceEntity != request.SourceEntity ||
                current.TargetEntity != request.TargetEntity ||
                !current.TargetCell.Equals(request.TargetCell) ||
                !current.WorldPosition.Equals(request.WorldPosition) ||
                current.TargetKind != request.TargetKind ||
                !current.TargetId.Equals(request.TargetId))
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            return true;
        }

        private static bool TryQueueAttackCommand(
            ref SystemState state,
            AssistantCommandIntentRequestElement request,
            out int downstreamRequestId,
            out TacticalCommandReasonCode reason)
        {
            downstreamRequestId = 0;
            reason = TacticalCommandReasonCode.None;
            EntityManager em = state.EntityManager;
            if (!TryValidatePlayerSource(em, request.SourceEntity, out reason))
                return false;

            if (!em.HasComponent<UnitAttack>(request.SourceEntity))
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            if (request.TargetEntity == Entity.Null ||
                !em.Exists(request.TargetEntity) ||
                !em.HasComponent<Faction>(request.TargetEntity))
            {
                reason = TacticalCommandReasonCode.TargetNotAttackable;
                return false;
            }

            byte targetFaction = em.GetComponentData<Faction>(request.TargetEntity).Id;
            if (!FactionIdentity.IsHostileToPlayer(targetFaction))
            {
                reason = TacticalCommandReasonCode.TargetNotEnemy;
                return false;
            }

            if (em.HasComponent<UnitHealth>(request.TargetEntity) &&
                em.GetComponentData<UnitHealth>(request.TargetEntity).Current <= 0)
            {
                reason = TacticalCommandReasonCode.TargetNotAttackable;
                return false;
            }

            int2 targetCell = request.TargetCell;
            if (em.HasComponent<UnitGrid>(request.TargetEntity))
                targetCell = em.GetComponentData<UnitGrid>(request.TargetEntity).Cell;
            float3 targetPosition = request.WorldPosition;
            if (em.HasComponent<LocalTransform>(request.TargetEntity))
                targetPosition = em.GetComponentData<LocalTransform>(request.TargetEntity).Position;

            downstreamRequestId = UnitAttackOrderRequestSystem.EnqueueDirectAttackTarget(
                em,
                request.SourceEntity,
                request.TargetEntity,
                targetCell,
                targetPosition);
            return downstreamRequestId > 0;
        }

        private static bool TryValidatePlayerSource(
            EntityManager entityManager,
            Entity source,
            out TacticalCommandReasonCode reason)
        {
            reason = TacticalCommandReasonCode.None;
            if (source == Entity.Null || !entityManager.Exists(source))
            {
                reason = TacticalCommandReasonCode.NoSelection;
                return false;
            }

            if (!entityManager.HasComponent<Faction>(source) ||
                !FactionIdentity.IsPlayerControlled(entityManager.GetComponentData<Faction>(source).Id))
            {
                reason = TacticalCommandReasonCode.NoSelection;
                return false;
            }

            if (entityManager.HasComponent<UnitHealth>(source) &&
                entityManager.GetComponentData<UnitHealth>(source).Current <= 0)
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            return true;
        }

        private static FixedString64Bytes ReasonMessage(TacticalCommandReasonCode reason)
        {
            return reason switch
            {
                TacticalCommandReasonCode.NoSelection => new FixedString64Bytes("No player unit is available."),
                TacticalCommandReasonCode.TargetOutOfBounds => new FixedString64Bytes("Target is outside the operation map."),
                TacticalCommandReasonCode.TargetNotEnemy => new FixedString64Bytes("Target is not hostile."),
                TacticalCommandReasonCode.TargetNotAttackable => new FixedString64Bytes("Target cannot be attacked."),
                _ => new FixedString64Bytes("Command is unavailable.")
            };
        }

        private static void AddDispatch(
            DynamicBuffer<AssistantCommandDispatchElement> dispatches,
            AssistantCommandIntentRequestElement request,
            AssistantDownstreamCommandKind downstreamKind,
            int downstreamRequestId,
            float now)
        {
            dispatches.Add(new AssistantCommandDispatchElement
            {
                AssistantRequestId = request.RequestId,
                RecommendationId = request.RecommendationId,
                IntentKind = request.Kind,
                DownstreamKind = downstreamKind,
                DownstreamRequestId = downstreamRequestId,
                Status = AssistantCommandIntentStatus.Accepted,
                RequestedAt = now
            });
        }

        private static void CancelPendingDispatches(DynamicBuffer<AssistantCommandDispatchElement> dispatches)
        {
            for (int i = 0; i < dispatches.Length; i++)
            {
                AssistantCommandDispatchElement dispatch = dispatches[i];
                if (dispatch.Status != AssistantCommandIntentStatus.Pending)
                    continue;

                dispatch.Status = AssistantCommandIntentStatus.Cancelled;
                dispatches[i] = dispatch;
            }
        }

        private static Entity ResolveExistingEntity(ref SystemState state, Entity entity)
        {
            return entity != Entity.Null && state.EntityManager.Exists(entity) ? entity : Entity.Null;
        }

        private static bool IsFinite(float3 value)
        {
            return math.all(math.isfinite(value));
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
                RecommendationKind = request.RecommendationKind,
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

        private static void TrimDispatches(DynamicBuffer<AssistantCommandDispatchElement> dispatches)
        {
            while (dispatches.Length > MaxDispatchRows)
            {
                int removeIndex = -1;
                for (int i = 0; i < dispatches.Length; i++)
                {
                    AssistantCommandIntentStatus status = dispatches[i].Status;
                    if (status == AssistantCommandIntentStatus.Pending ||
                        status == AssistantCommandIntentStatus.Accepted)
                    {
                        continue;
                    }

                    removeIndex = i;
                    break;
                }

                dispatches.RemoveAt(removeIndex >= 0 ? removeIndex : 0);
            }
        }
    }
}
