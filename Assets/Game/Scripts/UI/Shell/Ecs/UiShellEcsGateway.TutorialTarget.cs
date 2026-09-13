using Game.Components;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway : IUiMissionTutorialTargetGateway
    {
        public bool TryReadMissionTutorialTarget(out UiMissionTutorialTarget target)
        {
            target=default;
            if (!TryGetMissionRoot(out var em,out var root)) return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if(runtime.Phase!=MissionPhaseKind.Engage || runtime.Outcome!=MissionOutcomeKind.None ||
                !em.HasComponent<CampaignMissionGuidanceProjectionComponent>(root)) return false;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            if(guidance.Active==0) return false;
            if(runtime.MissionId.Equals(AirliftId) && em.HasComponent<CampaignMissionExtractionState>(root))
                return ResolveExtractionTutorialTarget(em,root,guidance.GuidanceId-55000,out target);
            if(runtime.MissionId.Equals(BreachId) && em.Exists(guidance.SourceEntity) && em.HasComponent<LocalTransform>(guidance.SourceEntity))
            {
                target=new UiMissionTutorialTarget(em.GetComponentData<LocalTransform>(guidance.SourceEntity).Position,guidance.WorldPosition,
                    !em.HasComponent<SelectedUnitTag>(guidance.SourceEntity),IsTutorialActorMoving(em,guidance.SourceEntity));return true;
            }
            if(guidance.GuidanceId<45001 || guidance.GuidanceId>45012 || !em.Exists(guidance.SourceEntity) ||
                !em.HasComponent<LocalTransform>(guidance.SourceEntity)) return false;
            var actor=guidance.SourceEntity;
            target=new UiMissionTutorialTarget(em.GetComponentData<LocalTransform>(actor).Position,guidance.WorldPosition,
                !em.HasComponent<SelectedUnitTag>(actor),IsTutorialActorMoving(em,actor));
            return true;
        }

        internal static bool ResolveExtractionTutorialTarget(EntityManager em,Entity root,int step,out UiMissionTutorialTarget target)
        {
            target=default;
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            float3 team=default; int count=0,selected=0; bool moving=false;
            var members=em.GetBuffer<CampaignMissionExtractionMember>(root,true);
            foreach(var member in members)
            {
                if(member.Kind!=1 || !TryGetLiveExtractionPosition(em,member.Entity,out var position)) continue;
                if(em.HasComponent<UnitTransportPassenger>(member.Entity) &&
                    TryGetLiveExtractionPosition(em,em.GetComponentData<UnitTransportPassenger>(member.Entity).Transport,out var aboard)) position=aboard;
                team+=position; count++;
                if(em.HasComponent<SelectedUnitTag>(member.Entity)) selected++;
                moving|=IsTutorialActorMoving(em,member.Entity);
            }
            if(count==0) return false;
            team/=count;
            bool teamStep=step is 4 or 5 or 9;
            Entity actor=step is 8 or 9 or 11 or 12 ? extraction.Aircraft : extraction.Carrier;
            if(!TryGetLiveExtractionPosition(em,actor,out var actorPosition)) return false;
            float3 destination=step is 6 or 7 or 10 ? extraction.LandingCenter : step>=11 ? extraction.DepartureCenter : team;
            if(step==5) destination=actorPosition;
            if(step==9 && !TryGetLiveExtractionPosition(em,extraction.Aircraft,out destination)) return false;
            target=new UiMissionTutorialTarget(teamStep ? team : actorPosition,destination,
                teamStep ? selected!=count : !em.HasComponent<SelectedUnitTag>(actor),
                teamStep ? moving : IsTutorialActorMoving(em,actor));
            return true;
        }

        private static bool IsTutorialActorMoving(EntityManager em,Entity actor) =>
            em.HasComponent<UnitPathRequest>(actor) || em.HasComponent<UnitPathFollow>(actor);
    }
}
