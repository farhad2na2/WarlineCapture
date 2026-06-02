#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
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
        em.AddComponentData(sourcePrefab, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(sourcePrefab, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(sourcePrefab, new VehicleDestroyedVisualPrefabReference { Prefab = destroyedVisualPrefab });

        Entity liveVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(liveVehicle, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Tank_USA") });
        em.AddComponent<SelectedUnitTag>(liveVehicle);

        SystemHandle backfillSystem = world.CreateSystem<UnitVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerPrefabReference>(liveVehicle));
        Assert.AreEqual(markerPrefab, em.GetComponentData<UnitSelectionMarkerPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<UnitHealthBarPrefabReference>(liveVehicle));
        Assert.AreEqual(healthBarPrefab, em.GetComponentData<UnitHealthBarPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveVehicle));
        Assert.AreEqual(destroyedVisualPrefab, em.GetComponentData<VehicleDestroyedVisualPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<UnitVisualPrefabReferencesBackfilledTag>(liveVehicle));

        SystemHandle markerSystem = world.CreateSystem<UnitSelectionMarkerSystem>();
        markerSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(liveVehicle));
    }

    [Test]
    public void VehicleVisualPrefabReferenceBackfillUsesSharedMarkerWhenSourcePrefabIsStale()
    {
        using var world = new World(nameof(VehicleVisualPrefabReferenceBackfillUsesSharedMarkerWhenSourcePrefabIsStale));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity sharedVehiclePrefab = CreateVehiclePrefab(em, "Unit_Veh");
        Entity staleSourcePrefab = CreateVehiclePrefab(em, "Unit_Veh_Helicopter_Attack");
        em.AddComponentData(sharedVehiclePrefab, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });

        Entity liveVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(liveVehicle, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Helicopter_Attack") });
        em.AddComponent<SelectedUnitTag>(liveVehicle);

        SystemHandle backfillSystem = world.CreateSystem<UnitVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerPrefabReference>(liveVehicle));
        Assert.AreEqual(markerPrefab, em.GetComponentData<UnitSelectionMarkerPrefabReference>(liveVehicle).Prefab);
        Assert.IsTrue(em.HasComponent<UnitVisualPrefabReferencesBackfilledTag>(liveVehicle));

        SystemHandle markerSystem = world.CreateSystem<UnitSelectionMarkerSystem>();
        markerSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(liveVehicle));
        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveVehicle), "Destroyed visuals must stay source-specific, not use the shared marker fallback.");
        Assert.IsTrue(em.Exists(staleSourcePrefab));
    }

    [Test]
    public void UnitVisualPrefabReferenceBackfillCopiesMarkerAndHealthReferencesForCharacterUnit()
    {
        using var world = new World(nameof(UnitVisualPrefabReferenceBackfillCopiesMarkerAndHealthReferencesForCharacterUnit));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity sourcePrefab = CreateCharacterPrefab(em, "Unit_Rifleman");
        em.AddComponentData(sourcePrefab, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(sourcePrefab, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });

        Entity liveCharacter = CreateCharacter(em, health: 100);
        em.AddComponentData(liveCharacter, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Rifleman") });

        SystemHandle backfillSystem = world.CreateSystem<UnitVisualPrefabReferenceBackfillSystem>();
        backfillSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerPrefabReference>(liveCharacter));
        Assert.AreEqual(markerPrefab, em.GetComponentData<UnitSelectionMarkerPrefabReference>(liveCharacter).Prefab);
        Assert.IsTrue(em.HasComponent<UnitHealthBarPrefabReference>(liveCharacter));
        Assert.AreEqual(healthBarPrefab, em.GetComponentData<UnitHealthBarPrefabReference>(liveCharacter).Prefab);
        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualPrefabReference>(liveCharacter));
        Assert.IsTrue(em.HasComponent<UnitVisualPrefabReferencesBackfilledTag>(liveCharacter));
    }

    [Test]
    public void UnitSelectionMarkerSystemCreatesMovesAndRemovesMarkersPerSelectedVehicle()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemCreatesMovesAndRemovesMarkersPerSelectedVehicle));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity firstVehicle = CreateVehicle(em, health: 100);
        Entity secondVehicle = CreateVehicle(em, health: 100);
        em.AddComponentData(firstVehicle, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponentData(secondVehicle, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(firstVehicle);
        em.AddComponent<SelectedUnitTag>(secondVehicle);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(firstVehicle));
        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(secondVehicle));
        Entity firstMarker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(firstVehicle).Instance;
        Entity secondMarker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(secondVehicle).Instance;
        Assert.AreNotEqual(firstMarker, secondMarker, "Each selected vehicle needs its own marker instance.");
        Assert.AreEqual(firstVehicle, em.GetComponentData<Parent>(firstMarker).Value);
        Assert.AreEqual(secondVehicle, em.GetComponentData<Parent>(secondMarker).Value);
        Assert.AreEqual(1f, em.GetComponentData<LocalTransform>(firstMarker).Scale, 0.001f);
        Assert.IsTrue(em.HasComponent<SelectionMarkerTag>(firstMarker));
        Assert.AreEqual(4.05f, em.GetComponentData<SelectionMarkerVisualChild>(firstMarker).VisibleScale, 0.001f);

        em.RemoveComponent<SelectedUnitTag>(firstVehicle);
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitSelectionMarkerInstanceReference>(firstVehicle));
        Assert.IsFalse(em.Exists(firstMarker), "Deselecting one vehicle must remove only that vehicle marker.");
        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(secondVehicle));
        Assert.IsTrue(em.Exists(secondMarker));
    }

    [Test]
    public void UnitSelectionMarkerSystemCreatesMarkerForSelectedCharacterUnit()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemCreatesMarkerForSelectedCharacterUnit));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(character);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(character).Instance;
        Assert.AreEqual(character, em.GetComponentData<Parent>(marker).Value);
        Assert.AreEqual(1.35f, em.GetComponentData<SelectionMarkerVisualChild>(marker).VisibleScale, 0.001f);
    }

    [Test]
    public void UnitSelectionMarkerSystemHidesMarkersForTransportedCharactersButKeepsCulledSelectedCharactersVisible()
    {
        using var world = new World(nameof(UnitSelectionMarkerSystemHidesMarkersForTransportedCharactersButKeepsCulledSelectedCharactersVisible));
        EntityManager em = world.EntityManager;
        Entity markerPrefab = CreateVisualPrefab(em);
        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new UnitSelectionMarkerPrefabReference { Prefab = markerPrefab });
        em.AddComponent<SelectedUnitTag>(character);

        SystemHandle system = world.CreateSystem<UnitSelectionMarkerSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Entity marker = em.GetComponentData<UnitSelectionMarkerInstanceReference>(character).Instance;

        em.AddComponentData(character, new UnitTransportPassenger { Transport = Entity.Null });
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
        Assert.IsFalse(em.Exists(marker));

        em.RemoveComponent<UnitTransportPassenger>(character);
        em.AddComponent<UnitRenderBudgetCulledUnitTag>(character);
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitSelectionMarkerInstanceReference>(character));
    }

    [Test]
    public void UnitRuntimeHealthBarSystemCreatesAndRemovesRuntimeHealthBar()
    {
        using var world = new World(nameof(UnitRuntimeHealthBarSystemCreatesAndRemovesRuntimeHealthBar));
        EntityManager em = world.EntityManager;
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity vehicle = CreateVehicle(em, health: 60);
        em.AddComponentData(vehicle, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(vehicle, new RecentDamageHealthBarVisibility { TimeRemaining = 1f });

        SystemHandle system = world.CreateSystem<UnitRuntimeHealthBarSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(vehicle));
        Entity healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(vehicle).Instance;
        Assert.AreEqual(vehicle, em.GetComponentData<Parent>(healthBar).Value);
        Assert.IsTrue(em.HasComponent<HealthBarFill>(healthBar));

        em.RemoveComponent<RecentDamageHealthBarVisibility>(vehicle);
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitHealthBarInstanceReference>(vehicle));
        Assert.IsFalse(em.Exists(healthBar));
    }

    [Test]
    public void UnitRuntimeHealthBarSystemCreatesHealthBarForDamagedCharacterUnit()
    {
        using var world = new World(nameof(UnitRuntimeHealthBarSystemCreatesHealthBarForDamagedCharacterUnit));
        EntityManager em = world.EntityManager;
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity character = CreateCharacter(em, health: 60);
        em.AddComponentData(character, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(character, new RecentDamageHealthBarVisibility { TimeRemaining = 1f });

        SystemHandle system = world.CreateSystem<UnitRuntimeHealthBarSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(character));
        Entity healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(character).Instance;
        Assert.AreEqual(character, em.GetComponentData<Parent>(healthBar).Value);
    }

    [Test]
    public void UnitRuntimeHealthBarSystemHidesBarsForTransportedOrImpostorOnlyCharacters()
    {
        using var world = new World(nameof(UnitRuntimeHealthBarSystemHidesBarsForTransportedOrImpostorOnlyCharacters));
        EntityManager em = world.EntityManager;
        Entity healthBarPrefab = CreateHealthBarPrefab(em);
        Entity character = CreateCharacter(em, health: 60);
        em.AddComponentData(character, new UnitHealthBarPrefabReference { Prefab = healthBarPrefab });
        em.AddComponentData(character, new RecentDamageHealthBarVisibility { TimeRemaining = 1f });

        SystemHandle system = world.CreateSystem<UnitRuntimeHealthBarSystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitHealthBarInstanceReference>(character));
        Entity healthBar = em.GetComponentData<UnitHealthBarInstanceReference>(character).Instance;

        em.AddComponentData(character, new UnitTransportPassenger { Transport = Entity.Null });
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitHealthBarInstanceReference>(character));
        Assert.IsFalse(em.Exists(healthBar));

        em.RemoveComponent<UnitTransportPassenger>(character);
        em.AddComponent<UnitRenderBudgetCulledUnitTag>(character);
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitHealthBarInstanceReference>(character));
    }

    [Test]
    public void FactionVisualSystemAppliesFactionTintToCharacterModelTargets()
    {
        using var world = new World(nameof(FactionVisualSystemAppliesFactionTintToCharacterModelTargets));
        EntityManager em = world.EntityManager;
        Entity config = em.CreateEntity(typeof(FactionVisualConfig));
        em.SetComponentData(config, new FactionVisualConfig
        {
            NeutralColor = new float4(0.5f, 0.5f, 0.5f, 1f),
            PlayerColor = new float4(0.1f, 0.7f, 1f, 1f),
            EnemyColor = new float4(1f, 0.2f, 0.1f, 1f)
        });

        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new Faction { Id = 1 });
        Entity renderer = em.CreateEntity(
            typeof(FactionTintTarget),
            typeof(FactionTintColor),
            typeof(FactionSnivelerBaseColor),
            typeof(Parent));
        em.SetComponentData(renderer, new FactionTintColor { Value = new float4(1f, 1f, 1f, 1f) });
        em.SetComponentData(renderer, new FactionSnivelerBaseColor { Value = new float4(1f, 1f, 1f, 1f) });
        em.SetComponentData(renderer, new Parent { Value = character });

        SystemHandle system = world.CreateSystem<FactionVisualSystem>();
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);

        em.SetComponentData(character, new Faction { Id = 2 });
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(1f, 0.2f, 0.1f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(1f, 0.2f, 0.1f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);

        em.SetComponentData(character, new Faction { Id = 0 });
        system.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(0.5f, 0.5f, 0.5f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(0.5f, 0.5f, 0.5f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);
    }

    [Test]
    public void UnitFactionTintTargetBackfillFindsDeepCharacterRenderHierarchy()
    {
        using var world = new World(nameof(UnitFactionTintTargetBackfillFindsDeepCharacterRenderHierarchy));
        EntityManager em = world.EntityManager;
        Entity config = em.CreateEntity(typeof(FactionVisualConfig));
        em.SetComponentData(config, new FactionVisualConfig
        {
            NeutralColor = new float4(0.5f, 0.5f, 0.5f, 1f),
            PlayerColor = new float4(0.1f, 0.7f, 1f, 1f),
            EnemyColor = new float4(1f, 0.2f, 0.1f, 1f)
        });

        Entity character = CreateCharacter(em, health: 100);
        em.AddComponentData(character, new Faction { Id = 1 });
        em.AddComponentData(character, new UnitGrid());
        em.AddComponentData(character, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Chr_Soldier_Male_02_Alt_04") });

        Entity parent = character;
        for (int i = 0; i < 32; i++)
        {
            Entity bone = em.CreateEntity(typeof(Parent));
            em.SetComponentData(bone, new Parent { Value = parent });
            parent = bone;
        }

        Entity renderer = em.CreateEntity(typeof(Parent), typeof(MaterialMeshInfo));
        em.SetComponentData(renderer, new Parent { Value = parent });

        SystemHandle backfill = world.CreateSystem<UnitFactionTintTargetBackfillSystem>();
        backfill.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<FactionTintTarget>(renderer));
        Assert.IsTrue(em.HasComponent<FactionTintColor>(renderer));
        Assert.IsTrue(em.HasComponent<FactionSnivelerBaseColor>(renderer));

        SystemHandle factionVisual = world.CreateSystem<FactionVisualSystem>();
        factionVisual.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();

        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionTintColor>(renderer).Value);
        Assert.AreEqual(new float4(0.1f, 0.7f, 1f, 1f), em.GetComponentData<FactionSnivelerBaseColor>(renderer).Value);
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
        em.AddComponentData(vehicle, new UnitSelectionMarkerInstanceReference { Instance = marker });
        em.AddComponentData(vehicle, new UnitHealthBarInstanceReference { Instance = healthBar });
        em.AddComponentData(vehicle, new UnitDetailedVisualReference { Root = aliveVisual });

        SystemHandle system = world.CreateSystem<VehicleDestroyedVisualSystem>();
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<VehicleDestroyedVisualSpawnRequest>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitSelectionMarkerInstanceReference>(vehicle));
        Assert.IsFalse(em.HasComponent<UnitHealthBarInstanceReference>(vehicle));
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

    private static Entity CreateCharacter(EntityManager em, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitMovementBehavior),
            typeof(UnitHealth),
            typeof(UnitFootprint),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 0 });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = 100 });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
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

    private static Entity CreateCharacterPrefab(EntityManager em, string sourceKey)
    {
        Entity entity = em.CreateEntity(
            typeof(Prefab),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitMovementBehavior { UsesVehicleMotion = 0 });
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
