using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeFactionResourceSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeFactionResourceSystemHelperTests();
            tests.Configure_CreatesCanonicalControlledFactionResources();
            tests.Configure_ReusesExistingControlledFactionEconomy();
            tests.CreditMutations_WriteFactionEconomyOnly();
            tests.CitizenContext_WritesSameFactionEconomy();
            tests.ConstructionSpend_WritesCanonicalCreditsAndMaterialsAtomically();
            tests.ConstructionRestore_RestoresCanonicalCreditsAndMaterialsAtomically();
            tests.FactionLookup_ResolvesOnlyControlledFaction();
            tests.WarmedCreditMutations_DoNotAllocateManagedMemory();
            Debug.Log("[RuntimeFactionResourceFocusedValidation] result=Passed tests=8");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RuntimeFactionResourceFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Configure_CreatesCanonicalControlledFactionResources()
    {
        using var world = new World(nameof(Configure_CreatesCanonicalControlledFactionResources));
        var resources = new RuntimeFactionResourceSystemHelper();
        resources.SetInitialDollars(345);

        resources.Configure(world.EntityManager);

        Entity factionResourceEntity = GetControlledFactionResourceEntity(world.EntityManager);
        Assert.AreEqual(345, resources.CurrentDollars);
        Assert.AreEqual(
            345,
            world.EntityManager.GetComponentData<FactionEconomy>(factionResourceEntity).Money);
        Assert.IsTrue(world.EntityManager.HasComponent<FactionEconomyPolicy>(factionResourceEntity));
        Assert.IsTrue(
            world.EntityManager.HasComponent<FactionTacticalMaterialsComponent>(factionResourceEntity));
    }

    [Test]
    public void Configure_ReusesExistingControlledFactionEconomy()
    {
        using var world = new World(nameof(Configure_ReusesExistingControlledFactionEconomy));
        EntityManager entityManager = world.EntityManager;
        Entity factionResourceEntity = entityManager.CreateEntity(typeof(FactionEconomy));
        entityManager.SetComponentData(factionResourceEntity, new FactionEconomy
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Money = 1
        });
        var resources = new RuntimeFactionResourceSystemHelper();
        resources.SetInitialDollars(500);

        resources.Configure(entityManager);

        Assert.AreEqual(
            factionResourceEntity,
            GetControlledFactionResourceEntity(entityManager));
        Assert.AreEqual(500, entityManager.GetComponentData<FactionEconomy>(factionResourceEntity).Money);
        Assert.IsTrue(entityManager.HasComponent<FactionEconomyPolicy>(factionResourceEntity));
        Assert.IsTrue(
            entityManager.HasComponent<FactionTacticalMaterialsComponent>(factionResourceEntity));
    }

    [Test]
    public void CreditMutations_WriteFactionEconomyOnly()
    {
        using var world = new World(nameof(CreditMutations_WriteFactionEconomyOnly));
        var resources = CreateConfiguredResources(world.EntityManager, 100);

        Assert.IsTrue(resources.TrySpendDollars(40));
        Assert.IsFalse(resources.TrySpendDollars(61));
        resources.AddDollars(15);

        Entity factionResourceEntity = GetControlledFactionResourceEntity(world.EntityManager);
        Assert.AreEqual(75, resources.CurrentDollars);
        Assert.AreEqual(
            75,
            world.EntityManager.GetComponentData<FactionEconomy>(factionResourceEntity).Money);
    }

    [Test]
    public void CitizenContext_WritesSameFactionEconomy()
    {
        using var world = new World(nameof(CitizenContext_WritesSameFactionEconomy));
        var resources = CreateConfiguredResources(world.EntityManager, 90);
        CitizenResourceCompositionSystemHelper.Context context = resources.CreateCitizenResourceContext();

        Assert.IsTrue(CitizenResourceCompositionSystemHelper.TrySpendDollars(null, context, 30));

        Entity factionResourceEntity = GetControlledFactionResourceEntity(world.EntityManager);
        Assert.AreEqual(60, resources.CurrentDollars);
        Assert.AreEqual(
            60,
            world.EntityManager.GetComponentData<FactionEconomy>(factionResourceEntity).Money);
    }

    [Test]
    public void ConstructionSpend_WritesCanonicalCreditsAndMaterialsAtomically()
    {
        using var world = new World(nameof(ConstructionSpend_WritesCanonicalCreditsAndMaterialsAtomically));
        EntityManager entityManager = world.EntityManager;
        var resources = CreateConfiguredResources(entityManager, 100);
        Entity factionResourceEntity = GetControlledFactionResourceEntity(entityManager);
        entityManager.SetComponentData(factionResourceEntity, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Current = 40,
            Capacity = 80,
            Version = 2u
        });

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            resources.TrySpendConstructionResources(25, 12));
        Assert.AreEqual(75, resources.CurrentDollars);
        Assert.AreEqual(28, resources.CurrentMaterials);

        FactionTacticalMaterialsComponent materials =
            entityManager.GetComponentData<FactionTacticalMaterialsComponent>(factionResourceEntity);
        Assert.AreEqual(12, materials.LifetimeSpent);
        Assert.AreEqual(3u, materials.Version);

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.InsufficientCreditsAndMaterials,
            resources.TrySpendConstructionResources(76, 29));
        Assert.AreEqual(75, resources.CurrentDollars);
        Assert.AreEqual(28, resources.CurrentMaterials);
    }

    [Test]
    public void ConstructionRestore_RestoresCanonicalCreditsAndMaterialsAtomically()
    {
        using var world = new World(nameof(ConstructionRestore_RestoresCanonicalCreditsAndMaterialsAtomically));
        EntityManager entityManager = world.EntityManager;
        var resources = CreateConfiguredResources(entityManager, 500);
        Entity factionResourceEntity = GetControlledFactionResourceEntity(entityManager);
        entityManager.SetComponentData(factionResourceEntity, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Current = 80,
            Capacity = 100,
            Version = 4u
        });

        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            resources.TrySpendConstructionResources(120, 30));
        Assert.AreEqual(
            FactionConstructionResourceMutationResult.Applied,
            resources.TryRestoreConstructionResources(120, 30));

        Assert.AreEqual(500, resources.CurrentDollars);
        Assert.AreEqual(80, resources.CurrentMaterials);
        FactionTacticalMaterialsComponent materials =
            entityManager.GetComponentData<FactionTacticalMaterialsComponent>(factionResourceEntity);
        Assert.AreEqual(0, materials.LifetimeSpent);
        Assert.AreEqual(6u, materials.Version);
    }

    [Test]
    public void FactionLookup_ResolvesOnlyControlledFaction()
    {
        using var world = new World(nameof(FactionLookup_ResolvesOnlyControlledFaction));
        var resources = CreateConfiguredResources(world.EntityManager, 100);

        Assert.IsTrue(resources.TryGetFactionResourceEntity(
            FactionIdentity.PlayerFactionId,
            out Entity factionResourceEntity));
        Assert.AreEqual(
            GetControlledFactionResourceEntity(world.EntityManager),
            factionResourceEntity);
        Assert.IsFalse(resources.TryGetFactionResourceEntity(
            unchecked((byte)(FactionIdentity.PlayerFactionId + 1)),
            out _));
    }

    [Test]
    public void WarmedCreditMutations_DoNotAllocateManagedMemory()
    {
        using var world = new World(nameof(WarmedCreditMutations_DoNotAllocateManagedMemory));
        var resources = CreateConfiguredResources(world.EntityManager, 1000);
        Assert.AreEqual(1000, resources.CurrentDollars);

        bool allSpendsAccepted = true;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 512; i++)
        {
            allSpendsAccepted &= resources.TrySpendDollars(1);
            resources.AddDollars(1);
        }

        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.IsTrue(allSpendsAccepted);
        Assert.AreEqual(0L, allocatedBytes);
        Assert.AreEqual(1000, resources.CurrentDollars);
    }

    private static RuntimeFactionResourceSystemHelper CreateConfiguredResources(
        EntityManager entityManager,
        int initialDollars)
    {
        var resources = new RuntimeFactionResourceSystemHelper();
        resources.SetInitialDollars(initialDollars);
        resources.Configure(entityManager);
        return resources;
    }

    private static Entity GetControlledFactionResourceEntity(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        Entity controlledFactionEntity = Entity.Null;
        for (int i = 0; i < entities.Length; i++)
        {
            FactionEconomy economy = entityManager.GetComponentData<FactionEconomy>(entities[i]);
            if (!FactionIdentity.IsPlayerControlled(economy.FactionId))
                continue;

            Assert.AreEqual(
                Entity.Null,
                controlledFactionEntity,
                "Only one controlled-faction economy may exist.");
            controlledFactionEntity = entities[i];
        }

        Assert.AreNotEqual(Entity.Null, controlledFactionEntity);
        return controlledFactionEntity;
    }
}
