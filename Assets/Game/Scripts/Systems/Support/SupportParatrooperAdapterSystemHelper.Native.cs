using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    public static partial class SupportParatrooperAdapterSystemHelper
    {
        private static bool NativeContext(EntityManager em,Entity root,out Entity session,out SkirmishCapacitySnapshot capacity,out SkirmishRoleOverlay rifle)
        {
            session=Entity.Null;capacity=default;rifle=default;
            if(!em.HasComponent<SkirmishSupportOwnerComponent>(root))return true;
            var support=em.GetComponentData<SupportSessionComponent>(root);
            if(SupportSessionUtility.IsTerminal(em,root,support))return false;
            session=em.GetComponentData<SkirmishSupportOwnerComponent>(root).Session;
            if(!em.HasComponent<SkirmishResolvedSetupRecord>(session)||!em.HasComponent<SupportPayloadBindingsComponent>(root))return false;
            var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if(setup==null||!SkirmishRoleOverlayCatalog.TryGet(setup.RoleOverlays,SkirmishRoleKind.Rifle,out rifle)||rifle.SupplyCost!=1)return false;
            var prefab=em.GetComponentData<SupportPayloadBindingsComponent>(root).InfantryPrefab;
            if(!em.Exists(prefab)||!em.HasComponent<UnitSourcePrefabKey>(prefab))return false;
            string expected=SkirmishRoleCatalogConfig.RuntimePrefabKey(SkirmishRoleKind.Rifle);
            foreach(var force in setup.Forces)if(force.RoleKind==SkirmishRoleKind.Rifle&&!string.IsNullOrEmpty(force.RuntimePrefabKey)){expected=force.RuntimePrefabKey;break;}
            if(!em.GetComponentData<UnitSourcePrefabKey>(prefab).Value.Equals(expected))return false;
            if(!ReadNativeCapacity(em,session,support.FactionId,out capacity))return false;
            return capacity.InfantryCap==setup.InfantryCapEach&&capacity.SupplyCap==setup.SupplyCapEach&&
                capacity.InfantryLive>=0&&capacity.InfantryReserved>=0&&capacity.SupplyLive>=0&&capacity.SupplyReserved>=0;
        }
        public static bool CanBindNativeManifest(EntityManager em,Entity root,int members)
        {
            if(!NativeContext(em,root,out var session,out var capacity,out _))return false;
            if(session==Entity.Null)return true;
            return members>0&&(long)capacity.InfantryLive+capacity.InfantryReserved+members<=capacity.InfantryCap&&
                (long)capacity.SupplyLive+capacity.SupplyReserved+members<=capacity.SupplyCap;
        }
        private static bool CanBindNativePassenger(EntityManager em,Entity transport)
        {
            var flight=em.GetComponentData<SupportFlightComponent>(transport);int remaining=0;
            foreach(var entry in em.GetBuffer<SupportPassengerReservationElement>(transport))if(entry.Released==0)remaining++;
            return CanBindNativeManifest(em,flight.Root,remaining);
        }
        private static bool TryBindNativePassenger(EntityManager em,Entity transport,Entity passenger)
        {
            var flight=em.GetComponentData<SupportFlightComponent>(transport);
            if(!NativeContext(em,flight.Root,out var session,out var capacity,out var rifle))return false;
            if(session==Entity.Null)return true;
            if(em.HasComponent<SkirmishAttemptOwnedComponent>(passenger)||em.HasComponent<SkirmishSupportPassengerCapacity>(passenger))return false;
            if(!SkirmishCapacityLedger.TryReserve(ref capacity,SkirmishPopulationCategory.Infantry,1,1))return false;
            int index=-1;var entries=em.GetBuffer<SupportPassengerReservationElement>(transport);
            for(int i=0;i<entries.Length;i++)if(entries[i].Passenger==passenger&&entries[i].Released==0){index=i;break;}
            if(index<0)return false;
            var support=em.GetComponentData<SupportSessionComponent>(flight.Root);
            SkirmishRuntimeActorOwnership.DetachFromSourceScene(em,passenger);
            em.AddComponentData(passenger,new SkirmishAttemptOwnedComponent {SessionId=support.SessionToken,FactionId=support.FactionId,
                StableObjectId=new Unity.Collections.FixedString64Bytes($"support.rifle.{support.FactionId}.{(int)flight.Request.Source}.{flight.Request.RequestId}.{index}")});
            em.AddComponentData(passenger,new SkirmishUnitRoleComponent {Role=SkirmishRoleKind.Rifle,Category=SkirmishPopulationCategory.Infantry,SupplyCost=1});
            em.AddComponent<SkirmishSharedActorTag>(passenger);
            em.AddComponentData(passenger,new SkirmishVisualSpawnedComponent {Spawned=1,FromRegistry=1});
            if(em.HasComponent<CampaignMissionUnitRoleComponent>(passenger))em.RemoveComponent<CampaignMissionUnitRoleComponent>(passenger);
            SkirmishRosterProjectionSystem.WriteOverlay(em,passenger,rifle);
            uint group=em.HasComponent<SkirmishSupportFlightGroup>(transport)?em.GetComponentData<SkirmishSupportFlightGroup>(transport).GroupId:0;
            if(group==0){group=SkirmishArmyGroupSystem.OpenGroup(em,session,support.FactionId,SkirmishRoleKind.Rifle,SkirmishPopulationCategory.Infantry);em.AddComponentData(transport,new SkirmishSupportFlightGroup {GroupId=group});}
            SkirmishArmyGroupSystem.BindMember(em,session,passenger,group);
            SkirmishCapacityLedger.PromoteLive(ref capacity,SkirmishPopulationCategory.Infantry,1,1);
            WriteNativeCapacity(em,session,support.FactionId,capacity);
            em.AddComponentData(passenger,new SkirmishSupportPassengerCapacity {Session=session,SessionId=support.SessionToken,FactionId=support.FactionId,SupplyCost=1});
            return true;
        }
        private static bool ReadNativeCapacity(EntityManager em,Entity session,byte faction,out SkirmishCapacitySnapshot snapshot)
        {
            snapshot=default;
            if(faction==1&&em.HasComponent<SkirmishCapacityComponent>(session))
            {var c=em.GetComponentData<SkirmishCapacityComponent>(session);if(c.FactionId!=1)return false;snapshot=SkirmishCapacityLedger.ToSnapshot(c);return true;}
            if(faction==2&&em.HasComponent<SkirmishEnemyCapacityComponent>(session))
            {var c=em.GetComponentData<SkirmishEnemyCapacityComponent>(session);snapshot=new SkirmishCapacitySnapshot {InfantryCap=c.InfantryCap,SupplyCap=c.SupplyCap,InfantryLive=c.InfantryLive,InfantryReserved=c.InfantryReserved,SupplyLive=c.SupplyLive,SupplyReserved=c.SupplyReserved,GroundCap=c.GroundCap,AirCap=c.AirCap,GroundLive=c.GroundLive,AirLive=c.AirLive,GroundReserved=c.GroundReserved,AirReserved=c.AirReserved};return true;}
            return false;
        }
        private static void WriteNativeCapacity(EntityManager em,Entity session,byte faction,in SkirmishCapacitySnapshot snapshot)
        {
            if(faction==1){var c=em.GetComponentData<SkirmishCapacityComponent>(session);em.SetComponentData(session,SkirmishCapacityLedger.FromSnapshot(c,snapshot));}
            else {var c=em.GetComponentData<SkirmishEnemyCapacityComponent>(session);c.InfantryLive=snapshot.InfantryLive;c.InfantryReserved=snapshot.InfantryReserved;c.SupplyLive=snapshot.SupplyLive;c.SupplyReserved=snapshot.SupplyReserved;em.SetComponentData(session,c);}
        }
        public static void ReleaseDestroyedNativePassenger(EntityManager em,in SkirmishSupportPassengerCapacity owner)
        {
            if(owner.Released!=0||!em.Exists(owner.Session)||!em.HasComponent<SkirmishExpandedSessionComponent>(owner.Session)||
                !em.GetComponentData<SkirmishExpandedSessionComponent>(owner.Session).SessionId.Equals(owner.SessionId))return;
            if(!ReadNativeCapacity(em,owner.Session,owner.FactionId,out var capacity))return;
            if(SkirmishCapacityLedger.TryReleaseDeath(ref capacity,SkirmishPopulationCategory.Infantry,owner.SupplyCost))WriteNativeCapacity(em,owner.Session,owner.FactionId,capacity);
        }
    }
}
