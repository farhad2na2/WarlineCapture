using Game.Components;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct SelectedMoveOrderCommandSystem
    {
        private static bool TryIssueCampaignGuidedMove(
            EntityManager entityManager,
            in GridConfig grid,
            NativeArray<Entity> selectedEntities,
            int2 requestedGoal,
            int currentFrame,
            byte factionId,
            out Result result)
        {
            result = default;
            if (!CampaignMissionGuidedMoveRouteUtility.IsGuidedMovePhaseActive(entityManager))
                return false;

            if (!CampaignMissionGuidedMoveRouteUtility.TryCreateContext(
                    entityManager,
                    grid,
                    requestedGoal,
                    out CampaignMissionGuidedMoveRouteUtility.Context guidedContext) ||
                !CampaignMissionGuidedSquadMoveUtility.TryIssue(
                    entityManager,
                    selectedEntities[0],
                    guidedContext.TargetCell,
                    currentFrame,
                    out _))
            {
                // An active M01 tutorial move owns this input. Never fall back to the ordinary
                // selected-unit pathfinder, which can move only a stale partial selection.
                result = Result.Rejected(TacticalCommandReasonCode.TargetBlocked);
                return true;
            }

            result = Result.Success(
                guidedContext.TargetCell,
                GridUtils.CellToWorldCenter(grid, guidedContext.TargetCell),
                factionId);
            return true;
        }

        private static bool IsAlreadyMovingToGoal(EntityManager entityManager, Entity entity, int2 goal)
        {
            if (!entityManager.Exists(entity))
                return false;

            bool sameTarget =
                entityManager.HasComponent<UnitTarget>(entity) &&
                entityManager.GetComponentData<UnitTarget>(entity).Cell.Equals(goal);
            bool samePendingRequest =
                entityManager.HasComponent<UnitPathRequest>(entity) &&
                entityManager.GetComponentData<UnitPathRequest>(entity).Goal.Equals(goal);
            bool hasActiveMovement =
                entityManager.HasComponent<UnitPathFollow>(entity) ||
                entityManager.HasComponent<UnitPathRequest>(entity);

            return sameTarget && (samePendingRequest || hasActiveMovement);
        }
    }
}
