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
        private static readonly FixedString64Bytes MarketMission="saga.ch02.m03.market_lifeline";
        private bool TryUpdateMarketLifelineGuidance(ref SystemState system,Entity root,in CampaignMissionRuntimeComponent runtime,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(MarketMission))return false;
            var em=system.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage||runtime.Outcome!=MissionOutcomeKind.None||!em.HasComponent<CampaignMissionMarketLifelineState>(root)||!em.HasBuffer<CampaignMissionMarketLifelineMember>(root))
            {ClearDefenseGuidance(em,root,current);return true;}
            var market=em.GetComponentData<CampaignMissionMarketLifelineState>(root);
            if(market.Ready==0||market.Failure!=MarketLifelineFailure.None||!market.SessionToken.Equals(runtime.SessionToken)||market.AttemptOrdinal!=runtime.AttemptOrdinal||market.SourceVersion!=runtime.SourceVersion)
            {ClearDefenseGuidance(em,root,current);return true;}
            var members=em.GetBuffer<CampaignMissionMarketLifelineMember>(root,true);Entity actor=Entity.Null,target=Entity.Null;
            int step=market.LegitimateManifestInspected==0?1:market.CorruptManifestInspected==0?2:market.DeliveredCount<3?3:4;
            if(step==4)
            {
                foreach(var m in members)if(m.Kind==2&&MarketLive(em,m.Entity)){target=m.Entity;break;}
                if(target==Entity.Null)step=5;
            }
            if(step==3)
            {
                foreach(var m in members)if(m.Kind==1&&m.Delivered==0&&MarketLive(em,m.Entity)){actor=m.Entity;break;}
            }
            else foreach(var m in members)if(m.Kind==0&&MarketLive(em,m.Entity)){actor=m.Entity;break;}
            if(actor==Entity.Null){ClearDefenseGuidance(em,root,current);return true;}
            float3 destination=step switch {1=>new float3(market.ManifestCell.x,0,market.ManifestCell.y),2=>new float3(market.CorruptManifestCell.x,0,market.CorruptManifestCell.y),3=>new float3(market.DeliveryCell.x,0,market.DeliveryCell.y),_=>em.GetComponentData<LocalTransform>(actor).Position};
            if(target!=Entity.Null&&em.HasComponent<LocalTransform>(target))destination=em.GetComponentData<LocalTransform>(target).Position;
            bool moving=em.HasComponent<UnitPathRequest>(actor)||em.HasComponent<UnitPathFollow>(actor);
            bool waiting=step==5||moving||(step==1||step==2)&&math.distancesq(em.GetComponentData<LocalTransform>(actor).Position.xz,destination.xz)<=36;
            FixedString64Bytes title="mission.market_lifeline.guide.";title.Append(step);title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body="mission.market_lifeline.guide.";body.Append(step);body.Append(ExtractionBodySuffix);
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=77000+step,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=step switch {1=>CampaignMissionGuidancePromptKind.MarketInspectLegitimate,2=>CampaignMissionGuidancePromptKind.MarketInspectCorrupt,3=>CampaignMissionGuidancePromptKind.MarketEscort,4=>CampaignMissionGuidancePromptKind.MarketDefend,_=>CampaignMissionGuidancePromptKind.MarketHold},GuidanceMode=runtime.Guidance,Active=1,Priority=step==4?AssistantMessagePriority.Critical:AssistantMessagePriority.High,
                RecommendationKind=waiting?AssistantRecommendationKind.Explain:step==4?AssistantRecommendationKind.Attack:AssistantRecommendationKind.Move,
                TargetKind=target!=Entity.Null?AssistantTargetKind.Entity:AssistantTargetKind.WorldPosition,SourceEntity=actor,TargetEntity=target,WorldPosition=destination,HasWorldPosition=1,
                Title=title,Body=body,ActionLabel=waiting?WaitAction:RadarAct,CanShow=1,CanExecute=waiting?(byte)0:(byte)1,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(current,next)||current.MissionSourceVersion!=next.MissionSourceVersion)em.SetComponentData(root,next);return true;
        }
        private static bool MarketLive(EntityManager em,Entity entity)=>em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)&&em.GetComponentData<UnitHealth>(entity).Current>0&&em.HasComponent<LocalTransform>(entity);
    }
}
