using Game.Components;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    internal static class AssistantObjectiveProjectionUtility
    {
        public static bool UpdateHud(
            EntityManager em, Entity boundary, DynamicBuffer<MatchObjectiveRuntimeElement> objectives,
            int count, int elapsedWholeSeconds)
        {
            if (!em.HasComponent<UiMatchHudStatusSurfacesComponent>(boundary)) return true;
            UiMatchHudStatusSurfacesComponent hud = em.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
            FixedString64Bytes row0 = count > 0 ? objectives[0].Title : default;
            FixedString64Bytes row1 = count > 1 ? objectives[1].Title : default;
            FixedString64Bytes row2 = count > 2 ? objectives[2].Title : default;
            UiMatchHudObjectiveIconKind icon0 = IconFor(objectives, count, 0);
            UiMatchHudObjectiveIconKind icon1 = IconFor(objectives, count, 1);
            UiMatchHudObjectiveIconKind icon2 = IconFor(objectives, count, 2);
            FixedString32Bytes elapsed = new("TIME ");
            elapsed.Append(math.max(0, elapsedWholeSeconds) / 60);
            elapsed.Append(':');
            int seconds = math.max(0, elapsedWholeSeconds) % 60;
            if (seconds < 10) elapsed.Append('0');
            elapsed.Append(seconds);
            if (hud.Objective0Text.Equals(row0) && hud.Objective1Text.Equals(row1) &&
                hud.Objective2Text.Equals(row2) && hud.Objective0IconKind == icon0 &&
                hud.Objective1IconKind == icon1 && hud.Objective2IconKind == icon2 && hud.ElapsedText.Equals(elapsed))
                return true;
            hud.ObjectivesTitle = new FixedString32Bytes("OBJECTIVES");
            hud.Objective0Text = row0; hud.Objective1Text = row1; hud.Objective2Text = row2;
            hud.Objective0IconKind = icon0; hud.Objective1IconKind = icon1; hud.Objective2IconKind = icon2;
            hud.ElapsedText = elapsed;
            em.SetComponentData(boundary, hud);
            return false;
        }

        public static void ClearHud(EntityManager em, Entity boundary)
        {
            if (!em.HasComponent<UiMatchHudStatusSurfacesComponent>(boundary)) return;
            UiMatchHudStatusSurfacesComponent hud = em.GetComponentData<UiMatchHudStatusSurfacesComponent>(boundary);
            if (hud.Objective0Text.IsEmpty && hud.Objective1Text.IsEmpty && hud.Objective2Text.IsEmpty &&
                hud.ElapsedText.IsEmpty) return;
            hud.Objective0Text = default; hud.Objective1Text = default; hud.Objective2Text = default;
            hud.Objective0IconKind = UiMatchHudObjectiveIconKind.Unchecked;
            hud.Objective1IconKind = UiMatchHudObjectiveIconKind.Unchecked;
            hud.Objective2IconKind = UiMatchHudObjectiveIconKind.Unchecked;
            hud.ElapsedText = default;
            em.SetComponentData(boundary, hud);
        }

        private static UiMatchHudObjectiveIconKind IconFor(
            DynamicBuffer<MatchObjectiveRuntimeElement> objectives, int count, int index) =>
            index < count && objectives[index].State == MatchObjectiveState.Complete
                ? UiMatchHudObjectiveIconKind.Checked : UiMatchHudObjectiveIconKind.Unchecked;

        public static AssistantGoalReadModelElement ToGoal(
            in MatchObjectiveRuntimeElement objective,
            uint sourceVersion)
        {
            return new AssistantGoalReadModelElement
            {
                GoalId = objective.GoalId,
                SourceVersion = (int)sourceVersion,
                ObjectiveId = objective.ObjectiveId,
                OperationMapAnchorId = objective.OperationMapAnchorId,
                State = (AssistantGoalState)objective.State,
                Priority = (AssistantMessagePriority)math.min(
                    (int)AssistantMessagePriority.Critical,
                    (int)objective.Priority),
                Title = objective.Title,
                Body = objective.Body,
                TargetEntity = objective.TargetEntity,
                TargetCell = objective.TargetCell,
                WorldPosition = objective.WorldPosition,
                IsPrimary = objective.IsPrimary,
                HasTargetCell = objective.HasTargetCell,
                HasWorldPosition = objective.HasWorldPosition
            };
        }

        public static bool TryBuildAnchorFocus(
            in AssistantGoalReadModelElement goal,
            out AssistantRecommendationElement recommendation)
        {
            recommendation = default;
            if (goal.OperationMapAnchorId.IsEmpty)
                return false;

            recommendation = new AssistantRecommendationElement
            {
                RecommendationId = 1000 + goal.GoalId,
                SourceVersion = goal.SourceVersion,
                Kind = AssistantRecommendationKind.CameraFocus,
                Priority = goal.Priority,
                TargetKind = AssistantTargetKind.Objective,
                TargetId = goal.OperationMapAnchorId,
                Score = 80f,
                Title = new FixedString64Bytes("Focus objective"),
                Reason = goal.Body,
                ActionLabel = new FixedString64Bytes("SHOW ME"),
                CanShow = 1
            };
            return true;
        }
    }
}
