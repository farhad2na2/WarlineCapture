using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Entities;

namespace Game.Runtime
{
    internal static class SkirmishNativeConvoyCheckpoint
    {
        internal static bool TryValidateClassification(SkirmishResolvedSetup setup,
            SkirmishNativeCheckpointSnapshotService.Actor saved,out string error)
        {
            error="Objective Cargo Truck marker/role classification mismatch.";
            if(saved?.Parts==null)return false;
            bool marker=false;
            foreach(var part in saved.Parts)
                if(part!=null && part.Type==typeof(SkirmishConvoyObjectiveTruckComponent).FullName)marker=true;
            bool objectiveRole=SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts,out SkirmishUnitRoleComponent role) &&
                role.Role==SkirmishRoleKind.ConvoyLogic;
            if(!marker&&!objectiveRole){error=null;return true;}
            return setup?.ObjectiveKind==SkirmishObjectiveKind.ConvoyEscort && marker && objectiveRole &&
                TryValidateSaved(setup,saved,out _,out error);
        }
        internal static bool TryValidateSaved(SkirmishResolvedSetup setup,
            SkirmishNativeCheckpointSnapshotService.Actor saved,out byte slot,out string error)
        {
            slot=0;error="Exact immutable CE original and checksum-covered native specialization required.";
            if(!SkirmishConvoySpawnHooks.TryResolveDefinition(setup,out var definition,out error) || saved==null ||
                saved.PrefabKey!=SkirmishConvoyTruckDefinition.ExistingChassisKey || saved.IsStructure!=0 ||
                !SkirmishOriginalObjectiveSpawnHooks.TryPlan(setup,out var originals,out error))return false;
            foreach(var binding in originals)
            {
                if(binding.StableActorId!=saved.Id)continue;
                var force=setup.Forces[binding.ForceIndex];
                slot=force.ObjectiveRoleId=="convoy.1"?(byte)1:force.ObjectiveRoleId=="convoy.2"?(byte)2:force.ObjectiveRoleId=="convoy.3"?(byte)3:(byte)0;
            }
            if(slot==0 || saved.Parts==null)return false;
            foreach(var part in saved.Parts)
                if(part==null || SkirmishConvoyTruckRuntime.IsForbiddenCheckpointPartName(part.Type))return false;
            if(!SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts,out SkirmishConvoyObjectiveTruckComponent marker) ||
                marker.Slot!=slot || marker.DefinitionVersion!=definition.ContentVersion || marker.DefinitionFingerprint.ToString()!=definition.Fingerprint() ||
                !SkirmishNativeCheckpointParts.TryReadComponent(saved.Parts,out SkirmishUnitRoleComponent role) ||
                role.Role!=SkirmishRoleKind.ConvoyLogic || role.Category!=SkirmishPopulationCategory.ObjectiveSupport || role.SupplyCost!=0)
                return false;
            error=null;return true;
        }

        // Called on the just-instantiated shared raw chassis, while Disabled and
        // combat suppression are still owned by native recovery. Saved death/HP,
        // position and capacity are applied later by the real Parts transaction.
        internal static bool TrySpecializeFresh(EntityManager em,Entity actor,SkirmishResolvedSetup setup,
            SkirmishNativeCheckpointSnapshotService.Actor saved,out string error)
        {
            if(!TryValidateSaved(setup,saved,out byte slot,out error) ||
                !SkirmishConvoySpawnHooks.TryResolveDefinition(setup,out var definition,out error))return false;
            var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
            owner.CapacityReleased=0;em.SetComponentData(actor,owner);
            return SkirmishConvoyTruckRuntime.TrySpecializeFreshInstance(em,actor,definition,owner.SessionId,saved.Id,slot,out error,true);
        }

        internal static void ValidateAfterParts(EntityManager em,Entity session,SkirmishResolvedSetup setup)
        {
            if(setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort)return;
            if(!SkirmishConvoySpawnHooks.TryResolveDefinition(setup,out var definition,out string error))
                throw new InvalidOperationException(error);
            var attempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},
                Options=EntityQueryOptions.IncludeDisabledEntities|EntityQueryOptions.IgnoreComponentEnabledState });
            using var actors=query.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach(var actor in actors)
            {
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(owner.SessionId!=attempt)continue;
                bool marker=em.HasComponent<SkirmishConvoyObjectiveTruckComponent>(actor);
                bool objectiveRole=em.HasComponent<SkirmishUnitRoleComponent>(actor) &&
                    em.GetComponentData<SkirmishUnitRoleComponent>(actor).Role==SkirmishRoleKind.ConvoyLogic;
                if(!marker&&!objectiveRole)continue;
                if(!marker||!objectiveRole)throw new InvalidOperationException("Restored CE marker/role classification changed.");
                if(!SkirmishConvoyTruckRuntime.ValidateSpecialized(em,actor,definition,out error))
                    throw new InvalidOperationException(error);
            }
        }
    }
}
