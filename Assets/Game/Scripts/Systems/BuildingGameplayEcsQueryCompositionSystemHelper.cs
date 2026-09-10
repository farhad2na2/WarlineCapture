using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingGameplayEcsQueryCompositionSystemHelper
    {
        private World _queryWorld;
        private EntityQuery _gridDataQuery, _redirectQuery, _prefabRegistryQuery, _spawnPrefabsQuery;
        private EntityQuery _selectedQuery, _haulerQuery, _playerUnitsQuery, _liveUnitFootprintQuery;
        private EntityQuery _factionUnitsQuery, _stateQuery, _missionQuery, _operationMapQuery, _surfaceQuery;
        internal readonly BuildingFactionAIOilAllocationInputSystemHelper AIOilInputSystemHelper;
        internal BuildingGameplayEcsQueryCompositionSystemHelper() => AIOilInputSystemHelper = new(this);

        internal EntityQuery GridDataQuery => _gridDataQuery;
        internal EntityQuery RedirectUnitsQuery => _redirectQuery;
        internal EntityQuery UnitPrefabRegistryQuery => _prefabRegistryQuery;
        internal EntityQuery SpawnPrefabCandidatesQuery => _spawnPrefabsQuery;
        internal EntityQuery SelectedUnitsQuery => _selectedQuery;
        internal EntityQuery HaulerUnitsQuery => _haulerQuery;
        internal EntityQuery LivePlayerUnitsQuery => _playerUnitsQuery;
        internal EntityQuery LiveUnitFootprintQuery => _liveUnitFootprintQuery;
        internal EntityQuery LiveFactionUnitsQuery => _factionUnitsQuery;
        internal EntityQuery BuildingRuntimeStateQuery => _stateQuery;
        internal EntityQuery CampaignMissionQuery => _missionQuery;
        internal EntityQuery OperationMapQuery => _operationMapQuery;
        internal EntityQuery SurfaceQuery => _surfaceQuery;
        internal bool TryResolveFactionAIOilAllocationInput(
            EntityManager em,
            byte factionId,
            out BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput input) =>
            AIOilInputSystemHelper.TryResolveFactionAIOilAllocationInput(em, factionId, out input);

        internal void EnsureEntityQueries(EntityManager em)
        {
            World world = em.World;
            if (_queryWorld == world && world != null && world.IsCreated)
                return;

            _queryWorld = world;
            _surfaceQuery = em.CreateEntityQuery(R<MapSurfaceComponent>());
            _gridDataQuery = em.CreateEntityQuery(R<GridConfig>(), R<GridRoad>(), R<DynamicBlockerComponent>());
            _redirectQuery = em.CreateEntityQuery(R<UnitMove>(), R<UnitGrid>(), R<LocalTransform>());
            _prefabRegistryQuery = em.CreateEntityQuery(R<UnitPrefabRegistryTag>(), R<UnitPrefabRegistryEntry>());
            _spawnPrefabsQuery = em.CreateEntityQuery(R<Prefab>(), R<UnitMove>());
            _selectedQuery = em.CreateEntityQuery(R<SelectedUnitTag>(), R<UnitGrid>(), R<UnitMove>());
            _haulerQuery = em.CreateEntityQuery(R<UnitResourceHauler>(), R<UnitGrid>(), R<UnitMove>());
            _playerUnitsQuery = em.CreateEntityQuery(R<Faction>(), R<UnitRespawnPrefab>(), R<UnitMove>());
            _liveUnitFootprintQuery = em.CreateEntityQuery(R<UnitGrid>(), R<UnitFootprint>());
            _factionUnitsQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { R<Faction>(), R<UnitGrid>(), R<UnitFootprint>() },
                None = new[] { R<StaticGridBlocker>(), R<RuntimeBuildingCombatTag>() }
            });
            _stateQuery = em.CreateEntityQuery(R<BuildingRuntimeStateTag>());
            _missionQuery = em.CreateEntityQuery(R<CampaignMissionRootComponent>(),
                R<CampaignMissionCatalogComponent>(), R<CampaignMissionRuntimeComponent>());
            _operationMapQuery = em.CreateEntityQuery(R<OperationMapRootComponent>(),
                R<ActiveOperationMapComponent>(), R<OperationMapMetadataComponent>());
        }

        private static ComponentType R<T>() => ComponentType.ReadOnly<T>();
    }
}
