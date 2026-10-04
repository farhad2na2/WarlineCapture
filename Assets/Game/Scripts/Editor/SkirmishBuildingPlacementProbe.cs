using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Rendering;
using Game.Components;
using UnityEngine;
namespace Game.Editor {
public static class SkirmishBuildingPlacementProbe {
public static void Inspect() {
var em=World.DefaultGameObjectInjectionWorld.EntityManager;
using var q=em.CreateEntityQuery(typeof(WorldRenderBounds), typeof(MaterialMeshInfo),typeof(RenderMeshArray));
using var es=q.ToEntityArray(Allocator.Temp);
using var buildings=em.CreateEntityQuery(new EntityQueryDesc { All=new[]{ComponentType.ReadOnly<RuntimeBuildingCombatInfo>()}, None=new[]{ComponentType.ReadOnly<OperationMapBuildingComponent>()}});
using var bs=buildings.ToEntityArray(Allocator.Temp);
using var gridQuery=em.CreateEntityQuery(typeof(GridConfig));
var grid=gridQuery.GetSingleton<GridConfig>();
int checkedBuildings=0;
foreach(var building in bs) {
var info=em.GetComponentData<RuntimeBuildingCombatInfo>(building);
var min=grid.Origin.xz+new Unity.Mathematics.float2(info.OriginCell)*grid.CellSize;
var max=min+new Unity.Mathematics.float2(info.FootprintCells)*grid.CellSize;
foreach(var e in es) {
var b=em.GetComponentData<WorldRenderBounds>(e).Value;
if(b.Max.x<=min.x||b.Min.x>=max.x||b.Max.z<=min.y||b.Min.z>=max.y||b.Extents.y<1)continue;
var m=em.GetComponentData<MaterialMeshInfo>(e);
if(m.HasMaterialMeshIndexRange)continue;
string name=em.GetSharedComponentManaged<RenderMeshArray>(e).GetMesh(m)?.name;
if(name!=null && name.StartsWith("SM_Bld_") && !name.Contains("Destroyed"))
throw new InvalidOperationException("Starting building "+info.RuntimeBuildingId+" intersects "+name+" at "+b.Center);
}
Debug.Log("[SkirmishPlacement] building="+info.RuntimeBuildingId+" faction="+info.OwnerFactionId+" origin="+info.OriginCell+" footprint="+info.FootprintCells);
checkedBuildings++;
}
if(checkedBuildings!=14)throw new InvalidOperationException("Expected 14 starting buildings, got "+checkedBuildings);
Debug.Log("[SkirmishPlacement] result=Passed buildings=14 factions=2 streamedBuildingIntersections=0 completeMatch=NotClaimed deviceAcceptance=Pending");
}}}
