using Game.Components;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        private static bool TryResolveRouteAssaultSelection(EntityManager em,Entity root,
            CampaignMissionGuidanceProjectionComponent guidance,out UiMissionTutorialTarget target)
        {
            target=default;
            if(guidance.Prompt is not (CampaignMissionGuidancePromptKind.RouteBreach or
                CampaignMissionGuidancePromptKind.RouteGarrison or CampaignMissionGuidancePromptKind.RouteRecords)||
                !em.HasBuffer<CampaignMissionRouteReopenedMember>(root))return false;
            Vector3 min=new(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
            Vector3 max=new(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
            int alive=0,selected=0;
            foreach(var member in em.GetBuffer<CampaignMissionRouteReopenedMember>(root,true))
            {
                var unit=member.Entity;
                if(member.Kind!=0||!em.Exists(unit)||!em.HasComponent<UnitHealth>(unit)||
                   em.GetComponentData<UnitHealth>(unit).Current<=0||!em.HasComponent<LocalTransform>(unit))continue;
                Vector3 position=em.GetComponentData<LocalTransform>(unit).Position;
                min=Vector3.Min(min,position);max=Vector3.Max(max,position);alive++;
                if(em.HasComponent<SelectedUnitTag>(unit))selected++;
            }
            if(alive==0)return false;
            bool attacking=guidance.Prompt==CampaignMissionGuidancePromptKind.RouteGarrison&&
                IsTutorialAttackInProgress(em,guidance.SourceEntity,guidance.TargetEntity);
            target=new UiMissionTutorialTarget((min+max)*.5f,guidance.WorldPosition,selected<alive,
                IsTutorialActorMoving(em,guidance.SourceEntity),alive,
                guidance.CanExecute==0?UiTutorialBattleAction.Watch:
                    guidance.RecommendationKind==AssistantRecommendationKind.Attack?UiTutorialBattleAction.Attack:UiTutorialBattleAction.Move,
                executingAttack:attacking,areaRadius:5,dragSelection:alive>1,
                selectionMin:min-new Vector3(3,0,3),selectionMax:max+new Vector3(3,0,3));
            return true;
        }
    }
}
