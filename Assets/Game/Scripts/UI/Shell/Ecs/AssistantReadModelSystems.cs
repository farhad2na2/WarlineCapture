using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    internal static class AssistantRuntimeStateUtility
    {
        public static bool IsActive(EntityManager entityManager, Entity boundary, EntityQuery matchStartQuery)
        {
            if (!entityManager.HasComponent<UiShellStateComponent>(boundary) ||
                matchStartQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            UiShellStateComponent shell = entityManager.GetComponentData<UiShellStateComponent>(boundary);
            if (shell.ActiveRoute != UIRoute.Match ||
                shell.CurrentMode != UiShellMode.MatchHud ||
                shell.IsTransitionRunning != 0)
            {
                return false;
            }

            MatchStartQueueComponent matchStart = matchStartQuery.GetSingleton<MatchStartQueueComponent>();
            return matchStart.HasStarted != 0;
        }

        public static void ClearInactiveReadModels(EntityManager entityManager, Entity boundary)
        {
            bool changed = false;
            changed |= ClearBuffer<AssistantGoalReadModelElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantRecommendationElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantThreatReadModelElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantMessageElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantNarrationRequestElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantCommandIntentRequestElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantCommandIntentResultElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantCommandDispatchElement>(entityManager, boundary);
            changed |= ClearBuffer<AssistantPreviewHighlightElement>(entityManager, boundary);

            if (entityManager.HasComponent<AssistantRecommendationReadModelComponent>(boundary))
            {
                AssistantRecommendationReadModelComponent recommendation =
                    entityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);
                if (recommendation.RecommendationCount != 0 || recommendation.TopRecommendationId != 0)
                {
                    recommendation.Version = NextVersion(recommendation.Version);
                    recommendation.RecommendationCount = 0;
                    recommendation.TopRecommendationId = 0;
                    recommendation.TopPriority = AssistantMessagePriority.Low;
                    recommendation.TopKind = AssistantRecommendationKind.None;
                    recommendation.UiDirty = 1;
                    entityManager.SetComponentData(boundary, recommendation);
                    changed = true;
                }
            }

            if (entityManager.HasComponent<AssistantThreatReadModelStateComponent>(boundary))
            {
                AssistantThreatReadModelStateComponent threat =
                    entityManager.GetComponentData<AssistantThreatReadModelStateComponent>(boundary);
                if (threat.VisibleCount != 0 || threat.NextExpiryAt > 0f)
                {
                    threat.Version = NextVersion(threat.Version);
                    threat.VisibleCount = 0;
                    threat.NextExpiryAt = 0f;
                    entityManager.SetComponentData(boundary, threat);
                    changed = true;
                }
            }

            if (entityManager.HasComponent<AssistantMessageReadModelComponent>(boundary))
            {
                AssistantMessageReadModelComponent messages =
                    entityManager.GetComponentData<AssistantMessageReadModelComponent>(boundary);
                if (messages.VisibleCount != 0 || messages.NextAgeBoundaryAt > 0f)
                {
                    messages.Version = NextVersion(messages.Version);
                    messages.VisibleCount = 0;
                    messages.NextAgeBoundaryAt = 0f;
                    entityManager.SetComponentData(boundary, messages);
                    changed = true;
                }
            }

            if (entityManager.HasComponent<AssistantTargetLockReadModelComponent>(boundary))
            {
                AssistantTargetLockReadModelComponent targetLock =
                    entityManager.GetComponentData<AssistantTargetLockReadModelComponent>(boundary);
                if (targetLock.Visible != 0 || targetLock.State != AssistantTargetLockState.None)
                {
                    uint nextVersion = NextVersion(targetLock.Version);
                    targetLock = default;
                    targetLock.Version = nextVersion;
                    entityManager.SetComponentData(boundary, targetLock);
                    changed = true;
                }
            }

            if (!entityManager.HasComponent<AssistantStateComponent>(boundary))
                return;

            AssistantStateComponent assistant = entityManager.GetComponentData<AssistantStateComponent>(boundary);
            if (changed ||
                assistant.PanelOpen != 0 ||
                assistant.HasActiveRecommendation != 0 ||
                assistant.ActiveRecommendationId != 0 ||
                assistant.ControlState != AssistantControlState.Player)
            {
                assistant.SourceVersion = NextVersion(assistant.SourceVersion);
                assistant.PublishedVersion = assistant.SourceVersion;
                assistant.PanelOpen = 0;
                assistant.HasActiveRecommendation = 0;
                assistant.ActiveRecommendationId = 0;
                assistant.ControlState = AssistantControlState.Player;
                assistant.UiDirty = 1;
                entityManager.SetComponentData(boundary, assistant);
            }
        }

        public static uint NextVersion(uint version)
        {
            uint next = version + 1u;
            return next == 0u ? 1u : next;
        }

        private static bool ClearBuffer<T>(EntityManager entityManager, Entity boundary)
            where T : unmanaged, IBufferElementData
        {
            if (!entityManager.HasBuffer<T>(boundary))
                return false;

            DynamicBuffer<T> buffer = entityManager.GetBuffer<T>(boundary);
            if (buffer.Length == 0)
                return false;

            buffer.Clear();
            return true;
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct AssistantGoalReadModelSystem : ISystem
    {
        private EntityQuery boundaryQuery;
        private EntityQuery matchStartQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            matchStartQuery = state.GetEntityQuery(ComponentType.ReadOnly<MatchStartQueueComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            EnsureAssistantReadModelBoundary(ref state, boundary);

            if (!AssistantRuntimeStateUtility.IsActive(state.EntityManager, boundary, matchStartQuery))
            {
                AssistantRuntimeStateUtility.ClearInactiveReadModels(state.EntityManager, boundary);
                AssistantObjectiveProjectionUtility.ClearHud(state.EntityManager, boundary);
                return;
            }

            if (!state.EntityManager.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary) ||
                !state.EntityManager.HasBuffer<MatchObjectiveRuntimeElement>(boundary))
            {
                AssistantObjectiveProjectionUtility.ClearHud(state.EntityManager, boundary);
                return;
            }

            MatchObjectiveRuntimeStateComponent objectiveState =
                state.EntityManager.GetComponentData<MatchObjectiveRuntimeStateComponent>(boundary);
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives =
                state.EntityManager.GetBuffer<MatchObjectiveRuntimeElement>(boundary, true);
            DynamicBuffer<AssistantGoalReadModelElement> goals =
                state.EntityManager.GetBuffer<AssistantGoalReadModelElement>(boundary);

            int expectedCount = objectiveState.MatchActive != 0 ? math.min(3, objectives.Length) : 0;
            bool goalsMatch = GoalsMatch(goals, objectives, expectedCount);
            bool hudMatches = AssistantObjectiveProjectionUtility.UpdateHud(
                state.EntityManager, boundary, objectives, expectedCount, objectiveState.ElapsedWholeSeconds);
            if (goalsMatch && hudMatches)
                return;

            if (!goalsMatch)
            {
                goals.Clear();
                for (int i = 0; i < expectedCount; i++)
                    goals.Add(ToGoal(objectives[i], objectiveState.Version));
            }

            AssistantStateComponent assistant = state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            assistant.SourceVersion = AssistantRuntimeStateUtility.NextVersion(assistant.SourceVersion);
            assistant.PublishedVersion = assistant.SourceVersion;
            assistant.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, assistant);
        }

        internal static void EnsureAssistantReadModelBoundary(ref SystemState state, Entity boundary)
        {
            EntityManager em = state.EntityManager;
            if (!em.HasComponent<MatchObjectiveProjectionBoundaryComponent>(boundary))
                em.AddComponent<MatchObjectiveProjectionBoundaryComponent>(boundary);
            if (!em.HasComponent<AssistantStateComponent>(boundary))
            {
                AssistantSettingsComponent settings = em.HasComponent<AssistantSettingsComponent>(boundary)
                    ? em.GetComponentData<AssistantSettingsComponent>(boundary)
                    : AssistantSettingsPersistenceSystemHelper.LoadSettingsComponent();
                em.AddComponentData(boundary, new AssistantStateComponent
                {
                    GuidanceLevel = settings.GuidanceLevel,
                    ControlState = AssistantControlState.Player,
                    SourceVersion = 1,
                    PublishedVersion = 1,
                    UiDirty = 1
                });
            }

            if (!em.HasComponent<AssistantSettingsComponent>(boundary))
                em.AddComponentData(boundary, AssistantSettingsPersistenceSystemHelper.LoadSettingsComponent());
            if (!em.HasComponent<AssistantRecommendationReadModelComponent>(boundary))
                em.AddComponentData(boundary, default(AssistantRecommendationReadModelComponent));
            if (!em.HasComponent<AssistantRecommendationEvaluationStateComponent>(boundary))
                em.AddComponentData(boundary, default(AssistantRecommendationEvaluationStateComponent));
            if (!em.HasComponent<AssistantMessageReadModelComponent>(boundary))
                em.AddComponentData(boundary, default(AssistantMessageReadModelComponent));
            if (!em.HasComponent<AssistantThreatReadModelStateComponent>(boundary))
                em.AddComponentData(boundary, default(AssistantThreatReadModelStateComponent));
            if (!em.HasComponent<AssistantTargetLockReadModelComponent>(boundary))
                em.AddComponentData(boundary, default(AssistantTargetLockReadModelComponent));
            EnsureBuffer<AssistantGoalReadModelElement>(em, boundary);
            EnsureBuffer<AssistantRecommendationElement>(em, boundary);
            EnsureBuffer<AssistantThreatReadModelElement>(em, boundary);
            EnsureBuffer<AssistantMessageElement>(em, boundary);
            EnsureBuffer<AssistantPreviewHighlightElement>(em, boundary);
            EnsureBuffer<AssistantCommandIntentRequestElement>(em, boundary);
            EnsureBuffer<AssistantCommandIntentResultElement>(em, boundary);
            EnsureBuffer<AssistantCommandDispatchElement>(em, boundary);
        }

        private static void EnsureBuffer<T>(EntityManager entityManager, Entity boundary)
            where T : unmanaged, IBufferElementData
        {
            if (!entityManager.HasBuffer<T>(boundary))
                entityManager.AddBuffer<T>(boundary);
        }

        private static AssistantGoalReadModelElement ToGoal(MatchObjectiveRuntimeElement objective, uint sourceVersion)
        {
            return AssistantObjectiveProjectionUtility.ToGoal(in objective, sourceVersion);
        }

        private static bool GoalsMatch(
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives,
            int expectedCount)
        {
            if (goals.Length != expectedCount)
                return false;

            for (int i = 0; i < expectedCount; i++)
            {
                MatchObjectiveRuntimeElement source = objectives[i];
                AssistantGoalReadModelElement current = goals[i];
                if (current.GoalId != source.GoalId ||
                    !current.ObjectiveId.Equals(source.ObjectiveId) ||
                    !current.OperationMapAnchorId.Equals(source.OperationMapAnchorId) ||
                    current.State != (AssistantGoalState)source.State ||
                    current.Priority != (AssistantMessagePriority)math.min((int)AssistantMessagePriority.Critical, (int)source.Priority) ||
                    !current.Title.Equals(source.Title) ||
                    !current.Body.Equals(source.Body) ||
                    current.TargetEntity != source.TargetEntity ||
                    !current.TargetCell.Equals(source.TargetCell) ||
                    !current.WorldPosition.Equals(source.WorldPosition) ||
                    current.IsPrimary != source.IsPrimary ||
                    current.HasTargetCell != source.HasTargetCell ||
                    current.HasWorldPosition != source.HasWorldPosition)
                {
                    return false;
                }
            }

            return true;
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantGoalReadModelSystem))]
    public partial struct AssistantRecommendationSystem : ISystem
    {
        private EntityQuery boundaryQuery;
        private EntityQuery matchStartQuery;
        private EntityQuery focusedSelectionQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            matchStartQuery = state.GetEntityQuery(ComponentType.ReadOnly<MatchStartQueueComponent>());
            focusedSelectionQuery = state.GetEntityQuery(ComponentType.ReadOnly<FocusedUnitUiReadModelComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);
            if (!AssistantRuntimeStateUtility.IsActive(state.EntityManager, boundary, matchStartQuery))
                return;

            AssistantStateComponent assistant = state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            AssistantThreatReadModelStateComponent threatState =
                state.EntityManager.GetComponentData<AssistantThreatReadModelStateComponent>(boundary);
            DynamicBuffer<AssistantGoalReadModelElement> goals =
                state.EntityManager.GetBuffer<AssistantGoalReadModelElement>(boundary, true);
            DynamicBuffer<AssistantThreatReadModelElement> threats =
                state.EntityManager.GetBuffer<AssistantThreatReadModelElement>(boundary, true);
            DynamicBuffer<AssistantRecommendationElement> recommendations =
                state.EntityManager.GetBuffer<AssistantRecommendationElement>(boundary);
            TryReadFocusedSelection(out FocusedUnitUiReadModelComponent focused);
            TryReadPlayerUsableFuelSummary(state.EntityManager, boundary, out BuildingRuntimeFactionUsableFuelSummary fuel);

            UiShellStateComponent shell = state.EntityManager.GetComponentData<UiShellStateComponent>(boundary);
            AssistantRecommendationEvaluationStateComponent evaluation =
                state.EntityManager.GetComponentData<AssistantRecommendationEvaluationStateComponent>(boundary);
            uint goalVersion = assistant.SourceVersion;
            uint focusedVersion = focused.HasFocusedUnit != 0 ? focused.CommandStateVersion : 0u;
            uint fuelVersion = fuel.Version;
            if (evaluation.Initialized != 0 &&
                evaluation.LastGoalVersion == goalVersion &&
                evaluation.LastThreatVersion == threatState.Version &&
                evaluation.LastFocusedUnitVersion == focusedVersion &&
                evaluation.LastFuelVersion == fuelVersion &&
                evaluation.LastRouteTransitionSequenceId == shell.TransitionSequenceId &&
                evaluation.LastControlState == assistant.ControlState)
            {
                return;
            }

            evaluation.LastGoalVersion = goalVersion;
            evaluation.LastThreatVersion = threatState.Version;
            evaluation.LastFocusedUnitVersion = focusedVersion;
            evaluation.LastFuelVersion = fuelVersion;
            evaluation.LastRouteTransitionSequenceId = shell.TransitionSequenceId;
            evaluation.LastControlState = assistant.ControlState;
            evaluation.Initialized = 1;
            state.EntityManager.SetComponentData(boundary, evaluation);

            AssistantRecommendationElement next = BuildRecommendation(goals, threats, focused, fuel, goalVersion);
            if (RecommendationsMatch(recommendations, next))
                return;

            recommendations.Clear();
            AssistantRecommendationReadModelComponent readModel =
                state.EntityManager.GetComponentData<AssistantRecommendationReadModelComponent>(boundary);
            if (next.RecommendationId != 0)
            {
                recommendations.Add(next);
                readModel.RecommendationCount = 1;
                readModel.TopRecommendationId = next.RecommendationId;
                readModel.TopPriority = next.Priority;
                readModel.TopKind = next.Kind;
                assistant.HasActiveRecommendation = 1;
                assistant.ActiveRecommendationId = next.RecommendationId;
            }
            else
            {
                readModel.RecommendationCount = 0;
                readModel.TopRecommendationId = 0;
                readModel.TopPriority = AssistantMessagePriority.Low;
                readModel.TopKind = AssistantRecommendationKind.None;
                assistant.HasActiveRecommendation = 0;
                assistant.ActiveRecommendationId = 0;
            }

            readModel.Version = AssistantRuntimeStateUtility.NextVersion(readModel.Version);
            readModel.UiDirty = 1;
            assistant.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, readModel);
            state.EntityManager.SetComponentData(boundary, assistant);
        }

        private static AssistantRecommendationElement BuildRecommendation(
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            DynamicBuffer<AssistantThreatReadModelElement> threats,
            FocusedUnitUiReadModelComponent focused,
            BuildingRuntimeFactionUsableFuelSummary fuel,
            uint sourceVersion)
        {
            if (threats.Length > 0)
            {
                AssistantThreatReadModelElement threat = threats[0];
                bool canAttack = threat.FriendlyTarget != Entity.Null && threat.HostileSource != Entity.Null;
                return new AssistantRecommendationElement
                {
                    RecommendationId = 200000 + math.abs(threat.ThreatId),
                    SourceVersion = (int)sourceVersion,
                    Kind = canAttack ? AssistantRecommendationKind.Attack : AssistantRecommendationKind.DefensiveAlert,
                    Priority = threat.Priority,
                    TargetKind = AssistantTargetKind.Entity,
                    SourceEntity = threat.FriendlyTarget,
                    TargetEntity = threat.HostileSource,
                    WorldPosition = threat.HostileWorldPosition,
                    Score = 100f,
                    Title = new FixedString64Bytes("Respond to verified threat"),
                    Reason = threat.Reason,
                    RejectionReason = canAttack ? default : new FixedString64Bytes("No verified hostile target"),
                    ActionLabel = new FixedString64Bytes("DO IT"),
                    HasWorldPosition = 1,
                    CanShow = 1,
                    CanExecute = canAttack ? (byte)1 : (byte)0
                };
            }

            if (HasFuelLogisticsWarning(fuel))
            {
                int roundedFuel = math.max(0, (int)math.round(fuel.StoredFuelBarrels));
                return new AssistantRecommendationElement
                {
                    RecommendationId = roundedFuel <= 0 ? 4001 : 4002,
                    SourceVersion = (int)sourceVersion,
                    Kind = AssistantRecommendationKind.Logistics,
                    Priority = roundedFuel <= 0 ? AssistantMessagePriority.High : AssistantMessagePriority.Normal,
                    TargetKind = AssistantTargetKind.UiSurface,
                    Score = roundedFuel <= 0 ? 95f : 84f,
                    Title = roundedFuel <= 0
                        ? new FixedString64Bytes("Fuel reserves empty")
                        : new FixedString64Bytes("Fuel reserves low"),
                    Reason = new FixedString128Bytes("Protect fuel production and delivery before issuing vehicle orders."),
                    RejectionReason = new FixedString64Bytes("No direct ARIA command"),
                    ActionLabel = new FixedString64Bytes("SHOW ME"),
                    CanShow = 1
                };
            }

            if (focused.HasFocusedUnit == 0)
            {
                return new AssistantRecommendationElement
                {
                    RecommendationId = 3001,
                    SourceVersion = (int)sourceVersion,
                    Kind = AssistantRecommendationKind.Select,
                    Priority = AssistantMessagePriority.High,
                    TargetKind = AssistantTargetKind.UiSurface,
                    Score = 90f,
                    Title = new FixedString64Bytes("Select a unit"),
                    Reason = new FixedString128Bytes("Select a player-controlled unit before issuing a tactical order."),
                    ActionLabel = new FixedString64Bytes("DO IT"),
                    CanShow = 1,
                    CanExecute = 1
                };
            }

            for (int i = 0; i < goals.Length; i++)
            {
                AssistantGoalReadModelElement goal = goals[i];
                if (goal.State != AssistantGoalState.Active && goal.State != AssistantGoalState.Warning)
                    continue;

                if (goal.TargetEntity != Entity.Null && focused.CanAttack != 0)
                {
                    return new AssistantRecommendationElement
                    {
                        RecommendationId = 1000 + goal.GoalId,
                        SourceVersion = goal.SourceVersion,
                        Kind = AssistantRecommendationKind.Attack,
                        Priority = goal.Priority,
                        TargetKind = AssistantTargetKind.Entity,
                        SourceEntity = focused.FocusedUnit,
                        TargetEntity = goal.TargetEntity,
                        TargetCell = goal.TargetCell,
                        WorldPosition = goal.WorldPosition,
                        Score = 86f,
                        Title = new FixedString64Bytes("Attack objective target"),
                        Reason = goal.Body,
                        ActionLabel = new FixedString64Bytes("DO IT"),
                        HasTargetCell = goal.HasTargetCell,
                        HasWorldPosition = goal.HasWorldPosition,
                        CanShow = 1,
                        CanExecute = focused.OwnedByPlayer
                    };
                }

                if (goal.HasTargetCell != 0 || goal.HasWorldPosition != 0)
                {
                    return new AssistantRecommendationElement
                    {
                        RecommendationId = 1000 + goal.GoalId,
                        SourceVersion = goal.SourceVersion,
                        Kind = AssistantRecommendationKind.Move,
                        Priority = goal.Priority,
                        TargetKind = goal.HasTargetCell != 0 ? AssistantTargetKind.Cell : AssistantTargetKind.WorldPosition,
                        SourceEntity = focused.FocusedUnit,
                        TargetCell = goal.TargetCell,
                        WorldPosition = goal.WorldPosition,
                        Score = 82f,
                        Title = new FixedString64Bytes("Move to objective"),
                        Reason = goal.Body,
                        ActionLabel = new FixedString64Bytes("DO IT"),
                        HasTargetCell = goal.HasTargetCell,
                        HasWorldPosition = goal.HasWorldPosition,
                        CanShow = goal.HasWorldPosition,
                        CanExecute = focused.OwnedByPlayer
                    };
                }

                if (AssistantObjectiveProjectionUtility.TryBuildAnchorFocus(in goal, out var focus))
                    return focus;
            }

            return default;
        }

        private static bool TryReadPlayerUsableFuelSummary(
            EntityManager entityManager,
            Entity boundary,
            out BuildingRuntimeFactionUsableFuelSummary fuel)
        {
            fuel = default;
            if (!entityManager.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary))
                return false;

            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                entityManager.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundary, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                if (!FactionIdentity.IsPlayerControlled(summaries[i].FactionId))
                    continue;
                fuel = summaries[i];
                return true;
            }

            return false;
        }

        private static bool HasFuelLogisticsWarning(BuildingRuntimeFactionUsableFuelSummary fuel)
        {
            if (!FactionIdentity.IsPlayerControlled(fuel.FactionId))
                return false;

            if (fuel.OilStorageCapacity <= 0 && fuel.FuelStorageCapacity <= 0)
                return false;

            return fuel.StoredFuelBarrels <= 0.5f ||
                   fuel.StoredFuelBarrels < math.min(100f, fuel.FuelStorageCapacity * 0.1f);
        }

        private bool TryReadFocusedSelection(out FocusedUnitUiReadModelComponent focused)
        {
            focused = default;
            if (focusedSelectionQuery.IsEmptyIgnoreFilter)
                return false;

            focused = focusedSelectionQuery.GetSingleton<FocusedUnitUiReadModelComponent>();
            return true;
        }

        private static bool RecommendationsMatch(
            DynamicBuffer<AssistantRecommendationElement> recommendations,
            AssistantRecommendationElement expected)
        {
            if (expected.RecommendationId == 0)
                return recommendations.Length == 0;
            if (recommendations.Length != 1)
                return false;

            AssistantRecommendationElement current = recommendations[0];
            return current.RecommendationId == expected.RecommendationId &&
                   current.SourceVersion == expected.SourceVersion &&
                   current.Kind == expected.Kind &&
                   current.Priority == expected.Priority &&
                   current.TargetKind == expected.TargetKind &&
                   current.SourceEntity == expected.SourceEntity &&
                   current.TargetEntity == expected.TargetEntity &&
                   current.TargetCell.Equals(expected.TargetCell) &&
                   current.WorldPosition.Equals(expected.WorldPosition) &&
                   current.TargetId.Equals(expected.TargetId) &&
                   current.Title.Equals(expected.Title) &&
                   current.Reason.Equals(expected.Reason) &&
                   current.RejectionReason.Equals(expected.RejectionReason) &&
                   current.ActionLabel.Equals(expected.ActionLabel) &&
                   current.HasTargetCell == expected.HasTargetCell &&
                   current.HasWorldPosition == expected.HasWorldPosition &&
                   current.CanShow == expected.CanShow &&
                   current.CanExecute == expected.CanExecute &&
                   current.CanTakeControl == expected.CanTakeControl;
        }
    }
}
