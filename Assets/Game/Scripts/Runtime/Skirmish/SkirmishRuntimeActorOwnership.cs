using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    /// <summary>Moves instantiated actors from map-stream lifetime to attempt lifetime.</summary>
    internal static class SkirmishRuntimeActorOwnership
    {
        internal static void DetachFromSourceScene(EntityManager em, Entity actor)
        {
            if (!em.Exists(actor) || em.HasComponent<Prefab>(actor)) return;
            if (em.HasBuffer<LinkedEntityGroup>(actor))
            {
                // Removing a shared component can invalidate the source buffer.
                using var linked = em.GetBuffer<LinkedEntityGroup>(actor).ToNativeArray(Allocator.Temp);
                foreach (var member in linked) Detach(em, member.Value);
            }
            Detach(em, actor);
        }

        private static void Detach(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity) || em.HasComponent<Prefab>(entity)) return;
            if (em.HasComponent<SceneTag>(entity)) em.RemoveComponent<SceneTag>(entity);
            if (em.HasComponent<SceneSection>(entity)) em.RemoveComponent<SceneSection>(entity);
        }
    }
}
