#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;

public sealed class FactionEconomyStartupSystemValidationTests
{
    [SetUp]
    public void SetUp()
    {
        AISettingsRuntimeState.ResetDefaults();
        AISettingsRuntimeState.EnemyAICount = 1;
    }

    [TearDown]
    public void TearDown()
    {
        AISettingsRuntimeState.ResetDefaults();
    }

    [Test]
    public void Initialize_ProjectsAiEconomyConfigIntoFactionEconomy()
    {
        AIControllerConfig enemy = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset");
        using var world = new World("FactionEconomyStartupSystemValidationTests");

        FactionEconomyStartupSystem system = new();
        system.Initialize(world.EntityManager, new[] { enemy });

        EntityManager em = world.EntityManager;
        Entity economyEntity = GetEntityForFaction(em, FactionIdentity.EnemyFactionId);
        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        FactionEconomyPolicy policy = em.GetComponentData<FactionEconomyPolicy>(economyEntity);

        Assert.AreEqual(FactionIdentity.EnemyFactionId, economy.FactionId);
        Assert.AreEqual(75000, economy.Money);
        Assert.AreEqual(0f, economy.Oil);
        Assert.AreEqual(0f, economy.Fuel);
        Assert.AreEqual(-999f, economy.LastLogTime);
        Assert.AreEqual(1, policy.Enabled);
        Assert.AreEqual(1.15f, policy.IncomeMultiplier, 0.0001f);
        Assert.AreEqual(150, policy.OilSellPrice);
        Assert.AreEqual(220, policy.FuelSellPrice);
        Assert.AreEqual(8f, policy.SellIntervalSeconds, 0.0001f);
    }

    [Test]
    public void Initialize_ProjectsDisabledPlayerAutoPolicy()
    {
        AIControllerConfig playerAuto = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_PlayerAuto_Config.asset");
        AISettingsRuntimeState.PlayerAutoAIEnabled = false;
        using var world = new World("FactionEconomyStartupSystemPlayerAutoPolicyTests");

        FactionEconomyStartupSystem system = new();
        system.Initialize(world.EntityManager, new[] { playerAuto });

        EntityManager em = world.EntityManager;
        Entity economyEntity = GetEntityForFaction(em, FactionIdentity.PlayerFactionId);
        FactionEconomy economy = em.GetComponentData<FactionEconomy>(economyEntity);
        FactionEconomyPolicy policy = em.GetComponentData<FactionEconomyPolicy>(economyEntity);

        Assert.AreEqual(300000, economy.Money);
        Assert.AreEqual(0, policy.Enabled);
        Assert.AreEqual(1f, policy.IncomeMultiplier, 0.0001f);
    }

    [Test]
    public void Initialize_AddsMissingPolicyToExistingEconomyEntity()
    {
        AIControllerConfig enemy = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset");
        using var world = new World("FactionEconomyStartupSystemExistingEntityTests");
        EntityManager em = world.EntityManager;
        Entity economyEntity = em.CreateEntity(typeof(FactionEconomy));
        em.SetComponentData(economyEntity, new FactionEconomy { FactionId = FactionIdentity.EnemyFactionId, Money = 1 });

        FactionEconomyStartupSystem system = new();
        system.Initialize(em, new[] { enemy });

        Assert.IsTrue(em.HasComponent<FactionEconomyPolicy>(economyEntity));
        Assert.AreEqual(75000, em.GetComponentData<FactionEconomy>(economyEntity).Money);
        Assert.AreEqual(1.15f, em.GetComponentData<FactionEconomyPolicy>(economyEntity).IncomeMultiplier, 0.0001f);
    }

    private static AIControllerConfig LoadAIConfig(string path)
    {
        AIControllerConfig config = AssetDatabase.LoadAssetAtPath<AIControllerConfig>(path);
        Assert.NotNull(config, $"Missing AI config asset at {path}");
        return config;
    }

    private static Entity GetEntityForFaction(EntityManager em, byte factionId)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            FactionEconomy economy = em.GetComponentData<FactionEconomy>(entities[i]);
            if (economy.FactionId == factionId)
                return entities[i];
        }

        Assert.Fail($"Missing FactionEconomy for faction {factionId}.");
        return Entity.Null;
    }
}
#endif
