#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class VehicleVisualAdornmentsSystemTests
{
    [Test]
    public void VehicleSelectionMarkerSystemCreatesMovesAndRemovesMarkersPerSelectedVehicle()
    {
        using var world = new World(nameof(VehicleSelectionMarkerSystemCreatesMovesAndRemovesMarkersPerSelectedVehicle));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity firstVehicle = CreateVehicle(em, health: 100);
        Entity secondVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(firstVehicle, new VehicleSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(secondVehicle, new VehicleSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(firstVehicle);
        em.AddComponent<SelectedUnitTag>(secondVehicle);

        SystemHandle system = world.CreateSystem<VehicleSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<VehicleSelectionMarkerInstanceReference>(firstVehicle));
        Assert.IsTrue(em.HasComponent<VehicleSelectionMarkerInstanceReference>(secondVehicle));
        Entity firstMarker = em.GetComponentData<VehicleSelectionMarkerInstanceReference>(firstVehicle).Instance;
        Entity secondMarker = em.GetComponentData<VehicleSelectionMarkerInstanceReference>(secondVehicle).Instance;
        Assert.AreNotEqual(firstMarker, secondMarker, "Each selected vehicle needs its own marker instance.");
        Assert.AreEqual(firstVehicle, em.GetComponentData<Parent>(firstMarker).Value);
        Assert.AreEqual(secondVehicle, em.GetComponentData<Parent>(secondMarker).Value);
        Assert.AreEqual(3f, em.GetComponentData<LocalTransform>(firstMarker).Scale);

        em.RemoveComponent<SelectedUnitTag>(firstVehicle);
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<VehicleSelectionMarkerInstanceReference>(firstVehicle));
        Assert.IsFalse(em.Exists(firstMarker), "Deselecting one vehicle must remove only that vehicle marker.");
        Assert.IsTrue(em.HasComponent<VehicleSelectionMarkerInstanceReference>(secondVehicle));
        Assert.IsTrue(em.Exists(secondMarker));
    }

    [Test]
    public void VehicleHealthBarSystemCreatesAndRemovesRuntimeHealthBar()
    {
        using var world = new World(nameof(VehicleHealthBarSystemCreatesAndRemovesRuntimeHealthBar));
        EntityManager em = world.EntityManager;
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity vehicle = CreateVehicle(em, health: 60);
        em.AddComponentData(vehicle, new VehicleHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(vehicle, new RecentDamageHealthBarVisibility { TimeRemaining = 1f });

        SystemHandle system = world.CreateSystem<VehicleHealthBarSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<VehicleHealthBarInstanceReference>(vehicle));
        Entity healthBar = em.GetComponentData<VehicleHealthBarInstanceReference>(vehicle).Instance;
        Assert.AreEqual(vehicle, em.GetComponentData<Parent>(healthBar).Value);
        Assert.IsTrue(em.HasComponent<HealthBarFill>(healthBar));

        em.RemoveComponent<RecentDamageHealthBarVisibility>(vehicle);
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<VehicleHealthBarInstanceReference>(vehicle));
        Assert.IsFalse(em.Exists(healthBar));
    }

    [Test]
    public void VehicleDestroyedVisualSystemSpawnsDestroyedVisualAndCleansRuntimeAdornments()
    {
        using var world = new World(nameof(VehicleDestroyedVisualSystemSpawnsDestroyedVisualAndCleansRuntimeAdornments));
        EntityManager em = world.EntityManager;
        Entity destroyedVisualPrefab = CreateVisualPrefab(em);
        Entity marker = CreateVisualInstance(em);
        Entity healthBar = CreateVisualInstance(em);
        Entity aliveVisual = CreateVisualInstance(em);
        Entity vehicle = CreateVehicle(em, health: 0);
        em.AddComponentData(vehicle, new VehicleDestroyedVisualPrefabReference { Prefab = destroyedVisualPrefab });
        em.AddComponentData(vehicle, new VehicleDestroyedVisualSpawnRequest());
        em.AddComponentData(vehicle, new VehicleSelectionMarkerInstanceReference { Instance = marker });
        em.AddComponentData(vehicle, new VehicleHealthBarInstanceReference { Instance = healthBar });
        em.AddComponentData(vehicle, new UnitDetailedVisualReference { Root = aliveVisual });

        SystemHandle system = world.CreateSystem<VehicleDestroyedVisualSystem>();
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualSpawnRequest>(vehicle));
        Assert.IsFalse(em.HasComponent<VehicleSelectionMarkerInstanceReference>(vehicle));
        Assert.IsFalse(em.HasComponent<VehicleHealthBarInstanceReference>(vehicle));
        Assert.IsFalse(em.Exists(marker));
        Assert.IsFalse(em.Exists(healthBar));
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(aliveVisual).Scale);
        Assert.IsTrue(em.HasComponent<VehicleDestroyedVisualInstanceReference>(vehicle));
        Entity destroyedVisual = em.GetComponentData<VehicleDestroyedVisualInstanceReference>(vehicle).Instance;
        Assert.AreEqual(vehicle, em.GetComponentData<Parent>(destroyedVisual).Value);
    }

    private static Entity CreateVehicle(EntityManager em, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitMovementBehavior),
            typeof(UnitHealth),
            typeof(UnitFootprint),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(3, 2) });
        em.SetComponentData(entity, LocalTransform.Identity);
        return entity;
    }

    private static Entity CreateHealthBarPrefab(EntityManager em)
    {
        Entity entity = CreateVisualPrefab(em);
        em.AddComponentData(entity, new HealthBarFill { Value = 1f });
        return entity;
    }

    private static Entity CreateVisualPrefab(EntityManager em)
    {
        Entity entity = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
        em.SetComponentData(entity, LocalTransform.Identity);
        return entity;
    }

    private static Entity CreateVisualInstance(EntityManager em)
    {
        Entity entity = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(float3.zero, quaternion.identity, 1f));
        return entity;
    }
}
#endif
