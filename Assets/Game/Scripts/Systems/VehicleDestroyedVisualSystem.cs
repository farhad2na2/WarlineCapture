using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(UnitDeathSystem))]
public partial struct VehicleDestroyedVisualSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VehicleDestroyedVisualSpawnRequest>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var requests = new NativeList<Entity>(Allocator.Temp);
        foreach (var (_, entity) in SystemAPI
                 .Query<RefRO<VehicleDestroyedVisualSpawnRequest>>()
                 .WithEntityAccess())
        {
            requests.Add(entity);
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        for (int i = 0; i < requests.Length; i++)
            ProcessRequest(em, ref ecb, requests[i]);

        ecb.Playback(em);
        ecb.Dispose();
        requests.Dispose();
    }

    private static void ProcessRequest(EntityManager em, ref EntityCommandBuffer ecb, Entity vehicle)
    {
        if (!em.Exists(vehicle))
            return;

        if (em.HasComponent<UnitSelectionMarkerInstanceReference>(vehicle))
        {
            UnitSelectionMarkerInstanceReference marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(vehicle);
            VehicleVisualEntityUtility.DestroyVisualTree(em, marker.Instance);
            em.RemoveComponent<UnitSelectionMarkerInstanceReference>(vehicle);
        }

        if (em.HasComponent<UnitHealthBarInstanceReference>(vehicle))
        {
            UnitHealthBarInstanceReference healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(vehicle);
            VehicleVisualEntityUtility.DestroyVisualTree(em, healthBar.Instance);
            em.RemoveComponent<UnitHealthBarInstanceReference>(vehicle);
        }

        HideAliveVisuals(em, vehicle);
        if (!em.HasComponent<VehicleDestroyedVisualInstanceReference>(vehicle))
            CreateDestroyedVisual(em, ref ecb, vehicle);

        if (em.HasComponent<VehicleDestroyedVisualSpawnRequest>(vehicle))
            em.RemoveComponent<VehicleDestroyedVisualSpawnRequest>(vehicle);
    }

    private static void HideAliveVisuals(EntityManager em, Entity vehicle)
    {
        HideOriginalLinkedVisuals(em, vehicle);

        if (em.HasComponent<UnitDetailedVisualReference>(vehicle))
            HideAliveVisualTree(em, em.GetComponentData<UnitDetailedVisualReference>(vehicle).Root);

        if (em.HasComponent<UnitModelInstanceReference>(vehicle))
            HideAliveVisualTree(em, em.GetComponentData<UnitModelInstanceReference>(vehicle).Instance);

        if (em.HasComponent<UnitMidLodInstanceReference>(vehicle))
            HideAliveVisualTree(em, em.GetComponentData<UnitMidLodInstanceReference>(vehicle).Instance);

        if (em.HasComponent<UnitLowLodInstanceReference>(vehicle))
            HideAliveVisualTree(em, em.GetComponentData<UnitLowLodInstanceReference>(vehicle).Instance);

        if (em.HasComponent<UnitDestroyedVisualReference>(vehicle))
        {
            UnitDestroyedVisualReference visualRef = em.GetComponentData<UnitDestroyedVisualReference>(vehicle);
            HideAliveVisualTree(em, visualRef.AliveVisual);
            HideAliveVisualTree(em, visualRef.DestroyedVisual);
        }

        if (em.HasComponent<UnitTurretReference>(vehicle))
            HideAliveVisualTree(em, em.GetComponentData<UnitTurretReference>(vehicle).Turret);
    }

    private static void HideOriginalLinkedVisuals(EntityManager em, Entity vehicle)
    {
        if (!em.HasBuffer<LinkedEntityGroup>(vehicle))
            return;

        DynamicBuffer<LinkedEntityGroup> linkedEntities = em.GetBuffer<LinkedEntityGroup>(vehicle);
        for (int i = 0; i < linkedEntities.Length; i++)
        {
            Entity linkedEntity = linkedEntities[i].Value;
            if (linkedEntity == vehicle)
                continue;

            HideAliveVisualTree(em, linkedEntity);
        }
    }

    private static void HideAliveVisualTree(EntityManager em, Entity root)
    {
        if (root == Entity.Null || !em.Exists(root))
            return;

        using NativeList<Entity> tree = new(Allocator.Temp);
        using NativeHashSet<Entity> visited = new(16, Allocator.Temp);
        CollectVisualTree(em, root, tree, visited);
        for (int i = 0; i < tree.Length; i++)
            HideAliveVisualEntity(em, tree[i]);
    }

    private static void CollectVisualTree(EntityManager em, Entity entity, NativeList<Entity> tree, NativeHashSet<Entity> visited)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return;
        if (!visited.Add(entity))
            return;

        tree.Add(entity);
        if (em.HasBuffer<LinkedEntityGroup>(entity))
        {
            DynamicBuffer<LinkedEntityGroup> linkedEntities = em.GetBuffer<LinkedEntityGroup>(entity);
            for (int i = 0; i < linkedEntities.Length; i++)
                CollectVisualTree(em, linkedEntities[i].Value, tree, visited);
        }

        if (!em.HasBuffer<Child>(entity))
            return;

        DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
            CollectVisualTree(em, children[i].Value, tree, visited);
    }

    private static void HideAliveVisualEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return;

        if (em.HasComponent<LocalTransform>(entity))
        {
            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            if (transform.Scale != 0f)
            {
                transform.Scale = 0f;
                em.SetComponentData(entity, transform);
            }
        }

        if (!em.HasComponent<Disabled>(entity))
            em.AddComponent<Disabled>(entity);

        if (!em.HasComponent<UnitRenderBudgetCulledTag>(entity))
            em.AddComponent<UnitRenderBudgetCulledTag>(entity);
    }

    private static void CreateDestroyedVisual(EntityManager em, ref EntityCommandBuffer ecb, Entity vehicle)
    {
        if (!em.HasComponent<VehicleDestroyedVisualPrefabReference>(vehicle))
            return;

        VehicleDestroyedVisualPrefabReference prefabRef = em.GetComponentData<VehicleDestroyedVisualPrefabReference>(vehicle);
        if (prefabRef.Prefab == Entity.Null || !em.Exists(prefabRef.Prefab))
            return;

        bool prefabHasParent = em.HasComponent<Parent>(prefabRef.Prefab);
        Entity visual = ecb.Instantiate(prefabRef.Prefab);
        if (prefabHasParent)
            ecb.SetComponent(visual, new Parent { Value = vehicle });
        else
            ecb.AddComponent(visual, new Parent { Value = vehicle });

        if (em.HasComponent<VehicleDestroyedVisualInstanceReference>(vehicle))
            ecb.SetComponent(vehicle, new VehicleDestroyedVisualInstanceReference { Instance = visual });
        else
            ecb.AddComponent(vehicle, new VehicleDestroyedVisualInstanceReference { Instance = visual });
    }
}
