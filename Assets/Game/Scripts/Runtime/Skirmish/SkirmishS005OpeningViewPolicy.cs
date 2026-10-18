using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    public struct SkirmishS005OpeningFocusOwner : IComponentData
    {
        public FixedString64Bytes Attempt,ActorId;
        public uint SetupHash;
        public float3 ActualActorWorld,GroundRequest;
        public int RequestedFrame;
    }
    // Opt-in normal mission presentation; the established S005 profile remains compatible. Uses actual own opening actors; no orders,
    // selection changes, camera transform writes or hidden enemy observations.
    public static class SkirmishS005OpeningViewPolicy
    {
        public static bool TryQueueAfterActualSpawn(EntityManager em,Entity session,out string error)
        {
            error="Actual opening camera identity unavailable.";
            if(!em.Exists(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session))return false;
            if(em.HasComponent<SkirmishExpandedSessionComponent>(session)&&em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy!=0){error=null;return true;}
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(setup==null||setup.PlayerFaction<=0||setup.PlayerFaction>byte.MaxValue)return false;

            var candidate=SkirmishMissionViewProfile.Load(setup.CatalogId);
            if(candidate==null||candidate.OpeningFocusPolicy==SkirmishMissionOpeningFocusPolicy.None)
                return TryQueueScopedCandidate(em,session,candidate,out error);
            if(!SkirmishMissionViewProfileApplication.TryResolve(em,out var profile))return false;
            return TryQueueScopedCandidate(em,session,profile,out error);
        }
        internal static bool TryQueueScopedCandidate(EntityManager em,Entity session,SkirmishMissionViewProfile profile,out string error)
        {
            // Shipping code alone preserves the existing coded mission. Only an
            // explicitly enabled logical asset opts into strict role validation.
            if(profile==null||profile.OpeningFocusPolicy==SkirmishMissionOpeningFocusPolicy.None)
            {error=null;return true;}
            return TryQueueValidated(em,session,profile,out error);
        }
        internal static bool TryQueueValidated(EntityManager em,Entity session,SkirmishMissionViewProfile profile,out string error)
        {
            error="Pinned normal profile and fresh actual owned opening Rifle group required.";
            if(!em.Exists(session)||profile==null||!profile.IsValid||profile.AuthoredNonBaseAssault||profile.AuthoredGenericBaseAssault||
                (profile.OpeningFocusPolicy!=SkirmishMissionOpeningFocusPolicy.S005FirstOpeningOwnedRifle&&profile.OpeningFocusPolicy!=SkirmishMissionOpeningFocusPolicy.FirstOpeningOwnedRifleGroup)||
                !em.HasComponent<SkirmishExpandedSessionComponent>(session)||!em.HasComponent<SkirmishResolvedSetupRecord>(session)||
                !em.HasComponent<SkirmishLaunchDefinitionRecord>(session)||!em.HasComponent<SkirmishObjectiveClockComponent>(session))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var record=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            if(native.IsLegacy!=0||native.CatalogId.ToString()!=profile.CatalogId||
                native.SpawnComplete!=1||native.Phase!=SkirmishSessionPhase.Playing||record==null||!record.Matches(setup,native)||
                !SkirmishMissionViewProfileApplication.MatchesSource(em,setup,record.Definition,profile))return false;
            if(em.HasComponent<SkirmishS005OpeningFocusOwner>(session))
            {
                if(!OwnsIntroduction(em,session))return false;
                error=null;return true; // Never pull the camera back after normal input.
            }
            var clock=em.GetComponentData<SkirmishObjectiveClockComponent>(session);
            if(clock.ElapsedSeconds!=0||clock.Playing!=1||clock.Paused!=0||
                !TryFindOpeningFocus(em,session,native,setup,profile,out var actorId,out var position)||
                !SkirmishMissionLogicalBindingProvider.TryProjectRoleFocus(position,profile.NormalHeight,profile.Pitch,profile.Yaw,out var ground))return false;
            var facade=new RuntimeGameplayStateSystem(em);facade.ReadCameraFocusRequest();
            using var requests=em.CreateEntityQuery(typeof(RuntimeCameraFocusRequestComponent));
            if(requests.CalculateEntityCount()!=1)return false;
            em.SetComponentData(requests.GetSingletonEntity(),new RuntimeCameraFocusRequestComponent {
                Requested=1,Smooth=1,UseExplicitPerspective=1,World=ground,
                Perspective=new float4(profile.NormalHeight,profile.Pitch,profile.Yaw,profile.NormalFieldOfView),
                SmoothTimeSeconds=RuntimeCameraFocusRequestUtility.EnsureMissionIntroSmoothTime(2.25f) });
            em.AddComponentData(session,new SkirmishS005OpeningFocusOwner { Attempt=native.SessionId,SetupHash=setup.SetupHash,
                ActorId=new FixedString64Bytes(actorId),ActualActorWorld=position,GroundRequest=ground,RequestedFrame=Time.frameCount });
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[SkirmishS005OpeningViewPolicy] result=Queued attempt="+native.SessionId+" actor="+actorId+
                " setupHash="+setup.SetupHash+" actorWorld="+position+" groundRequest="+ground+
                " normalHeight="+profile.NormalHeight+" nativeVisualAccepted=0");
#endif
            error=null;return true;
        }
        internal static bool TryFindOpeningFocus(EntityManager em,Entity session,in SkirmishExpandedSessionComponent native,
            SkirmishResolvedSetup setup,SkirmishMissionViewProfile profile,out string id,out float3 position)
        {
            if(profile.OpeningFocusPolicy==SkirmishMissionOpeningFocusPolicy.FirstOpeningOwnedRifleGroup)
            {
                if(!TryFindOpeningRifle(em,native,setup,out id,out position))return false;
            }
            else
            {
                if(string.IsNullOrEmpty(profile.OpeningFocusStableActorId))
                    return TryFindOpeningRifle(em,native,setup,out id,out position);
                id=profile.OpeningFocusStableActorId;position=default;
            }
            if(!IsOriginalRifleId(setup,id)||!em.HasBuffer<SkirmishArmyGroupRecord>(session))return false;
            using var query=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState });
            using var actors=query.ToEntityArray(Allocator.Temp);Entity hint=Entity.Null;uint group=0;
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=native.SessionId||own.StableObjectId.ToString()!=id)continue;
                if(hint!=Entity.Null||!ValidOriginalRifle(em,actor,own,setup)||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))return false;
                var member=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor);
                if(member.GroupId==0||member.Domain!=SkirmishPopulationCategory.Infantry)return false;
                hint=actor;group=member.GroupId;
            }
            if(hint==Entity.Null)return false;
            int records=0;
            foreach(var record in em.GetBuffer<SkirmishArmyGroupRecord>(session,true))
            {
                if(record.GroupId!=group)continue;
                if(record.FactionId!=setup.PlayerFaction||record.Role!=SkirmishRoleKind.Rifle||record.Domain!=SkirmishPopulationCategory.Infantry||
                    record.MemberCount!=SkirmishRoleIds.InfantrySquadMembers||record.AliveCount!=SkirmishRoleIds.InfantrySquadMembers)return false;
                records++;
            }
            if(records!=1)return false;
            var ids=new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);float3 sum=default;
            foreach(var actor in actors)
            {
                var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(own.SessionId!=native.SessionId||!em.HasComponent<SkirmishArmyGroupMembershipComponent>(actor))continue;
                var member=em.GetComponentData<SkirmishArmyGroupMembershipComponent>(actor);if(member.GroupId!=group)continue;
                if(member.Domain!=SkirmishPopulationCategory.Infantry||!ValidOriginalRifle(em,actor,own,setup)||
                    !ids.Add(own.StableObjectId.ToString())||ids.Count>SkirmishRoleIds.InfantrySquadMembers)return false;
                sum+=em.GetComponentData<LocalTransform>(actor).Position;
            }
            if(ids.Count!=SkirmishRoleIds.InfantrySquadMembers)return false;
            position=sum/ids.Count;return math.all(math.isfinite(position));
        }
        static bool IsOriginalRifleId(SkirmishResolvedSetup setup,string id)
        {
            if(setup?.Forces==null)return false;
            for(int i=0;i<setup.Forces.Length;i++)
            {
                var force=setup.Forces[i];if(force.FactionId!=setup.PlayerFaction||force.RoleKind!=SkirmishRoleKind.Rifle)continue;
                for(int member=0;member<force.Quantity;member++)
                    if(id==force.RoleId+"."+setup.PlayerFaction.ToString(System.Globalization.CultureInfo.InvariantCulture)+"."+i.ToString(System.Globalization.CultureInfo.InvariantCulture)+"."+member.ToString(System.Globalization.CultureInfo.InvariantCulture))return true;
            }
            return false;
        }
        static bool ValidOriginalRifle(EntityManager em,Entity actor,SkirmishAttemptOwnedComponent own,SkirmishResolvedSetup setup)
        {
            if(own.FactionId!=setup.PlayerFaction||own.IsStructure!=0||own.ReservationId!=0||own.CapacityReleased!=0||!IsOriginalRifleId(setup,own.StableObjectId.ToString())||
                !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=setup.PlayerFaction||
                !em.HasComponent<SkirmishUnitRoleComponent>(actor)||!em.HasComponent<UnitHealth>(actor)||!em.HasComponent<LocalTransform>(actor)||
                em.HasComponent<Disabled>(actor)||em.HasComponent<Parent>(actor)||em.HasComponent<PostTransformMatrix>(actor)||
                em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor))return false;
            var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);var health=em.GetComponentData<UnitHealth>(actor);
            return role.Role==SkirmishRoleKind.Rifle&&role.Category==SkirmishPopulationCategory.Infantry&&health.Current>0&&health.Max>0&&
                math.all(math.isfinite(em.GetComponentData<LocalTransform>(actor).Position));
        }
        internal static bool TryFindOpeningRifle(EntityManager em,in SkirmishExpandedSessionComponent native,SkirmishResolvedSetup setup,
            out string id,out float3 position)
        {
            id=null;position=default;
            if(setup?.Forces==null)return false;
            for(int i=0;i<setup.Forces.Length;i++)
            {
                var force=setup.Forces[i];if(force.FactionId!=setup.PlayerFaction||force.RoleKind!=SkirmishRoleKind.Rifle||force.Quantity<=0)continue;
                id=force.RoleId+"."+setup.PlayerFaction.ToString(System.Globalization.CultureInfo.InvariantCulture)+"."+i.ToString(System.Globalization.CultureInfo.InvariantCulture)+".0";break;
            }
            if(id==null)return false;
            using var query=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState});
            using var actors=query.ToEntityArray(Allocator.Temp);Entity found=Entity.Null;
            foreach(var actor in actors)
            {
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(owned.SessionId!=native.SessionId||owned.StableObjectId.ToString()!=id)continue;
                if(found!=Entity.Null||owned.FactionId!=setup.PlayerFaction||owned.IsStructure!=0||owned.ReservationId!=0||owned.CapacityReleased!=0||
                    !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=setup.PlayerFaction||
                    !em.HasComponent<SkirmishUnitRoleComponent>(actor)||!em.HasComponent<UnitHealth>(actor)||!em.HasComponent<LocalTransform>(actor)||
                    em.HasComponent<Disabled>(actor)||em.HasComponent<Parent>(actor)||em.HasComponent<PostTransformMatrix>(actor)||
                    em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                    em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor))return false;
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);var health=em.GetComponentData<UnitHealth>(actor);
                if(role.Role!=SkirmishRoleKind.Rifle||role.Category!=SkirmishRoleIds.Category(SkirmishRoleKind.Rifle)||health.Current<=0||health.Max<=0)return false;
                position=em.GetComponentData<LocalTransform>(actor).Position;if(!math.all(math.isfinite(position)))return false;found=actor;
            }
            return found!=Entity.Null;
        }
        public static bool OwnsIntroduction(EntityManager em,Entity session)
        {
            if(!em.Exists(session)||!em.HasComponent<SkirmishS005OpeningFocusOwner>(session)||!em.HasComponent<SkirmishExpandedSessionComponent>(session))return false;
            var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);var owner=em.GetComponentData<SkirmishS005OpeningFocusOwner>(session);
            if(!em.HasComponent<SkirmishResolvedSetupRecord>(session)||!em.HasComponent<SkirmishLaunchDefinitionRecord>(session))return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            var record=em.GetComponentObject<SkirmishLaunchDefinitionRecord>(session);
            return native.IsLegacy==0&&record!=null&&record.Matches(setup,native)&&
                owner.Attempt==native.SessionId&&owner.SetupHash==native.SetupHash;
        }
    }
}
