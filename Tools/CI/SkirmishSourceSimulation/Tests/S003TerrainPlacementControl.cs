using System;
using Game.Components;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
internal static class S003TerrainPlacementControl {
 public static int Main(){try{
  int next=1;var em=new EntityManager{LogicalAllocatorAdmitted=true};em.AllocateLogicalIdentity=()=>new Entity{Index=next++,Version=1};
  var grid=em.CreateEntity();var config=new GridConfig{Width=4,Height=4,CellSize=1f,Origin=float3.zero};em.AddComponentData(grid,config);
  var roads=em.AddBuffer<GridRoad>(grid);var walkable=em.AddBuffer<GridWalkable>(grid);
  for(int i=0;i<16;i++){roads.Add(new GridRoad{Value=0});walkable.Add(new GridWalkable{Value=(byte)(i==5?0:1)});}
  var bits=new NativeBitArray(16,Allocator.Persistent,NativeArrayOptions.ClearMemory);
  em.AddComponentData(grid,new DynamicBlockerComponent{GridSize=16,Blocked=bits});
  var prefab=em.CreateEntity();var definition=new RuntimeDefinition{Prefab=new SourceUnitPrefab("Building_GuardTower",prefab),SourceMetadataComplete=true,FootprintCells=new Vector2Int(1,1)};
  var owner=new SourceGuardTowerPlacementValidation(em,grid,new GeneratedBuildingMask(config,new bool[16]),new GeneratedRoadMask(config,new bool[16],new bool[16],new bool[16]),(x,y,w,h)=>false,(g,o,s)=>false,rect=>false,(d,o,g,r)=>new RectInt(o,d.FootprintCells));
  bool open=owner.IsPlacementValid(definition,new Vector2Int(0,0),definition.FootprintCells,false);bool blocked=owner.IsPlacementValid(definition,new Vector2Int(1,1),definition.FootprintCells,false);bool outside=owner.IsPlacementValid(definition,new Vector2Int(4,0),definition.FootprintCells,false);
  if(!open||outside)throw new InvalidOperationException("Open/bounds controls failed");
  Console.WriteLine("[S003TerrainPlacementControl] result=Observed open="+(open?1:0)+" blocked="+(blocked?1:0)+" outside="+(outside?1:0)+" fixtureOnly=1 readiness=0");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
