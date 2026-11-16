using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Runs the compiled S073 and strict S002 source-clearance adapters against one
// freshly-created shim grid. It does not consume the S073 startup bundle or
// invoke any queue, delivery owner, scheduler, or mission admission path.
internal static class S073ClearanceDifferentialScratchControl
{
    private const int Width = 2048;
    private const int Height = 1024;

    private static void Need(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("S073 clearance differential fixture: " + message);
    }

    internal static void Main(string[] args)
    {
        Need(args.Length == 1, "repository root argument required");
        string repositoryRoot = System.IO.Path.GetFullPath(args[0]);
        var em = new EntityManager();
        int nextId = 1;
        em.LogicalAllocatorAdmitted = true;
        em.AllocateLogicalIdentity = () => new Entity { Index = nextId++, Version = 1 };
        Entity gridEntity = em.CreateEntity();
        var grid = new GridConfig { Width = Width, Height = Height, CellSize = 1f, Origin = float3.zero };
        em.AddComponentData(gridEntity, grid);
        var walkable = em.AddBuffer<GridWalkable>(gridEntity);
        for (int i = 0; i < Width * Height; i++) walkable.Add(new GridWalkable { Value = 1 });
        em.AddComponentData(gridEntity, new DynamicBlockerComponent { GridSize = Width * Height });
        em.AddComponentData(gridEntity, new DynamicOccupancyComponent { GridSize = Width * Height });

        var world = new SourceProductionGroundWorld(em, gridEntity, Entity.Null,
            new Dictionary<int, RuntimeBuildingEntity>(), new Dictionary<int, SourceProducerRootFrame>());
        var adapter = S073SourceClearanceProjectionAdapter.Load(repositoryRoot, grid);
        JObject result = adapter.CompareAgainstS002(world, repositoryRoot);
        Need((string)result["status"] == "Passed" && (bool?)result["compiled_differential_executed"] == true &&
             (bool?)result["same_source_world"] == true && (bool?)result["full_masks_compared"] == true &&
             (bool?)result["delivery_invoked"] == false && (bool?)result["mission_ready"] == false,
             "compiled differential result or diagnostic-only scope was not preserved");
        result["kind"] = "S073ClearanceDifferentialScratchControl";
        result["schema_version"] = 1;
        result["fixture_world"] = "fresh shim EntityManager; exact 2048x1024 source grid; empty building/root dictionaries";
        result["startup_bundle_consumed"] = false;
        result["native_runtime_state_consumed"] = false;
        result["scheduler_admitted"] = false;
        result["queue_or_delivery_owner_invoked"] = false;
        result["callback_invoked"] = false;
        Console.WriteLine("[S073ClearanceDifferentialScratchControl] " + result.ToString(Newtonsoft.Json.Formatting.None));
    }
}
