using Game.Components;
using Unity.Entities;
namespace Game.Runtime
{
    public static class SupportDamageUtilitySystemHelper
    {
        public static int Apply(int damage, ushort permille)
        {
            if(damage<=0) return 0;
            if(permille==0 || permille>=1000) return damage;
            return (int)System.Math.Max(1L, ((long)damage*permille+500L)/1000L);
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
