using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal static class BuildingSurfaceFootprintControls
{
    private static int checks;
    private static void Require(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException(message);
    }

    private static MapSurfaceComponent Full(float firstHeight, float secondHeight, float secondSlope,
        int firstLayer, int secondLayer, bool missingSecond = false)
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.Dimensions = new int2(2, 1); root.CellSize = 1; root.RuntimeEncoding = MapSurfaceRuntimeEncoding.Full;
        var cells = builder.Allocate(ref root.Cells, 2);
        var samples = builder.Allocate(ref root.Samples, 2);
        builder.Allocate(ref root.Connections, 0); builder.Allocate(ref root.CompactSamples, 0);
        cells[0] = new MapSurfaceCell { FirstSurfaceIndex = 0, SurfaceCount = 1 };
        cells[1] = new MapSurfaceCell { FirstSurfaceIndex = 1, SurfaceCount = (ushort)(missingSecond ? 0 : 1) };
        samples[0] = new MapSurfaceSample { Cell = new int2(0, 0), SurfaceId = 101, LayerId = firstLayer,
            Height = firstHeight, SlopeDegrees = 0, Normal = new float3(0, 1, 0) };
        samples[1] = new MapSurfaceSample { Cell = new int2(1, 0), SurfaceId = 202, LayerId = secondLayer,
            Height = secondHeight, SlopeDegrees = secondSlope, Normal = new float3(0, 1, 0) };
        return new MapSurfaceComponent { SurfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent),
            Dimensions = root.Dimensions, CellSize = 1, HasSurfaceData = 1 };
    }

    private static MapSurfaceComponent Compact(short firstLayer, short secondLayer)
    {
        using var builder = new BlobBuilder(Allocator.Temp);
        ref MapSurfaceBlob root = ref builder.ConstructRoot<MapSurfaceBlob>();
        root.Dimensions = new int2(2, 1); root.CellSize = 1; root.RuntimeEncoding = MapSurfaceRuntimeEncoding.SingleLayerCompact;
        root.CompactMinHeight = 2; root.CompactHeightStep = 0.1f;
        builder.Allocate(ref root.Cells, 0); builder.Allocate(ref root.Samples, 0); builder.Allocate(ref root.Connections, 0);
        var samples = builder.Allocate(ref root.CompactSamples, 2);
        samples[0] = new MapSurfaceCompactSample { LayerId = firstLayer, PackedHeight = 0, NormalY = 127 };
        samples[1] = new MapSurfaceCompactSample { LayerId = secondLayer, PackedHeight = 1, NormalY = 127 };
        return new MapSurfaceComponent { SurfaceBlob = builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent),
            Dimensions = root.Dimensions, CellSize = 1, HasSurfaceData = 1 };
    }

    private static BuildingSurfacePlacementUtilitySystemHelper.Result Evaluate(
        MapSurfaceComponent surface, float maxHeight = 0.5f, float maxSlope = 45f)
    {
        var owner = new BuildingSurfacePlacementUtilitySystemHelper();
        Require(owner.TryEvaluateFootprint(surface, new Vector2Int(0, 0), new Vector2Int(2, 1),
            maxHeight, maxSlope, out var result), "Complete footprint must produce a source result");
        return result;
    }

    private static void Run()
    {
        var full = Full(2f, 2.1f, 3f, 7, 7);
        try
        {
            var result = Evaluate(full);
            Require(result.IsValid && result.SampleCount == 2 && result.SurfaceId == 101 && result.LayerId == 7,
                "Full encoding must admit distinct sample IDs on the same layer and retain the first ID");
            Require(Math.Abs(result.FoundationHeight - 2.05f) < 0.00001f &&
                Math.Abs(result.MaxFootprintHeightDelta - 0.1f) < 0.00001f && result.MaxFootprintSlopeDegrees == 3f,
                "Full encoding must aggregate every sample's height and slope");
            var component = new BuildingSurfacePlacementUtilitySystemHelper().ToComponent(result);
            Require(component.SurfaceId == 101 && component.LayerId == 7 && component.IsPlacementSurfaceValid == 1,
                "Published building surface must preserve the first sample identity");
        }
        finally { full.SurfaceBlob.Dispose(); }

        var compact = Compact(7, 7);
        try
        {
            var result = Evaluate(compact);
            Require(result.IsValid && result.SampleCount == 2 && result.SurfaceId == 0 && result.LayerId == 7,
                "Compact encoding must admit per-cell IDs on the same layer and retain the first ID");
            Require(Math.Abs(result.FoundationHeight - 2.05f) < 0.00001f &&
                Math.Abs(result.MaxFootprintHeightDelta - 0.1f) < 0.00001f,
                "Compact encoding must aggregate all decoded heights");
        }
        finally { compact.SurfaceBlob.Dispose(); }

        foreach (bool useCompact in new[] { false, true })
        {
            var surface = useCompact ? Compact(7, 8) : Full(2f, 2.1f, 0f, 7, 8);
            try
            {
                var result = Evaluate(surface);
                Require(!result.IsValid && result.SampleCount == 1 && result.SurfaceId == (useCompact ? 0 : 101) &&
                    result.LayerId == 7 && result.FoundationHeight == 2f && result.MaxFootprintHeightDelta == 0f,
                    "A different layer must reject at the original first mismatch and retain prefix results");
            }
            finally { surface.SurfaceBlob.Dispose(); }
        }

        var high = Full(2f, 2.6f, 0f, 7, 7);
        try { var result = Evaluate(high); Require(!result.IsValid && result.SampleCount == 2 && result.MaxFootprintHeightDelta > 0.5f,
            "Height limit must still reject after the complete same-layer footprint"); }
        finally { high.SurfaceBlob.Dispose(); }
        var steep = Full(2f, 2f, 46f, 7, 7);
        try { var result = Evaluate(steep); Require(!result.IsValid && result.SampleCount == 2 && result.MaxFootprintSlopeDegrees == 46f,
            "Slope limit must still reject after the complete same-layer footprint"); }
        finally { steep.SurfaceBlob.Dispose(); }
        var missing = Full(2f, 2f, 0f, 7, 7, true);
        try
        {
            var owner = new BuildingSurfacePlacementUtilitySystemHelper();
            Require(!owner.TryEvaluateFootprint(missing, new Vector2Int(0, 0), new Vector2Int(2, 1), 0.5f, 45f, out _),
                "A missing primary sample must still reject without a result");
        }
        finally { missing.SurfaceBlob.Dispose(); }
    }

    private static int Main()
    {
        try { Run(); Console.WriteLine("[BuildingSurfaceFootprintControls] result=Passed checks=" + checks); return 0; }
        catch (Exception error) { Console.Error.WriteLine("[BuildingSurfaceFootprintControls] result=Failed " + error); return 1; }
    }
}
