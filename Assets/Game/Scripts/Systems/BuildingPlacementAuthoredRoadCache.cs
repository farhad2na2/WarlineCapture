using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>One road projection per map/mesh revision, shared by preview, walls, and final validation.</summary>
    internal sealed class BuildingPlacementAuthoredRoadCache
    {
        private World world;
        private Entity owner, sidewalkOwner;
        private uint revision;
        private MapSurfaceComponent surface;
        private GridConfig grid;
        private bool[] mask, sidewalkMask, waterMask;

        internal void Ensure(EntityManager em,EntityQuery query,GridConfig currentGrid)
        {
            if(query.CalculateEntityCount()!=1) {mask=null;sidewalkMask=null;waterMask=null;world=null;return;}
            Entity entity=query.GetSingletonEntity();
            var current=em.GetComponentData<MapSurfaceComponent>(entity);
            uint version=em.HasComponent<MapSurfaceSceneOverlayRevision>(entity)
                ? em.GetComponentData<MapSurfaceSceneOverlayRevision>(entity).Value : 0;
            if(world==em.World && owner==entity && revision==version && surface.SurfaceBlob==current.SurfaceBlob && surface.HasSurfaceData==current.HasSurfaceData &&
                surface.CellSize==current.CellSize && math.all(surface.GridOrigin==current.GridOrigin) &&
                grid.Width==currentGrid.Width && grid.Height==currentGrid.Height && grid.CellSize==currentGrid.CellSize &&
                math.all(grid.Origin==currentGrid.Origin) && mask!=null) return;
            if(world!=em.World || !em.Exists(sidewalkOwner))
            {
                using var sidewalkQuery=em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(),ComponentType.ReadOnly<GridRoadSidewalk>());
                sidewalkOwner=sidewalkQuery.CalculateEntityCount()==1 ? sidewalkQuery.GetSingletonEntity() : Entity.Null;
            }
            world=em.World;owner=entity;revision=version;surface=current;grid=currentGrid;
            mask=new bool[grid.Width*grid.Height];
            waterMask=SkirmishWaterPlacement.CreateMask(em,grid);
            BuildingPlacementRoadSurfaceMask.Append(em,entity,grid,mask);
            sidewalkMask=new bool[mask.Length];
            var live=GetLiveSidewalks();
            if(live.IsCreated) for(int i=0;i<sidewalkMask.Length && i<live.Length;i++) sidewalkMask[i]=live[i].Value!=0;
            using(var sidewalkQuery=em.CreateEntityQuery(ComponentType.ReadOnly<OperationMapSidewalkFootprint>()))
            using(var owners=sidewalkQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
                foreach(var mapOwner in owners)
                    foreach(var footprint in em.GetBuffer<OperationMapSidewalkFootprint>(mapOwner,true))
                        AppendSidewalk(footprint);
            // Authored operation-map sidewalks can be visual meshes without GridRoadSidewalk records.
            // Reserve their actual footprints rather than treating the entire road prefab as carriageway.
            foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!renderer.gameObject.scene.IsValid() || !RoadGridProjectionSystem.TryGetFootprintKind(renderer.transform,false,out var kind) ||
                    kind!=RoadGridProjectionSystem.RoadFootprintKind.Sidewalk) continue;
                var mesh=renderer.GetComponent<MeshFilter>(); if(mesh==null || mesh.sharedMesh==null) continue;
                var bounds=renderer.bounds; var local=mesh.sharedMesh.bounds;
                int x0=Mathf.Max(0,Mathf.FloorToInt((bounds.min.x-grid.Origin.x)/grid.CellSize));
                int x1=Mathf.Min(grid.Width,Mathf.CeilToInt((bounds.max.x-grid.Origin.x)/grid.CellSize));
                int y0=Mathf.Max(0,Mathf.FloorToInt((bounds.min.z-grid.Origin.z)/grid.CellSize));
                int y1=Mathf.Min(grid.Height,Mathf.CeilToInt((bounds.max.z-grid.Origin.z)/grid.CellSize));
                var matrix=renderer.transform.worldToLocalMatrix;
                for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++)
                {
                    var point=matrix.MultiplyPoint3x4(new Vector3(grid.Origin.x+(x+.5f)*grid.CellSize,bounds.center.y,grid.Origin.z+(y+.5f)*grid.CellSize));
                    if(point.x>=local.min.x && point.x<=local.max.x && point.z>=local.min.z && point.z<=local.max.z) sidewalkMask[y*grid.Width+x]=true;
                }
            }
        }
        private void AppendSidewalk(in OperationMapSidewalkFootprint footprint)
        {
            int2 min=math.max(0,(int2)math.floor((footprint.WorldMin.xz-grid.Origin.xz)/grid.CellSize));
            int2 max=math.min(new int2(grid.Width,grid.Height),(int2)math.ceil((footprint.WorldMax.xz-grid.Origin.xz)/grid.CellSize));
            float height=(footprint.WorldMin.y+footprint.WorldMax.y)*.5f;
            for(int y=min.y;y<max.y;y++) for(int x=min.x;x<max.x;x++)
            {
                var point=math.transform(footprint.WorldToLocal,new float3(grid.Origin.x+(x+.5f)*grid.CellSize,height,grid.Origin.z+(y+.5f)*grid.CellSize));
                if(math.all(point.xz>=footprint.LocalMin.xz) && math.all(point.xz<=footprint.LocalMax.xz)) sidewalkMask[y*grid.Width+x]=true;
            }
        }
        private DynamicBuffer<GridRoadSidewalk> GetLiveSidewalks() => world!=null && world.IsCreated && world.EntityManager.Exists(sidewalkOwner)
            ? world.EntityManager.GetBuffer<GridRoadSidewalk>(sidewalkOwner,true) : default;
        internal bool[] GetSidewalks()=>sidewalkMask;
        internal bool[] GetWater()=>waterMask;
        internal void AppendWaterTo(bool[] target)
        {
            if(waterMask==null || waterMask.Length!=target.Length)return;
            for(int i=0;i<target.Length;i++)target[i]|=waterMask[i];
        }
        internal bool OverlapsWater(GridConfig currentGrid,Vector2Int origin,Vector2Int footprint)
        {
            if(waterMask==null)return false;
            if(!BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(origin,footprint,currentGrid))return true;
            for(int y=origin.y;y<origin.y+footprint.y;y++)
                for(int x=origin.x;x<origin.x+footprint.x;x++)if(waterMask[y*currentGrid.Width+x])return true;
            return false;
        }
        internal void AppendTo(bool[] target)
        {
            if(mask==null || mask.Length!=target.Length) return;
            for(int i=0;i<mask.Length;i++) target[i]|=mask[i];
        }
        internal bool Overlaps(GridConfig currentGrid,Vector2Int origin,Vector2Int footprint)
        {
            if(mask==null || currentGrid.Width!=grid.Width || currentGrid.Height!=grid.Height) return false;
            if(!BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(origin,footprint,currentGrid)) return true;
            for(int y=origin.y;y<origin.y+footprint.y;y++)
                for(int x=origin.x;x<origin.x+footprint.x;x++) if(mask[y*grid.Width+x]) return true;
            return false;
        }
    }
}
