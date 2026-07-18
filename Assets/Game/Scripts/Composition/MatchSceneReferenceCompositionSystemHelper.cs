using Unity.Entities;

namespace Game.Composition
{
    public sealed class MatchSceneReferenceCompositionSystemHelper
    {
        private World _world;
        private Entity _referenceEntity;

        public void Register(EntityManager entityManager, MatchSceneView view)
        {
            if (view == null)
                return;

            Entity entity = GetOrCreateReferenceEntity(entityManager);
            entityManager.SetComponentData(entity, new MatchSceneReferenceComponent { View = view });
        }

        public bool TryGet(EntityManager entityManager, out MatchSceneView view)
        {
            view = null;
            if (!TryGetReferenceEntity(entityManager, out Entity entity))
                return false;

            view = entityManager.GetComponentData<MatchSceneReferenceComponent>(entity).View.Value;
            return view != null;
        }

        public void Clear(EntityManager entityManager, MatchSceneView owner)
        {
            if (!TryGetReferenceEntity(entityManager, out Entity entity))
                return;

            MatchSceneView current = entityManager.GetComponentData<MatchSceneReferenceComponent>(entity).View.Value;
            if (current != owner)
                return;

            entityManager.DestroyEntity(entity);
            _referenceEntity = Entity.Null;
        }

        private Entity GetOrCreateReferenceEntity(EntityManager entityManager)
        {
            if (TryGetReferenceEntity(entityManager, out Entity entity))
                return entity;

            entity = entityManager.CreateEntity(typeof(MatchSceneReferenceComponent));
            entityManager.SetName(entity, "MatchSceneReference");
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
                entityManager.HasComponent<MatchSceneReferenceComponent>(_referenceEntity))
            {
                entity = _referenceEntity;
                return true;
            }

            _world = world;
            _referenceEntity = Entity.Null;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MatchSceneReferenceComponent>());
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
