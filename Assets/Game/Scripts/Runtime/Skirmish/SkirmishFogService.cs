using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public static class SkirmishFogService
    {
        public static void Project(EntityManager em, Entity session)
        {
            if (!em.HasComponent<SkirmishFogStateComponent>(session))
                return;

            SkirmishFogStateComponent fog = em.GetComponentData<SkirmishFogStateComponent>(session);
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            using var query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                var owned = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if (!owned.SessionId.Equals(sessionId))
                    continue;
                WriteSight(em, entity, Resolve(fog, owned.FactionId, Current(em, entity)));
            }
        }

        public static bool IsVisible(EntityManager em, Entity contact)
        {
            return Current(em, contact) == SkirmishContactSight.Visible;
        }

        public static void Reveal(EntityManager em, Entity contact)
        {
            WriteSight(em, contact, SkirmishContactSight.Visible);
        }

        public static void Hide(EntityManager em, Entity contact)
        {
            WriteSight(em, contact, SkirmishContactSight.Unknown);
        }

        private static SkirmishContactSight Resolve(
            SkirmishFogStateComponent fog,
            byte factionId,
            SkirmishContactSight current)
        {
            if (factionId == 1 || fog.DevelopmentFullVision != 0 || fog.SharedFog == 0)
                return SkirmishContactSight.Visible;
            return current == SkirmishContactSight.Visible
                ? SkirmishContactSight.LastSeen
                : SkirmishContactSight.Unknown;
        }

        private static SkirmishContactSight Current(EntityManager em, Entity entity)
        {
            return em.HasComponent<SkirmishContactSightComponent>(entity)
                ? em.GetComponentData<SkirmishContactSightComponent>(entity).Sight
                : SkirmishContactSight.Unknown;
        }

        private static void WriteSight(EntityManager em, Entity entity, SkirmishContactSight sight)
        {
            var component = new SkirmishContactSightComponent { Sight = sight };
            if (em.HasComponent<SkirmishContactSightComponent>(entity))
                em.SetComponentData(entity, component);
            else
                em.AddComponentData(entity, component);
        }
    }
}
