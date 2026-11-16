using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;

internal static class AIStartupSourceControl
{
    private static void Need(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Main()
    {
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        em.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        var config = new AIControllerConfig();
        typeof(AIControllerConfig).GetField("factionId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(config, 2);
        config.PreferredBuildingIds.Add("Building_Test");
        var startup = new AIStartupSystem();
        startup.Initialize(em, new List<AIControllerConfig> { config }, null, null, AISettingsSnapshot.Defaults, 0);
        using var controlsQuery = em.CreateEntityQuery(typeof(FactionControlConfigTag), typeof(FactionControlEntry));
        Need(controlsQuery.CalculateEntityCount() == 1, "Expected one control owner");
        var controls = em.GetBuffer<FactionControlEntry>(controlsQuery.GetSingletonEntity());
        Need(controls.Length == 2 && controls[0].FactionId == 2 && controls[0].AIControlled == 1 &&
             controls[1].FactionId == 1 && controls[1].IsPlayerFaction == 1, "Control projection changed");
        using var plans = em.CreateEntityQuery(typeof(AIBuildPlan), typeof(AIBuildPlanEntry));
        Need(plans.CalculateEntityCount() == 1, "Expected one AI construction plan");
        var plan = plans.GetSingletonEntity();
        var entries = em.GetBuffer<AIBuildPlanEntry>(plan);
        Need(em.GetComponentData<AIBuildPlan>(plan).FactionId == 2 && entries.Length == 1 &&
             entries[0].BuildingId.ToString() == BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Test"),
             "Build plan projection changed");
        Console.WriteLine("[AIStartupSourceControl] result=Passed scope=SyntheticSourceFixture_NotNativeOrMissionReadiness");
    }
}
