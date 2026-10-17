using System;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishOriginalObjectiveSpawnHooks
    {
        public static bool TryPlan(SkirmishResolvedSetup setup,out SkirmishOriginalObjectiveBinding[] bindings,out string error)
        {
            bindings=Array.Empty<SkirmishOriginalObjectiveBinding>();error=null;
            if(setup==null){error="Resolved setup missing.";return false;}
            if(setup.ObjectiveKind!=SkirmishObjectiveKind.Breakthrough&&setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort)return true;
            if(setup.Forces==null){error="Actual compiled opening forces missing.";return false;}
            var declarations=new SkirmishOriginalForceDeclaration[setup.Forces.Length];
            for(int i=0;i<declarations.Length;i++)
            {
                var force=setup.Forces[i];
                declarations[i]=new SkirmishOriginalForceDeclaration{ForceIndex=i,Faction=force.FactionId,Quantity=force.Quantity,
                    Role=force.RoleKind,RoleId=force.RoleId,NativePrefabKey=force.RuntimePrefabKey};
            }
            try{bindings=SkirmishOriginalObjectiveBindingPlan.Build(setup.ObjectiveKind,setup.PlayerDesignatedRifles,setup.PlayerObjectiveTrucks,declarations);return true;}
            catch(ArgumentException failure){error=failure.Message;return false;}
        }

        // Called only for the actual shared prefab instance after native placement.
        // No extra actor/squad, role substitution, health/resource or order mutation.
        public static bool TryTagActualOpeningMember(EntityManager em,Entity actor,FixedString64Bytes attempt,
            SkirmishResolvedSetup setup,int forceIndex,int memberIndex,out string error)
        {
            if(!TryPlan(setup,out var plan,out error))return false;
            foreach(var binding in plan)
            {
                if(binding.ForceIndex!=forceIndex||binding.MemberIndex!=memberIndex)continue;
                if(!em.Exists(actor)||!em.HasComponent<SkirmishSharedActorTag>(actor)||
                    !em.HasComponent<SkirmishAttemptOwnedComponent>(actor)||!em.HasComponent<SkirmishUnitRoleComponent>(actor))
                {error="Original designation requires actual native shared actor.";return false;}
                var owned=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                var unit=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                if(owned.SessionId!=attempt||owned.StableObjectId.ToString()!=binding.StableActorId||owned.FactionId!=1||
                    owned.IsStructure!=0||owned.ReservationId!=0||owned.CapacityReleased!=0||
                    unit.Role!=(binding.ObjectiveRole==SkirmishObjectiveRoleKind.BreakthroughDesignated?SkirmishRoleKind.Rifle:SkirmishRoleKind.ConvoyLogic))
                {error="Opening actor differs from immutable compiled original-ID plan.";return false;}
                var component=new SkirmishObjectiveRoleComponent{Role=binding.ObjectiveRole,FactionId=1,StableObjectId=owned.StableObjectId};
                if(em.HasComponent<SkirmishObjectiveRoleComponent>(actor))
                {
                    var existing=em.GetComponentData<SkirmishObjectiveRoleComponent>(actor);
                    if(existing.Role!=component.Role||existing.FactionId!=component.FactionId||existing.StableObjectId!=component.StableObjectId)
                    {error="Conflicting original objective designation.";return false;}
                }
                else em.AddComponentData(actor,component);
                error=null;return true;
            }
            error=null;return true;
        }
    }
}
