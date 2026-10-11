using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Rendering
{
    public static class SkirmishFogRenderVisibility
    {
        public static bool PlayerVisible(EntityManager em, Entity contact)
        {
            if (!em.Exists(contact)) return false;
            if (!em.HasComponent<SkirmishAttemptOwnedComponent>(contact)) return true;
            if (em.GetComponentData<SkirmishAttemptOwnedComponent>(contact).FactionId == 1) return true;
            return em.HasComponent<SkirmishContactSightComponent>(contact) &&
                em.GetComponentData<SkirmishContactSightComponent>(contact).Sight == SkirmishContactSight.Visible;
        }
        public static bool PositionObserved(EntityManager em, Entity source, float3 position, byte detectorDomain)
            => SkirmishFogObservedPointUtility.IsObserved(em, source, position, detectorDomain);
    }
}
