extern alias ActualCore;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Game.Components;
using Game.Rendering.SourceHost;
using Bounds = ActualCore::UnityEngine.Bounds;
using Rect = ActualCore::UnityEngine.Rect;

internal static class SourceStaticRenderBoundsOwnerFixture
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Add(EntityManager manager, int index, float3 center, float3 extents,
        bool prefab = false, bool disabled = false, bool unit = false)
    {
        var entity = new Entity { Index = index, Version = 1 };
        manager.ImportEntity(entity);
        manager.AddComponentData(entity, new WorldRenderBounds
        {
            Value = new AABB { Center = center, Extents = extents }
        });
        if (prefab) manager.AddComponentData(entity, new Prefab());
        if (disabled) manager.AddComponentData(entity, new Disabled());
        if (unit) manager.AddComponentData(entity, new UnitFootprint());
    }

    public static int Main()
    {
        try
        {
            var manager = new EntityManager();
            // Query area is [0,10) on X/Z. The first source bound touches its
            // minimum edge and is retained by the original asymmetric inequalities.
            Add(manager, 1, new float3(-1f, 4f, 5f), new float3(1f, 0.5f, 1f));
            Add(manager, 2, new float3(5f, 4f, 5f), new float3(0.25f, 0.2f, 0.25f));
            Add(manager, 3, new float3(float.NaN, 4f, 5f), new float3(1f, 0.5f, 1f));
            Add(manager, 4, new float3(5f, 4f, 5f), new float3(0.24f, 0.2f, 0.24f));
            Add(manager, 5, new float3(5f, 4f, 5f), new float3(1.01f, 0.5f, 0.5f));
            Add(manager, 6, new float3(11f, 4f, 5f), new float3(1f, 0.5f, 1f));
            Add(manager, 7, new float3(5f, 4f, 5f), new float3(1f, 0.5f, 1f), prefab: true);
            Add(manager, 8, new float3(5f, 4f, 5f), new float3(1f, 0.5f, 1f), disabled: true);
            Add(manager, 9, new float3(5f, 4f, 5f), new float3(1f, 0.5f, 1f), unit: true);

            var results = new NativeList<Bounds>(Allocator.Temp);
            var owner = new ExtractedOriginalStaticRenderBoundsOwner();
            owner.CollectStaticBounds(manager, new Rect(0f, 0f, 10f, 10f), 0.25f, 1f, ref results);

            Need(results.Length == 2, "Expected two retained static bounds; got " + results.Length);
            Need(results[0].center.x == -1f && results[0].center.z == 5f,
                "Original source-host query order/first included bound changed");
            Need(results[0].size.x == 2f && results[0].size.y == 1f && results[0].size.z == 2f,
                "Original Bounds size construction changed");
            Need(results[1].center.x == 5f && results[1].size.x == 0.5f,
                "Inclusive minimum extent boundary was not retained");

            Console.WriteLine("[SourceStaticRenderBoundsOwnerFixture] result=Passed case_count=9 included=2 excluded=7 " +
                "source_host_world=true mission_run=false native_chunk_order_claim=false scene_admission=false readiness=false");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("[SourceStaticRenderBoundsOwnerFixture] result=Failed error=" + error);
            return 1;
        }
    }
}
