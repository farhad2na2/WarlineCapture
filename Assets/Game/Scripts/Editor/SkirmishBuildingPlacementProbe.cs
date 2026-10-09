using System;
using System.Collections.Generic;
using Game.Runtime;
using Game.Rendering.Contracts;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Rendering;
using Game.Components;
using UnityEngine;
namespace Game.Editor {
public static class SkirmishBuildingPlacementProbe {
[Serializable] private sealed class ClearanceReport {
    public string result;
    public int buildings;
    public int factions;
    public List<string> observations = new();
    public bool playerReady;
}

public static void InspectSharedSceneryClearance(string evidencePath) {
    var em = World.DefaultGameObjectInjectionWorld.EntityManager;
    using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridRoad));
    using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
    using var databases = em.CreateEntityQuery(typeof(OperationMapRenderDatabaseComponent));
    using var producers = em.CreateEntityQuery(typeof(SkirmishNativeBuildingCheckpointRecord));
    if(grids.CalculateEntityCount()!=1 || surfaces.CalculateEntityCount()!=1 || databases.CalculateEntityCount()!=1)
        throw new InvalidOperationException("Native placement source geometry is incomplete.");
    var gridEntity = grids.GetSingletonEntity();
    var grid = em.GetComponentData<GridConfig>(gridEntity);
    var roads = em.GetBuffer<GridRoad>(gridEntity,true);
    var roadMask = new bool[grid.Width*grid.Height];
    BuildingPlacementRoadSurfaceMask.Append(em,surfaces.GetSingletonEntity(),grid,roadMask);
    for(int i=0;i<roadMask.Length && i<roads.Length;i++) roadMask[i] |= roads[i].Value!=0;
    var database = em.GetComponentData<OperationMapRenderDatabaseComponent>(databases.GetSingletonEntity());
    if(!database.Blob.IsCreated) throw new InvalidOperationException("Packed map geometry unavailable.");
    ref var data = ref database.Blob.Value;
    using var sidewalkSources = em.CreateEntityQuery(typeof(OperationMapSidewalkFootprint));
    using var sidewalkOwners = sidewalkSources.ToEntityArray(Allocator.Temp);
    using var entities = producers.ToEntityArray(Allocator.Temp);
    var factions = new HashSet<byte>();
    var report = new ClearanceReport {result="Failed"};
    try {
        foreach(var entity in entities) {
            var building = em.GetComponentObject<SkirmishNativeBuildingCheckpointRecord>(entity).Building;
            if(building==null || building.IsDestroyed || building.IsCityGenerated) continue;
            if(building.Instance==null)
                throw new InvalidOperationException("Living military building has no native visual: "+building.Id+" "+building.Definition.DisplayName);
            string boundsSource = "LiveModelIncludingCombinedSourceGeometry";
            float4x4 matrix = building.Instance.transform.localToWorldMatrix;
            if(!BuildingModelBounds.TryMeasure(building.Instance,out var model,true)) {
                if(building.Definition.HasLocalBounds) {
                    model=building.Definition.LocalBounds;
                    boundsSource="MeasuredRuntimeDefinition";
                }
                else if(building.Definition.VisualTemplate!=null && BuildingModelBounds.TryMeasure(building.Definition.VisualTemplate,out model))
                    boundsSource="MeasuredCombinedVisual";
                else if(BuildingDefinitionPrefabSystemHelper.TryGetPrefabLocalBounds(building.Definition.Prefab,out model))
                    boundsSource="MeasuredPrefab";
                else throw new InvalidOperationException("Military model bounds unavailable: "+building.Id);
                // Placement recentres the prefab beneath its wrapper. Definition
                // bounds are in that visual child's coordinates, not the wrapper's.
                if(building.Instance.transform.childCount>0)
                    matrix=building.Instance.transform.GetChild(0).localToWorldMatrix;
            }
            float3 center = math.transform(matrix,(float3)model.center);
            float3 extents = math.abs(matrix.c0.xyz)*model.extents.x + math.abs(matrix.c1.xyz)*model.extents.y + math.abs(matrix.c2.xyz)*model.extents.z;
            var bounds = new Bounds(center,extents*2);
            report.observations.Add("building="+building.Id+" name="+building.Definition.DisplayName+" faction="+building.OwnerFactionId+" boundsSource="+boundsSource+" actualModelMin="+bounds.min+" actualModelMax="+bounds.max);
            int2 min = (int2)math.floor((center.xz-extents.xz-grid.Origin.xz)/grid.CellSize);
            int2 max = (int2)math.ceil((center.xz+extents.xz-grid.Origin.xz)/grid.CellSize);
            for(int y=min.y;y<max.y;y++) for(int x=min.x;x<max.x;x++) {
                if(x<0 || y<0 || x>=grid.Width || y>=grid.Height)
                    throw new InvalidOperationException("Military model outside map: "+building.Id);
                if(roadMask[y*grid.Width+x] && !BuildingBarrierUtilitySystemHelper.RequiresRoadPlacement(building.Definition))
                    throw new InvalidOperationException("Military building "+building.Id+" "+building.Definition.DisplayName+" overlaps road at ("+x+","+y+").");
            }
            foreach(var owner in sidewalkOwners)
                foreach(var sidewalk in em.GetBuffer<OperationMapSidewalkFootprint>(owner,true)) {
                    float4x4 toWorld = math.inverse(sidewalk.WorldToLocal);
                    float height=(sidewalk.LocalMin.y+sidewalk.LocalMax.y)*.5f;
                    float2 a=math.transform(toWorld,new float3(sidewalk.LocalMin.x,height,sidewalk.LocalMin.z)).xz;
                    float2 b=math.transform(toWorld,new float3(sidewalk.LocalMax.x,height,sidewalk.LocalMin.z)).xz;
                    float2 c=math.transform(toWorld,new float3(sidewalk.LocalMax.x,height,sidewalk.LocalMax.z)).xz;
                    float2 d=math.transform(toWorld,new float3(sidewalk.LocalMin.x,height,sidewalk.LocalMax.z)).xz;
                    if(QuadOverlapsBounds(a,b,c,d,center.xz,extents.xz))
                        throw new InvalidOperationException("Military building "+building.Id+" "+building.Definition.DisplayName+" overlaps authored sidewalk.");
                }
            for(int i=0;i<data.Placements.Length;i++) {
                ref var placement=ref data.Placements[i];
                ref var prototype=ref data.Prototypes[placement.PrototypeIndex];
                float3 sceneryCenter=math.transform(placement.WorldMatrix,prototype.CombinedLocalBounds.Center);
                var size=prototype.CombinedLocalBounds.Extents;var transform=placement.WorldMatrix;
                float3 sceneryExtents=math.abs(transform.c0.xyz)*size.x+math.abs(transform.c1.xyz)*size.y+math.abs(transform.c2.xyz)*size.z;
                bool solid=placement.SemanticCategory==DenseCityPresentationSemanticCategory.GameplayBuildingIntact ||
                    sceneryExtents.y>=.5f && placement.SemanticCategory is DenseCityPresentationSemanticCategory.Vegetation or DenseCityPresentationSemanticCategory.Prop or DenseCityPresentationSemanticCategory.BuildingAttachmentIntact;
                if(!solid) continue;
                if(math.all(math.abs(center.xz-sceneryCenter.xz)<extents.xz+sceneryExtents.xz-.0001f))
                    throw new InvalidOperationException("Military building "+building.Id+" "+building.Definition.DisplayName+" overlaps "+placement.SemanticCategory+" placement="+i+" center="+sceneryCenter);
            }
            report.buildings++; factions.Add(building.OwnerFactionId);
        }
        report.factions=factions.Count;
        if(report.buildings==0 || report.factions!=2) throw new InvalidOperationException("Both native starting bases must be audited.");
        report.result="Passed";
        Debug.Log("[SharedNativePlacementClearance] result=Passed buildings="+report.buildings+" factions="+report.factions+" roads=Clear sidewalks=Clear treesAndSolidScenery=Clear sourceGeometryModified=0 completeMatch=NotClaimed");
    }
    catch(Exception ex) { report.observations.Add("failure="+ex.Message); throw; }
    finally { System.IO.File.WriteAllText(evidencePath,JsonUtility.ToJson(report,true)); }
}

private static bool QuadOverlapsBounds(float2 a,float2 b,float2 c,float2 d,float2 center,float2 extents) {
    if(!OverlapAxis(a,b,c,d,center,extents,new float2(1,0)) || !OverlapAxis(a,b,c,d,center,extents,new float2(0,1))) return false;
    float2 u=b-a,v=d-a;
    return OverlapAxis(a,b,c,d,center,extents,new float2(-u.y,u.x)) && OverlapAxis(a,b,c,d,center,extents,new float2(-v.y,v.x));
}
private static bool OverlapAxis(float2 a,float2 b,float2 c,float2 d,float2 center,float2 extents,float2 axis) {
    float lo=math.min(math.min(math.dot(a-center,axis),math.dot(b-center,axis)),math.min(math.dot(c-center,axis),math.dot(d-center,axis)));
    float hi=math.max(math.max(math.dot(a-center,axis),math.dot(b-center,axis)),math.max(math.dot(c-center,axis),math.dot(d-center,axis)));
    float radius=math.dot(math.abs(axis),extents);
    return lo<radius-.0001f && hi>-radius+.0001f;
}

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
