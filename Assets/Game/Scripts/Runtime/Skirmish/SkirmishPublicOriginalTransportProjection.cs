using System;
using System.Collections.Generic;
using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    // Passive own-roster projection. A returned carrier is not permission to
    // unload: the presented normal drawer and the native command still decide.
    public static class SkirmishPublicOriginalTransportProjection
    {
        public static bool TryRead(EntityManager em,Entity session,SkirmishPublicGoalSnapshot publicGoal,uint originalGroup,
            out SkirmishPublicOriginalTransportGroup result)
        {
            result=null;
            if(!em.Exists(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session)||originalGroup==0||
                publicGoal==null||publicGoal.ObserverFaction!=1||publicGoal.Perspective!=SkirmishPublicGoalPerspective.Evacuate||
                publicGoal.Outcome!=SkirmishOutcomeKind.None)return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if(native.IsLegacy!=0||native.Phase!=SkirmishSessionPhase.Playing||native.ObjectiveKind!=SkirmishObjectiveKind.Breakthrough||
                native.DefinitionId.ToString()!=publicGoal.DefinitionId||native.ContentVersion!=publicGoal.DefinitionVersion)return false;
            var publicIds=new Dictionary<string,bool>(StringComparer.Ordinal);
            for(int i=0;i<publicGoal.NodeCount;i++)
            {
                var node=publicGoal.Node(i);
                if(node.Kind==SkirmishPublicGoalNodeKind.OwnDesignatedSubject&&node.SubjectDisposition==SkirmishPublicSubjectDisposition.Live&&
                    node.RevealedSubjectKind==SkirmishPublicSubjectKind.DesignatedRifle&&node.OwnerKnown&&node.OwnerFaction==1)
                {if(publicIds.ContainsKey(node.PublicId))return false;publicIds.Add(node.PublicId,node.Mounted);}
            }
            using var query=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>(),ComponentType.ReadOnly<SkirmishArmyGroupMembershipComponent>(),
                    ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<Faction>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var actors=query.ToEntityArray(Allocator.Temp);
            var living=new List<string>();var mounted=new List<(string id,Entity carrier)>();var unique=new HashSet<string>(StringComparer.Ordinal);
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=native.SessionId||own.FactionId!=1||own.CapacityReleased!=0||own.IsStructure!=0||
                    em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor).GroupId!=originalGroup||
                    em.GetComponentData<UnitHealth>(actor).Current<=0)continue;
                string id=own.StableObjectId.ToString();if(!publicIds.TryGetValue(id,out bool publiclyMounted))continue;
                if(em.GetComponentData<Faction>(actor).Id!=1||!unique.Add(id)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role!=SkirmishRoleKind.Rifle||
                    publiclyMounted!=em.HasComponent<UnitTransportPassenger>(actor))return false;
                living.Add(id);
                if(!em.HasComponent<UnitTransportPassenger>(actor))
                {if(em.HasComponent<UnitTransportCargoPassenger>(actor))return false;continue;}
                var carrier=em.GetComponentData<UnitTransportPassenger>(actor).Transport;
                if(!OwnLiveCarrier(em,carrier,native.SessionId,out _))return false;
                int reciprocal=0;foreach(var entry in em.GetBuffer<UnitTransportPassengerElement>(carrier,true))if(entry.Passenger==actor)reciprocal++;
                if(reciprocal!=1)return false;
                mounted.Add((id,carrier));
            }
            if(living.Count==0||mounted.Count==0)return false;
            // Multiple carriers are handled through distinct normal interactions,
            // ordered by public original ID. Never select or unload all implicitly.
            mounted.Sort((a,b)=>StringComparer.Ordinal.Compare(a.id,b.id));living.Sort(StringComparer.Ordinal);
            Entity chosen=mounted[0].carrier;
            if(!OwnLiveCarrier(em,chosen,native.SessionId,out var carrierOwner))return false;
            uint carrierGroup=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(chosen).GroupId;
            if(!em.HasBuffer<SkirmishArmyGroupRecord>(session))return false;
            int known=0;foreach(var group in em.GetBuffer<SkirmishArmyGroupRecord>(session,true))
                if(group.GroupId==carrierGroup&&group.FactionId==1&&group.AliveCount>0)known++;
            if(known!=1)return false;
            var carried=new List<string>();foreach(var row in mounted)if(row.carrier==chosen)carried.Add(row.id);
            result=new SkirmishPublicOriginalTransportGroup {OriginalGroupId=originalGroup,CarrierGroupId=carrierGroup,
                CarrierPublicId=carrierOwner.StableObjectId.ToString(),LivingOriginalIds=living.ToArray(),
                CarrierOriginalIds=carried.ToArray(),MountedOriginalCount=mounted.Count};
            return true;
        }
        static bool OwnLiveCarrier(EntityManager em,Entity carrier,FixedString64Bytes attempt,out SkirmishAttemptOwnedComponent owner)
        {
            owner=default;
            if(carrier==Entity.Null||!em.Exists(carrier)||em.HasComponent<Disabled>(carrier)||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(carrier)||!em.HasComponent<Faction>(carrier)||
                !em.HasComponent<UnitHealth>(carrier)||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(carrier)||
                !em.HasBuffer<UnitTransportPassengerElement>(carrier))return false;
            owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(carrier);
            return owner.SessionId==attempt&&owner.FactionId==1&&owner.IsStructure==0&&owner.CapacityReleased==0&&owner.StableObjectId.Length>0&&
                em.GetComponentData<Faction>(carrier).Id==1&&em.GetComponentData<UnitHealth>(carrier).Current>0&&
                em.GetComponentData<SkirmishArmyGroupMembershipComponent>(carrier).GroupId!=0;
        }
    }
}
