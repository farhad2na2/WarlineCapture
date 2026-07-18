using Game.Components;
using Game.Configs;
using Unity.Entities;

namespace Game.Composition
{
    internal struct MatchAISettingsStartupComponent : IComponentData
    {
        public AISettingsSnapshot Snapshot;
    }

    internal static class MatchAISettingsStartupProjection
    {
        public static bool Project(EntityManager entityManager, AISettingsSnapshot snapshot)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MatchStartStateComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            Entity entity = query.GetSingletonEntity();
            var component = new MatchAISettingsStartupComponent { Snapshot = snapshot };
            if (entityManager.HasComponent<MatchAISettingsStartupComponent>(entity))
                entityManager.SetComponentData(entity, component);
            else
                entityManager.AddComponentData(entity, component);
            return true;
        }

        public static bool TryConsume(EntityManager entityManager, out AISettingsSnapshot snapshot)
        {
            snapshot = AISettingsSnapshot.Defaults;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MatchStartStateComponent>(),
                ComponentType.ReadOnly<MatchAISettingsStartupComponent>());
            if (query.CalculateEntityCount() != 1)
                return false;

            Entity entity = query.GetSingletonEntity();
            snapshot = entityManager.GetComponentData<MatchAISettingsStartupComponent>(entity).Snapshot;
            entityManager.RemoveComponent<MatchAISettingsStartupComponent>(entity);
            return true;
        }
    }
}
