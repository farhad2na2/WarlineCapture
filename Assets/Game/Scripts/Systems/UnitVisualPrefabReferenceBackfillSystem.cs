using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitVisualPrefabReferenceBackfillSystem : ISystem
{
    private EntityQuery _unitsToPatchQuery;

    public void OnCreate(ref SystemState state)
    {
        _unitsToPatchQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitSourcePrefabKey>()
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitVisualPrefabReferencesBackfilledTag>()
            }
        });
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_unitsToPatchQuery.IsEmptyIgnoreFilter)
            return;

        EntityManager em = state.EntityManager;
        using var referencesBySourceKey = new NativeHashMap<FixedString64Bytes, UnitVisualPrefabReferenceSet>(64, Allocator.Temp);
        UnitVisualPrefabReferenceSet defaultReferences = BuildReferenceLookup(em, referencesBySourceKey);
        MergeSharedRegistryReferences(em, ref defaultReferences);
        MergeInitialSpawnReferences(em, ref defaultReferences);
        if (referencesBySourceKey.Count == 0 && !defaultReferences.HasSharedMarkerOrHealth)
            return;

        var unitsToPatch = new NativeList<Entity>(Allocator.Temp);
        EntityTypeHandle entityType = state.GetEntityTypeHandle();
        ComponentTypeHandle<UnitSourcePrefabKey> sourceKeyType = state.GetComponentTypeHandle<UnitSourcePrefabKey>(true);
        using NativeArray<ArchetypeChunk> chunks = _unitsToPatchQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<UnitSourcePrefabKey> sourceKeys = chunk.GetNativeArray(ref sourceKeyType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                UnitSourcePrefabKey sourceKey = sourceKeys[i];
                if (sourceKey.Value.Length == 0)
                {
                    AddBackfilledTag(em, entity);
                    continue;
                }

                unitsToPatch.Add(entity);
            }
        }

        for (int i = 0; i < unitsToPatch.Length; i++)
            PatchUnit(em, unitsToPatch[i], referencesBySourceKey, defaultReferences);

        unitsToPatch.Dispose();
    }

    private static UnitVisualPrefabReferenceSet BuildReferenceLookup(
        EntityManager em,
        NativeHashMap<FixedString64Bytes, UnitVisualPrefabReferenceSet> referencesBySourceKey)
    {
        UnitVisualPrefabReferenceSet defaultReferences = default;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Prefab>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>());
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        ComponentTypeHandle<UnitSourcePrefabKey> sourceKeyType = em.GetComponentTypeHandle<UnitSourcePrefabKey>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> prefabs = chunk.GetNativeArray(entityType);
            NativeArray<UnitSourcePrefabKey> sourceKeys = chunk.GetNativeArray(ref sourceKeyType);
            for (int i = 0; i < prefabs.Length; i++)
            {
                Entity prefab = prefabs[i];
                FixedString64Bytes sourceKey = sourceKeys[i].Value;
                if (sourceKey.Length == 0)
                    continue;

                UnitVisualPrefabReferenceSet refs = CreateReferenceSet(em, prefab);
                if (!refs.HasAny)
                    continue;

                if (defaultReferences.SelectionMarkerPrefab == Entity.Null && refs.SelectionMarkerPrefab != Entity.Null)
                    defaultReferences.SelectionMarkerPrefab = refs.SelectionMarkerPrefab;
                if (defaultReferences.HealthBarPrefab == Entity.Null && refs.HealthBarPrefab != Entity.Null)
                    defaultReferences.HealthBarPrefab = refs.HealthBarPrefab;

                referencesBySourceKey[sourceKey] = refs;
            }
        }

        return defaultReferences;
    }

    private static void MergeSharedRegistryReferences(EntityManager em, ref UnitVisualPrefabReferenceSet defaultReferences)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<UnitSharedVisualPrefabReferences>());
        if (query.IsEmptyIgnoreFilter)
            return;

        ComponentTypeHandle<UnitSharedVisualPrefabReferences> sharedType = em.GetComponentTypeHandle<UnitSharedVisualPrefabReferences>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<UnitSharedVisualPrefabReferences> sharedReferences = chunks[chunkIndex].GetNativeArray(ref sharedType);
            for (int i = 0; i < sharedReferences.Length; i++)
            {
                UnitSharedVisualPrefabReferences shared = sharedReferences[i];
                if (defaultReferences.SelectionMarkerPrefab == Entity.Null && shared.SelectionMarkerPrefab != Entity.Null)
                    defaultReferences.SelectionMarkerPrefab = shared.SelectionMarkerPrefab;
                if (defaultReferences.HealthBarPrefab == Entity.Null && shared.HealthBarPrefab != Entity.Null)
                    defaultReferences.HealthBarPrefab = shared.HealthBarPrefab;

                if (defaultReferences.SelectionMarkerPrefab != Entity.Null && defaultReferences.HealthBarPrefab != Entity.Null)
                    return;
            }
        }
    }

    private static void MergeInitialSpawnReferences(EntityManager em, ref UnitVisualPrefabReferenceSet defaultReferences)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<InitialUnitsSpawnConfig>());
        if (query.IsEmptyIgnoreFilter)
            return;

        ComponentTypeHandle<InitialUnitsSpawnConfig> configType = em.GetComponentTypeHandle<InitialUnitsSpawnConfig>(true);
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<InitialUnitsSpawnConfig> configs = chunks[chunkIndex].GetNativeArray(ref configType);
            for (int i = 0; i < configs.Length; i++)
            {
                InitialUnitsSpawnConfig config = configs[i];
                if (defaultReferences.SelectionMarkerPrefab == Entity.Null && config.UnitSelectionMarkerPrefab != Entity.Null)
                    defaultReferences.SelectionMarkerPrefab = config.UnitSelectionMarkerPrefab;
                if (defaultReferences.HealthBarPrefab == Entity.Null && config.UnitHealthBarPrefab != Entity.Null)
                    defaultReferences.HealthBarPrefab = config.UnitHealthBarPrefab;

                if (defaultReferences.SelectionMarkerPrefab != Entity.Null && defaultReferences.HealthBarPrefab != Entity.Null)
                    return;
            }
        }
    }

    private static void PatchUnit(
        EntityManager em,
        Entity unit,
        NativeHashMap<FixedString64Bytes, UnitVisualPrefabReferenceSet> referencesBySourceKey,
        UnitVisualPrefabReferenceSet defaultReferences)
    {
        if (!em.Exists(unit) || !em.HasComponent<UnitSourcePrefabKey>(unit))
            return;

        if (HasAllUnitVisualPrefabReferences(em, unit))
        {
            AddBackfilledTag(em, unit);
            return;
        }

        FixedString64Bytes sourceKey = em.GetComponentData<UnitSourcePrefabKey>(unit).Value;
        UnitVisualPrefabReferenceSet refs = default;
        if (sourceKey.Length > 0)
            referencesBySourceKey.TryGetValue(sourceKey, out refs);

        refs = MergeSharedDefaults(refs, defaultReferences);
        if (!refs.HasAny)
        {
            return;
        }

        if (refs.SelectionMarkerPrefab != Entity.Null &&
            em.Exists(refs.SelectionMarkerPrefab) &&
            !em.HasComponent<UnitSelectionMarkerPrefabReference>(unit))
        {
            em.AddComponentData(unit, new UnitSelectionMarkerPrefabReference { Prefab = refs.SelectionMarkerPrefab });
        }

        if (refs.HealthBarPrefab != Entity.Null &&
            em.Exists(refs.HealthBarPrefab) &&
            !em.HasComponent<UnitHealthBarPrefabReference>(unit))
        {
            em.AddComponentData(unit, new UnitHealthBarPrefabReference { Prefab = refs.HealthBarPrefab });
        }

        if (refs.DestroyedVisualPrefab != Entity.Null &&
            em.Exists(refs.DestroyedVisualPrefab) &&
            !em.HasComponent<VehicleDestroyedVisualPrefabReference>(unit))
        {
            em.AddComponentData(unit, new VehicleDestroyedVisualPrefabReference { Prefab = refs.DestroyedVisualPrefab });
        }

        AddBackfilledTag(em, unit);
    }

    private static bool HasAllUnitVisualPrefabReferences(EntityManager em, Entity entity)
    {
        return em.HasComponent<UnitSelectionMarkerPrefabReference>(entity) &&
               em.HasComponent<UnitHealthBarPrefabReference>(entity) &&
               em.HasComponent<VehicleDestroyedVisualPrefabReference>(entity);
    }

    private static UnitVisualPrefabReferenceSet CreateReferenceSet(EntityManager em, Entity prefab)
    {
        return new UnitVisualPrefabReferenceSet
        {
            SelectionMarkerPrefab = em.HasComponent<UnitSelectionMarkerPrefabReference>(prefab)
                ? em.GetComponentData<UnitSelectionMarkerPrefabReference>(prefab).Prefab
                : Entity.Null,
            HealthBarPrefab = em.HasComponent<UnitHealthBarPrefabReference>(prefab)
                ? em.GetComponentData<UnitHealthBarPrefabReference>(prefab).Prefab
                : Entity.Null,
            DestroyedVisualPrefab = em.HasComponent<VehicleDestroyedVisualPrefabReference>(prefab)
                ? em.GetComponentData<VehicleDestroyedVisualPrefabReference>(prefab).Prefab
                : Entity.Null
        };
    }

    private struct UnitVisualPrefabReferenceSet
    {
        public Entity SelectionMarkerPrefab;
        public Entity HealthBarPrefab;
        public Entity DestroyedVisualPrefab;

        public bool HasAny => SelectionMarkerPrefab != Entity.Null ||
                              HealthBarPrefab != Entity.Null ||
                              DestroyedVisualPrefab != Entity.Null;

        public bool HasSharedMarkerOrHealth => SelectionMarkerPrefab != Entity.Null ||
                                               HealthBarPrefab != Entity.Null;
    }

    private static UnitVisualPrefabReferenceSet MergeSharedDefaults(
        UnitVisualPrefabReferenceSet refs,
        UnitVisualPrefabReferenceSet defaultReferences)
    {
        if (refs.SelectionMarkerPrefab == Entity.Null)
            refs.SelectionMarkerPrefab = defaultReferences.SelectionMarkerPrefab;
        if (refs.HealthBarPrefab == Entity.Null)
            refs.HealthBarPrefab = defaultReferences.HealthBarPrefab;
        return refs;
    }

    private static void AddBackfilledTag(EntityManager em, Entity unit)
    {
        if (!em.HasComponent<UnitVisualPrefabReferencesBackfilledTag>(unit))
            em.AddComponent<UnitVisualPrefabReferencesBackfilledTag>(unit);
    }
}
