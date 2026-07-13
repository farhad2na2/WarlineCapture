using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using UnityEngine;

public sealed class MaterialFabricationSystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new MaterialFabricationSystemTests();
            tests.ApplyTick_ConsumesOilAndGrantsMaterialsExactlyOnce();
            tests.ApplyTick_IsDeterministicAcrossRenderFramePartitions();
            tests.ApplyTick_StallsForEmptyAndPartialOilInput();
            tests.ApplyTick_StallsAtMaterialsCapacityAndResumes();
            tests.ApplyTick_DisableAndBuildingStatePreserveProgress();
            tests.ApplyTick_RejectsOwnershipMismatch();
            tests.ApplyTick_ConsumesOnlyUnreservedOil();
            tests.ApplyTick_ProcessesLargeDeltaArithmeticallyWithoutFrameDrift();
            tests.ApplyTick_IncrementsOnlyChangedVersions();
            tests.ApplyTick_DoesNotAllocateManagedMemoryAfterWarmup();
            tests.SystemUpdate_UsesCanonicalFactionMaterialsAtOneSecondCadence();
            tests.SystemUpdate_SteadyStateDoesNotAllocateManagedMemory();
            Debug.Log("[MaterialFabricationFocusedValidation] result=Passed tests=12");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MaterialFabricationFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ApplyTick_ConsumesOilAndGrantsMaterialsExactlyOnce()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        BuildingResourceStorageComponent storage = CreateStorage(12f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(10, 100);

        MaterialFabricationSystem.TickResult result = MaterialFabricationSystem.ApplyTick(
            ref fabrication,
            ref storage,
            ref materials,
            30f,
            buildingOperational: true);
        MaterialFabricationSystem.TickResult repeated = MaterialFabricationSystem.ApplyTick(
            ref fabrication,
            ref storage,
            ref materials,
            0f,
            buildingOperational: true);

        Assert.AreEqual(1, result.CompletedCycles);
        Assert.AreEqual(4f, result.OilConsumedBarrels);
        Assert.AreEqual(20, result.MaterialsProduced);
        Assert.AreEqual(8f, storage.StoredOilBarrels);
        Assert.AreEqual(30, materials.Current);
        Assert.AreEqual(20, materials.LifetimeFabricated);
        Assert.AreEqual(0, repeated.CompletedCycles);
        Assert.AreEqual(30, materials.Current);
    }

    [Test]
    public void ApplyTick_IsDeterministicAcrossRenderFramePartitions()
    {
        MaterialFabricationComponent thirtyFpsFabrication = CreateFabrication();
        BuildingResourceStorageComponent thirtyFpsStorage = CreateStorage(40f);
        FactionTacticalMaterialsComponent thirtyFpsMaterials = CreateMaterials(0, 1000);
        MaterialFabricationComponent sixtyFpsFabrication = thirtyFpsFabrication;
        BuildingResourceStorageComponent sixtyFpsStorage = thirtyFpsStorage;
        FactionTacticalMaterialsComponent sixtyFpsMaterials = thirtyFpsMaterials;

        TickFrames(ref thirtyFpsFabrication, ref thirtyFpsStorage, ref thirtyFpsMaterials, 360, 1f / 30f);
        TickFrames(ref sixtyFpsFabrication, ref sixtyFpsStorage, ref sixtyFpsMaterials, 720, 1f / 60f);

        Assert.AreEqual(thirtyFpsStorage.StoredOilBarrels, sixtyFpsStorage.StoredOilBarrels, 0.0001f);
        Assert.AreEqual(thirtyFpsMaterials.Current, sixtyFpsMaterials.Current);
        Assert.AreEqual(thirtyFpsMaterials.LifetimeFabricated, sixtyFpsMaterials.LifetimeFabricated);
        Assert.AreEqual(thirtyFpsFabrication.CycleProgressSeconds, sixtyFpsFabrication.CycleProgressSeconds, 0.001f);
    }

    [Test]
    public void ApplyTick_StallsForEmptyAndPartialOilInput()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        fabrication.CycleProgressSeconds = 7f;
        BuildingResourceStorageComponent storage = CreateStorage(0f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(0, 100);

        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 10f, true);
        Assert.AreEqual(MaterialFabricationStatusCode.Blocked, fabrication.Status);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.NoOilInput, fabrication.BlockReason);
        Assert.AreEqual(7f, fabrication.CycleProgressSeconds);

        storage.StoredOilBarrels = 3.99f;
        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 10f, true);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.NoOilInput, fabrication.BlockReason);
        Assert.AreEqual(0, materials.Current);
    }

    [Test]
    public void ApplyTick_StallsAtMaterialsCapacityAndResumes()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        BuildingResourceStorageComponent storage = CreateStorage(8f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(90, 100);

        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 30f, true);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.MaterialsCapacityFull, fabrication.BlockReason);
        Assert.AreEqual(8f, storage.StoredOilBarrels);

        materials.Current = 70;
        materials.Version++;
        MaterialFabricationSystem.TickResult result = MaterialFabricationSystem.ApplyTick(
            ref fabrication,
            ref storage,
            ref materials,
            30f,
            true);
        Assert.AreEqual(1, result.CompletedCycles);
        Assert.AreEqual(90, materials.Current);
        Assert.AreEqual(MaterialFabricationStatusCode.Blocked, fabrication.Status);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.MaterialsCapacityFull, fabrication.BlockReason);
    }

    [Test]
    public void ApplyTick_DisableAndBuildingStatePreserveProgress()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        fabrication.CycleProgressSeconds = 9f;
        fabrication.ProductionEnabled = 0;
        BuildingResourceStorageComponent storage = CreateStorage(8f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(0, 100);

        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 20f, true);
        Assert.AreEqual(MaterialFabricationStatusCode.Disabled, fabrication.Status);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.ProductionDisabled, fabrication.BlockReason);
        Assert.AreEqual(9f, fabrication.CycleProgressSeconds);

        fabrication.ProductionEnabled = 1;
        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 20f, false);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.BuildingDisabled, fabrication.BlockReason);
        Assert.AreEqual(9f, fabrication.CycleProgressSeconds);

        MaterialFabricationSystem.TickResult result = MaterialFabricationSystem.ApplyTick(
            ref fabrication,
            ref storage,
            ref materials,
            21f,
            true);
        Assert.AreEqual(1, result.CompletedCycles);
    }

    [Test]
    public void ApplyTick_RejectsOwnershipMismatch()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        BuildingResourceStorageComponent storage = CreateStorage(8f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(0, 100);
        materials.FactionId = 2;

        MaterialFabricationSystem.TickResult result = MaterialFabricationSystem.ApplyTick(
            ref fabrication,
            ref storage,
            ref materials,
            30f,
            true);

        Assert.AreEqual(0, result.CompletedCycles);
        Assert.AreEqual(8f, storage.StoredOilBarrels);
        Assert.AreEqual(0, materials.Current);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.BuildingDisabled, fabrication.BlockReason);
    }

    [Test]
    public void ApplyTick_ConsumesOnlyUnreservedOil()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        BuildingResourceStorageComponent storage = CreateStorage(8f);
        storage.ReservedOilOutboundBarrels = 5f;
        FactionTacticalMaterialsComponent materials = CreateMaterials(0, 100);

        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 30f, true);

        Assert.AreEqual(8f, storage.StoredOilBarrels);
        Assert.AreEqual(5f, storage.ReservedOilOutboundBarrels);
        Assert.AreEqual(0, materials.Current);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.NoOilInput, fabrication.BlockReason);
    }

    [Test]
    public void ApplyTick_ProcessesLargeDeltaArithmeticallyWithoutFrameDrift()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        BuildingResourceStorageComponent storage = CreateStorage(1000f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(0, 10000);

        MaterialFabricationSystem.TickResult result = MaterialFabricationSystem.ApplyTick(
            ref fabrication,
            ref storage,
            ref materials,
            10000f,
            true);

        Assert.AreEqual(250, result.CompletedCycles);
        Assert.AreEqual(1000f, result.OilConsumedBarrels);
        Assert.AreEqual(5000, result.MaterialsProduced);
        Assert.AreEqual(0f, fabrication.CycleProgressSeconds);
        Assert.AreEqual(MaterialFabricationBlockReasonCode.NoOilInput, fabrication.BlockReason);
    }

    [Test]
    public void ApplyTick_IncrementsOnlyChangedVersions()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        BuildingResourceStorageComponent storage = CreateStorage(0f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(0, 100);
        uint initialFabricationVersion = fabrication.Version;
        uint initialStorageVersion = storage.Version;
        uint initialMaterialsVersion = materials.Version;

        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 10f, true);

        Assert.AreEqual(initialFabricationVersion, fabrication.Version);
        Assert.AreEqual(initialStorageVersion, storage.Version);
        Assert.AreEqual(initialMaterialsVersion, materials.Version);
    }

    [Test]
    public void ApplyTick_DoesNotAllocateManagedMemoryAfterWarmup()
    {
        MaterialFabricationComponent fabrication = CreateFabrication();
        BuildingResourceStorageComponent storage = CreateStorage(1000f);
        FactionTacticalMaterialsComponent materials = CreateMaterials(0, 10000);
        MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 1f, true);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 256; i++)
            MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, 1f, true);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0L, allocatedBytes);
    }

    [Test]
    public void SystemUpdate_UsesCanonicalFactionMaterialsAtOneSecondCadence()
    {
        using World world = new(nameof(SystemUpdate_UsesCanonicalFactionMaterialsAtOneSecondCadence));
        EntityManager em = world.EntityManager;
        Entity gameplayState = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(gameplayState, new RuntimeGameplayStateComponent
        {
            SimulationActive = 1
        });
        Entity economy = em.CreateEntity(
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent),
            typeof(MaterialFabricationEconomyEventQueueComponent));
        em.AddBuffer<MaterialFabricationEconomyEventElement>(economy);
        em.SetComponentData(economy, new FactionEconomy
        {
            FactionId = FactionIdentity.PlayerFactionId
        });
        em.SetComponentData(economy, CreateMaterials(0, 100));
        Entity depot = em.CreateEntity(
            typeof(MaterialFabricationComponent),
            typeof(MaterialFabricationInputTag),
            typeof(BuildingResourceStorageComponent),
            typeof(UnitHealth));
        MaterialFabricationComponent fabrication = CreateFabrication();
        fabrication.CycleDurationSeconds = 2f;
        em.SetComponentData(depot, fabrication);
        em.SetComponentData(depot, CreateStorage(8f));
        em.SetComponentData(depot, new UnitHealth { Current = 100, Max = 100 });
        SystemHandle system = world.CreateSystem<MaterialFabricationSystem>();

        world.SetTime(new TimeData(1d, 1f));
        UpdateSystem(world, system);
        Assert.AreEqual(0, em.GetComponentData<FactionTacticalMaterialsComponent>(economy).Current);
        Assert.AreEqual(1f, em.GetComponentData<MaterialFabricationComponent>(depot).CycleProgressSeconds);

        world.SetTime(new TimeData(2d, 1f));
        UpdateSystem(world, system);
        Assert.AreEqual(20, em.GetComponentData<FactionTacticalMaterialsComponent>(economy).Current);
        Assert.AreEqual(4f, em.GetComponentData<BuildingResourceStorageComponent>(depot).StoredOilBarrels);
        DynamicBuffer<MaterialFabricationEconomyEventElement> events =
            em.GetBuffer<MaterialFabricationEconomyEventElement>(economy);
        Assert.AreEqual(2, events.Length);
        Assert.AreEqual(MaterialFabricationEconomyEventKind.StatusChanged, events[0].EventKind);
        Assert.AreEqual(MaterialFabricationEconomyEventKind.CycleCompleted, events[1].EventKind);
        Assert.AreEqual(1, events[1].CompletedCycles);
        Assert.AreEqual(4f, events[1].OilConsumedBarrels);
        Assert.AreEqual(20, events[1].MaterialsProduced);

        em.AddComponent<Disabled>(depot);
        world.SetTime(new TimeData(3d, 1f));
        UpdateSystem(world, system);
        Assert.AreEqual(20, em.GetComponentData<FactionTacticalMaterialsComponent>(economy).Current);
        Assert.AreEqual(
            MaterialFabricationBlockReasonCode.BuildingDisabled,
            em.GetComponentData<MaterialFabricationComponent>(depot).BlockReason);

        em.RemoveComponent<Disabled>(depot);
        world.SetTime(new TimeData(4d, 1f));
        UpdateSystem(world, system);
        Assert.AreEqual(20, em.GetComponentData<FactionTacticalMaterialsComponent>(economy).Current);
        Assert.AreEqual(
            MaterialFabricationStatusCode.Producing,
            em.GetComponentData<MaterialFabricationComponent>(depot).Status);
    }

    [Test]
    public void SystemUpdate_SteadyStateDoesNotAllocateManagedMemory()
    {
        using World world = new(nameof(SystemUpdate_SteadyStateDoesNotAllocateManagedMemory));
        EntityManager em = world.EntityManager;
        Entity gameplayState = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
        em.SetComponentData(gameplayState, new RuntimeGameplayStateComponent { SimulationActive = 1 });
        Entity economy = em.CreateEntity(
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent),
            typeof(MaterialFabricationEconomyEventQueueComponent));
        em.AddBuffer<MaterialFabricationEconomyEventElement>(economy)
            .EnsureCapacity(MaterialFabricationEconomyEventQueueComponent.Capacity);
        em.SetComponentData(economy, new FactionEconomy { FactionId = FactionIdentity.PlayerFactionId });
        em.SetComponentData(economy, CreateMaterials(0, 10000));
        Entity depot = em.CreateEntity(
            typeof(MaterialFabricationComponent),
            typeof(MaterialFabricationInputTag),
            typeof(BuildingResourceStorageComponent),
            typeof(UnitHealth));
        MaterialFabricationComponent fabrication = CreateFabrication();
        fabrication.CycleDurationSeconds = 1f;
        fabrication.MaterialsOutputPerCycle = 1;
        fabrication.OilConsumedPerCycle = 1f;
        em.SetComponentData(depot, fabrication);
        em.SetComponentData(depot, CreateStorage(1000f));
        em.SetComponentData(depot, new UnitHealth { Current = 100, Max = 100 });
        SystemHandle system = world.CreateSystem<MaterialFabricationSystem>();

        for (int i = 0; i < 32; i++)
        {
            world.SetTime(new TimeData(i + 1d, 1f));
            UpdateSystem(world, system);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 256; i++)
        {
            world.SetTime(new TimeData(i + 33d, 1f));
            UpdateSystem(world, system);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0L, allocatedBytes);
    }

    private static void UpdateSystem(World world, SystemHandle system)
    {
        world.Unmanaged.GetUnsafeSystemRef<MaterialFabricationSystem>(system)
            .OnUpdate(ref world.Unmanaged.ResolveSystemStateRef(system));
    }

    private static void TickFrames(
        ref MaterialFabricationComponent fabrication,
        ref BuildingResourceStorageComponent storage,
        ref FactionTacticalMaterialsComponent materials,
        int frameCount,
        float deltaTime)
    {
        for (int i = 0; i < frameCount; i++)
            MaterialFabricationSystem.ApplyTick(ref fabrication, ref storage, ref materials, deltaTime, true);
    }

    private static MaterialFabricationComponent CreateFabrication()
    {
        return new MaterialFabricationComponent
        {
            RuntimeBuildingId = 31,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            ProductionEnabled = 1,
            OutputCapacityPolicy = MaterialFabricationOutputCapacityPolicyCode.RequireFullCycleCapacity,
            OilConsumedPerCycle = 4f,
            MaterialsOutputPerCycle = 20,
            CycleDurationSeconds = 30f,
            Status = MaterialFabricationStatusCode.Blocked,
            BlockReason = MaterialFabricationBlockReasonCode.NoOilInput,
            Version = 1u
        };
    }

    private static BuildingResourceStorageComponent CreateStorage(float storedOil)
    {
        return new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = 31,
            OwnerFactionId = FactionIdentity.PlayerFactionId,
            OilStorageCapacity = 1000,
            StoredOilBarrels = storedOil,
            Version = 1u
        };
    }

    private static FactionTacticalMaterialsComponent CreateMaterials(int current, int capacity)
    {
        return new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Current = current,
            Capacity = capacity,
            Version = 1u
        };
    }
}
