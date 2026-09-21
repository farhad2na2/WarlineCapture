using Game.Components;
using Game.UI.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static EntityQuery tutorialSelectionBoundsQuery;
        private static bool hasTutorialSelectionBoundsQuery;

        private static UiMissionTutorialTarget WithTutorialSelectionBounds(in UiMissionTutorialTarget target)
        {
            if(!TryGetMissionRoot(out var em,out var root))return target;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            var actor=ResolveTutorialSelectionActor(em,root,guidance,target.Selection);
            if(!em.Exists(actor) || !em.HasComponent<CampaignMissionUnitRoleComponent>(actor))return target;
            var role=em.GetComponentData<CampaignMissionUnitRoleComponent>(actor);
            if(role.UnitGroupId.IsEmpty)return target;
            float3 min=new(float.MaxValue),max=new(float.MinValue);int count=0;
            if (!hasTutorialSelectionBoundsQuery)
            {
                tutorialSelectionBoundsQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                    ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<LocalTransform>());
                hasTutorialSelectionBoundsQuery = true;
            }
            using var units=tutorialSelectionBoundsQuery.ToEntityArray(Allocator.Temp);
            foreach(var unit in units)
            {
                var member=em.GetComponentData<CampaignMissionUnitRoleComponent>(unit);
                if(!member.SessionToken.Equals(role.SessionToken) || !member.UnitGroupId.Equals(role.UnitGroupId) ||
                    !member.MissionRoleId.Equals(role.MissionRoleId) || !CanSelectTutorialActor(em,unit) || em.HasComponent<UnitTransportPassenger>(unit))continue;
                var position=em.GetComponentData<LocalTransform>(unit).Position;
                min=math.min(min,position);max=math.max(max,position+new float3(0,2,0));count++;
            }
            if(count<2)return target;
            return new UiMissionTutorialTarget(target.Selection,target.Destination,target.NeedsSelection,target.Moving,count,
                target.BattleAction,target.SelectionLabelKey,target.ExecutingAttack,target.AreaRadius,true,min,max);
        }
    }
}
