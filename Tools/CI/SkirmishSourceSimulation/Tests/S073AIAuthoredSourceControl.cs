using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

internal static class S073AIAuthoredSourceControl
{
    private static void Need(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Main(string[] args)
    {
        Need(args.Length == 2, "Source graph path and SHA-256 required");
        using var sha = SHA256.Create();
        Func<string, string> digest = path => BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        Need(digest(args[0]) == args[1], "Source graph digest changed");
        var graph = JObject.Parse(File.ReadAllText(args[0]));
        foreach (var pin in ((JObject)graph["source_pins"]).Properties())
            Need(digest(pin.Name) == (string)pin.Value, "Authored source pin changed: " + pin.Name);
        var source = new SourceConfigMaterializer(graph);
        var enemy = source.ByPath<AIControllerConfig>("Assets/Game/Configs/Skirmish/Enemy.asset");
        var player = source.ByPath<AIControllerConfig>("Assets/Game/Configs/Skirmish/Player.asset");
        var planConfig = source.ByPath<AIPlanEntryStartupConfig>("Assets/Game/Configs/Skirmish/Plan.asset");
        Need(enemy.FactionId == 2 && enemy.PreferredBuildingIds.Count == 1 &&
             enemy.PreferredBuildingIds[0] == "Building_GuardTower" && player.FactionId == 1,
             "Referenced AI source data changed");
        var em = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        em.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        // Defaults, zero elapsed time, and no spawn resolver are explicit fixture
        // inputs; they are not evidence of the native match settings or cells.
        var startup = new AIStartupSystem();
        startup.Initialize(em, new List<AIControllerConfig> { enemy, player }, planConfig, null, AISettingsSnapshot.Defaults, 0);
        using var controlsQuery = em.CreateEntityQuery(typeof(FactionControlConfigTag), typeof(FactionControlEntry));
        Need(controlsQuery.CalculateEntityCount() == 1, "Unique control owner required");
        var controls = em.GetBuffer<FactionControlEntry>(controlsQuery.GetSingletonEntity());
        Need(controls.Length == 2 && controls[0].FactionId == 2 && controls[1].FactionId == 1,
             "Preset controller order or faction projection changed");
        using var plans = em.CreateEntityQuery(typeof(AIBuildPlan), typeof(AIBuildPlanEntry));
        using var entities = plans.ToEntityArray(Unity.Collections.Allocator.Temp);
        int found = 0;
        foreach (var entity in entities)
        {
            if (em.GetComponentData<AIBuildPlan>(entity).FactionId != 2) continue;
            var entries = em.GetBuffer<AIBuildPlanEntry>(entity);
            Need(entries.Length == 1 && entries[0].BuildingId.ToString() ==
                 BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_GuardTower"),
                 "Enemy plan differs from the actual referenced configuration");
            found++;
        }
        Need(found == 1, "One enemy plan required");
        var startupEntity = em.CreateEntity();
        em.AddComponentData(startupEntity, new CustomGameStartupStateComponent());
        em.AddComponentData(startupEntity, new InitialUnitsSpawnConfig());
        var recovery = new MaterialsScenarioRecoveryStartupSystemHelper(em).Validate(null);
        Need(!recovery.IsValid && recovery.Code == MaterialsScenarioRecoveryValidationCode.CatalogNotReady,
             "Recovery must consume the projected controls and reject the absent catalog");
        Console.WriteLine("[S073AIAuthoredSourceControl] result=Passed scope=SourceAssets_DefaultSettings_NoNativeOrMissionReadiness");
    }
}
