using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct VehicleSelectionMarkerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleSelectionMarkerPrefabReference>();
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
                              em.HasComponent<SelectedUnitTag>(entity) &&
                              em.HasComponent<VehicleSelectionMarkerPrefabReference>(entity);
            bool hasReference = em.HasComponent<VehicleSelectionMarkerInstanceReference>(entity);
            bool hasInstance = hasReference &&
                               em.Exists(em.GetComponentData<VehicleSelectionMarkerInstanceReference>(entity).Instance);
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
            DestroyMarker(em, remove[i]);

        for (int i = 0; i < create.Length; i++)
            CreateMarker(em, create[i]);

        create.Dispose();
        remove.Dispose();
    }

    private static void CreateMarker(EntityManager em, Entity vehicle)
    {
        VehicleSelectionMarkerPrefabReference prefabRef = em.GetComponentData<VehicleSelectionMarkerPrefabReference>(vehicle);
        if (prefabRef.Prefab == Entity.Null || !em.Exists(prefabRef.Prefab))
            return;

        Entity marker = em.Instantiate(prefabRef.Prefab);
        if (!em.HasComponent<Parent>(marker))
            em.AddComponentData(marker, new Parent { Value = vehicle });
        else
            em.SetComponentData(marker, new Parent { Value = vehicle });

        if (em.HasComponent<LocalTransform>(marker))
        {
            LocalTransform transform = em.GetComponentData<LocalTransform>(marker);
            transform.Position = new float3(0f, transform.Position.y, 0f);
            transform.Rotation = quaternion.identity;
            transform.Scale = ResolveMarkerScale(em, vehicle);
            em.SetComponentData(marker, transform);
        }

        em.AddComponentData(vehicle, new VehicleSelectionMarkerInstanceReference { Instance = marker });
    }

    private static float ResolveMarkerScale(EntityManager em, Entity vehicle)
    {
        if (!em.HasComponent<UnitFootprint>(vehicle))
            return 1f;

        int2 size = em.GetComponentData<UnitFootprint>(vehicle).Size;
        return math.max(1f, math.max(size.x, size.y));
    }

    private static void DestroyMarker(EntityManager em, Entity vehicle)
    {
        VehicleSelectionMarkerInstanceReference instance = em.GetComponentData<VehicleSelectionMarkerInstanceReference>(vehicle);
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        em.RemoveComponent<VehicleSelectionMarkerInstanceReference>(vehicle);
    }
}
