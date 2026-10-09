using Unity.Collections;
using Game.Missions.Contracts;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    // Explicit native attempt ownership; Campaign progress is never attached to this root.
    public struct SkirmishSupportOwnerComponent : IComponentData
    { public Entity Session; public FixedString64Bytes SessionId; }

    public struct SupportSmokeVisualConfigComponent : IComponentData { public UnityObjectRef<UnityEngine.GameObject> Prefab; }
    public struct SupportSmokeZoneComponent : IComponentData
    {
        public Entity Root;
        public FixedString64Bytes SessionToken; public int AttemptOrdinal;
        public float3 Center; public float RadiusSquared; public double ExpiresAt; public ushort DirectDamagePermille;
    }
    public struct SupportRangedCoverComponent : IComponentData { public ushort DirectDamagePermille; }
    public struct SupportTargetEligibilityComponent : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal;
        public byte CurrentlyVisible, HostileConfirmed, Protected, IsMilitaryTarget;
        public uint SourceVersion;
    }
    // Explicit mission-authored route. No default route is inferred from a target.
    public struct SupportAirRouteComponent : IComponentData
    {
        public FixedString64Bytes SessionToken; public int AttemptOrdinal; public uint Version;
        public float3 Entry, Release, Exit; public float Clearance; public byte Authored;
    }
    public struct SupportFlightComponent : IComponentData
    {
        public Entity Root,Payload; public SupportRequestElement Request;
        public SupportExecutionPhase Phase; public double StartedAt, ReleasedAt;
        public float ApproachSeconds, ExitSeconds; public float3 Entry, Release, Exit;
        public uint RouteVersion; public int Damage; public byte FactionId;
    }
    public struct SupportFlightCleanupComponent : ICleanupComponentData
    {public Entity Root,Payload;public SupportRequestElement Request;public byte Released;}
    public struct SupportPayloadRegistryComponent : IComponentData
    { public Entity InfantryPrefab,ParachutePrefab,CratePrefab;public int InfantryCount; }
    public struct SupportPayloadBindingsComponent : IComponentData
    { public Entity InfantryPrefab,ParachutePrefab,CratePrefab;public int InfantryCount; }
    // Live native capacity survives external destruction until cleanup releases it.
    public struct SkirmishSupportPassengerCapacity : ICleanupComponentData
    { public Entity Session;public FixedString64Bytes SessionId;public byte FactionId,Released;public int SupplyCost; }
    public struct SkirmishSupportFlightGroup : IComponentData { public uint GroupId; }
    public struct SupportPassengerTransitTag : IComponentData {}
    public struct SupportPassengerCleanupComponent : ICleanupComponentData {public Entity Canopy;}
    public struct SupportCollectionFeedbackComponent : IComponentData {public SupportRejectionReason Reason;}
    public struct SupportPassengerOwnerComponent : IComponentData
    {
        public Entity Root,Flight,Canopy;public FixedString64Bytes SessionToken;public int AttemptOrdinal;
        public int2 LandingCell;public float3 LandingPosition;public byte Released,Landed;
    }
    public struct SupportPassengerReservationElement : ICleanupBufferElementData
    { public Entity Passenger;public byte Released; }
    public struct SupportSupplyCrateComponent : IComponentData
    {
        public Entity Root,Flight,Canopy,Claimant;public FixedString64Bytes SessionToken;public int AttemptOrdinal,RemainingMaterials;
        public int2 LandingCell;public float3 LandingPosition;public byte FactionId,Released,Landed;
    }
    public struct SupportCollectOrderComponent : IComponentData {public Entity Crate;public int2 Goal;public double ExpiresAt;}
    public struct SupportCollectRequestElement : IBufferElementData {public Entity Collector,Crate;public FixedString64Bytes SessionToken;public int AttemptOrdinal;}
    public struct SupportSupplyReadModelComponent : IComponentData {public Entity Crate;public int Remaining;public byte Claimed,Full,Ready;}
    public static class SupportSessionUtility
    {
        public static bool IsTerminal(EntityManager em,Entity root,in SupportSessionComponent session)
        {
            if(session.SessionToken.IsEmpty || !em.Exists(root))return true;
            if(em.HasComponent<SkirmishSupportOwnerComponent>(root))
            {
                var owner=em.GetComponentData<SkirmishSupportOwnerComponent>(root);
                if(!em.Exists(owner.Session) || !em.HasComponent<SkirmishExpandedSessionComponent>(owner.Session))return true;
                var native=em.GetComponentData<SkirmishExpandedSessionComponent>(owner.Session);
                return native.IsLegacy!=0 || !native.SessionId.Equals(owner.SessionId) ||
                    !session.SessionToken.Equals(owner.SessionId) || session.AttemptOrdinal!=1 ||
                    native.Phase==Game.Skirmish.Contracts.SkirmishSessionPhase.Finished ||
                    native.Phase==Game.Skirmish.Contracts.SkirmishSessionPhase.Failed ||
                    native.Phase==Game.Skirmish.Contracts.SkirmishSessionPhase.Cleaning ||
                    em.HasComponent<SkirmishExpandedCleanupRequest>(owner.Session) ||
                    em.HasComponent<SkirmishObjectiveStateComponent>(owner.Session) &&
                    em.GetComponentData<SkirmishObjectiveStateComponent>(owner.Session).Terminal!=0;
            }
            return em.HasComponent<CampaignMissionRuntimeComponent>(root) &&
                em.GetComponentData<CampaignMissionRuntimeComponent>(root).Outcome!=MissionOutcomeKind.None;
        }

        public static bool TryPlayer(EntityManager em,NativeArray<Entity> candidates,out Entity root)
        {
            root=Entity.Null;Entity active=Entity.Null,fallback=Entity.Null;int activeCount=0,playerCount=0;
            foreach(var candidate in candidates)
            {
                var session=em.GetComponentData<SupportSessionComponent>(candidate);
                if(session.FactionId!=1)continue;
                playerCount++;fallback=candidate;
                if(session.Active!=0){active=candidate;activeCount++;}
            }
            if(activeCount==1){root=active;return true;}
            if(activeCount==0 && playerCount==1){root=fallback;return true;}
            return false;
        }

        private static bool MatchesSmoke(EntityManager em,Entity entity,in SupportSmokeZoneComponent zone)
        {
            if(!em.Exists(entity) || !em.HasComponent<SupportSessionComponent>(entity))return false;
            var session=em.GetComponentData<SupportSessionComponent>(entity);
            return session.SessionToken.Equals(zone.SessionToken) && session.AttemptOrdinal==zone.AttemptOrdinal;
        }

        public static bool TrySmokeOwner(EntityManager em,NativeArray<Entity> roots,in SupportSmokeZoneComponent zone,out Entity owner)
        {
            owner=Entity.Null;
            if(zone.Root!=Entity.Null)
            {
                if(!MatchesSmoke(em,zone.Root,zone))return false;
                owner=zone.Root;return true;
            }
            // Older authored/test effects without a root require an unambiguous attempt.
            foreach(var entity in roots)if(MatchesSmoke(em,entity,zone))
            {if(owner!=Entity.Null){owner=Entity.Null;return false;}owner=entity;}
            return owner!=Entity.Null;
        }
    }


}
