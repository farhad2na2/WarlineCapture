using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct AssistantGoalReadModelSystem : ISystem
    {
        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudStatusSurfacesComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            EnsureAssistantReadModelBoundary(ref state, boundary);

            UiMatchHudStatusSurfacesComponent status =
                state.EntityManager.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
            DynamicBuffer<AssistantGoalReadModelElement> goals =
                state.EntityManager.GetBuffer<AssistantGoalReadModelElement>(boundary);

            AssistantGoalReadModelElement goal0 = BuildGoal(1, status.Objective0Text, status.Objective0IconKind, AssistantMessagePriority.High, 1);
            AssistantGoalReadModelElement goal1 = BuildGoal(2, status.Objective1Text, status.Objective1IconKind, AssistantMessagePriority.Normal, 0);
            AssistantGoalReadModelElement goal2 = BuildGoal(3, status.Objective2Text, status.Objective2IconKind, AssistantMessagePriority.Low, 0);

            int expectedCount = CountVisible(goal0, goal1, goal2);
            if (GoalsMatch(goals, expectedCount, goal0, goal1, goal2))
                return;

            AssistantStateComponent assistantState = state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            uint nextVersion = assistantState.SourceVersion + 1u;
            if (nextVersion == 0u)
                nextVersion = 1u;

            goals.Clear();
            AddVisibleGoal(goals, goal0, nextVersion);
            AddVisibleGoal(goals, goal1, nextVersion);
            AddVisibleGoal(goals, goal2, nextVersion);

            assistantState.SourceVersion = nextVersion;
            assistantState.PublishedVersion = nextVersion;
            assistantState.UiDirty = 1;
            state.EntityManager.SetComponentData(boundary, assistantState);
        }

        internal static void EnsureAssistantReadModelBoundary(ref SystemState state, Entity boundary)
        {
            EntityManager em = state.EntityManager;
            if (!em.HasComponent<AssistantStateComponent>(boundary))
            {
                em.AddComponentData(boundary, new AssistantStateComponent
                {
                    GuidanceLevel = AssistantGuidanceLevel.FullGuidance,
                    ControlState = AssistantControlState.Player,
                    SourceVersion = 1,
                    PublishedVersion = 1,
                    UiDirty = 1
                });
            }

            if (!em.HasComponent<AssistantRecommendationReadModelComponent>(boundary))
                em.AddComponentData(boundary, default(AssistantRecommendationReadModelComponent));

            if (!em.HasBuffer<AssistantGoalReadModelElement>(boundary))
                em.AddBuffer<AssistantGoalReadModelElement>(boundary);

            if (!em.HasBuffer<AssistantRecommendationElement>(boundary))
                em.AddBuffer<AssistantRecommendationElement>(boundary);

            if (!em.HasBuffer<AssistantMessageElement>(boundary))
                em.AddBuffer<AssistantMessageElement>(boundary);
        }

        private static AssistantGoalReadModelElement BuildGoal(
            int goalId,
            FixedString64Bytes text,
            UiMatchHudObjectiveIconKind iconKind,
            AssistantMessagePriority priority,
            byte isPrimary)
        {
            return new AssistantGoalReadModelElement
            {
                GoalId = goalId,
                State = iconKind == UiMatchHudObjectiveIconKind.Checked
                    ? AssistantGoalState.Complete
                    : AssistantGoalState.Active,
                Priority = priority,
                Title = text,
                Body = iconKind == UiMatchHudObjectiveIconKind.Checked
                    ? new FixedString128Bytes("Objective complete.")
                    : new FixedString128Bytes("Current mission objective."),
                IsPrimary = isPrimary
            };
        }

        private static int CountVisible(
            AssistantGoalReadModelElement goal0,
            AssistantGoalReadModelElement goal1,
            AssistantGoalReadModelElement goal2)
        {
            int count = 0;
            if (goal0.Title.Length > 0)
                count++;
            if (goal1.Title.Length > 0)
                count++;
            if (goal2.Title.Length > 0)
                count++;
            return count;
        }

        private static bool GoalsMatch(
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            int expectedCount,
            AssistantGoalReadModelElement goal0,
            AssistantGoalReadModelElement goal1,
            AssistantGoalReadModelElement goal2)
        {
            if (goals.Length != expectedCount)
                return false;

            int index = 0;
            if (!GoalMatches(goals, ref index, goal0))
                return false;
            if (!GoalMatches(goals, ref index, goal1))
                return false;
            return GoalMatches(goals, ref index, goal2);
        }

        private static bool GoalMatches(
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            ref int index,
            AssistantGoalReadModelElement expected)
        {
            if (expected.Title.Length == 0)
                return true;

            if (index >= goals.Length)
                return false;

            AssistantGoalReadModelElement current = goals[index++];
            return current.GoalId == expected.GoalId
                && current.State == expected.State
                && current.Priority == expected.Priority
                && current.Title.Equals(expected.Title)
                && current.Body.Equals(expected.Body)
                && current.IsPrimary == expected.IsPrimary;
        }

        private static void AddVisibleGoal(
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            AssistantGoalReadModelElement goal,
            uint sourceVersion)
        {
            if (goal.Title.Length == 0)
                return;

            goal.SourceVersion = (int)sourceVersion;
            goals.Add(goal);
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(AssistantGoalReadModelSystem))]
    public partial struct AssistantRecommendationSystem : ISystem
    {
        private EntityQuery boundaryQuery;

        public void OnCreate(ref SystemState state)
        {
            boundaryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UiShellStateComponent>(),
                ComponentType.ReadOnly<UiMatchHudStatusSurfacesComponent>(),
                ComponentType.ReadOnly<UiMatchHudHeaderComponent>());
            state.RequireForUpdate(boundaryQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity boundary = boundaryQuery.GetSingletonEntity();
            AssistantGoalReadModelSystem.EnsureAssistantReadModelBoundary(ref state, boundary);

            UiMatchHudStatusSurfacesComponent status =
                state.EntityManager.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
            AssistantStateComponent assistantState =
                state.EntityManager.GetComponentData<AssistantStateComponent>(boundary);
            DynamicBuffer<AssistantGoalReadModelElement> goals =
                state.EntityManager.GetBuffer<AssistantGoalReadModelElement>(boundary);
            DynamicBuffer<AssistantRecommendationElement> recommendations =
                state.EntityManager.GetBuffer<AssistantRecommendationElement>(boundary);

            AssistantRecommendationElement next = BuildRecommendation(status, goals, assistantState.SourceVersion);
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
                assistantState.HasActiveRecommendation = 1;
                assistantState.ActiveRecommendationId = next.RecommendationId;
            }
            else
            {
                readModel.RecommendationCount = 0;
                readModel.TopRecommendationId = 0;
                readModel.TopPriority = AssistantMessagePriority.Low;
                readModel.TopKind = AssistantRecommendationKind.None;
                assistantState.HasActiveRecommendation = 0;
                assistantState.ActiveRecommendationId = 0;
            }

            readModel.Version++;
            if (readModel.Version == 0u)
                readModel.Version = 1u;
            readModel.UiDirty = 1;
            assistantState.UiDirty = 1;
            assistantState.PublishedVersion = assistantState.SourceVersion;

            state.EntityManager.SetComponentData(boundary, readModel);
            state.EntityManager.SetComponentData(boundary, assistantState);
        }

        private static AssistantRecommendationElement BuildRecommendation(
            UiMatchHudStatusSurfacesComponent status,
            DynamicBuffer<AssistantGoalReadModelElement> goals,
            uint sourceVersion)
        {
            if (status.ThreatVisible != 0)
            {
                return new AssistantRecommendationElement
                {
                    RecommendationId = 2001,
                    SourceVersion = (int)sourceVersion,
                    Kind = AssistantRecommendationKind.DefensiveAlert,
                    Priority = AssistantMessagePriority.Critical,
                    TargetKind = AssistantTargetKind.UiSurface,
                    Score = 100f,
                    Title = status.ThreatTitle.Length > 0
                        ? status.ThreatTitle
                        : new FixedString64Bytes("Threat detected"),
                    Reason = status.ThreatSubtitle.Length > 0
                        ? CopyTo128(status.ThreatSubtitle)
                        : new FixedString128Bytes("Respond to the active threat before issuing routine orders."),
                    ActionLabel = new FixedString64Bytes("SHOW ME"),
                    CanShow = 1
                };
            }

            for (int i = 0; i < goals.Length; i++)
            {
                AssistantGoalReadModelElement goal = goals[i];
                if (goal.State != AssistantGoalState.Active)
                    continue;

                return new AssistantRecommendationElement
                {
                    RecommendationId = 1000 + goal.GoalId,
                    SourceVersion = (int)sourceVersion,
                    Kind = AssistantRecommendationKind.CameraFocus,
                    Priority = goal.Priority,
                    TargetKind = AssistantTargetKind.Objective,
                    TargetCell = new int2(goal.GoalId, 0),
                    Score = 80f - goal.GoalId,
                    Title = new FixedString64Bytes("Review objective"),
                    Reason = new FixedString128Bytes("Focus the active objective before choosing the next order."),
                    ActionLabel = new FixedString64Bytes("SHOW ME"),
                    CanShow = 1
                };
            }

            return default;
        }

        private static FixedString128Bytes CopyTo128(FixedString64Bytes text)
        {
            FixedString128Bytes result = default;
            result.Append(text);
            return result;
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
            return current.RecommendationId == expected.RecommendationId
                && current.SourceVersion == expected.SourceVersion
                && current.Kind == expected.Kind
                && current.Priority == expected.Priority
                && current.TargetKind == expected.TargetKind
                && current.Score.Equals(expected.Score)
                && current.TargetCell.Equals(expected.TargetCell)
                && current.Title.Equals(expected.Title)
                && current.Reason.Equals(expected.Reason)
                && current.ActionLabel.Equals(expected.ActionLabel)
                && current.CanShow == expected.CanShow
                && current.CanExecute == expected.CanExecute
                && current.CanTakeControl == expected.CanTakeControl;
        }
    }
}
