using Unity.Entities;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingGameplayEcsQueryCompositionSystemHelper
    {
        private Unity.Entities.World _queryWorld;
        private EntityQuery _gridDataQuery;
        private EntityQuery _redirectUnitsQuery;
        private EntityQuery _unitPrefabRegistryQuery;
        private EntityQuery _spawnPrefabCandidatesQuery;
        private EntityQuery _selectedUnitsQuery;
        private EntityQuery _haulerUnitsQuery;
        private EntityQuery _livePlayerUnitsQuery;
        private EntityQuery _liveUnitFootprintQuery;
        private EntityQuery _liveFactionUnitsQuery;
        private EntityQuery _buildingRuntimeBoundaryQuery;

        internal EntityQuery GridDataQuery => _gridDataQuery;
        internal EntityQuery RedirectUnitsQuery => _redirectUnitsQuery;
        internal EntityQuery UnitPrefabRegistryQuery => _unitPrefabRegistryQuery;
        internal EntityQuery SpawnPrefabCandidatesQuery => _spawnPrefabCandidatesQuery;
        internal EntityQuery SelectedUnitsQuery => _selectedUnitsQuery;
        internal EntityQuery HaulerUnitsQuery => _haulerUnitsQuery;
        internal EntityQuery LivePlayerUnitsQuery => _livePlayerUnitsQuery;
        internal EntityQuery LiveUnitFootprintQuery => _liveUnitFootprintQuery;
        internal EntityQuery LiveFactionUnitsQuery => _liveFactionUnitsQuery;
        internal EntityQuery BuildingRuntimeStateQuery => _buildingRuntimeBoundaryQuery;

        internal void EnsureEntityQueries(EntityManager em)
        {
            Unity.Entities.World world = em.World;
            if (_queryWorld == world && world != null && world.IsCreated)
                return;

            _queryWorld = world;
            _gridDataQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<GridRoad>(),
                ComponentType.ReadOnly<DynamicBlockerComponent>());
            _redirectUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitMove>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<LocalTransform>());
            _unitPrefabRegistryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            _spawnPrefabCandidatesQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Prefab>(),
                ComponentType.ReadOnly<UnitMove>());
            _selectedUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<SelectedUnitTag>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>());
            _haulerUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitResourceHauler>(),
                ComponentType.ReadOnly<UnitResourceHaulOrder>(),
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitMove>());
            _livePlayerUnitsQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitRespawnPrefab>(),
                ComponentType.ReadOnly<UnitMove>());
            _liveUnitFootprintQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>());
            _liveFactionUnitsQuery = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<UnitFootprint>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<StaticGridBlocker>(),
                    ComponentType.ReadOnly<RuntimeBuildingCombatTag>()
                }
            });
            _buildingRuntimeBoundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
        }
    }
}
