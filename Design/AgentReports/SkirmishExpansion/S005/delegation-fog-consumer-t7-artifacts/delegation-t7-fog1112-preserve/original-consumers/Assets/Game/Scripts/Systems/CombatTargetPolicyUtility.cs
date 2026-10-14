using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public static class CombatTargetPolicyUtility
    {
        public static bool Allows(bool hasSourcePolicy, in CombatTargetPolicy source,
            bool hasTargetPolicy, in CombatTargetPolicy target)
        {
            if (hasTargetPolicy && target.Visible == 0) return false;
            if (!hasSourcePolicy) return true;
            var targetDomain = hasTargetPolicy ? target.Domain : CombatTargetDomain.All;
            return (source.AllowedTargets & targetDomain) != CombatTargetDomain.None;
        }

        public static bool Allows(EntityManager em, Entity source, Entity target)
        {
            if (!em.Exists(source) || !em.Exists(target)) return false;
            bool hasSource = em.HasComponent<CombatTargetPolicy>(source);
            bool hasTarget = em.HasComponent<CombatTargetPolicy>(target);
            var sourcePolicy = hasSource ? em.GetComponentData<CombatTargetPolicy>(source) : default;
            var targetPolicy = hasTarget ? em.GetComponentData<CombatTargetPolicy>(target) : new CombatTargetPolicy
            {
                Visible = 1,
                Domain = InferDomain(em.HasComponent<RuntimeBuildingCombatInfo>(target) || em.HasComponent<StaticGridBlocker>(target),
                    em.HasComponent<UnitAirMovement>(target), em.HasComponent<UnitMovementBehavior>(target) &&
                    em.GetComponentData<UnitMovementBehavior>(target).UsesVehicleMotion != 0)
            };
            return Allows(hasSource, sourcePolicy, true, targetPolicy);
        }

        public static CombatTargetDomain InferDomain(bool structure, bool air, bool vehicle) =>
            structure ? CombatTargetDomain.Structure : air ? CombatTargetDomain.Air :
            vehicle ? CombatTargetDomain.Ground : CombatTargetDomain.Infantry;
    }
}
