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
        private static readonly FixedString64Bytes PowerRelayMission="saga.ch02.m04.power_relay";
        private bool TryUpdatePowerRelayGuidance(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(PowerRelayMission))return false;
            var em=system.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None||!em.HasComponent<CampaignMissionPowerRelayState>(root)||!em.HasBuffer<CampaignMissionPowerRelayMember>(root))
            {ClearDefenseGuidance(em,root,current);return true;}
            var power=em.GetComponentData<CampaignMissionPowerRelayState>(root);
            if(power.Ready==0||power.Failure!=PowerRelayFailure.None||!power.SessionToken.Equals(runtime.SessionToken)||power.AttemptOrdinal!=runtime.AttemptOrdinal||power.SourceVersion!=runtime.SourceVersion)
            {ClearDefenseGuidance(em,root,current);return true;}
            var members=em.GetBuffer<CampaignMissionPowerRelayMember>(root,true);Entity actor=Entity.Null,target=Entity.Null;
            // Keep runtime progress aligned with the authored five-step field guide:
            // protected waypoint, school shelter, Fuel delivery, repair hold, secure relay.
            int step=power.SafeRouteConfirmed==0?1:power.FamiliesSheltered==0?2:power.FuelDelivered==0?3:power.PowerRestored==0?4:5;
            if(step==5)
            {
                foreach(var m in members)if(m.Kind==4&&PowerLive(em,m.Entity)){target=m.Entity;break;}
            }
            if(step is 1 or 2)
            {
                foreach(var m in members)if(m.Kind==2&&PowerLive(em,m.Entity)){actor=m.Entity;break;}
            }
            else if(step==3)
            {
                foreach(var m in members)if(m.Kind==3&&PowerLive(em,m.Entity)){actor=m.Entity;break;}
            }
            else if(step==4)
            {
                foreach(var m in members)if(m.Kind==1&&PowerLive(em,m.Entity)&&math.distancesq(em.GetComponentData<LocalTransform>(m.Entity).Position.xz,new float2(power.RepairCell.x,power.RepairCell.y))>36){actor=m.Entity;break;}
                if(actor==Entity.Null)foreach(var m in members)if(m.Kind==1&&PowerLive(em,m.Entity)){actor=m.Entity;break;}
            }
            else foreach(var m in members)if(m.Kind==0&&PowerLive(em,m.Entity)){actor=m.Entity;break;}
            if(actor==Entity.Null){ClearDefenseGuidance(em,root,current);return true;}
            float3 destination=step switch
            {
                1=>new float3(power.SafeRouteCell.x,0,power.SafeRouteCell.y),
                2=>new float3(power.ShelterCell.x,0,power.ShelterCell.y),
                3 or 4=>new float3(power.RepairCell.x,0,power.RepairCell.y),
                _=>em.GetComponentData<LocalTransform>(actor).Position
            };
            if(target!=Entity.Null&&em.HasComponent<LocalTransform>(target))destination=em.GetComponentData<LocalTransform>(target).Position;
            bool moving=em.HasComponent<UnitPathRequest>(actor)||em.HasComponent<UnitPathFollow>(actor);
            bool waiting=step==5&&target==Entity.Null||moving||step==4&&math.distancesq(em.GetComponentData<LocalTransform>(actor).Position.xz,destination.xz)<=36;
            FixedString64Bytes title="mission.power_relay.guide.";title.Append(step);title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body="mission.power_relay.guide.";body.Append(step);body.Append(ExtractionBodySuffix);
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=78000+step,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=step switch {1=>CampaignMissionGuidancePromptKind.PowerRoute,2=>CampaignMissionGuidancePromptKind.PowerFuel,3=>CampaignMissionGuidancePromptKind.PowerRepair,4=>CampaignMissionGuidancePromptKind.PowerDefend,_=>CampaignMissionGuidancePromptKind.PowerHold},GuidanceMode=runtime.Guidance,Active=1,Priority=step==5?AssistantMessagePriority.Critical:AssistantMessagePriority.High,
                RecommendationKind=waiting?AssistantRecommendationKind.Explain:step==5?AssistantRecommendationKind.Attack:AssistantRecommendationKind.Move,
                TargetKind=target!=Entity.Null?AssistantTargetKind.Entity:AssistantTargetKind.WorldPosition,SourceEntity=actor,TargetEntity=target,WorldPosition=destination,HasWorldPosition=1,
                Title=title,Body=body,ActionLabel=waiting?WaitAction:RadarAct,CanShow=1,CanExecute=waiting?(byte)0:(byte)1,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(current,next)||current.MissionSourceVersion!=next.MissionSourceVersion)em.SetComponentData(root,next);return true;
        }
        private static bool PowerLive(EntityManager em,Entity entity)=>em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)&&em.GetComponentData<UnitHealth>(entity).Current>0&&em.HasComponent<LocalTransform>(entity);
    }
}
