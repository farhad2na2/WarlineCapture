using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
internal static class S003SurfacePlacementIntegrationControl {
 static void Need(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
 static void Rejected(Action action,string name){try{action();throw new Exception("Missing rejection: "+name);}catch(NotSupportedException){}}
 public static int Main(){try{
  int next=1;var em=new EntityManager{LogicalAllocatorAdmitted=true};em.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
  var grid=em.CreateEntity();var config=new GridConfig{Width=5,Height=5,CellSize=1f,Origin=float3.zero};em.AddComponentData(grid,config);
  var roads=em.AddBuffer<GridRoad>(grid);var walk=em.AddBuffer<GridWalkable>(grid);for(int i=0;i<25;i++){roads.Add(new GridRoad{Value=0});walk.Add(new GridWalkable{Value=(byte)(i==6?0:1)});}
  var bits=new NativeBitArray(25,Allocator.Persistent,NativeArrayOptions.ClearMemory);em.AddComponentData(grid,new DynamicBlockerComponent{GridSize=25,Blocked=bits});
  var prefab=em.CreateEntity();var definition=new RuntimeDefinition{Prefab=new SourceUnitPrefab("Building_GuardTower",prefab),SourceMetadataComplete=true,FootprintCells=new Vector2Int(2,2)};
  var owner=new SourceGuardTowerPlacementValidation(em,grid,new GeneratedBuildingMask(config,new bool[25]),new GeneratedRoadMask(config,new bool[25],new bool[25],new bool[25]),(x,y,w,h)=>false,(g,o,s)=>false,rect=>false,(d,o,g,r)=>new RectInt(o,d.FootprintCells));
  bool Valid(int x,int y)=>owner.IsPlacementValid(definition,new Vector2Int(x,y),definition.FootprintCells,false);
  Need(Valid(3,3),"pre-match fixture rejected");
  var match=em.CreateEntity();em.AddComponentData(match,new SkirmishMatchState{Phase=SkirmishPhase.Playing});
  Need(!Valid(3,3),"missing surface accepted during Playing");
  using var builder=new BlobBuilder(Allocator.Temp);ref var root=ref builder.ConstructRoot<MapSurfaceBlob>();
  root.Dimensions=new int2(5,5);root.GridOrigin=float3.zero;root.CellSize=1;root.RuntimeEncoding=MapSurfaceRuntimeEncoding.Full;
  var cells=builder.Allocate(ref root.Cells,25);var samples=builder.Allocate(ref root.Samples,25);builder.Allocate(ref root.Connections,0);builder.Allocate(ref root.CompactSamples,0);
  for(int i=0;i<25;i++){cells[i]=new MapSurfaceCell{FirstSurfaceIndex=i,SurfaceCount=1};samples[i]=new MapSurfaceSample{Cell=new int2(i%5,i/5),SurfaceId=100+i,LayerId=0,Height=i==18?1f:0f,SlopeDegrees=i==12?9f:0f};}
  var blob=builder.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);
  try{
   var surface=em.CreateEntity();em.AddComponentData(surface,new MapSurfaceComponent{SurfaceBlob=blob,Dimensions=new int2(5,5),CellSize=1,HasSurfaceData=1});
   Need(Valid(0,2),"flat footprint rejected");Need(!Valid(2,2),"steep footprint accepted");Need(!Valid(3,3),"elevated footprint accepted");
   var second=em.CreateEntity();em.AddComponentData(second,new MapSurfaceComponent{SurfaceBlob=blob,Dimensions=new int2(5,5),CellSize=1,HasSurfaceData=1});
   Need(!Valid(0,2),"multiple surfaces accepted");
  }finally{blob.Dispose();}
  Console.WriteLine("[S003SurfacePlacementIntegrationControl] result=Passed flat=1 steep=1 elevated=1 missingSurface=1 multipleSurfaces=1 fixtureOnly=1 readiness=0");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
