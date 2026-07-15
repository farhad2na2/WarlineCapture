using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingFactionAIOilAllocationInputSystemHelper
    {
        private readonly BuildingGameplayEcsQueryCompositionSystemHelper _querySource;
        private Unity.Entities.World _queryWorld;
        private EntityQuery _aiBuildPlanQuery;
        private EntityQuery _factionEconomyQuery;
        private EntityQuery _factionControlQuery;

        internal BuildingFactionAIOilAllocationInputSystemHelper(
            BuildingGameplayEcsQueryCompositionSystemHelper querySource)
        {
            _querySource = querySource;
        }

        internal bool TryResolveFactionAIOilAllocationInput(
            EntityManager em,
            byte factionId,
            out BuildingResourceHaulerBridgeCompositionSystemHelper.FactionAIOilAllocationInput input)
        {
            input = default;
            _querySource.EnsureEntityQueries(em);
            EnsureEntityQueries(em);
            EntityQuery buildingRuntimeBoundaryQuery = _querySource.BuildingRuntimeStateQuery;
            if (buildingRuntimeBoundaryQuery.CalculateEntityCount() != 1)
                return false;

            Entity planEntity = FindAIBuildPlanEntity(em, factionId);
            if (planEntity == Entity.Null || !IsFactionAIControlled(em, factionId))
                return false;

            Entity economyEntity = FindFactionEconomyEntity(em, factionId);
            if (economyEntity == Entity.Null)
                return false;

            Entity boundaryEntity = buildingRuntimeBoundaryQuery.GetSingletonEntity();
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

        private void EnsureEntityQueries(EntityManager em)
        {
            Unity.Entities.World world = em.World;
            if (_queryWorld == world && world != null && world.IsCreated)
                return;

            _queryWorld = world;
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
