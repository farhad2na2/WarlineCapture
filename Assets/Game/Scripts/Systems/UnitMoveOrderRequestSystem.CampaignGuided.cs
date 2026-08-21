using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct UnitMoveOrderRequestSystem
    {
        public static int EnqueueCampaignGuidedSquadMoveOrder(
            EntityManager entityManager,
            Entity sourceEntity,
            int2 goal,
            int currentFrame)
        {
            return EnqueueMoveOrder(
                entityManager,
                sourceEntity,
                goal,
                UnitMoveOrderRequestKind.CampaignGuidedSquad,
                issueGroundPathNow: false,
                useGroundPathRetryCooldown: false,
                resumeFrame: 0,
                currentFrame: currentFrame);
        }

        private static UnitMoveOrderSystem.MoveOrderCommandResult IssueCampaignGuidedSquadMove(
            EntityManager entityManager,
            UnitMoveOrderRequestElement request)
        {
            return CampaignMissionGuidedSquadMoveUtility.TryIssue(
                entityManager,
                request.Entity,
                request.Goal,
                request.CurrentFrame,
                out UnitMoveOrderSystem.MoveOrderCommandResult result)
                    ? result
                    : default;
        }
    }
}
