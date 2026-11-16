using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal static class S073NativeAIParityControl
{
    private static void Need(bool value, string message) { if (!value) throw new InvalidDataException(message); }
    private static string Hash(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    private static float FloatBits(JToken token)
    {
        uint bits = Convert.ToUInt32((string)token["bits_hex"], 16);
        float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        Need(!float.IsNaN(value) && !float.IsInfinity(value) && value == (float)token["value"], "Exact finite native float required");
        return value;
    }
    private static void Main(string[] args)
    {
        Need(args.Length == 5 || args.Length == 7, "Repository, AI graph/hash, native receipt/hash and optional startup bundle/hash required");
        string root = Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        byte[] receiptBytes = File.ReadAllBytes(args[3]); Need(Hash(receiptBytes) == args[4], "Snapshot receipt changed");
        var receipt = JObject.Parse(System.Text.Encoding.UTF8.GetString(receiptBytes));
        Need((string)receipt["kind"] == "S073NativeAIStartupSnapshotReceipt" && (string)receipt["validation_status"] == "Passed" &&
             (int?)receipt["wrapper_exit_code"] == 0 && (bool?)receipt["normal_input_playthrough"] == false && (bool?)receipt["mission_ready"] == false,
             "Qualified instrumented native snapshot required");
        string dir = Path.GetDirectoryName(Path.GetFullPath(args[3]));
        byte[] snapshotBytes = File.ReadAllBytes(Path.Combine(dir, "snapshot.json")), log = File.ReadAllBytes(Path.Combine(dir, "editor.log"));
        Need(Hash(snapshotBytes) == (string)receipt["snapshot_sha256"] && Hash(log) == (string)receipt["editor_log_sha256"] &&
             System.Text.Encoding.UTF8.GetString(log).Contains("[S073NativeAIStartupSnapshot] result=Passed scope=instrumented_startup "), "Snapshot or log changed");
        var native = JObject.Parse(System.Text.Encoding.UTF8.GetString(snapshotBytes));
        Need((string)native["session_id"] == (string)receipt["session_id"] && (string)native["phase"] == "Playing" &&
             (bool?)native["normal_input_playthrough"] == false && (bool?)native["player_readiness"] == false, "Actual instrumented S073 session required");
        var pins = native["source_pins_sha256"] as JObject; Need(pins != null && pins.Count == (int?)receipt["source_pin_count"], "Complete native source pins required");
        foreach (var pin in pins.Properties())
        {
            string path = Path.GetFullPath(Path.Combine(root, pin.Name));
            Need(!Path.IsPathRooted(pin.Name) && path.StartsWith(root, StringComparison.Ordinal) && Hash(File.ReadAllBytes(path)) == (string)pin.Value,
                 "Native source changed: " + pin.Name);
        }
        var value = (JObject)native["pending_ai_settings_snapshot"];
        var settings = new AISettingsSnapshot {
            Difficulty = (AIDifficultySetting)(int)value["Difficulty"], StartingMoney = (AIStartingMoneySetting)(int)value["StartingMoney"],
            IncomeMultiplier = FloatBits(value["IncomeMultiplier"]), BuildSpeed = (AISpeedSetting)(int)value["BuildSpeed"],
            UnitProductionSpeed = (AISpeedSetting)(int)value["UnitProductionSpeed"], AttackGroupSize = (AIAttackGroupSizeSetting)(int)value["AttackGroupSize"],
            AttackFrequency = (AIAttackFrequencySetting)(int)value["AttackFrequency"], Aggression = (AIAggressionSetting)(int)value["Aggression"],
            Expansion = (AIExpansionSetting)(int)value["Expansion"], TargetPriority = (AITargetPriority)(int)value["TargetPriority"],
            PlayerAutoAIEnabled = (bool)value["PlayerAutoAIEnabled"], EnemyAICount = (int)value["EnemyAICount"]
        };
        var cells = new Dictionary<byte, int2>();
        foreach (JObject row in (JArray)native["post_startup_spawn_resolver_observations"])
        {
            Need((bool)row["resolved"] && (string)row["scope"] == "PostStartupOriginalResolverObservation", "Observed resolver value required");
            var cell = (JArray)row["cell"]; cells.Add((byte)row["faction_id"], new int2((int)cell[0], (int)cell[1]));
        }
        var nativePlans = ((JArray)native["ai_build_plans"]).OfType<JObject>().ToArray();
        Need(nativePlans.Length > 0, "Observed native plans required");
        float observedTime = FloatBits(nativePlans[0]["LastBuildTime"]);
        Need(nativePlans.All(row => FloatBits(row["LastBuildTime"]) == observedTime), "One common observed plan clock required");
        var manager = new EntityManager { LogicalAllocatorAdmitted = true }; int next = 0;
        manager.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        AIStartupSystem.TryResolveFactionSpawnCell resolver;
        if (args.Length == 7)
        {
            byte[] bundleBytes = File.ReadAllBytes(args[5]); Need(Hash(bundleBytes) == args[6], "Source startup bundle changed");
            var bundle = JObject.Parse(System.Text.Encoding.UTF8.GetString(bundleBytes));
            var configDescriptor = bundle["legacy_config"]; var prefabDescriptor = bundle["prefab_capture"];
            byte[] configBytes = File.ReadAllBytes((string)configDescriptor["path"]), prefabBytes = File.ReadAllBytes((string)prefabDescriptor["path"]);
            Need(Hash(configBytes) == (string)configDescriptor["sha256"] && Hash(prefabBytes) == (string)prefabDescriptor["sha256"], "Pinned S073 startup sources required");
            var config = JObject.Parse(System.Text.Encoding.UTF8.GetString(configBytes));
            Need((string)config["kind"] == "S073LegacyInitialSpawnExternalData" && (string)config["mission"] == "S073" &&
                 (string)config["source_artifacts"]?["prefab_capture_sha256"] == (string)prefabDescriptor["sha256"], "Same captured S073 startup data required");
            var data = config["initial_spawn"].ToObject<LegacyInitialSpawnData>();
            var keys = data.UnitPrefabRegistryKeys.Concat(data.Factions.SelectMany(f => f.Units).Select(u => u.SourceKey)).Distinct(StringComparer.Ordinal).ToArray();
            var imported = GeneratedReferenceTemplateImporter.Import(manager, JObject.Parse(System.Text.Encoding.UTF8.GetString(prefabBytes)),
                Path.GetDirectoryName((string)prefabDescriptor["path"]), keys);
            var initialized = new CustomGameStartupSystemHelper(manager).InitializeFromLegacyData(data, imported.Prefabs);
            Need(initialized.Initialized, "Original source startup owner rejected S073 data");
            var sourceResolver = new InitialFactionSpawnCellSystem();
            var noFallbacks = new List<InitialFactionSpawnCellFallbackEntry>();
            resolver = (byte faction, out int2 cell) => sourceResolver.TryGetConfiguredFactionSpawnCell(manager, noFallbacks, faction, out cell);
            foreach (var pair in cells)
                Need(resolver(pair.Key, out int2 actual) && actual.x == pair.Value.x && actual.y == pair.Value.y, "Original source resolver differs from post-start native observation");
            Need(Hash(File.ReadAllBytes(args[5])) == args[6], "Source bundle changed during resolver replay");
        }
        else resolver = (byte faction, out int2 cell) => cells.TryGetValue(faction, out cell);
        // The captured post-start resolver values and observed plan clock are
        // explicit replay inputs, not proof of startup-time provenance.
        S073AIStartupSourceBinding.Initialize(manager, args[0], args[1], args[2], settings, resolver, observedTime);
        using var controlQuery = manager.CreateEntityQuery(typeof(FactionControlConfigTag), typeof(FactionControlEntry));
        Need(controlQuery.CalculateEntityCount() == 1, "Unique source control owner required");
        var controls = manager.GetBuffer<FactionControlEntry>(controlQuery.GetSingletonEntity()); var nativeControls = (JArray)native["faction_control_entries"];
        Need(controls.Length == nativeControls.Count, "Control count differs");
        for (int i = 0; i < controls.Length; i++)
        {
            var actual = controls[i]; var expected = nativeControls[i];
            Need(actual.FactionId == (byte)expected["FactionId"] && actual.AIControlled == (byte)expected["AIControlled"] &&
                 actual.IsPlayerFaction == (byte)expected["IsPlayerFaction"] && actual.LastLogTime == FloatBits(expected["LastLogTime"]), "Observed control differs");
        }
        using var planQuery = manager.CreateEntityQuery(typeof(AIBuildPlan), typeof(AIBuildPlanEntry));
        using var entities = planQuery.ToEntityArray(Allocator.Temp); Need(entities.Length == nativePlans.Length, "Plan count differs");
        foreach (Entity entity in entities)
        {
            var actual = manager.GetComponentData<AIBuildPlan>(entity);
            var expected = nativePlans.Single(row => (byte)row["FactionId"] == actual.FactionId);
            var position = expected["BaseCenterCell"]; var entries = manager.GetBuffer<AIBuildPlanEntry>(entity);
            Need(actual.Enabled == (byte)expected["Enabled"] && actual.NextBuildIndex == (int)expected["NextBuildIndex"] &&
                 actual.BaseCenterCell.x == (int)position["x"] && actual.BaseCenterCell.y == (int)position["y"] &&
                 actual.BuildIntervalSeconds == FloatBits(expected["BuildIntervalSeconds"]) && actual.LastLogTime == FloatBits(expected["LastLogTime"]) &&
                 entries.Select(entry => entry.BuildingId.ToString()).SequenceEqual(((JArray)expected["entries_ordered"]).Select(x => (string)x)),
                 "Observed AI build plan differs");
        }
        Need(Hash(File.ReadAllBytes(args[3])) == args[4], "Snapshot receipt changed during replay");
        Console.WriteLine("[S073NativeAIParityControl] result=Passed scope=" + (args.Length == 7 ?
            "ObservedSettings_OriginalBakedSpawnResolver_ReplayedClock_NoLoadedMapOrReadiness" :
            "ObservedSettings_PostStartupCells_ReplayedClock_NotMissionReadiness"));
    }
}
