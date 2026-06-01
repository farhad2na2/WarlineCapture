using Unity.Collections;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct VehicleVisualPrefabReferenceBackfillSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        using var referencesBySourceKey = new NativeHashMap<FixedString64Bytes, VehicleVisualPrefabReferenceSet>(64, Allocator.Temp);
        VehicleVisualPrefabReferenceSet defaultReferences = BuildReferenceLookup(em, referencesBySourceKey);
        if (referencesBySourceKey.Count == 0 && !defaultReferences.HasSharedMarkerOrHealth)
            return;

        var vehiclesToPatch = new NativeList<Entity>(Allocator.Temp);
        foreach (var (movement, sourceKey, entity) in SystemAPI
                 .Query<RefRO<UnitMovementBehavior>, RefRO<UnitSourcePrefabKey>>()
                 .WithNone<VehicleVisualPrefabReferencesBackfilledTag>()
                 .WithEntityAccess())
        {
            if (movement.ValueRO.UsesVehicleMotion == 0 ||
                sourceKey.ValueRO.Value.Length == 0)
            {
                continue;
            }

            vehiclesToPatch.Add(entity);
        }

        for (int i = 0; i < vehiclesToPatch.Length; i++)
            PatchVehicle(em, vehiclesToPatch[i], referencesBySourceKey, defaultReferences);

        vehiclesToPatch.Dispose();
    }

    private static VehicleVisualPrefabReferenceSet BuildReferenceLookup(
        EntityManager em,
        NativeHashMap<FixedString64Bytes, VehicleVisualPrefabReferenceSet> referencesBySourceKey)
    {
        VehicleVisualPrefabReferenceSet defaultReferences = default;
        using EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Prefab>(),
            ComponentType.ReadOnly<UnitMovementBehavior>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>());
        using NativeArray<Entity> prefabs = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < prefabs.Length; i++)
        {
            Entity prefab = prefabs[i];
            UnitMovementBehavior movement = em.GetComponentData<UnitMovementBehavior>(prefab);
            if (movement.UsesVehicleMotion == 0)
                continue;

            FixedString64Bytes sourceKey = em.GetComponentData<UnitSourcePrefabKey>(prefab).Value;
            if (sourceKey.Length == 0)
                continue;

            VehicleVisualPrefabReferenceSet refs = CreateReferenceSet(em, prefab);
            if (!refs.HasAny)
                continue;

            if (defaultReferences.SelectionMarkerPrefab == Entity.Null && refs.SelectionMarkerPrefab != Entity.Null)
                defaultReferences.SelectionMarkerPrefab = refs.SelectionMarkerPrefab;
            if (defaultReferences.HealthBarPrefab == Entity.Null && refs.HealthBarPrefab != Entity.Null)
                defaultReferences.HealthBarPrefab = refs.HealthBarPrefab;

            referencesBySourceKey[sourceKey] = refs;
        }

        return defaultReferences;
    }

    private static void PatchVehicle(
        EntityManager em,
        Entity vehicle,
        NativeHashMap<FixedString64Bytes, VehicleVisualPrefabReferenceSet> referencesBySourceKey,
        VehicleVisualPrefabReferenceSet defaultReferences)
    {
        if (!em.Exists(vehicle) || !em.HasComponent<UnitSourcePrefabKey>(vehicle))
            return;

        if (HasAllVehicleVisualPrefabReferences(em, vehicle))
        {
            AddBackfilledTag(em, vehicle);
            return;
        }

        FixedString64Bytes sourceKey = em.GetComponentData<UnitSourcePrefabKey>(vehicle).Value;
        VehicleVisualPrefabReferenceSet refs = default;
        if (sourceKey.Length > 0)
            referencesBySourceKey.TryGetValue(sourceKey, out refs);

        refs = MergeSharedDefaults(refs, defaultReferences);
        if (!refs.HasAny)
        {
            return;
        }

        if (refs.SelectionMarkerPrefab != Entity.Null &&
            em.Exists(refs.SelectionMarkerPrefab) &&
            !em.HasComponent<VehicleSelectionMarkerPrefabReference>(vehicle))
        {
            em.AddComponentData(vehicle, new VehicleSelectionMarkerPrefabReference { Prefab = refs.SelectionMarkerPrefab });
        }

        if (refs.HealthBarPrefab != Entity.Null &&
            em.Exists(refs.HealthBarPrefab) &&
            !em.HasComponent<VehicleHealthBarPrefabReference>(vehicle))
        {
            em.AddComponentData(vehicle, new VehicleHealthBarPrefabReference { Prefab = refs.HealthBarPrefab });
        }

        if (refs.DestroyedVisualPrefab != Entity.Null &&
            em.Exists(refs.DestroyedVisualPrefab) &&
            !em.HasComponent<VehicleDestroyedVisualPrefabReference>(vehicle))
        {
            em.AddComponentData(vehicle, new VehicleDestroyedVisualPrefabReference { Prefab = refs.DestroyedVisualPrefab });
        }

        AddBackfilledTag(em, vehicle);
    }

    private static bool HasAllVehicleVisualPrefabReferences(EntityManager em, Entity entity)
    {
        return em.HasComponent<VehicleSelectionMarkerPrefabReference>(entity) &&
               em.HasComponent<VehicleHealthBarPrefabReference>(entity) &&
               em.HasComponent<VehicleDestroyedVisualPrefabReference>(entity);
    }

    private static VehicleVisualPrefabReferenceSet CreateReferenceSet(EntityManager em, Entity prefab)
    {
        return new VehicleVisualPrefabReferenceSet
        {
            SelectionMarkerPrefab = em.HasComponent<VehicleSelectionMarkerPrefabReference>(prefab)
                ? em.GetComponentData<VehicleSelectionMarkerPrefabReference>(prefab).Prefab
                : Entity.Null,
            HealthBarPrefab = em.HasComponent<VehicleHealthBarPrefabReference>(prefab)
                ? em.GetComponentData<VehicleHealthBarPrefabReference>(prefab).Prefab
                : Entity.Null,
            DestroyedVisualPrefab = em.HasComponent<VehicleDestroyedVisualPrefabReference>(prefab)
                ? em.GetComponentData<VehicleDestroyedVisualPrefabReference>(prefab).Prefab
                : Entity.Null
        };
    }

    private struct VehicleVisualPrefabReferenceSet
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

    private static VehicleVisualPrefabReferenceSet MergeSharedDefaults(
        VehicleVisualPrefabReferenceSet refs,
        VehicleVisualPrefabReferenceSet defaultReferences)
    {
        if (refs.SelectionMarkerPrefab == Entity.Null)
            refs.SelectionMarkerPrefab = defaultReferences.SelectionMarkerPrefab;
        if (refs.HealthBarPrefab == Entity.Null)
            refs.HealthBarPrefab = defaultReferences.HealthBarPrefab;
        return refs;
    }

    private static void AddBackfilledTag(EntityManager em, Entity vehicle)
    {
        if (!em.HasComponent<VehicleVisualPrefabReferencesBackfilledTag>(vehicle))
            em.AddComponent<VehicleVisualPrefabReferencesBackfilledTag>(vehicle);
    }
}
