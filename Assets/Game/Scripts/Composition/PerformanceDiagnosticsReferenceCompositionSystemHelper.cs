using Game.Runtime;
using Unity.Entities;

namespace Game.Composition
{
    public sealed class PerformanceDiagnosticsReferenceCompositionSystemHelper
    {
        private World _world;
        private Entity _referenceEntity;

        public void Register(EntityManager entityManager, PerformanceDiagnosticsSystemHelper diagnostics)
        {
            if (diagnostics == null)
                return;

            Entity entity = GetOrCreateReferenceEntity(entityManager);
            entityManager.GetComponentObject<PerformanceDiagnosticsReferenceComponent>(entity).Diagnostics = diagnostics;
        }

        public bool TryGet(EntityManager entityManager, out PerformanceDiagnosticsSystemHelper diagnostics)
        {
            diagnostics = null;
            if (!TryGetReferenceEntity(entityManager, out Entity entity))
                return false;

            diagnostics = entityManager.GetComponentObject<PerformanceDiagnosticsReferenceComponent>(entity).Diagnostics;
            return diagnostics != null;
        }

        public void Clear(EntityManager entityManager, PerformanceDiagnosticsSystemHelper owner)
        {
            if (!TryGetReferenceEntity(entityManager, out Entity entity))
                return;
            if (!ReferenceEquals(
                    entityManager.GetComponentObject<PerformanceDiagnosticsReferenceComponent>(entity).Diagnostics,
                    owner))
            {
                return;
            }

            entityManager.DestroyEntity(entity);
            _referenceEntity = Entity.Null;
        }

        private Entity GetOrCreateReferenceEntity(EntityManager entityManager)
        {
            if (TryGetReferenceEntity(entityManager, out Entity entity))
                return entity;

            entity = entityManager.CreateEntity();
            entityManager.AddComponentObject(entity, new PerformanceDiagnosticsReferenceComponent());
            entityManager.SetName(entity, "PerformanceDiagnosticsReference");
            _world = entityManager.World;
            _referenceEntity = entity;
            return entity;
        }

        private bool TryGetReferenceEntity(EntityManager entityManager, out Entity entity)
        {
            World world = entityManager.World;
            if (_world == world &&
                _referenceEntity != Entity.Null &&
                entityManager.Exists(_referenceEntity) &&
                entityManager.HasComponent<PerformanceDiagnosticsReferenceComponent>(_referenceEntity))
            {
                entity = _referenceEntity;
                return true;
            }

            _world = world;
            _referenceEntity = Entity.Null;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PerformanceDiagnosticsReferenceComponent>());
            if (query.IsEmptyIgnoreFilter)
            {
                entity = Entity.Null;
                return false;
            }

            entity = query.GetSingletonEntity();
            _referenceEntity = entity;
            return true;
        }
    }
}
