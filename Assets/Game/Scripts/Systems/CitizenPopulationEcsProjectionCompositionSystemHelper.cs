using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class CitizenPopulationEcsProjectionCompositionSystemHelper
    {
        private World _ecsWorld;
        private EntityManager _entityManager;
        private Entity _populationSummaryEntity;
        private EntityQuery _citizenEntityQuery;
        private EntityQuery _householdEntityQuery;
        private EntityQuery _gridConfigQuery;

        public bool HasWorld => _ecsWorld != null && _ecsWorld.IsCreated;
        public EntityManager EntityManager => _entityManager;

        public void ResolveEntityManager()
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;
            if (_ecsWorld == world && _entityManager.World.IsCreated)
                return;

            _ecsWorld = world;
            _entityManager = world.EntityManager;
            _citizenEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CitizenTag>(), ComponentType.ReadOnly<CitizenIdentity>());
            _householdEntityQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CitizenHouseholdTag>(), ComponentType.ReadOnly<CitizenHouseholdComponent>());
            _gridConfigQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
        }

        public void EnsurePopulationSummaryEntity()
        {
            if (!HasWorld)
                return;
            if (_populationSummaryEntity != Entity.Null && _entityManager.Exists(_populationSummaryEntity))
                return;

            EntityQuery query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<CitizenPopulationSummaryTag>(), ComponentType.ReadWrite<CitizenPopulationSummary>());
            if (!query.IsEmptyIgnoreFilter)
            {
                _populationSummaryEntity = query.GetSingletonEntity();
                query.Dispose();
                return;
            }

            query.Dispose();
            _populationSummaryEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(_populationSummaryEntity, new CitizenPopulationSummaryTag());
            _entityManager.AddComponentData(_populationSummaryEntity, default(CitizenPopulationSummary));
        }

        public void DestroyAllCitizenEntities(CitizenPopulationStateCompositionSystemHelper state)
        {
            if (!HasWorld)
                return;

            foreach (CitizenRecordComponent citizen in state.CitizensById.Values)
            {
                if (citizen.CitizenEntity != Entity.Null && _entityManager.Exists(citizen.CitizenEntity))
                    _entityManager.DestroyEntity(citizen.CitizenEntity);
            }

            foreach (CitizenHouseholdRecordComponent household in state.HouseholdsById.Values)
            {
                if (household.HouseholdEntity != Entity.Null && _entityManager.Exists(household.HouseholdEntity))
                    _entityManager.DestroyEntity(household.HouseholdEntity);
            }

            if (_populationSummaryEntity != Entity.Null && _entityManager.Exists(_populationSummaryEntity))
                _entityManager.DestroyEntity(_populationSummaryEntity);
        }

        public bool TryGetTotalsFromEcs(out CitizenPopulationSummary summary)
        {
            summary = default;
            if (!HasWorld)
                return false;
            if (_populationSummaryEntity == Entity.Null || !_entityManager.Exists(_populationSummaryEntity))
                return false;
            if (!_entityManager.HasComponent<CitizenPopulationSummary>(_populationSummaryEntity))
                return false;

            summary = _entityManager.GetComponentData<CitizenPopulationSummary>(_populationSummaryEntity);
            return true;
        }

        public void EnsureHouseholdEntity(ref CitizenHouseholdRecordComponent household)
        {
            if (!HasWorld)
                return;
            if (household.HouseholdEntity != Entity.Null && _entityManager.Exists(household.HouseholdEntity))
                return;

            household.HouseholdEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(household.HouseholdEntity, new CitizenHouseholdTag());
            _entityManager.AddComponentData(household.HouseholdEntity, default(CitizenHouseholdComponent));
        }

        public void EnsureCitizenEntity(ref CitizenRecordComponent citizen)
        {
            if (!HasWorld)
                return;
            if (citizen.CitizenEntity != Entity.Null && _entityManager.Exists(citizen.CitizenEntity))
                return;

            citizen.CitizenEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(citizen.CitizenEntity, new CitizenTag());
            _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenIdentity));
            _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenHouseholdRef));
            _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenHomeTarget));
            _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenAssignmentsComponent));
            _entityManager.AddComponentData(citizen.CitizenEntity, default(CitizenTimersComponent));
        }

        public void SyncHouseholdEntity(CitizenHouseholdRecordComponent household)
        {
            if (!HasWorld)
                return;
            if (household.HouseholdEntity == Entity.Null || !_entityManager.Exists(household.HouseholdEntity))
                return;

            _entityManager.SetComponentData(household.HouseholdEntity, new CitizenHouseholdComponent
            {
                HouseholdId = household.HouseholdId,
                HomeBuildingId = household.HomeBuildingId,
                MaleCitizenId = household.MaleCitizenId,
                FemaleCitizenId = household.FemaleCitizenId,
                RefugeeTentBuildingId = household.RefugeeTentBuildingId,
                IsDisplaced = household.IsDisplaced
            });
        }

        public void SyncCitizenEntity(CitizenPopulationStateCompositionSystemHelper state, CitizenRecordComponent citizen)
        {
            if (!HasWorld)
                return;
            if (citizen.CitizenEntity == Entity.Null || !_entityManager.Exists(citizen.CitizenEntity))
                return;

            Entity householdEntity = Entity.Null;
            if (state.TryGetHousehold(citizen.HouseholdId, out CitizenHouseholdRecordComponent household))
                householdEntity = household.HouseholdEntity;

            _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenIdentity
            {
                CitizenId = citizen.CitizenId,
                Gender = (byte)citizen.Gender,
                LifeState = (byte)citizen.LifeState,
                Status = (byte)citizen.Status
            });
            _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenHouseholdRef
            {
                HouseholdId = citizen.HouseholdId,
                HouseholdEntity = householdEntity
            });
            _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenHomeTarget
            {
                HomeBuildingId = citizen.HomeBuildingId,
                CurrentTargetBuildingId = citizen.CurrentTargetBuildingId
            });
            _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenAssignmentsComponent
            {
                WorkBuildingId = citizen.WorkBuildingId,
                PreferredShopBuildingId = citizen.PreferredShopBuildingId,
                LunchShopBuildingId = citizen.LunchShopBuildingId,
                PreferredWalkBuildingId = citizen.PreferredWalkBuildingId,
                PreferredCityHallBuildingId = citizen.PreferredCityHallBuildingId
            });
            _entityManager.SetComponentData(citizen.CitizenEntity, new CitizenTimersComponent
            {
                StateStartedAt = citizen.StateStartedAt,
                StateEndsAt = citizen.StateEndsAt
            });
        }

        public bool TryPublishSummary(CitizenPopulationTotals totals)
        {
            EnsurePopulationSummaryEntity();
            if (!HasWorld || _populationSummaryEntity == Entity.Null || !_entityManager.Exists(_populationSummaryEntity))
                return false;

            _entityManager.SetComponentData(_populationSummaryEntity, new CitizenPopulationSummary
            {
                Households = totals.Households,
                TotalCitizens = totals.TotalCitizens,
                HousedCitizens = totals.HousedCitizens,
                RefugeeCitizens = totals.RefugeeCitizens,
                DeadCitizens = totals.DeadCitizens
            });
            return true;
        }

        public bool HasHouseholdEntities()
        {
            return HasWorld && !_householdEntityQuery.IsEmptyIgnoreFilter;
        }

        public int GetCitizenEntityCount(int fallbackCount)
        {
            return HasWorld ? _citizenEntityQuery.CalculateEntityCount() : fallbackCount;
        }

        public int GetHouseholdEntityCount(int fallbackCount)
        {
            return HasWorld ? _householdEntityQuery.CalculateEntityCount() : fallbackCount;
        }

        public bool TryGetGridConfig(out GridConfig grid)
        {
            grid = default;
            if (!HasWorld || _gridConfigQuery.IsEmptyIgnoreFilter)
                return false;

            Entity gridEntity = _gridConfigQuery.GetSingletonEntity();
            grid = _entityManager.GetComponentData<GridConfig>(gridEntity);
            return true;
        }

        public void Reset()
        {
            _populationSummaryEntity = Entity.Null;
            _citizenEntityQuery = default;
            _householdEntityQuery = default;
            _gridConfigQuery = default;
            _entityManager = default;
            _ecsWorld = null;
        }
    }
}
