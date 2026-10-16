using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishConvoyTruckRuntime
    {
        // Only fresh native instances of the explicitly referenced existing chassis
        // are specialized. Preserve health, speed, footprint, visuals and Campaign
        // prefab data. This operation never touches ordinary logistics instances.
        public static bool TrySpecializeFreshInstance(EntityManager em,Entity actor,SkirmishConvoyTruckDefinition definition,
            FixedString64Bytes attempt,string plannedOriginalId,byte slot,out string error,bool keepSuppressedUntilRecoveryCommit=false)
        {
            error="Explicit fresh native objective Cargo Truck instance required.";
            if(definition==null||!definition.IsValid||slot<1||slot>3||!em.Exists(actor)||em.HasComponent<Prefab>(actor)||
                !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||
                !em.HasComponent<SkirmishUnitRoleComponent>(actor)||!em.HasComponent<UnitSourcePrefabKey>(actor)||
                !em.HasComponent<UnitHealth>(actor)||!em.HasComponent<UnitMove>(actor)||!em.HasComponent<UnitFootprint>(actor)||
                !em.HasComponent<UnitMovementBehavior>(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1)return false;
            var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            var hp=em.GetComponentData<UnitHealth>(actor);var move=em.GetComponentData<UnitMove>(actor);
            if(owner.SessionId!=attempt||owner.StableObjectId.ToString()!=plannedOriginalId||owner.FactionId!=1||owner.IsStructure!=0||
                owner.ReservationId!=0||owner.CapacityReleased!=0||role.Role!=SkirmishRoleKind.ConvoyLogic||
                role.Category!=SkirmishPopulationCategory.ObjectiveSupport||role.SupplyCost!=0||hp.Max!=definition.ChassisConfig.MaxHealth||hp.Current<=0||
                move.Speed!=definition.ChassisConfig.Speed||em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString()!=SkirmishConvoyTruckDefinition.ExistingChassisKey||
                em.HasComponent<UnitAirComponent>(actor)||em.HasComponent<UnitAirMovement>(actor)||
                em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor))return false;
            if(em.HasComponent<SkirmishConvoyObjectiveTruckComponent>(actor))
            {
                var old=em.GetComponentData<SkirmishConvoyObjectiveTruckComponent>(actor);
                if(old.Slot!=slot||old.DefinitionVersion!=definition.ContentVersion||old.DefinitionFingerprint.ToString()!=definition.Fingerprint())return false;
            }
            // Prefab-derived capabilities are removed rather than merely hidden.
            Remove<UnitFuelConsumption>(em,actor);Remove<UnitFuelConsumptionState>(em,actor);
            Remove<UnitResourceHauler>(em,actor);Remove<UnitResourceHaulStatus>(em,actor);
            Remove<UnitResourceHaulOrder>(em,actor);Remove<UnitResourceHaulReservation>(em,actor);
            Remove<FuelLogisticsOilHaulerTag>(em,actor);Remove<FuelLogisticsFuelHaulerTag>(em,actor);
            Remove<UnitTransportCapacity>(em,actor);Remove<UnitTransportCargoCapacity>(em,actor);
            Remove<UnitTransportPassengerElement>(em,actor);Remove<UnitAttack>(em,actor);Remove<ThreatDetector>(em,actor);
            Remove<GroundMissileLauncherComponent>(em,actor);Remove<AirMissileLauncherComponent>(em,actor);
            Remove<EngageTarget>(em,actor);Remove<ManualMoveOrderTag>(em,actor);Remove<AutoWanderMoveTag>(em,actor);
            Remove<UnitPathRequest>(em,actor);Remove<UnitPathFollow>(em,actor);Remove<UnitPathRange>(em,actor);
            var behavior=em.GetComponentData<UnitMovementBehavior>(actor);behavior.AllowIdleWander=0;em.SetComponentData(actor,behavior);
            if(em.HasComponent<UnitCombat>(actor)){var combat=em.GetComponentData<UnitCombat>(actor);combat.CanAttack=0;combat.AutoEngage=0;em.SetComponentData(actor,combat);}
            var policy=new CombatTargetPolicy{Domain=CombatTargetDomain.Ground,AllowedTargets=0,Visible=1};
            Set(em,actor,policy);
            Set(em,actor,new SkirmishConvoyObjectiveTruckComponent{Slot=slot,DefinitionVersion=definition.ContentVersion,
                DefinitionFingerprint=new FixedString128Bytes(definition.Fingerprint())});
            // The shared command gate can now see a legal movable, damageable
            // ground actor. No Move/Hold is issued automatically at startup.
            if(!keepSuppressedUntilRecoveryCommit)Remove<CampaignMissionCombatSuppressedTag>(em,actor);
            error=null;return true;
        }

        public static bool ValidateSpecialized(EntityManager em,Entity actor,SkirmishConvoyTruckDefinition definition,out string error)
        {
            error="Objective Cargo Truck privileges or identity were resurrected.";
            if(definition==null||!definition.IsValid||!em.Exists(actor)||!em.HasComponent<SkirmishConvoyObjectiveTruckComponent>(actor)||
                !em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor)||
                !em.HasComponent<UnitMovementBehavior>(actor)||!em.HasComponent<CombatTargetPolicy>(actor)||
                !em.HasComponent<UnitHealth>(actor)||!em.HasComponent<UnitMove>(actor)||!em.HasComponent<UnitFootprint>(actor)||
                !em.HasComponent<UnitSourcePrefabKey>(actor)||!em.HasComponent<Faction>(actor)||em.GetComponentData<Faction>(actor).Id!=1)return false;
            var marker=em.GetComponentData<SkirmishConvoyObjectiveTruckComponent>(actor);var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);var policy=em.GetComponentData<CombatTargetPolicy>(actor);
            if(marker.Slot<1||marker.Slot>3||marker.DefinitionVersion!=definition.ContentVersion||marker.DefinitionFingerprint.ToString()!=definition.Fingerprint()||
                owner.FactionId!=1||owner.IsStructure!=0||owner.ReservationId!=0||role.Role!=SkirmishRoleKind.ConvoyLogic||
                role.Category!=SkirmishPopulationCategory.ObjectiveSupport||role.SupplyCost!=0||owner.SessionId.IsEmpty||owner.StableObjectId.IsEmpty||
                em.GetComponentData<UnitHealth>(actor).Max!=definition.ChassisConfig.MaxHealth||
                em.GetComponentData<UnitMove>(actor).Speed!=definition.ChassisConfig.Speed||
                em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString()!=SkirmishConvoyTruckDefinition.ExistingChassisKey||
                em.GetComponentData<UnitMovementBehavior>(actor).AllowIdleWander!=0||policy.Domain!=CombatTargetDomain.Ground||policy.AllowedTargets!=0)return false;
            if(em.HasComponent<UnitCombat>(actor)){var combat=em.GetComponentData<UnitCombat>(actor);if(combat.CanAttack!=0||combat.AutoEngage!=0)return false;}
            if(em.HasComponent<UnitFuelConsumption>(actor)||em.HasComponent<UnitFuelConsumptionState>(actor)||em.HasComponent<UnitResourceHauler>(actor)||
                em.HasComponent<UnitResourceHaulStatus>(actor)||em.HasComponent<UnitResourceHaulOrder>(actor)||em.HasComponent<UnitResourceHaulReservation>(actor)||
                em.HasComponent<FuelLogisticsOilHaulerTag>(actor)||em.HasComponent<FuelLogisticsFuelHaulerTag>(actor)||
                em.HasComponent<UnitTransportCapacity>(actor)||em.HasComponent<UnitTransportCargoCapacity>(actor)||em.HasBuffer<UnitTransportPassengerElement>(actor)||
                em.HasComponent<UnitTransportPassenger>(actor)||em.HasComponent<UnitTransportCargoPassenger>(actor)||
                em.HasComponent<UnitTransportParachuteDropComponent>(actor)||em.HasComponent<UnitTransportRopeDropComponent>(actor)||
                em.HasComponent<UnitAttack>(actor)||em.HasComponent<ThreatDetector>(actor)||em.HasComponent<GroundMissileLauncherComponent>(actor)||
                em.HasComponent<AirMissileLauncherComponent>(actor)||em.HasComponent<UnitAirComponent>(actor)||em.HasComponent<UnitAirMovement>(actor))return false;
            error=null;return true;
        }
        // Native checkpoint codecs must reject these saved parts before applying
        // them to the freshly specialized shared chassis. Known codecs are not
        // sufficient authorization to resurrect an objective truck capability.
        public static bool IsForbiddenCheckpointPartName(string name)=>
            name==typeof(UnitFuelConsumption).FullName||name==typeof(UnitFuelConsumptionState).FullName||
            name==typeof(UnitResourceHauler).FullName||name==typeof(UnitResourceHaulStatus).FullName||
            name==typeof(UnitResourceHaulOrder).FullName||name==typeof(UnitResourceHaulReservation).FullName||
            name==typeof(FuelLogisticsOilHaulerTag).FullName||name==typeof(FuelLogisticsFuelHaulerTag).FullName||
            name==typeof(UnitTransportCapacity).FullName||name==typeof(UnitTransportCargoCapacity).FullName||
            name==typeof(UnitTransportPassengerElement).FullName||name==typeof(UnitTransportPassenger).FullName||
            name==typeof(UnitTransportCargoPassenger).FullName||name==typeof(UnitTransportParachuteDropComponent).FullName||
            name==typeof(UnitTransportRopeDropComponent).FullName||name==typeof(UnitAttack).FullName||name==typeof(ThreatDetector).FullName||
            name==typeof(GroundMissileLauncherComponent).FullName||name==typeof(AirMissileLauncherComponent).FullName||
            name==typeof(UnitAirComponent).FullName||name==typeof(UnitAirMovement).FullName;
        private static void Set<T>(EntityManager em,Entity actor,T value) where T:unmanaged,IComponentData
        {if(em.HasComponent<T>(actor))em.SetComponentData(actor,value);else em.AddComponentData(actor,value);}
        private static void Remove<T>(EntityManager em,Entity actor) where T:unmanaged
        {if(em.HasComponent(actor,ComponentType.ReadWrite<T>()))em.RemoveComponent<T>(actor);}
    }
}
