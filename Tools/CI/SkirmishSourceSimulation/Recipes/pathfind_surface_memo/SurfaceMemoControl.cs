using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game.Components;
using Game.Runtime.Pathfinding;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

internal sealed class RequestMemo
{
    readonly MapSurfaceTraversalValidation validation = new MapSurfaceTraversalValidation();
    readonly MapSurfaceComponent surface;
    readonly byte hasData;
    readonly GridConfig grid;
    readonly int2 footprint;
    readonly bool vehicle;
    readonly MapSurfaceMovementMask mask;
    readonly Dictionary<int2, bool> cells = new Dictionary<int2, bool>();
    readonly Dictionary<int2, bool> footprints = new Dictionary<int2, bool>();
    internal int CellHits, FootprintHits, Mismatches;

    // Models one Execute: the surface/grid and request's movement/footprint inputs stay fixed.
    internal RequestMemo(MapSurfaceComponent s, byte data, GridConfig g, int2 f, bool v, MapSurfaceMovementMask m)
    { surface=s; hasData=data; grid=g; footprint=f; vehicle=v; mask=m; }

    internal bool Cell(int2 p, bool shadow)
    {
        bool actual;
        if (cells.TryGetValue(p, out bool cached))
        {
            CellHits++; actual=validation.CanTraverse(surface,hasData,p,mask);
            if(shadow&&actual!=cached){Mismatches++;throw new InvalidOperationException("cell memo mismatch");}
            return shadow?actual:cached;
        }
        actual=validation.CanTraverse(surface,hasData,p,mask); cells[p]=actual; return actual;
    }

    internal bool Footprint(int2 p, bool shadow)
    {
        bool actual;
        if (footprints.TryGetValue(p, out bool cached))
        {
            FootprintHits++; actual=validation.CanTraverseFootprint(surface,hasData,grid,p,footprint,vehicle);
            if(shadow&&actual!=cached){Mismatches++;throw new InvalidOperationException("footprint memo mismatch");}
            return shadow?actual:cached;
        }
        actual=validation.CanTraverseFootprint(surface,hasData,grid,p,footprint,vehicle); footprints[p]=actual; return actual;
    }
}

internal static class SurfaceMemoControlProgram
{
    static void Need(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    public static int Main()
    {
        const int w=16,h=10; var blob=CreateSurface(w,h);
        try
        {
            var surface=new MapSurfaceComponent{SurfaceBlob=blob,Dimensions=new int2(w,h),HasSurfaceData=1,CellSize=1};
            var grid=new GridConfig{Width=w,Height=h,CellSize=1}; var oracle=new MapSurfaceTraversalValidation();
            var masks=new[]{MapSurfaceMovementMask.None,MapSurfaceMovementMask.Infantry,
                MapSurfaceMovementMask.WheeledVehicle|MapSurfaceMovementMask.TrackedVehicle,
                MapSurfaceMovementMask.AirGrounded,MapSurfaceMovementMask.AllGroundUnits};
            int cells=0,cellHits=0,footprints=0,footprintHits=0;
            foreach(var mask in masks)
            {
                var memo=new RequestMemo(surface,1,grid,new int2(1),false,mask);
                for(int n=0;n<2;n++)for(int z=-1;z<=h;z++)for(int x=-1;x<=w;x++)
                {var p=new int2(x,z);bool expected=oracle.CanTraverse(surface,1,p,mask);Need(memo.Cell(p,true)==expected,"cell parity");cells++;}
                cellHits+=memo.CellHits;
            }
            var sizes=new[]{new int2(1),new int2(2,3),new int2(0,2),new int2(40,40)};
            foreach(bool vehicle in new[]{false,true})foreach(var size in sizes)
                {
                    var memo=new RequestMemo(surface,1,grid,size,vehicle,MapSurfaceMovementMask.Infantry);
                    for(int n=0;n<2;n++)for(int z=-1;z<=h;z++)for(int x=-1;x<=w;x++)
                    {var p=new int2(x,z);bool expected=oracle.CanTraverseFootprint(surface,1,grid,p,size,vehicle);Need(memo.Footprint(p,true)==expected,"footprint parity");footprints++;}
                    footprintHits+=memo.FootprintHits;
                }
            foreach(var missingData in new[]{(byte)0,(byte)1})
            {
                var s=missingData==0?default:new MapSurfaceComponent{HasSurfaceData=1,Dimensions=new int2(w,h)};
                var memo=new RequestMemo(s,missingData,grid,new int2(2),true,MapSurfaceMovementMask.Infantry);
                bool expected=oracle.CanTraverse(s,missingData,new int2(-1,0),MapSurfaceMovementMask.Infantry);
                Need(memo.Cell(new int2(-1,0),true)==expected,"missing/invalid blob semantics");
                Need(memo.Cell(new int2(-1,0),true)==expected,"missing/invalid blob cache semantics");cells+=2;cellHits+=memo.CellHits;
                expected=oracle.CanTraverseFootprint(s,missingData,grid,new int2(w-1,h-1),new int2(2),true);
                Need(memo.Footprint(new int2(w-1,h-1),true)==expected,"missing/invalid footprint semantics");
                Need(memo.Footprint(new int2(w-1,h-1),true)==expected,"missing/invalid footprint cache semantics");footprints+=2;footprintHits+=memo.FootprintHits;
            }
            var timed=new RequestMemo(surface,1,grid,new int2(1),false,MapSurfaceMovementMask.Infantry);
            var points=new List<int2>();for(int z=0;z<h;z++)for(int x=0;x<w;x++)points.Add(new int2(x,z));
            foreach(var p in points)timed.Cell(p,false);
            const int passes=250;long expectedCount=0,actualCount=0;var sw=Stopwatch.StartNew();
            for(int n=0;n<passes;n++)foreach(var p in points)if(oracle.CanTraverse(surface,1,p,MapSurfaceMovementMask.Infantry))expectedCount++;
            sw.Stop();double uncachedMs=sw.Elapsed.TotalMilliseconds;sw.Restart();
            for(int n=0;n<passes;n++)foreach(var p in points)if(timed.Cell(p,false))actualCount++;
            sw.Stop();double memoMs=sw.Elapsed.TotalMilliseconds;
            Need(expectedCount==actualCount&&timed.CellHits==points.Count*passes,"timing parity/hits");
            Need(footprintHits>0&&cellHits>0,"repeated query hit evidence");
            var report=new JObject{["kind"]="S026SurfaceMemoStandaloneControl",["status"]="Passed",
                ["scope"]="Standalone managed control calling original MapSurfaceTraversalValidation; no path job/gameplay/placement/mission",
                ["cell_cases"] = cells,["cell_shadow_hits"] = cellHits,["footprint_cases"] = footprints,
                ["footprint_shadow_hits"] = footprintHits,["mismatches"] = 0,
                ["movement_masks"] = new JArray(Array.ConvertAll(masks,x=>x.ToString())),
                ["footprint_sizes"] = new JArray(Array.ConvertAll(sizes,x=>x.ToString())),
                ["timing"] = new JObject{["uncached_ms"] = uncachedMs,["memo_lookup_ms"] = memoMs,
                    ["query_count"] = points.Count*passes,["hits"] = timed.CellHits,["misses"] = points.Count,
                    ["result_count"] = actualCount}};
            Console.WriteLine(report.ToString(Newtonsoft.Json.Formatting.None));return 0;
        }
        finally{blob.Dispose();}
    }

    static BlobAssetReference<MapSurfaceBlob> CreateSurface(int w,int h)
    {
        using(var b=new BlobBuilder(Allocator.Temp))
        {
            ref MapSurfaceBlob root=ref b.ConstructRoot<MapSurfaceBlob>();root.Dimensions=new int2(w,h);root.CellSize=1;
            root.RuntimeEncoding=MapSurfaceRuntimeEncoding.Full;var cells=b.Allocate(ref root.Cells,w*h);
            var samples=b.Allocate(ref root.Samples,w*h);b.Allocate(ref root.Connections,0);b.Allocate(ref root.CompactSamples,0);
            for(int i=0;i<w*h;i++)
            {
                cells[i]=new MapSurfaceCell{FirstSurfaceIndex=i,SurfaceCount=1};
                samples[i]=new MapSurfaceSample{Cell=new int2(i%w,i/w),SurfaceId=i,
                    MovementMask=i%4==0?MapSurfaceMovementMask.Infantry:MapSurfaceMovementMask.AllGroundUnits,
                    SurfaceType=i%9==0?MapSurfaceType.Blocked:i%7==0?MapSurfaceType.Road:MapSurfaceType.Terrain,
                    Flags=i%11==0?MapSurfaceFlags.Reserved:MapSurfaceFlags.None,SlopeDegrees=i%5==0?55f:i%3==0?20f:0f};
            }
            return b.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
        }
    }
}
