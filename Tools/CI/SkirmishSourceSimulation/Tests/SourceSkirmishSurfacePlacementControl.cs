using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
internal static class SourceSkirmishSurfacePlacementControl {
 static void Need(bool value,string message){if(!value)throw new InvalidOperationException(message);}
 public static int Main(){try{
  using var builder=new BlobBuilder(Allocator.Temp);ref var root=ref builder.ConstructRoot<MapSurfaceBlob>();
  root.Dimensions=new int2(8,1);root.GridOrigin=float3.zero;root.CellSize=1;root.RuntimeEncoding=MapSurfaceRuntimeEncoding.Full;
  var cells=builder.Allocate(ref root.Cells,8);var samples=builder.Allocate(ref root.Samples,8);builder.Allocate(ref root.Connections,0);builder.Allocate(ref root.CompactSamples,0);
  float[] heights={0,0,1,0,0.6f,float.NaN,0.75f,0};float[] slopes={0,9,0,0,0,0,0,8};
  for(int i=0;i<8;i++){cells[i]=new MapSurfaceCell{FirstSurfaceIndex=i,SurfaceCount=1};samples[i]=new MapSurfaceSample{Cell=new int2(i,0),SurfaceId=100+i,LayerId=0,Height=heights[i],SlopeDegrees=slopes[i]};}
  var blob=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);var surface=new MapSurfaceComponent{SurfaceBlob=blob,Dimensions=new int2(8,1),CellSize=1,HasSurfaceData=1};
  try{
   bool Check(int x,int width)=>SourceSkirmishSurfacePlacementOwner.IsSkirmishFootprintLevel(surface,new RectInt(x,0,width,1));
   Need(Check(0,1),"flat rejected");Need(!Check(1,1),"steep accepted");Need(!Check(2,1),"high plateau accepted");Need(!Check(3,2),"foundation delta accepted");Need(!Check(5,1),"nonfinite accepted");Need(Check(6,1),"elevation threshold rejected");Need(Check(7,1),"slope threshold rejected");Need(!Check(8,1),"bounds accepted");surface.HasSurfaceData=0;Need(!Check(0,1),"missing data accepted");
  }finally{blob.Dispose();}
  Console.WriteLine("[SourceSkirmishSurfacePlacementControl] result=Passed flat=1 steep=1 elevated=1 delta=1 nonfinite=1 thresholds=1 bounds=1 missing=1 fixtureOnly=1 readiness=0");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
