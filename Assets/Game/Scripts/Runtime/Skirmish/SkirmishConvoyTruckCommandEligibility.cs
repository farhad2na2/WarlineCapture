using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    public static class SkirmishConvoyTruckCommandEligibility
    {
        // Separate command eligibility from population accounting. Only the actual
        // sealed CE original truck may use this ObjectiveSupport Move exception.
        public static bool AcceptsMove(EntityManager em,Entity actor)
        {
            if(!SkirmishActorCommandAuthority.IsLivingOwnedActor(em,actor,1)||
                !em.HasComponent<SkirmishSharedActorTag>(actor)||!em.HasComponent<SkirmishConvoyObjectiveTruckComponent>(actor)||
                !em.HasComponent<SkirmishUnitRoleComponent>(actor)||!em.HasComponent<SkirmishObjectiveRoleComponent>(actor)||
                !em.HasComponent<UnitSourcePrefabKey>(actor)||!em.HasComponent<UnitMovementBehavior>(actor)||
                em.HasComponent<Parent>(actor)||
                em.HasComponent<UnitAirComponent>(actor)||em.HasComponent<UnitAirMovement>(actor)||
                em.HasComponent<UnitTransportCargoPassenger>(actor))return false;
            var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
            var objective=em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);var marker=em.GetComponentData<SkirmishConvoyObjectiveTruckComponent>(actor);
            if(own.ReservationId!=0||role.Role!=SkirmishRoleKind.ConvoyLogic||role.Category!=SkirmishPopulationCategory.ObjectiveSupport||role.SupplyCost!=0||
                objective.Role!=SkirmishObjectiveRoleKind.ConvoyTruck||objective.FactionId!=1||objective.StableObjectId!=own.StableObjectId||
                marker.Slot<1||marker.Slot>3||marker.DefinitionVersion!=1||
                em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString()!=SkirmishConvoyTruckDefinition.ExistingChassisKey||
                em.GetComponentData<UnitMovementBehavior>(actor).UsesVehicleMotion==0)return false;
            using var query=em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishResolvedSetupRecord));
            using var sessions=query.ToEntityArray(Allocator.Temp);Entity found=Entity.Null;
            foreach(var session in sessions)
            {
                var native=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
                if(native.SessionId!=own.SessionId)continue;
                if(found!=Entity.Null)return false;found=session;
                var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
                if(native.IsLegacy!=0||native.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort||native.Phase!=SkirmishSessionPhase.Playing||
                    native.SpawnComplete!=1||setup==null||setup.ObjectiveKind!=native.ObjectiveKind||setup.SetupHash!=native.SetupHash||
                    setup.PlayerObjectiveTrucks!=3||setup.EnemyObjectiveTrucks!=0||setup.ObjectiveSupportExtraPlayer!=3||
                    string.IsNullOrEmpty(setup.ObjectiveActorDefinitionFingerprint)||marker.DefinitionFingerprint.ToString()!=setup.ObjectiveActorDefinitionFingerprint||
                    !SkirmishOriginalObjectiveSpawnHooks.TryPlan(setup,out var plan,out _))return false;
                if(!em.HasComponent<SkirmishNativeObjectiveRecord>(session)||
                    em.GetComponentObject<SkirmishNativeObjectiveRecord>(session)?.Collector==null||
                    !em.GetComponentObject<SkirmishNativeObjectiveRecord>(session).Collector.IsOriginalEntity(actor))return false;
                int matches=0;
                foreach(var entry in plan)
                    if(entry.ObjectiveRole==SkirmishObjectiveRoleKind.ConvoyTruck&&entry.StableActorId==own.StableObjectId.ToString()&&
                        setup.Forces[entry.ForceIndex].ObjectiveRoleId=="convoy."+marker.Slot)matches++;
                if(matches!=1)return false;
            }
            return found!=Entity.Null;
        }
    }
}
