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
        private static readonly FixedString64Bytes GridlockMissionId="saga.ch02.m01.gridlock";
        private bool TryUpdateGridlockGuidance(ref SystemState state,Entity root,in CampaignMissionRuntimeComponent runtime,
            in AssistantSettingsComponent settings,in CampaignMissionGuidanceProjectionComponent current)
        {
            if(!runtime.MissionId.Equals(GridlockMissionId))return false;
            var em=state.EntityManager;
            if(runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None ||
                !em.HasComponent<CampaignMissionGridlockState>(root) || !em.HasBuffer<CampaignMissionGridlockWorkSite>(root))
            {ClearDefenseGuidance(em,root,current);return true;}
            var g=em.GetComponentData<CampaignMissionGridlockState>(root);
            var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root,true);
            if(g.Ready==0 || g.Failure!=GridlockFailure.None || sites.Length!=2 ||
                !g.SessionToken.Equals(runtime.SessionToken) || g.AttemptOrdinal!=runtime.AttemptOrdinal || g.SourceVersion!=runtime.SourceVersion)
            {ClearDefenseGuidance(em,root,current);return true;}
            int siteIndex=sites[0].Complete==0?0:1;
            var site=sites[siteIndex];
            bool both=sites[0].Complete!=0 && sites[1].Complete!=0;
            float3 destination=both?g.HospitalCenter:site.Center;
            int step=siteIndex==0?1:5;
            Entity actor=Entity.Null;
            bool waiting=false;
            // Decisions use only friendly positions, public work states and the public relief position.
            // No hostile entity, hidden schedule, route solution or opponent health is read here.
            var members=em.GetBuffer<CampaignMissionGridlockMember>(root,true);
            if(g.RouteContested!=0)
            {
                destination=sites[0].Contested!=0?sites[0].Center:sites[1].Center;
                step=9;
                actor=GridlockActorOutside(em,members,GridlockMemberKind.Rifle,destination,12);
            }
            else if(both)
            {
                step=9;
                if(em.HasComponent<LocalTransform>(g.Vehicle)) destination=em.GetComponentData<LocalTransform>(g.Vehicle).Position;
                actor=GridlockActorOutside(em,members,GridlockMemberKind.Rifle,destination,14);
                if(g.VehicleArrived!=0) {step=10;destination=g.HospitalCenter;}
            }
            else
            {
                actor=GridlockActorOutside(em,members,GridlockMemberKind.Rifle,destination,12);
                if(actor==Entity.Null && site.Status!=GridlockWorkStatus.ThreatNearby)
                {
                    step=siteIndex==0?2:6;actor=GridlockActorOutside(em,members,GridlockMemberKind.Fadi,destination,4);
                    if(actor==Entity.Null)
                    {
                        step=siteIndex==0?3:7;
                        actor=GridlockWorkerNeeded(em,members,destination,4);
                        if(actor==Entity.Null)step=siteIndex==0?4:8;
                    }
                }
            }
            if(actor==Entity.Null)
            {
                waiting=true;
                foreach(var member in members)
                    if(member.Kind==GridlockMemberKind.Rifle && GridlockLiveFriendly(em,member.Entity)){actor=member.Entity;break;}
            }
            if(actor==Entity.Null){ClearDefenseGuidance(em,root,current);return true;}
            FixedString64Bytes title="mission.gridlock.tutorial.";title.Append(step);title.Append(ExtractionTitleSuffix);
            FixedString128Bytes body="mission.gridlock.tutorial.";body.Append(step);body.Append(ExtractionBodySuffix);
            var next=new CampaignMissionGuidanceProjectionComponent {GuidanceId=75000+step,Version=Next(current.Version),MissionSourceVersion=runtime.Version,
                Prompt=(CampaignMissionGuidancePromptKind)(44+step),GuidanceMode=runtime.Guidance,Active=1,Priority=AssistantMessagePriority.High,
                RecommendationKind=waiting?AssistantRecommendationKind.Explain:AssistantRecommendationKind.Move,
                TargetKind=AssistantTargetKind.WorldPosition,SourceEntity=actor,WorldPosition=destination,HasWorldPosition=1,
                Title=title,Body=body,ActionLabel=RadarAct,CanShow=1,CanExecute=waiting?(byte)0:(byte)1,
                SubtitlesEnabled=settings.SubtitlesEnabled,LargeTextEnabled=settings.LargeTextEnabled,HighContrastEnabled=settings.HighContrastEnabled};
            if(!ProjectionEquals(current,next) || current.MissionSourceVersion!=next.MissionSourceVersion)em.SetComponentData(root,next);
            return true;
        }
        private static bool GridlockLiveFriendly(EntityManager em,Entity actor)=>em.Exists(actor) && em.HasComponent<UnitHealth>(actor) &&
            em.GetComponentData<UnitHealth>(actor).Current>0 && em.HasComponent<LocalTransform>(actor);
        private static Entity GridlockActorOutside(EntityManager em,DynamicBuffer<CampaignMissionGridlockMember> members,GridlockMemberKind kind,float3 target,float radius)
        {
            foreach(var member in members)
                if(member.Kind==kind && GridlockLiveFriendly(em,member.Entity) &&
                    math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz,target.xz)>radius*radius)return member.Entity;
            return Entity.Null;
        }
        private static Entity GridlockWorkerNeeded(EntityManager em,DynamicBuffer<CampaignMissionGridlockMember> members,float3 target,float radius)
        {
            Entity actor=Entity.Null;
            foreach(var member in members)
            {
                if(member.Kind!=GridlockMemberKind.Worker || !GridlockLiveFriendly(em,member.Entity))continue;
                if(math.distancesq(em.GetComponentData<LocalTransform>(member.Entity).Position.xz,target.xz)<=radius*radius)return Entity.Null;
                if(actor==Entity.Null)actor=member.Entity;
            }
            return actor;
        }
    }
}
