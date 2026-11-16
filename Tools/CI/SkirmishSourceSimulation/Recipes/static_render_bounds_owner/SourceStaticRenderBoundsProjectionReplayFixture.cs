extern alias ActualCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Game.Components;
using Game.Rendering.SourceHost;
using Bounds = ActualCore::UnityEngine.Bounds;
using Rect = ActualCore::UnityEngine.Rect;

internal static class SourceStaticRenderBoundsProjectionReplayFixture
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static float Float(JToken token) => token.Value<float>();

    private static void AddBounds(EntityManager manager, int index, JToken row)
    {
        var center = (JArray)row["center"];
        var extents = (JArray)row["extents"];
        Need(center != null && center.Count == 3 && extents != null && extents.Count == 3,
            "Projection row must have three center/extents values");
        var entity = new Entity { Index = index, Version = 1 };
        manager.ImportEntity(entity);
        manager.AddComponentData(entity, new WorldRenderBounds
        {
            Value = new AABB
            {
                Center = new float3(Float(center[0]), Float(center[1]), Float(center[2])),
                Extents = new float3(Float(extents[0]), Float(extents[1]), Float(extents[2]))
            }
        });
    }

    private static string Key(Bounds value)
    {
        var min = value.min;
        var max = value.max;
        return string.Join(",", new[] {
            unchecked((uint)BitConverter.SingleToInt32Bits(min.x)).ToString(CultureInfo.InvariantCulture),
            unchecked((uint)BitConverter.SingleToInt32Bits(min.y)).ToString(CultureInfo.InvariantCulture),
            unchecked((uint)BitConverter.SingleToInt32Bits(min.z)).ToString(CultureInfo.InvariantCulture),
            unchecked((uint)BitConverter.SingleToInt32Bits(max.x)).ToString(CultureInfo.InvariantCulture),
            unchecked((uint)BitConverter.SingleToInt32Bits(max.y)).ToString(CultureInfo.InvariantCulture),
            unchecked((uint)BitConverter.SingleToInt32Bits(max.z)).ToString(CultureInfo.InvariantCulture)
        });
    }

    private static void AddExcludedControls(EntityManager manager)
    {
        AddControl(manager, 2, prefab: true);
        AddControl(manager, 3, disabled: true);
        AddControl(manager, 4, unit: true);
    }

    private static void AddControl(EntityManager manager, int index, bool prefab = false,
        bool disabled = false, bool unit = false)
    {
        var entity = new Entity { Index = index, Version = 1 };
        manager.ImportEntity(entity);
        manager.AddComponentData(entity, new WorldRenderBounds
        {
            Value = new AABB { Center = new float3(5f, 4f, 5f), Extents = new float3(1f, 0.5f, 1f) }
        });
        if (prefab) manager.AddComponentData(entity, new Prefab());
        if (disabled) manager.AddComponentData(entity, new Disabled());
        if (unit) manager.AddComponentData(entity, new UnitFootprint());
    }

    public static int Main(string[] args)
    {
        try
        {
            Need(args.Length == 2, "Expected captured projection path and independent reference path");
            var projectionRoot = JObject.Parse(File.ReadAllText(args[0]));
            var reference = JObject.Parse(File.ReadAllText(args[1]));
            var boundsRows = (JArray)projectionRoot["source_record"]?["bounds"];
            Need(boundsRows != null && boundsRows.Count == 90865,
                "Pinned full captured source projection bounds required");
            Need((string)reference["kind"] == "PinnedPregameSourceProjectionStaticBoundsReference" &&
                (int)reference["source_projection_bound_count"] == boundsRows.Count &&
                (int)reference["selected_bound_count"] == 2400,
                "Independent pinned source reference envelope differs");

            // Fresh disposable host world contains only synthetic geometry entities
            // representing the retained pregame source projection rows.
            var sourceWorld = new EntityManager();
            for (int i = 0; i < boundsRows.Count; i++) AddBounds(sourceWorld, i + 1, boundsRows[i]);
            var output = new NativeList<Bounds>(Allocator.Temp);
            var owner = new ExtractedOriginalStaticRenderBoundsOwner();
            var queryArea = new Rect(903f, 332f, 197f, 197f);
            owner.CollectStaticBounds(sourceWorld, queryArea, 0.25f, 20f, ref output);

            var actual = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var bounds in output)
            {
                string key = Key(bounds);
                actual.TryGetValue(key, out int count);
                actual[key] = count + 1;
            }
            var expected = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var row in (JArray)reference["bounds_multiset_bits_minmax"])
                expected.Add((string)row["key"], (int)row["count"]);
            Need(output.Length == 2400, "Original owner selected count differs: " + output.Length);
            Need(actual.Count == expected.Count, "Duplicate-aware unique bound key count differs");
            foreach (var row in expected)
                Need(actual.TryGetValue(row.Key, out int count) && count == row.Value,
                    "Actual original-owner multiset differs at key=" + row.Key);
            foreach (var row in actual)
                Need(expected.ContainsKey(row.Key), "Unexpected actual original-owner bound key=" + row.Key);

            // Separate synthetic world exercises the original query's None boundary.
            var controlWorld = new EntityManager();
            AddControl(controlWorld, 1);
            AddExcludedControls(controlWorld);
            var controlOutput = new NativeList<Bounds>(Allocator.Temp);
            owner.CollectStaticBounds(controlWorld, new Rect(0f, 0f, 10f, 10f), 0.25f, 1f,
                ref controlOutput);
            Need(controlOutput.Length == 1,
                "Original query must exclude synthetic Prefab, Disabled, and UnitFootprint entities");

            Console.WriteLine("[SourceStaticRenderBoundsProjectionReplayFixture] result=Passed source_projection_inputs=" +
                boundsRows.Count + " selected=" + output.Length + " unique=" + actual.Count +
                " source_duplicate_excess=" + ((int)reference["duplicate_excess_count"]) +
                " exclusion_controls=3 source_world_entities_synthetic=true native_state_imported=false " +
                "mission_world_admission=false current_source_fit=false readiness=false native_chunk_order_claim=false");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("[SourceStaticRenderBoundsProjectionReplayFixture] result=Failed error=" + error);
            return 1;
        }
    }
}
