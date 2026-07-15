using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingGameplayEcsQueryCompositionSystemHelper
    {
        private World _queryWorld;
        private EntityQuery _gridDataQuery;
        private EntityQuery _redirectQuery;
        private EntityQuery _prefabRegistryQuery;
        private EntityQuery _spawnPrefabsQuery;
        private EntityQuery _selectedQuery;
        private EntityQuery _haulerQuery;
        private EntityQuery _playerUnitsQuery;
        private EntityQuery _liveUnitFootprintQuery;
        private EntityQuery _factionUnitsQuery;
        private EntityQuery _runtimeBoundaryQuery;
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
        internal EntityQuery BuildingRuntimeStateQuery => _runtimeBoundaryQuery;

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
            _gridDataQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridRoad>(), ComponentType.ReadOnly<DynamicBlockerComponent>());
            _redirectQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<LocalTransform>());
            _prefabRegistryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            _spawnPrefabsQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            _selectedQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitMove>());
            _haulerQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitResourceHauler>(),
                ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitMove>());
            _playerUnitsQuery = em.CreateEntityQuery(ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitRespawnPrefab>(), ComponentType.ReadOnly<UnitMove>());
            _liveUnitFootprintQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());
            _factionUnitsQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<UnitFootprint>() },
                None = new[] { ComponentType.ReadOnly<StaticGridBlocker>(), ComponentType.ReadOnly<RuntimeBuildingCombatTag>() }
            });
            _runtimeBoundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
        }
    }
}
