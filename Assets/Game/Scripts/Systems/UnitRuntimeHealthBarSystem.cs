using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateBefore(typeof(UnitHealthBarSystem))]
public partial struct UnitRuntimeHealthBarSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitHealthBarPrefabReference>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var create = new NativeList<Entity>(Allocator.Temp);
        var remove = new NativeList<Entity>(Allocator.Temp);

        foreach (var (health, entity) in SystemAPI
                 .Query<RefRO<UnitHealth>>()
                 .WithEntityAccess())
        {
            bool shouldShow = health.ValueRO.Current > 0 &&
                              em.HasComponent<RecentDamageHealthBarVisibility>(entity) &&
                              em.HasComponent<UnitHealthBarPrefabReference>(entity) &&
                              !em.HasComponent<UnitTransportPassenger>(entity) &&
                              !em.HasComponent<UnitRenderBudgetCulledUnitTag>(entity);
            bool hasReference = em.HasComponent<UnitHealthBarInstanceReference>(entity);
            bool hasInstance = hasReference &&
                               em.Exists(em.GetComponentData<UnitHealthBarInstanceReference>(entity).Instance);
            if (hasReference && !hasInstance)
            {
                remove.Add(entity);
                if (shouldShow)
                    create.Add(entity);
                continue;
            }

            if (shouldShow && !hasInstance)
                create.Add(entity);
            else if (!shouldShow && hasReference)
                remove.Add(entity);
        }

        for (int i = 0; i < remove.Length; i++)
            DestroyHealthBar(em, remove[i]);

        for (int i = 0; i < create.Length; i++)
            CreateHealthBar(em, create[i]);

        create.Dispose();
        remove.Dispose();
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
