extern alias ActualCore;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Game.Components;
using Game.Runtime;
using Game.Rendering.Contracts;
using Game.Rendering.SourceHost;
using Bounds = ActualCore::UnityEngine.Bounds;
using Rect = ActualCore::UnityEngine.Rect;

internal sealed class CapturedProjectionProvider : IRenderBoundsPresentation
{
    private readonly EntityManager manager;
    private readonly ExtractedOriginalStaticRenderBoundsOwner collector;
    internal CapturedProjectionProvider(EntityManager manager, ExtractedOriginalStaticRenderBoundsOwner collector)
    { this.manager = manager; this.collector = collector; }
    public bool TryReadWorldBounds(EntityManager em, Entity entity, out Bounds bounds)
    { bounds = default; return false; }
    public void CollectStaticBounds(EntityManager em, Rect area, float minExtent, float maxExtent,
        ref NativeList<Bounds> results) => collector.CollectStaticBounds(manager, area, minExtent, maxExtent, ref results);
    public int CountAuthoredBuildingGeometry(EntityManager em) => 0;
    public void CollectAuthoredBuildingBounds(EntityManager em, ref NativeList<Bounds> results) { }
    public void CollectWaterFootprints(EntityManager em, ref NativeList<RenderSurfaceFootprint> results) { }
}

internal static class SourceStaticRenderReserveMaskComparisonFixture
{
    private const int Width = 2048, Height = 1024;
    private static void Need(bool ok, string message)
    { if (!ok) throw new InvalidOperationException("Reserve mask comparison: " + message); }

    private static float F(JToken token) => token.Value<float>();
    private static void ImportBounds(EntityManager em, int index, JToken row)
    {
        var center = (JArray)row["center"]; var extents = (JArray)row["extents"];
        Need(center?.Count == 3 && extents?.Count == 3, "Captured center/extents triple required");
        var entity = new Entity { Index = index, Version = 1 };
        em.ImportEntity(entity);
        em.AddComponentData(entity, new WorldRenderBounds { Value = new AABB {
            Center = new float3(F(center[0]), F(center[1]), F(center[2])),
            Extents = new float3(F(extents[0]), F(extents[1]), F(extents[2])) } });
    }

    private static NativeBitArray NewMask()
    {
        var mask = new NativeBitArray(checked(Width * Height), Allocator.Temp);
        for (int i = 0; i < mask.Length; i++) mask.Set(i, false);
        return mask;
    }

    public static int Main(string[] args)
    {
        EntityManager source = null;
        NativeBitArray originalMask = default, adapterMask = default;
        try
        {
            Need(args.Length == 2, "repository root and captured projection path required");
            string root = Path.GetFullPath(args[0]);
            JObject projection = JObject.Parse(File.ReadAllText(args[1]));
            JArray rows = projection["source_record"]?["bounds"] as JArray;
            Need((string)projection["kind"] == "GeneratedSourceStaticClearanceProjection" && rows?.Count == 90865,
                "Pinned 90,865 source-bounds projection required");

            source = new EntityManager();
            for (int i = 0; i < rows.Count; i++) ImportBounds(source, i + 1, rows[i]);
            int nextEntity = rows.Count + 1;
            source.LogicalAllocatorAdmitted = true;
            source.AllocateLogicalIdentity = () => new Entity { Index = nextEntity++, Version = 1 };
            var grid = new GridConfig { Width = Width, Height = Height, CellSize = 1f, Origin = float3.zero };
            var gridEntity = source.CreateEntity();
            source.AddComponentData(gridEntity, grid);
            var walkable = source.AddBuffer<GridWalkable>(gridEntity);
            for (int i = 0; i < Width * Height; i++) walkable.Add(new GridWalkable { Value = 1 });
            source.AddComponentData(gridEntity, new DynamicBlockerComponent { GridSize = Width * Height });
            source.AddComponentData(gridEntity, new DynamicOccupancyComponent { GridSize = Width * Height });

            var collector = new ExtractedOriginalStaticRenderBoundsOwner();
            RenderBoundsGateway.Bind(new CapturedProjectionProvider(source, collector));
            var sourceWorld = new SourceProductionGroundWorld(source, gridEntity, Entity.Null,
                new Dictionary<int, RuntimeBuildingEntity>(), new Dictionary<int, SourceProducerRootFrame>());
            var s073 = S073SourceClearanceProjectionAdapter.Load(root, grid);
            var preferredCells = new[] {
                new int2(1001, 430), // Previously reported natural native preferred cell.
                new int2(999, 428),  // Nearby previously reported natural native preferred cell.
                new int2(1024, 512), // Safe interior control.
                new int2(-20, -20),   // Search window begins exactly at the captured query's lower edge.
                new int2(2067, 1043), // Search window ends exactly at the captured query's upper edge.
                new int2(0, 0),       // Grid lower corner.
                new int2(Width - 1, Height - 1) // Grid upper corner.
            };
            int totalReserved = 0;
            for (int cellIndex = 0; cellIndex < preferredCells.Length; cellIndex++)
            {
                var preferred = preferredCells[cellIndex];
                originalMask = NewMask();
                adapterMask = NewMask();
                ExtractedOriginalDropBufferReservation.ReserveStaticRenderDropBuffers(source, ref originalMask, gridEntity, grid,
                    source.GetComponentData<DynamicBlockerComponent>(gridEntity), true, preferred);
                s073.Reserve(sourceWorld, preferred, ref adapterMask);
                int different = 0, originalSet = 0, adapterSet = 0, first = -1;
                for (int i = 0; i < checked(Width * Height); i++)
                {
                    bool a = originalMask.IsSet(i), b = adapterMask.IsSet(i);
                    if (a) originalSet++;
                    if (b) adapterSet++;
                    if (a == b) continue;
                    if (first < 0) first = i;
                    different++;
                }
                Need(different == 0, "Full mask differs at preferred=" + preferred + ": cells=" + different + " first=" + first);
                totalReserved += originalSet;
                Console.WriteLine("[SourceStaticRenderReserveMaskComparisonFixture] cell=" + preferred +
                    " original_reserved=" + originalSet + " s073_reserved=" + adapterSet + " differing_cells=0");
                originalMask.Dispose(); originalMask = default;
                adapterMask.Dispose(); adapterMask = default;
            }
            Console.WriteLine("[SourceStaticRenderReserveMaskComparisonFixture] result=Passed input_bounds=" + rows.Count +
                " preferred_count=" + preferredCells.Length + " total_reserved=" + totalReserved +
                " mask_cells=" + checked(Width * Height) + " differing_cells=0 provider_parity=false mission_ready=false");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("[SourceStaticRenderReserveMaskComparisonFixture] result=Failed error=" + error);
            return 1;
        }
        finally
        {
            RenderBoundsGateway.Bind(null);
            if (originalMask.IsCreated) originalMask.Dispose();
            if (adapterMask.IsCreated) adapterMask.Dispose();
        }
    }
}
