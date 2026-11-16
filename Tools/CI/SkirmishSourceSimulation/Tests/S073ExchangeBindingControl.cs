using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

internal static class S073ExchangeBindingControl
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string Sha(string path)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    private static bool Rejects(string root, string graph, string digest)
    {
        try { S073ExchangeSourceBinding.Load(root, graph, digest); return false; }
        catch (InvalidDataException) { return true; }
    }

    private static void Main(string[] args)
    {
        Need(args.Length == 3, "Repository, source graph, and isolated output directory required");
        string root = Path.GetFullPath(args[0]);
        string graphPath = Path.GetFullPath(args[1]);
        string output = Path.GetFullPath(args[2]);
        const string qualified = "cf51352a9046d8680cc8611b6d253fc812568b4169163afcb85e375ba7954427";
        Need(Sha(graphPath) == qualified, "Actual r3 source graph digest changed");

        Need(Rejects(root, graphPath, new string('0', 64)), "Changed expected digest was accepted");
        JObject altered = JObject.Parse(File.ReadAllText(graphPath));
        altered["scope"] = "tampered control graph";
        string alteredPath = Path.Combine(output, "altered-graph.json");
        File.WriteAllText(alteredPath, altered.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        Need(Rejects(root, alteredPath, qualified), "Changed graph bytes were accepted under the qualified digest");

        ResourceExchangeRecipeConfigSet config = S073ExchangeSourceBinding.Load(root, graphPath, qualified);
        Need(config != null && config.Recipes.Count == 5, "Typed scene config did not materialize its five recipes");
        ResourceExchangeScenarioGateConfigEntry gate = null;
        foreach (var candidate in config.ScenarioGates)
            if (candidate.ScenarioTag == "custom.skirmish.legacy") gate = candidate;
        Need(gate != null && gate.ExchangeEnabled && !gate.AllowAiExchange && gate.MaxQueueItems == 2,
             "Typed S073 scenario gate differs from authored scene config");

        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        em.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        Entity startup = em.CreateEntity(typeof(CustomGameStartupStateComponent), typeof(InitialUnitsSpawnConfig));
        em.SetComponentData(startup, new CustomGameStartupStateComponent
        {
            GameModeId = new FixedString64Bytes("custom.skirmish.legacy")
        });
        Entity player = em.CreateEntity(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent));
        em.SetComponentData(player, new FactionEconomy { FactionId = FactionIdentity.PlayerFactionId, Money = 30000000 });
        em.SetComponentData(player, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.PlayerFactionId, Current = 120, Capacity = 600
        });
        Entity enemy = em.CreateEntity(typeof(FactionEconomy), typeof(FactionTacticalMaterialsComponent));
        em.SetComponentData(enemy, new FactionEconomy { FactionId = FactionIdentity.EnemyFactionId, Money = 30000000 });
        em.SetComponentData(enemy, new FactionTacticalMaterialsComponent
        {
            FactionId = FactionIdentity.EnemyFactionId, Current = 120, Capacity = 600
        });
        Entity controlsEntity = em.CreateEntity(typeof(FactionControlConfigTag));
        var controls = em.AddBuffer<FactionControlEntry>(controlsEntity);
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.PlayerFactionId, IsPlayerFaction = 1 });
        controls.Add(new FactionControlEntry { FactionId = FactionIdentity.EnemyFactionId, AIControlled = 1 });

        var projection = new ResourceExchangeStartupProjectionSystemHelper(em);
        var playerResult = projection.Initialize(config);
        Need(playerResult.Projected && playerResult.RecipeCount == 5 && playerResult.Reason == ResourceExchangeReason.None,
             "Actual typed config failed player startup projection");
        Need(em.GetBuffer<ResourceExchangeRecipeComponent>(player).Length == 5,
             "Player startup projection did not publish all authored recipes");
        var aiResult = projection.InitializeEligibleAIFactions(config);
        Need(!aiResult.ScenarioAllowsAIExchange && aiResult.EligibleFactionCount == 1 &&
             aiResult.ProjectedFactionCount == 0 && aiResult.Reason == ResourceExchangeReason.None &&
             !em.HasComponent<ResourceExchangeEnabledComponent>(enemy),
             "Actual typed legacy gate did not suppress AI exchange projection");

        Entity runtimeBoundary = em.CreateEntity(typeof(BuildingRuntimeStateTag));
        em.AddBuffer<BuildingConfiguredSpawnableReadModel>(runtimeBoundary);
        em.AddBuffer<BuildingConfiguredUnitReadModel>(runtimeBoundary);
        var recovery = new MaterialsScenarioRecoveryStartupSystemHelper(em).Validate(config);
        Need(!recovery.IsValid && recovery.Code == MaterialsScenarioRecoveryValidationCode.CatalogNotReady &&
             recovery.ValidatedFactionCount == 0,
             "Actual S073 materials recovery owner did not retain its pre-catalog fail-closed result");

        Console.WriteLine("[S073ExchangeBindingControl] result=Passed digest_rejected=1 graph_bytes_rejected=1 typed_config=1 player_projection=1 ai_gate=1 recovery_owner=CatalogNotReady scope=SourceOwner_NotNativeStartupOrReadiness");
    }
}
