using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class ResourceExchangeAIRecoverySystemTests
{
    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            RunValidationStep(nameof(InputAmountUsesAuthoredMinimumAndStep), test => test.InputAmountUsesAuthoredMinimumAndStep(), ref passed);
            RunValidationStep(nameof(FasterLocalProductionPreventsImport), test => test.FasterLocalProductionPreventsImport(), ref passed);
            RunValidationStep(nameof(OilStarvedDepotReceivesAuthoredDurationGrace), test => test.OilStarvedDepotReceivesAuthoredDurationGrace(), ref passed);
            RunValidationStep(nameof(ExplicitScenarioGateIsRequired), test => test.ExplicitScenarioGateIsRequired(), ref passed);
            RunValidationStep(nameof(AIRecoveryQueuesOneCanonicalRequest), test => test.AIRecoveryQueuesOneCanonicalRequest(), ref passed);
            RunValidationStep(nameof(PlayerControlTransitionStopsAIRecovery), test => test.PlayerControlTransitionStopsAIRecovery(), ref passed);
            RunValidationStep(nameof(OrphanRecoveryNeedDoesNotQueueImport), test => test.OrphanRecoveryNeedDoesNotQueueImport(), ref passed);
            RunValidationStep(nameof(AIRecoveryValidationReservesCanonicalOil), test => test.AIRecoveryValidationReservesCanonicalOil(), ref passed);
            RunValidationStep(nameof(WarmedLocalRecoveryPathAllocatesNoManagedMemory), test => test.WarmedLocalRecoveryPathAllocatesNoManagedMemory(), ref passed);
            Debug.Log($"[ResourceExchangeAIRecoveryValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[ResourceExchangeAIRecoveryValidation] result=Failed passed={passed}\n{exception}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void InputAmountUsesAuthoredMinimumAndStep()
    {
        ResourceExchangeRecipeComponent recipe = CreateRecipe();

        Assert.IsTrue(ResourceExchangeAIRecoverySystem.TryResolveInputAmount(
            recipe,
            missingMaterials: 100,
            availableMaterialsCapacity: 500,
            out int inputAmount,
            out int outputAmount,
            out float durationSeconds));

        Assert.AreEqual(1800, inputAmount);
        Assert.AreEqual(100, outputAmount);
        Assert.AreEqual(90f, durationSeconds);
    }

    [Test]
    public void FasterLocalProductionPreventsImport()
    {
        AIMaterialsRecoveryNeedComponent need = CreateNeed(firstBlockedTimeSeconds: 0f);
        ResourceExchangeAIRecoverySystem.LocalRecoverySummary localRecovery = new()
        {
            ProjectedMaterials = 100
        };

        Assert.IsFalse(ResourceExchangeAIRecoverySystem.ShouldRequestImport(
            need,
            localRecovery,
            importDurationSeconds: 90f,
            now: 300f));
    }

    [Test]
    public void OilStarvedDepotReceivesAuthoredDurationGrace()
    {
        AIMaterialsRecoveryNeedComponent need = CreateNeed(firstBlockedTimeSeconds: 10f);
        ResourceExchangeAIRecoverySystem.LocalRecoverySummary localRecovery = new()
        {
            HasAwaitingOilDepot = 1
        };

        Assert.IsFalse(ResourceExchangeAIRecoverySystem.ShouldRequestImport(
            need,
            localRecovery,
            importDurationSeconds: 90f,
            now: 99.9f));
        Assert.IsTrue(ResourceExchangeAIRecoverySystem.ShouldRequestImport(
            need,
            localRecovery,
            importDurationSeconds: 90f,
            now: 100f));
    }

    [Test]
    public void ExplicitScenarioGateIsRequired()
    {
        using World world = CreateRecoveryWorld(allowAIExchange: false, aiControlled: true, out Entity exchange, out _);
        UpdateRecoverySystem(world);

        Assert.AreEqual(0, world.EntityManager.GetBuffer<ResourceExchangeRequestComponent>(exchange).Length);
    }

    [Test]
    public void AIRecoveryQueuesOneCanonicalRequest()
    {
        using World world = CreateRecoveryWorld(allowAIExchange: true, aiControlled: true, out Entity exchange, out _);
        UpdateRecoverySystem(world);
        UpdateRecoverySystem(world);

        DynamicBuffer<ResourceExchangeRequestComponent> requests =
            world.EntityManager.GetBuffer<ResourceExchangeRequestComponent>(exchange);
        Assert.AreEqual(1, requests.Length);
        Assert.AreEqual(ResourceExchangeRequestKind.Start, requests[0].RequestKind);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, requests[0].FactionId);
        Assert.AreEqual(1800, requests[0].InputAmount);
        Assert.AreEqual("exchange.import_materials.emergency", requests[0].RecipeId.ToString());
    }

    [Test]
    public void PlayerControlTransitionStopsAIRecovery()
    {
        using World world = CreateRecoveryWorld(allowAIExchange: true, aiControlled: false, out Entity exchange, out Entity control);
        UpdateRecoverySystem(world);
        Assert.AreEqual(0, world.EntityManager.GetBuffer<ResourceExchangeRequestComponent>(exchange).Length);

        DynamicBuffer<FactionControlEntry> controls = world.EntityManager.GetBuffer<FactionControlEntry>(control);
        FactionControlEntry entry = controls[0];
        entry.AIControlled = 1;
        entry.IsPlayerFaction = 0;
        controls[0] = entry;
        UpdateRecoverySystem(world);
        Assert.AreEqual(1, world.EntityManager.GetBuffer<ResourceExchangeRequestComponent>(exchange).Length);

        world.EntityManager.GetBuffer<ResourceExchangeRequestComponent>(exchange).Clear();
        entry.AIControlled = 0;
        entry.IsPlayerFaction = 1;
        controls[0] = entry;
        UpdateRecoverySystem(world);
        Assert.AreEqual(0, world.EntityManager.GetBuffer<ResourceExchangeRequestComponent>(exchange).Length);
    }

    [Test]
    public void OrphanRecoveryNeedDoesNotQueueImport()
    {
        using World world = CreateRecoveryWorld(allowAIExchange: true, aiControlled: true, out Entity exchange, out _);
        EntityManager em = world.EntityManager;
        using EntityQuery planQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<AIBuildPlan>(),
            ComponentType.ReadOnly<AIMaterialsRecoveryNeedComponent>());
        Entity planEntity = planQuery.GetSingletonEntity();
        em.RemoveComponent<AIBuildPlan>(planEntity);

        UpdateRecoverySystem(world);

        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeRequestComponent>(exchange).Length);
    }

    [Test]
    public void AIRecoveryValidationReservesCanonicalOil()
    {
        using World world = CreateRecoveryWorld(allowAIExchange: true, aiControlled: true, out Entity exchange, out _);
        UpdateRecoverySystem(world);
        SystemHandle validationSystem = world.GetOrCreateSystem<ResourceExchangeRequestValidationSystem>();
        validationSystem.Update(world.Unmanaged);

        EntityManager em = world.EntityManager;
        Assert.AreEqual(5000, em.GetComponentData<FactionEconomy>(exchange).Money);
        Assert.AreEqual(0, em.GetBuffer<ResourceExchangeRequestComponent>(exchange).Length);
        DynamicBuffer<ResourceExchangeQueueComponent> queue =
            em.GetBuffer<ResourceExchangeQueueComponent>(exchange);
        Assert.AreEqual(1, queue.Length);
        Assert.AreEqual(ResourceExchangeQueueState.InProgress, queue[0].State);
        Assert.AreEqual(ResourceExchangeResourceKind.Materials, queue[0].OutputResource);
        Assert.AreEqual(100, queue[0].OutputAmount);
        using EntityQuery storageQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingResourceStorageComponent>());
        BuildingResourceStorageComponent storage =
            em.GetComponentData<BuildingResourceStorageComponent>(storageQuery.GetSingletonEntity());
        Assert.AreEqual(1800f, storage.ReservedOilOutboundBarrels);
    }

    [Test]
    public void WarmedLocalRecoveryPathAllocatesNoManagedMemory()
    {
        using World world = CreateRecoveryWorld(allowAIExchange: true, aiControlled: true, out Entity exchange, out _);
        Entity depot = world.EntityManager.CreateEntity(
            typeof(MaterialFabricationComponent),
            typeof(BuildingResourceStorageComponent));
        world.EntityManager.SetComponentData(depot, new MaterialFabricationComponent
        {
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            ProductionEnabled = 1,
            OilConsumedPerCycle = 2f,
            MaterialsOutputPerCycle = 20,
            CycleDurationSeconds = 10f,
            Status = MaterialFabricationStatusCode.Producing
        });
        world.EntityManager.SetComponentData(depot, new BuildingResourceStorageComponent
        {
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            OilStorageCapacity = 100,
            StoredOilBarrels = 100f
        });
        SystemHandle system = world.GetOrCreateSystem<ResourceExchangeAIRecoverySystem>();
        for (int i = 0; i < 64; i++)
            system.Update(world.Unmanaged);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++)
            system.Update(world.Unmanaged);
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0, allocatedBytes);
        Assert.AreEqual(0, world.EntityManager.GetBuffer<ResourceExchangeRequestComponent>(exchange).Length);
    }

    private static World CreateRecoveryWorld(
        bool allowAIExchange,
        bool aiControlled,
        out Entity exchange,
        out Entity control)
    {
        World world = new(nameof(ResourceExchangeAIRecoverySystemTests));
        EntityManager em = world.EntityManager;
        RuntimeGameplayStateTestHelper.SetPlayRequested(em, true);
        em.CreateEntity(typeof(BuildingRuntimeStateTag));

        control = em.CreateEntity(typeof(FactionControlConfigTag));
        em.AddBuffer<FactionControlEntry>(control).Add(new FactionControlEntry
        {
            FactionId = FactionIdentity.EnemyFactionId,
            AIControlled = aiControlled ? (byte)1 : (byte)0,
            IsPlayerFaction = aiControlled ? (byte)0 : (byte)1
        });

        Entity planEntity = em.CreateEntity(
            typeof(AIBuildPlan),
            typeof(AIMaterialsRecoveryNeedComponent));
        em.SetComponentData(planEntity, new AIBuildPlan
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Enabled = 1,
            BuildIntervalSeconds = 10f
        });
        em.SetComponentData(planEntity, CreateNeed(0f));

        exchange = em.CreateEntity(
            typeof(ResourceExchangeRequestQueueComponent),
            typeof(ResourceExchangeEnabledComponent),
            typeof(ResourceExchangeWalletComponent),
            typeof(ResourceExchangeSummaryComponent),
            typeof(FactionEconomy),
            typeof(FactionTacticalMaterialsComponent));
        em.SetComponentData(exchange, new ResourceExchangeEnabledComponent
        {
            Enabled = 1,
            FactionId = FactionIdentity.EnemyFactionId,
            AllowAiExchange = allowAIExchange ? (byte)1 : (byte)0,
            MaxQueueItems = 2,
            ScenarioTag = new FixedString64Bytes("custom.skirmish.test")
        });
        em.SetComponentData(exchange, new FactionEconomy
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Money = 5000
        });
        em.SetComponentData(exchange, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Current = 0,
            Capacity = 500
        });
        em.AddBuffer<ResourceExchangeRecipeComponent>(exchange).Add(CreateRecipe());
        em.AddBuffer<ResourceExchangeRequestComponent>(exchange);
        em.AddBuffer<ResourceExchangeQueueComponent>(exchange);
        em.AddBuffer<ResourceExchangeResultComponent>(exchange);
        em.AddBuffer<ResourceExchangeEconomyEventComponent>(exchange);
        em.AddBuffer<ResourceExchangePhysicalReservationComponent>(exchange);
        Entity oilStorage = em.CreateEntity(typeof(BuildingResourceStorageComponent));
        em.SetComponentData(oilStorage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = 1,
            OwnerFactionId = FactionIdentity.EnemyFactionId,
            StoredOilBarrels = 9000f,
            OilStorageCapacity = 10000
        });
        return world;
    }

    private static void UpdateRecoverySystem(World world)
    {
        SystemHandle system = world.GetOrCreateSystem<ResourceExchangeAIRecoverySystem>();
        system.Update(world.Unmanaged);
    }

    private static AIMaterialsRecoveryNeedComponent CreateNeed(float firstBlockedTimeSeconds)
    {
        return new AIMaterialsRecoveryNeedComponent
        {
            FactionId = FactionIdentity.EnemyFactionId,
            Active = 1,
            RequiredCredits = 1000,
            RequiredMaterials = 100,
            MissingMaterials = 100,
            FirstBlockedTimeSeconds = firstBlockedTimeSeconds
        };
    }

    private static ResourceExchangeRecipeComponent CreateRecipe()
    {
        return new ResourceExchangeRecipeComponent
        {
            RecipeId = new FixedString128Bytes("exchange.import_materials.emergency"),
            RouteType = ResourceExchangeRouteType.Import,
            InputResource = ResourceExchangeResourceKind.Oil,
            OutputResource = ResourceExchangeResourceKind.Materials,
            InputAmountMin = 1800,
            InputAmountMax = 9000,
            InputStep = 1800,
            OutputPerInput = 1f / 18f,
            DurationSecondsBase = 90f,
            Enabled = 1,
            MissionTag = new FixedString64Bytes("custom.skirmish.test"),
            SortOrder = 10
        };
    }

    private static void RunValidationStep(
        string name,
        Action<ResourceExchangeAIRecoverySystemTests> action,
        ref int passed)
    {
        var test = new ResourceExchangeAIRecoverySystemTests();
        action(test);
        passed++;
        Debug.Log($"[ResourceExchangeAIRecoveryValidation] pass={name}");
    }
}
