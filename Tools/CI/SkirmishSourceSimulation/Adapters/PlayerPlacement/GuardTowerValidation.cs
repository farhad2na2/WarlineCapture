using System;using Game.Components;using Unity.Entities;
namespace Game.Runtime {
// Guard-tower-only binding of the non-road-gate branch of the original
// BuildingPlacementInvalidCellCacheCompositionSystemHelper.IsPlacementValid.
internal sealed class SourceGuardTowerPlacementValidation {
 readonly EntityManager manager;readonly Entity gridEntity;readonly GeneratedBuildingMask buildings;readonly GeneratedRoadMask roads;readonly Func<int,int,int,int,bool> runtimeBlocker;readonly Func<GridConfig,Vector2Int,Vector2Int,bool> sourceRoadFootprint;readonly Func<RectInt,bool> runtimeOverlap;readonly Func<RuntimeDefinition,Vector2Int,GridConfig,bool,RectInt> effectiveRect;
 internal SourceGuardTowerPlacementValidation(EntityManager em,Entity grid,GeneratedBuildingMask buildings,GeneratedRoadMask roads,Func<int,int,int,int,bool> blocker,Func<GridConfig,Vector2Int,Vector2Int,bool> roadFootprint,Func<RectInt,bool> overlap,Func<RuntimeDefinition,Vector2Int,GridConfig,bool,RectInt> effectiveRect){manager=em??throw new ArgumentNullException(nameof(em));gridEntity=grid;this.buildings=buildings??throw new ArgumentNullException(nameof(buildings));this.roads=roads??throw new ArgumentNullException(nameof(roads));runtimeBlocker=blocker??throw new ArgumentNullException(nameof(blocker));sourceRoadFootprint=roadFootprint??throw new ArgumentNullException(nameof(roadFootprint));runtimeOverlap=overlap??throw new ArgumentNullException(nameof(overlap));this.effectiveRect=effectiveRect??throw new ArgumentNullException(nameof(effectiveRect));}
 internal bool IsPlacementValid(RuntimeDefinition definition,Vector2Int originCell,Vector2Int footprintCells,bool rotateVertical){if(definition==null||!definition.SourceMetadataComplete||definition.Prefab==null||definition.Prefab.name!="Building_GuardTower"||definition.HasRunway||definition.IsWall||BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(definition)||rotateVertical)throw new NotSupportedException("Actual complete unrotated guard tower required");if(!manager.Exists(gridEntity)||!manager.HasComponent<GridConfig>(gridEntity)||!manager.HasBuffer<GridRoad>(gridEntity)||!manager.HasComponent<DynamicBlockerComponent>(gridEntity))throw new NotSupportedException("Same live grid and original buffers required");var grid=manager.GetComponentData<GridConfig>(gridEntity);var gridRoads=manager.GetBuffer<GridRoad>(gridEntity);var blockerData=manager.GetComponentData<DynamicBlockerComponent>(gridEntity);RectInt placementRect=effectiveRect(definition,originCell,grid,rotateVertical);if(!SkirmishSurfaceValid(placementRect)||!TerrainFootprintWalkable(placementRect,grid))return false;var source=new ValidationSource(this,placementRect,grid,gridRoads,blockerData);return BuildingPlacementOrdinaryValidationPolicy.IsValid(ref source);}
 // Match the original player-placement phase and singleton gates; the level
 // calculation itself is extracted unchanged from the gameplay owner.
 bool SkirmishSurfaceValid(RectInt rect){
  using(var matches=manager.CreateEntityQuery(typeof(SkirmishMatchState))){
   int count=matches.CalculateEntityCount();
   if(count==0)return true;
   if(count!=1)throw new NotSupportedException("One source Skirmish match required");
   if(matches.GetSingleton<SkirmishMatchState>().Phase!=SkirmishPhase.Playing)return true;
  }
  using(var surfaces=manager.CreateEntityQuery(typeof(MapSurfaceComponent))){
   if(surfaces.CalculateEntityCount()!=1)return false;
   return SourceSkirmishSurfacePlacementOwner.IsSkirmishFootprintLevel(surfaces.GetSingleton<MapSurfaceComponent>(),rect);
  }
 }
 // Source-host terrain is a separate conservative placement input: the native
 // owner below remains responsible for authored, road and runtime checks.
 bool TerrainFootprintWalkable(RectInt rect,GridConfig grid){
  if(!manager.HasBuffer<GridWalkable>(gridEntity))throw new NotSupportedException("Actual source grid walkability required");
  var cells=manager.GetBuffer<GridWalkable>(gridEntity,true);
  if(cells.Length!=checked(grid.Width*grid.Height))throw new NotSupportedException("Complete source grid walkability required");
  if(rect.width<=0||rect.height<=0||rect.xMin<0||rect.yMin<0||rect.xMax>grid.Width||rect.yMax>grid.Height)return false;
  for(int y=rect.yMin;y<rect.yMax;y++)for(int x=rect.xMin;x<rect.xMax;x++){
   byte value=cells[y*grid.Width+x].Value;
   if(value>1)throw new NotSupportedException("Binary source grid walkability required");
   if(value==0)return false;
  }
  return true;
 }
 readonly struct ValidationSource:IOrdinaryPlacementValidationSource {readonly SourceGuardTowerPlacementValidation owner;readonly RectInt rect;readonly GridConfig grid;readonly DynamicBuffer<GridRoad> gridRoads;readonly DynamicBlockerComponent blockers;internal ValidationSource(SourceGuardTowerPlacementValidation owner,RectInt rect,GridConfig grid,DynamicBuffer<GridRoad> gridRoads,DynamicBlockerComponent blockers){this.owner=owner;this.rect=rect;this.grid=grid;this.gridRoads=gridRoads;this.blockers=blockers;}public bool AuthoredOverlap()=>owner.buildings.Overlaps(grid,rect.position,rect.size);public bool WaterOverlap()=>owner.roads.OverlapsWater(grid,rect.position,rect.size);public bool RoadOverlap()=>owner.roads.Overlaps(grid,rect.position,rect.size);public bool ValidRect()=>BuildingPlacementValidationUtilitySystemHelper.IsPlacementRectValid(rect,grid,gridRoads,blockers,false,null,0,0,owner.runtimeBlocker,owner.sourceRoadFootprint,owner.runtimeOverlap,false);}

}}
