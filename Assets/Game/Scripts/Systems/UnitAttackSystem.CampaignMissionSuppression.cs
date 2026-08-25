using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct UnitAttackSystem
    {
        private static bool IsTargetAvailableForCombat(EntityManager entityManager, Entity target)
        {
            return entityManager.Exists(target) &&
                   !entityManager.HasComponent<CampaignMissionCombatSuppressedTag>(target);
        }
    }
}
