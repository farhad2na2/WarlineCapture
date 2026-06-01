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

        for (int i = 0; i < requests.Length; i++)
            ProcessRequest(em, requests[i]);

        requests.Dispose();
    }

    private static void ProcessRequest(EntityManager em, Entity vehicle)
    {
        if (!em.Exists(vehicle))
            return;

        if (em.HasComponent<VehicleSelectionMarkerInstanceReference>(vehicle))
        {
            VehicleSelectionMarkerInstanceReference marker = em.GetComponentData<VehicleSelectionMarkerInstanceReference>(vehicle);
            VehicleVisualEntityUtility.DestroyVisualTree(em, marker.Instance);
            em.RemoveComponent<VehicleSelectionMarkerInstanceReference>(vehicle);
        }

        if (em.HasComponent<VehicleHealthBarInstanceReference>(vehicle))
        {
            VehicleHealthBarInstanceReference healthBar = em.GetComponentData<VehicleHealthBarInstanceReference>(vehicle);
            VehicleVisualEntityUtility.DestroyVisualTree(em, healthBar.Instance);
            em.RemoveComponent<VehicleHealthBarInstanceReference>(vehicle);
        }

        HideAliveVisuals(em, vehicle);
        if (!em.HasComponent<VehicleDestroyedVisualInstanceReference>(vehicle))
            CreateDestroyedVisual(em, vehicle);

        if (em.HasComponent<VehicleDestroyedVisualSpawnRequest>(vehicle))
            em.RemoveComponent<VehicleDestroyedVisualSpawnRequest>(vehicle);
    }

    private static void HideAliveVisuals(EntityManager em, Entity vehicle)
    {
        if (em.HasComponent<UnitDetailedVisualReference>(vehicle))
            UnitDestroyedVisualSystem.SetChildVisible(em, em.GetComponentData<UnitDetailedVisualReference>(vehicle).Root, false);

        if (em.HasComponent<UnitTurretReference>(vehicle))
            UnitDestroyedVisualSystem.SetChildVisible(em, em.GetComponentData<UnitTurretReference>(vehicle).Turret, false);
    }

    private static void CreateDestroyedVisual(EntityManager em, Entity vehicle)
    {
        if (!em.HasComponent<VehicleDestroyedVisualPrefabReference>(vehicle))
            return;

        VehicleDestroyedVisualPrefabReference prefabRef = em.GetComponentData<VehicleDestroyedVisualPrefabReference>(vehicle);
        if (prefabRef.Prefab == Entity.Null || !em.Exists(prefabRef.Prefab))
            return;

        Entity visual = em.Instantiate(prefabRef.Prefab);
        if (!em.HasComponent<Parent>(visual))
            em.AddComponentData(visual, new Parent { Value = vehicle });
        else
            em.SetComponentData(visual, new Parent { Value = vehicle });

        em.AddComponentData(vehicle, new VehicleDestroyedVisualInstanceReference { Instance = visual });
    }
}
