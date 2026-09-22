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
        private static readonly FixedString64Bytes SupplyLineGuidanceMission = "saga.ch02.m02.supply_line";
        private bool TryUpdateSupplyLineGuidance(ref SystemState state,Entity root,in CampaignMissionRuntimeComponent runtime,in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(SupplyLineGuidanceMission))return false;
            var em=state.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None || !em.HasComponent<CampaignMissionSupplyLineState>(root) || !em.HasBuffer<CampaignMissionSupplyLineLink>(root))
            {ClearDefenseGuidance(em,root,current);return true;}
            var supply=em.GetComponentData<CampaignMissionSupplyLineState>(root);var links=em.GetBuffer<CampaignMissionSupplyLineLink>(root,true);
            if(supply.Ready==0 || supply.Failure!=SupplyLineFailure.None || links.Length!=3 || !supply.SessionToken.Equals(runtime.SessionToken) || supply.AttemptOrdinal!=runtime.AttemptOrdinal || supply.SourceVersion!=runtime.SourceVersion)
            {ClearDefenseGuidance(em,root,current);return true;}
            bool recovery=supply.OilTransferred!=0 && supply.RouteRecovered==0;
            int step=supply.OilTransferred==0?1:recovery || supply.FuelTransferred==0?2:supply.StoredFuel<40 || supply.AllocatedCivilianBarrels==0?3:4;
            int linkIndex=step==1?0:step==2?1:2;
            Entity actor=Entity.Null;float3 destination=new float3(links[linkIndex].Origin.x,0,links[linkIndex].Origin.y-8);
            var members=em.GetBuffer<CampaignMissionSupplyLineMember>(root,true);
            // Friendly chain facts and public objective locations only. No enemy roster or hidden schedule.
            bool wait=true;
            foreach(var m in members)
            {
                if(recovery)
                {
                    if(m.Kind!=1 || !GridlockLiveFriendly(em,m.Entity))continue;
                    actor=m.Entity;destination=new float3(supply.AlternateLane.x,0,supply.AlternateLane.y);
                    var cargo=em.GetComponentData<UnitResourceHauler>(actor);
                    wait=supply.RerouteOrdered!=0;break;
                }
                if(m.Kind!=0 || !GridlockLiveFriendly(em,m.Entity))continue;
                if(actor==Entity.Null)actor=m.Entity;
                if(math.distancesq(em.GetComponentData<LocalTransform>(m.Entity).Position.xz,destination.xz)>14*14){actor=m.Entity;wait=false;break;}
            }
            if(actor==Entity.Null){ClearDefenseGuidance(em,root,current);return true;}
            FixedString64Bytes title="mission.supply_line.guide.";title.Append(step);title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body="mission.supply_line.guide.";body.Append(step);body.Append(ExtractionBodySuffix);
            if(recovery){title="mission.supply_line.guide.recovery.title";body="mission.supply_line.guide.recovery.body";}
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=76000+step,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(54+step),GuidanceMode=runtime.Guidance,Active=1,Priority=AssistantMessagePriority.High,
                RecommendationKind=wait?AssistantRecommendationKind.Explain:AssistantRecommendationKind.Move,TargetKind=AssistantTargetKind.WorldPosition,
                SourceEntity=actor,WorldPosition=destination,HasWorldPosition=1,Title=title,Body=body,ActionLabel=RadarAct,CanShow=1,CanExecute=wait?(byte)0:(byte)1,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(current,next) || current.MissionSourceVersion!=next.MissionSourceVersion)em.SetComponentData(root,next);
            return true;
        }
    }
}
