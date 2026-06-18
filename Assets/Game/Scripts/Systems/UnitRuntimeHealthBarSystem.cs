using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateBefore(typeof(UnitHealthBarSystem))]
public partial struct UnitRuntimeHealthBarSystem : ISystem
{
    private EntityStorageInfoLookup _entityStorageInfoLookup;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitHealthBarPrefabReference>();
        _entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var create = new NativeList<Entity>(Allocator.TempJob);
        var removeReference = new NativeList<Entity>(Allocator.TempJob);
        var destroy = new NativeList<Entity>(Allocator.TempJob);
        _entityStorageInfoLookup.Update(ref state);
        new CollectHealthBarChangesJob
        {
            RecentDamageLookup = SystemAPI.GetComponentLookup<RecentDamageHealthBarVisibility>(true),
            PrefabReferenceLookup = SystemAPI.GetComponentLookup<UnitHealthBarPrefabReference>(true),
            PassengerLookup = SystemAPI.GetComponentLookup<UnitTransportPassenger>(true),
            CulledLookup = SystemAPI.GetComponentLookup<UnitRenderBudgetCulledUnitTag>(true),
            InstanceReferenceLookup = SystemAPI.GetComponentLookup<UnitHealthBarInstanceReference>(true),
            EntityStorageInfoLookup = _entityStorageInfoLookup,
            Create = create,
            RemoveReference = removeReference,
            Destroy = destroy
        }.Run();

        for (int i = 0; i < removeReference.Length; i++)
            RemoveHealthBarReference(em, removeReference[i]);

        for (int i = 0; i < destroy.Length; i++)
            DestroyHealthBar(em, destroy[i]);

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        for (int i = 0; i < create.Length; i++)
            CreateHealthBar(em, ref ecb, create[i]);

        ecb.Playback(em);
        ecb.Dispose();
        create.Dispose();
        removeReference.Dispose();
        destroy.Dispose();
    }

    [BurstCompile]
    [WithChangeFilter(typeof(UnitHealth))]
    private partial struct CollectHealthBarChangesJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<RecentDamageHealthBarVisibility> RecentDamageLookup;
        [ReadOnly] public ComponentLookup<UnitHealthBarPrefabReference> PrefabReferenceLookup;
        [ReadOnly] public ComponentLookup<UnitTransportPassenger> PassengerLookup;
        [ReadOnly] public ComponentLookup<UnitRenderBudgetCulledUnitTag> CulledLookup;
        [ReadOnly] public ComponentLookup<UnitHealthBarInstanceReference> InstanceReferenceLookup;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        public NativeList<Entity> Create;
        public NativeList<Entity> RemoveReference;
        public NativeList<Entity> Destroy;

        private void Execute(Entity entity, in UnitHealth health)
        {
            bool canOwnHealthBar = health.Current > 0 &&
                                   PrefabReferenceLookup.HasComponent(entity);
            bool shouldShow = canOwnHealthBar &&
                              RecentDamageLookup.HasComponent(entity) &&
                              !PassengerLookup.HasComponent(entity) &&
                              !CulledLookup.HasComponent(entity);
            bool hasReference = InstanceReferenceLookup.HasComponent(entity);
            bool hasInstance = hasReference &&
                               EntityStorageInfoLookup.Exists(InstanceReferenceLookup[entity].Instance);
            if (hasReference && !hasInstance)
            {
                RemoveReference.Add(entity);
                if (shouldShow)
                    Create.Add(entity);
                return;
            }

            if (!canOwnHealthBar && hasReference)
            {
                Destroy.Add(entity);
                return;
            }

            if (shouldShow && !hasInstance)
                Create.Add(entity);
        }
    }

    private static void CreateHealthBar(EntityManager em, ref EntityCommandBuffer ecb, Entity unit)
    {
        UnitHealthBarPrefabReference prefabRef = em.GetComponentData<UnitHealthBarPrefabReference>(unit);
        if (prefabRef.Prefab == Entity.Null || !em.Exists(prefabRef.Prefab))
            return;

        bool prefabHasParent = em.HasComponent<Parent>(prefabRef.Prefab);
        Entity healthBar = ecb.Instantiate(prefabRef.Prefab);
        if (prefabHasParent)
            ecb.SetComponent(healthBar, new Parent { Value = unit });
        else
            ecb.AddComponent(healthBar, new Parent { Value = unit });

        if (em.HasComponent<UnitHealthBarInstanceReference>(unit))
            ecb.SetComponent(unit, new UnitHealthBarInstanceReference { Instance = healthBar });
        else
            ecb.AddComponent(unit, new UnitHealthBarInstanceReference { Instance = healthBar });
    }

    private static void DestroyHealthBar(EntityManager em, Entity unit)
    {
        UnitHealthBarInstanceReference instance = em.GetComponentData<UnitHealthBarInstanceReference>(unit);
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        RemoveHealthBarReference(em, unit);
    }

    private static void RemoveHealthBarReference(EntityManager em, Entity unit)
    {
        if (!em.HasComponent<UnitHealthBarInstanceReference>(unit))
            return;

        em.RemoveComponent<UnitHealthBarInstanceReference>(unit);
    }
}
