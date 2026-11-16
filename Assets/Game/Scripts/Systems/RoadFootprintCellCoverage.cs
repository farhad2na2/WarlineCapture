using Unity.Mathematics;using UnityEngine;using Game.Components;
namespace Game.Runtime {internal static class RoadFootprintCellCoverage {
        public static bool IsGridCellCenterInsideBounds(GridConfig grid, int x, int y, Vector3 worldMin, Vector3 worldMax)
        {
            Vector3 center = (Vector3)grid.Origin + new Vector3((x + 0.5f) * grid.CellSize, 0f, (y + 0.5f) * grid.CellSize);
            return center.x >= worldMin.x && center.x < worldMax.x &&
                   center.z >= worldMin.z && center.z < worldMax.z;
        }
        private static void GetGridBounds(GridConfig grid, Vector3 worldMin, Vector3 worldMax, out int minX, out int minY, out int maxX, out int maxY)
        {
            float3 localMin = (float3)(worldMin - (Vector3)grid.Origin);
            float3 localMax = (float3)(worldMax - (Vector3)grid.Origin);

            minX = Mathf.Clamp(Mathf.FloorToInt(localMin.x / grid.CellSize), 0, grid.Width);
            minY = Mathf.Clamp(Mathf.FloorToInt(localMin.z / grid.CellSize), 0, grid.Height);
            maxX = Mathf.Clamp(Mathf.CeilToInt(localMax.x / grid.CellSize), 0, grid.Width);
            maxY = Mathf.Clamp(Mathf.CeilToInt(localMax.z / grid.CellSize), 0, grid.Height);
        }
 internal static bool Overlaps(GridConfig grid,Vector3 worldMin,Vector3 worldMax,Vector2Int originCell,Vector2Int footprintCells){
  int buildingMinX=originCell.x,buildingMinY=originCell.y,buildingMaxX=originCell.x+footprintCells.x,buildingMaxY=originCell.y+footprintCells.y;
  GetGridBounds(grid,worldMin,worldMax,out int minX,out int minY,out int maxX,out int maxY);
  for(int y=minY;y<maxY;y++)for(int x=minX;x<maxX;x++){
   if(!IsGridCellCenterInsideBounds(grid,x,y,worldMin,worldMax))continue;
   if(x<buildingMinX||y<buildingMinY||x>=buildingMaxX||y>=buildingMaxY)continue;
   return true;
  }return false;
 }
}}
