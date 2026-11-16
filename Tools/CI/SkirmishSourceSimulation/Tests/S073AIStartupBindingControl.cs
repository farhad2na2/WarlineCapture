using System;
using System.IO;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using Unity.Mathematics;
using Newtonsoft.Json.Linq;

internal static class S073AIStartupBindingControl
{
    private static void Need(bool value, string detail) { if (!value) throw new Exception(detail); }
    private static void Main(string[] args)
    {
        Need(args.Length == 3, "Repository, graph and graph digest required");
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        manager.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        AIStartupSystem.TryResolveFactionSpawnCell resolver = (byte faction, out int2 cell) =>
        {
            cell = faction == 2 ? new int2(22, 33) : new int2(44, 55);
            return faction == 1 || faction == 2;
        };
        bool rejected = false;
        try { S073AIStartupSourceBinding.Initialize(manager, args[0], args[1], new string('0', 64), AISettingsSnapshot.Defaults, resolver, 5); }
        catch (System.IO.InvalidDataException) { rejected = true; }
        Need(rejected, "Incorrect graph digest was accepted");
        rejected = false;
        try { S073AIStartupSourceBinding.Initialize(manager, args[0], args[1], args[2], AISettingsSnapshot.Defaults, null, 5); }
        catch (System.IO.InvalidDataException) { rejected = true; }
        Need(rejected, "Missing spawn resolver was accepted");
        var graph = JObject.Parse(File.ReadAllText(args[1]));
        ((JObject)graph["source_pins"]).Remove(Path.GetFullPath(Path.Combine(args[0], "Assets/Game/Configs/Skirmish/Enemy.asset")));
        string incomplete = Path.GetTempFileName();
        try
        {
            File.WriteAllText(incomplete, graph.ToString());
            using var sha = SHA256.Create();
            string incompleteHash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(incomplete))).Replace("-", "").ToLowerInvariant();
            rejected = false;
            try { S073AIStartupSourceBinding.Initialize(manager, args[0], incomplete, incompleteHash, AISettingsSnapshot.Defaults, resolver, 5); }
            catch (InvalidDataException) { rejected = true; }
            Need(rejected, "Graph lacking an authored asset pin was accepted");
        }
        finally { File.Delete(incomplete); }
        S073AIStartupSourceBinding.Initialize(manager, args[0], args[1], args[2], AISettingsSnapshot.Defaults, resolver, 5);
        using var plans = manager.CreateEntityQuery(typeof(AIBuildPlan), typeof(AIBuildPlanEntry));
        using var entities = plans.ToEntityArray(Unity.Collections.Allocator.Temp);
        int matching = 0;
        foreach (var entity in entities)
        {
            var plan = manager.GetComponentData<AIBuildPlan>(entity);
            if (plan.FactionId != 2) continue;
            Need(plan.BaseCenterCell.x == 22 && plan.BaseCenterCell.y == 33 && plan.LastBuildTime == 5,
                 "Explicit resolver or elapsed time was not used by the owner");
            matching++;
        }
        Need(matching == 1, "Expected exactly one authored enemy plan");
        Console.WriteLine("[S073AIStartupBindingControl] result=Passed scope=FixtureInputs_NotNativeOrMissionReadiness");
    }
}
