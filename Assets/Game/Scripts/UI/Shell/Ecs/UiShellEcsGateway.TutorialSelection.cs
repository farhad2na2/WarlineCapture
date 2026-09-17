using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        public bool TrySelectMissionTutorialGroup()
        {
            if(!TryReadMissionTutorialTarget(out var target) || !target.NeedsSelection ||
                !TryGetMissionRoot(out var em,out var root)) return false;
            var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
            Entity actor=ResolveTutorialSelectionActor(em,root,guidance,(float3)target.Selection);
            if(actor==Entity.Null) return false;
            using var query=em.CreateEntityQuery(typeof(RtsSelectionInputRequestQueueComponent),typeof(RtsSelectionCommandIntentRequestElement));
            if(query.CalculateEntityCount()!=1) return false;
            bool group=em.HasComponent<CampaignMissionUnitRoleComponent>(actor) &&
                !em.GetComponentData<CampaignMissionUnitRoleComponent>(actor).UnitGroupId.IsEmpty;
            var request=new AssistantCommandIntentRequestElement {Frame=Time.frameCount,
                TargetKind=group?AssistantTargetKind.Squad:AssistantTargetKind.Entity,SourceEntity=actor,TargetEntity=actor};
            if(!AssistantSelectionCommandUtility.TryQueue(em,query.GetSingletonEntity(),in request,out _,sameMissionRole:true)) return false;
            TryFocusMissionTutorialTarget(true);
            return true;
        }

        internal static Entity ResolveTutorialSelectionActor(EntityManager em,Entity root,
            CampaignMissionGuidanceProjectionComponent guidance,float3 position)
        {
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            // Optional mission state can remain on the persistent root after returning
            // to the campaign. Only M4 owns specialist/transport selection routing.
            if(runtime.MissionId.Equals(AirliftId) && em.HasComponent<CampaignMissionExtractionState>(root))
            {
                int step=guidance.GuidanceId-55000;
                var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
                if(step is 4 or 5 or 9)
                {
                    foreach(var member in em.GetBuffer<CampaignMissionExtractionMember>(root,true))
                        if(member.Kind==1 && CanSelectTutorialActor(em,member.Entity) &&
                            !em.HasComponent<UnitTransportPassenger>(member.Entity)) return member.Entity;
                    return Entity.Null;
                }
                var vehicle=step>=8?extraction.Aircraft:extraction.Carrier;
                return CanSelectTutorialActor(em,vehicle)?vehicle:Entity.Null;
            }
            if(CanSelectTutorialActor(em,guidance.SourceEntity)) return guidance.SourceEntity;
            var session=runtime.SessionToken;
            using var query=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(Faction),typeof(UnitMove),typeof(LocalTransform));
            using var units=query.ToEntityArray(Allocator.Temp);
            Entity closest=Entity.Null;float distance=float.MaxValue;
            foreach(var unit in units)
            {
                if(!CanSelectTutorialActor(em,unit) || !em.GetComponentData<CampaignMissionUnitRoleComponent>(unit).SessionToken.Equals(session)) continue;
                float d=math.distancesq(position,em.GetComponentData<LocalTransform>(unit).Position);
                if(d<distance){closest=unit;distance=d;}
            }
            return closest;
        }
        private static bool CanSelectTutorialActor(EntityManager em,Entity actor)=>em.Exists(actor) &&
            em.HasComponent<UnitMove>(actor) && em.HasComponent<Faction>(actor) &&
            FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(actor).Id) &&
            (!em.HasComponent<UnitHealth>(actor) || em.GetComponentData<UnitHealth>(actor).Current>0);
    }
}
