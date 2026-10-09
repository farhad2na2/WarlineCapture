using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Runtime
{
    /// <summary>Reserves carriageways and sidewalks for preview, starting grants, and final validation.</summary>
    internal sealed class BuildingPlacementAuthoredRoadCache
    {
        private World world;
        private Entity owner, sidewalkOwner;
        private uint revision;
        private uint sidewalkSourceSignature;
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
            // Packed sidewalk owners can stream in after the surface owner.
            // Surface revision alone does not describe that source census.
            using var authoredSidewalks=em.CreateEntityQuery(ComponentType.ReadOnly<OperationMapSidewalkFootprint>());
            using var authoredOwners=authoredSidewalks.ToEntityArray(Unity.Collections.Allocator.Temp);
            uint currentSidewalkSignature=2166136261;
            foreach(var sourceOwner in authoredOwners)
            {
                currentSidewalkSignature=unchecked((currentSidewalkSignature^(uint)sourceOwner.Index)*16777619);
                currentSidewalkSignature=unchecked((currentSidewalkSignature^(uint)sourceOwner.Version)*16777619);
                currentSidewalkSignature=unchecked((currentSidewalkSignature^(uint)em.GetBuffer<OperationMapSidewalkFootprint>(sourceOwner,true).Length)*16777619);
            }
            if(world==em.World && owner==entity && revision==version && surface.SurfaceBlob==current.SurfaceBlob && surface.HasSurfaceData==current.HasSurfaceData &&
                surface.CellSize==current.CellSize && math.all(surface.GridOrigin==current.GridOrigin) &&
                grid.Width==currentGrid.Width && grid.Height==currentGrid.Height && grid.CellSize==currentGrid.CellSize &&
                math.all(grid.Origin==currentGrid.Origin) && sidewalkSourceSignature==currentSidewalkSignature && mask!=null) return;
            if(world!=em.World || !em.Exists(sidewalkOwner))
            {
                using var sidewalkQuery=em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(),ComponentType.ReadOnly<GridRoadSidewalk>());
                sidewalkOwner=sidewalkQuery.CalculateEntityCount()==1 ? sidewalkQuery.GetSingletonEntity() : Entity.Null;
            }
            world=em.World;owner=entity;revision=version;surface=current;grid=currentGrid;sidewalkSourceSignature=currentSidewalkSignature;
            mask=new bool[grid.Width*grid.Height];
            waterMask=SkirmishWaterPlacement.CreateMask(em,grid);
            BuildingPlacementRoadSurfaceMask.Append(em,entity,grid,mask);
            sidewalkMask=new bool[mask.Length];
            var live=GetLiveSidewalks();
            if(live.IsCreated) for(int i=0;i<sidewalkMask.Length && i<live.Length;i++) sidewalkMask[i]=live[i].Value!=0;
                foreach(var mapOwner in authoredOwners)
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
                AppendSidewalkCells(renderer.transform.localToWorldMatrix, local.min, local.max, x0, x1, y0, y1);
            }
        }
        private void AppendSidewalk(in OperationMapSidewalkFootprint footprint)
        {
            int2 min=math.max(0,(int2)math.floor((footprint.WorldMin.xz-grid.Origin.xz)/grid.CellSize));
            int2 max=math.min(new int2(grid.Width,grid.Height),(int2)math.ceil((footprint.WorldMax.xz-grid.Origin.xz)/grid.CellSize));
            AppendSidewalkCells(math.inverse(footprint.WorldToLocal), footprint.LocalMin, footprint.LocalMax, min.x, max.x, min.y, max.y);
        }
        private void AppendSidewalkCells(float4x4 toWorld, float3 localMin, float3 localMax, int x0, int x1, int y0, int y1)
        {
            float height=(localMin.y+localMax.y)*.5f;
            float2 a=math.transform(toWorld,new float3(localMin.x,height,localMin.z)).xz;
            float2 b=math.transform(toWorld,new float3(localMax.x,height,localMin.z)).xz;
            float2 c=math.transform(toWorld,new float3(localMax.x,height,localMax.z)).xz;
            float2 d=math.transform(toWorld,new float3(localMin.x,height,localMax.z)).xz;
            float2 u=b-a,v=d-a;
            // A curb can overlap the edge of a cell without containing its centre.
            // Test the whole cell against the rotated footprint, preserving free corners.
            for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++)
            {
                float2 center=grid.Origin.xz+new float2(x+.5f,y+.5f)*grid.CellSize;
                float radius=grid.CellSize*.5f;
                if(OverlapSidewalkAxis(a,b,c,d,center,radius,new float2(1,0)) &&
                    OverlapSidewalkAxis(a,b,c,d,center,radius,new float2(0,1)) &&
                    OverlapSidewalkAxis(a,b,c,d,center,radius,new float2(-u.y,u.x)) &&
                    OverlapSidewalkAxis(a,b,c,d,center,radius,new float2(-v.y,v.x))) sidewalkMask[y*grid.Width+x]=true;
            }
        }
        private static bool OverlapSidewalkAxis(float2 a,float2 b,float2 c,float2 d,float2 center,float extent,float2 axis)
        {
            float lo=math.min(math.min(math.dot(a-center,axis),math.dot(b-center,axis)),math.min(math.dot(c-center,axis),math.dot(d-center,axis)));
            float hi=math.max(math.max(math.dot(a-center,axis),math.dot(b-center,axis)),math.max(math.dot(c-center,axis),math.dot(d-center,axis)));
            float radius=(math.abs(axis.x)+math.abs(axis.y))*extent;
            return lo<radius-.0001f && hi>-radius+.0001f;
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
            for(int i=0;i<mask.Length;i++) target[i]|=mask[i] || sidewalkMask != null && sidewalkMask[i];
        }
        internal bool Overlaps(GridConfig currentGrid,Vector2Int origin,Vector2Int footprint)
        {
            if(mask==null || currentGrid.Width!=grid.Width || currentGrid.Height!=grid.Height) return false;
            if(!BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(origin,footprint,currentGrid)) return true;
            for(int y=origin.y;y<origin.y+footprint.y;y++)
                for(int x=origin.x;x<origin.x+footprint.x;x++)
                    if(mask[y*grid.Width+x] || sidewalkMask != null && sidewalkMask[y*grid.Width+x]) return true;
            return false;
        }
    }
}
