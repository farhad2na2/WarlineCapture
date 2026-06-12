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
        var remove = new NativeList<Entity>(Allocator.TempJob);
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
            Remove = remove
        }.Run();

        for (int i = 0; i < remove.Length; i++)
            DestroyHealthBar(em, remove[i]);

        for (int i = 0; i < create.Length; i++)
            CreateHealthBar(em, create[i]);

        create.Dispose();
        remove.Dispose();
    }

    [BurstCompile]
    private partial struct CollectHealthBarChangesJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<RecentDamageHealthBarVisibility> RecentDamageLookup;
        [ReadOnly] public ComponentLookup<UnitHealthBarPrefabReference> PrefabReferenceLookup;
        [ReadOnly] public ComponentLookup<UnitTransportPassenger> PassengerLookup;
        [ReadOnly] public ComponentLookup<UnitRenderBudgetCulledUnitTag> CulledLookup;
        [ReadOnly] public ComponentLookup<UnitHealthBarInstanceReference> InstanceReferenceLookup;
        [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
        public NativeList<Entity> Create;
        public NativeList<Entity> Remove;

        private void Execute(Entity entity, in UnitHealth health)
        {
            bool shouldShow = health.Current > 0 &&
                              RecentDamageLookup.HasComponent(entity) &&
                              PrefabReferenceLookup.HasComponent(entity) &&
                              !PassengerLookup.HasComponent(entity) &&
                              !CulledLookup.HasComponent(entity);
            bool hasReference = InstanceReferenceLookup.HasComponent(entity);
            bool hasInstance = hasReference &&
                               EntityStorageInfoLookup.Exists(InstanceReferenceLookup[entity].Instance);
            if (hasReference && !hasInstance)
            {
                Remove.Add(entity);
                if (shouldShow)
                    Create.Add(entity);
                return;
            }

            if (shouldShow && !hasInstance)
                Create.Add(entity);
            else if (!shouldShow && hasReference)
                Remove.Add(entity);
        }
    }

    private static void CreateHealthBar(EntityManager em, Entity unit)
    {
        UnitHealthBarPrefabReference prefabRef = em.GetComponentData<UnitHealthBarPrefabReference>(unit);
        if (prefabRef.Prefab == Entity.Null || !em.Exists(prefabRef.Prefab))
            return;

        Entity healthBar = em.Instantiate(prefabRef.Prefab);
        if (!em.HasComponent<Parent>(healthBar))
            em.AddComponentData(healthBar, new Parent { Value = unit });
        else
            em.SetComponentData(healthBar, new Parent { Value = unit });

        em.AddComponentData(unit, new UnitHealthBarInstanceReference { Instance = healthBar });
    }

    private static void DestroyHealthBar(EntityManager em, Entity unit)
    {
        UnitHealthBarInstanceReference instance = em.GetComponentData<UnitHealthBarInstanceReference>(unit);
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        em.RemoveComponent<UnitHealthBarInstanceReference>(unit);
    }
}
