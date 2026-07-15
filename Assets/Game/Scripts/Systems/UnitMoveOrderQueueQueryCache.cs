using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class UnitMoveOrderQueueQueryCache
    {
        private World _world;
        private EntityQuery _query;

        public EntityQuery Get(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (_world == world && world != null && world.IsCreated)
                return _query;

            _world = world;
            _query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UnitMoveOrderQueueComponent>());
            return _query;
        }
    }
}
