using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionGuidanceProjectionSystem
    {
        private static readonly FixedString64Bytes RouteReopenedMission="saga.ch02.m05.route_reopened";
        private bool TryUpdateRouteReopenedGuidance(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(RouteReopenedMission))return false;var em=system.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None||!em.HasComponent<CampaignMissionRouteReopenedState>(root)||!em.HasBuffer<CampaignMissionRouteReopenedMember>(root)){ClearDefenseGuidance(em,root,current);return true;}
            var route=em.GetComponentData<CampaignMissionRouteReopenedState>(root);
            if(route.Ready==0||route.Failure!=RouteReopenedFailure.None||!route.SessionToken.Equals(runtime.SessionToken)||route.AttemptOrdinal!=runtime.AttemptOrdinal||route.SourceVersion!=runtime.SourceVersion){ClearDefenseGuidance(em,root,current);return true;}
            var members=em.GetBuffer<CampaignMissionRouteReopenedMember>(root,true);Entity actor=Entity.Null,target=Entity.Null;
            int step=route.ReliefDelivered==0?1:route.FuelDelivered==0?2:route.LinkRestored==0?3:route.HubEntered==0?4:route.GarrisonCleared==0?5:6;
            if(step==5)foreach(var m in members)if(m.Kind==4&&RouteLive(em,m.Entity)){target=m.Entity;break;}
            byte actorKind=step==1?(byte)2:step==2?(byte)3:step==3?(byte)1:(byte)0;int repairActorsAtLink=0;
            if(step==3)
                foreach(var m in members)
                    if(m.Kind==1&&RouteLive(em,m.Entity)&&math.distancesq(em.GetComponentData<LocalTransform>(m.Entity).Position.xz,new float2(route.DisruptedLinkCell.x,route.DisruptedLinkCell.y))<=49)
                        repairActorsAtLink++;
            foreach(var m in members)
            {
                if(m.Kind!=actorKind||!RouteLive(em,m.Entity))continue;
                // The lane repair is deliberately a two-engineer job. After ARIA moves the
                // first engineer, keep the same guidance actionable by choosing the remaining
                // engineer instead of turning the step into a passive wait.
                if(step==3&&math.distancesq(em.GetComponentData<LocalTransform>(m.Entity).Position.xz,new float2(route.DisruptedLinkCell.x,route.DisruptedLinkCell.y))<=49)continue;
                actor=m.Entity;break;
            }
            if(step==3&&actor==Entity.Null)
                foreach(var m in members)if(m.Kind==actorKind&&RouteLive(em,m.Entity)){actor=m.Entity;break;}
            if(actor==Entity.Null){ClearDefenseGuidance(em,root,current);return true;}
            float3 destination=step switch{1=>new float3(route.ReliefGoalCell.x,0,route.ReliefGoalCell.y),2=>new float3(route.FuelGoalCell.x,0,route.FuelGoalCell.y),3=>new float3(route.DisruptedLinkCell.x,0,route.DisruptedLinkCell.y),4=>new float3(route.HubGateCell.x,0,route.HubGateCell.y),_=>new float3(route.RecordsCell.x,0,route.RecordsCell.y)};
            // Keep the second 3x3 crew on the approach side of the same repair zone.
            // Six cells clears the first footprint while remaining inside the authored radius.
            if(step==3&&repairActorsAtLink==1)destination.x+=6;
            if(target!=Entity.Null&&em.HasComponent<LocalTransform>(target))destination=em.GetComponentData<LocalTransform>(target).Position;
            bool moving=em.HasComponent<UnitPathRequest>(actor)||em.HasComponent<UnitPathFollow>(actor);bool waiting=moving||step==3&&math.distancesq(em.GetComponentData<LocalTransform>(actor).Position.xz,destination.xz)<=49||step==6&&route.RecordsPreserved==0&&math.distancesq(em.GetComponentData<LocalTransform>(actor).Position.xz,destination.xz)<=36;
            FixedString64Bytes title="mission.route_reopened.guide.";title.Append(step);title.Append(ExtractionTitleSuffix);FixedString128Bytes body="mission.route_reopened.guide.";body.Append(step);body.Append(ExtractionBodySuffix);
            // Each engineer move is a separately acknowledged ARIA action. Reusing 79003
            // would hide the second move as already completed.
            int guidanceId=79000+step+(step==3?repairActorsAtLink*100:0);
            var next=new CampaignMissionGuidanceProjectionComponent{GuidanceId=guidanceId,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=step switch{1=>CampaignMissionGuidancePromptKind.RouteRelief,2=>CampaignMissionGuidancePromptKind.RouteFuel,3=>CampaignMissionGuidancePromptKind.RouteRepair,4=>CampaignMissionGuidancePromptKind.RouteBreach,5=>CampaignMissionGuidancePromptKind.RouteGarrison,_=>CampaignMissionGuidancePromptKind.RouteRecords},GuidanceMode=runtime.Guidance,Active=1,Priority=step>=5?AssistantMessagePriority.Critical:AssistantMessagePriority.High,
                RecommendationKind=waiting?AssistantRecommendationKind.Explain:step==5?AssistantRecommendationKind.Attack:AssistantRecommendationKind.Move,TargetKind=target!=Entity.Null?AssistantTargetKind.Entity:AssistantTargetKind.WorldPosition,SourceEntity=actor,TargetEntity=target,WorldPosition=destination,HasWorldPosition=1,
                Title=title,Body=body,ActionLabel=waiting?WaitAction:RadarAct,CanShow=1,CanExecute=waiting?(byte)0:(byte)1,SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(current,next)||current.MissionSourceVersion!=next.MissionSourceVersion)em.SetComponentData(root,next);return true;
        }
        private static bool RouteLive(EntityManager em,Entity entity)=>em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)&&em.GetComponentData<UnitHealth>(entity).Current>0&&em.HasComponent<LocalTransform>(entity);
    }
}
