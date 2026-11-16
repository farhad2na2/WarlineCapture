using System;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

internal static class MaterialsRecoveryReadOnlyBuffersControl
{
    private struct Entry : IBufferElementData { public int Value; }
    private static void Need(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Main()
    {
        var manager = new EntityManager { LogicalAllocatorAdmitted = true };
        int next = 0;
        manager.AllocateLogicalIdentity = () => new Entity { Index = ++next, Version = 1 };
        var first = manager.CreateEntity();
        var second = manager.CreateEntity();
        manager.AddBuffer<Entry>(first).Add(new Entry { Value = 7 });
        manager.AddBuffer<Entry>(second);
        var handle = manager.GetBufferTypeHandle<Entry>(true);
        var chunk = new ArchetypeChunk { Manager = manager, Entities = new[] { first, second }, Count = 2 };
        var accessor = chunk.GetBufferAccessor(ref handle);
        Need(accessor.Length == 2 && accessor[0].Length == 1 && accessor[0][0].Value == 7 && accessor[1].Length == 0,
            "Buffers lost entity alignment or empty buffer identity");
        accessor[0][0] = new Entry { Value = 9 };
        Need(manager.GetBuffer<Entry>(first)[0].Value == 7, "Snapshot mutated the world");
        bool writableRejected = false;
        try { manager.GetBufferTypeHandle<Entry>(false); } catch (NotSupportedException) { writableRejected = true; }
        Need(writableRejected, "Writable handle was admitted");
        var missing = manager.CreateEntity();
        chunk.Entities = new[] { first, missing };
        bool missingRejected = false;
        try { chunk.GetBufferAccessor(ref handle); } catch (InvalidOperationException) { missingRejected = true; }
        Need(missingRejected, "Missing required buffer was treated as empty");
        chunk.Count = 1;
        bool shapeRejected = false;
        try { chunk.GetBufferAccessor(ref handle); } catch (NotSupportedException) { shapeRejected = true; }
        Need(shapeRejected, "Mismatched entity count was admitted");
        CheckOwner(manager);
        Console.WriteLine("[MaterialsRecoveryReadOnlyBuffersControl] result=Passed scope=SourceFixture_NotNativeOrMissionReadiness");
    }

    private static void CheckOwner(EntityManager manager)
    {
        var catalogEntity = manager.CreateEntity();
        var catalog = manager.AddBuffer<BuildingConfiguredSpawnableReadModel>(catalogEntity);
        catalog.Add(new BuildingConfiguredSpawnableReadModel { BuildingId = new FixedString128Bytes("alpha"), CanRequest = 1, MaterialsCost = 30 });
        catalog.Add(new BuildingConfiguredSpawnableReadModel { BuildingId = new FixedString128Bytes("beta"), CanRequest = 1, MaterialsCost = 50 });
        var plan = manager.CreateEntity();
        manager.AddComponentData(plan, new AIBuildPlan { FactionId = 2, Enabled = 1 });
        var entries = manager.AddBuffer<AIBuildPlanEntry>(plan);
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("alpha") });
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("beta") });
        var foreign = manager.CreateEntity();
        manager.AddComponentData(foreign, new AIBuildPlan { FactionId = 1, Enabled = 1 });
        manager.AddBuffer<AIBuildPlanEntry>(foreign).Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("missing") });
        var owner = new MaterialsScenarioRecoveryStartupSystemHelper(manager);
        var method = typeof(MaterialsScenarioRecoveryStartupSystemHelper).GetMethod("TryResolveAIConstructionDemand", BindingFlags.NonPublic | BindingFlags.Instance);
        object[] args = { (byte)2, catalog, true, 0, 0, null };
        Need((bool)method.Invoke(owner, args) && (int)args[3] == 50 && (int)args[4] == 80,
            "Actual recovery owner did not align or sum AI plan entries");
        entries.Add(new AIBuildPlanEntry { BuildingId = new FixedString64Bytes("missing") });
        args = new object[] { (byte)2, catalog, true, 0, 0, null };
        Need(!(bool)method.Invoke(owner, args) && args[5].ToString() == "MissingConstructionDefinition" &&
             owner.LastInvalidConstructionId.ToString() == "missing", "Missing building did not fail closed");
        entries.RemoveAt(2);
        manager.SetComponentData(foreign, new AIBuildPlan { FactionId = 2, Enabled = 1 });
        manager.GetBuffer<AIBuildPlanEntry>(foreign).Clear();
        args = new object[] { (byte)2, catalog, true, 0, 0, null };
        Need(!(bool)method.Invoke(owner, args) && args[5].ToString() == "InvalidConstructionPlan" &&
             owner.LastInvalidConstructionId.ToString() == "duplicate-plan", "Duplicate AI plan was admitted");
    }
}
