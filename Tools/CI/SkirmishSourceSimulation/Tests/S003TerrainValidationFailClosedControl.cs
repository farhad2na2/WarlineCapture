using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
internal static class S003TerrainValidationFailClosedControl {
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
  Need(Valid(3,3),"open footprint rejected");Need(!Valid(0,0),"partially blocked footprint accepted");Need(!Valid(4,4),"out-of-bounds footprint accepted");
  walk[6]=new GridWalkable{Value=2};Rejected(()=>Valid(0,0),"nonbinary");em.RemoveComponent<GridWalkable>(grid);Rejected(()=>Valid(0,0),"missing");
  var shortBuffer=em.AddBuffer<GridWalkable>(grid);shortBuffer.Add(new GridWalkable{Value=1});Rejected(()=>Valid(0,0),"short");
  Console.WriteLine("[S003TerrainValidationFailClosedControl] result=Passed open=1 partialBlocked=1 bounds=1 nonbinary=1 missing=1 short=1 fixtureOnly=1 readiness=0");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
