using Game.Components;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    internal static class AssistantObjectiveProjectionUtility
    {
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
