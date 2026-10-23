using Game.Components;
using Game.Runtime.Combat;
using Unity.Entities;
namespace Game.Runtime
{
    public static class SupportDamageUtilitySystemHelper
    {
        public static int Apply(int damage, ushort permille)
        {
            return DirectDamageSequencer.ApplyCover(damage, permille);
        }
        public static int ApplyToTarget(EntityManager em, Entity target, int damage)
        {
            if (!em.HasComponent<SupportRangedCoverComponent>(target) || em.HasComponent<UnitAirMovement>(target) ||
                em.HasComponent<UnitAirComponent>(target) || em.HasComponent<RuntimeBuildingCombatInfo>(target) ||
                !em.HasComponent<UnitGrid>(target)) return damage;
            return Apply(damage, em.GetComponentData<SupportRangedCoverComponent>(target).DirectDamagePermille);
        }
    }
}
