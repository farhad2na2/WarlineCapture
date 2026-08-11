using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal static class OperationMapRuntimeRootContract
    {
        public static Entity Create(EntityManager entityManager)
        {
            Entity entity = entityManager.CreateEntity(
                typeof(OperationMapRootComponent),
                typeof(OperationMapQueueComponent),
                typeof(OperationMapLoadStateComponent),
                typeof(ActiveOperationMapComponent),
                typeof(OperationMapBoundsComponent),
                typeof(OperationMapMetadataComponent),
                typeof(OperationMapReadinessComponent));
            entityManager.AddBuffer<OperationMapLoadRequestElement>(entity);
            entityManager.AddBuffer<OperationMapLoadResultElement>(entity);
            return entity;
        }

        public static void Ensure(EntityManager entityManager, Entity rootEntity)
        {
            EnsureComponent<OperationMapQueueComponent>(entityManager, rootEntity);
            EnsureComponent<OperationMapLoadStateComponent>(entityManager, rootEntity);
            EnsureComponent<ActiveOperationMapComponent>(entityManager, rootEntity);
            EnsureComponent<OperationMapBoundsComponent>(entityManager, rootEntity);
            EnsureComponent<OperationMapMetadataComponent>(entityManager, rootEntity);
            EnsureComponent<OperationMapReadinessComponent>(entityManager, rootEntity);
            if (!entityManager.HasBuffer<OperationMapLoadRequestElement>(rootEntity))
                entityManager.AddBuffer<OperationMapLoadRequestElement>(rootEntity);
            if (!entityManager.HasBuffer<OperationMapLoadResultElement>(rootEntity))
                entityManager.AddBuffer<OperationMapLoadResultElement>(rootEntity);
        }

        public static bool TryResolveSingle(
            EntityManager entityManager,
            out Entity rootEntity,
            out string error)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRootComponent>());
            using NativeArray<Entity> roots = query.ToEntityArray(Allocator.Temp);
            if (roots.Length > 1)
            {
                rootEntity = Entity.Null;
                error = "Exactly zero or one operation-map root is permitted before publication.";
                return false;
            }

            rootEntity = roots.Length == 1 ? roots[0] : Entity.Null;
            error = null;
            return true;
        }

        private static void EnsureComponent<T>(EntityManager entityManager, Entity entity)
            where T : unmanaged, IComponentData
        {
            if (!entityManager.HasComponent<T>(entity))
                entityManager.AddComponent<T>(entity);
        }
    }
}
