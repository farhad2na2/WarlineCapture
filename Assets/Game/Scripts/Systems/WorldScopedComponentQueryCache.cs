using System;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class WorldScopedComponentQueryCache<T> : IDisposable
        where T : unmanaged, IComponentData
    {
        private enum EntityResolution : byte
        {
            Unknown = 0,
            Missing = 1,
            Found = 2
        }

        private readonly bool _readOnly;
        private World _world;
        private EntityQuery _query;
        private ComponentType _componentType;
        private Entity _entity;
        private int _componentOrderVersion;
        private EntityResolution _entityResolution;
        private bool _queryCreated;
        private bool _disposed;

        public WorldScopedComponentQueryCache(bool readOnly)
        {
            _readOnly = readOnly;
        }

        public EntityQuery Get(EntityManager entityManager)
        {
            ThrowIfDisposed();
            World world = entityManager.World;
            if (_world == world && world != null && world.IsCreated && _queryCreated)
                return _query;

            ReleaseQuery();
            _world = world;
            _componentType = _readOnly
                ? ComponentType.ReadOnly<T>()
                : ComponentType.ReadWrite<T>();
            _query = entityManager.CreateEntityQuery(_componentType);
            _queryCreated = true;
            _componentOrderVersion = entityManager.GetComponentOrderVersion<T>();
            ResetEntityResolution();
            return _query;
        }

        public bool TryGetSingleton(EntityManager entityManager, out Entity entity)
        {
            EntityQuery query = Get(entityManager);
            if (_componentType.IsEnableable)
            {
                throw new NotSupportedException(
                    $"{nameof(WorldScopedComponentQueryCache<T>)} cannot cache singleton entities for enableable component type {typeof(T).Name}.");
            }

            if (_entityResolution == EntityResolution.Found &&
                CanReuseResolvedEntity(entityManager, query))
            {
                entity = _entity;
                return true;
            }

            if (_entityResolution == EntityResolution.Found)
                ResetEntityResolution();
            if (_entityResolution == EntityResolution.Missing)
            {
                entity = Entity.Null;
                return false;
            }

            if (query.IsEmptyIgnoreFilter)
            {
                _entity = Entity.Null;
                _entityResolution = EntityResolution.Missing;
                entity = Entity.Null;
                return false;
            }

            _entity = query.GetSingletonEntity();
            _entityResolution = EntityResolution.Found;
            entity = _entity;
            return true;
        }

        public void Invalidate()
        {
            ThrowIfDisposed();
            ReleaseQuery();
            _world = null;
            _componentType = default;
            _componentOrderVersion = 0;
            ResetEntityResolution();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ReleaseQuery();
            _world = null;
            _componentType = default;
            _componentOrderVersion = 0;
            ResetEntityResolution();
            _disposed = true;
        }

        private bool CanReuseResolvedEntity(EntityManager entityManager, EntityQuery query)
        {
            int componentOrderVersion = entityManager.GetComponentOrderVersion<T>();
            if (componentOrderVersion != _componentOrderVersion)
            {
                _componentOrderVersion = componentOrderVersion;
                return false;
            }

            return _world != null &&
                   _world.IsCreated &&
                   _entity != Entity.Null &&
                   entityManager.Exists(_entity) &&
                   entityManager.HasComponent<T>(_entity);
        }

        private void ReleaseQuery()
        {
            if (_queryCreated && _world != null && _world.IsCreated)
                _query.Dispose();
            _query = default;
            _queryCreated = false;
        }

        private void ResetEntityResolution()
        {
            _entity = Entity.Null;
            _entityResolution = EntityResolution.Unknown;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
