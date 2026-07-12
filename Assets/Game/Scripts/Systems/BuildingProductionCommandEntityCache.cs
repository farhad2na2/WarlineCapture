using Game.Components;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed class BuildingProductionCommandEntityCache
    {
        private World _world;
        private Entity _campItemEntity;
        private Entity _productionEntity;
        private bool _campItemResolved;
        private bool _productionResolved;

        public Entity GetOrCreateCampItem(EntityManager entityManager)
        {
            BindWorld(entityManager.World);
            if (IsValidCampItemEntity(entityManager))
            {
                EnsureCampItemBuffers(entityManager, _campItemEntity);
                return _campItemEntity;
            }

            if ((!_campItemResolved || _campItemEntity != Entity.Null) &&
                TryResolveCampItem(entityManager, out Entity entity))
                return entity;

            _campItemEntity = entityManager.CreateEntity(typeof(BuildingUiCampItemCommandQueueComponent));
            _campItemResolved = true;
            entityManager.SetName(_campItemEntity, "BuildingUiCampItemCommands");
            EnsureCampItemBuffers(entityManager, _campItemEntity);
            return _campItemEntity;
        }

        public bool TryGetCampItem(EntityManager entityManager, out Entity entity)
        {
            BindWorld(entityManager.World);
            if (IsValidCampItemEntity(entityManager))
            {
                EnsureCampItemBuffers(entityManager, _campItemEntity);
                entity = _campItemEntity;
                return true;
            }

            if (_campItemResolved && _campItemEntity == Entity.Null)
            {
                entity = Entity.Null;
                return false;
            }

            return TryResolveCampItem(entityManager, out entity);
        }

        public Entity GetOrCreateProduction(EntityManager entityManager)
        {
            BindWorld(entityManager.World);
            if (IsValidProductionEntity(entityManager))
            {
                EnsureProductionBuffers(entityManager, _productionEntity);
                return _productionEntity;
            }

            if ((!_productionResolved || _productionEntity != Entity.Null) &&
                TryResolveProduction(entityManager, out Entity entity))
                return entity;

            _productionEntity = entityManager.CreateEntity(typeof(BuildingUiProductionCommandQueueComponent));
            _productionResolved = true;
            entityManager.SetName(_productionEntity, "BuildingUiProductionCommands");
            EnsureProductionBuffers(entityManager, _productionEntity);
            return _productionEntity;
        }

        public bool TryGetProduction(EntityManager entityManager, out Entity entity)
        {
            BindWorld(entityManager.World);
            if (IsValidProductionEntity(entityManager))
            {
                EnsureProductionBuffers(entityManager, _productionEntity);
                entity = _productionEntity;
                return true;
            }

            if (_productionResolved && _productionEntity == Entity.Null)
            {
                entity = Entity.Null;
                return false;
            }

            return TryResolveProduction(entityManager, out entity);
        }

        private void BindWorld(World world)
        {
            if (_world == world)
                return;

            _world = world;
            _campItemEntity = Entity.Null;
            _productionEntity = Entity.Null;
            _campItemResolved = false;
            _productionResolved = false;
        }

        private bool IsValidCampItemEntity(EntityManager entityManager)
        {
            return _world != null &&
                   _world.IsCreated &&
                   _campItemEntity != Entity.Null &&
                   entityManager.Exists(_campItemEntity) &&
                   entityManager.HasComponent<BuildingUiCampItemCommandQueueComponent>(_campItemEntity);
        }

        private bool IsValidProductionEntity(EntityManager entityManager)
        {
            return _world != null &&
                   _world.IsCreated &&
                   _productionEntity != Entity.Null &&
                   entityManager.Exists(_productionEntity) &&
                   entityManager.HasComponent<BuildingUiProductionCommandQueueComponent>(_productionEntity);
        }

        private bool TryResolveCampItem(EntityManager entityManager, out Entity entity)
        {
            _campItemResolved = true;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingUiCampItemCommandQueueComponent>());
            if (query.IsEmptyIgnoreFilter)
            {
                _campItemEntity = Entity.Null;
                entity = Entity.Null;
                return false;
            }

            _campItemEntity = query.GetSingletonEntity();
            EnsureCampItemBuffers(entityManager, _campItemEntity);
            entity = _campItemEntity;
            return true;
        }

        private bool TryResolveProduction(EntityManager entityManager, out Entity entity)
        {
            _productionResolved = true;
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingUiProductionCommandQueueComponent>());
            if (query.IsEmptyIgnoreFilter)
            {
                _productionEntity = Entity.Null;
                entity = Entity.Null;
                return false;
            }

            _productionEntity = query.GetSingletonEntity();
            EnsureProductionBuffers(entityManager, _productionEntity);
            entity = _productionEntity;
            return true;
        }

        private static void EnsureCampItemBuffers(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasBuffer<BuildingUiCampItemCommandRequestElement>(entity))
                entityManager.AddBuffer<BuildingUiCampItemCommandRequestElement>(entity);
            if (!entityManager.HasBuffer<BuildingUiCampItemCommandResultElement>(entity))
                entityManager.AddBuffer<BuildingUiCampItemCommandResultElement>(entity);
        }

        private static void EnsureProductionBuffers(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasBuffer<BuildingUiProductionCommandRequestElement>(entity))
                entityManager.AddBuffer<BuildingUiProductionCommandRequestElement>(entity);
            if (!entityManager.HasBuffer<BuildingUiProductionCommandResultElement>(entity))
                entityManager.AddBuffer<BuildingUiProductionCommandResultElement>(entity);
        }
    }
}
