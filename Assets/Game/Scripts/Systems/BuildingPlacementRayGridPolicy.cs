#if WARLINE_SOURCE_DECLARED_COMPILER_EXPORT
extern alias ActualCore;
using Ray = ActualCore::UnityEngine.Ray;
using Plane = ActualCore::UnityEngine.Plane;
#endif
using UnityEngine;using Unity.Mathematics;using Game.Components;
namespace Game.Runtime {
internal static class BuildingPlacementRayGridPolicy {
 internal static bool TryResolve(Ray ray,GridConfig grid,float buildPlaneY,out Vector2Int cell){
  cell=default;Plane plane=new(Vector3.up,new Vector3(0f,buildPlaneY,0f));
  if(!plane.Raycast(ray,out float distance))return false;
  var point=ray.GetPoint(distance);int2 gridCell=GridUtils.WorldToCell(grid,new float3(point.x,point.y,point.z));
  if(!GridUtils.InBounds(gridCell,grid.Width,grid.Height))return false;
  cell=new Vector2Int(gridCell.x,gridCell.y);return true;
 }
}}
