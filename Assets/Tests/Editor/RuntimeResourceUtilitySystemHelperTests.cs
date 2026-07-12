using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class RuntimeResourceUtilitySystemHelperTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new RuntimeResourceUtilitySystemHelperTests();
            tests.Configure_CreatesCanonicalPlayerEconomyAndCompanions();
            tests.Configure_ReusesExistingPlayerEconomy();
            tests.CreditMutations_WriteFactionEconomyOnly();
            tests.CitizenContext_WritesSameFactionEconomy();
            tests.WarmedCreditMutations_DoNotAllocateManagedMemory();
            Debug.Log("[RuntimeResourceUtilityFocusedValidation] result=Passed tests=5");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[RuntimeResourceUtilityFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void Configure_CreatesCanonicalPlayerEconomyAndCompanions()
    {
        using var world = new World(nameof(Configure_CreatesCanonicalPlayerEconomyAndCompanions));
        var resources = new RuntimeResourceUtilitySystemHelper();
        resources.SetInitialDollars(345);

        resources.Configure(world.EntityManager);

        Entity player = GetPlayerEconomyEntity(world.EntityManager);
        Assert.AreEqual(345, resources.CurrentDollars);
        Assert.AreEqual(345, world.EntityManager.GetComponentData<FactionEconomy>(player).Money);
        Assert.IsTrue(world.EntityManager.HasComponent<FactionEconomyPolicy>(player));
        Assert.IsTrue(world.EntityManager.HasComponent<FactionTacticalMaterialsComponent>(player));
    }

    [Test]
    public void Configure_ReusesExistingPlayerEconomy()
    {
        using var world = new World(nameof(Configure_ReusesExistingPlayerEconomy));
        EntityManager em = world.EntityManager;
        Entity player = em.CreateEntity(typeof(FactionEconomy));
        em.SetComponentData(player, new FactionEconomy
        {
            FactionId = FactionIdentity.PlayerFactionId,
            Money = 1
        });
        var resources = new RuntimeResourceUtilitySystemHelper();
        resources.SetInitialDollars(500);

        resources.Configure(em);

        Assert.AreEqual(player, GetPlayerEconomyEntity(em));
        Assert.AreEqual(500, em.GetComponentData<FactionEconomy>(player).Money);
        Assert.IsTrue(em.HasComponent<FactionEconomyPolicy>(player));
        Assert.IsTrue(em.HasComponent<FactionTacticalMaterialsComponent>(player));
    }

    [Test]
    public void CreditMutations_WriteFactionEconomyOnly()
    {
        using var world = new World(nameof(CreditMutations_WriteFactionEconomyOnly));
        var resources = new RuntimeResourceUtilitySystemHelper();
        resources.SetInitialDollars(100);
        resources.Configure(world.EntityManager);

        Assert.IsTrue(resources.TrySpendDollars(40));
        Assert.IsFalse(resources.TrySpendDollars(61));
        resources.AddDollars(15);

        Entity player = GetPlayerEconomyEntity(world.EntityManager);
        Assert.AreEqual(75, resources.CurrentDollars);
        Assert.AreEqual(75, world.EntityManager.GetComponentData<FactionEconomy>(player).Money);
    }

    [Test]
    public void CitizenContext_WritesSameFactionEconomy()
    {
        using var world = new World(nameof(CitizenContext_WritesSameFactionEconomy));
        var resources = new RuntimeResourceUtilitySystemHelper();
        resources.SetInitialDollars(90);
        resources.Configure(world.EntityManager);
        CitizenResourceCompositionSystemHelper.Context context = resources.CreateCitizenResourceContext();

        Assert.IsTrue(CitizenResourceCompositionSystemHelper.TrySpendDollars(null, context, 30));

        Entity player = GetPlayerEconomyEntity(world.EntityManager);
        Assert.AreEqual(60, resources.CurrentDollars);
        Assert.AreEqual(60, world.EntityManager.GetComponentData<FactionEconomy>(player).Money);
    }

    [Test]
    public void WarmedCreditMutations_DoNotAllocateManagedMemory()
    {
        using var world = new World(nameof(WarmedCreditMutations_DoNotAllocateManagedMemory));
        var resources = new RuntimeResourceUtilitySystemHelper();
        resources.SetInitialDollars(1000);
        resources.Configure(world.EntityManager);
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

    private static Entity GetPlayerEconomyEntity(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        Entity player = Entity.Null;
        for (int i = 0; i < entities.Length; i++)
        {
            FactionEconomy economy = entityManager.GetComponentData<FactionEconomy>(entities[i]);
            if (!FactionIdentity.IsPlayerControlled(economy.FactionId))
                continue;

            Assert.AreEqual(Entity.Null, player, "Only one player faction economy may exist.");
            player = entities[i];
        }

        Assert.AreNotEqual(Entity.Null, player);
        return player;
    }
}
