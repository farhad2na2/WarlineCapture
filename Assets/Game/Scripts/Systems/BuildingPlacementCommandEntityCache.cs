using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementCommandEntityCache
    {
        private World _world;
        private Entity _entity;
        private bool _resolved;

        public Entity GetOrCreate(EntityManager entityManager)
        {
            BindWorld(entityManager.World);
            if (IsValid(entityManager))
            {
                EnsureBuffers(entityManager, _entity);
                return _entity;
            }

            if ((!_resolved || _entity != Entity.Null) && TryResolve(entityManager, out Entity entity))
                return entity;

            _entity = entityManager.CreateEntity(typeof(BuildingUiPlacementCommandQueueComponent));
            _resolved = true;
            entityManager.SetName(_entity, "BuildingUiPlacementCommands");
            EnsureBuffers(entityManager, _entity);
            return _entity;
        }

        public bool TryGet(EntityManager entityManager, out Entity entity)
        {
            BindWorld(entityManager.World);
            if (IsValid(entityManager))
            {
                EnsureBuffers(entityManager, _entity);
                entity = _entity;
                return true;
            }

            if (_resolved && _entity == Entity.Null)
            {
                entity = Entity.Null;
                return false;
            }

            return TryResolve(entityManager, out entity);
        }

        private void BindWorld(World world)
        {
            if (_world == world)
                return;

            _world = world;
            _entity = Entity.Null;
            _resolved = false;
        }

        private bool IsValid(EntityManager entityManager)
        {
            return _world != null &&
                   _world.IsCreated &&
                   _entity != Entity.Null &&
                   entityManager.Exists(_entity) &&
                   entityManager.HasComponent<BuildingUiPlacementCommandQueueComponent>(_entity);
        }

        private bool TryResolve(EntityManager entityManager, out Entity entity)
        {
            _resolved = true;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingUiPlacementCommandQueueComponent>());
            if (query.IsEmptyIgnoreFilter)
            {
                _entity = Entity.Null;
                entity = Entity.Null;
                return false;
            }

            _entity = query.GetSingletonEntity();
            EnsureBuffers(entityManager, _entity);
            entity = _entity;
            return true;
        }

        private static void EnsureBuffers(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasBuffer<BuildingUiPlacementCommandRequestElement>(entity))
                entityManager.AddBuffer<BuildingUiPlacementCommandRequestElement>(entity);
            if (!entityManager.HasBuffer<BuildingUiPlacementCommandResultElement>(entity))
                entityManager.AddBuffer<BuildingUiPlacementCommandResultElement>(entity);
        }
    }
}
