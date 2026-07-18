using Game.Components;
using Game.Configs;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using UnityEditor;

public sealed class FactionEconomyStartupSystemValidationTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new FactionEconomyStartupSystemValidationTests();
            tests.Initialize_ProjectsAiEconomyConfigIntoFactionEconomy();
            tests.Initialize_ProjectsDisabledPlayerAutoPolicy();
            tests.Initialize_AddsMissingPolicyToExistingEconomyEntity();
            UnityEngine.Debug.Log("[FactionEconomyStartupValidation] result=Passed tests=3");
        }
        catch (System.Exception exception)
        {
            UnityEngine.Debug.LogError("[FactionEconomyStartupValidation] result=Failed");
            UnityEngine.Debug.LogException(exception);
            throw;
        }
    }

    [Test]
    public void Initialize_ProjectsAiEconomyConfigIntoFactionEconomy()
    {
        AIControllerConfig enemy = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_Enemy_Config.asset");
        using var world = new World("FactionEconomyStartupSystemValidationTests");

        FactionEconomyStartupSystem system = new();
        system.Initialize(world.EntityManager, new[] { ToStartupEntry(enemy) }, AISettingsSnapshot.Defaults);

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
        FactionTacticalMaterialsComponent materials =
            em.GetComponentData<FactionTacticalMaterialsComponent>(economyEntity);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, materials.FactionId);
        Assert.AreEqual(0, materials.Current);
        Assert.AreEqual(0, materials.Capacity);
        FactionMaterialFabricationTelemetryComponent telemetry =
            em.GetComponentData<FactionMaterialFabricationTelemetryComponent>(economyEntity);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, telemetry.FactionId);
        Assert.AreEqual(0f, telemetry.ActiveSeconds);
        FactionFuelLogisticsTelemetryComponent logisticsTelemetry =
            em.GetComponentData<FactionFuelLogisticsTelemetryComponent>(economyEntity);
        Assert.AreEqual(FactionIdentity.EnemyFactionId, logisticsTelemetry.FactionId);
        Assert.AreEqual(0, logisticsTelemetry.TrayRouteAssignmentCount);
    }

    [Test]
    public void Initialize_ProjectsDisabledPlayerAutoPolicy()
    {
        AIControllerConfig playerAuto = LoadAIConfig("Assets/Game/Configs/Scene/Game_AI_PlayerAuto_Config.asset");
        using var world = new World("FactionEconomyStartupSystemPlayerAutoPolicyTests");

        FactionEconomyStartupSystem system = new();
        system.Initialize(world.EntityManager, new[] { ToStartupEntry(playerAuto) }, AISettingsSnapshot.Defaults);

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
        system.Initialize(em, new[] { ToStartupEntry(enemy) }, AISettingsSnapshot.Defaults);

        Assert.IsTrue(em.HasComponent<FactionEconomyPolicy>(economyEntity));
        Assert.IsTrue(em.HasComponent<FactionTacticalMaterialsComponent>(economyEntity));
        Assert.IsTrue(em.HasComponent<FactionMaterialFabricationTelemetryComponent>(economyEntity));
        Assert.IsTrue(em.HasComponent<FactionFuelLogisticsTelemetryComponent>(economyEntity));
        Assert.AreEqual(75000, em.GetComponentData<FactionEconomy>(economyEntity).Money);
        Assert.AreEqual(1.15f, em.GetComponentData<FactionEconomyPolicy>(economyEntity).IncomeMultiplier, 0.0001f);
    }

    private static AIControllerConfig LoadAIConfig(string path)
    {
        AIControllerConfig config = AssetDatabase.LoadAssetAtPath<AIControllerConfig>(path);
        Assert.NotNull(config, $"Missing AI config asset at {path}");
        return config;
    }

    private static FactionEconomyStartupEntry ToStartupEntry(AIControllerConfig config)
    {
        return new FactionEconomyStartupEntry(
            config.Enabled,
            config.Role,
            (byte)UnityEngine.Mathf.Clamp(config.FactionId, 0, byte.MaxValue),
            config.StartingMoney,
            config.IncomeMultiplier,
            config.OilSellPrice,
            config.FuelSellPrice,
            config.BuildIntervalSeconds);
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
