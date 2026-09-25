using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct UnitAttackSystem
    {
        private static bool IsTargetAvailableForCombat(EntityManager entityManager, Entity source, Entity target)
        {
            return entityManager.Exists(target) &&
                   !entityManager.HasComponent<CampaignMissionCombatSuppressedTag>(target) &&
                   CombatTargetPolicyUtility.Allows(entityManager, source, target);
        }
    }
}
