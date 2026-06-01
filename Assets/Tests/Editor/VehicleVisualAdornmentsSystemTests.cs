#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class VehicleVisualAdornmentsSystemTests
{
    [Test]
    public void VehicleVisualPrefabReferenceBackfillCopiesMarkerReferenceFromSourcePrefab()
    {
        using var world = new World(nameof(VehicleVisualPrefabReferenceBackfillCopiesMarkerReferenceFromSourcePrefab));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity destroyedVisualPrefab = CreateVisualPrefab(em);
        Entity sourcePrefab = CreateVehiclePrefab(em, "Unit_Veh_Tank_USA");
        em.AddComponentData(sourcePrefab, new VehicleSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(sourcePrefab, new VehicleHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(sourcePrefab, new VehicleDestroyedVisualPrefabReference { Prefab = destroyedVisualPrefab });

        Entity liveVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(liveVehicle, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Tank_USA") });
        em.AddComponent<SelectedUnitTag>(liveVehicle);

        SystemHandle backfillSystem = world.CreateSystem<VehicleVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<VehicleSelectionMarkerPrefabReference>(liveVehicle));
        Assert.AreEqual(markerPrefab, em.GetComponentData<VehicleSelectionMarkerPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<VehicleHealthBarPrefabReference>(liveVehicle));
        Assert.AreEqual(healthBarPrefab, em.GetComponentData<VehicleHealthBarPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveVehicle));
        Assert.AreEqual(destroyedVisualPrefab, em.GetComponentData<VehicleDestroyedVisualPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<VehicleVisualPrefabReferencesBackfilledTag>(liveVehicle));

        SystemHandle markerSystem = world.CreateSystem<VehicleSelectionMarkerSystem>();
        markerSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<VehicleSelectionMarkerInstanceReference>(liveVehicle));
    }

    [Test]
    public void VehicleVisualPrefabReferenceBackfillUsesSharedMarkerWhenSourcePrefabIsStale()
    {
        using var world = new World(nameof(VehicleVisualPrefabReferenceBackfillUsesSharedMarkerWhenSourcePrefabIsStale));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity sharedVehiclePrefab = CreateVehiclePrefab(em, "Unit_Veh");
        Entity staleSourcePrefab = CreateVehiclePrefab(em, "Unit_Veh_Helicopter_Attack");
        em.AddComponentData(sharedVehiclePrefab, new VehicleSelectionMarkerPrefabReference { Prefab = markerPrefab });

        Entity liveVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(liveVehicle, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Helicopter_Attack") });
        em.AddComponent<SelectedUnitTag>(liveVehicle);

        SystemHandle backfillSystem = world.CreateSystem<VehicleVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<VehicleSelectionMarkerPrefabReference>(liveVehicle));
        Assert.AreEqual(markerPrefab, em.GetComponentData<VehicleSelectionMarkerPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<VehicleVisualPrefabReferencesBackfilledTag>(liveVehicle));

        SystemHandle markerSystem = world.CreateSystem<VehicleSelectionMarkerSystem>();
        markerSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<VehicleSelectionMarkerInstanceReference>(liveVehicle));
        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveVehicle), "Destroyed visuals must stay source-specific, not use the shared marker fallback.");
        Assert.IsTrue(em.Exists(staleSourcePrefab));
    }

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
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(firstMarker).Scale, 0.001f);
        Assert.IsTrue(em.HasComponent<SelectionMarkerTag>(firstMarker));
        Assert.AreEqual(4.05f, em.GetComponentData<SelectionMarkerVisualChild>(firstMarker).VisibleScale, 0.001f);

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

    private static Entity CreateVehiclePrefab(EntityManager em, string sourceKey)
    {
        Entity entity = em.CreateEntity(
            typeof(Prefab),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 1 });
        em.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceKey) });
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
