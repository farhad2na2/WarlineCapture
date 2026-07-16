using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class WorldScopedComponentQueryCache<T>
        where T : unmanaged, IComponentData
    {
        private readonly bool _readOnly;
        private World _world;
        private EntityQuery _query;

        public WorldScopedComponentQueryCache(bool readOnly)
        {
            _readOnly = readOnly;
        }

        public EntityQuery Get(EntityManager entityManager)
        {
            World world = entityManager.World;
            if (_world == world && world != null && world.IsCreated)
                return _query;

            _world = world;
            ComponentType componentType = _readOnly
                ? ComponentType.ReadOnly<T>()
                : ComponentType.ReadWrite<T>();
            _query = entityManager.CreateEntityQuery(componentType);
            return _query;
        }
    }
}
