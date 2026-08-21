using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    public partial struct AssistantCommandIntentSystem
    {
        private bool TryQueueMoveCommand(
            ref SystemState state,
            AssistantCommandIntentRequestElement request,
            out int downstreamRequestId,
            out TacticalCommandReasonCode reason)
        {
            downstreamRequestId = 0;
            reason = TacticalCommandReasonCode.None;
            EntityManager entityManager = state.EntityManager;
            if (!TryValidatePlayerSource(entityManager, request.SourceEntity, out reason))
                return false;
            if (gridQuery.IsEmptyIgnoreFilter)
            {
                reason = TacticalCommandReasonCode.CommandUnavailable;
                return false;
            }

            GridConfig grid = gridQuery.GetSingleton<GridConfig>();
            if (!TryResolveMoveTargetCell(request, grid, out int2 targetCell))
            {
                reason = TacticalCommandReasonCode.TargetOutOfBounds;
                return false;
            }

            bool guidedMove = CampaignMissionGuidedMoveRouteUtility.IsGuidedMovePhaseActive(entityManager);
            downstreamRequestId = guidedMove
                ? CampaignMissionGuidedMoveRouteUtility.TryCreateContext(
                    entityManager, grid, targetCell, out _)
                    ? UnitMoveOrderRequestSystem.EnqueueCampaignGuidedSquadMoveOrder(
                        entityManager, request.SourceEntity, targetCell, UnityEngine.Time.frameCount)
                    : 0
                : UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(
                    entityManager, request.SourceEntity, targetCell);
            return downstreamRequestId > 0;
        }

        private static bool TryResolveMoveTargetCell(
            AssistantCommandIntentRequestElement request,
            in GridConfig grid,
            out int2 targetCell)
        {
            targetCell = request.TargetKind switch
            {
                AssistantTargetKind.Cell => request.TargetCell,
                AssistantTargetKind.WorldPosition when IsFinite(request.WorldPosition) =>
                    GridUtils.WorldToCell(grid, request.WorldPosition),
                _ => new int2(-1, -1)
            };
            return targetCell.x >= 0 && targetCell.y >= 0 &&
                   targetCell.x < grid.Width && targetCell.y < grid.Height;
        }
    }
}
