using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateBefore(typeof(UnitHealthBarSystem))]
public partial struct VehicleHealthBarSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleHealthBarPrefabReference>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var create = new NativeList<Entity>(Allocator.Temp);
        var remove = new NativeList<Entity>(Allocator.Temp);

        foreach (var (movement, health, entity) in SystemAPI
                 .Query<RefRO<UnitMovementBehavior>, RefRO<UnitHealth>>()
                 .WithEntityAccess())
        {
            if (movement.ValueRO.UsesVehicleMotion == 0)
                continue;

            bool shouldShow = health.ValueRO.Current > 0 &&
                              em.HasComponent<RecentDamageHealthBarVisibility>(entity) &&
                              em.HasComponent<VehicleHealthBarPrefabReference>(entity);
            bool hasReference = em.HasComponent<VehicleHealthBarInstanceReference>(entity);
            bool hasInstance = hasReference &&
                               em.Exists(em.GetComponentData<VehicleHealthBarInstanceReference>(entity).Instance);
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

    private static void CreateHealthBar(EntityManager em, Entity vehicle)
    {
        VehicleHealthBarPrefabReference prefabRef = em.GetComponentData<VehicleHealthBarPrefabReference>(vehicle);
        if (prefabRef.Prefab == Entity.Null || !em.Exists(prefabRef.Prefab))
            return;

        Entity healthBar = em.Instantiate(prefabRef.Prefab);
        if (!em.HasComponent<Parent>(healthBar))
            em.AddComponentData(healthBar, new Parent { Value = vehicle });
        else
            em.SetComponentData(healthBar, new Parent { Value = vehicle });

        em.AddComponentData(vehicle, new VehicleHealthBarInstanceReference { Instance = healthBar });
    }

    private static void DestroyHealthBar(EntityManager em, Entity vehicle)
    {
        VehicleHealthBarInstanceReference instance = em.GetComponentData<VehicleHealthBarInstanceReference>(vehicle);
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        em.RemoveComponent<VehicleHealthBarInstanceReference>(vehicle);
    }
}
