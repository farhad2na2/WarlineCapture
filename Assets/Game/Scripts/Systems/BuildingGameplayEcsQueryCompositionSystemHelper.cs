using Unity.Collections;
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
        private EntityQuery _aiBuildPlanQuery;
        private EntityQuery _factionEconomyQuery;
        private EntityQuery _factionControlQuery;

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
            _aiBuildPlanQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<AIBuildPlan>(),
                ComponentType.ReadOnly<AIBuildPlanEntry>());
            _factionEconomyQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<FactionEconomy>(),
                ComponentType.ReadOnly<FactionTacticalMaterialsComponent>());
            _factionControlQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<FactionControlConfigTag>(),
                ComponentType.ReadOnly<FactionControlEntry>());
        }

        internal bool TryResolveFactionAIOilAllocationInput(
            EntityManager em,
            byte factionId,
            out BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput input)
        {
            input = default;
            EnsureEntityQueries(em);
            if (_buildingRuntimeBoundaryQuery.CalculateEntityCount() != 1)
                return false;

            Entity planEntity = FindAIBuildPlanEntity(em, factionId);
            if (planEntity == Entity.Null || !IsFactionAIControlled(em, factionId))
                return false;

            Entity economyEntity = FindFactionEconomyEntity(em, factionId);
            if (economyEntity == Entity.Null)
                return false;

            Entity boundaryEntity = _buildingRuntimeBoundaryQuery.GetSingletonEntity();
            if (!em.HasBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity) ||
                !em.HasBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity) ||
                !em.HasBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity))
            {
                return false;
            }

            AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(planEntity);
            FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
            FactionTacticalMaterialsComponent materials =
                em.GetComponentData<FactionTacticalMaterialsComponent>(economyEntity);
            AIBuildPlannerSystem.BuildDecision decision = AIBuildPlannerSystem.SelectBuildDecision(
                em.GetBuffer<AIBuildPlanEntry>(planEntity, true),
                em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundaryEntity, true),
                em.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(boundaryEntity, true),
                em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaryEntity, true),
                plan,
                economy,
                materials);

            ResolveFactionFuelSnapshot(
                em,
                boundaryEntity,
                factionId,
                out float storedFuelBarrels,
                out int fuelStorageCapacity);
            input = new BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput(
                decision.MaterialsCost,
                materials.Current,
                materials.Capacity,
                storedFuelBarrels,
                fuelStorageCapacity);
            return true;
        }

        private Entity FindAIBuildPlanEntity(EntityManager em, byte factionId)
        {
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<AIBuildPlan> planType = em.GetComponentTypeHandle<AIBuildPlan>(true);
            using NativeArray<ArchetypeChunk> chunks =
                _aiBuildPlanQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<AIBuildPlan> plans = chunk.GetNativeArray(ref planType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (plans[i].Enabled != 0 && plans[i].FactionId == factionId)
                        return entities[i];
                }
            }

            return Entity.Null;
        }

        private bool IsFactionAIControlled(EntityManager em, byte factionId)
        {
            if (_factionControlQuery.CalculateEntityCount() != 1)
                return FactionIdentity.IsAiControlledByDefault(factionId);

            DynamicBuffer<FactionControlEntry> controls =
                em.GetBuffer<FactionControlEntry>(_factionControlQuery.GetSingletonEntity(), true);
            for (int i = 0; i < controls.Length; i++)
            {
                FactionControlEntry control = controls[i];
                if (control.FactionId == factionId)
                    return control.AIControlled != 0;
            }

            return FactionIdentity.IsAiControlledByDefault(factionId);
        }

        private Entity FindFactionEconomyEntity(EntityManager em, byte factionId)
        {
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            ComponentTypeHandle<FactionEconomy> economyType =
                em.GetComponentTypeHandle<FactionEconomy>(true);
            ComponentTypeHandle<FactionTacticalMaterialsComponent> materialsType =
                em.GetComponentTypeHandle<FactionTacticalMaterialsComponent>(true);
            using NativeArray<ArchetypeChunk> chunks =
                _factionEconomyQuery.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
                NativeArray<FactionEconomy> economies = chunk.GetNativeArray(ref economyType);
                NativeArray<FactionTacticalMaterialsComponent> materials =
                    chunk.GetNativeArray(ref materialsType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (economies[i].FactionId == factionId && materials[i].FactionId == factionId)
                        return entities[i];
                }
            }

            return Entity.Null;
        }

        private static void ResolveFactionFuelSnapshot(
            EntityManager em,
            Entity boundaryEntity,
            byte factionId,
            out float storedFuelBarrels,
            out int fuelStorageCapacity)
        {
            storedFuelBarrels = 0f;
            fuelStorageCapacity = 0;
            if (!em.HasBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundaryEntity))
                return;

            DynamicBuffer<BuildingRuntimeFactionUsableFuelSummary> summaries =
                em.GetBuffer<BuildingRuntimeFactionUsableFuelSummary>(boundaryEntity, true);
            for (int i = 0; i < summaries.Length; i++)
            {
                BuildingRuntimeFactionUsableFuelSummary summary = summaries[i];
                if (summary.FactionId != factionId)
                    continue;

                storedFuelBarrels = summary.StoredFuelBarrels;
                fuelStorageCapacity = summary.FuelStorageCapacity;
                return;
            }
        }
    }
}
