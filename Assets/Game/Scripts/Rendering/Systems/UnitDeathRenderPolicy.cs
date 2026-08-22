using Game.Components;
using Unity.Entities;

namespace Game.Rendering
{
    public readonly struct UnitDeathRenderPolicy
    {
        public bool ShouldHideDeadLiveVisualRoots(bool hasAnimatedCorpseState)
        {
            return !hasAnimatedCorpseState;
        }

        public bool ShouldUseDestroyedVisual(EntityManager entityManager, Entity entity)
        {
            return entityManager.HasComponent<VehicleDestroyedVisualInstanceReference>(entity) ||
                   entityManager.HasComponent<VehicleDestroyedVisualSpawnRequest>(entity) ||
                   (entityManager.HasComponent<UnitHealth>(entity) &&
                    entityManager.GetComponentData<UnitHealth>(entity).Current <= 0 &&
                    !entityManager.HasComponent<UnitDeathAnimationComponent>(entity));
        }
    }
}
