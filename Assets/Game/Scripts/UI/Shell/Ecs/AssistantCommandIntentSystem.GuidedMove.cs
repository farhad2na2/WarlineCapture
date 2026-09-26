using Game.Components;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.UI.Shell.Ecs
{
    public partial struct AssistantCommandIntentSystem
    {
        private static readonly FixedString64Bytes RouteReopenedMissionId = "saga.ch02.m05.route_reopened";

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
            if (downstreamRequestId > 0)
                TryQueueRouteReopenedRepairPartner(entityManager, request.SourceEntity, targetCell);
            return downstreamRequestId > 0;
        }

        private static void TryQueueRouteReopenedRepairPartner(
            EntityManager entityManager,
            Entity sourceEntity,
            int2 targetCell)
        {
            using EntityQuery runtimeQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CampaignMissionRuntimeComponent, CampaignMissionRouteReopenedState, CampaignMissionRouteReopenedMember>()
                .Build(entityManager);
            if (runtimeQuery.CalculateEntityCount() != 1)
                return;

            Entity root = runtimeQuery.GetSingletonEntity();
            CampaignMissionRuntimeComponent runtime = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionRouteReopenedState route = entityManager.GetComponentData<CampaignMissionRouteReopenedState>(root);
            if (!runtime.MissionId.Equals(RouteReopenedMissionId) ||
                runtime.Phase != MissionPhaseKind.Engage || route.LinkRestored != 0 ||
                !targetCell.Equals(route.DisruptedLinkCell))
                return;

            DynamicBuffer<CampaignMissionRouteReopenedMember> members =
                entityManager.GetBuffer<CampaignMissionRouteReopenedMember>(root, true);
            bool sourceIsEngineer = false;
            Entity partner = Entity.Null;
            for (int i = 0; i < members.Length; i++)
            {
                CampaignMissionRouteReopenedMember member = members[i];
                if (member.Kind != 1 || member.Dead != 0 || !entityManager.Exists(member.Entity))
                    continue;
                if (member.Entity == sourceEntity)
                    sourceIsEngineer = true;
                else
                    partner = member.Entity;
            }
            if (!sourceIsEngineer || partner == Entity.Null)
                return;

            UnitMoveOrderRequestSystem.EnqueueImmediateMoveOrder(
                entityManager, partner, targetCell + new int2(6, 0));
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
