using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct VehicleSelectionMarkerSystem : ISystem
{
    private const float MarkerGroundLift = 0.04f;
    private const float MarkerFootprintScaleMultiplier = 1.35f;
    private const float MarkerMinimumVehicleScale = 2.5f;

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
        em.SetName(marker, "VehicleSelectionMarker");
        if (!em.HasComponent<Parent>(marker))
            em.AddComponentData(marker, new Parent { Value = vehicle });
        else
            em.SetComponentData(marker, new Parent { Value = vehicle });

        if (em.HasComponent<LocalTransform>(marker))
        {
            LocalTransform transform = em.GetComponentData<LocalTransform>(marker);
            transform.Position = new float3(0f, MarkerGroundLift, 0f);
            transform.Rotation = quaternion.identity;
            transform.Scale = 1f;
            em.SetComponentData(marker, transform);
        }

        EnsureSelectionMarkerComponents(em, marker, ResolveMarkerScale(em, vehicle));
        em.AddComponentData(vehicle, new VehicleSelectionMarkerInstanceReference { Instance = marker });
    }

    private static void EnsureSelectionMarkerComponents(EntityManager em, Entity marker, float visibleScale)
    {
        if (!em.HasComponent<SelectionMarkerTag>(marker))
            em.AddComponent<SelectionMarkerTag>(marker);

        if (em.HasComponent<SelectionMarkerVisualChild>(marker))
        {
            SelectionMarkerVisualChild visualChild = em.GetComponentData<SelectionMarkerVisualChild>(marker);
            visualChild.VisibleScale = visibleScale;
            em.SetComponentData(marker, visualChild);
            return;
        }

        Entity visual = ResolveRenderableLinkedChild(em, marker);
        if (visual == Entity.Null)
            visual = marker;

        em.AddComponentData(marker, new SelectionMarkerVisualChild
        {
            Value = visual,
            VisibleScale = visibleScale
        });
    }

    private static Entity ResolveRenderableLinkedChild(EntityManager em, Entity marker)
    {
        if (!em.HasBuffer<LinkedEntityGroup>(marker))
            return Entity.Null;

        DynamicBuffer<LinkedEntityGroup> linked = em.GetBuffer<LinkedEntityGroup>(marker);
        for (int i = 0; i < linked.Length; i++)
        {
            Entity entity = linked[i].Value;
            if (entity == marker || !em.Exists(entity))
                continue;

            if (em.HasComponent<MaterialMeshInfo>(entity))
                return entity;
        }

        for (int i = 0; i < linked.Length; i++)
        {
            Entity entity = linked[i].Value;
            if (entity != marker && em.Exists(entity) && em.HasComponent<LocalTransform>(entity))
                return entity;
        }

        return Entity.Null;
    }

    private static float ResolveMarkerScale(EntityManager em, Entity vehicle)
    {
        if (!em.HasComponent<UnitFootprint>(vehicle))
            return MarkerMinimumVehicleScale;

        int2 size = em.GetComponentData<UnitFootprint>(vehicle).Size;
        return math.max(MarkerMinimumVehicleScale, math.max(size.x, size.y) * MarkerFootprintScaleMultiplier);
    }

    private static void DestroyMarker(EntityManager em, Entity vehicle)
    {
        VehicleSelectionMarkerInstanceReference instance = em.GetComponentData<VehicleSelectionMarkerInstanceReference>(vehicle);
        VehicleVisualEntityUtility.DestroyVisualTree(em, instance.Instance);
        em.RemoveComponent<VehicleSelectionMarkerInstanceReference>(vehicle);
    }
}
