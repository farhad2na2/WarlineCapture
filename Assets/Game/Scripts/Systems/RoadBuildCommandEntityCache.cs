using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class RoadBuildCommandEntityCache
    {
        private World _world;
        private Entity _entity;

        public Entity GetOrCreate(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (_world == world &&
                world != null &&
                world.IsCreated &&
                _entity != Entity.Null &&
                entityManager.Exists(_entity) &&
                entityManager.HasComponent<RoadBuildCommandQueueComponent>(_entity))
            {
                EnsureBuffers(entityManager, _entity);
                return _entity;
            }

            _world = world;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RoadBuildCommandQueueComponent>());
            bool createEntity = query.IsEmptyIgnoreFilter;
            _entity = createEntity
                ? entityManager.CreateEntity(typeof(RoadBuildCommandQueueComponent))
                : query.GetSingletonEntity();
            if (createEntity)
                entityManager.SetName(_entity, "RoadBuildCommands");
            EnsureBuffers(entityManager, _entity);
            return _entity;
        }

        private static void EnsureBuffers(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasBuffer<RoadBuildCommandRequestElement>(entity))
                entityManager.AddBuffer<RoadBuildCommandRequestElement>(entity);
            if (!entityManager.HasBuffer<RoadBuildCommandResultElement>(entity))
                entityManager.AddBuffer<RoadBuildCommandResultElement>(entity);
        }
    }
}
